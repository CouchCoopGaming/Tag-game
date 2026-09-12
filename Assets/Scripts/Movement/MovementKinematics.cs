using UnityEngine;

namespace Tag.Movement
{
    /// <summary>
    /// Pure helpers for party movement numbers. Used by PlayerMotor and EditMode tests.
    /// No scene / CharacterController dependency — keeps tunables testable.
    /// </summary>
    public static class MovementKinematics
    {
        public static float EffectiveAirDodgeSpeed(float speed, float lockTime, float maxDistance)
        {
            if (maxDistance > 0f && lockTime > 0.0001f)
            {
                float maxSpeed = maxDistance / lockTime;
                if (speed > maxSpeed)
                    return maxSpeed;
            }
            return Mathf.Max(0f, speed);
        }

        public static float EffectiveAirDashDistance(float speed, float lockTime, float maxDistance)
        {
            float s = EffectiveAirDodgeSpeed(speed, lockTime, maxDistance);
            return s * Mathf.Max(0f, lockTime);
        }

        public static float EffectiveAirDashDistance(MovementTuning t)
        {
            if (t == null) return 0f;
            return EffectiveAirDashDistance(t.airDodgeSpeed, t.airDodgeLock, t.airDodgeMaxDistance);
        }

        public static float SlideEnterSpeed(float currentHoriz, float peak, float gate, bool boostToPeak, bool wipeToGate)
        {
            if (boostToPeak)
                return Mathf.Max(currentHoriz, peak);
            if (wipeToGate)
                return gate;
            return Mathf.Max(currentHoriz, gate);
        }

        public static float JumpLaunchSpeed(float gravity, float apex, float authoredLaunch, float rewriteEpsilon = 0.5f)
        {
            float g = Mathf.Max(0.01f, gravity);
            float h = Mathf.Max(0f, apex);
            float derived = Mathf.Sqrt(2f * g * h);
            return Mathf.Abs(authoredLaunch - derived) > rewriteEpsilon ? derived : authoredLaunch;
        }

        public static bool WantsSprint(bool autoSprint, float autoThreshold, float moveMagnitude, bool sprintHeld)
        {
            if (moveMagnitude < 0.01f) return false;
            if (autoSprint && moveMagnitude >= autoThreshold) return true;
            return sprintHeld;
        }

        public static bool CanStartSlide(bool grounded, bool alreadySliding, float cooldown, float horizSpeed, float gate, bool crouchEdge, bool crouchHeld, bool slideFromSpeed)
        {
            if (!grounded || alreadySliding || cooldown > 0f) return false;
            if (horizSpeed < gate) return false;
            if (crouchEdge) return true;
            return slideFromSpeed && crouchHeld;
        }

        /// <summary>Angle between surface normal and world up (0 = flat).</summary>
        public static float SlopeAngle(Vector3 normal)
        {
            return Vector3.Angle(normal, Vector3.up);
        }

        public static bool IsWalkableSlope(Vector3 normal, float slopeLimitDeg)
        {
            return SlopeAngle(normal) <= slopeLimitDeg + 0.01f;
        }

        /// <summary>
        /// Project planar wish onto the slope so path speed stays constant
        /// (Dave / CC slope tutorials — downhill is not faster than sprint).
        /// </summary>
        public static Vector3 ProjectWishOnSlope(Vector3 wish, Vector3 slopeNormal)
        {
            if (wish.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 proj = Vector3.ProjectOnPlane(wish, slopeNormal);
            if (proj.sqrMagnitude < 0.0001f) return Vector3.zero;
            return proj.normalized * wish.magnitude;
        }

        public static Vector3 SteepSlopeSlideVelocity(Vector3 slopeNormal, float slideSpeed)
        {
            Vector3 down = Vector3.ProjectOnPlane(Vector3.down, slopeNormal);
            if (down.sqrMagnitude < 0.0001f) return Vector3.zero;
            return down.normalized * Mathf.Max(0f, slideSpeed);
        }

        public static Vector3 ClampAlongDirection(Vector3 velocity, float maxSpeed)
        {
            float cap = Mathf.Max(0.01f, maxSpeed);
            if (velocity.sqrMagnitude > cap * cap)
                return velocity.normalized * cap;
            return velocity;
        }
    }
}
