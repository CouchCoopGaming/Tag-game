using Tag.Local;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Cooldown read for the solo pawn's air dash. Presentation only.
    /// A thin arc refills near the feet while the dash is recharging.
    /// When the cooldown ends, that arc closes and winks out.
    /// Opponent and couch pawns stay quiet. No lamp, no extra volume, no camera pop.
    /// Does not change dash duration, speed, cooldown, or the burst ribbons.
    /// </summary>
    public static class AirDashCooldownTell
    {
        /// <summary>How long the ready wink stays up after the cooldown hits zero.</summary>
        public const float ReadyFlashSeconds = 0.25f;
        /// <summary>Peak opacity. Low enough that the body stays readable.</summary>
        public const float MaxAlpha = 0.42f;
        /// <summary>World radius of the arc. Just outside the capsule, still close to the pawn.</summary>
        public const float RingRadius = 0.52f;
        /// <summary>Height above the pawn origin. Knee height, under the burst wings.</summary>
        public const float RingHeight = 0.36f;
        /// <summary>Stroke width. Thin, so the arc does not read as a second body.</summary>
        public const float RingWidth = 0.05f;
        /// <summary>Smallest visible fraction of the circle, so the recharge is there on the dash frame.</summary>
        public const float MinArc = 0.06f;
        public const int Segments = 28;
        public const float MarkR = 0.48f;
        public const float MarkG = 0.78f;
        public const float MarkB = 0.86f;
        /// <summary>No bloom. The chase camera must not wash out on the ready wink.</summary>
        public const float Glow = 0f;
        public const float CoolingEpsilon = 0.001f;
        public const float Tau = 6.2831855f;
        public const string MarkerName = "AirDashCooldownTell";

        public static int MaxPoints => Segments + 1;

        /// <summary>Solo human only. Couch pawns and the campus opponent do not.</summary>
        public static bool ForPawn(bool couch, bool ai, int index, string pawnName)
        {
            return SoloGrappleGate.EnableFor(couch, ai, index, pawnName);
        }

        /// <summary>0 at the start of the cooldown, 1 when it is ready.</summary>
        public static float Fill(float remaining, float cooldown)
        {
            float max = cooldown > 0.01f ? cooldown : 0.01f;
            float rem = remaining < 0f ? 0f : remaining;
            if (rem > max) rem = max;
            return 1f - (rem / max);
        }

        /// <summary>Arc fraction actually drawn. A hair stays visible at fill 0. A full cooldown closes the ring.</summary>
        public static float VisibleFill(float fill)
        {
            float f = Mathf.Clamp01(fill);
            if (f >= 0.999f) return 1f;
            return MinArc + (1f - MinArc) * f;
        }

        public static int PointCount(float visibleFill)
        {
            float f = visibleFill < 0f ? 0f : (visibleFill > 1f ? 1f : visibleFill);
            if (f <= 0.0001f) return 0;
            if (f >= 0.999f) return Segments + 1;
            int n = 2 + (int)(f * (Segments - 2));
            if (n < 2) n = 2;
            if (n > Segments) n = Segments;
            return n;
        }

        /// <summary>
        /// Horizontal arc around the pawn. visibleFill 1 closes the ring.
        /// A zero fraction returns false. The caller gates solo and cooldown.
        /// </summary>
        public static bool Arc(Vector3 origin, float visibleFill, Vector3[] buffer, out int count)
        {
            count = 0;
            float f = visibleFill < 0f ? 0f : (visibleFill > 1f ? 1f : visibleFill);
            if (f <= 0.0001f) return false;
            int n = PointCount(f);
            if (buffer == null || n < 2 || buffer.Length < n) return false;

            bool closed = f >= 0.999f;
            float sweep = closed ? Tau : f * Tau;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float ang = sweep * u;
                buffer[i] = origin + new Vector3(Mathf.Sin(ang) * RingRadius, RingHeight, Mathf.Cos(ang) * RingRadius);
            }

            if (closed)
                buffer[n - 1] = buffer[0];
            count = n;
            return true;
        }

        /// <summary>Refill arc. Hidden during the ready wink, and hidden once the dash is ready.</summary>
        public static bool ShowRing(bool solo, float remaining, float readyAge)
        {
            return solo && readyAge < 0f && remaining > CoolingEpsilon;
        }

        /// <summary>Ready wink. Solo only, and only inside the short window.</summary>
        public static bool ShowFlash(bool solo, float readyAge)
        {
            return solo && readyAge >= 0f && readyAge < ReadyFlashSeconds;
        }

        /// <summary>1 at the ready edge, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= ReadyFlashSeconds) return 0f;
            return 1f - (age / ReadyFlashSeconds);
        }

        public static float FlashAlpha(float age) => MaxAlpha * Fade(age);

        /// <summary>
        /// Watch the cooldown. A falling edge arms the ready wink once.
        /// A fresh pawn that is already ready does not wink.
        /// A new dash cancels a wink that is still up.
        /// </summary>
        public static void Note(ref bool seenCooling, ref float readyAge, bool solo, float remaining)
        {
            if (!solo)
            {
                seenCooling = false;
                readyAge = -1f;
                return;
            }

            bool cooling = remaining > CoolingEpsilon;
            if (cooling)
            {
                seenCooling = true;
                readyAge = -1f;
                return;
            }

            if (seenCooling && readyAge < 0f)
            {
                readyAge = 0f;
                return;
            }

            if (!seenCooling && readyAge <= 0f)
                readyAge = -1f;
        }

        /// <summary>
        /// Advance the ready wink. Returns false once it has closed.
        /// Clears the cooling latch when the wink ends so it does not restart.
        /// </summary>
        public static bool StepFlash(ref bool seenCooling, ref float readyAge, float dt, bool solo)
        {
            if (!ShowFlash(solo, readyAge))
            {
                if (!solo)
                {
                    seenCooling = false;
                    readyAge = -1f;
                }
                return false;
            }

            if (dt > 0f) readyAge += dt;
            if (!ShowFlash(solo, readyAge))
            {
                seenCooling = false;
                readyAge = -1f;
                return false;
            }

            return true;
        }
    }
}
