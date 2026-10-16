using System.Collections.Generic;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>cLuxPlayerFlashback (LuxPlayerHelpers.cpp): flashes, voices, world volume and queued flashbacks.</summary>
    public sealed class PlayerFlashback
    {
        readonly AmnesiaPlayer _player;
        readonly HplFlashbackAssets _assets;
        readonly float _radialBlurSize, _radialBlurStartDist, _worldSoundVolume, _moveSpeedMul, _runSpeedMul;
        readonly Queue<(string file, string callback)> _queue = new();
        float _flashDelay, _flashbackStartCount;
        string _flashbackFile, _callback;

        // eLuxGlobalVolumeType_Flashback.
        const int GlobalVolumeTypeFlashback = 1;

        public bool Active { get; private set; }
        /// <summary>cLuxDebugHandler::GetDisableFlashBacks (the debug menu is not ported yet).</summary>
        public bool Disabled { get; set; }

        public PlayerFlashback(AmnesiaPlayer player, HplConfig cfg)
        {
            _player = player;
            const string g = "Player_General";
            _radialBlurSize = cfg.GetFloat(g, "FlashbackRadialBlurSize", 0.12f);
            _radialBlurStartDist = cfg.GetFloat(g, "FlashbackRadialBlurStartDist", 0.4f);
            _worldSoundVolume = cfg.GetFloat(g, "FlashbackWorldSoundVolume", 0.4f);
            _moveSpeedMul = cfg.GetFloat(g, "FlashbackMoveSpeedMul", 1f);
            _runSpeedMul = cfg.GetFloat(g, "FlashbackRunSpeedMul", 1f);
            _assets = Resources.Load<HplFlashbackAssets>(HplFlashbackAssets.ResourceName);
            Reset();
        }

        /// <summary>cLuxPlayerFlashback::Reset.</summary>
        public void Reset()
        {
            _flashDelay = 0f;
            Active = false;
            _queue.Clear();
        }

        /// <summary>cLuxPlayerFlashback::Start.</summary>
        public void Start(string flashbackFile, string callback)
        {
            if (Disabled) return;
            // not ported: ProgLog; no progression log handler yet.
            if (Active)
            {
                _queue.Enqueue((flashbackFile, callback));
                return;
            }

            // Disable enemies
            ScriptApi.HplMap.Current?.BroadcastEnemyMessage(Enemies.EnemyMessage.Reset, false, Vector3.zero, 0f);
            _flashDelay = 0.5f;
            _player.sound?.PlayGuiSoundData("flashback_flash", SoundEntryType.Gui);
            // not ported: AddCompletionAmount; completion percentages are not implemented yet.
            _flashbackStartCount = 1.5f;
            _flashbackFile = flashbackFile;
            _callback = callback;
            _player.sound?.FadeGlobalVolume(_worldSoundVolume, (1f - _worldSoundVolume) / 1.5f,
                                            SoundEntryType.World, GlobalVolumeTypeFlashback, false);
            Active = true;
        }

        /// <summary>cLuxPlayerFlashback::Update.</summary>
        public void Update(float timeStep)
        {
            if (!Active)
            {
                if (_queue.Count > 0)
                {
                    // As in HPL2, the six-second count only advances while something is queued.
                    _flashbackStartCount -= timeStep;
                    if (_flashbackStartCount < 0f)
                    {
                        var data = _queue.Dequeue();
                        Start(data.file, data.callback);
                    }
                }
                return;
            }

            var map = HplMap.Current;
            var effects = map.effects;
            if (_flashDelay > 0f)
            {
                _flashDelay -= timeStep;
                // Keep HPL2's strict comparison (exactly zero does not trigger the flash).
                if (_flashDelay < 0f)
                {
                    effects.Flash.Start(0.5f, 0.5f, 2.5f);
                    _player.eventMoveSpeedMul = _moveSpeedMul;
                    _player.eventRunSpeedMul = _runSpeedMul;
                }
            }

            if (_flashbackStartCount > 0f)
            {
                _flashbackStartCount -= timeStep;
                if (_flashbackStartCount <= 0f)
                {
                    effects.Sepia.FadeTo(1f, 1f / 3.5f);
                    effects.RadialBlur.SetBlurStartDist(_radialBlurStartDist);
                    effects.RadialBlur.FadeTo(_radialBlurSize, _radialBlurSize / 3.5f);
                    LoadAndPlayFlashbackFile(_flashbackFile);
                }
            }
            else if (effects.PlayVoice.VoiceDonePlaying())
            {
                const float fadeTime = 4f;
                effects.Flash.Start(1f, 0.1f, 2.5f);
                effects.Sepia.FadeTo(0f, 1f / fadeTime);
                effects.RadialBlur.FadeTo(0f, _radialBlurSize / fadeTime);
                _player.sound?.FadeGlobalVolume(1f, 1f / fadeTime, SoundEntryType.World, GlobalVolumeTypeFlashback, true);
                _player.eventMoveSpeedMul = 1f;
                _player.eventRunSpeedMul = 1f;
                Active = false;
                _flashbackStartCount = 6f;
                if (!string.IsNullOrEmpty(_callback)) map.RunScript(_callback);
            }
        }

        /// <summary>cLuxPlayerFlashback::LoadAndPlayFlashbackFile.</summary>
        void LoadAndPlayFlashbackFile(string file)
        {
            var data = _assets != null ? _assets.Find(file) : null;
            if (data == null)
            {
                HplLog.Error($"Could not load flashback file: '{Path.ChangeExtension(file, "flash")}'");
                return;
            }
            var map = HplMap.Current;
            foreach (var voice in data.voices)
            {
                string text = !string.IsNullOrEmpty(voice.textCat) && !string.IsNullOrEmpty(voice.textEntry)
                    ? map.hud.Translate(voice.textCat, voice.textEntry) : "";
                map.effects.PlayVoice.AddVoice(voice.voiceSound, voice.effectSound, text, false, Vector3.zero, 0f, 0f);
            }
        }
    }
}
