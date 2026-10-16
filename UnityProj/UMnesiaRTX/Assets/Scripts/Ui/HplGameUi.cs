using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Ui
{
    public enum UiScreen { Menu, Inventory, Journal }

    /// <summary>
    /// Runtime UI owner (cLuxInputHandler's container switching): the shared pause of the world, the screen fade and
    /// the routing of keys to the open screen. The screens themselves live in <see cref="HplMenuScreen"/> (pause /
    /// title menu, options, confirmations), <see cref="HplReadingScreens"/> (inventory, journal) and
    /// <see cref="HplSceneTransition"/> (scene loads). MainMenu's in-game pause is separate from SetPlayerActive /
    /// effect pauses. Runs before device input so neither opening nor closing a screen leaks a click into the world.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HplGameUi : MonoBehaviour
    {
        public const string MainMenuScene = "Assets/AmnesiaImported/Levels/main_menu/main/menu_bg.unity";
        public const string FirstLevelScene = "Assets/AmnesiaImported/Levels/maps/main/ch01/00_rainy_hall.unity";

        public bool IsTitleScreen { get; private set; }
        public bool IsPaused { get; private set; }
        public float MenuOpacity { get; private set; }
        public int PauseGeneration { get; private set; }
        public bool IsClosing { get; private set; }
        public bool InventoryOpen { get; internal set; }
        public bool InventoryFromJournal { get; internal set; }
        public bool JournalOpen { get; internal set; }
        public bool ReadingUiOpen => InventoryOpen || JournalOpen;
        public HplUserSettings Settings { get; internal set; }
        /// <summary>Real render time excluding menu pauses, including pauses with no rendered frames.</summary>
        public double RenderTime => (IsPaused ? _pauseStartedAt : Time.realtimeSinceStartupAsDouble) - _pausedDuration;

        public HplPauseMenu Menu { get; private set; }
        public HplInventory Inventory => _reading?.Inventory;
        public HplJournal Journal => _reading?.Journal;

        // Menu screen state.
        public MenuControl PendingConfirmation => _menuScreen?.PendingConfirmation ?? MenuControl.ConfirmQuit;
        public bool ConfirmingQuit => _menuScreen != null && _menuScreen.ConfirmingQuit;
        public bool ShowingOptions => _menuScreen != null && _menuScreen.ShowingOptions;
        public HplUserSettings DraftSettings => _menuScreen?.DraftSettings;
        public bool IsTransitioning => _transition != null && _transition.IsTransitioning;

        internal HplMap Map => _map;
        /// <summary>The timeScale restored by <see cref="Resume"/>; a scene load sets it to 1 before activation.</summary>
        internal float PreviousTimeScale { get; set; }
        internal static Vector2 PointerPosition => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

        HplMap _map;
        HplMenuScreen _menuScreen;
        HplReadingScreens _reading;
        HplSceneTransition _transition;
        CursorLockMode _previousCursorLock;
        bool _previousCursorVisible, _hudWasEnabled;
        double _pauseStartedAt, _pausedDuration;

        public void Init(HplMap map, bool titleScreen = false)
        {
            if (_map != null) return;
            _map = map;
            IsTitleScreen = titleScreen;
            Settings = HplUserSettings.Load(map);
            if (Application.isPlaying) Settings.Apply(map);
            Menu = new HplPauseMenu(map, transform, titleScreen);
            _menuScreen = new HplMenuScreen(this, Menu);
            _transition = new HplSceneTransition(this);
            if (!titleScreen) _reading = new HplReadingScreens(this);
            else
            {
                IsPaused = true; MenuOpacity = 0.01f;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = !Menu.HasCursor;
                _menuScreen.Show();
            }
        }

        void Update()
        {
            // The load screen between maps takes the input (cLuxInputHandler LoadScreen state).
            if (_map == null || Menu == null || IsTransitioning || HplLoadScreen.Active) return;
            if (!IsTitleScreen && (_map.player == null || !_map.player.readDevices)) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && RouteKeys(keyboard)) return;
            if (!IsPaused) return;

            MenuOpacity = Mathf.MoveTowards(MenuOpacity, IsClosing ? 0f : 1f, Time.unscaledDeltaTime / Mathf.Max(0.001f, FadeTime()));
            if (InventoryOpen && !IsClosing && MenuOpacity >= 1) InventoryFromJournal = false;
            if (IsClosing && MenuOpacity <= 0f) { FinishClosing(); return; }

            if (ReadingUiOpen) _reading.Update(keyboard);
            else _menuScreen.Update(keyboard);
        }

        /// <summary>Esc, journal keys and Tab (LuxInputHandler.cpp UpdateGameInput / UpdateInventoryInput / UpdateJournalInput).</summary>
        bool RouteKeys(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (!IsPaused) Pause();
                else if (JournalOpen) Journal.ExitPressed(false);
                else if (InventoryOpen) _reading.CloseInventory();
                else _menuScreen.Escape();
                return true;
            }
            bool journalKey = keyboard.jKey.wasPressedThisFrame, questKey = keyboard.mKey.wasPressedThisFrame, recentKey = keyboard.nKey.wasPressedThisFrame;
            if (journalKey || questKey || recentKey)
            {
                if (JournalOpen) Journal.ExitPressed(true);
                else if (InventoryOpen && journalKey) _reading.OpenJournal(fromInventory: true);
                else if (!IsPaused)
                {
                    if (recentKey) _reading?.OpenRecentText();
                    else _reading?.OpenJournal(questKey ? JournalState.QuestLog : JournalState.Main);
                }
                return true;
            }
            if (keyboard.tabKey.wasPressedThisFrame && (JournalOpen || InventoryOpen || !IsPaused))
            {
                if (JournalOpen) Journal.ExitPressed(true);
                else if (InventoryOpen) _reading.CloseInventory();
                else _reading?.OpenInventory();
                return true;
            }
            return false;
        }

        /// <summary>Fade time of the open screen: journal 1/2 s in, 1/3 s out (cLuxJournal::Update); menu.cfg for the rest.</summary>
        float FadeTime()
        {
            if (JournalOpen) return IsClosing ? 1f / 3f : 0.5f;
            float fallback = IsClosing ? 0.3f : 1f;
            if (_map.menuConfig == null) return fallback;
            return InventoryOpen
                ? _map.menuConfig.GetFloat("Inventory", IsClosing ? "FadeOutTime" : "FadeInTime", fallback)
                : _map.menuConfig.GetFloat("Main", IsClosing ? "MainFadeOutTimeFast" : "MainFadeInTime", fallback);
        }

        // ------------------------------------------------------------------ screens

        public void Pause() { if (!IsTitleScreen) EnterScreen(UiScreen.Menu); }
        public void OpenOptions(int tab = -1) { if (IsPaused && !ReadingUiOpen) _menuScreen.OpenOptions(tab); }
        /// <summary>Routes a visible control through the same path as a mouse / keyboard action (also for checks).</summary>
        public bool Choose(MenuControl control) => !IsTransitioning && _menuScreen != null && _menuScreen.Choose(control);

        public bool OpenInventory() => _reading != null && _reading.OpenInventory();
        public void CloseInventory() => _reading?.CloseInventory();
        public bool OpenJournal(JournalState state = JournalState.Main, bool fromInventory = false, bool force = false, bool instantExit = false) =>
            _reading != null && _reading.OpenJournal(state, fromInventory, force, instantExit);
        public bool OpenJournalEntry(HplGame.JournalEntry entry, bool narrated, bool instantExit) =>
            _reading != null && _reading.OpenJournalEntry(entry, narrated, instantExit);
        public bool OpenRecentText() => _reading != null && _reading.OpenRecentText();
        public void CloseJournal() { if (JournalOpen) BeginClose(); }

        internal void BeginClose() => IsClosing = true;
        internal bool BeginSceneLoad(string path, bool newGame) => _transition.Begin(path, newGame);
        internal void ShowMenu() => _menuScreen.Show();
        internal void RefreshMenu() => _menuScreen.Refresh();
        internal void RefreshLoading(float alpha) => Menu.RefreshLoading(alpha);

        /// <summary>
        /// Pauses the world for a screen: timeScale 0, HUD hidden, UI input lock. The menu pauses all sound
        /// (cLuxMainMenu::OnEnterContainer); inventory and journal keep ambience / music, fade the physics loops and
        /// the world volume and pause the current voice (cLuxInventory / cLuxJournal::OnEnterContainer).
        /// </summary>
        internal void EnterScreen(UiScreen screen)
        {
            if (IsPaused || _map == null || Menu == null) return;
            PreviousTimeScale = Time.timeScale;
            _previousCursorLock = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _hudWasEnabled = _map.hud != null && _map.hud.Canvas != null && _map.hud.Canvas.enabled;
            _pauseStartedAt = Time.realtimeSinceStartupAsDouble;
            IsPaused = true;
            InventoryOpen = screen == UiScreen.Inventory;
            JournalOpen = screen == UiScreen.Journal;
            InventoryFromJournal = false;
            PauseGeneration++;
            MenuOpacity = 0.01f;
            IsClosing = false;
            _map.player?.SetUiInputBlocked(true);
            if (screen == UiScreen.Menu) _map.sound?.SetMenuPaused(true);
            else
            {
                _map.PhysicsWorld?.FadeOutLoopSounds(1f);
                _map.effects?.PlayVoice.SetInventoryPaused(true);
                // cLuxInventory / cLuxJournal::OnEnterContainer: the terror loop stops.
                _map.player?.StopTerrorSound();
                _map.sound?.SetInventoryActive(true);
            }
            // Stops Unity's automatic physics too; HplMap also guards explicit Step calls.
            Time.timeScale = 0f;
            if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = screen == UiScreen.Menu ? !Menu.HasCursor : !_reading.HasCursor(screen);
            if (screen == UiScreen.Menu) _menuScreen.Show();
        }

        /// <summary>Switches between inventory and journal inside the same pause, restarting the fade.</summary>
        internal void SwitchReadingScreen(UiScreen screen)
        {
            InventoryOpen = screen == UiScreen.Inventory;
            JournalOpen = screen == UiScreen.Journal;
            IsClosing = false;
            MenuOpacity = 0.01f;
        }

        void FinishClosing()
        {
            if (JournalOpen && Journal.OpenedFromInventory) _reading.ReturnToInventory();
            else Resume();
        }

        public void Resume()
        {
            if (!IsPaused) return;
            if (IsTitleScreen) { IsPaused = false; Menu?.SetVisible(false); return; }
            if (ReadingUiOpen)
            {
                _map.effects?.PlayVoice.SetInventoryPaused(false);
                _map.sound?.SetInventoryActive(false);
                _reading.CloseViews();
            }
            _pausedDuration += Time.realtimeSinceStartupAsDouble - _pauseStartedAt;
            IsPaused = false;
            InventoryOpen = JournalOpen = InventoryFromJournal = false;
            _menuScreen.Reset();
            MenuOpacity = 0f;
            IsClosing = false;
            Time.timeScale = PreviousTimeScale;
            if (_map != null)
            {
                _map.player?.SetUiInputBlocked(false);
                _map.sound?.SetMenuPaused(false);
                if (_map.hud != null && _map.hud.Canvas != null) _map.hud.Canvas.enabled = _hudWasEnabled;
            }
            Menu?.SetVisible(false);
            Cursor.lockState = _previousCursorLock;
            Cursor.visible = _previousCursorVisible;
        }

        void OnApplicationFocus(bool focused)
        {
            if (IsTitleScreen || ReadingUiOpen)
            {
                // The title world and an open inventory / journal stay as they are, but an unfocused
                // application must not keep playing their ambience.
                _map?.sound?.SetMenuPaused(!focused);
                return;
            }
            // Focus returning never resumes gameplay without a deliberate action.
            if (!focused && _map != null && _map.player != null && _map.player.readDevices) Pause();
        }

        // Includes stopping Play, disabling the level and unloading the scene.
        void OnDisable() => Resume();

        void OnDestroy()
        {
            Menu?.Destroy();
            _reading?.DestroyViews();
        }
    }
}
