using System;
using System.Collections.Generic;
using System.Reflection;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Hpl
{
    /// <summary>The DLSS choice of the RTX tab: off, DLAA (native resolution) or an upscaling mode.</summary>
    public enum HplDlssMode { Off, Dlaa, Quality, Balanced, Performance, UltraPerformance }

    /// <summary>
    /// NVIDIA DLSS through HDRP (module com.unity.modules.nvidia). The pipeline asset enables dynamic resolution with DLSS
    /// as its only upscaler (ConfigureHDRP); a camera uses it only while it allows dynamic resolution, which this class
    /// sets on every game camera before rendering (water mirrors excluded). DLSS replaces the camera's anti-aliasing.
    /// The model (render preset) is per project in HDRP: the chosen preset is written to the pipeline settings in memory
    /// (restored when Play ends in the editor, so it is never saved to the asset).
    /// </summary>
    public static class HplDlss
    {
        public static HplDlssMode Mode { get; private set; }
        /// <summary>The chosen render preset (a NVIDIA.DLSSPreset bit), 0 — NVIDIA's default for each mode.</summary>
        public static uint Preset { get; private set; }

        /// <summary>The GPU and driver run DLSS (an NVIDIA RTX card, the NVIDIA plugin loaded by HDRP).</summary>
        public static bool Available => HDDynamicResolutionPlatformCapabilities.DLSSDetected;
        /// <summary>DLSS renders the game cameras now.</summary>
        public static bool Active => Mode != HplDlssMode.Off && Available;

        /// <summary>The last game camera DLSS was set up for (the RTX tab shows its render resolution).</summary>
        public static Camera LastCamera { get; private set; }

        static readonly FieldInfo SettingsField = typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic);
#if UNITY_EDITOR
        static HDRenderPipelineAsset s_savedAsset;
        static GlobalDynamicResolutionSettings s_savedSettings;
#endif

        public static void Apply(HplUserSettings s)
        {
            Mode = (HplDlssMode)s.dlssMode;
            Preset = (uint)s.dlssPreset;
            if (Application.isPlaying) ApplyPreset();
        }

        /// <summary>Sets DLSS on a game camera before it renders (HplGraphics, beginContextRendering).</summary>
        public static void ConfigureCamera(Camera camera, HDAdditionalCameraData data)
        {
            bool on = Active;
            if (on) LastCamera = camera;
            if (data.allowDynamicResolution != on) data.allowDynamicResolution = on;
            camera.allowDynamicResolution = on;
            data.allowDeepLearningSuperSampling = on;
            if (!on) return;
            data.deepLearningSuperSamplingUseCustomQualitySettings = true;
            data.deepLearningSuperSamplingQuality = (uint)Quality(Mode);
            data.deepLearningSuperSamplingUseCustomAttributes = true;
            data.deepLearningSuperSamplingUseOptimalSettings = true;
            data.deepLearningSuperSamplingSharpening = 0f;
            // DLSS does the temporal anti-aliasing itself.
            if (data.antialiasing != HDAdditionalCameraData.AntialiasingMode.None)
                data.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        }

        /// <summary>The render size of the last DLSS camera and its output size (zero when DLSS is not active).</summary>
        public static bool RenderSize(out Vector2Int render, out Vector2Int output)
        {
            render = output = Vector2Int.zero;
            if (!Active || LastCamera == null) return false;
            var hd = HDCamera.GetOrCreate(LastCamera);
            render = new Vector2Int(hd.actualWidth, hd.actualHeight);
            output = new Vector2Int(LastCamera.pixelWidth, LastCamera.pixelHeight);
            return render.x > 0;
        }

        /// <summary>The NVIDIA quality of a mode (as uint for HDAdditionalCameraData).</summary>
        static int Quality(HplDlssMode mode)
        {
#if ENABLE_NVIDIA
            return (int)(mode switch
            {
                HplDlssMode.Dlaa => UnityEngine.NVIDIA.DLSSQuality.DLAA,
                HplDlssMode.Balanced => UnityEngine.NVIDIA.DLSSQuality.Balanced,
                HplDlssMode.Performance => UnityEngine.NVIDIA.DLSSQuality.MaximumPerformance,
                HplDlssMode.UltraPerformance => UnityEngine.NVIDIA.DLSSQuality.UltraPerformance,
                _ => UnityEngine.NVIDIA.DLSSQuality.MaximumQuality,
            });
#else
            return 0;
#endif
        }

        /// <summary>The selectable presets (bits), NVIDIA's default (0) first.</summary>
        public static List<uint> Presets()
        {
            var list = new List<uint> { 0 };
#if ENABLE_NVIDIA
            foreach (UnityEngine.NVIDIA.DLSSPreset p in Enum.GetValues(typeof(UnityEngine.NVIDIA.DLSSPreset)))
                if (p != UnityEngine.NVIDIA.DLSSPreset.Preset_Default && !list.Contains((uint)p)) list.Add((uint)p);
#endif
            return list;
        }

        /// <summary>The presets NVIDIA allows for a mode (bit mask); 0 when DLSS is off or not available.</summary>
        public static uint AvailablePresets(HplDlssMode mode)
        {
#if ENABLE_NVIDIA
            if (mode == HplDlssMode.Off || !Available) return 0;
            try { return UnityEngine.NVIDIA.GraphicsDevice.GetAvailableDLSSPresetsForQuality((UnityEngine.NVIDIA.DLSSQuality)Quality(mode)); }
            catch (Exception) { return 0; }
#else
            return 0;
#endif
        }

        /// <summary>The letter of a preset ("K"), "" for the default.</summary>
        public static string PresetLetter(uint preset)
        {
#if ENABLE_NVIDIA
            if (preset == 0) return "";
            string name = ((UnityEngine.NVIDIA.DLSSPreset)preset).ToString();
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 ? name.Substring(underscore + 1) : name;
#else
            return "";
#endif
        }

        /// <summary>A valid preset value: 0 or one of <see cref="Presets"/>.</summary>
        public static int ValidPreset(int preset) => preset != 0 && Presets().Contains((uint)preset) ? preset : 0;

        /// <summary>
        /// Writes the preset to every mode of the pipeline's DLSS settings (0 where NVIDIA does not offer it for the mode).
        /// The field is set directly: the property setter calls OnValidate, which recreates the pipeline.
        /// </summary>
        static void ApplyPreset()
        {
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset asset)) return;
            var settings = asset.currentPlatformRenderPipelineSettings;
            var drs = settings.dynamicResolutionSettings;
            uint For(HplDlssMode mode) => Preset != 0 && (AvailablePresets(mode) & Preset) != 0 ? Preset : 0;
            uint quality = For(HplDlssMode.Quality), balanced = For(HplDlssMode.Balanced), performance = For(HplDlssMode.Performance),
                 ultra = For(HplDlssMode.UltraPerformance), dlaa = For(HplDlssMode.Dlaa);
            if (drs.DLSSRenderPresetForQuality == quality && drs.DLSSRenderPresetForBalanced == balanced &&
                drs.DLSSRenderPresetForPerformance == performance && drs.DLSSRenderPresetForUltraPerformance == ultra &&
                drs.DLSSRenderPresetForDLAA == dlaa) return;
