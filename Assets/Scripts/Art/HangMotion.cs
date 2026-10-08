using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Secondary motion on hangs that already play. Zip sway and the leg trail
    /// scale with ride speed 14. A grapple pull orients the chest along the rope
    /// and trails the legs. A pad arc windmills the arms on the way up and
    /// settles on the way down. Speeds and the authored poses stay as they are.
    /// </summary>
    public static class HangMotion
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;

        public const float ZipSpeed = 14f;
        public const float TrailAtSpeed = -22f;
        public const float SwayAdd = 6f;
        public const float SwayRate = 2.4f;
        public const float RopeShare = 0.45f;
        public const float RopeTrail = -16f;
        public const float WindmillAmp = 28f;
        public const float WindmillRate = 9f;
        public const float RiseVy = 24.7f;

        public static float Speed01(float speed)
        {
            float u = ZipSpeed > 0.001f ? speed / ZipSpeed : 0f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u;
        }

        /// <summary>Extra thigh pitch at the given ride speed. 0 when stopped. Negative trails.</summary>
        public static float LegTrail(float speed)
        {
            return TrailAtSpeed * Speed01(speed);
        }

        /// <summary>Extra roll on top of the authored zip sway. 0 at speed 0.</summary>
        public static float SwayExtra(float time, float speed)
        {
            return (float)System.Math.Sin(time * SwayRate) * SwayAdd * Speed01(speed);
        }

        /// <summary>Spine pitch along the rope elevation. 0 when the rope is level.</summary>
        public static float RopeSpine(float elevDegrees)
        {
            float e = elevDegrees;
            if (e > 80f) e = 80f;
            if (e < -80f) e = -80f;
            return e * RopeShare;
        }

        /// <summary>Thigh trail while the pull carries planar speed.</summary>
        public static float RopeLeg(float planarSpeed)
        {
            return RopeTrail * Speed01(planarSpeed);
        }

        /// <summary>Arm pitch added on the rise. 0 once vertical speed is no longer upward.</summary>
        public static float Windmill(float time, float verticalSpeed)
        {
            if (verticalSpeed <= 0f) return 0f;
            float rise = RiseVy > 0.001f ? verticalSpeed / RiseVy : 0f;
            if (rise > 1f) rise = 1f;
            return (float)System.Math.Sin(time * WindmillRate) * WindmillAmp * rise;
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (System.Math.Abs(ZipSpeed - 14f) > 0.001f) return false;
            if (System.Math.Abs(LegTrail(0f)) > 0.001f) return false;
            if (LegTrail(ZipSpeed) > -18f) return false;
            if (System.Math.Abs(SwayExtra(0.2f, 0f)) > 0.001f) return false;
            float peakT = 1.5707963f / SwayRate;
            float sway = SwayExtra(peakT, ZipSpeed);
            if (sway < 5.5f || sway > 6.5f) return false;
            if (System.Math.Abs(RopeSpine(0f)) > 0.001f) return false;
            if (RopeSpine(40f) < 12f) return false;
            if (System.Math.Abs(RopeLeg(0f)) > 0.001f) return false;
            if (RopeLeg(ZipSpeed) > -12f) return false;
            if (System.Math.Abs(Windmill(peakT, -16f)) > 0.001f) return false;
            if (System.Math.Abs(Windmill(0.2f, 0f)) > 0.001f) return false;
            float mill = Windmill(1.5707963f / WindmillRate, RiseVy);
            if (mill < 24f) return false;
            return true;
        }

        public static string ProofLine()
        {
            float peakT = 1.5707963f / SwayRate;
            float millT = 1.5707963f / WindmillRate;
            return "hang-motion"
                + " trail=" + LegTrail(ZipSpeed).ToString("0.0")
                + " sway=" + SwayExtra(peakT, ZipSpeed).ToString("0.0")
                + " rope=" + RopeSpine(40f).ToString("0.0")
                + " ropeLeg=" + RopeLeg(ZipSpeed).ToString("0.0")
                + " mill=" + Windmill(millT, RiseVy).ToString("0.0")
                + " settle=" + Windmill(millT, -16f).ToString("0.0")
                + " gameplayDelay=0 rootMotion=0";
        }
    }
}
