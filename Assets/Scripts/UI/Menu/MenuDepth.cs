using System.Globalization;
using Tag.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Options pages. Audio, display, and accessibility sit behind the hub.
    /// Values are the settings that already exist. Nothing here is a feel number.
    /// </summary>
    public static class MenuDepth
    {
        public const int Hub = 0;
        public const int Audio = 1;
        public const int Display = 2;
        public const int Access = 3;
        public const int Look = 4;

        public const int Stay = 0;
        public const int Rebuild = 1;
        public const int OpenControls = 2;
        public const int Leave = 3;
        public const int OpenCredits = 4;

        public static int Page;

        public static void Reset()
        {
            Page = Hub;
        }

        public static bool ClosePage()
        {
            if (Page == Hub) return false;
            Page = Hub;
            return true;
        }

        public static int Count
        {
            get
            {
                if (Page == Audio) return 6;
                if (Page == Display) return 6;
                if (Page == Access) return 6;
                if (Page == Look) return 6;
                return 7;
            }
        }

        public static string Header()
        {
            if (Page == Audio) return "Sound";
            if (Page == Display) return "Picture";
            if (Page == Access) return "Accessibility";
            if (Page == Look) return "Look";
            return "Options";
        }

        public static string Banner()
        {
            if (Page == Audio) return "Sliders step the volumes you already have.";
            if (Page == Display) return "Resolution, fullscreen, vsync, and the couch UI scale.";
            if (Page == Access) return "Reduce motion, text size, player colors, and comic words.";
            if (Page == Look) return "Look is shared by the couch.";
            return "Sound, picture, accessibility, controls, look, and credits.";
        }

        public static string Title(int index)
        {
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            if (Page == Hub)
            {
                if (index == 0) return "Sound";
                if (index == 1) return "Picture";
                if (index == 2) return "Accessibility";
                if (index == 3) return "Controls";
                if (index == 4) return "Look";
                if (index == 5) return "Credits";
                return "Back";
            }
            if (index == Count - 1) return "Back";
            if (Page == Audio)
            {
                if (index == 0) return s.RowLabel(GameSettings.RowMaster);
                if (index == 1) return s.RowLabel(GameSettings.RowSfx);
                if (index == 2) return s.RowLabel(GameSettings.RowUi);
                if (index == 3) return s.RowLabel(GameSettings.RowMusic);
                return s.RowLabel(GameSettings.RowMute);
            }
            if (Page == Display)
            {
                if (index == 0) return "Resolution  " + MenuVideo.ResLabel();
                if (index == 1) return "Fullscreen  " + (MenuVideo.Full ? "On" : "Window");
                if (index == 2) return "VSync  " + (MenuVideo.VSync ? "On" : "Off");
                if (index == 3) return "Quality  " + MenuVideo.QualityLabel();
                return UiFit.Line(s.UiScale);
            }
            if (Page == Access)
            {
                if (index == 0) return "Reduce motion  " + (MenuVideo.ReduceMotion ? "On" : "Off");
                if (index == 1) return "Text size  " + s.HudScale.ToString("0.00", CultureInfo.InvariantCulture);
                if (index == 2) return s.RowLabel(GameSettings.RowPlayer);
                if (index == 3) return s.RowLabel(GameSettings.RowColorblind);
                return "Comic words  " + (Tag.Ui.Hud.MatchHudText.ComicWords ? "On" : "Off");
            }
            if (index == 0) return s.RowLabel(GameSettings.RowMouse);
            if (index == 1) return s.RowLabel(GameSettings.RowPad);
            if (index == 2) return s.RowLabel(GameSettings.RowInvert);
            if (index == 3) return s.RowLabel(GameSettings.RowFov);
            return "Reset look and audio";
        }

        public static string Detail(int index)
        {
            if (Page == Hub)
            {
                if (index == 0) return "Music, effects, and UI.";
                if (index == 1) return "Resolution, fullscreen, and scale.";
                if (index == 2) return "Motion, text size, and colors.";
                if (index == 3) return "Keyboard and pad. Space jumps.";
                if (index == 4) return "One sensitivity for the couch.";
                if (index == 5) return "Team, the font, and the tools.";
                return "Main menu";
            }
            if (index == Count - 1) return "";
            if (Page == Look && index == 4) return "Does not change the park or the binds";
            if (Page == Access && index == 0) return "Menu slides and the title pulse only";
            if (Page == Access && index == 1) return "Menu and HUD text";
            if (Page == Access && index == 4) return "Verb words during a match.";
            if (Page == Display && index == 4) return "80% to 130%, for a couch TV";
            if (Page == Audio && index < 4) return "Left / Right";
            return "Left / Right";
        }

        public static float Meter(int index)
        {
            if (index < 0 || index == Count - 1) return -1f;
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            if (Page == Audio)
            {
                if (index == 0) return s.Master;
                if (index == 1) return s.Sfx;
                if (index == 2) return s.Ui;
                if (index == 3) return s.Music;
                return -1f;
            }
            if (Page == Access && index == 1)
            {
                float u = (s.HudScale - GameSettings.HudMin) / (GameSettings.HudMax - GameSettings.HudMin);
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                return u;
            }
            if (Page == Display && index == 4)
            {
                float u = (s.UiScale - GameSettings.UiScaleMin) / (GameSettings.UiScaleMax - GameSettings.UiScaleMin);
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                return u;
            }
            return -1f;
        }

        public static int Activate(int index)
        {
            if (Page == Hub)
            {
                if (index == 0) { Page = Audio; return Rebuild; }
                if (index == 1) { Page = Display; return Rebuild; }
                if (index == 2) { Page = Access; return Rebuild; }
                if (index == 3) return OpenControls;
                if (index == 4) { Page = Look; return Rebuild; }
                if (index == 5) return OpenCredits;
                return Leave;
            }
            if (index == Count - 1)
            {
                Page = Hub;
                return Rebuild;
            }
            if (Page == Look && index == 4)
            {
                GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
                GameSettings.Current = s;
                s.ResetToDefaults();
                return Stay;
            }
            Step(index, 1);
            return Stay;
        }

        public static bool Step(int index, int dir)
        {
            if (Page == Hub || index < 0 || index >= Count - 1) return false;
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            if (Page == Audio)
            {
                if (index == 0) s.Nudge(GameSettings.RowMaster, dir);
                else if (index == 1) s.Nudge(GameSettings.RowSfx, dir);
                else if (index == 2) s.Nudge(GameSettings.RowUi, dir);
                else if (index == 3) s.Nudge(GameSettings.RowMusic, dir);
                else s.Nudge(GameSettings.RowMute, dir);
            }
            else if (Page == Display)
            {
                if (index == 0) MenuVideo.CycleRes(dir);
                else if (index == 1) MenuVideo.ToggleFull();
                else if (index == 2) MenuVideo.ToggleVSync();
                else if (index == 3) MenuVideo.CycleQuality(dir);
                else s.UiScale = UiFit.Nudge(s.UiScale, dir);
            }
            else if (Page == Access)
            {
                if (index == 0) MenuVideo.ToggleMotion();
                else if (index == 1) s.Nudge(GameSettings.RowHud, dir);
                else if (index == 2) s.Nudge(GameSettings.RowPlayer, dir);
                else if (index == 3) s.Nudge(GameSettings.RowColorblind, dir);
                else Tag.Ui.Hud.MatchHudText.ComicWords = !Tag.Ui.Hud.MatchHudText.ComicWords;
            }
            else if (Page == Look)
            {
                if (index == 4) return false;
                if (index == 0) s.Nudge(GameSettings.RowMouse, dir);
                else if (index == 1) s.Nudge(GameSettings.RowPad, dir);
                else if (index == 2) s.Nudge(GameSettings.RowInvert, dir);
                else s.Nudge(GameSettings.RowFov, dir);
            }
            else return false;
            s.Clamp();
            return true;
        }

        public static void PaintSwatches(RectTransform body)
        {
            if (Page != Access || body == null) return;
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            int pal = s.PaletteOf(s.AccessSeat);
            int shown = Count;
            int win = UiFit.Window(UiFit.Current(), UiFit.OptStep, 8f);
            if (shown > win) shown = win;
            float y = 8f + (shown - 1) * UiFit.OptStep + UiFit.OptRow + 8f;
            float room = UiFit.BodyH(UiFit.Current()) - 8f - y;
            float nameH = UiFit.FloorFont;
            float swH = 78f;
            if (room < 48f + nameH) return;
            if (swH + 4f + nameH > room) swH = room - 4f - nameH;
            if (swH < 48f) return;
            string palette = AccessibilityPalette.Name(pal);
            Text label = MenuWidgets.Words(body, palette, UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(0f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.anchoredPosition = new Vector2(40f, -y);
            labelRt.sizeDelta = new Vector2(280f, swH);
            for (int i = 0; i < 4; i++)
            {
                AccessibilityPalette.Player(pal, i, out float r, out float g, out float b);
                RectTransform rt = MenuWidgets.Place(body, "Swatch", 360f + i * 150f, y, 120f, swH);
                Image image = rt.gameObject.AddComponent<Image>();
                MenuArt.Plate(image, new Color(r, g, b, 1f), true);
                image.raycastTarget = false;
                string name = i == 0 ? "P1" : i == 1 ? "P2" : i == 2 ? "P3" : "P4";
                Text pname = MenuWidgets.Words(body, name, UiFit.FloorFont, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
                RectTransform prt = pname.rectTransform;
                prt.anchorMin = new Vector2(0f, 1f);
                prt.anchorMax = new Vector2(0f, 1f);
                prt.pivot = new Vector2(0f, 1f);
                prt.anchoredPosition = new Vector2(360f + i * 150f, -(y + swH + 4f));
                prt.sizeDelta = new Vector2(120f, nameH);
            }
        }
    }
}
