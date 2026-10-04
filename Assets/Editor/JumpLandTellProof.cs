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
/// Headless check: the solo pawn thuds and puffs heel dust after a real
/// airborne landing, then stops. Coyote re-grounds, slide enter, and wall
/// cling stay quiet. Opponent and couch stay quiet. Jump numbers stay put.
/// </summary>
public static class JumpLandTellProof
{
    const float Dt = 1f / 60f;

    public static JumpLandTellReport Run()
    {
        var report = new JumpLandTellReport();
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = PunchTagTuning.CreateRuntimeDefaults();

        report.FlashSeconds = JumpLandTell.FlashSeconds;
        report.MinAirSeconds = JumpLandTell.MinAirSeconds;
        report.ThudY = JumpLandTell.ThudY;
        report.ThudXZ = JumpLandTell.ThudXZ;
        report.MaxAlpha = JumpLandTell.MaxAlpha;
        report.JumpSpeed = cfg.jumpSpeed;
        report.SlideBoost = cfg.slideBoost;

        if (JumpLandTell.FlashSeconds < 0.15f || JumpLandTell.FlashSeconds > 0.35f)
            report.Fail("land tell is outside 0.15-0.35s");
        if (JumpLandTell.FlashSeconds < 0.20f || JumpLandTell.FlashSeconds > 0.30f)
            report.Fail("land tell is not a short thud");
        if (JumpLandTell.MinAirSeconds <= cfg.coyoteTime)
            report.Fail("land tell arms inside the coyote window");
        if (JumpLandTell.MinAirSeconds < 0.18f || JumpLandTell.MinAirSeconds > 0.30f)
            report.Fail("air gate is a flicker or long enough to miss a jump");
        if (JumpLandTell.ThudY >= 0.90f || JumpLandTell.ThudY < 0.68f)
            report.Fail("thud squash is invisible or a pancake");
        if (JumpLandTell.ThudXZ <= 1.06f || JumpLandTell.ThudXZ > 1.24f)
            report.Fail("thud width is invisible or a blob");
        JumpLandTell.ThudScale(1f, out float peakY, out float peakXZ);
        JumpLandTell.ThudScale(0f, out float restY, out float restXZ);
        if (Mathf.Abs(peakY - JumpLandTell.ThudY) > 0.001f || Mathf.Abs(peakXZ - JumpLandTell.ThudXZ) > 0.001f)
            report.Fail("impact frame is not the thud");
        if (Mathf.Abs(restY - 1f) > 0.001f || Mathf.Abs(restXZ - 1f) > 0.001f)
            report.Fail("a quiet frame still scales the body");
        JumpLandTell.ThudScale(0.5f, out float midY, out float midXZ);
        if (midY <= peakY || midY >= 1f || midXZ >= peakXZ || midXZ <= 1f)
            report.Fail("thud does not ease back to the pose");
        if (JumpLandTell.MaxAlpha < 0.40f || JumpLandTell.MaxAlpha > 0.70f)
            report.Fail("heel dust is too faint or hides the feet");
        if (Mathf.Abs(JumpLandTell.Alpha(0f) - JumpLandTell.MaxAlpha) > 0.001f)
            report.Fail("dust does not open at peak alpha");
        if (JumpLandTell.Alpha(JumpLandTell.FlashSeconds) != 0f || JumpLandTell.Alpha(-1f) != 0f)
            report.Fail("dust alpha stays up after the window");
        if (JumpLandTell.PuffCount != 2)
            report.Fail("land tell is not two heel puffs");
        if (Mathf.Abs(JumpLandTell.PuffDiameter(1f) - JumpLandTell.PuffTight) > 0.001f)
            report.Fail("puff does not start tight");
        if (Mathf.Abs(JumpLandTell.PuffDiameter(0f) - JumpLandTell.PuffWide) > 0.001f)
            report.Fail("puff does not bloom");
        if (JumpLandTell.PuffTight < 0.10f || JumpLandTell.PuffWide > 0.42f || JumpLandTell.PuffWide <= JumpLandTell.PuffTight)
            report.Fail("puffs are a speck or large enough to cover the pawn");
        if (JumpLandTell.PuffHeight < 0.03f || JumpLandTell.PuffHeight > 0.14f)
            report.Fail("puffs float or sink into the floor");
        if (JumpLandTell.FootOffset <= 0.06f || JumpLandTell.FootOffset >= cfg.radius)
            report.Fail("puffs leave the feet or sit on the same point");
        if (JumpLandTell.HeelBack < 0.08f || JumpLandTell.HeelBack > 0.35f)
            report.Fail("puffs are not at the heels");
        if (JumpLandTell.PairSpan > cfg.radius * 2f + 0.20f)
            report.Fail("puffs spread wider than a footfall");
        if (JumpLandTell.MarkR > 0.97f && JumpLandTell.MarkG > 0.97f && JumpLandTell.MarkB > 0.97f)
            report.Fail("dust is white enough to wash the body");
        if (JumpLandTell.MarkR < 0.70f || JumpLandTell.MarkG < 0.55f || JumpLandTell.MarkB > JumpLandTell.MarkG)
            report.Fail("dust does not read as heel dirt");
        if (JumpLandTell.Glow != 0f)
            report.Fail("land tell glows");
        if (JumpLandTell.VerticalImpulse != 0f)
            report.Fail("land tell adds a hop");

        Vector3 origin = new Vector3(2f, 0.4f, -6f);
        if (!JumpLandTell.Heels(origin, Vector3.forward, out Vector3 left, out Vector3 right))
            report.Fail("forward travel did not place heels");
        if (Mathf.Abs(left.y - (origin.y + JumpLandTell.PuffHeight)) > 0.001f
            || Mathf.Abs(right.y - (origin.y + JumpLandTell.PuffHeight)) > 0.001f)
            report.Fail("puffs left the ground");
        if (Mathf.Abs(left.z - (origin.z - JumpLandTell.HeelBack)) > 0.001f
            || Mathf.Abs(right.z - (origin.z - JumpLandTell.HeelBack)) > 0.001f)
            report.Fail("puffs are not behind travel");
        if (Mathf.Abs(Mathf.Abs(left.x - origin.x) - JumpLandTell.FootOffset) > 0.001f
            || Mathf.Abs(Mathf.Abs(right.x - origin.x) - JumpLandTell.FootOffset) > 0.001f)
            report.Fail("puffs are not split across the heels");
        if (JumpLandTell.Heels(origin, Vector3.zero, out _, out _) || JumpLandTell.Heels(origin, Vector3.up, out _, out _))
            report.Fail("a zero travel still placed puffs");

        if (Arm(report, cfg.coyoteTime, false, false, true))
            report.Fail("a coyote re-ground played the land tell");
        if (Arm(report, Dt, false, false, true))
            report.Fail("a one-frame ground flicker played the land tell");
        if (Arm(report, JumpLandTell.MinAirSeconds - 0.001f, false, false, true))
            report.Fail("air under the gate played the land tell");
        if (!Arm(report, JumpLandTell.MinAirSeconds, false, false, true))
            report.Fail("a real landing did not arm the tell");
        if (Arm(report, 1.5f, true, false, true))
            report.Fail("a slide landing played the land tell");
        if (Arm(report, 1.5f, false, true, true))
            report.Fail("a wall cling or mantle played the land tell");
        if (Arm(report, 1.5f, false, false, false))
            report.Fail("a pawn that is not solo played the land tell");

        report.ShownSeconds = PlayLand(report, origin, Vector3.forward);
        if (report.ShownSeconds < 0.15f || report.ShownSeconds > 0.35f)
            report.Fail("shown land tell left 0.15-0.35s");
        int slideShown = CountBlocked(report, true, false);
        int clingShown = CountBlocked(report, false, true);
        report.SlideFrames = slideShown;
        report.ClingFrames = clingShown;
        if (slideShown != 0)
            report.Fail("slide enter kept the land tell up");
        if (clingShown != 0)
            report.Fail("wall cling kept the land tell up");

        if (!JumpLandTell.ForPawn(false, false, 0, SoloGrappleGate.SoloPawnName))
            report.Fail("solo pawn land tell is off");
        if (JumpLandTell.ForPawn(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || JumpLandTell.ForPawn(false, false, 0, SoloGrappleGate.OpponentPawnName)
            || JumpLandTell.ForPawn(true, false, 0, SoloGrappleGate.SoloPawnName)
            || JumpLandTell.ForPawn(true, false, 1, "Player_P1")
            || JumpLandTell.ForPawn(false, false, 2, SoloGrappleGate.SoloPawnName))
            report.Fail("land tell leaked off the solo pawn");

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

        string tellSrc = ReadRepo("Assets/Scripts/Art/JumpLandTell.cs");
        string locoSrc = ReadRepo("Assets/Scripts/Art/DummyLocomotor.cs");
        string motorSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string cfgSrc = ReadRepo("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
        string spawnSrc = ReadRepo("Assets/Scripts/Local/LocalPlayerSpawner.cs");
        string binderSrc = ReadRepo("Assets/Scripts/Art/DummyAvatarBinder.cs");
        if (tellSrc == null || locoSrc == null || motorSrc == null || cfgSrc == null || spawnSrc == null || binderSrc == null)
            report.Fail("land tell sources missing");
        else
            CheckWiring(report, tellSrc, locoSrc, motorSrc, cfgSrc, spawnSrc, binderSrc);

        if (ReadRepo("Assets/Scripts/Art/SlideScrapeTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleMissTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/GrappleLatchTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/HitConfirmTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/AirDashCooldownTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandTell.cs") == null
            || ReadRepo("Assets/Scripts/Art/TagLandFlash.cs") == null
            || ReadRepo("Assets/Scripts/Art/WallClingTell.cs") == null)
            report.Fail("an existing tell was removed");

        if (punch.reach != 1.55f)
            report.Fail("punch reach changed");

        return report;
    }

    /// <summary>Preset air, then one landing edge. True when the cue is showing.</summary>
    static bool Arm(JumpLandTellReport report, float airTime, bool slide, bool cling, bool solo)
    {
        float age = -1f;
        float air = airTime;
        JumpLandTell.Note(ref age, ref air, solo, true, false, slide, cling, Dt);
        bool show = JumpLandTell.Show(solo, age);
        if (show && (slide || cling || !solo || airTime < JumpLandTell.MinAirSeconds))
            report.Fail("a blocked landing armed the cue");
        if (!slide && !cling && solo && airTime >= JumpLandTell.MinAirSeconds && !show)
            report.Fail("a qualified landing stayed quiet");
        if (air != 0f && solo)
            report.Fail("a landing left air time running");
        return show;
    }

    static float PlayLand(JumpLandTellReport report, Vector3 origin, Vector3 travel)
    {
        float age = -1f;
        float air = JumpLandTell.MinAirSeconds;
        JumpLandTell.Note(ref age, ref air, true, true, false, false, false, Dt);
        if (!JumpLandTell.Show(true, age) || age != 0f)
        {
            report.Fail("a real landing did not open the cue");
            return 0f;
        }

        if (!JumpLandTell.Heels(origin, travel, out Vector3 left, out Vector3 right))
            report.Fail("the landing did not place heels");
        if ((left - right).magnitude <= 0.05f)
            report.Fail("heel puffs stacked");

        float shown = 0f;
        int frames = 0;
        int guard = 0;
        bool wasGrounded = false;
        while (JumpLandTell.Show(true, age))
        {
            JumpLandTell.ThudScale(JumpLandTell.Fade(age), out float y, out float xz);
            if (y > 1f || xz < 1f)
                report.Fail("the thud inverted");
            if (!JumpLandTell.Heels(origin, travel, out _, out _))
                report.Fail("the landing lost its heels");
            shown += Dt;
            frames++;
            bool still = JumpLandTell.Step(ref age, Dt, true);
            if (!still && JumpLandTell.Show(true, age))
                report.Fail("step ended while the land tell was still up");
            wasGrounded = true;
            float held = age;
            JumpLandTell.Note(ref age, ref air, true, true, wasGrounded, false, false, Dt);
            if (JumpLandTell.Show(true, held) && age != held)
                report.Fail("standing on the landing restarted the thud");
            if (++guard > 40)
            {
                report.Fail("land tell did not stop");
                break;
            }
        }

        report.LandFrames = frames;
        if (JumpLandTell.Show(true, age) || age >= 0f)
            report.Fail("land tell stayed armed after it stopped");
        JumpLandTell.ThudScale(JumpLandTell.Fade(age), out float endY, out float endXZ);
        if (Mathf.Abs(endY - 1f) > 0.001f || Mathf.Abs(endXZ - 1f) > 0.001f)
            report.Fail("the body stayed squashed after the window");
        if (Mathf.Abs(shown - JumpLandTell.FlashSeconds) > Dt + 0.001f)
            report.Fail("shown time drifted off the window");

        float marked = age;
        for (int i = 0; i < 12; i++)
        {
            JumpLandTell.Note(ref age, ref air, true, true, true, false, false, Dt);
            if (age != marked || JumpLandTell.Show(true, age))
                report.Fail("idle ground replayed a finished land");
        }

        air = JumpLandTell.MinAirSeconds;
        JumpLandTell.Note(ref age, ref air, true, true, false, false, false, Dt);
        if (!JumpLandTell.Show(true, age) || age != 0f)
            report.Fail("a second jump land did not arm");
        return shown;
    }

    static int CountBlocked(JumpLandTellReport report, bool slide, bool cling)
    {
        float age = -1f;
        float air = 0f;
        bool was = true;
        int shown = 0;
        for (int i = 0; i < 40; i++)
        {
            bool grounded = i > 20;
            JumpLandTell.Note(ref age, ref air, true, grounded, was, slide, cling, Dt);
            was = grounded;
            if (JumpLandTell.Show(true, age))
                shown++;
            if (JumpLandTell.Step(ref age, Dt, true))
                report.Fail("a blocked landing stepped the cue");
            if (age >= 0f)
                report.Fail("a blocked landing left the cue armed");
        }

        return shown;
    }

    static void CheckLocks(JumpLandTellReport report, MovementConfig cfg, PunchTagTuning punch)
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
        if (Mathf.Abs(cfg.airCrouchFallMult - 2f) > 0.001f || Mathf.Abs(cfg.maxFallSpeed - 52f) > 0.001f)
            report.Fail("fall numbers changed");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f)
            report.Fail("punch reach is not 1.55");
    }

