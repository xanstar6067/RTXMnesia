using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System;

namespace RTXMnesia.EditorDev
{
    public static class EnhancedCompressionProbe
    {
        public static void RunGpuBatch()
        {
            RunBatch();
            Texture2D a = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AmnesiaImported/Enhanced/static_objects/castlebase/wall/castlebase_wall__albedo.png");
            Texture2D b = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
            Color[] Read(Texture texture)
            {
                var rt = RenderTexture.GetTemporary(512, 512, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, rt);
                var previous = RenderTexture.active;
                RenderTexture.active = rt;
                var image = new Texture2D(512, 512, TextureFormat.RGBAFloat, false, true);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                var colors = image.GetPixels();
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                RenderTexture.ReleaseTemporary(rt);
                return colors;
            }
            var x = Read(a); var y = Read(b); double same = 0, flip = 0;
            for (int j = 0; j < 512; j++) for (int i = 0; i < 512; i++)
            {
                var c = x[j * 512 + i]; var d = y[j * 512 + i]; var f = y[(511 - j) * 512 + i];
                same += Math.Abs(c.r - d.r) + Math.Abs(c.g - d.g) + Math.Abs(c.b - d.b);
                flip += Math.Abs(c.r - f.r) + Math.Abs(c.g - f.g) + Math.Abs(c.b - f.b);
            }
            Debug.Log($"[CompressionProbe] GPU same={same / (512*512*3):F6}, flipped={flip / (512*512*3):F6}");
            bool mismatch = same / (512 * 512 * 3) > 0.006 || flip <= same;
            foreach (string role in new[] { "normal", "mask", "height" })
            {
                string stem = "Assets/AmnesiaImported/Enhanced/static_objects/castlebase/wall/castlebase_wall__" + role;
                x = Read(AssetDatabase.LoadAssetAtPath<Texture2D>(stem + ".png"));
                y = Read(AssetDatabase.LoadAssetAtPath<Texture2D>(stem + ".dds"));
                same = 0; flip = 0;
                double Difference(Color c, Color d) => role == "normal" ? (Math.Abs(c.r - d.r) + Math.Abs(c.g - d.g)) / 2 :
                    role == "mask" ? Math.Abs(c.a - d.a) : Math.Abs(c.r - d.r);
                for (int j = 0; j < 512; j++) for (int i = 0; i < 512; i++)
                {
                    same += Difference(x[j * 512 + i], y[j * 512 + i]);
                    flip += Difference(x[j * 512 + i], y[(511 - j) * 512 + i]);
                }
                Debug.Log($"[CompressionProbe] {role}: GPU same={same / (512*512):F6}, flipped={flip / (512*512):F6}");
                mismatch |= same / (512 * 512) > 0.01 || flip <= same;
            }
            if (mismatch) throw new InvalidOperationException("Native DDS differs from the verified PNG orientation/colour space");
            Debug.Log("[CompressionProbe] GPU PASS: native DDS orientation and sRGB match the verified PNG");
        }
        const string Path = "Assets/AmnesiaImported/Enhanced/static_objects/castlebase/wall/castlebase_wall__albedo.dds";
        public static void RunBatch()
        {
            var watch = Stopwatch.StartNew();
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (IHVImageFormatImporter)AssetImporter.GetAtPath(Path);
            importer.streamingMipmaps = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
            Debug.Log($"[CompressionProbe] {watch.Elapsed.TotalSeconds:F2}s, {texture.width}x{texture.height}, {texture.format}, {texture.mipmapCount} mips, streaming={texture.streamingMipmaps}");
        }
    }
}
