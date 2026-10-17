using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.World
{
    /// <summary>
    /// The coverage amount of a mesh entity (iRenderable::SetCoverageAmount): HPL2 draws a mesh with coverage below 1
    /// with the screen-door dissolve of the Z pass (core_dissolve.tga, deferred_base_frag.glsl). Here the HDRP/Lit
    /// materials are swapped for transparent copies while fading (alpha = coverage, alpha clipping kept relative to
    /// it); other shaders (multiply decals, halos) stay as they are.
    /// </summary>
    public sealed class HplMeshFader
    {
        static readonly Dictionary<Material, Material> s_fadeMaterials = new();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int AlphaCutoffId = Shader.PropertyToID("_AlphaCutoff");
        static MaterialPropertyBlock s_block;

        readonly List<(Renderer renderer, Material[] originals, Material[] fade)> _items = new();
        public bool Fading { get; private set; }
        public float Alpha { get; private set; } = 1f;

        /// <summary>The renderers of the entity's model: not billboards or the meshes of its lights.</summary>
        public static List<Renderer> ModelRenderers(Transform root)
        {
            var list = new List<Renderer>();
            if (root == null) return list;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                if (renderer.GetComponent<AmnesiaBillboard>() != null || renderer.GetComponentInParent<AmnesiaLight>() != null) continue;
                list.Add(renderer);
            }
            return list;
        }

        /// <summary>A transparent copy of an HDRP/Lit material (cached), else null.</summary>
        public static Material FadeMaterial(Material material)
        {
            if (material == null || material.shader == null || material.shader.name != "HDRP/Lit") return null;
            if (s_fadeMaterials.TryGetValue(material, out var fade) && fade != null) return fade;
            fade = new Material(material) { name = material.name + " (fade)", hideFlags = HideFlags.DontSave };
            fade.SetFloat("_SurfaceType", 1f);
            fade.SetFloat("_BlendMode", 0f); // alpha
            if (fade.HasProperty("_TransparentZWrite")) fade.SetFloat("_TransparentZWrite", 1f);
            if (fade.HasProperty("_EnableFogOnTransparent")) fade.SetFloat("_EnableFogOnTransparent", 1f);
            HDMaterial.ValidateMaterial(fade);
            s_fadeMaterials[material] = fade;
            return fade;
        }

        public HplMeshFader(IEnumerable<Renderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var originals = renderer.sharedMaterials;
                var fade = new Material[originals.Length];
                bool any = false;
                for (int i = 0; i < originals.Length; i++)
                {
                    fade[i] = FadeMaterial(originals[i]);
                    any |= fade[i] != null;
                    if (fade[i] == null) fade[i] = originals[i];
                }
                if (any) _items.Add((renderer, originals, fade));
            }
        }

        public void SetAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (alpha >= 1f)
            {
                End();
                return;
            }
            if (!Fading)
            {
                Fading = true;
                foreach (var (renderer, _, fade) in _items)
                    if (renderer != null) renderer.sharedMaterials = fade;
            }
            Alpha = alpha;
            Apply(alpha);
        }

        /// <summary>Back to the opaque materials.</summary>
        public void End()
        {
            Alpha = 1f;
            if (!Fading) return;
            Fading = false;
            Apply(1f);
            foreach (var (renderer, originals, _) in _items)
                if (renderer != null) renderer.sharedMaterials = originals;
        }

        void Apply(float alpha)
        {
            s_block ??= new MaterialPropertyBlock();
            foreach (var (renderer, originals, fade) in _items)
            {
                if (renderer == null) continue;
                for (int i = 0; i < fade.Length; i++)
                {
                    if (fade[i] == originals[i]) continue;
                    var m = originals[i];
                    renderer.GetPropertyBlock(s_block, i);
                    Color c = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
                    c.a *= alpha;
                    s_block.SetColor(BaseColorId, c);
                    if (m.HasProperty(AlphaCutoffId)) s_block.SetFloat(AlphaCutoffId, m.GetFloat(AlphaCutoffId) * alpha);
                    renderer.SetPropertyBlock(s_block, i);
                }
            }
        }
    }

    /// <summary>
    /// cLuxDissolveEntity / cLuxMap::AddDissolveEntity: a copy of a model at its current pose (skinned meshes baked,
    /// animation stopped) whose coverage goes from 1 to 0 in the given time, then it is destroyed. The entity itself
    /// is usually turned off at the same time (SetPropActiveAndFade, FadeEnemyToSmoke, DissolveOnDestruction).
    /// </summary>
    public sealed class HplDissolveEntity
    {
        readonly GameObject _root;
        readonly List<Mesh> _bakedMeshes = new();
        readonly HplMeshFader _fader;
        readonly float _fadeSpeed;
        float _alpha = 1f;

        public string Name { get; }
        public float Alpha => _alpha;

        public HplDissolveEntity(Transform source, float time, Transform parent)
        {
            Name = source.name + "_Dissolve";
            _fadeSpeed = 1f / time;
            _root = new GameObject(Name) { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(parent, false);
            var renderers = new List<Renderer>();
            foreach (var renderer in HplMeshFader.ModelRenderers(source))
            {
                // As cLuxMap::AddDissolveEntity: the copy is made even of an entity that is already off (a new mesh
                // entity is visible); hidden meshes (ShowMesh false, an invisible enemy) are left out.
                if (!renderer.enabled) continue;
                var go = new GameObject(renderer.name) { hideFlags = HideFlags.DontSave, layer = renderer.gameObject.layer };
                go.transform.SetParent(_root.transform, false);
                Mesh mesh;
                Transform t = renderer.transform;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    mesh = new Mesh { name = renderer.name + "_Dissolve" };
                    skinned.BakeMesh(mesh, true);
                    _bakedMeshes.Add(mesh);
                    go.transform.SetPositionAndRotation(t.position, t.rotation);
                }
                else
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    mesh = filter != null ? filter.sharedMesh : null;
                    go.transform.SetPositionAndRotation(t.position, t.rotation);
                    go.transform.localScale = t.lossyScale;
                }
                if (mesh == null)
                {
                    DestroyObject(go);
                    continue;
                }
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var copy = go.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                copy.shadowCastingMode = renderer.shadowCastingMode;
                copy.renderingLayerMask = renderer.renderingLayerMask;
                // The illumination amount of the model is copied (its emission property blocks).
                var block = new MaterialPropertyBlock();
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    renderer.GetPropertyBlock(block, i);
                    if (!block.isEmpty) copy.SetPropertyBlock(block, i);
                }
                renderers.Add(copy);
            }
            _fader = new HplMeshFader(renderers);
            _fader.SetAlpha(_alpha);
        }

        /// <summary>cLuxMap::UpdateDissolveEntities for this one: false when it is gone.</summary>
        public bool Update(float timeStep)
        {
            _alpha -= _fadeSpeed * timeStep;
            if (_alpha <= 0f)
            {
                Destroy();
                return false;
            }
            _fader.SetAlpha(_alpha);
            return true;
        }

        public void Destroy()
        {
            if (_root != null) DestroyObject(_root);
            foreach (var mesh in _bakedMeshes)
                if (mesh != null) DestroyObject(mesh);
            _bakedMeshes.Clear();
        }

        static void DestroyObject(Object o)
        {
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
