using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.Sound;
using UnityEngine;

namespace RTXMnesia.Dynamics
{
    /// <summary>
    /// Impact, scrape and roll sounds of physics materials: cPhysicsMaterialNewton::ContactsProcessCallback's
    /// surface part, cSurfaceData::OnImpact / OnSlide / UpdateRollEffect, cPhysics impact limit (6 per 0.4 s) and
    /// the scrape bookkeeping of iPhysicsBody::UpdateAfterSimulate. Contacts come from <see cref="HplBodyContacts"/>
    /// (OnCollisionEnter / Stay) of every body; the speeds are taken from the velocities before the step, as
    /// Newton's contact callback saw them. Static geometry has its own scrape state, like its Newton bodies.
    /// The impact particle systems (CreateImpactEffect) come from the same contacts.
    /// </summary>
    public sealed class HplSurfaceSounds
    {
        /// <summary>The scrape state of iPhysicsBody (a dynamic body or a static collider).</summary>
        public sealed class State
        {
            public HplSoundEntity ScrapeSound, RollSound;
            public object ScrapeAgainst;
            public Matrix4x4 PrevScrapeMatrix = Matrix4x4.identity;
            public bool HasSlide, HasCollision;
            public int SlideCount;
        }

        const float MaxSeparation = 0.002f;
        const int MaxImpacts = 6;
        const float ImpactDuration = 0.4f;

        readonly HplPhysicsWorld _world;
        readonly List<float> _impactCounts = new();
        readonly Dictionary<Collider, State> _staticStates = new();
        readonly List<State> _activeStatic = new();

        public HplSoundHandler Sound;
        public HplSurfaces Surfaces;
        public Particles.HplParticleWorld Particles;

        public HplSurfaceSounds(HplPhysicsWorld world) => _world = world;

        public void FadeOutLoopSounds(float speed)
        {
            void Fade(State state)
            {
                if (Exists(state.ScrapeSound)) state.ScrapeSound.FadeOut(speed);
                if (Exists(state.RollSound)) state.RollSound.FadeOut(speed);
                state.ScrapeSound = state.RollSound = null;
                state.ScrapeAgainst = null;
            }
            foreach (var body in _world.Bodies) Fade(body.SurfaceState);
            foreach (var state in _activeStatic) Fade(state);
            _activeStatic.Clear();
        }

        // ------------------------------------------------------------------ per step

        /// <summary>iPhysicsBody::UpdateBeforeSimulate: the slide and collision flags of this step.</summary>
        public void BeforeSimulate()
        {
            foreach (var body in _world.Bodies)
            {
                body.SurfaceState.HasSlide = false;
                body.SurfaceState.HasCollision = false;
            }
            foreach (var s in _activeStatic) s.HasSlide = s.HasCollision = false;
        }

        /// <summary>iPhysicsBody::UpdateAfterSimulate (scrape stop, roll) and cPhysics::UpdateImpactCounts.</summary>
        public void AfterSimulate(float timeStep)
        {
            for (int i = 0; i < _impactCounts.Count;)
            {
                _impactCounts[i] += timeStep;
                if (_impactCounts[i] > ImpactDuration) _impactCounts.RemoveAt(i);
                else i++;
            }

            foreach (var body in _world.Bodies)
            {
                UpdateScrapeStop(body.SurfaceState);
                if (body.Enabled && body.Surface != null) UpdateRollEffect(body);
            }
            for (int i = 0; i < _activeStatic.Count;)
            {
                var s = _activeStatic[i];
                UpdateScrapeStop(s);
                if (s.ScrapeSound == null) _activeStatic.RemoveAt(i);
                else i++;
            }
        }

        void UpdateScrapeStop(State s)
        {
            if (s.HasSlide)
            {
                s.SlideCount = 8;
                return;
            }
            if (s.ScrapeSound == null) return;
            if (s.SlideCount <= 0)
            {
                if (Exists(s.ScrapeSound)) s.ScrapeSound.FadeOut(5.2f);
                s.ScrapeSound = null;
                s.ScrapeAgainst = null;
            }
            else s.SlideCount--;
        }

