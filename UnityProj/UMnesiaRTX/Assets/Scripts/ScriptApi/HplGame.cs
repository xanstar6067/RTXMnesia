using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// Game-wide state that outlives a map: global script variables (cLuxGlobalDataHandler), quest notes
    /// (cLuxJournal), given / blocked hints (cLuxHintHandler) and inventory items by name (cLuxInventory).
    /// Reset for a new game (and when play mode starts); a save file holds it (<see cref="CaptureSave"/>).
    /// </summary>
    public static class HplGame
    {
        [Serializable]
        public class QuestNote
        {
            public string name;
            public string nameEntry;  // "Quest_<entry>_Name" in category Journal
            public string textEntry;  // "Quest_<entry>_Text"
            public bool active;
        }

        [Serializable]
        public class Item
        {
            public string name;
            public string type;
            public string subType;
            public string image;
            public float amount;
            public string val = "", extraVal = "";
            public int count = 1;
        }

        [Serializable]
        public sealed class CombineCallback
        {
            public string name, itemA, itemB, function;
            public bool autoDestroy;
        }
        public static readonly List<CombineCallback> CombineCallbacks = new();
        public static Levels.InventoryScript InventoryScript { get; private set; }

        public static void EnsureInventoryScript(HplMap map)
        {
            // Justine inventory.hps has no operations; base recipes belong only to the main campaign.
            if (map.Campaign.IsJustine) return;
            bool first = InventoryScript == null;
            InventoryScript ??= new Levels.InventoryScript();
            InventoryScript.Map = map;
            if (first) InventoryScript.OnStart();
        }

        [Serializable]
        public sealed class JournalEntry
        {
            public string type, textEntry, image;
            public int number;
            public bool read;
            public string NameEntry => type == "Diary" ? "Diary_" + textEntry + "_Name" + number : "Note_" + textEntry + "_Name";
            public string TextEntry => type == "Diary" ? "Diary_" + textEntry + "_Text" + number : "Note_" + textEntry + "_Text";
            public string ImageFile(bool icon)
            {
                string file = (image ?? "").Replace('\\', '/');
                if (file == "") file = "journal/default_note";
                string ext = System.IO.Path.GetExtension(file);
                return (ext == "" ? file : file.Substring(0, file.Length - ext.Length)) + (icon ? "_icon" : "_large") + (ext == "" ? ".tga" : ext);
            }
        }
        public static readonly List<JournalEntry> JournalEntries = new();
        public static JournalEntry LastReadEntry;
        public static bool ShowJournalOnDiaryPickup = true;

        public static JournalEntry AddJournalEntry(string type, string textEntry, string image)
        {
            int number = type == "Diary" ? JournalEntries.FindAll(e => e.type == type && e.textEntry == textEntry).Count + 1 : 0;
            var entry = new JournalEntry { type = type, textEntry = textEntry, image = image, number = number };
            JournalEntries.Add(entry);
            return entry;
        }
        public static int Coins;

        static readonly string[] ItemTypes = { "Puzzle", "Coins", "Note", "Diary", "Lantern", "Health", "Sanity", "LampOil", "Tinderbox", "HandObject" };

        /// <summary>cLuxInventory::GetItemTypeFromString: the type name as the inventory knows it, "" if unknown.</summary>
        public static string ItemType(string name)
        {
            foreach (var t in ItemTypes) if (string.Equals(t, name, StringComparison.OrdinalIgnoreCase)) return t;
            HplLog.Error($"Item type '{name}' does not exist!");
            return "";
        }

        public static bool HasCount(string type) => type is "Health" or "Sanity" or "LampOil" or "Tinderbox";

        public static bool HasType(string type) => Items.Exists(i => i.type == type);

        public static void RemoveItem(Item item)
        {
            if (item == null) return;
            if (HasCount(item.type) && --item.count > 0) return;
            Items.Remove(item);
        }

        /// <summary>
        /// cLuxInventory::AddItem with the item types' BeforeAddItem (LuxItemType.cpp): tinderboxes and coins only
        /// count, notes and diaries go to the journal, the rest becomes an inventory item (counted for potions, oil).
        /// <paramref name="removeProp"/>: the picked-up prop is to be destroyed.
        /// </summary>
        public static Item AddItem(HplMap map, string name, string type, string subType, string image, float amount,
                                   string val, string extraVal, out bool removeProp)
        {
            removeProp = false;
            var hud = map.hud;

            if (type == "Tinderbox")
            {
                string collected = GetGlobalVar("TinderboxesCollected");
                int n = collected != "" ? Hpl.HplMath.Atoi(collected) + 1 : 0; // kept: the first one stays at 0
                SetGlobalVar("TinderboxesCollected", Hpl.HplMath.ToString(n));
            }

            if (HasCount(type))
            {
                var existing = Items.Find(i => i.subType == subType);
                if (existing != null)
                {
                    if (existing.count >= 99)
                    {
                        hud.Messages.SetMessage(hud.Translate("Inventory", "TooManyItemsOfSort"), 0f);
                        return null;
                    }
                    existing.count++;
                    removeProp = true;
                    return existing;
                }
            }

            image ??= "";
            var item = new Item { name = name, type = type, subType = subType, image = image.StartsWith("graphics/", StringComparison.OrdinalIgnoreCase) ? image : "graphics/item/" + image, amount = amount, val = val, extraVal = extraVal };

            bool consumed = false;
            switch (type)
            {
                case "Puzzle":
                    HplLog.Info("ProgLog: Picked up puzzle item " + name);
                    break;
                case "Coins":
                    Coins += (int)amount;
                    hud.Hints.Add("PickCoin", hud.Translate("Hints", "PickCoin"), 0f);
                    consumed = true;
                    break;
                case "Note":
                    var note = AddJournalEntry("Note", val, item.image);
                    HplLog.Info("ProgLog: Picked up note " + val);
                    if (amount > 0) map.GameUi?.OpenJournalEntry(note, amount >= 2, true);
                    consumed = true;
                    break;
                case "Diary":
                    var diary = AddJournalEntry("Diary", val, item.image);
                    HplLog.Info("ProgLog: Picked up diary " + val);
                    ShowJournalOnDiaryPickup = true;
                    if (extraVal != "") map.RunScript(extraVal, name, diary.number);
                    if (ShowJournalOnDiaryPickup) map.GameUi?.OpenJournalEntry(diary, true, true);
                    else LastReadEntry = diary;
                    consumed = true;
                    break;
                case "Lantern":
                    HplLog.Info("ProgLog: Picked up latern");
                    hud.Hints.Add("PickLantern", hud.Translate("Hints", "PickLantern"), 0f);
                    break;
                case "Health":
                    hud.Hints.Add("PickHealthPotion", hud.Translate("Hints", "PickHealthPotion"), 0f);
                    break;
                case "Sanity":
                    hud.Hints.Add("PickSanityPotion", hud.Translate("Hints", "PickSanityPotion"), 0f);
                    break;
                case "LampOil":
                    hud.Hints.Add("PickOil", hud.Translate("Hints", "PickOil"), 0f);
                    break;
                case "Tinderbox":
                    hud.Hints.Add("PickTinderbox", hud.Translate("Hints", "PickTinderbox"), 0f);
                    if (map.player != null) map.player.tinderboxes += 1;
                    consumed = true;
                    break;
            }
            removeProp = true;
            if (consumed) return null;
            // No slot limit, as in cLuxInventory::AddItem: items past the grid are kept but not shown.
            Items.Add(item);
            return item;
        }

        /// <summary>iLuxItemType::ShowPickUpMessage: notes and diaries show none.</summary>
        public static bool ShowPickUpMessage(string type) => type != "Note" && type != "Diary";

        public static readonly Dictionary<string, string> GlobalVars = new(StringComparer.Ordinal);
        public static readonly List<QuestNote> Quests = new();
        public static readonly HashSet<string> HintsGiven = new(StringComparer.Ordinal);
        public static readonly HashSet<string> HintsBlocked = new(StringComparer.Ordinal);
        public static readonly List<Item> Items = new();

        /// <summary>cLuxGlobalDataHandler::mfEnemyActivateSoundCount: 5 s after an enemy's activation sound no other plays.</summary>
        public static float EnemyActivateSoundCount;
        public static bool EnemyActivateSoundAllowed => EnemyActivateSoundCount <= 0f;
        public static void SetEnemyActivateSoundMade() => EnemyActivateSoundCount = 5f;

        /// <summary>cLuxGlobalDataHandler::Update.</summary>
        public static void Update(float timeStep)
        {
            if (EnemyActivateSoundCount > 0f) EnemyActivateSoundCount -= timeStep;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            EnemyActivateSoundCount = 0f;
            // cLuxInsanityHandler::Reset (its disabled sets and used events stay, as in HPL2).
            Effects.HplInsanity.Reset();
            GlobalVars.Clear();
            Quests.Clear();
            HintsGiven.Clear();
            HintsBlocked.Clear();
            Items.Clear();
            JournalEntries.Clear();
            LastReadEntry = null;
            ShowJournalOnDiaryPickup = true;
            Coins = 0;
            CombineCallbacks.Clear();
            InventoryScript = null;
            Player.PlayerStateVars.Reset();
            // cLuxMapHandler::Reset, cLuxLoadScreenHandler::Reset
            HplSavedMaps.Reset();
            HplMapChange.ClearPending();
            Ui.HplLoadScreen.ResetText();
            Ui.HplCredits.ResetKeyParts();
        }

        // ------------------------------------------------------------------ save (step 6)

        /// <summary>cLuxInventory_SaveData, cLuxJournal_SaveData, cLuxGlobalDataHandler_SaveData, cLuxHintHandler_SaveData.</summary>
        public static void CaptureSave(Save.SaveGame save, HplMap map)
        {
            save.inventory = new Save.InventorySave
            {
                items = new List<Item>(Items),
                combineCallbacks = new List<CombineCallback>(CombineCallbacks),
                scriptStarted = InventoryScript != null,
                disabled = map != null && map.Script != null && map.Script.InventoryDisabled,
            };
            save.journal = new Save.JournalSave
            {
                entries = new List<JournalEntry>(JournalEntries),
                quests = new List<QuestNote>(Quests),
                lastReadEntry = LastReadEntry != null ? JournalEntries.IndexOf(LastReadEntry) : -1,
                showJournalOnDiaryPickup = ShowJournalOnDiaryPickup,
            };
            save.globalVars.Clear();
            foreach (var pair in GlobalVars) save.globalVars.Add(new Save.SavedVar { name = pair.Key, value = pair.Value });
            save.hintsGiven = new List<string>(HintsGiven);
            save.hintsBlocked = new List<string>(HintsBlocked);
        }

        /// <summary>
        /// The ToInventory / ToJournal / ToGlobalDataHandler / ToHintHandler part of loading a save, on a reset game. The
        /// inventory script is loaded without its OnGameStart: its combine callbacks come from the save.
        /// </summary>
        public static void ApplySave(Save.SaveGame save, HplMap map)
        {
            Items.Clear();
            Items.AddRange(save.inventory.items);
            CombineCallbacks.Clear();
            CombineCallbacks.AddRange(save.inventory.combineCallbacks);
            if (save.inventory.scriptStarted && !map.Campaign.IsJustine)
            {
                InventoryScript = new Levels.InventoryScript { Map = map };
            }
            if (map.Script != null) map.Script.InventoryDisabled = save.inventory.disabled;
            JournalEntries.Clear();
            JournalEntries.AddRange(save.journal.entries);
            Quests.Clear();
            Quests.AddRange(save.journal.quests);
            int last = save.journal.lastReadEntry;
            LastReadEntry = last >= 0 && last < JournalEntries.Count ? JournalEntries[last] : null;
            ShowJournalOnDiaryPickup = save.journal.showJournalOnDiaryPickup;
            GlobalVars.Clear();
            foreach (var v in save.globalVars) GlobalVars[v.name] = v.value;
            HintsGiven.Clear();
            HintsGiven.UnionWith(save.hintsGiven);
            HintsBlocked.Clear();
            HintsBlocked.UnionWith(save.hintsBlocked);
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
