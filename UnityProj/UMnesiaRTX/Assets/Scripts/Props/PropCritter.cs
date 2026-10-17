using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// iLuxProp_CritterBase (LuxProp_CritterBase.cpp): a small creature on a static body that moves itself — no physics
    /// simulation, the body is placed every step. Only updated within 15 m of the player's feet. The velocity of the
    /// type moves the body (rays for bugs, the shape for the spider), the mesh turns towards the averaged movement
    /// (last 20 directions) on the averaged ground normal (last 40), the animation goes Idle ↔ Walk by speed (Walk
    /// speed = the speed). Death: sound, particle system, death entity, destroyed. No interaction. All the math is in
    /// HPL2 space.
    /// </summary>
    public abstract class PropCritterBase : HplProp
    {
        // .ent data (iLuxPropLoader_Critter::LoadVariables)
        string _deathEntity = "", _deathPS = "", _deathSound = "";
        protected float AttackSizeRadius;
        protected Vector3 AttackOffset;
        protected Vector2 AttackDamageMinMax, AttackForce;
        protected int AttackStrength;
        string _attackHitSound = "";
        Matrix4x4 _meshOffset = Matrix4x4.identity;   // HPL2

        /// <summary>mbUseRayCollision: bugs move by rays, the spider by its shape.</summary>
        protected bool UseRayCollision = true;
        protected bool CausesSanityDecreaseValue;

        // Saved like iLuxProp_CritterBase_SaveData (simple fields).
        protected Vector3 Vel;                  // HPL2
        protected Vector3 GravityVel;           // HPL2
        protected Vector3 SwarmPoint;           // HPL2
        protected int AnimState;
        protected Vector3 GroundNormal = new(0f, 1f, 0f);   // HPL2
        protected bool Colliding;
        protected bool UpdateAnimationEnabled = true;

        /// <summary>Not saved (a readonly holder): the mesh setup and the smoothing lists, as in HPL2.</summary>
        protected sealed class Runtime
        {
            public readonly List<Transform> subMeshes = new();
            public readonly List<Matrix4x4> baseMatrices = new();   // Unity, relative to the body (mvBaseMatrices)
            public Transform skinnedModel;
            public Matrix4x4 skinnedCorrection = Matrix4x4.identity; // Unity import correction under the HPL2 mesh matrix
            public readonly LinkedList<Vector3> fwdDirs = new();
            public readonly LinkedList<Vector3> upDirs = new();
            public Vector3 bodySize;                                 // HPL2 bounding volume size (no rotation)
            public float shapeRadius;
            public Collider shape;
        }
        protected readonly Runtime Rt = new();
        protected readonly CritterRay RayCb;

        protected PropCritterBase(World.AmnesiaEntity entity) : base(entity, HplEntityType.Prop)
        {
            RayCb = new CritterRay(this);
        }

        public bool CausesSanityDecrease => CausesSanityDecreaseValue;
        public Vector3 HplVelocity => Vel;
        public Vector3 HplGravityVelocity => GravityVel;
        public Vector3 HplGroundNormal => GroundNormal;
        public HplPhysicsBody CritterBody => PhysicsBodies.Count > 0 ? PhysicsBodies[0] : null;

        protected override void LoadVariables()
        {
            _deathEntity = VarString("DeathEntity", "");
            _deathPS = VarString("DeathPS", "");
            _deathSound = VarString("DeathSound", "");

            AttackSizeRadius = VarFloat("AttackSizeRadius", 0f);
            AttackOffset = VarVector3("AttackOffset", Vector3.zero);
            AttackDamageMinMax = VarVector2("AttackDamageMinMax", Vector2.zero);
            AttackForce = VarVector2("AttackForce", Vector2.zero);
            AttackStrength = VarInt("AttackStrength", 0);
            _attackHitSound = VarString("AttackHitSound", "");

            Vector3 offsetPos = VarVector3("MeshOffsetPos", Vector3.zero);
            Vector3 offsetRot = VarVector3("MeshOffsetRot", Vector3.zero) * Mathf.Deg2Rad;
            Vector3 offsetScale = VarVector3("MeshOffsetScale", Vector3.one);
            _meshOffset = HplMatrix.SetTranslation(HplMatrix.RotateXYZ(offsetRot) * Matrix4x4.Scale(offsetScale), offsetPos);

            LoadCritterVariables();
        }

        protected abstract void LoadCritterVariables();

        /// <summary>GetVarVector2f: "x y" (a missing value is 0).</summary>
        protected Vector2 VarVector2(string name, Vector2 fallback)
        {
            string s = VarString(name, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            var parts = s.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var v = Vector2.zero;
            for (int i = 0; i < 2 && i < parts.Length; i++) v[i] = HplMath.Atof(parts[i]);
            return v;
        }

        public override bool CanInteract(HplPhysicsBody body) => false;

        /// <summary>iLuxProp_CritterBase::OnSetupAfterLoad.</summary>
        protected override void OnSetupAfterLoad()
        {
            var body = CritterBody;
            if (body == null) return;
            Transform bodyT = body.Data.transform;

            // The mesh: a skinned model (posed as a whole) or the sub meshes (each keeps its local matrix).
            if (AnimatedModel != null)
            {
                Rt.skinnedModel = AnimatedModel.transform;
                // The import correction of the model root relative to the entity (HPL2 sets the mesh matrix outright).
                Rt.skinnedCorrection = GameObject.transform.worldToLocalMatrix * Rt.skinnedModel.localToWorldMatrix;
                if (Rt.skinnedModel.parent != bodyT) Rt.skinnedModel.SetParent(bodyT, true);
            }
            else
            {
                foreach (var f in bodyT.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (f.transform.parent != bodyT) continue;
                    Rt.subMeshes.Add(f.transform);
                    Rt.baseMatrices.Add(Matrix4x4.TRS(f.transform.localPosition, f.transform.localRotation, f.transform.localScale));
                }
            }

            SwarmPoint = body.HplPosition;
            // Play(0) is already done by the base (SetupAnimation).

            Matrix4x4 rotation = HplMatrix.GetRotation(body.HplMatrix);
            GroundNormal = HplMatrix.Mul3x3(rotation, GroundNormal);
            Vector3 forward = HplMatrix.Mul3x3(rotation, new Vector3(0f, 0f, -1f));
            CreateOrthoVectors(out _, ref GroundNormal, ref forward);
            Rt.fwdDirs.AddLast(forward);

            // Remove any rotation from the body!
            body.SetHplMatrix(Matrix4x4.Translate(body.HplPosition));

            // The bounding volume of the unrotated shape and the shape's radius.
            foreach (var c in body.Colliders)
            {
                if (c == null) continue;
                Rt.shape ??= c;
            }
            Physics.SyncTransforms();
            if (body.TryGetBounds(out var bounds)) Rt.bodySize = bounds.size;
            if (Rt.shape is SphereCollider sphere)
            {
                Vector3 s = sphere.transform.lossyScale;
                Rt.shapeRadius = sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            }
            else Rt.shapeRadius = Mathf.Max(Rt.bodySize.x, Rt.bodySize.z) * 0.5f;
        }

        protected override void UpdatePropSpecific(float timeStep)
        {
            var body = CritterBody;
            var player = Map.player;
            if (body == null || player == null || player.Body == null) return;
            // If the player is far away do not update.
            Vector3 playerFeet = HplSpace.Position(player.Body.FeetPosition);
            if ((playerFeet - body.HplPosition).sqrMagnitude > 15f * 15f) return;

            UpdateCritterSpecific(timeStep);
            UpdateMovement(timeStep);
            UpdateMesh();
        }

        protected abstract void UpdateCritterSpecific(float timeStep);
        protected virtual void OnShapeCollision(Vector3 pushVec, float timeStep) { }
        protected virtual void OnDamageCritter(float amount) { }
        protected virtual void OnKillCritter() { }

        /// <summary>iLuxProp_CritterBase::OnHealthChange: death.</summary>
        protected override void OnHealthChange()
        {
            if (Health > 0f || DestroyMe) return;
            var body = CritterBody;
            if (_deathSound != "") PlaySound("CritterDeath", _deathSound, true, true);
            if (_deathPS != "" && body != null)
            {
                var ps = Map.ParticleWorld.Create("CritterDeath", _deathPS);
                if (ps != null) ps.LocalMatrix = Matrix4x4.Translate(body.Position);
            }
            if (_deathEntity != "")
            {
                Transform mesh = Rt.skinnedModel != null ? Rt.skinnedModel : (body != null ? body.Data.transform : GameObject.transform);
                Map.CreateEntity(Name + "_Dead", _deathEntity, mesh.position, mesh.rotation, Vector3.one);
            }
            OnKillCritter();
            Map.DestroyEntity(this);
        }

        protected override void OnDamage(float amount, int strength) => OnDamageCritter(amount);

        // ------------------------------------------------------------------ helpers (HPL2 space)

        protected static void CreateOrthoVectors(out Vector3 right, ref Vector3 up, ref Vector3 forward)
        {
            if (forward == up || forward == -up)
            {
                forward.x = up.z;
                forward.y = -up.x;
                forward.z = up.x;
            }
            right = Vector3.Cross(up, forward).normalized;
            forward = Vector3.Cross(right, up).normalized;
        }

        protected Vector3 PlayerHplPosition => HplSpace.Position(Map.player.Body.Position);
        protected Vector3 PlayerHplFeet => HplSpace.Position(Map.player.Body.FeetPosition);

        protected float GetDistanceToPlayer() => Vector3.Distance(PlayerHplPosition, CritterBody.HplPosition);

        protected float GetDistanceToPlayer2D()
        {
            Vector3 a = PlayerHplFeet, b = CritterBody.HplPosition;
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        /// <summary>iLuxProp_CritterBase::Attack: a sphere AttackOffset.z ahead (cLuxMapHelper::ShapeDamage, player only, not lethal).</summary>
        protected bool Attack(Vector3 dir)
        {
            if (AttackSizeRadius <= 0f) return false;
            var body = CritterBody;
            Vector3 center = HplSpace.Position(body.HplPosition + dir * AttackOffset.z);
            bool hitPlayer = false;
            bool hit = Enemies.HplEnemyAttack.ShapeDamage(Map, null, center, Quaternion.identity, Vector3.one * AttackSizeRadius, body.Position,
                                                          AttackDamageMinMax.x, AttackDamageMinMax.y, AttackForce.x, AttackForce.y,
                                                          AttackStrength, 3f, Player.DamageType.BloodSplat, true, false, ref hitPlayer,
                                                          sphereRadius: AttackSizeRadius, lethalForPlayer: false);
            if (hit) PlaySound("CritterAttack", _attackHitSound, true, true);
            return hit;
        }

        protected Vector3 GetWanderAdd(float length, float radius, float timeStep)
        {
            Vector3 dir;
            if (Vel == Vector3.zero)
            {
                if (Rt.fwdDirs.Count == 0)
                {
                    float a = HplMath.RandRectf(0f, Mathf.PI * 2f);
                    dir = HplMatrix.Mul(HplMatrix.RotateY(a), new Vector3(1f, 0f, 0f));
                }
                else dir = Rt.fwdDirs.Last.Value;
            }
            else dir = Vel.normalized;
            dir -= GroundNormal * Vector3.Dot(GroundNormal, dir); // only the direction in the current movement plane

            float angle = HplMath.RandRectf(0f, Mathf.PI * 2f);
            // cQuaternion::FromAngleAxis + MatrixQuaternion: a right-handed rotation about the ground normal.
            Vector3 force = HplRotateAxis(new Vector3(2f, 0f, 0f), GroundNormal, angle);
            return (dir * 1f + force) * timeStep;   // length and radius are not used (as in HPL2)
        }

        /// <summary>The HPL2 (right-handed) rotation of v by angle about axis.</summary>
        static Vector3 HplRotateAxis(Vector3 v, Vector3 axis, float angle)
        {
            axis = axis.normalized;
            float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
            return v * c + Vector3.Cross(axis, v) * s + axis * (Vector3.Dot(axis, v) * (1f - c));
        }

        protected Vector3 GetTowardPlayerAdd(bool dependOnDistance, float timeStep)
        {
            Vector3 toPlayer = PlayerHplFeet - CritterBody.HplPosition;
            float dist = toPlayer.magnitude;
            toPlayer = toPlayer.normalized;
            toPlayer -= GroundNormal * Vector3.Dot(GroundNormal, toPlayer);
            Vector3 add = toPlayer * timeStep;
            if (dependOnDistance) add *= 1f / dist;
            return add;
        }

        void UpdateMovement(float timeStep)
        {
            if (Vel == Vector3.zero) return;
            Vector3 velAdd = Vel * timeStep;
            Vector3 gravityAdd = GravityVel * timeStep;
            if (UseRayCollision) CheckRayCollision(velAdd, gravityAdd);
            else CheckShapeCollision(velAdd, gravityAdd, timeStep);
        }

        /// <summary>iLuxProp_CritterBase::UpdateMesh: orientation and Idle / Walk.</summary>
        void UpdateMesh()
        {
            if (Vel != Vector3.zero)
            {
                Rt.fwdDirs.AddLast(Vel.normalized);
                if (Rt.fwdDirs.Count > 20) Rt.fwdDirs.RemoveFirst();
            }

            Vector3 forward = new(0f, 0f, 1f);
            if (Rt.fwdDirs.Count > 0)
            {
                Vector3 total = Vector3.zero;
                foreach (var d in Rt.fwdDirs) total += d;
                forward = total / Rt.fwdDirs.Count;
            }

            Rt.upDirs.AddLast(GroundNormal);
            if (Rt.upDirs.Count > 40) Rt.upDirs.RemoveFirst();
            Vector3 totalUp = Vector3.zero;
            foreach (var u in Rt.upDirs) totalUp += u;
            Vector3 up = totalUp / Rt.upDirs.Count;

            CreateOrthoVectors(out Vector3 right, ref up, ref forward);
            // mtxRotate rows are right / up / forward; its transpose puts them in the columns.
            var rotT = Matrix4x4.identity;
            rotT.SetColumn(0, right);
            rotT.SetColumn(1, up);
            rotT.SetColumn(2, forward);
            rotT.m33 = 1f;

            if (Rt.skinnedModel != null)
            {
                Matrix4x4 hpl = rotT * HplMatrix.RotateY(Mathf.PI) * _meshOffset;
                SetLocal(Rt.skinnedModel, HplSpace.Mirror(hpl) * Rt.skinnedCorrection);
            }
            else
            {
                Matrix4x4 m = HplSpace.Mirror(rotT * _meshOffset);
                for (int i = 0; i < Rt.subMeshes.Count; i++)
                    if (Rt.subMeshes[i] != null) SetLocal(Rt.subMeshes[i], m * Rt.baseMatrices[i]);
            }

            if (Animation != null && Animation.States.Count > 0 && UpdateAnimationEnabled)
            {
                float speed = Vel.magnitude;
                if (AnimState == 0)
                {
                    if (speed > 0.1f)
                    {
                        PlayFadeToName("Walk", true, 0.3f);
                        AnimState = 1;
                    }
                }
                else if (AnimState == 1)
                {
                    var walk = Animation.Get("Walk");
                    if (walk != null) walk.Speed = speed;
                    if (speed < 0.05f)
                    {
                        PlayFadeToName("Idle", true, 0.3f);
                        AnimState = 0;
                    }
                }
            }
        }

        /// <summary>cMeshEntity::PlayFadeToName (nothing when the animation does not exist).</summary>
        protected void PlayFadeToName(string name, bool loop, float time)
        {
            int index = Animation != null ? Animation.IndexOf(name) : -1;
            if (index >= 0) Animation.PlayFadeTo(index, loop, time);
        }

        static void SetLocal(Transform t, Matrix4x4 m)
        {
            t.localPosition = m.GetPosition();
            t.localRotation = m.rotation;
            t.localScale = m.lossyScale;
        }

        /// <summary>iLuxProp_CritterBase::CheckRayCollision (bugs).</summary>
        void CheckRayCollision(Vector3 velAdd, Vector3 gravityAdd)
        {
            var body = CritterBody;
            Vector3 pos = body.HplPosition;

            if (velAdd != Vector3.zero)
            {
                RayCb.Reset();
                Vector3 dir = velAdd.normalized;
                Vector3 extraAdd = dir * Rt.bodySize.x * 0.5f;
                RayCb.Cast(pos, pos + velAdd + extraAdd);
                if (RayCb.Intersected)
                {
                    Vector3 normal = RayCb.Normal;
                    normal.y = 0f;
                    // HPL2 normalizes a zero vector to NaN (a hit of a flat face) and loses the critter; here it stays zero.
                    normal = normal.sqrMagnitude > 0f ? normal / normal.magnitude : Vector3.zero;
                    Vel -= normal * Vector3.Dot(normal, Vel);
                    Vel.y = 0f;
                }
                else pos += velAdd;
            }
            if (gravityAdd != Vector3.zero)
            {
                RayCb.Reset();
                // cVector3f + float adds to every component: the ray also goes −x and −z by half the height (kept).
                float down = Rt.bodySize.y * -0.5f;
                RayCb.Cast(pos, pos + new Vector3(down, down, down) + gravityAdd);
                if (RayCb.Intersected) GravityVel = Vector3.zero;
                else pos += gravityAdd;
            }
            body.SetHplMatrix(Matrix4x4.Translate(pos));
        }

        /// <summary>iLuxProp_CritterBase::CheckShapeCollision (the spider).</summary>
        void CheckShapeCollision(Vector3 velAdd, Vector3 gravityAdd, float timeStep)
        {
            var body = CritterBody;
            Vector3 pos = body.HplPosition;
            Colliding = false;

            if (velAdd != Vector3.zero)
            {
                pos += velAdd;
                if (CheckShapeWorldCollision(pos, out Vector3 push))
                {
                    Colliding = true;
                    if (push != Vector3.zero)
                    {
                        pos += push;
                        Vector3 pushDir = push.normalized;
                        OnShapeCollision(push, timeStep);
                        Vel -= pushDir * Vector3.Dot(pushDir, Vel);
                    }
                }
            }
            if (gravityAdd != Vector3.zero)
            {
                pos += gravityAdd;
                if (CheckShapeWorldCollision(pos, out Vector3 push))
                {
                    Colliding = true;
                    if (push != Vector3.zero)
                    {
                        pos += push;
                        Vector3 pushDir = push.normalized;
                        OnShapeCollision(push, timeStep);
                        GravityVel -= pushDir * Vector3.Dot(pushDir, GravityVel);
                        GravityVel = GroundNormal * Vector3.Dot(GravityVel, GroundNormal);
                    }
                }
            }
            body.SetHplMatrix(Matrix4x4.Translate(pos));
        }

        static readonly Collider[] s_overlaps = new Collider[64];

        /// <summary>
        /// iPhysicsWorld::CheckShapeWorldCollision (not as a character): active colliding bodies, characters too, the
        /// own body skipped; the push is the per-axis largest of all contacts (HPL2 space).
        /// </summary>
        bool CheckShapeWorldCollision(Vector3 hplPos, out Vector3 push)
        {
            push = Vector3.zero;
            var shape = Rt.shape;
            if (shape == null) return false;
            Vector3 unityPos = HplSpace.Position(hplPos);
            Vector3 offset = unityPos - CritterBody.Position;
            Transform st = shape.transform;
            Vector3 shapePos = st.position + offset;
            Vector3 half = Rt.bodySize * 0.5f + Vector3.one * 0.01f;
            int mask = CritterRay.Mask | (1 << HplPhysics.CharacterLayer);
            int count = Physics.OverlapBoxNonAlloc(unityPos, half, s_overlaps, Quaternion.identity, mask, QueryTriggerInteraction.Ignore);
            bool collided = false;
            Vector3 total = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var other = s_overlaps[i];
                if (CritterRay.IsOwn(this, other) || !other.enabled) continue;
                Transform t = other.transform;
                if (!Physics.ComputePenetration(shape, shapePos, st.rotation, other, t.position, t.rotation, out Vector3 dir, out float dist) || dist <= 0f)
                    continue;
                collided = true;
                Vector3 p = HplSpace.Direction(dir * dist);
                if (Mathf.Abs(total.x) < Mathf.Abs(p.x)) total.x = p.x;
                if (Mathf.Abs(total.y) < Mathf.Abs(p.y)) total.y = p.y;
                if (Mathf.Abs(total.z) < Mathf.Abs(p.z)) total.z = p.z;
            }
            push = total;
            return collided;
        }

        /// <summary>
        /// cLuxCritterRayCallback: the closest hit, skipping non-colliding bodies, characters and the critter's own body.
        /// Like HPL2 it is only reset on Reset() (the spider's climb test relies on that).
        /// </summary>
        protected sealed class CritterRay
        {
            readonly PropCritterBase _owner;
            static readonly RaycastHit[] s_hits = new RaycastHit[32];

            /// <summary>Character shapes (Ignore Raycast) and non-colliding bodies (layer 30) are not hit.</summary>
            public static int Mask => Physics.DefaultRaycastLayers & ~(1 << HplPhysicsWorld.NoCollideLayer);

            public CritterRay(PropCritterBase owner) { _owner = owner; }

            public bool Intersected { get; private set; }
            public Vector3 Normal { get; private set; }   // HPL2
            public Vector3 Point { get; private set; }    // HPL2
            public Collider Collider { get; private set; }
            float _closestT;

            public void Reset()
            {
                Intersected = false;
                _closestT = 99999f;
                Collider = null;
            }

            public static bool IsOwn(PropCritterBase critter, Collider c)
            {
                var body = critter.CritterBody;
                if (body == null) return false;
                foreach (var own in body.Colliders) if (own == c) return true;
                return false;
            }

            /// <summary>HPL2 start → end (CastRay); every hit counts, the closest keeps its data.</summary>
            public void Cast(Vector3 hplStart, Vector3 hplEnd)
            {
                Vector3 start = HplSpace.Position(hplStart), end = HplSpace.Position(hplEnd);
                Vector3 delta = end - start;
                float length = delta.magnitude;
                if (length < 1e-6f) return;
                int count = Physics.RaycastNonAlloc(start, delta / length, s_hits, length, Mask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = s_hits[i];
                    if (IsOwn(_owner, hit.collider)) continue;
                    var data = HplPhysics.BodyOf(hit.collider);
                    if (data != null && !data.collideNonCharacter) continue;
                    float t = hit.distance / length;
                    if (t < _closestT)
                    {
                        _closestT = t;
                        Normal = HplSpace.Direction(hit.normal);
                        Point = HplSpace.Position(hit.point);
                        Collider = hit.collider;
                    }
                    Intersected = true;
                }
            }

            /// <summary>The mass of the hit body (0 for static geometry).</summary>
            public float BodyMass(HplMap map)
            {
                if (Collider == null) return 0f;
                var body = map.PhysicsWorld.BodyOf(Collider);
                return body != null ? body.Mass : 0f;
            }
        }
    }

    /// <summary>
    /// cLuxCritter_Bug (LuxCritter_Bug.cpp): cockroaches, rats and fish. Wanders (random push WanderCircleRadius,
    /// forward WanderCircleDist), swarms around its start point, flees the player within PlayerFleeDistance (faster:
    /// MaxSpeedAfraid), random pauses (HasRandomPauses), falls with Gravity until a ray finds ground, idle / scared
    /// sounds. Moves by rays.
    /// </summary>
    public sealed class PropCritterBug : PropCritterBase
    {
        float _maxSpeedNormal, _maxSpeedAfraid, _gravity, _playerFleeDistance, _fleeMul, _wanderCircleRadius, _wanderCircleDist;
        bool _hasRandomPauses;
        Vector2 _randomPauseTimeMinMax, _randomMoveTimeMinMax, _normalSoundRandMinMax, _scaredSoundRandMinMax;
        string _normalSound = "", _scaredSound = "";

        // Saved (cLuxCritter_Bug_SaveData).
        float _playSoundCount = HplMath.RandRectf(1f, 5f);

        /// <summary>Not saved in HPL2: the pause state and the current max speed.</summary>
        sealed class BugRuntime { public float pauseCount = HplMath.RandRectf(1f, 3f); public bool paused; public float maxSpeed; }
        readonly BugRuntime _bug = new();

        public PropCritterBug(World.AmnesiaEntity entity) : base(entity) { }

        public bool Paused => _bug.paused;
        public float MaxSpeed => _bug.maxSpeed;

        protected override void LoadCritterVariables()
        {
            _maxSpeedNormal = VarFloat("MaxSpeedNormal", 0f);
            _maxSpeedAfraid = VarFloat("MaxSpeedAfraid", 0f);
            _bug.maxSpeed = _maxSpeedNormal;
            _bug.maxSpeed = VarFloat("MaxSpeed", 0f);   // no .ent has it: 0 until the first update sets it (kept)
            _gravity = VarFloat("Gravity", 0f);
            _playerFleeDistance = VarFloat("PlayerFleeDistance", 0f);
            _fleeMul = VarFloat("FleeMul", 0f);
            VarFloat("BackToSpawnPointMul", 0f);       // read, not used
            _wanderCircleRadius = VarFloat("WanderCircleRadius", 0f);
            _wanderCircleDist = VarFloat("WanderCircleDist", 0f);
            _hasRandomPauses = VarBool("HasRandomPauses", false);
            _randomPauseTimeMinMax = VarVector2("RandomPauseTimeMinMax", new Vector2(1f, 3f));
            _randomMoveTimeMinMax = VarVector2("RandomMoveTimeMinMax", new Vector2(2f, 5f));
            _normalSound = VarString("NormalSound", "");
            _normalSoundRandMinMax = VarVector2("NormalSoundRandMinMax", Vector2.zero);
            _scaredSound = VarString("ScaredSound", "");
            _scaredSoundRandMinMax = VarVector2("ScaredSoundRandMinMax", Vector2.zero);
        }

        protected override void UpdateCritterSpecific(float timeStep) => UpdateVelocity(timeStep);

        /// <summary>cLuxCritter_Bug::UpdateVelocity.</summary>
        void UpdateVelocity(float timeStep)
        {
            var body = CritterBody;
            Vector3 bodyPos = body.HplPosition;

            bool scared = false;
            Vector3 playerPos = PlayerHplFeet;
            float playerDistanceSqr = (playerPos - bodyPos).sqrMagnitude;
            if (playerDistanceSqr < _playerFleeDistance * _playerFleeDistance)
            {
                scared = true;
                _bug.paused = false;
            }

            if (_hasRandomPauses && !scared)
            {
                _bug.pauseCount -= timeStep;
                if (_bug.pauseCount < 0f)
                {
                    if (_bug.paused)
                    {
                        _bug.paused = false;
                        _bug.pauseCount = HplMath.RandRectf(_randomMoveTimeMinMax.x, _randomMoveTimeMinMax.y);
                    }
                    else
                    {
                        _bug.paused = true;
                        _bug.pauseCount = HplMath.RandRectf(_randomPauseTimeMinMax.x, _randomPauseTimeMinMax.y);
                    }
                }
            }

            if (!_bug.paused)
            {
                // Wandering
                Vector3 dir;
                if (Vel == Vector3.zero)
                {
                    if (Rt.fwdDirs.Count == 0)
                    {
                        float a = HplMath.RandRectf(0f, Mathf.PI * 2f);
                        dir = HplMatrix.Mul(HplMatrix.RotateY(a), new Vector3(1f, 0f, 0f));
                    }
                    else dir = Rt.fwdDirs.Last.Value;
                }
                else dir = Vel.normalized;
                float angle = HplMath.RandRectf(0f, Mathf.PI * 2f);
                Vector3 force = HplMatrix.Mul(HplMatrix.RotateY(angle), new Vector3(_wanderCircleRadius, 0f, 0f));
                Vel += (dir * _wanderCircleDist + force) * timeStep;

                // Avoid player
                if (scared)
                {
                    float playerDistance = Mathf.Sqrt(playerDistanceSqr);
                    if (playerDistance == 0f) playerDistance = 0.0001f;
                    playerPos.y = bodyPos.y;
                    Vector3 wanted = (bodyPos - playerPos).normalized * _bug.maxSpeed;
                    Vector3 flee = (wanted - Vel) * _fleeMul * (1f / playerDistance);
                    Vel += flee * timeStep;
                    _bug.maxSpeed = _maxSpeedAfraid;
                }
                else _bug.maxSpeed = _maxSpeedNormal;

                // Swarm around point
                float swarmDist = Vector3.Distance(SwarmPoint, bodyPos);
                Vector3 swarmPos = new(SwarmPoint.x, bodyPos.y, SwarmPoint.z);
                Vector3 wantedVel = (swarmPos - bodyPos).normalized * _bug.maxSpeed;
                wantedVel.y = 0f;
                Vel += (wantedVel - Vel) * 0.2f * swarmDist * timeStep;
            }
            else Vel -= Vel * timeStep * 3f;   // stopping

            // Sound
            _playSoundCount -= timeStep;
            if (_playSoundCount < 0f)
            {
                _playSoundCount = scared ? HplMath.RandRectf(_scaredSoundRandMinMax.x, _scaredSoundRandMinMax.y)
                                         : HplMath.RandRectf(_normalSoundRandMinMax.x, _normalSoundRandMinMax.y);
                PlaySound("Critter_BugSound", scared ? _scaredSound : _normalSound, true, true);
            }

            // Cap speed
            float speed = Vel.magnitude;
            if (speed > _bug.maxSpeed) Vel = Vel / speed * _bug.maxSpeed;

            // Gravity
            GravityVel.y -= _gravity * timeStep;
            GravityVel = HplMatrix.Vector3MaxLength(GravityVel, 10f);
        }
    }

    /// <summary>
    /// cLuxCritter_Spider (LuxCritter_Spider.cpp): idle ↔ move (wander, swarm), flee when the lantern is on within its
    /// light radius (game.cfg Player_Lantern Radius = 0.0001: practically never, kept), jump attack at the player
    /// 1–4 m away after a move, hit by damage > 2; walks on walls (a new ground normal where the shape collides with a
    /// climbable static wall), falls back to (0,1,0) after 0.3 s in the air. Moves by its shape. Causes sanity
    /// decrease near the player. No map of the game places one.
    /// </summary>
    public sealed class PropCritterSpider : PropCritterBase
    {
        enum State { Idle, Move, Flee, Hunt, Hit, Attack1, Attack2 }

        float _maxIdleSpeed, _maxHuntSpeed, _huntPlayerDist;
        string _hitSound = "", _idleSound = "", _attackSound = "";
        Vector2 _idleSoundRandMinMax, _attackPlayerDistMinMax, _huntTimeMinMax, _moveTimeMinMax, _idleTimeMinMax;

        // Saved (cLuxCritter_Spider_SaveData).
        State _state = State.Idle;
        float _fallingCount;
        float _newGroundNormalCount;
        bool _onGround;
        float _maxSpeed = 0.5f;
        float _count;
        float _playSoundCount = HplMath.RandRectf(1f, 5f);

        public PropCritterSpider(World.AmnesiaEntity entity) : base(entity)
        {
            UseRayCollision = false;
            CausesSanityDecreaseValue = true;
        }

        public string StateName => _state.ToString();
        public bool OnGround => _onGround;

        protected override void LoadCritterVariables()
        {
            _maxIdleSpeed = VarFloat("MaxIdleSpeed", 0f);
            _maxHuntSpeed = VarFloat("MaxHuntSpeed", 0f);
            _hitSound = VarString("HitSound", "");
            _idleSound = VarString("IdleSound", "");
            _idleSoundRandMinMax = VarVector2("IdleSoundRandMinMax", Vector2.zero);
            _attackSound = VarString("AttackSound", "");
            _attackPlayerDistMinMax = VarVector2("AttackPlayerDistMinMax", Vector2.zero);
            _huntPlayerDist = VarFloat("HuntPlayerDist", 0f);
            _huntTimeMinMax = VarVector2("HuntTimeMinMax", Vector2.zero);
            _moveTimeMinMax = VarVector2("MoveTimeMinMax", Vector2.zero);
            _idleTimeMinMax = VarVector2("IdleTimeMinMax", Vector2.zero);
        }

        protected override void UpdateCritterSpecific(float timeStep)
        {
            UpdateVelocity(timeStep);
            UpdateGroundCheck(timeStep);
        }

        protected override void OnDamageCritter(float amount)
        {
            if (amount <= 2f) return;
            _count = 1f;
            _state = State.Hit;
            _maxSpeed = 0.1f;
            GroundNormal = new Vector3(0f, 1f, 0f);
            PlaySound("Critter_SpiderHitSound", _hitSound, true, true);
        }

        void StartAttack()
        {
            _state = State.Attack1;
            PlaySound("CritterAttack", _attackSound, true, true);
            Vector3 dir = (PlayerHplPosition + new Vector3(0f, 0.5f, 0f) - CritterBody.HplPosition).normalized;
            GravityVel += dir * 25f;
            _maxSpeed = 3f;
            UpdateAnimationEnabled = false;
            PlayFadeToName("Attack", false, 0.3f);
            AnimState = 0;
            _count = 0.8f;
            GroundNormal = new Vector3(0f, 1f, 0f);
        }

        /// <summary>cLuxCritter_Spider::UpdateVelocity.</summary>
        void UpdateVelocity(float timeStep)
        {
            var body = CritterBody;
            Vector3 moveVelAdd = Vector3.zero;
            if (_count > 0f) _count -= timeStep;

            // If lantern is on, flee!
            var player = Map.player;
            if (_state != State.Flee && player.Lantern != null && player.Lantern.Active)
            {
                float lightRadius = Map.gameConfig != null ? Map.gameConfig.GetFloat("Player_Lantern", "Radius", 0f) : 0f;
                if (GetDistanceToPlayer() < lightRadius)
                {
                    _state = State.Flee;
                    _count = HplMath.RandRectf(_moveTimeMinMax.x, _moveTimeMinMax.y);
                }
            }

            switch (_state)
            {
                case State.Idle:
                    _maxSpeed = 0f;
                    if (_count <= 0f)
                    {
                        _state = State.Move;
                        _count = HplMath.RandRectf(_moveTimeMinMax.x, _moveTimeMinMax.y);
                    }
                    break;
                case State.Flee:
                    _maxSpeed = _maxIdleSpeed;
                    moveVelAdd -= GetTowardPlayerAdd(false, timeStep);
                    moveVelAdd += GetWanderAdd(1f, 2f, timeStep);
                    if (_count <= 0f)
                    {
                        _state = State.Idle;
                        _count = HplMath.RandRectf(_idleTimeMinMax.x, _idleTimeMinMax.y);
                    }
                    break;
                case State.Move:
                {
                    _maxSpeed = _maxIdleSpeed;
                    moveVelAdd += GetWanderAdd(1f, 2f, timeStep);
                    // Swarm around point (y of the target is the body's x: a typo, kept).
                    Vector3 bodyPos = body.HplPosition;
                    float swarmDist = Vector3.Distance(SwarmPoint, bodyPos);
                    Vector3 swarmPos = new(SwarmPoint.x, bodyPos.x, SwarmPoint.z);
                    Vector3 wanted = (swarmPos - bodyPos).normalized * _maxSpeed;
                    wanted.y = 0f;
                    moveVelAdd += (wanted - Vel) * 0.01f * swarmDist * timeStep;

                    if (_count <= 0f)
                    {
                        float dist = GetDistanceToPlayer2D();
                        if (!player.IsDead && dist < _attackPlayerDistMinMax.y && dist > _attackPlayerDistMinMax.x) StartAttack();
                        else
                        {
                            _state = State.Idle;
                            _count = HplMath.RandRectf(_idleTimeMinMax.x, _idleTimeMinMax.y);
                        }
                    }
                    break;
                }
                case State.Hunt:   // never entered (the switch to it is commented out in HPL2)
                {
                    _maxSpeed = _maxHuntSpeed;
                    moveVelAdd += GetTowardPlayerAdd(false, timeStep) * 3f;
                    moveVelAdd += GetWanderAdd(1f, 2f, timeStep);
                    float dist = GetDistanceToPlayer2D();
                    if (dist < _attackPlayerDistMinMax.y && dist > _attackPlayerDistMinMax.x) StartAttack();
                    if (_count <= 0f)
                    {
                        _state = State.Idle;
                        _count = HplMath.RandRectf(1f, 3f);
                    }
                    break;
                }
                case State.Hit:
                    if (_count <= 0f) _state = State.Idle;
                    break;
                case State.Attack1:
                    _maxSpeed = _maxHuntSpeed;
                    moveVelAdd += GetTowardPlayerAdd(false, timeStep);
                    if (_count <= 0f || GetDistanceToPlayer2D() < 1.5f)
                    {
                        _state = State.Attack2;
                        UpdateAnimationEnabled = true;
                        Attack((PlayerHplPosition - body.HplPosition).normalized);
                        _count = 0.5f;
                    }
                    break;
                case State.Attack2:
                    _maxSpeed = _maxHuntSpeed;
                    moveVelAdd += GetTowardPlayerAdd(false, timeStep);
                    if (_count <= 0f) _state = State.Idle;
                    break;
            }

            Vel += moveVelAdd;

            if (_state == State.Move || _state == State.Hunt || _state == State.Flee)
            {
                _playSoundCount -= timeStep;
                if (_playSoundCount < 0f)
                {
                    _playSoundCount = HplMath.RandRectf(_idleSoundRandMinMax.x, _idleSoundRandMinMax.y);
                    PlaySound("Critter_SpiderSound", _idleSound, true, true);
                }
            }

            Vel = HplMatrix.Vector3MaxLength(Vel, _maxSpeed);
        }

        /// <summary>cLuxCritter_Spider::UpdateGroundCheck.</summary>
        void UpdateGroundCheck(float timeStep)
        {
            var body = CritterBody;
            _onGround = false;
            Vector3 start = body.HplPosition - GroundNormal * (Rt.shapeRadius - 0.05f);
            Vector3 end = start - GroundNormal * 0.25f;
            RayCb.Reset();
            RayCb.Cast(start, end);
            if (RayCb.Intersected)
            {
                GroundNormal = RayCb.Normal;
                _onGround = true;
            }

            if (!_onGround || !Colliding)
            {
                GravityVel -= GroundNormal * 9.8f * timeStep;
                _fallingCount += timeStep;
                if (_fallingCount > 0.3f)
                {
                    GroundNormal = new Vector3(0f, 1f, 0f);
                    GravityVel = GroundNormal * GravityVel.magnitude * -1f;
                }
                GravityVel = HplMatrix.Vector3MaxLength(GravityVel, 10f);
            }
            else _fallingCount = 0f;
        }

        /// <summary>cLuxCritter_Spider::OnShapeCollision: climbing onto a wall.</summary>
        protected override void OnShapeCollision(Vector3 pushVec, float timeStep)
        {
            if (_state == State.Hit) return;
            if (_newGroundNormalCount > 0f)
            {
                _newGroundNormalCount -= timeStep;
                return;
            }

            var body = CritterBody;
            float radius = Rt.shapeRadius;
            Vector3 pushDir = (-pushVec.normalized).normalized;
            pushDir -= GroundNormal * Vector3.Dot(GroundNormal, pushDir);

            // Check wall intersection
            Vector3 start = body.HplPosition + pushDir * (radius - 0.05f);
            Vector3 end = start + pushDir * 0.1f;
            Vector3 intersectNormal = Vector3.zero;
            RayCb.Reset();
            RayCb.Cast(start, end);
            if (!RayCb.Intersected) return;
            if (RayCb.BodyMass(Map) != 0f) return;   // no climbing on dynamic bodies
            intersectNormal = RayCb.Normal;
            if (intersectNormal == Vector3.zero) return;
            if (Vector3.Dot(intersectNormal, GroundNormal) > 8f) return;   // "> 8" instead of 0.71: never (kept)

            // Check so that the space above is free.
            const float testHeight = 1.5f;
            start = body.HplPosition - pushDir * radius;
            end = start + GroundNormal * testHeight;
            RayCb.Reset();
            RayCb.Cast(start, end);
            if (RayCb.Intersected) return;

            // Check so that there is something to climb on (the ray is not reset between the tests: after the first
            // hit every test counts — kept).
            int climbPoints = 0;
            for (int i = 1; i <= 5; ++i)
            {
                float height = testHeight / 5f * i;
                start = body.HplPosition + GroundNormal * height;
                end = start + pushDir * (radius + 0.25f);
                RayCb.Cast(start, end);
                if (RayCb.Intersected) climbPoints++;
            }
            if (climbPoints < 3) return;

            // Set new ground normal
            Vector3 oldGroundNormal = GroundNormal;
            GroundNormal = intersectNormal;
            Vector3 forward = Vel.normalized;
            Vector3 right = Vector3.Cross(oldGroundNormal, forward).normalized;
            forward = Vector3.Cross(right, GroundNormal).normalized;
            Vel = forward * Vel.magnitude;
        }
    }
}
