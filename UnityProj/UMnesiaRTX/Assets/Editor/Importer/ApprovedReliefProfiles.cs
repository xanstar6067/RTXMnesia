using System;
using System.IO;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>Transfers approved stand surfaces into their original atlas, never into classic assets.</summary>
    public static class ApprovedReliefProfiles
    {
        public const string Wall = "static_objects/castlebase/wall/castlebase_wall.mat";
        public const string Boards = "static_objects/cellarbase/floor/cellarbase_wood_floor.mat";
        public const string ResourceRoot = "HplApprovedRelief";
        const string Root = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + ResourceRoot;
        public sealed class Data
        {
            public Func<Vector2, float> height;
            public string normal, mask, specular, signature;
            public bool boards;
        }
        public static Data Prepare(string relative)
        {
            bool boards = relative == Boards;
            string stem = Path.GetFileNameWithoutExtension(relative);
            var prepared = JsonUtility.FromJson<EnhancedMaterialConverter.PreparedMaps>(File.ReadAllText(AmnesiaPaths.ToAbsolute(
                EnhancedMaterialConverter.Root + "/" + Path.ChangeExtension(relative, null) + "__prepared.json")));
            var result = JsonUtility.FromJson<EnhancedMaterialConverter.BatchResult>(File.ReadAllText(prepared.result));
            string stand = AmnesiaPaths.ImportedAssetsRoot + "/DevRoomRelief/" + stem;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(stand + "_solid.asset");
            if (mesh == null) throw new InvalidOperationException("Build the approved relief stand first: " + stem);
            var data = new Data { boards = boards, signature = (boards ? "approved-v1|" : "approved-v3|") + AssetDatabase.GetAssetDependencyHash(stand + "_solid.asset") + "|" + prepared.sourceSignature };
            var positions = mesh.vertices;
            int size = boards ? 0 : Mathf.RoundToInt(Mathf.Sqrt(mesh.GetSubMesh(0).vertexCount));
            var lo = new Vector2(.025f, .66f); var hi = new Vector2(.68f, .975f);
            data.height = uv =>
            {
                uv = new Vector2(Mathf.Repeat(uv.x, 1), Mathf.Repeat(uv.y, 1));
                if (uv.x < lo.x || uv.x > hi.x || uv.y < lo.y || uv.y > hi.y) return 0;
                float x = (uv.x - lo.x) / (hi.x - lo.x) * (size - 1), y = (uv.y - lo.y) / (hi.y - lo.y) * (size - 1);
                int ix = Mathf.Min((int)x, size - 2), iy = Mathf.Min((int)y, size - 2);
                // Keep the entire slab in front of the original wall plane. Flat legacy dirt decals
                // otherwise cut through recessed mortar and appear as floating polygons in the joints.
                // Original map decals sit up to 10 mm in front of the old wall, not exactly on it.
                return .020f - Mathf.Lerp(Mathf.Lerp(positions[iy * size + ix].z, positions[iy * size + ix + 1].z, x - ix),
                    Mathf.Lerp(positions[(iy + 1) * size + ix].z, positions[(iy + 1) * size + ix + 1].z, x - ix), y - iy);
            };
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Root));
            string source = Path.GetDirectoryName(prepared.result);
            Color32[] Read(string file, string hash, out int width, out int height)
            {
                byte[] bytes = File.ReadAllBytes(file);
                if (hash != null && EnhancedReliefConverter.Hash(bytes) != hash) throw new InvalidOperationException("Changed approved source: " + file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try { if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidOperationException("Invalid PNG: " + file);
                    width = texture.width; height = texture.height; return texture.GetPixels32(); }
                finally { Object.DestroyImmediate(texture); }
            }
            var normal = Read(Path.Combine(source, result.files.normal_ogl.file), result.files.normal_ogl.sha256, out int w, out int h);
            var roughness = Read(Path.Combine(source, result.files.roughness.file), result.files.roughness.sha256, out int rw, out int rh);
            if (w != rw || h != rh) throw new InvalidOperationException("Normal/roughness dimensions differ: " + relative);
            Color32[] micro = null, cavity = null, reflectance = null;
            if (!boards)
            {
                micro = Read(AmnesiaPaths.ToAbsolute(stand + "_micro_normal.png"), null, out int mw, out int mh);
                cavity = Read(AmnesiaPaths.ToAbsolute(stand + "_cavity_mask.png"), null, out int cw, out int ch);
                reflectance = Read(AmnesiaPaths.ToAbsolute(stand + "_cavity_specular.png"), null, out int sw, out int sh);
                if (mw != w || mh != h || cw != w || ch != h || sw != w || sh != h) throw new InvalidOperationException("Stand atlas dimensions differ.");
                data.signature += "|" + AssetDatabase.GetAssetDependencyHash(stand + "_micro_normal.png") + "|" + AssetDatabase.GetAssetDependencyHash(stand + "_cavity_mask.png")
                    + "|" + AssetDatabase.GetAssetDependencyHash(stand + "_cavity_specular.png");
            }
            var specular = new Color32[normal.Length];
            float[] beds = { 0, .130f, .255f, .382f, .505f, .632f, .757f, .882f, 1 };
            float[] ends = { .151f, .976f, .475f, .980f, .305f, .882f, .624f, .125f };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int p = y * w + x;
                var uv = new Vector2((x + .5f) / w, (y + .5f) / h);
                var n = normal[p]; n.g = (byte)(255 - n.g); // NormalV3 image-down G -> Unity, once.
                byte coverage = 255;
                if (!boards && uv.x >= lo.x && uv.x <= hi.x && uv.y >= lo.y && uv.y <= hi.y)
                { normal[p] = micro[p]; roughness[p] = cavity[p]; specular[p] = reflectance[p]; continue; }
                if (boards)
                {
                    float down = 1 - uv.y; int row = 0; while (row < 7 && down > beds[row + 1]) row++;
                    float d = Mathf.Min(Mathf.Abs(down - beds[row]), Mathf.Abs(down - beds[row + 1]));
                    d = Mathf.Min(d, Mathf.Abs(uv.x - ends[row]));
                    float weight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.0011f, .0025f, d));
                    coverage = (byte)Mathf.RoundToInt(weight * 255);
                    n = Color.Lerp(new Color(.5f, .5f, 1, 1), n, weight);
                }
                normal[p] = n;
                roughness[p] = new Color32(0, 255, 255, (byte)Mathf.RoundToInt((255 - roughness[p].r) * coverage / 255f));
                specular[p] = new Color32(coverage, coverage, coverage, 255);
            }
            string Save(string role, Color32[] pixels, bool isNormal)
            {
                string path = Root + "/" + stem + "_" + role + ".png";
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(AmnesiaPaths.ToAbsolute(path), texture.EncodeToPNG()); }
                finally { Object.DestroyImmediate(texture); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = false; importer.flipGreenChannel = false; importer.convertToNormalmap = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false;
                importer.maxTextureSize = 4096; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8; importer.isReadable = false;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                return ResourceRoot + "/" + stem + "_" + role;
            }
            data.normal = Save("normal", normal, true); data.mask = Save("mask", roughness, false); data.specular = Save("specular", specular, false);
            return data;
        }
    }
}
