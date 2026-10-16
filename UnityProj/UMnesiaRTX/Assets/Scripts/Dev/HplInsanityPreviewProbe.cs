#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using RTXMnesia.Effects;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Editor-only Play snapshots of insanity events: bugs, the red fade of a roar, Daniel's subtitle.</summary>
    public sealed class HplInsanityPreviewProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.InsanityPreview.ExitCode";
        string Folder => Path.GetFullPath("Logs/InsanityCheck");

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

        IEnumerator Run()
        {
            Directory.CreateDirectory(Folder);
            yield return new WaitForSecondsRealtime(1f);
            var map = HplMap.Current;
            if (map == null || map.player == null) throw new InvalidOperationException("no map");
            map.player.readDevices = false;
            if (map.GameUi != null && map.GameUi.IsPaused) map.GameUi.Resume();
            map.runScript = false;
            map.effects.Fade.FadeIn(0f);
            map.player.active = true;
            map.player.sanity = 40f;
            HplInsanity.ResetSession();
            yield return new WaitForSecondsRealtime(0.5f);

            HplInsanity.StartEvent(map, "Bugs_Swarm");
            yield return new WaitForSecondsRealtime(5f);
            Capture(map, "Bugs.png");
            HplInsanity.StopCurrentEvent();

            HplInsanity.StartEvent(map, "Stream_Monster_Roar01");
            yield return new WaitForSecondsRealtime(2.5f);
            Capture(map, "Roar.png");
            HplInsanity.StopCurrentEvent();

            HplInsanity.StartEvent(map, "Stream_Voice_Daniel_Ch01_01");
            yield return new WaitForSecondsRealtime(1f);
            Capture(map, "Ramble.png");
            HplInsanity.StopCurrentEvent();
            HplInsanity.ResetSession();
            Debug.Log("Insanity preview: screenshots " + Folder);
        }

        /// <summary>
        /// The player camera with the HUD canvas drawn by it (the overlay is not rendered in batch Play). HDRP draws a
        /// screen space camera canvas into its own UI buffer, so modulative images (the roar's fade) show their color
        /// instead of multiplying the scene here; in the game the canvas is an overlay drawn on the frame.
        /// </summary>
        void Capture(HplMap map, string name)
        {
            var camera = map.PlayerCamera;
            var canvas = map.hud.Canvas;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.01f;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            map.hud.Refresh();
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
            canvas.renderMode = mode;
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
