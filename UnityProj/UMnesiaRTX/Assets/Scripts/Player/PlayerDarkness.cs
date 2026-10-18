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
    /// </summary>
    public sealed class PlayerLightLevel
    {
        readonly AmnesiaPlayer _player;
        readonly float _radiusAdd;
        float _updateCount;

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

            var body = _player.Body;
            Vector3 pos = body.Position;
            Vector3 size = body.Size;
            Vector3 forward = body.Forward;
            Vector3 up = new(0f, size.y - 0.1f, 0f);
            Vector3 front = forward * size.z * 0.8f;
            Vector3[] test = { pos, pos + up, pos - up, pos + front, pos - up + front };

            float ext = 0f, normal = 0f;
            if (_player.Lantern != null && _player.Lantern.Active)
            {
                ext += 1f;
                normal += 1f;
            }
            var scene = _player.gameObject.scene;
            foreach (var p in test)
            {
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
            ExtendedLightLevel = ext;
            NormalLightLevel = normal;
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
