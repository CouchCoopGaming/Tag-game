namespace Tag.Art
{
    /// <summary>
    /// Which exit a verb ending asks for. The motor state is only read.
    /// A jump off a wall is the wall-jump exit. A climb that becomes a mantle
    /// waits, then tops out. A fast mantle is the vault sweep.
    /// </summary>
    public static class VerbExitPick
    {
        public const float WallJumpRise = 2.5f;
        public const float VaultPlanar = 7.5f;
        public const float SlideJumpVy = 1f;

        public static VerbExitId WallLeave(bool wasRun, bool wasClimb, bool intoMantle, bool intoAir, float verticalSpeed)
        {
            if (!wasRun && !wasClimb) return VerbExitId.None;
            if (intoMantle) return VerbExitId.None;
            if (intoAir && verticalSpeed > WallJumpRise) return VerbExitId.WallJump;
            if (wasRun) return VerbExitId.WallRun;
            if (intoAir) return VerbExitId.ClingDrop;
            return VerbExitId.ClimbTopOut;
        }

        public static bool StepDown(bool grounded, float verticalSpeed)
        {
            return grounded || verticalSpeed < 0.5f;
        }

        public static VerbExitId MantleLeave(bool fromClimb, float entryPlanar)
        {
            if (fromClimb) return VerbExitId.ClimbTopOut;
            if (entryPlanar >= VaultPlanar) return VerbExitId.Vault;
            return VerbExitId.Mantle;
        }

        public static bool PlaySlideExit(bool intoAir, float verticalSpeed)
        {
            return !(intoAir && verticalSpeed > SlideJumpVy);
        }

        /// <summary>
        /// Jump, slide, punch, dash, and lunge peel an exit that is already up.
        /// The wall-jump exit keeps the jump that started it.
        /// </summary>
        public static bool Cancels(VerbExitId playing, bool jump, bool slide, bool punch, bool dash, bool lunge)
        {
            if (playing == VerbExitId.None) return false;
            if (slide || punch || lunge || dash) return true;
            if (jump && playing != VerbExitId.WallJump) return true;
            return false;
        }

        public static bool CancelsState(VerbExitId playing, bool enteredSlide, bool enteredWall, bool enteredMantle, bool enteredDash, bool enteredLunge, bool enteredPunch)
        {
            if (playing == VerbExitId.None) return false;
            return enteredSlide || enteredWall || enteredMantle || enteredDash || enteredLunge || enteredPunch;
        }
    }
}
