using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Matches an exit's first frames to the live verb, then lets the authored
    /// recovery play. Hands stay on the real lip. A planted foot does not skate.
    /// A backward landing eases into the reversed stride. Visual only.
    /// </summary>
    public static class VerbExitFit
    {
        public const bool RootMotion = false;
        public const float Join = 0.22f;
        /// <summary>Backward speed, m/s on the facing axis, that starts the reversed stride.</summary>
        public const float BackEnter = 0.35f;

        public static VerbExitSample Apply(
            VerbExitSample authored,
            VerbExitId id,
            float u,
            bool shoulderLeft,
            bool fromMantle,
            float wallPhase,
            float fwd,
            float side,
            float vertical = 0f,
            float gait = 0f,
            float ropeElev = 0f,
            bool rope = false)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            VerbExitSample s = authored;
            if (Joins(id))
            {
                float t = Join > 0.0001f ? u / Join : 1f;
                if (t < 1f)
                {
                    VerbExitSample src = Source(id, shoulderLeft, fromMantle, wallPhase, fwd, side, vertical, gait, ropeElev, rope);
                    float spin = authored.RootSpin;
                    float pitch = authored.RootPitch;
                    float roll = authored.RootRoll;
                    float drop = authored.Drop;
                    s = VerbExitSample.Lerp(src, authored, PoseHandoff.Ease(t));
                    s.RootSpin = spin;
                    s.RootPitch = pitch;
                    s.RootRoll = roll;
                    s.Drop = drop;
                }
            }
            float rev = ReverseWeight(id, u, fwd);
            if (rev > 0.001f)
            {
                LocomotionPolish.Legs legs = LocomotionPolish.FacingStride(
                    s.ThighL, s.ThighR, s.KneeL, s.KneeR, fwd, side);
                s.ThighL = Mathf.Lerp(s.ThighL, legs.ThighL, rev);
                s.ThighR = Mathf.Lerp(s.ThighR, legs.ThighR, rev);
                s.KneeL = Mathf.Lerp(s.KneeL, legs.KneeL, rev);
                s.KneeR = Mathf.Lerp(s.KneeR, legs.KneeR, rev);
                s.HipYaw = Mathf.Lerp(s.HipYaw, legs.HipYaw, rev);
            }
            KeepRun(ref s, id, fwd, side, gait);
            if (FootWeight(id, u) > 0.35f)
            {
                s.FootL = GaitBlend.SoleLevelDeg(s.ThighL, s.KneeL);
                s.FootR = GaitBlend.SoleLevelDeg(s.ThighR, s.KneeR);
            }
            return s;
        }

        /// <summary>A sprint landing keeps the stride. The give stays in the knees.</summary>
        static void KeepRun(ref VerbExitSample s, VerbExitId id, float fwd, float side, float gait)
        {
            if (id != VerbExitId.SoftLand && id != VerbExitId.LaunchLand
                && id != VerbExitId.Roll && id != VerbExitId.RollAbsorb)
                return;
            float speed = fwd * fwd + side * side;
            if (speed > 0f) speed = Mathf.Sqrt(speed);
            if (speed < 13.8f * 0.85f) return;
            float keep = BodyLine.KeepStride(speed);
            if (keep < 0.02f) return;
            GaitBlend.Legs step = GaitBlend.At(gait, speed);
            LocomotionPolish.Legs legs = LocomotionPolish.FacingStride(
                step.ThighL, step.ThighR, step.KneeL, step.KneeR, fwd, side);
            s.ThighL = Mathf.Lerp(s.ThighL, legs.ThighL, keep);
            s.ThighR = Mathf.Lerp(s.ThighR, legs.ThighR, keep);
        }

        public static bool Joins(VerbExitId id)
        {
            return id != VerbExitId.Stagger && id != VerbExitId.TagBackEnd;
        }

        /// <summary>1 while the hands should meet the lip. 0 once the chest is up.</summary>
        public static float LipWeight(VerbExitId id, float u)
        {
            if (id != VerbExitId.ClimbTopOut && id != VerbExitId.Vault && id != VerbExitId.Mantle)
                return 0f;
            if (u <= 0.28f) return 1f;
            if (u >= 0.55f) return 0f;
            return 1f - PoseHandoff.Ease((u - 0.28f) / 0.27f);
        }

        /// <summary>1 while a palm should sit on the ground. The lip wins when both could apply.</summary>
        public static float HandGroundWeight(VerbExitId id, float u, float fallScale)
        {
            if (u < 0f) u = 0f;
            if (id == VerbExitId.RollAbsorb)
            {
                if (u <= 0.62f) return 1f;
                if (u >= 0.88f) return 0f;
                return 1f - PoseHandoff.Ease((u - 0.62f) / 0.26f);
            }
            if (id == VerbExitId.Roll)
            {
                if (u < 0.30f || u > 0.66f) return 0f;
                if (u < 0.40f) return PoseHandoff.Ease((u - 0.30f) / 0.10f);
                if (u > 0.56f) return 1f - PoseHandoff.Ease((u - 0.56f) / 0.10f);
                return 1f;
            }
            if (id == VerbExitId.SoftLand && fallScale >= 0.9f)
            {
                if (u < 0.22f || u > 0.72f) return 0f;
                if (u < 0.34f) return PoseHandoff.Ease((u - 0.22f) / 0.12f);
                if (u > 0.60f) return 1f - PoseHandoff.Ease((u - 0.60f) / 0.12f);
                return 1f;
            }
            return 0f;
        }

        /// <summary>1 while the sole is pinned, so the capsule can move and the foot does not.</summary>
        public static float FootWeight(VerbExitId id, float u)
        {
            if (u < 0f) u = 0f;
            if (id == VerbExitId.SoftLand || id == VerbExitId.LaunchLand || id == VerbExitId.RollAbsorb)
            {
                if (u <= 0.82f) return 1f;
                if (u >= 1f) return 0f;
                return 1f - PoseHandoff.Ease((u - 0.82f) / 0.18f);
            }
            if (id == VerbExitId.Roll)
            {
                if (u < 0.72f || u > 0.96f) return 0f;
                if (u < 0.80f) return PoseHandoff.Ease((u - 0.72f) / 0.08f);
                if (u > 0.90f) return 1f - PoseHandoff.Ease((u - 0.90f) / 0.06f);
                return 1f;
            }
            if (id == VerbExitId.Slide || id == VerbExitId.ClimbTopOut
                || id == VerbExitId.Vault || id == VerbExitId.Mantle || id == VerbExitId.WallRun)
            {
                if (u < 0.48f) return 0f;
                if (u < 0.62f) return PoseHandoff.Ease((u - 0.48f) / 0.14f);
                if (u > 0.88f) return 1f - PoseHandoff.Ease((u - 0.88f) / 0.12f);
                return 1f;
            }
            return 0f;
        }

        /// <summary>Centimeters the sole skates in one step. A full pin is zero.</summary>
        public static float SkateCm(float stepMeters, float pin)
        {
            float w = pin < 0f ? 0f : (pin > 1f ? 1f : pin);
            float miss = stepMeters * (1f - w);
            if (miss < 0f) miss = 0f;
            return miss * 100f;
        }

        public static float ReverseWeight(VerbExitId id, float u, float fwd)
        {
            if (!Reverses(id)) return 0f;
            if (fwd > -BackEnter) return 0f;
            if (u <= 0.62f) return 0f;
            return PoseHandoff.Ease((u - 0.62f) / 0.38f);
        }

        public static bool Reverses(VerbExitId id)
        {
            switch (id)
            {
                case VerbExitId.WallJump:
                case VerbExitId.AirDash:
                case VerbExitId.ZipDrop:
                case VerbExitId.ClingDrop:
                case VerbExitId.GrappleArrive:
                case VerbExitId.GrappleRelease:
                    return false;
                default:
                    return id != VerbExitId.None;
            }
        }

        public static float FitStep(VerbExitId id)
        {
            float dur = VerbExitClock.Duration(id);
            if (dur < 0.05f) return 0f;
            int frames = (int)(dur / VerbExitChain.Frame);
            if (frames < 2) frames = 2;
            if (frames > 48) frames = 48;
            bool mantle = id != VerbExitId.ClimbTopOut;
            VerbExitSample prev = Frame(id, 0f, 1f, 0f, mantle);
            float max = 0f;
            for (int i = 1; i <= frames; i++)
            {
                float u = i / (float)frames;
                VerbExitSample next = Frame(id, u, 1f, 0f, mantle);
                float step = VerbExitSample.MaxStep(prev, next);
                if (step > max) max = step;
                prev = next;
            }
            if (id == VerbExitId.ClimbTopOut)
            {
                VerbExitSample a = Frame(id, 0f, 1f, 0f, true);
                for (int i = 1; i <= frames; i++)
                {
                    float u = i / (float)frames;
                    VerbExitSample b = Frame(id, u, 1f, 0f, true);
                    float step = VerbExitSample.MaxStep(a, b);
                    if (step > max) max = step;
                    a = b;
                }
            }
            VerbExitSample back = Frame(id, 0f, -6f, 0f, mantle);
            for (int i = 1; i <= frames; i++)
            {
                float u = i / (float)frames;
                VerbExitSample back2 = Frame(id, u, -6f, 0f, mantle);
                float rev = VerbExitSample.MaxStep(back, back2);
                if (rev > max) max = rev;
                back = back2;
            }
            return max;
        }

        static VerbExitSample Frame(VerbExitId id, float u, float fwd, float side)
        {
            bool mantle = id == VerbExitId.Vault || id == VerbExitId.Mantle;
            return Frame(id, u, fwd, side, mantle);
        }

        static VerbExitSample Frame(VerbExitId id, float u, float fwd, float side, bool fromMantle)
        {
            VerbExitSample authored = VerbExitClips.At(id, u, 1f, false, false);
            float vy = 0f;
            if (id == VerbExitId.WallJump) vy = RiseVy(WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds);
            else if (id == VerbExitId.LaunchLand) vy = LaunchPose.OpenVy;
            return Apply(authored, id, u, false, fromMantle, 0.4f, fwd, side, vy, 1.2f, 0f, false);
        }

        /// <summary>Rise left after the locked jump speed has been in the air for the wall-jump arc.</summary>
        public static float RiseVy(float age)
        {
            float vy = 24.7f - 22f * age;
            if (vy < -56.16f) vy = -56.16f;
            return vy;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Join < 0.12f || Join > 0.30f) return false;
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f) return false;
            if (Mathf.Abs(ClimbContact.ClimbSpeed - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(WallJumpPose.BeatSeconds - 0.15f) > 0.001f) return false;
            if (Mathf.Abs(ZipPose.RideSpeed - 14f) > 0.001f) return false;

            VerbExitSample jump = Frame(VerbExitId.WallJump, 0f, 1f, 0f);
            float arcEnd = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            WallJumpPose.Sample risen = WallJumpPose.At(arcEnd, RiseVy(arcEnd), false, 1f);
            if (Mathf.Abs(jump.ArmPitchL - risen.ArmPitchL) > 8f) return false;
            VerbExitSample zip = Frame(VerbExitId.ZipDrop, 0f, 1f, 0f);
            if (Mathf.Abs(zip.ArmPitchL - BodyLine.CablePitch) > 8f) return false;
            VerbExitSample release = Frame(VerbExitId.GrappleRelease, 0f, 1f, 0f);
            GrapplePose.Sample pull = GrapplePose.ForBody(GrapplePose.Pull(0f, 0f, 0f));
            if (Mathf.Abs(release.ArmPitchL - pull.ArmPitchL) > 8f) return false;
            VerbExitSample arrive = Frame(VerbExitId.GrappleArrive, 0f, 1f, 0f);
            if (Mathf.Abs(arrive.ArmPitchL - pull.ArmPitchL) > 8f) return false;
            LaunchPose.Sample open = LaunchPose.At(-16f);
            VerbExitSample pad = Frame(VerbExitId.LaunchLand, 0f, 1f, 0f);
            if (Mathf.Abs(pad.ArmYawL - open.ArmYawL) > 8f) return false;
            if (Mathf.Abs(pad.ArmPitchL - open.ArmPitchL) > 8f) return false;

            VerbExitSample climb = Frame(VerbExitId.ClimbTopOut, 0f, 1f, 0f);
            WallPose.Sample liveClimb = WallPose.Climb(1f, ClimbContact.ClimbSpeed);
            float climbArm = liveClimb.ArmPitchL < liveClimb.ArmPitchR ? liveClimb.ArmPitchL : liveClimb.ArmPitchR;
            float fitArm = climb.ArmPitchL < climb.ArmPitchR ? climb.ArmPitchL : climb.ArmPitchR;
            if (Mathf.Abs(fitArm - climbArm) > 8f) return false;

            VerbExitSample vault = Apply(
                VerbExitClips.At(VerbExitId.Vault, 0f, 1f, false, false),
                VerbExitId.Vault, 0f, false, true, 0f, 1f, 0f);
            MantlePose.Sample land = MantlePose.At(1f, true);
            if (Mathf.Abs(vault.ArmPitchL - land.ArmPitchL) > 8f) return false;
            if (ClimbContact.LipMiss(1.40f, true) > 0.05f) return false;
            if (LipWeight(VerbExitId.Vault, 0.1f) < 0.99f) return false;
            if (LipWeight(VerbExitId.Vault, 0.9f) > 0.001f) return false;
            if (LipWeight(VerbExitId.ZipDrop, 0.1f) > 0.001f) return false;

            VerbExitSample roll0 = Frame(VerbExitId.Roll, 0f, 4f, 0f);
            if (roll0.ArmYawL < 40f) return false;
            if (roll0.RootSpin > 1f) return false;
            VerbExitSample rollMid = Frame(VerbExitId.Roll, 0.52f, 4f, 0f);
            if (rollMid.RootSpin < 170f) return false;
            if (HandGroundWeight(VerbExitId.Roll, 0.48f, 1f) < 0.99f) return false;
            if (HandGroundWeight(VerbExitId.RollAbsorb, 0.2f, 1f) < 0.99f) return false;
            if (HandGroundWeight(VerbExitId.SoftLand, 0.5f, 0.25f) > 0.001f) return false;
            if (HandGroundWeight(VerbExitId.SoftLand, 0.5f, 1f) < 0.99f) return false;
            if (FootWeight(VerbExitId.Roll, 0.84f) < 0.99f) return false;
            if (FootWeight(VerbExitId.Roll, 0.2f) > 0.001f) return false;

            if (SkateCm(0.12f, 0f) < 8f) return false;
            if (SkateCm(0.12f, 1f) > 0.05f) return false;

            VerbExitSample fore = Frame(VerbExitId.SoftLand, 1f, 6f, 0f);
            VerbExitSample back = Frame(VerbExitId.SoftLand, 1f, -6f, 0f);
            if (fore.ThighL <= 0f) return false;
            if (back.ThighL >= 0f) return false;
            if (ReverseWeight(VerbExitId.WallJump, 1f, -6f) > 0.001f) return false;
            if (ReverseWeight(VerbExitId.SoftLand, 0.5f, -6f) > 0.001f) return false;

            VerbExitSample flat = Frame(VerbExitId.SoftLand, 0.5f, 2f, 0f);
            if (Mathf.Abs(GaitBlend.StanceWorldPitch(flat.ThighL, flat.KneeL, flat.FootL)) > 0.05f) return false;

            for (int i = 0; i < VerbExitClock.Catalog.Length; i++)
            {
                if (FitStep(VerbExitClock.Catalog[i]) > VerbExitChain.StepBudget) return false;
            }
            return true;
        }

        public static string ProofLine()
        {
            VerbExitSample jump = Frame(VerbExitId.WallJump, 0f, 1f, 0f);
            VerbExitSample zip = Frame(VerbExitId.ZipDrop, 0f, 1f, 0f);
            VerbExitSample release = Frame(VerbExitId.GrappleRelease, 0f, 1f, 0f);
            float worst = 0f;
            for (int i = 0; i < VerbExitClock.Catalog.Length; i++)
            {
                float step = FitStep(VerbExitClock.Catalog[i]);
                if (step > worst) worst = step;
            }
            return "exit-fit"
                + " join=" + Join.ToString("0.00")
                + " wallJump=" + jump.ArmPitchL.ToString("0")
                + " zip=" + zip.ArmPitchL.ToString("0")
                + " grapple=" + release.ArmPitchL.ToString("0")
                + " lip=" + LipWeight(VerbExitId.Vault, 0.1f).ToString("0")
                + ">" + LipWeight(VerbExitId.Vault, 0.9f).ToString("0")
                + " skate=" + SkateCm(0.12f, 0f).ToString("0")
                + ">" + SkateCm(0.12f, 1f).ToString("0")
                + " reverse=1"
                + " rollOpen=" + Frame(VerbExitId.Roll, 0f, 4f, 0f).ArmYawL.ToString("0")
                + " step=" + worst.ToString("0.0")
                + " budget=" + VerbExitChain.StepBudget.ToString("0");
        }

        static VerbExitSample Source(
            VerbExitId id, bool shoulderLeft, bool fromMantle, float wallPhase,
            float fwd, float side, float vertical, float gait, float ropeElev, bool rope)
        {
            switch (id)
            {
                case VerbExitId.WallRun:
                    return FromWall(WallPose.RunCycle(wallPhase, shoulderLeft));
                case VerbExitId.WallJump:
                    return WallRise(shoulderLeft, vertical, fwd, side);
                case VerbExitId.ClimbTopOut:
                    if (fromMantle) return FromMantle(MantlePose.At(1f, shoulderLeft));
                    return FromWall(WallPose.Climb(shoulderLeft ? -1f : 1f, ClimbContact.ClimbSpeed));
                case VerbExitId.ClingDrop:
                    return FromWall(WallPose.Hold());
                case VerbExitId.Vault:
                case VerbExitId.Mantle:
                    return FromMantle(MantlePose.At(1f, shoulderLeft));
                case VerbExitId.Slide:
                    return FromSlide(shoulderLeft);
                case VerbExitId.AirDash:
                    return FromDash(AirDashPose.At(0f, 1f));
                case VerbExitId.Punch:
                    return FromPunch(fwd, side);
                case VerbExitId.Lunge:
                    return FromLunge(LungePose.Burst());
                case VerbExitId.ZipDrop:
                    return ZipHang();
                case VerbExitId.LaunchLand:
                    return PadLand(vertical);
                case VerbExitId.GrappleArrive:
                case VerbExitId.GrappleRelease:
                    return GrappleLine(ropeElev, rope);
                case VerbExitId.Roll:
                case VerbExitId.RollAbsorb:
                case VerbExitId.SoftLand:
                    return FallOpen();
                default:
                    return default;
            }
        }

        static VerbExitSample FallOpen()
        {
            VerbExitSample s = default;
            s.ThighL = JumpPose.FallThigh;
            s.ThighR = JumpPose.FallThigh;
            s.KneeL = JumpPose.FallKnee;
            s.KneeR = JumpPose.FallKnee;
            s.ArmPitchL = JumpPose.FallArmPitch;
            s.ArmPitchR = JumpPose.FallArmPitch;
            s.ArmYawL = JumpPose.FallArmYaw;
            s.ArmYawR = -JumpPose.FallArmYaw;
            s.ElbowL = JumpPose.FallElbow;
            s.ElbowR = JumpPose.FallElbow;
            s.Hip = JumpPose.FallHip;
            s.Spine = JumpPose.FallSpine;
            s.Head = 4f;
            return s;
        }

        static VerbExitSample FromWall(WallPose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            o.HipRoll = s.LeanZ;
            o.FootL = s.FootL;
            o.FootR = s.FootR;
            return o;
        }

        static VerbExitSample FromPush(WallJumpPose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            o.HipRoll = s.LeanZ;
            return o;
        }

        static VerbExitSample FromMantle(MantlePose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            return o;
        }

        static VerbExitSample FromZip(ZipPose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            o.HipRoll = s.LeanZ;
            return o;
        }

        static VerbExitSample FromLaunch(LaunchPose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            return o;
        }

        static VerbExitSample FromGrapple(GrapplePose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            o.HipYaw = s.HipYaw;
            o.SpineYaw = s.SpineYaw;
            o.HeadYaw = s.HeadYaw;
            return o;
        }

        static VerbExitSample FromDash(AirDashPose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.Spine = s.Spine;
            o.Head = s.Head;
            o.HipRoll = s.LeanZ;
            return o;
        }

        static VerbExitSample FromLunge(LungePose.Sample s)
        {
            VerbExitSample o = default;
            o.ThighL = s.ThighL;
            o.ThighR = s.ThighR;
            o.KneeL = s.KneeL;
            o.KneeR = s.KneeR;
            o.ArmPitchL = s.ArmPitchL;
            o.ArmPitchR = s.ArmPitchR;
            o.ArmYawL = s.ArmYawL;
            o.ArmYawR = s.ArmYawR;
            o.ArmRollL = s.ArmRollL;
            o.ArmRollR = s.ArmRollR;
            o.ElbowL = s.ElbowL;
            o.ElbowR = s.ElbowR;
            o.Hip = s.Hip;
            o.HipYaw = s.HipYaw;
            o.Spine = s.Spine;
            o.SpineYaw = s.SpineYaw;
            o.Head = s.Head;
            o.HeadYaw = s.HeadYaw;
            return o;
        }

        static VerbExitSample WallRise(bool shoulderLeft, float vertical, float fwd, float side)
        {
            float end = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            float speed = fwd * fwd + side * side;
            if (speed > 0f) speed = Mathf.Sqrt(speed);
            return FromPush(WallJumpPose.At(end, vertical, shoulderLeft, speed));
        }

        static VerbExitSample ZipHang()
        {
            ZipPose.Sample hang = ZipPose.Hang();
            hang.ArmPitchL = BodyLine.CablePitch;
            hang.ArmPitchR = BodyLine.CablePitch;
            return FromZip(hang);
        }

        static VerbExitSample PadLand(float vertical)
        {
            LaunchPose.Sample pose = LaunchPose.At(vertical);
            float fall = LaunchPose.OpenAmount(vertical);
            pose.KneeL = Mathf.Lerp(pose.KneeL, LandPose.SoftKnee, fall);
            pose.KneeR = Mathf.Lerp(pose.KneeR, LandPose.SoftKnee, fall);
            return FromLaunch(pose);
        }

        static VerbExitSample GrappleLine(float elev, bool rope)
        {
            VerbExitSample o = FromGrapple(GrapplePose.ForBody(GrapplePose.Pull(0f, 0f, 0f)));
            if (!rope) return o;
            float shared = HangMotion.RopeSpine(elev);
            float body = GrapplePose.PullHip + GrapplePose.PullSpine + shared;
            float fix = BodyLine.LineFix(body, elev);
            o.Spine += shared + fix * 0.55f;
            o.Hip += fix * 0.45f;
            return o;
        }

        static VerbExitSample FromPunch(float fwd, float side)
        {
            VerbExitSample o = default;
            o.ArmPitchR = VerbPoseClips.PunchStrikePitch;
            o.ArmYawR = VerbPoseClips.PunchStrikeYaw;
            o.ArmRollR = VerbPoseClips.PunchStrikeRoll;
            o.ElbowR = VerbPoseClips.PunchStrikeElbow;
            o.ArmPitchL = VerbPoseClips.PunchGuardPitchStrike;
            o.ArmYawL = VerbPoseClips.PunchGuardYawStrike;
            o.ElbowL = VerbPoseClips.PunchGuardElbowStrike;
            o.ArmRollL = VerbPoseClips.PunchGuardRoll;
            o.HipYaw = VerbPoseClips.PunchStrikeHipYaw;
            o.SpineYaw = VerbPoseClips.PunchStrikeSpineYaw;
            o.HeadYaw = VerbPoseClips.PunchStrikeHeadYaw;
            o.ThighL = VerbPoseClips.PunchStrikeLeadThigh;
            o.ThighR = VerbPoseClips.PunchStrikeTrailThigh;
            o.KneeL = VerbPoseClips.PunchStrikeLeadKnee;
            o.KneeR = VerbPoseClips.PunchStrikeTrailKnee;
            o.Hip = VerbPoseClips.PunchHipPitch;
            o.Spine = VerbPoseClips.PunchSpinePitch;
            float speed = fwd * fwd + side * side;
            if (speed > 0f) speed = Mathf.Sqrt(speed);
            float lead = BodyLine.ReachLead(speed);
            o.Hip += lead * 0.35f;
            o.Spine += lead * 0.65f;
            return o;
        }

        static VerbExitSample FromSlide(bool leadLeft)
        {
            VerbExitSample o = default;
            float pitchL, yawL, elbowL, pitchR, yawR, elbowR;
            VerbPoseClips.SlideArmOffsets(leadLeft, out pitchL, out yawL, out elbowL, out pitchR, out yawR, out elbowR);
            o.ArmPitchL = pitchL;
            o.ArmYawL = yawL;
            o.ElbowL = elbowL;
            o.ArmPitchR = pitchR;
            o.ArmYawR = yawR;
            o.ElbowR = elbowR;
            o.Hip = VerbPoseClips.SlideHip;
            o.Spine = VerbPoseClips.SlideSpine;
            o.Head = VerbPoseClips.SlideHead;
            o.ThighL = leadLeft ? VerbPoseClips.SlideLeadThigh : VerbPoseClips.SlideTrailThigh;
            o.ThighR = leadLeft ? VerbPoseClips.SlideTrailThigh : VerbPoseClips.SlideLeadThigh;
            o.KneeL = leadLeft ? VerbPoseClips.SlideLeadKnee : VerbPoseClips.SlideTrailKnee;
            o.KneeR = leadLeft ? VerbPoseClips.SlideTrailKnee : VerbPoseClips.SlideLeadKnee;
            o.ThighYawL = leadLeft ? VerbPoseClips.SlideLeadYaw : VerbPoseClips.SlideTrailYaw;
            o.ThighYawR = leadLeft ? VerbPoseClips.SlideTrailYaw : -VerbPoseClips.SlideLeadYaw;
            o.FootL = leadLeft ? VerbPoseClips.SlideLeadFoot : VerbPoseClips.SlideTrailFoot;
            o.FootR = leadLeft ? VerbPoseClips.SlideTrailFoot : VerbPoseClips.SlideLeadFoot;
            return o;
        }
    }
}
