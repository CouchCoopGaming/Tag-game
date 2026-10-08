using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual life on top of the poses that already play. Idle looks around,
    /// a crouch walk keeps its feet, a slide reads as a slide, a sprint pumps
    /// harder, and a hit flinches on the stagger clock. Nothing here writes
    /// speed, the capsule, or a new verb.
    /// </summary>
    public static class BodyLife
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;

        public const float WalkSpeed = 6.9f;
        public const float SprintSpeed = 13.8f;
        public const float CrouchSpeed = 3.68f;

        public const float LookPeriod = 4.8f;
        public const float LookWindow = 0.30f;
        public const float LookYaw = 14f;
        /// <summary>It stands a little taller. A runner keeps the chest ready.</summary>
        public const float ItChest = 6f;
        public const float RunnerChest = -4f;

        public const float SlideLean = 8f;
        public const float SlideHandDrag = 16f;
        public const float SlideHead = -8f;

        public const float PumpAtSprint = 0.35f;

        public const float FlinchHead = -18f;
        public const float FlinchSpine = -10f;

        public const float ScrabbleAmp = 18f;
        public const float EntryPlantSeconds = 0.12f;
        public const float EntryFoot = 22f;
        public const float ModeBlendSeconds = 0.12f;

        /// <summary>Mostly still. Once a period, the head turns and comes back.</summary>
        public static float LookYawAt(float time)
        {
            if (time < 0f) time = 0f;
            float period = LookPeriod;
            float u = time - period * (float)System.Math.Floor(time / period);
            float start = 1f - LookWindow;
            if (u < start) return 0f;
            float w = (u - start) / LookWindow;
            if (w < 0f) w = 0f;
            if (w > 1f) w = 1f;
            float s = Mathf.Sin(w * 3.14159265f);
            int lap = (int)System.Math.Floor(time / period);
            float sign = (lap & 1) == 0 ? 1f : -1f;
            return s * LookYaw * sign;
        }

        public static float PostureChest(bool isIt)
        {
            return isIt ? ItChest : RunnerChest;
        }

        /// <summary>Meters the crouch sole travels in half a step. The short crouch reach.</summary>
        public static float CrouchFootTravel()
        {
            float reach = CrouchPose.WalkThighReach * Mathf.Deg2Rad;
            float trail = CrouchPose.WalkThighTrail * Mathf.Deg2Rad;
            float meters = GaitBlend.LegLength * (Mathf.Sin(reach) + Mathf.Sin(trail));
            return meters < 0.08f ? 0.08f : meters;
        }

        /// <summary>Radians per second so a crouch at <see cref="CrouchSpeed"/> does not skate.</summary>
        public static float CrouchCadence(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            return s * 3.14159265f / CrouchFootTravel();
        }

        public static float CrouchSlip(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            float cadence = CrouchCadence(s);
            if (cadence < 0.05f || s <= 0.05f) return 0f;
            float body = s * 3.14159265f / cadence;
            if (body < 0.001f) return 0f;
            float slip = (body - CrouchFootTravel()) / body;
            return slip < 0f ? 0f : slip;
        }

        public static void SlideMotion(float phase, out float lean, out float hand, out float head)
        {
            float drag = 0.65f + 0.35f * Mathf.Sin(phase * 7f);
            if (drag < 0f) drag = 0f;
            lean = SlideLean;
            hand = SlideHandDrag * drag;
            head = SlideHead;
        }

        /// <summary>1 at walk and below. 1.35 at sprint. The stride amplitude scales with speed.</summary>
        public static float ArmPump(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            if (s <= WalkSpeed) return 1f;
            float t = (s - WalkSpeed) / (SprintSpeed - WalkSpeed);
            if (t > 1f) t = 1f;
            return 1f + PumpAtSprint * t;
        }

        /// <summary>Same clock as the stagger pose. 0 outside the quarter second.</summary>
        public static float FlinchWeight(float age)
        {
            return PunchStaggerPose.Weight(age);
        }

        public static void Scrabble(float phase, float slip01, out float handL, out float handR, out float footL, out float footR)
        {
            float w = slip01 < 0f ? 0f : (slip01 > 1f ? 1f : slip01);
            float s = Mathf.Sin(phase * 3.2f);
            float c = Mathf.Cos(phase * 3.2f);
            float a = ScrabbleAmp * w;
            handL = s * a;
            handR = -s * a;
            footL = c * a * 0.65f;
            footR = -c * a * 0.65f;
        }

        /// <summary>1 on the frame the wall run starts, 0 after the plant window.</summary>
        public static float EntryPlant(float age)
        {
            if (age < 0f || age >= EntryPlantSeconds) return 0f;
            float u = age / EntryPlantSeconds;
            return 1f - u * u;
        }

        public static float ModeBlend(float age)
        {
            float u = 1f;
            if (ModeBlendSeconds > 0.0001f)
                u = age / ModeBlendSeconds;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return PoseHandoff.Ease(u);
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (Mathf.Abs(CrouchSpeed - 3.68f) > 0.001f) return false;
            if (Mathf.Abs(WalkSpeed - 6.9f) > 0.001f || Mathf.Abs(SprintSpeed - 13.8f) > 0.001f) return false;
            if (CrouchSlip(CrouchSpeed) > 0.02f) return false;
            if (CrouchCadence(CrouchSpeed) < 8f) return false;
            if (Mathf.Abs(ArmPump(WalkSpeed) - 1f) > 0.001f) return false;
            if (ArmPump(SprintSpeed) < 1.3f || ArmPump(SprintSpeed) > 1.4f) return false;
            if (ArmPump(0f) < 0.99f) return false;
            if (Mathf.Abs(LookYawAt(0f)) > 0.001f) return false;
            float look = LookYawAt(LookPeriod * 0.85f + LookPeriod * (1f - LookWindow) * 0f);
            if (look < 0f) look = -look;
            if (LookYawAt(LookPeriod * 0.1f) > 0.001f) return false;
            if (Mathf.Abs(PostureChest(true) - ItChest) > 0.001f) return false;
            if (Mathf.Abs(PostureChest(false) - RunnerChest) > 0.001f) return false;
            if (!(ItChest > 0f && RunnerChest < 0f)) return false;
            SlideMotion(0.4f, out float lean, out float hand, out float head);
            if (lean < 4f || hand < 4f || head > -4f) return false;
            if (Mathf.Abs(FlinchWeight(0f)) > 0.001f) return false;
            if (FlinchWeight(PunchStaggerPose.RiseSeconds * 0.5f) < 0.4f) return false;
            if (FlinchWeight(PunchStaggerPose.Duration) > 0.001f) return false;
            Scrabble(0.4f, 1f, out float hL, out float hR, out float fL, out float fR);
            if (Mathf.Abs(hL + hR) > 0.05f) return false;
            if (Mathf.Abs(hL) < 2f || Mathf.Abs(fL) < 1f) return false;
            if (Mathf.Abs(EntryPlant(0f) - 1f) > 0.001f) return false;
            if (EntryPlant(EntryPlantSeconds) > 0.001f) return false;
            if (EntryPlant(-0.01f) > 0.001f) return false;
            if (Mathf.Abs(ModeBlend(0f)) > 0.001f) return false;
            if (Mathf.Abs(ModeBlend(ModeBlendSeconds) - 1f) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            return "body-life"
                + " crouchSlip=" + CrouchSlip(CrouchSpeed).ToString("0.000")
                + " crouchCadence=" + CrouchCadence(CrouchSpeed).ToString("0.00")
                + " pump=" + ArmPump(SprintSpeed).ToString("0.00")
                + " look=" + LookYaw.ToString("0")
                + " itChest=" + ItChest.ToString("0")
                + " slideHand=" + SlideHandDrag.ToString("0")
                + " flinch=" + FlinchHead.ToString("0")
                + " scrabble=" + ScrabbleAmp.ToString("0")
                + " entry=" + EntryFoot.ToString("0")
                + " modeBlend=" + ModeBlendSeconds.ToString("0.00")
                + " gameplayDelay=0 rootMotion=0";
        }
    }
}
