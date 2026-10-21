using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Hpl
{
    /// <summary>Offline generated geometry. Resource paths keep detailed meshes out of the startup dependency tree.</summary>
    public sealed class HplReliefSet : ScriptableObject
    {
        public const string ResourceName = "HplEnhancedRelief";
        [Serializable] public struct Slot
        {
            public Texture albedo;
            public Vector2 scale, offset;
            public bool displaced;
            public string normalResource, maskResource, specularResource;
        }
        [Serializable] public sealed class Entry
        {
            public Mesh original;
            public Vector3 objectScale;
            public Slot[] slots;
            public string detailedResource, coarseResource;

            public bool Matches(Renderer renderer, Material[] materials)
            {
                if (materials.Length != slots.Length || (ScaleOf(renderer.transform) - objectScale).sqrMagnitude > 0.000001f) return false;
                for (int i = 0; i < slots.Length; i++)
                {
                    var material = HplEnhancedTextures.ClassicMaterial(materials[i]);
                    if (slots[i].displaced && (material == null || material.shader == null || material.shader.name != "HDRP/Lit" ||
                        material.GetFloat("_SurfaceType") != 0 || material.GetFloat("_AlphaCutoffEnable") != 0 || material.GetFloat("_UVBase") != 0)) return false;
                    var texture = material != null && material.HasProperty("_BaseColorMap") ? material.GetTexture("_BaseColorMap") : null;
                    if (texture != slots[i].albedo) return false;
                    if (texture != null && ((material.GetTextureScale("_BaseColorMap") - slots[i].scale).sqrMagnitude > 0.00000001f ||
                        (material.GetTextureOffset("_BaseColorMap") - slots[i].offset).sqrMagnitude > 0.00000001f)) return false;
                }
                return true;
            }
        }
        public Entry[] entries = Array.Empty<Entry>();
        public static Vector3 ScaleOf(Transform transform)
        {
            var matrix = transform.localToWorldMatrix;
            return new Vector3(matrix.MultiplyVector(Vector3.right).magnitude,
                matrix.MultiplyVector(Vector3.up).magnitude, matrix.MultiplyVector(Vector3.forward).magnitude);
        }
    }

    /// <summary>Reversible visual mesh swaps; colliders and source meshes are never edited.</summary>
    public static class HplEnhancedRelief
    {
        sealed class State
        {
            public MeshFilter filter;
            public Renderer renderer;
            public HplReliefSet.Entry entry;
            public Mesh detailed, coarse;
            public bool high;
        }
        static HplReliefSet s_catalog;
        static bool s_initialized;
        static readonly Dictionary<Mesh, List<HplReliefSet.Entry>> s_entries = new();
        static readonly Dictionary<MeshFilter, State> s_states = new();
        static readonly Dictionary<(Material, string), Material> s_copies = new();
        static readonly Dictionary<Material, Material> s_sources = new();
        static HplReliefLod s_driver;
        public static int ActiveMeshCount => s_states.Count;
        public static Material SurfaceMaterial(Material material) => material != null && s_sources.TryGetValue(material, out var source) ? source : material;

        static void Initialize()
        {
            if (s_initialized) return;
            s_initialized = true;
            s_catalog = Resources.Load<HplReliefSet>(HplReliefSet.ResourceName);
            if (s_catalog == null) return;
            foreach (var entry in s_catalog.entries)
            {
                if (entry.original == null) continue;
                if (!s_entries.TryGetValue(entry.original, out var list)) s_entries[entry.original] = list = new();
                list.Add(entry);
            }
        }

        public static HplReliefSet.Entry Find(Renderer renderer, Mesh original, Material[] materials)
        {
            Initialize();
            if (original == null || !s_entries.TryGetValue(original, out var list)) return null;
            foreach (var entry in list) if (entry.Matches(renderer, materials)) return entry;
            return null;
        }

        public static void Apply(Renderer renderer, bool enhanced)
        {
            if (renderer is not MeshRenderer || !renderer.TryGetComponent<MeshFilter>(out var filter)) return;
            s_states.TryGetValue(filter, out var state);
            if (!enhanced)
            {
                if (state != null)
                {
                    if (filter.sharedMesh == state.detailed || filter.sharedMesh == state.coarse) filter.sharedMesh = state.entry.original;
                    s_states.Remove(filter);
                }
                return;
            }
            // Runtime static batches contain transformed combined geometry and cannot be swapped safely.
            // The build scene processor excludes eligible renderers from batching before the player is built.
            if (renderer.isPartOfStaticBatch) return;
            var materials = renderer.sharedMaterials;
            if (state != null && (!state.entry.Matches(renderer, materials) ||
                (filter.sharedMesh != state.detailed && filter.sharedMesh != state.coarse && filter.sharedMesh != state.entry.original)))
            {
                if (filter.sharedMesh == state.detailed || filter.sharedMesh == state.coarse) filter.sharedMesh = state.entry.original;
                s_states.Remove(filter);
                state = null;
                // A foreign material/UV change must not retain our no-POM copies or stale geometry.
                for (int i = 0; i < materials.Length; i++) materials[i] = SurfaceMaterial(materials[i]);
                renderer.sharedMaterials = materials;
            }
            if (state == null)
            {
                if (renderer.GetComponentInParent<Rigidbody>(true) != null) return;
                var entry = Find(renderer, filter.sharedMesh, materials);
                if (entry == null) return;
                var coarse = Resources.Load<Mesh>(entry.coarseResource);
                if (coarse == null) return;
                state = new State { filter = filter, renderer = renderer, entry = entry, coarse = coarse };
                s_states[filter] = state;
                SelectLod(state, Camera.main);
            }
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (!state.entry.slots[i].displaced) continue;
                var source = SurfaceMaterial(materials[i]);
                if (source == null || source.shader.name != "HDRP/Lit") continue;
                var slot = state.entry.slots[i];
                var materialKey = (source, slot.normalResource + "|" + slot.maskResource + "|" + slot.specularResource);
                if (!s_copies.TryGetValue(materialKey, out var copy))
                {
                    copy = new Material(source) { name = source.name + " (mesh relief)", hideFlags = HideFlags.HideAndDontSave };
                    copy.SetFloat("_DisplacementMode", 0);
                    void Map(string property, string resource)
                    {
                        if (string.IsNullOrEmpty(resource)) return;
                        var texture = Resources.Load<Texture2D>(resource);
                        if (texture == null) throw new InvalidOperationException("Missing relief texture: " + resource);
                        copy.SetTexture(property, texture);
                    }
                    Map("_NormalMap", slot.normalResource);
                    Map("_MaskMap", slot.maskResource);
                    Map("_SpecularColorMap", slot.specularResource);
                    if (!string.IsNullOrEmpty(slot.specularResource))
                    {
                        // Standard Lit ignores specular coverage. Preserve its dielectric F0 when enabling it.
                        if (copy.GetFloat("_MaterialID") == (float)MaterialId.LitStandard)
                            copy.SetColor("_SpecularColor", new Color(.04f, .04f, .04f, 1));
                        copy.SetFloat("_MaterialID", (float)MaterialId.LitSpecular);
                    }
                    UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(copy);
                    s_copies[materialKey] = copy;
                    s_sources[copy] = source;
                    HplEnhancedTextures.TrackDerivedMaterial(copy, HplEnhancedTextures.ClassicMaterial(source));
                }
                changed |= materials[i] != copy;
                materials[i] = copy;
            }
            if (changed) renderer.sharedMaterials = materials;
            if (Application.isPlaying && s_driver == null)
            {
                var root = new GameObject("Enhanced relief LOD") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(root);
                s_driver = root.AddComponent<HplReliefLod>();
            }
        }

        static bool SelectLod(State state, Camera camera)
        {
            bool high = camera == null || state.renderer.bounds.SqrDistance(camera.transform.position) < 12 * 12;
            if (high && state.detailed == null) state.detailed = Resources.Load<Mesh>(state.entry.detailedResource);
            var mesh = high && state.detailed != null ? state.detailed : state.coarse;
            bool changed = state.filter.sharedMesh != mesh;
            if (changed) state.filter.sharedMesh = mesh;
            state.high = high;
            return changed;
        }

        internal static void Tick()
        {
            var camera = Camera.main;
            bool changed = false;
            foreach (var state in s_states.Values)
            {
                if (state.filter == null || state.renderer == null) continue;
                if (state.filter.sharedMesh != state.detailed && state.filter.sharedMesh != state.coarse) continue;
                float distance = camera == null ? 0 : state.renderer.bounds.SqrDistance(camera.transform.position);
                // Hysteresis prevents rebuilding the RT acceleration structure at the threshold every frame.
                if ((state.high && distance > 14 * 14) || (!state.high && distance < 10 * 10)) changed |= SelectLod(state, camera);
            }
            if (changed && UnityEngine.Rendering.RenderPipelineManager.currentPipeline is
                UnityEngine.Rendering.HighDefinition.HDRenderPipeline hdrp) hdrp.ResetPathTracing();
        }

        public static void Release()
        {
            foreach (var state in s_states.Values)
                if (state.filter != null && (state.filter.sharedMesh == state.detailed || state.filter.sharedMesh == state.coarse))
                    state.filter.sharedMesh = state.entry.original;
            s_states.Clear();
            // Derived materials are owned and destroyed by HplEnhancedTextures after it restores material slots.
            s_copies.Clear();
            s_sources.Clear();
            s_entries.Clear();
            s_catalog = null;
            s_initialized = false;
            if (s_driver != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(s_driver.gameObject);
                else UnityEngine.Object.DestroyImmediate(s_driver.gameObject);
                s_driver = null;
            }
        }
    }

    [AddComponentMenu("")]
    public sealed class HplReliefLod : MonoBehaviour
    {
        float _next;
        void LateUpdate()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            HplEnhancedRelief.Tick();
        }
    }
}
