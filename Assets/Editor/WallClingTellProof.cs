using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Art;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check: a cling draws two short wall marks at the hands,
/// and the pass did not retune climb, jump, slide, or punch numbers.
/// </summary>
public static class WallClingTellProof
{
    public static WallClingTellReport Run()
    {
        var report = new WallClingTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.MarkCount = WallClingTell.MarkCount;
        report.RibbonTime = WallClingTell.RibbonTime;
        report.RibbonWidth = WallClingTell.RibbonWidth;
        report.ClimbHeight = WallClingTell.ClimbHeight;
        report.RunHeight = WallClingTell.RunHeight;
        report.MaxAlpha = WallClingTell.MaxAlpha;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;
        report.ClingGrace = cfg.clingReleaseGrace;

        if (WallClingTell.MarkCount != 2)
            report.Fail("cling tell is not two hand marks");
        if (WallClingTell.RibbonTime < 0.12f || WallClingTell.RibbonTime > 0.18f)
            report.Fail("ribbon lifetime is outside 0.12-0.18s");
        if (WallClingTell.RibbonWidth < 0.12f || WallClingTell.RibbonWidth > 0.22f)
            report.Fail("ribbon width is a hairline or wide enough to hide the arms");
        if (WallClingTell.MarkSize < 0.16f || WallClingTell.MarkSize > 0.26f)
            report.Fail("hand mark is too small to read or large enough to hide the torso");
        if (WallClingTell.HandSpread <= 0.08f || WallClingTell.HandSpread >= cfg.radius)
            report.Fail("hand marks are not across the chest");
        if (WallClingTell.PairSpan >= cfg.radius * 2f)
            report.Fail("ribbon pair is wider than the capsule");
        if (WallClingTell.MarkSpan >= cfg.radius * 2f)
            report.Fail("hand marks are wider than the capsule");
        float skinGap = cfg.radius - WallClingTell.SurfaceOffset;
        if (skinGap < 0.005f || skinGap > 0.05f)
            report.Fail("marks are not on the wall face of the capsule");
        if (WallClingTell.ClimbHeight < 0.85f || WallClingTell.ClimbHeight > cfg.standingHeight - 0.40f)
            report.Fail("climb marks are not at the hands");
        if (WallClingTell.RunHeight < 0.70f || WallClingTell.RunHeight > cfg.standingHeight - 0.55f)
            report.Fail("wall-run marks are not at the chest");
        if (WallClingTell.ClimbHeight + WallClingTell.ClimbStagger > cfg.standingHeight - 0.25f)
            report.Fail("the lead hand covers the head");
        if (WallClingTell.ClimbHeight - WallClingTell.RunHeight < 0.12f)
            report.Fail("a climb does not read higher than a wall run");
        if (WallClingTell.ClimbStagger < 0.10f || WallClingTell.ClimbStagger > 0.28f)
            report.Fail("climb stagger does not read as a grab");
        if (WallClingTell.RunStagger < 0f || WallClingTell.RunStagger > 0.06f)
            report.Fail("a wall run does not stay level");
        if (WallClingTell.MaxAlpha < 0.32f || WallClingTell.MaxAlpha > 0.48f)
            report.Fail("dust hides the body or is too faint to read");
        if (WallClingTell.DustR > 0.90f || WallClingTell.DustG > 0.90f || WallClingTell.DustB > 0.90f)
            report.Fail("dust is bright enough to wash the chase camera");
        if (WallClingTell.DustR < 0.45f)
            report.Fail("dust is too dark to read on a wall");
        if (WallClingTell.Glow != 0f)
            report.Fail("cling tell adds a glow");
        if (WallClingTell.RibbonTime * cfg.wallRunSpeed > 1.7f)
            report.Fail("ribbon is long enough to read as extra speed");
        if (WallClingTell.RibbonTime * cfg.climbSpeed > 1.2f)
            report.Fail("climb ribbon is long enough to read as extra speed");
        if (WallClingTell.VerticalImpulse != 0f)
            report.Fail("cling tell adds a vertical impulse");

        Vector3 origin = new Vector3(2f, 1f, 4f);
        Vector3 wallAhead = new Vector3(0f, 0f, -1f);
        if (!WallClingTell.Place(origin, wallAhead, true, out Vector3 climbL, out Vector3 climbR))
            report.Fail("a climb did not place hand marks");
        float climbY = origin.y + WallClingTell.ClimbHeight;
        if (Mathf.Abs(climbR.y - climbY) > 0.001f)
            report.Fail("the trail hand is not at climb height");
        if (Mathf.Abs(climbL.y - (climbY + WallClingTell.ClimbStagger)) > 0.001f)
            report.Fail("the lead hand is not raised");
        if (Mathf.Abs(climbL.z - (origin.z + WallClingTell.SurfaceOffset)) > 0.001f
            || Mathf.Abs(climbR.z - (origin.z + WallClingTell.SurfaceOffset)) > 0.001f)
            report.Fail("climb marks are not on the wall ahead");
        if (climbL.z <= origin.z || climbR.z <= origin.z)
            report.Fail("climb marks sit behind the pawn");
        if (Mathf.Abs((climbL.x + climbR.x) * 0.5f - origin.x) > 0.001f)
            report.Fail("climb marks are not centered on the pawn");
        if (Mathf.Abs(Mathf.Abs(climbL.x - origin.x) - WallClingTell.HandSpread) > 0.001f
            || Mathf.Abs(Mathf.Abs(climbR.x - origin.x) - WallClingTell.HandSpread) > 0.001f)
            report.Fail("climb marks are not at the hands");
        if (Mathf.Abs((climbL - climbR).magnitude) < WallClingTell.HandSpread)
            report.Fail("the two climb marks stack on one point");

        if (!WallClingTell.Place(origin, wallAhead, false, out Vector3 runL, out Vector3 runR))
            report.Fail("a wall run did not place hand marks");
        float runY = origin.y + WallClingTell.RunHeight;
        if (Mathf.Abs(runR.y - runY) > 0.001f)
            report.Fail("the wall-run trail hand is not at chest height");
        if (Mathf.Abs(runL.y - (runY + WallClingTell.RunStagger)) > 0.001f)
            report.Fail("the wall-run lead hand is not level");
        if (climbL.y - runL.y < 0.12f)
            report.Fail("a climb mark is not above the wall-run mark");
        if (Mathf.Abs(runL.z - (origin.z + WallClingTell.SurfaceOffset)) > 0.001f
            || Mathf.Abs(runR.z - (origin.z + WallClingTell.SurfaceOffset)) > 0.001f)
            report.Fail("wall-run marks are not on the wall ahead");

        Vector3 wallRight = new Vector3(-1f, 0f, 0f);
        if (!WallClingTell.Place(origin, wallRight, false, out Vector3 sideL, out Vector3 sideR))
            report.Fail("a side wall did not place hand marks");
        if (Mathf.Abs(sideL.x - (origin.x + WallClingTell.SurfaceOffset)) > 0.001f
            || Mathf.Abs(sideR.x - (origin.x + WallClingTell.SurfaceOffset)) > 0.001f)
            report.Fail("a side wall did not put the marks on that face");
        if (sideL.x <= origin.x || sideR.x <= origin.x)
            report.Fail("a side wall put the marks on the open side");
        if (Mathf.Abs(Mathf.Abs(sideL.z - origin.z) - WallClingTell.HandSpread) > 0.001f
            || Mathf.Abs(Mathf.Abs(sideR.z - origin.z) - WallClingTell.HandSpread) > 0.001f)
            report.Fail("a side wall did not split the marks across the hands");

        if (WallClingTell.Place(origin, Vector3.zero, true, out _, out _))
            report.Fail("a zero wall normal drew a mark");
        if (WallClingTell.Place(origin, Vector3.up, true, out _, out _))
            report.Fail("a vertical normal drew a mark");

        CheckLocks(report, cfg, punch, "defaults");

        string moveAsset = ReadRepo("Assets/Resources/TagArena/MovementConfig.asset");
        string punchAsset = ReadRepo("Assets/ScriptableObjects/PunchTagTuning.asset");
        if (moveAsset == null)
            report.Fail("MovementConfig asset missing");
        else if (!Has(moveAsset, "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
            "jumpSpeed: 24.7", "slideBoost: 0", "slideFlatFriction: 6.8", "slideDownhillAccel: 0",
            "enableJet: 0", "airDashDuration: 0.1", "airDashSpeed: 15", "airDashCooldown: 30",
            "climbSpeed: 6", "climbSlipSpeed: 3.7", "wallBounceSpeed: 9.2", "wallBounceUp: 7",
            "wallRunJumpOut: 8", "wallRunJumpUp: 6.2", "wallRunSpeed: 9.5"))
            report.Fail("MovementConfig asset drifted");
        if (punchAsset == null)
            report.Fail("PunchTagTuning asset missing");
        else if (!Has(punchAsset, "reach: 1.55", "ragdollDuration: 1.15", "windup: 0.12", "active: 0.1", "hitRecover: 0.15", "missRecover: 0.32"))
            report.Fail("PunchTagTuning asset drifted");

        string tellSrc = ReadRepo("Assets/Scripts/Art/WallClingTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string binderSrc = ReadRepo("Assets/Scripts/Art/DummyAvatarBinder.cs");
        string animSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Animation/MoveAnimDriver.cs");
        if (tellSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || spawnSrc == null || binderSrc == null || animSrc == null)
            report.Fail("cling sources missing");
        else
            CheckWiring(report, tellSrc, locoSrc, motorSrc, cfgSrc, spawnSrc, binderSrc, animSrc);

        if (VerbPoseClips.SlideBody != "SlideBody" || VerbPoseClips.PunchStrike != "PunchStrike" || VerbPoseClips.TagCatch != "TagCatch")
            report.Fail("verb clip names drifted");
        if (ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandFlash.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/AirDashTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/ItMarker.cs") == null)
            report.Fail("an existing tell was removed");

        return report;
    }

    static void CheckLocks(WallClingTellReport report, MovementConfig cfg, PunchTagTuning punch, string label)
    {
        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f)
            report.Fail(label + " coyote is not 0.10");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f)
            report.Fail(label + " jump buffer is not 0.16");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f)
            report.Fail(label + " cling grace is not 0.08");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f)
            report.Fail(label + " jumpSpeed changed");
        if (cfg.slideBoost != 0f)
            report.Fail(label + " slideBoost is not 0");
        if (Mathf.Abs(cfg.slideFlatFriction - 6.8f) > 0.001f)
            report.Fail(label + " slide friction changed");
        if (cfg.slideDownhillAccel != 0f)
            report.Fail(label + " slide downhill accel is not 0");
        if (cfg.enableJet)
            report.Fail(label + " jet is on");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f || Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f
            || Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail(label + " air dash numbers changed");
        if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f)
            report.Fail(label + " climb speed changed");
        if (Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f)
            report.Fail(label + " climb slip changed");
        if (Mathf.Abs(cfg.wallBounceSpeed - 9.2f) > 0.001f || Mathf.Abs(cfg.wallBounceUp - 7.0f) > 0.001f)
            report.Fail(label + " wall-jump impulse changed");
        if (Mathf.Abs(cfg.wallRunJumpOut - 8.0f) > 0.001f || Mathf.Abs(cfg.wallRunJumpUp - 6.2f) > 0.001f)
            report.Fail(label + " wall-run jump impulse changed");
        if (Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f)
            report.Fail(label + " wall-run speed changed");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail(label + " punch reach is not 1.55");
        if (Mathf.Abs(punch.ragdollDuration - 1.15f) > 0.001f)
            report.Fail(label + " ragdoll duration changed");
        if (Mathf.Abs(punch.windup - 0.12f) > 0.001f || Mathf.Abs(punch.active - 0.10f) > 0.001f
            || Mathf.Abs(punch.hitRecover - 0.15f) > 0.001f || Mathf.Abs(punch.missRecover - 0.32f) > 0.001f)
            report.Fail(label + " punch timings changed");
    }

    static void CheckWiring(WallClingTellReport report, string tellSrc, string locoSrc, string motorSrc, string cfgSrc, string spawnSrc, string binderSrc, string animSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("climbSpeed") || tellSrc.Contains("clingReleaseGrace")
            || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost") || tellSrc.Contains("FixedUpdate")
            || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce") || tellSrc.Contains("applyRootMotion"))
            report.Fail("cling constants write feel or the camera");

        if (!Has(cfgSrc, "slideBoost = 0f", "jumpSpeed = 24.7f", "clingReleaseGrace = 0.08f",
            "climbSpeed = 6.0f", "climbSlipSpeed = 3.7f", "wallBounceSpeed = 9.2f", "wallBounceUp = 7.0f",
            "wallRunJumpOut = 8.0f", "wallRunJumpUp = 6.2f", "wallRunSpeed = 9.5f"))
            report.Fail("movement defaults drifted");

        if (!Has(motorSrc, "case MoveState.WallClimb: return Locomotion.Climb;",
            "case MoveState.WallRun: return Locomotion.WallRun;",
            "SetState(MoveState.WallClimb)", "SetState(MoveState.WallRun)"))
            report.Fail("wall states no longer match climb and wall run");
        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("WallCling"))
            report.Fail("cling tell leaked into the motor");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickWallCling");
        string begin = MethodBody(locoSrc, "void BeginWallCling");
        string end = MethodBody(locoSrc, "void EndWallCling");
        string place = MethodBody(locoSrc, "void PlaceWallCling");
        string makeRibbon = MethodBody(locoSrc, "TrailRenderer MakeClingRibbon");
        string makeMark = MethodBody(locoSrc, "Transform MakeClingMark");
        if (late == null || tick == null || begin == null || end == null || place == null || makeRibbon == null || makeMark == null)
        {
            report.Fail("cling tell is not on the wall visual path");
            return;
        }

        int call = late.IndexOf("TickWallCling(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("cling tell does not run with the wall state");
        if (!tick.Contains("MoveState.WallClimb") || !tick.Contains("MoveState.WallRun")
            || !tick.Contains("BeginWallCling") || !tick.Contains("EndWallCling"))
            report.Fail("cling tell does not follow climb and wall-run enter and exit");
        if (tick.Contains("MoveState.Mantle") || tick.Contains("MoveState.Slide"))
            report.Fail("cling tell plays off the wall");
        if (!begin.Contains("PlaceWallCling") || !begin.Contains("ArmRibbon") || !begin.Contains("SetClingMarks(true)"))
            report.Fail("wall enter does not start the marks");
        if (!place.Contains("WallClingTell.Place"))
            report.Fail("marks are not placed by the cling builder");
        if (!end.Contains("emitting = false") || !end.Contains("SetClingMarks(false)"))
            report.Fail("wall exit does not stop the marks");
        if (end.Contains("Clear("))
            report.Fail("wall exit wipes the short scrape");
        if (!makeRibbon.Contains("WallClingTell.RibbonTime") || !makeRibbon.Contains("WallClingTell.RibbonWidth")
            || !makeRibbon.Contains("WallClingTell.MaxAlpha"))
            report.Fail("ribbons do not use the tell constants");
        if (makeMark.Contains("CreatePrimitive") || makeMark.Contains("AddComponent<Collider>")
            || makeMark.Contains("BoxCollider") || makeMark.Contains("SphereCollider")
            || makeMark.Contains("CapsuleCollider") || makeMark.Contains("Rigidbody")
            || makeMark.Contains("AddComponent<Light>") || makeMark.Contains("Light"))
            report.Fail("hand marks add a collider or a light");
        if (!makeMark.Contains("MeshFilter") || !makeMark.Contains("MeshRenderer"))
            report.Fail("hand marks are not a bare mesh");
        if (!makeMark.Contains("WallClingTell.MarkSize"))
            report.Fail("hand marks do not use the tell size");
        if (Count(locoSrc, "MakeClingRibbon(\"") != WallClingTell.MarkCount)
            report.Fail("cling tell does not build both ribbons");
        if (Count(locoSrc, "MakeClingMark(\"") != WallClingTell.MarkCount)
            report.Fail("cling tell does not build both hand marks");

        string clingBodies = tick + begin + end + place + makeRibbon + makeMark
            + (MethodBody(locoSrc, "void EnsureWallCling") ?? "")
            + (MethodBody(locoSrc, "void RefreshClingFace") ?? "")
            + (MethodBody(locoSrc, "void SetClingMarks") ?? "")
            + (MethodBody(locoSrc, "static Material MakeClingMat") ?? "")
            + (MethodBody(locoSrc, "static Mesh ClingQuad") ?? "");
        if (clingBodies.Contains("slideBoost") || clingBodies.Contains("jumpSpeed") || clingBodies.Contains("climbSpeed")
            || clingBodies.Contains("clingReleaseGrace") || clingBodies.Contains("wallRunJump")
            || clingBodies.Contains("CharacterController") || clingBodies.Contains("Rigidbody")
            || clingBodies.Contains("FovPop") || clingBodies.Contains("AddKick")
            || clingBodies.Contains("FixedUpdate") || clingBodies.Contains("applyRootMotion")
            || clingBodies.Contains("AddForce") || clingBodies.Contains("Light"))
            report.Fail("cling visual writes feel or the camera");

        if (!spawnSrc.Contains("DummyAvatarBinder") || !binderSrc.Contains("DummyLocomotor"))
            report.Fail("both pawns are not given the locomotor that plays the cling");
        if (animSrc.Contains("WallCling"))
            report.Fail("cling tell was wired through the anim driver");
        string audioBus = ReadRepo("Assets/Scripts/Audio/AudioBus.cs");
        bool slideShot = animSrc.Contains("TagSfx.Slide")
            || (motorSrc.Contains("AudioBus.Hook.SlideStart")
                && audioBus != null && audioBus.Contains("TagSfx.Slide"));
        if (!slideShot)
            report.Fail("existing slide one-shot was removed");
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

    static bool Has(string text, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
        {
            if (text.IndexOf(needles[i], StringComparison.Ordinal) < 0)
                return false;
        }
        return true;
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

public sealed class WallClingTellReport
{
    public int MarkCount;
    public float RibbonTime;
    public float RibbonWidth;
    public float ClimbHeight;
    public float RunHeight;
    public float MaxAlpha;
    public float JumpSpeed;
    public float SlideBoost;
    public float ClingGrace;
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
        text.Append(" cling marks=").Append(MarkCount.ToString(c));
        text.Append(" life_s=").Append(RibbonTime.ToString("0.00", c));
        text.Append(" width_m=").Append(RibbonWidth.ToString("0.00", c));
        text.Append(" climb_m=").Append(ClimbHeight.ToString("0.00", c));
        text.Append(" run_m=").Append(RunHeight.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        text.Append(" clingGrace=").Append(ClingGrace.ToString("0.00", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
