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
        // Renderers moved off a shared wall plane (SeparateSharedWalls) and where they were.
        static readonly List<(Transform transform, Vector3 position)> s_lifts = new();
        /// <summary>Gap left between a lifted wall's deepest joint and the shared plane (RT ray bias, shadow-map bias).</summary>
        public const float SharedWallMargin = 0.004f;
        /// <summary>Debug: off leaves shared walls in their plane (HplEnhancedLightingProbe mode "nolift").</summary>
        public static bool SeparateWalls = true;
        public static int SeparatedWallCount { get; private set; }
        public static int LiftedDecalCount { get; private set; }
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

        /// <summary>
        /// HPL2 builds a wall between two rooms of two modules back to back in one plane, without thickness. The relief
        /// recesses the joints up to 2 cm below the plane, so each side's joints came out through the other side's stone
        /// faces into the next room: there its torch lit them (orange cracks in a dark room, plainly with RT). Each wall
        /// of such a pair moves into its own room by its joint depth, and the map decals lying on it with it. Collision is
        /// unchanged. Call after the renderers of the loaded scenes were applied.
        /// </summary>
        public static void SeparateSharedWalls()
        {
            RestoreLifts();
            SeparatedWallCount = LiftedDecalCount = 0;
            if (!SeparateWalls) return;
            var planes = new List<(State state, Vector3 normal, float plane, float depth, Bounds bounds)>();
            foreach (var state in s_states.Values)
                if (state.renderer != null && state.filter != null && BackPlane(state, out var normal, out float plane, out float depth))
                    planes.Add((state, normal, plane, depth, state.renderer.bounds));
            var lifts = new Dictionary<Transform, (Vector3 normal, float plane, float lift, Bounds bounds)>();
            for (int i = 0; i < planes.Count; i++)
                for (int j = i + 1; j < planes.Count; j++)
                {
                    var a = planes[i]; var b = planes[j];
                    // Opposite faces of one plane (n_b = -n_a, so d_b = -d_a) that overlap.
                    if (Vector3.Dot(a.normal, b.normal) > -0.995f || Mathf.Abs(a.plane + b.plane) > 0.002f || !a.bounds.Intersects(b.bounds)) continue;
                    foreach (var (state, normal, plane, depth, bounds) in new[] { a, b })
                        lifts[state.renderer.transform] = (normal, plane, depth + SharedWallMargin, bounds);
                }
            if (lifts.Count == 0) return;
            foreach (var (transform, lift) in lifts)
            {
                s_lifts.Add((transform, transform.position));
                transform.position += lift.normal * lift.lift;
            }
            SeparatedWallCount = lifts.Count;
            // Decals of the map lie up to a centimetre in front of the wall plane: the lifted stones would cover them.
            var walls = new List<(Vector3 normal, float plane, float lift, Bounds bounds)>(lifts.Values);
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
            {
                if (!IsDecal(renderer) || renderer.isPartOfStaticBatch) continue;
                var bounds = renderer.bounds;
                foreach (var wall in walls)
                {
                    var n = wall.normal;
                    float thickness = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z)));
                    float offset = Vector3.Dot(n, bounds.center) - wall.plane;
                    if (thickness > 0.02f || offset < -0.002f || offset > 0.02f) continue;
                    var near = wall.bounds;
                    near.Expand(0.04f);
                    if (!near.Intersects(bounds)) continue;
                    s_lifts.Add((renderer.transform, renderer.transform.position));
                    renderer.transform.position += n * wall.lift;
                    LiftedDecalCount++;
                    break;
                }
            }
        }

        /// <summary>A flat decal of a map (HDRP mesh decal or HPL multiply decal).</summary>
        public static bool IsDecal(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            return material != null && material.shader != null &&
                   (material.FindPass("DBufferMesh") >= 0 || material.shader.name == "RTXMnesia/HplMultiply");
        }

        /// <summary>
        /// The original plane of a relief wall, facing its room, and how deep the relief goes behind it. The relief grows
        /// the module's bounds backwards by the joints and forwards only by the stone faces' few millimetres.
        /// </summary>
        static bool BackPlane(State state, out Vector3 normal, out float plane, out float depth)
        {
            normal = default;
            plane = depth = 0;
            Bounds original = state.entry.original.bounds, relief = state.coarse.bounds;
            if (state.detailed != null) relief.Encapsulate(state.detailed.bounds);
            int axis = -1;
            float sign = 0, behind = 0.005f;
            for (int k = 0; k < 3; k++)
            {
                float below = original.min[k] - relief.min[k], above = relief.max[k] - original.max[k];
                if (below > behind) { behind = below; axis = k; sign = 1; }
                if (above > behind) { behind = above; axis = k; sign = -1; }
            }
            if (axis < 0) return false;
            var local = Vector3.zero;
            local[axis] = sign;
            var transform = state.renderer.transform;
            normal = transform.worldToLocalMatrix.transpose.MultiplyVector(local).normalized;
            var point = original.center;
            point[axis] = sign > 0 ? original.min[axis] : original.max[axis];
            plane = Vector3.Dot(normal, transform.TransformPoint(point));
            depth = transform.localToWorldMatrix.MultiplyVector(local).magnitude * behind;
            return true;
        }

        static void RestoreLifts()
        {
            foreach (var (transform, position) in s_lifts)
                if (transform != null) transform.position = position;
            s_lifts.Clear();
        }

        public static void Release()
        {
            RestoreLifts();
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
