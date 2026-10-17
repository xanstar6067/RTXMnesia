using System;
using System.Collections.Generic;
using RTXMnesia.Effects;
using RTXMnesia.Enemies;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Dev
{
    /// <summary>Animation workbench. The generated scene owns its specimens; no gameplay state is saved.</summary>
    public sealed class HplDevRoom : MonoBehaviour
    {
        [Serializable]
        public sealed class Station
        {
            public string entityName, sourceFile, category;
            public Vector3 position;
            [NonSerialized] public HplEntity entity;
            [NonSerialized] public HplMeshAnimation animation;
            [NonSerialized] public bool physics;
        }

        public List<Station> stations = new();
        public int SelectedIndex { get; private set; }
        public bool PanelOpen { get; private set; }
        HplMap _map;
        Vector2 _scroll, _clipScroll;
        bool _loop = true, _paused;
        float _speed = 1f;
        string _filter = "";
        GUIStyle _label;

        public Station Selected => stations.Count > 0 ? stations[SelectedIndex] : null;

        public void Initialize(HplMap map)
        {
            _map = map;
            HplInsanity.StopCurrentEvent();
            foreach (var station in stations)
            {
                station.entity = map.GetEntityByName(station.entityName);
                if (station.entity is HplEnemy enemy)
                {
                    enemy.SetDisabled(true);
                    enemy.SetDisableTriggers(true);
                    enemy.SetSanityDecreaseActive(false);
                    station.animation = enemy.Animation;
                }
                else if (station.entity is HplProp prop)
                {
                    station.animation = prop.Animation;
                    // Keep ragdolls and animated props on their stands; joint samples remain interactive.
                    station.physics = station.category == "Механизмы";
                    if (!station.physics) prop.SetStaticPhysics(true);
                }
                if (station.animation != null && station.animation.States.Count > 0)
                    station.animation.Play(0, true, true);
            }
        }

        // NPCs and critters normally replace clips / move themselves. Only their poses run in the workbench.
        public bool PreviewOnly(HplEntity entity) => entity is PropNPC || entity is PropCritterBase;

        public void Select(int index)
        {
            SelectedIndex = Mathf.Clamp(index, 0, stations.Count - 1);
            _paused = false;
            _speed = 1f;
            _loop = true;
        }

        public void PlayClip(int index)
        {
            var animation = Selected?.animation;
            if (animation == null || index < 0 || index >= animation.States.Count) return;
            animation.Play(index, _loop, true);
            animation.States[index].Paused = _paused;
            animation.States[index].Speed = _speed;
            animation.Update(0f, null);
        }

        public HplAnimationState CurrentClip
        {
            get
            {
                if (Selected?.animation == null) return null;
                foreach (var state in Selected.animation.States) if (state.IsActive) return state;
                return null;
            }
        }

        void Update()
        {
            if (_map == null || _map.IsMenuPaused) return;
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) SetPanelOpen(!PanelOpen);
            if (PanelOpen && _map.player != null && !_map.player.UiInputBlocked) SetPanelOpen(true);
        }

        public void SetPanelOpen(bool open)
        {
            PanelOpen = open;
            if (_map?.player == null) return;
            _map.player.SetUiInputBlocked(open);
            AmnesiaPlayer.LockCursor(!open);
        }

        void OnDisable()
        {
            if (PanelOpen && _map != null && _map.player != null) SetPanelOpen(false);
        }

        public void GoToSelected()
        {
            if (Selected == null || _map?.player == null) return;
            _map.player.PlaceAt(Selected.position + new Vector3(0f, 0.06f, -3f), 0f);
        }

        void OnGUI()
        {
            if (_map == null || _map.IsMenuPaused) return;
            float scale = Mathf.Clamp(Screen.height / 900f, 0.65f, 1.5f);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.Box(new Rect(12, 12, 580, 48), "dev_room — F3: анимации / курсор · F2: свободная камера\nАвтосохранения, безумие и ИИ врагов отключены");
            if (!PanelOpen) DrawLabels(scale);
            else DrawPanel(width, height);
            GUI.matrix = previous;
        }

        void DrawLabels(float scale)
        {
            var camera = _map.PlayerCamera;
            if (camera == null) return;
            _label ??= new GUIStyle(GUI.skin.box) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            foreach (var station in stations)
            {
                if (Vector3.Distance(camera.transform.position, station.position) > 22f) continue;
                var p = camera.WorldToScreenPoint(station.position + Vector3.up * 2.8f);
                if (p.z <= 0f) continue;
                GUI.Label(new Rect(p.x / scale - 120, (Screen.height - p.y) / scale, 240, 38),
                    station.entityName + "\n" + station.category, _label);
            }
        }

        void DrawPanel(float width, float height)
        {
            GUILayout.BeginArea(new Rect(Mathf.Max(12, width - 445), 72, 430, height - 88), GUI.skin.box);
            GUILayout.Label($"Образцы: {stations.Count} · F3: вернуться к осмотру");
            _filter = GUILayout.TextField(_filter);
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Mathf.Min(230, height * 0.3f)));
            for (int i = 0; i < stations.Count; i++)
            {
                var station = stations[i];
                if (_filter.Length > 0 && station.sourceFile.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (GUILayout.Button((SelectedIndex == i ? "► " : "") + station.entityName)) Select(i);
            }
            GUILayout.EndScrollView();
            var selected = Selected;
            if (selected != null)
            {
                GUILayout.Label(selected.category);
                GUILayout.Label(selected.sourceFile, GUI.skin.box);
                if (GUILayout.Button("Подойти к стенду")) GoToSelected();
                var animation = selected.animation;
                if (animation != null && animation.States.Count > 0)
                {
                    GUILayout.BeginHorizontal();
                    _loop = GUILayout.Toggle(_loop, "Цикл");
                    _paused = GUILayout.Toggle(_paused, "Пауза");
                    if (GUILayout.Button("Сначала")) PlayClip(Mathf.Max(0, animation.IndexOf(CurrentClip?.Name)));
                    GUILayout.EndHorizontal();
                    GUILayout.Label($"Скорость: {_speed:0.00}× (поверх скорости .ent)");
                    _speed = GUILayout.HorizontalSlider(_speed, 0.1f, 2f);
                    var current = CurrentClip;
                    if (current != null)
                    {
                        current.Loop = _loop;
                        current.Paused = _paused;
                        current.Speed = _speed;
                        GUILayout.Label($"{current.Name} · {current.TimePosition:0.00} / {current.Length:0.00} с");
                        float time = GUILayout.HorizontalSlider(current.TimePosition, 0f, current.Length);
                        if (Mathf.Abs(time - current.TimePosition) > 0.0001f)
                        {
                            current.SetTimePosition(time);
                            animation.Update(0f, null);
                        }
                        if (GUILayout.Button("Шаг 1/60 с"))
                        {
                            _paused = true;
                            current.Paused = true;
                            current.SetTimePosition(current.TimePosition + _speed * current.BaseSpeed / 60f);
                            animation.Update(0f, null);
                        }
                    }
                    _clipScroll = GUILayout.BeginScrollView(_clipScroll, GUILayout.Height(Mathf.Min(220, height * 0.2f)));
                    for (int i = 0; i < animation.States.Count; i++)
                        if (GUILayout.Button(animation.States[i].Name)) PlayClip(i);
                    GUILayout.EndScrollView();
                }
                else GUILayout.Label("Клипов в ресурсах нет: скелет / физика / процедурное движение.");
                if (selected.entity is PropMoveObject moving)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Открыть механизм")) moving.MoveToState(1f);
                    if (GUILayout.Button("Закрыть механизм")) moving.MoveToState(0f);
                    GUILayout.EndHorizontal();
                }
                if (selected.entity is HplProp prop && prop.PhysicsBodies.Count > 0 && selected.entity is not PropCritterBase)
                {
                    bool physics = GUILayout.Toggle(selected.physics, "Физика объекта");
                    if (physics != selected.physics)
                    {
                        selected.physics = physics;
                        prop.SetStaticPhysics(!physics);
                    }
                }
            }
            GUILayout.EndArea();
        }
    }
}
