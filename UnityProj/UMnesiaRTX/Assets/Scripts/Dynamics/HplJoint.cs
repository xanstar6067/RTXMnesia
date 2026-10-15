using System;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// A joint between two bodies, or a body and the world (port of iPhysicsJoint, PhysicsJoint.cpp, and the Newton
    /// hinge / ball / slider joints). PhysX keeps the bodies together and enforces the limits; this class keeps the
    /// HPL2 logic around them, called before and after each simulation step like SubmitConstraints and
    /// OnPhysicsUpdate: angle / distance in HPL2's sense, limit callbacks, sticky limits, limit auto sleep, breaking.
    ///
    /// Angles: HPL2's hinge angle (PhysicsJointHingeNewton GetAngle) is minus the right-handed rotation of the child
    /// about the HPL2 pin. Mirrored to Unity it is the rotation (Quaternion.AngleAxis sense) of the child relative to
    /// the parent about <see cref="PinDir"/>, the mirrored pin. Min / max angles keep their HPL2 values.
    /// Limits apply when min or max is not 0 (0 / 0 = free), as in the original.
    /// </summary>
    public sealed class HplJoint
    {
        // Within this of a limit counts as at the limit: PhysX stops bodies on hard limits, where Newton's limit rows
        // let the angle pass the limit for a step.
        const float AngleLimitTolerance = 0.002f;
        const float DistanceLimitTolerance = 0.0005f;

        public readonly string Name;
        public readonly AmnesiaJoint Data;
        public readonly HplJointType Type;
        public readonly HplPhysicsBody Parent, Child;
        public readonly HplPhysicsWorld World;
        public Joint Joint { get; private set; }

        public float MinAngle { get; private set; }
        public float MaxAngle { get; private set; }
        public float MinDistance { get; private set; }
        public float MaxDistance { get; private set; }
        public bool StickyMinLimit, StickyMaxLimit;
        public bool Breakable;
        public float BreakForce;
        public bool Broken { get; private set; }

        /// <summary>Called each step the joint is at a limit, after its first contact only once (mbHasCollided).</summary>
        public event Action<HplJoint> MinLimitReached, MaxLimitReached;

        // Frames at setup, in each body's local space (or world for no parent).
        readonly Vector3 _pivotParent, _pinParent, _rightParent;
        readonly Vector3 _pivotChild, _rightChild;
        float _previousValue;
        int _limitStepCount;
        bool _hasCollided;

        public HplJoint(HplPhysicsWorld world, AmnesiaJoint data, HplPhysicsBody parent, HplPhysicsBody child)
        {
            World = world;
            Data = data;
            Name = data.jointName;
            Type = data.type;
            Parent = parent;
            Child = child;
            StickyMinLimit = data.stickyMinLimit;
            StickyMaxLimit = data.stickyMaxLimit;
            Breakable = data.breakable;
            BreakForce = data.breakForce;
            MinAngle = data.minAngle;
            MaxAngle = data.maxAngle;
            MinDistance = data.minDistance;
            MaxDistance = data.maxDistance;

            Transform entity = data.transform.parent;
            Vector3 pivot = entity.TransformPoint(data.pivot);
            Vector3 pin = entity.TransformDirection(data.pinDir).normalized;
            Vector3 right = Perpendicular(pin);

            _pivotParent = parent != null ? Inverse(parent).MultiplyPoint3x4(pivot) : pivot;
            _pinParent = parent != null ? Inverse(parent).MultiplyVector(pin) : pin;
            _rightParent = parent != null ? Inverse(parent).MultiplyVector(right) : right;
            _pivotChild = Inverse(child).MultiplyPoint3x4(pivot);
            _rightChild = Inverse(child).MultiplyVector(right);

            child.Joints.Add(this);
            parent?.Joints.Add(this);
            CreateJoint();
        }

        static Matrix4x4 Inverse(HplPhysicsBody body) => Matrix4x4.TRS(body.Position, body.Rotation, Vector3.one).inverse;

        static Matrix4x4 Pose(HplPhysicsBody body) => Matrix4x4.TRS(body.Position, body.Rotation, Vector3.one);

        static Vector3 Perpendicular(Vector3 pin)
        {
            Vector3 other = Mathf.Abs(pin.y) < 0.9f ? Vector3.up : Vector3.right;
            return Vector3.Cross(pin, other).normalized;
        }

        // ------------------------------------------------------------------ current state

        /// <summary>World pivot (follows the parent; fixed in the world without one).</summary>
        public Vector3 PivotPoint => Parent != null ? Pose(Parent).MultiplyPoint3x4(_pivotParent) : _pivotParent;

        /// <summary>World pin direction (Unity space).</summary>
        public Vector3 PinDir => Parent != null ? Pose(Parent).MultiplyVector(_pinParent) : _pinParent;

        public Vector3 HplPivotPoint => HplSpace.Position(PivotPoint);
        public Vector3 HplPinDir => HplSpace.Direction(PinDir);

        /// <summary>Hinge angle in radians (HPL2 GetAngle); 0 for other joints.</summary>
        public float GetAngle()
        {
            if (Type != HplJointType.Hinge) return 0f;
            Vector3 r0 = Parent != null ? Pose(Parent).MultiplyVector(_rightParent) : _rightParent;
            Vector3 r1 = Pose(Child).MultiplyVector(_rightChild);
            return Mathf.Atan2(Vector3.Dot(Vector3.Cross(r0, r1), PinDir), Vector3.Dot(r0, r1));
        }

        /// <summary>Slider position along the pin from the start (NewtonSliderGetJointPosit); 0 for other joints.</summary>
        public float GetDistance()
        {
            if (Type != HplJointType.Slider) return 0f;
            Vector3 childPivot = Pose(Child).MultiplyPoint3x4(_pivotChild);
            return Vector3.Dot(childPivot - PivotPoint, PinDir);
        }

        /// <summary>Hinge: angular velocity of the child along the pin (HPL2 space).</summary>
        public Vector3 HplAngularVelocity
        {
            get
            {
                Vector3 w = Child.HplAngularVelocity - (Parent != null ? Parent.HplAngularVelocity : Vector3.zero);
                Vector3 pin = HplPinDir;
                return pin * Vector3.Dot(w, pin);
            }
        }

        public void SetMinAngle(float angle) { if (MinAngle != angle) { MinAngle = angle; ApplyLimits(); } }
        public void SetMaxAngle(float angle) { if (MaxAngle != angle) { MaxAngle = angle; ApplyLimits(); } }
        public void SetMinDistance(float distance) { if (MinDistance != distance) { MinDistance = distance; ApplyLimits(); } }
        public void SetMaxDistance(float distance) { if (MaxDistance != distance) { MaxDistance = distance; ApplyLimits(); } }

        /// <summary>iPhysicsJoint::Break: removed after the next step.</summary>
        public void Break()
        {
            Broken = true;
            Breakable = true;
        }

        // ------------------------------------------------------------------ PhysX joint

        void CreateJoint()
        {
            var go = Child.Data.gameObject;
            Vector3 pivot = PivotPoint;
            Vector3 pin = PinDir;
            Transform ct = Child.Data.transform;

            switch (Type)
            {
                case HplJointType.Hinge:
                {
                    var hinge = go.AddComponent<HingeJoint>();
                    hinge.autoConfigureConnectedAnchor = false;
                    hinge.connectedBody = Parent?.Rigidbody;
                    hinge.anchor = ct.InverseTransformPoint(pivot);
                    hinge.axis = ct.InverseTransformDirection(pin);
                    hinge.connectedAnchor = Parent != null ? Parent.Data.transform.InverseTransformPoint(pivot) : pivot;
                    Joint = hinge;
                    break;
                }
                default:
                {
                    var joint = go.AddComponent<ConfigurableJoint>();
                    joint.autoConfigureConnectedAnchor = false;
                    joint.connectedBody = Parent?.Rigidbody;
                    joint.anchor = ct.InverseTransformPoint(pivot);
                    joint.axis = ct.InverseTransformDirection(pin);
                    joint.secondaryAxis = ct.InverseTransformDirection(Perpendicular(pin));
                    joint.connectedAnchor = Parent != null ? Parent.Data.transform.InverseTransformPoint(pivot) : pivot;
                    joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
                    joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
                    Joint = joint;
                    break;
                }
            }
            Joint.enableCollision = Data.collideBodies;
            Joint.enablePreprocessing = false;
            ApplyLimits();
        }

        void ApplyLimits()
        {
            switch (Joint)
            {
                case HingeJoint hinge:
                {
                    bool limited = MinAngle != 0f || MaxAngle != 0f;
                    // HingeJoint limits are within ±180°. A limit past ±π never triggers in HPL2 (the angle wraps),
                    // so such a range has a free side: then the limits are soft ones done in BeforeSimulate.
                    _softLimits = limited && (MinAngle < -Mathf.PI || MaxAngle > Mathf.PI || MinAngle > MaxAngle);
                    hinge.useLimits = limited && !_softLimits;
                    if (!hinge.useLimits) break;
                    // HingeJoint.angle has the sense of GetAngle (the AngleAxis rotation about the axis; checked by
                    // RTXMnesia.EditorDev.PhysicsTests), so the HPL2 range is the PhysX range.
                    float lo = MinAngle * Mathf.Rad2Deg, hi = MaxAngle * Mathf.Rad2Deg;
                    if (lo > hi) (lo, hi) = (hi, lo);
                    hinge.limits = new JointLimits
                    {
                        min = Mathf.Clamp(lo, -180f, 180f),
                        max = Mathf.Clamp(hi, -180f, 180f),
                        bounciness = 0f,
                        bounceMinVelocity = 0.2f,
                        contactDistance = 0f,
                    };
                    Child.WakeUp();
                    break;
                }
                case ConfigurableJoint joint when Type == HplJointType.Slider:
                {
                    bool limited = MinDistance != 0f || MaxDistance != 0f;
                    if (!limited)
                    {
                        joint.xMotion = ConfigurableJointMotion.Free;
                        break;
                    }
                    // Linear limits are symmetric around the anchor: move the connected anchor to the middle of the
                    // range, so ±half the range is [min, max] from the start.
                    float center = (MinDistance + MaxDistance) * 0.5f;
                    float half = Mathf.Abs(MaxDistance - MinDistance) * 0.5f;
                    Vector3 pivot = (Parent != null ? Pose(Parent).MultiplyPoint3x4(_pivotParent) : _pivotParent) + PinDir * center;
                    joint.connectedAnchor = Parent != null ? Parent.Data.transform.InverseTransformPoint(pivot) : pivot;
                    joint.xMotion = half > 0f ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Locked;
                    joint.linearLimit = new SoftJointLimit { limit = half, bounciness = 0f, contactDistance = 0f };
                    Child.WakeUp();
                    break;
                }
                case ConfigurableJoint joint:
                {
                    // Ball (screws load as balls): NewtonBallSetConeLimits, 0 = no limit.
                    float cone = Data.maxConeAngle * Mathf.Rad2Deg, twist = Data.maxTwistAngle * Mathf.Rad2Deg;
                    joint.angularXMotion = twist > 0f ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
                    joint.lowAngularXLimit = new SoftJointLimit { limit = -Mathf.Min(twist, 177f) };
                    joint.highAngularXLimit = new SoftJointLimit { limit = Mathf.Min(twist, 177f) };
                    var swing = cone > 0f ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
                    joint.angularYMotion = joint.angularZMotion = swing;
                    joint.angularYLimit = new SoftJointLimit { limit = Mathf.Min(cone, 177f) };
                    joint.angularZLimit = new SoftJointLimit { limit = Mathf.Min(cone, 177f) };
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ simulation

        /// <summary>SubmitConstraints (hinge) / LimitCallback (slider): limit callbacks, sticky limits, auto sleep.</summary>
        internal void BeforeSimulate()
        {
            if (Joint == null || !Child.Active) return;
            if (Type == HplJointType.Hinge)
            {
                float angle = GetAngle();
                if (_softLimits) ApplySoftLimit(angle);
                UpdateLimits(angle, MinAngle, MaxAngle, AngleLimitTolerance, true);
            }
            else if (Type == HplJointType.Slider) UpdateLimits(GetDistance(), MinDistance, MaxDistance, DistanceLimitTolerance, false);
        }

        bool _softLimits;

        /// <summary>
        /// Newton-style limit row for ranges PhysX cannot take: past a limit, the angular velocity along the pin
        /// towards it is removed and half the error is corrected per step; moving back stays free.
        /// </summary>
        void ApplySoftLimit(float angle)
        {
            if (Child.IsStatic) return;
            const float dt = 1f / 60f;
            Vector3 pin = PinDir;
            Vector3 relative = Child.AngularVelocity - (Parent != null ? Parent.AngularVelocity : Vector3.zero);
            float w = Vector3.Dot(relative, pin);
            float target;
            if (MaxAngle < Mathf.PI && angle > MaxAngle)
            {
                target = -(angle - MaxAngle) * 0.5f / dt;
                if (w <= target) return;
            }
            else if (MinAngle > -Mathf.PI && angle < MinAngle)
            {
                target = (MinAngle - angle) * 0.5f / dt;
                if (w >= target) return;
            }
            else return;
            Child.AngularVelocity += pin * (target - w);
        }

        void UpdateLimits(float value, float min, float max, float tolerance, bool hinge)
        {
            if (min == 0f && max == 0f) return;

            // CheckLimitAutoSleep
            if (Data.limitAutoSleep)
            {
                if (Mathf.Abs(max - value) < Data.limitAutoSleepDist || Mathf.Abs(min - value) < Data.limitAutoSleepDist)
                {
                    if (_limitStepCount >= Data.limitAutoSleepNumSteps) Child.DisableAfterSimulation();
                    else _limitStepCount++;
                }
                else _limitStepCount = 0;
            }

            // Hinge: an angle jump over 300° is the wrap at ±180°, not a limit.
            bool skip = hinge && Mathf.Abs(_previousValue - value) > 300f * Mathf.Deg2Rad;
            if (value < min + tolerance && !skip) OnLimit(false);
            else if (value > max - tolerance && !skip) OnLimit(true);
            else
            {
                if (Parent == null || Parent.IsStatic)
                {
                    if ((StickyMaxLimit && _previousValue > max - tolerance) || (StickyMinLimit && _previousValue < min + tolerance))
                    {
                        Child.LinearVelocity = Vector3.zero;
                        Child.AngularVelocity = Vector3.zero;
                    }
                }
                _hasCollided = false;
            }
            _previousValue = value;
        }

        void OnLimit(bool max)
        {
            if (!_hasCollided)
            {
                if (max) MaxLimitReached?.Invoke(this);
                else MinLimitReached?.Invoke(this);
            }
            if (max) LimitEffect(Data.maxLimitSound, Data.maxLimitMinSpeed, Data.maxLimitMaxSpeed);
            else LimitEffect(Data.minLimitSound, Data.minLimitMinSpeed, Data.minLimitMaxSpeed);
            _hasCollided = true;
        }

        Sound.HplSoundHandler SoundHandler => Child.World.SurfaceSounds.Sound;

        /// <summary>iPhysicsJoint::LimitEffect: the limit sound at the child body, louder the faster it hits.</summary>
        void LimitEffect(string sound, float minSpeed, float maxSpeed)
        {
            if (string.IsNullOrEmpty(sound) || _hasCollided || SoundHandler == null) return;
            Vector3 vel = Child.LinearVelocity - (Parent != null ? Parent.LinearVelocity : Vector3.zero);
            float speed = Mathf.Min(vel.magnitude, maxSpeed);
            if (speed < minSpeed) return;
            var s = SoundHandler.CreateSoundEntity("LimitSound", sound, true);
            if (s == null) return;
            s.IsSaved = false;
            s.Volume = (speed - minSpeed) / (maxSpeed - minSpeed);
            s.Position = Child.Position;
        }

        Sound.HplSoundEntity _moveSound;
        int _speedCount;
        Matrix4x4 _prevChild, _prevParent;

        /// <summary>
        /// iPhysicsJoint::OnPhysicsUpdate: the move sound (a door's creak) while the child moves faster than
        /// MinMoveSpeed relative to the parent, its pitch and volume by the speed (CalcSoundFreq).
        /// </summary>
        internal void UpdateSound()
        {
            bool frozen = !(Parent != null && Parent.Enabled) && !Child.Enabled;
            if (frozen && _moveSound == null) return;
            if (string.IsNullOrEmpty(Data.moveSound) || SoundHandler == null || SoundHandler.Silent) return;

            Vector3 vel = Data.moveSpeedAngular
                ? Child.AngularVelocity - (Parent != null ? Parent.AngularVelocity : Vector3.zero)
                : Child.LinearVelocity - (Parent != null ? Parent.LinearVelocity : Vector3.zero);
            Matrix4x4 child = Child.Data.transform.localToWorldMatrix;
            Matrix4x4 parent = Parent != null ? Parent.Data.transform.localToWorldMatrix : Matrix4x4.identity;
            if (child == _prevChild && parent == _prevParent) vel = Vector3.zero;
            _prevChild = child;
            _prevParent = parent;
            float speed = vel.magnitude;

            if (_moveSound != null && !SoundExists(_moveSound)) _moveSound = null;
            if (_moveSound != null)
            {
                if (speed <= Mathf.Max(Data.minMoveSpeed - 0.2f, 0.1f))
                {
                    _moveSound.FadeOut(4.3f);
                    _moveSound = null;
                    return;
                }
                var entry = _moveSound.GetEntry(Sound.SoundEntityType.Main);
                if (entry != null)
                {
                    CalcSoundFreq(speed, out float freq, out float volume);
                    entry.SetSpeedMul(freq);
                    entry.SetVolumeMul(volume);
                }
                _moveSound.Position = PivotPoint;
            }
            else if (speed > Data.minMoveSpeed)
            {
                if (_speedCount >= 3)
                {
                    _speedCount = 0;
                    _moveSound = SoundHandler.CreateSoundEntity("MoveSound", Data.moveSound, true);
                    if (_moveSound != null)
                    {
                        _moveSound.IsSaved = false;
                        _moveSound.FadeIn(3.3f);
                        _moveSound.Position = PivotPoint;
                    }
                }
                else _speedCount++;
            }
            else _speedCount = 0;
        }

        bool SoundExists(Sound.HplSoundEntity sound)
        {
            foreach (var s in SoundHandler.SoundEntities) if (s == sound) return true;
            return false;
        }

        void CalcSoundFreq(float speed, out float freq, out float volume)
        {
            float abs = Mathf.Abs(speed);
            if (abs >= Data.middleMoveSpeed)
            {
                if (abs >= Data.maxMoveFreqSpeed)
                {
                    freq = Data.maxMoveFreq;
                    volume = Data.maxMoveVolume;
                    return;
                }
                float t = (abs - Data.middleMoveSpeed) / (Data.maxMoveFreqSpeed - Data.middleMoveSpeed);
                freq = (1f - t) + t * Data.maxMoveFreq;
                volume = Data.middleMoveVolume * (1f - t) + t * Data.maxMoveVolume;
                return;
            }
            if (abs <= Data.minMoveFreqSpeed)
            {
                freq = Data.minMoveFreq;
                volume = Data.minMoveVolume;
                return;
            }
            float u = (Data.middleMoveSpeed - abs) / (Data.middleMoveSpeed - Data.minMoveFreqSpeed);
            freq = (1f - u) + u * Data.minMoveFreq;
            volume = Data.middleMoveVolume * (1f - u) + u * Data.minMoveVolume;
        }

        /// <summary>CheckBreakage: true when the joint is to be removed.</summary>
        internal bool CheckBreakage()
        {
            if (Joint == null) return true;
            if (!Breakable) return false;
            float force = Joint.currentForce.magnitude;
            if (force >= BreakForce || Broken)
            {
                if (!string.IsNullOrEmpty(Data.breakSound) && SoundHandler != null)
                {
                    var s = SoundHandler.CreateSoundEntity("BreakSound", Data.breakSound, true);
                    if (s != null)
                    {
                        s.IsSaved = false;
                        s.Position = PivotPoint;
                    }
                }
                return true;
            }
            return false;
        }

        internal void Destroy()
        {
            Child.Joints.Remove(this);
            Parent?.Joints.Remove(this);
            if (Joint != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(Joint);
                else UnityEngine.Object.DestroyImmediate(Joint);
            }
            Joint = null;
        }
    }
}
