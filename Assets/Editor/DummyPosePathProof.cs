using System;
using System.IO;
using System.Text;
using Tag.Art;
using Tag.Local;

/// <summary>
/// Headless check: DummyRunner reads the same chase-cam pose layers as the player,
/// except grapple and air dash. Chase ribbons and the lunge tell stay.
/// </summary>
public static class DummyPosePathProof
{
    public static DummyPosePathReport Run()
    {
        var report = new DummyPosePathReport();
        if (!DummyPosePaths.Holds())
            report.Fail("dummy pose share is not held");

        string loco = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string patrol = ReadRepo("Assets/Scripts/Modes/DummyPatrol.cs");
        string steer = ReadRepo("Assets/Scripts/Modes/OpponentChaseSteer.cs");
        string lunge = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        string chase = ReadRepo("Assets/Scripts/Art/OpponentChaseTell.cs");
        string gate = ReadRepo("Assets/Scripts/Local/SoloGrappleGate.cs");
        string motor = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string spawn = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        if (loco == null || patrol == null || steer == null || lunge == null || chase == null
            || gate == null || motor == null || spawn == null)
        {
            report.Fail("dummy pose sources missing");
            return report;
        }

        if (!loco.Contains("DummyPosePaths.Gait") || !loco.Contains("GaitBlend.PoseWeight"))
            report.Fail("gait is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Jump") || !loco.Contains("JumpPose.PoseActive"))
            report.Fail("jump beats are not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Wall") || !loco.Contains("WallPose.Climb"))
            report.Fail("wall climb/run is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Punch") || !loco.Contains("PunchTagPose.PunchWindup"))
            report.Fail("punch windup is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Tag") || !loco.Contains("PunchTagPose.Tag"))
            report.Fail("tag catch is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Lunge") || !loco.Contains("IsLunging")
            || !loco.Contains("ApplyLungePose") || !loco.Contains("LungePose.Burst")
            || !loco.Contains("LungePose.Telegraph"))
            report.Fail("lunge pose is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Slide") || !loco.Contains("SlideBody"))
            report.Fail("slide pose is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Crouch") || !loco.Contains("CrouchPose"))
            report.Fail("crouch pose is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Mantle") || !loco.Contains("MantlePose.At"))
            report.Fail("mantle pose is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Land") || !loco.Contains("JumpLandTell.ForPawn"))
            report.Fail("land thud is not on the dummy pose path");
        if (!loco.Contains("DummyPosePaths.Grapple") || !loco.Contains("DummyPosePaths.Dash"))
            report.Fail("grapple or air dash was not denied on the dummy");

        string land = MethodBody(loco, "bool JumpLandSolo");
        if (land == null || !land.Contains("DummyPosePaths.Land") || !land.Contains("JumpLandTell.ForPawn")
            || !land.Contains("SoloGrappleGate.OpponentPawnName"))
            report.Fail("land thud does not open for DummyRunner");

        string dashPose = MethodBody(loco, "void ApplyAirDashPose");
        if (dashPose == null || !dashPose.Contains("DummyPosePaths.Dash") || !dashPose.Contains("AirDashPose.At"))
            report.Fail("air dash pose is not denied for the dummy");

        if (!patrol.Contains("airDash: false") || patrol.Contains("airDash: airDash")
            || patrol.Contains("AirDashCooldownRemaining"))
            report.Fail("dummy still requests an air dash");
        if (!patrol.Contains("OpponentLungeTell") || !patrol.Contains("OpponentChaseSteer.Decide"))
            report.Fail("lunge tell or chase steer was disconnected");
        if (!lunge.Contains("LeadSeconds = 0.45f"))
            report.Fail("LungeTell lead is not 0.45s");
        if (!chase.Contains("OpponentPawnName = \"DummyRunner\"") || !loco.Contains("TickOpponentChase"))
            report.Fail("chase ribbons were removed");
        if (steer.Contains("AirDash = true"))
            report.Fail("chase steer grants an air dash");
        if (!gate.Contains("OpponentPawnName = \"DummyRunner\"") || !spawn.Contains("ApplySoloGrapple"))
            report.Fail("grapple gate was removed");
        if (Count(motor, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motor.Contains("DummyPosePaths"))
            report.Fail("dummy pose share leaked into the motor");

        if (!DummyPosePaths.Allows(SoloGrappleGate.OpponentPawnName, DummyPosePaths.Land)
            || DummyPosePaths.Allows(SoloGrappleGate.OpponentPawnName, DummyPosePaths.Grapple)
            || DummyPosePaths.Allows(SoloGrappleGate.OpponentPawnName, DummyPosePaths.Dash))
            report.Fail("dummy allow list drifted");

        return report;
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

    static string MethodBody(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        if (start < 0) return null;
        int brace = source.IndexOf('{', start);
        if (brace < 0) return null;
        int depth = 0;
        for (int i = brace; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0) return source.Substring(brace, i - brace + 1);
            }
        }
        return null;
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

public sealed class DummyPosePathReport
{
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
        return DummyPosePaths.ProofLine();
    }
}
