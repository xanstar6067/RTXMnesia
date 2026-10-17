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
        public HplConfig initConfig, preMenuConfig;
        public HplCampaign Campaign => HplCampaign.ForMap(mapFile);
        public HplConfig menuConfig;
        [Tooltip("config/default_user_keys.cfg: key names in messages and hints ($ButJournal).")]
        public HplConfig keysConfig;
        public HplLanguage language;
        [Tooltip("Decorative main-menu world: no player, level script or in-game pause UI.")]
        public bool menuBackground;
        public Camera backgroundCamera;
        public AmnesiaPlayer player;
        public HplEffects effects;
        public HplHud hud;
        public Sound.HplSoundHandler sound;
        [Tooltip("materials.cfg: physics materials and surface data.")]
        public HplSurfaces surfaces;
        [Tooltip("All particle systems (.ps), see ParticleConverter.")]
        public Particles.HplParticleBank particles;

        [Tooltip("ScriptDebugOn() in the level script (the original skips intros and runs test code).")]
        public bool scriptDebug;
        [Tooltip("Show AddDebugMessage calls in the console (HPL2 shows them only in debug mode).")]
        public bool logDebugMessages;
        [Tooltip("Run the level script. Off: the level without its events.")]
        public bool runScript = true;

        public static HplMap Current { get; private set; }

        public HplScript Script { get; private set; }
        public Dynamics.HplPhysicsWorld PhysicsWorld { get; private set; }
        public Particles.HplParticleWorld ParticleWorld { get; private set; }
        public HplGameUi GameUi { get; private set; }
        public bool IsMenuPaused => (GameUi != null && GameUi.IsPaused) || HplLoadScreen.Active || HplCredits.Active;
        /// <summary>cLuxMapHandler map change: fading out to change map, or left (frozen while the next map loads).</summary>
        public bool IsChangingMap => _mapChange.active || _leaving;
        /// <summary>cLuxMap::GetName: the map file name without extension, lower case.</summary>
        public string MapName => HplMapChange.FileToMapName(mapFile);
        public float GameTime { get; private set; }
        public Camera PlayerCamera => player != null ? player.playerCamera : backgroundCamera;
        public readonly HplCollideCallbacks PlayerCollideCallbacks = new();

        [NonSerialized] public string DisplayNameEntry = "";
        [NonSerialized] public int NumberOfQuests;
        /// <summary>cLuxMap::mlTotalCompletionAmount / mlCurrentCompletionAmount.</summary>
        [NonSerialized] public int TotalCompletionAmount, CurrentCompletionAmount;
        [NonSerialized] public string LanternLitCallback = "";
        // cLuxMap check point (SetCheckPoint / LoadCheckPoint): where death puts the player back.
        [NonSerialized] public string CheckPointName = "", CheckPointStartPos = "", CheckPointCallback = "", CheckPointMusic = "";
        [NonSerialized] public int CheckPointCount, CheckPointMusicPrio;
        [NonSerialized] public bool CheckPointMusicResume;
        [NonSerialized] public float CheckPointMusicVolume;

        sealed class Timer
        {
            public string name, function;
            public float count;
            public bool destroyMe;
        }

        readonly List<HplEntity> _entities = new();
        readonly List<RTXMnesia.Enemies.HplEnemy> _enemies = new();
        readonly List<HplProp> _animatedProps = new();

        /// <summary>A prop with mesh animations: posed after the physics like the enemies (cWorld::UpdateEntities).</summary>
        internal void RegisterAnimatedProp(HplProp prop)
        {
            if (!_animatedProps.Contains(prop)) _animatedProps.Add(prop);
        }
        // cWorld temp AI nodes (PathNode areas: name, id, HPL2 position) and the node containers by name.
        readonly List<(string name, int id, Vector3 hplPos)> _pathNodes = new();
        readonly Dictionary<string, RTXMnesia.Enemies.AINodeContainer> _nodeContainers = new(StringComparer.Ordinal);
        readonly Dictionary<string, HplEntity> _entitiesByName = new(StringComparer.Ordinal);
        readonly Dictionary<string, AmnesiaArea> _playerStarts = new(StringComparer.Ordinal);
        readonly List<AmnesiaArea> _playerStartList = new();
        readonly Dictionary<string, AmnesiaArea> _posNodes = new(StringComparer.Ordinal);
        readonly List<HplRope> _ropes = new();
        readonly List<HplStickyArea> _stickyAreas = new();
        readonly Dictionary<string, HplBody> _bodies = new(StringComparer.Ordinal);
        readonly Dictionary<string, AmnesiaLight> _lights = new(StringComparer.Ordinal);
        readonly List<AmnesiaLight> _lightList = new();
        readonly List<Timer> _timers = new();
        readonly Dictionary<string, string> _vars = new(StringComparer.Ordinal);
        bool _updatingTimers, _built, _started;

        // cLuxMapHandler::mMapChangeData
        struct MapChangeData
        {
            public bool active;
            public string mapFile, startPos, sound;
        }

        MapChangeData _mapChange;
        bool _leaving;
        HplMapChange.Arrival _arrival;
        RTXMnesia.Save.SaveGame _loadGame;

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

        void OnDestroy()
        {
            ParticleWorld?.DestroyAll();
            foreach (var dissolve in _dissolveEntities) dissolve.Destroy();
            _dissolveEntities.Clear();
            Sound.HplSoundEntity.Started -= OnSoundEntityStarted;
            foreach (var enemy in _enemies) enemy.DestroyQueryShape();
            foreach (var rope in _ropes) rope.DestroyGraphics();
            Dynamics.HplBuoyancy.ClearCache();
        }

        void Start() => Begin();

        // Play mode: Unity simulates after FixedUpdate (needed for Rigidbody interpolation); the part of the step
        // after the simulation runs when the physics step is done (WaitForFixedUpdate).
        void FixedUpdate()
        {
            if (!Application.isPlaying || IsMenuPaused) return;
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
            if (!menuBackground && player == null) player = FindAnyObjectByType<AmnesiaPlayer>(FindObjectsInactive.Include);
            // A map change loaded this scene: the player starts at the asked position (AmnesiaPlayer.Start).
            if (!menuBackground && Application.isPlaying) _arrival = HplMapChange.Take(mapFile);
            if (_arrival != null && player != null) player.startArea = _arrival.StartPos;
            // A save is being loaded into this map (cLuxSaveHandler::LoadSaveGameData).
            if (!menuBackground && _arrival == null) _loadGame = RTXMnesia.Save.HplSaveHandler.TakePending(mapFile);
            if (effects == null && !TryGetComponent(out effects)) effects = gameObject.AddComponent<HplEffects>();
            if (hud == null && !TryGetComponent(out hud)) hud = gameObject.AddComponent<HplHud>();
            HplKeyBindings.SetDefaultsConfig(keysConfig);
            effects.Init(this);
            hud.Init(this);
            InitSound();
            if (player != null)
            {
                player.steppedByMap = true;
                _playerBodies = new PlayerBodies(player);
                _checkPlayerCallbacks = () => PlayerCollideCallbacks.Check("Player", _playerBodies, this);
            }

            ParticleWorld = new Particles.HplParticleWorld(particles);
            if (particles == null) HplLog.Warn("No particle bank: re-import the level to get particle systems");
            PhysicsWorld = new Dynamics.HplPhysicsWorld();
            PhysicsWorld.SurfaceSounds.Sound = sound;
            PhysicsWorld.SurfaceSounds.Surfaces = surfaces;
            PhysicsWorld.SurfaceSounds.Particles = ParticleWorld;
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
                        // cLuxAreaNodeLoader_PathNode: cWorld::AddAINode(name, id, "Default", position + 5 cm up).
                        _pathNodes.Add((area.name, area.hplId, HplSpace.Position(area.transform.position + new Vector3(0f, 0.05f, 0f))));
                        break;
                    case "PosNode":
                        // cLuxAreaNodeLoader_PosNode: a named position (rope ends, script positions).
                        _posNodes.TryAdd(area.name, area);
                        break;
                    case "Rope":
                        // cLuxAreaRopeLoader: the rope entity (its physics rope after the world is loaded).
                        var rope = new HplRope(area);
                        AddEntity(rope);
                        _ropes.Add(rope);
                        break;
                    default:
                        AddEntity(new HplArea(area, gameConfig, surfaces));
                        break;
                }
            }
            foreach (var light in GetComponentsInChildren<AmnesiaLight>(true))
            {
                if (IsTemplate(light.transform)) continue;
                if (_lights.TryAdd(light.name, light)) _lightList.Add(light);
                light.sound = sound;
                light.particles = ParticleWorld;
                light.InitRuntime();
            }
            ConnectBillboards(transform);
            CreateSoundEntities(transform);
            CreateParticleSystems(transform);
            foreach (var entity in GetComponentsInChildren<AmnesiaEntity>(true))
            {
                if (IsTemplate(entity.transform)) continue;
                if (entity.entityType == "Enemy_Grunt" || entity.entityType == "Enemy_WaterLurker")
                {
                    // iLuxEnemyLoader (LuxBase.cpp: Enemy_Grunt, Enemy_WaterLurker). The man pig (Enemy_ManPig) has
                    // no entity in The Dark Descent's data.
                    RTXMnesia.Enemies.HplEnemy enemy = entity.entityType == "Enemy_Grunt"
                        ? new RTXMnesia.Enemies.EnemyGrunt(entity)
                        : new RTXMnesia.Enemies.EnemyWaterLurker(entity);
                    enemy.Load(this);
                    AddEntity(enemy);
                    _enemies.Add(enemy);
                    continue;
                }
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
            SetCharacterShapes();
            // iLuxEntity::AfterWorldLoad: the enemies' node containers, the ropes.
            foreach (var enemy in _enemies) enemy.AfterWorldLoad();
            foreach (var rope in _ropes) rope.AfterWorldLoad(transform);

            InitFog();

            var scriptType = menuBackground ? null : HplScript.FindScriptType(mapFile);
            if (scriptType != null)
            {
                Script = (HplScript)Activator.CreateInstance(scriptType);
                Script.Map = this;
            }
            else if (!menuBackground)
            {
                HplLog.Warn($"No level script for '{mapFile}' (class with [HplMapScript(\"{mapFile}\")])");
            }

            // Runtime-only: existing imported levels get the UI without regenerating game content.
            if (Application.isPlaying && !menuBackground)
            {
                GameUi = gameObject.AddComponent<HplGameUi>();
                GameUi.Init(this);
            }

            // The world reflection of water surfaces (a mirror camera, rendered only in Play mode).
            if (Application.isPlaying)
            {
                var renderers = new List<MeshRenderer>();
                foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
                    if (!IsTemplate(r.transform)) renderers.Add(r);
                WaterReflection = HplWaterReflection.Create(renderers, transform);
            }
        }

        /// <summary>The water world reflection of this map (null: no such water or not in Play mode).</summary>
        public HplWaterReflection WaterReflection { get; private set; }

        void AddEntity(HplEntity entity)
        {
            entity.Map = this;
            _entities.Add(entity);
            // cLuxMap::AddEntity: names are looked up in lower case, the first entity of a name wins.
            _entitiesByName.TryAdd(entity.Name.ToLowerInvariant(), entity);
            foreach (var body in entity.Bodies) _bodies.TryAdd(body.Name, body);
            if (entity is HplArea { Sticky: { } sticky }) _stickyAreas.Add(sticky);
        }

        /// <summary>cLuxMapHandler::SetCurrentMap: the player is at its start, then OnStart (first visit) and OnEnter run.</summary>
        public void Begin()
        {
            Build();
            if (_started) return;
            _started = true;
            SetCharacterShapes();
            player?.Hands?.SetWorld(sound, ParticleWorld);
            // cMapHandlerSoundCallback: started sound entities can be heard by enemies.
            Sound.HplSoundEntity.Started += OnSoundEntityStarted;
            if (player != null && player.LookAt != null)
            {
                player.LookAt.AtTarget += function => RunScript(function);
                player.SanityGained += () => effects.SanityGainFlash.Start();
                // cLuxPlayer::GiveSanityDamage: hint, then the hit effect (cLuxPlayerSanity, step 6).
                player.SanityHit += () => hud.Hints.Add("SanityHit", hud.Translate("Hints", "SanityHit"), 0f);
            }
            if (_arrival != null)
            {
                Arrive(_arrival);
                return;
            }
            if (_loadGame != null)
            {
                LoadGame(_loadGame);
                _loadGame = null;
                return;
            }
            foreach (var enemy in _enemies) enemy.OnMapEnter();
            // New game / a map opened directly: cLuxMapHandler::SetCurrentMap (first time) + OnStart / OnEnter.
            if (player != null && !Campaign.IsJustine) SetCheckPoint("_auto", player.startArea, "");
            if (!runScript || Script == null) return;
            Script.Run(Script.OnStart, "OnStart");
            CalculateTotalCompletionAmount();
            Script.Run(Script.OnEnter, "OnEnter");
        }

        /// <summary>
        /// cLuxMap::AddCompletionAmount: the completion of the map grows (the counter shows the increase after the
        /// delay).
        /// </summary>
        public void AddCompletionAmount(int amount, float delay = 0f)
        {
            float previous = TotalCompletionAmount == 0 ? 0f : (float)CurrentCompletionAmount / TotalCompletionAmount;
            CurrentCompletionAmount += amount;
            float next = TotalCompletionAmount == 0 ? 0f : (float)CurrentCompletionAmount / TotalCompletionAmount;
            hud?.Completion?.ShowCompletionIncrease(previous, next, delay);
        }

        /// <summary>
        /// cLuxMap::CalculateTotalCompletionAmount, after OnStart of the first visit: the map's quests (from
        /// SetNumberOfQuestsInMap), puzzle items, notes, diaries, chests (none in the game) and flashback areas.
        /// </summary>
        void CalculateTotalCompletionAmount()
        {
            var count = hud != null ? hud.Completion : null;
            if (count == null) return;
            TotalCompletionAmount += NumberOfQuests * count.QuestValue;
            foreach (var entity in _entities)
            {
                if (entity is Props.PropItem item)
                {
                    if (item.ItemType == "Puzzle") TotalCompletionAmount += count.ItemValue;
                    if (item.ItemType == "Note") TotalCompletionAmount += count.NoteValue;
                    if (item.ItemType == "Diary") TotalCompletionAmount += count.DiaryValue;
                }
                else if (entity is HplArea area && area.AreaType == "Flashback") TotalCompletionAmount += count.FlashbackValue;
            }
            if (logDebugMessages) HplLog.Info("Total completion value: " + TotalCompletionAmount);
        }

        /// <summary>
        /// The new map part of cLuxMapHandler::CheckMapChange after loading: the engine state of the left map
        /// (player, effects, music) goes on here, the player is placed (SetCurrentMap), the saved state of an
        /// earlier visit is loaded, OnStart runs on the first visit and OnEnter always; then the load screen
        /// keeps the map paused until its text was read (GameScreenLoadDone).
        /// </summary>
        void Arrive(HplMapChange.Arrival arrival)
        {
            sound.AdoptGameSound(arrival.Music, arrival.GlobalVolume, arrival.GlobalSpeed);
            if (HplLoadScreen.Current != null) HplLoadScreen.Current.Music = null;
            sound.Music.OnMapEnter();
            if (arrival.Effects != null) effects.ApplyCarry(arrival.Effects);
            if (player != null && arrival.Player != null) player.ApplyCarry(arrival.Player);
            PlacePlayerAtStartPos(arrival.StartPos);
            foreach (var enemy in _enemies) enemy.OnMapEnter();
            if (!Campaign.IsJustine) SetCheckPoint("_auto", arrival.StartPos, "");

            bool firstTime = !HplSavedMaps.MapExists(MapName);
            HplSavedMaps.LoadMap(this);
            if (runScript && Script != null)
            {
                if (firstTime)
                {
                    Script.Run(Script.OnStart, "OnStart");
                    CalculateTotalCompletionAmount();
                }
                Script.Run(Script.OnEnter, "OnEnter");
            }
            float loadTime = Time.realtimeSinceStartup - arrival.LoadStartTime;
            if (HplLoadScreen.Current != null) HplLoadScreen.Current.LoadDone(this, arrival.EndSound, loadTime);
            else sound.PlayGuiSoundData(arrival.EndSound, Sound.SoundEntryType.Gui);
            Debug.Log($"[RTXMnesia] Entering map {MapName} ({(firstTime ? "first visit" : "saved state")}), loaded in {loadTime:0.00} s");
        }

        /// <summary>
        /// cLuxSaveHandler::LoadSaveGameData into this freshly loaded map: the entities and modules take the saved state;
        /// OnStart / OnEnter do not run (the check point, timers and variables are the saved ones).
        /// </summary>
        void LoadGame(RTXMnesia.Save.SaveGame save)
        {
            foreach (var enemy in _enemies) enemy.OnMapEnter();
            RTXMnesia.Save.HplSaveGame.Load(this, save);
            Debug.Log($"[RTXMnesia] Loaded saved game of map {MapName} ({save.date})");
        }

        /// <summary>
        /// cLuxMap::SetCheckPoint: a new name only; remembers the highest priority looping music (its volume without
        /// the music handler's volume multiplier).
        /// </summary>
        public void SetCheckPoint(string name, string startPos, string callback)
        {
            if (CheckPointName == name) return;
            CheckPointName = name;
            CheckPointStartPos = startPos;
            CheckPointCallback = callback;
            CheckPointCount = 0;

            CheckPointMusic = "";
            var music = sound != null ? sound.Music : null;
            if (music != null)
            {
                for (int i = music.GetMaxPrio(); i >= 0; i--)
                {
                    var m = music.GetMusic(i);
                    // mbSpecialEffect: no script call plays special effect music, it is always false here.
                    if (m.file == "" || !m.loop) continue;
                    CheckPointMusic = m.file;
                    CheckPointMusicPrio = i;
                    CheckPointMusicResume = m.resume;
                    CheckPointMusicVolume = m.volume;
                    if (music.GameVolumeMul > 0f) CheckPointMusicVolume /= music.GameVolumeMul;
                    break;
                }
            }
            if (logDebugMessages) Debug.Log($"[Script debug] Setting check point {CheckPointName} Music: '{CheckPointMusic}'");
        }

        /// <summary>
        /// cLuxMap::LoadCheckPoint (after death): the player at the check point start, a short fade in, the check
        /// point music again, then the callback (name, count) as the last thing.
        /// </summary>
        public void LoadCheckPoint()
        {
            if (logDebugMessages) Debug.Log($"[Script debug] Loading check point {CheckPointName} Count: {CheckPointCount}");
            PlacePlayerAtStartPos(CheckPointStartPos);
            foreach (var enemy in _enemies)
            {
                enemy.SetActive(false);
                enemy.ResetProperties();
            }
            player?.ClearTerrorEnemies();
            effects?.Fade.FadeIn(0.2f);
            var music = sound != null ? sound.Music : null;
            if (music != null)
            {
                music.Reset();
                if (CheckPointMusic != "")
                    music.Play(CheckPointMusic, true, CheckPointMusicVolume, 1f, CheckPointMusicPrio, CheckPointMusicResume);
            }
            RunScript(CheckPointCallback, CheckPointName, CheckPointCount);
            CheckPointCount++;
        }

        /// <summary>cLuxMap::PlacePlayerAtStartPos: an unknown position is an error, then the first start.</summary>
        public void PlacePlayerAtStartPos(string name)
        {
            if (player == null) return;
            if (name == null || !_playerStarts.TryGetValue(name, out var node))
            {
                HplLog.Error($"StartPos '{name}' does not exist!");
                node = GetFirstPlayerStart();
            }
            if (node == null) return;
            player.startArea = node.name;
            player.PlaceAtStart(node);
        }

        /// <summary>One HPL2 logic step with an explicit physics simulation (edit mode tools, script simulation mode).</summary>
        public void Step(float timeStep)
        {
            if (IsMenuPaused) return;
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
            if (IsMenuPaused) return;
            Begin();

            // cLuxMapHandler::Update: the effect renderer's flash list is rebuilt by this update.
            effects.Renderer.ClearRenderLists();

            // cLuxMap::Update, only while no map change is active.
            bool updateMap = CheckMapChange();
            if (_leaving) return;
            if (updateMap)
            {
                HplGame.Update(timeStep);
                UpdateDissolveEntities(timeStep);
                UpdateTimers(timeStep);
                for (int i = 0; i < _entities.Count; i++)
                    if (_entities[i].Active) _entities[i].UpdateLogic(timeStep);
            }

            // cLuxPlayer::Update (its collide callbacks before the camera effects, the body moves later)
            if (player != null) player.StepLogic(timeStep, _checkPlayerCallbacks);
            // cLuxInsanityHandler::Update (the module after the player).
            if (!menuBackground) Effects.HplInsanity.Update(this, timeStep);

            hud.Step(timeStep);
            effects.Step(timeStep);

            // PostUpdate message: cScene::PostUpdate → cWorld::Update → iPhysicsWorld::Update.
            if (player != null) player.StepPhysics(timeStep);
            for (int i = 0; i < _enemies.Count; i++) _enemies[i].StepBody(timeStep);
            PhysicsWorld.BeforeSimulate(timeStep);
        }

        /// <summary>After the simulation: joints and bodies, lights, the player's post update, destroyed entities.</summary>
        public void StepAfterSimulate(float timeStep)
        {
            if (IsMenuPaused || _leaving) return;
            PhysicsWorld.AfterSimulate(timeStep);
            for (int i = 0; i < _enemies.Count; i++) _enemies[i].StepMesh(timeStep);
            for (int i = 0; i < _animatedProps.Count; i++) _animatedProps[i].StepMesh(timeStep);
            ParticleWorld.Update(timeStep);
            for (int i = 0; i < _lightList.Count; i++) _lightList[i].UpdateLight(timeStep);
            sound.Step(timeStep);
            if (player != null) player.StepPostUpdate(timeStep);
            DestroyPendingEntities();
            GameTime += timeStep;
        }

        // ------------------------------------------------------------------ entity creation / destruction

        readonly List<HplEntity> _destroy = new();
        readonly List<HplDissolveEntity> _dissolveEntities = new();
        Transform _dissolveRoot;

        public IReadOnlyList<HplDissolveEntity> DissolveEntities => _dissolveEntities;

        /// <summary>
        /// cLuxMap::AddDissolveEntity: a copy of the model under <paramref name="source"/> at its current pose fades out
        /// in <paramref name="time"/> s (not saved, as in HPL2).
        /// </summary>
        public void AddDissolveEntity(Transform source, float time)
        {
            if (source == null) return;
            if (_dissolveRoot == null)
            {
                _dissolveRoot = new GameObject("HplDissolve (runtime)") { hideFlags = HideFlags.DontSave }.transform;
                _dissolveRoot.SetParent(transform, false);
            }
            _dissolveEntities.Add(new HplDissolveEntity(source, time, _dissolveRoot));
        }

        /// <summary>cLuxMap::UpdateDissolveEntities (before the timers).</summary>
        void UpdateDissolveEntities(float timeStep)
        {
            for (int i = 0; i < _dissolveEntities.Count; )
            {
                if (_dissolveEntities[i].Update(timeStep)) i++;
                else _dissolveEntities.RemoveAt(i);
            }
        }
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

        // ------------------------------------------------------------------ sound

        readonly Dictionary<AmnesiaSound, Sound.HplSoundEntity> _placedSounds = new();

        void InitSound()
        {
            if (sound == null && !TryGetComponent(out sound)) sound = gameObject.AddComponent<Sound.HplSoundHandler>();
            sound.EnsureMusic();
            // cLuxMusicHandler: the music volume of game.cfg.
            if (gameConfig != null) sound.Music.GameVolumeMul = gameConfig.GetFloat("Sound", "InGameMusicVolume", 1f);
            if (player != null)
            {
                player.sound = sound;
                player.surfaces = surfaces;
            }
            hud.Messages.Sound = sound;
            if (PlayerCamera != null)
            {
                sound.listener = PlayerCamera.transform;
                if (Application.isPlaying && FindAnyObjectByType<AudioListener>() == null)
                    PlayerCamera.gameObject.AddComponent<AudioListener>();
            }
            if (sound.bank == null) HplLog.Warn("No sound bank: re-import the level to get sounds");
        }

        /// <summary>
        /// cEngineFileLoading::LoadSound for the sound entities of the map and of .ent files (ModelData/Entities):
        /// they follow their node (an entity's sounds move with its body). UseDefault = false takes the distances
        /// and volume of the instance. An inactive one does not update (and so does not start).
        /// </summary>
        void CreateSoundEntities(Transform root)
        {
            foreach (var placed in root.GetComponentsInChildren<AmnesiaSound>(true))
            {
                if (IsTemplate(placed.transform) || _placedSounds.ContainsKey(placed)) continue;
                var s = sound.CreateSoundEntity(placed.name, placed.soundEntityFile, false);
                if (s == null) continue;
                s.Parent = placed.transform;
                s.Active = placed.gameObject.activeInHierarchy;
                if (!placed.useDefault)
                {
                    s.MinDistance = placed.minDistance;
                    s.MaxDistance = placed.maxDistance;
                    s.Volume = placed.volume;
                }
                _placedSounds[placed] = s;
            }
        }

        /// <summary>The sound entities placed under a node (an entity's own sounds), e.g. for its effects.</summary>
        public IEnumerable<Sound.HplSoundEntity> SoundEntitiesUnder(Transform root)
        {
            foreach (var pair in _placedSounds)
                if (pair.Key != null && pair.Key.transform.IsChildOf(root)) yield return pair.Value;
        }

        /// <summary>
        /// Before an entity's objects go: its own sound entities are destroyed; sounds that were only attached to
        /// it (PlaySoundAtEntity, a break sound) stay where it was and play to the end.
        /// </summary>
        void ReleaseSounds(Transform root)
        {
            foreach (var placed in root.GetComponentsInChildren<AmnesiaSound>(true))
            {
                if (!_placedSounds.TryGetValue(placed, out var s)) continue;
                sound.DestroySoundEntity(s);
                _placedSounds.Remove(placed);
            }
            foreach (var s in sound.SoundEntities)
                if (s.Parent != null && s.Parent.IsChildOf(root)) s.Detach();
        }

        // ------------------------------------------------------------------ particle systems

        readonly Dictionary<AmnesiaParticleSystem, Particles.HplParticleSystem> _placedParticles = new();

        /// <summary>
        /// cEngineFileLoading::LoadParticleSystem for the particle systems of the map and of .ent files: they follow
        /// their node, with the instance's color and distance fade. One under an inactive node does not update.
        /// </summary>
        void CreateParticleSystems(Transform root)
        {
            foreach (var placed in root.GetComponentsInChildren<AmnesiaParticleSystem>(true))
            {
                if (IsTemplate(placed.transform) || _placedParticles.ContainsKey(placed)) continue;
                var ps = ParticleWorld.Create(placed.name, placed.file);
                if (ps == null) continue;
                ps.Parent = placed.transform;
                ps.Color = placed.color;
                ps.FadeAtDistance = placed.fadeAtDistance;
                ps.MinFadeDistanceStart = placed.minFadeDistance.x;
                ps.MinFadeDistanceEnd = placed.minFadeDistance.y;
                ps.MaxFadeDistanceStart = placed.maxFadeDistance.x;
                ps.MaxFadeDistanceEnd = placed.maxFadeDistance.y;
                ps.Active = placed.gameObject.activeInHierarchy;
                _placedParticles[placed] = ps;
            }
        }

        /// <summary>The particle systems placed under a node (an entity's own systems), as loaded.</summary>
        public List<Particles.HplParticleSystem> ParticleSystemsUnder(Transform root)
        {
            var list = new List<Particles.HplParticleSystem>();
            foreach (var placed in root.GetComponentsInChildren<AmnesiaParticleSystem>(true))
                if (_placedParticles.TryGetValue(placed, out var ps)) list.Add(ps);
            return list;
        }

        /// <summary>Before an entity's objects go: systems that follow it stay where they are (its own were killed).</summary>
        void ReleaseParticleSystems(Transform root)
        {
            foreach (var placed in root.GetComponentsInChildren<AmnesiaParticleSystem>(true)) _placedParticles.Remove(placed);
            foreach (var ps in ParticleWorld.Systems)
                if (ps.Parent != null && ps.Parent.IsChildOf(root)) ps.Detach();
        }

        /// <summary>cLuxMap::DestroyEntity: the entity goes after this step.</summary>
        public void DestroyEntity(HplEntity entity)
        {
            if (entity == null || _destroy.Contains(entity)) return;
            if (entity is HplProp prop) prop.DestroyMe = true;
            _destroy.Add(entity);
        }

        /// <summary>iLuxEntity::GetDestroyMe: destroyed in this step, not saved any more.</summary>
        public bool IsDestroyPending(HplEntity entity) => _destroy.Contains(entity);

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
                    prop.DestroyAttachments();
                    // iLuxProp::BeforeEntityDestruction: DissolveOnDestruction fades a copy of the model out.
                    if (prop.DissolveOnDestruction) AddDissolveEntity(prop.GameObject.transform, prop.DissolveTime);
                    prop.BeforeDestruction();
                    prop.KillParticleSystems();
                    foreach (var body in prop.PhysicsBodies)
                        foreach (var sticky in _stickyAreas) sticky.OnBodyRemoved(body);
                    PhysicsWorld.RemoveBodies(prop.PhysicsBodies);
                }
                if (entity is HplArea { Sticky: { } removedSticky }) _stickyAreas.Remove(removedSticky);
                if (entity is HplRope removedRope)
                {
                    // ~cLuxRope: the drawn and the physics rope go with it.
                    removedRope.DestroyGraphics();
                    PhysicsWorld.Ropes.Remove(removedRope.Physics);
                    _ropes.Remove(removedRope);
                }
                _entities.Remove(entity);
                if (entity is RTXMnesia.Enemies.HplEnemy removedEnemy) _enemies.Remove(removedEnemy);
                if (entity is HplProp removedProp) _animatedProps.Remove(removedProp);
                string key = entity.Name.ToLowerInvariant();
                if (_entitiesByName.TryGetValue(key, out var e) && e == entity) _entitiesByName.Remove(key);
                foreach (var body in entity.Bodies) if (_bodies.TryGetValue(body.Name, out var b) && b == body) _bodies.Remove(body.Name);
                foreach (var light in entity.GameObject.GetComponentsInChildren<AmnesiaLight>(true))
                {
                    _lightList.Remove(light);
                    if (_lights.TryGetValue(light.name, out var l) && l == light) _lights.Remove(light.name);
                }
                // Destroy waits for the end of the frame: off now, so the next physics step no longer sees its bodies
                // (the pieces of a broken pot would be pushed out of the whole one).
                ReleaseSounds(entity.GameObject.transform);
                ReleaseParticleSystems(entity.GameObject.transform);
                entity.GameObject.SetActive(false);
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
                light.sound = sound;
                light.particles = ParticleWorld;
                light.InitRuntime();
            }
            ConnectBillboards(go.transform);
            CreateSoundEntities(go.transform);
            CreateParticleSystems(go.transform);
            var prop = Props.PropFactory.Create(go.GetComponent<AmnesiaEntity>());
            if (prop == null) return null;
            prop.Load(this, PhysicsWorld);
            AddEntity(prop);
            return prop;
        }

        /// <summary>cLuxMapHandler::ChangeMap: the start sound, a 1.5 s fade out with the player inactive, then CheckMapChange.</summary>
        public void ChangeMap(string mapName, string startPos, string startSound, string endSound)
        {
            _mapChange = new MapChangeData
            {
                active = true,
                mapFile = System.IO.Path.ChangeExtension(mapName ?? "", "map"),
                startPos = startPos ?? "",
                sound = endSound ?? "",
            };
            sound.PlayGuiSoundData(startSound, Sound.SoundEntryType.Gui);
            effects.Fade.FadeOut(1.5f);
            if (player != null) player.active = false;
        }

        /// <summary>
        /// cLuxMapHandler::CheckMapChange, at the start of the map handler's update: once the fade out is done the
        /// screen fades in again (2 s) and the player is active. Another map: OnLeave, the map is saved, its world
        /// sounds fade out and the next map loads under the load screen (<see cref="HplMapChange"/>); this map stays
        /// frozen. The same map: the player only moves to the start position. False while the map must not update.
        /// </summary>
        bool CheckMapChange()
        {
            if (_leaving) return false;
            if (!_mapChange.active) return true;
            if (effects.Fade.IsFading) return false;

            _mapChange.active = false;
            effects.Fade.FadeIn(2f);
            if (player != null) player.active = true;

            string newName = HplMapChange.FileToMapName(_mapChange.mapFile);
            if (newName != MapName)
            {
                string scene = HplMapChange.FindScene(_mapChange.mapFile);
                if (scene == null)
                {
                    // "Could not load map" in HPL2; here a setup error: the map is not imported / not in the build.
                    Debug.LogError($"[RTXMnesia] Could not load map '{_mapChange.mapFile}': no scene in the build (import the map, then Import > Main menu and build scenes)");
                    return true;
                }
                // OnLeave runs before saving (SetCurrentMap does not run the script).
                if (runScript && Script != null) Script.Run(Script.OnLeave, "OnLeave");
                HplSavedMaps.SaveMap(this);
                sound.FadeOutAll(Sound.SoundEntryType.World, 0.5f, true);
                Effects.HplInsanity.OnMapLeave();
                _leaving = true;
                HplMapChange.Begin(this, scene, newName, _mapChange.startPos, _mapChange.sound);
                return false;
            }
            PlacePlayerAtStartPos(_mapChange.startPos);
            sound.PlayGuiSoundData(_mapChange.sound, Sound.SoundEntryType.Gui);
            return true;
        }

        /// <summary>The leaving part of CheckMapChange for <see cref="HplMapChange.Reenter"/> (tools).</summary>
        internal void LeaveForReentry(string scene, string startPos)
        {
            if (runScript && Script != null) Script.Run(Script.OnLeave, "OnLeave");
            HplSavedMaps.SaveMap(this);
            sound.FadeOutAll(Sound.SoundEntryType.World, 0.5f, true);
            Effects.HplInsanity.OnMapLeave();
            _leaving = true;
            HplMapChange.Begin(this, scene, MapName, startPos, "");
        }

        // ------------------------------------------------------------------ script calls

        /// <summary>Runs a script function by name (cLuxMap::RunScript with the call text HPL2 builds).</summary>
        public void RunScript(string function, params object[] args)
        {
            if (Script == null || !runScript) return;
            Script.Invoke(function, args);
        }

        // ------------------------------------------------------------------ enemies (cLuxMap)

        public IReadOnlyList<RTXMnesia.Enemies.HplEnemy> Enemies => _enemies;

        /// <summary>The character shapes of the player and the enemies: bodies with CollideCharacter = false skip them.</summary>
        void SetCharacterShapes()
        {
            var shapes = new List<Collider>();
            if (player != null && player.Body != null) shapes.AddRange(player.Body.Shapes);
            foreach (var enemy in _enemies) if (enemy.Body != null) shapes.AddRange(enemy.Body.Shapes);
            PhysicsWorld.SetCharacterShapes(shapes);
        }

        /// <summary>
        /// cWorld::CreateAINodeContainer: one container per name (the enemy file) with the map's path nodes; its
        /// connections from the map's .nodes file (&lt;map&gt;_&lt;name&gt;.nodes, newer than every shipped map), else compiled.
        /// Null with a warning when the map has no path nodes.
        /// </summary>
        public RTXMnesia.Enemies.AINodeContainer CreateAINodeContainer(string name, string nodeName, Vector3 size, bool nodeIsAtCenter,
                                                              int minEdges, int maxEdges, float maxEdgeDistance, float maxHeight)
        {
            if (_nodeContainers.TryGetValue(name, out var existing)) return existing;
            if (_pathNodes.Count == 0 || nodeName != "Default")
            {
                HplLog.Warn($"AI node type '{nodeName}' does not exist!");
                return null;
            }
            var container = new RTXMnesia.Enemies.AINodeContainer(name, nodeName, size)
            {
                MinEdges = minEdges, MaxEdges = maxEdges, MaxEdgeDistance = maxEdgeDistance, MaxHeight = maxHeight, NodeIsAtCenter = nodeIsAtCenter,
            };
            foreach (var (nodeName2, id, pos) in _pathNodes) container.AddNodeHpl(nodeName2, id, pos);
            string xml = RTXMnesia.Enemies.HplEnemyAssets.Instance != null ? RTXMnesia.Enemies.HplEnemyAssets.Instance.GetNodeFile(MapName, name) : null;
            if (xml != null) container.LoadFromXml(xml);
            else
            {
                Debug.Log($"[RTXMnesia] Rebuilding node connections of '{MapName}_{name}'");
                container.Compile();
            }
            _nodeContainers[name] = container;
            return container;
        }

        /// <summary>cLuxMap::BroadcastEnemyMessage: to active enemies (within the radius of the position if given).</summary>
        public void BroadcastEnemyMessage(RTXMnesia.Enemies.EnemyMessage type, bool hasPosition, Vector3 position, float radius,
                                          float time = 0f, bool localScope = false, Vector3 vector = default, float x = 0f, int ix = 0)
        {
            foreach (var enemy in _enemies.ToArray())
            {
                if (!enemy.Active) continue;
                if (hasPosition && Vector3.Distance(position, enemy.Body.Position) > radius + enemy.BodyRadius) continue;
                enemy.SendMessage(type, time, localScope, vector, x, ix);
            }
        }

        /// <summary>
        /// cLuxMap::BroadcastEnemySoundMessage: active enemies within the max distance (+ their radius) and at least 2 m
        /// away hear the sound at volume x (1 - (distance - min) / (max - min)), clamped.
        /// </summary>
        public void BroadcastEnemySoundMessage(Vector3 position, float volume, float minDist, float maxDist)
        {
            foreach (var enemy in _enemies.ToArray())
            {
                if (!enemy.Active) continue;
                Vector3 center = enemy.Body.Position;
                if (Vector3.Distance(position, center) > maxDist + enemy.BodyRadius) continue;
                float distance = Vector3.Distance(center, position);
                if (distance < 2f) continue;
                float t = (distance - minDist) / (maxDist - minDist);
                // cMath::Clamp lets NaN through (a sound entity passes min = max).
                if (t > 1f) t = 1f;
                else if (t < 0f) t = 0f;
                float hearVolume = (1f - t) * volume;
                enemy.SendMessage(RTXMnesia.Enemies.EnemyMessage.SoundHeard, 0f, false, position, hearVolume, 0);
            }
        }

        /// <summary>
        /// cMapHandlerSoundCallback::OnStart: a started sound entity whose .snt name begins with one of
        /// sounds/EnemySounds.dat is broadcast (with its max distance as both min and max distance, as in the original).
        /// </summary>
        void OnSoundEntityStarted(Sound.HplSoundEntity s)
        {
            if (sound == null || _enemies.Count == 0 || !sound.SoundEntityExists(s)) return;
            var assets = RTXMnesia.Enemies.HplEnemyAssets.Instance;
            if (assets == null) return;
            string type = s.Data.name ?? "";
            bool used = false;
            foreach (string prefix in assets.hearableSounds)
                if (type.StartsWith(prefix, StringComparison.Ordinal)) { used = true; break; }
            if (!used) return;
            BroadcastEnemySoundMessage(s.WorldPosition, s.Volume, s.MaxDistance, s.MaxDistance);
        }

        /// <summary>cLuxMap::AINodeIsUsedAsGoal: the first node of some enemy's path list.</summary>
        public bool AINodeIsUsedAsGoal(RTXMnesia.Enemies.AINode node)
        {
            foreach (var enemy in _enemies)
                if (enemy.Pathfinder != null && enemy.Pathfinder.PathFront == node) return true;
            return false;
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

        /// <summary>cLuxMap::GetPosNode.</summary>
        public AmnesiaArea GetPosNode(string name) => name != null && _posNodes.TryGetValue(name, out var n) ? n : null;

        public IReadOnlyList<HplRope> Ropes => _ropes;

        /// <summary>cLuxMap::BodyIsInDetachableStickyArea: the body is held by a sticky area that lets it go.</summary>
        public bool BodyIsInDetachableStickyArea(Dynamics.HplPhysicsBody body)
        {
            foreach (var sticky in _stickyAreas)
                if (sticky.AttachedBody == body) return sticky.CanDetach;
            return false;
        }

        /// <summary>cLuxMap::DetachBodyFromStickyArea: true if a sticky area holding the body let it go.</summary>
        public bool DetachBodyFromStickyArea(Dynamics.HplPhysicsBody body)
        {
            foreach (var sticky in _stickyAreas)
            {
                if (sticky.AttachedBody != body) continue;
                if (!sticky.CanDetach) return false;
                sticky.DetachBody();
                return true;
            }
            return false;
        }

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

        /// <summary>cLuxEventTimer list (map cache): name, function, time left.</summary>
        public List<(string name, string function, float count)> ExportTimers()
        {
            var list = new List<(string, string, float)>();
            foreach (var t in _timers) if (!t.destroyMe) list.Add((t.name, t.function, t.count));
            return list;
        }

        public void ImportTimers(List<(string name, string function, float count)> list)
        {
            _timers.Clear();
            foreach (var (name, function, count) in list) _timers.Add(new Timer { name = name, function = function, count = count });
        }

        // ------------------------------------------------------------------ map cache access

        /// <summary>The local variables (cLuxMap::m_mapVars).</summary>
        public Dictionary<string, string> Vars => _vars;

        /// <summary>Lights of the map itself (not of an entity: those are saved with their entity).</summary>
        public IEnumerable<AmnesiaLight> MapLights
        {
            get
            {
                foreach (var l in _lightList)
                    if (l != null && l.GetComponentInParent<AmnesiaEntity>(true) == null) yield return l;
            }
        }

        /// <summary>Sound entities placed in the map (not of an entity), by their scene object.</summary>
        public IEnumerable<(string name, Sound.HplSoundEntity sound)> PlacedMapSounds
        {
            get
            {
                foreach (var pair in _placedSounds)
                    if (pair.Key != null && pair.Key.GetComponentInParent<AmnesiaEntity>(true) == null) yield return (pair.Key.name, pair.Value);
            }
        }

        /// <summary>Particle systems placed in the map (not of an entity), by their scene object.</summary>
        public IEnumerable<(string name, Particles.HplParticleSystem ps)> PlacedMapParticleSystems
        {
            get
            {
                foreach (var pair in _placedParticles)
                    if (pair.Key != null && pair.Key.GetComponentInParent<AmnesiaEntity>(true) == null) yield return (pair.Key.name, pair.Value);
            }
        }

        public bool IsPlacedSound(Sound.HplSoundEntity sound) => _placedSounds.ContainsValue(sound);
        public bool IsPlacedParticleSystem(Particles.HplParticleSystem ps) => _placedParticles.ContainsValue(ps);

        /// <summary>cLuxMap::UpdateToBeDesotroyedEntities: entities destroyed while loading the cache go now.</summary>
        public void DestroyPendingEntitiesNow() => DestroyPendingEntities();

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
            HplLighting.SetMultiplyFog(FogActive, FogColor, FogEnd);
            HplLighting.SetTranslucentFog(FogActive, FogColor, FogStart, FogEnd, FogFalloffExp);
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
            HplLighting.SetTranslucentFog(FogActive, FogColor, FogStart, FogEnd, FogFalloffExp);
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
