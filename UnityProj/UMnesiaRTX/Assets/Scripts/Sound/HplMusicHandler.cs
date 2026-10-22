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
    /// game.cfg. Enemy music: danger music while an enemy is in range (priority 5), search and attack music of the
    /// enemies that registered for it (priorities 8 and 9).
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
        HplSoundBank _bank;
        Transform _parent;
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

        public HplMusicHandler(HplSoundHandler handler)
        {
            _bank = handler.bank;
            _parent = handler.transform;
        }

        // cLuxMusicHandler enemy music
        readonly List<Enemies.HplEnemy>[] _enemies = { new(), new() };
        readonly bool[] _enemyPlaying = new bool[EnemyMusicTypes];
        readonly float[] _enemyPlayCount = new float[EnemyMusicTypes], _enemyStopCount = new float[EnemyMusicTypes];
        bool _enemyClosePlaying;
        float _enemyCloseCount, _enemyGoneCount;

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

        /// <summary>cLuxMusicHandler::GetMaxPrio.</summary>
        public int GetMaxPrio() => MaxPrio;

        /// <summary>cLuxMusicHandler::GetMusic: the song of a priority (volume with <see cref="GameVolumeMul"/>).</summary>
        public (string file, bool loop, bool resume, float volume) GetMusic(int prio)
        {
            var m = _gameMusic[Mathf.Clamp(prio, 0, MaxPrio)];
            return (m.File ?? "", m.Loop, m.Resume, m.Volume);
        }

        /// <summary>
        /// cLuxMusicHandler::Reset: forgets the songs of all priorities and the resume positions (cMusicHandler::
        /// ResetResumeData); the playing song is not stopped. Enemy music state comes with step 6.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _gameMusic.Length; i++) _gameMusic[i] = default;
            _currentMaxPrio = -1;
            for (int i = 0; i < EnemyMusicTypes; i++)
            {
                _enemies[i].Clear();
                _enemyPlaying[i] = false;
                _enemyPlayCount[i] = 0f;
                _enemyStopCount[i] = 0f;
            }
            _resumePositions.Clear();
            _enemyClosePlaying = false;
            _enemyCloseCount = 0f;
            _enemyGoneCount = 0f;
        }

        /// <summary>cLuxMusicHandler::AddEnemy (a set: an enemy is in it once).</summary>
        public void AddEnemy(Enemies.EnemyMusic type, Enemies.HplEnemy enemy)
        {
            if (!_enemies[(int)type].Contains(enemy)) _enemies[(int)type].Add(enemy);
        }

        public void RemoveEnemy(Enemies.EnemyMusic type, Enemies.HplEnemy enemy) => _enemies[(int)type].Remove(enemy);

        public bool EnemyExists(Enemies.EnemyMusic type, Enemies.HplEnemy enemy) => _enemies[(int)type].Contains(enemy);

        /// <summary>
        /// cLuxMusicHandler::UpdateDangerMusic: without enemy music, an active living enemy with the player in range
        /// for 2 s starts its danger music (highest DangerMusicPrio) at priority 5; 6 s with none stops it.
        /// </summary>
        void UpdateDangerMusic(float timeStep)
        {
            if (Dev.HplDebugSession.EnemiesPaused)
            {
                if (_enemyClosePlaying) Stop(1f, 5);
                _enemyClosePlaying = false;
                _enemyCloseCount = _enemyGoneCount = 0;
                return;
            }
            // HARDMODE: no danger music.
            if (ScriptApi.HplDifficulty.HardMode) return;
            for (int i = 0; i < EnemyMusicTypes; i++) if (_enemyPlaying[i]) return;
            var map = HplMap.Current;
            if (map == null) return;
            if (!_enemyClosePlaying)
            {
                string music = "";
                int highestPrio = -1;
                foreach (var enemy in map.Enemies)
                {
                    if (enemy.Health <= 0f || !enemy.Active) continue;
                    if (enemy.PlayerInRange && enemy.DangerMusic != "" && (music == "" || highestPrio < enemy.DangerMusicPrio))
                    {
                        music = enemy.DangerMusic;
                        highestPrio = enemy.DangerMusicPrio;
                    }
                }
                if (music != "")
                {
                    _enemyGoneCount = 0f;
                    _enemyCloseCount += timeStep;
                    if (_enemyCloseCount > 2f)
                    {
                        Play(music, true, 1f, 6f, 5, true);
                        _enemyClosePlaying = true;
                    }
                }
            }
            else
            {
                bool found = false;
                foreach (var enemy in map.Enemies)
                {
                    if (enemy.Health <= 0f || !enemy.Active) continue;
                    if (enemy.PlayerInRange) found = true;
                }
                if (!found)
                {
                    _enemyCloseCount = 0f;
                    _enemyGoneCount += timeStep;
                    if (_enemyGoneCount > 6f)
                    {
                        Stop(6f, 5);
                        _enemyClosePlaying = false;
                    }
                }
            }
        }

        /// <summary>
        /// cLuxMusicHandler::UpdateEnemyMusic (not while a higher type plays): 0.1 s after enemies registered, the music
        /// of the one with the highest priority starts at 8 + type (the danger music stops; the original stops its own
        /// priority for each lower type instead of theirs); 1.2 s after the last one left it stops (5 s fade).
        /// </summary>
        void UpdateEnemyMusic(float timeStep, int type)
        {
            if (HplMap.Current == null) return;
            for (int i = type + 1; i < EnemyMusicTypes; i++) if (_enemyPlaying[i]) return;

            var set = _enemies[type];
            int count = Dev.HplDebugSession.EnemiesPaused ? 0 : set.Count;
            if (!_enemyPlaying[type] && count > 0)
            {
                _enemyStopCount[type] = 0f;
                _enemyPlayCount[type] += timeStep;
                if (_enemyPlayCount[type] > 0.1f)
                {
                    string music = "";
                    int maxPrio = -9999;
                    foreach (var enemy in set)
                    {
                        var t = (Enemies.EnemyMusic)type;
                        if (enemy.GetMusic(t) != "" && enemy.GetMusicPrio(t) > maxPrio)
                        {
                            music = enemy.GetMusic(t);
                            maxPrio = enemy.GetMusicPrio(t);
                        }
                    }
                    if (music != "")
                    {
                        if (_enemyClosePlaying)
                        {
                            _enemyClosePlaying = false;
                            Stop(2f, 5);
                        }
                        for (int i = 0; i < type; i++)
                        {
                            _enemyPlaying[i] = false;
                            Stop(2f, 8 + type);
                        }
                        Play(music, true, 1f, 1.5f, 8 + type, true);
                        _enemyPlaying[type] = true;
                        _enemyPlayCount[type] = 0f;
                    }
                }
            }
            else if (_enemyPlaying[type] && count == 0)
            {
                _enemyPlayCount[type] = 0f;
                _enemyStopCount[type] += timeStep;
                if (_enemyStopCount[type] > 1.2f)
                {
                    _enemyPlaying[type] = false;
                    Stop(5f, 8 + type);
                    _enemyStopCount[type] = 0f;
                }
            }
        }

        public bool EnemyMusicPlaying(Enemies.EnemyMusic type) => _enemyPlaying[(int)type];
        public bool DangerMusicPlaying => _enemyClosePlaying;

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
            // cLuxMusicHandler::Update: nothing while the player is dead.
            var player = HplMap.Current != null ? HplMap.Current.player : null;
            if (player != null && player.IsDead) return;
            UpdateDangerMusic(timeStep);
            UpdateEnemyMusic(timeStep, (int)Enemies.EnemyMusic.Search);
            UpdateEnemyMusic(timeStep, (int)Enemies.EnemyMusic.Attack);
            if (_currentMaxPrio >= 0 && _mainSong == null)
            {
                _gameMusic[_currentMaxPrio].File = "";
                _currentMaxPrio = -1;
                PlayHighestPriority();
            }
        }

        // ------------------------------------------------------------------ cMusicHandler

        /// <summary>cMusicHandler::Play called directly (the credits): no priorities, no game volume.</summary>
        public void PlayDirect(string file, float volume, float fadeStepSize, bool loop, bool resume) => PlaySong(file, volume, fadeStepSize, loop, resume);

        /// <summary>cMusicHandler::Stop called directly (the credits).</summary>
        public void StopDirect(float fadeStepSize) => StopSong(fadeStepSize);

        /// <summary>The engine part only (cMusicHandler::Update): songs and fades, while cLuxMusicHandler is not updated.</summary>
        public void UpdateEngine(float timeStep) => UpdateSongs(timeStep);

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

        void UpdateSongs(float timeStep, bool advanceClock = true)
        {
            if (_paused) return;
            if (_volumeMul < _volumeMulFadeGoal) _volumeMul = Mathf.Min(_volumeMul + timeStep * _volumeMulFadeSpeed, _volumeMulFadeGoal);
            else if (_volumeMul > _volumeMulFadeGoal) _volumeMul = Mathf.Max(_volumeMul - timeStep * _volumeMulFadeSpeed, _volumeMulFadeGoal);

            if (_mainSong != null)
            {
                if (advanceClock) _mainSong.Elapsed += timeStep;
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
                if (advanceClock) song.Elapsed += timeStep;
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
            var clip = _bank != null ? _bank.GetClip(file) : null;
            if (clip == null)
            {
                HplLog.Error($"Couldn't load music '{file}'");
                return null;
            }
            var go = new GameObject("Music " + file) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_parent, false);
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

        /// <summary>
        /// The music handler is one for the whole game (cMusicHandler, cLuxMusicHandler): a map change moves it,
        /// with its playing sources, to the next map's sound handler (or, while the scenes switch, to a node that
        /// survives the load).
        /// </summary>
        internal void MoveTo(Transform parent)
        {
            _parent = parent;
            if (_mainSong?.Source != null) _mainSong.Source.transform.SetParent(parent, false);
            foreach (var s in _fadingSongs)
                if (s.Source != null) s.Source.transform.SetParent(parent, false);
        }

        /// <summary>cLuxMusicHandler::OnMapEnter: the enemies are forgotten, enemy music stops (priorities 8 and up).</summary>
        public void OnMapEnter()
        {
            for (int i = 0; i < EnemyMusicTypes; i++)
            {
                _enemies[i].Clear();
                Stop(2f, 8 + i);
            }
        }

        /// <summary>
        /// cLuxMusicHandler::OnMapLeave: enemy music stops, then 60 updates of 0.1 s finish the fades (the streams
        /// play on by themselves, so only volumes move, not the play clocks).
        /// </summary>
        public void OnMapLeave()
        {
            OnMapEnter();
            for (int i = 0; i < 60; i++)
            {
                UpdateSongs(0.1f, false);
                if (_currentMaxPrio >= 0 && _mainSong == null)
                {
                    _gameMusic[_currentMaxPrio].File = "";
                    _currentMaxPrio = -1;
                    PlayHighestPriority();
                }
            }
        }

        /// <summary>cLuxMusicHandler_SaveData::FromMusicHandler (enemies by id are not kept: the enemies of a loaded map re-register).</summary>
        public RTXMnesia.Save.MusicSave CaptureSave()
        {
            var save = new RTXMnesia.Save.MusicSave
            {
                currentMaxPrio = _currentMaxPrio,
                enemyClosePlaying = _enemyClosePlaying,
                attackPlaying = _enemyPlaying[(int)Enemies.EnemyMusic.Attack],
                searchPlaying = _enemyPlaying[(int)Enemies.EnemyMusic.Search],
            };
            if (_mainSong != null)
            {
                save.currentMusic = _mainSong.File;
                save.currentMaxVolume = _mainSong.MaxVolume;
                save.currentVolume = _mainSong.Volume;
                save.currentVolumeAdd = _mainSong.VolumeAdd;
                save.currentTime = _mainSong.Elapsed;
                save.currentLoop = _mainSong.Loop;
            }
            foreach (var m in _gameMusic)
                save.music.Add(new RTXMnesia.Save.MusicSlotSave { file = m.File ?? "", loop = m.Loop, resume = m.Resume, volume = m.Volume });
            return save;
        }

        /// <summary>cLuxMusicHandler_SaveData::ToMusicHandler: the current song from its saved time, then the priorities.</summary>
        public void ApplySave(RTXMnesia.Save.MusicSave save)
        {
            if (!string.IsNullOrEmpty(save.currentMusic) && PlaySong(save.currentMusic, save.currentMaxVolume, 0.1f, save.currentLoop, false) && _mainSong != null)
            {
                _mainSong.MaxVolume = save.currentMaxVolume;
                _mainSong.Volume = save.currentVolume;
                _mainSong.VolumeAdd = save.currentVolumeAdd;
                _mainSong.Loop = save.currentLoop;
                _mainSong.Elapsed = save.currentTime;
                var clip = _mainSong.Source.clip;
                if (clip != null && clip.length > 0f)
                    _mainSong.Source.time = save.currentLoop ? save.currentTime % clip.length : Mathf.Min(save.currentTime, clip.length - 0.01f);
                _mainSong.Source.volume = _mainSong.Volume * _volumeMul;
            }
            _currentMaxPrio = save.currentMaxPrio;
            _enemyClosePlaying = save.enemyClosePlaying;
            _enemyPlaying[(int)Enemies.EnemyMusic.Attack] = save.attackPlaying;
            _enemyPlaying[(int)Enemies.EnemyMusic.Search] = save.searchPlaying;
            for (int i = 0; i < _gameMusic.Length && i < save.music.Count; i++)
            {
                var m = save.music[i];
                _gameMusic[i] = new GameMusic { File = m.file ?? "", Loop = m.loop, Resume = m.resume, Volume = m.volume };
            }
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
