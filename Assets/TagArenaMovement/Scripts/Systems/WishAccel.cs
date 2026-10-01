using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Quake-style acceleration along wishdir, up to a wish speed chosen by the caller.
    /// The cap is the component along the wish, not the length of the velocity.
    /// An off-axis wish can therefore lengthen the vector. That is the strafe-jump.
    /// <see cref="SteerNoGain"/> strips that extra. Air steer must not call it.
    /// </summary>
    public static class WishAccel
    {
        public static Vector3 Accelerate(Vector3 velocity, Vector3 wishDir, float wishSpeed, float accel, float dt)
        {
            if (wishDir.sqrMagnitude < 0.0001f) return velocity;
            wishDir.Normalize();
            float current = Vector3.Dot(velocity, wishDir);
            float add = wishSpeed - current;
            if (add <= 0f) return velocity;
            float accelSpeed = Mathf.Min(accel * dt * wishSpeed, add);
            return velocity + wishDir * accelSpeed;
        }

        /// <summary>
        /// Redirect velocity toward wishDir without raising its magnitude.
        /// Accelerate still adds length when the wish is off-axis; that extra is removed.
        /// </summary>
        public static Vector3 SteerNoGain(Vector3 velocity, Vector3 wishDir, float accel, float dt)
        {
            float speed = velocity.magnitude;
            if (speed < 0.0001f || wishDir.sqrMagnitude < 0.0001f) return velocity;
            Vector3 steered = Accelerate(velocity, wishDir, speed, accel / Mathf.Max(speed, 1f), dt);
            float mag = steered.magnitude;
            if (mag > speed)
                steered *= speed / mag;
            return steered;
        }

        public static Vector3 ClampPlanarSpeed(Vector3 velocity, float maxSpeed)
        {
            if (maxSpeed < 0f) maxSpeed = 0f;
            float speed = velocity.magnitude;
            if (speed > maxSpeed && speed > 0.0001f)
                return velocity * (maxSpeed / speed);
            return velocity;
        }

        public static Vector3 Friction(Vector3 velocity, float amount, float dt)
        {
            float speed = velocity.magnitude;
            if (speed < 0.05f) return Vector3.zero;
            float drop = speed * amount * dt;
            float ns = Mathf.Max(speed - drop, 0f);
            return velocity * (ns / speed);
        }

        public static Vector3 Horizontal(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public static float HorizSpeed(Vector3 v) => new Vector3(v.x, 0f, v.z).magnitude;

        public static Vector3 SetHoriz(Vector3 v, Vector3 horiz)
        {
            horiz.y = 0f;
            return new Vector3(horiz.x, v.y, horiz.z);
        }

        public static Vector3 CameraWish(Transform cam, Vector2 move)
        {
            Vector3 f = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            Vector3 r = Vector3.ProjectOnPlane(cam.right, Vector3.up);
            return PlanarWish(f, r, move);
        }

        /// <summary>
        /// Camera-space stick on the ground plane. A diagonal longer than 1 is normalized.
        /// Shorter input keeps its direction. Air speed is capped on that direction, not by this length.
        /// </summary>
        public static Vector3 PlanarWish(Vector3 forward, Vector3 right, Vector2 move)
        {
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            right.Normalize();
            Vector3 w = forward * move.y + right * move.x;
            if (w.sqrMagnitude > 1f) w.Normalize();
            return w;
        }
    }
}
