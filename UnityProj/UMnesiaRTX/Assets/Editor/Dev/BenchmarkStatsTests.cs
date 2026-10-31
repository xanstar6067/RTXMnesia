using System;
using RTXMnesia.World;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    public static class BenchmarkStatsTests
    {
        static int s_checks;
        static void Check(bool condition, string message)
        {
            s_checks++;
            if (!condition) throw new InvalidOperationException("[BenchmarkStats] " + message);
        }
        static void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < 1e-6, message + ": " + actual + " != " + expected);
        public static void Run()
        {
            s_checks = 0;
            var stats = new HplBenchmarkStats();
            for (int i = 0; i < 600; i++) stats.AddFrame(0, (i + 1) / 60.0, 1.0 / 60);
            var m = stats.Calculate();
            Near(m.AverageFps, 60, "steady average"); Near(m.Low1Fps, 60, "steady 1% low"); Near(m.Low01Fps, 60, "steady 0.1% low");
            Near(m.MedianMs, 1000.0 / 60, "steady median"); Near(m.P95Ms, m.MedianMs, "steady P95"); Near(m.P99Ms, m.MedianMs, "steady P99");
            Check(m.Stutters == 0 && m.Frames == 600 && m.Bottleneck == HplBenchmarkStats.Limit.Unavailable, "steady count, stutters, unavailable timings");
            stats.AddFrame(0, 10.1, 0.1);
            m = stats.Calculate();
            Near(m.AverageFps, 601 / 10.1, "spike average uses summed time");
            Near(m.Low1Fps, 2 / (0.1 + 1.0 / 60), "1% low consumes whole frames until time budget reached");
            Near(m.Low01Fps, 10, "0.1% low spike"); Near(m.MaxMs, 100, "maximum spike");
            Near(m.P99Ms, 1000.0 / 60, "nearest-rank P99"); Near(m.P999Ms, 100, "P99.9 includes spike");
            Check(m.Stutters == 1, "one 100 ms stutter");
            stats.Clear();
            for (int i = 0; i < 60; i++) stats.AddFrame(0, (i + 1) / 60.0, 1.0 / 60, 6, 8, 16);
            for (int i = 0; i < 30; i++) stats.AddFrame(1, (i + 1) / 30.0, 1.0 / 30, 6, 8, 16);
            m = stats.Calculate();
            Near(m.AverageFps, 45, "two-scene average"); Near(m.Low1Fps, 30, "two-scene low");
            Near(stats.Calculate(0).AverageFps, 60, "first scene"); Near(stats.Calculate(1).AverageFps, 30, "second scene");
            Check(stats.Calculate(0).Frames == 60 && stats.Calculate(1).Frames == 30 && m.Stutters == 0, "scene counts and stutters");
            Check(m.Bottleneck == HplBenchmarkStats.Limit.Gpu, "GPU limit");
            Check(stats[60].scene == 1 && stats[60].sceneTime > 0 && stats[60].cpuRenderMs == 8, "raw sample preserves scene, clock and threads");
            stats.Clear();
            stats.AddFrame(0, 0.01, 0.01, 4, 20, 8); stats.AddFrame(0, 0.03, 0.02, 4, 20, 8);
            Near(stats.Calculate().MedianMs, 15, "even median");
            Check(stats.Calculate().Bottleneck == HplBenchmarkStats.Limit.Cpu, "CPU uses maximum of threads");
            stats.Clear(); stats.AddFrame(0, 0.01, 0.01, 10, 8, 9.5);
            Check(stats.Calculate().Bottleneck == HplBenchmarkStats.Limit.Balanced, "balanced below 10%");
            stats.Clear(); stats.AddFrame(0, 0.01, 0.01, 10, 0, 9.5);
            Check(stats.Calculate().Bottleneck == HplBenchmarkStats.Limit.Unavailable, "missing render thread");
            stats.SetTiming(0, 10, 5, 0);
            Check(stats.Calculate().Bottleneck == HplBenchmarkStats.Limit.Unavailable, "missing GPU");
            stats.SetTiming(0, 10, 5, 9);
            Check(stats.Calculate().Bottleneck == HplBenchmarkStats.Limit.Cpu, "10% boundary is not balanced");
            stats.Clear(); stats.AddFrame(0, 0.025, 0.025); stats.AddFrame(0, 0.05, 0.025); stats.AddFrame(0, 0.1, 0.05);
            Check(stats.Calculate().Stutters == 0, "stutter threshold is strictly greater");
            stats.Clear();
            Check(stats.Count == 0 && stats.Calculate().Frames == 0 && stats.Calculate(2).AverageFps == 0, "empty and reset");
            Check(stats.AddFrame(0, 0, 0) == -1 && stats.AddFrame(0, 0, double.NaN) == -1 && stats.AddFrame(0, 0, double.PositiveInfinity) == -1, "reject invalid duration");
            var small = new HplBenchmarkStats(1);
            small.AddFrame(0, 1, 1, double.NaN, -1, double.PositiveInfinity); small.AddFrame(0, 2, 1);
            Check(small.Count == 1 && small.DroppedFrames == 1 && small[0].cpuMainMs == 0 && small[0].gpuMs == 0, "bounded buffer and invalid timings");
            Debug.Log("[BenchmarkStats] PASS: " + s_checks + " checks");
        }

        public static void RunBatch()
        {
            try { AudioListener.volume = 0; Run(); BuildCheck.RunBatch(); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
        }
    }
}
