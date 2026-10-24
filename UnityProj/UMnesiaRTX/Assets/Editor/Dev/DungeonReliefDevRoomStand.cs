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
    /// <summary>dev_room samples of the dungeonbase atlas relief (tools/Build-ManualRelief.py dungeon): three real
    /// wall/default_short modules (2 × 4 m) against the front wall, left of the relief stand: 1 — classic, as in
    /// the game; 2 — relief at the game's triangle budget; 3 — fine relief. Also removes the stand's first
    /// samples (flat / old relief / solid blocks 1–3): their assets stay, the boards profile still reads them.</summary>
    public static class DungeonReliefDevRoomStand
    {
        public const string RootName = "Dungeon relief samples";
        const string Material = "static_objects/dungeonbase/dungeonbase";
        const string Module = AmnesiaPaths.ImportedAssetsRoot + "/Models/static_objects/dungeonbase/wall/default_short.prefab";
        // The module's wall plane is its back: 5 cm off the room wall, behind it the recessed joints (to 3.2 cm).
        const float FrontWall = .25f, FirstX = 9.6f, Step = 2.4f;
        static string Renders => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "Dungeon", "DevRoom");

        [MenuItem("Tools/RTXMnesia/Dev Room/Add or update dungeon relief samples")]
        public static void InstallBatch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before updating dev_room.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save edited scenes before updating dev_room.");
            var scene = EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var map = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HplMap>(true)).Single();
            var stand = map.GetComponentInChildren<HplDevReliefStand>(true);
            if (stand == null) throw new InvalidOperationException("Install the relief stand first (Dev Room > Add or update relief stand).");
            RemoveFirstSamples(stand);
            Add(map.transform, stand);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[dev_room dungeon relief] Installed: dungeonbase 1 classic / 2 game relief / 3 fine relief; stand samples 1–3 removed.");
        }

        public static void InstallAndRenderBatch()
        {
            InstallBatch();
            RenderBatch();
        }

        /// <summary>The stand's flat / old relief / solid block samples, their labels and raking lights.</summary>
        static void RemoveFirstSamples(HplDevReliefStand stand)
        {
            var samples = stand.flat.Concat(stand.relief).Concat(stand.masonry).Where(f => f != null).Select(f => f.gameObject).ToList();
            var names = new HashSet<string>(samples.Select(s => s.name + " label"));
            stand.labels = stand.labels.Where(l => l.anchor != null && !names.Contains(l.anchor.name)).ToArray();
            foreach (var label in stand.GetComponentsInChildren<Transform>(true).Where(t => names.Contains(t.name)).ToList())
                Object.DestroyImmediate(label.gameObject);
            var lights = new[] { "Каменная стена", "Каменный пол", "Деревянный пол" }.Select(t => t + " raking light").ToHashSet();
            foreach (var light in stand.GetComponentsInChildren<Light>(true).Where(l => lights.Contains(l.name)).ToList())
                Object.DestroyImmediate(light.gameObject);
            foreach (var sample in samples) Object.DestroyImmediate(sample);
            stand.flat = Array.Empty<MeshFilter>(); stand.relief = Array.Empty<MeshFilter>(); stand.masonry = Array.Empty<MeshFilter>();
            stand.inspectionLights = stand.inspectionLights.Where(l => l != null).ToArray();
            EditorUtility.SetDirty(stand);
        }

        static void Add(Transform mapRoot, HplDevReliefStand stand)
        {
            var old = mapRoot.Find(RootName);
            if (old != null)
            {
                stand.labels = stand.labels.Where(l => l.anchor != null && !l.anchor.IsChildOf(old)).ToArray();
                stand.inspectionLights = stand.inspectionLights.Where(l => l != null && !l.transform.IsChildOf(old)).ToArray();
                Object.DestroyImmediate(old.gameObject);
            }
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(ManualReliefDevRoomStand.Folder));
            var root = new GameObject(RootName).transform; root.SetParent(mapRoot, false);
            var labels = stand.labels.ToList(); var lights = stand.inspectionLights.ToList();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Module) ?? throw new InvalidOperationException("Missing module: " + Module);
            string maps = ManualWallReliefProbe.MapsFor(Material);
            var info = JsonUtility.FromJson<ManualWallReliefProbe.Info>(File.ReadAllText(Path.Combine(maps, "relief.json")));
            var normal = ManualReliefDevRoomStand.CopyTexture(maps, "dungeonbase", "normal.png", true);
            var mask = ManualReliefDevRoomStand.CopyTexture(maps, "dungeonbase", "mask.png", false);
            var reliefMaterials = new Dictionary<Material, Material>();
            Material Relief(Material classic)
            {
                if (reliefMaterials.TryGetValue(classic, out var found)) return found;
                // Standard Lit with the classic albedo (the atlas has no PBRify set), the relief maps, no POM.
                var material = new Material(Shader.Find("HDRP/Lit")) { name = classic.name + " manual relief" };
                material.SetTexture("_BaseColorMap", classic.GetTexture("_BaseColorMap"));
                material.SetColor("_BaseColor", classic.GetColor("_BaseColor"));
                material.SetTexture("_NormalMap", normal); material.SetFloat("_NormalScale", 1);
                material.SetTexture("_MaskMap", mask);
                material.SetFloat("_MaterialID", 1); material.SetFloat("_DisplacementMode", 0);
                HDMaterial.ValidateMaterial(material);
                return reliefMaterials[classic] = ManualReliefDevRoomStand.Save(material, ManualReliefDevRoomStand.Folder + "/" + classic.name.Replace(' ', '_') + "_manual.mat");
            }

            using var height = new ManualReliefDevRoomStand.HeightMap(Path.Combine(maps, "height.png"), info);
            var samples = new[] { (0, "1 — КЛАССИКА\nкак в игре (POM)", "classic"), (120000, "2 — РЕЛЬЕФ ПО КАРТЕ ВЫСОТЫ HPL\nбюджет треугольников игры", "game"),
                (600000, "3 — ТОТ ЖЕ РЕЛЬЕФ\nсетка 1 см", "fine") };
            for (int i = 0; i < samples.Length; i++)
            {
                var (budget, title, file) = samples[i];
                var module = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                PrefabUtility.UnpackPrefabInstance(module, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                module.name = "dungeonbase default_short " + file;
                module.transform.SetParent(root, false);
                foreach (var collider in module.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                // Face the room (+Z): the module's area-weighted normal, turned about Y.
                var facing = Vector3.zero;
                foreach (var filter in module.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.sharedMesh; var v = mesh.vertices; var t = mesh.triangles;
                    for (int k = 0; k < t.Length; k += 3) facing += filter.transform.TransformDirection(Vector3.Cross(v[t[k + 1]] - v[t[k]], v[t[k + 2]] - v[t[k]]));
                }
                facing.y = 0;
                module.transform.rotation = Quaternion.FromToRotation(facing.normalized, Vector3.forward) * module.transform.rotation;
                var bounds = Bounds(module);
                module.transform.position += new Vector3(FirstX + i * Step - bounds.center.x, -bounds.min.y, FrontWall - bounds.min.z);
                if (budget > 0)
                    foreach (var filter in module.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var renderer = filter.GetComponent<MeshRenderer>();
                        var classic = renderer.sharedMaterials;
                        bool any = classic.Any(m => m != null && m.GetTexture("_BaseColorMap")?.name.StartsWith("dungeonbase__", StringComparison.Ordinal) == true);
                        if (!any) continue;
                        var surfaces = classic.Select(m => new HplReliefMeshBuilder.Surface { geometryNormals = false,
                            heightMetres = m != null && m.GetTexture("_BaseColorMap")?.name.StartsWith("dungeonbase__", StringComparison.Ordinal) == true ? height.Metres : _ => 0 }).ToArray();
                        var mesh = Build(filter.sharedMesh, surfaces, filter.transform.lossyScale, .01f, budget);
                        filter.sharedMesh = ManualReliefDevRoomStand.SaveMesh(mesh, "dungeon_" + file + "_" + filter.name);
                        renderer.sharedMaterials = classic.Select(m => m != null && m.GetTexture("_BaseColorMap")?.name.StartsWith("dungeonbase__", StringComparison.Ordinal) == true ? Relief(m) : m).ToArray();
                        Debug.Log($"[dev_room dungeon relief] {module.name}/{filter.name}: {filter.sharedMesh.vertexCount} vertices, {filter.sharedMesh.triangles.Length / 3} triangles");
                    }
                foreach (var renderer in module.GetComponentsInChildren<MeshRenderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                var anchor = new GameObject(module.name + " label").transform; anchor.SetParent(root, false);
                anchor.position = new Vector3(FirstX + i * Step, bounds.size.y + .35f, FrontWall + .6f);
                labels.Add(new HplDevReliefStand.Label { anchor = anchor, text = title });
            }
            var lamp = new GameObject("Dungeon relief raking light").AddComponent<Light>(); lamp.transform.SetParent(root, false);
            lamp.type = LightType.Spot; lamp.range = 14; lamp.spotAngle = 120;
            lamp.transform.position = new Vector3(FirstX + Step * 3.6f, 3.4f, 2.4f);
            lamp.transform.LookAt(new Vector3(FirstX + Step, 2, FrontWall));
            lamp.shadows = LightShadows.Soft; lamp.gameObject.AddComponent<HDAdditionalLightData>(); lamp.lightUnit = LightUnit.Candela; lamp.intensity = 100;
            lights.Add(lamp);

            stand.labels = labels.ToArray();
            stand.inspectionLights = lights.ToArray();
            EditorUtility.SetDirty(stand);
            AssetDatabase.SaveAssets();
        }

        /// <summary>As the game's generator: a coarser spacing until the budget fits.</summary>
        static Mesh Build(Mesh mesh, HplReliefMeshBuilder.Surface[] surfaces, Vector3 scale, float spacing, int budget)
        {
            for (int i = 0; ; i++)
            {
                try { return HplReliefMeshBuilder.Build(mesh, surfaces, scale, spacing, budget); }
                catch (InvalidOperationException e) when (e.Message.StartsWith("Relief triangle budget:", StringComparison.Ordinal) && i < 8) { spacing *= 1.5f; }
            }
        }

        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        public static void RenderBatch()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Rendering needs a GPU: run without -nographics.");
            Directory.CreateDirectory(Renders);
            var scene = EditorSceneManager.OpenScene(DevRoomBuilder.ScenePath, OpenSceneMode.Single);
            var camera = new GameObject("Dungeon relief render").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.nearClipPlane = .02f; camera.farClipPlane = 80; camera.fieldOfView = 60;
            camera.gameObject.AddComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            try
            {
                void Shot(string name, Vector3 position, Vector3 target)
                {
                    camera.transform.position = position; camera.transform.LookAt(target);
                    ImportMenu.RenderToFile(camera, Path.Combine(Renders, name + ".png"));
                }
                Shot("overview", new Vector3(FirstX + Step, 2.2f, 6.5f), new Vector3(FirstX + Step, 2, FrontWall));
                for (int i = 0; i < 3; i++)
                {
                    float x = FirstX + i * Step;
                    Shot($"close_{i + 1}_low", new Vector3(x + .9f, .9f, FrontWall + 1.1f), new Vector3(x - .2f, .8f, FrontWall));
                    Shot($"close_{i + 1}_high", new Vector3(x + .9f, 2.9f, FrontWall + 1.1f), new Vector3(x - .2f, 3f, FrontWall));
                }
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            }
        }
    }
}
