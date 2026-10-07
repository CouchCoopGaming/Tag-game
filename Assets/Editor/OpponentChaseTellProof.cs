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
/// Headless check: the opponent chase read is two short heel ribbons plus a chest chevron,
/// and jump, slide, cling, punch reach, and lunge numbers stayed put.
/// </summary>
public static class OpponentChaseTellProof
{
    public static OpponentChaseTellReport Run()
    {
        var report = new OpponentChaseTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.RibbonCount = OpponentChaseTell.RibbonCount;
        report.RibbonTime = OpponentChaseTell.RibbonTime;
        report.RibbonWidth = OpponentChaseTell.RibbonWidth;
        report.MaxAlpha = OpponentChaseTell.MaxAlpha;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;
        report.ClingGrace = cfg.clingReleaseGrace;
        report.Reach = punch.reach;
        report.LungeSpeed = cfg.taggerLungeSpeed;
        report.LungeDuration = cfg.taggerLungeDuration;
        report.LungeCooldown = cfg.taggerLungeCooldown;

        if (OpponentChaseTell.RibbonCount != 2)
            report.Fail("chase tell is not two heel ribbons");
        if (OpponentChaseTell.RibbonTime < 0.12f || OpponentChaseTell.RibbonTime > 0.18f)
            report.Fail("ribbon lifetime is outside 0.12-0.18s");
        if (OpponentChaseTell.RibbonWidth < 0.14f || OpponentChaseTell.RibbonWidth > 0.22f)
            report.Fail("ribbon width is a hairline or wide enough to hide the legs");
        if (OpponentChaseTell.RibbonHeight < 0.04f || OpponentChaseTell.RibbonHeight > 0.08f)
            report.Fail("ribbon is not a low ground streak");
        if (OpponentChaseTell.HeelOffset <= 0.08f || OpponentChaseTell.HeelOffset >= cfg.radius)
            report.Fail("ribbons are not under the heels");
        if (OpponentChaseTell.TrailBack < 0.12f || OpponentChaseTell.TrailBack > 0.32f)
            report.Fail("streak is not a short heel mark");
        if (OpponentChaseTell.PairSpan >= cfg.radius * 2f)
            report.Fail("heel pair is wider than the capsule");
        // 2.2 m was one short sprint at 12 m/s. The 15% sprint retune (13.8) keeps the same 0.16 s streak.
        if (OpponentChaseTell.RibbonTime * cfg.sprintSpeed > 2.53f)
            report.Fail("ribbon is long enough to read as extra speed");
        if (OpponentChaseTell.MaxAlpha < 0.35f || OpponentChaseTell.MaxAlpha > 0.45f)
            report.Fail("alpha hides the streak or paints over the body");
        if (OpponentChaseTell.MarkR > 0.95f || OpponentChaseTell.MarkG > 0.95f || OpponentChaseTell.MarkB > 0.95f)
            report.Fail("chase mark is bright enough to wash the pawn");
        if (OpponentChaseTell.Glow != 0f)
            report.Fail("chase tell glows");
        if (OpponentChaseTell.VerticalImpulse != 0f)
            report.Fail("chase tell adds a vertical impulse");
        if (OpponentChaseTell.ChevronHeight < 0.95f || OpponentChaseTell.ChevronHeight > cfg.standingHeight - 0.45f)
            report.Fail("chevron is not on the chest");
        if (OpponentChaseTell.ChevronSize < 0.12f || OpponentChaseTell.ChevronSize > 0.28f)
            report.Fail("chevron is a speck or big enough to hide the chest");
        if (OpponentChaseTell.ChevronForward <= cfg.radius || OpponentChaseTell.ChevronForward > cfg.radius + 0.20f)
            report.Fail("chevron is inside the body or out in front of the face");
        if (OpponentChaseTell.MinDistance <= punch.reach)
            report.Fail("chase band overlaps the fist");
        if (OpponentChaseTell.MaxDistance < 12f || OpponentChaseTell.MaxDistance > 24f)
            report.Fail("chase band is a melee pocket or the whole campus");
        if (OpponentChaseTell.MinSpeed < 0.5f || OpponentChaseTell.MinSpeed > 3f)
            report.Fail("closing speed gate is a crawl or a sprint");
        if (OpponentChaseTell.CloseDot < 0.2f || OpponentChaseTell.CloseDot > 0.8f)
            report.Fail("closing angle is a circle or a needle");
        if (OpponentChaseTell.OpponentPawnName != SoloGrappleGate.OpponentPawnName)
            report.Fail("chase tell pawn is not DummyRunner");
        if (OpponentChaseTell.IsOpponentPawn(SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn gets the chase streak");
        if (OpponentChaseTell.IsOpponentPawn("Player_P1"))
            report.Fail("couch clone gets the chase streak");
        if (!OpponentChaseTell.IsOpponentPawn(SoloGrappleGate.OpponentPawnName))
            report.Fail("opponent pawn does not get the chase streak");

        Vector3 origin = new Vector3(2f, 0f, 4f);
        Vector3 target = new Vector3(2f, 0f, 12f);
        Vector3 toward = new Vector3(0f, 0f, 4f);
        Vector3 away = new Vector3(0f, 0f, -4f);
        if (!OpponentChaseTell.Closing(origin, target, toward))
            report.Fail("closing on a runner did not count");
        if (OpponentChaseTell.Closing(origin, target, away))
            report.Fail("running away counted as a chase");
        if (OpponentChaseTell.Closing(origin, target, Vector3.zero))
            report.Fail("standing still counted as a chase");
        if (OpponentChaseTell.Closing(origin, target, Vector3.up * 6f))
            report.Fail("a vertical velocity counted as a chase");
        if (OpponentChaseTell.Closing(origin, target, new Vector3(0f, 0f, 0.4f)))
            report.Fail("a crawl counted as a chase");

        float mid = (OpponentChaseTell.MinDistance + OpponentChaseTell.MaxDistance) * 0.5f;
        if (!Chase(true, true, true, true, mid, true, false, false, false, false, false))
            report.Fail("an It opponent closing in band did not show the tell");
        if (Chase(false, true, true, true, mid, true, false, false, false, false, false))
            report.Fail("the solo pawn showed the chase tell");
        if (Chase(true, false, true, true, mid, true, false, false, false, false, false))
            report.Fail("flee or not-It showed the chase tell");
        if (Chase(true, true, false, true, mid, true, false, false, false, false, false))
            report.Fail("an airborne chase showed the ground streak");
        if (Chase(true, true, true, false, mid, true, false, false, false, false, false))
            report.Fail("a lost target showed the chase tell");
        if (Chase(true, true, true, true, mid, false, false, false, false, false, false))
            report.Fail("a hunter who is not closing showed the chase tell");
        if (Chase(true, true, true, true, mid, true, true, false, false, false, false))
            report.Fail("LungeTell did not stop the chase streak");
        if (Chase(true, true, true, true, mid, true, false, true, false, false, false))
            report.Fail("lunge Active did not stop the chase streak");
        if (Chase(true, true, true, true, mid, true, false, false, true, false, false))
            report.Fail("punch Active did not stop the chase streak");
        if (Chase(true, true, true, true, mid, true, false, false, false, true, false))
            report.Fail("air dash doubled the chase streak");
        if (Chase(true, true, true, true, mid, true, false, false, false, false, true))
            report.Fail("slide scrape doubled the chase streak");
        if (Chase(true, true, true, true, OpponentChaseTell.MinDistance - 0.05f, true, false, false, false, false, false))
            report.Fail("inside the fist band still showed the chase streak");
        if (Chase(true, true, true, true, OpponentChaseTell.MaxDistance + 0.05f, true, false, false, false, false, false))
            report.Fail("past the chase band still showed the streak");
        if (!Chase(true, true, true, true, OpponentChaseTell.MinDistance, true, false, false, false, false, false))
            report.Fail("the near edge of the chase band was dark");
        if (!Chase(true, true, true, true, OpponentChaseTell.MaxDistance, true, false, false, false, false, false))
            report.Fail("the far edge of the chase band was dark");

        if (!OpponentChaseTell.PlaceHeels(origin, Vector3.forward, out Vector3 left, out Vector3 right))
            report.Fail("forward chase did not place heel ribbons");
        if (Mathf.Abs(left.y - (origin.y + OpponentChaseTell.RibbonHeight)) > 0.001f
            || Mathf.Abs(right.y - (origin.y + OpponentChaseTell.RibbonHeight)) > 0.001f)
            report.Fail("ribbons are not at ground height");
        if (left.y > 0.10f || right.y > 0.10f)
            report.Fail("ribbons cover the legs");
        if (Mathf.Abs(left.z - (origin.z - OpponentChaseTell.TrailBack)) > 0.001f
            || Mathf.Abs(right.z - (origin.z - OpponentChaseTell.TrailBack)) > 0.001f)
            report.Fail("ribbons are not behind the pawn");
        if (left.z >= origin.z || right.z >= origin.z)
            report.Fail("ribbons lead the chase");
        if (Mathf.Abs((left.x + right.x) * 0.5f - origin.x) > 0.001f)
            report.Fail("ribbons are not centered on the pawn");
        if (Mathf.Abs(Mathf.Abs(left.x - origin.x) - OpponentChaseTell.HeelOffset) > 0.001f
            || Mathf.Abs(Mathf.Abs(right.x - origin.x) - OpponentChaseTell.HeelOffset) > 0.001f)
            report.Fail("ribbons are not at the heels");
        if (Mathf.Abs((left - right).magnitude - OpponentChaseTell.HeelOffset * 2f) > 0.001f)
            report.Fail("the two ribbons do not match the heel split");

        if (!OpponentChaseTell.PlaceHeels(origin, Vector3.right, out Vector3 sideL, out Vector3 sideR))
            report.Fail("a right chase did not place heel ribbons");
        if (Mathf.Abs(sideL.x - (origin.x - OpponentChaseTell.TrailBack)) > 0.001f
            || Mathf.Abs(sideR.x - (origin.x - OpponentChaseTell.TrailBack)) > 0.001f)
            report.Fail("a right chase did not leave the streak behind the heels");
        if (sideL.x >= origin.x || sideR.x >= origin.x)
            report.Fail("a right chase put the streak ahead of the pawn");

        if (OpponentChaseTell.PlaceHeels(origin, Vector3.zero, out _, out _))
            report.Fail("a zero travel direction drew heels");
        if (OpponentChaseTell.PlaceHeels(origin, Vector3.up, out _, out _))
            report.Fail("a vertical direction drew heels");

        if (!OpponentChaseTell.PlaceChevron(origin, Vector3.forward, out Vector3 chest, out Vector3 aim))
            report.Fail("forward aim did not place the chevron");
        if (Mathf.Abs(chest.y - (origin.y + OpponentChaseTell.ChevronHeight)) > 0.001f)
            report.Fail("chevron is not at chest height");
        if (chest.y > cfg.standingHeight - 0.35f)
            report.Fail("chevron covers the head");
        if (Mathf.Abs(chest.z - (origin.z + OpponentChaseTell.ChevronForward)) > 0.001f)
            report.Fail("chevron does not sit toward the target");
        if (Mathf.Abs(chest.x - origin.x) > 0.001f)
            report.Fail("chevron left the center line");
        if (Mathf.Abs(aim.z - 1f) > 0.001f || Mathf.Abs(aim.y) > 0.001f)
            report.Fail("chevron aim is not the flat heading");

        if (!OpponentChaseTell.PlaceChevron(origin, Vector3.right, out Vector3 sideChest, out Vector3 sideAim))
            report.Fail("a right aim did not place the chevron");
        if (Mathf.Abs(sideChest.x - (origin.x + OpponentChaseTell.ChevronForward)) > 0.001f)
            report.Fail("a right aim did not point the chevron at the target");
        if (Mathf.Abs(sideAim.x - 1f) > 0.001f)
            report.Fail("a right aim did not keep the heading");

        if (OpponentChaseTell.PlaceChevron(origin, Vector3.zero, out _, out _))
            report.Fail("a zero aim drew a chevron");
        if (OpponentChaseTell.PlaceChevron(origin, Vector3.up, out _, out _))
            report.Fail("a vertical aim drew a chevron");

        CheckLocks(report, cfg, punch, "defaults");

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

        string tellSrc = ReadRepo("Assets/Scripts/Art/OpponentChaseTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string binderSrc = ReadRepo("Assets/Scripts/Art/DummyAvatarBinder.cs");
        string animSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Animation/MoveAnimDriver.cs");
        string lungeSrc = ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs");
        string patrolSrc = ReadRepo("Assets/Scripts/Modes/DummyPatrol.cs");
        if (tellSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || spawnSrc == null
            || binderSrc == null || animSrc == null || lungeSrc == null || patrolSrc == null)
            report.Fail("chase sources missing");
        else
            CheckWiring(report, tellSrc, locoSrc, motorSrc, cfgSrc, spawnSrc, binderSrc, animSrc, lungeSrc, patrolSrc);

        if (VerbPoseClips.SlideBody != "SlideBody" || VerbPoseClips.PunchStrike != "PunchStrike" || VerbPoseClips.TagCatch != "TagCatch")
            report.Fail("verb clip names drifted");
        if (ReadRepo("Assets/Scripts/Art/WallClingTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandFlash.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleRopeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/AirDashTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/OpponentLungeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/ItMarker.cs") == null)
            report.Fail("an existing tell was removed");

        return report;
    }

    static bool Chase(
        bool opponent, bool isIt, bool grounded, bool target, float dist, bool closing,
        bool lungeTell, bool lungeActive, bool punchActive, bool airDash, bool slide)
    {
        return OpponentChaseTell.Show(
            opponent, isIt, grounded, target, dist, closing,
            lungeTell, lungeActive, punchActive, airDash, slide);
    }

    static void CheckLocks(OpponentChaseTellReport report, MovementConfig cfg, PunchTagTuning punch, string label)
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
        if (cfg.enableJet)
            report.Fail(label + " jet is on");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f || Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f
            || Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail(label + " air dash numbers changed");
        if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f)
            report.Fail(label + " taggerLungeSpeed is not 16");
        if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f)
            report.Fail(label + " lunge duration is not 0.20");
        if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f)
            report.Fail(label + " lunge cooldown is not 1");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail(label + " punch reach is not 1.55");
    }

    static void CheckWiring(
        OpponentChaseTellReport report,
        string tellSrc, string locoSrc, string motorSrc, string cfgSrc,
        string spawnSrc, string binderSrc, string animSrc, string lungeSrc, string patrolSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("taggerLunge") || tellSrc.Contains("clingReleaseGrace")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera"))
            report.Fail("chase constants write feel or the camera");

        if (!Has(cfgSrc, "jumpSpeed = 24.7f", "slideBoost = 0f", "clingReleaseGrace = 0.08f",
            "taggerLungeSpeed = 16f", "taggerLungeDuration = 0.20f", "taggerLungeCooldown = 1.0f"))
            report.Fail("movement defaults drifted");
        if (!lungeSrc.Contains("LeadSeconds = 0.45f"))
            report.Fail("LungeTell lead is not 0.45s");
        if (!patrolSrc.Contains("OpponentLungeTell.LeadSeconds"))
            report.Fail("the opponent no longer uses the fixed lunge lead");
        if (patrolSrc.Contains("taggerLungeSpeed"))
            report.Fail("patrol writes the lunge speed");

        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("OpponentChase"))
            report.Fail("chase tell leaked into the motor");
        if (!motorSrc.Contains("cfg.taggerLungeSpeed") || !motorSrc.Contains("cfg.taggerLungeDuration")
            || !motorSrc.Contains("cfg.taggerLungeCooldown"))
            report.Fail("motor lunge numbers were disconnected");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickOpponentChase");
        string begin = MethodBody(locoSrc, "void BeginOpponentChase");
        string end = MethodBody(locoSrc, "void EndOpponentChase");
        string place = MethodBody(locoSrc, "void PlaceOpponentChase");
        string want = MethodBody(locoSrc, "bool ChaseTellWanted");
        string make = MethodBody(locoSrc, "TrailRenderer MakeChaseRibbon");
        string chev = MethodBody(locoSrc, "Transform MakeChaseChevron");
        if (late == null || tick == null || begin == null || end == null || place == null || want == null
            || make == null || chev == null)
        {
            report.Fail("chase tell is not on the opponent visual path");
            return;
        }

        int call = late.IndexOf("TickOpponentChase(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("chase tell does not run with the other presentation tells");
        if (!tick.Contains("ChaseTellWanted") || !tick.Contains("BeginOpponentChase") || !tick.Contains("EndOpponentChase"))
            report.Fail("chase tell does not follow pursuit enter and exit");
        if (!begin.Contains("PlaceOpponentChase") || !begin.Contains("ArmRibbon"))
            report.Fail("chase enter does not start the ribbons");
        if (!place.Contains("OpponentChaseTell.PlaceHeels") || !place.Contains("OpponentChaseTell.PlaceChevron"))
            report.Fail("heels and chevron are not placed by the chase builder");
        if (!end.Contains("emitting = false"))
            report.Fail("chase exit does not stop the ribbons");
        if (end.Contains("Clear("))
            report.Fail("chase exit wipes the short streak");
        if (!make.Contains("OpponentChaseTell.RibbonTime") || !make.Contains("OpponentChaseTell.RibbonWidth")
            || !make.Contains("OpponentChaseTell.MaxAlpha"))
            report.Fail("ribbons do not use the tell constants");
        if (make.Contains("AddComponent<Light>") || make.Contains("AddComponent<Collider>") || make.Contains("Rigidbody"))
            report.Fail("heel ribbons add a light or a collider");
        if (chev.Contains("CreatePrimitive") || chev.Contains("AddComponent<Collider>")
            || chev.Contains("BoxCollider") || chev.Contains("SphereCollider")
            || chev.Contains("CapsuleCollider") || chev.Contains("Rigidbody")
            || chev.Contains("AddComponent<Light>") || chev.Contains("Light"))
            report.Fail("chevron adds a collider or a light");
        if (!chev.Contains("MeshFilter") || !chev.Contains("MeshRenderer"))
            report.Fail("chevron is not a bare mesh");
        if (!chev.Contains("OpponentChaseTell.ChevronSize"))
            report.Fail("chevron does not use the tell size");
        if (Count(locoSrc, "MakeChaseRibbon(\"") != OpponentChaseTell.RibbonCount)
            report.Fail("chase tell does not build both heel ribbons");
        if (Count(locoSrc, "MakeChaseChevron(\"") != 1)
            report.Fail("chase tell does not build one chest chevron");

        if (!want.Contains("OpponentChaseTell.IsOpponentPawn") || !want.Contains("OpponentChaseTell.Show"))
            report.Fail("chase tell is not gated to the opponent pursuit");
        if (!want.Contains("IsLunging") || !want.Contains("OpponentLungeTell") || !want.Contains("PunchPhase.Active"))
            report.Fail("chase tell does not stop for the lunge ring, the lunge, or punch Active");
        if (!want.Contains("IsAirDashing") || !want.Contains("MoveState.Slide"))
            report.Fail("chase tell can double the air dash or the slide scrape");
        string runners = MethodBody(locoSrc, "bool TryNearestRunner");
        if (runners == null || !runners.Contains("if (p.IsIt)") || !runners.Contains("!p.IsAlive"))
            report.Fail("chase tell does not require a live non-It target");

        string chaseBodies = tick + begin + end + place + want + make + chev
            + (MethodBody(locoSrc, "void EnsureOpponentChase") ?? "")
            + (MethodBody(locoSrc, "bool TryNearestRunner") ?? "")
            + (MethodBody(locoSrc, "static Material MakeChaseMat") ?? "")
            + (MethodBody(locoSrc, "static Mesh ChaseChevronMesh") ?? "");
        if (chaseBodies.Contains("slideBoost") || chaseBodies.Contains("jumpSpeed") || chaseBodies.Contains("taggerLunge")
            || chaseBodies.Contains("clingReleaseGrace") || chaseBodies.Contains("CharacterController")
            || chaseBodies.Contains("Rigidbody") || chaseBodies.Contains("FovPop") || chaseBodies.Contains("AddKick")
            || chaseBodies.Contains("FixedUpdate") || chaseBodies.Contains("applyRootMotion")
            || chaseBodies.Contains("AddForce") || chaseBodies.Contains("Light"))
            report.Fail("chase visual writes feel or the camera");

        if (!spawnSrc.Contains("DummyAvatarBinder") || !binderSrc.Contains("DummyLocomotor"))
            report.Fail("both pawns are not given the locomotor that can play the chase tell");
        if (animSrc.Contains("OpponentChase"))
            report.Fail("chase tell was wired through the anim driver");
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

public sealed class OpponentChaseTellReport
{
    public int RibbonCount;
    public float RibbonTime;
    public float RibbonWidth;
    public float MaxAlpha;
    public float JumpSpeed;
    public float SlideBoost;
    public float ClingGrace;
    public float Reach;
    public float LungeSpeed;
    public float LungeDuration;
    public float LungeCooldown;
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
        text.Append(" chase ribbons=").Append(RibbonCount.ToString(c));
        text.Append(" life_s=").Append(RibbonTime.ToString("0.00", c));
        text.Append(" width_m=").Append(RibbonWidth.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        text.Append(" slideBoost=").Append(SlideBoost.ToString("0.###", c));
        text.Append(" clingGrace=").Append(ClingGrace.ToString("0.00", c));
        text.Append(" reach=").Append(Reach.ToString("0.00", c));
        text.Append(" lunge=").Append(LungeSpeed.ToString("0.###", c));
        text.Append("/").Append(LungeDuration.ToString("0.00", c));
        text.Append("/").Append(LungeCooldown.ToString("0.###", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
