using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// Physics materials of the game (materials.cfg, cPhysics::LoadSurfaceData): friction / elasticity as Unity
    /// PhysicsMaterials (sub assets, combine modes as in cPhysicsMaterialNewton) and the surface data that impact,
    /// scrape, roll and step sounds use (step 6).
    /// </summary>
    public class HplSurfaces : ScriptableObject
    {
        [Serializable]
        public class Effect
        {
            public float minSpeed;
            public string soundName = "";
            public string psName = "";
            public int psPrio;
        }

        [Serializable]
        public class Surface
        {
            public string name;
            public float elasticity = 0.5f, staticFriction = 0.3f, kineticFriction = 0.3f;
            public int priority;
            public string elasticityMode = "Average", frictionMode = "Average";
            public string stepType = "";
            public string scrapeSoundName = "", rollSoundName = "";
            public List<Effect> impacts = new(), hits = new();
            public PhysicsMaterial physicsMaterial;
        }

        public List<Surface> surfaces = new();

        Dictionary<string, Surface> _byName;

        /// <summary>iPhysicsWorld::GetMaterialFromName: exact name, null if missing.</summary>
        public Surface Get(string name)
        {
            if (_byName == null)
            {
                _byName = new Dictionary<string, Surface>(StringComparer.Ordinal);
                foreach (var s in surfaces) _byName.TryAdd(s.name, s);
            }
            return name != null && _byName.TryGetValue(name, out var surface) ? surface : null;
        }

        /// <summary>Bodies always have a material: "Default" when theirs is missing (iPhysicsBody constructor).</summary>
        public Surface GetOrDefault(string name) => Get(name) ?? Get("Default");

        public static PhysicsMaterialCombine Combine(string mode) => mode?.ToLowerInvariant() switch
        {
            "min" => PhysicsMaterialCombine.Minimum,
            "multiply" => PhysicsMaterialCombine.Multiply,
            "max" => PhysicsMaterialCombine.Maximum,
            _ => PhysicsMaterialCombine.Average,
        };
    }
}
