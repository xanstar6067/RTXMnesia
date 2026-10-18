using System;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// The hard mode (cLuxBase::mbHardMode, "HARDMODE" blocks of the game code): chosen in the start game window
    /// (main campaign only, main_init.cfg AllowHardMode), kept in the save (cLuxSaveGame_SaveData::mbHardmode) and
    /// set from it when a save loads. It survives <see cref="HplGame.Reset"/>, as the flag of cLuxBase.
    /// </summary>
    public static class HplDifficulty
    {
        public static bool HardMode { get; set; }

        /// <summary>glHardMode_SaveCost (LuxMainMenu.cpp): the tinderboxes a hard mode save costs.</summary>
        public const int SaveCost = 4;

        /// <summary>
        /// The hard mode menu texts. Deviation (as the key typos fixed elsewhere): base_russian.lang has the Russian
        /// HardModeSaveCost / HardModeSavePopup and "HardModeTinderboxInInvenory" (typo) in a category of their own
        /// named HardModeSaveCost instead of MainMenu, so HPL2 showed the English fallback in the Russian game; they
        /// are read from there first. HardModeStartNewGame has no Russian entry (an empty category): own text.
        /// </summary>
        public static string Text(Func<string, string, string, string> translate, string key)
        {
            string misplaced = key switch
            {
                "HardModeSaveCost" => "HardModeSaveCost",
                "HardModeSavePopup" => "HardModeSavePopup",
                "HardModeTinderboxInInventory" => "HardModeTinderboxInInvenory",
                _ => null,
            };
            if (misplaced != null)
            {
                string text = translate("HardModeSaveCost", misplaced, null);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            if (key == "HardModeStartNewGame") return "Начать новую игру в сложном режиме?";
            return translate("MainMenu", key, "") ?? "";
        }
    }
}
