namespace Tag.Art
{
    /// <summary>
    /// How long an exit stays up, and how fast an input peels it off.
    /// Weights use <see cref="PoseHandoff.Ease"/> so a later blend change
    /// carries these exits with it. Input is not delayed.
    /// </summary>
    public static class VerbExitClock
    {
        public const bool RootMotion = false;
        /// <summary>An input peels the exit off inside this window. Under 0.08s.</summary>
        public const float CancelSeconds = 0.06f;

        public static readonly VerbExitId[] Catalog =
        {
            VerbExitId.WallRun,
            VerbExitId.WallJump,
            VerbExitId.ClimbTopOut,
            VerbExitId.ClingDrop,
            VerbExitId.Vault,
            VerbExitId.Mantle,
            VerbExitId.Slide,
            VerbExitId.AirDash,
            VerbExitId.Punch,
            VerbExitId.Lunge,
            VerbExitId.ZipDrop,
            VerbExitId.LaunchLand,
            VerbExitId.GrappleArrive,
            VerbExitId.GrappleRelease,
            VerbExitId.Stagger,
            VerbExitId.TagBackEnd,
            VerbExitId.SoftLand,
            VerbExitId.Roll,
            VerbExitId.RollAbsorb
        };

        public static float Duration(VerbExitId id)
        {
            switch (id)
            {
                case VerbExitId.WallRun: return 0.28f;
                case VerbExitId.WallJump: return 0.30f;
                case VerbExitId.ClimbTopOut: return 0.32f;
                case VerbExitId.ClingDrop: return 0.22f;
                case VerbExitId.Vault: return 0.30f;
                case VerbExitId.Mantle: return 0.28f;
                case VerbExitId.Slide: return 0.26f;
                case VerbExitId.AirDash: return 0.18f;
                case VerbExitId.Punch: return 0.28f;
                case VerbExitId.Lunge: return 0.26f;
                case VerbExitId.ZipDrop: return 0.24f;
                case VerbExitId.LaunchLand: return 0.30f;
                case VerbExitId.GrappleArrive: return 0.26f;
                case VerbExitId.GrappleRelease: return 0.22f;
                case VerbExitId.Stagger: return 0.30f;
                case VerbExitId.TagBackEnd: return 0.22f;
                case VerbExitId.SoftLand: return 0.24f;
                case VerbExitId.Roll: return LandingRollPose.Seconds;
                case VerbExitId.RollAbsorb: return LandingRollPose.AbsorbSeconds;
                default: return 0f;
            }
        }

        public static bool LengthOk(VerbExitId id)
        {
            float d = Duration(id);
            if (id == VerbExitId.Roll)
                return d >= 0.45f && d <= 0.60f;
            if (id == VerbExitId.None)
                return d == 0f;
            return d >= 0.15f && d <= 0.35f;
        }

        /// <summary>1 through the readable part, then ease to 0 so the pose underneath wins.</summary>
        public static float PoseWeight(float age, float duration)
        {
            if (duration <= 0.0001f) return 0f;
            if (age < 0f) age = 0f;
            float u = age / duration;
            if (u >= 1f) return 0f;
            const float hold = 0.62f;
            if (u <= hold) return 1f;
            float t = (u - hold) / (1f - hold);
            return 1f - PoseHandoff.Ease(t);
        }

        /// <summary>1 before a cancel, 0 once <see cref="CancelSeconds"/> has passed.</summary>
        public static float CancelMul(float cancelAge)
        {
            if (cancelAge < 0f) return 1f;
            if (CancelSeconds <= 0.0001f) return 0f;
            if (cancelAge >= CancelSeconds) return 0f;
            return 1f - PoseHandoff.Ease(cancelAge / CancelSeconds);
        }

        public static float Weight(float age, float duration, float cancelAge)
        {
            return PoseWeight(age, duration) * CancelMul(cancelAge);
        }
    }
}
