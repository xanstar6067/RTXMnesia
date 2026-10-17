using System.Collections.Generic;
using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>cVerletParticle (VerletParticle.cpp). Positions in HPL2 space.</summary>
    public sealed class HplVerletParticle
    {
        public Vector3 Position, PrevPosition, SmoothPosition;
        public float InvMass;

        public HplVerletParticle(Vector3 position, float invMass)
        {
            Position = position;
            PrevPosition = position;
            SmoothPosition = position;
            InvMass = invMass;
        }
    }

    /// <summary>cPhysicsRopeAttachment: a body that holds one end of the rope.</summary>
    public sealed class HplRopeAttachment
    {
        public HplPhysicsBody Body;
        public HplVerletParticle Particle;
        /// <summary>The attachment point in the body's frame (HPL2 space).</summary>
        public Vector3 BodyLocalPos;
    }

    /// <summary>
    /// A verlet rope of the physics world (port of iPhysicsRope and iVerletParticleContainer, PhysicsRope.cpp,
    /// VerletParticle.cpp; cPhysicsRopeNewton adds nothing). All math in HPL2 space.
    ///
    /// The first particle is fixed (inverse mass 0), the others fall with world gravity. Each step: sleep check, motor or
    /// auto move (changes the total length and so the particles), verlet movement, the ends follow their attached
    /// bodies, 3 rounds of length constraints, then a PID force pulls each attached body to its end particle. After
    /// the simulation the smooth positions spread the attachment offsets along the rope for drawing. Particle
    /// collisions are skipped, as in the original ("Skip collision testing for now!").
    /// </summary>
    public sealed class HplPhysicsRope
    {
        public readonly string Name;
        public int UniqueId = -1;
        readonly HplPhysicsWorld _world;
        readonly List<HplVerletParticle> _particles = new();
        readonly HplPid[] _forcePid = { new(0f, 0f, 0f, 10), new(0f, 0f, 0f, 10) };
        readonly HplRopeAttachment[] _attached = { new(), new() };

        // iVerletParticleContainer
        static readonly Vector3 Gravity = new(0f, -9.81f, 0f);
        float _damping = 0.001f, _dampingMul = 1f - 0.001f;
        public float ParticleRadius { get; private set; } = 0.05f;
        bool _sleeping;
        float _sleepCheckCount;
        const float SleepCheckTime = 1f / 3f;
        const float SleepCheckSqrLimit = 0.0015f * 0.0015f;
        int _sleepCount;
        const int SleepMaxCount = 3;
        public int UpdateCount { get; private set; }

        // iPhysicsRope
        bool _motorActive;
        public float MotorWantedLength, MotorSpeedMul = 1f, MotorMinSpeed = -1f, MotorMaxSpeed = 1f;
        public string MotorSound = "";
        public float MotorSoundStartSpeed = 0.1f, MotorSoundStopSpeed = 0.05f;
        Sound.HplSoundEntity _motorSoundEntity;
        bool _autoMoveActive;
        public float AutoMoveAcc = 1f, AutoMoveMaxSpeed = 1f, AutoMoveSpeed;
        float _totalLength, _segmentLength, _firstSegmentLength;
        public float MinTotalLength, MaxTotalLength;
        public int MaxIterations = 3;
        public float Strength = 100f, Stiffness = 1f;
        bool _hasUpdated;

        /// <summary>The sound handler for the motor sound (cWorld of the physics world).</summary>
        public Sound.HplSoundHandler Sound;

        public HplPhysicsRope(string name, HplPhysicsWorld world, Vector3 hplStart, Vector3 hplEnd)
        {
            Name = name;
            _world = world;
            _particles.Add(new HplVerletParticle(hplStart, 0f));
            _particles.Add(new HplVerletParticle(hplEnd, 1f));
            _totalLength = Vector3.Distance(hplStart, hplEnd);
            MotorWantedLength = _totalLength;
            MinTotalLength = _totalLength;
            MaxTotalLength = _totalLength;
            _firstSegmentLength = _totalLength;
        }

        public IReadOnlyList<HplVerletParticle> Particles => _particles;
        public HplVerletParticle StartParticle => _particles[0];
        public HplVerletParticle EndParticle => _particles[_particles.Count - 1];
        public HplRopeAttachment GetAttachment(int index) => _attached[index];
        public HplPhysicsBody AttachedStartBody => _attached[0].Body;
        public HplPhysicsBody AttachedEndBody => _attached[1].Body;
        public bool IsSleeping => _sleeping;
        public float TotalLength => _totalLength;
        public float SegmentLength => _segmentLength;
        public float Damping => _damping;
        public bool MotorActive => _motorActive;
        public bool AutoMoveActive => _autoMoveActive;

        // ------------------------------------------------------------------ properties

        public void SetDamping(float x)
        {
            _damping = Mathf.Clamp01(x);
            _dampingMul = 1f - _damping;
        }

        public void SetParticleRadius(float x)
        {
            if (ParticleRadius == x) return;
            ParticleRadius = x;
            SetSleeping(false);
        }

        public void SetMotorActive(bool x)
        {
            if (_motorActive == x) return;
            _motorActive = x;
            if (_motorActive)
            {
                AutoMoveSpeed = 0f;
                SetSleeping(false);
            }
        }

        public void SetAutoMoveActive(bool x)
        {
            _autoMoveActive = x;
            if (_autoMoveActive) SetSleeping(false);
        }

        public void SetTotalLength(float x)
        {
            if (_totalLength == x) return;
            _totalLength = x;
            BuildRopeParticles();
            SetSleeping(false);
        }

        public void SetSegmentLength(float x)
        {
            if (x < 0f || _segmentLength == x) return;
            _segmentLength = x;
            BuildRopeParticles();
            SetSleeping(false);
        }

        public void SetAttachedStartBody(HplPhysicsBody body) => SetAttachedBody(0, StartParticle, body);

        public void SetAttachedEndBody(HplPhysicsBody body) => SetAttachedBody(1, EndParticle, body);

        /// <summary>iPhysicsRope::RemoveAttachedBody (the body is destroyed).</summary>
        public void RemoveAttachedBody(HplPhysicsBody body)
        {
            for (int i = 0; i < 2; i++)
                if (_attached[i].Body == body) _attached[i].Body = null;
        }

        void SetAttachedBody(int index, HplVerletParticle particle, HplPhysicsBody body)
        {
            _forcePid[index].Reset();
            _attached[index].Body = body;
            _attached[index].Particle = particle;
            _attached[index].BodyLocalPos = HplMatrix.Mul(body.HplMatrix.inverse, particle.Position);
        }

        /// <summary>iVerletParticleContainer::SetSleeping: waking up wakes the attached bodies.</summary>
        public void SetSleeping(bool x)
        {
            if (_sleeping == x) return;
            _sleeping = x;
            _sleepCount = 0;
            _sleepCheckCount = 0f;
            if (!x)
                for (int i = 0; i < 2; i++) _attached[i].Body?.WakeUp();
        }

        // ------------------------------------------------------------------ simulation

        /// <summary>iPhysicsRope::UpdateBeforeSimulate.</summary>
        public void UpdateBeforeSimulate(float timeStep)
        {
            _hasUpdated = true;
            PreUpdate(timeStep);
            if (_sleeping) return;

            UpdateMotorAndAutoMove(timeStep);
            foreach (var p in _particles) UpdateMovement(p, timeStep);
            UpdateAttachedParticlePositions();
            for (int i = 0; i < MaxIterations; i++) UpdateConstraints();
            UpdateAttachedBodies(timeStep);
        }

        /// <summary>iPhysicsRope::UpdateAfterSimulate.</summary>
        public void UpdateAfterSimulate()
        {
            if (_sleeping) return;
            CalculateSmoothPositions();
        }

        /// <summary>The world position of an attachment point (from the body's current pose).</summary>
        public Vector3 AttachmentHplPosition(int index) =>
            HplMatrix.Mul(_attached[index].Body.HplMatrix, _attached[index].BodyLocalPos);

        void PreUpdate(float timeStep)
        {
            if (_sleeping && CheckSpecificDataAwake()) SetSleeping(false);
            if (_sleeping) return;

            UpdateCount++;
            _sleepCheckCount += timeStep;
            if (_sleepCheckCount < SleepCheckTime) return;

            bool allSleeping = true;
            foreach (var p in _particles)
            {
                if ((p.Position - p.PrevPosition).sqrMagnitude > SleepCheckSqrLimit)
                {
                    allSleeping = false;
                    break;
                }
            }
            if (allSleeping) allSleeping = CheckSpecificDataSleeping();

            if (allSleeping)
            {
                _sleepCount++;
                if (SleepMaxCount <= _sleepCount) SetSleeping(true);
            }
            else _sleepCount = 0;
        }

        bool CheckSpecificDataSleeping()
        {
            if (_motorActive) return false;
            for (int i = 0; i < 2; i++)
            {
                var body = _attached[i].Body;
                if (body == null) continue;
                if (body.HplAngularVelocity.sqrMagnitude > 0.001f * 0.001f) return false;
                if (body.HplLinearVelocity.sqrMagnitude > 0.001f * 0.001f) return false;
            }
            return true;
        }

        bool CheckSpecificDataAwake()
        {
            if (_motorActive) return true;
            for (int i = 0; i < 2; i++)
                if (_attached[i].Body != null && _attached[i].Body.Enabled) return true;
            return false;
        }

        void UpdateMovement(HplVerletParticle p, float timeStep)
        {
            Vector3 acc = p.InvMass == 0f ? Vector3.zero : Gravity;
            Vector3 temp = p.Position;
            p.Position += (p.Position * _dampingMul - p.PrevPosition * _dampingMul) + acc * timeStep * timeStep;
            p.PrevPosition = temp;
        }

        void UpdateMotorAndAutoMove(float timeStep)
        {
            float vel = 0f;
            if (_motorActive)
            {
                float totalLength = _totalLength;
                float dir = MotorWantedLength - _totalLength;
                vel = dir * MotorSpeedMul;
                if (vel > MotorMaxSpeed) vel = MotorMaxSpeed;
                if (vel < MotorMinSpeed) vel = MotorMinSpeed;
                totalLength += vel * timeStep;
                SetTotalLength(totalLength);
                if (Mathf.Abs(MotorWantedLength - totalLength) < 0.02f) _motorActive = false;
            }
            else if (_autoMoveActive)
            {
                float totalLength = _totalLength;
                AutoMoveSpeed += AutoMoveAcc * timeStep;
                if (AutoMoveSpeed > AutoMoveMaxSpeed) AutoMoveSpeed = AutoMoveMaxSpeed;
                if (AutoMoveSpeed < -AutoMoveMaxSpeed) AutoMoveSpeed = -AutoMoveMaxSpeed;
                totalLength += AutoMoveSpeed * timeStep;
                bool stop = false;
                if (totalLength > MaxTotalLength)
                {
                    totalLength = MaxTotalLength;
                    stop = true;
                }
                if (totalLength < MinTotalLength)
                {
                    totalLength = MinTotalLength;
                    stop = true;
                }
                if (stop) AutoMoveSpeed = 0f;
                vel = AutoMoveSpeed;
                SetTotalLength(totalLength);
            }

            // Motor sound.
            if (_motorSoundEntity == null && vel == 0f) return;
            if (Sound == null || Sound.Silent) return;
            if (_motorSoundEntity != null && !Sound.SoundEntityExists(_motorSoundEntity)) _motorSoundEntity = null;

            if (_motorSoundEntity != null && Mathf.Abs(vel) < MotorSoundStopSpeed)
            {
                _motorSoundEntity.FadeOut(10f);
                _motorSoundEntity = null;
            }
            else if (MotorSound != "" && _motorSoundEntity == null && Mathf.Abs(vel) > MotorSoundStartSpeed)
            {
                _motorSoundEntity = Sound.CreateSoundEntity(Name + "_MotorSound", MotorSound, true);
                if (_motorSoundEntity != null)
                {
                    _motorSoundEntity.IsSaved = false;
                    _motorSoundEntity.Position = HplSpace.Position(StartParticle.Position);
                    _motorSoundEntity.FadeIn(5f);
                }
            }
        }

        void UpdateAttachedParticlePositions()
        {
            // cVerletParticle::SetPosition(pos, false) also wakes the container, which is awake here.
            for (int i = 0; i < 2; i++)
                if (_attached[i].Body != null) _attached[i].Particle.Position = AttachmentHplPosition(i);
        }

        void UpdateAttachedBodies(float timeStep)
        {
            for (int i = 0; i < 2; i++)
            {
                var body = _attached[i].Body;
                if (body == null) continue;
                Vector3 wanted = _attached[i].Particle.Position;
                Vector3 current = AttachmentHplPosition(i);
                Vector3 error = wanted - current;

                var pid = _forcePid[i];
                pid.p = Strength;
                pid.i = 0f;
                pid.d = Strength * 0.1f * Stiffness;
                Vector3 force = pid.Output(error, timeStep);

                // From the center of mass (body position + rotated mass centre).
                Vector3 local = current - body.HplWorldCenterOfMass;
                Vector3 torque = Vector3.Cross(local, force);
                body.AddTorqueHpl(HplMatrix.Mul3x3(body.HplInertiaMatrix, torque));
                body.AddForceHpl(force * body.Mass);
            }
        }

        void UpdateConstraints()
        {
            for (int i = 1; i < _particles.Count; i++)
                UpdateLengthConstraint(_particles[i - 1], _particles[i], i == 1 ? _firstSegmentLength : _segmentLength);
        }

        static void UpdateLengthConstraint(HplVerletParticle p1, HplVerletParticle p2, float length)
        {
            Vector3 delta = p2.Position - p1.Position;
            float dist = delta.magnitude;
            // HPL2 divides by zero here (NaN); a NaN force would break the PhysX body attached to the rope.
            if (dist == 0f) return;
            float diff = (dist - length) / (dist * (p1.InvMass + p2.InvMass));
            p1.Position += delta * diff * p1.InvMass;
            p2.Position -= delta * diff * p2.InvMass;
        }

        void CalculateSmoothPositions()
        {
            Vector3 add0 = _attached[0].Body != null ? AttachmentHplPosition(0) - StartParticle.Position : Vector3.zero;
            Vector3 add1 = _attached[1].Body != null ? AttachmentHplPosition(1) - EndParticle.Position : Vector3.zero;
            float maxCount = _particles.Count - 1;
            for (int i = 0; i < _particles.Count; i++)
            {
                float t = i / maxCount;
                _particles[i].SmoothPosition = _particles[i].Position + add0 * (1f - t) + add1 * t;
            }
        }

        /// <summary>
        /// iPhysicsRope::BuildRopeParticles: the particles between the ends for the total length; the first segment
        /// takes the rest of the division. Before the first update the old ones are removed first; new ones are put
        /// along the start direction.
        /// </summary>
        void BuildRopeParticles()
        {
            if (!_hasUpdated && _particles.Count > 2) _particles.RemoveRange(1, _particles.Count - 2);

            int particleNum = _particles.Count - 2;
            int wantedNum = 0;
            if (_segmentLength > 0f)
            {
                wantedNum = (int)(_totalLength / _segmentLength + 0.999999f) - 1;
                _firstSegmentLength = HplMath.Modulus(_totalLength, _segmentLength);
                if (_firstSegmentLength == 0f) _firstSegmentLength = _segmentLength;
            }
            else _firstSegmentLength = _totalLength;

            if (wantedNum == particleNum) return;
            if (wantedNum < particleNum)
            {
                _particles.RemoveRange(1, particleNum - wantedNum);
            }
            else
            {
                Vector3 dir = (_particles[1].Position - _particles[0].Position).normalized;
                int addCount = wantedNum - particleNum;
                Vector3 start = StartParticle.Position;
                float length = _firstSegmentLength + _segmentLength * (addCount - 1);
                // Each one is inserted after the start, before the one added last.
                for (; addCount > 0; addCount--)
                {
                    _particles.Insert(1, new HplVerletParticle(start + dir * length, 1f));
                    length -= _segmentLength;
                }
            }
        }

        // ------------------------------------------------------------------ save (cEngineRope_SaveData)

        public void Restore(List<(Vector3 position, Vector3 prev, Vector3 smooth, float invMass)> particles)
        {
            _particles.Clear();
            foreach (var (position, prev, smooth, invMass) in particles)
                _particles.Add(new HplVerletParticle(position, invMass) { PrevPosition = prev, SmoothPosition = smooth });
            for (int i = 0; i < 2; i++)
                if (_attached[i].Body != null) _attached[i].Particle = i == 0 ? StartParticle : EndParticle;
        }

        /// <summary>Restores the total length and the segment data without rebuilding the particles.</summary>
        public void RestoreLengths(float totalLength, float segmentLength)
        {
            _totalLength = totalLength;
            _segmentLength = segmentLength;
            _hasUpdated = true;
            if (_segmentLength > 0f)
            {
                _firstSegmentLength = HplMath.Modulus(_totalLength, _segmentLength);
                if (_firstSegmentLength == 0f) _firstSegmentLength = _segmentLength;
            }
            else _firstSegmentLength = _totalLength;
        }

        /// <summary>Sets an end's body with a saved local position (cEngineRope_SaveData::ToRope).</summary>
        public void RestoreAttachment(int index, HplPhysicsBody body, Vector3 bodyLocalPos)
        {
            if (body == null) return;
            SetAttachedBody(index, index == 0 ? StartParticle : EndParticle, body);
            _attached[index].BodyLocalPos = bodyLocalPos;
        }
    }
}
