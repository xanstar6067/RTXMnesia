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
            Debug.Log("[dev_room relief] Installed: three flat/relief pairs, two solid masonry samples, one lantern, 24 large oil bottles. F4 controls inspection lights.");
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
                // The dry stone experiment keeps the same roughness calibration on all three
                // variants. The original enhanced materials on game maps are not edited.
                if (group < 2) material.SetFloat("_SmoothnessRemapMax", .4f);
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
                }
                finally { Object.DestroyImmediate(height); }
                detailed = Save(detailed, Folder + "/" + stem + "_relief.asset");
                Material solidMaterial = null, solidSides = null;
                if (solid != null)
                {
                    solid = Save(solid, Folder + "/" + stem + "_solid.asset");
                    solidMaterial = new Material(material) { name = stem + " solid blocks (POM off)" };
                    // Mesh normals now supply the large shoulders and vertical sides. Keep only a
                    // gentler contribution from the original normal map on those new surfaces.
                    solidMaterial.SetFloat("_NormalScale", .45f);
                    HDMaterial.ValidateMaterial(solidMaterial);
                    solidMaterial = Save(solidMaterial, Folder + "/" + stem + "_solid.mat");
                    solidSides = new Material(material) { name = stem + " stone sides" };
                    solidSides.SetTexture("_NormalMap", null); solidSides.SetFloat("_NormalScale", 0);
                    solidSides.SetFloat("_SmoothnessRemapMax", .25f);
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
                    if (side == 2) go.AddComponent<MeshCollider>().sharedMesh = solid;
                    else { var collider = go.AddComponent<BoxCollider>(); collider.center = Vector3.forward * 0.04f; collider.size = new Vector3(2.2f, 2.2f, 0.08f); }
                    (side == 0 ? flat : side == 1 ? relief : masonry).Add(filter);
                    Label(go.name + " label", titles[group] + "\n" + (side == 0 ? "1 — ПЛОСКО" : side == 1 ? "2 — ПРЕЖНИЙ РЕЛЬЕФ" : "3 — ОТДЕЛЬНЫЕ КАМНИ"),
                        go.transform.position + (group == 0 ? Vector3.up * 1.4f : new Vector3(0, 0.35f, -0.9f)));
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
            EditorUtility.CopySerialized(asset, previous); EditorUtility.SetDirty(previous); Object.DestroyImmediate(asset); return previous;
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
            Check(stand.masonry.Length == 2, "two saved solid masonry samples");
            for (int i = 0; i < stand.masonry.Length; i++)
            {
                var sample = stand.masonry[i]; var mesh = sample.sharedMesh;
                Check(mesh.bounds.size.z > (i == 0 ? .11f : .06f) && mesh.vertexCount > 15000, "pronounced solid geometry: sample " + i);
                Check(mesh.normals.Any(n => Mathf.Abs(n.z) < .15f), "actual sideways-facing stone surfaces: sample " + i);
                var material = sample.GetComponent<Renderer>().sharedMaterial;
                Check(material.GetTexture("_BaseColorMap") == stand.flat[i].GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseColorMap") && material.GetFloat("_DisplacementMode") == 0,
                    "same enhanced albedo and no POM: solid sample " + i);
                Check(sample.GetComponent<MeshCollider>().sharedMesh == mesh, "solid sample collision follows stones: sample " + i);
                Check(mesh.subMeshCount == 2 && sample.GetComponent<Renderer>().sharedMaterials.Length == 2 && sample.GetComponent<Renderer>().sharedMaterials[1].GetTexture("_NormalMap") == null,
                    "stone sides have a separate material without projected face normals: sample " + i);
                var positions = mesh.vertices; var indices = mesh.triangles;
                Check(indices.All(n => n >= 0 && n < positions.Length) && positions.All(p => float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)), "valid solid mesh buffers: sample " + i);
                Check(mesh.tangents.All(t => float.IsFinite(t.x) && float.IsFinite(t.y) && float.IsFinite(t.z) && new Vector3(t.x, t.y, t.z).sqrMagnitude > .5f), "usable tangent frames on stone faces and sides: sample " + i);
            }
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
            camera.transform.position = new Vector3(4.1f, 1.75f, 6); camera.transform.LookAt(new Vector3(4.1f, 0.95f, 4.3f));
            ImportMenu.RenderToFile(camera, Path.Combine(folder, "Supplies.png"));
        }
    }
}
