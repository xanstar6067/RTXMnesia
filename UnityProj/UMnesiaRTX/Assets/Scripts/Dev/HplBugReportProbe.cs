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
    /// <summary>Play-mode check of the F1 bug report (EditorDev.BugReportTests.RunPlayBatch).</summary>
    public sealed class HplBugReportProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.BugReportTests.PlayExitCode";
        const string ResultFile = "Logs/BugReportPlayCheck.txt", Folder = "Logs/BugReportCheck";
        Keyboard _keyboard;
        int _checks;
        InputSettings _inputSettings;
        InputSettings.BackgroundBehavior _backgroundBehavior;
        InputSettings.EditorInputBehaviorInPlayMode _editorBehavior;

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            _inputSettings = InputSystem.settings;
            _backgroundBehavior = _inputSettings.backgroundBehavior;
            _editorBehavior = _inputSettings.editorInputBehaviorInPlayMode;
            _inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _keyboard = InputSystem.AddDevice<Keyboard>("BugReportTestKeyboard");
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
            HplBugReport.RootOverride = Path.GetFullPath(Folder);
            var run = Run();
            while (true)
            {
                bool more = false, failed = false; object current = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    File.WriteAllText(Path.GetFullPath(ResultFile), $"FAIL after {_checks} checks: {ex}\n");
                    failed = true;
                }
                if (failed) { Finish(1); yield break; }
                if (!more) break;
                yield return current;
            }
            File.WriteAllText(Path.GetFullPath(ResultFile), $"PASS: {_checks} Play-mode bug report checks\n");
            Finish(0);
        }

        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            _checks++;
        }

        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
        }

        IEnumerator WaitMap(string name, HplMap previous)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 120;
            while (HplMap.Current == null || HplMap.Current == previous || HplMap.Current.MapName != name || HplLoadScreen.Active
                   || HplMap.Current.GameUi == null || HplMap.Current.GameUi.IsTransitioning)
            {
                if (Time.realtimeSinceStartupAsDouble >= deadline) throw new InvalidOperationException("Loading " + name + " timed out");
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2f);
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", HplBugReport.FolderName));
            Check(string.Equals(Path.GetFullPath(HplBugReport.Root), root, StringComparison.OrdinalIgnoreCase), "Reports must go next to the project in the editor: " + HplBugReport.Root);
            var title = FindAnyObjectByType<HplMainMenu>();
            Check(title != null && title.Ui != null, "Title screen not initialized");
            Check(title.Ui.BeginSceneLoad(HplGameUi.FirstLevelScene, true), "New game rejected");
            yield return WaitMap("00_rainy_hall", null);
            var map = HplMap.Current;
            var ui = map.GameUi;
            if (ui.IsPaused) ui.Resume();
            float timeScale = Time.timeScale;
            Check(!ui.IsPaused && map.player != null && map.player.readDevices, "Game not running after the new game");

            // F1 in the game: the window opens after the end-of-frame capture and pauses the world.
            Debug.LogWarning("[BugReportCheck] log marker before F1");
            yield return Press(HplBugReportView.OpenKey);
            yield return null;
            var view = ui.BugReportView;
            for (double end = Time.realtimeSinceStartupAsDouble + 3; ui.BugReportOpen && !view.IsOpen && Time.realtimeSinceStartupAsDouble < end;) yield return null;
            Check(ui.BugReportOpen && view.IsOpen, $"F1 did not open the report window (pending {ui.BugReportOpen}, open {view.IsOpen}, paused {ui.IsPaused}, " +
                  $"readDevices {map.player.readDevices}, blocked {map.player.UiInputBlocked}, F1 {_keyboard[HplBugReportView.OpenKey].isPressed}, current {Keyboard.current == _keyboard})");
            Check(ui.IsPaused && Time.timeScale == 0 && map.player.UiInputBlocked, "The report must pause the game");
            yield return Press(Key.Enter);
            Check(view.IsOpen, "An empty comment must not be accepted");
            InputSystem.QueueTextEvent(_keyboard, ' ');
            InputSystem.QueueTextEvent(_keyboard, ' ');
            yield return null; yield return null;
            Check(!view.CanConfirm && view.Comment.Length == 0, "Leading spaces must not count as a comment: '" + view.Comment + "'");
            InputSystem.QueueTextEvent(_keyboard, 'O');
            InputSystem.QueueTextEvent(_keyboard, 'k');
            yield return null; yield return null;
            Check(view.Comment == "Ok", "Typed characters did not arrive: '" + view.Comment + "'");
            yield return Press(Key.Backspace);
            Check(view.Comment == "O", "Backspace failed: '" + view.Comment + "'");
            yield return Press(Key.Backspace);
            view.Type("Проверка отчёта: текстура стены мерцает возле окна, " + new string('x', 600));
            Check(view.Comment.Length == HplBugReportView.MaxLength, "The comment must be limited to " + HplBugReportView.MaxLength);
            yield return Press(Key.Enter);
            Check(!ui.BugReportOpen && !view.IsOpen, "Enter did not save the report");
            Check(!ui.IsPaused && Time.timeScale == timeScale && !map.player.UiInputBlocked, "The game did not resume after the report");

            var folders = Directory.GetDirectories(Folder);
            Check(folders.Length == 1 && Path.GetFileName(folders[0]).EndsWith("_00_rainy_hall"), "One report folder expected: " + string.Join(", ", folders));
            string folder = folders[0];
            string report = File.ReadAllText(Path.Combine(folder, "report.txt"));
            Debug.Log("[BugReportCheck] report.txt:\n" + report);
            Check(report.StartsWith("Комментарий: Проверка отчёта:"), "The comment must open the report");
            foreach (var key in new[] { "Карта: 00_rainy_hall", "Позиция HPL (ноги): ", "Направление HPL: ", "Углы камеры HPL", "[Взгляд]", "[Графика]", "Сборка: ", "Снимок: ", "Сохранение: save.sav", "Лог: log.txt" })
                Check(report.Contains(key), "report.txt lacks '" + key + "'");
            // A batch editor has no screen to capture (ScreenCapture writes nothing there): the screenshot is checked
            // only when one was taken, and "none" must then be stated in the report.
            string jpg = Path.Combine(folder, "screen.jpg");
            if (File.Exists(jpg))
            {
                var shot = new Texture2D(2, 2);
                Check(report.Contains("Снимок: screen.jpg") && shot.LoadImage(File.ReadAllBytes(jpg)) && shot.width == Screen.width && shot.height == Screen.height,
                      $"Screenshot size {shot.width}x{shot.height}, screen {Screen.width}x{Screen.height}");
                Destroy(shot);
            }
            else
            {
                Check(report.Contains("Снимок: нет"), "No screenshot and the report does not say so");
                Debug.Log("[BugReportCheck] no screenshot in this run (batch editor)");
            }
            string log = File.ReadAllText(Path.Combine(folder, "log.txt"));
            Check(log.Contains("[W] [BugReportCheck] log marker before F1"), "log.txt lacks the recent messages: " + log);
            Check(File.Exists(Path.Combine(folder, "save.sav")), "save.sav missing");
            var position = map.player.Body.FeetPosition;

            // Over the pause menu: the menu's pause stays, Esc only closes the report, nothing is written.
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Press(Key.Escape);
            Check(ui.IsPaused && !ui.BugReportOpen, "Esc did not open the pause menu");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Press(HplBugReportView.OpenKey);
            for (double end = Time.realtimeSinceStartupAsDouble + 3; ui.BugReportOpen && !view.IsOpen && Time.realtimeSinceStartupAsDouble < end;) yield return null;
            Check(ui.BugReportOpen && view.IsOpen && ui.IsPaused, "F1 did not open the report over the pause menu");
            view.Type("отмена");
            yield return Press(Key.Escape);
            Check(!ui.BugReportOpen && ui.IsPaused && !ui.ReadingUiOpen, "Esc must cancel the report and keep the menu");
            Check(Directory.GetDirectories(Folder).Length == 1, "A cancelled report wrote a folder");
            ui.Resume();

            // The report's save restores the moment of the report.
            Check(ui.LoadSaveFile(Path.Combine(folder, "save.sav")), "The report's save was rejected");
            yield return WaitMap("00_rainy_hall", map);
            var loaded = HplMap.Current;
            float distance = Vector3.Distance(loaded.player.Body.FeetPosition, position);
            Check(distance < 0.5f, "The loaded save is " + distance + " m away from the report position");
        }

        void Finish(int code)
        {
            HplBugReport.RootOverride = null;
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_inputSettings != null)
            {
                _inputSettings.backgroundBehavior = _backgroundBehavior;
                _inputSettings.editorInputBehaviorInPlayMode = _editorBehavior;
            }
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
