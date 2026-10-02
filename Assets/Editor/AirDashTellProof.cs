using System.Globalization;
using System.Text;
using Tag.Art;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check that the air-dash read frames the pawn and does not retune feel.
/// Wings stay outside the capsule. The camera pop widens and the boom only goes out.
/// </summary>
public static class AirDashTellProof
{
    public static AirDashTellReport Run()
    {
        var report = new AirDashTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();

        report.Cooldown = cfg.airDashCooldown;
        report.Duration = cfg.airDashDuration;
        report.SlideBoost = cfg.slideBoost;
        report.Coyote = cfg.coyoteTime;
        report.JumpBuffer = cfg.jumpBuffer;
        report.ClingGrace = cfg.clingReleaseGrace;
        report.JumpSpeed = cfg.jumpSpeed;

        if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail("airDashCooldown is " + cfg.airDashCooldown.ToString("0.###", CultureInfo.InvariantCulture) + ", expected 30");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f)
            report.Fail("airDashDuration changed");
        if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f)
            report.Fail("airDashSpeed changed");
        if (cfg.slideBoost != 0f)
            report.Fail("slideBoost is not 0");
        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f)
            report.Fail("coyote is not 0.10");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f)
            report.Fail("jump buffer is not 0.16");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f)
            report.Fail("cling release grace is not 0.08");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f)
            report.Fail("jumpSpeed changed");
        if (cfg.enableJet)
            report.Fail("jet is on");
        if (cfg.enableAirDash == false)
            report.Fail("air dash is disabled");

        Vector3 origin = new Vector3(2f, 0f, 4f);
        bool placed = AirDashTell.Place(origin, Vector3.forward, out Vector3 left, out Vector3 right, out Vector3 ankle);
        if (!placed)
            report.Fail("forward dash did not place a tell");

        float gapL = AirDashTell.LateralGap(origin, left);
        float gapR = AirDashTell.LateralGap(origin, right);
        report.WingGap = gapL;
        if (Mathf.Abs(gapL - gapR) > 0.001f)
            report.Fail("wings are not symmetric");
        if (gapL <= cfg.radius)
            report.Fail("wings sit inside the capsule");
        if (Mathf.Abs(left.y - AirDashTell.WingHeight) > 0.001f || Mathf.Abs(right.y - AirDashTell.WingHeight) > 0.001f)
            report.Fail("wings are not at hip height");
        if (left.y > 0.9f)
            report.Fail("wings cover the chest");
        if (Mathf.Abs(ankle.y - origin.y - AirDashTell.AnkleHeight) > 0.001f)
            report.Fail("ankle streak is not under the feet");
        if (ankle.y > 0.25f)
            report.Fail("ankle streak is high enough to cover the torso");
        if (Mathf.Abs(ankle.x - origin.x) > 0.001f || Mathf.Abs(ankle.z - origin.z) > 0.001f)
            report.Fail("ankle streak is not under the pawn");

        bool side = AirDashTell.Place(origin, Vector3.right, out Vector3 sideA, out Vector3 sideB, out _);
        if (!side)
            report.Fail("lateral dash did not place a tell");
        if (AirDashTell.LateralGap(origin, sideA) <= cfg.radius)
            report.Fail("lateral wing sits inside the capsule");
        if (Mathf.Abs((sideA - origin).x) > 0.001f || Mathf.Abs((sideB - origin).x) > 0.001f)
            report.Fail("a right dash put a wing on the travel axis");

        float boom = AirDashTell.BoomExtra(AirDashTell.CameraAheadPop);
        float boomIn = AirDashTell.BoomExtra(-2f);
        report.BoomExtra = boom;
        if (boom <= 0f)
            report.Fail("boom comes in on a dash");
        if (boomIn != 0f)
            report.Fail("negative look-ahead pulled the boom in");
        if (AirDashTell.RibbonTime < cfg.airDashDuration)
            report.Fail("ribbon dies inside the burst");
        if (AirDashTell.WingInnerEdge <= cfg.radius)
            report.Fail("wing ribbon crosses the capsule");

        // A zero wish still returns a pose, but Place reports false so the caller can fall back.
        if (AirDashTell.Place(origin, Vector3.zero, out _, out _, out _))
            report.Fail("zero dash direction was treated as valid");

        return report;
    }
}

public sealed class AirDashTellReport
{
    public float Cooldown;
    public float Duration;
    public float SlideBoost;
    public float Coyote;
    public float JumpBuffer;
    public float ClingGrace;
    public float JumpSpeed;
    public float WingGap;
    public float BoomExtra;
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
        text.Append("cooldown_s=").Append(Cooldown.ToString("0.###", c));
        text.Append(" duration_s=").Append(Duration.ToString("0.###", c));
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        text.Append(" coyote=").Append(Coyote.ToString("0.###", c));
        text.Append(" jumpBuffer=").Append(JumpBuffer.ToString("0.###", c));
        text.Append(" clingGrace=").Append(ClingGrace.ToString("0.###", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.###", c));
        text.Append(" wingGap_m=").Append(WingGap.ToString("0.###", c));
        text.Append(" boomExtra_m=").Append(BoomExtra.ToString("0.###", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
