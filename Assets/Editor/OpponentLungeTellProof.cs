using System.Globalization;
using System.Text;
using Tag.Art;
using Tag.Gameplay;
using Tag.Level;
using Tag.Local;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check: one campus opponent, a fixed lunge tell, tag reach 1.55, locked feel numbers.
/// </summary>
public static class OpponentLungeTellProof
{
    public static OpponentLungeTellReport Run()
    {
        var report = new OpponentLungeTellReport();
        var defaults = ScriptableObject.CreateInstance<MovementConfig>();
        var asset = Resources.Load<MovementConfig>("TagArena/MovementConfig");
        CheckConfig(report, defaults, "defaults");
        if (asset == null)
            report.Fail("MovementConfig asset missing");
        else
            CheckConfig(report, asset, "asset");

        var punch = PunchTagTuning.CreateRuntimeDefaults();
        report.TagReach = punch.reach;
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail("punch reach is not 1.55");

        report.TellLead = OpponentLungeTell.LeadSeconds;
        report.Pawn = LocalPlayerSpawner.OpponentPawnName;
        report.Pad = LocalPlayerSpawner.OpponentPadName;
        if (report.Pawn != "DummyRunner")
            report.Fail("opponent pawn is not DummyRunner");
        if (LocalPlayerSpawner.SoloPawnName != SoloGrappleGate.SoloPawnName)
            report.Fail("solo pawn name drifted");
        if (!SoloGrappleGate.EnableFor(false, false, 0, LocalPlayerSpawner.SoloPawnName))
            report.Fail("solo pawn does not get the grapple");
        if (SoloGrappleGate.EnableFor(false, true, 1, LocalPlayerSpawner.OpponentPawnName))
            report.Fail("opponent gets a grapple");
        if (SoloGrappleGate.EnableFor(false, false, 0, LocalPlayerSpawner.OpponentPawnName))
            report.Fail("opponent name gets a grapple");
        if (SoloGrappleGate.EnableFor(true, false, 0, LocalPlayerSpawner.SoloPawnName))
            report.Fail("couch player gets a grapple");
        if (SoloGrappleGate.EnableFor(true, false, 1, "Player_P1"))
            report.Fail("couch clone gets a grapple");
        if (report.Pad != "Spawn_SE")
            report.Fail("opponent pad is not Spawn_SE");

        if (Mathf.Abs(OpponentLungeTell.LeadSecondsFor(0f) - OpponentLungeTell.LeadSecondsFor(1f)) > 0.0001f)
            report.Fail("lunge tell scales with skill");
        if (Mathf.Abs(OpponentLungeTell.LeadSecondsFor(0.5f) - OpponentLungeTell.LeadSeconds) > 0.0001f)
            report.Fail("lunge tell lead is not the fixed lead");
        if (OpponentLungeTell.LeadSeconds < 0.35f)
            report.Fail("lunge tell is too short to read");

        float reach = punch.reach;
        if (OpponentLungeTell.InLungeWindow(reach, 0f, reach))
            report.Fail("lunge window overlaps tag reach");
        if (!OpponentLungeTell.InLungeWindow(reach + 1f, 0f, reach))
            report.Fail("lunge window misses the approach band");
        if (OpponentLungeTell.InLungeWindow(reach + 1f, 40f, reach))
            report.Fail("lunge window ignores facing");
        if (OpponentLungeTell.RingRadius <= defaults.radius)
            report.Fail("lunge ring sits inside the capsule");
        if (OpponentLungeTell.RingHeight > 0.25f)
            report.Fail("lunge ring covers the torso");

        Vector3 spawn = LocalPlayerSpawner.Spawns[1];
        report.Spawn = spawn;
        float expectX = LocalPlayerSpawner.OpponentPadGrayX;
        float expectZ = LocalPlayerSpawner.OpponentPadGrayZ;
        if (Mathf.Abs(spawn.x - expectX) > 0.01f || Mathf.Abs(spawn.z - expectZ) > 0.01f)
            report.Fail("Spawns[1] is not the Mega Park Spawn_SE pad");
        if (Mathf.Abs(spawn.y - MegaParkP1Layout.SpawnY) > 0.01f)
            report.Fail("Spawn_SE is buried or floating");

        var host = new GameObject("OpponentLungeTellProofHost");
        try
        {
            var tell = host.AddComponent<OpponentLungeTell>();
            tell.Show(1f);
            var mark = host.transform.Find(OpponentLungeTell.MarkerName);
            if (mark == null)
                report.Fail("LungeTell marker was not built");
            else if (mark.GetComponentInChildren<Collider>(true) != null)
                report.Fail("lunge tell has a collider and can change tag reach");
            tell.Hide();
            if (tell.IsShowing)
                report.Fail("lunge tell stayed visible after hide");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }

        if (defaults != null)
            Object.DestroyImmediate(defaults);
        if (punch != null)
            Object.DestroyImmediate(punch);
        return report;
    }

