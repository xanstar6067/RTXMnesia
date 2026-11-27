using UnityEngine;

namespace RTXMnesia.Sound
{
    /// <summary>
    /// Checks run in the background (Unity -batchmode: previews, Play-mode tests, the test player) play no sound: the
    /// listener is paused for the whole run. The game's sound logic does not depend on what is heard (times come from the
    /// clips' lengths, HplSoundHandler), and the user's volume (AudioListener.volume) is left as the settings set it.
    /// </summary>
    public static class HplBatchAudio
    {
        /// <summary>A background check: batch mode, or a check that needs a window (-hplMute).</summary>
        public static bool IsCheckRun =>
            Application.isBatchMode || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-hplMute") >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Mute()
        {
            if (IsCheckRun) AudioListener.pause = true;
        }
    }
}
