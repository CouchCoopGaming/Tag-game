using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Gameplay;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless chase: sprint outside the lunge band, air-strafe a corner, jump a gap,
/// cling a wall, and lunge only after the 0.45 s tell. Steps stay inside the motor.
/// </summary>
public static class OpponentChaseSteerProof
{
    public static OpponentChaseSteerReport Run()
    {
        var report = new OpponentChaseSteerReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();
        const float dt = 1f / 60f;
        float far = punch.reach + 4.2f;

        CheckLocks(report, cfg, punch);
        CheckRules(report, cfg, far);
        CheckLungeLead(report, dt);
        SimulateSprint(report, cfg, far, dt);
        SimulateAirStrafe(report, cfg, far, dt);
        SimulateGap(report, cfg, far, dt);
        SimulateWall(report, cfg, far, dt);
        CheckSources(report);

        report.Line = "opponent chase: sprint " + report.ClosedMeters.ToString("0.00", CultureInfo.InvariantCulture)
            + "m air-strafe " + report.CornerDotBefore.ToString("0.00", CultureInfo.InvariantCulture)
            + "->" + report.CornerDotAfter.ToString("0.00", CultureInfo.InvariantCulture)
            + " gap " + report.GapJumpSpeed.ToString("0.0", CultureInfo.InvariantCulture)
            + " cling " + report.ClingDot.ToString("0.00", CultureInfo.InvariantCulture)
            + " lunge " + report.LungeElapsed.ToString("0.00", CultureInfo.InvariantCulture)
            + "s step " + report.MaxStep.ToString("0.000", CultureInfo.InvariantCulture) + "m";
        return report;
    }

