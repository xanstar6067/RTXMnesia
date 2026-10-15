// HPL2 halo billboards (cBillboard with IsHalo) for HDRP: a translucent billboard whose color is scaled by how
// much of its light source is visible.
//
// HPL2 draws a box (HaloSourceSize at the billboard's matrix) twice as occlusion queries, with and without the
// depth test; halo alpha = visible samples / all samples × the part of the box's screen rect that is inside the
// screen (Billboard.cpp RetrieveOcculsionQuery). The billboard material usually has DepthTest="False", so the
// glow shows through the geometry in front of it and only fades by that alpha. HDRP has no occlusion queries for
// this, so the vertex shader samples a 4×4×4 grid of points in the box against the depth buffer: visible points /
// points on screen stand for the two queries. _HplHaloBox (per renderer, AmnesiaBillboard) maps the unit box
// (-0.5..0.5) to the world.
//
// The fragment is HPL2's translucent shader in gamma like HplParticle.shader: texture × color (× halo alpha),
// the translucent fog fade, the blend mode's math, linearized for the HDR target.
Shader "RTXMnesia/HplHalo"
{
    Properties
    {
        _BaseColorMap ("Texture", 2D) = "white" {}
        _HplColor ("Color (HPL, gamma)", Vector) = (1, 1, 1, 1)
        [Enum(Add,0,Mul,1,MulX2,2,Alpha,3,PremulAlpha,4)] _HplBlendMode ("HPL blend mode", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst blend", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth test", Float) = 8
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

            TEXTURE2D(_BaseColorMap);
            SAMPLER(sampler_BaseColorMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorMap_ST;
                float4 _HplColor;
                float _HplBlendMode;
                float _SrcBlend;
                float _DstBlend;
                float _ZTest;
            CBUFFER_END

            float4x4 _HplHaloBox;      // unit box → world (absolute), set per renderer
            float _HplHaloActive;      // 0: not a halo (alpha 1)
            float4 _HplTranslucentFog; // HplLighting.SetTranslucentFog

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float viewDepth : TEXCOORD1;
            };

            // A world point → (pixel, device depth); false behind the camera or off screen.
            bool ScreenPoint(float3 positionWS, out uint2 pixel, out float depth)
            {
                float4 clip = mul(UNITY_MATRIX_VP, float4(GetCameraRelativePositionWS(positionWS), 1.0));
                pixel = 0;
                depth = 0;
                if (clip.w <= 0.0) return false;
                float3 ndc = clip.xyz / clip.w;
                float2 uv = ndc.xy * 0.5 + 0.5;
            #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
            #endif
                if (any(uv < 0.0) || any(uv >= 1.0)) return false;
                pixel = (uint2)(uv * _ScreenSize.xy);
                depth = ndc.z;
                return true;
            }

            // cBillboard::RetrieveOcculsionQuery with sampled points instead of queries.
            float HaloAlpha()
            {
                if (_HplHaloActive <= 0.0) return 1.0;
                int samples = 0, visible = 0;
                [loop] for (int i = 0; i < 64; i++)
                {
                    float3 local = (float3(i & 3, (i >> 2) & 3, i >> 4) + 0.5) / 4.0 - 0.5;
                    float3 world = mul(_HplHaloBox, float4(local, 1.0)).xyz;
                    uint2 pixel;
                    float depth;
                    if (!ScreenPoint(world, pixel, depth)) continue;
                    samples++;
                    float scene = LoadCameraDepth(pixel);
                #if UNITY_REVERSED_Z
                    if (depth >= scene) visible++;
                #else
                    if (depth <= scene) visible++;
                #endif
                }
                if (samples == 0) return 0.0;
                float alpha = (float)visible / samples;

                // The part of the box's normalized clip rect inside the screen.
                float2 rectMin = 1e5, rectMax = -1e5;
                bool found = false;
                [unroll] for (int c = 0; c < 8; c++)
                {
                    float3 corner = float3(c & 1, (c >> 1) & 1, c >> 2) - 0.5;
                    float4 clip = mul(UNITY_MATRIX_VP, float4(GetCameraRelativePositionWS(mul(_HplHaloBox, float4(corner, 1.0)).xyz), 1.0));
                    if (clip.w <= 0.0) continue;
                    float2 ndc = clip.xy / clip.w;
                    rectMin = min(rectMin, ndc);
                    rectMax = max(rectMax, ndc);
                    found = true;
                }
                if (!found) return 0.0;
                float2 total = rectMax - rectMin;
                float2 inside = min(rectMax, 1.0) - max(rectMin, -1.0);
                float totalArea = total.x * total.y;
                return totalArea > 0.0 ? alpha * max(inside.x, 0.0) * max(inside.y, 0.0) / totalArea : 0.0;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionRWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionRWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseColorMap);
                output.color = _HplColor * HaloAlpha();
                output.viewDepth = -TransformWorldToView(positionRWS).z;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv);
                float3 color = LinearToSRGB(t.rgb) * input.color.rgb;
                float alpha = t.a * input.color.a;

                float finalAlpha = 1.0;
                if (_HplTranslucentFog.w > 0.0)
                {
                    float fogAmount = saturate((input.viewDepth - _HplTranslucentFog.x) / max(_HplTranslucentFog.y, 1e-5));
                    fogAmount = pow(fogAmount, _HplTranslucentFog.w);
                    finalAlpha = _HplTranslucentFog.z * fogAmount + (1.0 - fogAmount);
                }

                int mode = (int)(_HplBlendMode + 0.5);
                if (mode == 0)      { color *= finalAlpha; alpha = 1.0; }
                else if (mode == 1) { color += (1.0 - color) * (1.0 - finalAlpha); alpha = 1.0; }
                else if (mode == 2) { color = (color * finalAlpha + 0.5 * (1.0 - finalAlpha)) * 2.0; alpha = 1.0; }
                else if (mode == 3) { alpha *= finalAlpha; }
                else                { color *= finalAlpha; alpha *= finalAlpha; }
                return float4(SRGBToLinear(max(color, 0.0)), alpha);
            }
            ENDHLSL
        }
    }
}
