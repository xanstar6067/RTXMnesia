// cRendererDeferred::RenderFog (deferred_fog_frag.glsl) for RTXMnesia.World.HplFogAreaPass: the fog areas over the
// opaque world, after the lights and before the world fog and the translucent objects, as in HPL2. One full screen
// pass instead of a box per area: per pixel the ray of each area box gives what the box draw covered.
// - Near plane touching the box (inside): the back faces without depth test; the fog depth is the view depth of the
//   scene, with ShowBacksideWhenInside at most the back of the box.
// - Outside: the front faces with depth test; the fog depth starts at the front face, with ShowBacksideWhenOutside it
//   ends at the back face.
// Depths are view space z (not distances), as in HPL2; nothing drawn (sky) has depth 0. Alpha = ((depth - start) /
// (end - start))^falloff × color.a, the color blended over (alpha blend, the areas back to front). HPL2 blends the
// gamma color in a gamma frame buffer; here the color is linear radiance (like the world fog, HplLighting.ApplyFog).
Shader "Hidden/RTXMnesia/HplFogArea"
{
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "FogAreas"
            ZWrite Off
            ZTest Always
            Cull Off
            // The composite of all areas: color + destination × transmittance.
            Blend One SrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Assets/Shaders/HplFogAreas.hlsl"

            float4 Frag(Varyings varyings) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
                float rawDepth = LoadCameraDepth(varyings.positionCS.xy);
                bool sky = rawDepth == UNITY_RAW_FAR_CLIP_VALUE;
                PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, sky ? 0.5 : rawDepth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                float sceneDepth = sky ? 0.0 : posInput.linearDepth;
                float3 cameraRWS = GetCurrentViewPosition();
                // The ray through the pixel with view depth 1 per unit: t on it is the view depth.
                float3 ray = (posInput.positionWS - cameraRWS) / max(posInput.linearDepth, 1e-6);
                float3 cameraAbsWS = GetAbsolutePositionWS(cameraRWS);
                float exposure = GetCurrentExposureMultiplier();

                float3 color;
                float transmittance;
                HplFogAreasAlongRay(cameraAbsWS, ray, sceneDepth, sky, exposure, color, transmittance);
                return float4(color, transmittance);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
