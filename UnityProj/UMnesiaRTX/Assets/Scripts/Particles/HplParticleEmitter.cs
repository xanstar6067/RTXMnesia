using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;
using UnityEngine.Rendering;
using EmitterData = RTXMnesia.Particles.HplParticleBank.EmitterData;

namespace RTXMnesia.Particles
{
    /// <summary>
    /// One emitter of a particle system: iParticleEmitter + cParticleEmitter_UserData (ParticleEmitter.cpp,
    /// ParticleEmitter_UserData.cpp). The particles live in HPL2 space (world, or the system's space for
    /// CoordSystem="Local") and move once per logic step, as in HPL2. The quads are built like
    /// iParticleEmitter::UpdateGraphicsForViewport, but turned to the camera in the vertex shader
    /// (Assets/Shaders/HplParticle.shader), so one mesh serves every camera; the distance fade of the system is
    /// done there too. Errors of the original are kept: a removed particle is still updated in its step and the one
    /// swapped into its place skips the step; SubDiv "1 n" has no sub division.
    /// </summary>
    public sealed class HplParticleEmitter
    {
        sealed class Particle
        {
            public Vector3 pos, lastPos, lastCollidePos, acc, vel;
            public float speedMul, maxSpeed;
            public Color startColor, color;
            public Vector2 startSize, size;
            public float startLife, life;
            public float lifeSizeMiddleStart, lifeSizeMiddleEnd, lifeColorMiddleStart, lifeColorMiddleEnd;
            public int subDivNum;
            public float bounceAmount;
            public int bounceCount;
            public float spin, spinVel, spinFactor;
            public Vector3 revolutionVel;
        }

        const float TwoPi = Mathf.PI * 2f;

        readonly HplParticleSystem _system;
        readonly Particle[] _particles;
        int _numOfParticles, _maxParticles;
        readonly Matrix4x4 _localMatrix; // HPL2 space, relative to the system
        readonly List<Vector2[]> _subDivUV = new();
        readonly Vector2 _maxDrawSize;

        int _sleepCount = 60 * 5; // a looping system reaches its equilibrium first
        bool _dying, _respawn;
        float _frame, _time, _createCount, _collideCount, _pauseCount, _pauseWaitCount;
        bool _paused;

        public EmitterData Data { get; }
        public int ParticleCount => _numOfParticles;

        public HplParticleEmitter(HplParticleSystem system, EmitterData data)
        {
            _system = system;
            Data = data;
            _particles = new Particle[Mathf.Max(0, data.maxParticleNum)];
            for (int i = 0; i < _particles.Length; i++) _particles[i] = new Particle();
            _maxParticles = _particles.Length;
            _localMatrix = HplMatrix.SetTranslation(HplMatrix.RotateXYZ(data.angleOffset), data.posOffset);

            _collideCount = 1f / data.collisionUpdateRate;
            _respawn = data.respawn;
            SetSubDivUV(data.subDiv);

            float sizeMul = data.startRelSize;
            if (sizeMul < data.middleRelSize) sizeMul = data.middleRelSize;
            if (sizeMul < data.endRelSize) sizeMul = data.endRelSize;
            _maxDrawSize = data.maxStartSize * sizeMul;
        }

        // ------------------------------------------------------------------ state

        /// <summary>cParticleEmitter_UserData::IsDying: no respawn (any more) or out of particles.</summary>
        public bool IsDying => !_respawn || _dying;

        public bool IsDead => _numOfParticles == 0 && _dying;

        /// <summary>cParticleEmitter_UserData::Kill: no more respawning; the particles that are left live on.</summary>
        public void Kill() => _respawn = false;

        public void KillInstantly()
        {
            _maxParticles = 0;
            _numOfParticles = 0;
            _dying = true;
        }

        /// <summary>The emitter's matrix in HPL2 world space (system × offset).</summary>
        Matrix4x4 WorldMatrix => _system.HplWorldMatrix * _localMatrix;

        // ------------------------------------------------------------------ update

