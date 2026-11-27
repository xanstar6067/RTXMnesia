using System;
using System.Collections.Generic;
using System.IO;
using RTXMnesia.Save;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Named states of the developer: ordinary save data in a folder of its own, never among the profile's saves
    /// (so the game's "continue" does not pick them up). Loading one starts a test session.
    /// </summary>
    public static class HplDebugSnapshots
    {
        public sealed class Entry
        {
            public string File, Title, Map, Date;
        }

        const string Extension = ".snapshot";

        /// <summary>Checks use their own folder.</summary>
        public static string FolderOverride;

        public static string Folder => FolderOverride ?? (Application.isEditor && Application.isBatchMode
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "BatchSnapshots"))
            : Path.Combine(Application.persistentDataPath, "DebugSnapshots"));

        /// <summary>Null where there is no game to keep (the title screen) or the file cannot be written.</summary>
        public static string Create(HplMap map, string title)
        {
            string json = HplSaveHandler.CreateJson(map);
            if (json == null) return null;
            title = Clean(title);
            if (title == "") title = map.MapName;
            string path = Path.Combine(Folder, title + Extension);
            for (int i = 2; File.Exists(path); i++) path = Path.Combine(Folder, title + " " + i + Extension);
            try { HplSaveHandler.WriteFile(path, json); }
            catch (Exception e)
            {
                Debug.LogError($"[DebugPanel] Could not write the snapshot '{path}': {e.Message}");
                return null;
            }
            return path;
        }

        static string Clean(string title)
        {
            title ??= "";
            foreach (char c in Path.GetInvalidFileNameChars()) title = title.Replace(c, ' ');
            title = title.Trim().TrimEnd('.');
            return title.Length > 80 ? title.Substring(0, 80).Trim() : title;
        }

        [Serializable] sealed class Header { public string mapFile; }

        /// <summary>Newest first.</summary>
        public static List<Entry> List()
        {
            var result = new List<Entry>();
            if (!Directory.Exists(Folder)) return result;
            var files = Directory.GetFiles(Folder, "*" + Extension);
            Array.Sort(files, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
            foreach (var file in files)
            {
                string map = "?";
                try { map = HplMapChange.FileToMapName(JsonUtility.FromJson<Header>(File.ReadAllText(file)).mapFile); }
                catch (Exception e) { Debug.LogWarning($"[DebugPanel] Unreadable snapshot '{file}': {e.Message}"); }
                result.Add(new Entry { File = file, Title = Path.GetFileNameWithoutExtension(file), Map = map,
                    Date = File.GetLastWriteTime(file).ToString("dd.MM HH:mm") });
            }
            return result;
        }

        public static void Delete(Entry entry)
        {
            if (entry != null && File.Exists(entry.File)) File.Delete(entry.File);
        }
    }
}
