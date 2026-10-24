using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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

        /// <summary>
        /// Rendering layer of the player's hands and hand objects. HPL2's light falloff is almost flat inside the
        /// radius, HDRP's is 1/d²: a hand-held light 10-20 cm from the hand would blow it out when matched at half
        /// its radius. Hand-held lights skip this layer; a copy matched at the hand's distance lights only it.
        /// Map lights light it too.
        /// </summary>
        public const int HandsRenderingLayer = 7;
        public const uint HandsLayerMask = 1u << HandsRenderingLayer;
        /// <summary>Rendering layer of lit candle wax in the enhanced lighting: the light inside the candle lights only it.</summary>
        public const int WaxRenderingLayer = 6;
        public const uint WaxLayerMask = 1u << WaxRenderingLayer;
        /// <summary>The rendering layers HDRP uses for light layers (the low 8; the others are shadow / decal layers).</summary>
        public const uint LightLayersMask = 0xFFu;

        /// <summary>The rendering layers an HDRP light lights (HDAdditionalLightData.lightlayersMask updates HDRP's light data).</summary>
        public static uint GetLightLayers(Light light) =>
            light.TryGetComponent(out HDAdditionalLightData data) ? (uint)data.lightlayersMask : (uint)light.renderingLayerMask;

        public static void SetLightLayers(Light light, uint mask)
        {
            if (light.TryGetComponent(out HDAdditionalLightData data)) data.lightlayersMask = (UnityEngine.Rendering.HighDefinition.RenderingLayerMask)mask;
            else light.renderingLayerMask = (int)mask;
        }

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
            // A pure power, as the screen values of HPL2 are shown (a gamma 2.2 monitor): the piecewise sRGB curve
            // (Color.linear) is 1.6× brighter at 0.1 and 8× at 0.02, which turned dark fog into a grey haze.
            float k = UnitRadiance;
            return new Color(Mathf.Pow(Mathf.Max(hplColor.r, 0f), 2.2f) * k, Mathf.Pow(Mathf.Max(hplColor.g, 0f), 2.2f) * k,
                Mathf.Pow(Mathf.Max(hplColor.b, 0f), 2.2f) * k, hplColor.a);
        }

        /// <summary>
        /// The world fog of HPL2 (deferred_fog_frag.glsl: alpha = ((d - start) / (end - start))^falloff × color.a over the
        /// view depth d, constant color) is drawn by <see cref="World.HplFogAreaPass"/> ("WorldFog" pass of
        /// HplFogArea.shader): HDRP's fog has no start distance and, matched at the end, hazed the whole near range
        /// (16_cistern_entrance: start 20, end 60, opaque at the end: half fog already at 10 m). The HDRP fog of the
        /// level volumes is switched off by an overriding volume of that pass; this only keeps the old call.
        /// </summary>
        public static void ApplyFog(UnityEngine.Rendering.HighDefinition.Fog fog, bool active, Color color, float fogEnd)
        {
            fog.enabled.Override(false);
        }

        /// <summary>The world fog state for <see cref="World.HplFogAreaPass"/>: (start, end - start, falloff exponent, alpha) and the color as radiance.</summary>
        public static bool WorldFogActive { get; private set; }
        public static Vector4 WorldFogParams { get; private set; }
        public static Color WorldFogRadiance { get; private set; }

        /// <summary>
        /// HPL2's fade of translucent objects in fog (deferred_transparent_frag.glsl, UseFog) for HplParticle.shader
        /// (_HplTranslucentFog): the world fog is drawn before them, so their alpha goes to 1 - fog alpha at
        /// ((depth - start) / (end - start))^falloff. Off: no fade.
        /// </summary>
        public static void SetTranslucentFog(bool active, Color color, float start, float end, float falloffExp)
        {
            // The world fog itself (HplFogAreaPass) and the haze the multiply decals keep (HplMultiply.shader, _HplFog:
            // HPL2 fogs after the decals, so they multiply only what is behind the fog): x = opacity at the end, 0 = off.
            WorldFogActive = active;
            WorldFogParams = new Vector4(start, end - start, falloffExp, color.a);
            WorldFogRadiance = Radiance(new Color(color.r, color.g, color.b, 1f));
            Shader.SetGlobalVector(MultiplyFogId, active ? new Vector4(Mathf.Max(color.a, 1e-4f), start, end - start, falloffExp) : Vector4.zero);
            if (active) World.HplFogAreaPass.EnsureVolume();
            Shader.SetGlobalVector(TranslucentFogId, active
                ? new Vector4(start, end - start, 1f - color.a, falloffExp)
                : new Vector4(0f, 1f, 1f, 1f));
            // The water surface mixes to the fog color itself (water_surface_frag.glsl), gamma values as in HPL2.
            Shader.SetGlobalVector(FogColorId, new Vector4(color.r, color.g, color.b, color.a));
        }

        /// <summary>
        /// The camera background: black without a sky box (HPL2 clears to black), else the sky box color. A gamma
        /// color shown as is: the color buffer holds pre-exposed values and HDRP does not expose the clear color.
        /// </summary>
        public static Color BackgroundColor { get; private set; } = Color.black;

        public static void SetBackground(bool skyBoxActive, Color skyBoxColor) =>
            BackgroundColor = skyBoxActive ? new Color(skyBoxColor.r, skyBoxColor.g, skyBoxColor.b, 1f).linear : Color.black;

        static readonly int TranslucentFogId = Shader.PropertyToID("_HplTranslucentFog");
        static readonly int FogColorId = Shader.PropertyToID("_HplFogColor");


        static readonly int MultiplyFogId = Shader.PropertyToID("_HplFog");
    }
}