        /// <summary>iParticleEmitter::UpdateLogic. Not drawn for a while (5 s at the start, then 10 steps) it sleeps.</summary>
        public void UpdateLogic(float timeStep)
        {
            if (!IsDying && !WasRendered())
            {
                if (_sleepCount <= 0) return;
                _sleepCount--;
            }
            else if (_sleepCount < 10) _sleepCount = 10;

            UpdateMotion(timeStep);
            _meshDirty = true;
        }

        void SetSubDivUV(Vector2Int subDiv)
        {
            // HPL2 tests x twice: a sub division only along y is ignored.
            if ((subDiv.x > 1 || subDiv.x > 1) && subDiv.x > 0 && subDiv.y > 0)
            {
                int count = subDiv.x * subDiv.y;
                for (int i = 0; i < count; i++) _subDivUV.Add(null);
                float invW = 1f / subDiv.x, invH = 1f / subDiv.y;
                for (int x = 0; x < subDiv.x; ++x)
                    for (int y = 0; y < subDiv.y; ++y)
                        _subDivUV[y * subDiv.x + x] = new[]
                        {
                            new Vector2((x + 1) * invW, (y + 1) * invH),
                            new Vector2(x * invW, (y + 1) * invH),
                            new Vector2(x * invW, y * invH),
                            new Vector2((x + 1) * invW, y * invH),
                        };
            }
        }

        Particle CreateParticle()
        {
            if (_numOfParticles == _maxParticles) return null;
            ++_numOfParticles;
            return _particles[_numOfParticles - 1];
        }

        void SwapRemove(int index)
        {
            if (index < _numOfParticles - 1)
                (_particles[index], _particles[_numOfParticles - 1]) = (_particles[_numOfParticles - 1], _particles[index]);
            _numOfParticles--;
        }

        static Vector3 RandRect(Vector3 min, Vector3 max) =>
            new(HplMath.RandRectf(min.x, max.x), HplMath.RandRectf(min.y, max.y), HplMath.RandRectf(min.z, max.z));

        static Vector2 RandRect(Vector2 min, Vector2 max) =>
            new(HplMath.RandRectf(min.x, max.x), HplMath.RandRectf(min.y, max.y));

        static Color RandRect(Color min, Color max) =>
            new(HplMath.RandRectf(min.r, max.r), HplMath.RandRectf(min.g, max.g), HplMath.RandRectf(min.b, max.b), HplMath.RandRectf(min.a, max.a));

