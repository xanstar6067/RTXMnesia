using UnityEditor;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// HDROutputSettings.available is false unless "Use HDR Display Output" is enabled in Player Settings, which would leave the
    /// HDR options tab on "unavailable" in the editor and in builds. Keeps the setting on (HplHdr switches to the saved mode).
    /// </summary>
    [InitializeOnLoad]
    static class EnsureHdrDisplay
    {
        static EnsureHdrDisplay()
        {
            if (PlayerSettings.allowHDRDisplaySupport && PlayerSettings.useHDRDisplay) return;
            PlayerSettings.allowHDRDisplaySupport = true;
            PlayerSettings.useHDRDisplay = true;
        }
    }
}
