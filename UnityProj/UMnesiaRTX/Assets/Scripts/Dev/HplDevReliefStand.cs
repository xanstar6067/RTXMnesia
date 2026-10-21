using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RTXMnesia.ScriptApi;

namespace RTXMnesia.Dev
{
    /// <summary>Labels and an inspection-light switch for the generated dev_room comparison stand.</summary>
    public sealed class HplDevReliefStand : MonoBehaviour
    {
        [Serializable] public struct Label
        {
            public Transform anchor;
            public string text;
        }
        public Label[] labels = Array.Empty<Label>();
        public Behaviour[] inspectionLights = Array.Empty<Behaviour>();
        public MeshFilter[] flat = Array.Empty<MeshFilter>(), relief = Array.Empty<MeshFilter>();
        public MeshFilter[] masonry = Array.Empty<MeshFilter>();
        public bool InspectionEnabled { get; private set; } = true;
        HplMap _map;
        GUIStyle _style;
        void Start() => _map = GetComponentInParent<HplMap>();
        void Update()
        {
            if (_map == null || _map.IsMenuPaused || (_map.GetComponent<HplDevRoom>()?.PanelOpen ?? false)) return;
            if (Keyboard.current?.f4Key.wasPressedThisFrame == true) SetInspection(!InspectionEnabled);
        }
        public void SetInspection(bool enabled)
        {
            InspectionEnabled = enabled;
            foreach (var light in inspectionLights) if (light != null) light.enabled = enabled;
            if (UnityEngine.Rendering.RenderPipelineManager.currentPipeline is UnityEngine.Rendering.HighDefinition.HDRenderPipeline hdrp)
                hdrp.ResetPathTracing();
        }
        void OnGUI()
        {
            if (_map == null || _map.IsMenuPaused || (_map.GetComponent<HplDevRoom>()?.PanelOpen ?? false)) return;
            var camera = _map.PlayerCamera;
            if (camera == null || Vector3.Distance(camera.transform.position, new Vector3(0, 1.5f, 3)) > 19) return;
            float scale = Mathf.Clamp(Screen.height / 900f, 0.65f, 1.5f);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            _style ??= new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            foreach (var label in labels)
            {
                if (label.anchor == null) continue;
                var point = camera.WorldToScreenPoint(label.anchor.position);
                if (point.z > 0) GUI.Label(new Rect(point.x / scale - 135, (Screen.height - point.y) / scale - 26, 270, 52), label.text, _style);
            }
            GUI.Box(new Rect(12, Screen.height / scale - 116, 850, 104),
                "СТЕНД РЕЛЬЕФА · улучшенные текстуры · POM выключен\n" +
                "Кладка слева направо: 1 — плоско · 2 — прежний рельеф · 3 — углублённые швы\n" +
                "Ряд выше: 4 — новая кладка по маске, сетка 1 см · 5 — она же, сетка 5 см (как в игре)\n" +
                "За полами: 6 / 7 — новый каменный пол, сетка 1 / 5 см · 8 — новые доски с глубокими щелями\n" +
                "F4: " + (InspectionEnabled ? "выключить проверочный свет, оставить свой фонарь" : "включить проверочный свет"));
            GUI.matrix = previous;
        }
        void OnDisable() { if (!InspectionEnabled) SetInspection(true); }
    }
}
