using System.IO;
using System.Xml;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// HPL2 .cfg (several root elements with attributes) → <see cref="HplConfig"/> asset in
    /// Assets/AmnesiaImported/Config/&lt;name&gt;.asset. Existing assets are updated in place (GUID kept).
    /// </summary>
    public static class ConfigConverter
    {
        public const string ConfigRoot = AmnesiaPaths.ImportedAssetsRoot + "/Config";
        public const string GameConfig = "config/game.cfg";

        public static string AssetPathFor(string sourceRelative) =>
            ConfigRoot + "/" + Path.GetFileNameWithoutExtension(sourceRelative) + ".asset";

        public static HplConfig Convert(string sourceRelative, ImportLog log)
        {
            string file = Path.Combine(AmnesiaPaths.SourceRoot, sourceRelative);
            if (!File.Exists(file))
            {
                log.Error($"Config not found: {file}");
                return null;
            }

            // .cfg files have several top-level elements: wrap them to get one XML document.
            var doc = new XmlDocument();
            doc.LoadXml("<Root>" + File.ReadAllText(file) + "</Root>");

            string assetPath = AssetPathFor(sourceRelative);
            var config = AssetDatabase.LoadAssetAtPath<HplConfig>(assetPath);
            bool created = config == null;
            if (created) config = ScriptableObject.CreateInstance<HplConfig>();

            config.sourceFile = sourceRelative;
            config.sections.Clear();
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node is not XmlElement element) continue;
                var section = new HplConfig.Section { name = element.Name };
                foreach (XmlAttribute a in element.Attributes) section.values.Add(new HplVariable(a.Name, a.Value));
                config.sections.Add(section);
            }

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(ConfigRoot));
                AssetDatabase.CreateAsset(config, assetPath);
            }
            else EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            log.Info($"Config {sourceRelative} → {assetPath} ({config.sections.Count} sections)");
            return config;
        }
    }
}
