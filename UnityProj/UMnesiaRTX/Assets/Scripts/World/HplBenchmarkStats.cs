using System;

namespace RTXMnesia.World
{
    /// <summary>In-memory samples only. AddFrame/SetTiming allocate nothing; analysis runs after the tour.</summary>
    public sealed class HplBenchmarkStats
    {
        public const int DefaultCapacity = 180 * 1000;
        public struct Frame
        {
            public int scene;
            public double sceneTime, seconds, cpuMainMs, cpuRenderMs, gpuMs;
            /// <summary>Video memory of the process in MB (HplVideoMemory), 0 where not sampled.</summary>
            public float localMb, sharedMb;
        }

        public enum Limit { Unavailable, Gpu, Cpu, Balanced }

        public sealed class Metrics
        {
            public int Frames { get; internal set; }
            public double Seconds { get; internal set; }
            public double AverageFps => Seconds > 0 ? Frames / Seconds : 0;
            public double Low1Fps { get; internal set; }
            public double Low01Fps { get; internal set; }
            public double MedianMs { get; internal set; }
            public double P95Ms { get; internal set; }
            public double P99Ms { get; internal set; }
            public double P999Ms { get; internal set; }
            public double MaxMs { get; internal set; }
            public int Stutters { get; internal set; }
            public Limit Bottleneck { get; internal set; }
            /// <summary>Peaks of the sampled video memory, MB (0: not available).</summary>
            public double PeakLocalMb { get; internal set; }
            public double PeakSharedMb { get; internal set; }
        }

