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
/// Headless check: the solo pawn flashes two small rings at punch reach
/// when a punch hits or a tag connects, then stops. A whiff stays quiet.
/// Opponent and couch stay quiet. Jump, dash, cling, and punch stay put.
/// </summary>
public static class HitConfirmTellProof
{
    const float Dt = 1f / 60f;

    public static HitConfirmTellReport Run()
    {
        var report = new HitConfirmTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.FlashSeconds = HitConfirmTell.FlashSeconds;
        report.MaxAlpha = HitConfirmTell.MaxAlpha;
        report.InnerRadius = HitConfirmTell.InnerRadius;
        report.OuterRadius = HitConfirmTell.OuterRadius;
        report.RingWidth = HitConfirmTell.RingWidth;
        report.ContactHeight = HitConfirmTell.ContactHeight;
        report.Reach = punch.reach;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;

        if (HitConfirmTell.FlashSeconds < 0.22f || HitConfirmTell.FlashSeconds > 0.30f)
            report.Fail("confirm is outside 0.22-0.30s");
        if (HitConfirmTell.MaxAlpha > 0.45f)
            report.Fail("confirm alpha hides the body");
        if (HitConfirmTell.MaxAlpha < 0.32f)
            report.Fail("confirm alpha is too faint to read");
        if (Mathf.Abs(HitConfirmTell.Alpha(0f) - HitConfirmTell.MaxAlpha) > 0.001f)
            report.Fail("confirm does not open at peak alpha");
        if (HitConfirmTell.Alpha(HitConfirmTell.FlashSeconds) != 0f || HitConfirmTell.Alpha(-1f) != 0f)
            report.Fail("confirm alpha stays up after the window");
        if (HitConfirmTell.Alpha(0.31f) != 0f)
            report.Fail("confirm alpha is still up at 0.31s");
        if (HitConfirmTell.RingCount != 2)
            report.Fail("confirm is not two rings");
        if (HitConfirmTell.InnerRadius < 0.10f || HitConfirmTell.OuterRadius > 0.45f
            || HitConfirmTell.OuterRadius <= HitConfirmTell.InnerRadius)
            report.Fail("rings cover the body or are too small to read");
        if (HitConfirmTell.RingWidth < 0.02f || HitConfirmTell.RingWidth > 0.08f)
            report.Fail("confirm stroke is a hairline or wide enough to hide the arm");
        if (HitConfirmTell.ContactHeight < 0.70f || HitConfirmTell.ContactHeight > 1.35f)
            report.Fail("confirm sits on the ground or above the head");
        if (Mathf.Abs(HitConfirmTell.ContactHeight - punch.midTorsoHeight) > 0.001f)
            report.Fail("contact height drifted off the punch torso");
        if (Mathf.Abs(HitConfirmTell.TagCatchOn - 0.04f) > 0.0001f || Mathf.Abs(HitConfirmTell.BecomeItOn - 0.20f) > 0.0001f)
            report.Fail("tag gates drifted off the clip");
        if (HitConfirmTell.TagCatch(0.04f) || HitConfirmTell.BecomeIt(0.20f) || HitConfirmTell.TagConnect(0.039f, 0.19f))
            report.Fail("tag connect is on below the clip");
        if (!HitConfirmTell.TagCatch(0.041f) || !HitConfirmTell.BecomeIt(0.201f))
            report.Fail("TagCatch or BecomeIt did not connect");
        if (!HitConfirmTell.TagConnect(1f, 0f) || !HitConfirmTell.TagConnect(0f, 1f) || !HitConfirmTell.TagConnect(1f, 1f))
            report.Fail("a landed tag did not connect");
        if (HitConfirmTell.MarkR > 0.97f && HitConfirmTell.MarkG > 0.97f && HitConfirmTell.MarkB > 0.97f)
            report.Fail("confirm is white enough to wash the body");
        if (HitConfirmTell.MarkR < 0.80f || HitConfirmTell.MarkR <= HitConfirmTell.MarkG || HitConfirmTell.MarkR <= HitConfirmTell.MarkB)
            report.Fail("confirm is not a strike color");
        if (HitConfirmTell.Glow != 0f)
            report.Fail("confirm glows");
        if (HitConfirmTell.VerticalImpulse != 0f)
            report.Fail("confirm adds a hop");

        Vector3 origin = new Vector3(4f, 0.2f, -3f);
        Vector3 pitched = new Vector3(1f, 4f, 1f);
        Vector3 contact = HitConfirmTell.Contact(origin, pitched, punch.reach);
        if (!ContactFits(report, origin, pitched, contact, punch.reach))
            report.Fail("contact left punch reach");
        Vector3 straightUp = HitConfirmTell.Contact(origin, new Vector3(0f, 5f, 0f), punch.reach);
        Vector3 upDelta = straightUp - origin;
        if (Mathf.Abs(upDelta.x) > 0.001f || Mathf.Abs(upDelta.z - punch.reach) > 0.001f
            || Mathf.Abs(upDelta.y - HitConfirmTell.ContactHeight) > 0.001f)
            report.Fail("a vertical aim put the contact inside the body");
        Vector3 zeroFwd = HitConfirmTell.Contact(origin, Vector3.zero, punch.reach);
        Vector3 zeroDelta = zeroFwd - origin;
        if (Mathf.Abs(zeroDelta.z - punch.reach) > 0.001f || Mathf.Abs(zeroDelta.x) > 0.001f)
            report.Fail("a zero aim put the contact inside the body");

        var buffer = new Vector3[HitConfirmTell.MaxPoints];
        if (!HitConfirmTell.Ring(contact, pitched, HitConfirmTell.InnerRadius, buffer, out int innerCount)
            || !RingFits(report, contact, pitched, buffer, innerCount, HitConfirmTell.InnerRadius))
            report.Fail("inner ring left the contact");
        if (!HitConfirmTell.Ring(contact, pitched, HitConfirmTell.OuterRadius, buffer, out int outerCount)
            || !RingFits(report, contact, pitched, buffer, outerCount, HitConfirmTell.OuterRadius))
            report.Fail("outer ring left the contact");
        if (Closest(origin, buffer, outerCount) < 1.20f)
            report.Fail("outer ring reaches the body");
        if (HitConfirmTell.Ring(contact, pitched, 0.01f, buffer, out _))
            report.Fail("a hairline ring still drew");
        if (HitConfirmTell.Ring(contact, pitched, HitConfirmTell.OuterRadius, null, out _))
            report.Fail("a missing buffer still drew");
        var tiny = new Vector3[2];
        if (HitConfirmTell.Ring(contact, pitched, HitConfirmTell.OuterRadius, tiny, out _))
            report.Fail("a short buffer still drew");

        float punchShown = Play(report, true, false, "punch hit");
        float tagShown = Play(report, false, true, "tag");
        float bothShown = Play(report, true, true, "punch and tag");
        report.ShownSeconds = punchShown;
        report.TagShownSeconds = tagShown;
        if (punchShown < 0.22f || punchShown > 0.30f)
            report.Fail("stepped punch confirm is outside 0.22-0.30s");
        if (tagShown < 0.22f || tagShown > 0.30f)
            report.Fail("stepped tag confirm is outside 0.22-0.30s");
        if (Mathf.Abs(punchShown - HitConfirmTell.FlashSeconds) > Dt + 0.001f)
            report.Fail("stepped punch confirm does not match the window");
        if (Mathf.Abs(tagShown - punchShown) > 0.001f || Mathf.Abs(bothShown - punchShown) > 0.001f)
            report.Fail("tag confirm does not use the punch window");

        float held = -1f;
        HitConfirmTell.Note(ref held, true, true, false, false, false);
        int guard = 0;
        while (HitConfirmTell.Show(true, held))
        {
            HitConfirmTell.Step(ref held, Dt, true);
            if (++guard > 40)
            {
                report.Fail("held-hit confirm did not stop");
                break;
            }
        }

        for (int i = 0; i < 40; i++)
        {
            HitConfirmTell.Note(ref held, true, true, false, true, false);
            if (HitConfirmTell.Show(true, held) || held >= 0f)
                report.Fail("a held hit replayed the confirm");
            if (HitConfirmTell.Step(ref held, Dt, true))
                report.Fail("a finished confirm restarted itself");
        }

        HitConfirmTell.Note(ref held, true, false, false, true, false);
        HitConfirmTell.Note(ref held, true, true, false, false, false);
        if (!HitConfirmTell.Show(true, held))
            report.Fail("a new hit did not confirm");
        HitConfirmTell.Step(ref held, Dt, true);

        report.MissFrames = CountMiss(report);
        if (report.MissFrames != 0)
            report.Fail("a whiff drew a confirm");

        float below = -1f;
        bool belowTag = HitConfirmTell.TagConnect(0.02f, 0.10f);
        HitConfirmTell.Note(ref below, true, false, belowTag, false, false);
        if (belowTag || HitConfirmTell.Show(true, below) || below >= 0f)
            report.Fail("a short flinch confirmed");

        report.OpponentFrames = CountQuiet(false, true, 1, SoloGrappleGate.OpponentPawnName, report, "opponent");
        report.CouchFrames = CountQuiet(true, false, 0, SoloGrappleGate.SoloPawnName, report, "couch");
        if (HitConfirmTell.ForPawn(false, false, 1, "Player_P1")
            || HitConfirmTell.ForPawn(true, false, 1, "Player_P1")
            || HitConfirmTell.ForPawn(false, false, 0, SoloGrappleGate.OpponentPawnName))
            report.Fail("hit confirm leaked off the solo pawn");
        if (!HitConfirmTell.ForPawn(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn hit confirm is off");
        if (report.OpponentFrames != 0)
            report.Fail("the opponent drew a hit confirm");
        if (report.CouchFrames != 0)
            report.Fail("a couch pawn drew a hit confirm");

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

        string tellSrc = ReadRepo("Assets/Scripts/Art/HitConfirmTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string lungeSrc = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        string burstSrc = ReadRepo("Assets/Scripts/Art/AirDashTell.cs");
        if (tellSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || lungeSrc == null || burstSrc == null)
            report.Fail("hit confirm sources missing");
        else
            CheckWiring(report, tellSrc, locoSrc, motorSrc, cfgSrc, lungeSrc, burstSrc);

        if (tellSrc != null && tellSrc.Contains("1.55"))
            report.Fail("hit confirm forked a reach");

        if (VerbPoseClips.SlideBody != "SlideBody" || VerbPoseClips.PunchStrike != "PunchStrike" || VerbPoseClips.TagCatch != "TagCatch")
            report.Fail("verb clip names drifted");
        if (ReadRepo("Assets/Scripts/Art/AirDashCooldownTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleLatchTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/OpponentChaseTell.cs") == null
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
            report.Fail("solo pawn gate is off");
        if (SoloGrappleGate.EnableFor(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(true, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("opponent or couch gained the solo gate");

        if (Mathf.Abs(report.JumpSpeed - cfg.jumpSpeed) > 0.001f || report.SlideBoost != cfg.slideBoost)
            report.Fail("the tell rewrote jumpSpeed or slideBoost");

        return report;
    }

    static float Play(HitConfirmTellReport report, bool punchHit, bool tagConnect, string label)
    {
        float age = -1f;
        HitConfirmTell.Note(ref age, true, punchHit, tagConnect, false, false);
        if (!HitConfirmTell.Show(true, age))
        {
            report.Fail(label + " did not arm");
            return 0f;
        }

        if (Mathf.Abs(HitConfirmTell.Alpha(age) - HitConfirmTell.MaxAlpha) > 0.001f)
            report.Fail(label + " did not open at peak alpha");

        float shown = 0f;
        int guard = 0;
        while (HitConfirmTell.Show(true, age))
        {
            shown += Dt;
            bool still = HitConfirmTell.Step(ref age, Dt, true);
            if (!still && HitConfirmTell.Show(true, age))
                report.Fail(label + " stepped off while still showing");
            if (++guard > 40)
            {
                report.Fail(label + " did not stop");
                break;
            }
        }

        if (HitConfirmTell.Show(true, age) || age >= 0f)
            report.Fail(label + " stayed armed after it stopped");
        if (HitConfirmTell.Step(ref age, Dt, true))
            report.Fail(label + " restarted itself");
        return shown;
    }

    static int CountMiss(HitConfirmTellReport report)
    {
        float age = -1f;
        bool wasPunch = false;
        bool wasTag = false;
        int shown = 0;
        for (int i = 0; i < 48; i++)
        {
            bool tag = HitConfirmTell.TagConnect(0.02f, 0.10f);
            HitConfirmTell.Note(ref age, true, false, tag, wasPunch, wasTag);
            wasPunch = false;
            wasTag = tag;
            if (HitConfirmTell.Show(true, age))
                shown++;
            if (HitConfirmTell.Step(ref age, Dt, true))
                report.Fail("a whiff stepped into a confirm");
            if (age >= 0f)
                report.Fail("a whiff armed a confirm");
        }

        return shown;
    }

    static int CountQuiet(bool couch, bool ai, int index, string pawnName, HitConfirmTellReport report, string label)
    {
        bool solo = HitConfirmTell.ForPawn(couch, ai, index, pawnName);
        if (solo)
            report.Fail(label + " was treated as the solo pawn");
        float age = -1f;
        bool wasPunch = false;
        bool wasTag = false;
        int shown = 0;
        for (int i = 0; i < 40; i++)
        {
            bool punch = i > 5;
            bool tag = i > 10;
            HitConfirmTell.Note(ref age, solo, punch, tag, wasPunch, wasTag);
            wasPunch = punch;
            wasTag = tag;
            if (HitConfirmTell.Show(solo, age))
                shown++;
            if (HitConfirmTell.Step(ref age, Dt, solo))
                report.Fail(label + " stepped into a confirm");
            if (age >= 0f)
                report.Fail(label + " armed a confirm");
        }

        return shown;
    }

    static bool ContactFits(HitConfirmTellReport report, Vector3 origin, Vector3 forward, Vector3 contact, float reach)
    {
        Vector3 delta = contact - origin;
        float horiz = Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
        if (Mathf.Abs(horiz - reach) > 0.001f)
            return false;
        if (Mathf.Abs(delta.y - HitConfirmTell.ContactHeight) > 0.001f)
            return false;
        Vector3 flat = new Vector3(forward.x, 0f, forward.z);
        flat.Normalize();
        Vector3 got = new Vector3(delta.x, 0f, delta.z);
        got.Normalize();
        float dot = got.x * flat.x + got.z * flat.z;
        if (dot < 0.999f)
            report.Fail("contact is not along the aim");
        return true;
    }

    static bool RingFits(HitConfirmTellReport report, Vector3 contact, Vector3 forward, Vector3[] buffer, int count, float radius)
    {
        if (count != HitConfirmTell.MaxPoints || count < 4)
            return false;
        Vector3 flat = new Vector3(forward.x, 0f, forward.z);
        flat.Normalize();
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            Vector3 d = buffer[i] - contact;
            float dist = d.magnitude;
            if (Mathf.Abs(dist - radius) > 0.02f)
                return false;
            float along = d.x * flat.x + d.z * flat.z;
            if (Mathf.Abs(along) > 0.02f)
                return false;
            if (d.y < minY) minY = d.y;
            if (d.y > maxY) maxY = d.y;
        }

        if (maxY - minY < radius)
            report.Fail("confirm ring is flat on the ground");
        Vector3 ends = buffer[count - 1] - buffer[0];
        return ends.sqrMagnitude < 0.0001f;
    }

    static float Closest(Vector3 origin, Vector3[] buffer, int count)
    {
        float best = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            float d = (buffer[i] - origin).magnitude;
            if (d < best) best = d;
        }

        return best;
    }

    static void CheckLocks(HitConfirmTellReport report, MovementConfig cfg, PunchTagTuning punch)
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
        HitConfirmTellReport report,
        string tellSrc, string locoSrc, string motorSrc, string cfgSrc, string lungeSrc, string burstSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera") || tellSrc.Contains("InputAction")
            || tellSrc.Contains("airDashDuration") || tellSrc.Contains("airDashSpeed") || tellSrc.Contains("airDashCooldown"))
            report.Fail("confirm constants write feel, input, or the camera");

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
        if (motorSrc.Contains("HitConfirmTell") || motorSrc.Contains("FixedUpdate"))
            report.Fail("hit confirm leaked into the motor");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickHitConfirm");
        string solo = MethodBody(locoSrc, "bool HitConfirmSolo");
        string place = MethodBody(locoSrc, "void PlaceHitConfirm");
        string ensure = MethodBody(locoSrc, "void EnsureHitConfirm");
        string ring = MethodBody(locoSrc, "LineRenderer MakeHitConfirmRing");
        string mat = MethodBody(locoSrc, "static Material MakeHitConfirmMat");
        string hide = MethodBody(locoSrc, "void HideHitConfirm");
        string paint = MethodBody(locoSrc, "void PaintHitConfirm");
        string apply = MethodBody(locoSrc, "void ApplyHitRing");
        string clips = MethodBody(locoSrc, "void ApplyVerbClips");
        string flinch = MethodBody(locoSrc, "public void PlayTagFlinch");
        string claim = MethodBody(locoSrc, "public void PlayItClaim");
        if (late == null || tick == null || solo == null || place == null || ensure == null || ring == null
            || mat == null || hide == null || paint == null || apply == null || clips == null
            || flinch == null || claim == null)
        {
            report.Fail("hit confirm is not on the solo visual path");
            return;
        }

        int call = late.IndexOf("TickHitConfirm(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("hit confirm does not run with the other presentation tells");
        if (!clips.Contains("VerbPoseClips.PunchStrike") || !clips.Contains("VerbPoseClips.TagCatch")
            || !clips.Contains("flinchAmt > 0.04f"))
            report.Fail("punch or tag clip binding drifted");
        if (clips.Contains("HitConfirmTell"))
            report.Fail("the confirm was baked into the clip");
        if (!flinch.Contains("_tagFlinch = 1f") || flinch.Contains("HitConfirmTell"))
            report.Fail("TagCatch hook was rewritten");
        if (!claim.Contains("_itClaim = 1f") || claim.Contains("HitConfirmTell"))
            report.Fail("BecomeIt hook was rewritten");

        if (!solo.Contains("HitConfirmTell.ForPawn") || !solo.Contains("LocalPlayerRoster.IsCouch")
            || !solo.Contains("SoloGrappleGate.OpponentPawnName"))
            report.Fail("hit confirm is not gated to the solo pawn");
        if (!tick.Contains("phase == PunchPhase.HitRecover") || !tick.Contains("HitConfirmTell.TagConnect")
            || !tick.Contains("_tagFlinch") || !tick.Contains("_itClaim")
            || !tick.Contains("HitConfirmTell.Note") || !tick.Contains("HitConfirmTell.Show")
            || !tick.Contains("HitConfirmTell.Step") || !tick.Contains("HitConfirmTell.Alpha"))
            report.Fail("hit confirm does not follow the punch hit and the tag connect");
        if (tick.Contains("PunchPhase.MissRecover") || tick.Contains("PunchPhase.Windup") || tick.Contains("PunchPhase.Active"))
            report.Fail("a whiff or a cock arms the confirm");
        int gate = tick.IndexOf("if (!HitConfirmTell.Show", StringComparison.Ordinal);
        int built = tick.IndexOf("EnsureHitConfirm(", StringComparison.Ordinal);
        if (gate < 0 || built < 0 || gate > built)
            report.Fail("a whiff still builds the confirm rings");
        if (!place.Contains("HitConfirmTell.Contact") || !place.Contains("HitConfirmTell.Ring")
            || !place.Contains("_punch.Reach") || !place.Contains("HitConfirmTell.InnerRadius")
            || !place.Contains("HitConfirmTell.OuterRadius"))
            report.Fail("the rings are not placed at punch reach");
        if (!apply.Contains("HitConfirmTell.RingWidth"))
            report.Fail("the rings do not use the tell width");
        if (!ring.Contains("HitConfirmTell.RingWidth") || !ring.Contains("HitConfirmTell.MaxAlpha")
            || !mat.Contains("HitConfirmTell.MaxAlpha"))
            report.Fail("the rings do not use the tell width and alpha");

        string bodies = tick + solo + place + ensure + ring + mat + hide + paint + apply;
        if (bodies.Contains("1.55") || bodies.Contains("CreatePrimitive") || bodies.Contains("Collider")
            || bodies.Contains("Rigidbody") || bodies.Contains("AddComponent<Light>") || bodies.Contains("Light")
            || bodies.Contains("_EmissionColor") || bodies.Contains("_EMISSION")
            || bodies.Contains("FovPop") || bodies.Contains("fieldOfView") || bodies.Contains("InputAction")
            || bodies.Contains("AddForce") || bodies.Contains("jumpSpeed") || bodies.Contains("slideBoost"))
            report.Fail("hit confirm adds a glow, a volume, a reach, or a feel write");
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

public sealed class HitConfirmTellReport
{
    public float FlashSeconds;
    public float ShownSeconds;
    public float TagShownSeconds;
    public float MaxAlpha;
    public float InnerRadius;
    public float OuterRadius;
    public float RingWidth;
    public float ContactHeight;
    public float Reach;
    public int MissFrames;
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
        text.Append(" hit confirm flash_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" shown_s=").Append(ShownSeconds.ToString("0.00", c));
        text.Append(" tag_s=").Append(TagShownSeconds.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" inner=").Append(InnerRadius.ToString("0.00", c));
        text.Append(" outer=").Append(OuterRadius.ToString("0.00", c));
        text.Append(" reach=").Append(Reach.ToString("0.00", c));
        text.Append(" miss=").Append(MissFrames.ToString(c));
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
