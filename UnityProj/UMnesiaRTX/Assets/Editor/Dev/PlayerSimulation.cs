using System;
using System.IO;
using System.Reflection;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Runs the player of a converted level without play mode (the scene is not saved): scripted input, fixed
    /// 60 Hz steps, state logged to Logs/AmnesiaImport.log and camera shots to Logs/Player_&lt;map&gt;_NN_*.png.
    /// Batch: -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch [-amnesiaMap maps/...map] [-script "..."]
    /// (shots need a GPU: no -nographics). Script commands, ';'-separated, durations in seconds:
    /// idle t | w/s/a/d t | run t | turn deg | pitch deg | crouch (toggle) | jump | leanr/leanl t |
    /// start AreaName | goto x y z [yawDeg] | shot name.
    /// </summary>
    public static class PlayerSimulation
    {
        const string DefaultScript = "idle 1; shot start; w 2; shot walked; run 2; shot ran; turn 90; w 3; shot turned; " +
                                     "crouch; idle 1; shot crouched; crouch; idle 1; jump; idle 0.25; shot jumping; idle 1.5; " +
                                     "leanr 1; shot lean_right; idle 1; turn 180; w 4; shot back";

        public static void RunBatch()
        {
            string map = ImportMenu.Arg("-amnesiaMap", ImportMenu.TestMap);
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, map));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Run(Path.GetFileNameWithoutExtension(map), ImportMenu.Arg("-script", DefaultScript), SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null);
        }

        static void Run(string mapName, string script, bool shots)
        {
            var log = new ImportLog();
            var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
            if (player == null)
            {
                log.Error("No AmnesiaPlayer in the scene (re-convert the level)");
                log.Save("Player simulation");
                return;
            }

            Physics.SyncTransforms();
            player.readDevices = false;
            Call(player, "Awake");
            Call(player, "Start");
            float dt = Time.fixedDeltaTime;
            log.Info($"Fixed step {dt:0.#####} s; start feet {player.Body.FeetPosition:F3} yaw {player.Body.Yaw * Mathf.Rad2Deg:0.#}°");

            int shot = 0;
            float time = 0f, maxSpeed = 0f, minY = float.MaxValue, maxY = float.MinValue;
            void Step(int count)
            {
                maxSpeed = 0f;
                minY = float.MaxValue;
                maxY = float.MinValue;
                for (int i = 0; i < count; i++)
                {
                    Call(player, "FixedUpdate");
                    time += dt;
                    Vector3 v = player.Body.GetVelocity(dt);
                    maxSpeed = Mathf.Max(maxSpeed, new Vector2(v.x, v.z).magnitude);
                    minY = Mathf.Min(minY, player.Body.FeetPosition.y);
                    maxY = Mathf.Max(maxY, player.Body.FeetPosition.y);
                }
                player.SnapCamera();
            }
            void State(string what)
            {
                var b = player.Body;
                Vector3 cam = player.playerCamera.transform.position - b.FeetPosition;
                log.Info($"t={time,6:0.00} {what,-16} feet {b.FeetPosition:F3} y {minY:0.000}..{maxY:0.000} max {maxSpeed:0.00} m/s now {b.GetVelocity(dt).magnitude:0.00} " +
                         $"ground {b.IsOnGround} size {b.ActiveSize} cam up {cam.y:0.000} side {Vector3.Dot(cam, b.Right):0.000} " +
                         $"roll {player.playerCamera.transform.eulerAngles.z:0.0}");
            }

            foreach (string raw in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0) continue;
                float arg = p.Length > 1 && float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float a) ? a : 1f;
                int steps = Mathf.RoundToInt(arg / dt);
                player.held = default;
                switch (p[0])
                {
                    case "idle": Step(steps); break;
                    case "w": player.held.forward = true; Step(steps); break;
                    case "s": player.held.backward = true; Step(steps); break;
                    case "a": player.held.left = true; Step(steps); break;
                    case "d": player.held.right = true; Step(steps); break;
                    case "run":
                        player.held.forward = true;
                        player.Run(true);
                        Step(steps);
                        player.Run(false);
                        break;
                    case "turn": player.AddLook(arg * Mathf.Deg2Rad, 0f); Step(1); break;
                    case "pitch": player.AddLook(0f, arg * Mathf.Deg2Rad); Step(1); break;
                    case "crouch": player.Crouch(true); Step(1); break;
                    case "jump": player.Jump(true); Step(1); player.Jump(false); break;
                    case "leanr": player.held.leanRight = true; Step(steps); break;
                    case "leanl": player.held.leanLeft = true; Step(steps); break;
                    case "start":
                        var area = Array.Find(UnityEngine.Object.FindObjectsByType<RTXMnesia.World.AmnesiaArea>(FindObjectsInactive.Exclude),
                                              x => x.name == (p.Length > 1 ? p[1] : ""));
                        if (area == null) { log.Warn($"No area '{raw.Trim()}'"); continue; }
                        player.PlaceAtStart(area);
                        Step(1);
                        break;
                    case "goto":
                        float F(int i) => float.Parse(p[i], System.Globalization.CultureInfo.InvariantCulture);
                        player.PlaceAt(new Vector3(F(1), F(2), F(3)), p.Length > 4 ? F(4) * Mathf.Deg2Rad : 0f);
                        Step(1);
                        break;
                    case "shot":
                        if (shots)
                        {
                            string file = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", $"Player_{mapName}_{shot++:00}_{(p.Length > 1 ? p[1] : "shot")}.png");
                            ImportMenu.RenderToFile(player.playerCamera, file);
                        }
                        break;
                    default: log.Warn($"Unknown command '{raw.Trim()}'"); continue;
                }
                State(raw.Trim());
            }
            log.Save($"Player simulation: {mapName}");
        }

        static void Call(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
