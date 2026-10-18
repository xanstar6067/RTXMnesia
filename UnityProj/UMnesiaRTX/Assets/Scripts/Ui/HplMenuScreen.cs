using System.Collections.Generic;
using RTXMnesia.Hpl;
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
        /// <summary>A key config slot waits for an input, or a profile name is typed: the keys go there, not to the game UI.</summary>
        public bool CapturingInput => _keyConfig != null && _keyConfig.IsWaiting || EnteringName && !ConfirmingQuit;
        /// <summary>The profile window (cLuxMainMenu_Profile) is open.</summary>
        public bool ShowingProfiles => _extras.profiles != null;
        /// <summary>The enter name window of the profiles is open.</summary>
        public bool EnteringName => _extras.enteringName;
        /// <summary>The start game window (cLuxMainMenu_StartGame) is open.</summary>
        public bool ShowingStartGame => _extras.startGame;
        public HplMenuExtras Extras => _extras;
        /// <summary>Tests: the title screen asks for a profile in batch runs too.</summary>
        public static bool RequireProfileInBatch;
        /// <summary>The open drop-down list of the graphics options; null: none.</summary>
        public HplMenuCombo OpenCombo => _combo;

        readonly HplGameUi _ui;
        readonly HplMap _map;
        readonly HplPauseMenu _menu;
        int _selected, _optionsTab;
        readonly List<string> _saveFiles = new(), _saveLabels = new();
        int _selectedSave = -1;
        MenuControl? _dragSlider;
        HplKeyConfig _keyConfig;
        HplMenuCombo _combo;
        readonly HplMenuExtras _extras = new();
        readonly List<string> _profileNames = new();
        readonly List<char> _typed = new();
        bool _typingHooked;
        /// <summary>The pause (HplGameUi.PauseGeneration) whose menu made a hard mode save: its Save is disabled.</summary>
        int _hardSavedPause = -1;
        /// <summary>The control to focus after the next drawing (its index is known only then).</summary>
        MenuControl? _focus;

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
            // cLuxMainMenu::OnEnterContainer: no user config yet — the title screen starts with the profiles.
            if (_ui.IsTitleScreen && !HplProfiles.ChosenThisRun && (!Application.isBatchMode || RequireProfileInBatch))
            {
                OpenProfiles();
                return;
            }
            Refresh();
        }

        public void Reset()
        {
            if (DraftSettings != null) HplHdr.Apply(_ui.Settings);
            HplHdr.CalibrationVisible = false;
            ConfirmingQuit = false;
            ShowingOptions = false;
            ShowingLoad = false;
            DraftSettings = null;
            _dragSlider = null;
            _keyConfig = null;
            _combo = null;
            _extras.profiles = null;
            _extras.startGame = false;
            _extras.message = null;
            CloseEnterName();
        }

        /// <summary>cLuxMainMenu::ExitPressed: closes a window, or the menu itself in game.</summary>
        public void Escape()
        {
            // An open drop-down list closes first (cWidgetComboBox loses focus).
            if (_combo != null)
            {
                _combo = null;
                return;
            }
            if (ShowingKeyConfig)
            {
                // cLuxInputMenuEntry: Esc cancels the waiting input; the window's pop up closes; else back to options.
                if (_keyConfig.IsWaiting) _keyConfig.CancelWaiting();
                else if (ConfirmingQuit) ConfirmingQuit = false;
                else CloseKeyConfig();
                return;
            }
            // Pop ups of the profile and start game windows close back to their window, then the windows close.
            if (ShowingProfiles || ShowingStartGame)
            {
                if (ConfirmingQuit) { ConfirmingQuit = false; _selected = 0; }
                else if (EnteringName) { CloseEnterName(); _selected = 0; }
                else if (ShowingProfiles) CloseProfiles();
                else Show();
                Refresh();
                return;
            }
            // A confirmation of the load window goes back to the window, as its pop up does.
            if (ConfirmingQuit && PendingConfirmation == MenuControl.ConfirmLoad) OpenLoadGame(_selectedSave);
            else if (ConfirmingQuit || ShowingOptions || ShowingLoad) Show();
            else if (!_ui.IsTitleScreen) _ui.BeginClose();
        }

        public void Refresh()
        {
            if (ShowingOptions && DraftSettings != null) HplHdr.Apply(DraftSettings);
            UpdateHardModeExtras();
            _menu.Refresh(ConfirmingQuit, ShowingOptions, _optionsTab, DraftSettings ?? _ui.Settings,
                _selected, _ui.MenuOpacity, HplGameUi.PointerPosition, PendingConfirmation, ShowingLoad || ConfirmingQuit && PendingConfirmation == MenuControl.ConfirmLoad ? _saveLabels : null, _selectedSave,
                _keyConfig, _combo, _extras);
            if (_focus.HasValue)
            {
                for (int i = 0; i < _menu.ButtonCount; i++)
                    if (_menu.ControlAt(i) == _focus.Value) { _selected = i; break; }
                _focus = null;
                Refresh();
            }
        }

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
            if (_keyConfig != null && _keyConfig.IsWaiting)
            {
                if (Time.frameCount > _keyConfig.WaitingFrame)
                {
                    string input = HplKeyBindings.PressedInput();
                    if (input != null) _keyConfig.Assign(input);
                }
                Refresh();
                return;
            }
            if (_combo != null)
            {
                UpdateCombo(mouse, keyboard);
                return;
            }
            if (EnteringName && !ConfirmingQuit) Type(keyboard);
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
            // The name text box takes Enter (create) and Space (a character) itself.
            else if (EnteringName && !ConfirmingQuit) { }
            else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                Activate(_selected);
        }

        // ------------------------------------------------------------------ profiles

        /// <summary>cLuxMainMenu_Profile::OnSetActive: the list; without profiles the enter name window at once.</summary>
        public void OpenProfiles()
        {
            Reset();
            ReloadProfiles();
            string pick = HplProfiles.ChosenThisRun ? HplProfiles.Current : HplProfiles.Last;
            _extras.selectedProfile = pick != null ? _profileNames.IndexOf(pick) : -1;
            if (_extras.selectedProfile < 0 && _profileNames.Count > 0) _extras.selectedProfile = 0;
            _selected = 0;
            if (_profileNames.Count == 0) OpenEnterName();
            Refresh();
        }

        void ReloadProfiles()
        {
            _profileNames.Clear();
            _profileNames.AddRange(HplProfiles.List());
            _extras.profiles = _profileNames;
        }

        /// <summary>cLuxMainMenu_Profile::WindowCloses: back to the menu with a profile in use, else the quit question.</summary>
        void CloseProfiles()
        {
            if (HplProfiles.ChosenThisRun && HplProfiles.Current != null)
            {
                Show();
                return;
            }
            PendingConfirmation = MenuControl.ConfirmQuit;
            ConfirmingQuit = true;
            _selected = 1;
        }

        /// <summary>cLuxMainMenu_Profile::SelectProfile: SetProfile, InitUserConfig and the menu again.</summary>
        void SelectProfile(int row)
        {
            if (row < 0 || row >= _profileNames.Count) return;
            HplProfiles.Select(_profileNames[row]);
            HplKeyBindings.Reload();
            _ui.Settings = HplUserSettings.Load(_map);
            if (Application.isPlaying) _ui.Settings.Apply(_map);
            Show();
        }

        /// <summary>cLuxMainMenu_Profile::PressCreateProfile: the default name ("New Player"), all selected.</summary>
        void OpenEnterName()
        {
            _extras.enteringName = true;
            _extras.nameText = _menu.Translate("MainMenu", "New Player", "Новый игрок");
            _extras.nameSelected = true;
            _typed.Clear();
            var keyboard = Keyboard.current;
            if (keyboard != null && !_typingHooked)
            {
                keyboard.onTextInput += OnTextInput;
                _typingHooked = true;
            }
            _selected = 0;
        }

        void CloseEnterName()
        {
            _extras.enteringName = false;
            _typed.Clear();
            if (_typingHooked && Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
            _typingHooked = false;
        }

        void OnTextInput(char c)
        {
            if (EnteringName) _typed.Add(c);
        }

        /// <summary>
        /// cWidgetTextBox of the name: typed characters (not the illegal ones, up to 20) replace the selected text or
        /// go to the end, Backspace deletes, Enter creates (SetForceCallBackOnEnter).
        /// </summary>
        void Type(Keyboard keyboard)
        {
            foreach (char c in _typed)
            {
                if (char.IsControl(c) || HplProfiles.IllegalChars.IndexOf(c) >= 0) continue;
                if (_extras.nameSelected)
                {
                    _extras.nameText = "";
                    _extras.nameSelected = false;
                }
                if (_extras.nameText.Length < HplProfiles.MaxNameLength) _extras.nameText += c;
            }
            _typed.Clear();
            if (keyboard == null) return;
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                if (_extras.nameSelected) _extras.nameText = "";
                else if (_extras.nameText.Length > 0) _extras.nameText = _extras.nameText.Substring(0, _extras.nameText.Length - 1);
                _extras.nameSelected = false;
            }
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) CreateProfile();
        }

        /// <summary>
        /// cLuxMainMenu_Profile::PressEnterNameCreate: spaces trimmed; an empty or existing name — "The profile name
        /// already exists" (the original shows it for an empty name too); else the list with the new one selected.
        /// </summary>
        void CreateProfile()
        {
            string name = HplProfiles.CleanName(_extras.nameText);
            bool first = HplProfiles.List().Count == 0;
            if (name == "" || !HplProfiles.Create(name))
            {
                Message(_menu.Translate("MainMenu", "The profile name already exists", "Профиль с таким именем уже существует"));
                return;
            }
            if (first)
            {
                // The first profile keeps the options and keys chosen before profiles existed.
                HplUserSettings.CopyGlobalToProfile(name);
                HplKeyBindings.CopyGlobalToProfile(name);
            }
            CloseEnterName();
            ReloadProfiles();
            _extras.selectedProfile = _profileNames.IndexOf(name);
            _focus = MenuControl.ProfileSelect;
        }

        /// <summary>cGuiPopUpMessageBox with one OK button over the current window.</summary>
        void Message(string text)
        {
            _extras.message = text;
            PendingConfirmation = MenuControl.MessageOk;
            ConfirmingQuit = true;
            _selected = 0;
        }

        // ------------------------------------------------------------------ start game and hard mode

        /// <summary>cLuxMainMenu::PressStartGame with AllowHardMode (the main campaign): the start game window, normal mode.</summary>
        void OpenStartGame()
        {
            Reset();
            _extras.startGame = true;
            _extras.hardMode = false;
            // OnSetActive: the focus on Start game.
            _focus = MenuControl.StartGameStart;
        }

        /// <summary>The in-game menu of a hard mode game: Save and the tinderboxes.</summary>
        void UpdateHardModeExtras()
        {
            _extras.hardModeSave = !_ui.IsTitleScreen && HplDifficulty.HardMode && !_map.Campaign.IsJustine && _map.player != null;
            _extras.tinderboxes = _map.player != null ? _map.player.tinderboxes : 0;
            _extras.hardModeSaved = _hardSavedPause == _ui.PauseGeneration;
        }

        /// <summary>
        /// The open list: the wheel and the arrow buttons scroll, the pointer or Up / Down highlight, a click or Enter
        /// picks (a disabled choice does nothing), a click outside closes it.
        /// </summary>
        void UpdateCombo(Mouse mouse, Keyboard keyboard)
        {
            var items = HplGraphicsOptions.Items(_combo.control, DraftSettings, _menu.Translate, out _);
            int shown = Mathf.Min(items.Count, HplMenuCombo.MaxItems);
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel != 0f) Scroll(wheel > 0f ? -1 : 1, items.Count, shown);
                int hit = _menu.HitTest(mouse.position.ReadValue());
                if (hit >= 0 && _menu.ControlAt(hit) == MenuControl.ComboItem && mouse.delta.ReadValue().sqrMagnitude > 0f)
                    _combo.hover = _menu.DataAt(hit);
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (hit < 0) _combo = null;
                    else Activate(hit);
                    Refresh();
                    return;
                }
            }
            if (keyboard != null)
            {
                int move = keyboard.upArrowKey.wasPressedThisFrame ? -1 : keyboard.downArrowKey.wasPressedThisFrame ? 1 : 0;
                if (move != 0)
                {
                    _combo.hover = HplGraphicsOptions.Step(items, _combo.hover, move);
                    if (_combo.hover < _combo.first) _combo.first = _combo.hover;
                    if (_combo.hover >= _combo.first + shown) _combo.first = _combo.hover - shown + 1;
                }
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) PickComboItem(_combo.hover);
            }
            Refresh();
        }

        void Scroll(int direction, int count, int shown) =>
            _combo.first = Mathf.Clamp(_combo.first + direction, 0, Mathf.Max(0, count - shown));

        void OpenComboList(MenuControl control)
        {
            var items = HplGraphicsOptions.Items(control, DraftSettings, _menu.Translate, out int current);
            int shown = Mathf.Min(items.Count, HplMenuCombo.MaxItems);
            _combo = new HplMenuCombo { control = control, hover = current, first = Mathf.Clamp(current - shown + 1, 0, Mathf.Max(0, items.Count - shown)) };
        }

        public void PickComboItem(int item)
        {
            if (_combo == null) return;
            var items = HplGraphicsOptions.Items(_combo.control, DraftSettings, _menu.Translate, out _);
            if (item < 0 || item >= items.Count || !items[item].enabled) return;
            HplGraphicsOptions.Set(_combo.control, DraftSettings, item, _menu.Translate);
            _combo = null;
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
                else if (_dragSlider == MenuControl.HdrPeakSlider)
                {
                    DraftSettings.hdrPeak = Mathf.Round((HplHdr.MinPeak + value * (HplHdr.MaxPeak - HplHdr.MinPeak)) / 10) * 10;
                    HplHdr.CalibrationPattern = 0;
                }
                else if (_dragSlider == MenuControl.HdrWhiteSlider)
                {
                    DraftSettings.hdrPaperWhite = Mathf.Round(HplHdr.MinWhite + value * (HplHdr.MaxWhite - HplHdr.MinWhite));
                    HplHdr.CalibrationPattern = 1;
                }
                else if (_dragSlider == MenuControl.HdrBlackSlider)
                {
                    DraftSettings.hdrBlack = Mathf.Round(value * HplHdr.MaxBlack * 1000) / 1000;
                    HplHdr.CalibrationPattern = 2;
                }
                else if (_dragSlider == MenuControl.HdrShadowsSlider)
                {
                    DraftSettings.hdrShadows = Mathf.Round((HplHdr.MinShadows + value * (HplHdr.MaxShadows - HplHdr.MinShadows)) * 20f) / 20f;
                    HplHdr.CalibrationPattern = 2;
                }
                else DraftSettings.mouseSensitivity = Mathf.Round((0.2f + value * 4.8f) * 10) / 10f;
            }
            if (!mouse.leftButton.isPressed) _dragSlider = null;
        }

        public void OpenOptions(int tab = -1)
        {
            if (tab >= 0) _optionsTab = Mathf.Clamp(tab, 0, HplPauseMenu.OptionsTabs - 1);
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
            if (control >= MenuControl.GameTab && control <= MenuControl.RtxTab)
            {
                _optionsTab = (_optionsTab + HplPauseMenu.OptionsTabs + direction) % HplPauseMenu.OptionsTabs;
                _selected = _optionsTab;
                _dragSlider = null;
                Refresh();
            }
            else if (HplGraphicsOptions.IsCombo(control))
            {
                // Arrows on a closed list step to the neighbouring choice.
                var items = HplGraphicsOptions.Items(control, DraftSettings, _menu.Translate, out int current);
                HplGraphicsOptions.Set(control, DraftSettings, HplGraphicsOptions.Step(items, current, direction), _menu.Translate);
                Refresh();
            }
            else if (control >= MenuControl.Subtitles && control <= MenuControl.SmoothMouse ||
                     control == MenuControl.GammaLess || control == MenuControl.GammaMore || control == MenuControl.Commentary ||
                     control is MenuControl.Bloom or MenuControl.ImageTrail or MenuControl.RadialBlur ||
                     control >= MenuControl.HdrMode && control <= MenuControl.HdrDefaults) Activate(_selected, direction);
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
                // The main campaign asks for the mode first (cLuxMainMenu_StartGame); Justine has no hard mode.
                case MenuControl.NewGame when _ui.IsTitleScreen && !_map.Campaign.IsJustine:
                    OpenStartGame();
                    break;
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
                case MenuControl.ConfirmNewGame:
                    HplDifficulty.HardMode = ShowingStartGame && _extras.hardMode;
                    LoadScene(_map.Campaign.FirstScene, true);
                    break;
                case MenuControl.StartNormal: _extras.hardMode = false; break;
                case MenuControl.StartHard: _extras.hardMode = true; break;
                case MenuControl.StartGameStart:
                    PendingConfirmation = MenuControl.ConfirmNewGame;
                    ConfirmingQuit = true;
                    _selected = 1;
                    break;
                case MenuControl.HardSave:
                    if (_extras.hardModeSaved || _map.player == null || _map.player.tinderboxes < HplDifficulty.SaveCost) break;
                    PendingConfirmation = MenuControl.ConfirmHardSave;
                    ConfirmingQuit = true;
                    _selected = 1;
                    break;
                // cLuxMainMenu::ClickedSaveGamePopup: the cost from the inventory, a "HardMode" save, Save disabled.
                case MenuControl.ConfirmHardSave:
                    ConfirmingQuit = false;
                    if (_map.player == null || _map.player.tinderboxes < HplDifficulty.SaveCost) break;
                    _map.player.tinderboxes = Mathf.Max(0, _map.player.tinderboxes - HplDifficulty.SaveCost);
                    HplSaveHandler.HardModeSave(_map);
                    _hardSavedPause = _ui.PauseGeneration;
                    _selected = 0;
                    break;
                case MenuControl.Justine: LoadScene(HplCampaign.Justine.MenuScene, false); break;
                case MenuControl.MainCampaign: LoadScene(HplCampaign.Main.MenuScene, false); break;
                case MenuControl.ChangeProfile: OpenProfiles(); break;
                case MenuControl.ProfileRow:
                {
                    int profileRow = _menu.DataAt(index);
                    // A click on the selected row again chooses it (the list's double click).
                    if (profileRow == _extras.selectedProfile && direction == 0) SelectProfile(profileRow);
                    else _extras.selectedProfile = profileRow;
                    break;
                }
                case MenuControl.ProfileSelect: SelectProfile(_extras.selectedProfile); break;
                case MenuControl.ProfileCreate: OpenEnterName(); break;
                case MenuControl.ProfileDelete:
                    if (_extras.selectedProfile < 0) Message(_menu.Translate("MainMenu", "No profile to be deleted!", "Нет профиля для удаления!"));
                    else
                    {
                        PendingConfirmation = MenuControl.ConfirmProfileDelete;
                        ConfirmingQuit = true;
                        _selected = 1;
                    }
                    break;
                case MenuControl.ConfirmProfileDelete:
                {
                    ConfirmingQuit = false;
                    int deleted = _extras.selectedProfile;
                    if (deleted < 0 || deleted >= _profileNames.Count) break;
                    if (!HplProfiles.Delete(_profileNames[deleted]))
                    {
                        Message(_menu.Translate("MainMenu", "Could not remove profile", "Не удалось удалить профиль"));
                        break;
                    }
                    if (!HplProfiles.ChosenThisRun)
                    {
                        // The profile in use was deleted: back to the global settings and keys.
                        HplKeyBindings.Reload();
                        _ui.Settings = HplUserSettings.Load(_map);
                    }
                    ReloadProfiles();
                    // PressDeleteProfilePopupClose: the row above (−1 of the first: nothing selected).
                    _extras.selectedProfile = Mathf.Min(deleted - 1, _profileNames.Count - 1);
                    _selected = 0;
                    break;
                }
                case MenuControl.ProfileClose: CloseProfiles(); break;
                case MenuControl.NameCreate: CreateProfile(); break;
                case MenuControl.NameCancel:
                    CloseEnterName();
                    _selected = 0;
                    break;
                case MenuControl.MessageOk:
                    ConfirmingQuit = false;
                    _extras.message = null;
                    _selected = 0;
                    break;
                case MenuControl.ConfirmReturnTitle: LoadScene(_map.Campaign.MenuScene, false); break;
                // eLuxMainMenuExit_ContinueGame → cLuxSaveHandler::AutoLoad: the newest save.
                case MenuControl.ConfirmContinue: LoadSave(HplSaveHandler.GetNewestSaveFile(_map.Campaign)); break;
                // Own behaviour: auto save, then quit the application ("В главное меню" is the way to the title screen).
                case MenuControl.ConfirmExitAndSave:
                    HplSaveHandler.AutoSave(_map);
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
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
                    // A pop up over the profiles or the start game window goes back to the window.
                    else if (ConfirmingQuit && (ShowingProfiles || ShowingStartGame)) ConfirmingQuit = false;
                    // A pop up of the in-game menu (the hard mode save) goes back to the list.
                    else if (ConfirmingQuit && PendingConfirmation == MenuControl.ConfirmHardSave) ConfirmingQuit = false;
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
                case MenuControl.GameTab: case MenuControl.GraphicsTab: case MenuControl.SoundTab: case MenuControl.InputTab: case MenuControl.HdrTab: case MenuControl.RtxTab:
                    _optionsTab = (int)control - (int)MenuControl.GameTab;
                    _selected = _optionsTab;
                    break;
                case MenuControl.Subtitles: DraftSettings.subtitles = !DraftSettings.subtitles; break;
                case MenuControl.Hints: DraftSettings.hints = !DraftSettings.hints; break;
                case MenuControl.Crosshair: DraftSettings.crosshair = !DraftSettings.crosshair; break;
                case MenuControl.Commentary: DraftSettings.commentary = !DraftSettings.commentary; break;
                case MenuControl.Fullscreen: DraftSettings.fullscreen = !DraftSettings.fullscreen; break;
                case MenuControl.VSync: DraftSettings.vSync = !DraftSettings.vSync; break;
                case MenuControl.InvertMouse: DraftSettings.invertMouse = !DraftSettings.invertMouse; break;
                case MenuControl.Bloom: DraftSettings.bloom = !DraftSettings.bloom; break;
                case MenuControl.ImageTrail: DraftSettings.imageTrail = !DraftSettings.imageTrail; break;
                case MenuControl.RadialBlur: DraftSettings.radialBlur = !DraftSettings.radialBlur; break;
                case MenuControl.Lighting: case MenuControl.Textures: case MenuControl.Resolution: case MenuControl.ScreenMode:
                case MenuControl.FpsLimit: case MenuControl.TextureQuality: case MenuControl.Anisotropy:
                case MenuControl.Antialiasing: case MenuControl.Shadows: case MenuControl.DlssMode: case MenuControl.DlssPreset:
                    OpenComboList(control);
                    break;
                case MenuControl.ComboItem: PickComboItem(_menu.DataAt(index)); break;
                case MenuControl.ComboScrollUp:
                case MenuControl.ComboScrollDown:
                {
                    var items = HplGraphicsOptions.Items(_combo.control, DraftSettings, _menu.Translate, out _);
                    Scroll(control == MenuControl.ComboScrollUp ? -1 : 1, items.Count, Mathf.Min(items.Count, HplMenuCombo.MaxItems));
                    break;
                }
                case MenuControl.SmoothMouse: DraftSettings.smoothMouse = !DraftSettings.smoothMouse; break;
                case MenuControl.VolumeLess: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * less); break;
                case MenuControl.VolumeMore: DraftSettings.masterVolume = Mathf.Clamp01(DraftSettings.masterVolume + 0.05f * more); break;
                case MenuControl.SensitivityLess: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * less, 0.2f, 5f); break;
                case MenuControl.SensitivityMore: DraftSettings.mouseSensitivity = Mathf.Clamp(DraftSettings.mouseSensitivity + 0.1f * more, 0.2f, 5f); break;
                // cLuxMainMenu_Options: the gamma slider steps 0.05 between 0.3 and 2.
                case MenuControl.GammaLess: DraftSettings.gamma = Mathf.Clamp(DraftSettings.gamma + 0.05f * less, HplUserSettings.MinGamma, HplUserSettings.MaxGamma); break;
                case MenuControl.GammaMore: DraftSettings.gamma = Mathf.Clamp(DraftSettings.gamma + 0.05f * more, HplUserSettings.MinGamma, HplUserSettings.MaxGamma); break;
                case MenuControl.HdrMode: DraftSettings.hdrMode = (DraftSettings.hdrMode + 3 + more) % 3; break;
                case MenuControl.HdrPeakLess: DraftSettings.hdrPeak += 10 * less; HplHdr.CalibrationPattern = 0; break;
                case MenuControl.HdrPeakMore: DraftSettings.hdrPeak += 10 * more; HplHdr.CalibrationPattern = 0; break;
                case MenuControl.HdrWhiteLess: DraftSettings.hdrPaperWhite += 5 * less; HplHdr.CalibrationPattern = 1; break;
                case MenuControl.HdrWhiteMore: DraftSettings.hdrPaperWhite += 5 * more; HplHdr.CalibrationPattern = 1; break;
                case MenuControl.HdrBlackLess: DraftSettings.hdrBlack += 0.001f * less; HplHdr.CalibrationPattern = 2; break;
                case MenuControl.HdrBlackMore: DraftSettings.hdrBlack += 0.001f * more; HplHdr.CalibrationPattern = 2; break;
                case MenuControl.HdrShadowsLess: DraftSettings.hdrShadows += 0.05f * less; HplHdr.CalibrationPattern = 2; break;
                case MenuControl.HdrShadowsMore: DraftSettings.hdrShadows += 0.05f * more; HplHdr.CalibrationPattern = 2; break;
                case MenuControl.HdrPreview: HplHdr.CalibrationPattern = (HplHdr.CalibrationPattern + 3 + more) % 3; break;
                case MenuControl.HdrDetect: HplHdr.UseDisplayValues(DraftSettings); break;
                case MenuControl.HdrDefaults:
                    DraftSettings.hdrMode = 1; DraftSettings.hdrPeak = 1000; DraftSettings.hdrPaperWhite = 160; DraftSettings.hdrBlack = 0; DraftSettings.hdrShadows = HplHdr.DefaultShadows;
                    break;
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
