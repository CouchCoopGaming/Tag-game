using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual jump only. Three beats at chase-cam distance: a crouch and a drive
    /// knee on the frame the jump fires, a tuck while rising, then legs down and
    /// arms wide as vertical speed falls. The impulse is not delayed. Wall jumps
    /// and slide-cancel jumps do not use this. The landing thud owns the ground.
    /// </summary>
    public static class JumpPose
    {
        /// <summary>The motor already wrote the impulse. This pose does not wait.</summary>
        public const float ImpulseDelaySeconds = 0f;
        public const bool RootMotion = false;

        /// <summary>How long the takeoff crouch leads. Short, so the tuck still reads.</summary>
        public const float TakeoffSeconds = 0.08f;
        /// <summary>Degrees per second while the crouch is up, so it arrives on the fire frame.</summary>
        public const float TakeoffSlew = 170f;
        /// <summary>Stride share fades out. The fire frame is already mostly the crouch.</summary>
        public const float StrideBlendSeconds = 0.06f;
        /// <summary>Jump-pose share on the fire frame. The rest is the stride phase.</summary>
        public const float StrideLead = 0.62f;

        public const float CrouchHip = 34f;
        public const float CrouchSpine = 18f;
        public const float CrouchKnee = -74f;
        public const float PlantThigh = -16f;
        public const float DriveThigh = 88f;
        public const float DriveKnee = -110f;
        public const float SwingArmPitch = -146f;
        public const float SwingArmYaw = 24f;
        public const float SwingElbow = -16f;

        public const float TuckThigh = 76f;
        public const float TuckKnee = -112f;
        public const float TuckArmPitch = -126f;
        public const float TuckArmYaw = 18f;
        public const float TuckElbow = -26f;
        public const float TuckSpine = -8f;
        public const float TuckHip = 14f;

        public const float FallThigh = 16f;
        public const float FallKnee = -18f;
        public const float FallArmPitch = -30f;
        public const float FallArmYaw = 58f;
        public const float FallElbow = -12f;
        public const float FallSpine = 6f;
        public const float FallHip = 4f;

        /// <summary>Extend is 0 at and above this vertical speed. The tuck holds.</summary>
        public const float RiseVy = 8f;
        /// <summary>Extend is 1 at and below this vertical speed. Legs are down, arms are wide.</summary>
        public const float FallVy = -12f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine;
        }

        /// <summary>1 on the fire frame, 0 once the crouch has led. Negative age is not a jump.</summary>
        public static float TakeoffWeight(float age)
        {
            if (age < 0f) return 0f;
            if (age >= TakeoffSeconds) return 0f;
            float u = age / TakeoffSeconds;
            return 1f - u * u;
        }

        /// <summary>
        /// 0 while rising, 1 while falling. Smooth through the apex.
        /// Horizontal speed is ignored, so the apex pose does not change with it.
        /// </summary>
        public static float Extend(float verticalSpeed)
        {
            return Mathf.SmoothStep(0f, 1f, Inv(RiseVy, FallVy, verticalSpeed));
        }

        public static float ExtendAt(float verticalSpeed, float planarSpeed)
        {
            return Extend(verticalSpeed) + planarSpeed * 0f;
        }

        /// <summary>Jump-pose share. StrideLead on the fire frame, 1 after the blend.</summary>
        public static float FromStride(float age)
        {
            if (age <= 0f) return StrideLead;
            float u = age / StrideBlendSeconds;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float s = u * u * (3f - 2f * u);
            return StrideLead + (1f - StrideLead) * s;
        }

        /// <summary>
        /// Plain airborne jump only. A wall jump, a slide-cancel jump, any other
        /// verb tell, or the ground (the landing thud) stays off.
        /// </summary>
        public static bool PoseActive(bool airborne, bool wallJump, bool slideJump, bool otherTell)
        {
            if (!airborne) return false;
            if (wallJump || slideJump) return false;
            if (otherTell) return false;
            return true;
        }

        public static Sample At(float verticalSpeed, float age, bool driveLeft)
        {
            float extend = Extend(verticalSpeed);
            float take = TakeoffWeight(age);
            float tuckR = TuckThigh - 6f;
            float tuckKr = TuckKnee + 6f;
            float bodyThighL = Mathf.Lerp(TuckThigh, FallThigh, extend);
            float bodyThighR = Mathf.Lerp(tuckR, FallThigh, extend);
            float bodyKneeL = Mathf.Lerp(TuckKnee, FallKnee, extend);
            float bodyKneeR = Mathf.Lerp(tuckKr, FallKnee, extend);
            float driveThigh = DriveThigh;
            float plantThigh = PlantThigh;
            float driveKnee = DriveKnee;
            float plantKnee = CrouchKnee;
            float takeL = driveLeft ? driveThigh : plantThigh;
            float takeR = driveLeft ? plantThigh : driveThigh;
            float takeKl = driveLeft ? driveKnee : plantKnee;
            float takeKr = driveLeft ? plantKnee : driveKnee;
            float pitch = Mathf.Lerp(Mathf.Lerp(TuckArmPitch, FallArmPitch, extend), SwingArmPitch, take);
            float yaw = Mathf.Lerp(Mathf.Lerp(TuckArmYaw, FallArmYaw, extend), SwingArmYaw, take);
            float elbow = Mathf.Lerp(Mathf.Lerp(TuckElbow, FallElbow, extend), SwingElbow, take);
            return new Sample
            {
                ThighL = Mathf.Lerp(bodyThighL, takeL, take),
                ThighR = Mathf.Lerp(bodyThighR, takeR, take),
                KneeL = Mathf.Lerp(bodyKneeL, takeKl, take),
                KneeR = Mathf.Lerp(bodyKneeR, takeKr, take),
                ArmPitchL = pitch,
                ArmPitchR = pitch,
                ArmYawL = yaw,
                ArmYawR = yaw,
                ElbowL = elbow,
                ElbowR = elbow,
                Hip = Mathf.Lerp(Mathf.Lerp(TuckHip, FallHip, extend), CrouchHip, take),
                Spine = Mathf.Lerp(Mathf.Lerp(TuckSpine, FallSpine, extend), CrouchSpine, take),
            };
        }

        /// <summary>
        /// Ground stride at this phase. Same reach and plant as the run, so a
        /// hop chain does not swap the front leg.
        /// </summary>
        public static Sample Stride(float planarSpeed, float sinC, float cycleRadians)
        {
            float gait = GaitBlend.PoseWeight(planarSpeed);
            float reach = GaitBlend.ThighReach(gait);
            float frontL = sinC > 0f ? sinC : 0f;
            float frontR = sinC < 0f ? -sinC : 0f;
            float thighL = (frontL - frontR * 0.58f) * reach;
            float thighR = (frontR - frontL * 0.58f) * reach;
            float kneeAmt = GaitBlend.KneeBend(gait);
            float kneeL = -(2f + frontL * kneeAmt);
            float kneeR = -(2f + frontR * kneeAmt);
            float cadence = GaitBlend.CadenceAt(planarSpeed);
            const float pi = 3.14159265f;
            const float tau = pi * 2f;
            float phase = cycleRadians % tau;
            if (phase < 0f) phase += tau;
            if (cadence >= 0.05f)
            {
                if (phase >= pi)
                {
                    float plant = GaitBlend.PlantCounterDeg(planarSpeed, cadence, (phase - pi) / pi);
                    thighL -= plant;
                    kneeL -= plant * 0.4f;
                }
                else
                {
                    float plant = GaitBlend.PlantCounterDeg(planarSpeed, cadence, phase / pi);
                    thighR -= plant;
                    kneeR -= plant * 0.4f;
                }
            }

            float amp = GaitBlend.ArmAmp(gait);
            float idle = 1f - gait;
            float outY = Mathf.Lerp(12f, 8f, gait);
            float reachY = Mathf.Lerp(outY, outY + 6f, gait);
            float yawL = Mathf.Lerp(outY, reachY, frontR * gait);
            float yawR = Mathf.Lerp(outY, reachY, frontL * gait);
            float elbowReach = Mathf.Lerp(-10f, -6f, gait);
            float elbowPull = Mathf.Lerp(-18f, -30f, gait);
            return new Sample
            {
                ThighL = thighL,
                ThighR = thighR,
                KneeL = kneeL,
                KneeR = kneeR,
                ArmPitchL = ArmPitch(-sinC, amp) - 12f * idle,
                ArmPitchR = ArmPitch(sinC, amp) - 12f * idle,
                ArmYawL = yawL,
                ArmYawR = yawR,
                ElbowL = Mathf.Lerp(elbowReach, elbowPull, frontL * gait),
                ElbowR = Mathf.Lerp(elbowReach, elbowPull, frontR * gait),
                Hip = 0f,
                Spine = 0f,
            };
        }

        public static Sample Mixed(float verticalSpeed, float age, bool driveLeft, float planarSpeed, float sinC, float cycleRadians)
        {
            Sample beat = At(verticalSpeed, age, driveLeft);
            Sample stride = Stride(planarSpeed, sinC, cycleRadians);
            return Lerp(stride, beat, FromStride(age));
        }

        public static bool Holds()
        {
            if (ImpulseDelaySeconds != 0f || RootMotion) return false;
            if (TakeoffSlew < 120f) return false;
            if (Mathf.Abs(TakeoffWeight(0f) - 1f) > 0.0001f) return false;
            if (TakeoffWeight(-0.01f) > 0.0001f) return false;
            if (TakeoffWeight(TakeoffSeconds) > 0.0001f) return false;
            if (TakeoffWeight(TakeoffSeconds * 0.5f) < 0.7f) return false;
            float prevTake = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float w = TakeoffWeight(TakeoffSeconds * i / 8f);
                if (w > prevTake + 0.0001f) return false;
                prevTake = w;
            }

            if (Extend(24.7f) > 0.0001f) return false;
            if (Extend(RiseVy) > 0.0001f) return false;
            if (Mathf.Abs(Extend(FallVy) - 1f) > 0.0001f) return false;
            if (Extend(-20f) < 0.999f) return false;
            float apex = Extend(0f);
            if (apex < 0.25f || apex > 0.55f) return false;
            if (Mathf.Abs(ExtendAt(0f, 0f) - ExtendAt(0f, 12f)) > 0.0001f) return false;
            if (Mathf.Abs(ExtendAt(0f, 12f) - ExtendAt(0f, 24f)) > 0.0001f) return false;
            float prevExt = -1f;
            float[] vys = { 24f, 16f, 8f, 4f, 0f, -4f, -8f, -12f, -16f };
            for (int i = 0; i < vys.Length; i++)
            {
                float e = Extend(vys[i]);
                if (e + 0.0001f < prevExt) return false;
                prevExt = e;
            }

            if (Mathf.Abs(FromStride(0f) - StrideLead) > 0.0001f) return false;
            if (StrideLead < 0.5f || StrideLead > 0.85f) return false;
            if (Mathf.Abs(FromStride(StrideBlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(FromStride(1f) - 1f) > 0.0001f) return false;
            float prevStride = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float w = FromStride(StrideBlendSeconds * i / 8f);
                if (w + 0.0001f < prevStride) return false;
                prevStride = w;
            }

            if (PoseActive(false, false, false, false)) return false;
            if (PoseActive(true, true, false, false)) return false;
            if (PoseActive(true, false, true, false)) return false;
            if (PoseActive(true, false, false, true)) return false;
            if (!PoseActive(true, false, false, false)) return false;

            if (!(DriveThigh > TuckThigh && TuckThigh > FallThigh)) return false;
            if (!(DriveKnee < CrouchKnee && TuckKnee < CrouchKnee && CrouchKnee < FallKnee)) return false;
            if (DriveKnee > -100f || TuckKnee > -100f) return false;
            if (!(CrouchHip > TuckHip && TuckHip > FallHip)) return false;
            if (!(SwingArmPitch < TuckArmPitch && TuckArmPitch < FallArmPitch)) return false;
            if (FallArmPitch > -20f) return false;
            if (!(FallArmYaw > SwingArmYaw && SwingArmYaw > TuckArmYaw)) return false;
            if (FallArmYaw < TuckArmYaw + 30f) return false;

            const float cycle = 0.9f;
            float sinC = Mathf.Sin(cycle);
            Sample sprintStride = Stride(12f, sinC, cycle);
            Sample sprint = Mixed(24.7f, 0f, true, 12f, sinC, cycle);
            if (sprintStride.ThighL <= sprintStride.ThighR) return false;
            if (sprint.ThighL <= sprint.ThighR) return false;
            if (sprint.ThighL <= sprintStride.ThighL) return false;
            if (sprint.KneeR >= sprintStride.KneeR - 20f) return false;
            if (sprint.ArmPitchL >= sprintStride.ArmPitchL) return false;
            if (sprint.ArmPitchR >= sprintStride.ArmPitchR) return false;
            if (sprint.Hip < 12f) return false;

            Sample walk = Mixed(24.7f, 0f, true, 6f, sinC, cycle);
            Sample walkStride = Stride(6f, sinC, cycle);
            if (walkStride.ThighL <= walkStride.ThighR) return false;
            if (walk.ThighL <= walk.ThighR) return false;
            if (walk.ThighL <= walkStride.ThighL) return false;

            Sample rise = At(24.7f, 0.2f, true);
            if (rise.ThighL < 68f || rise.ThighR < 68f) return false;
            if (rise.KneeL > -100f || rise.KneeR > -100f) return false;
            if (rise.ArmPitchL > -110f) return false;
            if (Mathf.Abs(rise.Hip - TuckHip) > 0.05f) return false;

            Sample hung = At(0f, 0.4f, true);
            if (hung.ThighL <= FallThigh || hung.ThighL >= TuckThigh) return false;
            if (hung.ArmYawL <= TuckArmYaw || hung.ArmYawL >= FallArmYaw) return false;

            Sample fall = At(-16f, 0.5f, true);
            if (fall.ThighL > 25f || fall.ThighR > 25f) return false;
            if (fall.KneeL < -30f || fall.KneeR < -30f) return false;
            if (fall.ArmYawL < 50f) return false;
            if (fall.ArmYawL < rise.ArmYawL + 25f) return false;

            Sample full = Mixed(24.7f, 0.2f, true, 12f, sinC, cycle);
            if (Mathf.Abs(full.ThighL - rise.ThighL) > 0.05f) return false;
            if (Mathf.Abs(full.ArmYawL - rise.ArmYawL) > 0.05f) return false;

            Sample right = At(24.7f, 0f, false);
            if (right.ThighR <= right.ThighL) return false;
            if (right.KneeR >= right.KneeL) return false;
            Sample left = At(24.7f, 0f, true);
            if (left.ThighL <= left.ThighR) return false;
            if (left.KneeL >= left.KneeR) return false;
            return true;
        }

        public static string ProofLine()
        {
            const float cycle = 0.9f;
            float sinC = Mathf.Sin(cycle);
            Sample stride = Stride(12f, sinC, cycle);
            Sample take = Mixed(24.7f, 0f, true, 12f, sinC, cycle);
            Sample tuck = At(24.7f, 0.2f, true);
            Sample fall = At(-16f, 0.5f, true);
            return "jump pose"
                + " takeoff=" + TakeoffSeconds.ToString("0.00")
                + " crouchHip=" + CrouchHip.ToString("0")
                + " crouchSpine=" + CrouchSpine.ToString("0")
                + " crouchKnee=" + CrouchKnee.ToString("0")
                + " plantThigh=" + PlantThigh.ToString("0")
                + " driveThigh=" + DriveThigh.ToString("0")
                + " driveKnee=" + DriveKnee.ToString("0")
                + " swingPitch=" + SwingArmPitch.ToString("0")
                + " swingYaw=" + SwingArmYaw.ToString("0")
                + " swingElbow=" + SwingElbow.ToString("0")
                + " tuckThigh=" + TuckThigh.ToString("0")
                + " tuckKnee=" + TuckKnee.ToString("0")
                + " tuckPitch=" + TuckArmPitch.ToString("0")
                + " tuckYaw=" + TuckArmYaw.ToString("0")
                + " tuckHip=" + TuckHip.ToString("0")
                + " fallThigh=" + FallThigh.ToString("0")
                + " fallKnee=" + FallKnee.ToString("0")
                + " fallPitch=" + FallArmPitch.ToString("0")
                + " fallYaw=" + FallArmYaw.ToString("0")
                + " extendRise=" + Extend(24.7f).ToString("0.00")
                + " extendApex=" + Extend(0f).ToString("0.00")
                + " extendFall=" + Extend(-16f).ToString("0.00")
                + " fromStride=" + FromStride(0f).ToString("0.00")
                + " leadThigh=" + take.ThighL.ToString("0.0")
                + " strideThigh=" + stride.ThighL.ToString("0.0")
                + " tuckRead=" + tuck.ThighL.ToString("0")
                + " fallRead=" + fall.ArmYawL.ToString("0")
                + " impulseDelay=" + ImpulseDelaySeconds.ToString("0.00")
                + " slew=" + TakeoffSlew.ToString("0")
                + " gate=takeoff:age 0.." + TakeoffSeconds.ToString("0.00") + " weight 1-(t/T)^2, slew " + TakeoffSlew.ToString("0")
                + "; extend:SmoothStep InverseLerp(vy " + RiseVy.ToString("0") + ".." + FallVy.ToString("0") + ") ignores planar speed"
                + "; stride:FromStride " + StrideLead.ToString("0.00") + " at fire -> 1 by " + StrideBlendSeconds.ToString("0.00")
                + " phase-matched drive, sprint and bunny-hop"
                + "; off:wall, slide-cancel, ski, dash, climb, air-crouch, punch, tag, miss, claim, grapple, ready, still-crouch, crouch-walk, soft/hard absorb"
                + "; land:pose off, JumpLandTell thud"
                + "; impulseDelay=0 rootMotion=0";
        }

        static Sample Lerp(Sample a, Sample b, float t)
        {
            return new Sample
            {
                ThighL = Mathf.Lerp(a.ThighL, b.ThighL, t),
                ThighR = Mathf.Lerp(a.ThighR, b.ThighR, t),
                KneeL = Mathf.Lerp(a.KneeL, b.KneeL, t),
                KneeR = Mathf.Lerp(a.KneeR, b.KneeR, t),
                ArmPitchL = Mathf.Lerp(a.ArmPitchL, b.ArmPitchL, t),
                ArmPitchR = Mathf.Lerp(a.ArmPitchR, b.ArmPitchR, t),
                ArmYawL = Mathf.Lerp(a.ArmYawL, b.ArmYawL, t),
                ArmYawR = Mathf.Lerp(a.ArmYawR, b.ArmYawR, t),
                ElbowL = Mathf.Lerp(a.ElbowL, b.ElbowL, t),
                ElbowR = Mathf.Lerp(a.ElbowR, b.ElbowR, t),
                Hip = Mathf.Lerp(a.Hip, b.Hip, t),
                Spine = Mathf.Lerp(a.Spine, b.Spine, t),
            };
        }

        static float ArmPitch(float phase, float amp)
        {
            float fwd = phase > 0f ? phase * amp : 0f;
            float back = phase < 0f ? -phase * amp * 0.22f : 0f;
            return -(fwd - back);
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
