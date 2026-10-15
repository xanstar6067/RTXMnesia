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
    /// cLuxProp_Object (LuxProp_Object.cpp): grabbable, pushable, sliding (drawers) or static objects; breaking by
    /// impact energy or health; the stuck state of its joints (SetPropObjectStuckState).
    /// Not yet: food attraction (enemies, step 6), insanity visions (step 6), life length fade.
    /// </summary>
    public sealed class PropObject : HplProp
    {
        public enum ObjectType { Static, Grab, Push, Slide }

        public ObjectType ObjType { get; private set; } = ObjectType.Grab;

        readonly InteractDataGrab _grab = new();
        readonly InteractDataPush _push = new();
        readonly InteractDataSlide _slide = new();
        bool _grabSkipNonOuterBodies;

        // Breaking
        bool _breakActive, _breakDestroyJoints, _disableBreakable, _broken;
        float _breakMinEnergy, _breakImpulse, _lifeLength, _lifeLengthCount;
        string _breakEntity = "", _breakEntityAlignBody = "", _breakSound = "", _breakParticleSystem = "";
        string _containedItem = "";
        float _hitDamageAmount, _minHitDamageSpeed;
        int _hitDamageStrength;

        int _stuckState;
        readonly List<(float min, float max, float lockedRange)> _jointData = new();

        public PropObject(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override void LoadVariables()
        {
            ObjType = GetObjectType(SubType);
            _lifeLength = VarFloat("LifeLength", 0f);

            _breakActive = VarBool("BreakActive", false);
            _breakDestroyJoints = VarBool("BreakDestroyJoints", false);
            _breakMinEnergy = VarFloat("BreakMinEnergy", 1000f);
            _breakEntity = VarString("BreakEntity", "");
            _breakEntityAlignBody = VarString("BreakEntityAlignBody", "");
            _breakSound = VarString("BreakSound", "");
            _breakParticleSystem = VarString("BreakParticleSystem", "");
            _breakImpulse = VarFloat("BreakImpulse", 3f);
            _hitDamageAmount = VarFloat("HitDamageAmount", 5f);
            _hitDamageStrength = VarInt("HitDamageStrength", 1);
            _minHitDamageSpeed = VarFloat("MinHitDamageSpeed", 3.5f);

            switch (ObjType)
            {
                case ObjectType.Grab:
                    _grab.grabUseDepth = VarBool("GrabUseDepth", false);
                    _grab.grabDepth = VarFloat("GrabDepth", 1.5f);
                    _grab.grabDepthInc = VarFloat("GrabDepthInc", 0.1f);
                    _grab.grabMinDepth = VarFloat("GrabMinDepth", 1f);
                    _grab.grabMaxDepth = VarFloat("GrabMaxDepth", 2f);
                    _grab.grabPositionOffset = VarVector3("GrabPositionOffset", Vector3.zero);
                    _grab.grabRotationOffset = VarVector3("GrabRotationOffset", Vector3.zero) * Mathf.Deg2Rad;
                    _grab.grabThrowImpulse = VarFloat("GrabThrowImpulse", 10f);
                    _grab.grabUseOffset = VarBool("GrabUseOffset", false);
                    _grab.grabMassMul = VarFloat("GrabMassMul", 0.1f);
                    _grab.forceMul = VarFloat("GrabForceMul", 1f);
                    _grab.torqueMul = VarFloat("GrabTorqueMul", 1f);
                    _grab.useRotation = VarBool("GrabUseRotation", true);
                    _grabSkipNonOuterBodies = VarBool("GrabSkipNonOuterBodies", false);
                    if (MaxFocusDistance <= 0f) MaxFocusDistance = ConfigFloat("Grab_DefaultMaxFocusDist");
                    break;
                case ObjectType.Push:
                    _push.pushAtPoint = VarBool("PushAtPoint", true);
                    _push.pushForceMul = VarFloat("PushForceMul", 1f);
                    _push.pushImpulse = VarFloat("PushImpulse", 2f);
                    if (MaxFocusDistance <= 0f) MaxFocusDistance = ConfigFloat("Push_DefaultMaxFocusDist");
                    break;
                case ObjectType.Slide:
                    _slide.slideMaxSpeed = VarFloat("SlideMaxSpeed", 5f);
                    _slide.slideSlowDownFactor = VarFloat("SlideSlowDownFactor", 1f);
                    _slide.slideSpeedFactor = VarFloat("SlideSpeedFactor", 1f);
                    _slide.slideThrowImpulse = VarFloat("SlideThrowImpulse", 3f);
                    if (MaxFocusDistance <= 0f) MaxFocusDistance = ConfigFloat("Slide_DefaultMaxFocusDist");
                    break;
            }
        }

        protected override void LoadInstanceVariables()
        {
            _disableBreakable = InstanceBool("DisableBreakable", false);
            _containedItem = InstanceString("ContainedItem", "");
            if (_containedItem == "None") _containedItem = "";
            // IsInsanityVision / VisionMaxSanity: step 6.
        }

        ObjectType GetObjectType(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "static": return ObjectType.Static;
                case "grab": return ObjectType.Grab;
                case "push": return ObjectType.Push;
                case "slide": return ObjectType.Slide;
            }
            HplLog.Error($"Object type '{name}' in '{Component.file}' does not exist!");
            return ObjectType.Grab;
        }

        protected override void OnSetupAfterLoad()
        {
            foreach (var body in PhysicsBodies)
            {
                var contacts = body.Contacts;
                contacts.Collided -= OnBodyCollide;
                contacts.Collided += OnBodyCollide;
            }
            foreach (var joint in Joints)
            {
                switch (joint.Type)
                {
                    case HplJointType.Hinge: _jointData.Add((joint.MinAngle, joint.MaxAngle, 5f * Mathf.Deg2Rad)); break;
                    case HplJointType.Slider: _jointData.Add((joint.MinDistance, joint.MaxDistance, 0.1f * Mathf.Deg2Rad)); break;
                    default: _jointData.Add((0f, 0f, 0f)); break;
                }
            }
        }

        public override InteractDataGrab GetGrabData() => _grab;
        public override InteractDataPush GetPushData() => _push;
        public override InteractDataSlide GetSlideData() => _slide;
        public override bool ShowOutlinesOnConnectedBodies => ObjType == ObjectType.Grab;

        public override bool CanInteract(HplPhysicsBody body)
        {
            if (body == null) return false;
            if (ObjType == ObjectType.Grab && _grabSkipNonOuterBodies && body.Joints.Count >= 2) return false;
            // Sticky areas: step 4.4 / 6.
            if ((body.IsStatic && InteractCallback == "") || (ObjType == ObjectType.Static && InteractCallback == "")) return false;
            return true;
        }

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            var interactBody = ExplicitMainBody ?? body;
            var hud = Map.hud;
            switch (ObjType)
            {
                case ObjectType.Grab:
                    hud.Hints.Add("EntityGrab", hud.Translate("Hints", "EntityGrab01"), 0f);
                    hud.Hints.Add("EntityGrab", hud.Translate("Hints", "EntityGrab02"), 0f);
                    PlayerStateVars.SetupInteraction(interactBody, hplFocusPos);
                    Map.player.States.ChangeState(PlayerStateType.InteractGrab);
                    break;
                case ObjectType.Push:
                    hud.Hints.Add("EntityPush", hud.Translate("Hints", "EntityPush"), 0f);
                    PlayerStateVars.SetupInteraction(interactBody, hplFocusPos);
                    Map.player.States.ChangeState(PlayerStateType.InteractPush);
                    break;
                case ObjectType.Slide:
                    hud.Hints.Add("EntitySlide", hud.Translate("Hints", "EntitySlide"), 0f);
                    PlayerStateVars.SetupInteraction(interactBody, hplFocusPos);
                    Map.player.States.ChangeState(PlayerStateType.InteractSlide);
                    break;
            }
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (!CanInteract(body)) return FocusCrosshair.Default;
            switch (ObjType)
            {
                case ObjectType.Push:
                    return CustomFocusCrossHair == FocusCrosshair.Default ? FocusCrosshair.Push : CustomFocusCrossHair;
                default:
                    return CustomFocusCrossHair == FocusCrosshair.Default ? FocusCrosshair.Grab : CustomFocusCrossHair;
            }
        }

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_lifeLength > 0f)
            {
                _lifeLengthCount += timeStep;
                // Mesh coverage fade over the last second: dissolve materials come later.
                if (_lifeLengthCount >= _lifeLength) Break();
            }
        }

        // ------------------------------------------------------------------ breaking

        /// <summary>
        /// cLuxProp_Object_BodyCallback::OnBodyCollide: the mass × velocity towards the impact of both bodies
        /// (velocities before the step) above BreakMinEnergy breaks the object. Hitting enemies: step 6.
        /// </summary>
        void OnBodyCollide(HplPhysicsBody body, Collision collision)
        {
            if (!_breakActive || _broken) return;
            if (collision.contactCount == 0) return;
            var contact = collision.GetContact(0);
            Vector3 point = contact.point;
            Vector3 normal = contact.normal;
            float energy = 0f;
            var other = PhysicsWorld.BodyOf(collision.rigidbody);
            foreach (var b in new[] { body, other })
            {
                if (b == null || b.IsStatic) continue;
                Vector3 center = b.WorldCenterOfMass;
                Vector3 towards = Vector3.Dot(point - center, normal) > 0f ? normal : -normal;
                Vector3 vel = b.PreviousVelocityAt(point);
                energy += Mathf.Abs(Vector3.Dot(towards, vel)) * b.Mass;
            }
            if (energy > _breakMinEnergy) Break();
        }

        public void Break()
        {
            if (_broken) return;
            _broken = true;
            Map.DestroyEntity(this);
        }

        protected override void OnHealthChange()
        {
            if (Health <= 0f && _breakActive) Break();
        }

        /// <summary>cLuxProp_Object::BeforePropDestruction: the broken entity, connected props, contained item, effects.</summary>
        internal override void BeforeDestruction()
        {
            if (!_broken || !_breakActive || PhysicsBodies.Count == 0 || _disableBreakable) return;

            int index = 0;
            if (_breakEntityAlignBody != "")
            {
                index = GetBodyIndexFromName(_breakEntityAlignBody);
                if (index < 0)
                {
                    index = 0;
                    HplLog.Warn($"Body '{_breakEntityAlignBody}' was not found in object '{Name}'");
                }
            }
            var baseBody = PhysicsBodies[index];
            Vector3 center = baseBody.Position;

            if (_breakDestroyJoints)
            {
                foreach (var joint in Joints) joint.Break();
                Vector3 sum = Vector3.zero;
                foreach (var b in PhysicsBodies) sum += b.Position;
                center = sum / PhysicsBodies.Count;
                // Kept: the original's impulse direction is (center - center), i.e. zero.
            }
            else if (_breakEntity != "")
            {
                // The entity whose same body is where this one is.
                Matrix4x4 bodyPose = Matrix4x4.TRS(baseBody.Position, baseBody.Rotation, Vector3.one);
                Matrix4x4 local = Matrix4x4.TRS(baseBody.LoadLocalPosition, baseBody.LoadLocalRotation, Vector3.one);
                Matrix4x4 entityPose = bodyPose * local.inverse;
                var created = Map.CreateEntity(Name + "_broken", _breakEntity, entityPose.GetPosition(), entityPose.rotation, Component.scale);
                if (created is HplProp prop)
                {
                    foreach (var b in prop.PhysicsBodies)
                    {
                        Vector3 dir = (b.WorldCenterOfMass - entityPose.GetPosition()).normalized;
                        b.AddImpulse(dir * _breakImpulse + baseBody.LinearVelocity);
                    }
                }
            }

            foreach (string name in ConnectedProps)
            {
                if (Map.GetEntityByName(name) is not HplProp prop)
                {
                    HplLog.Error($"Could not find prop '{name}' which is connected to '{Name}'");
                    continue;
                }
                prop.SetActive(true);
                foreach (var b in prop.PhysicsBodies)
                {
                    Vector3 dir = (b.WorldCenterOfMass - center).normalized;
                    b.AddImpulse(dir * _breakImpulse + baseBody.LinearVelocity);
                }
            }

            if (_containedItem != "")
                Map.CreateEntity(Name + "_item", _containedItem, center, Quaternion.identity, Vector3.one);

            PlaySoundAt(Name + "_BreakSound", _breakSound, center);
            if (_breakParticleSystem != "")
            {
                var ps = Map.ParticleWorld.Create(Name + "_BreakPS", _breakParticleSystem);
                if (ps != null)
                    ps.LocalMatrix = _breakDestroyJoints ? Matrix4x4.Translate(center) : Matrix4x4.TRS(baseBody.Position, baseBody.Rotation, Vector3.one);
            }
            RunCallbackFunc("Break");
        }

        // ------------------------------------------------------------------ stuck state

        /// <summary>cLuxProp_Object::SetStuckState: 1 / -1 keeps joints near max / min, 0 frees them.</summary>
        public void SetStuckState(int state)
        {
            if (_stuckState == state) return;
            _stuckState = state;
            for (int i = 0; i < Joints.Count && i < _jointData.Count; i++)
            {
                var (min, max, locked) = _jointData[i];
                if (_stuckState == 0) SetJointMinMax(i, min, max);
                else if (_stuckState == 1) SetJointMinMax(i, max - locked, max);
                else if (_stuckState == -1) SetJointMinMax(i, min, min + locked);
                // If sleeping, make sure it moves.
                Joints[i].Child.AddForce(HplSpace.Direction(Vector3.one));
            }
        }

        void SetJointMinMax(int index, float min, float max)
        {
            var joint = Joints[index];
            if (joint.Type == HplJointType.Hinge)
            {
                // Kept from the original: both limits get the min.
                joint.SetMinAngle(min);
                joint.SetMaxAngle(min);
            }
            else if (joint.Type == HplJointType.Slider)
            {
                joint.SetMinDistance(min);
                joint.SetMaxDistance(max);
            }
        }
    }
}
