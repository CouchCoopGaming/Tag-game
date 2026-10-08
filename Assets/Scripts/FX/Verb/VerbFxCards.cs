using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Textured cards for the owned verb FX: dust flipbook, ground rings,
    /// hook chips, and wet drips. Built once. Late ticks only move them.
    /// </summary>
    [DefaultExecutionOrder(128)]
    public sealed class VerbFxCards : MonoBehaviour
    {
        public const int Bits = 8;
        public const int Swirls = 8;
        public const int Drips = 6;
        public const int Frames = 8;
        const float BitLife = 0.40f;
        const float SwirlLife = 0.36f;
        const float DripLife = 0.46f;
        const float HookLife = 0.28f;

        Transform _ring;
        Material _ringMat;
        Transform _hookRing;
        Material _hookRingMat;
        Transform[] _bits;
        Material[] _bitMat;
        readonly float[] _bitAge = new float[Bits];
        readonly Vector3[] _bitPos = new Vector3[Bits];
        readonly float[] _bitSize = new float[Bits];
        readonly float[] _bitR = new float[Bits];
        readonly float[] _bitG = new float[Bits];
        readonly float[] _bitB = new float[Bits];
        int _bitCursor;

        Transform[] _swirls;
        Material[] _swirlMat;
        readonly float[] _swirlAge = new float[Swirls];
        readonly Vector3[] _swirlPos = new Vector3[Swirls];
        readonly float[] _swirlSize = new float[Swirls];
        readonly float[] _swirlR = new float[Swirls];
        readonly float[] _swirlG = new float[Swirls];
        readonly float[] _swirlB = new float[Swirls];
        int _swirlCursor;

        Transform[] _drips;
        Material[] _dripMat;
        readonly float[] _dripAge = new float[Drips];
        readonly Vector3[] _dripPos = new Vector3[Drips];
        int _dripCursor;

        Transform[] _hooks;
        Material[] _hookMat;
        readonly float[] _chipAge = new float[4];
        readonly Vector3[] _chipPos = new Vector3[4];
        readonly float[] _chipSize = new float[4];
        int _hookCursor;
        float _hookRingAge = -1f;
        Vector3 _hookRingPos;
        uint _seed = 0xA11u;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<VerbFxCards>() == null)
                host.AddComponent<VerbFxCards>();
        }

        void Awake()
        {
            Shader billboard = Shader.Find("Tag/ComicBillboard");
            if (billboard == null) billboard = Shader.Find("Universal Render Pipeline/Unlit");
            if (billboard == null) billboard = Shader.Find("Sprites/Default");
            Shader decal = Shader.Find("Tag/FxDecal");
            if (decal == null) decal = billboard;
            Texture2D dust = Resources.Load<Texture2D>("FX/DustPuff");
            Texture2D rings = Resources.Load<Texture2D>("FX/RingAtlas");
            Texture2D drip = Resources.Load<Texture2D>("FX/Drip");
            Mesh quad = Quad();
            _ring = Make(transform, "LandDecal", quad, decal, rings, out _ringMat);
            _ring.rotation = Quaternion.Euler(90f, 0f, 0f);
            _hookRing = Make(transform, "HookDecal", quad, decal, rings, out _hookRingMat);
            _hookRing.rotation = Quaternion.Euler(90f, 0f, 0f);
            _bits = new Transform[Bits];
            _bitMat = new Material[Bits];
            for (int i = 0; i < Bits; i++)
            {
                _bitAge[i] = -1f;
                _bits[i] = Make(transform, "Debris", quad, billboard, dust, out _bitMat[i]);
            }
            _swirls = new Transform[Swirls];
            _swirlMat = new Material[Swirls];
            for (int i = 0; i < Swirls; i++)
            {
                _swirlAge[i] = -1f;
                _swirls[i] = Make(transform, "Swirl", quad, billboard, dust, out _swirlMat[i]);
            }
            _drips = new Transform[Drips];
            _dripMat = new Material[Drips];
            for (int i = 0; i < Drips; i++)
            {
                _dripAge[i] = -1f;
                _drips[i] = Make(transform, "Drip", quad, billboard, drip, out _dripMat[i]);
            }
            _hooks = new Transform[4];
            _hookMat = new Material[4];
            for (int i = 0; i < 4; i++)
            {
                _hookAge[i] = -1f;
                _hooks[i] = Make(transform, "HookChip", quad, billboard, dust, out _hookMat[i]);
            }
        }

        public void PaintRing(Vector3 pos, float radius, float age, float life, int surface, bool cracked)
        {
            if (_ring == null) return;
            float a = VerbFxEase.Alpha(age, life);
            if (a <= 0.001f)
            {
                _ring.gameObject.SetActive(false);
                return;
            }
            float grow = VerbFxEase.Scale(age, life, 0.62f, 1.08f);
            float diam = radius * 2f * grow;
            _ring.gameObject.SetActive(true);
            _ring.position = pos;
            _ring.rotation = Quaternion.Euler(90f, 0f, 0f);
            _ring.localScale = new Vector3(diam, diam, 1f);
            Atlas(_ringMat, surface, cracked);
            Color c = _ringMat.color;
            c.r = 1f;
            c.g = 1f;
            c.b = 1f;
            c.a = a;
            _ringMat.color = c;
        }

        public void HideRing()
        {
            if (_ring != null) _ring.gameObject.SetActive(false);
        }

        public void Burst(Vector3 pos, int count, float size, float r, float g, float b, float spread)
        {
            if (count < 1) return;
            if (count > Bits) count = Bits;
            for (int i = 0; i < count; i++)
            {
                float ang = i / (float)count * 6.2831855f + Next() * 0.4f;
                float rad = spread * (0.45f + Next() * 0.7f);
                Vector3 p = pos + new Vector3(Mathf.Cos(ang) * rad, 0.06f, Mathf.Sin(ang) * rad);
                Spawn(_bits, _bitMat, _bitAge, _bitPos, _bitSize, _bitR, _bitG, _bitB, ref _bitCursor, Bits, p, size, r, g, b);
            }
        }

        public void SpawnSwirl(Vector3 pos, float r, float g, float b)
        {
            Spawn(_swirls, _swirlMat, _swirlAge, _swirlPos, _swirlSize, _swirlR, _swirlG, _swirlB, ref _swirlCursor, Swirls, pos, 0.55f, r, g, b);
        }

        public void SpawnDrip(Vector3 pos)
        {
            int slot = _dripCursor % Drips;
            _dripCursor++;
            _dripAge[slot] = 0.0001f;
            _dripPos[slot] = pos;
        }

        public void SpawnHook(Vector3 pos, int surface, bool cracked)
        {
            _hookRingAge = 0.0001f;
            _hookRingPos = pos;
            Atlas(_hookRingMat, surface, cracked);
            Spawn(_hooks, _hookMat, _chipAge, _chipPos, _chipSize, null, null, null, ref _hookCursor, 4, pos, 0.22f, 0.95f, 0.9f, 0.7f);
        }

        public void Tick(float dt)
        {
            TickPuffs(_bits, _bitMat, _bitAge, _bitPos, _bitSize, _bitR, _bitG, _bitB, Bits, BitLife, dt, 0.22f);
            TickPuffs(_swirls, _swirlMat, _swirlAge, _swirlPos, _swirlSize, _swirlR, _swirlG, _swirlB, Swirls, SwirlLife, dt, 0.08f);
            TickDrips(dt);
            TickHook(dt);
            TickPuffs(_hooks, _hookMat, _chipAge, _chipPos, _chipSize, null, null, null, 4, HookLife, dt, 0.35f);
        }

        public void HideAll()
        {
            HideRing();
            _hookRingAge = -1f;
            if (_hookRing != null) _hookRing.gameObject.SetActive(false);
            Clear(_bits, _bitAge, Bits);
            Clear(_swirls, _swirlAge, Swirls);
            Clear(_drips, _dripAge, Drips);
            Clear(_hooks, _chipAge, 4);
        }

        void TickDrips(float dt)
        {
            for (int i = 0; i < Drips; i++)
            {
                if (_dripAge[i] < 0f)
                {
                    _drips[i].gameObject.SetActive(false);
                    continue;
                }
                _dripAge[i] += dt;
                if (_dripAge[i] >= DripLife)
                {
                    _dripAge[i] = -1f;
                    _drips[i].gameObject.SetActive(false);
                    continue;
                }
                float a = VerbFxEase.Alpha(_dripAge[i], DripLife);
                float stretch = VerbFxEase.Scale(_dripAge[i], DripLife, 0.7f, 1.35f);
                float fall = VerbFxEase.Out(_dripAge[i] / DripLife) * 0.55f;
                _drips[i].gameObject.SetActive(true);
                _drips[i].position = _dripPos[i] + new Vector3(0f, -fall, 0f);
                _drips[i].localScale = new Vector3(0.07f, 0.18f * stretch, 1f);
                Color c = _dripMat[i].color;
                c.r = 0.75f;
                c.g = 0.88f;
                c.b = 1f;
                c.a = a;
                _dripMat[i].color = c;
            }
        }

        void TickHook(float dt)
        {
            if (_hookRing == null) return;
            if (_hookRingAge < 0f)
            {
                _hookRing.gameObject.SetActive(false);
                return;
            }
            _hookRingAge += dt;
            if (_hookRingAge >= HookLife)
            {
                _hookRingAge = -1f;
                _hookRing.gameObject.SetActive(false);
                return;
            }
            float a = VerbFxEase.Alpha(_hookRingAge, HookLife);
            float diam = VerbFxEase.Scale(_hookRingAge, HookLife, 0.18f, 0.55f);
            _hookRing.gameObject.SetActive(true);
            _hookRing.position = _hookRingPos;
            _hookRing.rotation = Quaternion.Euler(90f, 0f, 0f);
            _hookRing.localScale = new Vector3(diam, diam, 1f);
            Color c = _hookRingMat.color;
            c.r = 1f;
            c.g = 1f;
            c.b = 1f;
            c.a = a;
            _hookRingMat.color = c;
        }

        void TickPuffs(Transform[] view, Material[] mat, float[] age, Vector3[] pos, float[] size, float[] r, float[] g, float[] b, int n, float life, float dt, float rise)
        {
            for (int i = 0; i < n; i++)
            {
                if (age[i] < 0f)
                {
                    view[i].gameObject.SetActive(false);
                    continue;
                }
                age[i] += dt;
                if (age[i] >= life)
                {
                    age[i] = -1f;
                    view[i].gameObject.SetActive(false);
                    continue;
                }
                float a = VerbFxEase.Alpha(age[i], life);
                float s = VerbFxEase.Scale(age[i], life, 0.35f, 1f);
                float sz = size != null ? size[i] : 0.4f;
                view[i].gameObject.SetActive(true);
                view[i].position = pos[i] + new Vector3(0f, VerbFxEase.Out(age[i] / life) * rise, 0f);
                view[i].localScale = new Vector3(sz * s, sz * s * 0.8f, 1f);
                Frame(mat[i], age[i], life);
                Color c = mat[i].color;
                c.r = r != null ? r[i] : 0.9f;
                c.g = g != null ? g[i] : 0.88f;
                c.b = b != null ? b[i] : 0.8f;
                c.a = a;
                mat[i].color = c;
            }
        }

        void Spawn(Transform[] view, Material[] mat, float[] age, Vector3[] pos, float[] size, float[] r, float[] g, float[] b, ref int cursor, int n, Vector3 p, float sz, float cr, float cg, float cb)
        {
            int slot = cursor % n;
            cursor++;
            if (slot < 0) slot += n;
            age[slot] = 0.0001f;
            pos[slot] = p;
            if (size != null) size[slot] = sz;
            if (r != null) r[slot] = cr;
            if (g != null) g[slot] = cg;
            if (b != null) b[slot] = cb;
        }

        static void Clear(Transform[] view, float[] age, int n)
        {
            for (int i = 0; i < n; i++)
            {
                age[i] = -1f;
                if (view != null && view[i] != null)
                    view[i].gameObject.SetActive(false);
            }
        }

        static void Frame(Material mat, float age, float life)
        {
            if (mat == null) return;
            float u = 0f;
            if (life > 0.0001f) u = age / life;
            if (u < 0f) u = 0f;
            if (u > 0.999f) u = 0.999f;
            int frame = (int)(u * Frames);
            if (frame > Frames - 1) frame = Frames - 1;
            mat.mainTextureScale = new Vector2(1f / Frames, 1f);
            mat.mainTextureOffset = new Vector2(frame / (float)Frames, 0f);
        }

        static void Atlas(Material mat, int surface, bool cracked)
        {
            if (mat == null) return;
            if (surface < 0) surface = 0;
            if (surface > 5) surface = 5;
            float col = surface / 6f;
            float row = cracked ? 0.5f : 0f;
            mat.mainTextureScale = new Vector2(1f / 6f, 0.5f);
            mat.mainTextureOffset = new Vector2(col, row);
        }

        float Next()
        {
            _seed = _seed * 1664525u + 1013904223u;
            return (_seed & 255) / 255f;
        }

        static Transform Make(Transform parent, string name, Mesh mesh, Shader shader, Texture2D tex, out Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            mat = new Material(shader);
            if (tex != null)
            {
                mat.mainTexture = tex;
                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            }
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.SetActive(false);
            return go.transform;
        }

        static Mesh Quad()
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
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            return mesh;
        }
    }
}
