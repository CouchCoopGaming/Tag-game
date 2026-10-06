using System;
using System.IO;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

public static class RoundFlowProof
{
    public static RoundFlow.Report Run()
    {
        RoundFlow.Report report = RoundFlow.RunSeeded(RoundFlow.ProofRounds, 3956);
        CheckLocks(report);
        CheckHud(report);
        CheckAudio(report);
        if (!report.Ok && report.Line.IndexOf("FAIL", StringComparison.Ordinal) < 0)
            report.Line += " FAIL " + report.Failure;
        else if (report.Ok)
            report.Line += " | " + VerbHudLayout.ProofLine() + " | audio-hooks=20";
        return report;
    }

    static void CheckLocks(RoundFlow.Report report)
    {
        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
        if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) report.Fail("coyote drifted");
        if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) report.Fail("jump buffer drifted");
        if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) report.Fail("cling grace drifted");
        if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) report.Fail("jumpSpeed drifted");
        if (cfg.slideBoost != 0f) report.Fail("slideBoost drifted");
        if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f || Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f
            || Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f)
            report.Fail("air dash drifted");
        if (Mathf.Abs(punch.reach - 1.55f) > 0.001f) report.Fail("punch reach drifted");
        if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f || Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f
            || Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f)
            report.Fail("lunge drifted");
        string tell = Read("Assets/Scripts/Art/OpponentLungeTell.cs");
        if (tell == null || tell.IndexOf("LeadSeconds = 0.45f", StringComparison.Ordinal) < 0)
            report.Fail("lunge tell drifted");
        if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f || Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f
            || Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f)
            report.Fail("wall speeds drifted");
        if (Mathf.Abs(PunchStagger.Duration - 0.25f) > 0.001f || Mathf.Abs(PunchStagger.Immunity - 0.50f) > 0.001f)
            report.Fail("stagger drifted");
        if (Mathf.Abs(LaunchPadRules.DefaultCooldown - 0.3f) > 0.001f) report.Fail("pad cooldown drifted");
        if (Mathf.Abs(ZipLineRules.DefaultRideSpeed - 14f) > 0.001f) report.Fail("zip speed drifted");
        if (Mathf.Abs(ZipLineRules.DefaultRegrabCooldown - 0.3f) > 0.001f) report.Fail("zip regrab drifted");
        if (Mathf.Abs(TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f) report.Fail("tag-back drifted");
        if (Mathf.Abs(RoundFlow.TagBackSeconds - 1.0f) > 0.001f) report.Fail("round tag-back drifted");
    }

    static void CheckHud(RoundFlow.Report report)
    {
        if (!VerbHudLayout.Separated(1920f, 1080f) || !VerbHudLayout.Separated(1280f, 720f))
            report.Fail("verb hud overlaps minimap or mute");
        string hud = Read("Assets/Scripts/Modes/VerbStatusHud.cs");
        if (hud == null
            || hud.IndexOf("DASH", StringComparison.Ordinal) < 0
            || hud.IndexOf("SAFE", StringComparison.Ordinal) < 0
            || hud.IndexOf("STAGGER", StringComparison.Ordinal) < 0
            || hud.IndexOf("CLING", StringComparison.Ordinal) < 0
            || hud.IndexOf("READY", StringComparison.Ordinal) < 0)
            report.Fail("verb hud is missing a labeled ring");
        if (RoundFlow.Clock(90f) != "1:30" || RoundFlow.Clock(0f) != "0:00")
            report.Fail("round clock format drifted");
        if (RoundFlow.BannerLine(true, false, "", "Dummy") != "You're It")
            report.Fail("You're It banner drifted");
        if (RoundFlow.BannerLine(false, true, "Dummy", "Dummy") != "Tagged Dummy")
            report.Fail("Tagged banner drifted");
    }

    static void CheckAudio(RoundFlow.Report report)
    {
        string[] names =
        {
            "jump", "land soft", "land hard", "slide start", "slide loop", "slide end",
            "cling grab", "wall jump", "air dash", "punch whiff", "punch hit", "tag",
            "tag-back blocked", "stagger", "pad launch", "zip grab", "zip loop", "zip drop",
            "countdown beep", "round end"
        };
        if (names.Length != 20)
            report.Fail("audio hook list is not 20");
        string bus = Read("Assets/Scripts/Audio/AudioBus.cs");
        string doc = Read("Docs/AudioHooks.md");
        if (bus == null || doc == null)
        {
            report.Fail("audio hook doc is missing");
            return;
        }
        if (bus.IndexOf("AudioMaster.Muted", StringComparison.Ordinal) < 0)
            report.Fail("audio bus ignores mute");
        for (int i = 0; i < names.Length; i++)
        {
            string quoted = "\"" + names[i] + "\"";
            if (bus.IndexOf(quoted, StringComparison.Ordinal) < 0)
                report.Fail("bus is missing " + names[i]);
            if (doc.IndexOf(names[i], StringComparison.Ordinal) < 0)
                report.Fail("doc is missing " + names[i]);
        }
        if (bus.IndexOf("TagSfx.BecomeIt", StringComparison.Ordinal) < 0
            || bus.IndexOf("TagSfx.PunchConnect", StringComparison.Ordinal) < 0
            || bus.IndexOf("TagSfx.TagBackThunk", StringComparison.Ordinal) < 0)
            report.Fail("audio bus dropped a tag placeholder");
    }

    static string Read(string path)
    {
        if (!File.Exists(path)) return null;
        return File.ReadAllText(path);
    }
}
