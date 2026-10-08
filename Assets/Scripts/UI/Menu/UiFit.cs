using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Couch TV scale. 80–130% on a 1920x1080 canvas. At 3 m, 24 px is the
    /// smallest type that still reads, so 80% needs a 30 px font.
    /// </summary>
    public static class UiFit
    {
        public const float ScaleMin = GameSettings.UiScaleMin;
        public const float ScaleMax = GameSettings.UiScaleMax;
        public const float ScaleDefault = GameSettings.UiScaleDefault;
        public const float RefW = 1920f;
        public const float RefH = 1080f;
        public const float SafeX = 96f;
        public const float SafeY = 54f;
        public const int MinPx = 24;
        public const int FloorFont = 30;
        public const float SpaceStep = 16f;
        public const float ChromeTop = 108f;
        public const float ChromeBot = 78f;

        public static readonly float[] Steps = { 0.80f, 0.90f, 1f, 1.10f, 1.20f, 1.30f };
        public static readonly string[] ScaleLine =
        {
            "UI scale  80%",
            "UI scale  90%",
            "UI scale  100%",
            "UI scale  110%",
            "UI scale  120%",
            "UI scale  130%"
        };

        public static float Clamp(float scale)
        {
            if (scale < ScaleMin) return ScaleMin;
            if (scale > ScaleMax) return ScaleMax;
            return scale;
        }

        public static float Current()
        {
            GameSettings s = GameSettings.Current;
            if (s == null) return ScaleDefault;
            return Clamp(s.UiScale);
        }

        public static void Ref(float scale, out float w, out float h)
        {
            float s = Clamp(scale);
            w = RefW / s;
            h = RefH / s;
        }

        public static int IndexOf(float scale)
        {
            float s = Clamp(scale);
            int best = 0;
            float bestD = 99f;
            for (int i = 0; i < Steps.Length; i++)
            {
                float d = s - Steps[i];
                if (d < 0f) d = -d;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            return best;
        }

        public static float Nudge(float scale, int dir)
        {
            int i = IndexOf(scale) + dir;
            if (i < 0) i = 0;
            if (i >= Steps.Length) i = Steps.Length - 1;
            return Steps[i];
        }

        public static string Line(float scale)
        {
            return ScaleLine[IndexOf(scale)];
        }

        public static int ScreenPx(int font, float scale)
        {
            float px = font * Clamp(scale);
            int whole = (int)px;
            if (px > whole) whole++;
            return whole;
        }

        /// <summary>Menu and HUD text. At 1.00 the pixel count is the size passed in.</summary>
        public static int TextPx(int font)
        {
            if (font < 1) font = 1;
            float hud = GameSettings.HudDefault;
            GameSettings s = GameSettings.Current;
            if (s != null) hud = s.HudScale;
            if (hud < GameSettings.HudMin) hud = GameSettings.HudMin;
            if (hud > GameSettings.HudMax) hud = GameSettings.HudMax;
            if (hud > 0.999f && hud < 1.001f) return font;
            float px = font * hud;
            int whole = (int)(px + 0.5f);
            if (whole < 1) whole = 1;
            return whole;
        }

        public static bool FontsHold()
        {
            if (FloorFont < 1) return false;
            if (ScreenPx(FloorFont, ScaleMin) < MinPx) return false;
            if (SpaceStep < 15.99f || SpaceStep > 16.01f) return false;
            return true;
        }

        public static bool Remembers()
        {
            GameSettings s = GameSettings.Defaults();
            s.UiScale = 1.1f;
            string blob = SettingsFile.Write(s, ActionBinds.Defaults());
            GameSettings back = GameSettings.Defaults();
            SettingsFile.Read(blob, back, ActionBinds.Defaults());
            if (back.UiScale < 1.09f || back.UiScale > 1.11f) return false;
            GameSettings partial = GameSettings.Defaults();
            SettingsFile.Read("v=2\nmouse=1.8\n", partial, ActionBinds.Defaults());
            if (partial.UiScale < 0.99f || partial.UiScale > 1.01f) return false;
            return GameSettings.RowCount == 19;
        }

        public static float BodyW(float scale)
        {
            Ref(scale, out float w, out _);
            return w - SafeX * 2f;
        }

        public static float BodyH(float scale)
        {
            Ref(scale, out _, out float h);
            return h - ChromeTop - ChromeBot;
        }

        public static int Window(float scale, float row, float top)
        {
            int n = (int)((BodyH(scale) - top) / row);
            if (n < 3) n = 3;
            if (n > 8) n = 8;
            return n;
        }

        public static void Columns(float scale, out float leftX, out float leftW, out float rightX, out float rightW)
        {
            float w = BodyW(scale);
            leftX = 16f;
            leftW = w * 0.46f;
            if (leftW > 940f) leftW = 940f;
            rightX = leftX + leftW + 16f;
            rightW = w - rightX - 16f;
            if (rightW < 200f) rightW = 200f;
        }

        public static void MainSplit(float scale, out float logoW, out float tileX, out float tileW)
        {
            float w = BodyW(scale);
            logoW = w * 0.46f;
            if (logoW > 860f) logoW = 860f;
            tileX = logoW + 24f;
            tileW = w - tileX - 8f;
            if (tileW > 760f) tileW = 760f;
            if (tileX + tileW > w) tileW = w - tileX - 8f;
        }

        public static void ArenaSplit(float scale, out float listW, out float shotX, out float shotW, out float row, out float step)
        {
            float w = BodyW(scale);
            float h = BodyH(scale);
            listW = w * 0.42f;
            if (listW > 760f) listW = 760f;
            shotX = listW + 28f;
            shotW = w - shotX - 8f;
            if (shotW < 240f) shotW = 240f;
            row = 144f;
            step = 156f;
            if (12f + 4f * step + row > h)
            {
                row = 108f;
                step = 118f;
            }
        }

        public static void Bands(float scale, out float view, out float rankY, out float rankH, out float btnY, out float btnH)
        {
            float h = BodyH(scale);
            view = h * 0.46f;
            if (view > 420f) view = 420f;
            if (view < 180f) view = 180f;
            rankH = 180f;
            btnH = RematchH;
            if (!IdentityText())
            {
                float rankNeed = BlockH(180f, 3);
                if (rankNeed > rankH) rankH = rankNeed;
                float btnNeed = RowH(RematchH);
                if (btnNeed > btnH) btnH = btnNeed;
            }
            rankY = view + 10f;
            btnY = rankY + rankH + 10f;
            if (btnY + btnH > h)
            {
                rankH = 176f;
                btnH = 80f;
                view = h - rankH - btnH - 24f;
                if (view < 160f) view = 160f;
                rankY = view + 8f;
                btnY = rankY + rankH + 8f;
            }
        }

        public const float CastNameH = 78f;
        public const float CastStatusH = 52f;
        public const float CastLine = 36f;

        public static void CastBands(float scale, out float cardH, out float gridTop, out float gridH, out float gridStep)
        {
            float h = BodyH(scale);
            gridH = 100f;
            gridStep = 108f;
            if (!IdentityText())
            {
                gridH = RowH(100f);
                gridStep = gridH + 8f;
            }
            float keys = 216f;
            cardH = h - keys - 12f;
            if (cardH > 560f) cardH = 560f;
            if (cardH < 220f) cardH = 220f;
            gridTop = cardH + 12f;
            if (gridTop + gridStep + gridH > h)
            {
                cardH = h - gridStep - gridH - 20f;
                if (cardH < 220f) cardH = 220f;
                gridTop = cardH + 12f;
            }
        }

        public static float CastCardW(float scale)
        {
            float span = BodyW(scale);
            float cardW = (span - 16f * 5f) / 4f;
            if (cardW > 428f) cardW = 428f;
            return cardW;
        }

        public static float CastInk(float scale)
        {
            return CastCardW(scale) - 24f;
        }

        public static float RankInk(float scale)
        {
            float span = BodyW(scale);
            float rankW = (span - 32f) / 4f;
            if (rankW > 436f) rankW = 436f;
            return rankW - 20f;
        }

        public const float StripeY = 3f;
        public const float StripeH = 8f;
        public const float StripeGap = 6f;

        /// <summary>Options and Accessibility rows. Title is 40 px under the stripe. The sub-line is its own row.</summary>
        public const float OptRow = 108f;
        public const float OptStep = 116f;

        public static bool IdentityText()
        {
            return TextPx(40) == 40 && TextPx(FloorFont) == FloorFont;
        }

        /// <summary>At 1.00 the height is unchanged. Above that a title and one detail line fit.</summary>
        public static float RowH(float baseH)
        {
            if (IdentityText()) return baseH;
            float need = 24f + TextPx(40) + 6f + TextPx(FloorFont) + 12f;
            return need > baseH ? need : baseH;
        }

        public static float RowStep(float baseStep, float baseRow)
        {
            if (IdentityText()) return baseStep;
            float gap = baseStep - baseRow;
            if (gap < 8f) gap = 8f;
            return RowH(baseRow) + gap;
        }

        public static float BlockH(float baseH, int detailLines)
        {
            if (IdentityText()) return baseH;
            if (detailLines < 1) detailLines = 1;
            float need = 24f + TextPx(40) + 6f + TextPx(FloorFont) * detailLines + 12f;
            return need > baseH ? need : baseH;
        }

        /// <summary>One title, no detail. Practice and other single-line rows.</summary>
        public static float LineH(float baseH)
        {
            if (IdentityText()) return baseH;
            float need = StripeClear() + TextPx(40) + 16f;
            return need > baseH ? need : baseH;
        }

        public static float CastNameBand()
        {
            if (IdentityText()) return CastNameH;
            // Name, then the look. At 1.50 a long look wraps, so the band is three lines.
            float need = 12f + TextPx(FloorFont) * 3f + 8f;
            return need > CastNameH ? need : CastNameH;
        }

        public static float CastStatusBand()
        {
            if (IdentityText()) return CastStatusH;
            // The hat and ready line wraps at 1.50, so the band is two lines tall.
            float need = 12f + TextPx(FloorFont) * 2f + 8f;
            return need > CastStatusH ? need : CastStatusH;
        }

        /// <summary>Options row. At text size 1.00 this is the 108/116 pair. Above that the row grows so the type fits.</summary>
        public static void OptionSpan(out float row, out float step)
        {
            int title = TextPx(40);
            int detail = TextPx(FloorFont);
            if (title == 40 && detail == FloorFont)
            {
                row = OptRow;
                step = OptStep;
                return;
            }
            row = 24f + title + 6f + detail + 12f;
            if (row < OptRow) row = OptRow;
            step = row + 8f;
        }

        /// <summary>RESULTS action row. Rematch carries the "Same setup" sub-line.</summary>
        public const float RematchH = 128f;

        public static float StripeClear()
        {
            return StripeY + StripeH + StripeGap;
        }

        public static void TileText(float h, bool two, out float titleFromTop, out float titleH, out float detailFromTop, out float detailH)
        {
            float top = StripeClear();
            if (h >= 96f) top += 8f;
            float bot = 4f;
            float well = h - top - bot;
            if (well < 1f) well = 1f;
            if (two && well < FloorFont * 2f)
            {
                bot = 2f;
                well = h - top - bot;
                if (well < 1f) well = 1f;
            }
            if (!two)
            {
                titleFromTop = top;
                titleH = well;
                detailFromTop = top + well;
                detailH = 0f;
                return;
            }
            int titlePx = TextPx(40);
            int detailPx = TextPx(FloorFont);
            bool identity = titlePx == 40 && detailPx == FloorFont;
            float need = 24f + titlePx + 6f + detailPx + 8f;
            if (h >= OptRow && h <= RematchH && identity)
            {
                titleFromTop = 24f;
                titleH = 40f;
                detailFromTop = 70f;
                detailH = h - detailFromTop - 8f;
                if (detailH < FloorFont)
                {
                    detailH = FloorFont;
                    detailFromTop = h - 8f - detailH;
                }
                return;
            }
            if (!identity && h + 0.5f >= need)
            {
                titleFromTop = 24f;
                titleH = titlePx;
                detailFromTop = 24f + titlePx + 6f;
                detailH = h - detailFromTop - 8f;
                if (detailH < detailPx) detailH = detailPx;
                return;
            }
            float titleBand = FloorFont + 4f;
            if (titleBand > well - FloorFont) titleBand = well * 0.5f;
            if (titleBand < 1f) titleBand = 1f;
            titleFromTop = top;
            titleH = titleBand;
            detailFromTop = top + titleBand;
            detailH = well - titleBand;
        }

        public static float RankBoxH(float scale)
        {
            Bands(scale, out _, out _, out float rankH, out _, out _);
            TileText(rankH, true, out _, out _, out _, out float detailH);
            return detailH;
        }

        public static bool CardsHold()
        {
            string wide = "WWWWWWWWWWWW";
            string hat = "Hat off";
            string ready = "Not ready";
            string skin = "Lavender";
            float[] scales = { 0.80f, 1f, 1.30f };
            for (int seat = 0; seat < 4; seat++)
            {
                for (int i = 0; i < scales.Length; i++)
                {
                    float ink = CastInk(scales[i]);
                    if (ink < 160f) return false;
                    CastLines(wide, skin, skin, hat, ready, ink, out string top, out string bot);
                    if (!BlockFits(top, ink, CastNameH)) return false;
                    if (!BlockFits(bot, ink, CastStatusH)) return false;
                    if (bot != "Hat off   Not ready") return false;
                    if (LineCount(bot) != 1) return false;
                    CastLines("P1", skin, skin, "Hat on", "READY", ink, out string named, out _);
                    if (scales[i] < 1.2f && named.IndexOf("Lavender / Lavender") < 0) return false;
                }
            }
            return RankCards();
        }

        public static void CastLines(string name, string skin, string trim, string hat, string ready, float ink, out string top, out string bot)
        {
            if (name == null) name = "";
            if (skin == null) skin = "";
            if (trim == null) trim = "";
            if (hat == null) hat = "";
            if (ready == null) ready = "";
            bot = hat + "   " + ready;
            string look = skin + " / " + trim;
            if (Measure(name) <= ink && Measure(look) <= ink)
                top = name + "\n" + look;
            else
                top = WrapName(name, ink);
        }

        public static string FormatStats(bool winner, int tags, float time, int wins)
        {
            string tagWord = tags == 1 ? "1 tag" : tags.ToString() + " tags";
            string timeWord = time.ToString("0.0") + "s as It";
            string winWord = wins == 1 ? "1 round win" : wins.ToString() + " round wins";
            if (winner)
                return "WIN  " + tagWord + "\n" + timeWord + "\n" + winWord;
            string head = tagWord + "  " + timeWord;
            if (InkWidth(head) > RankInk(ScaleMax))
                head = tagWord + "\n" + timeWord;
            return head + "\n" + winWord;
        }

        static bool RankCards()
        {
            float[] scales = { 0.80f, 1f, 1.30f };
            string shown = FormatStats(true, 6, 8.5f, 2);
            string wide = FormatStats(false, 12, 99.9f, 0);
            for (int i = 0; i < scales.Length; i++)
            {
                float ink = RankInk(scales[i]);
                float box = RankBoxH(scales[i]);
                if (ink < 200f || box < CastLine * 3f) return false;
                if (!RankFits(shown, ink, box)) return false;
                if (!RankFits(wide, ink, box)) return false;
            }
            if (shown.IndexOf("2 round wins") < 0) return false;
            if (shown.IndexOf("s as It") < 0) return false;
            return true;
        }

        static bool RankFits(string text, float ink, float box)
        {
            if (!BlockFits(text, ink, box)) return false;
            int start = 0;
            for (int i = 0; i <= text.Length; i++)
            {
                if (i < text.Length && text[i] != '\n') continue;
                if (Measure(text, start, i - start) > ink) return false;
                start = i + 1;
            }
            return true;
        }

        static bool BlockFits(string text, float ink, float box)
        {
            if (string.IsNullOrEmpty(text)) return box >= CastLine;
            int lines = 0;
            int start = 0;
            for (int i = 0; i <= text.Length; i++)
            {
                if (i < text.Length && text[i] != '\n') continue;
                int count = i - start;
                if (count > 0 && Measure(text, start, count) > ink) return false;
                lines++;
                start = i + 1;
            }
            return lines * CastLine <= box + 0.5f;
        }

        static int LineCount(string text)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            int n = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') n++;
            }
            return n;
        }

        static string WrapName(string name, float ink)
        {
            if (Measure(name) <= ink) return name;
            int cut = name.Length;
            while (cut > 1 && Measure(name, 0, cut) > ink) cut--;
            if (cut < 1) cut = 1;
            if (cut >= name.Length) return name;
            return name.Substring(0, cut) + "\n" + name.Substring(cut);
        }

        public static int InkWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return Measure(text, 0, text.Length);
        }

        static int Measure(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return Measure(text, 0, text.Length);
        }

        static int Measure(string text, int start, int count)
        {
            int w = 0;
            int end = start + count;
            if (end > text.Length) end = text.Length;
            for (int i = start; i < end; i++)
                w += Advance(text[i]);
            return w;
        }

        static int Advance(char c)
        {
            if (c < 32 || c > 126) return 24;
            return Advances[c - 32];
        }

        static readonly int[] Advances =
        {
            9,10,15,17,17,27,22,8,10,10,12,18,9,10,9,9,17,17,17,17,17,17,17,17,17,17,10,10,18,18,18,19,30,22,22,22,22,21,19,24,22,9,17,22,19,25,22,24,21,24,22,21,19,22,21,29,21,21,19,10,9,10,18,17,10,17,19,17,19,17,10,19,19,9,9,17,9,27,19,19,19,19,12,17,10,19,17,24,17,17,15,12,9,12,18
        };

        public static void RowBox(float scale, float maxW, out float x, out float w)
        {
            float body = BodyW(scale);
            w = body - 48f;
            if (w > maxW) w = maxW;
            x = (body - w) * 0.5f;
            if (x < 16f) x = 16f;
            if (x + w > body) w = body - x - 8f;
        }
    }
}
