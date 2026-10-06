using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tag.Practice;
using Tag.Settings;

namespace Tag.Level
{
    /// <summary>
    /// Named ground zones, one tall crown per zone, and visual trim.
    /// Crowns sit on an existing flag and stay too thin to be a floor.
    /// Trim, gates, and the east-sand crown have no collider.
    /// </summary>
    public static class ZoneReadability
    {
        public struct Mark
        {
            public string Name;
            public string Mat;
            public float X, Y, Z, Sx, Sy, Sz;
        }

        public sealed class Report
        {
            public bool Ok = true;
            public string Line = "";
            public string Failure = "";
            public string Drift = "";

            public void Fail(string why)
            {
                Ok = false;
                if (Failure.Length == 0) Failure = why;
            }
        }

        struct Band
        {
            public string Name;
            public float X0, X1, Z0, Z1;
        }

        static readonly string[] MegaNames = { "West Yard", "North Bowl", "Mid Court", "South Court", "East Forts" };
        static readonly string[] PocketNames = { "West Lawn", "Center Court", "Fast Lane", "East Sand" };
        static readonly string[] StackNames = { "South Yard", "East Lane", "West Stack", "North Roof" };
        static readonly string[] ZoneMats = { "zbrick", "zwine", "zindigo", "zolive", "zslate" };
        static readonly string[] Gameplay = { "cling", "slide", "plate", "zip", "tag" };

        static readonly Band[] MegaBands =
        {
            new Band { Name = "West Yard", X0 = 0f, X1 = 46f, Z0 = 0f, Z1 = 100f },
            new Band { Name = "North Bowl", X0 = 46f, X1 = 120f, Z0 = 64f, Z1 = 100f },
            new Band { Name = "Mid Court", X0 = 46f, X1 = 120f, Z0 = 36f, Z1 = 64f },
            new Band { Name = "South Court", X0 = 46f, X1 = 120f, Z0 = 0f, Z1 = 36f },
            new Band { Name = "East Forts", X0 = 120f, X1 = 160f, Z0 = 0f, Z1 = 100f },
        };

        static readonly Band[] PocketBands =
        {
            new Band { Name = "West Lawn", X0 = 0f, X1 = 26f, Z0 = 0f, Z1 = 50f },
            new Band { Name = "Center Court", X0 = 26f, X1 = 52f, Z0 = 0f, Z1 = 50f },
            new Band { Name = "Fast Lane", X0 = 52f, X1 = 70f, Z0 = 0f, Z1 = 50f },
            new Band { Name = "East Sand", X0 = 70f, X1 = 80f, Z0 = 0f, Z1 = 50f },
        };

        static readonly Band[] StackBands =
        {
            new Band { Name = "South Yard", X0 = 0f, X1 = 55f, Z0 = 0f, Z1 = 32f },
            new Band { Name = "East Lane", X0 = 55f, X1 = 110f, Z0 = 0f, Z1 = 32f },
            new Band { Name = "West Stack", X0 = 0f, X1 = 55f, Z0 = 32f, Z1 = 70f },
            new Band { Name = "North Roof", X0 = 55f, X1 = 110f, Z0 = 32f, Z1 = 70f },
        };

        static readonly string[] MegaFlags = { "Landmark_Z1_Flag", "Landmark_Z5_Flag", "Landmark_Z7_Flag", "Landmark_Z8_Flag", "Landmark_Z6_Army_Flag" };
        static readonly string[] PocketFlags = { "Landmark_Yard_Flag", "Landmark_Dome_Flag", "Landmark_Lane_Flag" };
        static readonly string[] StackFlags = { "Landmark_Yard_Flag", "Landmark_Lane_Flag", "Landmark_Mid_Flag", "Landmark_Roof_Flag" };
        static readonly string[] CrownTags = { "Water", "Crane", "Clock", "Sign", "Chimney" };
        // Caps sit on the 16 m mega flags. A jump off anything above ~18.1 m clears the 33 m fence.
        // One axis stays under 1.1 so the cap is not a deck. Thickness stays off the 0.05–0.30 snag band.
        static readonly float[] CrownSx = { 3.4f, 7.2f, 2.2f, 0.22f, 0.55f };
        static readonly float[] CrownSy = { 2.0f, 0.36f, 2.0f, 2.0f, 2.0f };
        static readonly float[] CrownSz = { 0.8f, 0.22f, 0.22f, 3.2f, 0.55f };

