using Tag.Level;
using Tag.Modes;
using Tag.Settings;

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
            return ModeBlurb(id, GameSettings.Current);
        }

        public static string ModeBlurb(TagModeId id, GameSettings settings)
        {
            switch (id)
            {
                case TagModeId.HotPotato: return HotPotatoBlurb(settings);
                case TagModeId.TrailTag: return "Ribbons eliminate. Last standing.";
                case TagModeId.FreePlay: return "Punch transfers It. No timer.";
                default: return "Least time as It. Next punch breaks a tie.";
            }
        }

        /// <summary>
        /// Hot Potato's line follows the live win target. It does not repeat the
        /// rounds row, so "first to 2 round wins" cannot sit next to Rounds 1.
        /// </summary>
        public static string HotPotatoBlurb(GameSettings settings)
        {
            if (settings == null) settings = GameSettings.Defaults();
            int wins = settings.WinTarget;
            if (wins < GameSettings.WinTargetMin) wins = GameSettings.WinTargetMin;
            if (wins > GameSettings.WinTargetMax) wins = GameSettings.WinTargetMax;
            return "First to " + wins.ToString() + " wins. Fuse 45 / 40 / 35s.";
        }

        public static bool BlurbHolds()
        {
            GameSettings fresh = GameSettings.Defaults();
            string line = ModeBlurb(TagModeId.HotPotato, fresh);
            if (line != "First to 2 wins. Fuse 45 / 40 / 35s.") return false;
            if (line.IndexOf("round wins", System.StringComparison.Ordinal) >= 0) return false;
            fresh.WinTarget = 4;
            if (ModeBlurb(TagModeId.HotPotato, fresh) != "First to 4 wins. Fuse 45 / 40 / 35s.") return false;
            if (ModeBlurb(TagModeId.LeastIt, fresh).IndexOf("OK", System.StringComparison.Ordinal) >= 0) return false;
            return true;
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
                + "Team\n"
                + "Couch Co-op. This build is local tag.\n\n"
                + "Type\n"
                + "Headings are Bangers. Body letters are Liberation Sans Bold.\n"
                + "Bangers copyright 2010 The Bangers Project Authors.\n"
                + "Liberation Sans copyright 2010-2012 Red Hat, Inc.\n"
                + "SIL Open Font License, Version 1.1.\n"
                + "The license files sit next to the fonts. Nothing paid.\n\n"
                + "Tools\n"
                + "Unity, the input system already in the project, and the audio bus.\n"
                + "Menu sounds are clips that were already here.\n"
                + "One-shots under Assets/Audio are original synthesis.\n\n"
                + "Space still jumps. Online play is not in this build.";
        }
    }
}
