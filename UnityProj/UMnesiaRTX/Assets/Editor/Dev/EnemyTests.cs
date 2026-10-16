using System;
using System.IO;
using System.Linq;
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
    /// The grunt on the imported 10_daniels_room (6.2 / 6.3), edit mode with the map stepping like PlayerSimulation and
    /// no level script: the node container from the shipped .nodes file, A*, animations, activation and danger music,
    /// patrol, hearing, sight → alert → hunt → attack, terror and attack music, player death, the check point reset,
    /// the map cache and breaking a door. Batch: -batchmode -nographics -quit -executeMethod
    /// RTXMnesia.EditorDev.EnemyTests.RunBatch. Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class EnemyTests
    {
        const string MapFile = "maps/main/ch01/10_daniels_room.map";
        const string Grunt = "grunt_normal_1";

        [MenuItem("Tools/RTXMnesia/Tests/Enemies (10_daniels_room)")]
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
                // ---------------------------------------------------------- data, patrol, hearing, hunt, death
                var (map, player) = Open();
                var grunt = map.GetEntityByName(Grunt) as EnemyGrunt;
                Check(grunt != null, "grunt_normal_1 is a ported grunt");
                Check(!grunt.Active, "the grunt starts inactive (map Active=false)");
                var nodes = grunt.Pathfinder.NodeContainer;
                Check(nodes != null && nodes.NodeCount == 30, "node container of servant_grunt with the map's 30 path nodes");
                var n2 = nodes.GetNodeFromName("PathNodeArea_2");
                Check(n2 != null && n2.EdgeCount > 0, "edges loaded from 10_daniels_room_servant_grunt.nodes");
                Check(grunt.Animation != null && grunt.Animation.States.Count == 13, "13 grunt animations");
                bool found = grunt.Pathfinder.MoveTo(nodes.GetNodeFromName("PathNodeArea_13").Position);
                Check(found && grunt.Pathfinder.PathNodeCount > 0,
                      $"A* finds a node path into the next room ({grunt.Pathfinder.PathNodeCount} nodes: {string.Join(" ", grunt.Pathfinder.PathNodes.Select(n => n.Name))})");
                grunt.Pathfinder.Stop();
                Check(map.Script != null, "the level script object exists (not run)");

                // Player out of sight behind the grunt, standing still in the dark.
                player.PlaceAt(new Vector3(-10.51f, 0.05f, 7.54f), 0f);
                Step(map, 1f); // landed (the grunt would hear the landing step)
                grunt.SetActive(true);
                Step(map, 0.5f);
                Check(grunt.CurrentState == EnemyState.Wait && grunt.PlayerInRange, $"activated: player in range, waiting (no patrol nodes): {grunt.CurrentStateName} range {grunt.PlayerInRange}");
                Check(grunt.CurrentAnimationName != "", $"an animation plays ({grunt.CurrentAnimationName})");
                Vector3 feet = grunt.Body.FeetPosition, model = grunt.Model.transform.position;
                Check(new Vector2(feet.x - model.x, feet.z - model.z).magnitude < 0.05f && Mathf.Abs(model.y - feet.y) < 0.3f,
                      "the mesh follows the character body");
                Step(map, 2.5f);
                Check(map.sound.Music.DangerMusicPlaying && map.sound.Music.CurrentSongName == "dan_grunt.ogg", "danger music after 2 s in range");

                CallScript(map, "AddEnemyPatrolNode", Grunt, "PathNodeArea_1", 0f, "");
                Check(grunt.Patrol.Count == 1, "AddEnemyPatrolNode adds the node");
                Check(CallScript(map, "GetEnemyStateName", Grunt) as string == grunt.CurrentStateName, "GetEnemyStateName");

                // Hearing: a loud enemy-hearable sound entity next to the grunt.
                var assets = HplEnemyAssets.Instance;
                Check(assets != null && assets.hearableSounds.Contains("impact_"), "EnemySounds.dat imported");
                var loud = map.sound.bank.soundEntities.FirstOrDefault(d => d.name.StartsWith("impact_") && d.volume > 0.6f && d.maxDistance > 8f && d.HasSound(Sound.SoundEntityType.Main));
                Check(loud != null, "a loud impact sound entity exists");
                var s = map.sound.CreateSoundEntity("TestImpact", loud.name, true);
                s.Position = grunt.Body.Position + new Vector3(3f, 0f, 0f);
                Step(map, 0.2f);
                Check(grunt.CurrentState == EnemyState.Investigate, $"hearing '{loud.name}' (volume {loud.volume}) → Investigate");

                // Walking toward it: seen, alert, hunt, hit.
                player.held.forward = true;
                float t = 0f;
                while (t < 1.5f && grunt.CurrentState != EnemyState.Alert && grunt.CurrentState != EnemyState.Hunt) { Step(map, 1f / 60f); t += 1f / 60f; }
                player.held.forward = false;
                Check(grunt.CanSeePlayer && (grunt.CurrentState == EnemyState.Alert || grunt.CurrentState == EnemyState.Hunt), "the walking player is seen: Alert");
                Step(map, 0.3f);
                Check(player.Terror > 0f && player.TerrorEnemyCount == 1, "terror rises");
                t = 0f;
                while (t < 6f && player.health >= 100f) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(player.health < 100f && player.health > 100f - 61f, $"a claw hit damages the player (health {player.health:0})");
                Check(map.sound.Music.EnemyMusicPlaying(EnemyMusic.Attack) && map.sound.Music.CurrentSongName == "att_grunt.ogg", "attack music");
                Check(grunt.CurrentState == EnemyState.HuntPause || grunt.CurrentState == EnemyState.AttackMeleeShort, $"after a hit: hunt pause ({grunt.CurrentStateName})");

                t = 0f;
                while (t < 10f && !player.IsDead) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(player.IsDead, "the second attack kills the player");
                Step(map, 0.5f);
                Check(grunt.CurrentState == EnemyState.Idle, "PlayerDead: the grunt goes idle");

                map.LoadCheckPoint();
                Check(!grunt.Active && grunt.CurrentState != EnemyState.Hunt && Vector3.Distance(grunt.Body.FeetPosition, grunt.StartPosition) < 0.01f,
                      "check point: the grunt is off and back at its start");
                Check(player.TerrorEnemyCount == 0, "check point: terror enemies cleared");

                // Map cache: enemies come back inactive with their patrol.
                grunt.SetActive(true);
                HplSavedMaps.SaveMap(map);
                grunt.ClearPatrolNodes();
                HplSavedMaps.LoadMap(map);
                Check(!grunt.Active && grunt.Patrol.Count == 1 && grunt.Patrol[0].node.Name == "PathNodeArea_1", "map cache: inactive, patrol kept");
                HplSavedMaps.Reset();

                // ---------------------------------------------------------- breaking a door
                (map, player) = Open();
                grunt = (EnemyGrunt)map.GetEntityByName(Grunt);
                var door = (PropSwingDoor)map.GetEntityByName("mansion_1");
                Check(door.Closed && !door.IsBroken, "mansion_1 is closed");
                player.PlaceAt(new Vector3(1.13f, 0.05f, 8.14f), 90f * Mathf.Deg2Rad);
                Step(map, 1f);
                grunt.SetActive(true);
                CallScript(map, "ShowEnemyPlayerPosition", Grunt);
                Step(map, 0.1f);
                Check(grunt.CurrentState == EnemyState.Hunt, "ShowEnemyPlayerPosition → Hunt");
                t = 0f;
                bool brokeDoorState = false;
                while (t < 15f && !door.IsBroken)
                {
                    Step(map, 1f / 60f);
                    t += 1f / 60f;
                    brokeDoorState |= grunt.CurrentState == EnemyState.BreakDoor;
                }
                Check(brokeDoorState, "the hunting grunt gets stuck at the door and breaks it (BreakDoor)");
                Check(door.IsBroken && door.Health <= 0f, $"the door breaks after {t:0.0} s");
                t = 0f;
                while (t < 6f && grunt.Body.Position.x < -1.5f) { Step(map, 1f / 60f); t += 1f / 60f; }
                Check(grunt.Body.Position.x > -1.5f, "the grunt goes through the doorway");

                log.Info($"PASS: {checks} enemy checks (nodes, A*, animations, activation, danger music, patrol, hearing, " +
                         "sight, alert, hunt, attack, terror, attack music, death, check point, map cache, door breaking)");
                log.Save("Enemy tests");
                Debug.Log($"PASS: {checks} enemy checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Enemy tests");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                // Restored before any exit: the project settings must not keep the script simulation mode.
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