        static readonly string[] MegaChips = BuildChips(MegaNames);
        static readonly string[] PocketChips = BuildChips(PocketNames);
        static readonly string[] StackChips = BuildChips(StackNames);

        static readonly string[] SkillNames =
        {
            "ClingSkip", "BowlLine", "KnightLine", "FortHop", "WestZip",
            "LaneZip", "SouthPad", "DomeClimb", "LaneRun", "YardHop",
            "RoofZip", "YardPad", "WestClimb",
        };

        static readonly float[] SkillBeginner =
        {
            2.14f, 2.35f, 1.23f, 1.88f, 2.75f,
            1.36f, 1.25f, 1.08f, 1.58f, 1.74f,
            3.02f, 1.30f, 1.41f,
        };

        static readonly string[] PracticeIds =
        {
            "mega-beginner", "mega-wall", "mega-toy",
            "pocket-beginner", "pocket-toy",
            "stack-beginner", "stack-toy",
        };

        static readonly float[] PracticeBase =
        {
            7.550f, 2.650f, 2.500f, 2.017f, 5.267f, 5.383f, 10.033f,
        };

        static readonly float[] HueLock = { 22f, 55f, 108f, 174f, 220f, 296f };

        public static int ZoneCount => MegaNames.Length + PocketNames.Length + StackNames.Length;

        public static string NameAt(int arena, float x, float z)
        {
            Band[] bands = Bands(arena);
            for (int i = bands.Length - 1; i >= 0; i--)
            {
                Band b = bands[i];
                if (x >= b.X0 && x < b.X1 && z >= b.Z0 && z < b.Z1) return b.Name;
            }
            if (bands.Length == 0) return "";
            return bands[0].Name;
        }

        public static string Chip(int seat, float x, float z)
        {
            return Chip(seat, ParkArena.Id, x, z);
        }

        public static string Chip(int seat, int arena, float x, float z)
        {
            if (seat < 0) seat = 0;
            if (seat > 3) seat = 3;
            string[] names = Names(arena);
            string[] chips = Chips(arena);
            int zone = Index(arena, x, z);
            if (zone < 0) zone = 0;
            int i = seat * names.Length + zone;
            if (i < 0 || i >= chips.Length) return chips[0];
            return chips[i];
        }

        public static int LabelCount(int arena)
        {
            return Names(arena).Length;
        }

        public static void LabelAt(int arena, int index, out string name, out float x, out float z)
        {
            Band[] bands = Bands(arena);
            if (index < 0 || index >= bands.Length)
            {
                name = "";
                x = 0f;
                z = 0f;
                return;
            }
            Band b = bands[index];
            name = b.Name;
            x = (b.X0 + b.X1) * 0.5f;
            z = (b.Z0 + b.Z1) * 0.5f;
        }

