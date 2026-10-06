using System;
using Tag.Art;
using Tag.Level;
using Tag.Modes;
using Tag.Onboard;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Core
{
    /// <summary>
    /// Headless 4-player round on the Mega Park graybox.
    /// 120 seconds at 60 Hz. One human stand-in and three AI.
    /// Cost is op weights shared with the in-game meter. Worst frame versus median
    /// is the spike when all three AI refresh a decision and a path search together.
    /// </summary>
    public static class FrameBudget
    {
        public const float Seconds = 120f;
        public const int Hz = 60;
        public const int Players = 4;
        public const int AiCount = 3;

        public struct Report
        {
            public bool Ok;
            public int Frames;
            public int Median;
            public int Worst;
            public int Move;
            public int Ai;
            public int Pose;
            public int Hud;
            public int Audio;
            public int Round;
            public int Decides;
            public int LoopSearches;
            public string Line;
            public string Failure;

            public void Fail(string why)
            {
                Ok = false;
                if (string.IsNullOrEmpty(Failure)) Failure = why;
            }
        }

        public static Report Run()
        {
            var report = new Report { Ok = true };
            if (!CacheHolds())
                report.Fail("loop search cache changed a point");

            int frames = (int)(Seconds * Hz);
            report.Frames = frames;
            if (frames != 7200)
                report.Fail("frame count is not 120s at 60 Hz");

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (ActionBinds.Current == null)
                ActionBinds.Current = ActionBinds.Defaults();

            var pos = new Vector3[Players];
            var vel = new Vector3[Players];
            var face = new Vector3[Players];
            var mem = new EnemyMemory[Players];
            var wp = new int[Players];
            var since = new float[Players];
            var anchor = new Vector3[Players];
            MegaParkP1Layout.Pt[] loop = MegaParkP1Layout.LoopCcw;
            for (int i = 0; i < Players; i++)
            {
                MegaParkP1Layout.Pt p = loop[i % loop.Length];
                pos[i] = new Vector3(p.X, p.Y, p.Z + i * 1.5f);
                face[i] = new Vector3(0f, 0f, 1f);
                anchor[i] = pos[i];
                wp[i] = i;
            }

            float dt = 1f / Hz;
            float period = 1f / EnemyAi.DecisionHz;
            float moveLim = EnemyAi.PathMoveRefresh;
            float moveLimSq = moveLim * moveLim;
            var costs = new int[frames];
            int moveSteps = 0;
            int decides = 0;
            int moveOps = 0;
            int aiOps = 0;
            int poseOps = 0;
            int hudOps = 0;
            int audioOps = 0;
            int roundOps = 0;
            float sink = 0f;
            float remain = Seconds;

            EnemyAi.ResetLoopSearch();
            for (int f = 0; f < frames; f++)
            {
                int ops = 0;
                for (int i = 0; i < Players; i++)
                {
                    Vector3 delta = pos[i] - anchor[i];
                    delta.y = 0f;
                    since[i] += dt;
                    bool refresh = i > 0 && (since[i] >= period || delta.sqrMagnitude >= moveLimSq);
                    if (refresh)
                    {
                        decides++;
                        since[i] = 0f;
                        anchor[i] = pos[i];
                        if (i == 1)
                        {
                            Vector3 aim = EnemyAi.DelayedAim(ref mem[i], EnemyAi.DefaultDifficulty, dt, pos[i], pos[0], vel[0], 1, 0f);
                            EnemyAi.LoopOffset(pos[i].x, pos[i].z, 8f, out float ox, out float oz);
                            Vector3 toHuman = pos[0] - pos[i];
                            toHuman.y = 0f;
                            face[i] = toHuman.sqrMagnitude > 0.01f ? toHuman : aim - pos[i];
                            sink += ox + oz;
                        }
                        else
                        {
                            EnemySense sense = default;
                            sense.Difficulty = EnemyAi.DefaultDifficulty;
                            sense.Dt = dt;
                            sense.IsIt = false;
                            sense.SelfPos = pos[i];
                            sense.SelfVel = vel[i];
                            sense.Forward = face[i];
                            sense.Grounded = true;
                            sense.MegaPark = true;
                            sense.HasTarget = true;
                            sense.TargetPos = pos[1];
                            sense.TargetId = 1;
                            sense.PunchReach = 1.55f;
                            Vector3 away = pos[i] - pos[1];
                            away.y = 0f;
                            sense.PlanarDistance = away.magnitude;
                            Vector3 point = EnemyAi.LoopPoint(true, wp[i]);
                            sense.LoopAim = point - pos[i];
                            EnemyOverlay ev = EnemyAi.Evade(ref mem[i], sense);
                            EnemyAi.LoopOffset(pos[i].x, pos[i].z, i == 2 ? 10f : -10f, out float ox, out float oz);
                            face[i] = ev.Face.sqrMagnitude > 0.001f ? ev.Face : sense.LoopAim;
                            wp[i] = ev.Waypoint;
                            sink += ox + oz;
                        }
                        int heavy = FrameMeter.AiDecideOps + FrameMeter.AiPathOps + FrameMeter.AiLoopOps;
                        ops += heavy;
                        aiOps += heavy;
                    }
                    else if (i > 0)
                    {
                        ops += FrameMeter.AiTickOps;
                        aiOps += FrameMeter.AiTickOps;
                    }

                    Vector3 wish;
                    if (i == 0)
                    {
                        Vector3 mark = EnemyAi.LoopPoint(true, wp[0]);
                        wish = mark - pos[0];
                        wish.y = 0f;
                        if (wish.sqrMagnitude < 4f) wp[0]++;
                        face[0] = wish;
                    }
                    else
                    {
                        wish = face[i];
                        wish.y = 0f;
                    }
                    if (wish.sqrMagnitude > 0.0001f) wish.Normalize();
                    vel[i] = KinematicStep.GroundSteer(vel[i], wish, cfg.sprintSpeed, cfg.groundAccel, cfg.groundDecel, dt, false);
                    pos[i] += new Vector3(vel[i].x, 0f, vel[i].z) * dt;
                    moveSteps++;
                    ops += FrameMeter.MoveOps;
                    moveOps += FrameMeter.MoveOps;

                    float speed = new Vector3(vel[i].x, 0f, vel[i].z).magnitude;
                    sink += IdlePose.Weight(speed, 0f, 0f, 0f);
                    sink += PunchStaggerPose.Weight(0f);
                    ops += FrameMeter.PoseOps;
                    poseOps += FrameMeter.PoseOps;

                    if (float.IsNaN(pos[i].x) || float.IsNaN(pos[i].z))
                        report.Fail("a pawn left the number line");
                }

                PromptText.Ensure();
                string glyph = ControlGlyphs.Glyph(PlayAction.Jump);
                string digits = HudDigits.Whole0(new Vector3(vel[0].x, 0f, vel[0].z).magnitude);
                if (glyph == null || digits == null)
                    report.Fail("hud read returned null");
                ops += FrameMeter.HudOps;
                hudOps += FrameMeter.HudOps;
                ops += FrameMeter.AudioOps;
                audioOps += FrameMeter.AudioOps;
                remain -= dt;
                if (remain < 0f) remain = 0f;
                ops += FrameMeter.RoundOps;
                roundOps += FrameMeter.RoundOps;
                costs[f] = ops;
            }

            if (float.IsNaN(sink))
                report.Fail("pose solve was not a number");
            if (moveSteps != frames * Players)
                report.Fail("a pawn missed its one move");

            int worst = 0;
            for (int i = 0; i < costs.Length; i++)
                if (costs[i] > worst) worst = costs[i];
            int[] sorted = (int[])costs.Clone();
            Array.Sort(sorted);
            int median = sorted[frames / 2];
            report.Median = median;
            report.Worst = worst;
            report.Move = moveOps;
            report.Ai = aiOps;
            report.Pose = poseOps;
            report.Hud = hudOps;
            report.Audio = audioOps;
            report.Round = roundOps;
            report.Decides = decides;
            report.LoopSearches = EnemyAi.LoopProjectMisses;

            if (median <= 0)
                report.Fail("median frame was empty");
            if (worst > FrameMeter.BudgetOps)
                report.Fail("worst frame passed the budget");
            if (worst * 100 > median * 250)
                report.Fail("worst frame spiked past 2.5x the median");
            if (decides >= frames * AiCount)
                report.Fail("ai re-decided every frame");
            if (report.LoopSearches <= 0 || report.LoopSearches > decides)
                report.Fail("loop searches were not one cached lookup per decide");
            int headroom = FrameMeter.BudgetOps - worst;
            if (headroom <= 0)
                report.Fail("no headroom left for the four pawns");

            int hundredths = median > 0 ? worst * 100 / median : 0;
            string ratio = (hundredths / 100).ToString()
                + "."
                + (hundredths % 100 < 10 ? "0" : "")
                + (hundredths % 100).ToString();
            report.Line = "frame-budget seconds=120 hz=60 frames=" + frames.ToString()
                + " players=4 ai=3 map=mega-park"
                + " median=" + median.ToString()
                + " worst=" + worst.ToString()
                + " ratio=" + ratio
                + " move=" + moveOps.ToString()
                + " ai=" + aiOps.ToString()
                + " pose=" + poseOps.ToString()
                + " hud=" + hudOps.ToString()
                + " audio=" + audioOps.ToString()
                + " round=" + roundOps.ToString()
                + " decides=" + decides.ToString()
                + " loopSearches=" + report.LoopSearches.ToString()
                + " budget=" + FrameMeter.BudgetOps.ToString()
                + " headroom=" + headroom.ToString()
                + (report.Ok ? " steady=ok" : " steady=FAIL");
            return report;
        }

        static bool CacheHolds()
        {
            EnemyAi.ResetLoopSearch();
            EnemyAi.LoopOffset(12f, 18f, 6f, out float x, out float z);
            int misses = EnemyAi.LoopProjectMisses;
            EnemyAi.LoopOffset(12f, 18f, 6f, out float x2, out float z2);
            bool held = x == x2 && z == z2
                && EnemyAi.LoopProjectQueries == 2
                && EnemyAi.LoopProjectMisses == misses;
            Vector3 a = EnemyAi.LoopPoint(true, 3);
            Vector3 b = EnemyAi.LoopPoint(true, 3);
            EnemyAi.ResetLoopSearch();
            return held && a.x == b.x && a.y == b.y && a.z == b.z;
        }

        /// <summary>
        /// Four human pawns, four chase cameras, no AI. Same 120s at 60 Hz.
        /// Each pawn still takes one kinematic step. Cameras keep fovPop, shake, and slowMo at 0.
        /// </summary>
        public static Report RunSplit()
        {
            var report = new Report { Ok = true };
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f)
                report.Fail("chase cam locks moved");

            const int humans = 4;
            const int cameras = 4;
            const int camOps = 4;
            int frames = (int)(Seconds * Hz);
            report.Frames = frames;
            if (frames != 7200)
                report.Fail("frame count is not 120s at 60 Hz");

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            var pos = new Vector3[humans];
            var vel = new Vector3[humans];
            var wp = new int[humans];
            MegaParkP1Layout.Pt[] loop = MegaParkP1Layout.LoopCcw;
            for (int i = 0; i < humans; i++)
            {
                MegaParkP1Layout.Pt p = loop[i % loop.Length];
                pos[i] = new Vector3(p.X, p.Y, p.Z + i * 1.5f);
                wp[i] = i;
            }

            float dt = 1f / Hz;
            var costs = new int[frames];
            int moveSteps = 0;
            int moveOps = 0;
            int poseOps = 0;
            int hudOps = 0;
            int audioOps = 0;
            int roundOps = 0;
            int camTotal = 0;
            float sink = 0f;
            float remain = Seconds;

            for (int f = 0; f < frames; f++)
            {
                int ops = 0;
                for (int i = 0; i < humans; i++)
                {
                    Vector3 mark = EnemyAi.LoopPoint(true, wp[i]);
                    Vector3 wish = mark - pos[i];
                    wish.y = 0f;
                    if (wish.sqrMagnitude < 4f) wp[i]++;
                    if (wish.sqrMagnitude > 0.0001f) wish.Normalize();
                    vel[i] = KinematicStep.GroundSteer(vel[i], wish, cfg.sprintSpeed, cfg.groundAccel, cfg.groundDecel, dt, false);
                    pos[i] += new Vector3(vel[i].x, 0f, vel[i].z) * dt;
                    moveSteps++;
                    ops += FrameMeter.MoveOps;
                    moveOps += FrameMeter.MoveOps;

                    float speed = new Vector3(vel[i].x, 0f, vel[i].z).magnitude;
                    sink += IdlePose.Weight(speed, 0f, 0f, 0f);
                    sink += PunchStaggerPose.Weight(0f);
                    ops += FrameMeter.PoseOps;
                    poseOps += FrameMeter.PoseOps;

                    ops += FrameMeter.HudOps;
                    hudOps += FrameMeter.HudOps;
                    ops += camOps;
                    camTotal += camOps;

                    if (float.IsNaN(pos[i].x) || float.IsNaN(pos[i].z))
                        report.Fail("a pawn left the number line");
                    sink += ChaseCam.AheadRateFor(0f) + ChaseCam.FovPop + ChaseCam.Shake + ChaseCam.SlowMo;
                }

                remain -= dt;
                if (remain < 0f) remain = 0f;
                ops += FrameMeter.AudioOps;
                audioOps += FrameMeter.AudioOps;
                ops += FrameMeter.RoundOps;
                roundOps += FrameMeter.RoundOps;
                costs[f] = ops;
            }

            if (float.IsNaN(sink))
                report.Fail("pose solve was not a number");
            if (moveSteps != frames * humans)
                report.Fail("a pawn missed its one move");

            int worst = 0;
            for (int i = 0; i < costs.Length; i++)
                if (costs[i] > worst) worst = costs[i];
            int[] sorted = (int[])costs.Clone();
            Array.Sort(sorted);
            int median = sorted[frames / 2];
            report.Median = median;
            report.Worst = worst;
            report.Move = moveOps;
            report.Ai = 0;
            report.Pose = poseOps;
            report.Hud = hudOps;
            report.Audio = audioOps;
            report.Round = roundOps;
            int headroom = FrameMeter.BudgetOps - worst;
            if (median <= 0)
                report.Fail("median frame was empty");
            if (worst > FrameMeter.BudgetOps)
                report.Fail("worst frame passed the budget");
            if (headroom <= 0)
                report.Fail("no headroom left for four cameras");
            if (worst * 100 > median * 250)
                report.Fail("worst frame spiked past 2.5x the median");

            int hundredths = median > 0 ? worst * 100 / median : 0;
            string ratio = (hundredths / 100).ToString()
                + "."
                + (hundredths % 100 < 10 ? "0" : "")
                + (hundredths % 100).ToString();
            report.Line = "frame-budget-split seconds=120 hz=60 frames=" + frames.ToString()
                + " players=4 humans=4 ai=0 cameras=" + cameras.ToString()
                + " map=mega-park"
                + " median=" + median.ToString()
                + " worst=" + worst.ToString()
                + " ratio=" + ratio
                + " move=" + moveOps.ToString()
                + " ai=0"
                + " pose=" + poseOps.ToString()
                + " hud=" + hudOps.ToString()
                + " audio=" + audioOps.ToString()
                + " round=" + roundOps.ToString()
                + " cam=" + camTotal.ToString()
                + " budget=" + FrameMeter.BudgetOps.ToString()
                + " headroom=" + headroom.ToString()
                + (report.Ok ? " steady=ok" : " steady=FAIL");
            return report;
        }
    }
}
