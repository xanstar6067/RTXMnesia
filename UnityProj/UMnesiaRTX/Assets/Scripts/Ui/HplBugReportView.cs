using System;
using System.Collections.Generic;
using System.Text;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// The bug report window (own feature, <see cref="Dev.HplBugReport"/>): a required comment, OK / Cancel, in the
    /// gui_main_menu.skin look of the menu windows, on its own canvas over every other screen; after OK a short
    /// notice with the report folder. Enter — OK, Esc — Cancel, Ctrl+V pastes.
    /// </summary>
    public sealed class HplBugReportView
    {
        public enum Choice { None, Ok, Cancel }
        public const Key OpenKey = Key.F1;
        public const int MaxLength = 500;
        const float FontSize = 14, NoticeTime = 4f;
        const int Rows = 6;
        static readonly Rect WindowRect = new(140, 170, 520, 260);
        static readonly Color Highlight = new(232f / 255f, 201f / 255f, 28f / 255f, 1f), Grey = new(0.5f, 0.5f, 0.5f, 1f);

        readonly GameObject _root;
        readonly RectTransform _screen, _piecesParent;
        readonly HplMenuAssets _assets;
        readonly HplGuiText _text;
        readonly RawImage _cursor;
        readonly bool _english;
        readonly List<RawImage> _pieces = new();
        readonly List<char> _typed = new();
        readonly StringBuilder _comment = new();
        int _pieceIndex, _hover = -1;
        bool _open, _typingHooked;
        string _error, _notice;
        float _noticeUntil, _backspaceNext;
        Rect _ok, _cancel;

        public bool IsOpen => _open;
        public bool HasCursor => _cursor.texture != Texture2D.whiteTexture;
        public string Comment => _comment.ToString();
        /// <summary>The comment is required: something other than spaces.</summary>
        public bool CanConfirm => Comment.Trim().Length > 0;

        public HplBugReportView(HplMap map, Transform parent)
        {
            _english = map.language?.language?.StartsWith("eng", StringComparison.OrdinalIgnoreCase) == true;
            _assets = Resources.Load<HplMenuAssets>(map.Campaign.ResourceName(HplMenuAssets.ResourceName));
            _root = new GameObject("HPL bug report", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)) { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 1f;
            _screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            _screen.SetParent(_root.transform, false);
            _screen.anchorMin = _screen.anchorMax = _screen.pivot = Vector2.one * 0.5f;
            _screen.sizeDelta = new Vector2(800, 600);
            _piecesParent = new GameObject("Pieces", typeof(RectTransform)).GetComponent<RectTransform>();
            _piecesParent.SetParent(_screen, false);
            Place(_piecesParent, new Rect(0, 0, 800, 600));
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(_screen, false);
            Place(go.GetComponent<RectTransform>(), new Rect(0, 0, 800, 600));
            _text = go.AddComponent<HplGuiText>();
            _text.font = _assets != null ? _assets.guiFont : map.hud != null ? map.hud.defaultFont : null;
            _text.raycastTarget = false;
            _cursor = Image(_screen, "Menu cursor");
            var pointer = _assets != null ? _assets.Find("PointerNormal").texture : null;
            _cursor.texture = pointer != null ? pointer : Texture2D.whiteTexture;
            _cursor.enabled = false;
            _root.SetActive(false);
        }

        string T(string ru, string en) => _english ? en : ru;

        public void Open()
        {
            _open = true;
            _notice = null;
            _error = null;
            _comment.Clear();
            _typed.Clear();
            _hover = -1;
            var keyboard = Keyboard.current;
            if (keyboard != null && !_typingHooked)
            {
                keyboard.onTextInput += OnTextInput;
                _typingHooked = true;
            }
            _root.SetActive(true);
            Draw();
        }

        /// <summary>Closes the window; with the folder of a written report a notice shows it for a few seconds.</summary>
        public void Close(string savedFolder = null)
        {
            _open = false;
            if (_typingHooked && Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
            _typingHooked = false;
            _typed.Clear();
            string notice = null;
            if (savedFolder != null)
            {
                // Next to the game: the short form; the fallback folder is shown in full.
                string root = System.IO.Path.GetFullPath(Dev.HplBugReport.Root);
                string shown = savedFolder.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                    ? Dev.HplBugReport.FolderName + "/" + System.IO.Path.GetFileName(savedFolder) : savedFolder;
                notice = T("Отчёт сохранён: ", "Report saved: ") + shown;
            }
            _notice = notice;
            _noticeUntil = Time.unscaledTime + NoticeTime;
            _root.SetActive(notice != null);
            if (notice != null) Draw();
        }

        /// <summary>A failed write keeps the window open with the reason under the text box.</summary>
        public void ShowError(string error)
        {
            _error = error;
            Draw();
        }

        /// <summary>Hides the notice at once (a new report must not capture it in its screenshot).</summary>
        public void HideNotice()
        {
            _notice = null;
            if (!_open) _root.SetActive(false);
        }

        /// <summary>The notice after OK fades on its own; call each frame while the window is closed.</summary>
        public void UpdateNotice()
        {
            if (_open || _notice == null) return;
            if (Time.unscaledTime >= _noticeUntil) HideNotice();
            else Draw();
        }

        void OnTextInput(char c)
        {
            if (_open) _typed.Add(c);
        }

        public Choice Update()
        {
            if (!_open) return Choice.None;
            var keyboard = Keyboard.current;
            foreach (char c in _typed) Append(c.ToString());
            _typed.Clear();
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) return Choice.Cancel;
                if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) && CanConfirm) return Choice.Ok;
                if (keyboard.ctrlKey.isPressed && keyboard.vKey.wasPressedThisFrame) Append(GUIUtility.systemCopyBuffer);
                // Backspace repeats while held: 0.4 s, then every 40 ms.
                if (keyboard.backspaceKey.wasPressedThisFrame)
                {
                    Backspace(keyboard.ctrlKey.isPressed);
                    _backspaceNext = Time.unscaledTime + 0.4f;
                }
                else if (keyboard.backspaceKey.isPressed && Time.unscaledTime >= _backspaceNext)
                {
                    Backspace(false);
                    _backspaceNext = Time.unscaledTime + 0.04f;
                }
            }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var p = VirtualPoint(mouse.position.ReadValue());
                _hover = _ok.Contains(p) && CanConfirm ? 0 : _cancel.Contains(p) ? 1 : -1;
                if (mouse.leftButton.wasPressedThisFrame && _hover >= 0) return _hover == 0 ? Choice.Ok : Choice.Cancel;
            }
            Draw();
            return Choice.None;
        }

        /// <summary>Tests: the text as if typed.</summary>
        public void Type(string text) { Append(text); Draw(); }

        void Append(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            foreach (char raw in text)
            {
                // Pasted line breaks and tabs become spaces; other control characters (Ctrl+V's own) are dropped.
                char c = raw is '\n' or '\r' or '\t' ? ' ' : raw;
                if (char.IsControl(c) || _comment.Length >= MaxLength) continue;
                if (c == ' ' && (_comment.Length == 0 || _comment[_comment.Length - 1] == ' ')) continue;
                _comment.Append(c);
            }
            _error = null;
        }

        void Backspace(bool word)
        {
            if (_comment.Length == 0) return;
            if (!word) { _comment.Length--; return; }
            int i = _comment.Length;
            while (i > 0 && _comment[i - 1] == ' ') i--;
            while (i > 0 && _comment[i - 1] != ' ') i--;
            _comment.Length = i;
        }

        // ------------------------------------------------------------------ drawing

        void Draw()
        {
            _text.Clear();
            _pieceIndex = 0;
            if (_open) DrawWindow();
            else if (_notice != null)
            {
                float alpha = Mathf.Clamp01(_noticeUntil - Time.unscaledTime);
                float width = _text.font != null ? _text.font.GetLength(Vector2.one * FontSize, _notice) : 400;
                Piece(null, new Rect(400 - width / 2 - 12, 548, width + 24, 26), new Color(0, 0, 0, 0.7f * alpha));
                _text.DrawFont(_notice, new Vector2(400, 553), Vector2.one * FontSize, new Color(1, 1, 1, alpha), HplFontAlign.Center);
            }
            for (int i = _pieceIndex; i < _pieces.Count; i++) _pieces[i].enabled = false;
            _cursor.enabled = _open && HasCursor;
            if (_cursor.enabled && Mouse.current != null)
            {
                var p = VirtualPoint(Mouse.current.position.ReadValue());
                Place(_cursor.rectTransform, new Rect(p.x, p.y, _cursor.texture.width, _cursor.texture.height));
            }
        }

        void DrawWindow()
        {
            var size = Vector2.one * FontSize;
            Piece(null, new Rect(-2000, -2000, 4800, 4600), new Color(0, 0, 0, 0.45f));
            var w = WindowRect;
            Window(w);
            _text.DrawFont(T("Отчёт об ошибке", "Bug report"), w.position + new Vector2(35, -20), Vector2.one * 25, Color.white);
            _text.DrawFont(T("Опишите, что не так (обязательно):", "Describe what is wrong (required):"), new Vector2(w.x + 10, w.y + 30), size, Color.white);
            var box = new Rect(w.x + 10, w.y + 52, w.width - 20, Rows * (FontSize + 2) + 8);
            Piece("TextBoxBackground", box, Color.white);
            string comment = Comment;
            var rows = _text.font != null && comment.Length > 0 ? _text.font.GetWordWrapRows(box.width - 10, size, comment) : new List<string> { comment };
            if (rows.Count == 0) rows.Add("");
            int first = Mathf.Max(0, rows.Count - Rows);
            for (int i = first; i < rows.Count; i++)
                _text.DrawFont(rows[i], new Vector2(box.x + 5, box.y + 4 + (i - first) * (FontSize + 2)), size, Color.white);
            // TextBoxMarker: animated, 0.5 s per frame, after the last row.
            if (Mathf.FloorToInt(Time.unscaledTime / 0.5f) % 2 == 0)
            {
                string last = rows[rows.Count - 1];
                float x = box.x + 5 + (_text.font != null ? _text.font.GetLength(size, last) : last.Length * 7);
                Piece("TextBoxMarker", new Rect(x, box.y + 4 + (rows.Count - 1 - first) * (FontSize + 2), 2, FontSize), Color.white);
            }
            float y = box.yMax + 6;
            _text.DrawFont(comment.Length + " / " + MaxLength, new Vector2(box.xMax, y), Vector2.one * 12, Grey, HplFontAlign.Right);
            string hint = _error ?? T("Сохранятся: координаты, камера, снимок экрана, сохранение и лог.",
                                       "Saved: position, camera, screenshot, save game and log.");
            _text.DrawFont(hint, new Vector2(box.x, y), Vector2.one * 12, _error != null ? new Color(1f, 0.45f, 0.35f, 1f) : Grey);
            _cancel = new Rect(w.xMax - 10 - 100, w.yMax - 10 - 30, 100, 30);
            _ok = new Rect(_cancel.x - 10 - 100, _cancel.y, 100, 30);
            SkinButton(_ok, T("Ок", "OK"), !CanConfirm ? Grey : _hover == 0 ? Highlight : Color.white);
            SkinButton(_cancel, T("Отмена", "Cancel"), _hover == 1 ? Highlight : Color.white);
            _text.DrawFont(T("Enter - Ок, Esc - Отмена", "Enter - OK, Esc - Cancel"), new Vector2(w.x + 10, w.yMax - 10 - 30 + 8), Vector2.one * 12, Grey);
        }

        /// <summary>cWidgetWindow: WindowBackground and its borders / corners outside the rect (HplPauseMenu.Window).</summary>
        void Window(Rect r)
        {
            Piece("WindowBackground", r, Color.white);
            foreach (var type in new[] { "WindowBorderLeft", "WindowBorderRight", "WindowBorderUp", "WindowBorderDown", "WindowCornerLU", "WindowCornerRU", "WindowCornerLD", "WindowCornerRD" })
            {
                var size = Size(type);
                float w = size.x, h = size.y;
                Rect p = type switch
                {
                    "WindowBorderLeft" => new Rect(r.x - w, r.y, w, r.height), "WindowBorderRight" => new Rect(r.xMax, r.y, w, r.height),
                    "WindowBorderUp" => new Rect(r.x, r.y - h, r.width, h), "WindowBorderDown" => new Rect(r.x, r.yMax, r.width, h),
                    "WindowCornerLU" => new Rect(r.x - w, r.y - h, w, h), "WindowCornerRU" => new Rect(r.xMax, r.y - h, w, h),
                    "WindowCornerLD" => new Rect(r.x - w, r.yMax, w, h), _ => new Rect(r.xMax, r.yMax, w, h),
                };
                Piece(type, p, Color.white);
            }
        }

        /// <summary>cWidgetButton: ButtonUp background, borders and corners inside the rect (HplPauseMenu.SkinButton).</summary>
        void SkinButton(Rect r, string label, Color color)
        {
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
            _text.DrawFont(label, new Vector2(r.center.x, r.y + r.height / 2 - FontSize / 2f), Vector2.one * FontSize, color, HplFontAlign.Center);
        }

        Vector2 Size(string type)
        {
            var t = _assets != null ? _assets.Find(type).texture : null;
            return t != null ? new Vector2(t.width, t.height) : Vector2.one * 4;
        }

        void Piece(string type, Rect rect, Color color)
        {
            if (_pieceIndex == _pieces.Count) _pieces.Add(Image(_piecesParent, "Skin piece"));
            var target = _pieces[_pieceIndex++];
            var piece = type != null && _assets != null ? _assets.Find(type) : default;
            target.texture = piece.texture != null ? piece.texture : Texture2D.whiteTexture;
            // Without the menu resources the window is drawn in flat colours.
            target.color = piece.texture != null ? piece.color * color : type == null ? color : type == "WindowBackground" || type == "TextBoxBackground"
                ? new Color(0.08f, 0.07f, 0.06f, 0.95f) : new Color(0.4f, 0.35f, 0.28f, 1f);
            target.enabled = true;
            Place(target.rectTransform, rect);
        }

        Vector2 VirtualPoint(Vector2 position)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, position, null, out var p);
            return new Vector2(p.x + 400, 300 - p.y);
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
            if (_typingHooked && Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
            _typingHooked = false;
            if (_root == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_root); else UnityEngine.Object.DestroyImmediate(_root);
        }
    }
}
