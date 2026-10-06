using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Art;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check: a slide draws two short ground ribbons at the heels,
/// and the pass did not retune slide, jump, or punch numbers.
/// </summary>
public static class SlideScrapeTellProof
{
    public static SlideScrapeTellReport Run()
    {
        var report = new SlideScrapeTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.RibbonCount = SlideScrapeTell.RibbonCount;
        report.RibbonTime = SlideScrapeTell.RibbonTime;
        report.RibbonWidth = SlideScrapeTell.RibbonWidth;
        report.RibbonHeight = SlideScrapeTell.RibbonHeight;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;

        if (SlideScrapeTell.RibbonCount != 2)
            report.Fail("scrape tell is not two ribbons");
        if (SlideScrapeTell.RibbonTime < 0.12f || SlideScrapeTell.RibbonTime > 0.18f)
            report.Fail("ribbon lifetime is outside 0.12-0.18s");
        if (SlideScrapeTell.RibbonWidth < 0.14f || SlideScrapeTell.RibbonWidth > 0.22f)
            report.Fail("ribbon width is a hairline or wide enough to hide the legs");
        if (SlideScrapeTell.RibbonHeight < 0.04f || SlideScrapeTell.RibbonHeight > 0.10f)
            report.Fail("ribbon is not a low ground scrape");
        if (SlideScrapeTell.FootOffset <= 0.08f || SlideScrapeTell.FootOffset >= cfg.radius)
            report.Fail("ribbons are not under the feet");
        if (SlideScrapeTell.TrailBack < 0.10f || SlideScrapeTell.TrailBack > 0.35f)
            report.Fail("scrape is not a short heel mark");
        if (SlideScrapeTell.PairSpan >= cfg.radius * 2f)
            report.Fail("scrape pair is wider than the capsule");
        if (SlideScrapeTell.RibbonTime * cfg.sprintSpeed > 2.2f)
            report.Fail("ribbon is long enough to read as extra speed");
        if (SlideScrapeTell.VerticalImpulse != 0f)
            report.Fail("scrape tell adds a vertical impulse");

        Vector3 origin = new Vector3(2f, 0f, 4f);
        if (!SlideScrapeTell.Place(origin, Vector3.forward, out Vector3 left, out Vector3 right))
            report.Fail("forward slide did not place a scrape");
        if (Mathf.Abs(left.y - (origin.y + SlideScrapeTell.RibbonHeight)) > 0.001f
            || Mathf.Abs(right.y - (origin.y + SlideScrapeTell.RibbonHeight)) > 0.001f)
            report.Fail("ribbons are not at scrape height");
        if (left.y > 0.12f || right.y > 0.12f)
            report.Fail("ribbons cover the torso");
        if (Mathf.Abs(left.z - (origin.z - SlideScrapeTell.TrailBack)) > 0.001f
            || Mathf.Abs(right.z - (origin.z - SlideScrapeTell.TrailBack)) > 0.001f)
            report.Fail("ribbons are not behind the pawn");
        if (left.z >= origin.z || right.z >= origin.z)
            report.Fail("ribbons lead the slide");
        if (Mathf.Abs((left.x + right.x) * 0.5f - origin.x) > 0.001f)
            report.Fail("ribbons are not centered on the pawn");
        if (Mathf.Abs(Mathf.Abs(left.x - origin.x) - SlideScrapeTell.FootOffset) > 0.001f
            || Mathf.Abs(Mathf.Abs(right.x - origin.x) - SlideScrapeTell.FootOffset) > 0.001f)
            report.Fail("ribbons are not at the feet");
        if (Mathf.Abs((left - right).magnitude - SlideScrapeTell.FootOffset * 2f) > 0.001f)
            report.Fail("the two ribbons do not match the foot split");

        if (!SlideScrapeTell.Place(origin, Vector3.right, out Vector3 sideL, out Vector3 sideR))
            report.Fail("a right slide did not place a scrape");
        if (Mathf.Abs(sideL.x - (origin.x - SlideScrapeTell.TrailBack)) > 0.001f
            || Mathf.Abs(sideR.x - (origin.x - SlideScrapeTell.TrailBack)) > 0.001f)
            report.Fail("a right slide did not leave dust behind the heels");
        if (sideL.x >= origin.x || sideR.x >= origin.x)
            report.Fail("a right slide put dust ahead of the pawn");
        if (Mathf.Abs(Mathf.Abs(sideL.z - origin.z) - SlideScrapeTell.FootOffset) > 0.001f
            || Mathf.Abs(Mathf.Abs(sideR.z - origin.z) - SlideScrapeTell.FootOffset) > 0.001f)
            report.Fail("a right slide did not split the ribbons across the feet");

        if (SlideScrapeTell.Place(origin, Vector3.zero, out _, out _))
            report.Fail("a zero slide direction drew a scrape");
        if (SlideScrapeTell.Place(origin, Vector3.up, out _, out _))
            report.Fail("a vertical direction drew a scrape");

        CheckLocks(report, cfg, punch, "defaults");

        string moveAsset = ReadRepo("Assets/Resources/TagArena/MovementConfig.asset");
        string punchAsset = ReadRepo("Assets/ScriptableObjects/PunchTagTuning.asset");
        if (moveAsset == null)
            report.Fail("MovementConfig asset missing");
        else if (!Has(moveAsset, "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
            "jumpSpeed: 24.7", "slideBoost: 0", "slideFlatFriction: 6.8", "slideDownhillAccel: 0",
            "enableJet: 0", "airDashDuration: 0.1", "airDashSpeed: 15", "airDashCooldown: 30"))
            report.Fail("MovementConfig asset drifted");
        if (punchAsset == null)
            report.Fail("PunchTagTuning asset missing");
        else if (!Has(punchAsset, "reach: 1.55", "ragdollDuration: 1.15", "windup: 0.12", "active: 0.1", "hitRecover: 0.15", "missRecover: 0.32"))
            report.Fail("PunchTagTuning asset drifted");

        string tellSrc = ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string binderSrc = ReadRepo("Assets/Scripts/Art/DummyAvatarBinder.cs");
        string animSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Animation/MoveAnimDriver.cs");
        if (tellSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || spawnSrc == null || binderSrc == null || animSrc == null)
            report.Fail("scrape sources missing");
        else
            CheckWiring(report, tellSrc, locoSrc, motorSrc, cfgSrc, spawnSrc, binderSrc, animSrc);

        if (VerbPoseClips.SlideBody != "SlideBody" || VerbPoseClips.PunchStrike != "PunchStrike" || VerbPoseClips.TagCatch != "TagCatch")
            report.Fail("verb clip names drifted");
        if (VerbPoseClips.ClipForState(VerbPoseClips.StateSlide) != VerbPoseClips.SlideBody)
            report.Fail("Slide does not play SlideBody");
        if (ReadRepo("Assets/Scripts/Art/TagLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandFlash.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/AirDashTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs") == null)
            report.Fail("an existing tell was removed");

        return report;
    }

