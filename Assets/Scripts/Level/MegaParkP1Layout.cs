using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Mega Park playable graybox, real meters. Origin is the SW corner.
    /// Loop vertices are the P1 lock from 5ba0cfc (472 m CCW). Zone shells are graybox
    /// cubes so each collider matches its mesh. This type has no UnityEngine dependency
    /// so the headless sim can audit grounding, spawns, and the sprint-12 timing.
    ///
    /// Traversal pass: cling wall-jump chain, bar vault rhythm, and one elevated rim
    /// from the soft-play decks through slide mountain to the twin forts.
    /// Vertical pass: slide and fort decks each have two ways down (chute, drop,
    /// spiral, crawl). The +5 tower stays on the slide rim. Motor numbers are not
    /// stored here and are not retuned.
    /// Pass 4: collider matches the mesh, props sit flush, grapple faces stay on
    /// a local approach, crossings clear a capsule, and launch pads land on a floor.
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

        // Play.unity Player CharacterController. Crossings clear this capsule.
        // Step is the prefab value. A lip under this can be walked; a snag cannot.
        public const float PawnRadius = 0.4f;
        public const float PawnStep = 0.3f;
        public const float PawnHeight = 1.8f;
        public const float MeshMatch = 0.05f;
        public const float GrappleRange = 28f;
        const float RiseGravity = 22f;
        const float FallGravity = 1.5f;

        // Equal arc on the 472 m loop (118 m). Corner waypoints stay put; these pads
        // are the starts. Every It sees runners at 118 / 118 / 236, so no corner is
        // a shorter chase than the others.
        public const float SpawnSwX = 8f;
        public const float SpawnSwZ = 8f;
        public const float SpawnSeX = 118f;
        public const float SpawnSeZ = 16f;
        public const float SpawnNwX = 42f;
        public const float SpawnNwZ = 92f;
        public const float SpawnNeX = 152f;
        public const float SpawnNeZ = 84f;

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
        // Yaw is the outgoing CCW direction at that arc point.
        public static readonly SpawnPad[] Spawns =
        {
            new SpawnPad { Name = "Spawn_SW", X = SpawnSwX, Z = SpawnSwZ, YawDeg = 90f },
            new SpawnPad { Name = "Spawn_SE", X = SpawnSeX, Z = SpawnSeZ, YawDeg = 180f },
            new SpawnPad { Name = "Spawn_NW", X = SpawnNwX, Z = SpawnNwZ, YawDeg = -90f },
            new SpawnPad { Name = "Spawn_NE", X = SpawnNeX, Z = SpawnNeZ, YawDeg = 0f },
        };

        // Overflow runner pads for a 5–6 player lobby. Not It starts. RunS sits just
        // north of the bar spine so a standing capsule is not spawned under a beam.
        public static readonly SpawnPad[] RunnerSpawns =
        {
            new SpawnPad { Name = "Spawn_RunS", X = 59f, Z = 20.5f, YawDeg = 90f },
            new SpawnPad { Name = "Spawn_RunN", X = 101f, Z = 92f, YawDeg = -90f },
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
            public int RouteCount;
            public bool RimContinuous;
            public float RimGapMax;
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

            string levelNote = FloorReport(solids, ramps, fail);
            string pairNote = SpawnReport(fail);
            ToyReport(solids, fail);
            SightReport(solids, fail);
            ColorReport(solids, fail);

            int anchors = 0;
            for (int i = 0; i < solids.Length; i++)
            {
                float top = solids[i].Y + solids[i].Sy * 0.5f;
                if (top >= 2.4f && solids[i].Kind != "ground")
                    anchors++;
            }
            if (anchors < 8)
                fail.Append("not enough tall grapple anchors; ");

            NoPenetration(solids, fail);

            int routes = CountRoutes(solids, out bool rimContinuous, out float rimGap, out string routeWhy);
            if (routeWhy != null)
                fail.Append(routeWhy);

            int failBeforeMesh = fail.Length;
            float meshGap = ColliderVisualGap(solids, ramps, fail);
            float groundErr = GroundError(solids, fail);
            int failBeforePads = fail.Length;
            int pads = PadReport(solids, fail);
            bool padsLanded = fail.Length == failBeforePads;
            int zips = ZipReport(fail);
            GrappleReport(solids, fail);
            CrossingReport(solids, ramps, fail);
            SameWallReport(solids, fail);
            if (fail.Length == failBeforeMesh && meshGap > MeshMatch)
                fail.Append("collider mismatch ").Append(meshGap.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");

            var audit = new Audit
            {
                Ok = fail.Length == 0,
                LoopM = loop,
                Seconds = seconds,
                SolidCount = solids.Length,
                WallCount = walls,
                VaultCount = vaults,
                BarClear = bars > 0 ? barClear : 0f,
                RouteCount = routes,
                RimContinuous = rimContinuous,
                RimGapMax = rimGap,
            };
            audit.Line = string.Format(
                CultureInfo.InvariantCulture,
                "MegaPark map: loop {0:0.00} m at sprint {1:0} = {2:0.000} s; spawns 4+2; solids {3} grounded; cling walls {4}; vaults {5}; bar under-clear {6:0.00} m; crossings A+B open; routes {7}; rim {8}, max gap {9:0.00} m; {10}; {11}; collider mismatch {12:0.000} m; ground error {13:0.000} m; pads {14} {15}; zip slots {16}",
                audit.LoopM, SprintSpeed, audit.Seconds, audit.SolidCount, audit.WallCount, audit.VaultCount, audit.BarClear,
                audit.RouteCount, audit.RimContinuous ? "continuous" : "broken", audit.RimGapMax, levelNote, pairNote,
                meshGap, groundErr, pads, padsLanded ? "landed" : "miss", zips);
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

            // Z1 Soft-Play. South fringe z=8 and the x=38 corner stay clear. Coral, not rim brown.
            Add(list, "SoftPlay_DeckLow", "Z1", "block", "soft", 14f, 1f, 26f, 10f, 2f, 8f, 0f);
            Add(list, "SoftPlay_DeckHigh", "Z1", "cap", "soft", 14f, 2.75f, 26f, 6f, 1.5f, 5f, 2f);
            Add(list, "SoftPlay_TubeL", "Z1", "block", "soft", 14f, 1f, 16.9f, 8f, 2f, 0.2f, 0f);
            Add(list, "SoftPlay_TubeR", "Z1", "block", "soft", 14f, 1f, 19.5f, 8f, 2f, 0.2f, 0f);
            Add(list, "SoftPlay_TubeRoof", "Z1", "cap", "soft", 14f, 2.1f, 18.2f, 8f, 0.2f, 2.9f, 2f);
            Add(list, "SoftPlay_CubeA", "Z1", "vault", "soft", 26f, 0.55f, 20f, 1.4f, 1.1f, 1.4f, 0f);
            Add(list, "SoftPlay_CubeB", "Z1", "vault", "soft", 30f, 0.4f, 28f, 1.6f, 0.8f, 1.6f, 0f);
            Add(list, "SoftPlay_CubeC", "Z1", "vault", "soft", 24f, 0.85f, 32f, 1.2f, 1.7f, 1.2f, 0f);

            // Z2 Cling arena, west of the x=8 loop. Eight flat identical faces.
            // Lanes x=2.55 and x=6.15 (3.20 m face-to-face). Each face is 6.40 m,
            // longer than one 9.5 m/s run (0.62 s ≈ 5.9 m) so the jump is a choice
            // inside the 2.20 m Z overlap, not a corner snag. Low-skill mulch is
            // the open band east of the walls (the loop at x=8 and the lawn).
            AddClingChain(list);

            // Z3 Merry. Crossing B is the open band z[44,52]. Grapple toy is north of that band.
            Add(list, "Merry_Podium", "Z3", "bump", "merry", 34f, 0.15f, 39f, 5f, 0.3f, 5f, 0f);
            Add(list, "Merry_Post_SW", "Z3", "anchor", "steel", 31.2f, 1.2f, 36.6f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_Post_SE", "Z3", "anchor", "steel", 36.8f, 1.2f, 36.6f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_Post_NW", "Z3", "anchor", "steel", 31.2f, 1.2f, 41.4f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_Post_NE", "Z3", "anchor", "steel", 36.8f, 1.2f, 41.4f, 0.25f, 2.4f, 0.25f, 0f);
            Add(list, "Merry_TableW", "Z3", "vault", "merry", 28f, 0.42f, 56f, 2.4f, 0.84f, 1.2f, 0f);
            Add(list, "Merry_TableE", "Z3", "vault", "merry", 40f, 0.42f, 56f, 2.4f, 0.84f, 1.2f, 0f);
            AddMerryGrapple(list);

            // Z4 Slide mountain on the rim, south of the z=92 loop and the north spine.
            // Amber structure. Yellow is only the chutes. The +5 top stays this zone.
            Add(list, "Slide_T1", "Z4", "block", "amber", 28f, 1f, 78f, 4f, 2f, 4f, 0f);
            Add(list, "Slide_T2", "Z4", "block", "amber", 38f, 1.75f, 79f, 4f, 3.5f, 4f, 0f);
            Add(list, "Slide_T2Step", "Z4", "block", "amber", 40.8f, 1f, 79f, 1.6f, 2f, 2.2f, 0f);
            Add(list, "Slide_T3", "Z4", "block", "amber", 48f, 2.5f, 78f, 4f, 5f, 4f, 0f);
            // Step A ends on Step B's south face. The old z=77.2 overlapped Step B by 0.2 m.
            Add(list, "Slide_StepA", "Z4", "block", "amber", 45.1f, 1f, 77.0f, 1.8f, 2f, 1.8f, 0f);
            Add(list, "Slide_StepB", "Z4", "block", "amber", 45.1f, 1.75f, 78.8f, 1.8f, 3.5f, 1.8f, 0f);
            AddSlideVertical(list);

            // Z5 Swing grove, south of the north spine (z>=83) and the loop.
            AddSwing(list, 66f);
            AddSwing(list, 78f);
            AddSwing(list, 90f);

            // Z6 Twin forts. East spine x[130,138] and the loop x=152 stay empty.
            // Army is olive, knight is plum, so a callout can name the fort.
            AddCrawl(list, "Army", 123f, 28.2f, "army");
            AddSpiral(list, "Army", 143f, 28f, "army");
            AddCrawl(list, "Knight", 123f, 64.2f, "knight");
            AddSpiral(list, "Knight", 143f, 70f, "knight");
            AddFortDecks(list);

            // Z7 Kickball. East rail is the vault. Bases stay under mantle height.
            // Clay reads against the green field paint. The dugout bar is the toy line.
            Add(list, "Base_Home", "Z7", "bump", "kick", 96f, 0.1f, 34f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Base_First", "Z7", "bump", "kick", 110f, 0.1f, 48f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Base_Second", "Z7", "bump", "kick", 96f, 0.1f, 62f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Base_Third", "Z7", "bump", "kick", 82f, 0.1f, 48f, 0.9f, 0.2f, 0.9f, 0f);
            Add(list, "Mound", "Z7", "bump", "kick", 96f, 0.125f, 48f, 2.4f, 0.25f, 2.4f, 0f);
            Add(list, "Rail_East", "Z7", "vault", "kick", 114f, 0.45f, 48f, 0.12f, 0.9f, 40f, 0f);
            AddKickDugout(list);

            // Z8 toys, outside the open rect, sitting on the sand top.
            Add(list, "Toy_Sandbox_Bucket_SW", "Z8", "toy", "rubber", 51f, -0.825f, 42.5f, 0.4f, 0.35f, 0.4f, BowlFloorY);
            Add(list, "Toy_Sandbox_Shovel_SW", "Z8", "toy", "rubber", 51f, -0.97f, 43.6f, 0.55f, 0.06f, 0.08f, BowlFloorY);
            Add(list, "Toy_Sandbox_Mold_NE", "Z8", "toy", "rubber", 73f, -0.94f, 60.4f, 0.5f, 0.12f, 0.5f, BowlFloorY);
            Add(list, "Toy_Sandbox_Sifter_NE", "Z8", "toy", "rubber", 73f, -0.97f, 61.2f, 0.45f, 0.06f, 0.4f, BowlFloorY);

            // Z9 bars. 6.0 m cadence (sprint 12 → 0.50 s, just past a 0.40 s mantle).
            // Bar bottom 1.14 m so crouch (1.05) slides under. Side lips are the
            // stand-up mantle. The z=16 centerline stays open.
            AddBarHighway(list);

            // Z10 hopscotch. One straight mantle line, chalk blue, off the z=8 loop.
            for (int i = 0; i < 8; i++)
            {
                float x = 124f + i * 3.1f;
                float lip = 0.72f + (i % 4) * 0.22f;
                Add(list, "Hop_" + i.ToString(CultureInfo.InvariantCulture), "Z10", "vault", "hop", x, lip * 0.5f, 15f, 1.15f, lip, 1.15f, 0f);
            }

            AddSwingLine(list);
            AddSightCover(list);
            AddRimRoute(list);
            AddRoutePlates(list);
            return list.ToArray();
        }

        // Southbound chain. Even indices on the west lane, odd on the east lane.
        static void AddClingChain(List<Solid> list)
        {
            const float len = 6.4f;
            const float overlap = 2.2f;
            const float step = len - overlap;
            const float north = 77f;
            for (int i = 0; i < 8; i++)
            {
                float zNorth = north - i * step;
                float z = zNorth - len * 0.5f;
                float x = (i % 2 == 0) ? 2.55f : 6.15f;
                Add(list, "Cling_" + i.ToString(CultureInfo.InvariantCulture), "Z2", "wall", "blue",
                    x, 2.4f, z, 0.40f, 4.8f, len, 0f);
            }
        }

        static void AddBarHighway(List<Solid> list)
        {
            const int bays = 12;
            const float spacing = 6f;
            for (int i = 0; i < bays; i++)
            {
                float x = 44f + i * spacing;
                string id = i.ToString(CultureInfo.InvariantCulture);
                Add(list, "BarPost_S" + id, "Z9", "post", "steel", x, 0.57f, 12.7f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "BarPost_N" + id, "Z9", "post", "steel", x, 0.57f, 19.3f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "Bar_" + id, "Z9", "bar", "steel", x, 1.2f, 16f, 0.14f, 0.12f, 6.8f, 1.14f);
                // North lip sits east of the bar so the mantle top is not under the steel.
                // Top 0.96 is in the mantle window. Posts stay on the band edge.
                Add(list, "BarVault_N" + id, "Z9", "vault", "concrete", x + 1.6f, 0.48f, 18.55f, 1.30f, 0.96f, 0.90f, 0f);
                // South lip on the half beat so the weave is 3 m and each side is 6 m.
                Add(list, "BarVault_S" + id, "Z9", "vault", "concrete", x + 3f, 0.48f, 13.55f, 1.30f, 0.96f, 0.90f, 0f);
            }
        }

        // Elevated rim. Foot gaps stay inside a 0.95 m mantle. Wider breaks are
        // planar grapple faces (steel plates on the far lip, RMB ray, 28 m motor range).
        // Drop spurs end on the bowl lip and do not enter the open rect.
        static void AddRimRoute(List<Solid> list)
        {
            // Rim segments take the zone tint so a callout can name where you are.
            // Grapple plates stay orange, added by AddHook, and are not this tint.
            Add(list, "Rim_SoftN", "Z1", "block", "soft", 14.3f, 1f, 33.2f, 8.6f, 2f, 5.6f, 0f);
            // East face at x=21.5 so a 0.40 m capsule in crossing B (x>=22) clears the corner.
            Add(list, "Rim_W1", "Z2", "block", "pad", 20f, 1.25f, 40.4f, 3f, 2.5f, 8f, 0f);
            Add(list, "Rim_W2", "Z2", "block", "pad", 20f, 1.5f, 48.8f, 3f, 3f, 8f, 0f);
            Add(list, "Rim_W3", "Z2", "block", "pad", 20f, 1.75f, 57.2f, 3f, 3.5f, 8f, 0f);
            Add(list, "Rim_W4", "Z4", "block", "amber", 20f, 1.75f, 65.6f, 3f, 3.5f, 8f, 0f);
            Add(list, "Rim_W5", "Z4", "block", "amber", 20f, 1.75f, 74f, 3f, 3.5f, 8f, 0f);
            Add(list, "Rim_W6", "Z4", "block", "amber", 20f, 1.75f, 80.3f, 3f, 3.5f, 3.8f, 0f);
            Add(list, "Rim_SlideIn", "Z4", "block", "amber", 29f, 1.75f, 81.2f, 13.6f, 3.5f, 2f, 0f);
            Add(list, "Rim_Link", "Z4", "block", "amber", 42.10f, 1.75f, 77.125f, 4.04f, 3.5f, 1.25f, 0f);

            Add(list, "Rim_N1", "Z4", "block", "amber", 58f, 2f, 78f, 8f, 4f, 4f, 0f);
            AddHook(list, "Hook_Rim_N1", "Z4", 4f, 54.2f, 78f, 0.30f, 3f);
            Add(list, "Rim_N2", "Z5", "block", "swing", 72f, 1.75f, 77f, 8f, 3.5f, 4f, 0f);
            AddHook(list, "Hook_Rim_N2", "Z5", 3.5f, 68.2f, 77f, 0.30f, 3f);
            Add(list, "Rim_N3", "Z5", "block", "swing", 86f, 1.75f, 77f, 8f, 3.5f, 4f, 0f);
            AddHook(list, "Hook_Rim_N3", "Z5", 3.5f, 82.2f, 77f, 0.30f, 3f);
            Add(list, "Rim_N4", "Z5", "block", "swing", 100f, 1.6f, 77f, 8f, 3.2f, 4f, 0f);
            AddHook(list, "Hook_Rim_N4", "Z5", 3.2f, 96.2f, 77f, 0.30f, 3f);
            Add(list, "Rim_Corner", "Z6", "block", "knight", 112f, 1.5f, 72.7f, 8f, 3f, 7f, 0f);
            AddHook(list, "Hook_Rim_Corner", "Z6", 3f, 108.2f, 75.2f, 0.30f, 2f);
            Add(list, "Rim_Knight", "Z6", "block", "knight", 122f, 1.4f, 68.2f, 6f, 2.8f, 5f, 0f);
            AddHook(list, "Hook_Rim_Knight", "Z6", 2.8f, 119.2f, 70f, 0.30f, 1.2f);

            Add(list, "Rim_Ksouth", "Z6", "block", "knight", 122f, 1.1f, 58.15f, 5f, 2.2f, 8.3f, 0f);
            AddHook(list, "Hook_Rim_Ksouth", "Z6", 2.2f, 122f, 54.2f, 3f, 0.30f);
            Add(list, "Rim_GapS", "Z6", "block", "army", 122f, 1.1f, 43.2f, 5f, 2.2f, 5.6f, 0f);
            AddHook(list, "Hook_Rim_GapS", "Z6", 2.2f, 122f, 45.75f, 3f, 0.30f);
            Add(list, "Rim_Army", "Z6", "block", "army", 122f, 1f, 34.85f, 5f, 2f, 10.3f, 0f);

            // North lip of the bowl, off Rim_N1. South face stops on the mulch side of z=66.
            Add(list, "Rim_DropN", "Z4", "block", "amber", 58f, 1.25f, 71.1f, 4f, 2.5f, 9.4f, 0f);
            // East lip of the bowl, off Rim_N3. West face stops 2 m east of the sand edge x=78.
            Add(list, "Rim_DropE", "Z7", "block", "kick", 83f, 1.15f, 70.4f, 6f, 2.3f, 8.8f, 0f);
        }

        static void AddHook(List<Solid> list, string name, string zone, float deckTop,
            float x, float z, float sx, float sz)
        {
            // Orange plate, not bar steel. The face is the grapple target.
            Add(list, name, zone, "anchor", "plate", x, deckTop + 1f, z, sx, 2f, sz, deckTop);
        }

        public static Ramp[] BuildRamps()
        {
            var list = new List<Ramp>(120);
            // Straight banks stop at the corner squares. Strips fill those squares
            // so two slopes do not cross and leave a vertical lip.
            list.Add(RampOf("SandBank_W", "Z8", "sand", 46f, 0f, 50f, 50f, BowlFloorY, 50f, 24f));
            list.Add(RampOf("SandBank_E", "Z8", "sand", 78f, 0f, 50f, 74f, BowlFloorY, 50f, 24f));
            list.Add(RampOf("SandBank_S", "Z8", "sand", 62f, 0f, 34f, 62f, BowlFloorY, 38f, 24f));
            list.Add(RampOf("SandBank_N", "Z8", "sand", 62f, 0f, 66f, 62f, BowlFloorY, 62f, 24f));
            AddSandCorners(list);
            list.Add(RampOf("Slide_Chute1", "Z4", "yellow", 28f, 2f, 76f, 28f, 0f, 68f, 1.6f));
            list.Add(RampOf("Slide_Chute2", "Z4", "yellow", 38f, 3.5f, 77f, 38f, 0f, 68f, 1.6f));
            list.Add(RampOf("Slide_Chute3", "Z4", "yellow", 48f, 5f, 76f, 48f, 0f, 66f, 1.6f));
            list.Add(RampOf("Slide_ChuteL2", "Z4", "yellow", 24f, 2f, 76.5f, 24f, 0f, 69f, 1.2f));
            list.Add(RampOf("Slide_ArmyLo", "Z6", "yellow", 146.2f, 2f, 26.3f, 146.2f, 0f, 21.2f, 1.3f));
            list.Add(RampOf("Slide_ArmyHi", "Z6", "yellow", 148.2f, 3.7f, 29.5f, 148.2f, 0f, 34.8f, 1.3f));
            list.Add(RampOf("Slide_KnightLo", "Z6", "yellow", 146.2f, 2f, 69.6f, 146.2f, 0f, 63.6f, 1.3f));
            list.Add(RampOf("Slide_KnightHi", "Z6", "yellow", 148.2f, 3.7f, 71.1f, 148.2f, 0f, 76.6f, 1.3f));
            return list.ToArray();
        }

        static void AddSandCorners(List<Ramp> list)
        {
            AddCornerStrips(list, "SW", 46f, 50f, 34f, 38f);
            AddCornerStrips(list, "SE", 78f, 74f, 34f, 38f);
            AddCornerStrips(list, "NW", 46f, 50f, 66f, 62f);
            AddCornerStrips(list, "NE", 78f, 74f, 66f, 62f);
        }

        // Bilinear bowl corner, sliced into strips. Neighboring strips differ by 1/25 m
        // at the inner edge, under the 0.05 m mesh match.
        static void AddCornerStrips(List<Ramp> list, string id, float outerX, float innerX, float outerZ, float innerZ)
        {
            const int n = 25;
            float spanZ = innerZ - outerZ;
            float w = Math.Abs(spanZ) / n;
            for (int i = 0; i < n; i++)
            {
                float v = (i + 0.5f) / n;
                float z = outerZ + spanZ * v;
                string name = "SandCorner_" + id + "_" + i.ToString(CultureInfo.InvariantCulture);
                list.Add(RampOf(name, "Z8", "sand", outerX, 0f, z, innerX, -v, z, w));
            }
        }

        static void AddRoutePlates(List<Solid> list)
        {
            // Cling chain ends on the east lane. The deck is the grapple off that face
            // so the pawn does not have to regrab the last wall.
            Add(list, "Cling_ExitDeck", "Z2", "block", "pad", 28f, 1f, 34f, 3.2f, 2f, 3f, 0f);
            Add(list, "Hook_Cling_Exit", "Z2", "anchor", "plate", 26.55f, 3f, 34f, 0.3f, 2f, 2.2f, 2f);

            // Bar mantle line stops. Plate on the west lip of a deck past the last bay.
            Add(list, "Bar_EndDeck", "Z9", "block", "concrete", 122f, 1f, 24f, 3.2f, 2f, 3.2f, 0f);
            Add(list, "Hook_Bar_End", "Z9", "anchor", "plate", 120.55f, 3f, 24f, 0.3f, 2f, 2.4f, 2f);

            // The elevated rim ends at the crawls, short of the fort decks.
            Add(list, "Hook_Army_West", "Z6", "anchor", "plate", 145.75f, 3f, 28.8f, 0.3f, 2f, 1.6f, 2f);
            Add(list, "Hook_Knight_West", "Z6", "anchor", "plate", 145.75f, 3f, 71.8f, 0.3f, 2f, 1.6f, 2f);
        }

        public struct PadSpot
        {
            public string Name;
            public float X, Y, Z, Apex, DirX, DirZ, Speed;
        }

        public struct ZipMark
        {
            public string Name;
            public float X, Y, Z;
        }

        // Each pad sets a horizontal. Landing is the continuous return to pad height.
        public static readonly PadSpot[] LaunchPads =
        {
            new PadSpot { Name = "Launch_CrossB", X = 34f, Y = 0f, Z = 42.75f, Apex = 4f, DirX = 0f, DirZ = 1f, Speed = 10f },
            new PadSpot { Name = "Launch_FortGap", X = 140f, Y = 0f, Z = 39.5f, Apex = 5f, DirX = 0f, DirZ = 1f, Speed = 13.5f },
            new PadSpot { Name = "Launch_ClingEast", X = 14f, Y = 0f, Z = 38f, Apex = 3.5f, DirX = 1f, DirZ = 0f, Speed = 10f },
            new PadSpot { Name = "Launch_HopArmy", X = 126f, Y = 0f, Z = 18f, Apex = 3f, DirX = 0f, DirZ = 1f, Speed = 7.5f },
            new PadSpot { Name = "Launch_KickWest", X = 108f, Y = 0f, Z = 40f, Apex = 3.5f, DirX = -1f, DirZ = 0f, Speed = 12f },
        };

        public static readonly ZipMark[] ZipSlots =
        {
            new ZipMark { Name = "ZipLineSlot_WestRim_A", X = 20f, Y = 4.2f, Z = 48.8f },
            new ZipMark { Name = "ZipLineSlot_WestRim_B", X = 20f, Y = 4.6f, Z = 74f },
            new ZipMark { Name = "ZipLineSlot_Fort_A", X = 140f, Y = 4.4f, Z = 28f },
            new ZipMark { Name = "ZipLineSlot_Fort_B", X = 140f, Y = 4.4f, Z = 71.2f },
            new ZipMark { Name = "ZipLineSlot_Bars_A", X = 50f, Y = 3.2f, Z = 28f },
            new ZipMark { Name = "ZipLineSlot_Bars_B", X = 104f, Y = 3.2f, Z = 28f },
            new ZipMark { Name = "ZipLineSlot_Bowl_A", X = 44f, Y = 3.4f, Z = 50f },
            new ZipMark { Name = "ZipLineSlot_Bowl_B", X = 80f, Y = 3.4f, Z = 50f },
            new ZipMark { Name = "ZipLineSlot_NorthRim_A", X = 72f, Y = 4.6f, Z = 77f },
            new ZipMark { Name = "ZipLineSlot_NorthRim_B", X = 112f, Y = 4.2f, Z = 72.7f },
        };

        static void AddSwing(List<Solid> list, float x)
        {
            string id = x.ToString("0", CultureInfo.InvariantCulture);
            Add(list, "Swing_PostL_" + id, "Z5", "anchor", "steel", x - 1.2f, 1.035f, 81f, 0.2f, 2.07f, 0.2f, 0f);
            Add(list, "Swing_PostR_" + id, "Z5", "anchor", "steel", x + 1.2f, 1.035f, 81f, 0.2f, 2.07f, 0.2f, 0f);
            Add(list, "Swing_Beam_" + id, "Z5", "cap", "steel", x, 2.15f, 81f, 2.6f, 0.16f, 0.2f, 2.07f);
            Add(list, "Swing_Rail_" + id, "Z5", "vault", "swing", x, 0.45f, 82.3f, 2.2f, 0.9f, 0.16f, 0f);
        }

        static void AddCrawl(List<Solid> list, string fort, float x, float z, string mat)
        {
            string zone = fort == "Slide" ? "Z4" : "Z6";
            Add(list, fort + "_CrawlL", zone, "block", mat, x, 0.6f, z - 0.9f, 7f, 1.2f, 0.22f, 0f);
            Add(list, fort + "_CrawlR", zone, "block", mat, x, 0.6f, z + 0.9f, 7f, 1.2f, 0.22f, 0f);
            Add(list, fort + "_CrawlRoof", zone, "cap", mat, x, 1.29f, z, 7f, 0.18f, 2.02f, 1.2f);
        }

        static void AddSpiral(List<Solid> list, string fort, float x, float z, string mat)
        {
            Add(list, fort + "_Core", "Z6", "anchor", mat, x, 1.4f, z, 0.5f, 2.8f, 0.5f, 0f);
            Add(list, fort + "_L1", "Z6", "vault", mat, x - 1.5f, 0.35f, z - 1.4f, 1.5f, 0.7f, 1.5f, 0f);
            Add(list, fort + "_L2", "Z6", "vault", mat, x + 1.5f, 0.7f, z - 1.4f, 1.5f, 1.4f, 1.5f, 0f);
            Add(list, fort + "_L3", "Z6", "vault", mat, x + 1.5f, 1.05f, z + 1.4f, 1.5f, 2.1f, 1.5f, 0f);
            Add(list, fort + "_L4", "Z6", "block", mat, x - 1.5f, 1.4f, z + 1.4f, 1.5f, 2.8f, 1.5f, 0f);
        }

        // West +2 landing, a ground crawl under T1, and a spiral that meets T2's south face.
        // Two downs per floor: chute plus a lower landing. Spiral and crawl are in that set.
        static void AddSlideVertical(List<Solid> list)
        {
            Add(list, "Slide_Deck2", "Z4", "block", "amber", 24f, 1f, 78f, 2.6f, 2f, 3f, 0f);
            Add(list, "Slide_Deck2Step", "Z4", "vault", "amber", 25.5f, 0.45f, 75.2f, 1.6f, 0.9f, 1.4f, 0f);
            AddCrawl(list, "Slide", 33.2f, 74.4f, "amber");

            float x = 41.3f;
            float z = 74.4f;
            float o = 1.25f;
            float s = 1.2f;
            Add(list, "Slide_Spire", "Z4", "anchor", "amber", x, 1.4f, z, 0.4f, 2.8f, 0.4f, 0f);
            Add(list, "Slide_Sp1", "Z4", "vault", "amber", x - o, 0.4f, z - o, s, 0.8f, s, 0f);
            Add(list, "Slide_Sp2", "Z4", "vault", "amber", x + o, 0.8f, z - o, s, 1.6f, s, 0f);
            Add(list, "Slide_Sp3", "Z4", "vault", "amber", x + o, 1.2f, z + o, s, 2.4f, s, 0f);
            Add(list, "Slide_Sp4", "Z4", "block", "amber", x - o, 1.25f, z + o, s, 2.5f, s, 0f);
        }

        // East of each spiral, clear of the x[130,138] spine and the x=152 loop.
        // Low deck exposed around a smaller high deck, so the high floor can be walked off.
        static void AddFortDecks(List<Solid> list)
        {
            Add(list, "Army_Lo", "Z6", "block", "army", 147.4f, 1f, 28f, 3.6f, 2f, 3.4f, 0f);
            Add(list, "Army_Hi", "Z6", "block", "army", 148.2f, 2.85f, 28.8f, 1.6f, 1.7f, 1.4f, 2f);
            Add(list, "Knight_Lo", "Z6", "block", "knight", 147.4f, 1f, 71.2f, 3.6f, 2f, 3.2f, 0f);
            Add(list, "Knight_Hi", "Z6", "block", "knight", 148.2f, 2.85f, 70.4f, 1.6f, 1.7f, 1.4f, 2f);
        }

        // North of crossing B. 4.8 m gap, plate on the east deck's west lip. Max grapple 8 m.
        static void AddMerryGrapple(List<Solid> list)
        {
            Add(list, "Merry_A", "Z3", "block", "merry", 25.6f, 1f, 58.2f, 3f, 2f, 2.2f, 0f);
            Add(list, "Merry_B", "Z3", "block", "merry", 33.4f, 1.15f, 58.2f, 3f, 2.3f, 2.2f, 0f);
            Add(list, "Hook_Merry_B", "Z3", "anchor", "plate", 32.05f, 3.3f, 58.2f, 0.3f, 2f, 1.8f, 2.3f);
        }

        // One vault cadence in the grove, south of the swing posts and north of the rim.
        static void AddSwingLine(List<Solid> list)
        {
            for (int i = 0; i < 5; i++)
            {
                float x = 64f + i * 3.2f;
                Add(list, "Swing_Line" + i.ToString(CultureInfo.InvariantCulture), "Z5", "vault", "swing",
                    x, 0.48f, 79.95f, 1.3f, 0.96f, 0.5f, 0f);
            }
        }

        // N-S slide-under on the infield. Chest-high bar, mantle lip beside it. Not a wall.
        static void AddKickDugout(List<Solid> list)
        {
            Add(list, "Kick_PostS", "Z7", "post", "steel", 106f, 0.57f, 43.2f, 0.22f, 1.14f, 0.22f, 0f);
            Add(list, "Kick_PostN", "Z7", "post", "steel", 106f, 0.57f, 52.8f, 0.22f, 1.14f, 0.22f, 0f);
            Add(list, "Kick_Bar", "Z7", "bar", "steel", 106f, 1.2f, 48f, 0.14f, 0.12f, 10.2f, 1.14f);
            Add(list, "Kick_Lip", "Z7", "vault", "concrete", 107.6f, 0.48f, 48f, 0.9f, 0.96f, 2.2f, 0f);
        }

        // Low vault cover. Breaks long ground sightlines. Stays out of the bowl, crossing B,
        // and the rim-to-bowl slot x[52,72] z[58,76].
        static void AddSightCover(List<Solid> list)
        {
            Add(list, "Cover_S1", "Z9", "vault", "cover", 48f, 0.675f, 26.5f, 2.4f, 1.35f, 1.15f, 0f);
            Add(list, "Cover_S2", "Z7", "vault", "cover", 78f, 0.675f, 25.5f, 2.2f, 1.35f, 1.15f, 0f);
            Add(list, "Cover_S3", "Z10", "vault", "cover", 104f, 0.675f, 26.5f, 2.4f, 1.35f, 1.15f, 0f);
            Add(list, "Cover_W", "Z3", "vault", "cover", 40f, 0.675f, 30f, 1.8f, 1.35f, 1.2f, 0f);
            Add(list, "Cover_K1", "Z7", "vault", "cover", 88f, 0.675f, 40f, 1.5f, 1.35f, 1.5f, 0f);
            Add(list, "Cover_K2", "Z7", "vault", "cover", 100f, 0.675f, 58f, 1.5f, 1.35f, 1.5f, 0f);
            // East of Rim_Corner (x to 116) and north of Rim_Knight (z to 70.7).
            Add(list, "Cover_N", "Z6", "vault", "cover", 124f, 0.675f, 73.2f, 2.2f, 1.35f, 1.15f, 0f);
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
            bool corner = r.Name.StartsWith("SandCorner_", StringComparison.Ordinal);
            if (corner)
            {
                if (r.Y0 > 0.03f || r.Y1 < BowlFloorY - 0.03f || r.Y1 > 0.03f)
                {
                    why = "corner strip left the bowl";
                    return false;
                }
            }
            else if (!RampEndSupported(r.X0, r.Y0, r.Z0, solids) || !RampEndSupported(r.X1, r.Y1, r.Z1, solids))
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

        static readonly string[] FloorNames =
        {
            "Slide_T1", "Slide_Deck2", "Slide_T2", "Slide_T3",
            "Army_Lo", "Army_Hi", "Knight_Lo", "Knight_Hi",
        };

        static string FloorReport(Solid[] solids, Ramp[] ramps, StringBuilder fail)
        {
            var sb = new StringBuilder();
            bool slideChute = false, slideDrop = false, slideSpiral = false, slideCrawl = false;
            bool armyChute = false, armyDrop = false, armySpiral = false;
            bool knightChute = false, knightDrop = false, knightSpiral = false;
            float tallestOffRim = 0f;

            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "wall" || s.Kind == "anchor")
                    continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top >= 4.5f && s.Zone != "Z4" && top > tallestOffRim)
                    tallestOffRim = top;
            }
            if (tallestOffRim > 0f)
                fail.Append("height left the slide rim at ").Append(tallestOffRim.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");

            for (int f = 0; f < FloorNames.Length; f++)
            {
                string id = FloorNames[f];
                if (!TryFind(solids, id, out Solid deck))
                {
                    fail.Append("level missing ").Append(id).Append("; ");
                    sb.Append(id).Append(" 0");
                    if (f + 1 < FloorNames.Length) sb.Append(' ');
                    continue;
                }
                float top = deck.Y + deck.Sy * 0.5f;
                int exits = 0;
                for (int i = 0; i < ramps.Length; i++)
                {
                    Ramp r = ramps[i];
                    if (r.Mat != "yellow") continue;
                    bool aHigh = r.Y0 >= r.Y1;
                    float hy = aHigh ? r.Y0 : r.Y1;
                    float ly = aHigh ? r.Y1 : r.Y0;
                    float hx = aHigh ? r.X0 : r.X1;
                    float hz = aHigh ? r.Z0 : r.Z1;
                    if (hy - ly < 1.4f) continue;
                    if (Math.Abs(hy - top) > 0.25f) continue;
                    if (DistXZ(hx, hz, deck) > 0.45f) continue;
                    exits++;
                    NoteExit(id, "chute", ref slideChute, ref slideDrop, ref slideSpiral, ref slideCrawl,
                        ref armyChute, ref armyDrop, ref armySpiral, ref knightChute, ref knightDrop, ref knightSpiral);
                }
                for (int i = 0; i < solids.Length; i++)
                {
                    Solid s = solids[i];
                    if (s.Name == deck.Name || s.Kind == "ground" || s.Kind == "fence") continue;
                    float st = s.Y + s.Sy * 0.5f;
                    float dy = top - st;
                    if (dy < 0.55f || dy > 3.3f) continue;
                    if (s.Sx < 1.05f || s.Sz < 1.05f) continue;
                    if (AabbGap(deck, s) > 1.15f) continue;
                    exits++;
                    string kind = "drop";
                    if (s.Name.IndexOf("Crawl", StringComparison.Ordinal) >= 0) kind = "crawl";
                    else if (s.Name.IndexOf("_Sp", StringComparison.Ordinal) >= 0
                        || s.Name.EndsWith("_L1", StringComparison.Ordinal)
                        || s.Name.EndsWith("_L2", StringComparison.Ordinal)
                        || s.Name.EndsWith("_L3", StringComparison.Ordinal)
                        || s.Name.EndsWith("_L4", StringComparison.Ordinal))
                        kind = "spiral";
                    NoteExit(id, kind, ref slideChute, ref slideDrop, ref slideSpiral, ref slideCrawl,
                        ref armyChute, ref armyDrop, ref armySpiral, ref knightChute, ref knightDrop, ref knightSpiral);
                }
                if (exits < 2)
                    fail.Append(id).Append(" has ").Append(exits.ToString(CultureInfo.InvariantCulture)).Append(" exits; ");
                sb.Append(id).Append(' ').Append(exits.ToString(CultureInfo.InvariantCulture));
                if (f + 1 < FloorNames.Length) sb.Append(' ');
            }

            if (!slideChute || !slideDrop || !slideSpiral || !slideCrawl)
                fail.Append("slide mountain is missing a chute, drop, spiral, or crawl; ");
            if (!armyChute || !armyDrop || !armySpiral)
                fail.Append("army deck is missing a chute, drop, or spiral; ");
            if (!knightChute || !knightDrop || !knightSpiral)
                fail.Append("knight deck is missing a chute, drop, or spiral; ");
            CrawlLinked(solids, "Rim_Army", "Army_CrawlRoof", fail);
            CrawlLinked(solids, "Rim_Knight", "Knight_CrawlRoof", fail);
            return "levels " + sb.ToString();
        }

        static void NoteExit(string floor, string kind,
            ref bool slideChute, ref bool slideDrop, ref bool slideSpiral, ref bool slideCrawl,
            ref bool armyChute, ref bool armyDrop, ref bool armySpiral,
            ref bool knightChute, ref bool knightDrop, ref bool knightSpiral)
        {
            bool slide = floor.StartsWith("Slide", StringComparison.Ordinal);
            bool army = floor.StartsWith("Army", StringComparison.Ordinal);
            bool knight = floor.StartsWith("Knight", StringComparison.Ordinal);
            if (kind == "chute")
            {
                if (slide) slideChute = true;
                if (army) armyChute = true;
                if (knight) knightChute = true;
            }
            else if (kind == "spiral")
            {
                if (slide) slideSpiral = true;
                if (army) armySpiral = true;
                if (knight) knightSpiral = true;
            }
            else if (kind == "crawl")
            {
                if (slide) slideCrawl = true;
            }
            else
            {
                if (slide) slideDrop = true;
                if (army) armyDrop = true;
                if (knight) knightDrop = true;
            }
        }

        static void CrawlLinked(Solid[] solids, string rimName, string roofName, StringBuilder fail)
        {
            if (!TryFind(solids, rimName, out Solid rim) || !TryFind(solids, roofName, out Solid roof))
            {
                fail.Append(rimName).Append(" crawl link missing; ");
                return;
            }
            float rimTop = rim.Y + rim.Sy * 0.5f;
            float roofTop = roof.Y + roof.Sy * 0.5f;
            if (AabbGap(rim, roof) > 1.05f || rimTop - roofTop < 0.4f)
                fail.Append(roofName).Append(" is not a drop off ").Append(rimName).Append("; ");
        }

        static void ToyReport(Solid[] solids, StringBuilder fail)
        {
            float z0 = 0f;
            for (int i = 0; i < 5; i++)
            {
                string name = "Swing_Line" + i.ToString(CultureInfo.InvariantCulture);
                if (!TryFind(solids, name, out Solid s))
                {
                    fail.Append("swing toy line missing; ");
                    return;
                }
                float top = s.Y + s.Sy * 0.5f;
                if (Math.Abs(top - 0.96f) > 0.04f || Math.Abs(s.Z - 79.95f) > 0.05f)
                    fail.Append(name).Append(" left the swing vault line; ");
                if (Math.Abs(s.X - (64f + i * 3.2f)) > 0.05f)
                    fail.Append(name).Append(" left the 3.2 m cadence; ");
                if (i == 0) z0 = s.Z;
                else if (Math.Abs(s.Z - z0) > 0.05f)
                    fail.Append(name).Append(" is not collinear; ");
            }

            if (!TryFind(solids, "Kick_Bar", out Solid bar))
                fail.Append("kickball toy line missing; ");
            else
            {
                float bottom = bar.Y - bar.Sy * 0.5f;
                if (bottom < BarUnderClear - 0.001f || bar.Sz < 8f)
                    fail.Append("kickball bar is not a slide-under line; ");
            }
            if (!TryFind(solids, "Kick_Lip", out Solid lip))
                fail.Append("kickball mantle lip missing; ");
            else if (lip.Kind != "vault")
                fail.Append("kickball lip is not a vault; ");

            float prevX = -1f;
            float hopZ = 0f;
            for (int i = 0; i < 8; i++)
            {
                if (!TryFind(solids, "Hop_" + i.ToString(CultureInfo.InvariantCulture), out Solid hop))
                {
                    fail.Append("hopscotch line missing; ");
                    return;
                }
                if (hop.Kind != "vault" || hop.Mat != "hop")
                    fail.Append(hop.Name).Append(" is not the chalk mantle line; ");
                if (i == 0) hopZ = hop.Z;
                else if (Math.Abs(hop.Z - hopZ) > 0.05f || hop.X <= prevX)
                    fail.Append(hop.Name).Append(" left the straight hop line; ");
                prevX = hop.X;
            }

            if (!TryFind(solids, "Merry_A", out Solid a) || !TryFind(solids, "Merry_B", out Solid b))
            {
                fail.Append("merry grapple line missing; ");
                return;
            }
            float gap = AabbGap(a, b);
            if (gap < 3f || gap > 8f)
                fail.Append("merry grapple gap ").Append(gap.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
            if (!HookFaces(solids, a, b, fail))
                fail.Append("merry plate does not face the gap; ");
        }

        static void SightReport(Solid[] solids, StringBuilder fail)
        {
            int covers = 0;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (!s.Name.StartsWith("Cover_", StringComparison.Ordinal)) continue;
                covers++;
                float top = s.Y + s.Sy * 0.5f;
                if (s.Kind != "vault" || top < 0.9f || top > 1.6f)
                    fail.Append(s.Name).Append(" is not low vault cover; ");
                if (OverlapsOpen(s) || OverlapsCrossingB(s))
                    fail.Append(s.Name).Append(" blocks a crossing; ");
                if (AabbHits(s, 52f, 72f, 58f, 76f))
                    fail.Append(s.Name).Append(" hides the bowl from the rim; ");
            }
            if (covers < 6)
                fail.Append("sightline cover is thin; ");
        }

        static void ColorReport(Solid[] solids, StringBuilder fail)
        {
            bool soft = false, pad = false, merry = false, amber = false, swing = false;
            bool army = false, knight = false, kick = false, hop = false, plate = false, blueWall = false;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Mat == "soft" && s.Zone == "Z1") soft = true;
                if (s.Mat == "pad" && s.Zone == "Z2") pad = true;
                if (s.Mat == "merry" && s.Zone == "Z3") merry = true;
                if (s.Mat == "amber" && s.Zone == "Z4") amber = true;
                if (s.Mat == "swing" && s.Zone == "Z5") swing = true;
                if (s.Mat == "army" && s.Zone == "Z6") army = true;
                if (s.Mat == "knight" && s.Zone == "Z6") knight = true;
                if (s.Mat == "kick" && s.Zone == "Z7") kick = true;
                if (s.Mat == "hop" && s.Zone == "Z10") hop = true;
                if (s.Mat == "plate")
                {
                    plate = true;
                    if (!s.Name.StartsWith("Hook_", StringComparison.Ordinal) || s.Kind != "anchor")
                        fail.Append(s.Name).Append(" uses the grapple plate color; ");
                }
                if (s.Name.StartsWith("Hook_", StringComparison.Ordinal) && s.Mat != "plate")
                    fail.Append(s.Name).Append(" plate is not orange; ");
                if (s.Kind == "wall" && s.Mat == "blue") blueWall = true;
                if (s.Mat == "blue" && s.Kind != "wall")
                    fail.Append(s.Name).Append(" cling blue is on a non-wall; ");
            }
            if (!soft || !pad || !merry || !amber || !swing || !army || !knight || !kick || !hop || !plate || !blueWall)
                fail.Append("a zone tint or verb color is missing; ");
        }

        static string SpawnReport(StringBuilder fail)
        {
            if (Spawns.Length != 4)
                fail.Append("expected 4 primary spawns; ");
            if (RunnerSpawns == null || RunnerSpawns.Length != 2)
                fail.Append("expected 2 runner spawns; ");
            if (Math.Abs(SpawnY) < 0.05f || SpawnY > 1f)
                fail.Append("spawn Y is buried or floating; ");
            if (Spawns.Length >= 4 && (Spawns[0].YawDeg != 90f || Spawns[1].YawDeg != 180f || Spawns[2].YawDeg != -90f || Spawns[3].YawDeg != 0f))
                fail.Append("spawn facing drifted; ");

            var all = new List<SpawnPad>(8);
            for (int i = 0; i < Spawns.Length; i++) all.Add(Spawns[i]);
            if (RunnerSpawns != null)
            {
                for (int i = 0; i < RunnerSpawns.Length; i++) all.Add(RunnerSpawns[i]);
            }

            for (int i = 0; i < all.Count; i++)
            {
                SpawnPad s = all[i];
                if (s.X < 1f || s.X > MapW - 1f || s.Z < 1f || s.Z > MapD - 1f)
                    fail.Append(s.Name).Append(" is outside the park; ");
                float lat;
                float t = ProjectLoop(s.X, s.Z, out lat);
                bool primary = i < Spawns.Length;
                if (primary && lat > 0.05f)
                    fail.Append(s.Name).Append(" left the loop; ");
                if (!primary && lat > 6f)
                    fail.Append(s.Name).Append(" is far off the loop; ");
                if (PadBlocked(s))
                    fail.Append(s.Name).Append(" is inside a solid; ");
                if (!FacesCcw(s, t, primary, out string faceWhy))
                    fail.Append(s.Name).Append(' ').Append(faceWhy).Append("; ");
            }

            string pairs = PairClause(fail);
            return pairs;
        }

        static string PairClause(StringBuilder fail)
        {
            if (Spawns.Length < 4)
                return "pairs missing";
            // SW SE NW NE indices 0 1 2 3. Expected min-arc: adjacent 118, diagonal 236.
            float swSe = PathDist(Spawns[0], Spawns[1]);
            float swNw = PathDist(Spawns[0], Spawns[2]);
            float swNe = PathDist(Spawns[0], Spawns[3]);
            float seNw = PathDist(Spawns[1], Spawns[2]);
            float seNe = PathDist(Spawns[1], Spawns[3]);
            float neNw = PathDist(Spawns[3], Spawns[2]);
            ExpectPair("SW-SE", swSe, 118f, fail);
            ExpectPair("SW-NW", swNw, 118f, fail);
            ExpectPair("SW-NE", swNe, 236f, fail);
            ExpectPair("SE-NW", seNw, 236f, fail);
            ExpectPair("SE-NE", seNe, 118f, fail);
            ExpectPair("NE-NW", neNw, 118f, fail);

            float runS = 0f, runN = 0f;
            if (RunnerSpawns != null && RunnerSpawns.Length >= 2)
            {
                runS = NearestPrimary(RunnerSpawns[0]);
                runN = NearestPrimary(RunnerSpawns[1]);
                if (runS < 50f || runN < 50f)
                    fail.Append("a runner pad is stacked on a primary; ");
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "pairs SW-SE {0:0.0} SW-NW {1:0.0} SW-NE {2:0.0} SE-NW {3:0.0} SE-NE {4:0.0} NE-NW {5:0.0}; runners S {6:0.0} N {7:0.0}",
                swSe, swNw, swNe, seNw, seNe, neNw, runS, runN);
        }

        static void ExpectPair(string name, float got, float want, StringBuilder fail)
        {
            if (Math.Abs(got - want) > 0.2f)
                fail.Append(name).Append(" path ").Append(got.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" != ").Append(want.ToString("0.0", CultureInfo.InvariantCulture)).Append("; ");
        }

        static float NearestPrimary(SpawnPad pad)
        {
            float best = float.MaxValue;
            for (int i = 0; i < Spawns.Length; i++)
            {
                float d = PathDist(pad, Spawns[i]);
                if (d < best) best = d;
            }
            return best;
        }

        static float PathDist(SpawnPad a, SpawnPad b)
        {
            float latA, latB;
            float ta = ProjectLoop(a.X, a.Z, out latA);
            float tb = ProjectLoop(b.X, b.Z, out latB);
            float along = Math.Abs(ta - tb);
            if (along > LoopLengthM * 0.5f) along = LoopLengthM - along;
            return along + latA + latB;
        }

        static float ProjectLoop(float x, float z, out float lateral)
        {
            float bestLat = float.MaxValue;
            float bestT = 0f;
            float t0 = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                Pt a = LoopCcw[i];
                Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = (float)Math.Sqrt(dx * dx + dz * dz);
                float u = 0f;
                if (seg > 0.001f)
                {
                    u = ((x - a.X) * dx + (z - a.Z) * dz) / (seg * seg);
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                }
                float px = a.X + dx * u;
                float pz = a.Z + dz * u;
                float lat = DistPoint(x, z, px, pz);
                if (lat < bestLat)
                {
                    bestLat = lat;
                    bestT = t0 + seg * u;
                }
                t0 += seg;
            }
            lateral = bestLat;
            return bestT;
        }

        static float DistPoint(float x0, float z0, float x1, float z1)
        {
            float dx = x0 - x1;
            float dz = z0 - z1;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static bool FacesCcw(SpawnPad s, float t, bool primary, out string why)
        {
            why = null;
            float fx, fz;
            YawForward(s.YawDeg, out fx, out fz);
            float lat;
            float t2 = ProjectLoop(s.X + fx * 6f, s.Z + fz * 6f, out lat);
            float dt = t2 - t;
            if (dt < -200f) dt += LoopLengthM;
            if (dt < 4f)
            {
                why = "does not face CCW";
                return false;
            }
            if (primary && lat > 1.5f)
            {
                why = "facing leaves the loop";
                return false;
            }
            return true;
        }

        static void YawForward(float yawDeg, out float fx, out float fz)
        {
            double r = yawDeg * Math.PI / 180.0;
            fx = (float)Math.Sin(r);
            fz = (float)Math.Cos(r);
        }

        static bool PadBlocked(SpawnPad s)
        {
            Solid[] solids = BuildSolids();
            const float radius = 0.45f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid b = solids[i];
                if (b.Kind == "ground" || b.Kind == "fence") continue;
                float bottom = b.Y - b.Sy * 0.5f;
                float top = b.Y + b.Sy * 0.5f;
                if (top < 0.05f || bottom > 1.7f) continue;
                if (DistXZ(s.X, s.Z, b) < radius)
                    return true;
            }
            return false;
        }

        static bool Has(HashSet<string> zones, string id)
        {
            return zones.Contains(id);
        }

        // Two gameplay solids may share a face (a cap seated on a deck). They may not occupy the same volume.
        static bool NoPenetration(Solid[] solids, StringBuilder fail)
        {
            bool ok = true;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid a = solids[i];
                if (a.Kind == "ground" || a.Kind == "fence") continue;
                float a0 = a.Y - a.Sy * 0.5f;
                float a1 = a.Y + a.Sy * 0.5f;
                for (int j = i + 1; j < solids.Length; j++)
                {
                    Solid b = solids[j];
                    if (b.Kind == "ground" || b.Kind == "fence") continue;
                    float b0 = b.Y - b.Sy * 0.5f;
                    float b1 = b.Y + b.Sy * 0.5f;
                    float yOverlap = Math.Min(a1, b1) - Math.Max(a0, b0);
                    if (yOverlap <= 0.04f) continue;
                    if (Math.Abs(a0 - b1) <= 0.03f || Math.Abs(b0 - a1) <= 0.03f) continue;
                    float dx = Overlap1D(a.X, a.Sx, b.X, b.Sx);
                    float dz = Overlap1D(a.Z, a.Sz, b.Z, b.Sz);
                    if (dx > 0.02f && dz > 0.02f)
                    {
                        fail.Append(a.Name).Append(" penetrates ").Append(b.Name).Append("; ");
                        ok = false;
                    }
                }
            }
            return ok;
        }

        // Five chase lines: cling chain, cling ground, bar mantle, bar slide-under, rim.
        static int CountRoutes(Solid[] solids, out bool rimContinuous, out float rimGap, out string why)
        {
            rimContinuous = false;
            rimGap = 0f;
            var fail = new StringBuilder();
            int routes = 0;
            if (ClingChainOk(solids, fail)) routes++;
            if (ClingGroundOk(solids, fail)) routes++;
            if (BarMantleOk(solids, fail)) routes++;
            if (BarSlideOk(solids, fail)) routes++;
            if (RimOk(solids, out rimContinuous, out rimGap, fail)) routes++;
            why = fail.Length == 0 ? null : fail.ToString();
            return routes;
        }

        static bool ClingChainOk(Solid[] solids, StringBuilder fail)
        {
            const float len = 6.4f;
            const float overlap = 2.2f;
            const float faceGap = 3.2f;
            bool ok = true;
            Solid[] walls = new Solid[8];
            for (int i = 0; i < 8; i++)
            {
                if (!TryFind(solids, "Cling_" + i.ToString(CultureInfo.InvariantCulture), out walls[i]))
                {
                    fail.Append("cling chain missing a wall; ");
                    return false;
                }
                if (Math.Abs(walls[i].Sx - 0.40f) > 0.02f || Math.Abs(walls[i].Sy - 4.8f) > 0.02f
                    || Math.Abs(walls[i].Sz - len) > 0.02f)
                {
                    fail.Append(walls[i].Name).Append(" is not a flat 6.40 m cling face; ");
                    ok = false;
                }
                float wantX = (i % 2 == 0) ? 2.55f : 6.15f;
                if (Math.Abs(walls[i].X - wantX) > 0.02f)
                {
                    fail.Append(walls[i].Name).Append(" left its lane; ");
                    ok = false;
                }
            }

            // Nominal 9.5 m/s for 0.62 s covers ~5.9 m. The face is longer than that
            // run, and the overlap begins before the timer so the jump has a flat face.
            float runM = 9.5f * 0.62f;
            if (len < runM - 0.05f || (len - overlap) / 9.5f > 0.62f)
            {
                fail.Append("cling face is not spaced for a 9.5 m/s wall-run; ");
                ok = false;
            }

            for (int i = 0; i < 7; i++)
            {
                float gap = FaceGapX(walls[i], walls[i + 1]);
                if (Math.Abs(gap - faceGap) > 0.08f)
                {
                    fail.Append("cling face gap ").Append(gap.ToString("0.00", CultureInfo.InvariantCulture))
                        .Append(" is not a 3.20 m wall-jump; ");
                    ok = false;
                }
                float shared = Overlap1D(walls[i].Z, walls[i].Sz, walls[i + 1].Z, walls[i + 1].Sz);
                if (Math.Abs(shared - overlap) > 0.08f)
                {
                    fail.Append("cling Z overlap ").Append(shared.ToString("0.00", CultureInfo.InvariantCulture))
                        .Append(" is not 2.20 m; ");
                    ok = false;
                }
                if (AabbGap(walls[i], walls[i + 1]) < 2.5f)
                {
                    fail.Append("cling corners are snaggy; ");
                    ok = false;
                }
            }
            return ok;
        }

        static bool ClingGroundOk(Solid[] solids, StringBuilder fail)
        {
            // Open mulch east of the walls and west of the rim, beside the chain.
            const float x0 = 7.6f, x1 = 17.2f, z0 = 40f, z1 = 76f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float bottom = s.Y - s.Sy * 0.5f;
                if (bottom >= BarUnderClear - 0.001f) continue;
                if (AabbHits(s, x0, x1, z0, z1))
                {
                    fail.Append(s.Name).Append(" blocks the cling ground path; ");
                    return false;
                }
            }
            return true;
        }

        static bool BarMantleOk(Solid[] solids, StringBuilder fail)
        {
            bool ok = true;
            if (!Rhythm(solids, "BarVault_N", 12, 45.6f, 6f, 0.96f, fail)) ok = false;
            if (!Rhythm(solids, "BarVault_S", 12, 47f, 6f, 0.96f, fail)) ok = false;
            return ok;
        }

        static bool BarSlideOk(Solid[] solids, StringBuilder fail)
        {
            bool ok = true;
            int bars = 0;
            float minClear = float.MaxValue;
            for (int i = 0; i < 12; i++)
            {
                if (!TryFind(solids, "Bar_" + i.ToString(CultureInfo.InvariantCulture), out Solid bar))
                {
                    fail.Append("bar rhythm is short; ");
                    return false;
                }
                bars++;
                float bottom = bar.Y - bar.Sy * 0.5f;
                if (bottom < minClear) minClear = bottom;
                float wantX = 44f + i * 6f;
                if (Math.Abs(bar.X - wantX) > 0.05f)
                {
                    fail.Append(bar.Name).Append(" left the 6 m rhythm; ");
                    ok = false;
                }
            }
            if (minClear < BarUnderClear - 0.001f || minClear > 100f)
            {
                fail.Append("slide-under under-clear drifted; ");
                ok = false;
            }

            // Gaps between same-side lips must clear a crouched capsule (radius 0.38).
            if (!LipGaps(solids, "BarVault_N", 12, 1.2f, fail)) ok = false;
            if (!LipGaps(solids, "BarVault_S", 12, 1.2f, fail)) ok = false;

            // Centerline of the south spine stays a slide, not a lip.
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (!s.Name.StartsWith("BarVault_", StringComparison.Ordinal)) continue;
                float minZ = s.Z - s.Sz * 0.5f;
                float maxZ = s.Z + s.Sz * 0.5f;
                if (minZ < 16.9f && maxZ > 15.1f)
                {
                    fail.Append(s.Name).Append(" closes the slide-under centerline; ");
                    ok = false;
                }
            }
            return ok;
        }

        static bool Rhythm(Solid[] solids, string prefix, int count, float x0, float spacing, float lip, StringBuilder fail)
        {
            bool ok = true;
            for (int i = 0; i < count; i++)
            {
                string name = prefix + i.ToString(CultureInfo.InvariantCulture);
                if (!TryFind(solids, name, out Solid s))
                {
                    fail.Append(name).Append(" missing; ");
                    return false;
                }
                float top = s.Y + s.Sy * 0.5f;
                if (Math.Abs(top - lip) > 0.04f || Math.Abs((top - s.SupportY) - lip) > 0.04f)
                {
                    fail.Append(name).Append(" lip left the mantle cadence; ");
                    ok = false;
                }
                float wantX = x0 + i * spacing;
                if (Math.Abs(s.X - wantX) > 0.05f)
                {
                    fail.Append(name).Append(" left the 6 m rhythm; ");
                    ok = false;
                }
            }
            return ok;
        }

        static bool LipGaps(Solid[] solids, string prefix, int count, float minGap, StringBuilder fail)
        {
            for (int i = 0; i < count - 1; i++)
            {
                if (!TryFind(solids, prefix + i.ToString(CultureInfo.InvariantCulture), out Solid a)) return false;
                if (!TryFind(solids, prefix + (i + 1).ToString(CultureInfo.InvariantCulture), out Solid b)) return false;
                float gap = AabbGap(a, b);
                if (gap < minGap)
                {
                    fail.Append(prefix).Append(" slide gap ").Append(gap.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
                    return false;
                }
            }
            return true;
        }

        static readonly string[] RimChain =
        {
            "SoftPlay_DeckLow",
            "Rim_SoftN",
            "Rim_W1", "Rim_W2", "Rim_W3", "Rim_W4", "Rim_W5", "Rim_W6",
            "Rim_SlideIn",
            "Slide_T2",
            "Rim_Link",
            "Slide_StepB",
            "Slide_T3",
            "Rim_N1", "Rim_N2", "Rim_N3", "Rim_N4",
            "Rim_Corner",
            "Rim_Knight",
            "Knight_CrawlRoof",
            "Rim_Ksouth",
            "Rim_GapS",
            "Rim_Army",
            "Army_CrawlRoof",
        };

        static bool RimOk(Solid[] solids, out bool continuous, out float maxGap, StringBuilder fail)
        {
            continuous = false;
            maxGap = 0f;
            bool ok = true;
            if (!TryFind(solids, "SoftPlay_DeckLow", out Solid low) || !TryFind(solids, "SoftPlay_DeckHigh", out Solid high))
            {
                fail.Append("rim does not touch the soft-play decks; ");
                return false;
            }
            float lowTop = low.Y + low.Sy * 0.5f;
            float highTop = high.Y + high.Sy * 0.5f;
            if (AabbGap(low, high) > 0.05f || highTop - lowTop < 0.45f || highTop - lowTop > 2.0f)
            {
                fail.Append("soft-play high deck is off the low deck; ");
                ok = false;
            }

            Solid prev = default;
            bool havePrev = false;
            for (int i = 0; i < RimChain.Length; i++)
            {
                if (!TryFind(solids, RimChain[i], out Solid cur))
                {
                    fail.Append("rim missing ").Append(RimChain[i]).Append("; ");
                    return false;
                }
                if (havePrev)
                {
                    float gap = AabbGap(prev, cur);
                    if (gap > maxGap) maxGap = gap;
                    float dy = (cur.Y + cur.Sy * 0.5f) - (prev.Y + prev.Sy * 0.5f);
                    bool foot = gap <= 1.05f && dy <= 2.05f && dy >= -3.5f;
                    bool grapple = !foot && gap <= 12f && Math.Abs(dy) <= 3.2f && HookFaces(solids, prev, cur, fail);
                    if (!foot && !grapple)
                    {
                        fail.Append("rim break ").Append(prev.Name).Append(" -> ").Append(cur.Name)
                            .Append(" gap ").Append(gap.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
                        ok = false;
                    }
                }
                prev = cur;
                havePrev = true;
            }

            if (!DropOff(solids, "Rim_DropN", "Rim_N1", 66.8f, true, fail)) ok = false;
            if (!DropOff(solids, "Rim_DropE", "Rim_N3", 80.6f, false, fail)) ok = false;

            // Fort corridor z[46, 54] at the east forts stays empty. The 8 m break is the grapple.
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Kind == "ground" || solids[i].Kind == "fence") continue;
                if (solids[i].X < 118f) continue;
                if (AabbHits(solids[i], 118f, 158f, 46.05f, 53.95f))
                {
                    fail.Append(solids[i].Name).Append(" fills the fort gap; ");
                    ok = false;
                }
            }

            continuous = ok;
            return ok;
        }

        static bool DropOff(Solid[] solids, string name, string from, float lip, bool northSouth, StringBuilder fail)
        {
            if (!TryFind(solids, name, out Solid drop) || !TryFind(solids, from, out Solid src))
            {
                fail.Append(name).Append(" drop-off missing; ");
                return false;
            }
            if (AabbGap(src, drop) > 1.05f)
            {
                fail.Append(name).Append(" is off the rim; ");
                return false;
            }
            if (OverlapsOpen(drop))
            {
                fail.Append(name).Append(" enters the open bowl; ");
                return false;
            }
            bool reaches = northSouth
                ? (drop.Z - drop.Sz * 0.5f) <= lip
                : (drop.X - drop.Sx * 0.5f) <= lip;
            if (!reaches)
            {
                fail.Append(name).Append(" does not reach the bowl lip; ");
                return false;
            }
            return true;
        }

        static bool HookFaces(Solid[] solids, Solid src, Solid dest, StringBuilder fail)
        {
            string hookName = "Hook_" + dest.Name;
            if (!TryFind(solids, hookName, out Solid hook))
            {
                fail.Append(dest.Name).Append(" has no grapple face; ");
                return false;
            }
            float destTop = dest.Y + dest.Sy * 0.5f;
            float hookBottom = hook.Y - hook.Sy * 0.5f;
            if (hook.Mat != "plate" || hook.Kind != "anchor" || Math.Abs(hookBottom - destTop) > 0.03f)
            {
                fail.Append(hookName).Append(" is not a deck plate; ");
                return false;
            }
            if (AabbGap(hook, dest) > 0.02f)
            {
                fail.Append(hookName).Append(" is off its deck; ");
                return false;
            }

            float dx = Gap1(src.X, src.Sx, dest.X, dest.Sx);
            float dz = Gap1(src.Z, src.Sz, dest.Z, dest.Sz);
            bool alongX = dx >= dz;
            float thin = alongX ? hook.Sx : hook.Sz;
            float wide = alongX ? hook.Sz : hook.Sx;
            if (thin > 0.45f || wide < 1.1f)
            {
                fail.Append(hookName).Append(" is not a planar face; ");
                return false;
            }

            float edge = alongX
                ? (src.X < dest.X ? dest.X - dest.Sx * 0.5f : dest.X + dest.Sx * 0.5f)
                : (src.Z < dest.Z ? dest.Z - dest.Sz * 0.5f : dest.Z + dest.Sz * 0.5f);
            float hookC = alongX ? hook.X : hook.Z;
            if (Math.Abs(hookC - edge) > 0.85f)
            {
                fail.Append(hookName).Append(" does not face the gap; ");
                return false;
            }
            return true;
        }

        static bool TryFind(Solid[] solids, string name, out Solid found)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Name == name)
                {
                    found = solids[i];
                    return true;
                }
            }
            found = default;
            return false;
        }

        static float FaceGapX(Solid a, Solid b)
        {
            float aEast = a.X + a.Sx * 0.5f;
            float aWest = a.X - a.Sx * 0.5f;
            float bEast = b.X + b.Sx * 0.5f;
            float bWest = b.X - b.Sx * 0.5f;
            if (aEast <= bWest) return bWest - aEast;
            if (bEast <= aWest) return aWest - bEast;
            return 0f;
        }

        static float Overlap1D(float c0, float s0, float c1, float s1)
        {
            float a0 = c0 - s0 * 0.5f;
            float a1 = c0 + s0 * 0.5f;
            float b0 = c1 - s1 * 0.5f;
            float b1 = c1 + s1 * 0.5f;
            float lo = a0 > b0 ? a0 : b0;
            float hi = a1 < b1 ? a1 : b1;
            return hi > lo ? hi - lo : 0f;
        }

        static float AabbGap(Solid a, Solid b)
        {
            float dx = Gap1(a.X, a.Sx, b.X, b.Sx);
            float dz = Gap1(a.Z, a.Sz, b.Z, b.Sz);
            if (dx <= 0f && dz <= 0f) return 0f;
            if (dx <= 0f) return dz;
            if (dz <= 0f) return dx;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static float Gap1(float c0, float s0, float c1, float s1)
        {
            float a0 = c0 - s0 * 0.5f;
            float a1 = c0 + s0 * 0.5f;
            float b0 = c1 - s1 * 0.5f;
            float b1 = c1 + s1 * 0.5f;
            if (a1 < b0) return b0 - a1;
            if (b1 < a0) return a0 - b1;
            return 0f;
        }

        static string ReadText(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        static float ColliderVisualGap(Solid[] solids, Ramp[] ramps, StringBuilder fail)
        {
            float max = 0f;
            string boot = ReadText("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            if (boot == null
                || boot.IndexOf("localScale = new Vector3(s.Sx, s.Sy, s.Sz)", StringComparison.Ordinal) < 0
                || boot.IndexOf("CreatePrimitive(PrimitiveType.Cube)", StringComparison.Ordinal) < 0)
            {
                fail.Append("solid mesh is not the collider cube; ");
                max = 1f;
            }
            if (boot == null || boot.IndexOf("DestroyImmediate(col)", StringComparison.Ordinal) < 0)
            {
                fail.Append("paint kept a collider; ");
                max = 1f;
            }

            string padSrc = ReadText("Assets/Scripts/Level/LaunchPad.cs");
            if (padSrc == null
                || padSrc.IndexOf("localScale = new Vector3(1.6f, 0.12f, 1.6f)", StringComparison.Ordinal) < 0
                || padSrc.IndexOf("box.size = new Vector3(1.6f, 0.12f, 1.6f)", StringComparison.Ordinal) < 0)
            {
                fail.Append("launch pad slab does not match its collider; ");
                max = 1f;
            }
            else
            {
                // Glow is a decal on the slab. It may rise above the box, not past the match.
                float glowOver = (0.13f + 0.02f) - (0.06f + 0.06f);
                if (glowOver > max) max = glowOver;
                if (glowOver > MeshMatch)
                    fail.Append("launch pad glow overhang; ");
            }

            float seam = SandSeamLip(ramps);
            if (seam > max) max = seam;
            if (seam > MeshMatch)
                fail.Append("sand seam lip ").Append(seam.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");

            for (int i = 0; i < ramps.Length; i++)
            {
                Ramp r = ramps[i];
                if (r.Name.StartsWith("Sand", StringComparison.Ordinal)) continue;
                float gap = Math.Max(RampEndGap(r.X0, r.Y0, r.Z0, solids), RampEndGap(r.X1, r.Y1, r.Z1, solids));
                if (gap > max) max = gap;
                if (gap > MeshMatch)
                    fail.Append(r.Name).Append(" end gap ").Append(gap.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");
            }

            float lip = NeighborLip(solids);
            if (lip > max) max = lip;
            if (lip > MeshMatch)
                fail.Append("snag lip ").Append(lip.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");
            return max;
        }

        static float SandSeamLip(Ramp[] ramps)
        {
            float max = 0f;
            for (int i = 0; i < ramps.Length; i++)
            {
                if (!ramps[i].Name.StartsWith("SandCorner_", StringComparison.Ordinal)) continue;
                for (int j = i + 1; j < ramps.Length; j++)
                {
                    if (!ramps[j].Name.StartsWith("SandCorner_", StringComparison.Ordinal)) continue;
                    if (!SameCorner(ramps[i].Name, ramps[j].Name)) continue;
                    float dz = Math.Abs(ramps[i].Z0 - ramps[j].Z0);
                    if (dz > ramps[i].Width + 0.001f) continue;
                    float lip = Math.Abs(ramps[i].Y1 - ramps[j].Y1);
                    if (lip > max) max = lip;
                }
            }
            return max;
        }

        static bool SameCorner(string a, string b)
        {
            int ia = a.LastIndexOf('_');
            int ib = b.LastIndexOf('_');
            if (ia < 0 || ib < 0) return false;
            return string.CompareOrdinal(a, 0, b, 0, ia) == 0 && ia == ib;
        }

        static float RampEndGap(float x, float y, float z, Solid[] solids)
        {
            float best = Math.Min(Math.Abs(y), Math.Abs(y - BowlFloorY));
            for (int i = 0; i < solids.Length; i++)
            {
                if (DistXZ(x, z, solids[i]) > 0.5f) continue;
                float top = solids[i].Y + solids[i].Sy * 0.5f;
                float d = Math.Abs(top - y);
                if (d < best) best = d;
            }
            return best;
        }

        // A short solid sitting on another, sticking out by less than the capsule radius,
        // is a toe lip. A full ledge (overhang past the radius) is a step you can use.
        static float NeighborLip(Solid[] solids)
        {
            float max = 0f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid a = solids[i];
                if (a.Kind == "ground" || a.Kind == "fence" || a.Kind == "wall" || a.Kind == "anchor") continue;
                float aBottom = a.Y - a.Sy * 0.5f;
                float h = a.Sy;
                if (h <= MeshMatch || h > PawnStep) continue;
                float a0 = a.X - a.Sx * 0.5f;
                float a1 = a.X + a.Sx * 0.5f;
                float az0 = a.Z - a.Sz * 0.5f;
                float az1 = a.Z + a.Sz * 0.5f;
                for (int j = 0; j < solids.Length; j++)
                {
                    if (i == j) continue;
                    Solid b = solids[j];
                    if (b.Kind == "ground" || b.Kind == "fence") continue;
                    float bTop = b.Y + b.Sy * 0.5f;
                    if (Math.Abs(aBottom - bTop) > 0.03f) continue;
                    float b0 = b.X - b.Sx * 0.5f;
                    float b1 = b.X + b.Sx * 0.5f;
                    float bz0 = b.Z - b.Sz * 0.5f;
                    float bz1 = b.Z + b.Sz * 0.5f;
                    float ox = Overlap1D(a.X, a.Sx, b.X, b.Sx);
                    float oz = Overlap1D(a.Z, a.Sz, b.Z, b.Sz);
                    if (oz > 0.05f)
                    {
                        float over = Math.Max(b0 - a0, a1 - b1);
                        if (over > MeshMatch && over < PawnRadius && h > max) max = h;
                    }
                    if (ox > 0.05f)
                    {
                        float over = Math.Max(bz0 - az0, az1 - bz1);
                        if (over > MeshMatch && over < PawnRadius && h > max) max = h;
                    }
                }
            }
            return max;
        }

        static float GroundError(Solid[] solids, StringBuilder fail)
        {
            float max = 0f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float bottom = s.Y - s.Sy * 0.5f;
                float err = Math.Abs(bottom - s.SupportY);
                if (err > max) max = err;
                if (err > 0.02f)
                    fail.Append(s.Name).Append(" base error ").Append(err.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");
            }
            if (LaunchPads != null)
            {
                for (int i = 0; i < LaunchPads.Length; i++)
                {
                    PadSpot p = LaunchPads[i];
                    float surf = FloorAt(solids, p.X, p.Z);
                    float err = Math.Abs(p.Y - surf);
                    if (err > max) max = err;
                    if (err > 0.02f)
                        fail.Append(p.Name).Append(" pad float ").Append(err.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");
                }
            }
            return max;
        }

        static float FloorAt(Solid[] solids, float x, float z)
        {
            float best = BowlCut(x, z) ? BowlFloorY : 0f;
            float bestTop = -999f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "fence" || s.Kind == "wall" || s.Kind == "anchor" || s.Kind == "bar" || s.Kind == "post")
                    continue;
                if (DistXZ(x, z, s) > 0.01f) continue;
                float top = s.Y + s.Sy * 0.5f;
                if (s.Kind == "ground")
                {
                    if (top > bestTop) { bestTop = top; best = top; }
                    continue;
                }
                if (s.Sx < 1f || s.Sz < 1f) continue;
                if (top > bestTop) { bestTop = top; best = top; }
            }
            return best;
        }

        static bool BowlCut(float x, float z)
        {
            return x > 46.2f && x < 77.8f && z > 34.2f && z < 65.8f;
        }

        static int PadReport(Solid[] solids, StringBuilder fail)
        {
            if (LaunchPads == null || LaunchPads.Length < 4 || LaunchPads.Length > 6)
            {
                fail.Append("launch pads want 4-6; ");
                return LaunchPads == null ? 0 : LaunchPads.Length;
            }
            string scene = ReadText("Assets/Scenes/Play.unity");
            string boot = ReadText("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            if (scene == null || scene.IndexOf("d5b92f3c8a1e4f7b0c4d6e9f2a3b5c71", StringComparison.Ordinal) < 0
                || boot == null || boot.IndexOf("Instantiate(launchPadPrefab", StringComparison.Ordinal) < 0)
                fail.Append("launch pads are not Assets/Prefabs/LaunchPad.prefab instances; ");

            var names = new HashSet<string>();
            for (int i = 0; i < LaunchPads.Length; i++)
            {
                PadSpot p = LaunchPads[i];
                if (!names.Add(p.Name))
                    fail.Append(p.Name).Append(" duplicated; ");
                float mag = (float)Math.Sqrt(p.DirX * p.DirX + p.DirZ * p.DirZ);
                if (p.Apex < 2f || p.Speed < 4f || mag < 0.5f)
                {
                    fail.Append(p.Name).Append(" has no launch; ");
                    continue;
                }
                if (PadBuried(solids, p.X, p.Z, p.Y))
                    fail.Append(p.Name).Append(" starts inside a solid; ");
                float hang = Hang(p.Apex);
                float lx = p.X + p.DirX / mag * p.Speed * hang;
                float lz = p.Z + p.DirZ / mag * p.Speed * hang;
                float euclid = DistPoint(p.X, p.Z, lx, lz);
                if (euclid < 6f || euclid > 22f)
                    fail.Append(p.Name).Append(" hop ").Append(euclid.ToString("0.00", CultureInfo.InvariantCulture)).Append(" m; ");
                if (!LandingOk(solids, lx, lz, p.Y))
                    fail.Append(p.Name).Append(" landing misses a floor; ");
                if (ArcHits(solids, p, mag, hang))
                    fail.Append(p.Name).Append(" arc clips a solid; ");
                if (LoopShortcut(p.X, p.Z, lx, lz))
                    fail.Append(p.Name).Append(" shortcuts the loop; ");
            }
            return LaunchPads.Length;
        }

        static bool PadBuried(Solid[] solids, float x, float z, float y)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                if (DistXZ(x, z, s) > 0.5f) continue;
                float bottom = s.Y - s.Sy * 0.5f;
                float top = s.Y + s.Sy * 0.5f;
                if (bottom < y + 0.2f && top > y + 0.08f)
                    return true;
            }
            return false;
        }

        static bool LandingOk(Solid[] solids, float x, float z, float y)
        {
            if (!FloorMatches(solids, x, z, y)) return false;
            float r = 0.45f;
            if (!FloorMatches(solids, x + r, z, y)) return false;
            if (!FloorMatches(solids, x - r, z, y)) return false;
            if (!FloorMatches(solids, x, z + r, y)) return false;
            if (!FloorMatches(solids, x, z - r, y)) return false;
            return true;
        }

        static bool FloorMatches(Solid[] solids, float x, float z, float y)
        {
            if (x < 1f || z < 1f || x > MapW - 1f || z > MapD - 1f) return false;
            if (Occupies(solids, x, z, y)) return false;
            if (!BowlCut(x, z) && Math.Abs(y) <= 0.2f) return true;
            if (BowlCut(x, z) && Math.Abs(y - BowlFloorY) <= 0.2f) return true;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Sx < 1.2f || s.Sz < 1.2f) continue;
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "wall" || s.Kind == "anchor") continue;
                if (DistXZ(x, z, s) > 0.2f) continue;
                float top = s.Y + s.Sy * 0.5f;
                if (Math.Abs(top - y) <= 0.15f) return true;
            }
            return false;
        }

        static bool Occupies(Solid[] solids, float x, float z, float y)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                if (DistXZ(x, z, s) > 0.01f) continue;
                float bottom = s.Y - s.Sy * 0.5f;
                float top = s.Y + s.Sy * 0.5f;
                if (bottom < y + 0.05f && top > y + 0.08f) return true;
            }
            return false;
        }

        static bool ArcHits(Solid[] solids, PadSpot p, float mag, float hang)
        {
            float ux = p.DirX / mag;
            float uz = p.DirZ / mag;
            for (int s = 1; s < 24; s++)
            {
                float t = hang * s / 24f;
                if (t > hang - 0.05f) break;
                float x = p.X + ux * p.Speed * t;
                float z = p.Z + uz * p.Speed * t;
                float y = ArcY(p.Y, p.Apex, t);
                if (Occupies(solids, x, z, y - 0.2f)) return true;
            }
            return false;
        }

        static float Hang(float apex)
        {
            float vy = (float)Math.Sqrt(2f * RiseGravity * apex);
            float tUp = vy / RiseGravity;
            float tDown = (float)Math.Sqrt(2f * apex / (RiseGravity * FallGravity));
            return tUp + tDown;
        }

        static float ArcY(float y0, float apex, float t)
        {
            float vy = (float)Math.Sqrt(2f * RiseGravity * apex);
            float tUp = vy / RiseGravity;
            if (t <= tUp)
                return y0 + vy * t - 0.5f * RiseGravity * t * t;
            float td = t - tUp;
            return y0 + apex - 0.5f * RiseGravity * FallGravity * td * td;
        }

        static bool LoopShortcut(float x0, float z0, float x1, float z1)
        {
            float latA, latB;
            float ta = ProjectLoop(x0, z0, out latA);
            float tb = ProjectLoop(x1, z1, out latB);
            float arc = Math.Abs(ta - tb);
            if (arc > LoopLengthM * 0.5f) arc = LoopLengthM - arc;
            return latA < 10f && latB < 10f && arc > 40f;
        }

        static int ZipReport(StringBuilder fail)
        {
            if (ZipSlots == null || ZipSlots.Length < 8 || ZipSlots.Length > 12 || (ZipSlots.Length % 2) != 0)
            {
                fail.Append("zip slots want 4-6 pairs; ");
                return ZipSlots == null ? 0 : ZipSlots.Length / 2;
            }
            string boot = ReadText("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            if (boot == null || boot.IndexOf("new GameObject(z.Name)", StringComparison.Ordinal) < 0)
                fail.Append("zip slots are not empty markers; ");
            var seen = new HashSet<string>();
            int pairs = 0;
            for (int i = 0; i < ZipSlots.Length; i++)
            {
                ZipMark z = ZipSlots[i];
                if (!seen.Add(z.Name))
                    fail.Append(z.Name).Append(" duplicated; ");
                bool a = z.Name.EndsWith("_A", StringComparison.Ordinal);
                bool b = z.Name.EndsWith("_B", StringComparison.Ordinal);
                if (!a && !b)
                    fail.Append(z.Name).Append(" is not an A/B end; ");
                if (!a) continue;
                string other = z.Name.Substring(0, z.Name.Length - 2) + "_B";
                bool found = false;
                for (int j = 0; j < ZipSlots.Length; j++)
                {
                    if (ZipSlots[j].Name != other) continue;
                    found = true;
                    pairs++;
                    float d = DistPoint(z.X, z.Z, ZipSlots[j].X, ZipSlots[j].Z);
                    if (d < 8f || d > 55f)
                        fail.Append(z.Name).Append(" span ").Append(d.ToString("0.0", CultureInfo.InvariantCulture)).Append("; ");
                    if (LoopShortcut(z.X, z.Z, ZipSlots[j].X, ZipSlots[j].Z))
                        fail.Append(z.Name).Append(" would skip the loop; ");
                }
                if (!found)
                    fail.Append(other).Append(" missing; ");
            }
            if (pairs < 4 || pairs > 6)
                fail.Append("zip pairs ").Append(pairs.ToString(CultureInfo.InvariantCulture)).Append("; ");
            return pairs;
        }

        static void GrappleReport(Solid[] solids, StringBuilder fail)
        {
            int plates = 0;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (!s.Name.StartsWith("Hook_", StringComparison.Ordinal)) continue;
                plates++;
                if (s.Mat != "plate" || s.Kind != "anchor")
                    fail.Append(s.Name).Append(" is not an orange plate; ");
                float thin = Math.Min(s.Sx, s.Sz);
                float wide = Math.Max(s.Sx, s.Sz);
                if (thin > 0.45f || wide < 1.1f)
                    fail.Append(s.Name).Append(" is not a planar face; ");
                if (!PlateOnDeck(solids, s))
                    fail.Append(s.Name).Append(" is off its deck; ");
                if (!HasApproach(solids, s))
                    fail.Append(s.Name).Append(" has no approach; ");
                if (PlateSkipsLoop(solids, s))
                    fail.Append(s.Name).Append(" shortcuts the loop; ");
            }
            if (plates < 10)
                fail.Append("grapple plates are thin; ");
        }

        static bool PlateOnDeck(Solid[] solids, Solid plate)
        {
            float bottom = plate.Y - plate.Sy * 0.5f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid d = solids[i];
                if (d.Name == plate.Name) continue;
                float top = d.Y + d.Sy * 0.5f;
                if (Math.Abs(top - bottom) > 0.03f) continue;
                if (OverlapXZ(plate, d, 0.02f)) return true;
            }
            return false;
        }

        static bool HasApproach(Solid[] solids, Solid plate)
        {
            for (float x = plate.X - 18f; x <= plate.X + 18f; x += 3f)
            {
                for (float z = plate.Z - 18f; z <= plate.Z + 18f; z += 3f)
                {
                    float dx = x - plate.X;
                    float dz = z - plate.Z;
                    float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (dist < 4f || dist > 16f) continue;
                    float stand;
                    if (!TryStand(solids, x, z, out stand)) continue;
                    if (SeesPlate(solids, plate, x, stand + 1.6f, z)) return true;
                }
            }
            return false;
        }

        static bool TryStand(Solid[] solids, float x, float z, out float y)
        {
            y = 0f;
            if (x < 1f || z < 1f || x > MapW - 1f || z > MapD - 1f) return false;
            float deck = -999f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (DistXZ(x, z, s) > 0.05f) continue;
                float top = s.Y + s.Sy * 0.5f;
                float bottom = s.Y - s.Sy * 0.5f;
                if (s.Kind == "wall" || s.Kind == "fence" || s.Kind == "anchor" || s.Kind == "bar" || s.Kind == "post")
                {
                    if (top > 0.5f && bottom < 1.5f) return false;
                    continue;
                }
                if (s.Kind == "ground" || s.Kind == "toy") continue;
                if (s.Sx < 1.1f || s.Sz < 1.1f) continue;
                float inset = EdgeInset(x, z, s);
                if (inset < 0.3f) continue;
                if (top > deck) deck = top;
            }
            if (deck > -100f)
            {
                y = deck;
                return true;
            }
            if (BowlCut(x, z))
            {
                y = BowlFloorY;
                return true;
            }
            y = 0f;
            return true;
        }

        static float EdgeInset(float x, float z, Solid s)
        {
            float ix = s.Sx * 0.5f - Math.Abs(x - s.X);
            float iz = s.Sz * 0.5f - Math.Abs(z - s.Z);
            return Math.Min(ix, iz);
        }

        static bool SeesPlate(Solid[] solids, Solid plate, float x, float y, float z)
        {
            float best = 2f;
            string who = null;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float t;
                if (!SegmentHit(s, x, y, z, plate.X, plate.Y, plate.Z, out t)) continue;
                if (t < 0.02f || t >= best) continue;
                best = t;
                who = s.Name;
            }
            if (who == null) return false;
            if (who == plate.Name) return true;
            return false;
        }

        static bool SegmentHit(Solid s, float x0, float y0, float z0, float x1, float y1, float z1, out float tEnter)
        {
            tEnter = 0f;
            float tmin = 0f;
            float tmax = 1f;
            if (!Slab(x0, x1 - x0, s.X - s.Sx * 0.5f, s.X + s.Sx * 0.5f, ref tmin, ref tmax)) return false;
            if (!Slab(y0, y1 - y0, s.Y - s.Sy * 0.5f, s.Y + s.Sy * 0.5f, ref tmin, ref tmax)) return false;
            if (!Slab(z0, z1 - z0, s.Z - s.Sz * 0.5f, s.Z + s.Sz * 0.5f, ref tmin, ref tmax)) return false;
            if (tmax < tmin || tmax < 0f || tmin > 1f) return false;
            tEnter = tmin < 0f ? 0f : tmin;
            return true;
        }

        static bool Slab(float origin, float dir, float min, float max, ref float tmin, ref float tmax)
        {
            if (Math.Abs(dir) < 1e-8f)
                return origin >= min && origin <= max;
            float inv = 1f / dir;
            float t0 = (min - origin) * inv;
            float t1 = (max - origin) * inv;
            if (t0 > t1)
            {
                float swap = t0;
                t0 = t1;
                t1 = swap;
            }
            if (t0 > tmin) tmin = t0;
            if (t1 < tmax) tmax = t1;
            return tmin <= tmax;
        }

        static bool PlateSkipsLoop(Solid[] solids, Solid plate)
        {
            float t0 = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                Pt a = LoopCcw[i];
                Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = (float)Math.Sqrt(dx * dx + dz * dz);
                int steps = Math.Max(1, (int)Math.Round(seg / 8f));
                for (int s = 0; s <= steps; s++)
                {
                    float u = s / (float)steps;
                    float x = a.X + dx * u;
                    float z = a.Z + dz * u;
                    float dist = DistPoint(x, z, plate.X, plate.Z);
                    if (dist > GrappleRange || dist < 18f) continue;
                    float lat;
                    float tp = ProjectLoop(plate.X, plate.Z, out lat);
                    float arc = Math.Abs((t0 + seg * u) - tp);
                    if (arc > LoopLengthM * 0.5f) arc = LoopLengthM - arc;
                    if (arc < 80f) continue;
                    if (SeesPlate(solids, plate, x, 1.6f, z)) return true;
                }
                t0 += seg;
            }
            return false;
        }

        static void CrossingReport(Solid[] solids, Ramp[] ramps, StringBuilder fail)
        {
            // Crossing A is the open bowl. Crossing B is the merry band.
            // A capsule center inside either rect must clear every solid.
            if (InflatedHits(solids, 52f, 72f, 40f, 58f))
                fail.Append("crossing A snags the capsule; ");
            if (InflatedHits(solids, 22f, 46f, 44f, 52f))
                fail.Append("crossing B snags the capsule; ");
            if (!MouthClear(solids, 34f, 42.2f, 34f, 55f))
                fail.Append("crossing B mouth is blocked; ");
            if (!MouthClear(solids, 42f, 50f, 62f, 50f))
                fail.Append("crossing A west mouth is blocked; ");
            if (!MouthClear(solids, 70f, 50f, 82f, 50f))
                fail.Append("crossing A east mouth is blocked; ");
            if (!MouthClear(solids, 62f, 30f, 62f, 44f))
                fail.Append("crossing A south mouth is blocked; ");
            if (!MouthClear(solids, 62f, 56f, 62f, 70f))
                fail.Append("crossing A north mouth is blocked; ");
            if (!BankContinuous(ramps))
                fail.Append("bowl banks leave a corner lip; ");
        }

        static bool InflatedHits(Solid[] solids, float x0, float x1, float z0, float z1)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top < PawnStep) continue;
                float minX = s.X - s.Sx * 0.5f - PawnRadius;
                float maxX = s.X + s.Sx * 0.5f + PawnRadius;
                float minZ = s.Z - s.Sz * 0.5f - PawnRadius;
                float maxZ = s.Z + s.Sz * 0.5f + PawnRadius;
                if (minX < x1 && maxX > x0 && minZ < z1 && maxZ > z0)
                    return true;
            }
            return false;
        }

        static bool MouthClear(Solid[] solids, float x0, float z0, float x1, float z1)
        {
            for (int s = 0; s <= 8; s++)
            {
                float u = s / 8f;
                float x = x0 + (x1 - x0) * u;
                float z = z0 + (z1 - z0) * u;
                for (int i = 0; i < solids.Length; i++)
                {
                    Solid b = solids[i];
                    if (b.Kind == "ground" || b.Kind == "fence") continue;
                    float top = b.Y + b.Sy * 0.5f;
                    if (top < PawnStep) continue;
                    if (DistXZ(x, z, b) < PawnRadius) return false;
                }
            }
            return true;
        }

        static bool BankContinuous(Ramp[] ramps)
        {
            int corners = 0;
            for (int i = 0; i < ramps.Length; i++)
            {
                if (ramps[i].Name.StartsWith("SandCorner_", StringComparison.Ordinal))
                    corners++;
            }
            return corners >= 80 && SandSeamLip(ramps) <= MeshMatch;
        }

        static void SameWallReport(Solid[] solids, StringBuilder fail)
        {
            const float len = 6.4f;
            const float overlap = 2.2f;
            const float step = len - overlap;
            const float run = 9.5f * 0.62f;
            // One wall-run reaches the facing overlap and ends before the face does,
            // so the hop does not depend on grabbing that face again.
            if (!(step + 0.2f < run && run < len - 0.2f))
            {
                fail.Append("cling hop needs a same-face regrab; ");
                return;
            }
            for (int i = 0; i < 8; i++)
            {
                if (!TryFind(solids, "Cling_" + i.ToString(CultureInfo.InvariantCulture), out Solid w))
                {
                    fail.Append("cling chain missing for same-wall; ");
                    return;
                }
                float wantX = (i % 2 == 0) ? 2.55f : 6.15f;
                if (Math.Abs(w.X - wantX) > 0.02f)
                    fail.Append(w.Name).Append(" left the facing pair; ");
            }
            for (int i = 0; i < 7; i++)
            {
                if (!TryFind(solids, "Cling_" + i.ToString(CultureInfo.InvariantCulture), out Solid a)) return;
                if (!TryFind(solids, "Cling_" + (i + 1).ToString(CultureInfo.InvariantCulture), out Solid b)) return;
                if (Math.Abs(a.X - b.X) < 1f)
                    fail.Append("cling hop stays on one face; ");
                float shared = Overlap1D(a.Z, a.Sz, b.Z, b.Sz);
                if (shared < 1.5f)
                    fail.Append("cling facing overlap is short; ");
            }
            // Flat seams: same-lane walls share a plane. The chain touches the other
            // lane between them, which is a different face.
            if (!TryFind(solids, "Cling_0", out Solid laneA) || !TryFind(solids, "Cling_2", out Solid laneB))
                return;
            if (Math.Abs(laneA.X - laneB.X) > 0.02f)
                fail.Append("cling seam split a lane; ");
        }
    }
}
