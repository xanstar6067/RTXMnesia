using System;
using System.IO;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch Play snapshots of insanity events on 04_wine_cellar_lab (no -quit / -nographics): the bugs, the red fade
    /// of a roar and Daniel's subtitle drawn by the HUD, see <see cref="Dev.HplInsanityPreviewProbe"/>. Screenshots in
    /// Logs/InsanityCheck.
    /// </summary>
    [InitializeOnLoad]
    public static class InsanityPreview
    {
        const string Pending = "RTXMnesia.InsanityPreview.Pending", Start = "RTXMnesia.InsanityPreview.Start";
        const string MapFile = "maps/main/ch01/04_wine_cellar_lab.map";

        static InsanityPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Insanity verification").AddComponent<Dev.HplInsanityPreviewProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplInsanityPreviewProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 240)
                {
                    Debug.LogError("Insanity preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Insanity preview is batch-only.");
                EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, MapFile)), OpenSceneMode.Single);
                SessionState.SetInt(Dev.HplInsanityPreviewProbe.ExitCodeKey, 1);
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
