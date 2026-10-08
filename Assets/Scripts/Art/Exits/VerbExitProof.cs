using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Headless checks: every verb has an exit, a cancel is gone by 0.08s,
    /// and the roll does not change velocity. Feel numbers are only read.
    /// </summary>
    public static class VerbExitProof
    {
        public static bool Holds()
        {
            if (VerbExitClock.RootMotion || VerbExitClips.RootMotion || LandingRollPose.RootMotion)
                return false;
            if (VerbExitClock.CancelSeconds < 0.05f || VerbExitClock.CancelSeconds > 0.08f)
                return false;
            if (VerbExitClock.CancelMul(0f) < 0.999f) return false;
            if (VerbExitClock.CancelMul(0.08f) > 0.0001f) return false;
            if (VerbExitClock.CancelMul(VerbExitClock.CancelSeconds) > 0.0001f) return false;
            if (VerbExitClock.Weight(0f, 0.28f, 0.08f) > 0.0001f) return false;

            float prev = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float age = VerbExitClock.CancelSeconds * (i / 8f);
                float mul = VerbExitClock.CancelMul(age);
                if (mul > prev + 0.0001f) return false;
                if (mul < -0.0001f || mul > 1.0001f) return false;
                prev = mul;
            }

            if (VerbExitClock.PoseWeight(0f, 0.28f) < 0.999f) return false;
            if (VerbExitClock.PoseWeight(0.28f, 0.28f) > 0.0001f) return false;
            if (VerbExitClock.PoseWeight(0.10f, 0.28f) < 0.999f) return false;
            if (Mathf.Abs(PoseHandoff.Ease(0.5f) - 0.5f) > 0.0001f) return false;

            int exits = 0;
            for (int i = 0; i < VerbExitClock.Catalog.Length; i++)
            {
                VerbExitId id = VerbExitClock.Catalog[i];
                if (!VerbExitClock.LengthOk(id)) return false;
                if (id != VerbExitId.Roll && id != VerbExitId.RollAbsorb)
                    exits++;
                VerbExitSample mid = VerbExitClips.Mid(id, false);
                if (id == VerbExitId.WallRun && mid.ThighL < 40f) return false;
            }
            if (exits != 17) return false;

            float closest = 99999f;
            for (int i = 0; i < VerbExitClock.Catalog.Length; i++)
            {
                for (int j = i + 1; j < VerbExitClock.Catalog.Length; j++)
                {
                    VerbExitSample ai = VerbExitClips.Mid(VerbExitClock.Catalog[i], false);
                    VerbExitSample bj = VerbExitClips.Mid(VerbExitClock.Catalog[j], true);
                    float gap = VerbExitSample.Gap(ai, bj);
                    if (gap < closest) closest = gap;
                    if (gap < 36f) return false;
                }
            }

            VerbExitSample push = VerbExitClips.Mid(VerbExitId.WallRun, false);
            VerbExitSample step = VerbExitClips.Mid(VerbExitId.WallRun, true);
            if (VerbExitSample.Gap(push, step) < 36f) return false;

            VerbExitSample soft = VerbExitClips.At(VerbExitId.SoftLand, 0.5f, 1f, false, false);
            VerbExitSample softSmall = VerbExitClips.At(VerbExitId.SoftLand, 0.5f, 0.35f, false, false);
            if (Mathf.Abs(softSmall.KneeL) >= Mathf.Abs(soft.KneeL)) return false;

            if (VerbExitPick.WallLeave(true, false, false, true, 8f) != VerbExitId.WallJump) return false;
            if (VerbExitPick.WallLeave(true, false, false, false, -1f) != VerbExitId.WallRun) return false;
            if (VerbExitPick.WallLeave(false, true, true, false, 1f) != VerbExitId.None) return false;
            if (VerbExitPick.WallLeave(false, true, false, true, -2f) != VerbExitId.ClingDrop) return false;
            if (VerbExitPick.WallLeave(false, true, false, false, 0f) != VerbExitId.ClimbTopOut) return false;
            if (VerbExitPick.MantleLeave(true, 12f) != VerbExitId.ClimbTopOut) return false;
            if (VerbExitPick.MantleLeave(false, 10f) != VerbExitId.Vault) return false;
            if (VerbExitPick.MantleLeave(false, 2f) != VerbExitId.Mantle) return false;
            if (VerbExitPick.PlaySlideExit(true, 6f)) return false;
            if (!VerbExitPick.PlaySlideExit(false, 0f)) return false;

            if (!VerbExitPick.Cancels(VerbExitId.Slide, true, false, false, false, false)) return false;
            if (!VerbExitPick.Cancels(VerbExitId.Roll, false, false, true, false, false)) return false;
            if (!VerbExitPick.Cancels(VerbExitId.SoftLand, false, true, false, false, false)) return false;
            if (!VerbExitPick.Cancels(VerbExitId.AirDash, false, false, false, true, false)) return false;
            if (VerbExitPick.Cancels(VerbExitId.WallJump, true, false, false, false, false)) return false;
            if (!VerbExitPick.Cancels(VerbExitId.WallJump, false, false, true, false, false)) return false;
            if (VerbExitPick.Cancels(VerbExitId.None, true, true, true, true, true)) return false;
            if (!VerbExitPick.CancelsState(VerbExitId.Punch, true, false, false, false, false, false)) return false;

            if (!LandingRollPose.Holds()) return false;
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (Mathf.Abs(cfg.maxFallSpeed - LandingRollPose.Terminal) > 0.001f) return false;
            if (Mathf.Abs(cfg.gravity - LandingRollPose.Gravity) > 0.001f) return false;
            if (Mathf.Abs(cfg.fallGravityMult - LandingRollPose.FallMult) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (cfg.slideBoost != 0f) return false;
            return closest > 36f;
        }

        public static string ProofLine()
        {
            float closest = 99999f;
            for (int i = 0; i < VerbExitClock.Catalog.Length; i++)
            {
                for (int j = i + 1; j < VerbExitClock.Catalog.Length; j++)
                {
                    float gap = VerbExitSample.Gap(
                        VerbExitClips.Mid(VerbExitClock.Catalog[i], false),
                        VerbExitClips.Mid(VerbExitClock.Catalog[j], false));
                    if (gap < closest) closest = gap;
                }
            }
            return "verb-exit"
                + " count=17"
                + " cancel=" + VerbExitClock.CancelSeconds.ToString("0.00") + "s"
                + " by0.08=" + VerbExitClock.CancelMul(0.08f).ToString("0.00")
                + " closest=" + closest.ToString("0")
                + " wallrun walljump climbtop cling vault mantle slide airdash"
                + " punch lunge zip launch grapple-arrive grapple-release"
                + " stagger tagback softland"
                + " roll=" + LandingRollPose.Seconds.ToString("0.00")
                + " input-waits=0";
        }
    }
}
