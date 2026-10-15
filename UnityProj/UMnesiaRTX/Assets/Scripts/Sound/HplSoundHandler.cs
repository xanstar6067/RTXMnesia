using System;
using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Sound
{
    /// <summary>
    /// Port of cSoundHandler (SoundHandler.cpp) with the world's sound entities (cWorld::CreateSoundEntity /
    /// UpdateSoundEntities) and the music (<see cref="HplMusicHandler"/>). Stepped by the map at 60 Hz like the
    /// engine's sound update.
    ///
    /// Like HPL2 the volume of a 3D sound is computed here (linear from MinDistance to MaxDistance, blocking by
    /// bodies with BlocksSound); Unity only pans it: 3D sources have a flat custom rolloff. Whether a sound is
    /// still playing is counted here from the clip length and speed, so the logic (voice callbacks, sounds that
    /// end) runs the same without an audio device (batch mode).
    /// </summary>
    public sealed class HplSoundHandler : MonoBehaviour
    {
        public HplSoundBank bank;
        /// <summary>The listener (camera). Set by the map.</summary>
        public Transform listener;
        /// <summary>cSoundHandler::SetSilent: nothing starts (cWorld::PreUpdate).</summary>
        public bool Silent;

        readonly List<SoundEntry> _entries = new();
        readonly List<HplSoundEntity> _soundEntities = new();
        readonly Stack<AudioSource> _pool = new();
        int _idCount, _soundCreationId;
        readonly float[] _globalVolume = { 1f, 1f }, _globalSpeed = { 1f, 1f };

        public readonly MultipleSettings GlobalVolume = new(), GlobalSpeed = new();
        public HplMusicHandler Music { get; private set; }
        public IReadOnlyList<HplSoundEntity> SoundEntities => _soundEntities;
        public IReadOnlyList<SoundEntry> Entries => _entries;

        public Vector3 ListenerPosition => listener != null ? listener.position : Vector3.zero;

        static AnimationCurve _flatRolloff;
        static readonly RaycastHit[] Hits = new RaycastHit[32];

        void Awake()
        {
            Music ??= new HplMusicHandler(this);
        }

        public void EnsureMusic() => Music ??= new HplMusicHandler(this);

        // ------------------------------------------------------------------ update

        /// <summary>One engine step: the world's sound entities, then the sound entries, then the music.</summary>
        public void Step(float timeStep)
        {
            EnsureMusic();
            UpdateSoundEntities(timeStep);
            UpdateEntries(timeStep);
            Music.Update(timeStep);
        }

        void UpdateSoundEntities(float timeStep)
        {
            for (int i = 0; i < _soundEntities.Count;)
            {
                var sound = _soundEntities[i];
                if (sound.Active) sound.UpdateLogic(timeStep);
                if (sound.IsStopped() && sound.RemoveWhenOver)
                {
                    sound.Destroy();
                    _soundEntities.RemoveAt(i);
                }
                else i++;
            }
        }

        void UpdateEntries(float timeStep)
        {
            _globalVolume[0] = GlobalVolume.CalcMin(SoundEntryType.World, 1f);
            _globalVolume[1] = GlobalVolume.CalcMin(SoundEntryType.Gui, 1f);
            _globalSpeed[0] = GlobalSpeed.CalcMin(SoundEntryType.World, 1f);
            _globalSpeed[1] = GlobalSpeed.CalcMin(SoundEntryType.Gui, 1f);
            GlobalVolume.Update(timeStep);
            GlobalSpeed.Update(timeStep);

            for (int i = 0; i < _entries.Count;)
            {
                var entry = _entries[i];
                if (entry.Update(timeStep)) i++;
                else
                {
                    entry.Release();
                    _entries.RemoveAt(i);
                }
            }
        }

        public float GetGlobalVolume(SoundEntryType type) => type == SoundEntryType.World ? _globalVolume[0] : _globalVolume[1];
        public float GetGlobalSpeed(SoundEntryType type) => type == SoundEntryType.World ? _globalSpeed[0] : _globalSpeed[1];

        // ------------------------------------------------------------------ playing

        /// <summary>cSoundHandler::Play. Null (with an error) when the sound file does not exist.</summary>
        public SoundEntry Play(string name, bool loop, float volume, Vector3 position, float minDistance, float maxDistance,
                               SoundEntryType type, bool relative, bool is3D, int priorityModifier, bool stream)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var clip = bank != null ? bank.GetClip(name) : null;
            if (clip == null)
            {
                HplLog.Error($"Can't find sound '{name}'!");
                return null;
            }

            int distPriority = 0;
            if (is3D && !relative)
            {
                float dist = Vector3.Distance(position, ListenerPosition);
                distPriority = dist >= maxDistance ? 0 : dist >= minDistance ? 10 : 100;
            }

            var entry = new SoundEntry(this, name, clip, TakeSource(), volume, type, is3D, relative, _idCount++)
            {
                Loop = Silent ? false : loop,
                MinDistance = minDistance,
                MaxDistance = maxDistance,
                Position = position,
                Priority = distPriority + priorityModifier,
            };
            if (Silent) entry.Stop();
            _entries.Add(entry);
            return entry;
        }

        public SoundEntry Play3D(string name, bool loop, float volume, Vector3 position, float minDistance, float maxDistance,
                                 SoundEntryType type, bool relative, int priorityModifier, bool stream) =>
            Play(name, loop, volume, position, minDistance, maxDistance, type, relative, true, priorityModifier, stream);

        public SoundEntry PlayGui(string name, bool loop, float volume, SoundEntryType type = SoundEntryType.Gui) =>
            Play(name, loop, volume, new Vector3(0f, 0f, 1f), 1f, 1000f, type, true, false, 0, false);

        public SoundEntry PlayGuiStream(string file, bool loop, float volume, SoundEntryType type = SoundEntryType.Gui) =>
            Play(file, loop, volume, new Vector3(0f, 0f, 1f), 1f, 1000f, type, true, false, 0, true);

        /// <summary>
        /// cSoundHandler::PlaySoundEntityGui, with its quirk: a non-streamed sound plays at the .snt volume squared
        /// (the volume argument is only used for streamed ones).
        /// </summary>
        public SoundEntry PlaySoundEntityGui(string name, bool loop, float volume, SoundEntryType type = SoundEntryType.Gui)
        {
            var data = bank != null ? bank.GetSoundEntity(name) : null;
            if (data == null) return null;
            string sound = data.GetRandomSoundName(SoundEntityType.Main, true);
            if (sound == "") return null;
            return data.stream ? PlayGuiStream(sound, loop, volume * data.volume, type)
                               : PlayGui(sound, loop, data.volume * data.volume, type);
        }

        /// <summary>
        /// cLuxHelpFuncs::PlayGuiSoundData: a random sound of a .snt played without position, at the .snt volume ×
        /// <paramref name="volumeMul"/>. The .snt data (distances, volume) is returned for enemy hearing.
        /// </summary>
        public bool PlayGuiSoundData(string name, SoundEntryType type, float volumeMul = 1f,
                                     SoundEntityType soundType = SoundEntityType.Main, bool skipPreviousRandom = true)
            => PlayGuiSoundData(name, type, out _, out _, volumeMul, soundType, skipPreviousRandom);

        public bool PlayGuiSoundData(string name, SoundEntryType type, out HplSoundBank.SoundEntityData data, out SoundEntry entry,
                                     float volumeMul = 1f, SoundEntityType soundType = SoundEntityType.Main, bool skipPreviousRandom = true)
        {
            entry = null;
            data = null;
            if (string.IsNullOrEmpty(name)) return false;
            data = bank != null ? bank.GetSoundEntity(name) : null;
            if (data == null)
            {
                HplLog.Error($"Cannot find sound entity '{name}'");
                return false;
            }
            string sound = data.GetRandomSoundName(soundType, skipPreviousRandom);
            entry = PlayGui(sound, false, data.volume * volumeMul, type);
            return true;
        }

        public bool Stop(string name)
        {
            var entry = GetEntry(name);
            if (entry == null) return false;
            entry.Stop();
            return true;
        }

        public void StopAll(SoundEntryType types)
        {
            foreach (var e in _entries) if ((e.Type & types) != 0) e.Stop();
        }

        public void FadeOutAll(SoundEntryType types, float fadeSpeed, bool disableStop)
        {
            foreach (var e in _entries)
            {
                if ((e.Type & types) == 0) continue;
                e.FadeOut(fadeSpeed);
                if (disableStop) e.StopDisabled = true;
            }
        }

        public bool IsPlaying(string name) => GetEntry(name)?.IsPlaying ?? false;

        /// <summary>cSoundHandler::IsValid: the entry is still in the list (has not ended) and is the same sound.</summary>
        public bool IsValid(SoundEntry entry, int id) => entry != null && entry.Alive && entry.Id == id;

        public SoundEntry GetEntry(string name)
        {
            string low = (name ?? "").ToLowerInvariant();
            foreach (var e in _entries) if (e.Name == low) return e;
            return null;
        }

        // ------------------------------------------------------------------ global volume and speed

        public int SetGlobalVolume(float volume, SoundEntryType types, int id) => Set(GlobalVolume, volume, types, id);
        public int SetGlobalSpeed(float speed, SoundEntryType types, int id) => Set(GlobalSpeed, speed, types, id);

        public int FadeGlobalVolume(float destVolume, float speed, SoundEntryType types, int id, bool destroyAtDest) =>
            Fade(GlobalVolume, destVolume, speed, types, id, destroyAtDest);

        public int FadeGlobalSpeed(float destSpeed, float speed, SoundEntryType types, int id, bool destroyAtDest) =>
            Fade(GlobalSpeed, destSpeed, speed, types, id, destroyAtDest);

        static int Set(MultipleSettings handler, float value, SoundEntryType types, int id)
        {
            if (id < 0) id = handler.CreateEntry();
            var e = handler.GetEntry(id, true);
            e.Types = types;
            e.SetValAndDest(value);
            return id;
        }

        static int Fade(MultipleSettings handler, float dest, float speed, SoundEntryType types, int id, bool destroyAtDest)
        {
            if (id < 0) id = handler.CreateEntry();
            var e = handler.GetEntry(id, true);
            e.Types = types;
            e.FadeTo(dest, speed, destroyAtDest);
            return id;
        }

        // ------------------------------------------------------------------ sound entities (cWorld)

        /// <summary>cWorld::CreateSoundEntity: null (with an error) when the .snt does not exist.</summary>
        public HplSoundEntity CreateSoundEntity(string name, string soundEntityFile, bool removeWhenOver)
        {
            var data = bank != null ? bank.GetSoundEntity(soundEntityFile) : null;
            if (data == null)
            {
                HplLog.Error($"Cannot find sound entity '{soundEntityFile}'");
                return null;
            }
            var sound = new HplSoundEntity(name, data, this, removeWhenOver, _soundCreationId++);
            _soundEntities.Add(sound);
            return sound;
        }

        /// <summary>cWorld::GetSoundEntity: the first one with the name.</summary>
        public HplSoundEntity GetSoundEntity(string name)
        {
            foreach (var s in _soundEntities) if (s.Name == name) return s;
            return null;
        }

        public void DestroySoundEntity(HplSoundEntity sound)
        {
            if (_soundEntities.Remove(sound)) sound.Destroy();
        }

        // ------------------------------------------------------------------ blocking (cSoundRayCallback)

        /// <summary>cSoundHandler::CheckSoundIsBlocked: a body with BlocksSound (doors) between sound and listener.</summary>
        public bool CheckSoundIsBlocked(Vector3 soundPosition)
        {
            Vector3 delta = ListenerPosition - soundPosition;
            float length = delta.magnitude;
            if (length < 1e-4f) return false;
            int count = Physics.RaycastNonAlloc(soundPosition, delta / length, Hits, length, Physics.AllLayers,
                                                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (HplPhysics.BlocksSound(Hits[i].collider)) return true;
            return false;
        }

        // ------------------------------------------------------------------ audio sources

        AudioSource TakeSource()
        {
            AudioSource source;
            if (_pool.Count > 0)
            {
                source = _pool.Pop();
                source.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Sound") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, FlatRolloff);
                source.minDistance = 1f;
                source.maxDistance = 100000f;
            }
            source.volume = 0f;
            source.pitch = 1f;
            return source;
        }

        internal void ReturnSource(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            _pool.Push(source);
        }

        static AnimationCurve FlatRolloff => _flatRolloff ??= new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        void OnDestroy()
        {
            foreach (var e in _entries) e.Release();
            _entries.Clear();
            Music?.StopAllNow();
        }

        // ------------------------------------------------------------------ types

        /// <summary>
        /// cMultipleSettingsHandler: several named requests for one value (global volume: script, flashback,
        /// death...); the result is their minimum for the asked entry types.
        /// </summary>
        public sealed class MultipleSettings
        {
            public sealed class Entry
            {
                public float Val = 1f, Dest = 1f, Speed;
                public SoundEntryType Types;
                public bool DestroyAtDest;

                public void FadeTo(float dest, float speed, bool destroyAtDest)
                {
                    Dest = dest;
                    Speed = HplSoundMath.CorrectSignOfSpeed(Val, Dest, speed);
                    DestroyAtDest = destroyAtDest;
                }

                public void SetValAndDest(float value) => Val = Dest = value;
            }

            readonly List<Entry> _entries = new();

            public Entry GetEntry(int index, bool forceCreation)
            {
                if (index >= _entries.Count)
                {
                    if (!forceCreation) return null;
                    while (_entries.Count <= index) _entries.Add(null);
                    _entries[index] = new Entry();
                }
                else if (_entries[index] == null && forceCreation) _entries[index] = new Entry();
                return _entries[index];
            }

            public int CreateEntry()
            {
                for (int i = 0; i < _entries.Count; i++)
                    if (_entries[i] == null)
                    {
                        _entries[i] = new Entry();
                        return i;
                    }
                _entries.Add(new Entry());
                return _entries.Count - 1;
            }

            public void DestroyEntry(int index)
            {
                if (index < _entries.Count) _entries[index] = null;
            }

            public float CalcMin(SoundEntryType types, float fallback)
            {
                bool used = false;
                float result = 0f;
                foreach (var e in _entries)
                {
                    if (e == null || (e.Types & types) == 0) continue;
                    if (!used || result > e.Val) result = e.Val;
                    used = true;
                }
                return used ? result : fallback;
            }

            public void Update(float timeStep)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    var e = _entries[i];
                    if (e == null) continue;
                    if (e.Val != e.Dest)
                    {
                        e.Val += e.Speed * timeStep;
                        if (e.Speed < 0f && e.Val < e.Dest) e.Val = e.Dest;
                        else if (e.Speed > 0f && e.Val > e.Dest) e.Val = e.Dest;
                    }
                    if (e.DestroyAtDest && e.Val == e.Dest) _entries[i] = null;
                }
            }
        }
    }

    /// <summary>
    /// cSoundEntry: one playing sound with its volume, speed and blocking fades. The AudioSource only outputs;
    /// the play time is counted here.
    /// </summary>
    public sealed class SoundEntry
    {
        readonly HplSoundHandler _handler;
        AudioSource _source;

        public readonly string Name;
        public readonly AudioClip Clip;
        public readonly SoundEntryType Type;
        public readonly int Id;
        public readonly bool Is3D, Relative;
        public bool Loop;
        public float MinDistance, MaxDistance;
        public Vector3 Position;
        public int Priority;
        /// <summary>iSoundChannel block volume: 1 (no effect) unless a sound entity sets its .snt value.</summary>
        public float BlockVolumeMul = 1f;
        public bool StopDisabled;
        /// <summary>cSoundEntityChannelCallback::OnPriorityRelease: not used, Unity has no channel limit here.</summary>
        public Action OnPriorityRelease;

        float _normalVolume, _volumeMul = 1f, _volumeFadeDest = 1f, _volumeFadeSpeed;
        float _normalSpeed = 1f, _speedMul = 1f, _speedFadeDest = 1f, _speedFadeSpeed;
        float _blockMul = 1f, _blockFadeDest = 1f, _blockFadeSpeed;
        bool _stopAfterFadeOut, _firstTime = true, _stopped, _paused;
        float _elapsed;

        /// <summary>Still in the handler's list (cSoundHandler::IsValid).</summary>
        public bool Alive { get; private set; } = true;

        internal SoundEntry(HplSoundHandler handler, string name, AudioClip clip, AudioSource source, float volume,
                            SoundEntryType type, bool is3D, bool relative, int id)
        {
            _handler = handler;
            Name = (name ?? "").ToLowerInvariant();
            Clip = clip;
            _source = source;
            _normalVolume = volume;
            Type = type;
            Is3D = is3D;
            Relative = relative;
            Id = id;
            _source.clip = clip;
            _source.spatialBlend = is3D && !relative ? 1f : 0f;
            _source.gameObject.name = "Sound " + Name;
        }

        public bool IsPlaying => !_stopped && (Loop || _elapsed < Clip.length);
        public float ElapsedTime => _elapsed;
        public float VolumeMul => _volumeMul;

        /// <summary>cSoundEntry::Update: false when the sound is over.</summary>
        internal bool Update(float timeStep)
        {
            UpdateVolumeMulFade(timeStep);
            UpdateSpeedMulFade(timeStep);

            float speed = _normalSpeed * _speedMul * _handler.GetGlobalSpeed(Type);
            if (_source.pitch != speed) _source.pitch = speed;

            if (_blockMul != _blockFadeDest)
            {
                _blockMul += _blockFadeSpeed * timeStep;
                if (_blockFadeSpeed < 0f) { if (_blockMul < _blockFadeDest) _blockMul = _blockFadeDest; }
                else if (_blockMul > _blockFadeDest) _blockMul = _blockFadeDest;
            }

            if (Is3D && !Relative) Update3DSpecifics();
            else _source.volume = _normalVolume * _volumeMul * _handler.GetGlobalVolume(Type);

            if (_firstTime && !_handler.Silent)
            {
                _source.loop = Loop;
                if (!_stopped) _source.Play();
                _firstTime = false;
            }
            else if (!_paused && !_stopped) _elapsed += timeStep * speed;

            if (!IsPlaying && !_paused)
            {
                _source.Stop();
                return false;
            }
            return true;
        }

        void UpdateVolumeMulFade(float timeStep)
        {
            if (_volumeMul != _volumeFadeDest)
            {
                _volumeMul += _volumeFadeSpeed * timeStep;
                if (_volumeFadeSpeed < 0f && _volumeMul <= _volumeFadeDest) _volumeMul = _volumeFadeDest;
                else if (_volumeFadeSpeed >= 0f && _volumeMul >= _volumeFadeDest) _volumeMul = _volumeFadeDest;
            }
            if (_stopAfterFadeOut && Mathf.Abs(_volumeMul) < 0.001f)
            {
                _stopped = true;
                _source.Stop();
                _stopAfterFadeOut = false;
            }
        }

        void UpdateSpeedMulFade(float timeStep)
        {
            if (_speedMul == _speedFadeDest) return;
            _speedMul += _speedFadeSpeed * timeStep;
            if (_speedFadeSpeed < 0f && _speedMul <= _speedFadeDest) _speedMul = _speedFadeDest;
            else if (_speedFadeSpeed >= 0f && _speedMul >= _speedFadeDest) _speedMul = _speedFadeDest;
        }

        void Update3DSpecifics()
        {
            Vector3 listener = _handler.ListenerPosition;
            _source.transform.position = Position;
            float sqrDist = (Position - listener).sqrMagnitude;
            if (sqrDist >= MaxDistance * MaxDistance)
            {
                _source.volume = 0f;
                _source.priority = 256;
                return;
            }

            if (_handler.CheckSoundIsBlocked(Position))
            {
                _blockFadeDest = 0f;
                _blockFadeSpeed = -1f / 0.55f;
                if (_firstTime) _blockMul = 0f;
            }
            else
            {
                _blockFadeDest = 1f;
                _blockFadeSpeed = 1f / 0.2f;
                if (_firstTime) _blockMul = 1f;
            }

            float distMul;
            if (sqrDist < MinDistance * MinDistance)
            {
                _source.priority = 28;
                distMul = 1f;
            }
            else
            {
                _source.priority = 118;
                float delta = Mathf.Sqrt(sqrDist) - MinDistance;
                distMul = 1f - delta / (MaxDistance - MinDistance);
            }
            float block = BlockVolumeMul + _blockMul * (1f - BlockVolumeMul);
            _source.volume = _normalVolume * _volumeMul * block * distMul * _handler.GetGlobalVolume(Type);
        }

        public void Stop()
        {
            if (StopDisabled) return;
            _firstTime = false;
            _paused = false;
            _stopped = true;
            if (_source != null) _source.Stop();
        }

        public void SetPaused(bool paused)
        {
            if (_paused == paused || _source == null) return;
            _paused = paused;
            if (paused) _source.Pause();
            else if (!_firstTime) _source.UnPause();
        }

        public void SetDefaultVolume(float volume) => _normalVolume = volume;
        public void SetDefaultSpeed(float speed) => _normalSpeed = speed;

        public void SetVolumeMul(float mul) => _volumeMul = _volumeFadeDest = mul;
        public void SetSpeedMul(float mul) => _speedMul = _speedFadeDest = mul;

        public void FadeVolumeMulTo(float dest, float speed)
        {
            _volumeFadeDest = dest;
            _volumeFadeSpeed = HplSoundMath.CorrectSignOfSpeed(_volumeMul, dest, speed);
        }

        public void FadeSpeedMulTo(float dest, float speed)
        {
            _speedFadeDest = dest;
            _speedFadeSpeed = HplSoundMath.CorrectSignOfSpeed(_speedMul, dest, speed);
        }

        public void FadeOut(float speed)
        {
            _volumeFadeDest = 0f;
            _volumeFadeSpeed = HplSoundMath.CorrectSignOfSpeed(_volumeMul, 0f, speed);
            _stopAfterFadeOut = true;
        }

        public void FadeIn(float volumeMul, float speed)
        {
            _volumeFadeDest = volumeMul;
            _volumeMul = 0f;
            _volumeFadeSpeed = HplSoundMath.CorrectSignOfSpeed(0f, volumeMul, speed);
        }

        internal void Release()
        {
            Alive = false;
            _handler.ReturnSource(_source);
            _source = null;
        }
    }

    static class HplSoundMath
    {
        /// <summary>cMath::GetCorrectSignOfSpeed.</summary>
        public static float CorrectSignOfSpeed(float current, float dest, float speed)
        {
            if (dest < current && speed > 0f) return -speed;
            if (dest > current && speed < 0f) return -speed;
            return speed;
        }
    }
}
