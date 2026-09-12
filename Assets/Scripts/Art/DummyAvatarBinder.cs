using Tag.Gameplay;
using Tag.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Replaces capsule mesh with Dummy_Runner / Dummy_It visual.
    /// Load order: SerializeField → Resources/Characters prefab → primitive dummy.
    /// Hub <c>Tag → Setup Hub Visuals</c> writes those prefabs from
    /// <c>ArtMeshPaths.PreferCharacterFbx</c> (HiPoly Dummy_*_Hi.fbx when present).
    /// Applies Landon mats (paint lock) and DummyLocomotor.
    /// </summary>
    public class DummyAvatarBinder : MonoBehaviour
    {
        [SerializeField] GameObject runnerVisualPrefab;
        [SerializeField] GameObject itVisualPrefab;
        [SerializeField] Material runnerBaseMat;
        [SerializeField] Material runnerAccentMat;
        [SerializeField] Material runnerOverrideMat;
        [SerializeField] Material itBaseMat;
        [SerializeField] Material itAccentMat;
        [SerializeField] Material itOverrideMat;
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
            runnerVisualPrefab = FirstRenderable(runnerVisualPrefab, "Characters/Dummy_Runner");
            itVisualPrefab = FirstRenderable(itVisualPrefab, "Characters/Dummy_It");
            if (runnerBaseMat == null) runnerBaseMat = Resources.Load<Material>("Characters/Mat_Runner_Base");
            if (runnerAccentMat == null) runnerAccentMat = Resources.Load<Material>("Characters/Mat_Runner_Accent");
            if (runnerOverrideMat == null) runnerOverrideMat = Resources.Load<Material>("Characters/Mat_Runner_ItOverride");
            if (itBaseMat == null) itBaseMat = Resources.Load<Material>("Characters/Mat_It_Base");
            if (itAccentMat == null) itAccentMat = Resources.Load<Material>("Characters/Mat_It_Accent");
            if (itOverrideMat == null) itOverrideMat = Resources.Load<Material>("Characters/Mat_It_ItOverride");
        }

        static GameObject FirstRenderable(GameObject current, params string[] resourcePaths)
        {
            if (DummyPrimitiveFactory.PrefabHasRenderer(current)) return current;
            for (int i = 0; i < resourcePaths.Length; i++)
            {
                var loaded = Resources.Load<GameObject>(resourcePaths[i]);
                if (DummyPrimitiveFactory.PrefabHasRenderer(loaded))
                    return loaded;
            }
            return current;
        }

        void HideCapsuleMeshes()
        {
            if (!hideRootMeshRenderers) return;
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (_visualInstance != null && r.transform.IsChildOf(_visualInstance.transform))
                    continue;
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

            if (_visualInstance != null)
                Destroy(_visualInstance);

            if (DummyPrimitiveFactory.PrefabHasRenderer(prefab))
            {
                _visualInstance = Instantiate(prefab, transform);
                _visualInstance.name = asIt ? "DummyVisual_It" : "DummyVisual_Runner";
                _visualInstance.transform.localPosition = visualLocalPosition;
                _visualInstance.transform.localRotation = Quaternion.identity;
                _visualInstance.transform.localScale = visualLocalScale;
                foreach (var cc in _visualInstance.GetComponentsInChildren<CharacterController>())
                    Destroy(cc);
                foreach (var rb in _visualInstance.GetComponentsInChildren<Rigidbody>())
                    Destroy(rb);
                ApplyCharacterMats(_visualInstance, asIt);
                FitVisual(_visualInstance);
            }
            else
            {
                _visualInstance = DummyPrimitiveFactory.Build(transform, asIt);
            }

            var loco = _visualInstance.GetComponent<DummyLocomotor>();
            if (loco == null) loco = _visualInstance.AddComponent<DummyLocomotor>();
            loco.Bind(_visualInstance.transform, GetComponent<PlayerMotor>(), GetComponent<PunchHitbox>(), GetComponent<CharacterController>());

            HideCapsuleMeshes();
            if (GetComponent<ItMarker>() == null)
                gameObject.AddComponent<ItMarker>();
        }

        void ApplyCharacterMats(GameObject visual, bool asIt)
        {
            var mats = asIt
                ? new[]
                {
                    itBaseMat ?? DummyPrimitiveFactory.MakeMat(new Color(1f, 0.416f, 0f)),
                    itAccentMat ?? DummyPrimitiveFactory.MakeMat(new Color(0.04f, 0.04f, 0.04f)),
                    itOverrideMat ?? DummyPrimitiveFactory.MakeMat(new Color(1f, 0.416f, 0f))
                }
                : new[]
                {
                    runnerBaseMat ?? DummyPrimitiveFactory.MakeMat(new Color(0.91f, 0.851f, 0.753f)),
                    runnerAccentMat ?? DummyPrimitiveFactory.MakeMat(new Color(0.169f, 0.702f, 0.639f)),
                    runnerOverrideMat ?? DummyPrimitiveFactory.MakeMat(new Color(0.91f, 0.851f, 0.753f))
                };

            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r.gameObject.name.StartsWith("ItHat") || r.gameObject.name.StartsWith("ItHalo"))
                    continue;
                int n = Mathf.Max(1, r.sharedMaterials.Length);
                var next = new Material[n];
                for (int i = 0; i < n; i++)
                    next[i] = mats[Mathf.Min(i, mats.Length - 1)];
                r.sharedMaterials = next;
            }
        }

        static void FitVisual(GameObject visual)
        {
            var rends = visual.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            float h = b.size.y;
            if (h > 0.15f && (h < 1.15f || h > 2.7f))
            {
                float s = 1.8f / h;
                visual.transform.localScale *= s;
                b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                    b.Encapsulate(rends[i].bounds);
            }
            float feet = b.min.y - visual.transform.parent.position.y;
            if (Mathf.Abs(feet) > 0.05f)
                visual.transform.localPosition -= new Vector3(0f, feet, 0f);
        }
    }
}
