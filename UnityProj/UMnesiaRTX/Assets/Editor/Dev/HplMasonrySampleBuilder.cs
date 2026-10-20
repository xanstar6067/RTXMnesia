using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.EditorDev
{
    /// <summary>Art-directed stand experiment: trace mortar, then build separate bevelled solid stones.
    /// Seed positions describe this sample's UV islands, not a general-purpose material importer.</summary>
    public static class HplMasonrySampleBuilder
    {
        const int Size = 256, Sides = 80, Rings = 5;
        // Coordinates are fractions of the displayed crop, measured from its top left.
        static readonly Vector2[] WallSeeds = Rows(
            new[] { .04f, .06f, .22f, .38f, .55f, .71f, .9f },
            new[] { .23f, .02f, .15f, .32f, .51f, .69f, .89f },
            new[] { .44f, .075f, .21f, .39f, .60f, .77f, .94f },
            new[] { .61f, .02f, .16f, .34f, .51f, .69f, .89f },
            new[] { .78f, .055f, .23f, .41f, .61f, .81f, .98f },
            new[] { .965f, .025f, .16f, .35f, .55f, .72f, .93f });
        static readonly Vector2[] FloorSeeds = Rows(
            new[] { .015f, .08f, .36f, .59f, .89f },
            new[] { .09f, .02f, .14f, .29f, .52f, .7f, .89f },
            new[] { .24f, .1f, .32f, .51f, .7f, .89f },
            new[] { .4f, .02f, .23f, .43f, .59f, .77f, .96f },
            new[] { .55f, .08f, .3f, .49f, .7f, .91f },
            new[] { .7f, .025f, .2f, .42f, .65f, .85f, .98f },
            new[] { .88f, .06f, .26f, .46f, .63f, .82f },
            new[] { .99f, .12f, .36f, .6f, .79f, .97f });

        static Vector2[] Rows(params float[][] rows) => rows.SelectMany(r => r.Skip(1).Select(x => new Vector2(x, 1 - r[0]))).ToArray();

        public static Mesh Build(Texture2D height, Vector2 lo, Vector2 hi, bool wall)
        {
            var seeds = wall ? WallSeeds : FloorSeeds;
            var field = new float[Size * Size];
            Vector2 Uv(Vector2 p) => lo + Vector2.Scale(p, hi - lo);
            float Raw(Vector2 p)
            {
                var uv = Uv(p); float sum = 0;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                    sum += height.GetPixelBilinear(uv.x + x / (float)height.width * 3, uv.y + y / (float)height.height * 3).r;
                return sum / 9;
            }
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                field[y * Size + x] = Raw(new Vector2(x, y) / (Size - 1));
            var sorted = field.OrderBy(v => v).ToArray();
            float low = sorted[sorted.Length / 20], range = Mathf.Max(.05f, sorted[sorted.Length * 9 / 10] - low);
            for (int i = 0; i < field.Length; i++) field[i] = Mathf.Clamp01((field[i] - low) / range);
            int Index(Vector2 p) => Mathf.Clamp(Mathf.RoundToInt(p.y * (Size - 1)), 0, Size - 1) * Size + Mathf.Clamp(Mathf.RoundToInt(p.x * (Size - 1)), 0, Size - 1);
            var labels = Segment(field, seeds, Index);
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            var sideTriangles = new List<int>(); bool sides = false;
            int Vertex(Vector2 p, float h)
            {
                vertices.Add(new Vector3((p.x - .5f) * 2.2f, (p.y - .5f) * 2.2f, -h));
                uvs.Add(Uv(p)); return vertices.Count - 1;
            }
            void Triangle(int a, int b, int c) { var target = sides ? sideTriangles : triangles; target.Add(a); target.Add(b); target.Add(c); }
            for (int stone = 0; stone < seeds.Length; stone++)
            {
                var center = seeds[stone];
                var outline = new Vector2[Sides];
                for (int side = 0; side < Sides; side++)
                {
                    float angle = side * Mathf.PI * 2 / Sides;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float radius = 0;
                    for (int step = 1; step < Size; step++)
                    {
                        var p = center + direction * (step / (float)Size);
                        if (p.x <= 0 || p.x >= 1 || p.y <= 0 || p.y >= 1 || labels[Index(p)] != stone) break;
                        radius = step / (float)Size;
                    }
                    // Mortar remains visible between stones. This cuts into the stone colour rather
                    // than stretching the mortar colour up its new vertical side.
                    radius = Mathf.Max(.001f, radius - (wall ? .008f : .006f));
                    outline[side] = center + direction * radius;
                }
                // Smooth only the contour's sub-centimetre stair steps, preserving irregular shapes.
                for (int pass = 0; pass < 2; pass++)
                {
                    var smoothed = new Vector2[Sides];
                    for (int s = 0; s < Sides; s++) smoothed[s] = (outline[(s + Sides - 1) % Sides] + outline[s] * 2 + outline[(s + 1) % Sides]) / 4;
                    outline = smoothed;
                }
                float random = Mathf.Repeat(Mathf.Sin(stone * 78.233f + 4.1f) * 43758.5453f, 1);
                float elevation = wall ? Mathf.Lerp(.075f, .125f, random) : Mathf.Lerp(.032f, .062f, random);
                float tiltX = Mathf.Sin(stone * 7.31f) * (wall ? .05f : .025f), tiltY = Mathf.Cos(stone * 4.17f) * (wall ? .05f : .025f);
                float Face(Vector2 p) => elevation + (Raw(p) - Raw(center)) * .012f + (p.x - center.x) * tiltX + (p.y - center.y) * tiltY;
                int top = Vertex(center, Face(center));
                int rings = vertices.Count;
                for (int r = 0; r < Rings; r++)
                {
                    float t = (r + 1f) / Rings;
                    // Most of the face stays firm; only the outer shoulder rounds towards the side.
                    float drop = r == Rings - 1 ? (wall ? .018f : .009f) : r == Rings - 2 ? .0025f : 0;
                    for (int s = 0; s < Sides; s++)
                    {
                        var p = Vector2.Lerp(center, outline[s], t);
                        Vertex(p, Face(p) - drop);
                    }
                }
                for (int s = 0; s < Sides; s++) Triangle(top, rings + (s + 1) % Sides, rings + s);
                for (int r = 1; r < Rings; r++) for (int s = 0; s < Sides; s++)
                {
                    int a = rings + (r - 1) * Sides + s, b = rings + (r - 1) * Sides + (s + 1) % Sides;
                    int c = rings + r * Sides + s, d = rings + r * Sides + (s + 1) % Sides;
                    Triangle(a, b, c); Triangle(c, b, d);
                }
                // Duplicate the rim for a crisp face/side normal transition. Close every stone on
                // its back as well: the result is actual solid geometry, including for ray tracing.
                int rim = vertices.Count;
                for (int s = 0; s < Sides; s++)
                {
                    int v = Vertex(outline[s], Face(outline[s]) - (wall ? .018f : .009f));
                    // The photograph contains mortar around the outline, but no photograph of
                    // the original stone's side. Borrow its interior colour for the new side.
                    uvs[v] = Uv(Vector2.Lerp(outline[s], center, .35f));
                }
                int bottom = vertices.Count;
                for (int s = 0; s < Sides; s++)
                {
                    int v = Vertex(outline[s], -.012f);
                    // Give the side a genuine second UV direction; identical top/bottom UVs
                    // collapse its tangent basis and stretch a single texel down the entire side.
                    uvs[v] = Uv(Vector2.Lerp(outline[s], center, .7f));
                }
                int back = Vertex(center, -.012f);
                sides = true;
                for (int s = 0; s < Sides; s++)
                {
                    int next = (s + 1) % Sides;
                    Triangle(rim + s, rim + next, bottom + s); Triangle(bottom + s, rim + next, bottom + next);
                    Triangle(back, bottom + s, bottom + next);
                }
                sides = false;
            }
            // The original texture at a recessed level supplies the visible mortar in the gaps.
            int mortar = Vertex(Vector2.zero, -.008f);
            Vertex(Vector2.right, -.008f); Vertex(Vector2.one, -.008f); Vertex(Vector2.up, -.008f);
            Triangle(mortar, mortar + 2, mortar + 1); Triangle(mortar, mortar + 3, mortar + 2);
            var mesh = new Mesh { name = wall ? "Individual protruding wall blocks" : "Individual protruding paving stones", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.subMeshCount = 2;
            mesh.SetTriangles(triangles, 0); mesh.SetTriangles(sideTriangles, 1);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            Debug.Log($"[masonry sample] {mesh.name}: {seeds.Length} solid stones, {vertices.Count} vertices, {(triangles.Count + sideTriangles.Count) / 3} triangles, {mesh.bounds.size.z:F3} m depth");
            return mesh;
        }

        // Multi-source Dijkstra follows broad, low height valleys. Every marker retains its own
        // component, so small bumps inside a stone never become extra miniature stones.
        static int[] Segment(float[] field, Vector2[] seeds, Func<Vector2, int> index)
        {
            var labels = Enumerable.Repeat(-1, field.Length).ToArray();
            var distance = Enumerable.Repeat(float.PositiveInfinity, field.Length).ToArray();
            var queue = new SortedSet<(float cost, int pixel)>();
            for (int i = 0; i < seeds.Length; i++) { int p = index(seeds[i]); labels[p] = i; distance[p] = 0; queue.Add((0, p)); }
            while (queue.Count > 0)
            {
                var item = queue.Min; queue.Remove(item);
                int p = item.pixel, x = p % Size, y = p / Size;
                Visit(p - 1, x > 0); Visit(p + 1, x < Size - 1); Visit(p - Size, y > 0); Visit(p + Size, y < Size - 1);
                void Visit(int next, bool valid)
                {
                    if (!valid) return;
                    float cost = item.cost + 1 + 90 * Mathf.Pow(1 - (field[p] + field[next]) * .5f, 5);
                    if (cost >= distance[next]) return;
                    queue.Remove((distance[next], next)); distance[next] = cost; labels[next] = labels[p]; queue.Add((cost, next));
                }
            }
            return labels;
        }
    }
}
