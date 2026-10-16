using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// cLuxPlayerState_Ladder (LuxPlayerState_Ladder.cpp), speeds from game.cfg Player_Ladder:
    /// - attach (0.5 s, 1.2 s from above the ladder): the body slides to the start position without collision and the
    ///   view turns to face the ladder (pitch 0); the look is locked, then the head may turn ±120° / +79° / −60°;
    /// - forward / back climb at UpSpeed / DownSpeed (no sideways), step sounds every StepLength, stops where the body
    ///   does not fit; above the top it walks off forward (state 2: until there is ground under the feet or 2 s);
    ///   at the bottom (or ground under the feet / in front) it lets go;
    /// - jump lets go and jumps, interact / attack lets go; the lantern is put away and drawn again after.
    /// Sounds player_ladder_&lt;Material&gt;_attach / dettach / up / down. Positions in Unity space (y is the same).
    /// </summary>
    public sealed class PlayerStateLadder : PlayerState
    {
        readonly float _upSpeed, _downSpeed, _stepLength;
        HplArea _ladder;
        Vector3 _startPosition, _goalPos, _posAdd, _charPosition;
        float _goalYaw, _yawAdd, _pitchAdd;
        int _state;
        float _timeCount, _stepCount, _moveMul, _leaveAtTopCount;
        bool _playedSound, _lanternDrawn, _limitsPushed;

        public PlayerStateLadder(AmnesiaPlayer player) : base(player, PlayerStateType.Ladder)
        {
            var cfg = player.gameConfig;
            _upSpeed = cfg != null ? cfg.GetFloat("Player_Ladder", "UpSpeed", 0f) : 0f;
            _downSpeed = cfg != null ? cfg.GetFloat("Player_Ladder", "DownSpeed", 0f) : 0f;
            _stepLength = cfg != null ? cfg.GetFloat("Player_Ladder", "StepLength", 0f) : 0f;
        }

        public override bool AllowLantern => false;
        public int LadderState => _state;

        public override void OnEnterState(PlayerStateType previous)
        {
            _ladder = PlayerStateVars.Ladder;
            _startPosition = PlayerStateVars.LadderStartPos;
            var body = Player.Body;

            body.gravityActive = false;
            body.testCollision = false;
            body.StopMovement();
            body.ResetClimbing();
            Player.MoveState.SetCrouch(false);
            Player.MoveState.ResetJumping();

            _lanternDrawn = Player.Lantern.Active;
            Player.Lantern.SetActive(false, true, true, false);

            _state = 0;
            _stepCount = 0f;
            _playedSound = false;
            _moveMul = 0f;
            _limitsPushed = false;

            float time = 0.5f;
            if (body.Position.y > _ladder.LadderMaxY) time = 1.2f;
            _timeCount = time;

            _goalPos = _startPosition;
            _posAdd = (_goalPos - body.Position) / time;
            _charPosition = body.Position;

            // Face the ladder (HPL2: GetAngleFromPoints3D(0, -forward)), pitch 0, the short way round.
            Vector3 face = -_ladder.LadderForward;
            _goalYaw = Mathf.Atan2(face.x, face.z);
            _pitchAdd = Mathf.DeltaAngle(Player.Pitch * Mathf.Rad2Deg, 0f) * Mathf.Deg2Rad / time;
            _yawAdd = Mathf.DeltaAngle(Player.Yaw * Mathf.Rad2Deg, _goalYaw * Mathf.Rad2Deg) * Mathf.Deg2Rad / time;

            PlaySound("attach");
        }

        public override void OnLeaveState(PlayerStateType next)
        {
            Player.Body.gravityActive = true;
            Player.Body.testCollision = true;
            if (_limitsPushed) Player.PopViewLimits();
            _limitsPushed = false;
            if (_lanternDrawn) Player.Lantern.SetActive(true, true, true, false);
        }

        public override void Update(float timeStep)
        {
            var body = Player.Body;
            if (_state == 0)
            {
                _timeCount -= timeStep;
                _charPosition += _posAdd * timeStep;
                body.SetPosition(_charPosition);
                _charPosition = body.Position;
                Player.SetLook(Player.Yaw + _yawAdd * timeStep, Player.Pitch + _pitchAdd * timeStep);
                if (_timeCount <= 0f)
                {
                    body.testCollision = true;
                    SetupHeadTurnLimits();
                    _state++;
                }
            }
            else if (_state == 1)
            {
                _charPosition = body.Position;
                if (_moveMul > 0f)
                {
                    _charPosition.y += _moveMul * _upSpeed * timeStep;
                    if (_stepCount < 0f) _stepCount = 0f;
                }
                else if (_moveMul < 0f)
                {
                    _charPosition.y += _moveMul * _downSpeed * timeStep;
                    if (_stepCount > 0f) _stepCount = 0f;
                }
                else
                {
                    if (!_playedSound)
                    {
                        if (_stepCount > 0f) PlaySound("up");
                        if (_stepCount < 0f) PlaySound("down");
                    }
                    _stepCount = 0f;
                    _playedSound = false;
                }

                if (!body.CheckCharacterFits(_charPosition, false, 0, Hpl.HplPhysics.Epsilon))
                {
                    _moveMul = 0f;
                    return;
                }

                if (_moveMul > 0f)
                {
                    _stepCount += _moveMul * _upSpeed * timeStep;
                    if (_stepCount >= _stepLength)
                    {
                        _stepCount = 0f;
                        PlaySound("up");
                        _playedSound = true;
                    }
                }
                else if (_moveMul < 0f)
                {
                    _stepCount += _moveMul * _downSpeed * timeStep;
                    if (_stepCount <= -_stepLength)
                    {
                        _stepCount = 0f;
                        PlaySound("down");
                        _playedSound = true;
                    }
                }

                body.SetPosition(_charPosition);

                Vector3 groundRayStart1 = body.FeetPosition + new Vector3(0f, 0.05f, 0f) + _ladder.LadderForward * body.Size.x * 0.7f;
                Vector3 groundRayEnd1 = groundRayStart1 - new Vector3(0f, 0.1f, 0f);
                Vector3 groundRayStart2 = body.FeetPosition + new Vector3(0f, 0.05f, 0f);
                Vector3 groundRayEnd2 = groundRayStart2 - new Vector3(0f, 0.1f, 0f);

                if (_moveMul > 0f && _charPosition.y - body.Size.y * 0.5f > _ladder.LadderMaxY)
                {
                    _state = 2;
                    _leaveAtTopCount = 2f;
                    PlaySound("dettach");
                }
                else if (_moveMul < 0f &&
                         (_charPosition.y - body.Size.y / 2f < _ladder.LadderMinY ||
                          body.CheckRayIntersection(groundRayStart1, groundRayEnd1, out _, out _) ||
                          body.CheckRayIntersection(groundRayStart2, groundRayEnd2, out _, out _)))
                {
                    Player.States.ChangeState(PlayerStateType.Normal);
                }
                _moveMul = 0f;
            }
            else if (_state == 2)
            {
                body.Move(CharDir.Forward, 1f);
                Vector3 rayStart = body.FeetPosition + new Vector3(0f, 0.1f, 0f);
                Vector3 rayEnd = body.FeetPosition - new Vector3(0f, 0.3f, 0f);
                _leaveAtTopCount -= timeStep;
                if (body.CheckRayIntersection(rayStart, rayEnd, out _, out _)) Player.States.ChangeState(PlayerStateType.Normal);
                else if (_leaveAtTopCount <= 0f) Player.States.ChangeState(PlayerStateType.Normal);
            }
        }

        public override FocusCrosshair? GetCrosshair() => null;

        public override bool OnMove(CharDir dir, float mul)
        {
            if (_state == 0 || _state == 2) return false;
            if (dir == CharDir.Right) return false;
            _moveMul = mul;
            return false;
        }

        public override bool OnAddYaw(float amount) => _state != 0;
        public override bool OnAddPitch(float amount) => _state != 0;
        public override bool OnRun(bool pressed) => false;
        public override bool OnCrouch(bool pressed) => false;

        public override bool OnJump(bool pressed)
        {
            if (pressed)
            {
                Player.States.ChangeState(PlayerStateType.Normal);
                Player.MoveState.Jump();
            }
            return false;
        }

        public override bool OnDoAction(PlayerAction action, bool pressed)
        {
            if (pressed && (action == PlayerAction.Interact || action == PlayerAction.Attack))
                Player.States.ChangeState(PlayerStateType.Normal);
            // The lantern key still works (the lantern is not allowed on the ladder: AllowLantern).
            return action == PlayerAction.Lantern;
        }

        public override string Describe() => $"Ladder {_ladder?.Name} state {_state}";

        void PlaySound(string type) =>
            Player.sound?.PlayGuiSoundData("player_ladder_" + _ladder.LadderMaterial + "_" + type, SoundEntryType.World);

        /// <summary>SetupHeadTurnLimits: yaw ±120°, pitch 79° up / 60° down from the current view.</summary>
        void SetupHeadTurnLimits()
        {
            Player.PushViewLimits(60f * Mathf.Deg2Rad, 79f * Mathf.Deg2Rad, 120f * Mathf.Deg2Rad);
            _limitsPushed = true;
        }
    }
}
