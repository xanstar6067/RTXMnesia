using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// A physics body of an entity (.ent &lt;Body&gt;). For now only data: colliders are its children and
    /// everything is static. Rigidbodies, joints and attached sub meshes come with the player (step 4).
    /// </summary>
    public class AmnesiaBody : MonoBehaviour
    {
        public float mass;
        [Tooltip("Physics material name from materials.cfg.")]
        public string material;
        public bool collideCharacter = true;
        public bool collideNonCharacter = true;
        public bool hasGravity = true;
        [Tooltip("A character standing on it presses it down with its weight.")]
        public bool pushedByCharacterGravity;
        [Tooltip("A character standing on it moves with it.")]
        public bool canAttachCharacter;
        public float linearDamping;
        public float angularDamping;
        public bool blocksSound;
    }
}
