using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using RTXMnesia.Dev;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    [InitializeOnLoad]
    public static class DebugPanelTests
    {
        const string Pending = "RTXMnesia.DebugPanelTests.PlayPending", Started = "RTXMnesia.DebugPanelTests.Started";
        static DebugPanelTests()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Debug panel verification").AddComponent<HplDebugPanelProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(HplDebugPanelProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (!SessionState.GetBool(Pending, false) || EditorApplication.timeSinceStartup - SessionState.GetFloat(Started, 0) <= 300) return;
                Debug.LogError("Debug panel Play check timed out");
                SessionState.SetBool(Pending, false);
                EditorApplication.Exit(1);
            };
        }

        // No -quit: entering and leaving Play mode controls the batch process lifetime.
        public static void RunPlayBatch()
        {
            EditorSceneManager.OpenScene(HplGameUi.MainMenuScene);
            SessionState.SetInt(HplDebugPanelProbe.ExitCodeKey, 1);
            SessionState.SetFloat(Started, (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/RTXMnesia/Tests/Debug panel")]
        public static void RunBatch()
        {
            int checks = 0;
            void Check(bool value, string message)
            {
                if (!value) throw new InvalidOperationException(message);
                checks++;
            }
            var physics = Physics.simulationMode;
            float timeScale = Time.timeScale;
            var cursor = Cursor.lockState;
            bool visible = Cursor.visible;
            string report = Path.GetFullPath("Logs/DebugPanelCheck.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                HplGame.Reset();
                Check(!new HplUserSettings().debugPanel, "Panel must default to off");
                var stages = HplDebugProgression.AvailableStages();
                Check(stages.Select(s => s.MapName).Distinct().Count() == 38, "All 37 game maps and dev_room must be listed");
                var bank = Resources.Load<HplInventoryAssets>(HplInventoryAssets.ResourceName);
                Check(bank != null, "Imported item definitions missing");
                foreach (var stage in stages)
                {
                    Check(File.Exists(stage.ScenePath), "Scene missing: " + stage.ScenePath);
                    Check(stage.Items.Select(i => i.Name).Distinct().Count() == stage.Items.Count, "Duplicate items: " + stage.Label);
                    string source = Path.Combine(AmnesiaPaths.SourceRoot, stage.ScenePath.Substring("Assets/AmnesiaImported/Levels/".Length).Replace(".unity", ".map"));
                    var localItems = File.Exists(source) ? XDocument.Load(source).Descendants("Entity").Select(e => (string)e.Attribute("Name")).ToHashSet() : new System.Collections.Generic.HashSet<string>();
                    foreach (var item in stage.Items)
                    {
                        Check(item.File == "stone_hammer_chipper.ent" || bank.Item(item.File) != null, "Item definition missing: " + item.File);
                        Check(!localItems.Contains(item.Name), "Local puzzle item granted: " + stage.MapName + " / " + item.Name);
                    }
                }
                HplDebugProgression.Stage Stage(string map, string variant = "Первый вход") => stages.Single(s => s.MapName == map && s.Variant == variant);
                Check(!Stage("12_storage").Items.Any(i => i.Name.StartsWith("hand_drill_part")), "Storage must retain drill puzzle");
                Check(Stage("13_machine_room").Items.Count(i => i.Name.StartsWith("guiding_rod")) == 3, "Machine room requires three external rods");
                Check(!Stage("15_prison_south").Items.Any(i => i.Name == "stone_hammer_chipper"), "South prison must retain hammer puzzle");
                Check(Stage("15_prison_north").Items.Any(i => i.Name == "stone_hammer_chipper"), "North prison needs hammer from south");
                Check(Stage("04_wine_cellar_lab", "С реагентами из винного погреба").Items.Count == 4, "Lab return needs chemicals, not its local jar");
                Check(Stage("27_torture_chancel_redux").Items.Count(i => i.Name.StartsWith("OrbPiece_")) == 6, "Final chancel requires all six external pieces");

                // Real map, player, enemy, pause and inventory; scripts run their normal first-entry path.
                var entry = Stage("19_morgue");
                HplGame.Reset();
                HplDebugSession.Start(entry, false);
                EditorSceneManager.OpenScene(entry.ScenePath);
                var player = UnityEngine.Object.FindAnyObjectByType<AmnesiaPlayer>();
                Invoke(player, "Awake"); Invoke(player, "Start");
                player.readDevices = false;
                var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
                map.Build(); map.Begin();
                var ui = map.gameObject.AddComponent<HplGameUi>(); ui.Init(map);
                typeof(HplMap).GetProperty(nameof(HplMap.GameUi)).SetValue(map, ui);
                Check(HplDebugSession.Active && map.disableAutoSaves, "Debug entry must prevent autosaves before level OnEnter");
                Check(HplGame.HasType("Lantern") && HplGame.Items.Single(i => i.type == "LampOil").count == 5 && player.tinderboxes == 15 && player.lampOil == 100, "Starter supplies");
                Check(map.Script.HasUseItemCallback("handdrill", "AreaCorpseHead") && HplGame.GetItem("handdrill") != null, "Morgue drill must be usable");
                Check(HplGame.GetItem("copper_tube_1") == null, "Morgue copper tube must remain local");
                int count = HplGame.Items.Count;
                HplDebugSession.Supply(map); HplDebugSession.Supply(map);
                Check(HplGame.Items.Count == count && HplGame.Items.Single(i => i.type == "LampOil").count == 5, "Refill must be idempotent");
                Check(!ui.OpenDebugPanel(), "Disabled panel opened");
                ui.Settings.debugPanel = true;
                float before = Time.timeScale;
                Check(ui.OpenDebugPanel() && ui.DebugOpen && ui.IsPaused && player.UiInputBlocked && Time.timeScale == 0, "Panel must pause world and block player input");
                ui.Resume();
                Check(!ui.DebugOpen && !ui.IsPaused && !player.UiInputBlocked && Time.timeScale == before, "Panel close must restore input and clock");
                ui.Pause(); ui.OpenOptions(0);
                Check(ui.Choose(MenuControl.DebugPanel) && !ui.DraftSettings.debugPanel, "Settings checkbox must toggle draft");
                ui.Resume();
                HplDebugSession.DisableEnemyAI = true;
                Check(HplDebugSession.EnemiesPaused, "AI switch did not apply");
                var enemy = map.Enemies.First();
                enemy.SetActive(true);
                Vector3 position = enemy.Body.Position;
                enemy.AlertOfPlayerPresence();
                for (int i = 0; i < 30; i++) map.Step(Time.fixedDeltaTime);
                Check(enemy.Body.Position == position, "Disabled AI moved its character body");
                ui.Settings.debugPanel = false;
                Check(!HplDebugSession.EnemiesPaused, "Disabling panel must release AI override");
                string blocked = Path.GetFullPath("Logs/DebugPanelMustNotSave.sav");
                Check(HplSaveHandler.AutoSave(map) == null && !HplSaveHandler.SaveGameToFile(map, blocked) && !File.Exists(blocked), "Debug session wrote a save");
                HplGame.Reset();
                Check(!HplDebugSession.Active && !HplDebugSession.DisableEnemyAI && HplGame.Items.Count == 0, "New game / load must reset debug run");
                File.WriteAllText(report, $"PASS: {checks} debug panel checks, {stages.Count} entry presets\n");
                Debug.Log(File.ReadAllText(report));
            }
            catch (Exception ex)
            {
                File.WriteAllText(report, $"FAIL after {checks} checks: {ex}\n");
                Debug.LogException(ex);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                HplGame.Reset();
                Physics.simulationMode = physics;
                Time.timeScale = timeScale;
                Cursor.lockState = cursor; Cursor.visible = visible;
            }
        }

        static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
    }
}
