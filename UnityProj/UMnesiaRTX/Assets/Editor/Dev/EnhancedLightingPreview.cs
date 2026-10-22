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
    /// Batch Play snapshots of the enhanced lighting (no -quit / -nographics, GPU with DXR for the ray traced modes),
    /// see <see cref="Dev.HplEnhancedLightingProbe"/>: -amnesiaMap maps/main/ch01/01_old_archives.map
    /// -lightViews "name:feet_x feet_y feet_z target_x target_y target_z[|mode,mode];..." (HPL2 coordinates)
    /// [-lightModes classic_raster,classic_rt,enhanced_raster,enhanced_rt — "lantern" / "noshadow" in a mode: the lantern
    /// in the hand, without its RT shadows] [-lightLit lamp,lamp] [-lightSize 1600x900]
    /// → Logs/EnhancedLighting/&lt;map&gt;_&lt;view&gt;_&lt;mode&gt;.png and &lt;map&gt;_report.txt.
    /// </summary>
    [InitializeOnLoad]
    public static class EnhancedLightingPreview
    {
        const string Pending = "RTXMnesia.EnhancedLightingPreview.Pending", Start = "RTXMnesia.EnhancedLightingPreview.Start";
        const string ViewsKey = "RTXMnesia.EnhancedLightingPreview.Views", ModesKey = "RTXMnesia.EnhancedLightingPreview.Modes";
        const string MapKey = "RTXMnesia.EnhancedLightingPreview.Map", SizeKey = "RTXMnesia.EnhancedLightingPreview.Size";
        const string LitKey = "RTXMnesia.EnhancedLightingPreview.Lit";

        static EnhancedLightingPreview()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (!SessionState.GetBool(Pending, false)) return;
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    var probe = new GameObject("Enhanced lighting verification").AddComponent<Dev.HplEnhancedLightingProbe>();
                    probe.views = SessionState.GetString(ViewsKey, "");
                    probe.modes = SessionState.GetString(ModesKey, "");
                    probe.lit = SessionState.GetString(LitKey, "");
                    probe.mapName = SessionState.GetString(MapKey, "map");
                    probe.folder = Path.Combine(AmnesiaPaths.ProjectRoot, "Logs", "EnhancedLighting");
                    var size = SessionState.GetString(SizeKey, "1600x900").Split('x');
                    probe.width = int.Parse(size[0], CultureInfo.InvariantCulture);
                    probe.height = int.Parse(size[1], CultureInfo.InvariantCulture);
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(SessionState.GetInt(Dev.HplEnhancedLightingProbe.ExitCodeKey, 1));
                }
            };
            EditorApplication.update += () =>
            {
                if (SessionState.GetBool(Pending, false) && EditorApplication.timeSinceStartup - SessionState.GetFloat(Start, 0) > 600)
                {
                    Debug.LogError("Enhanced lighting preview timed out");
                    SessionState.SetBool(Pending, false);
                    EditorApplication.Exit(1);
                }
            };
        }

        public static void RunBatch()
        {
            try
            {
                if (!Application.isBatchMode) throw new InvalidOperationException("Enhanced lighting preview is batch-only.");
                string[] args = Environment.GetCommandLineArgs();
                string Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
                string map = Arg("-amnesiaMap") ?? "maps/main/ch01/01_old_archives.map";
                EditorSceneManager.OpenScene(LevelConverter.ScenePathFor(Path.Combine(AmnesiaPaths.SourceRoot, map)), OpenSceneMode.Single);
                // Preview only: keep the introduction, fades and sanity effects from changing the comparison.
                // The scene is never saved; normal Play still runs its script.
                var previewMap = UnityEngine.Object.FindAnyObjectByType<ScriptApi.HplMap>();
                previewMap.runScript = false;
                previewMap.disableAutoSaves = true;
                previewMap.disableInsanity = true;
                previewMap.disableEnemyAI = true;
                SessionState.SetString(ViewsKey, Arg("-lightViews") ?? "");
                SessionState.SetString(ModesKey, Arg("-lightModes") ?? "");
                SessionState.SetString(LitKey, Arg("-lightLit") ?? "");
                SessionState.SetString(MapKey, Path.GetFileNameWithoutExtension(map));
                SessionState.SetString(SizeKey, Arg("-lightSize") ?? "1600x900");
                SessionState.SetInt(Dev.HplEnhancedLightingProbe.ExitCodeKey, 1);
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
