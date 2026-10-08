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
            scaler.referenceResolution = new Vector2(UiFit.RefW, UiFit.RefH);
            scaler.matchWidthOrHeight = 0.5f;
            hud.Root = canvas;
            hud.Scaler = scaler;
            RectTransform root = go.GetComponent<RectTransform>();

            RectTransform plateRt = MenuWidgets.Box(root, "ClockPlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            plateRt.sizeDelta = new Vector2(HudCorner.ClockW, HudCorner.ClockH);
            plateRt.anchoredPosition = new Vector2(0f, -UiFit.SafeY);
            Image plate = plateRt.gameObject.AddComponent<Image>();
            plate.sprite = MenuArt.Round;
            plate.type = Image.Type.Sliced;
            plate.color = new Color(0.04f, 0.08f, 0.16f, 0.90f);
            plate.raycastTarget = false;
            hud.Clock = Label(plateRt, "Clock", 46, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.08f), new Vector2(0.64f, 0.92f), true);
            hud.RoundLabel = Label(plateRt, "Round", UiFit.FloorFont, TextAnchor.MiddleCenter, new Vector2(0.58f, 0.12f), new Vector2(0.98f, 0.88f));
            hud.RoundLabel.color = MenuTheme.Gold;

            RectTransform center = MenuWidgets.Box(root, "Center", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            center.sizeDelta = new Vector2(820f, 188f);
            hud.CenterPlate = center.gameObject.AddComponent<Image>();
            hud.CenterPlate.sprite = MenuArt.Round;
            hud.CenterPlate.type = Image.Type.Sliced;
            hud.CenterPlate.color = new Color(0.04f, 0.07f, 0.14f, 0.78f);
            hud.CenterPlate.raycastTarget = false;
            hud.CenterCall = Label(center, "Call", 96, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, true);
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
            pane.SafeGlow = Stretch(rt, "SafeGlow", new Color(1f, 0.92f, 0.55f, 0f));
            pane.SafeGlow.enabled = false;
            pane.EdgeT = Bar(rt, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 10f));
            pane.EdgeB = Bar(rt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f));
            pane.EdgeL = Bar(rt, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(10f, 0f));
            pane.EdgeR = Bar(rt, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(10f, 0f));
            RectTransform itFrame = MenuWidgets.Box(rt, "ItFrame", new Vector2(0.018f, 0.025f), new Vector2(0.982f, 0.975f), new Vector2(0.5f, 0.5f));
            Color itCue = new Color(1f, 0.86f, 0.2f, 1f);
            Bar(itFrame, "T", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 8f)).color = itCue;
            Bar(itFrame, "B", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f)).color = itCue;
            Bar(itFrame, "L", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(8f, 0f)).color = itCue;
            Bar(itFrame, "R", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(8f, 0f)).color = itCue;
            pane.ItFrame = itFrame.gameObject.AddComponent<CanvasGroup>();
            pane.ItFrame.alpha = 0f;
            pane.ItFrame.blocksRaycasts = false;
            pane.ItFrame.interactable = false;
            itFrame.gameObject.SetActive(false);

            RectTransform badge = MenuWidgets.Place(rt, "Badge", 16f, 12f, 108f, 52f);
            pane.Badge = badge.gameObject.AddComponent<Image>();
            pane.Badge.sprite = MenuArt.Round;
            pane.Badge.type = Image.Type.Sliced;
            pane.Badge.color = MenuTheme.Gold;
            pane.Badge.raycastTarget = false;
            pane.BadgeWord = Label(badge, "It", UiFit.FloorFont, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, true);
            pane.BadgeWord.color = MenuTheme.Ink;
            pane.BadgeWord.text = MatchHudText.It;
            RectTransform itPlate = MenuWidgets.Place(rt, "ItMark", 0f, 0f, 240f, 108f);
            itPlate.anchorMin = new Vector2(0.5f, 0.62f);
            itPlate.anchorMax = new Vector2(0.5f, 0.62f);
            itPlate.pivot = new Vector2(0.5f, 0.5f);
            pane.ItPlate = itPlate.gameObject.AddComponent<Image>();
            pane.ItPlate.sprite = MenuArt.Round;
            pane.ItPlate.type = Image.Type.Sliced;
            pane.ItPlate.color = MenuTheme.Gold;
            pane.ItPlate.raycastTarget = false;
            pane.ItPlate.enabled = false;
            pane.ItBig = Label(itPlate, "ItBig", 72, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, true);
            pane.ItBig.color = MenuTheme.Ink;
            pane.ItBig.text = MatchHudText.It;
            pane.ItBig.enabled = false;
            RectTransform clockRt = MenuWidgets.Place(rt, "PaneClock", 0f, 0f, 220f, 64f);
            clockRt.anchorMin = new Vector2(0.5f, 1f);
            clockRt.anchorMax = new Vector2(0.5f, 1f);
            clockRt.pivot = new Vector2(0.5f, 1f);
            Image clockPlate = clockRt.gameObject.AddComponent<Image>();
            clockPlate.sprite = MenuArt.Round;
            clockPlate.type = Image.Type.Sliced;
            clockPlate.color = new Color(0.04f, 0.07f, 0.16f, 0.90f);
            clockPlate.raycastTarget = false;
            pane.Timer = Label(clockRt, "Time", 36, TextAnchor.MiddleCenter, new Vector2(0.02f, 0.08f), new Vector2(0.62f, 0.92f), true);
            pane.TimerRound = Label(clockRt, "Round", UiFit.FloorFont, TextAnchor.MiddleCenter, new Vector2(0.58f, 0.12f), new Vector2(0.98f, 0.88f));
            pane.TimerRound.color = MenuTheme.Gold;
            Outline badgeEdge = pane.BadgeWord.GetComponent<Outline>();
            if (badgeEdge != null) badgeEdge.effectColor = new Color(1f, 0.98f, 0.9f, 0.9f);

            pane.Identity = MenuWidgets.Place(rt, "Identity", 136f, 12f, 480f, 128f);
            RectTransform mark = MenuWidgets.Place(pane.Identity, "SeatMark", 8f, 86f, 36f, 36f);
            pane.SeatMark = mark.gameObject.AddComponent<Image>();
            pane.SeatMark.color = MenuTheme.Ink;
            pane.SeatMark.preserveAspect = true;
            pane.SeatMark.raycastTarget = false;
            pane.SeatMark.enabled = false;
            pane.Name = Label(pane.Identity, "Name", 34, TextAnchor.MiddleLeft, new Vector2(0.10f, 0.66f), new Vector2(1f, 1f));
            pane.Profile = Label(pane.Identity, "Profile", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0f, 0.36f), new Vector2(0.58f, 0.68f));
            pane.Profile.color = MenuTheme.Mute;
            pane.Tags = Label(pane.Identity, "Tags", UiFit.FloorFont, TextAnchor.MiddleRight, new Vector2(0.56f, 0.36f), new Vector2(0.78f, 0.68f));
            pane.Tags.color = MenuTheme.Mute;
            pane.Tags.text = MatchHudText.Tags;
            pane.TagsValue = Label(pane.Identity, "TagsN", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0.80f, 0.36f), new Vector2(1f, 0.68f));
            pane.Metric = Label(pane.Identity, "Metric", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0f, 0f), new Vector2(0.42f, 0.38f));
            pane.Metric.color = MenuTheme.Gold;
            pane.Value = Label(pane.Identity, "Value", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0.40f, 0f), new Vector2(1f, 0.38f));

            pane.Call = Label(rt, "Call", 64, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.68f), true);
            pane.Call.enabled = false;

            pane.Verbs = MenuWidgets.Place(rt, "Verbs", 16f, 0f, 168f, 72f);
            pane.Verbs.anchorMin = new Vector2(0f, 0f);
            pane.Verbs.anchorMax = new Vector2(0f, 0f);
            pane.Verbs.pivot = new Vector2(0f, 0f);
            pane.Verbs.anchoredPosition = new Vector2(16f, 16f);
            BuildVerb(pane.Verbs, 0f, out pane.DashBg, out pane.DashFill, out pane.DashWord);
            BuildVerb(pane.Verbs, 56f, out pane.RopeMark, out Image ropeFill, out pane.RopeWord);
            if (ropeFill != null) ropeFill.enabled = false;
            BuildVerb(pane.Verbs, 112f, out pane.SafeBg, out pane.SafeFill, out pane.SafeWord);
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
            RectTransform compassRt = MenuWidgets.Place(root, "Compass" + index.ToString(), 0f, 0f, 72f, 40f);
            Image compassPlate = compassRt.gameObject.AddComponent<Image>();
            compassPlate.sprite = MenuArt.Round;
            compassPlate.type = Image.Type.Sliced;
            compassPlate.color = MenuTheme.Ink;
            compassPlate.raycastTarget = false;
            pane.CompassPlate = compassPlate;
            pane.Compass = Label(compassRt, "It", UiFit.FloorFont, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, true);
            pane.Compass.color = MenuTheme.Gold;
            pane.Compass.text = MatchHudText.It;
            pane.Compass.enabled = false;
            compassPlate.enabled = false;
            BuildBoard(rt, pane);
            BuildFeed(rt, pane);
            return pane;
        }

        static void BuildBoard(RectTransform paneRoot, HudPane pane)
        {
            RectTransform rt = MenuWidgets.Box(paneRoot, "Standings", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(640f, 520f);
            pane.Board = rt;
            Image plate = rt.gameObject.AddComponent<Image>();
            plate.sprite = MenuArt.Round;
            plate.type = Image.Type.Sliced;
            plate.color = new Color(0.04f, 0.08f, 0.16f, 0.94f);
            plate.raycastTarget = false;
            pane.BoardTitle = Label(rt, "Title", 36, TextAnchor.MiddleCenter, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.98f), true);
            pane.BoardTitle.text = ScorePeek.Title;
            pane.BoardRound = Label(rt, "Round", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0.08f, 0.72f), new Vector2(0.58f, 0.84f));
            pane.BoardClock = Label(rt, "Clock", UiFit.FloorFont, TextAnchor.MiddleRight, new Vector2(0.50f, 0.72f), new Vector2(0.92f, 0.84f));
            for (int i = 0; i < 4; i++)
            {
                float top = 0.68f - i * 0.15f;
                pane.BoardName[i] = Label(rt, "Who", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0.08f, top - 0.12f), new Vector2(0.48f, top));
                pane.BoardValue[i] = Label(rt, "Val", UiFit.FloorFont, TextAnchor.MiddleRight, new Vector2(0.48f, top - 0.12f), new Vector2(0.92f, top));
            }
            rt.gameObject.SetActive(false);
        }

        static void BuildFeed(RectTransform paneRoot, HudPane pane)
        {
            RectTransform rt = MenuWidgets.Place(paneRoot, "Feed", 0f, 0f, 460f, 132f);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-18f, 18f);
            for (int i = 0; i < 3; i++)
            {
                float y = i * 42f;
                RectTransform line = MenuWidgets.Place(rt, "Line", 0f, y, 460f, 40f);
                Image plate = line.gameObject.AddComponent<Image>();
                plate.sprite = MenuArt.Round;
                plate.type = Image.Type.Sliced;
                plate.color = new Color(0.04f, 0.07f, 0.16f, 0.92f);
                plate.raycastTarget = false;
                plate.enabled = false;
                pane.FeedPlate[i] = plate;
                RectTransform chip = MenuWidgets.Place(line, "Chip", 6f, 6f, 28f, 28f);
                Image chipImage = chip.gameObject.AddComponent<Image>();
                chipImage.raycastTarget = false;
                chipImage.enabled = false;
                pane.FeedChip[i] = chipImage;
                RectTransform shape = MenuWidgets.Place(line, "Mark", 40f, 6f, 28f, 28f);
                Image shapeImage = shape.gameObject.AddComponent<Image>();
                shapeImage.color = MenuTheme.Cream;
                shapeImage.preserveAspect = true;
                shapeImage.raycastTarget = false;
                shapeImage.enabled = false;
                pane.FeedMark[i] = shapeImage;
                RectTransform word = MenuWidgets.Place(line, "Word", 74f, 0f, 378f, 40f);
                Text text = word.gameObject.AddComponent<Text>();
                text.font = MenuTheme.Font;
                text.fontSize = UiFit.FloorFont;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleRight;
                text.color = MenuTheme.Cream;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.resizeTextForBestFit = false;
                text.raycastTarget = false;
                text.text = MatchHudText.Blank;
                pane.Feed[i] = text;
            }
        }

        static void BuildVerb(Transform parent, float x, out Image back, out Image fill, out Text word)
        {
            RectTransform rt = MenuWidgets.Place(parent, "Verb", x, 0f, 52f, 72f);
            RectTransform disc = MenuWidgets.Place(rt, "Disc", 8f, 0f, 36f, 36f);
            back = disc.gameObject.AddComponent<Image>();
            back.sprite = MatchHudArt.Disc;
            back.color = new Color(0.10f, 0.12f, 0.16f, 0.92f);
            back.raycastTarget = false;
            RectTransform pie = MenuWidgets.Place(disc, "Pie", 0f, 0f, 36f, 36f);
            fill = pie.gameObject.AddComponent<Image>();
            fill.sprite = MatchHudArt.Disc;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;
            fill.raycastTarget = false;
            word = Label(rt, "Word", UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0.28f, 0f), new Vector2(1f, 1f));
        }

        static RectTransform BuildScore(Transform root, MatchHud hud)
        {
            RectTransform rt = MenuWidgets.Box(root, "Score", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            Image plate = rt.gameObject.AddComponent<Image>();
            plate.sprite = MenuArt.Round;
            plate.type = Image.Type.Sliced;
            plate.color = new Color(0.05f, 0.10f, 0.20f, 0.92f);
            plate.raycastTarget = false;
            hud.ScoreTitle = Label(rt, "Title", 32, TextAnchor.MiddleLeft, new Vector2(0f, 0.82f), new Vector2(1f, 1f), true);
            hud.ScoreTitle.text = MatchHudText.Score;
            for (int i = 0; i < 4; i++)
            {
                float top = 0.80f - i * 0.18f;
                float mid = top - 0.08f;
                RectTransform chip = MenuWidgets.Box(rt, "Chip" + i.ToString(), new Vector2(0.06f, mid - 0.045f), new Vector2(0.11f, mid + 0.045f), new Vector2(0.5f, 0.5f));
                Image chipImage = chip.gameObject.AddComponent<Image>();
                chipImage.raycastTarget = false;
                chipImage.enabled = false;
                hud.ScoreChip[i] = chipImage;
                RectTransform mark = MenuWidgets.Box(rt, "Mark" + i.ToString(), new Vector2(0.13f, mid - 0.04f), new Vector2(0.18f, mid + 0.04f), new Vector2(0.5f, 0.5f));
                Image markImage = mark.gameObject.AddComponent<Image>();
                markImage.color = MenuTheme.Cream;
                markImage.preserveAspect = true;
                markImage.raycastTarget = false;
                markImage.enabled = false;
                hud.ScoreMark[i] = markImage;
                hud.ScoreLine[i] = Label(rt, "Line" + i.ToString(), UiFit.FloorFont, TextAnchor.MiddleLeft, new Vector2(0.20f, top - 0.16f), new Vector2(0.96f, top));
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

        static Text Label(Transform parent, string name, int size, TextAnchor align, Vector2 min, Vector2 max, bool comic = false)
        {
            RectTransform rt = MenuWidgets.Box(parent, name, min, max, new Vector2(0.5f, 0.5f));
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = new Vector2(-8f, 0f);
            Text text = rt.gameObject.AddComponent<Text>();
            if (size < UiFit.FloorFont) size = UiFit.FloorFont;
            text.font = comic ? MenuTheme.Display : MenuTheme.Font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = align;
            text.color = MenuTheme.Cream;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = UiFit.FloorFont;
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
