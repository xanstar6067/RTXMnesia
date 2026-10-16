using System;
using System.IO;
using System.Linq;
using RTXMnesia.Dynamics;
using RTXMnesia.Enemies;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The water lurker and liquid areas (6.4) on the imported 07_archives_cellar, edit mode with the map stepping like
    /// PlayerSimulation and no level script: the submerged volume, the liquid area (player in water: speed, step type;
    /// buoyancy of bodies), the lurker (hidden mesh, nodes, hunt while the player is in the water, attack and pause,
    /// attack music, sanity loss only while hunting, going home when the player leaves the water) and food (a corpse
    /// arm in the water draws it: search, eat). Batch: -batchmode -nographics -quit -executeMethod
    /// RTXMnesia.EditorDev.WaterLurkerTests.RunBatch. Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class WaterLurkerTests
    {
        const string MapFile = "maps/main/ch01/07_archives_cellar.map";
        const string Lurker = "waterlurker_1";

        [MenuItem("Tools/RTXMnesia/Tests/Water lurker (07_archives_cellar)")]
        public static void RunBatch()
        {
            var log = new ImportLog();
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++;
                log.Info("ok: " + message);
            }
            var simulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            bool failed = false;
            try
            {
                // ---------------------------------------------------------- submerged volume
                var probe = new GameObject("BuoyancyProbe") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    probe.transform.position = new Vector3(0f, 1000f, 0f);
                    var box = probe.AddComponent<BoxCollider>();
                    Check(HplBuoyancy.SubmergedVolume(new Collider[] { box }, 1000.25f, out float v, out Vector3 c) &&
                          Mathf.Abs(v - 0.75f) < 1e-3f && Mathf.Abs(c.y - (1000f - 0.125f)) < 1e-3f,
                          $"a 1 m box 0.75 m under the surface: volume {v:0.000}, centroid y {c.y - 1000f:0.000}");
                    probe.transform.rotation = Quaternion.Euler(30f, 20f, 10f);
                    HplBuoyancy.ClearCache();
                    Check(HplBuoyancy.SubmergedVolume(new Collider[] { box }, 1010f, out v, out _) && Mathf.Abs(v - 1f) < 1e-3f,
                          $"a turned box fully under: volume {v:0.000}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(probe);
                    HplBuoyancy.ClearCache();
                }

                // ---------------------------------------------------------- liquid area, player in water
                var (map, player) = Open();
                var liquidArea = map.Entities.OfType<HplArea>().FirstOrDefault(a => a.Liquid != null);
                Check(liquidArea != null && liquidArea.Name == "LiquidArea_1", "LiquidArea_1 is a liquid area");
                var liquid = liquidArea.Liquid;
                Check(Mathf.Abs(liquid.SurfaceY - (-3.675f + 0.65f / 2f)) < 1e-3f && liquid.Material != null && liquid.Material.stepType == "water",
                      $"surface at the top of the box ({liquid.SurfaceY:0.000}), material Water");

                map.PlacePlayerAtStartPos("PlayerStartArea_3");
                Step(map, 0.5f);
                Check(player.IsInWater && player.WaterStepSound == "water" && Mathf.Abs(player.WaterSpeedMul - 0.8f) < 1e-4f,
                      "the player at PlayerStartArea_3 is in the water: step type water, speed × 0.8");
                Check(map.sound.bank.soundEntities.Any(d => d.name == "step_walk_water"), "step_walk_water exists for the water steps");

                // Buoyancy: a floating body (BuoyancyDensityMul > 0) in the water.
                var floating = map.PhysicsWorld.Bodies.FirstOrDefault(b => b.BuoyancyActive);
                Check(floating != null, $"a body in the water has buoyancy ({floating?.Name})");

                // The broad phase: Newton's AABBs are padded by 1/8 m, so a body just above the area is still iterated
                // (its flag clears); farther it is not (the flag stays, as in HPL2).
                bool haveBounds = HplCollision.TryGetBounds(liquidArea.GetBodyColliders(0), out Bounds area);
                haveBounds &= floating.TryGetBounds(out Bounds fb);
                Check(haveBounds,
                      "liquid area and body bounds");
                Vector3 floatPos = floating.Position;
                foreach (var (gap, expected) in new[] { (0.1f, false), (0.4f, true) })
                {
                    floating.SetPose(floatPos + new Vector3(0f, area.max.y + gap - fb.min.y, 0f), floating.Rotation);
                    Physics.SyncTransforms();
                    floating.BuoyancyActive = true;
                    liquid.Update(map, Time.fixedDeltaTime);
                    Check(floating.BuoyancyActive == expected,
                          $"a body {gap:0.0} m above the area: buoyancy {(expected ? "left on (outside the padded broad phase)" : "cleared (inside the padded broad phase)")}");
                }
                floating.SetPose(floatPos, floating.Rotation);

                // ---------------------------------------------------------- the lurker
                var lurker = map.GetEntityByName(Lurker) as EnemyWaterLurker;
                Check(lurker != null && !lurker.Active, "waterlurker_1 is a ported, inactive water lurker");
                Check(lurker.GameObject.GetComponentsInChildren<Renderer>(true).All(r => !r.enabled), "its mesh is hidden");
                Check(lurker.Animation == null && lurker.Model == null, "no animations (mbUseAnimations = false)");
                var nodes = lurker.Pathfinder.NodeContainer;
                Check(nodes != null && nodes.NodeCount > 0 && nodes.GetNodeFromName("PathNodeArea_1")?.EdgeCount == 6,
                      $"node container with edges from 07_archives_cellar_waterlurker.nodes ({nodes?.NodeCount} nodes)");
                Check(!lurker.CausesSanityDecrease, "no sanity loss by default");

                lurker.SetActive(true);
                Step(map, 0.2f);
                Check(lurker.CurrentState == EnemyState.Hunt && lurker.PlayerDetected, $"the player in the water is detected: Hunt ({lurker.CurrentStateName})");
                Check(lurker.CausesSanityDecrease, "hunting: sanity loss on");
                Check(map.sound.Music.EnemyMusicPlaying(EnemyMusic.Attack) && map.sound.Music.CurrentSongName == "att_guardian.ogg", "attack music att_guardian.ogg");

                float t = 0f;
                while (t < 15f && player.health >= 100f) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(player.health < 100f && player.health >= 100f - 46f, $"the lurker reaches the player and bites (health {player.health:0}, {t:0.0} s)");
                Step(map, 0.6f);
                Check(lurker.CurrentState == EnemyState.HuntPause, $"after the attack: hunt pause ({lurker.CurrentStateName})");

                // Out of the water (PlayerStartArea_x4 is 1 m higher): no longer detected, going home.
                map.PlacePlayerAtStartPos("PlayerStartArea_x4");
                Step(map, 0.5f);
                Check(!player.IsInWater, "PlayerStartArea_x4 is dry");
                t = 0f;
                while (t < 4f && lurker.CurrentState != EnemyState.GoHome) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(lurker.CurrentState == EnemyState.GoHome && !lurker.CausesSanityDecrease, $"the player left the water: GoHome, no sanity loss ({lurker.CurrentStateName})");
                // ---------------------------------------------------------- food
                (map, player) = Open();
                lurker = (EnemyWaterLurker)map.GetEntityByName(Lurker);
                map.PlacePlayerAtStartPos("PlayerStartArea_x4");
                CallScript(map, "CreateEntityAtArea", "FoodTest", "/organic/corpse_male/corpse_male_arm.ent", "AreaArm_1", false);
                var food = map.GetEntityByName("FoodTest") as PropObject;
                Check(food != null && food.IsFood, "a corpse arm (IsFood) is created");
                Step(map, 0.2f);
                lurker.SetActive(true);
                Step(map, 0.5f);
                Check(lurker.CurrentState == EnemyState.Idle, $"the player is out of the water: Idle ({lurker.CurrentStateName})");
                // Into the water next to a path node 2-5 m from the lurker (the lurker needs a node within 1.5 m).
                var container = lurker.Pathfinder.NodeContainer;
                AINode foodNode = null;
                for (int i = 0; i < container.NodeCount && foodNode == null; i++)
                {
                    var n = container.GetNode(i);
                    float d = Vector3.Distance(n.Position, lurker.Body.FeetPosition);
                    if (d > 2f && d < 5f) foodNode = n;
                }
                Check(foodNode != null, $"a node near the lurker for the food ({foodNode?.Name})");
                Vector3 drop = foodNode.Position + new Vector3(0.3f, 0.3f, 0f);
                food.PhysicsBodies[0].SetPose(drop, Quaternion.identity);
                t = 0f;
                bool searched = false;
                float startHealth = food.Health;
                while (t < 10f && (food.Health >= startHealth) && !food.DestroyMe)
                {
                    Step(map, 1f / 60f);
                    t += 1f / 60f;
                    searched |= lurker.CurrentState == EnemyState.Search;
                }
                Check(searched, "the food in the water draws the lurker: Search");
                Check(lurker.CurrentState == EnemyState.Eat && food.Health < startHealth, $"it eats (food health {food.Health:0}, {t:0.0} s)");
                t = 0f;
                while (t < 20f && map.GetEntityByName("FoodTest") != null) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(map.GetEntityByName("FoodTest") == null, $"the arm is eaten up and breaks ({t:0.0} s more)");
                Step(map, 1.5f);
                Check(lurker.CurrentState != EnemyState.Eat && lurker.CurrentState != EnemyState.Search,
                      $"no more food: it leaves ({lurker.CurrentStateName})");

                log.Info($"PASS: {checks} water lurker checks (submerged volume, liquid area, player in water, buoyancy, " +
                         "lurker data, hunt, attack, attack music, sanity, going home, food eaten up)");
                log.Save("Water lurker tests");
                Debug.Log($"PASS: {checks} water lurker checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Water lurker tests");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                HplGame.Reset();
            }
            if (failed && Application.isBatchMode) EditorApplication.Exit(1);
        }

        static (HplMap map, AmnesiaPlayer player) Open()
        {
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, MapFile));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HplMath.Seed(1);
            HplGame.Reset();
            var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
            Physics.SyncTransforms();
            player.readDevices = false;
            Invoke(player, "Awake");
            Invoke(player, "Start");
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            map.runScript = false;
            map.Build();
            map.Begin();
            return (map, player);
        }

        static void Step(HplMap map, float seconds)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(seconds / Time.fixedDeltaTime));
            for (int i = 0; i < steps; i++) map.Step(Time.fixedDeltaTime);
        }

        static object CallScript(HplMap map, string function, params object[] args)
        {
            var method = map.Script.GetType().GetMethod(function, System.Reflection.BindingFlags.Instance |
                                                                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            return method.Invoke(map.Script, args);
        }

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
