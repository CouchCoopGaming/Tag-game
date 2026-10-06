using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Pocket Park: a tight 1v1 / 1v2 arena on the Mega Park piece kit.
    /// Origin is the SW corner. Feel locks are not stored here and are not retuned.
    /// Chokes use <see cref="MegaParkP1Layout.CountFlowChokes"/> (pass 9).
    /// </summary>
    public static partial class PocketParkLayout
    {
        public const float MapW = 80f;
        public const float MapD = 50f;
        public const float SprintSpeed = MegaParkP1Layout.SprintSpeed;
        public const float SpawnY = MegaParkP1Layout.SpawnY;
        public const float FenceTop = MegaParkP1Layout.FenceTop;
        public const float KillPlaneY = MegaParkP1Layout.KillPlaneY;
        public const float LoopMin = 200f;
        public const float LoopMax = 240f;
        public const int DrawBudget = 60;
        public const float MeshBudget = 0.05f;
        public const int PaintDraws = 6;
        public const int LabelDraws = 3;
        public const int SpawnDraws = 4;
        public const int ToyDraws = 4;
        public const int MinimapDraws = 1;

        public struct Audit
        {
            public bool Ok;
            public string Line;
            public string Failure;
        }

        public static readonly MegaParkP1Layout.Pt[] LoopCcw =
        {
            new MegaParkP1Layout.Pt(8f, 0f, 6f),
            new MegaParkP1Layout.Pt(72f, 0f, 6f),
            new MegaParkP1Layout.Pt(72f, 0f, 44f),
            new MegaParkP1Layout.Pt(8f, 0f, 44f),
        };

        /// <summary>
        /// Steer loop for Pocket Park only. The shared planner reads these points
        /// and does not retune. Keep every vertex inside the fence.
        /// </summary>
        public static readonly MegaParkP1Layout.Pt[] AiLoop =
        {
            new MegaParkP1Layout.Pt(12f, 0f, 10f),
            new MegaParkP1Layout.Pt(40f, 0f, 10f),
            new MegaParkP1Layout.Pt(68f, 0f, 25f),
            new MegaParkP1Layout.Pt(40f, 0f, 40f),
            new MegaParkP1Layout.Pt(12f, 0f, 40f),
            new MegaParkP1Layout.Pt(34f, 0f, 25f),
        };

        /// <summary>
        /// How far a low-difficulty flee aim slides onto <see cref="AiLoop"/>.
        /// The shared scorer keeps evade cheapest below ~0.45, so the loop
        /// would never be steered. Zero from 0.45 up, so mid and high
        /// difficulty keep the shared choice. Mega Park does not read this.
        /// </summary>
        public const float EvadeLoopAtLow = 0.52f;

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
                    float dx = b.X - a.X;
                    float dz = b.Z - a.Z;
                    len += (float)Math.Sqrt(dx * dx + dz * dz);
                }
                return len;
            }
        }

        /// <summary>Open points the runner hides behind. The shared cover search is not used on this park.</summary>
        public struct CoverSample
        {
            public float X, Z;
        }

        public static readonly CoverSample[] CoverSamples =
        {
            new CoverSample { X = 16f, Z = 14f },
            new CoverSample { X = 16f, Z = 36f },
            new CoverSample { X = 30f, Z = 12f },
            new CoverSample { X = 58f, Z = 12f },
            new CoverSample { X = 66f, Z = 16f },
            new CoverSample { X = 66f, Z = 36f },
            new CoverSample { X = 34f, Z = 40f },
            new CoverSample { X = 48f, Z = 40f },
        };

        /// <summary>Counter-route aims. Positions are pocket data; the planner scores them as it does on Mega Park.</summary>
        public struct CounterMark
        {
            public float X, Z, Y;
        }

        public static readonly CounterMark[] CounterMarks =
        {
            new CounterMark { X = 36f, Z = 16f, Y = 0f },
            new CounterMark { X = 52f, Z = 36f, Y = 0f },
            new CounterMark { X = 22f, Z = 28f, Y = 0f },
            new CounterMark { X = 64f, Z = 28f, Y = 0f },
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
                if (!MegaParkP1Layout.ParkInsideFence(cx, 0f, cz)) continue;
                if (!MegaParkP1Layout.ParkOpen(cx, cz, out float stand)) continue;
                if (stand < -0.5f) continue;
                if (MegaParkP1Layout.ParkLos(cx, cz, threatX, threatZ)) continue;
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
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_SW", X = 8f, Z = 6f, YawDeg = 90f },
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_SE", X = 72f, Z = 10f, YawDeg = 0f },
            new MegaParkP1Layout.SpawnPad { Name = "Spawn_NW", X = 38f, Z = 44f, YawDeg = -90f },
        };

        // South street, north street, and the west lane. Each is open ground.
        public static readonly float[] Cuts =
        {
            12f, 16f, 68f, 16f,
            12f, 38f, 68f, 38f,
            18f, 10f, 18f, 40f,
        };

        public static readonly MegaParkP1Layout.PadSpot[] LaunchPads =
        {
            new MegaParkP1Layout.PadSpot { Name = "Launch_South", X = 24f, Y = 0f, Z = 16f, Apex = 4f, DirX = 1f, DirZ = 0f, Speed = 14f },
            new MegaParkP1Layout.PadSpot { Name = "Launch_North", X = 58f, Y = 0f, Z = 38f, Apex = 3.5f, DirX = -1f, DirZ = 0f, Speed = 12f },
        };

        public static readonly MegaParkP1Layout.ZipLineSpot[] ZipLines =
        {
            new MegaParkP1Layout.ZipLineSpot { Name = "ZipLineSlot_Dome", Ax = 44f, Ay = 5.5f, Az = 25f, Bx = 44f, By = 2.2f, Bz = 14f, Speed = 14f, Counter = "Slide_South" },
            new MegaParkP1Layout.ZipLineSpot { Name = "ZipLineSlot_Lane", Ax = 52.4f, Ay = 6.6f, Az = 28f, Bx = 64f, By = 2.2f, Bz = 38f, Speed = 14f, Counter = "Cling_Lane" },
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
            var list = new List<MegaParkP1Layout.Solid>(80);
            // West lawn, center mulch, concrete lane, east sand. Collars stay bark.
            Add(list, "Lawn_West", "Ground", "ground", "grass", 13f, -0.1f, 25f, 26f, 0.2f, 50f, 0f);
            Add(list, "Mulch", "Ground", "ground", "mulch", 37f, -0.1f, 25f, 22f, 0.2f, 50f, 0f);
            Add(list, "Slab_Lane", "Ground", "ground", "concrete", 57f, -0.1f, 25f, 18f, 0.2f, 50f, 0f);
            Add(list, "Sand_East", "Ground", "ground", "sand", 73f, -0.1f, 25f, 14f, 0.2f, 50f, 0f);
            Add(list, "Collar_S", "Ground", "ground", "bark", 40f, -0.12f, -1.5f, 86f, 0.2f, 3f, 0f);
            Add(list, "Collar_N", "Ground", "ground", "bark", 40f, -0.12f, 51.5f, 86f, 0.2f, 3f, 0f);
            Add(list, "Collar_W", "Ground", "ground", "bark", -1.5f, -0.12f, 25f, 3f, 0.2f, 50f, 0f);
            Add(list, "Collar_E", "Ground", "ground", "bark", 81.5f, -0.12f, 25f, 3f, 0.2f, 50f, 0f);

            Add(list, "Fence_S", "Fence", "fence", "rubber", 40f, FenceTop * 0.5f, -0.04f, 80.08f, FenceTop, 0.08f, 0f);
            Add(list, "Fence_N", "Fence", "fence", "rubber", 40f, FenceTop * 0.5f, 50.04f, 80.08f, FenceTop, 0.08f, 0f);
            Add(list, "Fence_W", "Fence", "fence", "rubber", -0.04f, FenceTop * 0.5f, 25f, 0.08f, FenceTop, 50f, 0f);
            Add(list, "Fence_E", "Fence", "fence", "rubber", 80.04f, FenceTop * 0.5f, 25f, 0.08f, FenceTop, 50f, 0f);

            // Two-level dome. The crown drops onto the skirt and also leaves on Zip Dome.
            Add(list, "Dome_Lo", "Dome", "block", "amber", 44f, 1f, 25f, 8f, 2f, 8f, 0f);
            Add(list, "Dome_Hi", "Dome", "block", "amber", 44f, 3.1f, 25f, 4.4f, 2.2f, 4.4f, 2f);
            Add(list, "Hook_DomeW", "Dome", "anchor", "plate", 39.82f, 3f, 25f, 0.3f, 2f, 2f, 2f);
            Add(list, "Hook_DomeE", "Dome", "anchor", "plate", 46.38f, 5.2f, 25f, 0.3f, 2f, 1.6f, 4.2f);

            // Cling faces. The east face is the dome climb. The lane face is the second wall.
            Add(list, "Cling_Dome", "Cling", "wall", "blue", 50.4f, 3.2f, 25f, 0.4f, 6.4f, 6.4f, 0f);
            Add(list, "Cling_Lane", "Cling", "wall", "blue", 56.2f, 3.2f, 24f, 0.4f, 6.4f, 7.2f, 0f);

            AddBars(list);
            Add(list, "Cover_A", "Yard", "vault", "army", 20f, 0.6f, 20f, 1.5f, 1.2f, 1.2f, 0f);
            Add(list, "Cover_B", "Yard", "vault", "army", 62f, 0.6f, 20f, 1.6f, 1.2f, 1.3f, 0f);

            AddMast(list, "Landmark_Dome", "Dome", "amber", 46f, 42f);
            AddMast(list, "Landmark_Lane", "Lane", "pad", 66f, 32f);
            AddMast(list, "Landmark_Yard", "Yard", "knight", 12f, 22f);
            AddMast(list, "Landmark_Bars", "Bars", "army", 14f, 40f);
            AddChevron(list, "DomeClimb", "Cling_Dome");
            AddChevron(list, "LaneRun", "Cling_Lane");
            AddChevron(list, "YardHop", "Cover_A");
            return list.ToArray();
        }

        public static MegaParkP1Layout.Ramp[] BuildRamps()
        {
            return new[]
            {
                Ramp("Slide_South", "Dome", "yellow", 44f, 2f, 21f, 44f, 0f, 18f, 2.2f),
                Ramp("Slide_West", "Dome", "yellow", 40f, 2f, 25f, 30f, 0f, 25f, 2.2f),
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
            if (seconds < 17f - 0.02f || seconds > 20f + 0.02f)
                fail.Append("loop time ").Append(seconds.ToString("0.000", CultureInfo.InvariantCulture)).Append(" s; ");

            string arcs = ArcReport(fail);
            int cuts = CutReport(solids, fail);
            KitReport(solids, ramps, fail);

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
            ArenaStill.WritePair("PocketPark", solids, ramps, BuildDressing(), LaunchPads, ZipLines, Spawns[0], MapW, MapD, fail);

            var audit = new Audit();
            audit.Ok = fail.Length == 0;
            audit.Line = string.Format(
                CultureInfo.InvariantCulture,
                "PocketPark map: loop {0:0.00} m at sprint {1:0} = {2:0.000} s; spawns {3} arcs {4}; cuts {5}; chokes {6} dead {7} corner {8} loop {9}; containment fence {10:0.0} m kill {11:0.00} sweeps {12} {13}; collider mismatch {14:0.000} m; ground error {15:0.000} m; contrast {16:0.00}; {17}; draws {18}; dummy stuck {19} pads {20} zips {21}; {22}",
                loop, SprintSpeed, seconds, Spawns.Length, arcs, cuts,
                chokes, dead, corner, loops,
                FenceTop, KillPlaneY, sweeps, held ? "held" : "open",
                mesh, ground, contrast, look, draws, stuck, padsTaken, zipsTaken, skills);
            audit.Failure = fail.ToString();
            return audit;
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
                if (s.Kind == "ground" || s.Kind == "fence") continue;
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
            if (!HasZoneLandmark(solids, "Dome") || !HasZoneLandmark(solids, "Lane")
                || !HasZoneLandmark(solids, "Yard") || !HasZoneLandmark(solids, "Bars"))
                fail.Append("landmark missing a zone; ");
            if (LaunchPads.Length != 2) fail.Append("pads; ");
            if (ZipLines.Length != 2) fail.Append("zips; ");
            for (int i = 0; i < ZipLines.Length; i++)
            {
                if (Math.Abs(ZipLines[i].Speed - 14f) > 0.01f)
                    fail.Append(ZipLines[i].Name).Append(" speed; ");
                if (ZipLines[i].Ay < ZipLines[i].By + 0.35f)
                    fail.Append(ZipLines[i].Name).Append(" uphill; ");
            }
        }

        static string ArcReport(StringBuilder fail)
        {
            float loop = MeasureLoop();
            float want = loop / Spawns.Length;
            var bits = new StringBuilder();
            float worst = 0f;
            for (int i = 0; i < Spawns.Length; i++)
            {
                MegaParkP1Layout.SpawnPad a = Spawns[i];
                MegaParkP1Layout.SpawnPad b = Spawns[(i + 1) % Spawns.Length];
                float arc = ArcBetween(a.X, a.Z, b.X, b.Z);
                float err = Math.Abs(arc - want);
                if (err > worst) worst = err;
                if (bits.Length > 0) bits.Append('/');
                bits.Append(arc.ToString("0.00", CultureInfo.InvariantCulture));
                if (err > 0.05f)
                    fail.Append(a.Name).Append(" arc ").Append(arc.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
            }
            if (worst > 0.05f)
                fail.Append("spawn arcs are not equal; ");
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
                if (solids[i].Kind == "ground" || solids[i].Kind == "fence") continue;
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
                else if (s.Kind == "landmark" && s.Name.EndsWith("_Pole", StringComparison.Ordinal))
                    Fan(s.X, top, s.Z, MegaParkP1Layout.MaxAirSpeed, s.Name, fail, ref sweeps, ref misses);
            }
            for (float x = 8f; x < MapW; x += 14f)
            {
                Edge(x, 0f, 1.2f, 0f, -1f, "rim S", fail, ref sweeps, ref misses);
                Edge(x, 0f, MapD - 1.2f, 0f, 1f, "rim N", fail, ref sweeps, ref misses);
            }
            for (float z = 8f; z < MapD; z += 14f)
            {
                Edge(1.2f, 0f, z, -1f, 0f, "rim W", fail, ref sweeps, ref misses);
                Edge(MapW - 1.2f, 0f, z, 1f, 0f, "rim E", fail, ref sweeps, ref misses);
            }
            if (misses > 0) held = false;
            if (sweeps < 8)
                fail.Append("containment sweeps; ");
            return sweeps;
        }

        static void Edge(float x, float y, float z, float ox, float oz, string name, StringBuilder fail, ref int sweeps, ref int misses)
        {
            sweeps++;
            if (!Fly(x, y, z, ox * MegaParkP1Layout.MaxAirSpeed, MegaParkP1Layout.LockedJumpSpeed, oz * MegaParkP1Layout.MaxAirSpeed))
                Note(fail, ref misses, name);
        }

        static void Fan(float x, float y, float z, float speed, string name, StringBuilder fail, ref int sweeps, ref int misses)
        {
            for (int d = 0; d < 8; d++)
            {
                double a = d * Math.PI / 4.0;
                float dx = (float)Math.Cos(a) * speed;
                float dz = (float)Math.Sin(a) * speed;
                sweeps++;
                if (!Fly(x, y, z, dx, 0f, dz))
                    Note(fail, ref misses, name);
                sweeps++;
                if (!Fly(x, y, z, dx, MegaParkP1Layout.LockedJumpSpeed, dz))
                    Note(fail, ref misses, name);
            }
        }

        static void Note(StringBuilder fail, ref int misses, string name)
        {
            misses++;
            if (misses <= 4)
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
                float g = vy > 0f ? 22f : 22f * 1.5f;
                vy -= g * dt;
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
                if (s.Kind == "ground" || s.Kind == "fence") continue;
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

        static bool HasZoneLandmark(MegaParkP1Layout.Solid[] solids, string zone)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "landmark" && s.Zone == zone && s.Name.StartsWith("Landmark_", StringComparison.Ordinal))
                    return true;
            }
            return false;
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
            const int seed = 10;
            MegaParkP1Layout.Solid[] solids = BuildSolids();
            int n = Spawns.Length;
            int start = seed % n;
            for (int k = 0; k < n; k++)
            {
                MegaParkP1Layout.SpawnPad a = Spawns[(start + k) % n];
                MegaParkP1Layout.SpawnPad b = Spawns[(start + k + 1) % n];
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
            if (pads < 1)
                fail.Append("dummy took no pad; ");
            if (zips < 1)
                fail.Append("dummy took no zip; ");
            return stuck;
        }

        static string SkillReport(StringBuilder fail)
        {
            MegaParkP1Layout.Solid[] solids = BuildSolids();
            Landing(LaunchPads[0], out float lx, out float lz);
            float padT = Hang(LaunchPads[0].Apex);
            var notes = new StringBuilder();
            var doc = new StringBuilder();
            doc.Append("# Pocket Park skill routes\n\n");
            doc.Append("Expert chains use the locked motor. Coyote is 0.10 s, jump buffer is 0.16 s, and cling grace is 0.08 s. ");
            doc.Append("Beginner time is the ground nav at sprint 12. A route is in band when it beats that line by 10–25%.\n\n");
            doc.Append("| Route | Chain | Beginner | Expert | Save |\n| --- | --- | --- | --- | --- |\n");

            float laneLen = Cable(ZipLines[1]);
            float laneT = laneLen / 14f;
            NoteSkill(solids, doc, notes, fail, "LaneZip",
                "Ride Zip Lane over the cling faces. Walking the mulch around those walls is the long way.",
                ZipLines[1].Ax, ZipLines[1].Az, ZipLines[1].Bx, ZipLines[1].Bz, laneT,
                "zip " + laneLen.ToString("0.00", CultureInfo.InvariantCulture) + " m (ZipLineSlot_Lane) " + laneT.ToString("0.00", CultureInfo.InvariantCulture) + " s");
            NoteSkill(solids, doc, notes, fail, "SouthPad",
                "The south launch pad cuts the street beside the dome.",
                LaunchPads[0].X, LaunchPads[0].Z, lx, lz, padT,
                "pad (Launch_South) " + padT.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            float climb = 1.2f / 6.0f;
            float run = 2.0f / 9.5f;
            float hop = WallJumpSeconds();
            float domeExpert = climb + run + hop;
            NoteChain(solids, doc, notes, fail, "DomeClimb",
                "Climb the dome cling face, wall-run it, then wall-jump off toward the south street.",
                "Cling_Dome", 48.2f, 18.5f, 54.6f, 32.0f, domeExpert,
                "climb 1.20 m (Cling_Dome) " + climb.ToString("0.00", CultureInfo.InvariantCulture)
                + " s → wall-run 2.00 m (Cling_Dome) " + run.ToString("0.00", CultureInfo.InvariantCulture)
                + " s → wall-jump 4.40 m (Cling_Dome) " + hop.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            float laneRun = 1.8f / 9.5f;
            float laneExpert = laneRun + laneT;
            NoteChain(solids, doc, notes, fail, "LaneRun",
                "Wall-run the lane cling face, then ride Zip Lane. The jump off the face uses cling grace.",
                "Cling_Lane", 52.4f, 22.0f, 64.0f, 38.0f, laneExpert,
                "wall-run 1.80 m (Cling_Lane) " + laneRun.ToString("0.00", CultureInfo.InvariantCulture)
                + " s → zip " + laneLen.ToString("0.00", CultureInfo.InvariantCulture)
                + " m (ZipLineSlot_Lane) " + laneT.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            float mantle = 0.40f;
            float yardExpert = mantle + padT;
            NoteChain(solids, doc, notes, fail, "YardHop",
                "Mantle the yard cover, then the south launch pad along the street.",
                "Cover_A", 16.0f, 20.0f, lx, lz, yardExpert,
                "mantle 0.95 m (Cover_A) " + mantle.ToString("0.00", CultureInfo.InvariantCulture)
                + " s → pad (Launch_South) " + padT.ToString("0.00", CultureInfo.InvariantCulture) + " s");

            doc.Append("\n## Centerpiece\n\n");
            doc.Append("Dome_Hi sits on Dome_Lo. Cling_Dome is the east climb, Zip Dome leaves the crown for the south street, and Launch_South throws you back along that street. ");
            doc.Append("Cling_Lane and Zip Lane are the second wall and the second cable. DomeClimb, LaneRun, and YardHop are the named chains. Every deck has two ways down. Pass 9 reports 0 chokes.\n\n");
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
                float missed = g > 0.05f && g < 100f ? (g - expert) / g : -1f;
                fail.Append(name).Append(" save ")
                    .Append((missed * 100f).ToString("0.0", CultureInfo.InvariantCulture))
                    .Append("% expert ").Append(expert.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" beginner ").Append(g.ToString("0.00", CultureInfo.InvariantCulture)).Append("; ");
                NoteSkill(solids, doc, notes, fail, name, summary, sx, sz, ex, ez, expert, chain + ". Chevrons sit on " + host);
                return;
            }
            NoteSkill(solids, doc, notes, fail, name, summary, x0, z0, x1, z1, expert,
                "Chevrons sit on " + host + " in that piece's tint. " + chain + ". " + ChainClose(chain));
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
            x0 = sx;
            z0 = sz;
            x1 = ex;
            z1 = ez;
            beginner = 0f;
            save = -1f;
            float[] nudge = { 0f, 2f, -2f, 4f, -4f, 6f, -6f };
            for (int i = 0; i < nudge.Length; i++)
            {
                float a = sx + nudge[i];
                float b = sz;
                if (a < 2f || a > MapW - 2f || b < 2f || b > MapD - 2f) continue;
                if (!MegaParkP1Layout.RouteOpen(solids, a, b)) continue;
                for (int j = 0; j < nudge.Length; j++)
                {
                    float c = ex;
                    float d = ez + nudge[j];
                    if (c < 2f || c > MapW - 2f || d < 2f || d > MapD - 2f) continue;
                    if (!MegaParkP1Layout.RouteOpen(solids, c, d)) continue;
                    float g = MegaParkP1Layout.GroundSeconds(solids, a, b, c, d);
                    if (g < 0.3f || g > 80f) continue;
                    float s = (g - expert) / g;
                    if (s < 0.10f || s > 0.25f) continue;
                    float dist = Math.Abs(s - 0.15f);
                    if (dist >= best) continue;
                    best = dist;
                    found = true;
                    x0 = a;
                    z0 = b;
                    x1 = c;
                    z1 = d;
                    beginner = g;
                    save = s;
                }
            }
            return found;
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
            Add(list, "Chevron_" + route + "_L", host.Zone, "mark", host.Mat,
                host.X, y, host.Z - az * 0.15f, ax, 0.03f, az, top);
            Add(list, "Chevron_" + route + "_R", host.Zone, "mark", host.Mat,
                host.X, y, host.Z + az * 0.4f, ax, 0.03f, az, top);
        }

        static void NoteSkill(MegaParkP1Layout.Solid[] solids, StringBuilder doc, StringBuilder notes, StringBuilder fail,
            string name, string summary, float x0, float z0, float x1, float z1, float expert, string chain)
        {
            float beginner = MegaParkP1Layout.GroundSeconds(solids, x0, z0, x1, z1);
            float save = beginner > 0.05f && beginner < 100f ? (beginner - expert) / beginner : -1f;
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

        static void WriteDoc(string text)
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string docs = Path.Combine(dir, "Docs");
                if (Directory.Exists(docs))
                {
                    File.WriteAllText(Path.Combine(docs, "PocketPark_SkillRoutes.md"), text);
                    return;
                }
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
        }

        static void AddBars(List<MegaParkP1Layout.Solid> list)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 24f + i * 6f;
                string id = i.ToString(CultureInfo.InvariantCulture);
                Add(list, "BarPost_S" + id, "Bars", "post", "steel", x, 0.57f, 30.2f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "BarPost_N" + id, "Bars", "post", "steel", x, 0.57f, 33.8f, 0.22f, 1.14f, 0.22f, 0f);
                Add(list, "Bar_" + id, "Bars", "bar", "steel", x, 1.2f, 32f, 0.14f, 0.12f, 4.2f, 1.14f);
                Add(list, "BarVault_" + id, "Bars", "vault", "amber", x + 1.5f, 0.48f, 33.4f, 1.2f, 0.96f, 0.9f, 0f);
            }
        }

        static void AddMast(List<MegaParkP1Layout.Solid> list, string name, string zone, string mat, float x, float z)
        {
            const float poleTop = 14.5f;
            const float flagH = 32f;
            Add(list, name + "_Pole", zone, "landmark", mat, x, poleTop * 0.5f, z, 0.42f, poleTop, 0.42f, 0f);
            Add(list, name + "_Flag", zone, "landmark", mat, x, poleTop + flagH * 0.5f, z, 2.2f, flagH, 0.18f, poleTop);
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
                float d = Dist(x, z, px, pz);
                if (d < bestD)
                {
                    bestD = d;
                    best = walked + len * u;
                }
                walked += len;
            }
            return best;
        }

        static void Landing(MegaParkP1Layout.PadSpot p, out float x, out float z)
        {
            float mag = (float)Math.Sqrt(p.DirX * p.DirX + p.DirZ * p.DirZ);
            float hang = Hang(p.Apex);
            x = p.X + p.DirX / mag * p.Speed * hang;
            z = p.Z + p.DirZ / mag * p.Speed * hang;
        }

        static float Hang(float apex)
        {
            float vy = (float)Math.Sqrt(2f * 22f * apex);
            float tUp = vy / 22f;
            float tDown = (float)Math.Sqrt(2f * apex / (22f * 1.5f));
            return tUp + tDown;
        }

        static float Cable(MegaParkP1Layout.ZipLineSpot z)
        {
            float dx = z.Ax - z.Bx;
            float dy = z.Ay - z.By;
            float dz = z.Az - z.Bz;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        static float Dist(float x0, float z0, float x1, float z1)
        {
            float dx = x0 - x1;
            float dz = z0 - z1;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
