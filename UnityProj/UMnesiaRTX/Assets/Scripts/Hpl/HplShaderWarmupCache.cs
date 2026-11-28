using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>A completion receipt, not the driver cache itself. Interrupted/failed warmups never write it.</summary>
    public static class HplShaderWarmupCache
    {
        const int Format = 1;
        [Serializable] sealed class Receipt { public int format; public string fingerprint; }

        public static string Fingerprint(string revision, string driver, string device, string api, string engine) =>
            Hash(string.Join("\n", Format.ToString(), revision, driver, device, api, engine));

        public static string Hash(string text)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }

        public static bool Matches(string path, string fingerprint)
        {
            try
            {
                var receipt = JsonUtility.FromJson<Receipt>(File.ReadAllText(path));
                return receipt != null && receipt.format == Format && receipt.fingerprint == fingerprint;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { return false; }
        }

        public static void Save(string path, string fingerprint)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(new Receipt { format = Format, fingerprint = fingerprint }));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
