using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Draws <see cref="HplEffects"/> after post-processing, in the order of HPL2's post effect composite
    /// (LuxMapHandler.cpp priorities, higher first): image trail (10) → radial blur (9) → sepia (4), then the HUD
    /// layers drawn over the 3D view (fade: × (1 − alpha), flashes: + color × alpha).
    /// HPL2 renders in gamma space, so the effects work on sRGB values (posteffect_*_frag.glsl) and the result is
    /// converted back to linear.
    /// </summary>
    [Serializable]
    public class HplScreenEffectsPass : CustomPass
    {
        public const string ShaderName = "Hidden/RTXMnesia/HplScreenEffects";
        const int CopyPass = 0, TrailPass = 1, CompositePass = 2;

        public Shader shader;
        [NonSerialized] public HplEffects effects;

        Material _material;
        RTHandle _source, _trail;
        bool _clearTrail = true;
        double _lastRenderTime = -1.0;

        static readonly int SourceId = Shader.PropertyToID("_HplSource");
        static readonly int SepiaLookupId = Shader.PropertyToID("_HplSepiaLookup");
        static readonly int TrailAlphaId = Shader.PropertyToID("_HplTrailAlpha");
        static readonly int RadialBlurId = Shader.PropertyToID("_HplRadialBlur");
        static readonly int SepiaId = Shader.PropertyToID("_HplSepia");
        static readonly int FadeId = Shader.PropertyToID("_HplFade");
        static readonly int AddId = Shader.PropertyToID("_HplAdd");

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _clearTrail = true;
            _lastRenderTime = -1.0;
            if (shader == null) shader = Shader.Find(ShaderName);
            if (shader != null) _material = CoreUtils.CreateEngineMaterial(shader);
            _source = Alloc("HPL effects source");
            _trail = Alloc("HPL image trail");
        }

        static RTHandle Alloc(string name) =>
            RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                            colorFormat: GraphicsFormat.R16G16B16A16_SFloat, useDynamicScale: true, name: name);

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
            if (ctx.hdCamera.camera.cameraType != CameraType.Game) return;

            float trail = effects.ImageTrail.Amount;
            float blur = effects.RadialBlur.Size;
            float sepia = effects.Sepia.Amount;
            float fade = effects.Fade.Alpha;
            Color add = Color.white * effects.Flash.Alpha
                        + effects.SanityGainFlash.Color * effects.SanityGainFlash.Alpha
                        + Color.white * effects.EmotionFlash.Alpha;

            // cPostEffect_ImageTrail: turning it off resets it, the next frame starts from the current image.
            bool trailOn = trail > 0f;
            if (!trailOn) _clearTrail = true;
            if (!trailOn && blur <= 0f && sepia <= 0f && fade <= 0f && add.maxColorComponent <= 0f) return;

            var cmd = ctx.cmd;
            var props = ctx.propertyBlock;

            // Camera color (linear) → source (sRGB).
            CoreUtils.SetRenderTarget(cmd, _source, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, CopyPass);
            RTHandle input = _source;

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
                props.SetTexture(SourceId, _source);
                props.SetFloat(TrailAlphaId, alpha);
                CoreUtils.SetRenderTarget(cmd, _trail, ClearFlag.None);
                CoreUtils.DrawFullScreen(cmd, _material, props, TrailPass);
                input = _trail;
            }

            // Radial blur size is a fraction of the render width (cPostEffect_RadialBlur).
            props.SetTexture(SourceId, input);
            props.SetVector(RadialBlurId, new Vector4(Mathf.Max(0f, blur) * ctx.hdCamera.actualWidth, effects.RadialBlur.StartDist, 0f, 0f));
            props.SetFloat(SepiaId, sepia > 0f ? sepia : 0f);
            props.SetTexture(SepiaLookupId, effects.sepiaLookup != null ? effects.sepiaLookup : Texture2D.whiteTexture);
            props.SetFloat(FadeId, Mathf.Clamp01(fade));
            props.SetVector(AddId, add);
            CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, CompositePass);
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(_material);
            _source?.Release();
            _trail?.Release();
        }
    }
}
