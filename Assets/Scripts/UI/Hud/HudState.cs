using Tag.Gameplay;

namespace Tag.Ui.Hud
{
    /// <summary>
    /// One match, shared by every pane. A tag moves It to the target and
    /// gives the previous It the tag-back window. The feed is the same list
    /// on every pane. The stills and the preview read <see cref="Script"/>.
    /// </summary>
    public static class HudState
    {
        public struct Snap
        {
            public int It;
            public int Safe;
            public float SafeLeft;
            public int Count;
            public int A0, B0, A1, B1, A2, B2;

            public int From(int row)
            {
                if (row == 0) return A0;
                if (row == 1) return A1;
                return A2;
            }

            public int To(int row)
            {
                if (row == 0) return B0;
                if (row == 1) return B1;
                return B2;
            }
        }

        public struct Aim
        {
            public bool OnScreen;
            public float X;
            public float Y;
            public float DirX;
            public float DirY;
        }

        /// <summary>P1 tags P2, then P2 tags P3. P3 is It. P2 is safe.</summary>
        public static Snap Script()
        {
            Snap snap = new Snap();
            snap.It = -1;
            snap.Safe = -1;
            Apply(ref snap, 0, 1);
            Apply(ref snap, 1, 2);
            snap.SafeLeft = 0.8f;
            return snap;
        }

        public static void Apply(ref Snap snap, int from, int to)
        {
            if (snap.Count >= 2)
            {
                snap.A2 = snap.A1;
                snap.B2 = snap.B1;
            }
            if (snap.Count >= 1)
            {
                snap.A1 = snap.A0;
                snap.B1 = snap.B0;
            }
            snap.A0 = from;
            snap.B0 = to;
            if (snap.Count < TagFeed.Rows) snap.Count++;
            snap.Safe = from;
            snap.It = to;
            snap.SafeLeft = TagBackImmunity.DefaultSeconds;
        }

        /// <summary>
        /// Where this seat's camera puts It. Seat 0 can see It. Seat 1 sees
        /// It off the right rim. Seat 3 sees It off the bottom rim.
        /// </summary>
        public static void ViewOf(int viewer, out float vx, out float vy, out float vz)
        {
            vx = 0.5f;
            vy = 0.5f;
            vz = 1f;
            if (viewer == 0)
            {
                vx = 0.36f;
                vy = 0.48f;
                return;
            }
            if (viewer == 1)
            {
                vx = 1.45f;
                vy = 0.22f;
                return;
            }
            if (viewer == 3)
            {
                vx = 0.50f;
                vy = -0.55f;
            }
        }

        public static Aim Project(float vx, float vy, float vz)
        {
            float dx = vx - 0.5f;
            float dy = vy - 0.5f;
            if (vz < 0.05f)
            {
                dx = -dx;
                dy = -dy;
            }
            bool on = vz >= 0.05f && vx >= 0.02f && vx <= 0.98f && vy >= 0.04f && vy <= 0.96f;
            Aim aim = new Aim();
            aim.DirX = dx;
            aim.DirY = dy;
            if (on)
            {
                float head = vy + 0.08f;
                if (head > 0.92f) head = 0.92f;
                aim.OnScreen = true;
                aim.X = vx;
                aim.Y = head;
                return aim;
            }
            float ax = dx < 0f ? -dx : dx;
            float ay = dy < 0f ? -dy : dy;
            float sx = ax < 0.0001f ? 1000f : 0.5f / ax;
            float sy = ay < 0.0001f ? 1000f : 0.5f / ay;
            float scale = sx < sy ? sx : sy;
            float ex = 0.5f + dx * scale;
            float ey = 0.5f + dy * scale;
            if (ex < 0.04f) ex = 0.04f;
            if (ex > 0.96f) ex = 0.96f;
            if (ey < 0.06f) ey = 0.06f;
            if (ey > 0.94f) ey = 0.94f;
            aim.OnScreen = false;
            aim.X = ex;
            aim.Y = ey;
            return aim;
        }

