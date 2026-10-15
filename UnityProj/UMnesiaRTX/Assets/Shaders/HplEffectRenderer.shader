// cLuxEffectRenderer for RTXMnesia.Effects.HplEffectRendererPass: item flash (game_object_flash_frag.glsl) and the
// focus outline (game_outline_frag.glsl, stencil mask, posteffect_bloom_blur_frag.glsl). Gamma values like HPL2,
// linearized when added to the HDR target. Mesh passes get the object's diffuse texture and alpha test per draw.
Shader "Hidden/RTXMnesia/HplEffectRenderer"
{
    HLSLINCLUDE
    #pragma target 4.5
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

        TEXTURE2D_X(_HplSource);
        float2 _HplSourceSize;
        float2 _HplTargetSize;
        float4 _HplClipRect; // x min, y min, x max, y max in target pixels (y down)

        float LoadSource(int2 p)
        {
            p = clamp(p, int2(0, 0), int2(_HplSourceSize) - 1);
            return LOAD_TEXTURE2D_X(_HplSource, p).r;
        }

        // Bilinear read at a source pixel position (texel centers at .5), like texture2DRect with linear filtering.
        float SampleSource(float2 pos)
        {
            float2 f = pos - 0.5;
            int2 i = (int2)floor(f);
            float2 t = f - i;
            return lerp(lerp(LoadSource(i), LoadSource(i + int2(1, 0)), t.x),
                        lerp(LoadSource(i + int2(0, 1)), LoadSource(i + int2(1, 1)), t.x), t.y);
        }

        float Blur(Varyings v, float2 direction)
        {
            const float mul[5] = { 0.25, 0.3, 0.5, 0.3, 0.25 };
            const float offset[5] = { -2.5, -0.75, 0.0, 0.75, 2.5 };
            // The quad maps the target onto the whole source (DrawQuad with the source size).
            float2 pos = v.positionCS.xy / _HplTargetSize * _HplSourceSize;
            float amount = 0.0;
            for (int i = 0; i < 5; i++) amount += SampleSource(pos + direction * offset[i]) * mul[i];
            return amount / 1.6;
        }

        TEXTURE2D(_HplTexture);
        SAMPLER(sampler_HplTexture);
        TEXTURE2D_X(_HplMask);
        float _HplAlphaTest;  // alpha test limit, < 0: none
        float _HplUseAlpha;   // the outline color takes the texture alpha
        float _HplColorMul;

        struct MeshAttributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        struct MeshVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 normalVS : TEXCOORD1;
        };

        MeshVaryings MeshVert(MeshAttributes input)
        {
            MeshVaryings output;
            float3 positionRWS = TransformObjectToWorld(input.positionOS);
            output.positionCS = TransformWorldToHClip(positionRWS);
            output.uv = input.uv;
            output.normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS));
            return output;
        }

        float4 SampleObject(float2 uv)
        {
            float4 t = SAMPLE_TEXTURE2D(_HplTexture, sampler_HplTexture, uv);
            if (_HplAlphaTest >= 0.0) clip(t.a - _HplAlphaTest);
            return t;
        }
        ENDHLSL

        // 0: flash. Diffuse × (0.5, 0.5, 1) × rim light × alpha, drawn twice in HPL2 (so × 2), on the object itself.
        Pass
        {
            Name "Flash"
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Back
            Blend One One

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment Frag

            float4 Frag(MeshVaryings input) : SV_Target
            {
                float4 t = SAMPLE_TEXTURE2D(_HplTexture, sampler_HplTexture, input.uv);
                float rim = 1.0 - abs(dot(normalize(input.normalVS), float3(0.0, 0.0, -1.0)));
                float3 gamma = LinearToSRGB(t.rgb) * float3(0.5, 0.5, 1.0) * rim * _HplColorMul * 2.0;
                return float4(SRGBToLinear(gamma), 0.0);
            }
            ENDHLSL
        }

        // 1: outline mask (HPL2's stencil): 1 where the object is the visible surface.
        Pass
        {
            Name "OutlineMask"
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Back
            Blend Off

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment Frag

            float4 Frag(MeshVaryings input) : SV_Target
            {
                SampleObject(input.uv);
                return 1.0;
            }
            ENDHLSL
        }

        // 2: outline color: the grown object, blue 0.5 (× texture alpha) where the mask is not set.
        Pass
        {
            Name "OutlineColor"
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend Off

            HLSLPROGRAM
            #pragma vertex MeshVert
            #pragma fragment Frag

            float4 Frag(MeshVaryings input) : SV_Target
            {
                if (LOAD_TEXTURE2D_X(_HplMask, uint2(input.positionCS.xy)).r > 0.5) discard;
                float4 t = SAMPLE_TEXTURE2D(_HplTexture, sampler_HplTexture, input.uv);
                return 0.5 * (_HplUseAlpha > 0.5 ? t.a : 1.0);
            }
            ENDHLSL
        }

        // 3, 4: blur (posteffect_bloom_blur_frag.glsl): 5 bilinear taps, offsets in source pixels (Blur above).

        Pass
        {
            Name "BlurVertical"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings v) : SV_Target { return Blur(v, float2(0.0, 1.0)); }
            ENDHLSL
        }

        Pass
        {
            Name "BlurHorizontal"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings v) : SV_Target { return Blur(v, float2(1.0, 0.0)); }
            ENDHLSL
        }

        // 5: composite: the blurred blue added outside the mask, inside the clip rect.
        Pass
        {
            Name "Composite"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(Varyings v) : SV_Target
            {
                float2 pos = v.positionCS.xy;
                if (any(pos < _HplClipRect.xy) || any(pos > _HplClipRect.zw)) discard;
                if (LOAD_TEXTURE2D_X(_HplMask, uint2(pos)).r > 0.5) discard;
                float blue = SampleSource(pos / _HplTargetSize * _HplSourceSize);
                return float4(0.0, 0.0, SRGBToLinear(blue), 0.0);
            }
            ENDHLSL
        }
    }
}
