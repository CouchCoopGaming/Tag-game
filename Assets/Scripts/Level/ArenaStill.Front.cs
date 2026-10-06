using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tag.Profiles;

namespace Tag.Level
{
    /// <summary>
    /// Headless front-end stills. Unity has no UI camera here, so title, setup,
    /// join, and results are layout rects with labels. Split shots rasterize
    /// the park into each viewport, then draw the same labels on top.
    /// </summary>
    public static partial class ArenaStill
    {
        public static string WriteFrontEnd()
        {
            var fail = new StringBuilder();
            string dir = RepoDocs();
            if (dir == null)
            {
                return "front-stills FAIL docs folder";
            }
            string folder = Path.Combine(dir, "FrontEndStills");
            Directory.CreateDirectory(folder);
            Card(Path.Combine(folder, "Title.png"), "TAG", new[]
            {
                "* Play", "Practice", "Settings", "How to play", "Quit"
            }, fail);
            Card(Path.Combine(folder, "MatchSetup.png"), "Match setup", new[]
            {
                "Arena", "* Mega Park", "Pocket Park", "Stack Yard",
                "AI opponents  1", "Difficulty  Normal", "Round length  120s",
                "Rounds  1", "Start match", "Back"
            }, fail);
            Card(Path.Combine(folder, "Join.png"), "Join", new[]
            {
                "P1  keyboard   Sam", "P2  pad   Bea", "* P3  profile   Pat", "P4  Guest",
                "A seat picks a profile or Guest", "Split  Vertical", "Start match", "Back"
            }, fail);
            Results(Path.Combine(folder, "Results.png"), fail);
            Split(Path.Combine(folder, "Split4_MegaPark.png"), ParkArena.Mega, 4, false, fail);
            Split(Path.Combine(folder, "Split2_StackYard.png"), ParkArena.Stack, 2, false, fail);
            LocalProfiles.Clear();
            if (fail.Length > 0)
                return "front-stills FAIL " + fail.ToString();
            return "front-stills 1280x720 title,setup,join,split4,split2,results ui=layout viewports=in-world";
        }

        static void Card(string path, string title, string[] rows, StringBuilder fail)
        {
            var rgb = new byte[Width * Height * 3];
            Fill(rgb, Width, Height, 0, 0, Width, Height, 16, 22, 32);
            int panelW = 520;
            int panelH = 64 + rows.Length * 36;
            int x = (Width - panelW) / 2;
            int y = (Height - panelH) / 2;
            Fill(rgb, Width, Height, x, y, panelW, panelH, 28, 36, 48);
            Fill(rgb, Width, Height, x, y, panelW, 4, 90, 160, 210);
            Text(rgb, Width, Height, x + 28, y + 18, title, 230, 236, 242);
            for (int i = 0; i < rows.Length; i++)
            {
                bool mark = rows[i].Length > 0 && rows[i][0] == '*';
                int ry = y + 58 + i * 36;
                if (mark)
                    Fill(rgb, Width, Height, x + 16, ry - 6, panelW - 32, 30, 46, 78, 108);
                string label = mark ? rows[i].Substring(2) : rows[i];
                Text(rgb, Width, Height, x + 28, ry, label, 230, 236, 242);
            }
            WritePng(path, rgb, Width, Height);
            Check(path, Path.GetFileNameWithoutExtension(path), fail);
        }

