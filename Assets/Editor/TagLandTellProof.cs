using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Art;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check: a tag that transfers It flashes two rings for a short beat,
/// and the pass did not retune jump height, slide, or punch numbers.
/// </summary>
public static class TagLandTellProof
{
    public static TagLandTellReport Run()
    {
        var report = new TagLandTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.FlashSeconds = TagLandTell.FlashSeconds;
        report.RingCount = TagLandTell.RingCount;
        report.InnerRadius = TagLandTell.InnerRadius;
        report.OuterRadius = TagLandTell.OuterRadius;
        report.LabelSeconds = TagLandTell.LabelSeconds;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;
        report.Reach = punch.reach;
        report.At25 = TagLandTell.AngularDiameterDeg(TagLandTell.OuterRadius, 25f);

        if (TagLandTell.FlashSeconds < 0.25f || TagLandTell.FlashSeconds > 0.45f)
            report.Fail("flash lifetime is outside 0.25-0.45s");
        if (TagLandTell.RingCount < 1 || TagLandTell.RingCount > 2)
            report.Fail("ring count is not 1 or 2");
        if (TagLandTell.LabelSeconds < 0.75f || TagLandTell.LabelSeconds > 0.90f)
            report.Fail("label lifetime is not about 0.8s");
        if (TagLandTell.InnerRadius <= cfg.radius)
            report.Fail("inner ring sits inside the capsule");
        if (TagLandTell.OuterRadius <= TagLandTell.InnerRadius)
            report.Fail("outer ring is not outside the inner ring");
        if (TagLandTell.InnerHeight > 0.35f || TagLandTell.OuterHeight > 0.35f)
            report.Fail("rings cover the torso");
        if (TagLandTell.LabelHeight < 1.9f || TagLandTell.LabelHeight > 3.0f)
            report.Fail("label is not above the head");
        if (TagLandTell.LabelForward <= cfg.radius || TagLandTell.LabelForward > 1.2f)
            report.Fail("label is not just off the chest toward the camera");
        if (TagLandTell.VerticalImpulse != 0f)
            report.Fail("tag land tell adds a vertical impulse");
        if (TagLandTell.Cue != "BecomeIt")
            report.Fail("tag land cue is not the existing BecomeIt chirp");
        if (TagLandTell.VictimLine != "TAGGED" || TagLandTell.ItLine != "YOU'RE IT")
            report.Fail("handoff label drifted");

        float near = TagLandTell.AngularDiameterDeg(TagLandTell.OuterRadius, 15f);
        float mid = TagLandTell.AngularDiameterDeg(TagLandTell.OuterRadius, 20f);
        float far = report.At25;
        if (!(near > mid && mid > far))
            report.Fail("ring does not read larger as the camera closes");
        if (far < 9f)
            report.Fail("outer ring is under 9 degrees at 25 m");
        if (near < 14f)
            report.Fail("outer ring is under 14 degrees at 15 m");

        if (Mathf.Abs(TagLandTell.RingScale(TagLandTell.OuterRadius) - TagLandTell.OuterRadius * 2f) > 0.001f)
            report.Fail("ring scale is not the cylinder diameter");

        Vector3 feet = new Vector3(2f, 0f, 4f);
        Vector3 ring = TagLandTell.RingCenter(feet, TagLandTell.OuterHeight);
        if (Mathf.Abs(ring.x - feet.x) > 0.001f || Mathf.Abs(ring.z - feet.z) > 0.001f
            || Mathf.Abs(ring.y - TagLandTell.OuterHeight) > 0.001f)
            report.Fail("ring is not on the tagged feet");

        Vector3 label = TagLandTell.LabelPoint(feet, new Vector3(0f, 0f, -1f));
        if (Mathf.Abs(label.x - feet.x) > 0.001f
            || Mathf.Abs(label.y - (feet.y + TagLandTell.LabelHeight)) > 0.001f
            || Mathf.Abs(label.z - (feet.z - TagLandTell.LabelForward)) > 0.001f)
            report.Fail("label is not on the chase-cam side of the tagged body");

        Vector3 side = TagLandTell.LabelPoint(feet, Vector3.right);
        if (Mathf.Abs(side.x - (feet.x + TagLandTell.LabelForward)) > 0.001f || Mathf.Abs(side.z - feet.z) > 0.001f)
            report.Fail("a camera on the right did not pull the label toward it");

        Vector3 fallback = TagLandTell.LabelPoint(feet, Vector3.zero);
        if (Mathf.Abs(fallback.z - (feet.z - TagLandTell.LabelForward)) > 0.001f)
            report.Fail("a missing camera dropped the label into the body");

        if (!TagLandTell.Transferred(true, true, false))
            report.Fail("a real It swap did not count as a tag land");
        if (TagLandTell.Transferred(true, true, true))
            report.Fail("a punch that left the puncher It still flashed");
        if (TagLandTell.Transferred(true, false, false))
            report.Fail("a hit that did not make the victim It still flashed");
        if (TagLandTell.Transferred(false, true, false))
            report.Fail("a swap with no It puncher still flashed");

        CheckLocks(report, cfg, punch, "defaults");
        string moveAsset = ReadRepo("Assets/Resources/TagArena/MovementConfig.asset");
        string punchAsset = ReadRepo("Assets/ScriptableObjects/PunchTagTuning.asset");
        if (moveAsset == null)
            report.Fail("MovementConfig asset missing");
        else if (!Has(moveAsset, "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08", "jumpSpeed: 24.7", "slideBoost: 0"))
            report.Fail("MovementConfig asset drifted");
        if (punchAsset == null)
            report.Fail("PunchTagTuning asset missing");
        else if (!Has(punchAsset, "reach: 1.55", "ragdollDuration: 1.15", "windup: 0.12", "active: 0.1", "hitRecover: 0.15", "missRecover: 0.32"))
            report.Fail("PunchTagTuning asset drifted");

        string punchSrc = ReadRepo("Assets/Scripts/Tag/PunchHitbox.cs");
        string flashSrc = ReadRepo("Assets/Scripts/Art/TagLandFlash.cs");
        string itSrc = ReadRepo("Assets/Scripts/Tag/ItController.cs");
        string markerSrc = ReadRepo("Assets/Scripts/Art/ItMarker.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string bootSrc = ReadRepo("Assets/Scripts/Art/PlayVisualBootstrap.cs");
        if (punchSrc == null || flashSrc == null || itSrc == null || markerSrc == null || spawnSrc == null || bootSrc == null)
            report.Fail("tell sources missing");
        else
            CheckWiring(report, punchSrc, flashSrc, itSrc, markerSrc, spawnSrc, bootSrc);

        return report;
    }

    static void CheckLocks(TagLandTellReport report, MovementConfig cfg, PunchTagTuning punch, string label)
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
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail(label + " punch reach is not 1.55");
        if (Mathf.Abs(punch.ragdollDuration - 1.15f) > 0.001f)
            report.Fail(label + " ragdoll duration changed");
        if (Mathf.Abs(punch.windup - 0.12f) > 0.001f || Mathf.Abs(punch.active - 0.10f) > 0.001f
            || Mathf.Abs(punch.hitRecover - 0.15f) > 0.001f || Mathf.Abs(punch.missRecover - 0.32f) > 0.001f)
            report.Fail(label + " punch timings changed");
    }

