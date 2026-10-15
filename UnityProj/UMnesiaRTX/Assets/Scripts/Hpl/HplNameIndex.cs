using System;
using System.Collections.Generic;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// Imported game files found by name like FileSearcher::GetFilePath: the name without extension in any case,
    /// and among files of that name the one whose folders match the reference's folders from the end the most;
    /// the first added (resources.cfg order) on a tie. Old absolute paths of the editor do not hurt: only matching
    /// components count.
    /// </summary>
    public sealed class HplNameIndex<T> where T : class
    {
        readonly Dictionary<string, List<(string path, T value)>> _map = new();

        /// <param name="name">File name without extension, lower case.</param>
        /// <param name="path">Path relative to the game folder, lower case, without extension.</param>
        public void Add(string name, string path, T value)
        {
            if (!_map.TryGetValue(name, out var list)) _map[name] = list = new List<(string, T)>(1);
            list.Add((path ?? name, value));
        }

        /// <summary>The file for a reference like "grunt/amb_alert01" or "ps_dust_push.ps"; null if none has the name.</summary>
        public T Find(string reference)
        {
            string normalized = (reference ?? "").Replace('\\', '/').ToLowerInvariant();
            string name = System.IO.Path.GetFileNameWithoutExtension(normalized);
            if (!_map.TryGetValue(name, out var list)) return null;
            if (list.Count == 1) return list[0].value;

            string[] wanted = System.IO.Path.GetDirectoryName(normalized)?.Replace('\\', '/')
                                  .Split('/', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            T best = list[0].value;
            int bestScore = -1;
            foreach (var (path, value) in list)
            {
                string[] folders = path.Split('/');
                int score = 0;
                for (int i = wanted.Length - 1, j = folders.Length - 2; i >= 0 && j >= 0; i--, j--)
                {
                    if (wanted[i] != folders[j]) break;
                    score++;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    best = value;
                }
            }
            return best;
        }
    }
}
