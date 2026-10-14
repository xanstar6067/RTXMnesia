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
            if (shader == null) shader = Shader.Find(ShaderName);
            if (shader != null) _material = CoreUtils.CreateEngineMaterial(shader);
            _source = Alloc("HPL effects source");
            _trail = Alloc("HPL image trail");
        }

        static RTHandle Alloc(string name) =>
            RTHandles.Alloc(Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
                            colorFormat: GraphicsFormat.R8G8B8A8_UNorm, useDynamicScale: true, name: name);

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
            // renders outside the player loop, where Time.deltaTime does not advance).
            double now = Time.realtimeSinceStartupAsDouble;
            float frameTime = _lastRenderTime < 0.0 ? 1f / 60f : Mathf.Clamp((float)(now - _lastRenderTime), 1e-3f, 0.25f);
            _lastRenderTime = now;

            if (trailOn)
            {
                // Blend weight of the new image: exp(-(amount / frame time) × 0.015), 1 right after a reset.
                float alpha = _clearTrail ? 1f : Mathf.Exp(-(1f / frameTime) * trail * 0.015f);
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
