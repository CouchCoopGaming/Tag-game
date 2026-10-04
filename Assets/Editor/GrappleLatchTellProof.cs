using System;
using System.Globalization;
using System.IO;
using System.Text;
using Tag.Art;
using Tag.Gameplay;
using Tag.Local;
using TagArena.Movement;
using UnityEngine;

/// <summary>
/// Headless check: a planar latch flashes the knot and pulses the rope for a
/// short window, and a miss stays quiet. Jump, dash, cling, and punch stay put.
/// </summary>
public static class GrappleLatchTellProof
{
    const float Dt = 1f / 60f;

    public static GrappleLatchTellReport Run()
    {
        var report = new GrappleLatchTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.FlashSeconds = GrappleLatchTell.FlashSeconds;
        report.MaxAlpha = GrappleLatchTell.MaxAlpha;
        report.KnotSize = GrappleLatchTell.KnotSize;
        report.PulseLength = GrappleLatchTell.PulseLength;
        report.JumpSpeed = cfg.jumpSpeed;

        if (GrappleLatchTell.FlashSeconds < 0.22f || GrappleLatchTell.FlashSeconds > 0.28f)
            report.Fail("latch flash is outside 0.22-0.28s");
        if (GrappleLatchTell.MaxAlpha < 0.35f || GrappleLatchTell.MaxAlpha > 0.50f)
            report.Fail("latch alpha hides the body or is too faint to read");
        if (Mathf.Abs(GrappleLatchTell.Alpha(0f) - GrappleLatchTell.MaxAlpha) > 0.001f)
            report.Fail("latch does not open at peak alpha");
        if (GrappleLatchTell.Alpha(GrappleLatchTell.FlashSeconds) != 0f || GrappleLatchTell.Alpha(-1f) != 0f)
            report.Fail("latch alpha stays up after the window");
        if (GrappleLatchTell.Alpha(0.28f) != 0f)
            report.Fail("latch alpha is still up at 0.28s");
        if (GrappleLatchTell.KnotSize <= GrappleRopeTell.HookMarkerSize)
            report.Fail("knot flash is no bigger than the resting knot");
        if (GrappleLatchTell.KnotSize > 0.85f)
            report.Fail("knot flash is large enough to hide the attach");
        if (GrappleLatchTell.PulseWidth <= GrappleRopeTell.RopeStartWidth || GrappleLatchTell.PulseWidth > 0.32f)
            report.Fail("pulse is a hairline or wide enough to hide the rope");
        if (GrappleLatchTell.PulseLength < 0.6f || GrappleLatchTell.PulseLength > 2.0f)
            report.Fail("pulse is a speck or long enough to be a second rope");
        if (GrappleLatchTell.MarkR > 0.97f && GrappleLatchTell.MarkG > 0.97f && GrappleLatchTell.MarkB > 0.97f)
            report.Fail("latch flash is white enough to wash the rope");
        if (GrappleLatchTell.MarkR < 0.75f || GrappleLatchTell.MarkR <= GrappleLatchTell.MarkB)
            report.Fail("latch flash is not the rope gold");
        if (GrappleLatchTell.Glow != 0f)
            report.Fail("latch flash glows");
        if (GrappleLatchTell.VerticalImpulse != 0f)
            report.Fail("latch flash adds a vertical impulse");
        if (Mathf.Abs(GrappleRopeTell.AimLength - 2.4f) > 0.001f)
            report.Fail("aim line length changed");
        if (GrappleRopeTell.VerticalImpulse != 0f)
            report.Fail("rope tell adds a vertical impulse");

        Vector3 origin = new Vector3(2f, 1f, 4f);
        Vector3 hand = GrappleRopeTell.Hand(origin, Vector3.forward);
        Vector3 anchor = new Vector3(2f, 4f, 14f);
        if (!GrappleRopeTell.AttachedSpan(origin, Vector3.forward, anchor, out Vector3 spanHand, out Vector3 spanEnd))
            report.Fail("a latched hit did not keep its rope span");
        if ((spanHand - hand).sqrMagnitude > 0.0001f || (spanEnd - anchor).sqrMagnitude > 0.0001f)
            report.Fail("latch flash is not on the existing rope path");

        float age = -1f;
        GrappleLatchTell.Arm(ref age);
        if (!GrappleLatchTell.Show(true, age))
            report.Fail("a successful latch did not start the flash");
        if (!GrappleLatchTell.Knot(true, anchor, age, out Vector3 knotAt) || (knotAt - anchor).sqrMagnitude > 0.0001f)
            report.Fail("knot flash is not on the latch point");
        if (!GrappleLatchTell.Pulse(true, hand, anchor, age, out Vector3 pulseFrom, out Vector3 pulseTo))
            report.Fail("a successful latch did not pulse the rope");
        if ((pulseFrom - hand).sqrMagnitude > 0.0001f)
            report.Fail("pulse does not start at the hand");
        if (!OnSpan(hand, anchor, pulseFrom) || !OnSpan(hand, anchor, pulseTo))
            report.Fail("pulse leaves the rope");

        float shown = 0f;
        int guard = 0;
        while (GrappleLatchTell.Show(true, age))
        {
            if (!GrappleLatchTell.Knot(true, anchor, age, out knotAt) || (knotAt - anchor).sqrMagnitude > 0.0001f)
                report.Fail("knot flash left the latch point");
            if (!GrappleLatchTell.Pulse(true, hand, anchor, age, out pulseFrom, out pulseTo))
                report.Fail("pulse dropped during the latch window");
            else if (!OnSpan(hand, anchor, pulseFrom) || !OnSpan(hand, anchor, pulseTo))
                report.Fail("pulse left the rope during the latch window");
            shown += Dt;
            GrappleLatchTell.Step(ref age, Dt, true);
            if (++guard > 60)
            {
                report.Fail("latch flash did not stop");
                break;
            }
        }

        report.ShownSeconds = shown;
        if (shown < 0.22f || shown > 0.28f)
            report.Fail("stepped latch flash is outside 0.22-0.28s");
        if (Mathf.Abs(shown - GrappleLatchTell.FlashSeconds) > Dt + 0.001f)
            report.Fail("stepped latch flash does not match the window");
        float endAge = GrappleLatchTell.FlashSeconds - 0.0001f;
        if (!GrappleLatchTell.Show(true, endAge)
            || !GrappleLatchTell.Pulse(true, hand, anchor, endAge, out Vector3 endFrom, out Vector3 endTo))
            report.Fail("pulse dropped at the end of the window");
        else if ((endTo - anchor).sqrMagnitude > 0.0001f || !OnSpan(hand, anchor, endFrom))
            report.Fail("pulse did not reach the knot");
        if (GrappleLatchTell.Show(true, age) || age >= 0f)
            report.Fail("latch flash stayed armed after it stopped");
        if (GrappleLatchTell.Pulse(true, hand, anchor, age, out _, out _))
            report.Fail("pulse kept going after the window");
        if (GrappleLatchTell.Step(ref age, Dt, true))
            report.Fail("a finished latch restarted itself");

        if (GrappleLatchTell.Show(true, 0f) != true)
            report.Fail("age 0 of a latch is quiet");
        if (!GrappleLatchTell.Show(true, GrappleLatchTell.FlashSeconds - 0.001f))
            report.Fail("the flash ends before the window");
        if (GrappleLatchTell.Show(true, GrappleLatchTell.FlashSeconds))
            report.Fail("the flash stays up at the end of the window");
        if (GrappleLatchTell.Show(true, 0.28f))
            report.Fail("the flash is still up at 0.28s");

        report.MissFrames = CountQuiet(false, hand, anchor, report, "miss");
        report.AimFrames = CountQuiet(false, hand, anchor, report, "aim");
        if (report.MissFrames != 0)
            report.Fail("a miss drew a latch flash");
        if (report.AimFrames != 0)
            report.Fail("aim drew a latch flash");
        if (GrappleLatchTell.Show(false, 0f) || GrappleLatchTell.Show(false, 0.10f))
            report.Fail("an unlatched age drew a flash");
        if (GrappleLatchTell.Knot(false, anchor, 0f, out _))
            report.Fail("a miss placed a knot flash");
        if (GrappleLatchTell.Pulse(false, hand, anchor, 0.10f, out _, out _))
            report.Fail("a miss pulsed the rope");

        Vector3 same = hand;
        if (GrappleLatchTell.Pulse(true, hand, same, 0f, out _, out _))
            report.Fail("a zero-length rope pulsed");

        Vector3 steered = KinematicStep.GrappleHorizontal(
            new Vector3(6f, 9f, 0f), Vector3.zero, new Vector3(0f, 2f, 8f), 10f, 0.35f);
        if (steered.y != 0f)
            report.Fail("rope horizontal wrote vertical speed");

        CheckLocks(report, cfg, punch);

        string moveAsset = ReadRepo("Assets/Resources/TagArena/MovementConfig.asset");
        string punchAsset = ReadRepo("Assets/ScriptableObjects/PunchTagTuning.asset");
        if (moveAsset == null)
            report.Fail("MovementConfig asset missing");
        else if (!Has(moveAsset, "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
            "jumpSpeed: 24.7", "slideBoost: 0", "enableJet: 0",
            "airDashDuration: 0.1", "airDashSpeed: 15", "airDashCooldown: 30",
            "taggerLungeSpeed: 16", "taggerLungeDuration: 0.2", "taggerLungeCooldown: 1"))
            report.Fail("MovementConfig asset drifted");
        if (punchAsset == null)
            report.Fail("PunchTagTuning asset missing");
        else if (!Has(punchAsset, "reach: 1.55"))
            report.Fail("PunchTagTuning reach drifted");

        string tellSrc = ReadRepo("Assets/Scripts/Art/GrappleLatchTell.cs");
        string ropeSrc = ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs");
        string grappleSrc = ReadRepo("Assets/Scripts/Experimental/ExperimentalGrapple.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string lungeSrc = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        if (tellSrc == null || ropeSrc == null || grappleSrc == null || motorSrc == null || cfgSrc == null
            || spawnSrc == null || lungeSrc == null)
            report.Fail("latch sources missing");
        else
            CheckWiring(report, tellSrc, ropeSrc, grappleSrc, motorSrc, cfgSrc, spawnSrc, lungeSrc);

        if (VerbPoseClips.SlideBody != "SlideBody" || VerbPoseClips.PunchStrike != "PunchStrike" || VerbPoseClips.TagCatch != "TagCatch")
            report.Fail("verb clip names drifted");
        if (ReadRepo("Assets/Scripts/Art/OpponentChaseTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/WallClingTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandFlash.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/AirDashTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/ItMarker.cs") == null)
            report.Fail("an existing tell was removed");

        if (!SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn grapple is off");
        if (SoloGrappleGate.EnableFor(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.OpponentPawnName))
            report.Fail("opponent gained the solo grapple");

        return report;
    }

    static int CountQuiet(bool latched, Vector3 hand, Vector3 anchor, GrappleLatchTellReport report, string label)
    {
        float age = -1f;
        int shown = 0;
        for (int i = 0; i < 40; i++)
        {
            if (GrappleLatchTell.Show(latched, age))
                shown++;
            if (GrappleLatchTell.Knot(latched, anchor, age, out _))
                report.Fail(label + " placed a knot flash");
            if (GrappleLatchTell.Pulse(latched, hand, anchor, age, out _, out _))
                report.Fail(label + " pulsed the rope");
            if (GrappleLatchTell.Step(ref age, Dt, latched))
                report.Fail(label + " stepped into a flash");
            if (age >= 0f)
                report.Fail(label + " armed a flash");
        }

        return shown;
    }

    static bool OnSpan(Vector3 hand, Vector3 knot, Vector3 point)
    {
        Vector3 span = knot - hand;
        float len = span.magnitude;
        if (len < 1e-4f) return false;
        Vector3 dir = span * (1f / len);
        float t = Vector3.Dot(point - hand, dir);
        if (t < -0.02f || t > len + 0.02f) return false;
        Vector3 closest = hand + dir * t;
        Vector3 off = point - closest;
        return off.sqrMagnitude < 0.0001f;
    }

    static void CheckLocks(GrappleLatchTellReport report, MovementConfig cfg, PunchTagTuning punch)
    {
        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f)
            report.Fail("coyote is not 0.10");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f)
            report.Fail("jump buffer is not 0.16");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f)
            report.Fail("cling grace is not 0.08");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f)
            report.Fail("jumpSpeed changed");
        if (cfg.slideBoost != 0f)
            report.Fail("slideBoost is not 0");
        if (cfg.enableJet)
            report.Fail("jet is on");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f || Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f
            || Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail("air dash numbers changed");
        if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f)
            report.Fail("taggerLungeSpeed is not 16");
        if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f)
            report.Fail("lunge duration is not 0.20");
        if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f)
            report.Fail("lunge cooldown is not 1");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail("punch reach is not 1.55");
    }

    static void CheckWiring(
        GrappleLatchTellReport report,
        string tellSrc, string ropeSrc, string grappleSrc, string motorSrc, string cfgSrc, string spawnSrc, string lungeSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera") || tellSrc.Contains("InputAction")
            || tellSrc.Contains("maxRange"))
            report.Fail("latch constants write feel, input, or the camera");

        if (!Has(cfgSrc, "jumpSpeed = 24.7f", "slideBoost = 0f", "clingReleaseGrace = 0.08f",
            "coyoteTime = 0.10f", "jumpBuffer = 0.16f", "enableJet = false",
            "airDashDuration = 0.10f", "airDashSpeed = 15f", "airDashCooldown = 30f",
            "taggerLungeSpeed = 16f", "taggerLungeDuration = 0.20f", "taggerLungeCooldown = 1.0f"))
            report.Fail("movement defaults drifted");
        if (!lungeSrc.Contains("LeadSeconds = 0.45f"))
            report.Fail("LungeTell lead is not 0.45s");
        if (!ropeSrc.Contains("AimLength = 2.4f"))
            report.Fail("aim line is no longer 2.4 m");

        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("GrappleLatch") || motorSrc.Contains("FixedUpdate"))
            report.Fail("latch flash leaked into the motor");
        if (!grappleSrc.Contains("maxRange = 28f") || !grappleSrc.Contains("attachSlack = 0.35f")
            || !grappleSrc.Contains("FireButton = \"RMB\"") || !grappleSrc.Contains("enableGrapple = false"))
            report.Fail("grapple range, slack, or button changed");
        if (Count(grappleSrc, "RaycastNonAlloc(") != 1)
            report.Fail("latch flash added a second ray");
        if (grappleSrc.Contains("InputAction") || grappleSrc.Contains("FovPop") || grappleSrc.Contains("fieldOfView")
            || grappleSrc.Contains("AddForce") || grappleSrc.Contains("jumpSpeed"))
            report.Fail("grapple visual pass wrote input, camera, or motor code");

        string tryAttach = MethodBody(grappleSrc, "void TryAttach");
        string late = MethodBody(grappleSrc, "void LateUpdate");
        string release = MethodBody(grappleSrc, "void Release");
        string tick = MethodBody(grappleSrc, "void TickLatchFlash");
        string knot = MethodBody(grappleSrc, "Transform MakeLatchKnot");
        string pulse = MethodBody(grappleSrc, "LineRenderer MakeLatchPulse");
        string mat = MethodBody(grappleSrc, "static Material MakeLatchMat");
        string sphere = MethodBody(grappleSrc, "static Mesh LatchSphere");
        if (tryAttach == null || late == null || release == null || tick == null || knot == null || pulse == null
            || mat == null || sphere == null)
        {
            report.Fail("latch flash is not on the grapple visual path");
            return;
        }

        int miss = tryAttach.IndexOf("if (best < 0) return", StringComparison.Ordinal);
        int tooShort = tryAttach.IndexOf("if (_ropeLength <= 0.05f) return", StringComparison.Ordinal);
        int armed = tryAttach.IndexOf("_attached = true", StringComparison.Ordinal);
        int arm = tryAttach.IndexOf("GrappleLatchTell.Arm", StringComparison.Ordinal);
        if (miss < 0 || tooShort < 0 || armed < 0 || arm < 0 || !(miss < tooShort && tooShort < armed && armed < arm))
            report.Fail("latch flash does not wait for a real attach");
        if (Count(grappleSrc, "GrappleLatchTell.Arm") != 1)
            report.Fail("latch flash arms more than once");
        if (tryAttach.Contains("GrappleLatchTell.Pulse") || tryAttach.Contains("GrappleLatchTell.Knot"))
            report.Fail("the ray path draws the flash itself");

        int tickAt = late.IndexOf("TickLatchFlash(", StringComparison.Ordinal);
        int hideAt = late.IndexOf("HideLatchFlash(", StringComparison.Ordinal);
        int aimAt = late.IndexOf("GrappleRopeTell.AimSpan", StringComparison.Ordinal);
        if (tickAt < 0 || hideAt < 0 || aimAt < 0 || !(tickAt < hideAt && hideAt < aimAt))
            report.Fail("aim draws the latch flash");
        if (late.IndexOf("TickLatchFlash(", tickAt + 1, StringComparison.Ordinal) >= 0)
            report.Fail("latch flash ticks off the attached rope");
        if (!late.Contains("GrappleRopeTell.AttachedSpan"))
            report.Fail("latch flash is not on the attached rope span");
        if (!release.Contains("GrappleLatchTell.Clear"))
            report.Fail("releasing the rope leaves the flash armed");

        if (!tick.Contains("GrappleLatchTell.Show") || !tick.Contains("GrappleLatchTell.Knot")
            || !tick.Contains("GrappleLatchTell.Pulse") || !tick.Contains("GrappleLatchTell.Step")
            || !tick.Contains("GrappleLatchTell.Alpha"))
            report.Fail("latch flash does not use the tell timing");
        if (!pulse.Contains("GrappleLatchTell.PulseWidth") || !pulse.Contains("GrappleLatchTell.MaxAlpha"))
            report.Fail("pulse does not use the tell width and alpha");
        if (!knot.Contains("GrappleLatchTell.KnotSize") || !knot.Contains("MeshFilter") || !knot.Contains("MeshRenderer"))
            report.Fail("knot flash is not a bare mesh");

        string bodies = tick + knot + pulse + mat + sphere
            + (MethodBody(grappleSrc, "void PlaceLatchKnot") ?? "")
            + (MethodBody(grappleSrc, "void PlaceLatchPulse") ?? "")
            + (MethodBody(grappleSrc, "void HideLatchFlash") ?? "")
            + (MethodBody(grappleSrc, "void EnsureLatchFlash") ?? "")
            + (MethodBody(grappleSrc, "void PaintLatch") ?? "");
        if (bodies.Contains("CreatePrimitive") || bodies.Contains("Collider") || bodies.Contains("Rigidbody")
            || bodies.Contains("AddComponent<Light>") || bodies.Contains("Light")
            || bodies.Contains("_EmissionColor") || bodies.Contains("_EMISSION")
            || bodies.Contains("FovPop") || bodies.Contains("fieldOfView") || bodies.Contains("InputAction")
            || bodies.Contains("AddForce") || bodies.Contains("jumpSpeed") || bodies.Contains("maxRange"))
            report.Fail("latch flash adds a glow, a volume, or a feel write");

        if (!spawnSrc.Contains("SoloGrappleGate.EnableFor") || !spawnSrc.Contains("enableGrapple = false"))
            report.Fail("solo grapple gate is no longer what turns the rope on");
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

public sealed class GrappleLatchTellReport
{
    public float FlashSeconds;
    public float ShownSeconds;
    public float MaxAlpha;
    public float KnotSize;
    public float PulseLength;
    public int MissFrames;
    public int AimFrames;
    public float JumpSpeed;
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
        text.Append(" latch flash_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" shown_s=").Append(ShownSeconds.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" knot=").Append(KnotSize.ToString("0.00", c));
        text.Append(" pulse_m=").Append(PulseLength.ToString("0.00", c));
        text.Append(" miss=").Append(MissFrames.ToString(c));
        text.Append(" aim=").Append(AimFrames.ToString(c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
