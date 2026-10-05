using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual lunge only. Three beats at chase-cam distance.
    /// The 0.45 s LungeTell lead coils the torso, drops the leading shoulder,
    /// and pulls the free arm back. The 0.20 s motor burst is a committed
    /// forward stretch with the striking arm out. Recovery eases that shape
    /// back onto the gait. Speed, duration, cooldown, and the lead are not
    /// written here. No root motion.
    /// </summary>
    public static class LungePose
    {
        public const bool RootMotion = false;

        /// <summary>Same lead as OpponentLungeTell. This pose does not set it.</summary>
        public const float LeadSeconds = 0.45f;
        /// <summary>Same window as the motor lunge. This pose does not set it.</summary>
        public const float BurstSeconds = 0.20f;
        /// <summary>Burst or a cancelled coil back onto the gait. The gait wins.</summary>
        public const float RecoverSeconds = 0.16f;
        /// <summary>Fast enough that the coil and the stretch arrive inside their windows.</summary>
        public const float Slew = 72f;

        /// <summary>Charge at which the coil is fully on. The rest of the lead holds it.</summary>
        public const float TelegraphFullAt = 0.34f;

        public const float CoilPitchR = -36f;
        public const float CoilYawR = 18f;
        public const float CoilRollR = -34f;
        public const float CoilElbowR = -98f;
        public const float CoilPitchL = 76f;
        public const float CoilYawL = -22f;
        public const float CoilRollL = 8f;
        public const float CoilElbowL = -48f;
        public const float CoilThighL = 22f;
        public const float CoilThighR = -18f;
        public const float CoilKneeL = -40f;
        public const float CoilKneeR = -16f;
        public const float CoilSpine = 16f;
        public const float CoilSpineYaw = -36f;
        public const float CoilHip = 10f;
        public const float CoilHipYaw = 24f;
        public const float CoilHead = 6f;
        public const float CoilHeadYaw = -16f;

        public const float StretchPitchR = -124f;
        public const float StretchYawR = 22f;
        public const float StretchRollR = -6f;
        public const float StretchElbowR = -8f;
        public const float StretchPitchL = 70f;
        public const float StretchYawL = -36f;
        public const float StretchRollL = 6f;
        public const float StretchElbowL = -32f;
        public const float StretchThighL = 74f;
        public const float StretchThighR = -36f;
        public const float StretchKneeL = -78f;
        public const float StretchKneeR = -14f;
        public const float StretchSpine = 46f;
        public const float StretchSpineYaw = 22f;
        public const float StretchHip = 32f;
        public const float StretchHipYaw = -10f;
        public const float StretchHead = 14f;
        public const float StretchHeadYaw = 8f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, HipYaw, Spine, SpineYaw, Head, HeadYaw;
        }

        /// <summary>Coil. The striking shoulder is down and the free arm is back.</summary>
        public static Sample Telegraph()
        {
            return new Sample
            {
                ThighL = CoilThighL,
                ThighR = CoilThighR,
                KneeL = CoilKneeL,
                KneeR = CoilKneeR,
                ArmPitchL = CoilPitchL,
                ArmPitchR = CoilPitchR,
                ArmYawL = CoilYawL,
                ArmYawR = CoilYawR,
                ArmRollL = CoilRollL,
                ArmRollR = CoilRollR,
                ElbowL = CoilElbowL,
                ElbowR = CoilElbowR,
                Hip = CoilHip,
                HipYaw = CoilHipYaw,
                Spine = CoilSpine,
                SpineYaw = CoilSpineYaw,
                Head = CoilHead,
                HeadYaw = CoilHeadYaw,
            };
        }

        /// <summary>
        /// Committed stretch for the whole motor window. The right arm is the strike,
        /// the same side as PunchStrike. The left leg leads.
        /// </summary>
        public static Sample Burst()
        {
            return new Sample
            {
                ThighL = StretchThighL,
                ThighR = StretchThighR,
                KneeL = StretchKneeL,
                KneeR = StretchKneeR,
                ArmPitchL = StretchPitchL,
                ArmPitchR = StretchPitchR,
                ArmYawL = StretchYawL,
                ArmYawR = StretchYawR,
                ArmRollL = StretchRollL,
                ArmRollR = StretchRollR,
                ElbowL = StretchElbowL,
                ElbowR = StretchElbowR,
                Hip = StretchHip,
                HipYaw = StretchHipYaw,
                Spine = StretchSpine,
                SpineYaw = StretchSpineYaw,
                Head = StretchHead,
                HeadYaw = StretchHeadYaw,
            };
        }

        /// <summary>0 when the tell starts. 1 once the coil has arrived, then it holds.</summary>
        public static float TelegraphWeight(float charge01)
        {
            float c = charge01 < 0f ? 0f : (charge01 > 1f ? 1f : charge01);
            if (TelegraphFullAt <= 0.0001f) return c > 0f ? 1f : 0f;
            return PoseHandoff.Ease(c / TelegraphFullAt);
        }

        /// <summary>1 for the whole burst window. Outside it the burst is not on.</summary>
        public static float BurstWeight(float age)
        {
            if (age < 0f || age > BurstSeconds) return 0f;
            return 1f;
        }

        /// <summary>
        /// 1 at the start of the ease, matching the pose we just held, then down to 0.
        /// Smoothstep so the gait does not pop.
        /// </summary>
        public static float RecoverWeight(float age, float fromWeight)
        {
            float from = fromWeight < 0f ? 0f : (fromWeight > 1f ? 1f : fromWeight);
            if (age <= 0f) return from;
            if (RecoverSeconds <= 0.0001f || age >= RecoverSeconds) return 0f;
            float stay = 1f - PoseHandoff.Ease(age / RecoverSeconds);
            return from * stay;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(LeadSeconds - 0.45f) > 0.001f) return false;
            if (Mathf.Abs(BurstSeconds - 0.20f) > 0.001f) return false;
            if (RecoverSeconds < 0.12f || RecoverSeconds > 0.22f) return false;
            if (Slew < 64f || Slew > 120f) return false;
            if (TelegraphFullAt < 0.2f || TelegraphFullAt > 0.5f) return false;

            if (TelegraphWeight(0f) > 0.0001f) return false;
            if (TelegraphWeight(1f) < 0.999f) return false;
            if (Mathf.Abs(TelegraphWeight(TelegraphFullAt) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(TelegraphWeight(TelegraphFullAt * 0.5f) - 0.5f) > 0.0001f) return false;
            float prevTell = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float w = TelegraphWeight(i / 8f);
                if (w + 0.0001f < prevTell) return false;
                prevTell = w;
            }

            if (Mathf.Abs(BurstWeight(0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(BurstWeight(BurstSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(BurstWeight(BurstSeconds * 0.5f) - 1f) > 0.0001f) return false;
            if (BurstWeight(-0.01f) > 0.0001f || BurstWeight(BurstSeconds + 0.01f) > 0.0001f) return false;

            if (Mathf.Abs(RecoverWeight(0f, 1f) - 1f) > 0.0001f) return false;
            if (RecoverWeight(RecoverSeconds, 1f) > 0.0001f) return false;
            if (Mathf.Abs(RecoverWeight(RecoverSeconds * 0.5f, 1f) - 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(RecoverWeight(0f, 0.4f) - 0.4f) > 0.0001f) return false;
            if (RecoverWeight(RecoverSeconds, 0.4f) > 0.0001f) return false;
            float prevBack = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float w = RecoverWeight(RecoverSeconds * i / 8f, 1f);
                if (w > prevBack + 0.0001f) return false;
                prevBack = w;
            }

            Sample coil = Telegraph();
            Sample stretch = Burst();
            if (coil.SpineYaw > -20f) return false;
            if (coil.ArmRollR > -24f) return false;
            if (coil.ArmPitchL < 50f) return false;
            if (coil.ElbowR > -70f) return false;
            if (stretch.ArmPitchR > -100f || stretch.ArmPitchR < -150f) return false;
            if (stretch.ArmPitchR > coil.ArmPitchR - 50f) return false;
            if (stretch.ElbowR < -20f) return false;
            if (stretch.ElbowR < coil.ElbowR + 60f) return false;
            if (stretch.ArmPitchL < 40f) return false;
            if (stretch.Spine < 32f || stretch.Spine < coil.Spine + 20f) return false;
            if (stretch.SpineYaw < 8f || coil.SpineYaw > 0f || stretch.SpineYaw < coil.SpineYaw) return false;
            if (stretch.ThighL < coil.ThighL + 40f) return false;
            if (stretch.ThighR > coil.ThighR) return false;
            if (stretch.KneeL > -60f) return false;
            if (stretch.Hip < coil.Hip) return false;
            if (Mathf.Abs(stretch.ArmRollR) > 16f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample coil = Telegraph();
            Sample stretch = Burst();
            float midTell = TelegraphWeight(TelegraphFullAt * 0.5f);
            float midBack = RecoverWeight(RecoverSeconds * 0.5f, 1f);
            return "lunge pose"
                + " lead=" + LeadSeconds.ToString("0.00")
                + " burst=" + BurstSeconds.ToString("0.00")
                + " recover=" + RecoverSeconds.ToString("0.00")
                + " coilYaw=" + coil.SpineYaw.ToString("0")
                + " dropRoll=" + coil.ArmRollR.ToString("0")
                + " freeArm=" + coil.ArmPitchL.ToString("0")
                + " strike=" + stretch.ArmPitchR.ToString("0")
                + " strikeElbow=" + stretch.ElbowR.ToString("0")
                + " leadThigh=" + stretch.ThighL.ToString("0")
                + " spine=" + stretch.Spine.ToString("0")
                + " midTell=" + midTell.ToString("0.00")
                + " midBack=" + midBack.ToString("0.00")
                + " slew=" + Slew.ToString("0")
                + " gate=coil across LungeTell " + LeadSeconds.ToString("0.00")
                + "s; stretch weight 1 for 0.." + BurstSeconds.ToString("0.00")
                + "s; recover smoothstep " + RecoverSeconds.ToString("0.00")
                + "s onto gait"
                + "; tell=OpponentLungeTell confirm=HitConfirmTell"
                + "; speed duration cooldown lead untouched"
                + "; rootMotion=0";
        }
    }
}
