using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// The striking hand yanks back when a tag-back thunks. Visual only.
    /// Reach, the spark, and the immunity window are not written here.
    /// </summary>
    public static class TagBackRecoilPose
    {
        public const bool RootMotion = false;
        public const float Seconds = 0.16f;
        public const float RiseSeconds = 0.04f;

        /// <summary>Right arm is the strike. Positive pitch pulls it back off the line.</summary>
        public const float Pitch = 64f;
        public const float Yaw = -22f;
        public const float Roll = 12f;
        public const float Elbow = -82f;
        public const float SpineYaw = -12f;
        public const float HeadYaw = -6f;

        /// <summary>1 just after the thunk, 0 again when the spark is gone.</summary>
        public static float Weight(float age)
        {
            if (age <= 0f || age >= Seconds) return 0f;
            if (age < RiseSeconds)
                return Mathf.SmoothStep(0f, 1f, age / RiseSeconds);
            float back = (age - RiseSeconds) / (Seconds - RiseSeconds);
            return 1f - Mathf.SmoothStep(0f, 1f, back);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Seconds < 0.12f || Seconds > 0.20f) return false;
            if (Pitch < 48f || Elbow > -60f) return false;
            if (Mathf.Abs(Pitch - VerbPoseClips.PunchStrikePitch) < 40f) return false;
            if (Weight(0f) > 0.001f || Weight(Seconds) > 0.001f) return false;
            if (Weight(RiseSeconds) < 0.99f) return false;
            if (Weight(Seconds * 0.5f) < 0.2f) return false;
            return true;
        }

        public static string ProofLine()
        {
            return "tag-back recoil"
                + " pitch=" + Pitch.ToString("0")
                + " elbow=" + Elbow.ToString("0")
                + " seconds=" + Seconds.ToString("0.00")
                + " riseW=" + Weight(RiseSeconds).ToString("0.00")
                + " rootMotion=0";
        }
    }
}
