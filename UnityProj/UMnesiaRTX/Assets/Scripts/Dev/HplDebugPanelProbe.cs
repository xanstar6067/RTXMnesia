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
