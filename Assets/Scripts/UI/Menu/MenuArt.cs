using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Rounded plates, a sky gradient, and prompt chips. Built once at menu
    /// start. Gameplay frames do not call this.
    /// </summary>
    public static class MenuArt
    {
        static Sprite _round;
        static Sprite _sky;
        static Sprite _chip;
        static Texture2D _chevron;

        public static Sprite Round
        {
            get
            {
                if (_round != null) return _round;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                float r = 22f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float a = Cover(x + 0.5f, y + 0.5f, n, n, r);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _round = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect, new Vector4(24f, 24f, 24f, 24f));
                return _round;
            }
        }

        public static Sprite Sky
        {
            get
            {
                if (_sky != null) return _sky;
                const int h = 128;
                var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                for (int y = 0; y < h; y++)
                {
                    float t = y / (h - 1f);
                    Color c = Color.Lerp(MenuTheme.SkyBot, MenuTheme.SkyTop, t);
                    for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
                }
                tex.Apply();
                _sky = Sprite.Create(tex, new Rect(0f, 0f, 4f, h), new Vector2(0.5f, 0.5f), 4f);
                return _sky;
            }
        }

        public static Sprite Chip
        {
            get
            {
                if (_chip != null) return _chip;
                const int n = 32;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                float r = 8f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float a = Cover(x + 0.5f, y + 0.5f, n, n, r);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                tex.Apply();
                _chip = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n, 0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
                return _chip;
            }
        }

        public static Texture2D Chevron
        {
            get
            {
                if (_chevron != null) return _chevron;
                const int n = 64;
                _chevron = new Texture2D(n, n, TextureFormat.RGBA32, false);
                _chevron.wrapMode = TextureWrapMode.Repeat;
                _chevron.filterMode = FilterMode.Bilinear;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        int d = Mathf.Abs((x + y) % 32 - 16);
                        float a = d < 3 ? 0.55f : 0f;
                        _chevron.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                }
                _chevron.Apply();
                return _chevron;
            }
        }

        public static void Plate(Image image, Color color, bool sliced)
        {
            if (image == null) return;
            image.sprite = Round;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.pixelsPerUnitMultiplier = 1f;
        }

        static float Cover(float x, float y, float w, float h, float r)
        {
            float dx = x < r ? r - x : (x > w - r ? x - (w - r) : 0f);
            float dy = y < r ? r - y : (y > h - r ? y - (h - r) : 0f);
            if (dx == 0f && dy == 0f) return 1f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= r - 0.75f) return 1f;
            if (dist >= r + 0.75f) return 0f;
            return (r + 0.75f - dist) / 1.5f;
        }
    }
}
