using System;
using System.Collections.Generic;
using RTXMnesia.Enemies;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// What the debug panel knows about the loaded map: points of interest, the items met on this level and the
    /// actions on them. Created with the UI, before any script or save changes the map, so picked up items stay listed.
    /// Every changing action turns the run into a test session (no saves).
    /// </summary>
    public sealed class HplDebugWorld
    {
        public static readonly string[] Categories =
            { "Все", "Записки", "Предметы", "Двери", "Механизмы", "Мебель", "Враги", "Входы", "Прочее" };

        public sealed class Point
        {
            public string Category, Title, Name;
            public HplEntity Entity;
            public AmnesiaArea Start;
            public Transform Static;
            public float Distance;
        }

        public sealed class LevelItem
        {
            public string Name, Type, SubType = "", Image = "", Val = "", File = "", Source = "";
            public float Amount = 1f;
        }

        readonly HplMap _map;
        readonly List<LevelItem> _items = new();
        /// <summary>Diary parts share their text entry: the ones given here are told apart by prop name.</summary>
        readonly HashSet<string> _givenDiaries = new();
        (Vector3 feet, float yaw, float pitch)? _back;

        public IReadOnlyList<LevelItem> Items => _items;
        public bool CanReturn => _back.HasValue;

        public HplDebugWorld(HplMap map)
        {
            _map = map;
            CollectItems();
            AddScriptItems();
        }

        string Translate(string category, string entry, string fallback)
        {
            if (_map.hud == null || string.IsNullOrEmpty(entry)) return fallback;
            string text = _map.hud.Translate(category, entry);
            return string.IsNullOrWhiteSpace(text) || text.Contains(entry) ? fallback : text;
        }

        // ------------------------------------------------------------------ items

        /// <summary>New item props (spawned from broken containers) join the list; known ones stay.</summary>
        public void CollectItems()
        {
            foreach (var entity in _map.Entities)
            {
                if (entity is not PropItem item || _items.Exists(i => i.Name == item.Name)) continue;
                _items.Add(new LevelItem
                {
                    Name = item.Name, Type = item.ItemType, SubType = item.SubItemTypeName, Image = item.ImageFile,
                    Val = item.NoteText, Amount = item.Amount, File = item.Component.file, Source = "лежит на карте",
                });
            }
        }

        /// <summary>Items this level gets without a prop (recipes, script gifts) and the equipment of its entry preset.</summary>
        void AddScriptItems()
        {
            void Made(string name, string image, string source)
            {
                if (_items.Exists(i => i.Name == name)) return;
                _items.Add(new LevelItem { Name = name, Type = "Puzzle", SubType = name == "handdrill" ? "hand_drill" : name,
                    Image = image, Amount = 0f, Source = source });
            }
            bool Has(string name) => _items.Exists(i => i.Name == name);
            if (Has("hand_drill_part01") || Has("hand_drill_part02") || Has("hand_drill_part03"))
                Made("handdrill", "hand_drill.tga", "собирается из частей");
            if (Has("stone_chipper_1") || Has("stone_hammer_1")) Made("stone_hammer_chipper", "stone_hammer_chipper.tga", "соединяется");
            if (Has("copper_tube_1")) Made("copper_tube_and_needle", "copper_tube_and_needle.tga", "соединяется");
            if (Has("string_pile_2")) Made("container_and_string", "glass_container_string.tga", "соединяется");
            string map = _map.MapName;
            if (map is "23_torture_transept" or "26_torture_nave_redux") Made("glass_container_blood", "glass_container_blood.tga", "выдаётся скриптом");

            void FromFile(string name, string file, string source)
            {
                if (!_items.Exists(i => i.Name == name)) _items.Add(new LevelItem { Name = name, File = file + ".ent", Source = source });
            }
            if (map is "02_entrance_hall" or "06_distillery") FromFile("empty_container", "chemical_container", "выдаётся скриптом");
            if (map is "15_prison_north") FromFile("glass_container_filled", "glass_container_filled", "выдаётся скриптом");
            if (map is "21_torture_nave" or "26_torture_nave_redux") FromFile("fresh_meat_remains_1", "fresh_meat_remains", "выдаётся скриптом");
            if (map is "25_cell_tunnels") FromFile("wooden_bucket_filled_1", "wooden_bucket_filled", "выдаётся скриптом");

            if (string.IsNullOrEmpty(_map.gameObject.scene.path)) return;
            foreach (var item in HplDebugProgression.Create(_map.gameObject.scene.path).Items)
                FromFile(item.Name, System.IO.Path.GetFileNameWithoutExtension(item.File), "берётся с прошлых карт");
        }

        public string ItemTitle(LevelItem item)
        {
            if (item.Type == "Note") return Translate("Journal", "Note_" + item.Val + "_Name", "Записка");
            if (item.Type == "Diary") return Translate("Journal", "Diary_" + item.Val + "_Name1", "Дневник");
            string sub = item.SubType != "" ? item.SubType : System.IO.Path.GetFileNameWithoutExtension(item.File);
            return Translate("Inventory", "ItemName_" + sub, item.Name);
        }

        public string ItemTitle(HplGame.Item item) => Translate("Inventory", "ItemName_" + item.subType, item.name);

        /// <summary>The prop of a listed item while it is still on the map.</summary>
        public PropItem Prop(string name) =>
            _map.GetEntityByName(name) is PropItem prop && prop.Name == name && !_map.IsDestroyPending(prop) ? prop : null;

        /// <summary>
        /// "Как при подборе": the prop's own interaction with every callback. Notes and diaries open the journal, so the
        /// caller closes the panel first.
        /// </summary>
        public bool PickUp(PropItem prop)
        {
            if (prop == null) return false;
            HplDebugSession.MarkTest(_map);
            prop.OnInteract(null, default);
            prop.RunInteractCallback();
            bool picked = _map.IsDestroyPending(prop);
            _map.DestroyPendingEntitiesNow();
            return picked;
        }

        /// <summary>Into the inventory only: no hints, callbacks or completion points, the prop stays on the map.</summary>
        public bool Give(LevelItem item)
        {
            HplDebugSession.MarkTest(_map);
            if (item.Type is "Note" or "Diary")
            {
                if (item.Type == "Note" ? HplGame.JournalEntries.Exists(e => e.type == item.Type && e.textEntry == item.Val)
                                        : !_givenDiaries.Add(item.Name)) return false;
                HplGame.AddJournalEntry(item.Type, item.Val, "graphics/item/" + item.Image);
                return true;
            }
            if (item.Type == "Tinderbox")
            {
                if (_map.player != null) _map.player.tinderboxes++;
                return _map.player != null;
            }
            if (item.Type == "Coins")
            {
                HplGame.Coins += (int)item.Amount;
                return true;
            }
            if (item.Type != null && HplGame.HasCount(item.Type))
            {
                var stack = HplGame.Items.Find(i => i.subType == item.SubType);
                if (stack != null)
                {
                    stack.count = Mathf.Min(99, stack.count + 1);
                    return true;
                }
            }
            if (HplGame.GetItem(item.Name) != null) return false;
            if (item.Type == null) return HplDebugSession.Give(_map, item.Name, item.File);
            string image = item.Image ?? "";
            HplGame.Items.Add(new HplGame.Item
            {
                name = item.Name, type = item.Type, subType = item.SubType, amount = item.Amount, val = item.Val,
                image = image.StartsWith("graphics/", StringComparison.OrdinalIgnoreCase) ? image : "graphics/item/" + image,
            });
            return true;
        }

        /// <summary>One of a counted stack, or the whole stack / item.</summary>
        public void Remove(HplGame.Item item, bool all)
        {
            HplDebugSession.MarkTest(_map);
            if (all) HplGame.Items.Remove(item);
            else HplGame.RemoveItem(item);
            AfterRemoval();
        }

        public void ClearInventory()
        {
            HplDebugSession.MarkTest(_map);
            HplGame.Items.Clear();
            if (_map.player != null) _map.player.tinderboxes = 0;
            AfterRemoval();
        }

        public void AddTinderboxes(int amount)
        {
            if (_map.player == null) return;
            HplDebugSession.MarkTest(_map);
            _map.player.tinderboxes = Mathf.Max(0, _map.player.tinderboxes + amount);
        }

        void AfterRemoval()
        {
            var player = _map.player;
            if (player == null) return;
            if (player.lanternActive && !HplGame.HasType("Lantern")) player.Lantern.SetActive(false, false, false, false);
            if (player.States?.Current is PlayerStateUseItem use && !HplGame.Items.Contains(use.Item))
                player.States.ChangeState(PlayerStateType.Normal);
        }

        // ------------------------------------------------------------------ points of interest

        static readonly (string key, string title)[] Furniture =
        {
            ("table", "Стол"), ("desk", "Стол"), ("workbench", "Верстак"), ("bookshelf", "Книжный шкаф"),
            ("bookcase", "Книжный шкаф"), ("shelf", "Полка"), ("cabinet", "Шкаф"), ("cupboard", "Шкаф"),
            ("wardrobe", "Шкаф"), ("drawer", "Комод"), ("dresser", "Комод"), ("chest", "Сундук"), ("piano", "Пианино"),
            ("chair", "Стул"), ("stool", "Табурет"), ("bed_", "Кровать"),
        };

        static string FurnitureTitle(string name)
        {
            string lower = name.ToLowerInvariant();
            // Small things named after the furniture they lie on.
            if (lower.StartsWith("deskpad")) return null;
            foreach (var (key, title) in Furniture)
                if (lower.Contains(key)) return title;
            return null;
        }

        /// <summary>All points, nearest first.</summary>
        public List<Point> Points()
        {
            var list = new List<Point>();
            foreach (var entity in _map.Entities)
            {
                if (_map.IsDestroyPending(entity)) continue;
                var point = new Point { Entity = entity, Name = entity.Name, Category = "Прочее" };
                switch (entity)
                {
                    case PropItem item:
                        bool note = item.ItemType is "Note" or "Diary";
                        point.Category = note ? "Записки" : "Предметы";
                        var known = _items.Find(i => i.Name == item.Name);
                        point.Title = known != null ? ItemTitle(known) : "Предмет";
                        break;
                    case PropSwingDoor door:
                        point.Category = "Двери";
                        point.Title = door.IsBroken ? "Дверь, выломана" : door.Locked ? "Дверь, заперта" : "Дверь";
                        break;
                    case PropLevelDoor level:
                        point.Category = "Двери";
                        point.Title = "Переход в " + HplMapChange.FileToMapName(level.MapFile) + (level.Locked ? ", заперт" : "");
                        break;
                    case PropLever: point.Category = "Механизмы"; point.Title = "Рычаг"; break;
                    case PropWheel: point.Category = "Механизмы"; point.Title = "Колесо"; break;
                    case PropMultiSlider: point.Category = "Механизмы"; point.Title = "Переключатель"; break;
                    case PropMoveObject: point.Category = "Механизмы"; point.Title = "Подвижный объект"; break;
                    case HplEnemy: point.Category = "Враги"; point.Title = "Противник"; break;
                    case HplArea area:
                        if (area.AreaType != "Script" && area.AreaType != "Flashback" && area.AreaType != "Ladder") continue;
                        point.Title = area.AreaType == "Ladder" ? "Лестница" : "Область скрипта";
                        break;
                    case HplProp prop:
                        string furniture = FurnitureTitle(prop.Name);
                        point.Category = furniture != null ? "Мебель" : "Прочее";
                        point.Title = furniture ?? "Объект";
                        break;
                    default: point.Title = "Объект"; break;
                }
                list.Add(point);
            }
            var statics = _map.transform.Find("StaticObjects");
            if (statics != null)
                foreach (Transform child in statics)
                {
                    string title = FurnitureTitle(child.name);
                    if (title != null) list.Add(new Point { Category = "Мебель", Title = title, Name = child.name, Static = child });
                }
            foreach (var area in _map.PlayerStarts)
                list.Add(new Point { Category = "Входы", Title = "Точка входа", Name = area.name, Start = area });

            Vector3 from = _map.player != null ? _map.player.Body.FeetPosition : Vector3.zero;
            foreach (var point in list) point.Distance = Vector3.Distance(from, Bounds(point).center);
            list.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            return list;
        }

        public static Bounds Bounds(Point point)
        {
            if (point.Start != null) return new Bounds(point.Start.transform.position, Vector3.zero);
            var entity = point.Entity;
            if (entity is HplEnemy enemy && enemy.Body != null) return new Bounds(enemy.Body.Position, Vector3.one);
            if (entity != null)
            {
                Physics.SyncTransforms();
                Bounds bounds = default;
                bool any = false;
                for (int i = 0; i < entity.BodyCount && entity.Active; i++)
                    if (HplCollision.TryGetBounds(entity.GetBodyColliders(i), out var b))
                    {
                        if (any) bounds.Encapsulate(b);
                        else bounds = b;
                        any = true;
                    }
                if (any) return bounds;
            }
            Transform root = entity != null ? entity.AttachTransform : point.Static;
            var renderers = (entity != null ? entity.GameObject.transform : point.Static).GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
            var result = renderers[0].bounds;
            foreach (var r in renderers) result.Encapsulate(r.bounds);
            return result;
        }

        /// <summary>Moves the player next to the point; the message says what happened.</summary>
        public bool Teleport(Point point, out string message)
        {
            var player = _map.player;
            if (player == null) { message = "На этой карте нет игрока."; return false; }
            var previous = (player.Body.FeetPosition, player.Yaw, player.Pitch);
            if (point.Start != null)
            {
                HplDebugSession.MarkTest(_map);
                HplDebugTeleport.PlaceAtStart(player, point.Start);
                message = "Перемещение к " + point.Name;
            }
            else
            {
                var bounds = Bounds(point);
                Transform root = point.Entity != null ? point.Entity.GameObject.transform : point.Static;
                var result = HplDebugTeleport.Find(_map, bounds, root, out var feet);
                if (result == HplDebugTeleport.Result.Failed)
                {
                    message = "Рядом с " + point.Name + " не нашлось свободного места.";
                    return false;
                }
                HplDebugSession.MarkTest(_map);
                HplDebugTeleport.Place(player, feet, bounds.center);
                message = "Перемещение к " + point.Name +
                          (result == HplDebugTeleport.Result.Hidden ? " (объект закрыт или за преградой)" : "") +
                          (point.Entity != null && !point.Entity.Active ? " (объект сейчас выключен)" : "");
            }
            _back = previous;
            return true;
        }

        public bool Return()
        {
            if (!_back.HasValue || _map.player == null) return false;
            var (feet, yaw, pitch) = _back.Value;
            var current = (_map.player.Body.FeetPosition, _map.player.Yaw, _map.player.Pitch);
            _map.player.DebugPlace(feet, yaw, pitch);
            _back = current;
            return true;
        }

        public void SetActive(HplEntity entity, bool active)
        {
            HplDebugSession.MarkTest(_map);
            entity.SetActive(active);
        }

        public void SetLocked(HplEntity entity, bool locked)
        {
            HplDebugSession.MarkTest(_map);
            if (entity is PropSwingDoor door) door.SetLocked(locked, false);
            else if (entity is PropLevelDoor level) level.Locked = locked;
        }
    }
}
