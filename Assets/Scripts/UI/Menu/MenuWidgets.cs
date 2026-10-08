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
            var plate = rt.gameObject.AddComponent<Image>();
            plate.color = allow ? MenuTheme.Panel : MenuTheme.Off;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            var bar = Place(rt, "Bar", 0f, 0f, 14f, h);
            var barImage = bar.gameObject.AddComponent<Image>();
            barImage.color = new Color(1f, 1f, 1f, 0.16f);
            barImage.raycastTarget = false;

            var title = Words(rt, label, 36, TextAnchor.MiddleLeft, MenuTheme.Cream, new Vector2(0f, 0.42f), new Vector2(1f, 1f));
            var sub = Words(rt, detail, 22, TextAnchor.UpperLeft, MenuTheme.Mute, new Vector2(0f, 0f), new Vector2(1f, 0.48f));
            var tile = rt.gameObject.AddComponent<MenuTile>();
            tile.Plate = plate;
            tile.Label = title;
            tile.Detail = sub;
            tile.Bar = barImage;
            tile.Setup(index, label, detail, MenuTheme.Panel, allow, hover, press);
            button.onClick.AddListener(() => tile.Click());
            return tile;
        }
    }
}
