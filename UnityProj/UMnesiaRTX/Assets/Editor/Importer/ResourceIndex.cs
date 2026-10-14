using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Mirrors the HPL2 resource lookup: files are found by file name only, in the directories listed in
    /// resources.cfg. Paths written inside .mat/.dae files (often "G:/amnesia/redist/...") are ignored.
    /// When a name exists in several folders, the file next to the referencing file wins, then the first
    /// directory in resources.cfg order.
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
            string name = reference.Trim();
            name = name.Substring(name.LastIndexOfAny(new[] { '/', '\\', '|' }) + 1);
            if (!_byName.TryGetValue(name, out var list))
            {
                // No extension: HPL2 tries every supported image format (TextureManager.cpp FindTexture2D).
                if (Path.HasExtension(name)) return null;
                foreach (string ext in ImageExtensions)
                    if (_byName.TryGetValue(name + ext, out list)) break;
                if (list == null) return null;
            }

            if (nearFile != null && list.Count > 1)
            {
                string dir = Path.GetDirectoryName(nearFile);
                foreach (string candidate in list)
                    if (string.Equals(Path.GetDirectoryName(candidate), dir, StringComparison.OrdinalIgnoreCase))
                        return candidate;
            }
            return list[0];
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
