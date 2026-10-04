using Tag.Local;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Impact read for the solo pawn when a jump or a real drop meets the ground.
    /// Presentation only. A short body thud and two heel puffs.
    /// Arms only after <see cref="MinAirSeconds"/> off the ground, which is longer
    /// than the 0.10s coyote window, so a probe flicker or a coyote re-ground stays quiet.
    /// Slide enter, wall cling, and a mantle plant do not arm it.
    /// No lamp, no extra volume, no camera pop, no slow-mo.
    /// Does not change jump height, coyote, or the one kinematic step.
    /// </summary>
    public static class JumpLandTell
    {
        /// <summary>How long the thud and the puffs stay up. Inside 0.15–0.35s.</summary>
        public const float FlashSeconds = 0.24f;
        /// <summary>
        /// Continuous air required before a touchdown counts.
        /// Above coyote (0.10) so a re-ground inside that window stays quiet.
        /// A full jump is far longer than this.
        /// </summary>
        public const float MinAirSeconds = 0.22f;
        /// <summary>Body height at the impact frame. 1 is the pose scale.</summary>
        public const float ThudY = 0.76f;
        /// <summary>Body width at the impact frame. 1 is the pose scale.</summary>
        public const float ThudXZ = 1.14f;
        /// <summary>Peak puff opacity. The legs stay readable.</summary>
        public const float MaxAlpha = 0.58f;
        public const int PuffCount = 2;
        /// <summary>Disc diameter at the impact frame.</summary>
        public const float PuffTight = 0.16f;
        /// <summary>Disc diameter as the puff blooms out.</summary>
        public const float PuffWide = 0.32f;
        /// <summary>Meters above the pawn origin. On the ground, at the heels.</summary>
        public const float PuffHeight = 0.06f;
        /// <summary>Lateral split from the center line. Inside the capsule, at the feet.</summary>
        public const float FootOffset = 0.15f;
        /// <summary>Meters behind the origin along travel, so the puffs sit at the heels.</summary>
        public const float HeelBack = 0.18f;
        /// <summary>Dry heel dust. Not the punch rings and not a white flash.</summary>
        public const float MarkR = 0.86f;
        public const float MarkG = 0.72f;
        public const float MarkB = 0.46f;
        /// <summary>No bloom. The chase camera must not wash out on the land.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "JumpLandTell";

        /// <summary>Outer span of the pair, including the wide puff. Stays at the feet.</summary>
        public static float PairSpan => FootOffset * 2f + PuffWide;

        /// <summary>Solo human only. Couch pawns and the campus opponent do not.</summary>
        public static bool ForPawn(bool couch, bool ai, int index, string pawnName)
        {
            return SoloGrappleGate.EnableFor(couch, ai, index, pawnName);
        }

        /// <summary>
        /// Track air time and arm on a landing edge.
        /// A slide, a wall cling, or a mantle clears the cue and does not arm.
        /// Leaving the ground clears a cue that was still playing.
        /// Tiny air and a coyote re-ground do not arm.
        /// </summary>
        public static void Note(
            ref float age,
            ref float airTime,
            bool solo,
            bool grounded,
            bool wasGrounded,
            bool slide,
            bool cling,
            float dt)
        {
            if (!solo)
            {
                age = -1f;
                airTime = 0f;
                return;
            }

            if (slide || cling)
            {
                age = -1f;
                if (grounded)
                    airTime = 0f;
                else if (dt > 0f)
                    airTime += dt;
                return;
            }

            if (!grounded)
            {
                age = -1f;
                if (dt > 0f) airTime += dt;
                return;
            }

            if (!wasGrounded && airTime >= MinAirSeconds)
                age = 0f;

            airTime = 0f;
        }

        /// <summary>Drop the cue.</summary>
        public static void Clear(ref float age) => age = -1f;

        /// <summary>True only for the solo pawn, inside the short window.</summary>
        public static bool Show(bool solo, float age)
        {
            return solo && age >= 0f && age < FlashSeconds;
        }

        /// <summary>1 at the impact, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            return 1f - (age / FlashSeconds);
        }

        public static float Alpha(float age) => MaxAlpha * Fade(age);

        /// <summary>
        /// Extra scale on the pose. Fade 1 is the thud. Fade 0 leaves the pose alone.
        /// </summary>
        public static void ThudScale(float fade, out float y, out float xz)
        {
            y = Mathf.Lerp(1f, ThudY, fade);
            xz = Mathf.Lerp(1f, ThudXZ, fade);
        }

        /// <summary>Tight at the impact, wider as the puff dies.</summary>
        public static float PuffDiameter(float fade) => Mathf.Lerp(PuffWide, PuffTight, fade);

        /// <summary>
        /// Heel puffs. Left and right are across travel, behind the origin.
        /// A zero direction returns false so the caller can skip the frame.
        /// </summary>
        public static bool Heels(Vector3 origin, Vector3 travel, out Vector3 left, out Vector3 right)
        {
            Vector3 dir = travel;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
            {
                left = origin + Vector3.up * PuffHeight;
                right = left;
                return false;
            }

            dir.Normalize();
            Vector3 side = FlatSide(dir);
            Vector3 heel = origin - dir * HeelBack + Vector3.up * PuffHeight;
            left = heel - side * FootOffset;
            right = heel + side * FootOffset;
            return true;
        }

        /// <summary>Cross(up, dir) for a flattened direction: (dir.z, 0, -dir.x).</summary>
        static Vector3 FlatSide(Vector3 dir)
        {
            Vector3 side = new Vector3(dir.z, 0f, -dir.x);
            if (side.sqrMagnitude < 1e-6f)
                return Vector3.right;
            side.Normalize();
            return side;
        }

        /// <summary>
        /// Advance one frame. Returns false once the window has closed.
        /// A pawn that is not the solo human clears and stays quiet.
        /// </summary>
        public static bool Step(ref float age, float dt, bool solo)
        {
            if (!Show(solo, age))
            {
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
