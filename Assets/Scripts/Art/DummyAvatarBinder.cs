using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Swaps capsule mesh for Dummy_Runner / Dummy_It visual child.
    /// Assign visual prefabs in Inspector (or run Tag/Setup Dummy Prefabs in Editor).
    /// CharacterController + gameplay comps stay on this root.
    /// </summary>
    public class DummyAvatarBinder : MonoBehaviour
    {
        [SerializeField] GameObject runnerVisualPrefab;
        [SerializeField] GameObject itVisualPrefab;
        [SerializeField] bool hideCapsuleRenderer = true;
        [SerializeField] Vector3 visualLocalPosition = Vector3.zero;
        [SerializeField] Vector3 visualLocalScale = Vector3.one;

        ItController _it;
        GameObject _visualInstance;
        bool _showingIt;

        void Awake()
        {
            _it = GetComponent<ItController>();
            ApplyVisual(_it != null && _it.IsIt);
            if (hideCapsuleRenderer)
            {
                foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
                {
                    // Hide only root capsule renderers, not the dummy visual
                    if (_visualInstance != null && r.transform.IsChildOf(_visualInstance.transform))
                        continue;
                    if (r.GetComponent<CharacterController>() != null || r.transform == transform)
                        r.enabled = false;
                    // Common capsule child named Mesh or Capsule
                    if (r.gameObject.name.Contains("Capsule") || r.gameObject.name == "Mesh")
                        r.enabled = false;
                }
            }
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
            _showingIt = asIt;
            var prefab = asIt
                ? (itVisualPrefab != null ? itVisualPrefab : runnerVisualPrefab)
                : (runnerVisualPrefab != null ? runnerVisualPrefab : itVisualPrefab);
            if (prefab == null) return;

            if (_visualInstance != null)
                Destroy(_visualInstance);

            _visualInstance = Instantiate(prefab, transform);
            _visualInstance.name = asIt ? "DummyVisual_It" : "DummyVisual_Runner";
            _visualInstance.transform.localPosition = visualLocalPosition;
            _visualInstance.transform.localRotation = Quaternion.identity;
            _visualInstance.transform.localScale = visualLocalScale;

            // Strip any leftover gameplay comps from visual prefab copies
            foreach (var cc in _visualInstance.GetComponentsInChildren<CharacterController>())
                Destroy(cc);
        }
    }
}
