using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>iLuxPlayerState_Interact: the prop and body being interacted with, the focus point (HPL2 space).</summary>
    public abstract class PlayerStateInteract : PlayerState
    {
        protected HplPhysicsBody CurrentBody;
        protected HplProp CurrentProp;
        protected Vector3 CurrentFocusPos;

        protected PlayerStateInteract(AmnesiaPlayer player, PlayerStateType type) : base(player, type) { }

        protected HplConfig Config => Player.gameConfig;
        protected float InteractionFloat(string key, float fallback = 0f) => Config.GetFloat("Player_Interaction", key, fallback);

        protected void SetupInteractVars()
        {
            CurrentProp = PlayerStateVars.InteractProp;
            CurrentBody = PlayerStateVars.InteractBody;
            CurrentFocusPos = PlayerStateVars.InteractPos;
            CurrentProp.IsInteractedWith = true;
        }

        protected void ResetInteractVars() => CurrentProp.IsInteractedWith = false;

        public override void OnDestroyEntity(HplEntity entity)
        {
            if (entity == CurrentProp) Player.States.ChangeState(PreviousState);
        }

        public override FocusCrosshair? GetCrosshair() => null;

        public override string Describe() => $"{Type} {CurrentProp?.Name}/{CurrentBody?.Name}";
    }

    /// <summary>
    /// cLuxPlayerState_InteractGrab (LuxPlayerState_InteractGrab.cpp): the body is pulled by a PID force to a point in
    /// front of the camera and turned by a PID torque to its held rotation; its mass is lowered (GrabMassMul), it has no
    /// gravity and does not collide with the player. Heavy props slow the player. Scroll changes the depth, the rotate
    /// key turns it with the mouse, attack throws it. Let go when released or too far. Computed in HPL2 space.
    /// </summary>
    public sealed class PlayerStateInteractGrab : PlayerStateInteract
    {
        struct BodyProperties
        {
            public HplPhysicsBody body;
            public bool hasGravity, collideCharacter;
            public float mass;
        }

        readonly HplPid _forcePid = new(400f, 0f, 40f, 20);
        readonly HplPid _speedTorquePid = new(40f, 0f, 0.4f, 20);
        readonly List<BodyProperties> _bodyProperties = new();
        InteractDataGrab _grabData;
        Matrix4x4 _bodyRotation;
        Vector3 _localBodyOffset;
        float _depth, _massSum, _maxDistance;

        readonly float _maxForce, _maxTorque, _maxAngularSpeed, _maxLeaveAngularSpeed, _maxLeaveLinearSpeed;
        readonly float _minSlowPlayerMass, _maxSlowPlayerMass, _minSlowPlayerMul;

        public PlayerStateInteractGrab(AmnesiaPlayer player) : base(player, PlayerStateType.InteractGrab)
        {
            _maxForce = InteractionFloat("GrabMaxForce");
            _maxTorque = InteractionFloat("GrabMaxTorque");
            _maxAngularSpeed = InteractionFloat("GrabMaxAngularSpeed");
            _maxLeaveAngularSpeed = InteractionFloat("GrabMaxLeaveAngularSpeed");
            _maxLeaveLinearSpeed = InteractionFloat("GrabMaxLeaveLinearSpeed");
            _minSlowPlayerMass = InteractionFloat("GrabMinSlowPlayerMass");
            _maxSlowPlayerMass = InteractionFloat("GrabMaxSlowPlayerMass");
            _minSlowPlayerMul = InteractionFloat("GrabMinSlowPlayerMul");
        }

        public override void OnEnterState(PlayerStateType previous)
        {
            SetupInteractVars();
            _grabData = CurrentProp.GetGrabData();
            _forcePid.Reset();
            _speedTorquePid.Reset();

            if (_grabData.grabUseOffset) _bodyRotation = HplMatrix.RotateXYZ(_grabData.grabRotationOffset);
            else
            {
                Matrix4x4 camRot = HplMatrix.RotateXYZ(Player.HplCameraAngles);
                Matrix4x4 invCamRot = camRot.inverse;
                _bodyRotation = invCamRot * HplMatrix.GetRotation(CurrentBody.HplMatrix);
                _localBodyOffset = HplMatrix.Mul(invCamRot, CurrentBody.HplPosition - CurrentFocusPos);
            }

            _depth = _grabData.grabUseDepth ? _grabData.grabDepth : Vector3.Distance(CurrentFocusPos, Player.HplCameraPosition) - 0.08f;

            // Save the current body and all bodies jointed to it, then set them up.
            _bodyProperties.Clear();
            SaveBodyProperties(CurrentBody);
            _massSum = 0f;
            float properMassSum = 0f;
            foreach (var p in _bodyProperties)
            {
                var body = p.body;
                properMassSum += body.Mass;
                if (body == CurrentBody) body.Gravity = false;
                if (body == CurrentBody) body.CollideCharacter = false;
                body.Mass *= _grabData.grabMassMul;
                body.AngularVelocity = Vector3.zero;
                body.LinearVelocity = Vector3.zero;
                _massSum += body.Mass;
            }

            Player.Body.mass = Player.DefaultMass + properMassSum;
            CurrentProp.SetDisableCollisionUntilOutsidePlayer(false);

            if (properMassSum >= _minSlowPlayerMass)
            {
                if (properMassSum >= _maxSlowPlayerMass) Player.interactionMoveSpeedMul = _minSlowPlayerMul;
                else
                {
                    float mul = (properMassSum - _minSlowPlayerMass) / (_maxSlowPlayerMass - _minSlowPlayerMass);
                    Player.interactionMoveSpeedMul = mul * _minSlowPlayerMul + (1f - mul);
                }
            }

            _maxDistance = Vector3.Distance(Player.HplCameraPosition, CurrentBody.HplPosition);
            _maxDistance = Mathf.Max(_maxDistance, _grabData.grabMaxDepth) * 1.1f + 0.2f;
        }

        void SaveBodyProperties(HplPhysicsBody body)
        {
            _bodyProperties.Add(new BodyProperties { body = body, hasGravity = body.Gravity, collideCharacter = body.CollideCharacter, mass = body.Mass });
            foreach (var joint in body.Joints)
            {
                if (joint.Child != null && !_bodyProperties.Exists(p => p.body == joint.Child)) SaveBodyProperties(joint.Child);
                if (joint.Parent != null && !_bodyProperties.Exists(p => p.body == joint.Parent)) SaveBodyProperties(joint.Parent);
            }
        }

        public override void OnLeaveState(PlayerStateType next)
        {
            foreach (var p in _bodyProperties)
            {
                p.body.Gravity = p.hasGravity;
                p.body.Mass = p.mass;
            }
            // The player should not collide with the prop until outside of it.
            CurrentProp.SetDisableCollisionUntilOutsidePlayer(true);
            Player.Body.mass = Player.DefaultMass;
            Player.interactionMoveSpeedMul = 1f;
            ResetInteractVars();
        }

        public override void PostUpdate(float timeStep)
        {
            float distance = Vector3.Distance(Player.HplCameraPosition, CurrentBody.HplPosition);
            if (distance > _maxDistance)
            {
                Player.States.ChangeState(PreviousState);
                return;
            }

            Matrix4x4 camTransform = HplMatrix.SetTranslation(HplMatrix.RotateXYZ(Player.HplCameraAngles), Player.HplCameraPosition);
            Vector3 basePos = _grabData.grabUseOffset
                ? _grabData.grabPositionOffset + new Vector3(0f, 0f, -_depth)
                : new Vector3(0f, 0f, -_depth) + _localBodyOffset;
            Matrix4x4 goal = camTransform * (HplMatrix.Translate(basePos) * _bodyRotation);

            // Force
            Vector3 wantedPos = HplMatrix.GetTranslation(goal);
            Vector3 error = wantedPos - CurrentBody.HplPosition;
            Vector3 force = _forcePid.Output(error, timeStep) * _massSum;
            force = HplMatrix.Vector3MaxLength(force, _maxForce);
            CurrentBody.AddForceHpl(force * _grabData.forceMul);

            if (!_grabData.useRotation) return;

            // Wanted rotation speed: align up and right with the goal.
            Matrix4x4 goalInv = goal.inverse;
            Vector3 wantedUp = HplMatrix.GetUp(goalInv);
            Vector3 wantedRight = HplMatrix.GetRight(goalInv);
            Matrix4x4 bodyInv = CurrentBody.HplMatrix.inverse;
            Vector3 up = HplMatrix.GetUp(bodyInv);
            Vector3 right = HplMatrix.GetRight(bodyInv);

            Vector3 wantedRotSpeed = Vector3.Cross(up, wantedUp) * (HplMatrix.Vector3Angle(wantedUp, up) * 100f);
            wantedRotSpeed += Vector3.Cross(right, wantedRight) * (HplMatrix.Vector3Angle(wantedRight, right) * 100f);

            float speed = wantedRotSpeed.magnitude;
            // Kept: the original scales to 6 rad/s, not to the configured max.
            if (speed > _maxAngularSpeed) wantedRotSpeed = wantedRotSpeed / speed * 6f;

            Vector3 rotError = wantedRotSpeed - CurrentBody.HplAngularVelocity;
            Vector3 torque = _speedTorquePid.Output(rotError, timeStep);
            torque = HplMatrix.Mul3x3(CurrentBody.HplInertiaMatrix, torque);
            torque = HplMatrix.Vector3MaxLength(torque, _maxTorque);
            CurrentBody.AddTorqueHpl(torque * _grabData.torqueMul);
        }

        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (action == PlayerAction.Interact && !pressed)
            {
                // Limit the speed of the bodies when letting go.
                foreach (var p in _bodyProperties)
                {
                    var body = p.body;
                    float linear = body.LinearVelocity.magnitude;
                    if (linear > _maxLeaveLinearSpeed) body.LinearVelocity = body.LinearVelocity / linear * _maxLeaveLinearSpeed;
                    float angular = body.AngularVelocity.magnitude;
                    if (angular > _maxLeaveAngularSpeed) body.AngularVelocity = body.AngularVelocity / angular * _maxLeaveAngularSpeed;
                }
                Player.States.ChangeState(PreviousState);
                return false;
            }
            if (action == PlayerAction.Attack && pressed)
            {
                CurrentBody.LinearVelocity = Vector3.zero;
                CurrentBody.AngularVelocity = Vector3.zero;
                CurrentBody.AddImpulseHpl(Player.HplCameraForward * _grabData.grabThrowImpulse);
                Player.States.ChangeState(PreviousState);
                return false;
            }
            return true;
        }

        public override void OnScroll(float amount)
        {
            _depth += amount * _grabData.grabDepthInc;
            if (_depth < _grabData.grabMinDepth) _depth = _grabData.grabMinDepth;
            if (_depth > _grabData.grabMaxDepth) _depth = _grabData.grabMaxDepth;
        }

        public override bool OnAddYaw(float amount)
        {
            if (Player.RotateHeld && _grabData.useRotation)
            {
                _bodyRotation = HplMatrix.RotateY(amount * -3.2f) * _bodyRotation;
                return false;
            }
            return true;
        }

        public override bool OnAddPitch(float amount)
        {
            if (Player.RotateHeld && _grabData.useRotation)
            {
                if (Player.invertMouse) amount = -amount;
                _bodyRotation = HplMatrix.RotateX(amount * -3.2f) * _bodyRotation;
                return false;
            }
            return true;
        }

        public override string Describe() => $"{base.Describe()} depth {_depth:0.00}";
    }

    /// <summary>
    /// cLuxPlayerState_InteractPush (LuxPlayerState_InteractPush.cpp): the player walks the body (at the grabbed point
    /// or its center) with a force in the move direction, a PID keeps it from sliding sideways and stops it when not
    /// moving; the player's speed follows the body's. The view is limited around where it looked when starting.
    /// </summary>
    public sealed class PlayerStateInteractPush : PlayerStateInteract
    {
        readonly HplPid _velocityPid = new(15f, 0f, 0.1f, 20);
        readonly HplPid _stopVelocityPid = new(10f, 0f, 0.1f, 20);
        InteractDataPush _pushData;
        Vector3 _localAttachPos, _pushDir;
        float _maxAttachPointDist;
        readonly bool[] _moving = new bool[2];
        readonly float[] _totalMoveMul = new float[2];

        readonly float _walkMaxSpeed, _walkForce, _runMaxSpeed, _runForce, _crouchMaxSpeed, _crouchForce;
        readonly float _yawRange, _pitchMinRange, _pitchMaxRange, _maxForce;

        public PlayerStateInteractPush(AmnesiaPlayer player) : base(player, PlayerStateType.InteractPush)
        {
            _walkMaxSpeed = InteractionFloat("PushMaxSpeedWalk");
            _walkForce = InteractionFloat("PushForceWalk");
            _runMaxSpeed = InteractionFloat("PushMaxSpeedRun");
            _runForce = InteractionFloat("PushForceRun");
            _crouchMaxSpeed = InteractionFloat("PushMaxSpeedCrouch");
            _crouchForce = InteractionFloat("PushForceCrouch");
            _yawRange = InteractionFloat("PushYawRange") * Mathf.Deg2Rad;
            _pitchMinRange = InteractionFloat("PushPitchMinRange") * Mathf.Deg2Rad;
            _pitchMaxRange = InteractionFloat("PushPitchMaxRange") * Mathf.Deg2Rad;
            _maxForce = InteractionFloat("PushMaxForce");
        }

        void ClearMoveVars()
        {
            for (int i = 0; i < 2; i++)
            {
                _moving[i] = false;
                _totalMoveMul[i] = 0f;
            }
        }

        public override void OnEnterState(PlayerStateType previous)
        {
            SetupInteractVars();
            Vector3 interactPos = PlayerStateVars.InteractPos;
            _pushData = CurrentProp.GetPushData();
            ClearMoveVars();
            _velocityPid.Reset();
            _stopVelocityPid.Reset();
            _pushDir = Vector3.zero;

            Player.Body.DisableDiagSpeedBoost = false;
            Player.PushViewLimits(_pitchMinRange, _pitchMaxRange, _yawRange);

            if (_pushData.pushAtPoint) _localAttachPos = HplMatrix.Mul(CurrentBody.HplMatrix.inverse, interactPos);
            Vector3 feet = HplSpace.Position(Player.Body.FeetPosition);
            _maxAttachPointDist = _pushData.pushAtPoint ? Vector3.Distance(feet, interactPos) : Vector3.Distance(feet, CurrentBody.HplPosition);
            _maxAttachPointDist = _maxAttachPointDist * 1.2f + 0.3f;
        }

        public override void OnLeaveState(PlayerStateType next)
        {
            Player.Body.DisableDiagSpeedBoost = true;
            Player.PopViewLimits();
            ResetInteractVars();
        }

        public override void Update(float timeStep)
        {
            var charBody = Player.Body;
            Vector3 attachPos = _pushData.pushAtPoint ? HplMatrix.Mul(CurrentBody.HplMatrix, _localAttachPos) : Vector3.zero;
            Vector3 feet = HplSpace.Position(charBody.FeetPosition);
            float distance = _pushData.pushAtPoint ? Vector3.Distance(feet, attachPos) : Vector3.Distance(feet, CurrentBody.HplPosition);
            if (distance > _maxAttachPointDist)
            {
                Player.States.ChangeState(PreviousState);
                return;
            }

            Vector3[] dirAdd = { HplSpace.Direction(charBody.Forward), HplSpace.Direction(charBody.Right) };
            _pushDir = Vector3.zero;
            for (int i = 0; i < 2; i++) if (_moving[i]) _pushDir += dirAdd[i] * _totalMoveMul[i];

            if (_pushDir == Vector3.zero)
            {
                Vector3 vel = _pushData.pushAtPoint ? CurrentBody.HplVelocityAtPosition(attachPos) : CurrentBody.HplLinearVelocity;
                Vector3 velError = -vel;
                velError.y = 0f;
                Vector3 stopForce = HplMatrix.Vector3MaxLength(_stopVelocityPid.Output(velError, timeStep) * CurrentBody.Mass, _maxForce);
                if (_pushData.pushAtPoint) CurrentBody.AddForceAtPositionHpl(stopForce, attachPos);
                else CurrentBody.AddForceHpl(stopForce);
                return;
            }

            _pushDir.Normalize();

            // Only move in the push direction.
            Vector3 bodyVel = _pushData.pushAtPoint ? CurrentBody.HplVelocityAtPosition(attachPos) : CurrentBody.HplLinearVelocity;
            Vector3 velInDir = _pushDir * Vector3.Dot(_pushDir, bodyVel);
            _velocityPid.p = 12f;
            _velocityPid.i = 0f;
            _velocityPid.d = 0.2f;
            Vector3 error = velInDir - bodyVel;
            error.y = 0f;
            Vector3 correctForce = HplMatrix.Vector3MaxLength(_velocityPid.Output(error, timeStep) * CurrentBody.Mass, _maxForce);
            if (_pushData.pushAtPoint) CurrentBody.AddForceAtPositionHpl(correctForce, attachPos);
            else CurrentBody.AddForceHpl(correctForce);

            float maxSpeed = _walkMaxSpeed, pushForce = _walkForce;
            if (Player.MoveState.Running) { maxSpeed = _runMaxSpeed; pushForce = _runForce; }
            else if (Player.MoveState.Crouching) { maxSpeed = _crouchMaxSpeed; pushForce = _crouchForce; }

            if (velInDir.magnitude < maxSpeed || Vector3.Dot(velInDir, _pushDir) < 0f)
            {
                Vector3 force = HplMatrix.Vector3MaxLength(_pushDir * pushForce * _pushData.pushForceMul, _maxForce);
                if (_pushData.pushAtPoint) CurrentBody.AddForceAtPositionHpl(force, attachPos);
                else CurrentBody.AddForceHpl(force);
            }

            Vector3 maxPlayerVel = Vector3.Dot(velInDir, _pushDir) > 0f ? velInDir : Vector3.zero;

            // Will the body hit the player when moved a bit? Then keep the player's speed limits.
            bool willCollide = false;
            var shape = charBody.Shapes[charBody.ActiveSize];
            Vector3 add = HplSpace.Direction(_pushDir * 0.1f);
            foreach (var c in CurrentBody.Colliders)
            {
                if (c == null || !c.enabled) continue;
                if (Physics.ComputePenetration(shape, charBody.Position, Quaternion.identity, c, c.transform.position + add, c.transform.rotation, out _, out _))
                {
                    willCollide = true;
                    break;
                }
            }
            if (!willCollide)
            {
                for (int i = 0; i < 2; i++)
                {
                    float max = Mathf.Abs(Vector3.Dot(dirAdd[i], maxPlayerVel));
                    charBody.SetMaxPositiveMoveSpeed((CharDir)i, max);
                    charBody.SetMaxNegativeMoveSpeed((CharDir)i, -max);
                }
            }
            for (int i = 0; i < 2; i++) if (_moving[i]) charBody.Move((CharDir)i, _totalMoveMul[i]);
        }

        public override void PostUpdate(float timeStep) => ClearMoveVars();

        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (action == PlayerAction.Interact && !pressed)
            {
                Player.States.ChangeState(PreviousState);
                return false;
            }
            if (action == PlayerAction.Attack && pressed)
            {
                if (_pushData.pushAtPoint)
                    CurrentBody.AddImpulseAtPositionHpl(_pushDir * _pushData.pushImpulse, HplMatrix.Mul(CurrentBody.HplMatrix, _localAttachPos));
                else CurrentBody.AddImpulseHpl(_pushDir * _pushData.pushImpulse);
                Player.States.ChangeState(PreviousState);
                return false;
            }
            return true;
        }

        public override bool OnMove(CharDir dir, float mul)
        {
            _totalMoveMul[(int)dir] += mul;
            _moving[(int)dir] = true;
            return false;
        }
    }

    /// <summary>
    /// cLuxPlayerState_InteractSlide (LuxPlayerState_InteractSlide.cpp): drawers and other slider joints, moved along
    /// the pin by mouse (and move keys) with a PID force; attack pushes it along the view.
    /// </summary>
    public sealed class PlayerStateInteractSlide : PlayerStateInteract
    {
        readonly HplPid _forcePid = new(6f, 0f, 0.1f, 10);
        readonly float _moveToMouseAddFactor, _maxForce;
        InteractDataSlide _slideData;
        HplJoint _joint;
        Vector2 _mouseAdd;
        float _slideSpeed, _maxDistance;
        bool _hadGravity;
        Vector3 _localInteractPos;

        public PlayerStateInteractSlide(AmnesiaPlayer player) : base(player, PlayerStateType.InteractSlide)
        {
            _moveToMouseAddFactor = InteractionFloat("MoveToMouseAddFactor", 0.01f);
            _maxForce = InteractionFloat("SlideMaxForce", 1000f);
        }

        public override void OnEnterState(PlayerStateType previous)
        {
            SetupInteractVars();
            _slideData = CurrentProp.GetSlideData();
            _joint = CurrentBody.Joints.Count > 0 ? CurrentBody.Joints[0] : null;
            _forcePid.Reset();
            _mouseAdd = Vector2.zero;
            _slideSpeed = 0f;
            _hadGravity = CurrentBody.Gravity;
            CurrentBody.Gravity = false;
            _localInteractPos = HplMatrix.Mul(CurrentBody.HplMatrix.inverse, CurrentFocusPos);
            _maxDistance = Vector3.Distance(Player.HplCameraPosition, CurrentFocusPos) * 1.4f;
            if (_joint == null) Player.States.ChangeState(PreviousState);
        }

        public override void OnLeaveState(PlayerStateType next)
        {
            CurrentBody.Gravity = _hadGravity;
            ResetInteractVars();
        }

        public override void Update(float timeStep)
        {
            Vector3 up = Player.HplCameraUp, right = Player.HplCameraRight;
            Vector3 worldInteractPos = HplMatrix.Mul(CurrentBody.HplMatrix, _localInteractPos);
            if (Vector3.Distance(Player.HplCameraPosition, worldInteractPos) > _maxDistance) Player.States.ChangeState(PreviousState);

            Vector3 pin = _joint.HplPinDir;
            Vector3 mouseCamDir = up * _mouseAdd.y + right * -_mouseAdd.x;
            float speedAdd = Vector3.Dot(mouseCamDir, pin);

            float slowDown = Mathf.Abs(_slideSpeed) * 3f * _slideData.slideSlowDownFactor;
            if (_slideSpeed > 0f) _slideSpeed = Mathf.Max(0f, _slideSpeed - timeStep * slowDown);
            if (_slideSpeed < 0f) _slideSpeed = Mathf.Min(0f, _slideSpeed + timeStep * slowDown);
            if (_mouseAdd == Vector2.zero) _slideSpeed = 0f;
            _slideSpeed += speedAdd * 850f * timeStep * _slideData.slideSpeedFactor;
            _slideSpeed = Mathf.Clamp(_slideSpeed, -_slideData.slideMaxSpeed, _slideData.slideMaxSpeed);

            Vector3 bodyVel = CurrentBody.HplLinearVelocity;
            Vector3 jointVel = pin * Vector3.Dot(pin, bodyVel);
            Vector3 wantedVel = pin * _slideSpeed;
            Vector3 force = HplMatrix.Vector3MaxLength(_forcePid.Output(wantedVel - jointVel, timeStep) * CurrentBody.Mass, _maxForce);
            CurrentBody.AddForceHpl(force);
            _mouseAdd = Vector2.zero;
        }

        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (action == PlayerAction.Interact && !pressed)
            {
                Player.States.ChangeState(PreviousState);
                return false;
            }
            if (action == PlayerAction.Attack && pressed)
            {
                Vector3 pin = _joint.HplPinDir;
                Vector3 impulse = (pin * Vector3.Dot(pin, Player.HplCameraForward)).normalized * _slideData.slideThrowImpulse;
                CurrentBody.AddImpulseHpl(impulse);
                return false;
            }
            return true;
        }

        public override bool OnAddYaw(float amount)
        {
            _mouseAdd.x += amount;
            return false;
        }

        public override bool OnAddPitch(float amount)
        {
            if (Player.invertMouse) amount = -amount;
            _mouseAdd.y += amount;
            return false;
        }

        public override bool OnMove(CharDir dir, float mul)
        {
            if (dir == CharDir.Forward) _mouseAdd.y += mul * _moveToMouseAddFactor;
            else _mouseAdd.x += mul * -_moveToMouseAddFactor;
            return true;
        }

        public override string Describe() => $"{base.Describe()} speed {_slideSpeed:0.00} dist {(_joint != null ? _joint.GetDistance() : 0f):0.000}";
    }

    /// <summary>
    /// iLuxPlayerState_InteractRotateBase (LuxPlayerState_InteractRotateBase.cpp): hinged props turned with the mouse
    /// (and move keys) by a PID torque along the pin; subclasses map the mouse to a turning speed.
    /// </summary>
    public abstract class PlayerStateInteractRotateBase : PlayerStateInteract
    {
        protected readonly HplPid RotatePid = new(10f, 0f, 1f, 10);
        readonly float _moveToMouseAddFactor, _maxTorque;
        protected InteractDataRotate MoveBaseData;
        protected HplJoint CurrentJoint;
        protected Vector2 MouseAdd;
        float _rotSpeed, _maxDistance;
        bool _hadGravity;

        protected PlayerStateInteractRotateBase(AmnesiaPlayer player, PlayerStateType type) : base(player, type)
        {
            _moveToMouseAddFactor = InteractionFloat("MoveToMouseAddFactor", 0.01f);
            _maxTorque = InteractionFloat("MoveMaxForce", 1000f);
        }

        public override void OnEnterState(PlayerStateType previous)
        {
            SetupInteractVars();
            MoveBaseData = CurrentProp.GetMoveBaseData();
            CurrentJoint = CurrentBody.Joints.Count > 0 ? CurrentBody.Joints[0] : null;
            RotatePid.Reset();
            _rotSpeed = 0f;
            MouseAdd = Vector2.zero;
            _hadGravity = CurrentBody.Gravity;
            CurrentBody.Gravity = false;
            if (CurrentJoint == null)
            {
                Player.States.ChangeState(PreviousState);
                return;
            }
            _maxDistance = Vector3.Distance(HplSpace.Position(Player.Body.FeetPosition), CurrentJoint.HplPivotPoint) * 1.2f + 0.5f;
            EnterRotateBase();
        }

        protected virtual void EnterRotateBase() { }

        public override void OnLeaveState(PlayerStateType next)
        {
            CurrentBody.Gravity = _hadGravity;
            ResetInteractVars();
        }

        protected abstract float GetSpeedAdd();
        protected abstract void OnThrow();

        public override void Update(float timeStep)
        {
            if (CurrentJoint == null) return;
            if (Vector3.Distance(HplSpace.Position(Player.Body.FeetPosition), CurrentJoint.HplPivotPoint) > _maxDistance)
                Player.States.ChangeState(PreviousState);

            float slowDown = Mathf.Abs(_rotSpeed) * MoveBaseData.moveSlowDownFactor;
            if (_rotSpeed > 0f) _rotSpeed = Mathf.Max(0f, _rotSpeed - timeStep * slowDown);
            if (_rotSpeed < 0f) _rotSpeed = Mathf.Min(0f, _rotSpeed + timeStep * slowDown);
            if (MouseAdd == Vector2.zero) _rotSpeed = 0f;

            float speedAdd = GetSpeedAdd();
            _rotSpeed += speedAdd * 3000f * MoveBaseData.moveSpeedFactor * timeStep;
            _rotSpeed = Mathf.Clamp(_rotSpeed, -MoveBaseData.moveMaxSpeed, MoveBaseData.moveMaxSpeed);

            Vector3 rotateDir = CurrentJoint.HplPinDir;
            Vector3 bodyVel = CurrentBody.HplAngularVelocity;
            Vector3 hingeVel = rotateDir * Vector3.Dot(rotateDir, bodyVel);
            Vector3 wantedVel = rotateDir * _rotSpeed;
            Vector3 torque = RotatePid.Output(wantedVel - hingeVel, timeStep);
            torque = HplMatrix.Mul3x3(CurrentBody.HplInertiaMatrix, torque);
            torque = HplMatrix.Vector3MaxLength(torque, _maxTorque);
            CurrentBody.AddTorqueHpl(torque);

            MouseAdd = Vector2.zero;
        }

        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (action == PlayerAction.Interact && !pressed)
            {
                Player.States.ChangeState(PreviousState);
                return false;
            }
            if (action == PlayerAction.Attack && pressed)
            {
                OnThrow();
                Player.States.ChangeState(PreviousState);
                return false;
            }
            return true;
        }

        public override bool OnMove(CharDir dir, float mul)
        {
            if (dir == CharDir.Forward) MouseAdd.y += mul * _moveToMouseAddFactor;
            else MouseAdd.x += mul * -_moveToMouseAddFactor;
            return true;
        }

        public override bool OnAddYaw(float amount)
        {
            MouseAdd.x += amount;
            return false;
        }

        public override bool OnAddPitch(float amount)
        {
            if (Player.invertMouse) amount = -amount;
            MouseAdd.y += amount;
            return false;
        }

        /// <summary>The direction from the pivot to the body's center of mass (HPL2 space).</summary>
        protected Vector3 JointToBody => (CurrentBody.HplWorldCenterOfMass - CurrentJoint.HplPivotPoint).normalized;

        public override string Describe() =>
            $"{base.Describe()} speed {_rotSpeed:0.00} angle {(CurrentJoint != null ? CurrentJoint.GetAngle() * Mathf.Rad2Deg : 0f):0.0}";
    }

    /// <summary>cLuxPlayerState_InteractSwingDoor: up / forward mouse pushes the door around the hinge.</summary>
    public sealed class PlayerStateInteractSwingDoor : PlayerStateInteractRotateBase
    {
        public PlayerStateInteractSwingDoor(AmnesiaPlayer player) : base(player, PlayerStateType.InteractSwingDoor) { }

        protected override float GetSpeedAdd()
        {
            Vector3 up = Player.HplCameraUp, right = Player.HplCameraRight, forward = Player.HplCameraForward;
            Vector3 push = (up + forward) * MouseAdd.y + right * -MouseAdd.x;
            Vector3 pushRotateDir = Vector3.Cross(JointToBody, push);
            return Vector3.Dot(pushRotateDir, CurrentJoint.HplPinDir);
        }

        protected override void OnThrow()
        {
            Vector3 jointForward = Vector3.Cross(CurrentJoint.HplPinDir, JointToBody);
            Vector3 impulse = jointForward * MoveBaseData.moveThrowImpulse;
            if (Vector3.Dot(jointForward, Player.HplCameraForward) < 0f) impulse = -impulse;
            CurrentBody.AddImpulseHpl(impulse);
        }
    }

    /// <summary>cLuxPlayerState_InteractLever: the view's up / right bent towards the lever's turning plane.</summary>
    public sealed class PlayerStateInteractLever : PlayerStateInteractRotateBase
    {
        public PlayerStateInteractLever(AmnesiaPlayer player) : base(player, PlayerStateType.InteractLever) { }

        protected override float GetSpeedAdd()
        {
            Vector3 jointToBody = JointToBody;
            Vector3 pin = CurrentJoint.HplPinDir;
            Vector3 up = Player.HplCameraUp, right = Player.HplCameraRight;
            Vector3 upJointForward = Vector3.Cross(up, pin);
            up = (upJointForward * Vector3.Dot(up, jointToBody) + up).normalized;
            Vector3 rightJointForward = Vector3.Cross(right, pin);
            right = (rightJointForward * Vector3.Dot(right, jointToBody) + right).normalized;
            Vector3 push = up * MouseAdd.y + right * -MouseAdd.x;
            return Vector3.Dot(Vector3.Cross(jointToBody, push), pin);
        }

        protected override void OnThrow() { }
    }

    /// <summary>cLuxPlayerState_InteractWheel: circular mouse moves (the last 10 moves) turn the wheel.</summary>
    public sealed class PlayerStateInteractWheel : PlayerStateInteractRotateBase
    {
        readonly LinkedList<Vector2> _moveAdds = new();

        public PlayerStateInteractWheel(AmnesiaPlayer player) : base(player, PlayerStateType.InteractWheel)
        {
            RotatePid.p = 5f;
            RotatePid.i = 0f;
            RotatePid.d = 0.1f;
        }

        protected override void EnterRotateBase() => _moveAdds.Clear();

        protected override float GetSpeedAdd()
        {
            _moveAdds.AddLast(MouseAdd);
            if (_moveAdds.Count > 10) _moveAdds.RemoveFirst();
            if (_moveAdds.Count < 4) return 0f;

            Vector2 prevPos = Vector2.zero;
            bool first = true;
            float properCount = 0f;
            Vector2[] tanCenter = { Vector2.zero, Vector2.zero };
            foreach (var add in _moveAdds)
            {
                Vector2 pos = add.normalized;
                if (first)
                {
                    first = false;
                    prevPos = pos;
                    continue;
                }
                Vector2 diff = pos - prevPos;
                if (diff.magnitude < 0.00001f) continue;
                Vector2[] tangents = { new(diff.y, -diff.x), new(-diff.y, diff.x) };
                for (int i = 0; i < 2; i++) tanCenter[i] += prevPos + tangents[i].normalized;
                properCount += 1f;
            }
            // Kept: prevPos is not advanced in the original loop.

            float[] dist = { (tanCenter[0] / properCount).magnitude, (tanCenter[1] / properCount).magnitude };
            float dirMul = 1f, minDist = dist[1];
            if (dist[0] < dist[1])
            {
                dirMul = -1f;
                minDist = dist[0];
            }
            if (minDist > 0.75f) return 0f;
            if (Vector3.Dot(CurrentJoint.HplPinDir, Player.HplCameraForward) < 0f) dirMul = -dirMul;
            return dirMul * MouseAdd.magnitude;
        }

        protected override void OnThrow() { }
    }
}
