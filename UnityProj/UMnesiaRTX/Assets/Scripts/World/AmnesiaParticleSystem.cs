using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>
    /// Placeholder for an HPL2 particle system (.ps). Keeps the placement data until the .ps converter exists.
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
