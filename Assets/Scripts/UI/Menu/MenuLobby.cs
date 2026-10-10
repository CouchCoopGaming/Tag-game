using Tag.Modes;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// How many people a mode needs before the lobby can start.
    /// Free play can be one person. The tag modes need two.
    /// </summary>
    public static class MenuLobby
    {
        public static int MinHumans(TagModeId id)
        {
            if (id == TagModeId.FreePlay) return 1;
            return 2;
        }

        public static bool Enough(TagModeId id, int humans)
        {
            if (humans < 0) humans = 0;
            return humans >= MinHumans(id);
        }

        public static string Short(TagModeId id)
        {
            if (id == TagModeId.FreePlay) return "Free play needs 1 player.";
            if (id == TagModeId.HotPotato) return "Hot Potato needs 2 players.";
            if (id == TagModeId.TrailTag) return "Trail Tag needs 2 players.";
            return "Least It needs 2 players.";
        }
    }
}
