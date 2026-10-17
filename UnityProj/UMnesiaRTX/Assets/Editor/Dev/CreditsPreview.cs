using System;
using System.IO;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch Play check of the credits on 29_orb_chamber (no -quit / -nographics), see
    /// <see cref="Dev.HplCreditsPreviewProbe"/>: -batchmode -executeMethod RTXMnesia.EditorDev.CreditsPreview.RunBatch.
    /// -endNum 0/1/2 (Daniel by default); screenshots in Logs/CreditsCheck/{endNum}.
    /// </summary>
    [InitializeOnLoad]
    public static class CreditsPreview
    {
        const string Pending = "RTXMnesia.CreditsPreview.Pending", Start = "RTXMnesia.CreditsPreview.Start";
        const string MapFile = "maps/main/ch03/29_orb_chamber.map";

        static CreditsPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Credits verification").AddComponent<Dev.HplCreditsPreviewProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplCreditsPreviewProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 300)
                {
                    Debug.LogError("Credits preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Credits preview is batch-only.");
                EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, MapFile)), OpenSceneMode.Single);
                SessionState.SetInt(Dev.HplCreditsPreviewProbe.ExitCodeKey, 1);
                SessionState.SetFloat(Start, (float)EditorApplication.timeSinceStartup);
                SessionState.SetBool(Pending, true);
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
