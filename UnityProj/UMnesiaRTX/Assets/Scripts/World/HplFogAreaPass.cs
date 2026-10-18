using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.World
{
    /// <summary>
    /// The HPL2 fog areas (<see cref="AmnesiaFogArea"/>, cRendererDeferred::RenderFog): per camera the areas in view
    /// (cRenderList), sorted back to front by the view z of their centers, with the near plane test of
    /// iRenderer::CheckFogAreaInsideNearPlane, as shader globals (Assets/Shaders/HplFogAreas.hlsl), then one full
    /// screen draw of Hidden/RTXMnesia/HplFogArea. After the opaque lighting and the box lights, before sky and fog
    /// (HPL2: RenderFog, then RenderFullScreenFog, then the translucent objects which read the same globals).
    /// One global pass for all loaded scenes, created when the first area is enabled.
    /// </summary>
    [Serializable]
    public class HplFogAreaPass : CustomPass
    {
        public const string ShaderName = "Hidden/RTXMnesia/HplFogArea";
        const int MaxAreas = 16;

        static readonly List<AmnesiaFogArea> Areas = new();
        static CustomPassVolume _volume;
        static bool _warnedCount;

        static readonly int CountId = Shader.PropertyToID("_HplFogAreaCount");
        static readonly int WorldToBoxId = Shader.PropertyToID("_HplFogAreaWorldToBox");
        static readonly int ColorId = Shader.PropertyToID("_HplFogAreaColor");
        static readonly int ParamsId = Shader.PropertyToID("_HplFogAreaParams");
        static readonly int RadianceId = Shader.PropertyToID("_HplFogAreaRadiance");

        readonly Matrix4x4[] _worldToBox = new Matrix4x4[MaxAreas];
        readonly Vector4[] _colors = new Vector4[MaxAreas];
        readonly Vector4[] _params = new Vector4[MaxAreas];
        readonly Vector4[] _radiance = new Vector4[MaxAreas];
        readonly List<(float viewZ, AmnesiaFogArea area)> _visible = new();
        readonly Plane[] _frustum = new Plane[6];
        readonly Vector3[] _nearPlane = new Vector3[4];
        Material _material;

        public static void Register(AmnesiaFogArea area)
        {
            if (!Areas.Contains(area)) Areas.Add(area);
            if (_volume == null) CreateVolume();
        }

        public static void Unregister(AmnesiaFogArea area) => Areas.Remove(area);

        static void CreateVolume()
        {
            var go = new GameObject("HPL Fog Areas") { hideFlags = HideFlags.HideAndDontSave };
            if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
            _volume = go.AddComponent<CustomPassVolume>();
            _volume.isGlobal = true;
            _volume.injectionPoint = CustomPassInjectionPoint.AfterOpaqueColor;
            // Higher priorities run first: after the box lights (priority 0), like the HPL2 fog after the lights.
            _volume.priority = -1f;
            _volume.AddPassOfType(typeof(HplFogAreaPass)).name = "HPL fog areas";
        }

        protected override void Cleanup() => CoreUtils.Destroy(_material);

        protected override void Execute(CustomPassContext ctx)
        {
            var cmd = ctx.cmd;
            var camera = ctx.hdCamera.camera;
            GeometryUtility.CalculateFrustumPlanes(camera, _frustum);
            var view = camera.worldToCameraMatrix;
            _visible.Clear();
            foreach (var area in Areas)
            {
                if (area == null || !area.isActiveAndEnabled) continue;
                var box = new Bounds(area.transform.position, Vector3.zero);
                var toWorld = area.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                    box.Encapsulate(toWorld.MultiplyPoint3x4(new Vector3((c & 1) - 0.5f, ((c >> 1) & 1) - 0.5f, (c >> 2) - 0.5f)));
                if (!GeometryUtility.TestPlanesAABB(_frustum, box)) continue;
                _visible.Add((view.MultiplyPoint3x4(area.transform.position).z, area));
            }
            // SortFunc_FogAreaData: view z ascending (both look down -z): the farthest first.
            _visible.Sort((a, b) => a.viewZ.CompareTo(b.viewZ));

            int count = Mathf.Min(_visible.Count, MaxAreas);
            if (_visible.Count > MaxAreas && !_warnedCount)
            {
                Debug.LogWarning($"[RTXMnesia] More than {MaxAreas} fog areas in view, the nearest are skipped");
                _warnedCount = true;
            }
            float near = camera.nearClipPlane;
            for (int i = 0; i < 4; i++) _nearPlane[i] = camera.ViewportToWorldPoint(new Vector3(i & 1, i >> 1, near));
            Shader shader = null;
            for (int i = 0; i < count; i++)
            {
                var area = _visible[i].area;
                var worldToBox = area.transform.worldToLocalMatrix;
                bool inside = InsideNearPlane(worldToBox);
                bool backside = inside ? area.showBacksideWhenInside : area.showBacksideWhenOutside;
                _worldToBox[i] = worldToBox;
                _colors[i] = area.color;
                _params[i] = new Vector4(area.start, area.end - area.start, area.falloffExp, (inside ? 1 : 0) + (backside ? 2 : 0));
                Color radiance = Hpl.HplLighting.Radiance(area.color);
                _radiance[i] = new Vector4(radiance.r, radiance.g, radiance.b, 1f);
                if (shader == null) shader = area.shader;
            }
            // The translucent shaders of this camera read the same globals: also when no area is in view.
            cmd.SetGlobalInt(CountId, count);
            if (count == 0) return;
            cmd.SetGlobalMatrixArray(WorldToBoxId, _worldToBox);
            cmd.SetGlobalVectorArray(ColorId, _colors);
            cmd.SetGlobalVectorArray(ParamsId, _params);
            cmd.SetGlobalVectorArray(RadianceId, _radiance);

            if (_material == null)
            {
                if (shader == null) shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    Debug.LogError("[RTXMnesia] Shader " + ShaderName + " not found: fog areas are off");
                    return;
                }
                _material = CoreUtils.CreateEngineMaterial(shader);
            }
            CoreUtils.SetRenderTarget(cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(cmd, _material, shaderPassId: 0);
        }

        /// <summary>
        /// iRenderer::CheckFogAreaInsideNearPlane: a corner of the near plane inside the unit box, or the near plane
        /// not outside any single plane of it (cMath::CheckPointsAABBPlanesCollision, conservative).
        /// </summary>
        bool InsideNearPlane(Matrix4x4 worldToBox)
        {
            Span<Vector3> points = stackalloc Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                points[i] = worldToBox.MultiplyPoint3x4(_nearPlane[i]);
                if (Mathf.Abs(points[i].x) <= 0.5f && Mathf.Abs(points[i].y) <= 0.5f && Mathf.Abs(points[i].z) <= 0.5f) return true;
            }
            for (int axis = 0; axis < 3; axis++)
            {
                bool allAbove = true, allBelow = true;
                for (int i = 0; i < 4; i++)
                {
                    if (points[i][axis] <= 0.5f) allAbove = false;
                    if (points[i][axis] >= -0.5f) allBelow = false;
                }
                if (allAbove || allBelow) return false;
            }
            return true;
        }
    }
}
