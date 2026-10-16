using System;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    public static class JournalConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplJournal.asset";
        [MenuItem("Tools/RTXMnesia/Import/Journal resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), LanguageConverter.Convert(LanguageConverter.DefaultLanguage, log), log);
            log.Save("Journal resources");
            if (log.Errors > 0) throw new InvalidOperationException("Journal import failed; see AmnesiaImport.log");
        }

        public static HplJournalAssets Convert(ResourceIndex index, HplLanguage language, ImportLog log)
        {
            string assetPath = HplCampaign.ForLanguage(language).AssetPath(AssetPath);
            var assets = AssetDatabase.LoadAssetAtPath<HplJournalAssets>(assetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplJournalAssets>();
            assets.textFont = FontConverter.Convert("game_journal.fnt", language, index, log);
            assets.menuFont = FontConverter.Convert("game_header.fnt", language, index, log);
            assets.images.Clear();
            void Image(string file)
            {
                string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) log.Error("Missing journal texture: " + file);
                assets.images.Add(new HplJournalAssets.Image { file = AmnesiaPaths.ToSourceRelative(file), texture = texture });
            }
            foreach (string file in Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "graphics/journal"), "*.tga")) Image(file);
            string pointer = index.Find("gui_amn_mm_pointer_normal.tga");
            if (pointer == null) pointer = index.Find("gui/default/gui_def_pointer_normal.tga");
            if (pointer != null) Image(pointer);
            assets.ResetLookup();
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(assetPath)));
                AssetDatabase.CreateAsset(assets, assetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Journal: {assets.images.Count} images, fonts {assets.textFont?.file}, {assets.menuFont?.file}");
            return assets;
        }
    }
}
