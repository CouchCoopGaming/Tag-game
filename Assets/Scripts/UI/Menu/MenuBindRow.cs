using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Each bind row shows the keyboard glyph and the pad glyph together.
    /// Jump's keyboard glyph is the space bar. The pad glyph is South.
    /// </summary>
    public static class MenuBindRow
    {
        public static void Stamp(MenuTile row, int action)
        {
            if (row == null) return;
            RectTransform root = row.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 900f;
            float h = root != null ? root.sizeDelta.y : 88f;
            float s = 56f;
            if (h < 72f) s = 44f;
            float y = (h - s) * 0.5f;
            if (y < 8f) y = 8f;
            Well(row.transform, w - s * 2f - 36f, y, s, MenuIcons.BindOf(PadGlyph.Keyboard, action), MenuTheme.Navy);
            Well(row.transform, w - s - 16f, y, s, MenuIcons.BindOf(PadGlyph.Xbox, action), MenuTheme.Navy);
            Pull(row.Label, 18f, s * 2f + 48f);
            Pull(row.Detail, 18f, s * 2f + 48f);
        }

        public static void JoinPair(MenuTile tile)
        {
            if (tile == null) return;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 360f;
            float h = root != null ? root.sizeDelta.y : 420f;
            float s = 92f;
            if (s > h * 0.28f) s = h * 0.28f;
            if (s < 48f) s = 48f;
            float y = h * 0.36f;
            float gap = 18f;
            float x = (w - s * 2f - gap) * 0.5f;
            if (x < 12f) x = 12f;
            Well(tile.transform, x, y, s, MenuIcons.KeySpace, MenuTheme.Navy);
            Well(tile.transform, x + s + gap, y, s, MenuIcons.South, new Color(0.10f, 0.42f, 0.22f, 1f));
        }

        static void Well(Transform parent, float x, float y, float s, Sprite icon, Color plate)
        {
            if (icon == null) return;
            RectTransform well = MenuWidgets.Place(parent, "BindWell", x, y, s, s);
            Image back = well.gameObject.AddComponent<Image>();
            MenuArt.Plate(back, plate, true);
            back.raycastTarget = false;
            float pad = s * 0.16f;
            RectTransform mark = MenuWidgets.Place(well, "BindGlyph", pad, pad, s - pad * 2f, s - pad * 2f);
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
