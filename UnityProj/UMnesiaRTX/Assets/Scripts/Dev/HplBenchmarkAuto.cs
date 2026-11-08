#if RTXMNESIA_BENCHMARK_AUTO
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Test players only (define RTXMNESIA_BENCHMARK_AUTO, VideoMemoryPlayer): runs the benchmark from the title screen
    /// with the saved settings plus -benchmarkSet field=value,..., writes the results to -benchmarkAuto &lt;file&gt; and
    /// quits. Silent (rule of the project), no pre menu, preferences are never written.
    /// </summary>
    public sealed class HplBenchmarkAuto : MonoBehaviour
    {
        string _output;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Startup()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-benchmarkAuto");
            if (i < 0 || i + 1 >= args.Length) return;
            // Chain tests enter ordinary maps whose OnEnter calls AutoSave: isolate all test saves.
            Save.HplSaveHandler.FolderOverride = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[i + 1])), "Saves");
            typeof(HplPreMenu).GetProperty(nameof(HplPreMenu.ShownThisRun)).GetSetMethod(true).Invoke(null, new object[] { true });
            // As HplBatchAudio: sound logic and timings run, only the output is silent; the volume stays as saved.
            AudioListener.pause = true;
            var go = new GameObject("Benchmark auto run");
            DontDestroyOnLoad(go);
            go.AddComponent<HplBenchmarkAuto>()._output = args[i + 1];
        }

        IEnumerator Start()
        {
            var log = new StringBuilder();
            HplGameUi owner = null;
            for (float waited = 0; owner == null; waited += Time.unscaledDeltaTime)
            {
                if (waited > 120) { Write("FAIL title screen did not start"); Application.Quit(1); yield break; }
                owner = FindAnyObjectByType<HplMainMenu>()?.Ui;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(2);
            var settings = owner.Settings.Copy();
            settings.masterVolume = 0;
            string list = Arg("-benchmarkSet");
            if (!string.IsNullOrEmpty(list))
                foreach (var pair in list.Split(','))
                {
                    var kv = pair.Split('=');
                    var field = typeof(HplUserSettings).GetField(kv[0].Trim());
                    field.SetValue(settings, Convert.ChangeType(kv[1].Trim(), field.FieldType, CultureInfo.InvariantCulture));
                }
            settings.Validate();
            // Apply the initial test preset without changing the title owner's saved preset for the return.
            ApplySettings(owner.Map, settings);
            log.AppendLine("settings " + JsonUtility.ToJson(settings));
            log.AppendLine($"adapter {SystemInfo.graphicsDeviceName}, dedicated {HplVideoMemory.Dedicated / 1048576} MB");
            var census = new System.Collections.Generic.Dictionary<string, int>();
            yield return Title(log, "title before", census);
            // The title rendered at the saved (desktop) size first, and HDRP's shared buffers never shrink. A machine
            // whose desktop is the tested size would not have them: start the tour from that size (-benchmarkRtSize WxH).
            string rtSize = Arg("-benchmarkRtSize");
            if (rtSize != null)
            {
                var wh = rtSize.Split('x');
                UnityEngine.Rendering.RTHandles.ResetReferenceSize(int.Parse(wh[0]), int.Parse(wh[1]));
                log.AppendLine("RTHandles reference reset to " + rtSize);
            }
            // -benchmarkSpots <file>: the player stands at fixed places of one map, each place with each settings variant.
            string spotsFile = Arg("-benchmarkSpots");
            if (spotsFile != null)
            {
                yield return Spots(log, settings, File.ReadAllLines(spotsFile));
                Write(log.ToString());
                Application.Quit(0);
                yield break;
            }
            // -benchmarkChain: map changes of the game (HplMapChange, the scene replaced) round four maps of the tour.
            if (Arg("-benchmarkChain") != null)
            {
                settings.Apply(owner.Map);
                var first = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(HplBenchmarkRoute.All[0].ScenePath);
                while (!first.isDone) yield return null;
                for (int k = 0; k <= 8; k++)
                {
                    while (ScriptApi.HplMap.Current == null) yield return null;
                    ApplySettings(ScriptApi.HplMap.Current, settings);
                    float t = 0;
                    while (t < 10 || ScriptApi.HplMap.Current == null) { t += Time.unscaledDeltaTime; AudioListener.pause = true; yield return null; }
                    log.AppendLine($"  chain {k} {ScriptApi.HplMap.Current.MapName}: {Memory()}");
                    Write(log.ToString());
                    Shot($"chain {k}");
                    if (k == 8) break;
                    var next = HplBenchmarkRoute.All[(k + 1) % 4];
                    var from = ScriptApi.HplMap.Current;
                    ScriptApi.HplMapChange.Begin(from, next.ScenePath, ScriptApi.HplMapChange.FileToMapName(next.map), "", "");
                    while (ScriptApi.HplMap.Current == from || ScriptApi.HplMap.Current == null) yield return null;
                }
                Write(log.ToString());
                Application.Quit(0);
                yield break;
            }
            // -benchmarkReturn N: N tours, each ending back on the title, as a player would; what a tour leaves behind
            // shows there.
            int tours = int.TryParse(Arg("-benchmarkReturn"), out int n) ? Math.Max(1, n) : 0;
            for (int tour = 0; tour < Math.Max(1, tours); tour++)
            {
                if (!HplBenchmark.TryStart(owner, settings, out string error)) { Write("FAIL " + error); Application.Quit(1); yield break; }
                HplBenchmark test = null;
                while (test == null) { test = FindAnyObjectByType<HplBenchmark>(); yield return null; }
                var snapshot = new StringBuilder();
                int scene = -1, shot = -1;
                while (test != null && !test.ShowingResults)
                {
                    AudioListener.pause = true;
                    // The previous scene is unloaded (and unused assets too) when the index moves on.
                    if (test.SceneIndex != scene) { scene = test.SceneIndex; log.AppendLine($"  before scene {scene + 1}: {Memory()}"); }
                    if (test.IsPlaying && test.SceneTime > 12 && shot != scene) { shot = scene; Shot($"tour {tour + 1} scene {scene + 1}"); }
                    // The render targets of the last scene, while its camera still renders.
                    if (tour == 0 && snapshot.Length == 0 && test.IsPlaying && test.SceneIndex == HplBenchmarkRoute.All.Length - 1 && test.SceneTime > 15) Snapshot(snapshot);
                    yield return null;
                }
                if (test == null) { Write("FAIL benchmark ended without results"); Application.Quit(1); yield break; }
                var stats = test.Stats;
                var total = stats.Calculate();
                log.AppendLine($"tour {tour + 1}: fps {total.AverageFps:F1}, 1% {total.Low1Fps:F1}, median {total.MedianMs:F1} ms, stutters {total.Stutters}, peak local {total.PeakLocalMb:F0} MB, shared {total.PeakSharedMb:F0} MB, budget {stats.LocalBudgetMb:F0} MB");
                for (int s = 0; s < HplBenchmarkRoute.All.Length; s++)
                {
                    var m = stats.Calculate(s);
                    log.AppendLine($"  {s + 1} {HplBenchmarkRoute.All[s].map}: fps {m.AverageFps:F1}, 1% {m.Low1Fps:F1}, P99 {m.P99Ms:F1} ms, stutters {m.Stutters}, peak local {m.PeakLocalMb:F0} MB, shared {m.PeakSharedMb:F0} MB");
                }
                log.AppendLine($"textures: current {Texture.currentTextureMemory / 1048576} MB, nonstreaming {Texture.nonStreamingTextureMemory / 1048576} MB");
                log.Append(snapshot);
                yield return new WaitForSecondsRealtime(2);
                log.AppendLine("results screen: " + Memory());
                if (tours == 0) break;
                test.ChooseResult(false);
                while (test != null) yield return null;
                yield return new WaitForSecondsRealtime(3);
                yield return Title(log, $"title after tour {tour + 1}", census);
                yield return new WaitForSecondsRealtime(20);
                log.AppendLine("  30 s later: " + Memory());
                Write(log.ToString());
            }
            Write(log.ToString());
            Application.Quit(0);
        }

        /// <summary>
        /// The spots file: "map ch01/00_rainy_hall", "spot name feetX feetY feetZ dirX dirY dirZ" (HPL), "variant name
        /// field=value,..." (HplUserSettings over the test settings). Every spot with every variant: settle (8 s at a new
        /// spot, 4 s after a settings change), then 6 s measured: frame rate, 1% low, FrameTimingManager CPU / GPU, the
        /// GPU time of HDRP passes if the player records them, ray traced lights, video memory.
        /// </summary>
        IEnumerator Spots(StringBuilder log, HplUserSettings baseSettings, string[] lines)
        {
            string mapName = null;
            var spots = new System.Collections.Generic.List<(string name, Vector3 feet, Vector3 dir)>();
            var variants = new System.Collections.Generic.List<(string name, string fields)>();
            foreach (string raw in lines)
            {
                var p = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (p.Length == 0 || p[0].StartsWith("#")) continue;
                float F(int i) => float.Parse(p[i], CultureInfo.InvariantCulture);
                if (p[0] == "map") mapName = p[1];
                else if (p[0] == "spot") spots.Add((p[1], new Vector3(F(2), F(3), F(4)), new Vector3(F(5), F(6), F(7))));
                else if (p[0] == "variant") variants.Add((p[1], p.Length > 2 ? p[2] : ""));
            }
            var load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Assets/AmnesiaImported/Levels/maps/main/" + mapName + ".unity");
            while (!load.isDone) yield return null;
            while (ScriptApi.HplMap.Current == null) yield return null;
            var map = ScriptApi.HplMap.Current;
            yield return new WaitForSecondsRealtime(3);
            var player = map.player;
            player.Flashback.Disabled = true;
            player.readDevices = false;
            log.AppendLine($"spots on {mapName}: {Memory()}");

            var names = new System.Collections.Generic.List<string>();
            var ids = Type.GetType("UnityEngine.Rendering.HighDefinition.HDProfileId, Unity.RenderPipelines.HighDefinition.Runtime");
            if (ids != null) names.AddRange(Enum.GetNames(ids));
            var recorders = new System.Collections.Generic.List<UnityEngine.Profiling.Recorder>();
            foreach (string n in names) { var r = UnityEngine.Profiling.Recorder.Get(n); r.enabled = true; recorders.Add(r); }
            var timings = new FrameTiming[1];
            UnityEngine.Rendering.HighDefinition.RayTracingSettings cullSettings = null;
            foreach (var spot in spots)
                for (int v = 0; v < variants.Count; v++)
                {
                    var s = baseSettings.Copy();
                    string flashback = null; bool emotion = false; float rtCull = 0f;
                    foreach (var pair in variants[v].fields.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = pair.Split('=');
                        // "@flashback=<file>": the flashback plays while measured; "@emotion=1": an emotion flash.
                        if (kv[0] == "@flashback") { flashback = kv[1]; continue; }
                        if (kv[0] == "@emotion") { emotion = true; continue; }
                        // "@rtcull=<m>": the ray tracing structure keeps only objects within a sphere of that radius
                        // (HDRP RayTracingSettings, culling mode Sphere); -1: HDRP's default (extended frustum).
                        if (kv[0] == "@rtcull") { rtCull = float.Parse(kv[1], CultureInfo.InvariantCulture); continue; }
                        var field = typeof(HplUserSettings).GetField(kv[0].Trim());
                        field.SetValue(s, Convert.ChangeType(kv[1].Trim(), field.FieldType, CultureInfo.InvariantCulture));
                    }
                    s.Validate();
                    ApplySettings(map, s);
                    if (cullSettings == null)
                    {
                        var go = new GameObject("Benchmark RT culling");
                        DontDestroyOnLoad(go);
                        var volume = go.AddComponent<UnityEngine.Rendering.Volume>();
                        volume.isGlobal = true; volume.priority = 1000;
                        volume.profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                        cullSettings = volume.profile.Add<UnityEngine.Rendering.HighDefinition.RayTracingSettings>();
                    }
                    cullSettings.cullingMode.Override(rtCull > 0 ? UnityEngine.Rendering.HighDefinition.RTASCullingMode.Sphere
                                                                 : UnityEngine.Rendering.HighDefinition.RTASCullingMode.ExtendedFrustum);
                    cullSettings.cullingDistance.Override(rtCull > 0 ? rtCull : 1000f);
                    // 0: the game's own culling (HplRayTracing.StructureRadius); below 0: HDRP's default.
                    cullSettings.active = rtCull != 0;
                    map.effects.Fade.FadeIn(0f);
                    player.active = true;
                    Vector3 feet = HplSpace.Position(spot.feet), dir = HplSpace.Position(spot.feet + spot.dir) - feet;
                    player.PlaceAt(feet, Mathf.Atan2(dir.x, dir.z));
                    yield return new WaitForSecondsRealtime(0.3f);
                    player.AddLook(0f, -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) - player.Pitch);
                    for (float t = 0, settle = v == 0 ? 8 : 4; t < settle; t += Time.unscaledDeltaTime) { AudioListener.pause = true; yield return null; }
                    if (flashback != null) { player.Flashback.Disabled = false; player.Flashback.Start(flashback, ""); }
                    if (emotion) map.effects.EmotionFlash.Start("Benchmark", "");
                    if (flashback != null || emotion)
                        for (float t = 0; t < 2.5f; t += Time.unscaledDeltaTime) { AudioListener.pause = true; yield return null; }

                    var gpuPass = new double[names.Count];
                    var frameMs = new System.Collections.Generic.List<float>();
                    double gpu = 0, main = 0, render = 0; int timed = 0;
                    for (float t = 0; t < 6; t += Time.unscaledDeltaTime)
                    {
                        AudioListener.pause = true;
                        yield return null;
                        frameMs.Add(Time.unscaledDeltaTime * 1000f);
                        for (int k = 0; k < recorders.Count; k++) if (recorders[k].isValid) gpuPass[k] += recorders[k].gpuElapsedNanoseconds / 1e6;
                        FrameTimingManager.CaptureFrameTimings();
                        if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0)
                        { gpu += timings[0].gpuFrameTime; main += timings[0].cpuMainThreadFrameTime; render += timings[0].cpuRenderThreadFrameTime; timed++; }
                    }
                    float total = 0; foreach (float ms in frameMs) total += ms;
                    frameMs.Sort();
                    float low = frameMs[Math.Max(0, (int)(frameMs.Count * 0.99f) - 1)];
                    string state = flashback != null ? $" flashback active {player.Flashback.Active}" : emotion ? $" emotion active {map.effects.EmotionFlash.Active}" : "";
                    if (flashback != null) { player.Flashback.Reset(); player.Flashback.Disabled = true; }
                    var cam = map.PlayerCamera;
                    log.AppendLine($"  {spot.name} {variants[v].name}: fps {frameMs.Count / (total / 1000f):F1}, 1% low {1000f / low:F1}, " +
                                   (timed > 0 ? $"gpu {gpu / timed:F2} ms, cpu main {main / timed:F2}, render thread {render / timed:F2}" : "no frame timing") +
                                   $"; render {cam.scaledPixelWidth}x{cam.scaledPixelHeight} of {Screen.width}x{Screen.height}, traced {HplRayTracing.TracedLights} of {HplRayTracing.ShadowedLights}; {Memory()}{state}");
                    var order = new System.Collections.Generic.List<int>();
                    for (int k = 0; k < names.Count; k++) if (gpuPass[k] / frameMs.Count >= 0.3) order.Add(k);
                    order.Sort((a, b) => gpuPass[b].CompareTo(gpuPass[a]));
                    if (order.Count > 0)
                        log.AppendLine("      passes: " + string.Join(", ", order.ConvertAll(k => $"{names[k]} {gpuPass[k] / frameMs.Count:F2}")));
                    Write(log.ToString());
                    Shot($"{spot.name}_{variants[v].name}");
                    yield return null; yield return null;
                }
            foreach (var r in recorders) r.enabled = false;
        }

        /// <summary>Test settings stay in memory; loading a map must not restore the saved graphics preset.</summary>
        static void ApplySettings(ScriptApi.HplMap map, HplUserSettings settings)
        {
            if (map.GameUi != null)
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(settings), map.GameUi.Settings);
            settings.Apply(map);
        }

        /// <summary>Title frame rate, video memory and live objects; the second call lists what grew since the first.</summary>
        static IEnumerator Title(StringBuilder log, string label, System.Collections.Generic.Dictionary<string, int> census)
        {
            Shot(label);
            int frames = 0;
            float time = 0;
            while (time < 5) { time += Time.unscaledDeltaTime; frames++; yield return null; }
            log.AppendLine($"{label}: fps {frames / time:F1}, {Memory()}");
            log.AppendLine($"    textures {Texture.currentTextureMemory / 1048576} MB (desired {Texture.desiredTextureMemory / 1048576}, nonstreaming {Texture.nonStreamingTextureMemory / 1048576})");
            var now = new System.Collections.Generic.Dictionary<string, int>();
            void Count(string key) { now.TryGetValue(key, out int n); now[key] = n + 1; }
            foreach (var o in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                Count("type " + o.GetType().Name);
                if (o is GameObject go && go.transform.parent == null) Count("root " + go.name + (go.scene.IsValid() ? "" : " [no scene]"));
                else if (o is Texture t) Count($"tex {t.GetType().Name} {t.width}x{t.height}");
                else if (o is Material m) Count("mat " + m.name);
            }
            bool first = census.Count == 0;
            if (first) { foreach (var kv in now) census[kv.Key] = kv.Value; }
            var lines = new System.Collections.Generic.List<(int, string)>();
            foreach (var kv in now)
            {
                census.TryGetValue(kv.Key, out int was);
                if (first ? kv.Key.StartsWith("type ") : kv.Value > was) lines.Add((kv.Value - (first ? 0 : was), $"{kv.Key}: {(first ? "" : was + " -> ")}{kv.Value}"));
            }
            if (!first)
                foreach (var kv in census)
                    if (!now.ContainsKey(kv.Key) && kv.Key.StartsWith("type ")) lines.Add((-kv.Value, $"{kv.Key}: {kv.Value} -> 0"));
            lines.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            for (int i = 0; i < Math.Min(first ? 25 : 80, lines.Count); i++) log.AppendLine("    " + lines[i].Item2);
        }

        /// <summary>-benchmarkShots &lt;folder&gt;: a screenshot (PNG, next frame) named after the moment.</summary>
        static void Shot(string label)
        {
            string folder = Arg("-benchmarkShots");
            if (folder != null) ScreenCapture.CaptureScreenshot(Path.Combine(folder, label.Replace(' ', '_') + ".png"));
        }

        /// <summary>Created render targets (HDRP buffers, atlases), counted by their format and size.</summary>
        static void Snapshot(StringBuilder log)
        {
            long total = 0;
            var items = new System.Collections.Generic.List<(long, string)>();
            foreach (var rt in Resources.FindObjectsOfTypeAll<RenderTexture>())
            {
                if (!rt.IsCreated()) continue;
                long bytes = (long)(UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetBlockSize(rt.graphicsFormat)
                    * (double)rt.width * rt.height * Math.Max(1, rt.volumeDepth) * (rt.useMipMap ? 4.0 / 3 : 1));
                if (rt.dimension == UnityEngine.Rendering.TextureDimension.Cube) bytes *= 6;
                total += bytes;
                items.Add((bytes, $"{rt.name} {rt.width}x{rt.height}x{rt.volumeDepth} {rt.graphicsFormat}"));
            }
            items.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            log.AppendLine($"render textures: {total / 1048576} MB in {items.Count}");
            for (int i = 0; i < Math.Min(20, items.Count); i++) log.AppendLine($"    {items[i].Item1 / 1048576.0:F1} MB {items[i].Item2}");
        }

        static string Memory()
        {
            if (!HplVideoMemory.TryQuery(out var info)) return "no DXGI";
            return $"local {info.localUsage / 1048576} MB, shared {info.sharedUsage / 1048576} MB, budget {info.localBudget / 1048576} MB";
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        void Write(string text)
        {
            Debug.Log("[BenchmarkAuto]\n" + text);
            File.WriteAllText(_output, text);
        }
    }
}
#endif
