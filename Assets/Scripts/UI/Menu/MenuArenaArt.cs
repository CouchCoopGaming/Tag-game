using Tag.Level;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Overview stills of the three parks. Files live under Resources so a
    /// player build includes them, and under Assets/UI/ArenaThumbs as the
    /// bake folder. An editor script can recapture them.
    /// </summary>
    public static class MenuArenaArt
    {
        static readonly Texture2D[] Cache = new Texture2D[3];
        static readonly string[] Keys = { "Mega", "Pocket", "Stack" };

        public static Texture2D Thumb(int arena)
        {
            int i = arena;
            if (i < 0 || i >= ParkArena.Count) i = 0;
            if (Cache[i] != null) return Cache[i];
            Cache[i] = Resources.Load<Texture2D>("UI/ArenaThumbs/" + Keys[i]);
            return Cache[i];
        }

        public static string Key(int arena)
        {
            int i = arena;
            if (i < 0) i = 0;
            if (i >= Keys.Length) i = 0;
            return Keys[i];
        }
    }
}
