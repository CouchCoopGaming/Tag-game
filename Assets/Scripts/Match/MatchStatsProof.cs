using System;
using System.Globalization;
using System.IO;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Practice;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.MatchStats
{
    /// <summary>
    /// Four pawns, the existing opponent brain, and a short round.
    /// Stats, awards, the tie rule, and the highlight ring are checked here.
    /// </summary>
    public static class MatchStatsProof
    {
        public sealed class Report
        {
            public bool Ok = true;
            public string Line = "";
            public string Failure = "";
            public string Awards = "";

            public void Fail(string why)
            {
                Ok = false;
                if (Failure.Length == 0) Failure = why;
            }
        }

        public static Report Run()
        {
            var report = new Report();
            EnemyAi.ResetLoopSearch();
            bool fair = TiesAreFair();
            bool couch = CouchScaleHolds();
            bool palette = PalettesDiffer();
            bool wired = SourcesHold(report);

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            int allocBefore = MatchHighlight.RingAlloc;
            int savedArena = ParkArena.Id;
            bool savedChoice = ParkArena.HasExplicitChoice;
            bool bounds = true;
            for (int arena = 0; arena < ParkArena.Count; arena++)
            {
                ParkArena.Select(arena);
                ParkArena.HasExplicitChoice = true;
                Simulate(cfg, LoopOf(arena));
                ParkArena.Containment(arena, out float mapW, out float mapD, out float killY);
                if (!MatchHighlight.Inside(mapW, mapD, killY))
                    bounds = false;
                if (Sum(MatchBook.TagsMade) < 1 || Sum(MatchBook.WallRuns) < 1 || Sum(MatchBook.Pads) < 1
                    || Sum(MatchBook.Zips) < 1 || Sum(MatchBook.AirDashes) < 1)
                    bounds = false;
            }
            int allocAfter = MatchHighlight.RingAlloc;
            ParkArena.Select(savedArena);
            ParkArena.HasExplicitChoice = savedChoice;

            if (MatchBook.Count != 4)
                report.Fail("roster was not 4");
            if (MatchBook.StatCount != 17)
                report.Fail("stat count drifted");
            if (!fair)
                report.Fail("a tie crowned the first seat");
            if (!couch)
                report.Fail("results text did not follow the hud scale");
            if (!palette)
                report.Fail("a colorblind palette matched the default seat color");
            if (!wired && report.Ok)
                report.Fail("results or the ghost figure missed a wire");

            int tags = Sum(MatchBook.TagsMade);
            int tagged = Sum(MatchBook.TimesTagged);
            int walls = Sum(MatchBook.WallRuns);
            int jumps = Sum(MatchBook.WallJumps);
            int pads = Sum(MatchBook.Pads);
            int zips = Sum(MatchBook.Zips);
            int dashes = Sum(MatchBook.AirDashes);
            int landed = Sum(MatchBook.PunchesLanded);
            int whiff = Sum(MatchBook.PunchesWhiffed);
            int stag = Sum(MatchBook.Staggers);
            int near = Sum(MatchBook.NearMisses);
            int blocked = Sum(MatchBook.TagBacksBlocked);
            if (tags < 1 || tagged < 1) report.Fail("tags were empty");
            if (walls < 1) report.Fail("wall-runs were empty");
            if (jumps < 1) report.Fail("wall-jumps were empty");
            if (pads < 1) report.Fail("pads were empty");
            if (zips < 1) report.Fail("zips were empty");
            if (dashes < 1) report.Fail("air dashes were empty");
            if (landed < 1 || whiff < 1) report.Fail("punches were empty");
            if (stag < 1) report.Fail("staggers were empty");
            if (near < 1) report.Fail("near-misses were empty");
            if (blocked < 1) report.Fail("tag-backs were empty");
            if (SumF(MatchBook.TimeAsIt) <= 0.05f) report.Fail("time as It was empty");
            if (SumF(MatchBook.LongestSurvival) <= 0.05f) report.Fail("survival was empty");
            if (SumF(MatchBook.Distance) <= 1f) report.Fail("distance was empty");
            if (SumF(MatchBook.TopSpeed) <= 1f) report.Fail("top speed was empty");
            if (SumF(MatchBook.AirTime) <= 0.2f) report.Fail("air time was empty");

            bool awards = AwardsHold(report);
            bool highlight = HighlightHolds(report);
            if (allocAfter != allocBefore || allocAfter != 0)
                report.Fail("the ring allocated during the match");
            if (MatchHighlight.Leftovers != 0)
                report.Fail("a ghost figure was left up");
            if (MatchHighlight.HasCollider || MatchHighlight.HasCharacterController || MatchHighlight.HasRigidbody || MatchHighlight.RootMotion)
                report.Fail("the highlight claims a body");
            if (!bounds)
                report.Fail("highlight left an arena");

            int col = MatchHighlight.HasCollider ? 1 : 0;
            report.Line = "match-stats"
                + " players=" + MatchBook.Count.ToString(CultureInfo.InvariantCulture)
                + " stats=" + MatchBook.StatCount.ToString(CultureInfo.InvariantCulture)
                + " awards=" + (awards ? "ok" : "bad")
                + " ties=" + (fair && SlipTieHolds() ? "fair" : "bias")
                + " highlight=" + (highlight ? "ok" : "bad")
                + " ringAlloc=" + allocAfter.ToString(CultureInfo.InvariantCulture)
                + " leftovers=" + MatchHighlight.Leftovers.ToString(CultureInfo.InvariantCulture)
                + " colliders=" + col.ToString(CultureInfo.InvariantCulture)
                + " arenas=3 bounds=" + (bounds ? "ok" : "bad");
            if (!report.Ok)
                report.Line = report.Line + " FAIL " + report.Failure;
            EnemyAi.ResetLoopSearch();
            return report;
        }

        static MegaParkP1Layout.Pt[] LoopOf(int arena)
        {
            if (arena == ParkArena.Pocket) return PocketParkLayout.LoopCcw;
            if (arena == ParkArena.Stack) return StackYardLayout.LoopCcw;
            return MegaParkP1Layout.LoopCcw;
        }

        static void Simulate(MovementConfig cfg, MegaParkP1Layout.Pt[] loop)
        {
            const int n = 4;
            const float dt = 1f / 60f;
            const int frames = 600;
            MatchBook.ResetMatch();
            MatchHighlight.Reset();
            var names = new[] { "P1", "P2", "P3", "P4" };
            MatchBook.Open(names, n);
            MatchBook.SetIt(1);

            var pos = new Vector3[n];
            var vel = new Vector3[n];
            var face = new Vector3[n];
            var mem = new EnemyMemory[n];
            var prevWall = new bool[n];
            var prevJump = new bool[n];
            var prevPad = new bool[n];
            var prevZip = new bool[n];
            var prevDash = new bool[n];
            var xs = new float[n];
            var ys = new float[n];
            var zs = new float[n];
            var yaws = new float[n];
            var poses = new byte[n];
            if (loop == null || loop.Length < 1) loop = MegaParkP1Layout.LoopCcw;
            for (int i = 0; i < n; i++)
            {
                MegaParkP1Layout.Pt p = loop[i % loop.Length];
                pos[i] = new Vector3(p.X, p.Y, p.Z + i * 6f);
                face[i] = new Vector3(0f, 0f, 1f);
            }

            int it = 1;
            var stag = new PunchStagger.Clock();
            for (int f = 0; f < frames; f++)
            {
                PunchStagger.Tick(ref stag, dt);
                for (int i = 0; i < n; i++)
                {
                    EnemyOverlay ov = Brain(i, f, ref mem[i], pos[i], vel[i], face[i], dt);
                    Vector3 wish = ov.Face;
                    wish.y = 0f;
                    if (wish.sqrMagnitude > 0.0001f) wish.Normalize();
                    else wish = face[i];
                    vel[i] = KinematicStep.GroundSteer(vel[i], wish, cfg.sprintSpeed, cfg.groundAccel, cfg.groundDecel, dt, false);
                    pos[i] += new Vector3(vel[i].x, 0f, vel[i].z) * dt;
                    face[i] = wish;
                    bool wall = ov.Verb == EnemyVerb.WallRun;
                    bool jump = ov.Verb == EnemyVerb.WallJump;
                    bool pad = ov.Verb == EnemyVerb.Pad;
                    bool zip = ov.Verb == EnemyVerb.Zip;
                    bool dash = ov.Dash || ov.Verb == EnemyVerb.AirDash;
                    bool air = i == 2 || dash || ov.Jump;
                    float speed = new Vector3(vel[i].x, 0f, vel[i].z).magnitude;
                    xs[i] = pos[i].x;
                    ys[i] = pos[i].y;
                    zs[i] = pos[i].z;
                    yaws[i] = (float)Math.Atan2(wish.x, wish.z) * Mathf.Rad2Deg;
                    poses[i] = PoseOf(wall, dash, zip, pad, air);
                    MatchBook.NoteMotion(i, xs[i], ys[i], zs[i], speed, dt, i == it, air, wall && !prevWall[i], jump && !prevJump[i], pad && !prevPad[i], zip && !prevZip[i], dash && !prevDash[i]);
                    prevWall[i] = wall;
                    prevJump[i] = jump;
                    prevPad[i] = pad;
                    prevZip[i] = zip;
                    prevDash[i] = dash;
                }

                if (it != 0 && PunchStagger.IsStaggerHit(true, PunchStagger.IsTag(true, false, true, true)) && PunchStagger.TryStart(ref stag))
                {
                    MatchBook.NoteLanded(0);
                    MatchBook.NoteStagger(0);
                }

                if (TagAt(f, out int from, out int to))
                    poses[from] = PracticeVerb.Punch;
                MatchHighlight.Offer(dt, n, xs, ys, zs, yaws, poses);
                if (TagAt(f, out from, out to))
                {
                    MatchBook.NoteLanded(from);
                    MatchBook.NoteTag(from, to);
                    MatchBook.NoteBlocked(to);
                    it = to;
                }
                if (f == 120 || f == 210 || f == 300 || f == 390)
                {
                    MatchBook.NoteNear(0, 1.20f);
                    MatchBook.NoteNear(3, 1.05f);
                }
                if (f == 150)
                    MatchBook.NoteNear(1, 1.40f);
                if (f == 48 || f == 51)
                    MatchBook.NoteWhiff(1);
            }
            xs[0] = -30f;
            ys[0] = -8f;
            zs[0] = 400f;
            MatchHighlight.Offer(1f, n, xs, ys, zs, yaws, poses);
            MatchBook.Seal();
        }

        static bool TagAt(int frame, out int from, out int to)
        {
            from = 0;
            to = 0;
            if (frame == 90) { from = 1; to = 0; return true; }
            if (frame == 180) { from = 0; to = 1; return true; }
            if (frame == 258) { from = 1; to = 2; return true; }
            if (frame == 342) { from = 2; to = 1; return true; }
            if (frame == 558) { from = 1; to = 3; return true; }
            return false;
        }

        static EnemyOverlay Brain(int i, int frame, ref EnemyMemory mem, Vector3 pos, Vector3 vel, Vector3 face, float dt)
        {
            if (i == 0 && frame >= 240 && frame < 400)
                return Wall(ref mem, frame, pos, vel, face, dt);
            if (i == 1 && frame < 90)
                return Toy(ref mem, pos, vel, face, dt, true, false);
            if (i == 2 && frame >= 90 && frame < 180)
                return Toy(ref mem, pos, vel, face, dt, false, true);
            if (i == 3 && frame >= 180 && frame < 270)
                return Dash(ref mem, pos, vel, face, dt);
            return Cruise(ref mem, pos, vel, face, dt);
        }

        static EnemyOverlay Cruise(ref EnemyMemory mem, Vector3 pos, Vector3 vel, Vector3 face, float dt)
        {
            EnemySense sense = Base(pos, vel, face, dt);
            sense.MegaPark = true;
            sense.Grounded = true;
            return EnemyAi.Evade(ref mem, sense);
        }

        static EnemyOverlay Toy(ref EnemyMemory mem, Vector3 pos, Vector3 vel, Vector3 face, float dt, bool pad, bool zip)
        {
            EnemySense sense = Base(pos, vel, face, dt);
            sense.Grounded = true;
            sense.ThreatClosing = true;
            sense.MegaPark = true;
            if (pad)
            {
                sense.PadAhead = true;
                sense.PadHelps = true;
                sense.PadAim = new Vector3(0f, 0f, 1f);
            }
            if (zip)
            {
                sense.ZipAhead = true;
                sense.ZipHelps = true;
                sense.ZipAim = new Vector3(1f, 0f, 0f);
            }
            return EnemyAi.Evade(ref mem, sense);
        }

        static EnemyOverlay Dash(ref EnemyMemory mem, Vector3 pos, Vector3 vel, Vector3 face, float dt)
        {
            EnemySense sense = Base(pos, vel, face, dt);
            sense.Airborne = true;
            sense.Grounded = false;
            sense.AirDashReady = true;
            sense.ThreatClosing = true;
            sense.ForceVerbs = true;
            sense.MegaPark = true;
            return EnemyAi.Evade(ref mem, sense);
        }

        static EnemyOverlay Wall(ref EnemyMemory mem, int frame, Vector3 pos, Vector3 vel, Vector3 face, float dt)
        {
            var wish = new OpponentChaseWish();
            wish.Face = new Vector3(0f, 0f, 1f);
            wish.MoveY = 1f;
            wish.Sprint = true;
            wish.Verb = OpponentChaseVerb.WallCling;
            EnemySense sense = Base(pos, vel, face, dt);
            sense.OnWall = true;
            sense.SameWallClosed = false;
            sense.WallNormal = new Vector3(1f, 0f, 0f);
            sense.WallTime = frame >= 300 ? 0.40f : 0.10f;
            sense.TargetHeightDelta = frame >= 300 ? 2f : 0f;
            sense.ForceVerbs = true;
            sense.Grounded = false;
            sense.Airborne = true;
            return EnemyAi.Decorate(ref mem, sense, wish);
        }

        static EnemySense Base(Vector3 pos, Vector3 vel, Vector3 face, float dt)
        {
            EnemySense sense = default;
            sense.Difficulty = EnemyAi.DefaultDifficulty;
            sense.Dt = dt;
            sense.SelfPos = pos;
            sense.SelfVel = vel;
            sense.Forward = face;
            sense.PlanarSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
            sense.PunchReach = MatchBook.PunchReach;
            return sense;
        }

        static byte PoseOf(bool wall, bool dash, bool zip, bool pad, bool air)
        {
            if (wall) return PracticeVerb.WallRun;
            if (dash) return PracticeVerb.AirDash;
            if (zip) return PracticeVerb.Zip;
            if (pad) return PracticeVerb.Pad;
            if (air) return PracticeVerb.Jump;
            return PracticeVerb.None;
        }

        static bool TiesAreFair()
        {
            var tie = new[] { 4f, 1f, 4f, 0f };
            var late = new[] { 1f, 0f, 0f, 9f };
            var all = new[] { 2f, 2f, 2f, 2f };
            int shared = MatchBook.Leaders(tie, 4);
            int solo = MatchBook.Leaders(late, 4);
            int everyone = MatchBook.Leaders(all, 4);
            if (shared != ((1 << 0) | (1 << 2))) return false;
            if (solo != (1 << 3)) return false;
            if (everyone != 15) return false;
            if (MatchBook.Leaders(new[] { 0f, 0f, 0f, 0f }, 4) != 0) return false;
            return true;
        }

        static bool SlipTieHolds()
        {
            if (!MatchBook.Sealed || MatchBook.AwardCount < 2) return false;
            for (int i = 0; i < MatchBook.AwardCount; i++)
            {
                string line = MatchBook.AwardLine[i] ?? "";
                if (line.IndexOf(MatchBook.Slipperiest, StringComparison.Ordinal) < 0) continue;
                int mask = MatchBook.AwardMask[i];
                bool both = (mask & 1) != 0 && (mask & (1 << 3)) != 0;
                bool names = line.IndexOf(MatchBook.Name[0], StringComparison.Ordinal) >= 0
                    && line.IndexOf(MatchBook.Name[3], StringComparison.Ordinal) >= 0;
                string onlyFirst = MatchBook.Slipperiest + "  " + AccessibilityPalette.Glyph(0) + " " + MatchBook.Name[0];
                return both && names && line != onlyFirst;
            }
            return false;
        }

        static bool AwardsHold(Report report)
        {
            if (MatchBook.AwardCount < 2 || MatchBook.AwardCount > 3)
            {
                report.Fail("award count was " + MatchBook.AwardCount.ToString(CultureInfo.InvariantCulture));
                return false;
            }
            report.Awards = "";
            for (int i = 0; i < MatchBook.AwardCount; i++)
            {
                if (i > 0) report.Awards += ", ";
                report.Awards += MatchBook.AwardLine[i] ?? "";
                int mask = MatchBook.AwardMask[i];
                if (mask == 0)
                {
                    report.Fail("an award had no seat");
                    return false;
                }
            }
            string joined = report.Awards;
            if (joined.IndexOf(MatchBook.HotPotato, StringComparison.Ordinal) < 0
                || joined.IndexOf(MatchBook.Slipperiest, StringComparison.Ordinal) < 0
                || joined.IndexOf(MatchBook.SkyWalker, StringComparison.Ordinal) < 0)
            {
                report.Fail("awards missed the story set");
                return false;
            }
            if (MatchBook.TagsMade[1] <= MatchBook.TagsMade[0])
            {
                report.Fail("hot potato fell on the first seat");
                return false;
            }
            return true;
        }

        static bool HighlightHolds(Report report)
        {
            if (!MatchHighlight.FromTag || MatchHighlight.SnapCount < 2 || MatchHighlight.Filled != MatchHighlight.Samples)
            {
                report.Fail("highlight window was empty");
                return false;
            }
            if (!MatchHighlight.Playing)
            {
                report.Fail("highlight did not start");
                return false;
            }
            if (!MatchHighlight.At(1, 0f, out float x, out float y, out float z, out float yaw, out byte pose))
            {
                report.Fail("highlight missed the open");
                return false;
            }
            MatchHighlight.At(1, 0f, out float x2, out float y2, out float z2, out float yaw2, out byte pose2);
            if (x != x2 || y != y2 || z != z2 || yaw != yaw2 || pose != pose2)
            {
                report.Fail("highlight replay drifted");
                return false;
            }
            float end = MatchHighlight.Length();
            MatchHighlight.At(1, end, out _, out _, out _, out _, out byte endPose);
            if (endPose != PracticeVerb.Punch)
            {
                report.Fail("the final tag did not keep the punch pose");
                return false;
            }
            float weight = PracticePose.Weight(endPose, 0.08f, 0f, 0f);
            if (float.IsNaN(weight))
            {
                report.Fail("ghost pose weight was not a number");
                return false;
            }
            MatchHighlight.Skip();
            if (MatchHighlight.Playing || MatchHighlight.Leftovers != 0)
            {
                report.Fail("skip left the highlight up");
                return false;
            }
            return true;
        }

        static bool CouchScaleHolds()
        {
            int small = MatchBook.CouchFont(32, 0.75f);
            int mid = MatchBook.CouchFont(32, 1f);
            int big = MatchBook.CouchFont(32, 1.5f);
            return small >= 24 && big > mid && mid > small;
        }

        static bool PalettesDiffer()
        {
            AccessibilityPalette.Player(AccessibilityPalette.Default, 0, out float r0, out float g0, out float b0);
            AccessibilityPalette.Player(AccessibilityPalette.Deuteranopia, 0, out float r1, out float g1, out float b1);
            AccessibilityPalette.Player(AccessibilityPalette.HighContrast, 1, out float r2, out float g2, out float b2);
            bool seat = r0 != r1 || g0 != g1 || b0 != b1;
            bool other = r2 != r0 || g2 != g0 || b2 != b0;
            return seat && other;
        }

        static bool SourcesHold(Report report)
        {
            string ghost = Read("Assets/Scripts/Match/MatchGhostView.cs");
            string results = Read("Assets/Scripts/Match/MatchResults.cs");
            string live = Read("Assets/Scripts/Modes/TagModeController.cs");
            string punch = Read("Assets/Scripts/Tag/PunchHitbox.cs");
            if (ghost == null || results == null || live == null || punch == null)
            {
                report.Fail("match files were missing");
                return false;
            }
            if (ghost.IndexOf("CharacterController", StringComparison.Ordinal) >= 0
                || ghost.IndexOf("Rigidbody", StringComparison.Ordinal) >= 0
                || ghost.IndexOf("Collider", StringComparison.Ordinal) >= 0
                || ghost.IndexOf(".Move(", StringComparison.Ordinal) >= 0)
            {
                report.Fail("ghost figure has a body or a move");
                return false;
            }
            if (ghost.IndexOf("PracticePose.Weight", StringComparison.Ordinal) < 0)
            {
                report.Fail("ghost figure skipped the pose weights");
                return false;
            }
            if (results.IndexOf("PaletteOf", StringComparison.Ordinal) < 0
                || results.IndexOf("CouchFont", StringComparison.Ordinal) < 0)
            {
                report.Fail("stat cards skipped palette or text scale");
                return false;
            }
            if (live.IndexOf("HudScale", StringComparison.Ordinal) < 0
                || live.IndexOf("MatchResults.Paint", StringComparison.Ordinal) < 0
                || live.IndexOf("MatchHighlight.Skip", StringComparison.Ordinal) < 0
                || live.IndexOf("MatchLive.Sample", StringComparison.Ordinal) < 0)
            {
                report.Fail("results did not host the cards or the skip");
                return false;
            }
            if (punch.IndexOf("NoteWhiff", StringComparison.Ordinal) < 0
                || punch.IndexOf("NoteBlocked", StringComparison.Ordinal) < 0
                || punch.IndexOf("NoteStagger", StringComparison.Ordinal) < 0)
            {
                report.Fail("punch notes were not wired");
                return false;
            }
            return true;
        }

        static int Sum(int[] values)
        {
            int n = 0;
            if (values == null) return 0;
            int c = MatchBook.Count;
            for (int i = 0; i < c && i < values.Length; i++)
                n += values[i];
            return n;
        }

        static float SumF(float[] values)
        {
            float n = 0f;
            if (values == null) return 0f;
            int c = MatchBook.Count;
            for (int i = 0; i < c && i < values.Length; i++)
                n += values[i];
            return n;
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
