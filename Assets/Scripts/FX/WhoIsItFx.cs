using UnityEngine;
using UnityEngine.Rendering;

namespace Tag.FX
{
    /// <summary>
    /// Off-screen crown wedge, and a flat crown plate above the hat.
    /// The plate is hidden in the It's own camera. The HUD chip is not drawn here.
    /// The 0.40 s swell stays on Pass5Host. Time does not freeze.
    /// </summary>
    public sealed class WhoIsItFx : MonoBehaviour
    {
        const int Cameras = 4;
        const float HatHeight = 2.12f;
        const float PlateHeight = 2.48f;
        const float PlateSize = 0.32f;

        static WhoIsItFx _host;

        Transform _itBody;
        bool _itOn;
        float _cr, _cg, _cb;
        float _sr, _sg, _sb;

        Transform _plate;
        Transform _plateBack;
        Renderer _plateRend;
        Renderer _backRend;
        Material _plateMat;
        Material _backMat;
        bool _plateWant;
        bool _hooked;

        Camera[] _cam = new Camera[Cameras];
        Transform[] _star = new Transform[Cameras];
        Transform[] _pip = new Transform[Cameras];
        Renderer[] _starRend = new Renderer[Cameras];
        Renderer[] _pipRend = new Renderer[Cameras];
        Material[] _starMat = new Material[Cameras];
        Material[] _pipMat = new Material[Cameras];

        public static void Note(Transform body, bool isIt, float crownR, float crownG, float crownB, float seatR, float seatG, float seatB)
        {
            Ensure();
            if (_host == null) return;
            if (isIt && body != null)
            {
                _host._itBody = body;
                _host._itOn = true;
                _host._cr = crownR;
                _host._cg = crownG;
                _host._cb = crownB;
                _host._sr = seatR;
                _host._sg = seatG;
                _host._sb = seatB;
                return;
            }
            if (_host._itBody == body)
                _host._itOn = false;
        }

        public static void TickPlate(Transform body, bool isIt, bool allow)
        {
            Ensure();
            if (_host == null) return;
            _host.PlacePlate(body, isIt, allow);
        }

        public static void TickWedge(Camera cam, Transform owner, bool ownerIsIt, bool allow)
        {
            if (cam == null) return;
            Ensure();
            if (_host == null) return;
            _host.PlaceWedge(cam, owner, ownerIsIt, allow);
        }

        public static void Hide(Camera cam)
        {
            if (_host == null) return;
            _host.BlankWedge(cam);
            if (cam == null) return;
            _host._plateWant = false;
            _host.ApplyPlate();
        }

        static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("WhoIsItFx");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<WhoIsItFx>();
        }

        void Awake()
        {
            _host = this;
            Shader shader = Shader.Find("Tag/FxMark");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Mesh star = Star();
            Mesh quad = Quad();
            _backMat = new Material(shader);
            _backMat.SetFloat("_Mode", 1f);
            _backMat.SetColor("_BaseColor", new Color(0.06f, 0.05f, 0.04f, 1f));
            _plateMat = new Material(shader);
            _plateMat.SetFloat("_Mode", 1f);
            _plateBack = Make("CrownBack", quad, _backMat, out _backRend);
            _plate = Make("CrownPlate", star, _plateMat, out _plateRend);
            _plateBack.localScale = new Vector3(PlateSize * 1.28f, PlateSize * 1.28f, 1f);
            _plate.localScale = new Vector3(PlateSize, PlateSize, 1f);
            for (int i = 0; i < Cameras; i++)
            {
                _starMat[i] = new Material(shader);
                _starMat[i].SetFloat("_Mode", 0f);
                _pipMat[i] = new Material(shader);
                _pipMat[i].SetFloat("_Mode", 0f);
                _star[i] = Make("ItWedge", star, _starMat[i], out _starRend[i]);
                _pip[i] = Make("ItPip", quad, _pipMat[i], out _pipRend[i]);
                _starRend[i].enabled = false;
                _pipRend[i].enabled = false;
            }
            _plateRend.enabled = false;
            _backRend.enabled = false;
            RenderPipelineManager.beginCameraRendering -= OnCamera;
            RenderPipelineManager.beginCameraRendering += OnCamera;
            _hooked = true;
        }

        void OnDestroy()
        {
            if (_hooked)
                RenderPipelineManager.beginCameraRendering -= OnCamera;
        }

        static void OnCamera(ScriptableRenderContext context, Camera cam)
        {
            if (_host == null) return;
            bool own = _host._itBody != null && cam != null && cam.transform.IsChildOf(_host._itBody);
            bool show = _host._plateWant && !own;
            if (_host._plateRend != null) _host._plateRend.enabled = show;
            if (_host._backRend != null) _host._backRend.enabled = show;
        }

