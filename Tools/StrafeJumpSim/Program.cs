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
        Console.WriteLine(Tag.Art.VerbPoseClips.PolishProofLine());
        if (!Tag.Art.VerbPoseClips.PolishHolds())
        {
            Console.Error.WriteLine("slide pose polish is not held");
            return 1;
        }
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

        Console.WriteLine(TagArena.Movement.EnginePass2.ProofLine());
        if (!TagArena.Movement.EnginePass2.Holds())
        {
            Console.Error.WriteLine("engine pass 2 is not held");
            return 1;
        }

        string motorSrc = File.ReadAllText("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
        string probeSrc = File.ReadAllText("Assets/TagArenaMovement/Scripts/Detection/SurfaceProbe.cs");
        if (!TagArena.Movement.EnginePass2.Wired(motorSrc, probeSrc))
        {
            Console.Error.WriteLine("engine pass 2 is not wired");
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

        Console.WriteLine(Tag.Art.AirStrafeLeanPose.ProofLine());
        if (!Tag.Art.AirStrafeLeanPose.Holds())
        {
            Console.Error.WriteLine("air strafe lean is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.BunnyHopPose.ProofLine());
        if (!Tag.Art.BunnyHopPose.Holds())
        {
            Console.Error.WriteLine("bunny-hop pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.WallPose.ProofLine());
        Console.WriteLine(Tag.Art.WallPose.GraceProofLine());
        if (!Tag.Art.WallPose.Holds())
        {
            Console.Error.WriteLine("wall pose is not held");
            return 1;
        }

        Console.WriteLine(TagArena.Movement.SameWallLimit.ProofLine());
        if (!TagArena.Movement.SameWallLimit.Holds())
        {
            Console.Error.WriteLine("same-wall limit is not held");
            return 1;
        }

        Console.WriteLine(Tag.Level.LaunchPadRules.ProofLine());
        if (!Tag.Level.LaunchPadRules.Holds())
        {
            Console.Error.WriteLine("launch pad is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.LaunchPose.ProofLine());
        if (!Tag.Art.LaunchPose.Holds())
        {
            Console.Error.WriteLine("launch pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Level.ZipLineRules.ProofLine());
        if (!Tag.Level.ZipLineRules.Holds())
        {
            Console.Error.WriteLine("zip line is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.ZipPose.ProofLine());
        if (!Tag.Art.ZipPose.Holds())
        {
            Console.Error.WriteLine("zip pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.WallJumpPose.ProofLine());
        if (!Tag.Art.WallJumpPose.Holds())
        {
            Console.Error.WriteLine("wall-jump pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.PunchTagPose.ProofLine());
        if (!Tag.Art.PunchTagPose.Holds())
        {
            Console.Error.WriteLine("punch tag pose beats are not held");
            return 1;
        }

        Console.WriteLine(Tag.Gameplay.PunchStagger.ProofLine());
        if (!Tag.Gameplay.PunchStagger.Holds())
        {
            Console.Error.WriteLine("punch stagger is not held");
            return 1;
        }

        Console.WriteLine(Tag.Gameplay.TagBackImmunity.ProofLine());
        if (!Tag.Gameplay.TagBackImmunity.Holds())
        {
            Console.Error.WriteLine("no tag-back is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.TagBackRecoilPose.ProofLine());
        if (!Tag.Art.TagBackRecoilPose.Holds())
        {
            Console.Error.WriteLine("tag-back recoil is not held");
            return 1;
        }

        Console.WriteLine(Tag.Level.VerbIntegration.ProofLine());
        if (!Tag.Level.VerbIntegration.Holds())
        {
            Console.Error.WriteLine("verb integration is not held");
            return 1;
        }

        EnemyAiReport enemy = EnemyAiProof.Run();
        Console.WriteLine(enemy.ToString());
        if (!string.IsNullOrEmpty(enemy.PocketLine))
            Console.WriteLine(enemy.PocketLine);
        if (!string.IsNullOrEmpty(enemy.StackLine))
            Console.WriteLine(enemy.StackLine);
        if (!string.IsNullOrEmpty(enemy.AudioLine))
            Console.WriteLine(enemy.AudioLine);
        if (!enemy.Ok)
        {
            Console.Error.WriteLine(enemy.FailureText);
            return 1;
        }

        var rounds = RoundFlowProof.Run();
        Console.WriteLine(rounds.Line);
        if (!rounds.Ok)
        {
            Console.Error.WriteLine(rounds.Failure);
            return 1;
        }

        SettingsInputReport settings = SettingsInputProof.Run();
        Console.WriteLine(settings.Line);
        if (!settings.Ok)
        {
            Console.Error.WriteLine(settings.Failure);
            return 1;
        }

        Tag.Core.QaSweep.Report qa = Tag.Core.QaSweep.Run();
        Console.WriteLine(qa.Line);
        if (!qa.Ok)
        {
            Console.Error.WriteLine(qa.Failure);
            return 1;
        }

        Tag.Core.HotPathAlloc.Report hot = Tag.Core.HotPathAlloc.Run();
        Console.WriteLine(hot.Line);
        if (!hot.Ok || hot.After != 0)
        {
            Console.Error.WriteLine(hot.After != 0 ? "hot-path alloc flags are not zero" : hot.Failure);
            return 1;
        }

        Console.WriteLine(Tag.Art.VerbPoseClips.PunchTagPolishProofLine());
        if (!Tag.Art.VerbPoseClips.PunchTagPolishHolds())
        {
            Console.Error.WriteLine("punch tag polish is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.GrapplePose.ProofLine());
        if (!Tag.Art.GrapplePose.Holds())
        {
            Console.Error.WriteLine("grapple pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.GrapplePose.PolishProofLine());
        if (!Tag.Art.GrapplePose.PolishHolds())
        {
            Console.Error.WriteLine("grapple pose polish is not held");
            return 1;
        }

        if (!AssetHas("Assets/Scripts/Art/DummyLocomotor.cs",
                "GrapplePose.Miss(",
                "GrapplePose.MissBeat",
                "GrapplePose.ReadWeight",
                "GrappleVisual()",
                "_grappleMissOwns"))
        {
            Console.Error.WriteLine("grapple pose polish is not on the shared locomotor");
            return 1;
        }

        Console.WriteLine(Tag.Art.PoseHandoff.ProofLine());
        Console.WriteLine(Tag.Art.PoseHandoff.ProofLine2());
        Console.WriteLine(Tag.Art.PoseHandoff.ProofLine3());
        Console.WriteLine(Tag.Art.PoseHandoff.ProofLine4());
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

        Console.WriteLine(Tag.Art.LandPose.ProofLine());
        if (!Tag.Art.LandPose.Holds())
        {
            Console.Error.WriteLine("land pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.MechPoseProof.ProofLine());
        if (!Tag.Art.MechPoseProof.Holds())
        {
            Console.Error.WriteLine("mech pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.CrouchPose.ProofLine());
        if (!Tag.Art.CrouchPose.Holds())
        {
            Console.Error.WriteLine("crouch pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.MantlePose.ProofLine());
        if (!Tag.Art.MantlePose.Holds())
        {
            Console.Error.WriteLine("mantle pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.LungePose.ProofLine());
        if (!Tag.Art.LungePose.Holds())
        {
            Console.Error.WriteLine("lunge pose beats are not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.BecomeItPose.ProofLine());
        if (!Tag.Art.BecomeItPose.Holds())
        {
            Console.Error.WriteLine("become-it pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.PivotPose.ProofLine());
        if (!Tag.Art.PivotPose.Holds())
        {
            Console.Error.WriteLine("pivot pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.IdlePose.ProofLine());
        if (!Tag.Art.IdlePose.Holds())
        {
            Console.Error.WriteLine("idle pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.StopPlantPose.ProofLine());
        if (!Tag.Art.StopPlantPose.Holds())
        {
            Console.Error.WriteLine("stop plant pose is not held");
            return 1;
        }

        Console.WriteLine(Tag.Art.AimTorsoPose.ProofLine());
        if (!Tag.Art.AimTorsoPose.Holds())
        {
            Console.Error.WriteLine("aim torso is not held");
            return 1;
        }

        DummyPosePathReport dummyPose = DummyPosePathProof.Run();
        Console.WriteLine(dummyPose.ToString());
        if (!dummyPose.Ok)
        {
            Console.Error.WriteLine(dummyPose.FailureText);
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

        Console.WriteLine(Tag.Art.JumpLandTell.ProofLine());
        if (!Tag.Art.JumpLandTell.ScaleHolds())
        {
            Console.Error.WriteLine("land scale is not held");
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
            || !Locked(cfg.airCrouchFallMult, 2f) || !Locked(cfg.maxFallSpeed, 56.16f)
            || !Locked(cfg.walkSpeed, 6.9f) || !Locked(cfg.sprintSpeed, 13.8f)
            || !Locked(cfg.crouchSpeed, 3.68f) || !Locked(cfg.airSpeedCap, 13.8f)
            || !Locked(cfg.fallGravityMult, 1.62f) || !Locked(cfg.gravity, 22f))
        {
            Console.Error.WriteLine("locked feel numbers drifted");
            return 1;
        }

        if (!AssetHas("Assets/Resources/TagArena/MovementConfig.asset",
                "coyoteTime: 0.1", "jumpBuffer: 0.16", "clingReleaseGrace: 0.08",
                "jumpSpeed: 24.7", "slideBoost: 0", "enableJet: 0",
                "airDashDuration: 0.1", "airDashSpeed: 15", "airDashCooldown: 30",
                "airCrouchFallMult: 2", "maxFallSpeed: 56.16",
                "walkSpeed: 6.9", "sprintSpeed: 13.8", "crouchSpeed: 3.68",
                "airSpeedCap: 13.8", "fallGravityMult: 1.62", "gravity: 22")
            || !AssetHas("Assets/ScriptableObjects/PunchTagTuning.asset", "reach: 1.55"))
        {
            Console.Error.WriteLine("locked asset numbers drifted");
            return 1;
        }

        Tag.Level.MegaParkP1Layout.Audit map = Tag.Level.MegaParkP1Layout.Run();
        Console.WriteLine(map.Line);
        if (!map.Ok)
        {
            Console.Error.WriteLine(map.Failure);
            return 1;
        }

        if (!Locked(cfg.sprintSpeed, Tag.Level.MegaParkP1Layout.SprintSpeed))
        {
            Console.Error.WriteLine("sprint 12 is not the measured loop speed");
            return 1;
        }

        Tag.Level.PocketParkLayout.Audit pocket = Tag.Level.PocketParkLayout.Run();
        Console.WriteLine(pocket.Line);
        if (!pocket.Ok)
        {
            Console.Error.WriteLine(pocket.Failure);
            return 1;
        }

        Tag.Level.StackYardLayout.Audit stack = Tag.Level.StackYardLayout.Run();
        Console.WriteLine(stack.Line);
        if (!stack.Ok)
        {
            Console.Error.WriteLine(stack.Failure);
            return 1;
        }

        string teardown = Tag.Level.ParkArena.Census.Prove(out bool teardownOk);
        Console.WriteLine(teardown);
        if (!teardownOk)
        {
            Console.Error.WriteLine("arena teardown did not return to baseline");
            return 1;
        }

        OnboardingReport onboard = OnboardingProof.Run();
        Console.WriteLine(onboard.Line);
        if (!onboard.Ok)
        {
            Console.Error.WriteLine(onboard.Failure);
            return 1;
        }

        Tag.Core.FrameBudget.Report[] budgets = Tag.Core.FrameBudget.RunAll();
        for (int b = 0; b < budgets.Length; b++)
        {
            Console.WriteLine(budgets[b].Line);
            if (!budgets[b].Ok)
            {
                Console.Error.WriteLine(budgets[b].Failure);
                return 1;
            }
        }

        Tag.Core.FrameBudget.Report[] splits = Tag.Core.FrameBudget.RunSplitAll();
        for (int b = 0; b < splits.Length; b++)
        {
            Console.WriteLine(splits[b].Line);
            if (!splits[b].Ok)
            {
                Console.Error.WriteLine(splits[b].Failure);
                return 1;
            }
        }

        Tag.Audio.AudioReport audio = Tag.Audio.AudioProof.Run();
        Console.WriteLine(audio.Line);
        if (!audio.Ok)
        {
            Console.Error.WriteLine(audio.Failure);
            return 1;
        }

        Tag.Front.FrontSession.Report front = Tag.Front.FrontSession.Run();
        Console.WriteLine(front.Line);
        if (!front.Ok)
        {
            Console.Error.WriteLine(front.Failure);
            return 1;
        }

        Tag.Couch.CouchPlay.Report couch = Tag.Couch.CouchPlay.Run();
        Console.WriteLine(couch.Line);
        if (!couch.Ok)
        {
            Console.Error.WriteLine(couch.Failure);
            return 1;
        }

        Tag.Settings.AccessibilityReport access = Tag.Settings.AccessibilityProof.Run();
        Console.WriteLine(access.Line);
        if (!access.Ok)
        {
            Console.Error.WriteLine(access.Failure);
            return 1;
        }

        string stills = Tag.Level.ArenaStill.WriteFrontEnd();
        Console.WriteLine(stills);
        if (stills.IndexOf("FAIL", StringComparison.Ordinal) >= 0)
        {
            Console.Error.WriteLine(stills);
            return 1;
        }

        Tag.Core.QaSweep2.Report sweep2 = Tag.Core.QaSweep2.Run();
        Console.WriteLine(sweep2.Line);
        if (!sweep2.Ok)
        {
            Console.Error.WriteLine(sweep2.Failure);
            return 1;
        }

        Tag.Practice.PracticeProof.Report practice = Tag.Practice.PracticeProof.Run();
        Console.WriteLine(practice.Line);
        if (!practice.Ok)
        {
            Console.Error.WriteLine(practice.Failure);
            return 1;
        }

        Tag.MatchStats.MatchStatsProof.Report matchStats = Tag.MatchStats.MatchStatsProof.Run();
        Console.WriteLine(matchStats.Line);
        if (!matchStats.Ok)
        {
            Console.Error.WriteLine(matchStats.Failure);
            return 1;
        }

        Tag.Profiles.LocalProfiles.Report profiles = Tag.Profiles.LocalProfiles.Run();
        Console.WriteLine(profiles.Line);
        if (!profiles.Ok)
        {
            Console.Error.WriteLine(profiles.Failure);
            return 1;
        }

        Tag.Level.ZoneReadability.Report zones = Tag.Level.ZoneReadability.Run();
        Console.WriteLine(zones.Line);
        if (!zones.Ok)
        {
            Console.Error.WriteLine(zones.Failure);
            return 1;
        }

        Tag.Core.QaSweep3.Report sweep3 = Tag.Core.QaSweep3.Run();
        Console.WriteLine(sweep3.Line);
        if (!sweep3.Ok)
        {
            Console.Error.WriteLine(sweep3.Failure);
            return 1;
        }

        Console.WriteLine(Tag.Art.SmoothMotion.ProofLine());
        Console.WriteLine(Tag.Art.SmoothMotion.DetailLine());
        Console.WriteLine(Tag.Art.SmoothMotion.ParkourLine());
        Console.WriteLine(Tag.Art.SmoothMotion.ResponseLine());
        Console.WriteLine(Tag.Art.LocomotionPolish.ProofLine());
        Console.WriteLine(Tag.Art.LocomotionPolish.StepLine());
        Console.WriteLine(Tag.Art.BodyLife.ProofLine());
        Console.WriteLine(Tag.Art.TransitionMatrix.ProofLine());
        Console.WriteLine(Tag.Art.TransitionMatrix.TopLine());
        if (!Tag.Art.SmoothMotion.Holds())
        {
            Console.Error.WriteLine("smooth motion is not held");
            return 1;
        }
        if (!Tag.Art.LocomotionPolish.Holds())
        {
            Console.Error.WriteLine("locomotion polish is not held");
            return 1;
        }
        if (!Tag.Art.BodyLife.Holds())
        {
            Console.Error.WriteLine("body life is not held");
            return 1;
        }
        if (!Tag.Art.TransitionMatrix.Holds())
        {
            Console.Error.WriteLine("transition matrix is not held");
            return 1;
        }
        Console.WriteLine(Tag.Art.UpperBody.ProofLine());
        Console.WriteLine(Tag.Art.HangMotion.ProofLine());
        Console.WriteLine(Tag.Settings.ResponseLatency.ProofLine());
        Console.WriteLine(Tag.Settings.StickQuality.ProofLine());
        if (!Tag.Art.UpperBody.Holds())
        {
            Console.Error.WriteLine("upper body is not held");
            return 1;
        }
        if (!Tag.Art.HangMotion.Holds())
        {
            Console.Error.WriteLine("hang motion is not held");
            return 1;
        }
        if (!Tag.Settings.ResponseLatency.Holds())
        {
            Console.Error.WriteLine("response latency is not held");
            return 1;
        }
        if (!Tag.Settings.StickQuality.Holds())
        {
            Console.Error.WriteLine("stick quality is not held");
            return 1;
        }
        Console.WriteLine(Tag.Art.FootSlide.ProofLine());
        Console.WriteLine(Tag.Art.RespawnBlink.ProofLine());
        if (!Tag.Art.FootSlide.Holds())
        {
            Console.Error.WriteLine("foot slide is not held");
            return 1;
        }
        if (!Tag.Art.RespawnBlink.Holds())
        {
            Console.Error.WriteLine("respawn blink is not held");
            return 1;
        }
        Console.WriteLine(Tag.Art.ClimbContact.ProofLine());
        if (!Tag.Art.ClimbContact.Holds())
        {
            Console.Error.WriteLine("climb contact is not held");
            return 1;
        }
        Console.WriteLine(Tag.Art.BodyLine.ProofLine());
        if (!Tag.Art.BodyLine.Holds())
        {
            Console.Error.WriteLine("body line is not held");
            return 1;
        }
        Console.WriteLine(Tag.Art.LocoFeel.ProofLine());
        if (!Tag.Art.LocoFeel.Holds())
        {
            Console.Error.WriteLine("loco feel is not held");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/DummyLocomotor.cs",
                "SmoothMotion.Smooth",
                "SmoothMotion.SecondsForSlew",
                "SmoothMotion.CycleSlew",
                "ClimbPresented",
                "RepeatingPush",
                "AbsorbPop",
                "EaseFacing",
                "LocomotionPolish.PlayCadence",
                "LocomotionPolish.FacingStride",
                "LocomotionPolish.NoteTurn",
                "LocomotionPolish.AirPhase",
                "LocomotionPolish.FootPitch",
                "LocomotionPolish.HandPitch",
                "LocomotionPolish.HeadYaw",
                "TransitionMatrix.BoneSeconds",
                "TransitionMatrix.RootSeconds",
                "BodyLife.CrouchCadence",
                "BodyLife.Scrabble",
                "BodyLife.EntryPlant",
                "BodyLife.ModeBlend",
                "BodyLife.ArmPump",
                "BodyLife.FlinchWeight",
                "UpperBody.KeepLegs",
                "UpperBody.AimTwist",
                "HangMotion.LegTrail",
                "HangMotion.SwayExtra",
                "HangMotion.RopeSpine",
                "HangMotion.Windmill"))
        {
            Console.Error.WriteLine("smooth motion is not on the locomotor");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Input/PlayerInputReader.cs",
                "DefaultExecutionOrder(-200)",
                "_readSerial == serial"))
        {
            Console.Error.WriteLine("input read is not same-frame");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Camera/TpsMoveCamera.cs",
                "SmoothMotion.Smooth",
                "SmoothMotion.YawSeconds",
                "fovKick=0",
                "Quaternion.Slerp"))
        {
            Console.Error.WriteLine("camera follow is not smoothed");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Camera/ChaseCam.cs",
                "if (desired < current) return desired;",
                "BoomRate = 6f"))
        {
            Console.Error.WriteLine("boom pull-in is not instant");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Camera/TpsMoveCamera.cs",
                "DefaultExecutionOrder(-100)",
                "void ApplyLook()"))
        {
            Console.Error.WriteLine("third-person look is not before the motor");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Camera/FpsMoveCamera.cs",
                "DefaultExecutionOrder(-100)",
                "void ApplyLook()"))
        {
            Console.Error.WriteLine("first-person look is not before the motor");
            return 1;
        }
        if (LateHas("Assets/TagArenaMovement/Scripts/Camera/TpsMoveCamera.cs", "LookFeel.Deltas")
            || LateHas("Assets/TagArenaMovement/Scripts/Camera/FpsMoveCamera.cs", "LookFeel.Deltas"))
        {
            Console.Error.WriteLine("look is still applied in LateUpdate");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Input/PlayerInputReader.cs",
                "StickQuality.Shape"))
        {
            Console.Error.WriteLine("stick shape is not on the reader");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Settings/LookFeel.cs", "lookX * Sens"))
        {
            Console.Error.WriteLine("mouse look is not a straight multiply");
            return 1;
        }
        if (File.ReadAllText("Assets/Scripts/Settings/LookFeel.cs").IndexOf("SmoothDamp", StringComparison.Ordinal) >= 0)
        {
            Console.Error.WriteLine("look smoothing was added");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Settings/SettingsRuntime.cs", "ProcessEventsInDynamicUpdate"))
        {
            Console.Error.WriteLine("input events are not on the dynamic update");
            return 1;
        }
        if (!AssetHas("ProjectSettings/QualitySettings.asset", "vSyncCount: 1"))
        {
            Console.Error.WriteLine("vsync is not on");
            return 1;
        }
        if (!AssetHas("ProjectSettings/TimeManager.asset", "Fixed Timestep: 0.02"))
        {
            Console.Error.WriteLine("fixed timestep moved");
            return 1;
        }
        if (File.ReadAllText("ProjectSettings/InputManager.asset").IndexOf("dead: 0.19", StringComparison.Ordinal) >= 0)
        {
            Console.Error.WriteLine("joystick axial deadzone remains");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/WallPose.cs",
                "PlantShape",
                "HoldWeight",
                "Sample Entry",
                "FootSlide.WallTrail"))
        {
            Console.Error.WriteLine("climb cycle is not on the wall pose");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs",
                "VisualBlinkAge",
                "RespawnBlink.Seconds",
                "reason == \"kill-plane\"",
                "reason == \"practice-restart\"",
                "Vector3 settleTarget = stand"))
        {
            Console.Error.WriteLine("respawn blink or vault stand is not on the motor");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/DummyLocomotor.cs",
                "YawPivot",
                "DummyVisual",
                "Mathf.DeltaAngle",
                "RespawnBlink.Hidden"))
        {
            Console.Error.WriteLine("visual yaw is not on the mannequin");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Camera/TpsMoveCamera.cs",
                "RespawnBlink.Open"))
        {
            Console.Error.WriteLine("respawn blink is not on the camera");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/DummyLocomotor.cs",
                "ClimbContact.Drag",
                "ClimbContact.Tilt",
                "ClimbContact.Grab",
                "ApplyChestGap",
                "TryLip"))
        {
            Console.Error.WriteLine("parkour contact is not on the mannequin");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/DummyLocomotor.cs",
                "BodyLine.WallArc",
                "BodyLine.LineFix",
                "HangMotion.RopeSpine",
                "BodyLine.ZipGrab",
                "BodyLine.ZipWeights",
                "BodyLine.PadOpen",
                "BodyLine.KeepStride",
                "BodyLine.TellOpen",
                "BodyLine.ReverseBlend",
                "BodyLine.ReachLead"))
        {
            Console.Error.WriteLine("body line is not on the mannequin");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/DummyLocomotor.cs",
                "LocoFeel.ArmPitch",
                "LocoFeel.Sole",
                "LocoFeel.CruiseTarget",
                "LocoFeel.StartChest",
                "LocoFeel.StopOpen",
                "LocoFeel.NoteWalkPivot",
                "LocoFeel.IdleLook",
                "LocoFeel.CrouchThigh",
                "LocoFeel.SlideWeight",
                "LocoFeel.HeadHold"))
        {
            Console.Error.WriteLine("loco feel is not on the mannequin");
            return 1;
        }
        if (!AssetHas("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs",
                "LedgeStand"))
        {
            Console.Error.WriteLine("the lip point is not on the motor");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/MotionGallery.cs",
                "slip ? -1f"))
        {
            Console.Error.WriteLine("the gallery climb does not play the slip");
            return 1;
        }

        string stillDir = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass1");
        Tag.Art.SmoothMotion.WriteStrip(Path.Combine(stillDir, "transitions.ppm"));
        string still2 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass2");
        Tag.Art.SmoothMotion.WriteParkourStills(Path.Combine(still2, "climb-wall-mantle.png"));
        string still3 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass3");
        Tag.Art.SmoothMotion.WriteLocomotionStills(Path.Combine(still3, "loco-air-ik.png"));
        string still4 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass4");
        Tag.Art.SmoothMotion.WritePass4Stills(Path.Combine(still4, "idle-wall-flinch.png"));
        string still5 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass5");
        Tag.Art.SmoothMotion.WritePass5Stills(Path.Combine(still5, "layer-hang.png"));
        string still7 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass7");
        Tag.Art.SmoothMotion.WritePass7Stills(Path.Combine(still7, "plant-blink-yaw.png"));
        string still8 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass8");
        Tag.Art.SmoothMotion.WritePass8Stills(Path.Combine(still8, "climb-contact.png"));
        string still9 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass9");
        Tag.Art.SmoothMotion.WritePass9Stills(Path.Combine(still9, "body-line.png"));
        string still10 = Path.Combine(RepoRoot(), "Docs", "SmoothStills", "pass10");
        Tag.Art.SmoothMotion.WritePass10Stills(Path.Combine(still10, "locomotion.png"));
        if (!File.Exists(Path.Combine(RepoRoot(), "Assets", "Scenes", "MotionGallery.unity")))
        {
            Console.Error.WriteLine("motion gallery scene is missing");
            return 1;
        }
        if (!AssetHas("Assets/Editor/MotionGalleryMenu.cs", "Tag/Motion Gallery", "MotionGallery.unity"))
        {
            Console.Error.WriteLine("motion gallery menu is missing");
            return 1;
        }
        if (!AssetHas("Assets/Scripts/Art/MotionGallery.cs",
                "DefaultExecutionOrder(-300)",
                "Time.timeScale"))
        {
            Console.Error.WriteLine("motion gallery driver is missing");
            return 1;
        }
        string buildList = File.ReadAllText(Path.Combine(RepoRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
        int playAt = buildList.IndexOf("Assets/Scenes/Play.unity", StringComparison.Ordinal);
        int galleryAt = buildList.IndexOf("Assets/Scenes/MotionGallery.unity", StringComparison.Ordinal);
        if (playAt < 0 || galleryAt < 0 || galleryAt < playAt)
        {
            Console.Error.WriteLine("motion gallery is not after Play in the build list");
            return 1;
        }

        Console.WriteLine("solo grapple on; opponent and couch off; jet off; clips and locks held");
        return 0;
    }

    static string RepoRoot()
    {
        string dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "Docs", "WhatsNew.md")))
                return dir;
            DirectoryInfo parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return Directory.GetCurrentDirectory();
    }

    static bool Locked(float value, float expect) => Math.Abs(value - expect) <= 0.001f;

    static bool LateHas(string path, string needle)
    {
        if (!File.Exists(path)) return false;
        string text = File.ReadAllText(path);
        int late = text.IndexOf("void LateUpdate", StringComparison.Ordinal);
        if (late < 0) return false;
        return text.IndexOf(needle, late, StringComparison.Ordinal) >= 0;
    }

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
