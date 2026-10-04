using System.Globalization;
using System.Text;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// 60 Hz flat-ground step of the kinematic air steer. Same helpers PlayerMotor calls.
/// Takeoff frame is ground steer then the jump impulse (no air gravity). Later frames
/// are AirGravity, IntegrateVertical, and AirSteer. Non-ski locomotion has no planar cap.
/// </summary>
public static class StrafeJumpProof
{
    public static StrafeJumpReport Run60()
    {
        const float dt = 1f / 60f;
        var report = new StrafeJumpReport();

        Arc walk = Simulate(6f, new Vector2(0f, 0.4f), false, dt);
        Arc sprint = Simulate(12f, new Vector2(0f, 1f), false, dt);
        Arc stand = Simulate(0f, Vector2.zero, false, dt);
        Arc strafe = Simulate(12f, new Vector2(1f, 1f), true, dt);

        report.WalkDistance = walk.Distance;
        report.SprintDistance = sprint.Distance;
        report.StandDistance = stand.Distance;
        report.WalkApex = walk.Apex;
        report.SprintApex = sprint.Apex;
        report.StrafeApex = strafe.Apex;
        report.StrafeBefore = strafe.TakeoffSpeed;
        report.StrafeAfter = strafe.AfterHop;

        float ratio = walk.Distance > 0.001f ? sprint.Distance / walk.Distance : 0f;
        report.DistanceRatio = ratio;
        if (ratio < 1.9f || ratio > 2.1f)
            report.Fail("sprint jump distance should be about 2x walk (got " + ratio.ToString("0.000", CultureInfo.InvariantCulture) + ")");
        if (stand.Distance > 0.05f)
            report.Fail("standing jump with no steer should stay near 0 m (got " + stand.Distance.ToString("0.000", CultureInfo.InvariantCulture) + ")");
        if (Mathf.Abs(walk.LandSpeed - 6f) > 0.05f)
            report.Fail("straight walk jump changed horizontal speed");
        if (Mathf.Abs(sprint.LandSpeed - 12f) > 0.05f)
            report.Fail("straight sprint jump changed horizontal speed");

        if (Mathf.Abs(walk.Apex - sprint.Apex) > 0.001f || Mathf.Abs(walk.Apex - strafe.Apex) > 0.001f)
            report.Fail("apex changed with horizontal speed");

        if (!(strafe.TakeoffSpeed <= 12.05f))
            report.Fail("strafe takeoff was already above sprint");
        if (!(strafe.AfterHop > 16.8f))
            report.Fail("air strafe did not pass the old 16.8 planar cap");
        if (!(KinematicStep.LocomotionPlanarCap(24f, false) > 16.8f))
            report.Fail("non-ski planar cap still stops a strafe at 16.8");
        if (Mathf.Abs(KinematicStep.LocomotionPlanarCap(24f, true) - 24f) > 0.001f)
            report.Fail("ski planar cap changed");
        if (Mathf.Abs(strafe.AfterHop - strafe.LandSpeed) > 0.02f)
            report.Fail("landing hop did not keep the air speed");
        if (!(strafe.AfterNoHop < strafe.LandSpeed - 0.4f))
            report.Fail("staying on the ground did not apply friction");

        Vector3 side = KinematicStep.AirSteer(new Vector3(0f, 0f, 12f), new Vector3(1f, 0f, 0f), 12f, 30f, dt);
        Vector3 sideFast = KinematicStep.AirSteer(new Vector3(0f, 0f, 12f), new Vector3(1f, 0f, 0f), 12f, 30f, 1f / 144f);
        if (Mathf.Abs(side.x - 30f * dt) > 0.001f)
            report.Fail("air accel at 60 Hz was not accel*dt");
        if (Mathf.Abs(sideFast.x - 30f / 144f) > 0.001f)
            report.Fail("air accel at 144 Hz was not accel*dt");
        if (!(side.magnitude > 12f))
            report.Fail("off-axis wish did not raise total speed");

        Coyote(dt, out bool fired, out bool missedNext, out float elapsed);
        report.CoyoteFiredAtWindow = fired;
        report.CoyoteMissedNextFrame = missedNext;
        report.CoyoteElapsed = elapsed;
        if (!fired)
            report.Fail("coyote did not fire at 0.10s");
        if (!(fired && KinematicStep.JumpEdge(true, true, false, false) && KinematicStep.JumpEdge(true, false, false, false)))
            report.Fail("coyote space press did not call the gamepad jump");
        if (!missedNext)
            report.Fail("coyote did not miss the frame after 0.10s");
        if (Mathf.Abs(elapsed - 0.10f) > 0.0001f)
            report.Fail("coyote window frame was not 0.10s");

        // Space and the gamepad button are one edge. The vertical impulse stays jumpSpeed.
        if (!KinematicStep.JumpEdge(true, true, false, false))
            report.Fail("grounded space press did not call jump");
        if (!KinematicStep.JumpEdge(true, false, false, false))
            report.Fail("grounded gamepad press did not call jump");
        if (KinematicStep.JumpEdge(true, true, true, true))
            report.Fail("held space retriggered jump");
        if (KinematicStep.JumpEdge(true, false, true, false))
            report.Fail("held gamepad jump retriggered");
        if (!KinematicStep.JumpEdge(true, true, true, false))
            report.Fail("space press was swallowed while the jump axis was already held");
        if (!SameBuffer(dt, 9))
            report.Fail("buffered space press did not match the gamepad jump inside 0.16s");
        if (SameBuffer(dt, 10))
            report.Fail("buffered press still jumped after the 0.16s window");

        return report;
    }

