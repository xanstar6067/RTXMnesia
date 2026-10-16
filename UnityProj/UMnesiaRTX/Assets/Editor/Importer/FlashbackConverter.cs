using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RTXMnesia.Player;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>cLuxPlayerFlashback::LoadAndPlayFlashbackFile: .flash XML to a runtime voice bank, updated in place.</summary>
    public static class FlashbackConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplFlashbacks.asset";

        [MenuItem("Tools/RTXMnesia/Import/Flashbacks (.flash)")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(log);
            log.Save("Flashbacks");
            if (log.Errors > 0) throw new InvalidOperationException("Flashback import failed; see AmnesiaImport.log");
        }

        public static HplFlashbackAssets Convert(ImportLog log)
        {
            string folder = Path.Combine(AmnesiaPaths.SourceRoot, "flashbacks");
            if (!Directory.Exists(folder))
            {
                log.Error("Flashback folder not found: " + folder);
                return null;
            }

            var files = new List<string>(Directory.EnumerateFiles(folder, "*.flash", SearchOption.AllDirectories));
            files.Sort(StringComparer.OrdinalIgnoreCase);
            var flashbacks = new List<HplFlashbackAssets.FlashbackData>();
            int voices = 0;
            foreach (string file in files)
            {
                try
                {
                    var doc = new XmlDocument();
                    doc.Load(file);
                    var elements = doc.DocumentElement?.SelectSingleNode("Voices");
                    if (elements == null) throw new InvalidDataException("Could not find voice element");
                    var data = new HplFlashbackAssets.FlashbackData { name = Path.GetFileNameWithoutExtension(file) };
                    foreach (XmlNode node in elements.ChildNodes)
                    {
                        if (node is not XmlElement voice) continue;
                        data.voices.Add(new HplFlashbackAssets.VoiceData
                        {
                            voiceSound = voice.GetAttribute("VoiceSound"), effectSound = voice.GetAttribute("EffectSound"),
                            textCat = voice.GetAttribute("TextCat"), textEntry = voice.GetAttribute("TextEntry"),
                        });
                    }
                    flashbacks.Add(data);
                    voices += data.voices.Count;
                }
                catch (Exception e)
                {
                    log.Error($"{AmnesiaPaths.ToSourceRelative(file)}: {e.Message}");
                }
            }

            var assets = AssetDatabase.LoadAssetAtPath<HplFlashbackAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplFlashbackAssets>();
            assets.flashbacks = flashbacks;
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Flashbacks: {flashbacks.Count} flashbacks, {voices} voices -> {AssetPath}");
            return assets;
        }
    }
}
