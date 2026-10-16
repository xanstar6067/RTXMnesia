using System;

namespace RTXMnesia.Hpl
{
    /// <summary>Campaign context belongs to a scene, never to a global import switch.</summary>
    public sealed class HplCampaign
    {
        public static readonly HplCampaign Main = new(false);
        public static readonly HplCampaign Justine = new(true);
        public bool IsJustine { get; }
        public string Id => IsJustine ? "justine" : "main";
        public string InitFile => IsJustine ? "config/ptest_main_init.cfg" : "config/main_init.cfg";
        public string GameConfig => IsJustine ? "config/ptest_game.cfg" : "config/game.cfg";
        public string MenuConfig => IsJustine ? "config/ptest_menu.cfg" : "config/menu.cfg";
        public string PreMenuConfig => IsJustine ? "config/ptest_pre_menu.cfg" : "config/pre_menu.cfg";
        public string Materials => IsJustine ? "ptest_materials.cfg" : "materials.cfg";
        public string LanguageFolder => IsJustine ? "config/lang_ptest" : "config/lang_main";
        public string MenuMap => IsJustine ? "main_menu/ptest/ptest_menu.map" : "main_menu/main/menu_bg.map";
        public string FirstMap => IsJustine ? "maps/ptest/01_cells.map" : "maps/main/ch01/00_rainy_hall.map";
        public string MenuScene => "Assets/AmnesiaImported/Levels/" + MenuMap.Replace(".map", ".unity");
        public string FirstScene => "Assets/AmnesiaImported/Levels/" + FirstMap.Replace(".map", ".unity");
        public string ResourceName(string name) => (IsJustine ? "Justine/" : "") + name;
        /// <summary>Keep the existing main asset paths and give DLC data distinct identity.</summary>
        public string AssetPath(string mainPath)
        {
            if (!IsJustine) return mainPath;
            int slash = mainPath.LastIndexOf('/');
            return mainPath.Insert(slash + 1, "Justine/");
        }
        HplCampaign(bool justine) => IsJustine = justine;
        public static HplCampaign ForLanguage(HplLanguage language) => language != null && language.campaignId == "justine" ? Justine : Main;
        public static HplCampaign ForMap(string map)
        {
            string path = (map ?? "").Replace('\\', '/');
            return path.StartsWith("maps/ptest/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("main_menu/ptest/", StringComparison.OrdinalIgnoreCase) ? Justine : Main;
        }
    }
}
