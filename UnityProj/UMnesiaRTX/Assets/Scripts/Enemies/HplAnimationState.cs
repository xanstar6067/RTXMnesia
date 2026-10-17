using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>
    /// cAnimationState (AnimationState.cpp): time position, speed × base speed, loop, weight with fades, special event
    /// time and events. The pose is a Unity legacy <see cref="UnityEngine.AnimationState"/> driven with this time and
    /// weight (its own speed is 0), so the game logic does not depend on Unity's playback.
    /// </summary>
    public sealed class HplAnimationState
    {
        public string Name { get; }
        public float Length { get; }
        public float BaseSpeed { get; }
        public float SpecialEventTime { get; }
        public readonly List<HplEnemyAssets.AnimationEvent> Events;
        internal readonly UnityEngine.AnimationState UnityState;

        public bool IsActive { get; private set; }
        public float Weight { get; set; } = 1f;
        public float Speed { get; set; } = 1f;
        public bool Loop { get; set; }
        public bool Paused { get; set; }
        public float TimePosition { get; private set; }
        public float PreviousTimePosition { get; private set; }
        float _fadeStep;

        public HplAnimationState(HplEnemyAssets.Animation data, UnityEngine.AnimationState unityState)
        {
            Name = data.name;
            Length = data.clip != null ? data.clip.length : 0f;
            BaseSpeed = data.speed;
            SpecialEventTime = data.specialEventTime;
            Events = data.events;
            UnityState = unityState;
        }

        public bool IsFading => _fadeStep != 0f;
        public float FadeStep => _fadeStep;

        /// <summary>cEngineAnimationState_SaveData::ToAnim.</summary>
        public void Restore(bool active, bool loop, float weight, float fadeStep, float timePosition, float speed)
        {
            SetActive(active);
            Loop = loop;
            Weight = weight;
            _fadeStep = fadeStep;
            SetTimePosition(timePosition);
            Speed = speed;
        }
        public bool IsOver => !Loop && TimePosition >= Length;

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            Paused = false;
            _fadeStep = 0f;
        }

        public void FadeIn(float time) => _fadeStep = 1f / Mathf.Abs(time);
        public void FadeOut(float time) => _fadeStep = -1f / Mathf.Abs(time);

        public void SetTimePosition(float position)
        {
            if (Loop) TimePosition = Hpl.HplMatrix.Wrap(position, 0f, Length);
            else TimePosition = Mathf.Clamp(position, 0f, Length);
        }

        public float RelativeTimePosition
        {
            get => Length > 0f ? TimePosition / Length : 0f;
            set => SetTimePosition(value * Length);
        }

        public void AddTimePosition(float add)
        {
            if (Paused) return;
            PreviousTimePosition = TimePosition;
            SetTimePosition(TimePosition + add * Speed * BaseSpeed);
        }

        /// <summary>cAnimationState::Update: time, then the fade (a fade out to 0 deactivates).</summary>
        public void Update(float timeStep)
        {
            AddTimePosition(timeStep);
            if (_fadeStep == 0f) return;
            Weight += _fadeStep * timeStep;
            if (_fadeStep < 0f && Weight <= 0f)
            {
                Weight = 0f;
                IsActive = false;
                _fadeStep = 0f;
            }
            else if (_fadeStep > 0f && Weight >= 1f)
            {
                Weight = 1f;
                _fadeStep = 0f;
            }
        }
    }

    /// <summary>
    /// The animation part of cMeshEntity (MeshEntity.cpp UpdateLogic): the active states pose the skeleton with weights
    /// normalized to their sum (mbNormalizeAnimationWeights), then advance; events between the previous and the new
    /// time run (PlaySound: a sound entity at the first bone).
    /// </summary>
    public sealed class HplMeshAnimation
    {
        readonly Animation _animation;
        readonly List<HplAnimationState> _states = new();
        readonly Dictionary<string, HplAnimationState> _byName = new();
        public Transform SoundNode { get; }

        public IReadOnlyList<HplAnimationState> States => _states;

        public HplMeshAnimation(GameObject model, HplEnemyAssets.Enemy data)
        {
            _animation = model.GetComponent<Animation>();
            if (_animation == null) _animation = model.AddComponent<Animation>();
            _animation.playAutomatically = false;
            _animation.cullingType = AnimationCullingType.AlwaysAnimate;
            _animation.clip = null;
            if (data != null)
            {
                foreach (var a in data.animations)
                {
                    if (a.clip == null || _byName.ContainsKey(a.name)) continue;
                    if (_animation.GetClip(a.name) == null) _animation.AddClip(a.clip, a.name);
                    var unityState = _animation[a.name];
                    unityState.speed = 0f;
                    unityState.wrapMode = WrapMode.ClampForever;
                    unityState.enabled = false;
                    var state = new HplAnimationState(a, unityState);
                    _states.Add(state);
                    _byName[a.name] = state;
                }
            }
            // The bone state root's first child: the skeleton root under the model.
            var skinned = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            SoundNode = skinned != null && skinned.rootBone != null ? skinned.rootBone : model.transform;
        }

        public HplAnimationState Get(string name) => name != null && _byName.TryGetValue(name, out var s) ? s : null;

        /// <summary>cMeshEntity::GetAnimationStateIndex (−1 when missing).</summary>
        public int IndexOf(string name)
        {
            var state = Get(name);
            return state != null ? _states.IndexOf(state) : -1;
        }

        /// <summary>cMeshEntity::Play: from the start at full weight, the others stopped first if asked.</summary>
        public void Play(int index, bool loop, bool stopPrev)
        {
            if (stopPrev) Stop();
            if (index < 0 || index >= _states.Count) return;
            var state = _states[index];
            state.SetActive(true);
            state.SetTimePosition(0f);
            state.Loop = loop;
            state.Weight = 1f;
        }

        /// <summary>cMeshEntity::PlayFadeTo: the active states fade out, this one fades in from the start.</summary>
        public void PlayFadeTo(int index, bool loop, float time)
        {
            foreach (var s in _states) if (s.IsActive) s.FadeOut(time);
            var state = _states[index];
            if (!state.IsActive) state.Weight = 0f;
            state.SetActive(true);
            state.SetTimePosition(0f);
            state.Loop = loop;
            state.FadeIn(time);
        }

        /// <summary>cMeshEntity::Stop.</summary>
        public void Stop()
        {
            foreach (var s in _states) s.SetActive(false);
        }

        /// <summary>One logic step: pose, advance, events. <paramref name="onEvent"/> gets the events that passed.</summary>
        public void Update(float timeStep, System.Action<HplAnimationState, HplEnemyAssets.AnimationEvent> onEvent)
        {
            float total = 0f;
            bool any = false;
            foreach (var s in _states)
            {
                if (!s.IsActive) continue;
                any = true;
                total += s.Weight;
            }
            float mul = any && total > 0f ? 1f / total : 1f;
            foreach (var s in _states)
            {
                var u = s.UnityState;
                if (u == null) continue;
                u.enabled = s.IsActive;
                if (!s.IsActive) continue;
                u.weight = s.Weight * mul;
                u.time = s.TimePosition;
            }
            if (any) _animation.Sample();

            foreach (var s in _states) if (s.IsActive) s.Update(timeStep);

            foreach (var s in _states)
            {
                if (!s.IsActive || s.Paused) continue;
                foreach (var e in s.Events)
                    if (e.time >= s.PreviousTimePosition && e.time < s.TimePosition) onEvent?.Invoke(s, e);
            }
        }
    }
}
