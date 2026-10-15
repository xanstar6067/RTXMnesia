using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_Wheel (LuxProp_Wheel.cpp): a wheel turned with circular mouse moves. Its own angle accumulates
    /// over turns (the joint angle wraps at ±π); limits in that angle are mapped to joint limits near the current
    /// joint angle each step. States -1 / 1 at the limits go to connections; stuck states; auto move to an angle.
    /// Limit and stuck sounds as the original.
    /// </summary>
    public sealed class PropWheel : HplProp
    {
        readonly InteractDataRotate _data = new();
        bool _canInteractWithStaticBody = true;
        float _minLimit, _maxLimit, _minLimitRange, _maxLimitRange;
        bool _minLimitStuck, _maxLimitStuck, _slowDownRotation = true, _skipMiddleState = true;
        int _spinDir;
        float _angle, _prevAngle, _jointAngle, _prevJointAngle, _lastToMin, _lastToMax;
        float _stuckSoundTimer;
        bool _autoMoving;
        float _autoMoveGoal;
        HplJoint _hinge;
        HplPhysicsBody _wheelBody;
        readonly HplPid _rotatePid = new(10f, 0f, 0.5f, 10);

        public int CurrentState { get; private set; }
        public int StuckState { get; private set; }
        public bool InteractionDisablesStuck;

        public PropWheel(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        protected override string DefaultFocusDistanceKey => "Wheel_DefaultMaxFocusDist";

        static int ToSpinDir(string s) => s switch { "ToMax" => 1, "ToMin" => -1, _ => 0 };
        static int ToStuckState(string s) => s switch { "Min" => -1, "Max" => 1, _ => 0 };

        protected override void LoadVariables()
        {
            _data.moveMaxSpeed = VarFloat("MoveMaxSpeed", 13.5f);
            _data.moveSlowDownFactor = VarFloat("MoveSlowDownFactor", 3f);
            _data.moveSpeedFactor = VarFloat("MoveSpeedFactor", 1f);
            _data.moveThrowImpulse = VarFloat("MoveThrowImpulse", 6f);
            _canInteractWithStaticBody = VarBool("CanInteractWithStaticBody", true);
            _minLimit = VarFloat("MinLimit", -360f) * Mathf.Deg2Rad;
            _maxLimit = VarFloat("MaxLimit", 360f) * Mathf.Deg2Rad;
            _minLimitRange = VarFloat("MinLimitRange", 5f) * Mathf.Deg2Rad;
            _maxLimitRange = VarFloat("MaxLimitRange", 5f) * Mathf.Deg2Rad;
            _minLimitStuck = VarBool("MinLimitStuck", false);
            _maxLimitStuck = VarBool("MaxLimitStuck", false);
            _spinDir = ToSpinDir(VarString("SpinDir", "BothWays"));
            _slowDownRotation = VarBool("SlowDownRotation", true);
            _skipMiddleState = VarBool("SkipMiddleState", true);
            _minLimitSound = VarString("MinLimitSound", "");
            _maxLimitSound = VarString("MaxLimitSound", "");
            _stuckSound = VarString("StuckSound", "");
        }

        string _minLimitSound = "", _maxLimitSound = "", _stuckSound = "";

        protected override void OnSetupAfterLoad()
        {
            if (Joints.Count == 0 || Joints[0].Type != HplJointType.Hinge)
            {
                HplLog.Error($"Wheel {Name} does not have a hinge joint!");
                return;
            }
            _hinge = Joints[0];
            _wheelBody = _hinge.Child;
            _wheelBody.Gravity = false;
            if (_spinDir == 0)
            {
                _hinge.StickyMinLimit = true;
                _hinge.StickyMaxLimit = true;
            }
            _hinge.SetMinAngle(0f);
            _hinge.SetMaxAngle(0f);
        }

        protected override void LoadInstanceVariables()
        {
            if (InstanceBool("OverrideDefaults", false))
            {
                _minLimit = InstanceFloat("MinLimit", -360f) * Mathf.Deg2Rad;
                _maxLimit = InstanceFloat("MaxLimit", 360f) * Mathf.Deg2Rad;
                _minLimitStuck = InstanceBool("MinLimitStuck", false);
                _maxLimitStuck = InstanceBool("MaxLimitStuck", false);
                _spinDir = ToSpinDir(InstanceString("SpinDir", "BothWays"));
            }
            SetStuckState(ToStuckState(InstanceString("StuckState", "")), false);
            InteractionDisablesStuck = InstanceBool("InteractionDisablesStuck", false);
        }

        public override InteractDataRotate GetMoveBaseData() => _data;

        public override bool CanInteract(HplPhysicsBody body) => body != null && !(body.IsStatic && !_canInteractWithStaticBody);

        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos)
        {
            if (body.IsStatic && _canInteractWithStaticBody) body = _wheelBody;
            ResetAutoMove();
            if (StuckState != 0 && InteractionDisablesStuck) SetStuckState(0, true);
            if (StuckState == 0 && ShowHints) Map.hud.Hints.Add("EntityWheel", Map.hud.Translate("Hints", "EntityWheel"), 0f);
            PlayerStateVars.SetupInteraction(body, hplFocusPos);
            Map.player.States.ChangeState(PlayerStateType.InteractWheel);
            return true;
        }

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) =>
            body != null && body.IsStatic && !_canInteractWithStaticBody ? FocusCrosshair.Default : FocusCrosshair.Grab;

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_hinge == null) return;
            if (_stuckSoundTimer > 0f) _stuckSoundTimer -= timeStep;
            UpdateAngle(timeStep);
            UpdateCheckLimit();
            UpdateAutoRotation(timeStep);
        }

        public void SetStuckState(int state, bool effects)
        {
            if (StuckState == state || _hinge == null) return;
            StuckState = state;
            _stuckSoundTimer = 0f;
            if (StuckState != 0)
            {
                _angle = StuckState == 1 ? _maxLimit - _maxLimitRange / 2f : _minLimit + _minLimitRange / 2f;
                _prevAngle = _angle;
                float a = _hinge.GetAngle();
                _hinge.SetMaxAngle(Mathf.Min(a + Mathf.Deg2Rad, Mathf.PI));
                _hinge.SetMinAngle(Mathf.Max(a - Mathf.Deg2Rad, -Mathf.PI));
                ChangeState(StuckState, effects);
                ResetAutoMove();
            }
            else
            {
                _hinge.SetMinAngle(0f);
                _hinge.SetMaxAngle(0f);
            }
        }

        /// <summary>cLuxProp_Wheel::SetAngle (SetWheelAngle): jump to an angle, or turn there by itself.</summary>
        public void SetAngle(float x, bool autoMoveToAngle)
        {
            if (x == _angle) return;
            float wanted = Mathf.Clamp(x, _minLimit, _maxLimit);
            if (autoMoveToAngle) { if (wanted == _angle) return; }
            else if (Mathf.Abs(wanted - _angle) < 0.0001f) return;

            if (autoMoveToAngle)
            {
                _autoMoving = true;
                _autoMoveGoal = wanted;
                _rotatePid.Reset();
            }
            else
            {
                _angle = wanted;
                _prevAngle = _angle;
                ResetAutoMove();
            }
        }

        public override void OnConnectionStateChange(HplEntity entity, int state)
        {
            if (state > 0) SetStuckState(-1, true);
            else SetStuckState(0, true);
        }

        void UpdateAngle(float timeStep)
        {
            _jointAngle = _hinge.GetAngle();
            float add = HplMatrix.AngleDistanceRad(_prevJointAngle, _jointAngle);
            _prevAngle = _angle;
            _angle += add;
            _prevJointAngle = _jointAngle;

            // Moving while stuck: the stuck sound.
            if (StuckState != 0 && IsInteractedWith && _stuckSoundTimer <= 0f && Mathf.Abs(add) > 0.01f * timeStep)
            {
                PlaySound("WheelStuck", _stuckSound, true, true);
                _stuckSoundTimer = 1.5f;
            }

            float spinVal = 0f;
            if (_spinDir == 1 && _angle > _lastToMax)
            {
                _lastToMax = _angle;
                spinVal = _hinge.GetAngle();
            }
            if (_spinDir == -1 && _angle < _lastToMin)
            {
                _lastToMin = _angle;
                spinVal = _hinge.GetAngle();
            }

            if (_maxLimit == 0f && _minLimit == 0f) return;
            if (StuckState == 0)
            {
                if (_spinDir == 1)
                {
                    SetMaxJointAngle(_maxLimit);
                    _hinge.SetMinAngle(spinVal - 0.5f * Mathf.Deg2Rad);
                }
                else if (_spinDir == -1)
                {
                    SetMinJointAngle(_minLimit);
                    _hinge.SetMaxAngle(spinVal + 0.5f * Mathf.Deg2Rad);
                }
                else
                {
                    SetMaxJointAngle(_maxLimit);
                    SetMinJointAngle(_minLimit);
                }
            }
            // Interact connections (ropes, move objects): with the connection port.
        }

        void UpdateCheckLimit()
        {
            if (StuckState != 0) return;
            if (_maxLimit == 0f && _minLimit == 0f) return;
            if (_angle > _maxLimit - _maxLimitRange)
            {
                ChangeState(1, true);
                if (_maxLimitStuck) SetStuckState(1, false);
            }
            else if (_angle < _minLimit + _minLimitRange)
            {
                ChangeState(-1, true);
                if (_minLimitStuck) SetStuckState(-1, false);
            }
            else ChangeState(0, true);
        }

        void UpdateAutoRotation(float timeStep)
        {
            if (_autoMoving)
            {
                if (Mathf.Abs(_angle - _autoMoveGoal) < 0.1f * Mathf.Deg2Rad) ResetAutoMove();
                else
                {
                    float wantedSpeed = (_angle - _autoMoveGoal) * 10f + (_prevAngle - _angle) * timeStep * 1f;
                    Vector3 pin = _hinge.HplPinDir;
                    Vector3 bodyVel = _wheelBody.HplAngularVelocity;
                    Vector3 hingeVel = pin * Vector3.Dot(pin, bodyVel);
                    Vector3 torque = _rotatePid.Output(pin * wantedSpeed - hingeVel, timeStep);
                    _wheelBody.AddTorqueHpl(HplMatrix.Mul3x3(_wheelBody.HplInertiaMatrix, torque));
                }
            }
            if (IsInteractedWith) return;
            if (!_autoMoving && _slowDownRotation)
            {
                Vector3 bodyVel = _wheelBody.HplAngularVelocity;
                Vector3 torque = _rotatePid.Output(-bodyVel, timeStep);
                _wheelBody.AddTorqueHpl(HplMatrix.Mul3x3(_wheelBody.HplInertiaMatrix, torque));
            }
        }

        void SetMaxJointAngle(float maxAngle)
        {
            float dist = maxAngle - _angle;
            if (dist > Mathf.PI)
            {
                _hinge.SetMaxAngle(Mathf.PI * 2f);
                return;
            }
            float jointMax = _jointAngle + dist;
            if ((_jointAngle >= 0f && jointMax <= Mathf.PI) || (_jointAngle < 0f && jointMax < 0f)) _hinge.SetMaxAngle(jointMax);
            else _hinge.SetMaxAngle(Mathf.PI * 2f);
        }

        void SetMinJointAngle(float minAngle)
        {
            float dist = _angle - minAngle;
            if (dist > Mathf.PI)
            {
                _hinge.SetMinAngle(-Mathf.PI * 2f);
                return;
            }
            float jointMin = _jointAngle - dist;
            if ((_jointAngle >= 0f && jointMin >= 0f) || (_jointAngle < 0f && jointMin > -Mathf.PI)) _hinge.SetMinAngle(jointMin);
            else _hinge.SetMinAngle(-Mathf.PI * 2f);
        }

        void ChangeState(int state, bool effects)
        {
            if (CurrentState == state) return;
            if (_skipMiddleState && state == 0) return;
            CurrentState = state;
            string sound = "";
            if (CurrentState == 1)
            {
                sound = _maxLimitSound;
                ConnectionStateChange(1);
            }
            else if (CurrentState == -1)
            {
                sound = _minLimitSound;
                ConnectionStateChange(-1);
            }
            if (effects && sound != "") PlaySoundAt("WheelSound", sound, _hinge.PivotPoint);
        }

        void ResetAutoMove()
        {
            _autoMoving = false;
            _rotatePid.Reset();
        }
    }
}
