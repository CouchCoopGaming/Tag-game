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
        /// <summary>Dust billow outlives the ring. It peaks near 0.12 s and is gone by 0.50 s.</summary>
        public const float PlumeLife = 0.50f;
        public const float Expand = 0.56f;
        public const float Gravity = 48f;
        const int PlumeMax = 10;
        const int PieceMax = BitsMax + PlumeMax;
        const int Slots = 4;
        const float RingOuter = 0.96f;
        // Wider band than 0.90 so a 0.80 m ring has a stroke at quarter-pane size.
        // The outer edge stays at 0.96, so the ring does not grow past its radius.
        const float RingInner = 0.74f;
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
            /// <summary>1 draws the thick wall-run ring. Land stays on the thin band.</summary>
            public int Wide;
            /// <summary>Wall-run puff size in metres. 0 keeps the land plume.</summary>
            public float Puff;
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
            // Pass 27. The old 3.3 m hard-land ring drew an arc across the pane.
            // Sprint stays a small circle. A hard land stays under a metre.
            spec.Radius = 0.38f + k * 0.42f;
            spec.Chunk = 0.08f;
            // Fewer pieces. A sprint slam is three chips. A hard land is five chunks.
            int bits = k < 0.25f ? 3 : 5;
            spec.Bits = bits;
            spec.Life = LifeSeconds;
            // Quiet enough that the dust, not the ring, is the read.
            spec.Opacity = 0.12f + k * 0.10f;
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
                spec.Plumes = PlumeCount(k);
            }
            else if (surface == (int)DustLook.Surface.Brick)
            {
                spec.R = 0.55f;
                spec.G = 0.22f;
                spec.B = 0.14f;
                spec.DustR = 0.78f;
                spec.DustG = 0.42f;
                spec.DustB = 0.30f;
                spec.Plumes = PlumeCount(k);
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
                spec.Plumes = PlumeCount(k);
            }
            if (spec.Plumes > PlumeMax) spec.Plumes = PlumeMax;
            return spec;
        }

        /// <summary>Sprint stays a wisp. A hard land is 6–10 overlapping puffs.</summary>
        public static int PlumeCount(float k)
        {
            if (k < 0.25f) return 2;
            int n = 6 + (int)(k * 2.5f);
            if (n > PlumeMax) n = PlumeMax;
            return n;
        }

        /// <summary>Center of one puff. The cloud top is this plus half the puff.</summary>
        public static float PlumeCenter(float age, float peak)
        {
            if (peak < 0.02f) peak = 0.02f;
            if (age <= 0.12f)
            {
                float t = age / 0.12f;
                if (t < 0f) t = 0f;
                t = 1f - (1f - t) * (1f - t);
                return 0.05f + (peak - 0.05f) * t;
            }
            float u = (age - 0.12f) / (PlumeLife - 0.12f);
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return peak * (1f - 0.22f * u);
        }

        public static float PlumeAlpha(float age, bool hard)
        {
            return PlumeAlpha(age, hard ? 0.94f : 0.28f);
        }

        public static float PlumeAlpha(float age, float body)
        {
            if (age <= 0.04f) return body * (age / 0.04f);
            if (age >= PlumeLife) return 0f;
            if (age <= 0.16f) return body;
            float u = (age - 0.16f) / (PlumeLife - 0.16f);
            if (u > 1f) u = 1f;
            return body * (1f - u);
        }

        /// <summary>
        /// Wall-run start. Pops up to the 0.26 m cap, then sits on the authored radius.
        /// A slow contact never draws larger than 0.26 m.
        /// </summary>
        public static float WallShown(float radius, float age)
        {
            float pop = 1f;
            if (age < 0.08f)
            {
                float t = age / 0.08f;
                if (t < 0f) t = 0f;
                pop = 1.22f - 0.22f * t;
            }
            float shown = radius * pop;
            if (radius < 0.261f && shown > 0.26f) shown = 0.26f;
            return shown;
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
        int[] _wide;
        float[] _puff;
        const int ScuffSlots = 4;
        const int ScuffPuffs = 6;
        const float ScuffSeconds = 0.42f;
        Transform[] _scuffRoot;
        Transform[] _scuffMark;
        Renderer[] _scuffMarkRend;
        Material[] _scuffMarkMat;
        Transform[] _scuffPuff;
        Renderer[] _scuffPuffRend;
        Material[] _scuffPuffMat;
        float[] _scuffAge;
        float[] _scuffWidth;
        float[] _scuffHeight;
        int[] _scuffCount;
        Vector3[] _scuffNormal;
        Vector3[] _scuffAlong;
        Color[] _scuffInk;
        Color[] _scuffDust;
        Texture2D _ringTex;
        Texture2D _ringWide;
        Mesh _quad;

        public static void Land(Vector3 origin, float speed, int surface)
        {
            if (!FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Land)) return;
            Raise(origin, Vector3.up, speed, surface, FxKitOptions.Land);
        }

        public static void Wall(Vector3 origin, Vector3 normal, float speed, int surface)
        {
            Wall(origin, normal, speed, surface, null);
        }

        public static void Wall(Vector3 origin, Vector3 normal, float speed, int surface, string material)
        {
            if (!FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Wall)) return;
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.up;
            else normal.Normalize();
            Raise(origin, normal, speed, surface, FxKitOptions.Wall);
            Scuff(origin, normal, speed, surface, material);
        }

        /// <summary>
        /// Wall-run start. A small ring and a short puff, plus the scuff.
        /// This is not the wall-bounce shockwave, and it does not wait for sprint speed.
        /// Below 13.8 m/s the ring stays under 0.26 m.
        /// </summary>
        public static void WallRunStart(Vector3 origin, Vector3 normal, float speed, int surface, string material)
        {
            if (!FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Wall)) return;
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.forward;
            else normal.Normalize();
            Scuff(origin, normal, speed, surface, material);
            Ensure();
            if (_host == null || _host._age == null) return;
            _host.Spawn(origin, normal, SmallWall(surface, speed), FxKitOptions.Wall);
        }

        /// <summary>Ring and puff for a wall-run start. Kept small when speed is under a sprint slam.</summary>
        public static Spec SmallWall(int surface, float speed)
        {
            bool slow = speed < SlamSpeed;
            float u = SlamSpeed > 0.01f ? speed / SlamSpeed : 0f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float k = Strength(speed);
            var spec = Measure(surface, slow ? SlamSpeed : speed);
            spec.Radius = slow ? 0.14f + u * 0.12f : 0.28f + k * 0.18f;
            spec.Bits = 0;
            spec.Plumes = slow ? 2 : 3;
            if (!slow)
            {
                spec.Plumes = 3 + (int)(k * 2f);
                if (spec.Plumes > 5) spec.Plumes = 5;
            }
            // The land ring thins as it grows. This one stays opaque so a
            // sub-30 cm stroke still reads beside the body.
            spec.Opacity = slow ? 0.88f : 0.94f;
            spec.Wide = 1;
            spec.Puff = slow ? 0.32f : 0.40f;
            spec.HopLo = 0.02f;
            spec.HopHi = slow ? 0.06f : 0.12f;
            spec.Chunk = 0.06f;
            spec.Life = LifeSeconds;
            return spec;
        }

        /// <summary>
        /// Fading smear and a short dust puff on a hard wall contact.
        /// Wall-run start, wall-jump kick, and a high-speed slam all use it.
        /// Sized by speed. Brick, wood, and concrete keep their own tint.
        /// The wall FX toggle hides it. No motor change.
        /// </summary>
        public static void Scuff(Vector3 origin, Vector3 normal, float speed, int surface, string material)
        {
            if (!FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Wall)) return;
            if (normal.sqrMagnitude < 0.0001f) normal = Vector3.forward;
            else normal.Normalize();
            Ensure();
            if (_host == null || _host._scuffAge == null) return;
            _host.BeginScuff(origin, normal, speed, surface, material);
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
            _ringWide = RingWideTex();
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
            _wide = new int[Slots];
            _puff = new float[Slots];
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
            BuildScuffs(bits);
        }

        void LateUpdate()
        {
            TickScuffs();
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
                // The ring keeps its 0.25 s life. Puffs drift until 0.50 s.
                float span = _plumes[i] > 0 ? PlumeLife : LifeSeconds;
                if (_age[i] >= span)
                {
                    Hide(i);
                    continue;
                }
                bool ringOn = _age[i] < LifeSeconds;
                float grow = Grow(ringOn ? _age[i] : LifeSeconds, LifeSeconds);
                float fade = ringOn ? Fade(_age[i], LifeSeconds) : 0f;
                // Land rings keep Grow(). A wall-run ring pops, then settles.
                float shown = _wide[i] > 0 ? WallShown(_radius[i], _age[i]) : _radius[i] * grow;
                float diameter = shown * 2f / RingOuter;
                Vector3 n = _normal[i];
                _ring[i].localPosition = n * 0.03f;
                _ring[i].localRotation = Quaternion.FromToRotation(Vector3.up, n);
                _ring[i].localScale = new Vector3(diameter, 1f, diameter);
                Texture tex = _wide[i] > 0 ? _ringWide : _ringTex;
                if (_ringMat[i].mainTexture != tex)
                {
                    _ringMat[i].mainTexture = tex;
                    if (_ringMat[i].HasProperty("_MainTex")) _ringMat[i].SetTexture("_MainTex", tex);
                }
                // The land ring thins out as it expands. The wall-run ring stays solid.
                float thin = _wide[i] > 0 ? 1f : 1f - 0.55f * grow;
                if (thin < 0.2f) thin = 0.2f;
                Color ring = _color[i];
                ring.a = _opacity[i] * fade * thin;
                _ringMat[i].color = ring;
                _ringRend[i].enabled = ringOn;
                int live = _bits[i] + _plumes[i];
                for (int b = 0; b < PieceMax; b++)
                {
                    int k = i * PieceMax + b;
                    if (b >= live || _piece[k].On == 0)
                    {
                        _bitRend[k].enabled = false;
                        continue;
                    }
                    if (b >= _bits[i])
                    {
                        PlacePlume(i, b - _bits[i], k, _age[i]);
                        continue;
                    }
                    if (!ringOn)
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
            _wide[slot] = spec.Wide;
            _puff[slot] = spec.Puff;
            // Land ring is the dust colour. The wall-run ring is a pale stroke on the wall.
            if (spec.Wide > 0)
            {
                _color[slot] = new Color(
                    spec.DustR + 0.2f > 1f ? 1f : spec.DustR + 0.2f,
                    spec.DustG + 0.2f > 1f ? 1f : spec.DustG + 0.2f,
                    spec.DustB + 0.2f > 1f ? 1f : spec.DustB + 0.2f,
                    1f);
            }
            else
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
            if (n < 0) n = 0;
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
                    float pace = StrengthFromRadius(spec.Radius);
                    bool chunky = pace >= 0.75f;
                    float hop = chunky
                        ? Mathf.Lerp(spec.HopHi * 0.55f, spec.HopHi * 0.85f, h2)
                        : Mathf.Lerp(spec.HopLo, spec.HopHi, h2);
                    float vy = Mathf.Sqrt(2f * Gravity * hop);
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
                    // Hard land: five chunks, 36–44 cm, dark against a pale one.
                    // They start outside the body. Sprint stays grit.
                    // The hop tops out under the 1 m plume.
                    if (chunky)
                        piece.Size = 0.36f + h2 * 0.08f;
                    else
                    {
                        float span = h2 * h2;
                        piece.Size = 0.02f + span * 0.06f;
                        if (piece.Size > 0.08f) piece.Size = 0.08f;
                    }
                    float shade = chunky ? ((b & 1) == 0 ? 0.12f : 2.4f) : (0.82f + h * 0.28f);
                    piece.R = spec.R * shade;
                    piece.G = spec.G * shade;
                    piece.B = spec.B * shade;
                    if (piece.R > 1f) piece.R = 1f;
                    if (piece.G > 1f) piece.G = 1f;
                    if (piece.B > 1f) piece.B = 1f;
                    piece.Settle = -1f;
                    bool grass = spec.Splinter > 1.5f;
                    bool dirtClod = grass && (b % 4) == 0;
                    bool splinter = !grass && spec.Splinter > 0.5f;
                    if (chunky)
                    {
                        piece.Shape = ShapeChunk;
                        piece.Aspect = 0.62f + h * 0.35f;
                    }
                    else if (dirtClod)
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
                    if (chunky && floor && n > 0)
                    {
                        // In front of the body, where the chase camera can see them.
                        Vector3 toCam = Vector3.forward;
                        Camera view = Camera.main;
                        if (view != null)
                        {
                            toCam = view.transform.position - origin;
                            toCam.y = 0f;
                        }
                        if (toCam.sqrMagnitude < 0.0001f) toCam = Vector3.forward;
                        toCam.Normalize();
                        Vector3 side = Vector3.Cross(Vector3.up, toCam);
                        float mid = (n - 1) * 0.5f;
                        Vector3 place = toCam * 1.25f + side * ((b - mid) * 0.62f);
                        place.y = 0.50f + (b % 2) * 0.18f;
                        _bit[k].localPosition = place;
                    }
                    else
                    {
                        float outR = chunky ? 1.15f + h * 0.35f : 0.08f + h * 0.16f;
                        _bit[k].localPosition = normal * start + radial * outR;
                    }
                    _bitMat[k].SetFloat("_Shape", piece.Shape);
                }
                else if (b < n + spec.Plumes)
                {
                    int p = b - n;
                    float h = Hash(100 + p + slot * 9);
                    float pace = StrengthFromRadius(spec.Radius);
                    bool hard = pace >= 0.75f;
                    piece.Size = hard ? 0.66f + h * 0.04f : 0.16f + h * 0.06f;
                    piece.Aspect = 1.15f;
                    piece.R = spec.DustR;
                    piece.G = spec.DustG;
                    piece.B = spec.DustB;
                    piece.Shape = ShapeSoft;
                    piece.On = 1;
                    piece.Vel = Vector3.zero;
                    _bitMat[k].SetFloat("_Shape", ShapeSoft);
                }
                _piece[k] = piece;
            }
        }

        void PlacePlume(int slot, int p, int k, float age)
        {
            float h = Hash(100 + p + slot * 9);
            float h2 = Hash(130 + p);
            if (_puff[slot] > 0.01f)
            {
                PlaceWallPuff(slot, p, k, age, h, h2);
                return;
            }
            float pace = StrengthFromRadius(_radius[slot]);
            bool hard = pace >= 0.75f;
            float peak = hard ? 0.56f + h2 * 0.08f : 0.08f + h2 * 0.06f;
            float y = PlumeCenter(age, peak);
            float ang = h * 6.2831855f;
            // Hard-land cloud reaches past the feet, then keeps drifting.
            float spread = (hard ? 0.22f : 0.04f) + h * (hard ? 0.48f : 0.06f);
            spread += age * (hard ? 0.85f : 0.12f);
            Vector3 n = _normal[slot];
            Vector3 tangent = Vector3.Cross(n, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.Cross(n, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(n, tangent).normalized;
            Vector3 radial = tangent * Mathf.Cos(ang) + bitangent * Mathf.Sin(ang);
            Vector3 pos = radial * spread + n * y;
            float into = Vector3.Dot(pos, n);
            if (into < 0.04f) pos += n * (0.04f - into);
            _bit[k].localPosition = pos;
            float grow = age / 0.12f;
            if (grow < 0f) grow = 0f;
            if (grow > 1f) grow = 1f;
            grow = 0.72f + 0.28f * grow;
            float size = _piece[k].Size * grow;
            // Cloud top stays at or under 1 m. The life is still 0.50 s.
            float cap = 1.0f - y;
            if (cap < 0.05f) cap = 0.05f;
            if (size > cap * 2f) size = cap * 2f;
            _bit[k].localScale = new Vector3(size * 1.15f, size, 1f);
            float a = PlumeAlpha(age, hard);
            _bitMat[k].color = new Color(_piece[k].R, _piece[k].G, _piece[k].B, a);
            _bitRend[k].enabled = a > 0.02f;
        }

        void PlaceWallPuff(int slot, int p, int k, float age, float h, float h2)
        {
            float size = _puff[slot];
            Vector3 n = _normal[slot];
            Vector3 tangent = Vector3.Cross(n, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.Cross(n, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(n, tangent).normalized;
            float ang = h * 6.2831855f;
            Vector3 radial = tangent * Mathf.Cos(ang) + bitangent * Mathf.Sin(ang);
            // Above the ring, off the wall, so the stroke stays visible.
            Vector3 pos = radial * (0.06f + h * 0.10f) + n * (0.36f + h2 * 0.10f);
            pos.y += 0.58f + h * 0.14f;
            _bit[k].localPosition = pos;
            _bit[k].localScale = new Vector3(size * 1.15f, size, 1f);
            float a = PlumeAlpha(age, 0.78f);
            _bitMat[k].color = new Color(_piece[k].R, _piece[k].G, _piece[k].B, a);
            _bitRend[k].enabled = a > 0.02f;
        }

        static float StrengthFromRadius(float radius)
        {
            float k = (radius - 0.38f) / 0.42f;
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

        void BuildScuffs(Shader bits)
        {
            _scuffRoot = new Transform[ScuffSlots];
            _scuffMark = new Transform[ScuffSlots];
            _scuffMarkRend = new Renderer[ScuffSlots];
            _scuffMarkMat = new Material[ScuffSlots];
            _scuffPuff = new Transform[ScuffSlots * ScuffPuffs];
            _scuffPuffRend = new Renderer[ScuffSlots * ScuffPuffs];
            _scuffPuffMat = new Material[ScuffSlots * ScuffPuffs];
            _scuffAge = new float[ScuffSlots];
            _scuffWidth = new float[ScuffSlots];
            _scuffHeight = new float[ScuffSlots];
            _scuffCount = new int[ScuffSlots];
            _scuffNormal = new Vector3[ScuffSlots];
            _scuffAlong = new Vector3[ScuffSlots];
            _scuffInk = new Color[ScuffSlots];
            _scuffDust = new Color[ScuffSlots];
            Mesh quad = QuadXY();
            for (int i = 0; i < ScuffSlots; i++)
            {
                _scuffAge[i] = -1f;
                var root = new GameObject("WallScuff");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                _scuffRoot[i] = root.transform;
                _scuffMarkMat[i] = new Material(bits);
                _scuffMarkMat[i].SetFloat("_Shape", ShapeSoft);
                _scuffMarkMat[i].SetFloat("_Billboard", 0f);
                _scuffMarkMat[i].SetFloat("_Guard", 0f);
                _scuffMarkMat[i].SetFloat("_Edge", -1f);
                _scuffMark[i] = Child(root.transform, "Mark", quad, _scuffMarkMat[i], out _scuffMarkRend[i]);
                _scuffMarkRend[i].enabled = false;
                for (int p = 0; p < ScuffPuffs; p++)
                {
                    int k = i * ScuffPuffs + p;
                    _scuffPuffMat[k] = new Material(bits);
                    _scuffPuffMat[k].SetFloat("_Shape", ShapeSoft);
                    _scuffPuffMat[k].SetFloat("_Billboard", 1f);
                    _scuffPuffMat[k].SetFloat("_Guard", 0f);
                    _scuffPuffMat[k].SetFloat("_Edge", -1f);
                    _scuffPuff[k] = Child(root.transform, "Puff", quad, _scuffPuffMat[k], out _scuffPuffRend[k]);
                    _scuffPuffRend[k].enabled = false;
                }
            }
        }

        void BeginScuff(Vector3 origin, Vector3 normal, float speed, int surface, string material)
        {
            int slot = 0;
            for (int i = 0; i < ScuffSlots; i++)
            {
                if (_scuffAge[i] < 0f)
                {
                    slot = i;
                    break;
                }
            }
            float k = Strength(speed);
            float width = 0.26f + k * 0.52f;
            float height = 0.16f + k * 0.26f;
            int puffs = k < 0.25f ? 3 : 5 + (int)(k * 2f);
            if (puffs > ScuffPuffs) puffs = ScuffPuffs;
            ScuffTint(surface, material, out float r, out float g, out float b, out float dr, out float dg, out float db);
            Vector3 along = Vector3.Cross(normal, Vector3.up);
            if (along.sqrMagnitude < 0.0001f) along = Vector3.Cross(normal, Vector3.right);
            along.Normalize();
            Vector3 up = Vector3.Cross(along, normal).normalized;
            _scuffAge[slot] = 0.0001f;
            _scuffWidth[slot] = width;
            _scuffHeight[slot] = height;
            _scuffCount[slot] = puffs;
            _scuffNormal[slot] = normal;
            _scuffAlong[slot] = along;
            _scuffInk[slot] = new Color(r, g, b, 1f);
            _scuffDust[slot] = new Color(dr, dg, db, 1f);
            _scuffRoot[slot].position = origin + normal * 0.02f;
            _scuffRoot[slot].rotation = Quaternion.LookRotation(normal, up);
            _scuffRoot[slot].gameObject.SetActive(true);
        }

        void TickScuffs()
        {
            if (_scuffAge == null) return;
            bool show = FxKitLook.Master(GameSettings.Current) && FxKitLook.Bursts(GameSettings.Current, FxKitOptions.Wall);
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            for (int i = 0; i < ScuffSlots; i++)
            {
                if (_scuffAge[i] < 0f) continue;
                if (!show)
                {
                    HideScuff(i);
                    continue;
                }
                _scuffAge[i] += dt;
                if (_scuffAge[i] >= ScuffSeconds)
                {
                    HideScuff(i);
                    continue;
                }
                float markA = 0.72f;
                if (_scuffAge[i] > 0.12f)
                {
                    float fadeU = (_scuffAge[i] - 0.12f) / (ScuffSeconds - 0.12f);
                    if (fadeU > 1f) fadeU = 1f;
                    markA = 0.72f * (1f - fadeU);
                }
                _scuffMark[i].localPosition = Vector3.zero;
                _scuffMark[i].localRotation = Quaternion.identity;
                _scuffMark[i].localScale = new Vector3(_scuffWidth[i], _scuffHeight[i], 1f);
                Color ink = _scuffInk[i];
                ink.a = markA;
                _scuffMarkMat[i].color = ink;
                _scuffMarkRend[i].enabled = markA > 0.03f;
                Vector3 n = _scuffNormal[i];
                Vector3 along = _scuffAlong[i];
                for (int p = 0; p < ScuffPuffs; p++)
                {
                    int k = i * ScuffPuffs + p;
                    if (p >= _scuffCount[i] || _scuffAge[i] > 0.24f)
                    {
                        _scuffPuffRend[k].enabled = false;
                        continue;
                    }
                    float h = Hash(200 + p + i * 5);
                    float life = 0.22f;
                    float puffU = _scuffAge[i] / life;
                    if (puffU > 1f) puffU = 1f;
                    float fade = 1f - puffU;
                    float outD = 0.08f + (0.14f + h * 0.18f) * puffU;
                    float slide = (h - 0.5f) * _scuffWidth[i] * 0.85f;
                    float rise = _scuffHeight[i] * 0.45f + (Hash(240 + p) - 0.2f) * _scuffHeight[i] * 0.35f;
                    _scuffPuff[k].position = _scuffRoot[i].position + n * outD + along * slide + Vector3.up * rise;
                    float size = (0.16f + h * 0.14f) * (0.75f + 0.35f * puffU);
                    _scuffPuff[k].localScale = new Vector3(size, size * 0.85f, 1f);
                    Color dust = _scuffDust[i];
                    dust.a = 0.72f * fade;
                    _scuffPuffMat[k].color = dust;
                    _scuffPuffRend[k].enabled = dust.a > 0.03f;
                }
            }
        }

        void HideScuff(int slot)
        {
            _scuffAge[slot] = -1f;
            if (_scuffRoot[slot] != null) _scuffRoot[slot].gameObject.SetActive(false);
        }

        static void ScuffTint(int surface, string material, out float r, out float g, out float b, out float dr, out float dg, out float db)
        {
            string n = string.IsNullOrEmpty(material) ? "" : material.ToLowerInvariant();
            bool brick = surface == (int)DustLook.Surface.Brick
                || n.IndexOf("brick", System.StringComparison.Ordinal) >= 0
                || n.IndexOf("masonry", System.StringComparison.Ordinal) >= 0;
            bool wood = surface == (int)DustLook.Surface.Wood
                || n.IndexOf("wood", System.StringComparison.Ordinal) >= 0
                || n.IndexOf("plank", System.StringComparison.Ordinal) >= 0
                || n.IndexOf("cedar", System.StringComparison.Ordinal) >= 0;
            if (brick)
            {
                r = 0.26f; g = 0.10f; b = 0.07f;
                dr = 0.72f; dg = 0.40f; db = 0.30f;
            }
            else if (wood)
            {
                r = 0.22f; g = 0.12f; b = 0.05f;
                dr = 0.78f; dg = 0.60f; db = 0.36f;
            }
            else
            {
                r = 0.20f; g = 0.20f; b = 0.19f;
                dr = 0.84f; dg = 0.83f; db = 0.80f;
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
                    // Donut. The stroke is wider than the old 0.90 hole. Outer edge is unchanged.
                    if (r <= RingOuter && r >= RingInner)
                    {
                    float rise = (r - RingInner) / 0.028f;
                    if (rise > 1f) rise = 1f;
                    float fall = (RingOuter - r) / 0.028f;
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

        /// <summary>
        /// Thick band for the wall-run start. The land ring keeps RingTex.
        /// Inner 0.30 on outer 0.96, so a 0.26 m ring has a stroke and a hole.
        /// </summary>
        static Texture2D RingWideTex()
        {
            const int n = 128;
            const float inner = 0.30f;
            const float edge = 0.05f;
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
                    if (r <= RingOuter && r >= inner)
                    {
                        float rise = (r - inner) / edge;
                        if (rise > 1f) rise = 1f;
                        float fall = (RingOuter - r) / edge;
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
