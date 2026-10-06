using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Tag.Practice;
using Tag.Profiles;
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

        static readonly string[] MegaChips = new string[4 * 5];
        static readonly string[] PocketChips = new string[4 * 4];
        static readonly string[] StackChips = new string[4 * 4];
        static readonly string[] SeatWord = { "P1", "P2", "P3", "P4" };
        static int _chipGen = -1;

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
            EnsureChips();
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
            // Zone landmarks are visual marks. A solid cap would be a floor or a route snag.
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
            Decals(list, arena);
            Towers(list, arena);
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
            bool eye = EyeOk(report);
            bool cvd = CvdOk();
            bool contrast = ContrastOk();
            bool labels = LabelsOk(report);
            bool drift = RoutesOk(report);
            ParkArena.Select(saved);
            ParkArena.HasExplicitChoice = savedChoice;
            if (zones != 13) report.Fail("zone count");
            if (landmarks != 13) report.Fail("landmark count " + landmarks.ToString(CultureInfo.InvariantCulture));
            if (!eye) report.Fail("a split view lost its landmarks");
            if (!cvd) report.Fail("zone colors collide under a cvd sim");
            if (!contrast) report.Fail("a zone color missed 3:1");
            report.Line = "zones arenas=3 zones=" + zones.ToString(CultureInfo.InvariantCulture)
                + " landmarks=" + landmarks.ToString(CultureInfo.InvariantCulture)
                + " eyeVisible=" + (eye ? "ok" : "bad")
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
            const float y = 0.07f;
            const float sy = 0.08f;
            const float t = 0.42f;
            if (arena == ParkArena.Pocket)
            {
                EdgeX(list, "Border_P0", "abrick", 26f, 0f, 50f, y, sy, t);
                EdgeX(list, "Border_P1", "aclay", 52f, 0f, 50f, y, sy, t);
                EdgeX(list, "Border_P2", "aindigo", 70f, 0f, 50f, y, sy, t);
                return;
            }
            if (arena == ParkArena.Stack)
            {
                EdgeX(list, "Border_S0", "abrick", 55f, 0f, 70f, y, sy, t);
                EdgeZ(list, "Border_S1", "aolive", 32f, 0f, 110f, y, sy, t);
                return;
            }
            EdgeX(list, "Border_M0", "abrick", 46f, 0f, 100f, y, sy, t);
            EdgeX(list, "Border_M1", "aslate", 120f, 0f, 100f, y, sy, t);
            EdgeZ(list, "Border_M2", "aclay", 36f, 46f, 120f, y, sy, t);
            EdgeZ(list, "Border_M3", "aolive", 64f, 46f, 120f, y, sy, t);
        }

        static void EdgeX(List<Mark> list, string name, string mat, float x, float z0, float z1, float y, float sy, float t)
        {
            list.Add(new Mark { Name = name, Mat = mat, X = x, Y = y, Z = (z0 + z1) * 0.5f, Sx = t, Sy = sy, Sz = z1 - z0 });
        }

        static void EdgeZ(List<Mark> list, string name, string mat, float z, float x0, float x1, float y, float sy, float t)
        {
            list.Add(new Mark { Name = name, Mat = mat, X = (x0 + x1) * 0.5f, Y = y, Z = z, Sx = x1 - x0, Sy = sy, Sz = t });
        }

        static void Decals(List<Mark> list, int arena)
        {
            if (arena == ParkArena.Pocket)
            {
                Patch(list, "Decal_P0", "dbrick", 10f, 18f, 7f, 1.1f);
                Patch(list, "Decal_P0b", "dbrick", 16f, 36f, 1.1f, 6f);
                Patch(list, "Decal_P1", "dclay", 36f, 14f, 8f, 1.1f);
                Patch(list, "Decal_P1b", "dclay", 42f, 34f, 1.1f, 5f);
                Patch(list, "Decal_P2", "dindigo", 58f, 20f, 6f, 1.1f);
                Patch(list, "Decal_P3", "dolive", 75f, 34f, 4f, 1.1f);
                return;
            }
            if (arena == ParkArena.Stack)
            {
                Patch(list, "Decal_S0", "dbrick", 18f, 12f, 8f, 1.2f);
                Patch(list, "Decal_S0b", "dbrick", 36f, 22f, 1.2f, 6f);
                Patch(list, "Decal_S1", "dclay", 74f, 12f, 10f, 1.2f);
                Patch(list, "Decal_S2", "dindigo", 16f, 48f, 8f, 1.2f);
                Patch(list, "Decal_S3", "dolive", 78f, 48f, 10f, 1.2f);
                return;
            }
            Patch(list, "Decal_M0", "dbrick", 16f, 40f, 10f, 1.4f);
            Patch(list, "Decal_M0b", "dbrick", 30f, 72f, 1.4f, 8f);
            Patch(list, "Decal_M1", "dolive", 70f, 82f, 12f, 1.4f);
            Patch(list, "Decal_M2", "dindigo", 84f, 48f, 1.4f, 10f);
            Patch(list, "Decal_M2b", "dindigo", 100f, 52f, 8f, 1.4f);
            Patch(list, "Decal_M3", "dclay", 78f, 16f, 12f, 1.4f);
            Patch(list, "Decal_M4", "dslate", 140f, 40f, 10f, 1.4f);
            Patch(list, "Decal_M4b", "dslate", 146f, 70f, 1.4f, 8f);
        }

        static void Patch(List<Mark> list, string name, string mat, float x, float z, float sx, float sz)
        {
            list.Add(new Mark { Name = name, Mat = mat, X = x, Y = 0.05f, Z = z, Sx = sx, Sy = 0.04f, Sz = sz });
        }

        static void Towers(List<Mark> list, int arena)
        {
            if (arena == ParkArena.Pocket)
            {
                Windmill(list, "Mill", "abrick", "zbrick", 14f, 28f, 16f);
                Archway(list, "Arch", "aclay", "zwine", 38f, 22f, 14f);
                Board(list, "Board", "aindigo", "zindigo", 60f, 36f, 15f);
                Buoy(list, "Buoy", "aolive", "zolive", 75f, 18f, 18f);
                return;
            }
            if (arena == ParkArena.Stack)
            {
                Chimney(list, "Chimney", "abrick", "zbrick", 22f, 16f, 20f);
                Gantry(list, "Gantry", "aclay", "zwine", 82f, 16f, 16f);
                Radio(list, "Mast", "aindigo", "zindigo", 20f, 50f, 22f);
                Billboard(list, "Bill", "aolive", "zolive", 82f, 52f, 15f);
                return;
            }
            Water(list, "Water", "abrick", "zbrick", 24f, 58f, 18f);
            Crane(list, "Crane", "aolive", "zolive", 72f, 84f, 22f);
            Clock(list, "Clock", "aindigo", "zindigo", 88f, 48f, 20f);
            Sign(list, "Sign", "aclay", "zwine", 86f, 14f, 14f);
            Light(list, "Light", "aslate", "zslate", 138f, 58f, 24f);
        }

        static void Box(List<Mark> list, string name, string mat, float x, float y, float z, float sx, float sy, float sz)
        {
            list.Add(new Mark { Name = name, Mat = mat, X = x, Y = y, Z = z, Sx = sx, Sy = sy, Sz = sz });
        }

        static void Crown(List<Mark> list, string tag, string mat, float x, float top, float z, float sx, float sy, float sz)
        {
            Box(list, "Landmark_Crown_" + tag, mat, x, top - sy * 0.5f, z, sx, sy, sz);
        }

        static void Water(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            float leg = 11f;
            Box(list, "Landmark_Body_" + tag + "A", body, x - 1.5f, leg * 0.5f, z - 1.5f, 0.5f, leg, 0.5f);
            Box(list, "Landmark_Body_" + tag + "B", body, x + 1.5f, leg * 0.5f, z - 1.5f, 0.5f, leg, 0.5f);
            Box(list, "Landmark_Body_" + tag + "C", body, x - 1.5f, leg * 0.5f, z + 1.5f, 0.5f, leg, 0.5f);
            Box(list, "Landmark_Body_" + tag + "D", body, x + 1.5f, leg * 0.5f, z + 1.5f, 0.5f, leg, 0.5f);
            Box(list, "Landmark_Body_" + tag + "T", body, x, 13.2f, z, 4.6f, 3.4f, 4.6f);
            Crown(list, tag, accent, x, top, z, 3.4f, 1.6f, 3.4f);
        }

        static void Crane(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "M", body, x, 9f, z, 1.15f, 18f, 1.15f);
            Box(list, "Landmark_Body_" + tag + "J", body, x + 4.2f, 17.6f, z, 9.2f, 0.7f, 0.7f);
            Box(list, "Landmark_Body_" + tag + "K", body, x - 2.2f, 17.2f, z, 2.4f, 1.1f, 1.1f);
            Box(list, "Landmark_Body_" + tag + "C", body, x, 15.2f, z, 1.8f, 1.6f, 1.8f);
            Crown(list, tag, accent, x, top, z, 2.2f, 2f, 2.2f);
        }

        static void Clock(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "S", body, x, 8f, z, 2.6f, 16f, 2.6f);
            Box(list, "Landmark_Body_" + tag + "F", "ztrim", x, 15.2f, z + 1.35f, 2.2f, 2.2f, 0.2f);
            Crown(list, tag, accent, x, top, z, 3.4f, 2.2f, 3.4f);
        }

        static void Sign(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "L", body, x - 3f, 6f, z, 0.55f, 12f, 0.55f);
            Box(list, "Landmark_Body_" + tag + "R", body, x + 3f, 6f, z, 0.55f, 12f, 0.55f);
            Box(list, "Landmark_Body_" + tag + "P", body, x, 9.4f, z, 6.6f, 4.4f, 0.45f);
            Crown(list, tag, accent, x, top, z, 7.2f, 0.9f, 0.7f);
        }

        static void Light(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "B", body, x, 4f, z, 2.4f, 8f, 2.4f);
            Box(list, "Landmark_Body_" + tag + "M", body, x, 12f, z, 1.7f, 8f, 1.7f);
            Box(list, "Landmark_Body_" + tag + "G", body, x, 16.6f, z, 3.4f, 0.7f, 3.4f);
            Box(list, "Landmark_Body_" + tag + "L", "ztrim", x, 18.4f, z, 1.3f, 2.6f, 1.3f);
            Crown(list, tag, accent, x, top, z, 2.4f, 1.8f, 2.4f);
        }

        static void Windmill(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "P", body, x, 6.5f, z, 0.7f, 13f, 0.7f);
            Box(list, "Landmark_Body_" + tag + "H", body, x, 13.2f, z, 1.6f, 1.6f, 1.6f);
            Box(list, "Landmark_Body_" + tag + "A", body, x, 13.2f, z, 6.4f, 0.35f, 0.35f);
            Box(list, "Landmark_Body_" + tag + "B", body, x, 13.2f, z, 0.35f, 6.4f, 0.35f);
            Crown(list, tag, accent, x, top, z, 1.8f, 1.2f, 1.8f);
        }

        static void Archway(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "L", body, x - 2.2f, 6f, z, 0.7f, 12f, 0.7f);
            Box(list, "Landmark_Body_" + tag + "R", body, x + 2.2f, 6f, z, 0.7f, 12f, 0.7f);
            Crown(list, tag, accent, x, top, z, 5.6f, 1.3f, 0.9f);
        }

        static void Board(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "L", body, x - 2.4f, 6f, z, 0.45f, 12f, 0.45f);
            Box(list, "Landmark_Body_" + tag + "R", body, x + 2.4f, 6f, z, 0.45f, 12f, 0.45f);
            Box(list, "Landmark_Body_" + tag + "P", body, x, 10.2f, z, 5.6f, 3.6f, 0.4f);
            Crown(list, tag, accent, x, top, z, 6f, 0.8f, 0.6f);
        }

        static void Buoy(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "P", body, x, 7f, z, 1.3f, 14f, 1.3f);
            Box(list, "Landmark_Body_" + tag + "R", body, x, 13.4f, z, 2.8f, 0.5f, 2.8f);
            Crown(list, tag, accent, x, top, z, 2.2f, 2.2f, 2.2f);
        }

        static void Chimney(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "A", body, x - 1.3f, 8f, z, 1.5f, 16f, 1.5f);
            Box(list, "Landmark_Body_" + tag + "B", body, x + 1.3f, 6.5f, z, 1.3f, 13f, 1.3f);
            Crown(list, tag, accent, x - 1.3f, top, z, 2f, 2f, 2f);
        }

        static void Gantry(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "L", body, x - 4f, 6.5f, z, 0.6f, 13f, 0.6f);
            Box(list, "Landmark_Body_" + tag + "R", body, x + 4f, 6.5f, z, 0.6f, 13f, 0.6f);
            Box(list, "Landmark_Body_" + tag + "B", body, x, 13.2f, z, 9f, 0.7f, 0.7f);
            Crown(list, tag, accent, x, top, z, 2.4f, 1.6f, 1.4f);
        }

        static void Radio(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "P", body, x, 9f, z, 0.55f, 18f, 0.55f);
            Box(list, "Landmark_Body_" + tag + "D", body, x + 1.6f, 14f, z, 2.6f, 0.35f, 2.6f);
            Box(list, "Landmark_Body_" + tag + "A", body, x, 8f, z, 4f, 0.25f, 0.25f);
            Crown(list, tag, accent, x, top, z, 1.4f, 2.2f, 1.4f);
        }

        static void Billboard(List<Mark> list, string tag, string accent, string body, float x, float z, float top)
        {
            Box(list, "Landmark_Body_" + tag + "L", body, x - 3.2f, 6f, z, 0.5f, 12f, 0.5f);
            Box(list, "Landmark_Body_" + tag + "R", body, x + 3.2f, 6f, z, 0.5f, 12f, 0.5f);
            Box(list, "Landmark_Body_" + tag + "P", body, x, 10f, z, 7.2f, 4f, 0.4f);
            Crown(list, tag, accent, x, top, z, 7.6f, 0.8f, 0.7f);
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
                Mark[] marks = Fill(a, Solids(a));
                for (int i = 0; i < marks.Length; i++)
                {
                    if (marks[i].Name != null && marks[i].Name.StartsWith("Landmark_Crown_", StringComparison.Ordinal))
                        n++;
                }
            }
            return n;
        }

        static bool EyeOk(Report report)
        {
            Mark[] marks = Fill(ParkArena.Mega, Solids(ParkArena.Mega));
            int crowns = 0;
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i].Name == null || !marks[i].Name.StartsWith("Landmark_Crown_", StringComparison.Ordinal))
                    continue;
                crowns++;
                float top = marks[i].Y + marks[i].Sy * 0.5f;
                if (top < 12f || top > 25f)
                {
                    report.Fail(marks[i].Name + " height");
                    return false;
                }
            }
            if (crowns < 2) return false;
            float mapW = MegaParkP1Layout.MapW;
            float mapD = MegaParkP1Layout.MapD;
            const int vw = 628;
            const int vh = 334;
            for (int seat = 0; seat < 4; seat++)
            {
                ParkArena.HumanSeat(ParkArena.Mega, seat, out float ex, out _, out float ez, out _);
                int seen = 0;
                for (int i = 0; i < marks.Length; i++)
                {
                    Mark m = marks[i];
                    if (m.Name == null || !m.Name.StartsWith("Landmark_Crown_", StringComparison.Ordinal))
                        continue;
                    if (Projects(m.X, m.Y, m.Z, ex, 1.65f, ez, mapW * 0.5f, 3.2f, mapD * 0.5f, 68f, vw, vh))
                        seen++;
                }
                if (seen < 2)
                {
                    report.Fail("seat " + seat.ToString(CultureInfo.InvariantCulture) + " sees " + seen.ToString(CultureInfo.InvariantCulture));
                    return false;
                }
            }
            for (int a = 0; a < 3; a++)
            {
                Mark[] all = a == ParkArena.Mega ? marks : Fill(a, Solids(a));
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].Name == null || !all[i].Name.StartsWith("Landmark_Crown_", StringComparison.Ordinal))
                        continue;
                    float top = all[i].Y + all[i].Sy * 0.5f;
                    if (top < 12f || top > 25f) return false;
                }
            }
            return true;
        }

        static bool Projects(float x, float y, float z, float ex, float ey, float ez, float tx, float ty, float tz, float fov, int w, int h)
        {
            float fx = tx - ex;
            float fy = ty - ey;
            float fz = tz - ez;
            float m = (float)Math.Sqrt(fx * fx + fy * fy + fz * fz);
            if (m < 1e-4f) m = 1f;
            fx /= m; fy /= m; fz /= m;
            float ux0 = -fx * fy;
            float uy0 = 1f - fy * fy;
            float uz0 = -fz * fy;
            float um = (float)Math.Sqrt(ux0 * ux0 + uy0 * uy0 + uz0 * uz0);
            if (um < 1e-4f) um = 1f;
            float ux = ux0 / um;
            float uy = uy0 / um;
            float uz = uz0 / um;
            float rx = uy * fz - uz * fy;
            float ry = uz * fx - ux * fz;
            float rz = ux * fy - uy * fx;
            float dx = x - ex;
            float dy = y - ey;
            float dz = z - ez;
            float cx = dx * rx + dy * ry + dz * rz;
            float cy = dx * ux + dy * uy + dz * uz;
            float cz = dx * fx + dy * fy + dz * fz;
            if (cz < 0.8f) return false;
            float aspect = w / (float)h;
            float tan = (float)Math.Tan(fov * 0.5f * Math.PI / 180.0);
            float ndcX = (cx / cz) / tan / aspect;
            float ndcY = (cy / cz) / tan;
            float px = (ndcX * 0.5f + 0.5f) * (w - 1);
            float py = (0.5f - ndcY * 0.5f) * (h - 1);
            return px >= 8f && py >= 8f && px < w - 8f && py < h - 8f;
        }

        static MegaParkP1Layout.Solid[] Solids(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketParkLayout.BuildSolids();
            if (arena == ParkArena.Stack) return StackYardLayout.BuildSolids();
            return MegaParkP1Layout.BuildSolids();
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
                for (int pal = 0; pal < AccessibilityPalette.Count; pal++)
                {
                    for (int p = 0; p < AccessibilityPalette.Players; p++)
                    {
                        AccessibilityPalette.Player(pal, p, out float pr, out float pg, out float pb);
                        if (AccessibilityPalette.Contrast(r, g, b, pr, pg, pb) < 3f) return false;
                    }
                    AccessibilityPalette.It(pal, out float ir, out float ig, out float ib);
                    if (AccessibilityPalette.Contrast(r, g, b, ir, ig, ib) < 3f) return false;
                }
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
            LocalProfiles.Clear();
            int named = LocalProfiles.Create("Sam");
            if (!LocalProfiles.TrySeat(1, named) || Chip(1, ParkArena.Mega, 20f, 50f) != "Sam · West Yard")
            {
                LocalProfiles.Clear();
                report.Fail("named chip");
                return false;
            }
            LocalProfiles.Clear();
            if (Chip(1, ParkArena.Mega, 20f, 50f) != "P2 · West Yard")
            {
                report.Fail("chip reset");
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

        static void EnsureChips()
        {
            int gen = LocalProfiles.LabelGen;
            if (gen == _chipGen && MegaChips[0] != null) return;
            _chipGen = gen;
            FillChips(MegaChips, MegaNames);
            FillChips(PocketChips, PocketNames);
            FillChips(StackChips, StackNames);
        }

        static void FillChips(string[] all, string[] names)
        {
            for (int s = 0; s < 4; s++)
            {
                string who = LocalProfiles.SeatName(s);
                if (string.IsNullOrEmpty(who)) who = SeatWord[s];
                for (int z = 0; z < names.Length; z++)
                    all[s * names.Length + z] = who + " · " + names[z];
            }
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
