using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTXMnesia.Enemies
{
    /// <summary>
    /// Generated data of the enemies (Resources/HplEnemies.asset, EnemyConverter): the animations of every enemy .ent
    /// (cEntityLoader_Object: name, base speed, special event time, events; the clips are legacy clips of the
    /// .dae_anim files) and the AI node connection files of the maps (&lt;map&gt;_&lt;enemy file&gt;.nodes, written by
    /// cAINodeContainer::SaveToFile and shipped with the game).
    /// </summary>
    public sealed class HplEnemyAssets : ScriptableObject
    {
        public const string ResourceName = "HplEnemies";

        [Serializable]
        public sealed class AnimationEvent
        {
            public float time;
            [Tooltip("PlaySound or Step (eAnimationEventType).")]
            public string type;
            public string value;
        }

        [Serializable]
        public sealed class Animation
        {
            public string name;
            public AnimationClip clip;
            [Tooltip("Speed of the .ent (cAnimationState base speed).")]
            public float speed = 1f;
            public float specialEventTime;
            public List<AnimationEvent> events = new();
        }

        [Serializable]
        public sealed class Enemy
        {
            [Tooltip(".ent file name without extension, e.g. servant_grunt.")]
            public string name;
            public List<Animation> animations = new();
        }

        [Serializable]
        public sealed class NodeFile
        {
            [Tooltip("File name without extension: <map>_<enemy file>, e.g. 05_wine_cellar_servant_grunt.")]
            public string name;
            [TextArea] public string xml;
        }

        public List<Enemy> enemies = new();
        public List<NodeFile> nodeFiles = new();
        [Tooltip("sounds/EnemySounds.dat: sound entity name prefixes enemies hear (cMapHandlerSoundCallback).")]
        public List<string> hearableSounds = new();

        static HplEnemyAssets _instance;
        static bool _loaded;

        public static HplEnemyAssets Instance
        {
            get
            {
                if (_loaded) return _instance;
                _loaded = true;
                _instance = Resources.Load<HplEnemyAssets>(ResourceName);
                if (_instance == null) Debug.LogWarning("[RTXMnesia] Enemy data missing: Tools > RTXMnesia > Import > Enemies");
                return _instance;
            }
        }

        public Enemy GetEnemy(string entFile)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(entFile ?? "");
            return enemies.Find(e => string.Equals(e.name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The saved connections of a map's node container, or null (then the container compiles them).</summary>
        public string GetNodeFile(string mapName, string containerName)
        {
            string name = mapName + "_" + containerName;
            var f = nodeFiles.Find(n => string.Equals(n.name, name, StringComparison.OrdinalIgnoreCase));
            return f?.xml;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _loaded = false;
        }
    }
}
