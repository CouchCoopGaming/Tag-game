using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Third-person read for an air dash. Presentation only: no speed, cooldown, or jump numbers.
    /// Side ribbons sit outside the capsule. The ankle streak stays under the feet.
    /// The chase camera widens and leads; the boom does not come in.
    /// </summary>
    public static class AirDashTell
    {
        public const float WingOffset = 0.55f;
        public const float WingHeight = 0.62f;
        public const float AnkleHeight = 0.10f;
        public const float RibbonTime = 0.42f;
        public const float WingWidth = 0.18f;
        public const float AnkleWidth = 0.14f;
        /// <summary>Fraction of the shell replaced by the cyan sheen. Below half so the pawn stays itself.</summary>
        public const float FlashMix = 0.42f;
        public const float FlashSeconds = 0.24f;
        public const float CameraFovPop = 7f;
        public const float CameraAheadPop = 1.05f;
        /// <summary>Matches the existing chase boom stretch per metre of look-ahead. Always outward.</summary>
        public const float CameraBoomPerAhead = 0.35f;

        public static bool Place(Vector3 origin, Vector3 dashDir, out Vector3 left, out Vector3 right, out Vector3 ankle)
        {
            Vector3 dir = dashDir;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f)
            {
                left = origin + Vector3.up * WingHeight;
                right = left;
                ankle = origin + Vector3.up * AnkleHeight;
                return false;
            }

            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-6f)
                side = Vector3.right;
            else
                side.Normalize();

            left = origin + Vector3.up * WingHeight - side * WingOffset;
            right = origin + Vector3.up * WingHeight + side * WingOffset;
            ankle = origin + Vector3.up * AnkleHeight;
            return true;
        }

        public static float LateralGap(Vector3 origin, Vector3 wing)
        {
            Vector3 flat = wing - origin;
            flat.y = 0f;
            return flat.magnitude;
        }

        /// <summary>Extra boom distance. Never negative, so the lens does not crop the pawn.</summary>
        public static float BoomExtra(float ahead) => Mathf.Max(0f, ahead) * CameraBoomPerAhead;

        public static float WingInnerEdge => WingOffset - WingWidth * 0.5f;
    }
}
