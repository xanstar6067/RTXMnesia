using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Ui
{
    /// <summary>cLuxInventory and item type actions. The item list / combine callbacks belong to HplGame.</summary>
    public sealed class HplInventory
    {
        public const int Health = -2, Sanity = -3, Oil = -4, Tinderboxes = -5, Journal = -6;
        public readonly HplInventoryAssets Assets;
        public readonly HplInventoryView View;
        public int Selected { get; private set; } = Health;
        public HplGame.Item Picked { get; private set; }
        public Vector2 PickOffset { get; private set; }
        public string Message { get; private set; } = "";
        public bool MessageActive => Message != "";
        public int Columns => _map.menuConfig != null ? _map.menuConfig.GetInt("Inventory", "SlotsColumns", 6) : 6;
        public int Rows => _map.menuConfig != null ? _map.menuConfig.GetInt("Inventory", "SlotsRows", 3) : 3;
        public int Capacity => Columns * Rows;
        readonly HplMap _map;
        double _lastClick = -1;
        HplGame.Item _lastClicked;

        public HplInventory(HplMap map)
        {
            _map = map;
            Assets = Resources.Load<HplInventoryAssets>(map.Campaign.ResourceName(HplInventoryAssets.ResourceName));
            HplGame.EnsureInventoryScript(map);
            View = new HplInventoryView(map, this);
        }
        public Texture2D Icon(HplGame.Item item) => item != null && Assets != null ? Assets.Texture(item.image) : null;
        public HplGame.Item ItemAt(int index) => index >= 0 && index < HplGame.Items.Count ? HplGame.Items[index] : null;

        public void Open()
        {
            Picked = null;
            _lastClicked = null;
            DismissMessage();
            if (_map.player?.States?.CurrentType == PlayerStateType.UseItem) _map.player.States.ChangeState(PlayerStateType.Normal);
            View.SetVisible(true);
        }
        public void Close() { Picked = null; View.SetVisible(false); }
        public void Select(int index) => Selected = index;
        public void Navigate(int x, int y)
        {
            if (Selected < 0) Selected = 0;
            else Selected = (Selected + Capacity + x + y * Columns) % Capacity;
        }
        public void Tick(float dt)
        {
            if (Picked != null && !HplGame.Items.Contains(Picked)) Picked = null;
            if (_map.effects != null && _map.effects.SanityGainFlash.Active) _map.effects.SanityGainFlash.Update(dt);
        }
        public void SetMessage(string text, float time)
        {
            Message = text ?? "";
            // Original Update leaves mfMessageTextLife unchanged: every popup waits for dismissal,
            // even when the script passes a positive lifetime. Keep the API parameter for script parity.
            Picked = null;
        }
        public void DismissMessage() => Message = "";
        public void PointerDown(int index, Vector2 position)
        {
            if (MessageActive) { DismissMessage(); return; }
            if (index == Journal) { _map.GameUi?.OpenJournal(fromInventory: true); return; }
            var item = ItemAt(index);
            if (item == null) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (_lastClicked == item && now - _lastClick <= 0.3)
            {
                Picked = null;
                _lastClicked = null;
                Use(item);
                return;
            }
            _lastClick = now;
            _lastClicked = item;
            Picked = item;
            var texture = Icon(item);
            Vector2 size = texture != null ? new Vector2(texture.width, texture.height) : Vector2.one * 64;
            PickOffset = View.SlotRect(index).center - size * 0.5f - position;
        }
        public void PointerUp(int index)
        {
            var picked = Picked;
            Picked = null;
            if (picked == null || !HplGame.Items.Contains(picked)) return;
            var target = ItemAt(index);
            if (target != null && target != picked) Combine(target, picked);
            else if (index == Oil && picked.type == "LampOil") Use(picked);
        }
        public void UseSelected()
        {
            if (MessageActive) DismissMessage();
            else if (Selected == Journal) _map.GameUi?.OpenJournal(fromInventory: true);
            else Use(ItemAt(Selected));
        }
        public bool Use(HplGame.Item item)
        {
            if (item == null || !HplGame.Items.Contains(item) || _map.player == null) return false;
            var player = _map.player;
            string sound;
            switch (item.type)
            {
                case "Health":
                    if (player.health >= 100f) return false;
                    player.SetHealth(Mathf.Min(100f, player.health + item.amount));
                    sound = "ui_use_health";
                    break;
                case "Sanity":
                    if (player.sanity >= 100f) return false;
                    player.AddSanity(item.amount);
                    sound = "ui_use_sanity";
                    break;
                case "LampOil":
                    if (!HplGame.HasType("Lantern")) { SetMessage(T("OilNeedsLantern"), 0); return false; }
                    if (player.lampOil >= 100f) return false;
                    player.SetLampOil(Mathf.Min(100f, player.lampOil + item.amount));
                    sound = "ui_use_oil";
                    break;
                case "Lantern":
                    // cLuxItemType_Lantern::OnUse
                    player.Lantern.SetActive(!player.lanternActive, true);
                    return true;
                case "Puzzle":
                    if (player.States == null) return false;
                    PlayerStateVars.InventoryItem = item;
                    player.States.ChangeState(PlayerStateType.UseItem);
                    _map.GameUi?.CloseInventory();
                    return true;
                default: return false;
            }
            HplGame.RemoveItem(item);
            _map.sound?.PlayGuiSoundData(sound, SoundEntryType.Gui);
            return true;
        }
        public bool Combine(HplGame.Item a, HplGame.Item b)
        {
            if (a == null || b == null || a == b || !HplGame.Items.Contains(a) || !HplGame.Items.Contains(b)) return false;
            if (a.type == "Lantern" && b.type == "LampOil") return Use(b);
            if (b.type == "Lantern" && a.type == "LampOil") return Use(a);
            var callback = HplGame.CombineCallbacks.Find(c => (c.itemA == a.name && c.itemB == b.name) || (c.itemA == b.name && c.itemB == a.name));
            if (callback == null) { SetMessage(T("CombinationDoesNotWork"), 0); return false; }
            HplGame.EnsureInventoryScript(_map);
            HplGame.InventoryScript.Invoke(callback.function, new object[] { callback.itemA, callback.itemB });
            if (callback.autoDestroy) HplGame.CombineCallbacks.Remove(callback);
            return true;
        }

        public string T(string entry) => _map.hud.Translate("Inventory", entry);
        public string ItemName(HplGame.Item item) => item == null ? "" : T("ItemName_" + item.subType) +
            (item.type == "Lantern" ? " (" + T(_map.player.lanternActive ? "LanternOn" : "LanternOff") + ")" : "");
        public static int Status(float value) => value < 25 ? 0 : value < 50 ? 1 : value < 75 ? 2 : 3;
        public (string header, string text) Description()
        {
            var item = ItemAt(Selected);
            if (item != null)
            {
                if (Picked != null && Picked != item) return ("", T("Combine") + " " + ItemName(Picked) + " " + T("with") + " " + ItemName(item) + ".");
                return (ItemName(item), _map.hud.ParseString(T("ItemDesc_" + item.subType)));
            }
            return Selected switch {
                Health => (T("Health"), T("HealthDesc" + Status(_map.player.health))),
                Sanity => (T("Sanity"), T("SanityDesc" + Status(_map.player.sanity))),
                Oil => (T("LampOil"), T("LampOilDesc")),
                Tinderboxes => (T("Tinderboxes"), T("TinderboxesDesc")),
                Journal => (T("Journal"), T("JournalDesc")),
                _ => ("", ""),
            };
        }
    }
}
