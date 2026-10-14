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
    }
}
