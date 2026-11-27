using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// AffectedByLightLevel of translucent materials (cMaterialType_Translucent::SetupObjectSpecificData): HPL2 adds
    /// for each light reaching the object max color × (1 − distance from the light to the centre of the bounding
    /// volume / radius), a box light its max color, stops at 1, and multiplies the color (and the cube map reflection)
    /// by it. Here per renderer whose RTXMnesia/HplTranslucent material has _HplAffectedByLightLevel, with the classic
    /// lights of the map (<see cref="AmnesiaLight.LightLevelAt"/>: the lights the enhanced lighting adds do not count,
    /// as for the player's light level), as _HplLightLevel in a property block. HPL2 counts the lights of the frame's
    /// render list; a shadow-casting spot here also needs a line of sight (LightLevelAt). Used only by glass
    /// (glass_container_glass); the materials without the flag show the texture as is.
    /// </summary>
    public sealed class HplTranslucentLightLevel : MonoBehaviour
    {
        const string Shader = "RTXMnesia/HplTranslucent";
        static readonly int AffectedId = UnityEngine.Shader.PropertyToID("_HplAffectedByLightLevel");
        static readonly int LevelId = UnityEngine.Shader.PropertyToID("_HplLightLevel");

        readonly List<Renderer> _renderers = new();
        MaterialPropertyBlock _block;

        public IReadOnlyList<Renderer> Renderers => _renderers;

        /// <summary>The light level for the affected ones among <paramref name="renderers"/>; null if there are none.</summary>
        public static HplTranslucentLightLevel Create(IEnumerable<Renderer> renderers, Transform parent)
        {
            List<Renderer> affected = null;
            foreach (var r in renderers)
                if (r != null && Affected(r))
                    (affected ??= new List<Renderer>()).Add(r);
            if (affected == null) return null;
            var go = new GameObject("HplTranslucentLightLevel (runtime)") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            var level = go.AddComponent<HplTranslucentLightLevel>();
            level._renderers.AddRange(affected);
            return level;
        }

        static bool Affected(Renderer r)
        {
            foreach (var m in r.sharedMaterials)
                if (m != null && m.shader != null && m.shader.name == Shader && m.GetFloat(AffectedId) > 0.5f) return true;
            return false;
        }

        /// <summary>The HPL2 light level at <paramref name="center"/> (0..1).</summary>
        public static float Level(Vector3 center)
        {
            float sum = 0f;
            foreach (var light in AmnesiaLight.All)
            {
                if (light == null) continue;
                sum += light.LightLevelAt(center, 0f);
                if (sum >= 1f) return 1f;
            }
            return sum;
        }

        void LateUpdate()
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var r in _renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                r.GetPropertyBlock(_block);
                _block.SetFloat(LevelId, Level(r.bounds.center));
                r.SetPropertyBlock(_block);
            }
        }
    }
}
