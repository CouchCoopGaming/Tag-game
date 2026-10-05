using System;
using System.IO;
using Tag.Local;
using TagArena.Movement;
using UnityEngine;

static class Program
{
    static int Main()
    {
        StrafeJumpReport report = StrafeJumpProof.Run60();
        Console.WriteLine(report.ToString());
        if (!report.Ok)
        {
            Console.Error.WriteLine(report.FailureText);
            return 1;
        }

        GrappleReport grapple = GrappleProof.Run60();
        Console.WriteLine(grapple.ToString());
        if (!grapple.Ok)
        {
            Console.Error.WriteLine(grapple.FailureText);
            return 1;
        }

        VerbPoseClipReport clips = VerbPoseClipProof.Run();
        Console.WriteLine(clips.ToString());
        Console.WriteLine(Tag.Art.VerbPoseClips.SlideProofLine());
        Console.WriteLine(Tag.Art.GaitBlend.ProofLine());
        if (!Tag.Art.GaitBlend.Holds())
        {
            Console.Error.WriteLine("gait blend is not continuous");
            return 1;
        }

        Console.WriteLine(TagArena.Movement.MoveGrounding.ProofLine());
        if (!TagArena.Movement.MoveGrounding.Holds())
        {
            Console.Error.WriteLine("grounding fixes are not held");
            return 1;
        }

        Console.WriteLine(TagArena.Movement.ChaseCam.ProofLine());
        if (!TagArena.Movement.ChaseCam.Holds())
        {
            Console.Error.WriteLine("chase cam catch-up is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.JumpPose.ProofLine());
        if (!Tag.Art.JumpPose.Holds())
        {
            Console.Error.WriteLine("jump pose beats are not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.WallPose.ProofLine());
        if (!Tag.Art.WallPose.Holds())
        {
            Console.Error.WriteLine("wall pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.PunchTagPose.ProofLine());
        if (!Tag.Art.PunchTagPose.Holds())
        {
            Console.Error.WriteLine("punch tag pose beats are not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.GrapplePose.ProofLine());
        if (!Tag.Art.GrapplePose.Holds())
        {
            Console.Error.WriteLine("grapple pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.PoseHandoff.ProofLine());
        if (!Tag.Art.PoseHandoff.Holds())
        {
            Console.Error.WriteLine("pose handoff weights do not sum");
            return 1;
        }

        Console.WriteLine(Tag.Art.AirDashPose.ProofLine());
        if (!Tag.Art.AirDashPose.Holds())
        {
            Console.Error.WriteLine("air dash pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.CrouchPose.ProofLine());
        if (!Tag.Art.CrouchPose.Holds())
        {
            Console.Error.WriteLine("crouch pose is not held");
            return 1;
        }
        if (!clips.Ok)
        {
            Console.Error.WriteLine(string.Join(" | ", clips.Failures));
            return 1;
        }

        GrappleRopeTellReport ropeRead = GrappleRopeTellProof.Run();
        Console.WriteLine(ropeRead.ToString());
        if (!ropeRead.Ok)
        {
            Console.Error.WriteLine(ropeRead.FailureText);
            return 1;
        }

        TagLandTellReport tagLand = TagLandTellProof.Run();
        Console.WriteLine(tagLand.ToString());
        if (!tagLand.Ok)
        {
            Console.Error.WriteLine(tagLand.FailureText);
            return 1;
        }

        SlideScrapeTellReport scrape = SlideScrapeTellProof.Run();
        Console.WriteLine(scrape.ToString());
        if (!scrape.Ok)
        {
            Console.Error.WriteLine(scrape.FailureText);
            return 1;
        }

        WallClingTellReport cling = WallClingTellProof.Run();
        Console.WriteLine(cling.ToString());
        if (!cling.Ok)
        {
            Console.Error.WriteLine(cling.FailureText);
            return 1;
        }

        OpponentChaseTellReport chase = OpponentChaseTellProof.Run();
        Console.WriteLine(chase.ToString());
        if (!chase.Ok)
        {
            Console.Error.WriteLine(chase.FailureText);
            return 1;
        }

        OpponentChaseSteerReport chaseSteer = OpponentChaseSteerProof.Run();
        Console.WriteLine(chaseSteer.ToString());
        if (!chaseSteer.Ok)
        {
            Console.Error.WriteLine(chaseSteer.FailureText);
            return 1;
        }

        GrappleLatchTellReport latch = GrappleLatchTellProof.Run();
        Console.WriteLine(latch.ToString());
        if (!latch.Ok)
        {
            Console.Error.WriteLine(latch.FailureText);
            return 1;
        }

        AirDashCooldownTellReport dashCd = AirDashCooldownTellProof.Run();
        Console.WriteLine(dashCd.ToString());
        if (!dashCd.Ok)
        {
            Console.Error.WriteLine(dashCd.FailureText);
            return 1;
        }

        HitConfirmTellReport hitConfirm = HitConfirmTellProof.Run();
        Console.WriteLine(hitConfirm.ToString());
        if (!hitConfirm.Ok)
        {
            Console.Error.WriteLine(hitConfirm.FailureText);
            return 1;
        }

        GrappleMissTellReport miss = GrappleMissTellProof.Run();
        Console.WriteLine(miss.ToString());
        if (!miss.Ok)
        {
            Console.Error.WriteLine(miss.FailureText);
            return 1;
        }

        JumpLandTellReport jumpLand = JumpLandTellProof.Run();
        Console.WriteLine(jumpLand.ToString());
        if (!jumpLand.Ok)
        {
            Console.Error.WriteLine(jumpLand.FailureText);
            return 1;
        }

        WallJumpPushTellReport wallPush = WallJumpPushTellProof.Run();
        Console.WriteLine(wallPush.ToString());
        if (!wallPush.Ok)
        {
            Console.Error.WriteLine(wallPush.FailureText);
            return 1;
        }

        const string grapplePath = "Assets/Scripts/Experimental/ExperimentalGrapple.cs";
        if (!AssetHas(grapplePath, "GrappleRopeTell.AttachedSpan", "GrappleRopeTell.AimSpan", "enableGrapple = false"))
        {
            Console.Error.WriteLine("grapple rope tell is not what the component draws");
            return 1;
        }

        string grappleSrc = File.ReadAllText(grapplePath);
        if (grappleSrc.IndexOf("jumpSpeed", StringComparison.Ordinal) >= 0
            || grappleSrc.IndexOf(".Move(", StringComparison.Ordinal) >= 0
            || grappleSrc.IndexOf("_velocity", StringComparison.Ordinal) >= 0
            || grappleSrc.IndexOf("AddForce", StringComparison.Ordinal) >= 0)
        {
            Console.Error.WriteLine("grapple visual pass wrote motor code");
            return 1;
        }

        if (!SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.SoloPawnName))
        {
            Console.Error.WriteLine("solo pawn grapple is off");
            return 1;
        }

        if (SoloGrappleGate.EnableFor(false, true, 1, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(false, false, 0, SoloGrappleGate.OpponentPawnName)
            || SoloGrappleGate.EnableFor(true, false, 0, SoloGrappleGate.SoloPawnName)
            || SoloGrappleGate.EnableFor(true, false, 1, "Player_P1")
            || SoloGrappleGate.EnableFor(false, false, 2, SoloGrappleGate.SoloPawnName))
        {
            Console.Error.WriteLine("grapple leaked off the solo pawn");
            return 1;
        }

        MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
        if (cfg.enableJet)
        {
            Console.Error.WriteLine("jet default is on");
            return 1;
        }

        if (!Locked(cfg.coyoteTime, 0.10f) || !Locked(cfg.jumpBuffer, 0.16f) || !Locked(cfg.clingReleaseGrace, 0.08f)
            || !Locked(cfg.jumpSpeed, 24.7f) || cfg.slideBoost != 0f
            || !Locked(cfg.airDashDuration, 0.10f) || !Locked(cfg.airDashSpeed, 15f) || !Locked(cfg.airDashCooldown, 30f)
            || !Locked(cfg.airCrouchFallMult, 2f) || !Locked(cfg.maxFallSpeed, 52f))
        {
            Console.Error.WriteLine("locked feel numbers drifted");
            return 1;
        }

        if (!AssetHas("Assets/Resources/TagArena/MovementConfig.asset",
                "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
                "jumpSpeed: 24.7", "slideBoost: 0", "enableJet: 0",
                "airDashDuration: 0.1", "airDashSpeed: 15", "airDashCooldown: 30",
                "airCrouchFallMult: 2", "maxFallSpeed: 52")
            || !AssetHas("Assets/ScriptableObjects/PunchTagTuning.asset", "reach: 1.55"))
        {
            Console.Error.WriteLine("locked asset numbers drifted");
            return 1;
        }

        Console.WriteLine("solo grapple on; opponent and couch off; jet off; clips and locks held");
        return 0;
    }

    static bool Locked(float value, float expect) => Math.Abs(value - expect) <= 0.001f;

    static bool AssetHas(string path, params string[] needles)
    {
        if (!File.Exists(path)) return false;
        string text = File.ReadAllText(path);
        for (int i = 0; i < needles.Length; i++)
        {
            if (text.IndexOf(needles[i], StringComparison.Ordinal) < 0)
                return false;
        }
        return true;
    }
}
