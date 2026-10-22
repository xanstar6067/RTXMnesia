using System.Collections.Generic;
using System.Globalization;
using RTXMnesia.ScriptApi;
using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    public enum MenuControl
    {
        Resume, Options, Quit, ConfirmQuit, Cancel, Apply,
        GameTab, DisplayTab, GraphicsTab, ImageTab, RayTab, SoundTab, InputTab,
        Subtitles, Hints, Crosshair, Fullscreen, VSync, VolumeLess, VolumeMore,
        SensitivityLess, SensitivityMore, InvertMouse, SmoothMouse,
        VolumeSlider, SensitivitySlider, NewGame, ReturnTitle, ConfirmNewGame, ConfirmReturnTitle,
        Continue, ConfirmContinue, LoadGame, LoadSlot, LoadOk, ConfirmLoad, ExitAndSave, ConfirmExitAndSave,
        KeyConfig, KeyCategory, KeySlot, KeyDefaults, KeyOk, ConfirmKeyDefaults, ConfirmKeyInvalid,
        GammaLess, GammaMore, GammaSlider, Commentary,
        // Graphics drop-down lists (contiguous: HplGraphicsOptions.IsCombo), toggles, and the open list's controls.
        Lighting, Textures, Resolution, ScreenMode, FpsLimit, TextureQuality, TextureMemoryBudget, Anisotropy, ImageMethod, Shadows, DlssPreset, HdrMode, RtShadows, RtAo, RtReflections, RtGi, RtAllShadows, RtLanternShadows, RtPathTracing,
        Bloom, ImageTrail, RadialBlur, ComboItem, ComboScrollUp, ComboScrollDown,
        // Profiles (cLuxMainMenu_Profile), message boxes with one OK button.
        ChangeProfile, ProfileRow, ProfileSelect, ProfileCreate, ProfileDelete, ProfileClose, ConfirmProfileDelete,
        NameCreate, NameCancel, MessageOk,
        // Start game window (cLuxMainMenu_StartGame), the hard mode save, the Justine campaign.
        StartNormal, StartHard, StartGameStart, HardSave, ConfirmHardSave, Justine, MainCampaign,
        HdrPeakLess, HdrPeakMore, HdrPeakSlider, HdrWhiteLess, HdrWhiteMore, HdrWhiteSlider,
        HdrBlackLess, HdrBlackMore, HdrBlackSlider, HdrShadowsLess, HdrShadowsMore, HdrShadowsSlider, HdrPreview, HdrDetect, HdrDefaults,
        // Ray tracing switch, the scroll bar arrows of the options, the HDR calibration window.
        RtEnabled, ScrollUp, ScrollDown, HdrCalibrate, HdrCalibrationClose, DebugPanel,
    }

    /// <summary>The state of the windows added to the menu after the first version (profiles, start game, messages).</summary>
    public sealed class HplMenuExtras
    {
        /// <summary>The profile window is open with these names (null: closed); the selected row, −1 none.</summary>
        public IReadOnlyList<string> profiles;
        public int selectedProfile = -1;
        /// <summary>The enter name window over the profiles: its text, all of it selected (typing replaces it).</summary>
        public bool enteringName;
        public string nameText = "";
        public bool nameSelected;
        /// <summary>The text of a message box with one OK button (<see cref="MenuControl.MessageOk"/>).</summary>
        public string message;
        /// <summary>The start game window is open; the chosen mode.</summary>
        public bool startGame, hardMode;
        /// <summary>In game in hard mode: the save button, the player's tinderboxes and whether this menu saved.</summary>
        public bool hardModeSave, hardModeSaved;
        public int tinderboxes;
        /// <summary>The HDR calibration window (opened from the display tab) is open.</summary>
        public bool hdrCalibration;
    }

    /// <summary>The open drop-down list (cWidgetComboBox menu): its control, first shown choice and the highlighted one.</summary>
    public sealed class HplMenuCombo
    {
        /// <summary>cWidgetComboBox::mlMaxItems.</summary>
        public const int MaxItems = 12;
        public MenuControl control;
        public int first, hover;
    }

    /// <summary>In-game cLuxMainMenu: menu.cfg layout and gui_main_menu.skin on the same Canvas system as the HUD.</summary>
    public sealed partial class HplPauseMenu
    {
        readonly HplMap _map;
        readonly HplMenuAssets _assets;
        readonly GameObject _root;
        readonly RectTransform _screen;
        readonly HplGuiText _menuText, _guiText, _loadingText, _popupText, _topText;
        readonly RawImage _shade, _cursor, _logo, _loadFade;
        readonly bool _titleScreen;
        readonly List<RawImage> _pieces = new(), _popupPieces = new(), _topPieces = new();
        readonly Dictionary<MenuControl, Rect> _comboRects = new();
        readonly List<(Rect rect, MenuControl control)> _buttons = new();
        readonly List<int> _buttonData = new();
        readonly List<(Rect rect, MenuControl control)> _sliders = new();
        int _pieceIndex, _popupPieceIndex, _topPieceIndex, _selected, _popupStart = -1;
        float _opacity;
        static readonly Color Highlight = new(232f / 255f, 201f / 255f, 28f / 255f, 1f);
        /// <summary>gui_main_menu.skin: the Default font size and TextBoxSelectedTextBack (the list selection).</summary>
        const float SkinFontSize = 14;
        static readonly Color SelectionColor = new(0.51f, 0.427f, 0.318f, 1f);
        /// <summary>The layer drawn into: 0 — the window, 1 — a pop up / list over it, 2 — a message over a pop up.</summary>
        int _layer;
        /// <summary>A disabled button (drawn grey, not pressable): the hard mode save without tinderboxes.</summary>
        bool _disabled;
        public int ButtonCount => _buttons.Count;
        public bool HasCursor => _cursor.texture != Texture2D.whiteTexture;
        public Canvas Canvas => _root.GetComponent<Canvas>();
        public MenuControl ControlAt(int index) => _buttons[index].control;
        /// <summary>The save row of a <see cref="MenuControl.LoadSlot"/> button.</summary>
        public int DataAt(int index) => _buttonData[index];
        /// <summary>Title screen: Continue and Load game are shown (cLuxSaveHandler::SaveFileExists, checked when the menu opens).</summary>
        public bool HasSave { get; set; }

        public HplPauseMenu(HplMap map, Transform parent, bool titleScreen = false)
        {
            _titleScreen = titleScreen;
            _map = map;
            _assets = Resources.Load<HplMenuAssets>(map.Campaign.ResourceName(HplMenuAssets.ResourceName));
            _root = new GameObject("HPL game menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 1f;
            _shade = Image(_root.transform, "Lower menu shade");
            _shade.rectTransform.anchorMin = new Vector2(0, 0);
            _shade.rectTransform.anchorMax = new Vector2(1, 0.675f);
            _shade.rectTransform.offsetMin = _shade.rectTransform.offsetMax = Vector2.zero;
            _screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            _screen.SetParent(_root.transform, false);
            _screen.anchorMin = _screen.anchorMax = _screen.pivot = Vector2.one * 0.5f;
            _screen.sizeDelta = new Vector2(800, 600);
            _logo = Image(_screen, "Original title logo");
            _logo.texture = _assets != null ? _assets.titleLogo : null;
            _logo.enabled = _titleScreen && _logo.texture != null;
            _menuText = Text("Menu font", _assets != null ? _assets.menuFont : map.hud.defaultFont);
            // The scrolling area of the options: above the window's pieces, below the text and the lists over it.
            CreateScrollLayer(_assets != null ? _assets.guiFont : map.hud.defaultFont);
            _guiText = Text("GUI font", _assets != null ? _assets.guiFont : map.hud.defaultFont);
            // An open drop-down list is drawn over the window, its own pieces and text above the window's text.
            _popupText = Text("Drop-down list font", _assets != null ? _assets.guiFont : map.hud.defaultFont);
            _topText = Text("Message font", _assets != null ? _assets.guiFont : map.hud.defaultFont);
            _cursor = Image(_screen, "Original menu cursor");
            var pointer = _assets != null ? _assets.Find("PointerNormal").texture : null;
            _cursor.texture = pointer != null ? pointer : Texture2D.whiteTexture;
            _cursor.enabled = pointer != null;
            _loadFade = Image(_root.transform, "Scene transition fade");
            _loadFade.rectTransform.anchorMin = Vector2.zero; _loadFade.rectTransform.anchorMax = Vector2.one;
            _loadFade.rectTransform.offsetMin = _loadFade.rectTransform.offsetMax = Vector2.zero;
            _loadFade.color = Color.clear;
            _loadingText = Text("Loading text", _assets != null ? _assets.guiFont : map.hud.defaultFont);
            _loadingText.transform.SetParent(_root.transform, false);
            _loadingText.rectTransform.anchorMin = _loadingText.rectTransform.anchorMax = _loadingText.rectTransform.pivot = Vector2.one * 0.5f;
            _loadingText.rectTransform.anchoredPosition = Vector2.zero; _loadingText.rectTransform.sizeDelta = new Vector2(800, 600);
            if (_assets == null) Debug.LogWarning("[RTXMnesia] Menu resources missing: run Import > Menu resources.");
            _root.SetActive(false);
        }

        HplGuiText Text(string name, HplFont font)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_screen, false);
            Place(go.GetComponent<RectTransform>(), new Rect(0, 0, 800, 600));
            var text = go.AddComponent<HplGuiText>();
            text.font = font;
            text.raycastTarget = false;
            return text;
        }

        public void SetVisible(bool visible)
        {
            _root.SetActive(visible);
            if (!visible) HplHdr.CalibrationVisible = false;
        }
        Vector2 VirtualPoint(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, position, Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera, out var p);
            return new Vector2(p.x + 400, 300 - p.y);
        }
        /// <summary>The button under the pointer; with a drop-down list open only its own controls (-1 elsewhere).</summary>
        public int HitTest(Vector2 position)
        {
            var p = VirtualPoint(position);
            for (int i = Mathf.Max(0, _popupStart); i < _buttons.Count; i++) if (_buttons[i].rect.Contains(p)) return i;
            return -1;
        }
        public string Translate(string category, string entry, string fallback) => T(category, entry, fallback);
        public bool TrySlider(Vector2 position, out MenuControl control)
        {
            var p = VirtualPoint(position);
            foreach (var slider in _sliders)
                if (slider.rect.Contains(p)) { control = slider.control; return true; }
            control = default;
            return false;
        }
        public float SliderValue(MenuControl control, Vector2 position)
        {
            var slider = _sliders.Find(s => s.control == control);
            return Mathf.Clamp01((VirtualPoint(position).x - slider.rect.x) / slider.rect.width);
        }

        public void Refresh(bool confirmQuit, bool options, int tab, HplUserSettings settings, int selected, float opacity, Vector2 pointer,
                            MenuControl confirmation = MenuControl.ConfirmQuit, IReadOnlyList<string> saves = null, int selectedSave = -1,
                            HplKeyConfig keyConfig = null, HplMenuCombo combo = null, HplMenuExtras extras = null, float scroll = 0)
        {
            extras ??= new HplMenuExtras();
            HplHdr.CalibrationVisible = false;
            _loadFade.color = Color.clear; _loadingText.Clear();
            _menuText.Clear();
            _guiText.Clear();
            _popupText.Clear();
            _topText.Clear();
            _scrollText.Clear();
            _scrollPieceIndex = 0;
            _scrollFull.Clear();
            _disabledButtons.Clear();
            _noNavButtons.Clear();
            _inScroll = _skipDraw = false;
            _scroll = scroll;
            _topPieceIndex = 0;
            _buttons.Clear();
            _buttonData.Clear();
            _sliders.Clear();
            _comboRects.Clear();
            _pieceIndex = 0;
            _popupPieceIndex = 0;
            _popupStart = -1;
            _selected = selected;
            _opacity = opacity;
            _shade.color = new Color(0, 0, 0, _titleScreen ? 0 : 0.5f * opacity);
            _logo.enabled = _titleScreen && _logo.texture != null && !confirmQuit && !options && saves == null && keyConfig == null &&
                            extras.profiles == null && !extras.startGame;
            if (_logo.enabled)
            {
                float width = Screen.height > 0 ? Screen.width * 600f / Screen.height : 800;
                Vector2 pos = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "MainMenuLogoStartRelativePos", new Vector2(0.0525f, 0.22f)) : new Vector2(0.0525f, 0.22f);
                Vector2 size = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "MainMenuLogoRelativeSize", Vector2.one * 0.47f) : Vector2.one * 0.47f;
                float height = size.y * 600;
                Place(_logo.rectTransform, new Rect(pos.x * width - (width - 800) * 0.5f, pos.y * 600, height * _logo.texture.width / _logo.texture.height, height));
                _logo.color = new Color(1, 1, 1, opacity);
            }
            if (confirmQuit)
            {
                // cGuiPopUpMessageBox opens over the window that asked (load game, key config, profiles, start
                // game); that window is drawn but not pressable, the pop up goes on the layer above it.
                bool window = keyConfig != null || saves != null || extras.profiles != null || extras.startGame;
                if (window)
                {
                    _selected = -1;
                    if (keyConfig != null) KeyConfig(keyConfig);
                    else if (saves != null) LoadGame(saves, selectedSave);
                    else if (extras.profiles != null)
                    {
                        Profiles(extras);
                        if (extras.enteringName)
                        {
                            _layer = 1;
                            EnterName(extras);
                        }
                    }
                    else StartGame(extras);
                    _buttons.Clear();
                    _buttonData.Clear();
                    _selected = selected;
                }
                _layer = extras.profiles != null && extras.enteringName ? 2 : 1;
                Confirmation(confirmation, extras);
                _layer = 0;
            }
            else if (extras.profiles != null)
            {
                if (extras.enteringName)
                {
                    _selected = -1;
                    Profiles(extras);
                    _buttons.Clear();
                    _buttonData.Clear();
                    _selected = selected;
                    _layer = 1;
                    EnterName(extras);
                    _layer = 0;
                }
                else Profiles(extras);
            }
            else if (extras.startGame) StartGame(extras);
            else if (keyConfig != null) KeyConfig(keyConfig);
            else if (options)
            {
                if (extras.hdrCalibration) HdrCalibration(settings);
                else Options(tab, settings);
                if (combo != null) ComboList(combo, settings);
            }
            else if (saves != null) LoadGame(saves, selectedSave);
            else Main(extras);
            for (int i = _pieceIndex; i < _pieces.Count; i++) _pieces[i].enabled = false;
            for (int i = _popupPieceIndex; i < _popupPieces.Count; i++) _popupPieces[i].enabled = false;
            for (int i = _topPieceIndex; i < _topPieces.Count; i++) _topPieces[i].enabled = false;
            for (int i = _scrollPieceIndex; i < _scrollPieces.Count; i++) _scrollPieces[i].enabled = false;
            if (HasCursor)
            {
                _cursor.enabled = true;
                var p = VirtualPoint(pointer);
                Place(_cursor.rectTransform, new Rect(p.x, p.y, _cursor.texture.width, _cursor.texture.height));
                _cursor.transform.SetAsLastSibling();
            }
        }

        public void RefreshLoading(float alpha)
        {
            _loadFade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));
            _cursor.enabled = false;
            _loadingText.Clear();
            if (alpha >= 0.99f) _loadingText.DrawFont(T("LoadingScreen", "Loading", "Загрузка…"), new Vector2(400, 550), Vector2.one * 18, Color.white, HplFontAlign.Center);
        }

        void Main(HplMenuExtras extras)
        {
            Vector2 size = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "TopMenuFontRelativeSize", Vector2.one * 0.05f) : Vector2.one * 0.05f;
            size = Vector2.Scale(size, new Vector2(800, 600));
            Vector2 pos = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", _titleScreen ? "TopMenuStartRelativePos" : "TopMenuStartRelativePosInGame", _titleScreen ? new Vector2(0.8f, 0.325f) : Vector2.one * 0.5f) : Vector2.one * 0.5f;
            pos = Vector2.Scale(pos, new Vector2(800, 600));
            // cLuxMainMenu::CreateTopMenuGui: Continue / Load game with a save on the title screen, Exit and save in
            // game while the player lives; Exit stays in game too (like closing the window, it auto saves as OnQuit).
            var labels = new List<string>();
            var controls = new List<MenuControl>();
            void Add(string key, string fallback, MenuControl control)
            {
                labels.Add(T("MainMenu", key, fallback));
                controls.Add(control);
            }
            void AddOwn(string label, MenuControl control)
            {
                labels.Add(label);
                controls.Add(control);
            }
            bool justine = _map.Campaign.IsJustine;
            int saveIndex = -1;
            if (_titleScreen)
            {
                // Justine (mbPTestActivated) has no Continue / Load game: it does not save.
                if (HasSave && !justine) Add("Continue", "Продолжить", MenuControl.Continue);
                Add("Start Game", "Новая игра", MenuControl.NewGame);
                if (HasSave && !justine) Add("Load Game", "Загрузить игру", MenuControl.LoadGame);
                // Own entries (in HPL2 Justine is a separate launch): the other campaign's title screen.
                if (!justine) AddOwn(JustineLabel, MenuControl.Justine);
                Add("Options", "Настройки", MenuControl.Options);
                Add("Change Profile", "Сменить профиль", MenuControl.ChangeProfile);
                if (justine) AddOwn(MainCampaignLabel, MenuControl.MainCampaign);
                Add("Exit", "Выход", MenuControl.Quit);
            }
            else
            {
                Add("Back To Game", "Вернуться в игру", MenuControl.Resume);
                // HARDMODE: Save for tinderboxes (disabled with fewer than the cost, or once saved in this menu).
                if (extras.hardModeSave)
                {
                    saveIndex = labels.Count;
                    Add("Save", "Сохранить", MenuControl.HardSave);
                }
                Add("Options", "Настройки", MenuControl.Options);
                Add("ExitToMainMenu", "В главное меню", MenuControl.ReturnTitle);
                // Not in Justine (mbPTestActivated), for a dead player or in hard mode.
                if ((_map.player == null || !_map.player.IsDead) && !extras.hardModeSave && !justine) Add("ExitAndSave", "Сохранить и выйти", MenuControl.ExitAndSave);
                // No quit-the-application entry in the game menu: cLuxMainMenu adds "Exit" only without a map (or with
                // Main ExitMenuDirectly, off by default); the application is quit from the title screen.
            }
            float maxWidth = 0;
            foreach (var label in labels) maxWidth = Mathf.Max(maxWidth, _menuText.font != null ? _menuText.font.GetLength(size, label) : 0);
            float virtualWidth = Screen.height > 0 ? Screen.width * 600f / Screen.height : 800;
            if (_titleScreen) pos.x = pos.x / 800 * virtualWidth - (virtualWidth - 800) * 0.5f;
            size *= Mathf.Clamp(maxWidth > 0 ? virtualWidth * 0.35f / maxWidth : 1, 0.7f, 1);
            for (int i = 0; i < labels.Count; i++)
            {
                float y = pos.y + i * size.y * 1.3f;
                float width = _menuText.font != null ? _menuText.font.GetLength(size, labels[i]) : 300;
                _disabled = i == saveIndex && (extras.hardModeSaved || extras.tinderboxes < HplDifficulty.SaveCost);
                Button(new Rect(pos.x - width / 2 - 8, y, width + 16, size.y), labels[i], controls[i], _menuText, size);
                _disabled = false;
            }
            if (saveIndex >= 0)
            {
                // cLuxMainMenu::CreateTopMenuGui: the cost and the tinderboxes in the inventory, centered over the
                // menu (2.5 rows above its start), grey.
                float rowAdd = size.y * 1.3f;
                var grey = new Color(0.5f, 0.5f, 0.5f, 1);
                string cost = HplDifficulty.Text(Translate, "HardModeSaveCost").Replace("#", HplDifficulty.SaveCost.ToString(CultureInfo.InvariantCulture));
                Draw(_menuText, cost, new Vector2(pos.x, pos.y - 2.5f * rowAdd), size * 0.85f, grey, HplFontAlign.Center);
                string count = HplDifficulty.Text(Translate, "HardModeTinderboxInInventory").Replace("#", extras.tinderboxes.ToString(CultureInfo.InvariantCulture));
                Draw(_menuText, count, new Vector2(pos.x, pos.y - 2.5f * rowAdd + rowAdd * 0.65f), size * 0.75f, grey, HplFontAlign.Center);
            }
        }

        /// <summary>Own texts: the menu entries switching campaigns (HPL2 starts Justine as a separate game).</summary>
        public const string JustineLabel = "Дополнение «Жюстина»", MainCampaignLabel = "Основная игра";

        /// <summary>
        /// cGuiPopUpMessageBox: a window as wide as its text + 40 (at least 200) and 90 + font high, centered, the
        /// text at (20, 30) in the skin's default font (14), two skin buttons 80 × 30, 20 apart, at y 50 + font.
        /// The main menu's questions have no label; load game and key config pop ups are "Warning".
        /// </summary>
        void Confirmation(MenuControl action, HplMenuExtras extras)
        {
            var size = Vector2.one * SkinFontSize;
            var question = action switch
            {
                MenuControl.MessageOk => extras.message ?? "",
                MenuControl.ConfirmProfileDelete => T("MainMenu", "Sure you want to delete profile", "Удалить профиль?"),
                // cLuxMainMenu_StartGame::PressStartGame: the hard mode question.
                MenuControl.ConfirmNewGame when extras.startGame && extras.hardMode => HplDifficulty.Text(Translate, "HardModeStartNewGame"),
                MenuControl.ConfirmHardSave => HplDifficulty.Text(Translate, "HardModeSavePopup").Replace("#", HplDifficulty.SaveCost.ToString(CultureInfo.InvariantCulture)),
                MenuControl.ConfirmNewGame => T("MainMenu", "Start a new game?", "Начать новую игру?"),
                MenuControl.ConfirmReturnTitle => T("MainMenu", "Sure you want to exit to main menu?", "Выйти в главное меню?"),
                MenuControl.ConfirmContinue => T("MainMenu", "Continue old game?", "Продолжить прошлую игру?"),
                // Own text: here "save and exit" leaves the game (the original's string says "to the main menu").
                MenuControl.ConfirmExitAndSave => "Сохранить прогресс и выйти из игры?",
                MenuControl.ConfirmLoad => T("LoadGame", "LoadGameMessage", "Загрузить сохранённую игру?"),
                MenuControl.ConfirmKeyDefaults => T("KeyConfig", "LoadDefaultsMessage", "Применить раскладку по умолчанию?"),
                MenuControl.ConfirmKeyInvalid => T("KeyConfig", "InvalidInputMessage", "Некоторые клавиши повторяются. Сохранить?"),
                _ => T("MainMenu", "Sure you want to quit?", "Выйти из игры?"),
            };
            bool okCancel = action is MenuControl.ConfirmLoad or MenuControl.ConfirmKeyDefaults or MenuControl.ConfirmKeyInvalid;
            string label = okCancel ? T("Global", "Warning", "Внимание") : "";
            float length = _guiText.font != null ? Mathf.Max(_guiText.font.GetLength(size, label), _guiText.font.GetLength(size, question)) : 300;
            float width = Mathf.Max(length + 40, 200), height = 90 + size.y;
            var window = new Rect(400 - width / 2, 300 - height / 2, width, height);
            Window(window, label);
            Draw(_guiText, question, window.position + new Vector2(20, 30), size, Color.white);
            float y = window.y + 50 + size.y;
            // A message box: one OK button in the middle.
            if (action == MenuControl.MessageOk)
            {
                SkinButton(new Rect(window.x + width / 2 - 40, y, 80, 30), T("MainMenu", "OK", "OK"), MenuControl.MessageOk);
                return;
            }
            float x = window.x + width / 2 - (80 * 2 + 20) / 2f;
            string yes = action == MenuControl.ConfirmHardSave ? T("MainMenu", "Save", "Сохранить") : okCancel ? T("Global", "OK", "OK") : T("MainMenu", "Yes", "Да");
            string no = okCancel || action == MenuControl.ConfirmHardSave ? T("Global", "Cancel", "Отмена") : T("MainMenu", "No", "Нет");
            SkinButton(new Rect(x, y, 80, 30), yes, action);
            SkinButton(new Rect(x + 100, y, 80, 30), no, MenuControl.Cancel);
        }

        /// <summary>
        /// cLuxMainMenu_Profile::CreateMainGui: the window (menu.cfg Profiles WindowSize) centered with the close
        /// cross, the list box at (35, 75) of the window width − 70 and height − 75 − 40 − 70 (rows of ListFontSize
        /// + 2), Select / Create / Delete skin buttons a third of the list wide, 35 under it.
        /// </summary>
        void Profiles(HplMenuExtras extras)
        {
            Vector2 size = _map.menuConfig != null ? _map.menuConfig.GetVector2("Profiles", "WindowSize", new Vector2(500, 360)) : new Vector2(500, 360);
            float infoWidth = _map.menuConfig != null ? _map.menuConfig.GetFloat("Profiles", "InformationWidth", 0) : 0;
            Vector2 font = _map.menuConfig != null ? _map.menuConfig.GetVector2("Profiles", "ListFontSize", Vector2.one * 18) : Vector2.one * 18;
            var window = new Rect(400 - size.x / 2, 300 - size.y / 2, size.x, size.y);
            Window(window, T("MainMenu", "Profiles", "Профили"));
            const float border = 35;
            var list = new Rect(window.x + border, window.y + 40 + border, size.x - infoWidth - border * 2, size.y - (40 + border) - 40 - border * 2);
            Piece("ListBoxBackground", list, Color.white);
            float rowHeight = font.y + 2;
            int rows = Mathf.Max(1, Mathf.FloorToInt((list.height - 2) / rowHeight));
            var names = extras.profiles;
            int first = Mathf.Clamp(extras.selectedProfile - rows + 1, 0, Mathf.Max(0, names.Count - rows));
            for (int i = first; i < names.Count && i < first + rows; i++)
            {
                Rect rect = new(list.x, list.y + 2 + (i - first) * rowHeight, list.width, font.y);
                if (i == extras.selectedProfile) Piece(null, rect, SelectionColor);
                int index = _buttons.Count;
                _buttons.Add((rect, MenuControl.ProfileRow));
                _buttonData.Add(i);
                Draw(_guiText, names[i], new Vector2(rect.x + 3, rect.y), font, index == _selected && i != extras.selectedProfile ? Highlight : Color.white);
            }
            float y = list.yMax + border, width = list.width / 3f - 3, x = list.x;
            SkinButton(new Rect(x, y, width, 30), T("MainMenu", "Select", "Выбрать"), MenuControl.ProfileSelect);
            SkinButton(new Rect(x + width + 3, y, width, 30), T("MainMenu", "Create", "Создать"), MenuControl.ProfileCreate);
            SkinButton(new Rect(x + (width + 3) * 2, y, width, 30), T("MainMenu", "Delete", "Удалить"), MenuControl.ProfileDelete);
            CloseCross(window, MenuControl.ProfileClose);
        }

        /// <summary>
        /// cLuxMainMenu_Profile::CreateEnterNameGui: the window (EnterNameWindowSize) centered, the label at (5, 35),
        /// the text box under it (width − 10, 20) with the selection background and the blinking marker, Create and
        /// Cancel (EnterNameButtonLength × 30) at the bottom corners, 5 from the borders.
        /// </summary>
        void EnterName(HplMenuExtras extras)
        {
            Vector2 size = _map.menuConfig != null ? _map.menuConfig.GetVector2("Profiles", "EnterNameWindowSize", new Vector2(250, 150)) : new Vector2(250, 150);
            float buttonLength = _map.menuConfig != null ? _map.menuConfig.GetFloat("Profiles", "EnterNameButtonLength", 80) : 80;
            var window = new Rect(400 - size.x / 2, 300 - size.y / 2, size.x, size.y);
            Window(window, T("MainMenu", "Create Profile", "Создать профиль"));
            const float border = 5;
            var font = Vector2.one * SkinFontSize;
            float y = window.y + 35;
            Draw(_guiText, T("MainMenu", "Enter name of Profile", "Введите имя профиля"), new Vector2(window.x + border, y), font, Color.white);
            y += font.y + border;
            var box = new Rect(window.x + border, y, size.x - border * 2, 20);
            Piece("TextBoxBackground", box, Color.white);
            float textWidth = _guiText.font != null ? _guiText.font.GetLength(font, extras.nameText) : extras.nameText.Length * 7;
            var textPos = new Vector2(box.x + 3, box.y + (box.height - font.y) / 2);
            if (extras.nameSelected && extras.nameText != "") Piece(null, new Rect(textPos.x, textPos.y, textWidth, font.y), SelectionColor);
            Draw(_guiText, extras.nameText, textPos, font, Color.white);
            // TextBoxMarker: animated, 0.5 s per frame.
            if (Mathf.FloorToInt(Time.unscaledTime / 0.5f) % 2 == 0)
                Piece("TextBoxMarker", new Rect(textPos.x + textWidth, textPos.y, 2, font.y), Color.white);
            float buttonY = window.yMax - 30 - border;
            SkinButton(new Rect(window.x + border, buttonY, buttonLength, 30), T("MainMenu", "Create", "Создать"), MenuControl.NameCreate);
            SkinButton(new Rect(window.xMax - border - buttonLength, buttonY, buttonLength, 30), T("MainMenu", "Cancel", "Отмена"), MenuControl.NameCancel);
            CloseCross(window, MenuControl.NameCancel);
        }

        /// <summary>
        /// cLuxMainMenu_StartGame::CreateGui: a 400 × 220 window, Normal / Hard (120 × 30, the chosen in the highlight
        /// color) on the left, the description on a dark background on the right, Cancel and Start game at the bottom.
        /// </summary>
        void StartGame(HplMenuExtras extras)
        {
            var size = new Vector2(400, 220);
            var window = new Rect(400 - size.x / 2, 300 - size.y / 2, size.x, size.y);
            Window(window, T("MainMenu", "Start Game", "Новая игра"));
            const float border = 15, buttonWidth = 120, sepp = 3;
            float right = size.x - (buttonWidth * 2 + sepp * 4);
            float bottom = window.y + size.y - 35;
            float leftX = window.x + right / 2 - buttonWidth / 2;
            float modeY = window.y + border + size.y / 4 - 15;
            ModeButton(new Rect(leftX, modeY, buttonWidth, 30), T("MainMenu", "NormalMode", "Обычный"), MenuControl.StartNormal, !extras.hardMode);
            ModeButton(new Rect(leftX, modeY + 35 + sepp, buttonWidth, 30), T("MainMenu", "HardMode", "Сложный режим"), MenuControl.StartHard, extras.hardMode);
            var description = new Rect(window.x + right, window.y + 30 + border, size.x - right - sepp * 4, size.y - border * 2 - 60);
            Piece(null, description, new Color(0, 0, 0, 0.5f));
            string text = extras.hardMode ? T("MainMenu", "HardModeDescription", "") : T("MainMenu", "NormalModeDescription", "");
            var font = Vector2.one * SkinFontSize;
            var rows = _guiText.font != null ? _guiText.font.GetWordWrapRows(description.width, font, text) : new List<string> { text };
            for (int r = 0; r < rows.Count && (r + 1) * font.y <= description.height; r++)
                Draw(_guiText, rows[r], new Vector2(description.center.x, description.y + r * font.y), font, Color.white, HplFontAlign.Center);
            SkinButton(new Rect(leftX, bottom, buttonWidth, 30), T("Global", "Cancel", "Отмена"), MenuControl.Cancel);
            SkinButton(new Rect(window.x + right, bottom, buttonWidth * 2 + sepp, 30), T("MainMenu", "Start Game", "Новая игра"), MenuControl.StartGameStart);
        }

        /// <summary>A mode button of the start game window: its text in the highlight color when chosen.</summary>
        void ModeButton(Rect r, string label, MenuControl action, bool chosen)
        {
            SkinButton(r, label, action, chosen);
        }

        /// <summary>cWidgetWindow close button: the skin's WindowButtonCross (WindowButtonSize 18) 5 from the top right.</summary>
        void CloseCross(Rect window, MenuControl action)
        {
            var r = new Rect(window.xMax - 18 - 5, window.y + 5, 18, 18);
            _buttons.Add((r, action));
            _buttonData.Add(-1);
            Piece("WindowButtonCross", r, _buttons.Count - 1 == _selected ? Highlight : Color.white);
        }

        /// <summary>
        /// cLuxMainMenu_LoadGame::CreateGui: a 600 × 440 window centered, the list box (15, 75) 570 × 300 with the
        /// list box background, rows of the default font (14) 2 apart from 2 below its top, the selected one on the
        /// skin's selection color; OK / Cancel skin buttons 80 × 30, 3 apart, 5 from the right and 35 from the bottom.
        /// </summary>
        void LoadGame(IReadOnlyList<string> saves, int selectedSave)
        {
            var window = new Rect(100, 80, 600, 440);
            Window(window, T("LoadGame", "LoadGameTitle", "Загрузить игру"));
            var list = new Rect(window.x + 15, window.y + 75, 570, 300);
            Piece("ListBoxBackground", list, Color.white);
            var size = Vector2.one * SkinFontSize;
            float rowHeight = size.y + 2;
            int rows = Mathf.FloorToInt((list.height - 2) / rowHeight);
            // The list scrolls so that the selected save stays visible (the slider of cWidgetListBox).
            int first = Mathf.Clamp(selectedSave - rows + 1, 0, Mathf.Max(0, saves.Count - rows));
            for (int i = first; i < saves.Count && i < first + rows; i++)
            {
                Rect rect = new(list.x, list.y + 2 + (i - first) * rowHeight, list.width, size.y);
                if (i == selectedSave) Piece(null, rect, SelectionColor);
                int index = _buttons.Count;
                _buttons.Add((rect, MenuControl.LoadSlot));
                _buttonData.Add(i);
                Draw(_guiText, saves[i], new Vector2(rect.x + 3, rect.y), size, index == _selected && i != selectedSave ? Highlight : Color.white);
            }
            float x = window.xMax - 80 * 2 - 3 - 5, y = window.yMax - 25 - 10;
            SkinButton(new Rect(x, y, 80, 30), T("Global", "OK", "OK"), MenuControl.LoadOk);
            SkinButton(new Rect(x + 83, y, 80, 30), T("Global", "Cancel", "Отмена"), MenuControl.Cancel);
        }

        /// <summary>
        /// The open list under its combo box (moved up if it would leave the screen): up to 12 choices of font height
        /// + 2, the highlighted one on a light background, disabled ones grey; more choices - scroll arrows.
        /// </summary>
        void ComboList(HplMenuCombo combo, HplUserSettings settings)
        {
            if (!_comboRects.TryGetValue(combo.control, out var box)) return;
            var items = HplGraphicsOptions.Items(combo.control, settings, Translate, out int current);
            int shown = Mathf.Min(items.Count, HplMenuCombo.MaxItems);
            combo.first = Mathf.Clamp(combo.first, 0, Mathf.Max(0, items.Count - shown));
            const float rowHeight = 16 + 2;
            float height = 2 + rowHeight * shown + 2;
            var menu = new Rect(box.x, box.yMax, box.width, height);
            if (menu.yMax > 595) menu.y = Mathf.Max(5, 595 - height);
            _popupStart = _buttons.Count;
            PopupPiece(null, menu, new Color(0, 0, 0, 0.92f));
            PopupPiece("ComboBoxBackground", menu, Color.white);
            bool scroll = items.Count > shown;
            float textWidth = menu.width - (scroll ? 20 : 0);
            for (int i = 0; i < shown; i++)
            {
                int item = combo.first + i;
                var row = new Rect(menu.x + 1, menu.y + 2 + i * rowHeight, textWidth - 2, rowHeight);
                if (item == combo.hover) PopupPiece(null, row, new Color(0.82f, 0.81f, 0.79f, 0.35f));
                else if (item == current) PopupPiece(null, row, new Color(1, 1, 1, 0.12f));
                _buttons.Add((row, MenuControl.ComboItem));
                _buttonData.Add(item);
                var color = !items[item].enabled ? new Color(0.5f, 0.5f, 0.5f, 1) : item == combo.hover ? Highlight : Color.white;
                color.a *= _opacity;
                _popupText.DrawFont(items[item].label, new Vector2(row.x + 3, row.y), Vector2.one * 16, color);
            }
            if (scroll)
            {
                var up = new Rect(menu.xMax - 19, menu.y + 2, 18, 18);
                var down = new Rect(menu.xMax - 19, menu.yMax - 20, 18, 18);
                PopupPiece("SliderVertBackground", new Rect(up.x, up.yMax, 18, down.y - up.yMax), Color.white);
                float range = down.y - up.yMax - 12;
                float t = combo.first / (float)(items.Count - shown);
                PopupPiece(null, new Rect(up.x + 3, up.yMax + t * range, 12, 12), new Color(1, 1, 1, 0.6f));
                PopupPiece("SliderVertArrowUp", up, Color.white);
                PopupPiece("SliderVertArrowDown", down, Color.white);
                _buttons.Add((up, MenuControl.ComboScrollUp)); _buttonData.Add(-1);
                _buttons.Add((down, MenuControl.ComboScrollDown)); _buttonData.Add(-1);
            }
        }

        void Button(Rect rect, string label, MenuControl action, HplGuiText layer = null, Vector2 size = default, HplFontAlign align = HplFontAlign.Center)
        {
            // A disabled button is not pressable: it gets an empty rectangle.
            int index = AddButton(rect, action);
            if (size == default) size = Vector2.one * 18;
            var color = _disabled ? new Color(0.5f, 0.5f, 0.5f, 1) : index == _selected ? Highlight : Color.white;
            Draw(layer != null ? layer : _guiText, label, new Vector2(align == HplFontAlign.Left ? rect.x : rect.center.x, rect.y), size, color, align);
        }

        /// <summary>cWidgetWindow with a label: the skin's WindowLabel font (25) at WindowLabelTextOffset (35, −20).</summary>
        void Window(Rect r, string label)
        {
            Window(r);
            if (!string.IsNullOrEmpty(label)) Draw(_guiText, label, r.position + new Vector2(35, -20), Vector2.one * 25, Color.white);
        }

        /// <summary>
        /// cWidgetButton: ButtonUp background and borders inside the rect (iWidget::DrawBordersAndCorners), the text
        /// in the default font centered (the focused button in the highlight color).
        /// </summary>
        void SkinButton(Rect r, string label, MenuControl action, bool highlighted = false)
        {
            int index = AddButton(r, action);
            Vector2 Size(string type)
            {
                var t = _assets != null ? _assets.Find(type).texture : null;
                return t != null ? new Vector2(t.width, t.height) : Vector2.one * 4;
            }
            Vector2 lu = Size("ButtonUpCornerLU"), ru = Size("ButtonUpCornerRU"), rd = Size("ButtonUpCornerRD"), ld = Size("ButtonUpCornerLD");
            Piece("ButtonUpBackground", new Rect(r.x + lu.x, r.y + lu.y, r.width - lu.x - rd.x, r.height - lu.y - rd.y), Color.white);
            Vector2 right = Size("ButtonUpBorderRight"), left = Size("ButtonUpBorderLeft"), up = Size("ButtonUpBorderUp"), down = Size("ButtonUpBorderDown");
            Piece("ButtonUpBorderRight", new Rect(r.xMax - right.x, r.y + ru.y, right.x, r.height - rd.y - ru.y), Color.white);
            Piece("ButtonUpBorderLeft", new Rect(r.x, r.y + lu.y, left.x, r.height - ld.y - lu.y), Color.white);
            Piece("ButtonUpBorderUp", new Rect(r.x + lu.x, r.y, r.width - lu.x - ru.x, up.y), Color.white);
            Piece("ButtonUpBorderDown", new Rect(r.x + ld.x, r.yMax - down.y, r.width - rd.x - ld.x, down.y), Color.white);
            Piece("ButtonUpCornerLU", new Rect(r.x, r.y, lu.x, lu.y), Color.white);
            Piece("ButtonUpCornerRU", new Rect(r.xMax - ru.x, r.y, ru.x, ru.y), Color.white);
            Piece("ButtonUpCornerRD", new Rect(r.xMax - rd.x, r.yMax - rd.y, rd.x, rd.y), Color.white);
            Piece("ButtonUpCornerLD", new Rect(r.x, r.yMax - ld.y, ld.x, ld.y), Color.white);
            Draw(_guiText, label, new Vector2(r.center.x, r.y + r.height / 2 - SkinFontSize / 2f), Vector2.one * SkinFontSize,
                 _disabled ? Grey : index == _selected || highlighted ? Highlight : Color.white, HplFontAlign.Center);
        }

        void Window(Rect r, bool hdrHole = false)
        {
            if (!hdrHole) Piece("WindowBackground", r, Color.white);
            else
            {
                WindowPart(r, new Rect(r.x, r.y, r.width, HdrPatch.y - r.y));
                WindowPart(r, new Rect(r.x, HdrPatch.yMax, r.width, r.yMax - HdrPatch.yMax));
                WindowPart(r, new Rect(r.x, HdrPatch.y, HdrPatch.x - r.x, HdrPatch.height));
                WindowPart(r, new Rect(HdrPatch.xMax, HdrPatch.y, r.xMax - HdrPatch.xMax, HdrPatch.height));
            }
            var types = new[] { "WindowBorderLeft", "WindowBorderRight", "WindowBorderUp", "WindowBorderDown", "WindowCornerLU", "WindowCornerRU", "WindowCornerLD", "WindowCornerRD" };
            foreach (var type in types)
            {
                var tex = _assets != null ? _assets.Find(type).texture : null;
                float w = tex != null ? tex.width : 4, h = tex != null ? tex.height : 4;
                Rect p = type switch {
                    "WindowBorderLeft" => new Rect(r.x - w, r.y, w, r.height), "WindowBorderRight" => new Rect(r.xMax, r.y, w, r.height),
                    "WindowBorderUp" => new Rect(r.x, r.y - h, r.width, h), "WindowBorderDown" => new Rect(r.x, r.yMax, r.width, h),
                    "WindowCornerLU" => new Rect(r.x - w, r.y - h, w, h), "WindowCornerRU" => new Rect(r.xMax, r.y - h, w, h),
                    "WindowCornerLD" => new Rect(r.x - w, r.yMax, w, h), _ => new Rect(r.xMax, r.yMax, w, h),
                };
                Piece(type, p, Color.white);
            }
        }

        void PopupPiece(string type, Rect rect, Color color)
        {
            bool top = _layer == 2;
            var list = top ? _topPieces : _popupPieces;
            // A new piece goes just below its layer's text. An existing one must keep its place: SetSiblingIndex to the
            // text's index moves a piece that is already below the text to above it (the text is hidden by the piece).
            if (_topPieceIndex == list.Count && top || _popupPieceIndex == list.Count && !top)
            {
                var created = Image(_screen, top ? "Message piece" : "Drop-down list piece");
                created.transform.SetSiblingIndex((top ? _topText : _popupText).transform.GetSiblingIndex());
                list.Add(created);
            }
            var target = list[top ? _topPieceIndex++ : _popupPieceIndex++];
            var piece = type != null && _assets != null ? _assets.Find(type) : default;
            target.texture = piece.texture != null ? piece.texture : Texture2D.whiteTexture;
            target.color = (piece.texture != null ? piece.color * color : color) * new Color(1, 1, 1, _opacity);
            target.enabled = true;
            Place(target.rectTransform, rect);
        }

        void WindowPart(Rect whole, Rect part)
        {
            Piece("WindowBackground", part, Color.white);
            _pieces[_pieceIndex - 1].uvRect = new Rect((part.x - whole.x) / whole.width,
                1 - (part.yMax - whole.y) / whole.height, part.width / whole.width, part.height / whole.height);
        }

        void Piece(string type, Rect rect, Color color)
        {
            if (_layer == 0 && _inScroll)
            {
                if (!_skipDraw) ScrollPiece(type, rect, color);
                return;
            }
            if (_layer > 0)
            {
                PopupPiece(type, rect, color);
                return;
            }
            if (_pieceIndex == _pieces.Count)
            {
                var image = Image(_screen, "Skin piece");
                image.transform.SetSiblingIndex(_menuText.transform.GetSiblingIndex());
                _pieces.Add(image);
            }
            var target = _pieces[_pieceIndex++];
            target.uvRect = new Rect(0, 0, 1, 1);
            var piece = type != null && _assets != null ? _assets.Find(type) : default;
            target.texture = piece.texture != null ? piece.texture : Texture2D.whiteTexture;
            target.color = (piece.texture != null ? piece.color * color : color) * new Color(1, 1, 1, _opacity);
            target.enabled = true;
            Place(target.rectTransform, rect);
        }
        string T(string category, string key, string fallback) => _map.hud.TryTranslate(category, key, out var text) ? text : fallback;
        void Draw(HplGuiText layer, string text, Vector2 pos, Vector2 size, Color color, HplFontAlign align = HplFontAlign.Left)
        {
            color.a *= _opacity;
            if (_layer == 0 && _inScroll)
            {
                if (!_skipDraw) _scrollText.DrawFont(text, pos - Viewport.position, size, color, align);
                return;
            }
            if (_layer > 0) layer = _layer == 2 ? _topText : _popupText;
            layer.DrawFont(text, pos, size, color, align);
        }
        static RawImage Image(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.raycastTarget = false;
            return image;
        }
        static void Place(RectTransform rect, Rect position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = position.size;
        }
        public void Destroy()
        {
            HplHdr.CalibrationVisible = false;
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root); else Object.DestroyImmediate(_root);
        }
    }
}
