using System;
using System.Globalization;
using Tag.Art;
using Tag.Gameplay;
using Tag.Settings;

namespace Tag.FX
{
    /// <summary>
    /// Amounts for the visual-only FX kit. Nothing here writes speed, stun, or timing.
    /// Hard landings start at the light-tier ceiling. The roll uses the 65% gate.
    /// </summary>
    public static class FxKitLook
    {
        public const float HardImpact = LandingRollPose.LightCeil;
        public const float LandLife = 0.42f;
        public const float RollLife = LandingRollPose.Seconds;
        public const float SparkLife = 0.26f;
        public const float DebrisLife = 0.38f;
        public const float LaunchLife = 0.36f;
        public const float ScuffLife = 0.34f;
        public const float FootLife = 0.28f;
        public const float FlashSeconds = 0.18f;
        public const float ShimmerAmp = 0.028f;
        public const float ImmunitySeconds = TagBackImmunity.DefaultSeconds;
        public const float StaggerSeconds = PunchStagger.Duration;

        public const int DustFull = 12;
        public const int DustRoll = 16;
        public const int SparkFull = 8;
        public const int DebrisFull = 8;
        public const int RimCards = 12;
        public const int Stars = 5;
        public const int Streaks = 6;
        public const int Scuffs = 4;
        public const int FootDust = 4;
        public const int Players = 4;
        public const int RingSeg = 28;

        public static bool Hard(float impact)
        {
            return impact >= HardImpact;
        }

        public static bool IsRoll(float impact, float planar)
        {
            return LandingRollPose.Triggered(impact) && !LandingRollPose.Stationary(planar);
        }

        public static float LandScale(float impact)
        {
            float span = LandingRollPose.Threshold - HardImpact;
            if (span < 0.01f) span = 0.01f;
            float t = (impact - HardImpact) / span;
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            return 0.55f + t * 1.05f;
        }

        public static float RollScale(float impact)
        {
            return LandScale(impact) * 1.45f;
        }

        public static int DustCount(float impact, float planar, float density)
        {
            if (density <= 0f || impact < HardImpact) return 0;
            float span = LandingRollPose.Threshold - HardImpact;
            if (span < 0.01f) span = 0.01f;
            float t = (impact - HardImpact) / span;
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            int n = IsRoll(impact, planar) ? DustRoll : 6 + (int)(t * 6f);
            if (n > DustRoll) n = DustRoll;
            n = (int)(n * density + 0.001f);
            if (n < 1) n = 1;
            if (n > DustRoll) n = DustRoll;
            return n;
        }

        public static int Scaled(int full, float density)
        {
            if (full <= 0 || density <= 0f) return 0;
            int n = (int)(full * density + 0.001f);
            if (n < 1) n = 1;
            if (n > full) n = full;
            return n;
        }

        public static float RingRadius(float scale, float age, float life)
        {
            if (life < 0.0001f) life = 0.0001f;
            float u = age / life;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return scale * (0.28f + 0.92f * u);
        }

        public static float Fade(float age, float life)
        {
            if (life < 0.0001f || age < 0f || age >= life) return 0f;
            float u = age / life;
            if (u < 0.12f) return u / 0.12f;
            return (1f - u) / 0.88f;
        }

        public static float FlashAlpha(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            float u = age / FlashSeconds;
            float env = u < 0.18f ? u / 0.18f : (1f - u) / 0.82f;
            return env * 0.62f;
        }

        public static float RimAlpha(float remaining, float duration, float time, bool steady)
        {
            if (duration <= 0.0001f || remaining <= 0f) return 0f;
            float env = remaining / duration;
            if (env < 0f) env = 0f;
            if (env > 1f) env = 1f;
            if (steady) return env * 0.55f;
            float wave = 0.70f + 0.30f * (float)Math.Sin(time * 5.2);
            return env * wave;
        }

        public static float Shimmer(float t, float tension, float time)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            if (tension < 0f) tension = 0f;
            if (tension > 1f) tension = 1f;
            float bell = 4f * t * (1f - t);
            float amp = ShimmerAmp * (0.35f + 0.65f * tension);
            return amp * bell * (float)Math.Sin(time * 14.0 + t * 9.0);
        }

        public static float ScuffLength(float speed)
        {
            if (speed < 0f) speed = 0f;
            float t = speed / DustLook.Sprint;
            if (t < 0f) t = 0f;
            if (t > 1.6f) t = 1.6f;
            return 0.12f + t * 0.16f;
        }

