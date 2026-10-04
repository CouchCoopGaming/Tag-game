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
/// Headless check: a fired planar miss flicks a short cool stub, and a latch stays
/// on the latch flash. A hold with no new fire, and an unavailable grapple, stay quiet.
/// Jump, dash, cling, and punch stay put.
/// </summary>
public static class GrappleMissTellProof
{
    const float Dt = 1f / 60f;

    public static GrappleMissTellReport Run()
    {
        var report = new GrappleMissTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.FlashSeconds = GrappleMissTell.FlashSeconds;
        report.MaxAlpha = GrappleMissTell.MaxAlpha;
        report.StubLength = GrappleMissTell.StubLength;
        report.KnotSize = GrappleMissTell.KnotSize;
        report.JumpSpeed = cfg.jumpSpeed;

        if (GrappleMissTell.FlashSeconds < 0.20f || GrappleMissTell.FlashSeconds > 0.28f)
            report.Fail("miss cue is outside 0.20-0.28s");
        if (GrappleMissTell.MaxAlpha > 0.40f)
            report.Fail("miss alpha hides the body");
        if (GrappleMissTell.MaxAlpha < 0.28f)
            report.Fail("miss alpha is too faint to read");
        if (GrappleMissTell.MaxAlpha >= GrappleLatchTell.MaxAlpha)
            report.Fail("miss cue is as loud as the latch flash");
        if (Mathf.Abs(GrappleMissTell.Alpha(0f) - GrappleMissTell.MaxAlpha) > 0.001f)
            report.Fail("miss cue does not open at peak alpha");
        if (GrappleMissTell.Alpha(GrappleMissTell.FlashSeconds) != 0f || GrappleMissTell.Alpha(-1f) != 0f)
            report.Fail("miss alpha stays up after the window");
        if (GrappleMissTell.Alpha(0.28f) != 0f)
            report.Fail("miss alpha is still up at 0.28s");
        if (GrappleMissTell.Alpha(0.12f) >= GrappleMissTell.MaxAlpha - 0.01f)
            report.Fail("miss cue does not fade");
        if (GrappleMissTell.StubWidth < 0.03f || GrappleMissTell.StubWidth >= GrappleRopeTell.AimWidth
            || GrappleMissTell.StubWidth >= GrappleRopeTell.RopeStartWidth)
            report.Fail("miss stub is a hairline or as thick as the aim or the rope");
        if (GrappleMissTell.StubLength < 0.40f || GrappleMissTell.StubLength >= GrappleRopeTell.AimLength * 0.6f)
            report.Fail("miss stub is a speck or long enough to cover the aim");
        if (GrappleMissTell.KnotSize < 0.12f || GrappleMissTell.KnotSize >= GrappleRopeTell.HookMarkerSize
            || GrappleMissTell.KnotSize >= GrappleLatchTell.KnotSize)
            report.Fail("miss knot hides the hand or matches the latch knot");
        if (Mathf.Abs(GrappleMissTell.KnotScale(1f) - GrappleMissTell.KnotSize) > 0.001f)
            report.Fail("miss knot does not open at full size");
        if (GrappleMissTell.KnotScale(0f) >= GrappleMissTell.KnotScale(1f))
            report.Fail("miss knot does not collapse");
        if (GrappleMissTell.MarkR > 0.97f && GrappleMissTell.MarkG > 0.97f && GrappleMissTell.MarkB > 0.97f)
            report.Fail("miss cue is white enough to wash the body");
        if (GrappleMissTell.MarkB <= GrappleMissTell.MarkR)
            report.Fail("miss cue is not cooler than the gold rope");
        if (GrappleMissTell.MarkR >= GrappleLatchTell.MarkR || GrappleMissTell.MarkG >= GrappleLatchTell.MarkG)
            report.Fail("miss cue is as warm as the latch flash");
        float missPeak = Mathf.Max(GrappleMissTell.MarkR, Mathf.Max(GrappleMissTell.MarkG, GrappleMissTell.MarkB));
        float goldPeak = Mathf.Max(GrappleLatchTell.MarkR, Mathf.Max(GrappleLatchTell.MarkG, GrappleLatchTell.MarkB));
        if (missPeak >= goldPeak)
            report.Fail("miss cue is not muted against the gold rope");
        if (GrappleMissTell.Glow != 0f)
            report.Fail("miss cue glows");
        if (GrappleMissTell.VerticalImpulse != 0f)
            report.Fail("miss cue adds a vertical impulse");
        if (Mathf.Abs(GrappleRopeTell.AimLength - 2.4f) > 0.001f)
            report.Fail("aim line length changed");
        if (GrappleRopeTell.VerticalImpulse != 0f)
            report.Fail("rope tell adds a vertical impulse");
        if (Mathf.Abs(GrappleLatchTell.FlashSeconds - 0.25f) > 0.001f || Mathf.Abs(GrappleLatchTell.MaxAlpha - 0.44f) > 0.001f)
            report.Fail("latch flash timing drifted");

        if (GrappleMissTell.Show(true, false, GrappleMissTell.FlashSeconds))
            report.Fail("the cue stays up at the end of the window");
        if (!GrappleMissTell.Show(true, false, GrappleMissTell.FlashSeconds - 0.001f))
            report.Fail("the cue ends before the window");
        if (GrappleMissTell.Show(true, false, 0.28f) || GrappleMissTell.Show(true, true, 0f) || GrappleMissTell.Show(false, false, 0f))
            report.Fail("the cue shows on a latch, an unavailable grapple, or past the window");

        Vector3 origin = new Vector3(2f, 1f, 4f);
        Vector3 face = Vector3.forward;
        Vector3 aimDir = new Vector3(0f, 1f, 3f);
        Vector3 hand = GrappleRopeTell.Hand(origin, face);
        if (!GrappleRopeTell.AimSpan(origin, face, aimDir, out Vector3 aimHand, out Vector3 tip))
            report.Fail("aim preview dropped");
        if ((aimHand - hand).sqrMagnitude > 0.0001f)
            report.Fail("miss cue moved the aim hand");
        float aimLen = (tip - aimHand).magnitude;
        if (Mathf.Abs(aimLen - GrappleRopeTell.AimLength) > 0.001f)
            report.Fail("aim length changed");

        float armed = -1f;
        GrappleMissTell.Note(ref armed, true, true, true, false);
        if (!GrappleMissTell.Stub(true, false, hand, aimDir, armed, out Vector3 from, out Vector3 to))
            report.Fail("a miss did not show the stub");
        if ((from - hand).sqrMagnitude > 0.0001f)
            report.Fail("stub does not start at the hand");
        float len0 = (to - from).magnitude;
        if (Mathf.Abs(len0 - GrappleMissTell.StubLength) > 0.001f)
            report.Fail("stub does not open at full length");
        if (len0 >= aimLen - 0.05f)
            report.Fail("stub covers the aim line");
        if (!SameDir(aimDir, to - from))
            report.Fail("stub left the aim");
        if (!GrappleMissTell.Knot(true, false, hand, armed, out Vector3 knotAt) || (knotAt - hand).sqrMagnitude > 0.0001f)
            report.Fail("miss knot is not on the hand");

        float mid = GrappleMissTell.FlashSeconds * 0.5f;
        if (!GrappleMissTell.Stub(true, false, hand, aimDir, mid, out Vector3 midFrom, out Vector3 midTo))
            report.Fail("stub vanished at mid window");
        float lenMid = (midTo - midFrom).magnitude;
        if (lenMid >= len0 - 0.05f)
            report.Fail("stub does not collapse");
        if (Mathf.Abs(lenMid - GrappleMissTell.StubLength * GrappleMissTell.Fade(mid)) > 0.02f)
            report.Fail("stub collapse does not follow the fade");
        if ((midFrom - hand).sqrMagnitude > 0.0001f || !SameDir(aimDir, midTo - midFrom))
            report.Fail("collapsing stub left the hand or the aim");

        float late = GrappleMissTell.FlashSeconds - 0.004f;
        if (!GrappleMissTell.Show(true, false, late))
            report.Fail("cue ended early");
        if (GrappleMissTell.Stub(true, false, hand, aimDir, late, out _, out _))
            report.Fail("stub did not collapse away");
        if (!GrappleMissTell.Knot(true, false, hand, late, out _))
            report.Fail("knot dropped before the window ended");
        if (GrappleMissTell.Stub(true, false, hand, Vector3.zero, 0f, out _, out _))
            report.Fail("a zero aim drew a stub");
        if (GrappleMissTell.Stub(true, true, hand, aimDir, 0f, out _, out _) || GrappleMissTell.Knot(true, true, hand, 0f, out _))
            report.Fail("a latch drew the miss cue");

        float shown = PlayMiss(report, hand, aimDir);
        report.ShownSeconds = shown;
        if (shown < 0.20f || shown > 0.28f)
            report.Fail("stepped miss cue is outside 0.20-0.28s");
        if (Mathf.Abs(shown - GrappleMissTell.FlashSeconds) > Dt + 0.001f)
            report.Fail("stepped miss cue does not match the window");

        float live = -1f;
        GrappleMissTell.Note(ref live, true, true, true, false);
        GrappleMissTell.Step(ref live, Dt, true, false);
        float marked = live;
        GrappleMissTell.Note(ref live, true, false, true, false);
        if (Mathf.Abs(live - marked) > 0.0001f)
            report.Fail("hold-without-fire restarted the miss cue");

        float quietShot = -1f;
        GrappleMissTell.Note(ref quietShot, true, true, false, false);
        if (GrappleMissTell.Show(true, false, quietShot) || quietShot >= 0f)
            report.Fail("a shot that did not miss armed the cue");

        report.LatchFrames = CountLatch(report, hand, aimDir);
        report.HoldFrames = CountHold(report);
        report.OffFrames = CountOff(report);
        if (report.MissFrames <= 0)
            report.Fail("a miss did not show the cue");
        if (report.LatchFrames != 0)
            report.Fail("a latch drew the miss cue");
        if (report.HoldFrames != 0)
            report.Fail("hold-without-fire drew the miss cue");
        if (report.OffFrames != 0)
            report.Fail("an unavailable grapple drew the miss cue");

        float latchAge = -1f;
        GrappleLatchTell.Arm(ref latchAge);
        if (!GrappleLatchTell.Show(true, latchAge))
            report.Fail("latch success left the latch flash");
        if (GrappleMissTell.Show(true, true, 0f))
            report.Fail("latch success drew the miss cue");
        if (GrappleLatchTell.Show(false, 0f))
            report.Fail("a miss drew the latch flash");

        if (!GrappleMissTell.ForPawn(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn miss cue is off");
        if (GrappleMissTell.ForPawn(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || GrappleMissTell.ForPawn(false, false, 0, SoloGrappleGate.OpponentPawnName)
            || GrappleMissTell.ForPawn(true, false, 0, SoloGrappleGate.SoloPawnName)
            || GrappleMissTell.ForPawn(true, false, 1, "Player_P1")
            || GrappleMissTell.ForPawn(false, false, 2, SoloGrappleGate.SoloPawnName))
            report.Fail("miss cue leaked off the solo pawn");

        float opponent = -1f;
        bool opponentSolo = GrappleMissTell.ForPawn(false, true, 1, SoloGrappleGate.OpponentPawnName);
        GrappleMissTell.Note(ref opponent, opponentSolo, true, true, false);
        if (opponentSolo || GrappleMissTell.Show(opponentSolo, false, opponent) || opponent >= 0f)
            report.Fail("opponent drew the miss cue");
        float couch = -1f;
        bool couchSolo = GrappleMissTell.ForPawn(true, false, 0, SoloGrappleGate.SoloPawnName);
        GrappleMissTell.Note(ref couch, couchSolo, true, true, false);
        if (couchSolo || GrappleMissTell.Show(couchSolo, false, couch) || couch >= 0f)
            report.Fail("couch pawn drew the miss cue");

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

        string tellSrc = ReadRepo("Assets/Scripts/Art/GrappleMissTell.cs");
        string latchSrc = ReadRepo("Assets/Scripts/Art/GrappleLatchTell.cs");
        string ropeSrc = ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs");
        string grappleSrc = ReadRepo("Assets/Scripts/Experimental/ExperimentalGrapple.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string lungeSrc = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        if (tellSrc == null || latchSrc == null || ropeSrc == null || grappleSrc == null || motorSrc == null
            || cfgSrc == null || spawnSrc == null || lungeSrc == null)
            report.Fail("miss sources missing");
        else
            CheckWiring(report, tellSrc, latchSrc, ropeSrc, grappleSrc, motorSrc, cfgSrc, spawnSrc, lungeSrc);

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
            || ReadRepo("Assets/Scripts/Art/AirDashCooldownTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/ItMarker.cs") == null
            || ReadRepo("Assets/Scripts/Art/HitConfirmTell.cs") == null)
            report.Fail("an existing tell was removed");

        if (!SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn grapple is off");
        if (SoloGrappleGate.EnableFor(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(true, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("opponent or couch gained the solo grapple");

        return report;
    }

    static float PlayMiss(GrappleMissTellReport report, Vector3 hand, Vector3 aimDir)
    {
        float age = -1f;
        GrappleMissTell.Note(ref age, true, true, true, false);
        if (!GrappleMissTell.Show(true, false, age))
        {
            report.Fail("a miss did not show the cue");
            return 0f;
        }

        if (Mathf.Abs(GrappleMissTell.Alpha(age) - GrappleMissTell.MaxAlpha) > 0.001f)
            report.Fail("a miss did not open at peak alpha");

        float shown = 0f;
        int frames = 0;
        int guard = 0;
        while (GrappleMissTell.Show(true, false, age))
        {
            if (!GrappleMissTell.Knot(true, false, hand, age, out Vector3 knot) || (knot - hand).sqrMagnitude > 0.0001f)
                report.Fail("miss knot left the hand");
            shown += Dt;
            frames++;
            bool still = GrappleMissTell.Step(ref age, Dt, true, false);
            if (!still && GrappleMissTell.Show(true, false, age))
                report.Fail("step ended while the cue was still up");
            if (++guard > 40)
            {
                report.Fail("miss cue did not stop");
                break;
            }
        }

        report.MissFrames = frames;
        if (GrappleMissTell.Show(true, false, age) || age >= 0f)
            report.Fail("miss cue stayed armed after it stopped");
        if (GrappleMissTell.Stub(true, false, hand, aimDir, age, out _, out _) || GrappleMissTell.Knot(true, false, hand, age, out _))
            report.Fail("miss cue kept drawing after the window");
        if (GrappleMissTell.Step(ref age, Dt, true, false))
            report.Fail("a finished miss restarted itself");

        float marked = age;
        for (int i = 0; i < 20; i++)
        {
            GrappleMissTell.Note(ref age, true, false, true, false);
            if (age != marked || GrappleMissTell.Show(true, false, age))
                report.Fail("hold-without-fire replayed a finished miss");
        }

        GrappleMissTell.Note(ref age, true, true, true, false);
        if (!GrappleMissTell.Show(true, false, age) || age != 0f)
            report.Fail("a new miss did not arm");
        return shown;
    }

    static int CountLatch(GrappleMissTellReport report, Vector3 hand, Vector3 aimDir)
    {
        float age = -1f;
        int shown = 0;
        GrappleMissTell.Note(ref age, true, true, true, false);
        if (!GrappleMissTell.Show(true, false, age))
            report.Fail("a miss did not arm before the latch");
        GrappleMissTell.Note(ref age, true, true, false, true);
        for (int i = 0; i < 40; i++)
        {
            GrappleMissTell.Note(ref age, true, false, false, true);
            if (GrappleMissTell.Show(true, true, age))
                shown++;
            if (GrappleMissTell.Stub(true, true, hand, aimDir, age, out _, out _))
                report.Fail("a latch drew the miss stub");
            if (GrappleMissTell.Knot(true, true, hand, age, out _))
                report.Fail("a latch placed the miss knot");
            if (GrappleMissTell.Step(ref age, Dt, true, true))
                report.Fail("a latch stepped the miss cue");
            if (age >= 0f)
                report.Fail("a latch left the miss cue armed");
        }

        float fresh = -1f;
        GrappleMissTell.Note(ref fresh, true, true, false, true);
        if (GrappleMissTell.Show(true, true, fresh) || fresh >= 0f)
            report.Fail("latch success drew the miss cue");
        return shown;
    }

    static int CountHold(GrappleMissTellReport report)
    {
        float age = -1f;
        int shown = 0;
        for (int i = 0; i < 40; i++)
        {
            GrappleMissTell.Note(ref age, true, false, true, false);
            if (GrappleMissTell.Show(true, false, age))
                shown++;
            if (GrappleMissTell.Stub(true, false, Vector3.zero, Vector3.forward, age, out _, out _))
                report.Fail("hold-without-fire drew a stub");
            if (GrappleMissTell.Knot(true, false, Vector3.zero, age, out _))
                report.Fail("hold-without-fire placed a knot");
            if (GrappleMissTell.Step(ref age, Dt, true, false))
                report.Fail("hold-without-fire stepped into the cue");
            if (age >= 0f)
                report.Fail("hold-without-fire armed the cue");
        }

        return shown;
    }

    static int CountOff(GrappleMissTellReport report)
    {
        float age = -1f;
        GrappleMissTell.Note(ref age, true, true, true, false);
        GrappleMissTell.Note(ref age, false, true, true, false);
        if (age >= 0f)
            report.Fail("an unavailable grapple left the miss cue armed");

        int shown = 0;
        for (int i = 0; i < 40; i++)
        {
            GrappleMissTell.Note(ref age, false, true, true, false);
            if (GrappleMissTell.Show(false, false, age))
                shown++;
            if (GrappleMissTell.Stub(false, false, Vector3.zero, Vector3.forward, age, out _, out _))
                report.Fail("an unavailable grapple drew a stub");
            if (GrappleMissTell.Knot(false, false, Vector3.zero, age, out _))
                report.Fail("an unavailable grapple placed a knot");
            if (GrappleMissTell.Step(ref age, Dt, false, false))
                report.Fail("an unavailable grapple stepped the cue");
            if (age >= 0f)
                report.Fail("an unavailable grapple armed the cue");
        }

        return shown;
    }

    static bool SameDir(Vector3 aimDir, Vector3 along)
    {
        if (aimDir.sqrMagnitude < 1e-6f || along.sqrMagnitude < 1e-6f) return false;
        Vector3 delta = along.normalized - aimDir.normalized;
        return delta.sqrMagnitude < 0.0001f;
    }

    static void CheckLocks(GrappleMissTellReport report, MovementConfig cfg, PunchTagTuning punch)
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
        GrappleMissTellReport report,
        string tellSrc, string latchSrc, string ropeSrc, string grappleSrc, string motorSrc, string cfgSrc, string spawnSrc, string lungeSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera") || tellSrc.Contains("InputAction")
            || tellSrc.Contains("maxRange") || tellSrc.Contains("Shake") || tellSrc.Contains("fieldOfView"))
            report.Fail("miss constants write feel, input, or the camera");
        if (latchSrc.Contains("GrappleMissTell"))
            report.Fail("the miss cue was baked into the latch flash");
        if (!ropeSrc.Contains("AimLength = 2.4f"))
            report.Fail("aim line is no longer 2.4 m");

        if (!Has(cfgSrc, "jumpSpeed = 24.7f", "slideBoost = 0f", "clingReleaseGrace = 0.08f",
            "coyoteTime = 0.10f", "jumpBuffer = 0.16f", "enableJet = false",
            "airDashDuration = 0.10f", "airDashSpeed = 15f", "airDashCooldown = 30f",
            "taggerLungeSpeed = 16f", "taggerLungeDuration = 0.20f", "taggerLungeCooldown = 1.0f"))
            report.Fail("movement defaults drifted");
        if (!lungeSrc.Contains("LeadSeconds = 0.45f"))
            report.Fail("LungeTell lead is not 0.45s");

        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("GrappleMissTell") || motorSrc.Contains("GrappleLatch") || motorSrc.Contains("FixedUpdate"))
            report.Fail("miss cue leaked into the motor");

        if (!grappleSrc.Contains("maxRange = 28f") || !grappleSrc.Contains("attachSlack = 0.35f")
            || !grappleSrc.Contains("FireButton = \"RMB\"") || !grappleSrc.Contains("enableGrapple = false"))
            report.Fail("grapple range, slack, or button changed");
        if (Count(grappleSrc, "RaycastNonAlloc(") != 1)
            report.Fail("miss cue added a second ray");
        if (Count(grappleSrc, "GrappleLatchTell.Arm") != 1)
            report.Fail("latch flash arms more than once");
        if (grappleSrc.Contains("InputAction") || grappleSrc.Contains("FovPop") || grappleSrc.Contains("fieldOfView")
            || grappleSrc.Contains("AddForce") || grappleSrc.Contains("jumpSpeed") || grappleSrc.Contains("Shake"))
            report.Fail("miss cue wrote input, camera, or motor code");

        string resolve = MethodBody(grappleSrc, "void ResolveAttach");
        string tryAttach = MethodBody(grappleSrc, "void TryAttach");
        string late = MethodBody(grappleSrc, "void LateUpdate");
        string release = MethodBody(grappleSrc, "void Release");
        string disable = MethodBody(grappleSrc, "void OnDisable");
        string tickLatch = MethodBody(grappleSrc, "void TickLatchFlash");
        string tick = MethodBody(grappleSrc, "void TickMissTell");
        string solo = MethodBody(grappleSrc, "bool MissAvailable");
        string placeKnot = MethodBody(grappleSrc, "void PlaceMissKnot");
        string placeStub = MethodBody(grappleSrc, "void PlaceMissStub");
        string hide = MethodBody(grappleSrc, "void HideMiss");
        string ensure = MethodBody(grappleSrc, "void EnsureMissTell");
        string knot = MethodBody(grappleSrc, "Transform MakeMissKnot");
        string stub = MethodBody(grappleSrc, "LineRenderer MakeMissStub");
        string mat = MethodBody(grappleSrc, "static Material MakeMissMat");
        string paint = MethodBody(grappleSrc, "void PaintMiss");
        string diamond = MethodBody(grappleSrc, "static Mesh MissDiamond");
        if (resolve == null || tryAttach == null || late == null || release == null || disable == null
            || tickLatch == null || tick == null || solo == null || placeKnot == null || placeStub == null
            || hide == null || ensure == null || knot == null || stub == null || mat == null || paint == null
            || diamond == null)
        {
            report.Fail("miss cue is not on the grapple visual path");
            return;
        }

        int heldAt = resolve.IndexOf("bool held = gateOpen && ReadFire()", StringComparison.Ordinal);
        int firedAt = resolve.IndexOf("bool fired = held && !_fireWas", StringComparison.Ordinal);
        int wasAt = resolve.IndexOf("_fireWas = held", StringComparison.Ordinal);
        if (heldAt < 0 || firedAt < 0 || wasAt < 0 || !(heldAt < firedAt && firedAt < wasAt))
            report.Fail("the miss cue treats a hold as a new fire");
        if (Count(resolve, "GrappleMissTell.Clear(ref _missAge)") != 2)
            report.Fail("an unavailable grapple can leave the miss cue up");
        if (Count(resolve, "GrappleMissTell.Note") != 2)
            report.Fail("miss cue notes from the wrong place");
        if (!resolve.Contains("GrappleMissTell.Note(ref _missAge, MissAvailable(), false, false, false)"))
            report.Fail("hold-without-fire can arm the miss cue");
        int tryAt = resolve.IndexOf("TryAttach(", StringComparison.Ordinal);
        int noteAt = resolve.IndexOf("GrappleMissTell.Note(ref _missAge, MissAvailable(), fired, _rayMiss && !_attached, _attached)", StringComparison.Ordinal);
        if (tryAt < 0 || noteAt < 0 || tryAt > noteAt)
            report.Fail("a fired miss does not arm the cue");

        int flag = tryAttach.IndexOf("_rayMiss = best < 0", StringComparison.Ordinal);
        int miss = tryAttach.IndexOf("if (best < 0) return", StringComparison.Ordinal);
        int arm = tryAttach.IndexOf("GrappleLatchTell.Arm", StringComparison.Ordinal);
        int clear = tryAttach.IndexOf("_rayMiss = false", StringComparison.Ordinal);
        int ray = tryAttach.IndexOf("RaycastNonAlloc(", StringComparison.Ordinal);
        if (flag < 0 || miss < 0 || arm < 0 || !(flag < miss && miss < arm))
            report.Fail("a miss is not known before the latch flash arms");
        if (clear < 0 || ray < 0 || clear > ray)
            report.Fail("a missed ray sticks when the shot cannot cast");
        if (tryAttach.Contains("GrappleMissTell"))
            report.Fail("the ray path draws the miss cue");
        if (tryAttach.Contains("GrappleLatchTell.Pulse") || tryAttach.Contains("GrappleLatchTell.Knot"))
            report.Fail("the ray path draws the latch flash");

        int missTick = late.IndexOf("TickMissTell(", StringComparison.Ordinal);
        int latchTick = late.IndexOf("TickLatchFlash(", StringComparison.Ordinal);
        int hideFlash = late.IndexOf("HideLatchFlash(", StringComparison.Ordinal);
        int aimAt = late.IndexOf("GrappleRopeTell.AimSpan", StringComparison.Ordinal);
        if (missTick < 0 || latchTick < 0 || hideFlash < 0 || aimAt < 0
            || !(missTick < latchTick && latchTick < hideFlash && hideFlash < aimAt))
            report.Fail("the miss cue draws on the latch flash");
        if (late.IndexOf("TickLatchFlash(", latchTick + 1, StringComparison.Ordinal) >= 0)
            report.Fail("latch flash ticks off the attached rope");
        if (!late.Contains("GrappleRopeTell.AttachedSpan") || !late.Contains("GrappleRopeTell.AimWidth"))
            report.Fail("aim preview or the attached rope changed");
        if (!release.Contains("GrappleLatchTell.Clear") || release.Contains("GrappleMissTell"))
            report.Fail("releasing the button cuts the miss cue or leaves the latch flash armed");
        if (!disable.Contains("GrappleMissTell.Clear") || !disable.Contains("HideMiss("))
            report.Fail("a disabled pawn leaves the miss cue up");

        if (tick.Contains("GrappleLatchTell") || tickLatch.Contains("GrappleMissTell"))
            report.Fail("the miss cue and the latch flash share a draw");
        if (!tick.Contains("GrappleMissTell.Show") || !tick.Contains("GrappleMissTell.Knot")
            || !tick.Contains("GrappleMissTell.Stub") || !tick.Contains("GrappleMissTell.Step")
            || !tick.Contains("GrappleMissTell.Alpha"))
            report.Fail("miss cue does not use the tell timing");
        int gate = tick.IndexOf("if (!GrappleMissTell.Show", StringComparison.Ordinal);
        int built = tick.IndexOf("EnsureMissTell(", StringComparison.Ordinal);
        if (gate < 0 || built < 0 || gate > built)
            report.Fail("a latch or a hold still builds the miss cue");
        if (!solo.Contains("enableGrapple") || !solo.Contains("GrappleMissTell.ForPawn")
            || !solo.Contains("LocalPlayerRoster.IsCouch"))
            report.Fail("miss cue is not gated to the solo pawn");
        if (!placeStub.Contains("GrappleMissTell.StubWidth") || !stub.Contains("GrappleMissTell.StubWidth")
            || !stub.Contains("GrappleMissTell.MaxAlpha") || !mat.Contains("GrappleMissTell.MaxAlpha"))
            report.Fail("stub does not use the tell width and alpha");
        if (!knot.Contains("GrappleMissTell.KnotSize") || !knot.Contains("MeshFilter") || !knot.Contains("MeshRenderer"))
            report.Fail("miss knot is not a bare mesh");
        if (!mat.Contains("GrappleMissFail"))
            report.Fail("miss cue reuses the latch flash material");

        string bodies = tick + solo + placeKnot + placeStub + hide + ensure + knot + stub + mat + paint + diamond;
        if (bodies.Contains("CreatePrimitive") || bodies.Contains("Collider") || bodies.Contains("Rigidbody")
            || bodies.Contains("AddComponent<Light>") || bodies.Contains("Light")
            || bodies.Contains("_EmissionColor") || bodies.Contains("_EMISSION")
            || bodies.Contains("FovPop") || bodies.Contains("fieldOfView") || bodies.Contains("InputAction")
            || bodies.Contains("AddForce") || bodies.Contains("jumpSpeed") || bodies.Contains("maxRange")
            || bodies.Contains("Shake"))
            report.Fail("miss cue adds a glow, a volume, a shake, or a feel write");

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

public sealed class GrappleMissTellReport
{
    public float FlashSeconds;
    public float ShownSeconds;
    public float MaxAlpha;
    public float StubLength;
    public float KnotSize;
    public int MissFrames;
    public int LatchFrames;
    public int HoldFrames;
    public int OffFrames;
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
        text.Append(" grapple miss");
        text.Append(" miss_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" shown_s=").Append(ShownSeconds.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" stub_m=").Append(StubLength.ToString("0.00", c));
        text.Append(" on_miss=").Append(MissFrames.ToString(c));
        text.Append(" on_latch=").Append(LatchFrames.ToString(c));
        text.Append(" on_hold=").Append(HoldFrames.ToString(c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
