#if UNITY_EDITOR || RTXMNESIA_STARTUP_TEST
using System;
using System.Collections;
using System.IO;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.Ui;
using UnityEngine;

namespace RTXMnesia.Dev
{
    /// <summary>Release-player regression for starting the actual title scene when HDR output is unavailable.</summary>
    public sealed class HplHdrStartupProbe : MonoBehaviour
    {
        int _exceptions;

#if RTXMNESIA_STARTUP_TEST && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            if (!Application.isBatchMode) return;
            var go = new GameObject("HDR startup regression");
            DontDestroyOnLoad(go);
            go.AddComponent<HplHdrStartupProbe>();
        }
#endif

        void Awake() => Application.logMessageReceived += OnLog;
        void OnDestroy() => Application.logMessageReceived -= OnLog;
        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception) _exceptions++;
        }

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2);
            try
            {
                Check(!HplHdr.Available && !HplHdr.Active && !HplHdr.Pending, "Test requires SDR-only output (-nographics)");
                var map = HplMap.Current;
                var title = map != null ? map.GetComponent<HplMainMenu>() : null;
                var ui = title != null ? title.Ui : null;
                Check(map != null && map.menuBackground && ui != null && ui.Menu != null && ui.IsTitleScreen,
                    "Main menu initialization was interrupted");
                Check(map.sound.Music.CurrentSongName == "game_menu.ogg" && !map.sound.MenuPaused,
                    "Main menu music was not started");
                bool musicSource = false;
                foreach (var source in map.GetComponentsInChildren<AudioSource>())
                    if (source.clip != null && source.loop && source.isPlaying) musicSource = true;
                Check(musicSource, "No looping AudioSource is playing the title music");
                ui.OpenOptions(4);
                Check(ui.Choose(MenuControl.HdrTab), "HDR tab was not created in SDR");
                for (int i = 0; i < 3; i++) Check(ui.Choose(MenuControl.HdrMode), "HDR mode could not be changed in SDR");
                Check(ui.Choose(MenuControl.Cancel) && !ui.ShowingOptions && !HplHdr.CalibrationVisible,
                    "HDR options could not be cancelled");
                Check(_exceptions == 0, "Exceptions occurred during startup or HDR options");
                string result = "PASS: release player starts the real main menu and its playing music source; HDR options work without HDR support.\n";
                File.WriteAllText(Path.Combine(Application.dataPath, "../StartupResult.txt"), result);
                Debug.Log("[HdrStartup] " + result);
                Application.Quit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Application.Quit(1);
            }
        }

        static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("HDR startup: " + message);
        }
    }
}
#endif
