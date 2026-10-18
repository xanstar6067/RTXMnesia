// cLightBox for RTXMnesia.World.HplBoxLightPass (deferred_light_box_frag.glsl): inside an axis-aligned box every
// pixel gets G-buffer diffuse × light color, no N·L, no specular. The boxes are applied in order per pixel:
// Replace sets the color, Add adds to it (RenderBoxLight blend modes on the cleared light buffer, boxes first).
// HPL2 multiplies in gamma: (albedo × color)^2.2 = linear albedo × linearized color. Drawn as an override shader
// over the opaque renderers, so the material properties (_BaseColorMap, _BaseColor, alpha test, culling) are kept.
Shader "Hidden/RTXMnesia/HplBoxLight"
{
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "BoxLight"
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            #define MAX_BOXES 16

            int _HplBoxCount;
            float4 _HplBoxCenter[MAX_BOXES];  // xyz world center, w = 1 for Add, 0 for Replace
            float4 _HplBoxExtent[MAX_BOXES];  // xyz half size
            float4 _HplBoxColor[MAX_BOXES];   // HPL2 (gamma) color
            float _HplUnitRadiance;
            // Ray-traced ambient occlusion (HplRayTracing.Ao): HPL2 multiplies the box lights by its SSAO. HDRP keeps
            // the AO of this frame bound globally after the lighting passes; stored inverted (0 = no occlusion).
            float _HplBoxAo;
            TEXTURE2D_X(_AmbientOcclusionTexture);

            TEXTURE2D(_BaseColorMap);
            SAMPLER(sampler_BaseColorMap);
            float4 _BaseColorMap_ST;
            float4 _BaseColor;
            float _AlphaCutoffEnable;
            float _AlphaCutoff;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionRWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(positionRWS);
                output.positionWS = GetAbsolutePositionWS(positionRWS);
                output.uv = input.uv * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float3 color = 0;
                for (int i = 0; i < _HplBoxCount; i++)
                {
                    if (any(abs(input.positionWS - _HplBoxCenter[i].xyz) > _HplBoxExtent[i].xyz)) continue;
                    color = _HplBoxCenter[i].w > 0.5 ? color + _HplBoxColor[i].rgb : _HplBoxColor[i].rgb;
                }
                if (all(color <= 0)) discard;

                float4 albedo = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.uv) * _BaseColor;
                if (_AlphaCutoffEnable > 0.5) clip(albedo.a - _AlphaCutoff);

                float3 light = SRGBToLinear(saturate(color));
                if (_HplBoxAo > 0.5) light *= 1.0 - LOAD_TEXTURE2D_X(_AmbientOcclusionTexture, uint2(input.positionCS.xy)).x;
                return float4(albedo.rgb * light * _HplUnitRadiance * GetCurrentExposureMultiplier(), 0);
            }
            ENDHLSL
        }
    }
}
