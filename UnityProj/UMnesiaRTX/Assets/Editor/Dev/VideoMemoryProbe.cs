using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using RTXMnesia.World;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch, GPU: the real benchmark with given settings, sampling the process's video memory (DXGI) and Unity's own
    /// figures per scene, between scenes and after the return (leaks). Silent, never builds a player.
    /// -vramSet field=value,...  HplUserSettings fields for the test draft; -vramTag name (report file);
    /// -vramSize WxH (camera target, 1920x1080); -vramFull: the whole route in real time instead of seeking.
    /// Report: Logs/VramProbe/&lt;tag&gt;.txt.
    /// </summary>
    [InitializeOnLoad]
    public static class VideoMemoryProbe
    {
        const string Pending = "RTXMnesia.VideoMemoryProbe";
        static int s_stage;
        static double s_start, s_nextSample, s_breakdownScene = -1;
        static HplGameUi s_owner;
        static RenderTexture s_target;
        static readonly StringBuilder Report = new();
        static readonly Dictionary<string, Phase> Phases = new();
        static readonly List<string> Order = new();
        static ProfilerRecorder[] s_recorders;
        static readonly string[] RecorderNames =
        {
            "Gfx Used Memory", "Gfx Reserved Memory", "Texture Memory", "Mesh Memory", "Render Textures Bytes", "Used Buffers Bytes"
        };

        sealed class Phase
        {
            public double localPeak, sharedPeak, localLast, budget, framesSeconds;
            public int frames;
            public long texCurrent, texDesired, texTarget, texNonStreaming, texTotal;
            public long[] recorders = new long[RecorderNames.Length];
        }

        static VideoMemoryProbe()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    EditorApplication.isPaused = false;
                    s_start = EditorApplication.timeSinceStartup; s_stage = 0; s_breakdownScene = -1;
                    Report.Clear(); Phases.Clear(); Order.Clear();
                    s_recorders = RecorderNames.Select(n => ProfilerRecorder.StartNew(ProfilerCategory.Memory, n)).ToArray();
                    EditorApplication.update += Update;
                }
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.update -= Update;
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Pending + ".exit", 1));
                }
            };
        }

        public static void RunBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Video memory probe is batch-only");
            AudioListener.volume = 0;
            EditorSceneManager.OpenScene(HplBenchmarkRoute.All[1].ScenePath);
            var map = UnityEngine.Object.FindAnyObjectByType<HplMap>();
            map.runScript = false; map.disableAutoSaves = true; map.disableEnemyAI = true; map.disableInsanity = true;
            SessionState.SetBool(Pending, true);
            SessionState.SetInt(Pending + ".exit", 1);
            EditorApplication.EnterPlaymode();
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static void ApplySettings(HplUserSettings s)
        {
            s.masterVolume = 0;
            string list = Arg("-vramSet");
            if (string.IsNullOrEmpty(list)) return;
            foreach (var pair in list.Split(','))
            {
                var kv = pair.Split('=');
                var field = typeof(HplUserSettings).GetField(kv[0].Trim());
                if (field == null) throw new ArgumentException("Unknown setting " + kv[0]);
                field.SetValue(s, Convert.ChangeType(kv[1].Trim(), field.FieldType, CultureInfo.InvariantCulture));
            }
        }

        static void Update()
        {
            AudioListener.volume = 0;
            if (EditorApplication.timeSinceStartup - s_start > 1500) { Report.AppendLine("FAIL timeout"); Finish(1); return; }
            try
            {
                if (s_stage == 0)
                {
                    var origin = HplMap.Current;
                    if (origin == null || origin.GameUi == null || origin.player?.Body == null) return;
                    s_owner = origin.GameUi;
                    s_owner.Settings.masterVolume = 0;
                    s_owner.Pause(); s_owner.OpenOptions(HplPauseMenu.GraphicsTabIndex);
                    ApplySettings(s_owner.DraftSettings);
                    Report.AppendLine("settings " + JsonUtility.ToJson(s_owner.DraftSettings));
                    Report.AppendLine($"adapter {SystemInfo.graphicsDeviceName}, dedicated {HplVideoMemory.Dedicated / 1048576} MB, DXGI {HplVideoMemory.Available}");
                    Sample("0 before");
                    if (!s_owner.Choose(MenuControl.Benchmark) || !HplBenchmark.Active) { Report.AppendLine("FAIL benchmark did not start"); Finish(1); return; }
                    s_stage = 1;
                    return;
                }
                var test = UnityEngine.Object.FindAnyObjectByType<HplBenchmark>();
                if (s_stage == 1)
                {
                    if (test == null) { s_stage = 2; s_nextSample = EditorApplication.timeSinceStartup + 5; return; }
                    if (test.ShowingResults)
                    {
                        var stats = test.Stats;
                        Report.AppendLine($"benchmark stats: total peak local {stats.Calculate().PeakLocalMb:F0} MB, budget {stats.LocalBudgetMb:F0} MB, stutters {stats.Calculate().Stutters}");
                        for (int i = 0; i < HplBenchmarkRoute.All.Length; i++)
                        {
                            var m = stats.Calculate(i);
                            Report.AppendLine($"  scene {i}: fps {m.AverageFps:F1}, peak local {m.PeakLocalMb:F0} MB, shared {m.PeakSharedMb:F0} MB, stutters {m.Stutters}");
                        }
                        typeof(HplBenchmark).GetMethod("ChooseResult", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(test, new object[] { false });
                        return;
                    }
                    if (test.Map != null)
                    {
                        if (s_target == null)
                        {
                            var size = (Arg("-vramSize") ?? "1920x1080").Split('x');
                            s_target = new RenderTexture(int.Parse(size[0]), int.Parse(size[1]), 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                        }
                        test.Map.PlayerCamera.targetTexture = s_target;
                    }
                    string phase = test.IsPlaying && test.Map != null ? (test.SceneIndex + 1) + " scene " + HplBenchmarkRoute.All[test.SceneIndex].map
                        : test.SceneIndex >= 0 ? (test.SceneIndex + 1) + " loading" : "0 starting";
                    if (EditorApplication.timeSinceStartup >= s_nextSample) Sample(phase);
                    if (test.IsPlaying && test.Map != null)
                    {
                        var p = Get(phase); p.frames++; p.framesSeconds += Time.unscaledDeltaTime;
                        float duration = HplBenchmarkRoute.All[test.SceneIndex].Duration;
                        if (test.SceneTime > duration / 2 && s_breakdownScene != test.SceneIndex) { s_breakdownScene = test.SceneIndex; Breakdown(phase); }
                        // Visit the whole route (streaming requests follow the camera) in 2 s stops every 5 s of it.
                        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-vramFull") < 0)
                        {
                            float stop = Mathf.Floor(test.SceneTime / 5) * 5;
                            if (test.SceneTime - stop > 2 && stop + 5 < duration - 0.5f) Seek(test, stop + 5);
                        }
                    }
                }
                else if (s_stage == 2)
                {
                    if (EditorApplication.timeSinceStartup < s_nextSample) return;
                    Sample("9 after return");
                    Finish(0);
                }
            }
            catch (Exception e) { Report.AppendLine("FAIL " + e); Finish(1); }
        }

        static Phase Get(string name)
        {
            if (!Phases.TryGetValue(name, out var p)) { Phases[name] = p = new Phase(); Order.Add(name); }
            return p;
        }

        static void Sample(string name)
        {
            s_nextSample = EditorApplication.timeSinceStartup + 0.25;
            var p = Get(name);
            if (HplVideoMemory.TryQuery(out var info))
            {
                p.localLast = info.localUsage / 1048576.0;
                p.localPeak = Math.Max(p.localPeak, p.localLast);
                p.sharedPeak = Math.Max(p.sharedPeak, info.sharedUsage / 1048576.0);
                p.budget = info.localBudget / 1048576.0;
            }
            p.texCurrent = Math.Max(p.texCurrent, (long)Texture.currentTextureMemory);
            p.texDesired = Math.Max(p.texDesired, (long)Texture.desiredTextureMemory);
            p.texTarget = Math.Max(p.texTarget, (long)Texture.targetTextureMemory);
            p.texNonStreaming = Math.Max(p.texNonStreaming, (long)Texture.nonStreamingTextureMemory);
            p.texTotal = Math.Max(p.texTotal, (long)Texture.totalTextureMemory);
            for (int i = 0; i < s_recorders.Length; i++)
                if (s_recorders[i].Valid) p.recorders[i] = Math.Max(p.recorders[i], s_recorders[i].LastValue);
        }

        static void Breakdown(string phase)
        {
            Report.AppendLine("breakdown at " + phase);
            var groups = new Dictionary<string, long>();
            var items = new List<(long size, string text)>();
            foreach (var t in Resources.FindObjectsOfTypeAll<Texture>())
            {
                long size = Profiler.GetRuntimeMemorySizeLong(t);
                string type = t.GetType().Name + (t is Texture2D t2 && t2.streamingMipmaps ? " streaming" : "");
                groups[type] = (groups.TryGetValue(type, out var g) ? g : 0) + size;
                items.Add((size, $"{type} {t.name} {t.width}x{t.height}" + (t is RenderTexture rt ? $" {rt.graphicsFormat} vol{rt.volumeDepth}" : "")));
            }
            long meshes = Resources.FindObjectsOfTypeAll<Mesh>().Sum(m => Profiler.GetRuntimeMemorySizeLong(m));
            foreach (var g in groups.OrderByDescending(g => g.Value)) Report.AppendLine($"  {g.Key}: {g.Value / 1048576.0:F0} MB");
            Report.AppendLine($"  Mesh: {meshes / 1048576.0:F0} MB");
            foreach (var item in items.OrderByDescending(i => i.size).Take(25)) Report.AppendLine($"    {item.size / 1048576.0:F1} MB {item.text}");
        }

        static void Seek(HplBenchmark test, float time)
        {
            typeof(HplBenchmark).GetProperty("SceneTime").SetValue(test, time);
            typeof(HplBenchmark).GetField("_sceneStartedAt", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(test, Time.realtimeSinceStartupAsDouble - time);
        }

        static void Finish(int exit)
        {
            var text = new StringBuilder();
            text.AppendLine("phase | DXGI local peak / last / budget MB | shared peak | fps | textures current / desired / target / nonstreaming / total MB | " + string.Join(" / ", RecorderNames));
            foreach (var name in Order)
            {
                var p = Phases[name];
                string fps = p.frames > 0 ? (p.frames / p.framesSeconds).ToString("F1") : "-";
                text.AppendLine($"{name} | {p.localPeak:F0} / {p.localLast:F0} / {p.budget:F0} | {p.sharedPeak:F0} | {fps} | "
                    + $"{p.texCurrent / 1048576} / {p.texDesired / 1048576} / {p.texTarget / 1048576} / {p.texNonStreaming / 1048576} / {p.texTotal / 1048576} | "
                    + string.Join(" / ", p.recorders.Select(r => (r / 1048576).ToString())));
            }
            text.Append(Report);
            string folder = Path.GetFullPath("Logs/VramProbe");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, (Arg("-vramTag") ?? "report") + ".txt"), text.ToString());
            Debug.Log("[VideoMemoryProbe]\n" + text);
            foreach (var r in s_recorders ?? Array.Empty<ProfilerRecorder>()) r.Dispose();
            if (s_target != null) { UnityEngine.Object.DestroyImmediate(s_target); s_target = null; }
            SessionState.SetInt(Pending + ".exit", exit);
            EditorApplication.ExitPlaymode();
        }
    }
}
