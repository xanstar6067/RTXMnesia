// HPL2 particles (iParticleEmitter + Translucent material, deferred_transparent_frag.glsl) for HDRP.
//
// The mesh comes from HplParticleEmitter: per particle a quad of four vertices with the particle color (HPL,
// gamma). The quad is turned to the camera here, so one mesh serves every camera:
//   uv2.w = 0  point: position = particle center, uv1.xy = corner offset in view space (size and spin applied);
//   uv2.w = 1  line: position = this end, uv1.xyz = the other end, uv1.w = +1 head / -1 tail, uv2.xyz = size, side;
//              the quad runs along the view-space direction between the ends (ParticleEmitter.cpp LINE);
//   uv2.w = 2  axis: position is the final world vertex.
// The system's color and distance fade (UpdateGraphicsForViewport's colorMul) come per renderer; a fully faded
// system is not drawn. The fragment works in gamma like HPL2: texture × vertex color, the translucent fog fade of
// HPL2 (the world fog was drawn before, so fogged particles fade out), then the blend mode's math; the result is
// linearized for the HDR target. HDRP/Unlit is not exposed either, so the HPL color shows as is.
Shader "RTXMnesia/HplParticle"
{
    Properties
    {
        _BaseColorMap ("Texture", 2D) = "white" {}
        [Enum(Add,0,Mul,1,MulX2,2,Alpha,3,PremulAlpha,4)] _HplBlendMode ("HPL blend mode", Float) = 3
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 10
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 4
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
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Assets/Shaders/HplFogAreas.hlsl"

            TEXTURE2D(_BaseColorMap);
            SAMPLER(sampler_BaseColorMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float _HplBlendMode;
                float _SrcBlend;
                float _DstBlend;
                float _ZTest;
            CBUFFER_END

            // Per renderer (MaterialPropertyBlock), from the particle system.
            float4 _HplColorMul;     // cParticleSystem color
            float4 _HplFadeCenter;   // xyz: system position (world), w: fade at distance
            float4 _HplFadeDist;     // min start, min end, max start, max end
            float _HplMulRgbByAlpha; // MultiplyRGBWithAlpha of the emitter

            // HplLighting.SetTranslucentFog: fog start, end - start, 1 - fog alpha, falloff exponent (alpha 1 = off).
            float4 _HplTranslucentFog;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 data1 : TEXCOORD1;
                float4 data2 : TEXCOORD2;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float viewDepth : TEXCOORD1;
                float fogAreaVisibility : TEXCOORD2;
            };

            // UpdateGraphicsForViewport: alpha by the distance of the system to the camera.
            float4 ColorMul()
            {
                float4 mul = _HplColorMul;
                if (_HplFadeCenter.w > 0.0)
                {
                    float dist = length(GetCameraRelativePositionWS(_HplFadeCenter.xyz));
                    float minStart = _HplFadeDist.x, minEnd = _HplFadeDist.y, maxStart = _HplFadeDist.z, maxEnd = _HplFadeDist.w;
                    if (minStart > 0.0 && dist < minStart)
                        mul.a = dist <= minEnd ? 0.0 : mul.a * (dist - minEnd) / (minStart - minEnd);
                    if (maxStart > 0.0 && dist > maxStart)
                        mul.a = dist >= maxEnd ? 0.0 : mul.a * (1.0 - (dist - maxStart) / (maxEnd - maxStart));
                }
                if (_HplMulRgbByAlpha > 0.0) mul.rgb *= mul.a;
                return mul;
            }

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
                float3 positionRWS = TransformObjectToWorld(input.positionOS);
                float3 positionVS = TransformWorldToView(positionRWS);
                int mode = (int)(input.data2.w + 0.5);
                if (mode == 0)
                {
                    positionVS.xy += input.data1.xy;
                }
                else if (mode == 1)
                {
                    float3 otherVS = TransformWorldToView(TransformObjectToWorld(input.data1.xyz));
                    float3 d = positionVS - otherVS;
                    float3 dirY = all(d == 0.0) ? float3(0.0, input.data1.w, 0.0) : normalize(d);
                    float2 dirX = float2(dirY.y, -dirY.x);
                    dirX = dot(dirX, dirX) > 0.0 ? normalize(dirX) : float2(1.0, 0.0);
                    positionVS += dirY * input.data2.y;
                    positionVS.xy += dirX * input.data2.x * input.data2.z;
                }

                float4 mul = ColorMul();
                output.positionCS = mul.a > 0.0 ? TransformWViewToHClip(positionVS) : float4(0.0, 0.0, 0.0, -1.0);
                output.uv = TRANSFORM_TEX(input.uv, _BaseColorMap);
                output.color = input.color * mul;
                output.viewDepth = -positionVS.z;
                output.fogAreaVisibility = FogAreaVisibility();
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float3 color = LinearToSRGB(t.rgb) * input.color.rgb;
                float alpha = t.a * input.color.a;

                float finalAlpha = 1.0;
                if (_HplTranslucentFog.w > 0.0) // unset global: no fog
                {
                    float fogAmount = saturate((input.viewDepth - _HplTranslucentFog.x) / max(_HplTranslucentFog.y, 1e-5));
                    fogAmount = pow(fogAmount, _HplTranslucentFog.w);
                    finalAlpha = _HplTranslucentFog.z * fogAmount + (1.0 - fogAmount);
                }

                finalAlpha *= input.fogAreaVisibility;

                int mode = (int)(_HplBlendMode + 0.5);
                if (mode == 0)      // Add (One, One)
                {
                    color *= finalAlpha;
                    alpha = 1.0;
                }
                else if (mode == 1) // Mul (DstColor, Zero)
                {
                    color += (1.0 - color) * (1.0 - finalAlpha);
                    alpha = 1.0;
                }
                else if (mode == 2) // MulX2 (DstColor, SrcColor) = dst × 2 src, as (DstColor, Zero) with 2 src
                {
                    color = (color * finalAlpha + 0.5 * (1.0 - finalAlpha)) * 2.0;
                    alpha = 1.0;
                }
                else if (mode == 3) // Alpha (SrcAlpha, OneMinusSrcAlpha)
                {
                    alpha *= finalAlpha;
                }
                else                // PremulAlpha (One, OneMinusSrcAlpha)
                {
                    color *= finalAlpha;
                    alpha *= finalAlpha;
                }
                return float4(SRGBToLinear(max(color, 0.0)), alpha);
            }
            ENDHLSL
        }
    }
}
