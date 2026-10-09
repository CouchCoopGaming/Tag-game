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
            public float ThighYawL, ThighYawR;
            public float ThighRollL, ThighRollR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head;
            public float FootL, FootR;
            public float Drop;
        }

        /// <summary>
        /// Hips-bone drop for the seated land. The squash bob still uses SoftDrop and HardDrop.
        /// </summary>
        public const float BoneDrop = 0.491f;

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

        /// <summary>
        /// Deep absorb. Hip 35 over a spine of 15, chest forward, arms out.
        /// Thighs spread and roll so the spine stays out of the thigh. The hips bone is 49.1 cm down.
        /// HardThigh and HardKnee stay the brace constants.
        /// </summary>
        static Sample Seated()
        {
            return new Sample
            {
                ThighL = 116f,
                ThighR = 116f,
                KneeL = -96f,
                KneeR = -96f,
                ThighYawL = -56f,
                ThighYawR = 56f,
                ThighRollL = 20f,
                ThighRollR = -20f,
                ArmPitchL = 22f,
                ArmPitchR = 20f,
                ArmYawL = -46f,
                ArmYawR = 46f,
                ArmRollL = 6f,
                ArmRollR = -6f,
                ElbowL = -50f,
                ElbowR = -46f,
                Hip = 35f,
                Spine = 15f,
                Head = -20f,
                FootL = 16f,
                FootR = 16f,
                Drop = BoneDrop,
            };
        }

        /// <summary>Both feet down. Hands stay off the chest.</summary>
        public static Sample Soft()
        {
            return Seated();
        }

        /// <summary>Same sit as the soft land. handLeft used to plant a palm, which put the thigh through the spine.</summary>
        public static Sample Hard(bool handLeft)
        {
            Sample s = Seated();
            if (!handLeft)
            {
                s.ArmPitchL = 20f;
                s.ArmPitchR = 22f;
                s.ArmYawL = 46f;
                s.ArmYawR = -46f;
                s.ArmRollL = -6f;
                s.ArmRollR = 6f;
                s.ElbowL = -46f;
                s.ElbowR = -50f;
            }
            return s;
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
            if (soft.KneeL > -45f || soft.KneeR > -45f) return false;
            if (Mathf.Abs(soft.KneeL - soft.KneeR) > 0.01f) return false;
            if (soft.ThighL < 45f || soft.ThighR < 45f) return false;
            if (soft.ThighYawL > -15f || soft.ThighYawR < 15f) return false;
            if (soft.Drop < 0.20f) return false;
            if (soft.Hip < 35f || soft.Spine < 15f) return false;
            if (soft.Hip / soft.Spine < 1.5f) return false;
            if (soft.ArmPitchL > 24f || soft.ArmPitchR > 24f) return false;

            Sample hardL = Hard(true);
            Sample hardR = Hard(false);
            if (hardL.KneeL > -45f || hardL.ThighL < 45f) return false;
            if (hardL.Drop < 0.20f) return false;
            if (hardL.ThighYawL > -15f || hardL.ThighYawR < 15f) return false;
            if (hardL.ArmPitchL > 24f || hardL.ArmPitchR > 24f) return false;
            if (hardR.ArmPitchR > 24f || hardR.ArmPitchL > 24f) return false;
            if (Mathf.Abs(LandPose.HardThigh - 74f) > 0.01f) return false;
            if (Mathf.Abs(LandPose.HardKnee - -125f) > 0.01f) return false;
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
                + " softDrop=" + soft.Drop.ToString("0.00")
                + " hardKnee=" + hard.KneeL.ToString("0")
                + " hardThigh=" + hard.ThighL.ToString("0")
                + " hardHand=" + hard.ArmPitchL.ToString("0")
                + " hardFree=" + hard.ArmPitchR.ToString("0")
                + " hardDrop=" + hard.Drop.ToString("0.00")
                + " gate=impact>=" + SoftImpact.ToString("0") + " soft, impact>=" + HardImpact.ToString("0") + " hard"
                + " matches AudioBus LandSoft/LandHard"
                + " pose does not delay control"
                + " shared=DummyLocomotor"
                + " rootMotion=0";
        }
    }
}
