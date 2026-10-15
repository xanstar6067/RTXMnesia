using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Sound
{
    /// <summary>
    /// The music: port of cMusicHandler (MusicHandler.cpp: one main song, older ones fading out, resume positions)
    /// and cLuxMusicHandler (LuxMusicHandler.cpp: songs by priority, the highest one plays; when it is stopped
    /// or a non-looping one ends, the next looping one resumes). Music volume × Sound/InGameMusicVolume of
    /// game.cfg. Enemy music (priorities 8 and 9): step 6.
    /// </summary>
    public sealed class HplMusicHandler
    {
        sealed class MusicEntry
        {
            public string File;
            public AudioSource Source;
            public float Volume, MaxVolume, VolumeAdd;
            public bool Loop;
            public float Elapsed, Length;
            public bool IsPlaying => Loop || Elapsed < Length;
        }

        struct GameMusic
        {
            public string File;
            public bool Loop, Resume;
            public float Volume;
        }

        const int EnemyMusicTypes = 2; // eLuxEnemyMusic: Search, Attack
        readonly HplSoundHandler _handler;
        MusicEntry _mainSong;
        readonly List<MusicEntry> _fadingSongs = new();
        readonly Dictionary<string, float> _resumePositions = new();
        float _volumeMul = 1f, _volumeMulFadeGoal = 1f, _volumeMulFadeSpeed = 1f;
        bool _paused;

        readonly GameMusic[] _gameMusic = new GameMusic[8 + EnemyMusicTypes + 1];
        int MaxPrio => 8 + EnemyMusicTypes;
        int _currentMaxPrio = -1;
        /// <summary>cLuxMusicHandler::mfVolumeMul: Sound / InGameMusicVolume of game.cfg.</summary>
        public float GameVolumeMul = 1f;

        public HplMusicHandler(HplSoundHandler handler) => _handler = handler;

        public string CurrentSongName => _mainSong?.File ?? "";
        public float CurrentSongVolume => _mainSong?.Volume ?? 0f;
        public bool IsPaused => _paused;

        // ------------------------------------------------------------------ cLuxMusicHandler

        public void Play(string file, bool loop, float volume, float fadeTime, int prio, bool resume)
        {
            if (prio > MaxPrio) prio = MaxPrio;
            if (_gameMusic[prio].File == file) return;
            float v = volume * GameVolumeMul;
            if (_currentMaxPrio <= prio)
            {
                PlaySong(file, v, v / fadeTime, loop, resume);
                _currentMaxPrio = prio;
            }
            _gameMusic[prio] = new GameMusic { File = file, Loop = loop, Resume = resume, Volume = v };
        }

        public void Stop(float fadeTime, int prio)
        {
            if (prio > MaxPrio) prio = MaxPrio;
            if (string.IsNullOrEmpty(_gameMusic[prio].File)) return;
            _gameMusic[prio].File = "";
            if (prio != _currentMaxPrio) return;
            StopSong(_gameMusic[prio].Volume / fadeTime);
            _currentMaxPrio = -1;
            PlayHighestPriority();
        }

        void PlayHighestPriority()
        {
            for (int i = MaxPrio; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(_gameMusic[i].File)) continue;
                if (_gameMusic[i].Loop)
                {
                    PlaySong(_gameMusic[i].File, _gameMusic[i].Volume, 0.3f, true, _gameMusic[i].Resume);
                    _currentMaxPrio = i;
                    break;
                }
                _gameMusic[i].File = "";
            }
        }

        public void Update(float timeStep)
        {
            UpdateSongs(timeStep);
            if (_currentMaxPrio >= 0 && _mainSong == null)
            {
                _gameMusic[_currentMaxPrio].File = "";
                _currentMaxPrio = -1;
                PlayHighestPriority();
            }
        }

        // ------------------------------------------------------------------ cMusicHandler

        bool PlaySong(string file, float volume, float fadeStepSize, bool loop, bool resume)
        {
            bool playing = _mainSong != null && _mainSong.File == file;
            if (!playing)
            {
                if (_mainSong != null)
                {
                    _mainSong.VolumeAdd = fadeStepSize;
                    _fadingSongs.Add(_mainSong);
                }
                for (int i = 0; i < _fadingSongs.Count;)
                {
                    if (_fadingSongs[i].File == file)
                    {
                        Release(_fadingSongs[i]);
                        _fadingSongs.RemoveAt(i);
                    }
                    else i++;
                }
                _mainSong = LoadAndStart(file, loop, resume);
                if (_mainSong == null) return false;
            }
            else if (_mainSong.MaxVolume == volume) return true;

            _mainSong.MaxVolume = volume;
            _mainSong.Loop = loop;
            _mainSong.VolumeAdd = _mainSong.MaxVolume > _mainSong.Volume ? fadeStepSize : -fadeStepSize;
            return true;
        }

        void StopSong(float fadeStepSize)
        {
            if (_mainSong == null) return;
            if (fadeStepSize < 0f) fadeStepSize = -fadeStepSize;
            _mainSong.VolumeAdd = fadeStepSize;
            UpdateResumeEntry(_mainSong, fadeStepSize);
            if (fadeStepSize == 0f)
            {
                _mainSong.Source.volume = 0f;
                _mainSong.Source.Stop();
                _mainSong.Volume = 0f;
            }
            _fadingSongs.Add(_mainSong);
            _mainSong = null;
        }

        public void Pause()
        {
            if (_mainSong != null) _mainSong.Source.Pause();
            foreach (var s in _fadingSongs) s.Source.Pause();
            _paused = true;
        }

        public void Resume()
        {
            if (_mainSong != null) _mainSong.Source.UnPause();
            foreach (var s in _fadingSongs) s.Source.UnPause();
            _paused = false;
        }

        public void FadeVolumeMul(float dest, float speed)
        {
            _volumeMulFadeGoal = dest;
            _volumeMulFadeSpeed = speed;
        }

        public void SetVolumeMul(float mul) => _volumeMul = _volumeMulFadeGoal = mul;

        void UpdateSongs(float timeStep)
        {
            if (_paused) return;
            if (_volumeMul < _volumeMulFadeGoal) _volumeMul = Mathf.Min(_volumeMul + timeStep * _volumeMulFadeSpeed, _volumeMulFadeGoal);
            else if (_volumeMul > _volumeMulFadeGoal) _volumeMul = Mathf.Max(_volumeMul - timeStep * _volumeMulFadeSpeed, _volumeMulFadeGoal);

            if (_mainSong != null)
            {
                _mainSong.Elapsed += timeStep;
                if (!_mainSong.IsPlaying)
                {
                    Release(_mainSong);
                    _mainSong = null;
                }
                else
                {
                    _mainSong.Volume += _mainSong.VolumeAdd * timeStep;
                    if (_mainSong.VolumeAdd > 0f) { if (_mainSong.Volume >= _mainSong.MaxVolume) _mainSong.Volume = _mainSong.MaxVolume; }
                    else if (_mainSong.Volume <= _mainSong.MaxVolume) _mainSong.Volume = _mainSong.MaxVolume;
                    _mainSong.Source.volume = _mainSong.Volume * _volumeMul;
                }
            }

            for (int i = 0; i < _fadingSongs.Count;)
            {
                var song = _fadingSongs[i];
                song.Elapsed += timeStep;
                song.Volume -= song.VolumeAdd * timeStep;
                if (song.Volume <= 0f)
                {
                    Release(song);
                    _fadingSongs.RemoveAt(i);
                }
                else
                {
                    song.Source.volume = song.Volume * _volumeMul;
                    i++;
                }
            }
        }

        void UpdateResumeEntry(MusicEntry song, float fadeStepSize)
        {
            float timeAdd = fadeStepSize > 0f ? song.Source.volume / fadeStepSize : 0f;
            _resumePositions[song.File] = song.Elapsed + timeAdd;
        }

        MusicEntry LoadAndStart(string file, bool loop, bool resume)
        {
            var clip = _handler.bank != null ? _handler.bank.GetClip(file) : null;
            if (clip == null)
            {
                HplLog.Error($"Couldn't load music '{file}'");
                return null;
            }
            var go = new GameObject("Music " + file) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_handler.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.clip = clip;
            source.loop = loop;
            source.volume = 0f;
            source.priority = 0;

            var song = new MusicEntry { File = file, Source = source, Loop = loop, Length = clip.length };
            if (resume && _resumePositions.TryGetValue(file, out float pos))
            {
                if (pos >= clip.length) pos = 0f;
                song.Elapsed = pos;
                source.time = loop ? pos % clip.length : pos;
            }
            source.Play();
            return song;
        }

        static void Release(MusicEntry song)
        {
            if (song.Source == null) return;
            if (Application.isPlaying) Object.Destroy(song.Source.gameObject);
            else Object.DestroyImmediate(song.Source.gameObject);
        }

        internal void StopAllNow()
        {
            if (_mainSong != null) Release(_mainSong);
            foreach (var s in _fadingSongs) Release(s);
            _mainSong = null;
            _fadingSongs.Clear();
        }
    }
}
