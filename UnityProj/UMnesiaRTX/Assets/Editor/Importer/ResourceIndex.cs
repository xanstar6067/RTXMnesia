using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// HPL2 FileSearcher indexes by filename, then disambiguates duplicate names by directory similarity.
    /// Preserve paths even when they contain an obsolete exporter drive ("G:/amnesia/redist/...").
    /// A bare filename uses the referencing directory when available, then resources.cfg order.
    /// </summary>
    public class ResourceIndex
    {
        static readonly string[] ImageExtensions = { ".dds", ".tga", ".png", ".jpg", ".jpeg" };
        readonly Dictionary<string, List<string>> _byName = new(StringComparer.OrdinalIgnoreCase);

        public string Root { get; }

        public ResourceIndex(string root)
        {
            Root = root;
            foreach (string dir in ReadResourceDirs(root))
            {
                string abs = Path.Combine(root, dir);
                if (!Directory.Exists(abs)) continue;
                foreach (string file in Directory.EnumerateFiles(abs, "*", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    if (!_byName.TryGetValue(name, out var list)) _byName[name] = list = new List<string>();
                    if (!list.Contains(file)) list.Add(file);
                }
            }
        }

        /// <summary>Finds a resource by the file name part of <paramref name="reference"/>.</summary>
        /// <param name="reference">Any path or bare file name, e.g. "G:/amnesia/redist/x/y.dds".</param>
        /// <param name="nearFile">Absolute path of the referencing file; a match in its folder is preferred.</param>
        /// <returns>Absolute path, or null when not found.</returns>
        public string Find(string reference, string nearFile = null)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;
            string normalized = Uri.UnescapeDataString(reference.Trim()).Replace('\\', '/');
            string name = normalized;
            name = name.Substring(name.LastIndexOfAny(new[] { '/', '\\', '|' }) + 1);
            if (!_byName.TryGetValue(name, out var list))
            {
                // No extension: HPL2 tries every supported image format (TextureManager.cpp FindTexture2D).
                if (Path.HasExtension(name)) return null;
                foreach (string ext in ImageExtensions)
                    if (_byName.TryGetValue(name + ext, out list)) break;
                if (list == null) return null;
            }

            if (list.Count > 1)
            {
                // FileSearcher::GetFilePath compares matching directory components backwards in both
                // directions, allowing old machine prefixes and inserted resource directories.
                string wantedPath = normalized.Substring(0, Math.Max(0, normalized.LastIndexOf('/')));
                if (wantedPath.Length == 0 && nearFile != null) wantedPath = Path.GetDirectoryName(nearFile);
                var wanted = Directories(wantedPath);
                string best = list[0];
                int bestScore = 0;
                foreach (string candidate in list)
                {
                    var available = Directories(Path.GetDirectoryName(candidate));
                    int score = Math.Max(MatchDirectories(wanted, available), MatchDirectories(available, wanted));
                    if (score > bestScore) { best = candidate; bestScore = score; }
                }
                return best;
            }
            return list[0];
        }

        static string[] Directories(string path) => path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        static int MatchDirectories(string[] wanted, string[] available)
        {
            int matched = 0, i = wanted.Length - 1;
            for (int j = available.Length - 1; i >= 0 && j >= 0; j--)
                if (string.Equals(wanted[i], available[j], StringComparison.OrdinalIgnoreCase)) { matched++; i--; }
            return matched;
        }

        public IEnumerable<string> AllWithExtension(string extension)
        {
            foreach (var list in _byName.Values)
                foreach (string file in list)
                    if (file.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                        yield return file;
        }

        static IEnumerable<string> ReadResourceDirs(string root)
        {
            var dirs = new List<string>();
            string cfg = Path.Combine(root, "resources.cfg");
            if (File.Exists(cfg))
            {
                var doc = new XmlDocument();
                doc.Load(cfg);
                foreach (XmlElement e in doc.GetElementsByTagName("Directory"))
                {
                    string path = e.GetAttribute("Path").Trim('/', '\\');
                    if (path.Length > 0 && !dirs.Contains(path)) dirs.Add(path);
                }
            }
            // core/ is loaded by the engine itself, outside resources.cfg.
            if (!dirs.Contains("core")) dirs.Add("core");
            return dirs;
        }
    }
}
