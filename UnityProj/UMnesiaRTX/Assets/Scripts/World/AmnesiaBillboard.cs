using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// HPL2 billboard (Billboard.cpp): a quad in the local XY plane.
    /// FixedAxis billboards are plain mesh renderers on this object (scale = size).
    /// Point (faces the camera) and Axis (turns around <see cref="axis"/> towards the camera) billboards are drawn
    /// by a hidden, unsaved child that is re-oriented before every camera renders, also in the editor.
    /// Halos (IsHalo) use Assets/Shaders/HplHalo.shader: the glow (usually without depth test) fades by the visible part
    /// of its source box, as HPL2 does with occlusion queries.
    /// </summary>
    [ExecuteAlways]
    public class AmnesiaBillboard : MonoBehaviour
    {
        public enum Kind { Point, Axis, FixedAxis }

        public Kind kind = Kind.Point;
        [Tooltip("Rotation axis of Axis billboards, in local space.")]
        public Vector3 axis = Vector3.up;
        [Tooltip("Moves the quad towards the camera by this distance (Point/Axis).")]
        public float forwardOffset;
        public Color color = Color.white;
        public bool isHalo;
        public Vector3 haloSourceSize = Vector3.one;
        [Tooltip("Name of the light whose color this billboard follows.")]
        public string connectLight;

        [Header("Point / Axis drawing")]
        public Vector2 size = Vector2.one;
        public Mesh mesh;
        public Material material;
        public bool visible = true;

        Transform _quad;

        void OnEnable()
        {
            // A billboard is a fake glow: never in the ray tracing acceleration structure (its color changes with the
            // light every step, which also made HDRP's path tracer start its accumulation again).
            if (TryGetComponent(out Renderer own)) own.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            if (kind == Kind.FixedAxis || mesh == null) return;
            var go = new GameObject(name + " (camera facing)") { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var quadRenderer = go.AddComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = material;
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            quadRenderer.enabled = visible;
            _quad = go.transform;
            if (_runtimeColor) ApplyColor();
            RenderPipelineManager.beginCameraRendering += FaceCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= FaceCamera;
            if (_quad == null) return;
            if (Application.isPlaying) Destroy(_quad.gameObject);
            else DestroyImmediate(_quad.gameObject);
            _quad = null;
        }

        bool _runtimeColor;
        MaterialPropertyBlock _block;

        /// <summary>
        /// cBillboard::SetColor at runtime (prop effects fading): overrides the color the material variant has, as a
        /// property block. Black hides the billboard (Billboard.cpp IsVisible).
        /// </summary>
        public void SetColor(Color c)
        {
            color = c;
            _runtimeColor = true;
            ApplyColor();
        }

        public const string HaloShader = "RTXMnesia/HplHalo";
        static readonly int HplColorId = Shader.PropertyToID("_HplColor");
        static readonly int HaloBoxId = Shader.PropertyToID("_HplHaloBox");
        static readonly int HaloActiveId = Shader.PropertyToID("_HplHaloActive");

        void ApplyColor()
        {
            if (material == null) return;
            _block ??= new MaterialPropertyBlock();
            if (material.shader.name == HaloShader)
            {
                _block.SetVector(HplColorId, color);
            }
            else if (material.shader.name == "HDRP/Unlit")
            {
                // Additive HPL2 blending ignores alpha; HDRP multiplies additive color by it (as the import does).
                bool additive = Mathf.Approximately(material.GetFloat("_BlendMode"), 1f);
                _block.SetColor("_UnlitColor", new Color(color.r, color.g, color.b, additive ? 1f : color.a));
            }
            else _block.SetColor("_BaseColor", color);

            bool show = visible && (color.r > 0f || color.g > 0f || color.b > 0f);
            foreach (var r in new[] { GetComponent<Renderer>(), _quad != null ? _quad.GetComponent<Renderer>() : null })
            {
                if (r == null) continue;
                r.SetPropertyBlock(_block);
                r.enabled = show;
            }
        }

        void FaceCamera(ScriptableRenderContext context, Camera cam)
        {
            if (_quad == null || cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;

            Vector3 toCamera = cam.transform.position - transform.position;
            if (toCamera.sqrMagnitude < 1e-8f) return;
            toCamera.Normalize();

            if (kind == Kind.Point)
            {
                _quad.rotation = Quaternion.LookRotation(toCamera, cam.transform.up);
            }
            else
            {
                Vector3 up = transform.rotation * axis.normalized;
                Vector3 forward = Vector3.ProjectOnPlane(toCamera, up);
                if (forward.sqrMagnitude > 1e-8f) _quad.rotation = Quaternion.LookRotation(forward, up);
            }
            _quad.position = transform.position + toCamera * forwardOffset;
            if (isHalo) UpdateHaloSource();
        }

        /// <summary>
        /// The halo's source box (HaloSourceSize at the billboard's matrix) for HplHalo.shader, which fades the glow by
        /// the visible part of it (cBillboard::AssignOcclusionQuery / RetrieveOcculsionQuery).
        /// </summary>
        void UpdateHaloSource()
        {
            if (material == null || material.shader.name != HaloShader) return;
            _block ??= new MaterialPropertyBlock();
            if (!_runtimeColor) _block.SetVector(HplColorId, color);
            _block.SetMatrix(HaloBoxId, Matrix4x4.TRS(transform.position, transform.rotation, Vector3.Scale(transform.lossyScale, haloSourceSize)));
            _block.SetFloat(HaloActiveId, 1f);
            if (_quad != null && _quad.TryGetComponent(out Renderer r)) r.SetPropertyBlock(_block);
        }
    }
}
