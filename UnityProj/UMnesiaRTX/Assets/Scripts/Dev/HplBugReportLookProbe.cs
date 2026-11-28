#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
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
    /// classic / enhanced textures, without and with ray tracing (the saved RT levels), as PNG files there; with also
    /// -lookSeries <n>: n classic frames from one pose a quarter second of game time apart (effects that come and go;
    /// -lookSeriesSaved: in the saved settings instead). With
    /// -lookRays "u,v;u,v": the visible hits along rays through those points of the frame (0..1, from the bottom left).
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
                yield return Calls(HplMap.Current, folder, result);
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
                int rays = Array.IndexOf(args, "-lookRays");
                camera.aspect = (float)ShotWidth / ShotHeight;
                if (rays >= 0 && rays + 1 < args.Length)
                    foreach (string uv in args[rays + 1].Split(';').Where(t => t.Length > 0))
                    {
                        // A point of the frame (0..1 from the bottom left, as the shots' pixels / size with y flipped).
                        var parts = uv.Split(',');
                        var point = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
                        result.AppendLine($"-- луч через точку кадра {point.x:0.000} {point.y:0.000}:");
                        foreach (var hit in HplVisibleHit.Cast(camera.ViewportPointToRay(point), 100, map.gameObject.scene, camera.cullingMask, map.player.Hands?.Root, null).Take(6))
                            result.AppendLine($"{hit.distance:0.000} м {hit.renderer.name} ({hit.renderer.transform.parent?.name}) слот {hit.slot} точка HPL {HplSpace.Position(hit.point).ToString("0.000")} {(hit.Transparent ? "прозрачное" : "")} {(hit.backFace ? "обратная сторона" : "")}");
                    }
                camera.ResetAspect();
                report.Dispose();
                int shots = Array.IndexOf(args, "-lookShots");
                if (shots >= 0 && shots + 1 < args.Length)
                    yield return Shots(map, Path.Combine(args[shots + 1], Path.GetFileName(folder)), result);
                int series = Array.IndexOf(args, "-lookSeries");
                if (shots >= 0 && shots + 1 < args.Length && Array.IndexOf(args, "-lookDecals") >= 0)
                    yield return Decals(map, Path.Combine(args[shots + 1], Path.GetFileName(folder)), result);
                if (shots >= 0 && shots + 1 < args.Length && series >= 0 && series + 1 < args.Length)
                    yield return Series(map, Path.Combine(args[shots + 1], Path.GetFileName(folder)), int.Parse(args[series + 1]), Array.IndexOf(args, "-lookSeriesSaved") >= 0, result);
            }
        }

        const int ShotFrames = 48;
        // -lookSize WxH: the shots' size (1600x900; the report's output size, e.g. 2560x1440, for what DLSS draws at its render size).
        static readonly int ShotWidth = LookSize(0, 1600), ShotHeight = LookSize(1, 900);

        static int LookSize(int index, int fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-lookSize");
            return at >= 0 && at + 1 < args.Length ? int.Parse(args[at + 1].Split('x')[index], CultureInfo.InvariantCulture) : fallback;
        }

        static IEnumerator Shots(HplMap map, string prefix, StringBuilder result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(prefix));
            var saved = HplUserSettings.ReadSaved();
            var camera = map.PlayerCamera;
            var target = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = camera.targetTexture;
            try
            {
                // -lookLightingSets: the lighting set is given too (classic / enhanced, "light" in the tag), RT at
                // fixed levels (AO high, reflections medium, GI high); else the saved set and RT levels.
                bool sets = Array.IndexOf(Environment.GetCommandLineArgs(), "-lookLightingSets") >= 0;
                var variants = sets
                    ? new[] { (0, 0, 0, "classic"), (0, 0, 1, "light"), (1, 0, 1, "light_enhanced"), (0, 1, 1, "light_rt"), (1, 1, 1, "light_enhanced_rt") }
                    : new[] { (0, 0, -1, "classic"), (1, 0, -1, "enhanced"), (0, 1, -1, "classic_rt"), (1, 1, -1, "enhanced_rt") };
                foreach (var (textures, lighting, lightingSet, tag) in variants)
                {
                    var settings = saved.Copy();
                    settings.textures = textures;
                    settings.lighting = lighting;
                    if (lightingSet >= 0)
                    {
                        settings.lightingSet = lightingSet;
                        settings.rtAo = 3;
                        settings.rtReflections = 2;
                        settings.rtGi = 3;
                        settings.rtPathTracing = 0;
                    }
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
                    result.AppendLine($"снимок {tag}: RT {HplRayTracing.Active}, отражения {HplRayTracing.Reflections}, GI {HplRayTracing.Gi}, набор света {settings.lightingSet}");
                }
            }
            finally
            {
                camera.ResetAspect();
                camera.targetTexture = previous;
                target.Release();
                Destroy(target);
                HplGraphics.Apply(saved);
            }
        }

        /// <summary>A series of frames from one held pose, a quarter second of game time apart: effects that come and go. Classic
        /// textures without RT, or with <paramref name="savedSettings"/> the saved settings (-lookSeriesSaved).</summary>
        static IEnumerator Series(HplMap map, string prefix, int count, bool savedSettings, StringBuilder result)
        {
            var args = Environment.GetCommandLineArgs();
            int stepAt = Array.IndexOf(args, "-lookSeriesStep");
            double step = stepAt >= 0 && stepAt + 1 < args.Length ? double.Parse(args[stepAt + 1], CultureInfo.InvariantCulture) : 0.25;
            var saved = HplUserSettings.ReadSaved();
            var settings = saved.Copy();
            if (!savedSettings)
            {
                settings.textures = 0;
                settings.lighting = 0;
            }
            LookSet(settings);
            HplGraphics.Apply(settings);
            result.AppendLine($"серия: освещение {settings.lighting}, набор {settings.lightingSet}, DLSS {(HplDlssMode)settings.dlssMode}, RT {HplRayTracing.Active}");
            // -lookSeriesDrift <m/s>: the held pose slides sideways (the camera's right) at this speed, as a walking player.
            int driftAt = Array.IndexOf(args, "-lookSeriesDrift");
            float drift = driftAt >= 0 && driftAt + 1 < args.Length ? float.Parse(args[driftAt + 1], CultureInfo.InvariantCulture) : 0f;
            var camera = map.PlayerCamera;
            var target = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                // The pose is held: the head sway (and the insanity's field of view and aspect sway) would move every edge.
                camera.transform.GetPositionAndRotation(out var position, out var rotation);
                float fov = camera.fieldOfView;
                double startTime = Time.timeAsDouble;
                // -lookSeriesFree: the player's own sway (the idle head bob) is kept, as in play.
                bool hold = Array.IndexOf(args, "-lookSeriesFree") < 0;
                var off = LookOff();
                result.AppendLine($"выключено источников: {off.Length}" + (off.Length > 0 ? " (" + string.Join(", ", off.Select(l => l.name)) + ")" : ""));
                void Render()
                {
                    foreach (var light in off) light.intensity = 0f;
                    if (hold)
                    {
                        camera.transform.SetPositionAndRotation(position + rotation * Vector3.right * (float)(drift * (Time.timeAsDouble - startTime)), rotation);
                        camera.fieldOfView = fov;
                    }
                    camera.aspect = (float)ShotWidth / ShotHeight;
                    camera.Render();
                }
                for (int i = 0; i < ShotFrames; i++) { Render(); yield return null; }
                for (int n = 0; n < count; n++)
                {
                    for (double end = Time.timeAsDouble + step; Time.timeAsDouble < end;) { Render(); yield return null; }
                    var active = RenderTexture.active;
                    RenderTexture.active = target;
                    var texture = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
                    RenderTexture.active = active;
                    File.WriteAllBytes($"{prefix}_series{(savedSettings ? "_saved" : "")}_{n:00}.png", texture.EncodeToPNG());
                    Destroy(texture);
                    result.AppendLine($"кадр {n:00}: t {Time.timeAsDouble:0.00} с");
                }
            }
            finally
            {
                camera.ResetAspect();
                camera.targetTexture = previous;
                target.Release();
                Destroy(target);
                HplGraphics.Apply(saved);
            }
        }

        /// <summary>
        /// Classic frames with and without the Mul decals (HplMulDecal, HplMultiply below the transparent queue), from the pose and
        /// from poses 2 and 6 cm lower and a degree down: their ratio is the decals' factor, where they are covered or
        /// drawn over something it shows (-lookDecals).
        /// </summary>
        static IEnumerator Decals(HplMap map, string prefix, StringBuilder result)
        {
            var saved = HplUserSettings.ReadSaved();
            var settings = saved.Copy();
            settings.textures = 0;
            settings.lighting = 0;
            LookSet(settings);
            HplGraphics.Apply(settings);
            var camera = map.PlayerCamera;
            var decals = FindObjectsByType<Renderer>().Where(r => r.enabled && r.sharedMaterials.Any(m => m != null && m.shader != null
                && (m.shader.name == "RTXMnesia/HplMulDecal" || m.shader.name == "RTXMnesia/HplMultiply" && m.renderQueue < (int)UnityEngine.Rendering.RenderQueue.Transparent))).ToArray();
            result.AppendLine($"декалей Mul: {decals.Length}");
            var target = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = camera.targetTexture;
            camera.transform.GetPositionAndRotation(out var position, out var rotation);
            float fov = camera.fieldOfView;
            try
            {
                camera.targetTexture = target;
                foreach (var (drop, pitch, tag) in new[] { (0f, 0f, "a"), (0.02f, 0f, "b"), (0.06f, 1f, "c") })
                    foreach (bool shown in new[] { true, false })
                    {
                        foreach (var r in decals) r.enabled = shown;
                        for (int i = 0; i < ShotFrames; i++)
                        {
                            camera.transform.SetPositionAndRotation(position + Vector3.down * drop, rotation * Quaternion.Euler(pitch, 0f, 0f));
                            camera.fieldOfView = fov;
                            camera.aspect = (float)ShotWidth / ShotHeight;
                            camera.Render();
                            yield return null;
                        }
                        var active = RenderTexture.active;
                        RenderTexture.active = target;
                        var texture = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
                        texture.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
                        RenderTexture.active = active;
                        File.WriteAllBytes($"{prefix}_decals_{tag}_{(shown ? "on" : "off")}.png", texture.EncodeToPNG());
                        Destroy(texture);
                    }
            }
            finally
            {
                foreach (var r in decals) r.enabled = true;
                camera.ResetAspect();
                camera.targetTexture = previous;
                target.Release();
                Destroy(target);
                HplGraphics.Apply(saved);
            }
        }

        /// <summary>-lookSet field=value,...: <see cref="HplUserSettings"/> fields over the settings of the series and the decal pairs
        /// (the report's [Графика]: lighting, DLSS, RT levels).</summary>
        static void LookSet(HplUserSettings settings)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-lookSet");
            if (at < 0 || at + 1 >= args.Length) return;
            foreach (var pair in args[at + 1].Split(',').Where(p => p.Contains('=')))
            {
                var kv = pair.Split('=');
                var field = typeof(HplUserSettings).GetField(kv[0].Trim()) ?? throw new ArgumentException("Unknown setting " + kv[0]);
                field.SetValue(settings, Convert.ChangeType(kv[1].Trim(), field.FieldType, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// -lookCalls "Func(a,b);@wait 6;@effects;@freeze", right after the save is loaded: functions of the level script
        /// (and of HplScript) by name, arguments as the parameters' types (string without quotes); "@wait s" - game
        /// seconds; "@effects" - the screen effects reset (fades, flash); "@freeze" - game time stopped (timeScale 0) for
        /// the shots, so a scripted fade stays where it is. A save in the middle of a scripted scene (the orb flashback of
        /// 03_archives) goes on after loading; this puts the scene in the state to look at.
        /// </summary>
        static IEnumerator Calls(HplMap map, string folder, StringBuilder result)
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-lookCalls");
            if (at < 0 || at + 1 >= args.Length) yield break;
            Time.timeScale = 1f;
            s_aim = null;
            foreach (string raw in args[at + 1].Split(';').Select(c => c.Trim()).Where(c => c.Length > 0))
            {
                result.AppendLine("вызов: " + raw);
                if (raw.StartsWith("@wait", StringComparison.Ordinal))
                {
                    float seconds = float.Parse(raw.Substring(5).Trim(), CultureInfo.InvariantCulture);
                    for (double end = Time.timeAsDouble + seconds; Time.timeAsDouble < end;) yield return null;
                    continue;
                }
                if (raw == "@effects") { map.effects.ResetAll(); continue; }
                if (raw == "@freeze") { Time.timeScale = 0f; continue; }
                if (raw.StartsWith("@aim ", StringComparison.Ordinal))
                {
                    var v = raw.Substring(5).Trim().Split(' ');
                    s_aim = HplSpace.Position(new Vector3(float.Parse(v[0], CultureInfo.InvariantCulture), float.Parse(v[1], CultureInfo.InvariantCulture), float.Parse(v[2], CultureInfo.InvariantCulture)));
                    continue;
                }
                if (raw.StartsWith("@shot ", StringComparison.Ordinal))
                {
                    int shots = Array.IndexOf(args, "-lookShots");
                    if (shots < 0 || shots + 1 >= args.Length) throw new ArgumentException("@shot needs -lookShots");
                    yield return CallShot(map, folder, Path.Combine(args[shots + 1], Path.GetFileName(folder)) + "_call_" + raw.Substring(6).Trim() + ".png", result);
                    continue;
                }
                if (raw.StartsWith("@body ", StringComparison.Ordinal))
                {
                    // Saves of builds before 2026-11-27 20:20 kept the bodies of inactive props at the origin.
                    var parts = raw.Substring(6).Trim().Split(' ');
                    var prop = map.GetEntityByName(parts[0]) as HplProp ?? throw new ArgumentException("No prop " + raw);
                    var hpl = new Vector3(float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture), float.Parse(parts[3], CultureInfo.InvariantCulture));
                    prop.PhysicsBodies[0].SetPose(HplSpace.Position(hpl), Quaternion.identity);
                    continue;
                }
                if (raw.StartsWith("@show ", StringComparison.Ordinal))
                {
                    var entity = map.GetEntityByName(raw.Substring(6).Trim());
                    result.AppendLine(entity == null ? "  нет сущности" : $"  активна {entity.Active}, объект {entity.GameObject.activeInHierarchy}");
                    if (entity != null)
                        foreach (var r in entity.GameObject.GetComponentsInChildren<Renderer>(true))
                            result.AppendLine($"  {r.name}: {(r.enabled && r.gameObject.activeInHierarchy ? "виден" : "скрыт")}, {string.Join(", ", r.sharedMaterials.Select(m => m == null ? "null" : m.name + " / " + m.shader.name))}, центр HPL {HplSpace.Position(r.bounds.center).ToString("0.000")}");
                    continue;
                }
                int open = raw.IndexOf('(');
                string name = raw.Substring(0, open).Trim();
                string[] values = raw.Substring(open + 1, raw.LastIndexOf(')') - open - 1).Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToArray();
                var method = map.Script.GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                    .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == values.Length)
                    ?? throw new ArgumentException("No script function " + raw);
                var parameters = method.GetParameters();
                method.Invoke(map.Script, values.Select((v, i) => Convert.ChangeType(v, parameters[i].ParameterType, CultureInfo.InvariantCulture)).ToArray());
            }
        }

        /// <summary>"@shot tag" of -lookCalls: one frame in the saved settings with -lookSet over them, game time stopped
        /// while the temporal accumulation settles (the scripted fades stay where they are).</summary>
        static IEnumerator CallShot(HplMap map, string folder, string file, StringBuilder result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var saved = HplUserSettings.ReadSaved();
            var settings = saved.Copy();
            LookSet(settings);
            HplGraphics.Apply(settings);
            float scale = Time.timeScale;
            Time.timeScale = 0f;
            // A copy of the player's camera at the report's pose (or aimed with "@aim"): the player of a scripted scene
            // moves and turns after loading, and its camera follows it.
            var source = map.PlayerCamera;
            string report = File.ReadAllText(Path.Combine(folder, "report.txt"));
            Vector3? position = ReportVector(report, "Камера Unity:"), direction = ReportVector(report, "Направление Unity:");
            var go = new GameObject("Look probe camera") { hideFlags = HideFlags.DontSave };
            var camera = go.AddComponent<Camera>();
            var data = go.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            if (source.TryGetComponent(out UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData sourceData)) sourceData.CopyTo(data);
            camera.CopyFrom(source);
            camera.enabled = false;
            var target = new RenderTexture(ShotWidth, ShotHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            try
            {
                var p = position ?? source.transform.position;
                var d = s_aim is { } aim ? aim - p : direction ?? source.transform.forward;
                camera.transform.SetPositionAndRotation(p, Quaternion.LookRotation(d));
                camera.aspect = (float)ShotWidth / ShotHeight;
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
                File.WriteAllBytes(file, texture.EncodeToPNG());
                Destroy(texture);
                result.AppendLine($"  камера HPL {HplSpace.Position(camera.transform.position).ToString("0.000")}, вперёд {camera.transform.forward.ToString("0.000")}, FOV {camera.fieldOfView:0.0}, игрок HPL {HplSpace.Position(source.transform.position).ToString("0.000")}");
                result.AppendLine($"  снимок {Path.GetFileName(file)}: освещение {settings.lighting}, набор {settings.lightingSet}, текстуры {settings.textures}, RT {HplRayTracing.Active}");
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                Destroy(target);
                Destroy(go);
                Time.timeScale = scale;
                HplGraphics.Apply(saved);
            }
        }

        /// <summary>"@aim x y z" of -lookCalls: the HPL point the "@shot" camera looks at (else the report's direction).</summary>
        static Vector3? s_aim;

        /// <summary>A "key: x y z" line of report.txt.</summary>
        static Vector3? ReportVector(string report, string key)
        {
            foreach (var line in report.Split('\n'))
            {
                if (!line.StartsWith(key, StringComparison.Ordinal)) continue;
                var v = line.Substring(key.Length).Trim().Split(' ');
                return new Vector3(float.Parse(v[0], CultureInfo.InvariantCulture), float.Parse(v[1], CultureInfo.InvariantCulture), float.Parse(v[2], CultureInfo.InvariantCulture));
            }
            return null;
        }

        /// <summary>-lookOff "name;name": lights held at intensity 0 in the series (game object names; "lantern": the
        /// hand-held lights) - to find which source gives a highlight.</summary>
        static Light[] LookOff()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-lookOff");
            if (at < 0 || at + 1 >= args.Length) return Array.Empty<Light>();
            var names = args[at + 1].Split(';').Where(n => n.Length > 0).ToArray();
            return FindObjectsByType<Light>().Where(l => names.Contains(l.name)
                || names.Contains("lantern") && l.GetComponentInParent<World.AmnesiaLight>() is { handHeld: true }).ToArray();
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
