using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Seat-tinted streaks in the outer 12% of the owning camera.
    /// Full effects draws 6, low draws 3, off and reduced flashing draw 0.
    /// GameSettings.EdgeStreaks defaults off. Speed lines stay off and do not set the count.
    /// One mesh per camera, life 0.12 s, peak alpha 0.55.
    /// </summary>
    public sealed class PaneStreaks : MonoBehaviour
    {
        public const int Full = 6;
        public const int Low = 3;
        public const float Life = 0.12f;
        public const float Margin = 0.12f;
        public const float Peak = 0.55f;
        public const float Outline = 0.90f;

        const int Cameras = 4;
        const int Quads = 2;
        const int Verts = Full * Quads * 4;

        static readonly int[] Show = { 0, 2, 4, 1, 3, 5 };
        static readonly Color Ink = new Color(0.08f, 0.07f, 0.06f, 1f);

        sealed class Pane
        {
            public Camera Cam;
            public Transform Root;
            public Mesh Mesh;
            public Renderer Rend;
            public Vector3[] Pos;
            public Color[] Col;
            public float[] Age;
        }

        static PaneStreaks _host;
        Pane[] _pane;
        readonly bool[] _on = new bool[Full];

        public static void Tick(Camera cam, float speed, float r, float g, float b, int count, float dt)
        {
            if (cam == null) return;
            Ensure();
            if (_host == null) return;
            _host.Step(cam, speed, r, g, b, count, dt);
        }

        public static void Hide(Camera cam)
        {
            if (_host == null || cam == null) return;
            _host.Blank(cam);
        }

        static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("PaneStreaks");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<PaneStreaks>();
        }

        void Awake()
        {
            _host = this;
            _pane = new Pane[Cameras];
            Shader shader = Shader.Find("Tag/FxMark");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            for (int c = 0; c < Cameras; c++)
            {
                var pane = new Pane();
                pane.Age = new float[Full];
                pane.Pos = new Vector3[Verts];
                pane.Col = new Color[Verts];
                for (int i = 0; i < Full; i++)
                    pane.Age[i] = (i / (float)Full) * Life;
                pane.Mesh = Build();
                var mat = new Material(shader);
                mat.SetFloat("_Mode", 0f);
                mat.SetFloat("_Vtx", 1f);
                var go = new GameObject("Streaks");
                go.transform.SetParent(transform, false);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = pane.Mesh;
                var rend = go.AddComponent<MeshRenderer>();
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
                rend.enabled = false;
                pane.Root = go.transform;
                pane.Rend = rend;
                _pane[c] = pane;
            }
        }

        void Step(Camera cam, float speed, float r, float g, float b, int count, float dt)
        {
            int slot = Slot(cam);
            if (slot < 0) return;
            Pane pane = _pane[slot];
            if (count < 0) count = 0;
            if (count > Full) count = Full;
            if (speed < DustLook.Sprint) count = 0;
            if (count == 0)
            {
                pane.Rend.enabled = false;
                return;
            }
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            float d = cam.nearClipPlane + 0.22f;
            float halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;
            float len = halfW * 2f * (48f / 640f);
            float thick = halfH * 2f * (4f / 360f);
            float rim = halfH * 2f * (1f / 360f);
            float xEdge = halfW * (1f - Margin * 0.5f);
            float yEdge = halfH * (1f - Margin * 0.5f);
            for (int i = 0; i < Full; i++)
                _on[i] = false;
            int n = count;
            if (n > Show.Length) n = Show.Length;
            for (int s = 0; s < n; s++)
                _on[Show[s]] = true;
            for (int i = 0; i < Full; i++)
            {
                pane.Age[i] += dt;
                if (pane.Age[i] > Life) pane.Age[i] -= Life;
                float fade = 1f - pane.Age[i] / Life;
                if (!_on[i]) fade = 0f;
                float along = i < 2 || (i >= 2 && i < 4) ? ((i & 1) == 0 ? 0.42f : -0.42f) : ((i == 4) ? 0.28f : -0.22f);
                bool vertical = i < 4;
                float cx;
                float cy;
                float ox;
                float oy;
                if (vertical)
                {
                    cx = (i < 2 ? -xEdge : xEdge);
                    cy = halfH * along;
                    ox = thick;
                    oy = len;
                }
                else
                {
                    cx = halfW * along;
                    cy = i == 4 ? yEdge : -yEdge;
                    ox = len;
                    oy = thick;
                }
                Color core = new Color(r, g, b, Peak * fade);
                Color edge = Ink;
                edge.a = Outline * fade;
                Write(pane, i * 8, cx, cy, d, ox, oy, edge);
                float ix = ox - rim * 2f;
                float iy = oy - rim * 2f;
                if (ix < rim) ix = rim;
                if (iy < rim) iy = rim;
                Write(pane, i * 8 + 4, cx, cy, d - 0.001f, ix, iy, core);
            }
            pane.Root.SetParent(cam.transform, false);
            pane.Root.localPosition = Vector3.zero;
            pane.Root.localRotation = Quaternion.identity;
            pane.Root.localScale = Vector3.one;
            pane.Mesh.vertices = pane.Pos;
            pane.Mesh.colors = pane.Col;
            pane.Rend.enabled = true;
        }

        static void Write(Pane pane, int v, float cx, float cy, float z, float sx, float sy, Color color)
        {
            float hx = sx * 0.5f;
            float hy = sy * 0.5f;
            pane.Pos[v] = new Vector3(cx - hx, cy - hy, z);
            pane.Pos[v + 1] = new Vector3(cx + hx, cy - hy, z);
            pane.Pos[v + 2] = new Vector3(cx + hx, cy + hy, z);
            pane.Pos[v + 3] = new Vector3(cx - hx, cy + hy, z);
            pane.Col[v] = color;
            pane.Col[v + 1] = color;
            pane.Col[v + 2] = color;
            pane.Col[v + 3] = color;
        }

        void Blank(Camera cam)
        {
            int slot = -1;
            for (int i = 0; i < Cameras; i++)
            {
                if (_pane[i].Cam == cam) slot = i;
            }
            if (slot < 0) return;
            _pane[slot].Rend.enabled = false;
        }

        int Slot(Camera cam)
        {
            int free = -1;
            for (int i = 0; i < Cameras; i++)
            {
                if (_pane[i].Cam == cam) return i;
                if (free < 0 && _pane[i].Cam == null) free = i;
            }
            if (free < 0) return -1;
            _pane[free].Cam = cam;
            return free;
        }

        static Mesh Build()
        {
            var mesh = new Mesh();
            mesh.name = "PaneStreaks";
            mesh.MarkDynamic();
            var pos = new Vector3[Verts];
            var uv = new Vector2[Verts];
            var col = new Color[Verts];
            var tris = new int[Full * Quads * 6];
            for (int q = 0; q < Full * Quads; q++)
            {
                int v = q * 4;
                uv[v] = new Vector2(0f, 0f);
                uv[v + 1] = new Vector2(1f, 0f);
                uv[v + 2] = new Vector2(1f, 1f);
                uv[v + 3] = new Vector2(0f, 1f);
                int t = q * 6;
                tris[t] = v;
                tris[t + 1] = v + 2;
                tris[t + 2] = v + 1;
                tris[t + 3] = v;
                tris[t + 4] = v + 3;
                tris[t + 5] = v + 2;
            }
            mesh.vertices = pos;
            mesh.uv = uv;
            mesh.colors = col;
            mesh.triangles = tris;
            return mesh;
        }
    }
}
