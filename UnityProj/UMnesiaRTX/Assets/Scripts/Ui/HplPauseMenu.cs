using System.Collections.Generic;
using System.Globalization;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    public enum MenuControl
    {
        Resume, Options, Quit, ConfirmQuit, Cancel, Apply,
        GameTab, GraphicsTab, SoundTab, InputTab,
        Subtitles, Hints, Crosshair, Fullscreen, VSync, VolumeLess, VolumeMore,
        SensitivityLess, SensitivityMore, InvertMouse, SmoothMouse,
        VolumeSlider, SensitivitySlider, NewGame, ReturnTitle, ConfirmNewGame, ConfirmReturnTitle,
        Continue, ConfirmContinue, LoadGame, LoadSlot, LoadOk, ConfirmLoad, ExitAndSave, ConfirmExitAndSave,
        KeyConfig, KeyCategory, KeySlot, KeyDefaults, KeyOk, ConfirmKeyDefaults, ConfirmKeyInvalid,
        GammaLess, GammaMore, GammaSlider, Commentary,
    }

    /// <summary>In-game cLuxMainMenu: menu.cfg layout and gui_main_menu.skin on the same Canvas system as the HUD.</summary>
    public sealed class HplPauseMenu
    {
        readonly HplMap _map;
        readonly HplMenuAssets _assets;
        readonly GameObject _root;
        readonly RectTransform _screen;
        readonly HplGuiText _menuText, _guiText, _loadingText;
        readonly RawImage _shade, _cursor, _logo, _loadFade;
        readonly bool _titleScreen;
        readonly List<RawImage> _pieces = new();
        readonly List<(Rect rect, MenuControl control)> _buttons = new();
        readonly List<int> _buttonData = new();
        readonly List<(Rect rect, MenuControl control)> _sliders = new();
        int _pieceIndex, _selected;
        float _opacity;
        static readonly Color Highlight = new(232f / 255f, 201f / 255f, 28f / 255f, 1f);
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
            _guiText = Text("GUI font", _assets != null ? _assets.guiFont : map.hud.defaultFont);
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

        public void SetVisible(bool visible) => _root.SetActive(visible);
        Vector2 VirtualPoint(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, position, Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera, out var p);
            return new Vector2(p.x + 400, 300 - p.y);
        }
        public int HitTest(Vector2 position)
        {
            var p = VirtualPoint(position);
            for (int i = 0; i < _buttons.Count; i++) if (_buttons[i].rect.Contains(p)) return i;
            return -1;
        }
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
                            HplKeyConfig keyConfig = null)
        {
            _loadFade.color = Color.clear; _loadingText.Clear();
            _menuText.Clear();
            _guiText.Clear();
            _buttons.Clear();
            _buttonData.Clear();
            _sliders.Clear();
            _pieceIndex = 0;
            _selected = selected;
            _opacity = opacity;
            _shade.color = new Color(0, 0, 0, _titleScreen ? 0 : 0.5f * opacity);
            _logo.enabled = _titleScreen && _logo.texture != null && !confirmQuit && !options && saves == null && keyConfig == null;
            if (_logo.enabled)
            {
                float width = Screen.height > 0 ? Screen.width * 600f / Screen.height : 800;
                Vector2 pos = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "MainMenuLogoStartRelativePos", new Vector2(0.0525f, 0.22f)) : new Vector2(0.0525f, 0.22f);
                Vector2 size = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "MainMenuLogoRelativeSize", Vector2.one * 0.47f) : Vector2.one * 0.47f;
                float height = size.y * 600;
                Place(_logo.rectTransform, new Rect(pos.x * width - (width - 800) * 0.5f, pos.y * 600, height * _logo.texture.width / _logo.texture.height, height));
                _logo.color = new Color(1, 1, 1, opacity);
            }
            if (confirmQuit) Confirmation(confirmation);
            else if (keyConfig != null) KeyConfig(keyConfig);
            else if (options) Options(tab, settings);
            else if (saves != null) LoadGame(saves, selectedSave);
            else Main();
            for (int i = _pieceIndex; i < _pieces.Count; i++) _pieces[i].enabled = false;
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

        void Main()
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
            if (_titleScreen)
            {
                if (HasSave) Add("Continue", "Продолжить", MenuControl.Continue);
                Add("Start Game", "Новая игра", MenuControl.NewGame);
                if (HasSave) Add("Load Game", "Загрузить игру", MenuControl.LoadGame);
                Add("Options", "Настройки", MenuControl.Options);
                Add("Exit", "Выход", MenuControl.Quit);
            }
            else
            {
                Add("Back To Game", "Вернуться в игру", MenuControl.Resume);
                Add("Options", "Настройки", MenuControl.Options);
                Add("ExitToMainMenu", "В главное меню", MenuControl.ReturnTitle);
                if (_map.player == null || !_map.player.IsDead) Add("ExitAndSave", "Сохранить и выйти", MenuControl.ExitAndSave);
                Add("Exit", "Выход", MenuControl.Quit);
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
                Button(new Rect(pos.x - width / 2 - 8, y, width + 16, size.y), labels[i], controls[i], _menuText, size);
            }
        }

        void Confirmation(MenuControl action)
        {
            Window(new Rect(165, 205, 470, 215));
            var size = Vector2.one * 18;
            var question = action switch
            {
                MenuControl.ConfirmNewGame => T("MainMenu", "Start a new game?", "Начать новую игру?"),
                MenuControl.ConfirmReturnTitle => T("MainMenu", "Sure you want to exit to main menu?", "Выйти в главное меню?"),
                MenuControl.ConfirmContinue => T("MainMenu", "Continue old game?", "Продолжить прошлую игру?"),
                MenuControl.ConfirmExitAndSave => T("MainMenu", "Sure you want to exit and save?", "Сохранить и выйти в главное меню?"),
                MenuControl.ConfirmLoad => T("LoadGame", "LoadGameMessage", "Загрузить сохранённую игру?"),
                MenuControl.ConfirmKeyDefaults => T("KeyConfig", "LoadDefaultsMessage", "Применить раскладку по умолчанию?"),
                MenuControl.ConfirmKeyInvalid => T("KeyConfig", "InvalidInputMessage", "Некоторые клавиши повторяются. Сохранить?"),
                _ => T("MainMenu", "Sure you want to quit?", "Выйти из игры?"),
            };
            var rows = _guiText.font != null ? _guiText.font.GetWordWrapRows(410, size, question) : new List<string> { question };
            for (int i = 0; i < rows.Count; i++) Draw(_guiText, rows[i], new Vector2(400, 235 + i * 22), size, Color.white, HplFontAlign.Center);
            bool okCancel = action is MenuControl.ConfirmLoad or MenuControl.ConfirmKeyDefaults or MenuControl.ConfirmKeyInvalid;
            Button(new Rect(260, 365, 120, 28), okCancel ? T("Global", "OK", "OK") : T("MainMenu", "Yes", "Да"), action);
            Button(new Rect(420, 365, 120, 28), okCancel ? T("Global", "Cancel", "Отмена") : T("MainMenu", "No", "Нет"), MenuControl.Cancel);
        }

        /// <summary>cLuxMainMenu_LoadGame: the saves, newest first; a click selects, OK loads after a confirmation.</summary>
        void LoadGame(IReadOnlyList<string> saves, int selectedSave)
        {
            Window(new Rect(90, 70, 620, 460));
            Draw(_guiText, T("LoadGame", "LoadGameTitle", "Загрузить игру"), new Vector2(125, 50), Vector2.one * 25, Color.white);
            var size = Vector2.one * 16;
            for (int i = 0; i < saves.Count && i < 20; i++)
            {
                Rect rect = new(110, 90 + i * 19, 580, 18);
                if (i == selectedSave) Piece(null, rect, new Color(1, 1, 1, 0.15f));
                Button(rect, saves[i], MenuControl.LoadSlot, null, size, HplFontAlign.Left);
                _buttonData[_buttonData.Count - 1] = i;
            }
            if (saves.Count == 0)
                Draw(_guiText, T("LoadGame", "NoGame", "Нет игры для загрузки"), new Vector2(400, 200), Vector2.one * 18, Color.white, HplFontAlign.Center);
            Button(new Rect(500, 485, 80, 30), T("Global", "OK", "OK"), MenuControl.LoadOk);
            Button(new Rect(590, 485, 95, 30), T("Global", "Cancel", "Отмена"), MenuControl.Cancel);
        }

        void Options(int tab, HplUserSettings settings)
        {
            Window(new Rect(90, 70, 620, 460));
            Draw(_guiText, T("MainMenu", "Options", "Настройки"), new Vector2(125, 50), Vector2.one * 25, Color.white);
            string[] tabs = { "OptionsGame", "OptionsGraphics", "OptionsSound", "OptionsInput" };
            string[] fallbacks = { "Игра", "Графика", "Звук", "Управление" };
            for (int i = 0; i < 4; i++)
            {
                Rect rect = new(115 + i * 140, 117, 130, 28);
                Button(rect, T("MainMenu", tabs[i], fallbacks[i]), (MenuControl)((int)MenuControl.GameTab + i));
                if (i == tab) Piece(null, new Rect(rect.x, rect.yMax + 3, rect.width, 1), new Color(1, 1, 1, 0.7f));
            }
            switch (tab)
            {
                case 0:
                    Toggle(195, "ShowSubtitles", "Субтитры", settings.subtitles, MenuControl.Subtitles);
                    Toggle(245, "ShowHints", "Подсказки", settings.hints, MenuControl.Hints);
                    Toggle(295, "ShowCrosshair", "Прицел", settings.crosshair, MenuControl.Crosshair);
                    // menu.cfg Options ShowCommentary shows the developer commentary option.
                    if (_map.menuConfig == null || _map.menuConfig.GetString("Options", "ShowCommentary", "false") == "true")
                        Toggle(345, "Commentary", "Комментарии", settings.commentary, MenuControl.Commentary);
                    break;
                case 1:
                    Toggle(195, "FullScreen", "Полный экран", settings.fullscreen, MenuControl.Fullscreen);
                    Toggle(245, "VSync", "Вертикальная синхронизация", settings.vSync, MenuControl.VSync);
                    Adjust(305, "Gamma", "Гамма", settings.gamma.ToString("0.00", CultureInfo.InvariantCulture),
                           (settings.gamma - HplUserSettings.MinGamma) / (HplUserSettings.MaxGamma - HplUserSettings.MinGamma), MenuControl.GammaLess, MenuControl.GammaMore);
                    break;
                case 2:
                    Adjust(200, "Volume", "Общая громкость", Mathf.RoundToInt(settings.masterVolume * 100) + "%", settings.masterVolume, MenuControl.VolumeLess, MenuControl.VolumeMore);
                    break;
                case 3:
                    Toggle(195, "InvertMouse", "Инверсия мыши", settings.invertMouse, MenuControl.InvertMouse);
                    Toggle(245, "SmoothMouse", "Сглаживание мыши", settings.smoothMouse, MenuControl.SmoothMouse);
                    Adjust(305, "MouseSensitivity", "Чувствительность мыши", settings.mouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture), (settings.mouseSensitivity - 0.2f) / 4.8f, MenuControl.SensitivityLess, MenuControl.SensitivityMore);
                    // cLuxMainMenu_Options: the key config button under the input settings.
                    string keyConfig = T("OptionsMenu", "KeyConfigButton", "Конфигурация клавиш");
                    float keyWidth = _guiText.font != null ? _guiText.font.GetLength(Vector2.one * 18, keyConfig) : 200;
                    Button(new Rect(140, 380, keyWidth + 20, 25), keyConfig, MenuControl.KeyConfig, null, default, HplFontAlign.Left);
                    break;
            }
            Button(new Rect(500, 485, 80, 30), T("MainMenu", "OK", "OK"), MenuControl.Apply);
            Button(new Rect(590, 485, 95, 30), T("MainMenu", "Cancel", "Отмена"), MenuControl.Cancel);
        }

        /// <summary>
        /// cLuxMainMenu_KeyConfig::CreateGui (620 × 460 window): the category selector, the action / primary /
        /// secondary headers (font × 1.25), a row per action of the category (inputs 140 × 16, 10 apart, primary at
        /// 240, secondary 150 further), empty or shared inputs red, the tooltip of the selected row, "Default keys",
        /// OK and Cancel. A slot waiting for an input shows "...".
        /// </summary>
        void KeyConfig(HplKeyConfig keys)
        {
            var window = new Rect(90, 70, 620, 460);
            Window(window);
            Draw(_guiText, T("KeyConfig", "KeyConfigTitle", "Конфигурация клавиш"), new Vector2(125, 50), Vector2.one * 25, Color.white);
            var header = Vector2.one * 18 * 1.25f;
            var size = Vector2.one * 16;
            float x = window.x + 30, y = window.y + 85;
            string category = T("KeyConfig", "Category", "Категория");
            Draw(_guiText, category, new Vector2(x, y), header, Color.white);
            float comboX = x + (_guiText.font != null ? _guiText.font.GetLength(header, category) : 100) + 10;
            string[] categories = { T("KeyConfig", "Movement", "Движение"), T("KeyConfig", "Actions", "Действия"), T("KeyConfig", "Misc", "Разное") };
            var combo = new Rect(comboX, y, 175, 25);
            Piece(null, combo, new Color(0, 0, 0, 0.5f));
            Button(combo, categories[Mathf.Clamp(keys.Category, 0, 2)] + "  >", MenuControl.KeyCategory);

            x += 15;
            y += 25 + 5;
            Draw(_guiText, T("KeyConfig", "HeaderAction", "Действие"), new Vector2(x, y), header, Color.white);
            Draw(_guiText, T("KeyConfig", "HeaderPrimary", "Основная"), new Vector2(x + 240 + 70, y), header, Color.white, HplFontAlign.Center);
            Draw(_guiText, T("KeyConfig", "HeaderSecondary", "Дополнительная"), new Vector2(x + 390 + 70, y), header, Color.white, HplFontAlign.Center);
            y += header.y + 10;

            int selectedEntry = -1;
            foreach (int i in keys.EntriesOf(keys.Category))
            {
                var e = keys.Entries[i];
                Draw(_guiText, T("Actions", e.name, e.name), new Vector2(x, y), size, Color.white);
                for (int s = 0; s < 2; s++)
                {
                    var rect = new Rect(x + (s == 0 ? 240 : 390), y, 140, 16);
                    bool waiting = keys.WaitingEntry == i && keys.WaitingSlot == s;
                    Piece(null, rect, waiting ? new Color(1, 1, 1, 0.25f) : new Color(0, 0, 0, 0.45f));
                    string name = HplKeyBindings.ButtonName(e.inputs[s]);
                    string label = waiting ? "..." : name == "" ? "" : T("ButtonNames", name, name);
                    int index = _buttons.Count;
                    _buttons.Add((rect, MenuControl.KeySlot));
                    _buttonData.Add(i * 2 + s);
                    var color = e.invalid[s] ? Color.red : index == _selected ? Highlight : Color.white;
                    Draw(_guiText, label, new Vector2(rect.center.x, rect.y), size, color, HplFontAlign.Center);
                    if (index == _selected) selectedEntry = i;
                }
                y += 16 + 10;
            }
            string tip = selectedEntry >= 0 ? T("Actions", keys.Entries[selectedEntry].name + "Tooltip", "") : "";

            // Tip label (400 × 30, black 0.5 background) and the buttons along the bottom.
            float bottom = window.y + window.height - 25 - 10;
            if (tip != "")
            {
                Piece(null, new Rect(window.x + 40, bottom, 400, 30), new Color(0, 0, 0, 0.5f));
                var rows = _guiText.font != null ? _guiText.font.GetWordWrapRows(396, Vector2.one * 14, tip) : new List<string> { tip };
                for (int r = 0; r < rows.Count && r < 2; r++) Draw(_guiText, rows[r], new Vector2(window.x + 42, bottom + r * 15), Vector2.one * 14, Color.white);
            }
            string defaults = T("KeyConfig", "SetDefault", "Задать клавиши по умолчанию");
            float defaultsWidth = (_guiText.font != null ? _guiText.font.GetLength(Vector2.one * 18, defaults) : 200) + 20;
            Button(new Rect(window.x + 40, bottom - 35, defaultsWidth, 25), defaults, MenuControl.KeyDefaults);
            float okX = window.x + window.width - 80 * 2 - 3 - 5;
            Button(new Rect(okX, bottom, 80, 30), T("Global", "OK", "OK"), MenuControl.KeyOk);
            Button(new Rect(okX + 83, bottom, 80, 30), T("Global", "Cancel", "Отмена"), MenuControl.Cancel);
        }

        void Toggle(float y, string key, string fallback, bool value, MenuControl control)
        {
            Piece(value ? "CheckBoxEnabledChecked" : "CheckBoxEnabledUnchecked", new Rect(140, y + 2, 18, 18), Color.white);
            Button(new Rect(175, y, 470, 28), T("OptionsMenu", key, fallback), control, null, default, HplFontAlign.Left);
            _buttons[_buttons.Count - 1] = (new Rect(135, y, 510, 28), control);
        }

        void Adjust(float y, string key, string fallback, string value, float fraction, MenuControl less, MenuControl more)
        {
            Draw(_guiText, T("OptionsMenu", key, fallback), new Vector2(140, y), Vector2.one * 18, Color.white);
            Button(new Rect(440, y, 30, 28), "<", less);
            Button(new Rect(620, y, 30, 28), ">", more);
            Draw(_guiText, value, new Vector2(545, y), Vector2.one * 18, Color.white, HplFontAlign.Center);
            Piece("SliderHoriBackground", new Rect(475, y + 34, 140, 6), Color.white);
            Piece("SliderHoriArrowUp", new Rect(475 + Mathf.Clamp01(fraction) * 128, y + 29, 12, 18), Color.white);
            _sliders.Add((new Rect(475, y + 25, 140, 28), less switch
            {
                MenuControl.VolumeLess => MenuControl.VolumeSlider,
                MenuControl.GammaLess => MenuControl.GammaSlider,
                _ => MenuControl.SensitivitySlider,
            }));
        }

        void Button(Rect rect, string label, MenuControl action, HplGuiText layer = null, Vector2 size = default, HplFontAlign align = HplFontAlign.Center)
        {
            int index = _buttons.Count;
            _buttons.Add((rect, action));
            _buttonData.Add(-1);
            if (size == default) size = Vector2.one * 18;
            Draw(layer != null ? layer : _guiText, label, new Vector2(align == HplFontAlign.Left ? rect.x : rect.center.x, rect.y), size, index == _selected ? Highlight : Color.white, align);
        }

        void Window(Rect r)
        {
            Piece("WindowBackground", r, Color.white);
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

        void Piece(string type, Rect rect, Color color)
        {
            if (_pieceIndex == _pieces.Count)
            {
                var image = Image(_screen, "Skin piece");
                image.transform.SetSiblingIndex(_menuText.transform.GetSiblingIndex());
                _pieces.Add(image);
            }
            var target = _pieces[_pieceIndex++];
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
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root); else Object.DestroyImmediate(_root);
        }
    }
}
