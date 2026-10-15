using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_Item (LuxProp_Item.cpp): picked up on interaction into the inventory (<see cref="HplGame.AddItem"/>),
    /// "Picked up X" message, OnPickup callback. The flash when the player is near (outline effect) comes with the
    /// effect renderer.
    /// </summary>
    public sealed class PropItem : HplProp
    {
        string _itemType = "", _imageFile = "", _pickSound = "Generic", _subItemTypeName = "", _val = "", _extraVal = "";
        float _amount = 1f;

        public PropItem(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override string DefaultFocusDistanceKey => "Item_DefaultMaxFocusDist";

        protected override void LoadVariables()
        {
            _itemType = HplGame.ItemType(SubType);
            _imageFile = VarString("ImageFile", "");
            _pickSound = VarString("PickSound", "Generic");
            _subItemTypeName = VarString("SubItemTypeName", "");
            switch (_itemType)
            {
                case "Coins": _amount = VarFloat("CoinAmount", 0f); break;
                case "Health":
                case "Sanity": _amount = VarFloat("HealAmount", 0f); break;
                case "LampOil": _amount = VarFloat("OilAmount", 0f); break;
            }
        }

        protected override void LoadInstanceVariables()
        {
            string custom = InstanceString("CustomSubItemTypeName", "");
            if (custom != "") _subItemTypeName = custom;
            if (_itemType == "Note")
            {
                _val = InstanceString("NoteText", "");
                _amount = InstanceBool("OpenNoteInJournal", true) ? 1f : -1f;
                _amount *= InstanceBool("UseNarration", false) ? 2f : 1f;
            }
            else if (_itemType == "Diary")
            {
                _val = InstanceString("DiaryText", "");
                _extraVal = InstanceString("DiaryCallback", "");
            }
        }

        public override bool CanInteract(HplPhysicsBody body) => !DestroyMe;

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            HplGame.AddItem(Map, Name, _itemType, _subItemTypeName, _imageFile, _amount, _val, _extraVal, out bool removeProp);
            if (removeProp)
            {
                Map.DestroyEntity(this);
                Map.sound.PlayGuiSoundData("ui/pick_" + _pickSound, RTXMnesia.Sound.SoundEntryType.World);
                if (HplGame.ShowPickUpMessage(_itemType))
                {
                    var hud = Map.hud;
                    hud.Messages.SetMessage(hud.Translate("Inventory", "PickedUp") + " " + hud.Translate("Inventory", "ItemName_" + _subItemTypeName), 0f);
                }
                RunCallbackFunc("OnPickup");
            }
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) => FocusCrosshair.Pick;
    }

    /// <summary>
    /// cLuxProp_LevelDoor (LuxProp_LevelDoor.cpp): a door to another map. Locked: the locked sound and text,
    /// at most every 2 s; else the map changes (map switching comes later: logged).
    /// </summary>
    public sealed class PropLevelDoor : HplProp
    {
        string _textEntry = "", _mapFile = "", _startPos = "", _lockedTextCat = "", _lockedTextEntry = "";
        float _lockedCount;

        public bool Locked;
        public string LockedSound = "";

        public PropLevelDoor(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override string DefaultFocusDistanceKey => "LevelDoor_DefaultMaxFocusDist";

        protected override void LoadInstanceVariables()
        {
            _textEntry = InstanceString("TextEntry", "");
            _mapFile = InstanceString("MapFile", "");
            _startPos = InstanceString("StartPos", "");
            Locked = InstanceBool("Locked", false);
            LockedSound = InstanceString("LockedSound", "");
            _lockedTextCat = InstanceString("LockedTextCat", "");
            _lockedTextEntry = InstanceString("LockedTextEntry", "");
        }

        public void SetLockedText(string cat, string entry)
        {
            _lockedTextCat = cat;
            _lockedTextEntry = entry;
        }

        public override bool CanInteract(HplPhysicsBody body) => true;

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (Locked)
            {
                if (_lockedCount <= 0f)
                {
                    Map.sound.PlayGuiSoundData(LockedSound, RTXMnesia.Sound.SoundEntryType.World);
                    if (_lockedTextCat != "" && _lockedTextEntry != "")
                        Map.hud.Messages.SetMessage(Map.hud.Translate(_lockedTextCat, _lockedTextEntry), 0f);
                    _lockedCount = 2f;
                }
            }
            else Map.ChangeMap(_mapFile, _startPos);
            return true;
        }

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_lockedCount > 0f) _lockedCount -= timeStep;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) => FocusCrosshair.LevelDoor;

        public override string GetFocusText() => Map.hud.Translate("Levels", _textEntry) + "\n";
    }
}
