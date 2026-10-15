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
        VolumeSlider, SensitivitySlider,
    }

    /// <summary>In-game cLuxMainMenu: menu.cfg layout and gui_main_menu.skin on the same Canvas system as the HUD.</summary>
    public sealed class HplPauseMenu
    {
        readonly HplMap _map;
        readonly HplMenuAssets _assets;
        readonly GameObject _root;
        readonly RectTransform _screen;
        readonly HplGuiText _menuText, _guiText;
        readonly RawImage _shade, _cursor;
        readonly List<RawImage> _pieces = new();
        readonly List<(Rect rect, MenuControl control)> _buttons = new();
        readonly List<(Rect rect, MenuControl control)> _sliders = new();
        int _pieceIndex, _selected;
        float _opacity;
        static readonly Color Highlight = new(232f / 255f, 201f / 255f, 28f / 255f, 1f);
        public int ButtonCount => _buttons.Count;
        public bool HasCursor => _cursor.texture != Texture2D.whiteTexture;
        public MenuControl ControlAt(int index) => _buttons[index].control;

        public HplPauseMenu(HplMap map, Transform parent)
        {
            _map = map;
            _assets = Resources.Load<HplMenuAssets>(HplMenuAssets.ResourceName);
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
            _menuText = Text("Menu font", _assets != null ? _assets.menuFont : map.hud.defaultFont);
            _guiText = Text("GUI font", _assets != null ? _assets.guiFont : map.hud.defaultFont);
            _cursor = Image(_screen, "Original menu cursor");
            var pointer = _assets != null ? _assets.Find("PointerNormal").texture : null;
            _cursor.texture = pointer != null ? pointer : Texture2D.whiteTexture;
            _cursor.enabled = pointer != null;
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
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, position, null, out var p);
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

        public void Refresh(bool confirmQuit, bool options, int tab, HplUserSettings settings, int selected, float opacity, Vector2 pointer)
        {
            _menuText.Clear();
            _guiText.Clear();
            _buttons.Clear();
            _sliders.Clear();
            _pieceIndex = 0;
            _selected = selected;
            _opacity = opacity;
            _shade.color = new Color(0, 0, 0, 0.5f * opacity);
            if (confirmQuit) Confirmation();
            else if (options) Options(tab, settings);
            else Main();
            for (int i = _pieceIndex; i < _pieces.Count; i++) _pieces[i].enabled = false;
            if (HasCursor)
            {
                var p = VirtualPoint(pointer);
                Place(_cursor.rectTransform, new Rect(p.x, p.y, _cursor.texture.width, _cursor.texture.height));
                _cursor.transform.SetAsLastSibling();
            }
        }

        void Main()
        {
            Vector2 size = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "TopMenuFontRelativeSize", Vector2.one * 0.05f) : Vector2.one * 0.05f;
            size = Vector2.Scale(size, new Vector2(800, 600));
            Vector2 pos = _map.menuConfig != null ? _map.menuConfig.GetVector2("Main", "TopMenuStartRelativePosInGame", Vector2.one * 0.5f) : Vector2.one * 0.5f;
            pos = Vector2.Scale(pos, new Vector2(800, 600));
            string[] labels = { T("MainMenu", "Back To Game", "Вернуться в игру"), T("MainMenu", "Options", "Настройки"), T("MainMenu", "Exit", "Выход") };
            var controls = new[] { MenuControl.Resume, MenuControl.Options, MenuControl.Quit };
            float maxWidth = 0;
            foreach (var label in labels) maxWidth = Mathf.Max(maxWidth, _menuText.font != null ? _menuText.font.GetLength(size, label) : 0);
            float virtualWidth = Screen.height > 0 ? Screen.width * 600f / Screen.height : 800;
            size *= Mathf.Clamp(maxWidth > 0 ? virtualWidth * 0.35f / maxWidth : 1, 0.7f, 1);
            for (int i = 0; i < labels.Length; i++)
            {
                float y = pos.y + i * size.y * 1.3f;
                float width = _menuText.font != null ? _menuText.font.GetLength(size, labels[i]) : 300;
                Button(new Rect(pos.x - width / 2 - 8, y, width + 16, size.y), labels[i], controls[i], _menuText, size);
            }
        }

        void Confirmation()
        {
            Window(new Rect(165, 225, 470, 170));
            var size = Vector2.one * 18;
            var question = T("MainMenu", "Sure you want to quit?", "Выйти из игры?");
            var rows = _guiText.font != null ? _guiText.font.GetWordWrapRows(410, size, question) : new List<string> { question };
            for (int i = 0; i < rows.Count; i++) Draw(_guiText, rows[i], new Vector2(400, 255 + i * 22), size, Color.white, HplFontAlign.Center);
            Button(new Rect(260, 340, 120, 28), T("MainMenu", "Yes", "Да"), MenuControl.ConfirmQuit);
            Button(new Rect(420, 340, 120, 28), T("MainMenu", "No", "Нет"), MenuControl.Cancel);
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
                    break;
                case 1:
                    Toggle(195, "FullScreen", "Полный экран", settings.fullscreen, MenuControl.Fullscreen);
                    Toggle(245, "VSync", "Вертикальная синхронизация", settings.vSync, MenuControl.VSync);
                    break;
                case 2:
                    Adjust(200, "Volume", "Общая громкость", Mathf.RoundToInt(settings.masterVolume * 100) + "%", settings.masterVolume, MenuControl.VolumeLess, MenuControl.VolumeMore);
                    break;
                case 3:
                    Toggle(195, "InvertMouse", "Инверсия мыши", settings.invertMouse, MenuControl.InvertMouse);
                    Toggle(245, "SmoothMouse", "Сглаживание мыши", settings.smoothMouse, MenuControl.SmoothMouse);
                    Adjust(305, "MouseSensitivity", "Чувствительность мыши", settings.mouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture), (settings.mouseSensitivity - 0.2f) / 4.8f, MenuControl.SensitivityLess, MenuControl.SensitivityMore);
                    break;
            }
            Button(new Rect(500, 485, 80, 30), T("MainMenu", "OK", "OK"), MenuControl.Apply);
            Button(new Rect(590, 485, 95, 30), T("MainMenu", "Cancel", "Отмена"), MenuControl.Cancel);
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
            _sliders.Add((new Rect(475, y + 25, 140, 28), less == MenuControl.VolumeLess ? MenuControl.VolumeSlider : MenuControl.SensitivitySlider));
        }

        void Button(Rect rect, string label, MenuControl action, HplGuiText layer = null, Vector2 size = default, HplFontAlign align = HplFontAlign.Center)
        {
            int index = _buttons.Count;
            _buttons.Add((rect, action));
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
