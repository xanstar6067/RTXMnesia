using System;
using RTXMnesia.Importer;
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
                    EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(PendingKey, false) &&
                    EditorApplication.timeSinceStartup - SessionState.GetFloat("RTXMnesia.MenuPreview.Start", 0) > 180)
                {
                    Debug.LogError("Menu preview timed out");
                    SessionState.SetBool(PendingKey, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        // No -quit or -nographics: Play mode exits the editor after the captures.
        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Menu preview is batch-only.");
                MenuConverter.ConvertBatch();
                InventoryConverter.ConvertBatch();
                PauseTests.Run();
                InventoryTests.Run();
                EditorSceneManager.OpenScene("Assets/AmnesiaImported/Levels/maps/main/ch01/00_rainy_hall.unity", OpenSceneMode.Single);
                SessionState.SetInt(ExitCodeKey, 1);
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
