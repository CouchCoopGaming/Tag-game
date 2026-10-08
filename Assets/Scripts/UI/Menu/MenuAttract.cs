using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Title idle. The logo bobs and Press Start breathes.
    /// Reduce motion leaves both still. The arena photo drift stays on the host.
    /// </summary>
    public static class MenuAttract
    {
        static RectTransform _logo;
        static Text _press;
        static Vector2 _rest;
        static bool _have;

        public static void Bind(RectTransform logo, Text press)
        {
            _logo = logo;
            _press = press;
            _have = logo != null;
            if (_have) _rest = logo.anchoredPosition;
        }

        public static void Clear()
        {
            _logo = null;
            _press = null;
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
            if (_press != null)
            {
                Color c = _press.color;
                c.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(t * 2.4f));
                _press.color = c;
            }
        }
    }
}
