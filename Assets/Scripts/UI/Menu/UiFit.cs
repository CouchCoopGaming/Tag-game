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
            view = h * 0.48f;
            if (view > 460f) view = 460f;
            if (view < 180f) view = 180f;
            rankH = 110f;
            btnH = 88f;
            rankY = view + 10f;
            btnY = rankY + rankH + 10f;
            if (btnY + btnH > h)
            {
                view = h - 96f - 80f - 28f;
                if (view < 160f) view = 160f;
                rankH = 96f;
                btnH = 80f;
                rankY = view + 8f;
                btnY = rankY + rankH + 8f;
            }
        }

        public static void CastBands(float scale, out float cardH, out float gridTop, out float gridH, out float gridStep)
        {
            float h = BodyH(scale);
            cardH = h * 0.46f;
            if (cardH > 420f) cardH = 420f;
            if (cardH < 200f) cardH = 200f;
            gridTop = cardH + 16f;
            float room = h - gridTop - 8f;
            gridStep = room * 0.5f;
            gridH = gridStep - 8f;
            if (gridH > 160f)
            {
                gridH = 160f;
                gridStep = 172f;
            }
            if (gridH < 64f) gridH = 64f;
        }

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
