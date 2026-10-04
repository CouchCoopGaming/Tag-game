using System.Globalization;
using System.Text;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// 60 Hz jump with the same horizontal rope PlayerMotor applies after TryJump.
/// Takeoff writes vertical speed 24.7, then GrappleHorizontal and SetHoriz.
/// SetHoriz keeps that vertical speed. Apex is the integrated height.
/// Two hits and a straight-away cancel must not share one arc.
/// </summary>
public static class GrappleProof
{
    const float Dt = 1f / 60f;
    const float JumpSpeed = 24.7f;
    const float Gravity = 22f;
    const float FallMult = 1.5f;
    const float FallCap = 52f;
    const float Slack = 0.35f;

    public static GrappleReport Run60()
    {
        var report = new GrappleReport();
        Arc walkFree = Simulate(6f, Hook.None);
        Arc sprintFree = Simulate(12f, Hook.None);
        Arc walkRight = Simulate(6f, Hook.Right);
        Arc sprintRight = Simulate(12f, Hook.Right);
        Arc walkLeft = Simulate(6f, Hook.Left);
        Arc sprintLeft = Simulate(12f, Hook.Left);
        Arc sprintCancel = Simulate(12f, Hook.Cancel);

        report.WalkFreeApex = walkFree.Apex;
        report.SprintFreeApex = sprintFree.Apex;
        report.WalkRightApex = walkRight.Apex;
        report.SprintRightApex = sprintRight.Apex;
        report.WalkLeftApex = walkLeft.Apex;
        report.SprintLeftApex = sprintLeft.Apex;
        report.SprintCancelApex = sprintCancel.Apex;
        report.WalkRightX = walkRight.X;
        report.SprintRightX = sprintRight.X;
        report.WalkLeftX = walkLeft.X;
        report.SprintLeftX = sprintLeft.X;
        report.CancelX = sprintCancel.X;
        report.CancelZ = sprintCancel.Z;
        report.FreeSprintZ = sprintFree.Z;
        report.TakeoffVy = sprintRight.TakeoffVy;
        report.PreservedVy = sprintRight.PreservedVy && walkRight.PreservedVy && sprintCancel.PreservedVy;
        report.NoSpeedGain = walkRight.NoSpeedGain && sprintRight.NoSpeedGain && sprintCancel.NoSpeedGain && walkLeft.NoSpeedGain;

        SameApex(report, "walk free", walkFree.Apex, sprintFree.Apex);
        SameApex(report, "walk right hook", walkFree.Apex, walkRight.Apex);
        SameApex(report, "sprint right hook", walkFree.Apex, sprintRight.Apex);
        SameApex(report, "walk left hook", walkFree.Apex, walkLeft.Apex);
        SameApex(report, "sprint left hook", walkFree.Apex, sprintLeft.Apex);
        SameApex(report, "sprint cancel", walkFree.Apex, sprintCancel.Apex);

        if (Mathf.Abs(sprintRight.TakeoffVy - JumpSpeed) > 0.0001f)
            report.Fail("takeoff vertical was not jumpSpeed 24.7");
        if (!report.PreservedVy)
            report.Fail("SetHoriz did not keep the jump vertical speed");
        if (!report.NoSpeedGain)
            report.Fail("rope increased horizontal speed");

        if (!(sprintFree.Z > walkFree.Z * 1.9f && sprintFree.Z < walkFree.Z * 2.1f))
            report.Fail("free sprint jump did not carry about 2x the walk distance");
        if (Mathf.Abs(walkFree.X) > 0.02f || Mathf.Abs(sprintFree.X) > 0.02f)
            report.Fail("free jump drifted sideways");

        if (!(walkRight.X > 1f) || !(sprintRight.X > walkRight.X * 1.5f))
            report.Fail("right hook did not redirect, or walk and sprint shared one arc");
        if (!(walkLeft.X < -1f) || !(sprintLeft.X < walkLeft.X * 1.5f))
            report.Fail("left hook did not redirect the other way");
        if (Mathf.Abs(sprintRight.X - sprintLeft.X) < 2f)
            report.Fail("left and right hits traveled the same arc");

        if (Mathf.Abs(sprintCancel.X) > 0.05f || Mathf.Abs(sprintCancel.Z) > 0.05f)
            report.Fail("straight-away rope did not cancel horizontal travel");
        if (Mathf.Abs(sprintCancel.Z - sprintFree.Z) < 1f)
            report.Fail("cancel matched the free jump arc");

        Vector3 toward = KinematicStep.GrappleHorizontal(
            new Vector3(0f, 0f, 8f), Vector3.zero, new Vector3(0f, 3f, 12f), 12.369f, Slack);
        if (toward.y != 0f || Mathf.Abs(toward.z - 8f) > 0.001f || Mathf.Abs(toward.x) > 0.001f)
            report.Fail("moving toward the hit changed horizontal velocity");

        Vector3 under = KinematicStep.GrappleHorizontal(
            new Vector3(6f, 4f, 2f), new Vector3(0f, 1f, 0f), new Vector3(0f, 11f, 0f), 10f, 0f);
        if (under.sqrMagnitude > 0.0001f)
            report.Fail("a hit overhead did not cancel horizontal travel");

        return report;
    }

