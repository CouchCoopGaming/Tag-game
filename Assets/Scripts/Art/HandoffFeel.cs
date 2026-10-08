using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual handoffs only. Land-to-run, land-to-roll, wall-run-to-jump,
    /// and cling-to-drop. The roll gate is 65% of terminal. Nothing here
    /// writes a Move, a timer, or the root.
    /// </summary>
    public static class HandoffFeel
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;
        public const float Dt = 1f / 60f;
        public const float SquashRate = 3.1f;
        public const float RollShare = 0.65f;
        public const float RollSpeed = RollShare * 56.16f;
        public const float WallSeconds = 0.12f;

        public const float RollThigh = 62f;
        public const float RollKnee = -108f;
        public const float RollHip = 28f;
        public const float RollSpine = 36f;
        public const float RollHead = 14f;
        public const float RollArm = -20f;
        public const float RollElbow = -70f;
        public const float RunHip = 6f;
        public const float RunSpine = 8f;

        public struct Shot
        {
            public float ArmL, ArmR, ElbL, ElbR, ThL, ThR, KnL, KnR, Spine, Hip, Head, Lean;
        }

        public static bool Rolls(float impact)
        {
            return impact >= RollSpeed - 0.001f;
        }

        /// <summary>1 while the absorb is full. 0 once the squash is gone. Raised, so the first step is small.</summary>
        public static float Release(float squash)
        {
            float k = squash < 0f ? 0f : (squash > 1f ? 1f : squash);
            return 1f - Raised(1f - k);
        }

        /// <summary>0 on the stride, 1 in the middle of the roll, 0 again as the run returns.</summary>
        public static float RollWeight(float u)
        {
            if (u <= 0f || u >= 1f) return 0f;
            if (u < 0.5f) return Raised(u / 0.5f);
            return Raised((1f - u) / 0.5f);
        }

        /// <summary>Single arc across the push-off. 0 is the wall run. 1 is the balance tuck.</summary>
        public static float WallOpen(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= WallSeconds) return 1f;
            return Raised(age / WallSeconds);
        }

        public static Shot ShotAt(int kind, int frame, bool after)
        {
            float u = frame / 7f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            if (kind == 0) return RunShot(u, after);
            if (kind == 1) return RollShot(u, after);
            if (kind == 2) return WallShot(u, after);
            return ClingShot(u, after);
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "handoff"
                + " run=" + Pair(RunStep(false), RunStep(true), c)
                + " roll=" + Pair(RollStep(false), RollStep(true), c)
                + " wall=" + Pair(WallStep(false), WallStep(true), c)
                + " cling=" + Pair(ClingStep(false), ClingStep(true), c)
                + " rollAt=" + RollSpeed.ToString("0.0", c)
                + " gameplayDelay=0 rootMotion=0";
        }

        public static bool Holds()
        {
            bool motion = RootMotion;
            float delay = GameplayDelay;
            if (motion || delay != 0f) return false;
            if (Mathf.Abs(RollShare - 0.65f) > 0.001f) return false;
            if (Mathf.Abs(RollSpeed - 36.504f) > 0.01f) return false;
            if (!Rolls(RollSpeed) || Rolls(RollSpeed - 0.05f)) return false;
            if (Mathf.Abs(Release(1f) - 1f) > 0.0001f) return false;
            if (Release(0f) > 0.0001f) return false;
            if (RollWeight(0f) > 0.0001f || RollWeight(1f) > 0.0001f) return false;
            if (RollWeight(0.5f) < 0.99f) return false;
            if (WallOpen(0f) > 0.0001f) return false;
            if (Mathf.Abs(WallOpen(WallSeconds) - 1f) > 0.0001f) return false;
            if (RunStep(true) >= RunStep(false) * 0.55f) return false;
            if (RollStep(true) >= RollStep(false) * 0.55f) return false;
            if (WallStep(true) >= WallStep(false) * 0.55f) return false;
            if (ClingStep(true) >= ClingStep(false) * 0.55f) return false;
            if (RunStep(true) <= 0.05f || RollStep(true) <= 0.05f) return false;
            if (WallStep(true) <= 0.05f || ClingStep(true) <= 0.05f) return false;
            return true;
        }

        static Shot RunShot(float u, bool after)
        {
            float k = 1f - u;
            float w = after ? Release(k) : k * k * k * k;
            GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
            return new Shot
            {
                ThL = Lerp(legs.ThighL, LandPose.HardThigh, w),
                ThR = Lerp(legs.ThighR, LandPose.HardThigh, w),
                KnL = Lerp(legs.KneeL, LandPose.HardKnee, w),
                KnR = Lerp(legs.KneeR, LandPose.HardKnee, w),
                Hip = Lerp(RunHip, LandPose.HardHip, w),
                Spine = Lerp(RunSpine, LandPose.HardSpine, w),
                Head = Lerp(0f, LandPose.HardHead, w),
                ArmL = Lerp(-36f, LandPose.HardHandPitch, w),
                ArmR = Lerp(28f, LandPose.HardFreePitch, w),
                ElbL = Lerp(-18f, LandPose.HardHandElbow, w),
                ElbR = Lerp(-24f, LandPose.HardFreeElbow, w),
            };
        }

        static Shot RollShot(float u, bool after)
        {
            float w = after ? RollWeight(u) : (u > 0.2f && u < 0.7f ? 1f : 0f);
            GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
            float thigh = Lerp(LandPose.HardThigh, legs.ThighL, u);
            return new Shot
            {
                ThL = Lerp(thigh, RollThigh, w),
                ThR = Lerp(thigh, RollThigh - 18f, w),
                KnL = Lerp(Lerp(LandPose.HardKnee, legs.KneeL, u), RollKnee, w),
                KnR = Lerp(Lerp(LandPose.HardKnee, legs.KneeR, u), RollKnee + 20f, w),
                Hip = Lerp(Lerp(LandPose.HardHip, RunHip, u), RollHip, w),
                Spine = Lerp(Lerp(LandPose.HardSpine, RunSpine, u), RollSpine, w),
                Head = Lerp(LandPose.HardHead, RollHead, w),
                ArmL = Lerp(-36f, RollArm, w),
                ArmR = Lerp(28f, RollArm, w),
                ElbL = Lerp(-18f, RollElbow, w),
                ElbR = Lerp(-24f, RollElbow + 16f, w),
                Lean = 22f * w,
            };
        }

        static Shot WallShot(float u, bool after)
        {
            float age = (WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds) * u;
            float w = after ? BodyLine.WallArc(age) : WallJumpPose.JumpWeight(age);
            float end = after ? AirFeel.ApexPitch : JumpPose.TuckArmPitch;
            if (!after && u > 0.92f) end = AirFeel.ApexPitch;
            float pitch = Lerp(WallJumpPose.PushPitch, end, !after && u > 0.92f ? 1f : w);
            WallPose.Sample run = WallPose.Run(1f, true);
            float from = run.ArmPitchL;
            float show = after ? Lerp(from, pitch, Raised(u)) : (u < 0.08f ? from : pitch);
            return new Shot
            {
                ArmL = show,
                ArmR = Lerp(run.ArmPitchR, AirFeel.ApexPitch, after ? Raised(u) : w),
                ElbL = Lerp(run.ElbowL, AirFeel.ApexElbow, after ? Raised(u) : w),
                ElbR = Lerp(run.ElbowR, AirFeel.ApexElbow, after ? Raised(u) : w),
                ThL = Lerp(run.ThighL, JumpPose.TuckThigh, after ? Raised(u) : w),
                ThR = Lerp(run.ThighR, JumpPose.TuckThigh - 6f, after ? Raised(u) : w),
                KnL = Lerp(run.KneeL, JumpPose.TuckKnee, after ? Raised(u) : w),
                KnR = Lerp(run.KneeR, JumpPose.TuckKnee, after ? Raised(u) : w),
                Spine = Lerp(run.Spine, AirFeel.ApexSpine, after ? Raised(u) : w),
                Hip = Lerp(run.Hip, AirFeel.ApexHip, after ? Raised(u) : w),
                Head = run.Head,
                Lean = Lerp(run.LeanZ, 0f, after ? Raised(u) : w),
            };
        }

        static Shot ClingShot(float u, bool after)
        {
            WallPose.Sample hold = WallPose.Hold();
            float w = after ? u : (u < 0.85f ? WallPose.Ease(u) : 1f);
            float endPitch = after ? AirFeel.BraceArmPitch : (u < 0.85f ? JumpPose.FallArmPitch : AirFeel.BraceArmPitch);
            float endYaw = after ? AirFeel.BraceArmYaw : (u < 0.85f ? JumpPose.FallArmYaw : AirFeel.BraceArmYaw);
            return new Shot
            {
                ArmL = Lerp(hold.ArmPitchL, endPitch, w),
                ArmR = Lerp(hold.ArmPitchR, endPitch, w),
                ElbL = Lerp(hold.ElbowL, AirFeel.BraceElbow, w),
                ElbR = Lerp(hold.ElbowR, AirFeel.BraceElbow, w),
                ThL = Lerp(hold.ThighL, AirFeel.BraceThigh, w),
                ThR = Lerp(hold.ThighR, AirFeel.BraceThigh, w),
                KnL = Lerp(hold.KneeL, AirFeel.BraceKnee, w),
                KnR = Lerp(hold.KneeR, AirFeel.BraceKnee, w),
                Spine = Lerp(hold.Spine, AirFeel.BraceSpine, w),
                Hip = Lerp(hold.Hip, AirFeel.BraceHip, w),
                Head = Lerp(hold.Head, AirFeel.BraceHead, w),
            };
        }

        static float RunStep(bool eased)
        {
            float prev = LandPose.HardHip;
            float peak = 0f;
            float k = 1f;
            int n = (int)(1f / SquashRate / Dt) + 3;
            for (int i = 0; i < n; i++)
            {
                k -= SquashRate * Dt;
                if (k < 0f) k = 0f;
                float w = eased ? Release(k) : k * k * k * k;
                float hip = Lerp(RunHip, LandPose.HardHip, w);
                peak = Max(peak, Abs(hip - prev));
                prev = hip;
                if (k <= 0f) break;
            }
            return peak;
        }

        static float RollStep(bool eased)
        {
            float prev = LandPose.HardThigh;
            float peak = 0f;
            int n = (int)(1f / SquashRate / Dt) + 3;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                float w = eased ? RollWeight(u) : (u > 0.15f && u < 0.2f ? 1f : 0f);
                GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
                float baseThigh = Lerp(LandPose.HardThigh, legs.ThighL, u);
                float thigh = Lerp(baseThigh, RollThigh, w);
                peak = Max(peak, Abs(thigh - prev));
                prev = thigh;
            }
            return peak;
        }

        static float WallStep(bool eased)
        {
            float prev = WallJumpPose.PushPitch;
            float peak = 0f;
            float total = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            int n = (int)(total / Dt + 1.5f);
            for (int i = 1; i <= n; i++)
            {
                float age = i * Dt;
                float w = eased ? BodyLine.WallArc(age) : WallJumpPose.JumpWeight(age);
                float end = eased ? AirFeel.ApexPitch : JumpPose.TuckArmPitch;
                float pitch = Lerp(WallJumpPose.PushPitch, end, w);
                peak = Max(peak, Abs(pitch - prev));
                prev = pitch;
            }
            if (!eased)
                peak = Max(peak, Abs(AirFeel.ApexPitch - prev));
            return peak;
        }

        static float ClingStep(bool eased)
        {
            WallPose.Sample hold = WallPose.Hold();
            float from = hold.ArmPitchL;
            float prev = from;
            float peak = 0f;
            int n = (int)(WallPose.ReleaseBlendSeconds / Dt + 1.5f);
            for (int i = 1; i <= n; i++)
            {
                float u = i / (float)n;
                float w = eased ? u : WallPose.Ease(u);
                float end = eased ? AirFeel.BraceArmPitch : JumpPose.FallArmPitch;
                float pitch = Lerp(from, end, w);
                peak = Max(peak, Abs(pitch - prev));
                prev = pitch;
            }
            if (!eased)
                peak = Max(peak, Abs(AirFeel.BraceArmPitch - prev));
            return peak;
        }

        static string Pair(float before, float after, CultureInfo c)
        {
            return before.ToString("0.0", c) + ">" + after.ToString("0.0", c);
        }

        static float Raised(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return 0.5f * (1f - Mathf.Cos(u * 3.14159265f));
        }

        static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        static float Abs(float v)
        {
            return v < 0f ? -v : v;
        }

        static float Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}