        /// <summary>cParticleEmitter_UserData::SetParticleDefaults.</summary>
        void SetParticleDefaults(Particle p)
        {
            var d = Data;
            p.startColor = RandRect(d.minStartColor, d.maxStartColor);
            p.color = p.startColor * d.startRelColor;

            if (d.minStartSize.y == 0f && d.maxStartSize.y == 0f)
            {
                float s = HplMath.RandRectf(d.minStartSize.x, d.maxStartSize.x);
                p.startSize = new Vector2(s, s);
            }
            else p.startSize = RandRect(d.minStartSize, d.maxStartSize);
            p.size = p.startSize * d.startRelSize;

            if (_subDivUV.Count > 1)
                p.subDivNum = d.subDivType == PESubDivType.Animation ? 0 : HplMath.RandRectl(0, _subDivUV.Count - 1);

            p.bounceAmount = HplMath.RandRectf(d.minBounceAmount, d.maxBounceAmount);
            p.bounceCount = HplMath.RandRectl(d.minCollisionMax, d.maxCollisionMax);

            Matrix4x4 start = d.coordSystem == PECoordSystem.World ? WorldMatrix : _localMatrix;
            if (d.startPosType == PEStartPosType.Box)
            {
                p.pos = HplMatrix.GetTranslation(start) + RandRect(d.minStartPos, d.maxStartPos);
            }
            else
            {
                Vector2 rot = RandRect(d.minStartAngles, d.maxStartAngles);
                var offset = new Vector3(0f, HplMath.RandRectf(d.minStartRadius, d.maxStartRadius), 0f);
                p.pos = HplMatrix.GetTranslation(start) + HplMatrix.Mul(HplMatrix.RotateXYZ(rot), offset);
            }
            p.lastPos = p.pos;
            p.lastCollidePos = p.pos;

            if (d.startVelType == PEStartPosType.Box)
            {
                p.vel = RandRect(d.minStartVel, d.maxStartVel);
            }
            else
            {
                Vector2 rot = RandRect(d.minStartVelAngles, d.maxStartVelAngles);
                var speed = new Vector3(0f, HplMath.RandRectf(d.minStartVelSpeed, d.maxStartVelSpeed), 0f);
                p.vel = HplMatrix.Mul(HplMatrix.RotateXYZ(rot), speed);
            }
            if (d.usesDirection && d.coordSystem == PECoordSystem.World)
                p.vel = HplMatrix.Mul3x3(start, p.vel);

            p.maxSpeed = HplMath.RandRectf(d.minVelMaximum, d.maxVelMaximum);
            p.speedMul = HplMath.RandRectf(d.minSpeedMultiply, d.maxSpeedMultiply);
            p.acc = RandRect(d.minStartAcc, d.maxStartAcc);

            if (d.partSpinType == PEPartSpinType.Constant)
            {
                p.spinVel = HplMath.RandRectf(d.minSpinRange, d.maxSpinRange);
            }
            else
            {
                p.spinFactor = HplMath.RandRectf(d.minSpinRange, d.maxSpinRange);
                p.spinVel = 0f;
            }
            p.spin = HplMath.RandRectf(0f, TwoPi);
            p.revolutionVel = RandRect(d.minRevVel, d.maxRevVel);

            p.startLife = HplMath.RandRectf(d.minLifeSpan, d.maxLifeSpan);
            p.life = p.startLife;
            p.lifeSizeMiddleStart = p.life * (1f - d.middleRelSizeTime);
            p.lifeSizeMiddleEnd = p.life * (1f - (d.middleRelSizeTime + d.middleRelSizeLength));
            p.lifeColorMiddleStart = p.life * (1f - d.middleRelColorTime);
            p.lifeColorMiddleEnd = p.life * (1f - (d.middleRelColorTime + d.middleRelColorLength));
        }