        readonly Frame[] _frames;
        public int Count { get; private set; }
        public int DroppedFrames { get; private set; }
        /// <summary>Smallest OS budget of the card's own memory seen during the run, MB (0: not available).</summary>
        public double LocalBudgetMb { get; private set; }
        public Frame this[int index] => index >= 0 && index < Count ? _frames[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public HplBenchmarkStats(int capacity = DefaultCapacity) => _frames = new Frame[capacity];
        public void Clear() { Count = 0; DroppedFrames = 0; LocalBudgetMb = 0; }
        public static bool Valid(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);

        public int AddFrame(int scene, double sceneTime, double seconds, double cpuMainMs = 0, double cpuRenderMs = 0, double gpuMs = 0)
        {
            if (scene < 0 || !Valid(seconds) || double.IsNaN(sceneTime) || double.IsInfinity(sceneTime) || sceneTime < 0) return -1;
            if (Count == _frames.Length) { DroppedFrames++; return -1; }
            int index = Count++;
            _frames[index] = new Frame { scene = scene, sceneTime = sceneTime, seconds = seconds };
            SetTiming(index, cpuMainMs, cpuRenderMs, gpuMs);
            return index;
        }

        public void SetTiming(int index, double cpuMainMs, double cpuRenderMs, double gpuMs)
        {
            if (index < 0 || index >= Count) return;
            ref var f = ref _frames[index];
            f.cpuMainMs = Valid(cpuMainMs) ? cpuMainMs : 0;
            f.cpuRenderMs = Valid(cpuRenderMs) ? cpuRenderMs : 0;
            f.gpuMs = Valid(gpuMs) ? gpuMs : 0;
        }

        public void SetMemory(int index, double localMb, double sharedMb, double budgetMb)
        {
            if (index < 0 || index >= Count || !Valid(localMb)) return;
            _frames[index].localMb = (float)localMb;
            _frames[index].sharedMb = Valid(sharedMb) ? (float)sharedMb : 0;
            if (Valid(budgetMb) && (LocalBudgetMb == 0 || budgetMb < LocalBudgetMb)) LocalBudgetMb = budgetMb;
        }

        /// <summary>Frames longer than both twice the median of their neighbours (a quarter second on each side, same scene)
        /// and that median plus <see cref="StutterMinMs"/>. A local reference: a slow scene is not a run of stutters, and
        /// neither is a lasting change of level (half of the neighbours of the frame at a step already share its level);
        /// a single jump out of its own level is. The whole run's median counted every frame of a slow scene.</summary>
        public const double StutterSideSeconds = 0.25, StutterMinMs = 8;
        public const int StutterMinSide = 3, StutterMaxSide = 30;

        /// <summary>Median is the midpoint for an even count; other percentiles use nearest rank.</summary>
        public Metrics Calculate(int scene = -1)
        {
            var m = new Metrics();
            for (int i = 0; i < Count; i++)
                if (scene < 0 || _frames[i].scene == scene) { m.Frames++; m.Seconds += _frames[i].seconds; }
            if (m.Frames == 0) return m;
            var times = new double[m.Frames];
            var cpu = new double[m.Frames];
            var gpu = new double[m.Frames];
            int n = 0, paired = 0;
            var history = new double[2 * StutterMaxSide];
            for (int i = 0; i < Count; i++)
            {
                var f = _frames[i];
                if (scene >= 0 && f.scene != scene) continue;
                times[n++] = f.seconds * 1000;
                if (IsStutter(i, history)) m.Stutters++;
                m.PeakLocalMb = Math.Max(m.PeakLocalMb, f.localMb);
                m.PeakSharedMb = Math.Max(m.PeakSharedMb, f.sharedMb);
                // Compare the same frames. A missing thread time cannot establish a CPU limit.
                if (Valid(f.cpuMainMs) && Valid(f.cpuRenderMs) && Valid(f.gpuMs))
                { cpu[paired] = Math.Max(f.cpuMainMs, f.cpuRenderMs); gpu[paired++] = f.gpuMs; }
            }
            Array.Sort(times);
            m.MedianMs = Median(times, n);
            m.P95Ms = Percentile(times, 0.95);
            m.P99Ms = Percentile(times, 0.99);
            m.P999Ms = Percentile(times, 0.999);
            m.MaxMs = times[n - 1];
            m.Low1Fps = Low(times, m.Seconds, 0.01);
            m.Low01Fps = Low(times, m.Seconds, 0.001);
            if (paired > 0)
            {
                Array.Sort(cpu, 0, paired); Array.Sort(gpu, 0, paired);
                double c = Median(cpu, paired), g = Median(gpu, paired);
                m.Bottleneck = Math.Abs(c - g) / Math.Max(c, g) < 0.1 ? Limit.Balanced : g > c ? Limit.Gpu : Limit.Cpu;
            }
            return m;
        }

        bool IsStutter(int index, double[] history)
        {
            // The same number of frames on both sides: at a step from fast to slow frames a time window would hold
            // many fast frames on one side and few slow ones on the other, and call the first slow frame a stutter.
            int side = Math.Max(SideFrames(index, -1), SideFrames(index, 1)), count = 0, scene = _frames[index].scene;
            for (int step = -1; step <= 1; step += 2)
                for (int i = index + step, n = 0; n < side && i >= 0 && i < Count && _frames[i].scene == scene; i += step, n++)
                    history[count++] = _frames[i].seconds * 1000;
            if (count < 2 * StutterMinSide) return false;
            Array.Sort(history, 0, count);
            double reference = Median(history, count), ms = _frames[index].seconds * 1000;
            return ms > 2 * reference && ms > reference + StutterMinMs;
        }

        /// <summary>Frames covering a quarter second on one side; at very low frame rates at least the minimum.</summary>
        int SideFrames(int index, int step)
        {
            double seconds = 0;
            int n = 0, scene = _frames[index].scene;
            for (int i = index + step; i >= 0 && i < Count && n < StutterMaxSide && _frames[i].scene == scene; i += step)
            {
                if (seconds >= StutterSideSeconds && n >= StutterMinSide) break;
                seconds += _frames[i].seconds; n++;
            }
            return n;
        }

        /// <summary>Share of frames at least as fast as <paramref name="ms"/> (an FPS cap that was actually reached).</summary>
        public double ShareAtOrBelow(double ms)
        {
            if (Count == 0) return 0;
            int n = 0;
            for (int i = 0; i < Count; i++) if (_frames[i].seconds * 1000 <= ms) n++;
            return (double)n / Count;
        }

        static double Median(double[] sorted, int count) => count % 2 == 0 ? (sorted[count / 2 - 1] + sorted[count / 2]) / 2 : sorted[count / 2];
        static double Percentile(double[] sorted, double p) => sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * p) - 1)];
        static double Low(double[] sortedMs, double totalSeconds, double fraction)
        {
            double sumMs = 0, targetMs = totalSeconds * 1000 * fraction;
            int n = 0;
            // Whole slowest frames, time weighted (not the slowest fraction of the frame count).
            while (n < sortedMs.Length && sumMs + 1e-9 < targetMs) sumMs += sortedMs[sortedMs.Length - 1 - n++];
            return n * 1000 / sumMs;
        }
    }
}
