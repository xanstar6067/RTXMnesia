using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// Root of a model built from an HPL2 .dae (importer HplModelBuilder): children are the Collada nodes with the
    /// engine's transforms, sub meshes sit on the nodes named like in .ent files.
    /// </summary>
    public class AmnesiaModel : MonoBehaviour
    {
        [Tooltip("Source .dae, relative to the game folder.")]
        public string file;
        public int builderVersion;
    }
}
