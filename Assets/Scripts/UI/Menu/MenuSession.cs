using System;
using System.Globalization;
using System.Text;
using Tag.Couch;
using Tag.Modes;
using Tag.Profiles;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Picks for the open lobby. Skins write through LocalProfiles when a seat
    /// has a profile. Feel locks are not stored here.
    /// </summary>
    public static class MenuSession
    {
        public static TagModeId Mode = TagModeId.LeastIt;
        public static int Arena;
        public static bool RandomArena;
        public static readonly int[] Hier = { 4, 4, 4, 4 };
        public static readonly int[] Accent = { 0, 1, 2, 3 };
        public static readonly int[] Hat = new int[4];
        public static readonly bool[] Ready = new bool[4];
        public static readonly int[] Cursor = { 4, 4, 4, 4 };
        public static readonly int[] Bound = { -1, -1, -1, -1 };

        public static void ClearReady()
        {
            for (int i = 0; i < 4; i++)
                Ready[i] = false;
        }

        public static void Reset()
        {
            Mode = TagModeId.LeastIt;
            Arena = 0;
            RandomArena = false;
            ClearReady();
            for (int i = 0; i < 4; i++)
            {
                Hier[i] = 4;
                Accent[i] = i;
                Hat[i] = 0;
                Cursor[i] = 4;
                Bound[i] = -1;
            }
        }

        public static bool IsKey(string key)
        {
            if (key == "mode" || key == "randomArena") return true;
            return SeatField(key, out _, out _);
        }

        public static void Write(StringBuilder text)
        {
            if (text == null) return;
            text.Append("mode=");
            text.Append(((int)Mode).ToString(CultureInfo.InvariantCulture));
            text.Append('\n');
            text.Append("randomArena=");
            text.Append(RandomArena ? '1' : '0');
            text.Append('\n');
            for (int i = 0; i < 4; i++)
            {
                string n = i.ToString(CultureInfo.InvariantCulture);
                IntLine(text, "seat" + n + ".hier", Hier[i]);
                IntLine(text, "seat" + n + ".acc", Accent[i]);
                IntLine(text, "seat" + n + ".hat", Hat[i]);
            }
        }

        public static bool ApplyKey(string key, string value)
        {
            if (key == "mode")
            {
                int id;
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                    return true;
                if (id < 0 || id > (int)TagModeId.FreePlay) id = (int)TagModeId.LeastIt;
                Mode = (TagModeId)id;
                return true;
            }
            if (key == "randomArena")
            {
                RandomArena = value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                return true;
            }
            if (!SeatField(key, out int seat, out int field)) return false;
            int n;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                return true;
            if (field == 0)
            {
                if (n < 0) n = 0;
                if (n > 5) n = 5;
                Hier[seat] = n;
                Cursor[seat] = n;
            }
            else if (field == 1)
            {
                if (n < 0) n = 0;
                if (n > 5) n = 5;
                Accent[seat] = n;
            }
            else
                Hat[seat] = n == 0 ? 0 : 1;
            return true;
        }

        static bool SeatField(string key, out int seat, out int field)
        {
            seat = 0;
            field = 0;
            if (string.IsNullOrEmpty(key) || key.Length < 9) return false;
            if (!key.StartsWith("seat", StringComparison.Ordinal)) return false;
            if (key[4] < '0' || key[4] > '3' || key[5] != '.') return false;
            seat = key[4] - '0';
            string tail = key.Substring(6);
            if (tail == "hier") { field = 0; return true; }
            if (tail == "acc") { field = 1; return true; }
            if (tail == "hat") { field = 2; return true; }
            return false;
        }

        static void IntLine(StringBuilder text, string key, int value)
        {
            text.Append(key);
            text.Append('=');
            text.Append(value.ToString(CultureInfo.InvariantCulture));
            text.Append('\n');
        }

        public static void PullLook(int seat)
        {
            if (seat < 0 || seat >= 4) return;
            int id = LocalProfiles.ProfileAt(seat);
            if (id <= 0) return;
            string body = LocalProfiles.HierKey(id);
            string accent = LocalProfiles.AccentKey(id);
            for (int i = 0; i < LocalProfiles.HierNames.Length; i++)
            {
                if (LocalProfiles.HierNames[i] == body)
                {
                    Hier[seat] = i;
                    Cursor[seat] = i;
                }
                if (LocalProfiles.HierNames[i] == accent)
                    Accent[seat] = i;
            }
            Hat[seat] = LocalProfiles.HatOf(id);
        }

        public static void CommitLooks()
        {
            for (int s = 0; s < CouchPlay.Max; s++)
            {
                if (!CouchPlay.HumanAt(s)) continue;
                int id = LocalProfiles.ProfileAt(s);
                if (id <= 0)
                {
                    id = Bound[s];
                    if (id <= 0 || LocalProfiles.NameOf(id).Length == 0)
                        id = LocalProfiles.Create("P" + (s + 1).ToString());
                    Bound[s] = id;
                    if (id > 0) LocalProfiles.TrySeat(s, id);
                }
                if (id > 0)
                    LocalProfiles.SetLook(id, Hier[s], Accent[s], Hat[s]);
            }
        }

        public static bool AllReady()
        {
            int n = 0;
            for (int s = 0; s < CouchPlay.Max; s++)
            {
                if (!CouchPlay.HumanAt(s)) continue;
                n++;
                if (!Ready[s]) return false;
            }
            return n > 0;
        }
    }
}
