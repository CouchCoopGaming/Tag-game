using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Steady foam stroke around another runner when that body covers the owner
    /// in this camera only. 2 px on a 360-tall pane, 3 px on a 540-tall pane.
    /// The dark outline is alpha 0.90. Off until bodyStroke is on.
    /// Not one of the 21 rows, and not one of the seven FX-kit toggles.
    /// Runs after the kit so a shell that hides this frame yields the stroke the same frame.
    /// </summary>
    [DefaultExecutionOrder(160)]
    public sealed class BodyStroke : MonoBehaviour
    {
        public const int Bodies = 8;
        public const int Cameras = 4;
        public const int Strokes = 3;
        public const float FoamPx = 2f;
        public const float InkPx = 1f;
        public const float RefHeight = 360f;

        public static readonly Camera[] Cam = new Camera[Bodies];
        public static readonly Transform[] Body = new Transform[Bodies];
        public static readonly int[] Seat = new int[Bodies];
        public static readonly bool[] Shell = new bool[Bodies];
        public static int Count;

        public static void NoteShell(int seat, bool up)
        {
            if (seat < 0 || seat >= Bodies) return;
            Shell[seat] = up;
        }

        const int Quads = 8;
        const int Verts = Strokes * Quads * 4;

        static readonly Color Ink = new Color(VerbFxLook.InkR, VerbFxLook.InkG, VerbFxLook.InkB, 1f);

        sealed class Pane
        {
            public Camera Cam;
            public Transform Root;
            public Mesh Mesh;
            public Renderer Rend;
            public Vector3[] Pos;
            public Color[] Col;
        }

        static BodyStroke _host;
        Pane[] _pane;
        readonly bool[] _used = new bool[Cameras];

        public static void Note(Camera cam, Transform body, int seat)
        {
            if (body == null) return;
            Ensure();
            int slot = -1;
            for (int i = 0; i < Count; i++)
            {
                if (Body[i] == body)
                {
                    slot = i;
                    break;
                }
            }
            if (slot < 0)
            {
                if (Count >= Bodies) return;
                slot = Count;
                Count++;
            }
            Body[slot] = body;
            Seat[slot] = seat;
            if (cam != null) Cam[slot] = cam;
        }

        static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("BodyStroke");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<BodyStroke>();
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
                pane.Pos = new Vector3[Verts];
                pane.Col = new Color[Verts];
                pane.Mesh = Build();
                var mat = new Material(shader);
                mat.SetFloat("_Mode", 0f);
                mat.SetFloat("_Vtx", 1f);
                var go = new GameObject("Stroke");
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

        void LateUpdate()
        {
            if (_pane == null) return;
            GameSettings settings = GameSettings.Current;
            bool on = settings != null && settings.BodyStroke && FxAmount.Show(settings);
            if (!on)
            {
                HideAll();
                return;
            }
            for (int i = 0; i < Cameras; i++)
                _used[i] = false;
            for (int c = 0; c < Count; c++)
            {
                Camera cam = Cam[c];
                if (!First(c)) continue;
                int pane = Slot(cam);
                if (pane < 0) continue;
                _used[pane] = true;
                Transform owner = Body[c];
                int n = 0;
                for (int o = 0; o < Count; o++)
                {
                    if (o == c || Body[o] == null) continue;
                    if (!Covered(cam, owner, Body[o], out float x0, out float y0, out float x1, out float y1))
                        continue;
                    int otherSeat = Seat[o];
                    if (settings.StrokeYield && otherSeat >= 0 && otherSeat < Bodies && Shell[otherSeat])
                        continue;
                    if (n >= Strokes) break;
                    Paint(_pane[pane], n, cam, x0, y0, x1, y1, Seat[o]);
                    n++;
                }
                for (int s = n; s < Strokes; s++)
                    Blank(_pane[pane], s);
                Pane view = _pane[pane];
                view.Rend.enabled = n > 0;
                if (n <= 0) continue;
                view.Root.SetParent(cam.transform, false);
                view.Root.localPosition = Vector3.zero;
                view.Root.localRotation = Quaternion.identity;
                view.Root.localScale = Vector3.one;
                view.Mesh.vertices = view.Pos;
                view.Mesh.colors = view.Col;
            }
            for (int i = 0; i < Cameras; i++)
            {
                if (!_used[i] && _pane[i].Rend != null)
                    _pane[i].Rend.enabled = false;
            }
        }

        static bool First(int c)
        {
            Camera cam = Cam[c];
            if (cam == null || !cam.enabled || Body[c] == null) return false;
            for (int i = 0; i < c; i++)
            {
                if (Cam[i] == cam) return false;
            }
            return true;
        }

        static bool Covered(Camera cam, Transform owner, Transform other, out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = x1 = y1 = 0f;
            if (owner == null || other == null) return false;
            if (!RectOf(cam, owner, out float ax0, out float ay0, out float ax1, out float ay1)) return false;
            if (!RectOf(cam, other, out x0, out y0, out x1, out y1)) return false;
            return ax0 < x1 && ax1 > x0 && ay0 < y1 && ay1 > y0;
        }

        static bool RectOf(Camera cam, Transform body, out float x0, out float y0, out float x1, out float y1)
        {
            x0 = y0 = x1 = y1 = 0f;
            Vector3 p = body.position;
            Vector3 right = cam.transform.right * 0.38f;
            Vector3 up = Vector3.up * 1.8f;
            bool any = false;
            Corner(cam, p - right, ref any, ref x0, ref y0, ref x1, ref y1);
            Corner(cam, p + right, ref any, ref x0, ref y0, ref x1, ref y1);
            Corner(cam, p + up - right, ref any, ref x0, ref y0, ref x1, ref y1);
            Corner(cam, p + up + right, ref any, ref x0, ref y0, ref x1, ref y1);
            return any;
        }

        static void Corner(Camera cam, Vector3 world, ref bool any, ref float x0, ref float y0, ref float x1, ref float y1)
        {
            Vector3 vp = cam.WorldToViewportPoint(world);
            if (vp.z <= 0f) return;
            if (!any)
            {
                x0 = x1 = vp.x;
                y0 = y1 = vp.y;
                any = true;
                return;
            }
            if (vp.x < x0) x0 = vp.x;
            if (vp.y < y0) y0 = vp.y;
            if (vp.x > x1) x1 = vp.x;
            if (vp.y > y1) y1 = vp.y;
        }

        void Paint(Pane pane, int stroke, Camera cam, float x0, float y0, float x1, float y1, int seat)
        {
            float d = cam.nearClipPlane + 0.22f;
            float halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;
            float foam = halfH * (2f * FoamPx / RefHeight);
            float ink = halfH * (2f * InkPx / RefHeight);
            float vx = (x0 + x1) * 0.5f;
            float vy = (y0 + y1) * 0.5f;
            float hw = (x1 - x0) * halfW;
            float hh = (y1 - y0) * halfH;
            float cx = (vx * 2f - 1f) * halfW;
            float cy = (vy * 2f - 1f) * halfH;
            BodyFoam.Rgb rgb = BodyFoam.ForSeat(seat);
            Color core = new Color(rgb.R, rgb.G, rgb.B, 1f);
            Color edge = Ink;
            edge.a = VerbFxLook.InkA;
            int v = stroke * Quads * 4;
            Frame(pane, v, cx, cy, d, hw + ink, hh + ink, ink, edge);
            Frame(pane, v + 16, cx, cy, d - 0.001f, hw, hh, foam, core);
        }

        static void Frame(Pane pane, int v, float cx, float cy, float z, float hw, float hh, float thick, Color color)
        {
            if (thick < 0.0001f) thick = 0.0001f;
            if (hw < thick) hw = thick;
            if (hh < thick) hh = thick;
            Quad(pane, v, cx, cy - hh + thick * 0.5f, z, hw * 2f, thick, color);
            Quad(pane, v + 4, cx, cy + hh - thick * 0.5f, z, hw * 2f, thick, color);
            float inner = (hh - thick) * 2f;
            if (inner < 0f) inner = 0f;
            Quad(pane, v + 8, cx - hw + thick * 0.5f, cy, z, thick, inner, color);
            Quad(pane, v + 12, cx + hw - thick * 0.5f, cy, z, thick, inner, color);
        }

        static void Quad(Pane pane, int v, float cx, float cy, float z, float sx, float sy, Color color)
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

        static void Blank(Pane pane, int stroke)
        {
            int v = stroke * Quads * 4;
            int n = Quads * 4;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int i = 0; i < n; i++)
                pane.Col[v + i] = clear;
        }

        void HideAll()
        {
            if (_pane == null) return;
            for (int i = 0; i < Cameras; i++)
            {
                if (_pane[i] != null && _pane[i].Rend != null)
                    _pane[i].Rend.enabled = false;
            }
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
            mesh.name = "BodyStroke";
            mesh.MarkDynamic();
            var pos = new Vector3[Verts];
            var uv = new Vector2[Verts];
            var col = new Color[Verts];
            var tris = new int[Strokes * Quads * 6];
            for (int q = 0; q < Strokes * Quads; q++)
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
