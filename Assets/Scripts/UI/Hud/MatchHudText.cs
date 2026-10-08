using Tag.Modes;

namespace Tag.Ui.Hud
{
    /// <summary>
    /// Clock, round, and callout strings built once. The match HUD reads them.
    /// </summary>
    public static class MatchHudText
    {
        public const int ClockMax = 600;

        public static readonly string Blank = "";
        public static readonly string Free = "FREE";
        public static readonly string Go = "GO";
        public static readonly string RoundEnd = "ROUND END";
        public static readonly string YoureIt = "YOU'RE IT!";
        public static readonly string Tagged = "TAGGED!";
        public static readonly string It = "IT";
        public static readonly string Ready = "READY";
        public static readonly string DashGo = "GO";
        public static readonly string Off = "—";
        public static readonly string Pull = "PULL";
        public static readonly string Hook = "HOOK";
        public static readonly string Aim = "AIM";
        public static readonly string In = "IN";
        public static readonly string Out = "OUT";
        public static readonly string Score = "SCORE";
        public static readonly string Tags = "TAGS";
        public static readonly string ComicHint = "COMIC WORDS ON";
        /// <summary>Verb words during a match. The options row toggles this.</summary>
        public static bool ComicWords = true;
        public static readonly string[] Seat = { "P1", "P2", "P3", "P4" };
        public static readonly string[] PreviewProfile = { "Keyboard", "Pad", "Pad", "Pad" };

        static readonly string[] ClockTable = BuildClock();
        static readonly string[,] RoundTable = BuildRounds();

        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int whole = (int)seconds;
            if (seconds > whole) whole++;
            if (whole < 0) whole = 0;
            if (whole >= ClockTable.Length) whole = ClockTable.Length - 1;
            return ClockTable[whole];
        }

        public static string Round(int round, int cap)
        {
            if (round < 0) round = 0;
            if (round > 9) round = 9;
            if (cap < 0) cap = 0;
            if (cap > 9) cap = 9;
            return RoundTable[round, cap];
        }

        public static string Metric(TagModeId id)
        {
            if (id == TagModeId.HotPotato) return "WINS";
            if (id == TagModeId.TrailTag) return "TRAIL";
            if (id == TagModeId.FreePlay) return "TAGS";
            return "TIME";
        }

        static string[] BuildClock()
        {
            var table = new string[ClockMax + 1];
            for (int i = 0; i <= ClockMax; i++)
            {
                int m = i / 60;
                int s = i % 60;
                string sec = s < 10 ? "0" + s.ToString() : s.ToString();
                table[i] = m.ToString() + ":" + sec;
            }
            return table;
        }

        static string[,] BuildRounds()
        {
            var table = new string[10, 10];
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 10; c++)
                {
                    if (c <= 1) table[r, c] = "R" + r.ToString();
                    else table[r, c] = "R" + r.ToString() + " / " + c.ToString();
                }
            }
            return table;
        }
    }
}
