using System.IO;
using System.Xml;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    public static class MenuConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplMenus.asset";

        [MenuItem("Tools/RTXMnesia/Import/Menu resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot),
                LanguageConverter.Convert(LanguageConverter.DefaultLanguage, log),
                ConfigConverter.Convert(ConfigConverter.MenuConfig, log), log);
            log.Save("Menu resources");
            if (log.Errors > 0) throw new System.InvalidOperationException("Menu import failed; see AmnesiaImport.log");
        }

        public static HplMenuAssets Convert(ResourceIndex index, HplLanguage language, HplConfig config, ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplMenuAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplMenuAssets>();
            assets.menuFont = FontConverter.Convert(config.GetString("Main", "TopMenuFont", "menu.fnt"), language, index, log);
            string skinPath = Path.Combine(AmnesiaPaths.SourceRoot, "gui/main_menu/gui_main_menu.skin");
            var skin = new XmlDocument();
            skin.Load(skinPath);
            string guiFont = ((XmlElement)skin.SelectSingleNode("/GuiSkin/Fonts/Font[@type='Default']"))?.GetAttribute("file");
            assets.guiFont = FontConverter.Convert(guiFont ?? "menu_gui_default.fnt", language, index, log);
            string logoFile = null;
            foreach (string folder in language.resourceDirs)
            {
                string candidate = Path.Combine(AmnesiaPaths.SourceRoot, folder, "graphics/main_menu/menu_logo.tga");
                if (File.Exists(candidate)) { logoFile = candidate; break; }
            }
            logoFile ??= index.Find("graphics/main_menu/menu_logo.tga");
            if (logoFile != null)
            {
                string logoPath = TextureConverter.Convert(logoFile, TextureRole.Gui, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(logoPath, ImportAssetOptions.ForceSynchronousImport);
                assets.titleLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(logoPath);
            }
            else log.Error("Main menu logo missing");
            assets.pieces.Clear();
            foreach (XmlElement piece in skin.SelectNodes("/GuiSkin/GfxElements/Gfx"))
            {
                string file = piece.GetAttribute("file");
                if (string.IsNullOrEmpty(file)) continue;
                string source = index.Find(file, skinPath);
                if (source == null) { log.Warn("Skin references a missing image (unused by this menu): " + file); continue; }
                string path = TextureConverter.Convert(source, TextureRole.Gui, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                assets.pieces.Add(new HplMenuAssets.Piece {
                    type = piece.GetAttribute("type"), texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path),
                    color = piece.Color("color", Color.white),
                });
            }
            foreach (string required in new[] { "PointerNormal", "WindowBackground", "WindowBorderLeft", "WindowBorderRight", "WindowBorderUp", "WindowBorderDown", "CheckBoxEnabledChecked", "CheckBoxEnabledUnchecked" })
                if (assets.Find(required).texture == null) log.Error("Required menu image missing: " + required);
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Menu: {assets.pieces.Count} skin pieces, fonts {assets.menuFont?.file}, {assets.guiFont?.file}");
            return assets;
        }
    }
}
