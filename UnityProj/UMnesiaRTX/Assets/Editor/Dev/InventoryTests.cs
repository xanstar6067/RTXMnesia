using System;
using System.IO;
using System.Reflection;
using RTXMnesia.Dynamics;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>Inventory integration checks with real config, synthetic items and an occluded use target.</summary>
    public static class InventoryTests
    {
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        sealed class FixtureScript : HplScript
        {
            public int calls;
            public string receivedItem, receivedEntity;
            public void Disabled(bool value) => SetInventoryDisabled(value);
            public void Register(bool once) => AddUseItemCallback("use", "key", "target", nameof(Used), once);
            public void Replacing() => AddUseItemCallback("use", "key", "target", nameof(Replace), true);
            void Used(string item, string entity) { calls++; receivedItem = item; receivedEntity = entity; }
            void Replace(string item, string entity) { calls++; RemoveUseItemCallback("use"); Register(false); }
            public void FromFile(string name, string file) => GiveItemFromFile(name, file);
            public void Remove(string name) => RemoveItem(name);
        }

        [MenuItem("Tools/RTXMnesia/Tests/Inventory")]
        public static void Run()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run inventory checks outside Play mode.");
            float timeScale = Time.timeScale;
            var cursorLock = Cursor.lockState;
            bool cursorVisible = Cursor.visible;
            var previousMap = HplMap.Current;
            var root = new GameObject("Inventory checks") { hideFlags = HideFlags.HideAndDontSave };
            var bank = ScriptableObject.CreateInstance<HplSoundBank>();
            var clip = AudioClip.Create("inventory_test", 44100 * 10, 1, 44100, false);
            int checks = 0;
            void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
            static void Set(object obj, string property, object value) => obj.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(obj, value);
            HplGame.Reset();
            try
            {
                var map = root.AddComponent<HplMap>();
                SetStaticMap(map);
                map.gameConfig = AssetDatabase.LoadAssetAtPath<HplConfig>("Assets/AmnesiaImported/Config/game.asset");
                map.menuConfig = AssetDatabase.LoadAssetAtPath<HplConfig>("Assets/AmnesiaImported/Config/menu.asset");
                map.language = AssetDatabase.LoadAssetAtPath<HplLanguage>("Assets/AmnesiaImported/Lang/russian.asset");
                map.keysConfig = AssetDatabase.LoadAssetAtPath<HplConfig>("Assets/AmnesiaImported/Config/default_user_keys.asset");
                Check(map.gameConfig != null && map.menuConfig != null, "Missing imported configs");
                map.player = root.AddComponent<AmnesiaPlayer>();
                var player = map.player;
                player.gameConfig = map.gameConfig;
                player.readDevices = false;
                typeof(AmnesiaPlayer).GetMethod("Awake", PrivateInstance).Invoke(player, null);
                map.hud = root.AddComponent<HplHud>();
                var assets = Resources.Load<HplInventoryAssets>(HplInventoryAssets.ResourceName);
                map.hud.defaultFont = map.hud.hintsFont = map.hud.focusFont = assets.defaultFont;
                map.hud.Init(map);
                map.sound = root.AddComponent<HplSoundHandler>(); map.sound.bank = bank; map.sound.EnsureMusic();
                foreach (string name in new[] { "inventory_test", "12_make_drill", "15_make_hammer", "26_string_on_glass", "ui_use_health", "ui_use_sanity", "ui_use_oil", "ui_use_tinderbox" })
                {
                    bank.clips.Add(new HplSoundBank.Clip { name = name, path = name, clip = clip });
                    bank.soundEntities.Add(new HplSoundBank.SoundEntityData { name = name, path = name, main = new[] { name }, use3D = false, loop = false });
                }
                map.effects = root.AddComponent<HplEffects>(); map.effects.Init(map);
                var script = new FixtureScript(); Set(script, nameof(HplScript.Map), map); Set(map, nameof(HplMap.Script), script);
                var physics = new HplPhysicsWorld(); physics.SurfaceSounds.Sound = map.sound; Set(map, nameof(HplMap.PhysicsWorld), physics);
                var propObject = new GameObject("test prop"); propObject.transform.SetParent(root.transform);
                var propData = propObject.AddComponent<AmnesiaBody>(); propObject.AddComponent<BoxCollider>();
                var propBody = physics.AddEntity(propObject.transform, null, false)[0];
                var scrape = map.sound.CreateSoundEntity("scrape", "inventory_test", true); scrape.Play(); propBody.SurfaceState.ScrapeSound = scrape;
                var ui = root.AddComponent<HplGameUi>(); ui.Init(map); Set(map, nameof(HplMap.GameUi), ui);
                var inventory = ui.Inventory;
                HplGame.Item Add(string name, string type = "Puzzle", string subtype = null, float amount = 25) =>
                    HplGame.AddItem(map, name, type, subtype ?? name, "health.tga", amount, "", "", out _);

                Check(HplGame.CombineCallbacks.Count == 52, "Shared inventory script registrations incomplete");
                HplGame.EnsureInventoryScript(map);
                Check(HplGame.CombineCallbacks.Count == 52, "Shared inventory callbacks registered twice");
                Check(inventory.Assets != null && inventory.Assets.defaultFont != null && inventory.Assets.headerFont != null, "Missing inventory fonts / resource bank");
                Check(inventory.Assets.Texture("inventory_health_100.tga") != null && inventory.Assets.Texture("inventory_tinderboxes.tga") != null, "Missing inventory graphics");

                player.tinderboxes = 4;
                Add("tinder_a", "Tinderbox", "tinderbox"); Add("tinder_b", "Tinderbox", "tinderbox");
                Check(player.tinderboxes == 6 && HplGame.Items.Count == 0, "Tinder pickup no longer updates the existing player counter");
                player.active = false;
                Check(!ui.OpenInventory() && !ui.IsPaused, "Inventory opened during script input lock");
                player.active = true; script.Disabled(true);
                Check(!ui.OpenInventory(), "SetInventoryDisabled ignored"); script.Disabled(false);
                map.effects.SetPlayerIsPaused(true);
                Check(!ui.OpenInventory() && !ui.OpenJournal() && !ui.IsPaused, "Player menus opened while an effect pauses the player (emotion flash)");
                map.effects.SetPlayerIsPaused(false);
                map.effects.PlayVoice.AddVoice("inventory_test", "", "test voice", false, Vector3.zero, 0, 0);
                map.effects.PlayVoice.Update(0);
                var voice = (SoundEntry)typeof(HplEffects.PlayVoiceEffect).GetField("_voiceEntry", PrivateInstance).GetValue(map.effects.PlayVoice);
                Time.timeScale = 0.75f;
                Check(ui.OpenInventory() && ui.IsPaused && player.UiInputBlocked && Time.timeScale == 0, "Inventory did not freeze world / input");
                Check(scrape.IsFadingOut && propBody.SurfaceState.ScrapeSound == null, "Frozen prop kept its scrape loop in inventory");
                Check(!map.sound.MenuPaused && !map.sound.Music.IsPaused, "Inventory paused ambience / music like Esc menu");
                var ambient = map.sound.PlayGui("inventory_test", true, 1, SoundEntryType.World);
                Check(voice != null && voice.IsPaused, "Inventory did not pause the current voice effect");
                map.sound.StepMenu(0.5f); map.sound.StepMenu(0.5f); map.sound.StepMenu(0.01f);
                Check(ambient.ElapsedTime > 0.5f && voice.ElapsedTime == 0, "Inventory sound clocks / independent voice pause incorrect");
                Check(Mathf.Approximately(map.sound.GetGlobalVolume(SoundEntryType.World), 0.5f), "Inventory did not fade World volume to half");
                map.Step(1);
                Check(map.GameTime == 0, "Inventory advanced map logic");
                inventory.SetMessage("test", -1); ui.CloseInventory();
                Check(!inventory.MessageActive && ui.InventoryOpen, "Close must dismiss the popup first");
                inventory.SetMessage("test", 0.1f); inventory.Tick(20);
                Check(inventory.MessageActive, "Inventory popup auto-dismissed despite original's manual-dismiss behavior"); inventory.DismissMessage();
                ui.Resume();
                Check(Time.timeScale == 0.75f && !player.UiInputBlocked && player.tinderboxes == 6, "Closing inventory changed time scale / tinder count");
                Check(!voice.IsPaused, "Closing inventory did not resume the current voice");
                voice.SetPaused(true);
                typeof(SoundEntry).GetMethod("SetInventoryPaused", PrivateInstance).Invoke(voice, new object[] { true });
                typeof(SoundEntry).GetMethod("SetInventoryPaused", PrivateInstance).Invoke(voice, new object[] { false });
                Check(voice.IsPaused, "Removing inventory pause cleared explicit voice pause");

                var health = Add("health_a", "Health", "potion_health", 35);
                Check(ReferenceEquals(health, Add("health_b", "Health", "potion_health", 35)) && health.count == 2, "Potion pickups do not stack");
                health.count = 99;
                HplGame.AddItem(map, "excess", "Health", "potion_health", "potion_health.tga", 35, "", "", out bool removeExcess);
                Check(health.count == 99 && !removeExcess, "Full potion stack consumed a pickup"); health.count = 2;
                player.health = 100;
                Check(!inventory.Use(health) && health.count == 2, "Full health consumed a potion");
                player.health = 80;
                Check(inventory.Use(health) && player.health == 100 && health.count == 1, "Health potion did not cap / consume exactly one");
                player.health = 30; inventory.Use(health);
                Check(player.health == 65 && !HplGame.Items.Contains(health), "Last potion not removed");
                var sanity = Add("sanity", "Sanity", "potion_sanity", 30); player.sanity = 85;
                Check(inventory.Use(sanity) && player.sanity == 100 && !HplGame.Items.Contains(sanity), "Sanity potion failed");
                var oil = Add("oil", "LampOil", "oil", 40); player.lampOil = 80;
                Check(!inventory.Use(oil) && inventory.MessageActive && HplGame.Items.Contains(oil), "Oil used without lantern"); inventory.DismissMessage();
                var lantern = Add("lantern", "Lantern", "lantern");
                inventory.PointerDown(HplGame.Items.IndexOf(oil), Vector2.zero);
                inventory.PointerUp(HplGame.Items.IndexOf(lantern));
                Check(player.lampOil == 100 && !HplGame.Items.Contains(oil), "Dragging oil onto lantern failed");
                int beforeEmptyDrag = HplGame.Items.IndexOf(lantern);
                inventory.PointerDown(beforeEmptyDrag, Vector2.zero); inventory.PointerUp(17);
                Check(HplGame.Items.IndexOf(lantern) == beforeEmptyDrag && inventory.Picked == null, "Dragging to empty slot reordered / deleted an item");
                HplGame.Items.Clear();

                var chemA = Add("Chemical_1"); var chemB = Add("Chemical_2");
                Check(inventory.Combine(chemB, chemA) && inventory.MessageActive && HplGame.Items.Count == 2, "Chapter 1 chemicals should show a message, retaining both items");
                inventory.DismissMessage(); HplGame.Items.Clear();
                var part1 = Add("hand_drill_part01"); var part2 = Add("hand_drill_part02");
                Check(inventory.Combine(part1, part2) && HplGame.Items.Count == 2 && inventory.MessageActive, "Incomplete drill combined");
                inventory.DismissMessage(); Add("hand_drill_part03"); HplGame.SetGlobalVar("DrillParts", "3"); player.sanity = 60;
                Check(inventory.Combine(part2, part1) && HplGame.Items.Count == 1 && HplGame.GetItem("handdrill") != null && player.sanity == 70, "Drill recipe / canonical callback order failed");
                HplGame.Items.Clear();
                for (int i = 0; i < inventory.Capacity + 2; i++) Add("overflow" + i);
                Check(HplGame.Items.Count == inventory.Capacity + 2, "Items past the grid were refused (cLuxInventory::AddItem has no slot limit)");
                HplGame.Items.Clear();
                var hand1 = Add("hand1"); var hand2 = Add("hand2");
                Check(inventory.Combine(hand2, hand1) && HplGame.GetItem("key") != null && !HplGame.CombineCallbacks.Exists(c => c.name == "makekey"), "One-shot combination failed");

                var key = HplGame.GetItem("key");
                inventory.Use(key); map.hud.showCrossHair = false;
                typeof(HplHud).GetMethod("RefreshCrosshair", PrivateInstance).Invoke(map.hud, null);
                var cursor = (UnityEngine.UI.RawImage)typeof(HplHud).GetField("_crosshair", PrivateInstance).GetValue(map.hud);
                Check(cursor.enabled && cursor.texture == inventory.Icon(key), "Selected-item cursor disappeared with the ordinary crosshair setting disabled");
                player.States.ChangeState(PlayerStateType.Normal);
                var targetObject = new GameObject("target"); targetObject.transform.SetParent(root.transform); targetObject.transform.position = new Vector3(100, 0, 2);
                var areaComponent = targetObject.AddComponent<AmnesiaArea>(); areaComponent.areaType = "Script";
                targetObject.AddComponent<BoxCollider>().isTrigger = true;
                var target = new HplArea(areaComponent, map.gameConfig);
                typeof(HplMap).GetMethod("AddEntity", PrivateInstance).Invoke(map, new object[] { target });
                script.Register(true);
                Physics.SyncTransforms();
                Check(PlayerStateNormal.GetClosestEntity(new Vector3(100, 0, 0), Vector3.forward, 20, out _, out _, out var hit, "key") && hit == target, "Use-item ray cannot focus a registered trigger");
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(root.transform); wall.transform.position = new Vector3(100, 0, 1);
                Physics.SyncTransforms();
                Check(!PlayerStateNormal.GetClosestEntity(new Vector3(100, 0, 0), Vector3.forward, 20, out _, out _, out _, "key"), "Use-item ray reached target through a wall");
                UnityEngine.Object.DestroyImmediate(wall);
                Check(inventory.Use(key) && player.States.CurrentType == PlayerStateType.UseItem && HplGame.Items.Contains(key), "Selecting a puzzle consumed it / failed to enter UseItem");
                player.EntityInFocus = target; player.FocusDistance = 0.1f;
                player.States.Current.OnDoAction(PlayerAction.Interact, true);
                Check(script.calls == 1 && script.receivedItem == "key" && script.receivedEntity == "target" && !script.HasUseItemCallback("key", "target"), "One-shot use callback failed");
                Check(player.States.CurrentType == PlayerStateType.Normal && HplGame.Items.Contains(key), "Use did not return to normal or consumed an item without script instruction");
                script.Register(false); inventory.Use(key); player.EntityInFocus = target; player.FocusDistance = 100;
                player.States.Current.OnDoAction(PlayerAction.Interact, true);
                Check(script.calls == 1 && player.States.CurrentType == PlayerStateType.Normal, "Use callback ran outside interaction range");
                inventory.Use(key); player.EntityInFocus = target; player.FocusDistance = 0.1f;
                player.States.Current.OnDoAction(PlayerAction.Attack, true);
                Check(script.calls == 2, "Right mouse button did not attempt item use as in original");
                inventory.Use(key); Check(ui.OpenInventory() && player.States.CurrentType == PlayerStateType.Normal, "Opening inventory did not cancel use-item state"); ui.Resume();
                // An auto-destroy callback may replace itself under the same name; retain the replacement.
                var replacement = new FixtureScript(); Set(replacement, nameof(HplScript.Map), map); replacement.Replacing();
                Check(replacement.TryUseItem("key", "target") && replacement.HasUseItemCallback("key", "target"), "Auto-destroy removed a replacement callback");

                HplGame.Items.Clear();
                script.FromFile("potion", "potion_health.ent");
                var fromFile = HplGame.GetItem("potion");
                Check(fromFile != null && fromFile.type == "Health" && fromFile.amount > 0 && inventory.Icon(fromFile) != null, "GiveItemFromFile did not load item variables / image");
                Add("potion2", "Health", fromFile.subType, fromFile.amount); script.Remove("potion");
                Check(!HplGame.Items.Contains(fromFile), "Script RemoveItem must remove the entire named entry (unlike consumable use)");

                var lampObject = new GameObject("lamp"); lampObject.transform.SetParent(root.transform);
                var lamp = new PropLamp(lampObject.AddComponent<AmnesiaEntity>());
                Set(lamp, nameof(HplEntity.Map), map);
                typeof(HplProp).GetProperty("GameConfig", PrivateInstance).SetValue(lamp, map.gameConfig);
                lamp.SetLit(false, false);
                Check(lamp.GetFocusCrosshair(null, Vector3.zero) == FocusCrosshair.Ignite && lamp.GetFocusText() == "x 6", "Existing lamp crosshair / tinder counter changed");
                Check(lamp.OnInteract(null, Vector3.zero) && lamp.Lit && player.tinderboxes == 5, "Igniting a lamp must consume one existing tinderbox");
                lamp.OnInteract(null, Vector3.zero);
                Check(player.tinderboxes == 5, "A lit lamp consumed a second tinderbox");

                string report = $"PASS: {checks} inventory checks (pickups, stacks, consumables, audio, recipes, occlusion, use callbacks, tinder regression).";
                Directory.CreateDirectory("Logs/InventoryCheck"); File.WriteAllText("Logs/InventoryCheck/Tests.txt", report); Debug.Log(report);
            }
            finally
            {
                root.GetComponent<HplGameUi>()?.Resume();
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(bank); UnityEngine.Object.DestroyImmediate(clip);
                Time.timeScale = timeScale; Cursor.lockState = cursorLock; Cursor.visible = cursorVisible;
                HplGame.Reset(); SetStaticMap(previousMap);
            }
        }

        static void SetStaticMap(HplMap map) => typeof(HplMap).GetProperty(nameof(HplMap.Current)).SetValue(null, map);
    }
}