        /// <summary>cParticleEmitter_UserData::UpdateMotion.</summary>
        void UpdateMotion(float timeStep)
        {
            var d = Data;
            if (_time < d.startTimeOffset)
            {
                _time += timeStep;
                return;
            }

            if (_paused)
            {
                _pauseCount -= timeStep;
                if (_pauseCount <= 0f)
                {
                    _paused = false;
                    _pauseWaitCount = HplMath.RandRectf(d.minPauseInterval, d.maxPauseInterval);
                }
            }
            else if (d.minPauseLength > 0f && d.minPauseInterval > 0f)
            {
                _pauseWaitCount -= timeStep;
                if (_pauseWaitCount <= 0f)
                {
                    _pauseCount = HplMath.RandRectf(d.minPauseLength, d.maxPauseLength);
                    _paused = true;
                }
            }

            if (!_paused && _numOfParticles < _maxParticles)
            {
                _createCount += d.particlesPerSecond * timeStep;
                while (_createCount >= 0.99999f && _numOfParticles < _maxParticles)
                {
                    SetParticleDefaults(CreateParticle());
                    _createCount -= 1f;
                }
            }

            bool colliding = false;
            if (d.collides)
            {
                _collideCount -= timeStep;
                if (_collideCount <= 0f)
                {
                    _collideCount = 1f / d.collisionUpdateRate;
                    colliding = true;
                }
            }

            for (int i = 0; i < _numOfParticles; ++i)
            {
                var p = _particles[i];
                p.lastPos = p.pos;
                p.pos += p.vel * timeStep;
                p.vel += p.acc * timeStep;

                switch (d.gravityType)
                {
                    case PEGravityType.Vector:
                        p.vel += d.gravityAcc * timeStep;
                        break;
                    case PEGravityType.Center:
                    {
                        Matrix4x4 m = d.coordSystem == PECoordSystem.World ? WorldMatrix : _localMatrix;
                        Vector3 dir = (p.pos - HplMatrix.GetTranslation(m)).normalized;
                        p.vel += dir * d.gravityAcc.y * timeStep;
                        break;
                    }
                }

                if (p.maxSpeed > 0f)
                {
                    float speed = p.vel.magnitude;
                    if (speed > p.maxSpeed) p.vel = p.vel / speed * p.maxSpeed;
                }

                if (p.speedMul != 0f && p.speedMul != 1f) p.vel *= Mathf.Pow(p.speedMul, timeStep);

                if (d.usePartSpin)
                {
                    p.spin += p.spinVel * timeStep;
                    if (d.partSpinType == PEPartSpinType.Movement) p.spinVel = p.vel.magnitude * p.spinFactor;
                    if (p.spin >= TwoPi) p.spin -= TwoPi;
                    else if (p.spin <= -TwoPi) p.spin += TwoPi;
                }

                if (d.useRevolution)
                {
                    Matrix4x4 rot = HplMatrix.RotateXYZ(p.revolutionVel * timeStep);
                    p.pos = HplMatrix.Mul(rot, p.pos);
                    p.vel = HplMatrix.Mul(rot, p.vel);
                }

                if (colliding)
                {
                    if (CheckCollision(p.lastCollidePos, p.pos, out Vector3 normal, out Vector3 hitPos))
                    {
                        p.pos = hitPos;
                        float speed = p.vel.magnitude;
                        Vector3 reflection = (p.vel - normal * 2f * Vector3.Dot(p.vel, normal)).normalized;
                        p.vel = reflection * (speed * p.bounceAmount);
                        p.bounceCount--;
                        if (p.bounceCount <= 0) p.life = 0f;
                    }
                    p.lastCollidePos = p.pos;
                }

                p.life -= timeStep;
                if (p.life <= 0f)
                {
                    if (_respawn)
                    {
                        if (_paused) SwapRemove(i);
                        else SetParticleDefaults(p);
                    }
                    else
                    {
                        SwapRemove(i);
                        _maxParticles--;
                        if (_maxParticles <= 0) _dying = true;
                    }
                }

                if (d.subDivType == PESubDivType.Animation)
                {
                    float lifePercent = 1f - p.life / p.startLife;
                    p.subDivNum = (int)(lifePercent * _subDivUV.Count - 0.0001f);
                }

                if (p.life > p.lifeColorMiddleStart)
                {
                    float t = (p.life - p.lifeColorMiddleStart) / (p.startLife - p.lifeColorMiddleStart);
                    p.color = p.startColor * d.startRelColor * t + p.startColor * d.middleRelColor * (1f - t);
                }
                else if (p.life > p.lifeColorMiddleEnd)
                {
                    p.color = p.startColor * d.middleRelColor;
                }
                else
                {
                    float t = p.life / p.lifeColorMiddleEnd;
                    p.color = p.startColor * d.middleRelColor * t + p.startColor * d.endRelColor * (1f - t);
                }
                if (d.multiplyRGBWithAlpha)
                {
                    p.color.r *= p.color.a;
                    p.color.g *= p.color.a;
                    p.color.b *= p.color.a;
                }

                if (p.life > p.lifeSizeMiddleStart)
                {
                    float t = (p.life - p.lifeSizeMiddleStart) / (p.startLife - p.lifeSizeMiddleStart);
                    p.size = p.startSize * d.startRelSize * t + p.startSize * d.middleRelSize * (1f - t);
                }
                else if (p.life > p.lifeSizeMiddleEnd)
                {
                    p.size = p.startSize * d.middleRelSize;
                }
                else
                {
                    float t = p.life / p.lifeSizeMiddleEnd;
                    p.size = p.startSize * d.middleRelSize * t + p.startSize * d.endRelSize * (1f - t);
                }
            }

            if (d.materials.Length > 1)
            {
                _frame += d.frameStep * timeStep;
                if (_frame >= d.maxFrameTime) _frame = 0f;
            }
        }

