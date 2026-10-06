using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tag.Level
{
    public static partial class StackYardLayout
    {
        struct ZoneBox
        {
            public string Id;
            public float X0, X1, Z0, Z1;
        }

        static readonly ZoneBox[] Zones =
        {
            new ZoneBox { Id = "Yard", X0 = 0f, X1 = 55f, Z0 = 0f, Z1 = 32f },
            new ZoneBox { Id = "Lane", X0 = 55f, X1 = 110f, Z0 = 0f, Z1 = 32f },
            new ZoneBox { Id = "Mid", X0 = 0f, X1 = 55f, Z0 = 32f, Z1 = 70f },
            new ZoneBox { Id = "Roof", X0 = 55f, X1 = 110f, Z0 = 32f, Z1 = 70f },
        };

        public static MegaParkP1Layout.Dress[] BuildDressing()
        {
            var list = new List<MegaParkP1Layout.Dress>(48);
            Dress(list, "Horizon_S", "horizon", 55f, -0.12f, -16f, 140f, 0.2f, 18f);
            Dress(list, "Horizon_N", "horizon", 55f, -0.12f, 86f, 140f, 0.2f, 18f);
            Dress(list, "Horizon_W", "horizon", -16f, -0.12f, 35f, 18f, 0.2f, 100f);
            Dress(list, "Horizon_E", "horizon", 126f, -0.12f, 35f, 18f, 0.2f, 100f);

            Tree(list, "SW", -8f, 12f);
            Tree(list, "W", -8f, 35f);
            Tree(list, "NW", -8f, 58f);
            Tree(list, "SE", 118f, 12f);
            Tree(list, "E", 118f, 35f);
            Tree(list, "NE", 118f, 58f);
            Tree(list, "S0", 28f, -8f);
            Tree(list, "S1", 82f, -8f);
            Tree(list, "N0", 28f, 78f);
            Tree(list, "N1", 82f, 78f);

            Dress(list, "Treeline_S", "leaf", 55f, 7f, -22f, 80f, 12f, 0.35f);
            Dress(list, "Treeline_N", "leaf", 55f, 7f, 92f, 80f, 12f, 0.35f);
            Dress(list, "Treeline_W", "leaf", -22f, 7f, 35f, 0.35f, 12f, 50f);
            Dress(list, "Treeline_E", "leaf", 132f, 7f, 35f, 0.35f, 12f, 50f);

            Dress(list, "Skyline_S", "skyline", 55f, 6f, -30f, 60f, 11f, 0.3f);
            Dress(list, "Skyline_N", "skyline", 55f, 6.5f, 100f, 60f, 12f, 0.3f);
            Dress(list, "Skyline_W", "skyline", -30f, 6f, 35f, 0.3f, 11f, 40f);
            Dress(list, "Skyline_E", "skyline", 140f, 6.5f, 35f, 0.3f, 12f, 40f);

            Bench(list, "S", 55f, -4.2f);
            Bench(list, "N", 55f, 74.2f);
            Lamp(list, "W", -6.5f, 24f);
            Lamp(list, "E", 116.5f, 46f);
            Dress(list, "Trash_S", "trash", 40f, 0.45f, -5.5f, 0.55f, 0.9f, 0.45f);
            Dress(list, "Trash_N", "trash", 70f, 0.45f, 75.5f, 0.55f, 0.9f, 0.45f);
            return list.ToArray();
        }

        static int DressBatches()
        {
            MegaParkP1Layout.Dress[] all = BuildDressing();
            var keys = new HashSet<string>();
            for (int i = 0; i < all.Length; i++)
                keys.Add(all[i].Mat);
            return keys.Count;
        }

        static string LookNote(MegaParkP1Layout.Solid[] solids, StringBuilder fail)
        {
            LandmarkSight(solids, fail);
            DressingSight(fail);
            SunNote(fail);
            if (MegaParkP1Layout.SunPitch < 27f || MegaParkP1Layout.SunPitch > 29f)
                fail.Append("sun pitch; ");
            if (MegaParkP1Layout.SunR < MegaParkP1Layout.SunG + 0.2f
                || MegaParkP1Layout.SunG < MegaParkP1Layout.SunB + 0.15f)
                fail.Append("sun is not warm; ");
            if (MegaParkP1Layout.ShadowStrength < 0.28f || MegaParkP1Layout.ShadowStrength > 0.55f)
                fail.Append("shadow strength; ");
            float fill = ShadeFill();
            if (fill < 0.70f)
                fail.Append("shade hides a ledge; ");
            int horizons = 0;
            MegaParkP1Layout.Dress[] dress = BuildDressing();
            for (int i = 0; i < dress.Length; i++)
                if (dress[i].Name.StartsWith("Horizon_", StringComparison.Ordinal)) horizons++;
            return string.Format(
                CultureInfo.InvariantCulture,
                "landmarks {0} zones visible; horizon {1}; look sun {2:0}deg warm shadow {3:0.00} fill {4:0.00}",
                Zones.Length, horizons, MegaParkP1Layout.SunPitch, MegaParkP1Layout.ShadowStrength, fill);
        }

        static void SunNote(StringBuilder fail)
        {
            string boot = ReadRepoFile("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            if (boot == null
                || boot.IndexOf("ApplyLook()", StringComparison.Ordinal) < 0
                || boot.IndexOf("SunPitch", StringComparison.Ordinal) < 0
                || boot.IndexOf("BuildStack()", StringComparison.Ordinal) < 0
                || boot.IndexOf("StackYardLayout.BuildDressing()", StringComparison.Ordinal) < 0)
                fail.Append("stack sun or dressing is not wired; ");
        }

        static float ShadeFill()
        {
            float sunL = Lum(MegaParkP1Layout.SunR, MegaParkP1Layout.SunG, MegaParkP1Layout.SunB) * MegaParkP1Layout.SunIntensity;
            float ambL = (Lum(MegaParkP1Layout.AmbSkyR, MegaParkP1Layout.AmbSkyG, MegaParkP1Layout.AmbSkyB)
                + Lum(MegaParkP1Layout.AmbEqR, MegaParkP1Layout.AmbEqG, MegaParkP1Layout.AmbEqB)
                + Lum(MegaParkP1Layout.AmbGndR, MegaParkP1Layout.AmbGndG, MegaParkP1Layout.AmbGndB)) / 3f * MegaParkP1Layout.AmbIntensity;
            const float nd = 0.65f;
            float lit = ambL + sunL * nd;
            float shaded = ambL + sunL * nd * (1f - MegaParkP1Layout.ShadowStrength);
            return lit > 0.01f ? shaded / lit : 0f;
        }

        static float Lum(float r, float g, float b)
        {
            return 0.2126f * Lin(r) + 0.7152f * Lin(g) + 0.0722f * Lin(b);
        }

        static float Lin(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            return (float)Math.Pow((c + 0.055f) / 1.055f, 2.4);
        }

        static void LandmarkSight(MegaParkP1Layout.Solid[] solids, StringBuilder fail)
        {
            for (int z = 0; z < Zones.Length; z++)
            {
                if (!ZoneHasFlag(solids, Zones[z].Id))
                    fail.Append(Zones[z].Id).Append(" has no skyline; ");
            }
            int samples = 0;
            int seen = 0;
            for (float x = 8f; x <= 102f; x += 12f)
            {
                for (float z = 8f; z <= 62f; z += 12f)
                {
                    if (Sheltered(solids, x, z)) continue;
                    string zone = ZoneAt(x, z);
                    if (zone == null) continue;
                    samples++;
                    if (Sees(solids, x, z, zone)) seen++;
                    else
                        fail.Append(zone).Append(" hidden at ")
                            .Append(x.ToString("0", CultureInfo.InvariantCulture)).Append(',')
                            .Append(z.ToString("0", CultureInfo.InvariantCulture)).Append("; ");
                }
            }
            float[,] eyes =
            {
                { 55f, 10f },
                { 55f, 60f },
                { 12f, 35f },
                { 98f, 35f },
            };
            for (int e = 0; e < eyes.GetLength(0); e++)
            {
                for (int z = 0; z < Zones.Length; z++)
                {
                    samples++;
                    if (Sees(solids, eyes[e, 0], eyes[e, 1], Zones[z].Id)) seen++;
                    else fail.Append("eye cannot see ").Append(Zones[z].Id).Append("; ");
                }
            }
            if (samples > 0 && seen < samples)
                fail.Append("landmark visibility ").Append(seen.ToString(CultureInfo.InvariantCulture))
                    .Append('/').Append(samples.ToString(CultureInfo.InvariantCulture)).Append("; ");
        }

        static bool ZoneHasFlag(MegaParkP1Layout.Solid[] solids, string zone)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "landmark" && s.Zone == zone && s.Name.EndsWith("_Flag", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static string ZoneAt(float x, float z)
        {
            for (int i = 0; i < Zones.Length; i++)
            {
                ZoneBox b = Zones[i];
                if (x >= b.X0 && x <= b.X1 && z >= b.Z0 && z <= b.Z1) return b.Id;
            }
            return null;
        }

        static bool Sheltered(MegaParkP1Layout.Solid[] solids, float x, float z)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "landmark" || s.Kind == "mark") continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top < 2.2f) continue;
                if (DistTo(s, x, z) < 2.2f) return true;
            }
            return false;
        }

        static bool Sees(MegaParkP1Layout.Solid[] solids, float x, float z, string zone)
        {
            MegaParkP1Layout.Solid crown = default;
            bool found = false;
            float best = float.MaxValue;
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind != "landmark" || s.Zone != zone || !s.Name.EndsWith("_Flag", StringComparison.Ordinal))
                    continue;
                float d = Dist(x, z, s.X, s.Z);
                if (d < best)
                {
                    best = d;
                    crown = s;
                    found = true;
                }
            }
            if (!found) return false;
            float top = crown.Y + crown.Sy * 0.5f - 0.4f;
            return FirstIsLandmark(solids, x, 1.7f, z, crown.X, top, crown.Z, zone);
        }

        static bool FirstIsLandmark(MegaParkP1Layout.Solid[] solids, float x0, float y0, float z0, float x1, float y1, float z1, string zone)
        {
            float best = 2f;
            string who = null;
            string whoZone = null;
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "mark") continue;
                if (!Hit(s, x0, y0, z0, x1, y1, z1, out float t)) continue;
                if (t < 0.02f || t >= best) continue;
                best = t;
                who = s.Name;
                whoZone = s.Zone;
            }
            return who != null && whoZone == zone && who.StartsWith("Landmark_", StringComparison.Ordinal);
        }

        static bool Hit(MegaParkP1Layout.Solid s, float x0, float y0, float z0, float x1, float y1, float z1, out float tHit)
        {
            tHit = 2f;
            float t0 = 0f;
            float t1 = 1f;
            if (!Slab(x0, x1, s.X - s.Sx * 0.5f, s.X + s.Sx * 0.5f, ref t0, ref t1)) return false;
            if (!Slab(y0, y1, s.Y - s.Sy * 0.5f, s.Y + s.Sy * 0.5f, ref t0, ref t1)) return false;
            if (!Slab(z0, z1, s.Z - s.Sz * 0.5f, s.Z + s.Sz * 0.5f, ref t0, ref t1)) return false;
            tHit = t0;
            return true;
        }

        static bool Slab(float p0, float p1, float min, float max, ref float t0, ref float t1)
        {
            float d = p1 - p0;
            if (Math.Abs(d) < 1e-6f)
                return p0 >= min && p0 <= max;
            float a = (min - p0) / d;
            float b = (max - p0) / d;
            if (a > b) { float s = a; a = b; b = s; }
            if (a > t0) t0 = a;
            if (b < t1) t1 = b;
            return t0 <= t1;
        }

        static float DistTo(MegaParkP1Layout.Solid s, float x, float z)
        {
            float dx = Math.Abs(x - s.X) - s.Sx * 0.5f;
            float dz = Math.Abs(z - s.Z) - s.Sz * 0.5f;
            if (dx < 0f) dx = 0f;
            if (dz < 0f) dz = 0f;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static void DressingSight(StringBuilder fail)
        {
            MegaParkP1Layout.Dress[] all = BuildDressing();
            int trees = 0;
            int horizons = 0;
            for (int i = 0; i < all.Length; i++)
            {
                MegaParkP1Layout.Dress d = all[i];
                if (!MegaParkP1Layout.TryLook(d.Mat, out _, out _, out _, out _, out _))
                    fail.Append(d.Name).Append(" has no material; ");
                if (OverlapsPlay(d))
                    fail.Append(d.Name).Append(" is inside the fence; ");
                float gap = OutsideGap(d);
                if (gap < 0.40f)
                    fail.Append(d.Name).Append(" clips the fence; ");
                if (d.Name.StartsWith("Tree_", StringComparison.Ordinal) && d.Name.EndsWith("_Trunk", StringComparison.Ordinal))
                {
                    trees++;
                    if (gap < 2f)
                        fail.Append(d.Name).Append(" is not beyond the fence; ");
                }
                if (d.Name.StartsWith("Horizon_", StringComparison.Ordinal)) horizons++;
            }
            if (trees < 8 || horizons < 4)
                fail.Append("dressing is thin; ");
        }

        static bool OverlapsPlay(MegaParkP1Layout.Dress d)
        {
            float x0 = d.X - d.Sx * 0.5f;
            float x1 = d.X + d.Sx * 0.5f;
            float z0 = d.Z - d.Sz * 0.5f;
            float z1 = d.Z + d.Sz * 0.5f;
            return x1 > 0.05f && x0 < MapW - 0.05f && z1 > 0.05f && z0 < MapD - 0.05f;
        }

        static float OutsideGap(MegaParkP1Layout.Dress d)
        {
            float x0 = d.X - d.Sx * 0.5f;
            float x1 = d.X + d.Sx * 0.5f;
            float z0 = d.Z - d.Sz * 0.5f;
            float z1 = d.Z + d.Sz * 0.5f;
            float dx = 0f;
            float dz = 0f;
            if (x1 < 0f) dx = -x1;
            else if (x0 > MapW) dx = x0 - MapW;
            if (z1 < 0f) dz = -z1;
            else if (z0 > MapD) dz = z0 - MapD;
            if (dx == 0f && dz == 0f && OverlapsPlay(d)) return 0f;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static void Tree(List<MegaParkP1Layout.Dress> list, string id, float x, float z)
        {
            Dress(list, "Tree_" + id + "_Trunk", "bark", x, 2.1f, z, 0.55f, 4.2f, 0.55f);
            Dress(list, "Tree_" + id + "_Leaf", "leaf", x, 5.4f, z, 3.1f, 2.8f, 3.1f);
        }

        static void Bench(List<MegaParkP1Layout.Dress> list, string id, float x, float z)
        {
            Dress(list, "Bench_" + id + "_Seat", "wood", x, 0.42f, z, 1.6f, 0.08f, 0.42f);
            Dress(list, "Bench_" + id + "_Back", "wood", x, 0.78f, z, 1.6f, 0.48f, 0.08f);
            Dress(list, "Bench_" + id + "_LegL", "wood", x - 0.62f, 0.2f, z, 0.08f, 0.4f, 0.32f);
            Dress(list, "Bench_" + id + "_LegR", "wood", x + 0.62f, 0.2f, z, 0.08f, 0.4f, 0.32f);
        }

        static void Lamp(List<MegaParkP1Layout.Dress> list, string id, float x, float z)
        {
            Dress(list, "Lamp_" + id + "_Pole", "lamp", x, 1.6f, z, 0.16f, 3.2f, 0.16f);
            Dress(list, "Lamp_" + id + "_Head", "lamp", x, 3.3f, z, 0.55f, 0.28f, 0.55f);
        }

        static void Dress(List<MegaParkP1Layout.Dress> list, string name, string mat, float x, float y, float z, float sx, float sy, float sz)
        {
            list.Add(new MegaParkP1Layout.Dress
            {
                Name = name,
                Mat = mat,
                X = x, Y = y, Z = z,
                Sx = sx, Sy = sy, Sz = sz,
                Decal = false,
            });
        }

        static void WriteStills(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, StringBuilder fail)
        {
            ArenaStill.WritePair("StackYard", solids, ramps, BuildDressing(), LaunchPads, ZipLines, Spawns[0], MapW, MapD, fail);
        }

        static string ReadRepoFile(string relative)
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string path = Path.Combine(dir, relative);
                if (File.Exists(path)) return File.ReadAllText(path);
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return null;
        }
    }
}
