using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual crouch only. Knees bent, chest leaned forward, hips down.
    /// Blends to and from idle, walk, and run sit in a tenth of a second.
    /// A slide that starts here eases into SlideBody on the slide blend.
    /// The motor capsule and slideBoost are not written. No root motion.
    /// </summary>
    public static class CrouchPose
    {
        public const bool RootMotion = false;

        /// <summary>Idle, walk, and run, both ways.</summary>
        public const float BlendSeconds = 0.10f;
        /// <summary>Crouch or crouch-walk into SlideBody. Slide wins.</summary>
        public const float SlideHandoffSeconds = 0.10f;
        /// <summary>Tracks the stance inside the blend, then holds.</summary>
        public const float Slew = 1400f;
        /// <summary>Tracks the slide handoff. The largest step is the lead thigh.</summary>
        public const float SlideHandoffSlew = 3600f;

        public const float Hip = 28f;
        public const float Spine = 18f;
        /// <summary>Negative looks up, so the face stays readable against the forward chest.</summary>
        public const float Head = -14f;
        public const float Thigh = 64f;
        public const float Knee = -80f;
        public const float Elbow = -90f;
        public const float ArmPitch = -44f;
        public const float ArmYaw = 18f;
        /// <summary>Visual mesh drop only. The capsule stays put.</summary>
        public const float Drop = 0.22f;

        /// <summary>Low stride under the same forward chest. Still a crouch, not a run.</summary>
        public const float WalkThighBase = 52f;
        public const float WalkThighReach = 14f;
        public const float WalkThighTrail = 7f;
        public const float WalkKneeBase = 74f;
        public const float WalkKneeReach = 10f;

        public static float WalkThigh(float step, float other)
        {
            return WalkThighBase + step * WalkThighReach - other * WalkThighTrail;
        }

        public static float WalkKnee(float step)
        {
            return -(WalkKneeBase + step * WalkKneeReach);
        }

        /// <summary>Smoothstep. 0 at the start of a blend, 1 at <see cref="BlendSeconds"/>.</summary>
        public static float BlendWeight(float age)
        {
            float u = 1f;
            if (BlendSeconds > 0.0001f)
                u = age / BlendSeconds;
            return PoseHandoff.Ease(u);
        }

        /// <summary>Slide share. 0 on the crouch, 1 on SlideBody.</summary>
        public static float SlideWeight(float age)
        {
            float u = 1f;
            if (SlideHandoffSeconds > 0.0001f)
                u = age / SlideHandoffSeconds;
            return PoseHandoff.Ease(u);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (BlendSeconds < 0.08f || BlendSeconds > 0.12f) return false;
            if (SlideHandoffSeconds < 0.08f || SlideHandoffSeconds > 0.12f) return false;
            if (Mathf.Abs(SlideHandoffSeconds - VerbPoseClips.SlideBlendSeconds) > 0.001f) return false;
            if (Mathf.Abs(BlendWeight(0f)) > 0.0001f) return false;
            if (Mathf.Abs(BlendWeight(BlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(BlendWeight(BlendSeconds * 0.5f) - 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(SlideWeight(0f)) > 0.0001f) return false;
            if (Mathf.Abs(SlideWeight(SlideHandoffSeconds) - 1f) > 0.0001f) return false;
            float prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float w = SlideWeight(SlideHandoffSeconds * i / 8f);
                float from = 1f - w;
                if (Mathf.Abs(from + w - 1f) > 0.0001f) return false;
                if (w + 0.0001f < prev) return false;
                prev = w;
            }

            float chest = Hip + Spine;
            if (chest < 40f || Hip < 20f || Spine < 12f) return false;
            if (Knee > -70f || Thigh < 50f) return false;
            if (Elbow > -80f || ArmPitch > -30f || ArmPitch < -50f) return false;
            if (Drop < 0.18f || Drop > 0.30f) return false;
            if (VerbPoseClips.SlideBodyDrop < Drop + 0.5f) return false;

            float slideChest = VerbPoseClips.SlideHip + VerbPoseClips.SlideSpine;
            if (slideChest > 0f || chest < 0f) return false;
            if (Mathf.Abs(slideChest - chest) < 60f) return false;
            if (Mathf.Abs(VerbPoseClips.SlideLeadThigh - Thigh) < 40f) return false;
            if (Mathf.Abs(VerbPoseClips.SlideTrailKnee - Knee) < 24f) return false;
            if (Mathf.Abs(Hip - VerbPoseClips.CrouchHip) > 0.01f) return false;
            if (Mathf.Abs(Spine - VerbPoseClips.CrouchSpine) > 0.01f) return false;
            if (Mathf.Abs(Thigh - VerbPoseClips.CrouchThigh) > 0.01f) return false;
            if (Mathf.Abs(Knee - VerbPoseClips.CrouchKnee) > 0.01f) return false;
            if (Mathf.Abs(Elbow - VerbPoseClips.CrouchElbow) > 0.01f) return false;
            if (Mathf.Abs(ArmPitch - VerbPoseClips.CrouchArmPitch) > 0.01f) return false;
            if (Mathf.Abs(ArmYaw - VerbPoseClips.CrouchArmYaw) > 0.01f) return false;
            if (Mathf.Abs(Drop - VerbPoseClips.CrouchDrop) > 0.001f) return false;

            float walk = WalkThigh(1f, 0f);
            float plant = WalkThigh(0f, 1f);
            if (walk <= plant) return false;
            if (WalkKnee(1f) > -70f) return false;
            if (Mathf.Abs(WalkThighBase - Thigh) > 20f) return false;
            if (Mathf.Abs(Hip) < 8f || Mathf.Abs(Knee) < 8f) return false;
            return true;
        }

        public static string ProofLine()
        {
            float chest = Hip + Spine;
            float slideW = SlideWeight(SlideHandoffSeconds * 0.5f);
            return "crouch pose"
                + " hip=" + Hip.ToString("0")
                + " spine=" + Spine.ToString("0")
                + " chest=" + chest.ToString("0")
                + " head=" + Head.ToString("0")
                + " thigh=" + Thigh.ToString("0")
                + " knee=" + Knee.ToString("0")
                + " elbow=" + Elbow.ToString("0")
                + " armPitch=" + ArmPitch.ToString("0")
                + " armYaw=" + ArmYaw.ToString("0")
                + " drop=" + Drop.ToString("0.00")
                + " walkThigh=" + WalkThighBase.ToString("0")
                + " walkKnee=" + WalkKneeBase.ToString("0")
                + " blend=" + BlendSeconds.ToString("0.00")
                + " slideHandoff=" + SlideHandoffSeconds.ToString("0.00")
                + " midSlide=" + slideW.ToString("0.00")
                + " slew=" + Slew.ToString("0")
                + " gate=smoothstep " + BlendSeconds.ToString("0.00")
                + "s to and from idle/walk/run"
                + "; slide: captured crouch smoothstep " + SlideHandoffSeconds.ToString("0.00")
                + "s into SlideBody, slide wins"
                + "; knees bent torso forward"
                + "; rootMotion=0";
        }
    }
}
