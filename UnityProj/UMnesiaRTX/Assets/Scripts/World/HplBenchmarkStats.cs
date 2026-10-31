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
        }

        readonly Frame[] _frames;
        public int Count { get; private set; }
        public int DroppedFrames { get; private set; }
        public Frame this[int index] => index >= 0 && index < Count ? _frames[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public HplBenchmarkStats(int capacity = DefaultCapacity) => _frames = new Frame[capacity];
        public void Clear() { Count = 0; DroppedFrames = 0; }
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
            for (int i = 0; i < Count; i++)
            {
                var f = _frames[i];
                if (scene >= 0 && f.scene != scene) continue;
                times[n++] = f.seconds * 1000;
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
            double threshold = Math.Max(2 * m.MedianMs, 25);
            foreach (double time in times) if (time > threshold) m.Stutters++;
            if (paired > 0)
            {
                Array.Sort(cpu, 0, paired); Array.Sort(gpu, 0, paired);
                double c = Median(cpu, paired), g = Median(gpu, paired);
                m.Bottleneck = Math.Abs(c - g) / Math.Max(c, g) < 0.1 ? Limit.Balanced : g > c ? Limit.Gpu : Limit.Cpu;
            }
            return m;
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
