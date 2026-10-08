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