        bool Exists(HplSoundEntity sound)
        {
            if (sound == null || Sound == null) return false;
            foreach (var s in Sound.SoundEntities) if (s == sound) return true;
            return false;
        }

        // ------------------------------------------------------------------ contacts

        /// <summary>ContactsProcessCallback for a body and what it touches (each pair once).</summary>
        public void OnContact(HplPhysicsBody body, Collision collision)
        {
            if (Sound == null || Sound.Silent || collision.contactCount == 0) return;
            var other = _world.BodyOf(collision.collider);
            if (other != null && other.Order < body.Order) return;
            bool touching = false;
            for (int i = 0; i < collision.contactCount && !touching; i++)
                touching = collision.GetContact(i).separation <= MaxSeparation;
            if (!touching) return;

            body.SurfaceState.HasCollision = true;
            State otherState = other != null ? other.SurfaceState : StaticState(collision.collider);
            otherState.HasCollision = true;

            var surfaceA = body.Surface;
            var surfaceB = other != null ? other.Surface : StaticSurface(collision.collider);
            if (surfaceA == null || surfaceB == null) return;
            if (!body.Data.useSurfaceEffects || (other != null && !other.Data.useSurfaceEffects)) return;

            int count = 0;
            float normalSpeed = 0f, tangentSpeed = 0f;
            Vector3 position = Vector3.zero;
            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                // PhysX reports contacts within the contact offset (about 1 cm) before touching; Newton only real ones.
                if (contact.separation > MaxSeparation) continue;
                count++;
                Vector3 p = contact.point;
                Vector3 rel = body.PreviousVelocityAt(p) - (other != null ? other.PreviousVelocityAt(p) : Vector3.zero);
                float n = Mathf.Abs(Vector3.Dot(rel, contact.normal));
                float t = (rel - contact.normal * Vector3.Dot(rel, contact.normal)).magnitude;
                if (normalSpeed < n) normalSpeed = n;
                if (tangentSpeed < t) tangentSpeed = t;
                position += p;
            }
            if (count == 0) return;
            position /= count;

            CreateImpactEffect(surfaceA, normalSpeed, position, surfaceB);
            int prioA = surfaceA.priority, prioB = surfaceB.priority;
            if (prioA >= prioB)
            {
                if (normalSpeed > 0f) OnImpact(surfaceA, normalSpeed, position);
                if (tangentSpeed > 0f) OnSlide(surfaceA, tangentSpeed, position, count, body.SurfaceState, body, (object)other ?? collision.collider);
            }
            if (prioB >= prioA && surfaceA != surfaceB)
            {
                if (normalSpeed > 0f) OnImpact(surfaceB, normalSpeed, position);
                if (tangentSpeed > 0f) OnSlide(surfaceB, tangentSpeed, position, count, otherState, other, body);
            }
        }

        State StaticState(Collider c)
        {
            if (!_staticStates.TryGetValue(c, out var s)) _staticStates[c] = s = new State();
            return s;
        }

        /// <summary>Static geometry: the surface of its PhysicsMaterial (the .mat PhysicsMaterial), else "Default".</summary>
        HplSurfaces.Surface StaticSurface(Collider c)
        {
            if (Surfaces == null) return null;
            var pm = c.sharedMaterial;
            return Surfaces.GetOrDefault(pm != null ? pm.name : null);
        }

        static HplSurfaces.Effect FirstImpact(HplSurfaces.Surface surface, float speed)
        {
            foreach (var impact in surface.impacts)
                if (impact.minSpeed <= speed) return impact;
            return null;
        }

