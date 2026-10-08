using Tag.Level;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Baked menu art. Mega Park is a golden-hour render of the pass-19 yard.
    /// The lockup, the chase, and the ready burst match the comic wordmarks.
    /// Loaded once. The live character view stays the Hier preview.
    /// </summary>
    public static class MenuBackdrop
    {
        static Texture2D _dusk;
        static Texture2D _gold;
        static Texture2D _chase;
        static Texture2D _lockup;
        static Texture2D _ready;
        static bool _tried;

        public static Texture Fly(int arena)
        {
            Load();
            if (arena == ParkArena.Mega && _dusk != null) return _dusk;
            return MenuArenaArt.Thumb(arena);
        }

        /// <summary>The bright Mega Park shot. Loading uses this so the yard is not the dusk plate.</summary>
        public static Texture Bright(int arena)
        {
            Load();
            if (arena == ParkArena.Mega && _gold != null) return _gold;
            return Fly(arena);
        }

        public static Texture Shot(int arena)
        {
            Load();
            return MenuArenaArt.Thumb(arena);
        }

        public static Texture2D Chase
        {
            get
            {
                Load();
                return _chase;
            }
        }

        public static Texture2D Lockup
        {
            get
            {
                Load();
                return _lockup;
            }
        }

        public static Texture2D Ready
        {
            get
            {
                Load();
                return _ready;
            }
        }

        static void Load()
        {
            if (_tried) return;
            _tried = true;
            _dusk = Resources.Load<Texture2D>("UI/Menu/MegaDusk");
            _gold = Resources.Load<Texture2D>("UI/Menu/MegaGold");
            _chase = Resources.Load<Texture2D>("UI/Menu/Chase");
            _lockup = Resources.Load<Texture2D>("UI/Menu/TagLockup");
            _ready = Resources.Load<Texture2D>("UI/Menu/ReadyBurst");
        }
    }
}
