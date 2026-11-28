using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Stone of the enhanced (PBRify) set no glossier than the classic one. PBRify roughness centres every material at
    /// smoothness ~0.57 (median of 518 non-matte pairs), while the classic stone of castlebase walls, stairs and pillars is
    /// 0.18–0.24 (Specular power near 0): the GGX peak grew 30–65×, and with RT reflections dry stone shone like wet
    /// (bug report 2026-11-27 18-41-19, 03_archives: «не делать камни зеркально блестящими»; docs/EnhancedTextures.md,
    /// «Блеск камня»). For a stone-like material (<see cref="StoneLike"/>) the smoothness remap max is lowered so that the
    /// F0-weighted mean enhanced smoothness is at most the classic one + <see cref="Margin"/>; the PBRify spread is kept. Metal,
    /// glass, water and wood (lacquer) are not touched, matte-by-rule materials are capped already, a glossier classic
    /// (wet cellar stone) leaves the PBRify gloss as is.
    /// </summary>
    public static class EnhancedStoneGloss
    {
        /// <summary>How much glossier than the classic the enhanced stone may be on average (smoothness).</summary>
        public const float Margin = 0.05f;
        const int Grid = 512;
        static readonly Dictionary<string, Image> s_images = new(StringComparer.OrdinalIgnoreCase);

        static readonly string[] StonePhysics = { "Rock", "Rock_Reverb", "Stone", "Dirt" };
        /// <summary>Stone with the physics Default (walls and ceilings of castlebase, crack decals): by file name.</summary>
        static readonly string[] StoneNames = { "stone", "brick", "rock", "crack", "plaster", "cobble", "castlebase_wall", "castlebase_ceiling", "cave" };
        static readonly string[] NotStoneNames = { "metal", "iron", "window", "wood", "glass" };

        public static bool StoneLike(string relative, string physics)
        {
            physics ??= "";
            string name = Path.GetFileNameWithoutExtension(relative);
            if (NotStoneNames.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase))) return false;
            if (StonePhysics.Any(p => physics.Equals(p, StringComparison.OrdinalIgnoreCase))) return true;
            return physics.Equals("Default", StringComparison.OrdinalIgnoreCase) && relative.Replace('\\', '/').StartsWith("static_objects/", StringComparison.OrdinalIgnoreCase) &&
                   StoneNames.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The smoothness remap max of a stone-like enhanced material (1 — no cap); logs the means.</summary>
        public static float RemapMax(string relative, string physics, Material classic, Texture enhancedMask)
        {
            if (!StoneLike(relative, physics)) return 1f;
            var classicMask = classic.GetTexture("_MaskMap");
            if (classicMask == null || enhancedMask == null) return 1f;
            string classicPath = AssetDatabase.GetAssetPath(classicMask);
            var specular = classic.GetTexture("_SpecularColorMap");
            string specularPath = specular != null ? AssetDatabase.GetAssetPath(specular) : null;
            string roughness = PbrifyRoughnessSource(AssetDatabase.GetAssetPath(enhancedMask));
            if (!classicPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || roughness == null ||
                specularPath != null && !specularPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[EnhancedTextures] stone gloss skipped (no readable source): {classic.name}");
                return 1f;
            }
            var (glossC, glossE) = WeightedMeans(AmnesiaPaths.ToAbsolute(classicPath), roughness, specularPath != null ? AmnesiaPaths.ToAbsolute(specularPath) : null);
            float max = glossE > 1e-3f ? Mathf.Clamp01((glossC + Margin) / glossE) : 1f;
            Debug.Log($"[EnhancedTextures] stone gloss {classic.name}: classic {glossC:0.000}, PBRify {glossE:0.000} (F0-weighted), remap max {max:0.00}");
            return max;
        }

        /// <summary>
        /// Mean classic smoothness (Mask A) and mean PBRify smoothness (1 − roughness R), both weighted by the classic F0 (Specular
        /// map intensity, uniform without one) on a <see cref="Grid"/>² grid of UVs: the classic stone is glossier where its
        /// Specular map is bright (wet spots of dungeonbase), and a plain mean capped them below the classic peak (SurfaceAudit
        /// shine 0.39 at dungeonbase). The same weighting as SurfaceAudit's gloss.
        /// </summary>
        static (float classic, float enhanced) WeightedMeans(string classicMask, string roughness, string specular)
        {
            var mask = Load(classicMask);
            var rough = Load(roughness);
            var spec = specular != null ? Load(specular) : null;
            double wc = 0, we = 0, w = 0;
            for (int j = 0; j < Grid; j++)
                for (int i = 0; i < Grid; i++)
                {
                    float u = (i + 0.5f) / Grid, v = (j + 0.5f) / Grid;
                    float f0 = spec != null ? Sample(spec, u, v, p => (p.r + p.g + p.b) / 765f) : 1f;
                    wc += f0 * Sample(mask, u, v, p => p.a / 255f);
                    we += f0 * Sample(rough, u, v, p => 1f - p.r / 255f);
                    w += f0;
                }
            return w > 1e-6 ? ((float)(wc / w), (float)(we / w)) : (0f, 0f);
        }

        sealed class Image { public int width, height; public Color32[] pixels; }

        static float Sample(Image image, float u, float v, Func<Color32, float> value) =>
            value(image.pixels[Mathf.Min(image.height - 1, (int)(v * image.height)) * image.width + Mathf.Min(image.width - 1, (int)(u * image.width))]);

        /// <summary>A PNG as raw bytes (no colour conversion), cached for the run.</summary>
        static Image Load(string absolute)
        {
            if (s_images.TryGetValue(absolute, out var cached)) return cached;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolute), false))
                    throw new InvalidOperationException("Cannot decode " + absolute);
                var image = new Image { width = texture.width, height = texture.height, pixels = texture.GetPixels32() };
                if (s_images.Count > 8) s_images.Clear();
                s_images[absolute] = image;
                return image;
            }
            finally { Object.DestroyImmediate(texture); }
        }

        /// <summary>The PBRify roughness PNG behind an imported enhanced mask (native DDS via its prepared.json, or the PNG mask itself).</summary>
        static string PbrifyRoughnessSource(string enhancedMaskPath)
        {
            int cut = enhancedMaskPath.LastIndexOf("__mask", StringComparison.OrdinalIgnoreCase);
            if (cut < 0) return null;
            string prepared = AmnesiaPaths.ToAbsolute(enhancedMaskPath.Substring(0, cut) + "__prepared.json");
            if (!File.Exists(prepared)) return null;
            string result = EnhancedMaterialConverter.RebaseRemix(JsonUtility.FromJson<EnhancedMaterialConverter.PreparedMaps>(File.ReadAllText(prepared)).result ?? "");
            if (!File.Exists(result)) return null;
            var batch = JsonUtility.FromJson<EnhancedMaterialConverter.BatchResult>(File.ReadAllText(result));
            string file = batch.files?.roughness?.file;
            if (string.IsNullOrEmpty(file)) return null;
            string path = Path.Combine(Path.GetDirectoryName(result), file);
            return File.Exists(path) ? path : null;
        }

        /// <summary>Applies the stone cap to every imported enhanced material (no PBRify re-import); others keep their remap.</summary>
        [MenuItem("Tools/RTXMnesia/Import/Enhanced textures: stone gloss")]
        public static void RebuildBatch()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
            int capped = 0, stone = 0, seen = 0;
            foreach (var entry in catalog.entries)
            {
                if (entry.classic == null || string.IsNullOrEmpty(entry.enhancedResource)) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + entry.enhancedResource + ".mat");
                if (material == null) continue;
                string relative = AssetDatabase.GetAssetPath(entry.classic).Substring(AmnesiaPaths.MaterialsRoot.Length + 1);
                if (EnhancedMaterialConverter.ClassicSurface(relative)) continue;
                var src = AmnesiaMaterial.Load(Path.Combine(AmnesiaPaths.SourceRoot, relative));
                if (EnhancedMaterialConverter.Matte(relative, src) || !StoneLike(relative, src.PhysicsMaterial)) continue;
                stone++;
                float max = RemapMax(relative, src.PhysicsMaterial, entry.classic, material.GetTexture("_MaskMap"));
                material.SetFloat("_SmoothnessRemapMax", max);
                if (max < 1f) capped++;
                HDMaterial.ValidateMaterial(material);
                EditorUtility.SetDirty(material);
                if (++seen % 50 == 0) { AssetDatabase.SaveAssets(); EditorUtility.UnloadUnusedAssetsImmediate(); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhancedTextures] STONE GLOSS PASS: {stone} stone-like materials, {capped} capped to the classic gloss + {Margin}.");
        }
    }
}
