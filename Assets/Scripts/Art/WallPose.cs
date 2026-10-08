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

        /// <summary>Radians per second at <see cref="ClimbSpeedRef"/>. A hand plant covers about a meter.</summary>
        public const float ClimbCadenceFull = 16.5f;
        /// <summary>Radians per second at <see cref="SlipSpeedRef"/> downward. A drag, not a second climb.</summary>
        public const float SlipCadenceFull = 5.2f;
        /// <summary>Radians per second at <see cref="WallRunSpeedRef"/>. Same step rate as the ground gait at 9.5.</summary>
        public const float RunCadenceFull = 26.5f;

        /// <summary>Zip and cable hang keep this reach. The climb uses <see cref="ClimbReachPitch"/>.</summary>
        public const float ReachPitch = -100f;
        /// <summary>High hand on the wall in front. Open elbow, yaw slightly in.</summary>
        public const float ClimbReachPitch = -120f;
        public const float PullPitch = -70f;
        public const float ReachYaw = -16f;
        public const float PullYaw = 16f;
        public const float ReachElbow = -16f;
        public const float PullElbow = -48f;
        public const float DriveThigh = 76f;
        public const float PlantThigh = 56f;
        public const float DriveKnee = -88f;
        public const float PlantKnee = -60f;
        /// <summary>Chest into the wall, short of folding the hands off the surface.</summary>
        public const float ClimbSpine = 14f;
        public const float ClimbHip = 8f;
        /// <summary>Look up the wall. Negative is up, same sign as the air head.</summary>
        public const float ClimbHead = -26f;

        public const float DragPitch = -112f;
        public const float DragYaw = 8f;
        public const float DragElbow = -14f;
        public const float DragThigh = 40f;
        public const float DragKnee = -90f;
        /// <summary>Leftover sway while the hands drag. Not a reach.</summary>
        public const float DragAlt = 6f;
        public const float SlipSpine = 6f;
        public const float SlipHip = 10f;
        public const float SlipHead = -6f;
        /// <summary>Visual drop while the hands slide. The capsule does not move.</summary>
        public const float SlipSag = 0.18f;

        /// <summary>Roll off the wall, 15–20°. Wall-on-the-left is negative.</summary>
        public const float RunTilt = 15f;
        public const float InnerPitch = -32f;
        public const float InnerSway = 3f;
        /// <summary>Near arm stays up off the plant thigh and on the body side of the wall.</summary>
        public const float InnerYaw = -14f;
        /// <summary>Abduct the plant leg so the shoe, not the shoulder, meets the wall.</summary>
        public const float PlantRoll = -36f;
        public const float InnerElbow = -48f;
        public const float OuterFwdPitch = -78f;
        public const float OuterBackPitch = 24f;
        public const float OuterYaw = -16f;
        public const float OuterFwdElbow = -12f;
        public const float OuterBackElbow = -36f;
        public const float OuterThighFwd = 48f;
        public const float OuterThighBack = -22f;
        public const float InnerThighFwd = 18f;
        public const float InnerThighBack = -10f;
        public const float RunKneeFwd = -20f;
        public const float RunKneeBack = -8f;
        public const float RunSpine = 12f;
        public const float RunHip = 6f;
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
            public float ThighRollL, ThighRollR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head, LeanZ;
            public float FootL, FootR;
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
        /// Hands up to the wall, chest not on it yet. The still plant is <see cref="Hold"/>.
        /// Nothing here writes velocity or the root.
        /// </summary>
        public static Sample Entry()
        {
            return new Sample
            {
                ThighL = 28f,
                ThighR = 22f,
                KneeL = -40f,
                KneeR = -34f,
                ArmPitchL = -78f,
                ArmPitchR = -70f,
                ArmYawL = 22f,
                ArmYawR = -18f,
                ElbowL = -28f,
                ElbowR = -24f,
                Hip = 4f,
                Spine = 6f,
                Head = -10f,
                LeanZ = 0f,
                FootL = 8f,
                FootR = 8f,
            };
        }

        /// <summary>Both hands planted. A cling with almost no vertical speed. Not a reach and not a drag.</summary>
        public static Sample Hold()
        {
            return new Sample
            {
                ThighL = 62f,
                ThighR = 58f,
                KneeL = -78f,
                KneeR = -72f,
                ArmPitchL = -96f,
                ArmPitchR = -92f,
                ArmYawL = 10f,
                ArmYawR = -10f,
                ElbowL = -36f,
                ElbowR = -36f,
                Hip = 10f,
                Spine = 16f,
                Head = -18f,
                LeanZ = 0f,
                FootL = 10f,
                FootR = 10f,
            };
        }

        /// <summary>1 at rest, 0 once the climb or the slip is clearly moving. Endpoints of the cycle stay put.</summary>
        public static float HoldWeight(float verticalSpeed)
        {
            float a = verticalSpeed < 0f ? -verticalSpeed : verticalSpeed;
            if (a >= 1.8f) return 0f;
            if (a <= 0.2f) return 1f;
            float u = (a - 0.2f) / 1.6f;
            float s = u * u * (3f - 2f * u);
            return 1f - s;
        }

        /// <summary>
        /// Keeps ±1 so a full reach is still a full reach, and spends the middle of the
        /// cycle on that plant instead of a halfway hand.
        /// </summary>
        public static float PlantShape(float phaseSin)
        {
            float s = phaseSin;
            if (s > 1f) s = 1f;
            if (s < -1f) s = -1f;
            float a = s < 0f ? -s : s;
            float u;
            if (a <= 0.18f) u = 0f;
            else if (a >= 0.62f) u = 1f;
            else
            {
                float t = (a - 0.18f) / 0.44f;
                u = t * t * (3f - 2f * t);
            }
            return s < 0f ? -u : u;
        }

        public static Sample Mix(Sample a, Sample b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return new Sample
            {
                ThighL = a.ThighL + (b.ThighL - a.ThighL) * t,
                ThighR = a.ThighR + (b.ThighR - a.ThighR) * t,
                ThighRollL = a.ThighRollL + (b.ThighRollL - a.ThighRollL) * t,
                ThighRollR = a.ThighRollR + (b.ThighRollR - a.ThighRollR) * t,
                KneeL = a.KneeL + (b.KneeL - a.KneeL) * t,
                KneeR = a.KneeR + (b.KneeR - a.KneeR) * t,
                ArmPitchL = a.ArmPitchL + (b.ArmPitchL - a.ArmPitchL) * t,
                ArmPitchR = a.ArmPitchR + (b.ArmPitchR - a.ArmPitchR) * t,
                ArmYawL = a.ArmYawL + (b.ArmYawL - a.ArmYawL) * t,
                ArmYawR = a.ArmYawR + (b.ArmYawR - a.ArmYawR) * t,
                ElbowL = a.ElbowL + (b.ElbowL - a.ElbowL) * t,
                ElbowR = a.ElbowR + (b.ElbowR - a.ElbowR) * t,
                Hip = a.Hip + (b.Hip - a.Hip) * t,
                Spine = a.Spine + (b.Spine - a.Spine) * t,
                Head = a.Head + (b.Head - a.Head) * t,
                LeanZ = a.LeanZ + (b.LeanZ - a.LeanZ) * t,
                FootL = a.FootL + (b.FootL - a.FootL) * t,
                FootR = a.FootR + (b.FootR - a.FootR) * t,
            };
        }

        /// <summary>
        /// Hand-over-hand. phaseSin +1 reaches with the left hand and drives the right knee.
        /// The plant holds near each extreme. A near-zero vertical speed is the cling hold.
        /// Slip weight pulls both hands down into the drag.
        /// </summary>
        public static Sample Climb(float phaseSin, float verticalSpeed)
        {
            float shaped = PlantShape(phaseSin);
            float reachL = (shaped + 1f) * 0.5f;
            float reachR = 1f - reachL;
            float slip = SlipWeight(verticalSpeed);
            float pitchL = Mathf.Lerp(Mathf.Lerp(PullPitch, ClimbReachPitch, reachL), DragPitch + DragAlt * phaseSin, slip);
            float pitchR = Mathf.Lerp(Mathf.Lerp(PullPitch, ClimbReachPitch, reachR), DragPitch - DragAlt * phaseSin, slip);
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
            Sample moving = new Sample
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
                FootL = Mathf.Lerp(12f, 8f, slip),
                FootR = Mathf.Lerp(12f, 8f, slip),
            };
            float hold = HoldWeight(verticalSpeed);
            if (hold <= 0.0001f) return moving;
            return Mix(moving, Hold(), hold);
        }

        /// <summary>
        /// Same stride as the ground gait at the locked wall-run speed, rolled off the wall.
        /// The inner hand stays low and out, brushing the wall. The outer arm opposes the outer thigh.
        /// </summary>
        public static Sample RunCycle(float phase, bool wallLeft)
        {
            GaitBlend.Legs legs = GaitBlend.At(phase, WallRunSpeedRef);
            float sin = Mathf.Sin(phase);
            float sway = sin * InnerSway;
            float innerPitch = InnerPitch + sway;
            float lean = wallLeft ? -RunTilt : RunTilt;
            float outerThigh = wallLeft ? legs.ThighR : legs.ThighL;
            float span = OuterThighFwd - OuterThighBack;
            float along = span > 0.01f ? (outerThigh - OuterThighBack) / span : 0.5f;
            if (along < 0f) along = 0f;
            if (along > 1f) along = 1f;
            float outerPitch = Mathf.Lerp(OuterFwdPitch, OuterBackPitch, along);
            float outerElbow = Mathf.Lerp(OuterFwdElbow, OuterBackElbow, along);
            // The inner swing stays tucked. The planted foot keeps the full step,
            // plus a short toe-off, so a 9.5 wall run does not skate.
            const float pi = 3.14159265f;
            float cL = Mathf.Cos(phase);
            float cR = Mathf.Cos(phase + pi);
            float tuckL = wallLeft && cL > 0f ? 0.72f : 1f;
            float tuckR = !wallLeft && cR > 0f ? 0.72f : 1f;
            float thighL = legs.ThighL * tuckL - FootSlide.WallTrail(legs.ThighL * tuckL);
            float thighR = legs.ThighR * tuckR - FootSlide.WallTrail(legs.ThighR * tuckR);
            float kneeL = legs.KneeL;
            float kneeR = legs.KneeR;
            // A deep inner tuck lifts the shoe behind the shoulder, and the wall
            // test then reads the upper arm as inside the surface.
            if (wallLeft && kneeL < -40f) kneeL = -40f;
            if (!wallLeft && kneeR < -40f) kneeR = -40f;
            float rollL = wallLeft ? PlantRoll : 0f;
            float rollR = wallLeft ? 0f : -PlantRoll;
            float footL = wallLeft ? 6f : GaitBlend.SoleLevelDeg(thighL, kneeL);
            float footR = wallLeft ? GaitBlend.SoleLevelDeg(thighR, kneeR) : 6f;
            if (wallLeft)
            {
                return new Sample
                {
                    ThighL = thighL,
                    ThighR = thighR,
                    ThighRollL = rollL,
                    ThighRollR = rollR,
                    KneeL = kneeL,
                    KneeR = kneeR,
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
                    FootL = footL,
                    FootR = footR,
                };
            }

            return new Sample
            {
                ThighL = thighL,
                ThighR = thighR,
                ThighRollL = rollL,
                ThighRollR = rollR,
                KneeL = kneeL,
                KneeR = kneeR,
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
                FootL = footL,
                FootR = footR,
            };
        }

        /// <summary>
        /// Peak of <see cref="RunCycle"/>. phaseSin +1 puts the outer thigh forward and the outer arm back.
        /// The inner hand stays on the wall. wallLeft rolls the chest off the wall.
        /// </summary>
        public static Sample Run(float phaseSin, bool wallLeft)
        {
            // Outer leg is phase+pi when the wall is on the left, so its forward beat is phase 3π/2.
            float phase = phaseSin >= 0f ? 4.71238898f : 1.5707963f;
            if (!wallLeft)
                phase = phaseSin >= 0f ? 1.5707963f : 4.71238898f;
            return RunCycle(phase, wallLeft);
        }

        /// <summary>Both feet leave the wall. Same kick as <see cref="WallJumpPose"/>.</summary>
        public static Sample PushOff(bool plantLeft)
        {
            WallJumpPose.Sample kick = WallJumpPose.Push(plantLeft);
            return new Sample
            {
                ThighL = kick.ThighL,
                ThighR = kick.ThighR,
                KneeL = kick.KneeL,
                KneeR = kick.KneeR,
                ArmPitchL = kick.ArmPitchL,
                ArmPitchR = kick.ArmPitchR,
                ArmYawL = kick.ArmYawL,
                ArmYawR = kick.ArmYawR,
                ElbowL = kick.ElbowL,
                ElbowR = kick.ElbowR,
                Hip = kick.Hip,
                Spine = kick.Spine,
                Head = kick.Head,
                LeanZ = kick.LeanZ,
            };
        }

        /// <summary>
        /// Both hands up on a cable. The arms stay the cling reach so the grab
        /// still reads. The legs and the chest are the zip hang, not a wall climb.
        /// Not a ledge grab and not a shimmy. Nothing here writes velocity or the root.
        /// </summary>
        public static Sample CableHang()
        {
            ZipPose.Sample hang = ZipPose.Hang();
            return new Sample
            {
                ThighL = hang.ThighL,
                ThighR = hang.ThighR,
                KneeL = hang.KneeL,
                KneeR = hang.KneeR,
                ArmPitchL = ReachPitch,
                ArmPitchR = ReachPitch,
                ArmYawL = hang.ArmYawL,
                ArmYawR = hang.ArmYawR,
                ElbowL = hang.ElbowL,
                ElbowR = hang.ElbowR,
                Hip = hang.Hip,
                Spine = hang.Spine,
                Head = hang.Head,
                LeanZ = hang.LeanZ,
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
            if (Mathf.Abs(reach.ArmPitchL - ClimbReachPitch) > 0.05f) return false;
            if (Mathf.Abs(reach.ArmPitchR - PullPitch) > 0.05f) return false;
            if (reach.ArmPitchR - reach.ArmPitchL < 40f) return false;
            if (reach.Spine < 12f || reach.Hip < 6f) return false;
            if (reach.Head > -20f) return false;
            if (!(reach.ElbowL > reach.ElbowR)) return false;

            Sample drag = Climb(1f, -SlipSpeedRef);
            float dragGap = drag.ArmPitchL - drag.ArmPitchR;
            if (dragGap < 0f) dragGap = -dragGap;
            if (dragGap > 22f || dragGap < 4f) return false;
            if (drag.ArmPitchL > -80f || drag.ArmPitchR > -80f) return false;
            if (SlipSag < 0.10f) return false;
            if (drag.KneeL > -30f || drag.KneeR > -30f) return false;
            if (Mathf.Abs(drag.KneeL - drag.KneeR) > 0.05f) return false;
            if (drag.Head >= 0f || drag.Head <= reach.Head) return false;
            if (drag.Spine >= reach.Spine) return false;

            Sample runL = Run(1f, true);
            Sample runLBack = Run(-1f, true);
            if (runL.LeanZ > -15f || runL.LeanZ < -21f) return false;
            if (runL.ArmYawL > -4f) return false;
            if (runL.ThighR <= runL.ThighL) return false;
            if (runLBack.ThighR >= runLBack.ThighL) return false;
            if (runL.ArmPitchR <= runLBack.ArmPitchR) return false;
            float outerSpan = runL.ArmPitchR - runLBack.ArmPitchR;
            if (outerSpan < 70f) return false;
            float innerSpan = runL.ArmPitchL - runLBack.ArmPitchL;
            if (innerSpan < 0f) innerSpan = -innerSpan;
            if (innerSpan > 16f) return false;
            if (runL.ArmPitchL < -40f || runLBack.ArmPitchL < -40f) return false;
            if (runL.KneeR < -30f || runLBack.KneeR < -30f) return false;

            Sample runR = Run(1f, false);
            if (runR.LeanZ < 15f || runR.LeanZ > 21f) return false;
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
            if (fall.ArmYawL >= push.ArmYawL - 20f) return false;

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
            if (Mathf.Abs(runL.LeanZ - fall.LeanZ) < 14f) return false;
            if (RunTilt < 15f || RunTilt > 20f) return false;
            if (Mathf.Abs(RunCadenceFull - GaitBlend.CadenceAt(WallRunSpeedRef)) > 0.2f) return false;
            if (Mathf.Abs(PlantShape(1f) - 1f) > 0.001f || Mathf.Abs(PlantShape(-1f) + 1f) > 0.001f) return false;
            if (HoldWeight(ClimbSpeedRef) > 0.0001f || HoldWeight(-SlipSpeedRef) > 0.0001f) return false;
            if (HoldWeight(0f) < 0.999f) return false;
            Sample plant = Hold();
            Sample still = Climb(0.2f, 0f);
            if (Mathf.Abs(still.ArmPitchL - plant.ArmPitchL) > 0.05f) return false;
            if (Mathf.Abs(still.ThighL - plant.ThighL) > 0.05f) return false;
            Sample enter = Entry();
            if (enter.ArmPitchL > -60f || enter.ArmPitchR > -60f) return false;
            if (Mathf.Abs(enter.ArmPitchL - plant.ArmPitchL) < 8f) return false;
            Sample slipped = Climb(-0.4f, -SlipSpeedRef);
            if (Mathf.Abs(slipped.ArmPitchL - drag.ArmPitchL) < 1f && Mathf.Abs(slipped.ArmPitchR - drag.ArmPitchR) < 1f) return false;
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
            Sample kick = PushOff(true);
            Sample fall = Fall();
            return "wall pose"
                + " reachPitch=" + ReachPitch.ToString("0")
                + " climbReach=" + ClimbReachPitch.ToString("0")
                + " pullPitch=" + PullPitch.ToString("0")
                + " slipSag=" + SlipSag.ToString("0.00")
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
                + " pushPitch=" + kick.ArmPitchL.ToString("0")
                + " pushYaw=" + kick.ArmYawL.ToString("0")
                + " pushElbow=" + kick.ElbowL.ToString("0")
                + " pushDrive=" + kick.ThighR.ToString("0") + "/" + kick.KneeR.ToString("0")
                + " pushPlant=" + kick.ThighL.ToString("0") + "/" + kick.KneeL.ToString("0")
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
