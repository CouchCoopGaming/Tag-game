namespace Tag.Ui.Menu
{
    /// <summary>
    /// Contrast and fit for the couch screens. Body text needs 4.5:1.
    /// A focused row uses ink on the hot plate, and a gold ring on navy.
    /// At 130% every listed block stays inside the safe body.
    /// </summary>
    public static class UiSweep
    {
        public const float SeatMix = 0.40f;
        public const float PortraitMix = 0.06f;

        public static bool Holds()
        {
            if (Ratio(1f, 0.98f, 0.92f, 0.08f, 0.32f, 0.86f) < 4.5f) return false;
            if (Ratio(1f, 0.98f, 0.92f, 0.06f, 0.16f, 0.40f) < 4.5f) return false;
            if (Ratio(0.78f, 0.88f, 1f, 0.08f, 0.32f, 0.86f) < 4.5f) return false;
            if (Ratio(1f, 0.84f, 0.12f, 0.06f, 0.16f, 0.40f) < 4.5f) return false;
            if (Ratio(0.04f, 0.07f, 0.16f, 0.20f, 0.58f, 1f) < 4.5f) return false;
            if (Ratio(1f, 0.84f, 0.12f, 0.06f, 0.16f, 0.40f) < 3f) return false;
            if (!SeatText()) return false;
            if (!PortraitText()) return false;
            if (!Fits(0.80f) || !Fits(1f) || !Fits(1.30f)) return false;
            return true;
        }

        public static bool Fits(float scale)
        {
            float bodyH = UiFit.BodyH(scale);
            float bodyW = UiFit.BodyW(scale);
            if (bodyH < 400f || bodyW < 900f) return false;
            int opt = UiFit.Window(scale, 96f, 8f);
            if (8f + opt * 96f > bodyH + 0.5f) return false;
            int rules = UiFit.Window(scale, 84f, 12f);
            if (12f + rules * 84f > bodyH + 0.5f) return false;
            UiFit.Columns(scale, out _, out float leftW, out float rightX, out float rightW);
            if (leftW < 280f || rightW < 520f) return false;
            if (rightX + rightW > bodyW + 0.5f) return false;
            UiFit.MainSplit(scale, out float logoW, out float tileX, out float tileW);
            if (logoW + 24f > tileX) return false;
            if (tileX + tileW > bodyW + 0.5f) return false;
            if (8f + 4f * 108f + 96f > bodyH + 0.5f) return false;
            UiFit.ArenaSplit(scale, out float listW, out float shotX, out float shotW, out float row, out float step);
            if (12f + 4f * step + row > bodyH + 0.5f) return false;
            if (shotX + shotW > bodyW + 0.5f || listW < 280f) return false;
            UiFit.Bands(scale, out float view, out float rankY, out float rankH, out float btnY, out float btnH);
            if (btnY + btnH > bodyH + 0.5f || view < 160f || rankH < 72f) return false;
            UiFit.CastBands(scale, out float cardH, out float gridTop, out float gridH, out float gridStep);
            if (gridTop + gridStep + gridH > bodyH + 0.5f) return false;
            if (cardH < 180f || gridH < 64f) return false;
            float line = 24f * 16f;
            if (line > rightW || line > tileW) return false;
            return true;
        }

        static bool SeatText()
        {
            float[] r = { 0.95f, 0.16f, 0.94f, 0.62f };
            float[] g = { 0.16f, 0.45f, 0.42f, 0.32f };
            float[] b = { 0.22f, 1f, 0.14f, 0.86f };
            for (int i = 0; i < 4; i++)
            {
                float pr = Mix(0.04f, r[i]);
                float pg = Mix(0.07f, g[i]);
                float pb = Mix(0.16f, b[i]);
                if (Ratio(1f, 0.98f, 0.92f, pr, pg, pb) < 4.5f) return false;
                if (Ratio(0.78f, 0.88f, 1f, pr, pg, pb) < 4.5f) return false;
            }
            return true;
        }

        static bool PortraitText()
        {
            float pr = MixP(0.08f, 1f);
            float pg = MixP(0.32f, 0.86f);
            float pb = MixP(0.86f, 0.12f);
            if (Ratio(1f, 0.98f, 0.92f, pr, pg, pb) < 4.5f) return false;
            if (Ratio(0.78f, 0.88f, 1f, pr, pg, pb) < 4.5f) return false;
            return true;
        }

        static float Mix(float ink, float seat)
        {
            return ink + (seat - ink) * SeatMix;
        }

        static float MixP(float panel, float tint)
        {
            return panel + (tint - panel) * PortraitMix;
        }

        public static float Ratio(float ar, float ag, float ab, float br, float bg, float bb)
        {
            float l1 = Lum(ar, ag, ab);
            float l2 = Lum(br, bg, bb);
            if (l1 < l2)
            {
                float t = l1;
                l1 = l2;
                l2 = t;
            }
            return (l1 + 0.05f) / (l2 + 0.05f);
        }

        static float Lum(float r, float g, float b)
        {
            return 0.2126f * Lin(r) + 0.7152f * Lin(g) + 0.0722f * Lin(b);
        }

        static float Lin(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            float x = (c + 0.055f) / 1.055f;
            return (float)System.Math.Pow(x, 2.4);
        }
    }
}
