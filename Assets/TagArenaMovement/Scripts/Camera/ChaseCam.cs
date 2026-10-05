using System;
using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Chase boom and look-ahead. A wall pulls the boom in on the same frame.
    /// A wall jump or a slide catches the look up inside the 0.10s pose window.
    /// A normal turn keeps the slow slew.
    /// </summary>
    public static class ChaseCam
    {
        public const float AheadRate = 4.5f;
        public const float CatchRate = 22f;
        /// <summary>Same length as the wall-jump and slide pose windows.</summary>
        public const float CatchSeconds = 0.10f;
        public const float BoomRate = 18f;
        public const float LookRate = 8f;

        public static float Remaining(float rate, float seconds)
        {
            if (seconds <= 0f || rate <= 0f) return 1f;
            return (float)Math.Exp(-rate * seconds);
        }

        public static bool WantsCatchup(bool enteredSlide, bool wallToAir)
        {
            return enteredSlide || wallToAir;
        }

        public static float AheadRateFor(float catchRemaining)
        {
            return catchRemaining > 0f ? CatchRate : AheadRate;
        }

        public static float LookRateFor(float catchRemaining)
        {
            return catchRemaining > 0f ? CatchRate : LookRate;
        }

        /// <summary>Shorter than the current boom snaps. Longer eases, faster during a catch-up.</summary>
        public static float BoomDistance(float current, float desired, float dt, bool catchup)
        {
            if (desired < current) return desired;
            float rate = catchup ? CatchRate : BoomRate;
            float t = 1f - Remaining(rate, dt);
            return current + (desired - current) * t;
        }

        public static bool Holds()
        {
            if (Mathf.Abs(CatchSeconds - 0.10f) > 0.001f) return false;
            float slow = Remaining(AheadRate, CatchSeconds);
            float fast = Remaining(CatchRate, CatchSeconds);
            if (slow < 0.50f) return false;
            if (fast > 0.15f) return false;
            if (!WantsCatchup(true, false) || !WantsCatchup(false, true)) return false;
            if (WantsCatchup(false, false)) return false;
            if (Mathf.Abs(AheadRateFor(0f) - AheadRate) > 0.001f) return false;
            if (Mathf.Abs(AheadRateFor(0.05f) - CatchRate) > 0.001f) return false;
            if (Mathf.Abs(LookRateFor(0.05f) - CatchRate) > 0.001f) return false;
            if (Mathf.Abs(BoomDistance(5.2f, 0.55f, 0.016f, false) - 0.55f) > 0.001f) return false;
            float outSlow = BoomDistance(0.55f, 5.2f, CatchSeconds, false);
            float outFast = BoomDistance(0.55f, 5.2f, CatchSeconds, true);
            if (!(outFast > outSlow)) return false;
            return true;
        }

        public static string ProofLine()
        {
            float slow = Remaining(AheadRate, CatchSeconds);
            float fast = Remaining(CatchRate, CatchSeconds);
            return "chase cam"
                + " aheadLag@" + CatchSeconds.ToString("0.00") + "s=" + slow.ToString("0.00")
                + " catchLag=" + fast.ToString("0.00")
                + " boomSnapIn=yes"
                + " wallJump+slide=" + CatchSeconds.ToString("0.00") + "s";
        }
    }
}
