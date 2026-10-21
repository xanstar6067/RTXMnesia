using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.Importer;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    [InitializeOnLoad]
    public static class EnhancedReliefTests
    {
        const string PlayPending = "RTXMnesia.Relief.PlayPending", OriginalScene = "RTXMnesia.Relief.OriginalScene";
        static int s_phase;
        static double s_next, s_deadline;
        static HplUserSettings s_saved;
        static Mesh s_original;
        static Material s_classic;
        static string s_prefs;
        static EnhancedReliefTests()
        {
            EditorApplication.playModeStateChanged += OnPlayState;
            EditorApplication.update += UpdatePlay;
        }
        static int s_checks;
        static string Folder => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief");
        static void RequireCleanEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run this command outside Play mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save edited scenes before running relief checks.");
        }
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            s_checks++;
            Debug.Log("[EnhancedReliefCheck] ok: " + message);
        }

        [MenuItem("Tools/RTXMnesia/Tests/Enhanced relief: geometry and switching")]
        public static void RunBatch()
        {
            RequireCleanEditor();
            Directory.CreateDirectory(Folder);
            s_checks = 0;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Geometry();
                var catalog = AssetDatabase.LoadAssetAtPath<HplReliefSet>(EnhancedReliefConverter.CatalogPath);
                Check(catalog != null && catalog.entries.Length > 0, "generated geometry catalog exists");
                Check(AssetDatabase.GetDependencies(EnhancedReliefConverter.CatalogPath).All(p => !p.StartsWith(EnhancedReliefConverter.Root + "/", StringComparison.Ordinal)),
                    "detailed/coarse meshes load lazily");
                foreach (var entry in catalog.entries)
                {
                    var detail = Resources.Load<Mesh>(entry.detailedResource);
                    var coarse = Resources.Load<Mesh>(entry.coarseResource);
                    Check(detail != null && coarse != null && detail != entry.original && coarse != entry.original &&
                        detail.subMeshCount == entry.original.subMeshCount && coarse.subMeshCount == entry.original.subMeshCount,
                        "separate valid LOD meshes: " + entry.original.name);
                    foreach (var slot in entry.slots.Where(s => !string.IsNullOrEmpty(s.normalResource)))
                        // Manual wall / floor profiles keep the standard F0: no specular coverage map.
                        Check(Resources.Load<Texture2D>(slot.normalResource) != null && Resources.Load<Texture2D>(slot.maskResource) != null &&
                            (string.IsNullOrEmpty(slot.specularResource) || Resources.Load<Texture2D>(slot.specularResource) != null), "approved surface maps available lazily");
                    Resources.UnloadAsset(detail); Resources.UnloadAsset(coarse);
                }
                foreach (string map in new[] { "00_rainy_hall", "01_old_archives", "05_wine_cellar", "10_daniels_room" })
                {
                    string path = LevelConverter.LevelsRoot + "/maps/main/ch01/" + map + ".unity";
                    byte[] sceneBefore = File.ReadAllBytes(path);
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var meshes = renderers.Where(r => r.TryGetComponent<MeshFilter>(out _)).ToDictionary(r => r.GetComponent<MeshFilter>(), r => r.GetComponent<MeshFilter>().sharedMesh);
                    var materials = renderers.ToDictionary(r => r, r => r.sharedMaterials);
                    var colliders = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshCollider>(true)).ToDictionary(c => c, c => c.sharedMesh);
                    var assets = meshes.Values.Where(m => m != null).Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p)).Distinct()
                        .ToDictionary(p => p, p => EnhancedReliefConverter.Hash(File.ReadAllBytes(p)));
                    for (int cycle = 0; cycle < 3; cycle++)
                    {
                        HplEnhancedTextures.ApplyScene(scene, true);
                        int changed = meshes.Count(p => p.Key.sharedMesh != p.Value);
                        int expected = meshes.Count(p => p.Key.GetComponentInParent<Rigidbody>(true) == null && HplEnhancedRelief.Find(p.Key.GetComponent<Renderer>(), p.Value, materials[p.Key.GetComponent<MeshRenderer>()]) != null);
                        Check(changed == expected, map + ": all cataloged geometry replaced, cycle " + cycle + " (" + changed + ")");
                        Check(meshes.Where(p => p.Key.sharedMesh != p.Value).All(pair =>
                        {
                            var renderer = pair.Key.GetComponent<Renderer>();
                            var current = renderer.sharedMaterials;
                            var entry = HplEnhancedRelief.Find(renderer, pair.Value, current);
                            return entry != null && current.Where((m, i) => entry.slots[i].displaced).All(m => m != null && !m.IsKeywordEnabled("_PIXEL_DISPLACEMENT")) &&
                                current.Select((m, i) => (m, slot: entry.slots[i])).Where(p => !string.IsNullOrEmpty(p.slot.normalResource)).All(p =>
                                    p.m.GetTexture("_NormalMap") == Resources.Load<Texture2D>(p.slot.normalResource) &&
                                    p.m.GetTexture("_MaskMap") == Resources.Load<Texture2D>(p.slot.maskResource) &&
                                    (string.IsNullOrEmpty(p.slot.specularResource) || p.m.GetTexture("_SpecularColorMap") == Resources.Load<Texture2D>(p.slot.specularResource) &&
                                    p.m.IsKeywordEnabled("_MATERIAL_FEATURE_SPECULAR_COLOR")));
                        }), map + ": no POM on displaced material slots");
                        Check(colliders.All(p => p.Key.sharedMesh == p.Value), map + ": collider meshes untouched");
                        var applied = meshes.Keys.ToDictionary(f => f, f => f.sharedMesh);
                        HplEnhancedTextures.ApplyScene(scene, true);
                        Check(applied.All(p => p.Key.sharedMesh == p.Value), map + ": repeated application does not displace twice");
                        HplEnhancedTextures.ApplyScene(scene, false);
                        Check(meshes.All(p => p.Key.sharedMesh == p.Value) && materials.All(p => p.Key.sharedMaterials.SequenceEqual(p.Value)),
                            map + ": exact classic meshes and material references restored");
                    }
                    // Simulate the scene build processor without saving the scene.
                    new EnhancedReliefBuildProcessor().OnProcessScene(scene, null);
                    Check(renderers.Where(r => r.TryGetComponent<MeshFilter>(out var f) && meshes.TryGetValue(f, out var m) && HplEnhancedRelief.Find(r, m, r.sharedMaterials) != null)
                        .All(r => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.BatchingStatic) == 0), map + ": eligible meshes excluded from static batching");
                    Check(assets.All(p => EnhancedReliefConverter.Hash(File.ReadAllBytes(p.Key)) == p.Value) && File.ReadAllBytes(path).SequenceEqual(sceneBefore),
                        map + ": source mesh files and scene byte-identical");
                    HplEnhancedTextures.Shutdown();
                }
                File.WriteAllText(Path.Combine(Folder, "Checks.txt"), $"PASS: {s_checks} checks\n");
                Debug.Log($"[EnhancedReliefCheck] PASS: {s_checks} checks");
            }
            finally
            {
                HplEnhancedTextures.Shutdown();
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static void Geometry()
        {
            var height = new Texture2D(32, 32, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Repeat };
            var colors = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) colors[y * 32 + x] = Color.white * (0.5f + 0.4f * Mathf.Sin(x * Mathf.PI / 8));
            height.SetPixels(colors); height.Apply();
            var original = new Mesh
            {
                vertices = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
                normals = Enumerable.Repeat(Vector3.forward, 4).ToArray(),
                tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), 4).ToArray(),
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            Mesh result = null;
            try
            {
                var field = new HplReliefMeshBuilder.HeightField(height);
                result = HplReliefMeshBuilder.Build(original, new[] { new HplReliefMeshBuilder.Surface { height = field, amplitude = 0.04f } }, new Vector3(2, 1, 3), 0.06f);
                var p = result.vertices;
                Check(result.vertexCount > 100 && result.triangles.Length > original.triangles.Length, "height produces actual subdivided triangles");
                Check(p.Any(v => v.z > 0.001f) && p.Any(v => v.z < -0.001f), "both raised faces and recessed joints");
                Check(p.All(v => Mathf.Abs(v.z * 3) <= 0.0281f), "displacement remains in physical metres with nonuniform scale");
                Check(p.Where(v => v.x < 0.00001f || v.x > 0.99999f || v.y < 0.00001f || v.y > 0.99999f).All(v => Mathf.Abs(v.z) < 0.000001f), "module boundaries pinned without cracks");
                Check(result.normals.All(n => n == Vector3.forward) && result.uv.All(uv => uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1),
                    "authored tangent frame and interpolated UVs preserved");
                var counts = new Dictionary<(int, int), int>();
                var triangles = result.triangles;
                bool winding = true;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    winding &= Vector3.Cross(p[b] - p[a], p[c] - p[a]).z > 0;
                    void Edge(int x, int y) { var key = (Mathf.Min(x, y), Mathf.Max(x, y)); counts.TryGetValue(key, out int n); counts[key] = n + 1; }
                    Edge(a, b); Edge(b, c); Edge(c, a);
                }
                Check(winding, "triangle winding preserved");
                Check(counts.Where(e => e.Value == 1).All(e =>
                {
                    Vector3 a = p[e.Key.Item1], b = p[e.Key.Item2];
                    return (a.x == 0 && b.x == 0) || (a.x == 1 && b.x == 1) || (a.y == 0 && b.y == 0) || (a.y == 1 && b.y == 1);
                }) && counts.All(e => e.Value <= 2), "conforming subdivision has no internal T junctions or nonmanifold edges");
                Check(original.vertexCount == 4 && original.vertices.All(v => v.z == 0), "builder never modifies original mesh");
                var boards = HplReliefMeshBuilder.Build(original, new[] { new HplReliefMeshBuilder.Surface { boards = true,
                    uvScale = new Vector2(-2, 1.5f), uvOffset = new Vector2(-.3f, .2f) } }, new Vector3(2, 1, 3), .02f);
                try
                {
                    Check(boards.vertexCount > 100 && boards.vertexCount < 20000, "UV-clipped planks support mirrored, offset and repeated UVs without dense tessellation");
                    Check(boards.normals.Any(n => Mathf.Abs(n.z) < .1f) && boards.normals.Any(n => n.z > .99f), "planks have real hard vertical sides and flat faces");
                    Check(boards.vertices.All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && v.z * 3 > -.0061f && v.z * 3 < .0106f), "plank thickness stays in metres under nonuniform scale");
                    Check(boards.tangents.All(t => float.IsFinite(t.x) && float.IsFinite(t.y) && float.IsFinite(t.z)), "plank face and side tangent frames remain finite");
                }
                finally { Object.DestroyImmediate(boards); }
            }
            finally { Object.DestroyImmediate(result); Object.DestroyImmediate(original); Object.DestroyImmediate(height); }
        }

        [MenuItem("Tools/RTXMnesia/Tests/Enhanced relief: render wall floor boards")]
        public static void RunRenderBatch()
        {
            RequireCleanEditor();
            Directory.CreateDirectory(Folder);
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<HplReliefSet>(EnhancedReliefConverter.CatalogPath);
                // The castle atlas is shared by pillars: test its actual wall module in the level below.
                foreach (string name in new[] { "castlebase_floor", "cellarbase_wood_floor" })
                {
                    var entry = catalog.entries.Where(e => e.original.subMeshCount == 1 && e.slots[0].albedo != null && e.slots[0].albedo.name == name + "__albedo")
                        .OrderBy(e => Mathf.Abs(Vector3.Scale(e.original.bounds.size, e.objectScale).magnitude - 4)).First();
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var root = new GameObject("Relief sample " + name);
                    var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = entry.original;
                    var renderer = root.AddComponent<MeshRenderer>();
                    var classic = Resources.Load<HplTextureSet>(HplTextureSet.ResourceName).entries.First(e => e.classic.GetTexture("_BaseColorMap") == entry.slots[0].albedo).classic;
                    renderer.sharedMaterial = classic;
                    root.transform.localScale = entry.objectScale;
                    var vertices = entry.original.vertices; var triangles = entry.original.triangles;
                    int largest = 0; float area = 0;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        float a = Vector3.Cross(Vector3.Scale(vertices[triangles[i + 1]] - vertices[triangles[i]], entry.objectScale),
                            Vector3.Scale(vertices[triangles[i + 2]] - vertices[triangles[i]], entry.objectScale)).sqrMagnitude;
                        if (a > area) { area = a; largest = i; }
                    }
                    var normal = entry.original.normals[triangles[largest]];
                    root.transform.rotation = Quaternion.FromToRotation(new Vector3(normal.x / entry.objectScale.x, normal.y / entry.objectScale.y, normal.z / entry.objectScale.z).normalized, Vector3.back);
                    root.transform.position = -(root.transform.rotation * Vector3.Scale(entry.original.bounds.center, entry.objectScale));
                    var camera = new GameObject("Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
                    camera.nearClipPlane = 0.03f; camera.farClipPlane = 100; camera.fieldOfView = 55;
                    camera.transform.position = new Vector3(0.9f, 0.15f, -1.15f); camera.transform.LookAt(Vector3.zero);
                    var hd = camera.gameObject.AddComponent<HDAdditionalCameraData>(); hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
                    hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color; hd.backgroundColorHDR = new Color(0.015f, 0.015f, 0.015f);
                    var volume = new GameObject("Exposure").AddComponent<Volume>(); volume.isGlobal = true; volume.priority = 10000;
                    var profile = ScriptableObject.CreateInstance<VolumeProfile>(); volume.sharedProfile = profile;
                    var exposure = profile.Add<Exposure>(true); exposure.mode.Override(ExposureMode.Fixed); exposure.fixedExposure.Override(1);
                    profile.Add<VisualEnvironment>(true).skyType.Override(0);
                    profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                    var light = new GameObject("Raking point light").AddComponent<Light>(); light.type = LightType.Point;
                    light.transform.position = new Vector3(-1, 0.4f, -0.25f); light.range = 10; light.shadows = LightShadows.Soft;
                    light.gameObject.AddComponent<HDAdditionalLightData>(); light.lightUnit = LightUnit.Candela; light.intensity = 60;
                    var fill = new GameObject("Soft frontal fill").AddComponent<Light>(); fill.type = LightType.Directional;
                    fill.transform.rotation = Quaternion.LookRotation(Vector3.forward);
                    fill.gameObject.AddComponent<HDAdditionalLightData>(); fill.lightUnit = LightUnit.Lux; fill.intensity = 40;
                    HplEnhancedTextures.Apply(renderer, true);
                    var detailed = filter.sharedMesh;
                    Check(detailed != entry.original, name + ": render uses real relief");
                    Color[] before = null, after = null;
                    // Same improved textures and tangent normals in both images; only the physical shape changes.
                    filter.sharedMesh = entry.original;
                    ImportMenu.RenderToFile(camera, Path.Combine(Folder, name + "_0_flat.png"), t => before = t.GetPixels());
                    filter.sharedMesh = detailed;
                    ImportMenu.RenderToFile(camera, Path.Combine(Folder, name + "_1_relief.png"), t => after = t.GetPixels());
                    Check(before.Zip(after, (a, b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b)).Average() > 0.0001,
                        name + ": GPU image changes with geometry alone");
                    HplEnhancedTextures.Apply(renderer, false);
                    Check(filter.sharedMesh == entry.original && renderer.sharedMaterial == classic, name + ": render restores classic geometry/material");
                    HplEnhancedTextures.Shutdown();
                    volume.sharedProfile = null; Object.DestroyImmediate(profile);
                }
                ApprovedLevelReliefTests.RunBatch();
                File.WriteAllText(Path.Combine(Folder, "Render.txt"), "GPU PASS: actual Old Archives wall module and level, stone floor, wood floor; flat/relief pairs\n");
                Debug.Log("[EnhancedReliefCheck] GPU PASS");
            }
            finally
            {
                HplEnhancedTextures.Shutdown();
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path))) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static string PlayScene(int index) => AmnesiaPaths.ImportedAssetsRoot + "/Preview/Relief/ReliefPlay" + index + ".unity";

        [MenuItem("Tools/RTXMnesia/Tests/Enhanced relief: Play switching and LOD")]
        public static void RunPlayBatch()
        {
            RequireCleanEditor();
            if (SceneManager.sceneCount != 1 || string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                throw new InvalidOperationException("Play relief check requires one saved scene; the original scene will be restored.");
            string originalScene = SceneManager.GetActiveScene().path;
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(AmnesiaPaths.ImportedAssetsRoot + "/Preview/Relief"));
            AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<HplReliefSet>(EnhancedReliefConverter.CatalogPath);
            for (int i = 0; i < 2; i++)
            {
                var entry = catalog.entries.First(e => e.original.subMeshCount == 1 && e.slots[0].albedo != null &&
                    e.slots[0].albedo.name == (i == 0 ? "castlebase_wall__albedo" : "cellarbase_wood_floor__albedo"));
                var material = Resources.Load<HplTextureSet>(HplTextureSet.ResourceName).entries.First(e => e.classic.GetTexture("_BaseColorMap") == entry.slots[0].albedo).classic;
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Relief Play sample");
                root.transform.localScale = entry.objectScale;
                var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = entry.original;
                root.AddComponent<MeshRenderer>().sharedMaterial = material;
                root.AddComponent<MeshCollider>().sharedMesh = entry.original;
                GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
                var camera = new GameObject("Relief camera").AddComponent<Camera>(); camera.tag = "MainCamera";
                camera.transform.position = root.GetComponent<Renderer>().bounds.center + Vector3.back;
                camera.gameObject.AddComponent<HDAdditionalCameraData>();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), PlayScene(i));
            }
            EditorSceneManager.OpenScene(PlayScene(0), OpenSceneMode.Single);
            SessionState.SetString(OriginalScene, originalScene);
            SessionState.SetBool(PlayPending, true);
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "Play.txt"), "RUNNING\n");
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PlayPending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                s_saved = HplUserSettings.ReadSaved();
                s_prefs = PlayerPrefs.GetString(HplUserSettings.PrefsKey, "");
                s_phase = 0; s_next = EditorApplication.timeSinceStartup + 1;
                s_deadline = EditorApplication.timeSinceStartup + 90;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(PlayPending, false);
                string scene = SessionState.GetString(OriginalScene, "");
                if (!string.IsNullOrEmpty(scene)) EditorApplication.delayCall += () => EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            }
        }

        static void UpdatePlay()
        {
            if (!SessionState.GetBool(PlayPending, false) || !EditorApplication.isPlaying || s_saved == null || EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (EditorApplication.timeSinceStartup > s_deadline) throw new InvalidOperationException("Relief Play check timed out.");
                var root = GameObject.Find("Relief Play sample");
                if (root == null) { s_next = EditorApplication.timeSinceStartup + 0.5; return; }
                var renderer = root.GetComponent<Renderer>(); var filter = root.GetComponent<MeshFilter>();
                var settings = s_saved.Copy();
                switch (s_phase)
                {
                    case 0:
                        settings.textures = 0; HplGraphics.Apply(settings);
                        s_original = filter.sharedMesh; s_classic = renderer.sharedMaterial;
                        var block = new MaterialPropertyBlock(); block.SetFloat("_ReliefForeign", 0.43f); renderer.SetPropertyBlock(block);
                        settings.textures = 1; HplGraphics.Apply(settings);
                        Check(filter.sharedMesh != s_original && HplEnhancedRelief.ActiveMeshCount > 0, "Play: graphics event replaces actual mesh");
                        Check(!renderer.sharedMaterial.IsKeywordEnabled("_PIXEL_DISPLACEMENT"), "Play: physical geometry does not receive POM twice");
                        Check(root.GetComponent<MeshCollider>().sharedMesh == s_original, "Play: original collision mesh retained");
                        renderer.GetPropertyBlock(block); Check(Mathf.Approximately(block.GetFloat("_ReliefForeign"), 0.43f), "Play: foreign property block preserved");
                        Camera.main.transform.position = renderer.bounds.center + Vector3.back * 100;
                        break;
                    case 1:
                        var entry = HplEnhancedRelief.Find(renderer, s_original, renderer.sharedMaterials);
                        Check(filter.sharedMesh == Resources.Load<Mesh>(entry.coarseResource), "Play: distance selects coarse mesh");
                        Camera.main.transform.position = renderer.bounds.center + Vector3.back;
                        break;
                    case 2:
                        var nearEntry = HplEnhancedRelief.Find(renderer, s_original, renderer.sharedMaterials);
                        Check(filter.sharedMesh == Resources.Load<Mesh>(nearEntry.detailedResource), "Play: approaching restores detailed mesh");
                        settings.textures = 0; HplGraphics.Apply(settings);
                        Check(filter.sharedMesh == s_original && renderer.sharedMaterial == s_classic && HplEnhancedRelief.ActiveMeshCount == 0, "Play: classic restores exact references and releases geometry cache");
                        settings.textures = 1; HplGraphics.Apply(settings);
                        EditorSceneManager.LoadSceneInPlayMode(PlayScene(1), new LoadSceneParameters(LoadSceneMode.Single));
                        break;
                    case 3:
                        Check(HplEnhancedRelief.ActiveMeshCount > 0 && filter.sharedMesh != root.GetComponent<MeshCollider>().sharedMesh, "Play: newly loaded scene inherits enhanced geometry");
                        settings.textures = 0; HplGraphics.Apply(settings);
                        Check(filter.sharedMesh == root.GetComponent<MeshCollider>().sharedMesh, "Play: second scene returns to classic");
                        Check(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == s_prefs, "Play: saved user preferences untouched");
                        File.WriteAllText(Path.Combine(Folder, "Play.txt"), "PLAY PASS: settings events, physical meshes, both LODs, unchanged colliders/preferences, scene transition, classic restoration\n");
                        HplGraphics.Apply(s_saved);
                        s_saved = null;
                        EditorApplication.ExitPlaymode();
                        return;
                }
                s_phase++; s_next = EditorApplication.timeSinceStartup + 1.5;
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Combine(Folder, "Play.txt"), "FAIL\n" + e);
                Debug.LogException(e);
                if (s_saved != null) HplGraphics.Apply(s_saved);
                s_saved = null;
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
