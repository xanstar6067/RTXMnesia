using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Save
{
    /// <summary>
    /// cLuxSaveHandler (LuxSaveHandler.cpp): auto saves named "AutoSave_&lt;map display name&gt;_Y_M_D_h_m_s_n.sav", at most
    /// Saving/MaxAutoSaves of game.cfg (the oldest go first), the newest one for Continue, the list for Load game,
    /// and the pending load that the loaded map scene takes.
    ///
    /// Deviation: the files are JSON (JsonUtility of <see cref="SaveGame"/>) in our own folder, Documents/RTXMnesia/
    /// Saves/&lt;campaign&gt;, never in the game's Documents/Amnesia: saves of the original stay untouched. No profiles yet.
    /// The file is written at once through a temporary file (HPL2 writes on a save thread).
    /// </summary>
    public static class HplSaveHandler
    {
        public const string Extension = ".sav";

        /// <summary>Tools and tests: another folder instead of Documents/RTXMnesia/Saves.</summary>
        public static string FolderOverride;

        /// <summary>
        /// Documents/RTXMnesia/Saves; batch runs of the editor (tests, simulations, previews whose level scripts call
        /// AutoSave) write to Logs/BatchSaves of the project instead.
        /// </summary>
        public static string RootFolder
        {
            get
            {
                if (FolderOverride != null) return FolderOverride;
                if (Application.isEditor && Application.isBatchMode)
                    return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "BatchSaves"));
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RTXMnesia", "Saves");
            }
        }

        /// <summary>gpBase->msProfileSavePath: one folder per campaign.</summary>
        public static string Folder(HplCampaign campaign) => Path.Combine(RootFolder, campaign.Id);

        static DateTime s_latestSaveDate;
        static int s_saveNameCount;

        // ------------------------------------------------------------------ pending load

        static SaveGame s_pending;

        /// <summary>LoadGameFromFile: the save waits for its map scene, which loads it instead of OnStart / OnEnter.</summary>
        public static void BeginLoad(SaveGame save) => s_pending = save;

        public static bool LoadPending => s_pending != null;

        /// <summary>The save for the map scene being built, if one is pending for it.</summary>
        public static SaveGame TakePending(string mapFile)
        {
            if (s_pending == null) return null;
            var save = s_pending;
            s_pending = null;
            if (HplMapChange.FileToMapName(save.mapFile) != HplMapChange.FileToMapName(mapFile))
            {
                HplLog.PortError($"Save of '{save.mapFile}' was loaded into '{mapFile}'");
                return null;
            }
            return save;
        }

        public static void ClearPending() => s_pending = null;

        // ------------------------------------------------------------------ saving

        /// <summary>cLuxSaveHandler::AutoSave: the oldest saves over the limit go, then a new auto save. Not in hard mode.</summary>
        public static string AutoSave(HplMap map)
        {
            if (map == null || map.menuBackground || map.player == null || map.disableAutoSaves) return null;
            string folder = Folder(map.Campaign);
            int max = map.gameConfig != null ? map.gameConfig.GetInt("Saving", "MaxAutoSaves", 20) : 20;
            DeleteOldestSaveFiles(folder, max);
            string path = Path.Combine(folder, GetSaveName(map, "AutoSave"));
            return SaveGameToFile(map, path) ? path : null;
        }

        /// <summary>cLuxSaveHandler::SaveGameToFile.</summary>
        public static bool SaveGameToFile(HplMap map, string path)
        {
            Debug.Log($"[RTXMnesia] -------- BEGIN SAVE TO: {path} ---------");
            try
            {
                var save = HplSaveGame.Create(map);
                string json = JsonUtility.ToJson(save);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                Debug.Log($"[RTXMnesia] -------- END SAVE ({json.Length / 1024} KB) ---------");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RTXMnesia] Could not save the game to '{path}': {e.Message}");
                return false;
            }
        }

        /// <summary>cLuxSaveHandler::LoadGameFromFile, the reading part: null (with an error) for an unreadable file.</summary>
        public static SaveGame ReadFile(string path)
        {
            try
            {
                var save = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));
                if (save == null || save.game != "RTXMnesia" || string.IsNullOrEmpty(save.mapFile))
                    throw new InvalidDataException("not an RTXMnesia save");
                if (save.version > SaveGame.CurrentVersion) throw new InvalidDataException($"version {save.version} is newer than {SaveGame.CurrentVersion}");
                return save;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RTXMnesia] Could not load save file '{path}': {e.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------------ files

        /// <summary>cLuxSaveHandler::GetSaveName: prefix, map display entry ("NULL" if none), date and a counter.</summary>
        static string GetSaveName(HplMap map, string prefix)
        {
            string mapName = string.IsNullOrEmpty(map.DisplayNameEntry) ? "NULL" : map.DisplayNameEntry;
            var now = DateTime.Now;
            var date = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            string name = $"{prefix}_{mapName}_{date.Year}_{date.Month}_{date.Day}_{date.Hour}_{date.Minute}_{date.Second}_{s_saveNameCount}{Extension}";
            s_saveNameCount++;
            if (s_saveNameCount >= 100 || date != s_latestSaveDate) s_saveNameCount = 0;
            s_latestSaveDate = date;
            return name;
        }

        static string[] SaveFiles(string folder) => Directory.Exists(folder) ? Directory.GetFiles(folder, "*" + Extension) : Array.Empty<string>();

        /// <summary>cLuxSaveHandler::DeleteOldestSaveFiles: by creation date until fewer than the maximum are left.</summary>
        static void DeleteOldestSaveFiles(string folder, int max)
        {
            var files = new List<string>(SaveFiles(folder));
            int lastCount = -1;
            while (files.Count >= max && lastCount != files.Count)
            {
                lastCount = files.Count;
                string oldest = null;
                DateTime oldestDate = default;
                foreach (string f in files)
                {
                    var d = File.GetCreationTime(f);
                    if (oldest == null || d < oldestDate)
                    {
                        oldest = f;
                        oldestDate = d;
                    }
                }
                if (oldest == null) break;
                try { File.Delete(oldest); }
                catch (Exception e) { Debug.LogWarning($"[RTXMnesia] Could not delete old save '{oldest}': {e.Message}"); }
                files.Remove(oldest);
            }
        }

        /// <summary>cLuxSaveHandler::GetNewestSaveFile: by modification date, null if none.</summary>
        public static string GetNewestSaveFile(HplCampaign campaign)
        {
            string newest = null;
            DateTime newestDate = default;
            foreach (string f in SaveFiles(Folder(campaign)))
            {
                var d = File.GetLastWriteTime(f);
                if (newest == null || d > newestDate)
                {
                    newest = f;
                    newestDate = d;
                }
            }
            return newest;
        }

        /// <summary>cLuxSaveHandler::SaveFileExists.</summary>
        public static bool SaveFileExists(HplCampaign campaign) => GetNewestSaveFile(campaign) != null;

        /// <summary>cLuxMainMenu_LoadGame::PopulateSavedGameList: the save files, newest first.</summary>
        public static List<string> ListSaves(HplCampaign campaign)
        {
            var files = new List<string>(SaveFiles(Folder(campaign)));
            files.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            return files;
        }

        /// <summary>
        /// cLuxSaveHandler::GetProperSaveName: "AutoSave - &lt;level name&gt; - D/M-Y hh:mm:ss" from the file name
        /// (nine parts separated by '_', the map name translated from category Levels).
        /// </summary>
        public static string GetProperSaveName(string file, Func<string, string, string> translate)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            var parts = name.Split('_');
            if (parts.Length != 9) return "INVALID SAVE FILE SYNTAX";
            for (int i = 5; i <= 7; i++) if (parts[i].Length == 1) parts[i] = "0" + parts[i];
            string level = translate != null ? translate("Levels", parts[1]) : parts[1];
            return $"{parts[0]} - {level} - {parts[4]}/{parts[3]}-{parts[2]} {parts[5]}:{parts[6]}:{parts[7]}";
        }

    }
}
