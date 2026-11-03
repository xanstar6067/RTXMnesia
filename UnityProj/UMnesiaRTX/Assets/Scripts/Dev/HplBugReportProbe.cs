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
                Check(report.Contains("Снимок: нет ("), "No screenshot and the report does not say why");
                Debug.Log("[BugReportCheck] no screenshot in this run (batch editor)");
            }
            string log = File.ReadAllText(Path.Combine(folder, "log.txt"));
            Check(log.Contains("[W] [BugReportCheck] log marker before F1"), "log.txt lacks the recent messages: " + log);
            Check(File.Exists(Path.Combine(folder, "save.sav")), "save.sav missing");
            var position = map.player.Body.FeetPosition;

            // Stepped here, not as a nested coroutine: its failed checks must reach Start's handler.
            for (var capture = PassCapture(map); capture.MoveNext();) yield return capture.Current;

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

        /// <summary>
        /// The HDR output path (HplScreenEffectsPass), forced in SDR: output size, not black, and the same way up as a
        /// ScreenCapture or ReadPixels reference of the same view (the live display path is checked by hand).
        /// </summary>
        IEnumerator PassCapture(HplMap map)
        {
            var plain = HplBugReport.Begin(this, map);
            for (double end = Time.realtimeSinceStartupAsDouble + 3; !plain.Ready && Time.realtimeSinceStartupAsDouble < end;) yield return null;
            HplBugReport.ForcePassCapture = true;
            HplBugReport pass;
            try { pass = HplBugReport.Begin(this, map); }
            finally { HplBugReport.ForcePassCapture = false; }
            // A batch editor draws no game frames on its own: the camera is rendered by hand there.
            for (double end = Time.realtimeSinceStartupAsDouble + 4; !pass.Ready && Time.realtimeSinceStartupAsDouble < end;)
            {
                if (Application.isBatchMode && map.PlayerCamera != null) map.PlayerCamera.Render();
                yield return null;
            }
            try
            {
                var shot = pass.Screenshot;
                Check(pass.Ready && shot != null, "The effects pass gave no screenshot: " + pass.ScreenshotNote);
                Check(shot.width == Screen.width && shot.height == Screen.height, $"Pass screenshot {shot.width}x{shot.height}, screen {Screen.width}x{Screen.height}");
                float[] grid = Grid(shot);
                // The start of 00_rainy_hall is dark: an image is told from black by its brightest pixels (a candle), not the mean.
                float peak = shot.GetPixels32().Max(c => Mathf.Max(c.r, Mathf.Max(c.g, c.b))) / 255f;
                Check(peak > 0.25f, $"The pass screenshot is black: brightest {peak:0.000}, mean {grid.Average():0.000}");
                Debug.Log($"[BugReportCheck] pass screenshot {shot.width}x{shot.height}, mean {grid.Average():0.000}, brightest {peak:0.000}, note '{pass.ScreenshotNote}'");
                // The reference the right way up: ScreenCapture, or (batch editor: none) the same view rendered into a
                // texture and read with ReadPixels, which orders rows as a Texture2D.
                var reference = plain.Screenshot != null ? plain.Screenshot : RenderReference(map.PlayerCamera, shot.width, shot.height);
                string source = plain.Screenshot != null ? "ScreenCapture" : "ReadPixels";
                try
                {
                    float[] expected = Grid(reference);
                    float same = Correlation(grid, expected), flipped = Correlation(Flip(grid), expected);
                    Debug.Log($"[BugReportCheck] pass vs {source}: correlation {same:0.000}, flipped {flipped:0.000}");
                    Check(same > flipped && same > 0.5f, $"The pass screenshot does not match {source} (correlation {same:0.000}, flipped {flipped:0.000})");
                }
                finally { if (reference != plain.Screenshot) Destroy(reference); }
            }
            finally { plain.Dispose(); pass.Dispose(); }
        }

        static Texture2D RenderReference(Camera camera, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = camera.targetTexture;
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply(false);
                return texture;
            }
            finally
            {
                camera.targetTexture = previous;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        const int GridSize = 8;

        /// <summary>Mean luminance of 8 x 8 cells, bottom row first.</summary>
        static float[] Grid(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var sums = new float[GridSize * GridSize];
            var counts = new int[sums.Length];
            for (int y = 0; y < texture.height; y += 2)
            for (int x = 0; x < texture.width; x += 2)
            {
                var c = pixels[y * texture.width + x];
                int cell = y * GridSize / texture.height * GridSize + x * GridSize / texture.width;
                sums[cell] += (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
                counts[cell]++;
            }
            for (int i = 0; i < sums.Length; i++) sums[i] /= Math.Max(1, counts[i]);
            return sums;
        }

        static float[] Flip(float[] grid)
        {
            var flipped = new float[grid.Length];
            for (int y = 0; y < GridSize; y++) Array.Copy(grid, y * GridSize, flipped, (GridSize - 1 - y) * GridSize, GridSize);
            return flipped;
        }

        static float Correlation(float[] a, float[] b)
        {
            float ma = a.Average(), mb = b.Average(), ab = 0, aa = 0, bb = 0;
            for (int i = 0; i < a.Length; i++) { ab += (a[i] - ma) * (b[i] - mb); aa += (a[i] - ma) * (a[i] - ma); bb += (b[i] - mb) * (b[i] - mb); }
            return aa > 0 && bb > 0 ? ab / Mathf.Sqrt(aa * bb) : 0;
        }

        void Finish(int code)
        {
            HplBugReport.RootOverride = null;
            HplBugReport.ForcePassCapture = false;
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
