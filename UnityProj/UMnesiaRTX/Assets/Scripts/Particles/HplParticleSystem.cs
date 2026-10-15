using System.Collections.Generic;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using UnityEngine;

namespace RTXMnesia.Particles
{
    /// <summary>
    /// A running particle system (cParticleSystem): its emitters, color and distance fade, placed by a parent node
    /// (it follows it, like a child entity in HPL2) and a local matrix, or by a fixed matrix. The emitters draw
    /// through hidden objects under <see cref="RendererRoot"/> (not saved with the scene).
    /// </summary>
    public sealed class HplParticleSystem
    {
        public string Name { get; }
        /// <summary>The .ps name it was created from (GetDataName).</summary>
        public string DataName { get; }
        public HplParticleBank.SystemData Data { get; }
        public readonly List<HplParticleEmitter> Emitters = new();

        /// <summary>The node it follows; null: <see cref="LocalMatrix"/> is its world matrix.</summary>
        public Transform Parent;
        /// <summary>Unity space, relative to <see cref="Parent"/> (or the world).</summary>
        public Matrix4x4 LocalMatrix = Matrix4x4.identity;

        public Color Color = Color.white;
        public bool FadeAtDistance;
        public float MinFadeDistanceStart = 2f, MinFadeDistanceEnd = 1f, MaxFadeDistanceStart = 100f, MaxFadeDistanceEnd = 110f;
        public bool RemoveWhenDead = true;
        public bool IsSaved = true;
        /// <summary>iEntity::IsActive: an inactive system does not update.</summary>
        public bool Active = true;
        public bool Visible { get; private set; } = true;

        bool _firstUpdate = true, _hadParent;
        Matrix4x4 _lastWorld = Matrix4x4.identity;

        public HplParticleSystem(string name, string dataName, HplParticleBank.SystemData data)
        {
            Name = name;
            DataName = dataName;
            Data = data;
            foreach (var e in data.emitters) Emitters.Add(new HplParticleEmitter(this, e));
        }

        /// <summary>Unity world matrix. A parent that is gone leaves the system where it was.</summary>
        public Matrix4x4 UnityWorldMatrix
        {
            get
            {
                if (Parent != null)
                {
                    _hadParent = true;
                    return _lastWorld = Parent.localToWorldMatrix * LocalMatrix;
                }
                return _hadParent ? _lastWorld : LocalMatrix;
            }
        }

        public Vector3 UnityWorldPosition => UnityWorldMatrix.GetColumn(3);

        /// <summary>The world matrix in HPL2 space (scale included, like the map's Scale).</summary>
        public Matrix4x4 HplWorldMatrix => HplSpace.Mirror(UnityWorldMatrix);

        /// <summary>Stops following the parent, staying where it is (the parent is about to go).</summary>
        public void Detach()
        {
            if (Parent == null) return;
            LocalMatrix = UnityWorldMatrix;
            Parent = null;
            _hadParent = false;
        }

        public void SetVisible(bool visible) => Visible = visible;

        public bool IsDead
        {
            get
            {
                foreach (var e in Emitters) if (!e.IsDead) return false;
                return true;
            }
        }

        public bool IsDying
        {
            get
            {
                foreach (var e in Emitters) if (!e.IsDying) return false;
                return true;
            }
        }

        /// <summary>cParticleSystem::Kill: the emitters stop respawning, the system goes when its particles are dead.</summary>
        public void Kill()
        {
            foreach (var e in Emitters) e.Kill();
        }

        public void KillInstantly()
        {
            foreach (var e in Emitters) e.KillInstantly();
        }

        /// <summary>cParticleSystem::UpdateLogic: the warm up of each emitter on the first update, then a step.</summary>
        public void UpdateLogic(float timeStep)
        {
            if (!Active) return;
            foreach (var e in Emitters)
            {
                if (_firstUpdate && e.Data.warmUpTime > 0f)
                {
                    float time = e.Data.warmUpTime;
                    float stepSize = 1f / e.Data.warmUpStepsPerSec;
                    while (time > 0f)
                    {
                        e.UpdateLogic(stepSize);
                        time -= stepSize;
                    }
                }
                e.UpdateLogic(timeStep);
            }
            _firstUpdate = false;
        }

        public void UpdateGraphics()
        {
            foreach (var e in Emitters) e.UpdateGraphics(Visible);
        }

        public void DestroyGraphics()
        {
            foreach (var e in Emitters) e.DestroyGraphics();
        }

        public int ParticleCount
        {
            get
            {
                int n = 0;
                foreach (var e in Emitters) n += e.ParticleCount;
                return n;
            }
        }

        static Transform _rendererRoot;

        /// <summary>Holder of the emitters' renderers at the scene root (identity, not saved).</summary>
        public static Transform RendererRoot
        {
            get
            {
                if (_rendererRoot == null)
                    _rendererRoot = new GameObject("HplParticles (runtime)") { hideFlags = HideFlags.DontSave }.transform;
                return _rendererRoot;
            }
        }

        public static void DestroyRendererRoot()
        {
            if (_rendererRoot == null) return;
            Object.DestroyImmediate(_rendererRoot.gameObject);
            _rendererRoot = null;
        }
    }

    /// <summary>The world's particle systems (cWorld: CreateParticleSystem, UpdateParticles, DestroyParticleSystem).</summary>
    public sealed class HplParticleWorld
    {
        public HplParticleBank Bank;
        public readonly List<HplParticleSystem> Systems = new();

        public HplParticleWorld(HplParticleBank bank) => Bank = bank;

        /// <summary>cWorld::CreateParticleSystem (cParticleManager::CreatePS): null with an error when the .ps is missing.</summary>
        public HplParticleSystem Create(string name, string file, bool removeWhenDead = true)
        {
            var data = Bank != null ? Bank.Get(file) : null;
            if (data == null)
            {
                HplLog.Error($"Couldn't create particle system '{name}' of type '{file}'");
                return null;
            }
            if (data.emitters.Count == 0)
            {
                HplLog.Warn($"Particle system '{data.name}' has no emitters.");
                HplLog.Error($"Couldn't create particle system '{name}' of type '{file}'");
                return null;
            }
            var ps = new HplParticleSystem(name, file, data) { RemoveWhenDead = removeWhenDead };
            Systems.Add(ps);
            return ps;
        }

        /// <summary>cWorld::UpdateParticles: every system steps; dead ones that may go are removed.</summary>
        public void Update(float timeStep)
        {
            for (int i = 0; i < Systems.Count;)
            {
                var ps = Systems[i];
                ps.UpdateLogic(timeStep);
                if (ps.RemoveWhenDead && ps.IsDead)
                {
                    Systems.RemoveAt(i);
                    ps.DestroyGraphics();
                    continue;
                }
                ps.UpdateGraphics();
                i++;
            }
        }

        public void Destroy(HplParticleSystem ps)
        {
            if (ps == null) return;
            ps.DestroyGraphics();
            Systems.Remove(ps);
        }

        public bool Exists(HplParticleSystem ps) => ps != null && Systems.Contains(ps);

        /// <summary>All systems and the renderer holder go (the map is destroyed; DontSave objects would outlive Play).</summary>
        public void DestroyAll()
        {
            foreach (var ps in Systems) ps.DestroyGraphics();
            Systems.Clear();
            HplParticleSystem.DestroyRendererRoot();
        }
    }
}
