#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play check of the credits (RTXMnesia.EditorDev.CreditsPreview): StartCredits from the level script,
    /// the stopped map and ending song, the scroll (fast-forwarded), the song stopping near the end, the fades to
    /// "The End" with the ending's code, Exit back to the title screen with the game reset. Snapshots in Logs/CreditsCheck.
    /// </summary>
    public sealed class HplCreditsPreviewProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.CreditsPreview.ExitCode";
        string Folder => Path.GetFullPath("Logs/CreditsCheck");
        int _checks;

        IEnumerator Start()
        {
            // The probe goes on in the main menu scene.
            DontDestroyOnLoad(gameObject);
            var run = Run();
            while (true)
            {
                bool more = false, failed = false; object current = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; }
                if (failed) { Finish(1); yield break; }
                if (!more) break;
                yield return current;
            }
            Finish(0);
        }

        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FAIL: " + message);
            _checks++;
            Debug.Log("[CreditsCheck] ok: " + message);
        }

        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);
            yield return new WaitForSecondsRealtime(1f);
            var map = HplMap.Current;
            if (map == null || map.player == null || map.Script == null) throw new InvalidOperationException("no map with a level script");
            map.player.readDevices = false;
            if (map.GameUi != null && map.GameUi.IsPaused) map.GameUi.Resume();
            map.effects.Fade.FadeIn(0f);
            string menuScene = map.Campaign.MenuScene;
            HplGame.SetGlobalVar("credits_reset_probe", "1");
            yield return new WaitForSecondsRealtime(0.3f);

            // As 29_orb_chamber's Daniel ending (StartCredits("ending_daniel.ogg", false, "Ending", "MainCredits", 2)).
            typeof(HplScript).GetMethod("StartCredits", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(map.Script, new object[] { "ending_daniel.ogg", false, "Ending", "MainCredits", 2 });
            var credits = HplCredits.Current;
            Check(credits != null && HplCredits.Active, "StartCredits shows the credits");
            Check(credits.RowCount > 100, $"Ending/MainCredits word wrapped ({credits.RowCount} rows)");
            yield return null;
            float gameTime = map.GameTime;
            Check(map.IsMenuPaused && Time.timeScale == 0 && map.player.UiInputBlocked, "the map stops (Credits container)");
            Check(map.sound.Music.CurrentSongName == "ending_daniel.ogg", $"the ending song plays ({map.sound.Music.CurrentSongName})");
            yield return new WaitForSecondsRealtime(1f);
            credits.ExitPressed();
            Check(credits.State == 0, "Exit is ignored in the first 3 s");
            yield return new WaitForSecondsRealtime(6f);
            Check(credits.YPos < 600f - 6f * 25f && map.GameTime == gameTime, $"the text scrolls (y {credits.YPos:0}) while the map stands");
            Capture(map, credits, "Scroll.png");

            // Fast forward to the end of the text: the song stops near the end, then the fade out starts.
            while (credits.State == 0) credits.Step(0.25f);
            Check(credits.State == 1 && credits.YPos < -1000f, $"the scroll ends at its last row (y {credits.YPos:0})");
            Check(map.sound.Music.CurrentSongName == "", "the song stops near the end of the text");
            float wait = Time.realtimeSinceStartup + 10f;
            while (credits.State < 3 && Time.realtimeSinceStartup < wait) yield return null;
            Check(credits.State == 3 && credits.FadeAlpha == 0f, "fade out and in to The End (FadeSpeed 0.5)");
            yield return new WaitForSecondsRealtime(0.5f);
            Capture(map, credits, "TheEnd.png");
            // mfTime runs since the fade out began (4 s of fades): The End can be left at once.
            credits.ExitPressed();
            Check(credits.State == 4, "Exit leaves The End (its 3 s counted from the fade out, as in HPL2)");

            wait = Time.realtimeSinceStartup + 60f;
            while (SceneManager.GetActiveScene().path != menuScene && Time.realtimeSinceStartup < wait) yield return null;
            yield return null; yield return null;
            var title = HplMap.Current;
            var mainMenu = title != null ? title.GetComponent<HplMainMenu>() : null;
            var ui = mainMenu != null ? mainMenu.Ui : null;
            Check(SceneManager.GetActiveScene().path == menuScene && ui != null && ui.IsTitleScreen && Time.timeScale == 1, "the last fade returns to the main menu");
            Check(!HplCredits.Active && !HplGame.GlobalVars.ContainsKey("credits_reset_probe"), "the game is reset, the credits gone");
            Debug.Log($"PASS: {_checks} credits checks (start, stopped map, song, scroll, end of text, fades, The End, main menu, reset); screenshots {Folder}");
        }

        /// <summary>The credits canvas drawn on the CPU over a camera frame (overlays are not rendered in batch Play).</summary>
        void Capture(HplMap map, HplCredits credits, string name)
        {
            var camera = map.PlayerCamera;
            var canvas = credits.GetComponent<Canvas>();
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            Type.GetType("RTXMnesia.EditorDev.HudSnapshot, Assembly-CSharp-Editor").GetMethod("DrawCanvas").Invoke(null, new object[] { image, canvas, camera });
            File.WriteAllBytes(Path.Combine(Folder, name), image.EncodeToPNG());
            Destroy(image);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            target.Release();
            Destroy(target);
        }

        void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
