using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual locomotion on top of the gait and the jump. The mesh matches walk
    /// 6.9 and sprint 13.8, blends strafe and backpedal, plants a fast 180, and
    /// floats the apex. Feet and hands are posed only. The capsule is not moved.
    /// </summary>
    public static class LocomotionPolish
    {
        public const bool RootMotion = false;
        public const bool CapsuleMoved = false;
        public const float GameplayDelay = 0f;

        public const float WalkSpeed = 6.9f;
        public const float SprintSpeed = 13.8f;

        /// <summary>
        /// Mesh cycle cap. Above <see cref="GaitBlend.CadenceSprint"/> so a 13.8
        /// sprint covers the stance. The proof cadence stays <see cref="GaitBlend.CadenceAt"/>.
        /// </summary>
        public const float PlayCadenceCap = 42f;

        public const float HardTurnEnter = 120f;
        public const float HardTurnWindow = 0.28f;
        public const float HardTurnPlantSeconds = 0.16f;
        public const float HardPlantThigh = 10f;
        public const float HardPlantKnee = -28f;

        public const float ApexThigh = 10f;
        public const float ApexKnee = -16f;
        public const float ApexArm = -18f;
        public const float ApexSpine = -4f;
        public const float BalanceArm = -42f;

        public const float HeadLookMax = 35f;
        public const float SpineCounter = -0.4f;
        public const float ArmFollowMax = 12f;
        public const float SecondarySeconds = 0.10f;

        public const float FootLiftMax = 0.07f;
        public const float FootPitchMax = 22f;
        public const float HandPitchMax = 18f;

        public struct Legs
        {
            public float ThighL, ThighR, KneeL, KneeR, HipYaw;
        }

        /// <summary>Radians per second for the mesh cycle. 0 at rest.</summary>
        public static float PlayCadence(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            float gate = Mathf.SmoothStep(0f, 1f, Inv(GaitBlend.IdleGate, GaitBlend.IdleGate + GaitBlend.CadenceGateSpan, s));
            float travel = GaitBlend.FootTravel(GaitBlend.PoseWeight(s));
            if (travel < 0.08f) travel = 0.08f;
            float match = s * 3.14159265f / travel;
            if (match > PlayCadenceCap) match = PlayCadenceCap;
            return match * gate;
        }

        /// <summary>Share of stance travel the sole misses when the mesh uses <see cref="PlayCadence"/>.</summary>
        public static float PlaySlip(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            float cadence = PlayCadence(s);
            if (cadence < 0.05f || s <= GaitBlend.IdleGate) return 0f;
            float body = s * 3.14159265f / cadence;
            if (body < 0.001f) return 0f;
            float slip = (body - GaitBlend.FootTravel(GaitBlend.PoseWeight(s))) / body;
            return slip < 0f ? 0f : slip;
        }

        /// <summary>
        /// Forward stride, a shorter reversed backpedal, or a reduced side step.
        /// fwd and side are planar velocity on the facing axes. Hip yaw points at travel.
        /// </summary>
        public static Legs FacingStride(float thighL, float thighR, float kneeL, float kneeR, float fwd, float side)
        {
            float mag = Mathf.Sqrt(fwd * fwd + side * side);
            float facing = 1f;
            float sideShare = 0f;
            if (mag > 0.35f)
            {
                facing = fwd / mag;
                float sideAbs = side < 0f ? -side : side;
                sideShare = sideAbs / mag;
            }
            float back = facing < 0f ? -facing : 0f;
            float fore = 1f - back * 0.28f - sideShare * 0.62f;
            if (fore < 0.22f) fore = 0.22f;
            float flip = facing < -0.2f ? -1f : 1f;
            float kneeScale = (flip < 0f ? 0.85f : 1f) * (0.55f + 0.45f * fore);
            float hip = 0f;
            if (mag > 0.35f)
            {
                float yaw = (float)System.Math.Atan2(side, fwd) * Mathf.Rad2Deg;
                if (yaw > 28f) yaw = 28f;
                if (yaw < -28f) yaw = -28f;
                hip = yaw * sideShare;
            }
            return new Legs
            {
                ThighL = thighL * fore * flip,
                ThighR = thighR * fore * flip,
                KneeL = kneeL * kneeScale,
                KneeR = kneeR * kneeScale,
                HipYaw = hip,
            };
        }

        /// <summary>
        /// Accumulates grounded yaw. A heading change of <see cref="HardTurnEnter"/>
        /// inside <see cref="HardTurnWindow"/> plants. A blocked verb clears it.
        /// </summary>
        public static void NoteTurn(ref float accum, ref float age, ref float plant, ref float sign, float yawDeltaDeg, float dt, bool grounded, bool blocked)
        {
            if (blocked || !grounded)
            {
                accum = 0f;
                age = 0f;
                plant = 0f;
                return;
            }
            if (dt < 0f) dt = 0f;
            age += dt;
            if (age > HardTurnWindow)
            {
                accum = 0f;
                age = dt;
            }
            accum += yawDeltaDeg;
            if (accum > 180f) accum = 180f;
            if (accum < -180f) accum = -180f;
            float abs = accum < 0f ? -accum : accum;
            if (abs >= HardTurnEnter)
            {
                plant = 1f;
                sign = accum < 0f ? -1f : 1f;
                accum = 0f;
                age = 0f;
            }
            else if (plant > 0f)
            {
                float step = HardTurnPlantSeconds > 0.0001f ? dt / HardTurnPlantSeconds : 1f;
                plant -= step;
                if (plant < 0f) plant = 0f;
            }
        }

        /// <summary>1 at the apex, 0 while rising hard or falling hard. Does not change JumpPose.Extend.</summary>
        public static float ApexFloat(float verticalSpeed)
        {
            float a = verticalSpeed < 0f ? -verticalSpeed : verticalSpeed;
            float t = a / 7f;
            if (t > 1f) t = 1f;
            return 1f - t * t;
        }

        /// <summary>A hop keeps a wide arm. A normal jump keeps the authored pitch.</summary>
        public static float ChainArm(float pitch, bool chain)
        {
            if (!chain) return pitch;
            return Mathf.Lerp(BalanceArm, pitch, 0.38f);
        }

        /// <summary>Apex tuck, then hop arms stay on the balance pose.</summary>
        public static void AirPhase(ref float thighL, ref float thighR, ref float kneeL, ref float kneeR, ref float armL, ref float armR, ref float spine, float verticalSpeed, bool chain)
        {
            float apex = ApexFloat(verticalSpeed);
            thighL += ApexThigh * apex;
            thighR += ApexThigh * apex;
            kneeL += ApexKnee * apex;
            kneeR += ApexKnee * apex;
            armL += ApexArm * apex;
            armR += ApexArm * apex;
            spine += ApexSpine * apex;
            if (!chain) return;
            armL = ChainArm(armL, true);
            armR = ChainArm(armR, true);
        }

        /// <summary>Extra chest pitch while the first step is still blending in.</summary>
        public static float StartLean(float startBlend, float accelLean)
        {
            float blend = startBlend < 0f ? 0f : (startBlend > 1f ? 1f : startBlend);
            float push = accelLean > 0f ? accelLean : 0f;
            return blend * (4f + push * 0.35f);
        }

        public static float TravelYaw(float fwd, float side, float speed)
        {
            if (speed < 0.8f) return 0f;
            float yaw = (float)System.Math.Atan2(side, fwd) * Mathf.Rad2Deg;
            if (yaw > HeadLookMax) yaw = HeadLookMax;
            if (yaw < -HeadLookMax) yaw = -HeadLookMax;
            return yaw;
        }

        public static float HeadYaw(float travelYaw)
        {
            if (travelYaw > HeadLookMax) return HeadLookMax;
            if (travelYaw < -HeadLookMax) return -HeadLookMax;
            return travelYaw;
        }

        public static float SpineYaw(float headYaw)
        {
            return headYaw * SpineCounter;
        }

        public static float ArmFollowTarget(float swing)
        {
            if (swing > ArmFollowMax) return ArmFollowMax;
            if (swing < -ArmFollowMax) return -ArmFollowMax;
            return swing;
        }

        /// <summary>Meters to lift a foot bone. Positive is up.</summary>
        public static float FootLift(float gapMeters)
        {
            if (gapMeters > FootLiftMax) return FootLiftMax;
            if (gapMeters < -FootLiftMax) return -FootLiftMax;
            return gapMeters;
        }

        /// <summary>Sole pitch from the ground normal, degrees.</summary>
        public static float FootPitch(float normalY, float normalAlong)
        {
            float y = normalY < 0.05f ? 0.05f : normalY;
            float deg = (float)System.Math.Atan(normalAlong / y) * Mathf.Rad2Deg;
            if (deg > FootPitchMax) deg = FootPitchMax;
            if (deg < -FootPitchMax) deg = -FootPitchMax;
            return deg;
        }

        /// <summary>Extra arm pitch so the hand meets a nearer wall. Positive reaches.</summary>
        public static float HandPitch(float closeMeters)
        {
            float deg = closeMeters * 36f;
            if (deg > HandPitchMax) deg = HandPitchMax;
            if (deg < -HandPitchMax) deg = -HandPitchMax;
            return deg;
        }

        public static void PlantStep(out float before, out float after)
        {
            const float from = 48f;
            before = from - HardPlantThigh;
            if (before < 0f) before = -before;
            float w = 0f;
            float wv = 0f;
            w = SmoothMotion.Smooth(w, 1f, ref wv, SmoothMotion.ResponsiveSeconds, 1f / 60f);
            float target = from + (HardPlantThigh - from) * w;
            float bone = from;
            float a = 1f - (float)System.Math.Exp(-64.0 / 60.0);
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            bone = bone + (target - bone) * a;
            after = bone - from;
            if (after < 0f) after = -after;
        }

        public static void ApexStep(out float before, out float after)
        {
            before = ApexThigh;
            float cur = 16f;
            float vel = 0f;
            float next = SmoothMotion.Smooth(cur, cur + ApexThigh, ref vel, SmoothMotion.ResponsiveSeconds, 1f / 60f);
            after = next - cur;
            if (after < 0f) after = -after;
        }

        public static bool Holds()
        {
            if (RootMotion || CapsuleMoved) return false;
            if (GameplayDelay != 0f) return false;
            if (Mathf.Abs(WalkSpeed - 6.9f) > 0.001f || Mathf.Abs(SprintSpeed - 13.8f) > 0.001f) return false;
            if (PlaySlip(WalkSpeed) > 0.02f || PlaySlip(SprintSpeed) > 0.02f) return false;
            if (GaitBlend.FootSlip(SprintSpeed) < 0.2f) return false;
            if (PlayCadence(SprintSpeed) < GaitBlend.CadenceAt(SprintSpeed) + 4f) return false;
            if (Mathf.Abs(PlayCadence(WalkSpeed) - GaitBlend.CadenceAt(WalkSpeed)) > 0.05f) return false;
            if (Mathf.Abs(JumpPose.Extend(0f) - JumpPose.Extend(0f)) > 0.0001f) return false;

            GaitBlend.Legs legs = GaitBlend.At(1.2f, SprintSpeed);
            Legs side = FacingStride(legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, 0f, SprintSpeed);
            float foreAmp = Abs(legs.ThighL) + Abs(legs.ThighR);
            float sideAmp = Abs(side.ThighL) + Abs(side.ThighR);
            if (foreAmp < 8f) return false;
            if (sideAmp > foreAmp * 0.55f) return false;
            if (Abs(side.HipYaw) < 4f) return false;
            Legs back = FacingStride(legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, -SprintSpeed, 0f);
            if (Abs(legs.ThighL) > 2f && legs.ThighL * back.ThighL > 0f) return false;
            if (Abs(back.ThighL) > Abs(legs.ThighL) * 0.85f) return false;
            Legs ahead = FacingStride(legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, SprintSpeed, 0f);
            if (Abs(ahead.ThighL - legs.ThighL) > 0.05f) return false;
            if (Abs(ahead.HipYaw) > 0.05f) return false;

            float accum = 0f;
            float age = 0f;
            float plant = 0f;
            float sign = 0f;
            NoteTurn(ref accum, ref age, ref plant, ref sign, 70f, 0.1f, true, false);
            if (plant > 0.001f) return false;
            NoteTurn(ref accum, ref age, ref plant, ref sign, 70f, 0.1f, true, false);
            if (plant < 0.99f || Abs(sign) < 0.5f) return false;
            NoteTurn(ref accum, ref age, ref plant, ref sign, 70f, 0.1f, true, true);
            if (plant > 0.001f) return false;
            accum = 0f;
            age = 0f;
            plant = 0f;
            NoteTurn(ref accum, ref age, ref plant, ref sign, 40f, 0.2f, true, false);
            NoteTurn(ref accum, ref age, ref plant, ref sign, 40f, 0.2f, true, false);
            if (plant > 0.001f) return false;

            if (ApexFloat(0f) < 0.95f) return false;
            if (ApexFloat(24.7f) > 0.02f || ApexFloat(-20f) > 0.02f) return false;
            if (ApexFloat(JumpPose.RiseVy) > 0.02f) return false;
            float open = ChainArm(JumpPose.SwingArmPitch, false);
            float kept = ChainArm(JumpPose.SwingArmPitch, true);
            if (Abs(open - JumpPose.SwingArmPitch) > 0.01f) return false;
            if (kept <= open || kept < -100f || kept > -60f) return false;
            float tl = 16f;
            float tr = 16f;
            float kl = -18f;
            float kr = -18f;
            float al = -30f;
            float ar = -30f;
            float sp = 6f;
            AirPhase(ref tl, ref tr, ref kl, ref kr, ref al, ref ar, ref sp, 0f, false);
            if (tl < 24f || kl > -30f) return false;
            float tl2 = 16f;
            float tr2 = 16f;
            float kl2 = -18f;
            float kr2 = -18f;
            float al2 = -30f;
            float ar2 = -30f;
            float sp2 = 6f;
            AirPhase(ref tl2, ref tr2, ref kl2, ref kr2, ref al2, ref ar2, ref sp2, -20f, false);
            if (Abs(tl2 - 16f) > 0.01f) return false;

            if (Abs(HeadYaw(80f) - HeadLookMax) > 0.01f) return false;
            if (Abs(SpineYaw(HeadLookMax) - HeadLookMax * SpineCounter) > 0.01f) return false;
            if (Abs(ArmFollowTarget(40f) - ArmFollowMax) > 0.01f) return false;
            if (Abs(ArmFollowTarget(-40f) + ArmFollowMax) > 0.01f) return false;
            if (StartLean(0f, 6f) > 0.01f) return false;
            if (StartLean(1f, 0f) < 3f) return false;

            if (Abs(FootLift(1f) - FootLiftMax) > 0.001f) return false;
            if (Abs(FootLift(-1f) + FootLiftMax) > 0.001f) return false;
            if (Abs(FootPitch(1f, 0f)) > 0.01f) return false;
            if (Abs(FootPitch(0.8f, 0.5f)) < 8f) return false;
            if (Abs(HandPitch(0.4f)) < 8f) return false;
            if (Abs(HandPitch(0f)) > 0.01f) return false;

            PlantStep(out float plantBefore, out float plantAfter);
            if (plantAfter < 0.4f || plantAfter > plantBefore * 0.45f) return false;
            ApexStep(out float apexBefore, out float apexAfter);
            if (apexAfter < 0.4f || apexAfter > apexBefore * 0.5f) return false;
            if (!SmoothMotion.ResponsesSameFrame()) return false;
            return true;
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "loco-polish"
                + " walkSlip=" + PlaySlip(WalkSpeed).ToString("0.000", c)
                + " sprintSlip=" + PlaySlip(SprintSpeed).ToString("0.000", c)
                + " proofSlip=" + GaitBlend.FootSlip(SprintSpeed).ToString("0.000", c)
                + " playCadence=" + PlayCadence(SprintSpeed).ToString("0.00", c)
                + " proofCadence=" + GaitBlend.CadenceAt(SprintSpeed).ToString("0.00", c)
                + " apex=" + ApexFloat(0f).ToString("0.00", c)
                + " chainArm=" + ChainArm(JumpPose.SwingArmPitch, true).ToString("0.0", c)
                + " head=" + HeadLookMax.ToString("0", c)
                + " spine=" + SpineYaw(HeadLookMax).ToString("0.0", c)
                + " armFollow=" + ArmFollowMax.ToString("0", c)
                + " footIk=1 handIk=1 gameplayDelay=0 rootMotion=0";
        }

        public static string StepLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            PlantStep(out float plantBefore, out float plantAfter);
            ApexStep(out float apexBefore, out float apexAfter);
            return "loco-step"
                + " plantBefore=" + plantBefore.ToString("0.0", c)
                + " plantAfter=" + plantAfter.ToString("0.0", c)
                + " apexBefore=" + apexBefore.ToString("0.0", c)
                + " apexAfter=" + apexAfter.ToString("0.0", c)
                + " visualFrame=0 gameplayDelay=0";
        }

        static float Abs(float v)
        {
            return v < 0f ? -v : v;
        }

        static float Inv(float a, float b, float v)
        {
            float d = b - a;
            if (d > -0.00001f && d < 0.00001f) return 0f;
            float t = (v - a) / d;
            if (t < 0f) return 0f;
            if (t > 1f) return 1f;
            return t;
        }
    }
}
