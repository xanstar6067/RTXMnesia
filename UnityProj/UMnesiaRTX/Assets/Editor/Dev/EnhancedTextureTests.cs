using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// <summary>Import identity, reversible slots, real level coverage, menu and Play event checks.</summary>
    [InitializeOnLoad]
    public static class EnhancedTextureTests
    {
        const string PlayKey = "RTXMnesia.EnhancedTextures.PlayCheck";
        static int s_checks;
        static string Folder => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedTextureCheck");
        static string ClassicPath => AmnesiaPaths.MaterialsRoot + "/" + EnhancedMaterialConverter.Stone + ".mat";
        static string EnhancedPath => EnhancedMaterialConverter.MaterialPathFor(EnhancedMaterialConverter.Stone + ".mat");
        static Material Classic => AssetDatabase.LoadAssetAtPath<Material>(ClassicPath);
        static Material Enhanced => AssetDatabase.LoadAssetAtPath<Material>(EnhancedPath);

        static EnhancedTextureTests() => EditorApplication.playModeStateChanged += OnPlayState;
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            s_checks++;
            Debug.Log("[EnhancedTextureCheck] ok: " + message);
        }

        static string Hash(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream));
        }

        static Dictionary<string, string> ClassicHashes() => AssetDatabase.GetDependencies(ClassicPath)
            .Where(p => p.StartsWith(AmnesiaPaths.ImportedAssetsRoot + "/", StringComparison.Ordinal))
            .SelectMany(p => new[] { p, p + ".meta" }).Where(File.Exists).ToDictionary(p => p, Hash);

        public static void RunFullBatch()
        {
            Directory.CreateDirectory(Folder);
            string[] args = Environment.GetCommandLineArgs();
            int arg = Array.IndexOf(args, "-enhancedResults");
            var sourceFiles = File.ReadAllLines(args[arg + 1]);
            var sources = sourceFiles.Select(p => JsonUtility.FromJson<EnhancedMaterialConverter.BatchResult>(File.ReadAllText(p))).ToArray();
            var classicPaths = sources.SelectMany(r => r.materials).Distinct().ToArray();
            Debug.Log($"[EnhancedTextureCheck] Auditing classic dependencies of {classicPaths.Length} materials before import");
            var originals = AssetDatabase.GetDependencies(classicPaths.Select(p => AmnesiaPaths.MaterialsRoot + "/" + p).ToArray())
                .Where(p => p.StartsWith(AmnesiaPaths.ImportedAssetsRoot + "/", StringComparison.Ordinal))
                .SelectMany(p => new[] { p, p + ".meta" }).Distinct().Where(File.Exists).ToDictionary(p => p, Hash);
            File.WriteAllLines(Path.Combine(Folder, "full-classic-sha256.txt"), originals.Select(p => p.Value + "  " + p.Key));
            Debug.Log($"[EnhancedTextureCheck] Recorded {originals.Count} classic files; beginning full import");
            var previous = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
            string ExistingPath(HplTextureSet.Entry e) => !string.IsNullOrEmpty(e.enhancedResource) ?
                AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + e.enhancedResource + ".mat" : AssetDatabase.GetAssetPath(e.enhanced);
            var identities = previous.entries.Where(e => !string.IsNullOrEmpty(ExistingPath(e))).ToDictionary(e => AssetDatabase.GetAssetPath(e.classic),
                e => AssetDatabase.AssetPathToGUID(ExistingPath(e)));
            if (args.Contains("-enhancedAuditOnly")) EnhancedDdsImportRules.ConfigureAllBatch();
            else EnhancedMaterialConverter.ConvertReadyBatch();
            HplEnhancedTextures.Shutdown();
            var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
            Check(catalog.entries.Length >= classicPaths.Length, "catalog retains previous entries and covers the full snapshot");
            Check(AssetDatabase.GetDependencies(EnhancedMaterialConverter.CatalogPath).All(p =>
                !p.StartsWith(EnhancedMaterialConverter.Root + "/", StringComparison.Ordinal) &&
                !p.StartsWith(EnhancedMaterialConverter.MaterialRoot + "/", StringComparison.Ordinal)), "catalog has no eager references to enhanced materials or textures");
            var pairs = catalog.entries.ToDictionary(e => AssetDatabase.GetAssetPath(e.classic), e => e);
            Check(identities.All(p => p.Value == AssetDatabase.AssetPathToGUID(EnhancedMaterialConverter.MaterialPathFor(p.Key.Substring(AmnesiaPaths.MaterialsRoot.Length + 1)))),
                "migration to lazy Resources paths retains existing material GUIDs");
            int cutout = 0;
            foreach (string relative in classicPaths)
            {
                var entry = pairs[AmnesiaPaths.MaterialsRoot + "/" + relative];
                var original = entry.classic;
                var improved = entry.enhanced;
                Check(improved != null && improved != original && improved.shader == original.shader, "separate native PBR material: " + relative);
                foreach (string property in new[] { "_SurfaceType", "_AlphaCutoffEnable", "_AlphaCutoff", "_AlphaCutoffShadow", "_UseShadowThreshold", "_DoubleSidedEnable", "_EmissiveIntensity" })
                    if (!Mathf.Approximately(improved.GetFloat(property), original.GetFloat(property))) throw new InvalidOperationException(relative + ": changed " + property);
                if (improved.GetTexture("_EmissiveColorMap") != original.GetTexture("_EmissiveColorMap") || improved.GetColor("_EmissiveColor") != original.GetColor("_EmissiveColor"))
                    throw new InvalidOperationException(relative + ": emission changed");
                Check(improved.GetTextureScale("_BaseColorMap") == original.GetTextureScale("_BaseColorMap") &&
                    improved.GetTextureOffset("_BaseColorMap") == original.GetTextureOffset("_BaseColorMap"), "alpha, emission, culling and UV preserved: " + relative);
                foreach (var property in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_HeightMap" })
                {
                    var texture = improved.GetTexture(property) as Texture2D;
                    if (texture == null) { if (property == "_HeightMap") continue; throw new InvalidOperationException(relative + ": missing " + property); }
                    var expected = property == "_NormalMap" ? TextureFormat.BC5 : property == "_HeightMap" ? TextureFormat.BC4 : TextureFormat.BC7;
                    if (texture.format != expected || !texture.streamingMipmaps || texture.isReadable || texture.mipmapCount <= 1 ||
                        UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat) != (property == "_BaseColorMap"))
                        throw new InvalidOperationException(relative + ": compression/mips/colour space failed for " + property +
                            $" ({texture.format}, {texture.graphicsFormat}, streaming={texture.streamingMipmaps}, readable={texture.isReadable}, mips={texture.mipmapCount})");
                }
                if (original.GetFloat("_AlphaCutoffEnable") > 0)
                {
                    cutout++;
                    Check(!improved.IsKeywordEnabled("_PIXEL_DISPLACEMENT") && improved.IsKeywordEnabled("_ALPHATEST_ON"), "cutout keeps alpha testing and flat silhouette: " + relative);
                }
                if (s_checks % 32 == 0) EditorUtility.UnloadUnusedAssetsImmediate();
            }
            Check(cutout == sources.Where(r => r.alpha_mode == "source-nearest").Sum(r => r.materials.Length), "all cutout materials included");
            string levelRoot = AmnesiaPaths.ImportedAssetsRoot + "/Levels";
            int sceneCount = 0, slotCount = 0;
            foreach (string absolute in Directory.GetFiles(AmnesiaPaths.ToAbsolute(levelRoot), "*.unity", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(AmnesiaPaths.ToAbsolute(levelRoot), absolute).Replace('\\', '/');
                if (!relative.StartsWith("maps/main/") && !relative.StartsWith("maps/ptest/") && !relative.StartsWith("main_menu/")) continue;
                var scene = EditorSceneManager.OpenScene(levelRoot + "/" + relative, OpenSceneMode.Single);
                var snapshot = Snapshot(scene);
                HplEnhancedTextures.ApplyScene(scene, true);
                foreach (var item in snapshot)
                {
                    var current = item.Key.sharedMaterials;
                    for (int i = 0; i < item.Value.Length; i++)
                        if (item.Value[i] != null && pairs.TryGetValue(AssetDatabase.GetAssetPath(item.Value[i]), out var pair))
                        {
                            if (HplEnhancedRelief.SurfaceMaterial(current[i]) != pair.enhanced) throw new InvalidOperationException(relative + ": missed slot " + item.Value[i].name);
                            slotCount++;
                        }
                }
                HplEnhancedTextures.ApplyScene(scene, false);
                Check(Same(snapshot), relative + ": every original slot restored");
                HplEnhancedTextures.Shutdown();
                sceneCount++;
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
            Check(originals.All(p => Hash(p.Key) == p.Value), "all classic resources byte-identical after full import and scene audit");
            Debug.Log($"[EnhancedTextureCheck] FULL PASS: {sources.Length} sets, {catalog.entries.Length} materials ({cutout} cutout), {sceneCount} scenes, {slotCount} slots, {originals.Count} classic files, {s_checks} checks");
            RunBatch();
        }

        public static void RunReadyBatch() => RunFullBatch();

        [MenuItem("Tools/RTXMnesia/Tests/Enhanced textures")]
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);
            var hashes = ClassicHashes();
            Check(hashes.Count >= 10, "classic material, textures and metas included in byte audit");
            EnhancedMaterialConverter.ConvertBatch();
            var ids = AssetDatabase.GetDependencies(EnhancedPath).Append(EnhancedMaterialConverter.CatalogPath)
                .Where(p => p.StartsWith(AmnesiaPaths.ImportedAssetsRoot + "/", StringComparison.Ordinal))
                .ToDictionary(p => p, AssetDatabase.AssetPathToGUID);
            EnhancedMaterialConverter.ConvertBatch();
            Check(ids.All(p => AssetDatabase.AssetPathToGUID(p.Key) == p.Value), "reimport keeps all GUIDs, including catalog and material");
            Check(hashes.All(p => Hash(p.Key) == p.Value), "classic material, maps and metas are byte-identical after repeated enhanced imports");
            File.WriteAllLines(Path.Combine(Folder, "classic-sha256.txt"), hashes.Select(p => p.Value + "  " + p.Key));

            var material = Enhanced;
            Check(material != null && material != Classic && material.shader.name == "HDRP/Lit", "separate HDRP/Lit material");
            Check(material.GetFloat("_SurfaceType") == 0 && material.GetFloat("_AlphaCutoffEnable") == 0 &&
                  material.GetFloat("_MaterialID") == (float)MaterialId.LitStandard, "opaque dielectric metallic workflow");
            Check(material.GetFloat("_Metallic") == 0 && material.GetTexture("_SpecularColorMap") == null,
                  "stone uses dielectric F0, with no legacy specular map");
            Check(material.IsKeywordEnabled("_NORMALMAP_TANGENT_SPACE") && material.IsKeywordEnabled("_MASKMAP") &&
                  material.IsKeywordEnabled("_HEIGHTMAP") && material.IsKeywordEnabled("_PIXEL_DISPLACEMENT"), "all PBR maps and POM keywords active");
            Check(Mathf.Approximately(material.GetFloat("_HeightAmplitude"), EnhancedMaterialConverter.HeightCm * 0.01f), "height amplitude in metres");
            foreach (string property in new[] { "_BaseColorMap", "_NormalMap", "_MaskMap", "_HeightMap" })
            {
                var texture = material.GetTexture(property) as Texture2D;
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                if (importer is IHVImageFormatImporter native)
                {
                    Check(texture.width == 4096 && texture.height == 4096 && texture.mipmapCount > 1 && native.streamingMipmaps &&
                        native.wrapMode == TextureWrapMode.Repeat && UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat) == (property == "_BaseColorMap"),
                        property + ": native compressed 4K, streaming mips, repeat and correct colour space");
                    if (property == "_NormalMap") Check(texture.format == TextureFormat.BC5, "pre-corrected normal RG stored as native BC5");
                }
                else
                {
                    var png = (TextureImporter)importer;
                    Check(texture.width == 4096 && texture.height == 4096 && png.mipmapEnabled && png.wrapMode == TextureWrapMode.Repeat &&
                        png.sRGBTexture == (property == "_BaseColorMap"), property + ": legacy PNG settings");
                }
            }

            var settings = new HplUserSettings { textures = 1 };
            settings.Validate();
            Check(settings.textures == 1 && JsonUtility.FromJson<HplUserSettings>(JsonUtility.ToJson(settings)).textures == 1,
                  "enhanced selection survives validation and JSON serialization");
            var choices = HplGraphicsOptions.Items(MenuControl.Textures, settings, (c, k, f) => f, out int selected);
            Check(choices.Count == 2 && choices[1].enabled && selected == 1, "existing textures dropdown enables enhanced set");
            var budgetChoices = HplGraphicsOptions.Items(MenuControl.TextureMemoryBudget, settings, (c, k, f) => f, out int budgetSelected);
            Check(budgetSelected == 0 && budgetChoices.Select(c => c.label).SequenceEqual(new[] { "2 ГБ", "4 ГБ", "8 ГБ", "10 ГБ", "12 ГБ", "16 ГБ" }),
                "memory budget dropdown offers all six requested values, default 2 GB");
            for (int i = 0; i < budgetChoices.Count; i++)
            {
                var draft = settings.Copy();
                HplGraphicsOptions.Set(MenuControl.TextureMemoryBudget, draft, i, (c, k, f) => f);
                var decoded = JsonUtility.FromJson<HplUserSettings>(JsonUtility.ToJson(draft));
                decoded.Validate();
                Check(decoded.textureMemoryBudgetGb == HplGraphics.TextureMemoryBudgetsGb[i] && settings.textureMemoryBudgetGb == 2,
                    "memory budget draft and JSON round-trip: " + budgetChoices[i].label);
            }

            TestSlots();
            foreach (string map in new[] { "00_rainy_hall", "01_old_archives" })
            {
                var scene = EditorSceneManager.OpenScene(AmnesiaPaths.ImportedAssetsRoot + "/Levels/maps/main/ch01/" + map + ".unity", OpenSceneMode.Single);
                var snapshots = Snapshot(scene);
                int targets = snapshots.Count(p => p.Value.Contains(Classic));
                Check(targets > 0, map + ": contains the original castle stone (" + targets + " renderers)");
                HplEnhancedTextures.ApplyScene(scene, true);
                Check(snapshots.Where(p => p.Value.Contains(Classic)).All(p => p.Key.sharedMaterials.Select(HplEnhancedRelief.SurfaceMaterial).Contains(Enhanced)), map + ": all matching walls replaced");
                HplEnhancedTextures.ApplyScene(scene, false);
                Check(Same(snapshots), map + ": all slots restored to exact original references");
            }
            HplEnhancedTextures.Shutdown();
            Debug.Log($"[EnhancedTextureCheck] PASS: {s_checks} checks");
        }

        static Dictionary<Renderer, Material[]> Snapshot(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Renderer>(true)).ToDictionary(r => r, r => r.sharedMaterials.Select(HplEnhancedRelief.SurfaceMaterial).ToArray());
        static bool Same(Dictionary<Renderer, Material[]> snapshot) => snapshot.All(p => p.Key.sharedMaterials.SequenceEqual(p.Value));

        static void TestSlots()
        {
            var go = new GameObject("Enhanced material slot check");
            var renderer = go.AddComponent<MeshRenderer>();
            var unrelated = new Material(Shader.Find("HDRP/Lit"));
            var tinted = new Material(Classic);
            tinted.SetColor("_BaseColor", new Color(0.4f, 0.5f, 0.6f));
            tinted.SetColor("_EmissiveColor", new Color(2, 3, 4));
            tinted.SetTextureScale("_BaseColorMap", new Vector2(2, 3));
            tinted.SetTextureOffset("_BaseColorMap", new Vector2(0.1f, 0.2f));
            var slots = new[] { unrelated, Classic, null, tinted };
            renderer.sharedMaterials = slots;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_EnhancedTest", 0.37f);
            renderer.SetPropertyBlock(block);
            go.SetActive(false);
            try
            {
                for (int i = 0; i < 5; i++)
                {
                    HplEnhancedTextures.Apply(renderer, true);
                    var changed = renderer.sharedMaterials;
                    Check(changed[0] == unrelated && changed[1] == Enhanced && changed[2] == null && changed[3] != tinted,
                          "switch " + i + ": inactive renderer, unrelated/null slots preserved");
                    Check(changed[3].GetColor("_BaseColor") == tinted.GetColor("_BaseColor") &&
                          changed[3].GetColor("_EmissiveColor") == tinted.GetColor("_EmissiveColor") &&
                          changed[3].GetTextureScale("_BaseColorMap") == new Vector2(2, 3) &&
                          changed[3].GetTextureOffset("_BaseColorMap") == new Vector2(0.1f, 0.2f), "tint, emission and UV transform preserved");
                    HplEnhancedTextures.Apply(renderer, true);
                    Check(changed.SequenceEqual(renderer.sharedMaterials), "enhanced application is idempotent");
                    HplEnhancedTextures.Apply(renderer, false);
                    Check(renderer.sharedMaterials.SequenceEqual(slots), "classic restores all exact slot references");
                }
                renderer.GetPropertyBlock(block);
                Check(Mathf.Approximately(block.GetFloat("_EnhancedTest"), 0.37f), "renderer property block survives switching");
                HplEnhancedTextures.Apply(renderer, true);
                renderer.sharedMaterials = new[] { unrelated, Enhanced, unrelated, renderer.sharedMaterials[3] };
                HplEnhancedTextures.Apply(renderer, false);
                Check(renderer.sharedMaterials[2] == unrelated, "unrelated external slot changes are preserved");
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(unrelated); Object.DestroyImmediate(tinted); }
        }

        // Run without -quit. Exercises real HplGraphics.Changed subscriptions in Play, without saving preferences.
        public static void RunPlayBatch()
        {
            EditorSceneManager.OpenScene(AmnesiaPaths.ImportedAssetsRoot + "/Levels/maps/main/ch01/00_rainy_hall.unity", OpenSceneMode.Single);
            SessionState.SetBool(PlayKey, true);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PlayKey, false)) return;
            SessionState.SetBool(PlayKey, false);
            EditorApplication.delayCall += () =>
            {
                var saved = HplUserSettings.ReadSaved();
                bool hadPrefs = PlayerPrefs.HasKey(HplUserSettings.PrefsKey);
                string savedPrefs = PlayerPrefs.GetString(HplUserSettings.PrefsKey);
                void Restore()
                {
                    if (hadPrefs) PlayerPrefs.SetString(HplUserSettings.PrefsKey, savedPrefs);
                    else PlayerPrefs.DeleteKey(HplUserSettings.PrefsKey);
                    PlayerPrefs.Save();
                    HplGraphics.Apply(saved);
                }
                try
                {
                    var settings = saved.Copy();
                    settings.textures = 0;
                    HplGraphics.Apply(settings);
                    var snapshot = Snapshot(SceneManager.GetActiveScene());
                    var walls = snapshot.Where(p => p.Value.Contains(Classic)).Select(p => p.Key).ToArray();
                    Check(walls.Length > 0, "Play: original walls found");
                    settings.textures = 1;
                    // New maps read the saved UI settings, just as after the options dialog's OK.
                    // Restore the exact original key in all exit paths, without writing any profile file.
                    PlayerPrefs.SetString(HplUserSettings.PrefsKey, JsonUtility.ToJson(settings));
                    HplGraphics.Apply(settings);
                    Check(walls.All(r => r.sharedMaterials.Select(HplEnhancedRelief.SurfaceMaterial).Contains(Enhanced)), "Play: graphics Apply changes live material slots");
                    int totalMaterials = Resources.Load<HplTextureSet>(HplTextureSet.ResourceName).entries.Length;
                    Check(HplEnhancedTextures.LoadedMaterialCount > 0 && HplEnhancedTextures.LoadedMaterialCount < totalMaterials,
                        "Play: loads only materials used in this map");
                    foreach (int gb in HplGraphics.TextureMemoryBudgetsGb)
                    {
                        settings.textureMemoryBudgetGb = gb;
                        HplGraphics.Apply(settings);
                        Check(QualitySettings.streamingMipmapsActive && QualitySettings.streamingMipmapsMemoryBudget == gb * 1024f,
                            "Play: applies requested streaming memory budget " + gb + " GB");
                    }
                    settings.textureMemoryBudgetGb = saved.textureMemoryBudgetGb;
                    HplGraphics.Apply(settings);
                    var fadeObject = new GameObject("Texture set fade check");
                    var fadeRenderer = fadeObject.AddComponent<MeshRenderer>();
                    fadeRenderer.sharedMaterial = Enhanced;
                    var fader = new RTXMnesia.World.HplMeshFader(new[] { fadeRenderer });
                    fader.SetAlpha(0.5f);
                    settings.textures = 0;
                    HplGraphics.Apply(settings);
                    fader.SetAlpha(0.4f);
                    Check(fadeRenderer.sharedMaterial.GetTexture("_BaseColorMap") == Classic.GetTexture("_BaseColorMap"),
                        "Play: active fade follows the changed texture set");
                    fader.End();
                    Check(fadeRenderer.sharedMaterial == Classic, "Play: fade restores the exact current classic material");
                    Object.Destroy(fadeObject);
                    settings.textures = 1;
                    HplGraphics.Apply(settings);
                    // Load another scene while enhanced is chosen: sceneLoaded must apply automatically.
                    void VerifyLoaded(Scene next, LoadSceneMode mode)
                    {
                        SceneManager.sceneLoaded -= VerifyLoaded;
                        int exit = 1;
                        try
                        {
                            var nextSnapshot = Snapshot(next);
                            Debug.Log($"[EnhancedTextureCheck] loaded {next.name}, isLoaded={next.isLoaded}, set={HplGraphics.Textures}, classic={nextSnapshot.Count(p => p.Value.Contains(Classic))}, enhanced={nextSnapshot.Count(p => p.Value.Contains(Enhanced))}");
                            Check(nextSnapshot.Any(p => p.Value.Contains(Enhanced)) && !nextSnapshot.Any(p => p.Value.Contains(Classic)),
                                  "Play: new map automatically inherits enhanced selection");
                            settings.textures = 0;
                            HplGraphics.Apply(settings);
                            Check(Snapshot(next).Any(p => p.Value.Contains(Classic)) && !Snapshot(next).Any(p => p.Value.Contains(Enhanced)), "Play: new map switches back to classic");
                            Check(HplEnhancedTextures.LoadedMaterialCount == 0, "Play: classic mode releases the enhanced material cache");
                            exit = 0;
                            Debug.Log("[EnhancedTextureCheck] PLAY PASS");
                        }
                        catch (Exception e) { Debug.LogException(e); }
                        finally { Restore(); EditorApplication.Exit(exit); }
                    }
                    SceneManager.sceneLoaded += VerifyLoaded;
                    SceneManager.LoadSceneAsync(AmnesiaPaths.ImportedAssetsRoot + "/Levels/maps/main/ch01/01_old_archives.unity");
                }
                catch (Exception e) { Debug.LogException(e); Restore(); EditorApplication.Exit(1); }
            };
        }

        public static void RunRenderAndBuildBatch()
        {
            RunRenderBatch();
            BuildCheck.RunBatch();
        }

        public static void RunRenderBatch()
        {
            Directory.CreateDirectory(Folder);
            TestSlots();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            foreach (var r in Object.FindObjectsByType<Renderer>()) r.gameObject.SetActive(false);
            foreach (var l in Object.FindObjectsByType<Light>()) l.enabled = false;
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(0, 0, -2.6f), Quaternion.identity);
            camera.GetComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            var volume = new GameObject("PBR check exposure").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(3);
            var light = new GameObject("PBR raking light").AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = new Vector3(-1.5f, 1.1f, -1);
            light.range = 15;
            light.gameObject.AddComponent<HDAdditionalLightData>();
            light.lightUnit = LightUnit.Candela;
            light.intensity = 300;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.localScale = new Vector3(2, 2, 1);
            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = Classic;
            Color[] Render(string name)
            {
                Color[] pixels = null;
                ImportMenu.RenderToFile(camera, Path.Combine(Folder, name + ".png"), t => pixels = t.GetPixels());
                return pixels;
            }
            try
            {
                var original = Render("0_classic_atlas");
                HplEnhancedTextures.Apply(renderer, true);
                var enhanced = Render("1_enhanced_atlas");
                HplEnhancedTextures.Apply(renderer, false);
                var restored = Render("2_restored_atlas");
                double MeanDifference(Color[] a, Color[] b) => a.Zip(b, (x, y) => (double)Mathf.Abs(x.r - y.r) + Mathf.Abs(x.g - y.g) + Mathf.Abs(x.b - y.b)).Average() / 3;
                Check(MeanDifference(original, enhanced) > 0.001, "GPU: enhanced material visibly changes stone");
                Check(MeanDifference(original, restored) < 0.001, "GPU: classic render restored after switching");
                var catalog = AssetDatabase.LoadAssetAtPath<HplTextureSet>(EnhancedMaterialConverter.CatalogPath);
                foreach (string name in new[] { "castlebase_floor", "welder_wood", "church_window_glass", "cloth_pile", "hedge01" })
                {
                    var pair = catalog.entries.FirstOrDefault(e => e.classic != null && e.classic.name == name);
                    if (pair.classic == null) continue;
                    renderer.sharedMaterial = pair.classic;
                    var before = Render("batch_" + name + "_0_classic");
                    HplEnhancedTextures.Apply(renderer, true);
                    Check(renderer.sharedMaterial == pair.enhanced, "GPU: batch material selected: " + name);
                    Render("batch_" + name + "_1_enhanced");
                    HplEnhancedTextures.Apply(renderer, false);
                    var after = Render("batch_" + name + "_2_restored");
                    Check(MeanDifference(before, after) < 0.001, "GPU: batch material visually restored: " + name);
                }
                Debug.Log("[EnhancedTextureCheck] GPU PASS");
            }
            finally { HplEnhancedTextures.Shutdown(); volume.sharedProfile = null; Object.DestroyImmediate(profile); }

            // Existing wall geometry in the first map, lit by its own lights. No scene is saved.
            var scene = EditorSceneManager.OpenScene(AmnesiaPaths.ImportedAssetsRoot + "/Levels/maps/main/ch01/00_rainy_hall.unity", OpenSceneMode.Single);
            camera = Camera.main;
            ImportMenu.RenderToFile(camera, Path.Combine(Folder, "3_classic_level.png"));
            HplEnhancedTextures.ApplyScene(scene, true);
            ImportMenu.RenderToFile(camera, Path.Combine(Folder, "4_enhanced_level.png"));
            HplEnhancedTextures.Shutdown();
            RenderBudgetMenu();
        }

        static void RenderBudgetMenu()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Graphics budget menu check");
            string prefs = PlayerPrefs.GetString(HplUserSettings.PrefsKey, "");
            bool cursorVisible = Cursor.visible;
            var cursorLock = Cursor.lockState;
            try
            {
                var map = root.AddComponent<RTXMnesia.ScriptApi.HplMap>();
                map.hud = root.AddComponent<HplHud>();
                map.hud.Init(map);
                var ui = root.AddComponent<HplGameUi>();
                ui.Init(map, true);
                typeof(RTXMnesia.ScriptApi.HplMap).GetProperty("GameUi").SetValue(map, ui);
                ui.OpenOptions(1);
                Check(ui.Choose(MenuControl.TextureMemoryBudget), "GPU menu: memory dropdown is reachable");
                var camera = root.AddComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 300;
                var target = new RenderTexture(800, 600, 0);
                camera.targetTexture = target;
                var canvas = ui.Menu.Canvas;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                void Capture(string name, HplMenuCombo combo = null)
                {
                    ui.Menu.Refresh(false, true, 1, ui.DraftSettings, 1, 1, Vector2.zero, combo: combo);
                    Canvas.ForceUpdateCanvases();
                    var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
                    var pixels = new Color32[800 * 600];
                    for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(12, 12, 12, 255);
                    image.SetPixels32(pixels); image.Apply();
                    HudSnapshot.DrawCanvas(image, canvas, camera);
                    File.WriteAllBytes(Path.Combine(Folder, name + ".png"), image.EncodeToPNG());
                    Object.DestroyImmediate(image);
                }
                Capture("5_graphics_budget_dropdown", new HplMenuCombo { control = MenuControl.TextureMemoryBudget, hover = 3 });
                ui.ChooseComboItem(3);
                Check(ui.DraftSettings.textureMemoryBudgetGb == 10, "GPU menu: dropdown changes its draft to 10 GB");
                Capture("6_graphics_budget_selected");
                ui.Choose(MenuControl.Cancel);
                Check(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == prefs, "GPU menu: cancel leaves saved settings intact");
                camera.targetTexture = null;
                Object.DestroyImmediate(target);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Cursor.visible = cursorVisible;
                Cursor.lockState = cursorLock;
            }
        }
    }
}
