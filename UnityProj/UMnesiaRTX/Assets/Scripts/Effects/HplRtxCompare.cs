using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Comparison of the lightings in game (key C, not in the original): the current frame is kept, the lighting
    /// switches between the classic one and ray tracing, and a wipe line sweeps the new image over the kept one from
    /// left to right (<see cref="HplScreenEffectsPass"/>); a label in the HUD names the new lighting. The switch is
    /// not saved: the options window applies the chosen lighting again.
    /// </summary>
    public static class HplRtxCompare
    {
        public const float WipeTime = 0.9f, LabelTime = 1.5f;
        enum Phase { Idle, Capture, Switch, Wipe }

        static Phase s_phase;
        static float s_start, s_labelStart = -100f;
        static HplUserSettings s_settings;

        /// <summary>The GPU traces rays (otherwise there is nothing to compare).</summary>
        public static bool Available => SystemInfo.supportsRayTracing;
        /// <summary>The screen pass copies this frame as the kept image.</summary>
        public static bool CaptureRequested => s_phase == Phase.Capture;
        public static bool Wiping => s_phase == Phase.Wipe;
        /// <summary>The wipe line from 0 (left edge) to 1 (right edge), eased.</summary>
        public static float Progress => s_phase == Phase.Wipe ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - s_start) / WipeTime)) : 0f;
        /// <summary>The lighting shown after the switch is ray tracing.</summary>
        public static bool RayTracing { get; private set; }
        public static string Label => RayTracing ? "Трассировка лучей" : "Классическое освещение";
        /// <summary>The HUD label: shown at the switch, fades out after the wipe.</summary>
        public static float LabelAlpha
        {
            get
            {
                float t = Time.unscaledTime - s_labelStart;
                if (t < 0f || t > WipeTime + LabelTime) return 0f;
                return Mathf.Clamp01((WipeTime + LabelTime - t) / 0.5f) * Mathf.Clamp01(t / 0.15f);
            }
        }

        /// <summary>Key C: starts a comparison from the applied settings (ignored while one is running).</summary>
        public static void Toggle(HplUserSettings settings)
        {
            if (!Available || settings == null || s_phase != Phase.Idle) return;
            s_settings = settings;
            s_phase = Phase.Capture;
            s_start = Time.unscaledTime;
        }

        /// <summary>The screen pass has kept the frame: the lighting switches on the next update.</summary>
        public static void Captured()
        {
            if (s_phase == Phase.Capture) s_phase = Phase.Switch;
        }

        /// <summary>Every frame (HplGameUi): the switch after the capture, the end of the wipe.</summary>
        public static void Update()
        {
            switch (s_phase)
            {
                case Phase.Capture:
                    // No screen pass rendered (no camera): give up.
                    if (Time.unscaledTime - s_start > 0.5f) s_phase = Phase.Idle;
                    break;
                case Phase.Switch:
                {
                    var s = s_settings.Copy();
                    s.lighting = HplRayTracing.Active ? 0 : 1;
                    HplGraphics.Apply(s);
                    RayTracing = HplRayTracing.Active;
                    s_phase = Phase.Wipe;
                    s_start = s_labelStart = Time.unscaledTime;
                    break;
                }
                case Phase.Wipe:
                    if (Time.unscaledTime - s_start >= WipeTime) s_phase = Phase.Idle;
                    break;
            }
        }
    }
}
