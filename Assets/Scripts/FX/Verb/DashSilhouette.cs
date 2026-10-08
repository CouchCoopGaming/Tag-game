using Tag.Art;
using UnityEngine;

namespace Tag.FX
{
    /// <summary>
    /// Four frozen copies of the pawn mesh. Each one keeps the bone pose from
    /// the sample that spawned it, tinted with a fresnel rim. Visual only.
    /// </summary>
    [DefaultExecutionOrder(125)]
    public sealed class DashSilhouette : MonoBehaviour
    {
        public const int Slots = 4;

        Transform _visual;
        Transform[] _live;
        Transform[] _slotRoot;
        Transform[][] _slotBones;
        Material[] _mats;
        bool _ready;

        public bool Ready => _ready;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<DashSilhouette>() == null)
                host.AddComponent<DashSilhouette>();
        }

        void Awake()
        {
            Build();
        }

        void OnDestroy()
        {
            if (_slotRoot == null) return;
            for (int i = 0; i < _slotRoot.Length; i++)
            {
                if (_slotRoot[i] != null)
                    Destroy(_slotRoot[i].gameObject);
            }
        }

        void Build()
        {
            _visual = FindVisual(transform);
            if (_visual == null) return;
            _live = _visual.GetComponentsInChildren<Transform>(true);
            if (_live == null || _live.Length < 2) return;
            Shader shader = Shader.Find("Tag/Afterimage");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            int seat = Seat(gameObject.name);
            float r;
            float g;
            float b;
            VerbFxLook.PlayerColor(seat, out r, out g, out b);
            _slotRoot = new Transform[Slots];
            _slotBones = new Transform[Slots][];
            _mats = new Material[Slots];
            for (int i = 0; i < Slots; i++)
            {
                GameObject clone = Instantiate(_visual.gameObject);
                clone.name = "DashGhost";
                clone.transform.SetParent(null, true);
                Silence(clone);
                Material mat = new Material(shader);
                mat.color = new Color(r, g, b, 1f);
                mat.SetFloat("_Fresnel", 1.7f);
                mat.SetFloat("_Fade", 0f);
                Paint(clone, mat);
                Transform[] bones = clone.GetComponentsInChildren<Transform>(true);
                if (bones == null || bones.Length != _live.Length)
                {
                    Destroy(clone);
                    return;
                }
                _slotRoot[i] = clone.transform;
                _slotBones[i] = bones;
                _mats[i] = mat;
                clone.SetActive(false);
            }
            _ready = true;
        }

        public void Capture(int slot)
        {
            if (!_ready || slot < 0 || slot >= Slots) return;
            Transform ghost = _slotRoot[slot];
            ghost.SetPositionAndRotation(_visual.position, _visual.rotation);
            ghost.localScale = _visual.lossyScale;
            Transform[] dst = _slotBones[slot];
            int n = _live.Length;
            if (dst.Length < n) n = dst.Length;
            for (int i = 1; i < n; i++)
            {
                dst[i].localPosition = _live[i].localPosition;
                dst[i].localRotation = _live[i].localRotation;
                dst[i].localScale = _live[i].localScale;
            }
        }

        public void Show(int slot, float alpha)
        {
            if (!_ready || slot < 0 || slot >= Slots) return;
            _slotRoot[slot].gameObject.SetActive(true);
            _mats[slot].SetFloat("_Fade", alpha);
        }

        public void Hide(int slot)
        {
            if (!_ready || slot < 0 || slot >= Slots) return;
            if (_slotRoot[slot] != null)
                _slotRoot[slot].gameObject.SetActive(false);
        }

        public void HideAll()
        {
            if (!_ready) return;
            for (int i = 0; i < Slots; i++)
                Hide(i);
        }

        static int Seat(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            char c = name[name.Length - 1];
            if (c >= '1' && c <= '4') return c - '1';
            return 0;
        }

        static Transform FindVisual(Transform host)
        {
            SkinnedMeshRenderer[] skins = host.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skins != null && skins.Length > 0)
                return Climb(host, skins[0].transform);
            MeshRenderer[] meshes = host.GetComponentsInChildren<MeshRenderer>(true);
            if (meshes == null) return null;
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || meshes[i].transform == host) continue;
                return Climb(host, meshes[i].transform);
            }
            return null;
        }

        static Transform Climb(Transform host, Transform t)
        {
            while (t.parent != null && t.parent != host)
                t = t.parent;
            return t == host ? null : t;
        }

        static void Silence(GameObject clone)
        {
            Behaviour[] behaviours = clone.GetComponentsInChildren<Behaviour>(true);
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] != null)
                        behaviours[i].enabled = false;
                }
            }
            Collider[] cols = clone.GetComponentsInChildren<Collider>(true);
            if (cols == null) return;
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }
        }

        static void Paint(GameObject clone, Material mat)
        {
            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                int n = r.sharedMaterials.Length;
                var slots = new Material[n];
                for (int s = 0; s < n; s++)
                    slots[s] = mat;
                r.sharedMaterials = slots;
            }
        }
    }
}
