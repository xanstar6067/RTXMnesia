using UnityEngine;

namespace RTXMnesia.World
{
    /// <summary>Placeholder for an HPL2 sound entity (.snt) placed in a level. Playback comes with the sound system.</summary>
    public class AmnesiaSound : MonoBehaviour
    {
        [Tooltip("Sound entity file, e.g. general_rain_m.snt")]
        public string soundEntityFile;
        [Tooltip("Use the distances and volume from the .snt instead of the values below.")]
        public bool useDefault = true;
        public float minDistance;
        public float maxDistance;
        public float volume = 1f;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
    }
}
