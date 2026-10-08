using System;
using Tag.Local;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual grapple only. The solo pawn reaches along the aim, snaps the
    /// grappling hand to the rope, then hangs both hands on the line while the
    /// chest leans toward the latch. A miss throws that hand and the chest,
    /// then snaps them back, and latches nothing. Legs trail on a pull.
    /// Nothing here writes velocity, the rope, the camera, or the root.
    /// The hook still adds no vertical impulse and no jet.
    /// </summary>
    public static class GrapplePose
    {
        public const bool RootMotion = false;
        public const float VerticalImpulse = 0f;

        /// <summary>Left hand is the grappling hand. RMB still fires. The lead angles stay in the R sample fields.</summary>
        public const bool LeadRight = false;

        /// <summary>
        /// Extra outward yaw on both shoulders. The printed aim, latch, and pull
        /// yaws stay the sample. This is applied when the body is posed so the
        /// upper arm misses the chest. It is not a lift and it does not change a timer.
        /// </summary>
        public const float ShoulderFlare = 24f;

        /// <summary>Move the lead sample onto the left arm when the left hand fires.</summary>
        public static Sample ForBody(Sample s)
        {
            if (LeadRight) return s;
            float pitch = s.ArmPitchL;
            s.ArmPitchL = s.ArmPitchR;
            s.ArmPitchR = pitch;
            float yaw = s.ArmYawL;
            s.ArmYawL = -s.ArmYawR;
            s.ArmYawR = -yaw;
            float elbow = s.ElbowL;
            s.ElbowL = s.ElbowR;
            s.ElbowR = elbow;
            s.ArmYawL += ShoulderFlare;
            s.ArmYawR -= ShoulderFlare;
            return s;
        }

        /// <summary>Aim reach eases in. The aim preview stays on GrappleRopeTell.</summary>
        public const float AimBlendSeconds = 0.08f;
        /// <summary>Latch is the target on the attach frame. This is how long that snap leads.</summary>
        public const float LatchSnapSeconds = 0.07f;
        /// <summary>Release eases onto the fall beat. GrappleMissTell owns a miss.</summary>
        public const float ReleaseBlendSeconds = 0.10f;

        public const float PoseSlew = 170f;
        public const float AimSlew = 480f;
        public const float LatchSlew = 2400f;
        /// <summary>Fast enough that the recoil arrives inside the snap window.</summary>
        public const float MissSlew = 2400f;

        /// <summary>Throw. The lead arm and the chest open along the aim.</summary>
        public const float MissWhipSeconds = 0.07f;
        /// <summary>Recoil. The line breaks and the chest rocks back.</summary>
        public const float MissSnapSeconds = 0.09f;

        /// <summary>Outward planar speed that reads as a full pull. Vertical speed is not an input.</summary>
        public const float PlanarFull = 7f;
        /// <summary>A taut rope keeps the hands on the line even when outward speed is zero.</summary>
        public const float TautFloor = 0.70f;
        /// <summary>
        /// Chase-cam weight for that taut line. PullBlend stays the speed response.
        /// This lifts 0.70 off the gait so the arms are the line, not a punch return.
        /// </summary>
        public const float TautRead = 0.96f;
        /// <summary>No camera punch on a miss or a pull.</summary>
        public const float FovPop = 0f;

        /// <summary>Tuck weight is 0 at and above this vertical speed. The air-stride holds.</summary>
        public const float StrideVy = 3f;
        /// <summary>Tuck weight is 1 at and below this vertical speed.</summary>
        public const float TuckVy = -10f;
        public const float LeanYawMax = 36f;

        public const float AimPitchR = -64f;
        public const float AimYawR = 16f;
        public const float AimElbowR = -52f;
        public const float AimPitchL = -18f;
        public const float AimYawL = -34f;
        public const float AimElbowL = -22f;
        public const float AimSpine = 12f;
        public const float AimHip = 6f;
        public const float AimHead = -6f;
        public const float AimThigh = 10f;
        public const float AimKnee = -12f;
        public const float AimElevShare = 0.40f;
        public const float AimYawShare = 0.50f;

        public const float LatchPitchR = -88f;
        public const float LatchYawR = 28f;
        public const float LatchElbowR = -4f;
        public const float LatchPitchL = -42f;
        public const float LatchYawL = -28f;
        public const float LatchElbowL = -34f;
        public const float LatchSpine = 36f;
        public const float LatchHip = 20f;
        public const float LatchHead = -18f;

        public const float PullPitchL = -84f;
        public const float PullPitchR = -78f;
        public const float PullYawL = -32f;
        public const float PullYawR = 32f;
        public const float PullElbowL = -4f;
        public const float PullElbowR = -2f;
        public const float PullSpine = 42f;
        public const float PullHip = 40f;
        public const float PullHead = -20f;
        public const float HipYawShare = 0.40f;
        public const float HeadYawShare = 0.55f;

        public const float StrideThighFwd = 20f;
        public const float StrideThighBack = -30f;
        public const float StrideKneeFwd = -32f;
        public const float StrideKneeBack = -8f;
        public const float TuckThigh = 32f;
        public const float TuckKnee = -94f;
        public const float TuckAlt = 7f;

        public const float WhipPitchR = -106f;
        public const float WhipYawR = 18f;
        public const float WhipElbowR = -6f;
        public const float WhipPitchL = 28f;
        public const float WhipYawL = -24f;
        public const float WhipElbowL = -36f;
        public const float WhipSpine = 28f;
        public const float WhipHip = 16f;
        public const float WhipHead = -12f;
        public const float WhipSpineYaw = -14f;
        public const float WhipHeadYaw = -8f;

        public const float MissPitchR = 76f;
        public const float MissYawR = 28f;
        public const float MissElbowR = -112f;
        public const float MissPitchL = 12f;
        public const float MissYawL = -16f;
        public const float MissElbowL = -32f;
        public const float MissSpine = -32f;
        public const float MissHip = -10f;
        public const float MissHead = 18f;
        public const float MissSpineYaw = 18f;
        public const float MissHeadYaw = 10f;
        public const float MissHipYaw = 6f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head;
            public float HipYaw, SpineYaw, HeadYaw;
        }

        /// <summary>0 while slack and still, 1 when the rope is stripping outward speed. Vertical speed is ignored.</summary>
        public static float PullBlend(float outwardPlanar, bool taut)
        {
            float speed = Mathf.SmoothStep(0f, 1f, Cap01(outwardPlanar / PlanarFull));
            if (!taut) return speed;
            return Mathf.Lerp(TautFloor, 1f, speed);
        }

        /// <summary>
        /// Visual weight for the hang. PullBlend is unchanged.
        /// A taut rope lifts that blend onto the two-arm line so the gait
        /// and a punch return cannot sit in the arms. Slack stays on PullBlend.
        /// </summary>
        public static float ReadWeight(float outwardPlanar, bool taut, float latchWeight)
        {
            float pull = PullBlend(outwardPlanar, taut);
            float latch = latchWeight < 0f ? 0f : (latchWeight > 1f ? 1f : latchWeight);
            float hung = pull + (1f - pull) * latch;
            if (!taut) return hung;
            if (hung >= 1f) return 1f;
            float span = 1f - TautFloor;
            float t = span > 0.0001f ? (hung - TautFloor) / span : 1f;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return TautRead + (1f - TautRead) * t;
        }

        /// <summary>0 in the air-stride, 1 in the tucked trail. Planar speed is ignored.</summary>
        public static float TuckWeight(float verticalSpeed)
        {
            return Mathf.SmoothStep(0f, 1f, Inv(StrideVy, TuckVy, verticalSpeed));
        }

        public static float TuckAt(float verticalSpeed, float planarSpeed)
        {
            return TuckWeight(verticalSpeed) + planarSpeed * 0f;
        }

        /// <summary>1 on the attach frame, 0 once the snap has led.</summary>
        public static float LatchWeight(float age)
        {
            if (age < 0f) return 0f;
            if (age >= LatchSnapSeconds) return 0f;
            float u = age / LatchSnapSeconds;
            return 1f - u * u;
        }

        public static float AimWeight(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= AimBlendSeconds) return 1f;
            return Ease(age / AimBlendSeconds);
        }

        public static float ReleaseWeight(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= ReleaseBlendSeconds) return 1f;
            return Ease(age / ReleaseBlendSeconds);
        }

        /// <summary>Same fade as the miss stub. The stub still uses this. The body uses <see cref="MissBeat"/>.</summary>
        public static float MissWeight(float age) => GrappleMissTell.Fade(age);

        /// <summary>End of the recoil. The return uses the rest of the miss stub window.</summary>
        public static float MissSnapAge => MissWhipSeconds + MissSnapSeconds;

        /// <summary>
        /// 1 through the throw and the recoil, then eases off with the stub's tail.
        /// A latch stays at 0 because the miss age is cleared.
        /// </summary>
        public static float MissBeat(float age)
        {
            if (age < 0f || age >= GrappleMissTell.FlashSeconds) return 0f;
            if (age <= MissSnapAge) return 1f;
            float span = GrappleMissTell.FlashSeconds - MissSnapAge;
            float u = span > 0.0001f ? (age - MissSnapAge) / span : 1f;
            return 1f - Ease(u);
        }

        public static float ActiveSlew(bool latching, bool aiming, bool miss, bool releasing)
        {
            float s = PoseSlew;
            if (releasing) s = Mathf.Max(s, PoseSlew);
            if (aiming) s = Mathf.Max(s, AimSlew);
            if (miss) s = Mathf.Max(s, MissSlew);
            if (latching) s = Mathf.Max(s, LatchSlew);
            return s;
        }

        /// <summary>Chest yaw toward a planar direction in body space. Positive local X is to the right.</summary>
        public static float LeanYaw(float localX, float localZ)
        {
            float lean = -YawDegrees(localX, localZ);
            if (lean > LeanYawMax) return LeanYawMax;
            if (lean < -LeanYawMax) return -LeanYawMax;
            return lean;
        }

        public static float YawDegrees(float localX, float localZ)
        {
            if (localX * localX + localZ * localZ < 1e-8f) return 0f;
            return (float)Math.Atan2(localX, localZ) * Mathf.Rad2Deg;
        }

        public static float ElevDegrees(float y, float x, float z)
        {
            float planar = Mathf.Sqrt(x * x + z * z);
            if (planar < 1e-5f && Mathf.Abs(y) < 1e-5f) return 0f;
            return (float)Math.Atan2(y, planar) * Mathf.Rad2Deg;
        }

        /// <summary>Outward planar speed away from the latch. Vertical speed is stripped before the dot.</summary>
        public static float OutwardPlanar(Vector3 velocity, Vector3 pawn, Vector3 anchor)
        {
            Vector3 horiz = velocity;
            horiz.y = 0f;
            Vector3 to = anchor - pawn;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0025f) return 0f;
            Vector3 inward = to.normalized;
            float outward = -Vector3.Dot(horiz, inward);
            return outward > 0f ? outward : 0f;
        }

        /// <summary>Same taut test the motor uses. Slack stays inside the latched length.</summary>
        public static bool Taut(Vector3 pawn, Vector3 anchor, float ropeLength, float slack)
        {
            if (ropeLength <= 0.05f) return false;
            float dist = (anchor - pawn).magnitude;
            float limit = ropeLength - (slack > 0f ? slack : 0f);
            if (limit < 0.05f) limit = 0.05f;
            return dist >= limit;
        }

        /// <summary>Short reach. The right hand follows the aim. The elbow stays bent.</summary>
        public static Sample Aim(float aimElevDeg, float aimYawDeg)
        {
            float elev = Clamp(aimElevDeg, -40f, 35f);
            float yaw = Clamp(aimYawDeg, -LeanYawMax, LeanYawMax);
            return new Sample
            {
                ThighL = AimThigh,
                ThighR = AimThigh - 2f,
                KneeL = AimKnee,
                KneeR = AimKnee,
                ArmPitchL = AimPitchL,
                ArmPitchR = AimPitchR - elev * AimElevShare,
                ArmYawL = AimYawL,
                ArmYawR = AimYawR - yaw * AimYawShare,
                ElbowL = AimElbowL,
                ElbowR = AimElbowR,
                Hip = AimHip,
                Spine = AimSpine,
                Head = AimHead,
                HipYaw = 0f,
                SpineYaw = yaw * 0.25f,
                HeadYaw = yaw * 0.35f,
            };
        }

        /// <summary>Lead hand straight, chest into the pull. The off hand has not joined yet.</summary>
        public static Sample Latch(float leanYaw)
        {
            float y = Clamp(leanYaw, -LeanYawMax, LeanYawMax);
            return new Sample
            {
                ThighL = 16f,
                ThighR = 12f,
                KneeL = -20f,
                KneeR = -22f,
                ArmPitchL = LatchPitchL,
                ArmPitchR = LatchPitchR,
                ArmYawL = LatchYawL,
                ArmYawR = LatchYawR,
                ElbowL = LatchElbowL,
                ElbowR = LatchElbowR,
                Hip = LatchHip,
                Spine = LatchSpine,
                Head = LatchHead,
                HipYaw = y * HipYawShare,
                SpineYaw = y,
                HeadYaw = y * HeadYawShare,
            };
        }

        /// <summary>
        /// Both hands on the line. phaseSin +1 puts a small stride on the left leg.
        /// Falling vertical speed tucks the trail. The lean is the planar yaw only.
        /// </summary>
        public static Sample Pull(float phaseSin, float verticalSpeed, float leanYaw)
        {
            float tuck = TuckWeight(verticalSpeed);
            float fwd = (phaseSin + 1f) * 0.5f;
            float thighL = Mathf.Lerp(StrideThighBack, StrideThighFwd, fwd);
            float thighR = Mathf.Lerp(StrideThighFwd, StrideThighBack, fwd);
            float kneeL = Mathf.Lerp(StrideKneeBack, StrideKneeFwd, fwd);
            float kneeR = Mathf.Lerp(StrideKneeFwd, StrideKneeBack, fwd);
            float y = Clamp(leanYaw, -LeanYawMax, LeanYawMax);
            return new Sample
            {
                ThighL = Mathf.Lerp(thighL, TuckThigh + TuckAlt * phaseSin, tuck),
                ThighR = Mathf.Lerp(thighR, TuckThigh - TuckAlt * phaseSin, tuck),
                KneeL = Mathf.Lerp(kneeL, TuckKnee, tuck),
                KneeR = Mathf.Lerp(kneeR, TuckKnee + 4f, tuck),
                ArmPitchL = PullPitchL,
                ArmPitchR = PullPitchR,
                ArmYawL = PullYawL,
                ArmYawR = PullYawR,
                ElbowL = PullElbowL,
                ElbowR = PullElbowR,
                Hip = PullHip,
                Spine = PullSpine,
                Head = PullHead,
                HipYaw = y * HipYawShare,
                SpineYaw = y,
                HeadYaw = y * HeadYawShare,
            };
        }

        public static Sample Latched(float phaseSin, float verticalSpeed, float leanYaw, float latchAge)
        {
            return Lerp(Pull(phaseSin, verticalSpeed, leanYaw), Latch(leanYaw), LatchWeight(latchAge));
        }

        /// <summary>Lead arm straight along the aim. The off arm stays back. The chest follows the throw.</summary>
        public static Sample Whip(float aimElevDeg, float aimYawDeg)
        {
            float elev = Clamp(aimElevDeg, -40f, 35f);
            float yaw = Clamp(aimYawDeg, -LeanYawMax, LeanYawMax);
            return new Sample
            {
                ThighL = AimThigh,
                ThighR = AimThigh - 2f,
                KneeL = AimKnee,
                KneeR = AimKnee,
                ArmPitchL = WhipPitchL,
                ArmPitchR = WhipPitchR - elev * AimElevShare,
                ArmYawL = WhipYawL,
                ArmYawR = WhipYawR - yaw * AimYawShare,
                ElbowL = WhipElbowL,
                ElbowR = WhipElbowR,
                Hip = WhipHip,
                Spine = WhipSpine,
                Head = WhipHead,
                HipYaw = yaw * 0.20f,
                SpineYaw = WhipSpineYaw + yaw * 0.25f,
                HeadYaw = WhipHeadYaw + yaw * 0.20f,
            };
        }

        /// <summary>Lead hand and chest snapped back. Nothing is on the line.</summary>
        public static Sample Flick()
        {
            return new Sample
            {
                ThighL = AimThigh,
                ThighR = AimThigh - 2f,
                KneeL = AimKnee,
                KneeR = AimKnee,
                ArmPitchL = MissPitchL,
                ArmPitchR = MissPitchR,
                ArmYawL = MissYawL,
                ArmYawR = MissYawR,
                ElbowL = MissElbowL,
                ElbowR = MissElbowR,
                Hip = MissHip,
                Spine = MissSpine,
                Head = MissHead,
                HipYaw = MissHipYaw,
                SpineYaw = MissSpineYaw,
                HeadYaw = MissHeadYaw,
            };
        }

        /// <summary>
        /// Throw, then snap back, then ease onto the aim. Age 0 is the throw.
        /// The recoil is in by <see cref="MissSnapAge"/>. The stub window still ends the beat.
        /// A latch does not call this.
        /// </summary>
        public static Sample Miss(float aimElevDeg, float aimYawDeg, float age)
        {
            Sample whip = Whip(aimElevDeg, aimYawDeg);
            Sample back = Flick();
            Sample home = Aim(aimElevDeg, aimYawDeg);
            if (age <= MissWhipSeconds) return whip;
            if (age < MissSnapAge)
            {
                float u = (age - MissWhipSeconds) / MissSnapSeconds;
                return Lerp(whip, back, SnapEase(u));
            }

            float span = GrappleMissTell.FlashSeconds - MissSnapAge;
            float r = span > 0.0001f ? (age - MissSnapAge) / span : 1f;
            if (r < 0f) r = 0f;
            if (r > 1f) r = 1f;
            return Lerp(back, home, Ease(r));
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
                Head = WallPose.ReleaseHead,
                HipYaw = 0f,
                SpineYaw = 0f,
                HeadYaw = 0f,
            };
        }

        public static Sample Release(float phaseSin, float verticalSpeed, float leanYaw, float age)
        {
            return Lerp(Pull(phaseSin, verticalSpeed, leanYaw), Fall(), ReleaseWeight(age));
        }

        public static bool Holds()
        {
            if (RootMotion || VerticalImpulse != 0f || LeadRight) return false;
            if (VerticalImpulse != GrappleRopeTell.VerticalImpulse) return false;
            if (VerticalImpulse != GrappleLatchTell.VerticalImpulse) return false;
            if (VerticalImpulse != GrappleMissTell.VerticalImpulse) return false;
            if (AimBlendSeconds < 0.06f || AimBlendSeconds > 0.12f) return false;
            if (LatchSnapSeconds < 0.05f || LatchSnapSeconds > 0.10f) return false;
            if (ReleaseBlendSeconds < 0.08f || ReleaseBlendSeconds > 0.12f) return false;
            if (PoseSlew < 120f || AimSlew < PoseSlew || MissSlew < 800f || LatchSlew < 1200f) return false;
            if (MissWhipSeconds < 0.05f || MissWhipSeconds > 0.10f) return false;
            if (MissSnapSeconds < 0.07f || MissSnapSeconds > 0.12f) return false;
            if (MissSnapAge >= GrappleMissTell.FlashSeconds - 0.05f) return false;
            if (TautRead < 0.90f || TautRead > 0.99f) return false;
            if (FovPop != 0f) return false;
            if (PlanarFull < 4f || PlanarFull > 12f) return false;
            if (TautFloor < 0.55f || TautFloor > 0.85f) return false;

            if (AimWeight(0f) > 0.0001f || Mathf.Abs(AimWeight(AimBlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(LatchWeight(0f) - 1f) > 0.0001f) return false;
            if (LatchWeight(-0.01f) > 0.0001f || LatchWeight(LatchSnapSeconds) > 0.0001f) return false;
            if (ReleaseWeight(0f) > 0.0001f || Mathf.Abs(ReleaseWeight(ReleaseBlendSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Ease(0.5f) - 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(MissWeight(0f) - 1f) > 0.0001f) return false;
            if (MissWeight(GrappleMissTell.FlashSeconds) > 0.0001f) return false;
            if (Mathf.Abs(MissWeight(0.12f) - GrappleMissTell.Fade(0.12f)) > 0.0001f) return false;

            if (PullBlend(0f, false) > 0.0001f) return false;
            if (Mathf.Abs(PullBlend(0f, true) - TautFloor) > 0.0001f) return false;
            if (Mathf.Abs(PullBlend(PlanarFull, true) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(PullBlend(PlanarFull, false) - 1f) > 0.0001f) return false;
            if (ReadWeight(0f, false, 0f) > 0.0001f) return false;
            if (ReadWeight(0f, true, 0f) < TautRead - 0.001f) return false;
            if (Mathf.Abs(ReadWeight(PlanarFull, true, 0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(ReadWeight(0f, true, 1f) - 1f) > 0.0001f) return false;
            if (ReadWeight(0f, true, 0f) <= TautFloor) return false;
            if (Mathf.Abs(PullBlend(PlanarFull * 0.5f, false) - 0.5f) > 0.02f) return false;
            if (PullBlend(-4f, false) > 0.0001f) return false;
            float prevPull = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float w = PullBlend(PlanarFull * i / 8f, false);
                if (w + 0.0001f < prevPull) return false;
                prevPull = w;
            }

            if (TuckWeight(StrideVy) > 0.0001f || TuckWeight(24.7f) > 0.0001f) return false;
            if (Mathf.Abs(TuckWeight(TuckVy) - 1f) > 0.0001f || TuckWeight(-20f) < 0.999f) return false;
            if (Mathf.Abs(TuckAt(0f, 0f) - TuckAt(0f, 14f)) > 0.0001f) return false;
            if (Mathf.Abs(TuckAt(-6f, 2f) - TuckAt(-6f, 18f)) > 0.0001f) return false;
            float prevTuck = -1f;
            float[] vys = { 16f, 8f, 3f, 0f, -4f, -8f, -10f, -16f };
            for (int i = 0; i < vys.Length; i++)
            {
                float w = TuckWeight(vys[i]);
                if (w + 0.0001f < prevTuck) return false;
                prevTuck = w;
            }

            Vector3 pawn = Vector3.zero;
            Vector3 anchor = new Vector3(0f, 4f, 10f);
            if (Mathf.Abs(OutwardPlanar(new Vector3(0f, 20f, -5f), pawn, anchor) - 5f) > 0.05f) return false;
            if (Mathf.Abs(OutwardPlanar(new Vector3(0f, 0f, -5f), pawn, anchor) - 5f) > 0.05f) return false;
            if (OutwardPlanar(new Vector3(0f, -12f, 5f), pawn, anchor) > 0.0001f) return false;
            if (!Taut(pawn, anchor, 10f, 0.35f)) return false;
            if (Taut(pawn, new Vector3(0f, 0f, 4f), 10f, 0.35f)) return false;

            if (Mathf.Abs(LeanYaw(0f, 1f)) > 0.05f) return false;
            if (LeanYaw(1f, 0f) > -LeanYawMax + 0.05f) return false;
            if (LeanYaw(-1f, 0f) < LeanYawMax - 0.05f) return false;

            Sample aim = Aim(0f, 0f);
            if (aim.ArmPitchR >= aim.ArmPitchL) return false;
            if (aim.ElbowR > -40f) return false;
            if (aim.ArmPitchR - LatchPitchR < 20f) return false;
            Sample aimUp = Aim(30f, 0f);
            Sample aimDown = Aim(-30f, 0f);
            if (aimUp.ArmPitchR >= aim.ArmPitchR || aimDown.ArmPitchR <= aim.ArmPitchR) return false;
            Sample aimRight = Aim(0f, 24f);
            if (aimRight.ArmYawR >= aim.ArmYawR) return false;

            Sample latch = Latch(0f);
            if (latch.ElbowR < -12f || latch.ElbowR <= aim.ElbowR) return false;
            if (latch.ArmPitchR >= AimPitchR) return false;
            if (latch.Spine <= AimSpine + 16f) return false;
            if (latch.ArmPitchL <= latch.ArmPitchR) return false;
            Sample latchRight = Latch(-LeanYawMax);
            if (Mathf.Abs(latchRight.SpineYaw - (-LeanYawMax)) > 0.05f) return false;
            if (latchRight.HipYaw >= 0f || latchRight.HeadYaw >= 0f) return false;

            Sample stride = Pull(1f, 24.7f, 0f);
            Sample tuck = Pull(1f, -16f, 0f);
            if (stride.ArmPitchL > -70f || stride.ArmPitchR > -70f) return false;
            if (stride.ArmPitchL < -100f || stride.ArmPitchR < -100f) return false;
            if (Mathf.Abs(stride.ArmPitchL - tuck.ArmPitchL) > 0.05f) return false;
            if (Mathf.Abs(stride.ArmPitchR - tuck.ArmPitchR) > 0.05f) return false;
            if (stride.ArmPitchL - stride.ArmPitchR > 16f) return false;
            if (stride.ThighL <= stride.ThighR) return false;
            if (tuck.KneeL > -80f || tuck.KneeR > -80f) return false;
            if (tuck.KneeL >= stride.KneeL - 40f) return false;
            if (stride.Spine < LatchSpine || stride.Hip < LatchHip) return false;
            Sample strideOther = Pull(-1f, 24.7f, 0f);
            if (strideOther.ThighR <= strideOther.ThighL) return false;
            Sample leaned = Pull(0f, 0f, LeanYawMax);
            if (Mathf.Abs(leaned.SpineYaw - LeanYawMax) > 0.05f) return false;

            Sample snap = Latched(1f, 24.7f, 0f, 0f);
            if (Mathf.Abs(snap.ArmPitchR - latch.ArmPitchR) > 0.05f) return false;
            if (Mathf.Abs(snap.ElbowR - latch.ElbowR) > 0.05f) return false;
            Sample held = Latched(1f, 24.7f, 0f, LatchSnapSeconds);
            if (Mathf.Abs(held.ArmPitchL - stride.ArmPitchL) > 0.05f) return false;
            if (Mathf.Abs(held.ThighL - stride.ThighL) > 0.05f) return false;

            Sample whip = Miss(0f, 0f, 0f);
            if (whip.ArmPitchR >= aim.ArmPitchR - 30f) return false;
            if (whip.ElbowR <= aim.ElbowR) return false;
            if (whip.Spine <= aim.Spine + 8f) return false;
            if (whip.ArmPitchL <= 0f) return false;
            Sample whipUp = Whip(30f, 0f);
            if (whipUp.ArmPitchR >= whip.ArmPitchR) return false;
            Sample snapped = Miss(0f, 0f, MissSnapAge);
            if (snapped.ArmPitchR < 60f) return false;
            if (snapped.ElbowR > -90f) return false;
            if (snapped.Spine >= -20f || snapped.Spine >= whip.Spine) return false;
            if (snapped.ArmPitchR - whip.ArmPitchR < 150f) return false;
            if (Mathf.Abs(snapped.ArmPitchR - LatchPitchR) < 40f) return false;
            if (Mathf.Abs(snapped.ElbowR - LatchElbowR) < 60f) return false;
            Sample midSnap = Miss(0f, 0f, MissWhipSeconds + MissSnapSeconds * 0.35f);
            if (midSnap.ArmPitchR <= whip.ArmPitchR + 40f) return false;
            Sample flickGone = Miss(0f, 0f, GrappleMissTell.FlashSeconds);
            if (Mathf.Abs(flickGone.ArmPitchR - aim.ArmPitchR) > 0.05f) return false;
            if (Mathf.Abs(flickGone.ElbowR - aim.ElbowR) > 0.05f) return false;
            if (Mathf.Abs(flickGone.Spine - aim.Spine) > 0.05f) return false;
            if (Mathf.Abs(MissBeat(0f) - 1f) > 0.0001f || Mathf.Abs(MissBeat(MissSnapAge) - 1f) > 0.0001f) return false;
            if (MissBeat(-0.01f) > 0.0001f || MissBeat(GrappleMissTell.FlashSeconds) > 0.0001f) return false;
            float prevBeat = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float age = MissSnapAge + (GrappleMissTell.FlashSeconds - MissSnapAge) * (i / 8f);
                float beat = MissBeat(age);
                if (beat > prevBeat + 0.0001f) return false;
                prevBeat = beat;
            }

            Sample fall = Fall();
            if (fall.ThighL != JumpPose.FallThigh || fall.ThighR != JumpPose.FallThigh) return false;
            if (fall.KneeL != JumpPose.FallKnee || fall.KneeR != JumpPose.FallKnee) return false;
            if (fall.ArmPitchL != JumpPose.FallArmPitch || fall.ArmPitchR != JumpPose.FallArmPitch) return false;
            if (fall.ArmYawL != JumpPose.FallArmYaw || fall.ArmYawR != -JumpPose.FallArmYaw) return false;
            if (fall.ElbowL != JumpPose.FallElbow || fall.ElbowR != JumpPose.FallElbow) return false;
            if (fall.Spine != JumpPose.FallSpine || fall.Hip != JumpPose.FallHip) return false;
            if (fall.Head != WallPose.ReleaseHead) return false;

            Sample drop = Release(1f, 24.7f, 0f, ReleaseBlendSeconds);
            if (Mathf.Abs(drop.ArmYawL - fall.ArmYawL) > 0.05f) return false;
            if (Mathf.Abs(drop.ThighL - fall.ThighL) > 0.05f) return false;
            Sample drop0 = Release(1f, 24.7f, 0f, 0f);
            if (Mathf.Abs(drop0.ArmPitchR - stride.ArmPitchR) > 0.05f) return false;
            return true;
        }

        /// <summary>
        /// Miss reads as a throw then a recoil. A taut pull reads as both hands
        /// on one line, past a punch. No latch, no FOV pop, no jet, no root motion.
        /// </summary>
        public static string PolishProofLine()
        {
            Sample whip = Whip(0f, 0f);
            Sample snap = Flick();
            Sample home = Miss(0f, 0f, GrappleMissTell.FlashSeconds);
            Sample pull = Pull(1f, 24.7f, 0f);
            Vector3 whipHand = LeadHand(whip.Hip + whip.Spine, whip.ArmPitchR, whip.ArmYawR, whip.ElbowR);
            Vector3 snapHand = LeadHand(snap.Hip + snap.Spine, snap.ArmPitchR, snap.ArmYawR, snap.ElbowR);
            Vector3 pullR = LeadHand(pull.Hip + pull.Spine, pull.ArmPitchR, pull.ArmYawR, pull.ElbowR);
            Vector3 pullL = OffHand(pull.Hip + pull.Spine, pull.ArmPitchL, pull.ArmYawL, pull.ElbowL);
            return "grapple-pose-polish"
                + " miss=whip-snap-return"
                + " whipS=" + MissWhipSeconds.ToString("0.00")
                + " snapS=" + MissSnapSeconds.ToString("0.00")
                + " whipPitch=" + whip.ArmPitchR.ToString("0")
                + " whipElbow=" + whip.ElbowR.ToString("0")
                + " whipSpine=" + whip.Spine.ToString("0")
                + " whipZ=" + whipHand.z.ToString("0.00")
                + " snapPitch=" + snap.ArmPitchR.ToString("0")
                + " snapElbow=" + snap.ElbowR.ToString("0")
                + " snapSpine=" + snap.Spine.ToString("0")
                + " snapZ=" + snapHand.z.ToString("0.00")
                + " returnPitch=" + home.ArmPitchR.ToString("0")
                + " pull=two-hand-line"
                + " pullPitch=" + pull.ArmPitchR.ToString("0") + "/" + pull.ArmPitchL.ToString("0")
                + " pullChest=" + (pull.Hip + pull.Spine).ToString("0")
                + " pullZ=" + pullR.z.ToString("0.00") + "/" + pullL.z.ToString("0.00")
                + " tautRead=" + ReadWeight(0f, true, 0f).ToString("0.00")
                + " latch=0"
                + " fovPop=0"
                + " jet=0"
                + " impulse=0"
                + " dummy=shared-if-grapple"
                + " rootMotion=0";
        }

        public static bool PolishHolds()
        {
            if (RootMotion || VerticalImpulse != 0f || FovPop != 0f || LeadRight) return false;
            if (GrappleMissTell.VerticalImpulse != 0f || GrappleMissTell.Glow != 0f) return false;
            if (GrappleMissTell.Show(true, true, 0f)) return false;
            if (GrappleLatchTell.Show(false, 0f)) return false;

            Sample aim = Aim(0f, 0f);
            Sample whip = Miss(0f, 0f, 0f);
            Sample snap = Miss(0f, 0f, MissSnapAge);
            Sample back = Miss(0f, 0f, GrappleMissTell.FlashSeconds);
            if (Mathf.Abs(whip.ArmPitchR - Whip(0f, 0f).ArmPitchR) > 0.05f) return false;
            if (Mathf.Abs(snap.ArmPitchR - Flick().ArmPitchR) > 0.05f) return false;
            if (Mathf.Abs(back.ArmPitchR - aim.ArmPitchR) > 0.05f) return false;
            if (whip.ArmPitchR >= -90f || whip.ElbowR < -16f || whip.Spine < 20f) return false;
            if (snap.ArmPitchR < 60f || snap.ElbowR > -96f || snap.Spine > -24f) return false;
            if (whip.ArmPitchL <= 8f || snap.ArmPitchL <= 0f) return false;
            if (snap.SpineYaw <= 0f || whip.SpineYaw >= 0f) return false;
            if (MissBeat(0f) < 0.99f || MissBeat(MissSnapAge) < 0.99f) return false;
            if (MissBeat(GrappleMissTell.FlashSeconds) > 0.001f) return false;

            Vector3 whipHand = LeadHand(whip.Hip + whip.Spine, whip.ArmPitchR, whip.ArmYawR, whip.ElbowR);
            Vector3 snapHand = LeadHand(snap.Hip + snap.Spine, snap.ArmPitchR, snap.ArmYawR, snap.ElbowR);
            if (whipHand.z < snapHand.z + 0.35f) return false;
            if (snapHand.z > 0.15f) return false;

            Sample pull = Pull(0f, 0f, 0f);
            if (pull.ArmPitchL > -70f || pull.ArmPitchR > -70f) return false;
            if (pull.ArmPitchL < -100f || pull.ArmPitchR < -100f) return false;
            if (Mathf.Abs(pull.ArmPitchL - pull.ArmPitchR) > 14f) return false;
            if (pull.ElbowL < -12f || pull.ElbowR < -12f) return false;
            if (pull.Spine + pull.Hip < 80f) return false;
            if (pull.ArmPitchR >= VerbPoseClips.PunchStrikePitch) return false;
            if (pull.Spine + pull.Hip <= VerbPoseClips.PunchHipPitch + VerbPoseClips.PunchSpinePitch + 40f) return false;
            if (snap.ArmPitchR <= VerbPoseClips.PunchRecoverPitch) return false;
            if (Mathf.Abs(whip.ElbowR - VerbPoseClips.PunchCockElbow) < 70f) return false;

            Vector3 pullR = LeadHand(pull.Hip + pull.Spine, pull.ArmPitchR, pull.ArmYawR, pull.ElbowR);
            Vector3 pullL = OffHand(pull.Hip + pull.Spine, pull.ArmPitchL, pull.ArmYawL, pull.ElbowL);
            if (pullR.z < 0.35f || pullL.z < 0.35f) return false;
            if (Mathf.Abs(pullR.z - pullL.z) > 0.22f) return false;
            if (Mathf.Abs(pullR.y - pullL.y) > 0.28f) return false;

            if (ReadWeight(0f, true, 0f) < 0.94f) return false;
            if (ReadWeight(0f, false, 0f) > 0.001f) return false;
            if (Mathf.Abs(ReadWeight(PlanarFull, false, 0f) - PullBlend(PlanarFull, false)) > 0.001f) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            Tag.Gameplay.PunchTagTuning tuning = ScriptableObject.CreateInstance<Tag.Gameplay.PunchTagTuning>();
            if (cfg.enableJet || cfg.slideBoost != 0f) return false;
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f) return false;
            if (Mathf.Abs(tuning.reach - 1.55f) > 0.001f) return false;
            if (DummyPosePaths.Allows(SoloGrappleGate.OpponentPawnName, DummyPosePaths.Grapple)) return false;
            if (!DummyPosePaths.Allows(SoloGrappleGate.SoloPawnName, DummyPosePaths.Grapple)) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample aim = Aim(0f, 0f);
            Sample latch = Latch(0f);
            Sample stride = Pull(1f, 24.7f, 0f);
            Sample tuck = Pull(1f, -16f, 0f);
            Sample flick = Miss(0f, 0f, MissSnapAge);
            Sample fall = Fall();
            return "grapple pose"
                + " aimPitch=" + AimPitchR.ToString("0") + "/" + AimPitchL.ToString("0")
                + " aimYaw=" + AimYawR.ToString("0") + "/" + AimYawL.ToString("0")
                + " aimElbow=" + AimElbowR.ToString("0") + "/" + AimElbowL.ToString("0")
                + " aimSpine=" + AimSpine.ToString("0")
                + " aimHip=" + AimHip.ToString("0")
                + " aimHead=" + AimHead.ToString("0")
                + " latchPitch=" + LatchPitchR.ToString("0") + "/" + LatchPitchL.ToString("0")
                + " latchYaw=" + LatchYawR.ToString("0") + "/" + LatchYawL.ToString("0")
                + " latchElbow=" + LatchElbowR.ToString("0") + "/" + LatchElbowL.ToString("0")
                + " latchSpine=" + LatchSpine.ToString("0")
                + " latchHip=" + LatchHip.ToString("0")
                + " latchHead=" + LatchHead.ToString("0")
                + " pullPitch=" + PullPitchR.ToString("0") + "/" + PullPitchL.ToString("0")
                + " pullYaw=" + PullYawR.ToString("0") + "/" + PullYawL.ToString("0")
                + " pullElbow=" + PullElbowR.ToString("0") + "/" + PullElbowL.ToString("0")
                + " pullSpine=" + PullSpine.ToString("0")
                + " pullHip=" + PullHip.ToString("0")
                + " pullHead=" + PullHead.ToString("0")
                + " strideThigh=" + StrideThighFwd.ToString("0") + "/" + StrideThighBack.ToString("0")
                + " strideKnee=" + StrideKneeFwd.ToString("0") + "/" + StrideKneeBack.ToString("0")
                + " tuckThigh=" + TuckThigh.ToString("0")
                + " tuckKnee=" + TuckKnee.ToString("0")
                + " tuckAlt=" + TuckAlt.ToString("0")
                + " missPitch=" + MissPitchR.ToString("0")
                + " missYaw=" + MissYawR.ToString("0")
                + " missElbow=" + MissElbowR.ToString("0")
                + " missSpine=" + MissSpine.ToString("0")
                + " fallThigh=" + fall.ThighL.ToString("0")
                + " fallYaw=" + fall.ArmYawL.ToString("0")
                + " aimRead=" + aim.ArmPitchR.ToString("0")
                + " latchRead=" + latch.ArmPitchR.ToString("0")
                + " pullRead=" + stride.ArmPitchL.ToString("0")
                + " strideRead=" + stride.ThighL.ToString("0")
                + " tuckRead=" + tuck.KneeL.ToString("0")
                + " flickRead=" + flick.ArmPitchR.ToString("0")
                + " tautFloor=" + PullBlend(0f, true).ToString("0.00")
                + " pullFull=" + PullBlend(PlanarFull, true).ToString("0.00")
                + " tuckRise=" + TuckWeight(24.7f).ToString("0.00")
                + " tuckFall=" + TuckWeight(-16f).ToString("0.00")
                + " gate=aim:Ease(t/" + AimBlendSeconds.ToString("0.00") + ") right hand along aim, elbow bent, GrappleRopeTell.AimSpan unchanged"
                + "; latch:weight 1-(t/" + LatchSnapSeconds.ToString("0.00") + ")^2 lead hand straight to the knot, spine yaw LeanYaw clamp " + LeanYawMax.ToString("0")
                + "; pull:PullBlend outward/" + PlanarFull.ToString("0") + " SmoothStep, taut floor " + TautFloor.ToString("0.00") + ", ignores vertical speed"
                + "; legs:TuckWeight SmoothStep InverseLerp(vy " + StrideVy.ToString("0") + ".." + TuckVy.ToString("0") + ") air-stride or tucked trail, ignores planar speed"
                + "; release:Ease(t/" + ReleaseBlendSeconds.ToString("0.00") + ") onto JumpPose fall, airborne, not a jump-from-grapple"
                + "; miss:whip " + MissWhipSeconds.ToString("0.00") + "s then snap " + MissSnapSeconds.ToString("0.00") + "s then return, stub Fade " + GrappleMissTell.FlashSeconds.ToString("0.00") + " unchanged, latches nothing"
                + "; slew aim " + AimSlew.ToString("0") + " latch " + LatchSlew.ToString("0") + " pull " + PoseSlew.ToString("0") + " miss " + MissSlew.ToString("0")
                + "; verticalImpulse=0 rootMotion=0";
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
                Head = Mathf.Lerp(a.Head, b.Head, t),
                HipYaw = Mathf.Lerp(a.HipYaw, b.HipYaw, t),
                SpineYaw = Mathf.Lerp(a.SpineYaw, b.SpineYaw, t),
                HeadYaw = Mathf.Lerp(a.HeadYaw, b.HeadYaw, t),
            };
        }

        static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>Steep early, so the recoil reads as a snap.</summary>
        static float SnapEase(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            float s = 1f - u;
            return 1f - s * s * s;
        }

        static Vector3 LeadHand(float chestPitch, float pitch, float yaw, float elbow)
        {
            return PoseHand(1f, chestPitch, pitch, yaw, elbow);
        }

        static Vector3 OffHand(float chestPitch, float pitch, float yaw, float elbow)
        {
            return PoseHand(-1f, chestPitch, pitch, -yaw, elbow);
        }

        static Vector3 PoseHand(float sx, float chestPitch, float pitch, float yawOut, float elbow)
        {
            const float hipY = 1.05f;
            const float upper = 0.37f;
            const float lower = 0.33f;
            const float shX = 0.235f;
            const float shZ = -0.06f;
            Vector3 up = Rx(new Vector3(0f, 1f, 0f), chestPitch);
            Vector3 basis = new Vector3(0f, hipY, 0f) + up * (1.40f - hipY);
            Vector3 shoulder = basis + Rx(new Vector3(sx * shX, 0f, shZ), chestPitch);
            float outA = 24f * Mathf.Deg2Rad;
            float fwdA = 10f * Mathf.Deg2Rad;
            Vector3 rest = new Vector3(sx * Mathf.Sin(outA), -Mathf.Cos(outA), Mathf.Sin(fwdA));
            Vector3 dir = Ry(Rx(rest, pitch), yawOut * sx).normalized;
            Vector3 elbowP = shoulder + dir * upper;
            return elbowP + Rx(dir, elbow).normalized * lower;
        }

        static Vector3 Rx(Vector3 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            return new Vector3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
        }

        static Vector3 Ry(Vector3 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        static float Clamp(float v, float lo, float hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        static float Cap01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
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
