using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The game's bitmap fonts (.fnt, AngelCode XML) → <see cref="HplFont"/> in Assets/AmnesiaImported/Fonts/&lt;name&gt;.asset,
    /// like cSDLFontData::CreateFromBitmapFile: metrics divided by lineHeight (the x size by base / lineHeight × lineHeight /
    /// base, i.e. also lineHeight). The pages are stacked top to bottom into one atlas
    /// (Textures/&lt;font path&gt;__gui.png, so it gets the GUI import settings). A font is looked up in the language's
    /// RESOURCES folders first (fonts/rus), then anywhere (FileSearcher).
    /// </summary>
    public static class FontConverter
    {
        public const string FontsRoot = AmnesiaPaths.ImportedAssetsRoot + "/Fonts";

        public static HplFont Convert(string fontFile, HplLanguage language, ResourceIndex index, ImportLog log)
        {
            string abs = Find(fontFile, language, index);
            if (abs == null)
            {
                log.Error($"Font '{fontFile}' not found");
                return null;
            }
            string rel = AmnesiaPaths.ToSourceRelative(abs);
            try
            {
                var doc = new XmlDocument();
                doc.Load(abs);
                var root = doc.DocumentElement ?? throw new InvalidDataException("empty file");
                var common = root["common"] ?? throw new InvalidDataException("no common element");
                int lineHeight = HplMath.Atoi(common.GetAttribute("lineHeight"));
                int baseLine = HplMath.Atoi(common.GetAttribute("base"));
                if (lineHeight <= 0 || baseLine <= 0) throw new InvalidDataException("bad lineHeight / base");

                // Pages stacked into one atlas (all pages of a font have the size scaleW × scaleH).
                var pages = new List<DdsDecoder.Image>();
                foreach (XmlNode n in root["pages"].ChildNodes)
                {
                    if (n is not XmlElement page || page.Name != "page") continue;
                    string pagePath = Path.Combine(Path.GetDirectoryName(abs), page.GetAttribute("file"));
                    pages.Add(ImageLoader.Load(pagePath));
                }
                if (pages.Count == 0) throw new InvalidDataException("no pages");
                int width = pages[0].Width, pageHeight = pages[0].Height, height = pageHeight * pages.Count;
                var pixels = new Color32[width * height];
                for (int p = 0; p < pages.Count; p++)
                {
                    var page = pages[p];
                    if (page.Width != width || page.Height != pageHeight) throw new InvalidDataException("pages of different sizes");
                    Array.Copy(page.Faces[0], 0, pixels, p * width * pageHeight, width * pageHeight);
                }
                string atlasPath = TextureConverter.OutputAssetPath(abs, TextureRole.Gui);
                Directory.CreateDirectory(Path.GetDirectoryName(AmnesiaPaths.ToAbsolute(atlasPath)));
                File.WriteAllBytes(AmnesiaPaths.ToAbsolute(atlasPath), TextureConverter.EncodePng(pixels, width, height));
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);

                var font = ScriptableObject.CreateInstance<HplFont>();
                font.file = rel;
                font.atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                font.height = lineHeight;
                float h = lineHeight;
                int largest = -1;
                foreach (XmlNode n in root["chars"].ChildNodes)
                {
                    if (n is not XmlElement c || c.Name != "char") continue;
                    int id = HplMath.Atoi(c.GetAttribute("id"));
                    if (id < 0) continue;
                    largest = Mathf.Max(largest, id);
                    int x = HplMath.Atoi(c.GetAttribute("x")), y = HplMath.Atoi(c.GetAttribute("y"));
                    int w = HplMath.Atoi(c.GetAttribute("width")), gh = HplMath.Atoi(c.GetAttribute("height"));
                    int page = HplMath.Atoi(c.GetAttribute("page"));
                    float top = page * pageHeight + y;
                    font.glyphs.Add(new HplFont.Glyph
                    {
                        id = id,
                        uv = Rect.MinMaxRect((float)x / width, 1f - (top + gh) / height, (float)(x + w) / width, 1f - top / height),
                        offset = new Vector2(HplMath.Atoi(c.GetAttribute("xoffset")) / h, HplMath.Atoi(c.GetAttribute("yoffset")) / h),
                        size = new Vector2(w / h, gh / h),
                        advance = HplMath.Atoi(c.GetAttribute("xadvance")) / h,
                    });
                }
                font.lastChar = largest;

                string assetPath = FontsRoot + "/" + Path.GetFileNameWithoutExtension(abs) + ".asset";
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(FontsRoot));
                var existing = AssetDatabase.LoadAssetAtPath<HplFont>(assetPath);
                if (existing != null)
                {
                    EditorUtility.CopySerialized(font, existing);
                    UnityEngine.Object.DestroyImmediate(font);
                    font = existing;
                    EditorUtility.SetDirty(font);
                }
                else AssetDatabase.CreateAsset(font, assetPath);
                log.Info($"Font {rel} → {assetPath} ({font.glyphs.Count} glyphs, {pages.Count} pages)");
                return font;
            }
            catch (Exception e)
            {
                log.Error($"{rel}: {e.Message}");
                return null;
            }
        }

        static string Find(string fontFile, HplLanguage language, ResourceIndex index)
        {
            if (string.IsNullOrEmpty(fontFile)) return null;
            if (language != null)
                foreach (string dir in language.resourceDirs)
                {
                    string candidate = Path.Combine(AmnesiaPaths.SourceRoot, dir, Path.GetFileName(fontFile));
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }
            return index.Find(fontFile);
        }
    }
}