        public static string Glyph(char c)
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
                case ' ': return "00000000000000000000000000000000000";
                default: return "00000000000000000000000000000000000";
            }
        }

        public static void AppendCrowns(List<MegaParkP1Layout.Solid> list, int arena)
        {
            if (list == null) return;
            string[] flags = arena == ParkArena.Pocket ? PocketFlags : arena == ParkArena.Stack ? StackFlags : MegaFlags;
            for (int i = 0; i < flags.Length; i++)
            {
                int found = -1;
                for (int s = 0; s < list.Count; s++)
                {
                    if (list[s].Name == flags[i])
                    {
                        found = s;
                        break;
                    }
                }
                if (found < 0) continue;
                MegaParkP1Layout.Solid flag = list[found];
                int shape = i;
                if (shape >= CrownSx.Length) shape = CrownSx.Length - 1;
                float top = flag.Y + flag.Sy * 0.5f;
                float sy = CrownSy[shape];
                list.Add(new MegaParkP1Layout.Solid
                {
                    Name = "Landmark_Crown_" + CrownTags[shape],
                    Zone = flag.Zone,
                    Kind = "landmark",
                    Mat = flag.Mat,
                    X = flag.X,
                    Y = top + sy * 0.5f,
                    Z = flag.Z,
                    Sx = CrownSx[shape],
                    Sy = sy,
                    Sz = CrownSz[shape],
                    SupportY = top,
                });
            }
        }

        public static Mark[] Fill(int arena, MegaParkP1Layout.Solid[] solids)
        {
            var list = new List<Mark>(96);
            float mapW = arena == ParkArena.Stack ? StackYardLayout.MapW : arena == ParkArena.Pocket ? PocketParkLayout.MapW : MegaParkP1Layout.MapW;
            float mapD = arena == ParkArena.Stack ? StackYardLayout.MapD : arena == ParkArena.Pocket ? PocketParkLayout.MapD : MegaParkP1Layout.MapD;
            if (solids != null)
            {
                for (int i = 0; i < solids.Length; i++)
                {
                    MegaParkP1Layout.Solid s = solids[i];
                    if (s.Kind != "wall") continue;
                    bool thinX = s.Sx <= s.Sz;
                    list.Add(new Mark
                    {
                        Name = "WallTrim_" + s.Name,
                        Mat = "ztrim",
                        X = s.X,
                        Y = s.Y,
                        Z = s.Z,
                        Sx = thinX ? s.Sx + 0.12f : Math.Max(0.2f, s.Sx * 0.92f),
                        Sy = 0.22f,
                        Sz = thinX ? Math.Max(0.2f, s.Sz * 0.92f) : s.Sz + 0.12f,
                    });
                }
            }
            Brim(list, mapW, mapD);
            Borders(list, arena);
            if (arena == ParkArena.Pocket)
            {
                list.Add(new Mark { Name = "Landmark_Crown_East_Pole", Mat = "zolive", X = 75f, Y = 8f, Z = 25f, Sx = 0.42f, Sy = 16f, Sz = 0.42f });
                list.Add(new Mark { Name = "Landmark_Crown_East", Mat = "zolive", X = 75f, Y = 17f, Z = 25f, Sx = 3.2f, Sy = 2f, Sz = 0.8f });
            }
            Gates(list, arena);
            return list.ToArray();
        }

        public static Report Run()
        {
            var report = new Report();
            int saved = ParkArena.Id;
            bool savedChoice = ParkArena.HasExplicitChoice;
            int zones = ZoneCount;
            int landmarks = CountCrowns();
            bool cvd = CvdOk();
            bool contrast = ContrastOk();
            bool labels = LabelsOk(report);
            bool drift = RoutesOk(report);
            ParkArena.Select(saved);
            ParkArena.HasExplicitChoice = savedChoice;
            if (zones != 13) report.Fail("zone count");
            if (landmarks != 13) report.Fail("landmark count " + landmarks.ToString(CultureInfo.InvariantCulture));
            if (!cvd) report.Fail("zone colors collide under a cvd sim");
            if (!contrast) report.Fail("a zone color missed 3:1");
            report.Line = "zones arenas=3 zones=" + zones.ToString(CultureInfo.InvariantCulture)
                + " landmarks=" + landmarks.ToString(CultureInfo.InvariantCulture)
                + " cvd=" + (cvd ? "ok" : "bad")
                + " contrast=" + (contrast ? "ok" : "bad")
                + " routesDrift" + (drift ? "<=5%" : ">5%")
                + " labels=" + (labels ? "ok" : "bad");
            if (report.Drift.Length > 0)
                report.Line = report.Line + " " + report.Drift;
            if (!report.Ok)
                report.Line = report.Line + " FAIL " + report.Failure;
            return report;
        }

        static void Brim(List<Mark> list, float mapW, float mapD)
        {
            const float t = 0.4f;
            const float y = 0.08f;
            const float sy = 0.08f;
            list.Add(new Mark { Name = "KillBrim_S", Mat = "rubber", X = mapW * 0.5f, Y = y, Z = t * 0.5f, Sx = mapW - 1.2f, Sy = sy, Sz = t });
            list.Add(new Mark { Name = "KillBrim_N", Mat = "rubber", X = mapW * 0.5f, Y = y, Z = mapD - t * 0.5f, Sx = mapW - 1.2f, Sy = sy, Sz = t });
            list.Add(new Mark { Name = "KillBrim_W", Mat = "rubber", X = t * 0.5f, Y = y, Z = mapD * 0.5f, Sx = t, Sy = sy, Sz = mapD - 1.2f });
            list.Add(new Mark { Name = "KillBrim_E", Mat = "rubber", X = mapW - t * 0.5f, Y = y, Z = mapD * 0.5f, Sx = t, Sy = sy, Sz = mapD - 1.2f });
        }

        static void Borders(List<Mark> list, int arena)
        {
            const float y = 0.06f;
            const float sy = 0.06f;
            const float t = 0.28f;
            if (arena == ParkArena.Pocket)
            {
                EdgeX(list, "Border_P0", 26f, 0f, 50f, y, sy, t);
                EdgeX(list, "Border_P1", 52f, 0f, 50f, y, sy, t);
                EdgeX(list, "Border_P2", 70f, 0f, 50f, y, sy, t);
                return;
            }
            if (arena == ParkArena.Stack)
            {
                EdgeX(list, "Border_S0", 55f, 0f, 70f, y, sy, t);
                EdgeZ(list, "Border_S1", 32f, 0f, 110f, y, sy, t);
                return;
            }
            EdgeX(list, "Border_M0", 46f, 0f, 100f, y, sy, t);
            EdgeX(list, "Border_M1", 120f, 0f, 100f, y, sy, t);
            EdgeZ(list, "Border_M2", 36f, 46f, 120f, y, sy, t);
            EdgeZ(list, "Border_M3", 64f, 46f, 120f, y, sy, t);
        }

        static void EdgeX(List<Mark> list, string name, float x, float z0, float z1, float y, float sy, float t)
        {
            list.Add(new Mark { Name = name, Mat = "ztrim", X = x, Y = y, Z = (z0 + z1) * 0.5f, Sx = t, Sy = sy, Sz = z1 - z0 });
        }

        static void EdgeZ(List<Mark> list, string name, float z, float x0, float x1, float y, float sy, float t)
        {
            list.Add(new Mark { Name = name, Mat = "ztrim", X = (x0 + x1) * 0.5f, Y = y, Z = z, Sx = x1 - x0, Sy = sy, Sz = t });
        }

        static void Gates(List<Mark> list, int arena)
        {
            string want = arena == ParkArena.Pocket ? "Pocket Park" : arena == ParkArena.Stack ? "Stack Yard" : "Mega Park";
            PracticeCatalog.Load();
            for (int r = 0; r < PracticeCatalog.Count; r++)
            {
                PracticeRoute route = PracticeCatalog.All[r];
                if (route == null || route.Gates == null || route.Arena != want) continue;
                for (int g = 0; g < route.Gates.Length; g++)
                {
                    PracticeGate gate = route.Gates[g];
                    string kind = gate.Kind == PracticeCatalog.KindFinish ? "f" : gate.Kind == PracticeCatalog.KindCheck ? "c" : "s";
                    string mat = kind == "s" ? "hop" : kind == "f" ? "knight" : "amber";
                    string name = "Gate_" + route.Id + "_" + kind + g.ToString(CultureInfo.InvariantCulture);
                    float y = gate.Y > 0.2f ? gate.Y : 0.9f;
                    list.Add(new Mark { Name = name, Mat = mat, X = gate.X, Y = 0.08f, Z = gate.Z, Sx = 1.35f, Sy = 0.08f, Sz = 1.35f });
                    list.Add(new Mark { Name = name + "L", Mat = mat, X = gate.X - 0.62f, Y = y, Z = gate.Z, Sx = 0.16f, Sy = 2.2f, Sz = 0.16f });
                    list.Add(new Mark { Name = name + "R", Mat = mat, X = gate.X + 0.62f, Y = y, Z = gate.Z, Sx = 0.16f, Sy = 2.2f, Sz = 0.16f });
                    list.Add(new Mark { Name = name + "B", Mat = mat, X = gate.X, Y = y + 1.05f, Z = gate.Z, Sx = 1.4f, Sy = 0.16f, Sz = 0.16f });
                }
            }
        }

        static int CountCrowns()
        {
            int n = 0;
            for (int a = 0; a < 3; a++)
            {
                MegaParkP1Layout.Solid[] solids = a == ParkArena.Pocket ? PocketParkLayout.BuildSolids()
                    : a == ParkArena.Stack ? StackYardLayout.BuildSolids()
                    : MegaParkP1Layout.BuildSolids();
                for (int i = 0; i < solids.Length; i++)
                {
                    if (solids[i].Name != null && solids[i].Name.StartsWith("Landmark_Crown_", StringComparison.Ordinal))
                        n++;
                }
                Mark[] marks = Fill(a, solids);
                for (int i = 0; i < marks.Length; i++)
                {
                    if (marks[i].Name == "Landmark_Crown_East") n++;
                }
            }
            return n;
        }

        static bool CvdOk()
        {
            var rgb = new float[ZoneMats.Length * 3];
            for (int i = 0; i < ZoneMats.Length; i++)
            {
                if (!MegaParkP1Layout.TryLook(ZoneMats[i], out float r, out float g, out float b, out _, out _))
                    return false;
                rgb[i * 3] = r;
                rgb[i * 3 + 1] = g;
                rgb[i * 3 + 2] = b;
                if (Sat(r, g, b) >= 0.2f && HueGap(r, g, b) < 18f) return false;
            }
            for (int cvd = 0; cvd < AccessibilityPalette.CvdCount; cvd++)
            {
                for (int a = 0; a < ZoneMats.Length; a++)
                {
                    AccessibilityPalette.Simulate(cvd, rgb[a * 3], rgb[a * 3 + 1], rgb[a * 3 + 2], out float ar, out float ag, out float ab);
                    for (int b = a + 1; b < ZoneMats.Length; b++)
                    {
                        AccessibilityPalette.Simulate(cvd, rgb[b * 3], rgb[b * 3 + 1], rgb[b * 3 + 2], out float br, out float bg, out float bb);
                        float d = Dist(ar, ag, ab, br, bg, bb);
                        if (d < 0.06f) return false;
                    }
                }
            }
            return true;
        }

        static bool ContrastOk()
        {
            for (int i = 0; i < ZoneMats.Length; i++)
            {
                if (!MegaParkP1Layout.TryLook(ZoneMats[i], out float r, out float g, out float b, out _, out _))
                    return false;
                for (int t = 0; t < Gameplay.Length; t++)
                {
                    if (!MegaParkP1Layout.TryLook(Gameplay[t], out float tr, out float tg, out float tb, out _, out _))
                        return false;
                    if (AccessibilityPalette.Contrast(r, g, b, tr, tg, tb) < 3f) return false;
                }
                for (int p = 0; p < AccessibilityPalette.Players; p++)
                {
                    AccessibilityPalette.Player(AccessibilityPalette.Default, p, out float pr, out float pg, out float pb);
                    if (AccessibilityPalette.Contrast(r, g, b, pr, pg, pb) < 3f) return false;
                }
                AccessibilityPalette.It(AccessibilityPalette.Default, out float ir, out float ig, out float ib);
                if (AccessibilityPalette.Contrast(r, g, b, ir, ig, ib) < 3f) return false;
            }
            return true;
        }

        static bool LabelsOk(Report report)
        {
            string mini = Read("Assets/Scripts/Level/ParkMinimap.cs");
            string hud = Read("Assets/Scripts/Modes/VerbStatusHud.cs");
            string front = Read("Assets/Scripts/Level/ArenaStill.Front.cs");
            if (mini == null || hud == null || front == null)
            {
                report.Fail("label files missing");
                return false;
            }
            if (mini.IndexOf("LabelAt", StringComparison.Ordinal) < 0
                || hud.IndexOf("ZoneReadability.Chip", StringComparison.Ordinal) < 0
                || front.IndexOf("ZoneReadability.Chip", StringComparison.Ordinal) < 0)
            {
                report.Fail("a zone label is not wired");
                return false;
            }
            if (Chip(1, ParkArena.Mega, 20f, 50f) != "P2 · West Yard")
            {
                report.Fail("west chip");
                return false;
            }
            if (NameAt(ParkArena.Mega, 80f, 80f) != "North Bowl"
                || NameAt(ParkArena.Mega, 90f, 50f) != "Mid Court"
                || NameAt(ParkArena.Mega, 60f, 10f) != "South Court"
                || NameAt(ParkArena.Mega, 140f, 40f) != "East Forts"
                || NameAt(ParkArena.Pocket, 75f, 20f) != "East Sand"
                || NameAt(ParkArena.Stack, 90f, 50f) != "North Roof")
            {
                report.Fail("a zone name missed its rect");
                return false;
            }
            int walls = 0;
            int stripes = 0;
            for (int a = 0; a < 3; a++)
            {
                MegaParkP1Layout.Solid[] solids = a == ParkArena.Pocket ? PocketParkLayout.BuildSolids()
                    : a == ParkArena.Stack ? StackYardLayout.BuildSolids()
                    : MegaParkP1Layout.BuildSolids();
                for (int i = 0; i < solids.Length; i++)
                    if (solids[i].Kind == "wall") walls++;
                Mark[] marks = Fill(a, solids);
                for (int i = 0; i < marks.Length; i++)
                    if (marks[i].Name != null && marks[i].Name.StartsWith("WallTrim_", StringComparison.Ordinal))
                        stripes++;
                if (!GatesOk(marks, a, report)) return false;
            }
            if (walls < 1 || stripes != walls)
            {
                report.Fail("wall trim " + stripes.ToString(CultureInfo.InvariantCulture) + "/" + walls.ToString(CultureInfo.InvariantCulture));
                return false;
            }
            return true;
        }

        static bool GatesOk(Mark[] marks, int arena, Report report)
        {
            string want = arena == ParkArena.Pocket ? "Pocket Park" : arena == ParkArena.Stack ? "Stack Yard" : "Mega Park";
            PracticeCatalog.Load();
            int routes = 0;
            for (int r = 0; r < PracticeCatalog.Count; r++)
            {
                PracticeRoute route = PracticeCatalog.All[r];
                if (route == null || route.Arena != want) continue;
                routes++;
                bool start = false;
                bool check = false;
                for (int g = 0; g < route.Gates.Length; g++)
                {
                    string kind = route.Gates[g].Kind == PracticeCatalog.KindCheck ? "_c" : route.Gates[g].Kind == PracticeCatalog.KindFinish ? "_f" : "_s";
                    string needle = "Gate_" + route.Id + kind;
                    for (int m = 0; m < marks.Length; m++)
                    {
                        if (marks[m].Name == null || marks[m].Name.IndexOf(needle, StringComparison.Ordinal) < 0) continue;
                        if (kind == "_s") start = true;
                        if (kind == "_c") check = true;
                    }
                }
                if (!start || !check)
                {
                    report.Fail(route.Id + " gate marker");
                    return false;
                }
            }
            if (routes < 1)
            {
                report.Fail("practice routes missing for labels");
                return false;
            }
            return true;
        }

        static bool RoutesOk(Report report)
        {
            float worst = 0f;
            string worstName = "";
            bool ok = true;
            string[] docs = { "Docs/MegaPark_SkillRoutes.md", "Docs/PocketPark_SkillRoutes.md", "Docs/StackYard_SkillRoutes.md" };
            for (int d = 0; d < docs.Length; d++)
            {
                string text = Read(docs[d]);
                if (text == null)
                {
                    report.Fail("skill route doc missing");
                    return false;
                }
                for (int i = 0; i < SkillNames.Length; i++)
                {
                    float time;
                    if (!BeginnerOf(text, SkillNames[i], out time)) continue;
                    float drift = Rel(time, SkillBeginner[i]);
                    if (drift > worst)
                    {
                        worst = drift;
                        worstName = SkillNames[i];
                    }
                    if (drift > 0.05f) ok = false;
                }
            }
            PracticeCatalog.Load();
            for (int i = 0; i < PracticeIds.Length; i++)
            {
                if (PracticeBase[i] <= 0.001f) continue;
                float time = PracticeBests.TimeOf(PracticeIds[i]);
                float drift = Rel(time, PracticeBase[i]);
                if (drift > worst)
                {
                    worst = drift;
                    worstName = PracticeIds[i];
                }
                if (drift > 0.05f) ok = false;
            }
            report.Drift = "maxDrift=" + (worst * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "% " + worstName;
            if (!ok) report.Fail("route drift " + report.Drift);
            return ok;
        }

        static bool BeginnerOf(string text, string name, out float time)
        {
            time = 0f;
            string needle = "| " + name + " |";
            int i = text.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0) return false;
            int line = text.IndexOf('\n', i);
            if (line < 0) line = text.Length;
            string row = text.Substring(i, line - i);
            string[] bits = row.Split('|');
            if (bits.Length < 4) return false;
            string cell = bits[3].Replace("s", "").Trim();
            return float.TryParse(cell, NumberStyles.Float, CultureInfo.InvariantCulture, out time);
        }

        static float Rel(float now, float baseline)
        {
            if (baseline <= 0.001f) return 1f;
            float d = now - baseline;
            if (d < 0f) d = -d;
            return d / baseline;
        }

        static int Index(int arena, float x, float z)
        {
            Band[] bands = Bands(arena);
            for (int i = bands.Length - 1; i >= 0; i--)
            {
                Band b = bands[i];
                if (x >= b.X0 && x < b.X1 && z >= b.Z0 && z < b.Z1) return i;
            }
            return 0;
        }

        static Band[] Bands(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketBands;
            if (arena == ParkArena.Stack) return StackBands;
            return MegaBands;
        }

        static string[] Names(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketNames;
            if (arena == ParkArena.Stack) return StackNames;
            return MegaNames;
        }

        static string[] Chips(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketChips;
            if (arena == ParkArena.Stack) return StackChips;
            return MegaChips;
        }

        static string[] BuildChips(string[] names)
        {
            var all = new string[4 * names.Length];
            for (int s = 0; s < 4; s++)
            {
                string seat = s == 0 ? "P1 · " : s == 1 ? "P2 · " : s == 2 ? "P3 · " : "P4 · ";
                for (int z = 0; z < names.Length; z++)
                    all[s * names.Length + z] = seat + names[z];
            }
            return all;
        }

        static float HueGap(float r, float g, float b)
        {
            float h = Hue(r, g, b);
            float best = 999f;
            for (int i = 0; i < HueLock.Length; i++)
            {
                float d = Math.Abs(h - HueLock[i]);
                if (d > 180f) d = 360f - d;
                if (d < best) best = d;
            }
            return best;
        }

        static float Hue(float r, float g, float b)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float d = max - min;
            if (d < 1e-5f) return 0f;
            float h;
            if (max == r) h = ((g - b) / d) % 6f;
            else if (max == g) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;
            if (h < 0f) h += 6f;
            return h * 60f;
        }

        static float Sat(float r, float g, float b)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            if (max < 1e-5f) return 0f;
            return (max - min) / max;
        }

        static float Dist(float ar, float ag, float ab, float br, float bg, float bb)
        {
            float dr = ar - br;
            float dg = ag - bg;
            float db = ab - bb;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        static string Read(string relative)
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                string path = Path.Combine(dir, relative);
                if (File.Exists(path)) return File.ReadAllText(path);
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
