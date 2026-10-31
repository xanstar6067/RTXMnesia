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
            log.AppendLine("settings " + JsonUtility.ToJson(settings));
            log.AppendLine($"adapter {SystemInfo.graphicsDeviceName}, dedicated {HplVideoMemory.Dedicated / 1048576} MB");
            if (HplVideoMemory.TryQuery(out var before)) log.AppendLine($"title: local {before.localUsage / 1048576} MB, budget {before.localBudget / 1048576} MB");
            // The title rendered at the saved (desktop) size first, and HDRP's shared buffers never shrink. A machine
            // whose desktop is the tested size would not have them: start the tour from that size (-benchmarkRtSize WxH).
            string rtSize = Arg("-benchmarkRtSize");
            if (rtSize != null)
            {
                var wh = rtSize.Split('x');
                UnityEngine.Rendering.RTHandles.ResetReferenceSize(int.Parse(wh[0]), int.Parse(wh[1]));
                log.AppendLine("RTHandles reference reset to " + rtSize);
            }
            if (!HplBenchmark.TryStart(owner, settings, out string error)) { Write("FAIL " + error); Application.Quit(1); yield break; }
            HplBenchmark test = null;
            while (test == null) { test = FindAnyObjectByType<HplBenchmark>(); yield return null; }
            var snapshot = new StringBuilder();
            while (test != null && !test.ShowingResults)
            {
                AudioListener.pause = true;
                // The render targets of the last scene, while its camera still renders.
                if (snapshot.Length == 0 && test.IsPlaying && test.SceneIndex == HplBenchmarkRoute.All.Length - 1 && test.SceneTime > 15) Snapshot(snapshot);
                yield return null;
            }
            if (test == null) { Write("FAIL benchmark ended without results"); Application.Quit(1); yield break; }
            var stats = test.Stats;
            var total = stats.Calculate();
            log.AppendLine($"total: fps {total.AverageFps:F1}, 1% {total.Low1Fps:F1}, median {total.MedianMs:F1} ms, stutters {total.Stutters}, peak local {total.PeakLocalMb:F0} MB, shared {total.PeakSharedMb:F0} MB, budget {stats.LocalBudgetMb:F0} MB");
            for (int s = 0; s < HplBenchmarkRoute.All.Length; s++)
            {
                var m = stats.Calculate(s);
                log.AppendLine($"  {s + 1} {HplBenchmarkRoute.All[s].map}: fps {m.AverageFps:F1}, 1% {m.Low1Fps:F1}, P99 {m.P99Ms:F1} ms, stutters {m.Stutters}, peak local {m.PeakLocalMb:F0} MB, shared {m.PeakSharedMb:F0} MB");
            }
            log.AppendLine($"textures: current {Texture.currentTextureMemory / 1048576} MB, nonstreaming {Texture.nonStreamingTextureMemory / 1048576} MB");
            log.Append(snapshot);
            Write(log.ToString());
            Application.Quit(0);
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
