namespace Tag.Onboard
{
    public enum HintStep
    {
        Move = 0,
        Sprint = 1,
        Jump = 2,
        Slide = 3,
        ClingClimb = 4,
        WallJump = 5,
        AirDash = 6,
        Punch = 7,
        Count = 8
    }

    /// <summary>
    /// First-run hints. A matching action clears the current step.
    /// Skip and the hints themselves never pause play or swallow a press.
    /// </summary>
    public sealed class OnboardingSession
    {
        public const int StepCount = (int)HintStep.Count;
        const int SkipBit = 1 << StepCount;

        public static OnboardingSession Live = new OnboardingSession();

        public readonly bool[] Seen = new bool[StepCount];
        public int Current;
        public bool Running;
        public bool Skipped;
        public int Accepted;

        public bool Active => Running && !Skipped && Current < StepCount;
        public bool BlocksInput => false;
        public bool Pauses => false;
        public bool EatsInput => false;

        public static void ResetStatics()
        {
            Live = new OnboardingSession();
        }

        /// <summary>Countdown and play, including free play. Results and pause are outside.</summary>
        public static bool LearnWindow(bool countdown, bool playing)
        {
            return countdown || playing;
        }

        public void BeginIfNeeded(bool window)
        {
            if (Skipped || AllSeen())
            {
                Running = false;
                return;
            }
            Running = window;
            if (!Running) return;
            Current = FirstUnseen();
        }

        /// <summary>Counts the press either way. Only the current hint clears.</summary>
        public bool Offer(HintStep step, bool pressed)
        {
            if (!pressed) return false;
            Accepted++;
            if (!Active) return false;
            if ((int)step != Current) return false;
            Seen[Current] = true;
            Current = FirstUnseen();
            if (Current >= StepCount) Running = false;
            return true;
        }

        public void Skip()
        {
            Skipped = true;
            Running = false;
        }

        public void Replay()
        {
            for (int i = 0; i < StepCount; i++) Seen[i] = false;
            Skipped = false;
            Current = 0;
            Running = true;
        }

        public bool AllSeen()
        {
            for (int i = 0; i < StepCount; i++)
                if (!Seen[i]) return false;
            return true;
        }

        public int FirstUnseen()
        {
            for (int i = 0; i < StepCount; i++)
                if (!Seen[i]) return i;
            return StepCount;
        }

        public int Pack()
        {
            int mask = 0;
            for (int i = 0; i < StepCount; i++)
                if (Seen[i]) mask |= 1 << i;
            if (Skipped) mask |= SkipBit;
            return mask;
        }

        public void Unpack(int mask)
        {
            for (int i = 0; i < StepCount; i++)
                Seen[i] = (mask & (1 << i)) != 0;
            Skipped = (mask & SkipBit) != 0;
            Current = FirstUnseen();
            Running = false;
            Accepted = 0;
        }
    }
}
