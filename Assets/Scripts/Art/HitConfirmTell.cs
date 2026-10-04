using Tag.Local;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Connect read for the solo pawn. Presentation only.
    /// Two small rings sit at the punch reach when a hit lands or a tag connects.
    /// A whiff stays quiet. No lamp, no extra volume, no camera pop.
    /// Does not change reach, lunge speed, or the PunchStrike / TagCatch clips.
    /// </summary>
    public static class HitConfirmTell
    {
        /// <summary>How long the rings stay up after a connect.</summary>
        public const float FlashSeconds = 0.26f;
        /// <summary>Peak opacity. Low enough that the body stays readable.</summary>
        public const float MaxAlpha = 0.42f;
        /// <summary>Inner ring radius at the contact. Small, so the fist still reads.</summary>
        public const float InnerRadius = 0.18f;
        /// <summary>Outer ring radius. Still a local pop, not a ground halo.</summary>
        public const float OuterRadius = 0.34f;
        /// <summary>Stroke width. Thin, so the rings do not hide the arm.</summary>
        public const float RingWidth = 0.04f;
        public const int RingCount = 2;
        public const int Segments = 16;
        /// <summary>Matches PunchTagTuning.midTorsoHeight. The rings sit on the strike, not the ground.</summary>
        public const float ContactHeight = 1.00f;
        /// <summary>TagCatch is on above this flinch, the same gate as the clip.</summary>
        public const float TagCatchOn = 0.04f;
        /// <summary>BecomeIt claim is on above this, the same gate as the claim pose.</summary>
        public const float BecomeItOn = 0.20f;
        public const float MarkR = 0.96f;
        public const float MarkG = 0.42f;
        public const float MarkB = 0.22f;
        /// <summary>No bloom. The chase camera must not wash out on the connect.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const float Tau = 6.2831855f;
        public const string MarkerName = "HitConfirmTell";

        public static int MaxPoints => Segments + 1;

        /// <summary>Solo human only. Couch pawns and the campus opponent do not.</summary>
        public static bool ForPawn(bool couch, bool ai, int index, string pawnName)
        {
            return SoloGrappleGate.EnableFor(couch, ai, index, pawnName);
        }

        /// <summary>TagCatch clip is playing. The flinch gate is the existing one.</summary>
        public static bool TagCatch(float flinch) => flinch > TagCatchOn;

        /// <summary>BecomeIt claim pose is playing.</summary>
        public static bool BecomeIt(float claim) => claim > BecomeItOn;

        /// <summary>A tag landed: TagCatch, BecomeIt, or both on the same pawn.</summary>
        public static bool TagConnect(float flinch, float claim) => TagCatch(flinch) || BecomeIt(claim);

        /// <summary>
        /// Arm on the rising edge of a punch hit or a tag connect.
        /// A held pose does not re-arm. A whiff passes both flags false and stays quiet.
        /// Leaving the solo pawn clears the rings.
        /// </summary>
        public static void Note(ref float age, bool solo, bool punchHit, bool tagConnect, bool wasPunchHit, bool wasTag)
        {
            if (!solo)
            {
                age = -1f;
                return;
            }

            bool rise = (punchHit && !wasPunchHit) || (tagConnect && !wasTag);
            if (rise)
                age = 0f;
        }

        /// <summary>True only for the solo pawn, inside the short window.</summary>
        public static bool Show(bool solo, float age)
        {
            return solo && age >= 0f && age < FlashSeconds;
        }

        /// <summary>1 at the connect, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            return 1f - (age / FlashSeconds);
        }

        public static float Alpha(float age) => MaxAlpha * Fade(age);

        /// <summary>
        /// Contact is reach meters along planar forward, at torso height.
        /// Reach is the caller's punch reach. A zero forward falls back to +Z
        /// so the rings do not sit inside the body.
        /// </summary>
        public static Vector3 Contact(Vector3 origin, Vector3 forward, float reach)
        {
            Vector3 flat = forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-6f)
                flat = new Vector3(0f, 0f, 1f);
            else
                flat.Normalize();
            float span = reach > 0f ? reach : 0f;
            return origin + flat * span + Vector3.up * ContactHeight;
        }

        /// <summary>
        /// Vertical ring facing back along forward, centered on the contact.
        /// A zero radius or a short buffer returns false.
        /// </summary>
        public static bool Ring(Vector3 contact, Vector3 forward, float radius, Vector3[] buffer, out int count)
        {
            count = 0;
            if (radius < 0.02f) return false;
            if (buffer == null || buffer.Length < MaxPoints) return false;

            Vector3 flat = forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-6f)
                flat = new Vector3(0f, 0f, 1f);
            else
                flat.Normalize();
            Vector3 side = new Vector3(flat.z, 0f, -flat.x);

            for (int i = 0; i <= Segments; i++)
            {
                float ang = Tau * (i / (float)Segments);
                buffer[i] = contact + side * (Mathf.Sin(ang) * radius) + Vector3.up * (Mathf.Cos(ang) * radius);
            }

            count = MaxPoints;
            return true;
        }

        /// <summary>
        /// Advance one frame. Returns false once the window has closed.
        /// A quiet pawn does not arm itself.
        /// </summary>
        public static bool Step(ref float age, float dt, bool solo)
        {
            if (!Show(solo, age))
            {
                if (!solo)
                    age = -1f;
                return false;
            }

            if (dt > 0f) age += dt;
            if (!Show(solo, age))
            {
                age = -1f;
                return false;
            }

            return true;
        }
    }
}
