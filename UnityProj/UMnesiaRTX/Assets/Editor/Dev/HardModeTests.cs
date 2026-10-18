using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RTXMnesia.Effects;
using RTXMnesia.Enemies;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The hard mode (HplDifficulty, the "HARDMODE" blocks of the game code) on imported maps, edit mode with the map
    /// stepping and no level script: 01_old_archives — tinderboxes and lamp oil removed at load, no auto saves, the
    /// tinderbox save, saves keep the mode, sanity death and its hint, the darkness light; 11_study — the oil barrel;
    /// 10_daniels_room — the enemy senses, speeds, damage and no danger music.
    /// Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.HardModeTests.RunBatch.
    /// Result in Logs/AmnesiaImport.log.
    /// </summary>
    public static class HardModeTests
    {
        [MenuItem("Tools/RTXMnesia/Tests/Hard mode")]
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
            string saves = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "HardModeCheck"));
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
            HplSaveHandler.FolderOverride = saves;
            bool failed = false;
            try
            {
                HplGame.Reset();
                HplInsanity.ResetSession();
                Items(Check);
                Saves(Check, saves);
                Menu(Check);
                Player(Check);
                Barrel(Check);
                Enemies(Check);
                log.Info($"PASS: {checks} hard mode checks");
                log.Save("Hard mode tests");
                Debug.Log($"PASS: {checks} hard mode checks");
            }
            catch (Exception ex)
            {
                log.Error($"FAIL after {checks} checks: {ex.Message}\n{ex.StackTrace}");
                log.Save("Hard mode tests");
                Debug.LogError($"FAIL: {ex.Message}");
                failed = true;
            }
            finally
            {
                HplDifficulty.HardMode = false;
                Physics.simulationMode = simulationMode;
                HplSaveHandler.FolderOverride = null;
                HplSavedMaps.Reset();
                HplInsanity.ResetSession();
                HplGame.Reset();
            }
            if (failed && Application.isBatchMode) EditorApplication.Exit(1);
        }

        // ------------------------------------------------------------------ cLuxMap::LoadFromFile

        static void Items(Action<bool, string> check)
        {
            HplDifficulty.HardMode = false;
            var (map, player) = Open("maps/main/ch01/01_old_archives.map");
            var items = map.Entities.OfType<PropItem>().Where(i => i.ItemType is "Tinderbox" or "LampOil").Select(i => i.Name).ToList();
            check(!map.Entities.Any(map.IsDestroyPending), $"normal mode: all {items.Count} tinderboxes and oil stay");

            HplDifficulty.HardMode = true;
            (map, player) = Open("maps/main/ch01/01_old_archives.map");
            // The expected removal of LoadFromFile with the player's tinderboxes of this load.
            float t = player.tinderboxes / 6f, every = (1f - t) * 0.7f + t * 0.95f, tc = 0f, oc = 0f;
            int tinder = 0, oil = 0, removedTinder = 0, removedOil = 0;
            foreach (var item in map.Entities.OfType<PropItem>())
            {
                bool removed = map.IsDestroyPending(item);
                if (item.ItemType == "Tinderbox")
                {
                    tinder++;
                    tc += every;
                    bool expected = tc >= 1f;
                    if (expected) tc -= 1f;
                    check(removed == expected, $"tinderbox {item.Name}: removed {removed}, expected {expected}");
                    if (removed) removedTinder++;
                }
                else if (item.ItemType == "LampOil")
                {
                    oil++;
                    oc += 0.65f;
                    bool expected = oc >= 1f;
                    if (expected) oc -= 1f;
                    check(removed == expected, $"oil {item.Name}: removed {removed}, expected {expected}");
                    if (removed) removedOil++;
                }
                else check(!removed, $"{item.Name} ({item.ItemType}) stays");
            }
            check(tinder > 0 && removedTinder > 0 && removedOil > 0 && removedTinder < tinder,
                  $"hard mode: {removedTinder} of {tinder} tinderboxes (every {every:0.00}) and {removedOil} of {oil} oil removed");
            Step(map, 0.1f);
            check(map.Entities.OfType<PropItem>().Count(i => i.ItemType is "Tinderbox" or "LampOil") == tinder + oil - removedTinder - removedOil,
                  "the removed items are gone after the first step");
        }

        // ------------------------------------------------------------------ cLuxSaveHandler

        static void Saves(Action<bool, string> check, string folder)
        {
            HplDifficulty.HardMode = true;
            var (map, player) = Open("maps/main/ch01/01_old_archives.map");
            check(HplSaveHandler.AutoSave(map) == null && !Directory.Exists(Path.Combine(folder, "main")), "no auto saves");
            string file = HplSaveHandler.HardModeSave(map);
            check(file != null && Path.GetFileName(file).StartsWith("HardMode_", StringComparison.Ordinal), $"tinderbox save {Path.GetFileName(file)}");
            var save = HplSaveHandler.ReadFile(file);
            check(save != null && save.hardMode, "the save keeps the hard mode");
            check(HplSaveHandler.AutoLoadFile(map.Campaign) == file, "AutoLoad takes the newest HardMode save");
            check(HplSaveHandler.GetProperSaveName(file, null).StartsWith("HardMode - ", StringComparison.Ordinal), "its list name");
            // A newer auto save (of a normal game): the hard mode AutoLoad refuses it.
            string auto = Path.Combine(Path.GetDirectoryName(file), "AutoSave_NULL_2030_1_1_1_1_1_0.sav");
            File.Copy(file, auto);
            File.SetLastWriteTime(auto, DateTime.Now.AddMinutes(1));
            check(HplSaveHandler.AutoLoadFile(map.Campaign) == null, "hard mode AutoLoad: the newest is not a HardMode save → none");
            HplDifficulty.HardMode = false;
            check(HplSaveHandler.AutoLoadFile(map.Campaign) == auto, "normal AutoLoad takes the newest");
            HplSaveHandler.BeginLoad(save);
            check(HplDifficulty.HardMode, "loading a hard mode save sets the mode");
            HplSaveHandler.ClearPending();
            File.Delete(auto);
        }

        // ------------------------------------------------------------------ cLuxMainMenu in game: Save for tinderboxes

        static void Menu(Action<bool, string> check)
        {
            HplDifficulty.HardMode = true;
            var (map, player) = Open("maps/main/ch01/01_old_archives.map");
            var ui = map.gameObject.AddComponent<RTXMnesia.Ui.HplGameUi>();
            float timeScale = Time.timeScale;
            try
            {
                ui.Init(map);
                player.tinderboxes = 3;
                ui.Pause();
                bool Has(RTXMnesia.Ui.MenuControl c) => Enumerable.Range(0, ui.Menu.ButtonCount).Any(i => ui.Menu.ControlAt(i) == c);
                check(ui.Menu.ControlAt(1) == RTXMnesia.Ui.MenuControl.HardSave && !Has(RTXMnesia.Ui.MenuControl.ExitAndSave),
                      "in-game menu: Save after Back to game, no Exit and save");
                check(ui.Choose(RTXMnesia.Ui.MenuControl.HardSave) && !ui.ConfirmingQuit, "3 tinderboxes: Save is disabled");
                player.tinderboxes = 5;
                check(ui.Choose(RTXMnesia.Ui.MenuControl.HardSave) && ui.ConfirmingQuit && ui.PendingConfirmation == RTXMnesia.Ui.MenuControl.ConfirmHardSave,
                      "5 tinderboxes: Save asks");
                // Two saves in the same second share a name (cLuxSaveHandler::GetSaveName resets its counter): an empty folder.
                if (Directory.Exists(HplSaveHandler.Folder(map.Campaign))) Directory.Delete(HplSaveHandler.Folder(map.Campaign), true);
                int files = 0;
                check(ui.Choose(RTXMnesia.Ui.MenuControl.ConfirmHardSave) && player.tinderboxes == 1 && !ui.ConfirmingQuit &&
                      Directory.Exists(HplSaveHandler.Folder(map.Campaign)) && Directory.GetFiles(HplSaveHandler.Folder(map.Campaign)).Length == files + 1,
                      $"Save costs 4 tinderboxes and writes a save (tinderboxes {player.tinderboxes}, confirming {ui.ConfirmingQuit}, files {files})");
                player.tinderboxes = 10;
                check(ui.Choose(RTXMnesia.Ui.MenuControl.HardSave) && !ui.ConfirmingQuit, "Save is disabled for the rest of this menu");
                HplDifficulty.HardMode = false;
                ui.MenuScreen.Show();
                check(!Has(RTXMnesia.Ui.MenuControl.HardSave) && Has(RTXMnesia.Ui.MenuControl.ExitAndSave), "normal mode: Exit and save, no Save");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ui);
                // The pause stops the clock; a batch run would keep it in ProjectSettings.
                Time.timeScale = timeScale;
            }
        }

        // ------------------------------------------------------------------ cLuxPlayer, cLuxPlayerDeath, darkness

        static void Player(Action<bool, string> check)
        {
            HplDifficulty.HardMode = true;
            var (map, player) = Open("maps/main/ch01/01_old_archives.map");
            var cfg = map.gameConfig;
            check(Mathf.Approximately(player.InDarkness.AmbientLight.radius, cfg.GetFloat("Player_Darkness", "AmbientLightRadius", 0f) * 0.5f),
                  $"darkness light radius × 0.5 ({player.InDarkness.AmbientLight.radius})");
            player.SetSanity(10f);
            player.LowerSanity(20f, false);
            check(player.health <= 0f && player.Death.Active, "losing all sanity kills");
            check(player.Death.CurrentHintText == map.hud.Translate("Hints", "HardModeDeath"), $"death hint \"{player.Death.CurrentHintText}\"");

            (map, player) = Open("maps/main/ch01/01_old_archives.map");
            player.SetSanity(0f);
            check(player.Death.Active, "SetSanity(0) kills");

            HplDifficulty.HardMode = false;
            (map, player) = Open("maps/main/ch01/01_old_archives.map");
            player.SetSanity(10f);
            player.LowerSanity(20f, false);
            check(player.health > 0f && !player.Death.Active && player.Death.CurrentHintText == "", "normal mode: no sanity is the collapse, not death");
        }

        // ------------------------------------------------------------------ cLuxProp_OilBarrel

        static void Barrel(Action<bool, string> check)
        {
            HplDifficulty.HardMode = true;
            var (map, player) = Open("maps/main/ch01/11_study.map");
            var barrel = (PropOilBarrel)map.GetEntityByName("oil_barrel_1");
            HplGame.Items.Clear();
            player.lampOil = 40f;
            barrel.GetFocusText();
            check(barrel.OilAmount == 10f, $"oil barrel: always 10 ({barrel.OilAmount})");
        }

        // ------------------------------------------------------------------ iLuxEnemyLoader, cLuxMusicHandler

        static void Enemies(Action<bool, string> check)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            float Field(HplEnemy e, string name) => (float)typeof(HplEnemy).GetField(name, flags).GetValue(e);
            EnemyAttackDamage Damage(HplEnemy e) => (EnemyAttackDamage)typeof(HplEnemy).GetField("NormalAttackDamage", flags).GetValue(e);

            HplDifficulty.HardMode = false;
            var (map, player) = Open("maps/main/ch01/10_daniels_room.map");
            var grunt = (EnemyGrunt)map.GetEntityByName("grunt_normal_1");
            float sight = Field(grunt, "SightRange"), hear = Field(grunt, "HearVolume"), search = Field(grunt, "PlayerSearchTime"), turn = Field(grunt, "TurnSpeedMul");
            float minDamage = Damage(grunt).minDamage, maxDamage = Damage(grunt).maxDamage;

            HplDifficulty.HardMode = true;
            (map, player) = Open("maps/main/ch01/10_daniels_room.map");
            grunt = (EnemyGrunt)map.GetEntityByName("grunt_normal_1");
            check(Mathf.Approximately(Field(grunt, "SightRange"), sight * 1.15f) && Mathf.Approximately(Field(grunt, "HearVolume"), hear * 0.6f) &&
                  Mathf.Approximately(Field(grunt, "PlayerSearchTime"), search * 2.5f) && Mathf.Approximately(Field(grunt, "TurnSpeedMul"), turn * 1.35f),
                  $"grunt senses: sight {sight} → {Field(grunt, "SightRange")}, hearing {hear} → {Field(grunt, "HearVolume")}, search {search} → {Field(grunt, "PlayerSearchTime")}, turn × 1.35");
            check(Damage(grunt).minDamage == minDamage * 2f && Damage(grunt).maxDamage == maxDamage * 2f,
                  $"double damage {minDamage}–{maxDamage} → {Damage(grunt).minDamage}–{Damage(grunt).maxDamage}");
            // EnemyTests: the player in range for 2 s starts the danger music; not in hard mode.
            player.PlaceAt(new Vector3(-10.51f, 0.05f, 7.54f), 0f);
            Step(map, 1f);
            grunt.SetActive(true);
            Step(map, 3f);
            check(grunt.PlayerInRange && !map.sound.Music.DangerMusicPlaying, "no danger music with the player in range");
        }

        // ------------------------------------------------------------------ helpers

        static (HplMap map, AmnesiaPlayer player) Open(string mapFile)
        {
            HplGame.Reset();
            HplSavedMaps.Reset();
            string scenePath = LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, mapFile));
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

        static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
