using Tag.Level;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Baked menu art. Mega Park's bright plate is the graded yard: neutral
    /// white balance, an S-curve, and a saturation lift. The lockup, the chase,
    /// and the ready burst match the comic wordmarks. Loaded once.
    /// The live character view stays the Hier preview.
    /// </summary>
    public static class MenuBackdrop
    {
        static Texture2D _dusk;
        static Texture2D _gold;
        static Texture2D _grade;
        static Texture2D _soft;
        static Texture2D _chase;
        static Texture2D _idle;
        static Texture2D _lockup;
        static Texture2D _ready;
        static bool _tried;

        public static Texture Fly(int arena)
        {
            Load();
            if (arena == ParkArena.Mega && _dusk != null) return _dusk;
            return MenuArenaArt.Thumb(arena);
        }

        /// <summary>
        /// The graded Mega Park plate. Loading and pause use this so the yard
        /// matches the neutral, sunny stills. Mega Gold stays the ungraded source.
        /// </summary>
        public static Texture Bright(int arena)
        {
            Load();
            if (arena == ParkArena.Mega)
            {
                if (_grade != null) return _grade;
                if (_gold != null) return _gold;
            }
            return Fly(arena);
        }

        /// <summary>
        /// Title plate only. MegaGrade, blurred a little so the blocks fall back
        /// and the logo carries the frame. Loading and pause stay on the sharp grade.
        /// </summary>
        public static Texture Soft
        {
            get
            {
                Load();
                return _soft != null ? _soft : _grade;
            }
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

        /// <summary>Hier idle pair. Blender render of the seat meshes, feet on the discs.</summary>
        public static Texture2D SeatIdle
        {
            get
            {
                Load();
                return _idle;
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
            _grade = Resources.Load<Texture2D>("UI/Menu/MegaGrade");
            _soft = Resources.Load<Texture2D>("UI/Menu/MegaBlur");
            _chase = Resources.Load<Texture2D>("UI/Menu/Chase");
            _idle = Resources.Load<Texture2D>("UI/Menu/SeatIdle");
            _lockup = Resources.Load<Texture2D>("UI/Menu/TagLockup");
            _ready = Resources.Load<Texture2D>("UI/Menu/ReadyBurst");
        }
    }
}
