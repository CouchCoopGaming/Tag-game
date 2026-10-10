using Tag.Couch;
using Tag.Practice;
using Tag.Settings;
using UnityEngine;

namespace Tag.MatchStats
{
    /// <summary>
    /// Ghost figures for the results highlight. Same pose weights as the practice ghost.
    /// A figure is a mesh and a chest. Playback only writes a transform.
    /// </summary>
    public static class MatchGhostView
    {
        static readonly GameObject[] Figures = new GameObject[MatchHighlight.Pawns];
        static readonly Transform[] Chests = new Transform[MatchHighlight.Pawns];
        static readonly Renderer[] Renderers = new Renderer[MatchHighlight.Pawns];
        static Mesh _mesh;
        static int _live;

        public static int Live => _live;

        public static void Tick(float dt)
        {
            if (!MatchHighlight.Playing)
            {
                if (_live > 0) Release();
                return;
            }
            MatchHighlight.Tick(dt);
            int n = MatchBook.Count;
            if (n > MatchHighlight.Pawns) n = MatchHighlight.Pawns;
            float speed = 0f;
            for (int i = 0; i < n; i++)
            {
                if (!MatchHighlight.At(i, MatchHighlight.Clock, out float x, out float y, out float z, out float yaw, out byte pose))
                    continue;
                Show(i, x, y, z, yaw, pose, speed);
            }
            if (!MatchHighlight.Playing)
                Release();
        }

        public static void Release()
        {
            for (int i = 0; i < Figures.Length; i++)
            {
                if (Figures[i] == null) continue;
                Object.Destroy(Figures[i]);
                Figures[i] = null;
                Chests[i] = null;
                Renderers[i] = null;
            }
            _live = 0;
            MatchHighlight.Leftovers = 0;
            MatchHighlight.Playing = false;
        }

        static void Show(int slot, float x, float y, float z, float yaw, byte pose, float speed)
        {
            Ensure(slot);
            GameObject figure = Figures[slot];
            if (figure == null) return;
            MatchHighlight.Clamp(ref x, ref y, ref z);
            figure.transform.position = new Vector3(x, y, z);
            figure.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Renderer renderer = Renderers[slot];
            if (renderer != null) renderer.enabled = true;
            Transform chest = Chests[slot];
            if (chest == null) return;
            float w = PracticePose.Weight(pose, 0.08f, 0f, speed);
            chest.localRotation = Quaternion.Euler(w * 18f, 0f, 0f);
        }

        static void Ensure(int slot)
        {
            if (slot < 0 || slot >= Figures.Length) return;
            if (Figures[slot] != null) return;
            var figure = new GameObject("MatchGhost");
            var filter = figure.AddComponent<MeshFilter>();
            var renderer = figure.AddComponent<MeshRenderer>();
            filter.sharedMesh = Mesh();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                int seat = slot < AccessibilityPalette.Players ? slot : 0;
                CouchPlay.Tint(seat, out float r, out float g, out float b);
                mat.color = new Color(r, g, b, 0.45f);
                renderer.sharedMaterial = mat;
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var chest = new GameObject("Chest");
            chest.transform.SetParent(figure.transform, false);
            chest.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            Figures[slot] = figure;
            Chests[slot] = chest.transform;
            Renderers[slot] = renderer;
            _live++;
            MatchHighlight.Leftovers = _live;
        }

        static Mesh Mesh()
        {
            if (_mesh != null) return _mesh;
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.3f, 0f, -0.2f),
                new Vector3(0.3f, 0f, -0.2f),
                new Vector3(0.3f, 1.8f, -0.2f),
                new Vector3(-0.3f, 1.8f, -0.2f),
                new Vector3(-0.3f, 0f, 0.2f),
                new Vector3(0.3f, 0f, 0.2f),
                new Vector3(0.3f, 1.8f, 0.2f),
                new Vector3(-0.3f, 1.8f, 0.2f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 6, 2, 3, 7, 6,
                1, 2, 6, 1, 6, 5,
                0, 4, 7, 0, 7, 3
            };
            _mesh = mesh;
            return mesh;
        }
    }
}
