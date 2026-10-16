using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Generated insanity event data (Resources/HplInsanity.asset, InsanityConverter): the elements of game.cfg's
    /// Insanity EventsFile (misc/main_sanity_events.cfg) in file order, with their attributes as written, and the GUI
    /// images they name (BugImage, FadeImageFile).
    /// </summary>
    public sealed class HplInsanityAssets : ScriptableObject
    {
        public const string ResourceName = "HplInsanity";

        [Serializable]
        public sealed class EventData
        {
            [Tooltip("The element name: Bugs, Particles, SoundStream, Steps.")]
            public string type;
            public List<string> keys = new();
            public List<string> values = new();

            /// <summary>The attribute, or null (TinyXML: the first attribute with the name).</summary>
            public string Get(string key)
            {
                int i = keys.IndexOf(key);
                return i >= 0 ? values[i] : null;
            }
        }

        [Serializable]
        public sealed class Image
        {
            public string name;
            public Texture2D texture;
        }

        public string eventsFile = "";
        public List<EventData> events = new();
        public List<Image> images = new();

        public Texture2D GetImage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var image in images)
                if (string.Equals(image.name, name, StringComparison.OrdinalIgnoreCase)) return image.texture;
            return null;
        }

        static HplInsanityAssets _instance;
        static bool _loaded;

        /// <summary>The imported asset, or null (then there are no insanity events).</summary>
        public static HplInsanityAssets Instance
        {
            get
            {
                if (_loaded) return _instance;
                _loaded = true;
                _instance = Resources.Load<HplInsanityAssets>(ResourceName);
                if (_instance == null) Debug.LogWarning("[RTXMnesia] Insanity events missing: Tools > RTXMnesia > Import > Insanity events");
                return _instance;
            }
        }
    }
}
