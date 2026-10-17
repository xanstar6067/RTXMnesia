using System;
using System.Globalization;
using System.IO;
using RTXMnesia.Importer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Batch Play snapshot of one view of a map (no -quit / -nographics), see <see cref="Dev.HplPlayViewProbe"/>:
    /// -amnesiaMap maps/main/ch01/06_distillery.map -playView "x y z yaw [AimEntity]" → Logs/PlayView_&lt;map&gt;.png.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayViewPreview
    {
        const string Pending = "RTXMnesia.PlayViewPreview.Pending", Start = "RTXMnesia.PlayViewPreview.Start", View = "RTXMnesia.PlayViewPreview.View";
        const string FileKey = "RTXMnesia.PlayViewPreview.File";

        static PlayViewPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var p = SessionState.GetString(View, "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float F(int i) => p.Length > i ? float.Parse(p[i], CultureInfo.InvariantCulture) : 0f;
                    var probe = new GameObject("Play view verification").AddComponent<Dev.HplPlayViewProbe>();
                    probe.position = new Vector3(F(0), F(1), F(2));
                    probe.yawDegrees = F(3);
                    probe.aim = p.Length > 4 ? p[4] : null;
                    probe.file = SessionState.GetString(FileKey, "");
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplPlayViewProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 240)
                {
                    Debug.LogError("Play view preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Play view preview is batch-only.");
                string[] args = Environment.GetCommandLineArgs();
                string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
                string map = Arg("-amnesiaMap") ?? "maps/main/ch01/00_rainy_hall.map";
                EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, map)), OpenSceneMode.Single);
                SessionState.SetString(View, Arg("-playView") ?? "");
                SessionState.SetString(FileKey, Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", $"PlayView_{Path.GetFileNameWithoutExtension(map)}.png"));
                SessionState.SetInt(Dev.HplPlayViewProbe.ExitCodeKey, 1);
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
