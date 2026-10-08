using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Bold menu glyphs drawn in code. No purchased art.
    /// </summary>
    public static class MenuIcons
    {
        public static readonly Color PlayTint = new Color(0.95f, 0.22f, 0.28f, 1f);
        public static readonly Color PracticeTint = new Color(1f, 0.55f, 0.12f, 1f);
        public static readonly Color OptionsTint = new Color(0.25f, 0.62f, 1f, 1f);
        public static readonly Color ControlsTint = new Color(0.20f, 0.82f, 0.38f, 1f);
        public static readonly Color CreditsTint = new Color(1f, 0.84f, 0.12f, 1f);
        public static readonly Color QuitTint = new Color(0.95f, 0.45f, 0.18f, 1f);

        static Sprite _play;
        static Sprite _cone;
        static Sprite _gear;
        static Sprite _pad;
        static Sprite _star;
        static Sprite _door;
        static Sprite _keys;
        static Sprite _either;
        static Sprite _arrows;
        static Sprite _space;
        static Sprite _esc;
        static Sprite _stick;
        static Sprite _south;
        static Sprite _east;

        public static Sprite Play => _play ??= Runner();
        public static Sprite Cone => _cone ??= TrafficCone();
        public static Sprite Gear => _gear ??= Cog();
        public static Sprite Pad => _pad ??= Gamepad();
        public static Sprite Star => _star ??= Burst();
        public static Sprite Door => _door ??= ExitDoor();
        public static Sprite Keys => _keys ??= Keyboard();
        public static Sprite Either => _either ??= JoinMark();
        public static Sprite KeyArrows => _arrows ??= ArrowKeys();
        public static Sprite KeySpace => _space ??= SpaceKey();
        public static Sprite KeyEsc => _esc ??= EscKey();
        public static Sprite Stick => _stick ??= StickCap();
        public static Sprite South => _south ??= FaceCap(new Color(0.15f, 0.72f, 0.32f, 1f), LetterA());
        public static Sprite East => _east ??= FaceCap(new Color(0.90f, 0.22f, 0.28f, 1f), LetterB());

        static Sprite Runner()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            Color ink = Color.white;
            Disc(px, n, 62, 74, 11, ink);
            Limb(px, n, 54, 66, 46, 40, 8, ink);
            Limb(px, n, 48, 42, 28, 18, 7, ink);
            Limb(px, n, 50, 44, 78, 24, 7, ink);
            Limb(px, n, 52, 58, 30, 62, 6, ink);
            Limb(px, n, 56, 58, 82, 70, 6, ink);
            return Bake(px, n, n);
        }

        static Sprite TrafficCone()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            Tri(px, n, 48, 84, 22, 22, 74, 22, new Color(1f, 0.45f, 0.08f, 1f));
            Fill(px, n, 30, 48, 66, 58, Color.white);
            Fill(px, n, 18, 16, 78, 26, new Color(0.15f, 0.16f, 0.2f, 1f));
            return Bake(px, n, n);
        }

        static Sprite Cog()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            Disc(px, n, 48, 48, 28, Color.white);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                int cx = 48 + Mathf.RoundToInt(Mathf.Cos(a) * 30f);
                int cy = 48 + Mathf.RoundToInt(Mathf.Sin(a) * 30f);
                Disc(px, n, cx, cy, 8, Color.white);
            }
            Disc(px, n, 48, 48, 12, new Color(0f, 0f, 0f, 0f));
            return Bake(px, n, n);
        }

        static Sprite Gamepad()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            RoundBox(px, n, 10, 28, 86, 70, 16, Color.white);
            Fill(px, n, 24, 44, 30, 58, new Color(0.1f, 0.12f, 0.16f, 1f));
            Fill(px, n, 32, 36, 38, 66, new Color(0.1f, 0.12f, 0.16f, 1f));
            Disc(px, n, 64, 54, 6, new Color(0.95f, 0.22f, 0.28f, 1f));
            Disc(px, n, 76, 44, 6, new Color(0.25f, 0.62f, 1f, 1f));
            return Bake(px, n, n);
        }

        static Sprite Burst()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            DrawStar(px, n, 48, 48, 44f, 18f, new Color(0.12f, 0.06f, 0.02f, 1f));
            DrawStar(px, n, 48, 48, 34f, 14f, new Color(1f, 0.98f, 0.92f, 1f));
            return Bake(px, n, n);
        }

        static void DrawStar(Color[] px, int n, int cx, int cy, float outer, float inner, Color c)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / 5f;
                float b = a + Mathf.PI * 2f / 10f;
                float d = a + Mathf.PI * 2f / 5f;
                int x0 = cx + Mathf.RoundToInt(Mathf.Cos(a) * outer);
                int y0 = cy + Mathf.RoundToInt(Mathf.Sin(a) * outer);
                int x1 = cx + Mathf.RoundToInt(Mathf.Cos(b) * inner);
                int y1 = cy + Mathf.RoundToInt(Mathf.Sin(b) * inner);
                int x2 = cx + Mathf.RoundToInt(Mathf.Cos(d) * outer);
                int y2 = cy + Mathf.RoundToInt(Mathf.Sin(d) * outer);
                Tri(px, n, cx, cy, x0, y0, x1, y1, c);
                Tri(px, n, cx, cy, x1, y1, x2, y2, c);
            }
        }

        static Sprite ArrowKeys()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            KeyBody(px, n, 8, 16, 120, 112);
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            Tri(px, n, 64, 100, 48, 78, 80, 78, ink);
            Tri(px, n, 64, 28, 48, 50, 80, 50, ink);
            Tri(px, n, 28, 64, 50, 48, 50, 80, ink);
            Tri(px, n, 100, 64, 78, 48, 78, 80, ink);
            Fill(px, n, 58, 50, 70, 78, ink);
            Fill(px, n, 50, 58, 78, 70, ink);
            return Bake(px, n, n);
        }

        static Sprite SpaceKey()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            KeyBody(px, n, 6, 28, 122, 100);
            RoundBox(px, n, 22, 48, 106, 78, 8, new Color(0.06f, 0.10f, 0.20f, 1f));
            return Bake(px, n, n);
        }

        static Sprite EscKey()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            KeyBody(px, n, 8, 24, 120, 104);
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            Stamp(px, n, 18, 40, 5, LetterE(), ink);
            Stamp(px, n, 50, 40, 5, LetterS(), ink);
            Stamp(px, n, 82, 40, 5, LetterC(), ink);
            return Bake(px, n, n);
        }

        static Sprite StickCap()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Disc(px, n, 64, 58, 40, new Color(0.10f, 0.14f, 0.22f, 1f));
            Disc(px, n, 64, 58, 30, new Color(0.82f, 0.88f, 0.96f, 1f));
            Disc(px, n, 64, 58, 8, new Color(0.10f, 0.14f, 0.22f, 1f));
            Disc(px, n, 86, 80, 16, new Color(0.06f, 0.10f, 0.20f, 1f));
            Disc(px, n, 86, 80, 11, Color.white);
            return Bake(px, n, n);
        }

        static Sprite FaceCap(Color face, int[] letter)
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Disc(px, n, 64, 64, 52, new Color(0.06f, 0.08f, 0.12f, 1f));
            Disc(px, n, 64, 64, 44, face);
            Disc(px, n, 64, 78, 10, new Color(1f, 1f, 1f, 0.28f));
            Stamp(px, n, 46, 42, 7, letter, Color.white);
            return Bake(px, n, n);
        }

        static void KeyBody(Color[] px, int n, int x0, int y0, int x1, int y1)
        {
            RoundBox(px, n, x0, y0, x1, y1, 16, new Color(0.06f, 0.10f, 0.18f, 1f));
            RoundBox(px, n, x0 + 6, y0 + 6, x1 - 6, y1 - 6, 12, new Color(0.96f, 0.97f, 1f, 1f));
        }

        static int[] LetterE()
        {
            return new[] { 0x1F, 0x10, 0x1E, 0x10, 0x1F };
        }

        static int[] LetterS()
        {
            return new[] { 0x0F, 0x10, 0x0E, 0x01, 0x1E };
        }

        static int[] LetterC()
        {
            return new[] { 0x0F, 0x10, 0x10, 0x10, 0x0F };
        }

        static int[] LetterA()
        {
            return new[] { 0x0E, 0x11, 0x1F, 0x11, 0x11 };
        }

        static int[] LetterB()
        {
            return new[] { 0x1E, 0x11, 0x1E, 0x11, 0x1E };
        }

        static void Stamp(Color[] px, int w, int h, int ox, int oy, int scale, int[] rows, Color c)
        {
            int n = rows.Length;
            for (int r = 0; r < n; r++)
            {
                int bits = rows[r];
                for (int col = 0; col < 5; col++)
                {
                    int mask = 1 << (4 - col);
                    if ((bits & mask) == 0) continue;
                    int x0 = ox + col * scale;
                    int y0 = oy + (n - 1 - r) * scale;
                    Fill(px, w, h, x0, y0, x0 + scale - 1, y0 + scale - 1, c);
                }
            }
        }

        static Sprite ExitDoor()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            RoundBox(px, n, 22, 12, 74, 84, 8, new Color(0.95f, 0.55f, 0.22f, 1f));
            RoundBox(px, n, 34, 20, 66, 78, 6, new Color(0.18f, 0.28f, 0.55f, 1f));
            Disc(px, n, 58, 48, 4, MenuTheme.Gold);
            return Bake(px, n, n);
        }

        static Sprite Keyboard()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            RoundBox(px, n, 8, 28, 88, 70, 10, Color.white);
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int x = 18 + col * 17;
                    int y = 36 + row * 16;
                    Fill(px, n, x, y, x + 12, y + 10, new Color(0.12f, 0.16f, 0.24f, 1f));
                }
            }
            return Bake(px, n, n);
        }

        static Sprite JoinMark()
        {
            const int w = 128;
            const int h = 64;
            Color[] px = Clear(w, h);
            RoundBox(px, w, h, 4, 8, 58, 56, 8, Color.white);
            Fill(px, w, h, 12, 18, 24, 28, new Color(0.12f, 0.16f, 0.24f, 1f));
            Fill(px, w, h, 28, 18, 40, 28, new Color(0.12f, 0.16f, 0.24f, 1f));
            Fill(px, w, h, 12, 34, 50, 44, new Color(0.12f, 0.16f, 0.24f, 1f));
            RoundBox(px, w, h, 70, 14, 122, 52, 10, Color.white);
            Disc(px, w, h, 86, 34, 4, new Color(0.95f, 0.22f, 0.28f, 1f));
            Disc(px, w, h, 100, 28, 4, new Color(0.25f, 0.62f, 1f, 1f));
            return Bake(px, w, h);
        }

        static Color[] Clear(int w, int h)
        {
            return new Color[w * h];
        }

        static void Disc(Color[] px, int n, int cx, int cy, int r, Color c)
        {
            Disc(px, n, n, cx, cy, r, c);
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
                    if (dx * dx + dy * dy <= r2) Plot(px, w, h, x, y, c);
                }
            }
        }

        static void Fill(Color[] px, int n, int x0, int y0, int x1, int y1, Color c)
        {
            Fill(px, n, n, x0, y0, x1, y1, c);
        }

        static void Fill(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Plot(px, w, h, x, y, c);
        }

        static void RoundBox(Color[] px, int n, int x0, int y0, int x1, int y1, int r, Color c)
        {
            RoundBox(px, n, n, x0, y0, x1, y1, r, c);
        }

        static void RoundBox(Color[] px, int w, int h, int x0, int y0, int x1, int y1, int r, Color c)
        {
            Fill(px, w, h, x0 + r, y0, x1 - r, y1, c);
            Fill(px, w, h, x0, y0 + r, x1, y1 - r, c);
            Disc(px, w, h, x0 + r, y0 + r, r, c);
            Disc(px, w, h, x1 - r, y0 + r, r, c);
            Disc(px, w, h, x0 + r, y1 - r, r, c);
            Disc(px, w, h, x1 - r, y1 - r, r, c);
        }

        static void Limb(Color[] px, int n, int x0, int y0, int x1, int y1, int radius, Color c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Disc(px, n, x, y, radius, c);
            }
        }

        static void Tri(Color[] px, int n, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
        {
            int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
            int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
            int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
            int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
            float area = Edge(x0, y0, x1, y1, x2, y2);
            if (Mathf.Abs(area) < 0.5f) return;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(x1, y1, x2, y2, x, y);
                    float w1 = Edge(x2, y2, x0, y0, x, y);
                    float w2 = Edge(x0, y0, x1, y1, x, y);
                    bool pos = w0 >= 0f && w1 >= 0f && w2 >= 0f;
                    bool neg = w0 <= 0f && w1 <= 0f && w2 <= 0f;
                    if (pos || neg) Plot(px, n, n, x, y, c);
                }
            }
        }

        static float Edge(int ax, int ay, int bx, int by, int cx, int cy)
        {
            return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
        }

        static void Plot(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            if (c.a <= 0.01f)
            {
                px[i] = new Color(0f, 0f, 0f, 0f);
                return;
            }
            Color d = px[i];
            float a = c.a + d.a * (1f - c.a);
            if (a <= 0.001f) return;
            px[i] = new Color(
                (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
                (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1f - c.a)) / a,
                a);
        }

        static Sprite Bake(Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
