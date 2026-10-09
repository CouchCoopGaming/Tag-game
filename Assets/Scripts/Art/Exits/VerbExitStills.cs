using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Labeled stick-figure strips for the roll, the absorb, and the landings.
    /// The floor line is the ground. Nothing in the after row crosses it.
    /// </summary>
    public static class VerbExitStills
    {
        public static void Write(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            WriteRoll(Path.Combine(dir, "roll.png"));
            WriteAbsorbLands(Path.Combine(dir, "absorb-lands.png"));
            WriteSides(Path.Combine(dir, "vault-wall.png"));
        }

        static void WriteRoll(string path)
        {
            const int frames = 8;
            const int cellW = 210;
            const int cellH = 280;
            const int labelW = 168;
            const int rows = 2;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            string[] names = { "TUCK", "SWEEP", "HAND", "SHOULDER", "BACK", "LEGS", "PLANT", "RISE" };
            Text(pix, w, h, 12, 16, "ROLL", 236, 228, 210, 3);
            Text(pix, w, h, 12, 42, "AFTER", 120, 196, 150, 2);
            Text(pix, w, h, 12, cellH + 16, "ROLL", 236, 228, 210, 3);
            Text(pix, w, h, 12, cellH + 42, "BEFORE", 196, 122, 96, 2);
            for (int i = 0; i < frames; i++)
            {
                float u = i / 7f;
                int ox = labelW + i * cellW;
                int floor = 248;
                DrawFloor(pix, w, h, ox + 16, ox + cellW - 16, floor);
                VerbExitSample s = VerbExitClips.At(VerbExitId.Roll, u, 1f, false, false);
                LandingRollPose.Figure fig = LandingRollPose.PoseFigure(s, s.RootSpin, false);
                DrawFig(pix, w, h, ox + 78, floor, fig, fig.Shift, 120, 196, 160);
                Text(pix, w, h, ox + 12, 8, (i + 1).ToString(), 200, 196, 180, 2);
                Text(pix, w, h, ox + 12, cellH - 22, names[i], 220, 214, 196, 2);

                int by = cellH;
                DrawFloor(pix, w, h, ox + 16, ox + cellW - 16, by + floor);
                DrawBefore(pix, w, h, ox + 100, by + floor, u, 196, 130, 108);
                Text(pix, w, h, ox + 12, by + cellH - 22, names[i], 210, 180, 160, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void WriteAbsorbLands(string path)
        {
            const int frames = 8;
            const int cellW = 200;
            const int cellH = 250;
            const int labelW = 168;
            const int rows = 3;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            Text(pix, w, h, 12, 16, "ABSORB", 236, 228, 210, 2);
            Text(pix, w, h, 12, 40, "HANDS", 120, 186, 210, 2);
            Text(pix, w, h, 12, cellH + 16, "BEFORE", 196, 122, 96, 2);
            Text(pix, w, h, 12, cellH * 2 + 16, "LANDS", 236, 228, 210, 2);
            string[] land = { "HOP", "HOP", "KNEE", "KNEE", "KNEE", "HEAVY", "HEAVY", "HEAVY" };
            float[] scale = { 0.25f, 0.25f, 0.55f, 0.55f, 0.55f, 1f, 1f, 1f };
            float[] lu = { 0.15f, 0.5f, 0.2f, 0.5f, 0.85f, 0.2f, 0.5f, 0.9f };
            for (int i = 0; i < frames; i++)
            {
                int ox = labelW + i * cellW;
                int floor = 214;
                float u = i / 7f;
                DrawFloor(pix, w, h, ox + 12, ox + cellW - 12, floor);
                VerbExitSample deep = VerbExitClips.At(VerbExitId.RollAbsorb, u < 0.7f ? 0.2f : u, 1f, false, false);
                LandingRollPose.Figure fig = LandingRollPose.PoseFigure(deep, 0f, false);
                DrawFig(pix, w, h, ox + 70, floor, fig, fig.Shift, 120, 186, 210);
                Text(pix, w, h, ox + 10, 8, (i + 1).ToString(), 200, 196, 180, 2);

                DrawFloor(pix, w, h, ox + 12, ox + cellW - 12, cellH + floor);
                DrawBeforeAbsorb(pix, w, h, ox + 96, cellH + floor, 196, 130, 108);

                DrawFloor(pix, w, h, ox + 12, ox + cellW - 12, cellH * 2 + floor);
                VerbExitSample landPose = VerbExitClips.At(VerbExitId.SoftLand, lu[i], scale[i], false, false);
                LandingRollPose.Figure lf = LandingRollPose.PoseFigure(landPose, 0f, false);
                byte r = scale[i] > 0.8f ? (byte)230 : (byte)150;
                byte g = scale[i] < 0.4f ? (byte)210 : (byte)170;
                DrawFig(pix, w, h, ox + 70, cellH * 2 + floor, lf, lf.Shift, r, g, 120);
                Text(pix, w, h, ox + 10, cellH * 2 + cellH - 22, land[i], 220, 214, 196, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void WriteSides(string path)
        {
            const int frames = 6;
            const int cellW = 190;
            const int cellH = 240;
            const int labelW = 180;
            const int rows = 4;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            string[] row = { "VAULT R", "VAULT L", "WALL R", "WALL L" };
            VerbExitId[] ids = { VerbExitId.Vault, VerbExitId.Vault, VerbExitId.WallRun, VerbExitId.WallRun };
            bool[] left = { false, true, false, true };
            for (int rowI = 0; rowI < rows; rowI++)
            {
                Text(pix, w, h, 10, rowI * cellH + 18, row[rowI], 230, 224, 206, 2);
                for (int i = 0; i < frames; i++)
                {
                    float u = (i + 0.5f) / frames;
                    int ox = labelW + i * cellW;
                    int floor = rowI * cellH + 200;
                    DrawFloor(pix, w, h, ox + 12, ox + cellW - 12, floor);
                    VerbExitSample s = VerbExitClips.At(ids[rowI], u, 1f, false, left[rowI]);
                    LandingRollPose.Figure fig = LandingRollPose.PoseFigure(s, 0f, left[rowI]);
                    DrawFig(pix, w, h, ox + 70, floor, fig, fig.Shift, 210, 176, 120);
                    Text(pix, w, h, ox + 8, rowI * cellH + 8, (i + 1).ToString(), 190, 186, 170, 2);
                }
            }
            WritePng(path, pix, w, h);
        }

        static void DrawFig(byte[] pix, int w, int h, int ox, int floor, LandingRollPose.Figure f, float shift, byte r, byte g, byte b)
        {
            int hx = X(ox, f.Hip, shift);
            int hy = Y(floor, f.Hip, shift);
            int cx = X(ox, f.Chest, shift);
            int cy = Y(floor, f.Chest, shift);
            int headx = X(ox, f.Head, shift);
            int heady = Y(floor, f.Head, shift);
            int sx = X(ox, f.Shoulder, shift);
            int sy = Y(floor, f.Shoulder, shift);
            int handx = X(ox, f.Hand, shift);
            int handy = Y(floor, f.Hand, shift);
            int kx = X(ox, f.Knee, shift);
            int ky = Y(floor, f.Knee, shift);
            int fx = X(ox, f.Foot, shift);
            int fy = Y(floor, f.Foot, shift);
            int ox2 = X(ox, f.OffHand, shift);
            int oy = Y(floor, f.OffHand, shift);
            Bone(pix, w, h, hx, hy, cx, cy, r, g, b);
            Bone(pix, w, h, cx, cy, headx, heady, r, g, b);
            Bone(pix, w, h, cx, cy, sx, sy, r, g, b);
            Bone(pix, w, h, sx, sy, handx, handy, r, g, b);
            Bone(pix, w, h, hx, hy, kx, ky, r, g, b);
            Bone(pix, w, h, kx, ky, fx, fy, r, g, b);
            Bone(pix, w, h, cx, cy, ox2, oy, (byte)(r * 0.65f), (byte)(g * 0.65f), (byte)(b * 0.65f));
            Dot(pix, w, h, headx, heady, 7, r, g, b);
            Dot(pix, w, h, sx, sy, 6, 240, 208, 96);
            Dot(pix, w, h, hx, hy, 5, r, g, b);
            Dot(pix, w, h, fx, fy, 4, r, g, b);
        }

        /// <summary>Pass 1 read: a small tip and limbs that cross the floor line.</summary>
        static void DrawBefore(byte[] pix, int w, int h, int hx, int hy, float u, byte r, byte g, byte b)
        {
            float knee = Mathf.Lerp(-40f, -120f, u < 0.5f ? u * 2f : (1f - u) * 2f);
            float arm = Mathf.Lerp(20f, 70f, u < 0.4f ? u / 0.4f : (1f - u) / 0.6f);
            float tip = Mathf.Sin(u * 3.14159265f) * 22f;
            Limb2(pix, w, h, hx, hy, tip + 20f, knee, 48f, 44f, false, r, g, b);
            Limb2(pix, w, h, hx, hy - 46f, -arm, -16f, 40f, 34f, true, r, g, b);
            Bone(pix, w, h, hx, hy, hx + (int)tip, hy - 52, r, g, b);
            Dot(pix, w, h, hx + (int)tip, hy - 64, 6, r, g, b);
        }

        static void DrawBeforeAbsorb(byte[] pix, int w, int h, int hx, int hy, byte r, byte g, byte b)
        {
            Limb2(pix, w, h, hx, hy, 30f, -80f, 46f, 40f, false, r, g, b);
            Limb2(pix, w, h, hx, hy - 36f, -20f, -30f, 36f, 30f, true, r, g, b);
            Bone(pix, w, h, hx, hy, hx + 8, hy - 40, r, g, b);
            Dot(pix, w, h, hx + 8, hy - 52, 6, r, g, b);
        }

        static int X(int ox, Vector3 p, float shift)
        {
            return ox + (int)(p.z * 78f + p.x * 42f);
        }

        static int Y(int floor, Vector3 p, float shift)
        {
            return floor - (int)((p.y + shift) * 78f);
        }

        static void DrawFloor(byte[] pix, int w, int h, int x0, int x1, int y)
        {
            HLine(pix, w, h, x0, x1, y, 232, 196, 120);
            HLine(pix, w, h, x0, x1, y + 1, 232, 196, 120);
        }

        static void Limb2(byte[] pix, int w, int h, float x, float y, float pitch, float bend, float lenA, float lenB, bool arm, byte r, byte g, byte b)
        {
            float rad = pitch * 0.0174533f;
            float dx = arm ? -(float)Math.Sin(rad) : (float)Math.Sin(rad);
            float dy = (float)Math.Cos(rad);
            float x1 = x + dx * lenA;
            float y1 = y + dy * lenA;
            Bone(pix, w, h, (int)x, (int)y, (int)x1, (int)y1, r, g, b);
            float rad2 = (pitch + bend) * 0.0174533f;
            float dx2 = arm ? -(float)Math.Sin(rad2) : (float)Math.Sin(rad2);
            float dy2 = (float)Math.Cos(rad2);
            Bone(pix, w, h, (int)x1, (int)y1, (int)(x1 + dx2 * lenB), (int)(y1 + dy2 * lenB), r, g, b);
        }

        static void Bone(byte[] pix, int w, int h, int x0, int y0, int x1, int y1, byte r, byte g, byte b)
        {
            int steps = 36;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                int x = (int)(x0 + (x1 - x0) * u);
                int y = (int)(y0 + (y1 - y0) * u);
                Dot(pix, w, h, x, y, 3, r, g, b);
            }
        }

        static void Dot(byte[] pix, int w, int h, int x, int y, int rad, byte r, byte g, byte b)
        {
            for (int dy = -rad; dy <= rad; dy++)
            {
                for (int dx = -rad; dx <= rad; dx++)
                {
                    if (dx * dx + dy * dy > rad * rad + rad) continue;
                    Plot(pix, w, h, x + dx, y + dy, r, g, b);
                }
            }
        }

        static void HLine(byte[] pix, int w, int h, int x0, int x1, int y, byte r, byte g, byte b)
        {
            if (x1 < x0) { int t = x0; x0 = x1; x1 = t; }
            for (int x = x0; x <= x1; x++)
                Plot(pix, w, h, x, y, r, g, b);
        }

        static void Plot(byte[] pix, int w, int h, int x, int y, byte r, byte g, byte b)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            int p = (y * w + x) * 3;
            pix[p] = r;
            pix[p + 1] = g;
            pix[p + 2] = b;
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

        static void Text(byte[] pix, int w, int h, int x, int y, string text, byte r, byte g, byte b, int scale)
        {
            if (scale < 1) scale = 1;
            int cx = x;
            for (int i = 0; i < text.Length; i++)
            {
                Glyph(pix, w, h, cx, y, text[i], r, g, b, scale);
                cx += 6 * scale;
            }
        }

        static void Glyph(byte[] pix, int w, int h, int x, int y, char ch, byte r, byte g, byte b, int scale)
        {
            long bits = GlyphBits(ch);
            if (bits == 0) return;
            for (int row = 0; row < 7; row++)
            {
                int rowBits = (int)((bits >> ((6 - row) * 5)) & 31);
                for (int col = 0; col < 5; col++)
                {
                    if (((rowBits >> (4 - col)) & 1) == 0) continue;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        for (int sx = 0; sx < scale; sx++)
                            Plot(pix, w, h, x + col * scale + sx, y + row * scale + sy, r, g, b);
                    }
                }
            }
        }

        static long GlyphBits(char ch)
        {
            switch (ch)
            {
                case 'A': return 0b01110100011000111111100011000110001L;
                case 'B': return 0b111101000111110100011000111110L;
                case 'C': return 0b01110100011000010000100001000101110L;
                case 'D': return 0b11110100011000110001100011000111110L;
                case 'E': return 0b111111000011110100001000011111L;
                case 'F': return 0b111111000011110100001000010000L;
                case 'G': return 0b01110100011000010111100011000101110L;
                case 'H': return 0b10001100011000111111100011000110001L;
                case 'I': return 0b01110001000010000100001000010001110L;
                case 'K': return 0b10001100101010011000101001001010001L;
                case 'L': return 0b100001000010000100001000011111L;
                case 'M': return 0b10001110111010110001100011000110001L;
                case 'N': return 0b10001110011010110011100011000110001L;
                case 'O': return 0b01110100011000110001100011000101110L;
                case 'P': return 0b11110100011000111110100001000010000L;
                case 'R': return 0b111101000111110101011001010010L;
                case 'S': return 0b01111100001000001110000010000111110L;
                case 'T': return 0b11111001000010000100001000010000100L;
                case 'U': return 0b10001100011000110001100011000101110L;
                case 'V': return 0b10001100011000110001010100101000100L;
                case 'W': return 0b10001100011000110101101011010101010L;
                case 'Y': return 0b10001100010101000100001000010000100L;
                case '1': return 0b00100011000010000100001000010001110L;
                case '2': return 0b011101000100001000100010001000011111L;
                case '3': return 0b01110100010000100110000011000101110L;
                case '4': return 0b00010001100101010001111110001000010L;
                case '5': return 0b11111100001111000001000011000101110L;
                case '6': return 0b01110100001000011110100011000101110L;
                case '7': return 0b11111000010001000010001000010000100L;
                case '8': return 0b011101000110001011100100011000101110L;
                case ' ': return 0L;
                default: return 0L;
            }
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
                using (var def = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
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
