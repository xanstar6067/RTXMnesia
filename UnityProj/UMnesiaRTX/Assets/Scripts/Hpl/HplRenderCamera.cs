using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// One camera renders the game for the whole run. Each map scene and the title have their own camera (the player's,
    /// the menu's); with DLSS and many shadow maps every new rendering camera kept 100-500 MB of video memory that
    /// nothing in Unity sees and that never came back after the scene was unloaded: about 100 MB per map, the title
    /// on a 4 GB laptop fell from 120 to 42 fps after one benchmark tour. The scene cameras stay for the game logic
    /// (their transform, field of view, the audio listener), but they are switched off as soon as they are on, and this
    /// camera copies the newest of them every frame before rendering. A new source resets the history (TAA, DLSS).
    /// </summary>
    public static class HplRenderCamera
    {
        static Camera s_camera;
        static HDAdditionalCameraData s_data;
        static Camera s_source;
        /// <summary>The scene cameras taken over, oldest first: they stay switched off, so a scene shown again (the title
        /// after the benchmark) does not bring its camera back on by itself.</summary>
        static readonly System.Collections.Generic.List<Camera> s_sources = new();
        static Camera[] s_all = new Camera[8];

        /// <summary>The scene camera this one renders for (null: none, this camera is off).</summary>
        public static Camera Source => s_source;
        /// <summary>The camera that renders the game (null before the first scene camera).</summary>
        public static Camera Camera => s_camera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Startup()
        {
            s_camera = null; s_data = null; s_source = null;
            s_sources.Clear();
            Application.onBeforeRender -= OnBeforeRender;
            Application.onBeforeRender += OnBeforeRender;
        }

        static bool IsSource(Camera c) =>
            c != s_camera && c.cameraType == CameraType.Game && c.targetTexture == null && !HplWaterReflection.IsReflectionCamera(c);

        static void OnBeforeRender()
        {
            if (Camera.allCamerasCount > s_all.Length) s_all = new Camera[Camera.allCamerasCount * 2];
            int count = Camera.GetAllCameras(s_all);
            for (int i = 0; i < count; i++)
            {
                var c = s_all[i];
                if (!IsSource(c)) continue;
                c.enabled = false;
                s_sources.Remove(c);
                s_sources.Add(c);
            }
            System.Array.Clear(s_all, 0, count);
            // The newest one in an active scene object; a hidden or unloaded one gives way to the one before.
            Camera source = null;
            for (int i = s_sources.Count - 1; i >= 0 && source == null; i--)
            {
                if (s_sources[i] == null) s_sources.RemoveAt(i);
                else if (s_sources[i].gameObject.activeInHierarchy) source = s_sources[i];
            }
            if (source != null && source != s_source) Adopt(source);
            s_source = source;
            bool on = source != null;
            if (s_camera != null && s_camera.enabled != on) s_camera.enabled = on;
            if (!on) return;
            s_camera.CopyFrom(s_source);
            s_camera.enabled = true;
        }

        static void Adopt(Camera source)
        {
            if (s_camera == null)
            {
                var go = new GameObject("HPL render camera") { tag = "MainCamera" };
                Object.DontDestroyOnLoad(go);
                s_camera = go.AddComponent<Camera>();
                s_data = go.AddComponent<HDAdditionalCameraData>();
            }
            // Per-frame settings (DLSS, ray tracing, contact shadows) are set on this camera by HplGraphics.
            if (source.TryGetComponent(out HDAdditionalCameraData data)) data.CopyTo(s_data);
            s_camera.CopyFrom(source);
            s_camera.enabled = true;
            HDCamera.GetOrCreate(s_camera).Reset();
        }
    }
}
