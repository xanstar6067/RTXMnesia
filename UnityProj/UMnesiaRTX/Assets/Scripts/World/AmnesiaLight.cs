using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// An HPL2 point or spot light. Keeps the original parameters, because level scripts work in HPL2 terms
    /// (FadeLightTo(color, radius, time)), and drives the HDRP light from them via <see cref="HplLighting"/>.
    /// At runtime it does what iLight::UpdateLight does each step: color / radius fades and flickering.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class AmnesiaLight : MonoBehaviour
    {
        [Tooltip("HPL2 DiffuseColor, may exceed 1. Alpha = specular strength (HDRP has no per-light equivalent).")]
        public Color diffuseColor = Color.white;
        [Min(0f)] public float radius = 1f;
        [Tooltip("1D falloff texture over (distance / radius)². Only informative: HDRP uses 1/d² with a range window.")]
        public string falloffMap;
        public string gobo;

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

        // Runtime state (iLight): current color / radius, fade and flicker.
        Color _color, _colorAdd, _destColor, _flickerOnColor;
        float _radius, _radiusAdd, _destRadius, _fadeTime, _flickerOnRadius;
        bool _flickering, _flickerOn;
        float _flickerTime, _flickerStateLength;
        bool _runtime;

        public Color CurrentColor => _runtime ? _color : diffuseColor;
        public float CurrentRadius => _runtime ? _radius : radius;
        public bool Visible => TryGetComponent(out Light l) && l.enabled;

        public void Apply() => Apply(diffuseColor, radius);

        /// <summary>Sets the HDRP light to show an HPL2 light of this color and radius.</summary>
        public void Apply(Color color, float lightRadius)
        {
            var target = GetComponent<Light>();
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
        }

        /// <summary>Start of the map: current values from the map data, flicker as iLight::SetFlicker leaves it.</summary>
        public void InitRuntime()
        {
            _runtime = true;
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
            if (TryGetComponent(out Light l)) l.enabled = visible;
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
                        PlayFlickerSound("FlickerOff", flickerOffSound); // FlickerOffPS: with the particle converter

                        _flickerTime = 0f;
                        _flickerStateLength = HplMath.RandRectf(flickerOffLength.x, flickerOffLength.y);
                    }
                }
                else if (_flickerTime >= _flickerStateLength)
                {
                    _flickerOn = true;
                    if (!flickerFade) SetColorAndRadius(_flickerOnColor, _flickerOnRadius);
                    else FadeTo(_flickerOnColor, _flickerOnRadius, HplMath.RandRectf(flickerOnFadeLength.x, flickerOnFadeLength.y));
                    PlayFlickerSound("FlickerOn", flickerOnSound); // FlickerOnPS: with the particle converter
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
    }
}
