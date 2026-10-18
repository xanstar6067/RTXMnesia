using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

namespace RTXMnesia.World
{
    /// <summary>
    /// Draws the HPL2 box lights (<see cref="AmnesiaLight.box"/>, cLightBox): ambient light inside an axis-aligned
    /// box, G-buffer diffuse × color (Hidden/RTXMnesia/HplBoxLight over the opaque renderers). Injected after the
    /// opaque lighting and before sky and fog, like HPL2 which fogs after the lights. Not portable: SSAO
    /// (HPL2 multiplies box lights by it) and the albedo changes of HDRP decals.
    /// One global pass for all loaded scenes, created when the first box light is enabled.
    /// </summary>
    [Serializable]
    public class HplBoxLightPass : CustomPass
    {
        public const string ShaderName = "Hidden/RTXMnesia/HplBoxLight";
        const int MaxBoxes = 16;

        static readonly List<AmnesiaLight> Boxes = new();
        static CustomPassVolume _volume;
        static bool _warnedCount;

        static readonly ShaderTagId[] Tags = { new("GBuffer"), new("Forward") };
        static readonly int CountId = Shader.PropertyToID("_HplBoxCount");
        static readonly int CenterId = Shader.PropertyToID("_HplBoxCenter");
        static readonly int ExtentId = Shader.PropertyToID("_HplBoxExtent");
        static readonly int ColorId = Shader.PropertyToID("_HplBoxColor");
        static readonly int UnitRadianceId = Shader.PropertyToID("_HplUnitRadiance");
        static readonly int AoId = Shader.PropertyToID("_HplBoxAo");

        readonly Vector4[] _centers = new Vector4[MaxBoxes];
        readonly Vector4[] _extents = new Vector4[MaxBoxes];
        readonly Vector4[] _colors = new Vector4[MaxBoxes];
        Shader _shader;

        public static void Register(AmnesiaLight light)
        {
            if (!Boxes.Contains(light)) Boxes.Add(light);
            if (_volume == null) CreateVolume();
        }

        public static void Unregister(AmnesiaLight light) => Boxes.Remove(light);

        static void CreateVolume()
        {
            var go = new GameObject("HPL Box Lights") { hideFlags = HideFlags.HideAndDontSave };
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
            _volume = go.AddComponent<CustomPassVolume>();
            _volume.isGlobal = true;
            _volume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueColor;
            _volume.AddPassOfType(typeof(HplBoxLightPass)).name = "HPL box lights";
        }

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            _shader = Shader.Find(ShaderName);
            if (_shader == null) Debug.LogError("[RTXMnesia] Shader " + ShaderName + " not found: box lights are off");
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if (_shader == null) return;
            int count = 0;
            foreach (var light in Boxes)
            {
                if (light == null || !light.Visible) continue;
                // cLightBox::IsVisible: all of the color ≤ 0 hides the box.
                Color c = light.CurrentColor;
                if (c.r <= 0f && c.g <= 0f && c.b <= 0f && c.a <= 0f) continue;
                if (count == MaxBoxes)
                {
                    if (!_warnedCount) Debug.LogWarning($"[RTXMnesia] More than {MaxBoxes} box lights in view, the rest are skipped");
                    _warnedCount = true;
                    break;
                }
                Vector3 center = light.transform.position;
                _centers[count] = new Vector4(center.x, center.y, center.z, light.boxBlendAdd ? 1f : 0f);
                _extents[count] = light.boxSize * 0.5f;
                _colors[count] = new Vector4(c.r, c.g, c.b, c.a);
                count++;
            }
            if (count == 0) return;

            var cmd = ctx.cmd;
            cmd.SetGlobalInt(CountId, count);
            cmd.SetGlobalVectorArray(CenterId, _centers);
            cmd.SetGlobalVectorArray(ExtentId, _extents);
            cmd.SetGlobalVectorArray(ColorId, _colors);
            cmd.SetGlobalFloat(UnitRadianceId, Hpl.HplLighting.UnitRadiance);
            cmd.SetGlobalFloat(AoId, Hpl.HplRayTracing.Ao > 0 ? 1f : 0f);

            var desc = new RendererListDesc(Tags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                overrideShader = _shader,
                overrideShaderPassIndex = 0,
                excludeObjectMotionVectors = false,
            };
            CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None);
            CoreUtils.DrawRendererList(cmd, ctx.renderContext.CreateRendererList(desc));
        }
    }
}
