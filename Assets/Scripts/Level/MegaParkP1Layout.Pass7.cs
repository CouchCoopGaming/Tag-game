using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Pass 7 look: one warm afternoon sun, playground albedos, and dressing that
    /// stays outside the fence or lies flat on the ground. Gameplay tints are the
    /// pass 6 hexes. Feel locks are not stored here.
    /// </summary>
    public static partial class MegaParkP1Layout
    {
        public const float SunPitch = 28f;
        public const float SunYaw = -48f;
        public const float SunR = 1f;
        public const float SunG = 0.58f;
        public const float SunB = 0.28f;
        public const float SunIntensity = 1.25f;
        public const float ShadowStrength = 0.40f;
        public const float AmbSkyR = 0.55f;
        public const float AmbSkyG = 0.62f;
        public const float AmbSkyB = 0.82f;
        public const float AmbEqR = 0.85f;
        public const float AmbEqG = 0.62f;
        public const float AmbEqB = 0.42f;
        public const float AmbGndR = 0.28f;
        public const float AmbGndG = 0.20f;
        public const float AmbGndB = 0.14f;
        public const float AmbIntensity = 1.15f;
        public const int DrawCap = 120;
        /// <summary>One corner texture. Counted in the draw cap with the park.</summary>
        public const int MinimapDraws = 1;

        struct Swatch
        {
            public string Name;
            public float R, G, B, Smooth, Metal;
        }

        // Gameplay hexes stay: cling #3D7EFF, slide #F5D547, plate orange, zip #D946EF.
        // Tag-back aqua-cyan is read from TagBackGlow. Décor is darkened so those
        // tints clear 3:1 against the surfaces they sit on, and décor-to-décor stays quieter.
        static readonly Swatch[] Swatches =
        {
            new Swatch { Name = "cling", R = 0x3D / 255f, G = 0x7E / 255f, B = 0xFF / 255f, Smooth = 0.24f, Metal = 0f },
            new Swatch { Name = "slide", R = 0xF5 / 255f, G = 0xD5 / 255f, B = 0x47 / 255f, Smooth = 0.66f, Metal = 0f },
            new Swatch { Name = "plate", R = 1f, G = 0.42f, B = 0.05f, Smooth = 0.42f, Metal = 0.40f },
            new Swatch { Name = "zip", R = 0xD9 / 255f, G = 0x46 / 255f, B = 0xEF / 255f, Smooth = 0.35f, Metal = 0f },
            new Swatch { Name = "tag", R = 0.20f, G = 1f, B = 0.92f, Smooth = 0f, Metal = 0f },
            new Swatch { Name = "mulch", R = 0x3A / 255f, G = 0x22 / 255f, B = 0x18 / 255f, Smooth = 0.04f, Metal = 0f },
            new Swatch { Name = "sand", R = 0x7A / 255f, G = 0x62 / 255f, B = 0x40 / 255f, Smooth = 0.05f, Metal = 0f },
            new Swatch { Name = "rubber", R = 0x2C / 255f, G = 0x2A / 255f, B = 0x28 / 255f, Smooth = 0.12f, Metal = 0f },
            new Swatch { Name = "steel", R = 0x32 / 255f, G = 0x3C / 255f, B = 0x48 / 255f, Smooth = 0.48f, Metal = 0.55f },
            new Swatch { Name = "concrete", R = 0x6A / 255f, G = 0x61 / 255f, B = 0x54 / 255f, Smooth = 0.08f, Metal = 0f },
            new Swatch { Name = "cedar", R = 0x64 / 255f, G = 0x40 / 255f, B = 0x2E / 255f, Smooth = 0.16f, Metal = 0f },
            new Swatch { Name = "bark", R = 0x3A / 255f, G = 0x2A / 255f, B = 0x1E / 255f, Smooth = 0.05f, Metal = 0f },
            new Swatch { Name = "rim", R = 0x5A / 255f, G = 0x3E / 255f, B = 0x32 / 255f, Smooth = 0.12f, Metal = 0f },
            new Swatch { Name = "field", R = 0x2C / 255f, G = 0x64 / 255f, B = 0x40 / 255f, Smooth = 0.06f, Metal = 0f },
            new Swatch { Name = "grass", R = 0x3A / 255f, G = 0x64 / 255f, B = 0x32 / 255f, Smooth = 0.05f, Metal = 0f },
            new Swatch { Name = "soft", R = 0x70 / 255f, G = 0x44 / 255f, B = 0x30 / 255f, Smooth = 0.28f, Metal = 0f },
            new Swatch { Name = "pad", R = 0x12 / 255f, G = 0x32 / 255f, B = 0x38 / 255f, Smooth = 0.20f, Metal = 0f },
            new Swatch { Name = "merry", R = 0x64 / 255f, G = 0x2C / 255f, B = 0x42 / 255f, Smooth = 0.30f, Metal = 0f },
            new Swatch { Name = "amber", R = 0x40 / 255f, G = 0x28 / 255f, B = 0x14 / 255f, Smooth = 0.22f, Metal = 0f },
            new Swatch { Name = "swing", R = 0x2C / 255f, G = 0x3A / 255f, B = 0x1C / 255f, Smooth = 0.18f, Metal = 0f },
            new Swatch { Name = "army", R = 0x2C / 255f, G = 0x30 / 255f, B = 0x16 / 255f, Smooth = 0.20f, Metal = 0f },
            new Swatch { Name = "knight", R = 0x34 / 255f, G = 0x28 / 255f, B = 0x44 / 255f, Smooth = 0.20f, Metal = 0f },
            new Swatch { Name = "kick", R = 0x62 / 255f, G = 0x3C / 255f, B = 0x2C / 255f, Smooth = 0.15f, Metal = 0f },
            new Swatch { Name = "hop", R = 0x34 / 255f, G = 0x50 / 255f, B = 0x68 / 255f, Smooth = 0.25f, Metal = 0f },
            new Swatch { Name = "cover", R = 0x66 / 255f, G = 0x5C / 255f, B = 0x44 / 255f, Smooth = 0.10f, Metal = 0f },
            new Swatch { Name = "fence", R = 0x3A / 255f, G = 0x46 / 255f, B = 0x3C / 255f, Smooth = 0.38f, Metal = 0.50f },
            new Swatch { Name = "horizon", R = 0x46 / 255f, G = 0x60 / 255f, B = 0x38 / 255f, Smooth = 0.06f, Metal = 0f },
            new Swatch { Name = "leaf", R = 0x2C / 255f, G = 0x54 / 255f, B = 0x30 / 255f, Smooth = 0.08f, Metal = 0f },
            new Swatch { Name = "wood", R = 0x64 / 255f, G = 0x46 / 255f, B = 0x2E / 255f, Smooth = 0.18f, Metal = 0f },
            new Swatch { Name = "lamp", R = 0x46 / 255f, G = 0x3C / 255f, B = 0x34 / 255f, Smooth = 0.40f, Metal = 0.45f },
            new Swatch { Name = "trash", R = 0x36 / 255f, G = 0x3A / 255f, B = 0x36 / 255f, Smooth = 0.22f, Metal = 0.30f },
            new Swatch { Name = "skyline", R = 0x56 / 255f, G = 0x4C / 255f, B = 0x44 / 255f, Smooth = 0.05f, Metal = 0f },
            new Swatch { Name = "sky", R = 0xFF / 255f, G = 0xF3 / 255f, B = 0xD6 / 255f, Smooth = 0f, Metal = 0f },
        };

        public struct Dress
        {
            public string Name;
            public string Mat;
            public float X, Y, Z, Sx, Sy, Sz;
            public bool Decal;
        }

        public static bool TryLook(string name, out float r, out float g, out float b, out float smooth, out float metal)
        {
            if (name == "blue") name = "cling";
            if (name == "yellow") name = "slide";
            for (int i = 0; i < Swatches.Length; i++)
            {
                if (Swatches[i].Name != name) continue;
                r = Swatches[i].R;
                g = Swatches[i].G;
                b = Swatches[i].B;
                smooth = Swatches[i].Smooth;
                metal = Swatches[i].Metal;
                return true;
            }
            r = g = b = 0.5f;
            smooth = 0.06f;
            metal = 0f;
            return false;
        }

        public static Dress[] BuildDressing()
        {
            var list = new List<Dress>(180);
            Horizon(list);
            Trees(list);
            TreeLine(list);
            Skyline(list);
            Benches(list);
            Lamps(list);
            Cans(list);
            Sign(list);
            Tufts(list);
            return list.ToArray();
        }

        static void Horizon(List<Dress> list)
        {
            // Flush with the collar's outer edge. Tops match the collar so the void stops at the fence.
            AddDress(list, "Horizon_S", "horizon", 80f, -0.12f, -21.5f, 240f, 0.2f, 37f, false);
            AddDress(list, "Horizon_N", "horizon", 80f, -0.12f, 121.5f, 240f, 0.2f, 37f, false);
            AddDress(list, "Horizon_W", "horizon", -21.5f, -0.12f, 50f, 37f, 0.2f, 106f, false);
            AddDress(list, "Horizon_E", "horizon", 181.5f, -0.12f, 50f, 37f, 0.2f, 106f, false);
        }

        static void Trees(List<Dress> list)
        {
            float[] along = { 14f, 38f, 62f, 86f, 110f, 134f, 156f };
            for (int i = 0; i < along.Length; i++)
            {
                Tree(list, "S" + i.ToString(CultureInfo.InvariantCulture), along[i], -9f);
                Tree(list, "N" + i.ToString(CultureInfo.InvariantCulture), along[i], 109f);
            }
            float[] side = { 18f, 42f, 66f, 86f };
            for (int i = 0; i < side.Length; i++)
            {
                Tree(list, "W" + i.ToString(CultureInfo.InvariantCulture), -9f, side[i]);
                Tree(list, "E" + i.ToString(CultureInfo.InvariantCulture), 169f, side[i]);
            }
        }

        static void Tree(List<Dress> list, string id, float x, float z)
        {
            AddDress(list, "Tree_" + id + "_Trunk", "bark", x, 2.1f, z, 0.55f, 4.2f, 0.55f, false);
            AddDress(list, "Tree_" + id + "_Leaf", "leaf", x, 5.4f, z, 3.1f, 2.8f, 3.1f, false);
        }

        static void TreeLine(List<Dress> list)
        {
            float[] xs = { 20f, 60f, 100f, 140f };
            for (int i = 0; i < xs.Length; i++)
            {
                string n = i.ToString(CultureInfo.InvariantCulture);
                AddDress(list, "Treeline_S" + n, "leaf", xs[i], 8f, -28f, 34f, 16f, 0.35f, false);
                AddDress(list, "Treeline_N" + n, "leaf", xs[i], 8f, 128f, 34f, 16f, 0.35f, false);
            }
            float[] zs = { 22f, 50f, 78f };
            for (int i = 0; i < zs.Length; i++)
            {
                string n = i.ToString(CultureInfo.InvariantCulture);
                AddDress(list, "Treeline_W" + n, "leaf", -28f, 8f, zs[i], 0.35f, 16f, 26f, false);
                AddDress(list, "Treeline_E" + n, "leaf", 188f, 8f, zs[i], 0.35f, 16f, 26f, false);
            }
        }

        static void Skyline(List<Dress> list)
        {
            AddDress(list, "Skyline_S0", "skyline", 40f, 6.5f, -38f, 52f, 12f, 0.3f, false);
            AddDress(list, "Skyline_S1", "skyline", 120f, 7.5f, -38f, 56f, 14f, 0.3f, false);
            AddDress(list, "Skyline_N0", "skyline", 48f, 6.5f, 138f, 52f, 12f, 0.3f, false);
            AddDress(list, "Skyline_N1", "skyline", 118f, 8f, 138f, 60f, 15f, 0.3f, false);
            AddDress(list, "Skyline_W0", "skyline", -38f, 6f, 30f, 0.3f, 11f, 36f, false);
            AddDress(list, "Skyline_W1", "skyline", -38f, 7f, 72f, 0.3f, 13f, 40f, false);
            AddDress(list, "Skyline_E0", "skyline", 198f, 6.5f, 28f, 0.3f, 12f, 36f, false);
            AddDress(list, "Skyline_E1", "skyline", 198f, 7.5f, 70f, 0.3f, 14f, 40f, false);
        }

        static void Benches(List<Dress> list)
        {
            Bench(list, "S0", 22f, -1.5f, -1f);
            Bench(list, "S1", 58f, -1.5f, -1f);
            Bench(list, "S2", 98f, -1.5f, -1f);
            Bench(list, "S3", 138f, -1.5f, -1f);
            Bench(list, "N0", 30f, 101.5f, 1f);
            Bench(list, "N1", 74f, 101.5f, 1f);
            Bench(list, "N2", 118f, 101.5f, 1f);
            Bench(list, "N3", 150f, 101.5f, 1f);
        }

        static void Bench(List<Dress> list, string id, float x, float z, float outSign)
        {
            AddDress(list, "Bench_" + id + "_Seat", "wood", x, 0.42f, z, 1.5f, 0.08f, 0.42f, false);
            AddDress(list, "Bench_" + id + "_Back", "wood", x, 0.78f, z + outSign * 0.22f, 1.5f, 0.48f, 0.08f, false);
            AddDress(list, "Bench_" + id + "_LegL", "wood", x - 0.62f, 0.2f, z, 0.08f, 0.4f, 0.32f, false);
            AddDress(list, "Bench_" + id + "_LegR", "wood", x + 0.62f, 0.2f, z, 0.08f, 0.4f, 0.32f, false);
        }

        static void Lamps(List<Dress> list)
        {
            Lamp(list, "S0", 40f, -2.15f);
            Lamp(list, "S1", 80f, -2.15f);
            Lamp(list, "S2", 120f, -2.15f);
            Lamp(list, "N0", 52f, 102.15f);
            Lamp(list, "N1", 104f, 102.15f);
            Lamp(list, "W0", -2.15f, 28f);
        }

        static void Lamp(List<Dress> list, string id, float x, float z)
        {
            AddDress(list, "Lamp_" + id + "_Pole", "lamp", x, 1.6f, z, 0.14f, 3.2f, 0.14f, false);
            AddDress(list, "Lamp_" + id + "_Head", "lamp", x, 3.28f, z, 0.5f, 0.12f, 0.5f, false);
        }

        static void Cans(List<Dress> list)
        {
            AddDress(list, "Trash_S0", "trash", 36f, 0.38f, -1.15f, 0.42f, 0.76f, 0.42f, false);
            AddDress(list, "Trash_S1", "trash", 76f, 0.38f, -1.15f, 0.42f, 0.76f, 0.42f, false);
            AddDress(list, "Trash_S2", "trash", 116f, 0.38f, -1.15f, 0.42f, 0.76f, 0.42f, false);
            AddDress(list, "Trash_N0", "trash", 46f, 0.38f, 101.15f, 0.42f, 0.76f, 0.42f, false);
            AddDress(list, "Trash_N1", "trash", 96f, 0.38f, 101.15f, 0.42f, 0.76f, 0.42f, false);
        }

        static void Sign(List<Dress> list)
        {
            // West of the fence, in line with the SW kickoff pad.
            AddDress(list, "Sign_Kickoff_PostL", "wood", -1.9f, 0.95f, 7.5f, 0.12f, 1.9f, 0.12f, false);
            AddDress(list, "Sign_Kickoff_PostR", "wood", -1.9f, 0.95f, 8.5f, 0.12f, 1.9f, 0.12f, false);
            AddDress(list, "Sign_Kickoff_Board", "wood", -1.74f, 1.65f, 8f, 0.1f, 0.85f, 1.5f, false);
        }

        static void Tufts(List<Dress> list)
        {
            // Flat decals on open mulch. No collider. Off the bowl, the rim slot, and the covers.
            AddDress(list, "Tuft_01", "grass", 18f, 0.02f, 44f, 0.6f, 0.02f, 0.4f, true);
            AddDress(list, "Tuft_02", "grass", 26f, 0.02f, 12f, 0.5f, 0.02f, 0.35f, true);
            AddDress(list, "Tuft_03", "grass", 90f, 0.02f, 8.8f, 0.55f, 0.02f, 0.4f, true);
            AddDress(list, "Tuft_04", "grass", 140f, 0.02f, 5.2f, 0.5f, 0.02f, 0.35f, true);
            AddDress(list, "Tuft_05", "grass", 16f, 0.02f, 88f, 0.55f, 0.02f, 0.4f, true);
            AddDress(list, "Tuft_06", "grass", 108f, 0.02f, 94f, 0.5f, 0.02f, 0.35f, true);
            AddDress(list, "Tuft_07", "grass", 70f, 0.02f, 22f, 0.6f, 0.02f, 0.4f, true);
            AddDress(list, "Tuft_08", "grass", 132f, 0.02f, 96f, 0.5f, 0.02f, 0.35f, true);
        }

        static void AddDress(List<Dress> list, string name, string mat, float x, float y, float z, float sx, float sy, float sz, bool decal)
        {
            list.Add(new Dress
            {
                Name = name,
                Mat = mat,
                X = x,
                Y = y,
                Z = z,
                Sx = sx,
                Sy = sy,
                Sz = sz,
                Decal = decal,
            });
        }

        static int DressPieceCount()
        {
            return BuildDressing().Length;
        }

        static int DressBatchCount()
        {
            Dress[] all = BuildDressing();
            var keys = new HashSet<string>();
            for (int i = 0; i < all.Length; i++)
                keys.Add(all[i].Mat);
            return keys.Count;
        }

        static string LookReport(Solid[] solids, Ramp[] ramps, StringBuilder fail)
        {
            string boot = ReadText("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            if (boot == null
                || boot.IndexOf("SunPitch", StringComparison.Ordinal) < 0
                || boot.IndexOf("ShadowStrength", StringComparison.Ordinal) < 0
                || boot.IndexOf("LightType.Directional", StringComparison.Ordinal) < 0
                || boot.IndexOf("LightShadows.Soft", StringComparison.Ordinal) < 0
                || boot.IndexOf("LightmapBakeType.Mixed", StringComparison.Ordinal) < 0
                || boot.IndexOf("Skybox/Procedural", StringComparison.Ordinal) < 0
                || boot.IndexOf("AmbientMode.Trilight", StringComparison.Ordinal) < 0
                || boot.IndexOf("l.enabled = false", StringComparison.Ordinal) < 0
                || boot.IndexOf("BuildDressing", StringComparison.Ordinal) < 0
                || boot.IndexOf("StripCollider", StringComparison.Ordinal) < 0)
                fail.Append("afternoon sun or dressing is not wired; ");

            if (SunPitch < 22f || SunPitch > 36f)
                fail.Append("sun pitch is not late afternoon; ");
            if (SunR < SunG + 0.2f || SunG < SunB + 0.15f)
                fail.Append("sun is not warm; ");
            if (ShadowStrength < 0.28f || ShadowStrength > 0.55f)
                fail.Append("shadow strength hides a face or drops the sun; ");

            float sunL = Lum(SunR, SunG, SunB) * SunIntensity;
            float ambL = (Lum(AmbSkyR, AmbSkyG, AmbSkyB) + Lum(AmbEqR, AmbEqG, AmbEqB) + Lum(AmbGndR, AmbGndG, AmbGndB)) / 3f * AmbIntensity;
            const float nd = 0.65f;
            float lit = ambL + sunL * nd;
            float shaded = ambL + sunL * nd * (1f - ShadowStrength);
            float fill = lit > 0.01f ? shaded / lit : 0f;
            float away = lit > 0.01f ? ambL / lit : 0f;
            if (fill < 0.70f || away < 0.40f)
                fail.Append("shade is dark enough to hide a ledge; ");

            var pairs = new string[]
            {
                "cling", "mulch", "cling", "pad",
                "slide", "amber", "slide", "army", "slide", "knight", "slide", "mulch",
                "plate", "amber", "plate", "swing", "plate", "knight", "plate", "army", "plate", "pad", "plate", "merry", "plate", "steel",
                "zip", "sky", "zip", "amber", "zip", "army", "zip", "knight",
                "tag", "mulch", "tag", "sand", "tag", "grass", "tag", "concrete", "tag", "field", "tag", "cover",
            };
            var bits = new StringBuilder();
            float worst = 99f;
            for (int i = 0; i < pairs.Length; i += 2)
            {
                float cr = PairContrast(pairs[i], pairs[i + 1]);
                if (cr < worst) worst = cr;
                if (bits.Length > 0) bits.Append(' ');
                bits.Append(pairs[i]).Append('/').Append(pairs[i + 1]).Append(' ')
                    .Append(cr.ToString("0.00", CultureInfo.InvariantCulture));
                if (cr < 3f)
                    fail.Append(pairs[i]).Append(" vs ").Append(pairs[i + 1]).Append(" contrast ")
                        .Append(cr.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
            }

            float decorMax = 1f;
            string decorWho = "decor";
            for (int a = 0; a < Swatches.Length; a++)
            {
                if (GameplayOrSky(Swatches[a].Name)) continue;
                for (int b = a + 1; b < Swatches.Length; b++)
                {
                    if (GameplayOrSky(Swatches[b].Name)) continue;
                    float cr = Contrast(Swatches[a].R, Swatches[a].G, Swatches[a].B, Swatches[b].R, Swatches[b].G, Swatches[b].B);
                    if (cr > decorMax)
                    {
                        decorMax = cr;
                        decorWho = Swatches[a].Name + "/" + Swatches[b].Name;
                    }
                }
            }
            if (worst <= decorMax)
                fail.Append("gameplay contrast ").Append(worst.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" is not above decor ").Append(decorWho).Append(' ')
                    .Append(decorMax.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");

            DressReport(solids, fail);
            int draws = BatchCount(solids, true) + BatchCountRamps(ramps) + 12 + 7 + 35 + DressBatchCount() + MinimapDraws;
            return string.Format(
                CultureInfo.InvariantCulture,
                "look sun {0:0}deg warm shadow {1:0.00} fill {2:0.00}; contrast {3}; decor max {4:0.00}; draws {5}",
                SunPitch, ShadowStrength, fill, bits, decorMax, draws);
        }

        static bool GameplayOrSky(string name)
        {
            return name == "cling" || name == "slide" || name == "plate" || name == "zip" || name == "tag" || name == "sky";
        }

        public static float SwatchContrast(string a, string b)
        {
            return PairContrast(a, b);
        }

        static float PairContrast(string a, string b)
        {
            Swatch sa = default, sb = default;
            bool fa = false, fb = false;
            for (int i = 0; i < Swatches.Length; i++)
            {
                if (Swatches[i].Name == a) { sa = Swatches[i]; fa = true; }
                if (Swatches[i].Name == b) { sb = Swatches[i]; fb = true; }
            }
            if (!fa || !fb) return 1f;
            return Contrast(sa.R, sa.G, sa.B, sb.R, sb.G, sb.B);
        }

        static void DressReport(Solid[] solids, StringBuilder fail)
        {
            Dress[] all = BuildDressing();
            int benches = 0, trees = 0, lamps = 0, cans = 0, tufts = 0, horizons = 0, lines = 0, cards = 0, signs = 0;
            for (int i = 0; i < all.Length; i++)
            {
                Dress d = all[i];
                if (!TryLook(d.Mat, out _, out _, out _, out _, out _))
                    fail.Append(d.Name).Append(" has no material; ");
                bool inside = HitsPlay(d);
                if (inside && (!d.Decal || d.Sy > 0.03f || d.Y + d.Sy * 0.5f > 0.08f))
                    fail.Append(d.Name).Append(" is touchable inside the fence; ");
                if (!inside && d.Decal)
                    fail.Append(d.Name).Append(" decal left the play area; ");
                float gap = OutsideGap(d);
                if (!d.Decal && gap < 0.40f)
                    fail.Append(d.Name).Append(" clips the fence; ");
                if (d.Name.StartsWith("Tree_", StringComparison.Ordinal) && gap < 2f)
                    fail.Append(d.Name).Append(" is not beyond the fence; ");
                if (HitsBox(d, 52f, 72f, 58f, 76f) && !d.Decal)
                    fail.Append(d.Name).Append(" blocks the rim view slot; ");
                if (HitsBox(d, 52f, 72f, 40f, 58f) && !d.Decal)
                    fail.Append(d.Name).Append(" blocks the bowl; ");
                if (d.Name.StartsWith("Bench_", StringComparison.Ordinal)) benches++;
                if (d.Name.StartsWith("Tree_", StringComparison.Ordinal) && d.Name.EndsWith("_Trunk", StringComparison.Ordinal)) trees++;
                if (d.Name.StartsWith("Lamp_", StringComparison.Ordinal) && d.Name.EndsWith("_Pole", StringComparison.Ordinal)) lamps++;
                if (d.Name.StartsWith("Trash_", StringComparison.Ordinal)) cans++;
                if (d.Name.StartsWith("Tuft_", StringComparison.Ordinal)) tufts++;
                if (d.Name.StartsWith("Horizon_", StringComparison.Ordinal)) horizons++;
                if (d.Name.StartsWith("Treeline_", StringComparison.Ordinal)) lines++;
                if (d.Name.StartsWith("Skyline_", StringComparison.Ordinal)) cards++;
                if (d.Name.StartsWith("Sign_Kickoff", StringComparison.Ordinal)) signs++;
                if (d.Decal && HitsCover(solids, d))
                    fail.Append(d.Name).Append(" sits on a sightline cover; ");
            }
            if (benches < 8 || trees < 12 || lamps < 4 || cans < 4 || tufts < 6 || horizons < 4 || lines < 8 || cards < 4 || signs < 1)
                fail.Append("dressing is thin; ");
            bool signNear = false;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Name != "Sign_Kickoff_Board") continue;
                float dx = all[i].X - SpawnSwX;
                float dz = all[i].Z - SpawnSwZ;
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                signNear = dist >= 6f && dist <= 14f && all[i].X < 0f;
            }
            if (!signNear)
                fail.Append("park sign is not at kickoff; ");
        }

        static bool HitsPlay(Dress d)
        {
            float x0 = d.X - d.Sx * 0.5f;
            float x1 = d.X + d.Sx * 0.5f;
            float z0 = d.Z - d.Sz * 0.5f;
            float z1 = d.Z + d.Sz * 0.5f;
            return x1 > 0.02f && x0 < MapW - 0.02f && z1 > 0.02f && z0 < MapD - 0.02f;
        }

        static float OutsideGap(Dress d)
        {
            float x0 = d.X - d.Sx * 0.5f;
            float x1 = d.X + d.Sx * 0.5f;
            float z0 = d.Z - d.Sz * 0.5f;
            float z1 = d.Z + d.Sz * 0.5f;
            bool outX = x1 < 0f || x0 > MapW;
            bool outZ = z1 < 0f || z0 > MapD;
            float dx = x1 < 0f ? -x1 : (x0 > MapW ? x0 - MapW : 0f);
            float dz = z1 < 0f ? -z1 : (z0 > MapD ? z0 - MapD : 0f);
            if (outX && outZ) return (float)Math.Sqrt(dx * dx + dz * dz);
            if (outX) return dx;
            if (outZ) return dz;
            return 0f;
        }

        static bool HitsBox(Dress d, float x0, float x1, float z0, float z1)
        {
            float minX = d.X - d.Sx * 0.5f;
            float maxX = d.X + d.Sx * 0.5f;
            float minZ = d.Z - d.Sz * 0.5f;
            float maxZ = d.Z + d.Sz * 0.5f;
            return minX < x1 && maxX > x0 && minZ < z1 && maxZ > z0;
        }

        static bool HitsCover(Solid[] solids, Dress d)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (!s.Name.StartsWith("Cover_", StringComparison.Ordinal)) continue;
                if (HitsBox(d, s.X - s.Sx * 0.5f, s.X + s.Sx * 0.5f, s.Z - s.Sz * 0.5f, s.Z + s.Sz * 0.5f))
                    return true;
            }
            return false;
        }

        static void PlaySliceReport(StringBuilder fail)
        {
            string scene = ReadText("Assets/Scenes/Play.unity");
            if (scene == null
                || scene.IndexOf("m_Name: Player", StringComparison.Ordinal) < 0
                || scene.IndexOf("m_Name: DummyRunner", StringComparison.Ordinal) < 0
                || scene.IndexOf("m_Name: MegaParkP1Host", StringComparison.Ordinal) < 0
                || scene.IndexOf("5e3899b98cbe4c54796a0b45175db003", StringComparison.Ordinal) < 0
                || scene.IndexOf("c8e4a1b27f0d4e6a9b3c5d7e1f2a4b71", StringComparison.Ordinal) < 0
                || scene.IndexOf("c8e4a1b27f0d4e6a9b3c5d7e1f2a4b72", StringComparison.Ordinal) < 0
                || scene.IndexOf("5d9a187f5dd14f79baa15fdefd908aba", StringComparison.Ordinal) >= 0)
                fail.Append("play scene is not the solo mega park slice; ");

            string entry = ReadText("Assets/Scripts/Level/PlayEntry.cs");
            if (entry == null
                || entry.IndexOf("CutArenaBootstrap", StringComparison.Ordinal) < 0
                || entry.IndexOf("Built", StringComparison.Ordinal) < 0
                || entry.IndexOf("SpeedEnergyHUD", StringComparison.Ordinal) < 0)
                fail.Append("campus fallback or hud is not wired; ");

            string mini = ReadText("Assets/Scripts/Level/ParkMinimap.cs");
            if (mini == null
                || mini.IndexOf("TexSize = 256", StringComparison.Ordinal) < 0
                || mini.IndexOf("GUI.DrawTexture", StringComparison.Ordinal) < 0
                || mini.IndexOf("KeyCode.M", StringComparison.Ordinal) < 0
                || mini.IndexOf("JoystickButton6", StringComparison.Ordinal) < 0
                || mini.IndexOf("new Camera", StringComparison.Ordinal) >= 0
                || mini.IndexOf("AddComponent<Camera>", StringComparison.Ordinal) >= 0)
                fail.Append("minimap is not one corner texture; ");

            string map = ReadText("Assets/Scripts/Input/TagInputActions.cs");
            if (map == null
                || map.IndexOf("\"Minimap\"", StringComparison.Ordinal) < 0
                || map.IndexOf("<Keyboard>/m", StringComparison.Ordinal) < 0
                || map.IndexOf("<Gamepad>/select", StringComparison.Ordinal) < 0)
                fail.Append("minimap input map missing; ");

            string build = ReadText("ProjectSettings/EditorBuildSettings.asset");
            if (build == null)
            {
                fail.Append("build settings missing; ");
                return;
            }
            int play = build.IndexOf("Assets/Scenes/Play.unity", StringComparison.Ordinal);
            int boot = build.IndexOf("Assets/Scenes/Boot.unity", StringComparison.Ordinal);
            if (play < 0 || (boot >= 0 && boot < play))
                fail.Append("Play is not the first build scene; ");
        }
    }
}
