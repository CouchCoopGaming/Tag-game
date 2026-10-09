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
            {
                // The tier still scales the bend. Scale 1 is the full sit.
                VerbExitSample sa;
                VerbExitSample sb;
                VerbExitSample sc;
                Soft(out sa, out sb, out sc);
                VerbExitSample soft = u < 0.5f
                    ? VerbExitSample.Lerp(sa, sb, u * 2f)
                    : VerbExitSample.Lerp(sb, sc, (u - 0.5f) * 2f);
                return VerbExitSample.ScaleBend(soft, fallScale);
            }
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

        /// <summary>Both feet down, hips behind them. Arms are what make one exit readable from another.</summary>
        static VerbExitSample Sit(float head, float aPL, float aPR, float aYL, float aYR, float eL, float eR)
        {
            // Same legs as the vault landing that plants: symmetric thighs, shins forward,
            // hips bone down 20 cm. Hip roll stays 0. A roll sinks the lower sole.
            VerbExitSample s = P(18f, 8f, head, 64f, 64f, -82f, -82f, aPL, aPR, aYL, aYR, eL, eR);
            s.Drop = 0.20f;
            s.ThighYawL = -26f;
            s.ThighYawR = 26f;
            return s;
        }

        static void Spread(float head, float hipYaw, float spineYaw,
            float aYL, float aYR, float eL, float eR,
            out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // Arm pitch stays in the band that clears the chest and the thigh.
            b = Sit(head, -40f, -40f, aYL, aYR, eL, eR);
            b.HipYaw = hipYaw;
            b.SpineYaw = spineYaw;
            a = b;
            a.Head = head - 8f;
            a.ArmPitchL = -46f;
            a.ArmPitchR = -46f;
            c = b;
            c.Head = head + 6f;
            c.ArmPitchL = -32f;
            c.ArmPitchR = -32f;
        }

        /// <summary>The foot is down. The hips sit. The outside arm is what reads as the wall.</summary>
        static void WallRun(bool stepDown, out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            if (stepDown)
                Spread(58f, 2f, 0f, -30f, 30f, -50f, -50f, out a, out b, out c);
            else
                Spread(-18f, 12f, 0f, -20f, 20f, -36f, -36f, out a, out b, out c);
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

        /// <summary>Both feet on the lid. The hips bone is down and the shins point forward.</summary>
        static void ClimbTop(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // Thigh 64, knee 70, yaw 26. The knee sits ahead of the pelvis and
            // ahead of the ankle. Drop is the hips bone, 17.8 cm, which puts
            // the sole back on the lid. It is not a visual-root offset.
            b = P(18f, 8f, 26f, 64f, 64f, -70f, -70f, -40f, -40f, -30f, 30f, -50f, -50f);
            b.HipYaw = 8f;
            b.ThighYawL = -26f;
            b.ThighYawR = 26f;
            b.Drop = 0.178f;
            a = b;
            a.Head = 18f;
            a.ArmPitchL = -46f;
            a.ArmPitchR = -46f;
            c = b;
            c.Head = 32f;
            c.ArmPitchL = -32f;
            c.ArmPitchR = -32f;
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

        /// <summary>Recovery after the vault. Both feet down, hips behind them.</summary>
        static void Vault(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            // The played vault is MantlePose.Cleared. This beat is the recovery.
            Spread(0f, 10f, 0f, -10f, 10f, -20f, -20f, out a, out b, out c);
        }

        /// <summary>Both feet on the lid. Not the climb's arm line.</summary>
        static void Mantle(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            Spread(28f, 0f, 0f, -10f, 10f, -20f, -20f, out a, out b, out c);
        }

        /// <summary>The slide recovery sits. Slide boost is not here.</summary>
        static void Slide(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            Spread(12f, 0f, 0f, -20f, 20f, -36f, -36f, out a, out b, out c);
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
            a.FootR = 8f;
            b = P(-12f, -4f, 2f, 8f, -16f, -14f, -10f, -16f, -34f, -12f, 10f, -20f, -70f);
            b.HipYaw = -14f;
            b.SpineYaw = -8f;
            b.ArmRollR = -16f;
            b.FootR = 8f;
            c = P(2f, 0f, 0f, 10f, 4f, -10f, -8f, -14f, -18f, -14f, 12f, -12f, -36f);
            c.FootR = 8f;
        }

        /// <summary>The reach collapses, weight back, then the stride.</summary>
        static void Lunge(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(12f, 16f, -6f, 36f, -8f, -20f, -10f, -40f, -32f, -14f, 12f, -16f, -12f);
            a.ThighYawL = -36f;
            a.ThighYawR = 36f;
            a.FootR = 10f;
            b = P(-8f, -4f, 2f, 28f, 8f, -36f, -16f, -12f, -18f, -12f, 14f, -22f, -36f);
            b.HipYaw = -8f;
            b.ThighYawL = -36f;
            b.ThighYawR = 36f;
            b.FootR = 10f;
            c = P(4f, 2f, 0f, 18f, 8f, -16f, -10f, -16f, -12f, -16f, 14f, -10f, -12f);
            c.ThighYawL = -36f;
            c.ThighYawR = 36f;
            c.FootR = 10f;
        }

        /// <summary>Hands leave the cable and the body drops into the fall.</summary>
        static void Zip(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(-8f, -6f, -4f, 34f, 30f, -18f, -14f, -100f, -100f, -42f, 42f, -8f, -8f);
            b = P(-8f, -6f, -4f, 34f, 30f, -18f, -14f, -100f, -100f, -42f, 42f, -8f, -8f);
            c = P(-8f, -6f, -4f, 34f, 30f, -18f, -14f, -100f, -100f, -42f, 42f, -8f, -8f);
        }

        /// <summary>The pad landing sits. Not a roll.</summary>
        static void Launch(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            Spread(-30f, 0f, 0f, -10f, 10f, -20f, -20f, out a, out b, out c);
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

        /// <summary>The stumble catches with both feet and the hips sit.</summary>
        static void Stagger(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            Spread(46f, 6f, 0f, -20f, 20f, -36f, -36f, out a, out b, out c);
        }

        /// <summary>The immunity flinch shakes off. The glow is not this pose.</summary>
        static void TagBack(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 4f, -4f, 8f, -6f, -10f, -8f, -14f, -20f, -16f, 18f, -16f, -24f);
            a.SpineYaw = -12f;
            a.HipYaw = -8f;
            b = P(2f, 2f, 2f, 6f, 4f, -8f, -6f, 6f, -10f, -16f, 14f, -12f, -16f);
            b.SpineRoll = 16f;
            // A roll of -6 sinks the left sole 0.86 cm. -2 stays inside 0.5 cm.
            b.HipRoll = -2f;
            c = P(0f, 0f, 0f, 4f, 4f, -4f, -4f, -10f, -10f, -12f, 12f, -8f, -8f);
        }

        /// <summary>A landing under the roll gate. Depth scales with fall speed.</summary>
        static void Soft(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            Spread(-8f, 4f, 0f, -30f, 30f, -50f, -50f, out a, out b, out c);
        }
    }
}
