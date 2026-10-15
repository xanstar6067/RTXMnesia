using System.Collections.Generic;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.UI;

namespace RTXMnesia.Ui
{
    /// <summary>Original inventory graphics / menu.cfg layout on the shared 800×600 bitmap-font UI.</summary>
    public sealed class HplInventoryView
    {
        readonly HplMap _map;
        readonly HplInventory _inventory;
        readonly GameObject _root;
        readonly RectTransform _screen;
        readonly HplGuiText _text, _header, _message;
        readonly RawImage _cursor, _drag, _modalShade, _flash;
        readonly List<RawImage> _images = new();
        readonly Dictionary<int, Rect> _regions = new();
        int _imageCount;
        float _alpha;
        public bool HasCursor => _cursor.texture != Texture2D.whiteTexture;
        Vector2 SlotSize => V("SlotSize", new Vector2(74, 99));
        Vector2 Start => V("SlotsStart", new Vector2(172, 62));
        Vector2 Separation => V("SlotsSeparation", Vector2.one * 2);
        HplInventoryAssets Assets => _inventory.Assets;

        public HplInventoryView(HplMap map, HplInventory inventory)
        {
            _map = map;
            _inventory = inventory;
            _root = new GameObject("HPL inventory", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)) { hideFlags = HideFlags.DontSave };
            _root.transform.SetParent(map.transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            scaler.matchWidthOrHeight = 1;
            _screen = new GameObject("Virtual 800x600", typeof(RectTransform)).GetComponent<RectTransform>();
            _screen.SetParent(_root.transform, false);
            _screen.anchorMin = _screen.anchorMax = _screen.pivot = Vector2.one * 0.5f;
            _screen.sizeDelta = new Vector2(800, 600);
            _text = Text("Inventory text", Assets != null ? Assets.defaultFont : map.hud.defaultFont);
            _header = Text("Inventory headers", Assets != null ? Assets.headerFont : map.hud.defaultFont);
            _modalShade = Image(_root.transform, "Message shade");
            _modalShade.rectTransform.anchorMin = Vector2.zero;
            _modalShade.rectTransform.anchorMax = Vector2.one;
            _modalShade.rectTransform.offsetMin = _modalShade.rectTransform.offsetMax = Vector2.zero;
            _message = Text("Inventory message", _text.font);
            // The message must render above the full-screen shade (a sibling of the virtual screen).
            _message.transform.SetParent(_root.transform, false);
            _message.rectTransform.anchorMin = _message.rectTransform.anchorMax = Vector2.one * 0.5f;
            _message.rectTransform.pivot = Vector2.one * 0.5f;
            _message.rectTransform.anchoredPosition = Vector2.zero;
            _message.rectTransform.sizeDelta = new Vector2(800, 600);
            _drag = Image(_screen, "Picked inventory item");
            _flash = Image(_root.transform, "Sanity gain flash");
            _flash.rectTransform.anchorMin = Vector2.zero; _flash.rectTransform.anchorMax = Vector2.one;
            _flash.rectTransform.offsetMin = _flash.rectTransform.offsetMax = Vector2.zero;
            _cursor = Image(_screen, "Inventory cursor");
            var pointer = Assets != null ? Assets.Texture("gui/default/gui_def_pointer_normal.tga") : null;
            _cursor.texture = pointer != null ? pointer : Texture2D.whiteTexture;
            _cursor.enabled = pointer != null;
            _root.SetActive(false);
        }
        public void SetVisible(bool visible) => _root.SetActive(visible);
        public Vector2 VirtualPoint(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_screen, screenPosition, null, out var p);
            return new Vector2(p.x + 400, 300 - p.y);
        }
        public Rect SlotRect(int index) => new(Start + new Vector2(index % _inventory.Columns * (SlotSize.x + Separation.x), index / _inventory.Columns * (SlotSize.y + Separation.y)), SlotSize);
        public int HitTest(Vector2 screenPosition)
        {
            Vector2 p = VirtualPoint(screenPosition);
            for (int i = 0; i < _inventory.Capacity; i++) if (SlotRect(i).Contains(p)) return i;
            foreach (var region in _regions) if (region.Value.Contains(p)) return region.Key;
            return -1;
        }

