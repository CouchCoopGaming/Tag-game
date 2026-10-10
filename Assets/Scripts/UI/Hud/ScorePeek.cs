namespace Tag.Ui.Hud
{
    /// <summary>
    /// The hold-scoreboard belongs to the seat whose device is held.
    /// Another split stays clear.
    /// </summary>
    public static class ScorePeek
    {
        public const string Title = "STANDINGS";

        public static bool Shows(int paneDevice, int heldDevice)
        {
            return heldDevice >= 0 && paneDevice == heldDevice;
        }

        public static bool Holds()
        {
            if (Shows(0, 1)) return false;
            if (!Shows(1, 1)) return false;
            if (Shows(1, -1)) return false;
            if (Shows(0, 0) == false) return false;
            if (Title != "STANDINGS") return false;
            return true;
        }
    }
}
