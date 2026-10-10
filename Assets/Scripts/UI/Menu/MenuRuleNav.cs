namespace Tag.Ui.Menu
{
    /// <summary>
    /// Mode tiles are a 2 by 2. Rules are the rows under them.
    /// Up and down stay in a column. Right from a mode enters the rules.
    /// Left and right on a rule change its value. Left that cannot go
    /// lower returns to the mode tiles.
    /// </summary>
    public static class MenuRuleNav
    {
        public const int Modes = 4;
        public const int Cols = 2;

        public static int Vertical(int focus, int count, int dy, int selectedMode)
        {
            if (count < 1) return 0;
            if (focus < 0) focus = 0;
            if (focus >= count) focus = count - 1;
            if (focus < Modes)
            {
                int col = focus % Cols;
                int row = focus / Cols;
                int nextRow = row + (dy > 0 ? -1 : 1);
                if (nextRow < 0) return focus;
                if (nextRow > 1) return Modes < count ? Modes : focus;
                return nextRow * Cols + col;
            }
            int next = focus + (dy > 0 ? -1 : 1);
            if (next < Modes) return ModeFromRule(selectedMode);
            if (next >= count) return count - 1;
            return next;
        }

        public static int ModeSide(int focus, int dir)
        {
            if (focus < 0 || focus >= Modes) return focus;
            int col = focus % Cols;
            int row = focus / Cols;
            if (dir > 0 && col == Cols - 1) return Modes;
            if (dir < 0 && col == 0) return focus;
            return row * Cols + col + (dir > 0 ? 1 : -1);
        }

        public static int ModeFromRule(int selectedMode)
        {
            int mode = selectedMode;
            if (mode < 0) mode = 0;
            if (mode > Modes - 1) mode = Modes - 1;
            if ((mode & 1) == 0) mode += 1;
            return mode;
        }

        public static int AfterEdit(int focus, int dir, bool changed, int selectedMode)
        {
            if (focus >= Modes && dir < 0 && !changed)
                return ModeFromRule(selectedMode);
            return focus;
        }

        public static bool Holds()
        {
            if (ModeSide(0, 1) != 1) return false;
            if (ModeSide(1, 1) != Modes) return false;
            if (ModeSide(0, -1) != 0) return false;
            if (ModeSide(2, 1) != 3) return false;
            if (Vertical(0, 21, 1, 1) != 0) return false;
            if (Vertical(0, 21, -1, 1) != 2) return false;
            if (Vertical(1, 21, -1, 1) != 3) return false;
            if (Vertical(2, 21, -1, 1) != Modes) return false;
            if (Vertical(Modes, 21, 1, 1) != 1) return false;
            if (Vertical(5, 21, 1, 1) != 4) return false;
            if (Vertical(20, 21, -1, 1) != 20) return false;
            if (ModeFromRule(0) != 1) return false;
            if (ModeFromRule(1) != 1) return false;
            if (AfterEdit(4, -1, false, 1) != 1) return false;
            if (AfterEdit(4, -1, true, 1) != 4) return false;
            if (AfterEdit(6, 1, true, 1) != 6) return false;
            return true;
        }
    }
}
