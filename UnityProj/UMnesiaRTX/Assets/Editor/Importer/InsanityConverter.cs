using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RTXMnesia.Effects;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// cLuxInsanityHandler::LoadEvents: game.cfg Insanity EventsFile (misc/main_sanity_events.cfg) → the elements and
    /// their attributes in file order, plus the GUI images they name (BugImage, FadeImageFile), updated in place.
    /// </summary>
    public static class InsanityConverter
    {
        public const string AssetPath = AmnesiaPaths.ImportedAssetsRoot + "/Resources/HplInsanity.asset";

        [MenuItem("Tools/RTXMnesia/Import/Insanity events")]
        public static void ConvertBatch()
        {
            var log = new ImportLog();
            Convert(new ResourceIndex(AmnesiaPaths.SourceRoot), log);
            log.Save("Insanity events");
            if (log.Errors > 0) throw new InvalidOperationException("Insanity event import failed; see AmnesiaImport.log");
        }

        public static HplInsanityAssets Convert(ResourceIndex index, ImportLog log)
        {
            var gameConfig = ConfigConverter.Convert("config/game.cfg", log);
            string eventsFile = gameConfig != null ? gameConfig.GetString("Insanity", "EventsFile", "") : "";
            var events = new List<HplInsanityAssets.EventData>();
            var images = new List<HplInsanityAssets.Image>();
            if (eventsFile == "") log.Warn("No sanity events file specified!");
            else
            {
                string file = Path.Combine(AmnesiaPaths.SourceRoot, eventsFile);
                if (!File.Exists(file)) file = index.Find(Path.GetFileName(eventsFile));
                try
                {
                    if (file == null || !File.Exists(file)) throw new FileNotFoundException("not found");
                    var doc = new XmlDocument();
                    doc.Load(file);
                    foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                    {
                        if (node is not XmlElement element) continue;
                        var data = new HplInsanityAssets.EventData { type = element.Name };
                        foreach (XmlAttribute attribute in element.Attributes)
                        {
                            if (data.keys.Contains(attribute.Name)) continue;
                            data.keys.Add(attribute.Name);
                            data.values.Add(attribute.Value);
                        }
                        events.Add(data);
                        foreach (string key in new[] { "BugImage", "FadeImageFile" })
                        {
                            string image = data.Get(key);
                            if (string.IsNullOrEmpty(image) || images.Exists(i => string.Equals(i.name, image, StringComparison.OrdinalIgnoreCase))) continue;
                            var texture = Gui(index, image, log);
                            if (texture != null) images.Add(new HplInsanityAssets.Image { name = image, texture = texture });
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error($"Could not load sanity events file: '{eventsFile}' ({e.Message})");
                }
            }

            var assets = AssetDatabase.LoadAssetAtPath<HplInsanityAssets>(AssetPath);
            bool created = assets == null;
            if (created) assets = ScriptableObject.CreateInstance<HplInsanityAssets>();
            assets.eventsFile = eventsFile;
            assets.events = events;
            assets.images = images;
            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(AssetPath)));
                AssetDatabase.CreateAsset(assets, AssetPath);
            }
            else EditorUtility.SetDirty(assets);
            AssetDatabase.SaveAssets();
            log.Info($"Insanity events: {events.Count} events, {images.Count} images from {eventsFile} -> {AssetPath}");
            return assets;
        }

        static Texture2D Gui(ResourceIndex index, string name, ImportLog log)
        {
            string file = index.Find(name);
            if (file == null || !File.Exists(file))
            {
                log.Error("Missing insanity image: " + name);
                return null;
            }
            string path = TextureConverter.Convert(file, TextureRole.Gui, null, false, out bool written);
            if (written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) log.Error("Insanity image was not imported: " + name);
            return texture;
        }
    }
}
