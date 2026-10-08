using Tag.Core;
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
        public static readonly string RoundOver = "ROUND OVER";
        public static readonly string Locked = "LOCKED";
        public static readonly string ResultsWord = "RESULTS";
        public static readonly string NextRound = "NEXT ROUND";
        public static readonly string LeastWins = "LEAST IT TIME WINS";
        public static readonly string SoloAi = "AI";
        public static readonly string[] Left = { "P1 left", "P2 left", "P3 left", "P4 left" };
        public static readonly string YoureIt = "YOU'RE IT!";
        public static readonly string Tagged = "TAGGED!";
        public static readonly string It = "IT";
        public static readonly string DashLabel = "DASH";
        public static readonly string SafeLabel = "SAFE";
        public static readonly string[] SafeLine = BuildSafe();
        public static readonly string[] PreviewScore = { "P1   12.4", "P2   8.1", "P3   4.0", "P4   1.2" };
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
        public static bool ComicWords = true;

        public static string ComicLine()
        {
            return ComicWords ? ComicHint : "COMIC WORDS OFF";
        }

        /// <summary>Onomatopoeia. Cooldown digits are not words, so they stay.</summary>
        public static string Comic(string word)
        {
            if (!ComicWords || string.IsNullOrEmpty(word)) return Blank;
            return word;
        }
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

        static string[] BuildSafe()
        {
            var table = new string[11];
            for (int i = 0; i <= 10; i++)
                table[i] = SafeLabel + "  " + HudDigits.Tenth0(i * 0.1f);
            return table;
        }

        public static string SafeAt(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int i = (int)(seconds * 10f + 0.5f);
            if (i > 10) i = 10;
            return SafeLine[i];
        }

        static readonly string[] StandingLine = new string[4];
        static readonly int[] StandingKey = { -2, -2, -2, -2 };

        /// <summary>Seat plus tenths, cached per tenth so the standings card does not build a string every frame.</summary>
        public static string Standing(int seat, float time)
        {
            if (seat < 0 || seat > 3) return Blank;
            if (time < 0f) time = 0f;
            int key = (int)(time * 10f + 0.5f);
            if (key > 12000) key = 12000;
            if (StandingKey[seat] == key && StandingLine[seat] != null) return StandingLine[seat];
            StandingKey[seat] = key;
            StandingLine[seat] = Seat[seat] + "   " + HudDigits.Tenth0(key * 0.1f);
            return StandingLine[seat];
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
