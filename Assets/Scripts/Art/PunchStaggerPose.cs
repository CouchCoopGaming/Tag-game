using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual stumble for a punch that did not tag. The chest drops over a buckled
    /// knee and the arms fly wide, then the weight eases back onto the gait.
    /// Not the punch line and not the tag catch. No root motion, no impulse.
    /// </summary>
    public static class PunchStaggerPose
    {
        public const bool RootMotion = false;
        public const float Duration = 0.25f;
        /// <summary>Smoothstep onto the stumble. Short, so the hit reads.</summary>
        public const float RiseSeconds = 0.06f;
        /// <summary>Hold ends here. The rest eases off.</summary>
        public const float FallStart = 0.16f;
        /// <summary>Fast enough that the buckle arrives inside the rise.</summary>
        public const float Slew = 240f;

        public const float ArmPitchL = 52f;
        public const float ArmYawL = 64f;
        public const float ArmRollL = 18f;
        public const float ElbowL = -42f;
        public const float ArmPitchR = 38f;
        public const float ArmYawR = -58f;
        public const float ArmRollR = -14f;
        public const float ElbowR = -36f;
        public const float ThighL = 26f;
        public const float ThighR = -18f;
        public const float KneeL = -68f;
        public const float KneeR = -10f;
        public const float Hip = 24f;
        public const float HipYaw = 10f;
        public const float Spine = 36f;
        public const float SpineYaw = -8f;
        public const float Head = 28f;
        public const float HeadYaw = 6f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, HipYaw, Spine, SpineYaw, Head, HeadYaw;
        }

        /// <summary>Chest over the front knee. Arms wide and back. The other leg catches.</summary>
        public static Sample Stumble()
        {
            return new Sample
            {
                ThighL = ThighL,
                ThighR = ThighR,
                KneeL = KneeL,
                KneeR = KneeR,
                ArmPitchL = ArmPitchL,
                ArmPitchR = ArmPitchR,
                ArmYawL = ArmYawL,
                ArmYawR = ArmYawR,
                ArmRollL = ArmRollL,
                ArmRollR = ArmRollR,
                ElbowL = ElbowL,
                ElbowR = ElbowR,
                Hip = Hip,
                HipYaw = HipYaw,
                Spine = Spine,
                SpineYaw = SpineYaw,
                Head = Head,
                HeadYaw = HeadYaw,
            };
        }

        /// <summary>0 at the hit, 1 through the hold, 0 again at the end of the quarter second.</summary>
        public static float Weight(float age)
        {
            if (age <= 0f || age >= Duration) return 0f;
            if (age < RiseSeconds)
                return Mathf.SmoothStep(0f, 1f, age / RiseSeconds);
            if (age < FallStart)
                return 1f;
            float back = (age - FallStart) / (Duration - FallStart);
            return 1f - Mathf.SmoothStep(0f, 1f, back);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(Duration - PunchStagger.Duration) > 0.001f) return false;
            if (RiseSeconds < 0.04f || RiseSeconds > 0.08f) return false;
            if (FallStart <= RiseSeconds || FallStart > Duration - 0.06f) return false;
            if (Slew < 160f) return false;

            Sample pose = Stumble();
            if (pose.ArmPitchL < 36f || pose.ArmPitchR < 24f) return false;
            if (pose.ArmYawL < 40f || pose.ArmYawR > -40f) return false;
            if (pose.KneeL > -55f || pose.KneeR < -24f) return false;
            if (Mathf.Abs(pose.KneeL - pose.KneeR) < 40f) return false;
            if (pose.Hip + pose.Spine < 48f) return false;
            if (pose.Head < 16f) return false;
            if (Mathf.Abs(pose.ArmPitchL - VerbPoseClips.PunchStrikePitch) < 40f) return false;
            if (Mathf.Abs(pose.ArmPitchL - VerbPoseClips.TagArmPitch) < 40f) return false;
            if (pose.ArmPitchL < 0f || pose.ArmPitchR < 0f) return false;

            if (Weight(0f) > 0.001f || Weight(Duration) > 0.001f) return false;
            if (Weight(RiseSeconds) < 0.99f) return false;
            if (Weight((RiseSeconds + FallStart) * 0.5f) < 0.99f) return false;
            float prev = -1f;
            bool falling = false;
            for (int i = 0; i <= 10; i++)
            {
                float w = Weight(Duration * (i / 10f));
                if (!falling && w + 0.001f < prev)
                    falling = true;
                if (!falling && w + 0.0001f < prev) return false;
                if (falling && w > prev + 0.0001f) return false;
                if (i > 0 && Mathf.Abs(w - prev) > 0.60f) return false;
                prev = w;
            }

            return true;
        }
    }
}
