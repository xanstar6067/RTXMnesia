using System;
using System.IO;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Duration of an Ogg Vorbis file from its headers: the last page's granule position (samples) divided by the
    /// sample rate of the Vorbis identification header. No decoding.
    /// </summary>
    public static class OggInfo
    {
        static readonly byte[] Capture = { (byte)'O', (byte)'g', (byte)'g', (byte)'S' };

        public static bool TryGetDuration(string file, out float seconds)
        {
            seconds = 0f;
            using var stream = File.OpenRead(file);
            if (stream.Length < 64) return false;

            // First page: 27 byte header, segment table, then the identification packet
            // (1 type byte, "vorbis", 4 bytes version, 1 byte channels, 4 bytes sample rate).
            var head = new byte[Math.Min(512, (int)stream.Length)];
            stream.Read(head, 0, head.Length);
            if (!Matches(head, 0)) return false;
            int segments = head[26];
            int packet = 27 + segments;
            if (packet + 16 > head.Length || head[packet] != 1 || head[packet + 1] != (byte)'v') return false;
            uint sampleRate = BitConverter.ToUInt32(head, packet + 12);
            if (sampleRate == 0) return false;

            // Last page: search backwards for the capture pattern.
            int tail = (int)Math.Min(stream.Length, 65536);
            var end = new byte[tail];
            stream.Seek(-tail, SeekOrigin.End);
            stream.Read(end, 0, tail);
            for (int i = tail - 27; i >= 0; i--)
            {
                if (!Matches(end, i)) continue;
                long granule = BitConverter.ToInt64(end, i + 6);
                if (granule <= 0) continue;
                seconds = (float)((double)granule / sampleRate);
                return true;
            }
            return false;
        }

        static bool Matches(byte[] data, int at)
        {
            for (int k = 0; k < 4; k++)
                if (data[at + k] != Capture[k]) return false;
            return true;
        }
    }
}
