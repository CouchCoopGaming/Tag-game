using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Upper body on top of a live leg cycle. A punch, a lunge tell, and a
    /// grapple aim keep the stride, the slide, the air legs, or the wall run.
    /// The spine yaws toward the aim. Nothing here writes the capsule.
    /// </summary>
    public static class UpperBody
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;

        /// <summary>Planar speed that still counts as a run. A stand is below this.</summary>
        public const float RunSpeed = 1.2f;

        /// <summary>Spine yaw cap. Authored aim, tell, and punch yaws sit inside it.</summary>
        public const float AimYawMax = 64f;

        /// <summary>
        /// True while the legs already have a cycle: run, slide, air, or wall run.
        /// A stand returns false so an idle punch can still pose the whole body.
        /// </summary>
        public static bool KeepLegs(bool sliding, bool airborne, bool wallRun, float planarSpeed)
        {
            if (sliding || airborne || wallRun) return true;
            float s = planarSpeed < 0f ? 0f : planarSpeed;
            return s >= RunSpeed;
        }

        /// <summary>Spine yaw toward the aim. Values already inside the cap pass through.</summary>
        public static float AimTwist(float spineYaw)
        {
            if (spineYaw > AimYawMax) return AimYawMax;
            if (spineYaw < -AimYawMax) return -AimYawMax;
            return spineYaw;
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (!KeepLegs(true, false, false, 0f)) return false;
            if (!KeepLegs(false, true, false, 0f)) return false;
            if (!KeepLegs(false, false, true, 0f)) return false;
            if (!KeepLegs(false, false, false, 13.8f)) return false;
            if (KeepLegs(false, false, false, 0f)) return false;
            if (System.Math.Abs(AimTwist(0f)) > 0.001f) return false;
            if (System.Math.Abs(AimTwist(36f) - 36f) > 0.001f) return false;
            if (System.Math.Abs(AimTwist(-36f) - (-36f)) > 0.001f) return false;
            if (System.Math.Abs(AimTwist(52f) - 52f) > 0.001f) return false;
            if (System.Math.Abs(AimTwist(90f) - AimYawMax) > 0.001f) return false;
            if (System.Math.Abs(AimTwist(-90f) - (-AimYawMax)) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            return "upper-body"
                + " run=" + (KeepLegs(false, false, false, 13.8f) ? "1" : "0")
                + " slide=" + (KeepLegs(true, false, false, 0f) ? "1" : "0")
                + " air=" + (KeepLegs(false, true, false, 0f) ? "1" : "0")
                + " wall=" + (KeepLegs(false, false, true, 0f) ? "1" : "0")
                + " idle=" + (KeepLegs(false, false, false, 0f) ? "1" : "0")
                + " twist=" + AimTwist(36f).ToString("0")
                + " gameplayDelay=0 rootMotion=0";
        }
    }
}
