using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// Generated developer commentary data: every commentary/*.comment file (Talker, Topic, SoundFile), read by
    /// cLuxCommentaryIcon::LoadCommentaryFile. Ignored Resources/HplCommentary.asset.
    /// </summary>
    public sealed class HplCommentaryAssets : ScriptableObject
    {
        public const string ResourceName = "HplCommentary";

        [Serializable]
        public sealed class Entry
        {
            public string file = "", talker = "", topic = "", soundFile = "";
        }

        public List<Entry> entries = new();

        static HplCommentaryAssets s_instance;
        static bool s_loaded;

        public static HplCommentaryAssets Instance
        {
            get
            {
                if (!s_loaded)
                {
                    s_instance = Resources.Load<HplCommentaryAssets>(ResourceName);
                    s_loaded = true;
                }
                return s_instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_instance = null;
            s_loaded = false;
        }

        /// <summary>By file name without folder or extension (the resource search of HPL2 by name).</summary>
        public Entry Find(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string name = System.IO.Path.GetFileNameWithoutExtension(file.Replace('\\', '/'));
            return entries.Find(e => string.Equals(e.file, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
