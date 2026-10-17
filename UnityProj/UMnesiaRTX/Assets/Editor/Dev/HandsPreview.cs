using System;
using System.IO;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch Play check of the hands animations (no -quit / -nographics), see <see cref="Dev.HplHandsProbe"/>:
    /// draw / holster / redraw of the lantern on 01_old_archives (or -amnesiaMap) → Logs/HandsCheck (hands.csv, report.txt).
    /// </summary>
    [InitializeOnLoad]
    public static class HandsPreview
    {
        const string Pending = "RTXMnesia.HandsPreview.Pending", Start = "RTXMnesia.HandsPreview.Start";

        static HandsPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var probe = new GameObject("Hands verification").AddComponent<Dev.HplHandsProbe>();
                    probe.folder = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "HandsCheck");
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplHandsProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 240)
                {
                    Debug.LogError("Hands preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Hands preview is batch-only.");
                string[] args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-amnesiaMap");
                string map = i >= 0 && i + 1 < args.Length ? args[i + 1] : "maps/main/ch01/01_old_archives.map";
                EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, map)), OpenSceneMode.Single);
                SessionState.SetInt(Dev.HplHandsProbe.ExitCodeKey, 1);
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
