using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    // Screen effects, voices, insanity, sounds and particle systems (LuxScriptHandler.cpp → cLuxEffectHandler, ...).
    public abstract partial class HplScript
    {
        protected void FadeIn(float afTime) => Effects.Fade.FadeIn(afTime);

        protected void FadeOut(float afTime) => Effects.Fade.FadeOut(afTime);

        protected void FadeImageTrailTo(float afAmount, float afSpeed) => Effects.ImageTrail.FadeTo(afAmount, afSpeed);

        protected void FadeSepiaColorTo(float afAmount, float afSpeed) => Effects.Sepia.FadeTo(afAmount, afSpeed);

        protected void FadeRadialBlurTo(float afSize, float afSpeed) => Effects.RadialBlur.FadeTo(afSize, afSpeed);

        protected void SetRadialBlurStartDist(float afStartDist) => Effects.RadialBlur.SetBlurStartDist(afStartDist);

        /// <summary>As the original: the fade-out time is also used for fading in.</summary>
        protected void StartEffectFlash(float afFadeIn, float afWhite, float afFadeOut) => Effects.Flash.Start(afFadeOut, afWhite, afFadeOut);

        protected void StartEffectEmotionFlash(string asTextCat, string asTextEntry, string asSound)
        {
            Effects.EmotionFlash.Start(Translate(asTextCat, asTextEntry));
            if (!string.IsNullOrEmpty(asSound)) NotPorted(nameof(StartEffectEmotionFlash) + " sound", "the sound system (step 6)");
        }

        protected void SetInDarknessEffectsActive(bool abX) => NotPorted(nameof(SetInDarknessEffectsActive), "darkness effects (step 6, insanity)");

        /// <summary>The position is the first body of the entity (3D voices are heard with step 6).</summary>
        protected void AddEffectVoice(string asVoiceFile, string asEffectFile, string asTextCat, string asTextEntry, bool abUsePostion,
                                      string asPosEnitity, float afMinDistance, float afMaxDistance)
        {
            Vector3 position = Vector3.zero;
            if (abUsePostion)
            {
                var entity = GetEntity(asPosEnitity);
                if (entity != null && entity.Bodies.Count > 0) position = entity.Bodies[0].Transform.position;
            }
            string text = !string.IsNullOrEmpty(asTextCat) && !string.IsNullOrEmpty(asTextEntry) ? Translate(asTextCat, asTextEntry) : "";
            Effects.PlayVoice.AddVoice(asVoiceFile ?? "", asEffectFile ?? "", text, abUsePostion, position, afMinDistance, afMaxDistance);
        }

        protected void StopAllEffectVoices(float afFadeOutTime) => Effects.PlayVoice.StopVoices(afFadeOutTime > 0f ? 1f / afFadeOutTime : 0f);

        protected bool GetEffectVoiceActive() => Effects.PlayVoice.IsActive;

        protected void SetEffectVoiceOverCallback(string asFunc) => Effects.PlayVoice.SetOverCallback(asFunc);

        protected void StartScreenShake(float afAmount, float afTime, float afFadeInTime, float afFadeOutTime) =>
            Effects.ScreenShake.Start(afAmount, afTime, afFadeInTime, afFadeOutTime);

        protected bool GetFlashbackIsActive() => false;

        // ------------------------------------------------------------------ insanity (step 6)

        protected void SetInsanitySetEnabled(string asSet, bool abX) => NotPorted(nameof(SetInsanitySetEnabled), "insanity events (step 6)");
        protected void StartRandomInsanityEvent() => NotPorted(nameof(StartRandomInsanityEvent), "insanity events (step 6)");
        protected void StartInsanityEvent(string asEventName) => NotPorted(nameof(StartInsanityEvent), "insanity events (step 6)");
        protected void StopCurrentInsanityEvent() { }
        protected bool InsanityEventIsActive() => false;

        // ------------------------------------------------------------------ sounds and music (step 6)

        protected void PlayGuiSound(string asSoundFile, float afVolume) => NotPorted(nameof(PlayGuiSound), "the sound system (step 6)");

        protected void PreloadSound(string asSoundFile) { }

        protected void PlaySoundAtEntity(string asSoundName, string asSoundFile, string asEntity, float afFadeSpeed, bool abSaveSound) =>
            NotPorted(nameof(PlaySoundAtEntity), "the sound system (step 6)");

        protected void FadeInSound(string asSoundName, float afFadeTime, bool abPlayStart) => NotPorted(nameof(FadeInSound), "the sound system (step 6)");

        protected void StopSound(string asSoundName, float afFadeTime) => NotPorted(nameof(StopSound), "the sound system (step 6)");

        protected void PlayMusic(string asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume) =>
            NotPorted(nameof(PlayMusic), "the music handler (step 6)");

        protected void StopMusic(float afFadeTime, int alPrio) => NotPorted(nameof(StopMusic), "the music handler (step 6)");

        protected void FadeGlobalSoundVolume(float afDestVolume, float afTime) => NotPorted(nameof(FadeGlobalSoundVolume), "the sound system (step 6)");

        protected void FadeGlobalSoundSpeed(float afDestSpeed, float afTime) => NotPorted(nameof(FadeGlobalSoundSpeed), "the sound system (step 6)");

        // ------------------------------------------------------------------ particle systems (.ps converter)

        protected void PreloadParticleSystem(string asPSFile) { }

        protected void CreateParticleSystemAtEntity(string asPSName, string asPSFile, string asEntity, bool abSavePS) =>
            NotPorted(nameof(CreateParticleSystemAtEntity), "particle systems (.ps converter)");

        protected void CreateParticleSystemAtEntityExt(string asPSName, string asPSFile, string asEntity, bool abSavePS, float afR, float afG,
                                                       float afB, float afA, bool abFadeAtDistance, float afFadeMinEnd, float afFadeMinStart,
                                                       float afFadeMaxStart, float afFadeMaxEnd) =>
            NotPorted(nameof(CreateParticleSystemAtEntityExt), "particle systems (.ps converter)");

        protected void DestroyParticleSystem(string asName) => NotPorted(nameof(DestroyParticleSystem), "particle systems (.ps converter)");

        protected void StartPlayerSpawnPS(string asSPSFile) => NotPorted(nameof(StartPlayerSpawnPS), "particle systems (.ps converter)");

        protected void StopPlayerSpawnPS() { }
    }
}
