using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Contact read when a punch transfers It. Numbers only.
    /// TagLandFlash draws them on whichever pawn was tagged.
    /// No motor write and no camera punch. The chirp stays TagSfx.BecomeIt
    /// inside ItController.SetIt(true), which is also the ItMarker pop.
    /// </summary>
    public static class TagLandTell
    {
        public const float FlashSeconds = 0.36f;
        public const float LabelSeconds = 0.80f;
        public const int RingCount = 2;
        public const float InnerRadius = 1.45f;
        public const float OuterRadius = 2.40f;
        public const float InnerHeight = 0.10f;
        public const float OuterHeight = 0.18f;
        public const float LabelHeight = 2.45f;
        /// <summary>Meters toward the chase camera, so the words sit off the chest.</summary>
        public const float LabelForward = 0.55f;
        public const string VictimLine = "TAGGED";
        public const string ItLine = "YOU'RE IT";
        public const string Cue = "BecomeIt";
        public const float VerticalImpulse = 0f;
        public const string MarkerName = "TagLandFlash";

        public static bool Transferred(bool puncherWasIt, bool victimNowIt, bool puncherStillIt)
        {
            return puncherWasIt && victimNowIt && !puncherStillIt;
        }

        /// <summary>Unity cylinder radius is 0.5, so the XZ scale is twice the world radius.</summary>
        public static float RingScale(float radius) => radius * 2f;

        public static Vector3 RingCenter(Vector3 feet, float height) => feet + Vector3.up * height;

        /// <summary>
        /// Words above the tagged head, shifted toward the camera.
        /// A zero direction falls back to the chase side of a pawn facing +Z.
        /// </summary>
        public static Vector3 LabelPoint(Vector3 feet, Vector3 towardCamera)
        {
            Vector3 flat = towardCamera;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-6f)
                flat = new Vector3(0f, 0f, -1f);
            else
                flat.Normalize();
            return feet + Vector3.up * LabelHeight + flat * LabelForward;
        }

        /// <summary>Full angular width of a ring of this radius, in degrees, at a chase-cam distance.</summary>
        public static float AngularDiameterDeg(float radius, float distance)
        {
            if (distance <= 0.01f) return 180f;
            return 2f * Mathf.Atan(radius / distance) * Mathf.Rad2Deg;
        }
    }
}