#if UNITY_EDITOR
            if (s_savedAsset == null)
            {
                s_savedAsset = asset;
                s_savedSettings = drs;
                Application.quitting += RestoreEditorAsset;
            }
#endif
            drs.DLSSRenderPresetForQuality = quality;
            drs.DLSSRenderPresetForBalanced = balanced;
            drs.DLSSRenderPresetForPerformance = performance;
            drs.DLSSRenderPresetForUltraPerformance = ultra;
            drs.DLSSRenderPresetForDLAA = dlaa;
            settings.dynamicResolutionSettings = drs;
            SetSettings(asset, settings);
        }

        static void SetSettings(HDRenderPipelineAsset asset, RenderPipelineSettings settings)
        {
            if (SettingsField != null) SettingsField.SetValue(asset, settings);
            else asset.currentPlatformRenderPipelineSettings = settings;
        }

#if UNITY_EDITOR
        /// <summary>Leaving Play in the editor: the asset's DLSS presets as they were (never saved with the user's choice).</summary>
        static void RestoreEditorAsset()
        {
            Application.quitting -= RestoreEditorAsset;
            if (s_savedAsset == null) return;
            var settings = s_savedAsset.currentPlatformRenderPipelineSettings;
            settings.dynamicResolutionSettings = s_savedSettings;
            SetSettings(s_savedAsset, settings);
            s_savedAsset = null;
        }
#endif
    }
}
