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
    }
}
