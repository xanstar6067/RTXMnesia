// HPL2 multiplicative decals (Type="Decal", BlendMode="Mul": dirt, moisture, wine pools) as HDRP mesh decals.
// HPL2 multiplies them into the G-buffer color before the lighting and the fog (RendererDeferred.cpp RenderDecals,
// deferred_decal_frag.glsl: texture × vertex color, alpha test 0.01). HDRP's DBuffer does the same to the albedo
// of the opaque surfaces under the decal mesh, but blends: albedo × (1 − a) + color. With color 0 and a = 1 − factor
// that is albedo × factor, and the DBuffer's alpha compositing multiplies overlapping decals (Π (1 − a)) as HPL2's
// repeated multiply does. The factor is one number (DBuffer alpha is scalar): the luminance of the per-channel factor,
// the dirt textures are grey. It is computed in gamma as in HPL2: t_gamma × color, clamped like the 8-bit G-buffer,
// then linearized (albedo_lin × factor_lin = (albedo_gamma × factor_gamma)^2.2).
// Before (HplMultiply.shader on the lit frame, a transparent mesh 1-4 cm above the floor) the decals multiplied the
// lit color, needed a haze pass, and the dark spots of two or three stacked dirt_floor01 flickered in play
// (01_old_archives, docs/tasks/bug_05_mul_decals.md). MulX2 (brightening) cannot be a DBuffer blend and stays there.
// The pass layout and includes are HDRP/Decal's DBufferMesh pass (Runtime/Material/Decal/Decal.shader); only the
// surface data (GetSurfaceData, normally DecalData.hlsl) is ours. The material is set up by MaterialConverter
// (ConfigureMulDecal): color mask RGBA on DBuffer 0 only, decal stencil, queue Geometry.
Shader "RTXMnesia/HplMulDecal"
{
    Properties
    {
        [MainColor] _BaseColor("Color (HPL, gamma)", Color) = (1,1,1,1)
        [MainTexture] _BaseColorMap("Texture", 2D) = "white" {}
        _HplAlphaLimit("Alpha test limit", Float) = 0.01
        _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 1
        [Enum(Depth Bias, 0, View Bias, 1)] _DecalMeshBiasType("_DecalMeshBiasType", Int) = 0
        _DecalMeshDepthBias("_DecalMeshDepthBias", Float) = 0.0
        _DecalMeshViewBias("_DecalMeshViewBias", Float) = 0.0
        _DrawOrder("_DrawOrder", Int) = 0

        [HideInInspector] _DecalStencilRef("_DecalStencilRef", Int) = 16
        [HideInInspector] _DecalStencilWriteMask("_DecalStencilWriteMask", Int) = 16
        [HideInInspector] _DecalColorMask0("_DecalColorMask0", Int) = 15
        [HideInInspector] _DecalColorMask1("_DecalColorMask1", Int) = 0
        [HideInInspector] _DecalColorMask2("_DecalColorMask2", Int) = 0
        [HideInInspector] _DecalColorMask3("_DecalColorMask3", Int) = 0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
    #pragma multi_compile_instancing

    #define SUPPORT_GLOBAL_MIP_BIAS
    #define _COLORMAP
    #define _MATERIAL_AFFECTS_ALBEDO

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "DBufferMesh"
            Tags { "LightMode" = "DBufferMesh" }

            Stencil
            {
                WriteMask [_DecalStencilWriteMask]
                Ref [_DecalStencilRef]
                Comp Always
                Pass Replace
            }

            ZWrite Off
            ZTest LEqual

            Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
            Blend 3 Zero OneMinusSrcColor

            ColorMask [_DecalColorMask0]
            ColorMask [_DecalColorMask1] 1
            ColorMask [_DecalColorMask2] 2
            ColorMask [_DecalColorMask3] 3

            HLSLPROGRAM

            #pragma multi_compile_fragment DECALS_3RT DECALS_4RT
            #pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #define SHADERPASS SHADERPASS_DBUFFER_MESH
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_BaseColorMap);
            SAMPLER(sampler_BaseColorMap);

            // HDRP/Decal's per material block (DecalProperties.hlsl) with the alpha limit added: the shader pass reads
            // the mesh bias fields from it.
            CBUFFER_START(UnityPerMaterial)
                float _NormalBlendSrc;
                float _MaskBlendSrc;
                float _DecalBlend;
                float4 _BaseColor;
                float3 _EmissiveColor;
                float _EmissiveExposureWeight;
                int   _DecalMeshBiasType;
                float _DecalMeshDepthBias;
                float _DecalMeshViewBias;
                float _HplAlphaLimit;
            CBUFFER_END

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/ShaderPass/DecalSharePass.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            void GetSurfaceData(FragInputs input, float3 V, PositionInputs posInput, float angleFadeFactor, out DecalSurfaceData surfaceData)
            {
                #ifdef LOD_FADE_CROSSFADE
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
                float4 t = SAMPLE_TEXTURE2D(_BaseColorMap, sampler_BaseColorMap, input.texCoord0.xy);
                float3 factor = 1.0;
                if (t.a * _BaseColor.a >= _HplAlphaLimit)
                    factor = pow(saturate(LinearToSRGB(t.rgb) * _BaseColor.rgb), 2.2);
                // albedo × (1 − a) + 0: the factor's luminance (Rec. 709, the working color space).
                surfaceData.baseColor = float4(0.0, 0.0, 0.0, (1.0 - dot(factor, float3(0.2126, 0.7152, 0.0722))) * _DecalBlend);
            }

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDecal.hlsl"

            ENDHLSL
        }
    }

    FallBack Off
}
