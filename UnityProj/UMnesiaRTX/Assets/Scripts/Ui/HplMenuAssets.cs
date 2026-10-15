using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>Imported fonts and gui_main_menu.skin graphics. Generated game resources, never committed.</summary>
    public sealed class HplMenuAssets : ScriptableObject
    {
        public const string ResourceName = "HplMenus";
        [Serializable] public struct Piece
        {
            public string type;
            public Texture2D texture;
            public Color color;
        }
        public HplFont menuFont, guiFont;
        public List<Piece> pieces = new();

        public Piece Find(string type) => pieces.Find(p => p.type == type);
    }
}
