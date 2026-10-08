using Tag.Level;
using Tag.Modes;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Copy for screens the game already has. Mode blurbs match the existing
    /// title card. Arena blurbs match the layout notes. No new rules.
    /// </summary>
    public static class MenuCatalog
    {
        public static int Modes = 4;

        public static string ModeName(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return "Hot Potato";
                case TagModeId.TrailTag: return "Trail Tag";
                case TagModeId.FreePlay: return "Free play";
                default: return "Least It";
            }
        }

        public static string ModeBlurb(TagModeId id)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return "First to 2. Fuse 45 / 40 / 35s.";
                case TagModeId.TrailTag: return "Ribbons eliminate. Last standing.";
                case TagModeId.FreePlay: return "Punch transfers It. No timer.";
                default: return "Least time as It. Next punch breaks a tie.";
            }
        }

        public static string ArenaBlurb(int id)
        {
            if (id == ParkArena.Pocket) return "Tight park on the same piece kit. 80 by 50 m.";
            if (id == ParkArena.Stack) return "Vertical yard. Decks at 6 m, roofs at 12 m.";
            return "The default park. Pads, zips, and the long loop.";
        }

        public static string Tip(int index)
        {
            return MenuTips.At(index);
        }

        public static string Credits()
        {
            return "TAG\n"
                + "A couch tag game for one keyboard and up to four pads.\n\n"
                + "Arenas on this build: Mega Park, Pocket Park, Stack Yard.\n"
                + "Modes: Least It, Hot Potato, Trail Tag, Free play.\n\n"
                + "Menu type is the Unity built-in font. No paid typefaces.\n"
                + "Menu sounds are the existing UI bus (move, confirm, back, join, error).\n"
                + "One-shot audio under Assets/Audio is original synthesis, CC0.\n"
                + "The playground music bed was already in the project.\n\n"
                + "Space still jumps, even when Jump is rebound.\n"
                + "Online play is not in this build.";
        }
    }
}
