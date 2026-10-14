using System.IO;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Locations used by the importers. Source = copy of the game in repo/source (git-ignored),
    /// output = Assets/AmnesiaImported (git-ignored). Amnesia content must never leave these two folders.
    /// </summary>
    public static class AmnesiaPaths
    {
        public const string ImportedAssetsRoot = "Assets/AmnesiaImported";
        public const string TexturesRoot = ImportedAssetsRoot + "/Textures";
        public const string MaterialsRoot = ImportedAssetsRoot + "/Materials";
        public const string ModelsRoot = ImportedAssetsRoot + "/Models";

        /// <summary>Source file of an imported asset: "Assets/AmnesiaImported/Models/a/b.dae" → "&lt;source&gt;/a/b.dae".</summary>
        public static string SourceOf(string assetRoot, string assetPath) =>
            Path.GetFullPath(Path.Combine(SourceRoot, assetPath.Substring(assetRoot.Length + 1)));

        /// <summary>Absolute path of repo/source (the Unity project lives in repo/UnityProj/UMnesiaRTX).</summary>
        public static string SourceRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "source"));

        public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public static string LogFile => Path.Combine(ProjectRoot, "Logs", "AmnesiaImport.log");

        /// <summary>"static_objects/castlebase/x.dds" → "Assets/AmnesiaImported/Textures/static_objects/castlebase/x"</summary>
        public static string ToAssetPath(string assetRoot, string sourceRelativePath)
        {
            string withoutExt = Path.ChangeExtension(sourceRelativePath, null);
            return assetRoot + "/" + withoutExt.Replace('\\', '/');
        }

        public static string ToAbsolute(string assetPath) =>
            Path.GetFullPath(Path.Combine(ProjectRoot, assetPath));

        public static string ToSourceRelative(string absolutePath) =>
            Path.GetRelativePath(SourceRoot, absolutePath).Replace('\\', '/');
    }
}
