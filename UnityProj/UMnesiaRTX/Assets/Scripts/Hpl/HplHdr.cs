using RTXMnesia.Ui;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Hpl
{
    /// <summary>Native display output and display-referred calibration, independent of save games.</summary>
    public sealed class HplHdr : MonoBehaviour
    {
        public const float MinPeak = 400, MaxPeak = 5000, MinWhite = 80, MaxWhite = 400, MaxBlack = 0.1f;
        static HplHdr s_instance;
        static HplUserSettings s_settings = new();
        Tonemapping _tonemapping;
        VolumeProfile _profile;
        bool? _requested;
        bool _available, _lastActive;
        float _requestTime;
        // Unity reports HDR as available only with Player Settings > "Use HDR Display Output" (useHDRDisplay) enabled;
        // without it the display getters throw InvalidOperationException even though 'main' is non-null.
        public static bool Available
        {
            get
            {
                try
                {
                    return (SystemInfo.hdrDisplaySupportFlags & HDRDisplaySupportFlags.Supported) != 0 &&
                        HDROutputSettings.main != null && HDROutputSettings.main.available;
                }
                catch (System.InvalidOperationException) { return false; }
            }
        }
        public static bool Active
        {
            get
            {
                try { return Available && HDROutputSettings.main.active; }
                catch (System.InvalidOperationException) { return false; }
            }
        }
        public static bool Pending
        {
            get
            {
                try { return Available && HDROutputSettings.main.HDRModeChangeRequested; }
                catch (System.InvalidOperationException) { return false; }
            }
        }
        public static float PaperWhite => s_settings.hdrPaperWhite;
        public static float Peak => s_settings.hdrPeak;
        public static float Black => s_settings.hdrBlack;
        public static int Revision { get; private set; }
        public static bool CalibrationVisible { get; set; }
        public static int CalibrationPattern { get; set; }
        /// <summary>Viewport rectangle, top-left origin, normalized to the display.</summary>
        public static Rect CalibrationRect { get; set; }

        public static float Finite(float value, float fallback, float min, float max) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        public static void Apply(HplUserSettings settings)
        {
            if (settings == null) return;
            settings.Validate();
            if (s_settings.hdrMode != settings.hdrMode || s_settings.hdrPaperWhite != settings.hdrPaperWhite ||
                s_settings.hdrPeak != settings.hdrPeak || s_settings.hdrBlack != settings.hdrBlack) Revision++;
            s_settings = settings.Copy();
            if (!Application.isPlaying) return;
            if (s_instance == null)
            {
                var go = new GameObject("HPL HDR output") { hideFlags = HideFlags.DontSave };
                DontDestroyOnLoad(go);
                s_instance = go.AddComponent<HplHdr>();
                var volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1100;
                s_instance._profile = ScriptableObject.CreateInstance<VolumeProfile>();
                s_instance._profile.hideFlags = HideFlags.HideAndDontSave;
                volume.sharedProfile = s_instance._profile;
                s_instance._tonemapping = s_instance._profile.Add<Tonemapping>(false);
            }
            s_instance.Update();
        }

        void Update()
        {
            bool available = Available;
            if (!available)
            {
                _requested = null;
                _available = false;
                SetActive(false);
                return;
            }
            // Auto and On request native HDR only if the OS/display exposes it. Never change Windows settings.
            bool desired = s_settings.hdrMode != 0;
            var display = HDROutputSettings.main;
            bool canSwitch = (SystemInfo.hdrDisplaySupportFlags & HDRDisplaySupportFlags.RuntimeSwitchable) != 0;
            if (canSwitch && !Pending && (_requested != desired || _available != available))
            {
                _requested = desired;
                _available = available;
                _requestTime = Time.realtimeSinceStartup;
                if (display.active != desired) display.RequestHDRModeChange(desired);
            }
            // Reconcile display/OS changes without flooding a driver which refuses the request.
            if (canSwitch && !Pending && display.active != desired && Time.realtimeSinceStartup - _requestTime > 5)
                _requested = null;
            bool active = display.active;
            SetActive(active);
            if (!active || _tonemapping == null) return;
            display.automaticHDRTonemapping = false;
            _tonemapping.mode.Override(TonemappingMode.Neutral);
            _tonemapping.neutralHDRRangeReductionMode.Override(NeutralRangeReductionMode.BT2390);
            _tonemapping.hueShiftAmount.Override(0);
            _tonemapping.detectPaperWhite.Override(false);
            _tonemapping.detectBrightnessLimits.Override(false);
            _tonemapping.paperWhite.Override(PaperWhite);
            _tonemapping.minNits.Override(Black);
            _tonemapping.maxNits.Override(Peak);
        }

        void SetActive(bool active)
        {
            if (_lastActive != active) { Revision++; _lastActive = active; }
            Shader.SetGlobalFloat("_HplHdrActive", active ? 1 : 0);
            if (_tonemapping == null) return;
            // An inactive component leaves each SDR map's existing tonemapping untouched.
            _tonemapping.active = active;
        }

        public static void UseDisplayValues(HplUserSettings settings)
        {
            if (!Available) return;
            var display = HDROutputSettings.main;
            if (display.maxToneMapLuminance > 0) settings.hdrPeak = display.maxToneMapLuminance;
            if (display.paperWhiteNits > 0) settings.hdrPaperWhite = display.paperWhiteNits;
            if (display.minToneMapLuminance >= 0) settings.hdrBlack = display.minToneMapLuminance;
            settings.Validate();
        }

        void OnDestroy()
        {
            if (s_instance != this) return;
            s_instance = null;
            CalibrationVisible = false;
            Shader.SetGlobalFloat("_HplHdrActive", 0);
            if (_profile != null)
            {
                foreach (var component in _profile.components) Destroy(component);
                Destroy(_profile);
            }
        }
    }
}
