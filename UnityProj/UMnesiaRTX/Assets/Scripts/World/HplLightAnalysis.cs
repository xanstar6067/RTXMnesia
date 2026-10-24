using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.World
{
    /// <summary>
    /// Which lights' shadows are seen: the share of each light in the light of the part of the map the camera sees.
    /// A grid of rays from the camera finds the visible surfaces; at each point the illuminance of every light (candela
    /// or nits of the HDRP light, 1/d², the range window, the spot cone, N·L; box lights as their constant light) is
    /// summed over the lights a ray from the point reaches (walls between rooms, not the light's own lamp). A light
    /// counts where it gives at least <see cref="MinShare"/> of the light of the point — below that its shadow drowns in
    /// the other lights; the points are weighted by how bright they are (dark ones are not seen). The importance of a
    /// light is that over the grid: about the part of the screen where its shadow shows. So a candle alone in a dark
    /// room comes first, while the same candle next to a chandelier and a torch needs no shadow of its own; a light in
    /// the next room does not count, a light behind the camera that lights the room in view does. Lit, put out and
    /// flickering lamps change their HDRP lights, which the next estimate reads.
    /// Measured a few times a second (the grid shifts each time, so thin things are found too) and smoothed; the rays
    /// run as one batch on the job threads. HplRayTracing gives its RT shadow slots by it.
    /// The same light at points in the air is Daniel's own light sensor (<see cref="SensorLux"/>, the darkness by the
    /// visible light: Player.PlayerLightLevel).
    /// </summary>
    public static class HplLightAnalysis
    {
        /// <summary>Rays from the camera (columns, rows).</summary>
        const int GridX = 16, GridY = 9;
        /// <summary>Seconds between estimates.</summary>
        const float Interval = 0.2f;
        /// <summary>The weight of a new estimate against the smoothed one.</summary>
        const float Smoothing = 0.5f;
        /// <summary>A light counts at a point where it gives at least this share of its light.</summary>
        public const float MinShare = 0.15f;
        /// <summary>Lights below this importance (share of the screen) have no shadow worth showing.</summary>
        public const float MinImportance = 0.002f;
        /// <summary>A point lit by this much (lux) is half seen: darker ones count less.</summary>
        const float SeenLux = 1f;
        /// <summary>Illuminance (lux) below which a light is not traced to a point at all.</summary>
        const float MinLux = 0.01f;
        /// <summary>The rays to a light start this far above the surface and stop this short of the light (m).</summary>
        const float SurfaceOffset = 0.02f, LightMargin = 0.1f;
        const float MaxDistance = 60f;

        sealed class Source
        {
            public AmnesiaLight light;
            public Light unity;
            public Transform entity;
            public Vector3 position, forward;
            public float intensity, range, cosOuter, cosInner, area;
            public bool rect, cone;
            public float sum;
        }

        struct Pair
        {
            public int point, source;
            public float lux;
        }

        static readonly List<Source> s_sources = new();
        static readonly List<(Bounds bounds, float lux)> s_boxes = new();
        static readonly List<Pair> s_pairs = new();
        static readonly Dictionary<AmnesiaLight, float> s_importance = new();
        static readonly Dictionary<AmnesiaLight, Transform> s_entities = new();
        static readonly List<AmnesiaLight> s_gone = new();
        static Vector3[] s_points = new Vector3[GridX * GridY], s_normals = new Vector3[GridX * GridY];
        static float[] s_total = new float[GridX * GridY];
        static float s_next;
        static int s_pass;

        /// <summary>The last estimate: points on surfaces, rays to the lights, milliseconds of the main thread (probe).</summary>
        public static int Points { get; private set; }
        public static int Rays { get; private set; }
        public static float Milliseconds { get; private set; }

        /// <summary>About the share of the screen where the light's shadow shows (0 — nowhere).</summary>
        public static float Importance(AmnesiaLight light) => light != null && s_importance.TryGetValue(light, out float v) ? v : 0f;

        /// <summary>The lights with their importance, the most important first (probe).</summary>
        public static List<(AmnesiaLight light, float importance)> Ranked()
        {
            var list = new List<(AmnesiaLight, float)>();
            foreach (var (light, v) in s_importance) if (light != null && v > 0f) list.Add((light, v));
            list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return list;
        }

        /// <summary>The next <see cref="Update"/> estimates again (the lights or the settings changed).</summary>
        public static void Invalidate() => s_next = 0f;

        public static void Clear()
        {
            s_importance.Clear();
            s_entities.Clear();
            s_next = 0f;
        }

        /// <summary>Estimates again when it is time (HplRayTracing, before choosing its lights).</summary>
        public static void Update(Camera camera)
        {
            if (camera == null || Time.unscaledTime < s_next) return;
            s_next = Time.unscaledTime + Interval;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            GatherSources(false, default);
            int count = CastGrid(camera);
            var raw = Estimate(count);

            s_gone.Clear();
            foreach (var light in s_importance.Keys) s_gone.Add(light);
            foreach (var light in s_gone)
            {
                float v = Mathf.Lerp(s_importance[light], raw.TryGetValue(light, out float r) ? r : 0f, Smoothing);
                if (light == null || v < MinImportance * 0.1f) s_importance.Remove(light);
                else s_importance[light] = v;
            }
            foreach (var (light, r) in raw)
                if (!s_importance.ContainsKey(light)) s_importance[light] = r * Smoothing;
            Milliseconds = (float)watch.Elapsed.TotalMilliseconds;
            s_pass++;
        }

        /// <summary>
        /// The lights that light the map now. For the <paramref name="sensor"/> only those of <paramref name="scene"/>,
        /// without the lights in the hand (the lit lantern counts on its own) and the player's darkness light.
        /// </summary>
        static void GatherSources(bool sensor, Scene scene)
        {
            s_sources.Clear();
            s_boxes.Clear();
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null || !light.isActiveAndEnabled) continue;
                if (sensor && (light.handHeld || light.excludeFromLightLevel || light.gameObject.scene != scene)) continue;
                if (light.box)
                {
                    if (!light.Visible) continue;
                    // The box pass adds albedo × color × UnitRadiance: the radiance of a lambertian surface under π × that.
                    float lux = Mathf.PI * (light.CurrentColor * light.enhancedIntensity).maxColorComponent * HplLighting.UnitRadiance;
                    if (lux > 0f) s_boxes.Add((new Bounds(light.transform.position, light.boxSize), lux));
                    continue;
                }
                if (!light.TryGetComponent<Light>(out var unity) || !unity.enabled || unity.intensity <= 0f) continue;
                // Copies that light only the hands or the wax.
                if ((HplLighting.GetLightLayers(unity) & 1u) == 0) continue;
                var t = light.transform;
                var s = new Source
                {
                    light = light, unity = unity, position = t.position, forward = t.forward,
                    intensity = unity.intensity * unity.color.maxColorComponent, range = unity.range,
                };
                switch (unity.type)
                {
                    case LightType.Rectangle:
                        s.rect = true;
                        s.area = unity.areaSize.x * unity.areaSize.y;
                        break;
                    case LightType.Spot:
                    case LightType.Pyramid:
                    case LightType.Box:
                        s.cone = true;
                        s.cosOuter = Mathf.Cos(0.5f * unity.spotAngle * Mathf.Deg2Rad);
                        s.cosInner = Mathf.Cos(0.5f * Mathf.Min(unity.innerSpotAngle, unity.spotAngle - 0.1f) * Mathf.Deg2Rad);
                        break;
                    case LightType.Point: break;
                    default: continue;
                }
                if (!s_entities.TryGetValue(light, out s.entity))
                {
                    var entity = light.GetComponentInParent<AmnesiaEntity>(true);
                    s_entities[light] = s.entity = entity != null ? entity.transform : null;
                }
                s_sources.Add(s);
            }
        }

        public static int QueryMask => Physics.DefaultRaycastLayers & ~(1 << 2) & ~(1 << HplPhysicsWorld.NoCollideLayer);

        /// <summary>The visible surfaces: one ray per cell of the grid, shifted inside the cell each estimate.</summary>
        static int CastGrid(Camera camera)
        {
            int n = GridX * GridY;
            var commands = new NativeArray<RaycastCommand>(n, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(n, Allocator.TempJob);
            var query = new QueryParameters(QueryMask, false, QueryTriggerInteraction.Ignore, false);
            // A 4 × 4 pattern of offsets inside the cells.
            int k = s_pass & 15;
            float ox = ((k & 3) + 0.5f) / 4f, oy = ((k >> 2) + 0.5f) / 4f;
            float far = Mathf.Min(camera.farClipPlane, MaxDistance);
            for (int y = 0; y < GridY; y++)
                for (int x = 0; x < GridX; x++)
                {
                    var ray = camera.ViewportPointToRay(new Vector3((x + ox) / GridX, (y + oy) / GridY, 0f));
                    commands[y * GridX + x] = new RaycastCommand(ray.origin, ray.direction, query, far);
                }
            RaycastCommand.ScheduleBatch(commands, hits, 16, 1).Complete();
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                var hit = hits[i];
                if (hit.collider == null) continue;
                s_points[count] = hit.point;
                s_normals[count] = hit.normal;
                count++;
            }
            commands.Dispose();
            hits.Dispose();
            Points = count;
            return count;
        }

        /// <summary>
        /// Illuminance (lux) of a light at a point before shadows; 0 out of its range, cone or facing away. A zero
        /// <paramref name="normal"/>: a surface facing the light (the sensor).
        /// </summary>
        static float Lux(Source s, Vector3 point, Vector3 normal)
        {
            Vector3 toLight = s.position - point;
            float d2 = toLight.sqrMagnitude;
            float r2 = s.range * s.range;
            if (d2 >= r2 || d2 < 1e-6f) return 0f;
            Vector3 l = toLight / Mathf.Sqrt(d2);
            float nl = normal == Vector3.zero ? 1f : Vector3.Dot(normal, l);
            if (nl <= 0f) return 0f;
            // HDRP's range window.
            float f = d2 / r2;
            float window = Mathf.Clamp01(1f - f * f);
            window *= window;
            float emit = 1f;
            if (s.rect)
            {
                // A lambertian rectangle shining along its forward: L·A·cos at the light, d² kept above its size.
                emit = -Vector3.Dot(s.forward, l);
                if (emit <= 0f) return 0f;
                return s.intensity * s.area * emit * nl * window / (d2 + s.area);
            }
            if (s.cone)
            {
                float c = -Vector3.Dot(s.forward, l);
                if (c <= s.cosOuter) return 0f;
                emit = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(s.cosOuter, s.cosInner, c));
            }
            return s.intensity * emit * nl * window / d2;
        }

        /// <summary>
        /// Daniel's own light sensor: at each of the first <paramref name="count"/> points (in the air, around the body)
        /// the illuminance (lux) of a surface facing each light of <paramref name="scene"/>, summed over the lights a ray
        /// from the point reaches (not through walls; the light's own lamp does not stop it), plus the box lights the
        /// point is in. So it is the light that would be seen on Daniel there, of the lights as they shine now (lit, put
        /// out, fading, flickering), without the light that bounces (GI).
        /// </summary>
        public static void SensorLux(Scene scene, Vector3[] points, int count, float[] lux)
        {
            GatherSources(true, scene);
            s_pairs.Clear();
            for (int p = 0; p < count; p++)
            {
                float total = 0f;
                foreach (var (bounds, box) in s_boxes)
                    if (bounds.Contains(points[p])) total += box;
                lux[p] = total;
                for (int i = 0; i < s_sources.Count; i++)
                {
                    float l = Lux(s_sources[i], points[p], Vector3.zero);
                    if (l >= MinLux) s_pairs.Add(new Pair { point = p, source = i, lux = l });
                }
            }
            if (s_pairs.Count == 0) return;
            var commands = new NativeArray<RaycastCommand>(s_pairs.Count, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(s_pairs.Count, Allocator.TempJob);
            var query = new QueryParameters(QueryMask, false, QueryTriggerInteraction.Ignore, false);
            for (int i = 0; i < s_pairs.Count; i++)
            {
                var pair = s_pairs[i];
                Vector3 from = points[pair.point];
                Vector3 to = s_sources[pair.source].position - from;
                float d = to.magnitude;
                commands[i] = new RaycastCommand(from, to / d, query, Mathf.Max(d - LightMargin, 0f));
            }
            RaycastCommand.ScheduleBatch(commands, hits, 32, 1).Complete();
            for (int i = 0; i < s_pairs.Count; i++)
            {
                var pair = s_pairs[i];
                var entity = s_sources[pair.source].entity;
                var blocker = hits[i].collider;
                if (blocker == null || entity != null && blocker.transform.IsChildOf(entity)) lux[pair.point] += pair.lux;
            }
            commands.Dispose();
            hits.Dispose();
        }

        static Dictionary<AmnesiaLight, float> Estimate(int count)
        {
            s_pairs.Clear();
            for (int p = 0; p < count; p++)
            {
                float total = 0f;
                foreach (var (bounds, lux) in s_boxes)
                    if (bounds.Contains(s_points[p])) total += lux;
                s_total[p] = total;
                for (int i = 0; i < s_sources.Count; i++)
                {
                    float lux = Lux(s_sources[i], s_points[p], s_normals[p]);
                    if (lux >= MinLux) s_pairs.Add(new Pair { point = p, source = i, lux = lux });
                }
            }
            Rays = s_pairs.Count;
            var raw = new Dictionary<AmnesiaLight, float>();
            if (s_pairs.Count == 0) return raw;

            // Does the light reach the point: a wall between them, or only the light's own lamp?
            var commands = new NativeArray<RaycastCommand>(s_pairs.Count, Allocator.TempJob);
            var hits = new NativeArray<RaycastHit>(s_pairs.Count, Allocator.TempJob);
            var query = new QueryParameters(QueryMask, false, QueryTriggerInteraction.Ignore, false);
            for (int i = 0; i < s_pairs.Count; i++)
            {
                var pair = s_pairs[i];
                Vector3 from = s_points[pair.point] + s_normals[pair.point] * SurfaceOffset;
                Vector3 to = s_sources[pair.source].position - from;
                float d = to.magnitude;
                commands[i] = new RaycastCommand(from, to / d, query, Mathf.Max(d - LightMargin, 0f));
            }
            RaycastCommand.ScheduleBatch(commands, hits, 32, 1).Complete();
            for (int i = 0; i < s_pairs.Count; i++)
            {
                var hit = hits[i];
                var pair = s_pairs[i];
                var entity = s_sources[pair.source].entity;
                var blocker = hit.collider;
                bool seen = blocker == null || entity != null && blocker.transform.IsChildOf(entity);
                if (!seen) { pair.lux = 0f; s_pairs[i] = pair; }
                else s_total[pair.point] += pair.lux;
            }
            commands.Dispose();
            hits.Dispose();

            foreach (var s in s_sources) s.sum = 0f;
            foreach (var pair in s_pairs)
            {
                if (pair.lux <= 0f) continue;
                float total = s_total[pair.point];
                float share = pair.lux / total;
                if (share < MinShare) continue;
                s_sources[pair.source].sum += share * total / (total + SeenLux);
            }
            float cells = GridX * GridY;
            foreach (var s in s_sources)
                if (s.sum > 0f) raw[s.light] = raw.TryGetValue(s.light, out float v) ? v + s.sum / cells : s.sum / cells;
            return raw;
        }
    }
}
