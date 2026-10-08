using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Recovery poses for the end of each verb. Three beats: the shape the
    /// move finishes in, the readable exit, then a shape the gait can cover.
    /// Visual only. Durations live on <see cref="VerbExitClock"/>.
    /// </summary>
    public static class VerbExitClips
    {
        public const bool RootMotion = false;
        /// <summary>Arms trail the torso. The head trails further and settles last.</summary>
        public const float ArmLag = 0.10f;
        public const float HeadLag = 0.16f;

        public static VerbExitSample At(VerbExitId id, float u, float fallScale, bool stepDown, bool shoulderLeft)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            if (id == VerbExitId.Roll || id == VerbExitId.RollAbsorb)
                return LagRoll(id, u, shoulderLeft);
            float e = PoseHandoff.Ease(u);
            float armU = e - ArmLag;
            float headU = e - HeadLag;
            if (armU < 0f) armU = 0f;
            if (headU < 0f) headU = 0f;
            VerbExitSample torso = Raw(id, e, fallScale, stepDown, shoulderLeft);
            VerbExitSample arms = Raw(id, armU, fallScale, stepDown, shoulderLeft);
            VerbExitSample head = Raw(id, headU, fallScale, stepDown, shoulderLeft);
            torso.ArmPitchL = arms.ArmPitchL;
            torso.ArmPitchR = arms.ArmPitchR;
            torso.ArmYawL = arms.ArmYawL;
            torso.ArmYawR = arms.ArmYawR;
            torso.ArmRollL = arms.ArmRollL;
            torso.ArmRollR = arms.ArmRollR;
            torso.ElbowL = arms.ElbowL;
            torso.ElbowR = arms.ElbowR;
            torso.Head = head.Head;
            torso.HeadYaw = head.HeadYaw;
            float wobble = Mathf.Sin(e * 3.14159265f);
            torso.HipYaw += 8f * wobble;
            return torso;
        }

        static VerbExitSample LagRoll(VerbExitId id, float u, bool shoulderLeft)
        {
            float armU = u - 0.08f;
            float headU = u - 0.12f;
            if (armU < 0f) armU = 0f;
            if (headU < 0f) headU = 0f;
            VerbExitSample torso = id == VerbExitId.Roll
                ? LandingRollPose.RollAt(u, shoulderLeft)
                : LandingRollPose.AbsorbAt(u);
            VerbExitSample arms = id == VerbExitId.Roll
                ? LandingRollPose.RollAt(armU, shoulderLeft)
                : LandingRollPose.AbsorbAt(armU);
            VerbExitSample head = id == VerbExitId.Roll
                ? LandingRollPose.RollAt(headU, shoulderLeft)
                : LandingRollPose.AbsorbAt(headU);
            torso.ArmPitchL = arms.ArmPitchL;
            torso.ArmPitchR = arms.ArmPitchR;
            torso.ArmYawL = arms.ArmYawL;
            torso.ArmYawR = arms.ArmYawR;
            torso.ArmRollL = arms.ArmRollL;
            torso.ArmRollR = arms.ArmRollR;
            torso.ElbowL = arms.ElbowL;
            torso.ElbowR = arms.ElbowR;
            torso.Head = head.Head;
            torso.HeadYaw = head.HeadYaw;
            return torso;
        }

        static VerbExitSample Raw(VerbExitId id, float u, float fallScale, bool stepDown, bool shoulderLeft)
        {
            if (id == VerbExitId.SoftLand)
                return LandingRollPose.LandAt(u, fallScale);
            VerbExitSample a;
            VerbExitSample b;
            VerbExitSample c;
            Keys(id, stepDown, out a, out b, out c);
            VerbExitSample s = u < 0.5f
                ? VerbExitSample.Lerp(a, b, u * 2f)
                : VerbExitSample.Lerp(b, c, (u - 0.5f) * 2f);
            if (shoulderLeft && (id == VerbExitId.WallRun || id == VerbExitId.WallJump
                || id == VerbExitId.Vault || id == VerbExitId.Slide || id == VerbExitId.ClimbTopOut))
                s = VerbExitSample.Mirror(s);
            return s;
        }

        /// <summary>Mid beat. The proof uses this to tell exits apart.</summary>
        public static VerbExitSample Mid(VerbExitId id, bool stepDown)
        {
            return At(id, 0.5f, 1f, stepDown, false);
        }

        static void Keys(VerbExitId id, bool stepDown, out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            switch (id)
            {
                case VerbExitId.WallRun:
                    WallRun(stepDown, out a, out b, out c);
                    return;
                case VerbExitId.WallJump:
                    WallJump(out a, out b, out c);
                    return;
                case VerbExitId.ClimbTopOut:
                    ClimbTop(out a, out b, out c);
                    return;
                case VerbExitId.ClingDrop:
                    Cling(out a, out b, out c);
                    return;
                case VerbExitId.Vault:
                    Vault(out a, out b, out c);
                    return;
                case VerbExitId.Mantle:
                    Mantle(out a, out b, out c);
                    return;
                case VerbExitId.Slide:
                    Slide(out a, out b, out c);
                    return;
                case VerbExitId.AirDash:
                    Dash(out a, out b, out c);
                    return;
                case VerbExitId.Punch:
                    Punch(out a, out b, out c);
                    return;
                case VerbExitId.Lunge:
                    Lunge(out a, out b, out c);
                    return;
                case VerbExitId.ZipDrop:
                    Zip(out a, out b, out c);
                    return;
                case VerbExitId.LaunchLand:
                    Launch(out a, out b, out c);
                    return;
                case VerbExitId.GrappleArrive:
                    Arrive(out a, out b, out c);
                    return;
                case VerbExitId.GrappleRelease:
                    Release(out a, out b, out c);
                    return;
                case VerbExitId.Stagger:
                    Stagger(out a, out b, out c);
                    return;
                case VerbExitId.TagBackEnd:
                    TagBack(out a, out b, out c);
                    return;
                default:
                    Soft(out a, out b, out c);
                    return;
            }
        }

        static VerbExitSample P(
            float hip, float spine, float head,
            float tL, float tR, float kL, float kR,
            float aPL, float aPR, float aYL, float aYR,
            float eL, float eR)
        {
            VerbExitSample s = default;
            s.Hip = hip;
            s.Spine = spine;
            s.Head = head;
            s.ThighL = tL;
            s.ThighR = tR;
            s.KneeL = kL;
            s.KneeR = kR;
            s.ArmPitchL = aPL;
            s.ArmPitchR = aPR;
            s.ArmYawL = aYL;
            s.ArmYawR = aYR;
            s.ElbowL = eL;
            s.ElbowR = eR;
            return s;
        }

        /// <summary>Push off the wall, or reach a foot down. The outside arm leads.</summary>
        static void WallRun(bool stepDown, out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 8f, -8f, 28f, 12f, -28f, -16f, -36f, 14f, -12f, -8f, -16f, -14f);
            a.HipRoll = -10f;
            a.SpineRoll = -8f;
            a.ThighRollL = WallPose.PlantRoll;
            if (stepDown)
            {
                b = P(12f, 10f, 2f, 36f, 8f, -24f, -20f, -28f, 10f, -10f, -6f, -14f, -12f);
                b.Drop = 0.03f;
                b.RootPitch = 8f;
                b.ThighRollL = WallPose.PlantRoll;
            }
            else
            {
                b = P(-8f, -4f, -2f, 44f, 6f, -18f, -12f, 18f, 12f, -14f, 8f, -12f, -16f);
                b.HipRoll = 6f;
                b.SpineRoll = 4f;
                b.ThighRollL = WallPose.PlantRoll;
            }
            // Full plant abduction, arms inside the shoe. A short roll lets the hand enter the wall.
            c = P(4f, 2f, 0f, 22f, 8f, -18f, -12f, -28f, -10f, -6f, 4f, -14f, -10f);
            c.ThighRollL = WallPose.PlantRoll;
        }

        /// <summary>Tuck, then the body opens into the air stride.</summary>
        static void WallJump(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(8f, 6f, -4f, 48f, 36f, -36f, -42f, -28f, 16f, -14f, 8f, -18f, -14f);
            a.SpineRoll = 4f;
            a.ThighRollL = WallPose.PlantRoll;
            b = P(10f, 8f, -6f, 42f, 28f, -32f, -36f, -22f, 12f, -12f, 6f, -16f, -12f);
            b.Head = -8f;
            b.ThighRollL = WallPose.PlantRoll;
            c = P(-4f, -6f, 2f, 22f, 10f, -18f, -12f, -24f, -8f, -6f, 4f, -12f, -10f);
            c.ThighRollL = WallPose.PlantRoll;
            c.ArmRollL = -12f;
            c.ArmRollR = 12f;
        }

        /// <summary>Lead knee up on the lid, then both feet, then a stride. Pelvis stays at the standing capsule.</summary>
        static void ClimbTop(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // The exit starts after the mantle has already stood the capsule up.
            // Hip flexion carries the chest. The spine stays a small share of that.
            // The plant thigh reaches forward of the pelvis. No root drop.
            a = P(16f, 4f, -8f, -10f, 16f, -24f, -86f, -36f, -10f, -6f, 2f, -16f, -14f);
            b = P(14f, 4f, 0f, -8f, -4f, -32f, -28f, -34f, -16f, -6f, 2f, -16f, -14f);
            c = P(10f, 4f, 6f, -4f, 12f, -22f, -16f, -50f, -18f, -6f, 2f, -16f, -14f);
        }

        /// <summary>Hands open off the wall and the body drops into the fall.</summary>
        static void Cling(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(4f, 4f, -6f, 24f, 14f, -22f, -14f, -40f, -34f, -16f, 10f, -14f, -12f);
            a.ThighRollL = WallPose.PlantRoll;
            // Negative left yaw is off the wall. Positive left yaw drives the upper arm into it.
            b = P(-14f, -6f, 8f, 16f, 12f, -34f, -28f, -22f, -18f, -28f, 24f, -20f, -16f);
            b.SpineYaw = -8f;
            b.ThighRollL = WallPose.PlantRoll;
            c = P(4f, 2f, -4f, 18f, 8f, -16f, -12f, -20f, -14f, -16f, 14f, -14f, -12f);
            c.ThighRollL = WallPose.PlantRoll;
        }

        /// <summary>Trail leg clears, then the landing sits the hips behind both feet.</summary>
        static void Vault(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // The trail thigh stays at 54°. Straighter than that walks into the spine.
            // The lead foot is the support on the way over. Both feet then sit ahead
            // of the pelvis. Arm pitches stay negative so the pump does not stick out.
            a = P(12f, 4f, -12f, -12f, 54f, -32f, -82f, -22f, -10f, -6f, 2f, -16f, -14f);
            // The trail knee stays deep until the thigh is already in front, so the
            // foot does not land behind the pelvis on the way down.
            b = P(20f, 6f, 4f, -12f, -12f, -34f, -72f, -78f, -42f, -6f, 2f, -16f, -14f);
            b.SpineYaw = 16f;
            c = P(16f, 5f, 8f, -12f, -10f, -32f, -28f, -64f, -22f, -6f, 2f, -16f, -14f);
            c.SpineYaw = 8f;
        }

        /// <summary>Both feet on the lid, hips behind them. Not the climb's lead knee.</summary>
        static void Mantle(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // No thigh spread and no visual drop. The pelvis stays on the standing
            // capsule. A forward reach on the arms keeps this exit off the climb.
            a = P(18f, 6f, -8f, -12f, -8f, -32f, -26f, -18f, -36f, -6f, 2f, -16f, -14f);
            a.SpineYaw = -6f;
            b = P(16f, 5f, 2f, -12f, -8f, -28f, -24f, -22f, -70f, -6f, 2f, -18f, -16f);
            b.SpineYaw = -14f;
            c = P(14f, 4f, 6f, -12f, -8f, -28f, -22f, -16f, -40f, -6f, 2f, -16f, -14f);
            c.SpineYaw = -8f;
        }

        /// <summary>Rise through a forward lean. The arms pump. Slide boost is not here.</summary>
        static void Slide(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // Positive arm pitch swings the hand out to the side. Both pitches stay
            // negative so the pump stays in the line of the run.
            a = P(-16f, -10f, 6f, 38f, 22f, -22f, -58f, -42f, -22f, -6f, 2f, -24f, -20f);
            b = P(18f, 12f, -4f, 32f, 14f, -28f, -18f, -48f, -26f, -4f, 0f, -22f, -18f);
            c = P(10f, 6f, 0f, 26f, 12f, -20f, -14f, -36f, -20f, -4f, 2f, -16f, -14f);
        }

        /// <summary>The stretched dash settles back onto the air line.</summary>
        static void Dash(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(12f, 14f, -6f, 22f, -8f, -16f, -8f, 28f, 22f, -8f, 6f, -10f, -8f);
            a.SpineYaw = -6f;
            b = P(2f, 2f, 0f, 8f, 6f, -8f, -6f, -6f, -4f, -20f, 18f, -8f, -6f);
            c = P(0f, 0f, 0f, 6f, 4f, -6f, -4f, -12f, -10f, -14f, 12f, -8f, -8f);
        }

        /// <summary>Right arm folds back to the ribs and the weight sits back.</summary>
        static void Punch(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(8f, 6f, -4f, 16f, -10f, -8f, -6f, -20f, -72f, -16f, 12f, -18f, -6f);
            a.HipYaw = 18f;
            a.SpineYaw = 16f;
            b = P(-12f, -4f, 2f, 8f, -16f, -14f, -10f, -16f, -34f, -12f, 10f, -20f, -70f);
            b.HipYaw = -14f;
            b.SpineYaw = -8f;
            b.ArmRollR = -16f;
            c = P(2f, 0f, 0f, 10f, 4f, -10f, -8f, -14f, -18f, -14f, 12f, -12f, -36f);
        }

        /// <summary>The reach collapses, weight back, then the stride.</summary>
        static void Lunge(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(12f, 16f, -6f, 36f, -8f, -20f, -10f, -40f, -32f, -14f, 12f, -16f, -12f);
            b = P(-8f, -4f, 2f, 28f, 8f, -36f, -16f, -12f, -18f, -12f, 14f, -22f, -36f);
            b.HipYaw = -8f;
            c = P(4f, 2f, 0f, 18f, 8f, -16f, -10f, -16f, -12f, -16f, 14f, -10f, -12f);
        }

        /// <summary>Hands leave the cable and the body drops into the fall.</summary>
        static void Zip(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(-8f, -6f, -4f, 34f, 30f, -18f, -14f, -100f, -100f, -42f, 42f, -8f, -8f);
            b = P(-8f, -6f, -4f, 34f, 30f, -18f, -14f, -100f, -100f, -42f, 42f, -8f, -8f);
            c = P(-8f, -6f, -4f, 34f, 30f, -18f, -14f, -100f, -100f, -42f, 42f, -8f, -8f);
        }

        /// <summary>Pad arc opens, knees take the landing, then the stride. Not a roll.</summary>
        static void Launch(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(2f, 2f, 4f, 6f, 4f, -8f, -6f, -6f, -4f, -40f, 38f, -8f, -6f);
            b = P(10f, 8f, -2f, 28f, 22f, -36f, -28f, -16f, -12f, -18f, 16f, -18f, -16f);
            b.Drop = 0.04f;
            c = P(4f, 2f, 0f, 18f, 10f, -16f, -12f, -12f, -10f, -12f, 12f, -10f, -8f);
        }

        /// <summary>Left hand still leads as the body arrives on the pull.</summary>
        static void Arrive(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 4f, -6f, 18f, 10f, -16f, -10f, -70f, -24f, -28f, 24f, -14f, -12f);
            b = P(8f, 10f, -4f, 22f, 14f, -28f, -16f, -60f, -18f, -24f, 18f, -40f, -16f);
            b.SpineYaw = 8f;
            c = P(4f, 4f, 0f, 14f, 10f, -14f, -10f, -28f, -12f, -16f, 14f, -20f, -12f);
        }

        /// <summary>Left hand opens and folds in. The pull is already over.</summary>
        static void Release(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(4f, 6f, -2f, 12f, 8f, -12f, -8f, -64f, -18f, -22f, 18f, -16f, -12f);
            b = P(2f, 2f, 2f, 8f, 6f, -10f, -8f, -22f, -12f, -16f, 18f, -48f, -14f);
            b.ArmRollL = 18f;
            c = P(0f, 0f, 0f, 6f, 4f, -6f, -4f, -12f, -10f, -14f, 12f, -16f, -10f);
        }

        /// <summary>The stumble catches a step and the chest comes back up.</summary>
        static void Stagger(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(14f, 10f, 4f, 16f, -10f, -22f, -12f, -12f, 14f, -28f, 26f, -18f, -14f);
            a.SpineYaw = 18f;
            b = P(8f, 6f, 2f, 22f, 6f, -28f, -14f, -10f, -8f, -22f, 20f, -18f, -14f);
            b.HipYaw = 10f;
            b.SpineYaw = 14f;
            c = P(2f, 2f, 0f, 16f, 8f, -12f, -8f, -12f, -10f, -16f, 14f, -10f, -8f);
        }

        /// <summary>The immunity flinch shakes off. The glow is not this pose.</summary>
        static void TagBack(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 4f, -4f, 8f, -6f, -10f, -8f, -14f, -20f, -16f, 18f, -16f, -24f);
            a.SpineYaw = -12f;
            a.HipYaw = -8f;
            b = P(2f, 2f, 2f, 6f, 4f, -8f, -6f, 6f, -10f, -16f, 14f, -12f, -16f);
            b.SpineRoll = 16f;
            b.HipRoll = -6f;
            c = P(0f, 0f, 0f, 4f, 4f, -4f, -4f, -10f, -10f, -12f, 12f, -8f, -8f);
        }

        /// <summary>Knee bend for a landing under the roll gate. Depth scales with fall speed.</summary>
        static void Soft(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 4f, -2f, 16f, 16f, -18f, -18f, -12f, -10f, 10f, -10f, -10f, -8f);
            b = P(12f, 10f, -4f, 32f, 30f, -46f, -44f, -14f, -12f, 12f, -12f, -16f, -14f);
            b.Drop = 0.035f;
            b.RootPitch = 6f;
            c = P(2f, 1f, 0f, 12f, 8f, -10f, -8f, -12f, -10f, 12f, -12f, -8f, -8f);
        }
    }
}
