using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Pooled tag-hit burst. One comic starburst at the contact, no letters.
    /// The comic word stays on ComicBurst. Reduced flashing hides it.
    /// </summary>
    [DefaultExecutionOrder(9010)]
    public sealed class Pass5Burst : MonoBehaviour
    {
        const int Slots = 8;
        const int Spikes = 8;
        const int Layers = 4;

        static Pass5Burst _host;

        Transform[] _root;
        Material[] _layer;
        Material[] _spikeMat;
        Transform[] _spike;
        float[] _age;
        int[] _spikeOn;

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
            Mesh outline = Star(12, ComicArt.OutlineScale);
            Mesh fill = Star(12, 1f);
            Mesh flash = Star(12, ComicArt.InnerScale);
            Mesh dots = Halftone();
            var spikeMesh = new Mesh[Spikes];
            for (int i = 0; i < Spikes; i++)
                spikeMesh[i] = Spike(i, Spikes);
            _root = new Transform[Slots];
            _layer = new Material[Slots * Layers];
            _spike = new Transform[Slots * Spikes];
            _spikeMat = new Material[Slots * Spikes];
            _age = new float[Slots];
            _spikeOn = new int[Slots];
            for (int i = 0; i < Slots; i++)
            {
                _age[i] = -1f;
                var go = new GameObject("TagHit");
                go.transform.SetParent(transform, false);
                go.SetActive(false);
                _root[i] = go.transform;
                _layer[i * Layers + 0] = AddChild(go.transform, "Outline", outline, shader, new Color(0.05f, 0.04f, 0.04f, 1f), out _);
                _layer[i * Layers + 1] = AddChild(go.transform, "Fill", fill, shader, new Color(1f, 0.55f, 0.08f, 1f), out _);
                _layer[i * Layers + 2] = AddChild(go.transform, "Flash", flash, shader, new Color(1f, 0.98f, 0.92f, 1f), out _);
                _layer[i * Layers + 3] = AddChild(go.transform, "Dots", dots, shader, new Color(0.45f, 0.10f, 0.08f, 1f), out _);
                for (int s = 0; s < Spikes; s++)
                {
                    _spikeMat[i * Spikes + s] = AddChild(
                        go.transform, "Spike", spikeMesh[s], shader, new Color(0.10f, 0.05f, 0.04f, 1f), out Transform spike);
                    _spike[i * Spikes + s] = spike;
                }
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
                float s = 0.85f + u * 0.55f;
                if (FxAmount.Density(GameSettings.Current) < 0.99f) s *= 0.7f;
                _root[i].localScale = new Vector3(s, s, 1f);
                float a = (1f - u) * (show ? 1f : 0f);
                Fade(i, a);
            }
        }

        void Fade(int slot, float alpha)
        {
            for (int n = 0; n < Layers; n++)
            {
                Material mat = _layer[slot * Layers + n];
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
            int on = _spikeOn[slot];
            for (int s = 0; s < Spikes; s++)
            {
                if (s >= on) continue;
                Material mat = _spikeMat[slot * Spikes + s];
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }

        void Spawn(Vector3 origin, Vector3 forward, float reach)
        {
            int bits = Pass5Look.TagBits(FxAmount.Density(GameSettings.Current));
            if (bits > Spikes) bits = Spikes;
            if (bits < 1) return;
            Vector3 contact = HitConfirmTell.Contact(origin, forward, reach);
            contact.y += 0.15f;
            int slot = Free();
            _spikeOn[slot] = bits;
            for (int s = 0; s < Spikes; s++)
                _spike[slot * Spikes + s].gameObject.SetActive(s < bits);
            Place(slot, contact, 1.15f);
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
            Fade(slot, 1f);
        }

        static Material AddChild(Transform parent, string name, Mesh mesh, Shader shader, Color color, out Transform xform)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            xform = go.transform;
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            var mat = new Material(shader);
            mat.color = color;
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            return mat;
        }

        static Mesh Star(int count, float scale)
        {
            var verts = new Vector3[1 + count * 2];
            var tris = new int[count * 6];
            verts[0] = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                ComicArt.Point(i, count, false, out float vx, out float vy);
                ComicArt.Point(i, count, true, out float tx, out float ty);
                verts[1 + i * 2] = new Vector3(vx * scale, vy * scale, 0f);
                verts[2 + i * 2] = new Vector3(tx * scale, ty * scale, 0f);
            }
            int t = 0;
            for (int i = 0; i < count; i++)
            {
                int valley = 1 + i * 2;
                int tip = valley + 1;
                int next = 1 + ((i + 1) % count) * 2;
                tris[t++] = 0;
                tris[t++] = valley;
                tris[t++] = tip;
                tris[t++] = 0;
                tris[t++] = tip;
                tris[t++] = next;
            }
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            return mesh;
        }

        static Mesh Spike(int index, int count)
        {
            float ang = (index + 0.5f) / count * 6.2831855f;
            float inner = 0.48f;
            float outer = 0.92f + (index % 2) * 0.26f;
            float w = 0.04f;
            float c = Mathf.Cos(ang);
            float s = Mathf.Sin(ang);
            float px = -s * w;
            float py = c * w;
            var verts = new Vector3[3];
            verts[0] = new Vector3(c * inner + px, s * inner + py, 0f);
            verts[1] = new Vector3(c * inner - px, s * inner - py, 0f);
            verts[2] = new Vector3(c * outer, s * outer, 0f);
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 1 };
            return mesh;
        }

        static Mesh Halftone()
        {
            const int n = 13;
            var verts = new Vector3[n * 3];
            var tris = new int[n * 3];
            int k = 0;
            for (int iy = -2; iy <= 2; iy++)
            {
                for (int ix = -2; ix <= 2; ix++)
                {
                    if (((ix + iy) & 1) != 0) continue;
                    if (ix * ix + iy * iy > 5) continue;
                    if (k >= n) break;
                    float x = ix * 0.11f;
                    float y = iy * 0.11f;
                    float r = 0.028f;
                    verts[k * 3] = new Vector3(x, y + r, 0f);
                    verts[k * 3 + 1] = new Vector3(x - r, y - r, 0f);
                    verts[k * 3 + 2] = new Vector3(x + r, y - r, 0f);
                    tris[k * 3] = k * 3;
                    tris[k * 3 + 1] = k * 3 + 1;
                    tris[k * 3 + 2] = k * 3 + 2;
                    k++;
                }
            }
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            return mesh;
        }
    }
}
