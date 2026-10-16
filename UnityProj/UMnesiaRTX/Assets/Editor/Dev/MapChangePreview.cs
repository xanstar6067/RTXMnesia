using System;
using RTXMnesia.Importer;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch Play test of a map change (no -quit / -nographics): 00_rainy_hall's level door → 01_old_archives,
    /// see <see cref="Dev.HplMapChangePreviewProbe"/>. Screenshots and report in Logs/MapChangeCheck.
    /// </summary>
    [InitializeOnLoad]
    public static class MapChangePreview
    {
        const string Pending = "RTXMnesia.MapChangePreview.Pending", Start = "RTXMnesia.MapChangePreview.Start";

        static MapChangePreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Map change verification").AddComponent<Dev.HplMapChangePreviewProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplMapChangePreviewProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 360)
                {
                    Debug.LogError("Map change preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Map change preview is batch-only.");
                MainMenuConverter.ConfigureBuildScenes();
                LoadScreenConverter.ConvertBatch();
                if (HplMapChangeScene("01_old_archives") == null) throw new InvalidOperationException("01_old_archives is not imported");
                EditorSceneManager.OpenScene(HplGameUi.FirstLevelScene, OpenSceneMode.Single);
                SessionState.SetInt(Dev.HplMapChangePreviewProbe.ExitCodeKey, 1);
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

        static string HplMapChangeScene(string name)
        {
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled && System.IO.Path.GetFileNameWithoutExtension(scene.path) == name) return scene.path;
            return null;
        }
    }
}
