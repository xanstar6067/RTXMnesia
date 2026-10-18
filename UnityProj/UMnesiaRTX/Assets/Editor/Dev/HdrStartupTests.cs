using System;
using System.IO;
using RTXMnesia.Ui;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>A release build of the real title scene with a startup probe, without changing the main build list.</summary>
    public static class HdrStartupTests
    {
        public static void BuildPlayerBatch()
        {
            try
            {
                string folder = Path.GetFullPath("Logs/HdrStartup/Player");
                Directory.CreateDirectory(folder);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { HplGameUi.MainMenuScene },
                    locationPathName = Path.Combine(folder, "HdrStartup.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                    extraScriptingDefines = new[] { "RTXMNESIA_STARTUP_TEST" },
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("HDR startup player build: " + report.summary.result);
                Debug.Log("[HdrStartup] Release player built: " + folder);
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