        /// <summary>
        /// Press latches jumpBuffer 0.16 after the empty decay, then each later frame decays
        /// before the grounded check. Space and the pad share that latch. 9 decays still fire;
        /// 10 misses. Takeoff vertical is jumpSpeed 24.7 either way.
        /// </summary>
        static bool SameBuffer(float dt, int decaysBeforeLanding)
        {
            const float window = 0.16f;
            const float jumpSpeed = 24.7f;
            bool space = KinematicStep.JumpEdge(true, true, false, false);
            bool pad = KinematicStep.JumpEdge(true, false, false, false);
            if (!space || !pad || Mathf.Abs(jumpSpeed - 24.7f) > 0.001f)
                return decaysBeforeLanding < 0;
            float slot = window;
            for (int i = 0; i < decaysBeforeLanding; i++)
            {
                if (slot > 0f) slot -= dt;
            }
            return slot > 0f && KinematicStep.CoyoteJumpAllowed(true, 0f);
        }

        static void Coyote(float dt, out bool fired, out bool missedNext, out float elapsed)
    {
        // Last grounded frame stored a full coyote. Air frames decay, then the jump is tested.
        float coyote = 0.10f;
        float t = 0f;
        fired = false;
        missedNext = false;
        elapsed = 0f;
        int window = -1;
        for (int frame = 1; frame <= 12; frame++)
        {
            coyote = KinematicStep.DecayCoyote(coyote, dt);
            t += dt;
            bool allow = KinematicStep.CoyoteJumpAllowed(false, coyote);
            if (window < 0 && t + 0.00001f >= 0.10f)
            {
                window = frame;
                fired = allow;
                elapsed = t;
            }
            else if (frame == window + 1)
            {
                missedNext = !allow;
                break;
            }
        }
    }

    struct Arc
    {
        public float Distance;
        public float Apex;
        public float TakeoffSpeed;
        public float LandSpeed;
        public float AfterHop;
        public float AfterNoHop;
    }

