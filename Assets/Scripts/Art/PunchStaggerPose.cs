using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual absorb for a punch that did not tag. The chest stays forward over
    /// the knees for the quarter second, then the gait returns. Not the punch
    /// line and not the tag catch. No root motion, no impulse.
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

        public const float ArmPitchL = 22f;
        public const float ArmYawL = -46f;
        public const float ArmRollL = 6f;
        public const float ElbowL = -50f;
        public const float ArmPitchR = 20f;
        public const float ArmYawR = 46f;
        public const float ArmRollR = -6f;
        public const float ElbowR = -46f;
        public const float ThighL = 116f;
        public const float ThighR = 116f;
        public const float ThighYawL = -56f;
        public const float ThighYawR = 56f;
        public const float ThighRollL = 20f;
        public const float ThighRollR = -20f;
        public const float KneeL = -96f;
        public const float KneeR = -96f;
        public const float FootL = 16f;
        public const float FootR = 16f;
        public const float Hip = 35f;
        public const float HipYaw = 0f;
        public const float Spine = 15f;
        public const float SpineYaw = 0f;
        public const float Head = -20f;
        public const float HeadYaw = 0f;
        /// <summary>Hips-bone drop, meters. The capsule does not move.</summary>
        public const float BoneDrop = 0.491f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ThighYawL, ThighYawR, ThighRollL, ThighRollR;
            public float FootL, FootR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, HipYaw, Spine, SpineYaw, Head, HeadYaw;
            public float Drop;
        }

        /// <summary>Chest forward over both knees, arms out, hips sat back. Holds for the quarter second.</summary>
        public static Sample Stumble()
        {
            return new Sample
            {
                ThighL = ThighL,
                ThighR = ThighR,
                ThighYawL = ThighYawL,
                ThighYawR = ThighYawR,
                ThighRollL = ThighRollL,
                ThighRollR = ThighRollR,
                KneeL = KneeL,
                KneeR = KneeR,
                FootL = FootL,
                FootR = FootR,
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
                Drop = BoneDrop,
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
            if (pose.ArmPitchL < 16f || pose.ArmPitchR < 16f) return false;
            if (pose.ArmYawL > -30f || pose.ArmYawR < 30f) return false;
            if (pose.ThighL < 45f || pose.ThighR < 45f) return false;
            if (pose.ThighYawL > -30f || pose.ThighYawR < 30f) return false;
            if (pose.KneeL > -45f || pose.KneeR > -45f) return false;
            if (pose.Hip < 35f || pose.Spine < 15f) return false;
            if (pose.Hip / pose.Spine < 1.5f) return false;
            if (pose.Drop < 0.20f) return false;
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
