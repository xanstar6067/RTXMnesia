using System;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>User preferences, separate from inventory / map saves. Only implemented Unity options are exposed.</summary>
    [Serializable]
    public sealed class HplUserSettings
    {
        const string PrefsKey = "RTXMnesia.UserSettings.v1";
        public float mouseSensitivity = 1f, masterVolume = 1f;
        public bool invertMouse, smoothMouse = true, subtitles = true, hints = true, crosshair = true;
        public bool fullscreen, vSync;
        /// <summary>Display gamma (HPL2 Graphics Gamma, 0.3–2, SDL_SetGamma); drawn by HplScreenEffectsPass.</summary>
        public float gamma = 1f;
        /// <summary>Game option "Commentary" (user config Game ShowCommentary, off by default): developer commentary icons.</summary>
        public bool commentary;

        /// <summary>The applied commentary option (cLuxMapHandler::mbShowCommentary).</summary>
        public static bool ShowCommentary { get; set; }
        public const float MinGamma = 0.3f, MaxGamma = 2f;

        /// <summary>The applied gamma (also set by the pre menu before a map exists).</summary>
        public static float Gamma { get; set; } = 1f;

        /// <summary>The saved gamma without a map (pre menu at startup).</summary>
        public static float SavedGamma()
        {
            try
            {
                if (!PlayerPrefs.HasKey(PrefsKey)) return 1f;
                var s = new HplUserSettings();
                JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), s);
                s.Validate();
                return s.gamma;
            }
            catch (ArgumentException) { return 1f; }
        }

        /// <summary>Stores only the gamma (the pre menu's choice) in the saved settings.</summary>
        public static void SaveGamma(float value)
        {
            var s = new HplUserSettings();
            try
            {
                if (PlayerPrefs.HasKey(PrefsKey)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), s);
            }
            catch (ArgumentException) { }
            s.gamma = value;
            s.Save();
        }

        public HplUserSettings Copy() => (HplUserSettings)MemberwiseClone();

        public void Validate()
        {
            mouseSensitivity = float.IsNaN(mouseSensitivity) || float.IsInfinity(mouseSensitivity)
                ? 1f : Mathf.Clamp(mouseSensitivity, 0.2f, 5f);
            masterVolume = float.IsNaN(masterVolume) || float.IsInfinity(masterVolume)
                ? 1f : Mathf.Clamp01(masterVolume);
            gamma = float.IsNaN(gamma) || float.IsInfinity(gamma) || gamma <= 0f ? 1f : Mathf.Clamp(gamma, MinGamma, MaxGamma);
        }

        public static HplUserSettings Load(HplMap map)
        {
            var settings = new HplUserSettings {
                mouseSensitivity = map.player != null ? map.player.mouseSensitivity : 1f,
                invertMouse = map.player != null && map.player.invertMouse,
                smoothMouse = map.player == null || map.player.smoothMouse,
                subtitles = map.hud.showSubtitles, hints = map.hud.showHints, crosshair = map.hud.showCrossHair,
                masterVolume = AudioListener.volume, fullscreen = Screen.fullScreen, vSync = QualitySettings.vSyncCount > 0,
            };
            try
            {
                if (PlayerPrefs.HasKey(PrefsKey)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), settings);
            }
            catch (ArgumentException) { Debug.LogWarning("[RTXMnesia] Invalid user settings; using current defaults."); }
            settings.Validate();
            return settings;
        }

        public void Apply(HplMap map)
        {
            Validate();
            if (map.player != null)
            {
                map.player.mouseSensitivity = mouseSensitivity;
                map.player.invertMouse = invertMouse;
                map.player.smoothMouse = smoothMouse;
            }
            map.hud.showSubtitles = subtitles;
            map.hud.showHints = hints;
            map.hud.showCrossHair = crosshair;
            AudioListener.volume = masterVolume;
            Gamma = gamma;
            // cLuxMapHandler::SetShowCommentary: turning it off stops the playing commentary.
            if (ShowCommentary && !commentary) map.effects?.PlayCommentary.Stop();
            ShowCommentary = commentary;
            QualitySettings.vSyncCount = vSync ? 1 : 0;
            if (!Application.isEditor && Screen.fullScreen != fullscreen)
                Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        public void Save()
        {
            Validate();
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }
}
