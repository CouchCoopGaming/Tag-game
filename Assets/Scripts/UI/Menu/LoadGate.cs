namespace Tag.Ui.Menu
{
    /// <summary>
    /// Loading fill is three real steps: still waiting, StartMatch has been
    /// called, the round is active. It does not creep forward on a timer.
    /// </summary>
    public static class LoadGate
    {
        public const string Waiting = "Waiting";
        public const string Starting = "Starting";
        public const string Ready = "Ready";

        public static float Fill(bool fired, bool roundActive)
        {
            if (roundActive) return 1f;
            if (fired) return 0.5f;
            return 0f;
        }

        public static int Step(bool fired, bool roundActive)
        {
            if (roundActive) return 2;
            if (fired) return 1;
            return 0;
        }

        public static string Caption(bool fired, bool roundActive)
        {
            if (roundActive) return Ready;
            if (fired) return Starting;
            return Waiting;
        }

        public static bool Holds()
        {
            if (Fill(false, false) != 0f) return false;
            if (Fill(true, false) != 0.5f) return false;
            if (Fill(true, true) != 1f) return false;
            if (Fill(false, true) != 1f) return false;
            if (Step(false, false) != 0 || Step(true, false) != 1 || Step(true, true) != 2) return false;
            if (Caption(false, false) != Waiting) return false;
            if (Caption(true, false) != Starting) return false;
            if (Caption(false, true) != Ready) return false;
            if (Fill(true, false) != Fill(true, false)) return false;
            return true;
        }
    }
}
