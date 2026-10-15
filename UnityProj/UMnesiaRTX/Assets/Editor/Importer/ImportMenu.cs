using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Importer
{
    /// <summary>Menu entries and batch-mode entry points of the Amnesia importers.</summary>
    public static class ImportMenu
    {
        const string Menu = "Tools/RTXMnesia/Import/";
        const string TestFolder = "static_objects/castlebase";
        const string PreviewFolder = AmnesiaPaths.ImportedAssetsRoot + "/Preview";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";
        const string TestMapName = "00_rainy_hall";
        internal const string TestMap = "maps/main/ch01/" + TestMapName + ".map";

        enum Kind { Materials, Models }

        // ---------- Menu ----------

        [MenuItem(Menu + "Materials: test set (castlebase)", priority = 0)]
        static void MaterialsTestSet() => Convert(Kind.Materials, TestFolder, false);

        [MenuItem(Menu + "Materials: choose folder...", priority = 1)]
        static void MaterialsChosenFolder() => ConvertChosenFolder(Kind.Materials);

        [MenuItem(Menu + "Materials: all", priority = 2)]
        static void MaterialsAll()
        {
            if (Confirm("Convert all ~850 materials and their textures? This can take a long time."))
                Convert(Kind.Materials, "", false);
        }

        [MenuItem(Menu + "Materials: all, force textures", priority = 3)]
        static void MaterialsAllForce()
        {
            if (Confirm("Regenerate ALL textures from the original files?"))
                Convert(Kind.Materials, "", true);
        }

        [MenuItem(Menu + "Models: test set (castlebase)", priority = 10)]
        static void ModelsTestSet() => Convert(Kind.Models, TestFolder, false);

        [MenuItem(Menu + "Models: choose folder...", priority = 11)]
        static void ModelsChosenFolder() => ConvertChosenFolder(Kind.Models);

        [MenuItem(Menu + "Preview: materials (castlebase)", priority = 20)]
        static void PreviewMaterials() => BuildPreview(Kind.Materials, TestFolder);

        [MenuItem(Menu + "Preview: models (castlebase)", priority = 21)]
        static void PreviewModels() => BuildPreview(Kind.Models, TestFolder);

        [MenuItem(Menu + "Level: " + TestMapName + " (ch01)", priority = 30)]
        static void LevelTestMap() => ConvertLevel(TestMap);

        [MenuItem(Menu + "Level: choose .map...", priority = 31)]
        static void LevelChosenMap()
        {
            string abs = EditorUtility.OpenFilePanel("Map inside source/maps", Path.Combine(AmnesiaPaths.SourceRoot, "maps"), "map");
            if (string.IsNullOrEmpty(abs)) return;
            ConvertLevel(AmnesiaPaths.ToSourceRelative(abs));
        }

        [MenuItem(Menu + "Sounds (sounds, music, voices)", priority = 35)]
        static void Sounds()
        {
            var log = new ImportLog();
            SoundConverter.Convert(log, LanguageConverter.Convert(LanguageConverter.DefaultLanguage, log));
            log.Save("Sounds");
        }

        public static void ConvertSoundsBatch() => Sounds();

        [MenuItem(Menu + "Particles (.ps)", priority = 36)]
        static void Particles()
        {
            var log = new ImportLog();
            ParticleConverter.Convert(log);
            log.Save("Particles");
        }

        public static void ConvertParticlesBatch() => Particles();

        [MenuItem(Menu + "Open import log", priority = 40)]
        static void OpenLog()
        {
            if (File.Exists(AmnesiaPaths.LogFile)) EditorUtility.OpenWithDefaultApp(AmnesiaPaths.LogFile);
            else EditorUtility.DisplayDialog("RTXMnesia", "No import log yet.", "OK");
        }

        // ---------- Batch mode ----------
        // Unity.exe -batchmode -quit -projectPath ... -executeMethod RTXMnesia.Importer.ImportMenu.<Method>
        //   [-amnesiaFolder static_objects/castlebase] [-forceTextures]
        // Render* methods need a GPU: run them without -nographics. Results: Logs/AmnesiaImport.log, Logs/*Preview.png.

        //   ConvertLevelBatch / RenderLevelBatch [-amnesiaMap maps/main/ch01/00_rainy_hall.map] [-convert]
        //   DescribeModelsBatch -describe static_objects/a/b.dae,entities/c/d.dae (already imported models)

        public static void ConvertMaterialsBatch() => Convert(Kind.Materials, ArgFolder(), HasArg("-forceTextures"));
        public static void ConvertModelsBatch() => Convert(Kind.Models, ArgFolder(), false);
        public static void RenderMaterialsBatch() => RenderPreview(Kind.Materials, ArgFolder());
        public static void RenderModelsBatch() => RenderPreview(Kind.Models, ArgFolder());
        public static void ConvertLevelBatch() => ConvertLevel(Arg("-amnesiaMap", TestMap));
        public static void RenderLevelBatch() => RenderLevel(Arg("-amnesiaMap", TestMap), HasArg("-convert"));

        public static void DescribeModelsBatch()
        {
            var log = new ImportLog();
            foreach (string path in Arg("-describe", "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(AmnesiaPaths.ModelsRoot + "/" + path.Trim());
                log.Info(model != null ? ModelConverter.DescribeHierarchy(model) : $"{path}: not imported");
            }
            log.Save("Describe models");
        }

        static string ArgFolder() => Arg("-amnesiaFolder", TestFolder);

        internal static string Arg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : fallback;
        }

        internal static bool HasArg(string name) => Environment.GetCommandLineArgs().Contains(name);

        // ---------- Implementation ----------

        static bool Confirm(string message) =>
            Application.isBatchMode || EditorUtility.DisplayDialog("RTXMnesia", message, "OK", "Cancel");

        static void ConvertChosenFolder(Kind kind)
        {
            string abs = EditorUtility.OpenFolderPanel("Folder inside source/", AmnesiaPaths.SourceRoot, "");
            if (string.IsNullOrEmpty(abs)) return;
            string rel = AmnesiaPaths.ToSourceRelative(abs);
            if (rel.StartsWith("..")) { EditorUtility.DisplayDialog("RTXMnesia", "Choose a folder inside source/.", "OK"); return; }
            Convert(kind, rel == "." ? "" : rel, false);
        }

        /// <param name="relativeFolder">Folder relative to source/; "" = everything.</param>
        static void Convert(Kind kind, string relativeFolder, bool forceTextures)
        {
            string abs = Path.Combine(AmnesiaPaths.SourceRoot, relativeFolder);
            if (!Directory.Exists(abs))
            {
                Debug.LogError($"[RTXMnesia] Folder not found: {abs}. Run tools/Copy-AmnesiaResources.ps1 first.");
                return;
            }
            string name = relativeFolder.Length > 0 ? relativeFolder : "all";
            List<string> files = Directory.GetFiles(abs, kind == Kind.Materials ? "*.mat" : "*.dae", SearchOption.AllDirectories)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();

            if (kind == Kind.Materials) MaterialConverter.ConvertMaterials(files, forceTextures, $"Materials: {name}");
            else ModelConverter.ConvertModels(files, $"Models: {name}");
        }

        static string PreviewScenePath(Kind kind) => $"{PreviewFolder}/{kind}Preview.unity";

        /// <summary>A grid of every converted material (on cubes) or model of a folder.</summary>
        static void BuildPreview(Kind kind, string relativeFolder)
        {
            string folder = (kind == Kind.Materials ? AmnesiaPaths.MaterialsRoot : AmnesiaPaths.ModelsRoot) + "/" + relativeFolder;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog("RTXMnesia", $"Convert first ({folder} does not exist).", "OK");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string filter = kind == Kind.Materials ? "t:Material" : "t:Model";
            var assets = AssetDatabase.FindAssets(filter, new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();

            // Start from SampleScene: HDRP camera, sun, sky and fixed exposure are already set up there.
            var scene = File.Exists(AmnesiaPaths.ToAbsolute(SampleScene))
                ? EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var items = new List<GameObject>();
            foreach (string path in assets)
            {
                GameObject go;
                if (kind == Kind.Materials)
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.localScale = Vector3.one * 2f;
                    go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                else
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                }
                go.name = Path.GetFileNameWithoutExtension(path);
                items.Add(go);
            }

            // Grid with cell size from the largest item, so big wall pieces do not overlap.
            float cell = 2.5f;
            foreach (var go in items) cell = Mathf.Max(cell, Size(go) + 0.5f);
            int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(items.Count)));
            for (int i = 0; i < items.Count; i++)
            {
                var go = items[i];
                var bounds = WorldBounds(go);
                var target = new Vector3(i % columns * cell, 0f, i / columns * cell);
                go.transform.position += target - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }

            float center = (columns - 1) * cell * 0.5f;
            var light = new GameObject("Point Light (move me)").AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = new Vector3(center, 4f, center);
            light.range = 30f;
            light.gameObject.AddComponent<HDAdditionalLightData>();

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(center, center * 1.1f + 4f, -center * 0.6f - 3f);
                camera.transform.LookAt(new Vector3(center, 1f, center));
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, center * 6f);
            }

            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(PreviewFolder));
            EditorSceneManager.SaveScene(scene, PreviewScenePath(kind));
            Debug.Log($"[RTXMnesia] Preview scene with {items.Count} {kind.ToString().ToLowerInvariant()}: {PreviewScenePath(kind)}");
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        static float Size(GameObject go)
        {
            var s = WorldBounds(go).size;
            return Mathf.Max(s.x, s.z);
        }

        /// <summary>Builds the preview scene and renders it to Logs/&lt;Kind&gt;Preview.png.</summary>
        static void RenderPreview(Kind kind, string relativeFolder)
        {
            BuildPreview(kind, relativeFolder);
            var camera = Camera.main;
            if (camera == null) { Debug.LogError("[RTXMnesia] Preview scene has no main camera"); return; }
            RenderToFile(camera, Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", $"{kind}Preview.png"));
        }

        internal static void RenderToFile(Camera camera, string file, Action<Texture2D> compose = null)
        {
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = rt;
            // Several frames so HDRP history (exposure, TAA) settles.
            for (int i = 0; i < 30; i++) camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            compose?.Invoke(image);

            File.WriteAllBytes(file, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            rt.Release();
            Debug.Log($"[RTXMnesia] Rendered: {file}");
        }

        /// <param name="mapRelative">Map path relative to source/, e.g. maps/main/ch01/00_rainy_hall.map.</param>
        static void ConvertLevel(string mapRelative)
        {
            string abs = Path.Combine(AmnesiaPaths.SourceRoot, mapRelative);
            if (!File.Exists(abs))
            {
                Debug.LogError($"[RTXMnesia] Map not found: {abs}. Run tools/Copy-AmnesiaResources.ps1 first.");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string scene = LevelConverter.Convert(abs);
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(scene);
        }

        /// <summary>
        /// Renders the level from its camera and from every PlayerStart area to Logs/Level_&lt;map&gt;_&lt;n&gt;.png.
        /// Extra viewpoints: -views "x,y,z,yaw,pitch;..." in Unity coordinates.
        /// </summary>
        static void RenderLevel(string mapRelative, bool convert)
        {
            string abs = Path.Combine(AmnesiaPaths.SourceRoot, mapRelative);
            string scenePath = LevelConverter.ScenePathFor(abs);
            if (convert || !File.Exists(AmnesiaPaths.ToAbsolute(scenePath))) LevelConverter.Convert(abs);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var camera = Camera.main;
            if (camera == null) { Debug.LogError("[RTXMnesia] Level scene has no main camera"); return; }
            var views = new List<(string name, Vector3 position, Quaternion rotation)>
            {
                ("camera", camera.transform.position, camera.transform.rotation),
            };
            foreach (var area in UnityEngine.Object.FindObjectsByType<RTXMnesia.World.AmnesiaArea>(FindObjectsInactive.Include))
                if (area.areaType == "PlayerStart")
                    views.Add((area.name, area.transform.position + Vector3.up * 1.75f,
                        Quaternion.LookRotation(Vector3.ProjectOnPlane(area.transform.forward, Vector3.up))));
            foreach (string view in Arg("-views", "").Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                float[] v = HplXml.ParseFloats(view.Replace(',', ' '));
                if (v.Length >= 5) views.Add(($"view{views.Count}", new Vector3(v[0], v[1], v[2]), Quaternion.Euler(v[4], v[3], 0f)));
            }

            string mapName = Path.GetFileNameWithoutExtension(mapRelative);
            string[] picks = Arg("-pick", "").Split(';', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < views.Count; i++)
            {
                camera.transform.SetPositionAndRotation(views[i].position, views[i].rotation);
                RenderToFile(camera, Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", $"Level_{mapName}_{i:00}_{views[i].name}.png"));
                if (views[i].name.StartsWith("view"))
                    foreach (string pick in picks) Pick(camera, views[i].name, HplXml.ParseFloats(pick.Replace(',', ' ')));
            }
        }

        /// <summary>
        /// -pick "u,v;...": the renderers whose bounds the ray through the image point (fractions, v from the top)
        /// of every extra view crosses, nearest first, with their materials (finding what an odd pixel is).
        /// </summary>
        static void Pick(Camera camera, string view, float[] uv)
        {
            if (uv.Length < 2) return;
            camera.aspect = 16f / 9f;
            var ray = camera.ViewportPointToRay(new Vector3(uv[0], 1f - uv[1], 0f));
            var hits = new List<(float distance, Renderer renderer)>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                if (r.enabled && r.bounds.IntersectRay(ray, out float d) && d < 30f) hits.Add((d, r));
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            var sb = new System.Text.StringBuilder($"[RTXMnesia] Pick {view} ({uv[0]:0.##}, {uv[1]:0.##}):");
            for (int k = 0; k < Math.Min(8, hits.Count); k++)
            {
                var r = hits[k].renderer;
                var materials = string.Join(", ", Array.ConvertAll(r.sharedMaterials,
                    m => m == null ? "null" : $"{m.name} [{m.shader.name}, q{m.renderQueue}]"));
                sb.Append($"\n  {hits[k].distance:0.00} m  {Hierarchy(r.transform)}  {materials}");
            }
            Debug.Log(sb.ToString());
        }

        static string Hierarchy(Transform t) => t.parent == null ? t.name : Hierarchy(t.parent) + "/" + t.name;
    }
}
