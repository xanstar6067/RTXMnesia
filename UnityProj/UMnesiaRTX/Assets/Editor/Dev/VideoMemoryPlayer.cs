using System;
using System.IO;
using System.Linq;
using RTXMnesia.Ui;
using RTXMnesia.World;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// A test player of the title screen and the six benchmark maps with HplBenchmarkAuto, to measure real video
    /// memory (the editor keeps full textures resident: streaming budgets do not show there). Never the release build;
    /// delete the folder after the check. -playerOut &lt;folder&gt; (required).
    /// Run: Bench.exe -benchmarkAuto &lt;report.txt&gt; -benchmarkSet field=value,...
    /// </summary>
    public static class VideoMemoryPlayer
    {
        public static void BuildBatch()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-playerOut");
                if (i < 0 || i + 1 >= args.Length) throw new ArgumentException("-playerOut <folder> is required");
                string folder = Path.GetFullPath(args[i + 1]);
                Directory.CreateDirectory(folder);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { HplGameUi.MainMenuScene }.Concat(HplBenchmarkRoute.All.Select(r => r.ScenePath)).ToArray(),
                    locationPathName = Path.Combine(folder, "Bench.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                    extraScriptingDefines = new[] { "RTXMNESIA_BENCHMARK_AUTO" },
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Video memory test player: " + report.summary.result);
                Debug.Log("[VideoMemoryPlayer] Test player built: " + folder);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
