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
            // Generic manual relief 2026-10-27 (Build-ManualRelief.py <name>: shape from the HPL depth, matte, dimmed specular).
            ["static_objects/castlebase/wall/castlebase_wall_win.mat"] = 0.03f,
            ["static_objects/castlebase/wall/castlebase_wall_worn.mat"] = 0.035f,
            ["static_objects/castlebase/wall/castlebase_wall_extension.mat"] = 0.02f,
            ["static_objects/castlebase/wall/castlebase_wall_welder_convex.mat"] = 0.018f,
            ["static_objects/castlebase/wall/castlebase_wall_door_straight.mat"] = 0.018f,
            ["static_objects/castlebase/pillar/tall_stone.mat"] = 0.018f,
            ["static_objects/castlebase/church_wall/church_floor.mat"] = 0.014f,
            ["static_objects/murky_castlebase/murky_castlebase.mat"] = 0.02f,
            ["static_objects/cellarbase/cellarbase.mat"] = 0.018f,
            ["static_objects/cellarbase/cellarbase_details.mat"] = 0.015f,
            ["static_objects/dungeonbase/special/torture_chancel.mat"] = 0.03f,
            ["static_objects/dungeonbase/special/welder_sewer_stone.mat"] = 0.03f,
            ["static_objects/dungeonbase/pillar/stone_round.mat"] = 0.028f,
            ["static_objects/dungeonbase/pillar/gigantic.mat"] = 0.03f,
            ["static_objects/dungeonbase/dungeonbase_lab_wall.mat"] = 0.035f,
            ["static_objects/dungeonbase/wall/dungeon_wall_variant.mat"] = 0.025f,
            ["static_objects/dungeonbase/dungeonbase_wall_tileable.mat"] = 0.03f,
            ["static_objects/dungeonbase/dungeonbase_floor.mat"] = 0.018f,
            ["static_objects/dungeonbase/floor/gigantic_stones.mat"] = 0.03f,
            ["static_objects/dungeonbase/floor/dirt_mould.mat"] = 0.02f,
            ["static_objects/cisternbase/cisternbase.mat"] = 0.025f,
            ["static_objects/cisternbase/pillars/cistern_stone.mat"] = 0.03f,
            ["static_objects/cisternbase/ceiling/stone_arched.mat"] = 0.03f,
            ["static_objects/cisternbase/floor/stone_dirty.mat"] = 0.02f,
            ["static_objects/debris/dungeon_cave_in/dungeon_cave_in.mat"] = 0.04f,
            ["static_objects/sanctumbase/rock_tileable.mat"] = 0.035f,
            ["static_objects/castlebase/wall/castlebase_wall_square_bricks.mat"] = 0.035f,
            ["static_objects/castlebase/castlebase_floor.mat"] = 0.018f,
            ["static_objects/castlebase/castlebase_tiledfloor.mat"] = 0.012f,
            ["static_objects/cellarbase/floor/cellarbase_floor.mat"] = 0.025f,
            ["static_objects/cellarbase/floor/cellarbase_floor_alt.mat"] = 0.025f,
            ["static_objects/cellarbase/floor/cellarbase_wood_floor.mat"] = 0.008f,
            ["static_objects/mansionbase/floor/mansionbase_floor_boards.mat"] = 0.006f,
            ["static_objects/dungeonbase/floor/cobbled_stone.mat"] = 0.035f,
            // Third pass 2026-10-28: castle doors, windows, pillars and stairs, ceilings and beams, the mansion and the sanctum.
            ["static_objects/castlebase/wall/castlebase_wall_door.mat"] = 0.03f,
            ["static_objects/castlebase/wall/castlebase_wall_extension_win.mat"] = 0.03f,
            ["static_objects/castlebase/castlebase_details.mat"] = 0.022f,
            ["static_objects/castlebase/special/cell_wall.mat"] = 0.038f,
            ["static_objects/castlebase/wall/castlebase_wall_door_straight_worn.mat"] = 0.027f,
            ["static_objects/castlebase/pillar/castlebase_pillars.mat"] = 0.027f,
            ["static_objects/castlebase/stairs/castlebase_stairs.mat"] = 0.022f,
            ["static_objects/castlebase/stairs/castlebase_stairs_worn.mat"] = 0.022f,
            ["static_objects/castlebase/stairs/castlebase_stair_welder.mat"] = 0.022f,
            ["static_objects/castlebase/stairs/castlebase_stair_welder_worn.mat"] = 0.022f,
            ["static_objects/cellarbase/stairs/cellarbase_stone_stairs.mat"] = 0.022f,
            ["static_objects/cellarbase/cellarbase_wood_beams.mat"] = 0.015f,
            ["static_objects/castlebase/ceiling/ceiling02_arched.mat"] = 0.03f,
            ["static_objects/castlebase/castlebase_ceiling.mat"] = 0.03f,
            ["static_objects/castlebase/castlebase_ceiling02.mat"] = 0.03f,
            ["static_objects/castlebase/ceiling/welder_wood.mat"] = 0.012f,
            ["static_objects/castlebase/ceiling/ceiling_rough.mat"] = 0.03f,
            ["static_objects/castlebase/ceiling/ceiling02_window.mat"] = 0.03f,
            ["static_objects/castlebase/ceiling/ceiling02_window_worn.mat"] = 0.03f,
            ["static_objects/tunnelbase/special/tunnel.mat"] = 0.038f,
            ["static_objects/mansionbase/wall/mansionbase02.mat"] = 0.012f,
            ["static_objects/mansionbase/special/outside_wall.mat"] = 0.03f,
            ["static_objects/mansionbase/wall/mansionbase.mat"] = 0.009f,
            ["static_objects/mansionbase/ceiling/mansionbase_concave.mat"] = 0.009f,
            ["static_objects/mansionbase/wall/mansionbase_wood.mat"] = 0.009f,
            ["static_objects/sanctumbase/special/sanctumbase01.mat"] = 0.045f,
            ["static_objects/sanctumbase/special/sanctumbase02.mat"] = 0.045f,
            ["static_objects/sanctumbase/floor/sanctumbase_floor.mat"] = 0.03f,
            ["static_objects/cellarbase/floor/cellarbase_floor_rough.mat"] = 0.03f,
            ["static_objects/dungeonbase/floor/chasm_edge.mat"] = 0.045f,
            // Approved 2026-10-25 (tools/Build-ManualRelief.py dungeon; the amplitude is unused, the maps carry metres).
            ["static_objects/dungeonbase/dungeonbase.mat"] = 0.036f,
        };
        sealed class Profile
        {
            public Texture albedo;
            public HplReliefMeshBuilder.HeightField height;
            public float amplitude;
            public string hash;
            public ApprovedReliefProfiles.Data approved;
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

        [MenuItem("Tools/RTXMnesia/Import/Enhanced relief: approved game walls and boards")]
        public static void ConvertApprovedBatch() => Start(false, true);

        static void Start(bool samplesOnly, bool approvedOnly = false)
        {
            if (Application.isBatchMode)
            {
                using var work = Generate(samplesOnly, approvedOnly);
                while (work.MoveNext()) { }
                return;
            }
            if (s_work != null) throw new InvalidOperationException("Relief generation is already running.");
            s_work = Generate(samplesOnly, approvedOnly);
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

        static IEnumerator<object> Generate(bool samplesOnly, bool approvedOnly)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Generate relief outside Play mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save edited scenes before generating relief.");
            HplEnhancedTextures.Shutdown();
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Root));
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(CacheRoot));
            AssetDatabase.Refresh();
            var profiles = LoadProfiles(approvedOnly);
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
                    var jobs = new List<Job>();
                    var sources = new Dictionary<Mesh, HplReliefMeshBuilder.Source>();
                    var uses = new Dictionary<string, int>();
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
                            if (profile.approved != null)
                            {
                                surfaces[i].heightMetres = profile.approved.height;
                                surfaces[i].boards = profile.approved.boards;
                                surfaces[i].fadeAtCreases = !profile.approved.overCreases;
                                surfaces[i].tolerance = profile.approved.tolerance;
                                surfaces[i].geometryNormals = !profile.approved.boards && !profile.approved.mapNormals;
                                slots[i].normalResource = profile.approved.normal;
                                slots[i].maskResource = profile.approved.mask;
                                slots[i].specularResource = profile.approved.specular;
                                slots[i].specularF0 = profile.approved.specularF0;
                            }
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
                        if (!entries.ContainsKey(identity))
                        {
                            string detailPath = Root + "/" + identity + "_detail" + HplReliefMeshFile.Extension, coarsePath = Root + "/" + identity + "_coarse" + HplReliefMeshFile.Extension;
                            string stampPath = AmnesiaPaths.ToAbsolute(CacheRoot + "/" + identity + ".txt");
                            string stamp = Hash(signature.ToString());
                            if (!File.Exists(stampPath) || File.ReadAllText(stampPath) != stamp ||
                                AssetDatabase.LoadAssetAtPath<Mesh>(detailPath) == null || AssetDatabase.LoadAssetAtPath<Mesh>(coarsePath) == null)
                            {
                                if (!sources.TryGetValue(mesh, out var source)) sources[mesh] = source = HplReliefMeshBuilder.Read(mesh);
                                jobs.Add(new Job { identity = identity, mesh = mesh, source = source, surfaces = surfaces, scale = scale,
                                    renderer = renderer.name, detailPath = detailPath, coarsePath = coarsePath, stampPath = stampPath, stamp = stamp });
                            }
                            entries.Add(identity, new HplReliefSet.Entry { original = mesh, objectScale = scale, slots = slots,
                                detailedResource = "HplReliefMeshes/" + identity + "_detail", coarseResource = "HplReliefMeshes/" + identity + "_coarse" });
                        }
                        uses[identity] = uses.TryGetValue(identity, out int used) ? used + 1 : 1;
                        if (samplesOnly) sampleTextures.Add(slots[0].albedo);
                        s_progress = (sceneIndex + (rendererIndex + 1f) / renderers.Length * 0.2f) / scenes.Length;
                        s_description = Path.GetFileNameWithoutExtension(path) + ": " + renderer.name;
                        if (rendererIndex % 200 == 0) yield return null;
                    }
                    // The subdivision and displacement are plain array math: all new variants of the map on every core.
                    s_description = Path.GetFileNameWithoutExtension(path) + $": building {jobs.Count} relief meshes";
                    yield return null;
                    System.Threading.Tasks.Parallel.ForEach(jobs, job =>
                    {
                        try
                        {
                            float spacing = job.surfaces.Any(s => s?.heightMetres != null || s?.boards == true) ? .01f : 0.03f;
                            job.detailed = ComputeWithinBudget(job.source, job.surfaces, job.scale, spacing, 120000);
                            job.coarse = ComputeWithinBudget(job.source, job.surfaces, job.scale, 0.09f, 24000);
                        }
                        catch (InvalidOperationException e) when (e.Message.StartsWith("Relief triangle budget:", StringComparison.Ordinal))
                        {
                            // Huge planes (900-1900 m², one quad each) cannot hold 1 cm joints within the budget; coarse
                            // spacing turns them into soft waves. They keep their flat geometry with the relief maps
                            // (normal, matte, dimmed specular), like their neighbours, instead of the glossy plain material.
                            job.flat = e.Message;
                        }
                        catch (InvalidOperationException e) { job.error = e.Message; }
                    });
                    foreach (var job in jobs)
                    {
                        if (job.error != null)
                        {
                            report.Add("SKIP " + job.renderer + ": " + job.error);
                            entries.Remove(job.identity);
                            uses.Remove(job.identity);
                            continue;
                        }
                        Mesh detailed, coarse;
                        if (job.flat != null)
                        {
                            detailed = Object.Instantiate(job.mesh); detailed.name = job.mesh.name + " (flat relief)";
                            coarse = Object.Instantiate(job.mesh); coarse.name = job.mesh.name + " (flat relief)";
                            report.Add("FLAT " + job.renderer + ": " + job.flat);
                        }
                        else { detailed = HplReliefMeshBuilder.Create(job.detailed); coarse = HplReliefMeshBuilder.Create(job.coarse); }
                        job.detailed = job.coarse = null;
                        Save(detailed, job.detailPath); Save(coarse, job.coarsePath);
                        File.WriteAllText(job.stampPath, job.stamp);
                        generated++;
                    }
                    jobs.Clear(); sources.Clear();
                    foreach (var (identity, uses_) in uses)
                    {
                        var detail = Resources.Load<Mesh>(entries[identity].detailedResource);
                        for (int i = 0; i < detail.subMeshCount; i++) triangles += (long)detail.GetIndexCount(i) * uses_ / 3;
                        count += uses_;
                    }
                    uses.Clear();
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
                    AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + e.detailedResource + HplReliefMeshFile.Extension,
                    AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + e.coarseResource + HplReliefMeshFile.Extension
                }));
                foreach (string file in Directory.GetFiles(AmnesiaPaths.ToAbsolute(Root), "*" + HplReliefMeshFile.Extension))
                {
                    string name = Path.GetFileName(file), path = Root + "/" + name;
                    if (activePaths.Contains(path) || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[0-9a-f]{32}_(detail|coarse)\\.hplrelief$")) continue;
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
            var fresh = entries.ToArray();
            // An entry generated now replaces older ones for the same renderers (same mesh, scale and slots):
            // the runtime takes the first match, and a profile change alters the variant identity.
            var pairs = catalog.entries.Where(e => e != null && !string.IsNullOrEmpty(e.detailedResource) &&
                !fresh.Any(n => n.detailedResource != e.detailedResource && SameRenderers(n, e))).ToDictionary(e => e.detailedResource);
            foreach (var entry in fresh) pairs[entry.detailedResource] = entry;
            catalog.entries = pairs.Values.OrderBy(e => e.detailedResource, StringComparer.Ordinal).ToArray();
            EditorUtility.SetDirty(catalog);
        }

        static bool SameRenderers(HplReliefSet.Entry a, HplReliefSet.Entry b) =>
            a.original == b.original && (a.objectScale - b.objectScale).sqrMagnitude <= 0.000001f && a.slots.Length == b.slots.Length &&
            a.slots.Zip(b.slots, (x, y) => x.albedo == y.albedo && (x.scale - y.scale).sqrMagnitude <= 0.00000001f && (x.offset - y.offset).sqrMagnitude <= 0.00000001f).All(v => v);

        /// <summary>A relief variant to build: read on the main thread, computed in parallel, saved on the main thread.</summary>
        sealed class Job
        {
            public string identity, renderer, detailPath, coarsePath, stampPath, stamp, flat, error;
            public Mesh mesh;
            public HplReliefMeshBuilder.Source source;
            public HplReliefMeshBuilder.Surface[] surfaces;
            public Vector3 scale;
            public HplReliefMeshBuilder.Result detailed, coarse;
        }

        static HplReliefMeshBuilder.Result ComputeWithinBudget(HplReliefMeshBuilder.Source source, HplReliefMeshBuilder.Surface[] surfaces, Vector3 scale, float spacing, int budget)
        {
            for (int i = 0; ; i++)
            {
                try { return HplReliefMeshBuilder.Compute(source, surfaces, scale, spacing, budget); }
                catch (InvalidOperationException e) when (e.Message.StartsWith("Relief triangle budget:", StringComparison.Ordinal) && i < 8) { spacing *= 1.5f; }
            }
        }

        static void Save(Mesh mesh, string path)
        {
            // Rewriting the file in place (or after moving a parked one back) keeps its GUID.
            string parked = CacheRoot + "/Meshes/" + Path.GetFileName(path);
            if (!File.Exists(AmnesiaPaths.ToAbsolute(path)) && File.Exists(AmnesiaPaths.ToAbsolute(parked)))
            {
                string error = AssetDatabase.MoveAsset(parked, path);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            }
            try { HplReliefMeshFile.Write(mesh, AmnesiaPaths.ToAbsolute(path)); }
            finally { Object.DestroyImmediate(mesh); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>One-off: moves the text .asset meshes (Resources and parked) to binary .hplrelief files, same names, verified.</summary>
        [MenuItem("Tools/RTXMnesia/Import/Enhanced relief: convert stored meshes to binary")]
        public static void ConvertStorageBatch()
        {
            var report = new List<string>();
            long before = 0, after = 0;
            int converted = 0;
            foreach (string folder in new[] { Root, CacheRoot + "/Meshes" })
            {
                if (!Directory.Exists(AmnesiaPaths.ToAbsolute(folder))) continue;
                var files = Directory.GetFiles(AmnesiaPaths.ToAbsolute(folder), "*.asset").Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
                for (int i = 0; i < files.Length; i++)
                {
                    string path = folder + "/" + files[i], target = path.Substring(0, path.Length - ".asset".Length) + HplReliefMeshFile.Extension;
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (mesh == null) throw new InvalidOperationException("Not a mesh: " + path);
                    byte[] bytes = HplReliefMeshFile.Encode(mesh);
                    var decoded = HplReliefMeshFile.Decode(bytes, mesh.name);
                    try { string problem = Compare(mesh, decoded); if (problem != null) throw new InvalidOperationException(path + ": " + problem); }
                    finally { Object.DestroyImmediate(decoded); }
                    File.WriteAllBytes(AmnesiaPaths.ToAbsolute(target), bytes);
                    before += new FileInfo(AmnesiaPaths.ToAbsolute(path)).Length;
                    after += bytes.Length;
                    converted++;
                    if (i % 100 == 99) { EditorUtility.UnloadUnusedAssetsImmediate(); Debug.Log($"[EnhancedRelief] storage {folder}: {i + 1}/{files.Length}"); }
                }
                // Deleted only after every file of the folder was written and verified.
                var failed = new List<string>();
                if (!AssetDatabase.DeleteAssets(files.Select(n => folder + "/" + n).ToArray(), failed)) throw new InvalidOperationException("Cannot delete " + string.Join(", ", failed));
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            report.Add($"READY: {converted} meshes, {before / 1048576.0:N0} MB text -> {after / 1048576.0:N0} MB binary");
            int missing = AssetDatabase.LoadAssetAtPath<HplReliefSet>(CatalogPath).entries
                .Count(e => Resources.Load<Mesh>(e.detailedResource) == null || Resources.Load<Mesh>(e.coarseResource) == null);
            report.Add($"catalog entries without both meshes: {missing}");
            Directory.CreateDirectory(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief"));
            File.WriteAllLines(Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedRelief", "Storage.txt"), report);
            foreach (string line in report) Debug.Log("[EnhancedRelief] " + line);
            if (missing != 0) throw new InvalidOperationException("Relief catalog lost meshes after conversion.");
        }

        /// <summary>The compact layout keeps positions, UVs and indices exact, and directions within SNorm16.</summary>
        static string Compare(Mesh a, Mesh b)
        {
            if (a.vertexCount != b.vertexCount || a.subMeshCount != b.subMeshCount) return "counts";
            var pa = a.vertices; var pb = b.vertices;
            for (int i = 0; i < pa.Length; i++) if (pa[i] != pb[i]) return "position " + i;
            var na = a.normals; var nb = b.normals;
            if (na.Length != nb.Length) return "normals";
            for (int i = 0; i < na.Length; i++) if ((na[i] - nb[i]).sqrMagnitude > 1e-8f) return "normal " + i;
            var ta = a.tangents; var tb = b.tangents;
            if (ta.Length != tb.Length) return "tangents";
            for (int i = 0; i < ta.Length; i++) if ((ta[i] - tb[i]).sqrMagnitude > 1e-8f) return "tangent " + i;
            if (!a.colors32.SequenceEqual(b.colors32)) return "colors";
            var ua = new List<Vector4>(); var ub = new List<Vector4>();
            for (int channel = 0; channel < 8; channel++)
            {
                a.GetUVs(channel, ua); b.GetUVs(channel, ub);
                if (!ua.SequenceEqual(ub)) return "uv" + channel;
            }
            for (int sub = 0; sub < a.subMeshCount; sub++)
                if (a.GetTopology(sub) != b.GetTopology(sub) || !a.GetIndices(sub).SequenceEqual(b.GetIndices(sub))) return "indices " + sub;
            if (a.bounds != b.bounds) return "bounds";
            return null;
        }

        static Dictionary<Texture, Profile> LoadProfiles(bool approvedOnly)
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
                bool approved = pair.Key == ApprovedReliefProfiles.Wall || pair.Key == ApprovedReliefProfiles.Boards || ApprovedReliefProfiles.HasManual(pair.Key);
                if (approvedOnly && !approved) continue;
                var material = AssetDatabase.LoadAssetAtPath<Material>(AmnesiaPaths.MaterialsRoot + "/" + pair.Key);
                if (material == null || !catalog.entries.Any(e => e.classic == material)) continue;
                string stand = AmnesiaPaths.ImportedAssetsRoot + "/DevRoomRelief/" + Path.GetFileNameWithoutExtension(pair.Key) + "_solid.asset";
                if (approved && (approvedOnly || ApprovedReliefProfiles.HasManual(pair.Key) || File.Exists(AmnesiaPaths.ToAbsolute(stand))))
                {
                    var approvedProfile = ApprovedReliefProfiles.Prepare(pair.Key);
                    var albedo = material.GetTexture("_BaseColorMap");
                    result[albedo] = new Profile { albedo = albedo, approved = approvedProfile, hash = approvedProfile.signature, amplitude = pair.Value };
                    continue;
                }
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
            if (result.Count < (approvedOnly ? 2 : 3)) throw new InvalidOperationException("Missing wall/floor/wood height sources.");
            return result;
        }
    }

    /// <summary>
    /// Build-time copies of scenes (also on entering Play) must retain individual MeshFilters for runtime mesh replacement,
    /// and the decals near those walls their own transforms (HplEnhancedRelief.SeparateSharedWalls moves them).
    /// </summary>
    public sealed class EnhancedReliefBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HplReliefSet>(EnhancedReliefConverter.CatalogPath);
            if (catalog == null) return;
            var meshes = catalog.entries.Where(e => e.original != null).GroupBy(e => e.original).ToDictionary(g => g.Key, g => g.ToArray());
            var walls = new List<Bounds>();
            var renderers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true)).ToList();
            var decals = renderers.Where(HplEnhancedRelief.IsDecal).ToList();
            foreach (var renderer in renderers)
                if (renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null && meshes.TryGetValue(filter.sharedMesh, out var entries) &&
                    entries.Any(e => e.Matches(renderer, renderer.sharedMaterials)))
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & ~StaticEditorFlags.BatchingStatic);
                    var near = renderer.bounds;
                    near.Expand(0.04f);
                    walls.Add(near);
                }
            foreach (var renderer in decals)
                if (walls.Any(w => w.Intersects(renderer.bounds)))
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & ~StaticEditorFlags.BatchingStatic);
        }
    }
}
