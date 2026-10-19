using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        public const string MaterialRoot = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplEnhancedMaterials";
        public static string MaterialPathFor(string relative) => MaterialRoot + "/" + relative;
        public const string CatalogPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + HplTextureSet.ResourceName + ".asset";
        public const string Stone = "static_objects/castlebase/wall/castlebase_wall";
        public const string Experiment = "pbrify-x4-20261019-181639";
        public const float HeightCm = 2f;

        [Serializable] public class MapFile { public string file, sha256; public int[] size; }
        [Serializable] public class MapFiles { public MapFile albedo, normal_ogl, roughness, height, alpha; }
        [Serializable] public class BatchResult
        {
            public int version;
            public string texture, input_sha256, workflow_sha256, normal_convention;
            public string[] materials;
            public MapFiles files;
            public string alpha_mode;
        }
        [Serializable] public class PreparedMaps
        {
            public string signature, sourceSignature, result;
            public int width, height;
            public bool cutout, useHeight;
            public string[] files;
        }

        static string RemixRoot => Path.GetFullPath(Path.Combine(AmnesiaPaths.SourceRoot, "..", "sourceRTXRemix"));
        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static string Under(string root, string relative)
        {
            string path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Path outside resource root: " + relative);
            return path;
        }

        /// <summary>Snapshot of completed result.json files; a running worker may keep producing later results.</summary>
        [MenuItem("Tools/RTXMnesia/Import/Enhanced textures: all completed PBRify sets")]
        public static void ConvertReadyBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int snapshotArg = Array.IndexOf(args, "-enhancedResults");
            var results = snapshotArg >= 0 ? File.ReadAllLines(args[snapshotArg + 1]) :
                Directory.GetFiles(RemixRoot, "result.json", SearchOption.AllDirectories)
                    .Where(p => Path.GetFileName(Path.GetDirectoryName(p)) is "pbrify-x4-batch-v1" or "pbrify-alpha-x4-batch-v1")
                    .OrderBy(p => p, StringComparer.Ordinal).ToArray();
            int sets = 0, pairs = 0;
            var cutoutInputs = new HashSet<string>(results.Select(p => JsonUtility.FromJson<BatchResult>(File.ReadAllText(p)))
                .Where(r => r.alpha_mode == "source-nearest").Select(r => r.texture), StringComparer.Ordinal);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string file in results)
            {
                Under(RemixRoot, Path.GetRelativePath(RemixRoot, file));
                var result = JsonUtility.FromJson<BatchResult>(File.ReadAllText(file));
                if (result.version != 1 || result.files == null || result.materials == null || result.materials.Length == 0 ||
                    !(result.normal_convention ?? "").StartsWith("PBRify_NormalV3", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unsupported batch result: " + file);
                string folder = Path.GetDirectoryName(file);
                bool cutout = result.alpha_mode == "source-nearest";
                var maps = new[] { result.files.albedo, result.files.normal_ogl, result.files.roughness, result.files.height }
                    .Concat(cutout ? new[] { result.files.alpha } : Array.Empty<MapFile>()).ToArray();
                foreach (var map in maps)
                {
                    if (map == null || map.size == null || map.size.Length != 2 || map.size.Any(n => n <= 0 || n > 4096) ||
                        !map.size.SequenceEqual(maps[0].size) || Hash(Under(folder, map.file)) != map.sha256)
                        throw new InvalidOperationException("Invalid dimensions/hash: " + file + " / " + map?.file);
                }
                string input = Under(AmnesiaPaths.ToAbsolute(AmnesiaPaths.TexturesRoot), result.texture);
                if (Hash(input) != result.input_sha256) throw new InvalidOperationException("Changed classic albedo: " + result.texture);
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AmnesiaPaths.TexturesRoot + "/" + result.texture);
                foreach (string relative in result.materials)
                {
                    Under(AmnesiaPaths.ToAbsolute(AmnesiaPaths.MaterialsRoot), relative);
                    var classic = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + relative);
                    if (classic == null || classic.shader.name != "HDRP/Lit" || classic.GetFloat("_SurfaceType") != 0 ||
                        (classic.GetFloat("_AlphaCutoffEnable") > 0) != cutout || classic.GetTexture("_BaseColorMap") != albedo)
                        throw new InvalidOperationException("Classic material no longer matches batch input: " + relative);
                    var src = AmnesiaMaterial.Load(Under(AmnesiaPaths.SourceRoot, relative));
                    if (src.Type != AmnesiaMaterial.Kind.Solid || (cutout && classic.GetFloat("_AlphaCutoff") != 0.5f))
                        throw new InvalidOperationException("Material requires separate PBR calibration: " + relative);
                }
                if (!result.texture.EndsWith(".png", StringComparison.Ordinal) || !result.texture.Contains("__albedo")) throw new InvalidOperationException("Invalid albedo path");
                string textureStem = result.texture.Substring(0, result.texture.Length - 4).Replace("__albedo", "");
                if (!cutout && cutoutInputs.Contains(result.texture)) textureStem += "__opaque";
                if (!File.Exists(AmnesiaPaths.ToAbsolute(Root + "/" + textureStem + "__prepared.json")))
                    throw new InvalidOperationException("Prepare native DDS first: run tools/Prepare-EnhancedTextures.py (Unity closed): " + textureStem);
                ConvertSet(folder, textureStem, maps.Select(m => m.file).ToArray(), result.materials,
                    string.Join("|", maps.Select(m => m.sha256)), maps[0].size);
                sets++;
                pairs += result.materials.Length;
                if (sets % 16 == 0)
                {
                    AssetDatabase.SaveAssets();
                    EditorUtility.UnloadUnusedAssetsImmediate();
                    Debug.Log($"[EnhancedTextures] imported {sets}/{results.Length} sets");
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnhancedTextures] READY PASS: {sets} completed sets, {pairs} material pairs imported; existing catalog entries retained.");
        }

        [MenuItem("Tools/RTXMnesia/Import/Enhanced textures: castlebase wall (PBRify)")]
        public static void ConvertBatch()
        {
            string source = Path.GetFullPath(Path.Combine(AmnesiaPaths.SourceRoot, "..", "sourceRTXRemix", Stone, Experiment));
            var names = new[] { "albedo", "normal_ogl", "roughness", "height" }.Select(r => "castlebase_wall__" + r + ".png").ToArray();
            string prepared = AmnesiaPaths.ToAbsolute(Root + "/" + Stone + "__prepared.json");
            string signature = File.Exists(prepared) ? JsonUtility.FromJson<PreparedMaps>(File.ReadAllText(prepared)).sourceSignature :
                string.Join("|", names.Select(n => Hash(Path.Combine(source, n))));
            ConvertSet(source, Stone, names, new[] { Stone + ".mat" }, signature, new[] { 4096, 4096 });
            AssetDatabase.SaveAssets();
            Debug.Log("[EnhancedTextures] Imported castlebase_wall PBRify 4K; classic assets unchanged.");
        }

        static void ConvertSet(string source, string textureStem, string[] names, string[] materials, string hashes, int[] size)
        {
            string output = Root + "/" + textureStem;
            string preparedPath = AmnesiaPaths.ToAbsolute(output + "__prepared.json");
            if (File.Exists(preparedPath))
            {
                var prepared = JsonUtility.FromJson<PreparedMaps>(File.ReadAllText(preparedPath));
                bool signatureMatches = (!string.IsNullOrEmpty(prepared.sourceSignature) && prepared.sourceSignature == hashes) || (string.IsNullOrEmpty(prepared.sourceSignature) &&
                    prepared.signature == Hash(Under(RemixRoot, Path.GetRelativePath(RemixRoot, prepared.result))) + "|dds-pbrify-v3");
                if (!signatureMatches || !(prepared.signature ?? "").EndsWith("|dds-pbrify-v3", StringComparison.Ordinal) ||
                    prepared.files == null || prepared.files.Length != 4 || prepared.width != size[0] || prepared.height != size[1])
                    throw new InvalidOperationException("Prepared DDS out of date; run tools/Prepare-EnhancedTextures.py: " + textureStem);
                var native = new Texture2D[4];
                for (int i = 0; i < native.Length; i++)
                {
                    if (string.IsNullOrEmpty(prepared.files[i])) continue;
                    string absolute = Under(AmnesiaPaths.ToAbsolute(Root), Path.GetRelativePath(AmnesiaPaths.ToAbsolute(Root), prepared.files[i]));
                    string path = Root + "/" + Path.GetRelativePath(AmnesiaPaths.ToAbsolute(Root), absolute).Replace('\\', '/');
                    var importer = AssetImporter.GetAtPath(path) as IHVImageFormatImporter;
                    if (importer == null) throw new InvalidOperationException("Expected native DDS importer: " + path);
                    if (EnhancedDdsImportRules.Configure(importer)) importer.SaveAndReimport();
                    native[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    var expected = i == 1 ? TextureFormat.BC5 : i == 3 ? TextureFormat.BC4 : TextureFormat.BC7;
                    if (native[i] == null || native[i].width != size[0] || native[i].height != size[1] || native[i].format != expected || !native[i].streamingMipmaps)
                        throw new InvalidOperationException("DDS format/size/streaming mismatch: " + path);
                }
                foreach (string relative in materials) BuildMaterial(relative, native);
                // Keep earlier generated PNGs too: they are outside Resources and no longer enter the player.
                // Source PNGs and original game assets are never deleted or modified.
                return;
            }
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(output)));
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(CatalogPath)));
            AssetDatabase.Refresh();
            string stamp = AmnesiaPaths.ToAbsolute(output + "__import.txt");
            string signature = "pbrify-import-v2|" + hashes;
            string[] paths = new[] { "albedo", "normal", "mask", "height" }.Select(r => output + "__" + r + ".png").ToArray();
            bool cached = File.Exists(stamp) && File.ReadAllText(stamp) == signature && paths.All(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p) != null);
            if (!cached)
            {
                // Raw byte values: Comfy LoadImage reads sRGB PNG without decoding, and the workflow's
                // SaveTexture(linear) writes these values without a transfer function. Albedo remains sRGB.
                string albedoPath = output + "__albedo.png";
                File.Copy(Path.Combine(source, names[0]), AmnesiaPaths.ToAbsolute(albedoPath), true);
                string normalPath = output + "__normal.png";
                File.Copy(Path.Combine(source, names[1]), AmnesiaPaths.ToAbsolute(normalPath), true);

                var roughness = Read(Path.Combine(source, names[2]));
                var height = Read(Path.Combine(source, names[3]));
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
                Import(albedoPath, true);
                Import(normalPath, false, true);
                Import(output + "__mask.png", false);
                Import(output + "__height.png", false);
                File.WriteAllText(stamp, signature);
            }
            var textures = paths.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
            if (textures.Any(t => t.width != size[0] || t.height != size[1]))
                throw new InvalidOperationException("Imported PBR map dimensions disagree: " + textureStem);
            foreach (string relative in materials) BuildMaterial(relative, textures);
        }

        static void BuildMaterial(string relative, Texture2D[] textures)
        {
            var classic = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + relative);
            if (classic == null) throw new InvalidOperationException("Missing classic material: " + relative);
            var src = AmnesiaMaterial.Load(Under(AmnesiaPaths.SourceRoot, relative));
            string materialPath = MaterialPathFor(relative);
            EnsureAssetFolder(Path.GetDirectoryName(materialPath).Replace('\\', '/'));
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            string oldPath = Root + "/" + relative;
            if (material == null && File.Exists(AmnesiaPaths.ToAbsolute(oldPath)))
            {
                string error = AssetDatabase.MoveAsset(oldPath, materialPath);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            }
            if (material == null)
            {
                material = new Material(Shader.Find("HDRP/Lit"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            // Preserve emission, UV mapping, culling and other non-PBR features of each individual material.
            material.CopyPropertiesFromMaterial(classic);
            material.name = Path.GetFileNameWithoutExtension(relative) + " PBRify";
            bool metal = (src.PhysicsMaterial ?? "").StartsWith("Metal", StringComparison.OrdinalIgnoreCase);
            material.SetFloat("_MaterialID", metal ? classic.GetFloat("_MaterialID") : (float)MaterialId.LitStandard);
            material.SetTexture("_BaseColorMap", textures[0]);
            material.SetTexture("_NormalMap", textures[1]);
            material.SetFloat("_NormalScale", 1f);
            material.SetTexture("_MaskMap", textures[2]);
            if (!metal) material.SetTexture("_SpecularColorMap", null);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_MetallicRemapMin", 0f);
            material.SetFloat("_MetallicRemapMax", 0f);
            material.SetFloat("_AORemapMin", 1f);
            material.SetFloat("_AORemapMax", 1f);
            material.SetFloat("_SmoothnessRemapMin", 0f);
            material.SetFloat("_SmoothnessRemapMax", 1f);
            bool useHeight = textures[3] != null && src.Texture("Height") != null && !string.Equals(src.PhysicsMaterial, "Glass", StringComparison.OrdinalIgnoreCase) && classic.GetFloat("_AlphaCutoffEnable") == 0;
            float heightCm = (src.PhysicsMaterial ?? "").StartsWith("Wood", StringComparison.OrdinalIgnoreCase) ? 0.5f : HeightCm;
            material.SetTexture("_HeightMap", useHeight ? textures[3] : null);
            material.SetFloat("_DisplacementMode", useHeight ? 2f : 0f);
            material.SetFloat("_HeightPoMAmplitude", useHeight ? heightCm : 0f);
            material.SetFloat("_HeightAmplitude", useHeight ? heightCm * 0.01f : 0f);
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
            entries.Add(new HplTextureSet.Entry { classic = classic, enhancedResource = "HplEnhancedMaterials/" + Path.ChangeExtension(relative, null).Replace('\\', '/') });
            catalog.entries = entries.ToArray();
            EditorUtility.SetDirty(catalog);
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

        static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
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
