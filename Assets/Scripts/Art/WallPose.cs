using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual wall body only. A climb is hand-over-hand, a slip drags the hands,
    /// and a wall run strides with the outer arm. Nothing here writes velocity,
    /// cling, or the root.
    /// </summary>
    public static class WallPose
    {
        public const bool RootMotion = false;

        /// <summary>Air into a climb or a wall run.</summary>
        public const float AirBlendSeconds = 0.10f;
        /// <summary>Climb or wall run into the wall-jump push-off. The scuff tell stays.</summary>
        public const float PushBlendSeconds = 0.10f;
        /// <summary>Grace expiry, or a real leave, back to the fall beat.</summary>
        public const float ReleaseBlendSeconds = 0.10f;
        /// <summary>Locked cling grace. Read for the visual hold only. The motor timer is not written.</summary>
        public const float ClingGraceSeconds = 0.08f;
        /// <summary>Slew while a wall blend is in progress, so the curve is the blend.</summary>
        public const float BlendSlew = 170f;

        /// <summary>Locked climb up-speed. Read for cadence only. The motor is not written.</summary>
        public const float ClimbSpeedRef = 6.0f;
        /// <summary>Locked slip speed. Read for cadence only.</summary>
        public const float SlipSpeedRef = 3.7f;
        /// <summary>Locked wall-run speed. Read for cadence only.</summary>
        public const float WallRunSpeedRef = 9.5f;

        /// <summary>Radians per second at <see cref="ClimbSpeedRef"/>.</summary>
        public const float ClimbCadenceFull = 8.8f;
        /// <summary>Radians per second at <see cref="SlipSpeedRef"/> downward.</summary>
        public const float SlipCadenceFull = 3.2f;
        /// <summary>Radians per second at <see cref="WallRunSpeedRef"/>.</summary>
        public const float RunCadenceFull = 10.6f;

        public const float ReachPitch = -138f;
        public const float PullPitch = -46f;
        public const float ReachYaw = 26f;
        public const float PullYaw = 8f;
        public const float ReachElbow = -6f;
        public const float PullElbow = -48f;
        public const float DriveThigh = 84f;
        public const float PlantThigh = 12f;
        public const float DriveKnee = -102f;
        public const float PlantKnee = -16f;
        /// <summary>Chest into the wall.</summary>
        public const float ClimbSpine = 34f;
        public const float ClimbHip = 24f;
        /// <summary>Look up the wall. Negative is up, same sign as the air head.</summary>
        public const float ClimbHead = -28f;

        public const float DragPitch = -58f;
        public const float DragYaw = 14f;
        public const float DragElbow = -36f;
        public const float DragThigh = 30f;
        public const float DragKnee = -44f;
        /// <summary>Leftover sway while the hands drag. Not a reach.</summary>
        public const float DragAlt = 8f;
        public const float SlipSpine = 18f;
        public const float SlipHip = 12f;
        public const float SlipHead = -8f;

        /// <summary>Roll off the wall. Wall-on-the-left is negative, the bounce open.</summary>
        public const float RunTilt = 38f;
        public const float InnerPitch = -18f;
        public const float InnerSway = 4f;
        public const float InnerYaw = 18f;
        public const float InnerElbow = -44f;
        public const float OuterFwdPitch = -86f;
        public const float OuterBackPitch = 22f;
        public const float OuterYaw = 12f;
        public const float OuterFwdElbow = -12f;
        public const float OuterBackElbow = -38f;
        public const float OuterThighFwd = 56f;
        public const float OuterThighBack = -24f;
        public const float InnerThighFwd = 22f;
        public const float InnerThighBack = -8f;
        public const float RunKneeFwd = -84f;
        public const float RunKneeBack = -12f;
        public const float RunSpine = 14f;
        public const float RunHip = 8f;
        public const float RunHead = -4f;
        public const float HipRollShare = 0.45f;
        public const float HeadRollShare = 0.25f;

        /// <summary>Same push silhouette the wall jump already used. The tell is separate.</summary>
        public const float PushArmPitch = -36f;
        public const float PushArmYaw = 14f;
        public const float PushElbow = -14f;
        public const float PushSpine = -6f;
        public const float PushHip = 6f;
        public const float PushHead = 0f;
        public const float PushPlantThigh = -8f;
        public const float PushDriveThigh = 48f;
        public const float PushPlantKnee = -6f;
        public const float PushDriveKnee = -62f;

        /// <summary>Air head that rides with the fall beat. The fall limbs are JumpPose.</summary>
        public const float ReleaseHead = -6f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head, LeanZ;
        }

        /// <summary>0 while climbing, 1 at the locked slip speed and below.</summary>
        public static float SlipWeight(float verticalSpeed)
        {
            return Mathf.SmoothStep(0f, 1f, Inv(0.5f, -SlipSpeedRef, verticalSpeed));
        }

        /// <summary>Radians per second. 0 at rest, full at the locked climb speed. Upward only.</summary>
        public static float ClimbRate(float verticalSpeed)
        {
            float up = verticalSpeed > 0f ? verticalSpeed : 0f;
            return ClimbCadenceFull * Cap(up / ClimbSpeedRef);
        }

        /// <summary>Radians per second of the drag. 0 unless the body is moving down.</summary>
        public static float SlipRate(float verticalSpeed)
        {
            float down = verticalSpeed < 0f ? -verticalSpeed : 0f;
            return SlipCadenceFull * Cap(down / SlipSpeedRef);
        }

        /// <summary>Radians per second. 0 at rest, full at the locked wall-run speed.</summary>
        public static float RunRate(float alongSpeed)
        {
            float s = alongSpeed > 0f ? alongSpeed : 0f;
            return RunCadenceFull * Cap(s / WallRunSpeedRef);
        }

        /// <summary>Phase rate for whichever wall pose is up. Climb blends into the drag as vy falls.</summary>
        public static float SurfRate(bool climb, float verticalSpeed, float alongSpeed)
        {
            if (!climb) return RunRate(alongSpeed);
            float slip = SlipWeight(verticalSpeed);
            return Mathf.Lerp(ClimbRate(verticalSpeed), SlipRate(verticalSpeed), slip);
        }

        /// <summary>
        /// Hand-over-hand. phaseSin +1 reaches with the left hand and drives the right knee.
        /// Slip weight pulls both hands down into the drag.
        /// </summary>
        public static Sample Climb(float phaseSin, float verticalSpeed)
        {
            float reachL = (phaseSin + 1f) * 0.5f;
            float reachR = 1f - reachL;
            float slip = SlipWeight(verticalSpeed);
            float pitchL = Mathf.Lerp(Mathf.Lerp(PullPitch, ReachPitch, reachL), DragPitch + DragAlt * phaseSin, slip);
            float pitchR = Mathf.Lerp(Mathf.Lerp(PullPitch, ReachPitch, reachR), DragPitch - DragAlt * phaseSin, slip);
            float yawL = Mathf.Lerp(Mathf.Lerp(PullYaw, ReachYaw, reachL), DragYaw, slip);
            float yawR = Mathf.Lerp(-Mathf.Lerp(PullYaw, ReachYaw, reachR), -DragYaw, slip);
            float elbowL = Mathf.Lerp(Mathf.Lerp(PullElbow, ReachElbow, reachL), DragElbow, slip);
            float elbowR = Mathf.Lerp(Mathf.Lerp(PullElbow, ReachElbow, reachR), DragElbow, slip);
            float driveL = reachR;
            float driveR = reachL;
            float thighL = Mathf.Lerp(Mathf.Lerp(PlantThigh, DriveThigh, driveL), DragThigh + DragAlt * phaseSin, slip);
            float thighR = Mathf.Lerp(Mathf.Lerp(PlantThigh, DriveThigh, driveR), DragThigh - DragAlt * phaseSin, slip);
            float kneeL = Mathf.Lerp(Mathf.Lerp(PlantKnee, DriveKnee, driveL), DragKnee, slip);
            float kneeR = Mathf.Lerp(Mathf.Lerp(PlantKnee, DriveKnee, driveR), DragKnee, slip);
            return new Sample
            {
                ThighL = thighL,
                ThighR = thighR,
                KneeL = kneeL,
                KneeR = kneeR,
                ArmPitchL = pitchL,
                ArmPitchR = pitchR,
                ArmYawL = yawL,
                ArmYawR = yawR,
                ElbowL = elbowL,
                ElbowR = elbowR,
                Hip = Mathf.Lerp(ClimbHip, SlipHip, slip),
                Spine = Mathf.Lerp(ClimbSpine, SlipSpine, slip),
                Head = Mathf.Lerp(ClimbHead, SlipHead, slip),
                LeanZ = 0f,
            };
        }

        /// <summary>
        /// Stride along the wall. phaseSin +1 puts the outer thigh forward and the outer arm back.
        /// The inner arm stays low. wallLeft rolls the chest off the wall.
        /// </summary>
        public static Sample Run(float phaseSin, bool wallLeft)
        {
            float fwd = (phaseSin + 1f) * 0.5f;
            float back = 1f - fwd;
            float outerPitch = Mathf.Lerp(OuterFwdPitch, OuterBackPitch, fwd);
            float outerElbow = Mathf.Lerp(OuterFwdElbow, OuterBackElbow, fwd);
            float outerThigh = Mathf.Lerp(OuterThighBack, OuterThighFwd, fwd);
            float outerKnee = Mathf.Lerp(RunKneeBack, RunKneeFwd, fwd);
            float innerThigh = Mathf.Lerp(InnerThighFwd, InnerThighBack, fwd);
            float innerKnee = Mathf.Lerp(RunKneeFwd, RunKneeBack, fwd);
            float sway = (fwd - back) * InnerSway;
            float innerPitch = InnerPitch + sway;
            float lean = wallLeft ? -RunTilt : RunTilt;
            if (wallLeft)
            {
                return new Sample
                {
                    ThighL = innerThigh,
                    ThighR = outerThigh,
                    KneeL = innerKnee,
                    KneeR = outerKnee,
                    ArmPitchL = innerPitch,
                    ArmPitchR = outerPitch,
                    ArmYawL = InnerYaw,
                    ArmYawR = -OuterYaw,
                    ElbowL = InnerElbow,
                    ElbowR = outerElbow,
                    Hip = RunHip,
                    Spine = RunSpine,
                    Head = RunHead,
                    LeanZ = lean,
                };
            }

            return new Sample
            {
                ThighL = outerThigh,
                ThighR = innerThigh,
                KneeL = outerKnee,
                KneeR = innerKnee,
                ArmPitchL = outerPitch,
                ArmPitchR = innerPitch,
                ArmYawL = OuterYaw,
                ArmYawR = -InnerYaw,
                ElbowL = outerElbow,
                ElbowR = InnerElbow,
                Hip = RunHip,
                Spine = RunSpine,
                Head = RunHead,
                LeanZ = lean,
            };
        }

        /// <summary>plantLeft keeps the left shoe on the wall and lifts the right knee.</summary>
        public static Sample PushOff(bool plantLeft)
        {
            float thighL = plantLeft ? PushPlantThigh : PushDriveThigh;
            float thighR = plantLeft ? PushDriveThigh : PushPlantThigh;
            float kneeL = plantLeft ? PushPlantKnee : PushDriveKnee;
            float kneeR = plantLeft ? PushDriveKnee : PushPlantKnee;
            return new Sample
            {
                ThighL = thighL,
                ThighR = thighR,
                KneeL = kneeL,
                KneeR = kneeR,
                ArmPitchL = PushArmPitch,
                ArmPitchR = PushArmPitch,
                ArmYawL = PushArmYaw,
                ArmYawR = -PushArmYaw,
                ElbowL = PushElbow,
                ElbowR = PushElbow,
                Hip = PushHip,
                Spine = PushSpine,
                Head = PushHead,
                LeanZ = 0f,
            };
        }

        /// <summary>Hands down and open. A refused cling. Not a reach and not a pull.</summary>
        public const float SlideOffPitch = 36f;
        public const float SlideOffYaw = 42f;
        public const float SlideOffElbow = -20f;
        public const float SlideOffThigh = 18f;
        public const float SlideOffKnee = -24f;
        /// <summary>Chest off the wall. A grab pitches the other way.</summary>
        public const float SlideOffSpine = -10f;
        public const float SlideOffHip = 4f;
        public const float SlideOffHead = 8f;

        /// <summary>
        /// Both hands drop and open when the same wall will not take another grab.
        /// No reach, no pull, no into-wall chest. Nothing here writes velocity or the root.
        /// </summary>
        public static Sample SlideOff()
        {
            return new Sample
            {
                ThighL = SlideOffThigh,
                ThighR = SlideOffThigh,
                KneeL = SlideOffKnee,
                KneeR = SlideOffKnee,
                ArmPitchL = SlideOffPitch,
                ArmPitchR = SlideOffPitch,
                ArmYawL = SlideOffYaw,
                ArmYawR = -SlideOffYaw,
                ElbowL = SlideOffElbow,
                ElbowR = SlideOffElbow,
                Hip = SlideOffHip,
                Spine = SlideOffSpine,
                Head = SlideOffHead,
                LeanZ = 0f,
            };
        }

        /// <summary>JumpPose fall beat. Legs down, arms wide. Release blends here.</summary>
        public static Sample Fall()
        {
            return new Sample
            {
                ThighL = JumpPose.FallThigh,
                ThighR = JumpPose.FallThigh,
                KneeL = JumpPose.FallKnee,
                KneeR = JumpPose.FallKnee,
                ArmPitchL = JumpPose.FallArmPitch,
                ArmPitchR = JumpPose.FallArmPitch,
                ArmYawL = JumpPose.FallArmYaw,
                ArmYawR = -JumpPose.FallArmYaw,
                ElbowL = JumpPose.FallElbow,
                ElbowR = JumpPose.FallElbow,
                Hip = JumpPose.FallHip,
                Spine = JumpPose.FallSpine,
                Head = ReleaseHead,
                LeanZ = 0f,
            };
        }

        /// <summary>
        /// Stick has slipped, grace is still running, and the probe is still on the wall.
        /// The body stays on the climb, slip, or wall-run sample. A spent grace or a lost
        /// contact is the fall blend. Nothing here writes velocity, cling, or the root.
        /// </summary>
        public static bool GraceCommit(float graceRemaining, bool wallContact)
        {
            return wallContact && graceRemaining > 0f;
        }

        /// <summary>
        /// 0 while <see cref="GraceCommit"/> is true, so one leftover frame of grace
        /// does not start the fall. After grace expires or the probe leaves, the same
        /// release curve into <see cref="Fall"/>. Age 0 is the detach frame.
        /// </summary>
        public static float GraceFallWeight(float graceRemaining, bool wallContact, float detachAge)
        {
            if (GraceCommit(graceRemaining, wallContact)) return 0f;
            return BlendWeight(detachAge, ReleaseBlendSeconds);
        }

        /// <summary>Smoothstep. 0 at the start of the blend, 1 at the end.</summary>
        public static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        public static float BlendWeight(float age, float seconds)
        {
            if (seconds <= 0.0001f) return 1f;
            if (age <= 0f) return 0f;
            if (age >= seconds) return 1f;
            return Ease(age / seconds);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (!InBand(AirBlendSeconds) || !InBand(PushBlendSeconds) || !InBand(ReleaseBlendSeconds)) return false;
            if (Ease(0f) > 0.0001f || Mathf.Abs(Ease(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Ease(0.5f) - 0.5f) > 0.0001f) return false;
            if (BlendWeight(0f, AirBlendSeconds) > 0.0001f) return false;
            if (Mathf.Abs(BlendWeight(AirBlendSeconds, AirBlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(BlendWeight(PushBlendSeconds * 0.5f, PushBlendSeconds) - 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(BlendWeight(ReleaseBlendSeconds, ReleaseBlendSeconds) - 1f) > 0.0001f) return false;
            float prevEase = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float w = Ease(i / 8f);
                if (w + 0.0001f < prevEase) return false;
                prevEase = w;
            }

            if (Mathf.Abs(ClimbSpeedRef - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(SlipSpeedRef - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(WallRunSpeedRef - 9.5f) > 0.001f) return false;
            if (ClimbRate(0f) > 0.0001f || SlipRate(0f) > 0.0001f || RunRate(0f) > 0.0001f) return false;
            if (Mathf.Abs(ClimbRate(ClimbSpeedRef) - ClimbCadenceFull) > 0.02f) return false;
            if (Mathf.Abs(SlipRate(-SlipSpeedRef) - SlipCadenceFull) > 0.02f) return false;
            if (Mathf.Abs(RunRate(WallRunSpeedRef) - RunCadenceFull) > 0.02f) return false;
            if (!(ClimbRate(ClimbSpeedRef) > ClimbRate(ClimbSpeedRef * 0.5f) && ClimbRate(ClimbSpeedRef * 0.5f) > ClimbRate(0f))) return false;
            if (!(SlipRate(-SlipSpeedRef) > SlipRate(-SlipSpeedRef * 0.5f))) return false;
            if (!(RunRate(WallRunSpeedRef) > RunRate(WallRunSpeedRef * 0.5f))) return false;
            if (ClimbRate(-SlipSpeedRef) > 0.0001f) return false;
            if (SlipRate(ClimbSpeedRef) > 0.0001f) return false;
            if (Mathf.Abs(SurfRate(true, ClimbSpeedRef, 0f) - ClimbRate(ClimbSpeedRef)) > 0.02f) return false;
            if (Mathf.Abs(SurfRate(true, -SlipSpeedRef, 0f) - SlipRate(-SlipSpeedRef)) > 0.02f) return false;
            if (Mathf.Abs(SurfRate(false, 0f, WallRunSpeedRef) - RunRate(WallRunSpeedRef)) > 0.02f) return false;
            if (!(ClimbRate(ClimbSpeedRef) > SlipRate(-SlipSpeedRef))) return false;

            if (SlipWeight(ClimbSpeedRef) > 0.0001f) return false;
            if (Mathf.Abs(SlipWeight(-SlipSpeedRef) - 1f) > 0.0001f) return false;
            float prevSlip = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float vy = Mathf.Lerp(ClimbSpeedRef, -SlipSpeedRef, i / 8f);
                float w = SlipWeight(vy);
                if (w + 0.0001f < prevSlip) return false;
                prevSlip = w;
            }

            Sample reach = Climb(1f, ClimbSpeedRef);
            Sample other = Climb(-1f, ClimbSpeedRef);
            if (reach.ArmPitchL >= reach.ArmPitchR) return false;
            if (reach.ThighR <= reach.ThighL) return false;
            if (reach.KneeR >= reach.KneeL) return false;
            if (other.ArmPitchR >= other.ArmPitchL) return false;
            if (other.ThighL <= other.ThighR) return false;
            if (Mathf.Abs(reach.ArmPitchL - ReachPitch) > 0.05f) return false;
            if (Mathf.Abs(reach.ArmPitchR - PullPitch) > 0.05f) return false;
            if (reach.ArmPitchR - reach.ArmPitchL < 70f) return false;
            if (reach.Spine < 24f || reach.Hip < 16f) return false;
            if (reach.Head > -20f) return false;
            if (!(reach.ElbowL > reach.ElbowR)) return false;

            Sample drag = Climb(1f, -SlipSpeedRef);
            float dragGap = drag.ArmPitchL - drag.ArmPitchR;
            if (dragGap < 0f) dragGap = -dragGap;
            if (dragGap > 22f || dragGap < 4f) return false;
            if (drag.ArmPitchL < ReachPitch + 40f) return false;
            if (drag.KneeL > -30f || drag.KneeR > -30f) return false;
            if (Mathf.Abs(drag.KneeL - drag.KneeR) > 0.05f) return false;
            if (drag.Head >= 0f || drag.Head <= reach.Head) return false;
            if (drag.Spine >= reach.Spine) return false;

            Sample runL = Run(1f, true);
            Sample runLBack = Run(-1f, true);
            if (runL.LeanZ > -30f) return false;
            if (runL.ThighR <= runL.ThighL) return false;
            if (runLBack.ThighR >= runLBack.ThighL) return false;
            if (runL.ArmPitchR <= runLBack.ArmPitchR) return false;
            float outerSpan = runL.ArmPitchR - runLBack.ArmPitchR;
            if (outerSpan < 70f) return false;
            float innerSpan = runL.ArmPitchL - runLBack.ArmPitchL;
            if (innerSpan < 0f) innerSpan = -innerSpan;
            if (innerSpan > 16f) return false;
            if (runL.ArmPitchL < -40f || runLBack.ArmPitchL < -40f) return false;
            if (runL.KneeR >= runLBack.KneeR) return false;

            Sample runR = Run(1f, false);
            if (runR.LeanZ < 30f) return false;
            if (runR.ThighL <= runR.ThighR) return false;
            if (runL.LeanZ >= 0f || runR.LeanZ <= 0f) return false;

            Sample push = PushOff(true);
            if (push.ThighR <= push.ThighL) return false;
            if (push.KneeR >= push.KneeL) return false;
            if (push.ArmYawL > 24f) return false;
            Sample pushR = PushOff(false);
            if (pushR.ThighL <= pushR.ThighR) return false;

            Sample fall = Fall();
            if (fall.ThighL != JumpPose.FallThigh || fall.ThighR != JumpPose.FallThigh) return false;
            if (fall.KneeL != JumpPose.FallKnee || fall.KneeR != JumpPose.FallKnee) return false;
            if (fall.ArmPitchL != JumpPose.FallArmPitch || fall.ArmPitchR != JumpPose.FallArmPitch) return false;
            if (fall.ArmYawL != JumpPose.FallArmYaw || fall.ArmYawR != -JumpPose.FallArmYaw) return false;
            if (fall.ElbowL != JumpPose.FallElbow || fall.ElbowR != JumpPose.FallElbow) return false;
            if (fall.Spine != JumpPose.FallSpine || fall.Hip != JumpPose.FallHip) return false;
            if (fall.Head != ReleaseHead) return false;
            if (fall.ArmYawL <= push.ArmYawL + 20f) return false;

            if (Mathf.Abs(ClingGraceSeconds - 0.08f) > 0.001f) return false;
            if (!GraceCommit(ClingGraceSeconds, true)) return false;
            // One frame at 60 Hz still has grace left. The body stays on the wall.
            float frame = 1f / 60f;
            if (!GraceCommit(ClingGraceSeconds - frame, true)) return false;
            if (GraceFallWeight(ClingGraceSeconds - frame, true, ReleaseBlendSeconds) > 0.0001f) return false;
            if (GraceCommit(0f, true)) return false;
            if (GraceCommit(ClingGraceSeconds, false)) return false;
            if (GraceCommit(-frame, true)) return false;
            if (GraceFallWeight(ClingGraceSeconds, true, 0f) > 0.0001f) return false;
            if (GraceFallWeight(0f, true, 0f) > 0.0001f) return false;
            if (GraceFallWeight(ClingGraceSeconds, false, 0f) > 0.0001f) return false;
            if (Mathf.Abs(GraceFallWeight(0f, true, ReleaseBlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(GraceFallWeight(ClingGraceSeconds, false, ReleaseBlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(GraceFallWeight(0f, true, ReleaseBlendSeconds * 0.5f) - 0.5f) > 0.0001f) return false;
            float prevFall = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float w = GraceFallWeight(0f, false, ReleaseBlendSeconds * (i / 8f));
                if (w + 0.0001f < prevFall) return false;
                prevFall = w;
            }
            if (Mathf.Abs(reach.ArmPitchL - fall.ArmPitchL) < 20f) return false;
            if (Mathf.Abs(runL.LeanZ - fall.LeanZ) < 20f) return false;
            return true;
        }

        public static string GraceProofLine()
        {
            float frame = 1f / 60f;
            bool frameHolds = GraceCommit(ClingGraceSeconds - frame, true)
                && GraceFallWeight(ClingGraceSeconds - frame, true, ReleaseBlendSeconds) <= 0.0001f;
            bool expiryDetaches = !GraceCommit(0f, true)
                && GraceFallWeight(0f, true, ReleaseBlendSeconds) >= 0.999f;
            bool leaveDetaches = !GraceCommit(ClingGraceSeconds, false)
                && GraceFallWeight(ClingGraceSeconds, false, ReleaseBlendSeconds) >= 0.999f;
            Sample fall = Fall();
            bool fallStack = fall.ThighL == JumpPose.FallThigh
                && fall.ArmPitchL == JumpPose.FallArmPitch
                && fall.Spine == JumpPose.FallSpine;
            return "cling-grace-pose"
                + " grace=" + ClingGraceSeconds.ToString("0.00")
                + " frameHolds=" + (frameHolds ? "1" : "0")
                + " expiryDetaches=" + (expiryDetaches ? "1" : "0")
                + " leaveDetaches=" + (leaveDetaches ? "1" : "0")
                + " fallStack=" + (fallStack ? "1" : "0")
                + " commit while grace>0 and wallContact, body stays climb/slip/run"
                + " detach:Ease(t/" + ReleaseBlendSeconds.ToString("0.00") + ") into JumpPose fall"
                + " shared=DummyLocomotor"
                + " rootMotion=0";
        }

        public static string ProofLine()
        {
            Sample reach = Climb(1f, ClimbSpeedRef);
            Sample drag = Climb(1f, -SlipSpeedRef);
            Sample run = Run(1f, true);
            Sample fall = Fall();
            return "wall pose"
                + " reachPitch=" + ReachPitch.ToString("0")
                + " pullPitch=" + PullPitch.ToString("0")
                + " reachYaw=" + ReachYaw.ToString("0")
                + " pullYaw=" + PullYaw.ToString("0")
                + " reachElbow=" + ReachElbow.ToString("0")
                + " pullElbow=" + PullElbow.ToString("0")
                + " driveThigh=" + DriveThigh.ToString("0")
                + " plantThigh=" + PlantThigh.ToString("0")
                + " driveKnee=" + DriveKnee.ToString("0")
                + " plantKnee=" + PlantKnee.ToString("0")
                + " climbSpine=" + ClimbSpine.ToString("0")
                + " climbHip=" + ClimbHip.ToString("0")
                + " climbHead=" + ClimbHead.ToString("0")
                + " dragPitch=" + DragPitch.ToString("0")
                + " dragYaw=" + DragYaw.ToString("0")
                + " dragElbow=" + DragElbow.ToString("0")
                + " dragThigh=" + DragThigh.ToString("0")
                + " dragKnee=" + DragKnee.ToString("0")
                + " dragAlt=" + DragAlt.ToString("0")
                + " slipSpine=" + SlipSpine.ToString("0")
                + " slipHip=" + SlipHip.ToString("0")
                + " slipHead=" + SlipHead.ToString("0")
                + " runTilt=" + RunTilt.ToString("0")
                + " innerPitch=" + InnerPitch.ToString("0")
                + " innerSway=" + InnerSway.ToString("0")
                + " innerYaw=" + InnerYaw.ToString("0")
                + " innerElbow=" + InnerElbow.ToString("0")
                + " outerFwd=" + OuterFwdPitch.ToString("0")
                + " outerBack=" + OuterBackPitch.ToString("0")
                + " outerYaw=" + OuterYaw.ToString("0")
                + " outerThigh=" + OuterThighFwd.ToString("0") + "/" + OuterThighBack.ToString("0")
                + " innerThigh=" + InnerThighFwd.ToString("0") + "/" + InnerThighBack.ToString("0")
                + " runKnee=" + RunKneeFwd.ToString("0") + "/" + RunKneeBack.ToString("0")
                + " runSpine=" + RunSpine.ToString("0")
                + " runHip=" + RunHip.ToString("0")
                + " runHead=" + RunHead.ToString("0")
                + " pushPitch=" + PushArmPitch.ToString("0")
                + " pushYaw=" + PushArmYaw.ToString("0")
                + " pushElbow=" + PushElbow.ToString("0")
                + " pushDrive=" + PushDriveThigh.ToString("0") + "/" + PushDriveKnee.ToString("0")
                + " pushPlant=" + PushPlantThigh.ToString("0") + "/" + PushPlantKnee.ToString("0")
                + " fallThigh=" + fall.ThighL.ToString("0")
                + " fallYaw=" + fall.ArmYawL.ToString("0")
                + " climbRate=" + ClimbRate(ClimbSpeedRef).ToString("0.00")
                + " slipRate=" + SlipRate(-SlipSpeedRef).ToString("0.00")
                + " runRate=" + RunRate(WallRunSpeedRef).ToString("0.00")
                + " reachRead=" + reach.ArmPitchL.ToString("0")
                + " dragRead=" + drag.ArmPitchL.ToString("0")
                + " tiltRead=" + run.LeanZ.ToString("0")
                + " airBlend=" + AirBlendSeconds.ToString("0.00")
                + " pushBlend=" + PushBlendSeconds.ToString("0.00")
                + " releaseBlend=" + ReleaseBlendSeconds.ToString("0.00")
                + " gate=air:Ease(t/" + AirBlendSeconds.ToString("0.00") + ") on enter from air, not crouch, not dash"
                + "; push:Ease(t/" + PushBlendSeconds.ToString("0.00") + ") cling+Jump from climb or wall run into push-off, WallJumpPushTell unchanged"
                + "; release:Ease(t/" + ReleaseBlendSeconds.ToString("0.00") + ") when cling grace expires or the probe leaves the wall, target JumpPose fall"
                + "; cling-grace-pose grace=" + ClingGraceSeconds.ToString("0.00")
                + " commit while grace>0 and wallContact, body stays climb/slip/run"
                + "; detach:Ease(t/" + ReleaseBlendSeconds.ToString("0.00") + ") on grace expiry or lost contact, target JumpPose fall"
                + "; one frame of grace still commits"
                + "; cadence:climb vy/" + ClimbSpeedRef.ToString("0.0") + "*" + ClimbCadenceFull.ToString("0.0")
                + " slip |vy|/" + SlipSpeedRef.ToString("0.0") + "*" + SlipCadenceFull.ToString("0.0")
                + " run speed/" + WallRunSpeedRef.ToString("0.0") + "*" + RunCadenceFull.ToString("0.0")
                + "; slipWeight:SmoothStep InverseLerp(vy 0.5.." + (-SlipSpeedRef).ToString("0.0") + ")"
                + "; rootMotion=0";
        }

        static bool InBand(float seconds) => seconds >= 0.08f && seconds <= 0.12f;

        static float Cap(float t)
        {
            if (t < 0f) return 0f;
            if (t > 1.5f) return 1.5f;
            return t;
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
