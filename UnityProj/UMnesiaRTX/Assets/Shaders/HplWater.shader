// HPL2 water surface (cMaterialType_Water, core/shaders/water_surface_frag.glsl) for HDRP.
//
// Two scrolling, sine-warped copies of the normal map (70 % / 30 %) give the ripples; the diffuse map is read at
// the first one. The color behind the water is the HDRP color pyramid (the HPL2 refraction texture: the frame
// before translucent objects), offset by the ripple normal / view depth. Reflection, by _ReflectionMode:
//   0  none: the fake shading of the original (diffuse 0.5..1 and specular from a fixed light in tangent space);
//   1  cube map, sampled with HPL2 (unmirrored) world directions;
//   2  world reflection: a mirror camera's frame (HplWaterReflection) at the same distorted screen position,
//      black when no reflection was rendered for this surface (as HPL2 clears the reflection texture).
// Fresnel bias / power and the reflection fading by view depth as the original. The world fog of the map is
// applied with the HPL2 formula (_HplTranslucentFog, _HplFogColor from HplLighting.SetTranslucentFog).
// HPL2 renders in gamma space: the color math is done on sRGB values and linearized at the end.
Shader "RTXMnesia/HplWater"
{
    Properties
    {
        _BaseColorMap ("Diffuse", 2D) = "white" {}
        [Normal] _NormalMap ("Normal map", 2D) = "bump" {}
        _CubeMap ("Reflection cube map", Cube) = "black" {}
        _ReflectionMode ("Reflection (0 none, 1 cube map, 2 world)", Float) = 0
        _RefractionScale ("Refraction scale", Float) = 0.1
        _FresnelBias ("Fresnel bias", Float) = 0.2
        _FresnelPow ("Fresnel power", Float) = 8
        _WaveSpeed ("Wave speed", Float) = 1
        _WaveAmplitude ("Wave amplitude (x 0.04)", Float) = 0.04
        _WaveFreq ("Wave frequency (x 10)", Float) = 10
        _ReflectionFade ("Reflection fade (start, length, on, end)", Vector) = (0, 0, 0, 0)
        _OcclusionCullReflection ("Occlusion cull world reflection", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Blend One Zero
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_BaseColorMap);
            SAMPLER(sampler_BaseColorMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURECUBE(_CubeMap);
            SAMPLER(sampler_CubeMap);
            TEXTURE2D(_HplReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float4 _NormalMap_ST;
                float _ReflectionMode;
                float _RefractionScale;
                float _FresnelBias;
                float _FresnelPow;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFreq;
                float4 _ReflectionFade;
                float _OcclusionCullReflection;
                float _Cull;
                // Set per renderer by HplWaterReflection (property block): 1 when the mirror frame is of this surface.
                float _HplReflectionActive;
                float _HplReflectionFlipY;
            CBUFFER_END

            // HplLighting.SetTranslucentFog: fog start, end - start, 1 - fog alpha, falloff exponent; fog color (gamma).
            float4 _HplTranslucentFog;
            float4 _HplFogColor;

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
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.viewDepth = -TransformWorldToView(positionRWS).z;
                return output;
            }

            // helper_reflection.glsl
            float Fresnel(float eDotN, float bias, float power)
            {
                float facing = 1.0 - eDotN;
                return max(bias + (1.0 - bias) * pow(facing, power), 0.0);
            }

            float _HplHdrActive;
            float3 ToGamma(float3 c) { return LinearToSRGB(_HplHdrActive > 0.5 ? max(c, 0) : saturate(c)); }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // The two uv coordinates.
                float t = _Time.y * _WaveSpeed;
                float t1 = t * 0.8;
                float2 uv1 = input.uv + t * 0.01;
                uv1.x += sin(t1 + uv1.y * _WaveFreq) * _WaveAmplitude;
                uv1.y += sin(t1 + uv1.x * _WaveFreq) * _WaveAmplitude;
                float t2 = t * -2.6;
                float2 uv2 = input.uv + t * -0.012;
                uv2.x += sin(t2 + uv2.y * _WaveFreq * 1.2) * _WaveAmplitude * 0.75;
                uv2.y += sin(t2 + uv2.x * _WaveFreq * 1.2) * _WaveAmplitude * 0.75;

                // Final normal in tangent space (Unity convention: the import flipped green).
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv1), 1.0);
                float3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv2), 1.0);
                float3 normalTS = normalize(n1 * 0.7 + n2 * 0.3);
                // The same normal in HPL2's tangent space (green down the image).
                float3 normalHpl = float3(normalTS.x, -normalTS.y, normalTS.z);

                float3 surface = ToGamma(SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, uv1).rgb);

                // Fog amount (UseFog).
                float fogAmount = 0.0;
                if (_HplTranslucentFog.w > 0.0)
                {
                    fogAmount = saturate((input.viewDepth - _HplTranslucentFog.x) / max(_HplTranslucentFog.y, 1e-5));
                    fogAmount = pow(fogAmount, _HplTranslucentFog.w) * (1.0 - _HplTranslucentFog.z);
                }

                // Refraction: gl_FragCoord + normal.xy × scale × screen width / view z (z < 0 in HPL2, max 10).
                float2 screenUv = input.positionCS.xy * _ScreenSize.zw;
                float invDist = min(1.0 / -max(input.viewDepth, 1e-4), 10.0);
                float2 offsetPx = normalHpl.xy * _RefractionScale * _ScreenSize.x * invDist;
                // gl_FragCoord grows upwards, the screen uv here downwards.
                float2 distortedUv = screenUv + float2(offsetPx.x, -offsetPx.y) * _ScreenSize.zw;
                float3 refraction = ToGamma(SampleCameraColor(saturate(distortedUv)));

                float3 color;
                int mode = (int)(_ReflectionMode + 0.5);
                if (mode > 0)
                {
                    float reflFade = 1.0;
                    if (_ReflectionFade.z > 0.0)
                        reflFade = 1.0 - saturate((input.viewDepth - _ReflectionFade.x) / max(_ReflectionFade.y, 1e-5));

                    // Screen normal from the derivatives of position and uv (cotangent frame).
                    float3 N = normalize(input.normalWS);
                    float3 dp1 = ddx(input.positionRWS), dp2 = ddy(input.positionRWS);
                    float2 duv1 = ddx(input.uv), duv2 = ddy(input.uv);
                    float3 dp2perp = cross(dp2, N), dp1perp = cross(N, dp1);
                    float3 T = dp2perp * duv1.x + dp1perp * duv2.x;
                    float3 B = dp2perp * duv1.y + dp1perp * duv2.y;
                    float invMax = rsqrt(max(max(dot(T, T), dot(B, B)), 1e-20));
                    float3 worldNormal = normalize(normalTS.x * T * invMax + normalTS.y * B * invMax + normalTS.z * N);

                    float3 eye = normalize(input.positionRWS);
                    float eDotN = max(dot(-eye, worldNormal), 0.0);
                    float fresnel = Fresnel(eDotN, _FresnelBias, _FresnelPow) * reflFade;

                    float3 reflection = 0.0;
                    if (mode == 1)
                    {
                        float3 r = reflect(eye, worldNormal);
                        // HPL2 world space is Unity's with x mirrored.
                        reflection = ToGamma(SAMPLE_TEXTURECUBE(_CubeMap, sampler_CubeMap, float3(-r.x, r.y, r.z)).rgb);
                    }
                    else if (_HplReflectionActive > 0.5)
                    {
                        float2 reflUv = saturate(distortedUv);
                        if (_HplReflectionFlipY > 0.5) reflUv.y = 1.0 - reflUv.y;
                        reflection = ToGamma(SAMPLE_TEXTURE2D_LOD(_HplReflectionTex, s_linear_clamp_sampler, reflUv, 0).rgb);
                    }
                    color = refraction * surface + reflection * fresnel;
                }
                else
                {
                    float3 lightDir = normalize(float3(0.5, 0.5, 0.5));
                    float lDotN = max(dot(lightDir, normalHpl), 0.0);
                    float diffuse = lDotN * 0.5 + 0.5;
                    float specular = pow(lDotN, 16.0);
                    color = surface * refraction * diffuse + specular;
                }

                color = color * (1.0 - fogAmount) + _HplFogColor.rgb * fogAmount;
                return float4(SRGBToLinear(max(color, 0.0)), 1.0);
            }
            ENDHLSL
        }
    }
}
