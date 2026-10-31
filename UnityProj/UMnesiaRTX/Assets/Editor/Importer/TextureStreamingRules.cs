using System;
using UnityEditor;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Brings the mipmap streaming flag of already imported textures in line with the import rules: classic world
    /// textures (<see cref="TextureImportRules.Streams"/>) and the approved relief maps. Only the flag changes; the
    /// textures are reimported. Batch: -executeMethod RTXMnesia.Importer.TextureStreamingRules.ConfigureAllBatch
    /// </summary>
    public static class TextureStreamingRules
    {
        public const string ReliefRoot = AmnesiaPaths.ImportedAssetsRoot + "/Resources/" + ApprovedReliefProfiles.ResourceRoot;

        public static void ConfigureAllBatch()
        {
            try
            {
                int changed = 0, streamed = 0;
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { AmnesiaPaths.TexturesRoot, ReliefRoot }))
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                        bool stream;
                        if (path.StartsWith(ReliefRoot + "/", StringComparison.Ordinal)) stream = true;
                        else if (TextureConverter.TryParseRole(path, out var role)) stream = TextureImportRules.Streams(path, role);
                        else continue;
                        if (stream) streamed++;
                        if (importer.streamingMipmaps == stream) continue;
                        importer.streamingMipmaps = stream;
                        importer.SaveAndReimport();
                        changed++;
                    }
                }
                finally { AssetDatabase.StopAssetEditing(); }
                AssetDatabase.SaveAssets();
                Debug.Log($"[TextureStreaming] {streamed} streamed textures, {changed} reimported");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw;
            }
        }
    }
}
