using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual landing only. A soft step-down bends the knees. A hard impact
    /// drops into a crouch and puts one hand on the ground. The motor, the
    /// land-stun timer, and control are not written. The absorb weights in
    /// the locomotor still own how fast the pose arrives and leaves.
    /// </summary>
    public static class LandPose
    {
        public const bool RootMotion = false;

        /// <summary>The pose never holds input. Land stun stays the motor's timer.</summary>
        public const float ControlDelay = 0f;

        /// <summary>Same floor as the motor's soft-land audio branch.</summary>
        public const float SoftImpact = 5f;
        /// <summary>Same floor as MovementConfig.landStunSpeed and the hard-land audio.</summary>
        public const float HardImpact = 28f;

        /// <summary>Mesh drop, meters. The capsule does not move.</summary>
        public const float SoftDrop = 0.02f;
        public const float HardDrop = 0.50f;

        public const float SoftThigh = 18f;
        public const float SoftKnee = -26f;
        public const float SoftHip = 8f;
        public const float SoftSpine = 6f;
        public const float SoftHead = -4f;
        public const float SoftArmPitch = -16f;
        public const float SoftArmYaw = 12f;
        public const float SoftElbow = -14f;

        public const float HardThigh = 74f;
        public const float HardKnee = -125f;
        public const float HardHip = 46f;
        public const float HardSpine = 28f;
        public const float HardHead = 8f;
        public const float HardHandPitch = 18f;
        public const float HardHandYaw = 16f;
        public const float HardHandElbow = -16f;
        public const float HardFreePitch = -30f;
        public const float HardFreeYaw = 12f;
        public const float HardFreeElbow = -40f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head;
            public float FootL, FootR;
            public float Drop;
        }

        /// <summary>Impact at or above land-stun speed. Matches the hard audio gate.</summary>
        public static bool IsHard(float impact)
        {
            return impact >= HardImpact;
        }

        /// <summary>Impact at or above the soft audio floor, and under the hard gate.</summary>
        public static bool IsSoft(float impact)
        {
            return impact >= SoftImpact && impact < HardImpact;
        }

        /// <summary>Small knee bend. Both hands stay up. Soles level.</summary>
        public static Sample Soft()
        {
            float foot = GaitBlend.SoleLevelDeg(SoftThigh, SoftKnee);
            return new Sample
            {
                ThighL = SoftThigh,
                ThighR = SoftThigh,
                KneeL = SoftKnee,
                KneeR = SoftKnee,
                ArmPitchL = SoftArmPitch,
                ArmPitchR = SoftArmPitch,
                ArmYawL = SoftArmYaw,
                ArmYawR = -SoftArmYaw,
                ElbowL = SoftElbow,
                ElbowR = SoftElbow,
                Hip = SoftHip,
                Spine = SoftSpine,
                Head = SoftHead,
                FootL = foot,
                FootR = foot,
                Drop = SoftDrop,
            };
        }

        /// <summary>Deep crouch. handLeft plants that hand. The other hand stays up.</summary>
        public static Sample Hard(bool handLeft)
        {
            float foot = GaitBlend.SoleLevelDeg(HardThigh, HardKnee);
            float pitchL = handLeft ? HardHandPitch : HardFreePitch;
            float pitchR = handLeft ? HardFreePitch : HardHandPitch;
            float yawL = handLeft ? HardHandYaw : HardFreeYaw;
            float yawR = handLeft ? -HardFreeYaw : -HardHandYaw;
            float elbowL = handLeft ? HardHandElbow : HardFreeElbow;
            float elbowR = handLeft ? HardFreeElbow : HardHandElbow;
            return new Sample
            {
                ThighL = HardThigh,
                ThighR = HardThigh,
                KneeL = HardKnee,
                KneeR = HardKnee,
                ArmPitchL = pitchL,
                ArmPitchR = pitchR,
                ArmYawL = yawL,
                ArmYawR = yawR,
                ElbowL = elbowL,
                ElbowR = elbowR,
                Hip = HardHip,
                Spine = HardSpine,
                Head = HardHead,
                FootL = foot,
                FootR = foot,
                Drop = HardDrop,
            };
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (ControlDelay > 0.0001f) return false;
            if (Mathf.Abs(SoftImpact - 5f) > 0.001f) return false;
            if (Mathf.Abs(HardImpact - 28f) > 0.001f) return false;
            if (HardImpact <= SoftImpact) return false;
            if (HardDrop <= SoftDrop) return false;
            if (IsSoft(4.9f) || IsHard(4.9f)) return false;
            if (!IsSoft(5f) || IsHard(5f)) return false;
            if (!IsSoft(27.9f) || IsHard(27.9f)) return false;
            if (IsSoft(28f) || !IsHard(28f)) return false;
            if (IsSoft(40f) || !IsHard(40f)) return false;

            Sample soft = Soft();
            if (soft.KneeL > -12f || soft.KneeL < -40f) return false;
            if (Mathf.Abs(soft.KneeL - soft.KneeR) > 0.01f) return false;
            if (soft.ThighL > 30f || soft.ThighR > 30f) return false;
            if (soft.ArmPitchL >= 0f || soft.ArmPitchR >= 0f) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(soft.ThighL, soft.KneeL, soft.FootL)) > 0.05f) return false;
            if (soft.Drop > 0.06f) return false;

            Sample hardL = Hard(true);
            Sample hardR = Hard(false);
            if (hardL.KneeL > -100f || hardL.ThighL < 60f) return false;
            if (hardL.ArmPitchL < 10f || hardL.ArmPitchR > -10f) return false;
            if (hardR.ArmPitchR < 10f || hardR.ArmPitchL > -10f) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(hardL.ThighL, hardL.KneeL, hardL.FootL)) > 0.05f) return false;
            if (hardL.Hip < 30f || hardL.Spine < 20f) return false;
            if (Mathf.Abs(hardL.KneeL - soft.KneeL) < 40f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample soft = Soft();
            Sample hard = Hard(true);
            return "land pose"
                + " softImpact=" + SoftImpact.ToString("0")
                + " hardImpact=" + HardImpact.ToString("0")
                + " controlDelay=" + ControlDelay.ToString("0.00")
                + " softKnee=" + soft.KneeL.ToString("0")
                + " softThigh=" + soft.ThighL.ToString("0")
                + " softDrop=" + SoftDrop.ToString("0.00")
                + " hardKnee=" + hard.KneeL.ToString("0")
                + " hardThigh=" + hard.ThighL.ToString("0")
                + " hardHand=" + hard.ArmPitchL.ToString("0")
                + " hardFree=" + hard.ArmPitchR.ToString("0")
                + " hardDrop=" + HardDrop.ToString("0.00")
                + " gate=impact>=" + SoftImpact.ToString("0") + " soft, impact>=" + HardImpact.ToString("0") + " hard"
                + " matches AudioBus LandSoft/LandHard"
                + " pose does not delay control"
                + " shared=DummyLocomotor"
                + " rootMotion=0";
        }
    }
}
