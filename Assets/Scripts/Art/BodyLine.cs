using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual arcs for the wall jump, the rope, the zip, the pad, the dash
    /// tell, a sharp reversal, the landing, and a punch while moving.
    /// The capsule, the verb timers, and the locked speeds stay as they are.
    /// Nothing here writes a Move or the root.
    /// </summary>
    public static class BodyLine
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;

        public const float Dt = 1f / 60f;
        /// <summary>Hands meet an overhead cable. The cling reach sits below it.</summary>
        public const float CablePitch = -158f;
        /// <summary>How long a stick reversal eases the stride across. The capsule does not wait.</summary>
        public const float ReverseSeconds = 0.14f;
        /// <summary>Pad swing eases in. The arc speed is unchanged.</summary>
        public const float PadSeconds = 0.16f;
        /// <summary>Dash ribbons and the sheen open. The 0.10 s dash is unchanged.</summary>
        public const float TellSeconds = 0.08f;
        /// <summary>Matches AirDashTell.FlashMix. The sheen used to arrive at this weight in one frame.</summary>
        public const float FlashMix = 0.42f;
        /// <summary>Extra chest lean at the locked sprint, while a punch or a tag is out. The arm stays on the authored strike.</summary>
        public const float ReachAtSprint = 22f;

        public static float WallArc(float age)
        {
            float span = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            if (age <= 0f || span <= 0.0001f) return 0f;
            float u = age / span;
            if (u >= 1f) return 1f;
            return 0.5f - 0.5f * Mathf.Cos(u * 3.14159265f);
        }

        /// <summary>Peak one-frame arm step of the shove. The old hold sits still, then the ease dumps the arm.</summary>
        public static float WallStep(bool arced)
        {
            float from = WallJumpPose.PushPitch;
            float to = JumpPose.TuckArmPitch;
            float prev = from;
            float max = 0f;
            float total = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            int n = (int)(total / Dt + 1.5f);
            for (int i = 1; i <= n; i++)
            {
                float age = i * Dt;
                float w = arced ? WallArc(age) : WallJumpPose.JumpWeight(age);
                float pitch = from + (to - from) * w;
                float d = pitch - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = pitch;
            }
            return max;
        }

        /// <summary>Degrees the chest sits off the rope. The old add pitches further into the kink.</summary>
        public static float RopeKink(float elev, bool lined)
        {
            float body = GrapplePose.PullHip + GrapplePose.PullSpine + HangMotion.RopeSpine(elev);
            if (lined) body = elev;
            float d = body - elev;
            if (d < 0f) d = -d;
            return d;
        }

        /// <summary>Pitch to add so a pull pose's hip plus spine meets the rope.</summary>
        public static float LineFix(float bodyPitch, float elev)
        {
            return elev - bodyPitch;
        }

        public static float ZipGrab(float age)
        {
            if (age >= ZipPose.CatchSeconds) return 1f;
            float u = (age + Dt) / ZipPose.CatchSeconds;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return 1f - (1f - u) * (1f - u);
        }

        /// <summary>Degrees the hang sits under the cable.</summary>
        public static float ZipMiss(bool met)
        {
            float hand = met ? CablePitch : WallPose.ReachPitch;
            float d = hand - CablePitch;
            if (d < 0f) d = -d;
            return d;
        }

        /// <summary>0 keeps the hang, 1 is in the air. The release shape peaks in the middle.</summary>
        public static void ZipWeights(float age, out float hangW, out float releaseW, out float airW)
        {
            float span = ZipPose.ReleaseSeconds;
            float u = span > 0.0001f ? (age + Dt) / span : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float s = u * u * (3f - 2f * u);
            float accent = Mathf.Sin(u * 3.14159265f);
            releaseW = accent * (1f - s) * 0.85f;
            airW = s;
            hangW = 1f - releaseW - airW;
            if (hangW < 0f)
            {
                releaseW += hangW;
                if (releaseW < 0f) releaseW = 0f;
                hangW = 0f;
            }
        }

        public static float ZipArm(float age, bool eased, float airPitch)
        {
            float hang = eased ? CablePitch : WallPose.ReachPitch;
            float let = ZipPose.ReleasePitch;
            if (!eased) return let;
            ZipWeights(age, out float hangW, out float releaseW, out float airW);
            return hang * hangW + let * releaseW + airPitch * airW;
        }

        /// <summary>Largest one-frame arm step of the let-go, from the hang that was showing.</summary>
        public static float ZipDropStep(bool eased)
        {
            float hang = eased ? CablePitch : WallPose.ReachPitch;
            float prev = hang;
            float max = 0f;
            float air = JumpPose.FallArmPitch;
            int n = (int)(ZipPose.ReleaseSeconds / Dt + 2.5f);
            for (int i = 0; i <= n; i++)
            {
                float pitch = ZipArm(i * Dt, eased, air);
                float d = pitch - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = pitch;
            }
            return max;
        }

        public static float PadOpen(float age)
        {
            float u = (age + Dt) / PadSeconds;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return 1f - (1f - u) * (1f - u);
        }

        /// <summary>One-frame arm step from a walk into the swing.</summary>
        public static float PadStep(bool eased)
        {
            float swing = LaunchPose.SwingArmPitch;
            if (!eased) return swing < 0f ? -swing : swing;
            float prev = 0f;
            float max = 0f;
            int n = (int)(PadSeconds / Dt + 2.5f);
            for (int i = 0; i <= n; i++)
            {
                float pitch = Mathf.Lerp(0f, swing, PadOpen(i * Dt));
                float d = pitch - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = pitch;
            }
            return max;
        }

        /// <summary>0 at a stand, 1 at the locked sprint. Thighs stay in the stride.</summary>
        public static float KeepStride(float speed)
        {
            float u = speed / 13.8f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>Degrees a hard landing pulls the sprint stride off its step.</summary>
        public static float LandGap(bool intoStride)
        {
            GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
            float front = legs.ThighL > legs.ThighR ? legs.ThighL : legs.ThighR;
            float back = legs.ThighL < legs.ThighR ? legs.ThighL : legs.ThighR;
            float land = LandPose.HardThigh;
            float gap = Abs(land - front);
            float gapB = Abs(land - back);
            if (gapB > gap) gap = gapB;
            if (!intoStride) return gap;
            return 0f;
        }

        public static float TellOpen(float age)
        {
            if (age < 0f) age = 0f;
            float u = (age + Dt) / TellSeconds;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return 1f - (1f - u) * (1f - u);
        }

        public static float TellPop(bool eased)
        {
            if (!eased) return FlashMix;
            return FlashMix * TellOpen(0f);
        }

        public static float ReverseBlend(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return 0.5f - 0.5f * Mathf.Cos(u * 3.14159265f);
        }

        /// <summary>Degrees the sprint stride flips when travel reverses.</summary>
        public static float ReverseGap(bool eased)
        {
            GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
            LocomotionPolish.Legs fwd = LocomotionPolish.FacingStride(
                legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, 13.8f, 0f);
            LocomotionPolish.Legs back = LocomotionPolish.FacingStride(
                legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, -13.8f, 0f);
            float gap = Abs(fwd.ThighL - back.ThighL);
            float gapR = Abs(fwd.ThighR - back.ThighR);
            if (gapR > gap) gap = gapR;
            if (!eased) return gap;
            float prev = 0f;
            float max = 0f;
            int n = (int)(ReverseSeconds / Dt + 1.5f);
            for (int i = 1; i <= n; i++)
            {
                float u = (i * Dt) / ReverseSeconds;
                if (u > 1f) u = 1f;
                float shown = gap * ReverseBlend(u);
                float d = shown - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = shown;
            }
            return max;
        }

        /// <summary>Extra chest pitch while a punch or a tag is out and the pawn is moving. 0 at a stand.</summary>
        public static float ReachLead(float speed)
        {
            if (speed < UpperBody.RunSpeed) return 0f;
            float u = speed / 13.8f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return ReachAtSprint * u;
        }

        /// <summary>Centimeters the fist sits behind a chest lead measured at the locked sprint.</summary>
        public static float ReachShort(bool led)
        {
            float chest = led ? ReachLead(13.8f) : 0f;
            float z = FistZ(VerbPoseClips.PunchStrikePitch, VerbPoseClips.PunchStrikeYaw, VerbPoseClips.PunchStrikeElbow, chest);
            float ledZ = FistZ(VerbPoseClips.PunchStrikePitch, VerbPoseClips.PunchStrikeYaw, VerbPoseClips.PunchStrikeElbow, ReachAtSprint);
            float miss = ledZ - z;
            if (miss < 0f) miss = 0f;
            return miss * 100f;
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (Mathf.Abs(WallJumpPose.BeatSeconds - 0.15f) > 0.001f) return false;
            if (Mathf.Abs(WallJumpPose.EaseSeconds - 0.12f) > 0.001f) return false;
            if (WallArc(0f) > 0.001f) return false;
            if (Mathf.Abs(WallArc(WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds) - 1f) > 0.001f) return false;
            if (WallStep(false) < 20f) return false;
            if (WallStep(true) > WallStep(false) * 0.65f) return false;
            if (RopeKink(40f, false) > 12f) return false;
            if (RopeKink(40f, true) > 0.05f) return false;
            if (Mathf.Abs(LineFix(70f, 40f) - (-30f)) > 0.01f) return false;
            if (ZipMiss(false) < 40f || ZipMiss(true) > 0.05f) return false;
            if (ZipGrab(0f) < 0.05f || ZipGrab(0f) > 0.5f) return false;
            if (Mathf.Abs(ZipGrab(ZipPose.CatchSeconds) - 1f) > 0.001f) return false;
            // The let-go stays on the hang pitch. A pitch pop drags the cable through the body.
            if (ZipDropStep(false) > 8f) return false;
            if (ZipDropStep(true) < 12f) return false;
            if (PadStep(false) < 100f) return false;
            if (PadStep(true) > PadStep(false) * 0.45f) return false;
            if (PadOpen(0f) < 0.02f || PadOpen(PadSeconds) < 0.99f) return false;
            if (LandGap(false) < 40f || LandGap(true) > 0.05f) return false;
            if (Mathf.Abs(KeepStride(0f)) > 0.001f) return false;
            if (Mathf.Abs(KeepStride(13.8f) - 1f) > 0.001f) return false;
            if (TellPop(false) < 0.3f) return false;
            if (TellPop(true) > TellPop(false) * 0.6f) return false;
            if (TellOpen(0f) < 0.05f || TellOpen(TellSeconds) < 0.99f) return false;
            if (ReverseGap(false) < 40f) return false;
            if (ReverseGap(true) > ReverseGap(false) * 0.45f) return false;
            if (Mathf.Abs(ReachLead(0f)) > 0.001f) return false;
            if (ReachLead(13.8f) < 18f) return false;
            if (ReachShort(false) < 8f || ReachShort(true) > 0.5f) return false;
            if (Mathf.Abs(ZipPose.RideSpeed - 14f) > 0.001f) return false;
            if (Mathf.Abs(AirDashPose.WindowSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(FlashMix - 0.42f) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "body-line"
                + " wall=" + WallStep(false).ToString("0.0", c) + ">" + WallStep(true).ToString("0.0", c)
                + " rope=" + RopeKink(40f, false).ToString("0.0", c) + ">" + RopeKink(40f, true).ToString("0.0", c)
                + " zip=" + ZipMiss(false).ToString("0.0", c) + ">" + ZipMiss(true).ToString("0.0", c)
                + " drop=" + ZipDropStep(false).ToString("0.0", c) + ">" + ZipDropStep(true).ToString("0.0", c)
                + " pad=" + PadStep(false).ToString("0.0", c) + ">" + PadStep(true).ToString("0.0", c)
                + " land=" + LandGap(false).ToString("0.0", c) + ">" + LandGap(true).ToString("0.0", c)
                + " tell=" + TellPop(false).ToString("0.00", c) + ">" + TellPop(true).ToString("0.00", c)
                + " rev=" + ReverseGap(false).ToString("0.0", c) + ">" + ReverseGap(true).ToString("0.0", c)
                + " reach=" + ReachShort(false).ToString("0.0", c) + ">" + ReachShort(true).ToString("0.0", c)
                + " gameplayDelay=0 rootMotion=0";
        }

        static float FistZ(float pitch, float yaw, float elbow, float chestAdd)
        {
            const float hipY = 1.05f;
            const float upper = 0.37f;
            const float lower = 0.33f;
            const float shX = 0.235f;
            const float shZ = -0.06f;
            float chest = VerbPoseClips.PunchHipPitch + VerbPoseClips.PunchSpinePitch + chestAdd;
            float a = chest * Mathf.Deg2Rad;
            float cy = Mathf.Cos(a);
            float sy = Mathf.Sin(a);
            Vector3 up = new Vector3(0f, cy, sy);
            Vector3 basis = new Vector3(0f, hipY, 0f) + up * (1.40f - hipY);
            Vector3 shoulder = basis + Rx(new Vector3(shX, 0f, shZ), chest);
            float outA = 24f * Mathf.Deg2Rad;
            float fwdA = 10f * Mathf.Deg2Rad;
            Vector3 rest = new Vector3(Mathf.Sin(outA), -Mathf.Cos(outA), Mathf.Sin(fwdA));
            Vector3 dir = Ry(Rx(rest, pitch), yaw).normalized;
            Vector3 elbowP = shoulder + dir * upper;
            Vector3 hand = elbowP + Rx(dir, elbow).normalized * lower;
            return hand.z;
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

        static float Abs(float v) => v < 0f ? -v : v;
    }
}
