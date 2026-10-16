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
    /// Voices and effect sounds play through the map's sound handler (Sound/HplSoundHandler).
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

        /// <summary>cLuxPostEffect_Insanity (cLuxPostEffectHandler, LuxPostEffects.cpp): set by the player's sanity.</summary>
        public readonly InsanityEffect Insanity = new();
        /// <summary>Insanity textures (Resources/HplPostEffects.asset), null if not imported.</summary>
        public HplPostEffectAssets PostEffectAssets { get; private set; }

        /// <summary>cLuxEffectRenderer: focus outline and item flash (drawn by HplEffectRendererPass).</summary>
        public readonly HplEffectRenderer Renderer = new();

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
                SanityGainFlash.Sound = cfg.GetString("Player_General", "SanityGain_Sound", "");
            }
            PostEffectAssets = Resources.Load<HplPostEffectAssets>(HplPostEffectAssets.ResourceName);
            if (PostEffectAssets == null) Debug.LogWarning("[RTXMnesia] Post effect resources are not imported (Import > Post effect resources): no insanity effect");
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

            var rendererShader = Shader.Find(HplEffectRendererPass.ShaderName);
            if (rendererShader == null)
            {
                Debug.LogError("[RTXMnesia] Shader " + HplEffectRendererPass.ShaderName + " not found: outlines and item flashes are off");
                return;
            }
            var rendererGo = new GameObject("HPL Effect Renderer") { hideFlags = HideFlags.DontSave };
            rendererGo.transform.SetParent(transform, false);
            var rendererVolume = rendererGo.AddComponent<CustomPassVolume>();
            rendererVolume.isGlobal = true;
            rendererVolume.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
            var rendererPass = (HplEffectRendererPass)rendererVolume.AddPassOfType(typeof(HplEffectRendererPass));
            rendererPass.name = "HPL effect renderer";
            rendererPass.renderer = Renderer;
            rendererPass.shader = rendererShader;
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
            // cLuxPostEffectHandler::Update: active post effects.
            if (Insanity.Active) Insanity.Update(timeStep);
            Renderer.Update(timeStep);
        }

        /// <summary>
        /// Map change: HPL2 keeps one effect handler for all maps; here each map scene has its own. The fade
        /// (going black, then FadeIn from CheckMapChange), image trail, flash and screen shakes go on in the next
        /// map; cLuxEffectHandler::OnMapLeave fades sepia and radial blur to 0 in 1 s.
        /// </summary>
        public sealed class Carry
        {
            public readonly FadeEffect Fade = new();
            public readonly FlashEffect Flash = new();
            public readonly AmountEffect ImageTrail = new(), Sepia = new();
            public readonly RadialBlurEffect RadialBlur = new();
            public readonly ShakeScreenEffect ScreenShake = new();
        }

        public Carry CaptureCarry()
        {
            Sepia.FadeTo(0f, 1f);
            RadialBlur.FadeTo(0f, 1f);
            var carry = new Carry();
            carry.Fade.CopyFrom(Fade);
            carry.Flash.CopyFrom(Flash);
            carry.ImageTrail.CopyFrom(ImageTrail);
            carry.Sepia.CopyFrom(Sepia);
            carry.RadialBlur.CopyFrom(RadialBlur);
            carry.ScreenShake.CopyFrom(ScreenShake);
            return carry;
        }

        public void ApplyCarry(Carry carry)
        {
            Fade.CopyFrom(carry.Fade);
            Flash.CopyFrom(carry.Flash);
            ImageTrail.CopyFrom(carry.ImageTrail);
            Sepia.CopyFrom(carry.Sepia);
            RadialBlur.CopyFrom(carry.RadialBlur);
            ScreenShake.CopyFrom(carry.ScreenShake);
        }

        /// <summary>cLuxEffectHandler::Reset (new game, map change).</summary>
        public void ResetAll()
        {
            foreach (var e in _effects)
            {
                e.Reset();
                e.Active = false;
            }
            Renderer.Reset();
            PlayerIsPaused = false;
        }

        // ================================================================== effects

        /// <summary>
        /// cLuxPostEffect_Insanity: the screen is displaced by two of three amplitude maps blended over time
        /// (AnimCount 0.15/s) along sine waves (T advances WaveSpeed/s) and pushed along the zoom map
        /// (posteffect_insanity_frag.glsl). Drawn before the image trail (composite priority 25).
        /// </summary>
        public sealed class InsanityEffect
        {
            public const int AmpMapCount = 3;
            public bool Active;
            public float WaveAlpha, ZoomAlpha, WaveSpeed, T, AnimCount;

            public void Update(float timeStep)
            {
                T += timeStep * WaveSpeed;
                AnimCount += timeStep * 0.15f;
                if (AnimCount >= AmpMapCount) AnimCount -= AmpMapCount;
            }
        }

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

            public void CopyFrom(FadeEffect o)
            {
                Active = o.Active;
                Alpha = o.Alpha;
                _goalAlpha = o._goalAlpha;
                _fadeSpeed = o._fadeSpeed;
            }

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

            public void CopyFrom(FlashEffect o)
            {
                Active = o.Active;
                Alpha = o.Alpha;
                _step = o._step;
                _count = o._count;
                _fadeInSpeed = o._fadeInSpeed;
                _whiteSpeed = o._whiteSpeed;
                _fadeOutSpeed = o._fadeOutSpeed;
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
            public string Sound = "";
            int _step;
            float _count;

            public void Start()
            {
                if (Sound != "") Handler.Map.sound.PlayGuiSoundData(Sound, RTXMnesia.Sound.SoundEntryType.Gui);
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

            public void CopyFrom(AmountEffect o)
            {
                Active = o.Active;
                Amount = o.Amount;
                _goal = o._goal;
                _speed = o._speed;
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

            public void CopyFrom(RadialBlurEffect o)
            {
                Active = o.Active;
                Size = o.Size;
                StartDist = o.StartDist;
                _goal = o._goal;
                _speed = o._speed;
            }

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

            public void CopyFrom(ShakeScreenEffect o)
            {
                Active = o.Active;
                _shakes.Clear();
                _shakes.AddRange(o._shakes);
            }

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
        /// cLuxEffect_PlayVoice: voice lines play one after another with their subtitle (a GUI stream, or a 3D
        /// sound at a position) together with an optional effect sound; when the queue is done the over callback
        /// runs. A line is playing while its sound entry is valid (the handler counts the clip length).
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
            RTXMnesia.Sound.SoundEntry _voiceEntry, _effectEntry;
            int _voiceEntryId = -1, _effectEntryId = -1;
            public float VolumeMul = 1f;

            /// <summary>Subtitle of the line playing now (kept until the next line, as the original).</summary>
            public string CurrentText { get; private set; } = "";
            public string CurrentVoice { get; private set; } = "";

            RTXMnesia.Sound.HplSoundHandler Sound => Handler.Map.sound;
            bool VoiceValid => Sound.IsValid(_voiceEntry, _voiceEntryId);
            bool EffectValid => Sound.IsValid(_effectEntry, _effectEntryId);

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
                if (VoiceValid) _voiceEntry.FadeOut(fadeOutSpeed);
                _voiceEntry = null;
                if (EffectValid) _effectEntry.FadeOut(fadeOutSpeed);
                _effectEntry = null;
                _voices.Clear();
                Active = false;
            }

            public void SetOverCallback(string function) => _overCallback = function ?? "";

            public void SetInventoryPaused(bool paused)
            {
                if (VoiceValid) _voiceEntry.SetInventoryPaused(paused);
                if (EffectValid) _effectEntry.SetInventoryPaused(paused);
            }

            public bool IsActive => Active;

            /// <summary>cLuxEffect_PlayVoice::VoiceDonePlaying: checks the pointer, not validity; ignores effect sounds.</summary>
            public bool VoiceDonePlaying() => _voices.Count == 0 && _voiceEntry == null;

            public override void Update(float timeStep)
            {
                if (VolumeMul < 1f)
                {
                    if (VoiceValid) _voiceEntry.SetVolumeMul(VolumeMul);
                    if (EffectValid) _effectEntry.SetVolumeMul(VolumeMul);
                }

                if (VoiceValid) return;
                if (_voiceEntry == null && EffectValid) return;

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
                if (!voice.usePosition)
                {
                    _voiceEntry = Sound.PlayGuiStream(voice.voiceFile, false, 1f);
                    _effectEntry = voice.effectFile != "" ? Sound.PlayGuiStream(voice.effectFile, false, 1f) : null;
                }
                else
                {
                    _voiceEntry = Sound.Play(voice.voiceFile, false, 1f, voice.position, voice.minDistance, voice.maxDistance,
                                             RTXMnesia.Sound.SoundEntryType.Gui, false, true, 0, true);
                    _effectEntry = voice.effectFile != ""
                        ? Sound.Play(voice.effectFile, false, 1f, voice.position, voice.minDistance, voice.maxDistance,
                                     RTXMnesia.Sound.SoundEntryType.Gui, false, true, 0, true)
                        : null;
                }
                if (_voiceEntry != null) _voiceEntryId = _voiceEntry.Id;
                if (_effectEntry != null) _effectEntryId = _effectEntry.Id;
                CurrentVoice = voice.voiceFile;
                CurrentText = voice.text;

                // Extra check in case the voices do not load.
                if (_voices.Count == 0 && _voiceEntry == null && _effectEntry == null)
                {
                    string callback = _overCallback;
                    Reset();
                    Active = false;
                    if (callback != "") Handler.Map.RunScript(callback);
                }
            }

            public override void Reset()
            {
                if (VoiceValid) _voiceEntry.Stop();
                _voiceEntry = null;
                if (EffectValid) _effectEntry.Stop();
                _effectEntry = null;
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

            public void Start(string text, string sound = "")
            {
                Active = true;
                _step = 0;
                Alpha = 0f;
                Handler.Map.sound.PlayGuiSoundData(sound, RTXMnesia.Sound.SoundEntryType.Gui);
                Text = text ?? "";
                _textTime = 3f + 0.15f * Text.Length;
                TextAlpha = 0f;
                Handler.SetPlayerIsPaused(true);
                var player = Handler.Map.player;
                if (player != null) player.FadeFOVMulTo(0.5f, 0.5f);
                Handler.RadialBlur.FadeTo(0.15f, 3f);
                Handler.RadialBlur.SetBlurStartDist(0.6f);
                // TODO step 6: enemies reset (BroadcastEnemyMessage).
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