    static void CheckLocks(SlideScrapeTellReport report, MovementConfig cfg, PunchTagTuning punch, string label)
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
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail(label + " punch reach is not 1.55");
        if (Mathf.Abs(punch.ragdollDuration - 1.15f) > 0.001f)
            report.Fail(label + " ragdoll duration changed");
        if (Mathf.Abs(punch.windup - 0.12f) > 0.001f || Mathf.Abs(punch.active - 0.10f) > 0.001f
            || Mathf.Abs(punch.hitRecover - 0.15f) > 0.001f || Mathf.Abs(punch.missRecover - 0.32f) > 0.001f)
            report.Fail(label + " punch timings changed");
    }

    static void CheckWiring(SlideScrapeTellReport report, string tellSrc, string locoSrc, string motorSrc, string cfgSrc, string spawnSrc, string binderSrc, string animSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop")
            || tellSrc.Contains("AddForce") || tellSrc.Contains("applyRootMotion"))
            report.Fail("scrape constants write feel or the camera");

        if (!Has(cfgSrc, "slideBoost = 0f", "jumpSpeed = 24.7f", "slideFlatFriction = 6.8f", "slideDownhillAccel = 0f"))
            report.Fail("movement defaults drifted");

        string enter = MethodBody(motorSrc, "void EnterSlide");
        string slideMove = MethodBody(motorSrc, "Vector3 SlideMove");
        if (enter == null || slideMove == null)
            report.Fail("slide motor methods missing");
        else
        {
            if (enter.Contains("cfg.slideBoost") || enter.Contains("SlideScrape"))
                report.Fail("slide enter adds boost or the tell");
            if (!enter.Contains("SetHoriz"))
                report.Fail("slide enter no longer carries planar speed");
            if (!slideMove.Contains("slideFlatFriction") || slideMove.Contains("SlideScrape"))
                report.Fail("slide friction path changed");
        }
        if (motorSrc.Contains("SlideScrape"))
            report.Fail("scrape tell leaked into the motor");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickSlideScrape");
        string begin = MethodBody(locoSrc, "void BeginSlideScrape");
        string end = MethodBody(locoSrc, "void EndSlideScrape");
        string place = MethodBody(locoSrc, "void PlaceSlideScrape");
        string make = MethodBody(locoSrc, "TrailRenderer MakeScrape");
        if (late == null || tick == null || begin == null || end == null || place == null || make == null)
        {
            report.Fail("scrape tell is not on the slide visual path");
            return;
        }

        int call = late.IndexOf("TickSlideScrape(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("scrape tell does not run with the slide state");
        if (!tick.Contains("MoveState.Slide") || !tick.Contains("BeginSlideScrape") || !tick.Contains("EndSlideScrape"))
            report.Fail("scrape tell does not follow slide enter and exit");
        if (!begin.Contains("PlaceSlideScrape") || !begin.Contains("ArmRibbon"))
            report.Fail("slide enter does not start the ribbons");
        if (!place.Contains("SlideScrapeTell.Place"))
            report.Fail("ribbons are not placed by the scrape builder");
        if (!end.Contains("emitting = false"))
            report.Fail("slide exit does not stop the ribbons");
        if (end.Contains("Clear("))
            report.Fail("slide exit wipes the short scrape");
        if (!make.Contains("SlideScrapeTell.RibbonTime") || !make.Contains("SlideScrapeTell.RibbonWidth"))
            report.Fail("ribbons do not use the tell constants");
        if (Count(locoSrc, "MakeScrape(\"") != SlideScrapeTell.RibbonCount)
            report.Fail("scrape tell does not build both ribbons");

        string scrapeBodies = tick + begin + end + make
            + (MethodBody(locoSrc, "void PlaceSlideScrape") ?? "")
            + (MethodBody(locoSrc, "void EnsureSlideScrape") ?? "")
            + (MethodBody(locoSrc, "void RefreshScrapeDir") ?? "")
            + (MethodBody(locoSrc, "static Material MakeScrapeMat") ?? "");
        if (scrapeBodies.Contains("slideBoost") || scrapeBodies.Contains("jumpSpeed") || scrapeBodies.Contains("CharacterController")
            || scrapeBodies.Contains("Rigidbody") || scrapeBodies.Contains("FovPop") || scrapeBodies.Contains("AddKick")
            || scrapeBodies.Contains("FixedUpdate") || scrapeBodies.Contains("applyRootMotion"))
            report.Fail("scrape visual writes feel or the camera");

        if (!spawnSrc.Contains("DummyAvatarBinder") || !binderSrc.Contains("DummyLocomotor"))
            report.Fail("both pawns are not given the locomotor that plays the scrape");
        if (animSrc.Contains("SlideScrape"))
            report.Fail("scrape tell was wired through the anim driver");
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

public sealed class SlideScrapeTellReport
{
    public int RibbonCount;
    public float RibbonTime;
    public float RibbonWidth;
    public float RibbonHeight;
    public float JumpSpeed;
    public float SlideBoost;
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
        text.Append(" scrape ribbons=").Append(RibbonCount.ToString(c));
        text.Append(" life_s=").Append(RibbonTime.ToString("0.00", c));
        text.Append(" width_m=").Append(RibbonWidth.ToString("0.00", c));
        text.Append(" height_m=").Append(RibbonHeight.ToString("0.00", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