    static Arc Simulate(float startSpeed, Vector2 move, bool yawWish, float dt)
    {
        const float crouch = 3.2f;
        const float sprint = 12f;
        const float walk = 6f;
        const float jumpSpeed = 24.7f;
        const float gravity = 22f;
        const float fallMult = 1.5f;
        const float fallCap = 52f;
        const float airAccel = 30f;
        const float groundAccel = 52f;
        const float groundDecel = 38f;
        const float yawDegPerSec = 30f;

        if (move.sqrMagnitude > 1f) move.Normalize();
        float wishSpeed = KinematicStep.GaitCap(false, false, move.y, crouch, sprint, walk);
        float yaw = 0f;
        Vector3 wish = WishAt(yaw, move);
        Vector3 hv = Vector3.zero;
        if (startSpeed > 0f && wish.sqrMagnitude > 0.0001f)
            hv = wish.normalized * startSpeed;
        else if (startSpeed > 0f)
            hv = new Vector3(0f, 0f, startSpeed);

        // Jump frame: still grounded, so ground steer runs, then the vertical impulse. No air gravity.
        bool jumping = true;
        hv = KinematicStep.GroundSteer(hv, wish, wishSpeed, groundAccel, groundDecel, dt, jumping);
        float takeoff = hv.magnitude;
        float vy = jumpSpeed;
        float x = hv.x * dt;
        float y = vy * dt;
        float z = hv.z * dt;
        float apex = y;

        for (int frame = 0; frame < 600; frame++)
        {
            float g = KinematicStep.AirGravity(vy, gravity, fallMult);
            vy = KinematicStep.IntegrateVertical(vy, g, dt, fallCap);
            if (yawWish)
            {
                yaw += yawDegPerSec * Mathf.Deg2Rad * dt;
                wish = WishAt(yaw, move);
            }

            hv = KinematicStep.AirSteer(hv, wish, wishSpeed, airAccel, dt);

            float ny = y + vy * dt;
            x += hv.x * dt;
            z += hv.z * dt;
            if (ny <= 0f)
            {
                y = 0f;
                break;
            }

            y = ny;
            if (y > apex) apex = y;
        }

        Vector3 hopped = KinematicStep.GroundSteer(hv, wish, wishSpeed, groundAccel, groundDecel, dt, true);
        Vector3 stayed = KinematicStep.GroundSteer(hv, wish, wishSpeed, groundAccel, groundDecel, dt, false);
        Arc arc;
        arc.Distance = Mathf.Sqrt(x * x + z * z);
        arc.Apex = apex;
        arc.TakeoffSpeed = takeoff;
        arc.LandSpeed = hv.magnitude;
        arc.AfterHop = hopped.magnitude;
        arc.AfterNoHop = stayed.magnitude;
        return arc;
    }

    static Vector3 WishAt(float yawRad, Vector2 move)
    {
        Vector3 forward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
        Vector3 right = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
        return WishAccel.PlanarWish(forward, right, move);
    }
}

public sealed class StrafeJumpReport
{
    public float WalkDistance;
    public float SprintDistance;
    public float StandDistance;
    public float DistanceRatio;
    public float WalkApex;
    public float SprintApex;
    public float StrafeApex;
    public float StrafeBefore;
    public float StrafeAfter;
    public bool CoyoteFiredAtWindow;
    public bool CoyoteMissedNextFrame;
    public float CoyoteElapsed;
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
        var c = CultureInfo.InvariantCulture;
        var text = new StringBuilder();
        text.Append("walk_jump_m=").Append(WalkDistance.ToString("0.000", c));
        text.Append(" sprint_jump_m=").Append(SprintDistance.ToString("0.000", c));
        text.Append(" ratio=").Append(DistanceRatio.ToString("0.000", c));
        text.Append(" stand_jump_m=").Append(StandDistance.ToString("0.000", c));
        text.Append('\n');
        text.Append("apex_walk_m=").Append(WalkApex.ToString("0.000", c));
        text.Append(" apex_sprint_m=").Append(SprintApex.ToString("0.000", c));
        text.Append(" apex_strafe_m=").Append(StrafeApex.ToString("0.000", c));
        text.Append('\n');
        text.Append("strafe_before_mps=").Append(StrafeBefore.ToString("0.000", c));
        text.Append(" strafe_after_mps=").Append(StrafeAfter.ToString("0.000", c));
        text.Append('\n');
        text.Append("coyote_at_0.10=").Append(CoyoteFiredAtWindow ? "fire" : "miss");
        text.Append(" next_frame=").Append(CoyoteMissedNextFrame ? "miss" : "fire");
        text.Append(" elapsed_s=").Append(CoyoteElapsed.ToString("0.000000", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