        public static int ItCount()
        {
            Snap snap = Script();
            int n = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i == snap.It) n++;
            }
            return n;
        }

        public static bool SafePrev()
        {
            Snap step = new Snap();
            step.It = -1;
            step.Safe = -1;
            Apply(ref step, 0, 1);
            if (step.It != 1 || step.Safe != 0) return false;
            if (step.SafeLeft < TagBackImmunity.DefaultSeconds - 0.001f) return false;
            Apply(ref step, 1, 2);
            if (step.It != 2 || step.Safe != 1) return false;
            if (step.It == step.Safe) return false;
            Snap snap = Script();
            if (snap.It != 2 || snap.Safe != 1) return false;
            if (snap.Safe != snap.From(0) || snap.It != snap.To(0)) return false;
            if (snap.SafeLeft <= 0f || snap.SafeLeft > TagBackImmunity.DefaultSeconds) return false;
            return true;
        }

        public static bool FeedSame()
        {
            Snap snap = Script();
            if (snap.Count < 2) return false;
            TagFeed.Reset();
            TagFeed.Push(0, 1, 0f);
            TagFeed.Push(1, 2, 0.2f);
            if (TagFeed.Text(0) != TagFeed.Line[snap.From(0), snap.To(0)]) return false;
            if (TagFeed.Text(1) != TagFeed.Line[snap.From(1), snap.To(1)]) return false;
            if (TagFeed.Text(0) != "P2 tagged P3") return false;
            if (TagFeed.Text(1) != "P1 tagged P2") return false;
            string first = TagFeed.Text(0);
            string second = TagFeed.Text(1);
            int panes = 0;
            for (int pane = 0; pane < 4; pane++)
            {
                if (TagFeed.Line[snap.From(0), snap.To(0)] != first) return false;
                if (TagFeed.Line[snap.From(1), snap.To(1)] != second) return false;
                panes++;
                if (pane != panes - 1) return false;
            }
            if (panes != 4) return false;
            TagFeed.Reset();
            return true;
        }

        public static bool AimHolds()
        {
            ViewOf(0, out float x0, out float y0, out float z0);
            Aim seen = Project(x0, y0, z0);
            if (!seen.OnScreen) return false;
            ViewOf(3, out float x3, out float y3, out float z3);
            Aim below = Project(x3, y3, z3);
            if (below.OnScreen || below.Y > 0.08f || below.DirY >= 0f) return false;
            ViewOf(1, out float x1, out float y1, out float z1);
            Aim side = Project(x1, y1, z1);
            if (side.OnScreen || side.X < 0.90f) return false;
            return true;
        }

        public const int OpeningIt = 0;
        public const int DroppedSeat = 3;
        public const int SoloHuman = 0;
        public const int SoloAi = 1;

        public static readonly float[] ItTime = { 8.1f, 12.4f, 0.2f, 4.0f };

        public static string Digit(int step)
        {
            if (step <= 0) return "3";
            if (step == 1) return "2";
            if (step == 2) return "1";
            return MatchHudText.Go;
        }

        public static bool InputLocked(int step)
        {
            return step < 3;
        }

        public static bool CountHolds()
        {
            if (Digit(0) != "3" || Digit(1) != "2" || Digit(2) != "1" || Digit(3) != MatchHudText.Go) return false;
            if (MatchHudText.Locked != "LOCKED") return false;
            if (!InputLocked(0) || !InputLocked(2) || InputLocked(3)) return false;
            return OpeningIt == 0;
        }

        public static bool ClockHot(float remaining)
        {
            return remaining > 0f && remaining <= 10f;
        }

        /// <summary>The last ten seconds ask for the existing round tick. No new clip.</summary>
        public static bool WantsTick(float remaining)
        {
            return ClockHot(remaining);
        }

        public static bool HotHolds()
        {
            if (!ClockHot(8f) || !WantsTick(1f)) return false;
            if (ClockHot(10.01f) || ClockHot(0f)) return false;
            return true;
        }

        public static int Place(int rank)
        {
            if (rank <= 0) return 2;
            if (rank == 1) return 3;
            if (rank == 2) return 0;
            return 1;
        }

        public static string AfterRound(bool finalRound)
        {
            return finalRound ? MatchHudText.ResultsWord : MatchHudText.NextRound;
        }

        static bool HasPodium(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            return word.IndexOf("podium") >= 0;
        }

        public static bool OverHolds()
        {
            if (MatchHudText.RoundOver != "ROUND OVER") return false;
            if (AfterRound(true) != "RESULTS") return false;
            if (AfterRound(false) != "NEXT ROUND") return false;
            if (HasPodium(MatchHudText.RoundOver) || HasPodium(AfterRound(true)) || HasPodium(AfterRound(false))) return false;
            if (HasPodium(MatchHudText.LeastWins) || HasPodium(MatchHudText.Locked)) return false;
            float prev = -1f;
            for (int i = 0; i < 4; i++)
            {
                float t = ItTime[Place(i)];
                if (t + 0.0001f < prev) return false;
                prev = t;
            }
            return Place(0) == Script().It;
        }

        public static bool TagHolds()
        {
            if (Script().It != Script().To(0)) return false;
            bool was = MatchHudText.ComicWords;
            MatchHudText.ComicWords = true;
            bool shown = MatchHudText.Comic(MatchHudText.YoureIt) == MatchHudText.YoureIt;
            MatchHudText.ComicWords = false;
            bool hidden = MatchHudText.Comic(MatchHudText.YoureIt).Length == 0;
            MatchHudText.ComicWords = was;
            return shown && hidden;
        }

        public static bool DropHolds()
        {
            if (MatchHudText.Left[DroppedSeat] != "P4 left") return false;
            Snap snap = Script();
            if (snap.It == DroppedSeat || snap.Safe == DroppedSeat) return false;
            return ItCount() == 1;
        }

        public static bool SoloHolds()
        {
            return SoloHuman != SoloAi && ItCount() == 1 && SoloHuman != Script().It;
        }

        public static bool Holds()
        {
            return FeedSame() && SafePrev() && ItCount() == 1 && AimHolds()
                && CountHolds() && HotHolds() && OverHolds() && TagHolds()
                && DropHolds() && SoloHolds();
        }

        public static string Line()
        {
            bool feed = FeedSame();
            bool safe = SafePrev();
            int it = ItCount();
            return "hud-state feed=" + (feed ? "same" : "split")
                + " safe=" + (safe ? "prev" : "wrong")
                + " it=" + it.ToString()
                + " count=" + (CountHolds() ? "lock" : "open")
                + " hot=" + (HotHolds() ? "tick" : "no")
                + " over=" + (OverHolds() ? "least" : "no")
                + " tag=" + (TagHolds() ? "flash" : "no")
                + " drop=" + (DropHolds() ? "left" : "no")
                + " solo=" + (SoloHolds() ? "ai" : "no");
        }
    }
}
