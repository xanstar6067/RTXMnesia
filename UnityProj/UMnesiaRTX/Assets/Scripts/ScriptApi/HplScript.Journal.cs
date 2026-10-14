using System.Collections.Generic;

namespace RTXMnesia.ScriptApi
{
    // Messages, quests, notes, hints and the inventory (LuxScriptHandler.cpp → cLuxMessageHandler, cLuxJournal,
    // cLuxHintHandler, cLuxInventory). Journal and inventory screens come with step 6; their data is kept now.
    public abstract partial class HplScript
    {
        protected void SetMessage(string asTextCategory, string asTextEntry, float afTime) =>
            Hud.Messages.SetMessage(Translate(asTextCategory, asTextEntry), afTime);

        protected void SetDeathHint(string asTextCategory, string asTextEntry) { }

        protected void DisableDeathStartSound() { }

        protected void AddNote(string asNameAndTextEntry, string asImage) => NotPorted(nameof(AddNote), "the journal (step 6)");

        protected void AddDiary(string asNameAndTextEntry, string asImage) => NotPorted(nameof(AddDiary), "the journal (step 6)");

        protected void ReturnOpenJournal(bool abOpenJournal) { }

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
            if (HplGame.DisableQuest(asName)) ProgLog("High", "Completed Quest " + asName);
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

        public bool InventoryDisabled { get; private set; }

        protected void ExitInventory() { }

        protected void SetInventoryDisabled(bool abX) => InventoryDisabled = abX;

        protected void SetInventoryMessage(string asTextCategory, string asTextEntry, float afTime) =>
            NotPorted(nameof(SetInventoryMessage), "the inventory screen (step 6)");

        protected void GiveItem(string asName, string asType, string asSubTypeName, string asImageName, float afAmount)
        {
            if (HplGame.GetItem(asName) != null) return;
            HplGame.Items.Add(new HplGame.Item { name = asName, type = asType, subType = asSubTypeName, image = asImageName, amount = afAmount });
        }

        /// <summary>The item data of the .ent (type, image, amount) comes with the inventory (step 6); HasItem works now.</summary>
        protected void GiveItemFromFile(string asName, string asFileName)
        {
            if (HplGame.GetItem(asName) == null) HplGame.Items.Add(new HplGame.Item { name = asName, type = asFileName });
            NotPorted(nameof(GiveItemFromFile) + " item data", "the inventory (step 6)");
        }

        protected void RemoveItem(string asName) => HplGame.Items.RemoveAll(i => i.name == asName);

        protected bool HasItem(string asName) => HplGame.GetItem(asName) != null;

        readonly List<(string name, string item, string entity, string function, bool autoDestroy)> _useItemCallbacks = new();

        /// <summary>Kept until items can be used on entities (inventory, step 6).</summary>
        protected void AddUseItemCallback(string asName, string asItem, string asEntity, string asFunction, bool abAutoDestroy) =>
            _useItemCallbacks.Add((asName, asItem, asEntity, asFunction, abAutoDestroy));

        protected void RemoveUseItemCallback(string asName)
        {
            int i = _useItemCallbacks.FindIndex(c => c.name == asName);
            if (i >= 0) _useItemCallbacks.RemoveAt(i);
            else HplLog.Warn($"Could not find use item callback '{asName}'!");
        }

        protected void AddCombineCallback(string asName, string asItemA, string asItemB, string asFunction, bool abAutoDestroy) =>
            NotPorted(nameof(AddCombineCallback), "the inventory (step 6)");

        protected void RemoveCombineCallback(string asName) { }
    }
}
