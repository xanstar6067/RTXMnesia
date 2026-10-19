using System;
using System.IO;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>PBRify outputs have their own conversion path; original HPL maps/materials stay untouched.</summary>
    public static class EnhancedMaterialConverter
    {
        public const string Root = AmnesiaPaths.ImportedAssetsRoot + "/Enhanced";
        public const string CatalogPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + HplTextureSet.ResourceName + ".asset";
        public const string Stone = "static_objects/castlebase/wall/castlebase_wall";
        public const string Experiment = "pbrify-x4-20261019-181639";
        public const float HeightCm = 2f;

        [MenuItem("Tools/RTXMnesia/Import/Enhanced textures: castlebase wall (PBRify)")]
        public static void ConvertBatch()
        {
            string source = Path.GetFullPath(Path.Combine(AmnesiaPaths.SourceRoot, "..", "sourceRTXRemix", Stone, Experiment));
            string output = Root + "/" + Stone;
            var classic = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + Stone + ".mat");
            if (classic == null) throw new InvalidOperationException("Import the classic castlebase_wall material first.");
            foreach (string role in new[] { "albedo", "normal_ogl", "roughness", "height" })
                if (!File.Exists(Path.Combine(source, "castlebase_wall__" + role + ".png")))
                    throw new FileNotFoundException("Missing PBRify map: " + role, source);
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(output)));
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(CatalogPath)));
            AssetDatabase.Refresh();

            // Raw byte values: Comfy LoadImage reads sRGB PNG without decoding, and the workflow's
            // SaveTexture(linear) writes these values without a transfer function. Albedo remains sRGB.
            string albedoPath = output + "__albedo.png";
            File.Copy(Path.Combine(source, "castlebase_wall__albedo.png"), AmnesiaPaths.ToAbsolute(albedoPath), true);
            string normalPath = output + "__normal.png";
            File.Copy(Path.Combine(source, "castlebase_wall__normal_ogl.png"), AmnesiaPaths.ToAbsolute(normalPath), true);

            var roughness = Read(Path.Combine(source, "castlebase_wall__roughness.png"));
            var height = Read(Path.Combine(source, "castlebase_wall__height.png"));
            try
            {
                if (roughness.width != height.width || roughness.height != height.height)
                    throw new InvalidOperationException("PBR map dimensions disagree.");
                var pixels = roughness.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                    // HDRP Mask: R metallic=0, G AO=1 (no invented AO), B detail mask=1, A smoothness.
                    pixels[i] = new Color32(0, 255, 255, (byte)(255 - pixels[i].r));
                Write(output + "__mask.png", roughness.width, roughness.height, pixels);
                pixels = height.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(pixels[i].r, pixels[i].r, pixels[i].r, 255);
                Write(output + "__height.png", height.width, height.height, pixels);
            }
            finally { Object.DestroyImmediate(roughness); Object.DestroyImmediate(height); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var albedo = Import(albedoPath, true);
            var normal = Import(normalPath, false, true);
            var mask = Import(output + "__mask.png", false);
            var heightMap = Import(output + "__height.png", false);
            if (albedo.width != 4096 || normal.width != albedo.width || mask.width != albedo.width || heightMap.width != albedo.width ||
                albedo.height != 4096 || normal.height != albedo.height || mask.height != albedo.height || heightMap.height != albedo.height)
                throw new InvalidOperationException("Expected four matching 4096 x 4096 PBRify maps.");

            string materialPath = output + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("HDRP/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // Start from HDRP defaults, not from the old specular workflow and its HPL-derived maps.
            var defaults = new Material(Shader.Find("HDRP/Lit"));
            material.CopyPropertiesFromMaterial(defaults);
            Object.DestroyImmediate(defaults);
            material.name = "castlebase_wall PBRify 4K";
            material.SetFloat("_MaterialID", (float)MaterialId.LitStandard);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseColorMap", albedo);
            material.SetTexture("_NormalMap", normal);
            material.SetFloat("_NormalScale", 1f);
            material.SetTexture("_MaskMap", mask);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_SmoothnessRemapMin", 0f);
            material.SetFloat("_SmoothnessRemapMax", 1f);
            material.SetTexture("_HeightMap", heightMap);
            material.SetFloat("_DisplacementMode", 2f); // Pixel displacement; no mesh/collider changes.
            material.SetFloat("_HeightPoMAmplitude", HeightCm);
            material.SetFloat("_HeightAmplitude", HeightCm * 0.01f);
            material.SetFloat("_HeightCenter", 1f);
            material.SetFloat("_PPDMinSamples", 8);
            material.SetFloat("_PPDMaxSamples", 32);
            HDMaterial.ValidateMaterial(material);
            EditorUtility.SetDirty(material);

            var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<HplTextureSet>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            // Upsert this pair, preserving future additions to the catalog.
            var entries = new System.Collections.Generic.List<HplTextureSet.Entry>(catalog.entries);
            entries.RemoveAll(e => e.classic == classic);
            entries.Add(new HplTextureSet.Entry { classic = classic, enhanced = material });
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[EnhancedTextures] Imported castlebase_wall PBRify 4K; classic assets unchanged.");
        }

        static Texture2D Read(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Cannot decode " + path);
            }
            return texture;
        }

        static void Write(string path, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(AmnesiaPaths.ToAbsolute(path), texture.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static Texture2D Import(string path, bool srgb, bool normal = false)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.convertToNormalmap = false;
            // This particular NormalV3 output is named normal_ogl, but numerically has Y-down normals:
            // corr(dHeight/dImageY, normalY) = -0.728 (X = -0.721). Unity needs Y-up, so flip G once.
            // Keep the source PNG intact; do not run it through the original HPL normal converter.
            importer.flipGreenChannel = normal;
            importer.alphaSource = srgb || normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 4096;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Standalone", overridden = true, maxTextureSize = 4096,
                format = normal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7,
                textureCompression = TextureImporterCompression.CompressedHQ,
                compressionQuality = 100,
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
