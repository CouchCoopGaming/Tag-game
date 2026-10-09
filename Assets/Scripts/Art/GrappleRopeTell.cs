using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Where the experimental rope is drawn. Presentation only.
    /// The motor still strips outward horizontal speed. This type does not
    /// write velocity and does not add a vertical impulse.
    /// </summary>
    public static class GrappleRopeTell
    {
        // Hands on the long two-arm reach. Ahead of the capsule, below the head.
        public const float HandHeight = 1.28f;
        public const float HandForward = 0.62f;
        /// <summary>Left of the facing. Negative is the grappling hand.</summary>
        public const float HandSide = -0.34f;

        // The old line was 0.06 / 0.03 and disappeared at chase-cam distance.
        public const float RopeStartWidth = 0.16f;
        public const float RopeEndWidth = 0.09f;
        /// <summary>Core line width. The readable start width, not the old hairline.</summary>
        public const float RopeDiameter = RopeStartWidth;
        public const float HaloExtra = 0.14f;
        /// <summary>Mid-span drop once the line has slack. A taut pull stays at 0.</summary>
        public const float HoldSag = 0.06f;
        public const float AimLength = 2.4f;
        public const float AimWidth = 0.10f;
        public const float HookMarkerSize = 0.32f;
        public const float VerticalImpulse = 0f;

        public static float HaloWidth => RopeStartWidth + HaloExtra;

        /// <summary>0 while the rope is taut. A hold eases up to <see cref="HoldSag"/>. Presentation only.</summary>
        public static float CordSag(float slack)
        {
            if (slack < 0f) slack = 0f;
            if (slack > 1f) slack = 1f;
            if (slack <= 0.08f) return 0f;
            return HoldSag * (slack - 0.08f) / 0.92f;
        }

        public static Vector3 Hand(Vector3 origin, Vector3 forward)
        {
            Vector3 flat = forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-6f)
                flat = Vector3.forward;
            else
                flat.Normalize();
            Vector3 right = new Vector3(flat.z, 0f, -flat.x);
            return origin + Vector3.up * HandHeight + flat * HandForward + right * HandSide;
        }

        /// <summary>Latched rope. End is the hit, not a point invented along the ray.</summary>
        public static bool AttachedSpan(Vector3 origin, Vector3 forward, Vector3 anchor, out Vector3 hand, out Vector3 end)
        {
            hand = Hand(origin, forward);
            end = anchor;
            return (end - hand).sqrMagnitude > 0.04f;
        }

        /// <summary>Short aim cue while the hook is held and nothing is latched. Not a rope.</summary>
        public static bool AimSpan(Vector3 origin, Vector3 forward, Vector3 aimDir, out Vector3 hand, out Vector3 tip)
        {
            hand = Hand(origin, forward);
            if (aimDir.sqrMagnitude < 1e-6f)
            {
                tip = hand;
                return false;
            }

            Vector3 dir = aimDir.normalized;
            tip = hand + dir * AimLength;
            return true;
        }
    }
}
