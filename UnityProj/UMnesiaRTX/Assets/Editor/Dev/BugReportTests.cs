using RTXMnesia.Dev;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The F1 bug report in a real Play loop (<see cref="HplBugReportProbe"/>); result in Logs/BugReportPlayCheck.txt.
    /// RunLookBatch: the [Взгляд] section at the places of existing reports (<see cref="HplBugReportLookProbe"/>).
    /// </summary>
    [InitializeOnLoad]
    public static class BugReportTests
    {
        const string Pending = "RTXMnesia.BugReportTests.PlayPending", Started = "RTXMnesia.BugReportTests.Started", Look = "RTXMnesia.BugReportTests.Look";
        static BugReportTests()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                bool look = SessionState.GetBool(Look, false);
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var go = new GameObject("Bug report verification");
                    if (look) go.AddComponent<HplBugReportLookProbe>(); else go.AddComponent<HplBugReportProbe>();
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(look ? HplBugReportLookProbe.ExitCodeKey : HplBugReportProbe.ExitCodeKey, 1));
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
        public static void RunPlayBatch() => Run(false);

        /// <summary>-bugReports "dir1;dir2": report folders with save.sav and report.txt; result in Logs/BugReportLook.txt.</summary>
        public static void RunLookBatch() => Run(true);

        static void Run(bool look)
        {
            EditorSceneManager.OpenScene(HplGameUi.MainMenuScene);
            SessionState.SetBool(Look, look);
            SessionState.SetInt(look ? HplBugReportLookProbe.ExitCodeKey : HplBugReportProbe.ExitCodeKey, 1);
            SessionState.SetFloat(Started, (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
    }
}
