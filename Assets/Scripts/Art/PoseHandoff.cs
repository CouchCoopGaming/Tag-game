using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual handoffs between pose layers. Each edge has one smoothstep,
    /// and the weights on that edge sum to 1 so one layer owns the bones.
    /// Nothing here writes velocity, cling, or the root.
    /// </summary>
    public static class PoseHandoff
    {
        public const bool RootMotion = false;

        /// <summary>Slide cancel into the jump. Jump wins.</summary>
        public const float SlideJumpSeconds = VerbPoseClips.SlideBlendSeconds;
        /// <summary>Wall release into the fall beat. Fall wins.</summary>
        public const float WallFallSeconds = WallPose.ReleaseBlendSeconds;
        /// <summary>Punch interrupted by an air dash. Dash wins. Dash time is unchanged.</summary>
        public const float PunchDashSeconds = 0.10f;
        /// <summary>Punch interrupted by a jump. Jump wins. Jump height is unchanged.</summary>
        public const float PunchJumpSeconds = 0.10f;
        /// <summary>Grapple release into the fall beat. Fall wins.</summary>
        public const float GrappleFallSeconds = GrapplePose.ReleaseBlendSeconds;
        /// <summary>Landing thud into the gait. Gait wins once the absorb has eased.</summary>
        public const float LandGaitSeconds = GaitBlend.IdleBlendSeconds;
        /// <summary>Wall-jump push-off into the air stride. Stride wins.</summary>
        public const float WallStrideSeconds = WallPose.PushBlendSeconds;
        /// <summary>Crouch into SlideBody. Slide wins. Crouch blend time is unchanged.</summary>
        public const float CrouchSlideSeconds = 0.10f;
        /// <summary>Air dash into the air stride or the fall beat. Stride or fall wins.</summary>
        public const float DashAirSeconds = 0.10f;

        /// <summary>Smoothstep. 0 at the start of the blend, 1 at the end.</summary>
        public static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>Winner share of a 0..1 timer. The other layer keeps 1 - this.</summary>
        public static float ToWeight(float timer01)
        {
            return Ease(timer01);
        }

        /// <summary>fromW + toW = 1. toW is the winner and rises with age.</summary>
        public static void Pair(float age, float seconds, out float fromW, out float toW)
        {
            float u = 1f;
            if (seconds > 0.0001f)
                u = age / seconds;
            toW = Ease(u);
            fromW = 1f - toW;
        }

        /// <summary>
        /// Wall, push-off, and air stride. They sum to 1.
        /// Age 0 is the wall. The push-off leads the middle. The stride wins.
        /// </summary>
        public static void WallJump01(float timer01, out float wallW, out float pushW, out float strideW)
        {
            float s = Ease(timer01);
            float stay = 1f - s;
            wallW = stay * stay;
            pushW = 2f * s * stay;
            strideW = s * s;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(SlideJumpSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(WallFallSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(PunchDashSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(PunchJumpSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(GrappleFallSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(LandGaitSeconds - GaitBlend.IdleBlendSeconds) > 0.001f) return false;
            if (LandGaitSeconds > JumpLandTell.FlashSeconds) return false;
            if (Mathf.Abs(WallStrideSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(CrouchSlideSeconds - CrouchPose.SlideHandoffSeconds) > 0.001f) return false;
            if (CrouchSlideSeconds < 0.08f || CrouchSlideSeconds > 0.12f) return false;
            if (Mathf.Abs(DashAirSeconds - AirDashPose.HandoffSeconds) > 0.001f) return false;
            if (DashAirSeconds < 0.08f || DashAirSeconds > 0.12f) return false;
            if (Ease(0f) > 0.0001f || Mathf.Abs(Ease(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Ease(0.5f) - 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(ToWeight(0f)) > 0.0001f || Mathf.Abs(ToWeight(1f) - 1f) > 0.0001f) return false;

            float prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float age = SlideJumpSeconds * i / 8f;
                Pair(age, SlideJumpSeconds, out float fromW, out float toW);
                if (Mathf.Abs(fromW + toW - 1f) > 0.0001f) return false;
                if (toW + 0.0001f < prev) return false;
                prev = toW;
            }

            Pair(0f, WallFallSeconds, out float wallFrom, out float fallTo);
            Pair(WallFallSeconds, WallFallSeconds, out float wallGone, out float fallFull);
            if (wallFrom < 0.999f || fallTo > 0.0001f) return false;
            if (wallGone > 0.0001f || fallFull < 0.999f) return false;

            WallJump01(0f, out float w0, out float p0, out float s0);
            if (w0 < 0.999f || p0 > 0.0001f || s0 > 0.0001f) return false;
            WallJump01(1f, out float w1, out float p1, out float s1);
            if (s1 < 0.999f || w1 > 0.0001f || p1 > 0.0001f) return false;
            WallJump01(0.5f, out float wM, out float pM, out float sM);
            if (Mathf.Abs(wM + pM + sM - 1f) > 0.0001f) return false;
            if (pM + 0.0001f < wM || pM + 0.0001f < sM) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                WallJump01(i / 8f, out float w, out float p, out float s);
                if (Mathf.Abs(w + p + s - 1f) > 0.0001f) return false;
                if (s + 0.0001f < prev) return false;
                prev = s;
            }

            return true;
        }

        public static string ProofLine()
        {
            WallJump01(0.5f, out float wallW, out float pushW, out float strideW);
            return "pose handoff"
                + " slide->jump " + SlideJumpSeconds.ToString("0.00") + "s jump wins"
                + " wall->fall " + WallFallSeconds.ToString("0.00") + "s fall wins"
                + " punch->dash " + PunchDashSeconds.ToString("0.00") + "s dash wins"
                + " punch->jump " + PunchJumpSeconds.ToString("0.00") + "s jump wins"
                + " grapple->fall " + GrappleFallSeconds.ToString("0.00") + "s fall wins"
                + " land->gait " + LandGaitSeconds.ToString("0.00") + "s gait wins"
                + " walljump->stride " + WallStrideSeconds.ToString("0.00") + "s stride wins"
                + " crouch->slide " + CrouchSlideSeconds.ToString("0.00") + "s slide wins"
                + " dash->air " + DashAirSeconds.ToString("0.00") + "s stride or fall wins"
                + " mid=" + wallW.ToString("0.00") + "+" + pushW.ToString("0.00") + "+" + strideW.ToString("0.00")
                + " sum=1 smoothstep";
        }
    }
}
