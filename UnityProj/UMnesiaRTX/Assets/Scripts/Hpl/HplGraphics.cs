using System;
using System.Collections.Generic;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Hpl
{
    /// <summary>Anti-aliasing of the game cameras (HDRP renders deferred, so MSAA is not available).</summary>
    public enum HplAntialiasing { Off, Fxaa, Smaa, Taa }

    /// <summary>
    /// Applies the graphics part of <see cref="HplUserSettings"/>: screen (mode, resolution, VSync, FPS limit), texture
    /// quality and anisotropy, camera anti-aliasing, shadows, bloom and the blur post effects. The lighting and texture
    /// sets are kept here so that renderers and importers can ask for them.
    /// Settings are applied at startup (before the first scene) and again whenever a map's UI applies them.
    /// Quality settings are only changed in Play: in the editor they would be written to ProjectSettings.
    /// </summary>
    public static class HplGraphics
    {
        public static readonly int[] FpsLimits = { 0, 60, 120, 144, 240, 280, 360 };
        public static readonly int[] AnisotropyLevels = { 1, 2, 4, 8, 16 };
        /// <summary>Shadow choices: off, low, medium, high (cLuxMainMenu_Options shadows + ShadowQuality).</summary>
        public const int ShadowsOff = 0, ShadowsHigh = 3;

        /// <summary>The lighting set: 0 — classic (HPL2 conversion). Enhanced lighting comes later.</summary>
        public static int Lighting { get; private set; }
        /// <summary>The texture set: 0 — classic, 1 — enhanced PBR, with classic fallback for missing replacements.</summary>
        public static int Textures { get; private set; }
        public static HplAntialiasing Antialiasing { get; private set; } = HplAntialiasing.Taa;
        public static int Shadows { get; private set; } = ShadowsHigh;
        /// <summary>cLuxEffect_ImageTrail / cLuxEffect_RadialBlur are drawn (the game logic runs either way).</summary>
        public static bool ImageTrail { get; private set; } = true;
        public static bool RadialBlur { get; private set; } = true;
        public static bool Bloom { get; private set; } = true;
        /// <summary>Lighting, texture set or shadows changed (for systems that build their look from them).</summary>
        public static event Action Changed;

        static GameObject s_volumeObject;
        static VolumeProfile s_profile;
        /// <summary>The global settings volume (created by the first Apply in Play).</summary>
        public static Volume SettingsVolume => s_volumeObject != null ? s_volumeObject.GetComponent<Volume>() : null;
        static UnityEngine.Rendering.HighDefinition.Bloom s_bloom;
        static float s_defaultBloom = -1f;
        static bool s_hooked;
#if UNITY_EDITOR
        static bool s_qualitySaved;
        static int s_savedMipLimit;
        static AnisotropicFiltering s_savedAniso;
        static int s_savedFrameRate;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Startup() => Apply(HplUserSettings.ReadSaved());

        public static void Apply(HplUserSettings s)
        {
            if (s == null) return;
            s.Validate();
            bool changed = Lighting != s.lighting || Textures != s.textures || Shadows != s.shadows;
            Lighting = s.lighting;
            Textures = s.textures;
            Antialiasing = (HplAntialiasing)s.antialiasing;
            Shadows = s.shadows;
            ImageTrail = s.imageTrail;
            RadialBlur = s.radialBlur;
            Bloom = s.bloom;
            HplDlss.Apply(s);
            if (!Application.isPlaying) return;
            HplRayTracing.Apply(s);

            Hook();
            SaveEditorQuality();
            QualitySettings.vSyncCount = s.vSync ? 1 : 0;
            // Ignored by Unity while VSync is on.
            Application.targetFrameRate = s.fpsLimit > 0 ? s.fpsLimit : -1;
            // cLuxMainMenu_Options texture quality: High / Medium / Low = full size, half, quarter (mip limit).
            QualitySettings.globalTextureMipmapLimit = 2 - s.textureQuality;
            int aniso = AnisotropyLevels[s.anisotropy];
            if (aniso <= 1) QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            else
            {
                // Every texture gets exactly the chosen level (the import sets 8, GUI textures 0).
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                Texture.SetGlobalAnisotropicFilteringLimits(aniso, aniso);
            }
            ApplyScreen(s);
            ApplyVolume();
            HplHdr.Apply(s);
            foreach (var light in AmnesiaLight.All) ApplyShadows(light);
            if (changed) Changed?.Invoke();
        }

        /// <summary>The window / screen: mode, then the resolution (0 × 0 — the desktop). Not in the editor.</summary>
        static void ApplyScreen(HplUserSettings s)
        {
            if (Application.isEditor) return;
            var mode = s.screenMode switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.Windowed,
            };
            var size = s.resolutionWidth > 0 && s.resolutionHeight > 0
                ? new Vector2Int(s.resolutionWidth, s.resolutionHeight) : DesktopResolution();
            if (Screen.width == size.x && Screen.height == size.y && Screen.fullScreenMode == mode) return;
            var refresh = Screen.currentResolution.refreshRateRatio;
            foreach (var r in Screen.resolutions)
                if (r.width == size.x && r.height == size.y && r.refreshRateRatio.value > refresh.value) refresh = r.refreshRateRatio;
            Screen.SetResolution(size.x, size.y, mode, refresh);
        }

        public static Vector2Int DesktopResolution()
        {
            var display = Screen.mainWindowDisplayInfo;
            if (display.width > 0 && display.height > 0) return new Vector2Int(display.width, display.height);
            return new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        }

        /// <summary>The screen sizes of the display, largest first (refresh rates merged).</summary>
        public static List<Vector2Int> Resolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!list.Contains(size)) list.Add(size);
            }
            var desktop = DesktopResolution();
            if (desktop.x > 0 && !list.Contains(desktop)) list.Add(desktop);
            var current = new Vector2Int(Screen.width, Screen.height);
            if (current.x > 0 && !list.Contains(current)) list.Add(current);
            list.Sort((a, b) => a.x != b.x ? b.x.CompareTo(a.x) : b.y.CompareTo(a.y));
            return list;
        }

        /// <summary>
        /// The shadows of a map light: off, or the map's ShadowResolution (High) lowered one HDRP tier per step below
        /// High (a light without CastShadows never casts).
        /// </summary>
        public static void ApplyShadows(AmnesiaLight light)
        {
            if (!Application.isPlaying || light == null || !light.castShadows) return;
            var unityLight = light.GetComponent<Light>();
            var hd = light.GetComponent<HDAdditionalLightData>();
            if (unityLight == null) return;
            if (light.baseShadowLevel < 0) light.baseShadowLevel = hd != null ? hd.shadowResolution.level : 2;
            unityLight.shadows = Shadows == ShadowsOff ? LightShadows.None : LightShadows.Soft;
            if (hd != null && Shadows != ShadowsOff)
                hd.SetShadowResolutionLevel(Mathf.Max(0, light.baseShadowLevel - (ShadowsHigh - Shadows)));
        }

        static void Hook()
        {
            if (s_hooked) return;
            s_hooked = true;
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
#if UNITY_EDITOR
            Application.quitting += RestoreEditorQuality;
#endif
        }

        /// <summary>Anti-aliasing and ray tracing of every game camera (player, menu, fly camera); the water mirrors keep their anti-aliasing.</summary>
        static void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            var mode = Antialiasing switch
            {
                HplAntialiasing.Fxaa => HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing,
                HplAntialiasing.Smaa => HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                HplAntialiasing.Taa => HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing,
                _ => HDAdditionalCameraData.AntialiasingMode.None,
            };
            foreach (var camera in cameras)
            {
                if (camera == null || camera.cameraType != CameraType.Game || !camera.TryGetComponent<HDAdditionalCameraData>(out var data)) continue;
                bool mirror = HplWaterReflection.IsReflectionCamera(camera);
                // Ray tracing (lighting "Ray tracing", RTX tab); the water mirrors never trace.
                HplRayTracing.ConfigureCamera(camera, data, mirror);
                if (mirror) continue;
                if (data.antialiasing != mode) data.antialiasing = mode;
                if (mode == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    data.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.High;
                // DLSS (RTX tab) replaces the anti-aliasing above while it is on.
                HplDlss.ConfigureCamera(camera, data);
            }
        }

        /// <summary>
        /// A global volume over every scene: no HDRP motion blur or depth of field (HPL2 has neither; the motion
        /// blur of HDRP's default profile smeared every turn of the camera), bloom of the default profile or none.
        /// </summary>
        static void ApplyVolume()
        {
            if (s_volumeObject == null)
            {
                s_volumeObject = new GameObject("HPL graphics settings volume") { hideFlags = HideFlags.DontSave };
                UnityEngine.Object.DontDestroyOnLoad(s_volumeObject);
                var volume = s_volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1000f;
                s_profile = ScriptableObject.CreateInstance<VolumeProfile>();
                s_profile.hideFlags = HideFlags.HideAndDontSave;
                volume.sharedProfile = s_profile;
                s_profile.Add<MotionBlur>(true).intensity.Override(0f);
                s_profile.Add<DepthOfField>(true).focusMode.Override(DepthOfFieldMode.Off);
                s_bloom = s_profile.Add<UnityEngine.Rendering.HighDefinition.Bloom>(true);
            }
            if (s_defaultBloom < 0f)
            {
                s_defaultBloom = 0.2f;
                if (GraphicsSettings.TryGetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>(out var defaults) &&
                    defaults.volumeProfile != null && defaults.volumeProfile.TryGet<UnityEngine.Rendering.HighDefinition.Bloom>(out var bloom))
                    s_defaultBloom = bloom.intensity.value;
            }
            s_bloom.intensity.Override(Bloom ? s_defaultBloom : 0f);
            HplRayTracing.ApplyVolume(s_profile);
        }

        static void SaveEditorQuality()
        {
#if UNITY_EDITOR
            if (s_qualitySaved) return;
            s_qualitySaved = true;
            s_savedMipLimit = QualitySettings.globalTextureMipmapLimit;
            s_savedAniso = QualitySettings.anisotropicFiltering;
            s_savedFrameRate = Application.targetFrameRate;
#endif
        }

#if UNITY_EDITOR
        /// <summary>Leaving Play in the editor: the project's quality settings as they were.</summary>
        static void RestoreEditorQuality()
        {
            if (!s_qualitySaved) return;
            s_qualitySaved = false;
            QualitySettings.globalTextureMipmapLimit = s_savedMipLimit;
            QualitySettings.anisotropicFiltering = s_savedAniso;
            Application.targetFrameRate = s_savedFrameRate;
        }
#endif
    }
}
