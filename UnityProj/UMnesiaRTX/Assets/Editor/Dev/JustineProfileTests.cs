using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.ScriptApi;
using RTXMnesia.Player;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>J0 acceptance: import DLC data without mutating the base campaign's assets.</summary>
    public static class JustineProfileTests
    {
        sealed class DeathProbe : HplScript
        {
            public void SelectVoice() => SetPlayerPermaDeathSound("alois_death.snt");
            public void Die() => GivePlayerDamage(500, "BloodSplat", false, true);
        }

        public static void MapBatch()
        {
            var log = new ImportLog();
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++; log.Info("OK: " + message);
            }
            string original = Path.Combine(AmnesiaPaths.SourceRoot, HplCampaign.Justine.FirstMap);
            string scene = LevelConverter.ScenePathFor(original);
            if (!File.Exists(scene)) scene = LevelConverter.Convert(original);
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            var player = map.player;
            var simulation = Physics.simulationMode;
            try
            {
                HplGame.Reset();
                map.runScript = false;
                Physics.simulationMode = SimulationMode.Script;
                Physics.SyncTransforms();
                player.readDevices = false;
                foreach (string method in new[] { "Awake", "Start" })
                    typeof(AmnesiaPlayer).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                map.Build();
                map.Begin();
                Check(map.Campaign.IsJustine && map.gameConfig.sourceFile == "config/ptest_game.cfg", "DLC map has its own game config");
                Check(map.menuConfig.sourceFile == "config/ptest_menu.cfg" && map.language.campaignId == "justine", "DLC map has its own menu and language");
                Check(map.CheckPointName == "", "DLC receives no automatic checkpoint");
                var inventory = new HplInventory(map);
                Check(inventory.Assets == AssetDatabase.LoadAssetAtPath<HplInventoryAssets>(HplCampaign.Justine.AssetPath(InventoryConverter.AssetPath)), "Runtime inventory selects DLC data");
                Check(HplGame.CombineCallbacks.Count == 0, "DLC runtime has no main recipes");
                var probe = new DeathProbe();
                typeof(HplScript).GetProperty("Map").SetValue(probe, map);
                probe.SelectVoice();
                Check(player.Death.PermaDeathSound == "alois_death.snt", "Perma-death API stores selected sound");
                var carry = player.CaptureCarry();
                player.Death.PermaDeathSound = "";
                player.ApplyCarry(carry);
                Check(player.Death.PermaDeathSound == "alois_death.snt", "Selected death voice survives map carry");
                probe.Die();
                bool heard = false, waited = false;
                var played = new HashSet<string>();
                for (int i = 0; i < 60 * 45 && !player.Death.Terminal; i++)
                {
                    map.Step(1f / 60f);
                    foreach (var e in map.sound.Entries) played.Add(e.Clip.name);
                    if (map.sound.Entries.Any(e => e.Alive && e.Clip.name == "alois_death_01"))
                    {
                        heard = true;
                        waited |= player.Death.State == 4 && !player.Death.Terminal;
                    }
                }
                log.Info($"Death fixture: terminal={player.Death.Terminal}, state={player.Death.State}, health={player.health}, sound={player.Death.PermaDeathSound}, sounds={string.Join(",", played)}");
                Check(heard && waited, "Perma death plays and waits for the original English voice");
                Check(player.Death.Terminal && player.Death.State == 4 && player.IsDead, "Perma death reaches terminal state without respawn");
                Check(player.Death.TextAlpha == 0 && map.CheckPointCount == 0, "Perma death shows no checkpoint hint and never loads a checkpoint");
                log.Info($"PASS: {checks} Justine map and perma-death checks (edit-mode simulation; application exit requires a separate Play check)");
            }
            finally
            {
                log.Save("Justine map profile acceptance");
                Physics.simulationMode = simulation;
                HplGame.Reset();
            }
        }

        [MenuItem("Tools/RTXMnesia/Tests/Justine profile")]
        public static void PrepareBatch()
        {
            var log = new ImportLog();
            var c = HplCampaign.Justine;
            var mainFiles = new[] {
                ConfigConverter.AssetPathFor("config/game.cfg"), ConfigConverter.AssetPathFor("config/menu.cfg"),
                LanguageConverter.AssetPathFor("russian"), SurfaceConverter.AssetPath, SoundConverter.BankPath,
                MenuConverter.AssetPath, InventoryConverter.AssetPath, JournalConverter.AssetPath, LoadScreenConverter.AssetPath,
                FontConverter.FontsRoot + "/game_default.asset", FontConverter.FontsRoot + "/menu.asset" };
            var before = mainFiles.ToDictionary(p => p, p => (bytes: File.ReadAllBytes(p), guid: AssetDatabase.AssetPathToGUID(p)));
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++; log.Info("OK: " + message);
            }
            var init = ConfigConverter.Convert(c.InitFile, log, c);
            Check(init.GetString("ConfigFiles", "Game") == c.GameConfig, "DLC game config matches init");
            Check(init.GetString("ConfigFiles", "Menu") == c.MenuConfig, "DLC menu config matches init");
            Check(init.GetString("ConfigFiles", "PreMenu") == c.PreMenuConfig, "DLC pre-menu matches init");
            Check(init.GetString("ConfigFiles", "Materials") == c.Materials, "DLC physics materials match init");
            Check(init.GetString("Directories", "GameLanguageFolder").TrimEnd('/') == c.LanguageFolder, "DLC language folder matches init");
            Check(init.GetString("StartMap", "Folder") + init.GetString("StartMap", "File") == c.FirstMap, "DLC start map matches init");
            Check(init.GetString("StartMap", "Pos") == "PlayerStartArea_1", "DLC original start position");
            foreach (string file in new[] { c.GameConfig, c.MenuConfig, c.PreMenuConfig, c.Materials, "resources.cfg", "config/default_user_keys.cfg", "config/base_russian.lang", "config/base_english.lang" })
                Check(File.Exists(Path.Combine(AmnesiaPaths.SourceRoot, file)), "Referenced resource exists: " + file);
            ConfigConverter.Convert(c.GameConfig, log, c);
            var menu = ConfigConverter.Convert(c.MenuConfig, log, c);
            var preMenu = ConfigConverter.Convert(c.PreMenuConfig, log, c);
            Check(preMenu.sourceXml.Contains("PTestWelcome03"), "Nested pre-menu XML retained");
            ConfigConverter.Convert(ConfigConverter.KeysConfig, log, c);
            var language = LanguageConverter.Convert("russian", log, c);
            Check(language.sourceFiles.SequenceEqual(new[] { "config/lang_ptest/russian.lang", "config/base_russian.lang", "config/lang_ptest/english.lang", "config/base_english.lang" }), "Language loading and fallback order");
            Check(language.resourceDirs.Contains("fonts/rus") && language.resourceDirs.Contains("lang/eng") && !language.resourceDirs.Contains("lang/rus"), "Russian fonts and original English voices");
            Check(language.Translate("Inventory", "ItemName_Ladder").Contains("лестниц"), "Russian DLC item text decoded");
            Check(language.voices.Any(v => v.file.StartsWith("lang/eng/ptest/") && v.duration > 0), "DLC English voices have durations");
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            var menus = MenuConverter.Convert(index, language, menu, log);
            var inventory = InventoryConverter.Convert(index, language, log);
            JournalConverter.Convert(index, language, log);
            var loading = LoadScreenConverter.Convert(index, language, menu, log);
            Check(loading.images.Count(i => i.file.StartsWith("graphics/ptest/")) == 3, "All three DLC loading images imported");
            SurfaceConverter.Convert(log, c);
            var sound = SoundConverter.Convert(log, language);
            Check(menus.titleLogo != null && AssetDatabase.GetAssetPath(menus.titleLogo).Contains("graphics_ptest_menu"), "Separate Justine menu logo");
            Check(inventory.items.Any(i => i.file.Contains("stencil")), "DLC inventory definitions imported");
            Check(sound.clips.Any(i => i.path.StartsWith("lang/eng/ptest/")), "DLC voice clips imported");
            foreach (var p in mainFiles)
                Check(File.ReadAllBytes(p).SequenceEqual(before[p].bytes) && AssetDatabase.AssetPathToGUID(p) == before[p].guid, "Main asset content and GUID preserved: " + p);
            Check(HplCampaign.ForMap("maps/ptest/01_cells.map") == c && HplCampaign.ForMap(c.MenuMap) == c, "Map and menu select DLC profile");
            Check(HplCampaign.ForMap("maps/main/ch02/20_sewer.map") == HplCampaign.Main, "Main maps keep main profile");
            var go = new GameObject("Justine inventory profile checks") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                HplGame.Reset();
                var map = go.AddComponent<HplMap>(); map.mapFile = c.FirstMap;
                HplGame.EnsureInventoryScript(map);
                Check(HplGame.InventoryScript == null && HplGame.CombineCallbacks.Count == 0, "Empty DLC inventory script does not register base recipes");
                map.mapFile = HplCampaign.Main.FirstMap;
                HplGame.EnsureInventoryScript(map);
                Check(HplGame.InventoryScript != null && HplGame.CombineCallbacks.Count == 52, "Base inventory still registers 52 recipes");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); HplGame.Reset(); }
            MainMenuConverter.ConvertJustineBatch();
            Check(EditorBuildSettings.scenes[0].path == HplGameUi.MainMenuScene, "Main title scene remains first");
            Check(log.Errors == 0, "J0 resource import has no errors");
            log.Info($"PASS: {checks} Justine profile checks");
            log.Save("Justine profile acceptance");
        }
    }
}
