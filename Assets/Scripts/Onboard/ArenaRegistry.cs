using Tag.Settings;

namespace Tag.Onboard
{
    /// <summary>
    /// Pause-card arena keys. Names and the count come from <see cref="Tag.Level.ParkArena"/>,
    /// so key 1 is Mega Park, key 2 is Pocket Park, and key 3 is Stack Yard.
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

        public static int Count => Tag.Level.ParkArena.Count;

        static readonly string[] Digits = { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        static readonly PlayAction[] Keys =
        {
            PlayAction.Arena1,
            PlayAction.Arena2,
            PlayAction.Arena3
        };

        /// <summary>
        /// Match setup reads this list. Index 0 is Mega Park, the default.
        /// Names come from <see cref="Tag.Level.ParkArena"/> so the card cannot drift.
        /// </summary>
        public static readonly Entry[] All = Make();

        static Entry[] Make()
        {
            int n = Tag.Level.ParkArena.Count;
            var all = new Entry[n];
            for (int i = 0; i < n; i++)
                all[i] = At(i);
            return all;
        }

        public static Entry At(int index)
        {
            var entry = new Entry();
            if (index < 0 || index >= Count)
            {
                entry.Name = "Arena";
                entry.HasBind = false;
                return entry;
            }
            entry.Name = Tag.Level.ParkArena.NameOf(index);
            entry.Root = "MegaPark";
            entry.Key = index < Keys.Length ? Keys[index] : PlayAction.Arena1;
            entry.HasBind = index < Keys.Length;
            return entry;
        }

        public static string Digit(int index)
        {
            if (index < 0) return Digits[0];
            if (index >= Digits.Length) return Digits[Digits.Length - 1];
            return Digits[index];
        }

        public static string KeyLine(int index, ActionBinds binds, InputDeviceKind device)
        {
            Entry entry = At(index);
            string key = entry.HasBind
                ? ControlGlyphs.GlyphOf(entry.Key, binds, device)
                : Digit(index);
            return key + "  " + entry.Name;
        }
    }
}
