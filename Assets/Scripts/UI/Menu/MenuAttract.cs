using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Title idle. The logo bobs and the start line breathes.
    /// Reduce motion leaves both still. The arena photo drift stays on the host.
    /// </summary>
    public static class MenuAttract
    {
        static RectTransform _logo;
        static CanvasGroup _prompt;
        static Vector2 _rest;
        static float _restZ;
        static bool _have;

        public static void Bind(RectTransform logo, CanvasGroup prompt)
        {
            _logo = logo;
            _prompt = prompt;
            _have = logo != null;
            if (_have)
            {
                _rest = logo.anchoredPosition;
                _restZ = logo.localEulerAngles.z;
                if (_restZ > 180f) _restZ -= 360f;
            }
            if (_prompt != null) _prompt.alpha = 1f;
        }

        public static void Clear()
        {
            _logo = null;
            _prompt = null;
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
                _logo.localRotation = Quaternion.Euler(0f, 0f, _restZ + Mathf.Sin(t * 0.8f) * 2f);
            }
            if (_prompt != null)
                _prompt.alpha = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(t * 2.4f));
        }
    }
}