        public static void SurfaceColor(int surface, out float r, out float g, out float b)
        {
            switch (surface)
            {
                case (int)DustLook.Surface.Grass: r = 0.42f; g = 0.48f; b = 0.28f; return;
                case (int)DustLook.Surface.Dirt: r = 0.62f; g = 0.46f; b = 0.28f; return;
                case (int)DustLook.Surface.Wood: r = 0.55f; g = 0.40f; b = 0.24f; return;
                case (int)DustLook.Surface.Metal: r = 0.72f; g = 0.74f; b = 0.78f; return;
                case (int)DustLook.Surface.Wet: r = 0.28f; g = 0.36f; b = 0.42f; return;
                default: r = 0.62f; g = 0.62f; b = 0.60f; return;
            }
        }

        public static float Density(GameSettings settings)
        {
            if (settings != null && settings.Effects == FxAmount.Low) return FxAmount.LowDensity;
            return 1f;
        }

        public static bool Master(GameSettings settings)
        {
            if (settings == null) return true;
            return settings.Effects > FxAmount.Off;
        }

        public static bool Bursts(GameSettings settings, int slot)
        {
            if (!FxKitOptions.Enabled(slot)) return false;
            if (!Master(settings)) return false;
            if (settings != null && settings.AnyReduceFlash()) return false;
            return true;
        }

        public static bool Immunity(GameSettings settings)
        {
            if (!FxKitOptions.Enabled(FxKitOptions.Immunity)) return false;
            return Master(settings);
        }

        public static bool SteadyGlow(GameSettings settings)
        {
            return settings != null && settings.AnyReduceFlash();
        }

        public static string ProofLine()
        {
            int hop = DustCount(8f, 4f, 1f);
            int hard = DustCount(20f, 4f, 1f);
            int roll = DustCount(LandingRollPose.Threshold, 4f, 1f);
            int low = DustCount(20f, 4f, FxAmount.LowDensity);
            int spark = Scaled(SparkFull, 1f);
            int debris = Scaled(DebrisFull, 1f);
            float shim = Math.Abs(Shimmer(0.5f, 1f, 0.4f)) * 100f;
            return "fx-kit"
                + " hard=" + HardImpact.ToString("0", CultureInfo.InvariantCulture)
                + " roll=" + LandingRollPose.Threshold.ToString("0.00", CultureInfo.InvariantCulture)
                + " stagger=" + StaggerSeconds.ToString("0.00", CultureInfo.InvariantCulture)
                + " immune=" + ImmunitySeconds.ToString("0.00", CultureInfo.InvariantCulture)
                + " flash=" + FlashSeconds.ToString("0.00", CultureInfo.InvariantCulture)
                + " dust=" + hop.ToString(CultureInfo.InvariantCulture)
                + "/" + hard.ToString(CultureInfo.InvariantCulture)
                + "/" + roll.ToString(CultureInfo.InvariantCulture)
                + " low=" + low.ToString(CultureInfo.InvariantCulture)
                + " spark=" + spark.ToString(CultureInfo.InvariantCulture)
                + " debris=" + debris.ToString(CultureInfo.InvariantCulture)
                + " rim=" + RimCards.ToString(CultureInfo.InvariantCulture)
                + " stars=" + Stars.ToString(CultureInfo.InvariantCulture)
                + " streak=" + Streaks.ToString(CultureInfo.InvariantCulture)
                + " scuff=" + Scuffs.ToString(CultureInfo.InvariantCulture)
                + " foot=" + FootDust.ToString(CultureInfo.InvariantCulture)
                + " shimmer=" + shim.ToString("0.0", CultureInfo.InvariantCulture)
                + " pool=" + DustRoll.ToString(CultureInfo.InvariantCulture)
                + " players=" + Players.ToString(CultureInfo.InvariantCulture)
                + " toggles=" + FxKitOptions.Count.ToString(CultureInfo.InvariantCulture)
                + " alloc=0";
        }

