using System;
using System.Collections.Generic;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Simple IMGUI tool, owned by the regular UI pause so input and physics cannot leak into gameplay.</summary>
    public sealed class HplDebugPanel
    {
        readonly HplGameUi _ui;
        readonly HplMap _map;
        readonly List<HplDebugProgression.Stage> _stages;
        Vector2 _scroll;
        string _filter = "", _error = "";
        HplDebugProgression.Stage _selected;
        GUIStyle _wrap;

        public HplDebugPanel(HplGameUi ui, HplMap map)
        {
            _ui = ui; _map = map; _stages = HplDebugProgression.AvailableStages();
            _selected = _stages.Find(s => s.MapName == map.MapName);
        }

        public void Draw()
        {
            if (_ui.IsTransitioning) return;
            _wrap ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1000f, Screen.height / 720f), 0.5f, 2f);
            Matrix4x4 previous = GUI.matrix;
            int previousDepth = GUI.depth;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.depth = -100;
            GUILayout.BeginArea(new Rect(16, 16, width - 32, height - 32), GUI.skin.window);
            GUILayout.Label("Панель отладки — " + _map.MapName);
            GUILayout.Label("~ / Ё или Esc — закрыть. Мир на паузе.");
            HplDebugSession.DisableEnemyAI = GUILayout.Toggle(HplDebugSession.DisableEnemyAI, "Отключить AI противников (включая водяного монстра)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Поиск карты:", GUILayout.Width(100));
            _filter = GUILayout.TextField(_filter);
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var stage in _stages)
            {
                if (_filter.Length > 0 && (stage.Label + " " + stage.Variant).IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Toggle(_selected == stage, stage.Label + "  —  " + stage.Variant, GUI.skin.button)) _selected = stage;
            }
            GUILayout.EndScrollView();
            if (_selected != null)
            {
                var names = _selected.Items.ConvertAll(i => i.Name);
                GUILayout.Label("С собой: фонарь, полный запас масла, 5 бутылок масла, 15 трутниц" +
                    (names.Count > 0 ? "; " + string.Join(", ", names) : "") + ".", _wrap);
                GUILayout.Label("Переход начинает чистый тест карты. Предметы этой карты нужно найти на месте. " +
                    "В тестовом сеансе запись сохранений отключена; загрузка сохранения или новая игра завершает тест.", _wrap);
                if (GUILayout.Button("Телепорт: " + _selected.MapName + " — " + _selected.Variant, GUILayout.Height(32)))
                {
                    if (!_ui.BeginDebugLoad(_selected)) _error = "Сцена недоступна в сборке: " + _selected.ScenePath;
                }
            }
            if (_error != "") GUILayout.Label(_error, _wrap);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Пополнить фонарь / масло / трутницы")) HplDebugSession.Supply(_map);
            if (GUILayout.Button("Закрыть")) _ui.Resume();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.matrix = previous;
            GUI.depth = previousDepth;
        }
    }
}
