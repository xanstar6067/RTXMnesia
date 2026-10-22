using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// Per-object mesh illumination. Keep the actual material emission in sync, so all HDRP passes (including
    /// ray/path tracing) see an extinguished lamp, rather than leaving a lit shared material behind an override.
    /// Only emissive slots get private copies; source assets and the texture-set catalogue stay unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HplEmission : MonoBehaviour
    {
        static readonly int ColorId = Shader.PropertyToID("_EmissiveColor");
        readonly Dictionary<Material, Material> _copies = new();
        Renderer _renderer;
        Material[] _sources;
        public float Amount { get; private set; } = 1f;
        /// <summary>The renderer's classic material in a slot (its slots hold this component's emission copies).</summary>
        public Material ClassicSource(int slot) => _sources != null && slot < _sources.Length ? _sources[slot] : null;

        public void Initialize(Renderer renderer)
        {
            if (_sources != null) return;
            _renderer = renderer;
            _sources = renderer.sharedMaterials;
            for (int i = 0; i < _sources.Length; i++) _sources[i] = HplEnhancedTextures.ClassicMaterial(_sources[i]);
            hideFlags = HideFlags.DontSave;
            ApplyTextureSet(HplGraphics.Textures == 1);
        }

        public void SetAmount(float amount)
        {
            if (Amount == amount) return;
            Amount = amount;
            foreach (var pair in _copies)
                if (pair.Key != null && pair.Value != null) pair.Value.SetColor(ColorId, pair.Key.GetColor(ColorId) * amount);
        }

        public void ApplyTextureSet(bool enhanced)
        {
            if (_sources == null || _renderer == null) return;
            var materials = _renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < _sources.Length && i < materials.Length; i++)
            {
                var source = HplEnhancedTextures.ResolveMaterial(_sources[i], enhanced);
                var replacement = source;
                if (source != null && source.HasProperty(ColorId) && source.GetColor(ColorId).maxColorComponent > 0)
                {
                    if (!_copies.TryGetValue(source, out replacement) || replacement == null)
                    {
                        replacement = new Material(source) { name = source.name + " (prop emission)", hideFlags = HideFlags.DontSave };
                        replacement.SetColor(ColorId, source.GetColor(ColorId) * Amount);
                        _copies[source] = replacement;
                    }
                }
                if (materials[i] == replacement) continue;
                materials[i] = replacement;
                changed = true;
            }
            if (changed) _renderer.sharedMaterials = materials;
            if (!enhanced)
                foreach (var source in new List<Material>(_copies.Keys))
                    if (System.Array.IndexOf(_sources, source) < 0)
                    {
                        Release(_copies[source]);
                        _copies.Remove(source);
                    }
        }

        void OnDestroy()
        {
            // Also safe when a diagnostic removes the component while leaving its renderer alive.
            if (_renderer != null && _sources != null) _renderer.sharedMaterials = _sources;
            foreach (var copy in _copies.Values)
                Release(copy);
            _copies.Clear();
        }

        static void Release(Material material)
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
