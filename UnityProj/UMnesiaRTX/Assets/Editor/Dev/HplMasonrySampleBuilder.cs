using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.EditorDev
{
    /// <summary>Sample-specific masonry: a continuous surface with recessed mortar and firm stone faces.
    /// Markers identify stones in these texture crops; they are not a general material importer.</summary>
    public static class HplMasonrySampleBuilder
    {
        const int Size = 384;
        const float Width = 2.2f;
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

        public static Mesh BuildBoards()
        {
            // This texture's eight courses and staggered end joints, measured in image coordinates.
            float[] rows = { 0, .130f, .255f, .382f, .505f, .632f, .757f, .882f, 1 };
            float[] ends = { .151f, .976f, .475f, .980f, .305f, .882f, .624f, .125f };
            const float halfGap = .002f / Width, back = -.009f;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>();
            var tops = new List<int>(); var sides = new List<int>();
            Vector3 Point(Vector2 p, float height) => new Vector3((p.x - .5f) * Width, (p.y - .5f) * Width, -height);
            void Quad(Vector3[] p, Vector2[] tex, List<int> target)
            {
                int first = vertices.Count;
                vertices.AddRange(p); uv.AddRange(tex);
                target.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
            for (int row = 0; row < ends.Length; row++) for (int piece = 0; piece < 2; piece++)
            {
                float x0 = piece == 0 ? 0 : ends[row] + halfGap, x1 = piece == 0 ? ends[row] - halfGap : 1;
                float y0 = 1 - rows[row + 1] + (row == 7 ? 0 : halfGap);
                float y1 = 1 - rows[row] - (row == 0 ? 0 : halfGap);
                var tex = new[] { new Vector2(x0, y0), new Vector2(x0, y1), new Vector2(x1, y1), new Vector2(x1, y0) };
                float h = .009f + Mathf.Sin(row * 3.7f + piece) * .0015f;
                var front = tex.Select(p => Point(p, h)).ToArray();
                var bottom = tex.Select(p => Point(p, back)).ToArray();
                Quad(front, tex, tops);
                Quad(bottom.Reverse().ToArray(), tex.Reverse().ToArray(), sides);
                var center = (tex[0] + tex[2]) * .5f;
                for (int edge = 0; edge < 4; edge++)
                {
                    int next = (edge + 1) % 4;
                    Quad(new[] { front[edge], bottom[edge], bottom[next], front[next] },
                        new[] { tex[edge], Vector2.Lerp(tex[edge], center, .03f), Vector2.Lerp(tex[next], center, .03f), tex[next] }, sides);
                }
            }
            var baseUv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            Quad(baseUv.Select(p => Point(p, back - .001f)).ToArray(), baseUv, sides);
            var mesh = new Mesh { name = "Straight edged floor boards" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.subMeshCount = 2;
            mesh.SetTriangles(tops, 0); mesh.SetTriangles(sides, 1);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Build(Texture2D height, Vector2 lo, Vector2 hi, bool wall)
        {
            var seeds = wall ? WallSeeds : FloorSeeds;
            float panelHeight = wall ? Width * (hi.y - lo.y) / (hi.x - lo.x) : Width;
            var raw = new float[Size * Size];
            Vector2 Uv(Vector2 p) => lo + Vector2.Scale(p, hi - lo);
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                var uv = Uv(new Vector2(x, y) / (Size - 1));
                float sum = 0;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    sum += height.GetPixelBilinear(uv.x + dx * .00075f, uv.y + dy * .00075f).r;
                raw[y * Size + x] = sum / 9;
            }
            var sorted = raw.OrderBy(v => v).ToArray();
            float low = sorted[sorted.Length / 20], range = Mathf.Max(.05f, sorted[sorted.Length * 9 / 10] - low);
            var field = raw.Select(v => Mathf.Clamp01((v - low) / range)).ToArray();
            int Index(Vector2 p) => Mathf.Clamp(Mathf.RoundToInt(p.y * (Size - 1)), 0, Size - 1) * Size + Mathf.Clamp(Mathf.RoundToInt(p.x * (Size - 1)), 0, Size - 1);
            var labels = SegmentCourses(field, seeds, wall);
            var stoneMask = StoneMask(field, labels, seeds, Index);
            // Remove sub-centimetre tendrils and fill tiny bites before the bevel. They are
            // texture noise, not plausible stone silhouettes. Distances are measured in metres.
            stoneMask = RegularizeMask(stoneMask, panelHeight / Width);
            var distance = BoundaryDistance(stoneMask, panelHeight / Width);
            var outside = BoundaryDistance(stoneMask.Select(v => !v).ToArray(), panelHeight / Width);
            var elevations = new float[seeds.Length];
            for (int i = 0; i < elevations.Length; i++)
                elevations[i] = (wall ? .035f : .021f) + Mathf.Sin(i * 7.31f) * (wall ? .004f : .003f);
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            var weights = new List<Color>();
            var sides = new List<int>();
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int p = y * Size + x, stone = labels[p];
                var point = new Vector2(x, y) / (Size - 1);
                // A narrow bed of mortar, then a short chipped shoulder, then a broad firm face.
                // No radial fans, holes, arbitrary inset contours, or stretched side-wall UVs.
                float d = (distance[p] - outside[p]) * Width / (Size - 1);
                float shoulder = Mathf.SmoothStep(0, 1, Mathf.Clamp01((d + .009f) / (wall ? .018f : .015f)));
                float face = elevations[stone] + (raw[p] - raw[Index(seeds[stone])]) * .008f;
                float mortar = -.008f + raw[p] * .002f;
                float h = Mathf.Lerp(mortar, face, shoulder);
                vertices.Add(new Vector3((point.x - .5f) * Width, (point.y - .5f) * panelHeight, -h));
                uvs.Add(Uv(point));
                weights.Add(new Color(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, .95f, shoulder)), 0, 0, 1));
                if (x < Size - 1 && y < Size - 1)
                {
                    triangles.Add(p); triangles.Add(p + Size); triangles.Add(p + 1);
                    triangles.Add(p + 1); triangles.Add(p + Size); triangles.Add(p + Size + 1);
                }
            }
            // Close only the outer cut of the sample. Inside, every edge is shared by the stone
            // and mortar, so shadows and ray tracing see a continuous solid wall.
            var boundary = new List<int>();
            for (int x = 0; x < Size - 1; x++) boundary.Add(x);
            for (int y = 0; y < Size - 1; y++) boundary.Add(y * Size + Size - 1);
            for (int x = Size - 1; x > 0; x--) boundary.Add((Size - 1) * Size + x);
            for (int y = Size - 1; y > 0; y--) boundary.Add(y * Size);
            int start = vertices.Count;
            foreach (int p in boundary)
            {
                vertices.Add(vertices[p]); uvs.Add(uvs[p]);
                var back = vertices[p]; back.z = .025f;
                vertices.Add(back); uvs.Add(uvs[p] + Vector2.one * .005f);
            }
            int center = vertices.Count; vertices.Add(new Vector3(0, 0, .025f)); uvs.Add(Uv(Vector2.one * .5f));
            for (int i = 0; i < boundary.Count; i++)
            {
                int a = start + i * 2, b = start + ((i + 1) % boundary.Count) * 2;
                sides.Add(a); sides.Add(b); sides.Add(a + 1);
                sides.Add(a + 1); sides.Add(b); sides.Add(b + 1);
                sides.Add(center); sides.Add(a + 1); sides.Add(b + 1);
            }
            var mesh = new Mesh { name = wall ? "Stone wall with recessed mortar" : "Stone paving with recessed mortar", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.subMeshCount = 2;
            while (weights.Count < vertices.Count) weights.Add(Color.black);
            mesh.SetColors(weights);
            mesh.SetTriangles(triangles, 0); mesh.SetTriangles(sides, 1);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            Debug.Log($"[masonry sample] {mesh.name}: {seeds.Length} stones, {vertices.Count} vertices, {(triangles.Count + sides.Count) / 3} triangles, {mesh.bounds.size.z:F3} m depth");
            return mesh;
        }

        // The reference has staggered courses. Trace each bed joint left-to-right and each
        // head joint only within its course. Unconstrained flooding follows texture pits into
        // the stones and produces diagonal notches; radial clipping makes that even worse.
        static int[] SegmentCourses(float[] field, Vector2[] seeds, bool wall)
        {
            var rows = seeds.Select((p, i) => (p, i)).GroupBy(v => v.p.y).OrderBy(g => g.Key)
                .Select(g => g.OrderBy(v => v.p.x).ToArray()).ToArray();
            var filtered = new float[field.Length];
            const int radius = 3;
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                float sum = 0;
                for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                    sum += field[Mathf.Clamp(y + dy, 0, Size - 1) * Size + Mathf.Clamp(x + dx, 0, Size - 1)];
                filtered[y * Size + x] = sum / ((2 * radius + 1) * (2 * radius + 1));
            }
            var beds = new int[rows.Length - 1][];
            float[] wallBeds = { .115f, .30f, .475f, .665f, .87f };
            for (int r = 0; r < beds.Length; r++)
            {
                float guide = wall ? wallBeds[r] : (rows[r][0].p.y + rows[r + 1][0].p.y) * .5f;
                beds[r] = Trace(filtered, true, 0, Size - 1, guide, wall ? .022f : .025f);
            }
            var labels = new int[field.Length];
            for (int r = 0; r < rows.Length; r++)
            {
                int bottom = r == 0 ? 0 : beds[r - 1].Min(), top = r == beds.Length ? Size - 1 : beds[r].Max();
                var heads = new int[rows[r].Length - 1][];
                for (int c = 0; c < heads.Length; c++)
                    heads[c] = Trace(filtered, false, bottom, top, (rows[r][c].p.x + rows[r][c + 1].p.x) * .5f, .045f);
                for (int y = bottom; y <= top; y++) for (int x = 0; x < Size; x++)
                {
                    if (r > 0 && y < beds[r - 1][x] || r < beds.Length && y >= beds[r][x]) continue;
                    int c = 0;
                    while (c < heads.Length && x >= heads[c][y - bottom]) c++;
                    labels[y * Size + x] = rows[r][c].i;
                }
            }
            return labels;
        }

        // Minimum-cost valley path in a narrow authored corridor, with a penalty for slope.
        static int[] Trace(float[] field, bool horizontal, int first, int last, float guide, float corridor)
        {
            int lo = Mathf.Max(0, Mathf.RoundToInt((guide - corridor) * (Size - 1)));
            int hi = Mathf.Min(Size - 1, Mathf.RoundToInt((guide + corridor) * (Size - 1)));
            int width = hi - lo + 1, length = last - first + 1;
            var costs = new float[width]; var previous = new int[length * width];
            for (int along = 0; along < length; along++)
            {
                var next = new float[width];
                for (int j = 0; j < width; j++)
                {
                    float best = float.PositiveInfinity; int from = j;
                    for (int k = Mathf.Max(0, j - 1); k <= Mathf.Min(width - 1, j + 1); k++)
                    {
                        float cost = costs[k] + (j == k ? 0 : .035f);
                        if (cost < best) { best = cost; from = k; }
                    }
                    int across = j + lo, axis = along + first;
                    float v = field[horizontal ? across * Size + axis : axis * Size + across];
                    float offset = (across / (float)(Size - 1) - guide) / corridor;
                    next[j] = best + v + .035f * offset * offset;
                    previous[along * width + j] = from;
                }
                costs = next;
            }
            int end = 0;
            for (int j = 1; j < width; j++) if (costs[j] < costs[end]) end = j;
            var path = new int[length];
            for (int along = length - 1; along >= 0; along--)
            { path[along] = end + lo; end = previous[along * width + end]; }
            return path;
        }

        // A course partition alone also raises the pale mortar between the dark stone outlines.
        // Grow faces from their markers, stopping at the dark edge band. A one-pixel minimum
        // filter bridges small breaks in that band; enclosed texture pits are then filled.
        static bool[] StoneMask(float[] field, int[] labels, Vector2[] seeds, Func<Vector2, int> index)
        {
            var allowed = new bool[field.Length];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                float value = 1;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    value = Mathf.Min(value, field[Mathf.Clamp(y + dy, 0, Size - 1) * Size + Mathf.Clamp(x + dx, 0, Size - 1)]);
                allowed[y * Size + x] = value > .28f;
            }
            var mask = new bool[field.Length]; var queue = new Queue<int>();
            foreach (var seed in seeds) { int p = index(seed); mask[p] = true; queue.Enqueue(p); }
            while (queue.Count > 0)
            {
                int p = queue.Dequeue(), x = p % Size, y = p / Size;
                Visit(p - 1, x > 0); Visit(p + 1, x < Size - 1); Visit(p - Size, y > 0); Visit(p + Size, y < Size - 1);
                void Visit(int q, bool valid)
                {
                    if (!valid || mask[q] || !allowed[q] || labels[p] != labels[q]) return;
                    mask[q] = true; queue.Enqueue(q);
                }
            }
            // Flood the actual exterior. Unreached background is an internal texture blemish,
            // not a second mortar joint through the middle of a stone.
            var exterior = new bool[field.Length];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int p = y * Size + x;
                bool edge = x == 0 || x == Size - 1 || y == 0 || y == Size - 1
                    || labels[p - 1] != labels[p] || labels[p + 1] != labels[p]
                    || labels[p - Size] != labels[p] || labels[p + Size] != labels[p];
                if (edge && !mask[p]) { exterior[p] = true; queue.Enqueue(p); }
                // Keep a mortar line even at an incomplete source edge band.
                if (edge && x > 0 && x < Size - 1 && y > 0 && y < Size - 1)
                { mask[p] = false; if (!exterior[p]) { exterior[p] = true; queue.Enqueue(p); } }
            }
            while (queue.Count > 0)
            {
                int p = queue.Dequeue(), x = p % Size, y = p / Size;
                Visit(p - 1, x > 0); Visit(p + 1, x < Size - 1); Visit(p - Size, y > 0); Visit(p + Size, y < Size - 1);
                void Visit(int q, bool valid)
                {
                    if (!valid || exterior[q] || mask[q]) return;
                    exterior[q] = true; queue.Enqueue(q);
                }
            }
            for (int p = 0; p < mask.Length; p++) mask[p] = !exterior[p];
            return mask;
        }

        static float[] BoundaryDistance(bool[] mask, float verticalStep)
        {
            var d = new float[mask.Length];
            float diagonalStep = Mathf.Sqrt(1 + verticalStep * verticalStep);
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                int p = y * Size + x;
                d[p] = mask[p] ? Size : 0;
            }
            for (int p = 0; p < d.Length; p++)
            {
                int x = p % Size, y = p / Size;
                if (x > 0) d[p] = Mathf.Min(d[p], d[p - 1] + 1);
                if (y > 0) { d[p] = Mathf.Min(d[p], d[p - Size] + verticalStep);
                    if (x > 0) d[p] = Mathf.Min(d[p], d[p - Size - 1] + diagonalStep);
                    if (x < Size - 1) d[p] = Mathf.Min(d[p], d[p - Size + 1] + diagonalStep); }
            }
            for (int p = d.Length - 1; p >= 0; p--)
            {
                int x = p % Size, y = p / Size;
                if (x < Size - 1) d[p] = Mathf.Min(d[p], d[p + 1] + 1);
                if (y < Size - 1) { d[p] = Mathf.Min(d[p], d[p + Size] + verticalStep);
                    if (x > 0) d[p] = Mathf.Min(d[p], d[p + Size - 1] + diagonalStep);
                    if (x < Size - 1) d[p] = Mathf.Min(d[p], d[p + Size + 1] + diagonalStep); }
            }
            // Filter the sub-grid stair steps, never the centimetre-scale stone shape.
            var smooth = (float[])d.Clone();
            for (int y = 1; y < Size - 1; y++) for (int x = 1; x < Size - 1; x++)
            { int p = y * Size + x; smooth[p] = (d[p] * 4 + d[p - 1] + d[p + 1] + d[p - Size] + d[p + Size]) / 8; }
            return smooth;
        }

        static bool[] RegularizeMask(bool[] mask, float verticalStep)
        {
            const float radiusMetres = .008f;
            float radius = radiusMetres * (Size - 1) / Width;
            bool[] Filter(bool[] source, bool dilate)
            {
                var result = new bool[source.Length];
                int rx = Mathf.CeilToInt(radius), ry = Mathf.CeilToInt(radius / verticalStep);
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    bool value = !dilate;
                    for (int dy = -ry; dy <= ry; dy++) for (int dx = -rx; dx <= rx; dx++)
                    {
                        if (dx * dx + dy * dy * verticalStep * verticalStep > radius * radius) continue;
                        bool sample = source[Mathf.Clamp(y + dy, 0, Size - 1) * Size + Mathf.Clamp(x + dx, 0, Size - 1)];
                        value = dilate ? value || sample : value && sample;
                    }
                    result[y * Size + x] = value;
                }
                return result;
            }
            return Filter(Filter(Filter(Filter(mask, false), true), true), false);
        }
    }
}
