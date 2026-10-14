using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>Collects importer messages and writes them to Logs/AmnesiaImport.log (outside git).</summary>
    public class ImportLog
    {
        readonly List<string> _lines = new();
        public int Warnings { get; private set; }
        public int Errors { get; private set; }

        public void Info(string message) => _lines.Add(message);

        public void Warn(string message)
        {
            Warnings++;
            _lines.Add("WARNING: " + message);
        }

        public void Error(string message)
        {
            Errors++;
            _lines.Add("ERROR: " + message);
        }

        public void Save(string title)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AmnesiaPaths.LogFile));
            var text = new List<string> { $"=== {title} — {DateTime.Now:yyyy-MM-dd HH:mm:ss} — {Warnings} warnings, {Errors} errors ===" };
            text.AddRange(_lines);
            text.Add("");
            File.AppendAllLines(AmnesiaPaths.LogFile, text);

            string summary = $"[RTXMnesia] {title}: {Warnings} warnings, {Errors} errors. Details: {AmnesiaPaths.LogFile}";
            if (Errors > 0) Debug.LogError(summary);
            else if (Warnings > 0) Debug.LogWarning(summary);
            else Debug.Log(summary);
        }
    }
}
