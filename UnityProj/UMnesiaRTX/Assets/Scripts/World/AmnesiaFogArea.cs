using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// A map FogArea (cFogArea, cEngineFileLoading::LoadFogArea): local fog inside the unit box of this transform
    /// (the scale is the box size), drawn by <see cref="HplFogAreaPass"/>. The loader reads no Active: always on.
    /// </summary>
    [ExecuteAlways]
    public class AmnesiaFogArea : MonoBehaviour
    {
        public Color color = Color.white;
        public float start;
        public float end;
        public float falloffExp;
        public bool showBacksideWhenInside = true;
        public bool showBacksideWhenOutside = true;
        /// <summary>Hidden/RTXMnesia/HplFogArea, referenced so that builds include it.</summary>
        public Shader shader;

        void OnEnable() => HplFogAreaPass.Register(this);

        void OnDisable() => HplFogAreaPass.Unregister(this);
    }
}
