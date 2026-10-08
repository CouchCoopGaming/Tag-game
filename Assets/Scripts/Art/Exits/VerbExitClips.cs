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
            a = P(6f, 8f, -8f, 48f, 18f, -36f, -18f, -70f, 16f, 22f, -28f, -20f, -16f);
            a.HipRoll = -16f;
            a.SpineRoll = -12f;
            if (stepDown)
            {
                b = P(22f, 16f, 6f, 86f, 10f, -28f, -42f, -52f, -18f, 14f, 20f, -22f, -30f);
                b.Drop = 0.03f;
                b.RootPitch = 8f;
            }
            else
            {
                b = P(-16f, -6f, -4f, 70f, -8f, -18f, -12f, 34f, 22f, -16f, 18f, -12f, -20f);
                b.HipRoll = 10f;
                b.SpineRoll = 8f;
            }
            c = P(4f, 2f, 0f, 22f, 8f, -16f, -12f, -18f, -14f, 16f, -14f, -12f, -10f);
        }

        /// <summary>Tuck, then the body opens into the air stride.</summary>
        static void WallJump(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(14f, 10f, -6f, 88f, 80f, -110f, -104f, -36f, -28f, 8f, -10f, -96f, -88f);
            a.SpineRoll = 6f;
            b = P(20f, 24f, -12f, 104f, 98f, -124f, -116f, -42f, -34f, 6f, -8f, -108f, -100f);
            b.Head = -16f;
            c = P(-4f, -8f, 4f, 12f, 8f, -10f, -8f, -8f, -6f, 62f, -58f, -8f, -6f);
            c.ArmRollL = -12f;
            c.ArmRollR = 12f;
        }

        /// <summary>Hand plant, lead knee up, then stand.</summary>
        static void ClimbTop(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(26f, 38f, -14f, 34f, 16f, -48f, -22f, -112f, -104f, 16f, -14f, -92f, -86f);
            b = P(18f, 28f, -6f, 112f, 14f, -126f, -20f, -96f, -88f, 12f, -10f, -78f, -70f);
            b.Drop = 0.04f;
            c = P(6f, 4f, 0f, 18f, 14f, -22f, -16f, -16f, -12f, 14f, -12f, -14f, -12f);
        }

        /// <summary>Hands open off the wall and the body drops into the fall.</summary>
        static void Cling(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(8f, 6f, -10f, 36f, 28f, -40f, -24f, -102f, -96f, 10f, -12f, -18f, -16f);
            b = P(-14f, -6f, 8f, 16f, 12f, -34f, -28f, -22f, -18f, 46f, -42f, -36f, -30f);
            b.SpineYaw = -8f;
            c = P(4f, 2f, -4f, 8f, 6f, -14f, -12f, 18f, 14f, 28f, -26f, -16f, -14f);
        }

        /// <summary>Trail leg sweeps through, then the lead foot finds the stride.</summary>
        static void Vault(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(20f, 32f, -8f, 28f, 46f, -36f, -22f, -48f, -40f, 10f, -8f, -24f, -20f);
            b = P(8f, 18f, 2f, 14f, 94f, -16f, -28f, -16f, 24f, 18f, -30f, -12f, -18f);
            b.ThighYawR = 38f;
            b.HipYaw = -12f;
            b.SpineYaw = 10f;
            c = P(4f, 2f, 0f, 34f, -6f, -18f, -10f, -22f, 8f, 12f, -16f, -10f, -8f);
        }

        /// <summary>Both hands press the lip, then the chest comes up. Not the trail-leg sweep.</summary>
        static void Mantle(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(22f, 30f, -12f, 24f, 20f, -30f, -26f, -108f, -104f, 8f, -8f, -84f, -80f);
            b = P(10f, 16f, -4f, 58f, 52f, -48f, -44f, -90f, -86f, 6f, -6f, -64f, -60f);
            b.Drop = 0.03f;
            c = P(2f, 6f, 0f, 16f, 14f, -14f, -12f, -10f, -8f, 18f, -16f, -8f, -8f);
        }

        /// <summary>Pop the chest up out of the baseball slide and into the run. Slide boost is not here.</summary>
        static void Slide(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(-22f, -12f, 18f, 64f, 36f, -12f, -124f, -30f, 40f, 18f, -16f, -24f, -32f);
            b = P(16f, 6f, -4f, 20f, 52f, -18f, -72f, -28f, -36f, 22f, -8f, -16f, -40f);
            b.HipYaw = 8f;
            c = P(4f, 2f, 0f, 28f, 6f, -16f, -12f, -20f, -12f, 14f, -16f, -10f, -8f);
        }

        /// <summary>The stretched dash settles back onto the air line.</summary>
        static void Dash(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(12f, 14f, -6f, 22f, -8f, -16f, -8f, 28f, 22f, -8f, 6f, -10f, -8f);
            a.SpineYaw = -6f;
            b = P(2f, 2f, 0f, 8f, 6f, -8f, -6f, -6f, -4f, 34f, -32f, -8f, -6f);
            c = P(0f, 0f, 0f, 6f, 4f, -6f, -4f, -12f, -10f, 14f, -12f, -8f, -8f);
        }

        /// <summary>Right arm folds back to the ribs and the weight sits back.</summary>
        static void Punch(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(8f, 6f, -4f, 16f, -10f, -8f, -6f, -20f, -72f, 8f, 4f, -18f, -6f);
            a.HipYaw = 18f;
            a.SpineYaw = 16f;
            b = P(-12f, -4f, 2f, 8f, -16f, -14f, -10f, -16f, -34f, 6f, -8f, -20f, -104f);
            b.HipYaw = -14f;
            b.SpineYaw = -8f;
            b.ArmRollR = -16f;
            c = P(2f, 0f, 0f, 10f, 4f, -10f, -8f, -14f, -18f, 12f, -8f, -12f, -36f);
        }

        /// <summary>The reach collapses, weight back, then the stride.</summary>
        static void Lunge(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(16f, 22f, -8f, 62f, -12f, -20f, -8f, -48f, -40f, 8f, -6f, -16f, -12f);
            b = P(-16f, -6f, 4f, 40f, 8f, -58f, -16f, -12f, -18f, 10f, -14f, -22f, -48f);
            b.HipYaw = -8f;
            c = P(4f, 2f, 0f, 24f, 8f, -16f, -10f, -16f, -12f, 14f, -12f, -10f, -12f);
        }

        /// <summary>Hands leave the cable and the body drops into the fall.</summary>
        static void Zip(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(-8f, -6f, -8f, 30f, 26f, -16f, -12f, -148f, -142f, 8f, -8f, -8f, -8f);
            b = P(12f, 8f, -6f, -6f, -4f, -12f, -10f, -46f, -42f, 22f, -20f, -28f, -24f);
            c = P(2f, 0f, 2f, 8f, 6f, -8f, -6f, 12f, 10f, 36f, -34f, -12f, -10f);
        }

        /// <summary>Pad arc opens, knees take the landing, then the stride. Not a roll.</summary>
        static void Launch(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(2f, 2f, 4f, 6f, 4f, -8f, -6f, -6f, -4f, 70f, -66f, -8f, -6f);
            b = P(14f, 12f, -2f, 44f, 40f, -68f, -62f, -20f, -16f, 28f, -24f, -24f, -20f);
            b.Drop = 0.04f;
            c = P(4f, 2f, 0f, 20f, 10f, -16f, -12f, -14f, -10f, 16f, -14f, -10f, -8f);
        }

        /// <summary>Left hand still leads as the body arrives on the pull.</summary>
        static void Arrive(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 4f, -6f, 18f, 10f, -16f, -10f, -96f, -20f, 22f, 8f, -14f, -16f);
            b = P(10f, 14f, -4f, 28f, 16f, -36f, -18f, -74f, -16f, 16f, 6f, -76f, -18f);
            b.SpineYaw = 8f;
            c = P(4f, 4f, 0f, 14f, 10f, -14f, -10f, -36f, -12f, 12f, 8f, -28f, -12f);
        }

        /// <summary>Left hand opens and folds in. The pull is already over.</summary>
        static void Release(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(4f, 6f, -2f, 12f, 8f, -12f, -8f, -70f, -14f, 14f, 6f, -20f, -12f);
            b = P(2f, 2f, 2f, 8f, 6f, -10f, -8f, -26f, -10f, -6f, 18f, -90f, -14f);
            b.ArmRollL = 18f;
            c = P(0f, 0f, 0f, 6f, 4f, -6f, -4f, -12f, -10f, 16f, -14f, -16f, -10f);
        }

        /// <summary>The stumble catches a step and the chest comes back up.</summary>
        static void Stagger(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(22f, 18f, 6f, 18f, -14f, -28f, -12f, -10f, 16f, 36f, -28f, -20f, -16f);
            a.SpineYaw = 18f;
            b = P(12f, 8f, 2f, 38f, 6f, -50f, -16f, -8f, -6f, 42f, -18f, -22f, -14f);
            b.HipYaw = 10f;
            b.SpineYaw = 14f;
            c = P(2f, 2f, 0f, 16f, 8f, -12f, -8f, -12f, -10f, 14f, -12f, -10f, -8f);
        }

        /// <summary>The immunity flinch shakes off. The glow is not this pose.</summary>
        static void TagBack(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = P(6f, 4f, -4f, 8f, -6f, -10f, -8f, -14f, -24f, 8f, -20f, -16f, -28f);
            a.SpineYaw = -12f;
            a.HipYaw = -8f;
            b = P(2f, 2f, 2f, 6f, 4f, -8f, -6f, 6f, -12f, 20f, -8f, -12f, -18f);
            b.SpineRoll = 16f;
            b.HipRoll = -6f;
            c = P(0f, 0f, 0f, 4f, 4f, -4f, -4f, -10f, -10f, 12f, -12f, -8f, -8f);
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
