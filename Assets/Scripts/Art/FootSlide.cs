using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Centimeters a planted sole misses in one contact. The mesh cadence and
    /// the stance length match walk 6.9, sprint 13.8, crouch 3.68, and wall-run
    /// 9.5. The capsule speeds are not written. A short stride stays on the
    /// exponential slew.
    /// </summary>
    public static class FootSlide
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;

        public const float Walk = 6.9f;
        public const float Sprint = 13.8f;
        public const float Crouch = 3.68f;
        public const float Wall = 9.5f;
        /// <summary>Extra toe-off on a wall-run plant, degrees. The swing tuck stays.</summary>
        public const float WallToe = 2.2f;

        public static float Cm(float speed, float footMeters, float cadence)
        {
            if (cadence < 0.05f || speed <= 0.05f) return 0f;
            float body = speed * 3.14159265f / cadence;
            float miss = body - footMeters;
            if (miss < 0f) miss = 0f;
            return miss * 100f;
        }

        /// <summary>Degrees added to a trailing thigh. A forward thigh is unchanged.</summary>
        public static float WallTrail(float thighDeg)
        {
            if (thighDeg >= 0f) return 0f;
            return WallToe;
        }

        public static float WalkBefore()
        {
            return Cm(Walk, Gait(Walk), GaitBlend.CadenceAt(GaitBlend.WalkSpeed));
        }

        public static float WalkAfter()
        {
            return Cm(Walk, Gait(Walk), LocomotionPolish.PlayCadence(Walk));
        }

        public static float SprintBefore()
        {
            return Cm(Sprint, Gait(Sprint), GaitBlend.CadenceAt(GaitBlend.SprintSpeed));
        }

        public static float SprintAfter()
        {
            return Cm(Sprint, Gait(Sprint), LocomotionPolish.PlayCadence(Sprint));
        }

        public static float CrouchBefore()
        {
            return Cm(Crouch, BodyLife.CrouchFootTravel(), GaitBlend.CadenceAt(Crouch));
        }

        public static float CrouchAfter()
        {
            return Cm(Crouch, BodyLife.CrouchFootTravel(), BodyLife.CrouchCadence(Crouch));
        }

        public static float WallBefore()
        {
            return Cm(Wall, Tucked(Wall), WallPose.RunCadenceFull);
        }

        public static float WallAfter()
        {
            return Cm(Wall, Planted(Wall), WallPose.RunCadenceFull);
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "foot-slide"
                + " walk=" + WalkBefore().ToString("0.0", c) + ">" + WalkAfter().ToString("0.0", c)
                + " sprint=" + SprintBefore().ToString("0.0", c) + ">" + SprintAfter().ToString("0.0", c)
                + " crouch=" + CrouchBefore().ToString("0.0", c) + ">" + CrouchAfter().ToString("0.0", c)
                + " wall=" + WallBefore().ToString("0.0", c) + ">" + WallAfter().ToString("0.0", c)
                + " gameplayDelay=0 rootMotion=0";
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (Mathf.Abs(Walk - 6.9f) > 0.001f || Mathf.Abs(Sprint - 13.8f) > 0.001f) return false;
            if (Mathf.Abs(Crouch - 3.68f) > 0.001f || Mathf.Abs(Wall - 9.5f) > 0.001f) return false;
            if (SprintBefore() < 30f) return false;
            if (SprintAfter() > 1f) return false;
            if (CrouchBefore() < 20f) return false;
            if (CrouchAfter() > 1f) return false;
            if (WallBefore() < 10f) return false;
            if (WallAfter() > 2f) return false;
            if (WalkAfter() > 1f) return false;
            if (WalkBefore() + 0.5f < WalkAfter()) return false;
            if (WallTrail(10f) > 0.001f) return false;
            if (Mathf.Abs(WallTrail(-4f) - WallToe) > 0.001f) return false;
            return true;
        }

        static float Gait(float speed)
        {
            return GaitBlend.FootTravel(GaitBlend.PoseWeight(speed));
        }

        static float Tucked(float speed)
        {
            float w = GaitBlend.PoseWeight(speed);
            float front = GaitBlend.FrontReach(w) * 0.72f * Mathf.Deg2Rad;
            float back = GaitBlend.BackReach(w) * 0.72f * Mathf.Deg2Rad;
            float meters = GaitBlend.LegLength * (Mathf.Sin(front) + Mathf.Sin(back));
            return meters < 0.08f ? 0.08f : meters;
        }

        static float Planted(float speed)
        {
            float w = GaitBlend.PoseWeight(speed);
            float front = GaitBlend.FrontReach(w) * Mathf.Deg2Rad;
            float back = (GaitBlend.BackReach(w) + WallToe) * Mathf.Deg2Rad;
            float meters = GaitBlend.LegLength * (Mathf.Sin(front) + Mathf.Sin(back));
            return meters < 0.08f ? 0.08f : meters;
        }
    }

    /// <summary>
    /// A kill-box or practice restart hides the mesh and shuts the boom, then
    /// brings them back. The capsule position is already the new pad.
    /// </summary>
    public static class RespawnBlink
    {
        public const float Seconds = 0.12f;
        public const bool GameplayInstant = true;

        public static float Hidden(float age)
        {
            if (age < 0f || age >= Seconds) return 0f;
            float u = age / Seconds;
            if (u < 0.35f) return 1f;
            float t = (u - 0.35f) / 0.65f;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return 1f - t;
        }

        /// <summary>1 when the lens is open. A blink pulls the boom in, then eases it out.</summary>
        public static float Open(float age)
        {
            float open = 1f - Hidden(age) * 0.65f;
            if (open < 0.35f) open = 0.35f;
            return open;
        }

        public static bool Holds()
        {
            if (!GameplayInstant) return false;
            if (Mathf.Abs(Seconds - 0.12f) > 0.001f) return false;
            if (Mathf.Abs(Hidden(0f) - 1f) > 0.001f) return false;
            if (Hidden(Seconds) > 0.001f) return false;
            if (Hidden(-1f) > 0.001f) return false;
            if (Open(Seconds) < 0.99f) return false;
            if (Open(0f) > 0.4f) return false;
            return true;
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "respawn-blink"
                + " hide=" + Hidden(0f).ToString("0.00", c)
                + " back=" + Hidden(Seconds).ToString("0.00", c)
                + " open=" + Open(0f).ToString("0.00", c)
                + " seconds=" + Seconds.ToString("0.00", c)
                + " gameplayInstant=1";
        }
    }
}
