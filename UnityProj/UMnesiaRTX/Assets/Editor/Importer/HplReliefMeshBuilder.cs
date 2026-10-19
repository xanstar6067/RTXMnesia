using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Importer
{
    /// <summary>Conforming subdivision and physical height displacement, performed only by the importer.</summary>
    public static class HplReliefMeshBuilder
    {
        public const int Version = 2;
        public sealed class HeightField
        {
            public const int Size = 512;
            readonly float[] _values = new float[Size * Size];
            readonly float _low, _range;
            public HeightField(Texture2D texture)
            {
                // Low pass removes fine grain; normal maps retain the original fine surface detail.
                float stepX = 1f / texture.width, stepY = 1f / texture.height;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size, sum = 0;
                        for (int j = -1; j <= 1; j++)
                            for (int i = -1; i <= 1; i++) sum += texture.GetPixelBilinear(u + i * stepX * 2, v + j * stepY * 2).r;
                        _values[y * Size + x] = sum / 9;
                    }
                var sorted = (float[])_values.Clone();
                Array.Sort(sorted);
                _low = sorted[(int)(sorted.Length * 0.05f)];
                _range = Mathf.Max(0.08f, sorted[(int)(sorted.Length * 0.95f)] - _low);
            }
            public float Sample(Vector2 uv)
            {
                float x = Mathf.Repeat(uv.x, 1) * Size - 0.5f, y = Mathf.Repeat(uv.y, 1) * Size - 0.5f;
                int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
                float At(int a, int b) => _values[((b + Size) % Size) * Size + (a + Size) % Size];
                float value = Mathf.Lerp(Mathf.Lerp(At(ix, iy), At(ix + 1, iy), x - ix),
                    Mathf.Lerp(At(ix, iy + 1), At(ix + 1, iy + 1), x - ix), y - iy);
                return Mathf.Clamp01((value - _low) / _range);
            }
        }
        public sealed class Surface
        {
            public HeightField height;
            public Vector2 uvScale = Vector2.one, uvOffset;
            public float amplitude = 0.04f, reference = 0.7f;
        }
        struct Vertex
        {
            public Vector3 p, n;
            public Vector4 tangent, uv0, uv1, uv2, uv3;
            public Color color;
            public static Vertex Mid(Vertex a, Vertex b) => new()
            {
                p = (a.p + b.p) * 0.5f, n = (a.n + b.n).normalized,
                tangent = new Vector4((a.tangent.x + b.tangent.x) * 0.5f, (a.tangent.y + b.tangent.y) * 0.5f,
                    (a.tangent.z + b.tangent.z) * 0.5f, a.tangent.w),
                uv0 = (a.uv0 + b.uv0) * 0.5f, uv1 = (a.uv1 + b.uv1) * 0.5f,
                uv2 = (a.uv2 + b.uv2) * 0.5f, uv3 = (a.uv3 + b.uv3) * 0.5f, color = (a.color + b.color) * 0.5f
            };
        }
        readonly struct Edge : IEquatable<Edge>
        {
            public readonly int a, b;
            public Edge(int x, int y) { a = Mathf.Min(x, y); b = Mathf.Max(x, y); }
            public bool Equals(Edge other) => a == other.a && b == other.b;
            public override bool Equals(object obj) => obj is Edge other && Equals(other);
            public override int GetHashCode() => unchecked(a * 397 ^ b);
        }
        sealed class Border
        {
            public Vector3 a, b, normal;
            public Vector2 uvA, uvB;
            public int count;
            public bool crease;
        }

        public static Mesh Build(Mesh original, Surface[] surfaces, Vector3 scale, float spacing, int triangleBudget = 120000)
        {
            if (original.subMeshCount != surfaces.Length || scale.x < 0.0001f || scale.y < 0.0001f || scale.z < 0.0001f)
                throw new InvalidOperationException("Invalid relief surfaces/scale: " + original.name);
            var positions = original.vertices;
            var normals = original.normals;
            var tangents = original.tangents;
            if (normals.Length != positions.Length || tangents.Length != positions.Length || original.blendShapeCount != 0)
                throw new InvalidOperationException("Relief requires a static mesh with normals/tangents: " + original.name);
            var channels = new List<Vector4>[4];
            for (int i = 0; i < channels.Length; i++) { channels[i] = new(); original.GetUVs(i, channels[i]); }
            var colors = original.colors;
            var vertices = new List<Vertex>();
            var indices = new List<int>[surfaces.Length];
            int totalTriangles = 0;
            for (int sub = 0; sub < surfaces.Length; sub++)
            {
                if (original.GetTopology(sub) != MeshTopology.Triangles) throw new InvalidOperationException("Relief requires triangles.");
                var local = new List<Vertex>();
                var remap = new Dictionary<int, int>();
                int Local(int index)
                {
                    if (remap.TryGetValue(index, out int found)) return found;
                    int result = local.Count;
                    remap[index] = result;
                    Vector4 Uv(int channel) => channels[channel].Count == positions.Length ? channels[channel][index] : Vector4.zero;
                    local.Add(new Vertex { p = positions[index], n = normals[index], tangent = tangents[index],
                        uv0 = Uv(0), uv1 = Uv(1), uv2 = Uv(2), uv3 = Uv(3), color = colors.Length == positions.Length ? colors[index] : Color.white });
                    return result;
                }
                var triangles = new List<int>();
                foreach (int index in original.GetTriangles(sub)) triangles.Add(Local(index));
                var surface = surfaces[sub];
                if (surface != null)
                {
                    var borders = Borders(local, triangles, scale);
                    for (int iteration = 0; iteration < 20; iteration++)
                    {
                        var midpoints = new Dictionary<Edge, int>();
                        void Mark(int a, int b)
                        {
                            var edge = new Edge(a, b);
                            if (midpoints.ContainsKey(edge) || Vector3.Scale(local[a].p - local[b].p, scale).sqrMagnitude <= spacing * spacing) return;
                            midpoints[edge] = local.Count;
                            local.Add(Vertex.Mid(local[a], local[b]));
                        }
                        for (int t = 0; t < triangles.Count; t += 3)
                        { Mark(triangles[t], triangles[t + 1]); Mark(triangles[t + 1], triangles[t + 2]); Mark(triangles[t + 2], triangles[t]); }
                        if (midpoints.Count == 0) break;
                        var next = new List<int>(triangles.Count * 2);
                        void Add(int a, int b, int c) { next.Add(a); next.Add(b); next.Add(c); }
                        for (int t = 0; t < triangles.Count; t += 3)
                        {
                            int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                            bool ab = midpoints.TryGetValue(new Edge(a, b), out int d), bc = midpoints.TryGetValue(new Edge(b, c), out int e),
                                ca = midpoints.TryGetValue(new Edge(c, a), out int f);
                            int mask = (ab ? 1 : 0) | (bc ? 2 : 0) | (ca ? 4 : 0);
                            switch (mask)
                            {
                                case 0: Add(a, b, c); break;
                                case 1: Add(a, d, c); Add(d, b, c); break;
                                case 2: Add(b, e, a); Add(e, c, a); break;
                                case 4: Add(c, f, b); Add(f, a, b); break;
                                case 3: Add(b, e, d); Add(a, d, c); Add(d, e, c); break;
                                case 6: Add(c, f, e); Add(b, e, a); Add(e, f, a); break;
                                case 5: Add(a, d, f); Add(c, f, b); Add(f, d, b); break;
                                case 7: Add(a, d, f); Add(d, b, e); Add(f, e, c); Add(d, e, f); break;
                            }
                        }
                        if (next.Count / 3 + totalTriangles > triangleBudget) throw new InvalidOperationException("Relief triangle budget: " + original.name);
                        triangles = next;
                    }
                    for (int i = 0; i < local.Count; i++)
                    {
                        var vertex = local[i];
                        var point = Vector3.Scale(vertex.p, scale);
                        float distance = float.PositiveInfinity;
                        foreach (var border in borders)
                        {
                            Vector3 edge = border.b - border.a;
                            float along = edge.sqrMagnitude < 1e-12f ? 0 : Mathf.Clamp01(Vector3.Dot(point - border.a, edge) / edge.sqrMagnitude);
                            distance = Mathf.Min(distance, (point - border.a - edge * along).magnitude);
                        }
                        float fade = Mathf.SmoothStep(0, 1, distance / 0.025f);
                        float height = (surface.height.Sample(Vector2.Scale((Vector2)vertex.uv0, surface.uvScale) + surface.uvOffset) - surface.reference) * surface.amplitude * fade;
                        var direction = new Vector3(vertex.n.x / scale.x, vertex.n.y / scale.y, vertex.n.z / scale.z).normalized;
                        vertex.p += new Vector3(direction.x / scale.x, direction.y / scale.y, direction.z / scale.z) * height;
                        // Keep the original tangent frame. The enhanced normal map already contains the large
                        // height slopes; recalculating this frame would apply those slopes a second time.
                        local[i] = vertex;
                    }
                }
                int offset = vertices.Count;
                vertices.AddRange(local);
                indices[sub] = triangles;
                for (int i = 0; i < triangles.Count; i++) triangles[i] += offset;
                totalTriangles += triangles.Count / 3;
            }
            var mesh = new Mesh { name = original.name + " relief", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            var p = new List<Vector3>(); var n = new List<Vector3>(); var tan = new List<Vector4>(); var col = new List<Color>();
            var uv = new List<Vector4>[] { new(), new(), new(), new() };
            foreach (var vertex in vertices)
            {
                p.Add(vertex.p); n.Add(vertex.n); tan.Add(vertex.tangent); col.Add(vertex.color);
                uv[0].Add(vertex.uv0); uv[1].Add(vertex.uv1); uv[2].Add(vertex.uv2); uv[3].Add(vertex.uv3);
            }
            mesh.SetVertices(p); mesh.SetNormals(n); mesh.SetTangents(tan);
            if (colors.Length == positions.Length) mesh.SetColors(col);
            for (int i = 0; i < uv.Length; i++) if (channels[i].Count == positions.Length) mesh.SetUVs(i, uv[i]);
            mesh.subMeshCount = surfaces.Length;
            for (int i = 0; i < indices.Length; i++) mesh.SetTriangles(indices[i], i, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        static List<Border> Borders(List<Vertex> vertices, List<int> triangles, Vector3 scale)
        {
            var welded = new Dictionary<Vector3Int, int>();
            var ids = new int[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                var key = Vector3Int.RoundToInt(vertices[i].p * 100000);
                if (!welded.TryGetValue(key, out ids[i])) welded[key] = ids[i] = welded.Count;
            }
            var edges = new Dictionary<Edge, Border>();
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                var normal = Vector3.Cross(vertices[b].p - vertices[a].p, vertices[c].p - vertices[a].p).normalized;
                void Add(int x, int y)
                {
                    var key = new Edge(ids[x], ids[y]);
                    Vector2 uvA = ids[x] <= ids[y] ? (Vector2)vertices[x].uv0 : (Vector2)vertices[y].uv0;
                    Vector2 uvB = ids[x] <= ids[y] ? (Vector2)vertices[y].uv0 : (Vector2)vertices[x].uv0;
                    if (!edges.TryGetValue(key, out var edge)) edges[key] = edge = new Border
                        { a = Vector3.Scale(vertices[x].p, scale), b = Vector3.Scale(vertices[y].p, scale), normal = normal, uvA = uvA, uvB = uvB };
                    edge.crease |= Vector3.Dot(edge.normal, normal) < 0.8f;
                    bool Different(Vector2 a, Vector2 b)
                    {
                        var difference = a - b;
                        return Mathf.Abs(difference.x - Mathf.Round(difference.x)) > 0.0001f || Mathf.Abs(difference.y - Mathf.Round(difference.y)) > 0.0001f;
                    }
                    edge.crease |= Different(edge.uvA, uvA) || Different(edge.uvB, uvB);
                    edge.count++;
                }
                Add(a, b); Add(b, c); Add(c, a);
            }
            var result = new List<Border>();
            foreach (var edge in edges.Values) if (edge.count != 2 || edge.crease) result.Add(edge);
            return result;
        }
    }
}
