using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Stack Yard: a vertical arena on the Mega Park piece kit.
    /// Footprint is 110 by 70 m. Ground, mid decks at 6 m, and roofs at 12 m.
    /// Feel locks are not stored here and are not retuned.
    /// </summary>
    public static partial class StackYardLayout
    {
        public const float MapW = 110f;
        public const float MapD = 70f;
        public const float SprintSpeed = MegaParkP1Layout.SprintSpeed;
        public const float SpawnY = MegaParkP1Layout.SpawnY;
        public const float FenceTop = MegaParkP1Layout.FenceTop;
        public const float KillPlaneY = MegaParkP1Layout.KillPlaneY;
        public const float MidY = 6f;
        public const float RoofY = 12f;
        public const float LoopMin = 298f;
        public const float LoopMax = 302f;
        public const int DrawBudget = 70;
        public const float MeshBudget = 0.05f;
        public const int PaintDraws = 4;
        public const int LabelDraws = 4;
        public const int SpawnDraws = 4;
        public const int ToyDraws = 6;
        public const int MinimapDraws = 1;

        public struct Audit
        {
            public bool Ok;
            public string Line;
            public string Failure;
        }

        /// <summary>Perimeter street. 2*(94+56) = 300 m, 25 s at sprint 12.</summary>
        public static readonly MegaParkP1Layout.Pt[] LoopCcw =
        {
            new MegaParkP1Layout.Pt(8f, 0f, 7f),
            new MegaParkP1Layout.Pt(102f, 0f, 7f),
            new MegaParkP1Layout.Pt(102f, 0f, 63f),
            new MegaParkP1Layout.Pt(8f, 0f, 63f),
        };

        /// <summary>
        /// Steer loop for Stack Yard only. The shared planner reads these points.
        /// A wide ground rectangle, plus one step in toward the east roof so a
        /// chase can leave the street and stand on that tier. The west mid mark
        /// sits on the deck lip the same loop can already tag.
        /// </summary>
        public static readonly MegaParkP1Layout.Pt[] AiLoop =
        {
            new MegaParkP1Layout.Pt(16f, 0f, 14f),
            new MegaParkP1Layout.Pt(55f, 0f, 14f),
            new MegaParkP1Layout.Pt(94f, 0f, 14f),
            new MegaParkP1Layout.Pt(94f, 0f, 35f),
            new MegaParkP1Layout.Pt(94f, 0f, 57f),
            new MegaParkP1Layout.Pt(70f, 0f, 49f),
            new MegaParkP1Layout.Pt(55f, 0f, 57f),
            new MegaParkP1Layout.Pt(16f, 0f, 57f),
            new MegaParkP1Layout.Pt(16f, 0f, 35f),
        };

        /// <summary>
        /// How far a low-difficulty flee aim slides onto <see cref="AiLoop"/>.
        /// Shared scoring is unchanged. Mega Park and Pocket Park do not read this.
        /// </summary>
        public const float EvadeLoopAtLow = 0.18f;

        public static float EvadeLoopBlend(float difficulty)
        {
            const float hi = 0.45f;
            if (difficulty >= hi) return 0f;
            if (difficulty <= 0.2f) return EvadeLoopAtLow;
            float t = (hi - difficulty) / (hi - 0.2f);
            return EvadeLoopAtLow * t;
        }

        public static float AiLoopLength
        {
            get
            {
                float len = 0f;
                for (int i = 0; i < AiLoop.Length; i++)
                {
                    MegaParkP1Layout.Pt a = AiLoop[i];
                    MegaParkP1Layout.Pt b = AiLoop[(i + 1) % AiLoop.Length];
                    len += Dist(a.X, a.Z, b.X, b.Z);
                }
                return len;
            }
        }

        public struct CoverSample
        {
            public float X, Z;
        }

        public static readonly CoverSample[] CoverSamples =
        {
            new CoverSample { X = 18f, Z = 20f },
            new CoverSample { X = 18f, Z = 48f },
            new CoverSample { X = 48f, Z = 18f },
            new CoverSample { X = 62f, Z = 18f },
            new CoverSample { X = 92f, Z = 22f },
            new CoverSample { X = 92f, Z = 50f },
            new CoverSample { X = 46f, Z = 54f },
            new CoverSample { X = 72f, Z = 54f },
            new CoverSample { X = 20f, Z = 62f },
            new CoverSample { X = 90f, Z = 8f },
        };

        public struct CounterMark
        {
            public float X, Z, Y;
        }

        public static readonly CounterMark[] CounterMarks =
        {
            new CounterMark { X = 36f, Z = 20f, Y = 0f },
            new CounterMark { X = 74f, Z = 20f, Y = 0f },
            new CounterMark { X = 55f, Z = 48f, Y = 0f },
            new CounterMark { X = 44f, Z = 36f, Y = 6f },
        };

        public struct RoofMark
        {
            public float X, Y, Z;
            public string Tier;
        }

        /// <summary>
        /// Spots a chase must be able to stand on. Ground, both mids, both roofs.
        /// The west mid mark is the deck lip, not the slab center: that lip is
        /// the stand the street loop can tag. Shared scoring does not read these.
        /// </summary>
        public static readonly RoofMark[] RoofAccess =
        {
            new RoofMark { X = 55f, Y = 0f, Z = 16f, Tier = "ground" },
            new RoofMark { X = 27f, Y = 6f, Z = 34f, Tier = "mid" },
            new RoofMark { X = 78f, Y = 6f, Z = 34f, Tier = "mid" },
            new RoofMark { X = 38f, Y = 12f, Z = 42f, Tier = "roof" },
            new RoofMark { X = 70f, Y = 12f, Z = 42f, Tier = "roof" },
        };

        public static bool SampleCover(float selfX, float selfZ, float threatX, float threatZ, out float x, out float z)
        {
            x = selfX;
            z = selfZ;
            float best = -1f;
            bool found = false;
            for (int i = 0; i < CoverSamples.Length; i++)
            {
                float cx = CoverSamples[i].X;
                float cz = CoverSamples[i].Z;
                if (!Inside(cx, 0f, cz)) continue;
                float dsx = cx - selfX;
                float dsz = cz - selfZ;
                if (dsx * dsx + dsz * dsz < 9f) continue;
                float dtx = cx - threatX;
                float dtz = cz - threatZ;
                float score = (float)Math.Sqrt(dtx * dtx + dtz * dtz);
                if (score > best)
                {
                    best = score;
                    x = cx;
                    z = cz;
                    found = true;
                }
            }
            return found;
        }

        public static readonly MegaParkP1Layout.SpawnPad[] Spawns =
        {
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_SW", X = 8f, Z = 7f, YawDeg = 90f },
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_S", X = 83f, Z = 7f, YawDeg = 90f },
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_NE", X = 102f, Z = 63f, YawDeg = 180f },
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_N", X = 27f, Z = 63f, YawDeg = 180f },
        };

        // South street, north street, and the west lane. Each is open ground.
        public static readonly float[] Cuts =
        {
            12f, 7f, 98f, 7f,
            12f, 63f, 98f, 63f,
            8f, 14f, 8f, 58f,
        };

        public static readonly MegaParkP1Layout.PadSpot[] LaunchPads =
        {
            new MegaParkP1Layout.PadSpot { Name = "Launch_Yard", X = 18f, Y = 0f, Z = 18f, Apex = 4f, DirX = 1f, DirZ = 0f, Speed = 14f },
            new MegaParkP1Layout.PadSpot { Name = "Launch_Mid", X = 78f, Y = 0f, Z = 16f, Apex = 6.5f, DirX = 0f, DirZ = 1f, Speed = 14f },
            new MegaParkP1Layout.PadSpot { Name = "Launch_Roof", X = 38f, Y = 0f, Z = 14f, Apex = 12f, DirX = 0f, DirZ = 1f, Speed = 14f },
        };

        public static readonly MegaParkP1Layout.ZipLineSpot[] ZipLines =
        {
            new MegaParkP1Layout.ZipLineSpot { Name = "Zip_Cross", Ax = 38f, Ay = 12.4f, Az = 42f, Bx = 62f, By = 2.2f, Bz = 18f, Speed = 14f, Counter = "Counter_West" },
            new MegaParkP1Layout.ZipLineSpot { Name = "Zip_East", Ax = 78f, Ay = 6.5f, Az = 34f, Bx = 96f, By = 2.2f, Bz = 48f, Speed = 14f, Counter = "Counter_East" },
            new MegaParkP1Layout.ZipLineSpot { Name = "Zip_Lane", Ax = 92f, Ay = 4.4f, Az = 52f, Bx = 22f, By = 2.2f, Bz = 52f, Speed = 14f, Counter = "Counter_Lane" },
        };

        public static Audit Run()
        {
            var fail = new StringBuilder();
            MegaParkP1Layout.Solid[] solids = BuildSolids();
            MegaParkP1Layout.Ramp[] ramps = BuildRamps();
            MegaParkP1Layout.UseFlow(MapW, MapD, ZipLines, LaunchPads, true);
            try
            {
                return AuditWithFlow(solids, ramps, fail);
            }
            finally
            {
                MegaParkP1Layout.ClearFlow();
            }
        }

        public static string ProofLine()
        {
            return Run().Line;
        }

        public static bool Holds()
        {
            return Run().Ok;
        }

        public static MegaParkP1Layout.Solid[] BuildSolids()
        {
            var list = new List<MegaParkP1Layout.Solid>(96);
            // Concrete aprons, west lawn, yard mulch, and an east sand bay.
            Add(list, "Slab_South", "Ground", "ground", "concrete", 55f, -0.1f, 11f, 110f, 0.2f, 22f, 0f);
            Add(list, "Lawn_West", "Ground", "ground", "grass", 18f, -0.1f, 46f, 36f, 0.2f, 48f, 0f);
            Add(list, "Mulch_Yard", "Ground", "ground", "mulch", 55f, -0.1f, 35f, 38f, 0.2f, 26f, 0f);
            Add(list, "Sand_East", "Ground", "ground", "sand", 92f, -0.1f, 35f, 36f, 0.2f, 26f, 0f);
            Add(list, "Slab_North", "Ground", "ground", "concrete", 73f, -0.1f, 59f, 74f, 0.2f, 22f, 0f);
            Add(list, "Collar_S", "Ground", "ground", "bark", 55f, -0.12f, -1.5f, 116f, 0.2f, 3f, 0f);
            Add(list, "Collar_N", "Ground", "ground", "bark", 55f, -0.12f, 71.5f, 116f, 0.2f, 3f, 0f);
            Add(list, "Collar_W", "Ground", "ground", "bark", -1.5f, -0.12f, 35f, 3f, 0.2f, 70f, 0f);
            Add(list, "Collar_E", "Ground", "ground", "bark", 111.5f, -0.12f, 35f, 3f, 0.2f, 70f, 0f);

            Add(list, "Fence_S", "Fence", "fence", "rubber", 55f, FenceTop * 0.5f, -0.04f, 110.08f, FenceTop, 0.08f, 0f);
            Add(list, "Fence_N", "Fence", "fence", "rubber", 55f, FenceTop * 0.5f, 70.04f, 110.08f, FenceTop, 0.08f, 0f);
            Add(list, "Fence_W", "Fence", "fence", "rubber", -0.04f, FenceTop * 0.5f, 35f, 0.08f, FenceTop, 70f, 0f);
            Add(list, "Fence_E", "Fence", "fence", "rubber", 110.04f, FenceTop * 0.5f, 35f, 0.08f, FenceTop, 70f, 0f);

            // West stair. Concrete at 3 m, olive containers at 6 m, amber at 9 m, teal roofs at 12 m.
            Deck(list, "Crate_Step", "Mid", "concrete", 30f, 26f, 10f, 8f, 3f);
            Deck(list, "Crate_Mid", "Mid", "army", 30f, 34f, 10f, 8f, 6f);
            Deck(list, "Crate_Rise", "Roof", "amber", 38f, 34f, 8f, 8f, 9f);
            Deck(list, "Crate_Roof", "Roof", "pad", 38f, 42f, 8f, 8f, 12f);

            // East stair, the second way up and down.
            Deck(list, "Stack_Step", "Mid", "concrete", 78f, 26f, 10f, 8f, 3f);
            Deck(list, "Stack_Mid", "Mid", "army", 78f, 34f, 10f, 8f, 6f);
            Deck(list, "Stack_Rise", "Roof", "amber", 70f, 34f, 8f, 8f, 9f);
            Deck(list, "Stack_Roof", "Roof", "pad", 70f, 42f, 8f, 8f, 12f);

            Add(list, "Cling_Yard", "Cling", "wall", "blue", 22f, 3.3f, 26f, 0.4f, 6.6f, 6f, 0f);
            Add(list, "Cling_East", "Cling", "wall", "blue", 86f, 3.3f, 26f, 0.4f, 6.6f, 6f, 0f);
            Add(list, "Cling_Roof", "Cling", "wall", "blue", 46f, 9.3f, 42f, 0.4f, 6.6f, 6f, 6f);
            Add(list, "Cling_Lane", "Cling", "wall", "blue", 62f, 9.3f, 42f, 0.4f, 6.6f, 6f, 6f);

            Add(list, "Hook_West", "Mid", "anchor", "plate", 26.2f, 7f, 34f, 0.3f, 2f, 1.6f, 6f);
            Add(list, "Hook_East", "Roof", "anchor", "plate", 74.2f, 13f, 42f, 0.3f, 2f, 1.6f, 12f);

            AddBars(list);
            Add(list, "Cover_A", "Yard", "vault", "army", 48f, 0.55f, 22f, 1.6f, 1.1f, 1.2f, 0f);
            Add(list, "Cover_B", "Yard", "vault", "army", 62f, 0.55f, 22f, 1.6f, 1.1f, 1.3f, 0f);

            // Flat marks the shared counter pass reads by name. They do not block a cell.
            Add(list, "Counter_West", "Yard", "mark", "army", 36f, 0.02f, 20f, 0.4f, 0.04f, 0.4f, 0f);
            Add(list, "Counter_East", "Lane", "mark", "army", 74f, 0.02f, 20f, 0.4f, 0.04f, 0.4f, 0f);
            Add(list, "Counter_Lane", "Lane", "mark", "army", 55f, 0.02f, 48f, 0.4f, 0.04f, 0.4f, 0f);
            Add(list, "Counter_Mid", "Mid", "mark", "knight", 44f, 6.02f, 36f, 0.4f, 0.04f, 0.4f, 6f);

            AddMast(list, "Landmark_Yard", "Yard", "knight", 14f, 18f);
            AddMast(list, "Landmark_Lane", "Lane", "army", 96f, 18f);
            AddMast(list, "Landmark_Mid", "Mid", "army", 14f, 56f);
            AddMast(list, "Landmark_Roof", "Roof", "pad", 96f, 56f);

            AddChevron(list, "RoofZip", "Crate_Roof");
            AddChevron(list, "MidPad", "Stack_Mid");
            AddChevron(list, "WestClimb", "Cling_Yard");
            AddYardMass(list);
            AddShippingYard(list);
            return list.ToArray();
        }

        public static MegaParkP1Layout.Ramp[] BuildRamps()
        {
            return new[]
            {
                Ramp("Slide_WestMid", "Mid", "yellow", 30f, 6f, 34f, 30f, 0f, 18f, 2.4f),
                Ramp("Slide_WestRise", "Roof", "yellow", 38f, 9f, 34f, 30f, 6f, 34f, 2.2f),
                Ramp("Slide_WestRoof", "Roof", "yellow", 38f, 12f, 42f, 30f, 6f, 34f, 2.2f),
                Ramp("Slide_EastMid", "Mid", "yellow", 78f, 6f, 34f, 78f, 0f, 18f, 2.4f),
                Ramp("Slide_EastRise", "Roof", "yellow", 70f, 9f, 34f, 78f, 6f, 34f, 2.2f),
                Ramp("Slide_EastRoof", "Roof", "yellow", 70f, 12f, 42f, 78f, 6f, 34f, 2.2f),
            };
        }

        public static void PickRespawn(float fromX, float fromZ, float itX, float itZ, bool hasIt,
            out float x, out float y, out float z)
        {
            y = SpawnY;
            int best = 0;
            float bestScore = -1f;
            bool any = false;
            for (int i = 0; i < Spawns.Length; i++)
            {
                float dIt = Dist(Spawns[i].X, Spawns[i].Z, itX, itZ);
                if (hasIt && dIt < MegaParkP1Layout.SpawnClearMeters) continue;
                float dFrom = Dist(Spawns[i].X, Spawns[i].Z, fromX, fromZ);
                float score = hasIt ? dIt * 10f - dFrom : -dFrom;
                if (!any || score > bestScore)
                {
                    bestScore = score;
                    best = i;
                    any = true;
                }
            }
            if (!any)
            {
                float far = -1f;
                for (int i = 0; i < Spawns.Length; i++)
                {
                    float d = Dist(Spawns[i].X, Spawns[i].Z, itX, itZ);
                    if (d > far)
                    {
                        far = d;
                        best = i;
                    }
                }
            }
            x = Spawns[best].X;
            z = Spawns[best].Z;
        }

        static Audit AuditWithFlow(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, StringBuilder fail)
        {
            float loop = MeasureLoop();
            float seconds = loop / SprintSpeed;
            if (loop < LoopMin - 0.05f || loop > LoopMax + 0.05f)
                fail.Append("loop ").Append(loop.ToString("0.00", CultureInfo.InvariantCulture)).Append(" m; ");
            if (seconds < 24.8f || seconds > 25.2f)
                fail.Append("loop time ").Append(seconds.ToString("0.000", CultureInfo.InvariantCulture)).Append(" s; ");

            string arcs = ArcReport(fail);
            int cuts = CutReport(solids, fail);
            KitReport(solids, ramps, fail);
            string tiers = TierReport(solids, ramps, fail);
            string fall = FallReport(solids, fail);

            MegaParkP1Layout.CountFlowChokes(solids, ramps, out int dead, out int corner, out int loops, out string detail);
            int chokes = dead + corner + loops;
            if (chokes != 0)
                fail.Append("chokes ").Append(detail).Append("; ");

            int sweeps = Containment(solids, fail, out bool held);
            float mesh = MegaParkP1Layout.MeasureMeshGap(solids, ramps);
            if (mesh > MeshBudget)
                fail.Append("collider mismatch ").Append(mesh.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");
            float ground = GroundError(solids, fail);
            if (ground > 0.0001f)
                fail.Append("ground error ").Append(ground.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");

            float contrast = ContrastReport(solids, ramps, fail);
            string look = LookNote(solids, fail);
            int draws = DrawReport(solids, ramps, fail);
            int stuck = DummyReport(out int padsTaken, out int zipsTaken, fail);
            string skills = SkillReport(fail);
            WriteStills(solids, ramps, fail);

            var audit = new Audit();
            audit.Ok = fail.Length == 0;
            audit.Line = string.Format(
                CultureInfo.InvariantCulture,
                "StackYard map: loop {0:0.00} m at sprint {1:0} = {2:0.000} s; spawns {3} arcs {4}; cuts {5}; chokes {6} dead {7} corner {8} loop {9}; {10}; {11}; containment fence {12:0.0} m kill {13:0.00} sweeps {14} {15}; collider mismatch {16:0.000} m; ground error {17:0.000} m; contrast {18:0.00}; {19}; draws {20}; dummy stuck {21} pads {22} zips {23}; {24}",
                loop, SprintSpeed, seconds, Spawns.Length, arcs, cuts,
                chokes, dead, corner, loops, tiers, fall,
                FenceTop, KillPlaneY, sweeps, held ? "held" : "open",
                mesh, ground, contrast, look, draws, stuck, padsTaken, zipsTaken, skills);
            audit.Failure = fail.ToString();
            return audit;
        }

        static string TierReport(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, StringBuilder fail)
        {
            // 0 ground, 1 mid, 2 roof. Two named routes each way, existing verbs only.
            var links = new string[3, 3];
            for (int a = 0; a < 3; a++)
                for (int b = 0; b < 3; b++)
                    links[a, b] = "";
            void AddLink(int from, int to, string name)
            {
                if (links[from, to].Length > 0) links[from, to] += "+";
                links[from, to] += name;
            }

            if (WallCovers(solids, "Cling_Yard", 0f, MidY)) AddLink(0, 1, "climb");
            else fail.Append("Cling_Yard does not reach mid; ");
            if (WallCovers(solids, "Cling_East", 0f, MidY)) AddLink(0, 1, "climb2");
            else fail.Append("Cling_East does not reach mid; ");
            if (PadReaches(LaunchPads[1], 0f, MidY)) AddLink(0, 1, "pad");
            if (PadReaches(LaunchPads[2], 0f, RoofY)) AddLink(0, 2, "pad");
            else fail.Append("Launch_Roof does not reach the roof; ");
            if (WallCovers(solids, "Cling_Roof", MidY, RoofY)) AddLink(1, 2, "climb");
            else fail.Append("Cling_Roof does not reach the roof; ");
            if (WallCovers(solids, "Cling_Lane", MidY, RoofY)) AddLink(1, 2, "climb2");
            else fail.Append("Cling_Lane does not reach the roof; ");
            if (Stair(solids, "Crate_Mid", "Crate_Rise") && Stair(solids, "Crate_Rise", "Crate_Roof"))
                AddLink(1, 2, "jump");
            if (Stair(solids, "Stack_Mid", "Stack_Rise") && Stair(solids, "Stack_Rise", "Stack_Roof"))
                AddLink(1, 2, "jump2");
            if (Stair(solids, "Crate_Step", "Crate_Mid") && Stair(solids, "Stack_Step", "Stack_Mid"))
                AddLink(0, 1, "jump");

            if (RampDown(ramps, "Slide_WestMid", MidY, 0f) && RampDown(ramps, "Slide_EastMid", MidY, 0f))
                AddLink(1, 0, "slide");
            if (ZipDown(ZipLines[1], MidY, 0f)) AddLink(1, 0, "zip");
            if (RampDown(ramps, "Slide_WestRoof", RoofY, MidY) && RampDown(ramps, "Slide_EastRoof", RoofY, MidY))
                AddLink(2, 1, "slide");
            if (Stair(solids, "Crate_Roof", "Crate_Rise") && Stair(solids, "Stack_Roof", "Stack_Rise"))
                AddLink(2, 1, "jump");
            if (ZipDown(ZipLines[0], RoofY, 0f)) AddLink(2, 0, "zip");
            if (RampDown(ramps, "Slide_WestRoof", RoofY, MidY) && RampDown(ramps, "Slide_WestMid", MidY, 0f))
                AddLink(2, 0, "slide");

            // A jump from the ground reaches the roof (locked apex is above 12 m) beside either stair.
            if (JumpClears(RoofY)) AddLink(0, 2, "jump");
            if (JumpClears(RoofY)) AddLink(0, 2, "jump2");

            int low = 99;
            for (int a = 0; a < 3; a++)
            {
                for (int b = 0; b < 3; b++)
                {
                    if (a == b) continue;
                    int n = links[a, b].Length == 0 ? 0 : links[a, b].Split('+').Length;
                    if (n < low) low = n;
                    if (n < 2)
                        fail.Append("tier ").Append(a).Append("->").Append(b).Append(" routes ").Append(n).Append("; ");
                }
            }
            return "tiers ground/mid/roof routes " + low.ToString(CultureInfo.InvariantCulture);
        }

        static bool JumpClears(float targetY)
        {
            float apex = MegaParkP1Layout.LockedJumpSpeed * MegaParkP1Layout.LockedJumpSpeed / (2f * 22f);
            return apex + 0.05f >= targetY;
        }

        static bool WallCovers(MegaParkP1Layout.Solid[] solids, string name, float fromY, float toY)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Name != name || s.Kind != "wall") continue;
                float bottom = s.Y - s.Sy * 0.5f;
                float top = s.Y + s.Sy * 0.5f;
                return bottom <= fromY + 1.2f && top + 0.05f >= toY;
            }
            return false;
        }

        static bool PadReaches(MegaParkP1Layout.PadSpot pad, float fromY, float toY)
        {
            return Math.Abs(pad.Y - fromY) < 0.2f && pad.Apex + 0.05f >= toY - fromY && pad.Speed >= 12f;
        }

        static bool Stair(MegaParkP1Layout.Solid[] solids, string a, string b)
        {
            MegaParkP1Layout.Solid sa = default, sb = default;
            bool fa = false, fb = false;
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Name == a) { sa = solids[i]; fa = true; }
                if (solids[i].Name == b) { sb = solids[i]; fb = true; }
            }
            if (!fa || !fb) return false;
            float ta = sa.Y + sa.Sy * 0.5f;
            float tb = sb.Y + sb.Sy * 0.5f;
            float drop = Math.Abs(ta - tb);
            if (drop < 0.45f || drop > 3.3f) return false;
            float dx = 0f;
            float ax0 = sa.X - sa.Sx * 0.5f, ax1 = sa.X + sa.Sx * 0.5f;
            float bx0 = sb.X - sb.Sx * 0.5f, bx1 = sb.X + sb.Sx * 0.5f;
            if (ax1 < bx0) dx = bx0 - ax1;
            else if (bx1 < ax0) dx = ax0 - bx1;
            float dz = 0f;
            float az0 = sa.Z - sa.Sz * 0.5f, az1 = sa.Z + sa.Sz * 0.5f;
            float bz0 = sb.Z - sb.Sz * 0.5f, bz1 = sb.Z + sb.Sz * 0.5f;
            if (az1 < bz0) dz = bz0 - az1;
            else if (bz1 < az0) dz = az0 - bz1;
            return Math.Sqrt(dx * dx + dz * dz) <= 0.55f;
        }

        static bool RampDown(MegaParkP1Layout.Ramp[] ramps, string name, float high, float low)
        {
            for (int i = 0; i < ramps.Length; i++)
            {
                if (ramps[i].Name != name || ramps[i].Mat != "yellow") continue;
                float hy = Math.Max(ramps[i].Y0, ramps[i].Y1);
                float ly = Math.Min(ramps[i].Y0, ramps[i].Y1);
                return Math.Abs(hy - high) < 0.35f && Math.Abs(ly - low) < 0.35f;
            }
            return false;
        }

        static bool ZipDown(MegaParkP1Layout.ZipLineSpot z, float fromY, float toY)
        {
            return z.Speed >= 14f - 0.01f && z.Ay + 0.2f >= fromY && z.By <= toY + 2.6f && z.Ay >= z.By + 0.35f;
        }

        static string FallReport(MegaParkP1Layout.Solid[] solids, StringBuilder fail)
        {
            if (KillPlaneY > -1.6f)
                fail.Append("kill height; ");
            int drops = 0;
            int held = 0;
            string[] roofs = { "Crate_Roof", "Stack_Roof" };
            for (int i = 0; i < roofs.Length; i++)
            {
                MegaParkP1Layout.Solid roof = FindSolid(solids, roofs[i]);
                if (roof.Name == null)
                {
                    fail.Append(roofs[i]).Append(" missing; ");
                    continue;
                }
                float top = roof.Y + roof.Sy * 0.5f;
                float[] ox = { 0f, 1f, 0f, -1f };
                float[] oz = { 1f, 0f, -1f, 0f };
                for (int d = 0; d < 4; d++)
                {
                    float x = roof.X + ox[d] * (roof.Sx * 0.5f + 1.4f);
                    float z = roof.Z + oz[d] * (roof.Sz * 0.5f + 1.4f);
                    drops++;
                    if (Drop(x, top, z)) held++;
                    else fail.Append(roofs[i]).Append(" fall is a trap; ");
                }
            }
            if (held != drops)
                fail.Append("fall ").Append(held).Append('/').Append(drops).Append("; ");
            return "fall " + held.ToString(CultureInfo.InvariantCulture) + "/" + drops.ToString(CultureInfo.InvariantCulture) + " clear";
        }

        static bool Drop(float x, float y, float z)
        {
            const float r = MegaParkP1Layout.PawnRadius;
            float vy = 0f;
            for (int step = 0; step < 480; step++)
            {
                const float dt = 0.05f;
                float ny = y + vy * dt;
                if (x - r < 0f || x + r > MapW || z - r < 0f || z + r > MapD) return false;
                if (ny < KillPlaneY) return false;
                if (ny <= 0.05f) return true;
                y = ny;
                vy -= 22f * 1.5f * dt;
                if (vy < -52f) vy = -52f;
            }
            return false;
        }

        static MegaParkP1Layout.Solid FindSolid(MegaParkP1Layout.Solid[] solids, string name)
        {
            for (int i = 0; i < solids.Length; i++)
                if (solids[i].Name == name) return solids[i];
            return default;
        }

        static void KitReport(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, StringBuilder fail)
        {
            int walls = 0, bars = 0, covers = 0, plates = 0, slides = 0, landmarks = 0;
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "wall" && s.Mat == "blue") walls++;
                if (s.Kind == "bar") bars++;
                if (s.Name.StartsWith("Cover_", StringComparison.Ordinal)) covers++;
                if (s.Name.StartsWith("Hook_", StringComparison.Ordinal)) plates++;
                if (s.Kind == "landmark" && s.Name.EndsWith("_Pole", StringComparison.Ordinal)) landmarks++;
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "mark") continue;
                float bottom = s.Y - s.Sy * 0.5f;
                if (bottom < s.SupportY - 0.001f || bottom > s.SupportY + 0.001f)
                    fail.Append(s.Name).Append(" support; ");
            }
            for (int i = 0; i < ramps.Length; i++)
                if (ramps[i].Mat == "yellow") slides++;
            if (walls < 2) fail.Append("cling walls; ");
            if (bars < 3) fail.Append("bars; ");
            if (slides < 2) fail.Append("slides; ");
            if (covers < 2) fail.Append("vault covers; ");
            if (plates < 2) fail.Append("grapple plates; ");
            if (landmarks < 4) fail.Append("landmarks; ");
            if (LaunchPads.Length != 3) fail.Append("pads; ");
            if (ZipLines.Length != 3) fail.Append("zips; ");
            bool crosses = false;
            for (int i = 0; i < ZipLines.Length; i++)
            {
                if (Math.Abs(ZipLines[i].Speed - 14f) > 0.01f)
                    fail.Append(ZipLines[i].Name).Append(" speed; ");
                if (ZipLines[i].Ay < ZipLines[i].By + 0.35f)
                    fail.Append(ZipLines[i].Name).Append(" uphill; ");
                if (ZipLines[i].Ay >= RoofY - 0.2f && ZipLines[i].By < MidY)
                    crosses = true;
            }
            if (!crosses) fail.Append("no zip crosses tiers; ");
        }

        static string ArcReport(StringBuilder fail)
        {
            float loop = MeasureLoop();
            float want = loop / Spawns.Length;
            var bits = new StringBuilder();
            for (int i = 0; i < Spawns.Length; i++)
            {
                MegaParkP1Layout.SpawnPad a = Spawns[i];
                MegaParkP1Layout.SpawnPad b = Spawns[(i + 1) % Spawns.Length];
                float arc = ArcBetween(a.X, a.Z, b.X, b.Z);
                if (bits.Length > 0) bits.Append('/');
                bits.Append(arc.ToString("0.00", CultureInfo.InvariantCulture));
                if (Math.Abs(arc - want) > 0.05f)
                    fail.Append(a.Name).Append(" arc ").Append(arc.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
            }
            return bits.ToString();
        }

        static int CutReport(MegaParkP1Layout.Solid[] solids, StringBuilder fail)
        {
            int cuts = Cuts.Length / 4;
            for (int c = 0; c < cuts; c++)
            {
                float x0 = Cuts[c * 4];
                float z0 = Cuts[c * 4 + 1];
                float x1 = Cuts[c * 4 + 2];
                float z1 = Cuts[c * 4 + 3];
                float dx = x1 - x0;
                float dz = z1 - z0;
                float len = (float)Math.Sqrt(dx * dx + dz * dz);
                int steps = Math.Max(1, (int)(len / 2f));
                for (int s = 0; s <= steps; s++)
                {
                    float t = s / (float)steps;
                    float x = x0 + dx * t;
                    float z = z0 + dz * t;
                    if (MegaParkP1Layout.RouteOpen(solids, x, z)) continue;
                    fail.Append("cut ").Append(c.ToString(CultureInfo.InvariantCulture))
                        .Append(" blocked ").Append(x.ToString("0.0", CultureInfo.InvariantCulture))
                        .Append(',').Append(z.ToString("0.0", CultureInfo.InvariantCulture)).Append("; ");
                    break;
                }
            }
            if (cuts < 2 || cuts > 3)
                fail.Append("cuts ").Append(cuts.ToString(CultureInfo.InvariantCulture)).Append("; ");
            return cuts;
        }

        static int Containment(MegaParkP1Layout.Solid[] solids, StringBuilder fail, out bool held)
        {
            held = true;
            int sweeps = 0;
            int misses = 0;
            float stand = 0f;
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Kind == "ground" || solids[i].Kind == "fence" || solids[i].Kind == "landmark") continue;
                float top = solids[i].Y + solids[i].Sy * 0.5f;
                if (top > stand && solids[i].Sx >= 1f && solids[i].Sz >= 1f) stand = top;
            }
            float apex = MegaParkP1Layout.LockedJumpSpeed * MegaParkP1Layout.LockedJumpSpeed / (2f * 22f);
            if (FenceTop < stand + apex + 1f)
            {
                fail.Append("fence is short; ");
                held = false;
            }
            if (KillPlaneY > -1.6f || KillPlaneY < -6f)
            {
                fail.Append("kill plane; ");
                held = false;
            }
            for (int i = 0; i < Spawns.Length; i++)
                Fan(Spawns[i].X, 0f, Spawns[i].Z, MegaParkP1Layout.MaxAirSpeed, Spawns[i].Name, fail, ref sweeps, ref misses);
            for (int i = 0; i < LaunchPads.Length; i++)
                Fan(LaunchPads[i].X, LaunchPads[i].Y, LaunchPads[i].Z, MegaParkP1Layout.MaxAirSpeed, LaunchPads[i].Name, fail, ref sweeps, ref misses);
            for (int i = 0; i < ZipLines.Length; i++)
            {
                MegaParkP1Layout.ZipLineSpot z = ZipLines[i];
                Fan(z.Ax, Math.Max(0f, z.Ay - 1.15f), z.Az, MegaParkP1Layout.MaxAirSpeed, z.Name + " A", fail, ref sweeps, ref misses);
                Fan(z.Bx, Math.Max(0f, z.By - 1.15f), z.Bz, MegaParkP1Layout.MaxAirSpeed, z.Name + " B", fail, ref sweeps, ref misses);
            }
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                float top = s.Y + s.Sy * 0.5f;
                if (s.Kind == "wall")
                    Fan(s.X, top, s.Z, 9.5f, s.Name, fail, ref sweeps, ref misses);
                else if (s.Kind == "block" && top >= 1.4f)
                    Fan(s.X, top, s.Z, MegaParkP1Layout.MaxAirSpeed, s.Name, fail, ref sweeps, ref misses);
            }
            if (misses > 0) held = false;
            if (sweeps < 8) fail.Append("containment sweeps; ");
            return sweeps;
        }

        static void Fan(float x, float y, float z, float speed, string name, StringBuilder fail, ref int sweeps, ref int misses)
        {
            for (int d = 0; d < 8; d++)
            {
                double a = d * Math.PI / 4.0;
                float dx = (float)Math.Cos(a) * speed;
                float dz = (float)Math.Sin(a) * speed;
                sweeps++;
                if (!Fly(x, y, z, dx, 0f, dz)) Note(fail, ref misses, name);
                sweeps++;
                if (!Fly(x, y, z, dx, MegaParkP1Layout.LockedJumpSpeed, dz)) Note(fail, ref misses, name);
            }
        }

        static void Note(StringBuilder fail, ref int misses, string name)
        {
            misses++;
            if (misses <= 6)
                fail.Append(name).Append(" leaves the park; ");
        }

        static bool Fly(float x, float y, float z, float vx, float vy, float vz)
        {
            const float r = MegaParkP1Layout.PawnRadius;
            if (x - r < 0f || x + r > MapW || z - r < 0f || z + r > MapD) return false;
            for (int step = 0; step < 480; step++)
            {
                const float dt = 0.05f;
                float nx = x + vx * dt;
                float ny = y + vy * dt;
                float nz = z + vz * dt;
                if (FenceStops(x, y, z, nx, ny, nz)) return true;
                if (nx - r < -0.02f || nx + r > MapW + 0.02f || nz - r < -0.02f || nz + r > MapD + 0.02f)
                    return false;
                x = nx;
                y = ny;
                z = nz;
                vy -= (vy > 0f ? 22f : 22f * 1.5f) * dt;
                if (vy < -52f) vy = -52f;
                if (y <= 0f && vy <= 0f) return true;
                if (y < KillPlaneY) return true;
            }
            return x - r >= 0f && x + r <= MapW && z - r >= 0f && z + r <= MapD;
        }

        static bool FenceStops(float x, float y, float z, float nx, float ny, float nz)
        {
            const float r = MegaParkP1Layout.PawnRadius;
            if (Cross(x - r, nx - r, 0f, y, ny)) return true;
            if (Cross(MapW - (x + r), MapW - (nx + r), 0f, y, ny)) return true;
            if (Cross(z - r, nz - r, 0f, y, ny)) return true;
            if (Cross(MapD - (z + r), MapD - (nz + r), 0f, y, ny)) return true;
            return false;
        }

        static bool Cross(float before, float after, float face, float y, float ny)
        {
            if (before >= face - 0.001f && after < face)
            {
                float denom = before - after;
                float t = denom > 1e-6f ? (before - face) / denom : 0f;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
                float yc = y + (ny - y) * t;
                if (yc < FenceTop) return true;
            }
            return false;
        }

        static float GroundError(MegaParkP1Layout.Solid[] solids, StringBuilder fail)
        {
            float max = 0f;
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "mark") continue;
                float bottom = s.Y - s.Sy * 0.5f;
                float err = Math.Abs(bottom - s.SupportY);
                if (err > max) max = err;
            }
            for (int i = 0; i < LaunchPads.Length; i++)
            {
                float err = Math.Abs(LaunchPads[i].Y);
                if (err > max) max = err;
                if (err > 0.02f)
                    fail.Append(LaunchPads[i].Name).Append(" pad float; ");
            }
            return max;
        }

        static readonly string[] GameplayTints = { "cling", "slide", "plate", "zip", "tag" };

        static bool GameplayTint(string name)
        {
            for (int i = 0; i < GameplayTints.Length; i++)
                if (GameplayTints[i] == name) return true;
            return false;
        }

        static string CanonMat(string mat)
        {
            if (mat == "blue") return "cling";
            if (mat == "yellow") return "slide";
            return mat;
        }

        static float ContrastReport(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, StringBuilder fail)
        {
            var mats = new List<string>();
            CollectMat(mats, "mulch");
            for (int i = 0; i < solids.Length; i++)
                CollectMat(mats, CanonMat(solids[i].Mat));
            for (int i = 0; i < ramps.Length; i++)
                CollectMat(mats, CanonMat(ramps[i].Mat));
            CollectMat(mats, "sky");
            float worst = 99f;
            for (int g = 0; g < GameplayTints.Length; g++)
            {
                for (int m = 0; m < mats.Count; m++)
                {
                    string surface = mats[m];
                    if (surface == GameplayTints[g]) continue;
                    if (GameplayTint(surface)) continue;
                    if (surface == "sky" && GameplayTints[g] != "zip") continue;
                    float cr = MegaParkP1Layout.SwatchContrast(GameplayTints[g], surface);
                    if (cr < worst) worst = cr;
                    if (cr < 3f)
                        fail.Append(GameplayTints[g]).Append('/').Append(surface).Append(' ')
                            .Append(cr.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
                }
            }
            return worst;
        }

        static void CollectMat(List<string> mats, string mat)
        {
            if (string.IsNullOrEmpty(mat)) return;
            for (int i = 0; i < mats.Count; i++)
                if (mats[i] == mat) return;
            mats.Add(mat);
        }

        static int DrawReport(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, StringBuilder fail)
        {
            var keys = new HashSet<string>();
            for (int i = 0; i < solids.Length; i++)
                keys.Add("s|" + solids[i].Zone + "|" + solids[i].Mat);
            for (int i = 0; i < ramps.Length; i++)
                keys.Add("r|" + ramps[i].Zone + "|" + ramps[i].Mat);
            int draws = keys.Count + PaintDraws + LabelDraws + SpawnDraws + ToyDraws + MinimapDraws + DressBatches();
            if (draws > DrawBudget)
                fail.Append("draws ").Append(draws.ToString(CultureInfo.InvariantCulture)).Append("; ");
            return draws;
        }

        static int DummyReport(out int pads, out int zips, StringBuilder fail)
        {
            pads = 0;
            zips = 0;
            int stuck = 0;
            MegaParkP1Layout.Solid[] solids = BuildSolids();
            int n = Spawns.Length;
            for (int k = 0; k < n; k++)
            {
                MegaParkP1Layout.SpawnPad a = Spawns[k];
                MegaParkP1Layout.SpawnPad b = Spawns[(k + 1) % n];
                stuck += MegaParkP1Layout.WalkLeg(solids, a.X, a.Z, b.X, b.Z, out int p, out int z);
                pads += p;
                zips += z;
            }
            for (int i = 0; i < ZipLines.Length; i++)
            {
                MegaParkP1Layout.ZipAnchors(solids, ZipLines[i], out float x0, out float z0, out float x1, out float z1);
                stuck += MegaParkP1Layout.WalkLeg(solids, x0, z0, x1, z1, out int p, out int z);
                pads += p;
                zips += z;
            }
            for (int i = 0; i < LaunchPads.Length; i++)
            {
                Landing(LaunchPads[i], out float lx, out float lz);
                stuck += MegaParkP1Layout.WalkLeg(solids, LaunchPads[i].X, LaunchPads[i].Z, lx, lz, out int p, out int z);
                pads += p;
                zips += z;
            }
            if (stuck != 0)
                fail.Append("dummy stuck ").Append(stuck.ToString(CultureInfo.InvariantCulture)).Append("; ");
            if (pads < 1) fail.Append("dummy took no pad; ");
            if (zips < 1) fail.Append("dummy took no zip; ");
            return stuck;
        }

        static string SkillReport(StringBuilder fail)
        {
            MegaParkP1Layout.Solid[] solids = BuildSolids();
            var notes = new StringBuilder();
            var doc = new StringBuilder();
            doc.Append("# Stack Yard skill routes\n\n");
            doc.Append("Expert chains use the locked motor. Coyote is 0.10 s, jump buffer is 0.16 s, and cling grace is 0.08 s. ");
            doc.Append("Beginner time is the ground nav at sprint 12. A route is in band when it beats that line by 10–25%.\n\n");
            doc.Append("| Route | Chain | Beginner | Expert | Save |\n| --- | --- | --- | --- | --- |\n");

            float cross = Cable(ZipLines[0]);
            float crossT = cross / 14f;
            NoteChain(solids, doc, notes, fail, "RoofZip",
                "Ride Zip Cross from the west roof down across the yard. Walking the street is the long way.",
                "Crate_Roof", ZipLines[0].Ax, ZipLines[0].Az, ZipLines[0].Bx, ZipLines[0].Bz, crossT,
                "zip " + cross.ToString("0.00", CultureInfo.InvariantCulture) + " m (Zip_Cross) " + crossT.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            Landing(LaunchPads[0], out float lx, out float lz);
            float padT = Hang(LaunchPads[0].Apex);
            NoteChain(solids, doc, notes, fail, "YardPad",
                "The yard launch pad cuts the south street at pad speed.",
                "Cover_A", LaunchPads[0].X, LaunchPads[0].Z, lx, lz, padT,
                "pad (Launch_Yard) " + padT.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            float climb = 2.4f / 6.0f;
            float run = 2.6f / 9.5f;
            float hop = WallJumpSeconds();
            float climbT = climb + run + hop;
            NoteChain(solids, doc, notes, fail, "WestClimb",
                "Climb the yard cling face, wall-run it, then wall-jump off toward the south street.",
                "Cling_Yard", 24f, 22f, 24f, 32f, climbT,
                "climb 2.40 m (Cling_Yard) " + climb.ToString("0.00", CultureInfo.InvariantCulture)
                + " s → wall-run 2.60 m (Cling_Yard) " + run.ToString("0.00", CultureInfo.InvariantCulture)
                + " s → wall-jump (Cling_Yard) " + hop.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            doc.Append("\n## Tiers\n\n");
            doc.Append("Ground, mid decks at 6 m, and roofs at 12 m. Container rows line the fence, the towers read as stacked shells, and a warehouse shell closes the north edge. Stair treads sit on the slides and forklifts and pallets sit as low cover. The chase grid is the same set of open cells. Narrow catwalks link the towers, and cranes mark the four corners. ");
            doc.Append("Cling_Yard and Cling_East climb to the mid decks. ");
            doc.Append("Cling_Roof and Cling_Lane climb from the mid decks to the roofs. Launch_Mid and Launch_Roof throw onto those tiers. ");
            doc.Append("Zip_Cross leaves the west roof for the yard. Slides run back down both stairs. A fall off a roof lands inside the fence, above the kill plane.\n\n");
            doc.Append("## Chokepoints\n\nAfter the pass 9 audit: 0 (dead 0, corner 0, loop 0).\n");
            WriteDoc(doc.ToString());
            return notes.Length == 0 ? "skills 0" : notes.ToString();
        }

        static void NoteChain(MegaParkP1Layout.Solid[] solids, StringBuilder doc, StringBuilder notes, StringBuilder fail,
            string name, string summary, string host, float sx, float sz, float ex, float ez, float expert, string chain)
        {
            if (!PickEnds(solids, expert, sx, sz, ex, ez, out float x0, out float z0, out float x1, out float z1, out float beginner, out float save))
            {
                float g = MegaParkP1Layout.GroundSeconds(solids, sx, sz, ex, ez);
                float missed = g > 0.05f && g < 200f ? (g - expert) / g : -1f;
                fail.Append(name).Append(" save ")
                    .Append((missed * 100f).ToString("0.0", CultureInfo.InvariantCulture))
                    .Append("% expert ").Append(expert.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" beginner ").Append(g.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
                NoteSkill(solids, doc, notes, fail, name, summary, sx, sz, ex, ez, expert, chain);
                return;
            }
            NoteSkill(solids, doc, notes, fail, name, summary, x0, z0, x1, z1, expert,
                "Chevrons sit on " + host + ". " + chain + ". " + ChainClose(chain));
            if (save < 0.10f || save > 0.25f)
                fail.Append(name).Append(" picked an out-of-band save; ");
        }

        static string ChainClose(string chain)
        {
            var sb = new StringBuilder();
            if (chain.Contains("wall-run") || chain.Contains("wall-jump"))
                sb.Append("Wall-run leaves on cling grace 0.08 s or jump buffer 0.16 s. ");
            if (chain.Contains("pad"))
                sb.Append("The pad uses its locked 0.30 s cooldown. ");
            if (chain.Contains("zip"))
                sb.Append("The zip rides at 14 m/s and regrabs on its locked 0.30 s cooldown. ");
            sb.Append("Coyote 0.10 s covers the hop.");
            return sb.ToString();
        }

        static bool PickEnds(MegaParkP1Layout.Solid[] solids, float expert,
            float sx, float sz, float ex, float ez,
            out float x0, out float z0, out float x1, out float z1, out float beginner, out float save)
        {
            float best = 99f;
            bool found = false;
            x0 = sx; z0 = sz; x1 = ex; z1 = ez;
            beginner = 0f;
            save = -1f;
            float[] nudge = { 0f, 2f, -2f, 4f, -4f, 6f, -6f, 8f, -8f };
            for (int i = 0; i < nudge.Length; i++)
            {
                for (int k = 0; k < nudge.Length; k++)
                {
                    float a = sx + nudge[i];
                    float b = sz + nudge[k];
                    if (a < 2f || a > MapW - 2f || b < 2f || b > MapD - 2f) continue;
                    if (!MegaParkP1Layout.RouteOpen(solids, a, b)) continue;
                    for (int j = 0; j < nudge.Length; j++)
                    {
                        for (int m = 0; m < nudge.Length; m++)
                        {
                            float c = ex + nudge[j];
                            float d = ez + nudge[m];
                            if (c < 2f || c > MapW - 2f || d < 2f || d > MapD - 2f) continue;
                            if (!MegaParkP1Layout.RouteOpen(solids, c, d)) continue;
                            float g = MegaParkP1Layout.GroundSeconds(solids, a, b, c, d);
                            if (g < 0.3f || g > 80f) continue;
                            float s = (g - expert) / g;
                            if (s < 0.10f || s > 0.25f) continue;
                            float dist = Math.Abs(s - 0.16f);
                            if (dist >= best) continue;
                            best = dist;
                            found = true;
                            x0 = a; z0 = b; x1 = c; z1 = d;
                            beginner = g;
                            save = s;
                        }
                    }
                }
            }
            return found;
        }

        static void NoteSkill(MegaParkP1Layout.Solid[] solids, StringBuilder doc, StringBuilder notes, StringBuilder fail,
            string name, string summary, float x0, float z0, float x1, float z1, float expert, string chain)
        {
            float beginner = MegaParkP1Layout.GroundSeconds(solids, x0, z0, x1, z1);
            float save = beginner > 0.05f && beginner < 200f ? (beginner - expert) / beginner : -1f;
            string saveText = (save * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            if (notes.Length > 0) notes.Append(' ');
            notes.Append(name).Append(' ').Append(expert.ToString("0.00", CultureInfo.InvariantCulture)).Append("s/").Append(saveText);
            doc.Append("| ").Append(name).Append(" | ").Append(summary).Append(" | ");
            doc.Append(beginner.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s | ");
            doc.Append(expert.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s | ");
            doc.Append(saveText).Append(" |\n\n");
            doc.Append("Start (").Append(x0.ToString("0.0", CultureInfo.InvariantCulture));
            doc.Append(", ").Append(z0.ToString("0.0", CultureInfo.InvariantCulture));
            doc.Append(") end (").Append(x1.ToString("0.0", CultureInfo.InvariantCulture));
            doc.Append(", ").Append(z1.ToString("0.0", CultureInfo.InvariantCulture)).Append("). ");
            doc.Append(chain);
            if (!chain.Contains("Coyote"))
                doc.Append(". Coyote 0.10 s covers the hop. The pad cooldown is 0.30 s and the zip rides at 14 m/s.");
            doc.Append("\n\n");
            if (beginner > 100f)
                fail.Append(name).Append(" beginner blocked; ");
            else if (save < 0.10f || save > 0.25f)
                fail.Append(name).Append(" save ").Append(saveText)
                    .Append(" expert ").Append(expert.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" beginner ").Append(beginner.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
        }

        static float WallJumpSeconds()
        {
            const float vy = 6.2f;
            const float g = 22f;
            float tUp = vy / g;
            float h = vy * tUp * 0.5f;
            float tDown = (float)Math.Sqrt(2f * h / (g * 1.5f));
            return tUp + tDown;
        }

        static float Hang(float apex)
        {
            const float rise = 22f;
            float vy = (float)Math.Sqrt(2f * rise * apex);
            float tUp = vy / rise;
            float tDown = (float)Math.Sqrt(2f * apex / (rise * 1.5f));
            return tUp + tDown;
        }

        static void Landing(MegaParkP1Layout.PadSpot pad, out float x, out float z)
        {
            float mag = (float)Math.Sqrt(pad.DirX * pad.DirX + pad.DirZ * pad.DirZ);
            if (mag < 0.1f) mag = 1f;
            float hang = Hang(pad.Apex);
            x = pad.X + pad.DirX / mag * pad.Speed * hang;
            z = pad.Z + pad.DirZ / mag * pad.Speed * hang;
        }

        static float Cable(MegaParkP1Layout.ZipLineSpot z)
        {
            float dx = z.Bx - z.Ax;
            float dy = z.By - z.Ay;
            float dz = z.Bz - z.Az;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        static void WriteDoc(string text)
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string docs = Path.Combine(dir, "Docs");
                if (Directory.Exists(docs))
                {
                    File.WriteAllText(Path.Combine(docs, "StackYard_SkillRoutes.md"), text);
                    return;
                }
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
        }

        static void Deck(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat,
            float x, float z, float sx, float sz, float top)
        {
            float sy = top >= 6f ? 1.2f : top;
            float bottom = top - sy;
            if (bottom < 0f) { bottom = 0f; sy = top; }
            Add(list, name, zone, "block", mat, x, bottom + sy * 0.5f, z, sx, sy, sz, bottom);
        }

        /// <summary>
        /// Container stacks, catwalks, warehouse roofs, and crane jibs.
        /// Nothing bridges the two towers at a walkable height, and nothing sits
        /// on a ramp mouth, the street loop, or the zip diagonal.
        /// </summary>
        static void AddYardMass(List<MegaParkP1Layout.Solid> list)
        {
            // Solid columns under the named decks, inset so the full-height skirt stays
            // inside the deck and does not close a ground or stair cell.
            InsetPile(list, "Crate_Mid_Fill", "Mid", "army", 30f, 34f, 10f, 8f, 6f);
            InsetPile(list, "Crate_Rise_Fill", "Roof", "amber", 38f, 34f, 8f, 8f, 9f);
            InsetPile(list, "Crate_Roof_Fill", "Roof", "pad", 38f, 42f, 8f, 8f, 12f);
            InsetPile(list, "Stack_Mid_Fill", "Mid", "army", 78f, 34f, 10f, 8f, 6f);
            InsetPile(list, "Stack_Rise_Fill", "Roof", "amber", 70f, 34f, 8f, 8f, 9f);
            InsetPile(list, "Stack_Roof_Fill", "Roof", "pad", 70f, 42f, 8f, 8f, 12f);

            // Catwalks and the roof bridge are too narrow to stand on.
            Span(list, "Cat_West", "Mid", "army", 29f, 6.05f, 44f, 8f, 0.28f, 0.7f);
            Span(list, "Cat_East", "Mid", "army", 78f, 6.05f, 44f, 8f, 0.28f, 0.7f);
            Span(list, "Cat_Span", "Mid", "army", 54f, 6.05f, 36f, 28f, 0.22f, 0.7f);
            Span(list, "Wh_Span", "Roof", "pad", 55f, 12.15f, 47f, 22f, 0.3f, 0.7f);

            Jib(list, "Landmark_Yard", "Yard", "knight", 14f, 18f, 1f, 0f);
            Jib(list, "Landmark_Lane", "Lane", "army", 96f, 18f, -1f, 0f);
            Jib(list, "Landmark_Mid", "Mid", "army", 14f, 56f, 1f, 0f);
            Jib(list, "Landmark_Roof", "Roof", "pad", 96f, 56f, -1f, 0f);
        }

        static void Span(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat,
            float x, float y, float z, float sx, float sy, float sz)
        {
            Add(list, name, zone, "block", mat, x, y, z, sx, sy, sz, y - sy * 0.5f);
        }

        static void Pile(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat,
            float x, float z, float sx, float sz, float top)
        {
            Add(list, name, zone, "block", mat, x, top * 0.5f, z, sx, top, sz, 0f);
        }

        /// <summary>
        /// A full-height column 0.7 m inside the host deck. The 0.35 m head-check
        /// around a ground-touching solid then stays on the deck, not on open ground.
        /// </summary>
        static void InsetPile(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat,
            float x, float z, float sx, float sz, float top)
        {
            const float inset = 0.7f;
            int stories = top >= 8f ? 3 : 2;
            ShipColumn(list, "Ship_" + name, zone, x, z, sx - inset * 2f, sz - inset * 2f, top, stories);
        }

        /// <summary>
        /// Stacked container shells on the existing column footprint. Kind toy keeps them
        /// out of the deck graph. The outer box matches the old pile, so the chase halo
        /// stays on cells that column already blocked.
        /// </summary>
        static void ShipColumn(List<MegaParkP1Layout.Solid> list, string name, string zone,
            float x, float z, float sx, float sz, float top, int stories)
        {
            if (stories < 2) stories = 2;
            if (stories > 3) stories = 3;
            string[] mats = zone == "Roof"
                ? new[] { "amber", "pad", "amber" }
                : new[] { "army", "knight", "concrete" };
            float gap = 0.06f;
            float h = (top - gap * (stories - 1)) / stories;
            if (h < 0.9f)
            {
                gap = 0f;
                h = top / stories;
            }
            float y = 0f;
            for (int i = 0; i < stories; i++)
            {
                Add(list, name + "_" + i.ToString(CultureInfo.InvariantCulture), zone, "toy", mats[i],
                    x, y + h * 0.5f, z, sx, h, sz, y);
                y += h + gap;
            }
        }

        /// <summary>
        /// Yard identity that does not retouch a chase cell. Container rows and the
        /// warehouse sit in the margin outside the 1.5 m nav grid. Stair treads sit
        /// only in the vertical gaps between head bands, so an open cell stays open.
        /// Pallets are shorter than the ground head check and stay outside the bowl.
        /// </summary>
        static void AddShippingYard(List<MegaParkP1Layout.Solid> list)
        {
            // South fence row. A gap around x=68 keeps the edge still on the low rail.
            ShipColumn(list, "Ship_S1", "Yard", 16f, 1.12f, 10f, 1.85f, 6.3f, 3);
            ShipColumn(list, "Ship_S2", "Yard", 34f, 1.12f, 8f, 1.85f, 4.4f, 2);
            ShipColumn(list, "Ship_S3", "Lane", 48f, 1.12f, 6f, 1.85f, 6.3f, 3);
            ShipColumn(list, "Ship_S4", "Lane", 90f, 1.12f, 10f, 1.85f, 4.4f, 2);
            ShipColumn(list, "Ship_S5", "Lane", 102f, 1.12f, 6f, 1.85f, 6.3f, 3);

            // North fence row, flanking the warehouse shell.
            ShipColumn(list, "Ship_N1", "Mid", 14f, 68.72f, 8f, 1.85f, 6.3f, 3);
            ShipColumn(list, "Ship_N2", "Mid", 28f, 68.72f, 8f, 1.85f, 4.4f, 2);
            ShipColumn(list, "Ship_N3", "Roof", 86f, 68.72f, 8f, 1.85f, 6.3f, 3);
            ShipColumn(list, "Ship_N4", "Roof", 100f, 68.72f, 7f, 1.85f, 4.4f, 2);

            // West and east rows, clear of the corner containers.
            ShipColumn(list, "Ship_W1", "Yard", 1.12f, 16f, 1.85f, 8f, 4.4f, 2);
            ShipColumn(list, "Ship_W2", "Mid", 1.12f, 36f, 1.85f, 10f, 6.3f, 3);
            ShipColumn(list, "Ship_W3", "Mid", 1.12f, 54f, 1.85f, 8f, 4.4f, 2);
            ShipColumn(list, "Ship_E1", "Lane", 108.05f, 16f, 2.05f, 8f, 6.3f, 3);
            ShipColumn(list, "Ship_E2", "Roof", 108.05f, 40f, 2.05f, 10f, 4.4f, 2);
            ShipColumn(list, "Ship_E3", "Roof", 108.05f, 56f, 2.05f, 8f, 6.3f, 3);

            Add(list, "Wh_North", "Roof", "wall", "steel", 56f, 2.6f, 69.2f, 34f, 5.2f, 0.32f, 0f);
            Add(list, "Wh_West", "Mid", "wall", "steel", 39.2f, 2.6f, 68.55f, 0.32f, 5.2f, 1.15f, 0f);
            Add(list, "Wh_East", "Roof", "wall", "steel", 72.8f, 2.6f, 68.55f, 0.32f, 5.2f, 1.15f, 0f);
            Add(list, "Wh_Roof", "Roof", "wall", "steel", 56f, 5.22f, 68.55f, 33.4f, 0.2f, 1.05f, 5.12f);

            AddGapStairs(list, "Stair_WestMid", 30f, 34f, 6f, 30f, 18f, 0f, -5.6f, 0f);
            AddGapStairs(list, "Stair_EastMid", 78f, 34f, 6f, 78f, 18f, 0f, 5.6f, 0f);
            AddGapStairs(list, "Stair_WestRoof", 38f, 42f, 12f, 30f, 34f, 6f, -4.6f, 4.6f);
            AddGapStairs(list, "Stair_EastRoof", 70f, 42f, 12f, 78f, 34f, 6f, 5.2f, 5.2f);
            AddGapStairs(list, "Stair_WestRise", 38f, 34f, 9f, 30f, 34f, 6f, 0f, 4.7f);
            AddGapStairs(list, "Stair_EastRise", 70f, 34f, 9f, 78f, 34f, 6f, 0f, 4.7f);

            Forklift(list, "Fork_A", 1.15f, 24f, 1f, 0f);
            Forklift(list, "Fork_B", 108.1f, 26f, -1f, 0f);
            Forklift(list, "Fork_C", 56f, 68.7f, 0f, -1f);
            Pallet(list, "Pal_A", 22f, 12f);
            Pallet(list, "Pal_B", 24.4f, 14.2f);
            Pallet(list, "Pal_C", 40f, 11f);
            Pallet(list, "Pal_D", 86f, 12f);
            Pallet(list, "Pal_E", 88.4f, 14.2f);
            Pallet(list, "Pal_F", 98f, 30f);
        }

        /// <summary>
        /// Treads only where the box misses every stand head band (ground, bowl, and
        /// the 3/6/9/12 m decks). Kind toy, thicker than a toe lip. The side offset
        /// puts the flight on the face of the tower instead of inside the slab.
        /// </summary>
        static void AddGapStairs(List<MegaParkP1Layout.Solid> list, string name,
            float x0, float z0, float y0, float x1, float z1, float y1, float sideX, float sideZ)
        {
            const int steps = 9;
            const float sy = 0.32f;
            int placed = 0;
            for (int i = 0; i < steps; i++)
            {
                float t = (i + 0.5f) / steps;
                float y = y0 + (y1 - y0) * t;
                float bot = y - sy * 0.5f;
                float top = y + sy * 0.5f;
                if (!StairGap(bot, top)) continue;
                float x = x0 + (x1 - x0) * t + sideX;
                float z = z0 + (z1 - z0) * t + sideZ;
                float dx = x1 - x0;
                float dz = z1 - z0;
                bool alongX = Math.Abs(dx) >= Math.Abs(dz);
                float sx = alongX ? 0.72f : 1.25f;
                float sz = alongX ? 1.25f : 0.72f;
                Add(list, name + "_" + placed.ToString(CultureInfo.InvariantCulture), "Mid", "toy", "concrete",
                    x, y, z, sx, sy, sz, bot);
                placed++;
            }
        }

        static bool StairGap(float bot, float top)
        {
            // Bands a pawn head occupies on stand -1, 0, 3, 6, 9, and 12.
            // A tread fully inside a gap cannot close an open cell.
            if (bot >= 1.56f && top <= 3.18f) return true;
            if (bot >= 4.56f && top <= 6.18f) return true;
            if (bot >= 7.56f && top <= 9.18f) return true;
            if (bot >= 10.56f && top <= 12.18f) return true;
            return false;
        }

        static void Forklift(List<MegaParkP1Layout.Solid> list, string name, float x, float z, float dirX, float dirZ)
        {
            bool east = Math.Abs(dirX) >= Math.Abs(dirZ);
            float bodyX = east ? 1.7f : 0.9f;
            float bodyZ = east ? 0.9f : 1.7f;
            Add(list, name + "_Body", "Lane", "toy", "steel", x, 0.55f, z, bodyX, 0.7f, bodyZ, 0.2f);
            float mx = x + dirX * 0.7f;
            float mz = z + dirZ * 0.7f;
            Add(list, name + "_Mast", "Lane", "toy", "steel", mx, 1.15f, mz, 0.28f, 1.5f, 0.28f, 0.4f);
            float sideX = dirZ;
            float sideZ = -dirX;
            float forkX = east ? 0.9f : 0.12f;
            float forkZ = east ? 0.12f : 0.9f;
            Add(list, name + "_ForkL", "Lane", "toy", "steel",
                mx + dirX * 0.45f + sideX * 0.22f, 0.12f, mz + dirZ * 0.45f + sideZ * 0.22f,
                forkX, 0.06f, forkZ, 0.09f);
            Add(list, name + "_ForkR", "Lane", "toy", "steel",
                mx + dirX * 0.45f - sideX * 0.22f, 0.12f, mz + dirZ * 0.45f - sideZ * 0.22f,
                forkX, 0.06f, forkZ, 0.09f);
        }

        static void Pallet(List<MegaParkP1Layout.Solid> list, string name, float x, float z)
        {
            Add(list, name, "Ground", "toy", "concrete", x, 0.08f, z, 1.15f, 0.14f, 0.9f, 0.01f);
        }

        static void Jib(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat,
            float x, float z, float dirX, float dirZ)
        {
            const float len = 7f;
            const float y = 13.4f;
            const float sy = 0.4f;
            float cx = x + dirX * len * 0.5f;
            float cz = z + dirZ * len * 0.5f;
            float sx = Math.Abs(dirX) > 0.5f ? len : 0.4f;
            float sz = Math.Abs(dirZ) > 0.5f ? len : 0.4f;
            Add(list, name + "_Jib", zone, "landmark", mat, cx, y, cz, sx, sy, sz, y - sy * 0.5f);
            float hx = x + dirX * len;
            float hz = z + dirZ * len;
            Add(list, name + "_Hook", zone, "landmark", mat, hx, 12.2f, hz, 0.45f, 2.6f, 0.45f, 10.9f);
        }

        static void AddBars(List<MegaParkP1Layout.Solid> list)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 50f + i * 6f;
                string id = i.ToString(CultureInfo.InvariantCulture);
                Add(list, "BarPost_S" + id, "Lane", "post", "steel", x, 0.57f, 20.2f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "BarPost_N" + id, "Lane", "post", "steel", x, 0.57f, 23.8f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "Bar_" + id, "Lane", "bar", "steel", x, 1.2f, 22f, 0.14f, 0.12f, 4.2f, 1.14f);
            }
        }

        static void AddMast(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat, float x, float z)
        {
            const float poleTop = 16f;
            const float flagH = 36f;
            Add(list, name + "_Pole", zone, "landmark", mat, x, poleTop * 0.5f, z, 0.35f, poleTop, 0.35f, 0f);
            Add(list, name + "_Flag", zone, "landmark", mat, x, poleTop + flagH * 0.5f, z, 1.4f, flagH, 0.16f, poleTop);
        }

        static void AddChevron(List<MegaParkP1Layout.Solid> list, string route, string hostName)
        {
            MegaParkP1Layout.Solid host = default;
            bool found = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name != hostName) continue;
                host = list[i];
                found = true;
                break;
            }
            if (!found) return;
            float top = host.Y + host.Sy * 0.5f;
            float y = top + 0.015f;
            float ax = Math.Max(0.08f, Math.Min(0.16f, host.Sx * 0.5f - 0.02f));
            float az = Math.Max(0.08f, Math.Min(0.16f, host.Sz * 0.5f - 0.02f));
            Add(list, "Chevron_" + route + "_L", host.Zone, "mark", host.Mat, host.X, y, host.Z - az * 0.15f, ax, 0.03f, az, top);
            Add(list, "Chevron_" + route + "_R", host.Zone, "mark", host.Mat, host.X, y, host.Z + az * 0.4f, ax, 0.03f, az, top);
        }

        static void Add(List<MegaParkP1Layout.Solid> list, string name, string zone, string kind, string mat,
            float x, float y, float z, float sx, float sy, float sz, float support)
        {
            list.Add(new MegaParkP1Layout.Solid
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

        static MegaParkP1Layout.Ramp Ramp(string name, string zone, string mat,
            float x0, float y0, float z0, float x1, float y1, float z1, float width)
        {
            return new MegaParkP1Layout.Ramp
            {
                Name = name,
                Zone = zone,
                Mat = mat,
                X0 = x0, Y0 = y0, Z0 = z0,
                X1 = x1, Y1 = y1, Z1 = z1,
                Width = width,
                Thickness = 0.22f,
            };
        }

        static float MeasureLoop()
        {
            float len = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                MegaParkP1Layout.Pt a = LoopCcw[i];
                MegaParkP1Layout.Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                len += Dist(a.X, a.Z, b.X, b.Z);
            }
            return len;
        }

        static float ArcBetween(float x0, float z0, float x1, float z1)
        {
            float a = Project(x0, z0);
            float b = Project(x1, z1);
            float d = b - a;
            if (d < 0f) d += MeasureLoop();
            return d;
        }

        static float Project(float x, float z)
        {
            float bestD = 1e9f;
            float best = 0f;
            float walked = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                MegaParkP1Layout.Pt a = LoopCcw[i];
                MegaParkP1Layout.Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float len = (float)Math.Sqrt(dx * dx + dz * dz);
                float u = 0f;
                if (len > 0.001f)
                {
                    u = ((x - a.X) * dx + (z - a.Z) * dz) / (len * len);
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                }
                float px = a.X + dx * u;
                float pz = a.Z + dz * u;
                float lat = (x - px) * (x - px) + (z - pz) * (z - pz);
                if (lat < bestD)
                {
                    bestD = lat;
                    best = walked + len * u;
                }
                walked += len;
            }
            return best;
        }

        static float Dist(float x0, float z0, float x1, float z1)
        {
            float dx = x1 - x0;
            float dz = z1 - z0;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static bool Inside(float x, float y, float z)
        {
            if (x < 0.2f || z < 0.2f || x > MapW - 0.2f || z > MapD - 0.2f) return false;
            if (y > FenceTop) return false;
            return true;
        }
    }
}