        /// <summary>
        /// cSurfaceData::CreateImpactEffect: the particle system of the impact the speed reaches on either surface
        /// (higher PSPrio wins, the first surface on a tie), at the contact. Not limited like the impact sounds.
        /// </summary>
        void CreateImpactEffect(HplSurfaces.Surface surface, float speed, Vector3 position, HplSurfaces.Surface second)
        {
            if (speed == 0f || Particles == null) return;
            var a = FirstImpact(surface, speed);
            var b = second != surface && second != null ? FirstImpact(second, speed) : null;
            string ps = "";
            if (a != null && b == null) ps = a.psName;
            else if (a == null && b != null) ps = b.psName;
            else if (a != null) ps = a.psPrio >= b.psPrio ? a.psName : b.psName;
            if (string.IsNullOrEmpty(ps)) return;
            var system = Particles.Create("ImpactPS", ps);
            if (system != null) system.LocalMatrix = Matrix4x4.Translate(position);
        }

        /// <summary>cSurfaceData::OnImpact: the first impact (fastest first) the speed reaches, within the limit.</summary>
        void OnImpact(HplSurfaces.Surface surface, float speed, Vector3 position)
        {
            if (_impactCounts.Count >= MaxImpacts) return;
            HplSurfaces.Effect data = null;
            foreach (var impact in surface.impacts)
                if (impact.minSpeed <= speed)
                {
                    data = impact;
                    break;
                }
            if (data == null || data.soundName == "") return;
            _impactCounts.Add(0f);
            var sound = Sound.CreateSoundEntity("Impact", data.soundName, true);
            if (sound == null) return;
            sound.IsSaved = false;
            sound.Position = position;
        }

        /// <summary>cSurfaceData::OnSlide: a scrape sound while sliding fast enough, its speed (pitch) by the slide speed.</summary>
        void OnSlide(HplSurfaces.Surface surface, float speed, Vector3 position, int contacts, State state, HplPhysicsBody owner, object against)
        {
            if (contacts < surface.minScrapeContacts) return;
            if (state.ScrapeAgainst != null && !Equals(state.ScrapeAgainst, against)) return;
            if (state.ScrapeSound != null && !Exists(state.ScrapeSound)) state.ScrapeSound = null;

            if (owner != null && !owner.IsStatic)
            {
                Matrix4x4 m = owner.Rigidbody.transform.localToWorldMatrix;
                if (state.PrevScrapeMatrix == m) speed = 0f;
                state.PrevScrapeMatrix = m;
            }

            if (state.ScrapeSound != null)
            {
                float min = Mathf.Max(surface.minScrapeSpeed - 0.7f, 0.02f);
                if (Mathf.Abs(speed) < min)
                {
                    state.ScrapeSound.FadeOut(4.3f);
                    state.ScrapeSound = null;
                    state.ScrapeAgainst = null;
                    return;
                }
                state.HasSlide = true;
                float freq = Frequency(Mathf.Abs(speed), surface.minScrapeFreqSpeed, surface.middleScrapeSpeed,
                                       surface.maxScrapeFreqSpeed, surface.minScrapeFreq, surface.maxScrapeFreq);
                var entry = state.ScrapeSound.GetEntry(SoundEntityType.Main);
                if (entry != null)
                {
                    entry.SetSpeedMul(freq);
                    state.ScrapeSound.Position = position;
                }
            }
            else if (surface.minScrapeSpeed <= Mathf.Abs(speed) && surface.scrapeSoundName != "")
            {
                state.HasSlide = true;
                var sound = Sound.CreateSoundEntity("Scrape", surface.scrapeSoundName, true);
                if (sound != null)
                {
                    sound.FadeIn(3.3f);
                    sound.Position = position;
                    sound.IsSaved = false;
                    state.ScrapeSound = sound;
                    state.ScrapeAgainst = against;
                }
            }

            if (owner == null && state.HasSlide && !_activeStatic.Contains(state)) _activeStatic.Add(state);
        }

