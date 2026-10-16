using System;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>Load screen resources (cLuxLoadScreenHandler): the menu.cfg LoadScreen font and graphics/general/game_loading_*.jpg.</summary>
    public static class LoadScreenConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplLoadScreen.asset";

        [MenuItem("Tools/RTXMnesia/Import/Load screen resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            var index = new ResourceIndex(AmnesiaPaths.SourceRoot);
            var menu = ConfigConverter.Convert(ConfigConverter.MenuConfig, log);
            Convert(index, LanguageConverter.Convert(LanguageConverter.DefaultLanguage, log), menu, log);
            log.Save("Load screen resources");
            if (log.Errors > 0) throw new InvalidOperationException("Load screen import failed; see AmnesiaImport.log");
        }

        public static HplLoadScreenAssets Convert(ResourceIndex index, HplLanguage language, HplConfig menuConfig, ImportLog log)
        {
            string assetPath = HplCampaign.ForLanguage(language).AssetPath(AssetPath);
            var assets = AssetDatabase.LoadAssetAtPath<HplLoadScreenAssets>(assetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplLoadScreenAssets>();
            string font = menuConfig != null ? menuConfig.GetString("LoadScreen", "DefaultFont") : "";
            assets.defaultFont = FontConverter.Convert(string.IsNullOrEmpty(font) ? "game_default.fnt" : font, language, index, log);
            assets.images.Clear();
            foreach (string file in Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "graphics/general"), "game_loading_*.jpg"))
            {
                string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) log.Error("Missing load screen texture: " + file);
                assets.images.Add(new HplLoadScreenAssets.Image { file = AmnesiaPaths.ToSourceRelative(file), texture = texture });
            }
            if (HplCampaign.ForLanguage(language).IsJustine)
                foreach (string file in Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "graphics/ptest"), "ptest_gameloading_*.jpg"))
                {
                    string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
                    if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    assets.images.Add(new HplLoadScreenAssets.Image { file = AmnesiaPaths.ToSourceRelative(file), texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) });
                }
            assets.ResetLookup();
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(assetPath)));
                AssetDatabase.CreateAsset(assets, assetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Load screen: {assets.images.Count} images, font {assets.defaultFont?.file}");
            return assets;
        }
    }
}
