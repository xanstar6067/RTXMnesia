using UnityEditor;

namespace RTXMnesia.Importer
{
    /// <summary>Import settings for PNGs produced by <see cref="TextureConverter"/>, chosen by the role in the file name.</summary>
    public class TextureImportRules : AssetPostprocessor
    {
        // Leaves room for AI-upscaled textures later (originals are at most 2048).
        const int MaxSize = 4096;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AmnesiaPaths.TexturesRoot + "/")) return;
            if (!TextureConverter.TryParseRole(assetPath, out TextureRole role)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.anisoLevel = 8;
            importer.maxTextureSize = MaxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;

            switch (role)
            {
                case TextureRole.Albedo:
                case TextureRole.Emissive:
                    importer.sRGBTexture = true;
                    break;
                case TextureRole.Normal:
                    importer.textureType = TextureImporterType.NormalMap;
                    break;
                case TextureRole.Mask:
                case TextureRole.Specular:
                    importer.sRGBTexture = false;
                    break;
                case TextureRole.Height:
                    importer.textureType = TextureImporterType.SingleChannel;
                    importer.sRGBTexture = false;
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    settings.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                    importer.SetTextureSettings(settings);
                    break;
            }
        }
    }
}
