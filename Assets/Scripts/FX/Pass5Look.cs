using Tag.Art;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.FX
{
    /// <summary>
    /// Amounts for the pass-5 layer: speed lines, wall-scrape sparks,
    /// a tag hit burst, the It handoff flash, and pad and zip trails.
    /// Effects and Reduced flashing gate all of it. The comic toggle
    /// stays on the words. Nothing here writes speed or the camera.
    /// </summary>
    public static class Pass5Look
    {
        public const int HotPathNew = 0;
        public const float FlashSeconds = 0.40f;
        public const float FlashSize = 1.15f;
        public const int TrailPoints = 12;
        public const int LinesFull = 4;
        public const int ScrapeFullMetal = 6;

        public static int SpeedLines(float speed, float density)
        {
            if (density <= 0f || speed < DustLook.Sprint) return 0;
            float extra = (speed - DustLook.Sprint) / DustLook.Sprint;
            if (extra < 0f) extra = 0f;
            if (extra > 1.5f) extra = 1.5f;
            int n = LinesFull + (int)(extra * 4f);
            if (n > 8) n = 8;
            if (density < 0.99f)
            {
                n = n / 2;
                if (n < 2) n = 2;
            }
            return n;
        }

        public static float LineLength(float speed)
        {
            if (speed < DustLook.Sprint) return 0f;
            float t = speed / DustLook.Sprint;
            if (t > 2f) t = 2f;
            return 0.55f + (t - 1f) * 0.70f;
        }

        /// <summary>Sparks at the chest and the hand. The foot streak is a different effect.</summary>
        public static int ScrapeBits(int surface, float speed, float density)
        {
            if (density <= 0f || speed < 4f) return 0;
            int n;
            if (surface == (int)DustLook.Surface.Metal) n = ScrapeFullMetal;
            else if (surface == (int)DustLook.Surface.Wet) n = 4;
            else if (surface == (int)DustLook.Surface.Concrete) n = 3;
            else if (surface == (int)DustLook.Surface.Wood) n = 2;
            else if (surface == (int)DustLook.Surface.Dirt) n = 2;
            else n = 0;
            float pace = speed / 9.5f;
            if (pace < 0.35f) pace = 0.35f;
            if (pace > 1.4f) pace = 1.4f;
            n = (int)(n * pace * density + 0.001f);
            if (n < 1 && surface != (int)DustLook.Surface.Grass && density > 0f) n = 1;
            if (surface == (int)DustLook.Surface.Grass) n = 0;
            return n;
        }

        public static bool ScrapeSpark(int surface)
        {
            return surface == (int)DustLook.Surface.Metal || surface == (int)DustLook.Surface.Concrete;
        }

        public static bool ScrapeDrop(int surface)
        {
            return surface == (int)DustLook.Surface.Wet;
        }

        /// <summary>Above the sole, so this is not the foot scuff.</summary>
        public static float ScrapeHeight(bool hand)
        {
            return hand ? 1.05f : 1.28f;
        }

        public static int TagBits(float density)
        {
            if (density <= 0f) return 0;
            if (density < 0.99f) return 4;
            return 8;
        }

        public static bool TagShows(GameSettings settings)
        {
            return FxAmount.Show(settings);
        }

        public static float FlashAlpha(float age, float density)
        {
            if (density <= 0f || age < 0f || age >= FlashSeconds) return 0f;
            float u = age / FlashSeconds;
            float env;
            if (u < 0.18f) env = u / 0.18f;
            else if (u > 0.62f) env = (1f - u) / 0.38f;
            else env = 1f;
            if (env < 0f) env = 0f;
            float scale = density < 0.99f ? 0.62f : 1f;
            return env * scale;
        }

        public static float FlashSpan(float density)
        {
            if (density <= 0f) return 0f;
            if (density < 0.99f) return FlashSize * 0.72f;
            return FlashSize;
        }

        public static int TrailCount(float density)
        {
            if (density <= 0f) return 0;
            if (density < 0.99f) return TrailPoints / 2;
            return TrailPoints;
        }

        public static bool Holds()
        {
            if (HotPathNew != 0) return false;
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f) return false;
            if (SpeedLines(DustLook.Walk, 1f) != 0) return false;
            if (SpeedLines(DustLook.Sprint, 1f) < LinesFull) return false;
            if (SpeedLines(DustLook.Sprint * 1.6f, 1f) <= SpeedLines(DustLook.Sprint, 1f)) return false;
            if (SpeedLines(DustLook.Sprint, FxAmount.LowDensity) >= SpeedLines(DustLook.Sprint, 1f)) return false;
            if (SpeedLines(DustLook.Sprint, 0f) != 0) return false;
            if (LineLength(DustLook.Sprint) <= 0.4f) return false;
            if (LineLength(DustLook.Walk) > 0.001f) return false;

            int metal = ScrapeBits((int)DustLook.Surface.Metal, 9.5f, 1f);
            int grass = ScrapeBits((int)DustLook.Surface.Grass, 9.5f, 1f);
            int concrete = ScrapeBits((int)DustLook.Surface.Concrete, 9.5f, 1f);
            int wet = ScrapeBits((int)DustLook.Surface.Wet, 9.5f, 1f);
            if (grass != 0 || metal < 4) return false;
            if (concrete >= metal || wet < 2) return false;
            if (ScrapeBits((int)DustLook.Surface.Metal, 9.5f, FxAmount.LowDensity) >= metal) return false;
            if (ScrapeHeight(false) <= 0.4f || ScrapeHeight(true) <= 0.4f) return false;
            if (!ScrapeSpark((int)DustLook.Surface.Metal) || ScrapeSpark((int)DustLook.Surface.Grass)) return false;
            if (!ScrapeDrop((int)DustLook.Surface.Wet) || ScrapeDrop((int)DustLook.Surface.Metal)) return false;

            if (TagBits(1f) <= TagBits(FxAmount.LowDensity) || TagBits(0f) != 0) return false;
            GameSettings off = GameSettings.Defaults();
            off.ComicWords = false;
            off.Effects = FxAmount.Full;
            if (!TagShows(off)) return false;
            if (ComicWords.Visible(off)) return false;
            off.Effects = FxAmount.Off;
            if (TagShows(off)) return false;
            off.Effects = FxAmount.Full;
            off.ReduceFlash[0] = true;
            if (TagShows(off) || FlashAlpha(0.2f, 0f) > 0.001f) return false;

            off = GameSettings.Defaults();
            if (FlashAlpha(0.2f, 1f) < 0.8f) return false;
            if (FlashAlpha(-0.1f, 1f) > 0.001f || FlashAlpha(FlashSeconds, 1f) > 0.001f) return false;
            if (FlashAlpha(0.2f, FxAmount.LowDensity) >= FlashAlpha(0.2f, 1f)) return false;
            if (FlashSpan(1f) < 1f) return false;
            if (FlashSpan(FxAmount.LowDensity) >= FlashSpan(1f)) return false;
            if (TrailCount(1f) != TrailPoints) return false;
            if (TrailCount(FxAmount.LowDensity) >= TrailCount(1f) || TrailCount(0f) != 0) return false;
            if (System.Math.Abs(ZipPose.RideSpeed - 14f) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            return "fx-pass5"
                + " lines=" + SpeedLines(DustLook.Sprint, 1f).ToString()
                + "/" + SpeedLines(DustLook.Sprint, FxAmount.LowDensity).ToString()
                + " len=" + LineLength(DustLook.Sprint).ToString("0.00")
                + " scrape=" + ScrapeBits((int)DustLook.Surface.Metal, 9.5f, 1f).ToString()
                + " grass=" + ScrapeBits((int)DustLook.Surface.Grass, 9.5f, 1f).ToString()
                + " tag=" + TagBits(1f).ToString()
                + " comicOff=1"
                + " flash=" + FlashSeconds.ToString("0.00")
                + " span=" + FlashSpan(1f).ToString("0.00")
                + " trail=" + TrailCount(1f).ToString()
                + "/" + TrailCount(FxAmount.LowDensity).ToString()
                + " alloc=" + HotPathNew.ToString();
        }
    }
}
