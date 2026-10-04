using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Latch-success read for the solo planar rope. Presentation only.
    /// A knot flash sits on the hit. A short pulse runs the hand-to-knot span.
    /// Aim and a miss stay quiet. No lamp, no extra volume, no camera pop.
    /// Does not change range, slack, pull, or the planar speed strip.
    /// </summary>
    public static class GrappleLatchTell
    {
        /// <summary>How long the knot flash and the rope pulse stay up after a real latch.</summary>
        public const float FlashSeconds = 0.25f;
        /// <summary>Peak opacity. Low enough that the body and the rope stay readable.</summary>
        public const float MaxAlpha = 0.44f;
        /// <summary>World diameter of the knot flash. Larger than the resting knot so a latch reads at chase-cam distance.</summary>
        public const float KnotSize = 0.58f;
        /// <summary>Length of the traveling segment. Short, so it reads as a pulse and not a second rope.</summary>
        public const float PulseLength = 1.15f;
        /// <summary>Width of that segment. Wider than the rope, still narrow enough to leave the rope visible.</summary>
        public const float PulseWidth = 0.22f;
        public const float MarkR = 0.93f;
        public const float MarkG = 0.76f;
        public const float MarkB = 0.30f;
        /// <summary>No bloom. The chase camera must not wash out on the attach.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "GrappleLatchTell";

        /// <summary>Start the flash at the moment a latch succeeds.</summary>
        public static void Arm(ref float age) => age = 0f;

        /// <summary>Drop the flash. A release, a miss, and aim all stay quiet.</summary>
        public static void Clear(ref float age) => age = -1f;

        /// <summary>
        /// True only while a latch is held and the flash window is still open.
        /// A miss and the aim cue pass latched false, or a negative age, and stay off.
        /// </summary>
        public static bool Show(bool latched, float age)
        {
            return latched && age >= 0f && age < FlashSeconds;
        }

        /// <summary>1 at the latch, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            return 1f - (age / FlashSeconds);
        }

        public static float Alpha(float age) => MaxAlpha * Fade(age);

        /// <summary>Full size at the latch, easing down as the flash fades.</summary>
        public static float KnotScale(float fade) => KnotSize * Mathf.Lerp(0.65f, 1f, fade);

        /// <summary>Knot flash sits on the latch point. A miss returns false.</summary>
        public static bool Knot(bool latched, Vector3 anchor, float age, out Vector3 point)
        {
            point = anchor;
            return Show(latched, age);
        }

        /// <summary>
        /// Short segment on the hand-to-knot span. Age 0 starts at the hand.
        /// The window ends on the knot. A miss, aim, and a closed window return false.
        /// </summary>
        public static bool Pulse(bool latched, Vector3 hand, Vector3 knot, float age, out Vector3 from, out Vector3 to)
        {
            from = hand;
            to = hand;
            if (!Show(latched, age)) return false;

            Vector3 span = knot - hand;
            float len = span.magnitude;
            if (len < 0.05f) return false;

            float u = age / FlashSeconds;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            Vector3 dir = span * (1f / len);
            float half = PulseLength * 0.5f;
            float center = u * len;
            float a = center - half;
            float b = center + half;
            if (a < 0f)
            {
                b -= a;
                a = 0f;
            }

            if (b > len)
            {
                a -= b - len;
                b = len;
            }

            if (a < 0f) a = 0f;
            from = hand + dir * a;
            to = hand + dir * b;
            return (to - from).sqrMagnitude > 0.0004f;
        }

        /// <summary>
        /// Advance one frame. Returns false once the window has closed.
        /// A miss does not arm itself: latched false clears and stays quiet.
        /// </summary>
        public static bool Step(ref float age, float dt, bool latched)
        {
            if (!Show(latched, age))
            {
                age = -1f;
                return false;
            }

            if (dt > 0f) age += dt;
            if (!Show(latched, age))
            {
                age = -1f;
                return false;
            }

            return true;
        }
    }
}
