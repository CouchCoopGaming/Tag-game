using System.Globalization;
using Tag.Settings;
using Tag.Ui.Hud;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Pushes the options pages onto the listener, the bus gains, and the picture.
    /// The same call writes the blob the next boot reads back.
    /// </summary>
    public static class OptionApply
    {
        public const int Hub = 0;
        public const int Audio = 1;
        public const int Display = 2;
        public const int Access = 3;
        public const int Look = 4;

        public static float GroupMaster;
        public static float GroupSfx;
        public static float GroupUi;
        public static float GroupMusic;
        public static int AppliedQuality;
        public static int AppliedText;
        public static float AppliedScale;
        public static bool ComicOn = true;
        public static int Armed = -1;
        public static string BusLine = "";
        public static string CvdLine = "";
        public static string QualityLine = "";
        public static string SeatLine = "";

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

        /// <summary>First confirm arms the prompt. The second resets that page only.</summary>
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
            if (page == Audio) s.ResetSound();
            else if (page == Display) s.ResetPicture();
            else if (page == Access) s.ResetAccess();
            else if (page == Look) s.ResetLook();
            else if (page == Hub)
            {
                s.ResetSound();
                s.ResetPicture();
                s.ResetAccess();
            }
        }

        public static void SnapBuses(GameSettings s)
        {
            if (s == null) s = GameSettings.Defaults();
            float gate = s.Muted ? 0f : Unit(s.Master);
            GroupMaster = gate;
            GroupSfx = gate * Unit(s.Sfx);
            GroupUi = gate * Unit(s.Ui);
            GroupMusic = gate * Unit(s.Music);
            AudioListener.volume = gate;
        }

        public static void Apply(GameSettings s)
        {
            if (s == null) s = GameSettings.Current ?? GameSettings.Defaults();
            int res = s.ResIndex;
            if (res < 0) res = 0;
            if (res >= MenuVideo.Widths.Length) res = MenuVideo.Widths.Length - 1;
            MenuVideo.Index = res;
            MenuVideo.Full = s.Fullscreen;
            MenuVideo.VSync = s.VSync;
            int q = s.PictureQuality;
            if (q < 0) q = 0;
            MenuVideo.Quality = q;
            MenuVideo.ReduceMotion = s.ReduceMotion;
            MenuVideo.Adopted = true;
            MenuVideo.Apply();
            SnapBuses(s);
            MatchHudText.ComicWords = s.ComicWords;
            ComicOn = s.ComicWords;
            AppliedQuality = QualitySettings.GetQualityLevel();
            AppliedText = UiFit.TextPx(UiFit.FloorFont);
            AppliedScale = s.UiScale;
        }

        public static string Holds()
        {
            GameSettings prev = GameSettings.Current;
            int vsyncWas = QualitySettings.vSyncCount;
            bool comicWas = MatchHudText.ComicWords;
            int armedWas = Armed;
            int qualityNames = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            int qualityLive = qualityNames > 1 ? qualityNames - 1 : 0;
            bool master = false, sfx = false, ui = false, music = false, mute = false;
            bool res = false, full = false, vsync = false, quality = false, scale = false;
            bool motion = false, text = false, player = false, palette = false, comic = false;
            bool mouse = false, pad = false, invert = false, fov = false;
            bool reset = false, apply = false, persist = false;
            try
            {
                QualityLine = ProveQuality();
                SeatLine = SeatCvd.Line();
                float protan = SeatCvd.Min(SeatCvd.Off, AccessibilityPalette.CvdProtanopia);
                float deutan = SeatCvd.Min(SeatCvd.Off, AccessibilityPalette.CvdDeuteranopia);
                float tritan = SeatCvd.Min(SeatCvd.Off, AccessibilityPalette.CvdTritanopia);
                CvdLine = "ui-cvd protan=" + protan.ToString("0.00", CultureInfo.InvariantCulture)
                    + " deutan=" + deutan.ToString("0.00", CultureInfo.InvariantCulture)
                    + " tritan=" + tritan.ToString("0.00", CultureInfo.InvariantCulture)
                    + " floor=" + AccessibilityPalette.MinPairDistance.ToString("0.00", CultureInfo.InvariantCulture);
                GameSettings edited = GameSettings.Defaults();
                edited.Master = 0.5f;
                edited.Sfx = 0.75f;
                edited.Ui = 0.5f;
                edited.Music = 0.55f;
                edited.Muted = false;
                edited.ResIndex = 0;
                edited.Fullscreen = false;
                edited.VSync = false;
                edited.PictureQuality = qualityNames > 1 ? qualityLive : 2;
                edited.UiScale = 1.1f;
                edited.ReduceMotion = true;
                edited.HudScale = 1.25f;
                edited.AccessSeat = 2;
                edited.Palette[0] = 1;
                edited.Colorblind = true;
                edited.ComicWords = false;
                edited.MouseSensitivity = 3.2f;
                edited.GamepadLook = 3.0f;
                edited.InvertY = true;
                edited.Fov = 90f;
                GameSettings.Current = edited;
                Apply(edited);
                float openMaster = GroupMaster;
                float openEar = AudioListener.volume;
                bool buses = Near(GroupMaster, 0.5f) && Near(GroupSfx, 0.5f * 0.75f) && Near(GroupUi, 0.5f * 0.5f) && Near(GroupMusic, 0.5f * 0.55f);
                bool picture = QualitySettings.GetQualityLevel() == qualityLive && Screen.width == 1280 && Screen.fullScreenMode == FullScreenMode.Windowed && QualitySettings.vSyncCount == 0;
                bool words = !MatchHudText.ComicWords && MatchHudText.Comic(MatchHudText.DashGo).Length == 0 && MatchHudText.Comic(MatchHudText.YoureIt).Length == 0;
                MatchHudText.ComicWords = true;
                bool wordsOn = MatchHudText.Comic(MatchHudText.DashGo) == MatchHudText.DashGo && MatchHudText.Comic(MatchHudText.Pull) == MatchHudText.Pull;
                MatchHudText.ComicWords = false;
                bool type = UiFit.TextPx(40) == 50 && UiFit.TextPx(UiFit.FloorFont) == 38;
                apply = buses && picture && words && wordsOn && type && Near(AppliedScale, 1.1f);
                edited.Muted = true;
                Apply(edited);
                float shutMaster = GroupMaster;
                float shutEar = AudioListener.volume;
                BusLine = "ui-bus master=" + openMaster.ToString("0.00", CultureInfo.InvariantCulture)
                    + " listener=" + openEar.ToString("0.00", CultureInfo.InvariantCulture)
                    + " muted-master=" + shutMaster.ToString("0.00", CultureInfo.InvariantCulture)
                    + " muted-listener=" + shutEar.ToString("0.00", CultureInfo.InvariantCulture);
                bool mutedBus = GroupMaster == 0f && GroupSfx == 0f && GroupUi == 0f && GroupMusic == 0f && AudioListener.volume == 0f;
                string blob = SettingsFile.Write(edited, ActionBinds.Defaults());
                GameSettings loaded = GameSettings.Defaults();
                SettingsFile.Read(blob, loaded, ActionBinds.Defaults());
                GameSettings.Current = loaded;
                Apply(loaded);
                master = Near(loaded.Master, 0.5f) && mutedBus;
                sfx = Near(loaded.Sfx, 0.75f);
                ui = Near(loaded.Ui, 0.5f);
                music = Near(loaded.Music, 0.55f);
                mute = loaded.Muted && GroupMaster == 0f;
                res = loaded.ResIndex == 0 && Screen.width == 1280;
                full = !loaded.Fullscreen && Screen.fullScreenMode == FullScreenMode.Windowed;
                vsync = !loaded.VSync && QualitySettings.vSyncCount == 0;
                quality = loaded.PictureQuality == edited.PictureQuality && QualitySettings.GetQualityLevel() == qualityLive;
                scale = Near(loaded.UiScale, 1.1f) && Near(AppliedScale, 1.1f);
                motion = loaded.ReduceMotion && MenuVideo.ReduceMotion;
                text = Near(loaded.HudScale, 1.25f) && UiFit.TextPx(40) == 50;
                player = loaded.AccessSeat == 2;
                palette = loaded.Palette[0] == 1 && loaded.Colorblind;
                comic = !loaded.ComicWords && !MatchHudText.ComicWords && words && wordsOn;
                mouse = Near(loaded.MouseSensitivity, 3.2f);
                pad = Near(loaded.GamepadLook, 3.0f);
                invert = loaded.InvertY;
                fov = Near(loaded.Fov, 90f);
                MenuVideo.Adopted = false;
                MenuVideo.Save();
                MenuVideo.Index = 2;
                MenuVideo.Full = true;
                MenuVideo.VSync = true;
                MenuVideo.Quality = 0;
                MenuVideo.ReduceMotion = false;
                MenuVideo.Load();
                bool prefs = MenuVideo.Index == 0 && !MenuVideo.Full && !MenuVideo.VSync && MenuVideo.Quality == edited.PictureQuality && MenuVideo.ReduceMotion;
                if (!prefs)
                {
                    res = false;
                    full = false;
                    vsync = false;
                    quality = false;
                    motion = false;
                }
                GameSettings partial = GameSettings.Defaults();
                SettingsFile.Read("v=2\nmouse=1.8\n", partial, ActionBinds.Defaults());
                bool kept = partial.ComicWords && !partial.ReduceMotion && partial.ResIndex == 2 && partial.Fullscreen && partial.VSync && partial.PictureQuality == GameSettings.QualityMedium;
                if (!kept) persist = false;
                GameSettings page = GameSettings.Defaults();
                page.Master = 0.25f;
                page.MouseSensitivity = 3.2f;
                page.HudScale = 1.5f;
                page.ComicWords = false;
                bool armed = !ConfirmReset(Audio, 5, page) && page.Master < 0.3f;
                bool soundBack = ConfirmReset(Audio, 5, page) && page.Master > 0.79f && page.Master < 0.81f && page.MouseSensitivity > 3f;
                page.Master = 0.25f;
                page.MouseSensitivity = 3.2f;
                ConfirmReset(Look, 4, page);
                bool lookKeeps = ConfirmReset(Look, 4, page) && page.Master < 0.3f && page.MouseSensitivity > 1.7f && page.MouseSensitivity < 1.9f;
                page.Master = 0.25f;
                page.HudScale = 1.5f;
                page.MouseSensitivity = 3.2f;
                ConfirmReset(Hub, 6, page);
                bool hub = ConfirmReset(Hub, 6, page) && page.Master > 0.79f && page.HudScale > 0.99f && page.HudScale < 1.01f && page.MouseSensitivity > 3f;
                reset = armed && soundBack && lookKeeps && hub;
                persist = master && sfx && ui && music && mute && res && full && vsync && quality && scale && motion && text && player && palette && comic && mouse && pad && invert && fov && kept && reset;
                if (!persist) apply = apply && buses;
            }
            finally
            {
                QualitySettings.vSyncCount = vsyncWas;
                GameSettings.Current = prev ?? GameSettings.Defaults();
                MatchHudText.ComicWords = comicWas;
                Armed = armedWas;
                Apply(GameSettings.Current);
                MenuVideo.Save();
            }
            string line = "ui-apply"
                + " master=" + Bit(master)
                + " sfx=" + Bit(sfx)
                + " ui=" + Bit(ui)
                + " music=" + Bit(music)
                + " mute=" + Bit(mute)
                + " res=" + Bit(res)
                + " full=" + Bit(full)
                + " vsync=" + Bit(vsync)
                + " quality=" + Bit(quality)
                + " scale=" + Bit(scale)
                + " motion=" + Bit(motion)
                + " text=" + Bit(text)
                + " player=" + Bit(player)
                + " palette=" + Bit(palette)
                + " comic=" + Bit(comic)
                + " mouse=" + Bit(mouse)
                + " pad=" + Bit(pad)
                + " invert=" + Bit(invert)
                + " fov=" + Bit(fov)
                + " reset=" + Bit(reset)
                + " apply=" + Bit(apply)
                + " persist=" + Bit(persist);
            return line;
        }

        static string ProveQuality()
        {
            GameSettings bare = GameSettings.Defaults();
            SettingsFile.Read("quality=0\n", bare, ActionBinds.Defaults(), false);
            GameSettings legacy = GameSettings.Defaults();
            SettingsFile.Read("v=2\nquality=0\n", legacy, ActionBinds.Defaults(), false);
            bool oldMedium = bare.PictureQuality == GameSettings.QualityMedium
                && legacy.PictureQuality == GameSettings.QualityMedium;
            GameSettings low = GameSettings.Defaults();
            low.PictureQuality = GameSettings.QualityLow;
            string written = SettingsFile.Write(low, ActionBinds.Defaults());
            GameSettings back = GameSettings.Defaults();
            SettingsFile.Read(written, back, ActionBinds.Defaults(), false);
            bool lowStays = back.PictureQuality == GameSettings.QualityLow
                && written.IndexOf("qv=1", System.StringComparison.Ordinal) >= 0;
            return "ui-quality old=" + (oldMedium ? "medium" : "fail")
                + " low=" + (lowStays ? "low" : "fail");
        }

        static string Bit(bool ok)
        {
            return ok ? "ok" : "no";
        }

        static bool Near(float a, float b)
        {
            float d = a - b;
            if (d < 0f) d = -d;
            return d < 0.02f;
        }

        static float Unit(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
