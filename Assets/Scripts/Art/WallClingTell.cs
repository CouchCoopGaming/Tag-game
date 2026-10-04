using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Wall-face hand marks while MoveState is WallClimb or WallRun.
    /// Presentation only. Two contact patches and short dust ribbons sit on the wall
    /// beside the capsule, low enough in opacity that the body stays readable.
    /// A climb sets one hand higher. A wall run keeps the pair nearly level.
    /// Does not write velocity, friction, or the camera.
    /// </summary>
    public static class WallClingTell
    {
        public const int MarkCount = 2;
        /// <summary>Trail lifetime. Short so a wall run does not paint a speed line.</summary>
        public const float RibbonTime = 0.15f;
        /// <summary>View-aligned width. Readable at chase-cam distance, narrow enough to leave the arms clear.</summary>
        public const float RibbonWidth = 0.16f;
        /// <summary>Meters from the pawn origin toward the wall. On the capsule skin, where the face meets the pawn.</summary>
        public const float SurfaceOffset = 0.37f;
        /// <summary>Lateral split from the contact line. Inside the capsule width.</summary>
        public const float HandSpread = 0.18f;
        /// <summary>Hand height while climbing. Under the head, above the hips.</summary>
        public const float ClimbHeight = 1.22f;
        /// <summary>Hand height while wall-running. Chest height, still under the head.</summary>
        public const float RunHeight = 0.98f;
        /// <summary>Lead hand sits this much higher so a climb reads as a grab.</summary>
        public const float ClimbStagger = 0.16f;
        /// <summary>A wall run stays nearly level. The small offset keeps the two marks from stacking.</summary>
        public const float RunStagger = 0.02f;
        /// <summary>World size of each contact patch.</summary>
        public const float MarkSize = 0.20f;
        /// <summary>Peak opacity. Low enough that the pawn stays visible through the dust.</summary>
        public const float MaxAlpha = 0.42f;
        public const float DustR = 0.74f;
        public const float DustG = 0.70f;
        public const float DustB = 0.60f;
        /// <summary>No glow. A chase camera must not be washed out by the contact.</summary>
        public const float Glow = 0f;
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "WallClingTell";

        /// <summary>Outer span of the ribbon pair, including width. Stays inside the capsule footprint.</summary>
        public static float PairSpan => HandSpread * 2f + RibbonWidth;

        /// <summary>Outer span of the contact patches. Stays inside the capsule footprint.</summary>
        public static float MarkSpan => HandSpread * 2f + MarkSize;

        public static float HandHeight(bool climbing) => climbing ? ClimbHeight : RunHeight;

        public static float HandStagger(bool climbing) => climbing ? ClimbStagger : RunStagger;

        /// <summary>
        /// Two hand contacts on the wall. wallNormal points out of the wall, toward the pawn.
        /// Climbing raises the lead hand. A zero or vertical normal returns false.
        /// </summary>
        public static bool Place(Vector3 origin, Vector3 wallNormal, bool climbing, out Vector3 left, out Vector3 right)
        {
            Vector3 n = wallNormal;
            n.y = 0f;
            if (n.sqrMagnitude < 1e-6f)
            {
                left = origin + Vector3.up * HandHeight(climbing);
                right = left;
                return false;
            }

            n.Normalize();
            Vector3 into = origin - n * SurfaceOffset;
            Vector3 side = FlatSide(n);
            float height = HandHeight(climbing);
            float stagger = HandStagger(climbing);
            Vector3 contact = into + Vector3.up * height;
            // -n is toward the wall. side is across the face.
            left = contact - side * HandSpread + Vector3.up * stagger;
            right = contact + side * HandSpread;
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
