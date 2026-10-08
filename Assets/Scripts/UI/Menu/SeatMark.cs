using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Seat shapes on the results cards and the arena cursors.
    /// P1 circle, P2 square, P3 triangle, P4 diamond. The ink is
    /// MenuMannequin.DarkStep of the same swatch. No second shape set.
    /// </summary>
    public static class SeatMark
    {
        static Sprite[] _sprites;

        public static Sprite For(int seat)
        {
            if (_sprites == null) _sprites = new Sprite[4];
            int i = seat;
            if (i < 0) i = 0;
            if (i > 3) i = 3;
            if (_sprites[i] != null) return _sprites[i];
            _sprites[i] = Build(i);
            return _sprites[i];
        }

        static Sprite Build(int kind)
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float a = Cover(kind, (x + 0.5f) / n, (y + 0.5f) / n);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
        }

        static float Cover(int kind, float u, float v)
        {
            float dx = Mathf.Abs(u - 0.5f);
            float dy = Mathf.Abs(v - 0.5f);
            if (kind == 0)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                return d <= 0.40f ? 1f : 0f;
            }
            if (kind == 1)
                return dx <= 0.36f && dy <= 0.36f ? 1f : 0f;
            if (kind == 3)
                return dx + dy <= 0.46f ? 1f : 0f;
            return Inside(u, v, 0.50f, 0.88f, 0.12f, 0.14f, 0.88f, 0.14f) ? 1f : 0f;
        }

        static bool Inside(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float c0 = (bx - ax) * (py - ay) - (by - ay) * (px - ax);
            float c1 = (cx - bx) * (py - by) - (cy - by) * (px - bx);
            float c2 = (ax - cx) * (py - cy) - (ay - cy) * (px - cx);
            return c0 >= 0f && c1 >= 0f && c2 >= 0f;
        }
    }
}
