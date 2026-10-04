using Tag.Local;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Push-off read for the solo pawn when a wall climb or a wall run
    /// with cling takes Jump and leaves the wall into the air.
    /// Presentation only. The pose kicks a short way along the wall normal
    /// and two scuffs sit on the wall at the feet.
    /// Ground jump, coyote re-ground, slide start, mantle, and cling
    /// without Jump stay quiet. A frame the land tell is showing does not
    /// also arm this.
    /// No lamp, no extra volume, no camera punch, no slow-mo.
    /// Does not change jump speed, coyote, cling grace, or the one kinematic step.
    /// </summary>
    public static class WallJumpPushTell
    {
        /// <summary>How long the kick and the scuffs stay up. Inside 0.15–0.30s.</summary>
        public const float FlashSeconds = 0.22f;
        /// <summary>Meters the pose shifts along the wall normal at the first frame. Away from the wall.</summary>
        public const float PushOffset = 0.18f;
        /// <summary>Body height at the kick frame. 1 is the pose scale.</summary>
        public const float StretchY = 1.09f;
        /// <summary>Body width at the kick frame. 1 is the pose scale.</summary>
        public const float StretchXZ = 0.92f;
        /// <summary>Peak scuff opacity. The legs stay readable.</summary>
        public const float MaxAlpha = 0.56f;
        public const int PuffCount = 2;
        /// <summary>Disc diameter at the kick frame.</summary>
        public const float PuffTight = 0.16f;
        /// <summary>Disc diameter as the scuff blooms.</summary>
        public const float PuffWide = 0.32f;
        /// <summary>Meters from the pawn origin toward the wall. On the capsule skin.</summary>
        public const float SurfaceOffset = 0.37f;
        /// <summary>Meters above the pawn origin. At the shoes, under the cling hand marks.</summary>
        public const float FootHeight = 0.22f;
        /// <summary>Lateral split across the wall. Inside the capsule, at the feet.</summary>
        public const float FootSpread = 0.16f;
        /// <summary>Warm wall scuff. Not the gray cling marks and not a white flash.</summary>
        public const float MarkR = 0.82f;
        public const float MarkG = 0.70f;
        public const float MarkB = 0.48f;
        /// <summary>No bloom. The chase view must not wash out on the kick.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "WallJumpPushTell";

        /// <summary>Outer span of the pair, including the wide scuff. Stays at the feet.</summary>
        public static float PairSpan => FootSpread * 2f + PuffWide;

        /// <summary>Solo human only. Couch pawns and the campus opponent do not.</summary>
        public static bool ForPawn(bool couch, bool ai, int index, string pawnName)
        {
            return SoloGrappleGate.EnableFor(couch, ai, index, pawnName);
        }

        /// <summary>
        /// True only for a real wall-jump exit.
        /// Was on a wall climb or wall run, cling was held, Jump was pressed,
        /// and this frame is airborne and off the wall.
        /// Slide, mantle, a coyote re-ground, a ground jump, and a land tell
        /// that is already up all stay false.
        /// </summary>
        public static bool Qualifies(
            bool solo,
            bool wasWallClimbOrRun,
            bool clingHeld,
            bool jumpPressed,
            bool airborne,
            bool leftWall,
            bool slide,
            bool mantle,
            bool coyoteReground,
            bool groundJump,
            bool landTellBusy)
        {
            if (!solo) return false;
            if (slide || mantle || coyoteReground || groundJump || landTellBusy) return false;
            if (!wasWallClimbOrRun || !clingHeld || !jumpPressed) return false;
            if (!airborne || !leftWall) return false;
            return true;
        }

        /// <summary>
        /// Arm on a qualifying exit. A quiet frame does not clear a kick that is already up.
        /// A pawn that is not the solo human clears and stays quiet.
        /// </summary>
        public static void Note(ref float age, bool qualifies, bool solo)
        {
            if (!solo)
            {
                age = -1f;
                return;
            }

            if (qualifies && age < 0f)
                age = 0f;
        }

        /// <summary>Drop the cue.</summary>
        public static void Clear(ref float age) => age = -1f;

        /// <summary>True only for the solo pawn, inside the short window.</summary>
        public static bool Show(bool solo, float age)
        {
            return solo && age >= 0f && age < FlashSeconds;
        }

        /// <summary>1 at the kick, 0 when the window closes.</summary>
        public static float Fade(float age)
        {
            if (age < 0f || age >= FlashSeconds) return 0f;
            return 1f - (age / FlashSeconds);
        }

        public static float Alpha(float age) => MaxAlpha * Fade(age);

        /// <summary>
        /// Extra scale on the pose. Fade 1 is the kick. Fade 0 leaves the pose alone.
        /// </summary>
        public static void PushScale(float fade, out float y, out float xz)
        {
            y = Mathf.Lerp(1f, StretchY, fade);
            xz = Mathf.Lerp(1f, StretchXZ, fade);
        }

        /// <summary>Tight at the kick, wider as the scuff dies.</summary>
        public static float PuffDiameter(float fade) => Mathf.Lerp(PuffWide, PuffTight, fade);

        /// <summary>
        /// World offset along the wall normal, away from the wall.
        /// wallNormal points out of the wall, toward the pawn.
        /// A zero or vertical normal returns zero so the pose stays put.
        /// </summary>
        public static Vector3 PushWorld(Vector3 wallNormal, float fade)
        {
            Vector3 n = wallNormal;
            n.y = 0f;
            if (n.sqrMagnitude < 1e-6f || fade <= 0f)
                return Vector3.zero;
            n.Normalize();
            return n * (PushOffset * fade);
        }

        /// <summary>
        /// Foot scuffs on the wall. Left and right are across the face, at the shoes.
        /// wallNormal points out of the wall, toward the pawn.
        /// A zero or vertical normal returns false.
        /// </summary>
        public static bool Feet(Vector3 origin, Vector3 wallNormal, out Vector3 left, out Vector3 right)
        {
            Vector3 n = wallNormal;
            n.y = 0f;
            if (n.sqrMagnitude < 1e-6f)
            {
                left = origin + Vector3.up * FootHeight;
                right = left;
                return false;
            }

            n.Normalize();
            Vector3 into = origin - n * SurfaceOffset;
            Vector3 side = FlatSide(n);
            Vector3 contact = into + Vector3.up * FootHeight;
            left = contact - side * FootSpread;
            right = contact + side * FootSpread;
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
