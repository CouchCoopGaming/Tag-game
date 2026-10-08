using UnityEngine;
using TagArena.Movement;

namespace Tag.Art
{
    /// <summary>
    /// Visual-only check for the mechanics poses. Nothing here writes the motor,
    /// a timer, or the root. Player and DummyRunner share DummyLocomotor.
    /// </summary>
    public static class MechPoseProof
    {
        public static bool Holds()
        {
            if (WallPose.RootMotion || AirDashPose.RootMotion || LandPose.RootMotion || JumpPose.RootMotion) return false;
            if (LandPose.ControlDelay > 0.0001f) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (cfg == null) return false;
            if (Mathf.Abs(cfg.landStunSpeed - LandPose.HardImpact) > 0.001f) return false;
            if (Mathf.Abs(LandPose.SoftImpact - 5f) > 0.001f) return false;
            if (!LandPose.Holds()) return false;

            if (WallPose.RunTilt < 15f || WallPose.RunTilt > 20f) return false;
            if (Mathf.Abs(WallPose.WallRunSpeedRef - 9.5f) > 0.001f) return false;
            if (Mathf.Abs(WallPose.RunCadenceFull - GaitBlend.CadenceAt(WallPose.WallRunSpeedRef)) > 0.2f) return false;
            WallPose.Sample run = WallPose.Run(1f, true);
            if (run.LeanZ > -15f || run.LeanZ < -21f) return false;
            if (run.ArmYawL > -4f) return false;
            if (run.ArmPitchL > -8f || run.ArmPitchL < -40f) return false;
            WallPose.Sample runBack = WallPose.Run(-1f, true);
            if (run.ThighR <= run.ThighL) return false;
            if (runBack.ThighL <= runBack.ThighR) return false;

            if (Mathf.Abs(WallPose.ClimbSpeedRef - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(WallPose.SlipSpeedRef - 3.7f) > 0.001f) return false;
            WallPose.Sample reach = WallPose.Climb(1f, WallPose.ClimbSpeedRef);
            WallPose.Sample other = WallPose.Climb(-1f, WallPose.ClimbSpeedRef);
            if (reach.ArmPitchL >= reach.ArmPitchR) return false;
            if (other.ArmPitchR >= other.ArmPitchL) return false;
            if (reach.ThighR <= reach.ThighL) return false;
            if (other.ThighL <= other.ThighR) return false;
            if (Mathf.Abs(WallPose.ClimbRate(WallPose.ClimbSpeedRef) - WallPose.ClimbCadenceFull) > 0.02f) return false;

            WallPose.Sample slip = WallPose.Climb(1f, -WallPose.SlipSpeedRef);
            if (WallPose.SlipWeight(-WallPose.SlipSpeedRef) < 0.99f) return false;
            if (slip.ArmPitchL > -80f || slip.ArmPitchR > -80f) return false;
            if (WallPose.SlipSag < 0.10f) return false;
            if (slip.Spine >= reach.Spine) return false;
            if (Mathf.Abs(WallPose.SlipRate(-WallPose.SlipSpeedRef) - WallPose.SlipCadenceFull) > 0.02f) return false;

            WallJumpPose.Sample kick = WallJumpPose.Push(true);
            if (kick.ThighL < 40f || kick.ThighR < 40f) return false;
            if (kick.ArmPitchL > -12f || kick.ArmPitchR > -12f) return false;
            if (kick.LeanZ > -15f || kick.LeanZ < -21f) return false;
            float end = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            if (WallJumpPose.JumpWeight(end) < 0.999f) return false;
            WallJumpPose.Sample jumped = WallJumpPose.At(end, 24.7f, true, 0f);
            if (Mathf.Abs(jumped.LeanZ) > 0.05f) return false;
            if (!WallJumpPose.Holds()) return false;

            if (Mathf.Abs(AirDashPose.WindowSeconds - 0.10f) > 0.001f) return false;
            AirDashPose.Sample dash = AirDashPose.At(0f, 1f);
            float flat = dash.Hip + dash.Spine;
            if (flat < 75f || flat > 95f) return false;
            if (dash.ArmPitchL < 40f || dash.ThighL > -8f) return false;
            if (AirDashPose.DashWeight(0.05f) < 0.999f) return false;
            if (AirDashPose.DashWeight(AirDashPose.WindowSeconds + AirDashPose.HandoffSeconds) > 0.001f) return false;
            if (!AirDashPose.Holds()) return false;

            if (GaitBlend.FootSlip(GaitBlend.WalkSpeed) > 0.08f) return false;
            if (GaitBlend.FootSlip(GaitBlend.RunSpeed) > 0.08f) return false;
            if (GaitBlend.FootSlip(GaitBlend.SprintSpeed) > 0.24f) return false;
            if (!GaitBlend.StanceHolds(GaitBlend.WalkSpeed)) return false;
            if (!IdlePose.Holds()) return false;
            if (!WallPose.Holds()) return false;
            return true;
        }

        public static string ProofLine()
        {
            WallPose.Sample run = WallPose.Run(1f, true);
            WallPose.Sample reach = WallPose.Climb(1f, WallPose.ClimbSpeedRef);
            WallPose.Sample slip = WallPose.Climb(1f, -WallPose.SlipSpeedRef);
            WallJumpPose.Sample kick = WallJumpPose.Push(true);
            AirDashPose.Sample dash = AirDashPose.At(0f, 1f);
            float end = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            return "mech-pose"
                + " wallLean=" + (-run.LeanZ).ToString("0")
                + " innerYaw=" + run.ArmYawL.ToString("0")
                + " runCadence=" + WallPose.RunRate(WallPose.WallRunSpeedRef).ToString("0.0")
                + " climbReach=" + reach.ArmPitchL.ToString("0")
                + " climbPull=" + reach.ArmPitchR.ToString("0")
                + " climbRate=" + WallPose.ClimbRate(WallPose.ClimbSpeedRef).ToString("0.00")
                + " slipPitch=" + slip.ArmPitchL.ToString("0")
                + " slipSag=" + WallPose.SlipSag.ToString("0.00")
                + " slipRate=" + WallPose.SlipRate(-WallPose.SlipSpeedRef).ToString("0.00")
                + " kickThigh=" + kick.ThighL.ToString("0") + "/" + kick.ThighR.ToString("0")
                + " kickPitch=" + kick.ArmPitchL.ToString("0") + "/" + kick.ArmPitchR.ToString("0")
                + " jumpW=" + WallJumpPose.JumpWeight(end).ToString("0.00")
                + " dashWindow=" + AirDashPose.WindowSeconds.ToString("0.00")
                + " dashFlat=" + (dash.Hip + dash.Spine).ToString("0")
                + " dashBack=" + dash.ArmPitchL.ToString("0")
                + " dashExit=" + AirDashPose.DashWeight(AirDashPose.WindowSeconds + AirDashPose.HandoffSeconds).ToString("0.00")
                + " landSoft=" + LandPose.SoftImpact.ToString("0")
                + " landHard=" + LandPose.HardImpact.ToString("0")
                + " controlDelay=" + LandPose.ControlDelay.ToString("0.00")
                + " slipWalk=" + GaitBlend.FootSlip(GaitBlend.WalkSpeed).ToString("0.000")
                + " slipRun=" + GaitBlend.FootSlip(GaitBlend.RunSpeed).ToString("0.000")
                + " slipSprint=" + GaitBlend.FootSlip(GaitBlend.SprintSpeed).ToString("0.000")
                + " idleSole=1"
                + " shared=DummyLocomotor"
                + " rootMotion=0";
        }
    }
}
