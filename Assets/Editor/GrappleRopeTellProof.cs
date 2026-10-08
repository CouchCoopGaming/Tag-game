using System.Globalization;
using System.Text;
using Tag.Art;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check that the grapple rope is drawn from the hands, the aim cue
/// stays short, and the visual pass did not retune jump height.
/// </summary>
public static class GrappleRopeTellProof
{
    public static GrappleRopeTellReport Run()
    {
        var report = new GrappleRopeTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        report.JumpSpeed = cfg.jumpSpeed;
        report.HandHeight = GrappleRopeTell.HandHeight;
        report.HandForward = GrappleRopeTell.HandForward;
        report.RopeWidth = GrappleRopeTell.RopeStartWidth;
        report.HaloWidth = GrappleRopeTell.HaloWidth;
        report.AimLength = GrappleRopeTell.AimLength;
        report.HookSize = GrappleRopeTell.HookMarkerSize;

        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f)
            report.Fail("coyote is not 0.10");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f)
            report.Fail("jump buffer is not 0.16");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f)
            report.Fail("cling release grace is not 0.08");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f)
            report.Fail("jumpSpeed changed");
        if (cfg.slideBoost != 0f)
            report.Fail("slideBoost is not 0");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f)
            report.Fail("airDashDuration changed");
        if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f)
            report.Fail("airDashSpeed changed");
        if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail("airDashCooldown changed");
        if (cfg.enableJet)
            report.Fail("jet is on");
        if (GrappleRopeTell.VerticalImpulse != 0f)
            report.Fail("rope tell adds a vertical impulse");

        if (GrappleRopeTell.HandForward <= cfg.radius + 0.15f)
            report.Fail("rope starts inside the capsule");
        if (GrappleRopeTell.HandHeight <= 1.05f || GrappleRopeTell.HandHeight >= cfg.standingHeight)
            report.Fail("rope does not leave the hands");
        if (GrappleRopeTell.RopeStartWidth < 0.010f || GrappleRopeTell.RopeStartWidth > 0.016f)
            report.Fail("rope is not a thin cord");
        if (Mathf.Abs(GrappleRopeTell.RopeEndWidth - GrappleRopeTell.RopeStartWidth) > 0.001f)
            report.Fail("rope thickness changes along the cord");
        if (GrappleRopeTell.CordSag(0f) > 0.001f)
            report.Fail("a taut pull sags");
        if (GrappleRopeTell.CordSag(1f) < 0.04f || GrappleRopeTell.CordSag(1f) > 0.10f)
            report.Fail("a hold does not sag slightly");
        if (GrappleRopeTell.HaloWidth <= GrappleRopeTell.RopeStartWidth)
            report.Fail("rope has no wider halo");
        if (GrappleRopeTell.HaloWidth > 0.03f)
            report.Fail("rope halo is a pole");
        if (GrappleRopeTell.AimLength < 1.5f || GrappleRopeTell.AimLength > 4f)
            report.Fail("aim tell is not a short cast cue");
        if (GrappleRopeTell.AimWidth < 0.08f)
            report.Fail("aim tell is a hairline");
        if (GrappleRopeTell.HookMarkerSize <= GrappleRopeTell.RopeStartWidth)
            report.Fail("attach knot is smaller than the rope");

        if (GrappleRopeTell.HandSide >= 0f)
            report.Fail("rope is not on the left hand");

        Vector3 origin = new Vector3(2f, 1f, 4f);
        Vector3 hand = GrappleRopeTell.Hand(origin, Vector3.forward);
        if (Mathf.Abs(hand.x - (origin.x + GrappleRopeTell.HandSide)) > 0.001f
            || Mathf.Abs(hand.y - (origin.y + GrappleRopeTell.HandHeight)) > 0.001f
            || Mathf.Abs(hand.z - (origin.z + GrappleRopeTell.HandForward)) > 0.001f)
            report.Fail("forward hand is not in front of the left hand");

        Vector3 side = GrappleRopeTell.Hand(origin, Vector3.right);
        if (Mathf.Abs(side.x - (origin.x + GrappleRopeTell.HandForward)) > 0.001f
            || Mathf.Abs(side.z - (origin.z - GrappleRopeTell.HandSide)) > 0.001f)
            report.Fail("a right-facing pawn did not put the rope on the left hand");

        Vector3 fallback = GrappleRopeTell.Hand(origin, Vector3.zero);
        if (Mathf.Abs(fallback.z - (origin.z + GrappleRopeTell.HandForward)) > 0.001f)
            report.Fail("a zero facing dropped the rope into the body");

        Vector3 anchor = new Vector3(2f, 4f, 14f);
        if (!GrappleRopeTell.AttachedSpan(origin, Vector3.forward, anchor, out Vector3 from, out Vector3 end))
            report.Fail("a latched hit did not draw a rope");
        if ((from - hand).sqrMagnitude > 0.0001f)
            report.Fail("latched rope does not start at the hand");
        if (end.x != anchor.x || end.y != anchor.y || end.z != anchor.z)
            report.Fail("latched rope does not end on the hit");

        if (GrappleRopeTell.AttachedSpan(origin, Vector3.forward, hand, out _, out _))
            report.Fail("a zero-length rope was drawn");

        if (!GrappleRopeTell.AimSpan(origin, Vector3.forward, Vector3.up, out Vector3 aimHand, out Vector3 tip))
            report.Fail("casting did not draw an aim tell");
        Vector3 aim = tip - aimHand;
        if (Mathf.Abs(aim.magnitude - GrappleRopeTell.AimLength) > 0.001f)
            report.Fail("aim tell length drifted");
        if (aim.y <= 0f || Mathf.Abs(aim.x) > 0.001f || Mathf.Abs(aim.z) > 0.001f)
            report.Fail("aim tell did not follow the cast");
        if ((tip - origin).magnitude > 6f)
            report.Fail("aim tell is long enough to look like a latched rope");

        if (GrappleRopeTell.AimSpan(origin, Vector3.forward, Vector3.zero, out _, out _))
            report.Fail("a zero cast direction drew an aim tell");

        Vector3 steered = KinematicStep.GrappleHorizontal(
            new Vector3(6f, 9f, 0f), Vector3.zero, new Vector3(0f, 2f, 8f), 10f, 0.35f);
        if (steered.y != 0f)
            report.Fail("rope horizontal wrote vertical speed");
        if (steered.magnitude > 6.0001f)
            report.Fail("rope horizontal gained speed");

        return report;
    }
}

public sealed class GrappleRopeTellReport
{
    public float JumpSpeed;
    public float HandHeight;
    public float HandForward;
    public float RopeWidth;
    public float HaloWidth;
    public float AimLength;
    public float HookSize;
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
        text.Append(Ok ? "PASS" : "FAIL");
        text.Append(" rope hand_y=").Append(HandHeight.ToString("0.00", c));
        text.Append(" hand_fwd=").Append(HandForward.ToString("0.00", c));
        text.Append(" width=").Append(RopeWidth.ToString("0.00", c));
        text.Append(" halo=").Append(HaloWidth.ToString("0.00", c));
        text.Append(" aim_m=").Append(AimLength.ToString("0.00", c));
        text.Append(" knot=").Append(HookSize.ToString("0.00", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        text.Append(" vertical_impulse=").Append(GrappleRopeTell.VerticalImpulse.ToString("0.###", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
