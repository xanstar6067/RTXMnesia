using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Ephemeral test run. Ordinary doors preserve it; new game / load / title reset it.</summary>
    public static class HplDebugSession
    {
        public static bool Active { get; private set; }
        public static bool DisableEnemyAI { get; set; }
        public static bool EnemiesPaused => DisableEnemyAI && HplMap.Current?.GameUi?.Settings?.debugPanel == true;
        static HplDebugProgression.Stage s_entry;

        public static void Reset() { Active = DisableEnemyAI = false; s_entry = null; }

        public static void Start(HplDebugProgression.Stage stage, bool disableEnemyAI)
        {
            Active = true; DisableEnemyAI = disableEnemyAI; s_entry = stage;
            HplDifficulty.HardMode = false;
        }

        public static void Prepare(HplMap map)
        {
            if (Active) map.disableAutoSaves = true;
            if (s_entry == null || s_entry.MapName != map.MapName) return;
            if (!string.IsNullOrEmpty(s_entry.StartArea) && map.player != null) map.player.startArea = s_entry.StartArea;
            foreach (var v in s_entry.Globals) HplGame.SetGlobalVar(v.Key, v.Value);
            foreach (var item in s_entry.Items) Give(map, item.Name, item.File);
            Supply(map);
        }

        public static void CompleteEntry(HplMap map)
        {
            if (s_entry == null || s_entry.MapName != map.MapName) return;
            Supply(map);
            s_entry = null;
        }

        // Direct insertion deliberately avoids pickup hints, completion points and collect callbacks.
        public static bool Give(HplMap map, string name, string file)
        {
            if (HplGame.GetItem(name) != null) return true;
            if (file == "stone_hammer_chipper.ent")
            {
                HplGame.Items.Add(new HplGame.Item { name = name, type = "Puzzle", subType = "stone_hammer_chipper",
                    image = "graphics/item/stone_hammer_chipper.tga", amount = 0 });
                return true;
            }
            var bank = Resources.Load<HplInventoryAssets>(map.Campaign.ResourceName(HplInventoryAssets.ResourceName));
            var definition = bank != null ? bank.Item(file) : null;
            // Justine uses the same lamp / oil resources as the main campaign.
            if (definition == null) definition = Resources.Load<HplInventoryAssets>(HplInventoryAssets.ResourceName)?.Item(file);
            if (definition == null)
            {
                Debug.LogError("[DebugPanel] Item definition unavailable: " + name + " / " + file);
                return false;
            }
            string image = definition.image ?? "";
            HplGame.Items.Add(new HplGame.Item {
                name = name, type = definition.type, subType = definition.subType,
                image = image.StartsWith("graphics/") ? image : "graphics/item/" + image, amount = definition.amount,
            });
            return true;
        }

        public static void Supply(HplMap map)
        {
            if (map.player == null) return;
            if (!HplGame.HasType("Lantern")) Give(map, "debug_lantern", "lantern.ent");
            var oil = HplGame.Items.Find(i => i.type == "LampOil");
            if (oil == null && Give(map, "debug_oil", "potion_oil.ent")) oil = HplGame.GetItem("debug_oil");
            if (oil != null) oil.count = Mathf.Max(5, oil.count);
            map.player.tinderboxes = Mathf.Max(15, map.player.tinderboxes);
            map.player.SetLampOil(100);
        }
    }
}
