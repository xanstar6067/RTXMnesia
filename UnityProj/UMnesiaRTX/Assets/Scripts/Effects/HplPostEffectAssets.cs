using UnityEngine;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Generated textures of the game's post effects (Resources/HplPostEffects.asset, PostEffectsConverter):
    /// cLuxPostEffect_Insanity's amplitude maps shaders/posteffect_insanity_ampmap0..2.jpg and zoom map
    /// shaders/posteffect_insanity_zoom.jpg (linear data, no mip maps).
    /// </summary>
    public sealed class HplPostEffectAssets : ScriptableObject
    {
        public const string ResourceName = "HplPostEffects";
        public Texture2D[] insanityAmpMaps = new Texture2D[0];
        public Texture2D insanityZoomMap;
    }
}
