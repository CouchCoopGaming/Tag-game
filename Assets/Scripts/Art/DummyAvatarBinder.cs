using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Replaces capsule mesh with Dummy_Runner / Dummy_It visual.
    /// Loads from SerializeField or Resources/Characters/ if null.
    /// </summary>
    public class DummyAvatarBinder : MonoBehaviour
    {
        [SerializeField] GameObject runnerVisualPrefab;
        [SerializeField] GameObject itVisualPrefab;
        [SerializeField] bool hideRootMeshRenderers = true;
        [SerializeField] Vector3 visualLocalPosition = Vector3.zero;
        [SerializeField] Vector3 visualLocalScale = Vector3.one;

        ItController _it;
        GameObject _visualInstance;
        bool _showingIt;
        bool _resolved;

        void Awake()
        {
            _it = GetComponent<ItController>();
            ResolvePrefabs();
            ApplyVisual(_it != null && _it.IsIt);
            HideCapsuleMeshes();
        }

        void ResolvePrefabs()
        {
            if (_resolved) return;
            _resolved = true;
            if (runnerVisualPrefab == null)
                runnerVisualPrefab = Resources.Load<GameObject>("Characters/Dummy_Runner");
            if (itVisualPrefab == null)
                itVisualPrefab = Resources.Load<GameObject>("Characters/Dummy_It");
        }

        void HideCapsuleMeshes()
        {
            if (!hideRootMeshRenderers) return;
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (_visualInstance != null && r.transform.IsChildOf(_visualInstance.transform))
                    continue;
                // Keep only dummy visual renderers
                if (r.transform == transform || r.transform.parent == transform)
                {
                    var n = r.gameObject.name;
                    if (n.Contains("Capsule") || n == "Mesh" || n == "Player" || n == "DummyRunner" || r.GetComponent<CharacterController>() != null)
                        r.enabled = false;
                }
            }
            var rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;
            var rootMf = GetComponent<MeshFilter>();
            if (rootMf != null) rootMf.sharedMesh = null;
        }

        void LateUpdate()
        {
            if (_it == null) return;
            bool wantIt = _it.IsIt;
            if (wantIt != _showingIt)
                ApplyVisual(wantIt);
        }

        void ApplyVisual(bool asIt)
        {
            ResolvePrefabs();
            _showingIt = asIt;
            var prefab = asIt
                ? (itVisualPrefab != null ? itVisualPrefab : runnerVisualPrefab)
                : (runnerVisualPrefab != null ? runnerVisualPrefab : itVisualPrefab);
            if (prefab == null)
            {
                Debug.LogWarning("[DummyAvatarBinder] No dummy prefab — run Tag/Setup Hub Visuals once in Editor.");
                return;
            }

            if (_visualInstance != null)
                Destroy(_visualInstance);

            _visualInstance = Instantiate(prefab, transform);
            _visualInstance.name = asIt ? "DummyVisual_It" : "DummyVisual_Runner";
            _visualInstance.transform.localPosition = visualLocalPosition;
            _visualInstance.transform.localRotation = Quaternion.identity;
            _visualInstance.transform.localScale = visualLocalScale;

            foreach (var cc in _visualInstance.GetComponentsInChildren<CharacterController>())
                Destroy(cc);
            foreach (var rb in _visualInstance.GetComponentsInChildren<Rigidbody>())
                Destroy(rb);

            HideCapsuleMeshes();
        }
    }
}
