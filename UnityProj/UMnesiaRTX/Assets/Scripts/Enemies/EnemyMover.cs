using RTXMnesia.Hpl;
using RTXMnesia.Player;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>eLuxEnemyMoveState.</summary>
    public enum EnemyMoveState { Backward, Stopped, Walking, Running, LastEnum }

    /// <summary>
    /// cLuxEnemyMover (LuxEnemyMover.cpp): turning toward a goal angle (speed ∝ angle, capped), moving forward, the
    /// stuck counter (real speed below 30% of the wanted one or moving sideways), braking in sharp turns and the move
    /// animations by speed (idle / walk / run / backward, walk ↔ run synced), splashes of steps in water. Angles in
    /// HPL2 space.
    /// </summary>
    public sealed class EnemyMover
    {
        readonly HplEnemy _enemy;
        readonly HplCharacterBody _body;
        const float StuckLimit = 0.3f;
        public const float MaxStuckCounter = 2f;

        bool _turning;
        float _turnGoalAngle, _turnSpeed;
        public float StuckCounter { get; private set; }
        public EnemyMoveState MoveState { get; private set; } = EnemyMoveState.LastEnum;
        public bool OverrideMoveState { get; set; }

        public EnemyMover(HplEnemy enemy, HplCharacterBody body)
        {
            _enemy = enemy;
            _body = body;
        }

        public bool StuckCounterIsAtMax => StuckCounter >= MaxStuckCounter;
        public void ResetStuckCounter() => StuckCounter = 0f;

        public void OnUpdate(float timeStep)
        {
            UpdateStuckCounter(timeStep);
            UpdateTurning(timeStep);
            UpdateMoveAnimation();
            UpdateStepEffects();
        }

        /// <summary>
        /// cLuxEnemyMover::UpdateStepEffects: in water, a Step event of the current animation that passed this step
        /// splashes at the body on the surface. The speed picked by the move state (walk and run both take
        /// WaterStepSpeedWalk) is not used: the impact is always the one of speed 3.
        /// </summary>
        void UpdateStepEffects()
        {
            if (!_enemy.IsInWater || _enemy.WaterSurface == null) return;
            var anim = _enemy.CurrentAnimation;
            if (anim == null) return;

            bool step = false;
            foreach (var e in anim.Events)
            {
                if (e.type != "Step") continue;
                if (e.time >= anim.PreviousTimePosition && e.time < anim.TimePosition)
                {
                    step = true;
                    break;
                }
            }
            if (!step) return;

            var impact = _enemy.WaterSurface.GetImpactDataFromSpeed(3f);
            if (impact == null) return;
            Vector3 pos = _body.Position;
            pos.y = _enemy.WaterSurfaceY + 0.01f;
            var map = _enemy.Map;
            if (!string.IsNullOrEmpty(impact.psName) && map.ParticleWorld != null)
            {
                var system = map.ParticleWorld.Create("Splash", impact.psName);
                if (system != null) system.LocalMatrix = Matrix4x4.Translate(pos);
            }
            if (!string.IsNullOrEmpty(impact.soundName) && map.sound != null)
            {
                var sound = map.sound.CreateSoundEntity("Splash", impact.soundName, true);
                if (sound == null) return;
                sound.IsSaved = false;
                sound.Position = pos;
            }
        }

        public void MoveToPos(Vector3 feetPos)
        {
            TurnToPos(feetPos);
            _body.Move(CharDir.Forward, 1f);
        }

        /// <summary>cLuxEnemyMover::TurnToPos: the heading from the body center to the position (xz).</summary>
        public void TurnToPos(Vector3 feetPos)
        {
            Vector3 start = HplSpace.Position(_body.Position);
            Vector3 goal = HplSpace.Position(feetPos);
            TurnToAngle(-HplMatrix.GetAngleFromPoints2D(new Vector2(start.x, start.z), new Vector2(goal.x, goal.z)));
        }

        public void TurnToAngle(float angle)
        {
            _turning = true;
            _turnGoalAngle = angle;
        }

        public void UseMoveStateAnimations()
        {
            if (!OverrideMoveState) return;
            OverrideMoveState = false;
            MoveState = EnemyMoveState.LastEnum;
        }

        /// <summary>The max speed multiplier: sharp turns brake (by TurnBreakMul × angle) while moving.</summary>
        public float CalculateSpeedMul(float timeStep)
        {
            float mul = 1f;
            if (_turning)
            {
                float angleDist = Mathf.Abs(HplMatrix.AngleDistanceRad(_enemy.HplYaw, _turnGoalAngle));
                if (angleDist >= _enemy.TurnMinBreakAngle && _body.GetMoveSpeed(CharDir.Forward) > 0.15f)
                {
                    mul -= _enemy.TurnBreakMul * angleDist;
                    if (mul < 0f) mul = 0f;
                }
            }
            return mul;
        }

        /// <summary>The speed along the ground (no falling, climbing).</summary>
        public float GetMoveSpeed()
        {
            Vector3 v = _body.GetVelocity(Time.fixedDeltaTime);
            v.y = 0f;
            return v.magnitude;
        }

        public float GetWantedSpeedAmount()
        {
            float wanted = _body.GetMoveSpeed(CharDir.Forward);
            if (Mathf.Abs(wanted) < 0.001f) return 1f;
            return GetMoveSpeed() / wanted;
        }

        void UpdateStuckCounter(float timeStep)
        {
            float wanted = _body.GetMoveSpeed(CharDir.Forward);
            Vector3 delta = _body.Position - _body.LastPosition;
            float real = delta.magnitude / timeStep;
            Vector3 realDir = delta.normalized;
            float cos = Vector3.Dot(_body.Forward, realDir);

            // A wanted speed of 0 divides to NaN / ±inf like the original: still counts as not stuck.
            if (real / wanted < StuckLimit || (Mathf.Abs(cos) < 0.3f && wanted > 0.001f))
            {
                StuckCounter += timeStep;
                if (StuckCounter > MaxStuckCounter) StuckCounter = MaxStuckCounter;
            }
            else
            {
                StuckCounter -= timeStep * 0.8f;
                if (StuckCounter < 0f) StuckCounter = 0f;
            }
        }

        void UpdateTurning(float timeStep)
        {
            if (!_turning) return;
            float angleDist = HplMatrix.AngleDistanceRad(_enemy.HplYaw, _turnGoalAngle);
            if (Mathf.Abs(angleDist) < 0.001f)
            {
                _turning = false;
                _turnSpeed = 0f;
                return;
            }
            _turnSpeed = Mathf.Min(_enemy.TurnSpeedMul * Mathf.Abs(angleDist), _enemy.TurnMaxSpeed);
            _enemy.AddHplYaw(angleDist < 0f ? -_turnSpeed * timeStep : _turnSpeed * timeStep);
        }

        /// <summary>cLuxEnemyMover::UpdateMoveAnimation: the move state by speed and its looping animation.</summary>
        void UpdateMoveAnimation()
        {
            if (OverrideMoveState) return;
            float speed = GetMoveSpeed();
            if (_body.GetMoveSpeed(CharDir.Forward) < 0f) speed = -speed;

            var prev = MoveState;
            switch (MoveState)
            {
                case EnemyMoveState.Backward:
                    if (speed >= 0f) MoveState = EnemyMoveState.Stopped;
                    break;
                case EnemyMoveState.Stopped:
                    if (speed < -0.05f) MoveState = EnemyMoveState.Backward;
                    else if (speed >= _enemy.StoppedToWalkSpeed) MoveState = EnemyMoveState.Walking;
                    else if (Mathf.Abs(_turnSpeed) > 0.07f && _body.MoveDelay <= 0f) MoveState = EnemyMoveState.Walking;
                    break;
                case EnemyMoveState.Walking:
                    if (speed >= _enemy.WalkToRunSpeed) MoveState = EnemyMoveState.Running;
                    else if (speed <= _enemy.WalkToStoppedSpeed && Mathf.Abs(_turnSpeed) < 0.03f) MoveState = EnemyMoveState.Stopped;
                    break;
                case EnemyMoveState.Running:
                    if (speed <= _enemy.RunToWalkSpeed) MoveState = EnemyMoveState.Walking;
                    break;
                case EnemyMoveState.LastEnum:
                    MoveState = EnemyMoveState.Stopped;
                    break;
            }

            if (prev != MoveState)
            {
                // Quadruped names of the man pig are not ported; the grunt and brute are bipeds.
                switch (MoveState)
                {
                    case EnemyMoveState.Backward:
                        _enemy.PlayAnim("Backward", true, 0.4f, true, _enemy.MoveSpeedAnimMul, false, false);
                        break;
                    case EnemyMoveState.Stopped:
                        _enemy.PlayAnim("Idle", true, 0.7f, false, 1f, false, false);
                        break;
                    case EnemyMoveState.Walking:
                        _enemy.PlayAnim("Walk", true, 0.5f, true, _enemy.MoveSpeedAnimMul, prev == EnemyMoveState.Running, false);
                        break;
                    case EnemyMoveState.Running:
                        _enemy.PlayAnim("Run", true, 0.5f, true, _enemy.MoveSpeedAnimMul, prev == EnemyMoveState.Walking, false);
                        break;
                }
            }

            var anim = _enemy.CurrentAnimation;
            if (anim != null && MoveState != EnemyMoveState.Stopped)
            {
                if (Mathf.Abs(speed) > 0.05f) anim.Speed = Mathf.Abs(speed) * _enemy.MoveSpeedAnimMul;
                else anim.Speed = Mathf.Abs(_turnSpeed) * _enemy.MoveSpeedAnimMul * 2f;
            }
        }
    }
}
