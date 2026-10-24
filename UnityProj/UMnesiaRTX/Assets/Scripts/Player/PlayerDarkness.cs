using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Player
{
    /// <summary>
    /// cLuxPlayerLightLevel (LuxPlayerHelpers.cpp): twice a second the light level at five points of the body
    /// (center, above the feet, the head, in front of the center and of the head; HPL2's "feet" and "head" labels are
    /// swapped) is the max over the points of the summed lights (cLuxMapHelper::GetLightLevelAtPos, AmnesiaLight.LightLevelAt),
    /// once with the light radii as they are (normal) and once extended by Player_Darkness RadiusAdd. A lit lantern adds 1.
    /// The player's darkness light is skipped.
    /// The darkness by the visible light (game option, only with the enhanced lighting; not in HPL2): the enhanced
    /// lighting moves the lamps into their flames and gives them the physical falloff, so the light radii of HPL2 no
    /// longer match what is seen. Then the same points are Daniel's own light sensor (HplLightAnalysis.SensorLux: the
    /// HDRP lights as they shine, with the rays that reach them), in levels of <see cref="LevelLux"/>. The extended level
    /// (HPL2: the radii + RadiusAdd, "a light is near") is the max of the body and of eight points around it, up to
    /// RadiusAdd away, short of the walls. The lantern still adds 1.
    /// </summary>
    public sealed class PlayerLightLevel
    {
        /// <summary>
        /// Lux per light level: light level 1 is the light of an HPL2 light of color 1 at its center, which shows a white
        /// lambertian surface as white (HplLighting.UnitRadiance nits): π × that. With it the thresholds of game.cfg
        /// (darkness 0.15, the darkness light 0.2, enemies 0.35) keep their meaning. To be calibrated with the lamps.
        /// </summary>
        public static float LevelLux => Mathf.PI * Hpl.HplLighting.UnitRadiance;
        /// <summary>The ring of the extended level stops this short of a wall (m).</summary>
        const float RingWallMargin = 0.2f;

        /// <summary>Chosen in the game options (HplUserSettings.visibleDarkness).</summary>
        public static bool VisibleChosen { get; set; } = true;
        /// <summary>In effect: chosen, with the enhanced lighting.</summary>
        public static bool VisibleOn => VisibleChosen && Hpl.HplGraphics.Lighting == 1;

        static readonly Vector3[] s_points = new Vector3[13];
        static readonly float[] s_lux = new float[13];

        readonly AmnesiaPlayer _player;
        readonly float _radiusAdd;
        float _updateCount;
        bool _visible;

        public float ExtendedLightLevel { get; private set; } = 1f;
        public float NormalLightLevel { get; private set; } = 1f;

        public PlayerLightLevel(AmnesiaPlayer player, Hpl.HplConfig cfg)
        {
            _player = player;
            _radiusAdd = cfg.GetFloat("Player_Darkness", "RadiusAdd", 0f);
        }

        /// <summary>cLuxPlayerLightLevel::OnMapEnter.</summary>
        public void OnMapEnter() => _updateCount = 0f;

        public void Update(float timeStep)
        {
            if (_updateCount > 0f)
            {
                _updateCount -= timeStep;
                return;
            }
            _updateCount = 1f / 2f;
            Measure(VisibleOn, out float normal, out float ext);
            ExtendedLightLevel = ext;
            NormalLightLevel = normal;
        }

        /// <summary>The option or the lighting changed: measure again at the next update.</summary>
        public void Refresh()
        {
            if (_visible != VisibleOn) _updateCount = 0f;
        }

        /// <summary>The light levels at the body now, of HPL2's light radii or of the <paramref name="visible"/> light.</summary>
        public void Measure(bool visible, out float normal, out float ext)
        {
            _visible = visible;
            var body = _player.Body;
            Vector3 pos = body.Position;
            Vector3 size = body.Size;
            Vector3 forward = body.Forward;
            Vector3 up = new(0f, size.y - 0.1f, 0f);
            Vector3 front = forward * size.z * 0.8f;
            s_points[0] = pos; s_points[1] = pos + up; s_points[2] = pos - up; s_points[3] = pos + front; s_points[4] = pos - up + front;

            ext = 0f;
            normal = 0f;
            if (_player.Lantern != null && _player.Lantern.Active)
            {
                ext += 1f;
                normal += 1f;
            }
            var scene = _player.gameObject.scene;
            if (visible)
            {
                int count = 5;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float d = _radiusAdd;
                    if (Physics.Raycast(pos, dir, out var hit, _radiusAdd, HplLightAnalysis.QueryMask, QueryTriggerInteraction.Ignore))
                        d = Mathf.Max(hit.distance - RingWallMargin, 0f);
                    s_points[count++] = pos + dir * d;
                }
                HplLightAnalysis.SensorLux(scene, s_points, count, s_lux);
                float n = 0f, e = 0f;
                for (int i = 0; i < count; i++)
                {
                    if (i < 5) n = Mathf.Max(n, s_lux[i]);
                    e = Mathf.Max(e, s_lux[i]);
                }
                normal += n / LevelLux;
                ext += e / LevelLux;
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                var p = s_points[i];
                float e = 0f, n = 0f;
                foreach (var light in AmnesiaLight.All)
                {
                    if (light.excludeFromLightLevel || light.gameObject.scene != scene) continue;
                    e += light.LightLevelAt(p, _radiusAdd);
                    n += light.LightLevelAt(p, 0f);
                }
                ext = Mathf.Max(e, ext);
                normal = Mathf.Max(n, normal);
            }
        }
    }

    /// <summary>
    /// cLuxPlayerInDarkness: below Player_Darkness AmbientLightMinLightLevel (normal light level) a weak bluish point
    /// light fades in 0.3 m below the camera; below MinLightLevel (extended level) sanity drains at
    /// SanityLossPerSecond × a multiplier that grows 0.1/s (falls 0.3/s in light), with the DarknessDecrease hint every
    /// 3 s, unless a flashback or emotion flash is on or the drain is disabled. The loop sound is commented out in
    /// HPL2 too. The light is created per map (cLuxPlayerInDarkness::CreateWorldEntities) and not saved.
    /// HPL2's default falloff (core_falloff_linear over (d / r)²) is almost flat near the player while HDRP lights
    /// fall off with 1/d²: the HDRP light gets shapeRadius = radius (1/(d² + r²)) and 4× the candela, which keeps
    /// the HPL2 brightness at the player and close to it up to ~half the radius.
    /// </summary>
    public sealed class PlayerInDarkness
    {
        readonly AmnesiaPlayer _player;
        readonly float _minDarknessLightLevel, _ambientLightMinLightLevel, _ambientLightRadius, _ambientLightIntensity;
        readonly float _ambientLightFadeInTime, _ambientLightFadeOutTime, _sanityLossPerSecond;
        readonly Color _ambientLightColor;
        AmnesiaLight _ambientLight;
        bool _ambientLightIsOn;
        float _sanityLossMul, _showHintTimer;

        public bool Active { get; private set; } = true;
        public bool IsInDarkness { get; private set; }
        public AmnesiaLight AmbientLight => _ambientLight;

        public PlayerInDarkness(AmnesiaPlayer player, Hpl.HplConfig cfg)
        {
            _player = player;
            const string g = "Player_Darkness";
            _minDarknessLightLevel = cfg.GetFloat(g, "MinLightLevel", 0f);
            _ambientLightMinLightLevel = cfg.GetFloat(g, "AmbientLightMinLightLevel", 0f);
            _ambientLightRadius = cfg.GetFloat(g, "AmbientLightRadius", 0f);
            _ambientLightIntensity = cfg.GetFloat(g, "AmbientLightIntensity", 0f);
            _ambientLightFadeInTime = cfg.GetFloat(g, "AmbientLightFadeInTime", 0f);
            _ambientLightFadeOutTime = cfg.GetFloat(g, "AmbientLightFadeOutTime", 0f);
            _ambientLightColor = cfg.GetColor(g, "AmbientLightColor", Color.clear);
            _sanityLossPerSecond = cfg.GetFloat(g, "SanityLossPerSecond", 0f);
        }

        /// <summary>cLuxPlayerInDarkness::CreateWorldEntities: the darkness light, black, without shadows.</summary>
        public void CreateWorldEntities(Transform parent)
        {
            if (_ambientLight != null) return;
            var go = new GameObject("PlayerDarknessAmbient") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            go.AddHDLight(LightType.Point);
            var light = go.GetComponent<Light>();
            light.shadows = LightShadows.None;
            _ambientLight = go.AddComponent<AmnesiaLight>();
            _ambientLight.excludeFromLightLevel = true;
            _ambientLight.diffuseColor = Color.clear;
            // HARDMODE: half the radius.
            float radius = _ambientLightRadius * (ScriptApi.HplDifficulty.HardMode ? 0.5f : 1f);
            _ambientLight.radius = radius;
            _ambientLight.InitRuntime();
            _ambientLight.Apply(Color.clear, radius);
            light.shapeRadius = radius;
            _ambientLight.onApplied = ScaleNearField;
        }

        /// <summary>4× the candela with shapeRadius = radius: HDRP's 1/(d² + r²) is then 1 at the player like HPL2.</summary>
        static void ScaleNearField(Light light) => light.intensity *= 4f;

        /// <summary>cLuxPlayerInDarkness::OnMapEnter.</summary>
        public void OnMapEnter()
        {
            _ambientLightIsOn = false;
            IsInDarkness = false;
        }

        /// <summary>cLuxPlayerInDarkness::InDarkness.</summary>
        public bool InDarkness() => _player.LightLevel.ExtendedLightLevel <= _minDarknessLightLevel;

        public void SetActive(bool active)
        {
            Active = active;
            if (active) return;
            if (_ambientLightIsOn)
            {
                _ambientLightIsOn = false;
                _ambientLight?.FadeTo(Color.clear, _ambientLight.CurrentRadius, _ambientLightFadeOutTime);
            }
            IsInDarkness = false;
            _sanityLossMul = 0f;
        }

        public void Update(float timeStep)
        {
            if (!Active || _ambientLight == null) return;
            float extLightLevel = _player.LightLevel.ExtendedLightLevel;
            float normalLightLevel = _player.LightLevel.NormalLightLevel;

            // Only a real move: any write marks the light's transform changed, and HDRP's path tracer then restarts.
            Vector3 position = _player.LogicCameraPosition - new Vector3(0f, 0.3f, 0f);
            if ((_ambientLight.transform.position - position).sqrMagnitude > 1e-8f) _ambientLight.transform.position = position;
            // A world light of HPL2 (cWorld updates it); not in the map's light list here.
            _ambientLight.UpdateLight(timeStep);

            if (normalLightLevel > _ambientLightMinLightLevel)
            {
                if (_ambientLightIsOn)
                {
                    _ambientLightIsOn = false;
                    _ambientLight.FadeTo(Color.clear, _ambientLight.CurrentRadius, _ambientLightFadeOutTime);
                }
            }
            else if (!_ambientLightIsOn)
            {
                _ambientLightIsOn = true;
                // HARDMODE: 0.75 of the light, fading in 2.5 times slower.
                if (ScriptApi.HplDifficulty.HardMode)
                    _ambientLight.FadeTo(_ambientLightColor * _ambientLightIntensity * 0.75f, _ambientLight.CurrentRadius, _ambientLightFadeInTime * 2.5f);
                else
                    _ambientLight.FadeTo(_ambientLightColor * _ambientLightIntensity, _ambientLight.CurrentRadius, _ambientLightFadeInTime);
            }

            if (extLightLevel > _minDarknessLightLevel)
            {
                IsInDarkness = false;
                _sanityLossMul -= timeStep * 0.3f;
                if (_sanityLossMul < 0f) _sanityLossMul = 0f;
            }
            else
            {
                _sanityLossMul += timeStep * 0.1f;
                if (_sanityLossMul > 1f) _sanityLossMul = 1f;

                var map = HplMap.Current;
                bool emotionFlash = map != null && map.effects != null && map.effects.EmotionFlash.Active;
                if (!_player.Flashback.Active && !_player.sanityDrainDisabled && !emotionFlash)
                {
                    _player.LowerSanity(_sanityLossPerSecond * timeStep * _sanityLossMul, true);
                    if (_showHintTimer <= 0f && _sanityLossMul > 0.05f)
                    {
                        _showHintTimer = 3f;
                        if (map != null) map.hud.Hints.Add("DarknessDecrease", map.hud.Translate("Hints", "DarknessDecrease"), 0f);
                    }
                    else _showHintTimer -= timeStep;
                }
                IsInDarkness = true;
            }
        }
    }
}
