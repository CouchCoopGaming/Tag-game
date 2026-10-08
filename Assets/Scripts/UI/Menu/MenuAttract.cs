using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Title idle. The logo bobs and the start glyph breathes.
    /// Reduce motion leaves both still. The arena photo drift stays on the host.
    /// </summary>
    public static class MenuAttract
    {
        static RectTransform _logo;
        static Image _glyph;
        static Vector2 _rest;
        static bool _have;

        public static void Bind(RectTransform logo, Image glyph)
        {
            _logo = logo;
            _glyph = glyph;
            _have = logo != null;
            if (_have) _rest = logo.anchoredPosition;
        }

        public static void Clear()
        {
            _logo = null;
            _glyph = null;
            _have = false;
        }

        public static void Tick()
        {
            if (MenuVideo.ReduceMotion) return;
            float t = Time.unscaledTime;
            if (_have && _logo != null)
            {
                Vector2 p = _rest;
                p.y += Mathf.Sin(t * 1.2f) * 8f;
                _logo.anchoredPosition = p;
                _logo.localRotation = Quaternion.Euler(0f, 0f, -8f + Mathf.Sin(t * 0.8f) * 2f);
            }
            if (_glyph != null)
            {
                Color c = _glyph.color;
                c.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * 2.4f));
                _glyph.color = c;
            }
        }
    }
}
