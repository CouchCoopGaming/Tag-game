using Tag.Settings;
using UnityEngine;

namespace Tag.MatchStats
{
    /// <summary>
    /// Results stat cards. Text size follows the HUD scale. Each seat uses that seat's palette.
    /// Strings were built when the match sealed, so a frame here does not format.
    /// </summary>
    public static class MatchResults
    {
        static GUIStyle _style;
        static bool _ready;

        public static void Paint(float x, float y, float w, float h, float hud)
        {
            if (!MatchBook.Sealed || MatchBook.Count < 1) return;
            Ensure();
            if (_style == null) return;
            int awardSize = MatchBook.CouchFont(30, hud);
            int bodySize = MatchBook.CouchFont(24, hud);
            _style.fontSize = awardSize;
            _style.alignment = TextAnchor.UpperLeft;
            _style.wordWrap = true;
            _style.normal.textColor = Color.white;
            float ay = y;
            int awards = MatchBook.AwardCount;
            for (int a = 0; a < awards; a++)
            {
                int leader = 0;
                int mask = MatchBook.AwardMask[a];
                for (int bit = 0; bit < MatchBook.Cap; bit++)
                {
                    if ((mask & (1 << bit)) != 0)
                    {
                        leader = bit;
                        break;
                    }
                }
                int awardSwatch = Tag.Profiles.LocalProfiles.SeatColor(leader);
                if (awardSwatch < 0) awardSwatch = leader & 3;
                int awardPalette = 0;
                if (GameSettings.Current != null)
                    awardPalette = GameSettings.Current.PaletteOf(leader < AccessibilityPalette.Players ? leader : 0);
                AccessibilityPalette.Player(awardPalette, awardSwatch, out float ar, out float ag, out float ab);
                _style.normal.textColor = new Color(ar, ag, ab, 1f);
                string awardText = MatchBook.AwardLine[a] ?? "";
                int awardRows = MatchBook.AwardRows(awardText, w - 24f, awardSize);
                float awardH = awardRows * (awardSize + 4f);
                GUI.Label(new Rect(x + 12f, ay, w - 24f, awardH), awardText, _style);
                ay += awardH + 8f;
            }
            _style.normal.textColor = Color.white;
            int n = MatchBook.Count;
            int cols = n < 4 ? n : 4;
            if (cols < 1) cols = 1;
            int rows = (n + cols - 1) / cols;
            float gap = 8f;
            float cw = (w - 24f - gap * (cols - 1)) / cols;
            float used = ay - y;
            float ch = (h - used - gap * (rows - 1)) / rows;
            if (ch < bodySize * 4f) ch = bodySize * 4f;
            _style.fontSize = bodySize;
            for (int i = 0; i < n; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float cx = x + 12f + col * (cw + gap);
                float cy = ay + row * (ch + gap);
                int seat = i < AccessibilityPalette.Players ? i : 0;
                int palette = 0;
                if (GameSettings.Current != null)
                    palette = GameSettings.Current.PaletteOf(seat);
                int swatch = Tag.Profiles.LocalProfiles.SeatColor(i);
                if (swatch < 0) swatch = i & 3;
                AccessibilityPalette.Player(palette, swatch, out float r, out float g, out float b);
                _style.normal.textColor = new Color(r, g, b, 1f);
                GUI.Box(new Rect(cx, cy, cw, ch), "");
                GUI.Label(new Rect(cx + 8f, cy + 6f, cw - 16f, ch - 10f), MatchBook.Card[i] ?? "", _style);
            }
        }

        static void Ensure()
        {
            if (_ready && _style != null) return;
            _ready = true;
            if (GUI.skin == null) return;
            _style = new GUIStyle(GUI.skin.label);
            _style.alignment = TextAnchor.UpperLeft;
            _style.wordWrap = true;
        }
    }
}
