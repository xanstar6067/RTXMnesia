using System;
using System.IO;
using System.Xml;
using RTXMnesia.Props;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Developer commentary data: source/commentary/*.comment → ignored Resources/HplCommentary.asset (the .ogg
    /// files go with the sounds, SoundConverter). Called by the level import; no map re-import is needed.
    /// </summary>
    public static class CommentaryConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplCommentary.asset";

        [MenuItem("Tools/RTXMnesia/Import/Developer commentary")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(log);
            log.Save("Developer commentary");
            if (log.Errors > 0) throw new InvalidOperationException("Commentary import failed; see AmnesiaImport.log");
        }

        public static HplCommentaryAssets Convert(ImportLog log)
        {
            var assets = AssetDatabase.LoadAssetAtPath<HplCommentaryAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplCommentaryAssets>();
            assets.entries.Clear();
            string folder = Path.Combine(AmnesiaPaths.SourceRoot, "commentary");
            if (Directory.Exists(folder))
                foreach (string file in Directory.EnumerateFiles(folder, "*.comment", SearchOption.AllDirectories))
                {
                    try
                    {
                        var doc = new XmlDocument();
                        doc.Load(file);
                        var root = doc.DocumentElement;
                        assets.entries.Add(new HplCommentaryAssets.Entry
                        {
                            file = Path.GetFileNameWithoutExtension(file),
                            talker = root.GetAttribute("Talker"),
                            topic = root.GetAttribute("Topic"),
                            soundFile = root.GetAttribute("SoundFile"),
                        });
                    }
                    catch (Exception e) { log.Error($"{AmnesiaPaths.ToSourceRelative(file)}: {e.Message}"); }
                }
            else log.Warn("No commentary folder in source: developer commentary icons will have no data");

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Commentary: {assets.entries.Count} .comment files");
            return assets;
        }
    }
}
