using System;
using System.IO;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>Checks the pause lifecycle on synthetic objects, without importing game resources or changing a scene.</summary>
    public static class PauseTests
    {
        [MenuItem("Tools/RTXMnesia/Tests/Pause lifecycle")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run pause checks outside Play mode.");
            float originalTimeScale = Time.timeScale;
            var originalCursorLock = Cursor.lockState;
            bool originalCursorVisible = Cursor.visible;
            float originalVolume = AudioListener.volume;
            int originalVSync = QualitySettings.vSyncCount;
            var root = new GameObject("Pause checks") { hideFlags = HideFlags.HideAndDontSave };
            HplFont font = ScriptableObject.CreateInstance<HplFont>();
            font.lastChar = 1200;
            foreach (char letter in "Вернуться в игруНастройкиВыходRTXMnesia")
                font.glyphs.Add(new HplFont.Glyph { id = letter, size = Vector2.one, advance = 1f, uv = new Rect(0, 0, 1, 1) });
            HplSoundBank bank = ScriptableObject.CreateInstance<HplSoundBank>();
            AudioClip clip = AudioClip.Create("pause_test", 44100, 1, 44100, false);
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++;
            }
            try
            {
                var map = root.AddComponent<HplMap>();
                map.player = root.AddComponent<AmnesiaPlayer>();
                map.player.readDevices = false;
                map.player.active = false; // SetPlayerActive(false), e.g. during the opening cutscene.
                map.hud = root.AddComponent<HplHud>();
                map.hud.defaultFont = map.hud.hintsFont = map.hud.focusFont = font;
                map.hud.Init(map);
                map.sound = root.AddComponent<HplSoundHandler>();
                bank.clips.Add(new HplSoundBank.Clip { name = "pause_test", path = "pause_test", clip = clip });
                map.sound.bank = bank;
                var voice = map.sound.PlayGui("pause_test", true, 1f);
                var world = map.sound.PlayGui("pause_test", true, 1f, SoundEntryType.World);
                voice.SetPaused(true);

                var ui = root.AddComponent<HplGameUi>();
                ui.Init(map);
                // Build normally assigns this; no map geometry / script is needed for this fixture.
                typeof(HplMap).GetProperty(nameof(HplMap.GameUi)).SetValue(map, ui);
                Time.timeScale = 0.5f;
                map.player.held.forward = true;
                ui.Pause();
                Check(ui.IsPaused && map.IsMenuPaused && Time.timeScale == 0f, "World did not pause");
                double pausedRenderTime = ui.RenderTime;
                Check(ui.RenderTime == pausedRenderTime, "Image trail render clock advances during pause");
                Check(map.player.UiInputBlocked && !map.player.held.forward, "Input was not cleared and blocked");
                Check(!map.player.active, "Pause changed the script's player activation flag");
                Check(!map.hud.Canvas.enabled, "HUD is still visible over the menu");
                Check(Cursor.lockState == CursorLockMode.None, "Menu cursor locked");
                Canvas.ForceUpdateCanvases();
                int renderedGlyphs = 0;
                foreach (var text in root.GetComponentsInChildren<HplGuiText>())
                {
                    if (text.CommandCount == 0) continue;
                    Check(text.canvasRenderer != null, "Text has no CanvasRenderer");
                    var mesh = text.canvasRenderer.GetMesh();
                    if (mesh != null) renderedGlyphs += mesh.vertexCount;
                }
                Check(renderedGlyphs > 0, "Menu text did not produce any rendered glyph geometry");
                Check(world.IsPaused && voice.IsPaused && map.sound.Music.IsPaused, "Audio did not pause");
                var duringPause = map.sound.PlayGui("pause_test", true, 1f);
                Check(duringPause.IsPaused, "A sound created during pause was not paused");
                map.sound.Step(1f);
                Check(world.ElapsedTime == 0f && duringPause.ElapsedTime == 0f, "Sound clocks advanced during pause");
                map.Step(1f);
                map.StepBeforeSimulate(1f);
                map.StepAfterSimulate(1f);
                Check(map.GameTime == 0f && map.Script == null, "Explicit map steps ran during pause");
                ui.Pause(); // Idempotent: must not replace the saved time scale by zero.
                ui.Resume();
                Check(!ui.IsPaused && Time.timeScale == 0.5f, "Resume lost the previous time scale");
                Check(!map.player.UiInputBlocked && !map.player.active, "Resume bypassed script input lock");
                Check(map.hud.Canvas.enabled, "HUD was not restored");
                Check(!world.IsPaused && !duringPause.IsPaused && voice.IsPaused, "Resume lost independent sound pause reasons");
                Check(!map.sound.Music.IsPaused, "Music was not resumed");

                ui.Pause();
                float originalSensitivity = ui.Settings.mouseSensitivity;
                ui.OpenOptions();
                ui.DraftSettings.mouseSensitivity = 4.5f;
                Check(ui.Settings.mouseSensitivity == originalSensitivity, "Editing options modified applied settings");
                ui.Resume();
                Check(!ui.ShowingOptions && ui.Settings.mouseSensitivity == originalSensitivity, "Discarded options were applied");
                var invalidSettings = new HplUserSettings { mouseSensitivity = float.NaN, masterVolume = float.PositiveInfinity };
                invalidSettings.Validate();
                Check(invalidSettings.mouseSensitivity == 1f && invalidSettings.masterVolume == 1f, "Invalid preference values were not sanitized");
                var applied = new HplUserSettings { mouseSensitivity = 2.2f, masterVolume = 0.25f, invertMouse = true, subtitles = false, hints = false, crosshair = false, vSync = true };
                applied.Apply(map);
                Check(map.player.mouseSensitivity == 2.2f && map.player.invertMouse && !map.player.active, "Settings did not apply to input or changed the script lock");
                Check(!map.hud.showSubtitles && !map.hud.showHints && !map.hud.showCrossHair, "HUD settings were not applied");
                Check(AudioListener.volume == 0.25f && QualitySettings.vSyncCount == 1, "Audio / VSync settings were not applied");

                map.sound.Music.Pause();
                map.hud.Canvas.enabled = false;
                map.player.active = true;
                ui.Pause();
                ui.enabled = false;
                // Ordinary MonoBehaviour lifecycle messages do not run automatically in this edit-mode fixture.
                typeof(HplGameUi).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(ui, null);
                Check(Time.timeScale == 0.5f && !ui.IsPaused && !map.player.UiInputBlocked, "Disabling the UI left the game paused");
                Check(map.player.active && !map.hud.Canvas.enabled, "Disable did not preserve the prior player / HUD state");
                Check(map.sound.Music.IsPaused && voice.IsPaused && !world.IsPaused, "Disable lost the prior audio state");
                ui.Resume();
                Check(Time.timeScale == 0.5f, "Repeated Resume changed the time scale");

                string report = $"PASS: {checks} pause lifecycle checks (world, input, script lock, HUD, cursor, audio, disable cleanup).";
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/PauseTests.txt", report);
                Debug.Log(report);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(font);
                UnityEngine.Object.DestroyImmediate(bank);
                UnityEngine.Object.DestroyImmediate(clip);
                Time.timeScale = originalTimeScale;
                Cursor.lockState = originalCursorLock;
                Cursor.visible = originalCursorVisible;
                AudioListener.volume = originalVolume;
                QualitySettings.vSyncCount = originalVSync;
            }
        }

        public static void RunBatch()
        {
            try { Run(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
