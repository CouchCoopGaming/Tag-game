using Tag.Art;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.FX
{
    /// <summary>
    /// Visual amounts for the verb FX layer. Nothing here writes speed, stun, or timing.
    /// </summary>
    public static class FxAmount
    {
        public const int Off = 0;
        public const int Low = 1;
        public const int Full = 2;
        public const float LowDensity = 0.5f;

        public static bool Show(GameSettings settings)
        {
            if (settings == null) return true;
            if (settings.AnyReduceFlash()) return false;
            return settings.Effects > Off;
        }

        public static float Density(GameSettings settings)
        {
            if (!Show(settings)) return 0f;
            if (settings != null && settings.Effects == Low) return LowDensity;
            return 1f;
        }
    }

    public static class VerbFxLook
    {
        public const float DashSeconds = 0.10f;
        public const int GhostsFull = 4;
        public const int GhostsLow = 3;
        public const float GhostLife = 0.12f;
        public const float ReleaseSeconds = 0.12f;
        public const float PadRing = 1.15f;
        public const float WindSeconds = 0.40f;
        public const float ShimmerAmp = 0.035f;
        public const float ZipPace = 14f;
        public const int RopePoints = 8;

        public static float LandRing(float impact)
        {
            float gate = LandingRollPose.Threshold;
            if (gate < 0.01f) gate = 0.01f;
            float t = impact / gate;
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            return 0.45f + t * 0.85f;
        }

        public static int Debris(float impact, float density)
        {
            if (impact < LandingRollPose.SoftFloor || density <= 0f) return 0;
            float gate = LandingRollPose.Threshold;
            float t = impact / gate;
            if (t < 0f) t = 0f;
            if (t > 1.35f) t = 1.35f;
            int n = 4 + (int)(t * 8f);
            if (n > 14) n = 14;
            n = (int)(n * density + 0.001f);
            if (n < 1) n = 1;
            return n;
        }

        public static bool RollSwirl(float impact, float planar)
        {
            return LandingRollPose.Triggered(impact) && !LandingRollPose.Stationary(planar);
        }

        public static int Ghosts(float density)
        {
            if (density <= 0f) return 0;
            if (density < 0.99f) return GhostsLow;
            return GhostsFull;
        }

        public static float RopeWobble(float tension)
        {
            if (tension < 0f) tension = 0f;
            if (tension > 1f) tension = 1f;
            return 0.012f + tension * 0.05f;
        }

        public static float RopeSag(float slack)
        {
            if (slack < 0f) slack = 0f;
            if (slack > 1f) slack = 1f;
            return 0.08f + slack * 0.35f;
        }

        public static void RopeOffset(float t, float tension, float slack, float time, out float lateral, out float drop)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            float bell = 4f * t * (1f - t);
            float wob = RopeWobble(tension) * (float)System.Math.Sin(time * 18f + t * 6.2f);
            lateral = wob * bell;
            drop = RopeSag(slack) * bell;
        }

        public static int HookBits(int surface, bool hard)
        {
            if (surface == (int)DustLook.Surface.Metal)
                return hard ? 3 : 0;
            if (surface == (int)DustLook.Surface.Wet)
                return 4;
            return 2;
        }

        public static int ZipSparks(float speed, float density)
        {
            if (speed < 0f) speed = 0f;
            if (density <= 0f) return 0;
            int n = (int)(speed / ZipPace * 8f);
            if (n > 10) n = 10;
            if (n < 1) n = 1;
            n = (int)(n * density + 0.001f);
            if (n < 1) n = 1;
            return n;
        }

        public static float ScuffLength(float speed)
        {
            if (speed < 0f) speed = 0f;
            float t = speed / DustLook.Sprint;
            if (t > 1.4f) t = 1.4f;
            return 0.15f + t * 0.40f;
        }

        public static bool Drip(int surface)
        {
            return surface == (int)DustLook.Surface.Wet;
        }

        public static int Wisps(float speed, float density)
        {
            if (density <= 0f || speed < DustLook.Sprint) return 0;
            if (density < 0.99f) return 2;
            return 4;
        }

        public static float Rim(float remaining, float duration, float time)
        {
            if (duration <= 0.0001f || remaining <= 0f) return 0f;
            float env = remaining / duration;
            if (env > 1f) env = 1f;
            float wave = 0.62f + 0.38f * (float)System.Math.Sin(time * 9f);
            return env * wave;
        }

        public const float InkR = 0.08f;
        public const float InkG = 0.07f;
        public const float InkB = 0.06f;
        public const float InkA = 0.90f;
        /// <summary>About 1 px of dark edge at the pass-28 chase.</summary>
        public const float InkWorld = 0.028f;

        /// <summary>Seat 0 red, 1 blue, 2 orange, 3 lavender. Same RGB as BodyFoam.</summary>
        public static void PlayerColor(int seat, out float r, out float g, out float b)
        {
            BodyFoam.Rgb c = BodyFoam.ForSeat(seat);
            r = c.R;
            g = c.G;
            b = c.B;
        }

        public static string ProofLine()
        {
            float ring = LandRing(LandingRollPose.Threshold);
            int debris = Debris(LandingRollPose.Threshold, 1f);
            int swirl = RollSwirl(LandingRollPose.Threshold, 3f) ? 1 : 0;
            int ghosts = Ghosts(1f);
            float sag = RopeSag(1f);
            int hook = HookBits((int)DustLook.Surface.Dirt, false);
            int zip = ZipSparks(ZipPace, 1f);
            int drip = Drip((int)DustLook.Surface.Wet) ? 1 : 0;
            int wisps = Wisps(DustLook.Sprint, 1f);
            float scuff = ScuffLength(DustLook.Sprint);
            float rim = Rim(TagBackImmunity.DefaultSeconds, TagBackImmunity.DefaultSeconds, 0.4f);
            int level = GameSettings.Defaults().Effects;
            return "fx-verbs"
                + " landRing=" + ring.ToString("0.00")
                + " debris=" + debris.ToString()
                + " swirl=" + swirl.ToString()
                + " ghosts=" + ghosts.ToString()
                + " dash=" + DashSeconds.ToString("0.00")
                + " sag=" + sag.ToString("0.00")
                + " hook=" + hook.ToString()
                + " snap=" + ReleaseSeconds.ToString("0.00")
                + " pad=" + PadRing.ToString("0.00")
                + " zip=" + zip.ToString()
                + " dizzy=" + PunchStagger.Duration.ToString("0.00")
                + " rim=" + rim.ToString("0.00")
                + " scuff=" + scuff.ToString("0.00")
                + " drip=" + drip.ToString()
                + " wisps=" + wisps.ToString()
                + " level=" + level.ToString()
                + " low=" + FxAmount.LowDensity.ToString("0.00")
                + " tilt=" + ComicWords.TiltDegrees.ToString("0")
                + " spikes=" + ComicArt.SpikesFor(2).ToString()
                + " outline=1";
        }

        public static bool Holds()
        {
            if (DashSeconds < 0.099f || DashSeconds > 0.101f) return false;
            if (System.Math.Abs(ZipPace - 14f) > 0.001f) return false;
            if (System.Math.Abs(PunchStagger.Duration - 0.25f) > 0.001f) return false;
            if (System.Math.Abs(TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f) return false;
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f) return false;
            if (!ComicArt.Holds()) return false;
            if (System.Math.Abs(ComicWords.TiltDegrees - 12f) > 0.01f) return false;

            float light = LandRing(8f);
            float heavy = LandRing(LandingRollPose.Threshold);
            if (!(light < heavy) || heavy < 1.2f) return false;
            if (Debris(8f, 1f) >= Debris(LandingRollPose.Threshold, 1f)) return false;
            if (Debris(LandingRollPose.Threshold, FxAmount.LowDensity) >= Debris(LandingRollPose.Threshold, 1f)) return false;
            if (Debris(1f, 1f) != 0) return false;
            if (!RollSwirl(LandingRollPose.Threshold, 3f)) return false;
            if (RollSwirl(LandingRollPose.Threshold, 0.2f)) return false;
            if (RollSwirl(10f, 3f)) return false;

            if (Ghosts(1f) != GhostsFull || Ghosts(FxAmount.LowDensity) != GhostsLow || Ghosts(0f) != 0) return false;
            if (RopeWobble(1f) <= RopeWobble(0f)) return false;
            if (RopeSag(1f) <= RopeSag(0f)) return false;
            RopeOffset(0f, 1f, 1f, 0.2f, out float endLat, out float endDrop);
            RopeOffset(0.5f, 1f, 1f, 0.2f, out float midLat, out float midDrop);
            if (endDrop > 0.001f || midDrop <= endDrop) return false;
            if (HookBits((int)DustLook.Surface.Metal, false) != 0) return false;
            if (HookBits((int)DustLook.Surface.Metal, true) < 2) return false;
            if (HookBits((int)DustLook.Surface.Wet, false) < HookBits((int)DustLook.Surface.Concrete, false)) return false;
            if (ZipSparks(ZipPace, 1f) <= ZipSparks(ZipPace * 0.5f, 1f)) return false;
            if (ScuffLength(DustLook.Sprint) <= ScuffLength(6.9f)) return false;
            if (!Drip((int)DustLook.Surface.Wet) || Drip((int)DustLook.Surface.Concrete)) return false;
            if (Wisps(DustLook.Walk, 1f) != 0 || Wisps(DustLook.Sprint, 1f) < 3) return false;
            if (Wisps(DustLook.Sprint, FxAmount.LowDensity) >= Wisps(DustLook.Sprint, 1f)) return false;
            if (Rim(0f, 1f, 0.2f) != 0f || Rim(1f, 1f, 0.4f) < 0.2f) return false;

            GameSettings off = GameSettings.Defaults();
            if (off.Effects != FxAmount.Full) return false;
            if (!FxAmount.Show(off) || FxAmount.Density(off) < 0.99f) return false;
            off.Effects = FxAmount.Low;
            if (FxAmount.Density(off) > 0.51f || FxAmount.Density(off) < 0.49f) return false;
            off.Effects = FxAmount.Off;
            if (FxAmount.Show(off) || FxAmount.Density(off) != 0f) return false;
            off.Effects = FxAmount.Full;
            off.ReduceFlash[0] = true;
            if (FxAmount.Show(off)) return false;

            string blob = SettingsFile.Write(GameSettings.Defaults(), ActionBinds.Defaults());
            if (blob.IndexOf("effects=2", System.StringComparison.Ordinal) < 0) return false;
            GameSettings loaded = GameSettings.Defaults();
            loaded.Effects = FxAmount.Off;
            SettingsFile.Read("v=2\neffects=1\n", loaded, ActionBinds.Defaults());
            if (loaded.Effects != FxAmount.Low) return false;
            loaded.Nudge(GameSettings.RowEffects, 1);
            if (loaded.Effects != FxAmount.Full) return false;
            if (GameSettings.RowCount != 21) return false;
            PlayerColor(0, out float sr, out float sg, out float sb);
            if (sr != BodyFoam.Red.R || sg != BodyFoam.Red.G || sb != BodyFoam.Red.B) return false;
            PlayerColor(1, out sr, out sg, out sb);
            if (sr != BodyFoam.Blue.R || sg != BodyFoam.Blue.G || sb != BodyFoam.Blue.B) return false;
            PlayerColor(2, out sr, out sg, out sb);
            if (sr != BodyFoam.Orange.R || sg != BodyFoam.Orange.G || sb != BodyFoam.Orange.B) return false;
            PlayerColor(3, out sr, out sg, out sb);
            if (sr != BodyFoam.Lavender.R || sg != BodyFoam.Lavender.G || sb != BodyFoam.Lavender.B) return false;
            return true;
        }
    }
}
