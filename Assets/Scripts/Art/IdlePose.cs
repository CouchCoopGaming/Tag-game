using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual alive-idle only. Grounded and under the gait idle gate, the hips
    /// shift weight side to side, the loaded knee settles, and the chest and
    /// shoulders take a quiet breath. A walk into GaitBlend fades this off.
    /// Pivot, crouch, and Become-It own the bones instead. No root motion.
    /// </summary>
    public static class IdlePose
    {
        public const bool RootMotion = false;

        /// <summary>Off as soon as walk speed builds into the gait. The gait wins.</summary>
        public const float FadeSeconds = 0.12f;
        /// <summary>Tracks the small shifts. The idle default slew already can.</summary>
        public const float Slew = 24f;

        /// <summary>Radians per second. One side-to-side cycle stays slow.</summary>
        public const float ShiftRate = 0.72f;
        /// <summary>Radians per second. Quieter and slower than a pant.</summary>
        public const float BreathRate = 1.55f;

        /// <summary>Hip roll, degrees. Positive loads the left leg.</summary>
        public const float HipRoll = 3.2f;
        /// <summary>Chest rolls the other way, less, so the head stays over the feet.</summary>
        public const float ChestCounter = 1.4f;
        /// <summary>Spine pitch of the breath.</summary>
        public const float ChestBreath = 1.6f;
        /// <summary>Both shoulders rise and fall with the breath.</summary>
        public const float ShoulderBreath = 2.6f;
        /// <summary>Head pitches against the chest so the face does not pump.</summary>
        public const float HeadBreath = -0.7f;

        /// <summary>Both knees stay softly bent.</summary>
        public const float KneeRest = 4f;
        /// <summary>Extra bend on the loaded knee.</summary>
        public const float KneeSettle = 5.5f;
        /// <summary>Loaded thigh pitches a little so the foot stays under the hip.</summary>
        public const float ThighSettle = 2.2f;

        public struct Sample
        {
            public float HipRoll, ChestRoll, ChestPitch, Shoulder, HeadPitch;
            public float ThighL, ThighR, KneeL, KneeR, FootL, FootR;
        }

        /// <summary>1 at and under the idle gate. 0 as soon as speed builds into the gait.</summary>
        public static float SpeedWeight(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            return s <= GaitBlend.IdleGate ? 1f : 0f;
        }

        /// <summary>
        /// 0 when pivot, crouch, or Become-It owns the body. Those poses win.
        /// A trace of pivot does not count, so the idle can leave as the pivot arrives.
        /// </summary>
        public static float Yield(float pivot01, float crouch01, float become01)
        {
            float block = pivot01;
            if (crouch01 > block) block = crouch01;
            if (become01 > block) block = become01;
            if (block > 0.02f) return 0f;
            return 1f;
        }

        /// <summary>1 only while grounded-idle speed and no other pose owns the body.</summary>
        public static float Weight(float planarSpeed, float pivot01, float crouch01, float become01)
        {
            return SpeedWeight(planarSpeed) * Yield(pivot01, crouch01, become01);
        }

        /// <summary>
        /// shiftPhase drives the weight. breathPhase drives the chest and shoulders.
        /// Knees are negative. Feet cancel the added thigh and knee so the sole stays level.
        /// </summary>
        public static Sample At(float shiftPhase, float breathPhase)
        {
            float shift = Mathf.Sin(shiftPhase);
            float breath = Mathf.Sin(breathPhase);
            if (shift > 1f) shift = 1f;
            if (shift < -1f) shift = -1f;
            if (breath > 1f) breath = 1f;
            if (breath < -1f) breath = -1f;
            float loadL = shift > 0f ? shift : 0f;
            float loadR = shift < 0f ? -shift : 0f;
            float thighL = loadL * ThighSettle;
            float thighR = loadR * ThighSettle;
            float kneeL = -(KneeRest + loadL * KneeSettle);
            float kneeR = -(KneeRest + loadR * KneeSettle);
            return new Sample
            {
                HipRoll = shift * HipRoll,
                ChestRoll = -shift * ChestCounter,
                ChestPitch = breath * ChestBreath,
                Shoulder = breath * ShoulderBreath,
                HeadPitch = breath * HeadBreath,
                ThighL = thighL,
                ThighR = thighR,
                KneeL = kneeL,
                KneeR = kneeR,
                FootL = GaitBlend.SoleLevelDeg(thighL, kneeL),
                FootR = GaitBlend.SoleLevelDeg(thighR, kneeR),
            };
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(FadeSeconds - 0.12f) > 0.001f) return false;
            if (FadeSeconds < 0.10f || FadeSeconds > 0.14f) return false;
            if (Slew < 18f || Slew > 36f) return false;
            if (Mathf.Abs(GaitBlend.IdleGate - 0.35f) > 0.001f) return false;
            if (ShiftRate < 0.45f || ShiftRate > 1.10f) return false;
            if (BreathRate < 1.10f || BreathRate > 2.00f) return false;
            if (BreathRate <= ShiftRate) return false;
            if (HipRoll < 2.2f || HipRoll > 4.0f) return false;
            if (ChestCounter < 0.8f || ChestCounter >= HipRoll) return false;
            if (ChestBreath < 1.0f || ChestBreath > 2.2f) return false;
            if (ShoulderBreath < 1.6f || ShoulderBreath > 3.5f) return false;
            if (HeadBreath >= 0f || HeadBreath < -1.4f) return false;
            if (KneeRest < 2f || KneeSettle < 3f) return false;
            if (KneeRest + KneeSettle > 12f) return false;
            if (ThighSettle < 1f || ThighSettle >= KneeRest) return false;

            if (Mathf.Abs(SpeedWeight(0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(SpeedWeight(GaitBlend.IdleGate) - 1f) > 0.0001f) return false;
            if (SpeedWeight(GaitBlend.IdleGate + 0.01f) > 0.0001f) return false;
            if (SpeedWeight(GaitBlend.WalkSpeed) > 0.0001f) return false;
            if (SpeedWeight(GaitBlend.SprintSpeed) > 0.0001f) return false;
            if (Weight(GaitBlend.IdleGate + 0.01f, 0f, 0f, 0f) > 0.0001f) return false;

            if (Mathf.Abs(Yield(0f, 0f, 0f) - 1f) > 0.0001f) return false;
            if (Yield(1f, 0f, 0f) > 0.0001f) return false;
            if (Yield(0f, 1f, 0f) > 0.0001f) return false;
            if (Yield(0f, 0f, 1f) > 0.0001f) return false;
            if (Yield(0.5f, 0f, 0f) > 0.0001f) return false;
            if (Yield(0f, 0.5f, 0f) > 0.0001f) return false;
            if (Yield(0f, 0f, 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(Yield(0.02f, 0f, 0f) - 1f) > 0.0001f) return false;

            if (Mathf.Abs(Weight(0f, 0f, 0f, 0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Weight(GaitBlend.IdleGate, 0f, 0f, 0f) - 1f) > 0.0001f) return false;
            if (Weight(0f, 1f, 0f, 0f) > 0.0001f) return false;
            if (Weight(0f, 0f, 1f, 0f) > 0.0001f) return false;
            if (Weight(0f, 0f, 0f, 1f) > 0.0001f) return false;
            if (Weight(GaitBlend.WalkSpeed, 0f, 0f, 0f) > 0.0001f) return false;

            const float half = 1.5707963f;
            Sample rest = At(0f, 0f);
            if (Mathf.Abs(rest.HipRoll) > 0.0001f || Mathf.Abs(rest.ChestRoll) > 0.0001f) return false;
            if (Mathf.Abs(rest.ChestPitch) > 0.0001f || Mathf.Abs(rest.Shoulder) > 0.0001f) return false;
            if (Mathf.Abs(rest.KneeL - rest.KneeR) > 0.0001f) return false;
            if (Mathf.Abs(rest.KneeL - (-KneeRest)) > 0.01f) return false;

            Sample left = At(half, 0f);
            if (Mathf.Abs(left.HipRoll - HipRoll) > 0.02f) return false;
            if (Mathf.Abs(left.ChestRoll - (-ChestCounter)) > 0.02f) return false;
            if (left.KneeL >= left.KneeR) return false;
            if (Mathf.Abs(left.KneeL - (-(KneeRest + KneeSettle))) > 0.02f) return false;
            if (Mathf.Abs(left.KneeR - (-KneeRest)) > 0.02f) return false;
            if (Mathf.Abs(left.ThighL - ThighSettle) > 0.02f) return false;
            if (left.ThighR > 0.0001f) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(left.ThighL, left.KneeL, left.FootL)) > 0.05f) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(left.ThighR, left.KneeR, left.FootR)) > 0.05f) return false;

            Sample right = At(-half, half);
            if (Mathf.Abs(right.HipRoll - (-HipRoll)) > 0.02f) return false;
            if (right.KneeR >= right.KneeL) return false;
            if (Mathf.Abs(right.ChestPitch - ChestBreath) > 0.02f) return false;
            if (Mathf.Abs(right.Shoulder - ShoulderBreath) > 0.02f) return false;
            if (Mathf.Abs(right.HeadPitch - HeadBreath) > 0.02f) return false;
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(right.ThighR, right.KneeR, right.FootR)) > 0.05f) return false;

            Sample inhale = At(0f, half);
            if (Mathf.Abs(inhale.HipRoll) > 0.0001f) return false;
            if (inhale.ChestPitch <= 0f || inhale.Shoulder <= 0f) return false;
            if (inhale.HeadPitch >= 0f) return false;
            return true;
        }

        public static string ProofLine()
        {
            float idle = Weight(0f, 0f, 0f, 0f);
            float walk = Weight(GaitBlend.WalkSpeed, 0f, 0f, 0f);
            float pivot = Weight(0f, 1f, 0f, 0f);
            float crouch = Weight(0f, 0f, 1f, 0f);
            float become = Weight(0f, 0f, 0f, 1f);
            const float half = 1.5707963f;
            Sample side = At(half, half);
            return "idle pose"
                + " fade=" + FadeSeconds.ToString("0.00")
                + " idle=" + idle.ToString("0.00")
                + " walk=" + walk.ToString("0.00")
                + " pivot=" + pivot.ToString("0.00")
                + " crouch=" + crouch.ToString("0.00")
                + " become=" + become.ToString("0.00")
                + " hip=" + side.HipRoll.ToString("0.0")
                + " chest=" + side.ChestPitch.ToString("0.0")
                + " shoulder=" + side.Shoulder.ToString("0.0")
                + " knee=" + (-side.KneeL).ToString("0.0")
                + " shift=" + ShiftRate.ToString("0.00")
                + " breath=" + BreathRate.ToString("0.00")
                + " gate=grounded speed<=" + GaitBlend.IdleGate.ToString("0.00")
                + " fade " + FadeSeconds.ToString("0.00")
                + "s into GaitBlend"
                + "; weight shift; knee settle; chest/shoulder breath"
                + "; yields pivot+crouch+become"
                + "; rootMotion=0";
        }
    }
}
