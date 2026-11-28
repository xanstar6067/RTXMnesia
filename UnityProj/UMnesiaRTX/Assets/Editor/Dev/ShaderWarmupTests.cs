using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEditor;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RTXMnesia.EditorDev
{
    [InitializeOnLoad]
    public static class ShaderWarmupTests
    {
        const string Folder = "Logs/ShaderWarmupCheck";
        static int s_checks;
        static GraphicsStateCollection s_collection;
        static JobHandle s_job;
        static double s_started;
        static ShaderWarmupTests() => EditorApplication.update += UpdateGpu;
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("ShaderWarmup: " + message);
            s_checks++;
        }

        public static void RunBatch() => Run(false);
        public static void RunRenderBatch() => Run(true);

        static void Run(bool render)
        {
            try
            {
                s_checks = 0;
                Directory.CreateDirectory(Folder);
                string file = Path.GetFullPath(Folder + "/test-receipt.json");
                File.Delete(file);
                string Key(string revision = "r1", string driver = "d1", string device = "gpu1", string api = "dx12", string engine = "u1") =>
                    HplShaderWarmupCache.Fingerprint(revision, driver, device, api, engine);
                string key = Key();
                Check(!HplShaderWarmupCache.Matches(file, key), "first run needs warmup");
                HplShaderWarmupCache.Save(file, key);
                Check(HplShaderWarmupCache.Matches(file, Key()), "second process can reuse the saved receipt");
                Check(!HplShaderWarmupCache.Matches(file, Key(driver: "d2")), "driver update invalidates receipt");
                Check(!HplShaderWarmupCache.Matches(file, Key(device: "gpu2")), "different GPU invalidates receipt");
                Check(!HplShaderWarmupCache.Matches(file, Key(api: "dx11")), "DX11 fallback has a separate fingerprint");
                Check(!HplShaderWarmupCache.Matches(file, Key(revision: "r2")), "shader changes invalidate receipt");
                Check(!HplShaderWarmupCache.Matches(file, Key(engine: "u2")), "engine changes invalidate receipt");
                File.WriteAllText(file, "{broken");
                Check(!HplShaderWarmupCache.Matches(file, key), "truncated receipt retries safely");
                HplShaderWarmupCache.Save(file, key);
                HplShaderWarmupCache.Save(file, Key(driver: "d2"));
                Check(HplShaderWarmupCache.Matches(file, Key(driver: "d2")) && !HplShaderWarmupCache.Matches(file, key), "atomic replacement retains only the completed environment");
                File.Delete(file);
                File.WriteAllText(file + ".tmp", "incomplete");
                Check(!HplShaderWarmupCache.Matches(file, key), "interrupted write is not a successful receipt");
                File.Delete(file + ".tmp");
                if (render) { BeginGpu(); return; }
                File.WriteAllText(Folder + "/cache-result.txt", $"PASS: {s_checks} checks; GPU={render}\n");
                Debug.Log($"[ShaderWarmupTests] PASS: {s_checks} checks; GPU={render}");
                BuildCheck.RunBatch();
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void BeginGpu()
        {
            ShaderWarmupBuilder.Ensure();
            var manifest = AssetDatabase.LoadAssetAtPath<HplShaderWarmupManifest>(ShaderWarmupBuilder.ManifestPath);
            Check(manifest != null && manifest.collection.totalGraphicsStateCount > 0, "nonempty raster collection included through Resources");
            string revision = manifest.revision;
            var writeTime = File.GetLastWriteTimeUtc(AssetDatabase.GetAssetPath(manifest.collection));
            ShaderWarmupBuilder.Ensure();
            Check(manifest.revision == revision && File.GetLastWriteTimeUtc(AssetDatabase.GetAssetPath(manifest.collection)) == writeTime,
                "unchanged shader inputs reuse the generated collection");
            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            manifest.collection.GetVariants(variants);
            Check(variants.Any(v => v.shader.name == "HDRP/Lit"), "HDRP materials are generated");
            Check(variants.Any(v => v.shader.name.StartsWith("Hidden/RTXMnesia/", StringComparison.Ordinal)), "runtime fullscreen effects are generated");
            Check(variants.All(v => !ShaderWarmupBuilder.PassMode(v.shader, v.passId).Contains("DXR") &&
                                   !ShaderWarmupBuilder.PassMode(v.shader, v.passId).Contains("RayTracing")), "no RTX passes in the collection");
            Check(variants.Any(v => v.keywords.Any(k => k.name == "_PIXEL_DISPLACEMENT")), "POM variants generated without executing POM");
            s_collection = new GraphicsStateCollection { runtimePlatform = Application.platform, graphicsDeviceType = SystemInfo.graphicsDeviceType,
                qualityLevelName = QualitySettings.names[QualitySettings.GetQualityLevel()] };
            var states = new List<GraphicsStateCollection.GraphicsState>();
            // Exercise native jobs on representative ordinary shader states, without recompiling every editor variant.
            foreach (string shader in new[] { "HDRP/Lit", "Hidden/RTXMnesia/HplGui", "Hidden/RTXMnesia/HplScreenEffects" })
            {
                var variant = variants.First(v => v.shader.name == shader && !v.keywords.Any(k => k.name == "_PIXEL_DISPLACEMENT"));
                s_collection.AddVariant(variant.shader, variant.passId, variant.keywords);
                manifest.collection.GetGraphicsStatesForVariant(variant, states);
                s_collection.AddGraphicsStateForVariant(variant, states[0]);
            }
            s_started = EditorApplication.timeSinceStartup;
            s_job = s_collection.WarmUpProgressively(1);
            JobHandle.ScheduleBatchedJobs();
        }

        static void UpdateGpu()
        {
            if (s_collection == null) return;
            try
            {
                if (EditorApplication.timeSinceStartup - s_started > 180) throw new TimeoutException("GPU warmup test timed out");
                if (!s_job.IsCompleted) return;
                s_job.Complete();
                if (!s_collection.isWarmedUp)
                {
                    s_job = s_collection.WarmUpProgressively(1);
                    JobHandle.ScheduleBatchedJobs();
                    return;
                }
                Check(s_collection.completedWarmupCount > 0, "real progressive GPU warmup finishes");
                Object.DestroyImmediate(s_collection);
                s_collection = null;
                string driver = HplVideoMemory.DriverVersion;
                Check(!string.IsNullOrEmpty(driver), "DXGI provides the actual display driver version");
                Debug.Log("[ShaderWarmupTests] DXGI driver=" + driver);
                RenderScreen();
                File.WriteAllText(Folder + "/gpu-result.txt", $"PASS: {s_checks} checks; GPU=True\n");
                Debug.Log($"[ShaderWarmupTests] PASS: {s_checks} checks; GPU=True");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { s_collection = null; Debug.LogException(e); EditorApplication.Exit(1); }
        }

        static void RenderScreen()
        {
            var root = new GameObject("Shader warmup UI check");
            var map = root.AddComponent<HplMap>();
            map.runScript = false;
            map.hud = root.AddComponent<HplHud>();
            var screen = root.AddComponent<HplShaderWarmupScreen>();
            var target = new RenderTexture(800, 600, 0);
            var camera = root.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 300;
            camera.targetTexture = target;
            try
            {
                screen.Build(map);
                screen.DrawProgress(0.47f);
                Check(screen.Canvas.GetComponentsInChildren<HplGuiText>().Single().font != null, "screen uses the imported game font");
                screen.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                screen.Canvas.worldCamera = camera;
                screen.Canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
                try
                {
                    image.SetPixels(Enumerable.Repeat(Color.black, 800 * 600).ToArray());
                    image.Apply();
                    HudSnapshot.DrawCanvas(image, screen.Canvas, camera);
                    File.WriteAllBytes(Folder + "/screen.png", image.EncodeToPNG());
                }
                finally { Object.DestroyImmediate(image); }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(target); }
        }
    }
}
