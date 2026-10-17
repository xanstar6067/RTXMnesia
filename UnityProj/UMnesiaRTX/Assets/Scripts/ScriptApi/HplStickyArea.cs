using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// cLuxArea_Sticky (LuxArea_Sticky.cpp): a body that comes into the area is snapped into it and held there
    /// (static, no gravity) until it is detached by interaction (CanDetach) or by script. Candidates: active, colliding
    /// bodies with mass whose center of mass (CheckCenterInArea) or bounding box is inside the area's box, whose name
    /// contains AttachableBodyName (if set) and whose shape touches the area; the AttachFunction (area, body) may
    /// allow it with SetAllowStickyAreaAttachment. The body then moves / turns to the area's pose over PoseTime, and
    /// the attach sound and particle system start when it is in place. The last attached body is not taken again
    /// before it has left the area: it is forgotten when the broad phase still returns it (its AABB padded by 1/8 m
    /// touches the area) but the exact test finds it outside, so a body that jumps away in one step stays the last one.
    ///
    /// Not ported: character bodies (HPL2 also tests them, mass or not; the player here has no physics body).
    /// </summary>
    public sealed class HplStickyArea
    {
        readonly HplArea _area;
        readonly string _attachFunction, _detachFunction, _attachSound, _detachSound, _attachPS, _detachPS, _attachableBodyName;
        readonly bool _moveBody, _rotateBody, _checkCenterInArea, _canDetach;
        readonly float _poseTime;

        bool _attachedBodyGravity, _attachedEntityFullGameSaved;
        float _attachedBodyMass;
        float _setMtxTime = 1f;
        Matrix4x4 _attachedStart;   // HPL2 space

        public HplPhysicsBody AttachedBody { get; private set; }
        HplPhysicsBody _lastAttachedBody;
        static readonly Vector3 NewtonPadding = Vector3.one * (2f / 8f);

        /// <summary>cLuxArea_Sticky::mbAllowAttachment (static, set by SetAllowStickyAreaAttachment).</summary>
        public static bool AllowAttachment;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AllowAttachment = false;

        public HplStickyArea(HplArea area, AmnesiaArea data)
        {
            _area = area;
            string Var(string name) => data.GetVariable(name) ?? "";
            bool Bool(string name, bool fallback) => data.GetVariable(name) is { } v ? v == "true" : fallback;
            _attachFunction = Var("AttachFunction");
            _detachFunction = Var("DetachFunction");
            _attachSound = Var("AttachSound");
            _detachSound = Var("DetachSound");
            _attachPS = Var("AttachPS");
            _detachPS = Var("DetachPS");
            _attachableBodyName = Var("AttachableBodyName");
            _moveBody = Bool("MoveBody", true);
            _rotateBody = Bool("RotateBody", true);
            _checkCenterInArea = Bool("CheckCenterInArea", true);
            _canDetach = Bool("CanDetach", true);
            _poseTime = data.GetVariable("PoseTime") is { } t ? HplMath.Atof(t) : 1f;
            // cLuxArea_Sticky constructor: the whole area is saved in the map cache.
            area.FullGameSave = true;
        }

        HplMap Map => _area.Map;

        /// <summary>cLuxArea_Sticky::CanDetach: detachable and in place.</summary>
        public bool CanDetach => _canDetach && _setMtxTime >= 1f;

        Matrix4x4 AreaHplMatrix
        {
            get
            {
                var t = _area.GameObject.transform;
                return HplSpace.ToHpl(t.position, t.rotation);
            }
        }

        public void Update(float timeStep)
        {
            UpdateAttachBody(timeStep);
            UpdateCollision();
        }

        /// <summary>cLuxArea_Sticky::AttachBody.</summary>
        public void AttachBody(HplPhysicsBody body)
        {
            if (AttachedBody != null || body == null) return;
            // An interact state holding this body lets go.
            Map.player?.States?.Current.OnAttachBodyToStickyArea(body);

            _attachedStart = body.HplMatrix;
            _setMtxTime = 0f;
            body.LinearVelocity = Vector3.zero;
            body.AngularVelocity = Vector3.zero;
            _attachedBodyGravity = body.Gravity;
            body.Gravity = false;
            _attachedBodyMass = body.Mass;
            body.Mass = 0f;
            if (body.UserData is HplEntity entity)
            {
                _attachedEntityFullGameSaved = entity.FullGameSave;
                entity.FullGameSave = true;
            }
            else _attachedEntityFullGameSaved = false;
            AttachedBody = body;
            _lastAttachedBody = body;
        }

        /// <summary>cLuxArea_Sticky::DetachBody: the detach function, sound and particles, the body's mass and gravity back.</summary>
        public void DetachBody()
        {
            if (AttachedBody == null) return;
            if (_detachFunction != "") Map.RunScript(_detachFunction, _area.Name, AttachedBody.Name);
            Effects(_detachSound, _detachPS, "DetachSound", "DetachPS");
            AttachedBody.Gravity = _attachedBodyGravity;
            AttachedBody.Mass = _attachedBodyMass;
            AttachedBody.WakeUp();
            if (AttachedBody.UserData is HplEntity entity) entity.FullGameSave = _attachedEntityFullGameSaved;
            AttachedBody = null;
        }

        /// <summary>The attached body was destroyed (HPL2 would keep a dangling pointer).</summary>
        public void OnBodyRemoved(HplPhysicsBody body)
        {
            if (AttachedBody == body) AttachedBody = null;
            if (_lastAttachedBody == body) _lastAttachedBody = null;
        }

        void Effects(string sound, string ps, string soundName, string psName)
        {
            var t = _area.GameObject.transform;
            if (sound != "" && Map.sound != null)
            {
                var s = Map.sound.CreateSoundEntity(soundName, sound, true);
                if (s != null) s.Position = t.position;
            }
            if (ps != "" && Map.ParticleWorld != null)
            {
                var system = Map.ParticleWorld.Create(psName, ps);
                if (system != null) system.LocalMatrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
            }
        }

        /// <summary>
        /// cLuxArea_Sticky::UpdateAttachBody: the body moves and / or turns to the area over the pose time. The goal
        /// position is the area's minus the body's local mass centre, unrotated, as the original.
        /// </summary>
        void UpdateAttachBody(float timeStep)
        {
            var body = AttachedBody;
            if (body == null || _setMtxTime >= 1f) return;

            Matrix4x4 area = AreaHplMatrix;
            Vector3 areaPos = HplMatrix.GetTranslation(area);
            if (_moveBody && _rotateBody)
            {
                _setMtxTime = _poseTime == 0f ? 1f : _setMtxTime + timeStep / _poseTime;
                Matrix4x4 goal = HplMatrix.SetTranslation(area, areaPos - LocalMassCentre(body));
                body.SetHplMatrix(HplMatrix.Slerp(_setMtxTime, _attachedStart, goal));
            }
            else if (_moveBody)
            {
                _setMtxTime = _poseTime == 0f ? 1f : _setMtxTime + timeStep / _poseTime;
                Vector3 goal = areaPos - LocalMassCentre(body);
                Vector3 pos = HplMatrix.GetTranslation(_attachedStart) * (1f - _setMtxTime) + goal * _setMtxTime;
                body.SetHplMatrix(HplMatrix.SetTranslation(body.HplMatrix, pos));
            }
            else if (_rotateBody)
            {
                _setMtxTime = _poseTime == 0f ? 1f : _setMtxTime + timeStep / _poseTime;
                Matrix4x4 m = HplMatrix.Slerp(_setMtxTime, _attachedStart, area);
                body.SetHplMatrix(HplMatrix.SetTranslation(m, body.HplPosition));
            }
            else _setMtxTime = 1f;

            if (_setMtxTime >= 1f) Effects(_attachSound, _attachPS, "AttachSound", "AttachPS");
        }

        /// <summary>iPhysicsBody::GetMassCentre: the center of mass in the body's frame (HPL2 axes).</summary>
        static Vector3 LocalMassCentre(HplPhysicsBody body)
        {
            Vector3 c = body.Rigidbody.centerOfMass;
            return new Vector3(-c.x, c.y, c.z);
        }

        /// <summary>cLuxArea_Sticky::UpdateCollision.</summary>
        void UpdateCollision()
        {
            if (AttachedBody != null) return;
            if (!HplCollision.TryGetBounds(_area.GetBodyColliders(0), out Bounds areaBounds)) return;
            var areaColliders = _area.GetBodyColliders(0);

            foreach (var body in Map.PhysicsWorld.Bodies.ToArray())
            {
                if (!body.Collide || !body.Active) continue;
                if (body.Mass == 0f) continue;
                // GetBodiesInBV: Newton's broad phase pads the bodies' AABBs by 1/8 m (see HplLiquidArea).
                if (!body.TryGetBounds(out Bounds bodyBounds)) continue;
                Bounds padded = bodyBounds;
                padded.size += NewtonPadding;
                if (!padded.Intersects(areaBounds)) continue;

                bool inside = _checkCenterInArea ? areaBounds.Contains(body.WorldCenterOfMass) : bodyBounds.Intersects(areaBounds);
                if (!inside)
                {
                    // The last attached body has moved out: it may be attached again.
                    if (body == _lastAttachedBody) _lastAttachedBody = null;
                    continue;
                }
                if (body == _lastAttachedBody) continue;

                AllowAttachment = false;
                if (_attachableBodyName != "")
                {
                    if (body.Name.IndexOf(_attachableBodyName, System.StringComparison.Ordinal) < 0) continue;
                    AllowAttachment = true;
                }

                if (!HplCollision.BodiesCollide(body.Colliders, areaColliders)) continue;

                if (_attachFunction != "")
                {
                    Map.RunScript(_attachFunction, _area.Name, body.Name);
                    if (!AllowAttachment) continue;
                }
                AttachBody(body);
                // AttachBody returns at once for the next bodies (one is attached).
            }
        }

        // ------------------------------------------------------------------ save (cLuxArea_Sticky_SaveData)

        public Save.SavedSticky CaptureSave()
        {
            var s = new Save.SavedSticky
            {
                attachedBodyMass = _attachedBodyMass,
                attachedBodyGravity = _attachedBodyGravity,
                attachedEntityFullGameSaved = _attachedEntityFullGameSaved,
                setMtxTime = _setMtxTime,
            };
            if (AttachedBody != null && AttachedBody.UserData is HplEntity entity)
            {
                s.attachedEntity = entity.Name;
                s.attachedBody = AttachedBody.Name;
            }
            return s;
        }

        /// <summary>
        /// LoadFromSaveData: the attached body of a prop (an error if it is gone), also as the last attached one. The
        /// body is made static again here (the prop's saved bodies do not keep their mass); a pose in progress
        /// starts from where the body is (HPL2 does not save the start matrix).
        /// </summary>
        public void RestoreSave(Save.SavedSticky s)
        {
            _attachedBodyMass = s.attachedBodyMass;
            _attachedBodyGravity = s.attachedBodyGravity;
            _attachedEntityFullGameSaved = s.attachedEntityFullGameSaved;
            _setMtxTime = s.setMtxTime;
            AttachedBody = null;
            _lastAttachedBody = null;
            if (string.IsNullOrEmpty(s.attachedEntity)) return;
            if (Map.GetEntityByName(s.attachedEntity) is not HplProp prop)
            {
                HplLog.Error($"Could not load prop '{s.attachedEntity}'");
                return;
            }
            var body = prop.PhysicsBodies.Find(b => b.Name == s.attachedBody);
            if (body == null)
            {
                HplLog.Error($"Could not load body '{s.attachedBody}' in prop '{prop.Name}'");
                return;
            }
            AttachedBody = body;
            _lastAttachedBody = body;
            body.LinearVelocity = Vector3.zero;
            body.AngularVelocity = Vector3.zero;
            body.Gravity = false;
            body.Mass = 0f;
            _attachedStart = body.HplMatrix;
        }
    }
}
