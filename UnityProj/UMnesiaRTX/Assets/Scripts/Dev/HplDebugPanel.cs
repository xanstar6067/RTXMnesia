using System;
using System.Collections.Generic;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Simple IMGUI tool, owned by the regular UI pause so input and physics cannot leak into gameplay.</summary>
    public sealed class HplDebugPanel
    {
        public enum Page { Points, Inventory, State, Progress, Maps }

        static readonly string[] PageNames = { "Точки", "Инвентарь", "Состояние", "Прогресс", "Карты" };
        const int MaxRows = 150;

        // Kept over map loads of one run.
        static Page s_page = Page.Points;
        static int s_category;
        static string s_pointFilter = "", s_mapFilter = "";
        static bool s_closeAfterTeleport = true, s_pickUpWithEvents = true;

        readonly HplGameUi _ui;
        readonly HplMap _map;
        readonly List<HplDebugProgression.Stage> _stages;
        readonly HplDebugWorld _world;
        Vector2 _scroll, _secondScroll;
        string _snapshotName = "", _message = "";
        HplDebugProgression.Stage _selected;
        HplDebugSnapshots.Entry _confirmDelete;
        GUIStyle _wrap;
        GUISkin _skin;
        Texture2D _background;
        List<HplDebugWorld.Point> _points;
        List<HplDebugSnapshots.Entry> _snapshots;
        int _openedGeneration = -1;
        /// <summary>A changing action runs after the controls are drawn: IMGUI lists must not change mid pass.</summary>
        Action _pending;

        public HplDebugWorld World => _world;
        public Page CurrentPage { get => s_page; set { s_page = value; _scroll = _secondScroll = default; Refresh(); } }

        public HplDebugPanel(HplGameUi ui, HplMap map)
        {
            _ui = ui; _map = map; _stages = HplDebugProgression.AvailableStages();
            _selected = _stages.Find(s => s.MapName == map.MapName);
            _world = new HplDebugWorld(map);
        }

        bool InGame => !_ui.IsTitleScreen && _map.player != null;

        void Refresh() { _points = null; _snapshots = null; _confirmDelete = null; }

        public void Draw()
        {
            if (_ui.IsTransitioning) return;
            if (_openedGeneration != _ui.PauseGeneration)
            {
                _openedGeneration = _ui.PauseGeneration;
                _message = "";
                Refresh();
                _world.CollectItems();
            }
            // Opaque and with white text: the default skin is unreadable over a lit scene.
            GUISkin previousSkin = GUI.skin;
            if (_skin == null)
            {
                _skin = UnityEngine.Object.Instantiate(GUI.skin);
                _skin.hideFlags = HideFlags.HideAndDontSave;
                foreach (var style in new[] { _skin.label, _skin.button, _skin.toggle, _skin.textField })
                    foreach (var state in new[] { style.normal, style.hover, style.active, style.focused,
                                                  style.onNormal, style.onHover, style.onActive, style.onFocused })
                        state.textColor = Color.white;
                _skin.button.onNormal.textColor = _skin.button.onHover.textColor = new Color(1f, 0.85f, 0.35f);
                _wrap = new GUIStyle(_skin.label) { wordWrap = true };
                _background = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                _background.SetPixel(0, 0, new Color(0.07f, 0.07f, 0.08f, 0.94f));
                _background.Apply();
            }
            GUI.skin = _skin;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1000f, Screen.height / 720f), 0.5f, 2f);
            Matrix4x4 previous = GUI.matrix;
            int previousDepth = GUI.depth;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.depth = -100;
            var area = new Rect(16, 16, width - 32, height - 32);
            GUI.DrawTexture(area, _background);
            GUILayout.BeginArea(new Rect(area.x + 10, area.y + 8, area.width - 20, area.height - 16));
            Header();
            var page = (Page)GUILayout.Toolbar((int)s_page, PageNames, GUILayout.Height(28));
            if (page != s_page) _pending = () => CurrentPage = page;
            GUILayout.Space(4);
            switch (s_page)
            {
                case Page.Points: if (RequireGame()) DrawPoints(); break;
                case Page.Inventory: if (RequireGame()) DrawInventory(width - 52); break;
                case Page.State: DrawState(); break;
                case Page.Progress: if (RequireGame()) DrawProgress(); break;
                default: DrawMaps(); break;
            }
            GUILayout.FlexibleSpace();
            if (_message != "") GUILayout.Label(_message, _wrap);
            GUILayout.Label(HplDebugSession.Active
                ? "Тестовый сеанс: запись сохранений отключена; загрузка сохранения или новая игра завершает его."
                : "Первое перемещение или изменение начнёт тестовый сеанс без записи сохранений.", _wrap);
            GUILayout.EndArea();
            GUI.matrix = previous;
            GUI.depth = previousDepth;
            GUI.skin = previousSkin;

            var action = _pending;
            _pending = null;
            action?.Invoke();
        }

        void Header()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Панель отладки: " + _map.MapName + "    (~ / Ё или Esc: закрыть, мир на паузе)", GUILayout.ExpandWidth(true));
            HplDebugSession.DisableEnemyAI = GUILayout.Toggle(HplDebugSession.DisableEnemyAI, "AI противников выключен", GUILayout.ExpandWidth(false));
            if (GUILayout.Button("Пополнить фонарь / масло / трутницы", GUILayout.ExpandWidth(false)))
                _pending = () => { HplDebugSession.Supply(_map); _message = "Фонарь, масло и трутницы пополнены."; };
            if (_world.CanReturn && GUILayout.Button("Назад, где был", GUILayout.ExpandWidth(false)))
                _pending = () => { if (_world.Return()) AfterTeleport("Возврат на прежнее место."); };
            if (GUILayout.Button("Закрыть", GUILayout.ExpandWidth(false))) _pending = _ui.Resume;
            GUILayout.EndHorizontal();
        }

        bool RequireGame()
        {
            if (InGame) return true;
            GUILayout.Label("Эта вкладка работает на игровой карте: перейдите на неё во вкладке «Карты».");
            return false;
        }

        static bool Matches(string filter, string text) => filter == "" || text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        static string TextField(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            value = GUILayout.TextField(value);
            GUILayout.EndHorizontal();
            return value;
        }

        bool Button(string text, float width = 90) => GUILayout.Button(text, GUILayout.Width(width));

        void AfterTeleport(string message)
        {
            _message = message;
            _points = null;
            if (s_closeAfterTeleport) _ui.Resume();
        }

        void Teleport(HplDebugWorld.Point point)
        {
            if (_world.Teleport(point, out string message)) AfterTeleport(message);
            else _message = message;
        }

        void TeleportTo(string name)
        {
            _points ??= _world.Points();
            var point = _points.Find(p => p.Name == name);
            if (point != null) Teleport(point);
            else _message = "На карте сейчас нет объекта " + name + ".";
        }

        // ------------------------------------------------------------------ points

        void DrawPoints()
        {
            var category = GUILayout.Toolbar(s_category, HplDebugWorld.Categories);
            if (category != s_category) _pending = () => { s_category = category; _scroll = default; };
            string filter = TextField("Поиск:", s_pointFilter);
            if (filter != s_pointFilter) _pending = () => { s_pointFilter = filter; _scroll = default; };
            s_closeAfterTeleport = GUILayout.Toggle(s_closeAfterTeleport, "Закрывать панель после перемещения");
            _points ??= _world.Points();
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            int shown = 0, hidden = 0;
            foreach (var point in _points)
            {
                if (s_category != 0 && point.Category != HplDebugWorld.Categories[s_category]) continue;
                if (!Matches(s_pointFilter, point.Title + " " + point.Name)) continue;
                if (shown >= MaxRows) { hidden++; continue; }
                shown++;
                var entity = point.Entity;
                bool off = entity != null && !entity.Active;
                GUILayout.BeginHorizontal();
                GUI.contentColor = off ? Color.gray : Color.white;
                GUILayout.Label($"{point.Title}    {point.Name}    {point.Distance:0} м" + (off ? "    (выключен)" : ""), _wrap);
                GUI.contentColor = Color.white;
                if (entity is PropItem item && Button("Взять")) _pending = () => PickUp(item);
                if (entity is PropSwingDoor { Locked: var locked } && Button(locked ? "Отпереть" : "Запереть"))
                    _pending = () => { _world.SetLocked(entity, !locked); _points = null; };
                if (entity is PropLevelDoor { Locked: var closed } && Button(closed ? "Отпереть" : "Запереть"))
                    _pending = () => { _world.SetLocked(entity, !closed); _points = null; };
                if (entity != null && Button(off ? "Включить" : "Выключить"))
                    _pending = () => { _world.SetActive(entity, off); _points = null; };
                if (Button("К точке")) _pending = () => Teleport(point);
                GUILayout.EndHorizontal();
            }
            if (shown == 0) GUILayout.Label("Ничего не найдено.");
            if (hidden > 0) GUILayout.Label($"Ещё {hidden}: уточните поиск или категорию.");
            GUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ inventory

        void PickUp(PropItem item)
        {
            // Notes and diaries open the journal, which needs the panel closed.
            if (item.ItemType is "Note" or "Diary") _ui.Resume();
            string name = item.Name;
            _message = _world.PickUp(item) ? "Подобрано: " + name : "Предмет не подобрался (предел количества): " + name;
            _points = null;
        }

        void DrawInventory(float width)
        {
            s_pickUpWithEvents = GUILayout.Toggle(s_pickUpWithEvents,
                "Получать как при подборе: предмет исчезает с карты, срабатывают события уровня");
            float column = width / 2f - 16f;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(column));
            GUILayout.Label("Сейчас в инвентаре");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Трутницы x" + _map.player.tinderboxes, _wrap);
            if (Button("+1", 44)) _pending = () => _world.AddTinderboxes(1);
            if (Button("-1", 44)) _pending = () => _world.AddTinderboxes(-1);
            GUILayout.EndHorizontal();
            foreach (var item in HplGame.Items)
            {
                GUILayout.BeginHorizontal();
                bool counted = HplGame.HasCount(item.type) && item.count > 1;
                GUILayout.Label($"{_world.ItemTitle(item)}{(counted ? " x" + item.count : "")}    {item.name}", _wrap);
                if (counted && Button("-1", 44)) _pending = () => _world.Remove(item, false);
                if (Button("Удалить")) _pending = () => { _world.Remove(item, true); _message = "Удалено: " + item.name; };
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Очистить всё")) _pending = () => { _world.ClearInventory(); _message = "Инвентарь очищен."; };
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.Width(column));
            GUILayout.Label("Предметы этой карты");
            _secondScroll = GUILayout.BeginScrollView(_secondScroll, GUILayout.ExpandHeight(true));
            foreach (var item in _world.Items)
            {
                var prop = _world.Prop(item.Name);
                bool owned = HplGame.GetItem(item.Name) != null;
                string state = owned ? "в инвентаре" : prop != null ? (prop.Active ? item.Source : "на карте, выключен")
                    : item.Source == "лежит на карте" ? "уже взят" : item.Source;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{_world.ItemTitle(item)}    {item.Name}, {state}", _wrap);
                if (prop != null && Button("К точке")) _pending = () => TeleportTo(item.Name);
                if (!owned && Button("Получить")) _pending = () => Get(item);
                GUILayout.EndHorizontal();
            }
            if (_world.Items.Count == 0) GUILayout.Label("На этой карте нет предметов.");
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        void Get(HplDebugWorld.LevelItem item)
        {
            var prop = _world.Prop(item.Name);
            if (s_pickUpWithEvents && prop != null) PickUp(prop);
            else _message = _world.Give(item) ? "Получено без событий: " + item.Name : "Уже получено: " + item.Name;
        }

        // ------------------------------------------------------------------ snapshots

        void DrawState()
        {
            if (InGame)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Название:", GUILayout.Width(110));
                _snapshotName = GUILayout.TextField(_snapshotName);
                if (Button("Запомнить", 110)) _pending = () =>
                {
                    string file = HplDebugSnapshots.Create(_map, _snapshotName);
                    _message = file != null ? "Снимок записан: " + System.IO.Path.GetFileNameWithoutExtension(file) : "Снимок записать не удалось (см. лог).";
                    if (file != null) _snapshotName = "";
                    _snapshots = null;
                };
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Снимок хранит всё состояние игры, как сохранение, отдельно от сохранений профиля. " +
                            "После правок движка старый снимок может устареть.", _wrap);
            _snapshots ??= HplDebugSnapshots.List();
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var entry in _snapshots)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.Title}    {entry.Map}, {entry.Date}", _wrap);
                if (Button("Загрузить")) _pending = () =>
                {
                    if (!_ui.BeginDebugSnapshot(entry.File)) _message = "Снимок не загружается (см. лог).";
                };
                bool confirm = _confirmDelete == entry;
                if (Button(confirm ? "Точно?" : "Удалить")) _pending = () =>
                {
                    if (!confirm) { _confirmDelete = entry; return; }
                    HplDebugSnapshots.Delete(entry);
                    Refresh();
                };
                GUILayout.EndHorizontal();
            }
            if (_snapshots.Count == 0) GUILayout.Label("Снимков пока нет.");
            GUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ progress

        void DrawProgress()
        {
            var stages = HplDebugStages.ForMap(_map.MapName);
            if (stages.Count == 0)
            {
                GUILayout.Label("Для этой карты этапы не описаны (пока только 03_archives). Используйте точки, инвентарь и снимки.", _wrap);
                return;
            }
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < stages.Count; i++)
            {
                int index = i;
                var stage = stages[i];
                bool done = _map.Script != null && stage.Done(_map);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{i + 1}. {stage.Title}{(done ? "    (выполнен)" : "")}\n{stage.Note}", _wrap);
                if (!done && Button("Только этот", 110)) _pending = () => RunStage(index, false);
                if (!done && Button("Все до этого", 110)) _pending = () => RunStage(index, true);
                if (Button("К месту")) _pending = () => TeleportTo(stage.Place);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.Label("Отката нет: перезапустите карту во вкладке «Карты» или загрузите снимок.", _wrap);
        }

        void RunStage(int index, bool withPrevious)
        {
            HplDebugStages.Run(_world, _map, index, withPrevious);
            _points = null;
            _message = "Этап выполнен: " + HplDebugStages.ForMap(_map.MapName)[index].Title + ". Движение начнётся после закрытия панели.";
        }

        // ------------------------------------------------------------------ maps

        void DrawMaps()
        {
            string filter = TextField("Поиск карты:", s_mapFilter);
            if (filter != s_mapFilter) _pending = () => { s_mapFilter = filter; _scroll = default; };
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var stage in _stages)
            {
                if (!Matches(s_mapFilter, stage.Label + " " + stage.Variant)) continue;
                if (GUILayout.Toggle(_selected == stage, stage.Label + "    " + stage.Variant, GUI.skin.button)) _selected = stage;
            }
            GUILayout.EndScrollView();
            if (_selected == null) return;
            var names = _selected.Items.ConvertAll(i => i.Name);
            GUILayout.Label("С собой: фонарь, полный запас масла, 5 бутылок масла, 15 трутниц" +
                (names.Count > 0 ? "; " + string.Join(", ", names) : "") + ".", _wrap);
            GUILayout.Label("Переход начинает чистый тест карты. Предметы этой карты нужно найти на месте.", _wrap);
            if (GUILayout.Button("Телепорт: " + _selected.MapName + ", " + _selected.Variant, GUILayout.Height(32)))
            {
                var stage = _selected;
                _pending = () => { if (!_ui.BeginDebugLoad(stage)) _message = "Сцена недоступна в сборке: " + stage.ScenePath; };
            }
        }
    }
}
