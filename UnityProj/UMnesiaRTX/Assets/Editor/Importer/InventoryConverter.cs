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
    public static class InventoryConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplInventory.asset";

        [MenuItem("Tools/RTXMnesia/Import/Inventory resources")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), LanguageConverter.Convert(LanguageConverter.DefaultLanguage, log), log);
            log.Save("Inventory resources");
            if (log.Errors > 0) throw new InvalidOperationException("Inventory import failed; see AmnesiaImport.log");
        }

        public static HplInventoryAssets Convert(ResourceIndex index, HplLanguage language, ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplInventoryAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplInventoryAssets>();
            assets.defaultFont = FontConverter.Convert("game_default.fnt", language, index, log);
            assets.headerFont = FontConverter.Convert("game_header.fnt", language, index, log);
            assets.images.Clear();
            assets.items.Clear();
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Image(string file)
            {
                if (file == null) return;
                file = Path.GetFullPath(file);
                if (!added.Add(file)) return;
                string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
                if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                assets.images.Add(new HplInventoryAssets.Image { file = AmnesiaPaths.ToSourceRelative(file), texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) });
            }
            foreach (string directory in new[] { "graphics/inventory", "graphics/item" })
                foreach (string file in Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, directory), "*", SearchOption.AllDirectories))
                    if (Path.GetExtension(file).ToLowerInvariant() is ".tga" or ".dds" or ".png" or ".jpg") Image(file);
            Image(index.Find("gui/default/gui_def_pointer_normal.tga"));
            foreach (string file in Directory.EnumerateFiles(Path.Combine(AmnesiaPaths.SourceRoot, "entities"), "*.ent", SearchOption.AllDirectories))
            {
                var doc = new XmlDocument();
                doc.Load(file);
                var vars = doc.DocumentElement?["UserDefinedVariables"];
                if (vars == null || vars.GetAttribute("EntityType") != "Item") continue;
                string Var(string key) => ((XmlElement)vars.SelectSingleNode("Var[@Name='" + key + "']"))?.GetAttribute("Value") ?? "";
                string type = vars.GetAttribute("EntitySubType");
                string amountKey = type switch { "Health" or "Sanity" => "HealAmount", "LampOil" => "OilAmount", "Coins" => "CoinAmount", _ => "" };
                float amount = HplConfig.TryParseFloat(Var(amountKey), out var value) ? value : amountKey == "" ? 1f : 0f;
                string image = Var("ImageFile");
                assets.items.Add(new HplInventoryAssets.Definition { file = AmnesiaPaths.ToSourceRelative(file), type = type, subType = Var("SubItemTypeName"), image = image, amount = amount });
                if (!string.IsNullOrEmpty(image)) Image(index.Find("graphics/item/" + image, file));
            }
            assets.ResetLookup();
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Inventory: {assets.items.Count} item definitions, {assets.images.Count} images");
            return assets;
        }
    }
}