        /// <summary>
        /// cParticleEmitterData_UserData::CheckCollision: the closest hit of a ray through the world, skipping
        /// characters and bodies that collide with nothing (HPL2 positions in and out; like HPL2, the positions of a
        /// local emitter are cast as world positions).
        /// </summary>
        static bool CheckCollision(Vector3 start, Vector3 end, out Vector3 normal, out Vector3 position)
        {
            normal = position = default;
            Vector3 from = HplSpace.Position(start), delta = HplSpace.Position(end) - from;
            float length = delta.magnitude;
            if (length <= 0f) return false;
            int mask = Physics.DefaultRaycastLayers & ~(1 << Dynamics.HplPhysicsWorld.NoCollideLayer);
            if (!Physics.Raycast(from, delta / length, out RaycastHit hit, length, mask, QueryTriggerInteraction.Ignore)) return false;
            position = HplSpace.Position(hit.point);
            normal = HplSpace.Direction(hit.normal);
            return true;
        }

        // ------------------------------------------------------------------ rendering

        static readonly int ColorMulId = Shader.PropertyToID("_HplColorMul");
        static readonly int FadeCenterId = Shader.PropertyToID("_HplFadeCenter");
        static readonly int FadeDistId = Shader.PropertyToID("_HplFadeDist");
        static readonly int MulRgbByAlphaId = Shader.PropertyToID("_HplMulRgbByAlpha");

        static readonly Vector2[] DefaultUV = { new(1, 1), new(0, 1), new(0, 0), new(1, 0) };
        static readonly Vector2[] QuadCorner = { new(1, -1), new(-1, -1), new(-1, 1), new(1, 1) };
        static readonly float[] LineSign = { -1f, 1f, -1f, 1f };

        GameObject _rendererObject;
        MeshRenderer _renderer;
        Mesh _mesh;
        MaterialPropertyBlock _block;
        bool _meshDirty = true;
        readonly List<Vector3> _vertices = new();
        readonly List<Vector2> _uv = new();
        readonly List<Vector4> _uv1 = new(), _uv2 = new();
        readonly List<Color> _colors = new();
        readonly List<int> _indices = new();

        /// <summary>iRenderable::mlRenderFrameCount: drawn by a camera last frame. Edit mode and batch runs count as drawn.</summary>
        bool WasRendered() => !Application.isPlaying || _renderer == null || _renderer.isVisible || !_renderer.enabled;

        /// <summary>Builds the mesh of this step's particles (iParticleEmitter::UpdateGraphicsForViewport without the camera part).</summary>
        public void UpdateGraphics(bool visible)
        {
            if (!visible || _numOfParticles == 0 || Data.materials.Length == 0 || Data.beam)
            {
                if (_renderer != null) _renderer.enabled = false;
                return;
            }
            EnsureRenderer();
            var material = Data.materials[Mathf.Clamp((int)_frame, 0, Data.materials.Length - 1)];
            if (_renderer.sharedMaterial != material) _renderer.sharedMaterial = material;
            _renderer.enabled = material != null;

            SetSystemProperties();
            if (!_meshDirty) return;
            _meshDirty = false;

            _vertices.Clear(); _uv.Clear(); _uv1.Clear(); _uv2.Clear(); _colors.Clear(); _indices.Clear();
            var d = Data;
            bool local = d.coordSystem == PECoordSystem.Local;
            Matrix4x4 system = _system.HplWorldMatrix;
            Vector3 right = default, forward = default;
            if (d.drawType == PEDrawType.Axis)
            {
                Matrix4x4 inv = WorldMatrix.inverse;
                right = HplMatrix.GetRight(inv);
                forward = HplMatrix.GetForward(inv);
            }

            Vector3 origin = HplSpace.Position(HplMatrix.GetTranslation(WorldMatrix));
            Vector3 min = origin, max = origin;
            for (int i = 0; i < _numOfParticles; i++)
            {
                var p = _particles[i];
                Vector2[] uv = _subDivUV.Count > 1 ? _subDivUV[Mathf.Clamp(p.subDivNum, 0, _subDivUV.Count - 1)] : DefaultUV;
                Vector3 pos = local ? HplMatrix.Mul(system, p.pos) : p.pos;
                int start = _vertices.Count;

                for (int k = 0; k < 4; k++)
                {
                    Vector3 vertex;
                    Vector4 uv1 = Vector4.zero, uv2;
                    switch (d.drawType)
                    {
                        case PEDrawType.Line:
                        {
                            bool head = k >= 2; // vertices 2, 3 at the position, 0, 1 at the last position
                            Vector3 last = local ? HplMatrix.Mul(system, p.lastPos) : p.lastPos;
                            vertex = HplSpace.Position(head ? pos : last);
                            Vector3 other = HplSpace.Position(head ? last : pos);
                            uv1 = new Vector4(other.x, other.y, other.z, head ? 1f : -1f);
                            uv2 = new Vector4(p.size.x, p.size.y, LineSign[k], 1f);
                            break;
                        }
                        case PEDrawType.Axis:
                        {
                            // vAdd = right·(±size.x) + forward·(±size.y), corners in the order of ParticleEmitter.cpp
                            Vector3 add = right * (k == 0 || k == 3 ? p.size.x : -p.size.x) +
                                          forward * (k <= 1 ? p.size.y : -p.size.y);
                            vertex = HplSpace.Position(pos + add);
                            uv2 = new Vector4(0f, 0f, 0f, 2f);
                            break;
                        }
                        default:
                        {
                            Vector2 add = Vector2.Scale(QuadCorner[k], p.size);
                            if (d.usePartSpin)
                            {
                                float cos = Mathf.Cos(p.spin), sin = Mathf.Sin(p.spin);
                                add = new Vector2(cos * add.x - sin * add.y, sin * add.x + cos * add.y);
                            }
                            vertex = HplSpace.Position(pos);
                            uv1 = new Vector4(add.x, add.y, 0f, 0f);
                            uv2 = Vector4.zero;
                            break;
                        }
                    }
                    _vertices.Add(vertex);
                    _uv.Add(HplSpace.Uv(uv[k]));
                    _uv1.Add(uv1);
                    _uv2.Add(uv2);
                    _colors.Add(p.color);
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }
                _indices.Add(start); _indices.Add(start + 1); _indices.Add(start + 2);
                _indices.Add(start + 2); _indices.Add(start + 3); _indices.Add(start);
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uv);
            _mesh.SetUVs(1, _uv1);
            _mesh.SetUVs(2, _uv2);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_indices, 0, false);
            float extent = Mathf.Max(_maxDrawSize.x, _maxDrawSize.y);
            var bounds = new Bounds();
            bounds.SetMinMax(min - Vector3.one * extent, max + Vector3.one * extent);
            _mesh.bounds = bounds;
        }

