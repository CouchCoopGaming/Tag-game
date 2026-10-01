using System;

namespace UnityEngine
{
    public class Transform
    {
        public Vector3 forward;
        public Vector3 right;
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero => new Vector2(0f, 0f);

        public float sqrMagnitude => x * x + y * y;

        public void Normalize()
        {
            float mag = (float)Math.Sqrt(x * x + y * y);
            if (mag > 1e-5f)
            {
                x /= mag;
                y /= mag;
            }
            else
            {
                x = 0f;
                y = 0f;
            }
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 up => new Vector3(0f, 1f, 0f);
        public static Vector3 forward => new Vector3(0f, 0f, 1f);
        public static Vector3 right => new Vector3(1f, 0f, 0f);

        public float sqrMagnitude => x * x + y * y + z * z;

        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);

        public Vector3 normalized
        {
            get
            {
                Vector3 copy = this;
                copy.Normalize();
                return copy;
            }
        }

        public void Normalize()
        {
            float mag = magnitude;
            if (mag > 1e-5f)
            {
                x /= mag;
                y /= mag;
                z /= mag;
            }
            else
            {
                x = 0f;
                y = 0f;
                z = 0f;
            }
        }

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
        {
            float sqr = Dot(planeNormal, planeNormal);
            if (sqr < 1e-10f) return zero;
            return vector - planeNormal * (Dot(vector, planeNormal) / sqr);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
    }

    public static class Mathf
    {
        public const float Deg2Rad = 0.0174532924f;

        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Abs(float v) => v < 0f ? -v : v;
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Sin(float rad) => (float)Math.Sin(rad);
        public static float Cos(float rad) => (float)Math.Cos(rad);
    }
}
