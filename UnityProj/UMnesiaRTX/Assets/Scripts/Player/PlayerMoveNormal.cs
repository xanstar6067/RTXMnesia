using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Player
{
    public enum FootSound { Step, Landing }

    /// <summary>
    /// Port of cLuxMoveState_Normal (+ iLuxMoveState): walk/run/crouch speeds, jumping, head bob,
    /// the head bounce on landing and fall damage. Values from game.cfg (Player_Movement_*).
    /// Ledge climbing is disabled in the original too.
    /// </summary>
    public class PlayerMoveNormal
    {
        readonly AmnesiaPlayer _player;

        // iLuxMoveState
        readonly float _forwardAcc, _sidewayAcc, _forwardDeacc, _sidewayDeacc, _forwardOppositeDirAccMul, _sidewayOppositeDirAccMul;
        float _maxForwardSpeedMul = 1f, _maxBackwardSpeedMul = 1f, _maxSidwaySpeedMul = 1f;

        readonly float _maxForwardSpeed, _maxBackwardSpeed, _maxSidwaySpeed;
        readonly float _runForwardMul, _runBackwardMul, _runSidewayMul;
        readonly float _crouchForwardMul, _crouchBackwardMul, _crouchSidewayMul;
        readonly float _inAirForwardMul, _inAirBackwardMul, _inAirSidewayMul;
        readonly float _maxJumpCount, _jumpStartForce, _jumpCrouchStartForce;
        readonly Vector2 _crouchBobMax, _walkBobMax, _runBobMax;
        readonly float _crouchMinBobSpeed, _crouchMaxBobSpeed, _walkMinBobSpeed, _walkMaxBobSpeed, _runMinBobSpeed, _runMaxBobSpeed;
        readonly float _groundBounceSize, _groundBounceSpeed, _minHitGroundBounceSpeed;
        readonly float _fallDamageBounceSizeMul, _fallDamageBounceSpeedMul;
        readonly float _fallDamageSpeedMin, _fallDamageMin, _fallDamageSpeedMed, _fallDamageMed, _fallDamageSpeedMax, _fallDamageMax;
        public readonly string JumpSound, CrouchSound, StandSound, FallDamageSoundMin, FallDamageSoundMed, FallDamageSoundMax;

        public bool Running { get; private set; }
        public bool Crouching { get; private set; }
        bool _jumping;
        float _jumpCount;
        bool _wasMoving;
        float _defaultForwardMul = 1f;

        bool _headGroundBounceActive;
        float _headGroundBounce, _bounceSizeMul = 1f, _bounceSpeedMul = 1f;
        Vector2 _headBob, _bobMaxGoal, _currentBobMax;
        float _headBobCount, _prevHeadBobCount;
        bool _bobbing;

        public PlayerMoveNormal(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            const string d = "Player_Movement_Default", n = "Player_Movement_Normal", g = "Player_General";

            _forwardAcc = cfg.GetFloat(d, "ForwardAcc");
            _sidewayAcc = cfg.GetFloat(d, "SidewayAcc");
            _forwardDeacc = cfg.GetFloat(d, "ForwardDeacc");
            _sidewayDeacc = cfg.GetFloat(d, "SidewayDeacc");
            _forwardOppositeDirAccMul = cfg.GetFloat(d, "ForwardOppositeDirAccMul");
            _sidewayOppositeDirAccMul = cfg.GetFloat(d, "SidewayOppositeDirAccMul");

            JumpSound = cfg.GetString(n, "JumpSound", "player_jump");
            CrouchSound = cfg.GetString(n, "CrouchSound", "player_crouch");
            StandSound = cfg.GetString(n, "StandSound", "player_stand");

            _maxForwardSpeed = cfg.GetFloat(n, "MaxForwardSpeed");
            _maxBackwardSpeed = cfg.GetFloat(n, "MaxBackwardSpeed");
            _maxSidwaySpeed = cfg.GetFloat(n, "MaxSidwaySpeed");
            _runForwardMul = cfg.GetFloat(n, "RunForwardMul");
            _runBackwardMul = cfg.GetFloat(n, "RunBackwardMul");
            _runSidewayMul = cfg.GetFloat(n, "RunSidewayMul");
            _crouchForwardMul = cfg.GetFloat(n, "CrouchForwardMul");
            _crouchBackwardMul = cfg.GetFloat(n, "CrouchBackwardMul");
            _crouchSidewayMul = cfg.GetFloat(n, "CrouchSidewayMul");
            _inAirForwardMul = cfg.GetFloat(n, "InAirForwardMul");
            _inAirBackwardMul = cfg.GetFloat(n, "InAirBackwardMul");
            _inAirSidewayMul = cfg.GetFloat(n, "InAirSidewayMul");

            _maxJumpCount = cfg.GetFloat(n, "MaxJumpCount");
            _jumpCount = _maxJumpCount;
            _jumpStartForce = cfg.GetFloat(n, "JumpStartForce");
            _jumpCrouchStartForce = cfg.GetFloat(n, "JumpCrouchStartForce");

            _crouchBobMax = cfg.GetVector2(n, "CrouchBobMax");
            _walkBobMax = cfg.GetVector2(n, "WalkBobMax");
            _runBobMax = cfg.GetVector2(n, "RunBobMax");
            _crouchMinBobSpeed = cfg.GetFloat(n, "CrouchMinBobSpeed");
            _crouchMaxBobSpeed = cfg.GetFloat(n, "CrouchMaxBobSpeed");
            _walkMinBobSpeed = cfg.GetFloat(n, "WalkMinBobSpeed");
            _walkMaxBobSpeed = cfg.GetFloat(n, "WalkMaxBobSpeed");
            _runMinBobSpeed = cfg.GetFloat(n, "RunMinBobSpeed");
            _runMaxBobSpeed = cfg.GetFloat(n, "RunMaxBobSpeed");

            _groundBounceSize = cfg.GetFloat(n, "GroundBounceSize");
            _groundBounceSpeed = cfg.GetFloat(n, "GroundBounceSpeed");
            _minHitGroundBounceSpeed = cfg.GetFloat(n, "MinHitGroundBounceSpeed");

            _fallDamageBounceSizeMul = cfg.GetFloat(g, "FallDamageBounceSizeMul");
            _fallDamageBounceSpeedMul = cfg.GetFloat(g, "FallDamageBounceSpeedMul");
            _fallDamageSpeedMin = cfg.GetFloat(g, "FallDamageSpeed_Min");
            _fallDamageMin = cfg.GetFloat(g, "FallDamage_Min");
            FallDamageSoundMin = cfg.GetString(g, "FallDamageSound_Min");
            _fallDamageSpeedMed = cfg.GetFloat(g, "FallDamageSpeed_Med");
            _fallDamageMed = cfg.GetFloat(g, "FallDamage_Med");
            FallDamageSoundMed = cfg.GetString(g, "FallDamageSound_Med");
            _fallDamageSpeedMax = cfg.GetFloat(g, "FallSpeed_Max");
            _fallDamageMax = cfg.GetFloat(g, "FallDamage_Max");
            FallDamageSoundMax = cfg.GetString(g, "FallDamageSound_Max");
        }

        HplCharacterBody Body => _player.Body;

        public void OnEnterState()
        {
            Running = false;
            _wasMoving = false;
            _headGroundBounceActive = false;
            _headGroundBounce = 0f;
            _bounceSizeMul = _bounceSpeedMul = 1f;
            _headBob = Vector2.zero;
            _prevHeadBobCount = _headBobCount = 0f;
            _bobbing = false;
            _bobMaxGoal = _currentBobMax = _walkBobMax;
            Crouching = false;
            _defaultForwardMul = 1f;
            Body.GravityCollide += OnGravityCollide;
        }

        public void OnLeaveState()
        {
            Body.GravityCollide -= OnGravityCollide;
            _player.MoveHeadPosAdd(HeadPosAdd.Bob, Vector3.zero, 0.5f, 0.01f);
        }

        /// <summary>iLuxMoveState::Update: state update, then the speeds go to the character body.</summary>
        public void Update(float dt)
        {
            UpdateMovement();
            UpdateSpeedMultipliers();
            UpdateJumpAndGroundCheck(dt);
            UpdateHeadBob(dt);

            Body.SetMaxPositiveMoveSpeed(CharDir.Forward, _maxForwardSpeed * _maxForwardSpeedMul);
            Body.SetMaxNegativeMoveSpeed(CharDir.Forward, -_maxBackwardSpeed * _maxBackwardSpeedMul);
            Body.SetMaxPositiveMoveSpeed(CharDir.Right, _maxSidwaySpeed * _maxSidwaySpeedMul);
            Body.SetMaxNegativeMoveSpeed(CharDir.Right, -_maxSidwaySpeed * _maxSidwaySpeedMul);
            Body.SetMoveAcc(CharDir.Forward, _forwardAcc);
            Body.SetMoveAcc(CharDir.Right, _sidewayAcc);
            Body.SetMoveDeacc(CharDir.Forward, _forwardDeacc);
            Body.SetMoveDeacc(CharDir.Right, _sidewayDeacc);
            Body.SetMoveOppositeDirAccMul(CharDir.Forward, _forwardOppositeDirAccMul);
            Body.SetMoveOppositeDirAccMul(CharDir.Right, _sidewayOppositeDirAccMul);
        }

        public void OnRun(bool pressed)
        {
            // Running while moving stands up.
            if (pressed && !_player.crouchDisabled && Body.IsOnGround && _player.PressedMove) SetCrouch(false);
        }

        public void OnCrouch(bool pressed)
        {
            if (pressed) SetCrouch(!Crouching);
        }

        public void OnJump(bool pressed)
        {
            if (pressed && !_jumping && Body.IsOnGround) Jump();
        }

        /// <summary>cLuxMoveState_Normal::Jump (also the jump off a ladder, without the ground check).</summary>
        public void Jump()
        {
            SetCrouch(false);
            _player.PlaySound(JumpSound);
            // Still crouching only if there was no room to stand up.
            float startForce = Crouching ? _jumpCrouchStartForce : _jumpStartForce;
            startForce *= _player.scriptJumpForceMul;
            Body.AddForce(new Vector3(0f, startForce * _player.DefaultMass, 0f));
            _jumping = true;
            _jumpCount = 0f;
        }

        /// <summary>cLuxMoveState_Normal::OnMapEnter: a crouching player enters the new map crouching (no sound).</summary>
        public void RestoreCrouch()
        {
            Crouching = true;
            Body.SetActiveSize(1);
        }

        public void SetCrouch(bool active)
        {
            if (Crouching == active) return;
            if (active)
            {
                _player.PlaySound(CrouchSound);
                Crouching = true;
                Body.SetActiveSize(1);
                float sizeDiff = _player.BodySize.y - _player.BodyCrouchSize.y;
                _player.MoveHeadPosAdd(HeadPosAdd.Main, new Vector3(0f, -sizeDiff, 0f), 1.3f, 0.05f);
                return;
            }

            // Stand up only where the standing body fits (a few positions are tried).
            Vector3[] tests =
            {
                new(0f, 0.001f, 0f), new(0.01f, 0.001f, 0f), new(-0.01f, 0.001f, 0f), new(0f, 0.001f, 0.01f), new(0f, 0.001f, -0.01f),
            };
            foreach (var add in tests)
            {
                if (!Body.CheckCharacterFits(Body.FeetPosition + add, true, 0)) continue;
                _player.PlaySound(StandSound);
                Crouching = false;
                Body.SetPosition(Body.Position + add);
                Body.SetActiveSize(0);
                _player.MoveHeadPosAdd(HeadPosAdd.Main, Vector3.zero, 1.6f, 0.05f);
                return;
            }
        }

        void UpdateMovement()
        {
            Running = _player.PressingRun;
            if (Body.IsOnGround && _player.PressedMove && !_wasMoving)
            {
                _wasMoving = true;
                if (Running && !_player.crouchDisabled) SetCrouch(false);
            }
            else _wasMoving = false;
        }

        void UpdateSpeedMultipliers()
        {
            float moveMul = _player.MoveSpeedMul;
            _maxForwardSpeedMul = _maxBackwardSpeedMul = _maxSidwaySpeedMul = moveMul;
            _defaultForwardMul = 1f;

            if (Body.IsOnGround)
            {
                if (Crouching)
                {
                    _maxForwardSpeedMul *= _crouchForwardMul;
                    _maxBackwardSpeedMul *= _crouchBackwardMul;
                    _maxSidwaySpeedMul *= _crouchSidewayMul;
                    _defaultForwardMul *= _crouchForwardMul;
                }
                else if (Running)
                {
                    float runSpeedMul = _player.RunSpeedMul;
                    _maxForwardSpeedMul *= 1f + (_runForwardMul - 1f) * runSpeedMul;
                    _maxBackwardSpeedMul *= 1f + (_runBackwardMul - 1f) * runSpeedMul;
                    _maxSidwaySpeedMul *= 1f + (_runSidewayMul - 1f) * runSpeedMul;
                    _defaultForwardMul *= _runForwardMul;
                }
            }
            else
            {
                _maxForwardSpeedMul = _inAirForwardMul;
                _maxBackwardSpeedMul = _inAirBackwardMul;
                _maxSidwaySpeedMul = _inAirSidewayMul;
                _defaultForwardMul = _inAirForwardMul;
            }
        }

        void UpdateJumpAndGroundCheck(float dt)
        {
            if (!_jumping) return;
            _jumpCount += dt;
            if (_jumpCount >= _maxJumpCount)
            {
                _jumpCount = _maxJumpCount;
                _jumping = false;
            }
            // While jump is held the jump lasts longer: part of gravity is taken away.
            float mul = 0.4f + 0.5f * (1f - _jumpCount / _maxJumpCount);
            Body.AddForce(new Vector3(0f, -Body.gravity.y * Body.Mass * mul, 0f));
        }

        public void ResetJumping() => _jumping = false;

        void UpdateHeadBob(float dt)
        {
            _bobMaxGoal = _walkBobMax;
            float minBobSpeed = _walkMinBobSpeed, maxBobSpeed = _walkMaxBobSpeed;
            if (Crouching)
            {
                _bobMaxGoal = _crouchBobMax;
                minBobSpeed = _crouchMinBobSpeed;
                maxBobSpeed = _crouchMaxBobSpeed;
            }
            else if (Running)
            {
                _bobMaxGoal = _runBobMax;
                minBobSpeed = _runMinBobSpeed;
                maxBobSpeed = _runMaxBobSpeed;
            }

            float playerSpeed = Body.GetVelocity(dt).magnitude;
            float maxPlayerSpeed = _maxForwardSpeed * _defaultForwardMul;
            float bobSpeed = minBobSpeed + playerSpeed / maxPlayerSpeed * (maxBobSpeed - minBobSpeed);

            bool moving = Body.IsOnGround && Body.MovedLastUpdate;
            if (!moving) _bobMaxGoal = Vector2.zero;
            _currentBobMax = new Vector2(Mathf.MoveTowards(_currentBobMax.x, _bobMaxGoal.x, 0.1f * dt),
                                         Mathf.MoveTowards(_currentBobMax.y, _bobMaxGoal.y, 0.1f * dt));

            if (!moving)
            {
                // Bring the head back to rest (sin = 1) the shortest way.
                if (_bobbing)
                {
                    float x = Wrap(_headBobCount, -90f * Mathf.Deg2Rad, 270f * Mathf.Deg2Rad);
                    float add = x <= 90f * Mathf.Deg2Rad ? 1f : -1f;
                    float prevCos = Mathf.Cos(_headBobCount);
                    _prevHeadBobCount = _headBobCount;
                    _headBobCount += dt * Mathf.PI * 2f * add * 3.1f;
                    float cos = Mathf.Cos(_headBobCount);
                    // The original tests cos twice here (fSin = cos(...)).
                    float sinTest = Mathf.Cos(_headBobCount);
                    if (sinTest > 0f && ((cos <= 0f && prevCos >= 0f) || (cos >= 0f && prevCos <= 0f)))
                    {
                        _bobbing = false;
                        _headBob = Vector2.zero;
                    }
                }
            }
            else
            {
                _bobbing = true;
                _prevHeadBobCount = _headBobCount;
                _headBobCount += bobSpeed * dt * Mathf.PI * 2f;

                // Footstep at the lowest point of the bob.
                float prevCos = Mathf.Cos(_prevHeadBobCount), cos = Mathf.Cos(_headBobCount), sin = Mathf.Sin(_headBobCount);
                if (sin < 0f && ((cos <= 0f && prevCos >= 0f) || (cos >= 0f && prevCos <= 0f)))
                    PlayFootSound(FootSound.Step);
            }

            if (_bobbing)
            {
                _headBob.y = Mathf.Sin(_headBobCount) * _currentBobMax.y - _currentBobMax.y;
                _headBob.x = Mathf.Sin(_headBobCount / 2f - Mathf.PI / 4f) * _currentBobMax.x;
            }
            else _headBob = Vector2.zero;

            if (_headGroundBounceActive)
            {
                _headGroundBounce += dt * _groundBounceSpeed * _bounceSpeedMul;
                // Fast down, then slowly up again.
                if (_headGroundBounce < 0.5f)
                    _headBob.y += Mathf.Sin(Mathf.PI + _headGroundBounce * Mathf.PI) * _groundBounceSize * _bounceSizeMul;
                else
                    _headBob.y += (SmoothCurve((_headGroundBounce - 0.5f) * 2f) - 1f) * _groundBounceSize * _bounceSizeMul;

                if (_headGroundBounce >= 1f)
                {
                    _headGroundBounceActive = false;
                    _headGroundBounce = 0f;
                }
            }

            _player.SetHeadPosAdd(HeadPosAdd.Bob, new Vector3(_headBob.x, _headBob.y, 0f));
        }

        // cLuxMoveState_Normal_Callback::OnGravityCollide: landing hard enough bounces the head.
        void OnGravityCollide(Collider other, Vector3 normal, Vector3 velocity)
        {
            if (normal.y <= 0.001f || velocity.y >= -_minHitGroundBounceSpeed || _headGroundBounceActive) return;
            _headGroundBounceActive = true;
            _bounceSizeMul = 1f;
            _bounceSpeedMul = 1f;
            PlayFootSound(FootSound.Landing);
            FallDamage(velocity.y);
        }

        void FallDamage(float ySpeed)
        {
            if (_player.noFallDamage) return;
            string sound = "";
            float damage = 0f;
            if (ySpeed < -_fallDamageSpeedMax) { sound = FallDamageSoundMax; damage = _fallDamageMax; }
            else if (ySpeed < -_fallDamageSpeedMed) { sound = FallDamageSoundMed; damage = _fallDamageMed; }
            else if (ySpeed < -_fallDamageSpeedMin) { sound = FallDamageSoundMin; damage = _fallDamageMin; }
            if (damage <= 0f) return;

            _bounceSizeMul = _fallDamageBounceSizeMul;
            _bounceSpeedMul = _fallDamageBounceSpeedMul;
            _player.GiveDamage(damage, 100, DamageType.BloodSplat, false, false);
            _player.PlaySound(sound);
        }

        /// <summary>Step sound name like the original: step_walk|run|sneak_&lt;material step type&gt;.</summary>
        void PlayFootSound(FootSound type)
        {
            string moveType = Crouching ? "sneak" : Running ? "run" : "walk";
            if (type == FootSound.Landing) moveType = "run";
            _player.OnFootSound(type, moveType);
        }

        static float Wrap(float x, float min, float max)
        {
            float range = max - min;
            return x - range * Mathf.Floor((x - min) / range);
        }

        // cMath::SmoothCurve: 0..1 with zero slope at both ends.
        static float SmoothCurve(float x) => x * x * (3f - 2f * x);
    }
}
