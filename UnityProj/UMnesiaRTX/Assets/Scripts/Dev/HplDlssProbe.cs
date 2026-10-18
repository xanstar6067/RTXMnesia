#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play check of DLSS (RTXMnesia.EditorDev.DlssPreview): the player view is rendered with DLSS off, DLAA and
    /// the upscaling modes, each after enough frames for its history; the render size, whether HDRP really runs DLSS on the
    /// camera and the snapshots go to Logs/DlssCheck. A second pass repeats Off / Performance with the HPL screen effects
    /// (sepia, radial blur, trail) that run after the upscale. The user's saved settings are only read, never written.
    /// </summary>
    public sealed class HplDlssProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.DlssPreview.ExitCode";
        /// <summary>Place the player at <see cref="position"/>; otherwise the map's start position is used.</summary>
        public bool place;
        public Vector3 position;
        public float yawDegrees;
        public string aim;
        public string folder;
        public string mapName;
        public int width = 1920, height = 1080;

        readonly StringBuilder _report = new();
        int _failures;

        IEnumerator Start()
        {
            var run = Run();
            while (true)
            {
                bool more = false, failed = false; object current = null;
                try { more = run.MoveNext(); if (more) current = run.Current; }
                catch (Exception exception) { Debug.LogException(exception); failed = true; }
                if (failed) { Finish(1); yield break; }
                if (!more) break;
                yield return current;
            }
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, $"DlssReport_{mapName}.txt"), _report.ToString());
            Debug.Log("[DlssCheck]\n" + _report);
            Finish(_failures == 0 ? 0 : 1);
        }

        void Check(bool ok, string what)
        {
            _report.AppendLine((ok ? "PASS " : "FAIL ") + what);
            if (!ok) _failures++;
        }

        static readonly FieldInfo CanRenderDlss = typeof(HDAdditionalCameraData).GetField("cameraCanRenderDLSS", BindingFlags.Instance | BindingFlags.NonPublic);

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1f);
            var map = HplMap.Current;
            if (map == null || map.player == null) throw new InvalidOperationException("no map");
            var player = map.player;
            player.readDevices = false;
            if (map.GameUi != null && map.GameUi.IsPaused) map.GameUi.Resume();
            map.effects.Fade.FadeIn(0f);
            player.active = true;
            if (place) player.PlaceAt(position, yawDegrees * Mathf.Deg2Rad);
            yield return new WaitForSecondsRealtime(0.5f);
            if (!string.IsNullOrEmpty(aim) && aim.StartsWith("pitch=", StringComparison.Ordinal))
                player.AddLook(0f, -float.Parse(aim.Substring(6), System.Globalization.CultureInfo.InvariantCulture) * Mathf.Deg2Rad - player.Pitch);
            else if (!string.IsNullOrEmpty(aim) && map.GetEntityByName(aim) is { } target)
            {
                var body = target.Bodies.Count > 0 ? target.Bodies[0] : null;
                Vector3 goal = body?.Physics != null ? body.Physics.WorldCenterOfMass : target.GameObject.transform.position;
                Vector3 dir = goal - player.LogicCameraPosition;
                float yaw = Mathf.Atan2(dir.x, dir.z), pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude);
                player.AddLook(Mathf.DeltaAngle(player.Yaw * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg) * Mathf.Deg2Rad, pitch - player.Pitch);
            }
            yield return new WaitForSecondsRealtime(0.5f);

            var camera = map.PlayerCamera;
            var data = camera.GetComponent<HDAdditionalCameraData>();
            _report.AppendLine($"Map {mapName}, {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, output {width} x {height}");
            _report.AppendLine($"DLSS detected: {HplDlss.Available}, ray tracing supported: {SystemInfo.supportsRayTracing}");
            Check(HplDlss.Available, "HDRP detected DLSS (NVIDIA plugin and RTX GPU)");
            Check(CanRenderDlss != null, "HDAdditionalCameraData.cameraCanRenderDLSS found");
            var presets = HplDlss.Presets();
            var names = new List<string>();
            foreach (uint p in presets) names.Add(p == 0 ? "default" : HplDlss.PresetLetter(p));
            _report.AppendLine("Presets: " + string.Join(", ", names));
            foreach (HplDlssMode m in Enum.GetValues(typeof(HplDlssMode)))
                if (m != HplDlssMode.Off)
                {
                    uint mask = HplDlss.AvailablePresets(m);
                    var allowed = new List<string>();
                    foreach (uint p in presets) if (p != 0 && (mask & p) != 0) allowed.Add(HplDlss.PresetLetter(p));
                    _report.AppendLine($"  {m}: {string.Join(", ", allowed)}");
                }

            var saved = HplUserSettings.ReadSaved();
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = rt;
            var modes = new[] { HplDlssMode.Off, HplDlssMode.Dlaa, HplDlssMode.Quality, HplDlssMode.Balanced, HplDlssMode.Performance, HplDlssMode.UltraPerformance };
            // NVIDIA's ratios; HDRP rounds the percentage it gets from the optimal settings (Quality: 0.70).
            float[] expectedScale = { 1f, 1f, 0.667f, 0.58f, 0.5f, 0.333f };
            for (int i = 0; i < modes.Length; i++)
            {
                var s = saved.Copy();
                s.dlssMode = (int)modes[i];
                s.dlssPreset = 0;
                HplGraphics.Apply(s);
                double ms = Capture(camera, rt, $"{mapName}_{i}_{modes[i]}");
                var hd = HDCamera.GetOrCreate(camera);
                bool dlss = CanRenderDlss != null && (bool)CanRenderDlss.GetValue(data);
                float scale = hd.actualWidth / (float)width;
                _report.AppendLine($"{modes[i]}: render {hd.actualWidth} x {hd.actualHeight} ({scale:0.000}), DLSS on camera {dlss}, AA {data.antialiasing}, {ms:0.00} ms/frame");
                Check(dlss == (modes[i] != HplDlssMode.Off), $"{modes[i]}: DLSS runs on the camera only when chosen");
                Check(Mathf.Abs(scale - expectedScale[i]) < 0.04f, $"{modes[i]}: render scale {scale:0.000} ≈ {expectedScale[i]}");
                if (modes[i] != HplDlssMode.Off) Check(data.antialiasing == HDAdditionalCameraData.AntialiasingMode.None, $"{modes[i]}: camera anti-aliasing replaced by DLSS");
            }

            // A model other than the default: the first one NVIDIA offers for Quality.
            uint qualityMask = HplDlss.AvailablePresets(HplDlssMode.Quality);
            uint chosen = 0;
            foreach (uint p in presets) if (p != 0 && (qualityMask & p) != 0) chosen = p;
            if (chosen != 0)
            {
                var s = saved.Copy();
                s.dlssMode = (int)HplDlssMode.Quality;
                s.dlssPreset = (int)chosen;
                HplGraphics.Apply(s);
                var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
                uint inAsset = asset != null ? asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSRenderPresetForQuality : 0;
                Check(inAsset == chosen, $"model {HplDlss.PresetLetter(chosen)} written to the pipeline (in memory)");
                Capture(camera, rt, $"{mapName}_Quality_preset{HplDlss.PresetLetter(chosen)}");
            }

            // Screen effects after the upscale: sepia, radial blur, trail at the output size.
            var fx = map.effects;
            fx.Sepia.FadeTo(1f, 1000f);
            fx.RadialBlur.FadeTo(0.3f, 1000f);
            fx.RadialBlur.SetBlurStartDist(0.2f);
            fx.ImageTrail.FadeTo(0.5f, 1000f);
            // The effects reach their values in the map's next steps.
            yield return new WaitForSecondsRealtime(0.5f);
            _report.AppendLine($"Effects: sepia {fx.Sepia.Amount:0.00}, radial blur {fx.RadialBlur.Size:0.00}, trail {fx.ImageTrail.Amount:0.00}");
            foreach (var mode in new[] { HplDlssMode.Off, HplDlssMode.Performance })
            {
                var s = saved.Copy();
                s.dlssMode = (int)mode;
                s.radialBlur = s.imageTrail = true;
                HplGraphics.Apply(s);
                Capture(camera, rt, $"{mapName}_effects_{mode}");
            }
            fx.ResetAll();

            HplGraphics.Apply(saved);
            camera.targetTexture = null;
            rt.Release();
            Check(HplDlss.Mode == (HplDlssMode)saved.dlssMode, "the user's DLSS setting is back");
        }

        /// <summary>Renders 40 frames (DLSS history, effects), saves the last one; the mean GPU-synchronous frame time.</summary>
        double Capture(Camera camera, RenderTexture rt, string name)
        {
            var watch = new System.Diagnostics.Stopwatch();
            for (int i = 0; i < 40; i++)
            {
                if (i == 20) watch.Start();
                camera.Render();
                UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
            }
            watch.Stop();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = previous;
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Destroy(image);
            return watch.Elapsed.TotalMilliseconds / 20;
        }

        void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
