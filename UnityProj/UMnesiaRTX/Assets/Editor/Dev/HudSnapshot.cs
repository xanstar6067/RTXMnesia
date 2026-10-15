using System.Collections.Generic;
using System.IO;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Draws the HUD text onto a snapshot. Camera renders outside the player loop do not draw canvases, so the glyph
    /// quads of <see cref="HplGuiText.ForEachQuad"/> (the same layout as the UI mesh) are blended in on the CPU:
    /// the 800×600 virtual screen centered and scaled to the image height, the atlas sampled bilinearly, alpha
    /// blended in gamma like HPL2's HUD.
    /// </summary>
    static class HudSnapshot
    {
        public static void Draw(Texture2D image, HplHud hud)
        {
            if (hud == null) return;
            float scale = image.height / 600f;
            float offsetX = (image.width - 800f * scale) * 0.5f;
            var atlases = new Dictionary<Texture2D, Texture2D>();
            var pixels = image.GetPixels32();
            int width = image.width, height = image.height;
            foreach (var layer in hud.TextLayers)
            {
                if (layer == null || layer.font == null || layer.font.atlas == null) continue;
                if (!atlases.TryGetValue(layer.font.atlas, out var atlas)) atlases[layer.font.atlas] = atlas = Readable(layer.font.atlas);
                if (atlas == null) continue;
                layer.ForEachQuad((rect, uv, color) =>
                {
                    float left = offsetX + rect.xMin * scale, top = rect.yMin * scale;
                    float w = rect.width * scale, h = rect.height * scale;
                    int x0 = Mathf.Max(0, Mathf.FloorToInt(left)), x1 = Mathf.Min(width, Mathf.CeilToInt(left + w));
                    int y0 = Mathf.Max(0, Mathf.FloorToInt(top)), y1 = Mathf.Min(height, Mathf.CeilToInt(top + h));
                    for (int y = y0; y < y1; y++)
                    {
                        float v = Mathf.Lerp(uv.yMax, uv.yMin, (y + 0.5f - top) / h);
                        for (int x = x0; x < x1; x++)
                        {
                            float u = Mathf.Lerp(uv.xMin, uv.xMax, (x + 0.5f - left) / w);
                            Color t = atlas.GetPixelBilinear(u, v) * color;
                            if (t.a <= 0f) continue;
                            int i = (height - 1 - y) * width + x;
                            Color dst = pixels[i];
                            pixels[i] = Color.Lerp(dst, new Color(t.r, t.g, t.b, 1f), t.a);
                        }
                    }
                });
            }
            image.SetPixels32(pixels);
            image.Apply();
            foreach (var a in atlases.Values) if (a != null) Object.DestroyImmediate(a);
        }

        /// <summary>A readable copy of an imported atlas (its PNG as is).</summary>
        static Texture2D Readable(Texture2D atlas)
        {
            string path = AssetDatabase.GetAssetPath(atlas);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            copy.LoadImage(File.ReadAllBytes(path));
            return copy;
        }
    }
}
