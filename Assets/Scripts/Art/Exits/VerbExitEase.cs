namespace Tag.Art
{
    /// <summary>
    /// Visual onset for an exit. Cancel time and the clip clock stay put.
    /// A joined exit already leaves the live pose inside <see cref="VerbExitFit"/>
    /// over <see cref="VerbExitFit.Join"/> of the clip. That blend is the only
    /// onset, so <see cref="Enter"/> stays at 1 and does not stack a second ease.
    /// Stagger and tag-back do not join, and they still rise over
    /// <see cref="VerbExitChain.BlendSeconds"/>.
    /// The five loud exits (zip drop, hands-down absorb, wall jump, climb
    /// top-out, mantle) keep <see cref="SoftSeconds"/> for the case they stop
    /// joining. While they join, that 0.16 s weight is not applied.
    /// </summary>
    public static class VerbExitEase
    {
        public const float SoftSeconds = 0.16f;

        public static bool Soft(VerbExitId id)
        {
            return id == VerbExitId.ZipDrop
                || id == VerbExitId.RollAbsorb
                || id == VerbExitId.WallJump
                || id == VerbExitId.ClimbTopOut
                || id == VerbExitId.Mantle;
        }

        public static float Enter(VerbExitId id, float age)
        {
            if (VerbExitFit.Joins(id))
                return 1f;
            float window = Soft(id) ? SoftSeconds : VerbExitChain.BlendSeconds;
            float u = 1f;
            if (window > 0.0001f)
                u = age / window;
            return PoseHandoff.Ease(u);
        }
    }
}
