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
            stats.Clear();
            for (int i = 0; i < 20; i++) stats.AddFrame(0, 0, 0.025);
            stats.AddFrame(0, 0, 0.05);
            for (int i = 0; i < 20; i++) stats.AddFrame(0, 0, 0.025);
            Check(stats.Calculate().Stutters == 0, "stutter threshold is strictly greater than twice the local median");
            stats.Clear();
            for (int i = 0; i < 20; i++) stats.AddFrame(0, 0, 0.005);
            stats.AddFrame(0, 0, 0.012);
            for (int i = 0; i < 20; i++) stats.AddFrame(0, 0, 0.005);
            Check(stats.Calculate().Stutters == 0, "a jump under the absolute minimum is not a stutter");
            // Slow scenes and lasting level changes (the old whole-run median counted 471 and 627 such frames).
            stats.Clear();
            for (int i = 0; i < 300; i++) stats.AddFrame(0, 0, 0.007);
            for (int i = 0; i < 300; i++) stats.AddFrame(0, 0, 0.040);
            for (int i = 0; i < 40; i++) stats.AddFrame(1, 0, 0.2);
            for (int i = 0; i < 20; i++) stats.AddFrame(1, 0, 0.6);
            for (int i = 0; i < 20; i++) stats.AddFrame(1, 0, 1.1);
            m = stats.Calculate();
            Check(m.Stutters == 0 && stats.Calculate(0).Stutters == 0 && stats.Calculate(1).Stutters == 0, "slow scene and lasting steps are no stutters: " + m.Stutters);
            stats.AddFrame(1, 0, 3.0); for (int i = 0; i < 5; i++) stats.AddFrame(1, 0, 1.1);
            Check(stats.Calculate(1).Stutters == 1 && stats.Calculate().Stutters == 1, "a hitch at a low frame rate");
            stats.Clear();
            for (int i = 0; i < 100; i++) stats.AddFrame(0, 0, 1.0 / 144);
            stats.AddFrame(0, 0, 0.03); stats.AddFrame(0, 0, 0.03);
            for (int i = 0; i < 100; i++) stats.AddFrame(0, 0, 1.0 / 144);
            for (int i = 0; i < 100; i++) stats.AddFrame(1, 0, 1.0 / 30);
            Check(stats.Calculate().Stutters == 2 && stats.Calculate(0).Stutters == 2 && stats.Calculate(1).Stutters == 0,
                "two adjacent hitches; total equals the sum of scenes, a scene change is not a hitch");
            Near(stats.ShareAtOrBelow(1000.0 / 144 * 1.05), 200.0 / 302, "share of frames at a cap");
            stats.SetMemory(0, 3000, 200, 3800); stats.SetMemory(250, 3500, 300, 3700); stats.SetMemory(1, double.NaN, 1, 1);
            Check(stats.Calculate().PeakLocalMb == 3500 && stats.Calculate(0).PeakLocalMb == 3000 && stats.Calculate(1).PeakSharedMb == 300
                && stats.LocalBudgetMb == 3700 && stats[1].localMb == 0, "video memory peaks per scene and the smallest budget");
            stats.Clear();
            Check(stats.LocalBudgetMb == 0, "reset clears the memory budget");
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
