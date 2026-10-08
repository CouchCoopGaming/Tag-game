using Tag.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Each bind row shows the keys and the pad button the pawn actually samples.
    /// The pad mark is the Xbox face for that token. West is the blue X.
    /// </summary>
    public static class MenuBindRow
    {
        public const float PadCol = 188f;
        public const float KeyCol = 268f;

        public static void Columns(float rowW, out float keyRight, out float padRight)
        {
            padRight = rowW - 12f;
            keyRight = padRight - PadCol - 16f;
        }

        /// <summary>
        /// Keyboard glyphs sit in the left column. The pad glyph sits in the right column.
        /// Jump's second key is an extra keyboard glyph. Space stays the first one.
        /// </summary>
        public static void Stamp(MenuTile row, int action, ActionBinds keys, string padToken)
        {
            if (row == null) return;
            RectTransform root = row.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 900f;
            float h = root != null ? root.sizeDelta.y : 88f;
            float s = 56f;
            if (h < 72f) s = 44f;
            float y = (h - s) * 0.5f;
            if (y < 8f) y = 8f;
            if (keys == null) keys = ActionBinds.Defaults();
            var act = (PlayAction)action;
            Marks(act, keys, out string key, out string extra, out _);
            if (act == PlayAction.Jump && !string.IsNullOrEmpty(keys.JumpAlt))
                extra = keys.JumpAlt;
            Columns(w, out float keyRight, out float padRight);
            Place(row.transform, padRight, y, s, padToken);
            float keyEdge = keyRight;
            keyEdge = Place(row.transform, keyEdge, y, s, key);
            if (!string.IsNullOrEmpty(extra))
                keyEdge = Place(row.transform, keyEdge, y, s, extra);
            float reserve = w - keyEdge;
            if (reserve < w - keyRight + KeyCol) reserve = w - (keyRight - KeyCol);
            Pull(row.Label, 18f, reserve);
            Pull(row.Detail, 18f, reserve);
        }

        public static void StampToken(MenuTile row, string keyToken, string padToken)
        {
            if (row == null) return;
            RectTransform root = row.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 900f;
            float h = root != null ? root.sizeDelta.y : 88f;
            float s = 56f;
            if (h < 72f) s = 44f;
            float y = (h - s) * 0.5f;
            if (y < 8f) y = 8f;
            Columns(w, out float keyRight, out float padRight);
            if (!string.IsNullOrEmpty(padToken))
                Place(row.transform, padRight, y, s, padToken);
            float keyEdge = keyRight;
            if (!string.IsNullOrEmpty(keyToken))
                keyEdge = Place(row.transform, keyEdge, y, s, keyToken);
            float reserve = w - (keyRight - KeyCol);
            if (keyEdge < keyRight - KeyCol) reserve = w - keyEdge;
            Pull(row.Label, 18f, reserve);
            Pull(row.Detail, 18f, reserve);
        }

        /// <summary>
        /// Stored token, then the extra key PlayerInputReader always ORs in.
        /// Slide's C, air dash's Alt, punch's E, and sprint's Alt are those extras.
        /// </summary>
        public static void Marks(PlayAction action, ActionBinds binds, out string key, out string extra, out string pad)
        {
            if (binds == null) binds = ActionBinds.Defaults();
            int i = (int)action;
            if (i < 0 || i >= binds.Keyboard.Length)
            {
                key = "";
                extra = "";
                pad = "";
                return;
            }
            key = binds.Keyboard[i] ?? "";
            pad = binds.Gamepad[i] ?? "";
            extra = "";
            if (action == PlayAction.Slide && key == "leftCtrl") extra = "c";
            else if (action == PlayAction.AirDash && key != "leftAlt") extra = "leftAlt";
            else if (action == PlayAction.Punch && key != "e") extra = "e";
            else if (action == PlayAction.Sprint && key == "leftShift") extra = "leftAlt";
        }

        static float MarkWidth(string token, float s)
        {
            if (token == "leftCtrl" || token == "leftShift" || token == "rightShift") return s * 1.7f;
            if (token == "leftAlt" || token == "mouseLeft" || token == "mouseRight" || token == "mouseMiddle") return s * 1.45f;
            if (token == "rightShoulder" || token == "leftShoulder" || token == "leftTrigger" || token == "rightTrigger")
                return s * 1.25f;
            return s;
        }

        static float Place(Transform parent, float right, float y, float s, string token)
        {
            float mw = MarkWidth(token, s);
            float x = right - mw;
            Well(parent, x, y, mw, s, MenuIcons.Glyph(token), MenuTheme.Navy);
            return x - 8f;
        }

        public static void JoinPair(MenuTile tile)
        {
            if (tile == null) return;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 360f;
            float h = root != null ? root.sizeDelta.y : 420f;
            float glyph = 64f;
            if (glyph > h * 0.34f) glyph = h * 0.34f;
            float y = h * 0.46f;
            if (y + glyph > h - 20f) y = h - 20f - glyph;
            float gap = 28f;
            float x = (w - glyph * 2f - gap) * 0.5f;
            if (x < 16f) x = 16f;
            JoinMark(tile.transform, x, y, glyph, MenuIcons.KeySpace, MenuTheme.Navy);
            JoinMark(tile.transform, x + glyph + gap, y, glyph, MenuIcons.South, new Color(0.10f, 0.42f, 0.22f, 1f));
        }

        static void JoinMark(Transform parent, float x, float y, float s, Sprite icon, Color plate)
        {
            if (icon == null) return;
            RectTransform well = MenuWidgets.Place(parent, "JoinGlyph", x, y, s, s);
            Image back = well.gameObject.AddComponent<Image>();
            MenuArt.Plate(back, plate, true);
            back.raycastTarget = false;
            RectTransform mark = MenuWidgets.Place(well, "Mark", 0f, 0f, s, s);
            Image image = mark.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void Well(Transform parent, float x, float y, float w, float h, Sprite icon, Color plate)
        {
            if (icon == null) return;
            RectTransform well = MenuWidgets.Place(parent, "BindWell", x, y, w, h);
            Image back = well.gameObject.AddComponent<Image>();
            MenuArt.Plate(back, plate, true);
            back.raycastTarget = false;
            float pad = h * 0.12f;
            RectTransform mark = MenuWidgets.Place(well, "BindGlyph", pad, pad, w - pad * 2f, h - pad * 2f);
            Image image = mark.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void Pull(Text label, float left, float right)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 min = rt.offsetMin;
            Vector2 max = rt.offsetMax;
            if (min.x < left) min.x = left;
            if (max.x > -right) max.x = -right;
            rt.offsetMin = min;
            rt.offsetMax = max;
        }
    }
}
