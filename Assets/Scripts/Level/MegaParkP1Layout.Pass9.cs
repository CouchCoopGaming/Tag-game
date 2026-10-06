using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Pass 9 flow: chokepoints, a seeded chase heatmap, and skill chains.
    /// DummyRunner's evade loop aims outside the 160×100 fence, so the sim uses
    /// the same policy on the real 472 m loop: loop, pad when the landing
    /// increases the gap, zip when the exit increases the gap. It pathfinds
    /// and takes a pad or zip when that is the faster route.
    /// Feel locks are read, not written.
    /// </summary>
    public static partial class MegaParkP1Layout
    {
        static bool IncludePass9 = true;

        struct FlowLens
        {
            public bool On;
            public bool NoBowl;
            public float MapW, MapD;
            public ZipLineSpot[] Zips;
            public PadSpot[] Pads;
        }

        static FlowLens _flowLens;

        static float FlowMapW => _flowLens.On ? _flowLens.MapW : MapW;
        static float FlowMapD => _flowLens.On ? _flowLens.MapD : MapD;

        static ZipLineSpot[] FlowZips()
        {
            return _flowLens.On && _flowLens.Zips != null ? _flowLens.Zips : ZipLines;
        }

        static PadSpot[] FlowPads()
        {
            return _flowLens.On && _flowLens.Pads != null ? _flowLens.Pads : LaunchPads;
        }

        /// <summary>
        /// Pass 9 choke rules on another park that shares this piece kit.
        /// Mega Park leaves the lens off, so its own audit is unchanged.
        /// </summary>
        public static void UseFlow(float mapW, float mapD, ZipLineSpot[] zips, PadSpot[] pads, bool noBowl)
        {
            _flowLens = new FlowLens
            {
                On = true,
                NoBowl = noBowl,
                MapW = mapW,
                MapD = mapD,
                Zips = zips,
                Pads = pads,
            };
        }

        public static void ClearFlow()
        {
            _flowLens = default;
        }

        const float ItSpeed = 12.55f;
        const float SightCampSeconds = 3f;
        const float LoopStallMin = 8f;
        const float LoopStallMax = 25f;
        const float WallRunSpeed = 9.5f;
        const float WallRunMaxSeconds = 0.62f;
        const float ClimbSpeed = 6.0f;
        const float MantleSeconds = 0.40f;
        const float ZipRideSpeed = 14f;
        const float JumpBufferWindow = 0.16f;
        const float CoyoteWindow = 0.10f;
        const float ClingGraceWindow = 0.08f;

        static bool ChevronMark(Solid s)
        {
            return s.Kind == "mark" && s.Name.StartsWith("Chevron_", StringComparison.Ordinal);
        }

        static void AddPass9Solids(List<Solid> list)
        {
            // SoftPlay_DeckHigh only dropped onto SoftPlay_DeckLow. The lip reaches
            // past that deck to a ground step, so the cap has two ways down.
            Add(list, "SoftPlay_LipW", "Z1", "block", "soft", 9f, 2.75f, 26f, 4f, 1.5f, 1.4f, 2f);
            Add(list, "SoftPlay_StepW", "Z1", "block", "soft", 5.85f, 1f, 26f, 2.3f, 2f, 1.4f, 0f);
            AddSkillChevrons(list);
        }

        struct SkillLeg
        {
            public string Verb;
            public float Meters;
            public string Piece;
        }

        struct SkillDef
        {
            public string Name;
            public string Host;
            public string Summary;
            public float StartX, StartZ, EndX, EndZ;
            public SkillLeg[] Legs;
        }

        static SkillDef[] SkillDefs()
        {
            return new[]
            {
                new SkillDef
                {
                    Name = "ClingSkip",
                    Host = "Cling_5",
                    Summary = "Climb the east cling face, wall-run it, wall-jump toward the lane, then the cling launch pad.",
                    StartX = 6.5f,
                    StartZ = 50.2f,
                    EndX = 24.25f,
                    EndZ = 38f,
                    Legs = new[]
                    {
                        new SkillLeg { Verb = "climb", Meters = 0.8f, Piece = "Cling_5" },
                        new SkillLeg { Verb = "wall-run", Meters = 2.0f, Piece = "Cling_5" },
                        new SkillLeg { Verb = "wall-jump", Meters = 4.4f, Piece = "Cling_5" },
                        new SkillLeg { Verb = "pad", Meters = 0f, Piece = "Launch_ClingEast" },
                    },
                },
                new SkillDef
                {
                    Name = "BowlLine",
                    Host = "Rim_N1",
                    Summary = "Wall-run the north rim, then ride Zip Bowl into the sand. The jump off the face uses cling grace.",
                    StartX = 63.15f,
                    StartZ = 77.35f,
                    EndX = 66.4f,
                    EndZ = 50.2f,
                    Legs = new[]
                    {
                        new SkillLeg { Verb = "wall-run", Meters = 1.25f, Piece = "Rim_N1" },
                        new SkillLeg { Verb = "zip", Meters = 0f, Piece = "ZipLineSlot_Bowl" },
                    },
                },
                new SkillDef
                {
                    Name = "KnightLine",
                    Host = "Knight_Hi",
                    Summary = "Wall-run the knight lip, then Zip Bars down to open ground.",
                    StartX = 148.45f,
                    StartZ = 71.2f,
                    EndX = 148.45f,
                    EndZ = 57.6f,
                    Legs = new[]
                    {
                        new SkillLeg { Verb = "wall-run", Meters = 2.4f, Piece = "Knight_Hi" },
                        new SkillLeg { Verb = "zip", Meters = 0f, Piece = "ZipLineSlot_Bars" },
                    },
                },
                new SkillDef
                {
                    Name = "FortHop",
                    Host = "Army_Lo",
                    Summary = "Mantle the army lip, then the fort-gap pad across the open ground between the forts.",
                    StartX = 140f,
                    StartZ = 34.5f,
                    EndX = 140f,
                    EndZ = 56f,
                    Legs = new[]
                    {
                        new SkillLeg { Verb = "mantle", Meters = 0.95f, Piece = "Army_Lo" },
                        new SkillLeg { Verb = "pad", Meters = 0f, Piece = "Launch_FortGap" },
                    },
                },
                new SkillDef
                {
                    Name = "WestZip",
                    Host = "Slide_T3",
                    Summary = "Climb the +5 face, wall-run it, wall-jump to the cable, ride Zip WestRim to the mulch.",
                    StartX = 46.2f,
                    StartZ = 78f,
                    EndX = 36.4f,
                    EndZ = 60.4f,
                    Legs = new[]
                    {
                        new SkillLeg { Verb = "climb", Meters = 0.5f, Piece = "Slide_T3" },
                        new SkillLeg { Verb = "wall-run", Meters = 3.6f, Piece = "Slide_T3" },
                        new SkillLeg { Verb = "wall-jump", Meters = 4.4f, Piece = "Slide_T3" },
                        new SkillLeg { Verb = "zip", Meters = 0f, Piece = "ZipLineSlot_WestRim" },
                    },
                },
            };
        }

        static void AddSkillChevrons(List<Solid> list)
        {
            SkillDef[] defs = SkillDefs();
            for (int i = 0; i < defs.Length; i++)
            {
                Solid host = default;
                bool found = false;
                for (int s = 0; s < list.Count; s++)
                {
                    if (list[s].Name != defs[i].Host) continue;
                    host = list[s];
                    found = true;
                    break;
                }
                if (!found) continue;
                float top = host.Y + host.Sy * 0.5f;
                float y = top + 0.015f;
                float halfX = host.Sx * 0.5f - 0.1f;
                float halfZ = host.Sz * 0.5f - 0.1f;
                if (halfX < 0.08f) halfX = host.Sx * 0.5f - 0.04f;
                if (halfZ < 0.08f) halfZ = host.Sz * 0.5f - 0.04f;
                float ax = Math.Max(0.08f, Math.Min(0.16f, halfX));
                float az = Math.Max(0.08f, Math.Min(0.18f, halfZ));
                float z0 = host.Z - Math.Min(0.28f, halfZ * 0.45f);
                float z1 = host.Z + Math.Min(0.12f, halfZ * 0.25f);
                if (Math.Abs(z1 - z0) < az + 0.04f)
                    z1 = z0 + az + 0.06f;
                Add(list, "Chevron_" + defs[i].Name + "_L", host.Zone, "mark", host.Mat,
                    host.X - ax * 0.15f, y, z0, ax, 0.03f, az * 0.85f, top);
                Add(list, "Chevron_" + defs[i].Name + "_R", host.Zone, "mark", host.Mat,
                    host.X + ax * 0.35f, y, z1, ax, 0.03f, az * 0.85f, top);
            }
        }

        static string FlowReport(Solid[] solids, Ramp[] ramps, StringBuilder fail)
        {
            int beforeDead = 0, beforeCorner = 0, beforeLoop = 0;
            if (IncludePass9)
            {
                IncludePass9 = false;
                Solid[] priorS = BuildSolids();
                Ramp[] priorR = BuildRamps();
                CountChokes(priorS, priorR, out beforeDead, out beforeCorner, out beforeLoop);
                IncludePass9 = true;
            }

            var dead = new List<string>();
            var corner = new List<string>();
            var loops = new List<string>();
            CountChokes(solids, ramps, dead, corner, loops);
            int after = dead.Count + corner.Count + loops.Count;
            int before = beforeDead + beforeCorner + beforeLoop;

            Nav nav = BuildNav(solids);
            var skillNotes = new List<string>();
            var skillDocs = new StringBuilder();
            EvaluateSkills(nav, skillNotes, skillDocs, fail);

            int[] zoneVisits = new int[ZoneBoxes.Length];
            int[,] heat = RunChases(nav, zoneVisits, out int samples);
            WriteHeatmap(heat, zoneVisits, samples);
            WriteSkillDoc(skillDocs, before, after, beforeDead, beforeCorner, beforeLoop, dead, corner, loops);

            if (Math.Abs(MeasureLoop() - LoopLengthM) > 0.05f)
                fail.Append("pass 9 moved the 472 m loop; ");
            if (dead.Count > 0)
                fail.Append("dead-end decks ").Append(JoinNames(dead)).Append("; ");
            if (corner.Count > 0)
                fail.Append("camp corners ").Append(JoinNames(corner)).Append("; ");
            if (loops.Count > 0)
                fail.Append("stall loops ").Append(JoinNames(loops)).Append("; ");

            int used = 0;
            int dominant = 0;
            string hot = "";
            float hotShare = 0f;
            var shareBits = new StringBuilder();
            for (int i = 0; i < zoneVisits.Length; i++)
            {
                float share = samples > 0 ? zoneVisits[i] / (float)samples : 0f;
                if (shareBits.Length > 0) shareBits.Append(' ');
                shareBits.Append(ZoneBoxes[i].Id).Append(' ').Append((share * 100f).ToString("0", CultureInfo.InvariantCulture)).Append('%');
                if (zoneVisits[i] > 0) used++;
                if (share > hotShare)
                {
                    hotShare = share;
                    hot = ZoneBoxes[i].Id;
                }
                if (share >= 0.28f) dominant++;
                if (zoneVisits[i] == 0)
                    fail.Append(ZoneBoxes[i].Id).Append(" has no chase traffic; ");
            }
            if (dominant > 0)
                fail.Append(hot).Append(" dominates the chase at ").Append((hotShare * 100f).ToString("0", CultureInfo.InvariantCulture)).Append("%; ");

            return string.Format(
                CultureInfo.InvariantCulture,
                "flow chokes {0}->{1} dead {2}->{3} corner {4}->{5} loop {6}->{7}; heat {8}; {9}",
                before, after, beforeDead, dead.Count, beforeCorner, corner.Count, beforeLoop, loops.Count,
                shareBits, skillNotes.Count == 0 ? "skills 0" : string.Join(" ", skillNotes.ToArray()));
        }

        static string JoinNames(List<string> names)
        {
            int n = Math.Min(names.Count, 8);
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(names[i]);
            }
            if (names.Count > n) sb.Append(" +").Append((names.Count - n).ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        static void CountChokes(Solid[] solids, Ramp[] ramps, out int dead, out int corner, out int loops)
        {
            var a = new List<string>();
            var b = new List<string>();
            var c = new List<string>();
            CountChokes(solids, ramps, a, b, c);
            dead = a.Count;
            corner = b.Count;
            loops = c.Count;
        }

        static void CountChokes(Solid[] solids, Ramp[] ramps, List<string> dead, List<string> corner, List<string> loops)
        {
            DeadEndDecks(solids, ramps, dead);
            CampCorners(solids, corner);
            StallLoops(solids, loops);
        }

        struct DeckNode
        {
            public string Name;
            public float Top, X0, X1, Z0, Z1;
            public bool Deck;
        }

        static void DeadEndDecks(Solid[] solids, Ramp[] ramps, List<string> dead)
        {
            var nodes = new List<DeckNode>();
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind != "block" && s.Kind != "cap" && s.Kind != "vault") continue;
                if (s.Sx < 1.15f || s.Sz < 1.15f) continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top < 1.05f) continue;
                nodes.Add(new DeckNode
                {
                    Name = s.Name,
                    Top = top,
                    X0 = s.X - s.Sx * 0.5f,
                    X1 = s.X + s.Sx * 0.5f,
                    Z0 = s.Z - s.Sz * 0.5f,
                    Z1 = s.Z + s.Sz * 0.5f,
                    Deck = s.Sx >= 2.2f && s.Sz >= 2.2f && top >= 1.4f,
                });
            }
            int n = nodes.Count;
            if (n == 0) return;
            int[] parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(nodes[i].Top - nodes[j].Top) > 0.55f) continue;
                    if (BoxGap(nodes[i], nodes[j]) > 0.95f) continue;
                    Union(parent, i, j);
                }
            }

            var seen = new bool[n];
            for (int i = 0; i < n; i++)
            {
                int root = Find(parent, i);
                if (seen[root]) continue;
                seen[root] = true;
                bool anyDeck = false;
                string name = nodes[i].Name;
                float bestArea = 0f;
                for (int j = 0; j < n; j++)
                {
                    if (Find(parent, j) != root) continue;
                    if (!nodes[j].Deck) continue;
                    anyDeck = true;
                    float area = (nodes[j].X1 - nodes[j].X0) * (nodes[j].Z1 - nodes[j].Z0);
                    if (area > bestArea)
                    {
                        bestArea = area;
                        name = nodes[j].Name;
                    }
                }
                if (!anyDeck) continue;

                var exits = new HashSet<string>();
                for (int j = 0; j < n; j++)
                {
                    if (Find(parent, j) != root) continue;
                    for (int k = 0; k < n; k++)
                    {
                        if (Find(parent, k) == root) continue;
                        float drop = nodes[j].Top - nodes[k].Top;
                        if (drop < 0.45f || drop > 3.3f) continue;
                        if (BoxGap(nodes[j], nodes[k]) > 0.55f) continue;
                        exits.Add("d" + Find(parent, k).ToString(CultureInfo.InvariantCulture));
                    }
                    int sides = GroundSides(solids, nodes[j]);
                    if ((sides & 1) != 0) exits.Add("gN");
                    if ((sides & 2) != 0) exits.Add("gE");
                    if ((sides & 4) != 0) exits.Add("gS");
                    if ((sides & 8) != 0) exits.Add("gW");
                }
                if (RampPortal(ramps, nodes, parent, root)) exits.Add("ramp");
                if (ZipPortal(nodes, parent, root)) exits.Add("zip");
                if (exits.Count < 2)
                    dead.Add(name + "(" + exits.Count.ToString(CultureInfo.InvariantCulture) + ")");
            }
        }

        static int GroundSides(Solid[] solids, DeckNode node)
        {
            float midX = (node.X0 + node.X1) * 0.5f;
            float midZ = (node.Z0 + node.Z1) * 0.5f;
            float[] xs = { midX, node.X1 + 0.9f, midX, node.X0 - 0.9f };
            float[] zs = { node.Z1 + 0.9f, midZ, node.Z0 - 0.9f, midZ };
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                if (!TryStand(solids, xs[i], zs[i], out float y)) continue;
                if (node.Top - y < 0.7f || node.Top - y > 6f) continue;
                bool onDeck = false;
                for (int s = 0; s < solids.Length; s++)
                {
                    Solid solid = solids[s];
                    if (solid.Kind == "ground" || solid.Kind == "fence" || solid.Kind == "wall") continue;
                    if (DistXZ(xs[i], zs[i], solid) > 0.05f) continue;
                    float top = solid.Y + solid.Sy * 0.5f;
                    if (Math.Abs(top - y) < 0.08f && solid.Sx >= 1.1f && solid.Sz >= 1.1f)
                        onDeck = true;
                }
                if (!onDeck) mask |= 1 << i;
            }
            return mask;
        }

        static bool RampPortal(Ramp[] ramps, List<DeckNode> nodes, int[] parent, int root)
        {
            if (ramps == null) return false;
            for (int i = 0; i < ramps.Length; i++)
            {
                Ramp r = ramps[i];
                if (r.Mat != "yellow") continue;
                bool highA = r.Y0 >= r.Y1;
                float hy = highA ? r.Y0 : r.Y1;
                float hx = highA ? r.X0 : r.X1;
                float hz = highA ? r.Z0 : r.Z1;
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (Find(parent, j) != root) continue;
                    if (Math.Abs(hy - nodes[j].Top) > 0.35f) continue;
                    if (hx < nodes[j].X0 - 0.8f || hx > nodes[j].X1 + 0.8f) continue;
                    if (hz < nodes[j].Z0 - 0.8f || hz > nodes[j].Z1 + 0.8f) continue;
                    return true;
                }
            }
            return false;
        }

        static bool ZipPortal(List<DeckNode> nodes, int[] parent, int root)
        {
            ZipLineSpot[] lines = FlowZips();
            if (lines == null) return false;
            for (int i = 0; i < lines.Length; i++)
            {
                ZipLineSpot z = lines[i];
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (Find(parent, j) != root) continue;
                    if (Math.Abs(z.Ay - nodes[j].Top) > 2.4f) continue;
                    if (z.Ax < nodes[j].X0 - 1.6f || z.Ax > nodes[j].X1 + 1.6f) continue;
                    if (z.Az < nodes[j].Z0 - 1.6f || z.Az > nodes[j].Z1 + 1.6f) continue;
                    return true;
                }
            }
            return false;
        }

        static float BoxGap(DeckNode a, DeckNode b)
        {
            float dx = 0f;
            if (a.X1 < b.X0) dx = b.X0 - a.X1;
            else if (b.X1 < a.X0) dx = a.X0 - b.X1;
            float dz = 0f;
            if (a.Z1 < b.Z0) dz = b.Z0 - a.Z1;
            else if (b.Z1 < a.Z0) dz = a.Z0 - b.Z1;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra != rb) parent[rb] = ra;
        }

        static void CampCorners(Solid[] solids, List<string> corner)
        {
            Nav nav = BuildNav(solids);
            float[] block = SightTops(solids, nav);
            float limit = SightCampSeconds * ItSpeed;
            int n = nav.Floor.Length;
            var kept = new List<float>();
            for (int i = 0; i < n; i++)
            {
                if (float.IsNaN(nav.Floor[i])) continue;
                if (!CampNook(nav, block, i)) continue;
                float seen = SeenApproach(nav, block, i);
                if (seen <= limit) continue;
                CellCenter(nav, i, out float x, out float z);
                bool near = false;
                for (int k = 0; k < kept.Count; k += 2)
                {
                    float dx = kept[k] - x;
                    float dz = kept[k + 1] - z;
                    if (dx * dx + dz * dz < 12f * 12f) near = true;
                }
                if (near) continue;
                kept.Add(x);
                kept.Add(z);
                corner.Add(x.ToString("0", CultureInfo.InvariantCulture) + "," + z.ToString("0", CultureInfo.InvariantCulture)
                    + "/" + (seen / ItSpeed).ToString("0.0", CultureInfo.InvariantCulture) + "s");
            }
        }

        static bool CampNook(Nav nav, float[] block, int index)
        {
            if (CardinalDegree(nav, index) == 1) return true;
            int ix = index % nav.Nx;
            int iz = index / nav.Nx;
            bool e = !OpenCell(nav, ix + 1, iz);
            bool w = !OpenCell(nav, ix - 1, iz);
            bool n = !OpenCell(nav, ix, iz + 1);
            bool s = !OpenCell(nav, ix, iz - 1);
            int blocked = (e ? 1 : 0) + (w ? 1 : 0) + (n ? 1 : 0) + (s ? 1 : 0);
            if (blocked != 2) return false;
            if (e && w) return false;
            if (n && s) return false;
            int tall = 0;
            if (e && BlocksEye(nav, block, ix + 1, iz)) tall++;
            if (w && BlocksEye(nav, block, ix - 1, iz)) tall++;
            if (n && BlocksEye(nav, block, ix, iz + 1)) tall++;
            if (s && BlocksEye(nav, block, ix, iz - 1)) tall++;
            return tall == 2;
        }

        static bool BlocksEye(Nav nav, float[] block, int ix, int iz)
        {
            if (ix < 0 || iz < 0 || ix >= nav.Nx || iz >= nav.Nz) return false;
            return block[iz * nav.Nx + ix] >= 1.5f;
        }

        static int CardinalDegree(Nav nav, int index)
        {
            int ix = index % nav.Nx;
            int iz = index / nav.Nx;
            int deg = 0;
            if (OpenCell(nav, ix + 1, iz)) deg++;
            if (OpenCell(nav, ix - 1, iz)) deg++;
            if (OpenCell(nav, ix, iz + 1)) deg++;
            if (OpenCell(nav, ix, iz - 1)) deg++;
            return deg;
        }

        static bool OpenCell(Nav nav, int ix, int iz)
        {
            if (ix < 0 || iz < 0 || ix >= nav.Nx || iz >= nav.Nz) return false;
            return !float.IsNaN(nav.Floor[iz * nav.Nx + ix]);
        }

        static float[] SightTops(Solid[] solids, Nav nav)
        {
            var top = new float[nav.Floor.Length];
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "mark" || s.Kind == "toy") continue;
                float crown = s.Y + s.Sy * 0.5f;
                if (crown < 1.25f) continue;
                float x0 = s.X - s.Sx * 0.5f;
                float x1 = s.X + s.Sx * 0.5f;
                float z0 = s.Z - s.Sz * 0.5f;
                float z1 = s.Z + s.Sz * 0.5f;
                int ix0 = Math.Max(0, (int)((x0 - nav.X0) / nav.Cell));
                int ix1 = Math.Min(nav.Nx - 1, (int)((x1 - nav.X0) / nav.Cell));
                int iz0 = Math.Max(0, (int)((z0 - nav.Z0) / nav.Cell));
                int iz1 = Math.Min(nav.Nz - 1, (int)((z1 - nav.Z0) / nav.Cell));
                for (int iz = iz0; iz <= iz1; iz++)
                {
                    for (int ix = ix0; ix <= ix1; ix++)
                    {
                        float x = nav.X0 + (ix + 0.5f) * nav.Cell;
                        float z = nav.Z0 + (iz + 0.5f) * nav.Cell;
                        if (x < x0 || x > x1 || z < z0 || z > z1) continue;
                        int idx = iz * nav.Nx + ix;
                        if (crown > top[idx]) top[idx] = crown;
                    }
                }
            }
            return top;
        }

        static float SeenApproach(Nav nav, float[] block, int pocket)
        {
            int n = nav.Floor.Length;
            var dist = new float[n];
            for (int i = 0; i < n; i++) dist[i] = -1f;
            dist[pocket] = 0f;
            var q = new Queue<int>();
            q.Enqueue(pocket);
            float best = 0f;
            bool hid = false;
            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                if (dist[cur] > 70f) continue;
                int ix = cur % nav.Nx;
                int iz = cur / nav.Nx;
                bool see = LineClear(nav, block, pocket, cur);
                if (see && dist[cur] > best) best = dist[cur];
                if (!see) hid = true;
                for (int d = 0; d < 4; d++)
                {
                    int nx = ix + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    int nz = iz + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (!OpenCell(nav, nx, nz)) continue;
                    int nxt = nz * nav.Nx + nx;
                    float step = nav.Cell;
                    float y0 = nav.Floor[cur];
                    float y1 = nav.Floor[nxt];
                    if (Math.Abs(y1 - y0) > 2.4f) continue;
                    float nd = dist[cur] + step;
                    if (dist[nxt] >= 0f && dist[nxt] <= nd) continue;
                    dist[nxt] = nd;
                    q.Enqueue(nxt);
                }
            }
            if (!hid) return best;
            float frontier = 999f;
            for (int i = 0; i < n; i++)
            {
                if (dist[i] < 0f) continue;
                if (!LineClear(nav, block, pocket, i)) continue;
                int ix = i % nav.Nx;
                int iz = i / nav.Nx;
                bool edge = false;
                for (int d = 0; d < 4 && !edge; d++)
                {
                    int nx = ix + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    int nz = iz + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (!OpenCell(nav, nx, nz)) continue;
                    int nxt = nz * nav.Nx + nx;
                    if (dist[nxt] < 0f) continue;
                    if (!LineClear(nav, block, pocket, nxt)) edge = true;
                }
                if (edge && dist[i] < frontier) frontier = dist[i];
            }
            return frontier > 900f ? best : frontier;
        }

        static bool LineClear(Nav nav, float[] block, int a, int b)
        {
            if (a == b) return true;
            CellCenter(nav, a, out float x0, out float z0);
            CellCenter(nav, b, out float x1, out float z1);
            float dx = x1 - x0;
            float dz = z1 - z0;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            int steps = Math.Max(1, (int)(len / (nav.Cell * 0.5f)));
            for (int s = 1; s < steps; s++)
            {
                float t = s / (float)steps;
                int idx = NavIndex(nav, x0 + dx * t, z0 + dz * t, out int ix, out int iz);
                if (idx < 0) return false;
                if (block[idx] >= 1.25f) return false;
            }
            return true;
        }

        static void StallLoops(Solid[] solids, List<string> loops)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "wall" || s.Kind == "mark" || s.Kind == "landmark")
                    continue;
                if (Math.Min(s.Sx, s.Sz) < 2.5f) continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top <= MantleMax) continue;
                float orbit = 2f * (s.Sx + s.Sz) + 5f;
                if (orbit < LoopStallMin || orbit >= LoopStallMax) continue;
                if (seen.Contains(s.Name)) continue;
                if (JoinedToRim(solids, s)) continue;
                if (SolidHasCut(solids, s)) continue;
                seen.Add(s.Name);
                loops.Add(s.Name + "/" + orbit.ToString("0", CultureInfo.InvariantCulture) + "m");
            }
        }

        static bool JoinedToRim(Solid[] solids, Solid s)
        {
            float top = s.Y + s.Sy * 0.5f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid o = solids[i];
                if (o.Name == s.Name) continue;
                if (o.Kind == "ground" || o.Kind == "fence" || o.Kind == "mark" || o.Kind == "landmark") continue;
                if (Math.Min(o.Sx, o.Sz) < 1.4f) continue;
                float ot = o.Y + o.Sy * 0.5f;
                if (Math.Abs(ot - top) > 0.8f) continue;
                if (AabbGap(s, o) <= 1.2f) return true;
            }
            return false;
        }

        static bool SolidHasCut(Solid[] solids, Solid s)
        {
            float x0 = s.X - s.Sx * 0.5f - 1.2f;
            float x1 = s.X + s.Sx * 0.5f + 1.2f;
            float z0 = s.Z - s.Sz * 0.5f - 1.2f;
            float z1 = s.Z + s.Sz * 0.5f + 1.2f;
            float top = s.Y + s.Sy * 0.5f;
            PadSpot[] flowPads = FlowPads();
            if (flowPads != null)
            {
                for (int i = 0; i < flowPads.Length; i++)
                {
                    PadSpot p = flowPads[i];
                    float mag = (float)Math.Sqrt(p.DirX * p.DirX + p.DirZ * p.DirZ);
                    if (mag < 0.1f) continue;
                    float hang = Hang(p.Apex);
                    float lx = p.X + p.DirX / mag * p.Speed * hang;
                    float lz = p.Z + p.DirZ / mag * p.Speed * hang;
                    if (SegmentCrosses(p.X, p.Z, lx, lz, x0, x1, z0, z1)) return true;
                }
            }
            ZipLineSpot[] flowZips = FlowZips();
            if (flowZips != null)
            {
                for (int i = 0; i < flowZips.Length; i++)
                {
                    ZipLineSpot z = flowZips[i];
                    if (Math.Abs(z.Ax - s.X) < s.Sx && Math.Abs(z.Az - s.Z) < s.Sz) return true;
                    if (SegmentCrosses(z.Ax, z.Az, z.Bx, z.Bz, x0, x1, z0, z1)) return true;
                }
            }
            for (int i = 0; i < solids.Length; i++)
            {
                Solid o = solids[i];
                if (o.Name == s.Name) continue;
                float ot = o.Y + o.Sy * 0.5f;
                if (ot > top - 0.2f) continue;
                if (ot < top - 3.4f) continue;
                if (o.Sx < 1.1f || o.Sz < 1.1f) continue;
                float gap = AabbGap(s, o);
                if (gap <= 0.6f) return true;
            }
            return false;
        }

        static bool SegmentCrosses(float x0, float z0, float x1, float z1, float minX, float maxX, float minZ, float maxZ)
        {
            bool in0 = x0 >= minX && x0 <= maxX && z0 >= minZ && z0 <= maxZ;
            bool in1 = x1 >= minX && x1 <= maxX && z1 >= minZ && z1 <= maxZ;
            if (in0 && in1) return false;
            for (int s = 0; s <= 8; s++)
            {
                float t = s / 8f;
                float x = x0 + (x1 - x0) * t;
                float z = z0 + (z1 - z0) * t;
                if (x >= minX && x <= maxX && z >= minZ && z <= maxZ) return true;
            }
            return false;
        }

        static void EvaluateSkills(Nav nav, List<string> notes, StringBuilder doc, StringBuilder fail)
        {
            SkillDef[] defs = SkillDefs();
            doc.Append("# Mega Park skill routes\n\n");
            doc.Append("Expert chains use the locked motor. Coyote is 0.10 s, jump buffer is 0.16 s, ");
            doc.Append("and cling grace is 0.08 s. Nothing in these chains asks for a tighter window. ");
            doc.Append("Beginner time is the ground nav at sprint 12, the same grid the chase sim uses. ");
            doc.Append("A route is in band when it beats that line by 10–25%.\n\n");
            doc.Append("| Route | Chain | Beginner | Expert | Save |\n| --- | --- | --- | --- | --- |\n");
            for (int i = 0; i < defs.Length; i++)
            {
                SkillDef d = defs[i];
                float expert = 0f;
                string why = null;
                for (int L = 0; L < d.Legs.Length; L++)
                {
                    float leg = LegSeconds(d.Legs[L], out string legWhy);
                    if (legWhy != null) why = legWhy;
                    expert += leg;
                }
                int start = NearestOpen(nav, d.StartX, d.StartZ);
                int goal = NearestOpen(nav, d.EndX, d.EndZ);
                float beginner = (start < 0 || goal < 0) ? 999f : RouteCost(nav, start, goal, false, null);
                float save = beginner > 0.05f && beginner < 100f ? (beginner - expert) / beginner : -1f;
                string saveText = (save * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
                notes.Add(d.Name + " " + expert.ToString("0.00", CultureInfo.InvariantCulture) + "s/" + saveText);
                doc.Append("| ").Append(d.Name).Append(" | ").Append(d.Summary).Append(" | ");
                doc.Append(beginner.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s | ");
                doc.Append(expert.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s | ");
                doc.Append(saveText).Append(" |\n\n");
                doc.Append(LegDoc(d));
                if (why != null)
                    fail.Append(d.Name).Append(' ').Append(why).Append("; ");
                else if (beginner > 100f)
                    fail.Append(d.Name).Append(" beginner route is blocked; ");
                else if (save < 0.10f || save > 0.25f)
                    fail.Append(d.Name).Append(" save ").Append(saveText)
                        .Append(" expert ").Append(expert.ToString("0.00", CultureInfo.InvariantCulture))
                        .Append(" beginner ").Append(beginner.ToString("0.00", CultureInfo.InvariantCulture))
                        .Append("; ");
            }
        }

        static string LegDoc(SkillDef d)
        {
            var sb = new StringBuilder();
            sb.Append("Start (").Append(d.StartX.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(", ").Append(d.StartZ.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(") end (").Append(d.EndX.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append(", ").Append(d.EndZ.ToString("0.0", CultureInfo.InvariantCulture)).Append("). ");
            sb.Append("Chevrons sit on ").Append(d.Host).Append(" in that piece's tint. ");
            for (int i = 0; i < d.Legs.Length; i++)
            {
                SkillLeg leg = d.Legs[i];
                float sec = LegSeconds(leg, out _);
                if (i > 0) sb.Append(" → ");
                sb.Append(leg.Verb);
                if (leg.Meters > 0.05f)
                    sb.Append(' ').Append(leg.Meters.ToString("0.00", CultureInfo.InvariantCulture)).Append(" m");
                if (!string.IsNullOrEmpty(leg.Piece))
                    sb.Append(" (").Append(leg.Piece).Append(')');
                sb.Append(' ').Append(sec.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s");
            }
            sb.Append(". ");
            bool wall = false;
            bool pad = false;
            bool zip = false;
            for (int i = 0; i < d.Legs.Length; i++)
            {
                if (d.Legs[i].Verb == "wall-run" || d.Legs[i].Verb == "wall-jump") wall = true;
                if (d.Legs[i].Verb == "pad") pad = true;
                if (d.Legs[i].Verb == "zip") zip = true;
            }
            if (wall)
                sb.Append("Wall-run leaves on cling grace ").Append(ClingGraceWindow.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" s or jump buffer ").Append(JumpBufferWindow.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s. ");
            if (pad)
                sb.Append("The pad uses its locked 0.30 s cooldown. ");
            if (zip)
                sb.Append("The zip rides at 14 m/s and regrabs on its locked cooldown. ");
            if (pad || zip)
                sb.Append("Coyote ").Append(CoyoteWindow.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" s covers the hop into that piece.\n\n");
            else
                sb.Append("Coyote ").Append(CoyoteWindow.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" s and jump buffer ").Append(JumpBufferWindow.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" s cover the links.\n\n");
            return sb.ToString();
        }

        static float LegSeconds(SkillLeg leg, out string why)
        {
            why = null;
            if (leg.Verb == "sprint")
                return leg.Meters / SprintSpeed;
            if (leg.Verb == "wall-run")
            {
                float cap = WallRunSpeed * WallRunMaxSeconds;
                if (leg.Meters < 1.2f || leg.Meters > cap + 0.05f)
                    why = "wall-run " + leg.Meters.ToString("0.0", CultureInfo.InvariantCulture) + " m is outside one 0.62 s face";
                return leg.Meters / WallRunSpeed;
            }
            if (leg.Verb == "wall-jump")
            {
                float hang = WallJumpHang();
                if (leg.Meters < 3.2f || leg.Meters > 4.8f)
                    why = "wall-jump reach left the locked hop";
                return hang;
            }
            if (leg.Verb == "climb")
            {
                if (leg.Meters < 0.4f || leg.Meters > 3.2f)
                    why = "climb left the 6.0 m/s rise";
                return leg.Meters / ClimbSpeed;
            }
            if (leg.Verb == "mantle")
                return MantleSeconds;
            if (leg.Verb == "zip")
            {
                ZipLineSpot z = default;
                bool found = false;
                if (ZipLines != null)
                {
                    for (int i = 0; i < ZipLines.Length; i++)
                    {
                        if (ZipLines[i].Name != leg.Piece) continue;
                        z = ZipLines[i];
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    why = "zip " + leg.Piece + " missing";
                    return 0f;
                }
                if (Math.Abs(z.Speed - ZipRideSpeed) > 0.01f)
                    why = "zip speed is not 14";
                return CableLen(z) / z.Speed;
            }
            if (leg.Verb == "pad")
            {
                PadSpot p = default;
                bool found = false;
                if (LaunchPads != null)
                {
                    for (int i = 0; i < LaunchPads.Length; i++)
                    {
                        if (LaunchPads[i].Name != leg.Piece) continue;
                        p = LaunchPads[i];
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    why = "pad " + leg.Piece + " missing";
                    return 0f;
                }
                return Hang(p.Apex);
            }
            why = "unknown verb " + leg.Verb;
            return 0f;
        }

        static float WallJumpHang()
        {
            const float vy = 6.2f;
            float tUp = vy / RiseGravity;
            float h = vy * tUp * 0.5f;
            float tDown = (float)Math.Sqrt(Math.Max(0f, 2f * h / (RiseGravity * FallGravity)));
            return tUp + tDown;
        }

        static int[,] RunChases(Nav nav, int[] zoneVisits, out int samples)
        {
            const int gw = 16;
            const int gh = 10;
            var heat = new int[gw, gh];
            samples = 0;
            SpawnPad[] pads = AllPads();
            int pair = 0;
            for (int a = 0; a < pads.Length; a++)
            {
                for (int b = 0; b < pads.Length; b++)
                {
                    if (a == b) continue;
                    if ((pair++ % 2) == 1) continue;
                    ChaseOne(nav, heat, zoneVisits, ref samples, pads[a], pads[b], gw, gh);
                }
            }
            return heat;
        }

        static void ChaseOne(Nav nav, int[,] heat, int[] zoneVisits, ref int samples, SpawnPad itPad, SpawnPad runPad, int gw, int gh)
        {
            int it = NearestOpen(nav, itPad.X, itPad.Z);
            int runner = NearestOpen(nav, runPad.X, runPad.Z);
            if (it < 0 || runner < 0) return;
            float t = 0f;
            int guard = 0;
            float ahead = 0f;
            while (t < 14f && guard++ < 80)
            {
                CellCenter(nav, it, out float ix, out float iz);
                CellCenter(nav, runner, out float rx, out float rz);
                float gap = DistPoint(ix, iz, rx, rz);
                if (gap < 1.55f) break;
                Stamp(heat, zoneVisits, ref samples, ix, iz, gw, gh);
                Stamp(heat, zoneVisits, ref samples, rx, rz, gw, gh);
                int runNext = RunnerStep(nav, runner, it, ref ahead);
                int itNext = ItStep(nav, it, runner);
                float step = nav.Cell / SprintSpeed;
                if (runNext >= 0) runner = runNext;
                if (itNext >= 0) it = itNext;
                t += step;
            }
        }

        static void Stamp(int[,] heat, int[] zoneVisits, ref int samples, float x, float z, int gw, int gh)
        {
            int hx = (int)(x / MapW * gw);
            int hz = (int)(z / MapD * gh);
            if (hx < 0) hx = 0;
            if (hz < 0) hz = 0;
            if (hx >= gw) hx = gw - 1;
            if (hz >= gh) hz = gh - 1;
            heat[hx, hz]++;
            samples++;
            for (int i = 0; i < ZoneBoxes.Length; i++)
            {
                ZoneBox box = ZoneBoxes[i];
                if (x < box.X0 || x > box.X1 || z < box.Z0 || z > box.Z1) continue;
                zoneVisits[i]++;
                break;
            }
        }

        static int RunnerStep(Nav nav, int runner, int it, ref float ahead)
        {
            CellCenter(nav, runner, out float x, out float z);
            CellCenter(nav, it, out float ix, out float iz);
            if (TryEscapeToy(nav, x, z, ix, iz, true, out int toy)) return toy;
            ahead += nav.Cell;
            LoopAhead(x, z, 8f, out float tx, out float tz);
            return BestNeighbor(nav, runner, tx, tz, ix, iz, true);
        }

        static int ItStep(Nav nav, int it, int runner)
        {
            CellCenter(nav, it, out float x, out float z);
            CellCenter(nav, runner, out float rx, out float rz);
            if (TryEscapeToy(nav, x, z, rx, rz, false, out int toy)) return toy;
            return BestNeighbor(nav, it, rx, rz, rx, rz, false);
        }

        static bool TryEscapeToy(Nav nav, float x, float z, float tx, float tz, bool flee, out int dest)
        {
            dest = -1;
            float here = DistPoint(x, z, tx, tz);
            PadSpot[] flowPads = FlowPads();
            if (flowPads != null)
            {
                for (int i = 0; i < flowPads.Length; i++)
                {
                    PadSpot p = flowPads[i];
                    if (DistPoint(x, z, p.X, p.Z) > 3.2f) continue;
                    float mag = (float)Math.Sqrt(p.DirX * p.DirX + p.DirZ * p.DirZ);
                    if (mag < 0.1f) continue;
                    float hang = Hang(p.Apex);
                    float lx = p.X + p.DirX / mag * p.Speed * hang;
                    float lz = p.Z + p.DirZ / mag * p.Speed * hang;
                    float there = DistPoint(lx, lz, tx, tz);
                    bool take = flee ? there > here + 4f : there + 4f < here;
                    if (!take) continue;
                    dest = NearestOpen(nav, lx, lz);
                    return dest >= 0;
                }
            }
            ZipLineSpot[] flowZips = FlowZips();
            if (flowZips != null)
            {
                for (int i = 0; i < flowZips.Length; i++)
                {
                    ZipLineSpot line = flowZips[i];
                    if (DistPoint(x, z, line.Ax, line.Az) > 4.5f) continue;
                    float there = DistPoint(line.Bx, line.Bz, tx, tz);
                    bool take = flee ? there > here + 4f : there + 4f < here;
                    if (!take) continue;
                    dest = NearestOpen(nav, line.Bx, line.Bz);
                    return dest >= 0;
                }
            }
            return false;
        }

        static void LoopAhead(float x, float z, float lead, out float tx, out float tz)
        {
            float lat;
            float t = ProjectLoop(x, z, out lat);
            float goal = t + lead;
            if (goal >= LoopLengthM) goal -= LoopLengthM;
            PointAtArc(goal, out tx, out tz);
        }

        static void PointAtArc(float arc, out float x, out float z)
        {
            float walked = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                Pt a = LoopCcw[i];
                Pt b = LoopCcw[(i + 1) % LoopCcw.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float len = (float)Math.Sqrt(dx * dx + dz * dz);
                if (walked + len >= arc || i == LoopCcw.Length - 1)
                {
                    float u = len > 0.01f ? (arc - walked) / len : 0f;
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                    x = a.X + dx * u;
                    z = a.Z + dz * u;
                    return;
                }
                walked += len;
            }
            x = LoopCcw[0].X;
            z = LoopCcw[0].Z;
        }

        static int BestNeighbor(Nav nav, int cur, float tx, float tz, float ax, float az, bool flee)
        {
            int ix = cur % nav.Nx;
            int iz = cur / nav.Nx;
            int best = cur;
            float bestScore = flee ? -999f : 999f;
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int nx = ix + dx;
                    int nz = iz + dz;
                    if (!OpenCell(nav, nx, nz)) continue;
                    int nxt = nz * nav.Nx + nx;
                    float y0 = nav.Floor[cur];
                    float y1 = nav.Floor[nxt];
                    if (y1 > y0 + 2.2f) continue;
                    CellCenter(nav, nxt, out float x, out float z);
                    float to = DistPoint(x, z, tx, tz);
                    float from = DistPoint(x, z, ax, az);
                    float score = flee ? from - to * 0.35f : to;
                    if (flee)
                    {
                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = nxt;
                        }
                    }
                    else if (score < bestScore)
                    {
                        bestScore = score;
                        best = nxt;
                    }
                }
            }
            return best;
        }

        static void WriteHeatmap(int[,] heat, int[] zoneVisits, int samples)
        {
            string root = RepoRoot();
            string docs = Path.Combine(root, "Docs");
            Directory.CreateDirectory(docs);
            int gw = heat.GetLength(0);
            int gh = heat.GetLength(1);
            int max = 1;
            for (int z = 0; z < gh; z++)
            {
                for (int x = 0; x < gw; x++)
                    if (heat[x, z] > max) max = heat[x, z];
            }
            var csv = new StringBuilder();
            csv.Append("x0,z0,x1,z1,visits\n");
            for (int z = 0; z < gh; z++)
            {
                for (int x = 0; x < gw; x++)
                {
                    float x0 = x / (float)gw * MapW;
                    float x1 = (x + 1) / (float)gw * MapW;
                    float z0 = z / (float)gh * MapD;
                    float z1 = (z + 1) / (float)gh * MapD;
                    csv.Append(x0.ToString("0.0", CultureInfo.InvariantCulture)).Append(',');
                    csv.Append(z0.ToString("0.0", CultureInfo.InvariantCulture)).Append(',');
                    csv.Append(x1.ToString("0.0", CultureInfo.InvariantCulture)).Append(',');
                    csv.Append(z1.ToString("0.0", CultureInfo.InvariantCulture)).Append(',');
                    csv.Append(heat[x, z].ToString(CultureInfo.InvariantCulture)).Append('\n');
                }
            }
            csv.Append("zone,visits,share\n");
            for (int i = 0; i < zoneVisits.Length; i++)
            {
                float share = samples > 0 ? zoneVisits[i] / (float)samples : 0f;
                csv.Append(ZoneBoxes[i].Id).Append(',');
                csv.Append(zoneVisits[i].ToString(CultureInfo.InvariantCulture)).Append(',');
                csv.Append(share.ToString("0.000", CultureInfo.InvariantCulture)).Append('\n');
            }
            File.WriteAllText(Path.Combine(docs, "MegaPark_ChaseHeatmap.csv"), csv.ToString());

            const int scale = 8;
            int pw = gw * scale;
            int ph = gh * scale;
            var rgb = new byte[pw * ph * 3];
            for (int z = 0; z < gh; z++)
            {
                for (int x = 0; x < gw; x++)
                {
                    float u = heat[x, z] / (float)max;
                    byte r = (byte)(20 + 220 * u);
                    byte g = (byte)(28 + 80 * (1f - u));
                    byte b = (byte)(48 + 140 * (1f - u));
                    for (int dy = 0; dy < scale; dy++)
                    {
                        for (int dx = 0; dx < scale; dx++)
                        {
                            int px = x * scale + dx;
                            int py = (gh - 1 - z) * scale + dy;
                            int o = (py * pw + px) * 3;
                            rgb[o] = r;
                            rgb[o + 1] = g;
                            rgb[o + 2] = b;
                        }
                    }
                }
            }
            WritePng(Path.Combine(docs, "MegaPark_ChaseHeatmap.png"), rgb, pw, ph);
        }

        static void WriteSkillDoc(StringBuilder body, int before, int after, int bd, int bc, int bl, List<string> dead, List<string> corner, List<string> loops)
        {
            var sb = new StringBuilder();
            sb.Append(body);
            sb.Append("## Chokepoints\n\n");
            sb.Append("Before the pass 9 exits: ").Append(before.ToString(CultureInfo.InvariantCulture));
            sb.Append(" (dead ").Append(bd.ToString(CultureInfo.InvariantCulture));
            sb.Append(", corner ").Append(bc.ToString(CultureInfo.InvariantCulture));
            sb.Append(", loop ").Append(bl.ToString(CultureInfo.InvariantCulture)).Append("). ");
            sb.Append("After: ").Append(after.ToString(CultureInfo.InvariantCulture));
            sb.Append(" (dead ").Append(dead.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(", corner ").Append(corner.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append(", loop ").Append(loops.Count.ToString(CultureInfo.InvariantCulture)).Append(").\n\n");
            if (dead.Count > 0) sb.Append("Dead ends still open: ").Append(string.Join(", ", dead.ToArray())).Append(".\n\n");
            if (corner.Count > 0) sb.Append("Camp corners still open: ").Append(string.Join(", ", corner.ToArray())).Append(".\n\n");
            if (loops.Count > 0) sb.Append("Stall loops still open: ").Append(string.Join(", ", loops.ToArray())).Append(".\n\n");
            sb.Append("Cling faces stay 6.40 m. The east mulch lane is the cut around that chain, so a single face is not scored as a stall loop. ");
            sb.Append("The 472 m loop and the 118 m spawn arcs are unchanged.\n");
            File.WriteAllText(Path.Combine(RepoRoot(), "Docs", "MegaPark_SkillRoutes.md"), sb.ToString());
        }

        static string RepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                if (File.Exists(Path.Combine(dir, "Assets", "Scripts", "Level", "MegaParkP1Layout.cs")))
                    return dir;
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Directory.GetCurrentDirectory();
        }

        static void WritePng(string path, byte[] rgb, int w, int h)
        {
            int stride = w * 3;
            var raw = new byte[(stride + 1) * h];
            for (int y = 0; y < h; y++)
            {
                raw[y * (stride + 1)] = 0;
                Buffer.BlockCopy(rgb, y * stride, raw, y * (stride + 1) + 1, stride);
            }
            byte[] deflated;
            using (var ms = new MemoryStream())
            {
                using (var def = new DeflateStream(ms, CompressionLevel.Fastest, true))
                    def.Write(raw, 0, raw.Length);
                deflated = ms.ToArray();
            }
            uint adler = Adler32(raw);
            var zlib = new byte[deflated.Length + 6];
            zlib[0] = 0x78;
            zlib[1] = 0x01;
            Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
            zlib[zlib.Length - 4] = (byte)(adler >> 24);
            zlib[zlib.Length - 3] = (byte)(adler >> 16);
            zlib[zlib.Length - 2] = (byte)(adler >> 8);
            zlib[zlib.Length - 1] = (byte)adler;

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
            fs.Write(sig, 0, sig.Length);
            var ihdr = new byte[13];
            Be(ihdr, 0, w);
            Be(ihdr, 4, h);
            ihdr[8] = 8;
            ihdr[9] = 2;
            Chunk(fs, "IHDR", ihdr);
            Chunk(fs, "IDAT", zlib);
            Chunk(fs, "IEND", new byte[0]);
        }

        static void Chunk(Stream fs, string name, byte[] data)
        {
            var len = new byte[4];
            Be(len, 0, data.Length);
            fs.Write(len, 0, 4);
            byte[] tag = Encoding.ASCII.GetBytes(name);
            fs.Write(tag, 0, 4);
            if (data.Length > 0) fs.Write(data, 0, data.Length);
            uint crc = Crc32(tag, data);
            var c = new byte[4];
            Be(c, 0, crc);
            fs.Write(c, 0, 4);
        }

        static void Be(byte[] buf, int o, int value)
        {
            uint v = (uint)value;
            buf[o] = (byte)(v >> 24);
            buf[o + 1] = (byte)(v >> 16);
            buf[o + 2] = (byte)(v >> 8);
            buf[o + 3] = (byte)v;
        }

        static void Be(byte[] buf, int o, uint value)
        {
            buf[o] = (byte)(value >> 24);
            buf[o + 1] = (byte)(value >> 16);
            buf[o + 2] = (byte)(value >> 8);
            buf[o + 3] = (byte)value;
        }

        static uint Adler32(byte[] data)
        {
            uint a = 1, b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % 65521;
                b = (b + a) % 65521;
            }
            return (b << 16) | a;
        }

        static uint Crc32(byte[] tag, byte[] data)
        {
            uint crc = 0xFFFFFFFFu;
            for (int i = 0; i < tag.Length; i++) crc = CrcStep(crc, tag[i]);
            for (int i = 0; i < data.Length; i++) crc = CrcStep(crc, data[i]);
            return crc ^ 0xFFFFFFFFu;
        }

        static uint CrcStep(uint crc, byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
            {
                uint mask = (crc & 1) == 1 ? 0xEDB88320u : 0u;
                crc = (crc >> 1) ^ mask;
            }
            return crc;
        }

        public static void CountFlowChokes(Solid[] solids, Ramp[] ramps, out int dead, out int corner, out int loops, out string detail)
        {
            var d = new List<string>();
            var c = new List<string>();
            var l = new List<string>();
            CountChokes(solids, ramps, d, c, l);
            dead = d.Count;
            corner = c.Count;
            loops = l.Count;
            detail = "dead [" + string.Join(",", d.ToArray()) + "] corner [" + string.Join(",", c.ToArray()) + "] loop [" + string.Join(",", l.ToArray()) + "]";
        }

        public static float GroundSeconds(Solid[] solids, float x0, float z0, float x1, float z1)
        {
            Nav nav = BuildNav(solids);
            int a = NearestOpen(nav, x0, z0);
            int b = NearestOpen(nav, x1, z1);
            if (a < 0 || b < 0) return 999f;
            return RouteCost(nav, a, b, false, null);
        }

        public static bool RouteOpen(Solid[] solids, float x, float z)
        {
            return !float.IsNaN(CellFloor(solids, x, z));
        }

        public static float MeasureMeshGap(Solid[] solids, Ramp[] ramps)
        {
            return ColliderVisualGap(solids, ramps, new StringBuilder());
        }

        /// <summary>
        /// Replay one toy-aware route. Stuck is 1 when the grid has no path.
        /// Pads and zips count when that edge is the route the dummy takes.
        /// </summary>
        public static int WalkLeg(Solid[] solids, float x0, float z0, float x1, float z1, out int pads, out int zips)
        {
            pads = 0;
            zips = 0;
            Nav nav = BuildNav(solids);
            int start = NearestOpen(nav, x0, z0);
            int goal = NearestOpen(nav, x1, z1);
            if (start < 0 || goal < 0) return 1;
            if (start == goal) return 0;
            int n = nav.Floor.Length;
            var parent = new int[n];
            var kind = new byte[n];
            for (int i = 0; i < n; i++) parent[i] = -1;
            float cost = RouteCost(nav, start, goal, true, null, parent, kind);
            if (cost > 80f) return 1;
            int cur = goal;
            int guard = 0;
            while (cur != start && guard++ < n)
            {
                int prev = parent[cur];
                if (prev < 0 || prev == cur) return 1;
                if (kind[cur] == 1) zips++;
                if (kind[cur] == 2) pads++;
                cur = prev;
            }
            return cur == start ? 0 : 1;
        }

        public static void ZipAnchors(Solid[] solids, ZipLineSpot z, out float x0, out float z0, out float x1, out float z1)
        {
            Nav nav = BuildNav(solids);
            int mount = HighestNear(nav, z.Ax, z.Az, z.Ay);
            int exit = NearestOpen(nav, z.Bx, z.Bz);
            if (mount < 0) { x0 = z.Ax; z0 = z.Az; }
            else CellCenter(nav, mount, out x0, out z0);
            if (exit < 0) { x1 = z.Bx; z1 = z.Bz; }
            else CellCenter(nav, exit, out x1, out z1);
        }
    }
}
