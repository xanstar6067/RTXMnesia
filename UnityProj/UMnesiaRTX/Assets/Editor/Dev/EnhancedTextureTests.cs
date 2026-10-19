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
        static string EnhancedPath => EnhancedMaterialConverter.Root + "/" + EnhancedMaterialConverter.Stone + ".mat";
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
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
        }

        static Dictionary<string, string> ClassicHashes() => AssetDatabase.GetDependencies(ClassicPath)
            .Where(p => p.StartsWith(AmnesiaPaths.ImportedAssetsRoot + "/", StringComparison.Ordinal))
            .SelectMany(p => new[] { p, p + ".meta" }).Where(File.Exists).ToDictionary(p => p, Hash);

        [MenuItem("Tools/RTXMnesia/Tests/Enhanced textures")]
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);
            var hashes = ClassicHashes();
            Check(hashes.Count >= 10, "classic material, textures and metas included in byte audit");
            EnhancedMaterialConverter.ConvertBatch();
            var ids = AssetDatabase.GetDependencies(EnhancedMaterialConverter.CatalogPath)
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
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                Check(texture != null && texture.width == 4096 && texture.height == 4096 && importer.mipmapEnabled &&
                      importer.wrapMode == TextureWrapMode.Repeat && importer.sRGBTexture == (property == "_BaseColorMap"), property + ": 4K, mipmaps, repeat and correct colour space");
                if (property == "_NormalMap") Check(importer.textureType == TextureImporterType.NormalMap && importer.flipGreenChannel,
                    "NormalV3 G corrected once at import, source PNG retained");
            }

            var settings = new HplUserSettings { textures = 1 };
            settings.Validate();
            Check(settings.textures == 1 && JsonUtility.FromJson<HplUserSettings>(JsonUtility.ToJson(settings)).textures == 1,
                  "enhanced selection survives validation and JSON serialization");
            var choices = HplGraphicsOptions.Items(MenuControl.Textures, settings, (c, k, f) => f, out int selected);
            Check(choices.Count == 2 && choices[1].enabled && selected == 1, "existing textures dropdown enables enhanced set");

            TestSlots();
            foreach (string map in new[] { "00_rainy_hall", "01_old_archives" })
            {
                var scene = EditorSceneManager.OpenScene(AmnesiaPaths.ImportedAssetsRoot + "/Levels/maps/main/ch01/" + map + ".unity", OpenSceneMode.Single);
                var snapshots = Snapshot(scene);
                int targets = snapshots.Count(p => p.Value.Contains(Classic));
                Check(targets > 0, map + ": contains the original castle stone (" + targets + " renderers)");
                HplEnhancedTextures.ApplyScene(scene, true);
                Check(snapshots.Where(p => p.Value.Contains(Classic)).All(p => p.Key.sharedMaterials.Contains(Enhanced)), map + ": all matching walls replaced");
                HplEnhancedTextures.ApplyScene(scene, false);
                Check(Same(snapshots), map + ": all slots restored to exact original references");
            }
            HplEnhancedTextures.Shutdown();
            Debug.Log($"[EnhancedTextureCheck] PASS: {s_checks} checks");
        }

        static Dictionary<Renderer, Material[]> Snapshot(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Renderer>(true)).ToDictionary(r => r, r => r.sharedMaterials);
        static bool Same(Dictionary<Renderer, Material[]> snapshot) => snapshot.All(p => p.Key.sharedMaterials.SequenceEqual(p.Value));

        static void TestSlots()
        {
            var go = new GameObject("Enhanced material slot check");
            var renderer = go.AddComponent<MeshRenderer>();
            var unrelated = new Material(Shader.Find("HDRP/Lit"));
            var tinted = new Material(Classic);
            tinted.SetColor("_BaseColor", new Color(0.4f, 0.5f, 0.6f));
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
                          changed[3].GetTextureScale("_BaseColorMap") == new Vector2(2, 3) &&
                          changed[3].GetTextureOffset("_BaseColorMap") == new Vector2(0.1f, 0.2f), "tint and UV transform preserved");
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
                    Check(walls.All(r => r.sharedMaterials.Contains(Enhanced)), "Play: graphics Apply changes live material slots");
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

        public static void RunRenderBatch()
        {
            Directory.CreateDirectory(Folder);
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
        }
    }
}
