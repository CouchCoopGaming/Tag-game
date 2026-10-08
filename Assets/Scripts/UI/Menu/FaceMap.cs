namespace Tag.Ui.Menu
{
    /// <summary>
    /// Which physical face confirms in the menu. Nintendo and Mario Kart use
    /// the east button (A on a Switch Pro pad). Xbox and PlayStation use south.
    /// A seat can override that. Gameplay jump stays the south button.
    /// </summary>
    public static class FaceMap
    {
        public const int Auto = -1;
        public const int South = 0;
        public const int East = 1;

        public static int DefaultOf(int family)
        {
            if (family == PadGlyph.Switch) return East;
            return South;
        }

        public static int Resolve(int family, int over)
        {
            if (over == South || over == East) return over;
            return DefaultOf(family);
        }

        public static void Map(int face, bool southDown, bool eastDown, out bool confirm, out bool back)
        {
            if (face == East)
            {
                confirm = eastDown;
                back = southDown;
                return;
            }
            confirm = southDown;
            back = eastDown;
        }

        public static string Word(int face)
        {
            if (face == East) return "East";
            if (face == South) return "South";
            return "Auto";
        }

        public static bool Holds()
        {
            if (DefaultOf(PadGlyph.Switch) != East) return false;
            if (DefaultOf(PadGlyph.Xbox) != South) return false;
            if (DefaultOf(PadGlyph.PlayStation) != South) return false;
            if (DefaultOf(PadGlyph.Generic) != South) return false;
            if (DefaultOf(PadGlyph.Keyboard) != South) return false;
            if (Resolve(PadGlyph.Switch, Auto) != East) return false;
            if (Resolve(PadGlyph.Xbox, Auto) != South) return false;
            if (Resolve(PadGlyph.Switch, South) != South) return false;
            if (Resolve(PadGlyph.Xbox, East) != East) return false;
            if (Resolve(PadGlyph.PlayStation, East) != East) return false;
            Map(East, true, false, out bool confirm, out bool back);
            if (confirm || !back) return false;
            Map(East, false, true, out confirm, out back);
            if (!confirm || back) return false;
            Map(South, true, false, out confirm, out back);
            if (!confirm || back) return false;
            Map(South, false, true, out confirm, out back);
            if (confirm || !back) return false;
            if (Word(Auto) != "Auto" || Word(South) != "South" || Word(East) != "East") return false;
            return true;
        }
    }
}
