using RTXMnesia.Dynamics;
using RTXMnesia.Props;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// iLuxInteractConnection (LuxInteractConnections.cpp): a prop (wheel, lever) drives something while it turns or
    /// reaches a limit. Interaction only (the default): only while the player holds the prop, and then the driven
    /// thing moves the prop back when it is let go (UpdateProp). States used: 0 both limits, 1 max only, -1 min only.
    /// </summary>
    public abstract class HplInteractConnection
    {
        public readonly string Name;
        public bool InteractionOnly = true;
        protected readonly HplProp Prop;
        protected readonly bool Invert;
        protected readonly int StateUsed;

        protected HplInteractConnection(string name, HplProp prop, bool invert, int statesUsed)
        {
            Name = name;
            Prop = prop;
            Invert = invert;
            StateUsed = statesUsed;
        }

        public virtual void Update(float timeStep) { }
        public abstract void UpdateProp(float timeStep);
        public abstract void OnTurn(float angleAdd, float t);
        public abstract void OnLimit(int state);

        /// <summary>iLuxInteractConnection_SaveData::FromConnection and the type's own data.</summary>
        public virtual Save.SavedInteractConnection CaptureSave() => new()
        {
            name = Name, interactionOnly = InteractionOnly, invert = Invert, statesUsed = StateUsed,
        };

        /// <summary>CreateConnection of the save data: null (with HPL2's error) when the target is gone.</summary>
        public static HplInteractConnection FromSave(HplMap map, HplProp prop, Save.SavedInteractConnection s)
        {
            HplInteractConnection c = null;
            if (s.type == "Rope")
            {
                var rope = map.PhysicsWorld.GetRope(s.target);
                if (rope == null) HplLog.Error($"Rope '{s.target}' was not found when creating saved connection '{s.name}'!");
                else c = new HplInteractConnectionRope(s.name, prop, rope, s.speedMul, s.minSpeed, s.maxSpeed, s.invert, s.statesUsed);
            }
            else if (s.type == "MoveObject")
            {
                if (map.GetEntityByName(s.target) is not PropMoveObject moveObject)
                    HplLog.Error($"MoveObject '{s.target}' was not found when creating saved connection '{s.name}'!");
                else c = new HplInteractConnectionMoveObject(s.name, prop, moveObject, s.invert, s.statesUsed);
            }
            if (c != null) c.InteractionOnly = s.interactionOnly;
            return c;
        }
    }

    /// <summary>cLuxInteractConnection_Rope: turning or a limit runs the rope's motor to the matching length.</summary>
    public sealed class HplInteractConnectionRope : HplInteractConnection
    {
        readonly HplPhysicsRope _rope;
        readonly float _speedMul, _minSpeed, _maxSpeed;

        public HplInteractConnectionRope(string name, HplProp prop, HplPhysicsRope rope, float speedMul, float minSpeed, float maxSpeed,
                                         bool invert, int statesUsed) : base(name, prop, invert, statesUsed)
        {
            _rope = rope;
            _speedMul = speedMul;
            _minSpeed = minSpeed;
            _maxSpeed = maxSpeed;
        }

        /// <summary>The wheel follows the rope's length when it is not held.</summary>
        public override void UpdateProp(float timeStep)
        {
            if (Prop.IsInteractedWith) return;
            if (!InteractionOnly) return;
            float min = _rope.MinTotalLength, max = _rope.MaxTotalLength;
            float t = (_rope.TotalLength - min) / (max - min);
            if (Invert) t = 1f - t;
            if (Prop is PropWheel wheel)
                wheel.SetAngle(wheel.MinLimit + t * (wheel.MaxLimit - wheel.MinLimit), true);
        }

        public override void OnTurn(float angleAdd, float t)
        {
            if (Invert) t = 1f - t;
            float min = _rope.MinTotalLength, max = _rope.MaxTotalLength;
            RunMotor(min + (max - min) * t);
        }

        public override void OnLimit(int state)
        {
            if (Prop is PropWheel) return;
            if (state == 0) return;
            if (StateUsed != 0 && StateUsed != state) return;
            if (Invert) state = -state;
            float wanted = 0f;
            if (state == 1) wanted = _rope.MaxTotalLength;
            else if (state == -1) wanted = _rope.MinTotalLength;
            RunMotor(wanted);
        }

        void RunMotor(float wantedLength)
        {
            _rope.SetMotorActive(true);
            _rope.MotorWantedLength = wantedLength;
            _rope.MotorSpeedMul = _speedMul;
            _rope.MotorMinSpeed = _minSpeed;
            _rope.MotorMaxSpeed = _maxSpeed;
        }

        public override Save.SavedInteractConnection CaptureSave()
        {
            var s = base.CaptureSave();
            s.type = "Rope";
            s.target = _rope.Name;
            s.speedMul = _speedMul;
            s.minSpeed = _minSpeed;
            s.maxSpeed = _maxSpeed;
            return s;
        }
    }

    /// <summary>cLuxInteractConnection_MoveObject: turning or a limit moves the move object to the matching state.</summary>
    public sealed class HplInteractConnectionMoveObject : HplInteractConnection
    {
        readonly PropMoveObject _moveObject;
        // Makes sure the prop gets updated at the start.
        bool _propNeedsUpdate = true;

        public HplInteractConnectionMoveObject(string name, HplProp prop, PropMoveObject moveObject, bool invert, int statesUsed)
            : base(name, prop, invert, statesUsed) => _moveObject = moveObject;

        public override void UpdateProp(float timeStep)
        {
            if (!InteractionOnly || (!_moveObject.IsMoving && !_propNeedsUpdate)) return;
            _propNeedsUpdate = false;
            if (Prop is not PropWheel wheel) return;
            float t = _moveObject.GetMoveState();
            if (Invert) t = 1f - t;
            float angle = wheel.MinLimit + t * (wheel.MaxLimit - wheel.MinLimit);
            // While held, the original only computes adjusted limits (their use is commented out).
            if (!Prop.IsInteractedWith) wheel.SetAngle(angle, true);
        }

        public override void OnTurn(float angleAdd, float t)
        {
            if (Invert) t = 1f - t;
            _moveObject.MoveToState(t, 10f, 5f, 0.01f, false);
        }

        public override void OnLimit(int state)
        {
            if (Prop is PropWheel) return;
            if (state == 0) return;
            if (StateUsed != 0 && StateUsed != state) return;
            if (Invert) state = -state;
            _moveObject.MoveToState(state > 0 ? 1f : 0f);
        }

        public override Save.SavedInteractConnection CaptureSave()
        {
            var s = base.CaptureSave();
            s.type = "MoveObject";
            s.target = _moveObject.Name;
            return s;
        }
    }
}
