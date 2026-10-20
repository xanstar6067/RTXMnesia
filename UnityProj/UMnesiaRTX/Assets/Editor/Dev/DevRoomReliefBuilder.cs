using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Dev;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    public static class DevRoomReliefBuilder
    {
        public const string StandName = "Relief comparison stand";
        public const int OilBottles = 24;
        const string Folder = AmnesiaPaths.ImportedAssetsRoot + "/DevRoomRelief";

        public static void InstallAndCheckBatch()
        {
            InstallBatch();
            CheckBatch();
            BuildCheck.RunBatch();
        }

        public static void InstallRenderAndCheckBatch()
        {
            InstallBatch();
            CheckBatch();
            RenderBatch();
            BuildCheck.RunBatch();
        }

        public static void ValidateApproachBatch()
        {
            if (ImportMenu.HasArg("-rebuildReliefStand")) InstallBatch();
            CheckBatch();
            RenderBatch();
            PlayerSimulation.RunBatch();
            BuildCheck.RunBatch();
        }

        [MenuItem("Tools/RTXMnesia/Dev Room/Add or update relief stand and lantern supplies")]
        public static void InstallBatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before updating dev_room.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save edited scenes before updating dev_room.");
            var scene = EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var map = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HplMap>(true)).Single();
            Add(map.gameObject);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[dev_room relief] Installed: three flat/relief/solid comparisons, one lantern, 24 large oil bottles. F4 controls inspection lights.");
        }

        public static void Add(GameObject mapRoot)
        {
            HplEnhancedTextures.Shutdown();
            var old = mapRoot.transform.Find(StandName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Folder));
            AssetDatabase.Refresh();
            var root = new GameObject(StandName);
            root.transform.SetParent(mapRoot.transform, false);
            var stand = root.AddComponent<HplDevReliefStand>();
            var labels = new List<HplDevReliefStand.Label>();
            var flat = new List<MeshFilter>(); var relief = new List<MeshFilter>();
            var masonry = new List<MeshFilter>();
            var lamps = new List<Behaviour>();
            foreach (var name in new[] { "Inspection ambient", "Inspection key light", "Inspection fill light" })
            {
                var transform = mapRoot.transform.Find(name);
                if (transform != null) lamps.AddRange(transform.GetComponents<Behaviour>().Where(c => c is Light || c is AmnesiaLight));
            }
            void Label(string name, string text, Vector3 position)
            {
                var anchor = new GameObject(name).transform; anchor.SetParent(root.transform, false); anchor.position = position;
                labels.Add(new HplDevReliefStand.Label { anchor = anchor, text = text });
            }
            string[] materials = { EnhancedMaterialConverter.Stone + ".mat", "static_objects/castlebase/castlebase_floor.mat", "static_objects/cellarbase/floor/cellarbase_wood_floor.mat" };
            string[] titles = { "Каменная стена", "Каменный пол", "Деревянный пол" };
            for (int group = 0; group < materials.Length; group++)
            {
                string relative = materials[group];
                var enhanced = AssetDatabase.LoadAssetAtPath<Material>(EnhancedMaterialConverter.MaterialPathFor(relative));
                if (enhanced == null) throw new InvalidOperationException("Missing enhanced material: " + relative);
                string stem = Path.GetFileNameWithoutExtension(relative);
                var material = new Material(enhanced) { name = stem + " stand (POM off)" };
                material.SetFloat("_DisplacementMode", 0);
                // Keep the source's roughness/specular calibration on every comparison sample.
                HDMaterial.ValidateMaterial(material);
                material = Save(material, Folder + "/" + stem + ".mat");
                // A dedicated 2.2 m sample avoids bringing the original room-sized floor into the stand.
                var source = new Mesh { name = stem + " flat stand" };
                source.vertices = new[] { new Vector3(-1.1f, -1.1f, 0), new Vector3(1.1f, -1.1f, 0), new Vector3(1.1f, 1.1f, 0), new Vector3(-1.1f, 1.1f, 0) };
                source.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
                source.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, -1), 4).ToArray();
                // The wall is an atlas: inspect its upper brick island rather than its empty padding.
                Vector2 lo = group == 0 ? new Vector2(0.025f, 0.66f) : Vector2.zero;
                Vector2 hi = group == 0 ? new Vector2(0.68f, 0.975f) : Vector2.one;
                float panelHeight = group == 0 ? 2.2f * (hi.y - lo.y) / (hi.x - lo.x) : 2.2f;
                source.vertices = source.vertices.Select(v => new Vector3(v.x, v.y * panelHeight / 2.2f, v.z)).ToArray();
                source.uv = new[] { lo, new Vector2(hi.x, lo.y), hi, new Vector2(lo.x, hi.y) };
                source.triangles = new[] { 0, 2, 1, 0, 3, 2 }; source.RecalculateBounds();
                source = Save(source, Folder + "/" + stem + "_flat.asset");
                string preparedPath = EnhancedMaterialConverter.Root + "/" + Path.ChangeExtension(relative, null) + "__prepared.json";
                var prepared = JsonUtility.FromJson<EnhancedMaterialConverter.PreparedMaps>(File.ReadAllText(preparedPath));
                var result = JsonUtility.FromJson<EnhancedMaterialConverter.BatchResult>(File.ReadAllText(prepared.result));
                string heightFile = Path.Combine(Path.GetDirectoryName(prepared.result), result.files.height.file);
                byte[] bytes = File.ReadAllBytes(heightFile);
                if (EnhancedReliefConverter.Hash(bytes) != result.files.height.sha256) throw new InvalidOperationException("Height hash mismatch: " + relative);
                var height = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Repeat };
                Mesh detailed, solid = null;
                try
                {
                    if (!ImageConversion.LoadImage(height, bytes, false)) throw new InvalidOperationException("Cannot load height: " + relative);
                    detailed = HplReliefMeshBuilder.Build(source, new[] { new HplReliefMeshBuilder.Surface {
                        height = new HplReliefMeshBuilder.HeightField(height), amplitude = EnhancedReliefConverter.Profiles[relative] } }, Vector3.one, 0.018f);
                    if (group < 2) solid = HplMasonrySampleBuilder.Build(height, lo, hi, group == 0);
                    else solid = HplMasonrySampleBuilder.BuildBoards();
                }
                finally { Object.DestroyImmediate(height); }
                detailed = Save(detailed, Folder + "/" + stem + "_relief.asset");
                Material solidMaterial = null, solidSides = null;
                if (solid != null)
                {
                    solid = Save(solid, Folder + "/" + stem + "_solid.asset");
                    solidMaterial = new Material(material) { name = stem + " solid blocks (POM off)" };
                    if (group < 2)
                    {
                        solidMaterial.SetTexture("_NormalMap", MicroNormal(prepared, result, stem, hi.x - lo.x));
                        solidMaterial.SetTexture("_MaskMap", CavityMask(prepared, result, stem, solid, lo, hi, out var cavitySpecular));
                        solidMaterial.SetTexture("_SpecularColorMap", cavitySpecular);
                    }
                    solidMaterial.SetFloat("_NormalScale", 1);
                    HDMaterial.ValidateMaterial(solidMaterial);
                    solidMaterial = Save(solidMaterial, Folder + "/" + stem + "_solid.mat");
                    solidSides = new Material(material) { name = stem + " stone sides" };
                    solidSides.SetTexture("_NormalMap", null); solidSides.SetFloat("_NormalScale", 0);
                    solidSides.SetFloat("_SmoothnessRemapMin", 0); solidSides.SetFloat("_SmoothnessRemapMax", 0);
                    solidSides.SetColor("_SpecularColor", Color.black);
                    solidSides.SetTexture("_SpecularColorMap", null);
                    HDMaterial.ValidateMaterial(solidSides);
                    solidSides = Save(solidSides, Folder + "/" + stem + "_sides.mat");
                }
                // The player faces -Z: screen-left is +X in world space.
                float center = 5.2f - group * 7.8f;
                for (int side = 0; side < (solid == null ? 2 : 3); side++)
                {
                    var go = new GameObject(stem + (side == 0 ? " FLAT" : side == 1 ? " RELIEF" : " SOLID BLOCKS")); go.transform.SetParent(root.transform, false);
                    go.transform.position = new Vector3(center + 1.3f - side * 2.6f, group == 0 ? 1.55f : 0.04f, group == 0 ? 1.2f : 3.1f);
                    go.transform.rotation = group == 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(90, 0, 0);
                    var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = side == 0 ? source : side == 1 ? detailed : solid;
                    var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = side == 2 ? solidMaterial : material;
                    if (side == 2) renderer.sharedMaterials = new[] { solidMaterial, solidSides };
                    renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                    // Visual relief is not collision geometry. The character queries penetration
                    // several times per tick; feeding it ~300k triangles stalls when close to a face.
                    if (side == 2) { var collider = go.AddComponent<BoxCollider>(); collider.center = solid.bounds.center; collider.size = solid.bounds.size; }
                    else { var collider = go.AddComponent<BoxCollider>(); collider.center = Vector3.forward * 0.04f; collider.size = new Vector3(2.2f, panelHeight, 0.08f); }
                    (side == 0 ? flat : side == 1 ? relief : masonry).Add(filter);
                    Label(go.name + " label", titles[group] + "\n" + (side == 0 ? "1 — ПЛОСКО" : side == 1 ? "2 — ПРЕЖНИЙ РЕЛЬЕФ" : group == 2 ? "3 — ДОСКИ С ЩЕЛЯМИ" : "3 — КЛАДКА С УГЛУБЛЁННЫМИ ШВАМИ"),
                        go.transform.position + (group == 0 ? Vector3.up * (panelHeight * .5f + .3f) : new Vector3(0, 0.35f, -0.9f)));
                }
                var lamp = new GameObject(titles[group] + " raking light").AddComponent<Light>(); lamp.transform.SetParent(root.transform, false);
                lamp.type = LightType.Spot; lamp.range = 12; lamp.spotAngle = 120;
                lamp.transform.position = new Vector3(center - 5.8f, 2.9f, group == 0 ? 3 : 4.8f);
                lamp.transform.LookAt(new Vector3(center, group == 0 ? 1.5f : 0, group == 0 ? 1.2f : 3.1f));
                lamp.shadows = LightShadows.Soft; lamp.gameObject.AddComponent<HDAdditionalLightData>(); lamp.lightUnit = LightUnit.Candela; lamp.intensity = 100;
                lamps.Add(lamp);
            }
            var surfaceMaterial = new Material(Shader.Find("HDRP/Lit")) { name = "Relief supply table" };
            surfaceMaterial.SetColor("_BaseColor", new Color(0.16f, 0.11f, 0.065f)); surfaceMaterial.SetFloat("_Smoothness", 0.15f);
            surfaceMaterial = Save(surfaceMaterial, Folder + "/SupplyTable.mat");
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube); table.name = "Lantern and oil table"; table.transform.SetParent(root.transform, false);
            table.transform.position = new Vector3(4.1f, 0.43f, 4.3f); table.transform.localScale = new Vector3(3, 0.86f, 1.05f); table.GetComponent<Renderer>().sharedMaterial = surfaceMaterial;
            Label("Supply label", "ФОНАРЬ + 24 БОЛЬШИХ МАСЛА\nПодберите фонарь и бутылки", new Vector3(4.1f, 1.95f, 4.3f));
            AddSupplies(root.transform);
            stand.flat = flat.ToArray(); stand.relief = relief.ToArray(); stand.masonry = masonry.ToArray(); stand.labels = labels.ToArray(); stand.inspectionLights = lamps.Distinct().ToArray();
            var start = mapRoot.GetComponentsInChildren<AmnesiaArea>(true).First(a => a.areaType == "PlayerStart" && a.name == "PlayerStartArea_1");
            start.transform.SetPositionAndRotation(new Vector3(0, 0.05f, 5.1f), Quaternion.Euler(0, 180, 0));
            var player = Object.FindAnyObjectByType<AmnesiaPlayer>();
            if (player != null) player.transform.SetPositionAndRotation(start.transform.position + Vector3.up * 0.05f, start.transform.rotation);
            AssetDatabase.SaveAssets();
        }

        static void AddSupplies(Transform root)
        {
            string prefabPath = Folder + "/Supplies.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) prefab = LevelConverter.BuildEntitiesPrefab(prefabPath, new[] {
                (Path.Combine(AmnesiaPaths.SourceRoot, "entities/item/lantern/lantern.ent"), "lantern_template"),
                (Path.Combine(AmnesiaPaths.SourceRoot, "entities/item/potion_oil/potion_oil_large.ent"), "oil_template") }, null);
            void Place(string template, string name, Vector3 position, int id)
            {
                var source = prefab.transform.Find(template);
                var item = Object.Instantiate(source.gameObject, root); item.name = name; item.transform.localPosition = position;
                item.GetComponent<AmnesiaEntity>().hplId = id;
            }
            Place("lantern_template", "relief_lantern", new Vector3(5.2f, 0.88f, 4.3f), 90000);
            for (int i = 0; i < OilBottles; i++) Place("oil_template", "relief_oil_" + (i + 1).ToString("00"),
                new Vector3(4.65f - i % 6 * 0.31f, 0.88f, 3.94f + i / 6 * 0.24f), 90001 + i);
        }

        static T Save<T>(T asset, string path) where T : Object
        {
            var previous = AssetDatabase.LoadAssetAtPath<T>(path);
            if (previous == null) { AssetDatabase.CreateAsset(asset, path); return asset; }
            if (asset is Mesh source && previous is Mesh destination)
            {
                // CopySerialized updates the CPU/serialized mesh but can leave its old GPU buffers
                // alive until the next editor launch. Mesh setters invalidate those buffers too.
                destination.Clear(); destination.name = source.name; destination.indexFormat = source.indexFormat;
                destination.vertices = source.vertices; destination.normals = source.normals; destination.tangents = source.tangents;
                destination.colors = source.colors;
                var uv = new List<Vector4>();
                for (int channel = 0; channel < 8; channel++) { source.GetUVs(channel, uv); destination.SetUVs(channel, uv); }
                destination.subMeshCount = source.subMeshCount;
                for (int sub = 0; sub < source.subMeshCount; sub++) destination.SetIndices(source.GetIndices(sub), source.GetTopology(sub), sub);
                destination.bounds = source.bounds;
            }
            else EditorUtility.CopySerialized(asset, previous);
            EditorUtility.SetDirty(previous); Object.DestroyImmediate(asset); return previous;
        }

        // Retain pores/chips at their original strength, removing only the broad slopes already
        // represented by the mesh. Work in normal slopes, not sRGB colours or encoded RGB averages.
        static Texture2D MicroNormal(EnhancedMaterialConverter.PreparedMaps prepared, EnhancedMaterialConverter.BatchResult result, string stem, float uvWidth)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(prepared.result), result.files.normal_ogl.file));
            if (EnhancedReliefConverter.Hash(bytes) != result.files.normal_ogl.sha256) throw new InvalidOperationException("Normal hash mismatch: " + stem);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            string path = Folder + "/" + stem + "_micro_normal.png";
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidOperationException("Cannot read normal: " + stem);
                int w = texture.width, h = texture.height;
                int radius = Mathf.Max(2, Mathf.RoundToInt(w * uvWidth / 2.2f * .006f));
                var pixels = texture.GetPixels32();
                var slopes = new Vector2[pixels.Length]; var horizontal = new Vector2[pixels.Length];
                for (int p = 0; p < pixels.Length; p++)
                {
                    var c = pixels[p];
                    float z = Mathf.Max(.15f, c.b / 127.5f - 1);
                    // NormalV3 has image-down G; flip it exactly once for Unity tangent Y.
                    slopes[p] = new Vector2(c.r / 127.5f - 1, 1 - c.g / 127.5f) / z;
                }
                int taps = radius * 2 + 1;
                for (int y = 0; y < h; y++)
                {
                    Vector2 sum = Vector2.zero;
                    for (int x = -radius; x <= radius; x++) sum += slopes[y * w + Mathf.Clamp(x, 0, w - 1)];
                    for (int x = 0; x < w; x++)
                    {
                        horizontal[y * w + x] = sum / taps;
                        sum += slopes[y * w + Mathf.Clamp(x + radius + 1, 0, w - 1)] - slopes[y * w + Mathf.Clamp(x - radius, 0, w - 1)];
                    }
                }
                for (int x = 0; x < w; x++)
                {
                    Vector2 sum = Vector2.zero;
                    for (int y = -radius; y <= radius; y++) sum += horizontal[Mathf.Clamp(y, 0, h - 1) * w + x];
                    for (int y = 0; y < h; y++)
                    {
                        int p = y * w + x;
                        var slope = slopes[p] - sum / taps;
                        var n = new Vector3(slope.x, slope.y, 1).normalized;
                        pixels[p] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                        sum += horizontal[Mathf.Clamp(y + radius + 1, 0, h - 1) * w + x] - horizontal[Mathf.Clamp(y - radius, 0, h - 1) * w + x];
                    }
                }
                texture.SetPixels32(pixels); texture.Apply();
                File.WriteAllBytes(AmnesiaPaths.ToAbsolute(path), texture.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.NormalMap; importer.convertToNormalmap = false;
            importer.flipGreenChannel = false; importer.sRGBTexture = false; importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8; importer.mipmapEnabled = true; importer.isReadable = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // Private to the solid sample. Face gloss comes from the original roughness; recesses
        // and bevels become rough using the same coverage field that constructed the geometry.
        static Texture2D CavityMask(EnhancedMaterialConverter.PreparedMaps prepared, EnhancedMaterialConverter.BatchResult result, string stem, Mesh mesh, Vector2 lo, Vector2 hi, out Texture2D specular)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(prepared.result), result.files.roughness.file));
            if (EnhancedReliefConverter.Hash(bytes) != result.files.roughness.sha256) throw new InvalidOperationException("Roughness hash mismatch: " + stem);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            string path = Folder + "/" + stem + "_cavity_mask.png";
            string specularPath = Folder + "/" + stem + "_cavity_specular.png";
            try
            {
                if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidOperationException("Cannot read roughness: " + stem);
                int size = Mathf.RoundToInt(Mathf.Sqrt(mesh.GetSubMesh(0).vertexCount));
                var weights = mesh.colors; var pixels = texture.GetPixels32();
                var reflectance = new Color32[pixels.Length];
                for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
                {
                    int p = y * texture.width + x;
                    float u = ((x + .5f) / texture.width - lo.x) / (hi.x - lo.x);
                    float v = ((y + .5f) / texture.height - lo.y) / (hi.y - lo.y);
                    float weight = 0;
                    if (u >= 0 && u <= 1 && v >= 0 && v <= 1)
                    {
                        float fx = u * (size - 1), fy = v * (size - 1);
                        int ix = Mathf.Min((int)fx, size - 2), iy = Mathf.Min((int)fy, size - 2);
                        weight = Mathf.Lerp(Mathf.Lerp(weights[iy * size + ix].r, weights[iy * size + ix + 1].r, fx - ix),
                            Mathf.Lerp(weights[(iy + 1) * size + ix].r, weights[(iy + 1) * size + ix + 1].r, fx - ix), fy - iy);
                    }
                    pixels[p] = new Color32(0, 255, 255, (byte)Mathf.RoundToInt((255 - pixels[p].r) * weight));
                    byte factor = (byte)Mathf.RoundToInt(weight * 255);
                    reflectance[p] = new Color32(factor, factor, factor, 255);
                }
                // LoadImage can change an RGBA texture to RGB24 for an RGB roughness PNG.
                // Restore an alpha-capable format before packing smoothness into A.
                texture.Reinitialize(texture.width, texture.height, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels); texture.Apply();
                File.WriteAllBytes(AmnesiaPaths.ToAbsolute(path), texture.EncodeToPNG());
                texture.SetPixels32(reflectance); texture.Apply();
                File.WriteAllBytes(AmnesiaPaths.ToAbsolute(specularPath), texture.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(texture); }
            foreach (string generated in new[] { path, specularPath })
            {
                AssetDatabase.ImportAsset(generated, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(generated);
                importer.textureType = TextureImporterType.Default; importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = false;
                importer.maxTextureSize = 4096; importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 8; importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            specular = AssetDatabase.LoadAssetAtPath<Texture2D>(specularPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void CheckBatch()
        {
            EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var stand = Object.FindAnyObjectByType<HplDevReliefStand>();
            int checks = 0;
            void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
                checks++; Debug.Log("[dev_room relief] ok: " + message);
            }
            Check(stand != null && stand.flat.Length == 3 && stand.relief.Length == 3, "three flat/relief pairs saved in scene");
            var filters = stand.GetComponentsInChildren<MeshFilter>();
            var original = filters.ToDictionary(f => f, f => f.sharedMesh);
            var materials = stand.flat.Concat(stand.relief).Concat(stand.masonry).ToDictionary(f => f, f => f.GetComponent<Renderer>().sharedMaterial);
            for (int i = 0; i < 3; i++)
            {
                Check(stand.flat[i].sharedMesh.vertexCount == 4 && stand.relief[i].sharedMesh.vertexCount > 1000, "actual subdivided geometry: pair " + i);
                var flatMaterial = stand.flat[i].GetComponent<Renderer>().sharedMaterial;
                Check(flatMaterial == stand.relief[i].GetComponent<Renderer>().sharedMaterial && flatMaterial.GetFloat("_DisplacementMode") == 0,
                    "identical enhanced material, POM off: pair " + i);
            }
            Check(stand.masonry.Length == 3, "two masonry samples and straight edged boards saved");
            for (int i = 0; i < 2; i++)
            {
                var sample = stand.masonry[i]; var mesh = sample.sharedMesh;
                Check(mesh.bounds.size.z > (i == 0 ? .055f : .04f) && mesh.bounds.size.z < .08f && mesh.vertexCount > 15000, "masonry depth stays below 8 cm including backing: sample " + i);
                Check(mesh.normals.Any(n => Mathf.Abs(n.z) < .15f), "actual sideways-facing stone surfaces: sample " + i);
                var material = sample.GetComponent<Renderer>().sharedMaterial;
                string relative = i == 0 ? EnhancedMaterialConverter.Stone + ".mat" : "static_objects/castlebase/castlebase_floor.mat";
                var enhanced = AssetDatabase.LoadAssetAtPath<Material>(EnhancedMaterialConverter.MaterialPathFor(relative));
                var micro = material.GetTexture("_NormalMap");
                Check(micro != null && micro != enhanced.GetTexture("_NormalMap") && material.GetFloat("_NormalScale") == 1
                    && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(micro)) is TextureImporter normalImporter
                    && normalImporter.textureType == TextureImporterType.NormalMap && !normalImporter.flipGreenChannel,
                    "dedicated micro normal, full strength, green converted exactly once: sample " + i);
                foreach (var candidate in new[] { stand.flat[i].GetComponent<Renderer>().sharedMaterial, stand.relief[i].GetComponent<Renderer>().sharedMaterial })
                {
                    Check(new[] { "_SmoothnessRemapMin", "_SmoothnessRemapMax", "_MetallicRemapMax" }.All(p => candidate.GetFloat(p) == enhanced.GetFloat(p))
                        && candidate.GetColor("_SpecularColor") == enhanced.GetColor("_SpecularColor")
                        && candidate.GetTexture("_MaskMap") == enhanced.GetTexture("_MaskMap")
                        && candidate.GetTexture("_SpecularColorMap") == enhanced.GetTexture("_SpecularColorMap"), "source gloss and reflectance preserved: " + candidate.name);
                }
                Check(material.GetTexture("_MaskMap") != enhanced.GetTexture("_MaskMap") && material.GetFloat("_SmoothnessRemapMax") == enhanced.GetFloat("_SmoothnessRemapMax"),
                    "private cavity mask; stone-face gloss remap unchanged: sample " + i);
                Check(material.GetTexture("_SpecularColorMap") != null && material.IsKeywordEnabled("_SPECULARCOLORMAP")
                    && material.GetColor("_SpecularColor") == enhanced.GetColor("_SpecularColor"),
                    "private specular coverage removes recess glints and preserves face F0: sample " + i);
                Check(sample.GetComponent<Renderer>().sharedMaterials[1].GetFloat("_SmoothnessRemapMax") == 0,
                    "sample outer cut has no glossy highlight: sample " + i);
                var maskProbe = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                try
                {
                    ImageConversion.LoadImage(maskProbe, File.ReadAllBytes(AmnesiaPaths.ToAbsolute(AssetDatabase.GetAssetPath(material.GetTexture("_MaskMap")))), false);
                    var coverage = mesh.colors; var uv = mesh.uv;
                    int grid = Mathf.RoundToInt(Mathf.Sqrt(mesh.GetSubMesh(0).vertexCount));
                    int cavities = 0, faces = 0; float maximumCavityGloss = 0, maximumFaceGloss = 0;
                    for (int p = 0; p < mesh.GetSubMesh(0).vertexCount; p += 7)
                    {
                        float gloss = maskProbe.GetPixelBilinear(uv[p].x, uv[p].y).a;
                        int gx = p % grid, gy = p / grid;
                        if (gx > 0 && gx < grid - 1 && gy > 0 && gy < grid - 1 && coverage[p].r < .001f &&
                            coverage[p - 1].r < .001f && coverage[p + 1].r < .001f && coverage[p - grid].r < .001f && coverage[p + grid].r < .001f)
                        { cavities++; maximumCavityGloss = Mathf.Max(maximumCavityGloss, gloss); }
                        if (coverage[p].r > .999f) { faces++; maximumFaceGloss = Mathf.Max(maximumFaceGloss, gloss); }
                    }
                    Check(cavities > 100 && faces > 100 && maximumCavityGloss < .04f && maximumFaceGloss > .1f,
                        $"generated mask roughens recesses while retaining face highlights: sample {i}, cavity {maximumCavityGloss:F4}, face {maximumFaceGloss:F4}, counts {cavities}/{faces}");
                }
                finally { Object.DestroyImmediate(maskProbe); }
                Check(material.GetTexture("_BaseColorMap") == stand.flat[i].GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseColorMap") && material.GetFloat("_DisplacementMode") == 0,
                    "same enhanced albedo and no POM: solid sample " + i);
                var box = sample.GetComponent<BoxCollider>();
                Check(sample.GetComponent<MeshCollider>() == null && box != null && box.center == mesh.bounds.center && box.size == mesh.bounds.size,
                    "bounded primitive collision covers relief; dense render mesh never enters character queries: sample " + i);
                Check(mesh.subMeshCount == 2 && sample.GetComponent<Renderer>().sharedMaterials.Length == 2 && sample.GetComponent<Renderer>().sharedMaterials[1].GetTexture("_NormalMap") == null,
                    "stone sides have a separate material without projected face normals: sample " + i);
                var positions = mesh.vertices; var indices = mesh.triangles;
                var front = mesh.GetTriangles(0);
                var edgeCounts = new Dictionary<(int, int), int>();
                for (int t = 0; t < front.Length; t += 3) for (int e = 0; e < 3; e++)
                {
                    int a = front[t + e], b = front[t + (e + 1) % 3];
                    var key = (Math.Min(a, b), Math.Max(a, b));
                    edgeCounts.TryGetValue(key, out int count); edgeCounts[key] = count + 1;
                }
                Check(edgeCounts.All(e => e.Value == 2 || e.Value == 1 &&
                    (Mathf.Abs(positions[e.Key.Item1].x) > 1.099f && Mathf.Abs(positions[e.Key.Item2].x) > 1.099f ||
                     Mathf.Abs(positions[e.Key.Item1].y) > mesh.bounds.extents.y - .001f && Mathf.Abs(positions[e.Key.Item2].y) > mesh.bounds.extents.y - .001f)),
                    "continuous front surface: no open edges inside mortar joints: sample " + i);
                if (i == 0) Check(Mathf.Abs(mesh.bounds.size.x / mesh.bounds.size.y - .655f / .315f) < .001f,
                    "wall texture aspect preserved: stones are not stretched vertically");
                Check(indices.All(n => n >= 0 && n < positions.Length) && positions.All(p => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)), "valid solid mesh buffers: sample " + i);
                Check(mesh.tangents.All(t => float.IsFinite(t.x) && float.IsFinite(t.y) && float.IsFinite(t.z) && new Vector3(t.x, t.y, t.z).sqrMagnitude > .5f), "usable tangent frames on stone faces and sides: sample " + i);
            }
            var boards = stand.masonry[2];
            var boardMesh = boards.sharedMesh;
            Check(boardMesh.vertexCount < 1000 && boardMesh.bounds.size.z > .015f && boardMesh.bounds.size.z < .025f,
                "boards have real thickness with fewer than 1000 vertices");
            Check(boardMesh.GetTriangles(0).All(p => Vector3.Dot(boardMesh.normals[p], Vector3.back) > .999f),
                "flat board faces and hard edges; no rounded stone shoulders");
            Check(boards.GetComponent<MeshCollider>() == null && boards.GetComponent<BoxCollider>() != null,
                "boards use a simple walkable collider");
            Check(boards.GetComponent<Renderer>().sharedMaterial.GetTexture("_MaskMap") == stand.flat[2].GetComponent<Renderer>().sharedMaterial.GetTexture("_MaskMap"),
                "original wood face roughness preserved");
            foreach (bool enhanced in new[] { true, false, true, false })
            {
                foreach (var renderer in stand.GetComponentsInChildren<Renderer>()) HplEnhancedTextures.Apply(renderer, enhanced);
                Check(original.All(p => p.Key.sharedMesh == p.Value) && materials.All(p => p.Key.GetComponent<Renderer>().sharedMaterial == p.Value),
                    "stand comparison remains fixed with global texture mode " + enhanced);
            }
            var items = stand.GetComponentsInChildren<AmnesiaEntity>();
            Check(items.Count(e => e.entityType == "Item" && e.entitySubType == "Lantern") == 1, "one pickable lantern");
            Check(items.Count(e => e.entityType == "Item" && e.entitySubType == "LampOil") == OilBottles, "24 pickable oil bottles");
            Check(items.Where(e => e.entitySubType == "LampOil").All(e => e.GetVariable("OilAmount") == "50"), "large oil bottles, 50 oil each");
            Check(items.Select(e => e.name).Distinct().Count() == 25 && items.Select(e => e.hplId).Distinct().Count() == 25, "unique item names and IDs");
            stand.SetInspection(false); Check(stand.inspectionLights.All(l => !l.enabled), "F4 can disable all inspection lights");
            stand.SetInspection(true); Check(stand.inspectionLights.All(l => l.enabled), "F4 can restore inspection lights");
            var start = Object.FindObjectsByType<AmnesiaArea>(FindObjectsInactive.Include).First(a => a.name == "PlayerStartArea_1");
            Check(start.transform.position.z > 4.5f && Vector3.Dot(start.transform.forward, Vector3.back) > 0.99f, "spawn faces comparison stand");
            Check(Enumerable.Range(0, 3).All(i => Vector3.Dot(stand.flat[i].transform.position - stand.relief[i].transform.position, start.transform.right) < 0),
                "flat sample is on the player's left in every pair");
            HplEnhancedTextures.Shutdown();
            Directory.CreateDirectory(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "DevRoomRelief"));
            File.WriteAllText(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "DevRoomRelief", "Check.txt"), "PASS: " + checks + " checks\n");
            Debug.Log("[dev_room relief] PASS: " + checks + " checks");
        }

        public static void RenderBatch()
        {
            EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var camera = Object.FindAnyObjectByType<AmnesiaPlayer>().playerCamera;
            string folder = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "DevRoomRelief"); Directory.CreateDirectory(folder);
            camera.fieldOfView = 80;
            camera.transform.position = new Vector3(0, 4.8f, 10.5f); camera.transform.LookAt(new Vector3(0, 1, 2.5f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "Overview.png"));
            camera.fieldOfView = 65;
            camera.transform.position = new Vector3(3.9f, 2.8f, 6.7f); camera.transform.LookAt(new Vector3(3.9f, 1.45f, 1.2f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "Wall.png"));
            camera.fieldOfView = 55;
            camera.transform.position = new Vector3(-.45f, 1.8f, 3.15f); camera.transform.LookAt(new Vector3(1.3f, 1.45f, 1.25f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "SolidWall.png"));
            var stand = Object.FindAnyObjectByType<HplDevReliefStand>();
            var inspection = new GameObject("Temporary preview raking light").AddComponent<Light>();
            try
            {
                stand.SetInspection(false);
                inspection.type = LightType.Spot; inspection.range = 9; inspection.spotAngle = 115;
                inspection.transform.position = new Vector3(2.8f, 2.8f, 2.2f); inspection.transform.LookAt(new Vector3(1.3f, 1.3f, 1.2f));
                inspection.shadows = LightShadows.Soft; inspection.gameObject.AddComponent<HDAdditionalLightData>();
                inspection.lightUnit = LightUnit.Candela; inspection.intensity = 90;
                inspection.color = new Color(1, .82f, .64f);
                ImportMenu.RenderToFile(camera, Path.Combine(folder, "SolidWallRaking.png"));
            }
            finally { Object.DestroyImmediate(inspection.gameObject); stand.SetInspection(true); }
            camera.transform.position = new Vector3(-7.6f, 1.25f, 4.9f); camera.transform.LookAt(new Vector3(-6.5f, .08f, 3.1f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "SolidFloor.png"));
            camera.transform.position = new Vector3(-15.4f, 1.25f, 4.9f); camera.transform.LookAt(new Vector3(-14.3f, .05f, 3.1f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "SolidBoards.png"));
            camera.transform.position = new Vector3(4.1f, 1.75f, 6); camera.transform.LookAt(new Vector3(4.1f, 0.95f, 4.3f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "Supplies.png"));
            RenderMasonryDetail(camera, stand, folder);
        }

        static void RenderMasonryDetail(Camera camera, HplDevReliefStand stand, string folder)
        {
            var root = new GameObject("Temporary masonry preview");
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            try
            {
                stand.SetInspection(false);
                var volume = root.AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10000; volume.sharedProfile = profile;
                var exposure = profile.Add<Exposure>(true); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(3);
                profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                profile.Add<VisualEnvironment>(true).skyType.Override(0);
                var light = new GameObject("White grazing light").AddComponent<Light>(); light.transform.SetParent(root.transform);
                light.type = LightType.Point; light.transform.position = new Vector3(2.6f, 2.3f, 2.15f);
                light.range = 8; light.shadows = LightShadows.Soft; light.gameObject.AddComponent<HDAdditionalLightData>();
                light.lightUnit = LightUnit.Candela; light.intensity = 70;
                var fill = new GameObject("Soft fill").AddComponent<Light>(); fill.transform.SetParent(root.transform);
                fill.type = LightType.Directional; fill.transform.rotation = Quaternion.LookRotation(Vector3.back);
                fill.gameObject.AddComponent<HDAdditionalLightData>(); fill.lightUnit = LightUnit.Lux; fill.intensity = 12;
                camera.fieldOfView = 45;
                camera.transform.position = new Vector3(.35f, 1.85f, 3.35f); camera.transform.LookAt(new Vector3(1.3f, 1.55f, 1.2f));
                ImportMenu.RenderToFile(camera, Path.Combine(folder, "MasonryDetail.png"));
                camera.transform.position = new Vector3(1.3f, 1.55f, 3.45f); camera.transform.LookAt(new Vector3(1.3f, 1.55f, 1.2f));
                ImportMenu.RenderToFile(camera, Path.Combine(folder, "MasonryFront.png"));
                light.color = new Color(1, .65f, .32f); light.intensity = 90;
                ImportMenu.RenderToFile(camera, Path.Combine(folder, "MasonryWarm.png"));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); stand.SetInspection(true); }
        }
    }
}
