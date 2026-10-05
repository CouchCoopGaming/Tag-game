using Tag.Gameplay;
using Tag.Local;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual pose layers DummyRunner shares with the solo human.
    /// Grapple and air dash stay off. Feel numbers are not written here.
    /// Opponent chase ribbons and the lunge tell stay on their own paths.
    /// </summary>
    public static class DummyPosePaths
    {
        public const string Gait = "gait";
        public const string Pivot = "pivot";
        public const string Idle = "idle";
        public const string Stop = "stop";
        public const string Jump = "jump";
        public const string Wall = "wall";
        public const string Punch = "punch";
        public const string Tag = "tag";
        public const string Lunge = "lunge";
        public const string Slide = "slide";
        public const string Crouch = "crouch";
        public const string Mantle = "mantle";
        public const string Land = "land";
        public const string Become = "become";
        public const string Grapple = "grapple";
        public const string Dash = "dash";

        /// <summary>Same lead as OpponentLungeTell. This file does not set it.</summary>
        public const float LungeTellSeconds = 0.45f;

        public static readonly string[] Shared =
        {
            Gait, Jump, Wall, Punch, Tag, Lunge, Slide, Crouch, Mantle, Land, Become, Pivot, Idle, Stop
        };

        public static readonly string[] Denied = { Grapple, Dash };

        public static bool IsOpponent(string pawnName)
        {
            return pawnName == SoloGrappleGate.OpponentPawnName;
        }

        /// <summary>
        /// The solo human and couch clones keep every layer they already had.
        /// DummyRunner keeps the shared list. Grapple and air dash return false.
        /// </summary>
        public static bool Allows(string pawnName, string layer)
        {
            if (!IsOpponent(pawnName)) return true;
            if (string.IsNullOrEmpty(layer)) return false;
            for (int i = 0; i < Shared.Length; i++)
            {
                if (Shared[i] == layer) return true;
            }
            return false;
        }

        public static bool Holds()
        {
            if (Shared.Length != 14 || Denied.Length != 2) return false;
            if (Shared[0] != Gait || Shared[1] != Jump || Shared[2] != Wall || Shared[3] != Punch
                || Shared[4] != Tag || Shared[5] != Lunge || Shared[6] != Slide || Shared[7] != Crouch
                || Shared[8] != Mantle || Shared[9] != Land || Shared[10] != Become || Shared[11] != Pivot
                || Shared[12] != Idle || Shared[13] != Stop)
                return false;
            if (Denied[0] != Grapple || Denied[1] != Dash) return false;
            if (Mathf.Abs(LungeTellSeconds - 0.45f) > 0.001f) return false;

            string opponent = SoloGrappleGate.OpponentPawnName;
            string solo = SoloGrappleGate.SoloPawnName;
            if (opponent != "DummyRunner" || solo != "Player") return false;
            if (!IsOpponent(opponent) || IsOpponent(solo) || IsOpponent("Player_P1")) return false;

            for (int i = 0; i < Shared.Length; i++)
            {
                if (!Allows(opponent, Shared[i])) return false;
                if (!Allows(solo, Shared[i])) return false;
                if (!Allows("Player_P1", Shared[i])) return false;
            }

            if (Allows(opponent, Grapple) || Allows(opponent, Dash)) return false;
            if (!Allows(solo, Grapple) || !Allows(solo, Dash)) return false;
            if (Allows(opponent, null) || Allows(opponent, "")) return false;

            if (SoloGrappleGate.EnableFor(false, true, 1, opponent)
                || SoloGrappleGate.EnableFor(false, false, 0, opponent))
                return false;
            if (!SoloGrappleGate.EnableFor(false, false, 0, solo)) return false;

            if (JumpLandTell.ForPawn(false, true, 1, opponent)) return false;
            if (!Allows(opponent, Land)) return false;
            if (!JumpLandTell.ForPawn(false, false, 0, solo)) return false;

            if (!OpponentChaseTell.IsOpponentPawn(opponent)) return false;
            if (OpponentChaseTell.IsOpponentPawn(solo)) return false;

            if (JumpPose.RootMotion || WallPose.RootMotion || PunchTagPose.RootMotion
                || CrouchPose.RootMotion || MantlePose.RootMotion || AirDashPose.RootMotion
                || GrapplePose.RootMotion || PoseHandoff.RootMotion || LungePose.RootMotion
                || BecomeItPose.RootMotion || PivotPose.RootMotion || IdlePose.RootMotion
                || AimTorsoPose.RootMotion || StopPlantPose.RootMotion)
                return false;
            if (Mathf.Abs(PunchTagPose.ReachMeters - 1.55f) > 0.001f) return false;
            if (Mathf.Abs(AirDashPose.WindowSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(LungePose.LeadSeconds - LungeTellSeconds) > 0.001f) return false;
            if (Mathf.Abs(LungePose.BurstSeconds - 0.20f) > 0.001f) return false;
            if (!LungePose.Holds()) return false;
            if (!BecomeItPose.Holds()) return false;
            if (!PivotPose.Holds()) return false;
            if (PivotPose.BlendSeconds < 0.10f || PivotPose.BlendSeconds > 0.18f) return false;
            if (!IdlePose.Holds()) return false;
            if (Mathf.Abs(IdlePose.FadeSeconds - 0.12f) > 0.001f) return false;
            if (!StopPlantPose.Holds()) return false;
            if (StopPlantPose.WindowSeconds < 0.12f || StopPlantPose.WindowSeconds > 0.20f) return false;
            if (!AimTorsoPose.Holds()) return false;
            if (Mathf.Abs(AimTorsoPose.BlendSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(AimTorsoPose.YawMax - 45f) > 0.001f) return false;
            if (AimTorsoPose.PitchMax > 22f) return false;
            if (IdlePose.HipRoll > 4.0f || IdlePose.KneeRest + IdlePose.KneeSettle > 12f) return false;
            if (BecomeItPose.HoldSeconds < 0.30f || BecomeItPose.HoldSeconds > 0.50f) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (cfg.slideBoost != 0f) return false;
            if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f) return false;
            if (Mathf.Abs(punch.reach - 1.55f) > 0.001f) return false;
            if (cfg.enableJet) return false;
            return true;
        }

        public static string ProofLine()
        {
            return "dummy pose"
                + " pawn=" + SoloGrappleGate.OpponentPawnName
                + " gait=GaitBlend speed"
                + " pivot=PivotPose plant/hips-then-chest/lead"
                + " idle=IdlePose shift/knee/breath"
                + " stop=StopPlantPose plant/hips/arms"
                + " jump=JumpPose beats"
                + " wall=WallPose climb/run"
                + " punch=PunchTagPose windup + AimTorsoPose chest/head"
                + " aim=punch shared, grapple solo"
                + " tag=PunchTagPose catch"
                + " lunge=LungePose telegraph/burst/recover + LungeTell " + LungeTellSeconds.ToString("0.00") + "s"
                + " slide=SlideBody"
                + " crouch=CrouchPose if used"
                + " mantle=MantlePose"
                + " land=JumpLandTell thud"
                + " become=BecomeItPose claim/give-up"
                + " denied=grapple+airdash"
                + " chase=OpponentChaseTell"
                + " numbers=player";
        }
    }
}
