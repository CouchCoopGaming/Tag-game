using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Chase-cam read while the campus opponent is It and closing on a runner.
    /// Two short heel ribbons sit on the ground behind the pawn. A small chevron
    /// sits on the chest and points at the target. Alpha stays low so the body shows through.
    /// Presentation only. No light, no collider, no camera pop. LungeTell still owns the lunge.
    /// The solo pawn does not get this streak.
    /// </summary>
    public static class OpponentChaseTell
    {
        public const int RibbonCount = 2;
        /// <summary>Trail lifetime. Short so a sprint does not paint a speed line.</summary>
        public const float RibbonTime = 0.16f;
        /// <summary>View-aligned width. Readable at chase-cam distance, narrow enough to leave the legs clear.</summary>
        public const float RibbonWidth = 0.17f;
        /// <summary>Meters above the pawn origin. On the ground, under the ankle.</summary>
        public const float RibbonHeight = 0.05f;
        /// <summary>Lateral split from the center line. Inside the capsule, at the heels.</summary>
        public const float HeelOffset = 0.15f;
        /// <summary>Meters behind the origin along travel, so the streak starts at the heels.</summary>
        public const float TrailBack = 0.22f;
        /// <summary>Peak opacity. The pawn stays visible through the streak and the chevron.</summary>
        public const float MaxAlpha = 0.40f;
        public const float MarkR = 0.86f;
        public const float MarkG = 0.38f;
        public const float MarkB = 0.12f;
        /// <summary>No glow. A chase camera must not bloom on the hunter.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        /// <summary>Chest height. Under the head, above the hips.</summary>
        public const float ChevronHeight = 1.12f;
        /// <summary>World size of the heading mark.</summary>
        public const float ChevronSize = 0.20f;
        /// <summary>Meters from the origin toward the target, on the chest skin.</summary>
        public const float ChevronForward = 0.48f;
        /// <summary>Inside this distance the fist and the lunge ring own the read.</summary>
        public const float MinDistance = 2.0f;
        /// <summary>Beyond this the hunter is not yet in the chase band.</summary>
        public const float MaxDistance = 18f;
        /// <summary>Planar speed below this is a stand, not a chase.</summary>
        public const float MinSpeed = 1.0f;
        /// <summary>Velocity must face the target at least this much. 1 is straight at them.</summary>
        public const float CloseDot = 0.35f;
        public const string MarkerName = "OpponentChaseTell";
        /// <summary>Scene pawn that hunts. The solo human is a different name and stays clean.</summary>
        public const string OpponentPawnName = "DummyRunner";

        /// <summary>Outer span of the heel pair, including ribbon width. Stays inside the capsule footprint.</summary>
        public static float PairSpan => HeelOffset * 2f + RibbonWidth;

        public static bool IsOpponentPawn(string pawnName) => pawnName == OpponentPawnName;

        public static bool InBand(float planarDistance)
        {
            return planarDistance >= MinDistance && planarDistance <= MaxDistance;
        }

        /// <summary>
        /// True only for the opponent, while It, grounded, and closing on a live runner in the band.
        /// Stops for the solo pawn, flee, a lost target, air, the lunge ring, the lunge itself,
        /// punch Active, an air-dash streak, and the slide scrape.
        /// </summary>
        public static bool Show(
            bool opponentPawn,
            bool isIt,
            bool grounded,
            bool targetLiveNonIt,
            float planarDistance,
            bool closing,
            bool lungeTell,
            bool lungeActive,
            bool punchActive,
            bool airDashTell,
            bool slideScrape)
        {
            if (!opponentPawn || !isIt || !grounded)
                return false;
            if (!targetLiveNonIt)
                return false;
            if (lungeTell || lungeActive || punchActive || airDashTell || slideScrape)
                return false;
            if (!closing)
                return false;
            return InBand(planarDistance);
        }

        /// <summary>
        /// True when planar velocity is fast enough and aimed at the target.
        /// A zero or vertical velocity is not a chase.
        /// </summary>
        public static bool Closing(Vector3 from, Vector3 target, Vector3 velocity)
        {
            Vector3 to = target - from;
            to.y = 0f;
            Vector3 v = velocity;
            v.y = 0f;
            float minSq = MinSpeed * MinSpeed;
            if (to.sqrMagnitude < 1e-6f || v.sqrMagnitude < minSq)
                return false;
            to.Normalize();
            v.Normalize();
            return Vector3.Dot(v, to) >= CloseDot;
        }

        /// <summary>
        /// Heel ribbons behind travel. Left and right are across the heading.
        /// A zero direction returns false so the caller can skip the frame.
        /// </summary>
        public static bool PlaceHeels(Vector3 origin, Vector3 travelDir, out Vector3 left, out Vector3 right)
        {
            Vector3 dir = travelDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
            {
                left = origin + Vector3.up * RibbonHeight;
                right = left;
                return false;
            }

            dir.Normalize();
            Vector3 side = FlatSide(dir);
            Vector3 heel = origin - dir * TrailBack + Vector3.up * RibbonHeight;
            left = heel - side * HeelOffset;
            right = heel + side * HeelOffset;
            return true;
        }

        /// <summary>
        /// Chest chevron. aimDir is the flat heading toward the target.
        /// A zero or vertical aim returns false.
        /// </summary>
        public static bool PlaceChevron(Vector3 origin, Vector3 aimDir, out Vector3 chest, out Vector3 aim)
        {
            Vector3 dir = aimDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
            {
                chest = origin + Vector3.up * ChevronHeight;
                aim = Vector3.forward;
                return false;
            }

            dir.Normalize();
            aim = dir;
            chest = origin + Vector3.up * ChevronHeight + dir * ChevronForward;
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
    }
}
