namespace Tag.Settings
{
    /// <summary>
    /// Menu open/close latch for cling, sprint, and jump.
    /// A hold that is up when the menu closes stays up. A jump that confirmed
    /// the menu, or that is still down on the resume frame, is not a new press.
    /// </summary>
    public struct HoldSample
    {
        public bool Cling;
        public bool Sprint;
        public bool Jump;
    }

    public struct HoldResult
    {
        public bool Cling;
        public bool Sprint;
        public bool JumpEdge;
    }

    public static class MenuHoldGate
    {
        public static HoldSample WhileOpen(bool cling, bool sprint, bool jump)
        {
            return new HoldSample { Cling = cling, Sprint = sprint, Jump = jump };
        }

        /// <summary>
        /// Physical state wins. The latch only decides whether jump is a new edge.
        /// Resume never treats a held jump as a fresh press.
        /// </summary>
        public static HoldResult OnResume(HoldSample latched, bool cling, bool sprint, bool jump)
        {
            return new HoldResult
            {
                Cling = cling,
                Sprint = sprint,
                JumpEdge = false
            };
        }

        /// <summary>The frame after resume. A new press can jump. A held key cannot.</summary>
        public static HoldResult Live(bool prevJump, bool jump, bool cling, bool sprint)
        {
            return new HoldResult
            {
                Cling = cling,
                Sprint = sprint,
                JumpEdge = jump && !prevJump
            };
        }
    }
}
