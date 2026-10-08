using Tag.Ui.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Hud
{
    /// <summary>
    /// Builds the match canvas once. Gameplay frames do not call this.
    /// </summary>
    public static class MatchHudView
    {
        public static void Build(MatchHud hud)
        {
            var go = new GameObject("MatchHudCanvas", typeof(RectTransform));
            go.transform.SetParent(hud.transform, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            hud.Root = canvas;
            RectTransform root = go.GetComponent<RectTransform>();

            RectTransform plateRt = MenuWidgets.Box(root, "ClockPlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            plateRt.sizeDelta = new Vector2(440f, 112f);
            plateRt.anchoredPosition = new Vector2(0f, -8f);
            Image plate = plateRt.gameObject.AddComponent<Image>();
            plate.sprite = MenuArt.Round;
            plate.type = Image.Type.Sliced;
            plate.color = new Color(0.04f, 0.08f, 0.16f, 0.90f);
            plate.raycastTarget = false;
            hud.Clock = Label(plateRt, "Clock", 60, TextAnchor.MiddleCenter, new Vector2(0f, 0.36f), new Vector2(1f, 1f));
            hud.RoundLabel = Label(plateRt, "Round", 28, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.40f));
            hud.RoundLabel.color = MenuTheme.Gold;

            RectTransform center = MenuWidgets.Box(root, "Center", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            center.sizeDelta = new Vector2(820f, 188f);
            hud.CenterPlate = center.gameObject.AddComponent<Image>();
            hud.CenterPlate.sprite = MenuArt.Round;
            hud.CenterPlate.type = Image.Type.Sliced;
            hud.CenterPlate.color = new Color(0.04f, 0.07f, 0.14f, 0.78f);
            hud.CenterPlate.raycastTarget = false;
            hud.CenterCall = Label(center, "Call", 96, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            hud.CenterPlate.enabled = false;
            hud.CenterCall.enabled = false;

            for (int i = 0; i < 4; i++)
                hud.Panes[i] = BuildPane(root, i);
            hud.ScoreRoot = BuildScore(root, hud);
        }

        static HudPane BuildPane(Transform root, int index)
        {
            var pane = new HudPane();
            RectTransform rt = MenuWidgets.Box(root, "Pane" + index.ToString(), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            pane.Root = rt;
            pane.Glow = Stretch(rt, "Glow", new Color(1f, 0.84f, 0.12f, 0f));
            pane.EdgeT = Bar(rt, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 10f));
            pane.EdgeB = Bar(rt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f));
            pane.EdgeL = Bar(rt, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(10f, 0f));
            pane.EdgeR = Bar(rt, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(10f, 0f));

            RectTransform badge = MenuWidgets.Place(rt, "Badge", 16f, 12f, 108f, 52f);
            pane.Badge = badge.gameObject.AddComponent<Image>();
            pane.Badge.sprite = MenuArt.Round;
            pane.Badge.type = Image.Type.Sliced;
            pane.Badge.color = MenuTheme.Gold;
            pane.Badge.raycastTarget = false;
            pane.BadgeWord = Label(badge, "It", 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            pane.BadgeWord.color = MenuTheme.Ink;
            pane.BadgeWord.text = MatchHudText.It;
            Outline badgeEdge = pane.BadgeWord.GetComponent<Outline>();
            if (badgeEdge != null) badgeEdge.effectColor = new Color(1f, 0.98f, 0.9f, 0.9f);

            pane.Identity = MenuWidgets.Place(rt, "Identity", 136f, 12f, 480f, 128f);
            pane.Name = Label(pane.Identity, "Name", 34, TextAnchor.MiddleLeft, new Vector2(0f, 0.66f), new Vector2(1f, 1f));
            pane.Profile = Label(pane.Identity, "Profile", 20, TextAnchor.MiddleLeft, new Vector2(0f, 0.36f), new Vector2(1f, 0.68f));
            pane.Profile.color = MenuTheme.Mute;
            pane.Metric = Label(pane.Identity, "Metric", 18, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.42f, 0.38f));
            pane.Metric.color = MenuTheme.Gold;
            pane.Value = Label(pane.Identity, "Value", 22, TextAnchor.MiddleLeft, new Vector2(0.40f, 0f), new Vector2(1f, 0.38f));

            pane.Call = Label(rt, "Call", 64, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.68f));
            pane.Call.enabled = false;

            pane.Verbs = MenuWidgets.Place(rt, "Verbs", 16f, 0f, 300f, 108f);
            pane.Verbs.anchorMin = new Vector2(0f, 0f);
            pane.Verbs.anchorMax = new Vector2(0f, 0f);
            pane.Verbs.pivot = new Vector2(0f, 0f);
            pane.Verbs.anchoredPosition = new Vector2(16f, 16f);
            BuildVerb(pane.Verbs, 0f, out pane.DashBg, out pane.DashFill, out pane.DashWord);
            BuildVerb(pane.Verbs, 100f, out pane.RopeMark, out Image ropeFill, out pane.RopeWord);
            if (ropeFill != null) ropeFill.enabled = false;
            BuildVerb(pane.Verbs, 200f, out pane.SafeBg, out pane.SafeFill, out pane.SafeWord);
            pane.DashFill.color = new Color(0.25f, 0.62f, 1f, 1f);
            pane.SafeFill.color = new Color(0.95f, 0.78f, 0.20f, 1f);
            pane.RopeMark.color = new Color(0.55f, 0.62f, 0.72f, 1f);

            RectTransform arrowRt = MenuWidgets.Box(root, "Arrow" + index.ToString(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            arrowRt.sizeDelta = new Vector2(46f, 46f);
            pane.Arrow = arrowRt.gameObject.AddComponent<Image>();
            pane.Arrow.sprite = MatchHudArt.Arrow;
            pane.Arrow.color = MenuTheme.Gold;
            pane.Arrow.raycastTarget = false;
            pane.Arrow.enabled = false;
            return pane;
        }

        static void BuildVerb(Transform parent, float x, out Image back, out Image fill, out Text word)
        {
            RectTransform rt = MenuWidgets.Place(parent, "Verb", x, 0f, 92f, 108f);
            RectTransform disc = MenuWidgets.Place(rt, "Disc", 14f, 8f, 64f, 64f);
            back = disc.gameObject.AddComponent<Image>();
            back.sprite = MatchHudArt.Disc;
            back.color = new Color(0.10f, 0.12f, 0.16f, 0.92f);
            back.raycastTarget = false;
            RectTransform pie = MenuWidgets.Place(disc, "Pie", 0f, 0f, 64f, 64f);
            fill = pie.gameObject.AddComponent<Image>();
            fill.sprite = MatchHudArt.Disc;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            word = Label(rt, "Word", 16, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0.36f));
        }

        static RectTransform BuildScore(Transform root, MatchHud hud)
        {
            RectTransform rt = MenuWidgets.Box(root, "Score", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image plate = rt.gameObject.AddComponent<Image>();
            plate.sprite = MenuArt.Round;
            plate.type = Image.Type.Sliced;
            plate.color = new Color(0.05f, 0.10f, 0.20f, 0.92f);
            plate.raycastTarget = false;
            hud.ScoreTitle = Label(rt, "Title", 32, TextAnchor.MiddleLeft, new Vector2(0f, 0.82f), new Vector2(1f, 1f));
            hud.ScoreTitle.text = MatchHudText.Score;
            for (int i = 0; i < 4; i++)
            {
                float top = 0.80f - i * 0.18f;
                hud.ScoreLine[i] = Label(rt, "Line" + i.ToString(), 24, TextAnchor.MiddleLeft, new Vector2(0f, top - 0.16f), new Vector2(1f, top));
            }
            rt.gameObject.SetActive(false);
            return rt;
        }

        static Image Stretch(Transform parent, string name, Color color)
        {
            RectTransform rt = MenuWidgets.Box(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Image Bar(Transform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
        {
            RectTransform rt = MenuWidgets.Box(parent, name, min, max, pivot);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            Image image = rt.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        static Text Label(Transform parent, string name, int size, TextAnchor align, Vector2 min, Vector2 max)
        {
            RectTransform rt = MenuWidgets.Box(parent, name, min, max, new Vector2(0.5f, 0.5f));
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = new Vector2(-8f, 0f);
            Text text = rt.gameObject.AddComponent<Text>();
            text.font = MenuTheme.Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.color = MenuTheme.Cream;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            text.text = MatchHudText.Blank;
            Outline outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.92f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
