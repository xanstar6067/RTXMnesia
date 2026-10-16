using System.Collections;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// New game and return to the title screen (cLuxMainMenu PressStartGame / exit to main menu): the fade
    /// (MainFadeOutTimeSlow with the camera zoom and ZoomSound for a new game, MainFadeOutTimeFast back to the
    /// title), then an asynchronous load that replaces the scene. Game data is reset only right before activation.
    /// </summary>
    public sealed class HplSceneTransition
    {
        public bool IsTransitioning { get; private set; }

        readonly HplGameUi _ui;
        readonly HplMap _map;

        public HplSceneTransition(HplGameUi ui)
        {
            _ui = ui;
            _map = ui.Map;
        }

        /// <summary>False (with an error in the log) when the scene is not in the build: a setup error, not a game state.</summary>
        public bool Begin(string path, bool newGame)
        {
            if (IsTransitioning) return false;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError("[RTXMnesia] Scene unavailable in build: " + path + " (run Import > Main menu and build scenes)");
                return false;
            }
            IsTransitioning = true;
            _ui.StartCoroutine(Load(path, newGame));
            return true;
        }

        IEnumerator Load(string path, bool newGame)
        {
            var config = _map.menuConfig;
            float fadeTime = config != null ? config.GetFloat("Main", newGame ? "MainFadeOutTimeSlow" : "MainFadeOutTimeFast", newGame ? 3 : 0.3f) : newGame ? 3 : 0.3f;
            bool zoom = newGame && _ui.IsTitleScreen;
            var camera = _map.PlayerCamera;
            float fov = camera != null ? camera.fieldOfView : 70;
            float zoomedFov = config != null ? config.GetFloat("Main", "BGCamera_ZoomedFOV", 40) : 40;
            if (zoom) _map.sound?.PlayGui(config != null ? config.GetString("Main", "ZoomSound", "ui_zoom.ogg") : "ui_zoom.ogg", false, 1);
            for (float elapsed = 0; elapsed < fadeTime; elapsed += Time.unscaledDeltaTime)
            {
                float alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeTime));
                if (zoom && camera != null) camera.fieldOfView = Mathf.Lerp(fov, zoomedFov, alpha);
                _ui.RefreshLoading(alpha);
                yield return null;
            }
            _ui.RefreshLoading(1);
            Cursor.visible = false;
            _map.sound?.SetMenuPaused(true);
            // Both destinations replace this world. Keep it frozen while loading, and release time only
            // immediately before activation so the new scene's Awake / Start run with a valid clock.
            _map.enabled = false;
            Time.timeScale = 0;
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single); }
            catch (System.Exception exception) { Debug.LogException(exception); }
            if (operation == null)
            {
                _map.enabled = true;
                _map.sound?.SetMenuPaused(!_ui.IsTitleScreen);
                Time.timeScale = _ui.IsTitleScreen ? 1 : 0;
                IsTransitioning = false;
                _ui.ShowMenu();
                yield break;
            }
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f) yield return null;
            HplGame.Reset();
            _ui.PreviousTimeScale = 1;
            Time.timeScale = 1;
            operation.allowSceneActivation = true;
            // HplGameUi is destroyed when the destination activates; all old sound sources are released
            // by HplSoundHandler.OnDestroy. Preferences in PlayerPrefs are independent of the game reset.
        }
    }
}
