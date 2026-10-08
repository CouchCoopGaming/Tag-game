using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled tag-hit burst. No letters. The comic word stays on ComicBurst.
    /// Each split camera billboards the quads. Reduced flashing hides it.
    /// </summary>
    [DefaultExecutionOrder(9010)]
    public sealed class Pass5Burst : MonoBehaviour
    {
        const int Slots = 8;

        static Pass5Burst _host;

        Transform[] _root;
        Material[] _mat;
        float[] _age;

        public static void Ensure()
        {
            if (_host != null) return;
            var go = new GameObject("TagHitBurstPool");
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<Pass5Burst>();
        }

        public static void Raise(Vector3 origin, Vector3 forward, float reach)
        {
            if (!Pass5Look.TagShows(GameSettings.Current)) return;
            Ensure();
            if (_host == null || _host._age == null) return;
            _host.Spawn(origin, forward, reach);
        }

        void Awake()
        {
            _host = this;
            Shader shader = Shader.Find("Tag/ComicBillboard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Mesh star = StarMesh();
            _root = new Transform[Slots];
            _mat = new Material[Slots];
            _age = new float[Slots];
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var go = new GameObject("TagHit");
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = star;
                var rend = go.AddComponent<MeshRenderer>();
                _mat[i] = new Material(shader);
                _mat[i].color = new Color(1f, 0.86f, 0.25f, 1f);
                rend.sharedMaterial = _mat[i];
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
                _root[i] = go.transform;
            }
        }

        void LateUpdate()
        {
            if (_age == null) return;
            bool show = Pass5Look.TagShows(GameSettings.Current);
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) continue;
                _age[i] += dt;
                if (!show || _age[i] >= 0.28f)
                {
                    _age[i] = -1f;
                    _root[i].gameObject.SetActive(false);
                    continue;
                }
                float u = _age[i] / 0.28f;
                float s = 0.45f + u * 0.85f;
                if (FxAmount.Density(GameSettings.Current) < 0.99f) s *= 0.7f;
                _root[i].localScale = new Vector3(s, s, 1f);
                Color c = _mat[i].color;
                c.a = (1f - u) * (show ? 1f : 0f);
                _mat[i].color = c;
            }
        }

        void Spawn(Vector3 origin, Vector3 forward, float reach)
        {
            int bits = Pass5Look.TagBits(FxAmount.Density(GameSettings.Current));
            if (bits > Slots) bits = Slots;
            if (bits < 1) return;
            Vector3 contact = HitConfirmTell.Contact(origin, forward, reach);
            contact.y += 0.35f;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.right;
            side.Normalize();
            for (int n = 0; n < bits; n++)
            {
                int slot = Free();
                float ang = n * 6.2831855f / bits;
                Vector3 at = contact + side * (Mathf.Cos(ang) * 0.28f) + Vector3.up * (Mathf.Sin(ang) * 0.22f);
                Place(slot, at, 0.55f + (n & 1) * 0.2f);
            }
        }

        int Free()
        {
            int slot = 0;
            float oldest = -1f;
            for (int i = 0; i < Slots; i++)
            {
                if (_age[i] < 0f) return i;
                if (_age[i] > oldest)
                {
                    oldest = _age[i];
                    slot = i;
                }
            }
            return slot;
        }

        void Place(int slot, Vector3 at, float size)
        {
            _age[slot] = 0f;
            _root[slot].position = at;
            _root[slot].localScale = new Vector3(size, size, 1f);
            _root[slot].gameObject.SetActive(true);
            Color c = _mat[slot].color;
            c.r = 1f;
            c.g = 0.86f;
            c.b = 0.25f;
            c.a = 1f;
            _mat[slot].color = c;
        }

        static Mesh StarMesh()
        {
            const int spikes = 8;
            var verts = new Vector3[spikes * 2 + 1];
            var tris = new int[spikes * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < spikes; i++)
            {
                float a = i / (float)spikes * 6.2831855f;
                float rad = (i & 1) == 0 ? 0.5f : 0.22f;
                verts[1 + i] = new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, 0f);
                tris[i * 3] = 0;
                tris[i * 3 + 1] = 1 + i;
                tris[i * 3 + 2] = 1 + ((i + 1) % spikes);
            }
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            return mesh;
        }
    }
}