        public static bool Holds()
        {
            if (Math.Abs(LandingRollPose.Terminal - 56.16f) > 0.001f) return false;
            if (Math.Abs(LandingRollPose.Fraction - 0.65f) > 0.001f) return false;
            if (Math.Abs(LandingRollPose.Threshold - LandingRollPose.Terminal * LandingRollPose.Fraction) > 0.001f) return false;
            if (Math.Abs(PunchStagger.Duration - 0.25f) > 0.001f) return false;
            if (Math.Abs(TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f) return false;
            if (Math.Abs(StaggerSeconds - PunchStagger.Duration) > 0.001f) return false;
            if (Math.Abs(ImmunitySeconds - 1.0f) > 0.001f) return false;
            if (GameSettings.RowCount != 21) return false;
            if (Hard(HardImpact - 0.05f) || !Hard(HardImpact)) return false;
            if (DustCount(8f, 4f, 1f) != 0) return false;
            int mid = DustCount(20f, 4f, 1f);
            int heavy = DustCount(30f, 4f, 1f);
            int roll = DustCount(LandingRollPose.Threshold, 4f, 1f);
            int absorb = DustCount(LandingRollPose.Threshold, 0.2f, 1f);
            if (mid <= 0 || heavy <= mid || roll <= heavy) return false;
            if (roll != DustRoll || absorb >= roll) return false;
            if (DustCount(20f, 4f, FxAmount.LowDensity) >= mid) return false;
            if (DustCount(20f, 4f, 0f) != 0) return false;
            if (LandScale(LandingRollPose.Threshold) <= LandScale(HardImpact)) return false;
            if (RollScale(LandingRollPose.Threshold) <= LandScale(LandingRollPose.Threshold)) return false;
            if (FlashAlpha(0f) != 0f || FlashAlpha(FlashSeconds) != 0f) return false;
            if (FlashAlpha(FlashSeconds * 0.18f) < 0.5f) return false;
            if (Fade(0f, LandLife) != 0f || Fade(LandLife, LandLife) != 0f) return false;
            float rimA = RimAlpha(1f, 1f, 0.2f, true);
            float rimB = RimAlpha(1f, 1f, 0.8f, true);
            if (Math.Abs(rimA - rimB) > 0.0001f || rimA < 0.4f) return false;
            if (RimAlpha(0f, 1f, 0.2f, false) != 0f) return false;
            if (RimAlpha(1f, 1f, 0.3f, false) < 0.4f) return false;
            if (Math.Abs(Shimmer(0.5f, 1f, 0.4f)) > ShimmerAmp + 0.0001f) return false;
            if (Shimmer(0f, 1f, 0.4f) != 0f || Shimmer(1f, 1f, 0.4f) != 0f) return false;
            if (Scaled(SparkFull, 0.5f) >= SparkFull || Scaled(SparkFull, 0f) != 0) return false;
            if (ScuffLength(DustLook.Sprint) <= ScuffLength(6f)) return false;

            GameSettings off = GameSettings.Defaults();
            if (!Master(off) || !Bursts(off, FxKitOptions.Land)) return false;
            off.Effects = FxAmount.Off;
            if (Master(off) || Bursts(off, FxKitOptions.Land) || Immunity(off)) return false;
            off.Effects = FxAmount.Full;
            off.ReduceFlash[0] = true;
            if (Bursts(off, FxKitOptions.Land) || Bursts(off, FxKitOptions.TagFlash)) return false;
            if (!Immunity(off) || !SteadyGlow(off)) return false;
            if (Math.Abs(Density(off) - 1f) > 0.001f) return false;
            off.ReduceFlash[0] = false;
            off.Effects = FxAmount.Low;
            if (Math.Abs(Density(off) - FxAmount.LowDensity) > 0.001f) return false;

            FxKitOptions.Reset();
            if (!FxKitOptions.Enabled(FxKitOptions.Land)) return false;
            FxKitOptions.On[FxKitOptions.Grapple] = false;
            string blob = FxKitOptions.Write();
            FxKitOptions.Reset();
            FxKitOptions.Read(blob);
            if (FxKitOptions.Enabled(FxKitOptions.Grapple)) return false;
            if (!FxKitOptions.Enabled(FxKitOptions.Land)) return false;
            FxKitOptions.Read("v=1\n");
            bool kept = FxKitOptions.Enabled(FxKitOptions.Land) && !FxKitOptions.Enabled(FxKitOptions.Grapple);
            FxKitOptions.Reset();
            if (!kept) return false;
            if (FxKitOptions.Label(FxKitOptions.TagFlash).IndexOf("On", StringComparison.Ordinal) < 0) return false;
            return true;
        }
    }
}
