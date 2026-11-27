// HPL2 translucent materials with BlendMode Alpha / PremulAlpha (cMaterialType_Translucent,
// core/shaders/deferred_transparent_frag.glsl) for HDRP: cobwebs, blood pools and trails, cracks, chalk, hair, glass.
//
// HPL2 does not light translucent objects: the fragment is texture × vertex color (here _BaseColor, the decal / billboard
// color of the map), its alpha faded by the world fog (UseFog: the fog was drawn before, so fogged objects fade out)
// and by the fog areas between the camera and the object (iRenderer::GetFogAreaVisibilityForObject). The color is
// multiplied by the light level, 1 unless the material is AffectedByLightLevel: then the sum of max color ×
// (1 − distance / radius) of the lights reaching the centre of the object, at most 1 (SetupObjectSpecificData),
// per renderer from World/HplTranslucentLightLevel (_HplLightLevel in a property block).
// With a CubeMap (UseEnvMap) the reflection is added: cube map (HPL2 world directions: Unity's with x mirrored,
// as HplWater) × CubeMapAlpha alpha × Fresnel × alpha (fog) × light level, plus the rim light of the same reflection.
// Not ported: the refraction offset (Refraction=true: HPL2 blends with the background itself; plain blending here
// gives the same mix without the distortion) and normal maps on the reflection (none of these materials has one).
// HPL2 renders in gamma space: the math is done on sRGB values and linearized at the end. Like HDRP/Unlit the output
// is not exposed, so the texture shows as in HPL2 with any lighting set.
Shader "RTXMnesia/HplTranslucent"
{
    Properties
    {
        _BaseColorMap ("Diffuse", 2D) = "white" {}
        _BaseColor ("Color (HPL, gamma)", Color) = (1, 1, 1, 1)
        [Enum(Alpha,3,PremulAlpha,4)] _HplBlendMode ("HPL blend mode", Float) = 3
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 10
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Toggle] _HplEnvMap ("Cube map reflection", Float) = 0
        _CubeMap ("Cube map", Cube) = "black" {}
        _CubeMapAlpha ("Cube map alpha (A)", 2D) = "white" {}
        _FresnelBias ("Fresnel bias", Float) = 0.2
        _FresnelPow ("Fresnel power", Float) = 8
        _RimLightMul ("Rim light", Float) = 0
        _RimLightPow ("Rim light power", Float) = 8
        [Toggle] _HplAffectedByLightLevel ("Affected by light level", Float) = 0
        _HplLightLevel ("Light level (per renderer)", Float) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]

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
            TEXTURECUBE(_CubeMap);
            SAMPLER(sampler_CubeMap);
            TEXTURE2D(_CubeMapAlpha);
            SAMPLER(sampler_CubeMapAlpha);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float4 _BaseColor;
                float _HplBlendMode;
                float _SrcBlend;
                float _DstBlend;
                float _Cull;
                float _HplEnvMap;
                float _FresnelBias;
                float _FresnelPow;
                float _RimLightMul;
                float _RimLightPow;
                float _HplAffectedByLightLevel;
                float _HplLightLevel;
            CBUFFER_END

            // HplLighting.SetTranslucentFog: fog start, end - start, 1 - fog alpha, falloff exponent (alpha 1 = off).
            float4 _HplTranslucentFog;
            float _HplHdrActive;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionRWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float viewDepth : TEXCOORD3;
                float fogAreaVisibility : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // iRenderer::GetFogAreaVisibilityForObject: the ray from the camera to the center of the bounding volume.
            float FogAreaVisibility()
            {
                float3 boundsMin, boundsMax;
                GetAbsoluteWorldRendererBounds(boundsMin, boundsMax);
                float3 center = all(boundsMin == boundsMax) ? GetAbsolutePositionWS(GetObjectToWorldMatrix()._m03_m13_m23) : (boundsMin + boundsMax) * 0.5;
                return HplFogAreaVisibility(center, GetAbsolutePositionWS(GetCurrentViewPosition()));
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionRWS = TransformObjectToWorld(input.positionOS); // camera-relative
                output.positionCS = TransformWorldToHClip(positionRWS);
                output.positionRWS = positionRWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseColorMap);
                output.viewDepth = -TransformWorldToView(positionRWS).z;
                output.fogAreaVisibility = FogAreaVisibility();
                return output;
            }

            // helper_reflection.glsl
            float Fresnel(float eDotN, float bias, float power)
            {
                float facing = 1.0 - eDotN;
                return max(bias + (1.0 - bias) * pow(facing, power), 0.0);
            }

            float3 ToGamma(float3 c) { return LinearToSRGB(_HplHdrActive > 0.5 ? max(c, 0) : saturate(c)); }

            float4 Frag(Varyings input, bool frontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float3 color = LinearToSRGB(t.rgb) * _BaseColor.rgb;
                float alpha = t.a * _BaseColor.a;

                float finalAlpha = 1.0;
                if (_HplTranslucentFog.w > 0.0) // unset global: no fog
                {
                    float fogAmount = saturate((input.viewDepth - _HplTranslucentFog.x) / max(_HplTranslucentFog.y, 1e-5));
                    fogAmount = pow(fogAmount, _HplTranslucentFog.w);
                    finalAlpha = _HplTranslucentFog.z * fogAmount + (1.0 - fogAmount);
                }
                finalAlpha *= input.fogAreaVisibility;

                float lightLevel = _HplAffectedByLightLevel > 0.5 ? saturate(_HplLightLevel) : 1.0;

                if (_HplBlendMode < 3.5) // Alpha (SrcAlpha, OneMinusSrcAlpha)
                {
                    alpha *= finalAlpha;
                    color *= lightLevel;
                }
                else                     // PremulAlpha (One, OneMinusSrcAlpha)
                {
                    color *= finalAlpha;
                    alpha *= finalAlpha;
                    color *= lightLevel;
                }

                if (_HplEnvMap > 0.5)
                {
                    float3 n = normalize(input.normalWS);
                    if (!frontFace) n = -n;
                    float3 eye = normalize(input.positionRWS);
                    float eDotN = max(dot(-eye, n), 0.0);
                    float fresnel = Fresnel(eDotN, _FresnelBias, _FresnelPow);
                    float3 r = reflect(eye, n);
                    float3 reflection = ToGamma(SAMPLE_TEXTURECUBE(_CubeMap, sampler_CubeMap, float3(-r.x, r.y, r.z)).rgb);
                    reflection *= SAMPLE_TEXTURE2D(_CubeMapAlpha, sampler_CubeMapAlpha, input.uv).a;
                    color += reflection * fresnel * finalAlpha * lightLevel;
                    // Rim light: 1 − |normal · view axis| in view space.
                    float rim = pow(1.0 - abs(TransformWorldToViewDir(n, true).z), _RimLightPow) * _RimLightMul;
                    color += reflection * rim * finalAlpha * lightLevel;
                }

                return float4(SRGBToLinear(max(color, 0.0)), alpha);
            }
            ENDHLSL
        }
    }
}
