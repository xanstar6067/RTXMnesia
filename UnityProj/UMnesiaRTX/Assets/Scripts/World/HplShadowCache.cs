using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.World
{
    /// <summary>
    /// Cached shadow maps of the enhanced lighting (graphics tab "Shadow cache", shadows on): a light that stands still
    /// renders its shadow map once into HDRP's cached atlas (ShadowUpdateMode.OnDemand) instead of every frame, and again
    /// only when something that casts shadows moves, appears or disappears within its range, or the light itself moves
    /// or changes its range. What can move are the entities of the map (props, doors, enemies, critters, NPCs; static
    /// objects never move) and the copies of dissolving entities. The lantern in the hand and area lights keep their
    /// shadow maps every frame.
    /// The atlas (8192², ConfigureHDRP) holds about 150 lamps at 256 or 40 at 512 (a point light is six maps), fewer than a map has;
    /// the other lights render every frame, which costs nothing while they are out of view. So the places go to the
    /// lights nearest to the camera, chosen again twice a second (the cached ones a little longer, so that two lights
    /// at about the same distance do not swap); a light HDRP still cannot place renders every frame.
    /// </summary>
    public static class HplShadowCache
    {
        /// <summary>Frames a cached light may wait for its place in the atlas before it falls back to every frame.</summary>
        const int PlacementFrames = 3;
        /// <summary>
        /// A caster moved when its bounds moved or grew by more than this (m) since the shadows last saw it: resting
        /// bodies of the physics and joints at their limits write their transforms with sub-millimetre jitter.
        /// </summary>
        const float MoveThreshold = 0.002f;
        /// <summary>Seconds between choices of the cached lights; the share of the atlas they may fill (packing).</summary>
        const float ChooseInterval = 0.5f, AtlasShare = 0.9f;
        /// <summary>A cached light keeps its place against a new one up to this much farther (m).</summary>
        const float KeepMargin = 2f;

        sealed class CachedLight
        {
            public AmnesiaLight light;
            public Light unityLight;
            public HDAdditionalLightData hd;
            public float range;
            public int waited;
            public bool cached, placed, noRoom;
            public float score;
            public long cost;
        }

        sealed class Caster
        {
            public Bounds bounds;
            public bool shown;
            public Transform[] bones;
        }

        static readonly List<CachedLight> s_lights = new();
        static readonly Dictionary<Renderer, Caster> s_casters = new();
        static readonly List<Renderer> s_gone = new();
        static readonly List<Bounds> s_changed = new();
        static readonly List<CachedLight> s_order = new();
        static int s_frame = -1;
        static float s_nextChoice;

        /// <summary>Chosen in the graphics tab (applied whether or not the enhanced lighting is on).</summary>
        public static bool Chosen { get; private set; } = true;
        /// <summary>In effect: enhanced lighting, shadows on, the cache chosen.</summary>
        public static bool On => Chosen && HplGraphics.Lighting == 1 && HplGraphics.Shadows != HplGraphics.ShadowsOff;
        /// <summary>Lights with a cached shadow map, shadow maps redrawn in the last frame, lights the atlas had no room for (probe).</summary>
        public static int CachedLights { get; private set; }
        public static int Redraws { get; private set; }
        public static int Fallbacks { get; private set; }

        /// <summary>Probe: how often each caster triggered redraws, the lights the atlas had no room for.</summary>
        public static Dictionary<string, int> MoverCounts { get; set; }
        public static readonly List<string> FallbackNames = new();

        public static void Apply(HplUserSettings s) => Chosen = s.shadowCache;

        /// <summary>
        /// A light of the enhanced lighting with a shadow map (HplEnhancedLighting.ConfigureShadowMaps, after its record,
        /// which brings back the update mode): it may get a place in the cached atlas.
        /// </summary>
        public static void Add(AmnesiaLight light, Light unityLight, HDAdditionalLightData hd)
        {
            if (!On || hd == null || light.handHeld || unityLight.shadows == LightShadows.None) return;
            if (unityLight.type is not (LightType.Point or LightType.Spot or LightType.Pyramid or LightType.Box)) return;
            // Lights HplRayTracing renders on demand already (traced without a map shadow of their own).
            if (hd.shadowUpdateMode != ShadowUpdateMode.EveryFrame) return;
            light.transform.hasChanged = false;
            s_lights.Add(new CachedLight { light = light, unityLight = unityLight, hd = hd, range = unityLight.range });
            s_nextChoice = 0f;
        }

        /// <summary>The shadow casters of the map that can move (its entities), once the lights are cached.</summary>
        public static void TrackMap(Component map)
        {
            if (s_lights.Count == 0 || map == null) return;
            foreach (var entity in map.GetComponentsInChildren<AmnesiaEntity>(true)) Track(entity.gameObject);
        }

        /// <summary>Shadow casters that can move: an entity created by a script, the copy of a dissolving one.</summary>
        public static void Track(GameObject root)
        {
            if (s_lights.Count == 0 || root == null) return;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.shadowCastingMode == ShadowCastingMode.Off || r.isPartOfStaticBatch || s_casters.ContainsKey(r)) continue;
                if (r is not (MeshRenderer or SkinnedMeshRenderer)) continue;
                s_casters[r] = new Caster
                {
                    bounds = r.bounds, shown = Shown(r),
                    bones = r is SkinnedMeshRenderer skinned ? skinned.bones : null,
                };
            }
        }

        /// <summary>
        /// Every cached shadow map again: the static geometry changed (the texture set swaps the materials, the shared
        /// walls of the enhanced relief move apart: HplEnhancedRelief.SeparateSharedWalls).
        /// </summary>
        public static void RedrawAll()
        {
            foreach (var e in s_lights)
                if (e.cached && e.hd != null) e.hd.RequestShadowMapRendering();
        }

        /// <summary>The lights' update modes are brought back by the enhanced lighting's revert.</summary>
        public static void Clear()
        {
            s_lights.Clear();
            s_casters.Clear();
            Redraws = Fallbacks = CachedLights = 0;
            FallbackNames.Clear();
        }

        static bool Shown(Renderer r) => r.enabled && r.gameObject.activeInHierarchy;

        /// <summary>
        /// Once a frame, before the cameras render (HplGraphics, <paramref name="eye"/> — the game camera): which lights
        /// are cached, which cached shadow maps are redrawn.
        /// </summary>
        public static void Update(Vector3 eye)
        {
            if (s_frame == Time.frameCount) return;
            s_frame = Time.frameCount;
            if (s_lights.Count == 0) return;
            if (Time.unscaledTime >= s_nextChoice)
            {
                s_nextChoice = Time.unscaledTime + ChooseInterval;
                Choose(eye);
            }
            Redraws = 0;
            s_changed.Clear();
            s_gone.Clear();
            foreach (var (r, c) in s_casters)
            {
                if (r == null)
                {
                    if (c.shown) s_changed.Add(c.bounds);
                    s_gone.Add(r);
                    continue;
                }
                bool shown = Shown(r);
                bool moved = false;
                if (r.transform.hasChanged)
                {
                    r.transform.hasChanged = false;
                    var b = r.bounds;
                    moved = (b.center - c.bounds.center).sqrMagnitude > MoveThreshold * MoveThreshold ||
                            (b.extents - c.bounds.extents).sqrMagnitude > MoveThreshold * MoveThreshold;
                }
                if (c.bones != null)
                    foreach (var bone in c.bones)
                        if (bone != null && bone.hasChanged) { moved = true; bone.hasChanged = false; }
                if (shown == c.shown && (!shown || !moved)) continue;
                // Where it was and where it is now: both shadows change.
                if (c.shown) s_changed.Add(c.bounds);
                if (shown) { c.bounds = r.bounds; s_changed.Add(c.bounds); }
                c.shown = shown;
                if (MoverCounts != null)
                {
                    string key = (r.transform.parent != null ? r.transform.parent.name + "/" : "") + r.name;
                    MoverCounts[key] = MoverCounts.TryGetValue(key, out int n) ? n + 1 : 1;
                }
            }
            foreach (var r in s_gone) s_casters.Remove(r);

            for (int i = s_lights.Count - 1; i >= 0; i--)
            {
                var e = s_lights[i];
                if (e.light == null || e.hd == null) { s_lights.RemoveAt(i); continue; }
                var t = e.light.transform;
                bool redraw = t.hasChanged || e.unityLight.range != e.range;
                t.hasChanged = false;
                e.range = e.unityLight.range;
                if (!e.cached || !e.placed && !Place(e)) continue;
                if (!redraw)
                {
                    Vector3 position = t.position;
                    float range2 = e.range * e.range;
                    foreach (var b in s_changed)
                        if (b.SqrDistance(position) <= range2) { redraw = true; break; }
                }
                if (!redraw) continue;
                e.hd.RequestShadowMapRendering();
                Redraws++;
            }
        }

        /// <summary>
        /// The places of the atlas to the lights nearest to <paramref name="eye"/> (distance to their range), as many as
        /// fit; the lights that lose theirs go back to every frame first, freeing the room.
        /// </summary>
        static void Choose(Vector3 eye)
        {
            if (!(GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset asset)) return;
            var init = asset.currentPlatformRenderPipelineSettings.hdShadowInitParams;
            long budget = (long)(AtlasShare * init.cachedPunctualLightShadowAtlas * (long)init.cachedPunctualLightShadowAtlas);
            s_order.Clear();
            foreach (var e in s_lights)
            {
                if (e.light == null || e.hd == null || e.noRoom) continue;
                if (!e.hd.isActiveAndEnabled || !e.unityLight.enabled || e.unityLight.shadows == LightShadows.None) continue;
                var res = e.hd.shadowResolution;
                long size = Mathf.Min(res.Value(init.shadowResolutionPunctual), init.maxPunctualShadowMapResolution);
                e.cost = (e.unityLight.type == LightType.Point ? 6 : 1) * size * size;
                e.score = Vector3.Distance(eye, e.light.transform.position) - e.unityLight.range - (e.cached ? KeepMargin : 0f);
                s_order.Add(e);
            }
            s_order.Sort((a, b) => a.score.CompareTo(b.score));
            var chosen = new HashSet<CachedLight>();
            long used = 0;
            foreach (var e in s_order)
                if (used + e.cost <= budget) { used += e.cost; chosen.Add(e); }
            foreach (var e in s_lights)
                if (e.cached && !chosen.Contains(e)) SetCached(e, false);
            int count = 0;
            foreach (var e in s_lights)
            {
                if (chosen.Contains(e) && !e.cached) SetCached(e, true);
                if (e.cached) count++;
            }
            CachedLights = count;
        }

        static void SetCached(CachedLight e, bool on)
        {
            e.cached = on;
            e.placed = false;
            e.waited = 0;
            if (on) e.hd.onDemandShadowRenderOnPlacement = true;
            e.hd.shadowUpdateMode = on ? ShadowUpdateMode.OnDemand : ShadowUpdateMode.EveryFrame;
        }

        /// <summary>
        /// Whether the light has its place in the cached atlas; after a few frames without one it renders every frame
        /// again. A disabled light is not placed by HDRP, so it waits.
        /// </summary>
        static bool Place(CachedLight e)
        {
            if (!e.hd.isActiveAndEnabled || !e.unityLight.enabled) return true;
            if (HDCachedShadowManager.instance.LightHasBeenPlacedInAtlas(e.hd)) { e.placed = true; return true; }
            if (++e.waited <= PlacementFrames) return true;
            SetCached(e, false);
            e.noRoom = true;
            CachedLights--;
            Fallbacks++;
            var res = e.hd.shadowResolution;
            FallbackNames.Add($"{e.light.name} {e.unityLight.type} level {res.level} override {res.useOverride}/{res.@override}");
            return false;
        }
    }
}
