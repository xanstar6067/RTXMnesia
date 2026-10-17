using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>
    /// Generated pre menu resources (cLuxPreMenu, config/pre_menu.cfg via main_init.cfg ConfigFiles PreMenu): the
    /// sections with their images, texts and music, the gamma preview image. Game resources, never committed.
    /// </summary>
    public sealed class HplPreMenuAssets : ScriptableObject
    {
        public const string ResourceName = "HplPreMenu";

        /// <summary>cLuxPreMenuTextElement: a word wrapped label in its frame (Pos z + 1 is the draw depth).</summary>
        [Serializable]
        public sealed class Text
        {
            public string category = "", entry = "";
            public Vector2 position, frameSize, fontSize;
            public Color color = Color.white;
            public HplFontAlign align = HplFontAlign.Left;
        }

        /// <summary>cLuxPreMenuSection.</summary>
        [Serializable]
        public sealed class Section
        {
            public string imageFile = "";
            public Texture2D image;
            public Color color = new(0f, 0f, 0f, 1f);
            public float time = 2f;
            public bool showFirstStartOnly, hasGammaSettings, allowSkipping = true;
            public string music = "";
            public float musicVolume, musicFadeTime;
            public List<Text> texts = new();
        }

        public List<Section> sections = new();
        /// <summary>graphics/main_menu/menu_gamma.tga.</summary>
        public Texture2D gammaImage;
        /// <summary>Assets/Shaders/HplGui.shader, for the gamma preview (referenced so builds include it).</summary>
        public Shader guiShader;
    }
}
