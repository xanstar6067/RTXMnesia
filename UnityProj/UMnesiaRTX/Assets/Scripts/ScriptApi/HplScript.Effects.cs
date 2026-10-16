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
            Effects.EmotionFlash.Start(Translate(asTextCat, asTextEntry), asSound);
        }

        protected void SetInDarknessEffectsActive(bool abX) => Player.InDarkness.SetActive(abX);

        /// <summary>The position is the first body of the entity.</summary>
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

        protected bool GetFlashbackIsActive() => Player.Flashback.Active;

        // ------------------------------------------------------------------ insanity (step 6)

        // cLuxInsanityHandler (Effects/HplInsanity): one handler for the whole game.
        protected void SetInsanitySetEnabled(string asSet, bool abX)
        {
            if (abX) RTXMnesia.Effects.HplInsanity.EnableSet(asSet);
            else RTXMnesia.Effects.HplInsanity.DisableSet(asSet);
        }
        protected void StartRandomInsanityEvent() => RTXMnesia.Effects.HplInsanity.StartEvent(Map);
        protected void StartInsanityEvent(string asEventName) => RTXMnesia.Effects.HplInsanity.StartEvent(Map, asEventName);
        protected void StopCurrentInsanityEvent() => RTXMnesia.Effects.HplInsanity.StopCurrentEvent();
        protected bool InsanityEventIsActive() => RTXMnesia.Effects.HplInsanity.CurrentEventIndex >= 0;

        // ------------------------------------------------------------------ sounds and music

        protected Sound.HplSoundHandler SoundHandler => Map.sound;

        /// <summary>A .snt (no or "snt" extension) as a GUI sound, otherwise a sound file.</summary>
        protected void PlayGuiSound(string asSoundFile, float afVolume)
        {
            string ext = System.IO.Path.GetExtension(asSoundFile ?? "").TrimStart('.').ToLowerInvariant();
            if (ext == "" || ext == "snt") SoundHandler.PlayGuiSoundData(asSoundFile, Sound.SoundEntryType.Gui, afVolume);
            else SoundHandler.PlayGui(asSoundFile, false, afVolume);
        }

        protected void PreloadSound(string asSoundFile) { }

        /// <summary>
        /// The sound is removed when over (the original ignores abSaveSound for that). On "Player" it plays
        /// without position; otherwise it follows the entity, or with abSaveSound stays where the entity is now.
        /// The fade argument is a time (fade speed 1 / time).
        /// </summary>
        protected void PlaySoundAtEntity(string asSoundName, string asSoundFile, string asEntity, float afFadeSpeed, bool abSaveSound)
        {
            float fadeSpeed = afFadeSpeed == 0f ? 0f : 1f / afFadeSpeed;
            if (asEntity == "Player")
            {
                var s = SoundHandler.CreateSoundEntity(asSoundName, asSoundFile, true);
                if (s == null) return;
                s.ForcePlayAsGuiSound = true;
                s.IsSaved = abSaveSound;
                if (afFadeSpeed > 0f) s.FadeIn(fadeSpeed);
                return;
            }
            var entity = GetEntity(asEntity);
            if (entity == null) return;
            var sound = SoundHandler.CreateSoundEntity(asSoundName, asSoundFile, true);
            if (sound == null) return;
            if (!abSaveSound) sound.Parent = entity.AttachTransform;
            else sound.Position = entity.AttachTransform.position;
            sound.IsSaved = abSaveSound;
            if (afFadeSpeed > 0f) sound.FadeIn(fadeSpeed);
        }

        protected void FadeInSound(string asSoundName, float afFadeTime, bool abPlayStart)
        {
            float fadeSpeed = afFadeTime == 0f ? 0f : 1f / afFadeTime;
            var sound = SoundHandler.GetSoundEntity(asSoundName);
            if (sound == null)
            {
                HplLog.Error($"Could not find sound entity '{asSoundName}'");
                return;
            }
            if (fadeSpeed <= 0f) sound.Play(abPlayStart);
            else sound.FadeIn(fadeSpeed);
        }

        /// <summary>Every sound entity with the name.</summary>
        protected void StopSound(string asSoundName, float afFadeTime)
        {
            float fadeSpeed = afFadeTime == 0f ? 0f : 1f / afFadeTime;
            bool found = false;
            foreach (var sound in SoundHandler.SoundEntities)
            {
                if (sound.Name != asSoundName) continue;
                if (fadeSpeed <= 0f) sound.Stop(true);
                else sound.FadeOut(fadeSpeed);
                found = true;
            }
            if (!found) HplLog.Error($"Could not find sound entity '{asSoundName}'");
        }

        protected void PlayMusic(string asMusicFile, bool abLoop, float afVolume, float afFadeTime, int alPrio, bool abResume) =>
            SoundHandler.Music.Play(asMusicFile, abLoop, afVolume, afFadeTime, alPrio, abResume);

        protected void StopMusic(float afFadeTime, int alPrio) => SoundHandler.Music.Stop(afFadeTime, alPrio);

        /// <summary>The script's own global volume request (world sounds); back at 1 it is removed.</summary>
        protected void FadeGlobalSoundVolume(float afDestVolume, float afTime)
        {
            var entry = SoundHandler.GlobalVolume.GetEntry(GlobalVolumeTypeScript, true);
            float speed = afTime > 0f ? Mathf.Abs(afDestVolume - entry.Val) / afTime : 1000f;
            SoundHandler.FadeGlobalVolume(afDestVolume, speed, Sound.SoundEntryType.World, GlobalVolumeTypeScript, afDestVolume == 1f);
        }

        protected void FadeGlobalSoundSpeed(float afDestSpeed, float afTime)
        {
            var entry = SoundHandler.GlobalSpeed.GetEntry(GlobalVolumeTypeScript, true);
            float speed = afTime > 0f ? Mathf.Abs(afDestSpeed - entry.Val) / afTime : 1000f;
            SoundHandler.FadeGlobalSpeed(afDestSpeed, speed, Sound.SoundEntryType.World, GlobalVolumeTypeScript, afDestSpeed == 1f);
        }

        /// <summary>eLuxGlobalVolumeType_Script.</summary>
        const int GlobalVolumeTypeScript = 0;

        // ------------------------------------------------------------------ particle systems

        /// <summary>The bank has every system already.</summary>
        protected void PreloadParticleSystem(string asPSFile) { }

        protected void CreateParticleSystemAtEntity(string asPSName, string asPSFile, string asEntity, bool abSavePS) =>
            CreateParticleSystemAtEntityExt(asPSName, asPSFile, asEntity, abSavePS, 1, 1, 1, 1, false, 1, 2, 100, 110);

        /// <summary>
        /// At the player's body position, or on the entity's attach body: following it unless saved (then at its
        /// matrix of now).
        /// </summary>
        protected void CreateParticleSystemAtEntityExt(string asPSName, string asPSFile, string asEntity, bool abSavePS, float afR, float afG,
                                                       float afB, float afA, bool abFadeAtDistance, float afFadeMinEnd, float afFadeMinStart,
                                                       float afFadeMaxStart, float afFadeMaxEnd)
        {
            Particles.HplParticleSystem ps;
            if (asEntity == "Player")
            {
                ps = Map.ParticleWorld.Create(asPSName, asPSFile);
                if (ps == null) return;
                var body = Map.player != null ? Map.player.Body : null;
                ps.LocalMatrix = Matrix4x4.Translate(body != null ? body.Position : Vector3.zero);
                ps.IsSaved = abSavePS;
            }
            else
            {
                var entity = GetEntity(asEntity);
                if (entity == null) return;
                ps = Map.ParticleWorld.Create(asPSName, asPSFile);
                if (ps == null) return;
                var attach = entity.AttachTransform;
                if (!abSavePS) ps.Parent = attach;
                else ps.LocalMatrix = attach.localToWorldMatrix;
                ps.IsSaved = abSavePS;
            }
            ps.Color = new Color(afR, afG, afB, afA);
            ps.FadeAtDistance = abFadeAtDistance;
            ps.MinFadeDistanceEnd = afFadeMinEnd;
            ps.MinFadeDistanceStart = afFadeMinStart;
            ps.MaxFadeDistanceStart = afFadeMaxStart;
            ps.MaxFadeDistanceEnd = afFadeMaxEnd;
        }

        /// <summary>Every system with the name stops respawning and goes when its particles are dead.</summary>
        protected void DestroyParticleSystem(string asName)
        {
            bool found = false;
            foreach (var ps in Map.ParticleWorld.Systems)
            {
                if (ps.Name != asName) continue;
                ps.Kill();
                found = true;
            }
            if (!found) HplLog.Error($"Could not find particle system '{asName}'");
        }

        protected void StartPlayerSpawnPS(string asSPSFile) => NotPorted(nameof(StartPlayerSpawnPS), "player spawn particles (.sps)");

        protected void StopPlayerSpawnPS() { }
    }
}
