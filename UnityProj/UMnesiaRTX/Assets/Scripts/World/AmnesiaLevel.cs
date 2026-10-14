using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>Root of an imported level scene.</summary>
    public class AmnesiaLevel : MonoBehaviour
    {
        [Tooltip("Map file relative to the game folder, e.g. maps/main/ch01/00_rainy_hall.map")]
        public string mapFile;
    }
}
