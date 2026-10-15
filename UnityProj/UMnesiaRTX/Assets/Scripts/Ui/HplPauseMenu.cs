using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// In-game MainMenu view. Reuses the imported game font and language without scene changes or system fonts.
    /// UI geometry uses the same centered 800×600, height-scaled coordinate system as the HUD.
    /// </summary>
    public sealed class HplPauseMenu
    {
        readonly HplMap _map;
        readonly GameObject _root;
        readonly RectTransform _screen;
        readonly HplGuiText _text;
        readonly RawImage _selection;
        static readonly Rect[] Buttons = { new(210f, 275f, 380f, 42f), new(210f, 333f, 380f, 42f) };
        static readonly Color TextColor = new(0.75f, 0.71f, 0.62f, 1f);
        static readonly Color SelectedColor = new(1f, 0.93f, 0.76f, 1f);

        public HplPauseMenu(HplMap map, Transform parent)
        {
            _map = map;
            _root = new GameObject("HPL game menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _root.hideFlags = HideFlags.DontSave;
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 1f;

            var background = Image(_root.transform, "Backdrop", new Color(0.015f, 0.012f, 0.01f, 0.93f));
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;

            _screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            _screen.SetParent(_root.transform, false);
            _screen.anchorMin = _screen.anchorMax = _screen.pivot = new Vector2(0.5f, 0.5f);
            _screen.sizeDelta = new Vector2(800f, 600f);
            _selection = Image(_screen, "Selection", new Color(0.7f, 0.57f, 0.33f, 0.09f));
            var textObject = new GameObject("Menu text", typeof(RectTransform));
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(_screen, false);
            Place(textRect, new Rect(0f, 0f, 800f, 600f));
            _text = textObject.AddComponent<HplGuiText>();
            _text.font = map.hud.defaultFont;
            _text.raycastTarget = false;
            _root.SetActive(false);
        }

        public void SetVisible(bool visible) => _root.SetActive(visible);

        public int HitTest(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, screenPosition, null, out var p)) return -1;
            var virtualPosition = new Vector2(p.x + 400f, 300f - p.y);
            for (int i = 0; i < Buttons.Length; i++) if (Buttons[i].Contains(virtualPosition)) return i;
            return -1;
        }

        public void Refresh(bool confirmingQuit, int selected)
        {
            _text.Clear();
            Draw("RTXMnesia", 400f, 125f, 38f, SelectedColor);
            string mapName = string.IsNullOrEmpty(_map.DisplayNameEntry) ? "" : _map.hud.Translate("Levels", _map.DisplayNameEntry);
            Draw(mapName, 400f, 179f, 17f, TextColor);
            if (confirmingQuit)
            {
                string question = Translate("Sure you want to quit?", "Выйти из игры?");
                var rows = _text.font != null ? _text.font.GetWordWrapRows(560f, Vector2.one * 20f, question)
                                              : new System.Collections.Generic.List<string> { question };
                for (int i = 0; i < rows.Count; i++) Draw(rows[i], 400f, 214f + i * 23f, 20f, TextColor);
            }
            string[] labels = confirmingQuit
                ? new[] { Translate("Yes", "Да"), Translate("No", "Нет") }
                : new[] { Translate("Back To Game", "Вернуться в игру"), Translate("Exit", "Выход") };
            for (int i = 0; i < Buttons.Length; i++)
                Draw(labels[i], 400f, Buttons[i].y + 7f, 26f, i == selected ? SelectedColor : TextColor);
            Place(_selection.rectTransform, Buttons[selected]);
        }

        string Translate(string key, string fallback) =>
            _map.hud.TryTranslate("MainMenu", key, out var text) ? text : fallback;

        void Draw(string text, float x, float y, float size, Color color) =>
            _text.DrawFont(text, new Vector2(x, y), Vector2.one * size, color, HplFontAlign.Center);

        static RawImage Image(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>();
            image.texture = Texture2D.whiteTexture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Place(RectTransform rect, Rect position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = position.size;
        }

        public void Destroy()
        {
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root);
            else Object.DestroyImmediate(_root);
        }
    }
}
