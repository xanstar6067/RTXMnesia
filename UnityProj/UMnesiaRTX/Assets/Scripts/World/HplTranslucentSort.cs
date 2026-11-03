using System.Collections.Generic;
using RTXMnesia.Particles;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// The large plane of the translucent sorting (cRenderList::FindNearestLargeSurfacePlane, SortFunc_Translucent):
    /// for each camera the nearest large flat surface facing it is found, and translucent objects are drawn first
    /// below its plane, then the surface, then above it; within each group back to front as before. HDRP sorts by
    /// <see cref="Renderer.rendererPriority"/> before the distance, so the group is the priority: −1, 0, 1.
    /// Without it, ripples on a puddle (ps_impact_water_stream at 00_rainy_hall) came and went: the bounds centres of
    /// the puddle and of the ripples are a few centimetres apart in distance, the order flipped as particles were
    /// born, and the puddle's Mul (or water's opaque blend) covered the ripples drawn before it.
    /// Large surfaces: HPL2 marks only water materials with LargeSurface (8 of 18). Here every water surface and
    /// every flat translucent Mul surface (puddles: pool_water) counts, so both keep what lies on them; a surface is
    /// flat when its world bounds are under 2 cm thick and over 25 cm wide, its normal is up (HPL2's one-sided normal; the meshes are
    /// static batched, their triangles are not at hand).
    /// </summary>
    public sealed class HplTranslucentSort : MonoBehaviour
    {
        const string WaterShader = "RTXMnesia/HplWater", MultiplyShader = "RTXMnesia/HplMultiply";
        const float MaxThickness = 0.02f, MinWidth = 0.25f;

        readonly List<Renderer> _surfaces = new();
        readonly List<Renderer> _translucent = new();
        readonly List<Renderer> _particles = new();

        public IReadOnlyList<Renderer> Surfaces => _surfaces;
        /// <summary>The surface the last camera was sorted by (null: none in front of it).</summary>
        public Renderer ActiveSurface { get; private set; }

        /// <summary>The sorting for the translucent ones among <paramref name="renderers"/> (under <paramref name="parent"/>).</summary>
        public static HplTranslucentSort Create(IEnumerable<Renderer> renderers, Transform parent)
        {
            var go = new GameObject("HplTranslucentSort (runtime)") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            var sort = go.AddComponent<HplTranslucentSort>();
            foreach (var r in renderers)
            {
                bool translucent = false, surface = false;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null || m.renderQueue <= (int)RenderQueue.GeometryLast) continue;
                    translucent = true;
                    // Mul decals (queue Transparent − 100) are drawn before all translucent objects anyway.
                    if (m.shader != null && (m.shader.name == WaterShader ||
                                             m.shader.name == MultiplyShader && m.renderQueue >= (int)RenderQueue.Transparent))
                        surface = true;
                }
                if (!translucent) continue;
                sort._translucent.Add(r);
                var size = r.bounds.size;
                if (surface && size.y < MaxThickness && Mathf.Min(size.x, size.z) > MinWidth) sort._surfaces.Add(r);
            }
            return sort;
        }

        void OnEnable() => RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            ActiveSurface = null;
            Apply(null, 0f);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;
            // The nearest surface the camera is above (facing it: the normal is up).
            Vector3 eye = camera.transform.position;
            Renderer nearest = null;
            float nearestDistance = 0f;
            foreach (var s in _surfaces)
            {
                if (s == null || !s.enabled || !s.gameObject.activeInHierarchy) continue;
                float distance = eye.y - s.bounds.center.y;
                if (distance <= 0f) continue;
                if (nearest == null || distance < nearestDistance)
                {
                    nearest = s;
                    nearestDistance = distance;
                }
            }
            ActiveSurface = nearest;
            Apply(nearest, nearest != null ? nearest.bounds.center.y : 0f);
        }

        void Apply(Renderer plane, float height)
        {
            foreach (var r in _translucent) Place(r, plane, height);
            HplParticleSystem.RendererRoot.GetComponentsInChildren(_particles);
            foreach (var r in _particles) Place(r, plane, height);
        }

        static void Place(Renderer r, Renderer plane, float height)
        {
            if (r == null) return;
            int priority = plane == null || r == plane ? 0 : r.bounds.center.y < height ? -1 : 1;
            if (r.rendererPriority != priority) r.rendererPriority = priority;
        }
    }
}