        /// <summary>The system's color and distance fade (iParticleEmitter::UpdateGraphicsForViewport's color mul).</summary>
        void SetSystemProperties()
        {
            var s = _system;
            _block.SetVector(ColorMulId, (Vector4)s.Color);
            Vector3 center = s.UnityWorldPosition;
            _block.SetVector(FadeCenterId, new Vector4(center.x, center.y, center.z, s.FadeAtDistance ? 1f : 0f));
            _block.SetVector(FadeDistId, new Vector4(s.MinFadeDistanceStart, s.MinFadeDistanceEnd, s.MaxFadeDistanceStart, s.MaxFadeDistanceEnd));
            _block.SetFloat(MulRgbByAlphaId, Data.multiplyRGBWithAlpha ? 1f : 0f);
            _renderer.SetPropertyBlock(_block);
        }

        void EnsureRenderer()
        {
            if (_renderer != null) return;
            _rendererObject = new GameObject($"{_system.Name} ({Data.name})") { hideFlags = HideFlags.DontSave };
            _rendererObject.transform.SetParent(HplParticleSystem.RendererRoot, false);
            _rendererObject.AddComponent<MeshFilter>().sharedMesh = _mesh = new Mesh { name = _rendererObject.name, hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _renderer = _rendererObject.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _renderer.allowOcclusionWhenDynamic = false;
            // No ray tracing pass in HplParticle: out of the acceleration structure (Hpl.HplRayTracing.Exclude).
            _renderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
            _block = new MaterialPropertyBlock();
        }

        public void DestroyGraphics()
        {
            if (_rendererObject == null) return;
            if (Application.isPlaying) Object.Destroy(_rendererObject);
            else Object.DestroyImmediate(_rendererObject);
            if (_mesh != null)
            {
                if (Application.isPlaying) Object.Destroy(_mesh);
                else Object.DestroyImmediate(_mesh);
            }
            _rendererObject = null;
            _renderer = null;
        }
    }
}
