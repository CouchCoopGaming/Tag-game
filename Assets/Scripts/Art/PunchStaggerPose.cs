using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual stumble for a punch that did not tag. The head and the chest snap
    /// back and one leg steps, then the weight eases onto the gait. The window
    /// is the quarter second. Not the punch line and not the tag catch.
    /// No root motion, no impulse.
    /// </summary>
    public static class PunchStaggerPose
    {
        public const bool RootMotion = false;
        public const float Duration = 0.25f;
        /// <summary>Smoothstep onto the snap. Short, so the hit reads.</summary>
        public const float RiseSeconds = 0.05f;
        /// <summary>Hold ends here. The rest eases off.</summary>
        public const float FallStart = 0.16f;
        /// <summary>Fast enough that the snap arrives inside the rise.</summary>
        public const float Slew = 280f;

        public const float ArmPitchL = 46f;
        public const float ArmYawL = 28f;
        public const float ArmRollL = 14f;
        public const float ElbowL = -30f;
        public const float ArmPitchR = 34f;
        public const float ArmYawR = -26f;
        public const float ArmRollR = -12f;
        public const float ElbowR = -24f;
        public const float ThighL = 38f;
        public const float ThighR = -16f;
        public const float KneeL = -34f;
        public const float KneeR = -8f;
        public const float Hip = -18f;
        public const float HipYaw = 8f;
        public const float Spine = -16f;
        public const float SpineYaw = -6f;
        public const float Head = -24f;
        public const float HeadYaw = 4f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, HipYaw, Spine, SpineYaw, Head, HeadYaw;
        }

        /// <summary>Chest and head snap back. One leg steps. The other foot stays.</summary>
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
            if (pose.ArmYawL < 18f || pose.ArmYawR > -18f) return false;
            if (pose.ThighL < 28f || pose.ThighR > -8f) return false;
            if (pose.KneeL > -24f || pose.KneeR < -20f) return false;
            if (Mathf.Abs(pose.KneeL - pose.KneeR) < 16f) return false;
            if (pose.Hip + pose.Spine > -28f) return false;
            if (pose.Head > -16f) return false;
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
