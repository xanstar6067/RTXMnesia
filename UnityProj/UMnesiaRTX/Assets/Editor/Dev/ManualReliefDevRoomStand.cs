using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Dev;
using RTXMnesia.Importer;
using RTXMnesia.ScriptApi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>dev_room samples of the maps built by tools/Build-ManualRelief.py, next to the relief
    /// stand. Wall, one row above the stand masonry: 4 — 1 cm mesh, 5 — the 5.1 cm spacing the game's
    /// 4 m wall module gets. Behind the stand floors: 6 / 7 — stone floor at 1 cm / 5.1 cm, 8 — planks
    /// with chamfers and deep gaps. The relief stand itself is not rebuilt: its solid meshes feed the
    /// approved game profiles.</summary>
    public static class ManualReliefDevRoomStand
    {
        public const string RootName = "Manual relief samples";
        static readonly string[] OldRootNames = { "Manual wall relief samples" };
        internal const string Folder = AmnesiaPaths.ImportedAssetsRoot + "/DevRoomRelief/Manual";
        const string StandFolder = AmnesiaPaths.ImportedAssetsRoot + "/DevRoomRelief";
        const float Tile = 2.2f, GameSpacing = .051f;
        // Wall stand: island of the atlas, row above the stand's masonry (y 1.55).
        static readonly Vector2 WallLo = new(.025f, .66f), WallHi = new(.68f, .975f);
        const float WallRow = 3.05f, WallDepth = 1.2f, WallCenter = 5.2f;
        // Floor stands lie behind the stand floors (z 3.1), between them and the front wall.
        const float FloorSize = 1.6f, FloorZ = 1.1f, FloorY = .04f, FloorCenter = -2.6f, BoardsCenter = -10.4f;
        static string Renders => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "ManualWall", "DevRoom");

        [MenuItem("Tools/RTXMnesia/Dev Room/Add or update manual relief samples")]
        public static void InstallBatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before updating dev_room.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save edited scenes before updating dev_room.");
            var scene = EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var map = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HplMap>(true)).Single();
            var stand = map.GetComponentInChildren<HplDevReliefStand>(true);
            if (stand == null) throw new InvalidOperationException("Install the relief stand first (Dev Room > Add or update relief stand).");
            Add(map.transform, stand);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[dev_room manual relief] Installed: wall 4/5, stone floor 6/7, boards 8.");
        }

        public static void InstallAndRenderBatch()
        {
            InstallBatch();
            RenderBatch();
        }

        sealed class Builder
        {
            public Transform root;
            public List<HplDevReliefStand.Label> labels;
            public List<Behaviour> lights;
            public void Label(Vector3 position, string text)
            {
                var anchor = new GameObject(text.Split('\n')[0] + " label").transform; anchor.SetParent(root, false);
                anchor.position = position;
                labels.Add(new HplDevReliefStand.Label { anchor = anchor, text = text });
            }
            public void Lamp(string name, Vector3 position, Vector3 target)
            {
                var lamp = new GameObject(name).AddComponent<Light>(); lamp.transform.SetParent(root, false);
                lamp.type = LightType.Spot; lamp.range = 12; lamp.spotAngle = 120;
                lamp.transform.position = position; lamp.transform.LookAt(target);
                lamp.shadows = LightShadows.Soft; lamp.gameObject.AddComponent<HDAdditionalLightData>(); lamp.lightUnit = LightUnit.Candela; lamp.intensity = 100;
                lights.Add(lamp);
            }
            public GameObject Sample(string name, Mesh mesh, Material[] materials, Vector3 position, Quaternion rotation, bool collider)
            {
                var go = new GameObject(name); go.transform.SetParent(root, false);
                go.transform.SetPositionAndRotation(position, rotation);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                // Visual relief is not collision geometry: walkable floors get their outer box only.
                if (collider) { var box = go.AddComponent<BoxCollider>(); box.center = mesh.bounds.center; box.size = mesh.bounds.size + Vector3.forward * .04f; }
                Debug.Log($"[dev_room manual relief] {name}: {mesh.vertexCount} vertices, {Enumerable.Range(0, mesh.subMeshCount).Sum(s => (long)mesh.GetIndexCount(s)) / 3} triangles");
                return go;
            }
        }

        static void Add(Transform mapRoot, HplDevReliefStand stand)
        {
            foreach (string name in OldRootNames.Append(RootName))
            {
                var old = mapRoot.Find(name);
                if (old == null) continue;
                stand.labels = stand.labels.Where(l => l.anchor != null && !l.anchor.IsChildOf(old)).ToArray();
                stand.inspectionLights = stand.inspectionLights.Where(l => l != null && !l.transform.IsChildOf(old)).ToArray();
                Object.DestroyImmediate(old.gameObject);
            }
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Folder));
            var root = new GameObject(RootName).transform; root.SetParent(mapRoot, false);
            var builder = new Builder { root = root, labels = stand.labels.ToList(), lights = stand.inspectionLights.ToList() };
            var standUp = Quaternion.Euler(0, 180, 0);    // the stand convention: relief along local -Z
            var lying = Quaternion.Euler(90, 0, 0);

            // Wall: above stand samples 3 (x 1.3) and 2 (x 3.9).
            float wallHeight = Tile * (WallHi.y - WallLo.y) / (WallHi.x - WallLo.x);
            var wall = ManualMaterial("castlebase_wall", "static_objects/castlebase/wall/castlebase_wall", out var wallInfo, out var wallMaps);
            using (var height = new HeightMap(Path.Combine(wallMaps, "height.png"), wallInfo))
            {
                var flat = Panel(Tile, wallHeight, WallLo, WallHi);
                foreach (var (spacing, x, title, file) in new[] { (.01f, WallCenter - 3.9f, "4 — НОВАЯ КЛАДКА\nсетка 1 см", "wall_fine"),
                    (GameSpacing, WallCenter - 1.3f, "5 — НОВАЯ КЛАДКА\nсетка 5 см, как модуль 4 м в игре", "wall_game") })
                {
                    var mesh = SaveMesh(Displace(flat, height, spacing), file);
                    var go = builder.Sample("castlebase_wall MANUAL " + file, mesh, new[] { wall }, new Vector3(x, WallRow, WallDepth), standUp, false);
                    builder.Label(go.transform.position + Vector3.up * (wallHeight * .5f + .3f), title);
                }
                Object.DestroyImmediate(flat);
            }
            builder.Lamp("Manual wall raking light", new Vector3(WallCenter - 5.8f, 2.9f + (WallRow - 1.55f), 3), new Vector3(WallCenter, WallRow, WallDepth));

            // Stone floor: behind stand samples 3 (x -6.5) and 2 (x -3.9), cropped to 1.6 m.
            var crop = new Vector2(FloorSize / Tile, FloorSize / Tile);
            var floor = ManualMaterial("castlebase_floor", "static_objects/castlebase/castlebase_floor", out var floorInfo, out var floorMaps);
            using (var height = new HeightMap(Path.Combine(floorMaps, "height.png"), floorInfo))
            {
                var flat = Panel(FloorSize, FloorSize, Vector2.zero, crop);
                foreach (var (spacing, x, title, file) in new[] { (.01f, FloorCenter - 3.9f, "6 — НОВЫЙ КАМЕННЫЙ ПОЛ\nсетка 1 см", "floor_fine"),
                    (GameSpacing, FloorCenter - 1.3f, "7 — НОВЫЙ КАМЕННЫЙ ПОЛ\nсетка 5 см", "floor_game") })
                {
                    var mesh = SaveMesh(Displace(flat, height, spacing), file);
                    var go = builder.Sample("castlebase_floor MANUAL " + file, mesh, new[] { floor }, new Vector3(x, FloorY, FloorZ), lying, true);
                    builder.Label(go.transform.position + new Vector3(0, .35f, -.75f), title);
                }
                Object.DestroyImmediate(flat);
            }
            builder.Lamp("Manual floor raking light", new Vector3(FloorCenter - 5.8f, 2.9f, FloorZ + 1.7f), new Vector3(FloorCenter, 0, FloorZ));

            // Boards: behind stand sample 3 (x -14.3). Separate planks: chamfer, deep gaps, dark bottom.
            var boards = ManualMaterial("cellarbase_wood_floor", "static_objects/cellarbase/floor/cellarbase_wood_floor", out var boardInfo, out _);
            var gapMaterial = new Material(Shader.Find("HDRP/Lit")) { name = "Board gap bottom" };
            gapMaterial.SetColor("_BaseColor", new Color(.035f, .028f, .02f)); gapMaterial.SetFloat("_Smoothness", .05f);
            HDMaterial.ValidateMaterial(gapMaterial);
            gapMaterial = Save(gapMaterial, Folder + "/board_gap_bottom.mat");
            var planks = SaveMesh(Planks(boardInfo, new Vector2(0, 1 - crop.y), new Vector2(crop.x, 1)), "boards");
            var boardsGo = builder.Sample("cellarbase_wood_floor MANUAL planks", planks, new[] { boards, gapMaterial }, new Vector3(BoardsCenter - 3.9f, FloorY, FloorZ), lying, true);
            builder.Label(boardsGo.transform.position + new Vector3(0, .35f, -.75f),
                $"8 — НОВЫЕ ДОСКИ\nщели {boardInfo.gap * 1000:F0} мм, глубина {boardInfo.joint_depth * 1000:F0} мм");
            builder.Lamp("Manual boards raking light", new Vector3(BoardsCenter - 5.8f, 2.9f, FloorZ + 1.7f), new Vector3(BoardsCenter, 0, FloorZ));

            stand.labels = builder.labels.ToArray();
            stand.inspectionLights = builder.lights.ToArray();
            EditorUtility.SetDirty(stand);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Stand material (enhanced, POM off) with the manual normal and mask maps.</summary>
        static Material ManualMaterial(string stem, string source, out ManualWallReliefProbe.Info info, out string maps)
        {
            maps = ManualWallReliefProbe.MapsFor(source);
            info = JsonUtility.FromJson<ManualWallReliefProbe.Info>(File.ReadAllText(Path.Combine(maps, "relief.json")));
            var standMaterial = AssetDatabase.LoadAssetAtPath<Material>(StandFolder + "/" + stem + ".mat");
            if (standMaterial == null) throw new InvalidOperationException("Missing stand material: " + stem);
            var material = new Material(standMaterial) { name = stem + " manual relief (POM off)" };
            material.SetTexture("_NormalMap", CopyTexture(maps, stem, "normal.png", true)); material.SetFloat("_NormalScale", 1);
            material.SetTexture("_MaskMap", CopyTexture(maps, stem, "mask.png", false));
            material.SetFloat("_DisplacementMode", 0);
            HDMaterial.ValidateMaterial(material);
            return Save(material, Folder + "/" + stem + "_manual.mat");
        }

        internal sealed class HeightMap : IDisposable
        {
            readonly Texture2D _texture;
            readonly float _lo, _hi;
            public HeightMap(string file, ManualWallReliefProbe.Info info)
            {
                _texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Repeat };
                if (!ImageConversion.LoadImage(_texture, File.ReadAllBytes(file), false)) throw new InvalidOperationException("Cannot read " + file);
                _lo = info.height_encoding_metres[0]; _hi = info.height_encoding_metres[1];
            }
            public float Metres(Vector2 uv) => Mathf.Lerp(_lo, _hi, _texture.GetPixelBilinear(uv.x, uv.y).r);
            public void Dispose() => Object.DestroyImmediate(_texture);
        }

        /// <summary>Flat panel facing local -Z, UV lo..hi, as the stand's flat samples.</summary>
        static Mesh Panel(float width, float height, Vector2 lo, Vector2 hi)
        {
            var mesh = new Mesh { name = "manual relief flat" };
            mesh.vertices = new[] { new Vector3(-width / 2, -height / 2, 0), new Vector3(width / 2, -height / 2, 0),
                new Vector3(width / 2, height / 2, 0), new Vector3(-width / 2, height / 2, 0) };
            mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, -1), 4).ToArray();
            mesh.uv = new[] { lo, new Vector2(hi.x, lo.y), hi, new Vector2(lo.x, hi.y) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateBounds();
            return mesh;
        }

        // The maps carry the full stone shape: vertex normals stay those of the plane.
        static Mesh Displace(Mesh flat, HeightMap height, float spacing) => HplReliefMeshBuilder.Build(flat,
            new[] { new HplReliefMeshBuilder.Surface { geometryNormals = false, heightMetres = height.Metres } }, Vector3.one, spacing, 200000);

        /// <summary>Planks of the board texture inside the UV crop lo..hi: flat faces with a chamfer,
        /// side walls down to the gap bottom (submesh 1). Joints come from relief.json.</summary>
        static Mesh Planks(ManualWallReliefProbe.Info info, Vector2 lo, Vector2 hi)
        {
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>();
            var faces = new List<int>(); var bottom = new List<int>();
            var center = (lo + hi) * .5f;
            float half = info.gap / 2 / Tile, chamfer = info.chamfer, depth = info.joint_depth;
            Vector3 At(Vector2 uv, float h) => new((uv.x - center.x) * Tile, (uv.y - center.y) * Tile, -h);
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Vector3 outward, List<int> target)
            {
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { (b, d) = (d, b); (ub, ud) = (ud, ub); }
                int first = vertices.Count;
                vertices.AddRange(new[] { a, b, c, d }); uvs.AddRange(new[] { ua, ub, uc, ud });
                target.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
            var rows = info.rows_from_top;
            int index = 0;
            for (int row = 0; row < info.ends.Length; row++)
            {
                float vTop = 1 - rows[row], vBottom = 1 - rows[row + 1];
                foreach (var (u0, u1) in new[] { (0f, info.ends[row]), (info.ends[row], 1f) })
                {
                    // Clip to the crop; gaps only at real joints, not where the crop cuts a plank.
                    float x0 = Mathf.Max(u0, lo.x), x1 = Mathf.Min(u1, hi.x), y0 = Mathf.Max(vBottom, lo.y), y1 = Mathf.Min(vTop, hi.y);
                    if (x1 - x0 < .01f || y1 - y0 < .01f) continue;
                    bool jl = x0 > lo.x && u0 > 0, jr = x1 < hi.x && u1 < 1, jb = y0 > lo.y, jt = y1 < hi.y;
                    var a = new Vector2(x0 + (jl ? half : 0), y0 + (jb ? half : 0));
                    var b = new Vector2(x1 - (jr ? half : 0), y1 - (jt ? half : 0));
                    float h = Mathf.Sin(index++ * 3.7f + row) * info.board_variation;
                    float c = chamfer / Tile;
                    var ia = new Vector2(a.x + (jl ? c : 0), a.y + (jb ? c : 0));
                    var ib = new Vector2(b.x - (jr ? c : 0), b.y - (jt ? c : 0));
                    var corners = new[] { a, new Vector2(a.x, b.y), b, new Vector2(b.x, a.y) };       // outer, clockwise from -Z
                    var inner = new[] { ia, new Vector2(ia.x, ib.y), ib, new Vector2(ib.x, ia.y) };
                    Quad(At(inner[0], h), At(inner[1], h), At(inner[2], h), At(inner[3], h), inner[0], inner[1], inner[2], inner[3], Vector3.back, faces);
                    var plankCenter = (a + b) * .5f;
                    for (int e = 0; e < 4; e++)
                    {
                        int n = (e + 1) % 4;
                        var mid = (corners[e] + corners[n]) * .5f;
                        var outward = At(mid, 0) - At(plankCenter, 0); outward.z = 0;
                        // Chamfer: inner top edge down to the outer edge.
                        Quad(At(inner[e], h), At(inner[n], h), At(corners[n], h - chamfer), At(corners[e], h - chamfer),
                            inner[e], inner[n], corners[n], corners[e], outward + Vector3.back * outward.magnitude, faces);
                        // Side wall into the gap; its UV is squeezed toward the edge (no long stretched streaks).
                        var sq = (plankCenter - corners[e]).normalized * (depth / Tile * .25f);
                        var sqn = (plankCenter - corners[n]).normalized * (depth / Tile * .25f);
                        Quad(At(corners[e], h - chamfer), At(corners[n], h - chamfer), At(corners[n], -depth), At(corners[e], -depth),
                            corners[e], corners[n], corners[n] + sqn, corners[e] + sq, outward, faces);
                    }
                }
            }
            // Gap bottom under the whole crop.
            Quad(At(lo, -depth - .0005f), At(new Vector2(lo.x, hi.y), -depth - .0005f), At(hi, -depth - .0005f), At(new Vector2(hi.x, lo.y), -depth - .0005f),
                lo, new Vector2(lo.x, hi.y), hi, new Vector2(hi.x, lo.y), Vector3.back, bottom);
            var mesh = new Mesh { name = "cellarbase_wood_floor manual planks" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.subMeshCount = 2;
            mesh.SetTriangles(faces, 0); mesh.SetTriangles(bottom, 1);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        public static void RenderBatch()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Rendering needs a GPU: run without -nographics.");
            Directory.CreateDirectory(Renders);
            var scene = EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var camera = new GameObject("Manual relief render").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.nearClipPlane = .02f; camera.farClipPlane = 80; camera.fieldOfView = 60;
            camera.gameObject.AddComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            try
            {
                void Shot(string name, Vector3 position, Vector3 target)
                {
                    camera.transform.position = position; camera.transform.LookAt(target);
                    ImportMenu.RenderToFile(camera, Path.Combine(Renders, name + ".png"));
                }
                Shot("overview", new Vector3(3.9f, 2.2f, 4.9f), new Vector3(3.9f, 2.3f, WallDepth));
                // Same distance and angle to stand sample 3 (old solid) and to the new samples.
                foreach (var (name, x, y) in new[] { ("close_3_solid", 1.3f, 1.55f), ("close_4_fine", 1.3f, WallRow), ("close_5_game", 3.9f, WallRow) })
                    Shot(name, new Vector3(x - .9f, y + .15f, WallDepth + .75f), new Vector3(x + .1f, y, WallDepth));
                Shot("floor_overview", new Vector3(-5.2f, 2.6f, 5.6f), new Vector3(-5.2f, 0, 2.0f));
                foreach (var (name, x, z) in new[] { ("floor_3_solid", -6.5f, 3.1f), ("floor_6_fine", -6.5f, FloorZ), ("floor_7_game", -3.9f, FloorZ),
                    ("boards_3_solid", -14.3f, 3.1f), ("boards_8_planks", -14.3f, FloorZ) })
                    Shot(name, new Vector3(x + .5f, .55f, z + .75f), new Vector3(x - .15f, 0, z - .1f));
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            }
        }

        internal static Texture2D CopyTexture(string maps, string stem, string file, bool normalMap)
        {
            string path = Folder + "/" + stem + "_manual_" + Path.GetFileNameWithoutExtension(file) + ".png";
            File.Copy(Path.Combine(maps, file), AmnesiaPaths.ToAbsolute(path), true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            // normal.png is already Unity tangent space (G up): no flip.
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = false; importer.flipGreenChannel = false; importer.convertToNormalmap = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false;
            importer.maxTextureSize = 4096; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8; importer.isReadable = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        internal static Mesh SaveMesh(Mesh mesh, string file)
        {
            mesh.name = "manual relief " + file;
            return Save(mesh, Folder + "/" + file + ".asset");
        }

        internal static T Save<T>(T asset, string path) where T : Object
        {
            var previous = AssetDatabase.LoadAssetAtPath<T>(path);
            if (previous == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
            if (asset is Mesh source && previous is Mesh destination)
            {
                // Setters, not CopySerialized: they also drop the old GPU buffers in a running editor.
                destination.Clear(); destination.name = source.name; destination.indexFormat = source.indexFormat;
                destination.vertices = source.vertices; destination.normals = source.normals; destination.tangents = source.tangents;
                destination.uv = source.uv; destination.subMeshCount = source.subMeshCount;
                for (int sub = 0; sub < source.subMeshCount; sub++) destination.SetIndices(source.GetIndices(sub), source.GetTopology(sub), sub);
                destination.bounds = source.bounds;
            }
            else EditorUtility.CopySerialized(asset, previous);
            EditorUtility.SetDirty(previous); Object.DestroyImmediate(asset); return previous;
        }
    }
}
