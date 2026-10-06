namespace Tag.Settings
{
    /// <summary>
    /// Pause, settings, rebind, arena, the older subpanels, and the results card.
    /// Gamepad events move a cursor. Back and confirm-on-back return to the parent.
    /// Resume, Quit, and the results rows return to play.
    /// </summary>
    public enum MenuId
    {
        Play = 0,
        Pause = 1,
        Controls = 2,
        Look = 3,
        Audio = 4,
        Settings = 5,
        Rebind = 6,
        Arena = 7,
        Results = 8,
        HowToPlay = 9
    }

    public enum PadEvent
    {
        Up,
        Down,
        Left,
        Right,
        Confirm,
        Back
    }

    public struct MenuCursor
    {
        public MenuId Menu;
        public int Row;
        public MenuId Root;

        public string Key => ((int)Menu).ToString() + ":" + Row.ToString();
    }

    public static class MenuGraph
    {
        public const int PauseRows = 9;
        public const int SettingsRows = 13;
        public const int RebindRows = 14;
        public const int ArenaRows = 3;
        public const int HowToRows = 1;

        public static int Rows(MenuId menu)
        {
            switch (menu)
            {
                case MenuId.Pause: return PauseRows;
                case MenuId.Controls: return 3;
                case MenuId.Look: return 2;
                case MenuId.Audio: return 5;
                case MenuId.Settings: return SettingsRows;
                case MenuId.Rebind: return RebindRows;
                case MenuId.Arena: return ArenaRows;
                case MenuId.Results: return 2;
                case MenuId.HowToPlay: return HowToRows;
                default: return 1;
            }
        }

        public static bool Horizontal(MenuId menu)
        {
            return menu == MenuId.Pause || menu == MenuId.Results;
        }

        public static MenuCursor PauseRoot()
        {
            return new MenuCursor { Menu = MenuId.Pause, Row = 0, Root = MenuId.Pause };
        }

        public static MenuCursor ResultsRoot()
        {
            return new MenuCursor { Menu = MenuId.Results, Row = 0, Root = MenuId.Results };
        }

        public static MenuCursor Apply(MenuCursor cursor, PadEvent ev)
        {
            if (cursor.Menu == MenuId.Play)
            {
                if (ev == PadEvent.Confirm || ev == PadEvent.Back)
                    return PauseRoot();
                return cursor;
            }

            int rows = Rows(cursor.Menu);
            if (ev == PadEvent.Up || (ev == PadEvent.Left && Horizontal(cursor.Menu)))
            {
                if (cursor.Row > 0) cursor.Row--;
                return cursor;
            }
            if (ev == PadEvent.Down || (ev == PadEvent.Right && Horizontal(cursor.Menu)))
            {
                if (cursor.Row < rows - 1) cursor.Row++;
                return cursor;
            }
            if (!Horizontal(cursor.Menu) && (ev == PadEvent.Left || ev == PadEvent.Right))
                return cursor;
            if (ev == PadEvent.Back)
                return Close(cursor);
            if (ev == PadEvent.Confirm)
                return Activate(cursor);
            return cursor;
        }

        public static int NodeCount()
        {
            int n = 0;
            n += Rows(MenuId.Pause);
            n += Rows(MenuId.Controls);
            n += Rows(MenuId.Look);
            n += Rows(MenuId.Audio);
            n += Rows(MenuId.Settings);
            n += Rows(MenuId.Rebind);
            n += Rows(MenuId.Arena);
            n += Rows(MenuId.Results);
            n += Rows(MenuId.HowToPlay);
            return n;
        }

        static MenuCursor Close(MenuCursor cursor)
        {
            if (cursor.Menu == MenuId.Pause || cursor.Menu == MenuId.Results)
                return new MenuCursor { Menu = MenuId.Play, Row = 0, Root = cursor.Root };
            MenuId parent = cursor.Root == MenuId.Results ? MenuId.Results : MenuId.Pause;
            return new MenuCursor { Menu = parent, Row = ParentRow(cursor.Menu), Root = cursor.Root };
        }

        public static int ParentRow(MenuId child)
        {
            switch (child)
            {
                case MenuId.Controls: return 1;
                case MenuId.Look: return 2;
                case MenuId.Audio: return 3;
                case MenuId.Settings: return 5;
                case MenuId.Rebind: return 6;
                case MenuId.Arena: return 7;
                case MenuId.HowToPlay: return 8;
                default: return 0;
            }
        }

        static MenuCursor Activate(MenuCursor cursor)
        {
            if (cursor.Menu == MenuId.Pause)
            {
                switch (cursor.Row)
                {
                    case 0: return Play(cursor);
                    case 1: return Child(MenuId.Controls, cursor.Root);
                    case 2: return Child(MenuId.Look, cursor.Root);
                    case 3: return Child(MenuId.Audio, cursor.Root);
                    case 4: return Play(cursor);
                    case 5: return Child(MenuId.Settings, cursor.Root);
                    case 6: return Child(MenuId.Rebind, cursor.Root);
                    case 7: return Child(MenuId.Arena, cursor.Root);
                    case 8: return Child(MenuId.HowToPlay, cursor.Root);
                }
            }
            if (cursor.Menu == MenuId.Results)
                return Play(cursor);
            if (cursor.Row == Rows(cursor.Menu) - 1)
                return Close(cursor);
            return cursor;
        }

        static MenuCursor Play(MenuCursor cursor)
        {
            return new MenuCursor { Menu = MenuId.Play, Row = 0, Root = cursor.Root };
        }

        static MenuCursor Child(MenuId menu, MenuId root)
        {
            return new MenuCursor { Menu = menu, Row = 0, Root = root };
        }
    }
}
