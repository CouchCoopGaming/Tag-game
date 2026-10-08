using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Hard-land and wall-slam shockwave. A flat ring on the surface plus a
    /// short debris puff. Size follows impact speed and the dust surface.
    /// Visual only. The land and wall FX toggles hide it. No new settings row.
    /// </summary>
    [DefaultExecutionOrder(140)]
    public sealed class ImpactFx : MonoBehaviour
    {
        public const int BitsMax = 12;
        public const float SpeedCap = 2.8f;
        const int Slots = 4;
        const float RingBand = 0.86f;

        public struct Spec
        {
            public float Radius;
            public float Debris;
            public int Bits;
            public float Life;
            public float Opacity;
            public float R;
            public float G;
            public float B;
            public float OutSpeed;
            public float UpSpeed;
        }

        /// <summary>
        /// Radius and debris grow with speed. Colour, base size, and count
        /// come from the same surface table as the foot dust.
        /// </summary>
        public static Spec Measure(int surface, float speed)
        {
            if (speed < 0f) speed = 0f;
            DustLook.Puff dust = DustLook.At(surface, speed, (int)DustLook.Kick.None);
            float u = DustLook.Sprint > 0.01f ? speed / DustLook.Sprint : 0f;
            if (u < 0f) u = 0f;
            if (u > SpeedCap) u = SpeedCap;
            var spec = new Spec();
            spec.Radius = 0.35f + u * 0.45f;
            spec.Debris = dust.Size;
            if (spec.Debris < 0.04f) spec.Debris = 0.04f;
            spec.Debris *= 0.65f + u * 0.55f;
            int bits = dust.Count;
            if (bits < 3) bits = 3;
            bits += (int)(u * 3f);
            if (bits > BitsMax) bits = BitsMax;
            spec.Bits = bits;
            float lifeU = u > 2f ? 2f : u;
            spec.Life = 0.20f + lifeU * 0.04f;
            spec.Opacity = dust.Opacity;
            if (spec.Opacity < 0.35f) spec.Opacity = 0.35f;
            if (spec.Opacity > 0.90f) spec.Opacity = 0.90f;
            spec.R = dust.R;
            spec.G = dust.G;
            spec.B = dust.B;
            spec.OutSpeed = 1.1f + u * 0.85f;
            spec.UpSpeed = 1.4f + u * 0.55f;
            return spec;
        }

        /// <summary>0 at the pop, 1 when the ring has reached its radius.</summary>
        public static float Grow(float age, float life)
        {
            if (life < 0.0001f) life = 0.0001f;
            float u = age / life;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return 0.28f + 0.92f * u;
        }

        static ImpactFx _host;

        Transform[] _root;
        Transform[] _ring;
        Renderer[] _ringRend;
        Material[] _ringMat;
        Transform[] _bit;
        Renderer[] _bitRend;
        Material[] _bitMat;
        Vector3[] _bitVel;
        float[] _age;
        float[] _life;
        float[] _radius;
        float[] _debris;
        int[] _bits;
        int[] _kind;
        Vector3[] _normal;
        Color[] _color;
        float[] _opacity;
        Texture2D _ringTex;
        Mesh _quad;

        public static void Land(Vector3 origin, float speed, int surface)
        {
            if (!FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Land)) return;
            Raise(origin, Vector3.up, speed, surface, FxKitOptions.Land);
        }

        public static void Wall(Vector3 origin, Vector3 normal, float speed, int surface)
        {
            if (!FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Wall)) return;
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
            else normal.Normalize();
            Raise(origin, normal, speed, surface, FxKitOptions.Wall);
        }

        static void Raise(Vector3 origin, Vector3 normal, float speed, int surface, int kind)
        {
            Ensure();
            if (_host == null || _host._age == null) return;
            _host.Spawn(origin, normal, Measure(surface, speed), kind);
        }

        public static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("ImpactFx");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<ImpactFx>();
        }

        void Awake()
        {
            _host = this;
            _quad = QuadXZ();
            Mesh sprite = QuadXY();
            _ringTex = RingTex();
            Shader decal = Shader.Find("Tag/FxDecal");
            if (decal == null) decal = Shader.Find("Sprites/Default");
            Shader sprite = Shader.Find("Tag/FxKitSprite");
            if (sprite == null) sprite = decal;
            _root = new Transform[Slots];
            _ring = new Transform[Slots];
            _ringRend = new Renderer[Slots];
            _ringMat = new Material[Slots];
            _bit = new Transform[Slots * BitsMax];
            _bitRend = new Renderer[Slots * BitsMax];
            _bitMat = new Material[Slots * BitsMax];
            _bitVel = new Vector3[Slots * BitsMax];
            _age = new float[Slots];
            _life = new float[Slots];
            _radius = new float[Slots];
            _debris = new float[Slots];
            _bits = new int[Slots];
            _kind = new int[Slots];
            _normal = new Vector3[Slots];
            _color = new Color[Slots];
            _opacity = new float[Slots];
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var root = new GameObject("Impact");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                _root[i] = root.transform;
                _ringMat[i] = new Material(decal);
                _ringMat[i].mainTexture = _ringTex;
                if (_ringMat[i].HasProperty("_MainTex")) _ringMat[i].SetTexture("_MainTex", _ringTex);
                _ring[i] = Child(root.transform, "Ring", _quad, _ringMat[i], out _ringRend[i]);
                for (int b = 0; b < BitsMax; b++)
                {
                    int k = i * BitsMax + b;
                    _bitMat[k] = new Material(sprite);
                    _bitMat[k].SetFloat("_Shape", 0f);
                    _bitMat[k].SetFloat("_Billboard", 1f);
                    _bitMat[k].SetFloat("_Guard", 0f);
                    _bitMat[k].SetFloat("_Edge", -1f);
                    _bit[k] = Child(root.transform, "Bit", sprite, _bitMat[k], out _bitRend[k]);
                    _bitRend[k].enabled = false;
                }
            }
        }

        void LateUpdate()
        {
            if (_age == null) return;
            bool show = FxKitLook.Master(GameSettings.Current);
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) continue;
                if (!show || !FxKitLook.Bursts(GameSettings.Current, _kind[i]))
                {
                    Hide(i);
                    continue;
                }
                _age[i] += dt;
                if (_age[i] >= _life[i])
                {
                    Hide(i);
                    continue;
                }
                float fade = FxKitLook.Fade(_age[i], _life[i]);
                float shown = _radius[i] * Grow(_age[i], _life[i]);
                float diameter = shown * 2f / RingBand;
                Vector3 n = _normal[i];
                _ring[i].localPosition = n * 0.035f;
                _ring[i].localRotation = Quaternion.FromToRotation(Vector3.up, n);
                _ring[i].localScale = new Vector3(diameter, 1f, diameter);
                Color ring = _color[i];
                ring.a = _opacity[i] * fade;
                _ringMat[i].color = ring;
                for (int b = 0; b < BitsMax; b++)
                {
                    int k = i * BitsMax + b;
                    if (b >= _bits[i])
                    {
                        _bitRend[k].enabled = false;
                        continue;
                    }
                    _bitVel[k].y -= 12f * dt;
                    _bit[k].localPosition += _bitVel[k] * dt;
                    float puff = _debris[i] * (0.85f + fade * 0.4f);
                    _bit[k].localScale = new Vector3(puff, puff, 1f);
                    Color c = _color[i];
                    c.a = _opacity[i] * fade;
                    _bitMat[k].color = c;
                    _bitRend[k].enabled = true;
                }
            }
        }

        void Spawn(Vector3 origin, Vector3 normal, Spec spec, int kind)
        {
            int slot = Free();
            if (slot < 0) slot = 0;
            _age[slot] = 0.0001f;
            _life[slot] = spec.Life;
            _radius[slot] = spec.Radius;
            _debris[slot] = spec.Debris;
            _bits[slot] = spec.Bits;
            _kind[slot] = kind;
            _normal[slot] = normal;
            _color[slot] = new Color(spec.R, spec.G, spec.B, 1f);
            _opacity[slot] = spec.Opacity;
            _root[slot].position = origin;
            _root[slot].gameObject.SetActive(true);
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.Cross(normal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            int n = spec.Bits;
            if (n < 1) n = 1;
            for (int b = 0; b < BitsMax; b++)
            {
                int k = slot * BitsMax + b;
                if (b >= n)
                {
                    _bitRend[k].enabled = false;
                    _bitVel[k] = Vector3.zero;
                    continue;
                }
                float ang = b * 6.2831855f / n;
                Vector3 radial = tangent * Mathf.Cos(ang) + bitangent * Mathf.Sin(ang);
                float hop = 0.75f + (b & 1) * 0.35f;
                _bitVel[k] = radial * spec.OutSpeed + normal * (spec.UpSpeed * hop);
                _bit[k].localPosition = normal * 0.05f + radial * (spec.Radius * 0.18f);
                _bitRend[k].enabled = true;
            }
        }

        void Hide(int slot)
        {
            _age[slot] = -1f;
            if (_root[slot] != null) _root[slot].gameObject.SetActive(false);
        }

        int Free()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) return i;
            }
            return -1;
        }

        static Transform Child(Transform parent, string name, Mesh mesh, Material mat, out Renderer rend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go.transform;
        }

        static Mesh QuadXY()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        static Mesh QuadXZ()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }

        static Texture2D RingTex()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            float mid = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - mid) / mid;
                    float dy = (y - mid) / mid;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float band = 1f - Mathf.Abs(r - RingBand) / 0.10f;
                    if (band < 0f) band = 0f;
                    float core = 1f - Mathf.Clamp01((r - 0.15f) / 0.55f);
                    core *= 0.28f;
                    float a = band;
                    if (core > a) a = core;
                    pixels[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
    }
}
