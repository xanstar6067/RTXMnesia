using RTXMnesia.Player;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Generated HUD images of the hurt and damage effects (Resources/HplHudEffects.asset, HudEffectsConverter):
    /// cLuxPlayerHurt's graphics/hud/hud_hurt_noise00..07.dds and cLuxPlayerHudEffect's damage splashes
    /// graphics/hud/damage_&lt;bloodsplat|claws|slash&gt;N.tga (N from 0 while the file exists, as LoadDamageData).
    /// </summary>
    public sealed class HplHudEffectAssets : ScriptableObject
    {
        public const string ResourceName = "HplHudEffects";
        public Texture2D[] hurtNoise = new Texture2D[0];
        public Texture2D[] bloodSplat = new Texture2D[0];
        public Texture2D[] claws = new Texture2D[0];
        public Texture2D[] slash = new Texture2D[0];
        [Tooltip("Assets/Shaders/HplGui.shader (referenced here so that builds include it).")]
        public Shader guiShader;

        static HplHudEffectAssets _instance;
        static bool _loaded;

        /// <summary>The imported asset, or null (then the effects draw nothing).</summary>
        public static HplHudEffectAssets Instance
        {
            get
            {
                if (_loaded) return _instance;
                _loaded = true;
                _instance = Resources.Load<HplHudEffectAssets>(ResourceName);
                if (_instance == null) Debug.LogWarning("[RTXMnesia] HUD effect images missing: Tools > RTXMnesia > Import > HUD effect resources");
                return _instance;
            }
        }

        public Texture2D[] DamageImages(DamageType type) => type switch
        {
            DamageType.Claws => claws,
            DamageType.Slash => slash,
            _ => bloodSplat,
        };
    }
}
