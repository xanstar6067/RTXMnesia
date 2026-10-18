#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Editor-only Play check of the ray tracing lighting (RTXMnesia.EditorDev.RtPreview): the player view is rendered
    /// with the classic lighting and with ray-traced shadows at every level; whether the pipeline and the camera trace
    /// rays, which lights are traced, the frame time and the snapshots go to Logs/RtCheck. The user's saved settings
    /// are only read, never written.
    /// </summary>
    public sealed class HplRtProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.RtPreview.ExitCode";
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
            File.WriteAllText(Path.Combine(folder, $"RtReport_{mapName}.txt"), _report.ToString());
            Debug.Log("[RtCheck]\n" + _report);
            Finish(_failures == 0 ? 0 : 1);
        }

        void Check(bool ok, string what)
        {
            _report.AppendLine((ok ? "PASS " : "FAIL ") + what);
            if (!ok) _failures++;
        }

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
            var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            int shadowed = 0;
            foreach (var light in AmnesiaLight.All) if (light != null && light.castShadows) shadowed++;
            _report.AppendLine($"Map {mapName}, {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, output {width} x {height}, {AmnesiaLight.All.Count} lights, {shadowed} with shadows");
            Check(SystemInfo.supportsRayTracing, "the GPU and API support ray tracing");

            var saved = HplUserSettings.ReadSaved();
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = rt;

            var classic = saved.Copy();
            classic.lighting = 0;
            HplGraphics.Apply(classic);
            double classicMs = Capture(camera, rt, $"{mapName}_0_classic", out var classicImage);
            var hd = HDCamera.GetOrCreate(camera);
            Check(!hd.frameSettings.IsEnabled(FrameSettingsField.RayTracing), "classic lighting: the camera does not trace rays");
            Check(TracedCount() == 0, "classic lighting: no light has ray-traced shadows");
            _report.AppendLine($"Classic: {classicMs:0.00} ms/frame");

            byte[] highImage = null;
            for (int level = 0; level < HplRayTracing.ShadowLights.Length; level++)
            {
                var s = saved.Copy();
                s.lighting = 1;
                s.rtShadows = level;
                s.rtAo = 0;
                HplGraphics.Apply(s);
                double ms = Capture(camera, rt, $"{mapName}_{level + 1}_rtShadows{level}", out var image);
                if (level == HplRayTracing.ShadowsDefault) highImage = image;
                hd = HDCamera.GetOrCreate(camera);
                int traced = TracedCount();
                _report.AppendLine($"  HplRayTracing: active {HplRayTracing.Active}, supported {HplRayTracing.Supported}, shadows {HplRayTracing.Shadows}, selected {HplRayTracing.TracedLights}");
                _report.AppendLine($"RT shadows {level}: camera traces {hd.frameSettings.IsEnabled(FrameSettingsField.RayTracing)}, screen space shadows {hd.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows)}, traced {traced} of {HplRayTracing.ShadowedLights} in view, {ms:0.00} ms/frame");
                Check(hd.frameSettings.IsEnabled(FrameSettingsField.RayTracing), $"RT shadows {level}: the camera traces rays");
                if (level == 0)
                {
                    hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                    Check(hdrp != null && hdrp.rayTracingSupported, "the pipeline supports ray tracing");
                }
                if (level == 0) Check(traced == 0, "RT shadows off: lights keep their shadow maps");
                else
                {
                    int expected = Mathf.Min(HplRayTracing.ShadowLights[level], HplRayTracing.ShadowedLights);
                    Check(traced == expected, $"RT shadows {level}: {expected} lights traced (at most {HplRayTracing.ShadowLights[level]}, {HplRayTracing.ShadowedLights} in view)");
                    Check(hd.frameSettings.IsEnabled(FrameSettingsField.ScreenSpaceShadows), $"RT shadows {level}: screen space shadows on");
                    if (expected > 0) Check(TracedRadiusOk(), $"RT shadows {level}: traced lights have the source radius {HplRayTracing.SourceRadius}");
                }
            }
            if (HplRayTracing.ShadowedLights > 0)
                Check(highImage != null && Difference(classicImage, highImage) > 0.0001f, $"ray-traced shadows change the image (mean difference {Difference(classicImage, highImage):0.0000})");
            else _report.AppendLine("No light with shadows in view: the image check of ray-traced shadows is skipped");

            // Ray-traced ambient occlusion: every level, with the default shadows.
            byte[] aoOff = null, aoHigh = null;
            for (int level = 0; level < HplRayTracing.AoLevels; level++)
            {
                var s = saved.Copy();
                s.lighting = 1;
                s.rtShadows = HplRayTracing.ShadowsDefault;
                s.rtAo = level;
                HplGraphics.Apply(s);
                double ms = Capture(camera, rt, $"{mapName}_ao{level}", out var image);
                if (level == 0) aoOff = image;
                if (level == HplRayTracing.AoLevels - 1) aoHigh = image;
                var profile = HplGraphics.SettingsVolume != null ? HplGraphics.SettingsVolume.sharedProfile : null;
                ScreenSpaceAmbientOcclusion ao = null;
                bool found = profile != null && profile.TryGet(out ao);
                _report.AppendLine($"RTAO {level}: component {found}, active {ao?.active}, ray traced {ao?.rayTracing.value}, quality {ao?.quality.value}, {ms:0.00} ms/frame");
                Check(found && ao.active == (level > 0) && (level == 0 || ao.rayTracing.value && ao.quality.value == level - 1),
                      $"RTAO {level}: the settings volume {(level > 0 ? "traces AO at quality " + (level - 1) : "has no AO")}");
            }
            Check(Difference(aoOff, aoHigh) > 0.0001f, $"RTAO changes the image (mean difference {Difference(aoOff, aoHigh):0.0000})");

            // Key C: the comparison switches the lighting with a wipe, from the user's settings without RT and back.
            var ui = map.GameUi;
            if (ui != null)
            {
                var userSettings = ui.Settings;
                var start = saved.Copy();
                start.lighting = 0;
                ui.Settings = start;
                HplGraphics.Apply(start);
                yield return null;
                Effects.HplRtxCompare.Toggle(ui.Settings);
                int frames = 0;
                while (!Effects.HplRtxCompare.Wiping && frames++ < 30) yield return null;
                Check(Effects.HplRtxCompare.Wiping && HplRayTracing.Active, $"key C: the frame is kept and ray tracing switches on ({frames} frames)");
                yield return new WaitForSecondsRealtime(Effects.HplRtxCompare.WipeTime * 0.45f);
                Check(Effects.HplRtxCompare.LabelAlpha > 0f && Effects.HplRtxCompare.Label == "Трассировка лучей", "key C: the HUD names the new lighting");
                float progress = Effects.HplRtxCompare.Progress;
                SaveFrame(rt, $"{mapName}_compare_wipe");
                Check(progress > 0.05f && progress < 0.95f, $"key C: the wipe runs (progress {progress:0.00})");
                yield return new WaitForSecondsRealtime(Effects.HplRtxCompare.WipeTime);
                Check(!Effects.HplRtxCompare.Wiping, "key C: the wipe ends");
                Effects.HplRtxCompare.Toggle(ui.Settings);
                frames = 0;
                while (!Effects.HplRtxCompare.Wiping && frames++ < 30) yield return null;
                Check(Effects.HplRtxCompare.Wiping && !HplRayTracing.Active && Effects.HplRtxCompare.Label == "Классическое освещение", "key C again: back to the classic lighting");
                yield return new WaitForSecondsRealtime(Effects.HplRtxCompare.WipeTime + 0.1f);
                ui.Settings = userSettings;
            }

            HplGraphics.Apply(saved);
            yield return null;
            camera.Render();
            Check(saved.lighting == 1 || TracedCount() == 0, "the user's lighting is back");
            Check(HplRayTracing.Active == (saved.lighting == 1 && SystemInfo.supportsRayTracing), "the user's ray tracing setting is back");
            camera.targetTexture = null;
            rt.Release();
        }

        static int TracedCount()
        {
            int n = 0;
            foreach (var light in AmnesiaLight.All)
                if (light != null && light.TryGetComponent<HDAdditionalLightData>(out var hd) && hd.useRayTracedShadows) n++;
            return n;
        }

        static bool TracedRadiusOk()
        {
            foreach (var light in AmnesiaLight.All)
                if (light != null && light.TryGetComponent<HDAdditionalLightData>(out var hd) && hd.useRayTracedShadows &&
                    Mathf.Abs(light.GetComponent<Light>().shapeRadius - HplRayTracing.SourceRadius) > 1e-5f) return false;
            return true;
        }

        /// <summary>Mean absolute difference of two RGB24 images, 0..1.</summary>
        static float Difference(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0f;
            long sum = 0;
            for (int i = 0; i < a.Length; i++) sum += Math.Abs(a[i] - b[i]);
            return sum / (255f * a.Length);
        }

        /// <summary>Renders 40 frames (denoiser history), saves the last one; the mean GPU-synchronous frame time.</summary>
        double Capture(Camera camera, RenderTexture rt, string name, out byte[] pixels)
        {
            var watch = new System.Diagnostics.Stopwatch();
            for (int i = 0; i < 40; i++)
            {
                if (i == 20) watch.Start();
                camera.Render();
                AsyncGPUReadback.Request(rt, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
            }
            watch.Stop();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = previous;
            pixels = image.GetRawTextureData();
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Destroy(image);
            return watch.Elapsed.TotalMilliseconds / 20;
        }

        /// <summary>Saves what the camera last rendered to its target.</summary>
        void SaveFrame(RenderTexture rt, string name)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = previous;
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Destroy(image);
        }

        void Finish(int code)
        {
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}
#endif
