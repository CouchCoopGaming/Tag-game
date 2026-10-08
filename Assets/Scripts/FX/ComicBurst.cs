using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled comic words at the contact. Each split camera billboards them.
    /// The burst, halftone, and letters are one high-res cell. Visual only.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public sealed class ComicBurst : MonoBehaviour
    {
        const int Slots = 8;
        const float RestSize = 1.65f;

        static ComicBurst _host;
        static uint _rng = 0xC0F1u;
        static int _last = -1;

        GameObject[] _root;
        Material[] _mat;
        float[] _age;
        float[] _tilt;
        Texture2D _atlas;

        public static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("ComicBurstPool");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<ComicBurst>();
        }

        public static void Raise(Vector3 origin, Vector3 forward, float reach, bool tag)
        {
            if (!ComicWords.Visible(GameSettings.Current)) return;
            Ensure();
            if (_host == null || _host._age == null) return;
            _host.Spawn(origin, forward, reach, tag);
        }

        void Awake()
        {
            _host = this;
            _atlas = LoadAtlas();
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh quad = FullCell();
            _root = new GameObject[Slots];
            _mat = new Material[Slots];
            _age = new float[Slots];
            _tilt = new float[Slots];
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var root = new GameObject("ComicWord");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                _root[i] = root;
                _mat[i] = MakeMat(shader, _atlas);
                var go = new GameObject("Cell");
                go.transform.SetParent(root.transform, false);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = quad;
                var rend = go.AddComponent<MeshRenderer>();
                rend.sharedMaterial = _mat[i];
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        void LateUpdate()
        {
            if (_age == null) return;
            bool show = ComicWords.Visible(GameSettings.Current);
            float dt = Time.deltaTime;
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) continue;
                _age[i] += dt;
                if (!show || _age[i] >= ComicWords.LifeSeconds)
                {
                    _age[i] = -1f;
                    _root[i].SetActive(false);
                    continue;
                }
                float s = ComicWords.Scale(_age[i]) * RestSize;
                _root[i].transform.localScale = new Vector3(s, s, 1f);
                float a = ComicWords.Alpha(_age[i]);
                Paint(_mat[i], a);
                _mat[i].SetFloat("_Tilt", _tilt[i] + ComicWords.Wobble(_age[i]));
            }
        }

        void Spawn(Vector3 origin, Vector3 forward, float reach, bool tag)
        {
            int slot = Free();
            if (slot < 0) slot = Oldest();
            int word = ComicWords.Pick(ref _rng, _last, tag);
            _last = word;
            _rng = _rng * 1664525u + 1013904223u;
            _tilt[slot] = ComicWords.TiltRadians(_rng >> 8);
            _age[slot] = 0f;
            Vector3 contact = HitConfirmTell.Contact(origin, forward, reach);
            contact.y += 0.55f;
            _root[slot].transform.position = contact;
            _root[slot].transform.localScale = Vector3.zero;
            _root[slot].SetActive(true);
            Paint(_mat[slot], 1f);
            _mat[slot].SetFloat("_Tilt", _tilt[slot]);
            // One texel of gutter so a cell edge never samples the next word.
            const float gutter = 2f / 4096f;
            _mat[slot].mainTextureScale = new Vector2(0.25f - gutter * 2f, 1f);
            _mat[slot].mainTextureOffset = new Vector2(word * 0.25f + gutter, 0f);
        }

        int Free()
        {
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) return i;
            }
            return -1;
        }

        int Oldest()
        {
            int best = 0;
            float age = -1f;
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] > age)
                {
                    age = _age[i];
                    best = i;
                }
            }
            return best;
        }

        static void Paint(Material mat, float a)
        {
            if (mat == null) return;
            mat.color = new Color(1f, 1f, 1f, a);
        }

        static Material MakeMat(Shader shader, Texture2D tex)
        {
            var mat = new Material(shader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            mat.mainTextureScale = new Vector2(0.25f, 1f);
            return mat;
        }

        static Mesh FullCell()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(-1f, 1f, 0f)
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

        static Texture2D LoadAtlas()
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(ComicAtlas.Png());
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
