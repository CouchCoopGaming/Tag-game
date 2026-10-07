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
/// Headless check: the solo pawn kicks and scuffs the wall on a real
/// wall-jump exit, then stops. Ground jump, coyote re-ground, slide start,
/// cling without Jump, and mantle stay at 0. A land-tell frame does not
/// also arm. Opponent and couch stay quiet. Jump numbers stay put.
/// </summary>
public static class WallJumpPushTellProof
{
    const float Dt = 1f / 60f;

    public static WallJumpPushTellReport Run()
    {
        var report = new WallJumpPushTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.FlashSeconds = WallJumpPushTell.FlashSeconds;
        report.PushOffset = WallJumpPushTell.PushOffset;
        report.StretchY = WallJumpPushTell.StretchY;
        report.StretchXZ = WallJumpPushTell.StretchXZ;
        report.MaxAlpha = WallJumpPushTell.MaxAlpha;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;
        report.ClingGrace = cfg.clingReleaseGrace;

        if (WallJumpPushTell.FlashSeconds < 0.15f || WallJumpPushTell.FlashSeconds > 0.30f)
            report.Fail("push tell is outside 0.15-0.30s");
        if (WallJumpPushTell.FlashSeconds < 0.18f || WallJumpPushTell.FlashSeconds > 0.28f)
            report.Fail("push tell is not a short kick");
        if (WallJumpPushTell.PushOffset < 0.10f || WallJumpPushTell.PushOffset > 0.24f)
            report.Fail("pose kick is invisible or a teleport");
        if (WallJumpPushTell.StretchY < 1.05f || WallJumpPushTell.StretchY > 1.16f)
            report.Fail("pose stretch is a flicker or a launch");
        if (WallJumpPushTell.StretchXZ > 0.97f || WallJumpPushTell.StretchXZ < 0.86f)
            report.Fail("pose width is unchanged or a squeeze");
        WallJumpPushTell.PushScale(1f, out float peakY, out float peakXZ);
        WallJumpPushTell.PushScale(0f, out float restY, out float restXZ);
        if (Mathf.Abs(peakY - WallJumpPushTell.StretchY) > 0.001f
            || Mathf.Abs(peakXZ - WallJumpPushTell.StretchXZ) > 0.001f)
            report.Fail("kick frame is not the pose accent");
        if (Mathf.Abs(restY - 1f) > 0.001f || Mathf.Abs(restXZ - 1f) > 0.001f)
            report.Fail("a quiet frame still scales the body");
        WallJumpPushTell.PushScale(0.5f, out float midY, out float midXZ);
        if (midY >= peakY || midY <= 1f || midXZ <= peakXZ || midXZ >= 1f)
            report.Fail("kick does not ease back to the pose");
        if (WallJumpPushTell.MaxAlpha < 0.42f || WallJumpPushTell.MaxAlpha > 0.68f)
            report.Fail("foot scuffs are too faint or hide the shoes");
        if (Mathf.Abs(WallJumpPushTell.Alpha(0f) - WallJumpPushTell.MaxAlpha) > 0.001f)
            report.Fail("scuffs do not open at peak alpha");
        if (WallJumpPushTell.Alpha(WallJumpPushTell.FlashSeconds) != 0f || WallJumpPushTell.Alpha(-1f) != 0f)
            report.Fail("scuff alpha stays up after the window");
        if (WallJumpPushTell.PuffCount != 2)
            report.Fail("push tell is not two foot scuffs");
        if (Mathf.Abs(WallJumpPushTell.PuffDiameter(1f) - WallJumpPushTell.PuffTight) > 0.001f)
            report.Fail("scuff does not start tight");
        if (Mathf.Abs(WallJumpPushTell.PuffDiameter(0f) - WallJumpPushTell.PuffWide) > 0.001f)
            report.Fail("scuff does not bloom");
        if (WallJumpPushTell.PuffTight < 0.10f || WallJumpPushTell.PuffWide > 0.42f
            || WallJumpPushTell.PuffWide <= WallJumpPushTell.PuffTight)
            report.Fail("scuffs are a speck or large enough to cover the pawn");
        if (WallJumpPushTell.FootHeight < 0.10f || WallJumpPushTell.FootHeight > 0.45f)
            report.Fail("scuffs left the shoes");
        if (WallJumpPushTell.FootHeight > WallClingTell.RunHeight - 0.40f)
            report.Fail("foot scuffs sit on the cling hand marks");
        if (WallJumpPushTell.FootSpread <= 0.06f || WallJumpPushTell.FootSpread >= cfg.radius)
            report.Fail("scuffs leave the feet or sit on the same point");
        float skinGap = cfg.radius - WallJumpPushTell.SurfaceOffset;
        if (skinGap < -0.04f || skinGap > 0.08f)
            report.Fail("scuffs left the wall skin");
        if (WallJumpPushTell.PairSpan > cfg.radius * 2f + 0.20f)
            report.Fail("scuffs spread wider than a foot plant");
        if (WallJumpPushTell.MarkR > 0.97f && WallJumpPushTell.MarkG > 0.97f && WallJumpPushTell.MarkB > 0.97f)
            report.Fail("scuffs are white enough to wash the body");
        if (WallJumpPushTell.MarkR < 0.70f || WallJumpPushTell.MarkG < 0.55f || WallJumpPushTell.MarkB > WallJumpPushTell.MarkG)
            report.Fail("scuffs do not read as wall dust");
        if (WallJumpPushTell.Glow != 0f)
            report.Fail("push tell glows");
        if (WallJumpPushTell.VerticalImpulse != 0f)
            report.Fail("push tell adds a hop");

        Vector3 origin = new Vector3(2f, 0.4f, -6f);
        Vector3 wallNormal = Vector3.forward;
        if (!WallJumpPushTell.Feet(origin, wallNormal, out Vector3 left, out Vector3 right))
            report.Fail("a wall normal did not place the feet");
        if (Mathf.Abs(left.y - (origin.y + WallJumpPushTell.FootHeight)) > 0.001f
            || Mathf.Abs(right.y - (origin.y + WallJumpPushTell.FootHeight)) > 0.001f)
            report.Fail("scuffs left shoe height");
        if (Mathf.Abs(left.z - (origin.z - WallJumpPushTell.SurfaceOffset)) > 0.001f
            || Mathf.Abs(right.z - (origin.z - WallJumpPushTell.SurfaceOffset)) > 0.001f)
            report.Fail("scuffs are not on the wall");
        if (Mathf.Abs(Mathf.Abs(left.x - origin.x) - WallJumpPushTell.FootSpread) > 0.001f
            || Mathf.Abs(Mathf.Abs(right.x - origin.x) - WallJumpPushTell.FootSpread) > 0.001f)
            report.Fail("scuffs are not split across the feet");
        if ((left - right).magnitude <= 0.05f)
            report.Fail("foot scuffs stacked");
        if (WallJumpPushTell.Feet(origin, Vector3.zero, out _, out _)
            || WallJumpPushTell.Feet(origin, Vector3.up, out _, out _))
            report.Fail("a zero wall still placed scuffs");

        Vector3 push = WallJumpPushTell.PushWorld(wallNormal, 1f);
        if (Mathf.Abs(push.z - WallJumpPushTell.PushOffset) > 0.001f || Mathf.Abs(push.x) > 0.001f || Mathf.Abs(push.y) > 0.001f)
            report.Fail("pose kick is not along the wall normal");
        Vector3 halfPush = WallJumpPushTell.PushWorld(wallNormal, 0.5f);
        if (halfPush.magnitude >= push.magnitude || halfPush.sqrMagnitude < 0.0001f)
            report.Fail("pose kick does not ease out");
        if (WallJumpPushTell.PushWorld(wallNormal, 0f).sqrMagnitude > 0.0001f
            || WallJumpPushTell.PushWorld(Vector3.up, 1f).sqrMagnitude > 0.0001f
            || WallJumpPushTell.PushWorld(Vector3.zero, 1f).sqrMagnitude > 0.0001f)
            report.Fail("a quiet or vertical normal still shoved the pose");

        if (!WallJump(true, true, true, true, true, true, false, false, false, false, false))
            report.Fail("a wall climb jump did not qualify");
        if (!WallJump(true, true, true, true, true, true, false, false, false, false, false))
            report.Fail("a wall run jump did not qualify");
        if (WallJump(true, false, false, true, true, true, false, false, false, true, false))
            report.Fail("a ground jump qualified as a wall jump");
        if (WallJump(true, true, true, false, false, false, false, false, true, false, false))
            report.Fail("a coyote re-ground qualified as a wall jump");
        if (WallJump(true, true, true, true, true, true, true, false, false, false, false))
            report.Fail("a slide start qualified as a wall jump");
        if (WallJump(true, true, true, false, false, false, false, false, false, false, false))
            report.Fail("cling without Jump qualified as a wall jump");
        if (WallJump(true, true, true, false, true, true, false, false, false, false, false))
            report.Fail("a wall release qualified as a wall jump");
        if (WallJump(true, true, true, true, true, false, false, true, false, false, false))
            report.Fail("a mantle qualified as a wall jump");
        if (WallJump(false, true, true, true, true, true, false, false, false, false, false))
            report.Fail("a pawn that is not solo qualified");
        if (WallJump(true, true, true, true, true, true, false, false, false, false, true))
            report.Fail("a land-tell frame also qualified the push");
        if (WallJump(true, true, true, true, false, true, false, false, false, false, false))
            report.Fail("a grounded wall contact qualified as a wall jump");

        float landAge = -1f;
        float landAir = 1f;
        JumpLandTell.Note(ref landAge, ref landAir, true, false, false, false, false, Dt);
        if (JumpLandTell.Show(true, landAge))
            report.Fail("an airborne wall-jump frame armed the land tell");
        landAir = JumpLandTell.MinAirSeconds;
        JumpLandTell.Note(ref landAge, ref landAir, true, true, false, false, true, Dt);
        if (JumpLandTell.Show(true, landAge))
            report.Fail("a wall cling armed the land tell");

        report.ShownSeconds = PlayPush(report, origin, wallNormal);
        if (report.ShownSeconds < 0.15f || report.ShownSeconds > 0.30f)
            report.Fail("shown push tell left 0.15-0.30s");

        report.WallFrames = report.PushFrames;
        report.GroundFrames = CountBlocked(report, false, false, true, true, true, false, false, false, true, false);
        report.CoyoteFrames = CountBlocked(report, true, true, false, false, false, false, false, true, false, false);
        report.SlideFrames = CountBlocked(report, true, true, true, true, true, true, false, false, false, false);
        report.ClingFrames = CountBlocked(report, true, true, false, false, false, false, false, false, false, false);
        report.MantleFrames = CountBlocked(report, true, true, true, true, false, false, true, false, false, false);
        report.CouchFrames = CountBlocked(report, true, true, true, true, true, false, false, false, false, false, false);
        report.SameFrameFrames = CountBlocked(report, true, true, true, true, true, false, false, false, false, true);
        if (report.WallFrames <= 0)
            report.Fail("a wall jump showed 0 frames");
        if (report.GroundFrames != 0)
            report.Fail("a ground jump kept the push tell up");
        if (report.CoyoteFrames != 0)
            report.Fail("a coyote re-ground kept the push tell up");
        if (report.SlideFrames != 0)
            report.Fail("a slide start kept the push tell up");
        if (report.ClingFrames != 0)
            report.Fail("cling without Jump kept the push tell up");
        if (report.MantleFrames != 0)
            report.Fail("a mantle kept the push tell up");
        if (report.CouchFrames != 0)
            report.Fail("a couch or opponent pawn kept the push tell up");
        if (report.SameFrameFrames != 0)
            report.Fail("the push tell armed on a land-tell frame");

        if (!WallJumpPushTell.ForPawn(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn push tell is off");
        if (WallJumpPushTell.ForPawn(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || WallJumpPushTell.ForPawn(false, false, 0, SoloGrappleGate.OpponentPawnName)
            || WallJumpPushTell.ForPawn(true, false, 0, SoloGrappleGate.SoloPawnName)
            || WallJumpPushTell.ForPawn(true, false, 1, "Player_P1")
            || WallJumpPushTell.ForPawn(false, false, 2, SoloGrappleGate.SoloPawnName))
            report.Fail("push tell leaked off the solo pawn");

        if (JumpLandTell.MarkerName != "JumpLandTell" || WallClingTell.MarkerName != "WallClingTell")
            report.Fail("an existing tell marker was renamed");
        if (Mathf.Abs(JumpLandTell.FlashSeconds - 0.24f) > 0.001f || Mathf.Abs(JumpLandTell.MinAirSeconds - 0.22f) > 0.001f)
            report.Fail("land tell timing changed");
        if (WallClingTell.MarkCount != 2 || Mathf.Abs(WallClingTell.RibbonTime - 0.15f) > 0.001f)
            report.Fail("cling hand marks changed");

        CheckLocks(report, cfg, punch);

        string moveAsset = ReadRepo("Assets/Resources/TagArena/MovementConfig.asset");
        string punchAsset = ReadRepo("Assets/ScriptableObjects/PunchTagTuning.asset");
        if (moveAsset == null)
            report.Fail("MovementConfig asset missing");
        else if (!Has(moveAsset, "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
            "jumpSpeed: 24.7", "slideBoost: 0", "enableJet: 0"))
            report.Fail("MovementConfig asset drifted");
        if (punchAsset == null)
            report.Fail("PunchTagTuning asset missing");
        else if (!Has(punchAsset, "reach: 1.55"))
            report.Fail("PunchTagTuning reach drifted");

        string tellSrc = ReadRepo("Assets/Scripts/Art/WallJumpPushTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string binderSrc = ReadRepo("Assets/Scripts/Art/DummyAvatarBinder.cs");
        if (tellSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || spawnSrc == null || binderSrc == null)
            report.Fail("push tell sources missing");
        else
            CheckWiring(report, tellSrc, locoSrc, motorSrc, cfgSrc, spawnSrc, binderSrc);

        if (ReadRepo("Assets/Scripts/Art/JumpLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/WallClingTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs") == null)
            report.Fail("an existing tell was removed");

        if (punch.reach != 1.55f)
            report.Fail("punch reach changed");

        return report;
    }

    static bool WallJump(
        bool solo,
        bool wasWall,
        bool cling,
        bool jump,
        bool airborne,
        bool leftWall,
        bool slide,
        bool mantle,
        bool coyote,
        bool groundJump,
        bool landBusy)
    {
        return WallJumpPushTell.Qualifies(
            solo, wasWall, cling, jump, airborne, leftWall, slide, mantle, coyote, groundJump, landBusy);
    }

    static float PlayPush(WallJumpPushTellReport report, Vector3 origin, Vector3 wallNormal)
    {
        float age = -1f;
        bool qualifies = WallJump(true, true, true, true, true, true, false, false, false, false, false);
        WallJumpPushTell.Note(ref age, qualifies, true);
        if (!WallJumpPushTell.Show(true, age) || age != 0f)
        {
            report.Fail("a real wall jump did not open the cue");
            return 0f;
        }

        if (!WallJumpPushTell.Feet(origin, wallNormal, out Vector3 left, out Vector3 right))
            report.Fail("the wall jump did not place the feet");
        if ((left - right).magnitude <= 0.05f)
            report.Fail("foot scuffs stacked on the kick");
        Vector3 kick = WallJumpPushTell.PushWorld(wallNormal, WallJumpPushTell.Fade(age));
        if (kick.sqrMagnitude < 0.01f)
            report.Fail("the kick frame did not leave the wall");

        float shown = 0f;
        int frames = 0;
        int guard = 0;
        while (WallJumpPushTell.Show(true, age))
        {
            WallJumpPushTell.PushScale(WallJumpPushTell.Fade(age), out float y, out float xz);
            if (y < 1f || xz > 1f)
                report.Fail("the kick inverted");
            if (!WallJumpPushTell.Feet(origin, wallNormal, out _, out _))
                report.Fail("the kick lost its feet");
            float kickFade = WallJumpPushTell.Fade(age);
            if (kickFade > 0.12f
                && WallJumpPushTell.PushWorld(wallNormal, kickFade).sqrMagnitude < 0.0004f)
                report.Fail("the kick lost its push");
            shown += Dt;
            frames++;
            float held = age;
            WallJumpPushTell.Note(ref age, false, true);
            if (age != held)
                report.Fail("a quiet frame restarted or cleared the kick");
            bool still = WallJumpPushTell.Step(ref age, Dt, true);
            if (!still && WallJumpPushTell.Show(true, age))
                report.Fail("step ended while the push tell was still up");
            if (++guard > 40)
            {
                report.Fail("push tell did not stop");
                break;
            }
        }

        report.PushFrames = frames;
        if (WallJumpPushTell.Show(true, age) || age >= 0f)
            report.Fail("push tell stayed armed after it stopped");
        WallJumpPushTell.PushScale(WallJumpPushTell.Fade(age), out float endY, out float endXZ);
        if (Mathf.Abs(endY - 1f) > 0.001f || Mathf.Abs(endXZ - 1f) > 0.001f)
            report.Fail("the body stayed stretched after the window");
        if (WallJumpPushTell.PushWorld(wallNormal, WallJumpPushTell.Fade(age)).sqrMagnitude > 0.0001f)
            report.Fail("the pose stayed offset after the window");
        if (Mathf.Abs(shown - WallJumpPushTell.FlashSeconds) > Dt + 0.001f)
            report.Fail("shown time drifted off the window");

        float marked = age;
        for (int i = 0; i < 12; i++)
        {
            WallJumpPushTell.Note(ref age, false, true);
            WallJumpPushTell.Step(ref age, Dt, true);
            if (age != marked || WallJumpPushTell.Show(true, age))
                report.Fail("air time after the kick replayed it");
        }

        WallJumpPushTell.Note(ref age, true, true);
        if (!WallJumpPushTell.Show(true, age) || age != 0f)
            report.Fail("a second wall jump did not arm");
        WallJumpPushTell.Clear(ref age);
        return shown;
    }

    static int CountBlocked(
        WallJumpPushTellReport report,
        bool wasWall,
        bool cling,
        bool jump,
        bool airborne,
        bool leftWall,
        bool slide,
        bool mantle,
        bool coyote,
        bool groundJump,
        bool landBusy,
        bool solo = true)
    {
        float age = -1f;
        int shown = 0;
        for (int i = 0; i < 40; i++)
        {
            bool qualifies = WallJump(solo, wasWall, cling, jump, airborne, leftWall, slide, mantle, coyote, groundJump, landBusy);
            if (qualifies)
                report.Fail("a blocked exit qualified");
            WallJumpPushTell.Note(ref age, qualifies, solo);
            if (WallJumpPushTell.Show(solo, age))
                shown++;
            if (WallJumpPushTell.Step(ref age, Dt, solo))
                report.Fail("a blocked exit stepped the cue");
            if (age >= 0f)
                report.Fail("a blocked exit left the cue armed");
        }

        return shown;
    }

    static void CheckLocks(WallJumpPushTellReport report, MovementConfig cfg, PunchTagTuning punch)
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
        if (Mathf.Abs(cfg.airCrouchFallMult - 2f) > 0.001f || Mathf.Abs(cfg.maxFallSpeed - 56.16f) > 0.001f)
            report.Fail("fall numbers changed");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail("punch reach is not 1.55");
    }

    static void CheckWiring(
        WallJumpPushTellReport report,
        string tellSrc, string locoSrc, string motorSrc, string cfgSrc, string spawnSrc, string binderSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera") || tellSrc.Contains("InputAction")
            || tellSrc.Contains("timeScale") || tellSrc.Contains("fieldOfView"))
            report.Fail("push tell constants write feel, input, or the camera");

        if (!Has(cfgSrc, "jumpSpeed = 24.7f", "slideBoost = 0f", "clingReleaseGrace = 0.08f",
            "coyoteTime = 0.10f", "jumpBuffer = 0.16f", "enableJet = false"))
            report.Fail("movement defaults drifted");

        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("WallJumpPushTell") || motorSrc.Contains("FixedUpdate"))
            report.Fail("push tell leaked into the motor");
        if (!Has(motorSrc, "OnWallBounced?.Invoke()", "void DoWallRunJump", "void DoWallBounce"))
            report.Fail("wall jump no longer reports the exit");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickWallJumpPush");
        string solo = MethodBody(locoSrc, "bool WallJumpSolo");
        string place = MethodBody(locoSrc, "void PlaceWallJump");
        string ensure = MethodBody(locoSrc, "void EnsureWallJump");
        string hide = MethodBody(locoSrc, "void HideWallJump");
        string puff = MethodBody(locoSrc, "Transform MakeWallJumpPuff");
        string mat = MethodBody(locoSrc, "static Material MakeWallJumpMat");
        string scale = MethodBody(locoSrc, "Vector3 WallJumpPushScale");
        string nudge = MethodBody(locoSrc, "Vector3 WallJumpNudge");
        string bounce = MethodBody(locoSrc, "void HandleWallBounced");
        string land = MethodBody(locoSrc, "void TickJumpLand");
        string cling = MethodBody(locoSrc, "void TickWallCling");
        if (late == null || tick == null || solo == null || place == null || ensure == null
            || hide == null || puff == null || mat == null || scale == null || nudge == null
            || bounce == null || land == null || cling == null)
        {
            report.Fail("push tell is not on the solo visual path");
            return;
        }

        int call = late.IndexOf("TickWallJumpPush(", StringComparison.Ordinal);
        int landCall = late.IndexOf("TickJumpLand(", StringComparison.Ordinal);
        int clingCall = late.IndexOf("TickWallCling(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("push tell does not run with the other presentation tells");
        if (landCall < 0 || clingCall < 0 || landCall > call || clingCall > call)
            report.Fail("push tell runs before the land tell or the cling marks");
        if (!late.Contains("WallJumpPushScale(") || !late.Contains("JumpLandScale("))
            report.Fail("the kick is not applied with the land thud");
        if (!late.Contains("WallJumpNudge("))
            report.Fail("the pose does not kick away from the wall");
        if (!tick.Contains("MoveState.Slide") || !tick.Contains("MoveState.Mantle")
            || !tick.Contains("MoveState.WallClimb") || !tick.Contains("MoveState.WallRun")
            || !tick.Contains("MoveState.Air"))
            report.Fail("slide, mantle, or cling can still arm the push tell");
        if (!tick.Contains("WallJumpPushTell.Qualifies") || !tick.Contains("WallJumpPushTell.Note")
            || !tick.Contains("WallJumpPushTell.Show") || !tick.Contains("WallJumpPushTell.Step")
            || !tick.Contains("_wallJumpEdge") || !tick.Contains("clingReleaseGrace")
            || !tick.Contains("coyoteTime") || !tick.Contains("JumpLandTell.Show"))
            report.Fail("push tell does not follow a wall-jump exit");
        int gate = tick.IndexOf("if (!WallJumpPushTell.Show", StringComparison.Ordinal);
        int built = tick.IndexOf("EnsureWallJump(", StringComparison.Ordinal);
        if (gate < 0 || built < 0 || gate > built)
            report.Fail("a quiet frame still builds the foot scuffs");
        if (!solo.Contains("WallJumpPushTell.ForPawn") || !solo.Contains("LocalPlayerRoster.IsCouch")
            || !solo.Contains("SoloGrappleGate.OpponentPawnName"))
            report.Fail("push tell is not gated to the solo pawn");
        if (!place.Contains("WallJumpPushTell.Feet") || !place.Contains("WallJumpPushTell.PuffDiameter"))
            report.Fail("scuffs are not placed by the tell");
        if (!scale.Contains("WallJumpPushTell.PushScale") || !scale.Contains("WallJumpPushTell.Fade"))
            report.Fail("the kick does not use the tell scale");
        if (!nudge.Contains("WallJumpPushTell.PushWorld"))
            report.Fail("the pose offset is not the tell push");
        if (!mat.Contains("WallJumpPushTell.MaxAlpha")
            || (!puff.Contains("WallJumpPushTell.MarkerName") && !ensure.Contains("WallJumpPushTell.MarkerName")))
            report.Fail("scuffs do not use the tell marker");
        if (Count(locoSrc, "MakeWallJumpPuff(\"") != WallJumpPushTell.PuffCount)
            report.Fail("push tell does not build both scuffs");
        if (!bounce.Contains("_wallJumpEdge = true") || !bounce.Contains("_bouncePulse = 1f")
            || !bounce.Contains("_jumpFromClimb") || !bounce.Contains("_jumpFromWall"))
            report.Fail("the wall-jump pose hook was replaced");
        if (!land.Contains("JumpLandTell.Note") || !cling.Contains("WallClingTell") && !cling.Contains("MoveState.WallClimb"))
            report.Fail("land tell or cling marks were disconnected");

        string bodies = tick + solo + place + ensure + hide + puff + mat + scale + nudge;
        if (bodies.Contains("jumpSpeed") || bodies.Contains("slideBoost") || bodies.Contains("CharacterController")
            || bodies.Contains("Rigidbody") || bodies.Contains("AddForce") || bodies.Contains("FovPop")
            || bodies.Contains("fieldOfView") || bodies.Contains("timeScale") || bodies.Contains("Collider")
            || bodies.Contains("FixedUpdate") || bodies.Contains("InputAction"))
            report.Fail("push visual writes feel, a collider, or the camera");

        if (!spawnSrc.Contains("DummyAvatarBinder") || !binderSrc.Contains("DummyLocomotor"))
            report.Fail("the pawn is not given the locomotor that plays the push tell");
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

public sealed class WallJumpPushTellReport
{
    public float FlashSeconds;
    public float ShownSeconds;
    public float PushOffset;
    public float StretchY;
    public float StretchXZ;
    public float MaxAlpha;
    public int PushFrames;
    public int WallFrames;
    public int GroundFrames;
    public int CoyoteFrames;
    public int SlideFrames;
    public int ClingFrames;
    public int MantleFrames;
    public int CouchFrames;
    public int SameFrameFrames;
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
        text.Append(" wall jump push");
        text.Append(" kick_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" shown_s=").Append(ShownSeconds.ToString("0.00", c));
        text.Append(" offset=").Append(PushOffset.ToString("0.00", c));
        text.Append(" y=").Append(StretchY.ToString("0.00", c));
        text.Append(" xz=").Append(StretchXZ.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" on_wall=").Append(WallFrames.ToString(c));
        text.Append(" on_ground=").Append(GroundFrames.ToString(c));
        text.Append(" on_coyote=").Append(CoyoteFrames.ToString(c));
        text.Append(" on_slide=").Append(SlideFrames.ToString(c));
        text.Append(" on_cling=").Append(ClingFrames.ToString(c));
        text.Append(" on_mantle=").Append(MantleFrames.ToString(c));
        text.Append(" on_couch=").Append(CouchFrames.ToString(c));
        text.Append(" on_land_frame=").Append(SameFrameFrames.ToString(c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        text.Append(" clingGrace=").Append(ClingGrace.ToString("0.00", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
