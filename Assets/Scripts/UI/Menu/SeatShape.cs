using Tag.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Circle, triangle, square, diamond for seats 1–4. Drawn only when the
    /// color-blind seat colors are on, so the default chips stay as they are.
    /// </summary>
    public static class SeatShape
    {
        static Sprite[] _sprites;

        public static Sprite For(int seat)
        {
            return SpriteOf(seat);
        }

        public static void Stamp(Transform parent, int seat, float x, float y, float size, Color ink)
        {
            if (parent == null || GameSettings.Current == null || GameSettings.Current.CvdSeats == SeatCvd.Off)
                return;
            RectTransform rt = MenuWidgets.Place(parent, "SeatShape", x, y, size, size);
            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = SpriteOf(seat);
            image.color = ink;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static Sprite SpriteOf(int seat)
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
            if (kind == 2)
                return dx <= 0.36f && dy <= 0.36f ? 1f : 0f;
            if (kind == 3)
                return dx + dy <= 0.46f ? 1f : 0f;
            // Point-up triangle. Texture y grows upward, which is the top of the chip.
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
