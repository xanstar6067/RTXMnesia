using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// A particle system placed in the map or an .ent (.ps file, color, distance fade). HplMap creates the running
    /// system (RTXMnesia.Particles.HplParticleSystem) that follows this node.
    /// </summary>
    public class AmnesiaParticleSystem : MonoBehaviour
    {
        [Tooltip("Particle system file name, e.g. ps_fire_candle.ps")]
        public string file;
        public Color color = Color.white;
        public bool fadeAtDistance;
        public Vector2 minFadeDistance;
        public Vector2 maxFadeDistance;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}
