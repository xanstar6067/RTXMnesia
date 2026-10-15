using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Text of one game font on HPL2's 800×600 HUD set: cGuiSet::DrawFont calls collected for the frame (Clear, then
    /// DrawFont like the original OnDraw code), drawn as glyph quads (cGuiSet::DrawTextFromCharArry: the position is the
    /// top left of the row, the alignment moves it by the length). The RectTransform covers the virtual screen, so
    /// positions are virtual pixels from its top left, y down.
    /// </summary>
    public class HplGuiText : MaskableGraphic
    {
        public HplFont font;

        struct Command
        {
            public string text;
            public Vector2 position, size;
            public Color color;
            public HplFontAlign align;
        }

        readonly List<Command> _commands = new();

        /// <summary>DrawFont calls of this frame (for tools).</summary>
        public int CommandCount => _commands.Count;

        public override Texture mainTexture => font != null && font.atlas != null ? font.atlas : s_WhiteTexture;

        public void Clear()
        {
            if (_commands.Count == 0) return;
            _commands.Clear();
            SetVerticesDirty();
        }

        public void DrawFont(string text, Vector2 position, Vector2 size, Color color, HplFontAlign align = HplFontAlign.Left)
        {
            if (string.IsNullOrEmpty(text) || color.a <= 0f) return;
            _commands.Add(new Command { text = text, position = position, size = size, color = color, align = align });
            SetVerticesDirty();
        }

        /// <summary>
        /// The glyph quads of this frame's text: virtual rect (top left, size; y down), atlas UV rect and color
        /// (cGuiSet::DrawTextFromCharArry).
        /// </summary>
        public void ForEachQuad(System.Action<Rect, Rect, Color> quad)
        {
            if (font == null) return;
            foreach (var cmd in _commands)
            {
                Vector2 pos = cmd.position;
                if (cmd.align == HplFontAlign.Center) pos.x -= font.GetLength(cmd.size, cmd.text) / 2f;
                else if (cmd.align == HplFontAlign.Right) pos.x -= font.GetLength(cmd.size, cmd.text);
                foreach (char c in cmd.text)
                {
                    if (!font.TryGetGlyph(c, out var g)) continue;
                    Vector2 glyphSize = Vector2.Scale(g.size, cmd.size);
                    if (glyphSize.x > 0f && glyphSize.y > 0f)
                        quad(new Rect(pos + Vector2.Scale(g.offset, cmd.size), glyphSize), g.uv, cmd.color);
                    pos.x += g.advance * cmd.size.x;
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect area = rectTransform.rect;
            ForEachQuad((rect, uv, color) =>
            {
                // Virtual (y down, from the top left) → local (y up).
                float x0 = area.xMin + rect.x, x1 = x0 + rect.width;
                float y0 = area.yMax - rect.y, y1 = y0 - rect.height;
                Color32 c = color;
                int start = vh.currentVertCount;
                vh.AddVert(new Vector3(x0, y0), c, new Vector4(uv.xMin, uv.yMax));
                vh.AddVert(new Vector3(x1, y0), c, new Vector4(uv.xMax, uv.yMax));
                vh.AddVert(new Vector3(x1, y1), c, new Vector4(uv.xMax, uv.yMin));
                vh.AddVert(new Vector3(x0, y1), c, new Vector4(uv.xMin, uv.yMin));
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            });
        }
    }
}
