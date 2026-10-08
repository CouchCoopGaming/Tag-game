using Tag.Couch;
using Tag.Settings;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// One pause card per human pane. Solo play keeps a single card.
    /// The score pane of a three-player layout is not a player.
    /// </summary>
    public static class MenuSplitPause
    {
        public const int Items = 4;

        public static readonly string[] Item =
        {
            "Resume",
            "Restart round",
            "Options",
            "Quit to menu"
        };

        public static readonly string[] Blurb =
        {
            "",
            "Same arena, same rules",
            "",
            ""
        };

        public struct Card
        {
            public int Seat;
            public float X;
            public float Y;
            public float W;
            public float H;
            public bool Show;
        }

        public static int Preview;

        public static int Fill(Card[] cards)
        {
            if (cards == null || cards.Length < 1) return 0;
            for (int i = 0; i < cards.Length; i++) cards[i].Show = false;
            int humans = Preview > 0 ? Preview : CouchPlay.Humans;
            if (humans < 1) humans = 1;
            if (humans > 4) humans = 4;
            int split = GameSettings.SplitVertical;
            if (GameSettings.Current != null) split = GameSettings.Current.SplitAxis;
            UiFit.Ref(UiFit.Current(), out float cw, out float ch);
            float bodyW = cw - UiFit.SafeX * 2f;
            float bodyH = ch - UiFit.ChromeTop - UiFit.ChromeBot;
            if (humans < 2)
            {
                cards[0].Seat = 0;
                cards[0].W = bodyW * 0.48f;
                if (cards[0].W > 820f) cards[0].W = 820f;
                cards[0].H = bodyH * 0.78f;
                if (cards[0].H > 620f) cards[0].H = 620f;
                cards[0].X = (bodyW - cards[0].W) * 0.5f;
                cards[0].Y = (bodyH - cards[0].H) * 0.35f;
                cards[0].Show = true;
                return 1;
            }
            int panes = CouchPlay.Panes(humans);
            int n = 0;
            for (int i = 0; i < panes && n < cards.Length; i++)
            {
                CouchPlay.View view = CouchPlay.Pane(i, humans, 1f, 1f, split);
                if (view.Score) continue;
                Card card = Overlap(view.X, view.Y, view.W, view.H);
                card.Seat = i;
                card.Show = card.W > 80f && card.H > 80f;
                cards[n] = card;
                n++;
            }
            return n;
        }

        public static string SeatLabel(int seat, bool preview)
        {
            if (!preview && seat >= 0 && seat < CouchPlay.Max)
            {
                string who = CouchPlay.Name(seat);
                if (!string.IsNullOrEmpty(who)) return who;
            }
            int n = seat + 1;
            if (n < 1) n = 1;
            if (n > 4) n = 4;
            if (n == 1) return "P1";
            if (n == 2) return "P2";
            if (n == 3) return "P3";
            return "P4";
        }

        static Card Overlap(float nx, float ny, float nw, float nh)
        {
            UiFit.Ref(UiFit.Current(), out float canvasW, out float canvasH);
            float sx0 = nx * canvasW;
            float sy0 = ny * canvasH;
            float sx1 = sx0 + nw * canvasW;
            float sy1 = sy0 + nh * canvasH;
            float bx0 = UiFit.SafeX;
            float by0 = UiFit.ChromeBot;
            float bx1 = canvasW - UiFit.SafeX;
            float by1 = canvasH - UiFit.ChromeTop;
            float ix0 = sx0 > bx0 ? sx0 : bx0;
            float iy0 = sy0 > by0 ? sy0 : by0;
            float ix1 = sx1 < bx1 ? sx1 : bx1;
            float iy1 = sy1 < by1 ? sy1 : by1;
            Card card = new Card();
            card.X = ix0 - bx0;
            card.W = ix1 - ix0;
            card.H = iy1 - iy0;
            card.Y = by1 - iy1;
            return card;
        }
    }
}
