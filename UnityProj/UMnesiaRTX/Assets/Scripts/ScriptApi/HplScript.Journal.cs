using System.Collections.Generic;

namespace RTXMnesia.ScriptApi
{
    // Messages, quests, notes, hints and the inventory (LuxScriptHandler.cpp → cLuxMessageHandler, cLuxJournal,
    // cLuxHintHandler, cLuxInventory). Journal collections belong to the game, its screen to HplGameUi.
    public abstract partial class HplScript
    {
        protected void SetMessage(string asTextCategory, string asTextEntry, float afTime) =>
            Hud.Messages.SetMessage(Translate(asTextCategory, asTextEntry), afTime);

        protected void SetDeathHint(string asTextCategory, string asTextEntry) => Player.Death.SetHint(asTextCategory, asTextEntry);

        protected void DisableDeathStartSound() => Player.Death.DisableStartSound();

        protected void AddNote(string asNameAndTextEntry, string asImage) => HplGame.AddJournalEntry("Note", asNameAndTextEntry, asImage);

        protected void AddDiary(string asNameAndTextEntry, string asImage) => HplGame.AddJournalEntry("Diary", asNameAndTextEntry, asImage);

        protected void ReturnOpenJournal(bool abOpenJournal) => HplGame.ShowJournalOnDiaryPickup = abOpenJournal;

        /// <summary>A new quest note: "QuestAdded" hint and the quest icon (cLuxScriptHandler::AddQuest).</summary>
        protected void AddQuest(string asName, string asNameAndTextEntry)
        {
            if (!HplGame.AddQuest(asName, asNameAndTextEntry)) return;
            ProgLog("High", "Added Memento " + asName);
            Hud.Hints.Add("QuestAdded", Translate("Hints", "QuestAdded"), 0f);
            Hud.Messages.StartQuestAddedMessage();
        }

        /// <summary>Completion points (cLuxCompletionCountHandler) come with step 6.</summary>
        protected void CompleteQuest(string asName, string asNameAndTextEntry)
        {
            HplGame.AddQuest(asName, asNameAndTextEntry);
            if (HplGame.DisableQuest(asName))
            {
                ProgLog("High", "Completed Quest " + asName);
                Map.AddCompletionAmount(Map.hud.Completion != null ? Map.hud.Completion.QuestValue : 0, 6f);
            }
        }

        protected bool QuestIsCompleted(string asName)
        {
            var quest = HplGame.GetQuest(asName);
            return quest != null && !quest.active;
        }

        protected bool QuestIsAdded(string asName) => HplGame.GetQuest(asName) != null;

        protected void SetNumberOfQuestsInMap(int alNumberOfQuests) => Map.NumberOfQuests = alNumberOfQuests;

        protected void GiveHint(string asName, string asMessageCat, string asMessageEntry, float afTimeShown) =>
            Hud.Hints.Add(asName, Translate(asMessageCat, asMessageEntry), afTimeShown);

        protected void RemoveHint(string asName) => Hud.Hints.Remove(asName);

        protected void BlockHint(string asName) => HplGame.HintsBlocked.Add(asName);

        protected void UnBlockHint(string asName) => HplGame.HintsBlocked.Remove(asName);

        // ------------------------------------------------------------------ inventory (items by name)

        public bool InventoryDisabled { get; internal set; }

        protected void ExitInventory() => Map.GameUi?.CloseInventory();

        protected void SetInventoryDisabled(bool abX) => InventoryDisabled = abX;

        protected void SetInventoryMessage(string asTextCategory, string asTextEntry, float afTime) =>
            Map.GameUi?.Inventory?.SetMessage(Hud.ParseString(Translate(asTextCategory, asTextEntry)), afTime);

        protected void GiveItem(string asName, string asType, string asSubTypeName, string asImageName, float afAmount)
        {
            string type = HplGame.ItemType(asType);
            if (type != "") HplGame.AddItem(Map, asName, type, asSubTypeName, asImageName, afAmount, "", "", out _);
        }

        /// <summary>Item variables from the generated .ent definition bank, without creating a world prop.</summary>
        protected void GiveItemFromFile(string asName, string asFileName)
        {
            var bank = UnityEngine.Resources.Load<Ui.HplInventoryAssets>(Ui.HplInventoryAssets.ResourceName);
            var item = bank != null ? bank.Item(asFileName) : null;
            if (item == null) { HplLog.Error("Item definition not found: " + asFileName); return; }
            HplGame.AddItem(Map, asName, HplGame.ItemType(item.type), item.subType, item.image, item.amount, "", "", out _);
        }

        // The string overload removes the entire inventory entry; item-type consumption uses the pointer
        // overload (HplGame.RemoveItem) and decrements a counted stack by one, as in LuxInventory.cpp.
        protected void RemoveItem(string asName) => HplGame.Items.Remove(HplGame.GetItem(asName));

        protected bool HasItem(string asName) => HplGame.GetItem(asName) != null;

        sealed class UseItemCallback
        {
            public string name, item, entity, function;
            public bool autoDestroy;
        }
        readonly List<UseItemCallback> _useItemCallbacks = new();

        protected void AddUseItemCallback(string asName, string asItem, string asEntity, string asFunction, bool abAutoDestroy) =>
            _useItemCallbacks.Add(new UseItemCallback { name = asName, item = asItem, entity = asEntity, function = asFunction, autoDestroy = abAutoDestroy });

        public bool HasUseItemCallback(string item, string entity) => _useItemCallbacks.Exists(c => c.item == item && c.entity == entity);

        public bool TryUseItem(string item, string entity)
        {
            var callback = _useItemCallbacks.Find(c => c.item == item && c.entity == entity);
            if (callback == null) return false;
            // A callback may remove itself, the item, the entity, or register a replacement with the same name.
            Invoke(callback.function, new object[] { callback.item, callback.entity });
            if (callback.autoDestroy) _useItemCallbacks.Remove(callback);
            return true;
        }

        /// <summary>cLuxUseItemCallback list of the map (map cache).</summary>
        public List<(string name, string item, string entity, string function, bool autoDestroy)> ExportUseItemCallbacks() =>
            _useItemCallbacks.ConvertAll(c => (c.name, c.item, c.entity, c.function, c.autoDestroy));

        public void ImportUseItemCallbacks(List<(string name, string item, string entity, string function, bool autoDestroy)> list)
        {
            _useItemCallbacks.Clear();
            foreach (var (name, item, entity, function, autoDestroy) in list)
                _useItemCallbacks.Add(new UseItemCallback { name = name, item = item, entity = entity, function = function, autoDestroy = autoDestroy });
        }

        protected void RemoveUseItemCallback(string asName)
        {
            int i = _useItemCallbacks.FindIndex(c => c.name == asName);
            if (i >= 0) _useItemCallbacks.RemoveAt(i);
            else HplLog.Warn($"Could not find use item callback '{asName}'!");
        }

        protected void AddCombineCallback(string asName, string asItemA, string asItemB, string asFunction, bool abAutoDestroy) =>
            HplGame.CombineCallbacks.Add(new HplGame.CombineCallback { name = asName, itemA = asItemA, itemB = asItemB, function = asFunction, autoDestroy = abAutoDestroy });

        protected void RemoveCombineCallback(string asName)
        {
            int i = HplGame.CombineCallbacks.FindIndex(c => c.name == asName);
            if (i >= 0) HplGame.CombineCallbacks.RemoveAt(i);
        }
    }
}
