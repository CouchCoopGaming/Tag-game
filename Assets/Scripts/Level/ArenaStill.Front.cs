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

        /// <summary>
        /// Third-person chase frames for the couch HUD stills. Each seat stands
        /// on its own loop point and looks along that seat's yaw. Not part of
        /// the front-end proof stills.
        /// The headless rasterizer cannot instance the Unity Hier prefab (no
        /// AssetDatabase, and the FBX is binary). It reads the posed triangle
        /// bake of that mesh instead.
        /// </summary>
        public static string WriteHudChases(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return "hud-chases missing folder";
            Directory.CreateDirectory(folder);
            List<Tri> park = Gather(ParkArena.Mega);
            LiftChaseGround(park);
            float mapW = MegaParkP1Layout.MapW;
            float mapD = MegaParkP1Layout.MapD;
            Sun(out float lsx, out float lsy, out float lsz);
            float half = Math.Max(mapW, mapD) * 0.70f + 28f;
            var shadow = new float[768 * 768];
            for (int i = 0; i < shadow.Length; i++) shadow[i] = -1e20f;
            float sox = mapW * 0.5f;
            float soy = 6f;
            float soz = mapD * 0.5f;
            Basis(lsx, lsy, lsz, out float srx, out float sry, out float srz, out float sux, out float suy, out float suz);
            for (int i = 0; i < park.Count; i++)
            {
                if (park[i].A < 0.99f) continue;
                ShadowTri(park[i], shadow, 768, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            }
            bool hier = LoadHier();
            for (int i = 0; i < 4; i++)
            {
                ParkArena.HumanSeat(ParkArena.Mega, i, out float px, out float py, out float pz, out float yaw);
                var tris = new List<Tri>(park.Count + 64);
                tris.AddRange(park);
                LookPair(i, out float bodyR, out float bodyG, out float bodyB, out float accentR, out float accentG, out float accentB);
                if (hier) AddHier(tris, px, py, pz, yaw, bodyR, bodyG, bodyB, accentR, accentG, accentB, false);
                else AddRunner(tris, px, py, pz, bodyR, bodyG, bodyB);
                AddContact(tris, px, py, pz);
                var seatShadow = (float[])shadow.Clone();
                for (int t = park.Count; t < tris.Count; t++)
                {
                    if (tris[t].A < 0.99f) continue;
                    ShadowTri(tris[t], seatShadow, 768, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
                }
                ChaseEye(px, py, pz, yaw, out float ex, out float ey, out float ez, out float tx, out float ty, out float tz);
                if (i < 2)
                    ChasePng(tris, Path.Combine(folder, "chase2_" + i.ToString() + ".png"), 960, 1080,
                        ex, ey, ez, tx, ty, tz, seatShadow, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
                ChasePng(tris, Path.Combine(folder, "chase4_" + i.ToString() + ".png"), 960, 540,
                    ex, ey, ez, tx, ty, tz, seatShadow, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            }
            int idles = WriteIdlePortraits(folder);
            string runBake = RepoDocs();
            if (runBake != null) LoadHierFile(Path.Combine(runBake, "UiStills", "hier-run.tris"));
            WriteMenuPans(folder, park, shadow, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            string posed = hier ? "hier=posed" : "hier=missing";
            return "hud-chases " + folder + " " + posed + " idle=" + idles.ToString();
        }

        /// <summary>
        /// Two frames of one pass along the south straight, for the title and
        /// the main menu. Same runners, camera slid east. Not a proof still.
        /// </summary>
        static void WriteMenuPans(string folder, List<Tri> park, float[] shadow,
            float sox, float soy, float soz, float srx, float sry, float srz,
            float sux, float suy, float suz, float lsx, float lsy, float lsz, float half)
        {
            // A short broadside on the south straight. Spread them further and
            // the outer two leave the frame before each one is tall enough.
            float[] xs = { 75.45f, 77.15f, 78.85f, 80.55f };
            float[] yaws = { 90f, 90f, 90f, 90f };
            var tris = new List<Tri>(park.Count + 256);
            tris.AddRange(park);
            for (int i = 0; i < 4; i++)
            {
                LookPair(i, out float bodyR, out float bodyG, out float bodyB, out float accentR, out float accentG, out float accentB);
                AddHier(tris, xs[i], 0.2f, 16f, yaws[i], bodyR, bodyG, bodyB, accentR, accentG, accentB, false);
                AddContact(tris, xs[i], 0.2f, 16f);
            }
            var seatShadow = (float[])shadow.Clone();
            for (int t = park.Count; t < tris.Count; t++)
            {
                if (tris[t].A < 0.99f) continue;
                ShadowTri(tris[t], seatShadow, 768, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            }
            // Raised, south of the straight, looking down onto the path. The rail
            // and the crates sit under the frame instead of in front of it.
            ChasePng(tris, Path.Combine(folder, "pan_title.png"), 1920, 1080,
                78f, 7f, 4f, 78f, 1.4f, 16f, seatShadow, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half, 30f);
            // Same eye height, stepped east and a touch wider, so the line sits
            // in the open middle: right of the lockup, left of the buttons.
            ChasePng(tris, Path.Combine(folder, "pan_main.png"), 1920, 1080,
                86f, 7f, 4f, 78.4f, 2f, 16f, seatShadow, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half, 36f);
        }

        /// <summary>
        /// Chase cameras sit on the dark zone paint, so the path reads maroon.
        /// The locked swatches stay put for the contrast proof. Only upward
        /// dirt and concrete in these stills move: tan about #C8A878, and a
        /// lighter grey. The thin aslate border sits on that path and reads
        /// as a purple lip, so those low faces become the same grey curb.
        /// </summary>
        static void LiftChaseGround(List<Tri> tris)
        {
            const float tanR = 183f / 255f;
            const float tanG = 164f / 255f;
            const float tanB = 114f / 255f;
            const float greyR = 179f / 255f;
            const float greyG = 195f / 255f;
            const float greyB = 197f / 255f;
            for (int i = 0; i < tris.Count; i++)
            {
                Tri t = tris[i];
                float y = (t.Y0 + t.Y1 + t.Y2) / 3f;
                if (y > 0.55f || y < -0.8f) continue;
                if (t.Ny >= 0.72f && DirtPath(t.R, t.G, t.B))
                {
                    t.R = tanR; t.G = tanG; t.B = tanB;
                    tris[i] = t;
                }
                else if (t.Ny >= 0.72f && SameInk(t.R, t.G, t.B, 46f / 255f, 52f / 255f, 58f / 255f))
                {
                    t.R = greyR; t.G = greyG; t.B = greyB;
                    tris[i] = t;
                }
                else if (y < 0.22f && y > -0.05f && SameInk(t.R, t.G, t.B, 72f / 255f, 66f / 255f, 86f / 255f))
                {
                    t.R = greyR; t.G = greyG; t.B = greyB;
                    tris[i] = t;
                }
            }
        }

        static bool DirtPath(float r, float g, float b)
        {
            if (SameInk(r, g, b, 40f / 255f, 22f / 255f, 22f / 255f)) return true;
            if (SameInk(r, g, b, 70f / 255f, 43f / 255f, 30f / 255f)) return true;
            if (SameInk(r, g, b, 0x3A / 255f, 0x22 / 255f, 0x18 / 255f)) return true;
            if (SameInk(r, g, b, 66f / 255f, 36f / 255f, 32f / 255f)) return true;
            // The other zone paints. From the shoulder they read as dried blood or dusk, not a path.
            if (SameInk(r, g, b, 74f / 255f, 52f / 255f, 58f / 255f)) return true;
            if (SameInk(r, g, b, 52f / 255f, 46f / 255f, 80f / 255f)) return true;
            if (SameInk(r, g, b, 32f / 255f, 52f / 255f, 40f / 255f)) return true;
            if (SameInk(r, g, b, 34f / 255f, 30f / 255f, 46f / 255f)) return true;
            return false;
        }

        static bool SameInk(float r, float g, float b, float tr, float tg, float tb)
        {
            float dr = r - tr;
            float dg = g - tg;
            float db = b - tb;
            return dr * dr + dg * dg + db * db < 0.0004f;
        }

        /// <summary>
        /// One look per seat. The first color is the body: limbs, torso, head.
        /// The second is the accent: chest panel, hands, and feet.
        /// Red/Tan, Blue/Mint, Orange/Lavender, Lavender/Mint. The four first
        /// colors stay apart, so a chase reads four different runners.
        /// </summary>
        static void LookPair(int seat, out float bodyR, out float bodyG, out float bodyB, out float accentR, out float accentG, out float accentB)
        {
            float[] br = { 224f / 255f, 107f / 255f, 240f / 255f, 178f / 255f };
            float[] bg = { 56f / 255f, 173f / 255f, 107f / 255f, 148f / 255f };
            float[] bb = { 61f / 255f, 235f / 255f, 36f / 255f, 224f / 255f };
            float[] ar = { 230f / 255f, 107f / 255f, 178f / 255f, 107f / 255f };
            float[] ag = { 194f / 255f, 209f / 255f, 148f / 255f, 209f / 255f };
            float[] ab = { 133f / 255f, 178f / 255f, 224f / 255f, 178f / 255f };
            int i = seat < 0 ? 0 : (seat > 3 ? 3 : seat);
            bodyR = br[i]; bodyG = bg[i]; bodyB = bb[i];
            accentR = ar[i]; accentG = ag[i]; accentB = ab[i];
        }

        /// <summary>
        /// Character-card portraits. Same pairs as the chase runners. Joints
        /// stay charcoal here so the tint reads on the navy card.
        /// </summary>
        /// <summary>
        /// RESULTS runners. Same posed Hier bake and the same body/accent tint
        /// as the character cards. Body is limbs, torso, and head. Accent is
        /// the chest panel, the hands, and the feet. The plate behind them is
        /// the portrait sky, keyed later only where it still matches that sky.
        /// </summary>
        public static string WritePlaceFigures(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return "place-figures missing folder";
            Directory.CreateDirectory(folder);
            string docs = RepoDocs();
            if (docs == null) return "place-figures missing docs";
            int n = 0;
            for (int seat = 0; seat < 4; seat++)
            {
                string src = Path.Combine(docs, "UiStills", "hier-idle-" + seat.ToString() + ".tris");
                if (!LoadHierFile(src)) continue;
                var tris = new List<Tri>(8);
                AddBox(tris, 0f, -0.04f, 0f, 1.4f, 0.06f, 1.1f, 0.07f, 0.08f, 0.10f);
                PlacePair(seat, out float bodyR, out float bodyG, out float bodyB, out float accentR, out float accentG, out float accentB);
                AddHier(tris, 0f, 0f, 0f, 0f, bodyR, bodyG, bodyB, accentR, accentG, accentB, true);
                AddContact(tris, 0f, 0f, 0f);
                var shadow = new float[16 * 16];
                for (int s = 0; s < shadow.Length; s++) shadow[s] = -1e20f;
                PortraitPng(tris, Path.Combine(folder, "place_" + seat.ToString() + "-composite.png"), 480, 720,
                    0f, 1.22f, 3.55f, 0f, 1.08f, 0f, shadow);
                n++;
            }
            return "place-figures " + folder + " n=" + n.ToString();
        }

        /// <summary>
        /// RESULTS bodies use the seat palette: red, blue, orange, lavender.
        /// Accent is the locked sample pairs: Tan, Mint, Tan, Mint.
        /// </summary>
        static void PlacePair(int seat, out float bodyR, out float bodyG, out float bodyB, out float accentR, out float accentG, out float accentB)
        {
            int i = seat < 0 ? 0 : (seat > 3 ? 3 : seat);
            UnityEngine.Color body = Tag.Ui.Menu.MenuMannequin.SeatColor(i);
            bodyR = body.r;
            bodyG = body.g;
            bodyB = body.b;
            float[] ar = { 0.90f, 0.42f, 0.90f, 0.42f };
            float[] ag = { 0.76f, 0.82f, 0.76f, 0.82f };
            float[] ab = { 0.52f, 0.70f, 0.52f, 0.70f };
            accentR = ar[i]; accentG = ag[i]; accentB = ab[i];
        }

        static int WriteIdlePortraits(string folder)
        {
            int n = 0;
            string docs = RepoDocs();
            if (docs == null) return 0;
            for (int i = 0; i < 4; i++)
            {
                string src = Path.Combine(docs, "UiStills", "hier-idle-" + i.ToString() + ".tris");
                if (!LoadHierFile(src)) continue;
                var tris = new List<Tri>(8);
                AddBox(tris, 0f, -0.04f, 0f, 2.6f, 0.08f, 2.6f, 183f / 255f, 164f / 255f, 114f / 255f);
                LookPair(i, out float bodyR, out float bodyG, out float bodyB, out float accentR, out float accentG, out float accentB);
                AddHier(tris, 0f, 0f, 0f, 16f, bodyR, bodyG, bodyB, accentR, accentG, accentB, true);
                AddContact(tris, 0f, 0f, 0f);
                var shadow = new float[16 * 16];
                for (int s = 0; s < shadow.Length; s++) shadow[s] = -1e20f;
                PortraitPng(tris, Path.Combine(folder, "idle_" + i.ToString() + ".png"), 640, 800,
                    0.62f, 1.08f, 3.55f, 0f, 0.90f, 0.02f, shadow);
                n++;
            }
            return n;
        }

        static void PortraitPng(List<Tri> tris, string path, int w, int h,
            float ex, float ey, float ez, float tx, float ty, float tz, float[] shadow)
        {
            var rgb = new byte[w * h * 3];
            var depth = new float[w * h];
            Paint(tris, rgb, depth, w, h, ex, ey, ez, tx, ty, tz, 28f, true,
                shadow, 16, 0f, 2f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 4f);
            WritePng(path, rgb, w, h);
        }

        struct HierTri
        {
            public float X0, Y0, Z0, X1, Y1, Z1, X2, Y2, Z2;
            public byte Mat;
        }

        static HierTri[] _hier;
        static bool _hierTried;

        static bool LoadHier()
        {
            if (_hierTried) return _hier != null;
            _hierTried = true;
            string docs = RepoDocs();
            if (docs == null) return false;
            return LoadHierFile(Path.Combine(docs, "UiStills", "hier-run.tris"));
        }

        static bool LoadHierFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                int magic = br.ReadInt32();
                if (magic != 0x52454948) return false;
                int n = br.ReadInt32();
                if (n < 100 || n > 400000) return false;
                var mesh = new HierTri[n];
                for (int i = 0; i < n; i++)
                {
                    var t = new HierTri();
                    t.Mat = br.ReadByte();
                    t.X0 = br.ReadSingle();
                    t.Y0 = br.ReadSingle();
                    t.Z0 = br.ReadSingle();
                    t.X1 = br.ReadSingle();
                    t.Y1 = br.ReadSingle();
                    t.Z1 = br.ReadSingle();
                    t.X2 = br.ReadSingle();
                    t.Y2 = br.ReadSingle();
                    t.Z2 = br.ReadSingle();
                    mesh[i] = t;
                }
                _hier = mesh;
            }
            return true;
        }

        static void AddHier(List<Tri> tris, float x, float y, float z, float yawDeg, float r, float g, float b)
        {
            AddHier(tris, x, y, z, yawDeg, r, g, b, r * 0.55f, g * 0.55f, b * 0.55f, false);
        }

        static void AddHier(List<Tri> tris, float x, float y, float z, float yawDeg,
            float r, float g, float b, float pr, float pg, float pb, bool softJoints)
        {
            if (_hier == null) return;
            float yaw = yawDeg * (float)(Math.PI / 180.0);
            float fx = (float)Math.Sin(yaw);
            float fz = (float)Math.Cos(yaw);
            float rx = (float)Math.Cos(yaw);
            float rz = -(float)Math.Sin(yaw);
            for (int i = 0; i < _hier.Length; i++)
            {
                HierTri h = _hier[i];
                HierTint(h.Mat, r, g, b, pr, pg, pb, softJoints, out float cr, out float cg, out float cb);
                AddTri(tris,
                    x + rx * h.X0 + fx * h.Z0, y + h.Y0, z + rz * h.X0 + fz * h.Z0,
                    x + rx * h.X1 + fx * h.Z1, y + h.Y1, z + rz * h.X1 + fz * h.Z1,
                    x + rx * h.X2 + fx * h.Z2, y + h.Y2, z + rz * h.X2 + fz * h.Z2,
                    cr, cg, cb);
            }
        }

        static void HierTint(byte mat, float r, float g, float b, float pr, float pg, float pb, bool softJoints,
            out float cr, out float cg, out float cb)
        {
            if (mat == 2)
            {
                if (softJoints)
                {
                    cr = 0.34f; cg = 0.33f; cb = 0.32f;
                }
                else
                {
                    cr = 0.10f; cg = 0.10f; cb = 0.12f;
                }
                return;
            }
            if (mat == 3)
            {
                if (softJoints)
                {
                    cr = 0.20f; cg = 0.19f; cb = 0.20f;
                }
                else
                {
                    cr = 0.02f; cg = 0.02f; cb = 0.02f;
                }
                return;
            }
            if (mat == 1)
            {
                cr = pr; cg = pg; cb = pb;
                return;
            }
            cr = r; cg = g; cb = b;
        }

        /// <summary>
        /// Soft disc on the ground under the feet. The sun shadow map's bias
        /// lifts the contact off the shoes, so the disc is what reads.
        /// </summary>
        static void AddContact(List<Tri> tris, float x, float y, float z)
        {
            int before = tris.Count;
            const int seg = 16;
            float y0 = y + 0.04f;
            float ix = 0.26f;
            float iz = 0.20f;
            float ox = 0.70f;
            float oz = 0.52f;
            for (int i = 0; i < seg; i++)
            {
                float a0 = (float)(i * Math.PI * 2.0 / seg);
                float a1 = (float)((i + 1) * Math.PI * 2.0 / seg);
                float c0 = (float)Math.Cos(a0);
                float s0 = (float)Math.Sin(a0);
                float c1 = (float)Math.Cos(a1);
                float s1 = (float)Math.Sin(a1);
                // Wound so the normal points up. The chase camera sits above the feet.
                AddTri(tris, x, y0, z,
                    x + c1 * ix, y0, z + s1 * iz,
                    x + c0 * ix, y0, z + s0 * iz,
                    0.02f, 0.015f, 0.02f);
                AddTri(tris,
                    x + c1 * ix, y0, z + s1 * iz,
                    x + c0 * ox, y0, z + s0 * oz,
                    x + c0 * ix, y0, z + s0 * iz,
                    0.02f, 0.015f, 0.02f);
                AddTri(tris,
                    x + c1 * ix, y0, z + s1 * iz,
                    x + c1 * ox, y0, z + s1 * oz,
                    x + c0 * ox, y0, z + s0 * oz,
                    0.02f, 0.015f, 0.02f);
            }
            for (int i = before; i < tris.Count; i++)
            {
                Tri t = tris[i];
                t.A = (i - before) < seg ? 0.58f : 0.26f;
                tris[i] = t;
            }
        }

        static void AddRunner(List<Tri> tris, float x, float y, float z, float r, float g, float b)
        {
            AddBox(tris, x, y + 0.46f, z, 0.34f, 0.92f, 0.24f, 0.12f, 0.14f, 0.18f);
            AddBox(tris, x, y + 1.22f, z, 0.46f, 0.62f, 0.26f, r, g, b);
            AddBox(tris, x, y + 1.66f, z, 0.24f, 0.28f, 0.24f, 0.93f, 0.74f, 0.60f);
        }

        static void ChaseEye(float px, float py, float pz, float yawDeg,
            out float ex, out float ey, out float ez, out float tx, out float ty, out float tz)
        {
            float yaw = yawDeg * (float)(Math.PI / 180.0);
            float fx = (float)Math.Sin(yaw);
            float fz = (float)Math.Cos(yaw);
            float rx = (float)Math.Cos(yaw);
            float rz = -(float)Math.Sin(yaw);
            float pitch = 12f * (float)(Math.PI / 180.0);
            float cp = (float)Math.Cos(pitch);
            float sp = (float)Math.Sin(pitch);
            float lx = 0.4f;
            float ly = 0.45f * cp - (-5.2f) * sp;
            float lz = 0.45f * sp + (-5.2f) * cp;
            ex = px + rx * lx + fx * lz;
            ey = py + 1.4f + ly;
            ez = pz + rz * lx + fz * lz;
            tx = px + fx * 0.9f;
            ty = py + 1.25f;
            tz = pz + fz * 0.9f;
        }

        static void ChasePng(List<Tri> tris, string path, int w, int h,
            float ex, float ey, float ez, float tx, float ty, float tz,
            float[] shadow, float sox, float soy, float soz,
            float srx, float sry, float srz, float sux, float suy, float suz,
            float lsx, float lsy, float lsz, float half, float fov = 70f)
        {
            var rgb = new byte[w * h * 3];
            var depth = new float[w * h];
            Paint(tris, rgb, depth, w, h, ex, ey, ez, tx, ty, tz, fov, true,
                shadow, 768, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
            WritePng(path, rgb, w, h);
        }
    }
}
