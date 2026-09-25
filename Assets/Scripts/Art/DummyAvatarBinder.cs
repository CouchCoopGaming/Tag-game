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
    /// Load order: Tan/Orange Hier -> other Hier -> assigned prefab -> flat HiPoly -> Resources -> primitive.
    /// Defaults: Runner Tan Hier, It Orange Hier. Other *_Hier_Hi, flat HiPoly, then Navy Spade primitive.
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
        bool _loggedHier;
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
            // Scene pawns serialize the flat Dummy_Runner / Dummy_It prefab. FirstRenderable
            // keeps that prefab when it has a mesh, so Hier never won. Approved meshes are
            // Tan runner and Orange It. The catalog covers player builds; the editor path
            // loads the same FBX when the catalog ref is empty.
            GameObject hierRunner = HierPrefab(false);
            GameObject hierIt = HierPrefab(true);
#if UNITY_EDITOR
            var assignedRunner = runnerVisualPrefab;
            var assignedIt = itVisualPrefab;
            if (!DummyPrimitiveFactory.PrefabHasRenderer(hierRunner))
                hierRunner = LoadHiPoly("Dummy_Mannequin_Tan_Hier_Hi.fbx");
            if (!DummyPrimitiveFactory.PrefabHasRenderer(hierIt))
                hierIt = LoadHiPoly("Dummy_Mannequin_Orange_Hier_Hi.fbx");
#endif
            if (preferMannequinOverRunnerIt && DummyPrimitiveFactory.PrefabHasRenderer(hierRunner))
                runnerVisualPrefab = hierRunner;
            else
            {
#if UNITY_EDITOR
                runnerVisualPrefab = FirstRenderable(null,
                    LoadHiPoly("Dummy_Mannequin_Tan_Hier_Hi.fbx"),
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hier_Hi.fbx"),
                    assignedRunner,
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hi.fbx"),
                    LoadHiPoly("Dummy_Mannequin_Tan_Hi.fbx"),
                    LoadHiPoly("Dummy_Runner_Hi.fbx"));
#endif
                runnerVisualPrefab = FirstRenderable(runnerVisualPrefab, "Characters/Dummy_Runner");
            }

            if (preferMannequinOverRunnerIt && DummyPrimitiveFactory.PrefabHasRenderer(hierIt))
                itVisualPrefab = hierIt;
            else
            {
#if UNITY_EDITOR
                itVisualPrefab = FirstRenderable(null,
                    LoadHiPoly("Dummy_Mannequin_Orange_Hier_Hi.fbx"),
                    assignedIt,
                    LoadHiPoly("Dummy_Mannequin_Orange_Hi.fbx"),
                    LoadHiPoly("Dummy_It_Hi.fbx"),
                    LoadHiPoly($"Dummy_Mannequin_{color}_Hier_Hi.fbx"));
#endif
                itVisualPrefab = FirstRenderable(itVisualPrefab, "Characters/Dummy_It");
            }

            if (runnerBaseMat == null) runnerBaseMat = Resources.Load<Material>("Characters/Mat_Runner_Base");
            if (runnerAccentMat == null) runnerAccentMat = Resources.Load<Material>("Characters/Mat_Runner_Accent");
            if (runnerOverrideMat == null) runnerOverrideMat = Resources.Load<Material>("Characters/Mat_Runner_ItOverride");
            if (itBaseMat == null) itBaseMat = Resources.Load<Material>("Characters/Mat_It_Base");
            if (itAccentMat == null) itAccentMat = Resources.Load<Material>("Characters/Mat_It_Accent");
            if (itOverrideMat == null) itOverrideMat = Resources.Load<Material>("Characters/Mat_It_ItOverride");
        }

        static GameObject HierPrefab(bool asIt)
        {
            var catalog = Resources.Load<HierMannequinCatalog>("Characters/HierMannequinCatalog");
            if (catalog == null) return null;
            return asIt ? catalog.It : catalog.Runner;
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
                    _visualInstance = DummyPrimitiveFactory.Build(transform, asIt, PickColor());
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
                _visualInstance = DummyPrimitiveFactory.Build(transform, asIt, PickColor());
                usedPrimitive = true;
            }

            var loco = _visualInstance.GetComponent<DummyLocomotor>();
            if (loco == null) loco = _visualInstance.AddComponent<DummyLocomotor>();
            var motor = GetComponent<PlayerMotor>();
            loco.Bind(_visualInstance.transform, motor, GetComponent<PunchHitbox>());

            // Jet VFX gated — jet is disabled by default (enableJet=false). Do not auto-add.
            var jetFx = GetComponent<JetThrustVisual>();
            if (jetFx != null)
                jetFx.Bind(motor, _visualInstance.transform);

            HideCapsuleMeshes();
            if (_visualInstance != null)
                _visualBaseScale = _visualInstance.transform.localScale;
            if (GetComponent<ItMarker>() == null)
                gameObject.AddComponent<ItMarker>();

            if (becameIt)
                PlayTagHitFeedback();

            if (usedPrimitive)
                Debug.Log($"[DummyAvatarBinder] Navy Spade primitive active on {gameObject.name} (asIt={asIt}).");
            else if (!_loggedHier && prefab != null && prefab.name.IndexOf("Hier", System.StringComparison.Ordinal) >= 0)
            {
                _loggedHier = true;
                Debug.Log($"[DummyAvatarBinder] Hier mannequin '{prefab.name}' on {gameObject.name} (asIt={asIt}).");
            }
        }

        void ApplyCharacterMats(GameObject visual, bool asIt)
        {
            // Hier FBX already carries AD paint (Tan runner, Orange It with nested Vs).
            // Do not restamp slots — a one-material chevron renderer would turn orange.
            bool hasAuthored = false;
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.name != "Default-Material")
                {
                    hasAuthored = true;
                    break;
                }
            }
            if (hasAuthored) return;

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
            // Stronger squash / flash so tag transfer + punch connect read in third-person
            float dur = 0.28f;
            float elapsed = 0f;
            while (elapsed < dur && t != null)
            {
                elapsed += Time.deltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                // Overshoot then settle: 1.28 -> 0.88 -> 1
                float s = u < 0.32f
                    ? Mathf.Lerp(1f, 1.28f, u / 0.32f)
                    : (u < 0.62f ? Mathf.Lerp(1.28f, 0.88f, (u - 0.32f) / 0.3f)
                                 : Mathf.Lerp(0.88f, 1f, (u - 0.62f) / 0.38f));
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
