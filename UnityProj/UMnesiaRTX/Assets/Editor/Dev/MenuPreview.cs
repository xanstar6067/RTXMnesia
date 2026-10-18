using System;
using RTXMnesia.Importer;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>GPU Play-mode verification of the real menu. Batch only; never saves the imported scene.</summary>
    [InitializeOnLoad]
    public static class MenuPreview
    {
        public const string PendingKey = "RTXMnesia.MenuPreview.Pending";
        public const string ExitCodeKey = "RTXMnesia.MenuPreview.ExitCode";
        static MenuPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(PendingKey, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                    new GameObject("Menu verification").AddComponent<Dev.HplMenuPreviewProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(PendingKey, false);
                    RestorePrefs();
                    EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(PendingKey, false) &&
                    EditorApplication.timeSinceStartup - SessionState.GetFloat("RTXMnesia.MenuPreview.Start", 0) > 420)
                {
                    Debug.LogError("Menu preview timed out");
                    SessionState.SetBool(PendingKey, false);
                    RestorePrefs();
                    EditorApplication.Exit(1);
                }
            };
        }

        const string PrefsKey = "RTXMnesia.MenuPreview.Prefs", NoPrefs = "<none>";

        static void RestorePrefs()
        {
            string saved = SessionState.GetString(PrefsKey, null);
            if (saved == null) return;
            if (saved == NoPrefs) PlayerPrefs.DeleteKey(HplUserSettings.PrefsKey); else PlayerPrefs.SetString(HplUserSettings.PrefsKey, saved);
            PlayerPrefs.Save();
            SessionState.EraseString(PrefsKey);
        }

        // No -quit or -nographics: Play mode exits the editor after the captures.
        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Menu preview is batch-only.");
                MenuConverter.ConvertBatch();
                InventoryConverter.ConvertBatch();
                JournalConverter.ConvertBatch();
                PauseTests.Run();
                InventoryTests.Run();
                JournalTests.Run();
                EditorSceneManager.OpenScene("Assets/AmnesiaImported/Levels/maps/main/ch01/00_rainy_hall.unity", OpenSceneMode.Single);
                SessionState.SetInt(ExitCodeKey, 1);
                // The checks apply and save settings: the user's saved ones come back however the run ends.
                SessionState.SetString(PrefsKey, PlayerPrefs.HasKey(HplUserSettings.PrefsKey) ? PlayerPrefs.GetString(HplUserSettings.PrefsKey) : NoPrefs);
                SessionState.SetFloat("RTXMnesia.MenuPreview.Start", (float)EditorApplication.timeSinceStartup);
                SessionState.SetBool(PendingKey, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
