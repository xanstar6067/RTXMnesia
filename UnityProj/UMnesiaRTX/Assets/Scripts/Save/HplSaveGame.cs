using System;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Save
{
    /// <summary>cLuxSaveHandler::CreateSaveGameData / LoadSaveGameData: the game modules to and from a <see cref="SaveGame"/>.</summary>
    public static class HplSaveGame
    {
        /// <summary>cLuxSaveHandler::CreateSaveGameData (the saved maps are set before writing, as the save thread does).</summary>
        public static SaveGame Create(HplMap map)
        {
            var save = new SaveGame
            {
                campaign = map.Campaign.Id,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                hardMode = HplDifficulty.HardMode,
                mapFile = map.mapFile,
                map = HplSavedMaps.CaptureCurrent(map),
            };
            HplGame.CaptureSave(save, map);
            if (map.player != null)
            {
                save.player = map.player.CaptureSave();
                save.player.coins = HplGame.Coins;
            }
            if (map.sound != null && map.sound.Music != null) save.music = map.sound.Music.CaptureSave();
            if (map.effects != null) save.effects = map.effects.CaptureSave();
            save.insanity = Effects.HplInsanity.CaptureSave();
            save.loadScreen = HplLoadScreen.CaptureSave();
            save.savedMaps = HplSavedMaps.Export();
            return save;
        }

        /// <summary>
        /// cLuxSaveHandler::LoadSaveGameData on the map scene of the save, built and reset (HplGame.Reset before the scene
        /// activated): the map, hints, player, inventory, journal, music, effects, global data, insanity and load screen
        /// in the original order, then the saved maps. The level script's OnStart / OnEnter do not run.
        /// </summary>
        public static void Load(HplMap map, SaveGame save)
        {
            Debug.Log($"[RTXMnesia] LOADING SAVE GAME! ({save.mapFile}, {save.date})");
            HplSavedMaps.Apply(map, save.map);
            HplGame.ApplySave(save, map);
            if (map.player != null)
            {
                map.player.ApplySave(save.player);
                HplGame.Coins = save.player.coins;
            }
            if (map.sound != null)
            {
                map.sound.EnsureMusic();
                map.sound.Music.ApplySave(save.music);
            }
            map.effects?.ApplySave(save.effects);
            Effects.HplInsanity.ApplySave(map, save.insanity);
            HplLoadScreen.ApplySave(save.loadScreen);
            HplSavedMaps.Import(save.savedMaps);
        }
    }
}
