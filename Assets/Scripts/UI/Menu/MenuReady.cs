using Tag.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Three-second all-ready count. Un-ready or back cancels it.
    /// The digits are literals so the cast tick does not build strings.
    /// </summary>
    public static class MenuReady
    {
        public const float Seconds = 3f;

        static float _start;
        static bool _run;
        static int _digit;

        public static void Stop()
        {
            _run = false;
            _digit = 0;
        }

        public static int Digit
        {
            get { return _digit; }
        }

        public static bool Advance(bool allReady, Text banner)
        {
            if (!allReady)
            {
                if (banner != null) banner.text = "";
                Stop();
                return false;
            }
            if (!_run)
            {
                _run = true;
                _start = Time.unscaledTime;
                _digit = 0;
            }
            float left = Seconds - (Time.unscaledTime - _start);
            if (left <= 0f)
            {
                Stop();
                return true;
            }
            int n = 1;
            if (left > 2f) n = 3;
            else if (left > 1f) n = 2;
            if (n != _digit)
            {
                _digit = n;
                if (banner != null)
                {
                    if (n == 3) banner.text = "Starting in 3";
                    else if (n == 2) banner.text = "Starting in 2";
                    else banner.text = "Starting in 1";
                }
                TagSfx.CountdownBeep();
            }
            return false;
        }
    }
}
