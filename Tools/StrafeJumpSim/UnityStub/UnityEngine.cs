using System;
using System.Collections.Generic;

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
        public const float Rad2Deg = 57.29578f;

        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static float Abs(float v) => v < 0f ? -v : v;
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Sin(float rad) => (float)Math.Sin(rad);
        public static float Cos(float rad) => (float)Math.Cos(rad);
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        public static float Atan(float v) => (float)Math.Atan(v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = t * t * (3f - 2f * t);
            return from + (to - from) * t;
        }
    }

    public struct Quaternion
    {
        public float x, y, z, w;

        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Quaternion identity => new Quaternion(0f, 0f, 0f, 1f);

        public static Quaternion Euler(float x, float y, float z) => identity;

        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
    }

    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int v) => new LayerMask { value = v };
    }

    public static class Random
    {
        static int _n;

        public static int Range(int min, int max)
        {
            if (max <= min) return min;
            int span = max - min;
            int v = min + (_n % span);
            _n++;
            return v;
        }
    }

    public static class PlayerPrefs
    {
        static readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        static readonly Dictionary<string, string> Strings = new Dictionary<string, string>();

        public static void SetInt(string key, int value)
        {
            Ints[key] = value;
        }

        public static int GetInt(string key, int fallback)
        {
            int v;
            return Ints.TryGetValue(key, out v) ? v : fallback;
        }

        public static void SetString(string key, string value)
        {
            Strings[key] = value;
        }

        public static string GetString(string key, string fallback)
        {
            string v;
            return Strings.TryGetValue(key, out v) ? v : fallback;
        }

        public static bool HasKey(string key)
        {
            return Ints.ContainsKey(key) || Strings.ContainsKey(key);
        }

        public static void Save()
        {
        }
    }

    public class Object
    {
        public string name;
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName { get; set; }
        public string fileName { get; set; }
        public int order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) { }
    }
}
