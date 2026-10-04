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
/// Headless check: the solo pawn draws a refill arc while air dash is cooling,
/// then a short ready wink, and then stops. Opponent and couch stay quiet.
/// Jump, dash, cling, and punch stay put.
/// </summary>
public static class AirDashCooldownTellProof
{
    const float Dt = 1f / 60f;

    public static AirDashCooldownTellReport Run()
    {
        var report = new AirDashCooldownTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.RingRadius = AirDashCooldownTell.RingRadius;
        report.RingHeight = AirDashCooldownTell.RingHeight;
        report.RingWidth = AirDashCooldownTell.RingWidth;
        report.MinArc = AirDashCooldownTell.MinArc;
        report.MaxAlpha = AirDashCooldownTell.MaxAlpha;
        report.FlashSeconds = AirDashCooldownTell.ReadyFlashSeconds;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;

        if (AirDashCooldownTell.ReadyFlashSeconds < 0.20f || AirDashCooldownTell.ReadyFlashSeconds > 0.30f)
            report.Fail("ready flash is outside 0.20-0.30s");
        if (AirDashCooldownTell.MaxAlpha < 0.40f || AirDashCooldownTell.MaxAlpha > 0.45f)
            report.Fail("dash cooldown alpha hides the body or is too faint to read");
        if (Mathf.Abs(AirDashCooldownTell.FlashAlpha(0f) - AirDashCooldownTell.MaxAlpha) > 0.001f)
            report.Fail("ready flash does not open at peak alpha");
        if (AirDashCooldownTell.FlashAlpha(AirDashCooldownTell.ReadyFlashSeconds) != 0f
            || AirDashCooldownTell.FlashAlpha(-1f) != 0f)
            report.Fail("ready flash alpha stays up after the window");
        if (AirDashCooldownTell.FlashAlpha(0.31f) != 0f)
            report.Fail("ready flash alpha is still up at 0.31s");
        if (AirDashCooldownTell.RingRadius <= cfg.radius)
            report.Fail("cooldown ring sits inside the capsule");
        if (AirDashCooldownTell.RingRadius - AirDashCooldownTell.RingWidth * 0.5f <= cfg.radius)
            report.Fail("cooldown stroke crosses the capsule");
        if (AirDashCooldownTell.RingRadius > 0.85f)
            report.Fail("cooldown ring is large enough to leave the pawn");
        if (AirDashCooldownTell.RingHeight < 0.12f || AirDashCooldownTell.RingHeight > 0.55f)
            report.Fail("cooldown ring covers the ground or the chest");
        if (AirDashCooldownTell.RingWidth < 0.02f || AirDashCooldownTell.RingWidth > 0.10f)
            report.Fail("cooldown stroke is a hairline or wide enough to hide the leg");
        if (AirDashCooldownTell.MinArc < 0.04f || AirDashCooldownTell.MinArc > 0.12f)
            report.Fail("the refill arc starts empty or already looks ready");
        if (Mathf.Abs(AirDashCooldownTell.VisibleFill(0f) - AirDashCooldownTell.MinArc) > 0.001f)
            report.Fail("a fresh cooldown draws no arc");
        if (Mathf.Abs(AirDashCooldownTell.VisibleFill(1f) - 1f) > 0.001f)
            report.Fail("a finished cooldown does not close the ring");
        float startSweep = AirDashCooldownTell.VisibleFill(0f) * 360f;
        if (startSweep < 12f || startSweep > 40f)
            report.Fail("the opening arc is a speck or already a ring");
        if (AirDashCooldownTell.MarkR > 0.97f && AirDashCooldownTell.MarkG > 0.97f && AirDashCooldownTell.MarkB > 0.97f)
            report.Fail("cooldown ring is white enough to wash the body");
        if (AirDashCooldownTell.MarkB < 0.70f || AirDashCooldownTell.MarkG <= AirDashCooldownTell.MarkR)
            report.Fail("cooldown ring is not the dash cyan");
        if (AirDashCooldownTell.Glow != 0f)
            report.Fail("cooldown ring glows");
        if (Mathf.Abs(AirDashCooldownTell.Fill(30f, 30f)) > 0.001f)
            report.Fail("a full cooldown reads as ready");
        if (Mathf.Abs(AirDashCooldownTell.Fill(15f, 30f) - 0.5f) > 0.001f)
            report.Fail("half cooldown is not half full");
        if (Mathf.Abs(AirDashCooldownTell.Fill(0f, 30f) - 1f) > 0.001f)
            report.Fail("a finished cooldown is not full");
        if (AirDashCooldownTell.Fill(-2f, 30f) < 0.999f || AirDashCooldownTell.Fill(40f, 30f) > 0.001f)
            report.Fail("fill does not clamp to the cooldown");

        Vector3 origin = new Vector3(3f, 1f, 5f);
        var buffer = new Vector3[AirDashCooldownTell.MaxPoints];
        if (!AirDashCooldownTell.Arc(origin, AirDashCooldownTell.VisibleFill(0f), buffer, out int openCount))
            report.Fail("a fresh cooldown did not place an arc");
        else if (!RingFits(report, origin, buffer, openCount, closed: false))
            report.Fail("the opening arc left the pawn");
        if (!AirDashCooldownTell.Arc(origin, AirDashCooldownTell.VisibleFill(0.5f), buffer, out int halfCount))
            report.Fail("a half cooldown did not place an arc");
        else if (!RingFits(report, origin, buffer, halfCount, closed: false))
            report.Fail("the half arc left the pawn");
        if (halfCount <= openCount)
            report.Fail("the arc does not grow as the cooldown refills");
        if (!AirDashCooldownTell.Arc(origin, 1f, buffer, out int fullCount))
            report.Fail("a ready dash did not close the ring");
        else if (!RingFits(report, origin, buffer, fullCount, closed: true))
            report.Fail("the closed ring left the pawn");
        if (AirDashCooldownTell.Arc(origin, 0f, buffer, out _))
            report.Fail("a zero arc still drew");
        if (AirDashCooldownTell.Arc(origin, 1f, null, out _))
            report.Fail("a missing buffer still drew");

        bool seen = false;
        float age = -1f;
        AirDashCooldownTell.Note(ref seen, ref age, true, 0f);
        if (AirDashCooldownTell.ShowRing(true, 0f, age) || AirDashCooldownTell.ShowFlash(true, age) || age >= 0f)
            report.Fail("a ready pawn winked at spawn");

        seen = false;
        age = 0f;
        AirDashCooldownTell.Note(ref seen, ref age, true, 0f);
        if (AirDashCooldownTell.ShowFlash(true, age) || age >= 0f)
            report.Fail("a stored zero age winked without a cooldown");

        float rem = 30f;
        int ringFrames = 0;
        int guard = 0;
        seen = false;
        age = -1f;
        while (rem > AirDashCooldownTell.CoolingEpsilon && guard < 4000)
        {
            AirDashCooldownTell.Note(ref seen, ref age, true, rem);
            if (!AirDashCooldownTell.ShowRing(true, rem, age))
                report.Fail("ring dropped during cooldown");
            if (AirDashCooldownTell.ShowFlash(true, age))
                report.Fail("ready flash started during cooldown");
            float expect = 1f - (rem / 30f);
            if (Mathf.Abs(AirDashCooldownTell.Fill(rem, 30f) - expect) > 0.0001f)
                report.Fail("fill drifted off the cooldown");
            ringFrames++;
            rem -= Dt;
            if (rem < 0f) rem = 0f;
            if (++guard >= 4000)
                break;
        }

        report.RingFrames = ringFrames;
        if (ringFrames < 1780 || ringFrames > 1820)
            report.Fail("refill arc was not up for the 30s cooldown");
        if (!seen || age >= 0f)
            report.Fail("cooldown did not stay latched until it finished");

        AirDashCooldownTell.Note(ref seen, ref age, true, 0f);
        if (!AirDashCooldownTell.ShowFlash(true, age) || AirDashCooldownTell.ShowRing(true, 0f, age))
            report.Fail("the ready edge did not wink");

        float shown = 0f;
        guard = 0;
        while (AirDashCooldownTell.ShowFlash(true, age))
        {
            shown += Dt;
            AirDashCooldownTell.StepFlash(ref seen, ref age, Dt, true);
            if (++guard > 60)
            {
                report.Fail("ready flash did not stop");
                break;
            }
        }

        report.ShownSeconds = shown;
        if (shown < 0.20f || shown > 0.30f)
            report.Fail("stepped ready flash is outside 0.20-0.30s");
        if (Mathf.Abs(shown - AirDashCooldownTell.ReadyFlashSeconds) > Dt + 0.001f)
            report.Fail("stepped ready flash does not match the window");
        if (AirDashCooldownTell.ShowFlash(true, age) || age >= 0f || seen)
            report.Fail("ready flash stayed armed after it stopped");
        if (AirDashCooldownTell.StepFlash(ref seen, ref age, Dt, true))
            report.Fail("a finished ready flash restarted itself");

        for (int i = 0; i < 90; i++)
        {
            AirDashCooldownTell.Note(ref seen, ref age, true, 0f);
            if (AirDashCooldownTell.ShowRing(true, 0f, age) || AirDashCooldownTell.ShowFlash(true, age))
                report.Fail("ready wink replayed while the dash stayed ready");
            AirDashCooldownTell.StepFlash(ref seen, ref age, Dt, true);
        }

        bool againSeen = true;
        float againAge = 0.05f;
        AirDashCooldownTell.Note(ref againSeen, ref againAge, true, 30f);
        if (AirDashCooldownTell.ShowFlash(true, againAge) || !AirDashCooldownTell.ShowRing(true, 30f, againAge))
            report.Fail("a new dash kept the ready wink");

        report.OpponentFrames = CountQuiet(false, true, 1, SoloGrappleGate.OpponentPawnName, report, "opponent");
        report.CouchFrames = CountQuiet(true, false, 0, SoloGrappleGate.SoloPawnName, report, "couch");
        if (AirDashCooldownTell.ForPawn(false, false, 1, "Player_P1")
            || AirDashCooldownTell.ForPawn(true, false, 1, "Player_P1")
            || AirDashCooldownTell.ForPawn(false, false, 0, SoloGrappleGate.OpponentPawnName))
            report.Fail("dash cooldown leaked off the solo pawn");
        if (!AirDashCooldownTell.ForPawn(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn dash cooldown is off");
        if (report.OpponentFrames != 0)
            report.Fail("the opponent drew a dash cooldown");
        if (report.CouchFrames != 0)
            report.Fail("a couch pawn drew a dash cooldown");

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

        string tellSrc = ReadRepo("Assets/Scripts/Art/AirDashCooldownTell.cs");
        string burstSrc = ReadRepo("Assets/Scripts/Art/AirDashTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string lungeSrc = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        if (tellSrc == null || burstSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || lungeSrc == null)
            report.Fail("dash cooldown sources missing");
        else
            CheckWiring(report, tellSrc, burstSrc, locoSrc, motorSrc, cfgSrc, lungeSrc);

        if (VerbPoseClips.SlideBody != "SlideBody" || VerbPoseClips.PunchStrike != "PunchStrike" || VerbPoseClips.TagCatch != "TagCatch")
            report.Fail("verb clip names drifted");
        if (ReadRepo("Assets/Scripts/Art/OpponentChaseTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/WallClingTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandFlash.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleLatchTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/AirDashTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/ItMarker.cs") == null)
            report.Fail("an existing tell was removed");

        if (!SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn gate is off");
        if (SoloGrappleGate.EnableFor(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(true, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("opponent or couch gained the solo gate");

        if (Mathf.Abs(report.JumpSpeed - cfg.jumpSpeed) > 0.001f || report.SlideBoost != cfg.slideBoost)
            report.Fail("the tell rewrote jumpSpeed or slideBoost");

        return report;
    }

    static bool RingFits(AirDashCooldownTellReport report, Vector3 origin, Vector3[] buffer, int count, bool closed)
    {
        if (count < 2) return false;
        float minGap = float.MaxValue;
        float maxGap = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector3 d = buffer[i] - origin;
            if (Mathf.Abs(d.y - AirDashCooldownTell.RingHeight) > 0.001f)
                return false;
            float horiz = Mathf.Sqrt(d.x * d.x + d.z * d.z);
            if (Mathf.Abs(horiz - AirDashCooldownTell.RingRadius) > 0.02f)
                return false;
            if (i > 0)
            {
                Vector3 step = buffer[i] - buffer[i - 1];
                float gap = step.magnitude;
                if (gap < minGap) minGap = gap;
                if (gap > maxGap) maxGap = gap;
            }
        }

        Vector3 ends = buffer[count - 1] - buffer[0];
        float endGap = ends.magnitude;
        if (closed)
            return endGap < 0.001f;
        if (endGap < 0.05f || endGap > AirDashCooldownTell.RingRadius * 2.05f)
            return false;
        if (maxGap > AirDashCooldownTell.RingRadius)
            report.Fail("cooldown arc jumped across the pawn");
        return minGap > 0.0001f;
    }

    static int CountQuiet(bool couch, bool ai, int index, string pawnName, AirDashCooldownTellReport report, string label)
    {
        bool solo = AirDashCooldownTell.ForPawn(couch, ai, index, pawnName);
        if (solo)
            report.Fail(label + " was treated as the solo pawn");
        bool seen = false;
        float age = -1f;
        int shown = 0;
        float rem = 30f;
        for (int i = 0; i < 40; i++)
        {
            AirDashCooldownTell.Note(ref seen, ref age, solo, rem);
            if (AirDashCooldownTell.ShowRing(solo, rem, age) || AirDashCooldownTell.ShowFlash(solo, age))
                shown++;
            if (AirDashCooldownTell.StepFlash(ref seen, ref age, Dt, solo))
                report.Fail(label + " stepped into a ready flash");
            if (age >= 0f || seen)
                report.Fail(label + " armed a dash cooldown");
            rem -= 1f;
            if (rem < 0f) rem = 0f;
        }

        AirDashCooldownTell.Note(ref seen, ref age, solo, 0f);
        if (AirDashCooldownTell.ShowFlash(solo, age) || AirDashCooldownTell.ShowRing(solo, 0f, age))
            shown++;
        return shown;
    }

    static void CheckLocks(AirDashCooldownTellReport report, MovementConfig cfg, PunchTagTuning punch)
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
        AirDashCooldownTellReport report,
        string tellSrc, string burstSrc, string locoSrc, string motorSrc, string cfgSrc, string lungeSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera") || tellSrc.Contains("InputAction")
            || tellSrc.Contains("airDashDuration") || tellSrc.Contains("airDashSpeed") || tellSrc.Contains("airDashCooldown"))
            report.Fail("cooldown constants write feel, input, or the camera");

        if (!Has(burstSrc, "RibbonTime = 0.42f", "FlashMix = 0.42f", "FlashSeconds = 0.24f", "WingOffset = 0.55f"))
            report.Fail("air dash burst tell drifted");
        if (!Has(cfgSrc, "jumpSpeed = 24.7f", "slideBoost = 0f", "clingReleaseGrace = 0.08f",
            "coyoteTime = 0.10f", "jumpBuffer = 0.16f", "enableJet = false",
            "airDashDuration = 0.10f", "airDashSpeed = 15f", "airDashCooldown = 30f",
            "taggerLungeSpeed = 16f", "taggerLungeDuration = 0.20f", "taggerLungeCooldown = 1.0f"))
            report.Fail("movement defaults drifted");
        if (!lungeSrc.Contains("LeadSeconds = 0.45f"))
            report.Fail("LungeTell lead is not 0.45s");

        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("AirDashCooldownTell") || motorSrc.Contains("FixedUpdate"))
            report.Fail("cooldown tell leaked into the motor");
        if (!motorSrc.Contains("_airDashT = cfg.airDashDuration")
            || !motorSrc.Contains("_airDashCd = Mathf.Max(0.01f, cfg.airDashCooldown)")
            || !motorSrc.Contains("cfg.airDashSpeed"))
            report.Fail("air dash duration, speed, or cooldown was disconnected");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickAirDashCooldown");
        string solo = MethodBody(locoSrc, "bool DashCooldownSolo");
        string place = MethodBody(locoSrc, "void PlaceDashCooldown");
        string ensure = MethodBody(locoSrc, "void EnsureDashCooldown");
        string arc = MethodBody(locoSrc, "LineRenderer MakeDashCooldownArc");
        string mat = MethodBody(locoSrc, "static Material MakeDashCooldownMat");
        string hide = MethodBody(locoSrc, "void HideDashCooldown");
        string paint = MethodBody(locoSrc, "void PaintDashCooldown");
        string burst = MethodBody(locoSrc, "void TickAirDashTell");
        string begin = MethodBody(locoSrc, "void BeginAirDashTell");
        if (late == null || tick == null || solo == null || place == null || ensure == null || arc == null
            || mat == null || hide == null || paint == null || burst == null || begin == null)
        {
            report.Fail("dash cooldown is not on the solo visual path");
            return;
        }

        int call = late.IndexOf("TickAirDashCooldown(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("dash cooldown does not run with the other presentation tells");
        if (!burst.Contains("BeginAirDashTell") || !begin.Contains("AirDashTell.RibbonTime"))
            report.Fail("the dash burst tell was disconnected");

        if (!solo.Contains("AirDashCooldownTell.ForPawn") || !solo.Contains("LocalPlayerRoster.IsCouch")
            || !solo.Contains("SoloGrappleGate.OpponentPawnName"))
            report.Fail("dash cooldown is not gated to the solo pawn");
        if (!tick.Contains("AirDashCooldownTell.Note") || !tick.Contains("AirDashCooldownTell.ShowRing")
            || !tick.Contains("AirDashCooldownTell.ShowFlash") || !tick.Contains("AirDashCooldownTell.StepFlash")
            || !tick.Contains("AirDashCooldownRemaining"))
            report.Fail("dash cooldown does not follow the motor recharge");
        int gate = tick.IndexOf("if (!ring && !flash)", StringComparison.Ordinal);
        int built = tick.IndexOf("EnsureDashCooldown(", StringComparison.Ordinal);
        if (gate < 0 || built < 0 || gate > built)
            report.Fail("opponent and couch still build the cooldown arc");
        if (!place.Contains("AirDashCooldownTell.Arc") || !place.Contains("AirDashCooldownTell.VisibleFill")
            || !place.Contains("AirDashCooldownTell.RingWidth"))
            report.Fail("the arc is not placed by the cooldown builder");
        if (!arc.Contains("AirDashCooldownTell.RingWidth") || !arc.Contains("AirDashCooldownTell.MaxAlpha"))
            report.Fail("the arc does not use the tell width and alpha");
        if (!mat.Contains("AirDashCooldownTell.MaxAlpha"))
            report.Fail("the arc material does not use the tell alpha");

        string bodies = tick + solo + place + ensure + arc + mat + hide + paint;
        if (bodies.Contains("CreatePrimitive") || bodies.Contains("Collider") || bodies.Contains("Rigidbody")
            || bodies.Contains("AddComponent<Light>") || bodies.Contains("Light")
            || bodies.Contains("_EmissionColor") || bodies.Contains("_EMISSION")
            || bodies.Contains("FovPop") || bodies.Contains("fieldOfView") || bodies.Contains("InputAction")
            || bodies.Contains("AddForce") || bodies.Contains("jumpSpeed") || bodies.Contains("slideBoost")
            || bodies.Contains("airDashDuration") || bodies.Contains("airDashSpeed") || bodies.Contains("airDashCooldown ="))
            report.Fail("dash cooldown adds a glow, a volume, or a feel write");
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

public sealed class AirDashCooldownTellReport
{
    public float RingRadius;
    public float RingHeight;
    public float RingWidth;
    public float MinArc;
    public float MaxAlpha;
    public float FlashSeconds;
    public float ShownSeconds;
    public int RingFrames;
    public int OpponentFrames;
    public int CouchFrames;
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
        text.Append(" dash cd ring_r=").Append(RingRadius.ToString("0.00", c));
        text.Append(" ring_h=").Append(RingHeight.ToString("0.00", c));
        text.Append(" ring_w=").Append(RingWidth.ToString("0.00", c));
        text.Append(" min_arc=").Append(MinArc.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" flash_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" shown_s=").Append(ShownSeconds.ToString("0.00", c));
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
