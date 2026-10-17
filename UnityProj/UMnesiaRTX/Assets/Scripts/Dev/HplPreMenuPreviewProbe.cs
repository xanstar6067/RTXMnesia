#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play check of the pre menu on the real startup scene (RTXMnesia.EditorDev.PreMenuPreview): a
    /// first start — logos, welcome texts with music, the gamma section (slider to 2.0 and back), Continue to the
    /// title screen; snapshots in Logs/PreMenuCheck. The first start flag and the user settings are restored.
    /// </summary>
    public sealed class HplPreMenuPreviewProbe : MonoBehaviour
    {
        public const string PendingKey = "RTXMnesia.PreMenuPreview.Pending", ExitCodeKey = "RTXMnesia.PreMenuPreview.ExitCode";
        const string FirstStartKey = "RTXMnesia.FirstStartDone", SettingsKey = "RTXMnesia.UserSettings.v1";

        static int s_savedFirstStart;
        static string s_savedSettings;

        int _checks;
        Camera _camera;
        RenderTexture _target;
        string Folder => Path.GetFullPath("Logs/PreMenuCheck");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Prepare()
        {
            if (!UnityEditor.SessionState.GetBool(PendingKey, false)) return;
            // A first start: the welcome sections show; the user's flag and settings come back afterwards.
            s_savedFirstStart = PlayerPrefs.GetInt(FirstStartKey, 0);
            s_savedSettings = PlayerPrefs.HasKey(SettingsKey) ? PlayerPrefs.GetString(SettingsKey) : null;
            PlayerPrefs.DeleteKey(FirstStartKey);
            HplPreMenu.AllowInBatchMode = true;
            var go = new GameObject("Pre menu verification");
            DontDestroyOnLoad(go);
            go.AddComponent<HplPreMenuPreviewProbe>();
        }

        IEnumerator Start()
        {
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
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }

        IEnumerator WaitFor(Func<bool> condition, float seconds, string message)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartupAsDouble > deadline) throw new InvalidOperationException("Timed out: " + message);
                yield return null;
            }
        }

        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);
            HplMainMenu title = null;
            yield return WaitFor(() => (title = HplMap.Current != null ? HplMap.Current.GetComponent<HplMainMenu>() : null) != null && title.PreMenu != null, 10f, "pre menu start");
            var map = HplMap.Current;
            var pre = title.PreMenu;
            Check(title.Ui == null, "the title screen waits for the pre menu");
            Check(HplPreMenu.IsFirstStart, "first start flag cleared for the check");

            yield return WaitFor(() => pre.SectionIndex == 0 && pre.StateName == "ShowSection", 5f, "first logo");
            Check(pre.CurrentSection.image != null && pre.AlphaFade == 0f, "logo section shown after the fade in");
            CaptureSetup(map.PlayerCamera, pre.Canvas);
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("PreMenu_Logo.png");
            yield return WaitFor(() => pre.SectionIndex == 1, 4f, "the logo moves on by itself after 2 s");
            pre.ButtonPressed();
            yield return WaitFor(() => pre.SectionIndex == 2 && pre.StateName == "ShowSection", 5f, "first welcome section");
            Check(map.sound.Music.CurrentSongName == "12_amb.ogg", $"welcome music 12_amb.ogg ({map.sound.Music.CurrentSongName})");
            yield return new WaitForSecondsRealtime(2.5f);
            Check(pre.SectionIndex == 2, "a text section waits for Continue");
            Capture("PreMenu_Welcome.png");
            pre.ContinuePressed();
            yield return WaitFor(() => pre.SectionIndex == 3 && pre.StateName == "ShowSection", 5f, "welcome 2");
            pre.ContinuePressed();
            yield return WaitFor(() => pre.SectionIndex == 4 && pre.StateName == "ShowSection", 5f, "welcome 3");
            pre.ContinuePressed();
            yield return WaitFor(() => pre.SectionIndex == 5 && pre.StateName == "ShowSection", 5f, "welcome 4");
            pre.ContinuePressed();
            yield return WaitFor(() => pre.SectionIndex == 6 && pre.StateName == "ShowSection", 5f, "gamma section");
            Check(pre.CurrentSection.hasGammaSettings, "the gamma section");
            float gamma0 = pre.GammaValue;
            yield return new WaitForSecondsRealtime(0.2f);
            Capture("PreMenu_Gamma.png");
            pre.SetGammaSlider(1000);
            Check(Mathf.Approximately(HplUserSettings.Gamma, 2f) && Mathf.Approximately(HplUserSettings.SavedGamma(), 2f), "slider to the max: gamma 2.0 applied and saved");
            yield return new WaitForSecondsRealtime(0.2f); // the frame read is the previous render
            UnityEngine.UI.RawImage Handle() => Array.Find(pre.Canvas.GetComponentsInChildren<UnityEngine.UI.RawImage>(),
                i => i.enabled && i.texture != null && i.texture.name.Contains("slider_handle_bg"));
            var preview = Array.Find(pre.Canvas.GetComponentsInChildren<UnityEngine.UI.RawImage>(), i => i.name == "Gamma preview");
            var handleMax = Handle();
            Check(handleMax != null && preview != null && preview.material != null && Mathf.Approximately(preview.material.GetFloat("_HplGamma"), 2f),
                  "the slider handle is drawn and the preview material gets gamma 2.0");
            float xMax = handleMax.rectTransform.anchoredPosition.x;
            Capture("PreMenu_Gamma2.png");
            pre.SetGammaSlider(Mathf.RoundToInt((gamma0 - 0.3f) / 0.05f));
            Check(Mathf.Abs(HplUserSettings.Gamma - gamma0) < 0.01f, "slider back");
            yield return null;
            yield return null;
            Check(Handle() != null && Handle().rectTransform.anchoredPosition.x < xMax - 1f, "the handle moves with the value");
            pre.ContinuePressed();
            yield return WaitFor(() => pre.SectionIndex == 7 && pre.StateName == "ShowSection", 5f, "last welcome");
            CaptureCleanup();
            pre.ContinuePressed();
            yield return WaitFor(() => title.Ui != null, 5f, "title screen after the pre menu");
            Check(!HplPreMenu.IsFirstStart && HplPreMenu.ShownThisRun, "first start flag raised, shown once this run");
            Check(map.sound.Music.CurrentSongName == "game_menu.ogg" && title.Ui.IsTitleScreen, "title music and menu");
            string report = $"PASS: {_checks} Play-mode pre menu checks; screenshots {Folder}";
            File.WriteAllText(Path.Combine(Folder, "PlayTests.txt"), report);
            Debug.Log(report);
        }

        void CaptureSetup(Camera camera, Canvas canvas)
        {
            _camera = camera;
            _canvas = canvas;
            _target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _target.Create();
            camera.targetTexture = _target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            camera.cullingMask &= ~(1 << 31);
        }

        Canvas _canvas;

        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            var image = new Texture2D(_target.width, _target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0);
            image.Apply();
            Type.GetType("RTXMnesia.EditorDev.HudSnapshot, Assembly-CSharp-Editor").GetMethod("DrawCanvas").Invoke(null, new object[] { image, _canvas, _camera });
            File.WriteAllBytes(Path.Combine(Folder, name), image.EncodeToPNG());
            Destroy(image);
            RenderTexture.active = previous;
        }

        void CaptureCleanup()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_target != null) { _target.Release(); Destroy(_target); }
            _target = null;
        }

        void Finish(int code)
        {
            CaptureCleanup();
            PlayerPrefs.SetInt(FirstStartKey, s_savedFirstStart);
            if (s_savedFirstStart == 0) PlayerPrefs.DeleteKey(FirstStartKey);
            if (s_savedSettings != null) PlayerPrefs.SetString(SettingsKey, s_savedSettings);
            else PlayerPrefs.DeleteKey(SettingsKey);
            PlayerPrefs.Save();
            HplPreMenu.AllowInBatchMode = false;
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
