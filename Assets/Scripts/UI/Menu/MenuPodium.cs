using Tag.Couch;
using Tag.Gameplay;
using Tag.MatchStats;
using Tag.Modes;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Results order follows the mode that just ended.
    /// Hot Potato: round wins, first to 2.
    /// Least It: round wins, then least time as It.
    /// Trail Tag: last standing.
    /// Free play: no winner. Plates list tags.
    /// </summary>
    public static class MenuPodium
    {
        public struct Row
        {
            public string Name;
            public float Time;
            public int Tags;
            public int Punches;
            public float Untagged;
            public int Wins;
            public bool Alive;
            public bool Winner;
            public int Hier;
            public int Accent;
            public int Hat;
            public int Seat;
            public float Chase;
            public string ChaseName;
        }

        public static int Fill(Row[] rows)
        {
            if (rows == null) return 0;
            int n = 0;
            TagModeController mode = TagModeController.Instance;
            if (mode != null && mode.PlayersForHud != null)
            {
                int count = mode.PlayersForHud.Count;
                for (int i = 0; i < count && n < rows.Length; i++)
                {
                    ItController pawn = mode.PlayersForHud[i];
                    if (pawn == null) continue;
                    rows[n] = FromPawn(mode, pawn);
                    n++;
                }
            }
            if (n == 0 && MatchBook.Sealed && MatchBook.Count > 0)
            {
                n = MatchBook.Count;
                if (n > rows.Length) n = rows.Length;
                for (int i = 0; i < n; i++)
                {
                    Row row = new Row();
                    row.Name = string.IsNullOrEmpty(MatchBook.Name[i]) ? "Player" : MatchBook.Name[i];
                    row.Time = MatchBook.TimeAsIt[i];
                    row.Tags = MatchBook.TagsMade[i];
                    row.Punches = MatchBook.PunchesLanded[i];
                    row.Untagged = MatchBook.LongestSurvival[i];
                    row.Hier = 4;
                    row.Accent = i;
                    row.Seat = -1;
                    for (int s = 0; s < CouchPlay.Max; s++)
                    {
                        if (CouchPlay.Name(s) != row.Name) continue;
                        row.Seat = s;
                        row.Hier = MenuSession.Hier[s];
                        row.Accent = MenuSession.Accent[s];
                        row.Hat = MenuSession.Hat[s];
                        break;
                    }
                    rows[i] = row;
                }
            }
            TagModeId id = mode != null ? mode.SelectedMode : MenuSession.Mode;
            Sort(rows, n, id);
            float chase = mode != null ? mode.LongestChase : 0f;
            string chaseName = mode != null ? mode.LongestChaseName : "";
            for (int i = 0; i < n; i++)
            {
                Row row = rows[i];
                row.Chase = chase;
                row.ChaseName = i == 0 ? chaseName : "";
                rows[i] = row;
            }
            return n;
        }

        /// <summary>
        /// One results line. Names who led the mode, and who ran the longest chase.
        /// </summary>
        public static string Summary(TagModeId id, Row[] rows, int n, float seconds, string who)
        {
            string lead = Lead(id, rows, n);
            string chase = "longest chase " + seconds.ToString("0.0") + "s";
            if (!string.IsNullOrEmpty(who)) chase = chase + " (" + who + ")";
            if (lead.Length == 0) return chase;
            return lead + " · " + chase;
        }

        static string Lead(TagModeId id, Row[] rows, int n)
        {
            if (rows == null || n <= 0) return MenuCatalog.ModeName(id);
            string name = rows[0].Name;
            if (string.IsNullOrEmpty(name)) name = "P1";
            if (id == TagModeId.HotPotato) return name + " won the rounds";
            if (id == TagModeId.TrailTag) return name + " was last standing";
            if (id == TagModeId.FreePlay) return name + " made the most tags";
            return name + " was It the least";
        }

        public static string Detail(TagModeId id, Row row)
        {
            if (id == TagModeId.HotPotato)
                return row.Wins.ToString() + " round wins";
            if (id == TagModeId.TrailTag)
                return row.Alive ? "Last standing" : "Out";
            if (id == TagModeId.FreePlay)
                return row.Tags.ToString() + " tags";
            return row.Time.ToString("0.0") + "s as It";
        }

        public static string Stats(Row row)
        {
            string time = row.Time.ToString("0.0") + "s as It";
            string tags = row.Tags == 1 ? "1 tag" : row.Tags.ToString() + " tags";
            string punches = row.Punches == 1 ? "1 punch" : row.Punches.ToString() + " punches";
            string free = row.Untagged.ToString("0.0") + "s untagged";
            return time + "\n" + tags + "\n" + punches + "\n" + free;
        }

        public static string ChaseLine(float seconds, string who)
        {
            string line = "Longest chase  " + seconds.ToString("0.0") + "s";
            if (string.IsNullOrEmpty(who)) return line;
            return line + "  " + who;
        }

        public static int Sample(Row[] rows)
        {
            if (rows == null || rows.Length < 1) return 0;
            int n = rows.Length < 4 ? rows.Length : 4;
            for (int i = 0; i < n; i++)
            {
                Row row = new Row();
                row.Name = i == 0 ? "P1" : i == 1 ? "P2" : i == 2 ? "P3" : "P4";
                row.Tags = 6 - i;
                row.Time = 8.5f + i * 6.2f;
                row.Punches = 4 - i;
                row.Untagged = 18f - i * 4.5f;
                row.Wins = i == 0 ? 2 : i == 1 ? 1 : 0;
                row.Alive = i < 2;
                row.Winner = i == 0;
                int[] body = { 5, 0, 2, 3 };
                int[] accent = { 4, 1, 4, 1 };
                row.Hier = body[i];
                row.Accent = accent[i];
                row.Seat = i;
                row.Chase = 14.2f;
                row.ChaseName = i == 0 ? "P2" : "";
                rows[i] = row;
            }
            return n;
        }

        static Row FromPawn(TagModeController mode, ItController pawn)
        {
            Row row = new Row();
            row.Name = string.IsNullOrEmpty(pawn.PlayerId) ? "Player" : pawn.PlayerId;
            row.Time = pawn.TimeAsIt;
            row.Tags = pawn.TagsLanded;
            ReadBook(ref row);
            row.Alive = pawn.IsAlive;
            row.Wins = mode.RoundWinsOf(row.Name);
            row.Winner = mode.WinnerNamed(row.Name);
            row.Hier = 4;
            row.Accent = 0;
            row.Hat = 0;
            row.Seat = -1;
            for (int s = 0; s < CouchPlay.Max; s++)
            {
                if (CouchPlay.Name(s) != row.Name) continue;
                row.Hier = MenuSession.Hier[s];
                row.Accent = MenuSession.Accent[s];
                row.Hat = MenuSession.Hat[s];
                row.Seat = s;
                return row;
            }
            int h = 0;
            for (int i = 0; i < row.Name.Length; i++) h = h * 31 + row.Name[i];
            if (h < 0) h = -h;
            int colors = LocalProfilesLength();
            if (colors > 0) row.Hier = h % colors;
            row.Accent = (row.Hier + 1) % (colors > 0 ? colors : 1);
            return row;
        }

        static void ReadBook(ref Row row)
        {
            int i = BookOf(row.Name);
            if (i < 0) return;
            row.Punches = MatchBook.PunchesLanded[i];
            row.Untagged = MatchBook.LongestSurvival[i];
            if (!MatchBook.Sealed) return;
            row.Time = MatchBook.TimeAsIt[i];
            row.Tags = MatchBook.TagsMade[i];
        }

        static int BookOf(string name)
        {
            if (string.IsNullOrEmpty(name) || MatchBook.Count <= 0) return -1;
            int n = MatchBook.Count;
            if (n > MatchBook.Cap) n = MatchBook.Cap;
            for (int i = 0; i < n; i++)
            {
                if (MatchBook.Name[i] == name) return i;
            }
            return -1;
        }

        static int LocalProfilesLength()
        {
            return Tag.Profiles.LocalProfiles.HierNames.Length;
        }

        static void Sort(Row[] rows, int n, TagModeId id)
        {
            for (int a = 0; a < n; a++)
            {
                for (int b = a + 1; b < n; b++)
                {
                    if (!Better(rows[b], rows[a], id)) continue;
                    Row tmp = rows[a];
                    rows[a] = rows[b];
                    rows[b] = tmp;
                }
            }
        }

        static bool Better(Row a, Row b, TagModeId id)
        {
            if (a.Winner != b.Winner) return a.Winner;
            if (id == TagModeId.HotPotato)
            {
                if (a.Wins != b.Wins) return a.Wins > b.Wins;
                return false;
            }
            if (id == TagModeId.TrailTag)
            {
                if (a.Alive != b.Alive) return a.Alive;
                return false;
            }
            if (id == TagModeId.FreePlay)
                return a.Tags > b.Tags;
            if (id == TagModeId.LeastIt)
            {
                if (a.Wins != b.Wins) return a.Wins > b.Wins;
                return a.Time < b.Time;
            }
            return a.Time < b.Time;
        }
    }
}
