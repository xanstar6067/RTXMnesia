using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// A physics body of an entity (.ent &lt;Body&gt;, EntityLoader_Object.cpp SetBodyProperties). The colliders are its
    /// children (shapes), sub meshes, lights, billboards, particle systems and sounds attached to it in the .ent are
    /// children too, so they move with it. The map creates the Rigidbody at runtime (RTXMnesia.Dynamics.HplPhysicsBody).
    /// </summary>
    public class AmnesiaBody : MonoBehaviour
    {
        public int hplId = -1;
        [Tooltip("0 = static (moved only by code).")]
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
        public float maxLinearSpeed;
        public float maxAngularSpeed;
        public bool continuousCollision = true;
        public float buoyancyDensityMul = 1f;
        public bool isVolatile;
        public bool useSurfaceEffects = true;
        public bool blocksSound;
    }
}
