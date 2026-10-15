#if UNITY_EDITOR
using System.Collections;
using System.IO;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Editor-only Play-mode screenshot probe; excluded from player builds.</summary>
    public sealed class HplMenuPreviewProbe : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            var map = HplMap.Current;
            if (map == null || map.GameUi == null) { Finish(1); yield break; }
            map.effects.ResetAll();
            map.GameUi.Pause();
            // Batch has no Game view framebuffer. Render the real player loop and UI into a camera target.
            var camera = map.PlayerCamera;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            camera.targetTexture = target;
            Canvas menuCanvas = null;
            foreach (var canvas in map.GetComponentsInChildren<Canvas>())
                if (canvas.name == "HPL game menu") menuCanvas = canvas;
            if (menuCanvas == null) { Finish(1); yield break; }
            menuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            menuCanvas.worldCamera = camera;
            menuCanvas.planeDistance = 1f;
            // Keep UI out of HDRP's scene capture; runtime ScreenSpaceOverlay likewise renders after effects.
            foreach (var child in menuCanvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            camera.cullingMask &= ~(1 << 31);
            yield return new WaitForSecondsRealtime(1.3f);
            Canvas.ForceUpdateCanvases();
            int vertices = 0;
            foreach (var text in map.GetComponentsInChildren<HplGuiText>())
            {
                var mesh = text.canvasRenderer != null ? text.canvasRenderer.GetMesh() : null;
                if (mesh != null) vertices += mesh.vertexCount;
            }
            if (vertices == 0) { Debug.LogError("Menu has no rendered text geometry"); Finish(1); yield break; }
            string folder = Path.GetFullPath("Logs/MenuCheck");
            Directory.CreateDirectory(folder);
            string pausePath = Path.Combine(folder, "Pause.png");
            Capture(target, pausePath, camera, menuCanvas);
            map.GameUi.OpenOptions();
            yield return new WaitForSecondsRealtime(0.3f);
            string optionsPath = Path.Combine(folder, "Options.png");
            Capture(target, optionsPath, camera, menuCanvas);
            for (int tab = 1; tab < 4; tab++)
            {
                map.GameUi.OpenOptions(tab);
                yield return new WaitForSecondsRealtime(0.2f);
                Capture(target, Path.Combine(folder, $"Options_{tab}.png"), camera, menuCanvas);
            }
            Debug.Log($"PASS: Play-mode menu, {vertices} text vertices; screenshots {folder}");
            map.GameUi.Resume();
            camera.targetTexture = null;
            target.Release();
            Destroy(target);
            Finish(0);
        }

        static void Capture(RenderTexture target, string path, Camera camera, Canvas menuCanvas)
        {
            // Batch has no overlay framebuffer. The existing snapshot helper composites the real Canvas
            // meshes / font atlases after the GPU-rendered world, just as it does for HUD snapshots.
            Canvas.ForceUpdateCanvases();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            System.Type.GetType("RTXMnesia.EditorDev.HudSnapshot, Assembly-CSharp-Editor")
                .GetMethod("DrawCanvas").Invoke(null, new object[] { image, menuCanvas, camera });
            File.WriteAllBytes(path, image.EncodeToPNG());
            Destroy(image);
            RenderTexture.active = previous;
        }

        static void Finish(int code)
        {
            UnityEditor.SessionState.SetInt("RTXMnesia.MenuPreview.ExitCode", code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
