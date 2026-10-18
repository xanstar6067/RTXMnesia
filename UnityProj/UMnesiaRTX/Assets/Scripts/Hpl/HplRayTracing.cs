using System.Collections.Generic;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// Hardware ray tracing (DXR) of the "Ray tracing" lighting (graphics tab), its effects chosen on the RTX tab.
    /// The pipeline asset supports ray tracing and screen space shadows (ConfigureHDRP); a camera traces rays only
    /// when its frame settings allow it, which this class sets on every game camera before rendering (water mirrors
    /// never trace). Ray-traced shadows: HDRP has a fixed number of screen space shadow slots, so each frame the map
    /// lights with shadows nearest to the camera (visible, camera inside or close to their range) get ray-traced
    /// shadows up to the chosen count; the others keep their shadow maps.
    /// </summary>
    public static class HplRayTracing
    {
        /// <summary>RT shadow choices: off, low, medium, high, ultra — lights traced per frame and rays per pixel.</summary>
        public static readonly int[] ShadowLights = { 0, 4, 8, 12, 16 };
        static readonly int[] ShadowSamples = { 0, 1, 1, 2, 4 };
        public const int ShadowsDefault = 2;
        /// <summary>The source radius of a shadowed light while it is traced (HPL2 lights have none; candles, lamps).</summary>
        public const float SourceRadius = 0.04f;

        /// <summary>The GPU, the graphics API (DirectX 12) and the pipeline trace rays.</summary>
        public static bool Supported => SystemInfo.supportsRayTracing &&
            (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp.rayTracingSupported : true);
        /// <summary>The "Ray tracing" lighting is chosen and runs.</summary>
        public static bool Active { get; private set; }
        /// <summary>Index of <see cref="ShadowLights"/>.</summary>
        public static int Shadows { get; private set; }
        /// <summary>The lights traced in the last frame, of the shadowed lights considered (RTX tab).</summary>
        public static int TracedLights { get; private set; }
        public static int ShadowedLights { get; private set; }

        static readonly List<(AmnesiaLight light, float score)> s_candidates = new();
        static readonly HashSet<AmnesiaLight> s_traced = new();
        static readonly Dictionary<Light, float> s_radius = new();
        static int s_frame = -1;

        public static void Apply(HplUserSettings s)
        {
            Active = s.lighting == 1 && SystemInfo.supportsRayTracing;
            Shadows = Active ? s.rtShadows : 0;
            // The next render selects the lights again with the new settings.
            s_frame = -1;
            if (Shadows == 0) ReleaseLights();
        }

        /// <summary>Ray tracing and screen space shadows of a camera (HplGraphics, beginContextRendering).</summary>
        public static void ConfigureCamera(Camera camera, HDAdditionalCameraData data, bool mirror)
        {
            bool on = Active && !mirror && Supported;
            if (!data.customRenderingSettings) data.customRenderingSettings = true;
            Override(data, FrameSettingsField.RayTracing, on);
            Override(data, FrameSettingsField.ScreenSpaceShadows, on && Shadows > 0);
            if (on && !mirror) SelectLights(camera);
        }

        static void Override(HDAdditionalCameraData data, FrameSettingsField field, bool on)
        {
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
            if (data.renderingPathCustomFrameSettings.IsEnabled(field) != on) data.renderingPathCustomFrameSettings.SetEnabled(field, on);
        }

        /// <summary>
        /// The nearest shadowed lights get ray-traced shadows: the score is the distance from the camera to the light's
        /// range (negative inside it), lights out of view are skipped; the lights traced last frame are kept a little
        /// longer so that two lights at about the same distance do not swap every frame.
        /// </summary>
        static void SelectLights(Camera camera)
        {
            if (s_frame == Time.frameCount) return;
            s_frame = Time.frameCount;
            // Lights of an unloaded map.
            s_traced.RemoveWhere(l => l == null);
            if (s_radius.Count > s_traced.Count)
            {
                var gone = new List<Light>();
                foreach (var hd in s_radius.Keys) if (hd == null) gone.Add(hd);
                foreach (var hd in gone) s_radius.Remove(hd);
            }
            int max = ShadowLights[Shadows];
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Vector3 eye = camera.transform.position;
            s_candidates.Clear();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || !light.castShadows || !light.isActiveAndEnabled) continue;
                if (!light.TryGetComponent<Light>(out var unityLight) || !unityLight.enabled || unityLight.shadows == LightShadows.None || unityLight.intensity <= 0f) continue;
                float range = unityLight.range;
                Vector3 position = light.transform.position;
                if (!GeometryUtility.TestPlanesAABB(planes, new Bounds(position, Vector3.one * (2f * range)))) continue;
                float score = Vector3.Distance(eye, position) - range;
                if (s_traced.Contains(light)) score -= 1f;
                s_candidates.Add((light, score));
            }
            s_candidates.Sort((a, b) => a.score.CompareTo(b.score));
            ShadowedLights = s_candidates.Count;
            var chosen = new HashSet<AmnesiaLight>();
            for (int i = 0; i < s_candidates.Count && i < max; i++) chosen.Add(s_candidates[i].light);
            foreach (var light in s_traced)
                if (light != null && !chosen.Contains(light)) SetTraced(light, false);
            foreach (var light in chosen) SetTraced(light, true);
            s_traced.Clear();
            s_traced.UnionWith(chosen);
            TracedLights = chosen.Count;
        }

        static void SetTraced(AmnesiaLight light, bool on)
        {
            if (!light.TryGetComponent<HDAdditionalLightData>(out var hd) || !light.TryGetComponent<Light>(out var unityLight)) return;
            if (on)
            {
                if (!s_radius.ContainsKey(unityLight)) s_radius[unityLight] = unityLight.shapeRadius;
                unityLight.shapeRadius = SourceRadius;
                hd.numRayTracingSamples = ShadowSamples[Shadows];
                hd.filterTracedShadow = true;
                if (!hd.useRayTracedShadows) hd.useRayTracedShadows = true;
            }
            else
            {
                if (hd.useRayTracedShadows) hd.useRayTracedShadows = false;
                if (s_radius.TryGetValue(unityLight, out float radius)) { unityLight.shapeRadius = radius; s_radius.Remove(unityLight); }
            }
        }

        /// <summary>Every light back to its shadow map (RT shadows or the ray tracing lighting turned off).</summary>
        static void ReleaseLights()
        {
            foreach (var light in s_traced) if (light != null) SetTraced(light, false);
            s_traced.Clear();
            s_radius.Clear();
            TracedLights = ShadowedLights = 0;
        }
    }
}
