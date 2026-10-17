using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// Generated rope resources (Resources/HplRopes.asset, RopeConverter): the settings files (misc/*.rope, the
    /// attributes of their root element) and the converted materials, both by the reference text a map's rope area
    /// uses (RopeFile, Material; often an absolute path of the developers' machines), resolved at import like
    /// cResources::LoadXmlDocument / cMaterialManager::CreateMaterial find their files.
    /// </summary>
    public sealed class HplRopeAssets : ScriptableObject
    {
        public const string ResourceName = "HplRopes";

        [Serializable]
        public sealed class RopeFile
        {
            public string reference;
            public List<HplVariable> attributes = new();
        }

        [Serializable]
        public sealed class RopeMaterial
        {
            public string reference;
            public Material material;
        }

        public List<RopeFile> files = new();
        public List<RopeMaterial> materials = new();

        static HplRopeAssets _instance;
        static bool _loaded;

        public static HplRopeAssets Instance
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _instance = Resources.Load<HplRopeAssets>(ResourceName);
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _loaded = false;
        }

        /// <summary>The attributes of a rope settings file, null if it could not be loaded.</summary>
        public List<HplVariable> GetFile(string reference)
        {
            foreach (var f in files) if (f.reference == reference) return f.attributes;
            return null;
        }

        public Material GetMaterial(string reference)
        {
            foreach (var m in materials) if (m.reference == reference) return m.material;
            return null;
        }
    }
}
