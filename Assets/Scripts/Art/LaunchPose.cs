using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual launch-pad arc. Arms swing up on the rise, knees tuck at the apex,
    /// then the body opens for the landing. Not the jump rise. Vertical speed
    /// picks the beat. Planar speed does not. No root motion.
    /// </summary>
    public static class LaunchPose
    {
        public const bool RootMotion = false;

        /// <summary>Swing holds at and above this rise. The tuck is the apex.</summary>
        public const float RiseVy = 10f;
        /// <summary>Open is full at and below this fall.</summary>
        public const float OpenVy = -10f;

        public const float SwingArmPitch = -155f;
        /// <summary>Spread that puts the rise arms beside the head. Yaw alone stays on the arm axis.</summary>
        public const float SwingArmYaw = 140f;
        /// <summary>
        /// Bone Z. Negative so the left arm rolls out and the right arm rolls out.
        /// The hands sit above the crown and outside the head.
        /// </summary>
        public const float SwingArmRoll = -56f;
        public const float SwingElbow = VerbPoseClips.ElbowClear;
        /// <summary>Knee spread on the rise. Enough to keep the thighs off the spine.</summary>
        public const float SwingThighRoll = 8f;
        public const float SwingThigh = 36f;
        public const float SwingKnee = -84f;
        public const float SwingHip = 6f;
        public const float SwingSpine = -4f;
        public const float SwingHead = -10f;

        public const float TuckArmPitch = -168f;
        public const float TuckArmYaw = 8f;
        public const float TuckElbow = -10f;
        public const float TuckThigh = 96f;
        public const float TuckKnee = -128f;
        public const float TuckHip = 10f;
        public const float TuckSpine = -14f;
        public const float TuckHead = -16f;

        public const float OpenArmPitch = -8f;
        public const float OpenArmYaw = 74f;
        public const float OpenElbow = -8f;
        public const float OpenThigh = 4f;
        public const float OpenKnee = -6f;
        public const float OpenHip = 2f;
        public const float OpenSpine = 2f;
        public const float OpenHead = 6f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR, ThighRollL, ThighRollR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head;
        }

        /// <summary>0 on the rise, 1 at the apex and below.</summary>
        public static float ToApex(float verticalSpeed)
        {
            return Mathf.SmoothStep(0f, 1f, Inv(RiseVy, 0f, verticalSpeed));
        }

        /// <summary>0 at the apex and above, 1 once the landing open is full.</summary>
        public static float OpenAmount(float verticalSpeed)
        {
            return Mathf.SmoothStep(0f, 1f, Inv(0f, OpenVy, verticalSpeed));
        }

        public static Sample At(float verticalSpeed)
        {
            float apex = ToApex(verticalSpeed);
            float open = OpenAmount(verticalSpeed);
            float pitch = Mathf.Lerp(Mathf.Lerp(SwingArmPitch, TuckArmPitch, apex), OpenArmPitch, open);
            float yaw = Mathf.Lerp(Mathf.Lerp(SwingArmYaw, TuckArmYaw, apex), OpenArmYaw, open);
            float roll = Mathf.Lerp(Mathf.Lerp(SwingArmRoll, 0f, apex), 0f, open);
            float elbow = Mathf.Lerp(Mathf.Lerp(SwingElbow, TuckElbow, apex), OpenElbow, open);
            float thigh = Mathf.Lerp(Mathf.Lerp(SwingThigh, TuckThigh, apex), OpenThigh, open);
            float knee = Mathf.Lerp(Mathf.Lerp(SwingKnee, TuckKnee, apex), OpenKnee, open);
            float thighRoll = Mathf.Lerp(Mathf.Lerp(SwingThighRoll, 0f, apex), 0f, open);
            return new Sample
            {
                ThighL = thigh,
                ThighR = thigh,
                ThighRollL = -thighRoll,
                ThighRollR = thighRoll,
                KneeL = knee,
                KneeR = knee,
                ArmPitchL = pitch,
                ArmPitchR = pitch,
                ArmYawL = yaw,
                ArmYawR = yaw,
                ArmRollL = -roll,
                ArmRollR = roll,
                ElbowL = elbow,
                ElbowR = elbow,
                Hip = Mathf.Lerp(Mathf.Lerp(SwingHip, TuckHip, apex), OpenHip, open),
                Spine = Mathf.Lerp(Mathf.Lerp(SwingSpine, TuckSpine, apex), OpenSpine, open),
                Head = Mathf.Lerp(Mathf.Lerp(SwingHead, TuckHead, apex), OpenHead, open),
            };
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (ToApex(RiseVy) > 0.001f || ToApex(24.7f) > 0.001f) return false;
            if (Mathf.Abs(ToApex(0f) - 1f) > 0.001f) return false;
            if (OpenAmount(0f) > 0.001f || OpenAmount(8f) > 0.001f) return false;
            if (Mathf.Abs(OpenAmount(OpenVy) - 1f) > 0.001f) return false;
            if (Mathf.Abs(OpenAmount(-16f) - 1f) > 0.001f) return false;

            Sample rise = At(24.7f);
            Sample apex = At(0f);
            Sample land = At(-16f);
            if (rise.ArmPitchL > -148f || apex.ArmPitchL > rise.ArmPitchL) return false;
            if (apex.KneeL > -110f || apex.ThighL < 88f) return false;
            if (land.ThighL > 12f || land.KneeL < -16f) return false;
            if (land.ArmYawL < 60f || land.ArmPitchL > -4f) return false;
            if (Mathf.Abs(apex.ThighL - JumpPose.TuckThigh) < 12f) return false;
            if (Mathf.Abs(apex.ArmPitchL - JumpPose.TuckArmPitch) < 20f) return false;
            if (Mathf.Abs(land.ArmYawL - JumpPose.FallArmYaw) < 8f) return false;
            if (apex.ArmPitchL >= 0f || rise.ArmPitchL >= 0f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample apex = At(0f);
            Sample land = At(-16f);
            return "launch pose"
                + " risePitch=" + At(24.7f).ArmPitchL.ToString("0")
                + " apexThigh=" + apex.ThighL.ToString("0")
                + " apexKnee=" + apex.KneeL.ToString("0")
                + " apexPitch=" + apex.ArmPitchL.ToString("0")
                + " openYaw=" + land.ArmYawL.ToString("0")
                + " openThigh=" + land.ThighL.ToString("0")
                + " beats=swing/tuck/open"
                + " rootMotion=0";
        }

        static float Inv(float from, float to, float v)
        {
            float d = to - from;
            if (Mathf.Abs(d) < 0.0001f) return v >= to ? 1f : 0f;
            float u = (v - from) / d;
            if (u < 0f) return 0f;
            if (u > 1f) return 1f;
            return u;
        }
    }
}
