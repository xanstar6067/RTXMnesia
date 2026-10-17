using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Insanity events (cLuxInsanityHandler, LuxInsanityHandler.cpp): one handler for the whole game, like the
    /// original module, so its sets, used events and waiting time survive map changes. Events come from
    /// <see cref="HplInsanityAssets"/> (game.cfg Insanity EventsFile). Insanity areas start a random one when
    /// <see cref="NewEventIsPossible"/>, scripts enable / disable sets and start / stop events. After an event the
    /// wait depends on the sanity (game.cfg Insanity MaxSanity_* / TimeBetween_*). Updated by the map after the player
    /// (module order of the "Default" updater container); drawn by the HUD.
    ///
    /// Kept as in HPL2: <see cref="Reset"/> (new game, check point load) neither clears the disabled sets nor the used
    /// flags; the random pick is RandRectl(0, unused) inclusive, so one draw in unused + 1 starts nothing.
    /// Not as in HPL2: each map is its own scene here, so on a map change the event's sound entry and particle system
    /// are gone with the old scene (GUI sounds stop on a scene change); a sound stream event then ends.
    /// </summary>
    public static class HplInsanity
    {
        static readonly List<InsanityEvent> _events = new();
        static readonly HashSet<string> _disabledSets = new(StringComparer.Ordinal);
        static bool _loaded;
        static float _maxSanityLongWait, _maxSanityMedWait, _maxSanityShortWait;
        static float _timeBetweenLongWait, _timeBetweenMedWait, _timeBetweenShortWait;

        public static float NewEventCount { get; private set; }
        public static int CurrentEventIndex { get; private set; } = -1;
        public static InsanityEvent CurrentEvent => CurrentEventIndex >= 0 ? _events[CurrentEventIndex] : null;
        public static IReadOnlyList<InsanityEvent> Events => _events;
        public static IEnumerable<string> DisabledSets => _disabledSets;

        /// <summary>The constructor: the wait times from game.cfg and the events file.</summary>
        static void EnsureLoaded(HplConfig gameConfig)
        {
            if (_loaded) return;
            _loaded = true;
            if (gameConfig != null)
            {
                _maxSanityLongWait = gameConfig.GetFloat("Insanity", "MaxSanity_LongWait", 0f);
                _maxSanityMedWait = gameConfig.GetFloat("Insanity", "MaxSanity_MedWait", 0f);
                _maxSanityShortWait = gameConfig.GetFloat("Insanity", "MaxSanity_ShortWait", 0f);
                _timeBetweenLongWait = gameConfig.GetFloat("Insanity", "TimeBetween_LongWait", 0f);
                _timeBetweenMedWait = gameConfig.GetFloat("Insanity", "TimeBetween_MedWait", 0f);
                _timeBetweenShortWait = gameConfig.GetFloat("Insanity", "TimeBetween_ShortWait", 0f);
            }
            LoadEvents();
            CurrentEventIndex = -1;
        }

        static void LoadEvents()
        {
            var assets = HplInsanityAssets.Instance;
            if (assets == null) return;
            foreach (var data in assets.events)
            {
                var e = EventTypeToData(data.type);
                if (e == null) continue;
                e.LoadData(data, assets);
                _events.Add(e);
            }
        }

        static InsanityEvent EventTypeToData(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "soundstream": return new InsanityEventSoundStream();
                case "steps": return new InsanityEventSteps();
                case "particles": return new InsanityEventParticles();
                case "bugs": return new InsanityEventBugs();
            }
            HplLog.Error($"Insanity event type '{type}' does not exist!");
            return null;
        }

        /// <summary>The map makes sure the handler exists (and has read game.cfg) when it starts.</summary>
        public static void Init(HplMap map) => EnsureLoaded(map != null ? map.gameConfig : null);

        /// <summary>cLuxInsanityHandler::Reset: the current event exits, no wait.</summary>
        public static void Reset()
        {
            if (CurrentEventIndex >= 0) _events[CurrentEventIndex].OnExit();
            CurrentEventIndex = -1;
            NewEventCount = 0f;
        }

        /// <summary>For tools and tests: as after a start of the game (sets enabled, nothing used, events reloaded).</summary>
        public static void ResetSession()
        {
            Reset();
            _disabledSets.Clear();
            _events.Clear();
            _loaded = false;
        }

        /// <summary>cLuxInsanityHandler::Update: the wait without an event, the current event and its end.</summary>
        public static void Update(HplMap map, float timeStep)
        {
            EnsureLoaded(map.gameConfig);
            if (NewEventCount > 0f && CurrentEventIndex < 0) NewEventCount -= timeStep;
            if (CurrentEventIndex < 0) return;

            var e = _events[CurrentEventIndex];
            e.Update(map, timeStep);
            if (!e.IsOver) return;
            e.OnExit();
            CurrentEventIndex = -1;

            // The time between events depends on the sanity.
            float sanity = map.player != null ? map.player.sanity : 100f;
            if (sanity <= _maxSanityShortWait) NewEventCount = _timeBetweenShortWait;
            else if (sanity <= _maxSanityMedWait) NewEventCount = _timeBetweenMedWait;
            else NewEventCount = _timeBetweenLongWait;
        }

        /// <summary>cLuxInsanityHandler::StartEvent(): a random unused event of an enabled set within the sanity.</summary>
        public static void StartEvent(HplMap map)
        {
            EnsureLoaded(map.gameConfig);
            if (_events.Count == 0) return;
            float sanity = map.player != null ? map.player.sanity : 100f;

            if (CurrentEventIndex >= 0) _events[CurrentEventIndex].OnExit();
            CurrentEventIndex = -1;

            int totalEnabled = 0, unused = 0;
            foreach (var e in _events)
            {
                bool disabled = SetIsDisabled(e.Set) || sanity > e.MaxSanity;
                if (!e.Used && !disabled) unused++;
                if (!disabled) totalEnabled++;
            }
            if (totalEnabled == 0) return;

            // All events used: reset.
            if (unused == 0)
            {
                foreach (var e in _events) e.Used = false;
                unused = totalEnabled;
            }

            // Iterate and count the available events (RandRectl is inclusive: the last draw finds none).
            int index = HplMath.RandRectl(0, unused);
            int current = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                var e = _events[i];
                if (SetIsDisabled(e.Set) || e.Used || sanity > e.MaxSanity) continue;
                if (current == index)
                {
                    CurrentEventIndex = i;
                    break;
                }
                current++;
            }
            if (CurrentEventIndex < 0) return;
            _events[CurrentEventIndex].Start(map);
        }

        /// <summary>cLuxInsanityHandler::StartEvent(index).</summary>
        public static void StartEvent(HplMap map, int index)
        {
            EnsureLoaded(map.gameConfig);
            if (CurrentEventIndex >= 0) _events[CurrentEventIndex].OnExit();
            CurrentEventIndex = index;
            _events[CurrentEventIndex].Start(map);
        }

        /// <summary>cLuxInsanityHandler::StartEvent(name): the first event of that name; unknown names do nothing.</summary>
        public static void StartEvent(HplMap map, string name)
        {
            EnsureLoaded(map.gameConfig);
            int index = _events.FindIndex(e => e.Name == name);
            if (index < 0) return;
            if (CurrentEventIndex >= 0) _events[CurrentEventIndex].OnExit();
            CurrentEventIndex = index;
            _events[CurrentEventIndex].Start(map);
        }

        public static void StopCurrentEvent()
        {
            if (CurrentEventIndex >= 0) _events[CurrentEventIndex].OnExit();
            CurrentEventIndex = -1;
        }

        /// <summary>cLuxInsanityHandler::NewEventIsPossible: no wait left, sanity ≤ MaxSanity_LongWait, no event.</summary>
        public static bool NewEventIsPossible(HplMap map)
        {
            EnsureLoaded(map.gameConfig);
            float sanity = map.player != null ? map.player.sanity : 100f;
            return NewEventCount <= 0f && sanity <= _maxSanityLongWait && CurrentEventIndex < 0;
        }

        public static void EnableSet(string set) => _disabledSets.Remove(set ?? "");
        public static void DisableSet(string set) => _disabledSets.Add(set ?? "");
        static bool SetIsDisabled(string set) => _disabledSets.Contains(set ?? "");

        /// <summary>The map is left: the event's scene objects (sound entry, particle system) go with the scene.</summary>
        public static void OnMapLeave()
        {
            if (CurrentEventIndex >= 0) _events[CurrentEventIndex].OnMapLeave();
        }

        /// <summary>cLuxInsanityHandler_SaveData::FromInsanityHandler: the wait, the used flags, the disabled sets.</summary>
        public static Save.InsanitySave CaptureSave()
        {
            var save = new Save.InsanitySave { newEventCount = NewEventCount };
            foreach (var e in _events) save.used.Add(e.Used);
            save.disabledSets.AddRange(_disabledSets);
            return save;
        }

        /// <summary>cLuxInsanityHandler_SaveData::ToInsanityHandler: the used flags only when the event count matches.</summary>
        public static void ApplySave(HplMap map, Save.InsanitySave save)
        {
            EnsureLoaded(map != null ? map.gameConfig : null);
            NewEventCount = save.newEventCount;
            if (save.used.Count == _events.Count)
                for (int i = 0; i < _events.Count; i++) _events[i].Used = save.used[i];
            _disabledSets.Clear();
            foreach (string set in save.disabledSets) _disabledSets.Add(set);
        }

        /// <summary>For the simulation log.</summary>
        public static string Describe() =>
            $"insanity event {(CurrentEvent != null ? CurrentEvent.Name : "-")} wait {NewEventCount:0.0} " +
            $"disabled [{string.Join(",", _disabledSets)}] used {_events.FindAll(e => e.Used).Count}/{_events.Count}";
    }

    /// <summary>iLuxInstanityEvent: name, set, MaxSanity; used / over flags.</summary>
    public abstract class InsanityEvent
    {
        public string Name { get; private set; } = "Unknown";
        public string Set { get; private set; } = "";
        public float MaxSanity { get; private set; } = 100f;
        public bool Used { get; set; }
        public bool IsOver { get; private set; } = true;

        internal void LoadData(HplInsanityAssets.EventData data, HplInsanityAssets assets)
        {
            Name = data.Get("Name") ?? "Unknown";
            Set = data.Get("Set") ?? "";
            MaxSanity = Float(data, "MaxSanity", 100f);
            OnLoadData(data, assets);
        }

        internal void Start(HplMap map)
        {
            Used = true;
            IsOver = false;
            if (map.logDebugMessages) Debug.Log($"[Script debug] Starting Insanity event: '{Name}'set: '{Set}'");
            OnStart(map);
        }

        protected void EventIsDone() => IsOver = true;

        protected abstract void OnLoadData(HplInsanityAssets.EventData data, HplInsanityAssets assets);
        protected abstract void OnStart(HplMap map);
        public abstract void OnExit();
        public virtual void Update(HplMap map, float timeStep) { }
        internal virtual void OnMapLeave() { }

        // cXmlElement::GetAttribute*: a missing attribute gives the default; numbers as atof / atoi.
        protected static float Float(HplInsanityAssets.EventData d, string key, float fallback)
        {
            string s = d.Get(key);
            return s == null ? fallback : HplMath.Atof(s);
        }

        protected static int Int(HplInsanityAssets.EventData d, string key, int fallback)
        {
            string s = d.Get(key);
            return s == null ? fallback : HplMath.Atoi(s);
        }

        protected static bool Bool(HplInsanityAssets.EventData d, string key, bool fallback)
        {
            string s = d.Get(key);
            return s == null ? fallback : s.ToLowerInvariant() == "true";
        }

        protected static string String(HplInsanityAssets.EventData d, string key, string fallback) => d.Get(key) ?? fallback;

        /// <summary>cString::ToColor: exactly four numbers, else the default.</summary>
        protected static Color ColorOf(HplInsanityAssets.EventData d, string key, Color fallback)
        {
            string s = d.Get(key);
            if (s == null) return fallback;
            var parts = s.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) return fallback;
            return new Color(HplMath.Atof(parts[0]), HplMath.Atof(parts[1]), HplMath.Atof(parts[2]), HplMath.Atof(parts[3]));
        }

        protected static bool Valid(HplMap map, SoundEntry entry, int id) => map != null && map.sound != null && map.sound.IsValid(entry, id);
    }

    /// <summary>
    /// cLuxInstanityEvent_Bugs: bugs crawling on the HUD from the screen edges around the center (wander + swarm
    /// point 400, 300), fading in, then out when the duration is over; a looping GUI stream.
    /// Fix: the events file writes "mfMaxSizeMul" for MaxSizeMul (HPL2 then uses 1, bugs never larger than 1×);
    /// the misspelled key is read too, so the three bug events reach their intended 1.3×.
    /// </summary>
    public sealed class InsanityEventBugs : InsanityEvent
    {
        public sealed class Bug
        {
            public bool active;
            public Vector2 pos, vel;
            public float sizeMul, angle, alpha;
        }

        int _numOfBugs;
        string _loopSound;
        float _soundVolume, _duration, _minSizeMul, _maxSizeMul, _wanderCircleDist, _wanderCircleRadius, _swarmPointMul, _maxSpeed;
        public Texture2D BugImage { get; private set; }
        public readonly List<Bug> Bugs = new();
        SoundEntry _soundEntry;
        int _soundEntryId;
        float _timeCount;

        protected override void OnLoadData(HplInsanityAssets.EventData d, HplInsanityAssets assets)
        {
            string image = String(d, "BugImage", "");
            if (image != "") BugImage = assets.GetImage(image);
            _numOfBugs = Int(d, "NumOfBugs", 10);
            _minSizeMul = Float(d, "MinSizeMul", 1f);
            _maxSizeMul = Float(d, "MaxSizeMul", Float(d, "mfMaxSizeMul", 1f));
            _wanderCircleDist = Float(d, "WanderCircleDist", 1f);
            _wanderCircleRadius = Float(d, "WanderCircleRadius", 1f);
            _swarmPointMul = Float(d, "SwarmPointMul", 1f);
            _maxSpeed = Float(d, "MaxSpeed", 1f);
            _loopSound = String(d, "LoopSound", "");
            _soundVolume = Float(d, "SoundVolume", 1f);
            _duration = Float(d, "Duration", 5f);
        }

        /// <summary>LuxCalcGuiSetOffset: the extra width is 800 × (screen ratio − 4/3) on wide screens.</summary>
        static Vector2 GuiSetOffset()
        {
            float ratio = Screen.height > 0 ? (float)Screen.width / Screen.height : 4f / 3f;
            return ratio >= 4f / 3f - 0.001f ? new Vector2(800f * (ratio - 800f / 600f) * 0.5f, 0f) : Vector2.zero;
        }

        protected override void OnStart(HplMap map)
        {
            Vector2 offset = GuiSetOffset();
            var min = new Vector2(-offset.x, -offset.y);
            var max = new Vector2(800f + offset.x, 600f + offset.y);
            Bugs.Clear();
            for (int i = 0; i < _numOfBugs; i++)
            {
                // Position at the edges of the screen.
                Vector2 pos;
                bool vertical = HplMath.RandRectf(0f, 1f) > 0.7f;
                if (vertical)
                {
                    pos.x = HplMath.RandRectl(0, 1) == 0 ? min.x : max.x;
                    pos.y = HplMath.RandRectf(min.y, max.y);
                }
                else
                {
                    pos.x = HplMath.RandRectf(min.x, max.x);
                    pos.y = HplMath.RandRectl(0, 1) == 0 ? min.y : max.y;
                }
                Bugs.Add(new Bug
                {
                    active = false,
                    alpha = 0f,
                    angle = HplMath.RandRectf(0f, Mathf.PI * 2f),
                    pos = pos,
                    vel = Vector2.zero,
                    sizeMul = HplMath.RandRectf(_minSizeMul, _maxSizeMul),
                });
            }

            if (_loopSound != "" && map.sound != null)
            {
                _soundEntry = map.sound.PlayGuiStream(_loopSound, true, _soundVolume);
                if (_soundEntry != null)
                {
                    _soundEntryId = _soundEntry.Id;
                    _soundEntry.FadeIn(1f, 1f / 2f);
                }
            }
            _timeCount = _duration;
        }

        public override void OnExit()
        {
            Bugs.Clear();
            var map = HplMap.Current;
            if (_soundEntry != null && Valid(map, _soundEntry, _soundEntryId))
            {
                _soundEntry.FadeOut(1f / 2f);
                _soundEntry = null;
            }
        }

        internal override void OnMapLeave() => _soundEntry = null;

        public override void Update(HplMap map, float timeStep)
        {
            var swarmPoint = new Vector2(400f, 300f);
            bool active = true;
            int disabledCount = 0;

            _timeCount -= timeStep;
            if (_timeCount < 0f)
            {
                _timeCount = 0f;
                active = false;
            }

            for (int i = 0; i < _numOfBugs && i < Bugs.Count; i++)
            {
                var bug = Bugs[i];

                // Alpha
                if (active && bug.alpha < 1f)
                {
                    bug.alpha += HplMath.RandRectf(0.2f, 1f) * timeStep;
                    if (bug.alpha > 1f) bug.alpha = 1f;
                }
                else if (!active)
                {
                    bug.alpha -= HplMath.RandRectf(0.2f, 1f) * timeStep;
                    if (bug.alpha < 0f)
                    {
                        bug.alpha = 0f;
                        disabledCount++;
                    }
                }

                // Wandering (only once it moves: the swarm point starts it)
                if (bug.vel.magnitude > HplPhysics.Epsilon)
                {
                    Vector2 dir = bug.vel.normalized;
                    float angle = HplMath.RandRectf(0f, Mathf.PI * 2f);
                    Vector3 force = HplMatrix.Mul3x3(HplMatrix.RotateY(angle), new Vector3(_wanderCircleRadius, 0f, 0f));
                    bug.vel += (dir * _wanderCircleDist + new Vector2(force.x, force.z)) * timeStep;
                }

                // Swarm around the point
                {
                    float dist = Vector2.Distance(swarmPoint, bug.pos);
                    Vector2 wanted = swarmPoint - bug.pos;
                    wanted = wanted.normalized * _swarmPointMul;
                    Vector2 acc = wanted * Mathf.Min(dist * 0.025f, 1f);
                    bug.vel += acc * timeStep;
                }

                // Cap speed
                float speed = bug.vel.magnitude;
                if (speed > _maxSpeed) bug.vel = bug.vel / speed * _maxSpeed;

                bug.pos += bug.vel * timeStep;
                if (bug.vel.magnitude > HplPhysics.Epsilon) bug.angle = HplMatrix.GetAngleFromPoints2D(Vector2.zero, bug.vel);
            }

            if (!active && disabledCount == Bugs.Count) EventIsDone();
        }
    }

    /// <summary>cLuxInstanityEvent_Particles: a particle system at the camera (followed) and a GUI stream, for a time.</summary>
    public sealed class InsanityEventParticles : InsanityEvent
    {
        string _soundFile, _particleSystem;
        bool _loopSound;
        float _soundVolume, _soundFadeInTime, _soundFadeOutTime, _duration;
        float _timeCount;
        SoundEntry _soundEntry;
        int _soundEntryId;
        Particles.HplParticleSystem _ps;

        protected override void OnLoadData(HplInsanityAssets.EventData d, HplInsanityAssets assets)
        {
            _soundFile = String(d, "SoundFile", "");
            _loopSound = Bool(d, "LoopSound", false);
            _soundVolume = Float(d, "SoundVolume", 0f);
            _soundFadeInTime = Float(d, "SoundFadeInTime", 1f);
            _soundFadeOutTime = Float(d, "SoundFadeOutTime", 1f);
            _particleSystem = String(d, "ParticleSystem", "");
            _duration = Float(d, "Duration", 1f);
        }

        static Vector3 CameraPosition(HplMap map) => map.PlayerCamera != null ? map.PlayerCamera.transform.position : Vector3.zero;

        protected override void OnStart(HplMap map)
        {
            if (_soundFile != "" && map.sound != null)
            {
                _soundEntry = map.sound.PlayGuiStream(_soundFile, _loopSound, _soundVolume);
                if (_soundEntry != null)
                {
                    _soundEntryId = _soundEntry.Id;
                    if (_soundFadeInTime > 0f) _soundEntry.FadeIn(1f, 1f / _soundFadeInTime);
                }
            }
            if (_particleSystem != "" && map.ParticleWorld != null)
            {
                _ps = map.ParticleWorld.Create(Name + "_PS", _particleSystem);
                if (_ps != null)
                {
                    _ps.LocalMatrix = Matrix4x4.Translate(CameraPosition(map));
                    _ps.IsSaved = false;
                }
            }
            _timeCount = _duration;
        }

        public override void OnExit()
        {
            var map = HplMap.Current;
            if (map != null && map.ParticleWorld != null && _ps != null && map.ParticleWorld.Exists(_ps))
            {
                _ps.Kill();
                _ps = null;
            }
            if (_soundEntry != null && Valid(map, _soundEntry, _soundEntryId))
            {
                if (_soundFadeOutTime > 0f) _soundEntry.FadeOut(1f / _soundFadeOutTime);
                else _soundEntry.Stop();
                _soundEntry = null;
            }
        }

        internal override void OnMapLeave()
        {
            _ps = null;
            _soundEntry = null;
        }

        public override void Update(HplMap map, float timeStep)
        {
            if (_ps != null && map.ParticleWorld != null && map.ParticleWorld.Exists(_ps))
                _ps.LocalMatrix = Matrix4x4.Translate(CameraPosition(map));
            _timeCount -= timeStep;
            if (_timeCount < 0f) EventIsDone();
        }
    }

    /// <summary>
    /// cLuxInstanityEvent_SoundStream: a GUI stream after a delay, with an optional subtitle (drawn by the HUD while
    /// no voice plays), screen fade (modulative color, or an image) and disabled player; over when the sound is (and
    /// the fade is out).
    /// </summary>
    public sealed class InsanityEventSoundStream : InsanityEvent
    {
        string _file, _subtitleCat, _subtitleEntry, _fadeImageFile;
        float _volume, _soundDelayTime, _fadeInSpeed, _fadeOutSpeed;
        bool _disablePlayer;
        float _soundCount, _fadeAlphaSpeed;
        SoundEntry _soundEntry;
        int _soundEntryId;

        public bool FadeScreen { get; private set; }
        public Color FadeColor { get; private set; } = Color.white;
        public float FadeAlpha { get; private set; }
        /// <summary>The fade image (FadeImageFile), drawn alpha blended instead of the modulative color.</summary>
        public Texture2D FadeImage { get; private set; }
        Texture2D _fadeImageData;
        /// <summary>The subtitle of the event (SubtitleCat / SubtitleEntry), or "".</summary>
        public string Subtitle { get; private set; } = "";

        protected override void OnLoadData(HplInsanityAssets.EventData d, HplInsanityAssets assets)
        {
            _file = String(d, "File", "");
            _volume = Float(d, "Volume", 1f);
            _soundDelayTime = Float(d, "SoundDelayTime", 0f);
            if (_soundDelayTime <= 0f) _soundDelayTime = 0.001f;
            FadeScreen = Bool(d, "FadeScreen", false);
            FadeColor = ColorOf(d, "FadeColor", Color.white);
            _fadeInSpeed = Float(d, "FadeInTime", 0f);
            _fadeOutSpeed = Float(d, "FadeOutTime", 0f);
            if (_fadeInSpeed <= 0f) _fadeInSpeed = 0.001f;
            if (_fadeOutSpeed <= 0f) _fadeOutSpeed = 0.001f;
            _fadeInSpeed = 1f / _fadeInSpeed;
            _fadeOutSpeed = 1f / _fadeOutSpeed;
            _subtitleCat = String(d, "SubtitleCat", "");
            _subtitleEntry = String(d, "SubtitleEntry", "");
            _fadeImageFile = String(d, "FadeImageFile", "");
            _disablePlayer = Bool(d, "DisablePlayer", false);
            if (_fadeImageFile != "") _fadeImageData = assets.GetImage(_fadeImageFile);
        }

        protected override void OnStart(HplMap map)
        {
            FadeImage = null;
            _soundEntry = null;
            _soundCount = _soundDelayTime;
            FadeAlpha = 0f;
            _fadeAlphaSpeed = _fadeInSpeed;
            if (_disablePlayer && map.player != null) map.player.active = false;
            Subtitle = _subtitleCat != "" && _subtitleEntry != "" && map.language != null ? map.language.Translate(_subtitleCat, _subtitleEntry) : "";
            if (_fadeImageFile != "") FadeImage = _fadeImageData;
        }

        public override void OnExit()
        {
            FadeImage = null;
            var map = HplMap.Current;
            if (_disablePlayer && map != null && map.player != null) map.player.active = true;
            Subtitle = "";
        }

        internal override void OnMapLeave() => _soundEntry = null;

        public override void Update(HplMap map, float timeStep)
        {
            // Play the sound
            if (_soundEntry == null && _soundCount > 0f)
            {
                _soundCount -= timeStep;
                if (_soundCount <= 0f && map.sound != null)
                {
                    _soundEntry = map.sound.PlayGuiStream(_file, false, _volume);
                    if (_soundEntry != null) _soundEntryId = _soundEntry.Id;
                }
            }

            // Fade the screen
            if (FadeScreen)
            {
                FadeAlpha += _fadeAlphaSpeed * timeStep;
                if (_fadeAlphaSpeed > 0f && FadeAlpha > 1f) FadeAlpha = 1f;
                if (_fadeAlphaSpeed < 0f && FadeAlpha < 0f)
                {
                    FadeAlpha = 0f;
                    EventIsDone();
                }
            }

            // The sound is over
            if (_soundCount <= 0f && (_soundEntry == null || !Valid(map, _soundEntry, _soundEntryId)))
            {
                if (!FadeScreen) EventIsDone();
                if (_fadeAlphaSpeed > 0f) _fadeAlphaSpeed = -_fadeOutSpeed;
            }
        }
    }

    /// <summary>
    /// cLuxInstanityEvent_Steps: a sound entity at a random direction and distance from the body, StepNum times,
    /// each time sooner (TimeMulPerStep) and closer (DistanceMulPerStep).
    /// </summary>
    public sealed class InsanityEventSteps : InsanityEvent
    {
        string _sound;
        float _stepTime, _distance, _timeMulPerStep, _distanceMulPerStep;
        int _stepNum;
        float _soundCountMax, _soundCount;
        int _count;
        Vector3 _hplPosition;

        protected override void OnLoadData(HplInsanityAssets.EventData d, HplInsanityAssets assets)
        {
            _sound = String(d, "Sound", "");
            _stepTime = Float(d, "StepTime", 1f);
            _stepNum = Int(d, "StepNum", 0);
            _distance = Float(d, "Distance", 0f);
            _timeMulPerStep = Float(d, "TimeMulPerStep", 0f);
            _distanceMulPerStep = Float(d, "DistanceMulPerStep", 0f);
        }

        protected override void OnStart(HplMap map)
        {
            _hplPosition = HplMatrix.Mul3x3(HplMatrix.RotateY(HplMath.RandRectf(0f, Mathf.PI * 2f)), new Vector3(_distance, 0f, 0f));
            _soundCountMax = _stepTime;
            _soundCount = 0f;
            _count = _stepNum;
        }

        public override void OnExit() { }

        public override void Update(HplMap map, float timeStep)
        {
            if (_soundCount <= 0f)
            {
                var sound = map.sound != null ? map.sound.CreateSoundEntity("insanity_step", _sound, true) : null;
                if (sound != null && map.player != null)
                {
                    sound.IsSaved = false;
                    sound.Position = map.player.Body.Position + HplSpace.Direction(_hplPosition);
                }
                _soundCount = _soundCountMax;
                _soundCountMax *= _timeMulPerStep;
                _hplPosition = _hplPosition.normalized * _hplPosition.magnitude * _distanceMulPerStep;

                _count--;
                if (_count <= 0) EventIsDone();
            }
            else _soundCount -= timeStep;
        }
    }
}
