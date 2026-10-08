using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled comic words at the contact. The burst and the word are separate
    /// quads so the word can pop after the burst. Each split camera billboards
    /// them and shifts the quad in so the word stays inside that pane.
    /// Visual only.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public sealed class ComicBurst : MonoBehaviour
    {
        const int Slots = 8;
        const float RestSize = 1.65f;
        const float WordPeak = 1.15f;

        static ComicBurst _host;
        static uint _rng = 0xC0F1u;
        static string[] _lastWord;

        GameObject[] _root;
        Transform[] _burst;
        Transform[] _word;
        Material[] _burstMat;
        Material[] _wordMat;
        float[] _age;
        float[] _tilt;
        float[] _skew;
        float[] _arc;
        float[] _wide;
        float[] _tall;
        float[] _size;
        Texture2D _bursts;
        Texture2D _words;

        public static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("ComicBurstPool");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<ComicBurst>();
        }

        public static void Raise(Vector3 origin, Vector3 forward, float reach, bool tag)
        {
            Raise(origin, forward, reach, tag, 0f);
        }

        public static void Raise(Vector3 origin, Vector3 forward, float reach, bool tag, float speed)
        {
            int ev = tag ? ComicWords.EvTag : ComicWords.EvPunch;
            RaiseEvent(origin, forward, reach, ev, ComicWords.Strength(tag, speed));
        }

        public static void RaiseEvent(Vector3 origin, Vector3 forward, float reach, int ev, int strength)
        {
            if (!ComicWords.Visible(GameSettings.Current)) return;
            if (ev < 0 || ev >= ComicWords.EvCount) ev = ComicWords.EvPunch;
            Ensure();
            if (_host == null || _host._age == null) return;
            if (_lastWord == null)
            {
                _lastWord = new string[ComicWords.EvCount];
            }
            int pick = ComicWords.PickEvent(ref _rng, ev, strength, _lastWord[ev]);
            string text = ComicWords.PoolWord(ev, pick);
            _lastWord[ev] = text;
            int atlas = ComicWords.AtlasOf(ev, pick);
            _host.Spawn(origin, forward, reach, ev, atlas);
        }

        void Awake()
        {
            _host = this;
            _bursts = Load(ComicBurstAtlas.Png());
            _words = Load(ComicAtlas.Png());
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh quad = FullCell();
            _root = new GameObject[Slots];
            _burst = new Transform[Slots];
            _word = new Transform[Slots];
            _burstMat = new Material[Slots];
            _wordMat = new Material[Slots];
            _age = new float[Slots];
            _tilt = new float[Slots];
            _skew = new float[Slots];
            _arc = new float[Slots];
            _wide = new float[Slots];
            _tall = new float[Slots];
            _size = new float[Slots];
            float clamp = RestSize * WordPeak;
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var root = new GameObject("ComicWord");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                _root[i] = root;
                _burstMat[i] = MakeMat(shader, _bursts, 0f);
                _wordMat[i] = MakeMat(shader, _words, 0.0015f);
                _burstMat[i].SetFloat("_ClampExtent", clamp);
                _wordMat[i].SetFloat("_ClampExtent", clamp);
                _burst[i] = Child(root.transform, "Burst", quad, _burstMat[i]);
                _word[i] = Child(root.transform, "Word", quad, _wordMat[i]);
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
                float life = ComicWords.Scale(_age[i]);
                float burst = life * RestSize * _size[i];
                float word = life * ComicWords.WordPunch(_age[i]) * RestSize * _size[i];
                _burst[i].localScale = new Vector3(burst * _wide[i], burst * _tall[i], 1f);
                _word[i].localScale = new Vector3(word * _wide[i], word * _tall[i], 1f);
                _word[i].gameObject.SetActive(word > 0.001f);
                float a = ComicWords.Alpha(_age[i]);
                Paint(_burstMat[i], a);
                Paint(_wordMat[i], a);
                float tilt = _tilt[i] + ComicWords.Wobble(_age[i]);
                _burstMat[i].SetFloat("_Tilt", tilt);
                _wordMat[i].SetFloat("_Tilt", tilt);
                _burstMat[i].SetFloat("_Skew", _skew[i]);
                _wordMat[i].SetFloat("_Skew", _skew[i]);
                _burstMat[i].SetFloat("_Arc", _arc[i]);
                _wordMat[i].SetFloat("_Arc", _arc[i]);
            }
        }

        void Spawn(Vector3 origin, Vector3 forward, float reach, int ev, int atlas)
        {
            int slot = Free();
            if (slot < 0) slot = Oldest();
            float tiltDeg, skew, size, arc, wide, tall;
            ComicWords.StyleOf(atlas, out tiltDeg, out skew, out size, out arc, out wide, out tall);
            _tilt[slot] = tiltDeg * 0.017453292f;
            _skew[slot] = skew;
            _arc[slot] = arc;
            _size[slot] = size;
            _wide[slot] = wide;
            _tall[slot] = tall;
            _age[slot] = 0f;
            Vector3 contact = HitConfirmTell.Contact(origin, forward, reach);
            contact.y += 0.55f;
            _root[slot].transform.position = contact;
            _burst[slot].localScale = Vector3.zero;
            _word[slot].localScale = Vector3.zero;
            _word[slot].gameObject.SetActive(false);
            _root[slot].SetActive(true);
            Paint(_burstMat[slot], 1f);
            Paint(_wordMat[slot], 1f);
            _burstMat[slot].SetFloat("_Tilt", _tilt[slot]);
            _wordMat[slot].SetFloat("_Tilt", _tilt[slot]);
            _burstMat[slot].SetFloat("_Skew", _skew[slot]);
            _wordMat[slot].SetFloat("_Skew", _skew[slot]);
            _burstMat[slot].SetFloat("_Arc", _arc[slot]);
            _wordMat[slot].SetFloat("_Arc", _arc[slot]);
            ApplyUv(_burstMat[slot], ev, true);
            ApplyUv(_wordMat[slot], atlas, false);
        }

        static void ApplyUv(Material mat, int index, bool burst)
        {
            if (mat == null) return;
            float sx;
            float sy;
            float ox;
            float oy;
            if (burst) ComicBurstAtlas.Uv(index, out sx, out sy, out ox, out oy);
            else ComicAtlas.Uv(index, out sx, out sy, out ox, out oy);
            mat.mainTextureScale = new Vector2(sx, sy);
            mat.mainTextureOffset = new Vector2(ox, oy);
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

        static Material MakeMat(Shader shader, Texture2D tex, float front)
        {
            var mat = new Material(shader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Front")) mat.SetFloat("_Front", front);
            if (mat.HasProperty("_ClampExtent")) mat.SetFloat("_ClampExtent", 0f);
            return mat;
        }

        static Transform Child(Transform parent, string name, Mesh quad, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = quad;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return go.transform;
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

        static Texture2D Load(byte[] png)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(png);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }
    }
}
