using System;
using System.IO;
using System.Linq;
using RTXMnesia.Importer;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    public static class LampTests
    {
        const string Scene = "Assets/AmnesiaImported/Levels/maps/main/ch01/01_old_archives.unity";

        public static void RunBatch()
        {
            try
            {
                Open();
                var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
                map.Build();
                var lamp = map.Entities.OfType<PropLamp>().First(p => !p.Lit && p.Name.StartsWith("torch_static01_"));
                var light = lamp.GameObject.GetComponentInChildren<AmnesiaLight>();
                lamp.UpdateLogic(1f / 60);
                CheckEmission(lamp, 0);
                lamp.SetLit(true, true);
                float min = float.MaxValue, max = 0;
                for (int i = 0; i < 1200; i++)
                {
                    lamp.UpdateLogic(1f / 60);
                    light.UpdateLight(1f / 60);
                    if (i < 120) continue;
                    min = Mathf.Min(min, light.CurrentColor.r);
                    max = Mathf.Max(max, light.CurrentColor.r);
                }
                Debug.Log($"[LampCheck] {lamp.Name}: lit range {min:F4}..{max:F4}, authored {light.flickerOffColor.r:F4}..{light.diffuseColor.r:F4}");
                if (min < Mathf.Min(light.diffuseColor.r, light.flickerOffColor.r) - 0.001f)
                    throw new InvalidOperationException("Ignited torch flickers toward its extinguished color");
                CheckEmission(lamp, 1);
                var saved = light.Save();
                light.SetColor(Color.clear);
                light.SetRadius(0.1f);
                light.Restore(saved);
                if (light.Save().color != saved.color || light.Save().radius != saved.radius)
                    throw new InvalidOperationException("Restored flicker on values do not match the save");
                foreach (var ceiling in map.Entities.OfType<PropLamp>().Where(p => p.Name.StartsWith("chandelier_")))
                {
                    if (ceiling.Lit) throw new InvalidOperationException("Expected the original archives' ceiling lamps to start unlit");
                    ceiling.UpdateLogic(1f / 60);
                    CheckEmission(ceiling, 0);
                    Debug.Log("[LampCheck] unlit ceiling lamp: " + ceiling.Name);
                }
                lamp.SetLit(false, false);
                for (int i = 0; i < 3; i++)
                    foreach (bool enhanced in new[] { true, false })
                    {
                        Hpl.HplEnhancedTextures.ApplyLoadedScenes(enhanced);
                        CheckEmission(lamp, 0);
                        CheckEmission(map.Entities.OfType<PropLamp>().First(p => p.Name == "chandelier_simple_short_1"), 0);
                    }
                var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/AmnesiaImported/Materials/entities/lamp/torch_static01/torch_static01.mat");
                if (sourceMaterial.GetColor("_EmissiveColor").maxColorComponent <= 0)
                    throw new InvalidOperationException("Shared torch material was extinguished for all instances");
                Debug.Log("[LampCheck] PASS");
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void CheckEmission(PropLamp lamp, float amount)
        {
            int count = 0;
            foreach (var renderer in lamp.GameObject.GetComponentsInChildren<MeshRenderer>())
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var material = renderer.sharedMaterials[i];
                    if (!renderer.TryGetComponent<HplEmission>(out var emission) ||
                        !material.HasProperty("_EmissiveColorMap") || material.GetTexture("_EmissiveColorMap") == null) continue;
                    count++;
                    float value = material.GetColor("_EmissiveColor").maxColorComponent;
                    if (emission.Amount != amount || (amount == 0 ? value != 0 : value <= 0))
                        throw new InvalidOperationException($"Lamp material emission at {amount}: {value}");
                    if (AssetDatabase.Contains(material)) throw new InvalidOperationException("Lamp is modifying a shared asset");
                }
            if (count == 0) throw new InvalidOperationException("Torch has no emissive material");
            Debug.Log($"[LampCheck] material emission {amount}: {count} private slots");
        }

        static void Open()
        {
            EditorSceneManager.OpenScene(Scene);
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            map.runScript = false;
            map.disableAutoSaves = map.disableEnemyAI = map.disableInsanity = true;
        }

        public static void BuildPlayerBatch()
        {
            Open();
            new GameObject("Lamp regression probe").AddComponent<Dev.HplLampProbe>();
            string root = AmnesiaPaths.ImportedAssetsRoot + "/Preview/LampPlayer";
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(root));
            AssetDatabase.Refresh();
            string scene = root + "/LampPlayer.unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scene);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scene }, target = BuildTarget.StandaloneWindows64,
                locationPathName = Path.GetFullPath("Logs/LampCheck/Player/LampCheck.exe"),
                extraScriptingDefines = new[] { "RTXMNESIA_LAMP_TEST" },
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        // Fast iteration of this dedicated Mono test player only; all scene/shader/resource data stays from
        // BuildPlayerBatch. Always build the normal game through BuildPipeline, without this diagnostic define.
        public static void RecompilePlayerBatch()
        {
            RunBatch();
            string output = Path.GetFullPath("Temp/LampPlayerScripts");
            var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(
                new UnityEditor.Build.Player.ScriptCompilationSettings {
                    target = BuildTarget.StandaloneWindows64, group = BuildTargetGroup.Standalone,
                    extraScriptingDefines = new[] { "RTXMNESIA_LAMP_TEST" },
                }, output);
            if (result.assemblies == null || result.assemblies.Count == 0) throw new InvalidOperationException("No player script assemblies");
            File.Copy(Path.Combine(output, "Assembly-CSharp.dll"), Path.GetFullPath("Logs/LampCheck/Player/LampCheck_Data/Managed/Assembly-CSharp.dll"), true);
        }

        public static void ValidateBatch()
        {
            RunBatch();
            FeatureTests.RunBatch();
            SaveTests.RunBatch();
            BuildCheck.RunBatch();
        }
    }
}
