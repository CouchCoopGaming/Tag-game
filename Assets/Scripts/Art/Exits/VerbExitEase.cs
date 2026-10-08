namespace Tag.Art
{
    /// <summary>
    /// Visual onset for an exit. Cancel time and the clip clock stay put.
    /// The five loudest onsets, measured as the biggest bone step inside the
    /// first 0.08 s, rise on a longer smoothstep: zip drop, hands-down absorb,
    /// wall jump, climb top-out, and mantle.
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
            float window = Soft(id) ? SoftSeconds : VerbExitChain.BlendSeconds;
            float u = 1f;
            if (window > 0.0001f)
                u = age / window;
            return PoseHandoff.Ease(u);
        }
    }
}
