using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.World
{
    /// <summary>
    /// The world reflection of water (cRendererDeferred::RenderReflection / RenderSubMeshEntityReflection): a mirror
    /// camera renders the scene reflected in the plane of a one-sided water surface (all triangle normals within
    /// 0.9 of the first, cSubMesh::CompileOneSidedInfo) with an oblique near plane on the water and the same
    /// projection, so its frame lines up with the screen; HplWater.shader reads it at the distorted screen position.
    /// The reflection is rendered for the nearest visible surface in front of it, within ReflectionFadeEnd along the
    /// view (0: any distance); the others get none (HPL2 clears the texture). Water surfaces are not drawn in the
    /// reflection (as HPL2 skips world-reflection materials there). Resolution: the screen / 2 (mlReflectionSizeDiv).
    /// </summary>
    public sealed class HplWaterReflection : MonoBehaviour
    {
        const string WaterShader = "RTXMnesia/HplWater";
        const int SizeDiv = 2;
        const float ClipPlaneOffset = 0.01f;

        sealed class Surface
        {
            public Renderer renderer;
            public Vector3 localNormal, localPoint;
            public float maxDistance;
        }

        static readonly HashSet<Camera> s_reflectionCameras = new();
        static readonly int ActiveId = Shader.PropertyToID("_HplReflectionActive");
        static readonly int TexId = Shader.PropertyToID("_HplReflectionTex");
        static readonly int FlipId = Shader.PropertyToID("_HplReflectionFlipY");
        static readonly Plane[] s_frustum = new Plane[6];

        readonly List<Surface> _surfaces = new();
        MaterialPropertyBlock _block;
        Camera _mirror;
        RenderTexture _texture;
        Surface _active;
        bool _hidden;

        /// <summary>Cameras that render water reflections: screen effects are not drawn in them.</summary>
        public static bool IsReflectionCamera(Camera camera) => camera != null && s_reflectionCameras.Contains(camera);

        /// <summary>The surface the last reflection was rendered for (null: none).</summary>
        public Renderer ActiveSurface => _active?.renderer;
        public int SurfaceCount => _surfaces.Count;
        public Camera MirrorCamera => _mirror;

        /// <summary>
        /// A reflection handler (under <paramref name="parent"/>) for the water surfaces with world reflection among
        /// <paramref name="renderers"/>, or null if there are none.
        /// </summary>
        public static HplWaterReflection Create(IEnumerable<MeshRenderer> renderers, Transform parent)
        {
            var surfaces = new List<Surface>();
            foreach (var renderer in renderers)
            {
                Material water = null;
                foreach (var m in renderer.sharedMaterials)
                    if (m != null && m.shader != null && m.shader.name == WaterShader && Mathf.RoundToInt(m.GetFloat("_ReflectionMode")) == 2)
                        water = m;
                if (water == null) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || !OneSided(filter.sharedMesh, out Vector3 normal, out Vector3 point)) continue;
                surfaces.Add(new Surface
                {
                    renderer = renderer,
                    localNormal = normal,
                    localPoint = point,
                    maxDistance = water.GetVector("_ReflectionFade").w,
                });
            }
            if (surfaces.Count == 0) return null;
            var go = new GameObject("HplWaterReflection (runtime)") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            var handler = go.AddComponent<HplWaterReflection>();
            handler._surfaces.AddRange(surfaces);
            return handler;
        }

        /// <summary>cSubMesh::CompileOneSidedInfo: one-sided if every triangle normal is within 0.9 of the first.</summary>
        static bool OneSided(Mesh mesh, out Vector3 normal, out Vector3 point)
        {
            normal = Vector3.zero;
            point = Vector3.zero;
            if (!mesh.isReadable) return false;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var triangles = mesh.triangles;
            if (triangles.Length < 3) return false;
            Vector3 first = Vector3.zero, sum = Vector3.zero, posSum = Vector3.zero;
            int count = 0;
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]], v1 = vertices[triangles[i + 1]], v2 = vertices[triangles[i + 2]];
                Vector3 n = Vector3.Cross(v1 - v0, v2 - v0).normalized; // Unity's clockwise front faces
                posSum += v0;
                if (count == 0) first = n;
                else if (Vector3.Dot(first, n) < 0.9f) return false;
                sum += n;
                count++;
            }
            normal = (sum / count).normalized;
            point = posSum / count;
            // The facing from the vertex normals, if the mesh has them.
            if (normals != null && normals.Length == vertices.Length)
            {
                Vector3 vertexSum = Vector3.zero;
                foreach (var vn in normals) vertexSum += vn;
                if (Vector3.Dot(vertexSum, normal) < 0f) normal = -normal;
            }
            return normal != Vector3.zero;
        }

        void OnEnable()
        {
            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            ShowSurfaces();
        }

        void OnDestroy()
        {
            if (_mirror != null)
            {
                s_reflectionCameras.Remove(_mirror);
                DestroyNow(_mirror.gameObject);
            }
            if (_texture != null)
            {
                _texture.Release();
                DestroyNow(_texture);
            }
        }

        static void DestroyNow(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        static Camera MainCamera(List<Camera> cameras)
        {
            foreach (var c in cameras)
                if (c != null && c.cameraType == CameraType.Game && c.targetTexture == null && !IsReflectionCamera(c)) return c;
            return null;
        }

        void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            var main = MainCamera(cameras);
            if (main == null) return;

            _active = ChooseSurface(main);
            if (_active != null)
            {
                EnsureMirror(main);
                PlaceMirror(main, _active);
            }
            if (_mirror != null) _mirror.enabled = _active != null;
            ApplyBlocks();
        }

        /// <summary>
        /// Renders the reflection for <paramref name="main"/> at once (tools: edit mode renders of one camera, where
        /// no other camera is drawn in the same frame). False if no surface is in view.
        /// </summary>
        public bool RenderFor(Camera main)
        {
            _active = ChooseSurface(main);
            if (_active == null) return false;
            EnsureMirror(main);
            PlaceMirror(main, _active);
            _mirror.Render();
            _mirror.enabled = false;
            ApplyBlocks();
            return true;
        }

        void ApplyBlocks()
        {
            _block ??= new MaterialPropertyBlock();
            foreach (var s in _surfaces)
            {
                if (s.renderer == null) continue;
                s.renderer.GetPropertyBlock(_block);
                bool active = s == _active && _texture != null;
                _block.SetFloat(ActiveId, active ? 1f : 0f);
                _block.SetTexture(TexId, active ? _texture : Texture2D.blackTexture);
                _block.SetFloat(FlipId, SystemInfo.graphicsUVStartsAtTop ? 1f : 0f);
                s.renderer.SetPropertyBlock(_block);
            }
        }

        Surface ChooseSurface(Camera main)
        {
            GeometryUtility.CalculateFrustumPlanes(main, s_frustum);
            Vector3 camPos = main.transform.position, forward = main.transform.forward;
            Surface best = null;
            float bestDistance = float.MaxValue;
            foreach (var s in _surfaces)
            {
                var r = s.renderer;
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                var bounds = r.bounds;
                if (!GeometryUtility.TestPlanesAABB(s_frustum, bounds)) continue;
                Vector3 n = r.localToWorldMatrix.MultiplyVector(s.localNormal).normalized;
                Vector3 p = r.localToWorldMatrix.MultiplyPoint3x4(s.localPoint);
                if (Vector3.Dot(camPos - p, n) <= 0f) continue; // behind the surface
                // In range: the bounding box reaches closer than the maximum distance along the view.
                Vector3 nearest = bounds.ClosestPoint(camPos);
                float alongView = Vector3.Dot(nearest - camPos, forward);
                if (s.maxDistance > 0f && alongView > s.maxDistance) continue;
                float distance = (nearest - camPos).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = s;
                }
            }
            return best;
        }

        void EnsureMirror(Camera main)
        {
            int width = Mathf.Max(1, main.pixelWidth / SizeDiv), height = Mathf.Max(1, main.pixelHeight / SizeDiv);
            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                if (_texture != null)
                {
                    _texture.Release();
                    DestroyNow(_texture);
                }
                _texture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR)
                {
                    name = "HplWaterReflection",
                    hideFlags = HideFlags.DontSave,
                };
                _texture.Create();
                if (_mirror != null) _mirror.targetTexture = _texture;
            }
            if (_mirror != null) return;

            var go = new GameObject("HplWaterReflection Camera") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            _mirror = go.AddComponent<Camera>();
            _mirror.enabled = false;
            _mirror.targetTexture = _texture;
            _mirror.clearFlags = CameraClearFlags.SolidColor;
            _mirror.backgroundColor = Color.black;
            var data = go.AddComponent<HDAdditionalCameraData>();
            data.invertFaceCulling = true; // the mirrored view turns the triangles around
            data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            data.backgroundColorHDR = Color.black;
            if (main.TryGetComponent<HDAdditionalCameraData>(out var mainData))
            {
                data.volumeLayerMask = mainData.volumeLayerMask;
                data.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
            }
            s_reflectionCameras.Add(_mirror);
        }

        /// <summary>The main camera's view mirrored in the surface plane, its projection with the near plane on the water.</summary>
        void PlaceMirror(Camera main, Surface s)
        {
            // A mirror texture must stay scene-linear. HDRP applies display-gamut conversion / HDR encoding
            // to Game cameras even when they target a texture; Reflection cameras bypass display output.
            _mirror.cameraType = Hpl.HplHdr.Active ? CameraType.Reflection : CameraType.Game;
            // HPL2 renders the reflection with the world renderer: the sky box too.
            if (_mirror.TryGetComponent(out HDAdditionalCameraData mirrorData)) mirrorData.backgroundColorHDR = Hpl.HplLighting.BackgroundColor;
            var r = s.renderer;
            Vector3 n = r.localToWorldMatrix.MultiplyVector(s.localNormal).normalized;
            Vector3 p = r.localToWorldMatrix.MultiplyPoint3x4(s.localPoint);
            float d = -Vector3.Dot(n, p);
            var reflection = new Matrix4x4();
            reflection.m00 = 1f - 2f * n.x * n.x; reflection.m01 = -2f * n.x * n.y; reflection.m02 = -2f * n.x * n.z; reflection.m03 = -2f * d * n.x;
            reflection.m10 = -2f * n.y * n.x; reflection.m11 = 1f - 2f * n.y * n.y; reflection.m12 = -2f * n.y * n.z; reflection.m13 = -2f * d * n.y;
            reflection.m20 = -2f * n.z * n.x; reflection.m21 = -2f * n.z * n.y; reflection.m22 = 1f - 2f * n.z * n.z; reflection.m23 = -2f * d * n.z;
            reflection.m33 = 1f;

            var t = main.transform;
            _mirror.cullingMask = main.cullingMask;
            _mirror.nearClipPlane = main.nearClipPlane;
            _mirror.farClipPlane = main.farClipPlane;
            _mirror.fieldOfView = main.fieldOfView;
            _mirror.depth = main.depth - 10f;
            _mirror.transform.SetPositionAndRotation(reflection.MultiplyPoint(t.position),
                Quaternion.LookRotation(reflection.MultiplyVector(t.forward), reflection.MultiplyVector(t.up)));
            _mirror.worldToCameraMatrix = main.worldToCameraMatrix * reflection;

            // Oblique near plane on the water (Unity camera space looks down -z).
            Vector3 offsetPos = p + n * ClipPlaneOffset;
            var view = _mirror.worldToCameraMatrix;
            Vector3 cpos = view.MultiplyPoint(offsetPos);
            Vector3 cnormal = view.MultiplyVector(n).normalized;
            var clipPlane = new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
            _mirror.projectionMatrix = main.projectionMatrix;
            _mirror.projectionMatrix = _mirror.CalculateObliqueMatrix(clipPlane);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _mirror) return;
            // The water surfaces themselves are not in their reflection.
            foreach (var s in _surfaces)
                if (s.renderer != null) s.renderer.forceRenderingOff = true;
            _hidden = true;
        }

        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _mirror) ShowSurfaces();
        }

        void ShowSurfaces()
        {
            if (!_hidden) return;
            foreach (var s in _surfaces)
                if (s.renderer != null) s.renderer.forceRenderingOff = false;
            _hidden = false;
        }
    }
}
