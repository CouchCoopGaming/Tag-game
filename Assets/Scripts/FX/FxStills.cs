using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Tag.FX
{
    /// <summary>
    /// Labeled mockups of the foot dust and the comic words.
    /// Circles are the puffs. Letters are the same blocks the burst draws.
    /// </summary>
    public static class FxStills
    {
        public static void Write(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            WriteDust(Path.Combine(dir, "dust-surfaces.png"));
            WriteWords(Path.Combine(dir, "comic-words.png"));
        }

        static void WriteDust(string path)
        {
            const int cols = 2;
            const int rows = 6;
            const int cellW = 240;
            const int cellH = 200;
            const int labelW = 150;
            const int kickH = 210;
            int w = labelW + cols * cellW;
            int h = 36 + rows * cellH + kickH;
            var pix = new byte[w * h * 3];
            Fill(pix, 22, 24, 30);
            Text(pix, w, h, 12, 8, "DUST", 236, 228, 210, 2);
            Text(pix, w, h, labelW + 70, 8, "WALK 6.9", 180, 190, 170, 2);
            Text(pix, w, h, labelW + cellW + 50, 8, "SPRINT 13.8", 236, 210, 140, 2);
            string[] names = { "GRASS", "DIRT", "CONCRETE", "WOOD", "METAL", "WET" };
            for (int row = 0; row < rows; row++)
            {
                int y0 = 36 + row * cellH;
                Text(pix, w, h, 12, y0 + 80, names[row], 220, 214, 196, 2);
                for (int col = 0; col < cols; col++)
                {
                    float speed = col == 0 ? DustLook.Walk : DustLook.Sprint;
                    DustLook.Puff puff = DustLook.At(row, speed, (int)DustLook.Kick.None);
                    int ox = labelW + col * cellW;
                    int floor = y0 + cellH - 28;
                    DrawFloor(pix, w, h, ox + 16, ox + cellW - 16, floor);
                    DrawPuff(pix, w, h, ox + cellW / 2, floor, puff);
                }
            }
            int ky = 36 + rows * cellH + 8;
            Text(pix, w, h, 12, ky, "KICKS", 236, 228, 210, 2);
            string[] kicks = { "START", "PIVOT", "SLIDE" };
            int[] kickId = { (int)DustLook.Kick.RunStart, (int)DustLook.Kick.Pivot, (int)DustLook.Kick.Slide };
            int[] surf = { (int)DustLook.Surface.Grass, (int)DustLook.Surface.Metal, (int)DustLook.Surface.Dirt };
            for (int i = 0; i < 3; i++)
            {
                int ox = 16 + i * (w / 3);
                DustLook.Puff puff = DustLook.At(surf[i], DustLook.Sprint, kickId[i]);
                int floor = h - 24;
                DrawFloor(pix, w, h, ox + 8, ox + w / 3 - 24, floor);
                DrawPuff(pix, w, h, ox + w / 6, floor, puff);
                Text(pix, w, h, ox + 8, ky + 28, kicks[i], 200, 196, 180, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void WriteWords(string path)
        {
            const int n = 4;
            const int cellW = 280;
            const int cellH = 300;
            int w = n * cellW;
            int h = cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 16, 28);
            for (int i = 0; i < n; i++)
            {
                int ox = i * cellW;
                ComicWords.ColorOf(i, out float r, out float g, out float b);
                byte rr = (byte)(r * 255f);
                byte gg = (byte)(g * 255f);
                byte bb = (byte)(b * 255f);
                DrawStar(pix, w, h, ox + cellW / 2, 150, 108, rr, gg, bb);
                DrawWord(pix, w, h, ox + cellW / 2, 150, i, 255, 255, 255);
                Text(pix, w, h, ox + 16, 16, ComicWords.Text[i], rr, gg, bb, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void DrawPuff(byte[] pix, int w, int h, int cx, int floor, DustLook.Puff puff)
        {
            int radius = 6 + (int)(puff.Size * 90f);
            if (radius < 3) radius = 3;
            if (radius > 70) radius = 70;
            int count = puff.Count;
            if (count < 1 && puff.Spark == 0 && puff.Opacity < 0.02f) count = 0;
            byte r = (byte)(puff.R * 255f);
            byte g = (byte)(puff.G * 255f);
            byte b = (byte)(puff.B * 255f);
            int specks = count * 3;
            if (specks > 36) specks = 36;
            uint rng = 17u + (uint)(puff.R * 100f) + (uint)count * 13u;
            for (int i = 0; i < specks; i++)
            {
                rng = rng * 1664525u + 1013904223u;
                float u = ((rng >> 8) & 255u) / 255f;
                rng = rng * 1664525u + 1013904223u;
                float v = ((rng >> 8) & 255u) / 255f;
                int x = cx + (int)((u - 0.5f) * radius * 2f);
                int y = floor - 8 - (int)(v * radius * 1.4f);
                int rad = puff.Spark != 0 ? 2 : 3 + (int)(puff.Opacity * 6f);
                FillCircle(pix, w, h, x, y, rad, r, g, b);
            }
        }

        static void DrawStar(byte[] pix, int w, int h, int cx, int cy, int radius, byte r, byte g, byte b)
        {
            FillCircle(pix, w, h, cx, cy, radius / 2, r, g, b);
            for (int i = 0; i < 12; i++)
            {
                double a = i / 12.0 * 6.2831855;
                int x = cx + (int)(Math.Cos(a) * radius);
                int y = cy + (int)(Math.Sin(a) * radius * 0.72);
                FillCircle(pix, w, h, x, y, 10, r, g, b);
            }
        }

        static void DrawWord(byte[] pix, int w, int h, int cx, int cy, int word, byte r, byte g, byte b)
        {
            int letters = ComicWords.LetterCount(word);
            int step = 22;
            int origin = cx - (letters * step) / 2;
            for (int place = 0; place < letters; place++)
            {
                byte[] rows = ComicWords.Glyph(ComicWords.Letter(word, place));
                int ox = origin + place * step;
                for (int y = 0; y < 7; y++)
                {
                    byte row = rows[y];
                    for (int x = 0; x < 5; x++)
                    {
                        if ((row & (1 << (4 - x))) == 0) continue;
                        FillRect(pix, w, h, ox + x * 4 - 1, cy - 20 + y * 6 - 1, 5, 7, 16, 12, 12);
                        FillRect(pix, w, h, ox + x * 4, cy - 20 + y * 6, 3, 5, r, g, b);
                    }
                }
            }
        }

        static void FillCircle(byte[] pix, int w, int h, int cx, int cy, int rad, byte r, byte g, byte b)
        {
            int r2 = rad * rad;
            for (int y = -rad; y <= rad; y++)
            {
                for (int x = -rad; x <= rad; x++)
                {
                    if (x * x + y * y > r2) continue;
                    Plot(pix, w, h, cx + x, cy + y, r, g, b);
                }
            }
        }

        static void FillRect(byte[] pix, int w, int h, int x, int y, int rw, int rh, byte r, byte g, byte b)
        {
            for (int yy = 0; yy < rh; yy++)
            {
                for (int xx = 0; xx < rw; xx++)
                    Plot(pix, w, h, x + xx, y + yy, r, g, b);
            }
        }

        static void DrawFloor(byte[] pix, int w, int h, int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++)
                Plot(pix, w, h, x, y, 210, 196, 150);
        }

        static void Plot(byte[] pix, int w, int h, int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = (y * w + x) * 3;
            pix[i] = r;
            pix[i + 1] = g;
            pix[i + 2] = b;
        }

        static void Fill(byte[] pix, byte r, byte g, byte b)
        {
            for (int i = 0; i < pix.Length; i += 3)
            {
                pix[i] = r;
                pix[i + 1] = g;
                pix[i + 2] = b;
            }
        }

        static readonly string Font =
            "01110100011111110001100011111010001111101000111110011111000010000100000111111110100011000110001111101111110000111101000011111" +
            "11111100001111010000100000111010000101111000101110100011000111111100011000111111001000010000100111111000110010111001001010001" +
            "10000100001000010000111111000111011101011000110001100011100110101100111000101110100011000110001011101111010001111101000010000" +
            "11110100011111010010100010111110000011100000111110111110010000100001000010010001100011000110001011101000110001100010101000100" +
            "1000110001101011101110001";

        static void Text(byte[] pix, int w, int h, int x, int y, string text, byte r, byte g, byte b, int scale)
        {
            if (string.IsNullOrEmpty(text)) return;
            int pen = x;
            for (int i = 0; i < text.Length; i++)
            {
                StampChar(pix, w, h, pen, y, text[i], r, g, b, scale);
                pen += 6 * scale;
            }
        }

        static void StampChar(byte[] pix, int w, int h, int x, int y, char c, byte r, byte g, byte b, int scale)
        {
            int glyph = GlyphIndex(c);
            if (glyph < 0) return;
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    int bit = Font[glyph * 25 + row * 5 + col];
                    if (bit != '1') continue;
                    FillRect(pix, w, h, x + col * scale, y + row * scale, scale, scale, r, g, b);
                }
            }
        }

        static int GlyphIndex(char c)
        {
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);
            const string order = "ABCDEFGHIKLMNOPRSTUVW";
            int at = order.IndexOf(c);
            if (at >= 0) return at;
            return -1;
        }

        static void WritePng(string path, byte[] rgb, int w, int h)
        {
            int stride = w * 3;
            var raw = new byte[(stride + 1) * h];
            for (int y = 0; y < h; y++)
            {
                raw[y * (stride + 1)] = 0;
                Buffer.BlockCopy(rgb, y * stride, raw, y * (stride + 1) + 1, stride);
            }
            byte[] deflated;
            using (var ms = new MemoryStream())
            {
                using (var def = new DeflateStream(ms, CompressionLevel.Fastest, true))
                    def.Write(raw, 0, raw.Length);
                deflated = ms.ToArray();
            }
            uint a = 1;
            uint bsum = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                a = (a + raw[i]) % 65521;
                bsum = (bsum + a) % 65521;
            }
            uint adler = (bsum << 16) | a;
            var zlib = new byte[deflated.Length + 6];
            zlib[0] = 0x78;
            zlib[1] = 0x01;
            Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
            zlib[zlib.Length - 4] = (byte)(adler >> 24);
            zlib[zlib.Length - 3] = (byte)(adler >> 16);
            zlib[zlib.Length - 2] = (byte)(adler >> 8);
            zlib[zlib.Length - 1] = (byte)adler;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
                fs.Write(sig, 0, sig.Length);
                var ihdr = new byte[13];
                Be(ihdr, 0, w);
                Be(ihdr, 4, h);
                ihdr[8] = 8;
                ihdr[9] = 2;
                Chunk(fs, "IHDR", ihdr);
                Chunk(fs, "IDAT", zlib);
                Chunk(fs, "IEND", new byte[0]);
            }
        }

        static void Be(byte[] buf, int at, int value)
        {
            uint u = (uint)value;
            buf[at] = (byte)(u >> 24);
            buf[at + 1] = (byte)(u >> 16);
            buf[at + 2] = (byte)(u >> 8);
            buf[at + 3] = (byte)u;
        }

        static void Be(byte[] buf, int at, uint value)
        {
            buf[at] = (byte)(value >> 24);
            buf[at + 1] = (byte)(value >> 16);
            buf[at + 2] = (byte)(value >> 8);
            buf[at + 3] = (byte)value;
        }

        static void Chunk(Stream fs, string name, byte[] data)
        {
            var len = new byte[4];
            Be(len, 0, data.Length);
            fs.Write(len, 0, 4);
            byte[] tag = Encoding.ASCII.GetBytes(name);
            fs.Write(tag, 0, 4);
            if (data.Length > 0) fs.Write(data, 0, data.Length);
            uint crc = 0xffffffff;
            for (int i = 0; i < tag.Length; i++) crc = Crc(crc, tag[i]);
            for (int i = 0; i < data.Length; i++) crc = Crc(crc, data[i]);
            crc ^= 0xffffffff;
            var c = new byte[4];
            Be(c, 0, crc);
            fs.Write(c, 0, 4);
        }

        static uint Crc(uint crc, byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
            return crc;
        }
    }
}
