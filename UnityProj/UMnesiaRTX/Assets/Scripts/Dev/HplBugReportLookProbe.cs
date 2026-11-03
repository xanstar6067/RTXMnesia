#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// The [Взгляд] section of the bug report at the places of existing reports (EditorDev.BugReportTests.RunLookBatch):
    /// each report folder's save.sav is loaded and the section is written next to the one the report has, in
    /// Logs/BugReportLook.txt. Folders: -bugReports "dir1;dir2". With -lookShots <dir>: the view of each report in
    /// classic / enhanced textures, without and with ray tracing (the saved RT levels), as PNG files there.
    /// </summary>
    public sealed class HplBugReportLookProbe : MonoBehaviour
    {
        public const string ExitCodeKey = "RTXMnesia.BugReportTests.LookExitCode";
        const string ResultFile = "Logs/BugReportLook.txt";

        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);
            var result = new StringBuilder();
            int code = 0;
            var run = Run(result);
            while (true)
            {
                bool more;
                try { more = run.MoveNext(); }
                catch (Exception e) { Debug.LogException(e); result.AppendLine("FAIL: " + e); code = 1; break; }
                if (!more) break;
                yield return run.Current;
            }
            File.WriteAllText(Path.GetFullPath(ResultFile), result.ToString(), new UTF8Encoding(false));
            UnityEditor.SessionState.SetInt(ExitCodeKey, code);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        IEnumerator Run(StringBuilder result)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-bugReports");
            if (at < 0 || at + 1 >= args.Length) throw new InvalidOperationException("-bugReports \"dir1;dir2\" expected");
            yield return new WaitForSecondsRealtime(1);
            bool gpuChecked = false;
            foreach (string folder in args[at + 1].Split(';').Where(f => f.Length > 0))
            {
                var ui = HplMap.Current != null && HplMap.Current.GameUi != null ? HplMap.Current.GameUi : FindAnyObjectByType<HplMainMenu>()?.Ui;
                if (ui == null) throw new InvalidOperationException("No game UI to load " + folder);
                var previous = HplMap.Current;
                if (!ui.LoadSaveFile(Path.Combine(folder, "save.sav"))) throw new InvalidOperationException("save.sav rejected: " + folder);
                double deadline = Time.realtimeSinceStartupAsDouble + 120;
                while (HplMap.Current == null || HplMap.Current == previous || HplLoadScreen.Active || HplMap.Current.GameUi == null
                       || HplMap.Current.GameUi.IsTransitioning)
                {
                    if (Time.realtimeSinceStartupAsDouble > deadline) throw new InvalidOperationException("Loading timed out: " + folder);
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(2);
                var map = HplMap.Current;
                if (map.GameUi.IsPaused) map.GameUi.Resume();
                yield return null;
                if (!gpuChecked)
                {
                    // The GPU read back used for meshes without CPU data, on a readable one: the same triangles.
                    var sample = FindObjectsByType<MeshFilter>().Select(f => f.sharedMesh).FirstOrDefault(m => m != null && m.isReadable && m.vertexCount > 100);
                    if (sample == null) throw new InvalidOperationException("No readable mesh for the GPU read back check");
                    if (!HplVisibleHit.GpuReadMatches(sample, out string error)) throw new InvalidOperationException("GPU read back of " + sample.name + ": " + error);
                    result.AppendLine($"GPU read back = CPU data: {sample.name}, {sample.vertexCount} vertices, {sample.subMeshCount} sub meshes, {sample.indexFormat}");
                    gpuChecked = true;
                }
                var report = HplBugReport.Begin(this, map);
                for (double end = Time.realtimeSinceStartupAsDouble + 3; !report.Ready && Time.realtimeSinceStartupAsDouble < end;) yield return null;
                string old = File.ReadAllText(Path.Combine(folder, "report.txt"));
                result.AppendLine("== " + Path.GetFileName(folder));
                result.AppendLine("-- в отчёте:").AppendLine(Section(old, "[Взгляд]"));
                result.AppendLine("-- сейчас:").AppendLine(Section(report.ReportText, "[Взгляд]"));
                var camera = map.PlayerCamera;
                var ray = new Ray(map.player.LogicCameraPosition, map.player.LogicCameraRotation * Vector3.forward);
                result.AppendLine("-- все видимые попадания луча:");
                foreach (var hit in HplVisibleHit.Cast(ray, 100, map.gameObject.scene, camera.cullingMask, map.player.Hands?.Root, null).Take(8))
                    result.AppendLine($"{hit.distance:0.000} м {hit.renderer.name} ({hit.renderer.transform.parent?.name}) слот {hit.slot} {(hit.Transparent ? "прозрачное" : "")} {(hit.backFace ? "обратная сторона" : "")}");
                report.Dispose();
                int shots = Array.IndexOf(args, "-lookShots");
                if (shots >= 0 && shots + 1 < args.Length)
                    yield return Shots(map, Path.Combine(args[shots + 1], Path.GetFileName(folder)), result);
            }
        }

        const int ShotWidth = 1600, ShotHeight = 900, ShotFrames = 48;

        static IEnumerator Shots(HplMap map, string prefix, StringBuilder result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefix));
            var saved = HplUserSettings.ReadSaved();
            var camera = map.PlayerCamera;
            var target = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = camera.targetTexture;
            try
            {
                foreach (var (textures, lighting, tag) in new[] { (0, 0, "classic"), (1, 0, "enhanced"), (0, 1, "classic_rt"), (1, 1, "enhanced_rt") })
                {
                    var settings = saved.Copy();
                    settings.textures = textures;
                    settings.lighting = lighting;
                    HplGraphics.Apply(settings);
                    camera.targetTexture = target;
                    // Temporal accumulation (TAA / DLSS, RT denoisers) and texture streaming settle over frames.
                    for (int i = 0; i < ShotFrames; i++)
                    {
                        camera.Render();
                        yield return null;
                    }
                    var active = RenderTexture.active;
                    RenderTexture.active = target;
                    var texture = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
                    RenderTexture.active = active;
                    File.WriteAllBytes(prefix + "_" + tag + ".png", texture.EncodeToPNG());
                    Destroy(texture);
                    camera.targetTexture = previous;
                    result.AppendLine($"снимок {tag}: RT {HplRayTracing.Active}, отражения {HplRayTracing.Reflections}, GI {HplRayTracing.Gi}");
                }
            }
            finally
            {
                camera.targetTexture = previous;
                target.Release();
                Destroy(target);
                HplGraphics.Apply(saved);
            }
        }

        static string Section(string text, string title)
        {
            int start = text.IndexOf(title, StringComparison.Ordinal);
            if (start < 0) return "(нет раздела)";
            int end = text.IndexOf("\n[", start + title.Length, StringComparison.Ordinal);
            return (end < 0 ? text.Substring(start) : text.Substring(start, end - start)).TrimEnd();
        }
    }
}
#endif