    static void CheckWiring(TagLandTellReport report, string punchSrc, string flashSrc, string itSrc, string markerSrc, string spawnSrc, string bootSrc)
    {
        string resolve = MethodBody(punchSrc, "void ResolveHit");
        string active = MethodBody(punchSrc, "void TickActive");
        if (resolve == null || active == null)
        {
            report.Fail("punch methods missing");
            return;
        }

        if (!resolve.Contains("TagLandTell.Transferred") || !resolve.Contains("TagLandFlash.PlayOn"))
            report.Fail("successful tag does not play the tell");
        int gate = resolve.IndexOf("TagLandTell.Transferred", StringComparison.Ordinal);
        int play = resolve.IndexOf("TagLandFlash.PlayOn", StringComparison.Ordinal);
        if (gate < 0 || play < gate)
            report.Fail("tell plays before the It-swap check");
        if (Count(punchSrc, "TagLandFlash.PlayOn") != 1)
            report.Fail("tell is wired more than once");
        if (active.Contains("TagLand"))
            report.Fail("a miss plays the tag land tell");
        string audioBus = ReadRepo("Assets/Scripts/Audio/AudioBus.cs");
        bool punchSound = resolve.Contains("TagSfx.PunchConnect")
            || (resolve.Contains("AudioBus.Hook.PunchHit") && audioBus != null && audioBus.Contains("TagSfx.PunchConnect"));
        if (!punchSound)
            report.Fail("punch connect sound was removed");
        bool become = itSrc.Contains("TagSfx.BecomeIt")
            || (itSrc.Contains("AudioBus.Hook.Tag") && audioBus != null && audioBus.Contains("TagSfx.BecomeIt"));
        if (!become)
            report.Fail("SetIt no longer plays BecomeIt");
        if (!markerSrc.Contains("_pop = 1f"))
            report.Fail("ItMarker handoff pop is gone");

        if (!flashSrc.Contains("TagLandTell.FlashSeconds") || !flashSrc.Contains("TagLandTell.OuterRadius")
            || !flashSrc.Contains("TagLandTell.InnerRadius") || !flashSrc.Contains("TagLandTell.LabelSeconds")
            || !flashSrc.Contains("TagLandTell.VictimLine") || !flashSrc.Contains("TagLandTell.ItLine"))
            report.Fail("flash does not use the tell constants");
        if (Count(flashSrc, "MakeRing(\"") < TagLandTell.RingCount)
            report.Fail("flash does not build both rings");
        if (!flashSrc.Contains("DestroyImmediate"))
            report.Fail("rings keep their primitive collider");
        if (flashSrc.Contains("AddKick") || flashSrc.Contains("FovPop") || flashSrc.Contains("FixedUpdate")
            || flashSrc.Contains("CharacterController") || flashSrc.Contains("jumpSpeed") || flashSrc.Contains("slideBoost"))
            report.Fail("tell writes feel or the camera");
        if (flashSrc.Contains("TagSfx"))
            report.Fail("tell stacks a second chirp on BecomeIt");

        if (!spawnSrc.Contains("TagLandFlash") || !bootSrc.Contains("TagLandFlash"))
            report.Fail("both pawns are not given the tell");

        string patrol = ReadRepo("Assets/Scripts/Modes/DummyPatrol.cs");
        string motor = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string rope = ReadRepo("Assets/Scripts/Experimental/ExperimentalGrapple.cs");
        if (patrol == null || motor == null || rope == null)
            report.Fail("motor sources missing");
        else if (patrol.Contains("TagLand") || motor.Contains("TagLand") || rope.Contains("TagLand"))
            report.Fail("tag land tell leaked into patrol, motor, or grapple");
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

public sealed class TagLandTellReport
{
    public float FlashSeconds;
    public int RingCount;
    public float InnerRadius;
    public float OuterRadius;
    public float LabelSeconds;
    public float JumpSpeed;
    public float SlideBoost;
    public float Reach;
    public float At25;
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
        text.Append(" tagland flash_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" rings=").Append(RingCount.ToString(c));
        text.Append(" inner_m=").Append(InnerRadius.ToString("0.00", c));
        text.Append(" outer_m=").Append(OuterRadius.ToString("0.00", c));
        text.Append(" at25_deg=").Append(At25.ToString("0.00", c));
        text.Append(" label_s=").Append(LabelSeconds.ToString("0.00", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        text.Append(" reach=").Append(Reach.ToString("0.00", c));
        text.Append(" cue=").Append(TagLandTell.Cue);
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
