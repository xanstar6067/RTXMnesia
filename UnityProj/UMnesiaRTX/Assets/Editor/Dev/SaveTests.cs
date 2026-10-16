using System;
using System.IO;
using System.Linq;
using RTXMnesia.Effects;
using RTXMnesia.Enemies;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Saving and loading (cLuxSaveHandler) on the imported 10_daniels_room, edit mode with the map stepping like
    /// PlayerSimulation and no level script: the state of the map, its entities and the game modules is changed,
    /// written to a save file in Logs/SaveCheck (never the real save folder), the scene is opened again and the save
    /// loaded into it; then the state is compared. Also file names, the newest file, the list and the auto save limit.
    /// Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.SaveTests.RunBatch.
    /// Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class SaveTests
    {
        const string MapFile = "maps/main/ch01/10_daniels_room.map";
        const string Grunt = "grunt_normal_1";

        [MenuItem("Tools/RTXMnesia/Tests/Saving (10_daniels_room)")]
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
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "SaveCheck"));
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            HplSaveHandler.FolderOverride = folder;
            bool failed = false;
            try
            {
                // ---------------------------------------------------------- change the game
                HplGame.Reset();
                HplInsanity.ResetSession();
                var (map, player) = Open();
                Check(!HplSaveHandler.SaveFileExists(map.Campaign), "no save yet in the test folder");

                map.SetVar("SaveTestVar", "17");
                map.AddTimer("SaveTestTimer", 30f, "SaveTestTimerFunc");
                map.DisplayNameEntry = "DanielsRoom";
                map.SetCheckPoint("SaveTestCheckPoint", "PlayerStartArea_1", "SaveTestCheckPointFunc");
                HplGame.SetGlobalVar("SaveTestGlobal", "abc");
                HplGame.AddQuest("SaveTestQuest", "SaveTestQuestEntry");
                HplGame.Items.Add(new HplGame.Item { name = "save_test_key", type = "Puzzle", subType = "", image = "graphics/item/key.tga", count = 1 });
                HplGame.AddJournalEntry("Note", "SaveTestNote", "");
                HplGame.LastReadEntry = HplGame.JournalEntries[0];
                HplGame.HintsGiven.Add("SaveTestHint");
                HplGame.Coins = 7;
                HplLoadScreen.SetupLoadText("LoadTexts", "SaveTestText", 3, "game_loading_rose.jpg");

                player.health = 42f;
                player.sanity = 55f;
                player.lampOil = 33f;
                player.tinderboxes = 4;
                player.scriptMoveSpeedMul = 0.6f;
                player.FadeFOVMulTo(1.3f, 10f);
                var start = map.GetPlayerStart("PlayerStartArea_1");
                Vector3 feet = start.transform.position + start.transform.right * 1.0f + new Vector3(0f, 0.05f, 0f);
                player.PlaceAt(feet, 1.1f);
                player.SetLook(1.1f, 0.3f);
                Step(map, 0.5f);
                Vector3 playerFeet = player.Body.FeetPosition;

                // An item picked up, a disabled entity, a locked door, a moved grab prop.
                var item = map.Entities.OfType<PropItem>().First();
                string itemName = item.Name;
                map.DestroyEntity(item);
                var door = map.Entities.OfType<PropSwingDoor>().First(d => !d.Locked);
                door.SetLocked(true, false);
                var grab = map.Entities.OfType<PropObject>().First(o => o.ObjType == PropObject.ObjectType.Grab && o.PhysicsBodies.Count > 0 && o.Active && o.PhysicsBodies[0].Mass > 0f);
                var grabBody = grab.PhysicsBodies[0];
                Vector3 grabTarget = grabBody.Position + new Vector3(0.3f, 0.4f, -0.2f);
                grabBody.SetPose(grabTarget, Quaternion.Euler(10f, 20f, 30f) * grabBody.Rotation);
                grabBody.LinearVelocity = Vector3.zero;
                grabBody.AngularVelocity = Vector3.zero;
                var disabled = map.Entities.OfType<HplArea>().First(a => a.Active && a.AreaType == "Script");
                disabled.SetActive(false);
                Step(map, 1f / 60f);
                Vector3 grabPos = grabBody.Position;
                Quaternion grabRot = grabBody.Rotation;

                // The grunt: active, moved, patrolling.
                var grunt = (EnemyGrunt)map.GetEntityByName(Grunt);
                CallScript(map, "SetEnemyDisableTriggers", Grunt, true);
                grunt.SetActive(true);
                CallScript(map, "AddEnemyPatrolNode", Grunt, "PathNodeArea_1", 2f, "");
                CallScript(map, "AddEnemyPatrolNode", Grunt, "PathNodeArea_13", 0f, "");
                Step(map, 1f);
                Vector3 gruntFeet = grunt.Body.FeetPosition;
                var gruntState = grunt.CurrentState;
                float gruntYaw = grunt.HplYaw;

                // Modules: music, effects, insanity, global sound, voices.
                map.sound.Music.Play("09_amb_safe.ogg", true, 1f, 0.5f, 0, true);
                Step(map, 2f);
                float musicTime = 0f;
                map.effects.Sepia.FadeTo(0.6f, 100f);
                map.effects.Fade.FadeOut(0.001f);
                Step(map, 0.1f);
                map.effects.Fade.FadeIn(100f);
                CallScript(map, "SetInsanitySetEnabled", "Ch02", false);
                HplInsanity.StartEvent(map, "Steps_Muffled");
                HplInsanity.StopCurrentEvent();
                CallScript(map, "FadeGlobalSoundVolume", 0.4f, 0f);
                Step(map, 1f / 60f);
                var insanityBefore = HplInsanity.CaptureSave();
                float timerLeft = map.GetTimerTimeLeft("SaveTestTimer");
                var musicSave = map.sound.Music.CaptureSave();
                musicTime = musicSave.currentTime;
                Check(player.health > 40f && !grunt.PlayerDetected, $"the grunt (triggers off) leaves the player alone (health {player.health:0})");
                Check(musicSave.currentMusic != "" && musicTime > 1f && musicSave.music[0].file == "09_amb_safe.ogg",
                      $"music plays before saving ({musicSave.currentMusic} at {musicTime:0.00} s, priority 0: {musicSave.music[0].file})");

                // Leave and remember an earlier visit of another map in the collection.
                HplSavedMaps.Reset();
                HplSavedMaps.SaveMap(map);
                Check(HplSavedMaps.Count == 1, "the map cache holds one visited map");

                // ---------------------------------------------------------- save
                var playerBefore = player.CaptureSave();
                playerFeet = player.Body.FeetPosition;
                gruntFeet = grunt.Body.FeetPosition;
                gruntState = grunt.CurrentState;
                gruntYaw = grunt.HplYaw;
                grabPos = grabBody.Position;
                grabRot = grabBody.Rotation;
                string path = HplSaveHandler.AutoSave(map);
                Check(path != null && File.Exists(path), $"auto save written: {Path.GetFileName(path)}");
                Check(path.StartsWith(folder, StringComparison.OrdinalIgnoreCase), "into the test folder, not Documents/Amnesia");
                Check(Path.GetFileName(path).StartsWith("AutoSave_DanielsRoom_"), "file name: AutoSave_<map display name>_date_counter.sav");
                string proper = HplSaveHandler.GetProperSaveName(path, (cat, entry) => map.hud.TryTranslate(cat, entry, out var t) ? t : entry);
                Check(proper.StartsWith("AutoSave - ") && !proper.Contains("DanielsRoom") && !proper.Contains("INVALID"),
                      $"proper save name with the translated level name: {proper}");
                Check(HplSaveHandler.SaveFileExists(map.Campaign) && HplSaveHandler.GetNewestSaveFile(map.Campaign) == path, "the newest save is this one (Continue)");
                long size = new FileInfo(path).Length;
                var save = HplSaveHandler.ReadFile(path);
                Check(save != null && save.mapFile == MapFile && save.map.full && save.map.fullEntities.Count > 50,
                      $"the file reads back: {save?.map.fullEntities.Count} entities, {size / 1024} KB");
                Check(!save.map.fullEntities.Any(e => e.name == itemName), "the picked up item is not in the save");
                Check(save.savedMaps.Count == 1 && save.savedMaps[0].name == map.MapName, "the map cache is in the save");

                // ---------------------------------------------------------- load into the scene opened again
                HplGame.Reset();
                HplInsanity.Reset();
                HplSaveHandler.BeginLoad(save);
                (map, player) = Open();
                Check(!HplSaveHandler.LoadPending, "the map scene took the pending save");

                Check(map.GetVar("SaveTestVar") == "17", "local variable");
                Check(Mathf.Abs(map.GetTimerTimeLeft("SaveTestTimer") - timerLeft) < 0.001f, $"timer time left ({map.GetTimerTimeLeft("SaveTestTimer"):0.00})");
                Check(map.CheckPointName == "SaveTestCheckPoint" && map.CheckPointCallback == "SaveTestCheckPointFunc", "check point (not the _auto one)");
                Check(map.DisplayNameEntry == "DanielsRoom", "map display name");
                Check(HplGame.GetGlobalVar("SaveTestGlobal") == "abc", "global variable");
                Check(HplGame.GetQuest("SaveTestQuest") != null, "quest");
                Check(HplGame.GetItem("save_test_key") != null, "inventory item");
                Check(HplGame.JournalEntries.Count == 1 && HplGame.LastReadEntry == HplGame.JournalEntries[0], "journal entry and last read text");
                Check(HplGame.HintsGiven.Contains("SaveTestHint") && HplGame.Coins == 7, "hints given and coins");

                var c = playerBefore.carry;
                Check(Mathf.Approximately(player.health, c.health) && Mathf.Approximately(player.sanity, c.sanity) &&
                      Mathf.Approximately(player.lampOil, c.lampOil) && player.tinderboxes == 4,
                      $"health {player.health:0.0}, sanity {player.sanity:0.0}, oil {player.lampOil:0.0}, tinderboxes");
                Check(Mathf.Approximately(player.scriptMoveSpeedMul, 0.6f) && Mathf.Abs(player.FovMul - 1.3f) < 0.001f, $"script speed and FOV fade ({player.FovMul:0.000})");
                Check(Vector3.Distance(player.Body.FeetPosition, playerFeet) < 0.05f && Mathf.Abs(player.Yaw - 1.1f) < 0.001f &&
                      Mathf.Abs(player.Pitch - 0.3f) < 0.001f, $"player position and view ({player.Body.FeetPosition} vs {playerFeet})");

                Check(map.GetEntityByName(itemName) == null, $"the picked up item {itemName} is gone");
                var door2 = (PropSwingDoor)map.GetEntityByName(door.Name);
                Check(door2.Locked, $"door {door.Name} locked");
                var grab2 = (PropObject)map.GetEntityByName(grab.Name);
                var body2 = grab2.PhysicsBodies[0];
                Check(Vector3.Distance(body2.Position, grabPos) < 0.01f && Quaternion.Angle(body2.Rotation, grabRot) < 0.5f,
                      $"moved prop {grab.Name} at its saved pose ({body2.Position} vs {grabPos})");
                Check(!map.GetEntityByName(disabled.Name).Active, $"disabled area {disabled.Name}");

                var grunt2 = (EnemyGrunt)map.GetEntityByName(Grunt);
                Check(grunt2.Active && Vector3.Distance(grunt2.Body.FeetPosition, gruntFeet) < 0.05f && Mathf.Abs(grunt2.HplYaw - gruntYaw) < 0.001f,
                      $"grunt active at its saved position ({grunt2.Body.FeetPosition} vs {gruntFeet})");
                Check(grunt2.CurrentState == gruntState && grunt2.Patrol.Count == 2, $"grunt state {grunt2.CurrentStateName} and patrol nodes");

                var music2 = map.sound.Music.CaptureSave();
                Check(music2.currentMusic == musicSave.currentMusic && Mathf.Abs(music2.currentTime - musicTime) < 0.05f &&
                      music2.currentMaxPrio == musicSave.currentMaxPrio && music2.music.Select(m => m.file).SequenceEqual(musicSave.music.Select(m => m.file)),
                      $"music from its saved time ({music2.currentMusic} {music2.currentTime:0.00} s, priorities)");
                Check(Mathf.Abs(map.effects.Sepia.Amount - 0.6f) < 0.01f, $"sepia ({map.effects.Sepia.Amount:0.00})");
                Check(HplInsanity.DisabledSets.Contains("Ch02") && HplInsanity.Events.Count(e => e.Used) == insanityBefore.used.Count(u => u) &&
                      Mathf.Approximately(HplInsanity.NewEventCount, insanityBefore.newEventCount),
                      $"insanity sets, used events and wait ({HplInsanity.Describe()})");
                Check(Mathf.Abs(map.sound.GlobalVolume.CalcMin(Sound.SoundEntryType.World, 1f) - 0.4f) < 0.01f, "script global volume");
                Check(HplLoadScreen.CaptureSave().textEntry == "SaveTestText", "load screen text");
                Check(HplSavedMaps.MapExists(map.MapName), "map cache restored");

                // The loaded game runs on.
                Step(map, 1f);
                Check(grunt2.Active && player.Body != null, "the loaded map steps");

                // ---------------------------------------------------------- auto save limit
                for (int i = 0; i < 22; i++) HplSaveHandler.AutoSave(map);
                int count = Directory.GetFiles(HplSaveHandler.Folder(map.Campaign), "*.sav").Length;
                Check(count == 20, $"at most Saving/MaxAutoSaves = 20 auto saves ({count})");
                Check(HplSaveHandler.ListSaves(map.Campaign).Count == 20, "the load list shows them, newest first");
                map.DisplayNameEntry = "";
                string nullName = HplSaveHandler.AutoSave(map);
                Check(Path.GetFileName(nullName).StartsWith("AutoSave_NULL_"), "a map without display name saves as NULL");

                log.Info($"PASS: {checks} save checks (map state, entities, player, inventory, journal, globals, hints, music, " +
                         "effects, insanity, sound, load screen, map cache, files)");
                log.Save("Save tests");
                Debug.Log($"PASS: {checks} save checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Save tests");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                Physics.simulationMode = simulationMode;
                HplSaveHandler.FolderOverride = null;
                HplSaveHandler.ClearPending();
                HplInsanity.ResetSession();
                HplGame.Reset();
            }
            if (failed && Application.isBatchMode) EditorApplication.Exit(1);
        }

        static (HplMap map, AmnesiaPlayer player) Open()
        {
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, MapFile));
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            HplMath.Seed(1);
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
                                                                   System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                                                        null, args.Select(a => a.GetType()).ToArray(), null);
            return method.Invoke(map.Script, args);
        }

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                                               System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
