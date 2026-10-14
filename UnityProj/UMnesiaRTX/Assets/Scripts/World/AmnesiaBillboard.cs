using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// HPL2 billboard (Billboard.cpp): a quad in the local XY plane.
    /// FixedAxis billboards are plain mesh renderers on this object (scale = size).
    /// Point (faces the camera) and Axis (turns around <see cref="axis"/> towards the camera) billboards are drawn
    /// by a hidden, unsaved child that is re-oriented before every camera renders, also in the editor.
    /// Halos (occlusion-faded glows, drawn without depth test in HPL2) and light connections are stored for later.
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
            if (kind == Kind.FixedAxis || mesh == null) return;
            var go = new GameObject(name + " (camera facing)") { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var quadRenderer = go.AddComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = material;
            quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            quadRenderer.enabled = visible;
            _quad = go.transform;
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
        }
    }
}
