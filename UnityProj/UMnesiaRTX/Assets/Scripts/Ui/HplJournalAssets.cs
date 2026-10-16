using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>Generated journal paper, navigation graphics and original bitmap fonts.</summary>
    public sealed class HplJournalAssets : ScriptableObject
    {
        public const string ResourceName = "HplJournal";
        [Serializable] public struct Image { public string file; public Texture2D texture; }
        public HplFont textFont, menuFont;
        public List<Image> images = new();
        HplNameIndex<Texture2D> _images;
        public void ResetLookup() => _images = null;
        void OnEnable() => ResetLookup();
        public Texture2D Texture(string file)
        {
            if (_images == null)
            {
                _images = new HplNameIndex<Texture2D>();
                foreach (var i in images)
                    _images.Add(System.IO.Path.GetFileNameWithoutExtension(i.file).ToLowerInvariant(), System.IO.Path.ChangeExtension(i.file, null).Replace('\\', '/').ToLowerInvariant(), i.texture);
            }
            return _images.Find(file);
        }
    }
}
