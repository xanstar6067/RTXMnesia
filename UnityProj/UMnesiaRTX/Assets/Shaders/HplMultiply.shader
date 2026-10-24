// HPL2 multiplicative blending (eMaterialBlendMode_Mul / MulX2) for HDRP, which has no such blend mode.
// Used by Decal and Translucent materials with BlendMode="Mul" / "MulX2": dirt and moisture decals, puddles,
// bottles, window glass.
//
// HPL2 decals multiply the G-buffer color (RendererDeferred.cpp RenderDecals, deferred_decal_frag.glsl:
// texture × vertex color, alpha test 0.01), translucent objects multiply the lit frame. Both are done here on the
// lit color: albedo × t lit equals lit × t up to specular and emission. The engine works in gamma space, so the
// factor is computed there: t_gamma × color (× 2 for MulX2), clamped like the 8-bit target for Mul, and
// linearized; dst_lin × factor_lin then matches dst_gamma × factor_gamma.
// Fog: HPL2 multiplies the decals into the G-buffer, before the fog; here the fog (the areas, then the world fog,
// HplFogAreaPass) is already in the frame: dst = surface × transmittance + haze. Multiplying all of it would darken the
// haze too, and in a dark foggy room a decal or a bottle showed as a spot blacker than the darkness around it. So only
// what is behind the haze is multiplied: out = (dst − haze) × factor + haze = dst × factor + haze × (1 − factor), in
// two passes (no dual source blending): "ForwardOnly" multiplies, "SRPDefaultUnlit" (drawn after it, HDRP's transparent
// pass list) adds haze × (1 − factor). The haze is the world fog (_HplFog, HplLighting.SetTranslucentFog, colour
// _HplWorldFogRadiance of HplFogAreaPass) and the fog areas (HplFogAreas.hlsl) between the camera and the fragment.
// For decals this is HPL2's order; for translucent glass HPL2 fades the factor to 1 by the fog opacity instead,
// which darkened the haze in front of the glass as well.
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
            #pragma fragment FragMultiply
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

            float4 _HplFog; // x: fog opacity at the end (0 = no fog), y: start, z: end - start, w: falloff exponent (HPL2 world fog)
            float4 _HplWorldFogRadiance; // rgb: the world fog color as radiance (HplFogAreaPass)

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

            // The factor (linear) and the haze between the camera and the fragment.
            void Shade(Varyings input, out float3 factor, out float3 haze)
            {
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float alpha = t.a * _BaseColor.a;
                clip(alpha - _HplAlphaLimit);

                float3 gamma = LinearToSRGB(t.rgb) * _BaseColor.rgb * _HplScale;
                if (_HplScale <= 1.0) gamma = saturate(gamma);
                factor = pow(max(gamma, 0.0), 2.2); // HPL2 multiplies the screen value; a gamma 2.2 monitor shows value^2.2

                haze = 0.0;
                float viewDepth = max(-TransformWorldToView(input.positionRWS).z, 1e-6);
                float exposure = GetCurrentExposureMultiplier();
                // The fog areas are drawn first, then the world fog over them (HplFogAreaPass).
                if (_HplFogAreaCount > 0)
                {
                    float3 cameraRWS = GetCurrentViewPosition();
                    float transmittance;
                    HplFogAreasAlongRay(GetAbsolutePositionWS(cameraRWS), (input.positionRWS - cameraRWS) / viewDepth, viewDepth, false, exposure, haze, transmittance);
                }
                if (_HplFog.x > 0.0)
                {
                    float fogAmount = _HplFog.z > 0.0 ? saturate((viewDepth - _HplFog.y) / _HplFog.z) : (viewDepth > _HplFog.y ? 1.0 : 0.0);
                    float fogAlpha = fogAmount > 0.0 ? pow(fogAmount, _HplFog.w) * _HplFog.x : 0.0;
                    haze = haze * (1.0 - fogAlpha) + _HplWorldFogRadiance.rgb * exposure * fogAlpha;
                }
            }

            float4 FragMultiply(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 factor, haze;
                Shade(input, factor, haze);
                return float4(factor, 1.0);
            }

            float4 FragHaze(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 factor, haze;
                Shade(input, factor, haze);
                // Negative where the factor brightens (MulX2 above 1): the blend is in float, dst ≥ haze.
                return float4(haze * (1.0 - factor), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Haze"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FragHaze
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

            float4 _HplFog; // x: fog opacity at the end (0 = no fog), y: start, z: end - start, w: falloff exponent (HPL2 world fog)
            float4 _HplWorldFogRadiance; // rgb: the world fog color as radiance (HplFogAreaPass)

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

            // The factor (linear) and the haze between the camera and the fragment.
            void Shade(Varyings input, out float3 factor, out float3 haze)
            {
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float alpha = t.a * _BaseColor.a;
                clip(alpha - _HplAlphaLimit);

                float3 gamma = LinearToSRGB(t.rgb) * _BaseColor.rgb * _HplScale;
                if (_HplScale <= 1.0) gamma = saturate(gamma);
                factor = pow(max(gamma, 0.0), 2.2); // HPL2 multiplies the screen value; a gamma 2.2 monitor shows value^2.2

                haze = 0.0;
                float viewDepth = max(-TransformWorldToView(input.positionRWS).z, 1e-6);
                float exposure = GetCurrentExposureMultiplier();
                // The fog areas are drawn first, then the world fog over them (HplFogAreaPass).
                if (_HplFogAreaCount > 0)
                {
                    float3 cameraRWS = GetCurrentViewPosition();
                    float transmittance;
                    HplFogAreasAlongRay(GetAbsolutePositionWS(cameraRWS), (input.positionRWS - cameraRWS) / viewDepth, viewDepth, false, exposure, haze, transmittance);
                }
                if (_HplFog.x > 0.0)
                {
                    float fogAmount = _HplFog.z > 0.0 ? saturate((viewDepth - _HplFog.y) / _HplFog.z) : (viewDepth > _HplFog.y ? 1.0 : 0.0);
                    float fogAlpha = fogAmount > 0.0 ? pow(fogAmount, _HplFog.w) * _HplFog.x : 0.0;
                    haze = haze * (1.0 - fogAlpha) + _HplWorldFogRadiance.rgb * exposure * fogAlpha;
                }
            }

            float4 FragMultiply(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 factor, haze;
                Shade(input, factor, haze);
                return float4(factor, 1.0);
            }

            float4 FragHaze(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 factor, haze;
                Shade(input, factor, haze);
                // Negative where the factor brightens (MulX2 above 1): the blend is in float, dst ≥ haze.
                return float4(haze * (1.0 - factor), 1.0);
            }
            ENDHLSL
        }
    }
}
