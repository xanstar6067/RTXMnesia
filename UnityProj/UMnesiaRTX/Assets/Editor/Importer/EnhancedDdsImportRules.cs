using UnityEditor;
using UnityEngine;
using System;

namespace RTXMnesia.Importer
{
    /// <summary>Native, externally compressed DDS: Unity must not decode/recompress the PBR package.</summary>
    public sealed class EnhancedDdsImportRules : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(EnhancedMaterialConverter.Root + "/") || !assetPath.EndsWith(".dds")) return;
            if (assetImporter is IHVImageFormatImporter importer) Configure(importer);
        }

        public static bool Configure(IHVImageFormatImporter importer)
        {
            bool changed = !importer.streamingMipmaps || importer.isReadable || importer.ignoreMipmapLimit ||
                importer.streamingMipmapsPriority != 0 || importer.wrapMode != TextureWrapMode.Repeat || importer.filterMode != FilterMode.Trilinear;
            // IHV defaults to sRGB even for a linear DX10 header; unlike TextureImporter it has no public setter.
            var serialized = new SerializedObject(importer);
            var srgb = serialized.FindProperty("m_sRGBTexture");
            if (srgb == null) throw new InvalidOperationException("Missing native DDS sRGB setting: " + importer.assetPath);
            bool isAlbedo = importer.assetPath.EndsWith("__albedo.dds", StringComparison.Ordinal);
            if (srgb.boolValue != isAlbedo)
            {
                srgb.boolValue = isAlbedo;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            importer.streamingMipmaps = true;
            importer.streamingMipmapsPriority = 0;
            importer.ignoreMipmapLimit = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            return changed;
        }

        public static void ConfigureAllBatch()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { EnhancedMaterialConverter.Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".dds", StringComparison.Ordinal)) continue;
                var importer = AssetImporter.GetAtPath(path) as IHVImageFormatImporter;
                if (importer == null) throw new InvalidOperationException("Expected native DDS: " + path);
                if (Configure(importer)) { importer.SaveAndReimport(); count++; }
            }
            Debug.Log($"[EnhancedTextures] Configured {count} native DDS imports");
        }
    }
}
