using System;
using System.IO;
using System.Xml;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Pre menu resources (cLuxPreMenu::LoadPreMenuSections): the file named by main_init.cfg ConfigFiles PreMenu
    /// (config/pre_menu.cfg) → ignored Resources/HplPreMenu.asset — section images (Gui role), texts, music, and
    /// graphics/main_menu/menu_gamma.tga for the gamma section. No map re-import is needed.
    /// </summary>
    public static class PreMenuConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplPreMenu.asset";

        [MenuItem("Tools/RTXMnesia/Import/Pre menu resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), log);
            log.Save("Pre menu resources");
            if (log.Errors > 0) throw new InvalidOperationException("Pre menu import failed; see AmnesiaImport.log");
        }

        static string PreMenuConfigPath(ImportLog log)
        {
            string init = Path.Combine(AmnesiaPaths.SourceRoot, "config/main_init.cfg");
            string rel = "config/pre_menu.cfg";
            if (File.Exists(init))
            {
                var doc = new XmlDocument();
                doc.Load(init);
                if (doc.SelectSingleNode("//ConfigFiles") is XmlElement files && files.HasAttribute("PreMenu")) rel = files.GetAttribute("PreMenu");
            }
            else log.Warn("config/main_init.cfg missing, using config/pre_menu.cfg");
            return Path.Combine(AmnesiaPaths.SourceRoot, rel);
        }

        static HplFontAlign Align(string s)
        {
            switch (s.ToLowerInvariant())
            {
                case "center": return HplFontAlign.Center;
                case "right": return HplFontAlign.Right;
                case "left": return HplFontAlign.Left;
                default: return HplFontAlign.Left; // HPL2: Error() and left
            }
        }

        public static HplPreMenuAssets Convert(ResourceIndex index, ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplPreMenuAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplPreMenuAssets>();

            Texture2D Image(string name, string referencing)
            {
                if (string.IsNullOrEmpty(name)) return null;
                string file = index.Find(name, referencing);
                if (file == null)
                {
                    log.Error("Pre menu image not found: " + name);
                    return null;
                }
                string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) log.Error("Missing pre menu texture: " + path);
                return texture;
            }

            string configPath = PreMenuConfigPath(log);
            assets.sections.Clear();
            var doc = new XmlDocument();
            doc.Load(configPath);
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node is not XmlElement e) continue;
                var section = new HplPreMenuAssets.Section
                {
                    color = e.Color("Color", new Color(0f, 0f, 0f, 1f)),
                    imageFile = e.Str("Image", ""),
                    time = e.Float("Time", 2f),
                    showFirstStartOnly = e.Bool("ShowFirstStartOnly", false),
                    hasGammaSettings = e.Bool("HasGammaSettings", false),
                    music = e.Str("Music", ""),
                    musicVolume = e.Float("MusicVolume", 0f),
                    musicFadeTime = e.Float("MusicFadeTime", 0f),
                    allowSkipping = e.Bool("AllowSkipping", true),
                };
                section.image = Image(section.imageFile, configPath);
                foreach (XmlNode child in e.ChildNodes)
                {
                    if (child is not XmlElement t) continue;
                    section.texts.Add(new HplPreMenuAssets.Text
                    {
                        category = t.Str("TextCat", ""),
                        entry = t.Str("TextEntry", ""),
                        position = t.Vec2("Pos"),
                        frameSize = t.Vec2("FrameSize"),
                        fontSize = t.Vec2("FontSize", Vector2.zero),
                        color = t.Color("Color", Color.black),
                        align = Align(t.Str("Align", "Left")),
                    });
                }
                assets.sections.Add(section);
            }
            assets.gammaImage = Image("menu_gamma.tga", configPath);
            assets.guiShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/HplGui.shader");
            if (assets.guiShader == null) log.Error("Missing Assets/Shaders/HplGui.shader");

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Pre menu: {assets.sections.Count} sections from {AmnesiaPaths.ToSourceRelative(configPath)}");
            return assets;
        }
    }
}
