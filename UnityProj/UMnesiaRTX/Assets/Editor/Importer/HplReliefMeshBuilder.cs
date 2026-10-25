using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Importer
{
    /// <summary>Conforming subdivision and physical height displacement, performed only by the importer.</summary>
    public static class HplReliefMeshBuilder
    {
        public const int Version = 3;
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
            public Func<Vector2, float> heightMetres;
            public bool geometryNormals, boards;
            /// <summary>The relief fades out within 2.5 cm of geometric creases and UV seams too, not only of open edges.
            /// Off: it continues over corners (coincident vertices move together), joints notch the corner edges.</summary>
            public bool fadeAtCreases = true;
            /// <summary>Adaptive (metres, 0: uniform): an edge longer than the spacing is split only where the height
            /// along it leaves the straight line by more than this, or where it is longer than maxEdge. Triangles go
            /// to joints and shoulders; flat faces stay coarse.</summary>
            public float tolerance, maxEdge = 0.15f;
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
            // Displacement of each vertex, applied after all submeshes: coincident vertices (a UV or normal seam, a
            // material border) then move together, so the relief has no cracks along seams.
            var offsets = new List<Vector3>();
            var displaced = new List<bool>();
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
                var localOffsets = new Vector3[0];
                if (surface != null)
                {
                    if (surface.boards)
                    {
                        BuildBoards(ref local, ref triangles, surface, scale, triangleBudget - totalTriangles);
                    }
                    else
                    {
                    var borders = Borders(local, triangles, scale);
                    float Height(Vector2 uv)
                    {
                        uv = Vector2.Scale(uv, surface.uvScale) + surface.uvOffset;
                        return surface.heightMetres != null ? surface.heightMetres(uv) : (surface.height.Sample(uv) - surface.reference) * surface.amplitude;
                    }
                    // Adaptive: decided per geometric edge, so both sides of a UV seam split alike (no T-junctions);
                    // an edge kept whole stays the same edge, its decision is reused in later passes.
                    (Vector3Int, Vector3Int) Geometric(int a, int b)
                    {
                        Vector3Int x = Vector3Int.RoundToInt(local[a].p * 100000), y = Vector3Int.RoundToInt(local[b].p * 100000);
                        return x.x < y.x || x.x == y.x && (x.y < y.y || x.y == y.y && x.z <= y.z) ? (x, y) : (y, x);
                    }
                    var split = new Dictionary<(Vector3Int, Vector3Int), bool>();
                    for (int iteration = 0; iteration < 20; iteration++)
                    {
                        var midpoints = new Dictionary<Edge, int>();
                        if (surface.tolerance > 0)
                        {
                            void Decide(int a, int b)
                            {
                                float length = Vector3.Scale(local[a].p - local[b].p, scale).magnitude;
                                if (length <= spacing) return;
                                var key = Geometric(a, b);
                                if (split.ContainsKey(key)) return;
                                bool want = length > surface.maxEdge;
                                if (!want)
                                {
                                    Vector2 ua = local[a].uv0, ub = local[b].uv0;
                                    float ha = Height(ua), hb = Height(ub);
                                    int steps = Mathf.Clamp(Mathf.CeilToInt(length / 0.003f), 2, 32);
                                    for (int k = 1; k < steps && !want; k++)
                                    {
                                        float t = k / (float)steps;
                                        want = Mathf.Abs(Height(Vector2.Lerp(ua, ub, t)) - Mathf.Lerp(ha, hb, t)) > surface.tolerance;
                                    }
                                }
                                split[key] = want;
                            }
                            for (int t = 0; t < triangles.Count; t += 3)
                            { Decide(triangles[t], triangles[t + 1]); Decide(triangles[t + 1], triangles[t + 2]); Decide(triangles[t + 2], triangles[t]); }
                        }
                        void Mark(int a, int b)
                        {
                            var edge = new Edge(a, b);
                            if (midpoints.ContainsKey(edge) || Vector3.Scale(local[a].p - local[b].p, scale).sqrMagnitude <= spacing * spacing) return;
                            if (surface.tolerance > 0 && (!split.TryGetValue(Geometric(a, b), out bool want) || !want)) return;
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
                    localOffsets = new Vector3[local.Count];
                    for (int i = 0; i < local.Count; i++)
                    {
                        var vertex = local[i];
                        var point = Vector3.Scale(vertex.p, scale);
                        float distance = float.PositiveInfinity;
                        foreach (var border in borders)
                        {
                            if (!surface.fadeAtCreases && border.count == 2) continue;
                            Vector3 edge = border.b - border.a;
                            float along = edge.sqrMagnitude < 1e-12f ? 0 : Mathf.Clamp01(Vector3.Dot(point - border.a, edge) / edge.sqrMagnitude);
                            distance = Mathf.Min(distance, (point - border.a - edge * along).magnitude);
                        }
                        float fade = Mathf.SmoothStep(0, 1, distance / 0.025f);
                        var textureUv = Vector2.Scale((Vector2)vertex.uv0, surface.uvScale) + surface.uvOffset;
                        float height = (surface.heightMetres != null ? surface.heightMetres(textureUv) :
                            (surface.height.Sample(textureUv) - surface.reference) * surface.amplitude) * fade;
                        var direction = new Vector3(vertex.n.x / scale.x, vertex.n.y / scale.y, vertex.n.z / scale.z).normalized;
                        // Keep the original tangent frame. The enhanced normal map already contains the large
                        // height slopes; recalculating this frame would apply those slopes a second time.
                        localOffsets[i] = new Vector3(direction.x / scale.x, direction.y / scale.y, direction.z / scale.z) * height;
                    }
                    }
                }
                int offset = vertices.Count;
                vertices.AddRange(local);
                for (int i = 0; i < local.Count; i++)
                {
                    bool moved = i < localOffsets.Length;
                    offsets.Add(moved ? localOffsets[i] : Vector3.zero);
                    displaced.Add(moved);
                }
                indices[sub] = triangles;
                for (int i = 0; i < triangles.Count; i++) triangles[i] += offset;
                totalTriangles += triangles.Count / 3;
            }
            // Coincident displaced vertices share the mean displacement (seams stay closed).
            var groups = new Dictionary<Vector3Int, (Vector3 sum, int count)>();
            Vector3Int Key(Vector3 p) => Vector3Int.RoundToInt(p * 100000);
            for (int i = 0; i < vertices.Count; i++)
                if (displaced[i])
                {
                    var key = Key(vertices[i].p);
                    groups.TryGetValue(key, out var group);
                    groups[key] = (group.sum + offsets[i], group.count + 1);
                }
            for (int i = 0; i < vertices.Count; i++)
                if (displaced[i])
                {
                    var group = groups[Key(vertices[i].p)];
                    var vertex = vertices[i]; vertex.p += group.sum / group.count; vertices[i] = vertex;
                }
            for (int sub = 0; sub < surfaces.Length; sub++)
            {
                if (surfaces[sub] == null || surfaces[sub].boards || !surfaces[sub].geometryNormals) continue;
                var triangles = indices[sub];
                var accumulated = new Dictionary<int, Vector3>();
                for (int t = 0; t < triangles.Count; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    var normal = Vector3.Cross(vertices[b].p - vertices[a].p, vertices[c].p - vertices[a].p);
                    foreach (int v in new[] { a, b, c }) { accumulated.TryGetValue(v, out var sum); accumulated[v] = sum + normal; }
                }
                foreach (var (v, sum) in accumulated)
                    if (sum.sqrMagnitude > 1e-16f) { var vertex = vertices[v]; vertex.n = sum.normalized; vertices[v] = vertex; }
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
            if (Array.Exists(surfaces, s => s != null && (s.geometryNormals || s.boards))) mesh.RecalculateTangents();
            return mesh;
        }

        static void BuildBoards(ref List<Vertex> local, ref List<int> triangles, Surface surface, Vector3 scale, int budget)
        {
            float[] rows = { 0, .130f, .255f, .382f, .505f, .632f, .757f, .882f, 1 };
            float[] ends = { .151f, .976f, .475f, .980f, .305f, .882f, .624f, .125f };
            const float gap = .002f / 2.2f, bottom = -.006f;
            var output = new List<Vertex>(); var indices = new List<int>();
            Vector2 Uv(Vertex v) => Vector2.Scale((Vector2)v.uv0, surface.uvScale) + surface.uvOffset;
            Vertex Blend(Vertex a, Vertex b, float t) => new Vertex {
                p = Vector3.LerpUnclamped(a.p, b.p, t), n = Vector3.LerpUnclamped(a.n, b.n, t).normalized,
                tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, t), color = Color.LerpUnclamped(a.color, b.color, t),
                uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, t), uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, t),
                uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, t), uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, t) };
            Vertex Lift(Vertex v, float height)
            {
                var direction = new Vector3(v.n.x / scale.x, v.n.y / scale.y, v.n.z / scale.z).normalized;
                v.p += new Vector3(direction.x / scale.x, direction.y / scale.y, direction.z / scale.z) * height;
                return v;
            }
            List<Vertex> Clip(List<Vertex> polygon, int axis, float boundary, bool greater)
            {
                var result = new List<Vertex>();
                if (polygon.Count == 0) return result;
                var previous = polygon[^1]; float before = Uv(previous)[axis] - boundary;
                bool insideBefore = greater ? before >= 0 : before <= 0;
                foreach (var current in polygon)
                {
                    float after = Uv(current)[axis] - boundary; bool insideAfter = greater ? after >= 0 : after <= 0;
                    if (insideAfter != insideBefore) result.Add(Blend(previous, current, before / (before - after)));
                    if (insideAfter) result.Add(current);
                    previous = current; before = after; insideBefore = insideAfter;
                }
                return result;
            }
            void Face(List<Vertex> polygon, float h)
            {
                int first = output.Count;
                foreach (var vertex in polygon) output.Add(Lift(vertex, h));
                for (int j = 1; j < polygon.Count - 1; j++) indices.AddRange(new[] { first, first + j, first + j + 1 });
            }
            for (int t = 0; t < triangles.Count; t += 3)
            {
                var original = new List<Vertex> { local[triangles[t]], local[triangles[t + 1]], local[triangles[t + 2]] };
                Face(original, bottom);
                var a = Uv(original[0]); var b = Uv(original[1]); var c = Uv(original[2]);
                int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), maxX = Mathf.FloorToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - .000001f);
                int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), maxY = Mathf.FloorToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) - .000001f);
                if ((long)(maxX - minX + 1) * (maxY - minY + 1) > 1024) throw new InvalidOperationException("Board UV span exceeds safe budget.");
                for (int ty = minY; ty <= maxY; ty++) for (int tx = minX; tx <= maxX; tx++)
                for (int row = 0; row < 8; row++) for (int piece = 0; piece < 2; piece++)
                {
                    float x0 = tx + (piece == 0 ? 0 : ends[row] + gap), x1 = tx + (piece == 0 ? ends[row] - gap : 1);
                    float y0 = ty + 1 - rows[row + 1] + gap, y1 = ty + 1 - rows[row] - gap;
                    var polygon = Clip(Clip(Clip(Clip(original, 0, x0, true), 0, x1, false), 1, y0, true), 1, y1, false);
                    if (polygon.Count < 3) continue;
                    float height = .009f + Mathf.Sin(row * 3.7f + piece) * .0015f;
                    Face(polygon, height);
                    for (int j = 0; j < polygon.Count; j++)
                    {
                        var v = polygon[j]; var w = polygon[(j + 1) % polygon.Count]; var uv = Uv(v); var uw = Uv(w);
                        bool On(int axis, float line) => Mathf.Abs(uv[axis] - line) < .00001f && Mathf.Abs(uw[axis] - line) < .00001f;
                        // Source triangle edges inside a plank must not create internal side walls.
                        if (!(On(0, x0) && x0 > tx || On(0, x1) && x1 < tx + 1 || On(1, y0) || On(1, y1))) continue;
                        int first = output.Count;
                        var side = new[] { Lift(v, height), Lift(v, bottom), Lift(w, bottom), Lift(w, height) };
                        var normal = Vector3.Cross(side[1].p - side[0].p, side[2].p - side[0].p).normalized;
                        for (int k = 0; k < side.Length; k++) { var vertex = side[k]; vertex.n = normal;
                            if (k == 1 || k == 2) vertex.uv0 += new Vector4(.00002f, .00002f, 0, 0);
                            output.Add(vertex); }
                        indices.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
                    }
                    if (indices.Count / 3 > budget) throw new InvalidOperationException("Relief triangle budget: boards");
                }
            }
            local = output; triangles = indices;
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