    static void CheckWiring(
        JumpLandTellReport report,
        string tellSrc, string locoSrc, string motorSrc, string cfgSrc, string spawnSrc, string binderSrc)
    {
        if (tellSrc.Contains("Rigidbody") || tellSrc.Contains("CharacterController") || tellSrc.Contains("Collider")
            || tellSrc.Contains("Light") || tellSrc.Contains("jumpSpeed") || tellSrc.Contains("slideBoost")
            || tellSrc.Contains("FixedUpdate") || tellSrc.Contains("FovPop") || tellSrc.Contains("AddForce")
            || tellSrc.Contains("applyRootMotion") || tellSrc.Contains("Camera") || tellSrc.Contains("InputAction")
            || tellSrc.Contains("timeScale") || tellSrc.Contains("fieldOfView"))
            report.Fail("land tell constants write feel, input, or the camera");

        if (!Has(cfgSrc, "jumpSpeed = 24.7f", "slideBoost = 0f", "clingReleaseGrace = 0.08f",
            "coyoteTime = 0.10f", "jumpBuffer = 0.16f", "enableJet = false"))
            report.Fail("movement defaults drifted");

        if (Count(motorSrc, "_cc.Move(") != 1)
            report.Fail("motor no longer steps once");
        if (motorSrc.Contains("JumpLandTell") || motorSrc.Contains("FixedUpdate"))
            report.Fail("land tell leaked into the motor");

        string late = MethodBody(locoSrc, "void LateUpdate");
        string tick = MethodBody(locoSrc, "void TickJumpLand");
        string solo = MethodBody(locoSrc, "bool JumpLandSolo");
        string place = MethodBody(locoSrc, "void PlaceJumpLand");
        string ensure = MethodBody(locoSrc, "void EnsureJumpLand");
        string hide = MethodBody(locoSrc, "void HideJumpLand");
        string puff = MethodBody(locoSrc, "Transform MakeJumpLandPuff");
        string mat = MethodBody(locoSrc, "static Material MakeJumpLandMat");
        string scale = MethodBody(locoSrc, "Vector3 JumpLandScale");
        if (late == null || tick == null || solo == null || place == null || ensure == null
            || hide == null || puff == null || mat == null || scale == null)
        {
            report.Fail("land tell is not on the solo visual path");
            return;
        }

        int call = late.IndexOf("TickJumpLand(", StringComparison.Ordinal);
        int bail = late.IndexOf("if (!_bound) return", StringComparison.Ordinal);
        if (call < 0 || bail < 0 || call > bail)
            report.Fail("land tell does not run with the other presentation tells");
        if (!late.Contains("JumpLandScale("))
            report.Fail("the thud is not applied to the pose scale");
        if (!tick.Contains("MoveState.Slide") || !tick.Contains("MoveState.WallClimb")
            || !tick.Contains("MoveState.WallRun") || !tick.Contains("MoveState.Mantle"))
            report.Fail("slide, cling, or mantle can still arm the land tell");
        if (!tick.Contains("_wasGrounded") || !tick.Contains("JumpLandTell.Note")
            || !tick.Contains("JumpLandTell.Show") || !tick.Contains("JumpLandTell.Step")
            || !tick.Contains("JumpLandTell.Alpha") || !tick.Contains("JumpLandTell.Fade"))
            report.Fail("land tell does not follow a landing edge");
        int gate = tick.IndexOf("if (!JumpLandTell.Show", StringComparison.Ordinal);
        int built = tick.IndexOf("EnsureJumpLand(", StringComparison.Ordinal);
        if (gate < 0 || built < 0 || gate > built)
            report.Fail("a quiet frame still builds the heel puffs");
        if (!solo.Contains("JumpLandTell.ForPawn") || !solo.Contains("LocalPlayerRoster.IsCouch")
            || !solo.Contains("SoloGrappleGate.OpponentPawnName"))
            report.Fail("land tell is not gated to the solo pawn");
        if (!place.Contains("JumpLandTell.Heels") || !place.Contains("JumpLandTell.PuffDiameter"))
            report.Fail("puffs are not placed by the tell");
        if (!scale.Contains("JumpLandTell.ThudScale") || !scale.Contains("JumpLandTell.Fade"))
            report.Fail("the thud does not use the tell scale");
        if (!mat.Contains("JumpLandTell.MaxAlpha") || !puff.Contains("JumpLandTell.MarkerName") && !ensure.Contains("JumpLandTell.MarkerName"))
            report.Fail("puffs do not use the tell marker");
        if (Count(locoSrc, "MakeJumpLandPuff(\"") != JumpLandTell.PuffCount)
            report.Fail("land tell does not build both puffs");

        string bodies = tick + solo + place + ensure + hide + puff + mat + scale;
        if (bodies.Contains("jumpSpeed") || bodies.Contains("slideBoost") || bodies.Contains("CharacterController")
            || bodies.Contains("Rigidbody") || bodies.Contains("AddForce") || bodies.Contains("FovPop")
            || bodies.Contains("fieldOfView") || bodies.Contains("timeScale") || bodies.Contains("Collider")
            || bodies.Contains("FixedUpdate") || bodies.Contains("InputAction"))
            report.Fail("land visual writes feel, a collider, or the camera");

        if (!spawnSrc.Contains("DummyAvatarBinder") || !binderSrc.Contains("DummyLocomotor"))
            report.Fail("the pawn is not given the locomotor that plays the land tell");
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

public sealed class JumpLandTellReport
{
    public float FlashSeconds;
    public float MinAirSeconds;
    public float ShownSeconds;
    public float ThudY;
    public float ThudXZ;
    public float MaxAlpha;
    public int LandFrames;
    public int SlideFrames;
    public int ClingFrames;
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
        text.Append(" jump land");
        text.Append(" thud_s=").Append(FlashSeconds.ToString("0.00", c));
        text.Append(" air_s=").Append(MinAirSeconds.ToString("0.00", c));
        text.Append(" shown_s=").Append(ShownSeconds.ToString("0.00", c));
        text.Append(" y=").Append(ThudY.ToString("0.00", c));
        text.Append(" xz=").Append(ThudXZ.ToString("0.00", c));
        text.Append(" alpha=").Append(MaxAlpha.ToString("0.00", c));
        text.Append(" on_land=").Append(LandFrames.ToString(c));
        text.Append(" on_slide=").Append(SlideFrames.ToString(c));
        text.Append(" on_cling=").Append(ClingFrames.ToString(c));
        text.Append(" jumpSpeed=").Append(JumpSpeed.ToString("0.0", c));
        if (!Ok)
        {
            text.Append('\n');
            text.Append(FailureText);
        }
        return text.ToString();
    }
}
