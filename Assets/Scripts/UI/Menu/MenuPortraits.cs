using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Character grid portraits. A baked mesh render wins. Until the editor
    /// bake has written one, a bust in that Hier color is drawn here.
    /// </summary>
    public static class MenuPortraits
    {
        static readonly Texture2D[] Baked = new Texture2D[6];
        static readonly Texture2D[] Made = new Texture2D[6];
        static readonly bool[] Tried = new bool[6];

        public static Texture2D Of(int index)
        {
            int i = index;
            if (i < 0) i = 0;
            if (i > 5) i = 5;
            if (!Tried[i])
            {
                Tried[i] = true;
                Baked[i] = Resources.Load<Texture2D>("UI/Portraits/" + MenuMannequin.NameOf(i));
            }
            if (Baked[i] != null) return Baked[i];
            if (Made[i] == null) Made[i] = Bust(Tint(i));
            return Made[i];
        }

        public static Color Tint(int index)
        {
            switch (MenuMannequin.NameOf(index))
            {
                case "Blue": return new Color(0.42f, 0.68f, 0.92f, 1f);
                case "Mint": return new Color(0.42f, 0.82f, 0.70f, 1f);
                case "Orange": return new Color(0.94f, 0.42f, 0.14f, 1f);
                case "Lavender": return new Color(0.70f, 0.58f, 0.88f, 1f);
                case "Red": return new Color(0.88f, 0.22f, 0.24f, 1f);
                default: return new Color(0.90f, 0.76f, 0.52f, 1f);
            }
        }

        static Texture2D Bust(Color body)
        {
            const int w = 128;
            const int h = 160;
            var px = new Color[w * h];
            Color ink = new Color(0.05f, 0.07f, 0.12f, 1f);
            Color shade = Color.Lerp(body, ink, 0.35f);
            Color panel = Color.Lerp(body, Color.white, 0.35f);
            Fill(px, w, h, 18, 8, 110, 78, shade);
            Disc(px, w, h, 64, 118, 28, body);
            Disc(px, w, h, 54, 124, 4, ink);
            Disc(px, w, h, 74, 124, 4, ink);
            Fill(px, w, h, 46, 70, 82, 108, panel);
            Fill(px, w, h, 8, 48, 28, 96, shade);
            Fill(px, w, h, 100, 48, 120, 96, shade);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static void Fill(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
        }

        static void Disc(Color[] px, int w, int h, int cx, int cy, int r, Color c)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy > r2) continue;
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    px[y * w + x] = c;
                }
            }
        }
    }
}
