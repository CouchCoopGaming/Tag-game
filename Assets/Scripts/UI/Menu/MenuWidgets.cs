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
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static MenuTile Tile(Transform parent, float x, float y, float w, float h, int index, string label, string detail, bool allow, Action<int> hover, Action<int> press)
        {
            var rt = Place(parent, "Tile" + index.ToString(), x, y, w, h);
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
    }
}
