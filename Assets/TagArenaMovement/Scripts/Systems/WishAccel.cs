using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Quake-style acceleration. This is the tap-strafe / air-lurch primitive.
    /// Adding speed only along wishdir (not overwriting velocity) is what makes
    /// Apex mid-air redirects and Tribes edging feel skillful instead of sticky.
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
            if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
            f.Normalize(); r.Normalize();
            Vector3 w = f * move.y + r * move.x;
            if (w.sqrMagnitude > 1f) w.Normalize();
            return w;
        }
    }
}
