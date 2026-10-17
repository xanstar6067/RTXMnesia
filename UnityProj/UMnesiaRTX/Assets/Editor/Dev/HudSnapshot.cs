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
        /// <summary>Canvas snapshot from the actual generated quad meshes (including skin images), after rendering the world.</summary>
        public static void DrawCanvas(Texture2D image, Canvas canvas, Camera camera)
        {
            var textures = new Dictionary<Texture2D, Texture2D>();
            var pixels = image.GetPixels32();
            foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>())
            {
                if (!graphic.isActiveAndEnabled || graphic.canvasRenderer == null) continue;
                var mesh = graphic.canvasRenderer.GetMesh();
                if (mesh == null || mesh.vertexCount == 0) continue;
                var texture = graphic.mainTexture as Texture2D;
                if (texture == null) continue;
                if (!textures.TryGetValue(texture, out var source))
                    textures[texture] = source = texture == Texture2D.whiteTexture ? texture : Readable(texture);
                if (source == null) continue;
                // RectMask2D clipping (the inventory oil level), as the canvas renderer clips.
                Rect clip = new(0, 0, image.width, image.height);
                foreach (var mask in graphic.GetComponentsInParent<UnityEngine.UI.RectMask2D>())
                {
                    var corners = new Vector3[4];
                    mask.rectTransform.GetWorldCorners(corners);
                    Vector2 a = camera.WorldToScreenPoint(corners[0]), b = camera.WorldToScreenPoint(corners[2]);
                    clip = Rect.MinMaxRect(Mathf.Max(clip.xMin, Mathf.Min(a.x, b.x)), Mathf.Max(clip.yMin, Mathf.Min(a.y, b.y)),
                                           Mathf.Min(clip.xMax, Mathf.Max(a.x, b.x)), Mathf.Min(clip.yMax, Mathf.Max(a.y, b.y)));
                }
                var vertices = mesh.vertices;
                var uvs = mesh.uv;
                var colors = mesh.colors32;
                for (int start = 0; start + 3 < vertices.Length; start += 4)
                {
                    var points = new Vector2[4];
                    float left = float.MaxValue, right = float.MinValue, bottom = float.MaxValue, top = float.MinValue;
                    for (int j = 0; j < 4; j++)
                    {
                        points[j] = camera.WorldToScreenPoint(graphic.transform.TransformPoint(vertices[start + j]));
                        left = Mathf.Min(left, points[j].x); right = Mathf.Max(right, points[j].x);
                        bottom = Mathf.Min(bottom, points[j].y); top = Mathf.Max(top, points[j].y);
                    }
                    if (right <= left || top <= bottom) continue;
                    Vector2 UvAt(float x, float y)
                    {
                        int nearest = 0;
                        for (int j = 1; j < 4; j++)
                            if ((points[j] - new Vector2(x, y)).sqrMagnitude < (points[nearest] - new Vector2(x, y)).sqrMagnitude) nearest = j;
                        return uvs[start + nearest];
                    }
                    Vector2 bl = UvAt(left, bottom), br = UvAt(right, bottom), tl = UvAt(left, top), tr = UvAt(right, top);
                    Color tint = colors.Length > start ? colors[start] : Color.white;
                    for (int y = Mathf.Max(Mathf.CeilToInt(clip.yMin - 0.5f), Mathf.FloorToInt(bottom)); y < Mathf.Min(Mathf.CeilToInt(clip.yMax - 0.5f), Mathf.CeilToInt(top)); y++)
                    {
                        float v = (y + 0.5f - bottom) / (top - bottom);
                        for (int x = Mathf.Max(Mathf.CeilToInt(clip.xMin - 0.5f), Mathf.FloorToInt(left)); x < Mathf.Min(Mathf.CeilToInt(clip.xMax - 0.5f), Mathf.CeilToInt(right)); x++)
                        {
                            float u = (x + 0.5f - left) / (right - left);
                            Vector2 uv = Vector2.Lerp(Vector2.Lerp(bl, br, u), Vector2.Lerp(tl, tr, u), v);
                            Color color = source.GetPixelBilinear(uv.x, uv.y) * tint;
                            int i = y * image.width + x;
                            pixels[i] = Color.Lerp(pixels[i], new Color(color.r, color.g, color.b, 1), color.a);
                        }
                    }
                }
            }
            image.SetPixels32(pixels);
            image.Apply();
            foreach (var texture in textures.Values)
                if (texture != null && texture != Texture2D.whiteTexture) Object.DestroyImmediate(texture);
        }

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
