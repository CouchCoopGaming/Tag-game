using System.Collections;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tag.Art
{
    /// <summary>
    /// Replaces capsule mesh with HiPoly crash-dummy / mannequin visual.
    /// Load order: SerializeField -> Hier HiPoly -> flat HiPoly -> Resources -> primitive fallback.
    /// Prefers hierarchical HiPoly (*_Hier_Hi). Falls back to flat HiPoly, then Navy Spade primitive when unbound.
    /// </summary>
    public class DummyAvatarBinder : MonoBehaviour
    {
        static readonly string[] MannequinColors =
        {
            "Blue", "Mint", "Orange", "Lavender", "Tan", "Red"
        };

        const string HiPolyDir = "Assets/Art/Characters/HiPoly/";

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
        [Tooltip("If true, always use procedural mannequin. Leave false to use HiPoly FBX when it has bindable bones.")]
        [SerializeField] bool forcePrimitiveMannequin = false;
        [SerializeField] bool preferMannequinOverRunnerIt = true;
        [Tooltip("If HiPoly/Resources instantiate but have no hierarchical limb bones, use Navy Spade primitive.")]
        [SerializeField] bool fallbackToPrimitiveIfUnbound = true;

        ItController _it;
        GameObject _visualInstance;
        bool _showingIt;
        bool _resolved;
        Coroutine _hitPulseCo;
        Vector3 _visualBaseScale = Vector3.one;

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

            string color = PickColor();
#if UNITY_EDITOR
            if (preferMannequinOverRunnerIt)
            {
                // Prefer hierarchical HiPoly (*_Hier_Hi) so DummyLocomotor can swing limbs;
                // flat HiPoly remains as secondary before Runner/It legacy meshes.
                runnerVisualPrefab = FirstRenderable(runnerVisualPrefab,
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hier_Hi.fbx"),
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hi.fbx"),
                    LoadHiPoly("Dummy_Runner_Hi.fbx"));
                itVisualPrefab = FirstRenderable(itVisualPrefab,
                    LoadHiPoly("Dummy_Mannequin_Red_Hier_Hi.fbx"),
                    LoadHiPoly("Dummy_Mannequin_Red_Hi.fbx"),
                    LoadHiPoly("Dummy_It_Hi.fbx"),
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hier_Hi.fbx"));
            }
            else
            {
                runnerVisualPrefab = FirstRenderable(runnerVisualPrefab,
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hier_Hi.fbx"),
                    LoadHiPoly("Dummy_Runner_Hi.fbx"),
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hi.fbx"));
                itVisualPrefab = FirstRenderable(itVisualPrefab,
                    LoadHiPoly("Dummy_Mannequin_Red_Hier_Hi.fbx"),
                    LoadHiPoly("Dummy_It_Hi.fbx"),
                    LoadHiPoly("Dummy_Mannequin_Red_Hi.fbx"));
            }
#endif
            runnerVisualPrefab = FirstRenderable(runnerVisualPrefab, "Characters/Dummy_Runner");
            itVisualPrefab = FirstRenderable(itVisualPrefab, "Characters/Dummy_It");

            if (runnerBaseMat == null) runnerBaseMat = Resources.Load<Material>("Characters/Mat_Runner_Base");
            if (runnerAccentMat == null) runnerAccentMat = Resources.Load<Material>("Characters/Mat_Runner_Accent");
            if (runnerOverrideMat == null) runnerOverrideMat = Resources.Load<Material>("Characters/Mat_Runner_ItOverride");
            if (itBaseMat == null) itBaseMat = Resources.Load<Material>("Characters/Mat_It_Base");
            if (itAccentMat == null) itAccentMat = Resources.Load<Material>("Characters/Mat_It_Accent");
            if (itOverrideMat == null) itOverrideMat = Resources.Load<Material>("Characters/Mat_It_ItOverride");
        }

#if UNITY_EDITOR
        static GameObject LoadHiPoly(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(HiPolyDir + fileName);
        }
#endif

        string PickColor()
        {
            string id = _it != null ? _it.PlayerId : gameObject.name;
            if (string.IsNullOrEmpty(id)) id = gameObject.name;
            int h = 0;
            for (int i = 0; i < id.Length; i++) h = h * 31 + id[i];
            if (h < 0) h = -h;
            return MannequinColors[h % MannequinColors.Length];
        }

        static GameObject FirstRenderable(GameObject current, params object[] candidates)
        {
            if (DummyPrimitiveFactory.PrefabHasRenderer(current)) return current;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i] is GameObject go && DummyPrimitiveFactory.PrefabHasRenderer(go))
                    return go;
                if (candidates[i] is string path)
                {
                    var loaded = Resources.Load<GameObject>(path);
                    if (DummyPrimitiveFactory.PrefabHasRenderer(loaded))
                        return loaded;
                }
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
            bool becameIt = asIt && !_showingIt;
            _showingIt = asIt;
            var prefab = asIt
                ? (itVisualPrefab != null ? itVisualPrefab : runnerVisualPrefab)
                : (runnerVisualPrefab != null ? runnerVisualPrefab : itVisualPrefab);

            if (_visualInstance != null)
                Destroy(_visualInstance);

            bool usedPrimitive = false;
            if (!forcePrimitiveMannequin && DummyPrimitiveFactory.PrefabHasRenderer(prefab))
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

                // Flat HiPoly / Dummy_Runner meshes have limb names but no hierarchy —
                // procedural swing cannot move distal limbs. Prefer Navy Spade primitive.
                // HasBindableBones uses GetComponentsInChildren, so Unity FBX root wrapper is fine.
                if (fallbackToPrimitiveIfUnbound && !DummyLocomotor.HasBindableBones(_visualInstance.transform))
                {
                    Debug.Log($"[DummyAvatarBinder] '{prefab.name}' has no hierarchical limb bones — using Navy Spade primitive.");
                    Destroy(_visualInstance);
                    _visualInstance = DummyPrimitiveFactory.Build(transform, asIt);
                    usedPrimitive = true;
                }
                else
                {
                    ApplyCharacterMats(_visualInstance, asIt);
                    FitVisual(_visualInstance);
                }
            }
            else
            {
                _visualInstance = DummyPrimitiveFactory.Build(transform, asIt);
                usedPrimitive = true;
            }

            var loco = _visualInstance.GetComponent<DummyLocomotor>();
            if (loco == null) loco = _visualInstance.AddComponent<DummyLocomotor>();
            loco.Bind(_visualInstance.transform, GetComponent<PlayerMotor>(), GetComponent<PunchHitbox>());

            HideCapsuleMeshes();
            if (_visualInstance != null)
                _visualBaseScale = _visualInstance.transform.localScale;
            if (GetComponent<ItMarker>() == null)
                gameObject.AddComponent<ItMarker>();

            if (becameIt)
                PlayTagHitFeedback();

            if (usedPrimitive)
                Debug.Log($"[DummyAvatarBinder] Navy Spade primitive active on {gameObject.name} (asIt={asIt}).");
        }

        void ApplyCharacterMats(GameObject visual, bool asIt)
        {
            // HiPoly mannequins already authored with color — only tint if mats exist
            // and mesh looks uncolored (skip heavy override when FBX has materials).
            bool hasAuthored = false;
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.name != "Default-Material")
                {
                    hasAuthored = true;
                    break;
                }
            }
            if (hasAuthored && !asIt) return;

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


        public void PlayTagHitFeedback()
        {
            if (_visualInstance == null) return;
            if (_hitPulseCo != null) StopCoroutine(_hitPulseCo);
            _hitPulseCo = StartCoroutine(HitPulseRoutine());
        }

        IEnumerator HitPulseRoutine()
        {
            var t = _visualInstance != null ? _visualInstance.transform : null;
            if (t == null) yield break;
            Vector3 baseScale = _visualBaseScale.sqrMagnitude > 0.0001f ? _visualBaseScale : t.localScale;
            // Brief squash / flash scale so punch connect reads in third-person
            float dur = 0.22f;
            float elapsed = 0f;
            while (elapsed < dur && t != null)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                // Overshoot then settle: 1.18 -> 0.92 -> 1
                float s = u < 0.35f
                    ? Mathf.Lerp(1f, 1.18f, u / 0.35f)
                    : (u < 0.65f ? Mathf.Lerp(1.18f, 0.92f, (u - 0.35f) / 0.3f)
                                 : Mathf.Lerp(0.92f, 1f, (u - 0.65f) / 0.35f));
                t.localScale = baseScale * s;
                yield return null;
            }
            if (t != null) t.localScale = baseScale;
            _hitPulseCo = null;
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
