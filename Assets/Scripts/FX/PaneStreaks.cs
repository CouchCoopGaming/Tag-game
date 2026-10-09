using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Seat-tinted streaks in the outer 12% of the owning camera.
    /// They start at sprint and die in 0.12 s. The Speed lines toggle gates them.
    /// Built once. Other panes do not draw this camera's streaks.
    /// </summary>
    public sealed class PaneStreaks : MonoBehaviour
    {
        public const int Count = 8;
        public const float Life = 0.12f;
        public const float Margin = 0.12f;

        const int Cameras = 4;

        struct Pane
        {
            public Camera Cam;
            public Transform[] Quad;
            public Renderer[] Rend;
            public Material[] Mat;
            public float[] Age;
        }

        static PaneStreaks _host;
        Pane[] _pane;
        Mesh _mesh;

        public static void Tick(Camera cam, float speed, float r, float g, float b, bool allow, float dt)
        {
            if (cam == null) return;
            Ensure();
            if (_host == null) return;
            _host.Step(cam, speed, r, g, b, allow, dt);
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
            _mesh = Quad();
            _pane = new Pane[Cameras];
            Shader shader = Shader.Find("Tag/FxMark");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            for (int c = 0; c < Cameras; c++)
            {
                _pane[c].Quad = new Transform[Count];
                _pane[c].Rend = new Renderer[Count];
                _pane[c].Mat = new Material[Count];
                _pane[c].Age = new float[Count];
                for (int i = 0; i < Count; i++)
                {
                    _pane[c].Age[i] = (i / (float)Count) * Life;
                    var mat = new Material(shader);
                    mat.SetFloat("_Mode", 0f);
                    _pane[c].Mat[i] = mat;
                    var quad = new GameObject("Streak");
                    quad.transform.SetParent(transform, false);
                    var filter = quad.AddComponent<MeshFilter>();
                    filter.sharedMesh = _mesh;
                    var rend = quad.AddComponent<MeshRenderer>();
                    rend.sharedMaterial = mat;
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows = false;
                    rend.enabled = false;
                    _pane[c].Quad[i] = quad.transform;
                    _pane[c].Rend[i] = rend;
                }
            }
        }

        void Step(Camera cam, float speed, float r, float g, float b, bool allow, float dt)
        {
            int slot = Slot(cam);
            if (slot < 0) return;
            Pane pane = _pane[slot];
            if (!allow || speed < DustLook.Sprint)
            {
                BlankSlot(pane);
                return;
            }
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            float d = cam.nearClipPlane + 0.22f;
            float halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfW = halfH * cam.aspect;
            float pace = speed / DustLook.Sprint;
            if (pace < 1f) pace = 1f;
            if (pace > 2f) pace = 2f;
            float len = halfH * (0.16f + (pace - 1f) * 0.14f);
            float thick = halfH * 0.028f;
            float xEdge = halfW * (1f - Margin * 0.5f);
            float yEdge = halfH * (1f - Margin * 0.5f);
            for (int i = 0; i < Count; i++)
            {
                pane.Age[i] += dt;
                if (pane.Age[i] > Life) pane.Age[i] -= Life;
                float u = pane.Age[i] / Life;
                float fade = 1f - u;
                int side = i & 3;
                float along = ((i >> 2) == 0 ? -0.42f : 0.42f) + (u - 0.5f) * 0.18f;
                Vector3 local;
                Vector3 scale;
                if (side == 0)
                {
                    local = new Vector3(-xEdge, halfH * along, d);
                    scale = new Vector3(thick, len, 1f);
                }
                else if (side == 1)
                {
                    local = new Vector3(xEdge, halfH * along, d);
                    scale = new Vector3(thick, len, 1f);
                }
                else if (side == 2)
                {
                    local = new Vector3(halfW * along, yEdge, d);
                    scale = new Vector3(len, thick, 1f);
                }
                else
                {
                    local = new Vector3(halfW * along, -yEdge, d);
                    scale = new Vector3(len, thick, 1f);
                }
                Transform quad = pane.Quad[i];
                quad.SetParent(cam.transform, false);
                quad.localPosition = local;
                quad.localRotation = Quaternion.identity;
                quad.localScale = scale;
                Color tint = new Color(r, g, b, 0.85f * fade);
                pane.Mat[i].SetColor("_BaseColor", tint);
                pane.Rend[i].enabled = true;
            }
            _pane[slot] = pane;
        }

        void Blank(Camera cam)
        {
            int slot = Slot(cam);
            if (slot < 0) return;
            BlankSlot(_pane[slot]);
        }

        static void BlankSlot(Pane pane)
        {
            if (pane.Rend == null) return;
            for (int i = 0; i < Count; i++)
                pane.Rend[i].enabled = false;
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

        static Mesh Quad()
        {
            var mesh = new Mesh();
            mesh.name = "PaneStreak";
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
    }
}
