using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// Game-wide state that outlives a map: global script variables (cLuxGlobalDataHandler), quest notes
    /// (cLuxJournal), given / blocked hints (cLuxHintHandler) and inventory items by name (cLuxInventory).
    /// Reset for a new game (and when play mode starts). Saving comes with step 6.
    /// </summary>
    public static class HplGame
    {
        public class QuestNote
        {
            public string name;
            public string nameEntry;  // "Quest_<entry>_Name" in category Journal
            public string textEntry;  // "Quest_<entry>_Text"
            public bool active;
        }

        public class Item
        {
            public string name;
            public string type;
            public string subType;
            public string image;
            public float amount;
        }

        public static readonly Dictionary<string, string> GlobalVars = new(StringComparer.Ordinal);
        public static readonly List<QuestNote> Quests = new();
        public static readonly HashSet<string> HintsGiven = new(StringComparer.Ordinal);
        public static readonly HashSet<string> HintsBlocked = new(StringComparer.Ordinal);
        public static readonly List<Item> Items = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            GlobalVars.Clear();
            Quests.Clear();
            HintsGiven.Clear();
            HintsBlocked.Clear();
            Items.Clear();
        }

        /// <summary>cLuxGlobalDataHandler::GetVar: created empty on first use.</summary>
        public static string GetGlobalVar(string name) => GlobalVars.TryGetValue(name, out string v) ? v : GlobalVars[name] = "";

        public static void SetGlobalVar(string name, string value) => GlobalVars[name] = value;

        public static QuestNote GetQuest(string name) => Quests.Find(q => q.name == name);

        /// <summary>cLuxJournal::AddQuestNote: false if a quest with that name exists already.</summary>
        public static bool AddQuest(string name, string nameAndTextEntry)
        {
            if (GetQuest(name) != null) return false;
            Quests.Add(new QuestNote
            {
                name = name,
                nameEntry = "Quest_" + nameAndTextEntry + "_Name",
                textEntry = "Quest_" + nameAndTextEntry + "_Text",
                active = true,
            });
            return true;
        }

        /// <summary>cLuxJournal::DisableQuestNote: true if the quest was active.</summary>
        public static bool DisableQuest(string name)
        {
            var quest = GetQuest(name);
            if (quest == null)
            {
                HplLog.Error($"Quest '{name}' does not exist!");
                return false;
            }
            if (!quest.active) return false;
            quest.active = false;
            return true;
        }

        public static Item GetItem(string name) => Items.Find(i => i.name == name);
    }
}
