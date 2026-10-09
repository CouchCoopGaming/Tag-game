using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// One card per pawn on a punch, tag, hard land, or wall slam.
    /// 0.08 s. Alpha 0.72 while comic words are on, 0.88 while they are off.
    /// A whiff does not get a card. A second hit restarts the same quad.
    /// </summary>
    public sealed class InkCard : MonoBehaviour
    {
        public const int Slots = 4;
        public const float Life = 0.08f;
        public const float PeakOn = 0.72f;
        public const float PeakOff = 0.88f;
        public const float WideFrac = 0.42f;
        public const float TallFrac = 0.28f;

        static InkCard _host;
        static bool _allow;
        static int _frame = -1;

        Transform[] _card;
        Material[] _mat;
        float[] _age;
        float[] _peak;
        int[] _owner;

        public static void Allow(bool allow)
        {
            _allow = allow;
            if (!allow && _host != null) _host.HideAll();
        }

        public static void Pop(Vector3 origin, int owner)
        {
            if (!_allow) return;
            Ensure();
            if (_host == null) return;
            bool comic = GameSettings.Current != null && GameSettings.Current.ComicWords;
            _host.Spawn(origin, owner, comic ? PeakOn : PeakOff);
        }

        public static void Fit(int owner, Camera cam)
        {
            if (_host == null || cam == null) return;
            _host.FitOwner(owner, cam);
        }

        public static void Tick(float dt)
        {
            if (_host == null) return;
            int frame = Time.frameCount;
            if (frame == _frame) return;
            _frame = frame;
            _host.Advance(dt);
        }

        static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("InkCard");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<InkCard>();
        }

        void Awake()
        {
            _host = this;
            _card = new Transform[Slots];
            _mat = new Material[Slots];
            _age = new float[Slots];
            _peak = new float[Slots];
            _owner = new int[Slots];
            Shader shader = Shader.Find("Tag/FxMark");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Mesh mesh = Quad();
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                _owner[i] = int.MinValue;
                var mat = new Material(shader);
                mat.SetFloat("_Mode", 2f);
                mat.SetFloat("_Rim", 0.82f);
                mat.SetFloat("_Front", 0f);
                mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, PeakOn));
                _mat[i] = mat;
                var quad = new GameObject("Card");
                quad.transform.SetParent(transform, false);
                var filter = quad.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var rend = quad.AddComponent<MeshRenderer>();
                rend.sharedMaterial = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
                quad.SetActive(false);
                _card[i] = quad.transform;
            }
        }

        void Spawn(Vector3 origin, int owner, float peak)
        {
            int slot = -1;
            for (int i = 0; i < Slots; i++)
            {
                if (_owner[i] == owner)
                {
                    slot = i;
                    break;
                }
            }
            if (slot < 0)
            {
                float oldest = -1f;
                for (int i = 0; i < Slots; i++)
                {
                    if (_age[i] < 0f)
                    {
                        slot = i;
                        oldest = -1f;
                        break;
                    }
                    if (_age[i] > oldest)
                    {
                        oldest = _age[i];
                        slot = i;
                    }
                }
            }
            _owner[slot] = owner;
            _peak[slot] = peak;
            _age[slot] = 0.0001f;
            _card[slot].position = origin + Vector3.up * 0.95f;
            _card[slot].rotation = Quaternion.identity;
            _mat[slot].SetColor("_BaseColor", new Color(1f, 1f, 1f, peak));
            _card[slot].gameObject.SetActive(true);
        }

        void FitOwner(int owner, Camera cam)
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_owner[i] != owner || _age[i] < 0f) continue;
                Vector3 at = _card[i].position;
                float depth = Vector3.Dot(at - cam.transform.position, cam.transform.forward);
                if (depth < 0.2f) depth = 0.2f;
                float halfH = depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float halfW = halfH * cam.aspect;
                _card[i].localScale = new Vector3(halfW * 2f * WideFrac, halfH * 2f * TallFrac, 1f);
            }
        }

        void Advance(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (dt > 0.05f) dt = 0.05f;
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) continue;
                _age[i] += dt;
                if (!_allow || _age[i] >= Life)
                {
                    _age[i] = -1f;
                    _card[i].gameObject.SetActive(false);
                    continue;
                }
                float a = _peak[i] * (1f - _age[i] / Life);
                _mat[i].SetColor("_BaseColor", new Color(1f, 1f, 1f, a));
            }
        }

        void HideAll()
        {
            if (_age == null) return;
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                if (_card[i] != null) _card[i].gameObject.SetActive(false);
            }
        }

        static Mesh Quad()
        {
            var mesh = new Mesh();
            mesh.name = "InkCard";
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
