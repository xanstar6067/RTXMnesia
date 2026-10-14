using System;
using System.IO;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Decodes the top mip level of the DDS variants used by Amnesia: DXT1, DXT3, DXT5, ATI2 (BC5),
    /// uncompressed 24/32-bit, and cube maps. Pixels are returned top row first, as stored in the file.
    /// </summary>
    public static class DdsDecoder
    {
        public sealed class Image
        {
            public int Width, Height;
            /// <summary>One array per face (1 for 2D, 6 for cube: +X,-X,+Y,-Y,+Z,-Z), top row first.</summary>
            public Color32[][] Faces;
            public string Format;
            public bool IsCube => Faces.Length == 6;
        }

        const uint DDPF_ALPHAPIXELS = 0x1, DDPF_FOURCC = 0x4, DDPF_RGB = 0x40, DDPF_LUMINANCE = 0x20000;
        const uint DDSCAPS2_CUBEMAP = 0x200;

        public static Image Load(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data.Length < 128 || data[0] != 'D' || data[1] != 'D' || data[2] != 'S' || data[3] != ' ')
                throw new InvalidDataException($"Not a DDS file: {path}");

            int height = (int)U32(data, 12);
            int width = (int)U32(data, 16);
            uint mipCount = Math.Max(1u, U32(data, 28));
            uint pfFlags = U32(data, 80);
            string fourCC = System.Text.Encoding.ASCII.GetString(data, 84, 4);
            int bitCount = (int)U32(data, 88);
            uint rMask = U32(data, 92), gMask = U32(data, 96), bMask = U32(data, 100), aMask = U32(data, 104);
            bool cube = (U32(data, 112) & DDSCAPS2_CUBEMAP) != 0;

            Func<byte[], int, int, int, Color32[]> decode;
            Func<int, int, int> levelSize;
            string format;

            if ((pfFlags & DDPF_FOURCC) != 0)
            {
                format = fourCC;
                switch (fourCC)
                {
                    case "DXT1": decode = DecodeDxt1; levelSize = (w, h) => Blocks(w) * Blocks(h) * 8; break;
                    case "DXT3": decode = DecodeDxt3; levelSize = (w, h) => Blocks(w) * Blocks(h) * 16; break;
                    case "DXT5": decode = DecodeDxt5; levelSize = (w, h) => Blocks(w) * Blocks(h) * 16; break;
                    case "ATI2":
                    case "BC5U": decode = DecodeBc5; levelSize = (w, h) => Blocks(w) * Blocks(h) * 16; break;
                    default: throw new NotSupportedException($"DDS FourCC '{fourCC}' not supported: {path}");
                }
            }
            else if ((pfFlags & (DDPF_RGB | DDPF_LUMINANCE)) != 0 && (bitCount == 8 || bitCount == 16 || bitCount == 24 || bitCount == 32))
            {
                bool hasAlpha = (pfFlags & DDPF_ALPHAPIXELS) != 0 && aMask != 0;
                format = $"RGB{bitCount}";
                int bytes = bitCount / 8;
                decode = (d, o, w, h) => DecodeMasked(d, o, w, h, bytes, rMask, gMask, bMask, hasAlpha ? aMask : 0,
                    (pfFlags & DDPF_LUMINANCE) != 0);
                levelSize = (w, h) => w * h * bytes;
            }
            else throw new NotSupportedException($"DDS pixel format not supported (flags {pfFlags:X}): {path}");

            int faceCount = cube ? 6 : 1;
            var faces = new Color32[faceCount][];
            int offset = 128;
            for (int f = 0; f < faceCount; f++)
            {
                faces[f] = decode(data, offset, width, height);
                // Skip the whole mip chain of this face.
                int w = width, h = height;
                for (int m = 0; m < mipCount; m++)
                {
                    offset += levelSize(w, h);
                    w = Math.Max(1, w / 2);
                    h = Math.Max(1, h / 2);
                }
            }

            return new Image { Width = width, Height = height, Faces = faces, Format = format };
        }

        static int Blocks(int size) => Math.Max(1, (size + 3) / 4);
        static uint U32(byte[] d, int o) => BitConverter.ToUInt32(d, o);
        static ushort U16(byte[] d, int o) => BitConverter.ToUInt16(d, o);

        static Color32 From565(ushort c) => new(
            (byte)(((c >> 11) & 31) * 255 / 31),
            (byte)(((c >> 5) & 63) * 255 / 63),
            (byte)((c & 31) * 255 / 31), 255);

        static void ColorBlock(byte[] d, int o, Color32[] palette, bool dxt1)
        {
            ushort c0 = U16(d, o), c1 = U16(d, o + 2);
            palette[0] = From565(c0);
            palette[1] = From565(c1);
            if (!dxt1 || c0 > c1)
            {
                palette[2] = Lerp(palette[0], palette[1], 1, 3);
                palette[3] = Lerp(palette[0], palette[1], 2, 3);
            }
            else
            {
                palette[2] = Lerp(palette[0], palette[1], 1, 2);
                palette[3] = new Color32(0, 0, 0, 0);
            }
        }

        static Color32 Lerp(Color32 a, Color32 b, int num, int den) => new(
            (byte)((a.r * (den - num) + b.r * num) / den),
            (byte)((a.g * (den - num) + b.g * num) / den),
            (byte)((a.b * (den - num) + b.b * num) / den), 255);

        /// <summary>Interpolated 8-bit channel block (DXT5 alpha / BC4 / BC5 halves). Writes 16 values.</summary>
        static void ChannelBlock(byte[] d, int o, byte[] outValues)
        {
            int a0 = d[o], a1 = d[o + 1];
            Span<int> p = stackalloc int[8];
            p[0] = a0; p[1] = a1;
            if (a0 > a1) for (int i = 1; i < 7; i++) p[i + 1] = (a0 * (7 - i) + a1 * i) / 7;
            else
            {
                for (int i = 1; i < 5; i++) p[i + 1] = (a0 * (5 - i) + a1 * i) / 5;
                p[6] = 0; p[7] = 255;
            }
            ulong bits = 0;
            for (int i = 0; i < 6; i++) bits |= (ulong)d[o + 2 + i] << (8 * i);
            for (int i = 0; i < 16; i++) outValues[i] = (byte)p[(int)((bits >> (3 * i)) & 7)];
        }

        delegate void BlockWriter(byte[] d, int blockOffset, Color32[] dst, int x0, int y0, int w, int h);

        static Color32[] DecodeBlocks(byte[] d, int o, int w, int h, int blockBytes, BlockWriter writer)
        {
            var dst = new Color32[w * h];
            for (int by = 0; by < Blocks(h); by++)
                for (int bx = 0; bx < Blocks(w); bx++)
                {
                    writer(d, o, dst, bx * 4, by * 4, w, h);
                    o += blockBytes;
                }
            return dst;
        }

        static void Put(Color32[] dst, int x, int y, int w, int h, Color32 c)
        {
            if (x < w && y < h) dst[y * w + x] = c;
        }

        static Color32[] DecodeDxt1(byte[] d, int o, int w, int h)
        {
            var pal = new Color32[4];
            return DecodeBlocks(d, o, w, h, 8, (data, b, dst, x0, y0, W, H) =>
            {
                ColorBlock(data, b, pal, true);
                uint idx = U32(data, b + 4);
                for (int i = 0; i < 16; i++) Put(dst, x0 + i % 4, y0 + i / 4, W, H, pal[(idx >> (2 * i)) & 3]);
            });
        }

        static Color32[] DecodeDxt3(byte[] d, int o, int w, int h)
        {
            var pal = new Color32[4];
            return DecodeBlocks(d, o, w, h, 16, (data, b, dst, x0, y0, W, H) =>
            {
                ColorBlock(data, b + 8, pal, false);
                uint idx = U32(data, b + 12);
                ulong alpha = BitConverter.ToUInt64(data, b);
                for (int i = 0; i < 16; i++)
                {
                    Color32 c = pal[(idx >> (2 * i)) & 3];
                    c.a = (byte)(((alpha >> (4 * i)) & 15) * 17);
                    Put(dst, x0 + i % 4, y0 + i / 4, W, H, c);
                }
            });
        }

        static Color32[] DecodeDxt5(byte[] d, int o, int w, int h)
        {
            var pal = new Color32[4];
            var alpha = new byte[16];
            return DecodeBlocks(d, o, w, h, 16, (data, b, dst, x0, y0, W, H) =>
            {
                ChannelBlock(data, b, alpha);
                ColorBlock(data, b + 8, pal, false);
                uint idx = U32(data, b + 12);
                for (int i = 0; i < 16; i++)
                {
                    Color32 c = pal[(idx >> (2 * i)) & 3];
                    c.a = alpha[i];
                    Put(dst, x0 + i % 4, y0 + i / 4, W, H, c);
                }
            });
        }

        /// <summary>BC5: red + green, blue reconstructed as the normal's Z.</summary>
        static Color32[] DecodeBc5(byte[] d, int o, int w, int h)
        {
            var red = new byte[16];
            var green = new byte[16];
            return DecodeBlocks(d, o, w, h, 16, (data, b, dst, x0, y0, W, H) =>
            {
                ChannelBlock(data, b, red);
                ChannelBlock(data, b + 8, green);
                for (int i = 0; i < 16; i++)
                {
                    float x = red[i] / 127.5f - 1f, y = green[i] / 127.5f - 1f;
                    float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                    Put(dst, x0 + i % 4, y0 + i / 4, W, H, new Color32(red[i], green[i], (byte)((z * 0.5f + 0.5f) * 255f), 255));
                }
            });
        }

        static Color32[] DecodeMasked(byte[] d, int o, int w, int h, int bytes, uint rMask, uint gMask, uint bMask,
            uint aMask, bool luminance)
        {
            var dst = new Color32[w * h];
            for (int i = 0; i < w * h; i++, o += bytes)
            {
                uint v = 0;
                for (int k = 0; k < bytes; k++) v |= (uint)d[o + k] << (8 * k);
                byte r = Channel(v, rMask);
                dst[i] = luminance
                    ? new Color32(r, r, r, aMask != 0 ? Channel(v, aMask) : (byte)255)
                    : new Color32(r, Channel(v, gMask), Channel(v, bMask), aMask != 0 ? Channel(v, aMask) : (byte)255);
            }
            return dst;
        }

        static byte Channel(uint value, uint mask)
        {
            if (mask == 0) return 0;
            int shift = 0;
            while (((mask >> shift) & 1) == 0) shift++;
            uint max = mask >> shift;
            return (byte)(((value & mask) >> shift) * 255 / max);
        }
    }
}
