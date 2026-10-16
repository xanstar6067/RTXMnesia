using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using RTXMnesia.Hpl;
using RTXMnesia.Sound;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The game's sounds → <see cref="HplSoundBank"/> in Assets/AmnesiaImported/Sounds/sounds.asset: every .ogg /
    /// .wav of sounds/ and music/ (and the language folder's voices) is copied as is to
    /// Assets/AmnesiaImported/Sounds/&lt;source path&gt; and imported by Unity (<see cref="SoundImportRules"/>);
    /// every .snt is parsed like cSoundEntityData::CreateFromFile. Files are found at runtime by name like
    /// cSoundManager / FileSearcher; the bank keeps resources.cfg order for equal names. Unchanged files are not
    /// copied again, so a re-run only takes the .snt parsing.
    /// </summary>
    public static class SoundConverter
    {
        public const string SoundsRoot = AmnesiaPaths.ImportedAssetsRoot + "/Sounds";
        public const string BankPath = SoundsRoot + "/sounds.asset";
        static readonly string[] SoundFolders = { "sounds", "music" };

        public static HplSoundBank Convert(ImportLog log, HplLanguage language)
        {
            string bankPath = HplCampaign.ForLanguage(language).AssetPath(BankPath);
            Directory.CreateDirectory(AmnesiaPaths.ToAbsolute(Path.GetDirectoryName(bankPath)));
            var files = new List<string>(); // source-relative, in search order
            foreach (string folder in SoundFolders)
            {
                string abs = Path.Combine(AmnesiaPaths.SourceRoot, folder);
                if (!Directory.Exists(abs))
                {
                    log.Error($"Sound folder not found: {abs}");
                    continue;
                }
                foreach (string f in Directory.EnumerateFiles(abs, "*.*", SearchOption.AllDirectories))
                    if (IsSound(f)) files.Add(AmnesiaPaths.ToSourceRelative(f));
            }
            if (language != null)
                foreach (var v in language.voices) files.Add(v.file);

            int copied = CopyFiles(files);
            log.Info($"Sounds: {files.Count} files, {copied} copied now");
            if (copied > 0)
            {
                EditorUtility.DisplayProgressBar("Amnesia: sounds", $"Unity is importing {copied} sounds...", 1f);
                try { AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); }
                finally { EditorUtility.ClearProgressBar(); }
            }

            var bank = AssetDatabase.LoadAssetAtPath<HplSoundBank>(bankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<HplSoundBank>();
                AssetDatabase.CreateAsset(bank, bankPath);
            }

            bank.clips.Clear();
            int missing = 0;
            foreach (string rel in files)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetPathOf(rel));
                if (clip == null)
                {
                    missing++;
                    continue;
                }
                bank.clips.Add(new HplSoundBank.Clip { name = Name(rel), path = PathKey(rel), clip = clip });
            }
            if (missing > 0) log.Warn($"{missing} sound files were not imported by Unity");

            bank.soundEntities.Clear();
            int bad = 0;
            foreach (string folder in SoundFolders)
            {
                string abs = Path.Combine(AmnesiaPaths.SourceRoot, folder);
                if (!Directory.Exists(abs)) continue;
                foreach (string snt in Directory.EnumerateFiles(abs, "*.snt", SearchOption.AllDirectories))
                {
                    try { bank.soundEntities.Add(LoadSnt(snt)); }
                    catch (Exception e)
                    {
                        bad++;
                        log.Warn($"{AmnesiaPaths.ToSourceRelative(snt)}: {e.Message}");
                    }
                }
            }
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            log.Info($"Sound bank {bankPath}: {bank.clips.Count} clips, {bank.soundEntities.Count} sound entities" +
                     (bad > 0 ? $", {bad} unreadable .snt" : ""));
            return bank;
        }

        static bool IsSound(string file)
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            return ext == ".ogg" || ext == ".wav";
        }

        static string AssetPathOf(string sourceRelative) => SoundsRoot + "/" + sourceRelative.Replace('\\', '/');
        static string Name(string file) => Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
        static string PathKey(string file) => Path.ChangeExtension(file, null).Replace('\\', '/').ToLowerInvariant();

        static int CopyFiles(List<string> files)
        {
            int copied = 0;
            try
            {
                for (int i = 0; i < files.Count; i++)
                {
                    string src = Path.Combine(AmnesiaPaths.SourceRoot, files[i]);
                    string dst = AmnesiaPaths.ToAbsolute(AssetPathOf(files[i]));
                    var s = new FileInfo(src);
                    var d = new FileInfo(dst);
                    if (d.Exists && d.Length == s.Length && d.LastWriteTimeUtc >= s.LastWriteTimeUtc) continue;
                    if (i % 50 == 0)
                        EditorUtility.DisplayProgressBar("Amnesia: sounds", files[i], (float)i / files.Count);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));
                    File.Copy(src, dst, true);
                    copied++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            return copied;
        }

        /// <summary>cSoundEntityData::CreateFromFile, with its defaults.</summary>
        static HplSoundBank.SoundEntityData LoadSnt(string file)
        {
            var doc = new XmlDocument();
            try { doc.Load(file); }
            catch (XmlException)
            {
                // TinyXML reads bytes: a stray non-UTF-8 byte (28_amb_loop.snt) does not stop it.
                doc.LoadXml(System.Text.Encoding.GetEncoding(28591).GetString(File.ReadAllBytes(file)));
            }
            var root = doc.DocumentElement;
            var sounds = First(root, "SOUNDS") ?? throw new InvalidDataException("no SOUNDS element");
            var props = First(root, "PROPERTIES") ?? throw new InvalidDataException("no PROPERTIES element");

            string rel = AmnesiaPaths.ToSourceRelative(file);
            return new HplSoundBank.SoundEntityData
            {
                name = Name(rel),
                path = PathKey(rel),
                main = SoundNames(First(sounds, "Main")),
                start = SoundNames(First(sounds, "Start")),
                stop = SoundNames(First(sounds, "Stop")),
                use3D = Bool(props, "Use3D", true),
                loop = Bool(props, "Loop", true),
                stream = Bool(props, "Stream", true),
                blockable = Bool(props, "Blockable", false),
                blockVolumeMul = props.Float("BlockVolumeMul", 0.6f),
                volume = props.Float("Volume", 1f),
                maxDistance = props.Float("MaxDistance", 1f),
                minDistance = props.Float("MinDistance", 1f),
                fadeStart = Bool(props, "FadeStart", true),
                fadeStop = Bool(props, "FadeStop", true),
                random = props.Float("Random", 1f),
                interval = props.Float("Interval"),
                priority = props.Int("Priority"),
            };
        }

        /// <summary>cString::ToBool: present and not "true" (any case) is false.</summary>
        static bool Bool(XmlElement e, string name, bool fallback) =>
            e.HasAttribute(name) ? e.GetAttribute(name).ToLowerInvariant() == "true" : fallback;

        static XmlElement First(XmlElement parent, string name)
        {
            foreach (var c in parent.Children())
                if (c.Name == name) return c;
            return null;
        }

        static string[] SoundNames(XmlElement e)
        {
            if (e == null) return Array.Empty<string>();
            var names = new List<string>();
            foreach (var c in e.Children()) names.Add(c.Str("File"));
            return names.ToArray();
        }
    }

    /// <summary>Import settings of the copied game sounds: music and voices stream, short sounds are decompressed.</summary>
    public class SoundImportRules : AssetPostprocessor
    {
        const long DecompressBelowBytes = 200 * 1024;

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(SoundConverter.SoundsRoot + "/")) return;
            var importer = (AudioImporter)assetImporter;
            string rel = assetPath.Substring(SoundConverter.SoundsRoot.Length + 1).ToLowerInvariant();
            bool stream = rel.StartsWith("music/") || rel.StartsWith("lang/");
            long size = new FileInfo(AmnesiaPaths.ToAbsolute(assetPath)).Length;

            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.loadType = stream ? AudioClipLoadType.Streaming
                : size < DecompressBelowBytes ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
            settings.preloadAudioData = !stream;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
        }
    }
}
