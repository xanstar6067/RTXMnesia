// HPL2 screen effects for RTXMnesia.Effects.HplScreenEffectsPass (after post-processing).
// Port of core/shaders/posteffect_image_trail_frag.glsl, posteffect_radial_blur_frag.glsl,
// posteffect_color_conv_tex_frag.glsl and shaders/posteffect_insanity_frag.glsl plus the HUD fade / flash quads.
// Works on sRGB values like HPL2.
Shader "Hidden/RTXMnesia/HplScreenEffects"
{
    HLSLINCLUDE
    #pragma vertex Vert
    #pragma target 4.5

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D_X(_HplSource);
    TEXTURE2D_X(_HplMenuSharp);
    float _HplMenuBlend;
    TEXTURE2D(_HplSepiaLookup);
    float _HplTrailAlpha;
    float4 _HplRadialBlur;  // x: size in pixels, y: blur start distance (fraction of half the width)
    float _HplSepia;        // fade alpha of the color conversion
    float _HplFade;         // screen × (1 - fade)
    float4 _HplAdd;         // additive color of the flashes
    TEXTURE2D(_HplAmpMap0);
    TEXTURE2D(_HplAmpMap1);
    TEXTURE2D(_HplZoomMap);
    float4 _HplInsanity;    // x: wave time, y: amplitude map blend, z: wave alpha, w: zoom alpha
    float _HplGamma;        // gamma of the settings: sRGB value^(1/gamma), as SDL's gamma ramp

    float4 GammaFrag(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        float3 color = LoadSource(int2(v.positionCS.xy));
        return float4(SRGBToLinear(pow(saturate(color), 1.0 / max(_HplGamma, 0.01))), 1.0);
    }

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
    // posteffect_insanity_frag.glsl. HPL2 works in GL pixels (y up); the maps are read with the image's top row at
    // the screen bottom like HPL2 (Unity textures have v = 0 at the bottom row, the pixel y here grows downwards).
    float4 InsanityFrag(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        float2 size = _ScreenSize.xy;
        float2 pos = v.positionCS.xy;
        float2 uv = pos / size;
        float ampT = _HplInsanity.y;
        float3 amp = SAMPLE_TEXTURE2D_LOD(_HplAmpMap0, s_linear_clamp_sampler, uv, 0).xyz * (1.0 - ampT)
                   + SAMPLE_TEXTURE2D_LOD(_HplAmpMap1, s_linear_clamp_sampler, uv, 0).xyz * ampT;
        amp *= _HplInsanity.z * 0.04 * size.y;
        float3 zoom = SAMPLE_TEXTURE2D_LOD(_HplZoomMap, s_linear_clamp_sampler, uv, 0).xyz;
        float2 p = float2(pos.x, size.y - pos.y);
        p += (zoom.xy - 0.5) * 2.0 * 0.6 * zoom.z * size.y * _HplInsanity.w;
        float2 sinUv = (p / size.y) * 0.6;
        p.x += sin(_HplInsanity.x + sinUv.y) * amp.x;
        p.y += sin(_HplInsanity.x + sinUv.x * 1.83) * amp.y;
        return float4(SampleSource(float2(p.x, size.y - p.y)), 1.0);
    }

    // HPL mainmenu_screen_blur_frag.glsl: the original seven bilinear taps, in gamma space.
    float4 MenuBlur(Varyings v, float2 axis)
    {
        const float weights[7] = { 0.1, 0.25, 0.3, 0.5, 0.3, 0.25, 0.1 };
        const float offsets[7] = { -4.35, -2.5, -0.75, 0.0, 0.75, 2.5, 4.35 };
        float3 color = 0;
        for (int i = 0; i < 7; i++) color += SampleSource(v.positionCS.xy + axis * offsets[i]) * weights[i];
        return float4(color / 1.8, 1);
    }
    float4 MenuBlurH(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        return MenuBlur(v, float2(1, 0));
    }
    float4 MenuBlurV(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        return MenuBlur(v, float2(0, 1));
    }
    float4 MenuComposite(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        float3 sharp = LOAD_TEXTURE2D_X(_HplMenuSharp, uint2(v.positionCS.xy)).rgb;
        return float4(SRGBToLinear(lerp(sharp, LoadSource(int2(v.positionCS.xy)), _HplMenuBlend)), 1);
    }
    float4 InventoryComposite(Varyings v) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(v);
        float3 color = LoadSource(int2(v.positionCS.xy));
        // inventory_screen_effect_frag.glsl: dark sepia, no menu blur.
        float3 sepia = float3(dot(color, float3(0.39, 0.769, 0.189)),
                              dot(color, float3(0.349, 0.686, 0.168)),
                              dot(color, float3(0.272, 0.534, 0.131)));
        sepia = pow(max(0, sepia * 0.35), 1.5);
        return float4(SRGBToLinear(lerp(color, sepia, _HplMenuBlend)), 1);
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
        Pass
        {
            Name "Menu blur H"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment MenuBlurH
            ENDHLSL
        }
        Pass
        {
            Name "Menu blur V"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment MenuBlurV
            ENDHLSL
        }
        Pass
        {
            Name "Menu background"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment MenuComposite
            ENDHLSL
        }
        Pass
        {
            Name "Inventory background"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment InventoryComposite
            ENDHLSL
        }
        Pass
        {
            Name "Insanity"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment InsanityFrag
            ENDHLSL
        }
        Pass
        {
            Name "Gamma"
            ZWrite Off ZTest Always Blend Off Cull Off
            HLSLPROGRAM
            #pragma fragment GammaFrag
            ENDHLSL
        }
    }
    Fallback Off
}
