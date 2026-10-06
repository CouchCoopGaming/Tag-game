using Tag.Settings;

namespace Tag.Onboard
{
    /// <summary>
    /// Pause card. Rules, every taught verb with the live glyph, and one arena key per registry entry.
    /// </summary>
    public static class HowToPlay
    {
        public const int Cap = 24;
        static readonly string[] Lines = new string[Cap];
        static int _count;
        static ActionBinds _from;
        static int _rev;
        static InputDeviceKind _dev;
        static int _arenas = -1;

        public static int Count => _count;

        public static string Line(int index)
        {
            if (index < 0 || index >= _count) return "";
            return Lines[index] ?? "";
        }

        public static void ResetStatics()
        {
            _count = 0;
            _from = null;
            _rev = 0;
            _dev = InputDeviceKind.Keyboard;
            _arenas = -1;
            for (int i = 0; i < Lines.Length; i++) Lines[i] = null;
        }

        public static void Ensure(InputDeviceKind device, int arenaCount)
        {
            ActionBinds binds = ActionBinds.Current;
            int rev = binds != null ? binds.Revision : 0;
            if (binds == _from && rev == _rev && device == _dev && arenaCount == _arenas && _count > 0)
                return;
            Rebuild(binds, device, arenaCount);
        }

        public static void Rebuild(ActionBinds binds, InputDeviceKind device, int arenaCount)
        {
            _from = binds;
            _rev = binds != null ? binds.Revision : 0;
            _dev = device;
            _arenas = arenaCount;
            _count = 0;
            Add("It punches to hand It off.");
            Add("Tag-back immunity 1.0 s.");
            Add("A punch that is not a tag staggers 0.25 s, then 0.50 s immunity.");
            Add(Verb("Move", PlayAction.Move, binds, device));
            Add(Verb("Sprint", PlayAction.Sprint, binds, device));
            Add(Verb("Jump", PlayAction.Jump, binds, device));
            Add(Verb("Slide", PlayAction.Slide, binds, device));
            Add(Verb("Cling hold", PlayAction.Cling, binds, device));
            Add(Verb("Wall jump", PlayAction.Jump, binds, device));
            Add(Verb("Air dash", PlayAction.AirDash, binds, device));
            Add(Verb("Punch / tag", PlayAction.Punch, binds, device));
            int n = arenaCount;
            if (n < 0) n = 0;
            if (n > 9) n = 9;
            for (int i = 0; i < n; i++)
                Add(ArenaRegistry.KeyLine(i, binds, device));
        }

        static string Verb(string name, PlayAction action, ActionBinds binds, InputDeviceKind device)
        {
            return name + "  [" + ControlGlyphs.GlyphOf(action, binds, device) + "]";
        }

        static void Add(string line)
        {
            if (_count >= Cap) return;
            Lines[_count] = line;
            _count++;
        }
    }
}
