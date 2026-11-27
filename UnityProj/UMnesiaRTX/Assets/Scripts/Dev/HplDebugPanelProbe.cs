#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RTXMnesia.Dev
{
    public sealed class HplDebugPanelProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.DebugPanelTests.PlayExitCode";
        Keyboard _keyboard;
        int _checks;
        string _prefs;
        InputSettings _inputSettings;
        InputSettings.BackgroundBehavior _backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            Application.runInBackground = true;
            // A run with a window (screenshots) must not touch the profile's saves either.
            RTXMnesia.Save.HplSaveHandler.FolderOverride = Path.GetFullPath("Logs/BatchSaves");
            _prefs = PlayerPrefs.HasKey(HplUserSettings.PrefsKey) ? PlayerPrefs.GetString(HplUserSettings.PrefsKey) : null;
            var settings = HplUserSettings.ReadSaved(); settings.debugPanel = true;
            PlayerPrefs.SetString(HplUserSettings.PrefsKey, JsonUtility.ToJson(settings));
            _inputSettings = InputSystem.settings;
            _backgroundBehavior = _inputSettings.backgroundBehavior;
            _editorBehavior = _inputSettings.editorInputBehaviorInPlayMode;
            _inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _keyboard = InputSystem.AddDevice<Keyboard>("DebugPanelTestKeyboard");
            var run = Run();
            while (true)
            {
                bool more = false, failed = false; object current = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    File.WriteAllText(Path.GetFullPath("Logs/DebugPanelPlayCheck.txt"), $"FAIL after {_checks} checks: {ex}\n");
                    failed = true;
                }
                if (failed) { Finish(1); yield break; }
                if (!more) break;
                yield return current;
            }
            File.WriteAllText(Path.GetFullPath("Logs/DebugPanelPlayCheck.txt"), $"PASS: {_checks} Play-mode debug panel checks\n");
            Finish(0);
        }

        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            _checks++;
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            // With a window (screenshots) the first frames take much longer than in batch mode.
            double started = Time.realtimeSinceStartupAsDouble + 60;
            while (Time.realtimeSinceStartupAsDouble < started && FindAnyObjectByType<HplMainMenu>()?.Ui == null) yield return null;
            var title = FindAnyObjectByType<HplMainMenu>();
            Check(title != null && title.Ui != null, "Title screen not initialized");
            var ui = title.Ui;
            ui.Settings.debugPanel = true;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Backquote));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Check(ui.DebugOpen, $"Physical grave key did not open the panel from the title (enabled={_keyboard.enabled}, current={Keyboard.current == _keyboard}, pressed={_keyboard.backquoteKey.isPressed}, loading={HplLoadScreen.Active})");
            yield return new WaitForSecondsRealtime(0.4f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Check(!ui.DebugOpen && ui.IsTitleScreen && ui.IsPaused, "Escape failed to return to the title menu");
            Check(ui.OpenDebugPanel(), "Panel failed to reopen");
            var stages = HplDebugProgression.AvailableStages();
            var destination = stages.Single(s => s.MapName == "13_machine_room");
            HplDebugSession.DisableEnemyAI = true;
            Check(ui.BeginDebugLoad(destination), "Title teleport rejected");
            double deadline = Time.realtimeSinceStartupAsDouble + 90;
            while (HplMap.Current == null || HplMap.Current.MapName != "13_machine_room")
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline) throw new InvalidOperationException("Teleport timed out");
                yield return null;
            }
            yield return null;
            var map = HplMap.Current;
            map.player.readDevices = false;
            ui = map.GameUi;
            if (ui.IsPaused) ui.Resume();
            Check(HplDebugSession.Active && HplDebugSession.EnemiesPaused && map.disableAutoSaves, "Test flags did not survive scene activation");
            Check(HplGame.Items.Count(i => i.name.StartsWith("guiding_rod")) == 3 && HplGame.HasType("Lantern"), "Progression equipment missing after actual load");
            Check(HplGame.Items.Single(i => i.type == "LampOil").count == 5 && map.player.tinderboxes == 15, "Supplies missing after actual load");
            map.player.readDevices = true;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Backquote));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            Check(ui.DebugOpen && ui.IsPaused && map.player.UiInputBlocked && Time.timeScale == 0, "Grave key failed to pause gameplay");
            yield return new WaitForSecondsRealtime(0.4f);
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/DebugPanel.png"));
            yield return new WaitForSecondsRealtime(0.5f);
            Check(ui.BeginDebugLoad(destination), "Reloading the same map rejected");
            deadline = Time.realtimeSinceStartupAsDouble + 90;
            while (HplMap.Current == null || HplMap.Current == map)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline) throw new InvalidOperationException("Same-map reload timed out");
                yield return null;
            }
            yield return null;
            map = HplMap.Current;
            map.player.readDevices = false;
            ui = map.GameUi;
            if (ui.IsPaused) ui.Resume();
            Check(HplGame.Items.Count(i => i.name.StartsWith("guiding_rod")) == 3, "Same-map reload duplicated loadout");
            Check(ui.OpenDebugPanel() && ui.BeginDebugLoad(stages.Single(s => s.MapName == "02_library")), "Cross-campaign teleport rejected");
            deadline = Time.realtimeSinceStartupAsDouble + 90;
            while (HplMap.Current == null || HplMap.Current == map)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline) throw new InvalidOperationException("Justine teleport timed out");
                yield return null;
            }
            yield return null;
            map = HplMap.Current;
            map.player.readDevices = false;
            Check(map.Campaign.IsJustine && HplGame.Items.All(i => i.type != "Puzzle"), "Justine received old campaign puzzle items");
            Check(HplGame.HasType("Lantern") && HplGame.Items.Single(i => i.type == "LampOil").count == 5, "Justine starter supplies missing");
            map.player.Lantern.SetActive(true, true);
            Check(map.player.lanternActive, "Justine test lantern is unusable");
            map.player.Lantern.SetActive(false, true);
            yield return new WaitForSecondsRealtime(1.5f);

            yield return Tabs(stages);
            map = HplMap.Current;
            ui = map.GameUi;
            map.player.readDevices = false;

            // The lantern lit with its key (F), then teleports to other maps: only the current player's hands stay.
            // A player build showed the old player's hands with the lit lantern standing in the new map.
            int strays = 0;
            void OnLog(string message, string stack, LogType type)
            {
                if (message.Contains("Stray player hands")) { strays++; Debug.Log("[DebugPanelCheck] " + message); }
            }
            Application.logMessageReceived += OnLog;
            try
            {
                foreach (var next in new[] { "01_old_archives", "02_entrance_hall" })
                {
                    yield return PressLantern(map);
                    Check(map.player.lanternActive && map.player.Hands != null && map.player.Hands.CurrentHandObject == "lantern",
                          $"F did not light the lantern on {map.MapName}");
                    var before = map;
                    ui = map.GameUi;
                    Check(ui.OpenDebugPanel() && ui.BeginDebugLoad(stages.First(s => s.MapName == next)), "Teleport to " + next + " rejected");
                    deadline = Time.realtimeSinceStartupAsDouble + 90;
                    while (HplMap.Current == null || HplMap.Current == before || HplMap.Current.MapName != next)
                    {
                        if (Time.realtimeSinceStartupAsDouble >= deadline) throw new InvalidOperationException("Teleport to " + next + " timed out");
                        yield return null;
                    }
                    yield return new WaitForSecondsRealtime(1f);
                    map = HplMap.Current;
                    map.player.readDevices = false;
                    if (map.GameUi.IsPaused) map.GameUi.Resume();
                    var all = Array.FindAll(Resources.FindObjectsOfTypeAll<Player.HplHandsData>(), h => h.gameObject.scene.IsValid());
                    string list = string.Join(", ", all.Select(h => $"{h.gameObject.scene.name} model at {(h.hands != null ? h.hands.position : h.transform.position)} current={map.player.Hands != null && h.transform == map.player.Hands.Root}"));
                    Debug.Log($"[DebugPanelCheck] hands on {next}: {all.Length} ({list}), stray copies removed so far {strays}");
                    Check(strays == 0 && all.Length == 1 && map.player.Hands != null && all[0].transform == map.player.Hands.Root,
                          $"Hands copies on {next}: {all.Length} ({list}), stray copies removed {strays}");
                }
            }
            finally { Application.logMessageReceived -= OnLog; }
        }

        /// <summary>Every tab on 03_archives (screenshots), a teleport to the key and a snapshot loaded back.</summary>
        IEnumerator Tabs(System.Collections.Generic.List<HplDebugProgression.Stage> stages)
        {
            var map = HplMap.Current;
            Check(map.GameUi.OpenDebugPanel() && map.GameUi.BeginDebugLoad(stages.Single(s => s.MapName == "03_archives")), "Teleport to 03 rejected");
            yield return WaitMap(map, "03_archives");
            map = HplMap.Current;
            var ui = map.GameUi;
            string folder = Path.GetFullPath("Logs/DebugSnapshotPlayCheck");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            HplDebugSnapshots.FolderOverride = folder;
            var world = ui.DebugPanel.World;
            Check(world.Teleport(world.Points().Single(p => p.Name == "key_study_1"), out _), "Key teleport");
            yield return new WaitForSecondsRealtime(2f);
            Vector3 atKey = map.player.Body.FeetPosition;
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/DebugPanel_at_key.png"));
            yield return new WaitForSecondsRealtime(0.5f);
            Check(HplDebugSnapshots.Create(map, "у ключа") != null, "Snapshot at the key");
            Check(ui.OpenDebugPanel(), "Panel on 03");
            foreach (HplDebugPanel.Page page in Enum.GetValues(typeof(HplDebugPanel.Page)))
            {
                ui.DebugPanel.CurrentPage = page;
                yield return new WaitForSecondsRealtime(0.5f);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath($"Logs/DebugPanel_{page}.png"));
                yield return new WaitForSecondsRealtime(0.5f);
            }
            ui.DebugPanel.CurrentPage = HplDebugPanel.Page.Points;
            Check(world.PickUp(world.Prop("key_study_1")), "Key pickup in play mode");
            var before = map;
            Check(ui.BeginDebugSnapshot(HplDebugSnapshots.List().Single().File), "Snapshot load rejected");
            yield return WaitMap(before, "03_archives");
            map = HplMap.Current;
            Check(HplDebugSession.Active && map.disableAutoSaves, "A loaded snapshot must be a test session");
            Check(map.GetEntityByName("key_study_1") != null && HplGame.GetItem("key_study_1") == null &&
                  !HplGame.GlobalVars.ContainsKey("key_study_1"), "Snapshot did not restore the key");
            Check(Vector3.Distance(map.player.Body.FeetPosition, atKey) < 0.5f, "Snapshot did not restore the position");
            HplDebugSnapshots.FolderOverride = null;
            Directory.Delete(folder, true);
        }

        IEnumerator WaitMap(HplMap previous, string name)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 90;
            while (HplMap.Current == null || HplMap.Current == previous || HplMap.Current.MapName != name)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline) throw new InvalidOperationException("Load of " + name + " timed out");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1f);
            HplMap.Current.player.readDevices = false;
            if (HplMap.Current.GameUi.IsPaused) HplMap.Current.GameUi.Resume();
        }

        /// <summary>The lantern key (F) of the test keyboard, then its draw animation.</summary>
        IEnumerator PressLantern(HplMap map)
        {
            map.player.readDevices = true;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.F));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(1.5f);
            map.player.readDevices = false;
        }

        void Finish(int code)
        {
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_inputSettings != null)
            {
                _inputSettings.backgroundBehavior = _backgroundBehavior;
                _inputSettings.editorInputBehaviorInPlayMode = _editorBehavior;
            }
            if (_prefs == null) PlayerPrefs.DeleteKey(HplUserSettings.PrefsKey);
            else PlayerPrefs.SetString(HplUserSettings.PrefsKey, _prefs);
            PlayerPrefs.Save();
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
