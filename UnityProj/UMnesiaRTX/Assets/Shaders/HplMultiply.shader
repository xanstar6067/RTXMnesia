// HPL2 multiplicative blending (eMaterialBlendMode_Mul / MulX2) for HDRP, which has no such blend mode.
// Used by Translucent materials with BlendMode="Mul" / "MulX2" and by MulX2 decals (Mul decals are DBuffer decals,
// HplMulDecal.shader, since 2026-11-04): puddles, bottles, window glass.
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
// The haze is capped by the fog of the opaque surface behind (depth buffer, see Haze): what the frame holds behind the
// fragment; only that can be taken out of dst without going negative (a hanging web in front of a gap, 2026-11-27).
// Depth: HPL2 tests decals LessOrEqual without a bias (RenderDecals). The offset here is only the constant unit, no
// slope factor: at a grazing view the slope term pulled a decal over geometry millimetres above it, and the dirt
// under the pebbles (01_old_archives, pebbles 4 mm above dirt_floor01) darkened them in a band beyond some distance.
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
            Offset 0, -1

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

            // The factor (linear): texture x color in gamma (x 2 for MulX2), as the screen showed it.
            float3 Factor(Varyings input)
            {
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float alpha = t.a * _BaseColor.a;
                clip(alpha - _HplAlphaLimit);

                float3 gamma = LinearToSRGB(t.rgb) * _BaseColor.rgb * _HplScale;
                if (_HplScale <= 1.0) gamma = saturate(gamma);
                return pow(max(gamma, 0.0), 2.2); // HPL2 multiplies the screen value; a gamma 2.2 monitor shows value^2.2
            }

            // The fog drawn in the frame between the camera and view depth `depth` along the pixel's ray (sky: no
            // surface there, the world fog skips it, HplFogAreaPass).
            float3 FogTo(float3 positionRWS, float viewDepth, float depth, bool sky)
            {
                float3 haze = 0.0;
                float exposure = GetCurrentExposureMultiplier();
                // The fog areas are drawn first, then the world fog over them (HplFogAreaPass).
                if (_HplFogAreaCount > 0)
                {
                    float3 cameraRWS = GetCurrentViewPosition();
                    float transmittance;
                    HplFogAreasAlongRay(GetAbsolutePositionWS(cameraRWS), (positionRWS - cameraRWS) / viewDepth, depth, sky, exposure, haze, transmittance);
                }
                if (_HplFog.x > 0.0 && !sky)
                {
                    float fogAmount = _HplFog.z > 0.0 ? saturate((depth - _HplFog.y) / _HplFog.z) : (depth > _HplFog.y ? 1.0 : 0.0);
                    float fogAlpha = fogAmount > 0.0 ? pow(fogAmount, _HplFog.w) * _HplFog.x : 0.0;
                    haze = haze * (1.0 - fogAlpha) + _HplWorldFogRadiance.rgb * exposure * fogAlpha;
                }
                return haze;
            }

            // The haze kept out of the multiplication: the fog in front of the fragment, but never more than the fog
            // the frame holds there, the fog of the opaque surface behind it (depth buffer). A decal on a surface has
            // the same fog; a web hanging in the air (web_spider01, 02_entrance_hall) has a far wall behind it, where a
            // later fog layer covers part of an earlier one, or nothing at all (a gap to the background, no fog drawn):
            // there dst < haze, and dst x factor + haze x (1 - factor) went negative for MulX2 (black points that DLSS
            // spread and made flicker, 2026-11-27). With haze <= dst it is >= 0.
            float3 Haze(Varyings input)
            {
                float viewDepth = max(-TransformWorldToView(input.positionRWS).z, 1e-6);
                float rawDepth = LoadCameraDepth(uint2(input.positionCS.xy));
                bool sky = rawDepth == UNITY_RAW_FAR_CLIP_VALUE;
                float sceneDepth = sky ? 0.0 : LinearEyeDepth(rawDepth, _ZBufferParams);
                return min(FogTo(input.positionRWS, viewDepth, viewDepth, false), FogTo(input.positionRWS, viewDepth, sceneDepth, sky));
            }

            float4 FragMultiply(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return float4(Factor(input), 1.0);
            }

            float4 FragHaze(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Negative where the factor brightens (MulX2 above 1): the blend is in float, dst >= haze (Haze).
                return float4(Haze(input) * (1.0 - Factor(input)), 1.0);
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
            Offset 0, -1

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

            // The factor (linear): texture x color in gamma (x 2 for MulX2), as the screen showed it.
            float3 Factor(Varyings input)
            {
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float alpha = t.a * _BaseColor.a;
                clip(alpha - _HplAlphaLimit);

                float3 gamma = LinearToSRGB(t.rgb) * _BaseColor.rgb * _HplScale;
                if (_HplScale <= 1.0) gamma = saturate(gamma);
                return pow(max(gamma, 0.0), 2.2); // HPL2 multiplies the screen value; a gamma 2.2 monitor shows value^2.2
            }

            // The fog drawn in the frame between the camera and view depth `depth` along the pixel's ray (sky: no
            // surface there, the world fog skips it, HplFogAreaPass).
            float3 FogTo(float3 positionRWS, float viewDepth, float depth, bool sky)
            {
                float3 haze = 0.0;
                float exposure = GetCurrentExposureMultiplier();
                // The fog areas are drawn first, then the world fog over them (HplFogAreaPass).
                if (_HplFogAreaCount > 0)
                {
                    float3 cameraRWS = GetCurrentViewPosition();
                    float transmittance;
                    HplFogAreasAlongRay(GetAbsolutePositionWS(cameraRWS), (positionRWS - cameraRWS) / viewDepth, depth, sky, exposure, haze, transmittance);
                }
                if (_HplFog.x > 0.0 && !sky)
                {
                    float fogAmount = _HplFog.z > 0.0 ? saturate((depth - _HplFog.y) / _HplFog.z) : (depth > _HplFog.y ? 1.0 : 0.0);
                    float fogAlpha = fogAmount > 0.0 ? pow(fogAmount, _HplFog.w) * _HplFog.x : 0.0;
                    haze = haze * (1.0 - fogAlpha) + _HplWorldFogRadiance.rgb * exposure * fogAlpha;
                }
                return haze;
            }

            // The haze kept out of the multiplication: the fog in front of the fragment, but never more than the fog
            // the frame holds there, the fog of the opaque surface behind it (depth buffer). A decal on a surface has
            // the same fog; a web hanging in the air (web_spider01, 02_entrance_hall) has a far wall behind it, where a
            // later fog layer covers part of an earlier one, or nothing at all (a gap to the background, no fog drawn):
            // there dst < haze, and dst x factor + haze x (1 - factor) went negative for MulX2 (black points that DLSS
            // spread and made flicker, 2026-11-27). With haze <= dst it is >= 0.
            float3 Haze(Varyings input)
            {
                float viewDepth = max(-TransformWorldToView(input.positionRWS).z, 1e-6);
                float rawDepth = LoadCameraDepth(uint2(input.positionCS.xy));
                bool sky = rawDepth == UNITY_RAW_FAR_CLIP_VALUE;
                float sceneDepth = sky ? 0.0 : LinearEyeDepth(rawDepth, _ZBufferParams);
                return min(FogTo(input.positionRWS, viewDepth, viewDepth, false), FogTo(input.positionRWS, viewDepth, sceneDepth, sky));
            }

            float4 FragMultiply(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return float4(Factor(input), 1.0);
            }

            float4 FragHaze(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Negative where the factor brightens (MulX2 above 1): the blend is in float, dst >= haze (Haze).
                return float4(Haze(input) * (1.0 - Factor(input)), 1.0);
            }
            ENDHLSL
        }
    }
}
