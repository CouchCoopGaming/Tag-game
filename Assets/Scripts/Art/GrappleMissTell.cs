using Tag.Local;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Miss read for the solo planar rope. Presentation only.
    /// A fired shot that latches nothing flicks a thin stub along the aim
    /// and a small knot at the hand. Both collapse and fade.
    /// A latch, a hold with no new fire, and an unavailable grapple stay quiet.
    /// Cooler and fainter than the gold latch flash. No lamp, no extra volume, no camera pop.
    /// Does not change range, slack, pull, or the planar speed strip.
    /// </summary>
    public static class GrappleMissTell
    {
        /// <summary>How long the fail cue stays up after a fired miss.</summary>
        public const float FlashSeconds = 0.24f;
        /// <summary>Peak opacity. Low enough that the body stays readable, and under the latch flash.</summary>
        public const float MaxAlpha = 0.36f;
        /// <summary>Opening length of the stub. Shorter than the aim line, so it reads as a failed flick.</summary>
        public const float StubLength = 0.95f;
        /// <summary>Width of the stub. Thinner than the aim line and the gold rope.</summary>
        public const float StubWidth = 0.045f;
        /// <summary>World size of the knot at the hand. Smaller than the latch knot.</summary>
        public const float KnotSize = 0.20f;
        /// <summary>Muted steel. Blue above red, so it does not read as the gold rope.</summary>
        public const float MarkR = 0.42f;
        public const float MarkG = 0.56f;
        public const float MarkB = 0.68f;
        /// <summary>No bloom. The chase camera must not wash out on a miss.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "GrappleMissTell";

        /// <summary>Solo human only. Couch pawns and the campus opponent do not.</summary>
        public static bool ForPawn(bool couch, bool ai, int index, string pawnName)
        {
            return SoloGrappleGate.EnableFor(couch, ai, index, pawnName);
        }

        /// <summary>
        /// Arm on a fired miss. A latch clears the cue.
        /// A hold with no new fire does not arm and does not restart a cue already playing.
        /// An unavailable grapple clears the cue.
        /// </summary>
        public static void Note(ref float age, bool available, bool fired, bool missed, bool latched)
        {
            if (!available || latched)
            {
                age = -1f;
                return;
            }

            if (fired && missed)
                age = 0f;
        }

        /// <summary>Drop the cue. An unavailable grapple and a disable use this.</summary>
        public static void Clear(ref float age) => age = -1f;

        /// <summary>True only while the grapple is available, nothing is latched, and the window is open.</summary>
        public static bool Show(bool available, bool latched, float age)
        {
            return available && !latched && age >= 0f && age < FlashSeconds;
        }

        /// <summary>1 at the miss, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            return 1f - (age / FlashSeconds);
        }

        public static float Alpha(float age) => MaxAlpha * Fade(age);

        /// <summary>Full size at the miss, shrinking as the flick fades.</summary>
        public static float KnotScale(float fade) => KnotSize * Mathf.Lerp(0.35f, 1f, fade);

        /// <summary>Knot flick sits on the hand. A latch returns false.</summary>
        public static bool Knot(bool available, bool latched, Vector3 hand, float age, out Vector3 point)
        {
            point = hand;
            return Show(available, latched, age);
        }

        /// <summary>
        /// Thin stub from the hand along the aim. Age 0 is the full stub.
        /// It collapses back into the hand. A latch, a hold, and a closed window return false.
        /// </summary>
        public static bool Stub(bool available, bool latched, Vector3 hand, Vector3 aimDir, float age, out Vector3 from, out Vector3 to)
        {
            from = hand;
            to = hand;
            if (!Show(available, latched, age)) return false;
            if (aimDir.sqrMagnitude < 1e-6f) return false;

            float len = StubLength * Fade(age);
            if (len < 0.05f) return false;

            to = hand + aimDir.normalized * len;
            return (to - from).sqrMagnitude > 0.0004f;
        }

        /// <summary>
        /// Advance one frame. Returns false once the window has closed.
        /// A latch or an unavailable grapple clears and stays quiet.
        /// </summary>
        public static bool Step(ref float age, float dt, bool available, bool latched)
        {
            if (!Show(available, latched, age))
            {
                age = -1f;
                return false;
            }

            if (dt > 0f) age += dt;
            if (!Show(available, latched, age))
            {
                age = -1f;
                return false;
            }

            return true;
        }
    }
}
