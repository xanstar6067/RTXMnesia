using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_Lever (LuxProp_Lever.cpp): a hinged lever with three states (-1 min, 0 middle, 1 max) sent to its
    /// connections; it can get stuck at a limit and moves back to its rest angle by itself when let go.
    /// Limit and stuck sounds: step 6.
    /// </summary>
    public sealed class PropLever : HplProp
    {
        readonly InteractDataRotate _data = new();
        bool _canInteractWithStaticBody = true;
        float _minLimitRange, _maxLimitRange;
        bool _minLimitStuck, _maxLimitStuck;
        float _middleAngleAmount, _middleAngle;
        bool _autoMoveToAngle = true;
        int _autoMoveGoal;
        float _autoMoveSpeedFactor, _autoMoveMaxSpeed;
        float _defaultMinAngle, _defaultMaxAngle;
        bool _interactionDisablesStuck;
        float _stuckSoundTimer;
        HplJoint _hinge;
        HplPhysicsBody _leverBody;
        readonly HplPid _rotatePid = new(10f, 0f, 1f, 10);

        public int CurrentState { get; private set; }
        public int StuckState { get; private set; }
        public bool InteractionDisablesStuck { get => _interactionDisablesStuck; set => _interactionDisablesStuck = value; }

        public PropLever(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override string DefaultFocusDistanceKey => "Lever_DefaultMaxFocusDist";

        static int ToAutoMoveGoal(string s) => s switch { "Max" => 1, "Min" => -1, _ => 0 };
        static int ToStuckState(string s) => s switch { "Min" => -1, "Max" => 1, _ => 0 };

        protected override void LoadVariables()
        {
            _data.moveMaxSpeed = VarFloat("MoveMaxSpeed", 13.5f);
            _data.moveSlowDownFactor = VarFloat("MoveSlowDownFactor", 3f);
            _data.moveSpeedFactor = VarFloat("MoveSpeedFactor", 1f);
            _data.moveThrowImpulse = VarFloat("MoveThrowImpulse", 6f);
            _canInteractWithStaticBody = VarBool("CanInteractWithStaticBody", true);
            _minLimitRange = VarFloat("MinLimitRange", 5f) * Mathf.Deg2Rad;
            _maxLimitRange = VarFloat("MaxLimitRange", 5f) * Mathf.Deg2Rad;
            _minLimitStuck = VarBool("MinLimitStuck", false);
            _maxLimitStuck = VarBool("MaxLimitStuck", false);
            _middleAngleAmount = VarFloat("MiddleAngleAmount", 0f);
            _autoMoveToAngle = VarBool("AutoMoveToAngle", true);
            _autoMoveGoal = ToAutoMoveGoal(VarString("AutoMoveGoal", "Middle"));
            _autoMoveSpeedFactor = VarFloat("AutoMoveSpeedFactor", 2f);
            _autoMoveMaxSpeed = VarFloat("AutoMoveMaxSpeed", 8f);
        }

        protected override void OnSetupAfterLoad()
        {
            if (Joints.Count == 0 || Joints[0].Type != HplJointType.Hinge)
            {
                HplLog.Error($"Lever {Name} does not have a hinge joint!");
                return;
            }
            _hinge = Joints[0];
            _leverBody = _hinge.Child;
            if (_autoMoveToAngle) _leverBody.Gravity = false;
            _hinge.StickyMinLimit = true;
            _hinge.StickyMaxLimit = true;
            _defaultMinAngle = _hinge.MinAngle;
            _defaultMaxAngle = _hinge.MaxAngle;
            CalculateMiddleAngle();
        }

        protected override void LoadInstanceVariables()
        {
            SetStuckState(ToStuckState(InstanceString("StuckState", "")), false);
            _interactionDisablesStuck = InstanceBool("InteractionDisablesStuck", false);
            if (InstanceBool("OverrideDefaults", false))
            {
                _middleAngleAmount = InstanceFloat("MiddleAngleAmount", 0f);
                _autoMoveToAngle = InstanceBool("AutoMoveToAngle", true);
                _autoMoveGoal = ToAutoMoveGoal(InstanceString("AutoMoveGoal", "Middle"));
                CalculateMiddleAngle();
            }
        }

        public override InteractDataRotate GetMoveBaseData() => _data;

        public override bool CanInteract(HplPhysicsBody body) => body != null && !(body.IsStatic && !_canInteractWithStaticBody);

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (body.IsStatic && _canInteractWithStaticBody)
            {
                var dynamic = PhysicsBodies.Find(b => b.Mass > 0f);
                if (dynamic != null) body = dynamic;
            }
            if (StuckState != 0 && _interactionDisablesStuck) SetStuckState(0, true);
            if (StuckState == 0 && ShowHints) Map.hud.Hints.Add("EntityLever", Map.hud.Translate("Hints", "EntityLever"), 0f);
            PlayerStateVars.SetupInteraction(body, hplFocusPos);
            Map.player.States.ChangeState(PlayerStateType.InteractLever);
            _rotatePid.Reset();
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) =>
            body != null && body.IsStatic && !_canInteractWithStaticBody ? FocusCrosshair.Default : FocusCrosshair.Grab;

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_hinge == null) return;
            float angle = _hinge.GetAngle();
            if (StuckState != 0 && IsInteractedWith && _stuckSoundTimer > 0f) _stuckSoundTimer -= timeStep; // stuck sound: step 6
            UpdateCheckLimit(angle);
            UpdateAutoMove(angle, timeStep);
        }

        public void SetStuckState(int state, bool effects)
        {
            if (StuckState == state || _hinge == null) return;
            StuckState = state;
            _stuckSoundTimer = 0f;
            if (StuckState == 1)
            {
                ChangeState(1, effects);
                _hinge.SetMinAngle(_defaultMaxAngle - _maxLimitRange);
                _hinge.SetMaxAngle(_defaultMaxAngle);
            }
            else if (StuckState == -1)
            {
                ChangeState(-1, effects);
                _hinge.SetMinAngle(_defaultMinAngle);
                _hinge.SetMaxAngle(_defaultMinAngle + _minLimitRange);
            }
            else
            {
                _hinge.SetMinAngle(_defaultMinAngle);
                _hinge.SetMaxAngle(_defaultMaxAngle);
            }
        }

        public override void OnConnectionStateChange(HplEntity entity, int state)
        {
            if (state > 0) SetStuckState(-1, true);
            else SetStuckState(0, true);
        }

        void CalculateMiddleAngle()
        {
            float t = (_middleAngleAmount + 1f) / 2f;
            _middleAngle = _defaultMinAngle + (_defaultMaxAngle - _defaultMinAngle) * t;
        }

        void UpdateCheckLimit(float angle)
        {
            if (StuckState != 0) return;
            if (angle > _defaultMaxAngle - _maxLimitRange)
            {
                ChangeState(1, true);
                if (_maxLimitStuck) SetStuckState(1, false);
            }
            else if (angle < _defaultMinAngle + _minLimitRange)
            {
                ChangeState(-1, true);
                if (_minLimitStuck) SetStuckState(-1, false);
            }
            else if (angle < _defaultMaxAngle - _maxLimitRange - 5f * Mathf.Deg2Rad ||
                     angle > _defaultMinAngle + _minLimitRange + 5f * Mathf.Deg2Rad)
            {
                ChangeState(0, true);
            }
        }

        /// <summary>
        /// Back to the rest angle when not held. HPL2: a positive angular velocity about the pin lowers the angle,
        /// so the wanted speed is factor × (angle - goal) about the HPL2 pin.
        /// </summary>
        void UpdateAutoMove(float angle, float timeStep)
        {
            if (IsInteractedWith || !_autoMoveToAngle || StuckState != 0) return;
            float goal = _autoMoveGoal == 0 ? _middleAngle
                : _autoMoveGoal == -1 ? _defaultMinAngle + _minLimitRange / 2f
                : _defaultMaxAngle - _maxLimitRange / 2f;
            float wantedSpeed = Mathf.Clamp(_autoMoveSpeedFactor * (angle - goal), -_autoMoveMaxSpeed, _autoMoveMaxSpeed);

            Vector3 pin = _hinge.HplPinDir;
            Vector3 bodyVel = _leverBody.HplAngularVelocity;
            Vector3 hingeVel = pin * Vector3.Dot(pin, bodyVel);
            Vector3 wantedVel = pin * wantedSpeed;
            _leverBody.SetHplAngularVelocity(wantedVel);
            Vector3 torque = _rotatePid.Output(wantedVel - hingeVel, timeStep);
            torque = HplMatrix.Mul3x3(_leverBody.HplInertiaMatrix, torque);
            _leverBody.AddTorqueHpl(torque);
        }

        void ChangeState(int state, bool effects)
        {
            if (CurrentState == state) return;
            CurrentState = state;
            ConnectionStateChange(CurrentState);
            // Limit sounds: step 6. Interact connections (ropes, move objects): with the connection port.
        }
    }
}
