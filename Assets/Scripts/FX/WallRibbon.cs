using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// One seat-colored strip on the wall per runner. It is pooled.
    /// The strip stays after the body leaves, then clips away over 0.40 s.
    /// The wall-run start ring is a separate effect.
    /// </summary>
    public sealed class WallRibbon : MonoBehaviour
    {
        public const int Players = 4;
        public const int Segments = 12;
        public const float Width = 0.20f;
        public const float Life = 0.40f;
        public const float MinStep = 0.22f;

        struct Strip
        {
            public int Count;
            public float Age;
            public bool Live;
            public Vector3 Normal;
            public float R;
            public float G;
            public float B;
            public Vector3[] Points;
        }

        static WallRibbon _host;

        Strip[] _strip;
        Mesh[] _mesh;
        Material[] _mat;
        Renderer[] _rend;
        Vector3[][] _verts;
        Vector2[] _uv;

        public static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("WallRibbon");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<WallRibbon>();
        }

        public static void Begin(int seat, Vector3 point, Vector3 normal, Color color)
        {
            Ensure();
            if (_host == null) return;
            _host.Open(seat, point, normal, color);
        }

        public static void Extend(int seat, Vector3 point, Vector3 normal)
        {
            if (_host == null) return;
            _host.Push(seat, point, normal);
        }

        public static void End(int seat)
        {
            if (_host == null) return;
            _host.Close(seat);
        }

        public static void Tick(float dt)
        {
            if (_host == null) return;
            _host.Advance(dt);
        }

        void Awake()
        {
            _host = this;
            _strip = new Strip[Players];
            _mesh = new Mesh[Players];
            _mat = new Material[Players];
            _rend = new Renderer[Players];
            _verts = new Vector3[Players][];
            _uv = new Vector2[Segments * 4];
            Shader shader = Shader.Find("Tag/FxRibbon");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            int[] tris = new int[Segments * 6];
            for (int s = 0; s < Segments; s++)
            {
                int v = s * 4;
                int t = s * 6;
                tris[t] = v;
                tris[t + 1] = v + 2;
                tris[t + 2] = v + 1;
                tris[t + 3] = v;
                tris[t + 4] = v + 3;
                tris[t + 5] = v + 2;
                _uv[v] = new Vector2(0f, 1f);
                _uv[v + 1] = new Vector2(0f, 0f);
                _uv[v + 2] = new Vector2(1f, 0f);
                _uv[v + 3] = new Vector2(1f, 1f);
            }
            for (int i = 0; i < Players; i++)
            {
                _strip[i].Points = new Vector3[Segments + 1];
                _strip[i].Age = -1f;
                _verts[i] = new Vector3[Segments * 4];
                var mesh = new Mesh();
                mesh.name = "WallRibbon";
                mesh.vertices = _verts[i];
                mesh.uv = _uv;
                mesh.triangles = tris;
                _mesh[i] = mesh;
                var mat = new Material(shader);
                _mat[i] = mat;
                var quad = new GameObject("Ribbon");
                quad.transform.SetParent(transform, false);
                var filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var rend = quad.AddComponent<MeshRenderer>();
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
                rend.enabled = false;
                _rend[i] = rend;
            }
        }

        void Open(int seat, Vector3 point, Vector3 normal, Color color)
        {
            if (seat < 0 || seat >= Players) return;
            Strip strip = _strip[seat];
            strip.Count = 1;
            strip.Age = 0f;
            strip.Live = true;
            strip.Normal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.forward;
            strip.R = color.r;
            strip.G = color.g;
            strip.B = color.b;
            strip.Points[0] = point;
            _strip[seat] = strip;
            _mat[seat].color = color;
            if (_mat[seat].HasProperty("_Fade")) _mat[seat].SetFloat("_Fade", 0f);
            _rend[seat].enabled = false;
        }

        void Push(int seat, Vector3 point, Vector3 normal)
        {
            if (seat < 0 || seat >= Players) return;
            Strip strip = _strip[seat];
            if (!strip.Live || strip.Count < 1) return;
            if (normal.sqrMagnitude > 0.0001f) strip.Normal = normal.normalized;
            Vector3 last = strip.Points[strip.Count - 1];
            if ((point - last).sqrMagnitude < MinStep * MinStep)
            {
                _strip[seat] = strip;
                return;
            }
            if (strip.Count > Segments)
            {
                for (int i = 0; i < Segments; i++)
                    strip.Points[i] = strip.Points[i + 1];
                strip.Count = Segments;
            }
            strip.Points[strip.Count] = point;
            strip.Count++;
            _strip[seat] = strip;
            Write(seat, strip, 0f);
        }

        void Close(int seat)
        {
            if (seat < 0 || seat >= Players) return;
            Strip strip = _strip[seat];
            if (!strip.Live) return;
            strip.Live = false;
            strip.Age = 0.0001f;
            _strip[seat] = strip;
        }

        void Advance(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            for (int i = 0; i < Players; i++)
            {
                Strip strip = _strip[i];
                if (strip.Live || strip.Age < 0f) continue;
                strip.Age += dt;
                if (strip.Age >= Life || strip.Count < 2)
                {
                    strip.Age = -1f;
                    strip.Count = 0;
                    _rend[i].enabled = false;
                    _strip[i] = strip;
                    continue;
                }
                _strip[i] = strip;
                float fade = strip.Age / Life;
                if (_mat[i].HasProperty("_Fade")) _mat[i].SetFloat("_Fade", fade);
                Write(i, strip, fade);
            }
        }

        void Write(int seat, Strip strip, float fade)
        {
            if (strip.Count < 2)
            {
                _rend[seat].enabled = false;
                return;
            }
            int drop = (int)(fade * (strip.Count - 1));
            if (drop < 0) drop = 0;
            if (drop > strip.Count - 2) drop = strip.Count - 2;
            Vector3 n = strip.Normal;
            Vector3[] verts = _verts[seat];
            int q = 0;
            for (int s = drop; s < strip.Count - 1; s++)
            {
                Vector3 a = strip.Points[s];
                Vector3 b = strip.Points[s + 1];
                Vector3 dir = b - a;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;
                else dir.Normalize();
                Vector3 up = Vector3.Cross(n, dir);
                if (up.sqrMagnitude < 0.0001f) up = Vector3.up;
                else up.Normalize();
                Vector3 lift = n * 0.04f;
                float half = Width * 0.5f;
                int v = q * 4;
                verts[v] = a + up * half + lift;
                verts[v + 1] = a - up * half + lift;
                verts[v + 2] = b - up * half + lift;
                verts[v + 3] = b + up * half + lift;
                q++;
            }
            for (int s = q; s < Segments; s++)
            {
                int v = s * 4;
                Vector3 p = strip.Points[strip.Count - 1];
                verts[v] = p;
                verts[v + 1] = p;
                verts[v + 2] = p;
                verts[v + 3] = p;
            }
            _mesh[seat].vertices = verts;
            _mesh[seat].RecalculateBounds();
            _rend[seat].enabled = true;
        }
    }
}