        public void Refresh(float alpha, Vector2 mousePosition)
        {
            _alpha = alpha;
            _text.Clear(); _header.Clear(); _message.Clear();
            _imageCount = 0;
            _regions.Clear();
            Vector2 gridSize = new(_inventory.Columns * (SlotSize.x + Separation.x) - Separation.x, _inventory.Rows * (SlotSize.y + Separation.y) - Separation.y);
            var grid = new Rect(Start, gridSize);
            Frame("generic", grid);
            var gridColor = _map.menuConfig != null ? _map.menuConfig.GetColor("Inventory", "SlotsGridColor", new Color(0.4f, 0.4f, 0.4f, 0.25f)) : new Color(0.4f, 0.4f, 0.4f, 0.25f);
            for (int i = 1; i < _inventory.Columns; i++) Draw((Texture)null, new Rect(Start.x + i * (SlotSize.x + Separation.x) - 1, Start.y - 12, 1, gridSize.y + 24), gridColor);
            for (int i = 1; i < _inventory.Rows; i++) Draw((Texture)null, new Rect(Start.x - 12, Start.y + i * (SlotSize.y + Separation.y) - 1, gridSize.x + 24, 1), gridColor);
            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var slot = SlotRect(i);
                var item = _inventory.ItemAt(i);
                if (_inventory.Selected == i)
                    Draw("inventory_slot_glow.tga", slot, new Color(1, 1, 1, 0.5f + 0.25f * Mathf.Sin(Time.unscaledTime * 4)));
                var texture = _inventory.Icon(item);
                if (texture == null) continue;
                var r = new Rect(slot.center - new Vector2(texture.width, texture.height) * 0.5f, new Vector2(texture.width, texture.height));
                Draw(texture, r, new Color(1, 1, 1, item == _inventory.Picked ? 0.5f : 1));
                if (HplGame.HasCount(item.type))
                    Label(_text, "x " + item.count, new Vector2(slot.xMax, slot.y) + V("ItemCountOffset", new Vector2(-18, 10)), V("FontSize_ItemCount", Vector2.one * 17), HplFontAlign.Center);
            }
            Stat("health", _map.player.health, "Health", new Vector2(75, 168), HplInventory.Health);
            Stat("sanity", _map.player.sanity, "Sanity", new Vector2(75, 418), HplInventory.Sanity);
            var tinder = Center("inventory_tinderboxes.tga", V("TinderboxesCenter", new Vector2(720, 127)));
            _regions[HplInventory.Tinderboxes] = tinder;
            Around("generic", "Tinderboxes", tinder);
            Label(_text, "x " + _map.player.tinderboxes, V("TinderboxesCenter", new Vector2(720, 127)) - Vector2.up * 10, V("FontSize_Label", Vector2.one * 20), HplFontAlign.Center);
            Oil();
            var journal = Center("inventory_journal.tga", V("JournalCenter", new Vector2(720, 472)), 0.5f);
            _regions[HplInventory.Journal] = journal;
            Around("generic", "Journal", journal);

