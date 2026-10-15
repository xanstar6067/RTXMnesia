using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// cLuxEffectRenderer (LuxEffectRenderer.cpp): the objects drawn with game effects over the 3D view.
    /// Outline: the focused item glows blue around its silhouette (iLuxPlayerState_DefaultBase::AddOutlineObjects).
    /// Flash: items near the player pulse with a blue rim light (cLuxProp_Item::UpdatePropSpecific); the flash
    /// list is cleared at the start of every map update, like cLuxMapHandler::Update does. Enemy glow: step 6.
    /// Drawn by <see cref="HplEffectRendererPass"/>.
    /// </summary>
    public sealed class HplEffectRenderer
    {
        public readonly List<Renderer> OutlineObjects = new();
        public readonly List<(Renderer renderer, float alpha)> FlashObjects = new();

        // mFlashOscill.SetUp(0, 1, 0, 1, 1): a triangle wave 0..1 at 1 per second.
        float _flashVal, _flashAdd = 1f;

        /// <summary>The global flash alpha: 0.5 + oscillation × 0.5.</summary>
        public float FlashGlobalAlpha => 0.5f + _flashVal * 0.5f;

        public void Update(float timeStep)
        {
            // cLinearOscillation::Update
            _flashVal += _flashAdd * timeStep;
            if (_flashAdd > 0f)
            {
                if (_flashVal >= 1f) { _flashVal = 1f; _flashAdd = -1f; }
            }
            else if (_flashVal <= 0f) { _flashVal = 0f; _flashAdd = 1f; }
        }

        public void ClearRenderLists() => FlashObjects.Clear();

        public void ClearOutlineObjects() => OutlineObjects.Clear();

        public void AddOutlineObject(Renderer renderer) => OutlineObjects.Add(renderer);

        public void AddFlashObject(Renderer renderer, float alpha) => FlashObjects.Add((renderer, alpha));

        public void Reset()
        {
            OutlineObjects.Clear();
            ClearRenderLists();
        }

        /// <summary>The mesh renderers of a node (an entity body), without billboards and particle renderers.</summary>
        public static IEnumerable<Renderer> MeshRenderers(Transform node)
        {
            foreach (var r in node.GetComponentsInChildren<MeshRenderer>())
            {
                if (r.GetComponent<World.AmnesiaBillboard>() != null || r.GetComponentInParent<World.AmnesiaBillboard>() != null) continue;
                if (!r.TryGetComponent(out MeshFilter f) || f.sharedMesh == null) continue;
                yield return r;
            }
        }
    }

    /// <summary>
    /// Draws <see cref="HplEffectRenderer"/> before post-processing (HPL2: the translucent callback, into the
    /// accumulation buffer, before the post effects):
    /// - flash objects: diffuse × (0.5, 0.5, 1) × rim light × alpha, added twice, on the object's own surface;
    /// - outline: the visible pixels of the objects make a mask (HPL2: stencil), the objects grown by 2 cm
    ///   (about their bounding box center) write blue 0.5 outside it, the result is blurred twice at a quarter of the
    ///   resolution (posteffect_bloom_blur_frag.glsl) and added outside the mask within the objects' screen rect.
    /// HPL2 adds in gamma; here the gamma values are linearized and added (the same on the dark scene around).
    /// </summary>
    [Serializable]
    public class HplEffectRendererPass : CustomPass
    {
        public const string ShaderName = "Hidden/RTXMnesia/HplEffectRenderer";
        const int FlashPass = 0, MaskPass = 1, OutlineColorPass = 2, BlurVerticalPass = 3, BlurHorizontalPass = 4, CompositePass = 5;
        const float ScaleAdd = 0.02f;

        public Shader shader;
        [NonSerialized] public HplEffectRenderer renderer;

        Material _material;
        RTHandle _mask, _color, _blur0, _blur1;
        MaterialPropertyBlock _objectBlock;

        static readonly int TextureId = Shader.PropertyToID("_HplTexture");
        static readonly int AlphaTestId = Shader.PropertyToID("_HplAlphaTest");
        static readonly int UseAlphaId = Shader.PropertyToID("_HplUseAlpha");
        static readonly int ColorMulId = Shader.PropertyToID("_HplColorMul");
        static readonly int MaskId = Shader.PropertyToID("_HplMask");
        static readonly int SourceId = Shader.PropertyToID("_HplSource");
        static readonly int SourceSizeId = Shader.PropertyToID("_HplSourceSize");
        static readonly int TargetSizeId = Shader.PropertyToID("_HplTargetSize");
        static readonly int ClipRectId = Shader.PropertyToID("_HplClipRect");

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (shader == null) shader = Shader.Find(ShaderName);
            if (shader != null) _material = CoreUtils.CreateEngineMaterial(shader);
            _mask = Alloc(Vector2.one, "HPL outline mask");
            _color = Alloc(Vector2.one, "HPL outline color");
            _blur0 = Alloc(Vector2.one * 0.25f, "HPL outline blur 0");
            _blur1 = Alloc(Vector2.one * 0.25f, "HPL outline blur 1");
            _objectBlock = new MaterialPropertyBlock();
        }

        static RTHandle Alloc(Vector2 scale, string name) =>
            RTHandles.Alloc(scale, TextureXR.slices, dimension: TextureXR.dimension,
                            colorFormat: GraphicsFormat.R8_UNorm, useDynamicScale: true, name: name);

        protected override void Execute(CustomPassContext ctx)
        {
            if (renderer == null || _material == null) return;
            if (ctx.hdCamera.camera.cameraType != CameraType.Game) return;
            var cmd = ctx.cmd;

            if (renderer.FlashObjects.Count > 0)
            {
                CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer, ClearFlag.None);
                float global = renderer.FlashGlobalAlpha;
                foreach (var (r, alpha) in renderer.FlashObjects)
                    DrawObject(cmd, r, Matrix4x4.identity, FlashPass, alpha * global);
            }

            if (renderer.OutlineObjects.Count > 0) RenderOutline(ctx);
        }

        void RenderOutline(CustomPassContext ctx)
        {
            var cmd = ctx.cmd;
            var camera = ctx.hdCamera.camera;
            int width = ctx.hdCamera.actualWidth, height = ctx.hdCamera.actualHeight;

            // Objects in view and their total bounds (+ 2 × 2 cm), whose screen rect limits the glow.
            var bounds = new Bounds();
            bool any = false;
            foreach (var r in renderer.OutlineObjects)
            {
                if (r == null || !r.isVisible && Application.isPlaying) continue;
                if (!any) bounds = r.bounds;
                else bounds.Encapsulate(r.bounds);
                any = true;
            }
            if (!any) return;
            bounds.Expand(ScaleAdd * 2f * 2f);
            Vector4 clip = ClipRect(camera, bounds, width, height);

            // Mask: the visible pixels of the objects (the camera depth test).
            CoreUtils.SetRenderTarget(cmd, _mask, ctx.cameraDepthBuffer, ClearFlag.Color, Color.clear);
            foreach (var r in renderer.OutlineObjects)
                if (r != null) DrawObject(cmd, r, Matrix4x4.identity, MaskPass, 1f);

            // Color: the grown objects, blue 0.5 outside the mask.
            CoreUtils.SetRenderTarget(cmd, _color, ctx.cameraDepthBuffer, ClearFlag.Color, Color.clear);
            cmd.SetGlobalTexture(MaskId, _mask);
            foreach (var r in renderer.OutlineObjects)
            {
                if (r == null || !r.TryGetComponent(out MeshFilter f) || f.sharedMesh == null) continue;
                Bounds local = f.sharedMesh.bounds;
                Vector3 size = local.size;
                var scale = new Vector3(Grow(size.x), Grow(size.y), Grow(size.z));
                Matrix4x4 grow = Matrix4x4.Translate(local.center) * Matrix4x4.Scale(scale) * Matrix4x4.Translate(-local.center);
                DrawObject(cmd, r, grow, OutlineColorPass, 1f);
            }

            // Blur twice: vertical (into a quarter size), horizontal.
            var quarter = new Vector2(Mathf.Max(1, width / 4), Mathf.Max(1, height / 4));
            Blur(cmd, _color, new Vector2(width, height), _blur0, quarter, BlurVerticalPass);
            Blur(cmd, _blur0, quarter, _blur1, quarter, BlurHorizontalPass);
            Blur(cmd, _blur1, quarter, _blur0, quarter, BlurVerticalPass);
            Blur(cmd, _blur0, quarter, _blur1, quarter, BlurHorizontalPass);

            // Added outside the mask, inside the objects' screen rect.
            var props = ctx.propertyBlock;
            props.SetTexture(SourceId, _blur1);
            props.SetVector(SourceSizeId, quarter);
            props.SetVector(TargetSizeId, new Vector2(width, height));
            props.SetTexture(MaskId, _mask);
            props.SetVector(ClipRectId, clip);
            CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, CompositePass);
        }

        /// <summary>Scale that grows a size by 2 cm (HPL2: 1 / size × 0.02 + 1; a flat side stays).</summary>
        static float Grow(float size) => size > 1e-4f ? ScaleAdd / size + 1f : 1f;

        void Blur(CommandBuffer cmd, RTHandle source, Vector2 sourceSize, RTHandle target, Vector2 targetSize, int pass)
        {
            var props = new MaterialPropertyBlock();
            props.SetTexture(SourceId, source);
            props.SetVector(SourceSizeId, sourceSize);
            props.SetVector(TargetSizeId, targetSize);
            CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, props, pass);
        }

        /// <summary>cMath::GetClipRectFromBV: the screen rect of the bounds in pixels (y down); the whole screen when behind.</summary>
        static Vector4 ClipRect(Camera camera, Bounds b, int width, int height)
        {
            Vector2 min = new(float.MaxValue, float.MaxValue), max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 p = camera.WorldToViewportPoint(corner);
                if (p.z <= 0f) return new Vector4(0f, 0f, width, height);
                var px = new Vector2(p.x * width, (1f - p.y) * height);
                min = Vector2.Min(min, px);
                max = Vector2.Max(max, px);
            }
            return new Vector4(min.x, min.y, max.x, max.y);
        }

        /// <summary>One renderer's sub meshes with its material's diffuse texture and alpha test.</summary>
        void DrawObject(CommandBuffer cmd, Renderer r, Matrix4x4 local, int pass, float colorMul)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || !r.TryGetComponent(out MeshFilter f) || f.sharedMesh == null) return;
            var mesh = f.sharedMesh;
            var materials = r.sharedMaterials;
            Matrix4x4 matrix = r.localToWorldMatrix * local;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var m = i < materials.Length ? materials[i] : null;
                Texture texture = null;
                bool alphaTested = false;
                if (m != null)
                {
                    if (m.HasProperty("_BaseColorMap")) texture = m.GetTexture("_BaseColorMap");
                    else if (m.HasProperty("_UnlitColorMap")) texture = m.GetTexture("_UnlitColorMap");
                    alphaTested = m.HasProperty("_AlphaCutoffEnable") && m.GetFloat("_AlphaCutoffEnable") > 0.5f;
                }
                _objectBlock.Clear();
                _objectBlock.SetTexture(TextureId, texture != null ? texture : Texture2D.whiteTexture);
                _objectBlock.SetFloat(AlphaTestId, alphaTested ? 0.5f : -1f);
                _objectBlock.SetFloat(UseAlphaId, alphaTested ? 1f : 0f);
                _objectBlock.SetFloat(ColorMulId, colorMul);
                cmd.DrawMesh(mesh, matrix, _material, i, pass, _objectBlock);
            }
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(_material);
            _mask?.Release();
            _color?.Release();
            _blur0?.Release();
            _blur1?.Release();
        }
    }
}