        /// <summary>The scrape / roll frequency: 1 at the middle speed, towards min / max frequency below / above it.</summary>
        static float Frequency(float speed, float minFreqSpeed, float middleSpeed, float maxFreqSpeed, float minFreq, float maxFreq)
        {
            if (speed >= middleSpeed)
            {
                if (speed >= maxFreqSpeed) return maxFreq;
                float t = (speed - middleSpeed) / (maxFreqSpeed - middleSpeed);
                return (1f - t) + t * maxFreq;
            }
            if (speed <= minFreqSpeed) return minFreq;
            float u = (middleSpeed - speed) / (middleSpeed - minFreqSpeed);
            return (1f - u) + u * minFreq;
        }

        /// <summary>cSurfaceData::UpdateRollEffect: a roll sound while the body rolls on something about its roll axes.</summary>
        void UpdateRollEffect(HplPhysicsBody body)
        {
            var surface = body.Surface;
            var state = body.SurfaceState;
            if (!body.Data.useSurfaceEffects || surface.rollSoundName == "" || surface.rollAxisFlags == 0) return;

            // In HPL2 space (the axes are the body's own).
            Vector3 angular = body.HplMatrix.rotation * body.HplAngularVelocity;
            float rolling = 0f;
            if ((surface.rollAxisFlags & 1) != 0) rolling = Mathf.Abs(angular.x);
            if ((surface.rollAxisFlags & 2) != 0) rolling = Mathf.Max(rolling, Mathf.Abs(angular.y));
            if ((surface.rollAxisFlags & 4) != 0) rolling = Mathf.Max(rolling, Mathf.Abs(angular.z));
            if (rolling == 0f && state.RollSound == null) return;
            if (Sound == null || Sound.Silent) return;
            if (state.RollSound != null && !Exists(state.RollSound)) state.RollSound = null;

            if (state.RollSound != null)
            {
                float min = Mathf.Max(surface.minRollSpeed - 0.7f, 0.02f);
                if (rolling < min || !state.HasCollision)
                {
                    state.RollSound.FadeOut(4.3f);
                    state.RollSound = null;
                    return;
                }
                float freq, volume;
                if (rolling >= surface.middleRollSpeed)
                {
                    if (rolling >= surface.maxRollFreqSpeed) { freq = surface.maxRollFreq; volume = surface.maxRollVolume; }
                    else
                    {
                        float t = (rolling - surface.middleRollSpeed) / (surface.maxRollFreqSpeed - surface.middleRollSpeed);
                        freq = (1f - t) + t * surface.maxRollFreq;
                        volume = (1f - t) + t * surface.maxRollVolume;
                    }
                }
                else if (rolling <= surface.minRollFreqSpeed) { freq = surface.minRollFreq; volume = surface.minRollVolume; }
                else
                {
                    float t = (surface.middleRollSpeed - rolling) / (surface.middleRollSpeed - surface.minRollFreqSpeed);
                    freq = (1f - t) + t * surface.minRollFreq;
                    volume = (1f - t) + t * surface.minRollVolume;
                }
                var entry = state.RollSound.GetEntry(SoundEntityType.Main);
                if (entry != null)
                {
                    entry.SetSpeedMul(freq);
                    entry.FadeVolumeMulTo(Mathf.Min(volume * state.RollSound.Volume, 1f), 4f);
                    state.RollSound.Position = body.Position;
                }
            }
            else if (surface.minRollSpeed <= rolling && state.HasCollision)
            {
                var sound = Sound.CreateSoundEntity("Roll", surface.rollSoundName, true);
                if (sound == null) return;
                sound.FadeIn(3.3f);
                sound.Position = body.Position;
                sound.IsSaved = false;
                state.RollSound = sound;
            }
        }

        /// <summary>~iPhysicsBody: its scrape and roll sounds go with it.</summary>
        public void OnBodyRemoved(HplPhysicsBody body)
        {
            var s = body.SurfaceState;
            if (Exists(s.ScrapeSound)) Sound.DestroySoundEntity(s.ScrapeSound);
            if (Exists(s.RollSound)) Sound.DestroySoundEntity(s.RollSound);
            s.ScrapeSound = s.RollSound = null;
        }
    }
}
