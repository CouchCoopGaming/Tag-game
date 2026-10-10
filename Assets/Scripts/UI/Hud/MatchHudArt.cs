using UnityEngine;

namespace Tag.Ui.Hud
{
    /// <summary>
    /// Pie and arrow sprites for the match HUD. Built once, no purchased art.
    /// </summary>
    public static class MatchHudArt
    {
        static Sprite _disc;
        static Sprite _arrow;

        public static Sprite Disc => _disc != null ? _disc : (_disc = MakeDisc());
        public static Sprite Arrow => _arrow != null ? _arrow : (_arrow = MakeArrow());

        static Sprite MakeDisc()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            float r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - n * 0.5f;
                    float dy = y + 0.5f - n * 0.5f;
                    float a = 1f - Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - r) * 2f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
        }

        static Sprite MakeArrow()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float nx = x / (float)(n - 1);
                    float ny = y / (float)(n - 1);
                    float half = 0.5f - Mathf.Abs(ny - 0.5f);
                    bool ink = nx > 0.08f && nx < 0.92f && half > 0.12f && (0.92f - nx) * 0.72f < half;
                    bool edge = ink && (nx < 0.16f || half < 0.20f || (0.92f - nx) * 0.72f > half - 0.08f);
                    Color c = edge
                        ? new Color(0.05f, 0.06f, 0.08f, 1f)
                        : (ink ? Color.white : new Color(0f, 0f, 0f, 0f));
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
        }
    }
}
