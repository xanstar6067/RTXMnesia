using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RTXMnesia.Save
{
    /// <summary>
    /// Game profiles (cLuxBase::CreateProfile / SetProfile, cLuxMainMenu_Profile): a folder per profile with its user
    /// settings (game and input options), key config and saves. Graphics, sound volume and gamma stay global, as the
    /// main config of HPL2.
    ///
    /// Deviations: the folders are Documents/RTXMnesia/Profiles/&lt;name&gt; (never Documents/Amnesia); the saves of
    /// both campaigns are in Saves/&lt;campaign&gt; of the profile (HPL2 has one folder for both). The last chosen
    /// profile is remembered: the profile window still opens at the first title screen of a run, as in HPL2, with it
    /// selected (HPL2 selects the oldest), and a map played directly in the editor uses it (HPL2 creates DefaultProfileName).
    /// Saves of the time before profiles (Documents/RTXMnesia/Saves) move into the first created profile.
    /// </summary>
    public static class HplProfiles
    {
        /// <summary>cLuxMainMenu_Profile: the name text box takes up to 20 characters without these.</summary>
        public const int MaxNameLength = 20;
        public const string IllegalChars = "*/\\:<>|\"?";
        const string LastKey = "RTXMnesia.LastProfile";

        /// <summary>Tests: another folder instead of Documents/RTXMnesia/Profiles.</summary>
        public static string RootOverride;

        public static string Root => RootOverride ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RTXMnesia", "Profiles");

        /// <summary>Documents/RTXMnesia/Saves of the version without profiles.</summary>
        public static string LegacySaves => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RTXMnesia", "Saves");

        static string s_current;
        static bool s_chosen, s_lastRead;

        /// <summary>The profile in use; null — none (batch runs, or nothing created yet).</summary>
        public static string Current
        {
            get
            {
                // A map started directly (editor) uses the last chosen profile.
                if (s_current == null && !s_lastRead)
                {
                    s_lastRead = true;
                    if (!Application.isBatchMode || RootOverride != null)
                    {
                        string last = PlayerPrefs.GetString(LastKey, "");
                        if (last != "" && Exists(last)) s_current = last;
                    }
                }
                return s_current;
            }
        }

        /// <summary>cLuxMainMenu: no user config yet — the title screen starts with the profile window.</summary>
        public static bool ChosenThisRun => s_chosen;

        /// <summary>The last chosen profile (preselected in the profile window), if it still exists.</summary>
        public static string Last
        {
            get
            {
                string last = PlayerPrefs.GetString(LastKey, "");
                return last != "" && Exists(last) ? last : null;
            }
        }

        /// <summary>The folder of the profile in use; null without one.</summary>
        public static string Folder => Current != null ? Path.Combine(Root, Current) : null;

        /// <summary>A file of the profile in use; null without one.</summary>
        public static string File(string name) => Folder != null ? Path.Combine(Folder, name) : null;

        /// <summary>Profile settings or saves change: user settings and keys are read again.</summary>
        public static event Action Changed;

        public static bool Exists(string name) => !string.IsNullOrEmpty(name) && Directory.Exists(Path.Combine(Root, name));

        /// <summary>cLuxMainMenu_Profile::AddProfilesInListBox: the folders by creation date, oldest first.</summary>
        public static List<string> List()
        {
            var names = new List<string>();
            if (!Directory.Exists(Root)) return names;
            var dirs = new List<DirectoryInfo>(new DirectoryInfo(Root).GetDirectories());
            dirs.Sort((a, b) => a.CreationTime.CompareTo(b.CreationTime));
            foreach (var d in dirs) names.Add(d.Name);
            return names;
        }

        /// <summary>cLuxMainMenu_Profile::PressEnterNameCreate: spaces trimmed at both ends; empty or existing — false.</summary>
        public static string CleanName(string name)
        {
            name = (name ?? "").Trim(' ');
            return name;
        }

        /// <summary>cLuxBase::CreateProfile: false if the folder exists.</summary>
        public static bool Create(string name)
        {
            name = CleanName(name);
            if (name == "" || name.IndexOfAny(IllegalChars.ToCharArray()) >= 0) return false;
            bool first = List().Count == 0;
            string path = Path.Combine(Root, name);
            if (Directory.Exists(path)) return false;
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RTXMnesia] Could not create profile '{name}': {e.Message}");
                return false;
            }
            if (first && RootOverride == null) MoveLegacySaves(path);
            return true;
        }

        static void MoveLegacySaves(string profile)
        {
            if (!Directory.Exists(LegacySaves)) return;
            try
            {
                Directory.Move(LegacySaves, Path.Combine(profile, "Saves"));
                Debug.Log($"[RTXMnesia] Saves without a profile moved to '{profile}'");
            }
            catch (Exception e) { Debug.LogWarning($"[RTXMnesia] Could not move old saves into the profile: {e.Message}"); }
        }

        /// <summary>cLuxMainMenu_Profile::SelectProfile → cLuxBase::SetProfile + InitUserConfig.</summary>
        public static void Select(string name)
        {
            if (!Exists(name)) return;
            s_current = name;
            s_lastRead = true;
            s_chosen = true;
            PlayerPrefs.SetString(LastKey, name);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>
        /// cLuxMainMenu_Profile::PressDeleteProfilePopupClose: the profile in use is unset first; false if the folder
        /// could not be removed ("Could not remove profile").
        /// </summary>
        public static bool Delete(string name)
        {
            if (!Exists(name)) return false;
            if (string.Equals(s_current, name, StringComparison.Ordinal))
            {
                s_current = null;
                s_chosen = false;
                Changed?.Invoke();
            }
            try
            {
                Directory.Delete(Path.Combine(Root, name), true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RTXMnesia] Could not remove profile '{name}': {e.Message}");
                return false;
            }
        }

        /// <summary>Tests (and a new Play session): forget the profile state of the run.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetForTests()
        {
            s_current = null;
            s_chosen = false;
            s_lastRead = false;
        }

        /// <summary>
        /// Reads a JSON file of the profile in use into <paramref name="target"/>; false without a profile or the file
        /// (<paramref name="missing"/>: a profile is in use but has no such file — its defaults apply).
        /// </summary>
        public static bool ReadJson(string file, object target) => ReadJson(file, target, out _);

        public static bool ReadJson(string file, object target, out bool missing)
        {
            string path = File(file);
            missing = path != null && !System.IO.File.Exists(path);
            if (path == null || missing) return false;
            try
            {
                JsonUtility.FromJsonOverwrite(System.IO.File.ReadAllText(path), target);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RTXMnesia] Invalid profile file '{path}': {e.Message}");
                return false;
            }
        }

        /// <summary>Writes a JSON file of the profile in use; false without one.</summary>
        public static bool WriteJson(string file, object value) => WriteJson(null, file, value);

        /// <summary>Writes a JSON file of the named profile (null: the one in use); false without one.</summary>
        public static bool WriteJson(string profile, string file, object value)
        {
            string path = profile != null ? Path.Combine(Root, profile, file) : File(file);
            if (path == null) return false;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                System.IO.File.WriteAllText(path, JsonUtility.ToJson(value, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RTXMnesia] Could not write profile file '{path}': {e.Message}");
                return false;
            }
        }
    }
}