    static void SameApex(GrappleReport report, string name, float expected, float actual)
    {
        if (Mathf.Abs(expected - actual) > 0.001f)
            report.Fail(name + " apex changed (" + actual.ToString("0.000", CultureInfo.InvariantCulture) + ")");
    }

    enum Hook
    {
        None,
        Right,
        Left,
        Cancel
    }

    struct Arc
    {
        public float Apex;
        public float X;
        public float Z;
        public float TakeoffVy;
        public bool PreservedVy;
        public bool NoSpeedGain;
    }

    static Arc Simulate(float startSpeed, Hook hook)
    {
        Vector3 pos = Vector3.zero;
        Vector3 hv = new Vector3(0f, 0f, startSpeed);
        Vector3 anchor = Anchor(hook);
        float rope = (anchor - pos).magnitude;
        float vy = JumpSpeed;
        bool first = true;
        float apex = 0f;
        bool preserved = true;
        bool noGain = true;
        float takeoffVy = 0f;

        for (int frame = 0; frame < 600; frame++)
        {
            if (!first)
            {
                float g = KinematicStep.AirGravity(vy, Gravity, FallMult);
                vy = KinematicStep.IntegrateVertical(vy, g, Dt, FallCap);
            }

            Vector3 v = new Vector3(hv.x, vy, hv.z);
            if (hook != Hook.None)
            {
                float before = hv.magnitude;
                Vector3 steered = KinematicStep.GrappleHorizontal(WishAccel.Horizontal(v), pos, anchor, rope, Slack);
                if (steered.y != 0f) preserved = false;
                if (steered.magnitude > before + 0.0001f) noGain = false;
                v = WishAccel.SetHoriz(v, steered);
            }

            if (Mathf.Abs(v.y - vy) > 0.0001f) preserved = false;
            if (first) takeoffVy = v.y;
            first = false;
            hv = WishAccel.Horizontal(v);

            float ny = pos.y + v.y * Dt;
            pos.x += v.x * Dt;
            pos.z += v.z * Dt;
            if (ny <= 0f)
            {
                pos.y = 0f;
                break;
            }

            pos.y = ny;
            if (pos.y > apex) apex = pos.y;
        }

        Arc arc;
        arc.Apex = apex;
        arc.X = pos.x;
        arc.Z = pos.z;
        arc.TakeoffVy = takeoffVy;
        arc.PreservedVy = preserved;
        arc.NoSpeedGain = noGain;
        return arc;
    }

    static Vector3 Anchor(Hook hook)
    {
        if (hook == Hook.Right) return new Vector3(6f, 4f, -8f);
        if (hook == Hook.Left) return new Vector3(-6f, 4f, -8f);
        if (hook == Hook.Cancel) return new Vector3(0f, 8f, -10f);
        return new Vector3(0f, 4f, 10f);
    }
}

public sealed class GrappleReport
{
    public float WalkFreeApex;
    public float SprintFreeApex;
    public float WalkRightApex;
    public float SprintRightApex;
    public float WalkLeftApex;
    public float SprintLeftApex;
    public float SprintCancelApex;
    public float WalkRightX;
    public float SprintRightX;
    public float WalkLeftX;
    public float SprintLeftX;
    public float CancelX;
    public float CancelZ;
    public float FreeSprintZ;
    public float TakeoffVy;
    public bool PreservedVy;
    public bool NoSpeedGain;
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
        text.Append("button=RMB");
        text.Append(" attach=camera-forward raycast hit");
        text.Append(" jump_impulse=").Append(TakeoffVy.ToString("0.000", c));
        text.Append('\n');
        text.Append("apex_walk_free_m=").Append(WalkFreeApex.ToString("0.000", c));
        text.Append(" apex_sprint_free_m=").Append(SprintFreeApex.ToString("0.000", c));
        text.Append(" apex_walk_right_m=").Append(WalkRightApex.ToString("0.000", c));
        text.Append(" apex_sprint_right_m=").Append(SprintRightApex.ToString("0.000", c));
        text.Append('\n');
        text.Append("apex_walk_left_m=").Append(WalkLeftApex.ToString("0.000", c));
        text.Append(" apex_sprint_left_m=").Append(SprintLeftApex.ToString("0.000", c));
        text.Append(" apex_sprint_cancel_m=").Append(SprintCancelApex.ToString("0.000", c));
        text.Append('\n');
        text.Append("x_walk_right_m=").Append(WalkRightX.ToString("0.000", c));
        text.Append(" x_sprint_right_m=").Append(SprintRightX.ToString("0.000", c));
        text.Append(" x_walk_left_m=").Append(WalkLeftX.ToString("0.000", c));
        text.Append(" x_sprint_left_m=").Append(SprintLeftX.ToString("0.000", c));
        text.Append('\n');
        text.Append("cancel_xz_m=").Append(CancelX.ToString("0.000", c)).Append(",").Append(CancelZ.ToString("0.000", c));
        text.Append(" free_sprint_z_m=").Append(FreeSprintZ.ToString("0.000", c));
        text.Append(" vy_kept=").Append(PreservedVy ? "yes" : "no");
        text.Append(" no_speed_gain=").Append(NoSpeedGain ? "yes" : "no");
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
