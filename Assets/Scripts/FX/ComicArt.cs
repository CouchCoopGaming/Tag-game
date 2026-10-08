using System;
using System.IO;
using System.IO.Compression;

namespace Tag.FX
{
    /// <summary>
    /// Classic comic burst: uneven spikes, a thick outline, an inner burst, halftone.
    /// The letters are the baked Bangers atlas. Tilt stays within 12 degrees.
    /// </summary>
    public static class ComicArt
    {
        public const int SpikeMin = 10;
        public const int SpikeMax = 14;
        public const float OutlineScale = 1.18f;
        public const float InnerScale = 0.46f;

        static readonly float[] Tips =
        {
            1.16f, 0.82f, 1.24f, 0.76f, 1.08f, 0.90f, 1.20f, 0.78f,
            1.04f, 0.86f, 1.14f, 0.74f, 1.22f, 0.88f
        };

        static readonly float[] Valleys =
        {
            0.46f, 0.58f, 0.40f, 0.56f, 0.44f, 0.60f, 0.42f, 0.52f,
            0.48f, 0.57f, 0.41f, 0.54f, 0.45f, 0.59f
        };

        public static int SpikesFor(int word)
        {
            if (word < 0) word = 0;
            int n = 10 + (word % 4);
            if (n > SpikeMax) n = SpikeMax;
            return n;
        }

        public static void Point(int spike, int count, bool tip, out float x, out float y)
        {
            if (count < 3) count = 3;
            float u = tip ? spike + 0.5f : spike;
            float ang = u / count * 6.2831855f - 1.5707963f;
            int i = spike % Tips.Length;
            if (i < 0) i = 0;
            float rad = tip ? Tips[i] : Valleys[i];
            x = (float)Math.Cos(ang) * rad;
            y = (float)Math.Sin(ang) * rad;
        }

        public static void InnerColor(int word, out float r, out float g, out float b)
        {
            if (word == ComicWords.Wham)
            {
                r = 1f;
                g = 0.92f;
                b = 0.42f;
                return;
            }
            r = 1f;
            g = 0.98f;
            b = 0.88f;
        }

        public static bool DecodePng(byte[] png, out int width, out int height, out byte[] rgba)
        {
            width = 0;
            height = 0;
            rgba = null;
            if (png == null || png.Length < 8) return false;
            int i = 8;
            int w = 0;
            int h = 0;
            int color = -1;
            var idat = new MemoryStream();
            while (i + 8 <= png.Length)
            {
                int len = Be(png, i);
                i += 4;
                if (i + 4 + len + 4 > png.Length) return false;
                string kind = "" + (char)png[i] + (char)png[i + 1] + (char)png[i + 2] + (char)png[i + 3];
                i += 4;
                if (kind == "IHDR")
                {
                    w = Be(png, i);
                    h = Be(png, i + 4);
                    color = png[i + 9];
                    if (png[i + 8] != 8 || png[i + 10] != 0) return false;
                }
                else if (kind == "IDAT")
                    idat.Write(png, i, len);
                else if (kind == "IEND")
                    break;
                i += len + 4;
            }
            if (w <= 0 || h <= 0 || (color != 6 && color != 2)) return false;
            byte[] zlib = idat.ToArray();
            if (zlib.Length < 8) return false;
            byte[] raw;
            using (var ms = new MemoryStream(zlib, 2, zlib.Length - 6))
            using (var def = new DeflateStream(ms, CompressionMode.Decompress))
            using (var outMs = new MemoryStream())
            {
                def.CopyTo(outMs);
                raw = outMs.ToArray();
            }
            int channels = color == 6 ? 4 : 3;
            int stride = w * channels;
            if (raw.Length < (stride + 1) * h) return false;
            rgba = new byte[w * h * 4];
            var prev = new byte[stride];
            var row = new byte[stride];
            for (int y = 0; y < h; y++)
            {
                int filter = raw[y * (stride + 1)];
                int src = y * (stride + 1) + 1;
                for (int x = 0; x < stride; x++)
                {
                    int v = raw[src + x];
                    int a = x >= channels ? row[x - channels] : 0;
                    int b = prev[x];
                    int c = x >= channels ? prev[x - channels] : 0;
                    if (filter == 1) v += a;
                    else if (filter == 2) v += b;
                    else if (filter == 3) v += (a + b) / 2;
                    else if (filter == 4) v += Paeth(a, b, c);
                    v &= 255;
                    row[x] = (byte)v;
                }
                Buffer.BlockCopy(row, 0, prev, 0, stride);
                int dst = y * w * 4;
                for (int x = 0; x < w; x++)
                {
                    rgba[dst] = row[x * channels];
                    rgba[dst + 1] = row[x * channels + 1];
                    rgba[dst + 2] = row[x * channels + 2];
                    rgba[dst + 3] = channels == 4 ? row[x * channels + 3] : (byte)255;
                    dst += 4;
                }
            }
            width = w;
            height = h;
            return true;
        }

        public static bool Holds()
        {
            for (int word = 0; word < ComicWords.Count; word++)
            {
                int n = SpikesFor(word);
                if (n < SpikeMin || n > SpikeMax) return false;
            }
            if (OutlineScale < 1.08f || InnerScale >= 0.7f) return false;
            float min = 9f;
            float max = 0f;
            for (int i = 0; i < 12; i++)
            {
                Point(i, 12, true, out float x, out float y);
                float r = (float)Math.Sqrt(x * x + y * y);
                if (r < min) min = r;
                if (r > max) max = r;
            }
            if (max - min < 0.25f) return false;
            if (!DecodePng(ComicAtlas.Png(), out int w, out int h, out byte[] rgba)) return false;
            if (w != ComicAtlas.Cells * ComicAtlas.CellWidth || h != ComicAtlas.CellHeight) return false;
            int ink = 0;
            for (int i = 3; i < rgba.Length; i += 16)
            {
                if (rgba[i] > 200) ink++;
            }
            if (ink < 400) return false;
            return true;
        }

        static int Paeth(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = p - a;
            if (pa < 0) pa = -pa;
            int pb = p - b;
            if (pb < 0) pb = -pb;
            int pc = p - c;
            if (pc < 0) pc = -pc;
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        static int Be(byte[] buf, int at)
        {
            return (buf[at] << 24) | (buf[at + 1] << 16) | (buf[at + 2] << 8) | buf[at + 3];
        }
    }
}
