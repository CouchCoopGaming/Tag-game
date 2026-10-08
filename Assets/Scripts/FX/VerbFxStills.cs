using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Tag.FX
{
    /// <summary>
    /// Pass 4 mockups. Comic before is the old dotted burst. After is the Bangers star.
    /// Unity is not required. These are labeled drawings, not in-engine captures.
    /// </summary>
    public static class VerbFxStills
    {
        public static void Write(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            // Comic before / after / park are the baked Bangers sheets from Tools/BuildComicAtlas.py.
            WriteVerbs(Path.Combine(dir, "verb-fx.png"));
        }

        static void WriteBefore(string path)
        {
            const int n = 4;
            const int cellW = 280;
            const int cellH = 300;
            int w = n * cellW;
            int h = cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 16, 28);
            Text(pix, w, h, 12, 8, "BEFORE", 220, 210, 190, 2);
            for (int i = 0; i < n; i++)
            {
                int ox = i * cellW;
                ComicWords.ColorOf(i, out float r, out float g, out float b);
                byte rr = (byte)(r * 255f);
                byte gg = (byte)(g * 255f);
                byte bb = (byte)(b * 255f);
                DrawOldStar(pix, w, h, ox + cellW / 2, 160, 100, rr, gg, bb);
                DrawOldWord(pix, w, h, ox + cellW / 2, 160, i);
                Text(pix, w, h, ox + 16, 268, ComicWords.Text[i], rr, gg, bb, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void WriteAfter(string path)
        {
            const int n = 4;
            const int cellW = 320;
            const int cellH = 340;
            int w = n * cellW;
            int h = cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 22, 18, 32);
            Text(pix, w, h, 12, 8, "AFTER", 255, 236, 160, 2);
            int aw = 0;
            int ah = 0;
            byte[] rgba = null;
            ComicArt.DecodePng(ComicAtlas.Png(), out aw, out ah, out rgba);
            for (int i = 0; i < n; i++)
            {
                int ox = i * cellW;
                int cx = ox + cellW / 2;
                int cy = 168;
                ComicWords.ColorOf(i, out float r, out float g, out float b);
                byte rr = (byte)(r * 255f);
                byte gg = (byte)(g * 255f);
                byte bb = (byte)(b * 255f);
                int spikes = ComicArt.SpikesFor(i);
                DrawStar(pix, w, h, cx, cy, spikes, 118f, ComicArt.OutlineScale, 12, 10, 10, false);
                DrawStar(pix, w, h, cx, cy, spikes, 118f, 1f, rr, gg, bb, true);
                ComicArt.InnerColor(i, out float ir, out float ig, out float ib);
                DrawStar(pix, w, h, cx, cy, spikes, 118f, ComicArt.InnerScale, (byte)(ir * 255f), (byte)(ig * 255f), (byte)(ib * 255f), false);
                if (rgba != null)
                    BlitCell(pix, w, h, rgba, aw, ah, i, cx, cy + 6, 250, 108);
                Text(pix, w, h, ox + 16, 308, ComicWords.Text[i], rr, gg, bb, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void WriteVerbs(string path)
        {
            const int cols = 3;
            const int rows = 3;
            const int cellW = 280;
            const int cellH = 220;
            int w = cols * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 20, 22, 28);
            string[] names = { "LAND", "DASH", "ROPE", "PAD", "ZIP", "DIZZY", "RIM", "WALL", "WISP" };
            for (int i = 0; i < 9; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int x = col * cellW;
                int y = row * cellH;
                Text(pix, w, h, x + 12, y + 10, names[i], 230, 220, 190, 2);
                int cx = x + cellW / 2;
                int cy = y + 120;
                if (i == 0) DrawLand(pix, w, h, cx, cy);
                else if (i == 1) DrawDash(pix, w, h, cx, cy);
                else if (i == 2) DrawRope(pix, w, h, x + 30, cy, x + cellW - 30, cy - 10);
                else if (i == 3) DrawPad(pix, w, h, cx, cy);
                else if (i == 4) DrawZip(pix, w, h, x + 24, cy, x + cellW - 24);
                else if (i == 5) DrawDizzy(pix, w, h, cx, cy);
                else if (i == 6) DrawRim(pix, w, h, cx, cy);
                else if (i == 7) DrawWall(pix, w, h, x + 40, y + 40, x + 40, y + cellH - 30);
                else DrawWisp(pix, w, h, cx, cy);
            }
            WritePng(path, pix, w, h);
        }

        static void DrawLand(byte[] pix, int w, int h, int cx, int cy)
        {
            int rad = (int)(VerbFxLook.LandRing(36.5f) * 36f);
            Ring(pix, w, h, cx, cy + 20, rad, 210, 180, 120);
            int bits = VerbFxLook.Debris(36.5f, 1f);
            if (bits > 8) bits = 8;
            for (int i = 0; i < bits; i++)
            {
                double a = i / (double)bits * 6.283;
                int x = cx + (int)(Math.Cos(a) * (rad * 0.45));
                int y = cy + 10 - (int)(Math.Sin(a) * 18);
                FillCircle(pix, w, h, x, y, 4, 180, 140, 80);
            }
        }

        static void DrawDash(byte[] pix, int w, int h, int cx, int cy)
        {
            int n = VerbFxLook.Ghosts(1f);
            for (int i = 0; i < n; i++)
            {
                byte a = (byte)(80 + i * 40);
                FillRect(pix, w, h, cx - 70 + i * 28, cy - 36, 22, 64, a, (byte)(40 + i * 10), (byte)(40 + i * 8));
            }
        }

        static void DrawRope(byte[] pix, int w, int h, int x0, int y0, int x1, int y1)
        {
            int steps = 16;
            int prevX = x0;
            int prevY = y0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                VerbFxLook.RopeOffset(t, 0.8f, 0.6f, 0.4f, out float lateral, out float drop);
                int x = x0 + (int)((x1 - x0) * t);
                int y = y0 + (int)((y1 - y0) * t) + (int)(drop * 80f) + (int)(lateral * 40f);
                Line(pix, w, h, prevX, prevY, x, y, 240, 200, 60);
                prevX = x;
                prevY = y;
            }
            FillCircle(pix, w, h, x1, y1, 5, 255, 220, 80);
        }

        static void DrawPad(byte[] pix, int w, int h, int cx, int cy)
        {
            Ring(pix, w, h, cx, cy + 24, (int)(VerbFxLook.PadRing * 28f), 80, 190, 255);
            Line(pix, w, h, cx, cy + 20, cx, cy - 50, 180, 230, 255);
        }

        static void DrawZip(byte[] pix, int w, int h, int x0, int y, int x1)
        {
            Line(pix, w, h, x0, y, x1, y - 16, 200, 80, 230);
            int sparks = VerbFxLook.ZipSparks(VerbFxLook.ZipPace, 1f);
            if (sparks > 6) sparks = 6;
            for (int i = 0; i < sparks; i++)
            {
                int x = x0 + 20 + i * 28;
                FillCircle(pix, w, h, x, y - 8 - (i % 2) * 6, 3, 255, 220, 120);
            }
        }

        static void DrawDizzy(byte[] pix, int w, int h, int cx, int cy)
        {
            for (int i = 0; i < 3; i++)
            {
                double a = i / 3.0 * 6.283;
                int x = cx + (int)(Math.Cos(a) * 28);
                int y = cy + (int)(Math.Sin(a) * 16);
                DrawStar(pix, w, h, x, y, 10, 16f, 1f, 255, (byte)(180 - i * 40), 40, false);
            }
        }

        static void DrawRim(byte[] pix, int w, int h, int cx, int cy)
        {
            Ring(pix, w, h, cx, cy + 16, 34, 240, 70, 80);
            Ring(pix, w, h, cx, cy - 20, 30, 240, 70, 80);
        }

        static void DrawWall(byte[] pix, int w, int h, int x, int y0, int x1, int y1)
        {
            Line(pix, w, h, x, y0, x1, y1, 90, 90, 96);
            Line(pix, w, h, x + 6, y0 + 30, x + 6, y0 + 90, 190, 180, 160);
            for (int i = 0; i < 4; i++)
                FillCircle(pix, w, h, x + 10, y0 + 40 + i * 16, 3, 40, 70, 120);
        }

        static void DrawWisp(byte[] pix, int w, int h, int cx, int cy)
        {
            for (int i = 0; i < VerbFxLook.Wisps(14f, 1f); i++)
                FillCircle(pix, w, h, cx + 30 - i * 16, cy, 6 - i, 220, 230, 255);
        }

        static void DrawOldStar(byte[] pix, int w, int h, int cx, int cy, int radius, byte r, byte g, byte b)
        {
            FillCircle(pix, w, h, cx, cy, radius / 2, r, g, b);
            for (int i = 0; i < 12; i++)
            {
                double a = i / 12.0 * 6.2831855;
                int x = cx + (int)(Math.Cos(a) * radius);
                int y = cy + (int)(Math.Sin(a) * radius * 0.72);
                FillCircle(pix, w, h, x, y, 8, r, g, b);
            }
        }

        static void DrawOldWord(byte[] pix, int w, int h, int cx, int cy, int word)
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
                        FillRect(pix, w, h, ox + x * 4, cy - 20 + y * 6, 3, 5, 255, 255, 255);
                    }
                }
            }
        }

        static void DrawStar(byte[] pix, int w, int h, int cx, int cy, int spikes, float radius, float scale, byte r, byte g, byte b, bool dots)
        {
            for (int i = 0; i < spikes; i++)
            {
                ComicArt.Point(i, spikes, false, out float vx, out float vy);
                ComicArt.Point(i, spikes, true, out float tx, out float ty);
                int next = (i + 1) % spikes;
                ComicArt.Point(next, spikes, false, out float nx, out float ny);
                int x1 = cx + (int)(vx * radius * scale);
                int y1 = cy - (int)(vy * radius * scale);
                int x2 = cx + (int)(tx * radius * scale);
                int y2 = cy - (int)(ty * radius * scale);
                int x3 = cx + (int)(nx * radius * scale);
                int y3 = cy - (int)(ny * radius * scale);
                Tri(pix, w, h, cx, cy, x1, y1, x2, y2, r, g, b, dots);
                Tri(pix, w, h, cx, cy, x2, y2, x3, y3, r, g, b, dots);
            }
        }

        static void Tri(byte[] pix, int w, int h, int x0, int y0, int x1, int y1, int x2, int y2, byte r, byte g, byte b, bool dots)
        {
            int minX = x0;
            if (x1 < minX) minX = x1;
            if (x2 < minX) minX = x2;
            int maxX = x0;
            if (x1 > maxX) maxX = x1;
            if (x2 > maxX) maxX = x2;
            int minY = y0;
            if (y1 < minY) minY = y1;
            if (y2 < minY) minY = y2;
            int maxY = y0;
            if (y1 > maxY) maxY = y1;
            if (y2 > maxY) maxY = y2;
            int area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
            if (area == 0) return;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int w0 = (x1 - x0) * (y - y0) - (x - x0) * (y1 - y0);
                    int w1 = (x2 - x1) * (y - y1) - (x - x1) * (y2 - y1);
                    int w2 = (x0 - x2) * (y - y2) - (x - x2) * (y0 - y2);
                    if (area > 0)
                    {
                        if (w0 < 0 || w1 < 0 || w2 < 0) continue;
                    }
                    else if (w0 > 0 || w1 > 0 || w2 > 0)
                        continue;
                    byte rr = r;
                    byte gg = g;
                    byte bb = b;
                    if (dots && ((x + y) & 3) == 0)
                    {
                        rr = (byte)(r * 0.62f);
                        gg = (byte)(g * 0.62f);
                        bb = (byte)(b * 0.62f);
                    }
                    Plot(pix, w, h, x, y, rr, gg, bb);
                }
            }
        }

        static void BlitCell(byte[] pix, int w, int h, byte[] rgba, int aw, int ah, int cell, int cx, int cy, int dw, int dh)
        {
            int srcX = cell * ComicAtlas.CellWidth;
            for (int y = 0; y < dh; y++)
            {
                int sy = y * ah / dh;
                if (sy < 0 || sy >= ah) continue;
                for (int x = 0; x < dw; x++)
                {
                    int sx = srcX + x * ComicAtlas.CellWidth / dw;
                    if (sx < 0 || sx >= aw) continue;
                    int i = (sy * aw + sx) * 4;
                    int a = rgba[i + 3];
                    if (a < 16) continue;
                    int dx = cx - dw / 2 + x;
                    int dy = cy - dh / 2 + y;
                    byte rr = rgba[i];
                    byte gg = rgba[i + 1];
                    byte bb = rgba[i + 2];
                    if (a > 240)
                        Plot(pix, w, h, dx, dy, rr, gg, bb);
                    else
                        PlotMix(pix, w, h, dx, dy, rr, gg, bb, a);
                }
            }
        }

        static void Ring(byte[] pix, int w, int h, int cx, int cy, int rad, byte r, byte g, byte b)
        {
            int steps = 48;
            int prevX = cx + rad;
            int prevY = cy;
            for (int i = 1; i <= steps; i++)
            {
                double a = i / (double)steps * 6.2831855;
                int x = cx + (int)(Math.Cos(a) * rad);
                int y = cy + (int)(Math.Sin(a) * rad * 0.42);
                Line(pix, w, h, prevX, prevY, x, y, r, g, b);
                prevX = x;
                prevY = y;
            }
        }

        static void Line(byte[] pix, int w, int h, int x0, int y0, int x1, int y1, byte r, byte g, byte b)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int steps = dx + dy + 1;
            for (int i = 0; i < steps; i++)
            {
                Plot(pix, w, h, x0, y0, r, g, b);
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
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

        static void PlotMix(byte[] pix, int w, int h, int x, int y, byte r, byte g, byte b, int a)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = (y * w + x) * 3;
            int inv = 255 - a;
            pix[i] = (byte)((r * a + pix[i] * inv) / 255);
            pix[i + 1] = (byte)((g * a + pix[i + 1] * inv) / 255);
            pix[i + 2] = (byte)((b * a + pix[i + 2] * inv) / 255);
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
            return order.IndexOf(c);
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
