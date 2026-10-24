using UnityEngine;

namespace RTXMnesia.Sound
{
    /// <summary>
    /// Checks run in the background (Unity -batchmode: previews, Play-mode tests, the test player) play no sound: the
    /// listener is paused for the whole run. The game's sound logic does not depend on what is heard (times come from the
    /// clips' lengths, HplSoundHandler), and the user's volume (AudioListener.volume) is left as the settings set it.
    /// </summary>
    static class HplBatchAudio
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Mute()
        {
            if (Application.isBatchMode) AudioListener.pause = true;
        }
    }
}
