using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Regression checks for alpha decals: import, HDRP write masks, tinted level copies and stable references.
    /// RunBatch: -batchmode -nographics -quit; RunRenderBatch: -batchmode -quit (GPU required).
    /// Outputs: Logs/DecalCheck. Neither entry point saves scenes or changes user settings.
    /// </summary>
    public static class DecalMaterialTests
    {
        static int _checks;
        static string Folder => Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "DecalCheck");

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
            Debug.Log("[DecalCheck] ok: " + message);
        }

        static void Surface(Material m)
        {
            if (m != null && m.shader.name == "HDRP/Lit" && m.GetFloat("_SurfaceType") == 1)
            {
                Check(m.GetFloat("_EnableBlendModePreserveSpecularLighting") == 0,
                      m.name + ": transparent specular follows alpha (including alpha zero)");
                return;
            }
            Check(m != null && m.shader.name == "HDRP/Decal", "HDRP decal: " + m?.name);
            Check(m.GetFloat("_AffectMetal") == 0 && m.GetFloat("_AffectSmoothness") == 0 &&
                  m.GetFloat("_AffectAO") == 0 && !m.IsKeywordEnabled("_MATERIAL_AFFECTS_MASKMAP") &&
                  m.GetInt("_DecalColorMask2") == 0 && m.GetInt("_DecalColorMask3") == 0,
                  m.name + ": no metallic, smoothness or AO writes (properties, keyword and DBuffer masks)");
            Check(m.GetFloat("_DecalBlend") == 1 && m.GetFloat("_MaskBlendSrc") == 0,
                  m.name + ": full opacity, mask weight follows diffuse alpha");
        }

        static Material Create(Texture2D albedo, Texture2D normal)
        {
            var method = typeof(MaterialConverter).GetMethod("CreateDecal", BindingFlags.Static | BindingFlags.NonPublic);
            Func<TextureRole, Texture2D> texture = role => role == TextureRole.Albedo ? albedo : role == TextureRole.Normal ? normal : null;
            return (Material)method.Invoke(null, new object[] { texture });
        }

        static Material CreateTransparent(Texture2D albedo, string blend = "Alpha")
        {
            var method = typeof(MaterialConverter).GetMethod("CreateMaterial", BindingFlags.Static | BindingFlags.NonPublic);
            var source = new AmnesiaMaterial { SourcePath = Path.Combine(AmnesiaPaths.SourceRoot, "test.mat"),
                Type = AmnesiaMaterial.Kind.Translucent, BlendMode = blend };
            Func<TextureRole, Texture2D> texture = role => role == TextureRole.Albedo ? albedo : null;
            return (Material)method.Invoke(null, new object[] { source, texture, new ImportLog() });
        }

        static IEnumerable<Material> LevelCopies()
        {
            string root = AmnesiaPaths.ImportedAssetsRoot + "/Levels";
            foreach (string file in Directory.GetFiles(AmnesiaPaths.ToAbsolute(root), "*_generated.asset", SearchOption.AllDirectories))
            {
                string path = root + "/" + Path.GetRelativePath(AmnesiaPaths.ToAbsolute(root), file).Replace('\\', '/');
                foreach (var m in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>())
                    if (m.shader.name == "HDRP/Decal" || (m.shader.name == "HDRP/Lit" && m.GetFloat("_SurfaceType") == 1)) yield return m;
            }
        }

        static string Identity(Material m)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string guid, out long id))
                throw new InvalidOperationException("No asset identity: " + m.name);
            return guid + ":" + id;
        }

        [MenuItem("Tools/RTXMnesia/Tests/Alpha decal materials")]
        public static void RunBatch()
        {
            _checks = 0;
            var texture = new Texture2D(2, 2);
            try
            {
                foreach (bool albedo in new[] { false, true })
                    foreach (bool normal in new[] { false, true })
                    {
                        var m = Create(albedo ? texture : null, normal ? texture : null);
                        try
                        {
                            Surface(m);
                            Check(m.IsKeywordEnabled("_MATERIAL_AFFECTS_ALBEDO") == albedo &&
                                  m.IsKeywordEnabled("_MATERIAL_AFFECTS_NORMAL") == normal &&
                                  m.GetTexture("_BaseColorMap") == (albedo ? texture : null) &&
                                  m.GetTexture("_NormalMap") == (normal ? texture : null),
                                  $"texture/keyword combinations: albedo {albedo}, normal {normal}");
                        }
                        finally { Object.DestroyImmediate(m); }
                    }

                foreach (string blend in new[] { "Alpha", "PremulAlpha" })
                {
                    var m = CreateTransparent(texture, blend);
                    try
                    {
                        Surface(m);
                        Check(m.GetFloat("_BlendMode") == (blend == "Alpha" ? 0 : 4), blend + ": original blend mode kept");
                    }
                    finally { Object.DestroyImmediate(m); }
                }

                var files = Directory.GetFiles(AmnesiaPaths.SourceRoot, "*.mat", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        var m = AmnesiaMaterial.Load(f);
                        string blend = (m.BlendMode ?? "").ToLowerInvariant();
                        return (m.Type == AmnesiaMaterial.Kind.Decal || m.Type == AmnesiaMaterial.Kind.Translucent) &&
                               blend is not ("add" or "mul" or "mulx2");
                    }).OrderBy(f => f).ToArray();
                Check(files.Length > 0, $"found {files.Length} source alpha overlay materials");
                var materials = files.Select(f => AssetDatabase.LoadAssetAtPath<Material>(MaterialConverter.AssetPathFor(f))).ToArray();
                foreach (var m in materials) Surface(m);
                var copies = LevelCopies().ToArray();
                foreach (var m in copies) Surface(m);
                var all = materials.Concat(copies).ToArray();
                var identities = all.Select(Identity).ToArray();
                var colors = all.Select(m => m.GetColor("_BaseColor")).ToArray();
                var textures = all.Select(m => m.GetTexture("_BaseColorMap")).ToArray();

                // Simulate a stale copy left in an already imported map. A normal material reimport must repair it.
                Check(copies.Length > 0, $"found {copies.Length} tinted decal sub-assets");
                var stale = copies[0];
                var backup = new Material(stale);
                try
                {
                    Legacy(stale);
                    EditorUtility.SetDirty(stale);
                    AssetDatabase.SaveAssets();
                    MaterialConverter.ConvertMaterials(files, false, "Alpha decal regression reimport");
                    for (int i = 0; i < all.Length; i++)
                    {
                        Surface(all[i]);
                        Check(Identity(all[i]) == identities[i] && all[i].GetColor("_BaseColor") == colors[i] &&
                              all[i].GetTexture("_BaseColorMap") == textures[i],
                              all[i].name + ": reimport keeps GUID/local ID, tint and diffuse texture");
                    }
                }
                finally
                {
                    EditorUtility.CopySerialized(backup, stale);
                    EditorUtility.SetDirty(stale);
                    AssetDatabase.SaveAssets();
                    Object.DestroyImmediate(backup);
                }
                Debug.Log($"[DecalCheck] PASS: {_checks} checks; {materials.Length} materials, {copies.Length} level copies");
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static void Legacy(Material m)
        {
            if (m.shader.name == "HDRP/Lit")
            {
                m.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);
                HDMaterial.ValidateMaterial(m);
                return;
            }
            m.SetFloat("_AffectMetal", 1);
            m.SetFloat("_AffectSmoothness", 1);
            m.SetFloat("_DecalBlend", 0.5f);
            m.SetFloat("_MaskBlendSrc", 1);
            HDMaterial.ValidateMaterial(m);
        }

        public static void RunRenderBatch()
        {
            _checks = 0;
            Directory.CreateDirectory(Folder);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            foreach (var r in Object.FindObjectsByType<Renderer>()) r.gameObject.SetActive(false);
            foreach (var l in Object.FindObjectsByType<Light>()) l.enabled = false;
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(0, 0, -5), Quaternion.identity);
            camera.GetComponent<HDAdditionalCameraData>().antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            var volume = new GameObject("Decal check exposure").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(3);

            var floorMaterial = new Material(Shader.Find("HDRP/Lit"));
            floorMaterial.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f));
            floorMaterial.SetFloat("_Smoothness", 0.95f);
            HDMaterial.ValidateMaterial(floorMaterial);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.transform.localScale = new Vector3(10, 6, 1);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            var light = new GameObject("Decal check light").AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = new Vector3(-0.5f, 0.5f, -3);
            light.range = 20;
            light.gameObject.AddComponent<HDAdditionalLightData>();
            light.lightUnit = UnityEngine.Rendering.LightUnit.Candela;
            light.intensity = 1500;

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(new Color(0.6f, 0.02f, 0.01f, 0), 16).ToArray());
            texture.Apply();
            var fixedMaterial = Create(texture, null);
            var legacy = new Material(fixedMaterial);
            Legacy(legacy);
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.transform.position = new Vector3(0, 0, -0.001f);
            decal.transform.localScale = new Vector3(4, 3, 1);
            var renderer = decal.GetComponent<Renderer>();
            Color[] Render(string name, Material m)
            {
                decal.SetActive(m != null);
                renderer.sharedMaterial = m;
                Color[] pixels = null;
                ImportMenu.RenderToFile(camera, Path.Combine(Folder, name + ".png"), image => pixels = image.GetPixels());
                return pixels;
            }
            try
            {
                var baseline = Render("0_no_decal", null);
                var old = Render("1_transparent_legacy", legacy);
                var current = Render("2_transparent_fixed", fixedMaterial);
                float oldDifference = Difference(baseline, old), fixedDifference = Difference(baseline, current);
                Debug.Log($"[DecalCheck] transparent decal RGB mean difference: legacy={oldDifference:F6}, fixed={fixedDifference:F6}");
                Check(oldDifference > 0.002f, "legacy settings reproduce a visible rectangle at alpha zero");
                Check(fixedDifference < 0.001f, "fixed alpha-zero decal preserves the glossy surface");

                foreach (string blend in new[] { "Alpha", "PremulAlpha" })
                {
                    // Premultiplied input must have RGB zero wherever alpha is zero.
                    texture.SetPixels(Enumerable.Repeat(blend == "Alpha" ? new Color(0.6f, 0.02f, 0.01f, 0) : Color.clear, 16).ToArray());
                    texture.Apply();
                    var transparent = CreateTransparent(texture, blend);
                    var preserved = new Material(transparent);
                    try
                    {
                        Legacy(preserved);
                        var brightRectangle = Render("5_transparent_" + blend + "_legacy", preserved);
                        var invisible = Render("6_transparent_" + blend + "_fixed", transparent);
                        float legacyDifference = Difference(baseline, brightRectangle), alphaDifference = Difference(baseline, invisible);
                        Debug.Log($"[DecalCheck] {blend} transparent RGB difference: legacy={legacyDifference:F6}, fixed={alphaDifference:F6}");
                        Check(legacyDifference > 0.002f, blend + ": preserve-specular reproduces the alpha-zero rectangle");
                        Check(alphaDifference < 0.001f, blend + ": fixed transparent material leaves the glossy surface intact");
                    }
                    finally { Object.DestroyImmediate(transparent); Object.DestroyImmediate(preserved); }
                }

                texture.SetPixels(Enumerable.Repeat(new Color(0.6f, 0.02f, 0.01f, 1), 16).ToArray());
                texture.Apply();
                var full = Render("3_opaque_fixed", fixedMaterial);
                var half = new Material(fixedMaterial);
                try
                {
                    half.SetFloat("_DecalBlend", 0.5f);
                    HDMaterial.ValidateMaterial(half);
                    var pale = Render("4_opaque_half_blend", half);
                    float opacityDifference = Difference(full, pale);
                    Debug.Log($"[DecalCheck] full/half opacity RGB mean difference: {opacityDifference:F6}");
                    Check(opacityDifference > 0.01f, "full-opacity decal renders distinctly from the old half blend");
                }
                finally { Object.DestroyImmediate(half); }
                Debug.Log($"[DecalCheck] PASS: {_checks} GPU regression checks");
            }
            finally
            {
                Object.DestroyImmediate(fixedMaterial);
                Object.DestroyImmediate(legacy);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(floorMaterial);
                volume.sharedProfile = null;
                Object.DestroyImmediate(profile);
            }
        }

        static float Difference(Color[] a, Color[] b)
        {
            double sum = 0;
            int count = 0;
            for (int y = 420; y < 660; y++)
                for (int x = 760; x < 1160; x++)
                {
                    int i = y * 1920 + x;
                    sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
                    count += 3;
                }
            return (float)(sum / count);
        }

        // The room before the note/secret passage: translucent amnesia trails, not HDRP/Decal meshes.
        public static void RunArchivesRenderBatch()
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.OpenScene(AmnesiaPaths.ImportedAssetsRoot + "/Levels/maps/main/ch01/01_old_archives.unity", OpenSceneMode.Single);
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(-30.5f, -0.35f, 26.1f), Quaternion.Euler(10, 180, 0));
            var renderers = Object.FindObjectsByType<Renderer>();
            var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            var translucent = materials.Where(m => m.shader.name == "HDRP/Lit" && m.GetFloat("_SurfaceType") == 1).ToArray();
            var backups = translucent.Select(m => new Material(m)).ToArray();
            try
            {
                foreach (var m in translucent)
                {
                    m.SetFloat("_EnableBlendModePreserveSpecularLighting", 1);
                    HDMaterial.ValidateMaterial(m);
                }
                ImportMenu.RenderToFile(camera, Path.Combine(Folder, "Archives_0_imported.png"));
                foreach (var m in translucent)
                {
                    m.SetFloat("_EnableBlendModePreserveSpecularLighting", 0);
                    HDMaterial.ValidateMaterial(m);
                }
                ImportMenu.RenderToFile(camera, Path.Combine(Folder, "Archives_1_alpha_weighted_specular.png"));
                var multiply = renderers.Where(r => r.enabled && r.sharedMaterials.Any(m => m != null && m.shader.name == MaterialConverter.MultiplyShader)).ToArray();
                foreach (var r in multiply) r.enabled = false;
                ImportMenu.RenderToFile(camera, Path.Combine(Folder, "Archives_2_no_multiply.png"));
                foreach (var r in multiply) r.enabled = true;
                Debug.Log($"[DecalCheck] Archives: {translucent.Length} transparent materials compared");
            }
            finally
            {
                for (int i = 0; i < translucent.Length; i++)
                {
                    EditorUtility.CopySerialized(backups[i], translucent[i]);
                    Object.DestroyImmediate(backups[i]);
                }
            }
        }

        // Release player with the same HDRP asset and shader stripping as the game. Does not alter build scenes.
        public static void BuildRenderPlayerBatch()
        {
            string root = AmnesiaPaths.ImportedAssetsRoot + "/Preview/DecalPlayer";
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(root));
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            foreach (var r in Object.FindObjectsByType<Renderer>()) r.gameObject.SetActive(false);
            foreach (var l in Object.FindObjectsByType<Light>()) l.enabled = false;
            var camera = Camera.main;
            camera.transform.SetPositionAndRotation(new Vector3(0, 0, -5), Quaternion.identity);
            var volume = new GameObject("Decal test exposure").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var exposure = profile.Add<Exposure>(true);
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(3);
            Save(profile, "Exposure");
            // Volume components are separate objects and must also be persisted for the player.
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            volume.sharedProfile = profile;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.transform.localScale = new Vector3(10, 6, 1);
            var floorMaterial = new Material(Shader.Find("HDRP/Lit"));
            floorMaterial.SetColor("_BaseColor", new Color(0.25f, 0.25f, 0.25f));
            floorMaterial.SetFloat("_Smoothness", 0.95f);
            HDMaterial.ValidateMaterial(floorMaterial);
            Save(floorMaterial, "Floor");
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            var light = new GameObject("Decal test light").AddComponent<Light>();
            light.type = LightType.Point;
            light.transform.position = new Vector3(-0.5f, 0.5f, -3);
            light.range = 20;
            light.gameObject.AddComponent<HDAdditionalLightData>();
            light.lightUnit = LightUnit.Candela;
            light.intensity = 1500;
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(new Color(0.6f, 0.02f, 0.01f, 0), 16).ToArray());
            texture.Apply();
            Save(texture, "TransparentTexture");
            var premultiplied = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            premultiplied.SetPixels(Enumerable.Repeat(Color.clear, 16).ToArray());
            premultiplied.Apply();
            Save(premultiplied, "PremultipliedTexture");
            var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.transform.position = new Vector3(0, 0, -0.001f);
            overlay.transform.localScale = new Vector3(4, 3, 1);
            var probe = new GameObject("Player render regression").AddComponent<Dev.HplDecalRenderProbe>();
            probe.captureCamera = camera;
            probe.overlay = overlay.GetComponent<Renderer>();
            var tablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HplModelBuilder.PrefabPathFor(
                Path.Combine(AmnesiaPaths.SourceRoot, "entities/furniture/table_wood02/table_wood02.dae")));
            var table = (GameObject)PrefabUtility.InstantiatePrefab(tablePrefab);
            table.transform.position = new Vector3(3, -1, -1);
            table.transform.localScale = Vector3.one * 0.7f;
            probe.furniture = table.GetComponentsInChildren<MeshRenderer>();
            var materials = new List<Material>();
            foreach (string type in new[] { "Decal", "Alpha", "PremulAlpha" })
            {
                var fixedMaterial = type == "Decal" ? Create(texture, null) : CreateTransparent(type == "PremulAlpha" ? premultiplied : texture, type);
                var legacy = new Material(fixedMaterial);
                Legacy(legacy);
                Save(legacy, type + "_legacy");
                Save(fixedMaterial, type + "_fixed");
                materials.Add(legacy);
                materials.Add(fixedMaterial);
            }
            probe.materials = materials.ToArray();
            string scene = root + "/DecalPlayer.unity";
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scene);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.Combine(Folder, "Player", "DecalCheck.exe"),
                extraScriptingDefines = new[] { "RTXMNESIA_RENDER_TEST" },
            });
            Check(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded, "release render-test player built");

            void Save(Object asset, string name)
            {
                asset.name = name;
                string path = root + "/" + name + ".asset";
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(asset, path);
            }
        }
    }
}
