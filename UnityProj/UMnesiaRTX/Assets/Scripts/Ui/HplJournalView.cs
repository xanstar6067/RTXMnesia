using System.Collections.Generic;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>Original journal papers, fonts and coordinates on the shared 800×600 virtual screen.</summary>
    public sealed class HplJournalView
    {
        struct Button { public Rect rect; public JournalCommand command; public HplGame.JournalEntry entry; }
        readonly HplMap _map;
        readonly HplJournal _journal;
        readonly GameObject _root;
        readonly RectTransform _screen;
        readonly HplGuiText _text, _menu;
        readonly RawImage _background, _cursor, _flash, _transitionShade;
        readonly List<RawImage> _images = new();
        readonly List<Button> _buttons = new();
        int _usedImages;
        float _alpha;
        public int ButtonCount => _buttons.Count;
        public bool HasCursor => _cursor.texture != null && _cursor.texture != Texture2D.whiteTexture;
        public Canvas Canvas => _root.GetComponent<Canvas>();
        HplJournalAssets Assets => _journal.Assets;

        public HplJournalView(HplMap map, HplJournal journal)
        {
            _map = map; _journal = journal;
            _root = new GameObject("HPL journal", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)) { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(map.transform, false);
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay; Canvas.sortingOrder = 200;
            var scaler = _root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600); scaler.matchWidthOrHeight = 1;
            _screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            _screen.SetParent(_root.transform, false);
            _screen.anchorMin = _screen.anchorMax = _screen.pivot = Vector2.one * 0.5f; _screen.sizeDelta = new Vector2(800, 600);
            _background = Image(_screen, "Journal paper");
            _text = Text("Journal text", Assets != null ? Assets.textFont : map.hud.defaultFont);
            _menu = Text("Journal headers", Assets != null ? Assets.menuFont : map.hud.defaultFont);
            _transitionShade = Image(_root.transform, "Inventory transition fade");
            _transitionShade.rectTransform.anchorMin = Vector2.zero; _transitionShade.rectTransform.anchorMax = Vector2.one;
            _transitionShade.rectTransform.offsetMin = _transitionShade.rectTransform.offsetMax = Vector2.zero;
            _flash = Image(_root.transform, "Sanity gain flash");
            _flash.rectTransform.anchorMin = Vector2.zero; _flash.rectTransform.anchorMax = Vector2.one;
            _flash.rectTransform.offsetMin = _flash.rectTransform.offsetMax = Vector2.zero;
            _cursor = Image(_screen, "Journal cursor");
            var menus = Resources.Load<HplMenuAssets>(HplMenuAssets.ResourceName);
            _cursor.texture = menus != null ? menus.Find("PointerNormal").texture : Assets?.Texture("gui_def_pointer_normal.tga");
            _cursor.enabled = HasCursor;
            _root.SetActive(false);
        }
        public void SetVisible(bool value) => _root.SetActive(value);
        Vector2 VirtualPoint(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, position, Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Canvas.worldCamera, out var p);
            return new Vector2(p.x + 400, 300 - p.y);
        }
        public int HitTest(Vector2 position)
        {
            var p = VirtualPoint(position);
            for (int i = _buttons.Count - 1; i >= 0; i--) if (_buttons[i].rect.Contains(p)) return i;
            return -1;
        }
        public void Activate(int index)
        {
            if (index < 0 || index >= _buttons.Count) return;
            var button = _buttons[index]; _journal.Activate(button.command, button.entry);
        }
        public void Refresh(float opacity, Vector2 mouse)
        {
            _alpha = opacity * _journal.StateOpacity;
            _text.Clear(); _menu.Clear(); _buttons.Clear(); _usedImages = 0;
            string paper = _journal.State switch {
                JournalState.Main => "bg_main.tga", JournalState.Notes => "bg_notes.tga", JournalState.Diaries => "bg_diaries.tga", JournalState.QuestLog => "bg_questlog.tga",
                _ => _journal.CurrentEntry?.ImageFile(false),
            };
            var texture = Assets?.Texture(paper);
            if (texture == null && _journal.IsReading) texture = Assets?.Texture("default_note_large.tga");
            _background.enabled = texture != null;
            if (texture != null)
            {
                _background.texture = texture; _background.color = new Color(1, 1, 1, _alpha);
                Place(_background.rectTransform, new Rect(400 - texture.width * 0.5f, 300 - texture.height * 0.5f, texture.width, texture.height));
            }
            if (_journal.State == JournalState.Main)
            {
                MenuButton(_journal.T("Notes"), new Rect(300, 200, 200, 30), 30, JournalCommand.Notes);
                MenuButton(_journal.T("Diaries"), new Rect(300, 250, 200, 30), 30, JournalCommand.Diaries);
                MenuButton(_journal.T("Quest Log"), new Rect(300, 300, 200, 30), 30, JournalCommand.Quests);
                MenuButton(_journal.T("MainBack"), new Rect(300, 530, 200, 30), 28, JournalCommand.Back);
            }
            else if (_journal.IsReading) Reading();
            else List();
            for (int i = _usedImages; i < _images.Count; i++) _images[i].enabled = false;
            if (HasCursor) Place(_cursor.rectTransform, new Rect(VirtualPoint(mouse), new Vector2(_cursor.texture.width, _cursor.texture.height)));
            _cursor.color = new Color(1, 1, 1, opacity);
            // LuxJournal::OnDraw covers inventory transitions with black at 1 - alpha².
            _transitionShade.color = new Color(0, 0, 0, _journal.OpenedFromInventory ? 1 - opacity * opacity : 0);
            _flash.color = _map.effects != null ? new Color(_map.effects.SanityGainFlash.Color.r, _map.effects.SanityGainFlash.Color.g, _map.effects.SanityGainFlash.Color.b, _map.effects.SanityGainFlash.Alpha) : Color.clear;
        }
        void Reading()
        {
            Label(_menu, _journal.Header, new Vector2(400, _journal.F("NoteHeaderStartY", 60)), _journal.V("NoteHeaderFontSize", Vector2.one * 25), HplFontAlign.Center);
            if (_journal.Pages.Count > 0)
            {
                var rows = _journal.Pages[_journal.PageIndex].rows;
                float y = 300 - _journal.RowDistance * rows.Count * 0.5f;
                foreach (string row in rows)
                {
                    Label(_text, row, new Vector2(400 - _journal.TextWidth * 0.5f, y), _journal.FontSize, HplFontAlign.Left);
                    y += _journal.RowDistance;
                }
            }
            if (_journal.IsNarrated) return;
            Navigation();
            var back = Assets?.Texture("note_return.tga");
            if (back != null) GraphicButton(back, new Rect(400 - back.width * 0.5f, 530, back.width, back.height), JournalCommand.Back);
        }
        void List()
        {
            string key = _journal.State == JournalState.Notes ? "Notes" : _journal.State == JournalState.Diaries ? "Diaries" : "Quest Log";
            Label(_menu, _journal.T(key), new Vector2(400, 40), Vector2.one * 28, HplFontAlign.Center);
            if (_journal.VisibleEntries.Count == 0)
            {
                string empty = _journal.State == JournalState.Notes ? "NotesEmpty" : _journal.State == JournalState.Diaries ? "DiariesEmpty" : "QuestsEmpty";
                Label(_text, _journal.T(empty), new Vector2(400, 270), Vector2.one * 24, HplFontAlign.Center);
            }
            foreach (var item in _journal.VisibleEntries)
            {
                if (item.entry != null)
                {
                    var icon = Assets?.Texture(item.entry.ImageFile(true));
                    if (icon != null) Draw(icon, new Rect(225, item.y + 2, 20, 20));
                    MenuButton(_journal.T(item.entry.NameEntry), new Rect(250, item.y, 330, 28), 24, JournalCommand.Entry, item.entry, true);
                }
                else
                {
                    Label(_text, "-", new Vector2(135, item.y), Vector2.one * 19, HplFontAlign.Left);
                    for (int i = 0; i < item.rows.Count; i++) Label(_text, item.rows[i], new Vector2(150, item.y + i * 21), Vector2.one * 19, HplFontAlign.Left);
                }
            }
            Navigation();
            MenuButton(_journal.T("MainBack"), new Rect(350, 530, 100, 30), 28, JournalCommand.Back);
        }
        void Navigation()
        {
            float y = _journal.F("NoteTextStartY", 105) + (_journal.MaxRows + 1) * _journal.RowDistance;
            if (_journal.HasPrevious) GraphicButton(Assets?.Texture("note_backward.tga"), new Rect(400 - _journal.TextWidth * 0.5f + 80, y, 65, 46), JournalCommand.Previous);
            if (_journal.HasNext) GraphicButton(Assets?.Texture("note_forward.tga"), new Rect(400 + _journal.TextWidth * 0.5f - 145, y, 65, 46), JournalCommand.Next);
        }
        void MenuButton(string value, Rect rect, float size, JournalCommand command, HplGame.JournalEntry entry = null, bool left = false)
        {
            int index = _buttons.Count;
            _buttons.Add(new Button { rect = rect, command = command, entry = entry });
            var font = left ? _text : _menu;
            Vector2 position = new(left ? rect.x : rect.center.x, rect.y);
            var align = left ? HplFontAlign.Left : HplFontAlign.Center;
            Label(font, value, position, Vector2.one * size, align);
            if (_journal.Selected == index)
            {
                float pulse = 0.3f + (Mathf.Sin(Time.unscaledTime * 2.5f) + 1) * 0.35f;
                float add = 1 + (1 - pulse) * 5;
                var larger = new Vector2(size + add, size + 4);
                float offset = left && font.font != null ? (font.font.GetLength(larger, value) - font.font.GetLength(Vector2.one * size, value)) * 0.5f : add * 0.5f;
                font.DrawFont(value, position - new Vector2(offset, 2), larger, new Color(1, 0, 0, _alpha * pulse), align);
            }
        }
        void GraphicButton(Texture2D texture, Rect rect, JournalCommand command)
        {
            if (texture == null) return;
            _buttons.Add(new Button { rect = rect, command = command }); Draw(texture, rect);
        }
        void Draw(Texture texture, Rect rect)
        {
            if (_usedImages == _images.Count)
            {
                var image = Image(_screen, "Journal graphic"); image.transform.SetSiblingIndex(_text.transform.GetSiblingIndex()); _images.Add(image);
            }
            var target = _images[_usedImages++]; target.texture = texture; target.color = new Color(1, 1, 1, _alpha); target.enabled = true;
            Place(target.rectTransform, rect);
        }
        void Label(HplGuiText text, string value, Vector2 position, Vector2 size, HplFontAlign align) => text.DrawFont(value, position, size, new Color(1, 1, 1, _alpha), align);
        HplGuiText Text(string name, HplFont font)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(_screen, false); Place(go.GetComponent<RectTransform>(), new Rect(0, 0, 800, 600));
            var text = go.AddComponent<HplGuiText>(); text.font = font; text.raycastTarget = false; return text;
        }
        static RawImage Image(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>(); image.raycastTarget = false; return image;
        }
        static void Place(RectTransform rect, Rect r)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(r.x, -r.y); rect.sizeDelta = r.size;
        }
        public void Destroy() { if (_root == null) return; if (Application.isPlaying) Object.Destroy(_root); else Object.DestroyImmediate(_root); }
    }
}
