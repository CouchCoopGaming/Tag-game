using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Tag.Art;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Front and three-quarter sticks for the exits this pass refit,
    /// plus the pass-5 effects. Schematic. Unity is not required.
    /// </summary>
    public static class Pass5Stills
    {
        public static void Write(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            Pose(dir, "walljump", VerbExitId.WallJump, false);
            Pose(dir, "climb", VerbExitId.ClimbTopOut, false);
            Pose(dir, "vault", VerbExitId.Vault, true);
            Pose(dir, "zip", VerbExitId.ZipDrop, false);
            Pose(dir, "grapple", VerbExitId.GrappleRelease, false);
            Pose(dir, "pad", VerbExitId.LaunchLand, false);
            Pose(dir, "roll", VerbExitId.Roll, false);
            Pose(dir, "reversal", VerbExitId.SoftLand, false, true);
            Effects(dir);
        }

        static void Pose(string dir, string name, VerbExitId id, bool fromMantle)
        {
            Pose(dir, name, id, fromMantle, false);
        }

        static void Pose(string dir, string name, VerbExitId id, bool fromMantle, bool back)
        {
            Sheet(Path.Combine(dir, name + "-front.png"), id, fromMantle, back, false);
            Sheet(Path.Combine(dir, name + "-three-quarter.png"), id, fromMantle, back, true);
        }

        static void Sheet(string path, VerbExitId id, bool fromMantle, bool back, bool angle)
        {
            const int frames = 6;
            const int cellW = 170;
            const int cellH = 250;
            int w = frames * cellW;
            int h = cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            string label = angle ? "ANGLE" : "FRONT";
            Text(pix, w, h, 8, 8, label, 230, 224, 206, 2);
            for (int i = 0; i < frames; i++)
            {
                float u = i / 5f;
                int ox = i * cellW;
                int floor = 214;
                DrawFloor(pix, w, h, ox + 16, ox + cellW - 16, floor);
                VerbExitSample authored = VerbExitClips.At(id, u, 1f, false, false);
                float fwd = back ? -6f : 4f;
                VerbExitSample s = VerbExitFit.Apply(authored, id, u, false, fromMantle, 0.4f, fwd, back ? 0.2f : 0f);
                LandingRollPose.Figure fig = LandingRollPose.PoseFigure(s, s.RootSpin, false);
                DrawFig(pix, w, h, ox + 78, floor, fig, angle, 120, 196, 160);
                if (VerbExitFit.LipWeight(id, u) > 0.5f)
                    HLine(pix, w, h, ox + 20, ox + cellW - 24, Y(floor, fig.Hand, fig.Shift, angle), 240, 208, 96);
                Text(pix, w, h, ox + 8, cellH - 18, (i + 1).ToString(), 200, 196, 180, 2);
            }
            WritePng(path, pix, w, h);
        }

        static void Effects(string dir)
        {
            Lines(Path.Combine(dir, "speed-lines.png"));
            Scrape(Path.Combine(dir, "wall-scrape.png"));
            Burst(Path.Combine(dir, "tag-burst.png"));
            Flash(Path.Combine(dir, "handoff-flash.png"));
            Trails(Path.Combine(dir, "pad-zip-trails.png"));
        }

        static void Lines(string path)
        {
            int w = 640;
            int h = 280;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            Text(pix, w, h, 16, 16, "WALK", 200, 196, 180, 2);
            Text(pix, w, h, 330, 16, "SPRINT", 230, 224, 206, 2);
            Body(pix, w, h, 120, 200);
            Body(pix, w, h, 430, 200);
            int n = Pass5Look.SpeedLines(DustLook.Sprint, 1f);
            float len = Pass5Look.LineLength(DustLook.Sprint) * 70f;
            for (int i = 0; i < n; i++)
            {
                int y = 90 + i * 16;
                HLine(pix, w, h, 470, 470 + (int)len, y, 210, 220, 245);
            }
            WritePng(path, pix, w, h);
        }

        static void Scrape(string path)
        {
            int w = 760;
            int h = 280;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            string[] names = { "METAL", "WET", "CONCRETE", "GRASS" };
            int[] surf = {
                (int)DustLook.Surface.Metal,
                (int)DustLook.Surface.Wet,
                (int)DustLook.Surface.Concrete,
                (int)DustLook.Surface.Grass
            };
            for (int i = 0; i < 4; i++)
            {
                int ox = 20 + i * 185;
                Text(pix, w, h, ox, 16, names[i], 230, 224, 206, 2);
                VLine(pix, w, h, ox + 40, 50, 240, 90, 92, 98);
                HLine(pix, w, h, ox + 28, ox + 70, 210, 160, 150, 130);
                int bits = Pass5Look.ScrapeBits(surf[i], 9.5f, 1f);
                int hy = (int)(Pass5Look.ScrapeHeight(false) * 48f);
                for (int s = 0; s < bits; s++)
                {
                    int y = 200 - hy - (s % 3) * 8;
                    int x = ox + 48 + s * 6;
                    Dot(pix, w, h, x, y, 3, 240, 210, 120);
                }
            }
            WritePng(path, pix, w, h);
        }

        static void Burst(string path)
        {
            int w = 640;
            int h = 280;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            Text(pix, w, h, 24, 16, "TAG BURST", 230, 224, 206, 2);
            Text(pix, w, h, 360, 16, "WORD STAYS", 180, 176, 160, 2);
            int bits = Pass5Look.TagBits(1f);
            for (int i = 0; i < bits; i++)
            {
                float a = i / (float)bits * 6.2831855f;
                int x = 180 + (int)(Mathf.Cos(a) * 70f);
                int y = 160 + (int)(Mathf.Sin(a) * 50f);
                Dot(pix, w, h, x, y, 8, 255, 214, 64);
            }
            Dot(pix, w, h, 180, 160, 14, 255, 236, 160);
            Text(pix, w, h, 400, 140, "POP", 210, 80, 70, 3);
            WritePng(path, pix, w, h);
        }

        static void Flash(string path)
        {
            int w = 720;
            int h = 360;
            var pix = new byte[w * h * 3];
            Fill(pix, 14, 16, 22);
            Text(pix, w, h, 16, 12, "SPLIT", 230, 224, 206, 2);
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x0 = 16 + col * 350;
                int y0 = 48 + row * 150;
                Box(pix, w, h, x0, y0, x0 + 330, y0 + 136, 28, 32, 40);
                int cx = x0 + 80;
                int cy = y0 + 88;
                Body(pix, w, h, cx, cy);
                int rad = (int)(Pass5Look.FlashSpan(1f) * 18f);
                Ring(pix, w, h, cx, cy - 28, rad, 255, 90, 70);
                Text(pix, w, h, x0 + 150, y0 + 50, "IT", 255, 120, 90, 3);
            }
            WritePng(path, pix, w, h);
        }

        static void Trails(string path)
        {
            int w = 720;
            int h = 280;
            var pix = new byte[w * h * 3];
            Fill(pix, 18, 20, 26);
            Text(pix, w, h, 16, 16, "PAD", 180, 220, 245, 2);
            Text(pix, w, h, 370, 16, "ZIP", 210, 160, 245, 2);
            int padN = Pass5Look.TrailCount(1f);
            int zipN = padN;
            for (int i = 0; i < padN; i++)
            {
                int x = 40 + i * 22;
                int y = 200 - i * 6;
                Dot(pix, w, h, x, y, 4, 120, 210, 255);
                if (i > 0)
                    Bone(pix, w, h, 40 + (i - 1) * 22, 200 - (i - 1) * 6, x, y, 120, 210, 255);
            }
            for (int i = 0; i < zipN; i++)
            {
                int x = 390 + i * 24;
                int y = 150 + (int)(Mathf.Sin(i * 0.7f) * 18f);
                Dot(pix, w, h, x, y, 4, 190, 120, 255);
                if (i > 0)
                {
                    int px = 390 + (i - 1) * 24;
                    int py = 150 + (int)(Mathf.Sin((i - 1) * 0.7f) * 18f);
                    Bone(pix, w, h, px, py, x, y, 190, 120, 255);
                }
            }
            WritePng(path, pix, w, h);
        }

        static void Body(byte[] pix, int w, int h, int x, int y)
        {
            Bone(pix, w, h, x, y, x, y - 46, 180, 190, 200);
            Dot(pix, w, h, x, y - 58, 8, 180, 190, 200);
            Bone(pix, w, h, x, y - 20, x - 16, y, 180, 190, 200);
            Bone(pix, w, h, x, y - 20, x + 16, y, 180, 190, 200);
        }

        static void DrawFig(byte[] pix, int w, int h, int ox, int floor, LandingRollPose.Figure f, bool angle, byte r, byte g, byte b)
        {
            int hx = X(ox, f.Hip, angle);
            int hy = Y(floor, f.Hip, f.Shift, angle);
            int cx = X(ox, f.Chest, angle);
            int cy = Y(floor, f.Chest, f.Shift, angle);
            int headx = X(ox, f.Head, angle);
            int heady = Y(floor, f.Head, f.Shift, angle);
            int sx = X(ox, f.Shoulder, angle);
            int sy = Y(floor, f.Shoulder, f.Shift, angle);
            int handx = X(ox, f.Hand, angle);
            int handy = Y(floor, f.Hand, f.Shift, angle);
            int kx = X(ox, f.Knee, angle);
            int ky = Y(floor, f.Knee, f.Shift, angle);
            int fx = X(ox, f.Foot, angle);
            int fy = Y(floor, f.Foot, f.Shift, angle);
            Bone(pix, w, h, hx, hy, cx, cy, r, g, b);
            Bone(pix, w, h, cx, cy, headx, heady, r, g, b);
            Bone(pix, w, h, cx, cy, sx, sy, r, g, b);
            Bone(pix, w, h, sx, sy, handx, handy, r, g, b);
            Bone(pix, w, h, hx, hy, kx, ky, r, g, b);
            Bone(pix, w, h, kx, ky, fx, fy, r, g, b);
            Dot(pix, w, h, headx, heady, 6, r, g, b);
            Dot(pix, w, h, handx, handy, 4, 240, 208, 96);
            Dot(pix, w, h, fx, fy, 4, r, g, b);
        }

        static int X(int ox, Vector3 p, bool angle)
        {
            float x = angle ? p.x * 36f + p.z * 48f : p.x * 70f;
            return ox + (int)x;
        }

        static int Y(int floor, Vector3 p, float shift, bool angle)
        {
            return floor - (int)((p.y + shift) * 78f);
        }

        static void DrawFloor(byte[] pix, int w, int h, int x0, int x1, int y)
        {
            HLine(pix, w, h, x0, x1, y, 232, 196, 120);
        }

        static void Ring(byte[] pix, int w, int h, int cx, int cy, int rad, byte r, byte g, byte b)
        {
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * 6.2831855f;
                Dot(pix, w, h, cx + (int)(Mathf.Cos(a) * rad), cy + (int)(Mathf.Sin(a) * rad), 2, r, g, b);
            }
        }

        static void Box(byte[] pix, int w, int h, int x0, int y0, int x1, int y1, byte r, byte g, byte b)
        {
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                    Plot(pix, w, h, x, y, r, g, b);
            }
        }

        static void VLine(byte[] pix, int w, int h, int x, int y0, int y1, byte r, byte g, byte b)
        {
            if (y1 < y0) { int t = y0; y0 = y1; y1 = t; }
            for (int y = y0; y <= y1; y++) Plot(pix, w, h, x, y, r, g, b);
        }

        static void Bone(byte[] pix, int w, int h, int x0, int y0, int x1, int y1, byte r, byte g, byte b)
        {
            int steps = 28;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                Plot(pix, w, h, (int)(x0 + (x1 - x0) * u), (int)(y0 + (y1 - y0) * u), r, g, b);
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
            for (int x = x0; x <= x1; x++) Plot(pix, w, h, x, y, r, g, b);
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
                        for (int sx = 0; sx < scale; sx++)
                            Plot(pix, w, h, x + col * scale + sx, y + row * scale + sy, r, g, b);
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
