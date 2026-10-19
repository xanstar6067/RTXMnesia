using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Hpl
{
    /// <summary>Generated, build-included material pairs. Neither side is modified at runtime.</summary>
    public sealed class HplTextureSet : ScriptableObject
    {
        public const string ResourceName = "HplEnhancedTextures";
        [Serializable]
        public struct Entry
        {
            public Material classic;
            public string enhancedResource;
            [UnityEngine.Serialization.FormerlySerializedAs("enhanced")]
            [SerializeField] Material legacyEnhanced;
            public Material enhanced => !string.IsNullOrEmpty(enhancedResource) ? Resources.Load<Material>(enhancedResource) : legacyEnhanced;
            public void ClearLegacyReference() => legacyEnhanced = null;
        }
        public Entry[] entries = Array.Empty<Entry>();
    }

    /// <summary>Replaces shared material slots, keeping exact references for a lossless return to classic.</summary>
    public static class HplEnhancedTextures
    {
        static HplTextureSet s_catalog;
        static readonly Dictionary<Material, Material> s_forward = new();
        static readonly Dictionary<Material, HplTextureSet.Entry> s_entries = new();
        static readonly Dictionary<Texture, List<HplTextureSet.Entry>> s_textureEntries = new();
        static readonly Dictionary<Material, Material> s_reverse = new();
        static readonly List<Material> s_variants = new();
        static bool s_initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Startup()
        {
            Initialize();
            HplGraphics.Changed -= OnChanged;
            HplGraphics.Changed += OnChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            Application.quitting -= Shutdown;
            Application.quitting += Shutdown;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
            OnChanged();
        }

        static void Initialize()
        {
            if (s_initialized) return;
            s_initialized = true;
            s_catalog = Resources.Load<HplTextureSet>(HplTextureSet.ResourceName);
            if (s_catalog == null) return;
            foreach (var entry in s_catalog.entries)
            {
                if (entry.classic == null) continue;
                s_entries[entry.classic] = entry;
                var texture = entry.classic.HasProperty("_BaseColorMap") ? entry.classic.GetTexture("_BaseColorMap") : null;
                if (texture == null) continue;
                if (!s_textureEntries.TryGetValue(texture, out var candidates)) s_textureEntries[texture] = candidates = new();
                candidates.Add(entry);
            }
        }

        static void OnChanged()
        {
            ApplyLoadedScenes(HplGraphics.Textures == 1);
            if (HplGraphics.Textures == 0)
            {
                bool hadEnhanced = s_forward.Count > 0;
                ReleaseMaterials();
                if (hadEnhanced && Application.isPlaying) Resources.UnloadUnusedAssets();
            }
        }
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyScene(scene, HplGraphics.Textures == 1);
        static void OnSceneUnloaded(Scene scene)
        {
            // Drop materials belonging to the previous map before the next map is activated.
            ApplyLoadedScenes(false);
            ReleaseMaterials();
            if (HplGraphics.Textures == 1) ApplyLoadedScenes(true);
            Resources.UnloadUnusedAssets();
        }
        public static int LoadedMaterialCount => s_forward.Count;
        public static Material ClassicMaterial(Material material) => material != null && s_reverse.TryGetValue(material, out var classic) ? classic : material;

        public static Material ResolveMaterial(Material original, bool enhanced)
        {
            if (original == null) return null;
            Initialize();
            if (!enhanced) return ClassicMaterial(original);
            if (s_reverse.ContainsKey(original)) return original;
            if (s_forward.TryGetValue(original, out var cached)) return cached;
            if (!s_entries.TryGetValue(original, out var entry)) return Variant(original) ?? original;
            var replacement = entry.enhanced;
            if (replacement == null) return original;
            s_forward[original] = replacement;
            s_reverse[replacement] = original;
            return replacement;
        }

        public static void ApplyLoadedScenes(bool enhanced)
        {
            // Includes runtime hands and other DontDestroyOnLoad renderers.
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include)) Apply(renderer, enhanced);
            // A stationary path-tracing camera must not keep accumulating the previous material set.
            if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline is
                UnityEngine.Rendering.HighDefinition.HDRenderPipeline hdrp) hdrp.ResetPathTracing();
        }

        public static void ApplyScene(Scene scene, bool enhanced)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) Apply(renderer, enhanced);
        }

        /// <summary>Also available to systems that instantiate renderers after sceneLoaded.</summary>
        public static void Apply(Renderer renderer, bool enhanced)
        {
            if (renderer == null) return;
            Initialize();
            if (s_entries.Count == 0) return;
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                var original = materials[i];
                if (original == null) continue;
                var replacement = ResolveMaterial(original, enhanced);
                if (replacement == null || replacement == original) continue;
                materials[i] = replacement;
                changed = true;
            }
            if (changed) renderer.sharedMaterials = materials;
        }

        // Tinted copies generated by levels and runtime clones still reference the original albedo.
        // Match by texture identity, never by an ambiguous material name.
        static Material Variant(Material original)
        {
            if (s_catalog == null || original.shader == null || original.shader.name != "HDRP/Lit" ||
                    original.GetFloat("_SurfaceType") != 0 || !original.HasProperty("_BaseColorMap")) return null;
            var texture = original.GetTexture("_BaseColorMap");
            if (texture == null || !s_textureEntries.TryGetValue(texture, out var entries)) return null;
            foreach (var entry in entries)
            {
                if (entry.classic == null || entry.classic.GetTexture("_BaseColorMap") != texture ||
                    entry.classic.GetTexture("_HeightMap") != original.GetTexture("_HeightMap") ||
                    entry.classic.GetTexture("_EmissiveColorMap") != original.GetTexture("_EmissiveColorMap") ||
                    entry.classic.GetFloat("_AlphaCutoffEnable") != original.GetFloat("_AlphaCutoffEnable") ||
                    entry.classic.GetFloat("_AlphaCutoff") != original.GetFloat("_AlphaCutoff") ||
                    entry.classic.GetFloat("_DoubleSidedEnable") != original.GetFloat("_DoubleSidedEnable")) continue;
                var enhanced = entry.enhanced;
                if (enhanced == null) continue;
                // Start from the particular clone: emission, tint, UVs and culling can differ even when albedo is shared.
                var copy = new Material(original) { name = original.name + " (enhanced)", hideFlags = HideFlags.HideAndDontSave };
                foreach (string property in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_HeightMap", "_SpecularColorMap" })
                    copy.SetTexture(property, enhanced.GetTexture(property));
                foreach (string property in new[] { "_MaterialID", "_NormalScale", "_Metallic", "_MetallicRemapMin", "_MetallicRemapMax", "_AORemapMin", "_AORemapMax", "_SmoothnessRemapMin", "_SmoothnessRemapMax",
                    "_DisplacementMode", "_HeightPoMAmplitude", "_HeightAmplitude", "_HeightCenter", "_PPDMinSamples", "_PPDMaxSamples" })
                    copy.SetFloat(property, enhanced.GetFloat(property));
                UnityEngine.Rendering.HighDefinition.HDMaterial.ValidateMaterial(copy);
                s_forward[original] = copy;
                s_reverse[copy] = original;
                s_variants.Add(copy);
                return copy;
            }
            return null;
        }

        public static void Shutdown()
        {
            ApplyLoadedScenes(false);
            ReleaseMaterials();
            s_entries.Clear();
            s_textureEntries.Clear();
            s_catalog = null;
            s_initialized = false;
            HplGraphics.Changed -= OnChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Application.quitting -= Shutdown;
        }

        static void ReleaseMaterials()
        {
            foreach (var material in s_variants)
                if (material != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                    else UnityEngine.Object.DestroyImmediate(material);
                }
            s_variants.Clear();
            s_forward.Clear();
            s_reverse.Clear();
        }
#if UNITY_EDITOR
        static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) Shutdown();
        }
#endif
    }
}
