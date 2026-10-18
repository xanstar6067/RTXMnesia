// HPL2 multiplicative blending (eMaterialBlendMode_Mul / MulX2) for HDRP, which has no such blend mode.
// Used by Decal and Translucent materials with BlendMode="Mul" / "MulX2": dirt and moisture decals, puddles,
// bottles, window glass.
//
// HPL2 decals multiply the G-buffer color (RendererDeferred.cpp RenderDecals, deferred_decal_frag.glsl:
// texture × vertex color, alpha test 0.01), translucent objects multiply the lit frame. Both are done here on the
// lit color: albedo × t lit equals lit × t up to specular and emission. The engine works in gamma space, so the
// factor is computed there: t_gamma × color (× 2 for MulX2), clamped like the 8-bit target for Mul, and
// linearized; dst_lin × factor_lin then matches dst_gamma × factor_gamma.
// Fog is applied after decals in HPL2, here before: the factor is faded to 1 by the fog opacity (_HplFog, set by
// HplLighting.ApplyFog with the same formula as the HDRP fog), and likewise by the fog areas over the pixel
// (HplFogAreas.hlsl).
Shader "RTXMnesia/HplMultiply"
{
    Properties
    {
        _BaseColorMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color (HPL, gamma)", Vector) = (1, 1, 1, 1)
        _HplScale ("Scale (1 Mul, 2 MulX2)", Float) = 1
        _HplAlphaLimit ("Alpha test limit", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend DstColor Zero
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/Shaders/HplFogAreas.hlsl"

            TEXTURE2D(_BaseColorMap);
            SAMPLER(sampler_BaseColorMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float4 _BaseColor;
                float _HplScale;
                float _HplAlphaLimit;
                float _Cull;
            CBUFFER_END

            float4 _HplFog; // x: active, y: 1 / mean free path, z: max fog distance

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionRWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionRWS = TransformObjectToWorld(input.positionOS); // camera-relative
                output.positionCS = TransformWorldToHClip(positionRWS);
                output.positionRWS = positionRWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseColorMap);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float alpha = t.a * _BaseColor.a;
                clip(alpha - _HplAlphaLimit);

                float3 gamma = LinearToSRGB(t.rgb) * _BaseColor.rgb * _HplScale;
                if (_HplScale <= 1.0) gamma = saturate(gamma);
                float3 factor = SRGBToLinear(max(gamma, 0.0));

                if (_HplFog.x > 0.0)
                {
                    float d = min(length(input.positionRWS), _HplFog.z);
                    factor = lerp(factor, 1.0, 1.0 - exp(-d * _HplFog.y));
                }
                // The fog areas are drawn after the decals too (RenderFog): the same fade by their opacity here.
                if (_HplFogAreaCount > 0)
                {
                    float viewDepth = max(-TransformWorldToView(input.positionRWS).z, 1e-6);
                    float3 cameraRWS = GetCurrentViewPosition();
                    float3 areaColor;
                    float transmittance;
                    HplFogAreasAlongRay(GetAbsolutePositionWS(cameraRWS), (input.positionRWS - cameraRWS) / viewDepth, viewDepth, false, 1.0, areaColor, transmittance);
                    factor = lerp(factor, 1.0, 1.0 - transmittance);
                }
                return float4(factor, 1.0);
            }
            ENDHLSL
        }
    }
}
