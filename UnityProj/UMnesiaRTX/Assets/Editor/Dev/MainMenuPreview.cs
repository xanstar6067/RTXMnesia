using System;
using RTXMnesia.Importer;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    [InitializeOnLoad]
    public static class MainMenuPreview
    {
        const string Pending = "RTXMnesia.MainMenuPreview.Pending", ExitCode = "RTXMnesia.MainMenuPreview.ExitCode", Start = "RTXMnesia.MainMenuPreview.Start";
        static MainMenuPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Title flow verification").AddComponent<Dev.HplMainMenuPreviewProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false); EditorApplication.Exit(SessionState.GetInt(ExitCode, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 180)
                {
                    Debug.LogError("Main menu preview timed out"); SessionState.SetBool(Pending, false); EditorApplication.Exit(1);
                }
            };
        }
        // Batch only, no -quit or -nographics. The probe exits after an actual scene / new-game / return cycle.
        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Main menu preview is batch-only.");
                MainMenuConverter.ConvertBatch();
                PauseTests.Run(); InventoryTests.Run(); JournalTests.Run(); MainMenuTests.Run();
                EditorSceneManager.OpenScene(HplGameUi.MainMenuScene, OpenSceneMode.Single);
                SessionState.SetInt(ExitCode, 1); SessionState.SetFloat(Start, (float)EditorApplication.timeSinceStartup); SessionState.SetBool(Pending, true);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
    }
}
