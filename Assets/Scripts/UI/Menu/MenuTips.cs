namespace Tag.Ui.Menu
{
    /// <summary>
    /// Loading tips. Each line is a verb or a rule the match already uses.
    /// </summary>
    public static class MenuTips
    {
        public static readonly string[] Lines =
        {
            "Space jumps.",
            "Sprint, then slide.",
            "Hold into a wall to climb.",
            "Hold into a wall to cling. Jump while clinging to wall jump.",
            "Air dash in the air.",
            "Punch to tag. It changes hands.",
            "Tag-back is 1 second.",
            "Double-click RMB to let go of the grapple.",
            "Least It: least time as It. Next punch breaks a tie.",
            "Hot Potato: first to 2.",
            "Trail Tag: last one standing.",
            "Free play has no timer.",
            "Pads launch. Zips carry you."
        };

        public static int Count => Lines.Length;

        public static string At(int index)
        {
            int n = Lines.Length;
            if (n < 1) return "";
            int i = index;
            if (i < 0) i = 0;
            i %= n;
            return Lines[i];
        }

        public static bool Holds()
        {
            if (Count < 8) return false;
            if (At(0) != "Space jumps.") return false;
            if (At(Count) != At(0)) return false;
            if (At(9).IndexOf("first to 2", System.StringComparison.Ordinal) < 0) return false;
            return true;
        }
    }
}
