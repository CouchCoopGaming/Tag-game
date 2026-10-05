using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual turn-in-place only. Grounded, slow, and a large heading change
    /// plants the outside foot, yaws the hips toward the new heading, then the
    /// chest, and steps the inside foot once speed builds. Sprint keeps the
    /// GaitBlend lean. Turn rate, move speed, and the yaw slew are not written
    /// here. No root motion.
    /// </summary>
    public static class PivotPose
    {
        public const bool RootMotion = false;

        /// <summary>Smoothstep on, and back onto the gait. The gait wins.</summary>
        public const float BlendSeconds = 0.14f;
        /// <summary>Bone follow. The 0.1s yaw slew stays on the locomotor.</summary>
        public const float Slew = 36f;

        /// <summary>Below this, the turn is a lean or a walk plant, not a pivot.</summary>
        public const float TurnEnter = 0.42f;
        /// <summary>Full plant and twist. 1 is the existing yaw visual at full scale.</summary>
        public const float TurnFull = 0.72f;

        /// <summary>Lead step is in. The pose is still fully on.</summary>
        public const float LeadFull = 1.6f;
        /// <summary>Pose stays on through a near-idle walk.</summary>
        public const float SpeedHold = 2.2f;
        /// <summary>Pose is gone. The gait owns the legs. Well under a walk.</summary>
        public const float SpeedOff = 3.4f;

        /// <summary>Hips reach the new heading at this share of the blend. Chest is behind.</summary>
        public const float HipFullAt = 0.58f;
        /// <summary>Chest yaw starts after the hips have begun.</summary>
        public const float ChestDelay = 0.32f;
        /// <summary>Chest catches the heading at the end of the blend.</summary>
        public const float ChestSpan = 0.68f;

        public const float HipYaw = 32f;
        public const float ChestYaw = 18f;
        /// <summary>Outside foot stays back. Positive pitch is a forward reach.</summary>
        public const float PlantThigh = -8f;
        public const float PlantKnee = -14f;
        /// <summary>Outside foot yaws against the turn so the toe stays.</summary>
        public const float PlantYaw = 22f;
        /// <summary>Inside foot, short of a walk reach.</summary>
        public const float LeadThigh = 24f;
        public const float LeadKnee = -36f;
        public const float StandKnee = -8f;

        /// <summary>0 at rest, 1 at a large turn. Speed still has to be low.</summary>
        public static float TurnWeight(float turnAbs)
        {
            float a = turnAbs < 0f ? 0f : turnAbs;
            if (a <= TurnEnter) return 0f;
            if (a >= TurnFull) return 1f;
            return PoseHandoff.Ease((a - TurnEnter) / (TurnFull - TurnEnter));
        }

        /// <summary>1 at idle and a near-idle walk. 0 once the gait should own the stride.</summary>
        public static float SpeedWeight(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            if (s <= SpeedHold) return 1f;
            if (s >= SpeedOff) return 0f;
            return 1f - PoseHandoff.Ease((s - SpeedHold) / (SpeedOff - SpeedHold));
        }

        /// <summary>0 while the outside foot is planted. 1 once speed has built a short step.</summary>
        public static float Lead01(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            float gate = GaitBlend.IdleGate;
            if (s <= gate) return 0f;
            if (s >= LeadFull) return 1f;
            return PoseHandoff.Ease((s - gate) / (LeadFull - gate));
        }

        /// <summary>
        /// 0 below a run, 1 at sprint. The walk plant fades out.
        /// The sprint turn is the GaitBlend lean.
        /// </summary>
        public static float SprintLean01(float planarSpeed)
        {
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            float a = GaitBlend.RunSpeed;
            float b = GaitBlend.SprintSpeed;
            if (s <= a) return 0f;
            if (s >= b) return 1f;
            return PoseHandoff.Ease((s - a) / (b - a));
        }

        public static bool KeepsSprintLean(float planarSpeed)
        {
            return planarSpeed >= GaitBlend.SprintSpeed;
        }

        /// <summary>1 when a grounded slow large turn should read as a pivot.</summary>
        public static float Weight(float planarSpeed, float turnAbs)
        {
            if (KeepsSprintLean(planarSpeed)) return 0f;
            float speed = SpeedWeight(planarSpeed);
            float turn = TurnWeight(turnAbs);
            return speed * turn;
        }

        public static bool Engaged(float planarSpeed, float turnAbs)
        {
            return Weight(planarSpeed, turnAbs) > 0.001f;
        }

        /// <summary>
        /// Hips arrive first. Chest follows. blend01 is 0 at the start of the
        /// window and 1 at the end. Both are 1 once the pivot is holding.
        /// </summary>
        public static void Twist(float blend01, out float hip01, out float chest01)
        {
            float u = blend01 < 0f ? 0f : (blend01 > 1f ? 1f : blend01);
            float hipSpan = HipFullAt > 0.0001f ? HipFullAt : 1f;
            hip01 = PoseHandoff.Ease(u / hipSpan);
            float span = ChestSpan > 0.0001f ? ChestSpan : 1f;
            chest01 = PoseHandoff.Ease((u - ChestDelay) / span);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (BlendSeconds < 0.10f || BlendSeconds > 0.18f) return false;
            if (Slew < 28f || Slew > 48f) return false;
            if (TurnEnter < 0.30f || TurnEnter >= TurnFull) return false;
            if (TurnFull > 1f) return false;
            if (LeadFull <= GaitBlend.IdleGate) return false;
            if (SpeedHold < LeadFull || SpeedOff <= SpeedHold) return false;
            if (SpeedOff >= GaitBlend.WalkSpeed) return false;
            if (HipFullAt <= ChestDelay || HipFullAt >= 0.85f) return false;
            if (ChestDelay + ChestSpan < 0.99f) return false;
            if (HipYaw < 20f || ChestYaw < 12f) return false;
            if (PlantThigh >= 0f || LeadThigh <= 12f || LeadThigh >= 34f) return false;
            if (PlantYaw < 12f) return false;
            if (PlantKnee >= 0f || LeadKnee >= StandKnee) return false;

            if (TurnWeight(0f) > 0.0001f || TurnWeight(TurnEnter) > 0.0001f) return false;
            if (Mathf.Abs(TurnWeight(TurnFull) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(TurnWeight(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(SpeedWeight(0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(SpeedWeight(GaitBlend.IdleGate) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(SpeedWeight(SpeedHold) - 1f) > 0.0001f) return false;
            if (SpeedWeight(SpeedOff) > 0.0001f) return false;
            if (SpeedWeight(GaitBlend.WalkSpeed) > 0.0001f) return false;
            if (SpeedWeight(GaitBlend.SprintSpeed) > 0.0001f) return false;

            if (Lead01(0f) > 0.0001f || Lead01(GaitBlend.IdleGate) > 0.0001f) return false;
            if (Mathf.Abs(Lead01(LeadFull) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Lead01(SpeedOff) - 1f) > 0.0001f) return false;
            float prevLead = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float lead = Lead01(Mathf.Lerp(0f, LeadFull, i / 8f));
                if (lead + 0.0001f < prevLead) return false;
                prevLead = lead;
            }

            if (SprintLean01(0f) > 0.0001f || SprintLean01(GaitBlend.WalkSpeed) > 0.0001f) return false;
            if (SprintLean01(GaitBlend.RunSpeed) > 0.0001f) return false;
            if (Mathf.Abs(SprintLean01(GaitBlend.SprintSpeed) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(SprintLean01((GaitBlend.RunSpeed + GaitBlend.SprintSpeed) * 0.5f) - 0.5f) > 0.0001f) return false;
            if (!KeepsSprintLean(GaitBlend.SprintSpeed) || KeepsSprintLean(GaitBlend.WalkSpeed)) return false;
            if (KeepsSprintLean(0f) || KeepsSprintLean(SpeedOff)) return false;

            if (Weight(0f, 0f) > 0.0001f || Weight(0f, TurnEnter) > 0.0001f) return false;
            if (Mathf.Abs(Weight(0f, TurnFull) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Weight(GaitBlend.IdleGate, 1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Weight(SpeedHold, 1f) - 1f) > 0.0001f) return false;
            if (Weight(SpeedOff, 1f) > 0.0001f) return false;
            if (Weight(GaitBlend.WalkSpeed, 1f) > 0.0001f) return false;
            if (Weight(GaitBlend.RunSpeed, 1f) > 0.0001f) return false;
            if (Weight(GaitBlend.SprintSpeed, 1f) > 0.0001f) return false;
            if (!Engaged(0f, 1f) || Engaged(0f, 0.1f) || Engaged(GaitBlend.SprintSpeed, 1f)) return false;
            if (Engaged(GaitBlend.WalkSpeed, 1f)) return false;

            Twist(0f, out float hip0, out float chest0);
            if (hip0 > 0.0001f || chest0 > 0.0001f) return false;
            Twist(1f, out float hip1, out float chest1);
            if (Mathf.Abs(hip1 - 1f) > 0.0001f || Mathf.Abs(chest1 - 1f) > 0.0001f) return false;
            Twist(0.20f, out float hipEarly, out float chestEarly);
            if (chestEarly > 0.0001f || hipEarly < 0.08f) return false;
            Twist(0.45f, out float hipMid, out float chestMid);
            if (hipMid < chestMid + 0.35f) return false;
            if (hipMid < 0.70f || chestMid > 0.40f) return false;
            float prevHip = -1f;
            float prevChest = -1f;
            for (int i = 0; i <= 8; i++)
            {
                Twist(i / 8f, out float hip, out float chest);
                if (hip + 0.0001f < prevHip || chest + 0.0001f < prevChest) return false;
                if (i > 0 && i < 8 && chest > hip + 0.0001f) return false;
                prevHip = hip;
                prevChest = chest;
            }

            float planted = GaitBlend.StanceWorldPitch(PlantThigh, PlantKnee, GaitBlend.SoleLevelDeg(PlantThigh, PlantKnee));
            float stepped = GaitBlend.StanceWorldPitch(LeadThigh, LeadKnee, GaitBlend.SoleLevelDeg(LeadThigh, LeadKnee));
            if (Mathf.Abs(planted) > 0.05f || Mathf.Abs(stepped) > 0.05f) return false;
            if (Mathf.Abs(GaitBlend.TurnLeanWalk - 4.5f) > 0.01f) return false;
            if (Mathf.Abs(GaitBlend.TurnLeanSprint - 9f) > 0.01f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Twist(0.20f, out float hipEarly, out float chestEarly);
            Twist(0.45f, out float hipMid, out float chestMid);
            float idle = Weight(0f, 1f);
            float walk = Weight(GaitBlend.WalkSpeed, 1f);
            float sprint = Weight(GaitBlend.SprintSpeed, 1f);
            return "pivot pose"
                + " blend=" + BlendSeconds.ToString("0.00")
                + " idle=" + idle.ToString("0.00")
                + " walk=" + walk.ToString("0.00")
                + " sprint=" + sprint.ToString("0.00")
                + " leadIdle=" + Lead01(0f).ToString("0.00")
                + " leadStep=" + Lead01(LeadFull).ToString("0.00")
                + " hipEarly=" + hipEarly.ToString("0.00")
                + " chestEarly=" + chestEarly.ToString("0.00")
                + " hipMid=" + hipMid.ToString("0.00")
                + " chestMid=" + chestMid.ToString("0.00")
                + " hipYaw=" + HipYaw.ToString("0")
                + " chestYaw=" + ChestYaw.ToString("0")
                + " plant=" + PlantThigh.ToString("0")
                + " lead=" + LeadThigh.ToString("0")
                + " sprintLean=" + SprintLean01(GaitBlend.SprintSpeed).ToString("0.00")
                + " turnLean=" + GaitBlend.TurnLeanWalk.ToString("0.0") + "-" + GaitBlend.TurnLeanSprint.ToString("0.0")
                + " gate=grounded speed<=" + SpeedHold.ToString("0.0")
                + " fade " + SpeedOff.ToString("0.0")
                + " turn " + TurnEnter.ToString("0.00") + ".." + TurnFull.ToString("0.00")
                + "; hips then chest; outside plant; lead step once speed builds"
                + "; sprint keeps GaitBlend lean"
                + "; yaw slew untouched"
                + "; rootMotion=0";
        }
    }
}
