using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using RTXMnesia.Hpl;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Game texts → <see cref="HplLanguage"/> asset in Assets/AmnesiaImported/Lang/&lt;language&gt;.asset, updated in place.
    /// Files in HPL2's order (cLuxBase::LoadLanguage), the first entry of a name wins: config/lang_main/&lt;lang&gt;.lang,
    /// config/base_&lt;lang&gt;.lang, then the English ones for anything missing. Text decoding as cLanguageFile::AddFromFile:
    /// [br] → line break, [uNNNN] → character, two-byte UTF-8 sequences.
    /// The .ogg files of the language folder (from its RESOURCES element, e.g. lang/rus) are listed with durations.
    /// </summary>
    public static class LanguageConverter
    {
        public const string LangRoot = AmnesiaPaths.ImportedAssetsRoot + "/Lang";
        /// <summary>The user's choice: Russian, English as the fallback.</summary>
        public const string DefaultLanguage = "russian";
        const string FallbackLanguage = "english";

        public static string AssetPathFor(string language) => LangRoot + "/" + language + ".asset";

        public static HplLanguage Convert(string language, ImportLog log, HplCampaign campaign = null)
        {
            campaign ??= HplCampaign.Main;
            var files = new List<string>
            {
                $"{campaign.LanguageFolder}/{language}.lang",
                $"config/base_{language}.lang",
            };
            if (language != FallbackLanguage)
            {
                files.Add($"{campaign.LanguageFolder}/{FallbackLanguage}.lang");
                files.Add($"config/base_{FallbackLanguage}.lang");
            }

            string assetPath = campaign.AssetPath(AssetPathFor(language));
            var asset = AssetDatabase.LoadAssetAtPath<HplLanguage>(assetPath);
            bool created = asset == null;
            if (created) asset = ScriptableObject.CreateInstance<HplLanguage>();
            asset.language = language;
            asset.campaignId = campaign.Id;
            asset.sourceFiles.Clear();
            asset.categories.Clear();
            asset.voices.Clear();

            var categories = new Dictionary<string, (HplLanguage.Category category, HashSet<string> names)>(StringComparer.Ordinal);
            var resourceDirs = new List<string>();
            int entries = 0;
            for (int f = 0; f < files.Count; f++)
            {
                string file = Path.Combine(AmnesiaPaths.SourceRoot, files[f]);
                if (!File.Exists(file))
                {
                    if (f < 2) log.Warn($"Language file not found: {files[f]}");
                    continue;
                }
                var doc = new XmlDocument();
                try
                {
                    // Raw bytes as Latin-1: the decoder below works on the original byte values like HPL2.
                    // A UTF-8 byte order mark is skipped (TinyXML does too).
                    byte[] bytes = File.ReadAllBytes(file);
                    int start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                    doc.LoadXml(Encoding.GetEncoding("ISO-8859-1").GetString(bytes, start, bytes.Length - start));
                }
                catch (XmlException e)
                {
                    log.Error($"{files[f]}: {e.Message}");
                    continue;
                }
                asset.sourceFiles.Add(files[f]);
                var root = doc.DocumentElement;

                // Only the chosen language adds its resource folders (fallbacks are loaded without them).
                if (f < 2)
                {
                    foreach (XmlNode dir in root.SelectNodes("RESOURCES/Directory"))
                    {
                        string path = (dir as XmlElement)?.GetAttribute("Path").Trim('/', '\\');
                        if (!string.IsNullOrEmpty(path) && !resourceDirs.Contains(path)) resourceDirs.Add(path);
                    }
                }

                foreach (XmlNode node in root.SelectNodes("CATEGORY"))
                {
                    if (node is not XmlElement cat) continue;
                    string catName = cat.GetAttribute("Name");
                    if (!categories.TryGetValue(catName, out var c))
                    {
                        c = (new HplLanguage.Category { name = catName }, new HashSet<string>(StringComparer.Ordinal));
                        categories[catName] = c;
                        asset.categories.Add(c.category);
                    }
                    foreach (XmlNode e in cat.SelectNodes("Entry"))
                    {
                        if (e is not XmlElement entry) continue;
                        string name = entry.GetAttribute("Name");
                        if (!c.names.Add(name)) continue;
                        c.category.entries.Add(new HplLanguage.Entry { name = name, text = Decode(entry.InnerText) });
                        entries++;
                    }
                }
            }

            // Sound files of the language folders (voices, effects), with durations for timing voice lines.
            int failed = 0;
            asset.resourceDirs.Clear();
            asset.resourceDirs.AddRange(resourceDirs);
            foreach (string dir in resourceDirs)
            {
                string abs = Path.Combine(AmnesiaPaths.SourceRoot, dir);
                if (!Directory.Exists(abs)) continue;
                foreach (string ogg in Directory.EnumerateFiles(abs, "*.ogg", SearchOption.AllDirectories))
                {
                    if (!OggInfo.TryGetDuration(ogg, out float seconds))
                    {
                        failed++;
                        continue;
                    }
                    asset.voices.Add(new HplLanguage.Voice
                    {
                        name = Path.GetFileNameWithoutExtension(ogg).ToLowerInvariant(),
                        file = AmnesiaPaths.ToSourceRelative(ogg),
                        duration = seconds,
                    });
                }
            }
            if (failed > 0) log.Warn($"{failed} .ogg files of the language folders have no readable duration");

            if (created)
            {
                Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(assetPath)));
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            log.Info($"Language {language}: {entries} entries in {asset.categories.Count} categories, {asset.voices.Count} sounds " +
                     $"({string.Join(", ", resourceDirs)}) → {assetPath}");
            return asset;
        }

        /// <summary>cLanguageFile::AddFromFile text decoding on the raw bytes.</summary>
        static string Decode(string s)
        {
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '[')
                {
                    var command = new StringBuilder();
                    int count = 1;
                    while (i + count < s.Length && s[i + count] != ']' && count < 16) command.Append(s[i + count++]);
                    string cmd = command.ToString();
                    if (cmd == "br")
                    {
                        sb.Append('\n');
                        i += count;
                        continue;
                    }
                    if (cmd.Length > 0 && cmd[0] == 'u')
                    {
                        sb.Append((char)HplMath.Atoi(cmd.Substring(1)));
                        i += count;
                        continue;
                    }
                    sb.Append(c);
                }
                else if (c >= 128 && i + 1 < s.Length)
                {
                    // Two-byte UTF-8 sequence.
                    sb.Append((char)(((c & 0x1f) << 6) | (s[i + 1] & 0x3f)));
                    i++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