        void PlacePlate(Transform body, bool isIt, bool allow)
        {
            if (isIt && allow && body != null)
            {
                _plateWant = true;
                Vector3 at = body.position + Vector3.up * PlateHeight;
                _plate.position = at;
                _plateBack.position = at;
                _plate.rotation = Quaternion.identity;
                _plateBack.rotation = Quaternion.identity;
                _plateMat.SetColor("_BaseColor", new Color(_cr, _cg, _cb, 1f));
                ApplyPlate();
                return;
            }
            if (_itBody == body || body == null)
            {
                _plateWant = false;
                ApplyPlate();
            }
        }

        void ApplyPlate()
        {
            if (_plateRend != null) _plateRend.enabled = _plateWant;
            if (_backRend != null) _backRend.enabled = _plateWant;
        }

        void PlaceWedge(Camera cam, Transform owner, bool ownerIsIt, bool allow)
        {
            int slot = Slot(cam);
            if (slot < 0) return;
            bool show = allow && _itOn && _itBody != null && !ownerIsIt;
            if (show)
            {
                Vector3 hat = _itBody.position + Vector3.up * HatHeight;
                Vector3 vp = cam.WorldToViewportPoint(hat);
                bool inside = vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
                if (inside) show = false;
                else
                {
                    float x = vp.x;
                    float y = vp.y;
                    if (vp.z < 0f)
                    {
                        x = 1f - x;
                        y = 1f - y;
                    }
                    float ox = x < 0f ? -x : (x > 1f ? x - 1f : 0f);
                    float oy = y < 0f ? -y : (y > 1f ? y - 1f : 0f);
                    float nx;
                    float ny;
                    if (ox >= oy)
                    {
                        nx = x < 0.5f ? 0.04f : 0.96f;
                        ny = y < 0.15f ? 0.15f : (y > 0.85f ? 0.85f : y);
                    }
                    else
                    {
                        ny = y < 0.5f ? 0.06f : 0.94f;
                        nx = x < 0.15f ? 0.15f : (x > 0.85f ? 0.85f : x);
                    }
                    float depth = cam.nearClipPlane + 0.28f;
                    Vector3 world = cam.ViewportToWorldPoint(new Vector3(nx, ny, depth));
                    _star[slot].SetParent(cam.transform, true);
                    _pip[slot].SetParent(cam.transform, true);
                    _star[slot].position = world;
                    _star[slot].rotation = cam.transform.rotation;
                    float size = depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.16f;
                    _star[slot].localScale = new Vector3(size, size, 1f);
                    Vector3 pip = world;
                    Vector3 right = cam.transform.right;
                    pip += right * size * 0.85f;
                    _pip[slot].position = pip;
                    _pip[slot].rotation = cam.transform.rotation;
                    _pip[slot].localScale = new Vector3(size * 0.28f, size * 0.28f, 1f);
                    _starMat[slot].SetColor("_BaseColor", new Color(_cr, _cg, _cb, 1f));
                    _pipMat[slot].SetColor("_BaseColor", new Color(_sr, _sg, _sb, 1f));
                }
            }
            _starRend[slot].enabled = show;
            _pipRend[slot].enabled = show;
            if (owner == null) return;
        }

        void BlankWedge(Camera cam)
        {
            int slot = -1;
            for (int i = 0; i < Cameras; i++)
            {
                if (_cam[i] == cam) slot = i;
            }
            if (slot < 0) return;
            _starRend[slot].enabled = false;
            _pipRend[slot].enabled = false;
        }

        int Slot(Camera cam)
        {
            int free = -1;
            for (int i = 0; i < Cameras; i++)
            {
                if (_cam[i] == cam) return i;
                if (free < 0 && _cam[i] == null) free = i;
            }
            if (free < 0) return -1;
            _cam[free] = cam;
            return free;
        }

        Transform Make(string name, Mesh mesh, Material mat, out Renderer rend)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go.transform;
        }

        static Mesh Quad()
        {
            var mesh = new Mesh();
            mesh.name = "WhoQuad";
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

        static Mesh Star()
        {
            var verts = new Vector3[11];
            var uv = new Vector2[11];
            var tris = new int[30];
            verts[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < 10; i++)
            {
                float a = (i * 36f - 90f) * Mathf.Deg2Rad;
                float rad = (i & 1) == 0 ? 0.5f : 0.2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0f);
                uv[i + 1] = new Vector2(verts[i + 1].x + 0.5f, verts[i + 1].y + 0.5f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = 1 + i;
                tris[i * 3 + 2] = 1 + ((i + 1) % 10);
            }
            var mesh = new Mesh();
            mesh.name = "WhoStar";
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = tris;
            return mesh;
        }
    }
}
