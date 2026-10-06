using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Mega Park playable graybox, real meters. Origin is the SW corner.
    /// Loop vertices are the P1 lock from 5ba0cfc (472 m CCW). Zone shells are graybox
    /// cubes so each collider matches its mesh. This type has no UnityEngine dependency
    /// so the headless sim can audit grounding, spawns, and the sprint-12 timing.
    /// </summary>
    public static class MegaParkP1Layout
    {
        public const float MapW = 160f;
        public const float MapD = 100f;
        public const float BowlFloorY = -1f;
        public const float Collar = 3f;
        public const float LoopLengthM = 472f;
        public const float SprintSpeed = 12f;
        public const float MantleMin = 0.45f;
        public const float MantleMax = 2.55f;
        public const float BarUnderClear = 1.05f;
        public const float SpawnY = 0.2f;

        public const float SpawnSwX = 8f;
        public const float SpawnSwZ = 8f;
        public const float SpawnSeX = 152f;
        public const float SpawnSeZ = 8f;
        public const float SpawnNwX = 8f;
        public const float SpawnNwZ = 92f;
        public const float SpawnNeX = 152f;
        public const float SpawnNeZ = 92f;

        public struct Pt
        {
            public float X, Y, Z;
            public Pt(float x, float y, float z) { X = x; Y = y; Z = z; }
        }

        public static readonly Pt[] LoopCcw =
        {
            new Pt(8f, 0f, 8f),
            new Pt(38f, 0f, 8f),
            new Pt(38f, 0f, 16f),
            new Pt(118f, 0f, 16f),
            new Pt(118f, 0f, 8f),
            new Pt(152f, 0f, 8f),
            new Pt(152f, 0f, 92f),
            new Pt(8f, 0f, 92f),
        };

        public struct SpawnPad
        {
            public string Name;
            public float X, Z, YawDeg;
        }

        // Index order matches LocalPlayerSpawner: SW, SE, NW, NE.
        public static readonly SpawnPad[] Spawns =
        {
            new SpawnPad { Name = "Spawn_SW", X = SpawnSwX, Z = SpawnSwZ, YawDeg = 90f },
            new SpawnPad { Name = "Spawn_SE", X = SpawnSeX, Z = SpawnSeZ, YawDeg = 0f },
            new SpawnPad { Name = "Spawn_NW", X = SpawnNwX, Z = SpawnNwZ, YawDeg = 180f },
            new SpawnPad { Name = "Spawn_NE", X = SpawnNeX, Z = SpawnNeZ, YawDeg = -90f },
        };

        public struct Solid
        {
            public string Name;
            public string Zone;
            public string Kind;
            public string Mat;
            public float X, Y, Z, Sx, Sy, Sz;
            public float SupportY;
        }

        public struct Ramp
        {
            public string Name;
            public string Zone;
            public string Mat;
            public float X0, Y0, Z0, X1, Y1, Z1;
            public float Width, Thickness;
        }

        public struct Audit
        {
            public bool Ok;
            public string Line;
            public string Failure;
            public float LoopM;
            public float Seconds;
            public int SolidCount;
            public int WallCount;
            public int VaultCount;
            public float BarClear;
        }

        public static Audit Run()
        {
            Solid[] solids = BuildSolids();
            Ramp[] ramps = BuildRamps();
            var fail = new StringBuilder();
            float loop = MeasureLoop();
            float seconds = loop / SprintSpeed;

            if (Math.Abs(loop - LoopLengthM) > 0.05f)
                fail.Append("loop length ").Append(loop.ToString("0.00", CultureInfo.InvariantCulture)).Append(" != 472; ");

            int walls = 0;
            int vaults = 0;
            float barClear = float.MaxValue;
            int bars = 0;
            var zones = new HashSet<string>();

            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                zones.Add(s.Zone);
                float bottom = s.Y - s.Sy * 0.5f;
                float top = s.Y + s.Sy * 0.5f;
                if (s.Kind == "wall") walls++;
                if (s.Kind == "vault" || (s.Kind == "block" && top >= MantleMin && top <= MantleMax))
                    vaults++;
                if (s.Kind == "bar")
                {
                    bars++;
                    if (bottom < barClear) barClear = bottom;
                }

                if (!Grounded(s, solids, bottom, out string why))
                    fail.Append(s.Name).Append(' ').Append(why).Append("; ");

                if (s.Kind == "wall")
                {
                    float thin = Math.Min(s.Sx, s.Sz);
                    float longs = Math.Max(s.Sx, s.Sz);
                    if (s.Sy < 4.2f || thin > 0.6f || longs < 6f || s.Mat != "blue")
                        fail.Append(s.Name).Append(" is not a tall flat cling wall; ");
                }
                else if (s.Mat == "blue")
                    fail.Append(s.Name).Append(" is blue but not a cling wall; ");

                if (s.Kind == "vault")
                {
                    float lip = top - s.SupportY;
                    if (lip < MantleMin + 0.04f || lip > MantleMax - 0.04f)
                        fail.Append(s.Name).Append(" lip ").Append(lip.ToString("0.00", CultureInfo.InvariantCulture)).Append(" outside mantle; ");
                }

                if (s.Kind == "bar")
                {
                    if (bottom < BarUnderClear - 0.001f)
                        fail.Append(s.Name).Append(" under-clear ").Append(bottom.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
                    if (top > MantleMax || top < MantleMin)
                        fail.Append(s.Name).Append(" is not a mantle bar; ");
                }

                if (s.Kind == "toy")
                {
                    float maxDim = Math.Max(s.Sx, Math.Max(s.Sy, s.Sz));
                    if (maxDim > 0.6f)
                        fail.Append(s.Name).Append(" toy exceeds 0.6 m; ");
                }

                if (s.Kind != "ground" && OverlapsOpen(s))
                    fail.Append(s.Name).Append(" sits in the open bowl rect; ");
                if (s.Kind != "ground" && OverlapsCrossingB(s))
                    fail.Append(s.Name).Append(" blocks crossing B; ");
                if (BlocksLoop(s))
                    fail.Append(s.Name).Append(" blocks the CCW centerline under 1.05 m; ");
            }

            if (bars < 7)
                fail.Append("monkey bars missing; ");
            if (barClear > 100f) barClear = 0f;
            if (walls < 4)
                fail.Append("cling walls missing; ");
            if (vaults < 8)
                fail.Append("mantle vaults missing; ");

            for (int i = 0; i < ramps.Length; i++)
            {
                if (!RampOk(ramps[i], solids, out string why))
                    fail.Append(ramps[i].Name).Append(' ').Append(why).Append("; ");
                bool slide = ramps[i].Name.StartsWith("Slide", StringComparison.Ordinal);
                if (slide && ramps[i].Mat != "yellow")
                    fail.Append(ramps[i].Name).Append(" slide is not yellow; ");
                if (!slide && ramps[i].Mat == "yellow")
                    fail.Append(ramps[i].Name).Append(" is yellow but not a slide; ");
            }

            if (!Has(zones, "Z1") || !Has(zones, "Z2") || !Has(zones, "Z3") || !Has(zones, "Z4")
                || !Has(zones, "Z5") || !Has(zones, "Z6") || !Has(zones, "Z7") || !Has(zones, "Z8")
                || !Has(zones, "Z9") || !Has(zones, "Z10"))
                fail.Append("a locked zone is missing; ");

            if (!FindTopAtLeast(solids, "Slide_T3", 4.9f))
                fail.Append("slide mountain does not reach +5 m; ");
            if (!Route(solids, "Slide_StepA", "Slide_StepB", 0.45f, 2.0f))
                fail.Append("slide step A does not reach step B; ");
            if (!Route(solids, "Slide_StepB", "Slide_T3", 0.45f, 2.0f))
                fail.Append("slide step B does not reach the +5 tower; ");
            if (!Route(solids, "Slide_T2Step", "Slide_T2", 0.45f, 2.0f))
                fail.Append("slide T2 step does not reach the mid tower; ");

            if (!SpawnsOk(out string spawnWhy))
                fail.Append(spawnWhy);

            int anchors = 0;
            for (int i = 0; i < solids.Length; i++)
            {
                float top = solids[i].Y + solids[i].Sy * 0.5f;
                if (top >= 2.4f && solids[i].Kind != "ground")
                    anchors++;
            }
            if (anchors < 8)
                fail.Append("not enough tall grapple anchors; ");

            var audit = new Audit
            {
                Ok = fail.Length == 0,
                LoopM = loop,
                Seconds = seconds,
                SolidCount = solids.Length,
                WallCount = walls,
                VaultCount = vaults,
                BarClear = bars > 0 ? barClear : 0f,
            };
            audit.Line = string.Format(
                CultureInfo.InvariantCulture,
                "MegaPark map: loop {0:0.00} m at sprint {1:0} = {2:0.000} s; spawns 4; solids {3} grounded; cling walls {4}; vaults {5}; bar under-clear {6:0.00} m; crossings A+B open",
                audit.LoopM, SprintSpeed, audit.Seconds, audit.SolidCount, audit.WallCount, audit.VaultCount, audit.BarClear);
            audit.Failure = fail.ToString();
            return audit;
        }

        public static string ProofLine()
        {
            return Run().Line;
        }

        public static bool Holds()
        {
            return Run().Ok;
        }

        public static float MeasureLoop()
        {
            float len = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                Pt a = LoopCcw[i];
                Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                float dz = a.Z - b.Z;
                len += (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            return len;
        }

        public static Solid[] BuildSolids()
        {
            var list = new List<Solid>(160);
            // Footing. Tops at Y=0 except the bowl floor at Y=-1 and the collar a hair below.
            Add(list, "Mulch_West", "Ground", "ground", "mulch", 23f, -0.1f, 50f, 46f, 0.2f, 100f, 0f);
            Add(list, "Mulch_East", "Ground", "ground", "mulch", 119f, -0.1f, 50f, 82f, 0.2f, 100f, 0f);
            Add(list, "Mulch_South", "Ground", "ground", "mulch", 62f, -0.1f, 17f, 32f, 0.2f, 34f, 0f);
            Add(list, "Mulch_North", "Ground", "ground", "mulch", 62f, -0.1f, 83f, 32f, 0.2f, 34f, 0f);
            Add(list, "Sandbox_Floor", "Z8", "ground", "sand", 62f, BowlFloorY - 0.1f, 50f, 32f, 0.2f, 32f, BowlFloorY);
            Add(list, "Collar_S", "Ground", "ground", "grass", 80f, -0.12f, -1.5f, 166f, 0.2f, 3f, 0f);
            Add(list, "Collar_N", "Ground", "ground", "grass", 80f, -0.12f, 101.5f, 166f, 0.2f, 3f, 0f);
            Add(list, "Collar_W", "Ground", "ground", "grass", -1.5f, -0.12f, 50f, 3f, 0.2f, 100f, 0f);
            Add(list, "Collar_E", "Ground", "ground", "grass", 161.5f, -0.12f, 50f, 3f, 0.2f, 100f, 0f);

            // Boundary only. Rubber, not blue. Not the cling arena.
            Add(list, "Fence_S", "Fence", "fence", "rubber", 80f, 1.2f, -0.04f, 160.08f, 2.4f, 0.08f, 0f);
            Add(list, "Fence_N", "Fence", "fence", "rubber", 80f, 1.2f, 100.04f, 160.08f, 2.4f, 0.08f, 0f);
            Add(list, "Fence_W", "Fence", "fence", "rubber", -0.04f, 1.2f, 50f, 0.08f, 2.4f, 100f, 0f);
            Add(list, "Fence_E", "Fence", "fence", "rubber", 160.04f, 1.2f, 50f, 0.08f, 2.4f, 100f, 0f);

            // Z1 Soft-Play. South fringe z=8 and the x=38 corner stay clear.
            Add(list, "SoftPlay_DeckLow", "Z1", "block", "cedar", 14f, 1f, 26f, 10f, 2f, 8f, 0f);
            Add(list, "SoftPlay_DeckHigh", "Z1", "cap", "cedar", 14f, 2.75f, 26f, 6f, 1.5f, 5f, 2f);
            Add(list, "SoftPlay_TubeL", "Z1", "block", "cedar", 14f, 1f, 16.9f, 8f, 2f, 0.2f, 0f);
            Add(list, "SoftPlay_TubeR", "Z1", "block", "cedar", 14f, 1f, 19.5f, 8f, 2f, 0.2f, 0f);
            Add(list, "SoftPlay_TubeRoof", "Z1", "cap", "cedar", 14f, 2.1f, 18.2f, 8f, 0.2f, 2.9f, 2f);
            Add(list, "SoftPlay_CubeA", "Z1", "vault", "cedar", 26f, 0.55f, 20f, 1.4f, 1.1f, 1.4f, 0f);
            Add(list, "SoftPlay_CubeB", "Z1", "vault", "cedar", 30f, 0.4f, 28f, 1.6f, 0.8f, 1.6f, 0f);
            Add(list, "SoftPlay_CubeC", "Z1", "vault", "cedar", 24f, 0.85f, 32f, 1.2f, 1.7f, 1.2f, 0f);

            // Z2 Cling arena, west of the x=8 loop. Staggered flat faces.
            Add(list, "Cling_A", "Z2", "wall", "blue", 3.8f, 2.4f, 46f, 0.5f, 4.8f, 8f, 0f);
            Add(list, "Cling_B", "Z2", "wall", "blue", 5.2f, 2.35f, 54f, 0.45f, 4.7f, 8f, 0f);
            Add(list, "Cling_C", "Z2", "wall", "blue", 3.7f, 2.5f, 64f, 0.5f, 5f, 8f, 0f);
            Add(list, "Cling_D", "Z2", "wall", "blue", 5.05f, 2.3f, 72f, 0.4f, 4.6f, 7f, 0f);

            // Z3 Merry. Crossing B is the open band z[44,52].
            Add(list, "Merry_Podium", "Z3", "bump", "rubber", 34f, 0.15f, 39f, 5f, 0.3f, 5f, 0f);
            Add(list, "Merry_Post_SW", "Z3", "anchor", "steel", 31.2f, 1.2f, 36.6f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_Post_SE", "Z3", "anchor", "steel", 36.8f, 1.2f, 36.6f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_Post_NW", "Z3", "anchor", "steel", 31.2f, 1.2f, 41.4f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_Post_NE", "Z3", "anchor", "steel", 36.8f, 1.2f, 41.4f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_TableW", "Z3", "vault", "rubber", 28f, 0.42f, 56f, 2.4f, 0.84f, 1.2f, 0f);
            Add(list, "Merry_TableE", "Z3", "vault", "rubber", 40f, 0.42f, 56f, 2.4f, 0.84f, 1.2f, 0f);

            // Z4 Slide mountain on the rim, south of the z=92 loop and the north spine.
            Add(list, "Slide_T1", "Z4", "block", "rim", 28f, 1f, 78f, 4f, 2f, 4f, 0f);
            Add(list, "Slide_T2", "Z4", "block", "rim", 38f, 1.75f, 79f, 4f, 3.5f, 4f, 0f);
            Add(list, "Slide_T2Step", "Z4", "block", "rim", 40.8f, 1f, 79f, 1.6f, 2f, 2.2f, 0f);
            Add(list, "Slide_T3", "Z4", "block", "rim", 48f, 2.5f, 78f, 4f, 5f, 4f, 0f);
            Add(list, "Slide_StepA", "Z4", "block", "rim", 45.1f, 1f, 77.2f, 1.8f, 2f, 1.8f, 0f);
            Add(list, "Slide_StepB", "Z4", "block", "rim", 45.1f, 1.75f, 78.8f, 1.8f, 3.5f, 1.8f, 0f);

            // Z5 Swing grove, south of the north spine (z>=83) and the loop.
            AddSwing(list, 66f);
            AddSwing(list, 78f);
            AddSwing(list, 90f);

            // Z6 Twin forts. East spine x[130,138] and the loop x=152 stay empty.
            AddCrawl(list, "Army", 123f, 28.2f);
            AddSpiral(list, "Army", 143f, 28f);
            AddCrawl(list, "Knight", 123f, 64.2f);
            AddSpiral(list, "Knight", 143f, 70f);

            // Z7 Kickball. East rail is the vault. Bases stay under mantle height.
            Add(list, "Base_Home", "Z7", "bump", "rubber", 96f, 0.1f, 34f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Base_First", "Z7", "bump", "rubber", 110f, 0.1f, 48f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Base_Second", "Z7", "bump", "rubber", 96f, 0.1f, 62f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Base_Third", "Z7", "bump", "rubber", 82f, 0.1f, 48f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Mound", "Z7", "bump", "rubber", 96f, 0.125f, 48f, 2.4f, 0.25f, 2.4f, 0f);
            Add(list, "Rail_East", "Z7", "vault", "rubber", 114f, 0.45f, 48f, 0.12f, 0.9f, 40f, 0f);

            // Z8 toys, outside the open rect, sitting on the sand top.
            Add(list, "Toy_Sandbox_Bucket_SW", "Z8", "toy", "rubber", 51f, -0.825f, 42.5f, 0.4f, 0.35f, 0.4f, BowlFloorY);
            Add(list, "Toy_Sandbox_Shovel_SW", "Z8", "toy", "rubber", 51f, -0.97f, 43.6f, 0.55f, 0.06f, 0.08f, BowlFloorY);
            Add(list, "Toy_Sandbox_Mold_NE", "Z8", "toy", "rubber", 73f, -0.94f, 60.4f, 0.5f, 0.12f, 0.5f, BowlFloorY);
            Add(list, "Toy_Sandbox_Sifter_NE", "Z8", "toy", "rubber", 73f, -0.97f, 61.2f, 0.45f, 0.06f, 0.4f, BowlFloorY);

            // Z9 bars. Bottom 1.14 m so crouch (1.05) clears and the top is a mantle lip.
            for (int i = 0; i < 7; i++)
            {
                float x = 48f + i * 10f;
                string id = i.ToString(CultureInfo.InvariantCulture);
                Add(list, "BarPost_S" + id, "Z9", "post", "steel", x, 0.57f, 12.7f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "BarPost_N" + id, "Z9", "post", "steel", x, 0.57f, 19.3f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "Bar_" + id, "Z9", "bar", "steel", x, 1.2f, 16f, 0.14f, 0.12f, 6.8f, 1.14f);
            }

            // Z10 hopscotch lips, north of the z=8 approach.
            for (int i = 0; i < 8; i++)
            {
                float x = 124f + i * 3f;
                float z = (i % 2 == 0) ? 13.5f : 16.2f;
                Add(list, "Hop_" + i.ToString(CultureInfo.InvariantCulture), "Z10", "vault", "concrete", x, 0.32f, z, 1.05f, 0.64f, 1.05f, 0f);
            }

            return list.ToArray();
        }

        public static Ramp[] BuildRamps()
        {
            return new[]
            {
                RampOf("SandBank_W", "Z8", "sand", 46f, 0f, 50f, 50f, BowlFloorY, 50f, 32f),
                RampOf("SandBank_E", "Z8", "sand", 78f, 0f, 50f, 74f, BowlFloorY, 50f, 32f),
                RampOf("SandBank_S", "Z8", "sand", 62f, 0f, 34f, 62f, BowlFloorY, 38f, 32f),
                RampOf("SandBank_N", "Z8", "sand", 62f, 0f, 66f, 62f, BowlFloorY, 62f, 32f),
                RampOf("Slide_Chute1", "Z4", "yellow", 28f, 2f, 76f, 28f, 0f, 68f, 1.6f),
                RampOf("Slide_Chute2", "Z4", "yellow", 38f, 3.5f, 77f, 38f, 0f, 68f, 1.6f),
                RampOf("Slide_Chute3", "Z4", "yellow", 48f, 5f, 76f, 48f, 0f, 66f, 1.6f),
            };
        }

        static void AddSwing(List<Solid> list, float x)
        {
            string id = x.ToString("0", CultureInfo.InvariantCulture);
            Add(list, "Swing_PostL_" + id, "Z5", "anchor", "steel", x - 1.2f, 1.035f, 81f, 0.2f, 2.07f, 0.2f, 0f);
            Add(list, "Swing_PostR_" + id, "Z5", "anchor", "steel", x + 1.2f, 1.035f, 81f, 0.2f, 2.07f, 0.2f, 0f);
            Add(list, "Swing_Beam_" + id, "Z5", "cap", "steel", x, 2.15f, 81f, 2.6f, 0.16f, 0.2f, 2.07f);
            Add(list, "Swing_Rail_" + id, "Z5", "vault", "rubber", x, 0.45f, 82.3f, 2.2f, 0.9f, 0.16f, 0f);
        }

        static void AddCrawl(List<Solid> list, string fort, float x, float z)
        {
            Add(list, fort + "_CrawlL", "Z6", "block", "bark", x, 0.6f, z - 0.9f, 7f, 1.2f, 0.22f, 0f);
            Add(list, fort + "_CrawlR", "Z6", "block", "bark", x, 0.6f, z + 0.9f, 7f, 1.2f, 0.22f, 0f);
            Add(list, fort + "_CrawlRoof", "Z6", "cap", "bark", x, 1.29f, z, 7f, 0.18f, 2.02f, 1.2f);
        }

        static void AddSpiral(List<Solid> list, string fort, float x, float z)
        {
            Add(list, fort + "_Core", "Z6", "anchor", "bark", x, 1.4f, z, 0.5f, 2.8f, 0.5f, 0f);
            Add(list, fort + "_L1", "Z6", "vault", "bark", x - 1.5f, 0.35f, z - 1.4f, 1.5f, 0.7f, 1.5f, 0f);
            Add(list, fort + "_L2", "Z6", "vault", "bark", x + 1.5f, 0.7f, z - 1.4f, 1.5f, 1.4f, 1.5f, 0f);
            Add(list, fort + "_L3", "Z6", "vault", "bark", x + 1.5f, 1.05f, z + 1.4f, 1.5f, 2.1f, 1.5f, 0f);
            Add(list, fort + "_L4", "Z6", "block", "bark", x - 1.5f, 1.4f, z + 1.4f, 1.5f, 2.8f, 1.5f, 0f);
        }

        static void Add(List<Solid> list, string name, string zone, string kind, string mat,
            float x, float y, float z, float sx, float sy, float sz, float support)
        {
            list.Add(new Solid
            {
                Name = name,
                Zone = zone,
                Kind = kind,
                Mat = mat,
                X = x,
                Y = y,
                Z = z,
                Sx = sx,
                Sy = sy,
                Sz = sz,
                SupportY = support,
            });
        }

        static Ramp RampOf(string name, string zone, string mat,
            float x0, float y0, float z0, float x1, float y1, float z1, float width)
        {
            return new Ramp
            {
                Name = name,
                Zone = zone,
                Mat = mat,
                X0 = x0,
                Y0 = y0,
                Z0 = z0,
                X1 = x1,
                Y1 = y1,
                Z1 = z1,
                Width = width,
                Thickness = 0.22f,
            };
        }

        static bool Grounded(Solid s, Solid[] all, float bottom, out string why)
        {
            why = null;
            if (s.Kind == "ground")
            {
                if (s.Name == "Sandbox_Floor")
                {
                    float top = s.Y + s.Sy * 0.5f;
                    if (Math.Abs(top - BowlFloorY) > 0.02f)
                    {
                        why = "bowl floor is not Y=-1";
                        return false;
                    }
                }
                return true;
            }

            if (bottom < s.SupportY - 0.03f)
            {
                why = "buried below " + s.SupportY.ToString("0.00", CultureInfo.InvariantCulture);
                return false;
            }
            if (bottom > s.SupportY + 0.03f)
            {
                why = "floating above " + s.SupportY.ToString("0.00", CultureInfo.InvariantCulture);
                return false;
            }

            if (s.SupportY > 0.02f)
            {
                bool found = false;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].Name == s.Name) continue;
                    float top = all[i].Y + all[i].Sy * 0.5f;
                    if (Math.Abs(top - s.SupportY) > 0.03f) continue;
                    if (OverlapXZ(s, all[i], 0.02f))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    why = "no supporting solid";
                    return false;
                }
            }
            else if (s.SupportY < -0.5f)
            {
                if (s.X < 46f || s.X > 78f || s.Z < 34f || s.Z > 66f)
                {
                    why = "bowl toy is off the sand";
                    return false;
                }
            }
            return true;
        }

        static bool OverlapXZ(Solid a, Solid b, float pad)
        {
            return Math.Abs(a.X - b.X) <= (a.Sx + b.Sx) * 0.5f + pad
                && Math.Abs(a.Z - b.Z) <= (a.Sz + b.Sz) * 0.5f + pad;
        }

        static bool OverlapsOpen(Solid s)
        {
            return AabbHits(s, 52f, 72f, 40f, 58f);
        }

        static bool OverlapsCrossingB(Solid s)
        {
            return AabbHits(s, 22f, 46f, 44f, 52f);
        }

        static bool AabbHits(Solid s, float x0, float x1, float z0, float z1)
        {
            float minX = s.X - s.Sx * 0.5f;
            float maxX = s.X + s.Sx * 0.5f;
            float minZ = s.Z - s.Sz * 0.5f;
            float maxZ = s.Z + s.Sz * 0.5f;
            return minX < x1 && maxX > x0 && minZ < z1 && maxZ > z0;
        }

        static bool BlocksLoop(Solid s)
        {
            if (s.Kind == "ground" || s.Kind == "fence") return false;
            float bottom = s.Y - s.Sy * 0.5f;
            float top = s.Y + s.Sy * 0.5f;
            if (bottom >= BarUnderClear - 0.001f) return false;
            if (top <= 0.04f) return false;
            const float clear = 0.9f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                Pt a = LoopCcw[i];
                Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = (float)Math.Sqrt(dx * dx + dz * dz);
                int steps = Math.Max(1, (int)Math.Ceiling(seg));
                for (int sidx = 0; sidx <= steps; sidx++)
                {
                    float t = sidx / (float)steps;
                    float x = a.X + dx * t;
                    float z = a.Z + dz * t;
                    if (DistXZ(x, z, s) < clear && top > 0.05f && bottom < BarUnderClear)
                        return true;
                }
            }
            return false;
        }

        static float DistXZ(float px, float pz, Solid s)
        {
            float minX = s.X - s.Sx * 0.5f;
            float maxX = s.X + s.Sx * 0.5f;
            float minZ = s.Z - s.Sz * 0.5f;
            float maxZ = s.Z + s.Sz * 0.5f;
            float dx = px < minX ? minX - px : (px > maxX ? px - maxX : 0f);
            float dz = pz < minZ ? minZ - pz : (pz > maxZ ? pz - maxZ : 0f);
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static bool RampOk(Ramp r, Solid[] solids, out string why)
        {
            why = null;
            float dx = r.X1 - r.X0;
            float dy = r.Y1 - r.Y0;
            float dz = r.Z1 - r.Z0;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 1f)
            {
                why = "ramp is degenerate";
                return false;
            }
            if (r.Y0 > r.Y1)
            {
                // Endpoints are unordered. Both ends must be a floor height or a tower top.
            }
            if (!RampEndSupported(r.X0, r.Y0, r.Z0, solids) || !RampEndSupported(r.X1, r.Y1, r.Z1, solids))
            {
                why = "ramp end is floating";
                return false;
            }
            if (r.Mat == "yellow" && Math.Max(r.Y0, r.Y1) < 1.5f)
            {
                why = "slide does not leave a deck";
                return false;
            }
            float minX = Math.Min(r.X0, r.X1) - (Math.Abs(dz) > Math.Abs(dx) ? r.Width * 0.5f : 0f);
            float maxX = Math.Max(r.X0, r.X1) + (Math.Abs(dz) > Math.Abs(dx) ? r.Width * 0.5f : 0f);
            float minZ = Math.Min(r.Z0, r.Z1) - (Math.Abs(dx) > Math.Abs(dz) ? r.Width * 0.5f : 0f);
            float maxZ = Math.Max(r.Z0, r.Z1) + (Math.Abs(dx) > Math.Abs(dz) ? r.Width * 0.5f : 0f);
            bool hitsOpen = minX < 72f && maxX > 52f && minZ < 58f && maxZ > 40f;
            if (hitsOpen && r.Name.StartsWith("Slide", StringComparison.Ordinal))
            {
                why = "slide crosses the open bowl";
                return false;
            }
            if (hitsOpen && r.Name.StartsWith("Sand", StringComparison.Ordinal))
            {
                why = "sand bank crosses the open rect";
                return false;
            }
            return true;
        }

        static bool RampEndSupported(float x, float y, float z, Solid[] solids)
        {
            if (Math.Abs(y) <= 0.03f || Math.Abs(y - BowlFloorY) <= 0.03f)
                return true;
            for (int i = 0; i < solids.Length; i++)
            {
                float top = solids[i].Y + solids[i].Sy * 0.5f;
                if (Math.Abs(top - y) > 0.05f) continue;
                if (DistXZ(x, z, solids[i]) <= 0.2f)
                    return true;
            }
            return false;
        }

        static bool FindTopAtLeast(Solid[] solids, string name, float minTop)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Name != name) continue;
                return solids[i].Y + solids[i].Sy * 0.5f >= minTop;
            }
            return false;
        }

        static bool Route(Solid[] solids, string from, string to, float minDy, float maxDy)
        {
            Solid a = default;
            Solid b = default;
            bool fa = false, fb = false;
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Name == from) { a = solids[i]; fa = true; }
                if (solids[i].Name == to) { b = solids[i]; fb = true; }
            }
            if (!fa || !fb) return false;
            float topA = a.Y + a.Sy * 0.5f;
            float topB = b.Y + b.Sy * 0.5f;
            float dy = topB - topA;
            if (dy < minDy || dy > maxDy) return false;
            return Math.Abs(a.X - b.X) <= (a.Sx + b.Sx) * 0.5f + 0.2f
                && Math.Abs(a.Z - b.Z) <= (a.Sz + b.Sz) * 0.5f + 0.2f;
        }

        static bool SpawnsOk(out string why)
        {
            why = null;
            if (Spawns.Length != 4)
            {
                why = "expected 4 spawns; ";
                return false;
            }
            for (int i = 0; i < Spawns.Length; i++)
            {
                SpawnPad s = Spawns[i];
                bool onLoop = false;
                for (int p = 0; p < LoopCcw.Length; p++)
                {
                    if (Math.Abs(LoopCcw[p].X - s.X) < 0.01f && Math.Abs(LoopCcw[p].Z - s.Z) < 0.01f)
                        onLoop = true;
                }
                if (!onLoop)
                {
                    why = s.Name + " is not a loop vertex; ";
                    return false;
                }
                if (s.X < 1f || s.X > MapW - 1f || s.Z < 1f || s.Z > MapD - 1f)
                {
                    why = s.Name + " is outside the park; ";
                    return false;
                }
            }
            if (Math.Abs(SpawnY) < 0.05f || SpawnY > 1f)
            {
                why = "spawn Y is buried or floating; ";
                return false;
            }
            if (Spawns[0].YawDeg != 90f || Spawns[1].YawDeg != 0f || Spawns[2].YawDeg != 180f || Spawns[3].YawDeg != -90f)
            {
                why = "spawn facing drifted; ";
                return false;
            }
            return true;
        }

        static bool Has(HashSet<string> zones, string id)
        {
            return zones.Contains(id);
        }
    }
}
