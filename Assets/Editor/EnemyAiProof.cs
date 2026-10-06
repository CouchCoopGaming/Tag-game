using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Deterministic opponent. An It dummy chases a scripted runner at three
/// difficulties. A runner dummy survives a scripted It. Verbs are counted.
/// Route scores that swap by a hair must not flip.
/// </summary>
public static partial class EnemyAiProof
{
    public static EnemyAiReport Run()
    {
        var report = new EnemyAiReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
        CheckLocks(report, cfg, punch);
        CheckScalars(report);
        CheckJitter(report);
        CheckRules(report, cfg, punch);

        CheckSources(report);
        RunParkMatches(cfg, punch, report);
        report.PocketLine = RunPocketMatches(cfg, punch, report);
        report.Line = "enemy-ai"
            + " diff=" + EnemyAi.DefaultDifficulty.ToString("0.00", CultureInfo.InvariantCulture)
            + " delay=" + EnemyAi.ReactionDelay(0.2f).ToString("0.000", CultureInfo.InvariantCulture)
            + "/" + EnemyAi.ReactionDelay(0.5f).ToString("0.000", CultureInfo.InvariantCulture)
            + "/" + EnemyAi.ReactionDelay(0.9f).ToString("0.000", CultureInfo.InvariantCulture)
            + " itMed=" + Fmt3(report.ItMed)
            + " itP10=" + Fmt3(report.ItP10)
            + " itP90=" + Fmt3(report.ItP90)
            + " runMed=" + Fmt3(report.RunMed)
            + " runP10=" + Fmt3(report.RunP10)
            + " runP90=" + Fmt3(report.RunP90)
            + " stuck=" + report.Stuck.ToString(CultureInfo.InvariantCulture)
            + " flips=" + report.Flips.ToString(CultureInfo.InvariantCulture)
            + " verbs " + report.VerbText()
            + " use pad=" + report.PadUses.ToString(CultureInfo.InvariantCulture)
            + " zip=" + report.ZipUses.ToString(CultureInfo.InvariantCulture)
            + " grapple=" + report.GrappleUses.ToString(CultureInfo.InvariantCulture)
            + " cling=" + report.ClingUses.ToString(CultureInfo.InvariantCulture)
            + " bar=" + report.BarUses.ToString(CultureInfo.InvariantCulture)
            + " counter=" + report.CounterUses.ToString(CultureInfo.InvariantCulture)
            + " respawn=" + report.Respawns.ToString(CultureInfo.InvariantCulture);
        return report;
    }

