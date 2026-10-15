using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_SwingDoor (LuxProp_SwingDoor.cpp): hinged doors. Closed = the max angle is 2°, so the door stays
    /// shut; touching it opens it (unless locked). Near 0° it closes by itself (single doors, not while held).
    /// Open amount from the map. Breaking (damage meshes, broken entity): health and damage come with enemies.
    /// </summary>
    public sealed class PropSwingDoor : HplProp
    {
        sealed class JointData
        {
            public HplJoint hinge;
            public HplPhysicsBody childBody;
            public float maxAngle;
        }

        readonly InteractDataRotate _data = new();
        readonly List<JointData> _joints = new();
        bool _canInteractWithStaticBody;
        float _interactSoundCount;
        bool _breakable, _disableBreakable, _broken;
        string _brokenEntity = "";
        float _breakImpulse;
        float[] _healthDamage = new float[2];
        int _currentMeshEntity;

        public bool Closed { get; private set; }
        public bool Locked { get; private set; }
        public bool DisableAutoClose;

        public PropSwingDoor(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override string DefaultFocusDistanceKey => "SwingDoor_DefaultMaxFocusDist";

        protected override void LoadVariables()
        {
            _data.moveMaxSpeed = VarFloat("MoveMaxSpeed", 13.5f);
            _data.moveSlowDownFactor = VarFloat("MoveSlowDownFactor", 3f);
            _data.moveSpeedFactor = VarFloat("MoveSpeedFactor", 1f);
            _data.moveThrowImpulse = VarFloat("MoveThrowImpulse", 6f);
            _canInteractWithStaticBody = VarBool("CanInteractWithStaticBody", false);
            _breakable = VarBool("Breakable", false);
            _brokenEntity = VarString("BrokenEntity", "");
            _breakImpulse = VarFloat("BreakImpulse", 0f);
            _healthDamage[0] = VarFloat("HealthDamage1", 0f);
            _healthDamage[1] = VarFloat("HealthDamage2", 0f);
            // Close / lock / interact sounds, damage meshes and their effects: step 6.
        }

        protected override void OnSetupAfterLoad()
        {
            foreach (var joint in Joints)
            {
                if (joint.Type != HplJointType.Hinge) continue;
                joint.StickyMinLimit = true;
                joint.StickyMaxLimit = true;
                _joints.Add(new JointData { hinge = joint, childBody = joint.Child, maxAngle = joint.MaxAngle });
            }
        }

        protected override void LoadInstanceVariables()
        {
            _disableBreakable = InstanceBool("DisableBreakable", false);
            SetLocked(InstanceBool("Locked", false), false);
            SetupDoorPhysics(InstanceFloat("OpenAmount", 0f));
        }

        public override InteractDataRotate GetMoveBaseData() => _data;

        public override bool CanInteract(HplPhysicsBody body) =>
            body != null && !(body.IsStatic && !_canInteractWithStaticBody);

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            // A static body: interact with the dynamic one.
            if (body.IsStatic && _canInteractWithStaticBody)
            {
                var dynamic = PhysicsBodies.Find(b => !b.IsStatic);
                if (dynamic != null) body = dynamic;
            }
            if (_joints.Find(j => j.childBody == body) == null) HplLog.Error($"Could not find swing door data for body '{body.Name}'");

            if (Locked && _interactSoundCount <= 0f)
            {
                // Locked interact sound: step 6.
                _interactSoundCount = 0.5f;
            }
            if (!Locked) SetClosed(false, true);
            DisableAutoClose = false;

            if (!Locked && ShowHints) Map.hud.Hints.Add("EntitySwingDoor", Map.hud.Translate("Hints", "EntitySwingDoor"), 0f);

            PlayerStateVars.SetupInteraction(body, hplFocusPos);
            Map.player.States.ChangeState(PlayerStateType.InteractSwingDoor);
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) =>
            CanInteract(body) ? FocusCrosshair.Grab : FocusCrosshair.Default;

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_interactSoundCount > 0f) _interactSoundCount -= timeStep;

            // Close when near 0°.
            if (!Closed && !IsInteractedWith && !DisableAutoClose && Joints.Count == 1)
                foreach (var j in _joints)
                    if (Mathf.Abs(j.hinge.GetAngle()) < 10f * Mathf.Deg2Rad) SetClosed(true, true);
        }

        public void SetClosed(bool closed, bool effects)
        {
            if (Closed == closed) return;
            Closed = closed;
            foreach (var j in _joints)
            {
                if (Closed)
                {
                    j.hinge.SetMaxAngle(2f * Mathf.Deg2Rad);
                    j.hinge.StickyMinLimit = false;
                    j.hinge.StickyMaxLimit = false;
                }
                else
                {
                    j.hinge.SetMaxAngle(j.maxAngle);
                    j.hinge.StickyMinLimit = true;
                    j.hinge.StickyMaxLimit = true;
                }
            }
            // A small force so that the door really closes.
            foreach (var body in PhysicsBodies) body.AddForce(HplSpace.Direction(Vector3.one));
            // Close on / off sounds: step 6.
        }

        public void SetLocked(bool locked, bool effects)
        {
            if (Locked == locked) return;
            Locked = locked;
            if (Locked) SetClosed(true, false);
            // Lock sounds: step 6.
        }

        /// <summary>cLuxProp_SwingDoor::GetDoorState: -1 closed (all within 5°), 1 open (past 70% of max), 0 between.</summary>
        public int GetDoorState()
        {
            int state = 0;
            foreach (var j in _joints)
            {
                float abs = Mathf.Abs(j.hinge.GetAngle());
                float closedAngle = 5f * Mathf.Deg2Rad;
                float openAngle = Mathf.Abs(j.maxAngle) * 0.7f;
                if (abs < closedAngle)
                {
                    if (state == 1) return 0;
                    state = -1;
                }
                else if (abs > openAngle)
                {
                    if (state == -1) return 0;
                    state = 1;
                }
                else return 0;
            }
            return state;
        }

        public override void OnConnectionStateChange(HplEntity entity, int state) => SetLocked(state > 0, true);

        /// <summary>The door turned to the open amount × max angle about its hinge; closed if near 0.</summary>
        void SetupDoorPhysics(float openAmount)
        {
            if (openAmount > 0f)
            {
                foreach (var j in _joints)
                {
                    float wanted = j.maxAngle * openAmount;
                    // The hinge angle is the child's rotation about the (Unity) pin.
                    Quaternion rotation = Quaternion.AngleAxis(wanted * Mathf.Rad2Deg, j.hinge.PinDir);
                    Vector3 pivot = j.hinge.PivotPoint;
                    var body = j.childBody;
                    body.SetPose(pivot + rotation * (body.Position - pivot), rotation * body.Rotation);
                }
            }
            if (Joints.Count == 1)
                foreach (var j in _joints)
                    if (Mathf.Abs(j.hinge.GetAngle()) < 10f * Mathf.Deg2Rad) SetClosed(true, false);
        }

        protected override void OnHealthChange()
        {
            if (!_breakable || _disableBreakable || _broken) return;
            if (Health <= 0f)
            {
                _broken = true;
                HplPhysicsBody main = null;
                foreach (var body in PhysicsBodies)
                {
                    if (body.IsStatic) continue;
                    main = body;
                    body.Active = false;
                    foreach (var r in body.Data.GetComponentsInChildren<Renderer>()) r.enabled = false;
                }
                if (main != null && _brokenEntity != "")
                {
                    Matrix4x4 local = Matrix4x4.TRS(main.LoadLocalPosition, main.LoadLocalRotation, Vector3.one);
                    Matrix4x4 pose = Matrix4x4.TRS(main.Position, main.Rotation, Vector3.one) * local.inverse;
                    if (Map.CreateEntity(Name + "_broken", _brokenEntity, pose.GetPosition(), pose.rotation, Component.scale) is HplProp prop)
                        foreach (var b in prop.PhysicsBodies)
                            b.AddImpulse((b.WorldCenterOfMass - pose.GetPosition()).normalized * _breakImpulse + main.LinearVelocity);
                }
                RunCallbackFunc("Break");
            }
            else if (Health < _healthDamage[1] && _currentMeshEntity <= 1) _currentMeshEntity = 2; // damage meshes: later
            else if (Health < _healthDamage[0] && _currentMeshEntity == 0) _currentMeshEntity = 1;
        }
    }
}
