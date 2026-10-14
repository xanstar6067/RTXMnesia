using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// Brightness conversion HPL2 → HDRP.
    ///
    /// HPL2 has no physical units: a pixel lit by a light is albedo × light color × falloff(d / radius) × N·L,
    /// written to the screen as is (deferred_light_frag.glsl). The falloff maps used by Amnesia stay at ~1
    /// for most of the radius and drop to 0 at its end. HDRP lights are physical (1/d², candela) and the
    /// screen value is radiance × exposure, exposure = 1 / (1.2 · 2^EV100).
    ///
    /// Levels use a fixed exposure (<see cref="ExposureEV100"/>); what HPL2 writes straight to the screen and HDRP
    /// exposes (Lit emission, fog color) is multiplied by <see cref="UnitRadiance"/> so it shows with the original
    /// value. HDRP/Unlit color is not exposed and is used as is. Lights match the original at <see cref="ReferenceDistance"/> × radius:
    /// closer they are brighter, further dimmer (1/d²). Tuning: change the constants, re-run the material
    /// import (emission) and the level import.
    /// </summary>
    public static class HplLighting
    {
        public const float ExposureEV100 = 3f;

        /// <summary>Fraction of the light radius where HDRP and HPL2 brightness are equal.</summary>
        public const float ReferenceDistance = 0.5f;

        /// <summary>Radiance (nits) that ends up as screen value 1 under the level exposure.</summary>
        public static float UnitRadiance => 1.2f * Mathf.Pow(2f, ExposureEV100);

        /// <summary>
        /// Candela for an HPL2 light of this radius and color 1. Lambert: L = albedo/π · I/d² · N·L;
        /// on screen ×1/UnitRadiance; equal to albedo · N·L at d = radius × ReferenceDistance.
        /// </summary>
        public static float Candela(float radius)
        {
            float d = radius * ReferenceDistance;
            return Mathf.PI * d * d * UnitRadiance;
        }

        /// <summary>An HPL2 screen-space color (gamma, may exceed 1) as HDRP linear radiance.</summary>
        public static Color Radiance(Color hplColor)
        {
            Color linear = hplColor.linear;
            float k = UnitRadiance;
            return new Color(linear.r * k, linear.g * k, linear.b * k, hplColor.a);
        }
    }
}
