using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>Generated load screen resources: the menu.cfg LoadScreen font and the graphics/general loading images.</summary>
    public sealed class HplLoadScreenAssets : ScriptableObject
    {
        public const string ResourceName = "HplLoadScreen";
        [Serializable] public struct Image { public string file; public Texture2D texture; }
        public HplFont defaultFont;
        public List<Image> images = new();
        HplNameIndex<Texture2D> _images;
        public void ResetLookup() => _images = null;
        void OnEnable() => ResetLookup();
        public Texture2D Texture(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
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
