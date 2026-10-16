using RTXMnesia.ScriptApi;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Runtime UI owner. MainMenu's in-game pause is separate from SetPlayerActive / effect pauses.
    /// Runs before device input so neither opening nor closing a screen leaks a click into the world.
    /// Inventory keeps ambience / music running while pausing the current voice (LuxInventory::OnEnterContainer).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HplGameUi : MonoBehaviour
    {
        public const string MainMenuScene = "Assets/AmnesiaImported/Levels/main_menu/main/menu_bg.unity";
        public const string FirstLevelScene = "Assets/AmnesiaImported/Levels/maps/main/ch01/00_rainy_hall.unity";
        public bool IsTitleScreen { get; private set; }
        public bool IsTransitioning { get; private set; }
        public MenuControl PendingConfirmation { get; private set; } = MenuControl.ConfirmQuit;
        public string LoadError { get; private set; } = "";
        public HplPauseMenu Menu => _menu;
        public bool IsPaused { get; private set; }
        public bool ConfirmingQuit { get; private set; }
        public bool ShowingOptions { get; private set; }
        public float MenuOpacity { get; private set; }
        public int PauseGeneration { get; private set; }
        public HplUserSettings Settings { get; private set; }
        public HplUserSettings DraftSettings { get; private set; }
        public HplInventory Inventory { get; private set; }
        public bool InventoryOpen { get; private set; }
        public bool InventoryFromJournal { get; private set; }
        public HplJournal Journal { get; private set; }
        public bool JournalOpen { get; private set; }
        public bool ReadingUiOpen => InventoryOpen || JournalOpen;
        public bool IsClosing => _closing;
        /// <summary>Real render time excluding menu pauses, including pauses with no rendered frames.</summary>
        public double RenderTime => (IsPaused ? _pauseStartedAt : Time.realtimeSinceStartupAsDouble) - _pausedDuration;

        HplMap _map;
        HplPauseMenu _menu;
        float _previousTimeScale;
        CursorLockMode _previousCursorLock;
        bool _previousCursorVisible, _hudWasEnabled;
        int _selected;
        double _pauseStartedAt, _pausedDuration;
        int _optionsTab;
        bool _closing;
        MenuControl? _dragSlider;

        public void Init(HplMap map, bool titleScreen = false)
        {
            if (_map != null) return;
            _map = map;
            IsTitleScreen = titleScreen;
            Settings = HplUserSettings.Load(map);
            if (Application.isPlaying) Settings.Apply(map);
            _menu = new HplPauseMenu(map, transform, titleScreen);
            if (!titleScreen)
            {
                Inventory = new HplInventory(map);
                Journal = new HplJournal(map);
            }
            else
            {
                IsPaused = true; MenuOpacity = 0.01f;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = !_menu.HasCursor;
                ShowPauseMenu();
            }
        }

        void Update()
        {
            if (_map == null || _menu == null || IsTransitioning || (!IsTitleScreen && (_map.player == null || !_map.player.readDevices))) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (!IsPaused) Pause();
                else if (JournalOpen) Journal.ExitPressed(false);
                else if (InventoryOpen && Inventory.MessageActive) Inventory.DismissMessage();
                else if (ConfirmingQuit || ShowingOptions || LoadError != "") ShowPauseMenu();
                else if (!IsTitleScreen) _closing = true;
                return;
            }
            if (keyboard != null && (keyboard.jKey.wasPressedThisFrame || keyboard.mKey.wasPressedThisFrame || keyboard.nKey.wasPressedThisFrame))
            {
                if (JournalOpen) Journal.ExitPressed(true);
                else if (InventoryOpen && keyboard.jKey.wasPressedThisFrame) OpenJournal(fromInventory: true);
                else if (!IsPaused)
                {
                    if (keyboard.nKey.wasPressedThisFrame) OpenRecentText();
                    else OpenJournal(keyboard.mKey.wasPressedThisFrame ? JournalState.QuestLog : JournalState.Main);
                }
                return;
            }
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && JournalOpen) { Journal.ExitPressed(true); return; }
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && (!IsPaused || InventoryOpen))
            {
                if (InventoryOpen) CloseInventory(); else OpenInventory();
                return;
            }
            if (!IsPaused) return;
            float fadeTime = JournalOpen ? (_closing ? 1f / 3f : 0.5f) : _map.menuConfig != null ? _map.menuConfig.GetFloat(InventoryOpen ? "Inventory" : "Main", InventoryOpen ? (_closing ? "FadeOutTime" : "FadeInTime") : (_closing ? "MainFadeOutTimeFast" : "MainFadeInTime"), _closing ? 0.3f : 1f) : (_closing ? 0.3f : 1f);
            MenuOpacity = Mathf.MoveTowards(MenuOpacity, _closing ? 0f : 1f, Time.unscaledDeltaTime / Mathf.Max(0.001f, fadeTime));
            if (InventoryOpen && !_closing && MenuOpacity >= 1) InventoryFromJournal = false;
            if (_closing && MenuOpacity <= 0f) { FinishClosing(); return; }

            if (JournalOpen)
            {
                _map.sound?.StepMenu(Time.unscaledDeltaTime);
                Journal.Tick(Time.unscaledDeltaTime);
                UpdateJournalInput(keyboard);
                return;
            }

            if (InventoryOpen)
            {
                _map.sound?.StepMenu(Time.unscaledDeltaTime);
                Inventory.Tick(Time.unscaledDeltaTime);
                UpdateInventoryInput(keyboard);
                return;
            }

            // Keep the UI cursor available even if the editor or a focus transition relocks it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !_menu.HasCursor;
            var mouse = Mouse.current;
            Refresh();
            if (_closing) return;
            if (mouse != null && ShowingOptions)
            {
                if (mouse.leftButton.wasPressedThisFrame && _menu.TrySlider(mouse.position.ReadValue(), out var slider)) _dragSlider = slider;
                if (_dragSlider.HasValue && mouse.leftButton.isPressed)
                {
                    float value = _menu.SliderValue(_dragSlider.Value, mouse.position.ReadValue());
                    if (_dragSlider == MenuControl.VolumeSlider) DraftSettings.masterVolume = Mathf.Round(value * 100) / 100f;
                    else DraftSettings.mouseSensitivity = Mathf.Round((0.2f + value * 4.8f) * 10) / 10f;
                }
                if (!mouse.leftButton.isPressed) _dragSlider = null;
            }
            int hovered = mouse != null ? _menu.HitTest(mouse.position.ReadValue()) : -1;
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0f && hovered >= 0) _selected = hovered;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame) _selected = (_selected + _menu.ButtonCount - 1) % _menu.ButtonCount;
                if (keyboard.downArrowKey.wasPressedThisFrame) _selected = (_selected + 1) % _menu.ButtonCount;
                if (keyboard.tabKey.wasPressedThisFrame) _selected = (_selected + _menu.ButtonCount + (keyboard.shiftKey.isPressed ? -1 : 1)) % _menu.ButtonCount;
                if (ShowingOptions && keyboard.leftArrowKey.wasPressedThisFrame) AdjustSelected(-1);
                if (ShowingOptions && keyboard.rightArrowKey.wasPressedThisFrame) AdjustSelected(1);
            }
            Refresh();

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && hovered >= 0) Activate(hovered);
            else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Activate(_selected);
        }

        public void Pause() { if (!IsTitleScreen) EnterScreen(false); }

        bool CanOpenPlayerMenu() => _map?.player != null && _map.player.active && !_map.player.IsDead &&
            _map.Script?.InventoryDisabled != true && _map.player.States?.CurrentType != Player.PlayerStateType.Ladder;

        public bool OpenJournal(JournalState state = JournalState.Main, bool fromInventory = false, bool force = false, bool instantExit = false)
        {
            if (JournalOpen) return true;
            if (_map?.player == null || (IsPaused && !InventoryOpen) || (!force && !CanOpenPlayerMenu())) return false;
            bool returnToInventory = InventoryOpen && fromInventory;
            if (InventoryOpen)
            {
                Inventory.Close(); InventoryOpen = false; JournalOpen = true;
                _closing = false; MenuOpacity = 0.01f;
            }
            else EnterScreen(false, true);
            if (_map.player.States?.CurrentType == Player.PlayerStateType.UseItem) _map.player.States.ChangeState(Player.PlayerStateType.Normal);
            Journal.Open(returnToInventory, instantExit || state == JournalState.QuestLog, state);
            Journal.View.Refresh(MenuOpacity, Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
            return true;
        }
        public bool OpenJournalEntry(HplGame.JournalEntry entry, bool narrated, bool instantExit)
        {
            if (entry == null || !OpenJournal(fromInventory: InventoryOpen, force: true, instantExit: instantExit)) return false;
            Journal.OpenEntry(entry, narrated);
            return true;
        }
        public bool OpenRecentText()
        {
            if (!CanOpenPlayerMenu() || HplGame.LastReadEntry == null) return false;
            return OpenJournalEntry(HplGame.LastReadEntry, false, true);
        }
        public void CloseJournal() { if (JournalOpen) _closing = true; }
        void FinishClosing()
        {
            if (JournalOpen && Journal.OpenedFromInventory)
            {
                Journal.Close(); JournalOpen = false; InventoryOpen = true;
                InventoryFromJournal = true;
                _closing = false; MenuOpacity = 0.01f;
                Inventory.Open();
                Inventory.View.Refresh(MenuOpacity, Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
            }
            else Resume();
        }

        void UpdateJournalInput(Keyboard keyboard)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !Journal.View.HasCursor;
            var mouse = Mouse.current;
            Vector2 position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            if (!_closing)
            {
                if (keyboard != null)
                {
                    if (keyboard.upArrowKey.wasPressedThisFrame) Journal.Navigate(-1);
                    if (keyboard.downArrowKey.wasPressedThisFrame) Journal.Navigate(1);
                    if (keyboard.leftArrowKey.wasPressedThisFrame) Journal.TurnPage(-1);
                    if (keyboard.rightArrowKey.wasPressedThisFrame) { if (Journal.IsNarrated) Journal.ExitPressed(false); else Journal.TurnPage(1); }
                    if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    { if (Journal.IsNarrated) Journal.ExitPressed(false); else Journal.View.Activate(Journal.Selected); }
                }
                if (mouse != null)
                {
                    int hit = Journal.View.HitTest(position);
                    if (mouse.delta.ReadValue().sqrMagnitude > 0) Journal.Select(hit);
                    if (mouse.leftButton.wasPressedThisFrame) { if (Journal.IsNarrated) Journal.ExitPressed(false); else Journal.View.Activate(hit); }
                    if (mouse.rightButton.wasPressedThisFrame) Journal.ExitPressed(false);
                    float scroll = mouse.scroll.ReadValue().y;
                    if (scroll != 0) Journal.TurnPage(scroll < 0 ? 1 : -1);
                }
            }
            Journal.View.Refresh(MenuOpacity, position);
        }

        public bool OpenInventory()
        {
            if (IsPaused || _map?.player == null || !_map.player.active || _map.player.IsDead ||
                _map.Script?.InventoryDisabled == true || _map.player.States?.CurrentType == Player.PlayerStateType.Ladder) return false;
            EnterScreen(true);
            return true;
        }

        public void CloseInventory()
        {
            if (!InventoryOpen) return;
            if (Inventory.MessageActive) Inventory.DismissMessage();
            else _closing = true;
        }

        void UpdateInventoryInput(Keyboard keyboard)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !Inventory.View.HasCursor;
            var mouse = Mouse.current;
            Vector2 position = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            if (!_closing)
            {
                if (keyboard != null)
                {
                    if (keyboard.leftArrowKey.wasPressedThisFrame) Inventory.Navigate(-1, 0);
                    if (keyboard.rightArrowKey.wasPressedThisFrame) Inventory.Navigate(1, 0);
                    if (keyboard.upArrowKey.wasPressedThisFrame) Inventory.Navigate(0, -1);
                    if (keyboard.downArrowKey.wasPressedThisFrame) Inventory.Navigate(0, 1);
                    if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Inventory.UseSelected();
                }
                if (mouse != null)
                {
                    int hit = Inventory.View.HitTest(position);
                    if (mouse.delta.ReadValue().sqrMagnitude > 0) Inventory.Select(hit);
                    if (mouse.leftButton.wasPressedThisFrame) Inventory.PointerDown(hit, Inventory.View.VirtualPoint(position));
                    if (mouse.leftButton.wasReleasedThisFrame) Inventory.PointerUp(hit);
                }
            }
            Inventory.View.Refresh(MenuOpacity, position);
        }

        void EnterScreen(bool inventory, bool journal = false)
        {
            if (IsPaused || _map == null || _menu == null) return;
            _previousTimeScale = Time.timeScale;
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _hudWasEnabled = _map.hud != null && _map.hud.Canvas != null && _map.hud.Canvas.enabled;
            _pauseStartedAt = Time.realtimeSinceStartupAsDouble;
            IsPaused = true;
            InventoryOpen = inventory;
            InventoryFromJournal = false;
            JournalOpen = journal;
            PauseGeneration++;
            MenuOpacity = 0.01f;
            _closing = false;
            _dragSlider = null;
            _map.player?.SetUiInputBlocked(true);
            if (inventory || journal)
            {
                _map.PhysicsWorld?.FadeOutLoopSounds(1f);
                _map.effects?.PlayVoice.SetInventoryPaused(true);
                _map.sound?.SetInventoryActive(true);
            }
            else _map.sound?.SetMenuPaused(true);
            // Stops Unity's automatic physics too; HplMap also guards explicit Step calls.
            Time.timeScale = 0f;
            if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = journal ? !Journal.View.HasCursor : inventory ? !Inventory.View.HasCursor : !_menu.HasCursor;
            if (inventory)
            {
                Inventory.Open();
                Inventory.View.Refresh(MenuOpacity, Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero);
            }
            else if (!journal) ShowPauseMenu();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            if (IsTitleScreen) { IsPaused = false; _menu?.SetVisible(false); return; }
            if (ReadingUiOpen)
            {
                _map.effects?.PlayVoice.SetInventoryPaused(false);
                _map.sound?.SetInventoryActive(false);
                Inventory?.Close();
                Journal?.Close();
            }
            _pausedDuration += Time.realtimeSinceStartupAsDouble - _pauseStartedAt;
            IsPaused = false;
            InventoryOpen = false;
            InventoryFromJournal = false;
            JournalOpen = false;
            ConfirmingQuit = false;
            ShowingOptions = false;
            DraftSettings = null;
            _dragSlider = null;
            MenuOpacity = 0f;
            _closing = false;
            Time.timeScale = _previousTimeScale;
            if (_map != null)
            {
                if (_map.player != null) _map.player.SetUiInputBlocked(false);
                if (_map.sound != null) _map.sound.SetMenuPaused(false);
                if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = _hudWasEnabled;
            }
            _menu?.SetVisible(false);
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
        }

        void ShowPauseMenu()
        {
            LoadError = "";
            ConfirmingQuit = false;
            ShowingOptions = false;
            DraftSettings = null;
            _dragSlider = null;
            _selected = 0;
            _menu.SetVisible(true);
            Refresh();
        }

        void Refresh() => _menu.Refresh(ConfirmingQuit, ShowingOptions, _optionsTab, DraftSettings ?? Settings,
            _selected, MenuOpacity, Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero, PendingConfirmation, LoadError);

        public void OpenOptions(int tab = -1)
        {
            if (!IsPaused || ReadingUiOpen) return;
            if (tab >= 0) _optionsTab = Mathf.Clamp(tab, 0, 3);
            ShowingOptions = true;
            _dragSlider = null;
            DraftSettings = Settings.Copy();
            _selected = _optionsTab;
            Refresh();
        }

        void AdjustSelected(int direction)
        {
            var control = _menu.ControlAt(_selected);
            if (control >= MenuControl.GameTab && control <= MenuControl.InputTab)
            {
                _optionsTab = (_optionsTab + 4 + direction) % 4;
                _selected = _optionsTab;
                _dragSlider = null;
                Refresh();
            }
            else if (control >= MenuControl.Subtitles && control <= MenuControl.SmoothMouse) Activate(_selected, direction);
        }

        void Activate(int index, int direction = 0)
        {
            if (index < 0 || index >= _menu.ButtonCount) return;
            var control = _menu.ControlAt(index);
            switch (control)
            {
                case MenuControl.Resume: _closing = true; break;
                case MenuControl.Options: OpenOptions(); break;
                case MenuControl.Quit:
                    PendingConfirmation = MenuControl.ConfirmQuit;
                    ConfirmingQuit = true;
                    _selected = 1;
                    break;
                case MenuControl.NewGame:
                case MenuControl.ReturnTitle:
                    PendingConfirmation = control == MenuControl.NewGame ? MenuControl.ConfirmNewGame : MenuControl.ConfirmReturnTitle;
                    ConfirmingQuit = true; _selected = 1;
                    break;
                case MenuControl.ConfirmNewGame: BeginSceneLoad(FirstLevelScene, true); break;
                case MenuControl.ConfirmReturnTitle: BeginSceneLoad(MainMenuScene, false); break;
                case MenuControl.Cancel: ShowPauseMenu(); break;
                case MenuControl.Apply:
                    Settings = DraftSettings.Copy();
                    Settings.Apply(_map);
                    Settings.Save();
                    ShowPauseMenu();
                    break;
                case MenuControl.GameTab: case MenuControl.GraphicsTab: case MenuControl.SoundTab: case MenuControl.InputTab:
                    _optionsTab = (int)control - (int)MenuControl.GameTab;
                    _selected = _optionsTab;
                    break;
                case MenuControl.Subtitles: DraftSettings.subtitles = !DraftSettings.subtitles; break;
                case MenuControl.Hints: DraftSettings.hints = !DraftSettings.hints; break;
                case MenuControl.Crosshair: DraftSettings.crosshair = !DraftSettings.crosshair; break;
                case MenuControl.Fullscreen: DraftSettings.fullscreen = !DraftSettings.fullscreen; break;
                case MenuControl.VSync: DraftSettings.vSync = !DraftSettings.vSync; break;
                case MenuControl.InvertMouse: DraftSettings.invertMouse = !DraftSettings.invertMouse; break;
                case MenuControl.SmoothMouse: DraftSettings.smoothMouse = !DraftSettings.smoothMouse; break;
                case MenuControl.VolumeLess: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * (direction == 0 ? -1 : direction)); break;
                case MenuControl.VolumeMore: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * (direction == 0 ? 1 : direction)); break;
                case MenuControl.SensitivityLess: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * (direction == 0 ? -1 : direction), 0.2f, 5f); break;
                case MenuControl.SensitivityMore: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * (direction == 0 ? 1 : direction), 0.2f, 5f); break;
                case MenuControl.ConfirmQuit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
            if (!IsTransitioning) Refresh();
        }

        /// <summary>Routes a visible control through the same path as a mouse / keyboard action (also for checks).</summary>
        public bool Choose(MenuControl control)
        {
            if (IsTransitioning || _menu == null) return false;
            for (int i = 0; i < _menu.ButtonCount; i++)
                if (_menu.ControlAt(i) == control) { Activate(i); return true; }
            return false;
        }

        void BeginSceneLoad(string path, bool newGame)
        {
            if (IsTransitioning) return;
            if (!Application.CanStreamedLevelBeLoaded(path))
            {
                Debug.LogError("[RTXMnesia] Scene unavailable in build: " + path);
                LoadError = newGame ? "Не удалось запустить игру." : "Не удалось открыть главное меню.";
                ConfirmingQuit = false;
                return;
            }
            IsTransitioning = true;
            StartCoroutine(LoadScene(path, newGame));
        }

        IEnumerator LoadScene(string path, bool newGame)
        {
            float fadeTime = _map.menuConfig != null ? _map.menuConfig.GetFloat("Main", newGame ? "MainFadeOutTimeSlow" : "MainFadeOutTimeFast", newGame ? 3 : 0.3f) : newGame ? 3 : 0.3f;
            var camera = _map.PlayerCamera;
            float fov = camera != null ? camera.fieldOfView : 70;
            float zoom = _map.menuConfig != null ? _map.menuConfig.GetFloat("Main", "BGCamera_ZoomedFOV", 40) : 40;
            if (newGame && IsTitleScreen) _map.sound?.PlayGui(_map.menuConfig.GetString("Main", "ZoomSound", "ui_zoom.ogg"), false, 1);
            for (float elapsed = 0; elapsed < fadeTime; elapsed += Time.unscaledDeltaTime)
            {
                float alpha = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeTime));
                if (newGame && IsTitleScreen && camera != null) camera.fieldOfView = Mathf.Lerp(fov, zoom, alpha);
                _menu.RefreshLoading(alpha);
                yield return null;
            }
            _menu.RefreshLoading(1);
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
                _map.enabled = true; _map.sound?.SetMenuPaused(!IsTitleScreen);
                Time.timeScale = IsTitleScreen ? 1 : 0;
                IsTransitioning = false; ConfirmingQuit = false;
                LoadError = "Не удалось открыть экран."; Refresh(); yield break;
            }
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f) yield return null;
            HplGame.Reset();
            _previousTimeScale = 1;
            Time.timeScale = 1;
            operation.allowSceneActivation = true;
            // This component is destroyed when the destination activates; all old sound sources are released
            // by HplSoundHandler.OnDestroy. Preferences in PlayerPrefs are independent of the game reset.
        }

        void OnApplicationFocus(bool focused)
        {
            if (IsTitleScreen) { _map?.sound?.SetMenuPaused(!focused); return; }
            if (ReadingUiOpen)
            {
                // The inventory stays open, but an unfocused application must not keep playing its ambience.
                _map.sound?.SetMenuPaused(!focused);
                return;
            }
            // Focus returning never resumes gameplay without a deliberate action.
            if (!focused && _map != null && _map.player != null && _map.player.readDevices) Pause();
        }

        void OnDisable()
        {
            // Includes stopping Play, disabling the level and unloading the scene.
            Resume();
        }

        void OnDestroy()
        {
            _menu?.Destroy();
            Inventory?.View.Destroy();
            Journal?.View.Destroy();
        }
    }
}
