using Tag.Settings;

namespace Tag.Onboard
{
    /// <summary>
    /// Arenas the pause picker already saves. How-to-play reads Count from here.
    /// A third entry would show key 3. The card does not hardcode the list.
    /// </summary>
    public static class ArenaRegistry
    {
        public struct Entry
        {
            public string Name;
            public string Root;
            public PlayAction Key;
            public bool HasBind;
        }

        public static readonly Entry[] All =
        {
            new Entry { Name = "PARK", Root = "PARK", Key = PlayAction.Arena1, HasBind = true },
            new Entry { Name = "Mega Park", Root = "MegaPark", Key = PlayAction.Arena2, HasBind = true }
        };

        public static int Count => All.Length;

        static readonly string[] Digits = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        public static string Digit(int index)
        {
            if (index < 0) return Digits[0];
            if (index >= Digits.Length) return Digits[Digits.Length - 1];
            return Digits[index];
        }

        public static string KeyLine(int index, ActionBinds binds, InputDeviceKind device)
        {
            string name;
            string key;
            if (index >= 0 && index < All.Length)
            {
                name = All[index].Name;
                key = All[index].HasBind
                    ? ControlGlyphs.GlyphOf(All[index].Key, binds, device)
                    : Digit(index);
            }
            else
            {
                name = "Arena";
                key = Digit(index);
            }
            return key + "  " + name;
        }
    }
}
