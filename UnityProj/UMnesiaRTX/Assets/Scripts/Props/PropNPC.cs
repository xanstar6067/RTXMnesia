using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_NPC (LuxProp_NPC.cpp): a living character without AI (Agrippa, Alexander, the chained prisoner).
    /// Awake / asleep plays Awake / Sleep, then Idle / SleepIdle; without effects straight to the idle loops. When the
    /// player is inside the FollowPlayerArea (checked twice per second) and the NPC is awake and follows, the head
    /// turns towards him: the angle in the mesh's xz plane, at most MaxHeadAngle, turned at HeadMoveSpeedMul × the
    /// angle distance (at most HeadMoveMaxSpeed), split over MoveHeadBones × MoveHeadBoneMuls as a rotation after the
    /// animation (bone pre-transform). No interaction.
    /// </summary>
    public sealed class PropNPC : HplProp
    {
        sealed class HeadMoveBone
        {
            public Transform bone;
            public float mul;
            public Vector3 rotVec;   // HPL2, bone space
        }

        readonly List<HeadMoveBone> _headMoveBones = new();
        float _headMoveSpeedMul, _headMoveMaxSpeed, _maxHeadAngle;

        // Saved like cLuxProp_NPC_SaveData (simple fields).
        string _followPlayerArea = "";
        float _headAngle;
        bool _playerIsInArea;
        bool _awake = true;
        bool _playingWakeAnim;
        bool _followPlayer = true;
        float _inAreaCount;

        // Not saved (a readonly holder): resolved again after a load, from the saved name.
        sealed class FollowAreaRef { public HplEntity area; public bool resolved; }
        readonly FollowAreaRef _follow = new();

        public PropNPC(World.AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        public bool Awake => _awake;
        public float HeadAngle => _headAngle;
        public bool PlayerIsInArea => _playerIsInArea;
        public bool FollowPlayer { get => _followPlayer; set => _followPlayer = value; }

        /// <summary>cLuxPropLoader_NPC::LoadVariables.</summary>
        protected override void LoadVariables()
        {
            string[] names = VarString("MoveHeadBones", "").Split(new[] { ' ', ',', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            string[] muls = VarString("MoveHeadBoneMuls", "").Split(new[] { ' ', ',', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (Animation == null || AnimatedModel == null)
                HplLog.Error($"NPC '{Component.file}' is missing skeleton!");
            else if (names.Length == 0 || names.Length != muls.Length)
                HplLog.Error($"MoveHeadBones array is emtpy or do not match size of MoveHeadBoneMuls for NPC '{Component.file}'");
            else
            {
                var root = AnimatedModel.transform;
                for (int i = 0; i < names.Length; i++)
                {
                    var bone = FindBone(root, names[i]);
                    if (bone == null)
                    {
                        HplLog.Error($"Move head Bone '{names[i]}' does not exist in '{Component.file}'!");
                        continue;
                    }
                    // The local vector of the bone that is up (0,1,0) in the skeleton. HPL2 multiplies the point
                    // (0,1,0), so the bone's position shifts it too — kept.
                    Matrix4x4 boneInSkeleton = HplSpace.Mirror(root.worldToLocalMatrix * bone.localToWorldMatrix);
                    Vector3 v = boneInSkeleton.inverse.MultiplyPoint3x4(new Vector3(0f, 1f, 0f));
                    _headMoveBones.Add(new HeadMoveBone { bone = bone, mul = HplMath.Atof(muls[i]), rotVec = v.normalized });
                }
            }
            _headMoveSpeedMul = VarFloat("HeadMoveSpeedMul", 0f);
            _headMoveMaxSpeed = VarFloat("HeadMoveMaxSpeed", 0f);
            _maxHeadAngle = VarFloat("MaxHeadAngle", 0f) * Mathf.Deg2Rad;
        }

        static Transform FindBone(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindBone(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>cLuxPropLoader_NPC::LoadInstanceVariables.</summary>
        protected override void LoadInstanceVariables()
        {
            _followPlayerArea = InstanceString("FollowPlayerArea", "");
            SetAwake(InstanceBool("Awake", true), false);
        }

        /// <summary>
        /// cLuxProp_NPC::AfterWorldLoad, done at the first update (the other entities are loaded by then): the follow
        /// area by name.
        /// </summary>
        void ResolveFollowArea()
        {
            _follow.resolved = true;
            if (_followPlayerArea == "")
            {
                HplLog.Warn($"NPC '{Name}' does not have a follow player area!");
                return;
            }
            _follow.area = Map.GetEntityByName(_followPlayerArea);
            if (_follow.area == null) HplLog.Error($"Could not find follow area '{_followPlayerArea}' for NPC '{Name}'!");
        }

        public override bool CanInteract(Dynamics.HplPhysicsBody body) => false;

        /// <summary>cLuxProp_NPC::SetAwake.</summary>
        public void SetAwake(bool awake, bool effects)
        {
            if (_awake == awake) return;
            _awake = awake;
            if (effects)
            {
                PlayAnimation(_awake ? "Awake" : "Sleep", 0.3f, false, "");
                _playingWakeAnim = true;
            }
            else PlayAnimation(_awake ? "Idle" : "SleepIdle", 0.05f, true, "");
        }

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (!_follow.resolved) ResolveFollowArea();
            UpdateWakeState();
            UpdateCheckPlayerIsInArea(timeStep);
            UpdateHeadMovement(timeStep);
        }

        /// <summary>cLuxProp_NPC::UpdateWakeState: after Awake / Sleep, the idle loop.</summary>
        void UpdateWakeState()
        {
            if (!_playingWakeAnim) return;
            if (NonLoopAnimationPlaying) return;
            _playingWakeAnim = false;
            PlayAnimation(_awake ? "Idle" : "SleepIdle", 0.3f, true, "");
        }

        /// <summary>cLuxProp_NPC::UpdateCheckPlayerIsInArea: twice per second.</summary>
        void UpdateCheckPlayerIsInArea(float timeStep)
        {
            if (_follow.area == null) return;
            _inAreaCount -= timeStep;
            if (_inAreaCount > 0f) return;
            _inAreaCount = 0.5f;
            _playerIsInArea = _follow.area.CollidesWithPlayer();
        }

        /// <summary>cLuxProp_NPC::UpdateHeadMovement: the angle (the bones follow after the pose, see AfterAnimationPose).</summary>
        void UpdateHeadMovement(float timeStep)
        {
            float wantedAngle = 0f;
            if (_playerIsInArea && _awake && _followPlayer && AnimatedModel != null && Map.player != null)
            {
                // Rows of the inverse mesh matrix = the mesh's axes (HPL2 space).
                var mesh = AnimatedModel.transform;
                Vector3 right = HplSpace.Direction(-mesh.right);
                Vector3 fwd = HplSpace.Direction(mesh.forward);
                Vector3 toPlayer = HplSpace.Position(Map.player.Body.Position) - HplSpace.Position(mesh.position);
                fwd.y = 0f; fwd.Normalize();
                right.y = 0f; right.Normalize();
                toPlayer.y = 0f; toPlayer.Normalize();
                wantedAngle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(fwd, toPlayer), -1f, 1f));
                if (Vector3.Dot(right, toPlayer) < 0f) wantedAngle = -wantedAngle;
                wantedAngle = Mathf.Clamp(wantedAngle, -_maxHeadAngle, _maxHeadAngle);
            }
            if (Mathf.Abs(_headAngle - wantedAngle) > 0.0001f)
            {
                float dist = HplMatrix.AngleDistanceRad(_headAngle, wantedAngle);
                float turnSpeed = Mathf.Clamp(_headMoveSpeedMul * dist, -_headMoveMaxSpeed, _headMoveMaxSpeed);
                _headAngle += turnSpeed * timeStep;
            }
        }

        /// <summary>
        /// The bone pre-transforms of UpdateHeadMovement (local = animated local × rotation about rotVec): in Unity's
        /// mirrored bone space the HPL2 rotation is the negated angle about the mirrored axis.
        /// </summary>
        protected override void AfterAnimationPose()
        {
            if (Animation == null) return;
            bool posed = false;
            foreach (var s in Animation.States) if (s.IsActive) { posed = true; break; }
            if (!posed) return;
            foreach (var b in _headMoveBones)
            {
                var rotation = Quaternion.AngleAxis(-_headAngle * b.mul * Mathf.Rad2Deg, HplSpace.Direction(b.rotVec));
                b.bone.localRotation *= rotation;
            }
        }
    }
}
