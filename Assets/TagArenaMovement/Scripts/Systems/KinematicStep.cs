using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Shared kinematic steps. Air speed is capped along the wish, not on the whole vector.
    /// Ground speed above the gait cap bleeds unless this frame jumps (bunny hop).
    /// </summary>
    public static class KinematicStep
    {
        public static float GaitCap(bool crouch, bool sprintHeld, float moveY, float crouchSpeed, float sprintSpeed, float walkSpeed)
        {
            if (crouch) return crouchSpeed;
            if (sprintHeld || moveY > 0.4f) return sprintSpeed;
            return walkSpeed;
        }

        /// <summary>
        /// Quake air accelerate. <paramref name="wishSpeed"/> limits the component along the wish.
        /// Turning the wish off the current velocity can raise total horizontal speed.
        /// <paramref name="accelPerSecond"/> is meters per second squared; it is multiplied by dt.
        /// </summary>
        public static Vector3 AirSteer(Vector3 hv, Vector3 wish, float wishSpeed, float accelPerSecond, float dt)
        {
            if (wish.sqrMagnitude <= 0.01f || wishSpeed <= 0f) return hv;
            float accel = accelPerSecond / Mathf.Max(wishSpeed, 1f);
            return WishAccel.Accelerate(hv, wish, wishSpeed, accel, dt);
        }

        /// <summary>
        /// Ground accelerate toward the gait cap. Speed already above that cap bleeds by
        /// <paramref name="groundDecel"/> unless <paramref name="hopSkipsFriction"/> (jump this frame).
        /// Under the cap this is the previous ground accelerate, including its angle add.
        /// </summary>
        public static Vector3 GroundSteer(Vector3 hv, Vector3 wish, float maxSpeed, float groundAccel, float groundDecel, float dt, bool hopSkipsFriction)
        {
            if (wish.sqrMagnitude > 0.01f)
            {
                bool over = hv.magnitude > maxSpeed;
                if (over && !hopSkipsFriction)
                    hv = WishAccel.Friction(hv, groundDecel / Mathf.Max(hv.magnitude, 1f), dt);
                float preserved = hv.magnitude;
                hv = WishAccel.Accelerate(hv, wish, maxSpeed, groundAccel / Mathf.Max(maxSpeed, 1f), dt);
                if (over && preserved > 0.0001f && hv.magnitude > preserved)
                    hv *= preserved / hv.magnitude;
                return hv;
            }

            return WishAccel.Friction(hv, groundDecel / Mathf.Max(hv.magnitude, 1f), dt);
        }

        public static float AirGravity(float vy, float gravity, float fallGravityMult)
        {
            return gravity * (vy < 0f ? fallGravityMult : 1f);
        }

        public static float IntegrateVertical(float vy, float gravityThisFrame, float dt, float fallCap)
        {
            vy -= gravityThisFrame * dt;
            if (vy < -fallCap) vy = -fallCap;
            return vy;
        }

        /// <summary>TickTimers order: decay before the jump check. A grounded frame refreshes after this.</summary>
        public static float DecayCoyote(float coyote, float dt)
        {
            if (coyote > 0f) coyote -= dt;
            return coyote;
        }

        public static bool CoyoteJumpAllowed(bool grounded, float coyote)
        {
            return grounded || coyote > 0f;
        }

        /// <summary>ClampAndDrag planar cap. Ski, jet, and slide use the full ski max.</summary>
        public static float LocomotionPlanarCap(float skiMaxSpeed, bool skiJetOrSlide)
        {
            return skiJetOrSlide ? skiMaxSpeed : skiMaxSpeed * 0.7f;
        }
    }
}
