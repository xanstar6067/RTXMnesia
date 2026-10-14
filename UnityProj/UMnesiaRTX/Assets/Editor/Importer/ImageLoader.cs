using System;
using System.IO;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Loads any texture file Amnesia references (.dds, .tga, .jpg, .png) as top-row-first pixels.
    /// HPL2 loads non-DDS images through DevIL with IL_ORIGIN_UPPER_LEFT (BitmapLoaderDevil.cpp),
    /// so every format ends up with the same orientation as DDS.
    /// </summary>
    public static class ImageLoader
    {
        public static DdsDecoder.Image Load(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".dds" => DdsDecoder.Load(path),
                ".tga" => LoadTga(path),
                ".jpg" or ".jpeg" or ".png" => LoadWithUnity(path),
                _ => throw new NotSupportedException($"Unsupported image format '{ext}': {path}"),
            };
        }

        /// <summary>TGA types used by Amnesia: 2 (true color), 3 (grayscale), 10 (RLE true color), 8/24/32 bit.</summary>
        static DdsDecoder.Image LoadTga(string path)
        {
            byte[] d = File.ReadAllBytes(path);
            int idLength = d[0], colorMapType = d[1], type = d[2];
            int width = BitConverter.ToUInt16(d, 12), height = BitConverter.ToUInt16(d, 14);
            int bpp = d[16];
            bool topOrigin = (d[17] & 0x20) != 0;
            if (colorMapType != 0 || (type != 2 && type != 3 && type != 10 && type != 11))
                throw new NotSupportedException($"TGA type {type} (color map {colorMapType}) not supported: {path}");

            int bytes = bpp / 8;
            int o = 18 + idLength;
            var pixels = new Color32[width * height];

            Color32 Read(int at)
            {
                if (bytes == 1) return new Color32(d[at], d[at], d[at], 255);
                return new Color32(d[at + 2], d[at + 1], d[at], bytes == 4 ? d[at + 3] : (byte)255);
            }

            if (type == 2 || type == 3)
            {
                for (int i = 0; i < pixels.Length; i++, o += bytes) pixels[i] = Read(o);
            }
            else
            {
                int i = 0;
                while (i < pixels.Length)
                {
                    int header = d[o++];
                    int count = (header & 0x7F) + 1;
                    if ((header & 0x80) != 0)
                    {
                        Color32 c = Read(o);
                        o += bytes;
                        for (int k = 0; k < count && i < pixels.Length; k++) pixels[i++] = c;
                    }
                    else
                    {
                        for (int k = 0; k < count && i < pixels.Length; k++, o += bytes) pixels[i++] = Read(o);
                    }
                }
            }

            if (!topOrigin) FlipRows(pixels, width, height);
            return new DdsDecoder.Image { Width = width, Height = height, Faces = new[] { pixels }, Format = $"TGA{type}/{bpp}" };
        }

        static DdsDecoder.Image LoadWithUnity(string path)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!tex.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException($"Cannot decode image: {path}");
                Color32[] pixels = tex.GetPixels32();  // bottom row first
                FlipRows(pixels, tex.width, tex.height);
                return new DdsDecoder.Image { Width = tex.width, Height = tex.height, Faces = new[] { pixels }, Format = Path.GetExtension(path) };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        public static void FlipRows(Color32[] pixels, int width, int height)
        {
            var row = new Color32[width];
            for (int y = 0; y < height / 2; y++)
            {
                int a = y * width, b = (height - 1 - y) * width;
                Array.Copy(pixels, a, row, 0, width);
                Array.Copy(pixels, b, pixels, a, width);
                Array.Copy(row, 0, pixels, b, width);
            }
        }
    }
}
