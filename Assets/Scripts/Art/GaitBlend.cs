using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual stride only. Cadence and amplitude are smooth functions of planar
    /// speed, and the stance thigh pitches back so the sole stays with the ground.
    /// This does not move the root and does not change motor speeds.
    /// </summary>
    public static class GaitBlend
    {
        public const float IdleGate = 0.35f;
        public const float WalkSpeed = 6f;
        public const float RunSpeed = 9f;
        public const float SprintSpeed = 12f;
        /// <summary>Hip to sole, meters. Used only to pitch the stance thigh.</summary>
        public const float LegLength = 0.90f;
        /// <summary>Idle into the stride, and the stride back to idle.</summary>
        public const float IdleBlendSeconds = 0.18f;
        public const float AccelLeanDeg = 6.5f;
        public const float TurnLeanWalk = 4.5f;
        public const float TurnLeanSprint = 9f;
        public const float CadenceWalkMin = 5.2f;
        public const float CadenceSprint = 11.2f;
        /// <summary>Meters above the idle gate before cadence is fully on. Keeps the first step from buzzing.</summary>
        public const float CadenceGateSpan = 2.4f;
        public const float PlantCapDeg = 26f;
        public const float PlantDuty = 0.42f;
        /// <summary>Share of the reach already pitched back on the stance thigh.</summary>
        public const float StrideTrail = 0.58f;

        /// <summary>
        /// 0 at the idle gate, 1 at sprint. Ease-out, so a walk is already a stride
        /// and a sprint only finishes the reach. Continuous, no gait switch.
        /// </summary>
        public static float PoseWeight(float planarSpeed)
        {
            float t = Inv(IdleGate, SprintSpeed, planarSpeed);
            return 1f - (1f - t) * (1f - t);
        }

        /// <summary>Radians per second. 0 at rest. Rises through walk, run, and sprint.</summary>
        public static float CadenceAt(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            float t = Inv(IdleGate, SprintSpeed, s);
            float smooth = t * t * (3f - 2f * t);
            float gate = Mathf.SmoothStep(0f, 1f, Inv(IdleGate, IdleGate + CadenceGateSpan, s));
            return Mathf.Lerp(CadenceWalkMin, CadenceSprint, smooth) * gate;
        }

        public static float ArmAmp(float poseWeight)
        {
            return Mathf.Lerp(36f, 64f, Mathf.Clamp01(poseWeight));
        }

        public static float ThighReach(float poseWeight)
        {
            float w = Mathf.Clamp01(poseWeight);
            return Mathf.Lerp(34f, 58f, w) * Mathf.Lerp(0.96f, 1.16f, w);
        }

        public static float KneeBend(float poseWeight)
        {
            return Mathf.Lerp(48f, 90f, Mathf.Clamp01(poseWeight));
        }

        /// <summary>
        /// Degrees to pitch the stance thigh back. stancePhase is 0 at heel strike
        /// and 1 at toe-off. The ends are 0 so the plant does not pop on or off.
        /// </summary>
        public static float PlantCounterDeg(float planarSpeed, float cadence, float stancePhase01)
        {
            if (cadence < 0.05f) return 0f;
            float u = Mathf.Clamp01(stancePhase01);
            float contact = Mathf.Sin(u * 3.14159265f);
            float stanceTime = contact * (3.14159265f / cadence) * PlantDuty;
            float moved = (planarSpeed < 0f ? 0f : planarSpeed) * stanceTime;
            float deg = Mathf.Atan(moved / LegLength) * Mathf.Rad2Deg;
            return Mathf.Min(deg, PlantCapDeg);
        }

        /// <summary>Rearward pitch the stride already applies at this contact. 0..1 contact.</summary>
        public static float StrideRearDeg(float thighReach, float contact01)
        {
            float reach = thighReach < 0f ? 0f : thighReach;
            float contact = contact01 < 0f ? 0f : (contact01 > 1f ? 1f : contact01);
            return StrideTrail * reach * contact;
        }

        /// <summary>
        /// Plant angle the stride does not already cover. Adding the full plant
        /// on top of the trail pitches the sole through the ground.
        /// </summary>
        public static float PlantResidualDeg(float strideRearDeg, float plantDeg)
        {
            float rear = strideRearDeg < 0f ? 0f : strideRearDeg;
            float plant = plantDeg < 0f ? 0f : plantDeg;
            float extra = plant - rear;
            return extra > 0f ? extra : 0f;
        }

        /// <summary>Local foot pitch that cancels thigh + knee so the sole stays level.</summary>
        public static float SoleLevelDeg(float thighPitchDeg, float kneePitchDeg)
        {
            return -(thighPitchDeg + kneePitchDeg);
        }

        public static float StanceWorldPitch(float thighPitchDeg, float kneePitchDeg, float footPitchDeg)
        {
            return thighPitchDeg + kneePitchDeg + footPitchDeg;
        }

        /// <summary>Spine pitch from forward accel, m/s^2. Positive leans into the push.</summary>
        public static float AccelLean(float accelForward)
        {
            float n = accelForward / 48f;
            if (n > 1f) n = 1f;
            if (n < -1f) n = -1f;
            return n * AccelLeanDeg;
        }

        public static float TurnLeanDeg(float turn01, float poseWeight)
        {
            float mag = turn01 < 0f ? -turn01 : turn01;
            if (mag > 1f) mag = 1f;
            float sign = turn01 < 0f ? -1f : 1f;
            return sign * Mathf.Lerp(TurnLeanWalk, TurnLeanSprint, Mathf.Clamp01(poseWeight)) * mag;
        }

        public static bool Holds()
        {
            if (PoseWeight(0f) > 0.0001f || PoseWeight(IdleGate) > 0.0001f) return false;
            if (Mathf.Abs(PoseWeight(SprintSpeed) - 1f) > 0.0001f) return false;
            if (CadenceAt(0f) > 0.0001f || CadenceAt(IdleGate) > 0.0001f) return false;
            float poseWalk = PoseWeight(WalkSpeed);
            float poseRun = PoseWeight(RunSpeed);
            float poseSprint = PoseWeight(SprintSpeed);
            if (!(poseWalk < poseRun && poseRun < poseSprint)) return false;
            float cadWalk = CadenceAt(WalkSpeed);
            float cadRun = CadenceAt(RunSpeed);
            float cadSprint = CadenceAt(SprintSpeed);
            if (!(cadWalk < cadRun && cadRun < cadSprint)) return false;
            if (Mathf.Abs(cadSprint - CadenceSprint) > 0.02f) return false;
            if (!(ThighReach(poseWalk) < ThighReach(poseRun) && ThighReach(poseRun) < ThighReach(poseSprint))) return false;
            if (!(ArmAmp(poseWalk) < ArmAmp(poseRun) && KneeBend(poseWalk) < KneeBend(poseSprint))) return false;
            float prevPose = 0f;
            float prevCad = 0f;
            for (int i = 0; i <= 60; i++)
            {
                float s = i * 0.2f;
                float pose = PoseWeight(s);
                float cad = CadenceAt(s);
                if (pose + 0.0001f < prevPose) return false;
                if (cad + 0.0001f < prevCad) return false;
                if (i > 0 && pose - prevPose > 0.08f) return false;
                if (i > 0 && cad - prevCad > 1.2f) return false;
                prevPose = pose;
                prevCad = cad;
            }
            if (PlantCounterDeg(WalkSpeed, cadWalk, 0f) > 0.05f) return false;
            if (PlantCounterDeg(WalkSpeed, cadWalk, 1f) > 0.05f) return false;
            float plant = PlantCounterDeg(WalkSpeed, cadWalk, 0.5f);
            if (plant < 8f) return false;
            if (PlantCounterDeg(0f, cadWalk, 0.5f) > 0.05f) return false;
            if (Mathf.Abs(AccelLean(0f)) > 0.0001f) return false;
            if (AccelLean(48f) < AccelLeanDeg - 0.01f) return false;
            if (AccelLean(-48f) > -AccelLeanDeg + 0.01f) return false;
            if (TurnLeanDeg(1f, 0f) < TurnLeanWalk - 0.01f) return false;
            if (TurnLeanDeg(1f, 1f) < TurnLeanSprint - 0.01f) return false;
            if (IdleBlendSeconds < 0.12f) return false;

            float reachWalk = ThighReach(poseWalk);
            float rearWalk = StrideRearDeg(reachWalk, 1f);
            float residual = PlantResidualDeg(rearWalk, plant);
            float totalRear = rearWalk + residual;
            if (Mathf.Abs(totalRear - Mathf.Max(rearWalk, plant)) > 0.05f) return false;
            if (residual > plant + 0.01f) return false;
            float thigh = -totalRear;
            float knee = -2f;
            float sole = SoleLevelDeg(thigh, knee);
            if (Mathf.Abs(StanceWorldPitch(thigh, knee, sole)) > 0.05f) return false;
            float dug = StanceWorldPitch(-(rearWalk + plant), -(2f + plant * 0.4f), 0f);
            if (!(dug < -25f)) return false;
            if (Mathf.Abs(PlantResidualDeg(4f, 20f) - 16f) > 0.01f) return false;
            if (PlantResidualDeg(30f, 10f) > 0.01f) return false;
            return true;
        }

        public static string ProofLine()
        {
            float poseWalk = PoseWeight(WalkSpeed);
            float poseRun = PoseWeight(RunSpeed);
            float poseSprint = PoseWeight(SprintSpeed);
            float cadWalk = CadenceAt(WalkSpeed);
            float cadRun = CadenceAt(RunSpeed);
            float cadSprint = CadenceAt(SprintSpeed);
            float plant = PlantCounterDeg(WalkSpeed, cadWalk, 0.5f);
            float rearWalk = StrideRearDeg(ThighReach(poseWalk), 1f);
            float residual = PlantResidualDeg(rearWalk, plant);
            float thigh = -(rearWalk + residual);
            float knee = -2f;
            float sole = SoleLevelDeg(thigh, knee);
            float dug = StanceWorldPitch(-(rearWalk + plant), -(2f + plant * 0.4f), 0f);
            return "gait blend"
                + " poseWalk=" + poseWalk.ToString("0.00")
                + " poseRun=" + poseRun.ToString("0.00")
                + " poseSprint=" + poseSprint.ToString("0.00")
                + " cadenceWalk=" + cadWalk.ToString("0.00")
                + " cadenceRun=" + cadRun.ToString("0.00")
                + " cadenceSprint=" + cadSprint.ToString("0.00")
                + " armWalk=" + ArmAmp(poseWalk).ToString("0.0")
                + " armSprint=" + ArmAmp(poseSprint).ToString("0.0")
                + " thighWalk=" + ThighReach(poseWalk).ToString("0.0")
                + " thighSprint=" + ThighReach(poseSprint).ToString("0.0")
                + " kneeWalk=" + KneeBend(poseWalk).ToString("0.0")
                + " kneeSprint=" + KneeBend(poseSprint).ToString("0.0")
                + " plant=" + plant.ToString("0.0")
                + " rear=" + rearWalk.ToString("0.0")
                + " residual=" + residual.ToString("0.0")
                + " sole=" + StanceWorldPitch(thigh, knee, sole).ToString("0.0")
                + " dugWas=" + dug.ToString("0.0")
                + " accelLean=" + AccelLeanDeg.ToString("0.0")
                + " turnLean=" + TurnLeanWalk.ToString("0.0") + "-" + TurnLeanSprint.ToString("0.0")
                + " idleBlend=" + IdleBlendSeconds.ToString("0.00")
                + " gate=pose:ease-out InverseLerp(" + IdleGate.ToString("0.00") + "," + SprintSpeed.ToString("0") + ")"
                + "; cadence:SmoothStep*gate(" + IdleGate.ToString("0.00") + ".." + (IdleGate + CadenceGateSpan).ToString("0.00") + ") Lerp("
                + CadenceWalkMin.ToString("0.0") + "," + CadenceSprint.ToString("0.0") + ")"
                + "; plant:sin(phase)*Atan(speed*" + PlantDuty.ToString("0.00") + "*pi/cadence/" + LegLength.ToString("0.00") + ") cap " + PlantCapDeg.ToString("0")
                + "; start/stop:dt/" + IdleBlendSeconds.ToString("0.00")
                + "; lean:accel/48*" + AccelLeanDeg.ToString("0.0") + " spine+hips, turn Lerp("
                + TurnLeanWalk.ToString("0.0") + "," + TurnLeanSprint.ToString("0.0") + ",pose) smooth |yaw|";
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
