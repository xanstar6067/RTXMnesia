using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// cLuxPlayerLantern (LuxPlayerHelpers.cpp): the lantern is on while the player holds it (the "lantern" hand
    /// object), it burns Player_Lantern LowerOilSpeed oil per second (not during an emotion flash) and goes out with
    /// OutOfOilSound when the oil is gone. Turning it on needs the item (else the LanternNoItem hint), oil (else the
    /// LanternNoOil hint and sound) and a state that allows it (not on a ladder); the map's LanternLitCallback gets
    /// the new state. The helper's own point light of game.cfg (Radius 0.0001) lights nothing and is not created;
    /// the light comes from the hand object's lights. State: AmnesiaPlayer.lanternActive / lanternDisabled.
    /// </summary>
    public sealed class PlayerLantern
    {
        readonly AmnesiaPlayer _player;
        readonly string _turnOnSound, _turnOffSound, _outOfOilSound;
        readonly float _lowerOilSpeed;

        public PlayerLantern(AmnesiaPlayer player, Hpl.HplConfig cfg)
        {
            _player = player;
            _lowerOilSpeed = cfg.GetFloat("Player_Lantern", "LowerOilSpeed", 0f);
            _outOfOilSound = cfg.GetString("Player_Lantern", "OutOfOilSound", "");
            _turnOnSound = cfg.GetString("Player_Lantern", "TurnOnSound", "");
            _turnOffSound = cfg.GetString("Player_Lantern", "TurnOffSound", "");
        }

        public bool Active => _player.lanternActive;
        public bool Disabled => _player.lanternDisabled;

        /// <summary>cLuxPlayerLantern::Update: the oil.</summary>
        public void Update(float timeStep)
        {
            if (!Active) return;
            var map = HplMap.Current;
            if (map != null && map.effects != null && map.effects.EmotionFlash.Active) return;
            float oil = _player.lampOil - _lowerOilSpeed * timeStep;
            if (oil <= 0f)
            {
                oil = 0f;
                _player.sound?.PlayGuiSoundData(_outOfOilSound, Sound.SoundEntryType.Gui);
                SetActive(false, true);
            }
            _player.SetLampOil(oil);
        }

        /// <summary>cLuxPlayerLantern::SetActive.</summary>
        public void SetActive(bool active, bool useEffects, bool checkForOilAndItems = true, bool checkIfAllowed = true)
        {
            if (Active == active) return;
            if (checkIfAllowed && _player.States != null && !_player.States.Current.AllowLantern) return;

            var map = HplMap.Current;
            if (checkForOilAndItems && !HplGame.HasType("Lantern"))
            {
                if (map != null) map.hud.Hints.Add("LanternNoItem", map.hud.Translate("Hints", "LanternNoItem"), 0f);
                return;
            }
            if (active && Disabled) return;
            if (checkForOilAndItems && active && _player.lampOil <= 0f)
            {
                if (useEffects)
                {
                    if (map != null) map.hud.Hints.Add("LanternNoOil", map.hud.Translate("Hints", "LanternNoOil"), 0f);
                    _player.sound?.PlayGuiSoundData(_outOfOilSound, Sound.SoundEntryType.Gui);
                }
                return;
            }

            _player.lanternActive = active;
            if (useEffects) _player.sound?.PlayGuiSoundData(active ? _turnOnSound : _turnOffSound, Sound.SoundEntryType.Gui);
            _player.Hands?.SetActiveHandObject(active ? "lantern" : "");
            if (map != null && map.LanternLitCallback != "") map.RunScript(map.LanternLitCallback, active);
        }

        /// <summary>cLuxPlayerLantern::SetDisabled: disabling puts it out.</summary>
        public void SetDisabled(bool disabled)
        {
            _player.lanternDisabled = disabled;
            if (disabled) SetActive(false, true);
        }
    }
}
