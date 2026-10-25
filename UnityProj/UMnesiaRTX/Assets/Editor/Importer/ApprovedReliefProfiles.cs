using System;
using System.IO;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>Approved relief profiles in their original UVs, never in classic assets: the castle wall and stone floor
    /// from tools/Build-ManualRelief.py when its maps exist, otherwise (and for the boards) the dev_room stand surfaces.</summary>
    public static class ApprovedReliefProfiles
    {
        public const string Wall = "static_objects/castlebase/wall/castlebase_wall.mat";
        public const string Boards = "static_objects/cellarbase/floor/cellarbase_wood_floor.mat";
        public const string Floor = "static_objects/castlebase/castlebase_floor.mat";
        public const string ResourceRoot = "HplApprovedRelief";
        const string Root = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + ResourceRoot;
        public sealed class Data
        {
            public Func<Vector2, float> height;
            public string normal, mask, specular, signature;
            /// <summary>Linear F0 of the specular map (HplReliefSet.Slot.specularF0); 0: the earlier profiles' value.</summary>
            public float specularF0;
            public bool boards;
            /// <summary>The normal map carries the whole shape: vertex normals stay those of the plane.</summary>
            public bool mapNormals;
            /// <summary>The relief continues over corners and UV seams (relief.json over_creases, dungeonbase).</summary>
            public bool overCreases;
            /// <summary>Adaptive mesh tolerance, metres (HplReliefMeshBuilder.Surface.tolerance); 0: uniform spacing.</summary>
            public float tolerance;
        }

        [Serializable] class ManualInfo { public float[] height_encoding_metres; public bool over_creases; public float mesh_tolerance_metres; }

        /// <summary>Maps of tools/Build-ManualRelief.py (shape from the original HPL normal / depth maps).</summary>
        public static string ManualFolder(string relative) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(AmnesiaPaths.SourceRoot)),
            "sourceRTXRemix", Path.ChangeExtension(relative, null), "manual-relief-v1");
        public static bool HasManual(string relative) => relative != Boards && File.Exists(Path.Combine(ManualFolder(relative), "relief.json"));

        public static Data Prepare(string relative)
        {
            if (HasManual(relative)) return PrepareManual(relative);
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
            data.normal = Save(stem, "normal", normal, w, h, true); data.mask = Save(stem, "mask", roughness, w, h, false);
            data.specular = Save(stem, "specular", specular, w, h, false);
            return data;
        }

        // Wall and stone floor: shape from tools/Build-ManualRelief.py. Faces stay at the original plane,
        // joints are recessed, so flat map decals remain visible. Standard F0, unless the maps have specular.png
        // (dungeonbase: the dark stone's specular coverage, F0 = 0.04 x map with LitSpecular, HplReliefSet).
        static Data PrepareManual(string relative)
        {
            string stem = Path.GetFileNameWithoutExtension(relative), folder = ManualFolder(relative);
            var info = JsonUtility.FromJson<ManualInfo>(File.ReadAllText(Path.Combine(folder, "relief.json")));
            float lo = info.height_encoding_metres[0], hi = info.height_encoding_metres[1];
            string signature = "manual-v1";
            bool hasSpecular = File.Exists(Path.Combine(folder, "specular.png"));
            foreach (string file in hasSpecular ? new[] { "relief.json", "height.png", "normal.png", "mask.png", "specular.png" }
                         : new[] { "relief.json", "height.png", "normal.png", "mask.png" })
                signature += "|" + EnhancedReliefConverter.Hash(File.ReadAllBytes(Path.Combine(folder, file)));
            var heightPixels = Read(Path.Combine(folder, "height.png"), null, out int hw, out int hh);
            var metres = new float[heightPixels.Length];
            for (int i = 0; i < metres.Length; i++) metres[i] = Mathf.Lerp(lo, hi, heightPixels[i].r / 255f);
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Root));
            var normal = Read(Path.Combine(folder, "normal.png"), null, out int w, out int h);
            var mask = Read(Path.Combine(folder, "mask.png"), null, out int mw, out int mh);
            if (mw != w || mh != h) throw new InvalidOperationException("Manual normal/mask dimensions differ: " + relative);
            // A specular coverage map of an earlier approved profile is no longer referenced.
            string oldSpecular = Root + "/" + stem + "_specular.png";
            if (!hasSpecular && AssetDatabase.LoadAssetAtPath<Texture2D>(oldSpecular) != null) AssetDatabase.DeleteAsset(oldSpecular);
            string specular = null;
            if (hasSpecular)
            {
                var pixels = Read(Path.Combine(folder, "specular.png"), null, out int sw, out int sh);
                if (sw != w || sh != h) throw new InvalidOperationException("Manual normal/specular dimensions differ: " + relative);
                specular = Save(stem, "specular", pixels, w, h, false);
            }
            return new Data
            {
                signature = signature, mapNormals = true, overCreases = info.over_creases, tolerance = info.mesh_tolerance_metres,
                // LoadImage rows start at the bottom, as UV v: sample directly, repeating like the material.
                height = uv =>
                {
                    float x = Mathf.Repeat(uv.x, 1) * hw - .5f, y = Mathf.Repeat(uv.y, 1) * hh - .5f;
                    int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
                    float At(int a, int b) => metres[((b % hh + hh) % hh) * hw + (a % hw + hw) % hw];
                    return Mathf.Lerp(Mathf.Lerp(At(ix, iy), At(ix + 1, iy), x - ix), Mathf.Lerp(At(ix, iy + 1), At(ix + 1, iy + 1), x - ix), y - iy);
                },
                normal = Save(stem, "normal", normal, w, h, true),
                mask = Save(stem, "mask", mask, w, h, false),
                specular = specular, specularF0 = specular != null ? .04f : 0,
            };
        }

        static Color32[] Read(string file, string hash, out int width, out int height)
        {
            byte[] bytes = File.ReadAllBytes(file);
            if (hash != null && EnhancedReliefConverter.Hash(bytes) != hash) throw new InvalidOperationException("Changed approved source: " + file);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try { if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidOperationException("Invalid PNG: " + file);
                width = texture.width; height = texture.height; return texture.GetPixels32(); }
            finally { Object.DestroyImmediate(texture); }
        }

        static string Save(string stem, string role, Color32[] pixels, int w, int h, bool isNormal)
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
    }
}
