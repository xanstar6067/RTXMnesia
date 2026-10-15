using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Sound
{
    /// <summary>
    /// Port of cSoundEntity (SoundEntity.cpp): a sound of a .snt in the world. It starts on its first update,
    /// loops or plays its main sound once, plays a random main sound every Interval seconds with the Random
    /// chance, stops its looping sound while the listener is out of MaxDistance and restarts it inside, and is
    /// removed when over if created with removeWhenOver. Its position follows <see cref="Parent"/> (an entity it
    /// is attached to) or <see cref="Position"/>.
    /// </summary>
    public sealed class HplSoundEntity
    {
        readonly HplSoundHandler _handler;
        readonly SoundEntry[] _entries = new SoundEntry[3];
        readonly int[] _entryIds = { -1, -1, -1 };

        public readonly string Name;
        public readonly HplSoundBank.SoundEntityData Data;
        public readonly int CreationId;
        public bool RemoveWhenOver;
        public bool Active = true;
        /// <summary>cSoundEntity::SetForcePlayAsGUISound: played without position (PlaySoundAtEntity on "Player").</summary>
        public bool ForcePlayAsGuiSound;
        public bool IsSaved = true;

        /// <summary>The entity node the sound is a child of (follows it), or null.</summary>
        public Transform Parent;
        /// <summary>World position when there is no parent; with a parent, the offset in its space.</summary>
        public Vector3 Position;

        public float Volume, MinDistance, MaxDistance;

        float _intervalCount, _sleepCount;
        bool _stopped, _started, _skipStartEnd, _prioRemove, _fadingOut, _outOfRange;

        internal HplSoundEntity(string name, HplSoundBank.SoundEntityData data, HplSoundHandler handler, bool removeWhenOver, int creationId)
        {
            Name = name ?? "";
            Data = data;
            _handler = handler;
            RemoveWhenOver = removeWhenOver;
            CreationId = creationId;
            _intervalCount = data.interval;
            Volume = data.volume;
            MinDistance = data.minDistance;
            MaxDistance = data.maxDistance;

            if (handler.Silent)
            {
                _started = _stopped = _skipStartEnd = true;
                Volume = 0f;
                RemoveWhenOver = true;
            }
        }

        public Vector3 WorldPosition => Parent != null ? Parent.TransformPoint(Position) : Position;

        /// <summary>Stays at its current place when the node it followed goes away.</summary>
        public void Detach()
        {
            if (Parent == null) return;
            Position = Parent.TransformPoint(Position);
            Parent = null;
        }

        public bool IsStopped() => _stopped && _entries[(int)SoundEntityType.Stop] == null;
        public bool IsFadingOut => _fadingOut;

        SoundEntry Entry(SoundEntityType type, bool checkValid)
        {
            var e = _entries[(int)type];
            if (checkValid && e != null && !_handler.IsValid(e, _entryIds[(int)type])) return null;
            return e;
        }

        bool Valid(SoundEntityType type) => _handler.IsValid(_entries[(int)type], _entryIds[(int)type]);

        /// <summary>cSoundEntity::GetSoundEntry(type, true): the playing sound of the type, or null.</summary>
        public SoundEntry GetEntry(SoundEntityType type) => Entry(type, true);

        public void Play(bool playStart = true)
        {
            if (_handler.Silent) return;
            _stopped = false;
            _outOfRange = false;
            _fadingOut = false;

            if (playStart && !_skipStartEnd && Data.loop && Data.HasSound(SoundEntityType.Start))
            {
                PlaySound(SoundEntityType.Start, false);
                _started = false;
            }

            if (_entries[0] == null && _entries[(int)SoundEntityType.Start] == null && (!Data.loop || Data.interval == 0f))
            {
                PlaySound(SoundEntityType.Main, Data.loop);
                _started = true;
            }
        }

        public void Stop(bool playEnd = true)
        {
            if (_stopped) return;
            _stopped = true;
            _outOfRange = false;
            if (_entries[0] == null) return;

            if (Data.loop && playEnd && !_skipStartEnd) PlaySound(SoundEntityType.Stop, false);
            if (Valid(SoundEntityType.Main)) _entries[0].Stop();
            _entries[0] = null;
        }

        public void FadeIn(float speed)
        {
            if (_handler.Silent) return;
            Play(false);
            Entry(SoundEntityType.Main, false)?.FadeIn(1f, speed);
        }

        public void FadeOut(float speed)
        {
            _fadingOut = true;
            if (_handler.Silent) return;
            if (_entries[0] == null)
            {
                _stopped = true;
                return;
            }
            var entry = Entry(SoundEntityType.Main, true);
            if (entry != null) entry.FadeOut(speed);
            else
            {
                _entries[0] = null;
                _stopped = true;
            }
        }

        internal void UpdateLogic(float timeStep)
        {
            if (_handler.Silent) return;
            if (_sleepCount > 0f)
            {
                _sleepCount -= timeStep;
                return;
            }

            if (_outOfRange && !_stopped)
            {
                if (CheckIsOutOfRange()) return;
                Play(false);
                _outOfRange = false;
            }

            for (int i = 0; i < 3; i++)
            {
                if (_entries[i] == null) continue;
                if (!_handler.IsValid(_entries[i], _entryIds[i]))
                {
                    _entries[i] = null;
                    if (_prioRemove && i == 0)
                    {
                        if (!_stopped)
                        {
                            _started = false;
                            _sleepCount = 0.3f;
                        }
                    }
                    else if ((Data.interval == 0f || !Data.loop) && _started) _stopped = true;
                }
                else if (!_entries[i].Relative) _entries[i].Position = WorldPosition;
            }

            if (_stopped) return;

            if (_entries[0] == null && _entries[(int)SoundEntityType.Start] == null && (!Data.loop || Data.interval == 0f))
            {
                if (PlaySound(SoundEntityType.Main, Data.loop))
                {
                    _started = true;
                    _prioRemove = false;
                }
                else if (_prioRemove) _sleepCount = 0.3f;
            }

            if (Data.loop && Data.interval > 0f && _entries[(int)SoundEntityType.Start] == null && _entries[0] == null)
            {
                _intervalCount += timeStep;
                if (_intervalCount >= Data.interval)
                {
                    if (HplMath.RandRectf(0f, 1f) <= Data.random || Data.random == 0f) PlaySound(SoundEntityType.Main, false);
                    _intervalCount = 0f;
                }
            }

            if (_entries[(int)SoundEntityType.Start] == null && Data.loop && Data.use3D && CheckIsOutOfRange())
            {
                if (_fadingOut) Stop(false);
                else
                {
                    if (_entries[0] != null)
                    {
                        if (Valid(SoundEntityType.Main)) _entries[0].Stop();
                        _entries[0] = null;
                    }
                    _outOfRange = true;
                }
            }
        }

        bool CheckIsOutOfRange() => (_handler.ListenerPosition - WorldPosition).sqrMagnitude >= MaxDistance * MaxDistance;

        bool PlaySound(SoundEntityType type, bool loop)
        {
            string sound = Data.GetRandomSoundName(type, true);
            if (sound == "") return false;

            SoundEntry entry;
            if (ForcePlayAsGuiSound)
                entry = _handler.Play3D(sound, loop, Volume, new Vector3(0f, 0f, 1f), 100f, 200f, SoundEntryType.Gui, true, Data.priority, Data.stream);
            else if (Data.use3D)
                entry = _handler.Play3D(sound, loop, Volume, WorldPosition, MinDistance, MaxDistance, SoundEntryType.World, false, Data.priority, Data.stream);
            else
                entry = _handler.Play3D(sound, loop, Volume, new Vector3(0f, 0f, 1f), 100f, 200f, SoundEntryType.World, true, Data.priority, Data.stream);

            _entries[(int)type] = entry;
            if (entry == null)
            {
                HplLog.Error($"Couldn't play sound '{sound}' for sound entity {Name}");
                if (type == SoundEntityType.Main) _stopped = true;
                return false;
            }
            _entryIds[(int)type] = entry.Id;
            entry.BlockVolumeMul = Data.blockVolumeMul;
            if (type == SoundEntityType.Main) entry.OnPriorityRelease = OnPriorityRelease;
            return true;
        }

        void OnPriorityRelease()
        {
            if (_stopped || _fadingOut) return;
            _prioRemove = true;
        }

        /// <summary>~cSoundEntity: its sounds stop.</summary>
        internal void Destroy()
        {
            for (int i = 0; i < 3; i++)
                if (_handler.IsValid(_entries[i], _entryIds[i])) _entries[i].Stop();
        }
    }
}
