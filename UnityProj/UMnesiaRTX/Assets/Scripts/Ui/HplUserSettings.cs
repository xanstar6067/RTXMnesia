using System;
using RTXMnesia.Hpl;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// User preferences, separate from inventory / map saves. Only implemented Unity options are exposed. As in HPL2,
    /// the game and input options belong to the profile (user_settings.cfg of the profile → <see cref="ProfilePart"/>
    /// in user_settings.json); graphics, volume and gamma are global (main settings → PlayerPrefs).
    /// </summary>
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
        /// <summary>Lighting: 0 — classic, 1 — ray tracing (DXR). Textures: 0 — classic, 1 — enhanced with classic fallback.</summary>
        public int lighting, textures;
        /// <summary>The lighting set (graphics tab): 0 — classic (the map's lights), 1 — enhanced (the map's overlay, <see cref="World.HplEnhancedLighting"/>).</summary>
        public int lightingSet;
        /// <summary><see cref="HplDlssMode"/> (RTX tab); DLSS replaces <see cref="antialiasing"/> while it is on.</summary>
        public int dlssMode;
        /// <summary>The DLSS model: a NVIDIA.DLSSPreset bit, 0 — NVIDIA's default for each mode.</summary>
        public int dlssPreset;
        /// <summary>Ray-traced shadows (RTX tab, "Ray tracing" lighting): index of <see cref="HplRayTracing.ShadowLights"/>, 0 — off.</summary>
        public int rtShadows = HplRayTracing.ShadowsDefault;
        /// <summary>Ray-traced ambient occlusion (RTX tab): 0 — off, 1..3 — low, medium, high.</summary>
        public int rtAo = HplRayTracing.AoDefault;
        /// <summary>Ray-traced reflections (RTX tab): 0 — off, 1..3 — low, medium, high.</summary>
        public int rtReflections = HplRayTracing.ReflectionsDefault;
        /// <summary>Ray-traced global illumination (RTX tab): 0 — off, 1..3 — low, medium, high.</summary>
        public int rtGi = HplRayTracing.GiDefault;
        /// <summary>RTX tab: ray-traced shadows also from the lights HPL2 drew without shadows (candles).</summary>
        public bool rtAllShadows = true;
        /// <summary>RTX tab: the lantern in the hand casts ray-traced shadows.</summary>
        public bool rtLanternShadows = true;
        /// <summary>RTX tab: path tracing, index of <see cref="HplRayTracing.PathTracingSamples"/> (0 — off).</summary>
        public int rtPathTracing;
        /// <summary>0 — low, 1 — medium, 2 — high (cLuxMainMenu_Options texture quality).</summary>
        public int textureQuality = 2;
        /// <summary>Streaming texture memory budget in GiB; shared graphics setting, outside profiles.</summary>
        public int textureMemoryBudgetGb = 2;
        /// <summary>Index of <see cref="HplGraphics.AnisotropyLevels"/> (8× — the import default).</summary>
        public int anisotropy = 3;
        /// <summary><see cref="HplAntialiasing"/>.</summary>
        public int antialiasing = (int)HplAntialiasing.Taa;
        /// <summary>0 — off, 1 — low, 2 — medium, 3 — high.</summary>
        public int shadows = HplGraphics.ShadowsHigh;
        /// <summary>Enhanced lighting: soft shadows (PCSS) and contact shadows of the shadow maps (HplEnhancedShadows).</summary>
        public bool softShadows = true, contactShadows = true;
        /// <summary>Enhanced lighting: the lantern in the hand casts shadows (HplEnhancedShadows.Lantern).</summary>
        public bool lanternShadow = true;
        /// <summary>Enhanced lighting: still lights render their shadow maps once (HplShadowCache).</summary>
        public bool shadowCache = true;
        /// <summary>Enhanced lighting without RTGI: screen space GI, 0 — off, 1..3 — low, medium, high (HplRayTracing.ScreenGi).</summary>
        public int ssgi = HplRayTracing.SsgiDefault;
        /// <summary>Post effects of the original options: bloom, image trail, radial blur.</summary>
        public bool bloom = true, imageTrail = true, radialBlur = true;
        /// <summary>Display gamma (HPL2 Graphics Gamma, 0.3–2, SDL_SetGamma); drawn by HplScreenEffectsPass.</summary>
        public float gamma = 1f;
        /// <summary>Native HDR output: 0 off, 1 automatic, 2 on when supported. Global, outside profiles.</summary>
        public int hdrMode = 1;
        public float hdrPeak = 1000f, hdrPaperWhite = 160f, hdrBlack = 0f;
        /// <summary>HDR shadow depth: the display exponent of HPL2's screen values (2.2 = a gamma 2.2 monitor, 2.4 = BT.1886 for dark rooms, higher: deeper shadows on panels with raised blacks).</summary>
        public float hdrShadows = HplHdr.DefaultShadows;
        /// <summary>Game option "Commentary" (user config Game ShowCommentary, off by default): developer commentary icons.</summary>
        public bool commentary;
        /// <summary>Technical map testing panel on the physical grave/Ё key. Global, outside profiles.</summary>
        public bool debugPanel;
        /// <summary>Darkness by the visible light with the enhanced lighting (Player.PlayerLightLevel). Global, outside profiles.</summary>
        public bool visibleDarkness = true;

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
            s.ReadProfile();
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
            // Gamma is global: the profile part is not touched.
            s.Validate();
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(s));
            PlayerPrefs.Save();
        }

        /// <summary>The options of user_settings.cfg (Game, Input) kept in the profile folder.</summary>
        [Serializable]
        sealed class ProfilePart
        {
            public float mouseSensitivity = 1f;
            public bool invertMouse, smoothMouse = true, subtitles = true, hints = true, crosshair = true, commentary;
        }

        public const string ProfileFile = "user_settings.json";

        /// <summary>The profile's options over the global ones (nothing without a profile or its file).</summary>
        void ReadProfile()
        {
            var p = new ProfilePart();
            // A profile without the file (just created) starts with the defaults (default_user_settings.cfg).
            if (!HplProfiles.ReadJson(ProfileFile, p, out bool missing) && !missing) return;
            mouseSensitivity = p.mouseSensitivity; invertMouse = p.invertMouse; smoothMouse = p.smoothMouse;
            subtitles = p.subtitles; hints = p.hints; crosshair = p.crosshair; commentary = p.commentary;
        }

        /// <summary>The first profile takes over the options chosen before profiles existed.</summary>
        public static void CopyGlobalToProfile(string profile)
        {
            var s = new HplUserSettings();
            try
            {
                if (PlayerPrefs.HasKey(PrefsKey)) JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(PrefsKey), s);
            }
            catch (ArgumentException) { }
            s.Validate();
            HplProfiles.WriteJson(profile, ProfileFile, s.ProfileValues());
        }

        ProfilePart ProfileValues() => new() {
            mouseSensitivity = mouseSensitivity, invertMouse = invertMouse, smoothMouse = smoothMouse,
            subtitles = subtitles, hints = hints, crosshair = crosshair, commentary = commentary,
        };

        void WriteProfile() => HplProfiles.WriteJson(ProfileFile, new ProfilePart {
            mouseSensitivity = mouseSensitivity, invertMouse = invertMouse, smoothMouse = smoothMouse,
            subtitles = subtitles, hints = hints, crosshair = crosshair, commentary = commentary,
        });

        public HplUserSettings Copy() => (HplUserSettings)MemberwiseClone();

        public void Validate()
        {
            hdrMode = Mathf.Clamp(hdrMode, 0, 2);
            hdrPeak = HplHdr.Finite(hdrPeak, 1000, HplHdr.MinPeak, HplHdr.MaxPeak);
            hdrPaperWhite = HplHdr.Finite(hdrPaperWhite, 160, HplHdr.MinWhite, HplHdr.MaxWhite);
            hdrBlack = HplHdr.Finite(hdrBlack, 0, 0, HplHdr.MaxBlack);
            hdrShadows = HplHdr.Finite(hdrShadows, HplHdr.DefaultShadows, HplHdr.MinShadows, HplHdr.MaxShadows);
            mouseSensitivity = float.IsNaN(mouseSensitivity) || float.IsInfinity(mouseSensitivity)
                ? 1f : Mathf.Clamp(mouseSensitivity, 0.2f, 5f);
            masterVolume = float.IsNaN(masterVolume) || float.IsInfinity(masterVolume)
                ? 1f : Mathf.Clamp01(masterVolume);
            gamma = float.IsNaN(gamma) || float.IsInfinity(gamma) || gamma <= 0f ? 1f : Mathf.Clamp(gamma, MinGamma, MaxGamma);
            if (screenMode < 0 || screenMode > 2) screenMode = fullscreen ? 1 : 2;
            fullscreen = screenMode != 2;
            if (resolutionWidth <= 0 || resolutionHeight <= 0) resolutionWidth = resolutionHeight = 0;
            if (Array.IndexOf(HplGraphics.FpsLimits, fpsLimit) < 0) fpsLimit = 0;
            lighting = lighting == 1 && SystemInfo.supportsRayTracing ? 1 : 0;
            textures = Mathf.Clamp(textures, 0, 1);
            lightingSet = Mathf.Clamp(lightingSet, 0, 1);
            dlssMode = Mathf.Clamp(dlssMode, 0, (int)HplDlssMode.UltraPerformance);
            dlssPreset = HplDlss.ValidPreset(dlssPreset);
            rtShadows = Mathf.Clamp(rtShadows, 0, HplRayTracing.ShadowLights.Length - 1);
            rtAo = Mathf.Clamp(rtAo, 0, HplRayTracing.AoLevels - 1);
            rtReflections = Mathf.Clamp(rtReflections, 0, HplRayTracing.ReflectionLevels - 1);
            rtGi = Mathf.Clamp(rtGi, 0, HplRayTracing.GiLevels - 1);
            ssgi = Mathf.Clamp(ssgi, 0, HplRayTracing.SsgiLevels - 1);
            rtPathTracing = Mathf.Clamp(rtPathTracing, 0, HplRayTracing.PathTracingSamples.Length - 1);
            textureQuality = Mathf.Clamp(textureQuality, 0, 2);
            if (Array.IndexOf(HplGraphics.TextureMemoryBudgetsGb, textureMemoryBudgetGb) < 0) textureMemoryBudgetGb = 2;
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
            settings.ReadProfile();
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
            Player.PlayerLightLevel.VisibleChosen = visibleDarkness;
            map.player?.LightLevel?.Refresh();
        }

        public void Save()
        {
            Validate();
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            WriteProfile();
        }
    }
}
