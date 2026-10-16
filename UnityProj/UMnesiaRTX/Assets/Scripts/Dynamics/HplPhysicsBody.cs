using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// A rigid body of an entity (port of iPhysicsBody / cPhysicsBodyNewton, PhysicsBody.cpp, PhysicsBodyNewton.cpp)
    /// over a Unity Rigidbody on the <see cref="AmnesiaBody"/> object. Positions and velocities are Unity space;
    /// the Hpl* members give HPL2 space for ports that compute there (<see cref="HplSpace"/>).
    ///
    /// - Mass 0 is a static body: kinematic, moved only by <see cref="StaticLinearMove"/> / <see cref="StaticAngularMove"/>
    ///   (velocity for this step, applied around the simulation) or by <see cref="SetPose"/>.
    /// - Forces and torques collect until the next simulation step, like the Newton force callback, which also adds
    ///   gravity (world gravity, -9.81 as Newton's default; Unity's project gravity is the same) and clamps speeds.
    /// - Newton damping (dgBody: velocity -= velocity · 0.02 · damping per 1/60 s step, damping clamped to
    ///   [0.001, 1] by HPL2) becomes Unity damping 0.02 · damping · 60.
    /// </summary>
    public sealed class HplPhysicsBody
    {
        const float NewtonDampingPerStep = 0.02f;
        const float NewtonStep = 1f / 60f;

        public readonly string Name;
        public readonly AmnesiaBody Data;
        public readonly Rigidbody Rigidbody;
        public readonly Collider[] Colliders;
        /// <summary>Its contact messages (impacts of props, surface sounds).</summary>
        public readonly HplBodyContacts Contacts;
        /// <summary>Scrape / roll sound state (HplSurfaceSounds).</summary>
        public readonly HplSurfaceSounds.State SurfaceState = new();
        /// <summary>Creation order: a contact pair of two bodies is handled once, by the earlier one.</summary>
        public readonly int Order = _orderCount++;
        static int _orderCount;
        public readonly HplPhysicsWorld World;
        /// <summary>The script entity of the body (HplProp), like iPhysicsBody::GetUserData.</summary>
        public object UserData;
        public readonly List<HplJoint> Joints = new();
        /// <summary>Pose relative to the entity at load (cEntityBodyExtraData::m_mtxLocalTransform).</summary>
        public readonly Vector3 LoadLocalPosition;
        public readonly Quaternion LoadLocalRotation;
        public readonly HplSurfaces.Surface Surface;

        float _mass;
        bool _gravity, _collide = true, _active = true, _collideCharacter, _continuous;
        Vector3 _staticLinearVel, _staticAngularVel;
        bool _staticMovement, _disableAfterSimulation;
        readonly int[] _layers;

        public HplPhysicsBody(HplPhysicsWorld world, string name, AmnesiaBody data, HplSurfaces surfaces, bool interpolate)
        {
            World = world;
            Name = name;
            Data = data;
            Colliders = data.GetComponentsInChildren<Collider>(true);
            _layers = new int[Colliders.Length];
            for (int i = 0; i < Colliders.Length; i++) _layers[i] = Colliders[i].gameObject.layer;
            LoadLocalPosition = data.transform.localPosition;
            LoadLocalRotation = data.transform.localRotation;
            Surface = surfaces != null ? surfaces.GetOrDefault(data.material) : null;

            if (!data.TryGetComponent(out Rigidbody)) Rigidbody = data.gameObject.AddComponent<Rigidbody>();
            Rigidbody.automaticCenterOfMass = true;
            Rigidbody.automaticInertiaTensor = true;
            Rigidbody.interpolation = interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
            Rigidbody.maxAngularVelocity = 100f;
            Rigidbody.solverIterations = 12;
            Rigidbody.solverVelocityIterations = 4;
            _continuous = data.continuousCollision;
            Contacts = data.GetComponent<HplBodyContacts>();
            if (Contacts == null) Contacts = data.gameObject.AddComponent<HplBodyContacts>();
            Contacts.Body = this;
            SetLinearDamping(data.linearDamping);
            SetAngularDamping(data.angularDamping);
            Gravity = data.hasGravity;
            Mass = data.mass;
            _collideCharacter = data.collideCharacter;
            Collide = data.collideNonCharacter;
        }

        // ------------------------------------------------------------------ properties

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                bool kinematic = value <= 0f;
                // Continuous collision (ContinuousCollision in .ent): a sweep for moving bodies. Speculative
                // contacts miss thin floors under fast spinning pieces (a broken pot inherits the throw speed);
                // kinematic bodies only support the speculative mode.
                if (kinematic) Rigidbody.collisionDetectionMode = _continuous ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Discrete;
                if (Rigidbody.isKinematic != kinematic) Rigidbody.isKinematic = kinematic;
                if (!kinematic) Rigidbody.collisionDetectionMode = _continuous ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
                if (!kinematic) Rigidbody.mass = value;
                Rigidbody.useGravity = _gravity && !kinematic;
            }
        }

        public bool Gravity
        {
            get => _gravity;
            set
            {
                _gravity = value;
                Rigidbody.useGravity = value && _mass > 0f;
            }
        }

        public bool IsStatic => _mass <= 0f;

        /// <summary>The character collides with the body (queries read <see cref="AmnesiaBody.collideCharacter"/>).</summary>
        public bool CollideCharacter
        {
            get => _collideCharacter;
            set
            {
                _collideCharacter = value;
                Data.collideCharacter = value;
                World.UpdateCharacterCollision(this);
            }
        }

        /// <summary>SetCollide: false collides with nothing but characters (no contacts, still blocks the player).</summary>
        public bool Collide
        {
            get => _collide;
            set
            {
                _collide = value;
                for (int i = 0; i < Colliders.Length; i++)
                    if (Colliders[i] != null) Colliders[i].gameObject.layer = value ? _layers[i] : HplPhysicsWorld.NoCollideLayer;
            }
        }

        /// <summary>iPhysicsBody::SetActive: an inactive body neither moves nor collides.</summary>
        public bool Active
        {
            get => _active && Data.gameObject.activeInHierarchy;
            set
            {
                _active = value;
                Rigidbody.detectCollisions = value;
                if (!value && !Rigidbody.isKinematic)
                {
                    Rigidbody.linearVelocity = Vector3.zero;
                    Rigidbody.angularVelocity = Vector3.zero;
                }
                foreach (var c in Colliders) if (c != null) c.enabled = value;
            }
        }

        /// <summary>Awake (not frozen by Newton's auto sleep / PhysX sleeping).</summary>
        public bool Enabled => !Rigidbody.isKinematic ? !Rigidbody.IsSleeping() : _staticMovement;

        public Vector3 Position => Rigidbody.position;
        public Quaternion Rotation => Rigidbody.rotation;
        public Vector3 LinearVelocity
        {
            get => Rigidbody.isKinematic ? _staticLinearVel : Rigidbody.linearVelocity;
            set { if (!Rigidbody.isKinematic) Rigidbody.linearVelocity = value; }
        }
        public Vector3 AngularVelocity
        {
            get => Rigidbody.isKinematic ? _staticAngularVel : Rigidbody.angularVelocity;
            set { if (!Rigidbody.isKinematic) Rigidbody.angularVelocity = value; }
        }

        /// <summary>World center of mass.</summary>
        public Vector3 WorldCenterOfMass => Rigidbody.worldCenterOfMass;

        public void SetLinearDamping(float damping) =>
            Rigidbody.linearDamping = Mathf.Clamp(damping, 0.001f, 1f) * NewtonDampingPerStep / NewtonStep;

        public void SetAngularDamping(float damping) =>
            Rigidbody.angularDamping = Mathf.Clamp(damping, 0.001f, 1f) * NewtonDampingPerStep / NewtonStep;

        /// <summary>World inertia tensor (I = R·diag·Rᵀ), as iPhysicsBody::GetInertiaMatrix.</summary>
        public Matrix4x4 InertiaMatrix
        {
            get
            {
                if (Rigidbody.isKinematic) return Matrix4x4.zero;
                var r = Matrix4x4.Rotate(Rigidbody.rotation * Rigidbody.inertiaTensorRotation);
                var d = Matrix4x4.Scale(Rigidbody.inertiaTensor);
                return r * d * r.transpose;
            }
        }

        // ------------------------------------------------------------------ HPL2 space

        public Vector3 HplPosition => HplSpace.Position(Position);
        /// <summary>The body matrix in HPL2 space (iPhysicsBody::GetLocalMatrix).</summary>
        public Matrix4x4 HplMatrix => HplSpace.ToHpl(Position, Rotation);
        public Vector3 HplLinearVelocity => HplSpace.Direction(LinearVelocity);
        public Vector3 HplAngularVelocity => HplSpace.Axial(AngularVelocity);
        public Vector3 HplWorldCenterOfMass => HplSpace.Position(WorldCenterOfMass);
        /// <summary>I_hpl = S·I·S (the tensor maps axial vectors to axial vectors).</summary>
        public Matrix4x4 HplInertiaMatrix => HplSpace.Mirror(InertiaMatrix);

        public void AddForceHpl(Vector3 f) => AddForce(HplSpace.Direction(f));
        public void AddTorqueHpl(Vector3 t) => AddTorque(HplSpace.Axial(t));
        public void AddImpulseHpl(Vector3 i) => AddImpulse(HplSpace.Direction(i));
        public void AddForceAtPositionHpl(Vector3 f, Vector3 p) => AddForceAtPosition(HplSpace.Direction(f), HplSpace.Position(p));
        public void AddImpulseAtPositionHpl(Vector3 i, Vector3 p) => AddImpulseAtPosition(HplSpace.Direction(i), HplSpace.Position(p));
        public Vector3 HplVelocityAtPosition(Vector3 hplPos) => HplSpace.Direction(VelocityAtPosition(HplSpace.Position(hplPos)));
        public void SetHplLinearVelocity(Vector3 v) => LinearVelocity = HplSpace.Direction(v);
        public void SetHplAngularVelocity(Vector3 v) => AngularVelocity = HplSpace.Axial(v);

        // ------------------------------------------------------------------ actions

        public void AddForce(Vector3 force)
        {
            if (Rigidbody.isKinematic || !Active) return;
            Rigidbody.AddForce(force, ForceMode.Force);
        }

        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            if (Rigidbody.isKinematic || !Active) return;
            Rigidbody.AddForceAtPosition(force, position, ForceMode.Force);
        }

        public void AddTorque(Vector3 torque)
        {
            if (Rigidbody.isKinematic || !Active) return;
            Rigidbody.AddTorque(torque, ForceMode.Force);
        }

        /// <summary>
        /// cPhysicsBodyNewton::AddImpulse → NewtonBodyAddImpulse, whose argument is the wanted change of velocity at the
        /// point (Newton.h: "pointDeltaVeloc"), not an impulse: at the center of mass the velocity changes by it
        /// whatever the mass (a thrown prop leaves at GrabThrowImpulse m/s).
        /// </summary>
        public void AddImpulse(Vector3 deltaVelocity)
        {
            if (Rigidbody.isKinematic || !Active) return;
            Rigidbody.AddForce(deltaVelocity, ForceMode.VelocityChange);
        }

        /// <summary>
        /// NewtonBodyAddImpulse at a point: a velocity change of the point. Approximated as the impulse mass × change
        /// applied there (it also turns the body).
        /// </summary>
        public void AddImpulseAtPosition(Vector3 deltaVelocity, Vector3 position)
        {
            if (Rigidbody.isKinematic || !Active) return;
            Rigidbody.AddForceAtPosition(deltaVelocity * Rigidbody.mass, position, ForceMode.Impulse);
        }

        public Vector3 VelocityAtPosition(Vector3 position) =>
            Rigidbody.isKinematic ? _staticLinearVel + Vector3.Cross(_staticAngularVel, position - Position) : Rigidbody.GetPointVelocity(position);

        public void WakeUp()
        {
            if (!Rigidbody.isKinematic) Rigidbody.WakeUp();
        }

        /// <summary>Static bodies: move with this velocity during the next step (iPhysicsBody::StaticLinearMove).</summary>
        public void StaticLinearMove(Vector3 velocity)
        {
            if (!IsStatic) return;
            _staticLinearVel += velocity;
            _staticMovement = true;
        }

        public void StaticAngularMove(Vector3 velocity)
        {
            if (!IsStatic) return;
            _staticAngularVel += velocity;
            _staticMovement = true;
        }

        /// <summary>Teleport (iPhysicsBody::SetMatrix).</summary>
        public void SetPose(Vector3 position, Quaternion rotation)
        {
            Rigidbody.position = position;
            Rigidbody.rotation = rotation;
            Data.transform.SetPositionAndRotation(position, rotation);
        }

        public void SetHplMatrix(Matrix4x4 hpl)
        {
            var m = HplSpace.Mirror(hpl);
            SetPose(m.GetPosition(), m.rotation);
        }

        public void DisableAfterSimulation() => _disableAfterSimulation = true;

        // ------------------------------------------------------------------ buoyancy

        /// <summary>iPhysicsBody::SetBuoyancyActive and the buoyancy data (cPhysicsBody_Buoyancy) set by a liquid area.</summary>
        public bool BuoyancyActive { get; set; }
        /// <summary>The height of the surface plane (normal up).</summary>
        public float BuoyancySurfaceY { get; set; }
        public float BuoyancyDensity { get; set; } = 1f;
        // Stored as in HPL2; Newton's clamps make the damping independent of them (HplBuoyancy).
        public float BuoyancyLinearViscosity { get; set; } = 1f;
        public float BuoyancyAngularViscosity { get; set; } = 1f;

        // ------------------------------------------------------------------ simulation

        /// <summary>The Newton force callback and iPhysicsBody::UpdateBeforeSimulate.</summary>
        Vector3 _previousLinear, _previousAngular, _previousCenter;

        /// <summary>The velocity of a point of the body before the last simulation step.</summary>
        public Vector3 PreviousVelocityAt(Vector3 point) => _previousLinear + Vector3.Cross(_previousAngular, point - _previousCenter);

        internal void BeforeSimulate(float timeStep)
        {
            _previousLinear = LinearVelocity;
            _previousAngular = AngularVelocity;
            _previousCenter = Rigidbody.worldCenterOfMass;
            if (!Active) return;
            if (!Rigidbody.isKinematic)
            {
                // cPhysicsBodyNewton::OnUpdateCallback: buoyancy (BuoyancyDensityMul of the .ent body > 0).
                if (BuoyancyActive && Data.buoyancyDensityMul > 0f)
                    HplBuoyancy.Apply(this, BuoyancySurfaceY, BuoyancyDensity * Data.buoyancyDensityMul);
                // It compares the length with the squared max speed and scales by
                // its square root; kept.
                if (Data.maxLinearSpeed > 0f)
                {
                    Vector3 v = Rigidbody.linearVelocity;
                    float length = v.magnitude;
                    if (length > Data.maxLinearSpeed * Data.maxLinearSpeed)
                        Rigidbody.linearVelocity = v / Mathf.Sqrt(length) * Data.maxLinearSpeed;
                }
                if (Data.maxAngularSpeed > 0f)
                {
                    Vector3 v = Rigidbody.angularVelocity;
                    float length = v.magnitude;
                    if (length > Data.maxAngularSpeed * Data.maxAngularSpeed)
                        Rigidbody.angularVelocity = v / Mathf.Sqrt(length) * Data.maxAngularSpeed;
                }
                return;
            }

            if (!_staticMovement) return;
            // UpdateAfterSimulate moves the static body by its velocity; the kinematic move does it in the step so
            // contacts see the motion. Rotation as MatrixRotate(angular velocity · dt) · R in HPL2 space.
            Vector3 position = Rigidbody.position + _staticLinearVel * timeStep;
            Quaternion rotation = Rigidbody.rotation;
            if (_staticAngularVel != Vector3.zero)
            {
                Matrix4x4 rotH = HplSpace.Mirror(Matrix4x4.Rotate(rotation));
                rotH = Hpl.HplMatrix.RotateXYZ(HplSpace.Axial(_staticAngularVel) * timeStep) * rotH;
                rotation = HplSpace.Mirror(rotH).rotation;
            }
            Rigidbody.MovePosition(position);
            Rigidbody.MoveRotation(rotation);
            World.WakeBodiesNear(this);
        }

        internal void AfterSimulate(float timeStep)
        {
            if (_staticMovement)
            {
                if (_staticLinearVel == Vector3.zero && _staticAngularVel == Vector3.zero) _staticMovement = false;
                _staticLinearVel = Vector3.zero;
                _staticAngularVel = Vector3.zero;
            }
            if (_disableAfterSimulation)
            {
                _disableAfterSimulation = false;
                LinearVelocity = Vector3.zero;
                AngularVelocity = Vector3.zero;
            }
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var c in Colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (any) bounds.Encapsulate(c.bounds);
                else bounds = c.bounds;
                any = true;
            }
            return any;
        }
    }
}
