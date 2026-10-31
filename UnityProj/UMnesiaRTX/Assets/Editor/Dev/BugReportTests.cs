using RTXMnesia.Dev;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>The F1 bug report in a real Play loop (<see cref="HplBugReportProbe"/>); result in Logs/BugReportPlayCheck.txt.</summary>
    [InitializeOnLoad]
    public static class BugReportTests
    {
        const string Pending = "RTXMnesia.BugReportTests.PlayPending", Started = "RTXMnesia.BugReportTests.Started";
        static BugReportTests()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode) new GameObject("Bug report verification").AddComponent<HplBugReportProbe>();
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(HplBugReportProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (!SessionState.GetBool(Pending, false) || EditorApplication.timeSinceStartup - SessionState.GetFloat(Started, 0) <= 400) return;
                Debug.LogError("Bug report Play check timed out");
                SessionState.SetBool(Pending, false);
                EditorApplication.Exit(1);
            };
        }

        // No -quit / -nographics: entering and leaving Play mode controls the batch process lifetime; the screenshot needs a GPU.
        public static void RunPlayBatch()
        {
            EditorSceneManager.OpenScene(HplGameUi.MainMenuScene);
            SessionState.SetInt(HplBugReportProbe.ExitCodeKey, 1);
            SessionState.SetFloat(Started, (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
