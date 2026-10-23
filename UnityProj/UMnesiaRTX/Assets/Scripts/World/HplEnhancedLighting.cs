using System;
using System.Collections.Generic;
using System.Globalization;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.World
{
    /// <summary>
    /// The enhanced lighting (graphics tab "Lighting: Enhanced"): a hand-made overlay per map that sets up the light
    /// again where HPL2's lights do not work with physically based lighting and ray tracing — fill lights without a
    /// source (removed or dimmed; ray-traced GI brings the bounce), window light from the window opening (area lights),
    /// lamps with the size of their flame. The classic lights stay in the map: the overlay only changes how they are
    /// drawn and is taken off again without reloading (classic ↔ enhanced at any time). The light level of the game
    /// logic (darkness, sanity, enemies) stays the classic one.
    ///
    /// The overlay is Resources/HplEnhancedLighting/&lt;map file without extension&gt;.json (our own data, in git):
    /// "lights" change lights of the map by name, "add" creates new ones. Positions are HPL2 map coordinates
    /// ("x y z", as in the .map), lights look at "target". Values are strings, empty = unchanged. "when" limits an
    /// entry to "rt" / "raster" (ray tracing on or off) or "gi" / "nogi" (ray-traced GI on or off).
    /// </summary>
    public static class HplEnhancedLighting
    {
        public const string ResourceFolder = "HplEnhancedLighting/";
        /// <summary>Resources path of the diffusion profile of lit candle wax (created by the editor, EnsureCandleWaxProfile).</summary>
        public const string CandleWaxProfile = ResourceFolder + "CandleWax";
        /// <summary>Thickness of candle wax for the thin-object transmission (0..1 of the profile's 0-5 mm).</summary>
        public const float WaxThickness = 0.35f;
        /// <summary>
        /// Brightness of the light inside a lit candle (the glow of its wax), as a share of the candle's flame. HDRP's
        /// ray-traced shadows count a surface facing away from the light as shadowed, so with them the flame itself
        /// lets no light through the wax; the glow light has no shadows and lights only the wax.
        /// </summary>
        public const float WaxGlowShare = 0.05f;
        /// <summary>How deep under the wick the glow light sits (inside the wax).</summary>
        public const float WaxGlowDepth = 0.015f;
        /// <summary>
        /// Resources folder of the generated wax materials (Assets/AmnesiaImported/Resources, made by the editor:
        /// EnsureCandleWaxProfile). HDRP/Lit compiles transmission as shader_feature: a player has the variant only when
        /// a material in the build uses it, so a wax material made at runtime drew without its glow in a build.
        /// </summary>
        public const string WaxMaterialFolder = "HplCandleWax/";
        /// <summary>The generated wax material of a lamp's classic material (enhanced: made from its enhanced texture set).</summary>
        public static string WaxMaterialName(Material classic, bool enhanced) => classic.name + (enhanced ? " enhanced" : "");
        /// <summary>
        /// One candle flame, cd: the classic candlestick at its map color (Candela(1.7) × 0.85 → 15), so a room lit by
        /// candles keeps its brightness near them; the other fires are counted in candles from it.
        /// </summary>
        public const float CandleCandela = 15f;
        public const float LanternCandles = 10f;
        /// <summary>Illuminance (lux) where a light ends: an albedo-0.5 wall at EV100 3 is ~0.4% of white there.</summary>
        public const float VisibleLux = 0.25f;
        public const float MaxRange = 25f;
        /// <summary>Shadow map bias of the lamps (HDRP defaults 0.75 / 0.5); 2 still let seams through, 4 none.</summary>
        public const float ShadowNormalBias = 4f, ShadowSlopeBias = 0f, ShadowNearPlane = 0.02f;

        [Serializable]
        sealed class Overlay
        {
            public Entry[] lights;
            public Entry[] add;
        }

        [Serializable]
        sealed class Entry
        {
            /// <summary>The light of the map (lights) or the new light's name (add).</summary>
            public string name;
            /// <summary>"" — always, "rt" / "raster", "gi" / "nogi".</summary>
            public string when;
            /// <summary>Added lights: "point", "spot" or "rect".</summary>
            public string type;
            public string position, target;
            /// <summary>Rect: width height (m). Spot: outer angle (degrees) in <see cref="angle"/>.</summary>
            public string size, angle;
            /// <summary>Added lights: sRGB color.</summary>
            public string color;
            /// <summary>Lights of the map: multiplier. Added lights: candela (point, spot) or nits (rect).</summary>
            public string intensity;
            public string range;
            /// <summary>"on", "off" (a fill light: no shadows, also no ray-traced ones) or "" (unchanged / off for added lights).</summary>
            public string shadows;
            /// <summary>The source radius of soft (ray-traced) shadows, m.</summary>
            public string sourceRadius;
            /// <summary>Added lights: a light of the map whose brightness and visibility the light follows (a lamp that is lit / put out, flicker).</summary>
            public string follow;
            /// <summary>Why: not read, for the reader of the file.</summary>
            public string note;
        }

        sealed class Changed
        {
            public AmnesiaLight light;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public bool moved, castShadows;
            public int baseShadowLevel;
            public LightShadows shadows;
            public ShadowUpdateMode updateMode;
            public float normalBias, slopeBias, nearPlane;
        }

        static HplMap s_map;
        static Overlay s_overlay;
        static readonly List<Changed> s_changed = new();
        static readonly List<(Renderer renderer, ShadowCastingMode mode)> s_casters = new();
        static GameObject s_addedRoot;
        static readonly List<(MeshFilter filter, Mesh original, Mesh split, GameObject wax, Mesh waxMesh, Material ownMaterial)> s_wax = new();
        static readonly HashSet<Renderer> s_waxRenderers = new();
        static readonly HashSet<string> s_missingWax = new();
        static readonly List<(Light flame, Light glow, float share)> s_glows = new();
        static bool s_glowsHooked;
        static DiffusionProfileSettings s_waxProfile;
        static bool s_waxProfileLoaded;

        static DiffusionProfileSettings WaxProfile
        {
            get
            {
                if (!s_waxProfileLoaded)
                {
                    s_waxProfileLoaded = true;
                    s_waxProfile = Resources.Load<DiffusionProfileSettings>(CandleWaxProfile);
                    if (s_waxProfile == null) HplLog.PortError("Enhanced lighting: no diffusion profile Resources/" + CandleWaxProfile);
                }
                return s_waxProfile;
            }
        }

        /// <summary>The profile's hash (saved in the asset by the editor; the field is internal to HDRP).</summary>
        static uint WaxProfileHash
        {
            get
            {
                const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
                var profile = typeof(DiffusionProfileSettings).GetField("profile", Flags)?.GetValue(WaxProfile);
                return profile?.GetType().GetField("hash", Flags)?.GetValue(profile) is uint hash ? hash : 0u;
            }
        }

        /// <summary>The global settings volume (HplGraphics): HDRP knows a diffusion profile only from a volume's list.</summary>
        public static void ApplyVolume(VolumeProfile profile)
        {
            if (WaxProfile == null || profile.TryGet(out DiffusionProfileList _)) return;
            profile.Add<DiffusionProfileList>(true).diffusionProfiles.Override(new[] { WaxProfile });
        }

        /// <summary>The enhanced lighting is chosen (graphics tab).</summary>
        public static bool Enabled => HplGraphics.Lighting == 1;
        /// <summary>The enhanced lighting is applied to the current map (its rules, and its overlay if it has one).</summary>
        public static bool Applied { get; private set; }
        /// <summary>Lights of the map changed and lights added (checks).</summary>
        public static int ChangedLights => s_changed.Count;
        public static int AddedLights => s_addedRoot != null ? s_addedRoot.transform.childCount : 0;
        /// <summary>Candle meshes whose wax was given the translucent material (checks).</summary>
        public static int WaxCandles => s_wax.Count;
        /// <summary>Wax drawn with a material made at runtime, without a generated one (no glow in a build; checks).</summary>
        public static int WaxWithoutGenerated => s_wax.FindAll(w => w.ownMaterial != null).Count;

        /// <summary>The map is built (HplMap.Build, Play only): its overlay is read and applied if the enhanced lighting is chosen.</summary>
        public static void Attach(HplMap map)
        {
            if (s_map != null && s_map != map) Revert();
            s_map = map;
            s_overlay = Load(map.mapFile);
            Refresh();
        }

        /// <summary>
        /// The overlay of a map file; a map without one gets an empty overlay (the rules for every map still apply),
        /// null only without a map file.
        /// </summary>
        static Overlay Load(string mapFile)
        {
            if (string.IsNullOrEmpty(mapFile)) return null;
            string path = ResourceFolder + System.IO.Path.ChangeExtension(mapFile.Replace('\\', '/'), null);
            var text = Resources.Load<TextAsset>(path);
            if (text == null) return new Overlay();
            try
            {
                return JsonUtility.FromJson<Overlay>(text.text);
            }
            catch (ArgumentException e)
            {
                HplLog.PortError($"Enhanced lighting {path}: {e.Message}");
                return null;
            }
        }

        /// <summary>The overlay as the settings want it (graphics settings applied, map built).</summary>
        public static void Refresh()
        {
            if (!Application.isPlaying) return;
            Revert();
            if (s_map == null || s_overlay == null || !Enabled) return;
            Apply();
        }

        static bool When(string when) => (when ?? "").Trim().ToLowerInvariant() switch
        {
            "" => true,
            "rt" => HplRayTracing.Active,
            "raster" => !HplRayTracing.Active,
            "gi" => HplRayTracing.Gi > 0,
            "nogi" => HplRayTracing.Gi == 0,
            var other => Warn($"unknown \"when\": {other}"),
        };

        static bool Warn(string message)
        {
            HplLog.Warn("[Enhanced lighting] " + message);
            return false;
        }

        static void Apply()
        {
            Applied = true;
            ApplyRules();
            if (HplRayTracing.Active && HplRayTracing.Gi > 0) HplEnhancedEmission.Apply(s_map);
            if (s_overlay.lights != null)
                foreach (var e in s_overlay.lights)
                {
                    if (!When(e.when)) continue;
                    var light = s_map.GetLight(e.name);
                    if (light == null) { Warn($"no light '{e.name}' in the map"); continue; }
                    Change(light, e);
                }
            if (s_overlay.add != null)
                foreach (var e in s_overlay.add)
                    if (When(e.when)) Add(e);
            HplRayTracing.Reselect();
        }

        /// <summary>
        /// Rules for every map, before its overlay (which can change their result per light):
        /// lamps — HPL2 puts the light of most lamps away from the flame (a candlestick 0.4 m to the side, a wall torch
        /// 1.6 m out into the room, so that the wall is not blown out and the room is lit); with physical falloff and
        /// shadows that light comes from empty air. The light goes to the flame (the middle of the flames for a
        /// chandelier), its reach grows by the distance moved, the flame's size makes the shadow soft, and lamps
        /// always cast shadows (low resolution shadow maps; ray traced when there is a slot).
        /// Fill lights — lights of the map itself (not of an entity) without CastShadows are fill light without a
        /// source: no shadows, also not by "shadows from all lights", so the ray-traced slots go to real sources.
        /// </summary>
        static void ApplyRules()
        {
            // HPL2's level geometry is one-sided: a single plane or relief seen by a lamp from behind cast no shadow
            // map. Both sides cast (the thin walls themselves are handled by the lamps' bias, see ShadowNormalBias).
            foreach (var r in s_map.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.shadowCastingMode != ShadowCastingMode.On) continue;
                s_casters.Add((r, r.shadowCastingMode));
                r.shadowCastingMode = ShadowCastingMode.TwoSided;
            }
            foreach (var entity in s_map.GetComponentsInChildren<AmnesiaEntity>(true))
            {
                if (entity.entityType != "Lamp") continue;
                var flames = new List<(Vector3 position, float radius, float lift)>();
                float candles = 0f;
                foreach (var ps in entity.GetComponentsInChildren<AmnesiaParticleSystem>(true))
                    if (Flame(ps.file, out float r, out float c, out float h)) { flames.Add((ps.transform.position, r, h)); candles += c; }
                if (flames.Count == 0) continue;
                // The lamp's own model does not shadow its flame: a point light just above a torch's cup threw the
                // cup's shadow over the whole wall and floor below (the real flame is larger than the cup's rim).
                foreach (var r in entity.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (r.shadowCastingMode == ShadowCastingMode.Off) continue;
                    if (!s_casters.Exists(c => c.renderer == r)) s_casters.Add((r, r.shadowCastingMode));
                    r.shadowCastingMode = ShadowCastingMode.Off;
                }
                Vector3 center = Vector3.zero;
                float flameRadius = 0f, lift = 0f;
                foreach (var f in flames) { center += f.position; flameRadius = Mathf.Max(flameRadius, f.radius); lift = Mathf.Max(lift, f.lift); }
                center /= flames.Count;
                float spread = 0f;
                foreach (var f in flames) spread = Mathf.Max(spread, Vector3.Distance(f.position, center));
                // The light in the body of the flame: the particle system starts at its base, inside a torch's cup.
                // There the cup hid the light from ray-traced shadows, and its rim was nearer than the shadow map's
                // near plane: the prongs threw huge shadows and the light went through the wall behind.
                Vector3 target = center + Vector3.up * lift;
                var lights = new List<AmnesiaLight>();
                foreach (var light in entity.GetComponentsInChildren<AmnesiaLight>(true))
                    if (!light.box && !light.handHeld && s_map.GetLight(light.name) == light) lights.Add(light);
                var wicks = new List<Vector3>();
                foreach (var f in flames)
                    if (f.radius <= 0.02f) wicks.Add(f.position);
                if (wicks.Count > 0 && lights.Count > 0 && lights[0].TryGetComponent(out Light flame))
                    foreach (var r in entity.GetComponentsInChildren<MeshRenderer>(true))
                        SplitWax(r, wicks, flame, WaxGlowShare * lights.Count / flames.Count, ClassicMaterial(r));
                foreach (var light in lights)
                {
                    var record = Record(light);
                    if (Vector3.Distance(light.transform.position, target) > 0.05f) Move(light, record, target, null);
                    // The flames share the lamp's lights.
                    SetPhysical(light, candles * CandleCandela / lights.Count);
                    light.tracedSourceRadius = Mathf.Min(Mathf.Max(flameRadius, spread), 0.5f);
                    SetShadows(light, true, lowResolution: true);
                    light.Apply(light.CurrentColor, light.CurrentRadius);
                }
            }
            foreach (var light in s_map.GetComponentsInChildren<AmnesiaLight>(true))
            {
                if (light.box || light.castShadows || light.handHeld || s_map.GetLight(light.name) != light) continue;
                if (light.GetComponentInParent<AmnesiaEntity>(true) != null) continue;
                Record(light);
                light.noTracedShadows = true;
            }
            foreach (var light in AmnesiaLight.All)
                if (light != null && light.handHeld) HandLight(light);
        }

        /// <summary>
        /// Lit wax glows: the flame's light goes through the thin walls of the candle, warm and strongest near the flame
        /// (falling off with 1/d^2 down the candle), and goes out with the lamp. HPL2's candle is one mesh with its
        /// holder, so the wax is found by its shape: from each wick down, a narrow column of vertices until the mesh
        /// widens into the holder's pan. Its triangles become a child renderer with a translucent copy of the material on
        /// the wax rendering layer (HplLighting.WaxRenderingLayer), lit from inside by a light without shadows that follows
        /// the lamp's light (<paramref name="share"/> of its intensity per wick).
        /// </summary>
        static void SplitWax(MeshRenderer renderer, List<Vector3> wicks, Light flame, float share, Material classic)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            var material = renderer.sharedMaterial;
            if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1 || renderer.sharedMaterials.Length != 1 ||
                material == null || classic == null || material.shader.name != "HDRP/Lit" || WaxProfile == null || WaxProfileHash == 0) return;
            var vertices = mesh.vertices;
            var world = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) world[i] = renderer.transform.TransformPoint(vertices[i]);
            var wax = new bool[vertices.Length];
            var glows = new List<Vector3>();
            foreach (var wick in wicks)
                if (MarkWax(world, wick, wax, out float bottom)) glows.Add(wick + Vector3.down * Mathf.Min(WaxGlowDepth, bottom * 0.5f));
            var triangles = mesh.triangles;
            var rest = new List<int>(triangles.Length);
            var waxTriangles = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool w = wax[triangles[i]] && wax[triangles[i + 1]] && wax[triangles[i + 2]];
                var list = w ? waxTriangles : rest;
                list.Add(triangles[i]); list.Add(triangles[i + 1]); list.Add(triangles[i + 2]);
            }
            if (waxTriangles.Count == 0 || rest.Count == 0) return;
            var split = UnityEngine.Object.Instantiate(mesh);
            split.name = mesh.name + " (no wax)";
            split.hideFlags = HideFlags.DontSave;
            split.SetTriangles(rest, 0);
            var waxMesh = UnityEngine.Object.Instantiate(mesh);
            waxMesh.name = mesh.name + " (wax)";
            waxMesh.hideFlags = HideFlags.DontSave;
            waxMesh.SetTriangles(waxTriangles, 0);
            // The generated material of the current texture set (an enhanced one may be missing: the classic wax then).
            bool enhanced = HplGraphics.Textures == 1;
            var waxMaterial = Resources.Load<Material>(WaxMaterialFolder + WaxMaterialName(classic, enhanced));
            if (waxMaterial == null && enhanced) waxMaterial = Resources.Load<Material>(WaxMaterialFolder + WaxMaterialName(classic, false));
            Material ownMaterial = null;
            if (waxMaterial == null)
            {
                // Only the editor has every shader variant: a build would draw this copy without transmission.
                if (!Application.isEditor && s_missingWax.Add(classic.name))
                    HplLog.PortError("Enhanced lighting: no generated wax material Resources/" + WaxMaterialFolder + classic.name);
                waxMaterial = ownMaterial = MakeWaxMaterial(material);
                ownMaterial.hideFlags = HideFlags.DontSave;
            }
            var go = new GameObject("Wax") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(renderer.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = waxMesh;
            var waxRenderer = go.AddComponent<MeshRenderer>();
            waxRenderer.sharedMaterial = waxMaterial;
            waxRenderer.renderingLayerMask = renderer.renderingLayerMask | HplLighting.WaxLayerMask;
            waxRenderer.shadowCastingMode = ShadowCastingMode.Off;
            waxRenderer.rayTracingMode = renderer.rayTracingMode;
            foreach (var position in glows)
            {
                var light = new GameObject("Wax glow").AddComponent<Light>();
                light.transform.SetParent(go.transform, false);
                light.transform.position = position;
                light.type = LightType.Point;
                light.shadows = LightShadows.None;
                light.lightUnit = flame.lightUnit;
                light.range = 0.3f;
                light.gameObject.AddComponent<HDAdditionalLightData>().affectSpecular = false;
                HplLighting.SetLightLayers(light, HplLighting.WaxLayerMask);
                s_glows.Add((flame, light, share));
            }
            s_wax.Add((filter, mesh, split, go, waxMesh, ownMaterial));
            s_waxRenderers.Add(waxRenderer);
            filter.sharedMesh = split;
            if (!s_glowsHooked)
            {
                s_glowsHooked = true;
                RenderPipelineManager.beginContextRendering += SyncGlows;
            }
            SyncGlows(default, null);
        }

        /// <summary>
        /// The wax material of a lamp's material: translucent (thin object, the wax profile), without HPL2's illumination.
        /// The source's emission is that of the asset, which HplEmission switches off only on the lamp's own renderer,
        /// and ValidateMaterial rebuilds _EmissiveColor from _EmissiveColorLDR × _EmissiveIntensity (an unlit
        /// chandelier's wax glowed at full). The shader finds the profile by its hash: HDMaterial.SetDiffusionProfile also
        /// records the asset in the material's external references, which fails on a runtime copy in the editor.
        /// </summary>
        public static Material MakeWaxMaterial(Material source)
        {
            var wax = new Material(source) { name = source.name + " (wax)" };
            wax.SetFloat("_MaterialID", (float)MaterialId.LitTranslucent);
            wax.SetFloat("_TransmissionEnable", 1f);
            wax.SetFloat("_Thickness", WaxThickness);
            wax.SetTexture("_ThicknessMap", null);
            wax.SetTexture("_EmissiveColorMap", null);
            wax.SetColor("_EmissiveColorLDR", Color.black);
            wax.SetColor("_EmissiveColor", Color.black);
            wax.SetFloat("_DiffusionProfileHash", System.BitConverter.Int32BitsToSingle((int)WaxProfileHash));
            HDMaterial.ValidateMaterial(wax);
            wax.SetColor("_EmissiveColor", Color.black);
            return wax;
        }

        /// <summary>The classic material of a lamp's renderer (behind HplEmission's copies and the enhanced texture set).</summary>
        static Material ClassicMaterial(Renderer renderer) =>
            renderer.TryGetComponent(out HplEmission emission) && emission.ClassicSource(0) != null
                ? emission.ClassicSource(0)
                : HplEnhancedTextures.ClassicMaterial(renderer.sharedMaterial);

        /// <summary>A renderer of candle wax: the texture sets leave it alone (its material is the wax one of the set).</summary>
        public static bool IsWax(Renderer renderer) => s_waxRenderers.Contains(renderer);

        /// <summary>The wax glows with its flame: lit, dimmed, flickering or out (before each frame).</summary>
        static void SyncGlows(ScriptableRenderContext context, List<Camera> cameras)
        {
            foreach (var (flame, glow, share) in s_glows)
            {
                if (glow == null) continue;
                bool on = flame != null && flame.isActiveAndEnabled && flame.intensity > 0f;
                glow.enabled = on;
                if (!on) continue;
                glow.color = flame.color;
                glow.intensity = flame.intensity * share;
            }
        }

        /// <summary>Marks the wax below a wick: 5 mm bins of the widest vertex around the axis, down to the holder's pan.</summary>
        static bool MarkWax(Vector3[] world, Vector3 wick, bool[] wax, out float bottom)
        {
            bottom = 0f;
            const float Bin = 0.005f, Depth = 0.4f, Above = 0.02f, Search = 0.08f, Widening = 1.4f;
            var widest = new float[(int)(Depth / Bin) + 1];
            for (int i = 0; i < widest.Length; i++) widest[i] = -1f;
            foreach (var p in world)
            {
                float h = wick.y - p.y, r = new Vector2(p.x - wick.x, p.z - wick.z).magnitude;
                if (h < -Above || h > Depth || r > Search) continue;
                int k = (int)(Mathf.Max(h, 0f) / Bin);
                widest[k] = Mathf.Max(widest[k], r);
            }
            // The candle's radius: the widest of the first centimetres below the wick (the wick itself is thinner).
            float candle = 0f;
            for (int k = 1; k <= 6 && k < widest.Length; k++) candle = Mathf.Max(candle, widest[k]);
            if (candle <= 0.002f) return false;
            for (int k = 0; k < widest.Length; k++)
            {
                if (widest[k] < 0f) continue;
                if (widest[k] > candle * Widening) break;
                bottom = (k + 1) * Bin;
            }
            for (int i = 0; i < world.Length; i++)
            {
                var p = world[i];
                float h = wick.y - p.y, r = new Vector2(p.x - wick.x, p.z - wick.z).magnitude;
                if (h >= -Above && h <= bottom && r <= candle * Widening) wax[i] = true;
            }
            return bottom > 0f;
        }

        /// <summary>
        /// A fire particle system: the radius of its flame (the source of soft shadows), its brightness in candles
        /// (a torch about 10, a stove's fire 20, an oil flame 3) and the height of the flame's body above the system
        /// (ps_torch_fire: particles of 15-20 cm rising from the bottom of the cup, its rim 4 cm above the base).
        /// </summary>
        static bool Flame(string file, out float radius, out float candles, out float lift)
        {
            string f = (file ?? "").ToLowerInvariant();
            (radius, candles, lift) = f.Contains("candle") ? (0.012f, 1f, 0.015f) : f.Contains("torch") ? (0.06f, 10f, 0.15f)
                : f.Contains("stove") || f.Contains("bonfire") ? (0.15f, 20f, 0.15f) : (0.03f, 3f, 0.05f);
            return f.Contains("fire") || f.Contains("flame") || f.Contains("candle");
        }

        /// <summary>
        /// Physical brightness: candela at the map color (fades and flicker scale it) and the reach where the light
        /// falls below <see cref="VisibleLux"/>; HPL2's radius was a hard edge (a candle 1.7 m, a torch 3.4 m).
        /// </summary>
        static void SetPhysical(AmnesiaLight light, float candela)
        {
            light.enhancedCandela = candela;
            light.enhancedRange = Mathf.Min(Mathf.Sqrt(candela / VisibleLux), MaxRange);
        }

        /// <summary>
        /// The light of the hand object (the lantern), when it is drawn or the settings change: a hurricane lantern
        /// gives about 10 candles. The small glow of the glass keeps HPL2's values.
        /// </summary>
        public static void HandLight(AmnesiaLight light)
        {
            if (!Applied || light == null || !light.handHeld || light.radius < HplRayTracing.MinHandShadowRadius) return;
            if (s_changed.Exists(c => c.light == light)) return;
            Record(light);
            SetPhysical(light, LanternCandles * CandleCandela);
            light.Apply(light.CurrentColor, light.CurrentRadius);
        }

        static Changed Record(AmnesiaLight light)
        {
            HplRayTracing.Release(light);
            var unityLight = light.GetComponent<Light>();
            var hd = light.GetComponent<HDAdditionalLightData>();
            var record = new Changed
            {
                light = light, castShadows = light.castShadows, baseShadowLevel = light.baseShadowLevel,
                localPosition = light.transform.localPosition, localRotation = light.transform.localRotation,
                shadows = unityLight != null ? unityLight.shadows : LightShadows.None,
                updateMode = hd != null ? hd.shadowUpdateMode : ShadowUpdateMode.EveryFrame,
                normalBias = hd != null ? hd.normalBias : 0.75f, slopeBias = hd != null ? hd.slopeBias : 0.5f,
                nearPlane = hd != null ? hd.shadowNearPlane : 0.1f,
            };
            s_changed.Add(record);
            return record;
        }

        static void Move(AmnesiaLight light, Changed record, Vector3 position, Quaternion? rotation)
        {
            if (!record.moved)
            {
                record.moved = true;
                light.classicLocalPose ??= Matrix4x4.TRS(light.transform.localPosition, light.transform.localRotation, light.transform.localScale);
            }
            light.transform.position = position;
            if (rotation is { } r) light.transform.rotation = r;
        }

        static void SetShadows(AmnesiaLight light, bool on, bool lowResolution = false)
        {
            var unityLight = light.GetComponent<Light>();
            if (!on)
            {
                light.castShadows = false;
                light.noTracedShadows = true;
                if (unityLight != null) unityLight.shadows = LightShadows.None;
                return;
            }
            light.castShadows = true;
            light.noTracedShadows = false;
            if (light.TryGetComponent(out HDAdditionalLightData hd))
            {
                hd.shadowUpdateMode = ShadowUpdateMode.EveryFrame;
                // HPL2's walls are two relief halves back to back: the mortar of both sides lies in one plane, the
                // wall is zero thick at the seams. A torch behind the wall lit the next room along the seams
                // (torch_static01_4 of 01_old_archives). The normal bias moves the receiver into its own room, the
                // slope bias would move the far side's depth through the wall at grazing angles.
                hd.normalBias = ShadowNormalBias;
                hd.slopeBias = ShadowSlopeBias;
                // A lamp hangs a few cm from its wall and its own holder: HDRP's 10 cm near plane cut them out.
                hd.shadowNearPlane = ShadowNearPlane;
            }
            if (lowResolution) light.baseShadowLevel = 0;
            HplGraphics.ApplyShadows(light);
        }

        static void Change(AmnesiaLight light, Entry e)
        {
            var record = Record(light);
            if (TryFloat(e.intensity, out float intensity)) light.enhancedIntensity = intensity;
            if (TryFloat(e.range, out float range)) light.enhancedRange = range;
            if (TryFloat(e.sourceRadius, out float sourceRadius)) light.tracedSourceRadius = sourceRadius;
            bool move = TryVector(e.position, out Vector3 position);
            bool aim = TryVector(e.target, out Vector3 target);
            if (move || aim)
            {
                Vector3 p = move ? HplSpace.Position(position) : light.transform.position;
                Move(light, record, p, aim ? Look(HplSpace.Position(target) - p) : null);
            }
            switch ((e.shadows ?? "").Trim().ToLowerInvariant())
            {
                case "on": SetShadows(light, true); break;
                case "off": SetShadows(light, false); break;
            }
            light.Apply(light.CurrentColor, light.CurrentRadius);
        }

        static void Add(Entry e)
        {
            if (!TryVector(e.position, out Vector3 position)) { Warn($"added light '{e.name}' without a position"); return; }
            var type = (e.type ?? "point").Trim().ToLowerInvariant() switch
            {
                "spot" => LightType.Spot,
                "rect" => LightType.Rectangle,
                "point" => LightType.Point,
                var other => (LightType)(-1),
            };
            if ((int)type < 0) { Warn($"added light '{e.name}': unknown type '{e.type}'"); return; }
            if (s_addedRoot == null)
            {
                s_addedRoot = new GameObject("Enhanced lights") { hideFlags = HideFlags.DontSave };
                s_addedRoot.transform.SetParent(s_map.transform, false);
            }
            var go = new GameObject(e.name) { hideFlags = HideFlags.DontSave };
            go.SetActive(false);
            go.transform.SetParent(s_addedRoot.transform, false);
            Vector3 p = HplSpace.Position(position);
            go.transform.position = p;
            if (TryVector(e.target, out Vector3 target)) go.transform.rotation = Look(HplSpace.Position(target) - p);
            go.AddHDLight(type);
            var light = go.GetComponent<Light>();
            var hd = go.GetComponent<HDAdditionalLightData>();
            Color color = TryVector(e.color, out Vector3 c) ? new Color(c.x, c.y, c.z) : Color.white;
            light.color = color;
            light.lightUnit = type == LightType.Rectangle ? LightUnit.Nits : LightUnit.Candela;
            float intensity = TryFloat(e.intensity, out float i) ? i : 0f;
            light.intensity = intensity;
            light.range = TryFloat(e.range, out float range) ? range : 10f;
            if (type == LightType.Rectangle)
                light.areaSize = TryVector2(e.size, out Vector2 size) ? size : Vector2.one;
            if (type == LightType.Spot && TryFloat(e.angle, out float angle))
            {
                light.spotAngle = angle;
                light.innerSpotAngle = angle * 0.5f;
            }
            if (TryFloat(e.sourceRadius, out float sourceRadius) && type != LightType.Rectangle) light.shapeRadius = sourceRadius;
            bool shadows = (e.shadows ?? "").Trim().ToLowerInvariant() == "on";
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            if (hd != null)
            {
                hd.SetShadowResolutionOverride(false);
                hd.SetShadowResolutionLevel(2);
            }
            // Like the map's lights, they also light the player's hands.
            HplLighting.SetLightLayers(light, HplLighting.LightLayersMask);

            var data = go.AddComponent<AmnesiaLight>();
            data.enhancedAdded = true;
            data.castShadows = shadows;
            data.noTracedShadows = !shadows;
            data.radius = light.range;
            data.diffuseColor = color;
            data.tracedSourceRadius = TryFloat(e.sourceRadius, out sourceRadius) ? sourceRadius : -1f;
            if (!string.IsNullOrEmpty(e.follow))
            {
                var source = s_map.GetLight(e.follow);
                if (source == null) Warn($"added light '{e.name}': no light '{e.follow}' to follow");
                else
                {
                    var follow = go.AddComponent<HplEnhancedLightFollow>();
                    follow.source = source;
                    follow.intensity = intensity;
                }
            }
            go.SetActive(true);
        }

        /// <summary>Back to the classic lights of the map.</summary>
        static void Revert()
        {
            for (int i = s_changed.Count - 1; i >= 0; i--)
            {
                var r = s_changed[i];
                var light = r.light;
                if (light == null) continue;
                HplRayTracing.Release(light);
                light.enhancedIntensity = 1f;
                light.enhancedRange = 0f;
                light.enhancedCandela = 0f;
                light.tracedSourceRadius = -1f;
                light.noTracedShadows = false;
                if (r.moved)
                {
                    light.transform.localPosition = r.localPosition;
                    light.transform.localRotation = r.localRotation;
                    light.classicLocalPose = null;
                }
                light.castShadows = r.castShadows;
                light.baseShadowLevel = r.baseShadowLevel;
                if (light.TryGetComponent(out Light unityLight)) unityLight.shadows = r.shadows;
                if (light.TryGetComponent(out HDAdditionalLightData hd))
                {
                    hd.shadowUpdateMode = r.updateMode;
                    hd.normalBias = r.normalBias;
                    hd.slopeBias = r.slopeBias;
                    hd.shadowNearPlane = r.nearPlane;
                }
                HplGraphics.ApplyShadows(light);
                light.Apply(light.CurrentColor, light.CurrentRadius);
            }
            s_changed.Clear();
            HplEnhancedEmission.Revert();
            foreach (var (filter, original, split, wax, waxMesh, ownMaterial) in s_wax)
            {
                if (filter != null) filter.sharedMesh = original;
                // Destroy waits for the end of the frame; inactive and out of the lamp, the wax and its glow are gone
                // now (a refresh in the same frame would split the old wax again).
                if (wax != null)
                {
                    wax.SetActive(false);
                    wax.transform.SetParent(null, false);
                    UnityEngine.Object.Destroy(wax);
                }
                UnityEngine.Object.Destroy(split);
                UnityEngine.Object.Destroy(waxMesh);
                if (ownMaterial != null) UnityEngine.Object.Destroy(ownMaterial);
            }
            s_wax.Clear();
            s_waxRenderers.Clear();
            s_glows.Clear();
            foreach (var (r, mode) in s_casters)
                if (r != null) r.shadowCastingMode = mode;
            s_casters.Clear();
            if (s_addedRoot != null)
            {
                foreach (var light in s_addedRoot.GetComponentsInChildren<AmnesiaLight>(true)) HplRayTracing.Release(light);
                // Destroy waits for the end of the frame; inactive, the lights are gone now.
                s_addedRoot.SetActive(false);
                UnityEngine.Object.Destroy(s_addedRoot);
                s_addedRoot = null;
            }
            if (Applied) HplRayTracing.Reselect();
            Applied = false;
        }

        /// <summary>The rotation of an HDRP light shining along a Unity direction.</summary>
        static Quaternion Look(Vector3 direction)
        {
            if (direction.sqrMagnitude < 1e-8f) return Quaternion.identity;
            Vector3 up = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(direction, up);
        }

        static bool TryFloat(string s, out float value)
        {
            value = 0f;
            return !string.IsNullOrWhiteSpace(s) && float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static float[] Floats(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<float>();
            var parts = s.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var values = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i])) return Array.Empty<float>();
            return values;
        }

        static bool TryVector(string s, out Vector3 value)
        {
            var v = Floats(s);
            value = v.Length == 3 ? new Vector3(v[0], v[1], v[2]) : Vector3.zero;
            return v.Length == 3;
        }

        static bool TryVector2(string s, out Vector2 value)
        {
            var v = Floats(s);
            value = v.Length == 2 ? new Vector2(v[0], v[1]) : Vector2.zero;
            return v.Length == 2;
        }
    }

    /// <summary>
    /// An added light of the enhanced lighting that follows a light of the map: its brightness (a lamp lit or put out,
    /// fades, flicker) and visibility. The HDRP light is written only when it changes (path tracing restarts then).
    /// </summary>
    public sealed class HplEnhancedLightFollow : MonoBehaviour
    {
        public AmnesiaLight source;
        /// <summary>The intensity while the source shows its map color.</summary>
        public float intensity;
        Light _light;
        float _baseLevel;

        void Start()
        {
            _light = GetComponent<Light>();
            Color c = source != null ? source.diffuseColor : Color.black;
            _baseLevel = Mathf.GammaToLinearSpace(Mathf.Max(c.r, c.g, c.b));
            LateUpdate();
        }

        void LateUpdate()
        {
            if (_light == null) return;
            float factor = 0f;
            if (source != null && source.isActiveAndEnabled && source.Visible)
            {
                Color c = source.CurrentColor;
                float level = Mathf.GammaToLinearSpace(Mathf.Max(0f, Mathf.Max(c.r, c.g, c.b)));
                factor = _baseLevel > 0f ? level / _baseLevel : (level > 0f ? 1f : 0f);
            }
            float value = intensity * factor;
            bool on = value > 0f;
            if (Mathf.Abs(_light.intensity - value) > 1e-4f * Mathf.Max(1f, intensity)) _light.intensity = value;
            if (_light.enabled != on) _light.enabled = on;
        }
    }
}
