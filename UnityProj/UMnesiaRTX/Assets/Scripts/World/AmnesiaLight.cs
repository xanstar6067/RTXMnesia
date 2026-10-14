using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// An HPL2 point or spot light. Keeps the original parameters, because level scripts work in HPL2 terms
    /// (FadeLightTo(color, radius, time)), and drives the HDRP light from them via <see cref="HplLighting"/>.
    /// Flicker parameters are stored for later; the light is static for now.
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

        [Header("Flicker (stored, not simulated yet)")]
        public bool flickerActive;
        public Color flickerOffColor = Color.black;
        public float flickerOffRadius;
        public Vector2 flickerOnLength;
        public Vector2 flickerOffLength;
        public bool flickerFade;
        public Vector2 flickerOnFadeLength;
        public Vector2 flickerOffFadeLength;
        public string flickerOnSound, flickerOffSound, flickerOnPS, flickerOffPS;

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
    }
}
