using System.Reflection;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.World
{
    /// <summary>
    /// Shadow maps of the enhanced lighting (graphics tab, shadows on): soft shadows (PCSS — HDRP's "High" punctual
    /// filtering: the penumbra grows with the distance to the occluder and the size of the source, the flame's for
    /// lamps) and contact shadows (a short screen-space ray towards the light: thin shadows in the gaps the shadow map
    /// misses — legs of furniture, candles on a table). The classic lighting keeps HDRP's "Medium" filtering of the
    /// project's asset and no contact shadows.
    /// PCSS is one setting of the whole pipeline: a global shader keyword, plus the asset's value read by the light loop on
    /// the CPU (filter width, bias). Both are set here, the asset only in memory (in the editor it is restored on leaving
    /// Play). A build keeps the PCSS variants through Assets/Settings/HDRP_PcssVariants.asset (EnsurePcssVariants).
    /// The lights themselves (source radius, contact shadows) are set by <see cref="HplEnhancedLighting"/>.
    /// </summary>
    public static class HplEnhancedShadows
    {
        /// <summary>Contact shadow ray length (m): longer reaches into the room as a dark halo around objects.</summary>
        public const float ContactLength = 0.15f;

        static readonly FieldInfo SettingsField = typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly string[] Keywords = { "PUNCTUAL_SHADOW_LOW", "PUNCTUAL_SHADOW_MEDIUM", "PUNCTUAL_SHADOW_HIGH" };
        static HDRenderPipelineAsset s_asset;
        static HDShadowFilteringQuality s_assetQuality;
        static ContactShadows s_contact;

        /// <summary>Settings chosen in the graphics tab (applied whether or not the enhanced lighting is on).</summary>
        public static bool SoftChosen { get; private set; } = true;
        public static bool ContactChosen { get; private set; } = true;
        public static bool LanternChosen { get; private set; } = true;
        /// <summary>The lantern in the hand casts shadows (a shadow map, or RT when the RTX tab traces it).</summary>
        public static bool Lantern => LanternChosen && On;
        /// <summary>PCSS is in effect: enhanced lighting, shadows on, soft shadows chosen.</summary>
        public static bool Soft => SoftChosen && On;
        /// <summary>Contact shadows are in effect.</summary>
        public static bool Contact => ContactChosen && On;
        static bool On => HplGraphics.Lighting == 1 && HplGraphics.Shadows != HplGraphics.ShadowsOff;

        /// <summary>The graphics settings changed (HplGraphics.Apply, before the enhanced lighting is refreshed).</summary>
        public static void Apply(HplUserSettings s)
        {
            SoftChosen = s.softShadows;
            ContactChosen = s.contactShadows;
            LanternChosen = s.lanternShadow;
            if (!Application.isPlaying) return;
            SetFiltering(Soft ? HDShadowFilteringQuality.High : (HDShadowFilteringQuality?)null);
        }

        /// <summary>The punctual shadow filtering: <paramref name="quality"/>, or the asset's own (null).</summary>
        static void SetFiltering(HDShadowFilteringQuality? quality)
        {
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset asset)) return;
            if (s_asset != asset)
            {
                if (s_asset != null) Restore();
                s_asset = asset;
                s_assetQuality = asset.currentPlatformRenderPipelineSettings.hdShadowInitParams.punctualShadowFilteringQuality;
#if UNITY_EDITOR
                Application.quitting -= Restore;
                Application.quitting += Restore;
#endif
            }
            var wanted = quality ?? s_assetQuality;
            var settings = asset.currentPlatformRenderPipelineSettings;
            if (settings.hdShadowInitParams.punctualShadowFilteringQuality != wanted)
            {
                settings.hdShadowInitParams.punctualShadowFilteringQuality = wanted;
                // The field is set directly: the property setter calls OnValidate, which recreates the pipeline.
                if (SettingsField != null) SettingsField.SetValue(asset, settings);
                else asset.currentPlatformRenderPipelineSettings = settings;
            }
            foreach (var k in Keywords) Shader.DisableKeyword(k);
            Shader.EnableKeyword(Keywords[(int)wanted]);
        }

        /// <summary>The asset's filtering and keyword as they were (leaving Play in the editor: never saved).</summary>
        static void Restore()
        {
            if (s_asset == null) return;
            var asset = s_asset;
            s_asset = null;
            var settings = asset.currentPlatformRenderPipelineSettings;
            settings.hdShadowInitParams.punctualShadowFilteringQuality = s_assetQuality;
            if (SettingsField != null) SettingsField.SetValue(asset, settings);
            foreach (var k in Keywords) Shader.DisableKeyword(k);
            Shader.EnableKeyword(Keywords[(int)s_assetQuality]);
        }

        /// <summary>The contact shadows of the global settings volume (HplGraphics.ApplyVolume).</summary>
        public static void ApplyVolume(VolumeProfile profile)
        {
            if (s_contact == null && !profile.TryGet(out s_contact))
            {
                s_contact = profile.Add<ContactShadows>(true);
                s_contact.length.Override(ContactLength);
                // Fade out with the distance from the camera: a screen-space ray is long in pixels only up close.
                s_contact.maxDistance.Override(15f);
                s_contact.fadeDistance.Override(5f);
                s_contact.opacity.Override(1f);
            }
            s_contact.enable.Override(Contact);
        }

        /// <summary>The contact shadows frame setting of a game camera (beginContextRendering).</summary>
        public static void ConfigureCamera(HDAdditionalCameraData data, bool mirror)
        {
            bool on = Contact && !mirror;
            if (!data.customRenderingSettings) data.customRenderingSettings = true;
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.ContactShadows] = true;
            if (data.renderingPathCustomFrameSettings.IsEnabled(FrameSettingsField.ContactShadows) != on)
                data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.ContactShadows, on);
        }

        /// <summary>
        /// A light of the enhanced lighting with a shadow map: the source size for PCSS (the flame's, else
        /// <see cref="HplRayTracing.SourceRadius"/>) and contact shadows. Area lights keep their size (their shadows are
        /// filtered by HDRP's area setting).
        /// Contact shadows only for lights outside an entity's model (lights of the map, added ones): a lamp's light sits
        /// in its model, which casts neither shadow maps nor RT shadows, but the screen-space ray sees it — the torch
        /// went dark and threw its cage on the wall behind. HDRP cannot leave one object out of contact shadows.
        /// </summary>
        public static void ConfigureLight(AmnesiaLight light, Light unityLight, HDAdditionalLightData hd)
        {
            bool shadowed = unityLight.shadows != LightShadows.None;
            if (Soft && shadowed && unityLight.type is LightType.Point or LightType.Spot or LightType.Pyramid or LightType.Box)
                unityLight.shapeRadius = light.tracedSourceRadius >= 0f ? light.tracedSourceRadius : HplRayTracing.SourceRadius;
            if (hd != null)
            {
                hd.useContactShadow.useOverride = true;
                hd.useContactShadow.@override = Contact && shadowed && !light.handHeld && light.GetComponentInParent<AmnesiaEntity>(true) == null;
            }
        }
    }
}
