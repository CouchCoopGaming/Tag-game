using Tag.Couch;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Screen travel. The slide and the input lock share one duration.
    /// Back never opens a hidden or loading screen. A lost pad uses the
    /// seat prompt the couch already tracks.
    /// </summary>
    public static class MenuFlow
    {
        public const float SlideSeconds = 0.2f;

        /// <summary>
        /// Title, main, character select, rules, arena, and results.
        /// Pause, options, controls, credits, records, and the lobby keep the plain slide.
        /// </summary>
        public static bool Feel(MenuScreenId id)
        {
            return id == MenuScreenId.Title
                || id == MenuScreenId.Main
                || id == MenuScreenId.Cast
                || id == MenuScreenId.Rules
                || id == MenuScreenId.Arena
                || id == MenuScreenId.Results;
        }

        public static float Ease(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>
        /// slide is 1 at the open and 0 at rest. sign -1 plays the same move backward.
        /// </summary>
        public static void Travel(float slide, float sign, out float offset, out float scale)
        {
            if (slide < 0f) slide = 0f;
            if (slide > 1f) slide = 1f;
            float e = Ease(1f - slide);
            float from = sign < 0f ? -160f : 160f;
            offset = from * (1f - e);
            scale = 0.92f + 0.08f * e;
        }

        const int Cap = 10;

        static readonly MenuScreenId[] _from = new MenuScreenId[Cap];
        static int _n;
        static bool _quiet;
        static bool _previewLost;
        static string _shown = "";
        static Image _plate;
        static Text _who;

        public static bool Disconnected
        {
            get { return _previewLost || CouchPlay.NeedsRejoin; }
        }

        public static void Clear()
        {
            _n = 0;
        }

        public static void Quiet()
        {
            _quiet = true;
        }

        public static void Enter(MenuScreenId leaving)
        {
            if (_quiet)
            {
                _quiet = false;
                return;
            }
            if (leaving == MenuScreenId.Hidden || leaving == MenuScreenId.Loading) return;
            if (_n > 0 && _from[_n - 1] == leaving) return;
            if (_n >= Cap)
            {
                for (int i = 1; i < Cap; i++) _from[i - 1] = _from[i];
                _n = Cap - 1;
            }
            _from[_n++] = leaving;
        }

        public static MenuScreenId Parent(MenuScreenId fallback)
        {
            for (int i = _n - 1; i >= 0; i--)
            {
                MenuScreenId id = _from[i];
                if (id == MenuScreenId.Hidden || id == MenuScreenId.Loading) continue;
                return id;
            }
            return fallback;
        }

        public static void TrimTo(MenuScreenId id)
        {
            _quiet = true;
            while (_n > 0 && _from[_n - 1] != id) _n--;
            if (_n > 0 && _from[_n - 1] == id) _n--;
        }

        public static bool Locked(float slide, float gateTime)
        {
            if (MenuCapture.Running) return false;
            if (MenuVideo.ReduceMotion)
                return Time.unscaledTime < gateTime;
            if (slide > 0.02f) return true;
            return Time.unscaledTime < gateTime;
        }

        public static void BindLost(Image plate, Text who)
        {
            _plate = plate;
            _who = who;
            if (_plate != null) _plate.gameObject.SetActive(false);
        }

        public static void PreviewLost(bool on)
        {
            _previewLost = on;
            _shown = "";
        }

        public static void Watch()
        {
            if (MenuCapture.Running) return;
            CouchDevices.PollHotplug();
        }

        public static void PaintLost()
        {
            if (_plate == null) return;
            string line = _previewLost ? "P1 reconnect" : CouchPlay.RejoinPrompt;
            bool on = !string.IsNullOrEmpty(line);
            if (_plate.gameObject.activeSelf != on)
                _plate.gameObject.SetActive(on);
            if (!on)
            {
                _shown = "";
                return;
            }
            if (_who != null && line != _shown)
            {
                _who.text = line;
                _shown = line;
            }
        }
    }
}
