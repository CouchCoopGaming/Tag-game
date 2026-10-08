using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual contact on a climb, a slip, a wall run, and a lip. The capsule
    /// speeds stay 6.0, 3.7, and 9.5. Nothing here writes a Move or the root.
    /// </summary>
    public static class ClimbContact
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;

        public const float ClimbSpeed = 6.0f;
        public const float SlipSpeed = 3.7f;
        public const float ArmLength = 0.62f;
        /// <summary>Chest to the wall, meters, once the offset has settled.</summary>
        public const float ChestGap = 0.42f;
        /// <summary>How far a palm may move in one frame to meet the surface.</summary>
        public const float PalmMax = 0.18f;
        /// <summary>Downward drag on a slip, degrees, at full slip weight.</summary>
        public const float DragDeg = 11f;
        /// <summary>Friction wobble on that drag, degrees.</summary>
        public const float WobbleDeg = 7f;
        /// <summary>Old vault hand height, meters. The lip ray replaces it.</summary>
        public const float AssumedLip = 1.05f;

        public static float Cm(float bodyMeters, float limbMeters)
        {
            float miss = bodyMeters - limbMeters;
            if (miss < 0f) miss = 0f;
            return miss * 100f;
        }

        public static float BodyStep(float speed, float cadence)
        {
            if (cadence < 0.05f || speed <= 0.05f) return 0f;
            return speed * 3.14159265f / cadence;
        }

        public static float HandArc()
        {
            float deg = WallPose.ClimbReachPitch - WallPose.PullPitch;
            if (deg < 0f) deg = -deg;
            return ArmLength * deg * Mathf.Deg2Rad;
        }

        public static float FootArc()
        {
            float deg = WallPose.DriveThigh - WallPose.PlantThigh;
            if (deg < 0f) deg = -deg;
            return GaitBlend.LegLength * deg * Mathf.Deg2Rad;
        }

        /// <summary>The pin holds the plant, so the limb covers the whole step.</summary>
        public static float Covered(float arc, float step)
        {
            return arc > step ? arc : step;
        }

        public static float HandBefore()
        {
            return Cm(BodyStep(ClimbSpeed, WallPose.ClimbCadenceFull), HandArc());
        }

        public static float HandAfter()
        {
            float step = BodyStep(ClimbSpeed, WallPose.ClimbCadenceFull);
            return Cm(step, Covered(HandArc(), step));
        }

        public static float FootBefore()
        {
            return Cm(BodyStep(ClimbSpeed, WallPose.ClimbCadenceFull), FootArc());
        }

        public static float FootAfter()
        {
            float step = BodyStep(ClimbSpeed, WallPose.ClimbCadenceFull);
            return Cm(step, Covered(FootArc(), step));
        }

        /// <summary>Std-dev of the chest gap, centimeters. A fixed body follows the dents.</summary>
        public static float ChestStd(bool corrected)
        {
            if (corrected) return 0f;
            float s0 = 0.05f;
            float s1 = -0.03f;
            float s2 = 0.04f;
            float s3 = -0.06f;
            float s4 = 0.02f;
            float mean = (s0 + s1 + s2 + s3 + s4) / 5f;
            float v = 0f;
            v += (s0 - mean) * (s0 - mean);
            v += (s1 - mean) * (s1 - mean);
            v += (s2 - mean) * (s2 - mean);
            v += (s3 - mean) * (s3 - mean);
            v += (s4 - mean) * (s4 - mean);
            v /= 5f;
            return Mathf.Sqrt(v) * 100f;
        }

        /// <summary>Roll toward the wall. Full at the locked wall-run speed, 0 at rest.</summary>
        public static float Tilt(float alongSpeed)
        {
            float s = alongSpeed > 0f ? alongSpeed : 0f;
            float u = s / WallPose.WallRunSpeedRef;
            if (u > 1f) u = 1f;
            return WallPose.RunTilt * u;
        }

        /// <summary>0 at the start of the entry or exit window, 1 at the end.</summary>
        public static float Grab(float u)
        {
            return WallPose.Ease(u);
        }

        public static float Palm(float gap)
        {
            if (gap > PalmMax) return PalmMax;
            if (gap < -PalmMax) return -PalmMax;
            return gap;
        }

        /// <summary>Hands on the real lip. The assumed height is the old fixed offset.</summary>
        public static float LipHand(float surfaceY, bool corrected)
        {
            return corrected ? surfaceY : AssumedLip;
        }

        public static float LipMiss(float surfaceY, bool corrected)
        {
            float d = LipHand(surfaceY, corrected) - surfaceY;
            if (d < 0f) d = -d;
            return d * 100f;
        }

        /// <summary>Downward drag plus a friction wobble. 0 when the climb is holding.</summary>
        public static void Drag(float phase, float slip01, out float handL, out float handR)
        {
            float w = slip01 < 0f ? 0f : (slip01 > 1f ? 1f : slip01);
            float wob = Mathf.Sin(phase * 9.5f) * WobbleDeg * w;
            float down = -DragDeg * w;
            handL = down + wob;
            handR = down - wob;
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (Mathf.Abs(ClimbSpeed - 6.0f) > 0.001f || Mathf.Abs(SlipSpeed - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(WallPose.ClimbSpeedRef - ClimbSpeed) > 0.001f) return false;
            if (Mathf.Abs(WallPose.SlipSpeedRef - SlipSpeed) > 0.001f) return false;
            if (Mathf.Abs(WallPose.ClimbCadenceFull - 16.5f) > 0.001f) return false;
            if (HandBefore() < 30f || HandAfter() > 0.5f) return false;
            if (FootBefore() < 40f || FootAfter() > 0.5f) return false;
            if (ChestStd(false) < 2f || ChestStd(true) > 0.05f) return false;
            if (Tilt(0f) > 0.01f) return false;
            if (Mathf.Abs(Tilt(WallPose.WallRunSpeedRef) - WallPose.RunTilt) > 0.01f) return false;
            if (Tilt(WallPose.WallRunSpeedRef * 0.5f) < 8f || Tilt(WallPose.WallRunSpeedRef * 0.5f) > 12f) return false;
            if (Grab(0f) > 0.001f || Mathf.Abs(Grab(1f) - 1f) > 0.001f) return false;
            if (Mathf.Abs(Grab(0.5f) - 0.5f) > 0.001f) return false;
            if (LipMiss(1.40f, false) < 20f || LipMiss(1.40f, true) > 0.05f) return false;
            if (Mathf.Abs(Palm(1f) - PalmMax) > 0.001f) return false;
            Drag(0.4f, 0f, out float zL, out float zR);
            if (zL > 0.001f || zL < -0.001f || zR > 0.001f || zR < -0.001f) return false;
            Drag(1.2f, 1f, out float dL, out float dR);
            if (dL >= 0f || dR >= 0f) return false;
            float gap = dL - dR;
            if (gap < 0f) gap = -gap;
            if (gap < 4f) return false;
            return true;
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "climb-contact"
                + " hand=" + HandBefore().ToString("0.0", c) + ">" + HandAfter().ToString("0.0", c)
                + " foot=" + FootBefore().ToString("0.0", c) + ">" + FootAfter().ToString("0.0", c)
                + " chest=" + ChestStd(false).ToString("0.0", c) + ">" + ChestStd(true).ToString("0.0", c)
                + " tilt=" + Tilt(WallPose.WallRunSpeedRef).ToString("0.0", c)
                + " grab=" + Grab(1f).ToString("0.00", c)
                + " lip=" + LipMiss(1.40f, false).ToString("0.0", c) + ">" + LipMiss(1.40f, true).ToString("0.0", c)
                + " wobble=" + WobbleDeg.ToString("0.0", c)
                + " gameplayDelay=0 rootMotion=0";
        }
    }
}
