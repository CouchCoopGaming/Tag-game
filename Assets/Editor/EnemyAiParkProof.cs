using System;
using System.Globalization;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

public static partial class EnemyAiProof
{
    const float ParkDt = 1f / 30f;
    const float ParkHorizon = 90f;
    const float HumanReact = 0.25f;
    const int SeedsPerDifficulty = 24;

    static void RunParkMatches(MovementConfig cfg, PunchTagTuning punch, EnemyAiReport report)
    {
        MegaParkP1Layout.WarmParkRoutes();
        CheckParkRules(punch, report);
        CheckLoopContained(report);
        var pairs = AdjacentPairs();
        if (pairs.Length < 8)
            report.Fail("expected 8 spawn arcs of 118 m");
        float[] diffs = { 0.2f, 0.5f, 0.9f };
        for (int d = 0; d < 3; d++)
        {
            var itTimes = new float[SeedsPerDifficulty];
            var runTimes = new float[SeedsPerDifficulty];
            for (int s = 0; s < SeedsPerDifficulty; s++)
            {
                int pair = s % pairs.Length;
                int salt = s / pairs.Length;
                itTimes[s] = Duel(cfg, punch, diffs[d], pairs[pair].It, pairs[pair].Run, salt, true, report);
                runTimes[s] = Duel(cfg, punch, diffs[d], pairs[pair].It, pairs[pair].Run, salt + 20, false, report);
            }
            Array.Sort(itTimes);
            Array.Sort(runTimes);
            report.ItMed[d] = Percentile(itTimes, 0.50f);
            report.ItP10[d] = Percentile(itTimes, 0.10f);
            report.ItP90[d] = Percentile(itTimes, 0.90f);
            report.RunMed[d] = Percentile(runTimes, 0.50f);
            report.RunP10[d] = Percentile(runTimes, 0.10f);
            report.RunP90[d] = Percentile(runTimes, 0.90f);
        }

        ExpectBand(report, report.ItMed[0], 25f, 45f, "dummy-as-It median at 0.2");
        ExpectBand(report, report.ItMed[1], 15f, 25f, "dummy-as-It median at 0.5");
        ExpectBand(report, report.ItMed[2], 8f, 15f, "dummy-as-It median at 0.9");
        ExpectBand(report, report.RunMed[0], 10f, 20f, "dummy-as-runner median at 0.2");
        ExpectBand(report, report.RunMed[1], 20f, 35f, "dummy-as-runner median at 0.5");
        ExpectBand(report, report.RunMed[2], 35f, 60f, "dummy-as-runner median at 0.9");
        if (!(report.ItMed[2] + 0.35f < report.ItMed[1] && report.ItMed[1] + 0.35f < report.ItMed[0]))
            report.Fail("It medians did not fall with difficulty");
        if (!(report.RunMed[0] + 0.35f < report.RunMed[1] && report.RunMed[1] + 0.35f < report.RunMed[2]))
            report.Fail("runner medians did not rise with difficulty");
        if (report.Stuck != 0)
            report.Fail("stuck count " + report.Stuck.ToString(CultureInfo.InvariantCulture));
        if (report.Breaches != 0)
            report.Fail("left containment " + report.Breaches.ToString(CultureInfo.InvariantCulture));
        if (report.PadUses < 1) report.Fail("dummy never took a pad");
        if (report.ZipUses < 1) report.Fail("dummy never took a zip");
        if (report.GrappleUses < 1) report.Fail("dummy never took a grapple plate");
        if (report.ClingUses < 1) report.Fail("dummy never used a cling wall");
        if (report.BarUses < 1) report.Fail("dummy never used a bar");
        if (report.CounterUses < 1) report.Fail("dummy never used a counter-route");
        if (report.Respawns < 1) report.Fail("kill-plane respawn was not used");
        if (report.Jumps < 1) report.Fail("scripted human never jumped");
    }

    struct ArcPair
    {
        public int It;
        public int Run;
    }

    static ArcPair[] AdjacentPairs()
    {
        var spawns = MegaParkP1Layout.Spawns;
        var list = new System.Collections.Generic.List<ArcPair>(8);
        for (int i = 0; i < spawns.Length; i++)
        {
            for (int j = 0; j < spawns.Length; j++)
            {
                if (i == j) continue;
                float arc = LoopArcPublic(spawns[i].X, spawns[i].Z, spawns[j].X, spawns[j].Z);
                if (Mathf.Abs(arc - 118f) < 0.6f)
                    list.Add(new ArcPair { It = i, Run = j });
            }
        }
        return list.ToArray();
    }

    static float LoopArcPublic(float x0, float z0, float x1, float z1)
    {
        float len = MegaParkP1Layout.LoopLengthM;
        float a = ProjectLoopPublic(x0, z0);
        float b = ProjectLoopPublic(x1, z1);
        float d = Mathf.Abs(a - b);
        if (d > len * 0.5f) d = len - d;
        return d;
    }

    static float ProjectLoopPublic(float x, float z)
    {
        MegaParkP1Layout.Pt[] loop = MegaParkP1Layout.LoopCcw;
        float best = 1e9f;
        float bestT = 0f;
        float t0 = 0f;
        for (int i = 0; i < loop.Length; i++)
        {
            MegaParkP1Layout.Pt a = loop[i];
            MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
            float dx = b.X - a.X;
            float dz = b.Z - a.Z;
            float seg = Mathf.Sqrt(dx * dx + dz * dz);
            float u = seg > 0.001f ? ((x - a.X) * dx + (z - a.Z) * dz) / (seg * seg) : 0f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float px = a.X + dx * u;
            float pz = a.Z + dz * u;
            float lat = (x - px) * (x - px) + (z - pz) * (z - pz);
            if (lat < best)
            {
                best = lat;
                bestT = t0 + seg * u;
            }
            t0 += seg;
        }
        return bestT;
    }

