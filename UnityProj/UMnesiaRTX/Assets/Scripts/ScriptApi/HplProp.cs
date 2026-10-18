using System.Collections.Generic;
using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    /// <summary>cLuxInteractData_Grab (LuxProp.h): how a grabbed prop follows the camera.</summary>
    public sealed class InteractDataGrab
    {
        public bool grabUseDepth;
        public float grabDepth, grabDepthInc, grabMinDepth, grabMaxDepth;
        public bool grabUseOffset;
        public Vector3 grabPositionOffset, grabRotationOffset; // HPL2 space, radians
        public float grabThrowImpulse, grabMassMul, forceMul, torqueMul;
        public bool useRotation;
    }

    public sealed class InteractDataPush
    {
        public bool pushAtPoint;
        public float pushForceMul, pushImpulse;
    }

    public sealed class InteractDataSlide
    {
        public float slideMaxSpeed, slideSlowDownFactor, slideSpeedFactor, slideThrowImpulse;
    }

    /// <summary>iLuxInteractData_RotateBase: swing doors, levers, wheels.</summary>
    public sealed class InteractDataRotate
    {
        public float moveMaxSpeed, moveSlowDownFactor, moveSpeedFactor, moveThrowImpulse;
    }

    /// <summary>
    /// A prop, enemy or commentary icon from an .ent file: port of iLuxProp (LuxProp.cpp) and its loader
    /// (iLuxPropLoader::AfterLoad). Type behavior is in the subclasses (RTXMnesia.Props); enemies and commentary
    /// icons use this base until step 6.
    ///
    /// Covers: base variables, bodies (Rigidbodies from <see cref="HplPhysicsWorld"/>), main body, health and
    /// toughness, effects on / off (lights, billboards, particle systems, mesh illumination, fading), static
    /// movement (MoveLinearTo / MoveAngularTo / RotateAtSpeed, computed in HPL2 space), "no collision with the
    /// player until outside it", static physics, reset, sounds of effects and static movement.
    /// </summary>
    public class HplProp : HplEntity
    {
        public readonly AmnesiaEntity Component;
        readonly HplEntityType _type;
        public override HplEntityType Type => _type;
        /// <summary>The .ent EntityType: Object, SwingDoor, Lamp, Enemy_Grunt, ...</summary>
        public string PropType => Component.entityType;
        public string SubType => Component.entitySubType;

        public readonly List<HplPhysicsBody> PhysicsBodies = new();
        public readonly List<HplJoint> Joints = new();

        public float Health = 100f;
        public int Toughness;
        public bool ShowMesh = true, ShowHints = true;

        /// <summary>iLuxProp::mbDissolveOnDestruction / mfDissolveTime: the model fades out when the prop is destroyed.</summary>
        public bool DissolveOnDestruction;
        public float DissolveTime = 1f;
        public bool IsInteractedWith;
        public readonly List<string> ConnectedProps = new();
        public bool StaticPhysics { get; private set; }
        /// <summary>Destroyed (break, pick up): the map removes it after the step.</summary>
        public bool DestroyMe { get; internal set; }

        protected HplPhysicsWorld PhysicsWorld { get; private set; }
        protected HplConfig GameConfig { get; private set; }

        string _mainBodyName = "";
        HplPhysicsBody _mainBody;
        float[] _bodyMasses;
        bool[] _defaultCollideCharacter;
        Vector3 _loadPosition;
        Quaternion _loadRotation;

        // ------------------------------------------------------------------ construction

        public HplProp(AmnesiaEntity entity, HplEntityType type) : base(entity.gameObject, entity.hplId)
        {
            Component = entity;
            _type = type;
            foreach (var body in entity.GetComponentsInChildren<AmnesiaBody>(true))
                Bodies.Add(new HplBody(Name + "_" + body.name, this, body.transform, body, body.GetComponentsInChildren<Collider>(true)));
        }

        /// <summary>
        /// iLuxPropLoader::AfterLoad: bodies, base variables, type variables, SetupAfterLoad, instance variables.
        /// The map adds the entity and sets its active state.
        /// </summary>
        internal void Load(HplMap map, HplPhysicsWorld world)
        {
            Map = map;
            PhysicsWorld = world;
            GameConfig = map.gameConfig;
            _loadPosition = GameObject.transform.position;
            _loadRotation = GameObject.transform.rotation;

            world.AddEntity(GameObject.transform, map.surfaces, Application.isPlaying);
            foreach (var b in Bodies)
            {
                b.Physics = world.BodyOf(b.Data);
                if (b.Physics == null) continue;
                b.Physics.UserData = this;
                PhysicsBodies.Add(b.Physics);
            }
            foreach (var j in world.Joints)
                if (j.Child.UserData == this && !Joints.Contains(j)) Joints.Add(j);

            // Base properties
            Health = VarFloat("Health", 100f);
            Toughness = VarInt("Toughness", 0);
            MaxFocusDistance = VarFloat("MaxFocusDistance", 0f);
            _mainBodyName = VarString("MainPhysicsBody", "");
            ShowMesh = VarBool("ShowMesh", true);
            if (!ShowMesh) SetMeshVisible(false);
            ShowHints = VarBool("ShowHints", true);
            DissolveOnDestruction = VarBool("DissolveOnDestruction", false);
            DissolveTime = VarFloat("DissolveTime", 1f);

            // Effects properties
            _effectsOnSound = VarString("EffectsOnSound", "");
            _effectsOffSound = VarString("EffectsOffSound", "");
            _effectsOnTime = VarFloat("EffectsOnTime", 1f);
            _effectsOffTime = VarFloat("EffectsOffTime", 1f);
            _effectsOffLightColor = VarColor("EffectsOffLightColor", new Color(0f, 0f, 0f, 0f));
            _effectsOffLightRadius = VarFloat("EffectsOffLightRadius", 1f);

            // Move properties
            _moveCheckCollision = VarBool("StaticMoveCheckCollision", true);
            _moveStartSound = VarString("StaticMoveStartSound", "");
            _moveStopSound = VarString("StaticMoveStopSound", "");
            _moveLoopSound = VarString("StaticMoveLoopSound", "");

            SetupAnimation();
            LoadVariables();
            if (MaxFocusDistance <= 0f) MaxFocusDistance = ConfigFloat(DefaultFocusDistanceKey);

            SetupAfterLoad();

            // Instance variables
            if (InstanceVar("CastShadows") == "false") { } // set by the level import
            ConnectedProps.Clear();
            foreach (string s in (InstanceVar("ConnectedProps") ?? "").Split(new[] { ' ', ',', '\t' }, System.StringSplitOptions.RemoveEmptyEntries))
                ConnectedProps.Add(s);
            CallbackFunc = InstanceVar("CallbackFunc") ?? "";
            ConnectionStateChangeCallback = InstanceVar("ConnectionStateChangeCallback") ?? "";
            FullGameSave = ForceFullGameSave || InstanceBool("FullGameSave", false);
            SetPlayerLookAtCallback(InstanceVar("PlayerLookAtCallback") ?? "", InstanceBool("PlayerLookAtCallbackAutoRemove", false));
            SetPlayerInteractCallback(InstanceVar("PlayerInteractCallback") ?? "", InstanceBool("PlayerInteractCallbackAutoRemove", false));
            SetStaticPhysics(InstanceBool("StaticPhysics", false));
            LoadInstanceVariables();
        }

        /// <summary>iLuxPropLoader::mbForceFullGameSave (move objects are always saved whole).</summary>
        protected virtual bool ForceFullGameSave => false;

        /// <summary>
        /// After the map cache restored this prop's fields (a full game save): bring the scene objects in line with
        /// them (cLuxProp SetupSaveData). The base re-applies the effects and the mesh visibility.
        /// </summary>
        public virtual void AfterStateRestore() { }

        /// <summary>The game.cfg Player_Interaction key of the type's default focus distance.</summary>
        protected virtual string DefaultFocusDistanceKey => "Default_DefaultMaxFocusDist";

        /// <summary>The type loader's LoadVariables (.ent variables).</summary>
        protected virtual void LoadVariables() { }

        /// <summary>The type loader's LoadInstanceVariables (map instance variables).</summary>
        protected virtual void LoadInstanceVariables() { }

        protected virtual void OnSetupAfterLoad() { }

        void SetupAfterLoad()
        {
            _defaultCollideCharacter = new bool[PhysicsBodies.Count];
            _bodyMasses = new float[PhysicsBodies.Count];
            for (int i = 0; i < PhysicsBodies.Count; i++)
            {
                _defaultCollideCharacter[i] = PhysicsBodies[i].CollideCharacter;
                _bodyMasses[i] = PhysicsBodies[i].Mass;
            }
            if (_mainBodyName != "")
            {
                _mainBody = PhysicsBodies.Find(b => b.Name == Name + "_" + _mainBodyName);
                if (_mainBody == null) HplLog.Warn($"Could not find main physics body '{Name}'");
            }
            SetupEffectData();
            OnSetupAfterLoad();
        }

        // ------------------------------------------------------------------ variables

        protected string VarString(string name, string fallback) => HplVariable.Find(Component.variables, name) ?? fallback;
        protected float VarFloat(string name, float fallback) => ParseFloat(HplVariable.Find(Component.variables, name), fallback);
        protected int VarInt(string name, int fallback) => ParseInt(HplVariable.Find(Component.variables, name), fallback);
        protected bool VarBool(string name, bool fallback) => ParseBool(HplVariable.Find(Component.variables, name), fallback);
        protected Vector3 VarVector3(string name, Vector3 fallback) => ParseVector3(HplVariable.Find(Component.variables, name), fallback);
        protected Color VarColor(string name, Color fallback) => ParseColor(HplVariable.Find(Component.variables, name), fallback);

        protected string InstanceVar(string name) => HplVariable.Find(Component.instanceVariables, name);
        protected float InstanceFloat(string name, float fallback) => ParseFloat(InstanceVar(name), fallback);
        protected bool InstanceBool(string name, bool fallback) => ParseBool(InstanceVar(name), fallback);
        protected string InstanceString(string name, string fallback) => InstanceVar(name) ?? fallback;

        protected float ConfigFloat(string key) => GameConfig != null ? GameConfig.GetFloat("Player_Interaction", key) : 0f;

        static float ParseFloat(string s, float fallback) => string.IsNullOrEmpty(s) ? fallback : HplMath.Atof(s);
        static int ParseInt(string s, int fallback) => string.IsNullOrEmpty(s) ? fallback : HplMath.Atoi(s);
        static bool ParseBool(string s, bool fallback) => string.IsNullOrEmpty(s) ? fallback : s.Trim().ToLowerInvariant() == "true";

        static Vector3 ParseVector3(string s, Vector3 fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            var parts = s.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var v = Vector3.zero;
            for (int i = 0; i < 3 && i < parts.Length; i++) v[i] = HplMath.Atof(parts[i]);
            return v;
        }

        static Color ParseColor(string s, Color fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            var parts = s.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            var c = new Color(0f, 0f, 0f, 1f);
            for (int i = 0; i < 4 && i < parts.Length; i++) c[i] = HplMath.Atof(parts[i]);
            return c;
        }

        // ------------------------------------------------------------------ bodies

        public HplPhysicsBody MainBody => _mainBody ?? (PhysicsBodies.Count > 0 ? PhysicsBodies[0] : null);

        /// <summary>The MainPhysicsBody variable's body, null if not set (mpMainBody).</summary>
        protected HplPhysicsBody ExplicitMainBody => _mainBody;

        public override Transform AttachTransform => MainBody != null ? MainBody.Data.transform : GameObject.transform;

        public HplPhysicsBody GetBodyFromId(int id) => PhysicsBodies.Find(b => b.Data.hplId == id);

        public int GetBodyIndexFromName(string name) => PhysicsBodies.FindIndex(b => b.Name == Name + "_" + name);

        /// <summary>iLuxProp::GetWorldCenterPos (Unity space).</summary>
        public Vector3 WorldCenterPos
        {
            get
            {
                if (MainBody != null) return MainBody.Position;
                var renderers = GameObject.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return GameObject.transform.position;
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                return b.center;
            }
        }

        public virtual InteractDataGrab GetGrabData() => null;
        public virtual InteractDataPush GetPushData() => null;
        public virtual InteractDataSlide GetSlideData() => null;
        public virtual InteractDataRotate GetMoveBaseData() => null;

        /// <summary>Highlight all bodies connected by joints when focused (grab objects).</summary>
        public virtual bool ShowOutlinesOnConnectedBodies => true;

        public void SetStaticPhysics(bool x)
        {
            if (StaticPhysics == x) return;
            StaticPhysics = x;
            for (int i = 0; i < PhysicsBodies.Count; i++)
            {
                var body = PhysicsBodies[i];
                body.Mass = StaticPhysics ? 0f : _bodyMasses[i];
                body.LinearVelocity = Vector3.zero;
                body.AngularVelocity = Vector3.zero;
            }
        }

        /// <summary>The mass a body had at load (cLuxProp_BodyData), for states that change and restore it.</summary>
        public float DefaultMass(HplPhysicsBody body)
        {
            int i = PhysicsBodies.IndexOf(body);
            return i >= 0 ? _bodyMasses[i] : body.Mass;
        }

        /// <summary>
        /// iLuxProp::SetDisableCollisionUntilOutSidePlayer: the player passes through the bodies until it is
        /// outside them (after letting go of a grabbed prop).
        /// </summary>
        public void SetDisableCollisionUntilOutsidePlayer(bool x)
        {
            _checkOutsidePlayer = x;
            if (!x) return;
            foreach (var body in PhysicsBodies) body.CollideCharacter = false;
        }

        bool _checkOutsidePlayer;

        void UpdateCheckIfOutsidePlayer()
        {
            if (!_checkOutsidePlayer) return;
            var player = Map.player;
            if (player == null || player.Body == null || player.Body.Shapes.Count == 0) return;
            var playerShape = player.Body.Shapes[player.Body.ActiveSize];
            var playerT = playerShape.transform;

            bool notColliding = true;
            for (int i = 0; i < PhysicsBodies.Count; i++)
            {
                var body = PhysicsBodies[i];
                if (body.CollideCharacter || !_defaultCollideCharacter[i]) continue;

                bool collide = false;
                foreach (var c in body.Colliders)
                {
                    if (c == null || !c.enabled) continue;
                    Transform t = c.transform;
                    if (Physics.ComputePenetration(c, t.position, t.rotation, playerShape, playerT.position, playerT.rotation, out _, out _))
                    {
                        collide = true;
                        break;
                    }
                }

                if (collide)
                {
                    notColliding = false;
                    // A small impulse away from the player, keeping the speed in that direction low.
                    Vector3 dir = body.HplPosition - HplSpace.Position(playerT.position);
                    dir.y *= 0.1f;
                    dir.Normalize();
                    Vector3 vel = body.HplLinearVelocity;
                    Vector3 relVel = dir * Vector3.Dot(vel, dir);
                    if (relVel.magnitude < 0.5f) body.AddImpulseHpl(dir * 0.5f);
                }
                else body.CollideCharacter = true;
            }
            if (notColliding) _checkOutsidePlayer = false;
        }

        // ------------------------------------------------------------------ health

        public void SetHealth(float x)
        {
            Health = x;
            OnHealthChange();
        }

        /// <summary>iLuxProp::GiveDamage: strength one below the toughness halves, lower does nothing.</summary>
        public override void GiveDamage(float amount, int strength)
        {
            if (strength < Toughness - 1) amount = 0f;
            else if (strength == Toughness - 1) amount *= 0.5f;
            SetHealth(Health - amount);
            OnDamage(amount, strength);
        }

        protected virtual void OnHealthChange() { }
        protected virtual void OnDamage(float amount, int strength) { }

        /// <summary>iLuxProp::ResetProperties: bodies back to their load pose, still, health 100.</summary>
        public void ResetProperties()
        {
            Matrix4x4 root = Matrix4x4.TRS(_loadPosition, _loadRotation, Vector3.one);
            foreach (var body in PhysicsBodies)
            {
                var pose = root * Matrix4x4.TRS(body.LoadLocalPosition, body.LoadLocalRotation, Vector3.one);
                body.SetPose(pose.GetPosition(), pose.rotation);
                body.LinearVelocity = Vector3.zero;
                body.AngularVelocity = Vector3.zero;
            }
            _moving = _movingAngular = _movingLinear = false;
            _moveAngularSpeed = 0f;
            Health = 100f;
            OnResetProperties();
        }

        protected virtual void OnResetProperties() { }

        /// <summary>Called by the map before the entity is removed (iLuxProp::BeforeEntityDestruction).</summary>
        internal virtual void BeforeDestruction() { }

        /// <summary>~iLuxProp: its particle systems that still exist are killed (they live on where they are).</summary>
        internal void KillParticleSystems()
        {
            foreach (var ps in _particleSystems)
                if (ps != null && Map.ParticleWorld.Exists(ps)) ps.Kill();
        }

        /// <summary>iLuxProp::OnSetActive: its particle systems show and update with it.</summary>
        protected override void OnSetActive(bool active)
        {
            base.OnSetActive(active);
            foreach (var ps in _particleSystems)
            {
                if (ps == null) continue;
                ps.SetVisible(active);
                ps.Active = active;
            }
        }

        // ------------------------------------------------------------------ update

        protected override void OnUpdate(float timeStep)
        {
            UpdateMoving(timeStep);
            UpdateCheckIfOutsidePlayer();
            UpdateEffectFading(timeStep);
            UpdateMeshFading(timeStep);
            UpdateAnimation();
            UpdateAttachedProps(false);
            for (int i = 0; i < InteractConnections.Count; i++)
            {
                InteractConnections[i].Update(timeStep);
                InteractConnections[i].UpdateProp(timeStep);
            }
            UpdatePropSpecific(timeStep);
        }

        protected virtual void UpdatePropSpecific(float timeStep) { }

        // iLuxProp::mfFadeInAlpha / mfFadeInSpeed: in a reference object, not saved (a loaded prop shows at once).
        sealed class MeshFade
        {
            public HplMeshFader fader;
            public float alpha = 1f, speed;
        }
        readonly MeshFade _meshFade = new();

        /// <summary>The coverage of the model while it fades in (1 when not fading).</summary>
        public float MeshFadeAlpha => _meshFade.alpha;

        /// <summary>iLuxProp::FadeInMeshEntity: the coverage of the model goes from 0 to 1 in the given time.</summary>
        public void FadeInMeshEntity(float time)
        {
            _meshFade.fader ??= new HplMeshFader(HplMeshFader.ModelRenderers(GameObject.transform));
            _meshFade.alpha = 0f;
            _meshFade.speed = 1f / time;
            // HPL2 sets the coverage in the next update; 0 now, so the model does not show for one frame.
            _meshFade.fader.SetAlpha(time > 0f ? 0f : 1f);
            if (time <= 0f) _meshFade.alpha = 1f;
        }

        /// <summary>iLuxProp::UpdateMeshFading.</summary>
        void UpdateMeshFading(float timeStep)
        {
            if (_meshFade.alpha >= 1f || _meshFade.fader == null) return;
            _meshFade.alpha += _meshFade.speed * timeStep;
            _meshFade.fader.SetAlpha(_meshFade.alpha);
        }

        // ------------------------------------------------------------------ attached props

        sealed class AttachedProp
        {
            public string name, file;
            public Matrix4x4 offset;          // HPL2, relative to the parent's main body
            public HplProp prop;
            public Matrix4x4 bodyLocal;       // HPL2, the attached prop's first body in its entity (mvBodyExtraData[0])
        }

        readonly List<AttachedProp> _attachedProps = new();

        /// <summary>iLuxProp::mpAttachmentParent.</summary>
        public HplProp AttachmentParent { get; private set; }
        public bool HasAttachedProp => _attachedProps.Count > 0;
        public IEnumerable<HplProp> AttachedProps { get { foreach (var a in _attachedProps) yield return a.prop; } }

        /// <summary>
        /// iLuxProp::AddAndAttachProp: the .ent is created at the origin, not saved, its bodies static, colliding with
        /// nothing and inactive, and it follows this prop's main body with the offset.
        /// </summary>
        public void AddAndAttachProp(string name, string file, Matrix4x4 hplOffset)
        {
            if (MainBody == null)
            {
                HplLog.Error($"Could not attach prop {Name} to {file} because it does not have a main body!");
                return;
            }
            var entity = Map.CreateEntity(name, file, Vector3.zero, Quaternion.identity, Vector3.one);
            if (entity is not HplProp prop || prop.Type != HplEntityType.Prop || prop.Name != name)
            {
                HplLog.Error($"Could not create attach entity '{file}' for prop '{Name}'");
                return;
            }
            prop.IsSaved = false;
            foreach (var body in prop.PhysicsBodies)
            {
                body.Mass = 0f;
                body.Collide = false;
                body.CollideCharacter = false;
                body.Active = false;
            }
            // Created at the origin: the body's pose is its local transform in the entity.
            Matrix4x4 bodyLocal = prop.PhysicsBodies.Count > 0 ? prop.PhysicsBodies[0].HplMatrix : Matrix4x4.identity;
            _attachedProps.Add(new AttachedProp { name = name, file = file, offset = hplOffset, prop = prop, bodyLocal = bodyLocal });
            prop.AttachmentParent = this;
            UpdateAttachedProps(true);
        }

        /// <summary>iLuxProp::DestroyAttachedProp(name).</summary>
        public bool DestroyAttachedProp(string name)
        {
            int i = _attachedProps.FindIndex(a => a.name == name);
            if (i < 0) return false;
            var attached = _attachedProps[i];
            _attachedProps.RemoveAt(i);
            attached.prop.AttachmentParent = null;
            Map.DestroyEntity(attached.prop);
            return true;
        }

        /// <summary>The prop's destruction (~iLuxProp): off its parent; its own attached props are destroyed.</summary>
        internal void DestroyAttachments()
        {
            if (AttachmentParent != null)
            {
                AttachmentParent._attachedProps.RemoveAll(a => a.prop == this);
                AttachmentParent = null;
            }
            foreach (var a in _attachedProps)
            {
                a.prop.AttachmentParent = null;
                Map.DestroyEntity(a.prop);
            }
            _attachedProps.Clear();
        }

        /// <summary>iLuxProp::UpdateAttachedProps: body 0 = main body × offset × its local transform (not while the main body sleeps).</summary>
        void UpdateAttachedProps(bool force)
        {
            if (_attachedProps.Count == 0) return;
            if (!force && !MainBody.Enabled) return;
            foreach (var a in _attachedProps)
            {
                if (a.prop.PhysicsBodies.Count == 0) continue;
                a.prop.PhysicsBodies[0].SetHplMatrix(MainBody.HplMatrix * a.offset * a.bodyLocal);
            }
        }

        /// <summary>iLuxProp_SaveData::mvAttachedProps.</summary>
        public void CaptureAttachedProps(List<Save.SavedAttachedProp> list)
        {
            list.Clear();
            foreach (var a in _attachedProps) list.Add(new Save.SavedAttachedProp { name = a.name, file = a.file, offset = a.offset });
        }

        /// <summary>iLuxProp::SetupSaveData: the attached props are made again.</summary>
        public void RestoreAttachedProps(List<Save.SavedAttachedProp> list)
        {
            if (list == null) return;
            foreach (var a in list) AddAndAttachProp(a.name, a.file, a.offset);
        }

        // ------------------------------------------------------------------ animations

        /// <summary>The mesh entity's animations (cMeshEntity), null for a prop without them.</summary>
        public Enemies.HplMeshAnimation Animation { get; private set; }
        public GameObject AnimatedModel { get; private set; }
        int _currentNonLoopAnimIndex = -1;
        string _animCallback = "";

        /// <summary>
        /// cEntityLoader_Object plays the first animation looped (Play(0, true, true)); iLuxPropLoader::LoadProperties
        /// then starts it at a random time (RandomizeAnimationStart, default true). The skinned model under the entity
        /// is posed with the clips of its .ent (Import > Enemies converts every animated entity).
        /// </summary>
        void SetupAnimation()
        {
            var data = Enemies.HplEnemyAssets.Instance != null ? Enemies.HplEnemyAssets.Instance.GetEnemy(Component.file) : null;
            if (data == null || data.animations.Count == 0) return;
            var skinned = GameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var animator = GameObject.GetComponentInChildren<Animator>(true);
            if (skinned == null && animator == null) return;
            // The model root: the child of the entity, or of the body the import attached it to.
            Transform t = skinned != null ? skinned.transform : animator.transform;
            while (t.parent != null && t.parent != GameObject.transform && t.parent.GetComponent<World.AmnesiaBody>() == null) t = t.parent;
            var animation = new Enemies.HplMeshAnimation(t.gameObject, data);
            if (animation.States.Count == 0) return;
            AnimatedModel = t.gameObject;
            Animation = animation;
            Animation.Play(0, true, true);
            if (VarBool("RandomizeAnimationStart", true))
            {
                var first = Animation.States[0];
                first.SetTimePosition(HplMath.RandRectf(0f, first.Length));
            }
            Map?.RegisterAnimatedProp(this);
        }

        /// <summary>iLuxProp::PlayAnimation: a fade to the animation; a non-looping one calls back when it is over.</summary>
        public void PlayAnimation(string name, float fadeTime, bool loop, string callback)
        {
            int index = Animation != null ? Animation.IndexOf(name) : -1;
            if (index < 0)
            {
                HplLog.Error($"Could not find animation '{name}' for entity '{Name}'");
                return;
            }
            Animation.PlayFadeTo(index, loop, fadeTime);
            _currentNonLoopAnimIndex = loop ? -1 : index;
            _animCallback = loop ? "" : callback ?? "";
        }

        /// <summary>cEngineMeshEntity_SaveData::FromMeshEntity: the animation states.</summary>
        public void CaptureAnimations(List<Save.SavedAnimationState> list)
        {
            list.Clear();
            if (Animation == null) return;
            foreach (var s in Animation.States)
                list.Add(new Save.SavedAnimationState
                {
                    active = s.IsActive, loop = s.Loop, weight = s.Weight, fadeStep = s.FadeStep, timePosition = s.TimePosition, speed = s.Speed,
                });
        }

        /// <summary>cEngineMeshEntity_SaveData::ToMeshEntity: only when the count matches ("something is wrong" otherwise).</summary>
        public void RestoreAnimations(List<Save.SavedAnimationState> list)
        {
            if (Animation == null || list == null || list.Count != Animation.States.Count) return;
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                Animation.States[i].Restore(a.active, a.loop, a.weight, a.fadeStep, a.timePosition, a.speed);
            }
        }

        /// <summary>The non-looping animation playing (iLuxProp::mlCurrentNonLoopAnimIndex ≥ 0).</summary>
        protected bool NonLoopAnimationPlaying => _currentNonLoopAnimIndex >= 0;

        /// <summary>iLuxProp::UpdateAnimation.</summary>
        void UpdateAnimation()
        {
            if (_currentNonLoopAnimIndex < 0 || Animation == null) return;
            if (!Animation.States[_currentNonLoopAnimIndex].IsOver) return;
            _currentNonLoopAnimIndex = -1;
            if (_animCallback != "") Map.RunScript(_animCallback, Name);
        }

        /// <summary>cWorld::UpdateEntities: the mesh entity's animations and their events (after the physics).</summary>
        internal void StepMesh(float timeStep)
        {
            if (!Active || Animation == null) return;
            Animation.Update(timeStep, OnAnimationEvent);
            AfterAnimationPose();
        }

        /// <summary>After the skeleton was posed this step (cNode3D pre / post transforms of bones).</summary>
        protected virtual void AfterAnimationPose() { }

        void OnAnimationEvent(Enemies.HplAnimationState state, Enemies.HplEnemyAssets.AnimationEvent e)
        {
            // cMeshEntity::HandleAnimationEvent: only PlaySound does something.
            if (string.IsNullOrEmpty(e.value) || e.type != "PlaySound" || Map.sound == null) return;
            var sound = Map.sound.CreateSoundEntity("AnimEvent", e.value, true);
            if (sound == null) return;
            sound.IsSaved = false;
            sound.Parent = Animation.SoundNode;
        }

        // ------------------------------------------------------------------ interact connections

        /// <summary>iLuxProp::mvInteractConnections (InteractConnectPropWithRope / ...WithMoveObject).</summary>
        public readonly List<HplInteractConnection> InteractConnections = new();

        /// <summary>iLuxProp::InteractConnectionLimit: a lever / wheel state reaches the connections.</summary>
        protected void InteractConnectionLimit(int state)
        {
            foreach (var c in InteractConnections)
            {
                if (c.InteractionOnly && !IsInteractedWith) continue;
                c.OnLimit(state);
            }
        }

        /// <summary>iLuxProp::InteractConnectionTurn: the turned share of the range reaches the connections.</summary>
        protected void InteractConnectionTurn(float angle, float prevAngle, float minAngle, float maxAngle)
        {
            float t = (angle - minAngle) / (maxAngle - minAngle);
            foreach (var c in InteractConnections)
            {
                if (c.InteractionOnly && !IsInteractedWith) continue;
                c.OnTurn(angle - prevAngle, t);
            }
        }

        // ------------------------------------------------------------------ effects (lights, billboards, particles)

        string _effectsOnSound = "", _effectsOffSound = "";
        float _effectsOnTime = 1f, _effectsOffTime = 1f, _effectsOffLightRadius = 1f;
        Color _effectsOffLightColor;
        bool _effectsActive = true, _effectAlphaFading = true;
        protected bool HasFlickering { get; private set; }

        public float EffectsAlpha { get; private set; } = 1f;
        public bool EffectsActive => _effectsActive;

        protected readonly List<AmnesiaLight> Lights = new();
        protected readonly List<(Color onColor, float onRadius, bool flickering)> LightData = new();
        readonly List<(AmnesiaBillboard billboard, Color onColor, bool connectedToLight)> _billboards = new();
        // mvParticleSystems (null once destroyed) and cLuxProp_PSData to create them again when the effects go on.
        readonly List<Particles.HplParticleSystem> _particleSystems = new();
        readonly List<(string name, string dataName, Matrix4x4 local, Transform parent)> _psData = new();
        readonly List<(Renderer renderer, int index, Color emissive)> _illumination = new();
        float _illuminationAmount = float.NaN;
        MaterialPropertyBlock _block;

        void SetupEffectData()
        {
            foreach (var light in GameObject.GetComponentsInChildren<AmnesiaLight>(true))
            {
                Lights.Add(light);
                LightData.Add((light.diffuseColor, light.radius, light.flickerActive));
            }
            if (Lights.Count > 0 && Lights[0].flickerActive) HasFlickering = true;

            foreach (var bb in GameObject.GetComponentsInChildren<AmnesiaBillboard>(true))
            {
                bool connected = !string.IsNullOrEmpty(bb.connectLight) && Lights.Exists(l => l.name == bb.connectLight);
                _billboards.Add((bb, bb.color, connected));
            }
            foreach (var ps in Map.ParticleSystemsUnder(GameObject.transform))
            {
                _particleSystems.Add(ps);
                _psData.Add((ps.Name, ps.DataName, ps.LocalMatrix, ps.Parent));
            }

            foreach (var renderer in GameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.GetComponent<AmnesiaBillboard>() != null) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var m = materials[i];
                    if (m == null || !m.HasProperty("_EmissiveColor")) continue;
                    Color e = m.GetColor("_EmissiveColor");
                    if (e.maxColorComponent <= 0f) continue;
                    _illumination.Add((renderer, i, e));
                }
            }
        }

        /// <summary>iLuxProp::SetEffectsActive: lights, particle systems and sounds on / off, faded or at once.</summary>
        public void SetEffectsActive(bool active, bool fadeAndPlaySounds)
        {
            if (active == _effectsActive) return;
            _effectsActive = active;
            _effectAlphaFading = true;

            if (_effectsActive)
            {
                // New systems from the saved data: the color and fade of the instance are not kept (as in HPL2).
                for (int i = 0; i < _particleSystems.Count; i++)
                {
                    var (psName, dataName, local, parent) = _psData[i];
                    var ps = Map.ParticleWorld.Create(psName, dataName);
                    if (ps != null)
                    {
                        ps.LocalMatrix = local;
                        ps.Parent = parent;
                    }
                    _particleSystems[i] = ps;
                }
                for (int i = 0; i < Lights.Count; i++)
                {
                    var (onColor, onRadius, flickering) = LightData[i];
                    if (fadeAndPlaySounds) Lights[i].FadeTo(onColor, onRadius, _effectsOnTime);
                    else
                    {
                        Lights[i].SetColor(onColor);
                        Lights[i].SetRadius(onRadius);
                        Lights[i].SetFlickerActive(flickering);
                    }
                }
                if (!fadeAndPlaySounds) EffectsAlpha = 1f;
            }
            else
            {
                for (int i = 0; i < _particleSystems.Count; i++)
                {
                    var ps = _particleSystems[i];
                    _particleSystems[i] = null;
                    if (ps == null) continue;
                    if (fadeAndPlaySounds) ps.Kill();
                    else Map.ParticleWorld.Destroy(ps);
                }
                for (int i = 0; i < Lights.Count; i++)
                {
                    var light = Lights[i];
                    light.StopFading();
                    light.SetFlickerActive(false);
                    float offRadius = _effectsOffLightRadius >= 0f ? _effectsOffLightRadius : LightData[i].onRadius;
                    if (fadeAndPlaySounds) light.FadeTo(_effectsOffLightColor, offRadius, _effectsOffTime);
                    else
                    {
                        light.SetColor(_effectsOffLightColor);
                        light.SetRadius(offRadius);
                    }
                }
                if (!fadeAndPlaySounds) EffectsAlpha = 0f;
            }
            if (Map.sound == null) return;
            foreach (var sound in Map.SoundEntitiesUnder(GameObject.transform))
            {
                if (_effectsActive)
                {
                    if (fadeAndPlaySounds) sound.FadeIn(1f / _effectsOnTime);
                    else sound.Play(false);
                }
                else if (fadeAndPlaySounds) sound.FadeOut(1f / _effectsOffTime);
                else sound.Stop(false);
            }
            if (fadeAndPlaySounds)
                PlaySoundAt(Name + "_lightlitchange", _effectsActive ? _effectsOnSound : _effectsOffSound, WorldCenterPos);
        }

        void UpdateEffectFading(float timeStep)
        {
            if (!_effectAlphaFading) return;
            if (_effectsActive)
            {
                EffectsAlpha += 1f / _effectsOnTime * timeStep;
                if (EffectsAlpha > 1f)
                {
                    EffectsAlpha = 1f;
                    for (int i = 0; i < Lights.Count; i++) Lights[i].SetFlickerActive(LightData[i].flickering);
                    _effectAlphaFading = false;
                }
            }
            else
            {
                EffectsAlpha -= 1f / _effectsOffTime * timeStep;
                if (EffectsAlpha < 0f)
                {
                    EffectsAlpha = 0f;
                    _effectAlphaFading = false;
                }
            }

            foreach (var (billboard, onColor, connected) in _billboards)
                if (!connected && billboard != null) billboard.SetColor(onColor * EffectsAlpha);

            SetIlluminationAmount(EffectsAlpha);
        }

        /// <summary>cMeshEntity::SetIlluminationAmount: emission × amount (property blocks).</summary>
        void SetIlluminationAmount(float amount)
        {
            // Only a change: every property block write marks the material changed for HDRP's path tracer.
            if (_illumination.Count == 0 || amount == _illuminationAmount) return;
            _illuminationAmount = amount;
            _block ??= new MaterialPropertyBlock();
            foreach (var (renderer, index, emissive) in _illumination)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(_block, index);
                _block.SetColor("_EmissiveColor", emissive * amount);
                renderer.SetPropertyBlock(_block, index);
            }
        }

        protected void SetMeshVisible(bool visible)
        {
            foreach (var renderer in GameObject.GetComponentsInChildren<Renderer>(true))
                if (renderer.GetComponent<AmnesiaBillboard>() == null && renderer.GetComponentInParent<AmnesiaLight>() == null)
                    renderer.enabled = visible;
        }

        // ------------------------------------------------------------------ static movement (HPL2 space)

        bool _moving, _movingLinear, _movingAngular, _moveCheckCollision = true;
        float _moveStartCount;
        Matrix4x4 _lastBodyMoveMatrix = Matrix4x4.identity;
        Vector3 _moveLinearGoal;
        float _moveLinearAcc, _moveLinearMaxSpeed, _moveLinearSpeed, _moveLinearSlowdownDist;
        Matrix4x4 _moveAngularGoal;
        float _moveAngularAcc, _moveAngularMaxSpeed, _moveAngularSpeed, _moveAngularSlowdownDist;
        bool _moveAngularNoGoal, _moveAngularUseOffset;
        Vector3 _moveAngularNoGoalDir, _moveAngularWorldOffset, _moveAngularLocalOffset;

        public bool IsMoving => _moving;

        string _moveStartSound = "", _moveStopSound = "", _moveLoopSound = "";
        Sound.HplSoundEntity _moveLoopSoundEntity;
        float _movingVolume;

        void PlayMoveStartSound()
        {
            if (!_moving && _moveStartCount <= 0f) PlaySound("MoveStart", _moveStartSound, true, true);
        }

        /// <summary>iLuxProp::MoveLinearTo (HPL2 space goal).</summary>
        public void MoveLinearTo(Vector3 goal, float acc, float maxSpeed, float slowdownDist, bool resetSpeed)
        {
            PlayMoveStartSound();
            _moveStartCount = 1f;
            _moving = _movingLinear = true;
            _moveLinearGoal = goal;
            _moveLinearAcc = acc;
            _moveLinearMaxSpeed = maxSpeed;
            _moveLinearSlowdownDist = slowdownDist;
            if (resetSpeed) _moveLinearSpeed = 0f;
            OnStartMove();
        }

        public void MoveAngularTo(Matrix4x4 goal, float acc, float maxSpeed, float slowdownDist, bool resetSpeed,
                                  bool useOffset, Vector3 worldOffset, Vector3 localOffset)
        {
            PlayMoveStartSound();
            _moveStartCount = 1f;
            _moving = _movingAngular = true;
            _moveAngularGoal = goal;
            _moveAngularAcc = acc;
            _moveAngularMaxSpeed = maxSpeed;
            _moveAngularSlowdownDist = slowdownDist;
            if (resetSpeed) _moveAngularSpeed = 0f;
            _moveAngularUseOffset = useOffset;
            _moveAngularWorldOffset = worldOffset;
            _moveAngularLocalOffset = localOffset;
            _moveAngularNoGoal = false;
            OnStartMove();
        }

        public void RotateAtSpeed(float acc, float goalSpeed, Vector3 axis, bool resetSpeed, bool useOffset, Vector3 worldOffset, Vector3 localOffset)
        {
            PlayMoveStartSound();
            _moveStartCount = 1f;
            _moving = _movingAngular = true;
            _moveAngularAcc = acc;
            _moveAngularMaxSpeed = goalSpeed;
            if (resetSpeed) _moveAngularSpeed = 0f;
            _moveAngularNoGoal = true;
            _moveAngularNoGoalDir = axis.normalized;
            _moveAngularUseOffset = useOffset;
            _moveAngularWorldOffset = worldOffset;
            _moveAngularLocalOffset = localOffset;
            OnStartMove();
        }

        public void StopMove()
        {
            _movingLinear = _movingAngular = _moving = false;
            if (_moveStartCount <= 0f) PlaySound("MoveStop", _moveStopSound, true, true);
        }

        void SetMoveSoundVolume()
        {
            _moveLoopSoundEntity?.GetEntry(Sound.SoundEntityType.Main)?.SetVolumeMul(_movingVolume);
        }

        void DestroyMoveLoopSound()
        {
            if (_moveLoopSoundEntity == null) return;
            _moveLoopSoundEntity.Stop(false);
            Map.sound.DestroySoundEntity(_moveLoopSoundEntity);
            _moveLoopSoundEntity = null;
        }

        /// <summary>
        /// iLuxProp::UpdateMoving's sound part: the loop sound fades in (1.2 / s) while the main body moves and out
        /// (1 / s) when it stops.
        /// </summary>
        void UpdateMoveSound(float timeStep)
        {
            if (!_moving)
            {
                if (_movingVolume <= 0f) return;
                _movingVolume -= timeStep;
                if (_movingVolume < 0f)
                {
                    _movingVolume = 0f;
                    DestroyMoveLoopSound();
                }
                else SetMoveSoundVolume();
                return;
            }

            const float minMove = 0.001f * (1f / 60f);
            bool hasSpeed = false;
            Matrix4x4 m = MainBody.HplMatrix;
            if (_movingLinear && (m.GetPosition() - _lastBodyMoveMatrix.GetPosition()).sqrMagnitude > minMove * minMove) hasSpeed = true;
            if (!hasSpeed && _movingAngular &&
                HplMatrix.EulerAngleDistance(_lastBodyMoveMatrix, m).sqrMagnitude > minMove * minMove) hasSpeed = true;

            if (hasSpeed && _movingVolume < 1f)
            {
                if (_moveLoopSoundEntity == null && _moveLoopSound != "")
                {
                    _moveLoopSoundEntity = PlaySound("MoveLoop", _moveLoopSound, false, true);
                    if (_moveLoopSoundEntity != null)
                    {
                        _moveLoopSoundEntity.Play(false);
                        _moveLoopSoundEntity.GetEntry(Sound.SoundEntityType.Main)?.SetVolumeMul(0f);
                    }
                }
                _movingVolume = Mathf.Min(1f, _movingVolume + timeStep * 1.2f);
                SetMoveSoundVolume();
            }
            else if (!hasSpeed && _movingVolume > 0f)
            {
                _movingVolume -= timeStep;
                if (_movingVolume < 0f)
                {
                    _movingVolume = 0f;
                    DestroyMoveLoopSound();
                }
                else SetMoveSoundVolume();
            }
        }

        protected virtual void OnStartMove() { }

        void UpdateMoving(float timeStep)
        {
            if (_moveStartCount > 0f) _moveStartCount -= timeStep;
            if (MainBody == null) return;
            UpdateMoveSound(timeStep);
            if (!_moving) return;
            _lastBodyMoveMatrix = MainBody.HplMatrix;
            UpdateLinearMoving(timeStep);
            UpdateAngularMoving(timeStep);
            if (!_movingLinear && !_movingAngular) StopMove();
        }

        void UpdateLinearMoving(float timeStep)
        {
            if (!_movingLinear) return;
            var body = MainBody;
            Vector3 delta = _moveLinearGoal - body.HplPosition;
            float dist = delta.magnitude;
            Vector3 moveVel;

            if (dist <= _moveLinearSpeed * timeStep * 1.05f || dist < 0.01f)
            {
                _moveLinearSpeed = 0f;
                moveVel = delta / timeStep;
                _movingLinear = false;
            }
            else
            {
                Vector3 dir = delta / dist;
                if (dist < _moveLinearSlowdownDist) _moveLinearSpeed = dist / _moveLinearSlowdownDist * _moveLinearMaxSpeed;
                else
                {
                    _moveLinearSpeed += _moveLinearAcc * timeStep;
                    if (_moveLinearSpeed > _moveLinearMaxSpeed) _moveLinearSpeed = _moveLinearMaxSpeed;
                }
                moveVel = dir * _moveLinearSpeed;
            }

            if (_moveCheckCollision) CheckMoveCollision(ref moveVel, ref _moveLinearSpeed, timeStep);
            if (moveVel != Vector3.zero) body.StaticLinearMove(HplSpace.Direction(moveVel));
        }

        void UpdateAngularMoving(float timeStep)
        {
            if (!_movingAngular) return;
            var body = MainBody;
            Vector3 moveVel;

            if (_moveAngularNoGoal)
            {
                float acc = _moveAngularMaxSpeed < _moveAngularSpeed ? -_moveAngularAcc : _moveAngularAcc;
                _moveAngularSpeed += acc * timeStep;
                if ((acc < 0f && _moveAngularSpeed < _moveAngularMaxSpeed) || (acc > 0f && _moveAngularSpeed > _moveAngularMaxSpeed))
                {
                    _moveAngularSpeed = _moveAngularMaxSpeed;
                    if (_moveAngularSpeed == 0f) _movingAngular = false;
                }
                moveVel = _moveAngularNoGoalDir * _moveAngularSpeed;
            }
            else
            {
                Vector3 delta = HplMatrix.EulerAngleDistance(body.HplMatrix, _moveAngularGoal);
                float dist = delta.magnitude;
                if (dist <= _moveAngularSpeed * timeStep * 1.05f || dist < 0.01f)
                {
                    _moveAngularSpeed = 0f;
                    moveVel = delta / timeStep;
                    _movingAngular = false;
                }
                else
                {
                    Vector3 dir = delta / dist;
                    if (dist < _moveAngularSlowdownDist) _moveAngularSpeed = dist / _moveAngularSlowdownDist * _moveAngularMaxSpeed;
                    else
                    {
                        _moveAngularSpeed += _moveAngularAcc * timeStep;
                        if (_moveAngularSpeed > _moveAngularMaxSpeed) _moveAngularSpeed = _moveAngularMaxSpeed;
                    }
                    moveVel = dir * _moveAngularSpeed;
                }
            }

            body.StaticAngularMove(HplSpace.Axial(moveVel));

            if (_moveAngularUseOffset)
            {
                Matrix4x4 m = body.HplMatrix;
                Matrix4x4 newBody = HplMatrix.RotateXYZ(moveVel * timeStep) * HplMatrix.GetRotation(m);
                newBody = HplMatrix.SetTranslation(newBody, HplMatrix.GetTranslation(m));
                Vector3 offset = HplMatrix.Mul(newBody, _moveAngularLocalOffset);
                Vector3 diff = _moveAngularWorldOffset - offset;
                body.StaticLinearMove(HplSpace.Direction(diff / timeStep));
            }
        }

        readonly Collider[] _moveOverlaps = new Collider[32];

        /// <summary>
        /// iLuxProp::CheckMoveCollision: the body's shape at the moved pose against dynamic bodies; the part of the
        /// move into them is removed.
        /// </summary>
        void CheckMoveCollision(ref Vector3 moveVel, ref float speed, float timeStep)
        {
            if (moveVel == Vector3.zero) return;
            var body = MainBody;
            Vector3 add = moveVel * timeStep;
            Vector3 addU = HplSpace.Direction(add);
            Vector3 push = Vector3.zero;
            foreach (var c in body.Colliders)
            {
                if (c == null || !c.enabled) continue;
                Transform t = c.transform;
                Vector3 pos = t.position + addU;
                var b = c.bounds;
                int count = Physics.OverlapBoxNonAlloc(b.center + addU, b.extents, _moveOverlaps, Quaternion.identity,
                                                       Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var other = _moveOverlaps[i];
                    var rb = other.attachedRigidbody;
                    if (rb == null || rb.isKinematic || rb == body.Rigidbody) continue;
                    if (!Physics.ComputePenetration(c, pos, t.rotation, other, other.transform.position, other.transform.rotation,
                                                    out Vector3 dir, out float d)) continue;
                    Vector3 p = HplSpace.Direction(dir * d);
                    if (Mathf.Abs(push.x) < Mathf.Abs(p.x)) push.x = p.x;
                    if (Mathf.Abs(push.y) < Mathf.Abs(p.y)) push.y = p.y;
                    if (Mathf.Abs(push.z) < Mathf.Abs(p.z)) push.z = p.z;
                }
            }
            if (push == Vector3.zero) return;
            float velAmount = 1f - Vector3.Dot(push, add * -1f) / add.sqrMagnitude;
            if (velAmount < 0f) velAmount = 0f;
            speed *= velAmount;
            moveVel *= velAmount;
        }
    }
}
