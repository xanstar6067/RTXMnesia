using System;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>User preferences, separate from inventory / map saves. Only implemented Unity options are exposed.</summary>
    [Serializable]
    public sealed class HplUserSettings
    {
        public const string PrefsKey = "RTXMnesia.UserSettings.v1";
        public float mouseSensitivity = 1f, masterVolume = 1f;
        public bool invertMouse, smoothMouse = true, subtitles = true, hints = true, crosshair = true;
        /// <summary>Old screen flag, read only to migrate to <see cref="screenMode"/>.</summary>
        public bool fullscreen;
        public bool vSync;
        /// <summary>0 — exclusive full screen, 1 — borderless full screen window, 2 — window; −1: from <see cref="fullscreen"/>.</summary>
        public int screenMode = -1;
        /// <summary>The screen size; 0 × 0 — the desktop resolution.</summary>
        public int resolutionWidth, resolutionHeight;
        /// <summary>Frames per second limit (one of <see cref="HplGraphics.FpsLimits"/>); 0 — none. VSync overrides it.</summary>
        public int fpsLimit;
        /// <summary>Lighting and texture sets: 0 — classic (the only one so far).</summary>
        public int lighting, textures;
        /// <summary>0 — low, 1 — medium, 2 — high (cLuxMainMenu_Options texture quality).</summary>
        public int textureQuality = 2;
        /// <summary>Index of <see cref="HplGraphics.AnisotropyLevels"/> (8× — the import default).</summary>
        public int anisotropy = 3;
        /// <summary><see cref="HplAntialiasing"/>.</summary>
        public int antialiasing = (int)HplAntialiasing.Taa;
        /// <summary>0 — off, 1 — low, 2 — medium, 3 — high.</summary>
        public int shadows = HplGraphics.ShadowsHigh;
        /// <summary>Post effects of the original options: bloom, image trail, radial blur.</summary>
        public bool bloom = true, imageTrail = true, radialBlur = true;
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
        public static float SavedGamma() => PlayerPrefs.HasKey(PrefsKey) ? ReadSaved().gamma : 1f;

        /// <summary>The saved settings without a map (startup); defaults where nothing is saved.</summary>
        public static HplUserSettings ReadSaved()
        {
            var s = new HplUserSettings { fullscreen = Screen.fullScreen, vSync = QualitySettings.vSyncCount > 0 };
            try
            {
                if (PlayerPrefs.HasKey(PrefsKey)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), s);
            }
            catch (ArgumentException) { }
            s.Validate();
            return s;
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
            if (screenMode < 0 || screenMode > 2) screenMode = fullscreen ? 1 : 2;
            fullscreen = screenMode != 2;
            if (resolutionWidth <= 0 || resolutionHeight <= 0) resolutionWidth = resolutionHeight = 0;
            if (Array.IndexOf(HplGraphics.FpsLimits, fpsLimit) < 0) fpsLimit = 0;
            lighting = 0;
            textures = 0;
            textureQuality = Mathf.Clamp(textureQuality, 0, 2);
            anisotropy = Mathf.Clamp(anisotropy, 0, HplGraphics.AnisotropyLevels.Length - 1);
            antialiasing = Mathf.Clamp(antialiasing, 0, (int)HplAntialiasing.Taa);
            shadows = Mathf.Clamp(shadows, HplGraphics.ShadowsOff, HplGraphics.ShadowsHigh);
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
            HplGraphics.Apply(this);
        }

        public void Save()
        {
            Validate();
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }
    }
}