    static void CheckConfig(OpponentLungeTellReport report, MovementConfig cfg, string label)
    {
        report.Coyote = cfg.coyoteTime;
        report.JumpBuffer = cfg.jumpBuffer;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;
        report.AirDashDuration = cfg.airDashDuration;
        report.AirDashSpeed = cfg.airDashSpeed;
        report.AirDashCooldown = cfg.airDashCooldown;
        report.ClingGrace = cfg.clingReleaseGrace;
        report.AirCrouchFall = cfg.airCrouchFallMult;
        report.MaxFall = cfg.maxFallSpeed;

        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f)
            report.Fail(label + " coyote is not 0.10");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f)
            report.Fail(label + " jump buffer is not 0.16");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f)
            report.Fail(label + " jumpSpeed is not 24.7");
        if (cfg.slideBoost != 0f)
            report.Fail(label + " slideBoost is not 0");
        if (cfg.slideFlatFriction <= 0f)
            report.Fail(label + " slide does not decay");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f)
            report.Fail(label + " air dash duration is not 0.10");
        if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f)
            report.Fail(label + " air dash speed is not 15");
        if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail(label + " air dash cooldown is not 30");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f)
            report.Fail(label + " cling grace is not 0.08");
        if (Mathf.Abs(cfg.airCrouchFallMult - 2f) > 0.001f)
            report.Fail(label + " air crouch fall is not x2");
        if (Mathf.Abs(cfg.maxFallSpeed - 56.16f) > 0.001f)
            report.Fail(label + " maxFall is not 52");
        if (cfg.enableJet)
            report.Fail(label + " jet is on");
    }
}

public sealed class OpponentLungeTellReport
{
    public float Coyote;
    public float JumpBuffer;
    public float JumpSpeed;
    public float SlideBoost;
    public float AirDashDuration;
    public float AirDashSpeed;
    public float AirDashCooldown;
    public float ClingGrace;
    public float AirCrouchFall;
    public float MaxFall;
    public float TagReach;
    public float TellLead;
    public string Pawn;
    public string Pad;
    public Vector3 Spawn;
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
        text.Append("pawn=").Append(Pawn);
        text.Append(" pad=").Append(Pad);
        text.Append(" spawn=").Append(Spawn.ToString());
        text.Append(" tellLead=").Append(TellLead.ToString("0.###", c));
        text.Append(" tagReach=").Append(TagReach.ToString("0.###", c));
        text.Append(" coyote=").Append(Coyote.ToString("0.###", c));
        text.Append(" jumpBuffer=").Append(JumpBuffer.ToString("0.###", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.###", c));
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        text.Append(" airDash=").Append(AirDashDuration.ToString("0.###", c));
        text.Append("/").Append(AirDashSpeed.ToString("0.###", c));
        text.Append("/").Append(AirDashCooldown.ToString("0.###", c));
        text.Append(" clingGrace=").Append(ClingGrace.ToString("0.###", c));
        text.Append(" airCrouchFall=").Append(AirCrouchFall.ToString("0.###", c));
        text.Append(" maxFall=").Append(MaxFall.ToString("0.###", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
