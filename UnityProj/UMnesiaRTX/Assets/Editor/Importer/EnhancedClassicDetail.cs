using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The classic HPL normal map as the HDRP detail normal of an enhanced (PBRify) material. PBRify normals are 3–10× softer
    /// than the classic ones and drop what the albedo does not show: the carved ornaments of chest_of_drawers_nice (classic tilt
    /// 0.113, PBRify 0.042), the pitted face of cellarbase stones (0.257 → 0.080; its mesh relief normal 0.059). Bug reports
    /// 2026-11-27 18-31-06, 18-33-53 (04_wine_cellar_lab), 18-41-19, 18-43-12, 18-48-59 (03_archives); docs/EnhancedTextures.md,
    /// «Классическая деталь нормали».
    ///
    /// Detail map (HDRP layout): R = 0.5 (albedo untouched, scale 0), G = classic normal Y, B = 0.5 (smoothness untouched,
    /// scale 0), A = classic normal X; the Mask B (detail mask) of PBRify and relief masks is 1. HDRP blends it with RNM, so
    /// the tilts add roughly in quadrature: the weight w = √(1 − (t_e / t_c)²) brings the mean tilt back to the classic one
    /// (t — mean |xy| of the tangent normal, both maps box-filtered to the classic resolution, at most 1024). A material whose
    /// PBRify normal is already as bumpy gets no detail map. Mesh relief copies (HplReliefSet) inherit it from the material.
    /// The classic material is not touched.
    /// </summary>
    public static class EnhancedClassicDetail
    {
        public const string DetailRoot = EnhancedMaterialConverter.Root + "/ClassicDetail";
        /// <summary>Below this weight the detail is not worth a texture sample.</summary>
        public const float MinWeight = 0.1f;
        const int MaxMeasure = 1024;

        static readonly Dictionary<string, float> s_tilts = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Sets or clears the classic detail normal of <paramref name="material"/>; returns the weight (0 — none).</summary>
        public static float Apply(Material material, Material classic)
        {
            material.SetTexture("_DetailMap", null);
            material.SetFloat("_DetailNormalScale", 0f);
            var classicNormal = classic.GetTexture("_NormalMap") as Texture2D;
            var enhancedNormal = material.GetTexture("_NormalMap") as Texture2D;
            if (classicNormal == null || enhancedNormal == null || classicNormal == enhancedNormal) return 0f;
            string classicPath = AssetDatabase.GetAssetPath(classicNormal);
            string pbrSource = PbrifyNormalSource(AssetDatabase.GetAssetPath(enhancedNormal));
            if (!classicPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || pbrSource == null)
            {
                Debug.LogWarning($"[EnhancedTextures] classic detail skipped (no readable source): {classic.name}");
                return 0f;
            }
            int width = Mathf.Min(classicNormal.width, MaxMeasure), height = Mathf.Min(classicNormal.height, MaxMeasure);
            float tiltC = Tilt(AmnesiaPaths.ToAbsolute(classicPath), width, height) * classic.GetFloat("_NormalScale");
            float tiltE = Tilt(pbrSource, width, height) * material.GetFloat("_NormalScale");
            float weight = Weight(tiltC, tiltE);
            if (weight < MinWeight) return 0f;
            material.SetTexture("_DetailMap", DetailTexture(classicPath));
            material.SetTextureScale("_DetailMap", Vector2.one);
            material.SetTextureOffset("_DetailMap", Vector2.zero);
            material.SetFloat("_LinkDetailsWithBase", 1f);
            material.SetFloat("_UVDetail", 0f);
            material.SetVector("_UVDetailsMappingMask", new Vector4(1, 0, 0, 0));
            material.SetFloat("_DetailAlbedoScale", 0f);
            material.SetFloat("_DetailSmoothnessScale", 0f);
            material.SetFloat("_DetailNormalScale", weight);
            Debug.Log($"[EnhancedTextures] classic detail {classic.name}: tilt classic {tiltC:0.000}, PBRify {tiltE:0.000}, weight {weight:0.00}");
            return weight;
        }

        public static float Weight(float tiltClassic, float tiltEnhanced)
        {
            if (tiltClassic <= 1e-4f) return 0f;
            float r = Mathf.Clamp01(tiltEnhanced / tiltClassic);
            return Mathf.Sqrt(1f - r * r);
        }

        /// <summary>The PBRify NormalV3 PNG behind an imported enhanced normal (native DDS via its prepared.json, or the PNG itself).</summary>
        static string PbrifyNormalSource(string enhancedNormalPath)
        {
            if (enhancedNormalPath.EndsWith("__normal.png", StringComparison.OrdinalIgnoreCase)) return AmnesiaPaths.ToAbsolute(enhancedNormalPath);
            if (!enhancedNormalPath.EndsWith("__normal.dds", StringComparison.OrdinalIgnoreCase)) return null;
            string prepared = AmnesiaPaths.ToAbsolute(enhancedNormalPath.Substring(0, enhancedNormalPath.Length - "__normal.dds".Length) + "__prepared.json");
            if (!File.Exists(prepared)) return null;
            string result = EnhancedMaterialConverter.RebaseRemix(JsonUtility.FromJson<EnhancedMaterialConverter.PreparedMaps>(File.ReadAllText(prepared)).result ?? "");
            if (!File.Exists(result)) return null;
            var batch = JsonUtility.FromJson<EnhancedMaterialConverter.BatchResult>(File.ReadAllText(result));
            string file = batch.files?.normal_ogl?.file;
            if (string.IsNullOrEmpty(file)) return null;
            string path = Path.Combine(Path.GetDirectoryName(result), file);
            return File.Exists(path) ? path : null;
        }

        /// <summary>Mean |xy| of a tangent normal PNG (raw bytes, RGB), xy box-filtered to width × height.</summary>
        static float Tilt(string absolute, int width, int height)
        {
            string key = absolute + "|" + width + "x" + height;
            if (s_tilts.TryGetValue(key, out float cached)) return cached;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(absolute), false))
                    throw new InvalidOperationException("Cannot decode " + absolute);
                var pixels = texture.GetPixels32();
                int w = texture.width, h = texture.height;
                var sx = new double[width * height];
                var sy = new double[width * height];
                var n = new int[width * height];
                for (int y = 0; y < h; y++)
                {
                    int by = (int)((long)y * height / h) * width;
                    for (int x = 0; x < w; x++)
                    {
                        var p = pixels[y * w + x];
                        int b = by + (int)((long)x * width / w);
                        sx[b] += p.r / 127.5 - 1.0;
                        sy[b] += p.g / 127.5 - 1.0;
                        n[b]++;
                    }
                }
                double sum = 0;
                for (int i = 0; i < n.Length; i++)
                    if (n[i] > 0) sum += Math.Min(1.0, Math.Sqrt(sx[i] * sx[i] + sy[i] * sy[i]) / n[i]);
                float tilt = (float)(sum / n.Length);
                s_tilts[key] = tilt;
                return tilt;
            }
            finally { Object.DestroyImmediate(texture); }
        }

        /// <summary>One detail map per classic normal PNG (shared by every material that uses it); rebuilt when the PNG is newer.</summary>
        static Texture2D DetailTexture(string classicNormalPath)
        {
            string relative = classicNormalPath.Substring(AmnesiaPaths.TexturesRoot.Length + 1);
            string path = DetailRoot + "/" + relative.Substring(0, relative.Length - ".png".Length) + "__detail.png";
            string absolute = AmnesiaPaths.ToAbsolute(path);
            string source = AmnesiaPaths.ToAbsolute(classicNormalPath);
            bool fresh = File.Exists(absolute) && File.GetLastWriteTimeUtc(absolute) >= File.GetLastWriteTimeUtc(source);
            if (!fresh)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(source), false))
                        throw new InvalidOperationException("Cannot decode " + source);
                    var pixels = texture.GetPixels32();
                    // The classic PNG already has Unity's green-up convention (TextureConverter, role Normal).
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(128, pixels[i].g, 128, pixels[i].r);
                    var output = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
                    output.SetPixels32(pixels);
                    output.Apply();
                    File.WriteAllBytes(absolute, output.EncodeToPNG());
                    Object.DestroyImmediate(output);
                }
                finally { Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (Configure(importer)) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static bool Configure(TextureImporter importer)
        {
            var platform = importer.GetPlatformTextureSettings("Standalone");
            if (importer.textureType == TextureImporterType.Default && !importer.sRGBTexture && importer.mipmapEnabled &&
                importer.streamingMipmaps && importer.wrapMode == TextureWrapMode.Repeat && importer.anisoLevel == 8 &&
                importer.alphaSource == TextureImporterAlphaSource.FromInput && platform.overridden &&
                platform.format == TextureImporterFormat.BC7) return false;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone", overridden = true, maxTextureSize = 4096, format = TextureImporterFormat.BC7,
                textureCompression = TextureImporterCompression.CompressedHQ, compressionQuality = 100,
            });
            return true;
        }

        /// <summary>Adds the classic detail to every imported enhanced material (no PBRify re-import); classic-surface ones keep the classic normal anyway.</summary>
        [MenuItem("Tools/RTXMnesia/Import/Enhanced textures: classic detail normals")]
        public static void RebuildBatch()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
            int with = 0, without = 0;
            foreach (var entry in catalog.entries)
            {
                if (entry.classic == null || string.IsNullOrEmpty(entry.enhancedResource)) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + entry.enhancedResource + ".mat");
                if (material == null) continue;
                string relative = AssetDatabase.GetAssetPath(entry.classic).Substring(AmnesiaPaths.MaterialsRoot.Length + 1);
                if (EnhancedMaterialConverter.ClassicSurface(relative)) continue;
                if (Apply(material, entry.classic) > 0f) with++; else without++;
                HDMaterial.ValidateMaterial(material);
                EditorUtility.SetDirty(material);
                if ((with + without) % 50 == 0) { AssetDatabase.SaveAssets(); EditorUtility.UnloadUnusedAssetsImmediate(); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhancedTextures] CLASSIC DETAIL PASS: {with} materials with the classic detail normal, {without} without.");
        }
    }
}
