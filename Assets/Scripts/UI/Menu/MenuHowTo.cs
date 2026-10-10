using Tag.Modes;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// One rule line and a small diagram for the mode under the cursor.
    /// Shapes are UI images. No new art files.
    /// </summary>
    public static class MenuHowTo
    {
        public static void Show(RectTransform body, TagModeId id)
        {
            if (body == null) return;
            for (int i = body.childCount - 1; i >= 0; i--)
            {
                Transform child = body.GetChild(i);
                if (child.name == "HowTo") Object.DestroyImmediate(child.gameObject);
            }
            UiFit.Columns(UiFit.Current(), out float leftX, out float leftW, out _, out _);
            float modeH = UiFit.BlockH(152f, 4);
            float modeStep = UiFit.IdentityText() ? 168f : modeH + 16f;
            float y = 12f + modeStep + modeH + 12f;
            if (y < 360f) y = 360f;
            float h = 220f;
            float bodyH = UiFit.BodyH(UiFit.Current());
            if (y + h > bodyH - 8f) h = bodyH - y - 8f;
            if (h < 120f) h = 120f;
            if (leftW > 900f) leftW = 900f;
            RectTransform card = MenuWidgets.Place(body, "HowTo", leftX, y, leftW, h);
            Image plate = card.gameObject.AddComponent<Image>();
            MenuArt.Plate(plate, MenuTheme.Navy, true);
            plate.raycastTarget = false;
            Text title = MenuWidgets.Words(card, "How to play", MenuTokens.Section, TextAnchor.MiddleLeft, MenuTheme.Gold, new Vector2(0f, 0.72f), new Vector2(1f, 1f));
            title.alignment = TextAnchor.MiddleLeft;
            title.rectTransform.offsetMin = new Vector2(24f, 0f);
            Text line = MenuWidgets.Words(card, MenuCatalog.ModeBlurb(id), MenuTokens.Body, TextAnchor.UpperLeft, MenuTheme.Cream, new Vector2(0f, 0.48f), new Vector2(1f, 0.74f));
            line.alignment = TextAnchor.MiddleLeft;
            line.rectTransform.offsetMin = new Vector2(24f, 0f);
            Diagram(card, id);
        }

        static void Diagram(RectTransform card, TagModeId id)
        {
            if (id == TagModeId.HotPotato)
            {
                Bar(card, 28f, 150f, 220f, MenuTheme.Gold);
                Bar(card, 268f, 150f, 160f, MenuTheme.Seat(0));
                Bar(card, 448f, 150f, 110f, MenuTheme.Seat(1));
                Dot(card, 640f, 136f, 56f, MenuTheme.Seat(2));
                return;
            }
            if (id == TagModeId.TrailTag)
            {
                for (int i = 0; i < 6; i++)
                    Bar(card, 28f + i * 90f, 158f - (i % 2) * 18f, 70f, i == 5 ? MenuTheme.Off : MenuTheme.Seat(3));
                return;
            }
            if (id == TagModeId.FreePlay)
            {
                Dot(card, 48f, 140f, 48f, MenuTheme.Seat(0));
                Dot(card, 220f, 140f, 48f, MenuTheme.Seat(1));
                Bar(card, 110f, 158f, 90f, MenuTheme.Cream);
                return;
            }
            Bar(card, 28f, 158f, 420f, MenuTheme.Seat(0));
            Dot(card, 470f, 146f, 36f, MenuTheme.Gold);
        }

        static void Bar(RectTransform card, float x, float y, float w, Color color)
        {
            RectTransform rt = MenuWidgets.Place(card, "Mark", x, y, w, 28f);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        static void Dot(RectTransform card, float x, float y, float s, Color color)
        {
            RectTransform rt = MenuWidgets.Place(card, "Mark", x, y, s, s);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
