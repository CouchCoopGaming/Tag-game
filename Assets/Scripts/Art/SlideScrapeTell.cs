using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Ground scrape while MoveState is Slide. Presentation only.
    /// Two short dust ribbons sit at the heels, low enough that the pawn stays readable.
    /// Lifetime is short so the streak does not read as extra speed.
    /// Does not write velocity, friction, or the camera.
    /// </summary>
    public static class SlideScrapeTell
    {
        public const int RibbonCount = 2;
        /// <summary>Trail lifetime. Short so a fast slide does not paint a speed line.</summary>
        public const float RibbonTime = 0.14f;
        /// <summary>View-aligned width. Thick enough to read at chase-cam distance, narrow enough to leave the legs clear.</summary>
        public const float RibbonWidth = 0.18f;
        /// <summary>Meters above the pawn origin. Under the ankle, on the ground.</summary>
        public const float RibbonHeight = 0.06f;
        /// <summary>Lateral split from the center line. Inside the capsule, at the feet.</summary>
        public const float FootOffset = 0.16f;
        /// <summary>Meters behind the origin along travel, so dust starts at the heels.</summary>
        public const float TrailBack = 0.20f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "SlideScrapeTell";

        /// <summary>Outer span of the pair, including ribbon width. Stays inside the capsule footprint.</summary>
        public static float PairSpan => FootOffset * 2f + RibbonWidth;

        /// <summary>
        /// Heel ribbons for a slide. Left and right are across travel, behind the origin.
        /// A zero direction returns false so the caller can skip the frame.
        /// </summary>
        public static bool Place(Vector3 origin, Vector3 slideDir, out Vector3 left, out Vector3 right)
        {
            Vector3 dir = slideDir;
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
    }
}
