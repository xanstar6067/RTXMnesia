using System;
using System.Collections.Generic;
using RTXMnesia.Effects;
using RTXMnesia.Hpl;
using RTXMnesia.Player;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.ScriptApi
{
    /// <summary>
    /// The running map (cLuxMap + the map handler's update order): script entities by name, player starts,
    /// bodies and lights, script timers and local variables, and the level script (<see cref="HplScript"/>,
    /// found by <see cref="HplMapScriptAttribute"/>).
    ///
    /// One HPL2 step per FixedUpdate (60 Hz), in the engine's order: map (timers, then entity logic with collide
    /// and look-at callbacks), player (move state, head, helpers, "Player" collide callbacks), messages / hints,
    /// effects, then physics (the character body) and lights.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class HplMap : MonoBehaviour
    {
        [Tooltip("Map file relative to the game folder, e.g. maps/main/ch01/00_rainy_hall.map")]
        public string mapFile;
        public HplConfig gameConfig;
        public HplConfig menuConfig;
        [Tooltip("config/default_user_keys.cfg: key names in messages and hints ($ButJournal).")]
        public HplConfig keysConfig;
        public HplLanguage language;
        public AmnesiaPlayer player;
        public HplEffects effects;
        public HplHud hud;
        [Tooltip("materials.cfg: physics materials and surface data.")]
        public HplSurfaces surfaces;

        [Tooltip("ScriptDebugOn() in the level script (the original skips intros and runs test code).")]
        public bool scriptDebug;
        [Tooltip("Show AddDebugMessage calls in the console (HPL2 shows them only in debug mode).")]
        public bool logDebugMessages;
        [Tooltip("Run the level script. Off: the level without its events.")]
        public bool runScript = true;

        public static HplMap Current { get; private set; }

        public HplScript Script { get; private set; }
        public Dynamics.HplPhysicsWorld PhysicsWorld { get; private set; }
        public float GameTime { get; private set; }
        public Camera PlayerCamera => player != null ? player.playerCamera : null;
        public readonly HplCollideCallbacks PlayerCollideCallbacks = new();

        [NonSerialized] public string DisplayNameEntry = "";
        [NonSerialized] public int NumberOfQuests;
        [NonSerialized] public string LanternLitCallback = "";
        [NonSerialized] public (string name, string startPos, string callback, int count) CheckPointData;
        [NonSerialized] public (string textCat, string textEntry, int randomNum, string image) LoadScreen;

        sealed class Timer
        {
            public string name, function;
            public float count;
            public bool destroyMe;
        }

        readonly List<HplEntity> _entities = new();
        readonly Dictionary<string, HplEntity> _entitiesByName = new(StringComparer.Ordinal);
        readonly Dictionary<string, AmnesiaArea> _playerStarts = new(StringComparer.Ordinal);
        readonly List<AmnesiaArea> _playerStartList = new();
        readonly Dictionary<string, HplBody> _bodies = new(StringComparer.Ordinal);
        readonly Dictionary<string, AmnesiaLight> _lights = new(StringComparer.Ordinal);
        readonly List<AmnesiaLight> _lightList = new();
        readonly List<Timer> _timers = new();
        readonly Dictionary<string, string> _vars = new(StringComparer.Ordinal);
        bool _updatingTimers, _built, _started;

        Fog _fog;
        AmnesiaLevel _level;
        PlayerBodies _playerBodies;
        Action _checkPlayerCallbacks;

        public IReadOnlyList<HplEntity> Entities => _entities;

        void Awake() => Build();

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void Start() => Begin();

        // Play mode: Unity simulates after FixedUpdate (needed for Rigidbody interpolation); the part of the step
        // after the simulation runs when the physics step is done (WaitForFixedUpdate).
        void FixedUpdate()
        {
            if (!Application.isPlaying) return;
            if (Physics.simulationMode == SimulationMode.Script)
            {
                Step(Time.fixedDeltaTime);
                return;
            }
            StepBeforeSimulate(Time.fixedDeltaTime);
            _afterPending = true;
            if (_afterRoutine == null) _afterRoutine = StartCoroutine(AfterPhysics());
        }

        bool _afterPending;
        Coroutine _afterRoutine;
        static readonly WaitForFixedUpdate WaitPhysics = new();

        System.Collections.IEnumerator AfterPhysics()
        {
            while (true)
            {
                yield return WaitPhysics;
                if (!_afterPending) continue;
                _afterPending = false;
                StepAfterSimulate(Time.fixedDeltaTime);
            }
        }

        /// <summary>Collects the scene into the script registries and creates the level script. Safe to call twice.</summary>
        public void Build()
        {
            if (_built) return;
            _built = true;
            Current = this;
            _level = GetComponent<AmnesiaLevel>();
            if (string.IsNullOrEmpty(mapFile) && _level != null) mapFile = _level.mapFile;
            if (player == null) player = FindAnyObjectByType<AmnesiaPlayer>(FindObjectsInactive.Include);
            if (effects == null && !TryGetComponent(out effects)) effects = gameObject.AddComponent<HplEffects>();
            if (hud == null && !TryGetComponent(out hud)) hud = gameObject.AddComponent<HplHud>();
            effects.Init(this);
            hud.Init(this);
            if (player != null)
            {
                player.steppedByMap = true;
                _playerBodies = new PlayerBodies(player);
                _checkPlayerCallbacks = () => PlayerCollideCallbacks.Check("Player", _playerBodies, this);
            }

            PhysicsWorld = new Dynamics.HplPhysicsWorld();
            _templateRoot = transform.Find("EntityTemplates");
            _entityRoot = transform.Find("Entities") ?? transform;

            foreach (var area in GetComponentsInChildren<AmnesiaArea>(true))
            {
                if (IsTemplate(area.transform)) continue;
                switch (area.areaType)
                {
                    case "PlayerStart":
                        _playerStarts.TryAdd(area.name, area);
                        _playerStartList.Add(area);
                        break;
                    case "PathNode":
                    case "PosNode":
                        break;
                    default:
                        AddEntity(new HplArea(area, gameConfig));
                        break;
                }
            }
            foreach (var light in GetComponentsInChildren<AmnesiaLight>(true))
            {
                if (IsTemplate(light.transform)) continue;
                if (_lights.TryAdd(light.name, light)) _lightList.Add(light);
                light.InitRuntime();
            }
            ConnectBillboards(transform);
            foreach (var entity in GetComponentsInChildren<AmnesiaEntity>(true))
            {
                if (IsTemplate(entity.transform)) continue;
                var prop = Props.PropFactory.Create(entity);
                if (prop == null)
                {
                    // cLuxStaticPropLoader: not a script entity, but its bodies (and joints) exist.
                    PhysicsWorld.AddEntity(entity.transform, surfaces, Application.isPlaying);
                    continue;
                }
                prop.Load(this, PhysicsWorld);
                AddEntity(prop);
            }
            if (player != null && player.Body != null) PhysicsWorld.SetCharacterShapes(player.Body.Shapes);

            InitFog();

            var scriptType = HplScript.FindScriptType(mapFile);
            if (scriptType != null)
            {
                Script = (HplScript)Activator.CreateInstance(scriptType);
                Script.Map = this;
            }
            else
            {
                HplLog.Warn($"No level script for '{mapFile}' (class with [HplMapScript(\"{mapFile}\")])");
            }
        }

        void AddEntity(HplEntity entity)
        {
            entity.Map = this;
            _entities.Add(entity);
            // cLuxMap::AddEntity: names are looked up in lower case, the first entity of a name wins.
            _entitiesByName.TryAdd(entity.Name.ToLowerInvariant(), entity);
            foreach (var body in entity.Bodies) _bodies.TryAdd(body.Name, body);
        }

        /// <summary>cLuxMapHandler::SetCurrentMap: the player is at its start, then OnStart (first visit) and OnEnter run.</summary>
        public void Begin()
        {
            Build();
            if (_started) return;
            _started = true;
            if (player != null && player.Body != null) PhysicsWorld.SetCharacterShapes(player.Body.Shapes);
            if (player != null && player.LookAt != null)
            {
                player.LookAt.AtTarget += function => RunScript(function);
                player.SanityGained += () => effects.SanityGainFlash.Start();
                // cLuxPlayer::GiveSanityDamage: hint, then the hit effect (cLuxPlayerSanity, step 6).
                player.SanityHit += () => hud.Hints.Add("SanityHit", hud.Translate("Hints", "SanityHit"), 0f);
            }
            if (!runScript || Script == null) return;
            Script.Run(Script.OnStart, "OnStart");
            Script.Run(Script.OnEnter, "OnEnter");
        }

        /// <summary>One HPL2 logic step with an explicit physics simulation (edit mode tools, script simulation mode).</summary>
        public void Step(float timeStep)
        {
            StepBeforeSimulate(timeStep);
            Physics.Simulate(timeStep);
            StepAfterSimulate(timeStep);
        }

        /// <summary>
        /// The Update message (map: timers, entities; player; HUD; effects), then the world update up to the rigid
        /// body simulation: the character body, the bodies' forces and joints' limits.
        /// </summary>
        public void StepBeforeSimulate(float timeStep)
        {
            Begin();

            // cLuxMap::Update
            UpdateTimers(timeStep);
            for (int i = 0; i < _entities.Count; i++)
                if (_entities[i].Active) _entities[i].UpdateLogic(timeStep);

            // cLuxPlayer::Update (its collide callbacks before the camera effects, the body moves later)
            if (player != null) player.StepLogic(timeStep, _checkPlayerCallbacks);

            hud.Step(timeStep);
            effects.Step(timeStep);

            // PostUpdate message: cScene::PostUpdate → cWorld::Update → iPhysicsWorld::Update.
            if (player != null) player.StepPhysics(timeStep);
            PhysicsWorld.BeforeSimulate(timeStep);
        }

        /// <summary>After the simulation: joints and bodies, lights, the player's post update, destroyed entities.</summary>
        public void StepAfterSimulate(float timeStep)
        {
            PhysicsWorld.AfterSimulate(timeStep);
            for (int i = 0; i < _lightList.Count; i++) _lightList[i].UpdateLight(timeStep);
            if (player != null) player.StepPostUpdate(timeStep);
            DestroyPendingEntities();
            GameTime += timeStep;
        }

        // ------------------------------------------------------------------ entity creation / destruction

        readonly List<HplEntity> _destroy = new();
        Transform _templateRoot, _entityRoot;

        bool IsTemplate(Transform t) => _templateRoot != null && t.IsChildOf(_templateRoot);

        /// <summary>Lights drive the billboards connected to them (cEFL_LightBillboardConnection, iLight::AttachBillboard).</summary>
        void ConnectBillboards(Transform root)
        {
            foreach (var bb in root.GetComponentsInChildren<AmnesiaBillboard>(true))
            {
                if (string.IsNullOrEmpty(bb.connectLight) || IsTemplate(bb.transform)) continue;
                // A billboard of an entity connects to a light of that entity, a map billboard to a map light.
                var owner = bb.GetComponentInParent<AmnesiaEntity>(true);
                var light = owner != null ? FindLight(owner.transform, bb.connectLight) : GetLight(bb.connectLight);
                if (light == null) HplLog.Warn($"Light with name '{bb.connectLight}' does not exist!");
                else light.AttachBillboard(bb);
            }
        }

        static AmnesiaLight FindLight(Transform root, string name)
        {
            foreach (var l in root.GetComponentsInChildren<AmnesiaLight>(true)) if (l.name == name) return l;
            return null;
        }

        /// <summary>cLuxMap::DestroyEntity: the entity goes after this step.</summary>
        public void DestroyEntity(HplEntity entity)
        {
            if (entity == null || _destroy.Contains(entity)) return;
            if (entity is HplProp prop) prop.DestroyMe = true;
            _destroy.Add(entity);
        }

        void DestroyPendingEntities()
        {
            if (_destroy.Count == 0) return;
            var list = new List<HplEntity>(_destroy);
            _destroy.Clear();
            foreach (var entity in list)
            {
                if (player != null && player.States != null) player.States.Current.OnDestroyEntity(entity);
                if (entity is HplProp prop)
                {
                    prop.BeforeDestruction();
                    PhysicsWorld.RemoveBodies(prop.PhysicsBodies);
                }
                _entities.Remove(entity);
                string key = entity.Name.ToLowerInvariant();
                if (_entitiesByName.TryGetValue(key, out var e) && e == entity) _entitiesByName.Remove(key);
                foreach (var body in entity.Bodies) if (_bodies.TryGetValue(body.Name, out var b) && b == body) _bodies.Remove(body.Name);
                foreach (var light in entity.GameObject.GetComponentsInChildren<AmnesiaLight>(true))
                {
                    _lightList.Remove(light);
                    if (_lights.TryGetValue(light.name, out var l) && l == light) _lights.Remove(light.name);
                }
                if (Application.isPlaying) Destroy(entity.GameObject);
                else DestroyImmediate(entity.GameObject);
            }
        }

        /// <summary>
        /// cLuxMap::CreateEntity: a copy of the imported template of the .ent file (EntityTemplates), named, placed
        /// (Unity space) and loaded like a map entity. Null if the file has no template. Scaled copies: later.
        /// </summary>
        public HplEntity CreateEntity(string name, string file, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(file ?? "");
            Transform template = null;
            if (_templateRoot != null)
                foreach (Transform t in _templateRoot)
                    if (string.Equals(t.name, fileName, StringComparison.OrdinalIgnoreCase)) { template = t; break; }
            if (template == null)
            {
                HplLog.Warn($"CreateEntity: no template for '{file}' (re-import the level to build it)");
                return null;
            }
            if (scale != Vector3.one && (scale - Vector3.one).sqrMagnitude > 1e-6f)
                HplLog.Warn($"CreateEntity '{name}': scale {scale} is not applied to created entities yet");

            var go = Instantiate(template.gameObject, _entityRoot);
            go.name = name;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t != go.transform && t.name.StartsWith(template.name + "_", StringComparison.Ordinal))
                    t.name = name + t.name.Substring(template.name.Length);
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);

            foreach (var light in go.GetComponentsInChildren<AmnesiaLight>(true))
            {
                if (_lights.TryAdd(light.name, light)) _lightList.Add(light);
                light.InitRuntime();
            }
            ConnectBillboards(go.transform);
            var prop = Props.PropFactory.Create(go.GetComponent<AmnesiaEntity>());
            if (prop == null) return null;
            prop.Load(this, PhysicsWorld);
            AddEntity(prop);
            return prop;
        }

        /// <summary>cLuxMapHandler::ChangeMap: map switching comes with the next maps (logged).</summary>
        public void ChangeMap(string mapFile, string startPos)
        {
            HplLog.Warn($"ChangeMap('{mapFile}', '{startPos}'): skipped, needs map switching");
        }

        // ------------------------------------------------------------------ script calls

        /// <summary>Runs a script function by name (cLuxMap::RunScript with the call text HPL2 builds).</summary>
        public void RunScript(string function, params object[] args)
        {
            if (Script == null || !runScript) return;
            Script.Invoke(function, args);
        }

        // ------------------------------------------------------------------ lookups

        /// <summary>cLuxMap::GetEntityByName: any case.</summary>
        public HplEntity GetEntityByName(string name) =>
            name != null && _entitiesByName.TryGetValue(name.ToLowerInvariant(), out var e) ? e : null;

        public AmnesiaArea GetPlayerStart(string name)
        {
            if (name != null && _playerStarts.TryGetValue(name, out var area)) return area;
            HplLog.Warn($"Could not find player start node '{name}'");
            return null;
        }

        /// <summary>cLuxMap::GetFirstPlayerStart: the node map is sorted by name.</summary>
        public AmnesiaArea GetFirstPlayerStart()
        {
            AmnesiaArea first = null;
            foreach (var a in _playerStartList)
                if (first == null || string.CompareOrdinal(a.name, first.name) < 0) first = a;
            return first;
        }

        public HplBody GetBody(string name) => name != null && _bodies.TryGetValue(name, out var b) ? b : null;

        public AmnesiaLight GetLight(string name) => name != null && _lights.TryGetValue(name, out var l) ? l : null;

        // ------------------------------------------------------------------ timers (cLuxMap)

        public void AddTimer(string name, float time, string function)
        {
            // Not allowed 0 or lower for time.
            _timers.Add(new Timer { name = name, count = time > 0f ? time : 0.001f, function = function });
        }

        public void RemoveTimer(string name)
        {
            for (int i = 0; i < _timers.Count;)
            {
                if (_timers[i].name != name) { i++; continue; }
                if (_updatingTimers)
                {
                    _timers[i].destroyMe = true;
                    i++;
                }
                else _timers.RemoveAt(i);
            }
        }

        public float GetTimerTimeLeft(string name)
        {
            var timer = _timers.Find(t => t.name == name);
            return timer?.count ?? 0f;
        }

        /// <summary>
        /// cLuxMap::UpdateTimers: all counts go down first (a timer is never called in the step it was created),
        /// then expired timers run their function with their name and are removed.
        /// </summary>
        void UpdateTimers(float timeStep)
        {
            _updatingTimers = true;
            foreach (var t in _timers) t.count -= timeStep;

            for (int i = 0; i < _timers.Count;)
            {
                var t = _timers[i];
                if (t.count <= 0f && !t.destroyMe)
                {
                    RunScript(t.function, t.name);
                    // Callbacks only append or flag timers while updating, so t is still at i.
                    _timers.RemoveAt(i);
                }
                else i++;
            }
            _timers.RemoveAll(t => t.destroyMe);
            _updatingTimers = false;
        }

        public int TimerCount => _timers.Count;

        // ------------------------------------------------------------------ local variables

        /// <summary>cLuxMap::GetVar: created empty on first use.</summary>
        public string GetVar(string name) => _vars.TryGetValue(name, out string v) ? v : _vars[name] = "";

        public void SetVar(string name, string value) => _vars[name] = value;

        // ------------------------------------------------------------------ fog (cWorld)

        public bool FogActive { get; private set; }
        public Color FogColor { get; private set; } = Color.white;
        public float FogStart { get; private set; }
        public float FogEnd { get; private set; } = 40f;
        public float FogFalloffExp { get; private set; } = 1f;

        void InitFog()
        {
            if (_level != null)
            {
                FogActive = _level.fogActive;
                FogColor = _level.fogColor;
                FogStart = _level.fogStart;
                FogEnd = _level.fogEnd;
                FogFalloffExp = _level.fogFalloffExp;
            }
            var volume = GetComponentInChildren<Volume>(true);
            // Only in play mode: Volume.profile copies the shared profile there, edits must not reach the asset.
            if (volume != null && Application.isPlaying && volume.profile.TryGet(out Fog fog)) _fog = fog;
        }

        public void SetFog(bool? active = null, Color? color = null, float? start = null, float? end = null, float? falloffExp = null)
        {
            FogActive = active ?? FogActive;
            FogColor = color ?? FogColor;
            FogStart = start ?? FogStart;
            FogEnd = end ?? FogEnd;
            FogFalloffExp = falloffExp ?? FogFalloffExp;
            if (_fog != null) HplLighting.ApplyFog(_fog, FogActive, FogColor, FogEnd);
        }

        /// <summary>The player as a collide callback owner: one body, the current character shape.</summary>
        sealed class PlayerBodies : IHplBodies
        {
            readonly AmnesiaPlayer _player;
            readonly Collider[] _shape = new Collider[1];

            public PlayerBodies(AmnesiaPlayer player) => _player = player;

            public int BodyCount => 1;

            public IReadOnlyList<Collider> GetBodyColliders(int index)
            {
                var body = _player.Body;
                _shape[0] = body != null && body.Shapes.Count > 0 ? body.Shapes[body.ActiveSize] : null;
                return _shape;
            }
        }
    }
}
