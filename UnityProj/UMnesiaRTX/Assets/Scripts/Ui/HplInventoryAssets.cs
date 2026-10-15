using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>Generated inventory graphics / item definitions. Stored in the ignored imported Resources folder.</summary>
    public sealed class HplInventoryAssets : ScriptableObject
    {
        public const string ResourceName = "HplInventory";
        [Serializable] public struct Image { public string file; public Texture2D texture; }
        [Serializable] public class Definition
        {
            public string file, type, subType, image;
            public float amount;
        }
        public HplFont defaultFont, headerFont;
        public List<Image> images = new();
        public List<Definition> items = new();
        HplNameIndex<Texture2D> _images;
        HplNameIndex<Definition> _items;
        public void ResetLookup() { _images = null; _items = null; }
        void OnEnable() => ResetLookup();

        public Texture2D Texture(string file)
        {
            if (_images == null)
            {
                _images = new HplNameIndex<Texture2D>();
                foreach (var image in images) _images.Add(System.IO.Path.GetFileNameWithoutExtension(image.file).ToLowerInvariant(), System.IO.Path.ChangeExtension(image.file, null).Replace('\\', '/').ToLowerInvariant(), image.texture);
            }
            return _images.Find(file);
        }
        public Definition Item(string file)
        {
            if (_items == null)
            {
                _items = new HplNameIndex<Definition>();
                foreach (var item in items) _items.Add(System.IO.Path.GetFileNameWithoutExtension(item.file).ToLowerInvariant(), System.IO.Path.ChangeExtension(item.file, null).Replace('\\', '/').ToLowerInvariant(), item);
            }
            return _items.Find(file);
        }
    }
}
