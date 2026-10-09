using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual air only. The jump still leaves on the press frame. Takeoff,
    /// tuck, fall, hop, strafe, and coyote ease on the mesh. Coyote stays
    /// 0.10, terminal stays 56.16, and the landing pose is not replaced.
    /// </summary>
    public static class AirFeel
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;
        public const float Dt = 1f / 60f;

        public const float Walk = 6.9f;
        public const float Sprint = 13.8f;
        public const float CoyoteSeconds = 0.10f;
        public const float Terminal = 56.16f;
        public const float JumpSpeed = 24.7f;
        public const float RiseGravity = 22f;
        public const float FallMult = 1.62f;

        /// <summary>Jump share already on the press frame. Smaller than the old stride lead.</summary>
        public const float PushLead = 0.28f;
        public const float HopSeconds = 0.10f;
        public const float StrafeSeconds = 0.12f;
        /// <summary>Bank on the rise, once the push has started. The fall blend still wins later.</summary>
        public const float RiseBank = 0.28f;

        /// <summary>1 at a hop, this much of the tuck remains at a sprint.</summary>
        public const float TuckKeep = 0.42f;

        public const float BraceThigh = 54f;
        /// <summary>About 30 degrees of knee bend. Feet stay a little ahead of the hips.</summary>
        public const float BraceKnee = -32f;
        /// <summary>Thighs open so the knees are not locked together.</summary>
        public const float BraceLegYaw = 12f;
        /// <summary>Hanging pitch. The spread is the yaw. Elbows stay soft.</summary>
        public const float BraceArmPitch = 12f;
        public const float BraceArmYaw = 58f;
        public const float BraceElbow = -34f;
        /// <summary>Chest up. Positive spine folds forward.</summary>
        public const float BraceSpine = -6f;
        public const float BraceHip = 2f;
        /// <summary>
        /// Bone pitch while the brace is open. The sample thigh stays
        /// <see cref="BraceThigh"/> so the land-gap proof stays 20. Yaw on
        /// this rig twists the arm and does not raise it, so the spread is roll.
        /// </summary>
        public const float BraceShowThigh = 26f;
        public const float BraceShowHip = 8f;
        public const float BraceArmRoll = 14f;
        /// <summary>Eyes toward the landing. Positive pitches the head down.</summary>
        public const float BraceHead = 16f;
        /// <summary>Palm toward the ground on the hand bone.</summary>
        public const float BraceHand = -50f;

        /// <summary>Elbows out, hands at the chin. Docs/Storror is not on this branch.</summary>
        public const float ApexPitch = -45f;
        public const float ApexYaw = -35f;
        public const float ApexElbow = -34f;
        public const float ApexHip = 8f;
        public const float ApexSpine = -4f;

        public const float HopThigh = 24f;
        public const float HopAmp = 14f;
        public const float HopKnee = -36f;
        public const float HopRate = 9f;

        public struct Shot
        {
            public float ArmL, ArmR, ElbL, ElbR, ThL, ThR, KnL, KnR, Spine, Hip, Head, Lean;
        }

        /// <summary>Press frame is already pushing. The rest of the crouch eases out of that lead.</summary>
        public static float PushOpen(float age)
        {
            if (age <= 0f) return PushLead;
            if (age >= JumpPose.TakeoffSeconds) return 1f;
            return PushLead + (1f - PushLead) * Raised(age / JumpPose.TakeoffSeconds);
        }

        /// <summary>0 on the press frame, so a coyote jump leaves the run. 1 after the coyote window.</summary>
        public static float CoyoteOpen(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= CoyoteSeconds) return 1f;
            return Raised(age / CoyoteSeconds);
        }

        /// <summary>0 keeps the pose the hop left. The light cycle wins across the window.</summary>
        public static float HopOpen(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= HopSeconds) return 1f;
            return Raised(age / HopSeconds);
        }

        /// <summary>1 straight up. Shorter at sprint so a long jump stays long.</summary>
        public static float TuckScale(float planar)
        {
            return 1f - (1f - TuckKeep) * Inv(0f, Sprint, planar);
        }

        public static void ScaleTuck(ref JumpPose.Sample s, float planar, float vy, float age)
        {
            if (vy <= JumpPose.FallVy) return;
            float tuck = 1f - JumpPose.TakeoffWeight(age);
            float scale = Lerp(1f, TuckScale(planar), tuck);
            s.ThighL = Lerp(JumpPose.FallThigh, s.ThighL, scale);
            s.ThighR = Lerp(JumpPose.FallThigh, s.ThighR, scale);
            s.KneeL = Lerp(JumpPose.FallKnee, s.KneeL, scale);
            s.KneeR = Lerp(JumpPose.FallKnee, s.KneeR, scale);
        }

        /// <summary>
        /// Tuck arms stay out and forward. A hop opens them a little more.
        /// The authored overhead tuck is replaced, not stacked.
        /// </summary>
        public static void BalanceArms(ref JumpPose.Sample s, float planar, float vy, float age)
        {
            float tuck = (1f - JumpPose.Extend(vy)) * (1f - JumpPose.TakeoffWeight(age));
            float hop = 1f - Inv(0f, Sprint, planar);
            float yaw = ApexYaw - 6f * hop;
            float elbow = ApexElbow - 4f * hop;
            s.ArmYawL = Lerp(s.ArmYawL, yaw, tuck);
            s.ArmYawR = Lerp(s.ArmYawR, yaw, tuck);
            s.ArmPitchL = Lerp(s.ArmPitchL, ApexPitch, tuck);
            s.ArmPitchR = Lerp(s.ArmPitchR, ApexPitch, tuck);
            s.ElbowL = Lerp(s.ElbowL, elbow, tuck);
            s.ElbowR = Lerp(s.ElbowR, elbow, tuck);
            s.Hip = Lerp(s.Hip, ApexHip, tuck);
            s.Spine = Lerp(s.Spine, ApexSpine, tuck);
        }

        /// <summary>0 through the authored fall. 1 at terminal, part way to the hard land.</summary>
        public static float Brace01(float vy)
        {
            if (vy >= JumpPose.FallVy) return 0f;
            if (vy <= -Terminal) return 1f;
            float span = JumpPose.FallVy - (-Terminal);
            if (span < 0.001f) return 1f;
            return Raised((JumpPose.FallVy - vy) / span);
        }

        public static void Brace(ref JumpPose.Sample s, float vy)
        {
            float b = Brace01(vy);
            s.ThighL = Lerp(s.ThighL, BraceThigh, b);
            s.ThighR = Lerp(s.ThighR, BraceThigh, b);
            s.KneeL = Lerp(s.KneeL, BraceKnee, b);
            s.KneeR = Lerp(s.KneeR, BraceKnee, b);
            s.ArmPitchL = Lerp(s.ArmPitchL, BraceArmPitch, b);
            s.ArmPitchR = Lerp(s.ArmPitchR, BraceArmPitch, b);
            s.ArmYawL = Lerp(s.ArmYawL, BraceArmYaw, b);
            s.ArmYawR = Lerp(s.ArmYawR, BraceArmYaw, b);
            s.ElbowL = Lerp(s.ElbowL, BraceElbow, b);
            s.ElbowR = Lerp(s.ElbowR, BraceElbow, b);
            s.Spine = Lerp(s.Spine, BraceSpine, b);
            s.Hip = Lerp(s.Hip, BraceHip, b);
        }

        /// <summary>0 through the tuck. The brace looks down as it opens.</summary>
        public static float HeadPitch(float vy)
        {
            return Lerp(0f, BraceHead, Brace01(vy));
        }

        /// <summary>Light opposing steps. open 0 leaves the sample. open 1 is the cycle.</summary>
        public static void HopCycle(ref JumpPose.Sample s, float phase, float open)
        {
            float o = open < 0f ? 0f : (open > 1f ? 1f : open);
            float sin = Mathf.Sin(phase);
            float front = sin > 0f ? sin : 0f;
            float back = sin < 0f ? -sin : 0f;
            s.ThighL = Lerp(s.ThighL, HopThigh + HopAmp * sin, o);
            s.ThighR = Lerp(s.ThighR, HopThigh - HopAmp * sin, o);
            s.KneeL = Lerp(s.KneeL, HopKnee - 10f * front, o);
            s.KneeR = Lerp(s.KneeR, HopKnee - 10f * back, o);
        }

        /// <summary>Fall blend, or a small bank on the rise. The mesh eases whichever is larger.</summary>
        public static float StrafeOpen(float vy, float age, float fallBlend)
        {
            float rise = RiseBank * (1f - JumpPose.TakeoffWeight(age));
            float fall = fallBlend < 0f ? 0f : fallBlend;
            return fall > rise ? fall : rise;
        }

        public static JumpPose.Sample Blend(JumpPose.Sample a, JumpPose.Sample b, float t)
        {
            float u = t < 0f ? 0f : (t > 1f ? 1f : t);
            return new JumpPose.Sample
            {
                ThighL = Lerp(a.ThighL, b.ThighL, u),
                ThighR = Lerp(a.ThighR, b.ThighR, u),
                KneeL = Lerp(a.KneeL, b.KneeL, u),
                KneeR = Lerp(a.KneeR, b.KneeR, u),
                ArmPitchL = Lerp(a.ArmPitchL, b.ArmPitchL, u),
                ArmPitchR = Lerp(a.ArmPitchR, b.ArmPitchR, u),
                ArmYawL = Lerp(a.ArmYawL, b.ArmYawL, u),
                ArmYawR = Lerp(a.ArmYawR, b.ArmYawR, u),
                ArmRollL = Lerp(a.ArmRollL, b.ArmRollL, u),
                ArmRollR = Lerp(a.ArmRollR, b.ArmRollR, u),
                ThighYawL = Lerp(a.ThighYawL, b.ThighYawL, u),
                ThighYawR = Lerp(a.ThighYawR, b.ThighYawR, u),
                ElbowL = Lerp(a.ElbowL, b.ElbowL, u),
                ElbowR = Lerp(a.ElbowR, b.ElbowR, u),
                Hip = Lerp(a.Hip, b.Hip, u),
                Spine = Lerp(a.Spine, b.Spine, u),
            };
        }

        public static Shot ShotAt(int kind, int frame, bool after)
        {
            float u = frame / 7f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            if (kind == 0)
                return FromSample(TakeoffSample(JumpPose.TakeoffSeconds * u, after), 0f);
            if (kind == 1)
            {
                float vy = Lerp(10f, -4f, u);
                float planar = Sprint;
                return FromSample(AirSample(vy, 0.2f, planar, 0f, false, after, false), 0f);
            }
            if (kind == 2)
            {
                float vy = Lerp(JumpPose.FallVy, -Terminal, u);
                JumpPose.Sample s = AirSample(vy, 0.4f, Sprint, 0f, false, false, after);
                if (!after && frame >= 6)
                {
                    s.ThighL = LandPose.HardThigh;
                    s.ThighR = LandPose.HardThigh;
                    s.KneeL = LandPose.HardKnee;
                    s.KneeR = LandPose.HardKnee;
                }
                if (after && frame >= 7)
                {
                    s.ThighL = Lerp(BraceThigh, LandPose.HardThigh, 0.35f);
                    s.ThighR = s.ThighL;
                    s.KneeL = Lerp(BraceKnee, LandPose.HardKnee, 0.35f);
                    s.KneeR = s.KneeL;
                }
                Shot fallShot = FromSample(s, 0f);
                fallShot.Head = after ? HeadPitch(vy) : 12f;
                return fallShot;
            }
            if (kind == 3)
            {
                float age = HopSeconds * u;
                float phase = HopRate * age;
                JumpPose.Sample fall = AirSample(JumpPose.FallVy, 0.4f, Sprint, 0f, false, false, false);
                if (!after)
                {
                    PoseHandoff.HopTakeoff(age, out _, out float w);
                    JumpPose.Sample drive = JumpPose.At(JumpSpeed, 0f, true);
                    return FromSample(Blend(fall, drive, w), 0f);
                }
                JumpPose.Sample cyc = fall;
                HopCycle(ref cyc, phase, 1f);
                return FromSample(Blend(fall, cyc, HopOpen(age)), 0f);
            }
            if (kind == 4)
            {
                JumpPose.Sample body = AirSample(-16f, 0.4f, Sprint, 0f, false, true, true);
                float w = after ? StrafeEase(u) : (u < 0.15f ? 0f : 1f);
                Shot shot = FromSample(body, -AirStrafeLeanPose.Roll * w);
                shot.ArmL += AirStrafeLeanPose.ArmCounterPitch * w;
                shot.ArmR -= AirStrafeLeanPose.ArmCounterPitch * w;
                return shot;
            }
            float coyoteAge = CoyoteSeconds * u;
            return FromSample(TakeoffSample(coyoteAge, after ? false : true, true), 0f);
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "air-feel"
                + " takeoff=" + Pair(TakeoffStep(false), TakeoffStep(true), c)
                + " tuck=" + Pair(TuckStep(false), TuckStep(true), c)
                + " fall=" + Pair(FallStep(false), FallStep(true), c)
                + " hop=" + Pair(HopStep(false), HopStep(true), c)
                + " strafe=" + Pair(StrafeStep(false), StrafeStep(true), c)
                + " coyote=" + Pair(CoyoteStep(false), CoyoteStep(true), c)
                + " clear=" + ClearanceCm().ToString("0.0", c) + "cm"
                + " gameplayDelay=0 rootMotion=0";
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (Mathf.Abs(CoyoteSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(Terminal - 56.16f) > 0.001f) return false;
            if (Mathf.Abs(JumpSpeed - 24.7f) > 0.001f) return false;
            if (Mathf.Abs(RiseGravity - 22f) > 0.001f) return false;
            if (Mathf.Abs(FallMult - 1.62f) > 0.001f) return false;
            if (Mathf.Abs(JumpPose.ExtendAt(0f, 0f) - JumpPose.ExtendAt(0f, 24f)) > 0.0001f) return false;
            if (Mathf.Abs(JumpPose.TakeoffWeight(0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(JumpPose.FromStride(0f) - JumpPose.StrideLead) > 0.0001f) return false;
            if (AirStrafeLeanPose.FallBlend(JumpSpeed, 0.2f) > 0.0001f) return false;
            if (Mathf.Abs(LandPose.HardThigh - 74f) > 0.01f) return false;
            if (Mathf.Abs(LandPose.HardKnee - -125f) > 0.01f) return false;

            if (PushOpen(0f) < 0.2f || PushOpen(0f) > 0.45f) return false;
            if (PushOpen(0f) >= JumpPose.StrideLead) return false;
            if (Mathf.Abs(PushOpen(JumpPose.TakeoffSeconds) - 1f) > 0.0001f) return false;
            if (CoyoteOpen(0f) > 0.0001f) return false;
            if (Mathf.Abs(CoyoteOpen(CoyoteSeconds) - 1f) > 0.0001f) return false;
            if (HopOpen(0f) > 0.0001f) return false;
            if (Mathf.Abs(HopOpen(HopSeconds) - 1f) > 0.0001f) return false;
            if (Brace01(0f) > 0.0001f) return false;
            if (Brace01(JumpPose.FallVy) > 0.0001f) return false;
            if (Mathf.Abs(Brace01(-Terminal) - 1f) > 0.0001f) return false;
            if (!(JumpPose.FallThigh < BraceThigh && BraceThigh < LandPose.HardThigh)) return false;
            if (TuckScale(0f) < 0.99f) return false;
            if (TuckScale(Sprint) > 0.55f) return false;
            if (TuckThigh(0f) < TuckThigh(Sprint) + 12f) return false;

            if (TakeoffStep(true) >= TakeoffStep(false) * 0.75f) return false;
            if (TuckStep(true) >= TuckStep(false) * 0.75f) return false;
            if (FallStep(true) >= FallStep(false) * 0.55f) return false;
            if (HopStep(true) >= HopStep(false) * 0.75f) return false;
            if (StrafeStep(true) >= StrafeStep(false) * 0.55f) return false;
            if (CoyoteStep(true) >= CoyoteStep(false) * 0.75f) return false;
            if (TakeoffStep(true) <= 0.05f || TuckStep(true) <= 0.05f) return false;
            if (FallStep(true) <= 0.05f || HopStep(true) <= 0.05f) return false;
            if (StrafeStep(true) <= 0.05f || CoyoteStep(true) <= 0.05f) return false;
            if (ClearanceCm() < 2f) return false;
            if (ApexFlex() < 30f || ApexFlex() > 45f) return false;
            if (!ApexHandsOut()) return false;
            if (BraceKnee > -18f || BraceKnee < -48f) return false;
            if (BraceKnee > -25f || BraceKnee < -35f) return false;
            if (BraceElbow > -18f || BraceElbow < -42f) return false;
            if (BraceArmYaw < 40f || BraceArmYaw > 70f) return false;
            if (BraceLegYaw < 8f || BraceLegYaw > 24f) return false;
            if (BraceSpine > 0f) return false;
            if (BraceHead < 8f) return false;
            if (BraceArmPitch < 0f || BraceArmPitch > 35f) return false;
            return true;
        }

        static JumpPose.Sample AirSample(float vy, float age, float planar, float phase, bool hop, bool scale, bool brace)
        {
            JumpPose.Sample s = JumpPose.At(vy, age, true);
            LocomotionPolish.AirPhase(ref s.ThighL, ref s.ThighR, ref s.KneeL, ref s.KneeR, ref s.ArmPitchL, ref s.ArmPitchR, ref s.Spine, vy, hop);
            BalanceArms(ref s, planar, vy, age);
            if (scale)
                ScaleTuck(ref s, planar, vy, age);
            if (hop)
                HopCycle(ref s, phase, 1f);
            else if (brace)
                Brace(ref s, vy);
            return s;
        }

        static JumpPose.Sample TakeoffSample(float age, bool oldLead)
        {
            return TakeoffSample(age, oldLead, false);
        }

        static JumpPose.Sample TakeoffSample(float age, bool oldLead, bool coyote)
        {
            float vy = JumpSpeed - RiseGravity * (age < 0f ? 0f : age);
            JumpPose.Sample stride = JumpPose.Stride(Sprint, 1f, 1.5707963f);
            JumpPose.Sample beat = AirSample(vy, age, Sprint, 0f, false, !oldLead, false);
            float w;
            if (coyote)
                w = oldLead ? JumpPose.FromStride(age) : CoyoteOpen(age);
            else
                w = oldLead ? JumpPose.FromStride(age) : PushOpen(age);
            return Blend(stride, beat, w);
        }

        static float TuckThigh(float planar)
        {
            JumpPose.Sample s = AirSample(JumpPose.RiseVy, JumpPose.TakeoffSeconds, planar, 0f, false, true, false);
            return s.ThighL;
        }

        static float TakeoffStep(bool eased)
        {
            JumpPose.Sample stride = JumpPose.Stride(Sprint, 1f, 1.5707963f);
            float prev = stride.ThighL;
            float peak = 0f;
            float age = 0f;
            int n = (int)(JumpPose.TakeoffSeconds / Dt) + 3;
            for (int i = 0; i < n; i++)
            {
                float now = TakeoffSample(age, !eased).ThighL;
                peak = Max(peak, Abs(now - prev));
                prev = now;
                age += Dt;
            }
            return peak;
        }

        static float TuckStep(bool eased)
        {
            float vy = JumpSpeed;
            float prev = AirSample(vy, JumpPose.TakeoffSeconds, Sprint, 0f, false, eased, false).ThighL;
            float peak = 0f;
            int n = (int)(1.4f / Dt);
            for (int i = 0; i < n; i++)
            {
                float g = vy > 0f ? RiseGravity : RiseGravity * FallMult;
                vy -= g * Dt;
                if (vy < JumpPose.FallVy) break;
                float now = AirSample(vy, 0.2f, Sprint, 0f, false, eased, false).ThighL;
                peak = Max(peak, Abs(now - prev));
                prev = now;
            }
            return peak;
        }

        static float FallStep(bool eased)
        {
            float vy = JumpSpeed;
            float prev = AirSample(vy, 0.2f, Sprint, 0f, false, false, eased).ThighL;
            float peak = 0f;
            int n = (int)(3.2f / Dt);
            for (int i = 0; i < n; i++)
            {
                float g = vy > 0f ? RiseGravity : RiseGravity * FallMult;
                vy -= g * Dt;
                if (vy < -Terminal) vy = -Terminal;
                float now = AirSample(vy, 0.4f, Sprint, 0f, false, false, eased).ThighL;
                peak = Max(peak, Abs(now - prev));
                prev = now;
                if (vy <= -Terminal) break;
            }
            peak = Max(peak, Abs(LandPose.HardThigh - prev));
            return peak;
        }

        static float HopStep(bool eased)
        {
            JumpPose.Sample fall = AirSample(JumpPose.FallVy, 0.4f, Sprint, 0f, false, false, false);
            float prev = fall.ThighL;
            float peak = 0f;
            float age = 0f;
            float phase = 0f;
            int n = (int)(0.45f / Dt);
            for (int i = 0; i < n; i++)
            {
                float now;
                if (!eased)
                {
                    PoseHandoff.HopTakeoff(age, out _, out float w);
                    JumpPose.Sample drive = JumpPose.At(JumpSpeed, 0f, true);
                    now = Lerp(fall.ThighL, drive.ThighL, w);
                }
                else
                {
                    JumpPose.Sample cyc = fall;
                    HopCycle(ref cyc, phase, 1f);
                    now = Lerp(fall.ThighL, cyc.ThighL, HopOpen(age));
                }
                peak = Max(peak, Abs(now - prev));
                prev = now;
                age += Dt;
                phase += HopRate * Dt;
            }
            return peak;
        }

        static float StrafeStep(bool eased)
        {
            if (!eased) return AirStrafeLeanPose.Roll;
            float cur = 0f;
            float vel = 0f;
            float peak = 0f;
            float target = AirStrafeLeanPose.Roll;
            int n = (int)(StrafeSeconds / Dt) + 16;
            for (int i = 0; i < n; i++)
            {
                float next = SmoothMotion.Smooth(cur, target, ref vel, StrafeSeconds, Dt);
                peak = Max(peak, Abs(next - cur));
                cur = next;
            }
            return peak;
        }

        static float CoyoteStep(bool eased)
        {
            JumpPose.Sample stride = JumpPose.Stride(Sprint, 1f, 1.5707963f);
            float prev = stride.ThighL;
            float peak = 0f;
            float age = 0f;
            int n = (int)(CoyoteSeconds / Dt) + 3;
            for (int i = 0; i < n; i++)
            {
                float now = TakeoffSample(age, !eased, true).ThighL;
                peak = Max(peak, Abs(now - prev));
                prev = now;
                age += Dt;
            }
            return peak;
        }

        static float StrafeEase(float u)
        {
            float cur = 0f;
            float vel = 0f;
            float target = 1f;
            int n = (int)(u * 8f);
            if (n < 1) n = u <= 0f ? 0 : 1;
            for (int i = 0; i < n; i++)
                cur = SmoothMotion.Smooth(cur, target, ref vel, StrafeSeconds, Dt);
            return cur;
        }

        static Shot FromSample(JumpPose.Sample s, float lean)
        {
            return new Shot
            {
                ArmL = s.ArmPitchL,
                ArmR = s.ArmPitchR,
                ElbL = s.ElbowL,
                ElbR = s.ElbowR,
                ThL = s.ThighL,
                ThR = s.ThighR,
                KnL = s.KneeL,
                KnR = s.KneeR,
                Spine = s.Spine,
                Hip = s.Hip,
                Lean = lean,
            };
        }

        static string Pair(float before, float after, CultureInfo c)
        {
            return before.ToString("0.0", c) + ">" + after.ToString("0.0", c);
        }

        /// <summary>Minimum arm clearance to the head and the torso, in centimetres, on the tuck.</summary>
        public static float ClearanceCm()
        {
            JumpPose.Sample s = AirSample(JumpSpeed, 0.2f, Sprint, 0f, false, true, false);
            return AirClear.Centimetres(s.Hip, s.Spine, s.ArmPitchL, s.ArmYawL, -s.ArmYawR, s.ElbowL, out _, out _);
        }

        static float ApexFlex()
        {
            JumpPose.Sample s = AirSample(JumpSpeed, 0.2f, Sprint, 0f, false, true, false);
            AirClear.Centimetres(s.Hip, s.Spine, s.ArmPitchL, s.ArmYawL, -s.ArmYawR, s.ElbowL, out AirClear.Hand left, out _);
            return left.Flex;
        }

        static bool ApexHandsOut()
        {
            JumpPose.Sample s = AirSample(JumpSpeed, 0.2f, Sprint, 0f, false, true, false);
            AirClear.Centimetres(s.Hip, s.Spine, s.ArmPitchL, s.ArmYawL, -s.ArmYawR, s.ElbowL, out AirClear.Hand left, out AirClear.Hand right);
            if (left.X < 0.15f || right.X > -0.15f) return false;
            if (left.Y > -0.25f || right.Y > -0.25f) return false;
            if (left.Z < 1.25f || left.Z > 1.56f) return false;
            if (right.Z < 1.25f || right.Z > 1.56f) return false;
            return true;
        }

        static float Raised(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return 0.5f * (1f - Mathf.Cos(u * 3.14159265f));
        }

        static float Inv(float a, float b, float v)
        {
            if (b <= a) return v >= b ? 1f : 0f;
            float t = (v - a) / (b - a);
            if (t < 0f) return 0f;
            if (t > 1f) return 1f;
            return t;
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
