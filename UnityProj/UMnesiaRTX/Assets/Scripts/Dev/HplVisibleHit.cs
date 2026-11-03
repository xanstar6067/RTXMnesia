using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// What the camera ray shows, not what it collides with (bug report F1): the triangles of the rendered meshes
    /// along the ray, nearest first. Colliders differ from the image — character colliders stand in front of
    /// curtains, entities collide with boxes — so the physics hit alone does not name the visible model.
    /// Static batched renderers are tested on their own sub meshes of the combined mesh (vertices in the batch root's
    /// space). A mesh without CPU data (not readable) is listed but not tested.
    /// </summary>
    public static class HplVisibleHit
    {
        // Internal in Unity 6000.6: the first sub mesh of a batched renderer in the combined mesh (MeshRenderer) and the
        // batch root (Renderer). Read by reflection (Mono player, no stripping); unavailable: all sub meshes, the slot unknown.
        const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        static readonly PropertyInfo s_subMeshStart = typeof(MeshRenderer).GetProperty("subMeshStartIndex", Internal);
        static readonly PropertyInfo s_batchRoot = typeof(Renderer).GetProperty("staticBatchRootTransform", Internal);

        public struct Hit
        {
            public Renderer renderer;
            public float distance;
            public Vector3 point, normal;
            /// <summary>Material slot of the hit sub mesh; -1 — unknown (static batch without its sub mesh range).</summary>
            public int slot;
            /// <summary>The ray met the triangle's back (a one-sided face is invisible from there).</summary>
            public bool backFace;
            /// <summary>Drawn over what is behind (transparent queue: Mul decals, glass, halos); slot unknown — all its materials are.</summary>
            public bool Transparent
            {
                get
                {
                    static bool Over(Material m) => m != null && m.renderQueue > (int)RenderQueue.GeometryLast;
                    var materials = renderer.sharedMaterials;
                    if (slot >= 0) return Over(Material);
                    foreach (var m in materials) if (!Over(m)) return false;
                    return materials.Length > 0;
                }
            }
            public Material Material
            {
                get
                {
                    var materials = renderer.sharedMaterials;
                    return materials.Length == 0 || slot < 0 ? null : materials[Mathf.Min(slot, materials.Length - 1)];
                }
            }
        }

        /// <summary>The hits along the ray (one per renderer, the nearest), sorted by distance; unreadable meshes on the ray.</summary>
        public static List<Hit> Cast(Ray ray, float maxDistance, Scene scene, int cullingMask, Transform exclude, List<Renderer> unreadable)
        {
            var hits = new List<Hit>();
            var geometry = new Dictionary<Mesh, Geometry>();
            Mesh baked = null;
            foreach (var renderer in Object.FindObjectsByType<Renderer>())
            {
                if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer) continue;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                if (renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly || (cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                if (renderer.gameObject.scene != scene || exclude != null && renderer.transform.IsChildOf(exclude)) continue;
                if (!renderer.bounds.IntersectRay(ray, out float boundsDistance) || boundsDistance > maxDistance) continue;

                Mesh mesh;
                Matrix4x4 toWorld;
                int first = 0, count;
                if (renderer is SkinnedMeshRenderer skinned)
                {
                    if (skinned.sharedMesh == null) continue;
                    baked ??= new Mesh { hideFlags = HideFlags.HideAndDontSave };
                    skinned.BakeMesh(baked, true);
                    mesh = baked;
                    geometry.Remove(baked);
                    toWorld = Matrix4x4.TRS(skinned.transform.position, skinned.transform.rotation, Vector3.one);
                    count = mesh.subMeshCount;
                }
                else
                {
                    if (!renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                    mesh = filter.sharedMesh;
                    if (renderer.isPartOfStaticBatch)
                    {
                        var root = s_batchRoot?.GetValue(renderer) as Transform;
                        toWorld = root != null ? root.localToWorldMatrix : Matrix4x4.identity;
                        if (s_subMeshStart != null)
                        {
                            first = (int)s_subMeshStart.GetValue(renderer);
                            // One sub mesh per material in the combined mesh.
                            count = renderer.sharedMaterials.Length;
                        }
                        else { first = -1; count = mesh.subMeshCount; }
                    }
                    else
                    {
                        toWorld = renderer.localToWorldMatrix;
                        count = mesh.subMeshCount;
                    }
                }
                if (!geometry.TryGetValue(mesh, out var data)) geometry[mesh] = data = Geometry.Read(mesh);
                if (data == null) { unreadable?.Add(renderer); continue; }
                var positions = data.positions;

                // The ray in the mesh's space; distances are measured back in the world.
                var toLocal = toWorld.inverse;
                Vector3 origin = toLocal.MultiplyPoint3x4(ray.origin), direction = toLocal.MultiplyVector(ray.direction);
                bool found = false;
                var best = new Hit { renderer = renderer, distance = float.MaxValue };
                bool known = first >= 0;
                if (!known) first = 0;
                for (int sub = first; sub < first + count && sub < mesh.subMeshCount; sub++)
                {
                    var indices = data.indices[sub];
                    if (indices == null) continue;
                    for (int i = 0; i + 2 < indices.Length; i += 3)
                    {
                        Vector3 a = positions[indices[i]], b = positions[indices[i + 1]], c = positions[indices[i + 2]];
                        if (!Intersect(origin, direction, a, b, c, out float t)) continue;
                        Vector3 world = toWorld.MultiplyPoint3x4(origin + direction * t);
                        float distance = Vector3.Distance(ray.origin, world);
                        if (distance > maxDistance || distance >= best.distance) continue;
                        // Without its range the combined mesh holds other renderers' triangles too: only those in its bounds.
                        if (!known && !Inflate(renderer.bounds).Contains(world)) continue;
                        // Unity's front faces wind clockwise; mirrored transforms flip them.
                        Vector3 normal = toWorld.MultiplyVector(Vector3.Cross(b - a, c - a)).normalized;
                        if (toWorld.determinant < 0) normal = -normal;
                        best.distance = distance;
                        best.point = world;
                        best.normal = Vector3.Dot(normal, ray.direction) > 0 ? -normal : normal;
                        best.backFace = Vector3.Dot(normal, ray.direction) > 0;
                        best.slot = known ? sub - first : -1;
                        found = true;
                    }
                }
                if (found) hits.Add(best);
            }
            if (baked != null) Object.Destroy(baked);
            hits.Sort((x, y) => x.distance.CompareTo(y.distance));
            return hits;
        }

        /// <summary>
        /// Positions and triangle indices of a mesh. A mesh without CPU data (imported .dae: Read/Write off,
        /// ModelImportRules) is read back from its GPU buffers — a stall, acceptable once per F1. Null: not possible.
        /// </summary>
        sealed class Geometry
        {
            public Vector3[] positions;
            /// <summary>Per sub mesh, base vertex applied; null — not triangles.</summary>
            public int[][] indices;

            public static Geometry Read(Mesh mesh, bool forceGpu = false)
            {
                var data = new Geometry { indices = new int[mesh.subMeshCount][] };
                if (mesh.isReadable && !forceGpu)
                {
                    data.positions = mesh.vertices;
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        if (mesh.GetTopology(sub) == MeshTopology.Triangles) data.indices[sub] = mesh.GetIndices(sub);
                    return data;
                }
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || !mesh.HasVertexAttribute(VertexAttribute.Position) ||
                    mesh.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32 ||
                    mesh.GetVertexAttributeDimension(VertexAttribute.Position) < 3) return null;
                try
                {
                    int stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
                    int offset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
                    int stride = mesh.GetVertexBufferStride(stream);
                    byte[] bytes;
                    using (var buffer = mesh.GetVertexBuffer(stream))
                    {
                        bytes = new byte[buffer.count * buffer.stride];
                        buffer.GetData(bytes);
                    }
                    data.positions = new Vector3[Mathf.Min(mesh.vertexCount, bytes.Length / stride)];
                    for (int v = 0; v < data.positions.Length; v++)
                    {
                        int at = v * stride + offset;
                        data.positions[v] = new Vector3(System.BitConverter.ToSingle(bytes, at), System.BitConverter.ToSingle(bytes, at + 4), System.BitConverter.ToSingle(bytes, at + 8));
                    }
                    bool wide = mesh.indexFormat == IndexFormat.UInt32;
                    int[] all;
                    using (var buffer = mesh.GetIndexBuffer())
                    {
                        all = new int[buffer.count];
                        if (wide) { var raw = new uint[buffer.count]; buffer.GetData(raw); for (int i = 0; i < raw.Length; i++) all[i] = (int)raw[i]; }
                        else { var raw = new ushort[buffer.count]; buffer.GetData(raw); for (int i = 0; i < raw.Length; i++) all[i] = raw[i]; }
                    }
                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        var d = mesh.GetSubMesh(sub);
                        if (d.topology != MeshTopology.Triangles) continue;
                        var list = new int[d.indexCount];
                        for (int i = 0; i < list.Length; i++) list[i] = all[d.indexStart + i] + d.baseVertex;
                        data.indices[sub] = list;
                    }
                    return data;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HplVisibleHit] " + mesh.name + ": no GPU read back: " + e.Message);
                    return null;
                }
            }
        }

        /// <summary>Checks: the GPU read back of a readable mesh gives its CPU positions and triangles.</summary>
        public static bool GpuReadMatches(Mesh mesh, out string error)
        {
            error = null;
            var gpu = Geometry.Read(mesh, true);
            if (gpu == null) { error = "no read back"; return false; }
            var positions = mesh.vertices;
            if (gpu.positions.Length != positions.Length) { error = $"{gpu.positions.Length} positions, {positions.Length} expected"; return false; }
            for (int i = 0; i < positions.Length; i++)
                if ((gpu.positions[i] - positions[i]).sqrMagnitude > 1e-10f) { error = $"position {i}: {gpu.positions[i]} vs {positions[i]}"; return false; }
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var cpu = mesh.GetIndices(sub);
                if (gpu.indices[sub] == null || !System.Linq.Enumerable.SequenceEqual(gpu.indices[sub], cpu)) { error = "indices of sub mesh " + sub; return false; }
            }
            return true;
        }

        static Bounds Inflate(Bounds bounds) { bounds.Expand(0.01f); return bounds; }

        /// <summary>Möller–Trumbore, both sides; t along <paramref name="direction"/>.</summary>
        static bool Intersect(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float t)
        {
            t = 0;
            Vector3 ab = b - a, ac = c - a, p = Vector3.Cross(direction, ac);
            float det = Vector3.Dot(ab, p);
            if (Mathf.Abs(det) < 1e-12f) return false;
            float inv = 1f / det;
            Vector3 s = origin - a;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0 || u > 1) return false;
            Vector3 q = Vector3.Cross(s, ab);
            float v = Vector3.Dot(direction, q) * inv;
            if (v < 0 || u + v > 1) return false;
            t = Vector3.Dot(ac, q) * inv;
            return t > 0;
        }
    }
}
