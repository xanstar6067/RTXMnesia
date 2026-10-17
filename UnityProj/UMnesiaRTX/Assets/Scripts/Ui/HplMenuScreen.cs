using System.Collections.Generic;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The pause / title menu of <see cref="HplGameUi"/> (cLuxMainMenu): the main list, confirmations and the options
    /// with their draft settings. Mouse, arrows / Tab and Enter / Space choose controls; <see cref="HplPauseMenu"/> draws.
    /// </summary>
    public sealed class HplMenuScreen
    {
        public MenuControl PendingConfirmation { get; private set; } = MenuControl.ConfirmQuit;
        public bool ConfirmingQuit { get; private set; }
        public bool ShowingOptions { get; private set; }
        /// <summary>The load game window (cLuxMainMenu_LoadGame) is open.</summary>
        public bool ShowingLoad { get; private set; }
        public IReadOnlyList<string> SaveFiles => _saveFiles;
        public int SelectedSave => _selectedSave;
        public HplUserSettings DraftSettings { get; private set; }
        /// <summary>The key config window (cLuxMainMenu_KeyConfig), opened from the input options; null: closed.</summary>
        public HplKeyConfig KeyConfig => _keyConfig;
        public bool ShowingKeyConfig => _keyConfig != null;
        /// <summary>A key config slot waits for an input: the keys go to it, not to the game UI.</summary>
        public bool CapturingInput => _keyConfig != null && _keyConfig.IsWaiting;

        readonly HplGameUi _ui;
        readonly HplMap _map;
        readonly HplPauseMenu _menu;
        int _selected, _optionsTab;
        readonly List<string> _saveFiles = new(), _saveLabels = new();
        int _selectedSave = -1;
        MenuControl? _dragSlider;
        HplKeyConfig _keyConfig;

        public HplMenuScreen(HplGameUi ui, HplPauseMenu menu)
        {
            _ui = ui;
            _map = ui.Map;
            _menu = menu;
        }

        /// <summary>Back to the main list (also after a confirmation, options or a failed scene load).</summary>
        public void Show()
        {
            Reset();
            _selected = 0;
            if (_ui.IsTitleScreen) _menu.HasSave = HplSaveHandler.SaveFileExists(_map.Campaign);
            _menu.SetVisible(true);
            Refresh();
        }

        public void Reset()
        {
            ConfirmingQuit = false;
            ShowingOptions = false;
            ShowingLoad = false;
            DraftSettings = null;
            _dragSlider = null;
            _keyConfig = null;
        }

        /// <summary>cLuxMainMenu::ExitPressed: closes a window, or the menu itself in game.</summary>
        public void Escape()
        {
            if (ShowingKeyConfig)
            {
                // cLuxInputMenuEntry: Esc cancels the waiting input; the window's pop up closes; else back to options.
                if (_keyConfig.IsWaiting) _keyConfig.CancelWaiting();
                else if (ConfirmingQuit) ConfirmingQuit = false;
                else CloseKeyConfig();
                return;
            }
            // A confirmation of the load window goes back to the window, as its pop up does.
            if (ConfirmingQuit && PendingConfirmation == MenuControl.ConfirmLoad) OpenLoadGame(_selectedSave);
            else if (ConfirmingQuit || ShowingOptions || ShowingLoad) Show();
            else if (!_ui.IsTitleScreen) _ui.BeginClose();
        }

        public void Refresh() => _menu.Refresh(ConfirmingQuit, ShowingOptions, _optionsTab, DraftSettings ?? _ui.Settings,
            _selected, _ui.MenuOpacity, HplGameUi.PointerPosition, PendingConfirmation, ShowingLoad ? _saveLabels : null, _selectedSave,
            _keyConfig);

        /// <summary>cLuxMainMenu_LoadGame::OnSetActive → PopulateSavedGameList.</summary>
        public void OpenLoadGame(int select = -1)
        {
            Reset();
            ShowingLoad = true;
            _saveFiles.Clear();
            _saveLabels.Clear();
            foreach (string file in HplSaveHandler.ListSaves(_map.Campaign))
            {
                _saveFiles.Add(file);
                _saveLabels.Add(HplSaveHandler.GetProperSaveName(file, (cat, entry) => _map.hud.TryTranslate(cat, entry, out var text) ? text : entry));
            }
            _selectedSave = select >= 0 && select < _saveFiles.Count ? select : -1;
            _selected = 0;
            Refresh();
        }

        public void Update(Keyboard keyboard)
        {
            // Keep the UI cursor available even if the editor or a focus transition relocks it.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !_menu.HasCursor;
            var mouse = Mouse.current;
            Refresh();
            if (_ui.IsClosing) return;
            // A slot waiting for an input takes the next key or mouse button (not the click that started it).
            if (CapturingInput)
            {
                if (Time.frameCount > _keyConfig.WaitingFrame)
                {
                    string input = HplKeyBindings.PressedInput();
                    if (input != null) _keyConfig.Assign(input);
                }
                Refresh();
                return;
            }
            if (mouse != null && ShowingOptions && !ShowingKeyConfig) DragSlider(mouse);
            int hovered = mouse != null ? _menu.HitTest(mouse.position.ReadValue()) : -1;
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0f && hovered >= 0) _selected = hovered;
            if (keyboard != null)
            {
                int count = _menu.ButtonCount;
                if (keyboard.upArrowKey.wasPressedThisFrame) _selected = (_selected + count - 1) % count;
                if (keyboard.downArrowKey.wasPressedThisFrame) _selected = (_selected + 1) % count;
                if (keyboard.tabKey.wasPressedThisFrame) _selected = (_selected + count + (keyboard.shiftKey.isPressed ? -1 : 1)) % count;
                if (ShowingOptions && keyboard.leftArrowKey.wasPressedThisFrame) AdjustSelected(-1);
                if (ShowingOptions && keyboard.rightArrowKey.wasPressedThisFrame) AdjustSelected(1);
                // Within the button count of the window (the key config rows change with the category).
                if (_selected >= _menu.ButtonCount) _selected = 0;
            }
            Refresh();

            if (mouse != null && mouse.leftButton.wasPressedThisFrame && hovered >= 0) Activate(hovered);
            else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Activate(_selected);
        }

        void DragSlider(Mouse mouse)
        {
            if (mouse.leftButton.wasPressedThisFrame && _menu.TrySlider(mouse.position.ReadValue(), out var slider)) _dragSlider = slider;
            if (_dragSlider.HasValue && mouse.leftButton.isPressed)
            {
                float value = _menu.SliderValue(_dragSlider.Value, mouse.position.ReadValue());
                if (_dragSlider == MenuControl.VolumeSlider) DraftSettings.masterVolume = Mathf.Round(value * 100) / 100f;
                else if (_dragSlider == MenuControl.GammaSlider)
                    DraftSettings.gamma = Mathf.Round((HplUserSettings.MinGamma + value * (HplUserSettings.MaxGamma - HplUserSettings.MinGamma)) * 20f) / 20f;
                else DraftSettings.mouseSensitivity = Mathf.Round((0.2f + value * 4.8f) * 10) / 10f;
            }
            if (!mouse.leftButton.isPressed) _dragSlider = null;
        }

        public void OpenOptions(int tab = -1)
        {
            if (tab >= 0) _optionsTab = Mathf.Clamp(tab, 0, 3);
            ShowingOptions = true;
            _dragSlider = null;
            DraftSettings = _ui.Settings.Copy();
            _selected = _optionsTab;
            Refresh();
        }

        public bool Choose(MenuControl control)
        {
            for (int i = 0; i < _menu.ButtonCount; i++)
                if (_menu.ControlAt(i) == control) { Activate(i); return true; }
            return false;
        }

        void AdjustSelected(int direction)
        {
            if (_selected < 0 || _selected >= _menu.ButtonCount) return;
            var control = _menu.ControlAt(_selected);
            if (control == MenuControl.KeyCategory)
            {
                Activate(_selected, direction);
                return;
            }
            if (control >= MenuControl.GameTab && control <= MenuControl.InputTab)
            {
                _optionsTab = (_optionsTab + 4 + direction) % 4;
                _selected = _optionsTab;
                _dragSlider = null;
                Refresh();
            }
            else if (control >= MenuControl.Subtitles && control <= MenuControl.SmoothMouse ||
                     control == MenuControl.GammaLess || control == MenuControl.GammaMore) Activate(_selected, direction);
        }

        void Activate(int index, int direction = 0)
        {
            if (index < 0 || index >= _menu.ButtonCount) return;
            var control = _menu.ControlAt(index);
            // Arrows on a < or > button move the value their way; a click moves it the button's way.
            int less = direction == 0 ? -1 : direction, more = direction == 0 ? 1 : direction;
            switch (control)
            {
                case MenuControl.Resume: _ui.BeginClose(); break;
                case MenuControl.Options: OpenOptions(); break;
                case MenuControl.Quit:
                case MenuControl.NewGame:
                case MenuControl.ReturnTitle:
                case MenuControl.Continue:
                case MenuControl.ExitAndSave:
                    PendingConfirmation = control switch
                    {
                        MenuControl.NewGame => MenuControl.ConfirmNewGame,
                        MenuControl.ReturnTitle => MenuControl.ConfirmReturnTitle,
                        MenuControl.Continue => MenuControl.ConfirmContinue,
                        MenuControl.ExitAndSave => MenuControl.ConfirmExitAndSave,
                        _ => MenuControl.ConfirmQuit,
                    };
                    ConfirmingQuit = true;
                    _selected = 1;
                    break;
                case MenuControl.ConfirmNewGame: LoadScene(_map.Campaign.FirstScene, true); break;
                case MenuControl.ConfirmReturnTitle: LoadScene(_map.Campaign.MenuScene, false); break;
                // eLuxMainMenuExit_ContinueGame → cLuxSaveHandler::AutoLoad: the newest save.
                case MenuControl.ConfirmContinue: LoadSave(HplSaveHandler.GetNewestSaveFile(_map.Campaign)); break;
                // eLuxMainMenuExit_QuitAndSave: auto save, then back to the title screen.
                case MenuControl.ConfirmExitAndSave:
                    HplSaveHandler.AutoSave(_map);
                    LoadScene(_map.Campaign.MenuScene, false);
                    break;
                case MenuControl.LoadGame: OpenLoadGame(); break;
                case MenuControl.LoadSlot:
                    int row = _menu.DataAt(index);
                    // A click on the selected row again loads it (the list's double click).
                    if (row == _selectedSave && direction == 0) goto case MenuControl.LoadOk;
                    _selectedSave = row;
                    break;
                case MenuControl.LoadOk:
                    // cLuxMainMenu_LoadGame::PressOK: nothing selected → the warning stays in the window.
                    if (_selectedSave < 0 || _selectedSave >= _saveFiles.Count) break;
                    ShowingLoad = false;
                    PendingConfirmation = MenuControl.ConfirmLoad;
                    ConfirmingQuit = true;
                    _selected = 1;
                    break;
                case MenuControl.ConfirmLoad: LoadSave(_saveFiles[_selectedSave]); break;
                case MenuControl.Cancel:
                    if (ShowingKeyConfig)
                    {
                        // A pop up of the key config window closes; the window's Cancel drops the draft.
                        if (ConfirmingQuit) ConfirmingQuit = false;
                        else CloseKeyConfig();
                    }
                    else if (PendingConfirmation == MenuControl.ConfirmLoad && ConfirmingQuit) OpenLoadGame(_selectedSave);
                    else Show();
                    break;
                case MenuControl.KeyConfig:
                    _keyConfig = new HplKeyConfig();
                    _keyConfig.Begin();
                    _selected = 0;
                    break;
                case MenuControl.KeyCategory:
                    _keyConfig.Category = (_keyConfig.Category + 3 + (direction < 0 ? -1 : 1)) % 3;
                    _selected = 0;
                    break;
                case MenuControl.KeySlot:
                {
                    int data = _menu.DataAt(index);
                    _keyConfig.StartWaiting(data / 2, data % 2, Time.frameCount);
                    break;
                }
                case MenuControl.KeyDefaults:
                case MenuControl.KeyOk when _keyConfig != null && _keyConfig.HasInvalidInputs:
                    // cLuxMainMenu_KeyConfig::PressSetDefault / PressOK with invalid inputs: a warning pop up first.
                    PendingConfirmation = control == MenuControl.KeyDefaults ? MenuControl.ConfirmKeyDefaults : MenuControl.ConfirmKeyInvalid;
                    ConfirmingQuit = true;
                    _selected = 0;
                    break;
                case MenuControl.ConfirmKeyDefaults:
                    _keyConfig.SetDefaults();
                    ConfirmingQuit = false;
                    _selected = 0;
                    break;
                case MenuControl.KeyOk:
                case MenuControl.ConfirmKeyInvalid:
                    _keyConfig.Apply();
                    ConfirmingQuit = false;
                    CloseKeyConfig();
                    break;
                case MenuControl.Apply:
                    _ui.Settings = DraftSettings.Copy();
                    _ui.Settings.Apply(_map);
                    _ui.Settings.Save();
                    Show();
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
                case MenuControl.VolumeLess: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * less); break;
                case MenuControl.VolumeMore: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * more); break;
                case MenuControl.SensitivityLess: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * less, 0.2f, 5f); break;
                case MenuControl.SensitivityMore: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * more, 0.2f, 5f); break;
                // cLuxMainMenu_Options: the gamma slider steps 0.05 between 0.3 and 2.
                case MenuControl.GammaLess: DraftSettings.gamma = Mathf.Clamp(DraftSettings.gamma + 0.05f * less, HplUserSettings.MinGamma, HplUserSettings.MaxGamma); break;
                case MenuControl.GammaMore: DraftSettings.gamma = Mathf.Clamp(DraftSettings.gamma + 0.05f * more, HplUserSettings.MinGamma, HplUserSettings.MaxGamma); break;
                case MenuControl.ConfirmQuit:
                    // cLuxMainMenu::OnQuit with a map loaded: auto save first (a dead player is not saved).
                    if (!_ui.IsTitleScreen && _map.player != null && !_map.player.IsDead) HplSaveHandler.AutoSave(_map);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
            if (!_ui.IsTransitioning) Refresh();
        }

        /// <summary>cLuxMainMenu_KeyConfig::ExitPressed: back to the options window (its draft settings kept).</summary>
        void CloseKeyConfig()
        {
            _keyConfig = null;
            _selected = 3; // the input tab
        }

        /// <summary>A scene that cannot be loaded is a build setup error: logged, and the menu goes back to its list.</summary>
        void LoadScene(string path, bool newGame)
        {
            if (!_ui.BeginSceneLoad(path, newGame)) Show();
        }

        /// <summary>
        /// cLuxMainMenu_LoadGame::LoadGame / AutoLoad: the save waits for its map scene (HplMap takes it instead of
        /// OnStart / OnEnter); an unreadable file or a map not in the build leaves the menu as it was.
        /// </summary>
        void LoadSave(string file)
        {
            var save = file != null ? HplSaveHandler.ReadFile(file) : null;
            string scene = save != null ? HplMapChange.FindScene(save.mapFile) : null;
            if (save != null && scene == null)
                Debug.LogError($"[RTXMnesia] Could not load the saved map '{save.mapFile}': no scene in the build (import the map, then Import > Main menu and build scenes)");
            if (scene == null)
            {
                Show();
                return;
            }
            HplSaveHandler.BeginLoad(save);
            if (!_ui.BeginSceneLoad(scene, false))
            {
                HplSaveHandler.ClearPending();
                Show();
            }
        }
    }
}
