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
            int drawn = UiFit.TextPx(size < UiFit.FloorFont ? UiFit.FloorFont : size);
            int minPx = UiFit.TextPx(UiFit.FloorFont);
            if (minPx > drawn) minPx = drawn;
            label.font = MenuTheme.Font;
            label.text = text ?? "";
            label.fontSize = drawn;
            label.fontStyle = FontStyle.Bold;
            label.alignment = align;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = minPx;
            label.resizeTextMaxSize = drawn;
            label.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return label;
        }

        public static Text Heading(Transform parent, string text, int size, TextAnchor align, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            Text label = Words(parent, text, size, align, color, anchorMin, anchorMax);
            label.font = MenuTheme.Display;
            return label;
        }

        public static MenuTile Tile(Transform parent, float x, float y, float w, float h, int index, string label, string detail, bool allow, Action<int> hover, Action<int> press)
        {
            var rt = Place(parent, "Tile" + index.ToString(), x, y, w, h);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + w * 0.5f, -y - h * 0.5f);
            var shadow = Place(rt, "Shadow", -8f, 10f, w + 16f, h + 28f);
            var shadowImage = shadow.gameObject.AddComponent<Image>();
            shadowImage.sprite = MenuArt.Soft;
            shadowImage.color = new Color(0f, 0f, 0f, 0.42f);
            shadowImage.raycastTarget = false;

            var strokeRt = Place(rt, "Stroke", -5f, -5f, w + 10f, h + 10f);
            var stroke = strokeRt.gameObject.AddComponent<Image>();
            MenuArt.Plate(stroke, allow ? MenuTheme.Stroke : MenuTheme.Off, true);
            stroke.raycastTarget = false;

            var fill = Place(rt, "Fill", 0f, 0f, w, h);
            var plate = fill.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, allow ? MenuTheme.Panel : MenuTheme.Off, true);
            var sheen = Place(rt, "Sheen", 8f, UiFit.StripeY, w - 16f, UiFit.StripeH);
            var sheenImage = sheen.gameObject.AddComponent<Image>();
            sheenImage.sprite = MenuArt.Sheen;
            sheenImage.color = new Color(1f, 1f, 1f, 0.55f);
            sheenImage.raycastTarget = false;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            float barTop = UiFit.StripeClear() + 2f;
            var bar = Place(rt, "Bar", 10f, barTop, 12f, h - barTop - 8f);
            var barImage = bar.gameObject.AddComponent<Image>();
            MenuArt.Plate(barImage, new Color(1f, 1f, 1f, 0.35f), true);
            barImage.raycastTarget = false;

            bool two = !string.IsNullOrEmpty(detail);
            UiFit.TileText(h, two, out float titleFromTop, out float titleH, out float detailFromTop, out float detailH);
            var title = Words(rt, label, 40, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
            title.font = MenuTheme.Display;
            var sub = Words(rt, detail, UiFit.FloorFont, TextAnchor.MiddleLeft, MenuTheme.Mute, Vector2.zero, Vector2.one);
            int titlePx = UiFit.TextPx(40);
            int detailPx = UiFit.TextPx(UiFit.FloorFont);
            bool identityType = titlePx == 40 && detailPx == UiFit.FloorFont;
            bool typedRow = h >= UiFit.OptRow && h <= UiFit.RematchH && identityType;
            if (typedRow)
            {
                title.resizeTextForBestFit = false;
                title.fontSize = titlePx;
                title.resizeTextMaxSize = titlePx;
                sub.resizeTextForBestFit = false;
                sub.fontSize = detailPx;
                sub.resizeTextMaxSize = detailPx;
                sub.resizeTextMinSize = detailPx;
            }
            else if (!identityType)
            {
                if (titleH + 1f >= titlePx)
                {
                    title.resizeTextForBestFit = false;
                    title.fontSize = titlePx;
                    title.resizeTextMaxSize = titlePx;
                    title.resizeTextMinSize = titlePx;
                }
                if (!two || detailH + 1f >= detailPx)
                {
                    sub.resizeTextForBestFit = false;
                    sub.fontSize = detailPx;
                    sub.resizeTextMaxSize = detailPx;
                    sub.resizeTextMinSize = detailPx;
                }
            }
            Band(title, h, titleFromTop, titleH);
            Band(sub, h, detailFromTop, detailH);
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
            float s = Mathf.Min(size, h - UiFit.StripeClear() - 8f);
            if (s < 24f) s = 24f;
            float iconY = (h - s) * 0.5f;
            if (iconY < UiFit.StripeClear()) iconY = UiFit.StripeClear();
            var well = Place(tile.transform, "IconWell", 14f, iconY, s, s);
            var wellImage = well.gameObject.AddComponent<Image>();
            MenuArt.Plate(wellImage, new Color(tint.r, tint.g, tint.b, 0.9f), false);
            wellImage.type = Image.Type.Simple;
            wellImage.raycastTarget = false;
            var iconRt = Place(well, "Icon", 8f, 8f, s - 16f, s - 16f);
            var image = iconRt.gameObject.AddComponent<Image>();
            image.sprite = icon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            float left = 14f + s + 16f;
            ClearLabel(tile.Label, left);
            ClearLabel(tile.Detail, left);
            if (tile.Label != null) tile.Label.transform.SetAsLastSibling();
            if (tile.Detail != null) tile.Detail.transform.SetAsLastSibling();
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
            tile.Tint(Color.Lerp(MenuTheme.Panel, tint, UiSweep.PortraitMix));
        }

        public static void NamePlate(MenuTile tile, float inset)
        {
            if (tile == null) return;
            if (tile.Label != null)
            {
                RectTransform root = tile.transform as RectTransform;
                float h = root != null ? root.sizeDelta.y : 140f;
                if (h < 1f) h = 140f;
                float top = UiFit.StripeClear() / h;
                RectTransform rt = tile.Label.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.40f);
                rt.anchorMax = new Vector2(1f, 1f - top);
                rt.offsetMin = new Vector2(inset, 2f);
                rt.offsetMax = new Vector2(-16f, -2f);
                tile.Label.alignment = TextAnchor.MiddleLeft;
                int namePx = UiFit.TextPx(46);
                tile.Label.fontSize = namePx;
                tile.Label.resizeTextMaxSize = namePx;
                tile.Label.resizeTextMinSize = UiFit.TextPx(UiFit.FloorFont);
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

        /// <summary>
        /// Drop-in card. The plate and the stripe stay the seat tint. Focus is the gold
        /// stroke only. A joined seat gets a bust in the slot color and a Ready or Joined chip.
        /// </summary>
        public static void JoinDress(MenuTile tile, Color seat, bool joined, bool ready, string profile, string deviceLine, int index)
        {
            if (tile == null) return;
            tile.LockColors = true;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 400f;
            float h = root != null ? root.sizeDelta.y : 420f;
            if (tile.Label != null) Band(tile.Label, h, 22f, 36f);
            if (!joined)
            {
                float side = h * 0.42f;
                if (side > 220f) side = 220f;
                if (side < 120f) side = 120f;
                if (side > w * 0.78f) side = w * 0.78f;
                float bustY = 86f;
                if (!HierSeat(tile.transform, (w - side) * 0.5f, bustY, side, index))
                {
                    Ghost(tile.transform, (w - side * 0.72f) * 0.5f, bustY, side * 0.72f, side, seat);
                    float mark = 76f;
                    float mx = (w - mark) * 0.5f;
                    float my = bustY + side * 0.38f;
                    var well = Place(tile.transform, "GhostWell", mx, my, mark, mark);
                    var wellImage = well.gameObject.AddComponent<Image>();
                    wellImage.color = new Color(0.02f, 0.02f, 0.04f, 0.85f);
                    wellImage.raycastTarget = false;
                    SeatShape.StampKind(well, MenuMannequin.Shape(index), 10f, 10f, mark - 20f, MenuTheme.SeatFill(index));
                }
                if (tile.Detail != null)
                {
                    Band(tile.Detail, h, bustY + side + 10f, 44f);
                    tile.Detail.alignment = TextAnchor.MiddleCenter;
                }
                return;
            }
            if (tile.Detail != null) tile.Detail.text = "";
            float bustH = h * 0.40f;
            float joinedY = 62f;
            if (joinedY + bustH > h - 148f) bustH = h - 148f - joinedY;
            if (bustH < 96f) bustH = 96f;
            if (bustH > w * 0.78f) bustH = w * 0.78f;
            if (!HierSeat(tile.transform, (w - bustH) * 0.5f, joinedY, bustH, index))
                Bust(tile.transform, (w - bustH * 0.72f) * 0.5f, joinedY, bustH * 0.72f, bustH, seat);
            string name = string.IsNullOrEmpty(profile) ? "" : "<  " + profile + "  >";
            var nameRt = Place(tile.transform, "ProfileName", 16f, joinedY + bustH + 6f, w - 32f, 36f);
            Words(nameRt, name, 28, TextAnchor.MiddleCenter, MenuTheme.Cream, Vector2.zero, Vector2.one);
            float chipH = 40f;
            float chipY = h - chipH - 14f;
            var hint = Place(tile.transform, "ReadyHint", 16f, chipY - 68f, w - 32f, 26f);
            Words(hint, "Y  Ready", 22, TextAnchor.MiddleCenter, MenuTheme.Mute, Vector2.zero, Vector2.one);
            DeviceLine(tile.transform, w, chipY - 38f, deviceLine);
            string word = ready ? "Ready" : "Joined";
            Color plate = ready ? MenuTheme.Gold : MenuTheme.Navy;
            Color ink = ready ? MenuTheme.Ink : MenuTheme.Cream;
            var chip = Place(tile.transform, "ReadyChip", (w - 168f) * 0.5f, chipY, 168f, chipH);
            var chipImage = chip.gameObject.AddComponent<Image>();
            MenuArt.Plate(chipImage, plate, true);
            chipImage.raycastTarget = false;
            Words(chip, word, 28, TextAnchor.MiddleCenter, ink, Vector2.zero, Vector2.one);
        }

        /// <summary>
        /// Seat mark on a drop-in card. The kind is <see cref="MenuMannequin.Shape"/>.
        /// A dark well keeps the fill readable at couch size.
        /// </summary>
        public static void JoinShape(MenuTile tile, int seat)
        {
            if (tile == null) return;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 400f;
            const float size = 88f;
            var well = Place(tile.transform, "JoinWell", w - size - 16f, 14f, size, size);
            var back = well.gameObject.AddComponent<Image>();
            back.color = new Color(0.02f, 0.02f, 0.04f, 1f);
            back.raycastTarget = false;
            SeatShape.StampKind(well, MenuMannequin.Shape(seat), 10f, 10f, size - 20f, MenuTheme.SeatFill(seat));
        }

        /// <summary>
        /// Head, chest, and legs in the seat color. A joined drop-in card uses this
        /// instead of a tiny device mark.
        /// </summary>
        /// <summary>Seat Hier idle, square so the disc is not stretched. False when the atlas is missing.</summary>
        public static bool HierSeat(Transform parent, float x, float y, float side, int index)
        {
            Texture tex = MenuBackdrop.SeatLoad;
            if (parent == null || tex == null) return false;
            if (index < 0) index = 0;
            if (index > 3) index = 3;
            RectTransform rt = Place(parent, "HierSeat", x, y, side, side);
            RawImage raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(index * 0.25f, 0f, 0.25f, 1f);
            return true;
        }

        /// <summary>
        /// Results figure. Four rows, top to bottom in the file: celebrate, fist pump,
        /// weight shift, slump. Unity's v origin is the bottom, so rank 0 is v 0.75.
        /// The cell is square and the same size for every seat, so the body stays one height.
        /// Falls back to the idle atlas when the results art is missing.
        /// </summary>
        public static bool HierRank(Transform parent, float x, float y, float side, int seat, int rank)
        {
            Texture tex = MenuBackdrop.SeatResult;
            if (parent == null || tex == null) return HierSeat(parent, x, y, side, seat);
            if (seat < 0) seat = 0;
            if (seat > 3) seat = 3;
            if (rank < 0) rank = 0;
            if (rank > 3) rank = 3;
            float row = (3 - rank) * 0.25f;
            RectTransform rt = Place(parent, "HierRank", x, y, side, side);
            RawImage raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(seat * 0.25f, row, 0.25f, 0.25f);
            return true;
        }

        /// <summary>Empty drop-in seat. A faint body with a light outline, not a blank slab.</summary>
        public static void Ghost(Transform parent, float x, float y, float w, float h, Color seat)
        {
            if (parent == null) return;
            Color line = Color.Lerp(seat, Color.white, 0.45f);
            line.a = 0.72f;
            const float o = 4f;
            Bust(parent, x - o, y, w, h, line);
            Bust(parent, x + o, y, w, h, line);
            Bust(parent, x, y - o, w, h, line);
            Bust(parent, x, y + o, w, h, line);
            Color fill = seat;
            fill.a = 0.18f;
            Bust(parent, x, y, w, h, fill);
        }

        public static void Bust(Transform parent, float x, float y, float w, float h, Color body)
        {
            if (parent == null || w < 8f || h < 8f) return;
            var rt = Place(parent, "Bust", x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = MenuIcons.HierBust;
            image.color = body;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        static void DeviceLine(Transform parent, float w, float y, string word)
        {
            if (parent == null || string.IsNullOrEmpty(word)) return;
            bool keyboard = word == "Keyboard";
            float icon = 26f;
            float textW = keyboard ? 132f : 124f;
            float gap = 8f;
            float group = icon + gap + textW;
            float x = (w - group) * 0.5f;
            if (x < 8f) x = 8f;
            var iconRt = Place(parent, "DeviceIcon", x, y + 3f, icon, icon);
            var iconImage = iconRt.gameObject.AddComponent<Image>();
            iconImage.sprite = keyboard ? MenuIcons.Keys : MenuIcons.Pad;
            iconImage.color = MenuTheme.Cream;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var text = Place(parent, "DeviceLine", x + icon + gap, y, textW, 32f);
            Words(text, word, 26, TextAnchor.MiddleLeft, MenuTheme.Cream, Vector2.zero, Vector2.one);
        }

        /// <summary>Empty records card. A pedestal and a gold cup, not a stack of blank rows.</summary>
        public static void EmptyMark(Transform parent, float x, float y, float s)
        {
            if (parent == null) return;
            float bowlX = x + s * 0.22f;
            float bowlY = y + s * 0.08f;
            float bowlW = s * 0.56f;
            float bowlH = s * 0.40f;
            Plate(parent, "CupRim", x + s * 0.16f, y, s * 0.68f, s * 0.14f, MenuTheme.Gold);
            Plate(parent, "CupBowl", bowlX, bowlY, bowlW, bowlH, MenuTheme.Cream);
            Handle(parent, "CupLeft", x, bowlY + s * 0.06f, s * 0.20f, bowlH * 0.72f);
            Handle(parent, "CupRight", x + s * 0.80f, bowlY + s * 0.06f, s * 0.20f, bowlH * 0.72f);
            Plate(parent, "CupStem", x + s * 0.44f, y + s * 0.50f, s * 0.12f, s * 0.24f, MenuTheme.Gold);
            Plate(parent, "CupBase", x + s * 0.18f, y + s * 0.74f, s * 0.64f, s * 0.16f, MenuTheme.Gold);
        }

        static void Plate(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rt = Place(parent, name, x, y, w, h);
            var image = rt.gameObject.AddComponent<Image>();
            MenuArt.Plate(image, color, true);
            image.raycastTarget = false;
        }

        static void Handle(Transform parent, string name, float x, float y, float w, float h)
        {
            var outer = Place(parent, name, x, y, w, h);
            var gold = outer.gameObject.AddComponent<Image>();
            MenuArt.Plate(gold, MenuTheme.Gold, true);
            gold.raycastTarget = false;
            float inset = w * 0.34f;
            var hole = Place(outer, "Hole", inset, h * 0.18f, w - inset * 2f, h * 0.64f);
            var navy = hole.gameObject.AddComponent<Image>();
            MenuArt.Plate(navy, MenuTheme.Navy, true);
            navy.raycastTarget = false;
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
                float labelH = 52f;
                float top = UiFit.StripeClear();
                if (h >= 96f) top += 8f;
                RectTransform rt = tile.Label.rectTransform;
                rt.anchorMin = new Vector2(0f, 1f - (top + labelH) / h);
                rt.anchorMax = new Vector2(1f, 1f - top / h);
                rt.offsetMin = new Vector2(18f, 0f);
                rt.offsetMax = new Vector2(-18f, 0f);
                tile.Label.alignment = TextAnchor.MiddleCenter;
            }
            if (tile.Detail != null)
            {
                RectTransform rt = tile.Detail.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0.28f);
            }
        }

        public static RectTransform Logo(Transform parent, float x, float y, float w, float h, int size)
        {
            var root = Place(parent, "Logo", x, y, w, h);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = new Vector2(x + w * 0.5f, -y - h * 0.5f);
            Texture2D lockup = MenuBackdrop.Lockup;
            if (lockup != null)
            {
                root.localRotation = Quaternion.identity;
                var plate = Box(root, "Lockup", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
                var raw = plate.gameObject.AddComponent<RawImage>();
                raw.texture = lockup;
                raw.raycastTarget = false;
                raw.color = Color.white;
                return root;
            }
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
                Text edge = Heading(root, "TAG", size, TextAnchor.MiddleCenter, MenuTheme.Stroke, Vector2.zero, Vector2.one);
                edge.alignment = TextAnchor.MiddleCenter;
                edge.rectTransform.anchoredPosition = new Vector2(ox[i], oy[i]);
            }
            Text fill = Heading(root, "TAG", size, TextAnchor.MiddleCenter, MenuTheme.Gold, Vector2.zero, Vector2.one);
            fill.alignment = TextAnchor.MiddleCenter;
            return root;
        }

        /// <summary>
        /// On/off pill. Gold track and the knob on the right means on.
        /// </summary>
        public static void Toggle(MenuTile tile, bool on)
        {
            if (tile == null) return;
            RectTransform root = tile.transform as RectTransform;
            float w = root != null ? root.sizeDelta.x : 900f;
            float h = root != null ? root.sizeDelta.y : UiFit.OptRow;
            float pw = 96f;
            float ph = 40f;
            float x = w - pw - 28f;
            float y = 24f;
            if (h > ph + 48f) y = (h - ph) * 0.5f;
            var track = Place(tile.transform, "Switch", x, y, pw, ph);
            var trackImage = track.gameObject.AddComponent<Image>();
            MenuArt.Plate(trackImage, on ? MenuTheme.Gold : new Color(0.05f, 0.08f, 0.16f, 1f), true);
            trackImage.raycastTarget = false;
            float knob = 32f;
            float kx = on ? pw - knob - 4f : 4f;
            var knobRt = Place(track, "Knob", kx, 4f, knob, ph - 8f);
            var knobImage = knobRt.gameObject.AddComponent<Image>();
            MenuArt.Plate(knobImage, MenuTheme.Cream, true);
            knobImage.raycastTarget = false;
            ClearRight(tile.Label, pw + 40f);
            ClearRight(tile.Detail, pw + 40f);
        }

        static void ClearRight(Text label, float right)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 max = rt.offsetMax;
            if (max.x > -right) max.x = -right;
            rt.offsetMax = max;
        }

        public static void Reflow(MenuTile tile)
        {
            if (tile == null || tile.Label == null) return;
            RectTransform root = tile.transform as RectTransform;
            if (root == null) return;
            float h = root.sizeDelta.y;
            if (h < 1f) return;
            bool two = false;
            if (tile.Detail != null && !string.IsNullOrEmpty(tile.Detail.text))
            {
                string text = tile.Detail.text;
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] != ' ')
                    {
                        two = true;
                        break;
                    }
                }
            }
            UiFit.TileText(h, two, out float titleFromTop, out float titleH, out float detailFromTop, out float detailH);
            Band(tile.Label, h, titleFromTop, titleH);
            if (tile.Detail != null) Band(tile.Detail, h, detailFromTop, detailH);
        }

        public static void SeatLine(Text label, float h, float fromTop, float band, float left)
        {
            Band(label, h, fromTop, band);
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 min = rt.offsetMin;
            min.x = left;
            rt.offsetMin = min;
        }

        static void Band(Text label, float h, float fromTop, float band)
        {
            if (label == null || h < 1f) return;
            if (band < 0f) band = 0f;
            float yMax = (h - fromTop) / h;
            float yMin = (h - fromTop - band) / h;
            if (yMax > 1f) yMax = 1f;
            if (yMin < 0f) yMin = 0f;
            RectTransform rt = label.rectTransform;
            rt.anchorMin = new Vector2(0f, yMin);
            rt.anchorMax = new Vector2(1f, yMax);
            rt.offsetMin = new Vector2(18f, 0f);
            rt.offsetMax = new Vector2(-18f, 0f);
            label.alignment = TextAnchor.MiddleLeft;
        }

        static void ClearLabel(Text label, float left)
        {
            if (label == null) return;
            RectTransform rt = label.rectTransform;
            Vector2 min = rt.offsetMin;
            if (min.x < left) min.x = left;
            rt.offsetMin = min;
        }
    }
}