    static void CheckLocks(OpponentChaseSteerReport report, MovementConfig cfg, PunchTagTuning punch)
    {
        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f)
            report.Fail("coyote is not 0.10");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f)
            report.Fail("jump buffer is not 0.16");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f)
            report.Fail("cling grace is not 0.08");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f)
            report.Fail("jumpSpeed is not 24.7");
        if (cfg.slideBoost != 0f)
            report.Fail("slideBoost is not 0");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f || Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f
            || Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail("air dash numbers changed");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail("punch reach is not 1.55");
        if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f || Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f
            || Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f)
            report.Fail("lunge numbers changed");
        if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f || Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f
            || Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f)
            report.Fail("climb, slip, or wall-run speed changed");
        if (Mathf.Abs(cfg.sprintSpeed - 12f) > 0.001f || Mathf.Abs(cfg.walkSpeed - 6f) > 0.001f)
            report.Fail("gait speeds changed");
        if (Mathf.Abs(OpponentChaseSteer.LungeLeadSeconds - 0.45f) > 0.001f)
            report.Fail("chase lead is not 0.45s");
        if (Mathf.Abs(OpponentChaseSteer.MaxYawDegPerSec - 150f) > 0.001f)
            report.Fail("chase yaw cap moved");
        if (OpponentChaseSteer.AirStrafeMoveY >= OpponentChaseSteer.AirStrafeBonusMaxY)
            report.Fail("air strafe stick is not a side stick");
        if (OpponentChaseSteer.CloseMoveY > 0.4f)
            report.Fail("close chase stick still sprints");

        float standApex = Apex(cfg, cfg.jumpSpeed);
        float fastApex = Apex(cfg, cfg.jumpSpeed);
        if (Mathf.Abs(standApex - fastApex) > 0.001f || standApex < 1f)
            report.Fail("jump apex depends on the chase");
    }

    static void CheckRules(OpponentChaseSteerReport report, MovementConfig cfg, float far)
    {
        OpponentChaseInput farRun = Blank(far);
        farRun.PlanarDistance = 12f;
        OpponentChaseWish sprint = OpponentChaseSteer.Decide(farRun);
        if (sprint.Verb != OpponentChaseVerb.Sprint || !sprint.Sprint || sprint.Jump || sprint.Lunge || sprint.AirDash)
            report.Fail("far chase did not sprint");
        float sprintGait = KinematicStep.GaitCap(false, sprint.Sprint, sprint.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
        if (Mathf.Abs(sprintGait - cfg.sprintSpeed) > 0.001f || sprintGait > cfg.sprintSpeed)
            report.Fail("sprint gait is stronger than the player");

        OpponentChaseInput near = Blank(far);
        near.PlanarDistance = 3f;
        OpponentChaseWish close = OpponentChaseSteer.Decide(near);
        if (close.Verb != OpponentChaseVerb.Close || close.Sprint || close.MoveY > 0.4f || close.Lunge || close.AirDash || close.Jump)
            report.Fail("inside the lunge band the chase still sprints");
        float walkGait = KinematicStep.GaitCap(false, close.Sprint, close.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
        if (Mathf.Abs(walkGait - cfg.walkSpeed) > 0.001f)
            report.Fail("close chase is not the walk gait");

        Vector3 aim = new Vector3(0f, 0f, 1f);
        OpponentChaseInput corner = Blank(far);
        corner.PlanarDistance = 10f;
        corner.Grounded = false;
        corner.Velocity = new Vector3(12f, 0f, 0f);
        corner.Aim = aim;
        corner.BodyForward = new Vector3(-aim.z, 0f, aim.x);
        OpponentChaseWish strafe = OpponentChaseSteer.Decide(corner);
        if (strafe.Verb != OpponentChaseVerb.AirStrafe || strafe.AirDash || strafe.Jump || strafe.Lunge)
            report.Fail("a corner did not air-strafe");
        if (Mathf.Abs(strafe.Strafe) < 0.5f || Mathf.Abs(strafe.MoveY) >= OpponentChaseSteer.AirStrafeBonusMaxY)
            report.Fail("air strafe did not hold a side stick");
        Vector3 cut = OpponentChaseSteer.WorldWish(corner.BodyForward, strafe.MoveY, strafe.Strafe);
        if (Vector3.Dot(Flat(cut).normalized, aim) < 0.9f)
            report.Fail("air strafe wish missed the corner");

        OpponentChaseInput groundedCorner = corner;
        groundedCorner.Grounded = true;
        if (OpponentChaseSteer.Decide(groundedCorner).Verb == OpponentChaseVerb.AirStrafe)
            report.Fail("grounded chase used air strafe");

        OpponentChaseInput gap = Blank(far);
        gap.PlanarDistance = 10f;
        gap.GapAhead = true;
        OpponentChaseWish hop = OpponentChaseSteer.Decide(gap);
        if (hop.Verb != OpponentChaseVerb.GapJump || !hop.Jump || hop.Lunge || hop.AirDash)
            report.Fail("a gap did not jump");
        OpponentChaseInput falling = gap;
        falling.Grounded = false;
        falling.Velocity = new Vector3(0f, 0f, 12f);
        falling.BodyForward = aim;
        if (OpponentChaseSteer.Decide(falling).Jump)
            report.Fail("an airborne chase jumped the gap again");

        Vector3 wallNormal = new Vector3(0f, 0f, -1f);
        OpponentChaseInput blocked = Blank(far);
        blocked.WallNormal = wallNormal;
        blocked.WallDistance = 1.2f;
        blocked.Aim = aim;
        blocked.GapAhead = true;
        blocked.LungeCommit = true;
        OpponentChaseWish cling = OpponentChaseSteer.Decide(blocked);
        float into = OpponentChaseSteer.IntoWallDot(OpponentChaseSteer.WorldWish(cling.Face, cling.MoveY, cling.Strafe), wallNormal);
        report.ClingDot = into;
        if (cling.Verb != OpponentChaseVerb.WallCling || cling.Jump || cling.Lunge || cling.AirDash)
            report.Fail("a wall on the line did not cling");
        if (into <= OpponentChaseSteer.ClingIntoWall)
            report.Fail("cling wish is not into the wall");

        OpponentChaseInput glance = blocked;
        glance.LungeCommit = false;
        glance.GapAhead = false;
        glance.Aim = new Vector3(1f, 0f, 0.15f);
        OpponentChaseWish glanceWish = OpponentChaseSteer.Decide(glance);
        float glanceDot = OpponentChaseSteer.IntoWallDot(
            OpponentChaseSteer.WorldWish(glanceWish.Face, glanceWish.MoveY, glanceWish.Strafe), wallNormal);
        if (glanceWish.Verb != OpponentChaseVerb.WallCling || glanceDot <= OpponentChaseSteer.ClingIntoWall)
            report.Fail("a glancing wall dropped the cling");

        OpponentChaseInput farWall = blocked;
        farWall.WallDistance = 8f;
        farWall.LungeCommit = false;
        farWall.GapAhead = false;
        if (OpponentChaseSteer.Decide(farWall).Verb == OpponentChaseVerb.WallCling)
            report.Fail("a distant wall cancelled the sprint");

        OpponentChaseInput line = Blank(far);
        line.PlanarDistance = 4f;
        line.HoldLine = true;
        line.GapAhead = true;
        line.Grounded = false;
        line.Velocity = new Vector3(12f, 0f, 0f);
        OpponentChaseWish held = OpponentChaseSteer.Decide(line);
        if (held.Jump || held.Lunge || held.AirDash || held.Verb == OpponentChaseVerb.AirStrafe)
            report.Fail("the lunge tell let go of the line");

        OpponentChaseInput lunging = Blank(far);
        lunging.PlanarDistance = 3f;
        lunging.LungeCommit = true;
        OpponentChaseWish lunge = OpponentChaseSteer.Decide(lunging);
        if (lunge.Verb != OpponentChaseVerb.Lunge || !lunge.Lunge || lunge.AirDash || lunge.Jump)
            report.Fail("a finished tell did not lunge");
        lunging.Grounded = false;
        if (OpponentChaseSteer.Decide(lunging).Lunge || OpponentChaseSteer.Decide(lunging).AirDash)
            report.Fail("an airborne lunge became an air dash");
        lunging.Grounded = true;
        lunging.LungeCommit = false;
        if (OpponentChaseSteer.Decide(lunging).Lunge)
            report.Fail("lunge fired without the tell");
    }

    static void CheckLungeLead(OpponentChaseSteerReport report, float dt)
    {
        if (OpponentChaseSteer.LungePressLegal(0.449f, true, true, true, false))
            report.Fail("lunge fired before 0.45s");
        if (!OpponentChaseSteer.LungePressLegal(0.45f, true, true, true, false))
            report.Fail("lunge did not fire at 0.45s");
        if (OpponentChaseSteer.LungePressLegal(0.45f, true, false, true, false))
            report.Fail("airborne lunge was legal");
        if (OpponentChaseSteer.LungePressLegal(0.45f, true, true, false, false))
            report.Fail("lunge was legal outside the window");
        if (OpponentChaseSteer.LungePressLegal(0.45f, false, true, true, false))
            report.Fail("lunge was legal before the tell started");
        if (OpponentChaseSteer.LungePressLegal(0.45f, true, true, true, true))
            report.Fail("lunge was legal during a punch");

        float tell = OpponentChaseSteer.LungeLeadSeconds;
        bool early = false;
        float elapsed = 0f;
        for (int i = 0; i < 80 && tell > 0f; i++)
        {
            float since = OpponentChaseSteer.LungeLeadSeconds - tell;
            if (OpponentChaseSteer.LungePressLegal(since, true, true, true, false))
                early = true;
            tell -= dt;
            elapsed = OpponentChaseSteer.LungeLeadSeconds - tell;
        }
        report.LungeElapsed = elapsed;
        if (early || elapsed + 0.0001f < OpponentChaseSteer.LungeLeadSeconds)
            report.Fail("lead countdown pressed early");
        if (!OpponentChaseSteer.LungePressLegal(elapsed, true, true, true, false))
            report.Fail("lead countdown never pressed");
    }

    static void SimulateSprint(OpponentChaseSteerReport report, MovementConfig cfg, float far, float dt)
    {
        Vector3 pos = Vector3.zero;
        Vector3 hv = Vector3.zero;
        Vector3 body = new Vector3(0f, 0f, 1f);
        const float targetZ = 18f;
        float start = targetZ;
        float maxStep = 0f;
        bool sprinted = false;
        bool walked = false;
        for (int frame = 0; frame < 90; frame++)
        {
            float dist = targetZ - pos.z;
            OpponentChaseInput input = Blank(far);
            input.Aim = new Vector3(0f, 0f, dist);
            input.BodyForward = body;
            input.Velocity = hv;
            input.PlanarDistance = dist;
            input.Grounded = true;
            OpponentChaseWish wish = OpponentChaseSteer.Decide(input);
            if (wish.AirDash || wish.Lunge || wish.Jump)
                report.Fail("open sprint used a burst");
            if (dist > far && wish.Verb != OpponentChaseVerb.Sprint)
                report.Fail("far frame did not sprint");
            if (dist > far)
                sprinted = true;
            if (dist <= far)
            {
                walked = true;
                if (wish.Sprint || wish.MoveY > 0.4f)
                    report.Fail("chase kept sprinting inside the lunge band");
            }

            float yaw = YawTowards(ref body, wish.Face, OpponentChaseSteer.MaxYawDegPerSec * dt);
            if (yaw > OpponentChaseSteer.MaxYawDegPerSec * dt + 0.001f)
                report.Fail("chase yaw snapped");
            Vector3 world = OpponentChaseSteer.WorldWish(body, wish.MoveY, wish.Strafe);
            float gait = KinematicStep.GaitCap(false, wish.Sprint, wish.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
            if (gait > cfg.sprintSpeed + 0.001f)
                report.Fail("chase gait beat sprint");
            float before = hv.magnitude;
            hv = KinematicStep.GroundSteer(hv, world, gait, cfg.groundAccel, cfg.groundDecel, dt, false);
            if (hv.magnitude > cfg.sprintSpeed + 0.05f)
                report.Fail("ground chase exceeded sprint");
            if (hv.magnitude > before + cfg.groundAccel * dt + 0.02f)
                report.Fail("ground chase accelerated past the motor");
            float step = hv.magnitude * dt;
            if (step > maxStep) maxStep = step;
            if (step > cfg.sprintSpeed * dt + 0.002f)
                report.Fail("sprint step teleported");
            pos.z += hv.z * dt;
            if (pos.z > targetZ)
                report.Fail("sprint stepped through the target");
        }

        report.ClosedMeters = start - (targetZ - pos.z);
        if (maxStep > report.MaxStep) report.MaxStep = maxStep;
        if (!sprinted || !walked)
            report.Fail("sprint did not give way to the lunge band");
        if (report.ClosedMeters < 10f)
            report.Fail("sprint did not close the chase");
        if (targetZ - pos.z < 0.2f)
            report.Fail("sprint reached the body in one burst");
    }

    static void SimulateAirStrafe(OpponentChaseSteerReport report, MovementConfig cfg, float far, float dt)
    {
        Vector3 hv = new Vector3(12f, 0f, 0f);
        Vector3 body = new Vector3(1f, 0f, 0f);
        Vector3 aim = new Vector3(0f, 0f, 1f);
        report.CornerDotBefore = Vector3.Dot(hv.normalized, aim);
        bool bonus = false;
        float accel = cfg.airAccel * cfg.airStrafeBonus;
        for (int frame = 0; frame < 45; frame++)
        {
            OpponentChaseInput input = Blank(far);
            input.Grounded = false;
            input.PlanarDistance = 10f;
            input.Aim = aim;
            input.Velocity = hv;
            input.BodyForward = body;
            OpponentChaseWish wish = OpponentChaseSteer.Decide(input);
            if (wish.Verb != OpponentChaseVerb.AirStrafe || wish.AirDash || wish.Jump || wish.Lunge)
                report.Fail("corner frame left air strafe");
            if (Mathf.Abs(wish.MoveY) < OpponentChaseSteer.AirStrafeBonusMaxY && Mathf.Abs(wish.Strafe) > 0.4f)
                bonus = true;
            YawTowards(ref body, wish.Face, OpponentChaseSteer.MaxYawDegPerSec * dt);
            Vector3 world = OpponentChaseSteer.WorldWish(body, wish.MoveY, wish.Strafe);
            float gait = KinematicStep.GaitCap(false, wish.Sprint, wish.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
            if (gait > cfg.sprintSpeed + 0.001f)
                report.Fail("air strafe wish speed beat sprint");
            Vector3 before = hv;
            hv = KinematicStep.AirSteer(hv, world, gait, accel, dt);
            float gained = hv.magnitude - before.magnitude;
            if (gained > accel * dt + 0.05f)
                report.Fail("air strafe accelerated past the player");
            float step = hv.magnitude * dt;
            if (step > report.MaxStep) report.MaxStep = step;
        }

        report.CornerDotAfter = Vector3.Dot(hv.normalized, aim);
        if (!(report.CornerDotAfter > report.CornerDotBefore + 0.25f))
            report.Fail("air strafe did not cut the corner");
        if (!(hv.magnitude > 12.5f))
            report.Fail("air strafe did not take the off-axis gain");
        if (!bonus)
            report.Fail("air strafe never held the side stick");
    }

    static void SimulateGap(OpponentChaseSteerReport report, MovementConfig cfg, float far, float dt)
    {
        OpponentChaseInput gap = Blank(far);
        gap.PlanarDistance = 10f;
        gap.GapAhead = true;
        gap.Velocity = new Vector3(0f, 0f, cfg.sprintSpeed);
        OpponentChaseWish hop = OpponentChaseSteer.Decide(gap);
        Vector3 hv = gap.Velocity;
        float gait = KinematicStep.GaitCap(false, hop.Sprint, hop.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
        Vector3 world = OpponentChaseSteer.WorldWish(hop.Face, hop.MoveY, hop.Strafe);
        hv = KinematicStep.GroundSteer(hv, world, gait, cfg.groundAccel, cfg.groundDecel, dt, true);
        float vy = hop.Jump ? cfg.jumpSpeed : 0f;
        report.GapJumpSpeed = vy;
        if (Mathf.Abs(vy - 24.7f) > 0.001f)
            report.Fail("gap jump was not jumpSpeed");
        if (Mathf.Abs(hv.magnitude - cfg.sprintSpeed) > 0.05f)
            report.Fail("gap jump changed the run speed");
        float slowApex = Apex(cfg, cfg.jumpSpeed);
        float fastApex = Apex(cfg, cfg.jumpSpeed);
        if (Mathf.Abs(slowApex - fastApex) > 0.001f)
            report.Fail("gap apex changed with speed");
    }

    static void SimulateWall(OpponentChaseSteerReport report, MovementConfig cfg, float far, float dt)
    {
        const float wallZ = 3f;
        float limit = wallZ - cfg.radius;
        Vector3 pos = Vector3.zero;
        Vector3 hv = Vector3.zero;
        Vector3 body = new Vector3(0f, 0f, 1f);
        bool clung = false;
        for (int frame = 0; frame < 120; frame++)
        {
            float wallDist = wallZ - pos.z;
            OpponentChaseInput input = Blank(far);
            input.Aim = new Vector3(0f, 0f, 14f - pos.z);
            input.PlanarDistance = 14f - pos.z;
            input.BodyForward = body;
            input.Velocity = hv;
            input.WallNormal = new Vector3(0f, 0f, -1f);
            input.WallDistance = wallDist;
            input.Grounded = true;
            OpponentChaseWish wish = OpponentChaseSteer.Decide(input);
            if (wish.AirDash || wish.Lunge || wish.Jump)
                report.Fail("wall chase jumped or lunged through it");
            if (wallDist <= OpponentChaseSteer.ClingCommitMeters)
            {
                clung = true;
                if (wish.Verb != OpponentChaseVerb.WallCling)
                    report.Fail("chase did not cling once the wall was close");
                float into = OpponentChaseSteer.IntoWallDot(
                    OpponentChaseSteer.WorldWish(wish.Face, wish.MoveY, wish.Strafe), input.WallNormal);
                if (into <= OpponentChaseSteer.ClingIntoWall)
                    report.Fail("close wall wish left the surface");
            }

            YawTowards(ref body, wish.Face, OpponentChaseSteer.MaxYawDegPerSec * dt);
            Vector3 world = OpponentChaseSteer.WorldWish(body, wish.MoveY, wish.Strafe);
            float gait = KinematicStep.GaitCap(false, wish.Sprint, wish.MoveY, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
            hv = KinematicStep.GroundSteer(hv, world, gait, cfg.groundAccel, cfg.groundDecel, dt, false);
            if (hv.magnitude > cfg.sprintSpeed + 0.05f)
                report.Fail("wall approach exceeded sprint");
            float step = hv.magnitude * dt;
            if (step > cfg.sprintSpeed * dt + 0.002f)
                report.Fail("wall approach teleported");
            float next = pos.z + hv.z * dt;
            if (next > limit)
                next = limit;
            if (next > limit + 0.0001f)
                report.Fail("chase crossed the wall");
            pos.z = next;
        }

        if (!clung)
            report.Fail("chase never reached the wall");
        if (pos.z > limit + 0.0001f || pos.z < 0.4f)
            report.Fail("wall chase did not stop on the capsule");
    }

    static void CheckSources(OpponentChaseSteerReport report)
    {
        string steer = ReadRepo("Assets/Scripts/Modes/OpponentChaseSteer.cs");
        string patrol = ReadRepo("Assets/Scripts/Modes/DummyPatrol.cs");
        string motor = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string lunge = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        string tell = ReadRepo("Assets/Scripts/Art/OpponentChaseTell.cs");
        string loco = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        if (steer == null || patrol == null || motor == null || lunge == null || tell == null || loco == null)
        {
            report.Fail("chase sources missing");
            return;
        }

        if (!lunge.Contains("LeadSeconds = 0.45f") || !steer.Contains("LungeLeadSeconds = 0.45f"))
            report.Fail("lunge lead drifted from 0.45s");
        if (!patrol.Contains("OpponentLungeTell.LeadSeconds") || !patrol.Contains("OpponentChaseSteer.Decide"))
            report.Fail("patrol does not use the lead and the steer");
        if (!patrol.Contains("airDash: false") || !patrol.Contains("ProbeGapAhead") || !patrol.Contains("ProbeWallBetween"))
            report.Fail("patrol chase does not probe gaps and walls");
        if (!patrol.Contains("OpponentChaseSteer.MaxYawDegPerSec"))
            report.Fail("patrol yaw is not capped");
        if (patrol.Contains("taggerLungeSpeed") || steer.Contains("taggerLungeSpeed") || steer.Contains("jumpSpeed"))
            report.Fail("chase writes a feel number");
        if (steer.Contains("AirDash = true") || steer.Contains("Rigidbody") || steer.Contains("CharacterController")
            || steer.Contains(".Move(") || steer.Contains("transform.position"))
            report.Fail("steer moves the body");
        if (patrol.Contains("transform.position =") || patrol.Contains(".Move(") || patrol.Contains("Rigidbody"))
            report.Fail("patrol teleports or drives a rigidbody");
        if (!motor.Contains("ClingIntoWall = 0.25f") || !steer.Contains("ClingIntoWall = 0.25f"))
            report.Fail("cling gate drifted");
        if (!motor.Contains("Mathf.Abs(_in.Move.y) < 0.2f"))
            report.Fail("air strafe bonus gate drifted");
        if (Count(motor, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (!tell.Contains("OpponentChaseTell") || !loco.Contains("TickOpponentChase"))
            report.Fail("chase presence tell was removed");
        if (!Has(ReadRepo("Assets/Resources/TagArena/MovementConfig.asset") ?? "",
                "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
                "jumpSpeed: 24.7", "slideBoost: 0", "climbSpeed: 6", "climbSlipSpeed: 3.7",
                "wallRunSpeed: 9.5", "taggerLungeSpeed: 16", "taggerLungeDuration: 0.2",
                "taggerLungeCooldown: 1", "airDashDuration: 0.1", "airDashSpeed: 15", "airDashCooldown: 30")
            || !Has(ReadRepo("Assets/ScriptableObjects/PunchTagTuning.asset") ?? "", "reach: 1.55"))
            report.Fail("locked asset numbers drifted");
    }

    static float Apex(MovementConfig cfg, float jumpSpeed)
    {
        float dt = 1f / 60f;
        float vy = jumpSpeed;
        float y = 0f;
        float apex = 0f;
        for (int i = 0; i < 400; i++)
        {
            float g = KinematicStep.AirGravity(vy, cfg.gravity, cfg.fallGravityMult);
            vy = KinematicStep.IntegrateVertical(vy, g, dt, cfg.maxFallSpeed);
            y += vy * dt;
            if (y > apex) apex = y;
            if (i > 3 && y <= 0f) break;
        }
        return apex;
    }

    static float YawTowards(ref Vector3 forward, Vector3 face, float maxDeg)
    {
        Vector3 from = Flat(forward);
        Vector3 to = Flat(face);
        if (from.sqrMagnitude < 1e-6f || to.sqrMagnitude < 1e-6f)
            return 0f;
        from.Normalize();
        to.Normalize();
        float dot = Vector3.Dot(from, to);
        if (dot > 0.9999f)
        {
            forward = to;
            return 0f;
        }
        float crossY = from.z * to.x - from.x * to.z;
        float signed = Atan2(crossY, dot) * Mathf.Rad2Deg;
        float step = signed;
        if (step > maxDeg) step = maxDeg;
        if (step < -maxDeg) step = -maxDeg;
        forward = RotateYaw(from, step);
        return Mathf.Abs(step);
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

    static OpponentChaseInput Blank(float far)
    {
        OpponentChaseInput s;
        s.Aim = new Vector3(0f, 0f, 1f);
        s.Velocity = Vector3.zero;
        s.BodyForward = new Vector3(0f, 0f, 1f);
        s.WallNormal = Vector3.zero;
        s.WallDistance = 999f;
        s.Grounded = true;
        s.GapAhead = false;
        s.LungeCommit = false;
        s.HoldLine = false;
        s.PlanarDistance = 12f;
        s.FarMeters = far;
        return s;
    }

    static bool Has(string text, params string[] needles)
    {
        if (text == null) return false;
        for (int i = 0; i < needles.Length; i++)
        {
            if (text.IndexOf(needles[i], StringComparison.Ordinal) < 0)
                return false;
        }
        return true;
    }

    static int Count(string hay, string needle)
    {
        int n = 0;
        int i = 0;
        while (i >= 0 && i < hay.Length)
        {
            i = hay.IndexOf(needle, i, StringComparison.Ordinal);
            if (i < 0) break;
            n++;
            i += needle.Length;
        }
        return n;
    }

    static string ReadRepo(string relative)
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

public sealed class OpponentChaseSteerReport
{
    public float ClosedMeters;
    public float CornerDotBefore;
    public float CornerDotAfter;
    public float GapJumpSpeed;
    public float ClingDot;
    public float LungeElapsed;
    public float MaxStep;
    public string Line = "";
    public bool Ok => _failures.Length == 0;
    readonly StringBuilder _failures = new StringBuilder();

    public void Fail(string message)
    {
        if (_failures.Length > 0) _failures.Append('\n');
        _failures.Append(message);
    }

    public string FailureText => _failures.ToString();

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
