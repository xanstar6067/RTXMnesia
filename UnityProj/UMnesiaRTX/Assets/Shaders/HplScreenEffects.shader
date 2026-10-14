// HPL2 screen effects for RTXMnesia.Effects.HplScreenEffectsPass (after post-processing).
// Port of core/shaders/posteffect_image_trail_frag.glsl, posteffect_radial_blur_frag.glsl and
// posteffect_color_conv_tex_frag.glsl plus the HUD fade / flash quads. Works on sRGB values like HPL2.
Shader "Hidden/RTXMnesia/HplScreenEffects"
{
    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D_X(_HplSource);
    TEXTURE2D(_HplSepiaLookup);
    float _HplTrailAlpha;
    float4 _HplRadialBlur;  // x: size in pixels, y: blur start distance (fraction of half the width)
    float _HplSepia;        // fade alpha of the color conversion
    float _HplFade;         // screen × (1 - fade)
    float4 _HplAdd;         // additive color of the flashes

    float3 LoadSource(int2 p)
    {
        p = clamp(p, int2(0, 0), int2(_ScreenSize.xy) - 1);
        return LOAD_TEXTURE2D_X(_HplSource, p).rgb;
    }

    // Bilinear read at a pixel position (texel centers at .5), like texture2DRect with linear filtering.
    float3 SampleSource(float2 pos)
    {
        float2 f = pos - 0.5;
        int2 i = (int2)floor(f);
        float2 t = f - i;
        float3 a = LoadSource(i), b = LoadSource(i + int2(1, 0));
        float3 c = LoadSource(i + int2(0, 1)), d = LoadSource(i + int2(1, 1));
        return lerp(lerp(a, b, t.x), lerp(c, d, t.x), t.y);
    }

    float4 CopyFrag(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        float3 color = CustomPassLoadCameraColor(uint2(v.positionCS.xy), 0);
        return float4(LinearToSRGB(saturate(color)), 1.0);
    }

    // Blended into the accumulation buffer with SrcAlpha / OneMinusSrcAlpha.
    float4 TrailFrag(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        return float4(LoadSource(int2(v.positionCS.xy)), _HplTrailAlpha);
    }

    float4 CompositeFrag(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        float2 pos = v.positionCS.xy;
        float3 color;

        // Radial blur: 5 samples along the direction to the screen center.
        if (_HplRadialBlur.x > 0.0)
        {
            const float colorMul[5] = { 0.1, 0.2, 0.5, 0.2, 0.1 };
            const float sizeMul[5] = { -1.0, -0.5, 0.0, 0.5, 1.0 };
            float2 halfSize = _ScreenSize.xy * 0.5;
            float2 dir = halfSize - pos;
            float dist = length(dir) / halfSize.x;
            dir = dist > 0.0 ? normalize(dir) : float2(0.0, 0.0);
            dist = max(0.0, dist - _HplRadialBlur.y);
            dir *= dist * _HplRadialBlur.x;

            color = 0.0;
            for (int i = 0; i < 5; ++i) color += SampleSource(pos + dir * sizeMul[i]) * colorMul[i];
            color /= 1.1;
        }
        else
        {
            color = LoadSource(int2(pos));
        }

        // Sepia: per channel lookup in the 1D conversion texture, faded with the original.
        if (_HplSepia > 0.0)
        {
            float3 conv = float3(
                SAMPLE_TEXTURE2D_LOD(_HplSepiaLookup, s_linear_clamp_sampler, float2(color.r, 0.5), 0).r,
                SAMPLE_TEXTURE2D_LOD(_HplSepiaLookup, s_linear_clamp_sampler, float2(color.g, 0.5), 0).g,
                SAMPLE_TEXTURE2D_LOD(_HplSepiaLookup, s_linear_clamp_sampler, float2(color.b, 0.5), 0).b);
            float alpha = saturate(_HplSepia);
            color = conv * alpha + color * (1.0 - alpha);
        }

        // HUD layers over the 3D view: modulative fade, additive flashes.
        color *= 1.0 - _HplFade;
        color += _HplAdd.rgb;

        return float4(SRGBToLinear(saturate(color)), 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Copy"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment CopyFrag
            ENDHLSL
        }

        Pass
        {
            Name "Trail"
            ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha Cull Off
            HLSLPROGRAM
            #pragma fragment TrailFrag
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment CompositeFrag
            ENDHLSL
        }
    }
    Fallback Off
}
