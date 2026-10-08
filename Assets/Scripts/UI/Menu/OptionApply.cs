using Tag.Settings;
using Tag.Ui.Hud;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Options confirm and apply, matching the screens lane.
    /// First confirm arms a page reset. The second resets that page only.
    /// Picture and motion stay on MenuVideo. Seat CVD and comic words are the
    /// same rows as that lane, held here so the settings blob is unchanged.
    /// </summary>
    public static class OptionApply
    {
        public const int Hub = 0;
        public const int Audio = 1;
        public const int Display = 2;
        public const int Access = 3;
        public const int Look = 4;

        public const int CvdOff = 0;
        public const int CvdProtan = 1;
        public const int CvdTritan = 2;

        public static int CvdMode;
        public static bool ComicOn = true;
        public static int Armed = -1;

        static readonly float[] PdR = { 0f, 213f / 255f, 86f / 255f, 240f / 255f };
        static readonly float[] PdG = { 114f / 255f, 94f / 255f, 180f / 255f, 228f / 255f };
        static readonly float[] PdB = { 178f / 255f, 0f, 233f / 255f, 66f / 255f };
        static readonly float[] TrR = { 0.90f, 0.10f, 0.98f, 0.22f };
        static readonly float[] TrG = { 0.20f, 0.78f, 0.62f, 0.12f };
        static readonly float[] TrB = { 0.25f, 0.82f, 0.12f, 0.58f };

        public static bool IsResetRow(int page, int index)
        {
            if (page == Hub && index == 6) return true;
            if (page == Access && index == 6) return true;
            if ((page == Audio || page == Display) && index == 5) return true;
            if (page == Look && index == 4) return true;
            return false;
        }

        public static bool ArmedRow(int page, int index)
        {
            return Armed == page * 16 + index;
        }

        public static void Disarm()
        {
            Armed = -1;
        }

        public static bool ConfirmReset(int page, int index, GameSettings s)
        {
            if (!IsResetRow(page, index) || s == null) return false;
            int key = page * 16 + index;
            if (Armed != key)
            {
                Armed = key;
                return false;
            }
            Armed = -1;
            ResetPage(page, s);
            Apply(s);
            return true;
        }

        public static void ResetPage(int page, GameSettings s)
        {
            if (s == null) return;
            GameSettings d = GameSettings.Defaults();
            if (page == Audio || page == Hub)
            {
                s.Master = d.Master;
                s.Sfx = d.Sfx;
                s.Ui = d.Ui;
                s.Music = d.Music;
                s.Muted = d.Muted;
            }
            if (page == Display || page == Hub)
            {
                MenuVideo.Index = 2;
                MenuVideo.Full = true;
                MenuVideo.VSync = true;
                MenuVideo.Quality = 0;
                s.UiScale = d.UiScale;
                MenuVideo.Save();
                MenuVideo.Apply();
            }
            if (page == Access || page == Hub)
            {
                MenuVideo.ReduceMotion = false;
                MenuVideo.Save();
                s.HudScale = d.HudScale;
                s.AccessSeat = 0;
                s.Colorblind = false;
                CvdMode = CvdOff;
                MatchHudText.ComicWords = true;
                ComicOn = true;
                for (int i = 0; i < GameSettings.SeatCount; i++)
                    s.Palette[i] = 0;
            }
            if (page == Look)
            {
                s.MouseSensitivity = d.MouseSensitivity;
                s.GamepadLook = d.GamepadLook;
                s.InvertY = d.InvertY;
                s.Fov = d.Fov;
            }
            s.Clamp();
        }

        public static void Apply(GameSettings s)
        {
            if (s == null) s = GameSettings.Current ?? GameSettings.Defaults();
            MenuVideo.Apply();
            float gate = s.Muted ? 0f : Unit(s.Master);
            AudioListener.volume = gate;
            MatchHudText.ComicWords = ComicOn;
        }

        public static void CycleCvd(int dir)
        {
            int next = CvdMode + (dir < 0 ? -1 : 1);
            if (next < CvdOff) next = CvdTritan;
            if (next > CvdTritan) next = CvdOff;
            CvdMode = next;
        }

        public static string CvdWord()
        {
            if (CvdMode == CvdProtan) return "Protan/Deutan";
            if (CvdMode == CvdTritan) return "Tritan";
            return "Off";
        }

        public static void SeatColor(int mode, int seat, out float r, out float g, out float b)
        {
            int i = seat;
            if (i < 0) i = 0;
            if (i > 3) i = 3;
            if (mode == CvdProtan)
            {
                r = PdR[i];
                g = PdG[i];
                b = PdB[i];
                return;
            }
            if (mode == CvdTritan)
            {
                r = TrR[i];
                g = TrG[i];
                b = TrB[i];
                return;
            }
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            AccessibilityPalette.Player(s.PaletteOf(s.AccessSeat), i, out r, out g, out b);
        }

        static float Unit(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