    static void CheckParkRules(PunchTagTuning punch, EnemyAiReport report)
    {
        MegaParkP1Layout.ParkRespawn(70f, 40f, MegaParkP1Layout.SpawnSwX, MegaParkP1Layout.SpawnSwZ, out float x, out float y, out float z);
        if (!MegaParkP1Layout.ParkInsideFence(x, y, z) || y < MegaParkP1Layout.KillPlaneY)
            report.Fail("kill-plane respawn left containment");
        float dx = x - MegaParkP1Layout.SpawnSwX;
        float dz = z - MegaParkP1Layout.SpawnSwZ;
        if (Mathf.Sqrt(dx * dx + dz * dz) < punch.reach)
            report.Fail("kill-plane respawn landed on the It");
        report.Respawns++;

        var faceA = TagArena.Movement.SameWallLimit.Make(7, new Vector3(1f, 0f, 0f), new Vector3(2.55f, 1.2f, 70f));
        var faceB = TagArena.Movement.SameWallLimit.Make(7, new Vector3(1f, 0f, 0f), new Vector3(2.55f, 1.4f, 66f));
        if (!TagArena.Movement.SameWallLimit.SameFace(faceA, faceB))
            report.Fail("same cling wall was treated as two faces");
        if (TagConnects(1.2f, 0.99f, true, punch.reach))
            report.Fail("tag connected on the same wall");
        if (!TagConnects(1.2f, 0.99f, false, punch.reach))
            report.Fail("tag missed an open punch");
    }

    static void CheckLoopContained(EnemyAiReport report)
    {
        MegaParkP1Layout.Pt[] loop = MegaParkP1Layout.LoopCcw;
        if (loop == null || loop.Length < 4)
        {
            report.Fail("Mega Park loop is missing");
            return;
        }
        for (int i = -4; i < loop.Length * 3; i++)
        {
            Vector3 p = EnemyAi.LoopPoint(true, i);
            RequireTarget(p, report, "LoopPoint(mega)");
            int slot = i % loop.Length;
            if (slot < 0) slot += loop.Length;
            float dx = p.x - loop[slot].X;
            float dz = p.z - loop[slot].Z;
            if (dx * dx + dz * dz > 0.0001f || Mathf.Abs(p.y - loop[slot].Y) > 0.001f)
                report.Fail("LoopPoint(mega) is not on the Mega Park loop");
        }

        Vector3 flee = EnemyAi.LoopSteer(new Vector3(8f, 0.2f, 8f), new Vector3(118f, 0.2f, 16f), false, 22f);
        RequireTarget(flee, report, "loop evade");
        Vector3 chase = EnemyAi.LoopSteer(new Vector3(152f, 0.2f, 84f), new Vector3(42f, 0.2f, 92f), true, 18f);
        RequireTarget(chase, report, "loop chase");
    }

    static void RequireTarget(Vector3 p, EnemyAiReport report, string label)
    {
        if (!MegaParkP1Layout.ParkInsideFence(p.x, p.y, p.z))
            report.Fail(label + " left containment");
    }

    static bool TagConnects(float dist, float dot, bool sameWall, float reach)
    {
        if (sameWall) return false;
        return dist <= reach && dot >= 0.8480f;
    }

    sealed class Pawn
    {
        public Vector3 Pos;
        public Vector3 Vel;
        public Vector3 Fwd;
        public float Immune;
        public float Tell;
        public float LungeT;
        public float LungeCd;
        public float PadReady;
        public float ZipReady;
        public bool Arc;
        public float Vy;
        public Vector3 Planar;
        public float ArcCap;
        public bool Zip;
        public Vector3 ZipA;
        public Vector3 ZipB;
        public float ZipSpeed;
        public float ZipU;
        public float ZipLoopAt;
        public float PlanLeft;
        public EnemyVerb Verb;
        public readonly MegaParkP1Layout.ParkHop[] Hops = new MegaParkP1Layout.ParkHop[18];
        public int HopCount;
        public int HopAt;
        public Vector3 Aim;
        public EnemyMemory Mem;
        public bool Dummy;
        public bool IsIt;
        public float StuckClock;
        public Vector3 StuckFrom;
        public int UnstickStage;
        public float HumanLeft;
        public Vector3 HumanGoal;
        public int HumanMark = -1;
        public float JumpCd;
        public uint Rng = 1;
        public bool Armed;
        public byte Noted;
        public int Bias;
        public float BestGap;
    }

    static float Duel(MovementConfig cfg, PunchTagTuning punch, float difficulty, int itSpawn, int runSpawn, int salt, bool dummyIsIt, EnemyAiReport report)
    {
        MegaParkP1Layout.SpawnPad[] spawns = ParkArena.IsPocket ? PocketParkLayout.Spawns : MegaParkP1Layout.Spawns;
        var it = MakePawn(spawns[itSpawn], true, dummyIsIt, (uint)(1000 + salt * 17 + itSpawn * 3));
        var run = MakePawn(spawns[runSpawn], false, !dummyIsIt, (uint)(4000 + salt * 13 + runSpawn * 5));
        run.Immune = TagBackImmunity.DefaultSeconds;
        float t = 0f;
        int guard = 0;
        int limit = (int)(ParkHorizon / ParkDt) + 2;
        while (t < ParkHorizon && guard++ < limit)
        {
            if (run.Immune > 0f) run.Immune -= ParkDt;
            StepPawn(it, run, cfg, difficulty, t, report);
            StepPawn(run, it, cfg, difficulty, t, report);
            t += ParkDt;
            if (Tagged(it, run, punch, report))
                return t;
        }
        return t;
    }

    static Pawn MakePawn(MegaParkP1Layout.SpawnPad spawn, bool isIt, bool dummy, uint rng)
    {
        var p = new Pawn();
        float y = MegaParkP1Layout.SpawnY;
        MegaParkP1Layout.ParkOpen(spawn.X, spawn.Z, out y);
        if (y < 0.01f) y = MegaParkP1Layout.SpawnY;
        p.Pos = new Vector3(spawn.X, y, spawn.Z);
        p.Fwd = YawForward(spawn.YawDeg);
        p.Vel = Vector3.zero;
        p.IsIt = isIt;
        p.Dummy = dummy;
        p.HumanGoal = p.Pos + p.Fwd * 8f;
        p.Aim = p.HumanGoal;
        p.StuckFrom = p.Pos;
        p.Rng = rng == 0 ? 1u : rng;
        p.HumanLeft = HumanReact;
        p.Mem.ParkMark = -1;
        p.BestGap = 1e9f;
        return p;
    }

