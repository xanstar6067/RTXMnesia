using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RTXMnesia.Dev;
using RTXMnesia.Effects;
using RTXMnesia.Enemies;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    public static class DevRoomTests
    {
        [MenuItem("Tools/RTXMnesia/Tests/Dev room")]
        public static void RunBatch()
        {
            var log = new ImportLog();
            int checks = 0, clips = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++;
                log.Info("ok: " + message);
            }
            var previousSimulation = Physics.simulationMode;
            string previousFolder = HplSaveHandler.FolderOverride;
            bool failed = false;
            try
            {
                if (Environment.GetCommandLineArgs().Contains("-rebuildDevRoom")) DevRoomBuilder.RunBatch();
                EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
                Physics.simulationMode = SimulationMode.Script;
                HplMath.Seed(1);
                var player = Object.FindAnyObjectByType<AmnesiaPlayer>();
                player.readDevices = false;
                Invoke(player, "Awake");
                Invoke(player, "Start");
                var map = Object.FindAnyObjectByType<HplMap>();
                var room = map.GetComponent<HplDevRoom>();
                map.Build();
                map.Begin();
                Check(map.disableAutoSaves && map.disableInsanity && map.disableEnemyAI && !map.runScript,
                    "dev_room disables auto saves, insanity, AI and story scripts");
                Check(room.stations.Count == DevRoomBuilder.FindSpecimens().Count && room.stations.All(s => s.entity != null),
                    $"all {room.stations.Count} catalogue specimens loaded as runtime entities (missing: {string.Join(", ", room.stations.Where(s => s.entity == null).Select(s => s.entityName))})");
                Check(EditorBuildSettings.scenes.Any(s => s.path == DevRoomBuilder.ScenePath && s.enabled), "dev_room is in build scenes");
                Check(room.stations.Any(s => s.sourceFile == "models/player/hands/hands.ent" && s.animation?.States.Count == 3), "player hands and their 3 clips");
                Check(room.stations.Where(s => s.sourceFile.Contains("enemy_suitor")).Count(s => s.animation?.States.Count > 0) == 4,
                    "all 4 Justine suitor variants have clips");
                var assets = HplEnemyAssets.Instance;
                Check(assets.GetEnemy("entities/critter/spider/spider.ent").sourceFile != assets.GetEnemy("entities/insan_visions/spider/spider.ent").sourceFile,
                    "different spider.ent paths resolve to their own animations");
                Check(room.stations.Where(s => s.sourceFile.Contains("slime_anim") || s.sourceFile.Contains("spinning_cogwheel"))
                    .All(s => s.animation?.States.Count > 0), "embedded slime and cogwheel animations are available");

                for (int i = 0; i < room.stations.Count; i++)
                {
                    room.Select(i);
                    var station = room.Selected;
                    if (station.animation == null) continue;
                    for (int j = 0; j < station.animation.States.Count; j++)
                    {
                        room.PlayClip(j);
                        var state = room.CurrentClip;
                        state.SetTimePosition(state.Length * 0.4f);
                        station.animation.Update(0f, null);
                        Check(state.IsActive && state.Length > 0f && state.TimePosition > 0f,
                            station.entityName + ": " + state.Name + " can be selected and sampled");
                        foreach (var t in station.entity.GameObject.GetComponentsInChildren<Transform>(true))
                            if (!float.IsFinite(t.localPosition.sqrMagnitude) || !float.IsFinite(t.localRotation.x))
                                throw new InvalidOperationException(station.entityName + ": nonfinite pose in " + state.Name);
                        clips++;
                    }
                    room.PlayClip(0);
                }
                int gruntIndex = room.stations.FindIndex(s => s.sourceFile.EndsWith("servant_grunt.ent"));
                room.Select(gruntIndex);
                var grunt = (HplEnemy)room.Selected.entity;
                var bones = grunt.Model.GetComponentsInChildren<SkinnedMeshRenderer>()[0].bones.Where(b => b != null).ToArray();
                room.PlayClip(room.Selected.animation.IndexOf("Walk"));
                var before = bones.Select(b => b.localRotation).ToArray();
                Step(map, 0.3f);
                Check(bones.Select((b, i) => Quaternion.Angle(before[i], b.localRotation)).Max() > 0.1f,
                    "grunt skeleton actually changes pose with AI disabled");
                var positions = map.Enemies.Select(e => e.Body.FeetPosition).ToArray();
                room.GoToSelected();
                float health = player.health;
                room.PlayClip(room.Selected.animation.IndexOf("SwingClaws01"));
                player.SetSanity(0f);
                player.GiveSanityDamage(200f);
                player.LowerSanity(200f, true);
                HplInsanity.StartEvent(map, "Bugs_1");
                Step(map, 5f);
                Check(player.sanity == 100f && !map.effects.Insanity.Active && HplInsanity.CurrentEventIndex == -1 && !HplInsanity.NewEventIsPossible(map),
                    "sanity damage and insanity events/visuals stay disabled");
                Check(map.Enemies.All(e => e.Disabled && !e.PlayerDetected && !e.CanSeePlayer) && Mathf.Abs(player.health - health) < 0.001f,
                    "nearby enemies do not perceive or damage the player while attack clips run");
                Check(map.Enemies.Select((e, i) => Vector2.Distance(new Vector2(e.Body.FeetPosition.x, e.Body.FeetPosition.z),
                    new Vector2(positions[i].x, positions[i].z))).All(d => d < 0.01f), "enemy bodies do not walk away from their stands");
                var paused = room.CurrentClip;
                paused.Paused = true;
                float pausedTime = paused.TimePosition;
                Step(map, 0.5f);
                Check(Mathf.Abs(pausedTime - paused.TimePosition) < 0.0001f, "paused animation time stays fixed");

                HplSaveHandler.FolderOverride = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "DevRoomSaveGuard");
                string folder = HplSaveHandler.Folder(map.Campaign);
                Directory.CreateDirectory(folder);
                string sentinel = Path.Combine(folder, "dev_room_existing_save.sav");
                File.WriteAllText(sentinel, "existing save must survive");
                int count = Directory.GetFiles(folder).Length;
                Check(HplSaveHandler.AutoSave(map) == null && Directory.GetFiles(folder).Length == count && File.ReadAllText(sentinel) == "existing save must survive",
                    "AutoSave returns before creating saves or deleting existing files (also used by menu exit)");

                EditorSceneManager.OpenScene(HplGameUiPath(), OpenSceneMode.Single);
                var normal = Object.FindAnyObjectByType<HplMap>();
                Check(!normal.disableAutoSaves && !normal.disableInsanity && !normal.disableEnemyAI,
                    "normal map keeps auto saves, sanity and enemy AI enabled");
                EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
                log.Info($"PASS: {checks} checks, {clips} animation clips; catalogue, poses, AI, health, sanity, pause, save guard, normal map defaults");
            }
            catch (Exception exception)
            {
                failed = true;
                log.Error($"FAIL after {checks} checks: {exception}");
            }
            finally
            {
                Physics.simulationMode = previousSimulation;
                HplSaveHandler.FolderOverride = previousFolder;
                log.Save("Dev room tests");
                if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
            }
        }

        static string HplGameUiPath() => LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, "maps/main/ch01/00_rainy_hall.map"));
        static void Step(HplMap map, float seconds)
        {
            for (int i = 0; i < Mathf.CeilToInt(seconds / Time.fixedDeltaTime); i++) map.Step(Time.fixedDeltaTime);
        }
        static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
