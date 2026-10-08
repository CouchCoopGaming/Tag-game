using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Rules the lobby can change. Length and round count already existed.
    /// Win target, who starts as It, a per-seat handicap label, and pad/zip
    /// hazards are stored beside them. Handicap is a label only.
    /// </summary>
    public static class RuleBook
    {
        public const int Length = 4;
        public const int Rounds = 5;
        public const int Win = 6;
        public const int Start = 7;
        public const int Seat = 8;
        public const int Hand0 = 9;
        public const int Pads = 13;
        public const int Zips = 14;
        public const int Ai = 15;
        public const int Diff = 16;
        public const int Split = 17;
        public const int Listen = 18;
        public const int Arena = 19;
        public const int Back = 20;
        public const int Count = 21;

        static readonly string[] Seconds = { "60 s", "120 s", "180 s", "300 s" };
        static readonly string[] CountWord = { "1", "2", "3", "4", "5" };
        static readonly string[] AiWord =
        {
            "0   (0-3, seats left over)",
            "1   (0-3, seats left over)",
            "2   (0-3, seats left over)",
            "3   (0-3, seats left over)"
        };
        static readonly string[] HandWord = { "Off", "Light", "Heavy" };
        static readonly string[] SeatWord = { "P1", "P2", "P3", "P4" };
        static readonly string[] HandTitle = { "P1 handicap", "P2 handicap", "P3 handicap", "P4 handicap" };
        static readonly string[] Chosen =
        {
            "Starting It  Chosen  P1",
            "Starting It  Chosen  P2",
            "Starting It  Chosen  P3",
            "Starting It  Chosen  P4"
        };

        public static string Title(int index)
        {
            if (index == Length) return "Round length";
            if (index == Rounds) return "Rounds";
            if (index == Win) return "Win target";
            if (index == Start) return "Starting It";
            if (index == Seat) return "Chosen seat";
            if (index >= Hand0 && index < Hand0 + GameSettings.SeatCount) return HandTitle[index - Hand0];
            if (index == Pads) return "Launch pads";
            if (index == Zips) return "Zip lines";
            if (index == Ai) return "AI opponents";
            if (index == Diff) return "Difficulty";
            if (index == Split) return "Split";
            if (index == Listen) return "Listener";
            if (index == Arena) return "Arena select";
            return "Back";
        }

        public static string Detail(GameSettings s, int index)
        {
            if (s == null) s = GameSettings.Defaults();
            if (index == Length) return Seconds[Clamp(s.RoundLengthIndex, 0, Seconds.Length - 1)];
            if (index == Rounds) return CountWord[Clamp(s.RoundsPerMatch - 1, 0, CountWord.Length - 1)];
            if (index == Win) return CountWord[Clamp(s.WinTarget - 1, 0, CountWord.Length - 1)];
            if (index == Start) return StartWord(s.StartIt);
            if (index == Seat) return SeatWord[Clamp(s.StartSeat, 0, SeatWord.Length - 1)];
            if (index >= Hand0 && index < Hand0 + GameSettings.SeatCount)
                return HandWord[Clamp(s.Handicap[index - Hand0], 0, HandWord.Length - 1)];
            if (index == Pads) return s.HazardPads ? "On" : "Off";
            if (index == Zips) return s.HazardZips ? "On" : "Off";
            if (index == Ai) return AiWord[Clamp(s.AiOpponents, 0, AiWord.Length - 1)];
            if (index == Diff) return s.DifficultyLabel();
            if (index == Split) return s.SplitAxis == GameSettings.SplitHorizontal ? "Horizontal" : "Vertical";
            if (index == Listen) return s.Listener == GameSettings.ListenAverage ? "Average" : "P1";
            if (index == Arena) return "Next";
            return "Characters";
        }

        public static string LoadLength(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            return Seconds[Clamp(s.RoundLengthIndex, 0, Seconds.Length - 1)];
        }

        public static string LoadRounds(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            return CountWord[Clamp(s.RoundsPerMatch - 1, 0, CountWord.Length - 1)];
        }

        public static string LoadWin(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            return CountWord[Clamp(s.WinTarget - 1, 0, CountWord.Length - 1)];
        }

        public static string LoadStart(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            if (s.StartIt == GameSettings.StartChosen)
                return Chosen[Clamp(s.StartSeat, 0, Chosen.Length - 1)];
            if (s.StartIt == GameSettings.StartLast) return "Starting It  Last place";
            return "Starting It  Random";
        }

        public static string LoadHand(GameSettings s, int seat)
        {
            if (s == null) s = GameSettings.Defaults();
            int i = seat;
            if (i < 0 || i >= GameSettings.SeatCount) i = 0;
            return HandWord[Clamp(s.Handicap[i], 0, HandWord.Length - 1)];
        }

        public static string LoadPads(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            return s.HazardPads ? "On" : "Off";
        }

        public static string LoadZips(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            return s.HazardZips ? "On" : "Off";
        }

        public static bool Edit(GameSettings s, int focus, int dir)
        {
            if (s == null || dir == 0) return false;
            if (focus < Length || focus >= Arena) return false;
            int step = dir > 0 ? 1 : -1;
            if (focus == Length) s.RoundLengthIndex += step;
            else if (focus == Rounds) s.RoundsPerMatch += step;
            else if (focus == Win) s.WinTarget += step;
            else if (focus == Start) s.StartIt += step;
            else if (focus == Seat) s.StartSeat += step;
            else if (focus >= Hand0 && focus < Hand0 + GameSettings.SeatCount)
                s.Handicap[focus - Hand0] += step;
            else if (focus == Pads) s.HazardPads = !s.HazardPads;
            else if (focus == Zips) s.HazardZips = !s.HazardZips;
            else if (focus == Ai) s.AiOpponents += step;
            else if (focus == Diff) s.DifficultyTier += step;
            else if (focus == Split)
                s.SplitAxis = s.SplitAxis == GameSettings.SplitHorizontal ? GameSettings.SplitVertical : GameSettings.SplitHorizontal;
            else if (focus == Listen)
                s.Listener = s.Listener == GameSettings.ListenAverage ? GameSettings.ListenP1 : GameSettings.ListenAverage;
            else return false;
            s.Clamp();
            return true;
        }

        public static bool Holds()
        {
            if (GameSettings.RoundLengthPresets[1] != 120f) return false;
            if (GameSettings.FovDefault != 78f) return false;
            if (GameSettings.WinTargetDefault != 2) return false;
            GameSettings fresh = GameSettings.Defaults();
            if (fresh.WinTarget != 2 || fresh.StartIt != GameSettings.StartRandom) return false;
            if (!fresh.HazardPads || !fresh.HazardZips) return false;
            if (fresh.Handicap[0] != 0 || fresh.ConfirmFace[0] != FaceMap.Auto) return false;
            if (LoadLength(fresh) != "120 s" || LoadRounds(fresh) != "1" || LoadWin(fresh) != "2") return false;
            if (LoadStart(fresh) != "Starting It  Random") return false;
            if (LoadPads(fresh) != "On" || LoadZips(fresh) != "On") return false;
            if (!Edit(fresh, Win, 1) || fresh.WinTarget != 3) return false;
            if (!Edit(fresh, Start, 1) || fresh.StartIt != GameSettings.StartLast) return false;
            if (LoadStart(fresh) != "Starting It  Last place") return false;
            if (!Edit(fresh, Start, 1) || fresh.StartIt != GameSettings.StartChosen) return false;
            if (!Edit(fresh, Seat, 1) || LoadStart(fresh) != "Starting It  Chosen  P2") return false;
            if (!Edit(fresh, Hand0 + 2, 1) || fresh.Handicap[2] != GameSettings.HandicapLight) return false;
            if (!Edit(fresh, Pads, 1) || fresh.HazardPads) return false;
            if (!Edit(fresh, Zips, 1) || fresh.HazardZips) return false;
            if (fresh.RoundSeconds() != 120f) return false;
            string blob = SettingsFile.Write(fresh, ActionBinds.Defaults());
            GameSettings back = GameSettings.Defaults();
            SettingsFile.Read(blob, back, ActionBinds.Defaults());
            if (back.WinTarget != 3 || back.StartIt != GameSettings.StartChosen || back.StartSeat != 1) return false;
            if (back.Handicap[2] != GameSettings.HandicapLight) return false;
            if (back.HazardPads || back.HazardZips) return false;
            if (back.RoundLengthIndex != GameSettings.RoundLengthDefault) return false;
            GameSettings partial = GameSettings.Defaults();
            SettingsFile.Read("v=2\nmouse=1.8\n", partial, ActionBinds.Defaults());
            if (partial.WinTarget != 2 || partial.StartIt != 0 || !partial.HazardPads || !partial.HazardZips) return false;
            if (partial.ConfirmFace[1] != FaceMap.Auto) return false;
            if (Title(Arena) != "Arena select" || Title(Back) != "Back") return false;
            if (Count != Back + 1) return false;
            return true;
        }

        static string StartWord(int start)
        {
            if (start == GameSettings.StartLast) return "Last place";
            if (start == GameSettings.StartChosen) return "Chosen";
            return "Random";
        }

        static int Clamp(int v, int lo, int hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