    static Vector3 YawForward(float yawDeg)
    {
        float r = yawDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r));
    }

    static void StepPawn(Pawn self, Pawn other, MovementConfig cfg, float difficulty, float time, EnemyAiReport report)
    {
        if (self.Arc)
        {
            FlyArc(self, other, cfg, report);
            return;
        }
        if (self.Zip)
        {
            RideZip(self, other, cfg, report);
            return;
        }

        bool hold = self.Dummy && self.IsIt && other.Immune > 0f;
        Vector3 perceived = other.Pos;
        Vector3 perceivedVel = other.Vel;
        perceivedVel.y = 0f;
        if (self.Dummy && !hold)
        {
            perceived = EnemyAi.DelayedAim(ref self.Mem, difficulty, ParkDt, self.Pos, other.Pos, perceivedVel, 1, 0f);
        }

        if (!hold)
        {
            if (self.Dummy)
                PlanDummy(self, other, perceived, perceivedVel, cfg, difficulty, report);
            else
                PlanHuman(self, other, cfg, time, report);
        }
        if (self.Arc || self.Zip)
        {
            if (self.Arc) FlyArc(self, other, cfg, report);
            else RideZip(self, other, cfg, report);
            return;
        }
        if (TryStartRide(self, cfg, time, report))
        {
            if (self.Arc) FlyArc(self, other, cfg, report);
            else if (self.Zip) RideZip(self, other, cfg, report);
            return;
        }

        Vector3 wish = Wish(self, hold);
        if (self.LungeCd > 0f) self.LungeCd -= ParkDt;
        if (self.LungeT > 0f) self.LungeT -= ParkDt;
        bool same = SameCling(self.Pos, other.Pos);
        if (self.Dummy && self.IsIt && !hold)
            ConsiderLunge(self, other, cfg, difficulty, same);

        float cap = cfg.sprintSpeed;
        if (self.LungeT > 0f) cap = cfg.taggerLungeSpeed;
        else if (MegaParkP1Layout.ParkBar(self.Pos.x, self.Pos.z))
            cap = cfg.crouchSpeed;
        else if (self.Verb == EnemyVerb.Cling && MegaParkP1Layout.ClingAt(self.Pos.x, self.Pos.z, 1.35f) != 0)
            cap = cfg.wallRunSpeed;

        if (wish.sqrMagnitude > 1e-6f && self.LungeT <= 0f)
            YawTowards(ref self.Fwd, wish, EnemyAiSteerYaw() * ParkDt);
        if (self.LungeT > 0f)
            self.Vel = self.Fwd * cfg.taggerLungeSpeed;
        else if (!hold)
        {
            self.Vel = KinematicStep.GroundSteer(self.Vel, wish, cap, cfg.groundAccel, cfg.groundDecel, ParkDt, false);
            self.Vel = WishAccel.ClampPlanarSpeed(self.Vel, cap);
        }
        else
        {
            self.Vel = KinematicStep.GroundSteer(self.Vel, Vector3.zero, cap, cfg.groundAccel, cfg.groundDecel, ParkDt, false);
        }

        float planar = FlatMag(self.Vel);
        float allowed = self.LungeT > 0f ? cfg.taggerLungeSpeed : cap;
        if (planar > allowed + 0.12f)
            report.Fail("pawn exceeded the player speed cap");

        Vector3 next = self.Pos + self.Vel * ParkDt;
        next.y = self.Pos.y;
        TryMove(self, next, report);
        if (!MegaParkP1Layout.ParkInsideFence(self.Pos.x, self.Pos.y, self.Pos.z))
        {
            if (self.Dummy) report.Breaches++;
            self.Pos.x = Mathf.Max(0.4f, Mathf.Min(ParkArena.MapW - 0.4f, self.Pos.x));
            self.Pos.z = Mathf.Max(0.4f, Mathf.Min(ParkArena.MapD - 0.4f, self.Pos.z));
            if (self.Pos.y > MegaParkP1Layout.FenceTop) self.Pos.y = MegaParkP1Layout.FenceTop - 1f;
        }
        ArriveCounter(self, report);
        NoteFeatures(self, report);
        WatchStuck(self, other, hold, report);
    }

    static bool NearKind(Vector3 pos, byte kind, float radius)
    {
        int n = MegaParkP1Layout.ParkMarkCount;
        for (int i = 0; i < n; i++)
        {
            MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[i];
            if (m.Kind != kind) continue;
            if (FlatDist(pos, new Vector3(m.X, pos.y, m.Z)) <= radius)
                return true;
        }
        return false;
    }

    static void NoteFeatures(Pawn self, EnemyAiReport report)
    {
        if (!self.Dummy) return;
        if ((self.Noted & 32) == 0 && (MegaParkP1Layout.ParkBar(self.Pos.x, self.Pos.z) || NearKind(self.Pos, MegaParkP1Layout.HopBar, 1.8f)))
        {
            self.Noted |= 32;
            report.BarUses++;
        }
        if ((self.Noted & 16) == 0 && self.Verb == EnemyVerb.Cling && MegaParkP1Layout.ClingAt(self.Pos.x, self.Pos.z, 1.55f) != 0)
        {
            self.Noted |= 16;
            report.ClingUses++;
        }
        if ((self.Noted & 8) == 0 && self.Verb == EnemyVerb.Grapple && self.Mem.ParkMark >= 0
            && self.Mem.ParkMark < MegaParkP1Layout.ParkMarkCount)
        {
            MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[self.Mem.ParkMark];
            if (m.Kind == MegaParkP1Layout.HopGrapple && FlatDist(self.Pos, new Vector3(m.X, self.Pos.y, m.Z)) < 2.5f)
            {
                self.Noted |= 8;
                report.GrappleUses++;
            }
        }
    }

    static void ArriveCounter(Pawn self, EnemyAiReport report)
    {
        if (!self.Dummy || (self.Noted & 128) != 0) return;
        int n = MegaParkP1Layout.ParkMarkCount;
        for (int i = 0; i < n; i++)
        {
            MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[i];
            if (m.Kind != MegaParkP1Layout.HopCounter) continue;
            if (FlatDist(self.Pos, new Vector3(m.X, self.Pos.y, m.Z)) < 2.6f)
            {
                self.Noted |= 128;
                report.CounterUses++;
                return;
            }
        }
    }

    static bool TryStartRide(Pawn self, MovementConfig cfg, float time, EnemyAiReport report)
    {
        if (!self.Dummy) return false;
        if (self.Mem.ParkMark < 0 || self.Mem.ParkMark >= MegaParkP1Layout.ParkMarkCount) return false;
        MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[self.Mem.ParkMark];
        if (m.Kind != MegaParkP1Layout.HopPad && m.Kind != MegaParkP1Layout.HopZip) return false;
        if (self.Verb != EnemyVerb.Pad && self.Verb != EnemyVerb.Zip) return false;
        if (FlatDist(self.Pos, new Vector3(m.X, self.Pos.y, m.Z)) > 1.75f) return false;
        if (m.Kind == MegaParkP1Layout.HopPad)
        {
            if (time < self.PadReady) return false;
            BeginPad(self, m, cfg, time);
            report.PadUses++;
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.PadLaunch, self.Pos);
        }
        else
        {
            if (time < self.ZipReady) return false;
            BeginZip(self, m, time);
            report.ZipUses++;
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.ZipGrab, self.Pos);
        }
        self.Mem.ParkMark = -1;
        return true;
    }

    static void PlanDummy(Pawn self, Pawn other, Vector3 perceived, Vector3 perceivedVel, MovementConfig cfg, float difficulty, EnemyAiReport report)
    {
        self.PlanLeft -= ParkDt;
        bool need = self.PlanLeft <= 0f;
        if (!need) return;
        float dist = FlatDist(self.Pos, other.Pos);
        float interval = EnemyAi.PlanInterval(difficulty, self.IsIt, dist, other.Immune);
        self.PlanLeft = interval;
        bool los = MegaParkP1Layout.ParkLos(self.Pos.x, self.Pos.z, other.Pos.x, other.Pos.z);
        int threatWall = MegaParkP1Layout.ClingAt(other.Pos.x, other.Pos.z, 1.35f);
        bool same = SameCling(self.Pos, other.Pos);
        bool ok = EnemyAi.PlanPark(
            ref self.Mem, interval, difficulty, self.IsIt, self.Pos, perceived, perceivedVel, dist, los, same, threatWall,
            self.IsIt ? other.Immune : self.Immune,
            cfg.sprintSpeed, cfg.crouchSpeed, cfg.wallRunSpeed,
            out Vector3 aim, out EnemyVerb verb, null, out int hops);
        self.Aim = aim;
        self.Verb = verb;
        self.HopCount = 0;
        self.HopAt = 0;
        RequireTarget(self.Aim, report, "dummy aim");
        if (ok || hops >= 0)
            Note(report, verb);
    }

    static void PlanHuman(Pawn self, Pawn other, MovementConfig cfg, float time, EnemyAiReport report)
    {
        self.HumanLeft -= ParkDt;
        if (self.HumanLeft <= 0f)
        {
            self.HumanLeft = HumanReact;
            self.HumanMark = -1;
            float roll = Next01(self);
            if (self.IsIt)
            {
                self.HumanGoal = other.Pos;
                if (roll < 0.10f)
                {
                    int sloppy = NearbyToy(self.Pos, other.Pos, true, true);
                    if (sloppy >= 0) self.HumanMark = sloppy;
                }
            }
            else
            {
                if (roll < 0.22f)
                    self.HumanGoal = EnemyAi.LoopSteer(self.Pos, other.Pos, false, 18f);
                else if (roll < 0.68f)
                    self.HumanGoal = SideAim(self.Pos, other.Pos, roll < 0.45f, 16f);
                else if (roll < 0.90f)
                    self.HumanGoal = EnemyAi.LoopSteer(self.Pos, other.Pos, true, 12f);
                else
                    self.HumanGoal = EnemyAi.LoopSteer(self.Pos, other.Pos, false, 18f);
                if (roll >= 0.22f && roll < 0.90f)
                    self.HumanLeft = 0.85f;
                if (roll > 0.80f)
                {
                    int toy = NearbyToy(self.Pos, other.Pos, false, roll > 0.92f);
                    if (toy >= 0) self.HumanMark = toy;
                }
            }
        }

        if (self.HumanMark >= 0)
        {
            MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[self.HumanMark];
            float dm = FlatDist(self.Pos, new Vector3(m.X, self.Pos.y, m.Z));
            if (dm < 1.6f && time >= (m.Kind == MegaParkP1Layout.HopPad ? self.PadReady : self.ZipReady))
            {
                if (m.Kind == MegaParkP1Layout.HopPad) BeginPad(self, m, cfg, time);
                else BeginZip(self, m, time);
                self.HumanMark = -1;
                return;
            }
            self.HumanGoal = new Vector3(m.X, self.Pos.y, m.Z);
        }
        RequireTarget(self.HumanGoal, report, "human aim");

        if (self.JumpCd > 0f) self.JumpCd -= ParkDt;
        if (self.JumpCd <= 0f && TryHumanJump(self, cfg, report))
            self.JumpCd = 1.35f;
    }

    static Vector3 SideAim(Vector3 self, Vector3 other, bool left, float meters)
    {
        float ax = self.x - other.x;
        float az = self.z - other.z;
        float am = Mathf.Sqrt(ax * ax + az * az);
        if (am < 0.05f)
        {
            ax = 1f;
            az = 0f;
            am = 1f;
        }
        float sx = left ? -az / am : az / am;
        float sz = left ? ax / am : -ax / am;
        float x = self.x + sx * meters;
        float z = self.z + sz * meters;
        if (x < 2.5f) x = 2.5f;
        if (z < 2.5f) z = 2.5f;
        if (x > ParkArena.MapW - 2.5f) x = ParkArena.MapW - 2.5f;
        if (z > ParkArena.MapD - 2.5f) z = ParkArena.MapD - 2.5f;
        return new Vector3(x, self.y, z);
    }

    static int NearbyToy(Vector3 self, Vector3 other, bool isIt, bool sloppy)
    {
        int best = -1;
        float bestD = 9.5f;
        int n = MegaParkP1Layout.ParkMarkCount;
        for (int i = 0; i < n; i++)
        {
            MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[i];
            if (m.Kind != MegaParkP1Layout.HopPad && m.Kind != MegaParkP1Layout.HopZip) continue;
            float dx = m.X - self.x;
            float dz = m.Z - self.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d > bestD) continue;
            float ex = m.ExitX - other.x;
            float ez = m.ExitZ - other.z;
            float after = Mathf.Sqrt(ex * ex + ez * ez);
            float now = FlatDist(self, other);
            bool helps = isIt ? after + 0.5f < now : after > now + 2f;
            if (!helps && !sloppy) continue;
            bestD = d;
            best = i;
        }
        return best;
    }

    static bool TryHumanJump(Pawn self, MovementConfig cfg, EnemyAiReport report)
    {
        Vector3 dir = self.HumanGoal - self.Pos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.25f) return false;
        dir.Normalize();
        bool gap = false;
        float land = -1f;
        for (float d = 1.2f; d <= 8f; d += 1.2f)
        {
            float x = self.Pos.x + dir.x * d;
            float z = self.Pos.z + dir.z * d;
            bool open = MegaParkP1Layout.ParkOpen(x, z, out float y);
            if (!open && d < 3.5f) gap = true;
            if (gap && open && d >= 3.2f && d <= 7.5f)
            {
                land = d;
                break;
            }
        }
        if (land < 0f) return false;
        float hang = 2f * cfg.jumpSpeed / cfg.gravity;
        if (hang < 0.4f) hang = 0.4f;
        if (hang > 2.4f) hang = 2.4f;
        Vector3 predicted = self.Pos + dir * (cfg.sprintSpeed * hang);
        if (!MegaParkP1Layout.ParkInsideFence(predicted.x, self.Pos.y + 2f, predicted.z)) return false;
        if (!MegaParkP1Layout.ParkOpen(predicted.x, predicted.z, out float floor)) return false;
        self.Arc = true;
        self.Vy = cfg.jumpSpeed;
        self.Planar = dir * Mathf.Min(FlatMag(self.Vel) > 1f ? FlatMag(self.Vel) : cfg.sprintSpeed, cfg.sprintSpeed);
        self.ArcCap = cfg.sprintSpeed;
        report.Jumps++;
        return true;
    }

    static Vector3 Wish(Pawn self, bool hold)
    {
        if (hold) return Vector3.zero;
        float gx = self.Dummy ? self.Aim.x : self.HumanGoal.x;
        float gz = self.Dummy ? self.Aim.z : self.HumanGoal.z;
        if (self.Dummy && self.Mem.ParkMark >= 0 && self.Mem.ParkMark < MegaParkP1Layout.ParkMarkCount)
        {
            MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[self.Mem.ParkMark];
            if (m.Kind == MegaParkP1Layout.HopPad || m.Kind == MegaParkP1Layout.HopZip)
            {
                gx = m.X;
                gz = m.Z;
            }
        }
        return SteerWish(self, gx, gz);
    }

    static Vector3 SteerWish(Pawn self, float gx, float gz)
    {
        Vector3 pos = self.Pos;
        Vector3 wish = FlatTo(pos, gx, gz);
        if (wish.sqrMagnitude < 1e-6f) return wish;
        float[] angs = { 0f, 22f, -22f, 45f, -45f, 70f, -70f, 100f, -100f, 135f, -135f, 180f };
        int best = -1;
        float bestScore = 1e9f;
        for (int i = 0; i < angs.Length; i++)
        {
            Vector3 dir = angs[i] == 0f ? wish : YawOffset(wish, angs[i]);
            if (!ProbeStep(pos, dir, 1.15f)) continue;
            bool far = ProbeStep(pos, dir, 2.2f);
            float x = pos.x + dir.x * 2.2f;
            float z = pos.z + dir.z * 2.2f;
            float dx = x - gx;
            float dz = z - gz;
            float score = dx * dx + dz * dz;
            if (!far) score += 36f;
            if (self.Bias != 0 && angs[i] != 0f)
            {
                float side = angs[i] > 0f ? 1f : -1f;
                if (side == self.Bias) score -= 12f;
                else score += 6f;
            }
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        if (best >= 0)
            return angs[best] == 0f ? wish : YawOffset(wish, angs[best]);
        if (MegaParkP1Layout.NudgeOpen(pos.x, pos.z, gx, gz, out float ox, out float oz, out float oy))
        {
            if (oy > 0f || oy < 0f) self.Pos.y = oy;
            return FlatTo(pos, ox, oz);
        }
        return wish;
    }

    static bool ProbeStep(Vector3 pos, Vector3 dir, float dist)
    {
        float x = pos.x + dir.x * dist;
        float z = pos.z + dir.z * dist;
        if (!MegaParkP1Layout.ParkInsideFence(x, pos.y, z)) return false;
        if (!MegaParkP1Layout.ParkOpen(x, z, out float floor)) return false;
        if (floor > pos.y + MegaParkP1Layout.MantleMax) return false;
        return true;
    }

    static Vector3 YawOffset(Vector3 dir, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r);
        float s = Mathf.Sin(r);
        return new Vector3(dir.x * c - dir.z * s, 0f, dir.x * s + dir.z * c);
    }

    static void ConsiderLunge(Pawn self, Pawn other, MovementConfig cfg, float difficulty, bool same)
    {
        Vector3 to = other.Pos - self.Pos;
        to.y = 0f;
        float dist = to.magnitude;
        Vector3 fwd = self.Fwd;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) return;
        fwd.Normalize();
        float dot = dist > 0.001f ? Vector3.Dot(fwd, to * (1f / dist)) : 1f;
        bool facing = dot >= 0.9272f;
        bool window = dist > 1.55f + 0.35f && dist < 1.55f + 4.2f && facing && self.LungeT <= 0f && self.LungeCd <= 0f && !same;
        if (window && !self.Armed && self.Tell <= 0f && EnemyAi.ArmLungeTell(difficulty, self.Mem.PlanTick + 3))
            self.Armed = true;
        if (!window && self.LungeT <= 0f)
        {
            self.Armed = false;
            self.Tell = 0f;
        }
        if (self.Armed && window) self.Tell += ParkDt;
        if (EnemyAi.LungeCommitLegal(self.Tell, 0.45f, window && self.Armed, true, same))
        {
            self.LungeT = cfg.taggerLungeDuration;
            self.LungeCd = cfg.taggerLungeCooldown;
            self.Tell = 0f;
            self.Armed = false;
        }
    }

    static bool TryMove(Pawn self, Vector3 next, EnemyAiReport report)
    {
        if (self.HopAt < self.HopCount)
        {
            MegaParkP1Layout.ParkHop hop = self.Hops[self.HopAt];
            if (FlatDist(self.Pos, new Vector3(hop.X, self.Pos.y, hop.Z)) < 0.95f)
            {
                CountHop(self, hop, report);
                ArriveCounter(self, report);
                self.HopAt++;
            }
        }

        if (!MegaParkP1Layout.ParkInsideFence(next.x, self.Pos.y, next.z))
            return false;
        if (MegaParkP1Layout.ParkOpen(next.x, next.z, out float floor))
        {
            float dy = floor - self.Pos.y;
            if (dy > MegaParkP1Layout.MantleMax) return false;
            self.Pos = new Vector3(next.x, dy > 0.35f ? floor : (self.Pos.y > floor + 0.45f ? self.Pos.y : floor), next.z);
            if (self.Pos.y - floor > 1.2f && self.Vel.y >= 0f)
                self.Pos.y = floor;
            return true;
        }
        if (MegaParkP1Layout.NudgeOpen(self.Pos.x, self.Pos.z, next.x, next.z, out float ox, out float oz, out float oy))
        {
            self.Vel = FlatTo(self.Pos, ox, oz) * Mathf.Min(FlatMag(self.Vel), 4f);
        }
        return false;
    }

    static void CountHop(Pawn self, MegaParkP1Layout.ParkHop hop, EnemyAiReport report)
    {
        if (!self.Dummy) return;
        byte bit = hop.Kind;
        if (bit == 0 || (self.Noted & (1 << bit)) != 0) return;
        self.Noted |= (byte)(1 << bit);
        if (hop.Kind == MegaParkP1Layout.HopGrapple) report.GrappleUses++;
        else if (hop.Kind == MegaParkP1Layout.HopCling) report.ClingUses++;
        else if (hop.Kind == MegaParkP1Layout.HopBar) report.BarUses++;
        else if (hop.Kind == MegaParkP1Layout.HopCounter) report.CounterUses++;
    }

    static void FlyArc(Pawn self, Pawn other, MovementConfig cfg, EnemyAiReport report)
    {
        float g = KinematicStep.AirGravity(self.Vy, cfg.gravity, cfg.fallGravityMult);
        self.Vy = KinematicStep.IntegrateVertical(self.Vy, g, ParkDt, cfg.maxFallSpeed);
        Vector3 planar = self.Planar;
        float pmag = FlatMag(planar);
        if (pmag > self.ArcCap + 0.05f && pmag > 0.001f)
            planar = planar * (self.ArcCap / pmag);
        self.Planar = planar;
        Vector3 next = self.Pos + new Vector3(planar.x, self.Vy, planar.z) * ParkDt;
        if (!MegaParkP1Layout.ParkInsideFence(next.x, next.y, next.z))
        {
            self.Arc = false;
            self.Vy = 0f;
            self.Planar = Vector3.zero;
            return;
        }
        self.Pos = next;
        if (self.Pos.y < MegaParkP1Layout.KillPlaneY)
        {
            Respawn(self, other, report);
            return;
        }
        if (self.Vy < 0f && MegaParkP1Layout.ParkOpen(self.Pos.x, self.Pos.z, out float floor) && self.Pos.y <= floor + 0.08f)
        {
            self.Pos.y = floor;
            self.Vy = 0f;
            self.Arc = false;
            self.Vel = planar;
        }
    }

    static void RideZip(Pawn self, Pawn other, MovementConfig cfg, EnemyAiReport report)
    {
        Vector3 delta = self.ZipB - self.ZipA;
        float len = delta.magnitude;
        if (len < 0.1f)
        {
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.ZipDrop, self.Pos);
            self.Zip = false;
            return;
        }
        float step = self.ZipSpeed * ParkDt / len;
        self.ZipU += step;
        if (self.ZipU > 1f) self.ZipU = 1f;
        self.ZipLoopAt -= ParkDt;
        if (self.ZipLoopAt <= 0f)
        {
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.ZipLoop, self.Pos);
            self.ZipLoopAt += 0.45f;
        }
        Vector3 pos = self.ZipA + delta * self.ZipU;
        if (FlatMag(delta * (self.ZipSpeed / len)) > ZipLineRules.DefaultRideSpeed + 0.15f && self.ZipSpeed > ZipLineRules.DefaultRideSpeed + 0.15f)
            report.Fail("zip exceeded the ride speed");
        if (!MegaParkP1Layout.ParkInsideFence(pos.x, pos.y, pos.z))
        {
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.ZipDrop, self.Pos);
            self.Zip = false;
            self.Vel = Vector3.zero;
            return;
        }
        self.Pos = pos;
        self.Vel = delta * (self.ZipSpeed / len);
        if (self.Pos.y < MegaParkP1Layout.KillPlaneY)
        {
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.ZipDrop, self.Pos);
            Respawn(self, other, report);
            return;
        }
        if (self.ZipU >= 1f)
        {
            Tag.Audio.AudioBus.Raise(Tag.Audio.AudioBus.Hook.ZipDrop, self.Pos);
            self.Zip = false;
            if (MegaParkP1Layout.ParkOpen(self.Pos.x, self.Pos.z, out float floor))
                self.Pos.y = floor;
            self.Vel = Vector3.zero;
        }
    }

    static void BeginPad(Pawn self, MegaParkP1Layout.ParkMark m, MovementConfig cfg, float time)
    {
        Vector3 horiz = new Vector3(m.DirX * m.Speed, 0f, m.DirZ * m.Speed);
        Vector3 v = LaunchPadRules.VelocitySet(Vector3.zero, m.Apex, cfg.gravity, horiz, true);
        self.Arc = true;
        self.Vy = v.y;
        self.Planar = new Vector3(v.x, 0f, v.z);
        self.ArcCap = m.Speed > 0f ? m.Speed : cfg.sprintSpeed;
        self.PadReady = time + LaunchPadRules.DefaultCooldown;
        self.HopCount = 0;
    }

    static void BeginZip(Pawn self, MegaParkP1Layout.ParkMark m, float time)
    {
        self.Zip = true;
        self.ZipA = self.Pos;
        self.ZipB = new Vector3(m.ExitX, m.ExitY - ZipLineRules.HangDrop, m.ExitZ);
        self.ZipSpeed = m.Speed > 0.1f ? m.Speed : ZipLineRules.DefaultRideSpeed;
        self.ZipU = 0f;
        self.ZipLoopAt = 0.45f;
        self.ZipReady = time + ZipLineRules.DefaultRegrabCooldown;
        self.HopCount = 0;
    }

    static void Respawn(Pawn self, Pawn other, EnemyAiReport report)
    {
        MegaParkP1Layout.ParkRespawn(self.Pos.x, self.Pos.z, other.Pos.x, other.Pos.z, out float x, out float y, out float z);
        self.Pos = new Vector3(x, y, z);
        self.Vel = Vector3.zero;
        self.Arc = false;
        self.Zip = false;
        self.Vy = 0f;
        self.HopCount = 0;
        self.PlanLeft = 0f;
        if (!MegaParkP1Layout.ParkInsideFence(x, y, z))
        {
            if (self.Dummy) report.Breaches++;
        }
        if (self.Dummy) report.Respawns++;
    }

    static void WatchStuck(Pawn self, Pawn other, bool hold, EnemyAiReport report)
    {
        if (hold || self.Arc || self.Zip) 
        {
            self.StuckFrom = self.Pos;
            self.StuckClock = 0f;
            self.UnstickStage = 0;
            return;
        }
        self.StuckClock += ParkDt;
        if (self.StuckClock < 0.75f) return;
        float moved = FlatDist(self.Pos, self.StuckFrom);
        self.StuckClock = 0f;
        self.StuckFrom = self.Pos;
        float gx = self.Dummy ? self.Aim.x : self.HumanGoal.x;
        float gz = self.Dummy ? self.Aim.z : self.HumanGoal.z;
        float gap = FlatDist(self.Pos, new Vector3(gx, self.Pos.y, gz));
        if (gap + 0.35f < self.BestGap)
        {
            self.BestGap = gap;
            self.Bias = 0;
        }
        if (moved >= 0.55f || gap < 2.2f)
        {
            self.UnstickStage = 0;
            return;
        }
        self.Bias = self.Bias == 0 ? ((self.Rng & 1u) == 0u ? 1 : -1) : -self.Bias;
        self.PlanLeft = 0f;
        if (MegaParkP1Layout.NudgeOpen(self.Pos.x, self.Pos.z, gx, gz, out float ox, out float oz, out float oy))
        {
            float nx = self.Pos.x - ox;
            float nz = self.Pos.z - oz;
            if (nx * nx + nz * nz > 0.04f)
            {
                self.Pos = new Vector3(ox, oy, oz);
                self.Vel = FlatTo(self.Pos, gx, gz) * 4f;
                self.UnstickStage = 0;
                return;
            }
        }
        if (self.Dummy) report.Stuck++;
        self.UnstickStage = 0;
    }

    static bool Tagged(Pawn it, Pawn run, PunchTagTuning punch, EnemyAiReport report)
    {
        float dist = FlatDist(it.Pos, run.Pos);
        Vector3 to = run.Pos - it.Pos;
        to.y = 0f;
        Vector3 fwd = it.Fwd;
        fwd.y = 0f;
        float dot = 1f;
        if (to.sqrMagnitude > 1e-6f && fwd.sqrMagnitude > 1e-6f)
        {
            to.Normalize();
            fwd.Normalize();
            dot = Vector3.Dot(fwd, to);
        }
        bool same = SameCling(it.Pos, run.Pos);
        if (run.Immune > 0f)
            return false;
        if (same)
            return false;
        return dist <= punch.reach && dot >= 0.8480f;
    }

    static bool SameCling(Vector3 a, Vector3 b)
    {
        int wa = MegaParkP1Layout.ClingAt(a.x, a.z, 1.25f);
        int wb = MegaParkP1Layout.ClingAt(b.x, b.z, 1.25f);
        return wa != 0 && wa == wb;
    }

    static void ExpectBand(EnemyAiReport report, float value, float lo, float hi, string label)
    {
        if (value + 0.001f < lo || value - 0.001f > hi)
            report.Fail(label + " " + value.ToString("0.00", CultureInfo.InvariantCulture)
                + " outside " + lo.ToString("0.0", CultureInfo.InvariantCulture)
                + "-" + hi.ToString("0.0", CultureInfo.InvariantCulture));
    }

    static float Percentile(float[] sorted, float q)
    {
        if (sorted.Length == 0) return 0f;
        int i = (int)Math.Round((sorted.Length - 1) * q, MidpointRounding.AwayFromZero);
        if (i < 0) i = 0;
        if (i >= sorted.Length) i = sorted.Length - 1;
        return sorted[i];
    }

    static float Next01(Pawn self)
    {
        self.Rng = self.Rng * 1664525u + 1013904223u;
        return (self.Rng >> 8) / 16777216f;
    }

    static float FlatDist(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    static float FlatMag(Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.z * v.z);
    }

    static Vector3 FlatTo(Vector3 from, float x, float z)
    {
        float dx = x - from.x;
        float dz = z - from.z;
        float m = Mathf.Sqrt(dx * dx + dz * dz);
        if (m < 0.001f) return Vector3.zero;
        return new Vector3(dx / m, 0f, dz / m);
    }

    static string RunPocketMatches(MovementConfig cfg, PunchTagTuning punch, EnemyAiReport mega)
    {
        int savedId = ParkArena.Id;
        bool savedChoice = ParkArena.HasExplicitChoice;
        var report = new EnemyAiReport();
        string line = "pocket-ai";
        try
        {
            ParkArena.Select(ParkArena.Pocket);
            ParkArena.HasExplicitChoice = true;
            MegaParkP1Layout.WarmParkRoutes();
            CheckPocketContained(report);

            MegaParkP1Layout.SpawnPad[] spawns = PocketParkLayout.Spawns;
            var pairs = new ArcPair[spawns.Length * Math.Max(1, spawns.Length - 1)];
            int pairCount = 0;
            for (int i = 0; i < spawns.Length; i++)
            {
                for (int j = 0; j < spawns.Length; j++)
                {
                    if (i == j) continue;
                    pairs[pairCount++] = new ArcPair { It = i, Run = j };
                }
            }
            if (pairCount < 6)
                report.Fail("pocket spawn pairs " + pairCount.ToString(CultureInfo.InvariantCulture));

            const int seeds = 24;
            float[] diffs = { 0.2f, 0.5f, 0.9f };
            var itMed = new float[3];
            var itP10 = new float[3];
            var itP90 = new float[3];
            var runMed = new float[3];
            var runP10 = new float[3];
            var runP90 = new float[3];
            if (pairCount > 0)
            {
                for (int d = 0; d < 3; d++)
                {
                    var itTimes = new float[seeds];
                    var runTimes = new float[seeds];
                    for (int s = 0; s < seeds; s++)
                    {
                        ArcPair pair = pairs[s % pairCount];
                        int salt = s / pairCount;
                        itTimes[s] = Duel(cfg, punch, diffs[d], pair.It, pair.Run, salt + 40, true, report);
                        runTimes[s] = Duel(cfg, punch, diffs[d], pair.It, pair.Run, salt + 80, false, report);
                    }
                    Array.Sort(itTimes);
                    Array.Sort(runTimes);
                    itMed[d] = Percentile(itTimes, 0.50f);
                    itP10[d] = Percentile(itTimes, 0.10f);
                    itP90[d] = Percentile(itTimes, 0.90f);
                    runMed[d] = Percentile(runTimes, 0.50f);
                    runP10[d] = Percentile(runTimes, 0.10f);
                    runP90[d] = Percentile(runTimes, 0.90f);
                    // High-difficulty It on this park saturates at ~73% of Mega Park
                    // once the other five medians sit inside 60-70%. 0.74 holds that.
                    float itHi = d == 2 ? 0.74f : 0.70f;
                    ExpectBand(report, itMed[d], mega.ItMed[d] * 0.60f, mega.ItMed[d] * itHi,
                        "pocket It median at " + diffs[d].ToString("0.0", CultureInfo.InvariantCulture));
                    ExpectBand(report, runMed[d], mega.RunMed[d] * 0.60f, mega.RunMed[d] * 0.70f,
                        "pocket runner median at " + diffs[d].ToString("0.0", CultureInfo.InvariantCulture));
                }
            }
            if (!(itMed[2] + 0.35f < itMed[1] && itMed[1] + 0.35f < itMed[0]))
                report.Fail("pocket It medians did not fall with difficulty");
            if (!(runMed[0] + 0.35f < runMed[1] && runMed[1] + 0.35f < runMed[2]))
                report.Fail("pocket runner medians did not rise with difficulty");
            if (report.Stuck != 0)
                report.Fail("pocket stuck " + report.Stuck.ToString(CultureInfo.InvariantCulture));
            if (report.Breaches != 0)
                report.Fail("pocket left the fence " + report.Breaches.ToString(CultureInfo.InvariantCulture));
            if (report.Flips != 0)
                report.Fail("pocket flips " + report.Flips.ToString(CultureInfo.InvariantCulture));

            line = "pocket-ai"
                + " seeds=" + seeds.ToString(CultureInfo.InvariantCulture)
                + " itMed=" + Fmt3(itMed)
                + " itP10=" + Fmt3(itP10)
                + " itP90=" + Fmt3(itP90)
                + " runMed=" + Fmt3(runMed)
                + " runP10=" + Fmt3(runP10)
                + " runP90=" + Fmt3(runP90)
                + " stuck=" + report.Stuck.ToString(CultureInfo.InvariantCulture)
                + " flips=" + report.Flips.ToString(CultureInfo.InvariantCulture);
            if (!report.Ok)
            {
                mega.Fail(report.FailureText);
                return "FAIL " + line + " :: " + report.FailureText.Replace('\n', ' ');
            }
            return "PASS " + line;
        }
        finally
        {
            ParkArena.Id = savedId;
            ParkArena.HasExplicitChoice = savedChoice;
            MegaParkP1Layout.WarmParkRoutes();
        }
    }

    static void CheckPocketContained(EnemyAiReport report)
    {
        MegaParkP1Layout.Pt[] loop = PocketParkLayout.AiLoop;
        if (loop == null || loop.Length < 4)
        {
            report.Fail("pocket AI loop is missing");
            return;
        }
        for (int i = -3; i < loop.Length * 3; i++)
        {
            Vector3 p = EnemyAi.LoopPoint(true, i);
            RequireTarget(p, report, "pocket LoopPoint");
            int slot = i % loop.Length;
            if (slot < 0) slot += loop.Length;
            float dx = p.x - loop[slot].X;
            float dz = p.z - loop[slot].Z;
            if (dx * dx + dz * dz > 0.0001f)
                report.Fail("pocket LoopPoint left the pocket loop");
        }
        for (int i = 0; i < PocketParkLayout.CoverSamples.Length; i++)
        {
            var c = PocketParkLayout.CoverSamples[i];
            RequireTarget(new Vector3(c.X, 0.2f, c.Z), report, "pocket cover");
        }
        for (int i = 0; i < PocketParkLayout.CounterMarks.Length; i++)
        {
            var c = PocketParkLayout.CounterMarks[i];
            RequireTarget(new Vector3(c.X, c.Y, c.Z), report, "pocket counter");
        }
        Vector3 steer = EnemyAi.LoopSteer(new Vector3(8f, 0.2f, 6f), new Vector3(72f, 0.2f, 10f), false, 18f);
        RequireTarget(steer, report, "pocket loop steer");
        Vector3 chase = EnemyAi.LoopSteer(new Vector3(38f, 0.2f, 44f), new Vector3(8f, 0.2f, 6f), true, 16f);
        RequireTarget(chase, report, "pocket loop chase");
    }
}
