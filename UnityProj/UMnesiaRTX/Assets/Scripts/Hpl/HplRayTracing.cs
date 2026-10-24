using System;
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
    /// shadows up to the chosen count; the others keep their shadow maps. "Shadows from all lights" adds the lights
    /// HPL2 drew without shadows (most candles) to that choice; the lantern in the hand has its own setting and slot.
    /// </summary>
    public static class HplRayTracing
    {
        /// <summary>RT shadow choices: off, low, medium, high, ultra — lights traced per frame and rays per pixel.</summary>
        public static readonly int[] ShadowLights = { 0, 4, 8, 12, 16 };
        static readonly int[] ShadowSamples = { 0, 1, 1, 2, 4 };
        public const int ShadowsDefault = 2;
        /// <summary>RT ambient occlusion choices: off, low, medium, high (HDRP's RTAO quality levels).</summary>
        public const int AoLevels = 4, AoDefault = 2;
        /// <summary>How much RTAO darkens the direct light of lamps (HDRP applies AO to indirect light only, which HPL2 maps do not have).</summary>
        public const float AoDirectLighting = 0.35f;

        /// <summary>
        /// RT reflection choices: off, low, medium, high. HPL2 materials are rough (specular power → smoothness 0.3–0.6),
        /// so the smoothness from which surfaces reflect goes down with the level (HDRP's default 0.9 reflects nothing);
        /// low traces at half resolution.
        /// </summary>
        public const int ReflectionLevels = 4, ReflectionsDefault = 2;
        static readonly float[] ReflectionMinSmoothness = { 1f, 0.55f, 0.45f, 0.35f };
        static readonly float[] ReflectionRayLength = { 0f, 10f, 25f, 50f };

        /// <summary>
        /// RTGI choices: off, low, medium, high (the remaster lighting: on by default). Light bounced from the lamps and
        /// emissive surfaces; low — half resolution with a half resolution denoiser, medium — half resolution with a
        /// second denoiser pass, high — full resolution.
        /// </summary>
        public const int GiLevels = 4, GiDefault = 2;
        /// <summary>
        /// Screen space GI choices (graphics tab, enhanced lighting without RTGI): off, low, medium, high. The same HDRP
        /// component as RTGI, ray marched in the depth buffer: it bounces only the light of what is on screen.
        /// </summary>
        public const int SsgiLevels = 4, SsgiDefault = 2;
        static readonly int[] SsgiRaySteps = { 0, 32, 48, 64 };
        /// <summary>Lights found by the rays (reflections, GI) within this distance of the camera (HDRP: 10 m).</summary>
        public const float LightClusterRange = 40f;

        /// <summary>
        /// Path tracing choices: off, or the samples accumulated while the camera stands still (HDRP's path tracer
        /// starts again whenever the view changes, so a moving camera sees a noisy image), then denoised (OptiX).
        /// It replaces the rasterized lighting: the custom passes (box lights, fog) and the HPL shaders (particles,
        /// halos, Mul decals, water) are not path traced.
        /// </summary>
        public static readonly int[] PathTracingSamples = { 0, 64, 256, 1024 };
        /// <summary>
        /// Layer of the renderers only RTGI sees (HplEnhancedEmission's copies of glowing materials): no camera draws it,
        /// RTAO, RT reflections and path tracing leave it out of their rays.
        /// </summary>
        public const int GiOnlyLayer = 28;
        const int AllButGiOnly = ~(1 << GiOnlyLayer);
        /// <summary>
        /// Layer of the player's hands and the object in them (PlayerHands): RTGI leaves it out of its rays. The lantern's
        /// light is a few cm from its model and the hand, which the rays of the hand's own pixels hit at once: the hands
        /// and the lantern glowed orange (mean of the frame 31 against 20 without RTGI). Still in the acceleration
        /// structure: path tracing draws only what is in it.
        /// </summary>
        public const int HandsLayer = 27;

        /// <summary>The source radius of a shadowed light while it is traced (HPL2 lights have none; candles, lamps).</summary>
        public const float SourceRadius = 0.04f;

        /// <summary>The GPU, the graphics API (DirectX 12) and the pipeline trace rays.</summary>
        public static bool Supported => SystemInfo.supportsRayTracing &&
            (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp ? hdrp.rayTracingSupported : true);
        /// <summary>The "Ray tracing" lighting is chosen and runs.</summary>
        public static bool Active { get; private set; }
        /// <summary>Index of <see cref="ShadowLights"/>.</summary>
        public static int Shadows { get; private set; }
        /// <summary>RT ambient occlusion: 0 — off, 1..3 — low, medium, high.</summary>
        public static int Ao { get; private set; }
        /// <summary>RT reflections: 0 — off, 1..3 — low, medium, high.</summary>
        public static int Reflections { get; private set; }
        /// <summary>RTGI: 0 — off, 1..3 — low, medium, high.</summary>
        public static int Gi { get; private set; }
        /// <summary>Screen space GI in effect: 0 — off, 1..3 — low, medium, high (never with RTGI).</summary>
        public static int ScreenGi { get; private set; }
        /// <summary>Path tracing: index of <see cref="PathTracingSamples"/>, 0 — off.</summary>
        public static int PathTracing { get; private set; }
        /// <summary>Lights without CastShadows in the map (candles) also get ray-traced shadows (remaster).</summary>
        public static bool AllShadows { get; private set; }
        /// <summary>The lantern in the hand casts ray-traced shadows (remaster).</summary>
        public static bool LanternShadows { get; private set; }
        /// <summary>The hand-held lights traced in the last frame (RTX tab, checks).</summary>
        public static int TracedHandLights { get; private set; }
        /// <summary>The lights traced in the last frame, of the shadowed lights considered (RTX tab).</summary>
        public static int TracedLights { get; private set; }
        public static int ShadowedLights { get; private set; }

        static readonly List<(AmnesiaLight light, float score)> s_candidates = new();
        static readonly HashSet<AmnesiaLight> s_traced = new();
        static readonly HashSet<AmnesiaLight> s_hand = new();
        /// <summary>Hand-held lights with a smaller HPL2 radius are glows, never traced.</summary>
        public const float MinHandShadowRadius = 1f;
        /// <summary>HDRP's screen space shadow slots (the pipeline asset, ConfigureHDRP).</summary>
        const int MaxSlots = 16;
        static readonly Dictionary<Light, float> s_radius = new();
        static int s_frame = -1;

        public static void Apply(HplUserSettings s)
        {
            Active = s.lighting == 1 && SystemInfo.supportsRayTracing;
            Shadows = Active ? s.rtShadows : 0;
            Ao = Active ? s.rtAo : 0;
            Reflections = Active ? s.rtReflections : 0;
            Gi = Active ? s.rtGi : 0;
            ScreenGi = Gi == 0 && s.lightingSet == 1 ? s.ssgi : 0;
            PathTracing = Active ? s.rtPathTracing : 0;
            AllShadows = Shadows > 0 && s.rtAllShadows;
            LanternShadows = Active && s.rtLanternShadows;
            // The next render selects the lights again with the new settings.
            s_frame = -1;
            if (Shadows == 0 && !LanternShadows) ReleaseLights();
        }

        static ScreenSpaceAmbientOcclusion s_ao;
        static ScreenSpaceReflection s_reflection;
        static GlobalIllumination s_gi;
        static LightCluster s_cluster;
        static UnityEngine.Rendering.HighDefinition.PathTracing s_pathTracing;

        /// <summary>
        /// The ray tracing part of the global settings volume (HplGraphics): RTAO, RT reflections and RTGI. With the classic
        /// lighting the components are inactive (and the cameras render no SSR), so the classic look is unchanged.
        /// </summary>
        public static void ApplyVolume(VolumeProfile profile)
        {
            if (s_ao == null && !profile.TryGet(out s_ao)) s_ao = profile.Add<ScreenSpaceAmbientOcclusion>(true);
            s_ao.active = Ao > 0;
            if (Ao > 0)
            {
                s_ao.layerMask.Override(AllButGiOnly);
                s_ao.rayTracing.Override(true);
                s_ao.quality.Override(Ao - 1);
                s_ao.intensity.Override(1f);
                s_ao.directLightingStrength.Override(AoDirectLighting);
            }

            if (s_reflection == null && !profile.TryGet(out s_reflection))
            {
                s_reflection = profile.Add<ScreenSpaceReflection>();
                // The values below are set through properties (no overrideState of their own).
                s_reflection.SetAllOverridesTo(true);
            }
            ApplyGi(profile);
            ApplyPathTracing(profile);
            s_reflection.active = Reflections > 0;
            if (Reflections == 0) return;
            s_reflection.enabled.Override(true);
            s_reflection.layerMask.Override(AllButGiOnly);
            s_reflection.tracing.Override(RayCastingMode.RayTracing);
            s_reflection.mode.Override(RayTracingMode.Performance);
            // Custom quality: the values below instead of the asset's quality levels.
            s_reflection.quality.Override(ScalableSettingLevelParameter.LevelCount);
            s_reflection.minSmoothness = ReflectionMinSmoothness[Reflections];
            s_reflection.smoothnessFadeStart = ReflectionMinSmoothness[Reflections] + 0.1f;
            s_reflection.rayLength = ReflectionRayLength[Reflections];
            s_reflection.fullResolution = Reflections > 1;
            s_reflection.denoise = true;
            s_reflection.denoiserRadius = Reflections > 1 ? 0.75f : 0.5f;
        }

        static void ApplyPathTracing(VolumeProfile profile)
        {
            if (s_pathTracing == null && !profile.TryGet(out s_pathTracing)) s_pathTracing = profile.Add<UnityEngine.Rendering.HighDefinition.PathTracing>();
            s_pathTracing.active = PathTracing > 0;
            if (PathTracing == 0) return;
            s_pathTracing.enable.Override(true);
            s_pathTracing.layerMask.Override(AllButGiOnly);
            s_pathTracing.maximumSamples.Override(PathTracingSamples[PathTracing]);
            s_pathTracing.maximumDepth.Override(4);
            s_pathTracing.maximumIntensity.Override(10f);
            // The NVIDIA denoiser of com.unity.rendering.denoising, once the samples are accumulated.
            if (Enum.TryParse("Optix", out HDDenoiserType optix)) s_pathTracing.denoising.Override(optix);
        }

        /// <summary>RTGI or SSGI, and the light cluster the rays of the reflections and of the GI see.</summary>
        static void ApplyGi(VolumeProfile profile)
        {
            if (s_cluster == null && !profile.TryGet(out s_cluster)) s_cluster = profile.Add<LightCluster>();
            s_cluster.active = Active;
            if (Active) s_cluster.cameraClusterRange.Override(LightClusterRange);

            if (s_gi == null && !profile.TryGet(out s_gi))
            {
                s_gi = profile.Add<GlobalIllumination>();
                // The values below are set through properties (no overrideState of their own).
                s_gi.SetAllOverridesTo(true);
            }
            s_gi.active = Gi > 0 || ScreenGi > 0;
            if (!s_gi.active) return;
            s_gi.enable.Override(true);
            if (Gi == 0)
            {
                s_gi.tracing.Override(RayCastingMode.RayMarching);
                s_gi.quality.Override(ScalableSettingLevelParameter.LevelCount);
                s_gi.fullResolutionSS.Override(ScreenGi == 3);
                s_gi.maxRaySteps = SsgiRaySteps[ScreenGi];
                s_gi.denoiseSS = true;
                s_gi.halfResolutionDenoiserSS = ScreenGi == 1;
                s_gi.secondDenoiserPassSS = ScreenGi > 1;
                s_gi.denoiserRadiusSS = 0.75f;
                return;
            }
            s_gi.layerMask.Override(~(1 << HandsLayer));
            s_gi.tracing.Override(RayCastingMode.RayTracing);
            s_gi.mode.Override(RayTracingMode.Performance);
            // Custom quality: the values below instead of the asset's quality levels.
            s_gi.quality.Override(ScalableSettingLevelParameter.LevelCount);
            s_gi.rayLength = 50f;
            s_gi.fullResolution = Gi == 3;
            s_gi.denoise = true;
            s_gi.halfResolutionDenoiser = Gi == 1;
            s_gi.secondDenoiserPass = Gi > 1;
            s_gi.denoiserRadius = 0.66f;
        }

        /// <summary>
        /// Renderers of the HPL shaders (RTXMnesia/*: particles, halos, Mul decals, water) and mesh decals (HDRP/Decal)
        /// have no ray tracing pass: in the acceleration structure the path tracer drew them black (it takes transparent
        /// materials too; the dirt decals covered the floors). They are transparent or decals, so the raster RT
        /// effects never needed them either.
        /// </summary>
        public static void Exclude(Renderer renderer)
        {
            if (renderer == null || renderer.rayTracingMode == UnityEngine.Experimental.Rendering.RayTracingMode.Off) return;
            foreach (var material in renderer.sharedMaterials)
                if (material != null && material.shader != null &&
                    (material.shader.name.StartsWith("RTXMnesia/", StringComparison.Ordinal) || material.FindPass("DBufferMesh") >= 0))
                {
                    renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
                    return;
                }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookScenes() => UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, _) =>
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) Exclude(renderer);
        };

        /// <summary>The ray tracing settings of <paramref name="s"/> are the applied ones (RTX tab: "applies after OK").</summary>
        public static bool Applied(HplUserSettings s)
        {
            bool active = s.lighting == 1 && SystemInfo.supportsRayTracing;
            if (active != Active) return false;
            return !active || Shadows == s.rtShadows && Ao == s.rtAo && Reflections == s.rtReflections && Gi == s.rtGi &&
                   AllShadows == (s.rtShadows > 0 && s.rtAllShadows) && LanternShadows == s.rtLanternShadows &&
                   PathTracing == s.rtPathTracing;
        }

        /// <summary>Ray tracing and screen space shadows of a camera (HplGraphics, beginContextRendering).</summary>
        public static void ConfigureCamera(Camera camera, HDAdditionalCameraData data, bool mirror)
        {
            bool on = Active && !mirror && Supported;
            if ((camera.cullingMask & ~AllButGiOnly) != 0) camera.cullingMask &= AllButGiOnly;
            if (!data.customRenderingSettings) data.customRenderingSettings = true;
            Override(data, FrameSettingsField.RayTracing, on);
            Override(data, FrameSettingsField.ScreenSpaceShadows, on && Shadows > 0);
            // The asset supports SSR only for these reflections: never ray marched, never with the classic lighting.
            Override(data, FrameSettingsField.SSR, on && Reflections > 0);
            // Likewise SSGI: the ray-traced GI, or the screen space GI of the enhanced lighting without it.
            Override(data, FrameSettingsField.SSGI, on && Gi > 0 || !mirror && ScreenGi > 0);
            if (on && !mirror && (Shadows > 0 || LanternShadows)) SelectLights(camera);
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
            // The lantern first: it keeps its slot, the map lights share the rest of HDRP's slots.
            int hand = 0;
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || !light.handHeld) continue;
                // The lantern's second light is a 0.3 m glow of its glass: no shadows worth a slot.
                bool lit = LanternShadows && light.radius >= MinHandShadowRadius && light.isActiveAndEnabled && light.TryGetComponent<Light>(out var handLight) &&
                           handLight.enabled && handLight.intensity > 0f && HplGraphics.Shadows != HplGraphics.ShadowsOff;
                if (lit) { s_hand.Add(light); hand++; }
                SetTraced(light, lit);
            }
            s_hand.RemoveWhere(l => l == null || !l.handHeld || !LanternShadows || !l.isActiveAndEnabled);
            TracedHandLights = hand;
            int max = Mathf.Min(ShadowLights[Shadows], MaxSlots - hand);
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Vector3 eye = camera.transform.position;
            s_candidates.Clear();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || light.box || light.handHeld || light.excludeFromLightLevel || !light.isActiveAndEnabled) continue;
                if (!light.castShadows && (!AllShadows || light.noTracedShadows)) continue;
                if (!light.TryGetComponent<Light>(out var unityLight) || !unityLight.enabled || unityLight.intensity <= 0f) continue;
                // Map shadows: the graphics tab's shadow setting (Off: none); the added ones only follow its Off.
                if (light.castShadows ? unityLight.shadows == LightShadows.None : HplGraphics.Shadows == HplGraphics.ShadowsOff) continue;
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
            if (!light.castShadows)
            {
                // A light HPL2 drew without shadows: shadows only while traced. HDRP still wants a shadow map for it;
                // the smallest one, rendered once (on demand), costs almost nothing.
                if (on && unityLight.shadows == LightShadows.None)
                {
                    hd.shadowUpdateMode = ShadowUpdateMode.OnDemand;
                    hd.SetShadowResolutionLevel(0);
                    unityLight.shadows = LightShadows.Soft;
                }
                else if (!on && unityLight.shadows != LightShadows.None)
                {
                    unityLight.shadows = LightShadows.None;
                    hd.shadowUpdateMode = ShadowUpdateMode.EveryFrame;
                }
            }
            if (on)
            {
                if (!s_radius.ContainsKey(unityLight)) s_radius[unityLight] = unityLight.shapeRadius;
                // Area lights have their size; the enhanced lighting gives its lights their own source radius.
                if (unityLight.type is LightType.Point or LightType.Spot or LightType.Pyramid or LightType.Box)
                    unityLight.shapeRadius = light.tracedSourceRadius >= 0f ? light.tracedSourceRadius : SourceRadius;
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

        /// <summary>The traced lights are chosen again at the next render (the lights of the map changed).</summary>
        public static void Reselect() => s_frame = -1;

        /// <summary>A light stops being traced now (it is changed or removed by the enhanced lighting).</summary>
        public static void Release(AmnesiaLight light)
        {
            if (light == null || !s_traced.Remove(light)) return;
            SetTraced(light, false);
            s_frame = -1;
        }

        /// <summary>Every light back to its shadow map (RT shadows or the ray tracing lighting turned off).</summary>
        static void ReleaseLights()
        {
            foreach (var light in s_traced) if (light != null) SetTraced(light, false);
            foreach (var light in s_hand) if (light != null) SetTraced(light, false);
            s_traced.Clear();
            s_hand.Clear();
            TracedHandLights = 0;
            s_radius.Clear();
            TracedLights = ShadowedLights = 0;
        }
    }
}
