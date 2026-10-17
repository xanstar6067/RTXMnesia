using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Runs a converted level without play mode (the scene is not saved): scripted input, fixed 60 Hz steps, state
    /// logged to Logs/AmnesiaImport.log and camera shots to Logs/Player_&lt;map&gt;_NN_*.png. If the level has an
    /// <see cref="HplMap"/>, the steps go through it: the level script, its timers and callbacks, effects and HUD run
    /// too, and script messages are copied into the log.
    /// Batch: -executeMethod RTXMnesia.EditorDev.PlayerSimulation.RunBatch [-amnesiaMap maps/...map] [-script "..."]
    /// [-scriptDebug] [-noScript] [-seed N] [-loadSave path] (shots need a GPU: no -nographics). Script commands, ';'-separated,
    /// durations in seconds:
    /// idle t | watch t interval | w/s/a/d t | run t | turn deg | pitch deg | crouch (toggle) | jump | leanr/leanl t |
    /// start AreaName | goto x y z [yawDeg] | shot name |
    /// interact | release | throw | scroll n | rotate on/off | mouse x y t (HPL2 mouse units per step) | aim Entity [Body] |
    /// body Entity (bodies and joints) | call Function args (script or engine function).
    /// "watch" steps like idle and logs the script state every interval.
    /// until Var value timeout | expectvar Var value: wait for / assert a local script variable.
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

            if (int.TryParse(ImportMenu.Arg("-seed", ""), out int seed)) HplMath.Seed(seed);
            HplGame.Reset();
            string savePath = ImportMenu.Arg("-loadSave", "");
            if (savePath != "")
            {
                var save = RTXMnesia.Save.HplSaveHandler.ReadFile(savePath);
                if (save == null) throw new InvalidDataException("Cannot read -loadSave: " + savePath);
                RTXMnesia.Save.HplSaveHandler.BeginLoad(save);
            }
            // -globals "name=value;…": global script variables set by other maps (hub return visits).
            foreach (string pair in ImportMenu.Arg("-globals", "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = pair.IndexOf('=');
                if (eq > 0) HplGame.SetGlobalVar(pair.Substring(0, eq).Trim(), pair.Substring(eq + 1).Trim());
            }

            float time = 0f;
            void OnLog(string message, string stack, LogType type)
            {
                if (!message.StartsWith("[Script")) return;
                string line = $"t={time,6:0.00} {message}";
                if (type == LogType.Error || type == LogType.Exception) log.Error(line);
                else if (type == LogType.Warning) log.Warn(line);
                else log.Info(line);
            }
            Application.logMessageReceived += OnLog;
            // Rigid bodies are stepped by the map (Physics.Simulate), not by the editor.
            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            try
            {
                Physics.SyncTransforms();
                player.readDevices = false;
                Call(player, "Awake");
                Call(player, "Start");

                var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
                if (map != null)
                {
                    map.scriptDebug = ImportMenu.HasArg("-scriptDebug");
                    map.runScript = !ImportMenu.HasArg("-noScript");
                    map.logDebugMessages = true;
                    map.Build();
                    map.Begin();
                    log.Info($"Map {map.mapFile}: script {map.Script?.GetType().Name ?? "none"}, {map.Entities.Count} script entities, " +
                             $"debug {map.scriptDebug}");
                }

                float dt = Time.fixedDeltaTime;
                log.Info($"Fixed step {dt:0.#####} s; start feet {player.Body.FeetPosition:F3} yaw {player.Body.Yaw * Mathf.Rad2Deg:0.#}°");

                int shot = 0;
                float maxSpeed = 0f, minY = float.MaxValue, maxY = float.MinValue;
                void Step(int count, float logInterval = 0f)
                {
                    maxSpeed = 0f;
                    minY = float.MaxValue;
                    maxY = float.MinValue;
                    float nextLog = time + logInterval;
                    for (int i = 0; i < count; i++)
                    {
                        if (map != null) map.Step(dt);
                        else Call(player, "FixedUpdate");
                        time += dt;
                        Vector3 v = player.Body.GetVelocity(dt);
                        maxSpeed = Mathf.Max(maxSpeed, new Vector2(v.x, v.z).magnitude);
                        minY = Mathf.Min(minY, player.Body.FeetPosition.y);
                        maxY = Mathf.Max(maxY, player.Body.FeetPosition.y);
                        if (logInterval > 0f && time >= nextLog - 1e-4f)
                        {
                            ScriptState(log, map, player, time);
                            nextLog += logInterval;
                        }
                    }
                    player.SnapCamera();
                    if (map != null) map.hud.Refresh();
                }
                void State(string what)
                {
                    var b = player.Body;
                    Vector3 cam = player.playerCamera.transform.position - b.FeetPosition;
                    log.Info($"t={time,6:0.00} {what,-16} feet {b.FeetPosition:F3} y {minY:0.000}..{maxY:0.000} max {maxSpeed:0.00} m/s now {b.GetVelocity(dt).magnitude:0.00} " +
                             $"ground {b.IsOnGround} size {b.ActiveSize} cam up {cam.y:0.000} side {Vector3.Dot(cam, b.Right):0.000} " +
                             $"roll {player.playerCamera.transform.eulerAngles.z:0.0} | {player.States?.Current.Describe()} " +
                             $"crosshair {player.States?.Current.GetCrosshair()?.ToString() ?? "none"} text \"{player.FocusText.Replace('\n', ' ')}\"");
                }

                foreach (string raw in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] p = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length == 0) continue;
                    float Arg(int i, float fallback) =>
                        p.Length > i && float.TryParse(p[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float a) ? a : fallback;
                    float arg = Arg(1, 1f);
                    int steps = Mathf.RoundToInt(arg / dt);
                    player.held = default;
                    // The input handler gives an inactive player no input (cLuxInputHandler).
                    bool input = player.active;
                    switch (p[0])
                    {
                        case "idle": Step(steps); break;
                        case "watch": Step(steps, Arg(2, 1f)); break;
                        case "until":
                        {
                            string name = p[1], expected = p[2];
                            int limit = Mathf.CeilToInt(Arg(3, 240f) / dt);
                            while (map.GetVar(name) != expected && limit-- > 0) Step(1);
                            if (map.GetVar(name) != expected) throw new InvalidOperationException($"Timeout: {name}={map.GetVar(name)}, expected {expected}");
                            log.Info($"t={time:0.00} reached {name}={expected}");
                            break;
                        }
                        case "expectvar":
                            // Script integer variables default to zero even when their stored text is empty.
                            int.TryParse(map.GetVar(p[1]), NumberStyles.Integer, CultureInfo.InvariantCulture, out int actual);
                            if (actual != int.Parse(p[2], CultureInfo.InvariantCulture)) throw new InvalidOperationException($"FAIL: {p[1]}={actual}, expected {p[2]}");
                            log.Info($"PASS: {p[1]}={p[2]}");
                            break;
                        case "w": player.held.forward = true; Step(steps); break;
                        case "s": player.held.backward = true; Step(steps); break;
                        case "a": player.held.left = true; Step(steps); break;
                        case "d": player.held.right = true; Step(steps); break;
                        case "run":
                            player.held.forward = true;
                            if (input) player.Run(true);
                            Step(steps);
                            player.Run(false);
                            break;
                        case "turn": if (input) player.AddLook(arg * Mathf.Deg2Rad, 0f); Step(1); break;
                        case "pitch": if (input) player.AddLook(0f, arg * Mathf.Deg2Rad); Step(1); break;
                        case "crouch": if (input) player.Crouch(true); Step(1); break;
                        case "jump": if (input) player.Jump(true); Step(1); player.Jump(false); break;
                        case "leanr": player.held.leanRight = true; Step(steps); break;
                        case "leanl": player.held.leanLeft = true; Step(steps); break;
                        case "start":
                            var area = Array.Find(UnityEngine.Object.FindObjectsByType<RTXMnesia.World.AmnesiaArea>(FindObjectsInactive.Include),
                                                  x => x.name == (p.Length > 1 ? p[1] : ""));
                            if (area == null) { log.Warn($"No area '{raw.Trim()}'"); continue; }
                            player.PlaceAtStart(area);
                            Step(1);
                            break;
                        case "goto":
                            float F(int i) => float.Parse(p[i], CultureInfo.InvariantCulture);
                            player.PlaceAt(new Vector3(F(1), F(2), F(3)), p.Length > 4 ? F(4) * Mathf.Deg2Rad : 0f);
                            Step(1);
                            break;
                        case "fx":
                            // fx sepia|trail|fade|blur|flash|off value [blurStartDist]: set a screen effect at once.
                            if (map == null) { log.Warn("fx needs an HplMap"); continue; }
                            var fx = map.effects;
                            string which = p.Length > 1 ? p[1] : "";
                            float v = Arg(2, 1f);
                            switch (which)
                            {
                                case "sepia": fx.Sepia.FadeTo(v, 1000f); break;
                                case "trail": fx.ImageTrail.FadeTo(v, 1000f); break;
                                case "fade": fx.Fade.FadeOut(0f); fx.Fade.Alpha = v; break;
                                case "blur": fx.RadialBlur.FadeTo(v, 1000f); fx.RadialBlur.SetBlurStartDist(Arg(3, 0f)); break;
                                case "flash": fx.Flash.Start(0.001f, 10f, 0.001f); break;
                                case "off": fx.ResetAll(); break;
                                default: log.Warn($"Unknown effect '{which}'"); continue;
                            }
                            Step(1);
                            break;
                        // Interaction (step 4.3): buttons and mouse as the input handler gives them to the player.
                        case "interact": player.DoAction(PlayerAction.Interact, true); Step(1); break;
                        case "release": player.DoAction(PlayerAction.Interact, false); Step(1); break;
                        case "throw": player.DoAction(PlayerAction.Attack, true); Step(1); player.DoAction(PlayerAction.Attack, false); break;
                        case "scroll": player.Scroll(arg); Step(1); break;
                        case "rotate": player.RotateHeld = p.Length > 1 && p[1] == "on"; Step(1); break;
                        case "mouse":
                        {
                            // mouse x y t: an HPL2 mouse move (screen units, y down) every step for t seconds.
                            float mx = Arg(1, 0f), my = Arg(2, 0f);
                            int n = Mathf.Max(1, Mathf.RoundToInt(Arg(3, 0.5f) / dt));
                            bool trace = p.Length > 4 && p[4] == "log";
                            for (int i = 0; i < n; i++)
                            {
                                player.AddYaw(-mx);
                                player.AddPitch(-my);
                                Step(1);
                                if (trace && i % 5 == 0) log.Info($"t={time,6:0.00}     {player.States.Current.Describe()}");
                            }
                            break;
                        }
                        case "aim":
                        {
                            // aim Entity [Body]: look at the body (or entity) center.
                            var target = map?.GetEntityByName(p.Length > 1 ? p[1] : "");
                            if (target == null) { log.Warn($"No entity '{raw.Trim()}'"); continue; }
                            var body = p.Length > 2 ? target.Bodies.Find(b => b.Name == target.Name + "_" + p[2]) : (target.Bodies.Count > 0 ? target.Bodies[0] : null);
                            Vector3 goal = body?.Physics != null ? body.Physics.WorldCenterOfMass : (body != null ? body.Transform.position : target.GameObject.transform.position);
                            if (target is RTXMnesia.Enemies.HplEnemy aimedEnemy) goal = aimedEnemy.Body.Position;
                            Vector3 dir = goal - player.LogicCameraPosition;
                            float yaw = Mathf.Atan2(dir.x, dir.z), pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude);
                            player.AddLook(Mathf.DeltaAngle(player.Yaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad,
                                           (pitch - player.Pitch));
                            Step(1);
                            break;
                        }
                        case "body":
                        {
                            // body Entity: positions, velocities and joint angles of its bodies.
                            if (map?.GetEntityByName(p.Length > 1 ? p[1] : "") is not HplProp prop) { log.Warn($"No prop '{raw.Trim()}'"); continue; }
                            var firstLight = prop.GameObject.GetComponentInChildren<RTXMnesia.World.AmnesiaLight>(true);
                            log.Info($"t={time,6:0.00}   prop {prop.Name} {prop.PropType}/{prop.SubType} active {prop.Active} health {prop.Health:0.#}{(prop is RTXMnesia.Props.PropSwingDoor sd && sd.IsBroken ? " BROKEN" : "")} effects {prop.EffectsAlpha:0.00} " +
                                     (prop is RTXMnesia.Props.PropLamp lamp ? $"lit {lamp.Lit} " : "") +
                                     (prop is RTXMnesia.Props.PropSwingDoor door ? $"closed {door.Closed} locked {door.Locked} state {door.GetDoorState()} " : "") +
                                     (firstLight != null ? $"light {firstLight.name} {firstLight.CurrentColor} r {firstLight.CurrentRadius:0.##}" : ""));
                            foreach (var b in prop.PhysicsBodies)
                                log.Info($"t={time,6:0.00}   body {b.Name}: pos {b.Position:F3} vel {b.LinearVelocity.magnitude:0.000} ang {b.AngularVelocity.magnitude:0.000} " +
                                         $"mass {b.Mass:0.##} awake {b.Enabled}");
                            foreach (var j in prop.Joints)
                                log.Info($"t={time,6:0.00}   joint {j.Name} {j.Type}: angle {j.GetAngle() * Mathf.Rad2Deg:0.0}° dist {j.GetDistance():0.000} " +
                                         $"limits {j.MinAngle * Mathf.Rad2Deg:0.#}..{j.MaxAngle * Mathf.Rad2Deg:0.#}°");
                            continue;
                        }
                        case "active":
                        {
                            // active Entity true|false: SetEntityActive for maps whose script is not ported.
                            var entity = map?.GetEntityByName(p.Length > 1 ? p[1] : "");
                            if (entity == null) { log.Warn($"No entity '{raw.Trim()}'"); continue; }
                            entity.SetActive(p.Length < 3 || p[2] == "true");
                            log.Info($"t={time,6:0.00}   {entity.Name} active {entity.Active}");
                            Step(1);
                            break;
                        }
                        case "fitsup":
                        {
                            // fitsup dy: does the standing body fit dy higher, and what does it touch there.
                            Vector3 test = player.Body.Position + new Vector3(0f, Arg(1, 0.1f), 0f);
                            bool fits = player.Body.CheckCharacterFits(test, out Vector3 push, false, 0, HplPhysics.Epsilon);
                            log.Info($"t={time,6:0.00}   fits {fits} at {test:F3} push {push:F4}");
                            player.Body.ForEachCollision(test, 0, (c, n, d) =>
                                log.Info($"t={time,6:0.00}     {c.name} ({c.GetType().Name}, layer {c.gameObject.layer}, {c.transform.root.name}) normal {n:F2} depth {d:0.0000}"));
                            continue;
                        }
                        case "lightsoff":
                        {
                            // lightsoff: hide every light of the scene except the player's darkness light (darkness tests).
                            int count = 0;
                            foreach (var l in RTXMnesia.World.AmnesiaLight.All.ToArray())
                                if (!l.excludeFromLightLevel) { l.SetVisible(false); count++; }
                            log.Info($"t={time,6:0.00}   lightsoff: {count} lights hidden");
                            continue;
                        }
                        case "hide":
                        {
                            // hide lights|passes|volumes|<shader name part>: turn off the lights, custom passes, volumes
                            // or the renderers using a matching shader (bisecting render problems).
                            string what = p.Length > 1 ? p[1] : "";
                            int count = 0;
                            if (what == "lights")
                                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) { l.enabled = false; count++; }
                            else if (what == "passes")
                                foreach (var pass in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.HighDefinition.CustomPassVolume>(FindObjectsInactive.Exclude)) { pass.enabled = false; count++; }
                            else if (what == "volumes")
                                foreach (var vol in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsInactive.Exclude)) { vol.enabled = false; count++; }
                            else
                                foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                                    if (r.enabled && Array.Exists(r.sharedMaterials, m => m != null && m.shader != null &&
                                        m.shader.name.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0)) { r.enabled = false; count++; }
                            log.Info($"t={time,6:0.00}   hide {what}: {count}");
                            continue;
                        }
                        case "sanity":
                        {
                            // sanity [value]: set the sanity, then log the light level, darkness and sanity effects.
                            if (p.Length > 1) player.SetSanity(arg);
                            var dark = player.InDarkness;
                            var ins = map?.effects?.Insanity;
                            log.Info($"t={time,6:0.00}   sanity {player.sanity:0.00} light ext {player.LightLevel.ExtendedLightLevel:0.000} normal {player.LightLevel.NormalLightLevel:0.000} " +
                                     $"| darkness {dark.IsInDarkness} light {(dark.AmbientLight != null ? dark.AmbientLight.CurrentColor.ToString() : "-")} " +
                                     $"| hit {player.SanityHelper.HitAlpha:0.00} drain {player.SanityHelper.SanityDrainVolume:0.00} sway {player.CamDirEffects.SwayAlpha:0.00} " +
                                     $"collapse {player.InsanityCollapseHelper.Active} | insanity {(ins != null ? $"{ins.Active} wave {ins.WaveAlpha:0.00} zoom {ins.ZoomAlpha:0.00}" : "-")}");
                            foreach (var l in RTXMnesia.World.AmnesiaLight.All
                                         .Select(l => (l, v: l.LightLevelAt(player.Body.Position, 3f))).Where(x => x.v > 0f).OrderByDescending(x => x.v).Take(3))
                                log.Info($"t={time,6:0.00}     light {l.l.name}: {l.v:0.000} visible {l.l.Visible} box {l.l.box}");
                            continue;
                        }
                        case "enemy":
                        {
                            // enemy [name]: the enemies' state (state, senses, path, animation), terror and enemy music.
                            if (map == null) { log.Warn("enemy needs an HplMap"); continue; }
                            var music = map.sound?.Music;
                            log.Info($"t={time,6:0.00}   terror {player.Terror:0.00} ({player.TerrorEnemyCount} enemies) avgSpeed {player.AvgSpeed:0.00} | " +
                                     $"music \"{music?.CurrentSongName}\" danger {music?.DangerMusicPlaying} search {music?.EnemyMusicPlaying(RTXMnesia.Enemies.EnemyMusic.Search)} " +
                                     $"attack {music?.EnemyMusicPlaying(RTXMnesia.Enemies.EnemyMusic.Attack)}");
                            foreach (var en in map.Enemies)
                            {
                                if (p.Length > 1 && !string.Equals(en.Name, p[1], StringComparison.OrdinalIgnoreCase)) continue;
                                log.Info($"t={time,6:0.00}   enemy active {en.Active} {en.DebugText()} nodes {en.Pathfinder.NodeContainer?.NodeCount ?? 0}");
                            }
                            continue;
                        }
                        case "insanity":
                        {
                            // insanity [Name|random|stop]: start / stop an insanity event, then the handler's state.
                            if (map == null) { log.Warn("insanity needs an HplMap"); continue; }
                            if (p.Length > 1)
                            {
                                if (p[1] == "random") RTXMnesia.Effects.HplInsanity.StartEvent(map);
                                else if (p[1] == "stop") RTXMnesia.Effects.HplInsanity.StopCurrentEvent();
                                else RTXMnesia.Effects.HplInsanity.StartEvent(map, p[1]);
                            }
                            var ev = RTXMnesia.Effects.HplInsanity.CurrentEvent;
                            string detail = ev switch
                            {
                                RTXMnesia.Effects.InsanityEventBugs b => $"bugs {b.Bugs.Count} alpha {(b.Bugs.Count > 0 ? b.Bugs[0].alpha : 0f):0.00} pos {(b.Bugs.Count > 0 ? b.Bugs[0].pos : Vector2.zero):F0}",
                                RTXMnesia.Effects.InsanityEventSoundStream st => $"fade {st.FadeAlpha:0.00} color {st.FadeColor} subtitle \"{st.Subtitle}\"",
                                _ => "",
                            };
                            log.Info($"t={time,6:0.00}   {RTXMnesia.Effects.HplInsanity.Describe()} possible {RTXMnesia.Effects.HplInsanity.NewEventIsPossible(map)} sanity {player.sanity:0.0} {detail}");
                            continue;
                        }
                        case "health":
                        {
                            // health: health, the hurt effect, HUD splashes / flash, death and the check point.
                            var death = player.Death;
                            var hudFx = player.HudEffect;
                            log.Info($"t={time,6:0.00}   health {player.health:0.00} sanity {player.sanity:0.00} dead {player.IsDead} pos {player.Body.Position:F2} " +
                                     $"roll {player.Roll * Mathf.Rad2Deg:0.0} pitch {player.Pitch * Mathf.Rad2Deg:0.0} yaw {player.Yaw * Mathf.Rad2Deg:0.0} | " +
                                     $"hurt {player.Hurt.Alpha:0.00} noise {player.Hurt.CurrentNoise} move× {player.MoveSpeedMul:0.00} head {player.GetHeadPosAdd(RTXMnesia.Player.HeadPosAdd.Hurt).y:0.000} | " +
                                     $"splashes {hudFx.Splashes.Count} flash {(hudFx.FlashActive ? hudFx.FlashAlpha.ToString("0.00") : "-")} | " +
                                     $"death {(death.Active ? $"state {death.State} fade {death.FadeAlpha:0.00} text {death.TextAlpha:0.00} \"{death.CurrentHintText}\" deathY {player.GetHeadPosAdd(RTXMnesia.Player.HeadPosAdd.Death).y:0.00}" : "-")} " +
                                     $"hint {death.HintCat}/{death.HintEntry} | check point {map?.CheckPointName} at {map?.CheckPointStartPos} count {map?.CheckPointCount}");
                            continue;
                        }
                        case "sounds":
                        {
                            // sounds: the playing sound entries, sound entities and music.
                            var snd = map?.sound;
                            if (snd == null) { log.Warn("sounds needs an HplMap with a sound handler"); continue; }
                            log.Info($"t={time,6:0.00}   music \"{snd.Music.CurrentSongName}\" vol {snd.Music.CurrentSongVolume:0.00} | " +
                                     $"global world vol {snd.GetGlobalVolume(RTXMnesia.Sound.SoundEntryType.World):0.00} speed {snd.GetGlobalSpeed(RTXMnesia.Sound.SoundEntryType.World):0.00}");
                            foreach (var e in snd.Entries)
                                log.Info($"t={time,6:0.00}   entry {e.Name} {e.Type}{(e.Is3D && !e.Relative ? $" 3D {Vector3.Distance(e.Position, snd.ListenerPosition):0.0} m ({e.MinDistance:0.#}..{e.MaxDistance:0.#})" : "")} " +
                                         $"loop {e.Loop} t {e.ElapsedTime:0.00}/{e.Clip.length:0.00} mul {e.VolumeMul:0.00}");
                            foreach (var s in snd.SoundEntities)
                                log.Info($"t={time,6:0.00}   entity {s.Name} ({s.Data.name}) active {s.Active} stopped {s.IsStopped()} " +
                                         $"at {s.WorldPosition:F1} {Vector3.Distance(s.WorldPosition, snd.ListenerPosition):0.0} m");
                            continue;
                        }
                        case "ropes":
                        {
                            // ropes: the map's ropes (length, particles, motor, ends) and what the sticky areas hold.
                            if (map == null) { log.Warn("ropes needs an HplMap"); continue; }
                            foreach (var rope in map.Ropes)
                            {
                                var r = rope.Physics;
                                if (r == null) { log.Info($"t={time,6:0.00}   rope {rope.Name}: not created"); continue; }
                                log.Info($"t={time,6:0.00}   rope {rope.Name}: length {r.TotalLength:0.00} ({r.MinTotalLength:0.00}..{r.MaxTotalLength:0.00}) " +
                                         $"particles {r.Particles.Count} {(r.IsSleeping ? "sleeping " : "")}motor {(r.MotorActive ? $"→ {r.MotorWantedLength:0.00}" : "off")} " +
                                         $"auto {(r.AutoMoveActive ? $"{r.AutoMoveSpeed:0.00} m/s" : "off")} start {r.AttachedStartBody?.Name ?? "-"} end {r.AttachedEndBody?.Name ?? "-"}");
                            }
                            foreach (var e in map.Entities)
                                if (e is RTXMnesia.ScriptApi.HplArea { Sticky: { } sticky })
                                    log.Info($"t={time,6:0.00}   sticky {e.Name}{(e.Active ? "" : " (off)")}: {sticky.AttachedBody?.Name ?? "-"}{(sticky.AttachedBody != null && !sticky.CanDetach ? " (posing / fixed)" : "")}");
                            Step(1);
                            break;
                        }
                        case "particles":
                        {
                            // particles: the running particle systems, their particles per emitter and state.
                            var world = map?.ParticleWorld;
                            if (world == null) { log.Warn("particles needs an HplMap"); continue; }
                            Vector3 eye = player.playerCamera != null ? player.playerCamera.transform.position : player.transform.position;
                            log.Info($"t={time,6:0.00}   {world.Systems.Count} particle systems");
                            foreach (var ps in world.Systems)
                            {
                                var counts = new List<string>();
                                foreach (var e in ps.Emitters) counts.Add($"{e.Data.name}:{e.ParticleCount}");
                                log.Info($"t={time,6:0.00}   ps {ps.Name} ({ps.DataName}) {string.Join(" ", counts)} " +
                                         $"{(ps.Active ? "" : "inactive ")}{(ps.Visible ? "" : "hidden ")}{(ps.IsDying ? "dying " : "")}" +
                                         $"at {ps.UnityWorldPosition:F1} {Vector3.Distance(ps.UnityWorldPosition, eye):0.0} m");
                            }
                            continue;
                        }
                        case "overlaps":
                        {
                            // overlaps Entity: penetrations of its body colliders with each other and with the world.
                            if (map?.GetEntityByName(p.Length > 1 ? p[1] : "") is not HplProp prop) { log.Warn($"No prop '{raw.Trim()}'"); continue; }
                            var own = new HashSet<Collider>();
                            foreach (var b in prop.PhysicsBodies) foreach (var c in b.Colliders) if (c != null) own.Add(c);
                            var buffer = new Collider[64];
                            foreach (var b in prop.PhysicsBodies)
                                foreach (var c in b.Colliders)
                                {
                                    if (c == null) continue;
                                    var bounds = c.bounds;
                                    int n = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents + Vector3.one * 0.01f, buffer, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                                    for (int i = 0; i < n; i++)
                                    {
                                        var o = buffer[i];
                                        if (o == c || map.PhysicsWorld.BodyOf(o) == b) continue;
                                        if (Physics.ComputePenetration(c, c.transform.position, c.transform.rotation, o, o.transform.position, o.transform.rotation, out Vector3 dir, out float depth))
                                            log.Info($"t={time,6:0.00}   overlap {b.Name}/{c.name} ↔ {(own.Contains(o) ? "own " : "")}{o.transform.parent?.name}/{o.name} " +
                                                     $"layer {o.gameObject.layer}: depth {depth:0.000} dir {dir:F2}");
                                    }
                                }
                            continue;
                        }
                        case "call":
                        {
                            // call Function args...: a level script function or engine function of HplScript by name.
                            if (map?.Script == null) { log.Warn("call needs a level script"); continue; }
                            var method = map.Script.GetType().GetMethod(p.Length > 1 ? p[1] : "", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (method == null) { log.Warn($"No function '{raw.Trim()}'"); continue; }
                            var pars = method.GetParameters();
                            var args = new object[pars.Length];
                            for (int i = 0; i < pars.Length; i++)
                                args[i] = Convert.ChangeType(p.Length > i + 2 ? p[i + 2] : "", pars[i].ParameterType, CultureInfo.InvariantCulture);
                            var result = method.Invoke(map.Script, args);
                            log.Info($"t={time,6:0.00}   call {raw.Trim()} → {result}");
                            Step(1);
                            break;
                        }
                        case "shot":
                            if (shots)
                            {
                                string file = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", $"Player_{mapName}_{shot++:00}_{(p.Length > 1 ? p[1] : "shot")}.png");
                                // Canvases are not drawn by camera renders outside the player loop: the HUD text is
                                // blended into the snapshot (HudSnapshot).
                                var hudForShot = map != null ? map.hud : null;
                                if (hudForShot != null)
                                {
                                    hudForShot.Refresh();
                                    log.Info($"t={time,6:0.00}   hud {hudForShot.DescribeText()}");
                                }
                                ImportMenu.RenderToFile(player.playerCamera, file, image => HudSnapshot.Draw(image, hudForShot));
                            }
                            break;
                        default: log.Warn($"Unknown command '{raw.Trim()}'"); continue;
                    }
                    State(raw.Trim());
                    if (map != null) ScriptState(log, map, player, time);
                }
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
                Physics.simulationMode = simulationMode;
            }
            log.Save($"Player simulation: {mapName}");
        }

        /// <summary>What the level script controls: player state, camera fades, screen effects, texts, timers.</summary>
        static void ScriptState(ImportLog log, HplMap map, AmnesiaPlayer player, float time)
        {
            if (map == null) return;
            var e = map.effects;
            var hud = map.hud;
            string voice = e.PlayVoice.Active ? e.PlayVoice.CurrentVoice : "-";
            string Short(string s) => string.IsNullOrEmpty(s) ? "-" : (s.Length > 40 ? s.Substring(0, 40) + "…" : s).Replace('\n', ' ');
            log.Info($"t={time,6:0.00}   script: active {player.active} feet {player.Body.FeetPosition:F2} yaw {player.Yaw * Mathf.Rad2Deg:0} " +
                     $"pitch {player.Pitch * Mathf.Rad2Deg:0} roll {player.Roll * Mathf.Rad2Deg:0} fov×{player.FovMul:0.00} " +
                     $"aspect×{player.AspectMul:0.00} move×{player.MoveSpeedMul:0.00} look×{player.lookSpeedMul:0.00} lookAt {player.LookAt.Active} | " +
                     $"fade {e.Fade.Alpha:0.00} trail {e.ImageTrail.Amount:0.00} sepia {e.Sepia.Amount:0.00} blur {e.RadialBlur.Size:0.00} " +
                     $"flash {e.Flash.Alpha:0.00} | voice {voice} \"{Short(e.PlayVoice.Active ? e.PlayVoice.CurrentText : "")}\" | " +
                     $"msg \"{Short(hud.Messages.Alpha > 0f ? hud.Messages.Text : "")}\" hint \"{Short(hud.Hints.Alpha > 0f ? hud.Hints.Text : "")}\" " +
                     $"quest {hud.Messages.QuestAlpha:0.00} | timers {map.TimerCount} sanity {player.sanity:0} terror {player.Terror:0.00} | {EnemyLine(map)}snd {map.sound?.Entries.Count ?? 0} music \"{map.sound?.Music?.CurrentSongName}\" | ps {map.ParticleWorld?.Systems.Count ?? 0} outline {map.effects?.Renderer.OutlineObjects.Count ?? 0} flash {map.effects?.Renderer.FlashObjects.Count ?? 0}");
        }

        static string EnemyLine(HplMap map)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var en in map.Enemies)
                if (en.Active) sb.Append($"{en.Name} {en.CurrentStateName} at {en.Body.FeetPosition:F1} d {Vector3.Distance(en.Body.FeetPosition, map.player.Body.FeetPosition):0.0} {(en.CanSeePlayer ? "sees " : "")}| ");
            return sb.ToString();
        }

        static void Call(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