            Vector2 header = V("ItemHeaderCenter", new Vector2(400, 420));
            Vector2 desc = V("ItemDescCenter", new Vector2(400, 440));
            float width = _map.menuConfig != null ? _map.menuConfig.GetFloat("Inventory", "ItemDescTextWidth", 456) : 456;
            Frame("generic", new Rect(header.x - width * 0.5f, header.y, width, width * 0.25f));
            var description = _inventory.Description();
            Label(_header, description.header, header, V("FontSize_Header", Vector2.one * 24), HplFontAlign.Center);
            var fontSize = V("FontSize_Description", Vector2.one * 18);
            var rows = _text.font != null ? _text.font.GetWordWrapRows(width, fontSize, description.text) : new List<string>();
            for (int i = 0; i < rows.Count; i++) Label(_text, rows[i], desc + Vector2.up * (i * (fontSize.y + 2)), fontSize, HplFontAlign.Center);
            for (int i = _imageCount; i < _images.Count; i++) _images[i].enabled = false;
            _modalShade.enabled = _inventory.MessageActive;
            _modalShade.color = new Color(0, 0, 0, alpha * 0.7f);
            if (_inventory.MessageActive)
            {
                fontSize = V("FontSize_Message", Vector2.one * 22);
                rows = _message.font != null ? _message.font.GetWordWrapRows(650, fontSize, _inventory.Message) : new List<string>();
                Vector2 pos = V("PopUpMessageCenter", new Vector2(400, 200));
                for (int i = 0; i < rows.Count; i++) Label(_message, rows[i], pos + Vector2.up * (i * (fontSize.y + 2)), fontSize, HplFontAlign.Center);
            }
            var picked = _inventory.Icon(_inventory.Picked);
            _drag.enabled = picked != null;
            if (picked != null)
            {
                _drag.texture = picked; _drag.color = new Color(1, 1, 1, alpha * 0.8f);
                Place(_drag.rectTransform, new Rect(VirtualPoint(mousePosition) + _inventory.PickOffset, new Vector2(picked.width, picked.height)));
            }
            _flash.color = _map.effects != null ? new Color(_map.effects.SanityGainFlash.Color.r, _map.effects.SanityGainFlash.Color.g, _map.effects.SanityGainFlash.Color.b, _map.effects.SanityGainFlash.Alpha) : Color.clear;
            if (HasCursor) Place(_cursor.rectTransform, new Rect(VirtualPoint(mousePosition), new Vector2(_cursor.texture.width, _cursor.texture.height)));
        }

        void Stat(string file, float value, string key, Vector2 fallback, int region)
        {
            string suffix = ((HplInventory.Status(value) + 1) * 25).ToString();
            var rect = Center("inventory_" + file + "_" + suffix + ".tga", V(key + "Center", fallback));
            _regions[region] = rect;
            float glow = 0.2f + 0.1f * Mathf.Sin(Time.unscaledTime * (1.5f - HplInventory.Status(value) * 0.15f));
            Center("inventory_" + file + "_glow_" + suffix + ".tga", rect.center, glow);
            Around(file, key, rect);
        }
        void Oil()
        {
            Vector2 center = V("OilCenter", new Vector2(720, 293));
            var rect = Center("inventory_oil_bg.tga", center);
            float fraction = Mathf.Clamp01(_map.player.lampOil / 100f);
            if (fraction > 0)
            {
                var fill = new Rect(rect.x, rect.yMax - rect.height * fraction, rect.width, rect.height * fraction);
                Draw("inventory_oil_liquid.tga", fill, Color.white).uvRect = new Rect(0, 0, 1, fraction);
                for (int i = 0; i < 3; i++)
                {
                    float y = Mathf.Repeat(Time.unscaledTime * rect.height * 0.5f * (0.5f + i * 0.25f) + i * 25f, Mathf.Max(1, fill.height - 6));
                    Draw("inventory_oil_bubble_" + (i + 1) + ".tga", new Rect(fill.x + 6 + i * 10, fill.yMax - y - 6, 6, 6), Color.white);
                }
            }
            Draw("inventory_oil_fg.tga", rect, Color.white);
            _regions[HplInventory.Oil] = rect;
            Around("generic", "Oil", rect);
        }
        void Around(string style, string key, Rect widget)
        {
            var size = V(key + "FrameSize", new Vector2(-1, -1));
            var offset = V(key + "FrameOffset", Vector2.zero);
            var hp = V(key + "FrameHPadding", Vector2.zero); var vp = V(key + "FrameVPadding", Vector2.zero);
            Frame(style, new Rect(widget.x + offset.x - hp.x, widget.y + offset.y - vp.x,
                (size.x < 0 ? widget.width : size.x) + hp.x + hp.y, (size.y < 0 ? widget.height : size.y) + vp.x + vp.y));
        }
        void Frame(string style, Rect r)
        {
            string prefix = "inventory_frame_" + style + "_";
            var corner = Assets != null ? Assets.Texture(prefix + "corner_lu.tga") : null;
            float w = corner != null ? corner.width : 21, h = corner != null ? corner.height : 21;
            Draw(prefix + "corner_lu.tga", new Rect(r.x - w, r.y - h, w, h), Color.white);
            Draw(prefix + "corner_ru.tga", new Rect(r.xMax, r.y - h, w, h), Color.white);
            Draw(prefix + "corner_ld.tga", new Rect(r.x - w, r.yMax, w, h), Color.white);
            Draw(prefix + "corner_rd.tga", new Rect(r.xMax, r.yMax, w, h), Color.white);
            Draw(prefix + "border_u.tga", new Rect(r.x, r.y - h, r.width, h), Color.white);
            Draw(prefix + "border_d.tga", new Rect(r.x, r.yMax, r.width, h), Color.white);
            Draw(prefix + "border_l.tga", new Rect(r.x - w, r.y, w, r.height), Color.white);
            Draw(prefix + "border_r.tga", new Rect(r.xMax, r.y, w, r.height), Color.white);
        }
        Rect Center(string file, Vector2 center, float alpha = 1)
        {
            var texture = Assets != null ? Assets.Texture(file) : null;
            Vector2 size = texture != null ? new Vector2(texture.width, texture.height) : Vector2.one * 64;
            var rect = new Rect(center - size * 0.5f, size);
            Draw(texture, rect, new Color(1, 1, 1, alpha));
            return rect;
        }
        RawImage Draw(string file, Rect rect, Color color) => Draw(file != null && Assets != null ? Assets.Texture(file) : null, rect, color);
        RawImage Draw(Texture texture, Rect rect, Color color)
        {
            if (_imageCount == _images.Count)
            {
                var image = Image(_screen, "Inventory graphic");
                image.transform.SetSiblingIndex(_text.transform.GetSiblingIndex());
                _images.Add(image);
            }
            var target = _images[_imageCount++];
            target.texture = texture != null ? texture : Texture2D.whiteTexture;
            target.uvRect = new Rect(0, 0, 1, 1);
            color.a *= _alpha; target.color = color; target.enabled = true;
            Place(target.rectTransform, rect);
            return target;
        }
        void Label(HplGuiText text, string value, Vector2 position, Vector2 size, HplFontAlign align) => text.DrawFont(value, position, size, new Color(1, 1, 1, _alpha), align);
        Vector2 V(string name, Vector2 fallback) => _map.menuConfig != null ? _map.menuConfig.GetVector2("Inventory", name, fallback) : fallback;
        HplGuiText Text(string name, HplFont font)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(_screen, false);
            Place(go.GetComponent<RectTransform>(), new Rect(0, 0, 800, 600));
            var text = go.AddComponent<HplGuiText>(); text.font = font; text.raycastTarget = false;
            return text;
        }
        static RawImage Image(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var image = go.AddComponent<RawImage>(); image.texture = Texture2D.whiteTexture; image.raycastTarget = false;
            return image;
        }
        static void Place(RectTransform rect, Rect r)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(r.x, -r.y); rect.sizeDelta = r.size;
        }
        public void Destroy() { if (_root == null) return; if (Application.isPlaying) Object.Destroy(_root); else Object.DestroyImmediate(_root); }
    }
}
