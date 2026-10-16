using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// cLuxPlayerSanity (LuxPlayerHelpers.cpp), numbers from game.cfg Player_Sanity:
    /// - hit (GiveSanityDamage): sanity_damage sound, zoom in (image trail, FOV and aspect) and back out;
    /// - losing sanity (LowerSanity with the effect): heartbeat and drain sounds that speed up as sanity falls;
    /// - insanity visuals below SanityEffectsStart: the wave / zoom screen distortion (HplEffects.Insanity);
    /// - very low sanity: SanityLow hint every 3 s, image trail 1.6, panting;
    /// - sanity regains SanityRegainSpeed/s up to SanityRegainLimit; after SanityLowLimitMaxTime s below
    ///   SanityLowLimit it jumps to SanityLowNewSanityAmount;
    /// - enemies and critters in view lower it (UpdateCheckEnemySeen; none are ported yet, so nothing is seen).
    /// </summary>
    public sealed class PlayerSanity
    {
        readonly AmnesiaPlayer _player;
        readonly float _hitZoomInSpeed, _hitZoomOutSpeed, _hitZoomInFOVMul, _hitZoomInAspectMul;
        readonly float _sanityRegainSpeed, _sanityRegainLimit, _sanityVeryLowLimit, _sanityEffectsStart;
        readonly float _sanityWaveAlphaMul, _sanityWaveSpeedMul;
        readonly float _sanityLowLimit, _sanityLowLimitMaxTime, _sanityLowNewSanityAmount;
        readonly float _checkNearEnemyInterval;

        float _hitAlpha;
        bool _hitActive, _hitIsUpdated, _sanityLostIsUpdated, _sanityEffectUpdated;
        float _sanityLostCount, _pantCount = 1f, _checkEnemySeenCount, _t, _insaneWaveAlpha;
        float _sanityDrainCount, _sanityDrainVolume, _sanityHeartbeatCount;
        float _seenEnemyCount, _atLowSanityCount, _showHintTimer;
        bool _enemyIsSeen;

        public PlayerSanity(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            const string g = "Player_Sanity";
            _hitZoomInSpeed = cfg.GetFloat(g, "HitZoomInSpeed", 0f);
            _hitZoomOutSpeed = cfg.GetFloat(g, "HitZoomOutSpeed", 0f);
            _hitZoomInFOVMul = cfg.GetFloat(g, "HitZoomInFOVMul", 0f);
            _hitZoomInAspectMul = cfg.GetFloat(g, "HitZoomInAspectMul", 0f);
            _sanityRegainSpeed = cfg.GetFloat(g, "SanityRegainSpeed", 0f);
            _sanityRegainLimit = cfg.GetFloat(g, "SanityRegainLimit", 0f);
            _sanityVeryLowLimit = cfg.GetFloat(g, "SanityVeryLowLimit", 0f);
            _sanityEffectsStart = cfg.GetFloat(g, "SanityEffectsStart", 0f);
            _sanityWaveAlphaMul = cfg.GetFloat(g, "SanityWaveAlphaMul", 0f);
            _sanityWaveSpeedMul = cfg.GetFloat(g, "SanityWaveSpeedMul", 0f);
            _sanityLowLimit = cfg.GetFloat(g, "SanityLowLimit", 0f);
            _sanityLowLimitMaxTime = cfg.GetFloat(g, "SanityLowLimitMaxTime", 0f);
            _sanityLowNewSanityAmount = cfg.GetFloat(g, "SanityLowNewSanityAmount", 0f);
            _checkNearEnemyInterval = cfg.GetFloat(g, "CheckNearEnemyInterval", 0f);
        }

        public float HitAlpha => _hitAlpha;
        public float SanityDrainVolume => _sanityDrainVolume;

        /// <summary>cLuxPlayerSanity::StartHit.</summary>
        public void StartHit()
        {
            _hitActive = true;
            _player.sound?.PlayGuiSoundData("sanity_damage", SoundEntryType.Gui);
        }

        /// <summary>cLuxPlayerSanity::SetSanityLost.</summary>
        public void SetSanityLost() => _sanityLostCount = 1f;

        public void Update(float timeStep)
        {
            _t += timeStep;
            _hitIsUpdated = false;
            _sanityLostIsUpdated = false;

            if (_player.sanity < _sanityLowLimit)
            {
                _atLowSanityCount += timeStep;
                if (_atLowSanityCount > _sanityLowLimitMaxTime)
                {
                    _atLowSanityCount = 0f;
                    _player.SetSanity(_sanityLowNewSanityAmount);
                }
            }
            else if (_atLowSanityCount > 0f)
            {
                _atLowSanityCount -= timeStep;
                if (_atLowSanityCount < 0f) _atLowSanityCount = 0f;
            }

            UpdateCheckEnemySeen(timeStep);
            UpdateEnemySeenEffect(timeStep);
            UpdateHit(timeStep);
            UpdateInsaneEffects(timeStep);
        }

        static Effects.HplEffects Effects => HplMap.Current != null ? HplMap.Current.effects : null;

        void UpdateInsanityVisuals(float timeStep)
        {
            var effects = Effects;
            if (effects == null) return;
            var insanity = effects.Insanity;
            if (_player.sanity > _sanityEffectsStart && _sanityDrainVolume <= 0f && _insaneWaveAlpha <= 0f)
            {
                insanity.Active = false;
                return;
            }
            insanity.Active = true;

            float goalAlpha = 1f - _player.sanity / _sanityEffectsStart;
            if (goalAlpha < 0f) goalAlpha = 0f;
            if (goalAlpha < _insaneWaveAlpha)
            {
                _insaneWaveAlpha -= timeStep;
                if (_insaneWaveAlpha < goalAlpha) _insaneWaveAlpha = goalAlpha;
            }
            else
            {
                _insaneWaveAlpha += timeStep;
                if (_insaneWaveAlpha > goalAlpha) _insaneWaveAlpha = goalAlpha;
            }

            float zoomMul = (Mathf.Sin(_t * 2f) + 1f) * 0.5f * 0.4f + 0.6f;
            insanity.WaveAlpha = _insaneWaveAlpha * _sanityWaveAlphaMul;
            insanity.WaveSpeed = _insaneWaveAlpha * _sanityWaveSpeedMul;
            insanity.ZoomAlpha = _sanityDrainVolume * zoomMul;
        }

        void UpdateInsaneEffects(float timeStep)
        {
            if (_hitIsUpdated) return;
            var effects = Effects;

            if (_sanityEffectUpdated)
            {
                effects?.ImageTrail.FadeTo(0f, 1f);
                _player.FadeAspectMulTo(1f, 1f);
                _player.FadeFOVMulTo(1f, 1f);
                _sanityEffectUpdated = false;
            }

            UpdateInsanityVisuals(timeStep);

            if (_player.IsDead) return;
            UpdateLosingSanity(timeStep);
            UpdateLowSanity(timeStep);

            float sanity = _player.sanity;
            if (sanity < _sanityRegainLimit)
            {
                sanity += timeStep * _sanityRegainSpeed;
                _player.SetSanity(sanity);
            }
        }

        /// <summary>
        /// cLuxPlayerSanity::UpdateCheckEnemySeen: every CheckNearEnemyInterval s critters within 3 m and enemies in view
        /// (frustum and 2 of 5 rays) lower sanity. Critters and enemies are not ported yet; the seen state still ends
        /// the radial blur like the original.
        /// </summary>
        void UpdateCheckEnemySeen(float timeStep)
        {
            if (_checkEnemySeenCount > 0f)
            {
                _checkEnemySeenCount -= timeStep;
                return;
            }
            _checkEnemySeenCount = _checkNearEnemyInterval;

            bool seenEnemy = false;
            if (!seenEnemy && _enemyIsSeen)
            {
                _enemyIsSeen = false;
                Effects?.RadialBlur.FadeTo(0f, 0.12f / 2f);
            }
        }

        void UpdateHit(float timeStep)
        {
            if (_player.IsDead) return;
            if (_hitAlpha <= 0f && !_hitActive) return;
            _hitIsUpdated = true;

            if (_hitActive)
            {
                _hitAlpha += timeStep * _hitZoomInSpeed;
                if (_hitAlpha >= 1f)
                {
                    _hitAlpha = 1f;
                    _hitActive = false;
                }
            }
            else
            {
                _hitAlpha -= timeStep * _hitZoomOutSpeed;
                if (_hitAlpha < 0f) _hitAlpha = 0f;
            }

            Effects?.ImageTrail.FadeTo(_hitAlpha * 0.9f, 100f);
            _player.FadeAspectMulTo(1f - _hitAlpha * _hitZoomInAspectMul, 100f);
            _player.FadeFOVMulTo(1f - _hitAlpha * _hitZoomInFOVMul, 100f);
            _sanityEffectUpdated = true;
        }

        void UpdateEnemySeenEffect(float timeStep)
        {
            if (_enemyIsSeen)
            {
                if (_seenEnemyCount < 1f)
                {
                    _seenEnemyCount += timeStep * 0.3f;
                    if (_seenEnemyCount > 1f)
                    {
                        _seenEnemyCount = 1f;
                        var map = HplMap.Current;
                        if (map != null) map.hud.Hints.Add("EnemySeen", map.hud.Translate("Hints", "EnemySeen"), 0f);
                    }
                }
                float pulse = 0.5f + (Mathf.Sin(_t * 2.5f) * 0.5f + 0.5f) * 0.5f;
                var effects = Effects;
                if (effects != null)
                {
                    effects.RadialBlur.SetBlurStartDist(0.2f);
                    effects.RadialBlur.FadeTo(0.12f * _seenEnemyCount * pulse, 10f);
                }
            }
            else if (_seenEnemyCount > 0f)
            {
                _seenEnemyCount -= timeStep * 0.15f;
                if (_seenEnemyCount < 0f) _seenEnemyCount = 0f;
            }
        }

        void UpdateLosingSanity(float timeStep)
        {
            if (_sanityLostCount <= 0f)
            {
                _sanityDrainCount = 0f;
                _sanityHeartbeatCount = 0f;
                _sanityDrainVolume -= timeStep * 0.5f;
                if (_sanityDrainVolume < 0f) _sanityDrainVolume = 0f;
                return;
            }

            float sanity = _player.sanity;
            float normalizedSanity = sanity / 100f;
            _sanityLostIsUpdated = true;
            _sanityLostCount -= timeStep;

            _sanityDrainVolume += timeStep * 0.1f;
            if (_sanityDrainVolume > 1f) _sanityDrainVolume = 1f;

            float speedMul = 1f + (1f - normalizedSanity) * 2f;

            _sanityHeartbeatCount += timeStep * speedMul * 0.1f;
            if (_sanityHeartbeatCount >= 1f)
            {
                _sanityHeartbeatCount = 0f;
                float volume = (1f - normalizedSanity * 0.5f) * _sanityDrainVolume;
                if (!_player.IsDead) _player.sound?.PlayGuiSoundData("sanity_heartbeat", SoundEntryType.Gui, volume);
            }

            _sanityDrainCount += timeStep * speedMul * 0.33f;
            if (_sanityDrainCount >= 1f)
            {
                _sanityDrainCount = 0f;
                string file = sanity > 75f ? "sanity_drain_low" : sanity > 50f ? "sanity_drain_med" : "sanity_drain_high";
                if (!_player.IsDead) _player.sound?.PlayGuiSoundData(file, SoundEntryType.Gui, _sanityDrainVolume);
            }
        }

        void UpdateLowSanity(float timeStep)
        {
            if (_player.sanity > _sanityVeryLowLimit) return;

            if (_showHintTimer <= 0f)
            {
                _showHintTimer = 3f;
                var map = HplMap.Current;
                if (map != null) map.hud.Hints.Add("SanityLow", map.hud.Translate("Hints", "SanityLow"), 0f);
            }
            else _showHintTimer -= timeStep;

            if (!_sanityLostIsUpdated)
            {
                Effects?.ImageTrail.FadeTo(1.6f, 3f);
                _sanityEffectUpdated = true;
            }

            if (_pantCount < 0f)
            {
                _pantCount = HplMath.RandRectf(0.5f, 5f);
                if (!_player.IsDead) _player.sound?.PlayGuiSoundData("sanity_pant", SoundEntryType.Gui);
            }
            else _pantCount -= timeStep;
        }
    }

    /// <summary>
    /// cLuxPlayerCamDirEffects: at sanity ≤ StartSwayMaxSanity the view lags — each look step becomes the average of
    /// the last SwayMaxSavedPositions steps (alpha fades in 0.1/s, out 0.2/s). The mouse turns the view every frame
    /// here, HPL2 once per 60 Hz step: a frame's input adds to the step's input, and the average is applied scaled by
    /// the frame's share of a step.
    /// </summary>
    public sealed class PlayerCamDirEffects
    {
        readonly AmnesiaPlayer _player;
        readonly float _startSwayMaxSanity;
        readonly int _maxPositions;
        readonly LinkedList<Vector2> _prevAdd = new();
        Vector2 _swayAdd, _nextAdd;
        bool _swayActive;
        float _swayAlpha;

        public PlayerCamDirEffects(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            _startSwayMaxSanity = cfg.GetFloat("Player_Sanity", "StartSwayMaxSanity", 0f);
            _maxPositions = cfg.GetInt("Player_Sanity", "SwayMaxSavedPositions", 0);
        }

        public float SwayAlpha => _swayAlpha;

        public void Update(float timeStep)
        {
            if (!_swayActive && _player.sanity <= _startSwayMaxSanity) _swayActive = true;
            else if (_swayActive && _player.sanity > _startSwayMaxSanity) _swayActive = false;

            if (_swayActive && _swayAlpha < 1f)
            {
                _swayAlpha += timeStep * 0.1f;
                if (_swayAlpha > 1f) _swayAlpha = 1f;
            }
            else if (!_swayActive && _swayAlpha > 0f)
            {
                _swayAlpha -= timeStep * 0.2f;
                if (_swayAlpha < 0f)
                {
                    _swayAlpha = 0f;
                    _prevAdd.Clear();
                    _nextAdd = Vector2.zero;
                    _swayAdd = Vector2.zero;
                }
            }
            UpdateSway();
        }

        /// <summary>AddAndGetYawAdd / AddAndGetPitchAdd for one frame's look input (x yaw, y pitch; steps = frame time × 60).</summary>
        public Vector2 AddAndGetLookAdd(Vector2 add, float steps)
        {
            Vector2 result = add;
            if (_prevAdd.Count > 0) result = _swayAdd * _swayAlpha * steps + add * (1f - _swayAlpha);
            if (_swayAlpha > 0f) _nextAdd += add;
            return result;
        }

        void UpdateSway()
        {
            if (!_swayActive && _prevAdd.Count == 0 && _swayAlpha <= 0f) return;
            _prevAdd.AddLast(_nextAdd);
            _nextAdd = Vector2.zero;
            if (_prevAdd.Count > _maxPositions) _prevAdd.RemoveFirst();
            Vector2 total = Vector2.zero;
            foreach (var v in _prevAdd) total += v;
            _swayAdd = total / _prevAdd.Count;
        }
    }

    /// <summary>
    /// cLuxPlayerInsanityCollapse: sanity below 0 makes the player fall (head down to InsanityCollapse_HeightAddGoal,
    /// roll up to 35°), sleep InsanityCollapse_SleepTime s with a loop and random sounds and world sound slowed and
    /// quieter, then wake with InsanityCollapse_AwakenSanity. Moving is slowed by the collapse speed multiplier.
    /// Enemies are alerted (step 6 / stage C enemies).
    /// </summary>
    public sealed class PlayerInsanityCollapse
    {
        /// <summary>eLuxGlobalVolumeType_InsanityCollapse.</summary>
        const int GlobalVolumeType = 4;

        readonly AmnesiaPlayer _player;
        readonly float _heightAddGoal, _heightAddCollapseSpeed, _heightAddAwakeSpeed, _rollCollapseSpeed, _rollAwakeSpeed;
        readonly float _sleepTime, _sleepSpeedMul, _wakeUpSpeedMul, _sleepLoopSoundVolume;
        readonly float _sleepRandomMinTime, _sleepRandomMaxTime, _awakenSanity;
        readonly string _startSound, _awakenSound, _sleepLoopSound, _sleepRandomSound;
        SoundEntry _loopSound;
        int _state;
        float _heightAdd, _roll, _t, _randomCount;

        public bool Active { get; private set; }
        public float SpeedMul { get; private set; } = 1f;

        public PlayerInsanityCollapse(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            const string g = "Player_General";
            _heightAddGoal = cfg.GetFloat(g, "InsanityCollapse_HeightAddGoal", 0f);
            _heightAddCollapseSpeed = cfg.GetFloat(g, "InsanityCollapse_HeightAddCollapseSpeed", 0f);
            _heightAddAwakeSpeed = cfg.GetFloat(g, "InsanityCollapse_HeightAddAwakeSpeed", 0f);
            _rollCollapseSpeed = cfg.GetFloat(g, "InsanityCollapse_RollCollapseSpeed", 0f);
            _rollAwakeSpeed = cfg.GetFloat(g, "InsanityCollapse_RollAwakeSpeed", 0f);
            _sleepTime = cfg.GetFloat(g, "InsanityCollapse_SleepTime", 0f);
            _sleepSpeedMul = cfg.GetFloat(g, "InsanityCollapse_SleepSpeedMul", 0f);
            _wakeUpSpeedMul = cfg.GetFloat(g, "InsanityCollapse_WakeUpSpeedMul", 0f);
            _startSound = cfg.GetString(g, "InsanityCollapse_StartSound", "");
            _awakenSound = cfg.GetString(g, "InsanityCollapse_AwakenSound", "");
            _sleepLoopSound = cfg.GetString(g, "InsanityCollapse_SleepLoopSound", "");
            _sleepLoopSoundVolume = cfg.GetFloat(g, "InsanityCollapse_SleepLoopSoundVolume", 0f);
            _sleepRandomSound = cfg.GetString(g, "InsanityCollapse_SleepRandomSound", "");
            _sleepRandomMinTime = cfg.GetFloat(g, "InsanityCollapse_SleepRandomMinTime", 0f);
            _sleepRandomMaxTime = cfg.GetFloat(g, "InsanityCollapse_SleepRandomMaxTime", 0f);
            _awakenSanity = cfg.GetFloat(g, "InsanityCollapse_AwakenSanity", 0f);
        }

        public void Start()
        {
            if (Active) return;
            var sound = _player.sound;
            sound?.PlayGuiSoundData(_startSound, SoundEntryType.World);

            _player.States?.ChangeState(PlayerStateType.Normal);
            _player.Lantern?.SetActive(false, false, false);
            SpeedMul = _sleepSpeedMul;

            if (sound != null)
            {
                sound.FadeGlobalVolume(0.75f, 0.15f, SoundEntryType.World, GlobalVolumeType, false);
                sound.FadeGlobalSpeed(0.5f, 0.125f, SoundEntryType.World, GlobalVolumeType, false);
                _loopSound = sound.PlayGui(_sleepLoopSound, true, 1f);
                _loopSound?.FadeIn(_sleepLoopSoundVolume, 0.2f);
            }
            // Enemies: AlertOfPlayerPresence (stage C).

            Active = true;
            _state = 0;
            _heightAdd = 0f;
            _roll = 0f;
            _t = 0f;
            _randomCount = 0f;
        }

        public void Stop()
        {
            if (!Active) return;
            var sound = _player.sound;
            if (sound != null)
            {
                sound.FadeGlobalVolume(1f, 0.3f, SoundEntryType.World, GlobalVolumeType, false);
                sound.FadeGlobalSpeed(1f, 0.5f, SoundEntryType.World, GlobalVolumeType, false);
            }
            SpeedMul = 1f;
            if (_loopSound != null && _loopSound.Alive) _loopSound.FadeOut(1f);
            Active = false;
            _state = 0;
            _t = 0f;
            _roll = 0f;
            _heightAdd = 0f;
            _player.FadeRollTo(0f, 10f, 10f);
            _player.MoveHeadPosAdd(HeadPosAdd.InsanityCollapse, Vector3.zero, 1f, 0.1f);
        }

        public void Update(float timeStep)
        {
            if (!Active) return;

            if (_state == 0 || _state == 1)
            {
                if (_heightAdd > _heightAddGoal)
                {
                    _heightAdd -= _heightAddCollapseSpeed * timeStep;
                    if (_heightAdd < _heightAddGoal)
                    {
                        _heightAdd = _heightAddGoal;
                        _state = 1;
                    }
                    _player.SetHeadPosAdd(HeadPosAdd.InsanityCollapse, new Vector3(0f, _heightAdd, 0f));
                }
                _roll += _rollCollapseSpeed * Mathf.Deg2Rad * timeStep;
                if (_roll > 35f * Mathf.Deg2Rad) _roll = 35f * Mathf.Deg2Rad;
                _player.FadeRollTo(_roll, 10f, 10f);
            }

            if (_state == 1)
            {
                _t += timeStep;
                _randomCount -= timeStep;
                if (_randomCount < 0f)
                {
                    _player.sound?.PlayGuiSoundData(_sleepRandomSound, SoundEntryType.Gui);
                    _randomCount = HplMath.RandRectf(_sleepRandomMinTime, _sleepRandomMaxTime);
                }

                if (_t > _sleepTime)
                {
                    var sound = _player.sound;
                    if (sound != null)
                    {
                        sound.FadeGlobalVolume(1f, 0.2f, SoundEntryType.World, GlobalVolumeType, false);
                        sound.FadeGlobalSpeed(1f, 0.25f, SoundEntryType.World, GlobalVolumeType, false);
                    }
                    SpeedMul = _wakeUpSpeedMul;
                    _player.FadeRollTo(0f, 5f, 6f);
                    if (_loopSound != null && _loopSound.Alive) _loopSound.FadeOut(0.3f);
                    _player.SetSanity(_awakenSanity);
                    sound?.PlayGuiSoundData(_awakenSound, SoundEntryType.World);
                    _state = 2;
                }
            }
            else if (_state == 2)
            {
                if (_heightAdd < 0f)
                {
                    _heightAdd += _heightAddAwakeSpeed * timeStep;
                    if (_heightAdd > 0f)
                    {
                        _heightAdd = 0f;
                        Active = false;
                        SpeedMul = 1f;
                        _player.crouchDisabled = false;
                        _player.jumpDisabled = false;
                    }
                    _player.SetHeadPosAdd(HeadPosAdd.InsanityCollapse, new Vector3(0f, _heightAdd, 0f));
                }
            }
        }
    }
}
