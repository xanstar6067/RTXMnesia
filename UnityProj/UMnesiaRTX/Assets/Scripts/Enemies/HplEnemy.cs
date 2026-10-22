using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.Props;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>eLuxEnemyState (LuxEnemy.h), same order; the names of gsLuxEnemyStates.</summary>
    public enum EnemyState
    {
        Idle, GoHome, Wait, Patrol, Investigate, Alert, Search, Eat, Hurt, Hunt, HuntPause, HuntWander,
        AttackMeleeShort, AttackMeleeLong, AttackRange, BreakDoor, Dead, PigEnumStart, Flee, Stalk, Track, LastEnum
    }

    public enum EnemyStateEvent { Enter, Leave, Update, Message }

    /// <summary>eLuxEnemyMessage (LuxTypes.h).</summary>
    public enum EnemyMessage
    {
        TimeOut, TimeOut_2, TimeOut_3, TimeOut_4, AnimationOver, AnimationSpecialEvent, EndOfPath, FoodInRange,
        PlayerSeen, PlayerUnseen, PlayerDetected, PlayerUndetected, SoundHeard, PlayerInRange, PlayerOutOfRange,
        PlayerDead, ChangePose, HelpMe, StuckAtDoor, TakeHit, Death, Reset, LastEnum
    }

    public enum EnemyMusic { Search, Attack }
    public enum EnemySoundState { Silent, Idle, Alert, Hunt }
    public enum EnemyMoveSpeed { Walk, Run }

    /// <summary>cLuxStateMessage. The custom vector is in Unity space.</summary>
    public sealed class EnemyStateMessage
    {
        public EnemyMessage type;
        public float time, count;
        public Vector3 customVector;
        public float customFloat;
        public int customInt;
        public EnemyState state;
    }

    /// <summary>cLuxEnemyPatrolNode.</summary>
    public sealed class EnemyPatrolNode
    {
        public AINode node;
        public float waitTime;
        public string animation;
        public bool loopAnimation;
    }

    /// <summary>cEnemyAttackDamageData.</summary>
    public sealed class EnemyAttackDamage
    {
        public float minDamage, maxDamage, force, maxImpulse;
        public int strength;
        public float hitSpeed = 3f;
        public DamageType damageType = DamageType.BloodSplat;
        public bool checkPlayer = true, checkProps = true;
        public string hitSound = "";
    }

    /// <summary>cEnemyAttackSizeData: a box shape (the attack shapes are boxes), offset right / up / forward.</summary>
    public sealed class EnemyAttackSize
    {
        public Vector3 size, offset;
    }

    /// <summary>
    /// An enemy (iLuxEnemy, LuxEnemy.cpp, and iLuxEnemyLoader): a character body moved by <see cref="EnemyMover"/> and
    /// <see cref="EnemyPathfinder"/>, senses (sight every 0.3 s with the darkness, crouch and lantern rules, detection,
    /// activation range, being stuck at a door, the last known player position), a state machine driven by events and
    /// delayed messages, animations with their events, ambient sounds by sound state, health regeneration,
    /// hallucinations, the ground ray alignment of the mesh, attacks with a box shape.
    ///
    /// Angles are worked in HPL2 space (the yaw of the body, FOV tests, search directions); positions are Unity space.
    /// </summary>
    public abstract class HplEnemy : HplEntity
    {
        static readonly string[] StateNames =
        {
            "Idle", "GoHome", "Wait", "Patrol", "Investigate", "Alert", "Search", "Eat", "Hurt", "Hunt", "HuntPause",
            "HuntWander", "AttackMeleeShort", "AttackMeleeLong", "AttackRange", "BreakDoor", "Dead", "NULL", "Flee",
            "Stalk", "Track", "NULL",
        };

        public readonly AmnesiaEntity Component;
        public override HplEntityType Type => HplEntityType.Enemy;
        public string FileName => Component.file;

        // ------------------------------------------------------------------ data (loader)
        public HplCharacterBody Body { get; private set; }
        public GameObject Model { get; private set; }
        public HplMeshAnimation Animation { get; private set; }
        public EnemyPathfinder Pathfinder { get; private set; }
        public EnemyMover Mover { get; private set; }

        protected int Toughness;
        protected float SightRange, DarknessSightRange, HearVolume, Fov, FovXMul = 1f, ActivationDistance;
        protected float MaxRegenHealth = 100f, RegenHealthSpeed;
        public string DangerMusic { get; private set; } = "";
        public int DangerMusicPrio { get; private set; }
        readonly string[] _music = { "", "" };
        readonly int[] _musicPrio = new int[2];
        protected bool AutoRemoveAtPathEnd = true;
        protected float PlayerSearchMaxAngle, PlayerSearchMinDistMul, PlayerSearchMaxDistMul, PlayerSearchTime;
        protected float PlayerPatrolMaxAngle, PlayerPatrolMinDist, PlayerPatrolMaxDist;
        internal float TurnSpeedMul = 1f, TurnMaxSpeed = 1f, TurnMinBreakAngle, TurnBreakMul = 1f, MoveSpeedAnimMul = 1f;
        internal float StoppedToWalkSpeed, WalkToStoppedSpeed, WalkToRunSpeed, RunToWalkSpeed;
        readonly float[] _defaultForwardSpeed = new float[2], _defaultBackwardSpeed = new float[2];
        readonly float[] _defaultForwardAcc = new float[2], _defaultForwardDeacc = new float[2];
        readonly string[] _hitPS = new string[4], _hitSound = new string[4];
        readonly string[] _ambientSound = { "", "", "", "" };
        readonly float[] _ambientMinTime = new float[4], _ambientMaxTime = new float[4];
        protected float NormalAttackDistance;
        protected readonly EnemyAttackDamage NormalAttackDamage = new(), BreakDoorAttackDamage = new();
        protected readonly EnemyAttackSize NormalAttackSize = new();
        float _enemyDarknessGlowMaxDistance;
        const float PlayerInDarknessLightLevel = 0.35f, CrouchVisibleRangeMul = 0.5f;
        protected bool AlignEntityWithGroundRay;
        Matrix4x4 _charMeshOffsetHpl = Matrix4x4.identity;
        Matrix4x4 _modelLocal = Matrix4x4.identity;
        readonly List<float> _entityYPositions = new();
        const int EntitySmoothYPosNum = 20;

        // ------------------------------------------------------------------ variables
        public bool Disabled { get; private set; }
        public bool DisableTriggers { get; private set; }
        bool _hallucination;
        float _hallucinationEndDist = 3f;
        public float Health { get; protected set; } = 100f;
        public bool CausesSanityDecrease { get; set; } = true;
        protected bool CausesSanityDecreaseAsDefault = true;

        public EnemyState CurrentState { get; private set; } = EnemyState.LastEnum;
        protected EnemyState NextState = EnemyState.Idle;
        protected EnemyState PreviousState = EnemyState.LastEnum;
        public EnemySoundState SoundState { get; private set; } = EnemySoundState.Silent;
        Sound.HplSoundEntity _currentSound;
        float _ambientSoundCount;

        float _lookForPlayerCount;
        int _playerInLosCount;
        protected bool CanSeePlayerFlag;
        public bool CanSeePlayer => CanSeePlayerFlag;
        public bool PlayerDetected { get; protected set; }
        protected bool PlayerInRangeFlag;
        public bool PlayerInRange => PlayerInRangeFlag;

        float _checkAtDoorCount;
        protected bool StuckAtDoor;
        protected HplEntity StuckDoor;

        protected float ForwardSpeed = 1f, BackwardSpeed = 1f, ForwardAcc = 1f, ForwardDeacc = 1f;
        float _darknessGlowAlpha, _darknessGlowAlphaGoal, _darknessGlowUpdateCount;
        public float DarknessGlowAlpha => _darknessGlowAlpha;
        protected float FovMul = 1f;

        internal HplAnimationState CurrentAnimation;
        bool _animationIsSpeedDependant;
        float _animationSpeedMul = 1f;
        bool _useMoveAnimWhenCurrentIsOver = true;

        readonly List<EnemyStateMessage> _messages = new();
        public Vector3 StartPosition { get; private set; }
        AINode _lastSearchNode;
        public Vector3 LastKnownPlayerPos { get; protected set; }
        float _lastPlayerPosCount;
        protected int AttackHitCounter;
        protected Vector3 TempPos;
        protected float TempVal;
        protected float TempIntVal; // mlTempVal is a float in LuxEnemy.h
        protected float DamageCount;
        protected readonly List<EnemyPatrolNode> PatrolNodes = new();
        protected int CurrentPatrolNode;
        public bool IsSeenByPlayer { get; set; }
        float _checkGroundRayCount;
        readonly List<float> _groundCheckDists = new();
        const int MaxGroundCheckDists = 3;
        float _groundOffset;
        internal int CurrentPose; // eLuxEnemyPoseType: 0 biped

        // Water (set by a liquid area, cLuxArea_Liquid::DoBuoyancyOnCharBody): step splashes of the mover.
        public bool IsInWater { get; set; }
        public float WaterSurfaceY { get; set; }
        public HplSurfaces.Surface WaterSurface { get; set; }
        internal float WaterStepSpeedWalk, WaterStepSpeedRun, WaterStepSpeedMisc;

        /// <summary>The yaw of the body in HPL2 space (cCharacterBody::GetYaw).</summary>
        float _hplYaw;

        protected HplEnemy(AmnesiaEntity entity) : base(entity.gameObject, entity.hplId)
        {
            Component = entity;
        }

        // ------------------------------------------------------------------ loading

        public string Var(string name, string fallback = "") => Component.GetVariable(name) ?? fallback;
        string EntVar(string name, string fallback = "") => HplVariable.Find(Component.variables, name) ?? fallback;
        protected string InstVar(string name) => HplVariable.Find(Component.instanceVariables, name);
        protected float VarFloat(string name, float fallback = 0f) { string s = EntVar(name, null); return s == null ? fallback : HplMath.Atof(s); }
        protected int VarInt(string name, int fallback = 0) { string s = EntVar(name, null); return s == null ? fallback : HplMath.Atoi(s); }
        protected bool VarBool(string name, bool fallback = false) { string s = EntVar(name, null); return s == null ? fallback : s == "true"; }
        protected string VarString(string name, string fallback = "") => EntVar(name, fallback);
        protected Vector3 VarVector3(string name, Vector3 fallback) => ParseVector(EntVar(name, null), fallback);
        protected Vector2 VarVector2(string name, Vector2 fallback)
        {
            string s = EntVar(name, null);
            if (s == null) return fallback;
            var parts = s.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? new Vector2(HplMath.Atof(parts[0]), HplMath.Atof(parts[1])) : fallback;
        }

        static Vector3 ParseVector(string s, Vector3 fallback)
        {
            if (s == null) return fallback;
            var parts = s.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 3 ? new Vector3(HplMath.Atof(parts[0]), HplMath.Atof(parts[1]), HplMath.Atof(parts[2])) : fallback;
        }

        /// <summary>
        /// iLuxEnemyLoader::AfterLoad + iLuxEnemy::SetupAfterLoad / AfterWorldLoad: properties, the character body at the
        /// entity's position (yaw 0: HPL2 does not take the map rotation), movement, attacks, ambient sounds, the type's
        /// variables, the mover / pathfinder and the node container of the map; then the instance variables.
        /// </summary>
        internal void Load(HplMap map)
        {
            Map = map;
            var cfg = map.gameConfig;

            Health = VarFloat("Health", 100f);
            Toughness = VarInt("Toughness", 0);
            SightRange = VarFloat("SightRange");
            DarknessSightRange = VarFloat("DarknessSightRange");
            HearVolume = VarFloat("HearVolume");
            Fov = VarFloat("FOV") * Mathf.Deg2Rad;
            FovXMul = VarFloat("FOVXMul", 1f);
            MaxRegenHealth = VarFloat("MaxRegenHealth", 100f);
            RegenHealthSpeed = VarFloat("RegenHealthSpeed");
            ActivationDistance = VarFloat("ActivationDistance");
            DangerMusic = VarString("DangerMusic");
            DangerMusicPrio = VarInt("DangerMusicPrio");
            _music[(int)EnemyMusic.Search] = VarString("SearchMusic");
            _musicPrio[(int)EnemyMusic.Search] = VarInt("SearchMusicPrio");
            _music[(int)EnemyMusic.Attack] = VarString("AttackMusic");
            _musicPrio[(int)EnemyMusic.Attack] = VarInt("AttackMusicPrio");
            AutoRemoveAtPathEnd = VarBool("AutoRemoveAtPathEnd", true);

            PlayerSearchMaxAngle = VarFloat("PlayerSearchMaxAngle") * Mathf.Deg2Rad;
            PlayerSearchMinDistMul = VarFloat("PlayerSearchMinDistMul");
            PlayerSearchMaxDistMul = VarFloat("PlayerSearchMaxDistMul");
            PlayerSearchTime = VarFloat("PlayerSearchTime");
            PlayerPatrolMaxAngle = VarFloat("PlayerPatrolMaxAngle") * Mathf.Deg2Rad;
            PlayerPatrolMinDist = VarFloat("PlayerPatrolMinDist");
            PlayerPatrolMaxDist = VarFloat("PlayerPatrolMaxDist");

            // Character body
            Vector3 size = VarVector3("Body_Size", Vector3.one);
            Vector3 feet = GameObject.transform.position;
            Model = FindModel();
            if (Model != null)
            {
                Model.transform.SetParent(null, true);
                _modelLocal = GameObject.transform.worldToLocalMatrix * Model.transform.localToWorldMatrix;
            }
            GameObject.transform.rotation = Quaternion.identity;
            Body = GameObject.AddComponent<HplCharacterBody>();
            Body.mass = VarFloat("Body_Mass", 1f);
            Body.accurateClimbing = VarBool("Body_AccurateClimbing");
            Body.maxNoSlideSlopeAngle = VarFloat("Body_MaxNoSlideSlopeAngle");
            Body.maxPushMass = VarFloat("Body_MaxPushMass");
            Body.pushForce = VarFloat("Body_PushForce");
            Body.maxStepHeight = VarFloat("Body_MaxStepSize");
            Body.maxStepHeightInAir = VarFloat("Body_MaxStepSize");
            Body.stepClimbSpeed = VarFloat("Body_StepClimbSpeed");
            Body.cameraSmoothPosNum = 0;
            Body.Init(size);
            Body.SetFeetPosition(feet);
            if (Model != null) Model.transform.SetParent(GameObject.transform, true);
            Vector3 offsetRot = VarVector3("Body_OffsetRot", Vector3.zero) * Mathf.Deg2Rad;
            _charMeshOffsetHpl = HplMatrix.SetTranslation(HplMatrix.RotateXYZ(offsetRot),
                VarVector3("Body_OffsetTrans", Vector3.zero) - new Vector3(0f, Body.Size.y / 2f, 0f));
            SetHplYaw(0f);

            // Movement
            _defaultForwardSpeed[0] = VarFloat("Walk_ForwardSpeed");
            _defaultBackwardSpeed[0] = VarFloat("Walk_BackwardSpeed");
            _defaultForwardAcc[0] = VarFloat("Walk_ForwardAcc");
            _defaultForwardDeacc[0] = VarFloat("Walk_ForwardDeacc");
            _defaultForwardSpeed[1] = VarFloat("Run_ForwardSpeed");
            _defaultBackwardSpeed[1] = VarFloat("Run_BackwardSpeed");
            _defaultForwardAcc[1] = VarFloat("Run_ForwardAcc");
            _defaultForwardDeacc[1] = VarFloat("Run_ForwardDeacc");
            TurnSpeedMul = VarFloat("TurnSpeedMul", 1f);
            TurnMaxSpeed = VarFloat("TurnMaxSpeed", 1f);
            TurnMinBreakAngle = VarFloat("TurnMinBreakAngle", 1f) * Mathf.Deg2Rad;
            TurnBreakMul = VarFloat("TurnBreakMul", 1f);
            MoveSpeedAnimMul = VarFloat("MoveSpeedAnimMul", 1f);
            StoppedToWalkSpeed = VarFloat("StoppedToWalkSpeed");
            WalkToStoppedSpeed = VarFloat("WalkToStoppedSpeed");
            WalkToRunSpeed = VarFloat("WalkToRunSpeed");
            RunToWalkSpeed = VarFloat("RunToWalkSpeed");

            // Hits (bullet uses the club values, as in the loader)
            _hitPS[0] = VarString("HitPS_Sword"); _hitSound[0] = VarString("HitSound_Sword");
            _hitPS[1] = VarString("HitPS_Club"); _hitSound[1] = VarString("HitSound_Club");
            _hitPS[2] = VarString("HitPS_Club"); _hitSound[2] = VarString("HitSound_Club");
            _hitPS[3] = VarString("HitPS_Dud"); _hitSound[3] = VarString("HitSound_Dud");

            WaterStepSpeedWalk = VarFloat("WaterStepSpeedWalk");
            WaterStepSpeedRun = VarFloat("WaterStepSpeedRun");
            WaterStepSpeedMisc = VarFloat("WaterStepSpeedMisc");

            NormalAttackDistance = VarFloat("NormalAttackDistance");
            // HARDMODE (iLuxEnemyLoader::Load): sharper senses, longer search, faster movement and turning.
            if (HplDifficulty.HardMode)
            {
                SightRange *= 1.15f;
                DarknessSightRange *= 1.15f;
                HearVolume *= 0.6f;
                Fov *= 1.1f;
                FovXMul *= 1.1f;
                PlayerSearchMaxAngle *= 1.1f;
                PlayerSearchMinDistMul *= 1.25f;
                PlayerSearchMaxDistMul *= 1.25f;
                PlayerSearchTime *= 2.5f;
                PlayerPatrolMaxAngle *= 1.25f;
                PlayerPatrolMinDist *= 1.5f;
                PlayerPatrolMaxDist *= 1.5f;
                for (int i = 0; i < 2; i++)
                {
                    float mul = i == 0 ? 1.1f : 1.2f;
                    _defaultForwardSpeed[i] *= mul;
                    _defaultBackwardSpeed[i] *= mul;
                    _defaultForwardAcc[i] *= mul;
                    _defaultForwardDeacc[i] *= mul;
                }
                TurnSpeedMul *= 1.35f;
                WaterStepSpeedWalk *= 1.5f;
                WaterStepSpeedRun *= 1.5f;
            }
            LoadAttackDamage("Normal", NormalAttackDamage);
            LoadAttackDamage("BreakDoor", BreakDoorAttackDamage);
            NormalAttackSize.size = VarVector3("NormalDamageSize", Vector3.zero);
            NormalAttackSize.offset = VarVector3("NormalDamageOffset", Vector3.zero);

            _ambientSound[(int)EnemySoundState.Idle] = VarString("AmbientSound_Idle");
            _ambientMinTime[(int)EnemySoundState.Idle] = VarFloat("AmbientMinTime_Idle");
            _ambientMaxTime[(int)EnemySoundState.Idle] = VarFloat("AmbientMaxTime_Idle");
            _ambientSound[(int)EnemySoundState.Alert] = VarString("AmbientSound_Alert");
            _ambientMinTime[(int)EnemySoundState.Alert] = VarFloat("AmbientMinTime_Alert");
            _ambientMaxTime[(int)EnemySoundState.Alert] = VarFloat("AmbientMaxTime_Alert");
            _ambientSound[(int)EnemySoundState.Hunt] = VarString("AmbientSound_Hunt");
            _ambientMinTime[(int)EnemySoundState.Hunt] = VarFloat("AmbientMinTime_Hunt");
            _ambientMaxTime[(int)EnemySoundState.Hunt] = VarFloat("AmbientMaxTime_Hunt");

            _enemyDarknessGlowMaxDistance = cfg != null ? cfg.GetFloat("Enemy", "EnemyDarknessGlowMaxDistance", 0f) : 0f;

            LoadVariables();

            // iLuxEnemy::SetupAfterLoad
            Mover = new EnemyMover(this, Body);
            Pathfinder = new EnemyPathfinder(this, Mover);
            OnSetupAfterLoad();
            SetMoveSpeed(EnemyMoveSpeed.Walk);

            if (Model != null)
            {
                Animation = new HplMeshAnimation(Model, HplEnemyAssets.Instance != null ? HplEnemyAssets.Instance.GetEnemy(FileName) : null);
                if (Animation.States.Count == 0) HplLog.Warn($"Enemy '{Name}': no animations (Tools > RTXMnesia > Import > Enemies)");
            }

            // Instance variables
            CallbackFunc = InstVar("CallbackFunc") ?? "";
            DisableTriggers = InstVar("DisableTriggers") == "true";
            _hallucination = InstVar("Hallucination") == "true";
            string endDist = InstVar("HallucinationEndDist");
            _hallucinationEndDist = endDist != null ? HplMath.Atof(endDist) : 0f;
            string pose = InstVar("Pose") ?? "biped";
            if (pose.ToLowerInvariant() != "biped")
            {
                if (pose.ToLowerInvariant() == "quadruped") CurrentPose = 1;
                else HplLog.Error($"Pose type '{pose}' does not exist! Using biped");
            }
            LoadInstanceVariables();
            UpdateEntity();
        }

        GameObject FindModel()
        {
            var skinned = GameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinned == null) return null;
            Transform t = skinned.transform;
            while (t.parent != null && t.parent != GameObject.transform) t = t.parent;
            return t.gameObject;
        }

        void LoadAttackDamage(string prefix, EnemyAttackDamage data)
        {
            Vector2 damage = VarVector2(prefix + "AttackDamage", Vector2.zero);
            Vector2 force = VarVector2(prefix + "AttackForce", Vector2.zero);
            data.hitSound = VarString(prefix + "AttackHitSound", "");
            data.strength = VarInt(prefix + "AttackStrength", 0);
            data.minDamage = damage.x;
            data.maxDamage = damage.y;
            // HARDMODE (iLuxEnemyLoader::LoadAttackDamageData): double damage.
            if (HplDifficulty.HardMode)
            {
                data.minDamage *= 2f;
                data.maxDamage *= 2f;
            }
            data.force = force.x;
            data.maxImpulse = force.y;
            data.damageType = ToDamageType(VarString(prefix + "DamageType", "bloodsplat"));
        }

        static DamageType ToDamageType(string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "bloodsplat": return DamageType.BloodSplat;
                case "claws": return DamageType.Claws;
                case "slash": return DamageType.Slash;
            }
            HplLog.Error($"Damage type '{type}' does not exist!");
            return DamageType.BloodSplat;
        }

        /// <summary>iLuxEnemy::AfterWorldLoad: the node container of the enemy's file (cLuxEnemyPathfinder).</summary>
        internal void AfterWorldLoad()
        {
            Pathfinder.AfterWorldLoad();
            OnAfterWorldLoad();
        }

        /// <summary>iLuxEnemy::OnMapEnter.</summary>
        internal void OnMapEnter() => StartPosition = Body.FeetPosition + new Vector3(0f, 0.1f, 0f);

        // ------------------------------------------------------------------ type hooks

        protected abstract void LoadVariables();
        protected virtual void LoadInstanceVariables() { }
        protected virtual void OnSetupAfterLoad() { }
        protected virtual void OnAfterWorldLoad() { }
        protected virtual void UpdateEnemySpecific(float timeStep) { }
        protected abstract bool PlayerIsDetected();
        protected abstract float GetDamageMul(float amount, int strength);
        protected virtual void OnDamage(float amount, int strength) { }
        protected virtual void OnSetActiveEnemySpecific(bool active) { }
        protected virtual void OnResetProperties() { }
        protected virtual void OnDisableTriggers() { }

        /// <summary>iLuxEnemy::InRangeOfFood: no type but the water lurker is drawn by food (cLuxProp_Object food attraction).</summary>
        public virtual bool InRangeOfFood(HplPhysicsBody foodBody) => false;

        /// <summary>
        /// The type's state machine (kLuxBeginStateMachine ... in the original): true when the state (or the global
        /// part, state -1) handles the event.
        /// </summary>
        protected abstract bool StateEventImplement(int state, EnemyStateEvent ev, EnemyStateMessage message);

        // ------------------------------------------------------------------ properties

        public string GetMusic(EnemyMusic type) => _music[(int)type];
        public int GetMusicPrio(EnemyMusic type) => _musicPrio[(int)type];
        public string CurrentStateName => StateNames[(int)CurrentState];
        public string CurrentAnimationName => CurrentAnimation != null ? CurrentAnimation.Name : "";
        public float Speed => Mover != null ? Mover.GetMoveSpeed() : 0f;
        public IReadOnlyList<EnemyPatrolNode> Patrol => PatrolNodes;
        public int CurrentPatrolNodeIndex => CurrentPatrolNode;
        public float ActivationRange => ActivationDistance;
        public int MessageCount => _messages.Count;

        public override int BodyCount => 1;
        readonly Collider[] _bodyShape = new Collider[1];
        MeshCollider _queryShape;

        /// <summary>
        /// The character body for entity collision tests (GetEntitiesCollide, collide callbacks). HPL2 tests the body's
        /// shape whether the enemy is active or not (a script checks it in OnAutoDisabled), so a copy of the shape that
        /// stays usable when the enemy is off is used: a trigger on the no-collision layer, placed at the body.
        /// </summary>
        public override IReadOnlyList<Collider> GetBodyColliders(int index)
        {
            if (Body == null || Body.Shapes.Count == 0)
            {
                _bodyShape[0] = null;
                return _bodyShape;
            }
            if (_queryShape == null)
            {
                var go = new GameObject(Name + " (query shape)") { hideFlags = HideFlags.HideAndDontSave, layer = HplPhysicsWorld.NoCollideLayer };
                _queryShape = go.AddComponent<MeshCollider>();
                _queryShape.sharedMesh = Body.Shapes[Body.ActiveSize].sharedMesh;
                _queryShape.convex = true;
                _queryShape.isTrigger = true;
            }
            _queryShape.transform.SetPositionAndRotation(Body.Position, Quaternion.identity);
            _bodyShape[0] = _queryShape;
            return _bodyShape;
        }

        /// <summary>The query shape goes with the map.</summary>
        internal void DestroyQueryShape()
        {
            if (_queryShape == null) return;
            if (Application.isPlaying) Object.Destroy(_queryShape.gameObject);
            else Object.DestroyImmediate(_queryShape.gameObject);
        }

        /// <summary>
        /// iLuxEnemy::SaveToSaveData beyond the plain fields (saved with every entity): the body, the patrol nodes by id,
        /// the sound state, the current animation, the mover and the path.
        /// </summary>
        public Save.EnemySave CaptureSave()
        {
            var save = new Save.EnemySave
            {
                feet = Body != null ? Body.FeetPosition : GameObject.transform.position,
                yaw = _hplYaw,
                soundState = (int)SoundState,
                mover = Save.HplFieldBag.Capture(Mover, typeof(EnemyMover)),
            };
            foreach (var p in PatrolNodes)
                save.patrol.Add(new Save.SavedPatrolNode { nodeId = p.node.Id, waitTime = p.waitTime, animation = p.animation, loopAnimation = p.loopAnimation });
            if (CurrentAnimation != null)
            {
                save.animation = CurrentAnimation.Name;
                save.animationTime = CurrentAnimation.TimePosition;
                save.animationLoop = CurrentAnimation.Loop;
            }
            Pathfinder?.Save(save);
            return save;
        }

        /// <summary>iLuxEnemy::LoadFromSaveData + SetupSaveData, after the plain fields: the state is set, not entered.</summary>
        public void RestoreSave(Save.EnemySave save)
        {
            if (Body != null)
            {
                Body.StopMovement();
                Body.SetFeetPosition(save.feet);
            }
            SetHplYaw(save.yaw);
            _entityYPositions.Clear();
            ChangeSoundState((EnemySoundState)save.soundState);
            Save.HplFieldBag.Restore(Mover, typeof(EnemyMover), save.mover);

            PatrolNodes.Clear();
            var container = Pathfinder?.NodeContainer;
            if (container != null)
                foreach (var p in save.patrol)
                {
                    var node = container.GetNodeFromId(p.nodeId);
                    if (node == null)
                    {
                        HplLog.Error($"Could not find patrol node id: {p.nodeId} for '{Name}'!");
                        continue;
                    }
                    PatrolNodes.Add(new EnemyPatrolNode { node = node, waitTime = p.waitTime, animation = p.animation, loopAnimation = p.loopAnimation });
                }

            CurrentAnimation = null;
            var anim = Animation != null && save.animation != "" ? Animation.Get(save.animation) : null;
            if (anim != null)
            {
                anim.SetActive(true);
                anim.Weight = 1f;
                anim.Loop = save.animationLoop;
                anim.SetTimePosition(save.animationTime);
                CurrentAnimation = anim;
            }
            Pathfinder?.Load(save);
        }

        public override Transform AttachTransform => Model != null ? Model.transform : GameObject.transform;

        /// <summary>The HPL2 yaw of the body.</summary>
        public float HplYaw => _hplYaw;

        /// <summary>cCharacterBody::SetYaw in HPL2 terms: the Unity body faces the mirrored HPL2 forward.</summary>
        internal void SetHplYaw(float yaw)
        {
            _hplYaw = yaw;
            Vector3 f = HplSpace.Direction(HplMatrix.Mul3x3(HplMatrix.RotateY(yaw), new Vector3(0f, 0f, -1f)));
            Body.Yaw = Mathf.Atan2(f.x, f.z);
        }

        internal void AddHplYaw(float add) => SetHplYaw(_hplYaw + add);

        /// <summary>The bounding volume radius of the current body (half the diagonal of its box).</summary>
        public float BodyRadius => Body.Size.magnitude * 0.5f;

        // ------------------------------------------------------------------ update

        /// <summary>iLuxEnemy::OnUpdate: helpers, senses, state machine, type update, body speeds, animation, sounds...</summary>
        protected override void OnUpdate(float timeStep)
        {
            if (Disabled || Dev.HplDebugSession.EnemiesPaused) return;
            // Quick updates (saving, ...) are skipped.
            if (timeStep < Time.fixedDeltaTime * 0.8f) return;

            Pathfinder.OnUpdate(timeStep);
            Mover.OnUpdate(timeStep);

            if (Health > 0f)
            {
                UpdateCanSeePlayer(timeStep);
                UpdatePlayerDetected();
                UpdatePlayerInRange();
                UpdateCheckStuckAtDoor(timeStep);
                UpdateCheckLastPlayerPos(timeStep);
            }
            else
            {
                CanSeePlayerFlag = false;
                PlayerDetected = false;
                PlayerInRangeFlag = false;
            }

            UpdateStateMachine(timeStep);
            UpdateEnemySpecific(timeStep);
            UpdateCharBody(timeStep);
            UpdateAnimation(timeStep);
            UpdateSoundState(timeStep);
            UpdateDarknessGlow(timeStep);
            UpdateRegenHealth(timeStep);
            UpdateHallucination();
            UpdateAlignEntityWithGroundRay(timeStep);
        }

        /// <summary>The world update after the logic: the character body moves (iPhysicsWorld::Update).</summary>
        internal void StepBody(float timeStep)
        {
            if (!Active || Body == null || Dev.HplDebugSession.EnemiesPaused) return;
            Body.Step(timeStep);
            UpdateEntity();
        }

        /// <summary>cWorld::UpdateEntities: the mesh entity's animations and their events.</summary>
        internal void StepMesh(float timeStep)
        {
            if (!Active || Animation == null) return;
            Animation.Update(timeStep, OnAnimationEvent);
        }

        void OnAnimationEvent(HplAnimationState state, HplEnemyAssets.AnimationEvent e)
        {
            // cMeshEntity::HandleAnimationEvent: only PlaySound does something.
            if (string.IsNullOrEmpty(e.value) || e.type != "PlaySound" || Map.sound == null) return;
            var sound = Map.sound.CreateSoundEntity("AnimEvent", e.value, true);
            if (sound == null) return;
            sound.IsSaved = false;
            sound.Parent = Animation.SoundNode;
        }

        /// <summary>
        /// iCharacterBody::UpdateEntity: the mesh at the body (xz), the average y of the last 20 steps, the body yaw,
        /// then the mesh offset (Body_OffsetRot / Body_OffsetTrans and the ground ray offset).
        /// </summary>
        void UpdateEntity()
        {
            if (Model == null) return;
            Vector3 hplPos = HplSpace.Position(Body.Position);
            _entityYPositions.Add(hplPos.y);
            if (_entityYPositions.Count > EntitySmoothYPosNum) _entityYPositions.RemoveAt(0);
            float smoothY = 0f;
            foreach (float y in _entityYPositions) smoothY += y;
            smoothY /= _entityYPositions.Count;

            Matrix4x4 entity = HplMatrix.SetTranslation(HplMatrix.RotateY(_hplYaw), new Vector3(hplPos.x, smoothY, hplPos.z));
            Matrix4x4 offset = _charMeshOffsetHpl;
            offset = HplMatrix.SetTranslation(offset, HplMatrix.GetTranslation(offset) + new Vector3(0f, -_groundOffset, 0f));
            Matrix4x4 world = HplSpace.Mirror(entity * offset) * _modelLocal;
            Model.transform.SetPositionAndRotation(world.GetPosition(), world.rotation);
        }

        // ------------------------------------------------------------------ actions

        /// <summary>iLuxEnemy::GiveDamage (the missing braces of the original: the toughness test runs only when alive).</summary>
        public override void GiveDamage(float amount, int strength)
        {
            if (Health > 0f)
            {
                if (strength < Toughness - 1) amount = 0f;
                else if (strength == Toughness - 1) amount *= 0.5f;
            }
            amount *= GetDamageMul(amount, strength);
            Health -= amount;
            if (Map.logDebugMessages) Debug.Log($"[Script debug] Enemy damage {amount}");

            if (Health <= 0f)
            {
                ChangeSoundState(EnemySoundState.Silent);
                ChangeState(EnemyState.Dead);
                RunCallbackFunc("OnDeath");
            }
            else
            {
                SendMessage(EnemyMessage.TakeHit, 0f, false, Vector3.zero, amount, strength);
                OnDamage(amount, strength);
            }
        }

        public void ShowPlayerPosition() => LastKnownPlayerPos = Map.player.Body.FeetPosition;

        public void AlertOfPlayerPresence()
        {
            if (Dev.HplDebugSession.EnemiesPaused) return;
            if (TriggersDisabled()) return;
            ShowPlayerPosition();
            if (CurrentState == EnemyState.Idle || CurrentState == EnemyState.Patrol) ChangeState(EnemyState.Search);
        }

        public void ChangeState(EnemyState state)
        {
            if (state == EnemyState.LastEnum) return;
            if (CurrentState == state) return;
            NextState = state;
        }

        /// <summary>iLuxEnemy::SendMessage: with disabled triggers only the path end and range messages get through.</summary>
        public void SendMessage(EnemyMessage type, float time = 0f, bool localScope = false, Vector3 vector = default, float x = 0f, int ix = 0)
        {
            if (Disabled || Dev.HplDebugSession.EnemiesPaused) return;
            if (TriggersDisabled() && type > EnemyMessage.EndOfPath && type != EnemyMessage.PlayerInRange && type != EnemyMessage.PlayerOutOfRange)
                return;
            _messages.Add(new EnemyStateMessage
            {
                count = time,
                time = time,
                state = localScope ? CurrentState : EnemyState.LastEnum,
                type = type,
                customVector = vector,
                customFloat = x,
                customInt = ix,
            });
        }

        /// <summary>iLuxEnemy::PlayAnim.</summary>
        public void PlayAnim(string name, bool loop, float fadeTime, bool dependsOnSpeed = false, float speedMul = 1f,
                             bool syncWithPrevFrame = false, bool overrideMoveState = true, bool useMoveAnimWhenCurrentIsOver = true)
        {
            if (Animation == null) return;
            if (CurrentAnimation != null && CurrentAnimation.Name == name && CurrentAnimation.IsActive && !CurrentAnimation.IsOver) return;

            var anim = Animation.Get(name);
            if (anim == null)
            {
                HplLog.Warn($"Animation '{name}' does not exist!");
                return;
            }
            anim.SetActive(true);
            if (CurrentAnimation != null && CurrentAnimation != anim)
            {
                CurrentAnimation.FadeOut(fadeTime);
                if (!anim.IsFading) anim.Weight = 0f;
                anim.FadeIn(fadeTime);
            }
            else anim.Weight = 1f;
            anim.Loop = loop;

            if (syncWithPrevFrame && CurrentAnimation != null) anim.RelativeTimePosition = CurrentAnimation.RelativeTimePosition;
            else anim.SetTimePosition(0f);

            CurrentAnimation = anim;
            _animationIsSpeedDependant = dependsOnSpeed;
            _animationSpeedMul = speedMul;
            _useMoveAnimWhenCurrentIsOver = useMoveAnimWhenCurrentIsOver;
            Mover.OverrideMoveState = overrideMoveState;
        }

        public void FadeOutCurrentAnim(float fadeTime)
        {
            if (CurrentAnimation == null || CurrentAnimation.Loop) return;
            CurrentAnimation.FadeOut(fadeTime);
            CurrentAnimation = null;
            if (_useMoveAnimWhenCurrentIsOver) Mover.UseMoveStateAnimations();
        }

        /// <summary>iLuxEnemy::PlaySound: a sound entity that follows the mesh.</summary>
        public Sound.HplSoundEntity PlaySound(string name)
        {
            if (string.IsNullOrEmpty(name) || Map.sound == null) return null;
            var sound = Map.sound.CreateSoundEntity("EnemySound", name, true);
            if (sound != null) sound.Parent = AttachTransform;
            return sound;
        }

        public void SetPositionAtStartPos() => Body.SetFeetPosition(StartPosition);

        /// <summary>iLuxEnemy::ResetProperties (check point load): at the start, idle, senses and counters cleared.</summary>
        public void ResetProperties()
        {
            SetPositionAtStartPos();
            ChangeState(EnemyState.Idle);
            CausesSanityDecrease = CausesSanityDecreaseAsDefault;
            Disabled = false;
            _lookForPlayerCount = 0f;
            CanSeePlayerFlag = false;
            _playerInLosCount = 0;
            PlayerDetected = false;
            PlayerInRangeFlag = false;
            _checkAtDoorCount = 0f;
            StuckAtDoor = false;
            _darknessGlowAlpha = 0f;
            _darknessGlowUpdateCount = 0f;
            FovMul = 1f;
            DamageCount = 0f;
            LastKnownPlayerPos = Vector3.zero;
            _lastPlayerPosCount = 0f;
            CurrentPatrolNode = 0;
            AttackHitCounter = 0;
            IsSeenByPlayer = false;
            SetMoveSpeed(EnemyMoveSpeed.Walk);
            OnResetProperties();
        }

        /// <summary>
        /// iLuxEnemy::FadeToSmoke: smoke at the bones (smaller ones closer than 10 cm to their parent), off, the
        /// hallucination sound; a visible model dissolves in 1 s (cLuxMap::AddDissolveEntity).
        /// </summary>
        public void FadeToSmoke(bool playSound)
        {
            var skinned = Model != null ? Model.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            if (skinned != null && Map.ParticleWorld != null)
            {
                foreach (var bone in skinned.bones)
                {
                    if (bone == null) continue;
                    string ps = "ps_enemy_disappear_per_bone.ps";
                    if (bone.parent != null && bone.localPosition.magnitude < 0.1f) ps = "ps_enemy_disappear_per_bone_small.ps";
                    var system = Map.ParticleWorld.Create("Disappear", ps);
                    if (system != null) system.LocalMatrix = Matrix4x4.Translate(bone.position);
                }
            }
            if (Model != null && Model.activeInHierarchy) Map.AddDissolveEntity(Model.transform, 1f);
            SetActive(false);
            if (playSound) Map.sound?.PlayGuiSoundData("enemy_hallucination_disappear", Sound.SoundEntryType.Gui);
        }

        public void AddPatrolNode(AINode node, float waitTime, string animation, bool loopAnimation = false) =>
            PatrolNodes.Add(new EnemyPatrolNode { node = node, waitTime = waitTime, animation = animation, loopAnimation = loopAnimation });

        public void ClearPatrolNodes()
        {
            PatrolNodes.Clear();
            CurrentPatrolNode = 0;
        }

        protected EnemyPatrolNode GetCurrentPatrolNode() =>
            CurrentPatrolNode >= PatrolNodes.Count || CurrentPatrolNode < 0 ? null : PatrolNodes[CurrentPatrolNode];

        protected bool IsAtLastPatrolNode() => CurrentPatrolNode >= PatrolNodes.Count - 1;

        protected void IncCurrentPatrolNode(bool loopIfAtEnd)
        {
            CurrentPatrolNode++;
            if (CurrentPatrolNode >= PatrolNodes.Count) CurrentPatrolNode = loopIfAtEnd ? 0 : PatrolNodes.Count - 1;
        }

        public void SetDisableTriggers(bool x)
        {
            DisableTriggers = x;
            if (DisableTriggers) OnDisableTriggers();
        }

        public void SetDisabled(bool x)
        {
            Disabled = x;
            if (Disabled) Pathfinder.Stop();
        }

        public void SetHallucination(bool x) => _hallucination = x;
        public void SetHallucinationEndDist(float x) => _hallucinationEndDist = x;
        public void SetSanityDecreaseActive(bool x) => CausesSanityDecrease = x;

        // ------------------------------------------------------------------ state machine

        bool StateEvent(int state, EnemyStateEvent ev, EnemyStateMessage message)
        {
            bool ret = StateEventImplement(state, ev, message);
            if (!ret) ret = StateEventImplement(-1, ev, message);
            return ret;
        }

        /// <summary>iLuxEnemy::UpdateStateMachine: update, then the messages whose time is up (or out of scope).</summary>
        void UpdateStateMachine(float timeStep)
        {
            CheckStateChange();
            StateEvent((int)CurrentState, EnemyStateEvent.Update, null);
            CheckStateChange();

            for (int i = 0; i < _messages.Count;)
            {
                var message = _messages[i];
                if (message.state != EnemyState.LastEnum && message.state != CurrentState)
                {
                    _messages.RemoveAt(i);
                    continue;
                }
                message.count -= timeStep;
                if (message.count <= 0f)
                {
                    StateEvent((int)CurrentState, EnemyStateEvent.Message, message);
                    // Handlers only append messages, so this one is still at i.
                    _messages.RemoveAt(i);
                    continue;
                }
                i++;
            }
            CheckStateChange();
        }

        void CheckStateChange()
        {
            if (NextState == CurrentState) return;
            if (CurrentState != EnemyState.LastEnum) StateEvent((int)CurrentState, EnemyStateEvent.Leave, null);
            PreviousState = CurrentState;
            CurrentState = NextState;
            StateEvent((int)CurrentState, EnemyStateEvent.Enter, null);
        }

        /// <summary>An undefined state of the type (FatalError in HPL2).</summary>
        protected bool UndefinedState(int state)
        {
            HplLog.PortError($"Tried calling undefined state {state}!");
            return false;
        }

        // ------------------------------------------------------------------ sounds

        protected void ChangeSoundState(EnemySoundState state)
        {
            if (SoundState == state) return;
            SoundState = state;
            if (_currentSound != null && Map.sound.SoundEntityExists(_currentSound))
            {
                _currentSound.FadeOut(3f);
                _currentSound = null;
            }
            _ambientSoundCount = HplMath.RandRectf(_ambientMinTime[(int)SoundState], _ambientMaxTime[(int)SoundState]);
        }

        void UpdateSoundState(float timeStep)
        {
            if (SoundState == EnemySoundState.Silent) return;
            if (_ambientSound[(int)SoundState] == "") return;
            if (_currentSound != null && Map.sound.SoundEntityExists(_currentSound)) return;
            _currentSound = null;

            if (_ambientSoundCount <= 0f)
            {
                _currentSound = PlaySound(_ambientSound[(int)SoundState]);
                _ambientSoundCount = HplMath.RandRectf(_ambientMinTime[(int)SoundState], _ambientMaxTime[(int)SoundState]);
            }
            else _ambientSoundCount -= timeStep;
        }

        // ------------------------------------------------------------------ animation, body

        void UpdateAnimation(float timeStep)
        {
            if (Animation == null) return;
            if (!Mover.OverrideMoveState || CurrentAnimation == null) return;

            if (CurrentAnimation.PreviousTimePosition <= CurrentAnimation.SpecialEventTime &&
                CurrentAnimation.TimePosition > CurrentAnimation.SpecialEventTime)
                SendMessage(EnemyMessage.AnimationSpecialEvent);

            if (CurrentAnimation.IsOver)
            {
                if (_useMoveAnimWhenCurrentIsOver) Mover.UseMoveStateAnimations();
                SendMessage(EnemyMessage.AnimationOver);
            }

            // UseMoveStateAnimations does not clear the animation, so it may be used here still.
            if (_animationIsSpeedDependant && CurrentAnimation != null)
            {
                float speed = Body.GetVelocity(timeStep).magnitude;
                if (Body.GetMoveSpeed(CharDir.Forward) < 0f) speed = -speed;
                CurrentAnimation.Speed = Mathf.Abs(speed) * MoveSpeedAnimMul;
            }
        }

        void UpdateCharBody(float timeStep)
        {
            float mul = Mover.CalculateSpeedMul(timeStep);
            Body.SetMaxPositiveMoveSpeed(CharDir.Forward, ForwardSpeed * mul);
            Body.SetMaxNegativeMoveSpeed(CharDir.Forward, BackwardSpeed * mul);
            Body.SetMoveAcc(CharDir.Forward, ForwardAcc);
            Body.SetMoveDeacc(CharDir.Forward, ForwardDeacc);
        }

        protected void SetMoveSpeed(EnemyMoveSpeed type)
        {
            int pose = CurrentPose == 0 ? (int)type : -1;
            // Quadruped speeds are 0 for the loader's types.
            ForwardSpeed = pose < 0 ? 0f : _defaultForwardSpeed[pose];
            BackwardSpeed = pose < 0 ? 0f : _defaultBackwardSpeed[pose];
            ForwardAcc = pose < 0 ? 0f : _defaultForwardAcc[pose];
            ForwardDeacc = pose < 0 ? 0f : _defaultForwardDeacc[pose];
        }

        // ------------------------------------------------------------------ senses

        AmnesiaPlayer Player => Map.player;

        bool PlayerCrouching => Player.MoveState != null && Player.MoveState.Crouching;

        /// <summary>
        /// iLuxEnemy::UpdateCanSeePlayer every 0.3 s: the sight range (darkness range if the player is still in the dark
        /// without a lantern; half when crouching without lantern), then 2 of 9 rays (FOV tested beyond touching range);
        /// seen after 3 checks in a row. The original multiplies the sight range itself by 1.5 when the lantern is on
        /// (the range used here is taken before that), so it grows with every check.
        /// </summary>
        void UpdateCanSeePlayer(float timeStep)
        {
            var player = Player;
            if (player == null || player.IsDead) return;
            if (TriggersDisabled()) CanSeePlayerFlag = false;

            _lookForPlayerCount -= timeStep;
            if (_lookForPlayerCount > 0f) return;
            _lookForPlayerCount = 0.3f;

            var playerBody = player.Body;
            float dist = Vector3.Distance(playerBody.Position, Body.Position);
            bool lanternOn = player.Lantern != null && player.Lantern.Active;
            bool crouching = PlayerCrouching;

            float maxRange = SightRange;
            if (lanternOn) SightRange *= 1.5f;
            if (!lanternOn && player.LightLevel.NormalLightLevel < PlayerInDarknessLightLevel && player.AvgSpeed < 0.05f && !SkipVisibilityRangeHandicaps)
                maxRange = DarknessSightRange;
            if (crouching && !lanternOn && !SkipVisibilityRangeHandicaps) maxRange *= CrouchVisibleRangeMul;

            if (maxRange < dist)
            {
                if (!CanSeePlayerFlag) SendMessage(EnemyMessage.PlayerUnseen);
                CanSeePlayerFlag = false;
                _playerInLosCount = 0;
                return;
            }

            float minDist = BodyRadius + playerBody.Size.magnitude * 0.5f + 0.05f;
            bool useFov = dist >= minDist;
            if (LineOfSight(playerBody.Position, playerBody.Size, useFov))
            {
                if (_playerInLosCount >= 2)
                {
                    if (!CanSeePlayerFlag) SendMessage(EnemyMessage.PlayerSeen);
                    CanSeePlayerFlag = true;
                }
                else _playerInLosCount++;
            }
            else if (_playerInLosCount > 0)
            {
                _playerInLosCount = 0;
                if (!CanSeePlayerFlag) SendMessage(EnemyMessage.PlayerUnseen);
                CanSeePlayerFlag = false;
            }
        }

        protected bool SkipVisibilityRangeHandicaps;

        void UpdatePlayerDetected()
        {
            if (Player == null || Player.IsDead) return;
            bool detected = PlayerIsDetected();
            if (detected && !PlayerDetected) SendMessage(EnemyMessage.PlayerDetected);
            else if (!detected && PlayerDetected) SendMessage(EnemyMessage.PlayerUndetected);
            PlayerDetected = detected;
        }

        void UpdatePlayerInRange()
        {
            if (Player == null || Player.IsDead) return;
            float distance = DistToPlayer();
            if (distance <= ActivationDistance && !PlayerInRangeFlag)
            {
                PlayerInRangeFlag = true;
                SendMessage(EnemyMessage.PlayerInRange);
            }
            else if (distance > ActivationDistance * 1.25f + 2f && PlayerInRangeFlag)
            {
                PlayerInRangeFlag = false;
                SendMessage(EnemyMessage.PlayerOutOfRange);
            }
        }

        /// <summary>
        /// iLuxEnemy::UpdateCheckStuckAtDoor: when the mover is stuck, every 0.1 s, a swing door body (with mass) whose
        /// bounding box touches the body's and that lies ahead (toward the next path goal) makes the enemy stuck at it.
        /// </summary>
        void UpdateCheckStuckAtDoor(float timeStep)
        {
            if (Mover.StuckCounter < 0.1f)
            {
                StuckAtDoor = false;
                return;
            }
            _checkAtDoorCount -= timeStep;
            if (_checkAtDoorCount > 0f) return;
            _checkAtDoorCount = 0.1f;

            bool atDoor = false;
            StuckDoor = null;
            PropSwingDoor door = null;
            var bv = new Bounds(Body.Position, Body.Size);
            Vector3 dir = Pathfinder.IsMoving ? (Pathfinder.GetNextGoalPos() - Body.Position).normalized : Body.Forward;

            foreach (var entity in Map.Entities)
            {
                if (entity is not PropSwingDoor prop || prop.DestroyMe) continue;
                foreach (var body in prop.PhysicsBodies)
                {
                    if (body.Mass == 0f) continue;
                    if (!body.TryGetBounds(out Bounds bodyBounds) || !bv.Intersects(bodyBounds)) continue;
                    Vector3 toDoor = (body.Position - Body.Position).normalized;
                    if (Vector3.Dot(dir, toDoor) > 0.2f)
                    {
                        StuckDoor = prop;
                        door = prop;
                        atDoor = true;
                        break;
                    }
                }
                if (atDoor) break;
            }

            if (atDoor && !StuckAtDoor)
            {
                StuckAtDoor = true;
                Mover.ResetStuckCounter();
                var main = door.MainBody;
                SendMessage(EnemyMessage.StuckAtDoor, 0f, false, main != null ? main.Position : door.AttachTransform.position);
            }
            else if (!atDoor && StuckAtDoor) StuckAtDoor = false;
        }

        void UpdateCheckLastPlayerPos(float timeStep)
        {
            if (CanSeePlayer)
            {
                _lastPlayerPosCount += timeStep;
                if (_lastPlayerPosCount > 1f) _lastPlayerPosCount = 1f;
            }
            else
            {
                _lastPlayerPosCount -= timeStep;
                if (_lastPlayerPosCount < 0f) _lastPlayerPosCount = 0f;
            }
            if (_lastPlayerPosCount > 0f) LastKnownPlayerPos = Player.Body.FeetPosition;
        }

        /// <summary>
        /// iLuxEnemy::UpdateDarknessGlow: the glow fades toward 1 − light level (twice the average of the body center
        /// and the feet, clamped). The update counter of the original never skips (it is reset every step).
        /// Not ported: drawing the glow (cLuxEffectRenderer::AddEnemyGlow) - the alpha is kept for it.
        /// </summary>
        void UpdateDarknessGlow(float timeStep)
        {
            if (_darknessGlowAlpha != _darknessGlowAlphaGoal)
            {
                if (_darknessGlowAlpha > _darknessGlowAlphaGoal) _darknessGlowAlpha = Mathf.Max(_darknessGlowAlphaGoal, _darknessGlowAlpha - timeStep);
                else _darknessGlowAlpha = Mathf.Min(_darknessGlowAlphaGoal, _darknessGlowAlpha + timeStep);
            }
            if (Model == null || !Model.activeInHierarchy) return;
            if (_darknessGlowUpdateCount > 0f) _darknessGlowUpdateCount -= timeStep;
            _darknessGlowUpdateCount = 0.3f;

            float goal = LightLevelAtPos(Body.Position);
            goal += LightLevelAtPos(Body.FeetPosition + new Vector3(0f, 0.1f, 0f));
            goal /= 2f;
            goal *= 2f;
            if (goal > 1f) goal = 1f;
            _darknessGlowAlphaGoal = 1f - goal;
        }

        /// <summary>cLuxMapHelper::GetLightLevelAtPos: the summed light levels of the map's lights (the player's darkness light skipped).</summary>
        float LightLevelAtPos(Vector3 position)
        {
            var scene = Map.gameObject.scene;
            float level = 0f;
            foreach (var light in AmnesiaLight.All)
            {
                if (light.excludeFromLightLevel || light.gameObject.scene != scene) continue;
                level += light.LightLevelAt(position, 0f);
            }
            return level;
        }

        void UpdateRegenHealth(float timeStep)
        {
            if (Health <= 0f) return;
            if (Health < MaxRegenHealth && RegenHealthSpeed > 0f)
                Health = Mathf.Min(MaxRegenHealth, Health + timeStep * RegenHealthSpeed);
        }

        void UpdateHallucination()
        {
            if (!_hallucination || Health <= 0f) return;
            if ((Body.Position - Player.Body.Position).sqrMagnitude < _hallucinationEndDist * _hallucinationEndDist) FadeToSmoke(true);
        }

        /// <summary>
        /// iLuxEnemy::UpdateAlignEntityWithGroundRay every 0.02 s: a ray down from the front of the feet (half the body
        /// width ahead, 5 cm up, 0.5 m long) against character colliders; the mesh is lowered by the average of the last 3.
        /// </summary>
        void UpdateAlignEntityWithGroundRay(float timeStep)
        {
            if (!AlignEntityWithGroundRay) return;
            _checkGroundRayCount -= timeStep;
            if (_checkGroundRayCount > 0f) return;
            _checkGroundRayCount = 0.02f;

            const float startAdd = 0.05f;
            Vector3 start = Body.FeetPosition + new Vector3(0f, startAdd, 0f) + Body.Forward * (Body.Size.x * 0.5f);
            if (!HplPhysics.Raycast(start, start + Vector3.down * 0.5f, out float dist, out _, out _)) return;
            dist -= startAdd;

            _groundCheckDists.Add(dist);
            if (_groundCheckDists.Count > MaxGroundCheckDists) _groundCheckDists.RemoveAt(0);
            float final = 0f;
            foreach (float d in _groundCheckDists) final += d;
            _groundOffset = final / _groundCheckDists.Count;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>iLuxEnemy::TriggersDisabled: by script or during a flashback.</summary>
        protected bool TriggersDisabled() => DisableTriggers || (Player != null && Player.Flashback != null && Player.Flashback.Active);

        /// <summary>
        /// iLuxEnemy::Attack (cLuxMapHelper::ShapeDamage with this enemy's attack box): every body the box touches (other
        /// enemies skipped) with no non-character body between the enemy and it gets a push, a hit effect and damage; a
        /// hit counts and plays the hit sound. True when the player was hit.
        /// </summary>
        protected bool Attack(EnemyAttackSize size, EnemyAttackDamage damage, float damageMul = 1f)
        {
            bool hitPlayer = false;
            Vector3 center = Body.Position + Body.Right * size.offset.x + Vector3.up * size.offset.y + Body.Forward * size.offset.z;
            Quaternion rotation = Quaternion.Euler(0f, Body.Yaw * Mathf.Rad2Deg, 0f);
            bool hit = HplEnemyAttack.ShapeDamage(Map, this, center, rotation, size.size * 0.5f, Body.Position,
                damage.minDamage * damageMul, damage.maxDamage * damageMul, damage.force, damage.maxImpulse, damage.strength,
                damage.hitSpeed, damage.damageType, damage.checkPlayer, damage.checkProps, ref hitPlayer);
            if (hit)
            {
                AttackHitCounter++;
                PlaySound(damage.hitSound);
            }
            return hitPlayer;
        }

        public string GetHitSound(int weaponHitType) => _hitSound[weaponHitType] ?? "";
        public string GetHitPS(int weaponHitType) => _hitPS[weaponHitType] ?? "";

        /// <summary>iLuxEnemy::GetSearchForPlayerNode (unused by the grunt, kept for the other types).</summary>
        protected AINode GetSearchForPlayerNode(int maxIterations = 5, float maxAngleMul = 1f, float minDistMul = 1f, float maxDistMul = 1f)
        {
            float maxAngle = PlayerSearchMaxAngle * maxAngleMul;
            for (int i = 0; i < maxIterations; i++)
            {
                float angle = HplMath.RandRectf(-maxAngle, maxAngle);
                Vector3 toPlayer = HplSpace.Position(Player.Body.FeetPosition - Body.FeetPosition);
                float toPlayerDist = toPlayer.magnitude;
                toPlayer.Normalize();
                Vector3 dir = HplSpace.Direction(HplMatrix.Mul(HplMatrix.RotateY(angle), toPlayer));
                float length = HplMath.RandRectf(PlayerSearchMinDistMul * minDistMul, PlayerSearchMaxDistMul * maxDistMul) * toPlayerDist;
                var node = Pathfinder.GetNodeAtPos(Body.FeetPosition + dir * length, 0f, length, true, false, true, _lastSearchNode);
                if (node != null)
                {
                    _lastSearchNode = node;
                    return node;
                }
            }
            return Pathfinder.GetNodeAtPos(Body.FeetPosition, 0f, 13f, false, false, true, _lastSearchNode);
        }

        static readonly Vector2[] LosPosAdds =
        {
            new(0f, 0f), new(0.75f, 0f), new(-0.75f, 0f), new(0f, 1f), new(0f, -1f), new(0.5f, 0.5f), new(-0.5f, 0.5f),
            new(0.25f, 0f), new(-0.25f, 0f),
        };

        protected bool LineOfSight(Vector3 pos, Vector3 size, bool checkFov) =>
            LineOfSight(pos, size, checkFov, Body.Position + new Vector3(0f, Body.Size.y / 2f - 0.1f, 0f));

        /// <summary>iLuxEnemy::LineOfSight: (FOV), then 2 free rays of 9 over 40% of the width / height.</summary>
        protected bool LineOfSight(Vector3 pos, Vector3 size, bool checkFov, Vector3 sourcePos)
        {
            Vector3 forward = (pos - sourcePos).normalized;
            // The right vector in HPL2 space, mirrored: the side rays are the same.
            Vector3 right = HplSpace.Direction(Vector3.Cross(HplSpace.Direction(forward), Vector3.up));
            if (checkFov && !InFov(pos)) return false;
            float halfWidth = size.x * 0.4f, halfHeight = size.y * 0.4f;
            int count = 0;
            for (int i = 0; i < LosPosAdds.Length; i++)
            {
                Vector3 add = right * (LosPosAdds[i].x * halfWidth) + Vector3.up * (LosPosAdds[i].y * halfHeight);
                if (HplCollision.CheckLineOfSight(sourcePos + add, pos + add)) count++;
                if (count == 2) return true;
            }
            return false;
        }

        /// <summary>iLuxEnemy::InFOV: heading and pitch differences from the eye (20 cm below the top), in HPL2 terms.</summary>
        protected bool InFov(Vector3 pos)
        {
            float fov = Fov * FovMul;
            if (fov >= Mathf.PI * 2f) return true;
            Vector3 eye = Body.Position + new Vector3(0f, Body.Size.y / 2f - 0.2f, 0f);
            Vector3 dirToPos = HplSpace.Direction((pos - eye).normalized);
            Vector3 enemyForward = HplSpace.Direction(Body.Forward);
            Vector3 toPlayerAngle = HplMatrix.GetAngleFromPoints3D(Vector3.zero, dirToPos);
            Vector3 enemyAngle = HplMatrix.GetAngleFromPoints3D(Vector3.zero, enemyForward);
            float angleX = Mathf.Abs(HplMatrix.AngleDistanceRad(toPlayerAngle.x, enemyAngle.x));
            float angleY = Mathf.Abs(HplMatrix.AngleDistanceRad(toPlayerAngle.y, enemyAngle.y));
            if (angleY > fov * 0.5f) return false;
            if (angleX > fov * FovXMul * 0.5f) return false;
            return true;
        }

        protected bool PlayerInFov() => InFov(Player.Body.Position);

        protected Vector3 GetPlayerFeetPos() => Player.Body.FeetPosition;

        /// <summary>iLuxEnemy::Dist2D: from the feet, in xz.</summary>
        protected float Dist2D(Vector3 pos)
        {
            Vector3 a = Body.FeetPosition;
            a.y = pos.y = 0f;
            return Vector3.Distance(a, pos);
        }

        /// <summary>iLuxEnemy::AbsHeightDistToPlayer: the height difference of the feet (100000 when the player is dead).</summary>
        protected float AbsHeightDistToPlayer() => Player == null || Player.IsDead ? 100000f : Mathf.Abs(Body.FeetPosition.y - Player.Body.FeetPosition.y);

        /// <summary>
        /// iLuxEnemy::GetClosestFood: the food object (IsFood) closest to the body center within the distance whose first
        /// body's lowest point is within the height of the feet.
        /// </summary>
        protected PropObject GetClosestFood(float maxDist, float maxHeightDist)
        {
            PropObject output = null;
            float shortest = -1f;
            foreach (var entity in Map.Entities)
            {
                if (entity is not PropObject food || !food.IsFood || food.PhysicsBodies.Count == 0) continue;
                var body = food.PhysicsBodies[0];
                if (!body.TryGetBounds(out Bounds bounds)) continue;
                if (Mathf.Abs(bounds.min.y - Body.FeetPosition.y) > maxHeightDist) continue;
                float distSqr = (Body.Position - body.Position).sqrMagnitude;
                if (distSqr > maxDist * maxDist) continue;
                if (shortest < 0f || distSqr < shortest)
                {
                    shortest = distSqr;
                    output = food;
                }
            }
            return output;
        }

        protected float DistToPlayer() => Player == null || Player.IsDead ? 100000f : Vector3.Distance(Body.FeetPosition, Player.Body.FeetPosition);

        protected float DistToPlayer2D()
        {
            if (Player == null || Player.IsDead) return 100000f;
            Vector3 a = Body.FeetPosition, b = Player.Body.FeetPosition;
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>The cos of the angle between player → enemy and the player's movement: 1 straight toward, −1 away or still.</summary>
        protected float GetPlayerMovementTowardEnemyAmount()
        {
            Vector3 dir = Player.Body.GetVelocity(Time.fixedDeltaTime);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.1f * 0.1f) return -1f;
            dir.Normalize();
            Vector3 toEnemy = Body.Position - Player.Body.Position;
            toEnemy.y = 0f;
            toEnemy.Normalize();
            return Vector3.Dot(dir, toEnemy);
        }

        protected bool OutsideStartRadius() => Vector3.Distance(Body.FeetPosition, StartPosition) > ActivationDistance * 1.2f + 2f;

        /// <summary>iLuxEnemy::OnSetActive: body and mesh follow; off: out of the enemy music and the terror, idle.</summary>
        protected override void OnSetActive(bool active)
        {
            base.OnSetActive(active);
            Body?.SetBodyActive(active);
            if (!active)
            {
                Map.sound?.Music.RemoveEnemy(EnemyMusic.Attack, this);
                Map.sound?.Music.RemoveEnemy(EnemyMusic.Search, this);
                Player?.RemoveTerrorEnemy(this);
                ChangeState(EnemyState.Idle);
            }
            OnSetActiveEnemySpecific(active);
        }

        /// <summary>For the debug line of the simulation.</summary>
        public string DebugText() =>
            $"{Name}: {CurrentStateName} health {Health:0} seen {(CanSeePlayer ? 1 : 0)} detected {(PlayerDetected ? 1 : 0)} " +
            $"range {(PlayerInRange ? 1 : 0)} door {(StuckAtDoor ? 1 : 0)} fovMul {FovMul:0.#} pos {Body.FeetPosition:F2} " +
            $"speed {Speed:0.00} stuck {Mover.StuckCounter:0.00} anim {CurrentAnimation?.Name ?? "-"} " +
            $"patrol {CurrentPatrolNode}/{PatrolNodes.Count} moving {(Pathfinder.IsMoving ? 1 : 0)} " +
            $"path {Pathfinder.PathNodeCount} dist {DistToPlayer():0.0} fov {(Player != null && InFov(Player.Body.Position) ? 1 : 0)} " +
            $"los {(Player != null && LineOfSight(Player.Body.Position, Player.Body.Size, false) ? 1 : 0)} yaw {HplYaw * Mathf.Rad2Deg:0} " +
            $"fwd {Body.Forward:F2} glow {DarknessGlowAlpha:0.00}";
    }
}
