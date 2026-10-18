using System;
using System.IO;
using System.Linq;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.EditorDev
{
    /// <summary>Settings migration / menu cancellation and floating-point shader readback. No monitor required.</summary>
    public static class HdrTests
    {
        const string Folder = "Logs/HdrCheck";
        static int s_checks;
        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("HDR: " + message);
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
                if (!HplHdr.Available)
                    Check(!HplHdr.Active && !HplHdr.Pending, "SDR display status does not call unsupported native HDR getters");
                var settings = new HplUserSettings();
                JsonUtility.FromJsonOverwrite("{\"gamma\":1.2}", settings);
                settings.Validate();
                Check(settings.hdrMode == 1 && settings.hdrPeak == 1000 && settings.hdrPaperWhite == 160,
                    "pre-HDR preferences migrate to safe defaults");
                settings.hdrPeak = float.NaN; settings.hdrPaperWhite = float.PositiveInfinity; settings.hdrBlack = -1;
                settings.Validate();
                Check(settings.hdrPeak == 1000 && settings.hdrPaperWhite == 160 && settings.hdrBlack == 0, "non-finite and negative calibration");
                settings.hdrMode = 99; settings.hdrPeak = 10000; settings.hdrPaperWhite = 1; settings.hdrBlack = 1;
                settings.Validate();
                Check(settings.hdrMode == 2 && settings.hdrPeak == 5000 && settings.hdrPaperWhite == 80 && settings.hdrBlack == 0.1f, "calibration limits");
                var copy = JsonUtility.FromJson<HplUserSettings>(JsonUtility.ToJson(settings));
                Check(copy.hdrMode == 2 && copy.hdrPeak == 5000 && copy.hdrPaperWhite == 80 && copy.hdrBlack == 0.1f, "JSON round trip");
                Menu(render);
                if (render) ShaderReadback();
                File.WriteAllText(Folder + "/Result.txt", $"PASS: {s_checks} HDR checks; GPU={render}. Physical HDR output requires a display check.\n");
                Debug.Log($"[HdrTests] PASS: {s_checks} checks; GPU={render}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }

        static void Menu(bool render)
        {
            var root = new GameObject("HDR menu check") { hideFlags = HideFlags.HideAndDontSave };
            var saved = HplUserSettings.ReadSaved();
            string prefs = PlayerPrefs.GetString(HplUserSettings.PrefsKey, "");
            bool cursorVisible = Cursor.visible;
            var cursorLock = Cursor.lockState;
            try
            {
                var map = root.AddComponent<HplMap>();
                map.hud = root.AddComponent<HplHud>();
                map.hud.Init(map);
                var ui = root.AddComponent<HplGameUi>();
                ui.Init(map, true);
                typeof(HplMap).GetProperty(nameof(HplMap.GameUi)).SetValue(map, ui);
                ui.OpenOptions(4);
                Check(ui.Choose(MenuControl.HdrTab), "HDR tab reachable");
                ui.DraftSettings.hdrPeak = 1000;
                Check(ui.Choose(MenuControl.HdrPeakMore) && ui.DraftSettings.hdrPeak == 1010 && HplHdr.Peak == 1010, "peak live preview");
                Check(ui.Choose(MenuControl.HdrWhiteMore) && HplHdr.CalibrationPattern == 1, "white preview selection");
                Check(ui.Choose(MenuControl.HdrBlackMore) && HplHdr.CalibrationPattern == 2, "black preview selection");
                Check(ui.Choose(MenuControl.HdrDefaults) && ui.DraftSettings.hdrPaperWhite == 160, "reset draft only");
                if (render)
                {
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
                    for (int pattern = 0; pattern < 3; pattern++)
                    {
                        HplHdr.CalibrationPattern = pattern;
                        ui.Menu.Refresh(false, true, 4, ui.DraftSettings, 4, 1, Vector2.zero);
                        Canvas.ForceUpdateCanvases();
                        var image = new Texture2D(800, 600, TextureFormat.RGB24, false);
                        var pixels = new Color32[800 * 600];
                        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(12, 12, 12, 255);
                        image.SetPixels32(pixels); image.Apply();
                        // GPU patch below, actual canvas meshes over it. This is a labelled SDR layout capture.
                        DrawPreview(image, pattern);
                        HudSnapshot.DrawCanvas(image, canvas, camera);
                        File.WriteAllBytes($"{Folder}/Menu_{pattern}.png", image.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(image);
                    }
                    camera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(target);
                }
                ui.Choose(MenuControl.Cancel);
                Check(!ui.ShowingOptions && HplHdr.Peak == saved.hdrPeak && HplHdr.PaperWhite == saved.hdrPaperWhite &&
                    !HplHdr.CalibrationVisible, "cancel restores output and hides calibration");
                Check(PlayerPrefs.GetString(HplUserSettings.PrefsKey, "") == prefs, "preview never writes preferences");
                ui.OpenOptions(4);
                ui.Choose(MenuControl.GraphicsTab);
                Check(!HplHdr.CalibrationVisible && ui.Choose(MenuControl.Resolution), "other tabs still work");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                HplHdr.Apply(saved);
                HplHdr.CalibrationVisible = false;
                Cursor.visible = cursorVisible; Cursor.lockState = cursorLock;
            }
        }

        static Material Material()
        {
            var shader = Shader.Find(Effects.HplScreenEffectsPass.ShaderName);
            Check(shader != null && shader.isSupported, "screen effect shader supported");
            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        static Texture2D Draw(Material material, int pass, int width, int height)
        {
            ShaderUtil.CompilePass(material, pass, true);
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            var cmd = new CommandBuffer();
            cmd.SetRenderTarget(target);
            cmd.SetViewport(new Rect(0, 0, width, height));
            cmd.DisableScissorRect();
            cmd.ClearRenderTarget(false, true, Color.black);
            // HDRP owns _ScreenSize in its bound constant buffer. SetGlobalVector alone is ignored once
            // that buffer has been bound by an editor camera. Use the installed package's actual layout.
            var globalType = typeof(UnityEngine.Rendering.HighDefinition.HDRenderPipeline).Assembly
                .GetType("UnityEngine.Rendering.HighDefinition.ShaderVariablesGlobal", true);
            var globals = Activator.CreateInstance(globalType);
            globalType.GetField("_ScreenSize").SetValue(globals, new Vector4(width, height, 1f / width, 1f / height));
            typeof(ConstantBuffer).GetMethods().Single(m => m.Name == "PushGlobal" && m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(CommandBuffer))
                .MakeGenericMethod(globalType).Invoke(null, new[] { (object)cmd, globals, Shader.PropertyToID("ShaderVariablesGlobal") });
            cmd.DrawProcedural(Matrix4x4.identity, material, pass, MeshTopology.Triangles, 3);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
            RenderTexture.active = target;
            var result = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return result;
        }

        static void DrawPreview(Texture2D image, int pattern)
        {
            var material = Material();
            material.SetVector("_HplEffectHdr", new Vector4(0, 160, 1000, 0));
            material.SetVector("_HplCalibrationRect", new Vector4(0, 0, 1, 1));
            material.SetVector("_HplCalibration", new Vector4(pattern, 1000, 160, 0));
            var patch = Draw(material, 9, 250, 150);
            for (int y = 0; y < 150; y++)
                for (int x = 0; x < 250; x++) image.SetPixel(420 + x, 600 - 224 - 150 + y, patch.GetPixel(x, y).gamma);
            image.Apply();
            UnityEngine.Object.DestroyImmediate(patch);
            UnityEngine.Object.DestroyImmediate(material);
        }

        static void ShaderReadback()
        {
            var material = Material();
            var source = new Texture2D(8, 8, TextureFormat.RGBAFloat, false, true);
            try
            {
                material.SetVector("_HplEffectHdr", new Vector4(1, 160, 1000, 0));
                // Rec709 display gamut; values at this stage are absolute luminance.
                HDROutputUtils.GetColorSpaceForGamut(ColorGamut.sRGB, out int gamut);
                material.SetInteger("_HDRColorspace", gamut);
                var colors = new Color[64];
                // Match Color.hlsl's extended sRGB transfer, not Mathf's >1 gamma-2.2 approximation.
                float encoded = 1.055f * Mathf.Pow(4, 1f / 2.4f) - 0.055f;
                for (int i = 0; i < colors.Length; i++) colors[i] = new Color(encoded, encoded, encoded, 1);
                source.SetPixels(colors); source.Apply();
                material.SetTexture("_HplSource", source);
                material.SetFloat("_HplFade", 0);
                material.SetFloat("_HplSepia", 0);
                material.SetVector("_HplAdd", Vector4.zero);
                material.SetVector("_HplRadialBlur", Vector4.zero);
                material.SetFloat("_HplMenuBlend", 0);
                foreach (int pass in new[] { 2, 5 })
                {
                    material.SetTexture("_HplMenuSharp", source);
                    var image = Draw(material, pass, 8, 8);
                    Check(Mathf.Abs(image.GetPixel(4, 4).r - 640) < 2, "effects / menu preserve 640 nit highlights above white, pass " + pass + ": " + image.GetPixel(4, 4).r);
                    UnityEngine.Object.DestroyImmediate(image);
                }
                material.SetFloat("_HplFade", 1);
                var black = Draw(material, 2, 8, 8);
                Check(black.GetPixel(4, 4).r < 0.00001f, "fade reaches physical black");
                UnityEngine.Object.DestroyImmediate(black);
                material.SetVector("_HplCalibrationRect", new Vector4(0, 0, 1, 1));
                material.SetVector("_HplCalibration", new Vector4(0, 1000, 160, 0));
                var peak = Draw(material, 9, 100, 100);
                Check(Mathf.Abs(peak.GetPixel(50, 50).r - 1000) < 1 && Mathf.Abs(peak.GetPixel(40, 40).r - 900) < 1,
                    "peak calibration bypasses paper white and tone mapping");
                UnityEngine.Object.DestroyImmediate(peak);
                foreach (var message in ShaderUtil.GetShaderMessages(material.shader))
                    Check(message.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error, message.message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }
    }
}
