using System.Collections.Generic;
using RTXMnesia.Player;
using RTXMnesia.ScriptApi;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace RTXMnesia.Effects
{
    /// <summary>
    /// Port of cLuxEffectHandler (LuxEffectHandler.cpp): fade, flashes, image trail, sepia, radial blur, screen
    /// shake and the effect voice queue. Updated in the map step (60 Hz), only effects that are active.
    /// Screen effects are drawn by <see cref="HplScreenEffectsPass"/> (after post-processing, before the HUD),
    /// voice subtitles by <see cref="Ui.HplHud"/>.
    /// Until the sound system (step 6) voices are not heard: each line lasts as long as its .ogg and shows its text.
    /// </summary>
    public class HplEffects : MonoBehaviour
    {
        [Tooltip("Hidden/RTXMnesia/HplScreenEffects")]
        public Shader screenEffectsShader;
        [Tooltip("textures/effects/colorconv_sepia.tga (1D lookup, imported linear).")]
        public Texture2D sepiaLookup;

        public HplMap Map { get; private set; }
        public bool PlayerIsPaused { get; private set; }

        public readonly FadeEffect Fade = new();
        public readonly FlashEffect Flash = new();
        public readonly SanityGainFlashEffect SanityGainFlash = new();
        public readonly PlayVoiceEffect PlayVoice = new();
        public readonly AmountEffect ImageTrail = new();
        public readonly ShakeScreenEffect ScreenShake = new();
        public readonly AmountEffect Sepia = new();
        public readonly RadialBlurEffect RadialBlur = new();
        public readonly EmotionFlashEffect EmotionFlash = new();

        readonly List<Effect> _effects = new();
        bool _initialized;

        public void Init(HplMap map)
        {
            if (_initialized) return;
            _initialized = true;
            Map = map;
            // Same order as the handler's list.
            _effects.AddRange(new Effect[] { Fade, Flash, SanityGainFlash, PlayVoice, ImageTrail, ScreenShake, Sepia, RadialBlur, EmotionFlash });
            foreach (var e in _effects) e.Handler = this;

            var cfg = map.gameConfig;
            if (cfg != null)
            {
                SanityGainFlash.Color = cfg.GetColor("Player_General", "SanityGain_Color", Color.black);
                SanityGainFlash.FadeInTime = cfg.GetFloat("Player_General", "SanityGain_FadeInTime", 0.25f);
                SanityGainFlash.FadeOutTime = cfg.GetFloat("Player_General", "SanityGain_FadeOutTime", 1.25f);
            }
            CreatePass();
        }

        /// <summary>
        /// The custom pass that draws the screen effects, on a global volume that is not saved with the scene
        /// (a child of this object, destroyed with it).
        /// </summary>
        void CreatePass()
        {
            if (screenEffectsShader == null) screenEffectsShader = Shader.Find(HplScreenEffectsPass.ShaderName);
            if (screenEffectsShader == null)
            {
                Debug.LogError("[RTXMnesia] Shader " + HplScreenEffectsPass.ShaderName + " not found: screen effects are off");
                return;
            }
            var go = new GameObject("HPL Screen Effects") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            var passVolume = go.AddComponent<CustomPassVolume>();
            passVolume.isGlobal = true;
            passVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
            var pass = (HplScreenEffectsPass)passVolume.AddPassOfType(typeof(HplScreenEffectsPass));
            pass.name = "HPL screen effects";
            pass.effects = this;
            pass.shader = screenEffectsShader;
        }

        public void SetPlayerIsPaused(bool paused)
        {
            PlayerIsPaused = paused;
            if (Map != null && Map.player != null) Map.player.active = !paused;
        }

        /// <summary>cLuxEffectHandler::Update.</summary>
        public void Step(float timeStep)
        {
            foreach (var e in _effects)
                if (e.Active) e.Update(timeStep);
        }

        /// <summary>cLuxEffectHandler::Reset (new game, map change).</summary>
        public void ResetAll()
        {
            foreach (var e in _effects)
            {
                e.Reset();
                e.Active = false;
            }
            PlayerIsPaused = false;
        }

        // ================================================================== effects

        public abstract class Effect
        {
            public HplEffects Handler;
            public bool Active;
            public abstract void Update(float timeStep);
            public virtual void Reset() { }
        }

        /// <summary>cLuxEffect_Fade: the screen is multiplied by 1 − alpha.</summary>
        public sealed class FadeEffect : Effect
        {
            public float Alpha;
            float _goalAlpha, _fadeSpeed = 1f;

            public void FadeIn(float time)
            {
                _goalAlpha = 0f;
                if (time <= 0f) Alpha = 0f;
                else _fadeSpeed = 1f / time;
                Active = true;
            }

            public void FadeOut(float time)
            {
                _goalAlpha = 1f;
                if (time <= 0f) Alpha = 1f;
                else _fadeSpeed = 1f / time;
                Active = true;
            }

            public bool IsFading => (_goalAlpha == 0f && Alpha > 0f) || (_goalAlpha == 1f && Alpha < 1f);

            public override void Update(float timeStep)
            {
                if (_goalAlpha == 0f && Alpha > 0f)
                {
                    Alpha -= timeStep * _fadeSpeed;
                    if (Alpha < 0f)
                    {
                        Alpha = 0f;
                        Active = false;
                    }
                }
                else if (_goalAlpha == 1f && Alpha < 1f)
                {
                    Alpha += timeStep * _fadeSpeed;
                    if (Alpha > 1f) Alpha = 1f;
                }
            }

            public override void Reset()
            {
                _goalAlpha = 0f;
                Alpha = 0f;
                _fadeSpeed = 1f;
            }
        }

        /// <summary>cLuxEffect_Flash: additive white, in → hold → out.</summary>
        public sealed class FlashEffect : Effect
        {
            public float Alpha;
            int _step;
            float _count, _fadeInSpeed, _whiteSpeed, _fadeOutSpeed;

            public void Start(float fadeIn, float white, float fadeOut)
            {
                Active = true;
                _step = 0;
                Alpha = 0f;
                if (fadeIn == 0f) fadeIn = 0.000001f;
                if (white == 0f) white = 0.000001f;
                if (fadeOut == 0f) fadeOut = 0.000001f;
                _fadeInSpeed = 1f / fadeIn;
                _whiteSpeed = 1f / white;
                _fadeOutSpeed = 1f / fadeOut;
            }

            public override void Update(float timeStep)
            {
                switch (_step)
                {
                    case 0:
                        Alpha += _fadeInSpeed * timeStep;
                        if (Alpha >= 1f)
                        {
                            Alpha = 1f;
                            _step = 1;
                            _count = 1f;
                        }
                        break;
                    case 1:
                        _count -= _whiteSpeed * timeStep;
                        if (_count <= 0f) _step = 2;
                        break;
                    case 2:
                        Alpha -= _fadeOutSpeed * timeStep;
                        if (Alpha <= 0f)
                        {
                            Alpha = 0f;
                            Active = false;
                        }
                        break;
                }
            }

            public override void Reset()
            {
                _step = 0;
                Alpha = 0f;
            }
        }

        /// <summary>cLuxEffect_SanityGainFlash: additive SanityGain_Color (game.cfg), 0.05 s at full.</summary>
        public sealed class SanityGainFlashEffect : Effect
        {
            public float Alpha;
            public Color Color = Color.black;
            public float FadeInTime = 0.25f, FadeOutTime = 1.25f;
            int _step;
            float _count;

            public void Start()
            {
                // TODO step 6 (sound): SanityGain_Sound.
                Active = true;
                _step = 0;
                Alpha = 0f;
            }

            public override void Update(float timeStep)
            {
                switch (_step)
                {
                    case 0:
                        Alpha += 1f / FadeInTime * timeStep;
                        if (Alpha >= 1f)
                        {
                            Alpha = 1f;
                            _step = 1;
                            _count = 1f;
                        }
                        break;
                    case 1:
                        _count -= 1f / 0.05f * timeStep;
                        if (_count <= 0f) _step = 2;
                        break;
                    case 2:
                        Alpha -= 1f / FadeOutTime * timeStep;
                        if (Alpha <= 0f)
                        {
                            Alpha = 0f;
                            Active = false;
                        }
                        break;
                }
            }

            public override void Reset() => Alpha = 0f;
        }

        /// <summary>
        /// cLuxEffect_ImageTrail and cLuxEffect_SepiaColor: an amount fading linearly to a goal; the post effect is
        /// on while the amount is above 0.
        /// </summary>
        public sealed class AmountEffect : Effect
        {
            public float Amount;
            float _goal, _speed;

            public void FadeTo(float amount, float speed)
            {
                _goal = amount;
                _speed = speed;
                Active = true;
            }

            public override void Update(float timeStep)
            {
                if (_goal < Amount)
                {
                    Amount -= _speed * timeStep;
                    if (Amount <= _goal)
                    {
                        Amount = _goal;
                        Active = false;
                    }
                }
                else
                {
                    Amount += _speed * timeStep;
                    if (Amount >= _goal)
                    {
                        Amount = _goal;
                        Active = false;
                    }
                }
            }

            public override void Reset()
            {
                Amount = 0f;
                _goal = 0f;
            }
        }

        /// <summary>cLuxEffect_RadialBlur: size fades like <see cref="AmountEffect"/>; blur starts at a distance from the center.</summary>
        public sealed class RadialBlurEffect : Effect
        {
            public float Size, StartDist;
            float _goal, _speed;

            public void FadeTo(float size, float speed)
            {
                _goal = size;
                _speed = speed;
                Active = true;
            }

            public void SetBlurStartDist(float dist) => StartDist = dist;

            public override void Update(float timeStep)
            {
                if (_goal < Size)
                {
                    Size -= _speed * timeStep;
                    if (Size <= _goal)
                    {
                        Size = _goal;
                        Active = false;
                    }
                }
                else
                {
                    Size += _speed * timeStep;
                    if (Size >= _goal)
                    {
                        Size = _goal;
                        Active = false;
                    }
                }
            }

            public override void Reset()
            {
                Size = 0f;
                _goal = 0f;
                StartDist = 0f;
            }
        }

        /// <summary>cLuxEffect_ShakeScreen: the largest running shake moves the head by a random offset each step.</summary>
        public sealed class ShakeScreenEffect : Effect
        {
            struct Shake
            {
                public float size, maxSize, time, fadeInTime, maxFadeInTime, fadeOutTime, maxFadeOutTime;
            }

            readonly List<Shake> _shakes = new();

            public void Start(float amount, float time, float fadeInTime, float fadeOutTime)
            {
                _shakes.Add(new Shake
                {
                    size = amount, maxSize = amount, time = time,
                    fadeInTime = fadeInTime, maxFadeInTime = fadeInTime,
                    fadeOutTime = fadeOutTime, maxFadeOutTime = fadeOutTime,
                });
                Active = true;
            }

            public override void Update(float timeStep)
            {
                float largest = 0f;
                var player = Handler.Map != null ? Handler.Map.player : null;
                for (int i = 0; i < _shakes.Count;)
                {
                    var s = _shakes[i];
                    if (s.fadeInTime > 0f)
                    {
                        s.fadeInTime = Mathf.Max(0f, s.fadeInTime - timeStep);
                        float t = s.fadeInTime / s.maxFadeInTime;
                        s.size = (1f - t) * s.maxSize;
                    }
                    else if (s.time > 0f)
                    {
                        s.time = Mathf.Max(0f, s.time - timeStep);
                        s.size = s.maxSize;
                    }
                    else
                    {
                        s.fadeOutTime = Mathf.Max(0f, s.fadeOutTime - timeStep);
                        float t = s.fadeOutTime / s.maxFadeOutTime;
                        s.size = t * s.maxSize;
                    }
                    if (largest < s.size) largest = s.size;

                    if (s.time <= 0f && s.fadeOutTime <= 0f && s.fadeInTime <= 0f)
                    {
                        _shakes.RemoveAt(i);
                        if (_shakes.Count == 0)
                        {
                            Active = false;
                            if (player != null) player.SetHeadPosAdd(HeadPosAdd.ScreenShake, Vector3.zero);
                            return;
                        }
                    }
                    else
                    {
                        _shakes[i] = s;
                        i++;
                    }
                }

                // A random offset in HPL2 space; the shake is symmetric, so the mirror does not matter.
                var add = new Vector3(Hpl.HplMath.RandRectf(-largest, largest), Hpl.HplMath.RandRectf(-largest, largest),
                                      Hpl.HplMath.RandRectf(-largest, largest));
                if (player != null) player.SetHeadPosAdd(HeadPosAdd.ScreenShake, add);
            }

            public override void Reset() => _shakes.Clear();
        }

        /// <summary>
        /// cLuxEffect_PlayVoice: voice lines play one after another with their subtitle; when the queue is done the
        /// over callback runs. Without sound a line counts as playing for the duration of its .ogg (lang folder).
        /// </summary>
        public sealed class PlayVoiceEffect : Effect
        {
            struct VoiceData
            {
                public string voiceFile, effectFile, text;
                public bool usePosition;
                public Vector3 position;
                public float minDistance, maxDistance;
            }

            readonly Queue<VoiceData> _voices = new();
            string _overCallback = "";
            // A loaded sound stays "not null" after it ends (the original keeps the entry pointer); valid = still playing.
            bool _voiceLoaded, _effectLoaded;
            float _voiceTimeLeft, _effectTimeLeft;
            public float VolumeMul = 1f;

            /// <summary>Subtitle of the line playing now (kept until the next line, as the original).</summary>
            public string CurrentText { get; private set; } = "";
            public string CurrentVoice { get; private set; } = "";

            bool VoiceValid => _voiceLoaded && _voiceTimeLeft > 0f;
            bool EffectValid => _effectLoaded && _effectTimeLeft > 0f;

            public void AddVoice(string voiceFile, string effectFile, string text, bool usePosition, Vector3 position, float minDistance, float maxDistance)
            {
                _voices.Enqueue(new VoiceData
                {
                    voiceFile = voiceFile, effectFile = effectFile, text = text ?? "",
                    usePosition = usePosition, position = position, minDistance = minDistance, maxDistance = maxDistance,
                });
                Active = true;
            }

            public void StopVoices(float fadeOutSpeed)
            {
                _voiceLoaded = _effectLoaded = false;
                _voices.Clear();
                Active = false;
            }

            public void SetOverCallback(string function) => _overCallback = function ?? "";

            public bool IsActive => Active;

            public override void Update(float timeStep)
            {
                _voiceTimeLeft -= timeStep;
                _effectTimeLeft -= timeStep;

                if (VoiceValid) return;
                if (!_voiceLoaded && EffectValid) return;

                if (_voices.Count == 0)
                {
                    // Reset before calling so the callback can start new voices.
                    string callback = _overCallback;
                    float volumeMul = VolumeMul;
                    Reset();
                    VolumeMul = volumeMul;
                    Active = false;
                    if (callback != "") Handler.Map.RunScript(callback);
                    return;
                }

                var voice = _voices.Dequeue();
                _voiceLoaded = TryDuration(voice.voiceFile, out _voiceTimeLeft);
                _effectLoaded = voice.effectFile != "" && TryDuration(voice.effectFile, out _effectTimeLeft);
                CurrentVoice = voice.voiceFile;
                CurrentText = voice.text;
                // TODO step 6 (sound): stream the voice (GUI or 3D at the position) and the effect sound.

                // Extra check in case the voices do not load.
                if (_voices.Count == 0 && !_voiceLoaded && !_effectLoaded)
                {
                    string callback = _overCallback;
                    Reset();
                    Active = false;
                    if (callback != "") Handler.Map.RunScript(callback);
                }
            }

            bool TryDuration(string file, out float duration)
            {
                duration = 0f;
                var language = Handler.Map != null ? Handler.Map.language : null;
                if (language != null && language.TryGetVoice(file, out var v))
                {
                    duration = v.duration;
                    return true;
                }
                HplLog.Warn($"Voice '{file}' not found in the language folder");
                return false;
            }

            public override void Reset()
            {
                _voiceLoaded = _effectLoaded = false;
                CurrentText = "";
                CurrentVoice = "";
                _voices.Clear();
                _overCallback = "";
                VolumeMul = 1f;
            }
        }

        /// <summary>
        /// cLuxEffect_EmotionFlash: white fades in, a text is shown while the player is paused, then all fades out.
        /// The text is drawn by the HUD; the FOV / radial blur parts are as in HPL2.
        /// </summary>
        public sealed class EmotionFlashEffect : Effect
        {
            public float Alpha, TextAlpha;
            public string Text = "";
            int _step;
            float _textTime;

            public void Start(string text)
            {
                Active = true;
                _step = 0;
                Alpha = 0f;
                Text = text ?? "";
                _textTime = 3f + 0.15f * Text.Length;
                TextAlpha = 0f;
                Handler.SetPlayerIsPaused(true);
                var player = Handler.Map.player;
                if (player != null) player.FadeFOVMulTo(0.5f, 0.5f);
                Handler.RadialBlur.FadeTo(0.15f, 3f);
                Handler.RadialBlur.SetBlurStartDist(0.6f);
                // TODO step 6: sound, enemies reset (BroadcastEnemyMessage).
            }

            /// <summary>Any action key skips the text (DoAction).</summary>
            public void Skip()
            {
                if (_step == 1) _textTime = 0f;
            }

            public override void Update(float timeStep)
            {
                switch (_step)
                {
                    case 0:
                        Alpha += 0.5f * timeStep;
                        if (Alpha >= 1f)
                        {
                            Alpha = 1f;
                            _step = 1;
                        }
                        break;
                    case 1:
                        TextAlpha = Mathf.Min(1f, TextAlpha + timeStep * 3f);
                        _textTime -= timeStep;
                        if (_textTime < 0f)
                        {
                            var player = Handler.Map.player;
                            if (player != null) player.FadeFOVMulTo(1f, 0.33f);
                            Handler.SetPlayerIsPaused(false);
                            Handler.RadialBlur.FadeTo(0f, 1f);
                            _step = 2;
                        }
                        break;
                    case 2:
                        TextAlpha = Mathf.Max(0f, TextAlpha - timeStep);
                        Alpha -= 0.33f * timeStep;
                        if (Alpha <= 0f)
                        {
                            Alpha = 0f;
                            Active = false;
                        }
                        break;
                }
            }
        }
    }
}
