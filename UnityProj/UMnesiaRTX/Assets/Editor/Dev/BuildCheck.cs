using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Compiles the player scripts of the Windows build (as a player build does, without the scenes) and reports
    /// errors plus obsolete API warnings (CS0618 / CS0612 / CS0619) of our assemblies.
    /// Batch: -batchmode -nographics -quit -executeMethod RTXMnesia.EditorDev.BuildCheck.RunBatch
    /// </summary>
    public static class BuildCheck
    {
        static readonly string[] Obsolete = { "CS0618", "CS0612", "CS0619" };

        public static void RunBatch()
        {
            int errors = 0, obsolete = 0;
            void OnFinished(string assembly, CompilerMessage[] messages)
            {
                foreach (var m in messages)
                {
                    bool isError = m.type == CompilerMessageType.Error;
                    bool isObsolete = Obsolete.Any(c => m.message.Contains(c));
                    if (!isError && !isObsolete) continue;
                    if (!m.file.Replace('\\', '/').Contains("Assets/")) continue;
                    if (isError) errors++; else obsolete++;
                    Debug.Log($"[BuildCheck] {Path.GetFileName(assembly)}: {m.message}");
                }
            }
            CompilationPipeline.assemblyCompilationFinished += OnFinished;
            try
            {
                string output = Path.GetFullPath("Temp/BuildCheckPlayerScripts");
                var settings = new ScriptCompilationSettings
                {
                    target = BuildTarget.StandaloneWindows64,
                    group = BuildTargetGroup.Standalone,
                    options = ScriptCompilationOptions.None,
                };
                var result = PlayerBuildInterface.CompilePlayerScripts(settings, output);
                int assemblies = result.assemblies?.Count ?? 0;
                string verdict = errors == 0 && obsolete == 0 && assemblies > 0 ? "PASS" : "FAIL";
                Debug.Log($"[BuildCheck] {verdict}: player scripts, {assemblies} assemblies, {errors} errors, {obsolete} obsolete API warnings");
                if (Application.isBatchMode) EditorApplication.Exit(verdict == "PASS" ? 0 : 1);
            }
            finally
            {
                CompilationPipeline.assemblyCompilationFinished -= OnFinished;
            }
        }
    }
}
