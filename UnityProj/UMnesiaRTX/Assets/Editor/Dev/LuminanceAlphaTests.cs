using System;
using System.IO;
using System.Linq;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Materials whose Alpha unit is a grayscale TGA (pile_of_food, broken/damaged doors, keys, chains, rag piles...):
    /// HPL2 loads such a file as an alpha texture, so its gray value is the cutout. Reimports these materials with
    /// forced textures and checks that the albedo PNG alpha equals the source gray value (nearest sample) and that the
    /// material is alpha-tested. Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.LuminanceAlphaTests.RunBatch
    /// </summary>
    public static class LuminanceAlphaTests
    {
        public static void RunBatch()
        {
            try { Run(); EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogError("[LuminanceAlpha] FAIL: " + e); EditorApplication.Exit(1); }
        }

        [MenuItem("Tools/RTXMnesia/Dev/Reimport grayscale-alpha cutouts")]
        public static void Run()
        {
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            var pairs = Directory.GetFiles(AmnesiaPaths.SourceRoot, "*.mat", SearchOption.AllDirectories)
                .Select(f => (mat: f, src: AmnesiaMaterial.Load(f)))
                .Where(p => p.src.Texture("Alpha") != null && p.src.Texture("Diffuse") != null)
                .Select(p => (p.mat, alpha: index.Find(p.src.Texture("Alpha"), p.mat), diffuse: index.Find(p.src.Texture("Diffuse"), p.mat)))
                .Where(p => p.alpha != null && p.diffuse != null && p.alpha.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) &&
                            ImageLoader.Load(p.alpha).Luminance)
                .OrderBy(p => p.mat, StringComparer.Ordinal).ToList();
            if (pairs.Count == 0) throw new InvalidOperationException("no grayscale-alpha materials found");
            Debug.Log($"[LuminanceAlpha] {pairs.Count} materials with a grayscale TGA Alpha unit");

            MaterialConverter.ConvertMaterials(pairs.Select(p => p.mat).ToList(), true, "Grayscale TGA alpha cutouts");
            AssetDatabase.SaveAssets();

            foreach (var (mat, alpha, diffuse) in pairs)
            {
                string rel = AmnesiaPaths.ToSourceRelative(mat);
                var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(mat));
                if (m == null || m.GetFloat("_AlphaCutoffEnable") < 0.5f) throw new InvalidOperationException(rel + ": not alpha-tested");

                string png = AmnesiaPaths.ToAbsolute(TextureConverter.OutputAssetPath(diffuse, TextureRole.Albedo, alpha));
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    tex.LoadImage(File.ReadAllBytes(png));
                    Color32[] px = tex.GetPixels32();  // bottom row first
                    ImageLoader.FlipRows(px, tex.width, tex.height);
                    var src = ImageLoader.Load(alpha);
                    int mismatched = 0, cut = 0;
                    for (int y = 0; y < tex.height; y++)
                        for (int x = 0; x < tex.width; x++)
                        {
                            byte want = src.Faces[0][(y * src.Height / tex.height) * src.Width + x * src.Width / tex.width].r;
                            byte got = px[y * tex.width + x].a;
                            if (got != want) mismatched++;
                            if (got < 128) cut++;
                        }
                    if (mismatched > 0) throw new InvalidOperationException($"{rel}: {mismatched} albedo alpha texels differ from the gray TGA");
                    Debug.Log($"[LuminanceAlpha] ok: {rel}: cut {100.0 * cut / px.Length:F1}% ({Path.GetFileName(alpha)})");
                }
                finally { UnityEngine.Object.DestroyImmediate(tex); }
            }
            Debug.Log($"[LuminanceAlpha] PASS: {pairs.Count} materials");
        }
    }
}
