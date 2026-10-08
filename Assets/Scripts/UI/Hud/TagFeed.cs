namespace Tag.Ui.Hud
{
    /// <summary>
    /// Three tag lines per match, shared by every split. The sentences are
    /// built once. A repeat of the same pair still pushes because the caller
    /// bumps a serial.
    /// </summary>
    public static class TagFeed
    {
        public const int Rows = 3;
        public const float Life = 3.2f;

        public static readonly string[,] Line = Build();

        struct Item
        {
            public int From;
            public int To;
            public float Until;
            public bool On;
        }

        static readonly Item[] Items = new Item[Rows];
        static int _count;

        public static void Reset()
        {
            _count = 0;
            for (int i = 0; i < Rows; i++) Items[i].On = false;
        }

        public static void Push(int from, int to, float now)
        {
            if (from < 0 || from > 3 || to < 0 || to > 3) return;
            if (_count < Rows) _count++;
            for (int i = _count - 1; i > 0; i--) Items[i] = Items[i - 1];
            Items[0].From = from;
            Items[0].To = to;
            Items[0].Until = now + Life;
            Items[0].On = true;
        }

        public static bool On(int row, float now)
        {
            if (row < 0 || row >= _count) return false;
            return Items[row].On && now < Items[row].Until;
        }

        public static string Text(int row)
        {
            if (row < 0 || row >= _count || !Items[row].On) return "";
            return Line[Items[row].From, Items[row].To];
        }

        public static int From(int row)
        {
            if (row < 0 || row >= _count || !Items[row].On) return 0;
            return Items[row].From;
        }

        public static float Alpha(int row, float now, bool hold)
        {
            if (!On(row, now)) return 0f;
            if (hold) return 1f;
            float left = Items[row].Until - now;
            float a = left / Life;
            if (a < 0f) a = 0f;
            if (a > 1f) a = 1f;
            return a;
        }

        public static bool Holds()
        {
            Reset();
            if (Line[0, 2] != "P1 tagged P3") return false;
            if (Line[2, 0] != "P3 tagged P1") return false;
            Push(0, 2, 0f);
            Push(1, 0, 0.2f);
            Push(2, 3, 0.4f);
            Push(0, 1, 0.6f);
            if (Text(0) != "P1 tagged P2") return false;
            if (Text(1) != "P3 tagged P4") return false;
            if (Text(2) != "P2 tagged P1") return false;
            if (On(0, 0.6f + Life - 0.05f) == false) return false;
            if (On(0, 0.6f + Life + 0.05f)) return false;
            if (Alpha(0, 0.6f, true) < 0.99f) return false;
            float mid = Alpha(0, 0.6f + Life * 0.5f, false);
            if (mid < 0.45f || mid > 0.55f) return false;
            string again = Line[0, 2];
            Push(0, 2, 1f);
            if (Text(0) != again) return false;
            return true;
        }

        static string[,] Build()
        {
            var table = new string[4, 4];
            string[] seat = { "P1", "P2", "P3", "P4" };
            for (int a = 0; a < 4; a++)
            {
                for (int b = 0; b < 4; b++)
                    table[a, b] = seat[a] + " tagged " + seat[b];
            }
            return table;
        }
    }
}
