using System;
using System.IO;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>How a texture is used by a material. Decides pixel conversion and Unity import settings.</summary>
    public enum TextureRole
    {
        Albedo,    // Amnesia Diffuse (+ Alpha). sRGB.
        Normal,    // Amnesia NMap. Green flipped to Unity's convention.
        Mask,      // HDRP mask map built from Amnesia Specular: A = smoothness from specular power.
        Specular,  // HDRP specular color map built from Amnesia Specular: RGB = specular intensity.
        Height,    // HDRP height map (R) from Amnesia Height (alpha channel).
        Emissive,  // Amnesia Illumination. sRGB.
        Cookie,    // Spot light gobo. Stored upside down: HPL2 projects the top image row onto the light's -Y side.
    }

    /// <summary>
    /// Converts Amnesia DDS files into PNGs under Assets/AmnesiaImported/Textures, one PNG per (file, role).
    /// Output name: "&lt;dds name&gt;__&lt;role&gt;[~&lt;alpha source&gt;].png"; <see cref="TextureImportRules"/> reads the role back.
    ///
    /// Conventions verified against the HPL2 source:
    /// - Collada UVs get v = 1 - v (MeshLoaderColladaHelpers.cpp) and DDS rows are uploaded top row first,
    ///   so a PNG with the same top-to-bottom layout as the DDS matches Unity with unmodified Collada UVs.
    /// - Bitangent = cross(N, T) * w built from the flipped v (Math.cpp CreateTriTangentVectors), so green
    ///   points down the image: flipped here for Unity (green up).
    /// - Specular map: R = intensity, G = power, exponent = exp2(G * 10) + 1 (deferred_light_frag.glsl).
    /// - Height is read from the alpha channel and is a depth map (deferred_gbuffer_solid_frag.glsl): inverted.
    /// </summary>
    public static class TextureConverter
    {
        /// <summary>Bump to force regeneration of all PNGs after changing the conversion math.</summary>
        public const int Version = 2;

        public const string RoleSeparator = "__";
        public const string AlphaSeparator = "~";

        public static string OutputAssetPath(string ddsAbsolute, TextureRole role, string alphaDdsAbsolute = null)
        {
            string rel = AmnesiaPaths.ToSourceRelative(ddsAbsolute);
            string path = AmnesiaPaths.ToAssetPath(AmnesiaPaths.TexturesRoot, rel) + RoleSeparator + role.ToString().ToLowerInvariant();
            if (alphaDdsAbsolute != null && !SamePath(alphaDdsAbsolute, ddsAbsolute))
                path += AlphaSeparator + Path.GetFileNameWithoutExtension(alphaDdsAbsolute);
            return path + ".png";
        }

        public static bool TryParseRole(string assetPath, out TextureRole role)
        {
            role = default;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            int sep = name.LastIndexOf(RoleSeparator, StringComparison.Ordinal);
            if (sep < 0) return false;
            string token = name.Substring(sep + RoleSeparator.Length);
            int tilde = token.IndexOf(AlphaSeparator, StringComparison.Ordinal);
            if (tilde >= 0) token = token.Substring(0, tilde);
            return Enum.TryParse(token, true, out role);
        }

        /// <summary>Writes the PNG if missing or older than its sources. Returns the PNG asset path.</summary>
        public static string Convert(string ddsAbsolute, TextureRole role, string alphaDdsAbsolute, bool force, out bool written)
        {
            string assetPath = OutputAssetPath(ddsAbsolute, role, alphaDdsAbsolute);
            string pngAbsolute = AmnesiaPaths.ToAbsolute(assetPath);
            written = false;

            if (!force && File.Exists(pngAbsolute))
            {
                DateTime png = File.GetLastWriteTimeUtc(pngAbsolute);
                bool stale = png < VersionChangedAtUtc() || File.GetLastWriteTimeUtc(ddsAbsolute) > png ||
                             (alphaDdsAbsolute != null && File.GetLastWriteTimeUtc(alphaDdsAbsolute) > png);
                if (!stale) return assetPath;
            }

            var image = ImageLoader.Load(ddsAbsolute);
            if (image.IsCube) throw new NotSupportedException($"Cube map used as {role}: {ddsAbsolute}");
            Color32[] pixels = image.Faces[0];

            switch (role)
            {
                case TextureRole.Albedo:
                    if (alphaDdsAbsolute != null && !SamePath(alphaDdsAbsolute, ddsAbsolute))
                        CopyAlpha(pixels, image.Width, image.Height, ImageLoader.Load(alphaDdsAbsolute));
                    break;
                case TextureRole.Normal:
                    for (int i = 0; i < pixels.Length; i++) pixels[i].g = (byte)(255 - pixels[i].g);
                    break;
                case TextureRole.Mask:
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(0, 255, 0, SmoothnessFromSpecularPower(pixels[i].g));
                    break;
                case TextureRole.Specular:
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = new Color32(pixels[i].r, pixels[i].r, pixels[i].r, 255);
                    break;
                case TextureRole.Height:
                {
                    // Height lives in alpha; grayscale files without alpha keep it in luminance.
                    // HPL2 relief mapping treats it as DEPTH (white = deep), HDRP wants height (white = high):
                    // inverted. Verified on castlebase_wall_square_bricks: inverted map's gradient matches its normal map.
                    bool hasAlpha = Array.Exists(pixels, p => p.a != 255);
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        byte h = (byte)(255 - (hasAlpha ? pixels[i].a : pixels[i].r));
                        pixels[i] = new Color32(h, h, h, 255);
                    }
                    break;
                }
                case TextureRole.Cookie:
                    // LightSpot.cpp: gobo uv = projected position * 0.5 + 0.5 without a flip, so v = 0 (first DDS row,
                    // the top of the image) lands on the light's -Y side. Unity cookies put the image bottom there.
                    pixels = FlipRows(pixels, image.Width, image.Height);
                    break;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(pngAbsolute));
            File.WriteAllBytes(pngAbsolute, EncodePng(pixels, image.Width, image.Height));
            written = true;
            return assetPath;
        }

        static DateTime? _versionChangedAt;

        /// <summary>
        /// When <see cref="Version"/> was last changed (recorded in Library/). PNGs written before that moment are
        /// regenerated on their next conversion, so partial runs (one folder) also pick up new conversion math.
        /// </summary>
        static DateTime VersionChangedAtUtc()
        {
            if (_versionChangedAt.HasValue) return _versionChangedAt.Value;
            string file = Path.Combine(AmnesiaPaths.ProjectRoot, "Library", "AmnesiaTextureConverterVersion.txt");
            string[] parts = File.Exists(file) ? File.ReadAllText(file).Trim().Split('|') : Array.Empty<string>();
            if (parts.Length == 2 && parts[0] == Version.ToString() && long.TryParse(parts[1], out long ticks))
                _versionChangedAt = new DateTime(ticks, DateTimeKind.Utc);
            else
            {
                _versionChangedAt = DateTime.UtcNow;
                File.WriteAllText(file, $"{Version}|{_versionChangedAt.Value.Ticks}");
            }
            return _versionChangedAt.Value;
        }

        /// <summary>
        /// Blinn-Phong exponent n = exp2(g * 10) + 1 → Beckmann-equivalent roughness alpha = sqrt(2 / (n + 2)),
        /// HDRP perceptual smoothness = 1 - sqrt(alpha). Range: g = 0 → 0.0, g = 1 → ~0.79.
        /// </summary>
        public static byte SmoothnessFromSpecularPower(byte g)
        {
            double n = Math.Pow(2.0, g / 255.0 * 10.0) + 1.0;
            double alpha = Math.Sqrt(2.0 / (n + 2.0));
            double smoothness = 1.0 - Math.Sqrt(alpha);
            return (byte)Math.Round(Math.Clamp(smoothness, 0.0, 1.0) * 255.0);
        }

        static void CopyAlpha(Color32[] dst, int width, int height, DdsDecoder.Image alphaSource)
        {
            Color32[] src = alphaSource.Faces[0];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int sx = x * alphaSource.Width / width, sy = y * alphaSource.Height / height;
                    dst[y * width + x].a = src[sy * alphaSource.Width + sx].a;
                }
        }

        static Color32[] FlipRows(Color32[] pixels, int width, int height)
        {
            var flipped = new Color32[pixels.Length];
            for (int y = 0; y < height; y++)
                Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
            return flipped;
        }

        /// <summary>Pixels are top row first; Unity textures are bottom row first, so rows are flipped.</summary>
        static byte[] EncodePng(Color32[] topDown, int width, int height)
        {
            var bottomUp = FlipRows(topDown, width, height);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            try
            {
                tex.SetPixels32(bottomUp);
                return tex.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        static bool SamePath(string a, string b) =>
            string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
    }
}
