using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled comic words at the contact. Each split camera billboards them.
    /// No hitstop, no shake, no change to punch reach or tag timing.
    /// </summary>
    [DefaultExecutionOrder(9000)]
    public sealed class ComicBurst : MonoBehaviour
    {
        const int Slots = 8;
        const int Atlas = 256;

        static ComicBurst _host;
        static uint _rng = 0xC0F1u;
        static int _last = -1;

        GameObject[] _root;
        Material[] _starMat;
        Material[] _wordMat;
        float[] _age;
        Texture2D _atlas;
        Texture2D _white;

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
            _white = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            _white.Apply(false, true);
            _atlas = BuildAtlas();
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _root = new GameObject[Slots];
            _starMat = new Material[Slots];
            _wordMat = new Material[Slots];
            _age = new float[Slots];
            Mesh star = StarMesh();
            Mesh word = WordMesh();
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var root = new GameObject("ComicWord");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                _root[i] = root;
                _starMat[i] = MakeMat(shader, _white);
                _wordMat[i] = MakeMat(shader, _atlas);
                AddQuad(root.transform, "Burst", star, _starMat[i], 0.02f);
                AddQuad(root.transform, "Letters", word, _wordMat[i], 0f);
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
                float s = ComicWords.Scale(_age[i]) * 1.35f;
                _root[i].transform.localScale = new Vector3(s, s, 1f);
                float a = ComicWords.Alpha(_age[i]);
                Color star = _starMat[i].color;
                star.a = a;
                _starMat[i].color = star;
                Color word = _wordMat[i].color;
                word.a = a;
                _wordMat[i].color = word;
            }
        }

        void Spawn(Vector3 origin, Vector3 forward, float reach, bool tag)
        {
            int slot = Free();
            if (slot < 0) slot = Oldest();
            int word = ComicWords.Pick(ref _rng, _last, tag);
            _last = word;
            _rng = _rng * 1664525u + 1013904223u;
            float tilt = (((_rng >> 8) & 255u) / 255f) * 0.34f - 0.17f;
            _age[slot] = 0f;
            Vector3 contact = HitConfirmTell.Contact(origin, forward, reach);
            contact.y += 0.35f;
            _root[slot].transform.position = contact;
            _root[slot].transform.localScale = new Vector3(0.2f, 0.2f, 1f);
            _root[slot].SetActive(true);
            ComicWords.ColorOf(word, out float r, out float g, out float b);
            _starMat[slot].color = new Color(r, g, b, 1f);
            _starMat[slot].SetFloat("_Tilt", tilt);
            _wordMat[slot].color = Color.white;
            _wordMat[slot].SetFloat("_Tilt", tilt);
            _wordMat[slot].mainTextureScale = new Vector2(0.25f, 1f);
            _wordMat[slot].mainTextureOffset = new Vector2(word * 0.25f, 0f);
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

        static Material MakeMat(Shader shader, Texture2D tex)
        {
            var mat = new Material(shader);
            mat.mainTexture = tex;
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            return mat;
        }

        static void AddQuad(Transform parent, string name, Mesh mesh, Material mat, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, z);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
        }

        static Mesh StarMesh()
        {
            const int spikes = 12;
            var verts = new Vector3[spikes * 2 + 1];
            var uv = new Vector2[verts.Length];
            var tris = new int[spikes * 6];
            verts[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < spikes; i++)
            {
                float a0 = i / (float)spikes * 6.2831855f;
                float a1 = (i + 0.5f) / spikes * 6.2831855f;
                verts[1 + i * 2] = new Vector3(Mathf.Cos(a0) * 0.72f, Mathf.Sin(a0) * 0.72f, 0f);
                verts[2 + i * 2] = new Vector3(Mathf.Cos(a1) * 1.15f, Mathf.Sin(a1) * 1.15f, 0f);
                uv[1 + i * 2] = new Vector2(0.5f, 0.5f);
                uv[2 + i * 2] = new Vector2(0.5f, 0.5f);
                int t = i * 6;
                int tip = 2 + i * 2;
                int next = 1 + ((i + 1) % spikes) * 2;
                tris[t] = 0;
                tris[t + 1] = 1 + i * 2;
                tris[t + 2] = tip;
                tris[t + 3] = 0;
                tris[t + 4] = tip;
                tris[t + 5] = next;
            }
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.triangles = tris;
            return mesh;
        }

        static Mesh WordMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.95f, -0.42f, 0f),
                new Vector3(0.95f, -0.42f, 0f),
                new Vector3(0.95f, 0.42f, 0f),
                new Vector3(-0.95f, 0.42f, 0f)
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

        static Texture2D BuildAtlas()
        {
            var tex = new Texture2D(Atlas, 64, TextureFormat.RGBA32, false);
            var clear = new Color32[Atlas * 64];
            tex.SetPixels32(clear);
            for (int word = 0; word < ComicWords.Count; word++)
            {
                int letters = ComicWords.LetterCount(word);
                int origin = word * 64 + (64 - letters * 12) / 2;
                for (int place = 0; place < letters; place++)
                {
                    byte[] rows = ComicWords.Glyph(ComicWords.Letter(word, place));
                    int ox = origin + place * 12;
                    for (int y = 0; y < 7; y++)
                    {
                        byte row = rows[y];
                        for (int x = 0; x < 5; x++)
                        {
                            if ((row & (1 << (4 - x))) == 0) continue;
                            Stamp(tex, ox + x * 2, 50 - y * 6, 2, new Color32(20, 16, 16, 255), 1);
                            Stamp(tex, ox + x * 2, 51 - y * 6, 2, new Color32(255, 255, 255, 255), 0);
                        }
                    }
                }
            }
            tex.filterMode = FilterMode.Point;
            tex.Apply(false, true);
            return tex;
        }

        static void Stamp(Texture2D tex, int x, int y, int size, Color32 color, int grow)
        {
            for (int yy = -grow; yy < size + grow; yy++)
            {
                for (int xx = -grow; xx < size + grow; xx++)
                {
                    int px = x + xx;
                    int py = y + yy;
                    if (px < 0 || py < 0 || px >= tex.width || py >= tex.height) continue;
                    tex.SetPixel(px, py, color);
                }
            }
        }
    }
}
