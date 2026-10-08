using Tag.Couch;
using Tag.Settings;
using Tag.Ui.Menu;

namespace Tag.Ui.Hud
{
    /// <summary>
    /// Top-right of a split: the IT badge stays in the screen corner, the name
    /// sits beside it, and neither crosses the clock. Checked at 2- and 4-player
    /// and at every UI scale.
    /// </summary>
    public static class HudCorner
    {
        public const float ClockW = 260f;
        public const float ClockH = 70f;
        public const float Gap = 16f;
        public const float BadgeW = 112f;
        public const float BadgeH = 52f;
        public const float NameMax = 520f;
        public const float NameH = 132f;
        public const float CornerW = 160f;
        public const float CornerH = 80f;

        public struct Box
        {
            public float L, T, R, B;
        }

        public struct Lay
        {
            public bool Right;
            public bool Corner;
            public float BadgeX, BadgeY;
            public float NameX, NameY, NameW;
            public float VerbX, VerbY;
            public Box BadgeBox, NameBox, ClockBox;
        }

        public static Lay Measure(int humans, int split, int pane, float scale)
        {
            float s = UiFit.Clamp(scale);
            UiFit.Ref(s, out float sw, out float sh);
            CouchPlay.View v = CouchPlay.Pane(pane, humans < 1 ? 1 : humans, sw, sh, split);
            float paneL = v.X;
            float paneR = v.Right;
            float paneT = sh - v.Top;
            float paneB = paneT + v.H;
            bool touchL = paneL <= 1f;
            bool touchR = paneR >= sw - 1f;
            bool touchT = paneT <= 1f;
            bool touchB = paneB >= sh - 1f;
            bool right = paneL >= sw * 0.49f;
            float insetX = (right ? touchR : touchL) ? UiFit.SafeX : 18f;
            float insetY = touchT ? UiFit.SafeY : 14f;
            float clockL = (sw - ClockW) * 0.5f;
            float clockR = clockL + ClockW;
            float clockT = UiFit.SafeY;
            float clockB = clockT + ClockH;

            float bL;
            float bR;
            float nL;
            float nR;
            if (right)
            {
                bR = paneR - insetX;
                bL = bR - BadgeW;
                nR = bL - 10f;
                float limit = paneL + 8f;
                float cap = nR - limit;
                if (cap > NameMax) cap = NameMax;
                if (Hits(clockT, clockB, paneT + insetY, paneT + insetY + NameH))
                {
                    float room = nR - (clockR + Gap);
                    if (room < cap) cap = room;
                }
                if (cap < 1f) cap = 1f;
                nL = nR - cap;
            }
            else
            {
                bL = paneL + insetX;
                bR = bL + BadgeW;
                nL = bR + 10f;
                float limit = paneR - 8f;
                float cap = limit - nL;
                if (cap > NameMax) cap = NameMax;
                if (Hits(clockT, clockB, paneT + insetY, paneT + insetY + NameH))
                {
                    float room = (clockL - Gap) - nL;
                    if (room < cap) cap = room;
                }
                if (cap < 1f) cap = 1f;
                nR = nL + cap;
            }

            float top = paneT + insetY;
            var lay = new Lay();
            lay.Right = right;
            lay.Corner = right && touchR && touchT;
            lay.BadgeBox = new Box { L = bL, T = top, R = bR, B = top + BadgeH };
            lay.NameBox = new Box { L = nL, T = top, R = nR, B = top + NameH };
            lay.ClockBox = new Box { L = clockL, T = clockT, R = clockR, B = clockB };
            lay.NameW = nR - nL;
            lay.VerbX = touchL ? UiFit.SafeX : 18f;
            lay.VerbY = touchB ? UiFit.SafeY : 18f;
            if (right)
            {
                lay.BadgeX = -(paneR - bR);
                lay.NameX = -(paneR - nR);
            }
            else
            {
                lay.BadgeX = bL - paneL;
                lay.NameX = nL - paneL;
            }
            lay.BadgeY = -(top - paneT);
            lay.NameY = lay.BadgeY;
            return lay;
        }

        public static bool Clear(int humans, int split, float scale)
        {
            float s = UiFit.Clamp(scale);
            int n = CouchPlay.Panes(humans);
            for (int i = 0; i < n; i++)
            {
                if (humans == 3 && i == 3) continue;
                Lay lay = Measure(humans, split, i, s);
                if (lay.NameW < 40f) return false;
                if (Overlap(lay.BadgeBox, lay.NameBox)) return false;
                if (Overlap(lay.BadgeBox, lay.ClockBox)) return false;
                if (Overlap(lay.NameBox, lay.ClockBox)) return false;
                Box corner = ScreenCorner(s);
                Box badge = ToScreen(lay.BadgeBox, s);
                Box name = ToScreen(lay.NameBox, s);
                if (Overlap(name, corner)) return false;
                if (lay.Corner && !Overlap(badge, corner)) return false;
            }
            return true;
        }

        public static bool ClearAll()
        {
            float[] scales = { 0.80f, 1f, 1.30f };
            int[] humans = { 2, 4 };
            int[] splits = { GameSettings.SplitVertical, GameSettings.SplitHorizontal };
            for (int h = 0; h < humans.Length; h++)
            {
                for (int p = 0; p < splits.Length; p++)
                {
                    for (int s = 0; s < scales.Length; s++)
                    {
                        if (!Clear(humans[h], splits[p], scales[s])) return false;
                    }
                }
            }
            return true;
        }

        static Box ScreenCorner(float scale)
        {
            UiFit.Ref(scale, out float sw, out _);
            float w = CornerW / scale;
            float h = CornerH / scale;
            var box = new Box { L = sw - w, T = 0f, R = sw, B = h };
            return ToScreen(box, scale);
        }

        static Box ToScreen(Box box, float scale)
        {
            return new Box
            {
                L = box.L * scale,
                T = box.T * scale,
                R = box.R * scale,
                B = box.B * scale
            };
        }

        static bool Hits(float a0, float a1, float b0, float b1)
        {
            return a0 < b1 && b0 < a1;
        }

        static bool Overlap(Box a, Box b)
        {
            return a.L < b.R - 0.5f && b.L < a.R - 0.5f && a.T < b.B - 0.5f && b.T < a.B - 0.5f;
        }
    }
}
