using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RTXMnesia.Importer
{
    /// <summary>Generates independent visual meshes for selected architectural materials; never saves source scenes.</summary>
    public static class EnhancedReliefConverter
    {
        public const string Root = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplReliefMeshes";
        public const string CatalogPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + HplReliefSet.ResourceName + ".asset";
        const string CacheRoot = AmnesiaPaths.ImportedAssetsRoot + "/EnhancedReliefCache";
        static IEnumerator<object> s_work;
        static int s_progressId = -1;
        static float s_progress;
        static string s_description;
        public static bool IsGenerating => s_work != null;
        public static readonly Dictionary<string, float> Profiles = new(StringComparer.Ordinal)
        {
            ["static_objects/castlebase/wall/castlebase_wall.mat"] = 0.045f,
            ["static_objects/castlebase/wall/castlebase_wall_worn.mat"] = 0.035f,
            ["static_objects/castlebase/wall/castlebase_wall_square_bricks.mat"] = 0.035f,
            ["static_objects/castlebase/castlebase_floor.mat"] = 0.018f,
            ["static_objects/castlebase/castlebase_tiledfloor.mat"] = 0.012f,
            ["static_objects/cellarbase/floor/cellarbase_floor.mat"] = 0.025f,
            ["static_objects/cellarbase/floor/cellarbase_floor_alt.mat"] = 0.025f,
            ["static_objects/cellarbase/floor/cellarbase_wood_floor.mat"] = 0.008f,
            ["static_objects/mansionbase/floor/mansionbase_floor_boards.mat"] = 0.006f,
            ["static_objects/dungeonbase/floor/cobbled_stone.mat"] = 0.035f,
        };
        sealed class Profile
        {
            public Texture albedo;
            public HplReliefMeshBuilder.HeightField height;
            public float amplitude;
            public string hash;
        }
        public static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static string Hash(string text) => Hash(Encoding.UTF8.GetBytes(text));
        static string F(float value) => value.ToString("0.0000", CultureInfo.InvariantCulture);
        static string V(Vector2 v) => F(v.x) + "," + F(v.y);
        static string Id(Object asset)
        {
            if (asset == null) return "null";
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long local))
                throw new InvalidOperationException("Relief source must be a saved asset: " + asset.name);
            return guid + ":" + local;
        }

        [MenuItem("Tools/RTXMnesia/Import/Enhanced relief: generate architectural meshes")]
        public static void ConvertBatch() => Start(false);

        [MenuItem("Tools/RTXMnesia/Import/Enhanced relief: first wall floor boards samples")]
        public static void ConvertSamplesBatch() => Start(true);

        static void Start(bool samplesOnly)
        {
            if (Application.isBatchMode)
            {
                using var work = Generate(samplesOnly);
                while (work.MoveNext()) { }
                return;
            }
            if (s_work != null) throw new InvalidOperationException("Relief generation is already running.");
            s_work = Generate(samplesOnly);
            s_progress = 0; s_description = "Preparing height maps";
            s_progressId = Progress.Start("RTXMnesia: рельеф улучшенных поверхностей", s_description);
            Progress.RegisterCancelCallback(s_progressId, () => { Cancel(); return true; });
            EditorApplication.update += Step;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            EditorApplication.wantsToQuit += OnQuit;
        }

        static bool OnQuit() { Cancel(); return true; }
        static void Step()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Cancel(); return; }
            try
            {
                if (s_work.MoveNext()) Progress.Report(s_progressId, s_progress, s_description);
                else Finish(Progress.Status.Succeeded);
            }
            catch (Exception e) { Debug.LogException(e); Finish(Progress.Status.Failed); }
        }
        [MenuItem("Tools/RTXMnesia/Import/Enhanced relief: cancel generation")]
        public static void Cancel() => Finish(Progress.Status.Canceled);
        static void Finish(Progress.Status status)
        {
            EditorApplication.update -= Step;
            AssemblyReloadEvents.beforeAssemblyReload -= Cancel;
            EditorApplication.wantsToQuit -= OnQuit;
            var work = s_work; s_work = null;
            try { work?.Dispose(); }
            finally { if (s_progressId >= 0) Progress.Finish(s_progressId, status); s_progressId = -1; }
        }

        static IEnumerator<object> Generate(bool samplesOnly)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Generate relief outside Play mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save edited scenes before generating relief.");
            HplEnhancedTextures.Shutdown();
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Root));
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(CacheRoot));
            AssetDatabase.Refresh();
            var profiles = LoadProfiles();
            var entries = new Dictionary<string, HplReliefSet.Entry>();
            var sampleTextures = new HashSet<Texture>();
            var report = new List<string>();
            int instances = 0, generated = 0;
            Scene scanScene = default;
            bool ownsScene = false;
            try
            {
                string[] scenes = Directory.GetFiles(AmnesiaPaths.ToAbsolute(LevelConverter.LevelsRoot), "*.unity", SearchOption.AllDirectories)
                    .Select(p => LevelConverter.LevelsRoot + "/" + Path.GetRelativePath(AmnesiaPaths.ToAbsolute(LevelConverter.LevelsRoot), p).Replace('\\', '/'))
                    .Where(p => p.Contains("/maps/main/") || p.Contains("/maps/ptest/") || p.Contains("/main_menu/")).OrderBy(p => p, StringComparer.Ordinal).ToArray();
                if (samplesOnly) scenes = scenes.Where(p => new[] { "00_rainy_hall", "05_wine_cellar", "10_daniels_room" }.Contains(Path.GetFileNameWithoutExtension(p))).ToArray();
                for (int sceneIndex = 0; sceneIndex < scenes.Length; sceneIndex++)
                {
                    string path = scenes[sceneIndex];
                    scanScene = SceneManager.GetSceneByPath(path);
                    ownsScene = !scanScene.IsValid() || !scanScene.isLoaded;
                    if (ownsScene)
                    {
                        // Keep the user's scene open and active. Temporary scan scenes never get saved.
                        scanScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                        foreach (var root in scanScene.GetRootGameObjects()) root.SetActive(false);
                    }
                    int count = 0;
                    long triangles = 0;
                    var renderers = scanScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    {
                        var renderer = renderers[rendererIndex];
                        if (renderer == null) continue;
                        if (renderer.GetComponentInParent<Rigidbody>(true) != null || !renderer.TryGetComponent<MeshFilter>(out var filter)) continue;
                        var mesh = filter.sharedMesh;
                        var materials = renderer.sharedMaterials;
                        if (mesh == null || materials.Length != mesh.subMeshCount) continue;
                        var scale = HplReliefSet.ScaleOf(renderer.transform);
                        if (Mathf.Min(scale.x, scale.y, scale.z) < 0.0001f) continue;
                        var matrix = renderer.localToWorldMatrix;
                        if (Mathf.Abs(Vector3.Dot(matrix.GetColumn(0).normalized, matrix.GetColumn(1).normalized)) > 0.001f ||
                            Mathf.Abs(Vector3.Dot(matrix.GetColumn(0).normalized, matrix.GetColumn(2).normalized)) > 0.001f ||
                            Mathf.Abs(Vector3.Dot(matrix.GetColumn(1).normalized, matrix.GetColumn(2).normalized)) > 0.001f) continue;
                        var surfaces = new HplReliefMeshBuilder.Surface[materials.Length];
                        var slots = new HplReliefSet.Slot[materials.Length];
                        var key = new StringBuilder(Id(mesh) + "|" + F(scale.x) + "," + F(scale.y) + "," + F(scale.z));
                        var signature = new StringBuilder("relief-v" + HplReliefMeshBuilder.Version + "|" + AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(mesh)));
                        bool any = false;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            var material = materials[i];
                            var texture = material != null && material.HasProperty("_BaseColorMap") ? material.GetTexture("_BaseColorMap") : null;
                            slots[i] = new HplReliefSet.Slot { albedo = texture,
                                scale = texture != null ? material.GetTextureScale("_BaseColorMap") : Vector2.one,
                                offset = texture != null ? material.GetTextureOffset("_BaseColorMap") : Vector2.zero };
                            key.Append("|" + Id(texture) + ":" + V(slots[i].scale) + ":" + V(slots[i].offset));
                            if (texture == null || !profiles.TryGetValue(texture, out var profile) || material.shader.name != "HDRP/Lit" ||
                                material.GetFloat("_SurfaceType") != 0 || material.GetFloat("_AlphaCutoffEnable") != 0 || material.GetFloat("_UVBase") != 0) continue;
                            surfaces[i] = new HplReliefMeshBuilder.Surface { height = profile.height, amplitude = profile.amplitude,
                                uvScale = slots[i].scale, uvOffset = slots[i].offset };
                            signature.Append("|" + profile.hash + ":" + F(profile.amplitude));
                            slots[i].displaced = true;
                            any = true;
                        }
                        if (!any) continue;
                        if (samplesOnly)
                        {
                            string name = slots.Length == 1 && slots[0].albedo != null ? slots[0].albedo.name : "";
                            if (!new[] { "castlebase_wall__albedo", "castlebase_floor__albedo", "cellarbase_wood_floor__albedo", "mansionbase_floor_boards__albedo" }.Contains(name) ||
                                sampleTextures.Contains(slots[0].albedo)) continue;
                            var size = Vector3.Scale(mesh.bounds.size, scale);
                            if (size.magnitude < 2.5f || Mathf.Min(size.x, size.y, size.z) > 0.5f) continue;
                        }
                        key.Append("|" + string.Join(",", slots.Select(s => s.displaced ? "1" : "0")));
                        string identity = Hash(key.ToString()).Substring(0, 32);
                        if (!entries.TryGetValue(identity, out var entry))
                        {
                            string detailPath = Root + "/" + identity + "_detail.asset", coarsePath = Root + "/" + identity + "_coarse.asset";
                            string stampPath = AmnesiaPaths.ToAbsolute(CacheRoot + "/" + identity + ".txt");
                            string stamp = Hash(signature.ToString());
                            try
                            {
                                if (!File.Exists(stampPath) || File.ReadAllText(stampPath) != stamp ||
                                    AssetDatabase.LoadAssetAtPath<Mesh>(detailPath) == null || AssetDatabase.LoadAssetAtPath<Mesh>(coarsePath) == null)
                                {
                                    Mesh detailed = BuildWithinBudget(mesh, surfaces, scale, 0.03f, 120000);
                                    Mesh coarse = BuildWithinBudget(mesh, surfaces, scale, 0.09f, 24000);
                                    Save(detailed, detailPath); Save(coarse, coarsePath);
                                    File.WriteAllText(stampPath, stamp);
                                    generated++;
                                }
                            }
                            catch (InvalidOperationException e)
                            {
                                report.Add("SKIP " + renderer.name + ": " + e.Message);
                                continue;
                            }
                            entry = new HplReliefSet.Entry { original = mesh, objectScale = scale, slots = slots,
                                detailedResource = "HplReliefMeshes/" + identity + "_detail", coarseResource = "HplReliefMeshes/" + identity + "_coarse" };
                            entries.Add(identity, entry);
                        }
                        var detail = Resources.Load<Mesh>(entry.detailedResource);
                        for (int i = 0; i < detail.subMeshCount; i++) triangles += (long)detail.GetIndexCount(i) / 3;
                        count++;
                        if (samplesOnly) sampleTextures.Add(slots[0].albedo);
                        s_progress = (sceneIndex + (rendererIndex + 1f) / renderers.Length) / scenes.Length;
                        s_description = Path.GetFileNameWithoutExtension(path) + ": " + renderer.name;
                        yield return null;
                    }
                    instances += count;
                    report.Add($"{path}: {count} relief renderers, {triangles:N0} detailed triangles (before distance LOD)");
                    Debug.Log("[EnhancedRelief] " + report[^1]);
                    // Persist generated meshes before releasing map dependencies; catalog entries retain originals.
                    UpsertCatalog(entries.Values);
                    AssetDatabase.SaveAssets();
                    if (ownsScene) EditorSceneManager.CloseScene(scanScene, true);
                    ownsScene = false;
                    EditorUtility.UnloadUnusedAssetsImmediate();
                    yield return null;
                }
                UpsertCatalog(entries.Values);
                AssetDatabase.SaveAssets();
                // Interrupted experiments must not add unused generated meshes to every player build.
                // Keep them outside Resources, preserving their GUIDs for a future continuation.
                string parked = CacheRoot + "/Meshes";
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(parked));
                AssetDatabase.Refresh();
                var catalogNow = AssetDatabase.LoadAssetAtPath<HplReliefSet>(CatalogPath);
                var activePaths = new HashSet<string>(catalogNow.entries.SelectMany(e => new[]
                {
                    AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + e.detailedResource + ".asset",
                    AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + e.coarseResource + ".asset"
                }));
                foreach (string file in Directory.GetFiles(AmnesiaPaths.ToAbsolute(Root), "*.asset"))
                {
                    string name = Path.GetFileName(file), path = Root + "/" + name;
                    if (activePaths.Contains(path) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[0-9a-f]{32}_(detail|coarse)\\.asset$")) continue;
                    string error = AssetDatabase.MoveAsset(path, parked + "/" + name);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    s_description = "Keeping unused experiment meshes outside the build";
                    yield return null;
                }
                report.Add($"READY: {entries.Count} mesh variants, {generated} generated, {instances} instances, {profiles.Count} approved textures");
                Directory.CreateDirectory(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief"));
                File.WriteAllLines(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "Import.txt"), report);
                Debug.Log("[EnhancedRelief] " + report[^1]);
            }
            finally
            {
                if (ownsScene && scanScene.IsValid() && scanScene.isLoaded) EditorSceneManager.CloseScene(scanScene, true);
                if (entries.Count > 0) { UpsertCatalog(entries.Values); AssetDatabase.SaveAssets(); }
                HplEnhancedTextures.Shutdown();
            }
        }

        static void UpsertCatalog(IEnumerable<HplReliefSet.Entry> entries)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HplReliefSet>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<HplReliefSet>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var pairs = catalog.entries.Where(e => e != null && !string.IsNullOrEmpty(e.detailedResource)).ToDictionary(e => e.detailedResource);
            foreach (var entry in entries) pairs[entry.detailedResource] = entry;
            catalog.entries = pairs.Values.OrderBy(e => e.detailedResource, StringComparer.Ordinal).ToArray();
            EditorUtility.SetDirty(catalog);
        }

        static Mesh BuildWithinBudget(Mesh mesh, HplReliefMeshBuilder.Surface[] surfaces, Vector3 scale, float spacing, int budget)
        {
            for (int i = 0; ; i++)
            {
                try { return HplReliefMeshBuilder.Build(mesh, surfaces, scale, spacing, budget); }
                catch (InvalidOperationException e) when (e.Message.StartsWith("Relief triangle budget:", StringComparison.Ordinal) && i < 4) { spacing *= 1.5f; }
            }
        }
        static void Save(Mesh mesh, string path)
        {
            var previous = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            string parked = CacheRoot + "/Meshes/" + Path.GetFileName(path);
            if (previous == null && AssetDatabase.LoadAssetAtPath<Mesh>(parked) != null)
            {
                string error = AssetDatabase.MoveAsset(parked, path);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                previous = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            }
            if (previous == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, previous); EditorUtility.SetDirty(previous); Object.DestroyImmediate(mesh); }
        }

        static Dictionary<Texture, Profile> LoadProfiles()
        {
            var result = new Dictionary<Texture, Profile>();
            var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Import enhanced textures first.");
            string remix = Path.GetFullPath(Path.Combine(AmnesiaPaths.SourceRoot, "..", "sourceRTXRemix"));
            var completed = Directory.GetFiles(remix, "result.json", SearchOption.AllDirectories)
                .Where(p => Path.GetFileName(Path.GetDirectoryName(p)) == "pbrify-x4-batch-v1")
                .Select(p => (path: p, data: JsonUtility.FromJson<EnhancedMaterialConverter.BatchResult>(File.ReadAllText(p)))).ToArray();
            foreach (var pair in Profiles)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + pair.Key);
                if (material == null || !catalog.entries.Any(e => e.classic == material)) continue;
                var source = completed.FirstOrDefault(r => r.data.materials.Contains(pair.Key));
                if (source.data?.files?.height == null) continue;
                string file = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source.path), source.data.files.height.file));
                if (!file.StartsWith(remix + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Height outside sourceRTXRemix.");
                byte[] bytes = File.ReadAllBytes(file);
                if (Hash(bytes) != source.data.files.height.sha256) throw new InvalidOperationException("Changed height map: " + file);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Repeat };
                try
                {
                    if (!ImageConversion.LoadImage(texture, bytes, false)) throw new InvalidOperationException("Cannot read height: " + file);
                    var albedo = material.GetTexture("_BaseColorMap");
                    result[albedo] = new Profile { albedo = albedo, amplitude = pair.Value, height = new HplReliefMeshBuilder.HeightField(texture), hash = Hash(bytes) };
                    Debug.Log($"[EnhancedRelief] Profile {pair.Key}: {pair.Value * 100:F1} cm");
                }
                finally { Object.DestroyImmediate(texture); }
            }
            if (result.Count < 3) throw new InvalidOperationException("Missing wall/floor/wood height sources.");
            return result;
        }
    }

    /// <summary>Build-time copies of scenes must retain individual MeshFilters for runtime mesh replacement.</summary>
    public sealed class EnhancedReliefBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HplReliefSet>(EnhancedReliefConverter.CatalogPath);
            if (catalog == null) return;
            var meshes = catalog.entries.Where(e => e.original != null).GroupBy(e => e.original).ToDictionary(g => g.Key, g => g.ToArray());
            foreach (var renderer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)))
                if (renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null && meshes.TryGetValue(filter.sharedMesh, out var entries) &&
                    entries.Any(e => e.Matches(renderer, renderer.sharedMaterials)))
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & ~StaticEditorFlags.BatchingStatic);
        }
    }
}