        static void Split(string path, int arena, int humans, bool horizontal, StringBuilder fail)
        {
            List<Tri> tris = Gather(arena);
            float mapW = arena == ParkArena.Stack ? StackYardLayout.MapW
                : arena == ParkArena.Pocket ? PocketParkLayout.MapW
                : MegaParkP1Layout.MapW;
            float mapD = arena == ParkArena.Stack ? StackYardLayout.MapD
                : arena == ParkArena.Pocket ? PocketParkLayout.MapD
                : MegaParkP1Layout.MapD;
            Sun(out float lsx, out float lsy, out float lsz);
            float half = Math.Max(mapW, mapD) * 0.70f + 28f;
            var shadow = new float[768 * 768];
            for (int i = 0; i < shadow.Length; i++) shadow[i] = -1e20f;
            float sox = mapW * 0.5f;
            float soy = 6f;
            float soz = mapD * 0.5f;
            Basis(lsx, lsy, lsz, out float srx, out float sry, out float srz, out float sux, out float suy, out float suz);
            for (int i = 0; i < tris.Count; i++)
            {
                if (tris[i].A < 0.99f) continue;
                ShadowTri(tris[i], shadow, 768, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            }

            var rgb = new byte[Width * Height * 3];
            Fill(rgb, Width, Height, 0, 0, Width, Height, 8, 10, 14);
            int cols = humans == 2 && !horizontal ? 2 : humans <= 2 ? 1 : 2;
            int rows = humans <= 2 && !horizontal ? 1 : 2;
            if (humans == 2 && horizontal)
            {
                cols = 1;
                rows = 2;
            }
            int gap = 8;
            int vw = (Width - gap * (cols + 1)) / cols;
            int vh = (Height - gap * (rows + 1) - 28) / rows;
            SeatStills();
            for (int i = 0; i < humans; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int ox = gap + col * (vw + gap);
                int oy = 28 + gap + row * (vh + gap);
                ParkArena.HumanSeat(arena, i, out float ex, out _, out float ez, out _);
                var view = new byte[vw * vh * 3];
                var depth = new float[vw * vh];
                Paint(tris, view, depth, vw, vh, ex, 1.65f, ez, mapW * 0.5f, 3.2f, mapD * 0.5f, 68f, true,
                    shadow, 768, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
                Blit(rgb, Width, view, vw, vh, ox, oy);
                string chip = ZoneReadability.Chip(i, arena, ex, ez);
                int chipW = 8 + chip.Length * 6;
                Fill(rgb, Width, Height, ox, oy, chipW, 18, 12, 16, 22);
                Text(rgb, Width, Height, ox + 4, oy + 4, chip, 240, 244, 248);
            }
            string map = arena == ParkArena.Stack ? "Stack Yard" : arena == ParkArena.Pocket ? "Pocket Park" : "Mega Park";
            Text(rgb, Width, Height, 12, 6, map + "  " + humans.ToString() + " humans", 230, 236, 242);
            WritePng(path, rgb, Width, Height);
            Check(path, Path.GetFileNameWithoutExtension(path), fail);
        }

        static List<Tri> Gather(int arena)
        {
            MegaParkP1Layout.Solid[] solids;
            MegaParkP1Layout.Ramp[] ramps;
            MegaParkP1Layout.Dress[] dress;
            MegaParkP1Layout.PadSpot[] pads;
            MegaParkP1Layout.ZipLineSpot[] zips;
            if (arena == ParkArena.Pocket)
            {
                solids = PocketParkLayout.BuildSolids();
                ramps = PocketParkLayout.BuildRamps();
                dress = PocketParkLayout.BuildDressing();
                pads = PocketParkLayout.LaunchPads;
                zips = PocketParkLayout.ZipLines;
            }
            else if (arena == ParkArena.Stack)
            {
                solids = StackYardLayout.BuildSolids();
                ramps = StackYardLayout.BuildRamps();
                dress = StackYardLayout.BuildDressing();
                pads = StackYardLayout.LaunchPads;
                zips = StackYardLayout.ZipLines;
            }
            else
            {
                solids = MegaParkP1Layout.BuildSolids();
                ramps = MegaParkP1Layout.BuildRamps();
                dress = MegaParkP1Layout.BuildDressing();
                pads = MegaParkP1Layout.LaunchPads;
                zips = MegaParkP1Layout.ZipLines;
            }
            var tris = new List<Tri>(1024);
            if (solids != null)
            {
                for (int i = 0; i < solids.Length; i++)
                {
                    MegaParkP1Layout.Solid s = solids[i];
                    if (s.Kind == "mark") continue;
                    if (s.Kind == "fence")
                    {
                        AddRailFence(tris, s);
                        continue;
                    }
                    if (ContainerDeck(s))
                    {
                        AddContainerStack(tris, s);
                        continue;
                    }
                    Albedo(s.Mat, out float r, out float g, out float b);
                    AddBox(tris, s.X, s.Y, s.Z, s.Sx, s.Sy, s.Sz, r, g, b);
                    if (s.Name.StartsWith("Ship_", StringComparison.Ordinal))
                        AddCorrugation(tris, s, r, g, b);
                }
            }
            if (dress != null)
            {
                for (int i = 0; i < dress.Length; i++)
                {
                    MegaParkP1Layout.Dress d = dress[i];
                    Albedo(d.Mat, out float r, out float g, out float b);
                    AddBox(tris, d.X, d.Y, d.Z, d.Sx, d.Sy, d.Sz, r, g, b);
                }
            }
            if (ramps != null)
            {
                for (int i = 0; i < ramps.Length; i++)
                    AddRamp(tris, ramps[i]);
            }
            if (pads != null)
            {
                for (int i = 0; i < pads.Length; i++)
                    AddPad(tris, pads[i]);
            }
            if (zips != null)
            {
                for (int i = 0; i < zips.Length; i++)
                    AddZip(tris, zips[i]);
            }
            string park = arena == ParkArena.Stack ? "StackYard" : arena == ParkArena.Pocket ? "PocketPark" : "MegaPark";
            AddZoneMarks(tris, park, solids);
            return tris;
        }

        static void SeatStills()
        {
            LocalProfiles.Clear();
            int sam = LocalProfiles.Create("Sam");
            int bea = LocalProfiles.Create("Bea");
            int pat = LocalProfiles.Create("Pat");
            int alex = LocalProfiles.Create("Alex");
            LocalProfiles.TrySeat(0, sam);
            LocalProfiles.TrySeat(1, bea);
            LocalProfiles.TrySeat(2, pat);
            LocalProfiles.TrySeat(3, alex);
        }

        static void Results(string path, StringBuilder fail)
        {
            var rgb = new byte[Width * Height * 3];
            Fill(rgb, Width, Height, 0, 0, Width, Height, 16, 22, 32);
            Fill(rgb, Width, Height, 36, 24, Width - 72, Height - 48, 24, 32, 44);
            Text(rgb, Width, Height, 56, 40, "RESULTS", 236, 240, 244);
            string[] awards = { "Hot Potato   Bea", "Slipperiest   Sam  Alex", "Sky Walker   Pat", "Wall Crawler   Sam" };
            for (int i = 0; i < awards.Length; i++)
            {
                int y = 68 + i * 16;
                Fill(rgb, Width, Height, 56, y - 2, 280, 14, 46, 62, 84);
                Text(rgb, Width, Height, 60, y, awards[i], 236, 232, 210);
            }
            string[] seats = { "Sam", "Bea", "Pat", "Alex" };
            byte[] cr = { 200, 90, 230, 40 };
            byte[] cg = { 170, 210, 210, 180 };
            byte[] cb = { 40, 80, 255, 160 };
            string[] lines =
            {
                "tags 2", "It 1.4s", "air 2.1", "walls 1", "pad 0", "zip 0", "dash 0", "hit 2", "slip 1",
                "tags 3", "It 3.6s", "air 0.4", "walls 0", "pad 1", "zip 0", "dash 0", "hit 2", "slip 2",
                "tags 1", "It 2.2s", "air 4.8", "walls 0", "pad 0", "zip 1", "dash 1", "hit 1", "slip 0",
                "tags 1", "It 2.8s", "air 1.1", "walls 4", "pad 0", "zip 0", "dash 0", "hit 1", "slip 2",
            };
            int cardW = 250;
            int cardH = 420;
            for (int s = 0; s < 4; s++)
            {
                int x = 56 + s * (cardW + 16);
                int y = 150;
                Fill(rgb, Width, Height, x, y, cardW, cardH, 18, 24, 34);
                Fill(rgb, Width, Height, x, y, cardW, 6, cr[s], cg[s], cb[s]);
                Text(rgb, Width, Height, x + 12, y + 16, seats[s], cr[s], cg[s], cb[s]);
                for (int row = 0; row < 9; row++)
                    Text(rgb, Width, Height, x + 12, y + 40 + row * 18, lines[s * 9 + row], 220, 226, 232);
            }
            Text(rgb, Width, Height, 56, Height - 52, "Rematch    Change setup    Title", 180, 190, 200);
            WritePng(path, rgb, Width, Height);
            Check(path, "Results", fail);
        }

        static void Paint(List<Tri> tris, byte[] rgb, float[] depth, int w, int h,
            float ex, float ey, float ez, float tx, float ty, float tz, float fov, bool eyeLevel,
            float[] shadow, int sn, float sox, float soy, float soz,
            float srx, float sry, float srz, float sux, float suy, float suz,
            float lsx, float lsy, float lsz, float half)
        {
            for (int y = 0; y < h; y++)
            {
                float v = 1f - y / (float)(h - 1);
                float sky = eyeLevel ? 0.55f + v * 0.45f : 0.35f + v * 0.65f;
                byte br = (byte)(255f * (0.95f * (1f - sky) + 0.45f * sky));
                byte bg = (byte)(255f * (0.62f * (1f - sky) + 0.68f * sky));
                byte bb = (byte)(255f * (0.38f * (1f - sky) + 0.88f * sky));
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    int p = (row + x) * 3;
                    rgb[p] = br;
                    rgb[p + 1] = bg;
                    rgb[p + 2] = bb;
                    depth[row + x] = 1e20f;
                }
            }
            BasisFrom(ex, ey, ez, tx, ty, tz, out float fx, out float fy, out float fz, out float crx, out float cry, out float crz, out float cux, out float cuy, out float cuz);
            for (int i = 0; i < tris.Count; i++)
            {
                DrawTri(tris[i], rgb, depth, w, h, ex, ey, ez, fx, fy, fz, crx, cry, crz, cux, cuy, cuz, fov,
                    shadow, sn, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            }
        }

        static void Blit(byte[] dst, int dw, byte[] src, int sw, int sh, int ox, int oy)
        {
            for (int y = 0; y < sh; y++)
            {
                int dy = oy + y;
                for (int x = 0; x < sw; x++)
                {
                    int si = (y * sw + x) * 3;
                    int di = (dy * dw + ox + x) * 3;
                    dst[di] = src[si];
                    dst[di + 1] = src[si + 1];
                    dst[di + 2] = src[si + 2];
                }
            }
        }

        static void Fill(byte[] rgb, int w, int h, int x, int y, int rw, int rh, byte r, byte g, byte b)
        {
            int x1 = x + rw;
            int y1 = y + rh;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x1 > w) x1 = w;
            if (y1 > h) y1 = h;
            for (int py = y; py < y1; py++)
            {
                int row = py * w;
                for (int px = x; px < x1; px++)
                {
                    int i = (row + px) * 3;
                    rgb[i] = r;
                    rgb[i + 1] = g;
                    rgb[i + 2] = b;
                }
            }
        }

        static void Text(byte[] rgb, int w, int h, int x, int y, string text, byte r, byte g, byte b)
        {
            if (string.IsNullOrEmpty(text)) return;
            int pen = x;
            for (int i = 0; i < text.Length; i++)
            {
                string rows = Font(text[i]);
                for (int row = 0; row < 7; row++)
                {
                    int py = y + row;
                    if (py < 0 || py >= h) continue;
                    for (int col = 0; col < 5; col++)
                    {
                        if (rows[row * 5 + col] != '1') continue;
                        int px = pen + col;
                        if (px < 0 || px >= w) continue;
                        int p = (py * w + px) * 3;
                        rgb[p] = r;
                        rgb[p + 1] = g;
                        rgb[p + 2] = b;
                    }
                }
                pen += 6;
            }
        }

        static string Font(char c)
        {
            if (c >= 'a' && c <= 'z') c = (char)(c - 32);
            switch (c)
            {
                case 'A': return "01110100011000111111100011000110001";
                case 'B': return "11110100011111010001100011111000000";
                case 'C': return "01110100011000010000100010111000000";
                case 'D': return "11110100011000110001100011111000000";
                case 'E': return "11111100001111010000100001111100000";
                case 'F': return "11111100001111010000100001000000000";
                case 'G': return "01110100011000010111100010111000000";
                case 'H': return "10001100011111110001100011000100000";
                case 'I': return "11111001000010000100001001111100000";
                case 'J': return "00111000100001000010100010111000000";
                case 'K': return "10001100101110010010100011000100000";
                case 'L': return "10000100001000010000100001111100000";
                case 'M': return "10001110111010110001100011000100000";
                case 'N': return "10001110011010110011100011000100000";
                case 'O': return "01110100011000110001100010111000000";
                case 'P': return "11110100011000111110100001000000000";
                case 'Q': return "01110100011000110010100100110100000";
                case 'R': return "11110100011000111110100101000100000";
                case 'S': return "01111100000111000001100011111000000";
                case 'T': return "11111001000010000100001000010000000";
                case 'U': return "10001100011000110001100010111000000";
                case 'V': return "10001100011000110001010100010000000";
                case 'W': return "10001100011010110101101010001000000";
                case 'X': return "10001010100010001010100011000100000";
                case 'Y': return "10001100010101000100001000010000000";
                case 'Z': return "11111000010001000100010001111100000";
                case '0': return "01110100011010110011100010111000000";
                case '1': return "00100011000010000100001000111000000";
                case '2': return "01110100010001000100010001111100000";
                case '3': return "11110000010111000001100011111000000";
                case '4': return "00010001100101011111000100001000000";
                case '5': return "11111100001111000001100011111000000";
                case '6': return "01110100001111010001100010111000000";
                case '7': return "11111000010001000100010000010000000";
                case '8': return "01110100010111010001100010111000000";
                case '9': return "01110100011000101111000010111000000";
                case '*': return "00100010101111101010001000000000000";
                case '.': return "00000000000000000000000000100000000";
                case '-': return "00000000001111100000000000000000000";
                case ' ': return "00000000000000000000000000000000000";
                case '·': return "00000000000000000000001000000000000";
                default: return "11111100011000110001100011111100000";
            }
        }
    }
}
