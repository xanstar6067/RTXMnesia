using System;
using System.Diagnostics;
using System.IO;
using RTXMnesia.Dev;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// The commit of a player build for the bug reports (<see cref="HplBuildInfo"/>): before each build the short hash,
    /// the branch, uncommitted changes and the date go into a text resource in the ignored Assets/AmnesiaImported.
    /// </summary>
    public sealed class WriteBuildInfo : IPreprocessBuildWithReport
    {
        public const string AssetPath = "Assets/AmnesiaImported/Resources/" + HplBuildInfo.ResourceName + ".txt";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string hash = Git("rev-parse --short=10 HEAD") ?? "без git";
            string branch = Git("rev-parse --abbrev-ref HEAD");
            string status = Git("status --porcelain --untracked-files=no");
            string text = hash + (branch != null ? " " + branch : "") + (!string.IsNullOrEmpty(status) ? " (есть незакоммиченные правки)" : "")
                          + ", собрано " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            File.WriteAllText(AssetPath, text);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        static string Git(string args)
        {
            try
            {
                var info = new ProcessStartInfo("git", args)
                {
                    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true,
                    WorkingDirectory = Path.GetFullPath(".."),
                };
                using var process = Process.Start(info);
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(5000);
                return process.ExitCode == 0 ? output : null;
            }
            catch (Exception) { return null; }
        }
    }
}
