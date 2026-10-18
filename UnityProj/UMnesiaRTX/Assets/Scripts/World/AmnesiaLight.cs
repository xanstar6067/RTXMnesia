using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// An HPL2 point, spot or box light. Keeps the original parameters, because level scripts work in HPL2 terms
    /// (FadeLightTo(color, radius, time)), and drives the HDRP light from them via <see cref="HplLighting"/>.
    /// A box light (cLightBox, <see cref="box"/>) has no HDRP light: <see cref="HplBoxLightPass"/> draws it.
    /// At runtime it does what iLight::UpdateLight does each step: color / radius fades and flickering.
    /// </summary>
    [ExecuteAlways]
    public class AmnesiaLight : MonoBehaviour
    {
        [Tooltip("HPL2 DiffuseColor, may exceed 1. Alpha = specular strength (HDRP has no per-light equivalent).")]
        public Color diffuseColor = Color.white;
        [Min(0f)] public float radius = 1f;
        [Tooltip("1D falloff texture over (distance / radius)². Only informative: HDRP uses 1/d² with a range window.")]
        public string falloffMap;
        public string gobo;
        public bool castShadows;
        [Tooltip("Spot frustum (cLightSpot): vertical FOV in radians, aspect, near clip. 0 = from the HDRP light.")]
        public float spotFov, spotAspect = 1f, spotNearClip = 0.1f;

        [Header("Flicker")]
        public bool flickerActive;
        public Color flickerOffColor = Color.black;
        public float flickerOffRadius;
        public Vector2 flickerOnLength;
        public Vector2 flickerOffLength;
        public bool flickerFade;
        public Vector2 flickerOnFadeLength;
        public Vector2 flickerOffFadeLength;
        public string flickerOnSound, flickerOffSound, flickerOnPS, flickerOffPS;

        [Header("Box (cLightBox)")]
        [Tooltip("Ambient light inside an axis-aligned box (HPL2 ignores the rotation): diffuse × color, no N·L.")]
        public bool box;
        public Vector3 boxSize = Vector3.one;
        [Tooltip("BlendFunc 1: adds to the boxes before it; 0 (Replace): sets the color.")]
        public bool boxBlendAdd;
        public bool boxVisible = true;

        // Runtime state (iLight): current color / radius, fade and flicker.
        Color _color, _colorAdd, _destColor, _flickerOnColor;
        float _radius, _radiusAdd, _destRadius, _fadeTime, _flickerOnRadius;
        bool _flickering, _flickerOn;
        float _flickerTime, _flickerStateLength;
        bool _runtime, _boxVisible;

        public Color CurrentColor => _runtime ? _color : diffuseColor;
        public float CurrentRadius => _runtime ? _radius : radius;
        public bool Visible => box ? (_runtime ? _boxVisible : boxVisible) : TryGetComponent(out Light l) && l.enabled;

        /// <summary>Enabled lights of all loaded scenes (the light level of the player, cLuxMapHelper::GetLightLevelAtPos).</summary>
        public static readonly System.Collections.Generic.List<AmnesiaLight> All = new();

        /// <summary>The player's darkness light (skipped by the light level).</summary>
        [System.NonSerialized] public bool excludeFromLightLevel;

        /// <summary>A light of the hand object (the lantern, PlayerHands): its ray-traced shadows have their own setting.</summary>
        [System.NonSerialized] public bool handHeld;

        /// <summary>The HDRP shadow resolution tier of the map (before the shadow setting lowers it); −1: not read yet.</summary>
        [System.NonSerialized] public int baseShadowLevel = -1;

        void OnEnable()
        {
            All.Add(this);
            HplGraphics.ApplyShadows(this);
            if (box) HplBoxLightPass.Register(this);
        }

        void OnDisable()
        {
            All.Remove(this);
            HplBoxLightPass.Unregister(this);
        }

        /// <summary>
        /// cLuxMapHelper::GetLightLevelAtPos for one light: 0 when the point is outside the light's volume
        /// (GetLightsAtNode: box, sphere of the radius, spot frustum); a box gives the max of its color, point and spot
        /// lights max color × (1 − distance / (radius + radiusAdd)). Shadow-casting spots need a line of sight.
        /// </summary>
        public float LightLevelAt(Vector3 position, float radiusAdd)
        {
            if (!Visible || !isActiveAndEnabled) return 0f;
            Color c = CurrentColor;
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            if (box)
            {
                Vector3 d = position - transform.position, half = boxSize * 0.5f;
                if (Mathf.Abs(d.x) > half.x || Mathf.Abs(d.y) > half.y || Mathf.Abs(d.z) > half.z) return 0f;
                return max;
            }
            float r = CurrentRadius;
            float dist = Vector3.Distance(transform.position, position);
            if (TryGetComponent(out Light light) && light.type != LightType.Point)
            {
                if (!InSpotFrustum(position, light, r)) return 0f;
                if (castShadows && !ScriptApi.HplCollision.CheckLineOfSight(transform.position, position)) return 0f;
            }
            else if (dist > r) return 0f;
            float t = 1f - dist / (r + radiusAdd);
            return t > 0f ? max * t : 0f;
        }

        /// <summary>cFrustum::CollidePoint of the spot (perspective along the light's forward, far plane = radius).</summary>
        bool InSpotFrustum(Vector3 position, Light light, float far)
        {
            float fov = spotFov > 0f ? spotFov : light.spotAngle * Mathf.Deg2Rad;
            float aspect = spotFov > 0f ? spotAspect : 1f;
            Vector3 p = transform.InverseTransformPoint(position);
            if (p.z < spotNearClip || p.z > far) return false;
            float h = p.z * Mathf.Tan(fov * 0.5f);
            return Mathf.Abs(p.y) <= h && Mathf.Abs(p.x) <= h * aspect;
        }

        public void Apply() => Apply(diffuseColor, radius);

        /// <summary>Sets the HDRP light to show an HPL2 light of this color and radius.</summary>
        public void Apply(Color color, float lightRadius)
        {
            if (!TryGetComponent(out Light target)) return;
            float max = Mathf.Max(color.r, color.g, color.b);
            if (max <= 0f || lightRadius <= 0f)
            {
                target.intensity = 0f;
                return;
            }
            // Light.color is sRGB and kept ≤ 1; the brightest channel goes into the intensity.
            target.color = new Color(color.r / max, color.g / max, color.b / max);
            target.lightUnit = LightUnit.Candela;
            target.intensity = HplLighting.Candela(lightRadius) * Mathf.GammaToLinearSpace(max);
            target.range = lightRadius;
            onApplied?.Invoke(target);
        }

        /// <summary>Adjusts the HDRP light after <see cref="Apply(Color, float)"/> (the player's darkness light).</summary>
        [System.NonSerialized] public System.Action<Light> onApplied;

        /// <summary>Start of the map: current values from the map data, flicker as iLight::SetFlicker leaves it.</summary>
        public void InitRuntime()
        {
            // Map lights also light the player's hands (their own rendering layer, see HplLighting.HandsRenderingLayer).
            if (TryGetComponent(out Light light)) HplLighting.SetLightLayers(light, HplLighting.GetLightLayers(light) | HplLighting.HandsLayerMask);
            _runtime = true;
            _boxVisible = boxVisible;
            _color = diffuseColor;
            _radius = radius;
            _fadeTime = 0f;
            _flickerOnColor = _color;
            _flickerOnRadius = _radius;
            _flickerOn = true;
            _flickerTime = 0f;
            _flickerStateLength = HplMath.RandRectf(flickerOnLength.x, flickerOnLength.y);
            _flickering = flickerActive;
        }

        /// <summary>iLight::FadeTo: linear change of color and radius over the time.</summary>
        public void FadeTo(Color color, float toRadius, float time)
        {
            if (time <= 0f) time = 0.0001f;
            _fadeTime = time;
            _colorAdd = (color - _color) / time;
            _radiusAdd = (toRadius - _radius) / time;
            _destRadius = toRadius;
            _destColor = color;
        }

        public void SetVisible(bool visible)
        {
            if (box) _boxVisible = visible;
            else if (TryGetComponent(out Light l)) l.enabled = visible;
        }

        public void SetFlickerActive(bool active) => _flickering = active;

        public bool FlickerActive => _runtime ? _flickering : flickerActive;

        /// <summary>iLight::SetDiffuseColor (also the color flicker returns to).</summary>
        public void SetColor(Color color)
        {
            _flickerOnColor = color;
            SetColorAndRadius(color, _radius);
        }

        /// <summary>iLight::SetRadius (also the radius flicker returns to).</summary>
        public void SetRadius(float lightRadius)
        {
            _flickerOnRadius = lightRadius;
            SetColorAndRadius(_color, lightRadius);
        }

        public void StopFading() => _fadeTime = 0f;

        /// <summary>cEngineLight_SaveData (map cache) for a light of the map.</summary>
        [System.Serializable]
        public sealed class SaveData
        {
            public bool visible, flickering, flickerFade;
            public Color color, flickerOffColor;
            public float radius, flickerOffRadius;
            public Vector2 flickerOnLength, flickerOffLength, flickerOnFadeLength, flickerOffFadeLength;
            public string flickerOnSound, flickerOffSound, flickerOnPS, flickerOffPS;
        }

        /// <summary>cEngineLight_SaveData::FromLight: a fade (not a flicker one) is taken at its destination.</summary>
        public SaveData Save()
        {
            if (_runtime && _fadeTime > 0f && !_flickering)
            {
                _fadeTime = 0f;
                SetColorAndRadius(_destColor, _destRadius);
            }
            return new SaveData
            {
                visible = Visible,
                flickering = FlickerActive,
                color = _runtime ? (_flickering ? _flickerOnColor : _color) : diffuseColor,
                radius = _runtime ? (_flickering ? _flickerOnRadius : _radius) : radius,
                flickerOffColor = flickerOffColor, flickerOffRadius = flickerOffRadius,
                flickerOnLength = flickerOnLength, flickerOffLength = flickerOffLength,
                flickerFade = flickerFade, flickerOnFadeLength = flickerOnFadeLength, flickerOffFadeLength = flickerOffFadeLength,
                flickerOnSound = flickerOnSound, flickerOffSound = flickerOffSound, flickerOnPS = flickerOnPS, flickerOffPS = flickerOffPS,
            };
        }

        /// <summary>cEngineLight_SaveData::ToLight.</summary>
        public void Restore(SaveData d)
        {
            SetVisible(d.visible);
            SetColor(d.color);
            SetRadius(d.radius);
            flickerOffColor = d.flickerOffColor;
            flickerOffRadius = d.flickerOffRadius;
            flickerOnLength = d.flickerOnLength;
            flickerOffLength = d.flickerOffLength;
            flickerFade = d.flickerFade;
            flickerOnFadeLength = d.flickerOnFadeLength;
            flickerOffFadeLength = d.flickerOffFadeLength;
            flickerOnSound = d.flickerOnSound;
            flickerOffSound = d.flickerOffSound;
            flickerOnPS = d.flickerOnPS;
            flickerOffPS = d.flickerOffPS;
            SetFlickerActive(d.flickering);
        }

        readonly System.Collections.Generic.List<(AmnesiaBillboard billboard, Color baseColor)> _billboards = new();

        /// <summary>iLight::AttachBillboard: the billboard's color becomes its base color × the light color (alpha 1).</summary>
        public void AttachBillboard(AmnesiaBillboard billboard)
        {
            _billboards.Add((billboard, billboard.color));
            billboard.SetColor(billboard.color * new Color(CurrentColor.r, CurrentColor.g, CurrentColor.b, 1f));
        }

        void SetColorAndRadius(Color color, float lightRadius)
        {
            _color = color;
            _radius = lightRadius;
            Apply(_color, _radius);
            // iLight::OnSetDiffuse
            foreach (var (billboard, baseColor) in _billboards)
                if (billboard != null) billboard.SetColor(baseColor * new Color(color.r, color.g, color.b, 1f));
        }

        /// <summary>iLight::UpdateLight, every world step.</summary>
        public void UpdateLight(float timeStep)
        {
            if (!_runtime) return;

            if (_fadeTime > 0f)
            {
                SetColorAndRadius(_color + _colorAdd * timeStep, _radius + _radiusAdd * timeStep);
                _fadeTime -= timeStep;
                if (_fadeTime <= 0f)
                {
                    _fadeTime = 0f;
                    SetColorAndRadius(_destColor, _destRadius);
                }
            }

            if (_flickering && _fadeTime <= 0f)
            {
                if (_flickerOn)
                {
                    if (_flickerTime >= _flickerStateLength)
                    {
                        _flickerOn = false;
                        if (!flickerFade) SetColorAndRadius(flickerOffColor, flickerOffRadius);
                        else FadeTo(flickerOffColor, flickerOffRadius, HplMath.RandRectf(flickerOffFadeLength.x, flickerOffFadeLength.y));
                        PlayFlickerSound("FlickerOff", flickerOffSound);
                        CreateFlickerParticles(flickerOffPS);

                        _flickerTime = 0f;
                        _flickerStateLength = HplMath.RandRectf(flickerOffLength.x, flickerOffLength.y);
                    }
                }
                else if (_flickerTime >= _flickerStateLength)
                {
                    _flickerOn = true;
                    if (!flickerFade) SetColorAndRadius(_flickerOnColor, _flickerOnRadius);
                    else FadeTo(_flickerOnColor, _flickerOnRadius, HplMath.RandRectf(flickerOnFadeLength.x, flickerOnFadeLength.y));
                    PlayFlickerSound("FlickerOn", flickerOnSound);
                    CreateFlickerParticles(flickerOnPS);
                    _flickerTime = 0f;
                    _flickerStateLength = HplMath.RandRectf(flickerOnLength.x, flickerOnLength.y);
                }
                _flickerTime += timeStep;
            }
        }

        /// <summary>The map's sound handler (set by the map) for the flicker sounds.</summary>
        [System.NonSerialized] public RTXMnesia.Sound.HplSoundHandler sound;

        /// <summary>iLight::UpdateLogic: a flicker on / off sound at the light.</summary>
        void PlayFlickerSound(string name, string file)
        {
            if (string.IsNullOrEmpty(file) || sound == null) return;
            var s = sound.CreateSoundEntity(name, file, true);
            if (s == null) return;
            s.IsSaved = false;
            s.Position = transform.position;
        }

        /// <summary>The map's particle systems (set by the map) for the flicker particles.</summary>
        [System.NonSerialized] public RTXMnesia.Particles.HplParticleWorld particles;

        /// <summary>iLight::OnFlickerOn / OnFlickerOff: a particle system "&lt;light&gt;_PS" at the light's matrix.</summary>
        void CreateFlickerParticles(string file)
        {
            if (string.IsNullOrEmpty(file) || particles == null) return;
            var ps = particles.Create(name + "_PS", file);
            // HPL2 lights look down their local -Z; the HDRP light is turned to +Z.
            if (ps != null) ps.LocalMatrix = Matrix4x4.TRS(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f), Vector3.one);
        }
    }
}
