using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    public static class MenuWidgets
    {
        public static RectTransform Box(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Place(Transform parent, string name, float x, float y, float w, float h)
        {
            var rt = Box(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image Fill(Transform parent, Color color)
        {
            var rt = Box(parent, "Fill", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Words(Transform parent, string text, int size, TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = Box(parent, "Text", anchorMin, anchorMax, new Vector2(0.5f, 0.5f));
            rt.offsetMin = new Vector2(18f, 8f);
            rt.offsetMax = new Vector2(-18f, -8f);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = MenuTheme.Font;
            label.text = text ?? "";
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = align;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = size;
            label.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static MenuTile Tile(Transform parent, float x, float y, float w, float h, int index, string label, string detail, bool allow, Action<int> hover, Action<int> press)
        {
            var rt = Place(parent, "Tile" + index.ToString(), x, y, w, h);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -y - h * 0.5f);
            var shadow = Place(rt, "Shadow", 10f, 12f, w, h);
            var shadowImage = shadow.gameObject.AddComponent<Image>();
            MenuArt.Plate(shadowImage, MenuTheme.Shadow, true);
            shadowImage.raycastTarget = false;

            var strokeRt = Place(rt, "Stroke", -5f, -5f, w + 10f, h + 10f);
            var stroke = strokeRt.gameObject.AddComponent<Image>();
            MenuArt.Plate(stroke, allow ? MenuTheme.Stroke : MenuTheme.Off, true);
            stroke.raycastTarget = false;

            var fill = Place(rt, "Fill", 0f, 0f, w, h);
            var plate = fill.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, allow ? MenuTheme.Panel : MenuTheme.Off, true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var bar = Place(rt, "Bar", 10f, 16f, 12f, h - 32f);
            var barImage = bar.gameObject.AddComponent<Image>();
            MenuArt.Plate(barImage, new Color(1f, 1f, 1f, 0.35f), true);
            barImage.raycastTarget = false;

            var title = Words(rt, label, 40, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0f, 0.42f), new Vector2(1f, 1f));
            var sub = Words(rt, detail, 22, TextAnchor.UpperLeft, MenuTheme.Mute, new Vector2(0f, 0f), new Vector2(1f, 0.48f));
            var tile = rt.gameObject.AddComponent<MenuTile>();
            tile.Plate = plate;
            tile.Stroke = stroke;
            tile.Label = title;
            tile.Detail = sub;
            tile.Bar = barImage;
            tile.Setup(index, label, detail, MenuTheme.Panel, allow, hover, press);
            button.onClick.AddListener(() => tile.Click());
            return tile;
        }

        public static RawImage Thumb(MenuTile tile, Texture tex, float x, float y, float w, float h)
        {
            if (tile == null) return null;
            var rt = Place(tile.transform, "Thumb", x, y, w, h);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = tex != null ? Color.white : new Color(0.15f, 0.35f, 0.7f, 1f);
            raw.raycastTarget = false;
            return raw;
        }

        public static void Mark(MenuTile tile, Sprite icon, Color tint, float size)
        {
            if (tile == null || icon == null) return;
            RectTransform root = tile.transform as RectTransform;
            float h = root != null ? root.sizeDelta.y : size + 20f;
            float s = Mathf.Min(size, h - 16f);
            if (s < 24f) s = 24f;
            var well = Place(tile.transform, "IconWell", 14f, (h - s) * 0.5f, s, s);
            var wellImage = well.gameObject.AddComponent<Image>();
            MenuArt.Plate(wellImage, new Color(tint.r, tint.g, tint.b, 0.9f), true);
            wellImage.raycastTarget = false;
            var iconRt = Place(well, "Icon", 8f, 8f, s - 16f, s - 16f);
            var image = iconRt.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Inset(tile.Label, s + 6f);
            Inset(tile.Detail, s + 6f);
        }

        public static void Portrait(MenuTile tile, Texture tex, Color tint)
        {
            if (tile == null) return;
            RectTransform root = tile.transform as RectTransform;
            float h = root != null ? root.sizeDelta.y : 140f;
            float s = h - 20f;
            var plate = Place(tile.transform, "PortraitPlate", 12f, 10f, s, s);
            var plateImage = plate.gameObject.AddComponent<Image>();
            MenuArt.Plate(plateImage, Color.Lerp(MenuTheme.Ink, tint, 0.35f), true);
            plateImage.raycastTarget = false;
            var shot = Place(plate, "Portrait", 6f, 6f, s - 12f, s - 12f);
            var raw = shot.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = Color.white;
            raw.raycastTarget = false;
            NamePlate(tile, s + 20f);
            tile.Tint(Color.Lerp(MenuTheme.Panel, tint, 0.28f));
        }

        public static void NamePlate(MenuTile tile, float inset)
        {
            if (tile == null) return;
            if (tile.Label != null)
            {
                RectTransform rt = tile.Label.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.40f);
                rt.anchorMax = new Vector2(1f, 0.96f);
                rt.offsetMin = new Vector2(inset, 2f);
                rt.offsetMax = new Vector2(-16f, -2f);
                tile.Label.alignment = TextAnchor.MiddleLeft;
                tile.Label.fontSize = 46;
                tile.Label.resizeTextMaxSize = 46;
                tile.Label.resizeTextMinSize = 16;
            }
            if (tile.Detail != null)
            {
                RectTransform rt = tile.Detail.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.04f);
                rt.anchorMax = new Vector2(1f, 0.40f);
                rt.offsetMin = new Vector2(inset, 2f);
                rt.offsetMax = new Vector2(-16f, -2f);
            }
        }

        public static void Glyph(MenuTile tile, Sprite icon, Color tint)
        {
            if (tile == null || icon == null) return;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 400f;
            float h = root != null ? root.sizeDelta.y : 400f;
            float s = Mathf.Min(220f, h * 0.42f);
            var well = Place(tile.transform, "Device", (w - s) * 0.5f, h * 0.28f, s, s);
            var wellImage = well.gameObject.AddComponent<Image>();
            MenuArt.Plate(wellImage, new Color(tint.r, tint.g, tint.b, 0.95f), true);
            wellImage.raycastTarget = false;
            var iconRt = Place(well, "Glyph", 18f, 18f, s - 36f, s - 36f);
            var image = iconRt.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            if (tile.Label != null)
            {
                RectTransform rt = tile.Label.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.78f);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            if (tile.Detail != null)
            {
                RectTransform rt = tile.Detail.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0.28f);
            }
        }

        public static void Logo(Transform parent, float x, float y, float w, float h, int size)
        {
            var root = Place(parent, "Logo", x, y, w, h);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(x + w * 0.5f, -y - h * 0.5f);
            root.localRotation = Quaternion.Euler(0f, 0f, -8f);
            var streak = Place(root, "Streak", -30f, h * 0.42f, w * 1.15f, h * 0.22f);
            var streakImage = streak.gameObject.AddComponent<Image>();
            streakImage.sprite = MenuArt.Streak;
            streakImage.color = new Color(1f, 0.86f, 0.2f, 0.95f);
            streakImage.raycastTarget = false;
            streak.localRotation = Quaternion.Euler(0f, 0f, -4f);
            int[] ox = { -7, 7, 0, 0, -5, 5, -5, 5 };
            int[] oy = { 0, 0, -7, 7, -5, -5, 5, 5 };
            for (int i = 0; i < ox.Length; i++)
            {
                Text edge = Words(root, "TAG", size, TextAnchor.MiddleCenter, MenuTheme.Stroke, Vector2.zero, Vector2.one);
                edge.alignment = TextAnchor.MiddleCenter;
                edge.rectTransform.anchoredPosition = new Vector2(ox[i], oy[i]);
            }
            Text fill = Words(root, "TAG", size, TextAnchor.MiddleCenter, MenuTheme.Gold, Vector2.zero, Vector2.one);
            fill.alignment = TextAnchor.MiddleCenter;
        }

        static void Inset(Text label, float extra)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 min = rt.offsetMin;
            min.x += extra;
            rt.offsetMin = min;
        }
    }
}
