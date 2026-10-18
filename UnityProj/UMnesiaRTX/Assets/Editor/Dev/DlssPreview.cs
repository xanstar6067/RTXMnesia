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
    /// Batch Play check of DLSS (no -quit / -nographics, NVIDIA RTX GPU), see <see cref="Dev.HplDlssProbe"/>:
    /// -amnesiaMap maps/main/ch01/01_old_archives.map [-playView "x y z yaw [AimEntity | pitch=degrees]"] (default: the map's start) [-dlssSize 1920x1080]
    /// → Logs/DlssCheck (snapshots per mode, DlssReport_&lt;map&gt;.txt). Exit code 0 when every check passes.
    /// </summary>
    [InitializeOnLoad]
    public static class DlssPreview
    {
        const string Pending = "RTXMnesia.DlssPreview.Pending", Start = "RTXMnesia.DlssPreview.Start", View = "RTXMnesia.DlssPreview.View";
        const string MapKey = "RTXMnesia.DlssPreview.Map", SizeKey = "RTXMnesia.DlssPreview.Size";

        static DlssPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var p = SessionState.GetString(View, "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float F(int i) => p.Length > i ? float.Parse(p[i], CultureInfo.InvariantCulture) : 0f;
                    var probe = new GameObject("DLSS verification").AddComponent<Dev.HplDlssProbe>();
                    probe.place = p.Length >= 4;
                    probe.position = new Vector3(F(0), F(1), F(2));
                    probe.yawDegrees = F(3);
                    probe.aim = p.Length > 4 ? p[4] : null;
                    probe.folder = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "DlssCheck");
                    probe.mapName = SessionState.GetString(MapKey, "map");
                    var size = SessionState.GetString(SizeKey, "1920x1080").Split('x');
                    probe.width = int.Parse(size[0], CultureInfo.InvariantCulture);
                    probe.height = int.Parse(size[1], CultureInfo.InvariantCulture);
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplDlssProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 300)
                {
                    Debug.LogError("DLSS preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("DLSS preview is batch-only.");
                string[] args = Environment.GetCommandLineArgs();
                string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
                string map = Arg("-amnesiaMap") ?? "maps/main/ch01/01_old_archives.map";
                EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, map)), OpenSceneMode.Single);
                SessionState.SetString(View, Arg("-playView") ?? "");
                SessionState.SetString(MapKey, Path.GetFileNameWithoutExtension(map));
                SessionState.SetString(SizeKey, Arg("-dlssSize") ?? "1920x1080");
                SessionState.SetInt(Dev.HplDlssProbe.ExitCodeKey, 1);
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
