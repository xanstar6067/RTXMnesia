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
