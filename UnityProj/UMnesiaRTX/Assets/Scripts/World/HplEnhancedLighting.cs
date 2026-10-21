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
        /// <summary>
        /// One candle flame, cd: the classic candlestick at its map color (Candela(1.7) × 0.85 → 15), so a room lit by
        /// candles keeps its brightness near them; the other fires are counted in candles from it.
        /// </summary>
        public const float CandleCandela = 15f;
        public const float LanternCandles = 10f;
        /// <summary>Illuminance (lux) where a light ends: an albedo-0.5 wall at EV100 3 is ~0.4% of white there.</summary>
        public const float VisibleLux = 0.25f;
        public const float MaxRange = 25f;

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
        }

        static HplMap s_map;
        static Overlay s_overlay;
        static readonly List<Changed> s_changed = new();
        static GameObject s_addedRoot;

        /// <summary>The enhanced lighting is chosen (graphics tab).</summary>
        public static bool Enabled => HplGraphics.Lighting == 1;
        /// <summary>The current map has an overlay and it is applied.</summary>
        public static bool Applied { get; private set; }
        /// <summary>Lights of the map changed and lights added (checks).</summary>
        public static int ChangedLights => s_changed.Count;
        public static int AddedLights => s_addedRoot != null ? s_addedRoot.transform.childCount : 0;

        /// <summary>The map is built (HplMap.Build, Play only): its overlay is read and applied if the enhanced lighting is chosen.</summary>
        public static void Attach(HplMap map)
        {
            if (s_map != null && s_map != map) Revert();
            s_map = map;
            s_overlay = Load(map.mapFile);
            Refresh();
        }

        /// <summary>The overlay of a map file, null without one.</summary>
        static Overlay Load(string mapFile)
        {
            if (string.IsNullOrEmpty(mapFile)) return null;
            string path = ResourceFolder + System.IO.Path.ChangeExtension(mapFile.Replace('\\', '/'), null);
            var text = Resources.Load<TextAsset>(path);
            if (text == null) return null;
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
            foreach (var entity in s_map.GetComponentsInChildren<AmnesiaEntity>(true))
            {
                if (entity.entityType != "Lamp") continue;
                var flames = new List<(Vector3 position, float radius)>();
                float candles = 0f;
                foreach (var ps in entity.GetComponentsInChildren<AmnesiaParticleSystem>(true))
                    if (Flame(ps.file, out float r, out float c)) { flames.Add((ps.transform.position, r)); candles += c; }
                if (flames.Count == 0) continue;
                Vector3 center = Vector3.zero;
                float flameRadius = 0f;
                foreach (var f in flames) { center += f.position; flameRadius = Mathf.Max(flameRadius, f.radius); }
                center /= flames.Count;
                float spread = 0f;
                foreach (var f in flames) spread = Mathf.Max(spread, Vector3.Distance(f.position, center));
                // The light in the flame (the particle system starts at its base).
                Vector3 target = center + Vector3.up * flameRadius;
                var lights = new List<AmnesiaLight>();
                foreach (var light in entity.GetComponentsInChildren<AmnesiaLight>(true))
                    if (!light.box && !light.handHeld && s_map.GetLight(light.name) == light) lights.Add(light);
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
        /// A fire particle system: the radius of its flame (the source of soft shadows) and its brightness in candles
        /// (a torch about 10, a stove's fire 20, an oil flame 3).
        /// </summary>
        static bool Flame(string file, out float radius, out float candles)
        {
            string f = (file ?? "").ToLowerInvariant();
            (radius, candles) = f.Contains("candle") ? (0.012f, 1f) : f.Contains("torch") ? (0.06f, 10f)
                : f.Contains("stove") || f.Contains("bonfire") ? (0.15f, 20f) : (0.03f, 3f);
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
            if (light.TryGetComponent(out HDAdditionalLightData hd)) hd.shadowUpdateMode = ShadowUpdateMode.EveryFrame;
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
                if (light.TryGetComponent(out HDAdditionalLightData hd)) hd.shadowUpdateMode = r.updateMode;
                HplGraphics.ApplyShadows(light);
                light.Apply(light.CurrentColor, light.CurrentRadius);
            }
            s_changed.Clear();
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
