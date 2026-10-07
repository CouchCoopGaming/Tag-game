using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual chest and head aim only. While a punch is telegraphed or in its
    /// windup, or a planar grapple is showing its aim or hanging on, the chest
    /// and the head ease toward that aim. PunchTagPose and GrapplePose keep
    /// the arms. Yaw and pitch are capped so the ribcage does not fold over.
    /// No root motion, and the camera is not touched.
    /// </summary>
    public static class AimTorsoPose
    {
        public const bool RootMotion = false;

        /// <summary>Ease in, and the same ease back out.</summary>
        public const float BlendSeconds = 0.10f;

        /// <summary>World yaw of the face. The chest carries most of it. About ±45°.</summary>
        public const float YawMax = 45f;
        /// <summary>World pitch of the face. Modest, so a high hook does not tip the torso over.</summary>
        public const float PitchMax = 18f;

        /// <summary>Share of the capped yaw on the spine. The head takes the rest.</summary>
        public const float ChestYawShare = 0.72f;
        public const float HeadYawShare = 0.28f;
        /// <summary>Share of the capped pitch. The face tracks a little more than the ribs.</summary>
        public const float ChestPitchShare = 0.40f;
        public const float HeadPitchShare = 0.60f;

        /// <summary>Fast enough that the 0.10 s blend is what you see, not the slew.</summary>
        public const float Slew = 240f;

        public struct Sample
        {
            public float ChestYaw, ChestPitch, HeadYaw, HeadPitch;
        }

        /// <summary>
        /// Planar yaw in body space, same sign as GrapplePose.LeanYaw.
        /// Positive local X is to the right and yaws negative. Clamped to ±YawMax.
        /// </summary>
        public static float YawToward(float localX, float localZ)
        {
            return Clamp(-GrapplePose.YawDegrees(localX, localZ), -YawMax, YawMax);
        }

        /// <summary>
        /// elevDeg is positive up (GrapplePose.ElevDegrees). Bone pitch positive looks down.
        /// Clamped to ±PitchMax.
        /// </summary>
        public static float PitchToward(float elevDeg)
        {
            return Clamp(-elevDeg, -PitchMax, PitchMax);
        }

        /// <summary>
        /// Chest and head offsets for an already-capped yaw and pitch.
        /// Local head yaw plus chest yaw is the world yaw of the face.
        /// </summary>
        public static Sample At(float yawDeg, float pitchDeg)
        {
            float yaw = Clamp(yawDeg, -YawMax, YawMax);
            float pitch = Clamp(pitchDeg, -PitchMax, PitchMax);
            return new Sample
            {
                ChestYaw = yaw * ChestYawShare,
                HeadYaw = yaw * HeadYawShare,
                ChestPitch = pitch * ChestPitchShare,
                HeadPitch = pitch * HeadPitchShare,
            };
        }

        /// <summary>Same smoothstep the other pose blends use.</summary>
        public static float Ease(float u)
        {
            return PoseHandoff.Ease(u);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(BlendSeconds - 0.10f) > 0.001f) return false;
            if (BlendSeconds < 0.08f || BlendSeconds > 0.12f) return false;
            if (Mathf.Abs(YawMax - 45f) > 0.001f) return false;
            if (PitchMax < 12f || PitchMax > 22f) return false;
            if (ChestYawShare < 0.55f || HeadYawShare < 0.15f) return false;
            if (Mathf.Abs(ChestYawShare + HeadYawShare - 1f) > 0.001f) return false;
            if (ChestPitchShare < 0.25f || HeadPitchShare < 0.40f) return false;
            if (Mathf.Abs(ChestPitchShare + HeadPitchShare - 1f) > 0.001f) return false;
            if (Slew < 180f) return false;

            if (Mathf.Abs(Ease(0f)) > 0.0001f || Mathf.Abs(Ease(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Ease(0.5f) - 0.5f) > 0.0001f) return false;
            if (Ease(0.25f) > Ease(0.75f)) return false;

            if (Mathf.Abs(YawToward(0f, 1f)) > 0.05f) return false;
            if (Mathf.Abs(YawToward(0f, 0f)) > 0.05f) return false;
            if (Mathf.Abs(YawToward(1f, 0f) - (-YawMax)) > 0.05f) return false;
            if (Mathf.Abs(YawToward(-1f, 0f) - YawMax) > 0.05f) return false;
            float mild = YawToward(0.2f, 1f);
            if (mild >= 0f || mild <= -YawMax) return false;
            if (Mathf.Abs(mild - GrapplePose.LeanYaw(0.2f, 1f)) > 0.05f) return false;
            if (YawToward(1f, 0f) > GrapplePose.LeanYaw(1f, 0f)) return false;

            if (Mathf.Abs(PitchToward(0f)) > 0.05f) return false;
            if (Mathf.Abs(PitchToward(40f) - (-PitchMax)) > 0.05f) return false;
            if (Mathf.Abs(PitchToward(-40f) - PitchMax) > 0.05f) return false;
            float mildUp = PitchToward(8f);
            if (Mathf.Abs(mildUp - (-8f)) > 0.05f) return false;
            float straightUp = GrapplePose.ElevDegrees(1f, 0f, 0f);
            if (straightUp < 80f) return false;
            if (Mathf.Abs(PitchToward(straightUp) - (-PitchMax)) > 0.05f) return false;

            Sample rest = At(0f, 0f);
            if (Mathf.Abs(rest.ChestYaw) > 0.001f || Mathf.Abs(rest.HeadYaw) > 0.001f) return false;
            if (Mathf.Abs(rest.ChestPitch) > 0.001f || Mathf.Abs(rest.HeadPitch) > 0.001f) return false;

            Sample right = At(YawToward(1f, 0f), PitchToward(40f));
            if (right.ChestYaw >= 0f || right.HeadYaw >= 0f) return false;
            if (Mathf.Abs(right.ChestYaw - (-YawMax * ChestYawShare)) > 0.05f) return false;
            if (Mathf.Abs(right.HeadYaw - (-YawMax * HeadYawShare)) > 0.05f) return false;
            if (Mathf.Abs((right.ChestYaw + right.HeadYaw) - (-YawMax)) > 0.05f) return false;
            if (right.ChestPitch >= 0f || right.HeadPitch >= 0f) return false;
            if (Mathf.Abs(right.ChestPitch - (-PitchMax * ChestPitchShare)) > 0.05f) return false;
            if (Mathf.Abs(right.HeadPitch - (-PitchMax * HeadPitchShare)) > 0.05f) return false;
            if (Mathf.Abs((right.ChestPitch + right.HeadPitch) - (-PitchMax)) > 0.05f) return false;
            if (Mathf.Abs(right.ChestYaw) > YawMax + 0.05f) return false;
            if (Mathf.Abs(right.ChestPitch) > PitchMax + 0.05f) return false;

            Sample left = At(YawMax + 20f, PitchMax + 20f);
            if (Mathf.Abs(left.ChestYaw + left.HeadYaw - YawMax) > 0.05f) return false;
            if (Mathf.Abs(left.ChestPitch + left.HeadPitch - PitchMax) > 0.05f) return false;

            if (Mathf.Abs(PunchTagPose.ReachMeters - 1.55f) > 0.001f) return false;
            if (GrapplePose.VerticalImpulse != 0f || GrapplePose.LeadRight) return false;
            if (GrapplePose.RootMotion || PunchTagPose.RootMotion) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample right = At(YawToward(1f, 0f), PitchToward(40f));
            return "aim torso"
                + " blend=" + BlendSeconds.ToString("0.00")
                + " yawCap=" + YawMax.ToString("0")
                + " pitchCap=" + PitchMax.ToString("0")
                + " chestYaw=" + right.ChestYaw.ToString("0.0")
                + " headYaw=" + right.HeadYaw.ToString("0.0")
                + " chestPitch=" + right.ChestPitch.ToString("0.0")
                + " headPitch=" + right.HeadPitch.ToString("0.0")
                + " slew=" + Slew.ToString("0")
                + " ease=" + Ease(0.5f).ToString("0.00")
                + " gate=punch telegraph/windup Ease " + BlendSeconds.ToString("0.00")
                + "s, grapple aim preview/attached Ease " + BlendSeconds.ToString("0.00")
                + "s; chest+head yaw cap " + YawMax.ToString("0")
                + " pitch cap " + PitchMax.ToString("0")
                + "; arms stay PunchTagPose and GrapplePose"
                + "; dummy shares punch aim; grapple stays solo"
                + "; rootMotion=0";
        }

        static float Clamp(float v, float lo, float hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
