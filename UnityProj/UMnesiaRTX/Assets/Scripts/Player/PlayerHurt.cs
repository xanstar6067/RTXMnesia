using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>eLuxDamageType (LuxTypes.h); the script type string: "claws", "slash", anything else BloodSplat.</summary>
    public enum DamageType { BloodSplat, Claws, Slash }

    /// <summary>
    /// cLuxPlayerHudEffect (LuxPlayerHelpers.cpp): damage splashes (graphics/hud/damage_&lt;type&gt;N.tga, two copies
    /// that fade, then slide and stretch down) and the colored HUD flash. Drawn by <see cref="HplHud"/> on the
    /// virtual 800×600 screen.
    /// </summary>
    public sealed class PlayerHudEffect
    {
        public struct Splash
        {
            public Texture2D image;
            public Vector2 pos, size, posVel, sizeVel;
            public float alpha, alphaMul, alphaVel, alphaMoveStart;
        }

        readonly List<Splash> _splashes = new();
        public IReadOnlyList<Splash> Splashes => _splashes;

        public Color FlashColor { get; private set; }
        public float FlashAlpha { get; private set; }
        public bool FlashActive { get; private set; }
        float _flashAlphaSpeed, _flashAlphaOutSpeed;

        public void Reset()
        {
            _splashes.Clear();
            FlashAlpha = 0f;
            FlashActive = false;
        }

        public void AddDamageSplash(DamageType type)
        {
            var images = HplHudEffectAssets.Instance != null ? HplHudEffectAssets.Instance.DamageImages(type) : null;
            if (images == null || images.Length == 0) return;
            var image = images[HplMath.RandRectl(0, images.Length - 1)];
            if (image == null) return;
            var size = new Vector2(image.width, image.height);
            // cMath::RandRectVector2f(0, mvHudVirtualCenterSize - size): x and y in this order.
            float x = HplMath.RandRectf(0f, 800f - size.x);
            float y = HplMath.RandRectf(0f, 600f - size.y);

            for (int i = 0; i < 2; i++)
            {
                _splashes.Add(new Splash
                {
                    image = image,
                    pos = new Vector2(x, y),
                    size = size,
                    alpha = 1f,
                    alphaMul = i == 0 ? 0.3f : 0.7f,
                    posVel = new Vector2(0f, i == 0 ? 1f : 6f),
                    sizeVel = new Vector2(0f, i == 0 ? 8f : 16f),
                    alphaVel = 0.4f,
                    alphaMoveStart = 0.8f,
                });
            }
        }

        /// <summary>Flash(color, material, in, out): the material is Alpha in every call of the game.</summary>
        public void Flash(Color color, float inTime, float outTime)
        {
            FlashColor = color;
            FlashAlpha = 0f;
            _flashAlphaSpeed = inTime == 0f ? 100000f : 1f / inTime;
            _flashAlphaOutSpeed = outTime == 0f ? -100000f : -1f / outTime;
            FlashActive = true;
        }

        public void Update(float timeStep)
        {
            for (int i = 0; i < _splashes.Count;)
            {
                var s = _splashes[i];
                if (s.alpha < s.alphaMoveStart)
                {
                    s.pos += s.posVel * timeStep;
                    s.size += s.sizeVel * timeStep;
                }
                s.alpha -= s.alphaVel * timeStep;
                if (s.alpha < 0f)
                {
                    _splashes.RemoveAt(i);
                    continue;
                }
                _splashes[i] = s;
                i++;
            }

            if (!FlashActive) return;
            FlashAlpha += _flashAlphaSpeed * timeStep;
            if (FlashAlpha > 1f && _flashAlphaSpeed > 0f)
            {
                FlashAlpha = 1f;
                _flashAlphaSpeed = _flashAlphaOutSpeed;
            }
            if (FlashAlpha < 0f && _flashAlphaSpeed < 0f)
            {
                FlashAlpha = 0f;
                FlashActive = false;
            }
        }
    }

    /// <summary>
    /// cLuxPlayerHurt (LuxPlayerHelpers.cpp), game.cfg Player_General Hurt_* / HealthRegain*: health regain below the
    /// limit; below Hurt_EffectStartHealth a red noise over the screen (hud_hurt_noise0N.dds, modulative), slower
    /// movement and panting (head up and down with "hurt_pant") after moving.
    /// </summary>
    public sealed class PlayerHurt
    {
        const int NoiseCount = 8;
        readonly AmnesiaPlayer _player;
        readonly float _effectStartHealth, _minMoveMul, _maxPantCount, _pantSpeed, _pantSize;
        readonly float _healthRegainSpeed, _healthRegainLimit, _noiseFreq;

        float _pantCount, _pantPosAdd, _pantPosAddVel, _pantPosAddDir = 1f;
        float _noiseUpdateCount;

        /// <summary>The noise image (0..7) and the effect strength, for the HUD.</summary>
        public int CurrentNoise { get; private set; }
        public float Alpha { get; private set; }
        public float MoveSpeedMul { get; private set; } = 1f;

        public PlayerHurt(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            _effectStartHealth = cfg.GetFloat("Player_General", "Hurt_EffectStartHealth", 0f);
            _minMoveMul = cfg.GetFloat("Player_General", "Hurt_MinSpeedMul", 0f);
            _maxPantCount = cfg.GetFloat("Player_General", "Hurt_MaxPantCount", 0f);
            _pantSpeed = cfg.GetFloat("Player_General", "Hurt_PantSpeed", 0f);
            _pantSize = cfg.GetFloat("Player_General", "Hurt_PantSize", 0f);
            _healthRegainSpeed = cfg.GetFloat("Player_General", "HealthRegainSpeed", 0f);
            _healthRegainLimit = cfg.GetFloat("Player_General", "HealthRegainLimit", 0f);
            // Hurt_NoiseAlpha and Hurt_NoiseColor are read by the original but OnDraw does not use them.
            _noiseFreq = cfg.GetFloat("Player_General", "Hurt_NoiseFreq", 0f);
        }

        public void Reset()
        {
            Alpha = 0f;
            _pantCount = 0f;
            _pantPosAdd = 0f;
            _pantPosAddVel = 0f;
            _pantPosAddDir = 1f;
            CurrentNoise = 0;
            _noiseUpdateCount = 0f;
        }

        public void Update(float timeStep)
        {
            // Health regain
            if (_player.health < _healthRegainLimit) _player.AddHealth(_healthRegainSpeed * timeStep);

            // Check if update is needed
            if (Alpha <= 0f && _player.health > _effectStartHealth) return;
            if (_player.IsDead) return;

            float wantedAlpha = Mathf.Clamp01(1f - _player.health / _effectStartHealth);
            if (Alpha < wantedAlpha) Alpha = Mathf.Min(Alpha + timeStep, wantedAlpha);
            else if (Alpha > wantedAlpha) Alpha = Mathf.Max(Alpha - timeStep, wantedAlpha);

            MoveSpeedMul = _minMoveMul * Alpha + (1f - Alpha);

            // Noise
            _noiseUpdateCount -= timeStep;
            if (_noiseUpdateCount <= 0f)
            {
                int noiseMax = NoiseCount - 1;
                CurrentNoise += HplMath.RandRectl(1, noiseMax);
                if (CurrentNoise > noiseMax) CurrentNoise -= noiseMax + 1;
                _noiseUpdateCount = 1f / _noiseFreq;
            }

            // Pant count
            float speed = _player.Body.GetVelocity(timeStep).magnitude;
            if (speed < 0.05f)
            {
                if (_pantCount > 0f) _pantCount -= timeStep;
            }
            else
            {
                _pantCount += timeStep;
                float max = _maxPantCount * Alpha;
                if (_pantCount > max) _pantCount = max;
            }

            // Panting
            if (_pantCount > 0f && speed < 0.05f)
            {
                _pantPosAddVel = Mathf.Clamp(_pantPosAddVel + _pantPosAddDir * timeStep, -1f, 1f);
                _pantPosAdd += timeStep * _pantPosAddVel * _pantSpeed;
                if (_pantPosAddDir > 0f)
                {
                    if (_pantPosAdd > _pantSize)
                    {
                        _pantPosAddDir = -_pantPosAddDir;
                        _player.sound?.PlayGuiSoundData("hurt_pant", SoundEntryType.Gui, Mathf.Min(Alpha + 0.3f, 1f));
                    }
                }
                else if (_pantPosAdd < -_pantSize * 0.5f) _pantPosAddDir = -_pantPosAddDir;
            }
            else if (_pantPosAdd != 0f)
            {
                float dir = _pantPosAdd > 0f ? -1f : 1f;
                _pantPosAddVel += dir * timeStep;
                _pantPosAddDir = 1f;
                float max = Mathf.Abs(_pantPosAdd / _pantSize);
                _pantPosAddVel = Mathf.Clamp(_pantPosAddVel, -max, max);
                _pantPosAdd += timeStep * _pantPosAddVel * _pantSpeed;
                if ((dir > 0f && _pantPosAdd > 0f) || (dir < 0f && _pantPosAdd < 0f)) _pantPosAdd = 0f;
            }

            _player.SetHeadPosAdd(HeadPosAdd.Hurt, new Vector3(0f, _pantPosAdd, 0f));
        }
    }

    /// <summary>
    /// cLuxPlayerDeath (LuxPlayerHelpers.cpp), game.cfg Player_General Death_*: the head sinks and rolls, music
    /// stops, the world sound fades and slows (global volume id Death = 2), the screen fades out (modulative) and the
    /// death hint appears; after 5.5 s (or a key) the player is reset (health 50, sanity at least 40), put back at
    /// the check point (<see cref="HplMap.LoadCheckPoint"/>) with a white flash, and the screen fades in.
    /// Hard mode and perma death (states 4 / 5) are not used by the game modes of this port.
    /// </summary>
    public sealed class PlayerDeath
    {
        public const int GlobalVolumeId = 2; // eLuxGlobalVolumeType_Death
        readonly AmnesiaPlayer _player;
        readonly float _heightAddGoal, _heightAddGoalCrouch, _fadeOutTime, _heightAddSpeed, _rollSpeed;
        readonly string _startSound, _awakenSound;

        float _heightAdd, _minHeightAdd, _roll, _textOnScreenCount, _whiteCount;
        bool _skipStartSound;
        int _state;
        readonly bool _permaDeath;
        SoundEntry _permaVoice;
        int _permaVoiceId;
        public string PermaDeathSound { get; set; } = "";
        public bool Terminal { get; private set; }

        public bool Active { get; private set; }
        public float FadeAlpha { get; private set; }
        public float TextAlpha { get; private set; }
        /// <summary>Time since the start, for the text wave.</summary>
        public float T { get; private set; }
        public string CurrentHintText { get; private set; } = "";
        /// <summary>User setting Game ShowDeathHints (no option in this port's settings yet).</summary>
        public bool ShowHint { get; set; } = true;
        public string HintCat { get; private set; } = "";
        public string HintEntry { get; private set; } = "";
        public int State => _state;

        public PlayerDeath(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            // Edit-mode simulation initializes the player before HplMap.Build; use its scene's context too.
            foreach (var map in UnityEngine.Object.FindObjectsByType<HplMap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (map.gameObject.scene == player.gameObject.scene) { _permaDeath = map.Campaign.IsJustine; break; }
            _heightAddGoal = cfg.GetFloat("Player_General", "Death_HeightAdd", 0f);
            _heightAddGoalCrouch = cfg.GetFloat("Player_General", "Death_HeightAddCrouch", 0f);
            _fadeOutTime = cfg.GetFloat("Player_General", "Death_FadeTime", 1000f);
            _heightAddSpeed = cfg.GetFloat("Player_General", "Death_HeightAddSpeed", 0f);
            _rollSpeed = cfg.GetFloat("Player_General", "Death_RollSpeed", 0f);
            _startSound = cfg.GetString("Player_General", "Death_StartSound", "");
            _awakenSound = cfg.GetString("Player_General", "Death_AwakenSound", "");
            // cLuxPlayerDeath: Justine falls faster and fades out in half the time.
            if (_permaDeath) { _fadeOutTime /= 2f; _heightAddSpeed *= 1.2f; _rollSpeed *= 1.2f; }
            // Death_Max/Min*Gain are only used by commented out code of the original (OnPressButton).
        }

        public void Reset()
        {
            PermaDeathSound = "";
            _permaVoice = null;
            Terminal = false;
            _heightAdd = 0f;
            _roll = 0f;
            TextAlpha = 0f;
            _whiteCount = 0f;
            _skipStartSound = false;
            CurrentHintText = "";
            HintCat = "";
            HintEntry = "";
            FadeAlpha = 0f;
            _textOnScreenCount = 0f;
            _state = 0;
            T = 0f;
            Active = false;
        }

        public void DisableStartSound() => _skipStartSound = true;

        public void SetHint(string cat, string entry)
        {
            HintCat = cat ?? "";
            HintEntry = entry ?? "";
        }

        public void Start()
        {
            if (Active) return;
            Active = true;

            // not ported: progress log; input handler state (the input of a dead player goes to OnPressButton).
            _player.States?.ChangeState(PlayerStateType.Normal);
            // ChangeMoveState(Normal): the normal move state is the only one of this port.
            _player.Lantern?.SetActive(false, false, false);
            _player.SetCurrentHandObjectDrawn(false);
            _player.InsanityCollapseHelper?.Stop();

            var hud = HplMap.Current != null ? HplMap.Current.hud : null;
            string Translate(string cat, string entry) => hud != null ? hud.Translate(cat, entry) : entry;
            CurrentHintText = HintCat != "" ? Translate(HintCat, HintEntry) : Translate("Hints", "DefaultDeath");

            var sound = _player.sound;
            if (sound != null)
            {
                var music = sound.Music;
                if (music != null)
                    for (int i = 0; i <= music.GetMaxPrio(); i++) music.Stop(0.2f, i);
                if (!_skipStartSound) sound.PlayGuiSoundData(_startSound, SoundEntryType.World);
                sound.FadeGlobalVolume(0f, 0.15f, SoundEntryType.World, GlobalVolumeId, false);
                sound.FadeGlobalSpeed(0.5f, 0.125f, SoundEntryType.World, GlobalVolumeId, false);
            }
            _skipStartSound = false;

            ScriptApi.HplMap.Current?.BroadcastEnemyMessage(Enemies.EnemyMessage.PlayerDead, false, Vector3.zero, 0f);

            _heightAdd = 0f;
            _roll = 0f;
            _minHeightAdd = _player.MoveState != null && _player.MoveState.Crouching ? _heightAddGoalCrouch : _heightAddGoal;
            _state = 0;
            FadeAlpha = 0f;
            _whiteCount = 0f;
        }

        public void Update(float timeStep)
        {
            if (!Active) return;
            T += timeStep;

            // Height add and roll
            if (_state == 0 || _state == 1)
            {
                if (_heightAdd > _minHeightAdd)
                {
                    _heightAdd -= _heightAddSpeed * timeStep;
                    if (_heightAdd < _minHeightAdd) _heightAdd = _minHeightAdd;
                    _player.SetHeadPosAdd(HeadPosAdd.Death, new Vector3(0f, _heightAdd, 0f));
                    _state = 1;
                }
                _roll += _rollSpeed * Mathf.Deg2Rad * timeStep;
                if (_roll > 65f * Mathf.Deg2Rad) _roll = 65f * Mathf.Deg2Rad;
                _player.FadeRollTo(_roll, 10f, 10f);
            }

            // Fade out
            if (_state == 1)
            {
                FadeAlpha += timeStep * (1f / _fadeOutTime);
                if (FadeAlpha > 1f)
                {
                    FadeAlpha = 1f;
                    if (_permaDeath)
                    {
                        _state = 4;
                        if (_player.sound != null && _player.sound.PlayGuiSoundData(PermaDeathSound, SoundEntryType.Gui, out _, out _permaVoice,
                                1f, SoundEntityType.Main, true)) _permaVoiceId = _permaVoice.Id;
                    }
                }
                if (FadeAlpha > 0.75f && !_permaDeath)
                {
                    TextAlpha += (1f - TextAlpha) * 0.5f * timeStep;
                    if (TextAlpha > 1f) TextAlpha = 1f;
                }
                if (TextAlpha > 0.9f && FadeAlpha == 1f)
                {
                    _textOnScreenCount += timeStep;
                    if (_textOnScreenCount > 5.5f || !ShowHint)
                    {
                        _state = 2;
                        ResetGame();
                        HplMap.Current?.effects?.Flash.Start(0.7f, 1.2f, 2.5f);
                        _player.sound?.PlayGuiSoundData(_awakenSound, SoundEntryType.Gui);
                    }
                }
            }

            // cLuxPlayerDeath state 4 exits the application after the selected voice, with no checkpoint reset.
            if (_state == 4 && !Terminal && (_player.sound == null || !_player.sound.IsValid(_permaVoice, _permaVoiceId)))
            {
                Terminal = true;
                if (Application.isPlaying)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }

            // Fade to white
            if (_state == 2)
            {
                _whiteCount += timeStep;
                if (_whiteCount >= 0.5f) _state = 3;
            }

            // Fade in
            if (_state == 3)
            {
                TextAlpha -= timeStep * 0.85f;
                FadeAlpha -= timeStep * 0.75f;
                if (FadeAlpha < 0f)
                {
                    FadeAlpha = 0f;
                    Active = false;
                    // Keep the current hint.
                    string cat = HintCat, entry = HintEntry;
                    Reset();
                    HintCat = cat;
                    HintEntry = entry;
                }
            }
        }

        /// <summary>A key while dead: skips the rest of the text once the screen is black.</summary>
        public void OnPressButton()
        {
            if (!Active || FadeAlpha < 1f) return;
            _textOnScreenCount = 100f;
        }

        void ResetGame()
        {
            _player.SetRoll(0f);
            _player.SetLook(_player.Yaw, 0f);
            _player.SetHeadPosAdd(HeadPosAdd.Death, Vector3.zero);
            _player.SetCurrentHandObjectDrawn(true);
            _player.Body.ForceVelocity = Vector3.zero;
            _player.Body.SetMoveSpeed(CharDir.Forward, 0f);
            _player.Body.SetMoveSpeed(CharDir.Right, 0f);

            _player.SetHealth(50f);
            if (_player.sanity < 40f) _player.SetSanity(40f);

            var sound = _player.sound;
            if (sound != null)
            {
                sound.FadeGlobalVolume(1f, 1f, SoundEntryType.World, GlobalVolumeId, false);
                sound.FadeGlobalSpeed(1f, 0.5f, SoundEntryType.World, GlobalVolumeId, false);
            }

            HplMap.Current?.LoadCheckPoint();
        }
    }
}
