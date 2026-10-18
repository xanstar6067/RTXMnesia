using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>Root of an imported level scene.</summary>
    public class AmnesiaLevel : MonoBehaviour
    {
        [Tooltip("Map file relative to the game folder, e.g. maps/main/ch01/00_rainy_hall.map")]
        public string mapFile;

        [Header("Fog of the map (scripts change it at runtime)")]
        public bool fogActive;
        public Color fogColor = Color.white;
        public float fogStart;
        public float fogEnd = 40f;
        public float fogFalloffExp = 1f;
        public bool fogCulling = true;

        [Header("Sky box of the map (cWorld: the background after the fog; scripts change it at runtime)")]
        public bool skyBoxActive;
        public Color skyBoxColor = Color.white;
        [Tooltip("Cube map of the sky box (empty on all maps of the game: only the color)")]
        public string skyBoxTexture = "";
    }
}
