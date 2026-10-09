using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// One body-sized black card on a punch or tag hit. A few frames, then gone.
    /// The comic word stays. Time does not freeze.
    /// </summary>
    public sealed class InkCard : MonoBehaviour
    {
        public const int Slots = 4;
        public const float Life = 0.07f;
        public const float Wide = 1.70f;
        public const float Tall = 1.90f;

        static InkCard _host;
        static bool _allow;
        static int _frame = -1;

        Transform[] _card;
        float[] _age;

        public static void Allow(bool allow)
        {
            _allow = allow;
            if (!allow && _host != null) _host.HideAll();
        }

        public static void Pop(Vector3 origin)
        {
            if (!_allow) return;
            Ensure();
            if (_host == null) return;
            _host.Spawn(origin);
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
            _age = new float[Slots];
            Shader shader = Shader.Find("Tag/FxMark");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Mesh mesh = Quad();
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var mat = new Material(shader);
                mat.SetFloat("_Mode", 2f);
                mat.SetFloat("_Rim", 0.82f);
                mat.SetFloat("_Front", 0.35f);
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

        void Spawn(Vector3 origin)
        {
            int slot = 0;
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
            _age[slot] = 0.0001f;
            _card[slot].position = origin + Vector3.up * 0.95f;
            _card[slot].rotation = Quaternion.identity;
            _card[slot].localScale = new Vector3(Wide, Tall, 1f);
            _card[slot].gameObject.SetActive(true);
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
                }
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
