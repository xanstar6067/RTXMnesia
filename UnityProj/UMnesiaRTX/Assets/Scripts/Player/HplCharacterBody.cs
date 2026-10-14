using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Player
{
    public enum CharDir { Forward = 0, Right = 1 }

    /// <summary>
    /// Port of HPL2 iCharacterBody (core/sources/physics/CharacterBody.cpp): a vertical cylinder that is
    /// not simulated but moved by per-direction speeds, gravity and forces and pushed out of the world
    /// after each move; small steps are climbed after ray tests and the camera sits at the top of the
    /// standing size, smoothed over the last steps. World space is Unity's, yaw is in radians and
    /// positive turns right. Call <see cref="Step"/> once per fixed update (HPL2 runs at 60 Hz).
    /// Not ported yet: gravity attachment (moving platforms), body connections (push interaction).
    /// </summary>
    public class HplCharacterBody : MonoBehaviour
    {
        [Header("Settings (game.cfg Player_Body)")]
        public float mass = 1f;
        public Vector3 gravity = new(0f, -9.8f, 0f);
        public bool gravityActive = true;
        public float maxGravitySpeed = 30f;
        [Tooltip("Degrees: on less steep slopes the character does not slide down.")]
        public float maxNoSlideSlopeAngle = 45f;
        public float maxStepHeight = 0.36f;
        public float maxStepHeightInAir = 0.36f;
        public float stepClimbSpeed = 1f;
        public bool accurateClimbing;
        public float groundFriction = 6f;
        public float airFriction = 0.3f;
        public float maxPushMass;
        public float pushForce;
        public int cameraSmoothPosNum;
        public bool testCollision = true;

        /// <summary>Camera offset: y up, x along <see cref="Right"/>, z along <see cref="Forward"/>.</summary>
        [NonSerialized] public Vector3 cameraPosAdd;

        // Fixed values of the iCharacterBody constructor.
        const float ClimbForwardMul = 1f;
        const float ClimbHeightAdd = 0.01f;
        const float CheckStepClimbInterval = 1f / 20f;
        const int MaxOnGroundCount = 12;
        const bool DeaccelerateMoveSpeedInAir = false;
        const float ShapeMargin = 0.01f;

        readonly List<Vector3> _sizes = new();
        readonly List<MeshCollider> _shapes = new();
        int _currentShape;
        Transform _shapeRoot;

        Vector3 _size;
        Vector3 _position, _lastPosition;
        float _yaw;
        Vector3 _forward = Vector3.forward, _right = Vector3.right;

        readonly float[] _maxPosSpeed = { 10f, 10f };
        readonly float[] _maxNegSpeed = { -10f, -10f };
        readonly float[] _moveSpeed = new float[2];
        readonly float[] _moveAcc = { 20f, 20f };
        readonly float[] _moveDeacc = { 20f, 20f };
        readonly float[] _currentMoveAcc = new float[2];
        readonly float[] _oppositeDirAccMul = { 1f, 1f };
        readonly bool[] _moving = new bool[2];

        Vector3 _force, _velocity, _lastGroundNormal;
        int _onGroundCount;
        bool _climbing;
        float _checkStepClimbCount;
        float _moveDelayCount;

        readonly List<Vector3> _cameraPositions = new();
        Vector3 _cameraPosition, _prevCameraPosition;

        HplPhysics.CollisionCallback _pushCallback, _gravityCallback;

        /// <summary>Ground contact during the gravity move: collider, contact normal, velocity before the contact.</summary>
        public event Action<Collider, Vector3, Vector3> GravityCollide;

        public Vector3 Size => _size;
        public Vector3 Position => _position;
        public Vector3 FeetPosition => _position - new Vector3(0f, _size.y * 0.5f, 0f);
        public Vector3 Forward => _forward;
        public Vector3 Right => _right;
        public float Radius => _size.x * 0.5f;
        public int ActiveSize => _currentShape;
        public bool IsOnGround => _onGroundCount > 0;
        public bool MovedLastUpdate { get; private set; }
        public bool IsClimbing => _climbing;
        /// <summary>Velocity from gravity, jumps and forces (iCharacterBody::GetForceVelocity).</summary>
        public Vector3 ForceVelocity { get => _velocity; set => _velocity = value; }
        /// <summary>The collider stood on during the last gravity move (for step sounds).</summary>
        public Collider GroundCollider { get; private set; }
        public IReadOnlyList<MeshCollider> Shapes => _shapes;
        public Vector3 CameraPosition => _cameraPosition;
        public float Mass => mass;

        public float Yaw
        {
            get => _yaw;
            set { _yaw = value; UpdateMoveMatrix(); }
        }

        /// <summary>Creates the main shape. Further sizes (crouch) are added with <see cref="AddExtraSize"/>.</summary>
        public void Init(Vector3 size)
        {
            _pushCallback = OnPushCollide;
            _gravityCallback = OnGravityCollide;

            _shapeRoot = new GameObject("CharacterShapes").transform;
            _shapeRoot.SetParent(transform, false);
            _shapeRoot.gameObject.layer = HplPhysics.CharacterLayer;

            _sizes.Clear();
            _shapes.Clear();
            AddExtraSize(size);
            _currentShape = 0;
            _shapes[0].isTrigger = false;
            _size = ShapeSize(0);
            SetPosition(transform.position + new Vector3(0f, _size.y * 0.5f, 0f));
        }

        /// <summary>A further size (inactive until <see cref="SetActiveSize"/>), e.g. crouching.</summary>
        public int AddExtraSize(Vector3 size)
        {
            float radius = Mathf.Max(size.x, size.z) * 0.5f;
            var shape = _shapeRoot.gameObject.AddComponent<MeshCollider>();
            shape.sharedMesh = CreateCylinderMesh(radius, size.y, 24);
            shape.convex = true;
            // Inactive sizes stay as query shapes only.
            shape.isTrigger = true;
            _sizes.Add(new Vector3(radius * 2f, size.y, radius * 2f));
            _shapes.Add(shape);
            return _shapes.Count - 1;
        }

        public void SetActiveSize(int index)
        {
            if (index == _currentShape) return;
            Vector3 feet = FeetPosition;
            _shapes[_currentShape].isTrigger = true;
            _currentShape = index;
            _shapes[_currentShape].isTrigger = false;
            _size = ShapeSize(index);
            SetPosition(feet + new Vector3(0f, _size.y * 0.5f, 0f), true);
        }

        public void SetPosition(Vector3 position, bool smooth = false)
        {
            _lastPosition = _position = position;
            UpdateBody();
            if (!smooth)
            {
                _cameraPositions.Clear();
                UpdateCamera();
                _prevCameraPosition = _cameraPosition;
            }
        }

        public void SetFeetPosition(Vector3 feet, bool smooth = false) =>
            SetPosition(feet + new Vector3(0f, _size.y * 0.5f, 0f), smooth);

        public void SetMaxPositiveMoveSpeed(CharDir dir, float x) => _maxPosSpeed[(int)dir] = x;
        public void SetMaxNegativeMoveSpeed(CharDir dir, float x) => _maxNegSpeed[(int)dir] = x;
        public void SetMoveAcc(CharDir dir, float x) => _moveAcc[(int)dir] = x;
        public void SetMoveDeacc(CharDir dir, float x) => _moveDeacc[(int)dir] = x;
        public void SetMoveOppositeDirAccMul(CharDir dir, float x) => _oppositeDirAccMul[(int)dir] = x;
        public float GetMoveSpeed(CharDir dir) => _moveSpeed[(int)dir];
        public void SetMoveSpeed(CharDir dir, float x) => _moveSpeed[(int)dir] = x;

        /// <summary>Accelerate in a direction this step (-1..1; several calls add up).</summary>
        public void Move(CharDir dir, float mul)
        {
            if (_moveDelayCount > 0f) return;
            _currentMoveAcc[(int)dir] += mul;
            _moving[(int)dir] = true;
        }

        public void StopMovement()
        {
            _velocity = Vector3.zero;
            for (int i = 0; i < 2; i++)
            {
                _currentMoveAcc[i] = 0f;
                _moving[i] = false;
                _moveAcc[i] = 0f;
                _moveSpeed[i] = 0f;
            }
        }

        public void SetMoveDelay(float seconds) => _moveDelayCount = seconds;

        public void AddForce(Vector3 force) => _force += force;
        public void SetForce(Vector3 force) => _force = force;

        /// <summary>Actual velocity of the last step (position change / step).</summary>
        public Vector3 GetVelocity(float timeStep) => timeStep <= 0f ? Vector3.zero : (_position - _lastPosition) / timeStep;

        /// <summary>Camera position between the last two steps, for rendering between fixed updates.</summary>
        public Vector3 InterpolatedCameraPosition(float alpha) => Vector3.LerpUnclamped(_prevCameraPosition, _cameraPosition, alpha);

        public void Step(float timeStep)
        {
            if (!isActiveAndEnabled) return;

            if (_moveDelayCount > 0f) _moveDelayCount -= timeStep;
            _prevCameraPosition = _cameraPosition;

            _lastPosition = _position;
            UpdateMoveMatrix();
            UpdateStepClimbing(timeStep);

            Vector3 posAdd = UpdatePositionFromCharSpeed(timeStep);
            AlignPosAddAccordingToGroundNormal(ref posAdd);

            if (testCollision) CheckMoveCollision(posAdd, timeStep);
            else _position += posAdd;

            UpdateForces(timeStep);
            if (testCollision) CheckForceCollision(timeStep);
            UpdateFriction(timeStep);

            UpdateBody();
            UpdateCamera();
        }

        /// <summary>Would the character (a size, at a center or feet position) be free of the world here?</summary>
        public bool CheckCharacterFits(Vector3 position, bool feetPosition = false, int sizeIndex = -1, float epsilon = 0f)
            => CheckCharacterFits(position, out _, feetPosition, sizeIndex, epsilon);

        public bool CheckCharacterFits(Vector3 position, out Vector3 pushBack, bool feetPosition = false, int sizeIndex = -1, float epsilon = 0f)
        {
            if (sizeIndex < 0) sizeIndex = _currentShape;
            if (feetPosition) position.y += _sizes[sizeIndex].y * 0.5f;
            CheckCollision(out pushBack, position, null, sizeIndex);
            Vector3 a = new(Mathf.Abs(pushBack.x), Mathf.Abs(pushBack.y), Mathf.Abs(pushBack.z));
            return epsilon == 0f ? a == Vector3.zero : a.x < epsilon && a.y < epsilon && a.z < epsilon;
        }

        public bool CheckRayIntersection(Vector3 start, Vector3 end, out float distance, out Vector3 normal) =>
            HplPhysics.Raycast(start, end, out distance, out normal, out _);

        // ------------------------------------------------------------------ movement

        void UpdateMoveMatrix()
        {
            _forward = new Vector3(Mathf.Sin(_yaw), 0f, Mathf.Cos(_yaw));
            _right = new Vector3(Mathf.Cos(_yaw), 0f, -Mathf.Sin(_yaw));
        }

        Vector3 UpdatePositionFromCharSpeed(float dt)
        {
            // Moving diagonally is not faster.
            float diagMul = _moving[0] && _moving[1] ? 0.7071f : 1f;

            MovedLastUpdate = false;
            for (int i = 0; i < 2; i++)
            {
                bool lowerSpeed = IsOnGround || DeaccelerateMoveSpeedInAir;
                if (!_moving[i])
                {
                    if (lowerSpeed)
                    {
                        if (_moveSpeed[i] > 0f) _moveSpeed[i] = Mathf.Max(0f, _moveSpeed[i] - _moveDeacc[i] * dt);
                        else _moveSpeed[i] = Mathf.Min(0f, _moveSpeed[i] + _moveDeacc[i] * dt);
                    }
                }
                else
                {
                    MovedLastUpdate = true;
                    float maxPos = _maxPosSpeed[i] * diagMul;
                    float maxNeg = _maxNegSpeed[i] * diagMul;
                    if (lowerSpeed)
                    {
                        if (_moveSpeed[i] > maxPos) _moveSpeed[i] = Mathf.Max(maxPos, _moveSpeed[i] - _moveDeacc[i] * dt);
                        else if (_moveSpeed[i] < maxNeg) _moveSpeed[i] = Mathf.Min(maxNeg, _moveSpeed[i] + _moveDeacc[i] * dt);
                    }
                    _moving[i] = false;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                float maxPos = _maxPosSpeed[i] * diagMul;
                float maxNeg = _maxNegSpeed[i] * diagMul;

                // Turning around on the ground accelerates faster.
                float accMul = 1f;
                if (IsOnGround && ((_currentMoveAcc[i] > 0f && _moveSpeed[i] < 0f) || (_currentMoveAcc[i] < 0f && _moveSpeed[i] > 0f)))
                    accMul = _oppositeDirAccMul[i];

                // Only accelerate below the max speed, so running speed fades out when walking again.
                float speedAdd = _currentMoveAcc[i] * _moveAcc[i] * accMul * dt;
                if (speedAdd > 0f && _moveSpeed[i] < maxPos) _moveSpeed[i] = Mathf.Min(maxPos, _moveSpeed[i] + speedAdd);
                else if (speedAdd < 0f && _moveSpeed[i] > maxNeg) _moveSpeed[i] = Mathf.Max(maxNeg, _moveSpeed[i] + speedAdd);

                _currentMoveAcc[i] = 0f;
            }

            return _forward * (_moveSpeed[0] * dt) + _right * (_moveSpeed[1] * dt);
        }

        void AlignPosAddAccordingToGroundNormal(ref Vector3 posAdd)
        {
            if (!IsOnGround || _velocity.y > 0f) return;
            if (_lastGroundNormal.sqrMagnitude < 0.01f) return;

            Vector3 bodyForward = posAdd.normalized;
            Vector3 right = Vector3.Cross(bodyForward, _lastGroundNormal);
            Vector3 forward = Vector3.Cross(_lastGroundNormal, right);
            posAdd = forward * posAdd.magnitude;
        }

        void CheckMoveCollision(Vector3 posAdd, float dt)
        {
            _position += posAdd;
            bool collided = CheckCollision(out Vector3 pushBack, _position, posAdd != Vector3.zero ? _pushCallback : null);
            if (collided)
            {
                _position += pushBack;

                // In the air, hitting an obstacle takes away the speed into it.
                if (!IsOnGround && pushBack.sqrMagnitude != 0f)
                {
                    Vector3 normal = pushBack.normalized;
                    ClampSpeedAlongWall(0, _forward, normal);
                    ClampSpeedAlongWall(1, _right, normal);
                }
            }
            CheckStepClimbing(posAdd, dt);
        }

        // Kept as in CharacterBody.cpp, including its dangling else: only positive speeds are reduced.
        void ClampSpeedAlongWall(int i, Vector3 axis, Vector3 normal)
        {
            if (_moveSpeed[i] == 0f) return;
            Vector3 v = axis * _moveSpeed[i];
            v -= normal * Vector3.Dot(normal, v);
            float speed = v.magnitude;
            if (_moveSpeed[i] > 0f)
            {
                if (_moveSpeed[i] > speed) _moveSpeed[i] = speed;
                else if (_moveSpeed[i] < speed) _moveSpeed[i] = -speed;
            }
        }

        void CheckStepClimbing(Vector3 posAdd, float dt)
        {
            if (_checkStepClimbCount > 0f) return;
            if (posAdd.sqrMagnitude < 1e-6f) return;

            float radius = Radius;
            float forwardAdd = Mathf.Max(posAdd.magnitude, 0.05f);
            Vector3 moveDir = posAdd.normalized;

            int rays = accurateClimbing ? 3 : 1;
            Span<Vector3> stepAdd = stackalloc Vector3[3];
            stepAdd[0] = moveDir * (radius + forwardAdd);
            if (accurateClimbing)
            {
                stepAdd[1] = Quaternion.Euler(0f, 45f, 0f) * moveDir * radius + moveDir * forwardAdd;
                stepAdd[2] = Quaternion.Euler(0f, -45f, 0f) * moveDir * radius + moveDir * forwardAdd;
            }

            bool firmlyOnGround = _onGroundCount > MaxOnGroundCount - 4;
            float maxHeight = firmlyOnGround || _climbing ? maxStepHeight : maxStepHeightInAir;

            // Rays from the middle of the body down to the feet: steps up to half the height can be seen.
            for (int i = 0; i < rays; i++)
            {
                Vector3 start = _position + stepAdd[i];
                Vector3 end = start - new Vector3(0f, _size.y * 0.5f, 0f);
                if (!HplPhysics.Raycast(start, end, out float distance, out _, out _)) continue;

                float height = _size.y * 0.5f - distance;
                if (height <= maxHeight && height > 0.025f)
                {
                    Vector3 stepPos = _position + new Vector3(0f, height + ClimbHeightAdd, 0f) + moveDir * (forwardAdd * ClimbForwardMul);
                    if (CheckCharacterFits(stepPos))
                    {
                        _position.y += stepClimbSpeed * dt;
                        _climbing = true;
                        break;
                    }
                }
            }
            _checkStepClimbCount = CheckStepClimbInterval;
        }

        void UpdateStepClimbing(float dt)
        {
            if (_climbing)
            {
                _checkStepClimbCount = 0f;
                _onGroundCount = MaxOnGroundCount;
            }
            else _checkStepClimbCount -= dt;
            _climbing = false;
        }

        void UpdateForces(float dt)
        {
            if (gravityActive && !_climbing)
            {
                _velocity += gravity * dt;
                float length = _velocity.magnitude;
                if (length > maxGravitySpeed) _velocity = _velocity / length * maxGravitySpeed;
            }
            _velocity += _force * (dt / mass);
            _force = Vector3.zero;
        }

        void UpdateFriction(float dt)
        {
            float friction = IsOnGround ? groundFriction : airFriction;
            Vector3 xz = new(_velocity.x, 0f, _velocity.z);
            float speed = Mathf.Max(0f, xz.magnitude - friction * dt);
            xz = xz.normalized * speed;
            _velocity.x = xz.x;
            _velocity.z = xz.z;
        }

        void CheckForceCollision(float dt)
        {
            Vector3 posAdd = _velocity * dt;
            Vector3 newVelocity = Vector3.zero;

            // XZ
            if (posAdd.x != 0f || posAdd.z != 0f)
            {
                _position.x += posAdd.x;
                _position.z += posAdd.z;
                if (CheckCollision(out Vector3 pushBack, _position, _pushCallback) && pushBack != Vector3.zero)
                {
                    _position += pushBack;
                    // Slide along without bounce.
                    Vector3 normal = pushBack.normalized;
                    Vector3 xzVel = new(_velocity.x, 0f, _velocity.z);
                    newVelocity += xzVel - normal * Vector3.Dot(normal, xzVel);
                }
                else
                {
                    newVelocity.x += _velocity.x;
                    newVelocity.z += _velocity.z;
                }
            }

            // Y
            if (posAdd.y != 0f)
            {
                Vector3 oldPos = _position;
                _position.y += posAdd.y;

                GroundCollider = null;
                if (CheckCollision(out Vector3 pushBack, _position, _gravityCallback) && pushBack != Vector3.zero)
                {
                    Vector3 normal = pushBack.normalized;
                    _lastGroundNormal = normal.y <= 0.2f ? Vector3.up : normal;

                    // Pushed mostly up while moving: on ground. Otherwise count down.
                    if (_velocity.y != 0f && normal.y >= 0.001f) _onGroundCount = MaxOnGroundCount;
                    else _onGroundCount = Mathf.Max(0, _onGroundCount - 1);

                    _position += pushBack;

                    // Slopes below the no-slide angle count as flat ground.
                    Vector3 up = -gravity.normalized;
                    if (Vector3.Dot(normal, up) >= Mathf.Cos(maxNoSlideSlopeAngle * Mathf.Deg2Rad)) normal = up;

                    Vector3 yVel = new(0f, _velocity.y, 0f);
                    Vector3 newVelAdd = yVel - normal * Vector3.Dot(normal, yVel);
                    newVelocity += newVelAdd;

                    // No velocity left: stay at the old position (no slope sliding).
                    if (newVelAdd.magnitude < 0.0001f) _position = oldPos;
                }
                else
                {
                    _onGroundCount = Mathf.Max(0, _onGroundCount - 1);
                    newVelocity.y += _velocity.y;

                    // Still counted as on ground: get the ground normal with a ray.
                    if (_onGroundCount > 0 && !_climbing)
                    {
                        Vector3 start = FeetPosition + new Vector3(0f, 0.001f, 0f);
                        Vector3 end = start - new Vector3(0f, _size.x * 2.0001f, 0f);
                        _lastGroundNormal = HplPhysics.Raycast(start, end, out _, out Vector3 n, out _) && n.y > 0.2f ? n : Vector3.up;
                    }
                }
            }

            _velocity = newVelocity;
        }

        bool CheckCollision(out Vector3 pushBack, Vector3 position, HplPhysics.CollisionCallback callback, int shapeIndex = -1)
        {
            if (shapeIndex < 0) shapeIndex = _currentShape;
            Vector3 size = _sizes[shapeIndex];
            Vector3 halfExtents = size * 0.5f + new Vector3(ShapeMargin, ShapeMargin, ShapeMargin);
            return HplPhysics.CheckShapeWorldCollision(_shapes[shapeIndex], position, Quaternion.identity, halfExtents, out pushBack, callback);
        }

        // cCharacterBodyCollidePush: push light bodies the character walks into (not ones below the feet).
        void OnPushCollide(Collider other, Vector3 normal, float depth)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || rb.isKinematic || rb.mass > maxPushMass) return;

            Vector3 point = other.ClosestPoint(_position);
            if (point.y < FeetPosition.y + 0.01f) return;

            Vector3 dir = point - _position;
            dir.y = 0f;
            rb.AddForceAtPosition(dir.normalized * pushForce, point);
        }

        // cCharacterBodyCollideGravity: remember the ground and press dynamic bodies below with the weight.
        void OnGravityCollide(Collider other, Vector3 normal, float depth)
        {
            GroundCollider = other;
            GravityCollide?.Invoke(other, normal, _velocity);

            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || rb.isKinematic || normal.y <= 0.001f) return;
            var body = HplPhysics.BodyOf(other);
            if (body != null && !body.pushedByCharacterGravity) return;
            if (gravity.y < 0f) rb.AddForceAtPosition(new Vector3(0f, mass * gravity.y, 0f), other.ClosestPoint(FeetPosition));
        }

        void UpdateBody()
        {
            if (_shapeRoot == null) return;
            transform.SetPositionAndRotation(FeetPosition, Quaternion.Euler(0f, _yaw * Mathf.Rad2Deg, 0f));
            _shapeRoot.SetPositionAndRotation(_position, Quaternion.identity);
        }

        void UpdateCamera()
        {
            if (_sizes.Count == 0) return;
            // The head is at the top of the main (standing) size; crouching lowers it by a head offset.
            Vector3 headPos = _position + new Vector3(0f, _sizes[0].y - _size.y * 0.5f, 0f);
            if (cameraSmoothPosNum > 0)
            {
                _cameraPositions.Add(headPos);
                if (_cameraPositions.Count > cameraSmoothPosNum) _cameraPositions.RemoveAt(0);
                Vector3 sum = Vector3.zero;
                foreach (var p in _cameraPositions) sum += p;
                headPos = sum / _cameraPositions.Count;
            }
            _cameraPosition = headPos + new Vector3(0f, cameraPosAdd.y, 0f) + _right * cameraPosAdd.x + _forward * cameraPosAdd.z;
        }

        Vector3 ShapeSize(int index) => _sizes[index];

        /// <summary>Closed cylinder around the origin, axis Y (only its hull matters for the convex collider).</summary>
        public static Mesh CreateCylinderMesh(float radius, float height, int segments)
        {
            var vertices = new Vector3[segments * 2 + 2];
            var triangles = new List<int>();
            float h = height * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var p = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                vertices[i] = p + Vector3.up * h;
                vertices[segments + i] = p - Vector3.up * h;
            }
            int top = segments * 2, bottom = top + 1;
            vertices[top] = Vector3.up * h;
            vertices[bottom] = -Vector3.up * h;
            for (int i = 0; i < segments; i++)
            {
                int j = (i + 1) % segments;
                triangles.AddRange(new[] { i, j, segments + i, j, segments + j, segments + i });
                triangles.AddRange(new[] { top, j, i });
                triangles.AddRange(new[] { bottom, segments + i, segments + j });
            }
            var mesh = new Mesh { name = $"CharacterCylinder_{radius:0.##}x{height:0.##}", vertices = vertices, triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDrawGizmosSelected()
        {
            if (_sizes.Count == 0) return;
            Gizmos.color = IsOnGround ? Color.green : Color.yellow;
            Gizmos.DrawWireMesh(_shapes[_currentShape].sharedMesh, _position);
        }
    }
}
