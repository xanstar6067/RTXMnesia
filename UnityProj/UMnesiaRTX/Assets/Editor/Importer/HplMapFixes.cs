using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Deviations from the original maps (docs/Reference/LevelImport.md, «Правки карт»): static objects cut by a plane.
    /// The part with normal · p &lt; distance (HPL2 world space) is removed from the instance's meshes (a cut copy,
    /// the cut edge interpolates every vertex attribute) and its box colliders (shortened along the box axis the
    /// plane is perpendicular to). Shared model meshes are not changed.
    /// </summary>
    static class HplMapFixes
    {
        readonly struct Trim
        {
            public readonly string map, name;
            public readonly Vector3 normal;
            public readonly float distance;
            public Trim(string map, string name, Vector3 normal, float distance) { this.map = map; this.name = name; this.normal = normal; this.distance = distance; }
        }

        static readonly Trim[] Trims =
        {
            // Report 21-14-53: the 4 m wall module goes 1.37 m out of the wall wall_default_square_brick_7 (face
            // x = 2.222) into the room, a one-sided sliver seen from one side only. The game's map_cache has it too.
            // Cut 3 cm inside that wall; the rest of the module is the wall of the room behind it.
            new("maps/main/ch01/00_rainy_hall.map", "wall_default_4", new Vector3(1f, 0f, 0f), 2.25f),
        };

        /// <summary>Applies the cuts for <paramref name="go"/> (a placed static object); new meshes go through <paramref name="add"/>.</summary>
        public static void Apply(string mapRelative, GameObject go, Func<Mesh, Mesh> add, ImportLog log)
        {
            string map = mapRelative.Replace('\\', '/');
            foreach (var trim in Trims)
            {
                if (!string.Equals(trim.map, map, StringComparison.OrdinalIgnoreCase) || go.name != trim.name) continue;
                var plane = new Plane(HplSpace.Direction(trim.normal), 0f) { distance = -trim.distance };
                int meshes = 0, boxes = 0, removed = 0;
                var cut = new Dictionary<Mesh, Mesh>();
                foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null) continue;
                    var mesh = Cut(filter.sharedMesh, filter.transform.localToWorldMatrix, plane, add);
                    if (mesh == filter.sharedMesh) continue;
                    cut[filter.sharedMesh] = mesh;
                    filter.sharedMesh = mesh;
                    meshes++;
                }
                foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                {
                    switch (collider)
                    {
                        case MeshCollider mc when mc.sharedMesh != null:
                            if (!cut.TryGetValue(mc.sharedMesh, out var m))
                                cut[mc.sharedMesh] = m = Cut(mc.sharedMesh, mc.transform.localToWorldMatrix, plane, add);
                            mc.sharedMesh = m;
                            break;
                        case BoxCollider box:
                            switch (CutBox(box, plane))
                            {
                                case 1: boxes++; break;
                                case 2: removed++; break;
                                case -1: log.Warn($"{trim.name}: box collider {box.name} is not perpendicular to the cut, kept"); break;
                            }
                            break;
                        default:
                            if (collider.bounds.min != collider.bounds.max) log.Warn($"{trim.name}: collider {collider.name} ({collider.GetType().Name}) not cut");
                            break;
                    }
                }
                log.Info($"Map fix: {trim.name} cut at HPL {trim.normal}·p = {trim.distance}: {meshes} meshes, {boxes} boxes shortened, {removed} disabled");
            }
        }

        /// <summary>0 untouched, 1 shortened, 2 disabled (all on the cut side), −1 not perpendicular to an axis.</summary>
        static int CutBox(BoxCollider box, Plane world)
        {
            var m = box.transform.localToWorldMatrix;
            // dot(n, A·p + t) ≥ −d  ⇔  dot(Aᵀ·n, p) ≥ −d − dot(n, t)
            Vector3 n = m.transpose.MultiplyVector(world.normal);
            float limit = -world.distance - Vector3.Dot(world.normal, (Vector3)m.GetColumn(3));
            int axis = Mathf.Abs(n.x) >= Mathf.Abs(n.y) ? (Mathf.Abs(n.x) >= Mathf.Abs(n.z) ? 0 : 2) : (Mathf.Abs(n.y) >= Mathf.Abs(n.z) ? 1 : 2);
            float along = n[axis];
            if (Mathf.Abs(along) < 0.999f * n.magnitude) return -1;
            float bound = limit / along;
            float min = box.center[axis] - box.size[axis] * 0.5f, max = box.center[axis] + box.size[axis] * 0.5f;
            if (along > 0f) min = Mathf.Max(min, bound); else max = Mathf.Min(max, bound);
            float oldSize = box.size[axis];
            if (max <= min)
            {
                box.enabled = false;
                return 2;
            }
            if (Mathf.Approximately(max - min, oldSize)) return 0;
            var center = box.center; center[axis] = (min + max) * 0.5f; box.center = center;
            var size = box.size; size[axis] = max - min; box.size = size;
            return 1;
        }

        /// <summary>The part of <paramref name="mesh"/> on the plane's positive side, or the mesh itself if nothing is cut.</summary>
        static Mesh Cut(Mesh mesh, Matrix4x4 toWorld, Plane world, Func<Mesh, Mesh> add)
        {
            var positions = mesh.vertices;
            var side = new float[positions.Length];
            bool any = false;
            for (int i = 0; i < positions.Length; i++)
            {
                side[i] = world.GetDistanceToPoint(toWorld.MultiplyPoint3x4(positions[i]));
                any |= side[i] < 0f;
            }
            if (!any) return mesh;

            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var colors = mesh.colors;
            var uvs = new List<Vector4>[8];
            var dimensions = new int[8];
            for (int c = 0; c < 8; c++)
                if (mesh.HasVertexAttribute(VertexAttribute.TexCoord0 + c))
                {
                    mesh.GetUVs(c, uvs[c] = new List<Vector4>());
                    dimensions[c] = mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0 + c);
                }

            var outPositions = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outTangents = new List<Vector4>();
            var outColors = new List<Color>();
            var outUvs = new List<Vector4>[8];
            for (int c = 0; c < 8; c++) if (uvs[c] != null) outUvs[c] = new List<Vector4>();
            var kept = new Dictionary<int, int>();
            var edges = new Dictionary<(int, int), int>();

            int Keep(int i)
            {
                if (kept.TryGetValue(i, out int k)) return k;
                k = outPositions.Count;
                outPositions.Add(positions[i]);
                if (normals.Length > 0) outNormals.Add(normals[i]);
                if (tangents.Length > 0) outTangents.Add(tangents[i]);
                if (colors.Length > 0) outColors.Add(colors[i]);
                for (int c = 0; c < 8; c++) outUvs[c]?.Add(uvs[c][i]);
                kept[i] = k;
                return k;
            }

            int Edge(int a, int b)
            {
                var key = a < b ? (a, b) : (b, a);
                if (edges.TryGetValue(key, out int k)) return k;
                float t = side[key.Item1] / (side[key.Item1] - side[key.Item2]);
                int i = key.Item1, j = key.Item2;
                k = outPositions.Count;
                outPositions.Add(Vector3.Lerp(positions[i], positions[j], t));
                if (normals.Length > 0) outNormals.Add(Vector3.Lerp(normals[i], normals[j], t).normalized);
                if (tangents.Length > 0)
                {
                    Vector3 tangent = Vector3.Lerp(tangents[i], tangents[j], t).normalized;
                    outTangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w));
                }
                if (colors.Length > 0) outColors.Add(Color.Lerp(colors[i], colors[j], t));
                for (int c = 0; c < 8; c++) outUvs[c]?.Add(Vector4.Lerp(uvs[c][i], uvs[c][j], t));
                edges[key] = k;
                return k;
            }

            var subMeshes = new List<int>[mesh.subMeshCount];
            var polygon = new List<int>(4);
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var triangles = mesh.GetTriangles(s);
                var output = subMeshes[s] = new List<int>();
                for (int t = 0; t + 2 < triangles.Length; t += 3)
                {
                    // Sutherland–Hodgman against one plane: the kept polygon in the triangle's winding.
                    polygon.Clear();
                    for (int e = 0; e < 3; e++)
                    {
                        int a = triangles[t + e], b = triangles[t + (e + 1) % 3];
                        bool inA = side[a] >= 0f, inB = side[b] >= 0f;
                        if (inA) polygon.Add(Keep(a));
                        if (inA != inB) polygon.Add(Edge(a, b));
                    }
                    for (int p = 1; p + 1 < polygon.Count; p++)
                    {
                        output.Add(polygon[0]);
                        output.Add(polygon[p]);
                        output.Add(polygon[p + 1]);
                    }
                }
            }

            var result = new Mesh { name = mesh.name + " (cut)" };
            if (outPositions.Count > 65535) result.indexFormat = IndexFormat.UInt32;
            result.SetVertices(outPositions);
            if (outNormals.Count > 0) result.SetNormals(outNormals);
            if (outTangents.Count > 0) result.SetTangents(outTangents);
            if (outColors.Count > 0) result.SetColors(outColors);
            for (int c = 0; c < 8; c++)
            {
                if (outUvs[c] == null) continue;
                // The same channel sizes as the source (Vector4 lists would make every channel 4 wide).
                if (dimensions[c] == 2) result.SetUVs(c, outUvs[c].ConvertAll(v => (Vector2)v));
                else if (dimensions[c] == 3) result.SetUVs(c, outUvs[c].ConvertAll(v => (Vector3)v));
                else result.SetUVs(c, outUvs[c]);
            }
            result.subMeshCount = subMeshes.Length;
            for (int s = 0; s < subMeshes.Length; s++) result.SetTriangles(subMeshes[s], s, false);
            result.RecalculateBounds();
            return add(result);
        }
    }
}
