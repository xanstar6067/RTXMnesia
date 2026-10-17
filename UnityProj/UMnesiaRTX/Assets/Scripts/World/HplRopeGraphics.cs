using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.World
{
    /// <summary>
    /// The drawn rope (port of cRopeEntity, RopeEntity.cpp): one quad per segment between particles, turned to the
    /// camera around the segment (cRopeEntity::UpdateGraphicsForViewport), built before each camera renders. The
    /// first point is the start particle, the others the smooth positions; the UVs tile LengthTileAmount times per
    /// segment and the first (shorter) segment is cut at its end, as the original.
    ///
    /// Differences: the attachment offsets are taken from the bodies' interpolated poses of the frame (HPL2 draws at
    /// its 60 Hz step; the rope end would trail an interpolated body at a higher frame rate), and the normal faces
    /// the camera (cRopeEntity stores the segment's forward, away from the camera; the HDRP light would hit the back).
    /// </summary>
    public sealed class HplRopeGraphics
    {
        readonly HplPhysicsRope _rope;
        readonly GameObject _gameObject;
        readonly MeshRenderer _renderer;
        readonly Mesh _mesh;
        readonly int _maxSegments;
        public float Radius, LengthTileAmount = 1f, LengthTileSize = 1f;

        readonly List<Vector3> _vertices = new(), _normals = new();
        readonly List<Vector4> _tangents = new();
        readonly List<Vector2> _uvs = new();
        readonly List<int> _indices = new();
        readonly List<Vector3> _points = new();

        public HplRopeGraphics(string name, HplPhysicsRope rope, int maxSegments, Transform parent, Material material)
        {
            _rope = rope;
            _maxSegments = maxSegments;
            Radius = rope.ParticleRadius;
            _gameObject = new GameObject(name + " (rope)") { hideFlags = HideFlags.DontSave };
            _gameObject.transform.SetParent(parent, false);
            _gameObject.AddComponent<MeshFilter>().sharedMesh = _mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _renderer = _gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            if (material == null) _renderer.enabled = false;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        public GameObject GameObject => _gameObject;
        public int SegmentCount { get; private set; }

        public void Destroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (_gameObject != null)
            {
                if (Application.isPlaying) Object.Destroy(_gameObject);
                else Object.DestroyImmediate(_gameObject);
            }
            if (_mesh != null)
            {
                if (Application.isPlaying) Object.Destroy(_mesh);
                else Object.DestroyImmediate(_mesh);
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_gameObject == null)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                return;
            }
            if (!_renderer.enabled) return;
            Build(camera.transform.forward);
        }

        /// <summary>The drawn points (HPL2 space): the start particle, then the smooth positions.</summary>
        void CollectPoints()
        {
            _points.Clear();
            var particles = _rope.Particles;
            int count = particles.Count;
            Vector3 add0 = AttachmentOffset(0), add1 = AttachmentOffset(1);
            float maxCount = count - 1;
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    _points.Add(particles[0].Position);
                    continue;
                }
                float t = i / maxCount;
                _points.Add(particles[i].Position + add0 * (1f - t) + add1 * t);
            }
        }

        /// <summary>CalculateSmoothPositions' offset of an attached end, from the body's drawn (interpolated) pose.</summary>
        Vector3 AttachmentOffset(int index)
        {
            var attachment = _rope.GetAttachment(index);
            if (attachment.Body == null || attachment.Particle == null) return Vector3.zero;
            if (_rope.IsSleeping)
            {
                // Smooth positions are not updated while the rope sleeps: the last ones.
                var p = index == 0 ? _rope.StartParticle : _rope.EndParticle;
                return p.SmoothPosition - p.Position;
            }
            var t = attachment.Body.Data.transform;
            Vector3 point = HplMatrix.Mul(HplSpace.ToHpl(t.position, t.rotation), attachment.BodyLocalPos);
            return point - attachment.Particle.Position;
        }

        /// <summary>cRopeEntity::UpdateGraphicsForViewport in HPL2 space, then mirrored to Unity.</summary>
        public void Build(Vector3 unityCameraForward)
        {
            CollectPoints();
            Vector3 camForward = HplSpace.Direction(unityCameraForward);
            float segmentLength = _rope.SegmentLength;

            _vertices.Clear(); _normals.Clear(); _tangents.Clear(); _uvs.Clear(); _indices.Clear();
            Vector2 uv0 = new(1f, 1f * LengthTileAmount), uv1 = new(0f, 1f * LengthTileAmount), uv2 = new(0f, 0f), uv3 = new(1f, 0f);
            int segments = 0;
            Vector3 prev = Vector3.zero;
            for (int count = 1; count <= _points.Count; count++)
            {
                if (count - 1 >= _maxSegments) break;
                Vector3 pos = _points[count - 1];
                if (count == 1)
                {
                    prev = pos;
                    continue;
                }
                Vector3 delta = pos - prev;
                float length = delta.magnitude;
                if (length <= 0f)
                {
                    prev = pos;
                    continue;
                }
                Vector3 up = delta / length;
                Vector3 right = Vector3.Cross(up, camForward).normalized;
                Vector3 forward = Vector3.Cross(right, up);

                AddVertex(prev + right * Radius, -forward, right);
                AddVertex(prev - right * Radius, -forward, right);
                AddVertex(prev - right * Radius + up * length, -forward, right);
                AddVertex(prev + right * Radius + up * length, -forward, right);

                Vector2 a = uv0, b = uv1;
                if (count == 2 && (length < segmentLength || segmentLength == 0f))
                {
                    float yAdd = segmentLength == 0f ? 1f - length / LengthTileSize : (1f - length / segmentLength) * LengthTileAmount;
                    a.y -= yAdd;
                    b.y -= yAdd;
                }
                // HPL2 v grows down the image (top row first): Unity v = 1 − v.
                _uvs.Add(HplSpace.Uv(a));
                _uvs.Add(HplSpace.Uv(b));
                _uvs.Add(HplSpace.Uv(uv2));
                _uvs.Add(HplSpace.Uv(uv3));

                int baseIndex = segments * 4;
                _indices.Add(baseIndex); _indices.Add(baseIndex + 1); _indices.Add(baseIndex + 2);
                _indices.Add(baseIndex + 2); _indices.Add(baseIndex + 3); _indices.Add(baseIndex);
                segments++;
                prev = pos;
            }
            SegmentCount = segments;

            _mesh.Clear();
            if (segments == 0) return;
            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetTangents(_tangents);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetTriangles(_indices, 0, true);
        }

        // The X mirror keeps the triangles front facing (HPL2 counter-clockwise → Unity clockwise); the bitangent
        // (normal × tangent) then points up the rope, the way v grows.
        void AddVertex(Vector3 hplPos, Vector3 hplNormal, Vector3 hplTangent)
        {
            _vertices.Add(HplSpace.Position(hplPos));
            _normals.Add(HplSpace.Direction(hplNormal));
            Vector3 t = HplSpace.Direction(hplTangent);
            _tangents.Add(new Vector4(t.x, t.y, t.z, 1f));
        }
    }
}
