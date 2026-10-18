using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Draws <see cref="HplEffects"/> after post-processing, in the order of HPL2's post effect composite
    /// (LuxMapHandler.cpp / LuxPostEffects.cpp priorities, higher first): insanity (25) → image trail (10) →
    /// radial blur (9) → sepia (4), then the HUD
    /// layers drawn over the 3D view (fade: × (1 − alpha), flashes: + color × alpha).
    /// HPL2 renders in gamma space, so the effects work on sRGB values (posteffect_*_frag.glsl) and the result is
    /// converted back to linear.
    /// </summary>
    [Serializable]
    public class HplScreenEffectsPass : CustomPass
    {
        public const string ShaderName = "Hidden/RTXMnesia/HplScreenEffects";
        const int CopyPass = 0, TrailPass = 1, CompositePass = 2, InsanityPass = 7, GammaPass = 8, HdrDisplayPass = 10;

        public Shader shader;
        [NonSerialized] public HplEffects effects;

        Material _material;
        RTHandle _source, _trail, _insanity;
        RTHandle _menuSharp, _menuBlur, _menuTemp;
        int _menuGeneration = -1, _menuWidth, _menuHeight;
        bool _clearTrail = true;
        int _hdrRevision = -1;
        double _lastRenderTime = -1.0;

        static readonly int SourceId = Shader.PropertyToID("_HplSource");
        static readonly int SepiaLookupId = Shader.PropertyToID("_HplSepiaLookup");
        static readonly int TrailAlphaId = Shader.PropertyToID("_HplTrailAlpha");
        static readonly int RadialBlurId = Shader.PropertyToID("_HplRadialBlur");
        static readonly int SepiaId = Shader.PropertyToID("_HplSepia");
        static readonly int FadeId = Shader.PropertyToID("_HplFade");
        static readonly int AddId = Shader.PropertyToID("_HplAdd");
        static readonly int MenuSharpId = Shader.PropertyToID("_HplMenuSharp");
        static readonly int MenuBlendId = Shader.PropertyToID("_HplMenuBlend");
        static readonly int AmpMap0Id = Shader.PropertyToID("_HplAmpMap0");
        static readonly int AmpMap1Id = Shader.PropertyToID("_HplAmpMap1");
        static readonly int ZoomMapId = Shader.PropertyToID("_HplZoomMap");
        static readonly int InsanityId = Shader.PropertyToID("_HplInsanity");
        static readonly int GammaId = Shader.PropertyToID("_HplGamma");

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _clearTrail = true;
            _lastRenderTime = -1.0;
            if (shader == null) shader = Shader.Find(ShaderName);
            if (shader != null) _material = CoreUtils.CreateEngineMaterial(shader);
            _source = Alloc("HPL effects source");
            _trail = Alloc("HPL image trail");
            _insanity = Alloc("HPL insanity");
        }

        // After post processing the screen has the output size even when DLSS renders at a lower one (HDRP resets the
        // RTHandle reference size to it): no dynamic scale, or hardware dynamic resolution would shrink these buffers.
        static RTHandle Alloc(string name) =>
            RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: false, name: name);

        // Preserve HPL2's appearance at 60 Hz, then decay the history by elapsed time.
        // The original exp(-amount * FPS * 0.015) nearly freezes the image at high FPS.
        internal static float CalculateTrailAlpha(float amount, double frameTime)
        {
            if (amount <= 0f) return 1f;
            if (frameTime <= 0.0) return 0f;
            double referenceAlpha = Math.Exp(-amount * 60.0 * 0.015);
            return (float)(1.0 - Math.Exp(Math.Log(1.0 - referenceAlpha) * frameTime * 60.0));
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (effects == null || _material == null) return;
            if (ctx.hdCamera.camera.cameraType != CameraType.Game || World.HplWaterReflection.IsReflectionCamera(ctx.hdCamera.camera)) return;
            bool hdr = Hpl.HplHdr.Active;
            if (_hdrRevision != Hpl.HplHdr.Revision)
            {
                _hdrRevision = Hpl.HplHdr.Revision;
                _clearTrail = true;
                ReleaseMenu();
            }
            // HDRP's AfterPostProcess custom pass sees linear display-gamut nits. The final blit encodes it.
            _material.SetVector("_HplEffectHdr", new Vector4(hdr ? 1 : 0, Hpl.HplHdr.PaperWhite, Hpl.HplHdr.Peak, Hpl.HplHdr.DisplayGamma));
            if (hdr && HDROutputUtils.GetColorSpaceForGamut(HDROutputSettings.main.displayColorGamut, out int gamut))
                _material.SetInteger("_HDRColorspace", gamut);
            RenderEffects(ctx);

            // The gamma of the settings (SDL_SetGamma in HPL2: the display ramp, output = input^(1/gamma)), last. Always in SDR:
            // this pass also turns the screen values back into what the swap chain's piecewise sRGB encode shows.
            float gamma = Ui.HplUserSettings.Gamma;
            if (!hdr)
            {
                var props = ctx.propertyBlock;
                CoreUtils.SetRenderTarget(ctx.cmd, _source, ClearFlag.None);
                CoreUtils.DrawFullScreen(ctx.cmd, _material, props, CopyPass);
                props.SetTexture(SourceId, _source);
                props.SetFloat(GammaId, gamma);
                CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
                CoreUtils.DrawFullScreen(ctx.cmd, _material, props, GammaPass);
            }
            if (hdr && Hpl.HplHdr.DisplayGamma > 0f && Mathf.Abs(Hpl.HplHdr.DisplayGamma - 2.2f) > 0.001f)
            {
                // Optional display exponent; 2.2 (HPL2 on a gamma 2.2 monitor) is the identity and skips this pass.
                var props = ctx.propertyBlock;
                CoreUtils.SetRenderTarget(ctx.cmd, _source, ClearFlag.None);
                CoreUtils.DrawFullScreen(ctx.cmd, _material, props, CopyPass);
                props.SetTexture(SourceId, _source);
                CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
                CoreUtils.DrawFullScreen(ctx.cmd, _material, props, HdrDisplayPass);
            }
            RenderCalibration(ctx);
        }

        void RenderCalibration(CustomPassContext ctx)
        {
            if (!Hpl.HplHdr.CalibrationVisible) return;
            var rect = Hpl.HplHdr.CalibrationRect;
            var props = ctx.propertyBlock;
            props.SetVector("_HplCalibrationRect", new Vector4(rect.x, rect.y, rect.width, rect.height));
            props.SetVector("_HplCalibration", new Vector4(Hpl.HplHdr.CalibrationPattern, Hpl.HplHdr.Peak,
                Hpl.HplHdr.PaperWhite, Hpl.HplHdr.Black));
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(ctx.cmd, _material, props, 9);
        }

        void RenderEffects(CustomPassContext ctx)
        {

            // The graphics options can hide the trail and the radial blur; the effects themselves keep running.
            float trail = Hpl.HplGraphics.ImageTrail ? effects.ImageTrail.Amount : 0f;
            float blur = Hpl.HplGraphics.RadialBlur ? effects.RadialBlur.Size : 0f;
            float sepia = effects.Sepia.Amount;
            float fade = effects.Fade.Alpha;
            Color add = Color.white * effects.Flash.Alpha
                        + effects.SanityGainFlash.Color * effects.SanityGainFlash.Alpha
                        + Color.white * effects.EmotionFlash.Alpha;

            // cPostEffect_ImageTrail: turning it off resets it, the next frame starts from the current image.
            bool trailOn = trail > 0f;
            bool menuOn = effects.Map != null && effects.Map.IsMenuPaused;
            var assets = effects.PostEffectAssets;
            bool insanityOn = effects.Insanity.Active && assets != null && assets.insanityZoomMap != null &&
                              assets.insanityAmpMaps.Length == HplEffects.InsanityEffect.AmpMapCount;
            if (!trailOn) _clearTrail = true;
            if (!insanityOn && !trailOn && blur <= 0f && sepia <= 0f && fade <= 0f && add.maxColorComponent <= 0f)
            {
                if (menuOn) RenderMenu(ctx); else ReleaseMenu();
                return;
            }

            var cmd = ctx.cmd;
            var props = ctx.propertyBlock;

            // Camera color (linear) → source (sRGB).
            CoreUtils.SetRenderTarget(cmd, _source, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, CopyPass);
            RTHandle input = _source;

            if (insanityOn)
            {
                // cLuxPostEffect_Insanity::RenderEffect: the current and next amplitude map, blended by the fraction.
                var insanity = effects.Insanity;
                int amp0 = (int)insanity.AnimCount;
                int amp1 = (int)(insanity.AnimCount + 1f);
                if (amp1 >= HplEffects.InsanityEffect.AmpMapCount) amp1 = 0;
                props.SetTexture(SourceId, _source);
                props.SetTexture(AmpMap0Id, assets.insanityAmpMaps[amp0]);
                props.SetTexture(AmpMap1Id, assets.insanityAmpMaps[amp1]);
                props.SetTexture(ZoomMapId, assets.insanityZoomMap);
                props.SetVector(InsanityId, new Vector4(insanity.T, insanity.AnimCount - Mathf.Floor(insanity.AnimCount), insanity.WaveAlpha, insanity.ZoomAlpha));
                CoreUtils.SetRenderTarget(cmd, _insanity, ClearFlag.None);
                CoreUtils.DrawFullScreen(cmd, _material, props, InsanityPass);
                input = _insanity;
            }

            // Frame time of the renderer: real time since the previous render of this pass (also right for
            // renders outside the player loop, where Time.deltaTime does not advance). Menu time is excluded
            // even if rendering was suspended while minimized, so resuming does not erase the trail.
            double now = effects.Map != null && effects.Map.GameUi != null
                ? effects.Map.GameUi.RenderTime : Time.realtimeSinceStartupAsDouble;
            double frameTime = _lastRenderTime < 0.0 ? 1.0 / 60.0 : Math.Max(0.0, now - _lastRenderTime);
            _lastRenderTime = now;

            if (trailOn)
            {
                // Floating-point history keeps small updates instead of rounding back to an old 8-bit frame.
                float alpha = _clearTrail ? 1f : effects.Map != null && effects.Map.IsMenuPaused
                    ? 0f : CalculateTrailAlpha(trail, frameTime);
                _clearTrail = false;
                props.SetTexture(SourceId, input);
                props.SetFloat(TrailAlphaId, alpha);
                CoreUtils.SetRenderTarget(cmd, _trail, ClearFlag.None);
                CoreUtils.DrawFullScreen(cmd, _material, props, TrailPass);
                input = _trail;
            }

            // Radial blur size is a fraction of the screen width (cPostEffect_RadialBlur; the output size, also with DLSS).
            props.SetTexture(SourceId, input);
            props.SetVector(RadialBlurId, new Vector4(Mathf.Max(0f, blur) * ctx.hdCamera.camera.pixelWidth, effects.RadialBlur.StartDist, 0f, 0f));
            props.SetFloat(SepiaId, sepia > 0f ? sepia : 0f);
            props.SetTexture(SepiaLookupId, effects.sepiaLookup != null ? effects.sepiaLookup : Texture2D.whiteTexture);
            props.SetFloat(FadeId, Mathf.Clamp01(fade));
            props.SetVector(AddId, add);
            CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, CompositePass);
            if (menuOn) RenderMenu(ctx); else ReleaseMenu();
        }

        void RenderMenu(CustomPassContext ctx)
        {
            var ui = effects.Map.GameUi;
            var cmd = ctx.cmd;
            var props = ctx.propertyBlock;
            if (_menuSharp == null)
            {
                _menuSharp = Alloc("HPL menu screenshot");
                _menuBlur = Alloc("HPL menu blur");
                _menuTemp = Alloc("HPL menu blur temp");
            }
            if (_menuGeneration != ui.PauseGeneration || _menuWidth != ctx.hdCamera.camera.pixelWidth || _menuHeight != ctx.hdCamera.camera.pixelHeight)
            {
                _menuGeneration = ui.PauseGeneration;
                _menuWidth = ctx.hdCamera.camera.pixelWidth;
                _menuHeight = ctx.hdCamera.camera.pixelHeight;
                CoreUtils.SetRenderTarget(cmd, _menuSharp, ClearFlag.None);
                CoreUtils.DrawFullScreen(cmd, _material, props, CopyPass);
                RTHandle input = _menuSharp;
                // LuxMainMenu::RenderBlurTexture: one blur plus six further H/V pairs.
                for (int i = 0; i < (ui.ReadingUiOpen ? 0 : 7); i++)
                {
                    props.SetTexture(SourceId, input);
                    CoreUtils.SetRenderTarget(cmd, _menuTemp, ClearFlag.None);
                    CoreUtils.DrawFullScreen(cmd, _material, props, 3);
                    props.SetTexture(SourceId, _menuTemp);
                    CoreUtils.SetRenderTarget(cmd, _menuBlur, ClearFlag.None);
                    CoreUtils.DrawFullScreen(cmd, _material, props, 4);
                    input = _menuBlur;
                }
            }
            props.SetTexture(MenuSharpId, _menuSharp);
            props.SetTexture(SourceId, ui.ReadingUiOpen ? _menuSharp : _menuBlur);
            props.SetFloat(MenuBlendId, ui.MenuOpacity);
            CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, ui.ReadingUiOpen ? 6 : 5);
        }

        void ReleaseMenu()
        {
            _menuSharp?.Release(); _menuSharp = null;
            _menuBlur?.Release(); _menuBlur = null;
            _menuTemp?.Release(); _menuTemp = null;
            _menuGeneration = -1;
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(_material);
            _source?.Release();
            _trail?.Release();
            _insanity?.Release();
            ReleaseMenu();
        }
    }
}
