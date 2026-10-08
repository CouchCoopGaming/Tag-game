namespace Tag.Ui.Hud
{
    /// <summary>
    /// Which full-screen card the round is on. Sudden death wins. Round 2 and
    /// the last round replace the countdown digit. Match point is the rest.
    /// </summary>
    public static class RoundCard
    {
        public const int None = 0;
        public const int Digit = 1;
        public const int Round2 = 2;
        public const int Final = 3;
        public const int Sudden = 4;
        public const int Point = 5;

        public const string Round2Text = "ROUND 2";
        public const string FinalText = "FINAL ROUND";
        public const string SuddenText = "SUDDEN DEATH";
        public const string PointText = "MATCH POINT";

        public static int Pick(bool sudden, bool countdown, int round, int cap, bool point)
        {
            if (sudden) return Sudden;
            if (countdown)
            {
                if (cap > 1 && round >= cap) return Final;
                if (round == 2) return Round2;
                return Digit;
            }
            if (point) return Point;
            return None;
        }

        public static bool Holds()
        {
            if (Pick(true, true, 2, 3, true) != Sudden) return false;
            if (Pick(false, true, 2, 3, false) != Round2) return false;
            if (Pick(false, true, 3, 3, false) != Final) return false;
            if (Pick(false, true, 1, 3, false) != Digit) return false;
            if (Pick(false, true, 4, 1, false) != Digit) return false;
            if (Pick(false, false, 2, 3, true) != Point) return false;
            if (Pick(false, false, 1, 1, false) != None) return false;
            if (Round2Text != "ROUND 2" || FinalText != "FINAL ROUND") return false;
            if (SuddenText != "SUDDEN DEATH" || PointText != "MATCH POINT") return false;
            return true;
        }
    }
}