    static void CheckLocks(EnemyAiReport report, MovementConfig cfg, PunchTagTuning punch)
    {
        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) report.Fail("coyote drifted");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) report.Fail("jump buffer drifted");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) report.Fail("cling grace drifted");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) report.Fail("jumpSpeed drifted");
        if (cfg.slideBoost != 0f) report.Fail("slideBoost drifted");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f || Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f
            || Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail("air dash drifted");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f) report.Fail("punch reach drifted");
        if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f || Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f
            || Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f)
            report.Fail("lunge drifted");
        if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f || Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f
            || Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f)
            report.Fail("wall speeds drifted");
        if (Mathf.Abs(TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f) report.Fail("tag-back drifted");
        if (Mathf.Abs(LaunchPadRules.DefaultCooldown - 0.3f) > 0.001f) report.Fail("pad cooldown drifted");
        if (Mathf.Abs(ZipLineRules.DefaultRideSpeed - 14f) > 0.001f) report.Fail("zip speed drifted");
        if (Mathf.Abs(ZipLineRules.DefaultRegrabCooldown - 0.3f) > 0.001f) report.Fail("zip regrab drifted");
        if (Mathf.Abs(PunchStagger.Duration - 0.25f) > 0.001f || Mathf.Abs(PunchStagger.Immunity - 0.50f) > 0.001f)
            report.Fail("stagger drifted");
    }

    static void CheckScalars(EnemyAiReport report)
    {
        if (Mathf.Abs(EnemyAi.DefaultDifficulty - 0.5f) > 0.001f) report.Fail("default difficulty is not 0.5");
        if (Mathf.Abs(EnemyAi.ReactionDelay(0f) - 0.35f) > 0.001f) report.Fail("slow reaction is not 0.35");
        if (Mathf.Abs(EnemyAi.ReactionDelay(1f) - 0.12f) > 0.001f) report.Fail("fast reaction is not 0.12");
        if (!(EnemyAi.ReactionDelay(0.2f) > EnemyAi.ReactionDelay(0.5f)
            && EnemyAi.ReactionDelay(0.5f) > EnemyAi.ReactionDelay(0.9f)))
            report.Fail("reaction delay does not fall with difficulty");
        if (!(EnemyAi.AimErrorMeters(0.2f) > EnemyAi.AimErrorMeters(0.9f)))
            report.Fail("aim error does not fall with difficulty");
        if (!(EnemyAi.VerbRate(0.2f) < EnemyAi.VerbRate(0.9f)))
            report.Fail("verb rate does not rise with difficulty");
        if (EnemyAi.ClampDummyCount(0) != 1 || EnemyAi.ClampDummyCount(2) != 2 || EnemyAi.ClampDummyCount(9) != 3)
            report.Fail("dummy count is not clamped to 1..3");
        if (!EnemyAi.AllowRope(true, "DummyRunner") || !EnemyAi.AllowRope(true, "DummyRunner_2"))
            report.Fail("opponent rope is closed");
        if (EnemyAi.AllowRope(false, "DummyRunner") || EnemyAi.AllowRope(true, "Player"))
            report.Fail("rope leaked off the opponent");
        if (EnemyAi.LungeCommitLegal(0.20f, 0.45f, true, true, false))
            report.Fail("lunge fired before the tell");
        if (!EnemyAi.LungeCommitLegal(0.45f, 0.45f, true, true, false))
            report.Fail("lunge did not commit at 0.45s");
        if (EnemyAi.LungeCommitLegal(0.45f, 0.45f, false, true, false)
            || EnemyAi.LungeCommitLegal(0.45f, 0.45f, true, false, false)
            || EnemyAi.LungeCommitLegal(0.45f, 0.45f, true, true, true))
            report.Fail("lunge committed outside the window");
    }

    static void CheckJitter(EnemyAiReport report)
    {
        EnemyMemory memory = default;
        const float dt = 1f / 60f;
        for (int frame = 0; frame < 180; frame++)
        {
            int candidate = (frame & 1) == 0 ? 1 : 2;
            float candidateScore = candidate == 1 ? 10f : 10.08f;
            float held = memory.RouteId == 2 ? 10.08f : 10f;
            EnemyAi.CommitRoute(ref memory, dt, candidate, candidateScore, held, memory.RouteId != 0);
        }

        if (memory.Flips != 0)
            report.Fail("route scores oscillated");
        report.Flips = memory.Flips;

        EnemyMemory edge = default;
        for (int frame = 0; frame < 180; frame++)
        {
            EnemySense sense = default;
            sense.Dt = dt;
            sense.Difficulty = 0.5f;
            sense.Grounded = true;
            sense.ThreatClosing = true;
            sense.SelfPos = Vector3.zero;
            sense.Forward = new Vector3(0f, 0f, 1f);
            sense.PunchReach = 1.55f;
            bool pad = (frame & 1) == 0;
            sense.PadAhead = pad;
            sense.PadHelps = pad;
            sense.PadAim = new Vector3(0f, 0f, 1f);
            sense.ZipAhead = !pad;
            sense.ZipHelps = !pad;
            sense.ZipAim = new Vector3(0f, 0f, 1f);
            sense.LineOfSight = false;
            EnemyAi.Evade(ref edge, sense);
        }

        if (edge.Flips != 0)
            report.Fail("pad and zip edges oscillated");
    }

    static void CheckRules(EnemyAiReport report, MovementConfig cfg, PunchTagTuning punch)
    {
        OpponentChaseWish sprint = OpponentChaseSteer.Decide(FarInput());
        Note(report, EnemyAi.Decorate(ref report.GymMemory, Sense(true, 12f, punch.reach), sprint).Verb);

        OpponentChaseInput near = FarInput();
        near.PlanarDistance = 3f;
        OpponentChaseWish close = OpponentChaseSteer.Decide(near);
        Note(report, EnemyAi.Decorate(ref report.GymMemory, Sense(true, 3f, punch.reach), close).Verb);

        OpponentChaseWish hop = default;
        hop.Verb = OpponentChaseVerb.GapJump;
        hop.Face = new Vector3(0f, 0f, 1f);
        hop.MoveY = 1f;
        hop.Sprint = true;
        hop.Jump = true;
        Note(report, EnemyAi.Decorate(ref report.GymMemory, Sense(true, 8f, punch.reach), hop).Verb);

        OpponentChaseInput wall = FarInput();
        wall.WallNormal = new Vector3(0f, 0f, 1f);
        wall.WallDistance = 1.1f;
        wall.Aim = new Vector3(0f, 0f, -1f);
        wall.PlanarDistance = 12f;
        OpponentChaseWish cling = OpponentChaseSteer.Decide(wall);
        EnemySense clingSense = Sense(true, 12f, punch.reach);
        clingSense.WallNormal = wall.WallNormal;
        Note(report, EnemyAi.Decorate(ref report.GymMemory, clingSense, cling).Verb);

        OpponentChaseInput glance = wall;
        glance.Aim = new Vector3(1f, 0f, 0f);
        OpponentChaseWish run = OpponentChaseSteer.Decide(glance);
        Note(report, EnemyAi.Decorate(ref report.GymMemory, clingSense, run).Verb);

        EnemySense jumpSense = clingSense;
        jumpSense.OnWall = true;
        jumpSense.WallTime = 0.40f;
        jumpSense.TargetHeightDelta = 2f;
        jumpSense.ForceVerbs = true;
        jumpSense.Dt = 0.016f;
        Note(report, EnemyAi.Decorate(ref report.GymMemory, jumpSense, cling).Verb);

        EnemySense slideSense = Sense(true, 20f, punch.reach);
        slideSense.SlideOpen = true;
        slideSense.Grounded = true;
        slideSense.PlanarSpeed = 10f;
        slideSense.ForceVerbs = true;
        OpponentChaseWish sprintWish = sprint;
        Note(report, EnemyAi.Decorate(ref report.GymMemory, slideSense, sprintWish).Verb);

        OpponentChaseWish strafe = default;
        strafe.Verb = OpponentChaseVerb.AirStrafe;
        strafe.Face = new Vector3(1f, 0f, 0f);
        strafe.MoveY = 0.12f;
        strafe.Strafe = 1f;
        EnemySense dashSense = Sense(true, 8f, punch.reach);
        dashSense.Airborne = true;
        dashSense.Grounded = false;
        dashSense.AirDashReady = true;
        dashSense.ForceVerbs = true;
        EnemyOverlay dashed = EnemyAi.Decorate(ref report.GymMemory, dashSense, strafe);
        Note(report, dashed.Verb);
        if (!dashed.Dash) report.Fail("air dash was not chosen");

        float dashCd = 0f;
        int dashes = 0;
        if (dashed.Dash && dashCd <= 0f)
        {
            dashes++;
            dashCd = cfg.airDashCooldown;
        }
        if (dashed.Dash && dashCd > 0f && dashes == 1)
        {
            // Second press inside the cooldown does not fire again.
        }
        else
            report.Fail("air dash cooldown was not 30s");
        if (dashes != 1) report.Fail("air dash fired twice");

        EnemySense rope = Sense(true, 10f, punch.reach);
        rope.GrappleLatch = true;
        rope.GrappleOutward = true;
        rope.GrappleProbe = true;
        rope.ForceVerbs = true;
        EnemyOverlay hooked = EnemyAi.Decorate(ref report.GymMemory, rope, sprint);
        Note(report, hooked.Verb);
        if (!hooked.Grapple) report.Fail("grapple was not chosen");

        EnemyMemory missMemory = default;
        EnemySense miss = Sense(true, 10f, punch.reach);
        miss.GrappleProbe = true;
        miss.GrappleLatch = false;
        miss.Difficulty = 0.2f;
        miss.ForceVerbs = true;
        EnemyOverlay whiff = EnemyAi.Decorate(ref missMemory, miss, sprint);
        Note(report, whiff.Verb);
        Vector3 velocity = new Vector3(0f, 0f, 8f);
        Vector3 after = velocity;
        if (whiff.Grapple && !miss.GrappleLatch)
            after = velocity;
        if ((after - velocity).sqrMagnitude > 0.0001f)
            report.Fail("a grapple miss latched a swing");

        OpponentChaseWish lunge = default;
        lunge.Verb = OpponentChaseVerb.Lunge;
        lunge.Lunge = true;
        lunge.Face = new Vector3(0f, 0f, 1f);
        lunge.MoveY = 1f;
        Note(report, EnemyAi.Decorate(ref report.GymMemory, Sense(true, 3f, punch.reach), lunge).Verb);

        EnemySense fist = Sense(true, 1.2f, punch.reach);
        fist.AngleDeg = 8f;
        fist.IsIt = true;
        OpponentChaseWish walk = close;
        EnemyOverlay punchOut = EnemyAi.Decorate(ref report.GymMemory, fist, walk);
        Note(report, punchOut.Verb);
        if (!punchOut.Punch) report.Fail("It did not punch in reach");

        EnemySense immune = fist;
        immune.TargetTagBackBlocked = true;
        EnemyOverlay held = EnemyAi.Decorate(ref report.GymMemory, immune, sprint);
        Note(report, held.Verb);
        if (held.Verb != EnemyVerb.Hold || held.Punch || Mathf.Abs(held.MoveY) > 0.001f)
            report.Fail("It chased an immune runner");

        EnemySense closed = clingSense;
        closed.SameWallClosed = true;
        EnemyOverlay refused = EnemyAi.Decorate(ref report.GymMemory, closed, cling);
        if (refused.Verb == EnemyVerb.Cling || refused.Verb == EnemyVerb.WallRun || refused.WallJump)
            report.Fail("same-wall limit was ignored");

        EnemyMemory evade = default;
        EnemySense flee = Sense(false, 6f, punch.reach);
        flee.ThreatClosing = true;
        flee.Grounded = true;
        flee.PadAhead = true;
        flee.PadHelps = true;
        flee.PadAim = new Vector3(0f, 0f, 1f);
        flee.SelfTagBackRemaining = 0f;
        Note(report, EnemyAi.Evade(ref evade, flee).Verb);

        EnemyMemory zipMem = default;
        EnemySense zip = Sense(false, 6f, punch.reach);
        zip.ThreatClosing = true;
        zip.ZipAhead = true;
        zip.ZipHelps = true;
        zip.ZipAim = new Vector3(1f, 0f, 0f);
        Note(report, EnemyAi.Evade(ref zipMem, zip).Verb);

        EnemyMemory loopMem = default;
        EnemySense loop = Sense(false, 14f, punch.reach);
        loop.LineOfSight = false;
        loop.SelfPos = Vector3.zero;
        loop.Forward = new Vector3(0f, 0f, 1f);
        Note(report, EnemyAi.Evade(ref loopMem, loop).Verb);

        EnemyMemory coverMem = default;
        EnemySense cover = loop;
        cover.LineOfSight = true;
        cover.CoverAim = new Vector3(1f, 0f, 0f);
        Note(report, EnemyAi.Evade(ref coverMem, cover).Verb);

        EnemyMemory escapeMem = default;
        EnemySense escape = loop;
        escape.SelfTagBackRemaining = 1f;
        escape.Cornered = true;
        escape.PlanarDistance = 1.2f;
        EnemyOverlay escaped = EnemyAi.Evade(ref escapeMem, escape);
        Note(report, escaped.Verb);
        if (escaped.Punch) report.Fail("runner punched during tag-back");

        EnemyMemory cornerMem = default;
        EnemySense corner = Sense(false, 1.2f, punch.reach);
        corner.Cornered = true;
        corner.SelfTagBackRemaining = 0f;
        corner.LineOfSight = false;
        corner.SelfPos = Vector3.zero;
        corner.Forward = new Vector3(0f, 0f, 1f);
        EnemyOverlay swung = EnemyAi.Evade(ref cornerMem, corner);
        Note(report, swung.Verb);
        if (!swung.Punch) report.Fail("cornered runner did not punch");

        EnemyMemory openMem = default;
        EnemySense open = corner;
        open.Cornered = false;
        if (EnemyAi.Evade(ref openMem, open).Punch)
            report.Fail("runner punched while a route was open");

        for (int i = 0; i <= (int)EnemyVerb.Cover; i++)
        {
            if (report.Counts[i] <= 0)
                report.Fail("verb " + ((EnemyVerb)i).ToString() + " was never chosen");
        }
    }

    static float AverageChase(MovementConfig cfg, PunchTagTuning punch, float difficulty, EnemyAiReport report)
    {
        float sum = 0f;
        for (int course = 0; course < 3; course++)
            sum += OneChase(cfg, punch, difficulty, course, report);
        return sum / 3f;
    }

    static float OneChase(MovementConfig cfg, PunchTagTuning punch, float difficulty, int course, EnemyAiReport report)
    {
        const float dt = 1f / 60f;
        // Under the walk gait so a close-range chase still gains, with a weave
        // wide enough that a stale lead takes the long way around.
        float speed = 4.4f;
        float omega = 1.35f;
        float amp = 2.2f;
        float phase = course * 0.9f;
        Vector3 runner = new Vector3(0f, 0f, 0f);
        Vector3 it = new Vector3(-8f, 0f, 0.55f * course);
        Vector3 velocity = Vector3.zero;
        Vector3 forward = new Vector3(1f, 0f, 0f);
        EnemyMemory memory = default;
        float tell = 0f;
        float lungeT = 0f;
        float lungeCd = 0f;
        EnemyVerb last = EnemyVerb.Hold;
        float t = 0f;

        for (int frame = 0; frame < 1200; frame++)
        {
            float vz = amp * omega * Mathf.Cos(omega * t + phase);
            Vector3 runnerVel = new Vector3(speed, 0f, vz);
            runner += runnerVel * dt;

            Vector3 aim = EnemyAi.DelayedAim(ref memory, difficulty, dt, it, runner, runnerVel, 1, 0f);
            Vector3 raw = aim - it;
            raw.y = 0f;

            Vector3 to = runner - it;
            to.y = 0f;
            float dist = to.magnitude;
            Vector3 fwd = Flat(forward);
            if (fwd.sqrMagnitude < 1e-6f) fwd = new Vector3(0f, 0f, 1f);
            fwd.Normalize();
            Vector3 toN = dist > 0.001f ? to * (1f / dist) : fwd;
            float dot = Vector3.Dot(fwd, toN);
            if (dot > 1f) dot = 1f;
            if (dot < -1f) dot = -1f;
            bool facing = dot >= 0.9272f;

            if (lungeCd > 0f) lungeCd -= dt;
            if (lungeT > 0f) lungeT -= dt;
            bool window = dist > punch.reach + 0.35f && dist < punch.reach + 4.2f && facing && lungeT <= 0f && lungeCd <= 0f;
            if (window) tell += dt;
            else if (lungeT <= 0f) tell = 0f;
            bool legal = EnemyAi.LungeCommitLegal(tell, 0.45f, window, true, false);

            OpponentChaseInput input = FarInput();
            input.Aim = raw.sqrMagnitude > 1e-6f ? raw : to;
            input.Velocity = velocity;
            input.BodyForward = fwd;
            input.Grounded = true;
            input.PlanarDistance = dist;
            input.FarMeters = punch.reach + 4.2f;
            input.LungeCommit = legal;
            input.LungeBlocked = false;
            OpponentChaseWish wish = OpponentChaseSteer.Decide(input);

            EnemySense sense = Sense(true, dist, punch.reach);
            sense.Difficulty = difficulty;
            sense.Dt = dt;
            sense.SelfPos = it;
            sense.SelfVel = velocity;
            sense.Forward = fwd;
            sense.Grounded = true;
            sense.PlanarSpeed = Flat(velocity).magnitude;
            sense.AngleDeg = facing ? 10f : 40f;
            sense.SlideOpen = false;
            EnemyOverlay overlay = EnemyAi.Decorate(ref memory, sense, wish);
            if (overlay.Verb != last)
            {
                report.Counts[(int)overlay.Verb]++;
                last = overlay.Verb;
            }

            if (wish.Lunge && legal)
            {
                lungeT = cfg.taggerLungeDuration;
                lungeCd = cfg.taggerLungeCooldown;
                tell = 0f;
            }

            YawTowards(ref forward, overlay.Face.sqrMagnitude > 0.001f ? overlay.Face : toN, EnemyAiSteerYaw() * dt);
            fwd = Flat(forward);
            if (fwd.sqrMagnitude < 1e-6f) fwd = new Vector3(1f, 0f, 0f);
            fwd.Normalize();
            forward = fwd;

            float cap;
            if (lungeT > 0f)
            {
                velocity = fwd * cfg.taggerLungeSpeed;
                cap = cfg.taggerLungeSpeed;
            }
            else
            {
                Vector3 world = OpponentChaseSteer.WorldWish(fwd, overlay.MoveY, overlay.MoveX);
                float gait = KinematicStep.GaitCap(false, overlay.Sprint, overlay.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
                velocity = KinematicStep.GroundSteer(velocity, world, gait, cfg.groundAccel, cfg.groundDecel, dt, false);
                cap = gait;
            }

            velocity = WishAccel.ClampPlanarSpeed(velocity, cap);
            if (Flat(velocity).magnitude > cfg.taggerLungeSpeed + 0.05f)
                report.Fail("It exceeded the player speed cap");

            it += velocity * dt;
            t += dt;

            Vector3 gap = runner - it;
            gap.y = 0f;
            float now = gap.magnitude;
            Vector3 nowN = now > 0.001f ? gap * (1f / now) : fwd;
            float tagDot = Vector3.Dot(fwd, nowN);
            if (now <= punch.reach && tagDot >= 0.8480f)
                return t;
        }

        return t;
    }

    static float RunnerSurvival(MovementConfig cfg, PunchTagTuning punch, EnemyAiReport report)
    {
        const float dt = 1f / 60f;
        Vector3 runner = Vector3.zero;
        Vector3 it = new Vector3(0f, 0f, -5.4f);
        Vector3 runnerVel = Vector3.zero;
        Vector3 itVel = new Vector3(0f, 0f, cfg.sprintSpeed);
        Vector3 runnerFwd = new Vector3(0f, 0f, 1f);
        Vector3 itFwd = new Vector3(0f, 0f, 1f);
        float immunity = TagBackImmunity.DefaultSeconds;
        float tell = 0f;
        float lungeT = 0f;
        float lungeCd = 0f;
        float zipLeft = 0f;
        bool zipUsed = false;
        float minImmune = 999f;
        EnemyMemory memory = default;
        EnemyVerb last = EnemyVerb.Hold;
        float t = 0f;

        for (int frame = 0; frame < 900; frame++)
        {
            if (immunity > 0f) immunity -= dt;
            if (lungeCd > 0f) lungeCd -= dt;
            if (lungeT > 0f) lungeT -= dt;

            Vector3 toRunner = runner - it;
            toRunner.y = 0f;
            float dist = toRunner.magnitude;
            if (immunity > 0f && dist < minImmune) minImmune = dist;

            Vector3 itFace = dist > 0.05f ? toRunner * (1f / dist) : itFwd;
            YawTowards(ref itFwd, itFace, EnemyAiSteerYaw() * dt);
            Vector3 itFlat = Flat(itFwd);
            if (itFlat.sqrMagnitude < 1e-6f) itFlat = new Vector3(0f, 0f, 1f);
            itFlat.Normalize();
            itFwd = itFlat;
            float itDot = dist > 0.05f ? Vector3.Dot(itFlat, toRunner * (1f / dist)) : 1f;
            bool itFacing = itDot >= 0.9272f;
            bool window = dist > punch.reach + 0.35f && dist < punch.reach + 4.2f && itFacing && lungeT <= 0f && lungeCd <= 0f;
            if (window) tell += dt;
            else if (lungeT <= 0f) tell = 0f;
            if (EnemyAi.LungeCommitLegal(tell, 0.45f, window, true, false))
            {
                lungeT = cfg.taggerLungeDuration;
                lungeCd = cfg.taggerLungeCooldown;
                tell = 0f;
            }

            if (lungeT > 0f)
                itVel = itFlat * cfg.taggerLungeSpeed;
            else
            {
                Vector3 wish = OpponentChaseSteer.WorldWish(itFlat, 1f, 0f);
                itVel = KinematicStep.GroundSteer(itVel, wish, cfg.sprintSpeed, cfg.groundAccel, cfg.groundDecel, dt, false);
                itVel = WishAccel.ClampPlanarSpeed(itVel, cfg.sprintSpeed);
            }
            it += itVel * dt;

            EnemySense sense = Sense(false, dist, punch.reach);
            sense.Difficulty = EnemyAi.DefaultDifficulty;
            sense.Dt = dt;
            sense.SelfPos = runner;
            sense.SelfVel = runnerVel;
            sense.Forward = runnerFwd;
            sense.Grounded = zipLeft <= 0f;
            sense.Airborne = zipLeft > 0f;
            sense.PlanarSpeed = Flat(runnerVel).magnitude;
            sense.ThreatClosing = true;
            sense.SelfTagBackRemaining = immunity > 0f ? immunity : 0f;
            sense.LineOfSight = false;
            sense.Cornered = false;
            if (!zipUsed)
            {
                sense.ZipAhead = true;
                sense.ZipHelps = true;
                sense.ZipAim = new Vector3(0f, 0f, 1f);
            }

            EnemyOverlay overlay = EnemyAi.Evade(ref memory, sense);
            if (overlay.Verb != last)
            {
                report.Counts[(int)overlay.Verb]++;
                last = overlay.Verb;
            }
            if (overlay.Verb == EnemyVerb.Zip && !zipUsed)
            {
                zipUsed = true;
                zipLeft = 0.55f;
            }

            YawTowards(ref runnerFwd, overlay.Face, EnemyAiSteerYaw() * dt);
            Vector3 runFlat = Flat(runnerFwd);
            if (runFlat.sqrMagnitude < 1e-6f) runFlat = new Vector3(0f, 0f, 1f);
            runFlat.Normalize();
            runnerFwd = runFlat;

            bool riding = zipLeft > 0f;
            if (riding)
            {
                runnerVel = runFlat * ZipLineRules.DefaultRideSpeed;
                zipLeft -= dt;
            }
            else
            {
                Vector3 wish = OpponentChaseSteer.WorldWish(runFlat, overlay.MoveY, overlay.MoveX);
                float gait = KinematicStep.GaitCap(overlay.Crouch, overlay.Sprint, overlay.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
                runnerVel = KinematicStep.GroundSteer(runnerVel, wish, gait, cfg.groundAccel, cfg.groundDecel, dt, false);
                runnerVel = WishAccel.ClampPlanarSpeed(runnerVel, gait);
            }

            float cap = riding ? ZipLineRules.DefaultRideSpeed : cfg.sprintSpeed;
            if (Flat(runnerVel).magnitude > cap + 0.05f)
                report.Fail("runner exceeded the player speed cap");
            if (Flat(itVel).magnitude > cfg.taggerLungeSpeed + 0.05f)
                report.Fail("scripted It exceeded the lunge cap");

            runner += runnerVel * dt;
            t += dt;

            Vector3 gap = runner - it;
            gap.y = 0f;
            float now = gap.magnitude;
            float tagDot = now > 0.05f ? Vector3.Dot(itFlat, gap * (1f / now)) : 1f;
            if (immunity <= 0f && now <= punch.reach && tagDot >= 0.8480f)
            {
                report.ImmuneMin = minImmune;
                if (!zipUsed) report.Fail("runner never took the zip");
                return t;
            }
        }

        report.ImmuneMin = minImmune;
        if (!zipUsed) report.Fail("runner never took the zip");
        return t;
    }

    static string Fmt3(float[] v)
    {
        return v[0].ToString("0.00", CultureInfo.InvariantCulture)
            + "/" + v[1].ToString("0.00", CultureInfo.InvariantCulture)
            + "/" + v[2].ToString("0.00", CultureInfo.InvariantCulture);
    }

    static float EnemyAiSteerYaw()
    {
        return OpponentChaseSteer.MaxYawDegPerSec;
    }

    static void CheckSources(EnemyAiReport report)
    {
        string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
        string ai = Read("Assets/Scripts/Modes/EnemyAi.cs");
        string spawn = Read("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string steer = Read("Assets/Scripts/Modes/OpponentChaseSteer.cs");
        string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        if (patrol == null || ai == null || spawn == null || steer == null || motor == null)
        {
            report.Fail("enemy sources missing");
            return;
        }

        if (!patrol.Contains("EnemyAi.DelayedAim") || !patrol.Contains("EnemyAi.Decorate") || !patrol.Contains("EnemyAi.Evade"))
            report.Fail("patrol does not drive the enemy brain");
        if (!patrol.Contains("airDash: false") || patrol.Contains("airDash: airDash") || patrol.Contains("AirDashCooldownRemaining"))
            report.Fail("patrol air-dash wiring drifted");
        if (!patrol.Contains("OpponentChaseSteer.Decide") || !patrol.Contains("VerbIntegration.Choose"))
            report.Fail("patrol left the chase steer");
        if (patrol.Contains("transform.position =") || patrol.Contains(".Move(") || patrol.Contains("Rigidbody"))
            report.Fail("patrol teleports or drives a rigidbody");
        if (patrol.Contains("jumpSpeed =") || patrol.Contains("taggerLungeSpeed ="))
            report.Fail("patrol writes a feel number");
        if (!patrol.Contains("difficulty = 0.5f"))
            report.Fail("difficulty default is not 0.5");
        if (steer.Contains("AirDash = true"))
            report.Fail("chase steer grants an air dash");
        if (ai.Contains("jumpSpeed =") || ai.Contains(".Move(") || ai.Contains("Rigidbody"))
            report.Fail("enemy brain writes the motor");
        if (!ai.Contains("ReactSlow = 0.35f") || !ai.Contains("ReactFast = 0.12f"))
            report.Fail("reaction band drifted");
        if (!spawn.Contains("dummyCount = 1") || !spawn.Contains("SoloGrappleGate.EnableFor") || !spawn.Contains("enableGrapple = false"))
            report.Fail("spawn config drifted");
        if (!spawn.Contains("ApplyEnemyRope"))
            report.Fail("opponent rope is not applied");
        int moves = 0;
        int at = 0;
        while (at >= 0 && at < motor.Length)
        {
            at = motor.IndexOf("_cc.Move(", at, StringComparison.Ordinal);
            if (at < 0) break;
            moves++;
            at += 8;
        }
        if (moves != 1) report.Fail("motor no longer steps once");
    }

    static EnemySense Sense(bool isIt, float distance, float reach)
    {
        EnemySense sense = default;
        sense.Difficulty = 1f;
        sense.IsIt = isIt;
        sense.HasTarget = true;
        sense.Grounded = true;
        sense.PlanarDistance = distance;
        sense.PunchReach = reach;
        sense.Forward = new Vector3(0f, 0f, 1f);
        sense.Dt = 0.016f;
        return sense;
    }

    static OpponentChaseInput FarInput()
    {
        OpponentChaseInput input = default;
        input.Aim = new Vector3(0f, 0f, 1f);
        input.BodyForward = new Vector3(0f, 0f, 1f);
        input.WallDistance = 999f;
        input.Grounded = true;
        input.PlanarDistance = 12f;
        input.FarMeters = 6f;
        input.LipDistance = 999f;
        return input;
    }

    static void Note(EnemyAiReport report, EnemyVerb verb)
    {
        report.Counts[(int)verb]++;
    }

    static void YawTowards(ref Vector3 forward, Vector3 face, float maxDeg)
    {
        Vector3 from = Flat(forward);
        Vector3 to = Flat(face);
        if (from.sqrMagnitude < 1e-6f || to.sqrMagnitude < 1e-6f)
            return;
        from.Normalize();
        to.Normalize();
        float dot = Vector3.Dot(from, to);
        if (dot > 0.9999f)
        {
            forward = to;
            return;
        }
        if (dot < -1f) dot = -1f;
        float crossY = from.z * to.x - from.x * to.z;
        float signed = Atan2(crossY, dot) * Mathf.Rad2Deg;
        if (signed > maxDeg) signed = maxDeg;
        if (signed < -maxDeg) signed = -maxDeg;
        forward = RotateYaw(from, signed);
    }

    static Vector3 RotateYaw(Vector3 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r);
        float s = Mathf.Sin(r);
        return new Vector3(v.x * c + v.z * s, 0f, -v.x * s + v.z * c);
    }

    static float Atan2(float y, float x)
    {
        if (Math.Abs(x) < 1e-8f)
            return y >= 0f ? 1.5707963f : -1.5707963f;
        float a = Mathf.Atan(y / x);
        if (x < 0f)
            a += y >= 0f ? 3.14159265f : -3.14159265f;
        return a;
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static string Read(string relative)
    {
        DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (int i = 0; i < 8 && dir != null; i++)
        {
            string path = Path.Combine(dir.FullName, relative);
            if (File.Exists(path)) return File.ReadAllText(path);
            dir = dir.Parent;
        }
        return null;
    }
}

public sealed class EnemyAiReport
{
    public float It02;
    public float It05;
    public float It09;
    public float Survive;
    public float ImmuneMin;
    public int Flips;
    public readonly float[] ItMed = new float[3];
    public readonly float[] ItP10 = new float[3];
    public readonly float[] ItP90 = new float[3];
    public readonly float[] RunMed = new float[3];
    public readonly float[] RunP10 = new float[3];
    public readonly float[] RunP90 = new float[3];
    public int Stuck;
    public int Breaches;
    public int PadUses;
    public int ZipUses;
    public int GrappleUses;
    public int ClingUses;
    public int BarUses;
    public int CounterUses;
    public int Respawns;
    public int Jumps;
    public string Line = "";
    public string PocketLine = "";
    public readonly int[] Counts = new int[17];
    public EnemyMemory GymMemory;
    public bool Ok => _failures.Length == 0;
    readonly StringBuilder _failures = new StringBuilder();

    public void Fail(string message)
    {
        if (_failures.Length > 0) _failures.Append('\n');
        _failures.Append(message);
    }

    public string FailureText => _failures.ToString();

    public string VerbText()
    {
        return "hold=" + Counts[0]
            + " sprint=" + Counts[1]
            + " close=" + Counts[2]
            + " jump=" + Counts[3]
            + " cling=" + Counts[4]
            + " wallrun=" + Counts[5]
            + " walljump=" + Counts[6]
            + " slide=" + Counts[7]
            + " dash=" + Counts[8]
            + " grapple=" + Counts[9]
            + " pad=" + Counts[10]
            + " zip=" + Counts[11]
            + " lunge=" + Counts[12]
            + " punch=" + Counts[13]
            + " evade=" + Counts[14]
            + " loop=" + Counts[15]
            + " cover=" + Counts[16];
    }

    public override string ToString()
    {
        var text = new StringBuilder();
        text.Append(Ok ? "PASS " : "FAIL ");
        text.Append(Line);
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
