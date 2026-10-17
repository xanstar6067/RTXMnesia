using System;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch Play check of the pre menu (no -quit / -nographics), see <see cref="Dev.HplPreMenuPreviewProbe"/>:
    /// -batchmode -executeMethod RTXMnesia.EditorDev.PreMenuPreview.RunBatch → Logs/PreMenuCheck.
    /// </summary>
    [InitializeOnLoad]
    public static class PreMenuPreview
    {
        const string Start = "RTXMnesia.PreMenuPreview.Start";

        static PreMenuPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Dev.HplPreMenuPreviewProbe.PendingKey, false)) return;
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Dev.HplPreMenuPreviewProbe.PendingKey, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplPreMenuPreviewProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Dev.HplPreMenuPreviewProbe.PendingKey, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 180)
                {
                    Debug.LogError("Pre menu preview timed out");
                    SessionState.SetBool(Dev.HplPreMenuPreviewProbe.PendingKey, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Pre menu preview is batch-only.");
                EditorSceneManager.OpenScene(HplGameUi.MainMenuScene, OpenSceneMode.Single);
                SessionState.SetInt(Dev.HplPreMenuPreviewProbe.ExitCodeKey, 1);
                SessionState.SetFloat(Start, (float)EditorApplication.timeSinceStartup);
                SessionState.SetBool(Dev.HplPreMenuPreviewProbe.PendingKey, true);
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
