using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Hard-land and wall-slam shockwave. A thin ring on the surface, a burst
    /// of chunky debris, and a short dust plume on dirt and concrete.
    /// Colour follows the surface. Size follows impact speed.
    /// Visual only. The land and wall FX toggles hide it. No new settings row.
    /// </summary>
    [DefaultExecutionOrder(140)]
    public sealed class ImpactFx : MonoBehaviour
    {
        public const int BitsMin = 5;
        public const int BitsMax = 30;
        public const float SlamSpeed = 13.8f;
        public const float HardSpeed = 36.5f;
        public const float LifeSeconds = 0.25f;
        public const float Expand = 0.56f;
        public const float Gravity = 48f;
        const int PlumeMax = 8;
        const int PieceMax = BitsMax + PlumeMax;
        const int Slots = 4;
        const float RingOuter = 0.96f;
        const float RingInner = 0.78f;
        const float ShapeSoft = 0f;
        const float ShapeChunk = 3f;
        const float ShapeSplinter = 4f;

        public struct Spec
        {
            public float Radius;
            public float Chunk;
            public int Bits;
            public int Plumes;
            public float Life;
            public float Opacity;
            public float R;
            public float G;
            public float B;
            public float DustR;
            public float DustG;
            public float DustB;
            public float HopLo;
            public float HopHi;
            public float Splinter;
        }

        /// <summary>0 at a sprint slam, 1 at the hard-land speed.</summary>
        public static float Strength(float speed)
        {
            float span = HardSpeed - SlamSpeed;
            float k = span > 0.01f ? (speed - SlamSpeed) / span : 0f;
            if (k < 0f) k = 0f;
            if (k > 1.15f) k = 1.15f;
            return k;
        }

        /// <summary>
        /// Thin ring and chunky debris. A hard land is a stronger, fuller burst
        /// than a sprint slam. Colours are the surface dust.
        /// </summary>
        public static Spec Measure(int surface, float speed)
        {
            float k = Strength(speed);
            var spec = new Spec();
            spec.Radius = 1.45f + k * 1.85f;
            // Chips stay grit-sized. Speed changes how many fly and how high, not how big.
            spec.Chunk = 0.08f;
            int bits = BitsMin + (int)(k * 14f);
            if (bits < BitsMin) bits = BitsMin;
            if (bits > BitsMax) bits = BitsMax;
            spec.Bits = bits;
            spec.Life = LifeSeconds;
            spec.Opacity = 0.42f + k * 0.48f;
            // A sprint slam stays near the ground. A hard land kicks 0.3–0.8 m.
            spec.HopLo = 0.04f + k * 0.26f;
            spec.HopHi = 0.10f + k * 0.70f;
            spec.Splinter = 0f;
            spec.Plumes = 0;
            // Pale grey concrete, green-brown grass, tan dirt, wood splinters plus dust.
            if (surface == (int)DustLook.Surface.Grass)
            {
                spec.R = 0.34f;
                spec.G = 0.40f;
                spec.B = 0.16f;
                spec.DustR = 0.58f;
                spec.DustG = 0.64f;
                spec.DustB = 0.30f;
                spec.Splinter = 2f;
            }
            else if (surface == (int)DustLook.Surface.Dirt)
            {
                spec.R = 0.42f;
                spec.G = 0.26f;
                spec.B = 0.12f;
                spec.DustR = 0.72f;
                spec.DustG = 0.50f;
                spec.DustB = 0.26f;
                spec.Plumes = 2 + (int)(k * 4f);
            }
            else if (surface == (int)DustLook.Surface.Wood)
            {
                spec.R = 0.62f;
                spec.G = 0.48f;
                spec.B = 0.28f;
                spec.DustR = 0.80f;
                spec.DustG = 0.70f;
                spec.DustB = 0.52f;
                spec.Splinter = 1f;
                spec.Plumes = 2 + (int)(k * 2f);
            }
            else if (surface == (int)DustLook.Surface.Metal)
            {
                spec.R = 0.72f;
                spec.G = 0.76f;
                spec.B = 0.82f;
                spec.DustR = spec.R;
                spec.DustG = spec.G;
                spec.DustB = spec.B;
            }
            else if (surface == (int)DustLook.Surface.Wet)
            {
                spec.R = 0.42f;
                spec.G = 0.56f;
                spec.B = 0.64f;
                spec.DustR = 0.55f;
                spec.DustG = 0.68f;
                spec.DustB = 0.74f;
                spec.Plumes = 2 + (int)(k * 2f);
            }
            else
            {
                spec.R = 0.50f;
                spec.G = 0.49f;
                spec.B = 0.47f;
                spec.DustR = 0.76f;
                spec.DustG = 0.75f;
                spec.DustB = 0.72f;
                spec.Plumes = 2 + (int)(k * 4f);
            }
            if (spec.Plumes > PlumeMax) spec.Plumes = PlumeMax;
            return spec;
        }

        /// <summary>0.22 at the pop, 1 once the ring has reached its radius.</summary>
        public static float Grow(float age, float life)
        {
            if (life < 0.0001f) life = 0.0001f;
            float u = age / life;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            if (u >= Expand) return 1f;
            float t = u / Expand;
            t = 1f - (1f - t) * (1f - t);
            return 0.22f + 0.78f * t;
        }

        /// <summary>Holds through the expansion, then fades out over the rest of the life.</summary>
        public static float Fade(float age, float life)
        {
            if (life < 0.0001f) life = 0.0001f;
            float u = age / life;
            if (u <= Expand) return 1f;
            float t = (u - Expand) / (1f - Expand);
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return 1f - t;
        }

        struct Piece
        {
            public Vector3 Vel;
            public float Size;
            public float Aspect;
            public float R;
            public float G;
            public float B;
            public float Shape;
            public float Settle;
            public byte On;
        }

        static ImpactFx _host;

        Transform[] _root;
        Transform[] _ring;
        Renderer[] _ringRend;
        Material[] _ringMat;
        Transform[] _bit;
        Renderer[] _bitRend;
        Material[] _bitMat;
        Piece[] _piece;
        float[] _age;
        float[] _life;
        float[] _radius;
        int[] _bits;
        int[] _plumes;
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
            Shader bits = Shader.Find("Tag/FxKitSprite");
            if (bits == null) bits = decal;
            _root = new Transform[Slots];
            _ring = new Transform[Slots];
            _ringRend = new Renderer[Slots];
            _ringMat = new Material[Slots];
            _bit = new Transform[Slots * PieceMax];
            _bitRend = new Renderer[Slots * PieceMax];
            _bitMat = new Material[Slots * PieceMax];
            _piece = new Piece[Slots * PieceMax];
            _age = new float[Slots];
            _life = new float[Slots];
            _radius = new float[Slots];
            _bits = new int[Slots];
            _plumes = new int[Slots];
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
                for (int b = 0; b < PieceMax; b++)
                {
                    int k = i * PieceMax + b;
                    _bitMat[k] = new Material(bits);
                    _bitMat[k].SetFloat("_Shape", ShapeChunk);
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
            if (dt > 0.05f) dt = 0.05f;
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
                float grow = Grow(_age[i], _life[i]);
                float fade = Fade(_age[i], _life[i]);
                float shown = _radius[i] * grow;
                float diameter = shown * 2f / RingOuter;
                Vector3 n = _normal[i];
                _ring[i].localPosition = n * 0.03f;
                _ring[i].localRotation = Quaternion.FromToRotation(Vector3.up, n);
                _ring[i].localScale = new Vector3(diameter, 1f, diameter);
                // The ring thins out as it expands, so the ground stays visible through it.
                float thin = 1f - 0.55f * grow;
                if (thin < 0.2f) thin = 0.2f;
                Color ring = _color[i];
                ring.a = _opacity[i] * fade * thin;
                _ringMat[i].color = ring;
                int live = _bits[i] + _plumes[i];
                for (int b = 0; b < PieceMax; b++)
                {
                    int k = i * PieceMax + b;
                    if (b >= live || _piece[k].On == 0)
                    {
                        _bitRend[k].enabled = false;
                        continue;
                    }
                    Vector3 vel = _piece[k].Vel;
                    vel.y -= Gravity * dt;
                    Vector3 pos = _bit[k].localPosition + vel * dt;
                    float into = Vector3.Dot(pos, n);
                    if (into < 0.03f)
                    {
                        pos += n * (0.03f - into);
                        float vn = Vector3.Dot(vel, n);
                        if (vn < 0f) vel -= n * vn;
                    }
                    if (n.y > 0.75f && pos.y < 0.03f)
                    {
                        pos.y = 0.03f;
                        if (vel.y < 0f)
                        {
                            // One short bounce, then they stay down and fade. No pile.
                            if (_piece[k].Settle < 0f && -vel.y > 0.8f)
                                vel.y = -vel.y * 0.28f;
                            else
                                vel.y = 0f;
                            if (_piece[k].Settle < 0f) _piece[k].Settle = 0f;
                        }
                        if (_piece[k].Settle >= 0f) _piece[k].Settle += dt;
                    }
                    _piece[k].Vel = vel;
                    _bit[k].localPosition = pos;
                    float wide = _piece[k].Size * _piece[k].Aspect;
                    float tall = _piece[k].Size;
                    if (_piece[k].Shape < 0.5f)
                    {
                        float puff = 0.85f + 0.35f * (_age[i] / _life[i]);
                        wide *= puff;
                        tall *= puff;
                    }
                    _bit[k].localScale = new Vector3(wide, tall, 1f);
                    Color c = new Color(_piece[k].R, _piece[k].G, _piece[k].B, 1f);
                    float a = fade;
                    if (_piece[k].Shape < 0.5f) a *= 0.62f;
                    else if (_piece[k].Settle >= 0f)
                    {
                        float settled = _piece[k].Settle / 0.08f;
                        if (settled > 1f) settled = 1f;
                        a *= 1f - settled;
                    }
                    c.a = a;
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
            _bits[slot] = spec.Bits;
            _plumes[slot] = spec.Plumes;
            _kind[slot] = kind;
            _normal[slot] = normal;
            // The ring is the dust colour. Chunks are a step darker so they read against it.
            _color[slot] = new Color(spec.DustR, spec.DustG, spec.DustB, 1f);
            _opacity[slot] = spec.Opacity;
            _root[slot].position = origin;
            _root[slot].rotation = Quaternion.identity;
            _root[slot].gameObject.SetActive(true);
            bool floor = normal.y > 0.75f;
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.Cross(normal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
            int n = spec.Bits;
            if (n < 1) n = 1;
            for (int b = 0; b < PieceMax; b++)
            {
                int k = slot * PieceMax + b;
                Piece piece = _piece[k];
                piece.On = 0;
                piece.Vel = Vector3.zero;
                piece.Settle = -1f;
                _bitRend[k].enabled = false;
                if (b < n)
                {
                    float h = Hash(b + slot * 17);
                    float h2 = Hash(b + 40 + slot * 3);
                    float ang = (b + h * 0.35f) * 6.2831855f / n;
                    float hop = Mathf.Lerp(spec.HopLo, spec.HopHi, h2);
                    float vy = Mathf.Sqrt(2f * Gravity * hop);
                    float pace = StrengthFromRadius(spec.Radius);
                    float spread = (0.8f + pace * 2.2f) * (0.4f + h);
                    Vector3 radial = tangent * Mathf.Cos(ang) + bitangent * Mathf.Sin(ang);
                    Vector3 vel;
                    float start;
                    if (floor)
                    {
                        vel = radial * spread + Vector3.up * vy;
                        start = 0.06f;
                    }
                    else
                    {
                        // Pop off the wall toward the camera so the bits clear the body.
                        vel = tangent * Mathf.Cos(ang) * spread + normal * spread * (0.55f + h * 0.35f) + Vector3.up * vy;
                        start = 0.35f;
                    }
                    piece.Vel = vel;
                    // Most chips 2–4 cm. The square bias keeps the 8 cm ones rare.
                    float span = h2 * h2;
                    piece.Size = 0.02f + span * 0.06f;
                    if (piece.Size > 0.08f) piece.Size = 0.08f;
                    float shade = 0.82f + h * 0.28f;
                    piece.R = spec.R * shade;
                    piece.G = spec.G * shade;
                    piece.B = spec.B * shade;
                    piece.Settle = -1f;
                    bool grass = spec.Splinter > 1.5f;
                    bool dirtClod = grass && (b % 4) == 0;
                    bool splinter = !grass && spec.Splinter > 0.5f;
                    if (dirtClod)
                    {
                        piece.Shape = ShapeChunk;
                        piece.Aspect = 0.75f + h * 0.35f;
                        piece.R = 0.42f * shade;
                        piece.G = 0.26f * shade;
                        piece.B = 0.12f * shade;
                    }
                    else if (splinter)
                    {
                        piece.Shape = ShapeSplinter;
                        piece.Aspect = 0.22f;
                    }
                    else if (grass)
                    {
                        piece.Shape = ShapeSplinter;
                        piece.Aspect = 1.8f;
                        piece.Size *= 0.85f;
                    }
                    else
                    {
                        piece.Shape = ShapeChunk;
                        piece.Aspect = 0.7f + h * 0.5f;
                    }
                    piece.On = 1;
                    _bit[k].localPosition = normal * start + radial * (0.08f + h * 0.16f);
                    _bitMat[k].SetFloat("_Shape", piece.Shape);
                }
                else if (b < n + spec.Plumes)
                {
                    int p = b - n;
                    float h = Hash(100 + p + slot * 9);
                    float h2 = Hash(130 + p);
                    float ang = h * 6.2831855f;
                    float hop = Mathf.Lerp(0.22f, 0.55f, h2);
                    float vy = Mathf.Sqrt(2f * Gravity * hop);
                    float spread = spec.Radius * (0.55f + h * 1.15f);
                    Vector3 radial = tangent * Mathf.Cos(ang) + bitangent * Mathf.Sin(ang);
                    Vector3 vel = floor
                        ? radial * spread + Vector3.up * vy
                        : tangent * Mathf.Cos(ang) * spread + normal * spread * 0.4f + Vector3.up * vy;
                    piece.Vel = vel;
                    piece.Size = (0.62f + StrengthFromRadius(spec.Radius) * 0.40f) * (0.85f + h2 * 0.40f);
                    piece.Aspect = 1.15f;
                    piece.R = spec.DustR;
                    piece.G = spec.DustG;
                    piece.B = spec.DustB;
                    piece.Shape = ShapeSoft;
                    piece.On = 1;
                    _bit[k].localPosition = normal * 0.08f + radial * (0.12f + h * 0.2f);
                    _bitMat[k].SetFloat("_Shape", ShapeSoft);
                }
                _piece[k] = piece;
            }
        }

        static float StrengthFromRadius(float radius)
        {
            float k = (radius - 1.45f) / 1.85f;
            if (k < 0f) k = 0f;
            if (k > 1.15f) k = 1.15f;
            return k;
        }

        static float Hash(int i)
        {
            uint x = (uint)(i * 374761393 + 668265263);
            x = (x ^ (x >> 13)) * 1274126177u;
            return (x & 65535) / 65535f;
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
            const int n = 128;
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
                    float a = 0f;
                    // Donut. Inner radius stays at least 70% of the outer edge.
                    if (r <= RingOuter && r >= RingInner)
                    {
                        float rise = (r - RingInner) / 0.035f;
                        if (rise > 1f) rise = 1f;
                        float fall = (RingOuter - r) / 0.035f;
                        if (fall > 1f) fall = 1f;
                        a = rise < fall ? rise : fall;
                    }
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
