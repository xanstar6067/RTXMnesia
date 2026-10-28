using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Graphics comparisons in game (configurable keys C / L / T): the current frame is kept, the selected setting
    /// switches and is saved — the options window shows the new state —
    /// and a wipe line sweeps the new image over the kept one from left to right (<see cref="HplScreenEffectsPass"/>);
    /// a label in the HUD names the new state. Each comparison changes only its own setting.
    /// </summary>
    public static class HplRtxCompare
    {
        public const float WipeTime = 0.9f, LabelTime = 1.5f;
        public enum Comparison { RayTracing, Lighting, Textures }
        enum Phase { Idle, Capture, Switch, Wipe }

        static Phase s_phase;
        static float s_start, s_labelStart = -100f;
        static HplUserSettings s_settings;
        static Comparison s_comparison;
        static bool s_english;

        /// <summary>The GPU traces rays (otherwise there is nothing to compare).</summary>
        public static bool Available => SystemInfo.supportsRayTracing;
        /// <summary>The screen pass copies this frame as the kept image.</summary>
        public static bool CaptureRequested => s_phase == Phase.Capture;
        public static bool Wiping => s_phase == Phase.Wipe;
        /// <summary>The wipe line from 0 (left edge) to 1 (right edge), eased.</summary>
        public static float Progress => s_phase == Phase.Wipe ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - s_start) / WipeTime)) : 0f;
        /// <summary>Ray tracing is on after the switch.</summary>
        public static bool RayTracing { get; private set; }
        public static string Label { get; private set; } = "";
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

        /// <summary>Switches one applied setting, ignored while a comparison is running. Only RTX requires DXR.</summary>
        public static void Toggle(HplUserSettings settings, Comparison comparison = Comparison.RayTracing, HplLanguage language = null)
        {
            if (settings == null || s_phase != Phase.Idle || (comparison == Comparison.RayTracing && !Available)) return;
            s_settings = settings;
            s_comparison = comparison;
            s_english = language != null && language.language != null && language.language.StartsWith("eng", System.StringComparison.OrdinalIgnoreCase);
            s_phase = Phase.Capture;
            s_start = Time.unscaledTime;
        }

        /// <summary>The screen pass has kept the frame: the selected setting switches on the next update.</summary>
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
                    switch (s_comparison)
                    {
                        case Comparison.RayTracing:
                            s_settings.lighting = HplRayTracing.Active ? 0 : 1;
                            Label = s_english ? (s_settings.lighting == 1 ? "Ray tracing: on" : "Ray tracing: off")
                                : s_settings.lighting == 1 ? "Трассировка лучей: вкл" : "Трассировка лучей: выкл";
                            break;
                        case Comparison.Lighting:
                            s_settings.lightingSet = HplGraphics.Lighting == 1 ? 0 : 1;
                            Label = s_english ? (s_settings.lightingSet == 1 ? "Lighting: enhanced" : "Lighting: classic")
                                : s_settings.lightingSet == 1 ? "Освещение: улучшенное" : "Освещение: классическое";
                            break;
                        case Comparison.Textures:
                            s_settings.textures = HplGraphics.Textures == 1 ? 0 : 1;
                            Label = s_english ? (s_settings.textures == 1 ? "Textures: enhanced" : "Textures: classic")
                                : s_settings.textures == 1 ? "Текстуры: улучшенные" : "Текстуры: классические";
                            break;
                    }
                    HplGraphics.Apply(s_settings);
                    s_settings.Save();
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
