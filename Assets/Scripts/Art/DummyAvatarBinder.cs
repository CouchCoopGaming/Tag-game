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
            // Strip the placeholder before any visual load can throw. Match start
            // used to re-enable this renderer after a hide, which brought the pill back.
            StripPlaceholderBody();
            _it = GetComponent<ItController>();
            if (_it != null) _it.ReleaseRootAccent();
            ResolvePrefabs();
            ApplyVisual(_it != null && _it.IsIt);
            StripPlaceholderBody();
        }

        void ResolvePrefabs()
        {
            if (_resolved) return;
            _resolved = true;

            string color = PickColor();
            // Scene pawns serialize the flat Dummy_Runner / Dummy_It prefab. FirstRenderable
            // keeps that prefab when it has a mesh, so Hier never won. Approved meshes are
            // Tan runner and Orange It. The catalog covers player builds; the editor path
            // loads the same FBX when the catalog ref is empty. Player and bot share this path.
            GameObject hierRunner = HierPrefab(false);
            GameObject hierIt = HierPrefab(true);
#if UNITY_EDITOR
            var assignedRunner = runnerVisualPrefab;
            var assignedIt = itVisualPrefab;
            // Catalog PPtrs use Prefab fileID 100100000 on an FBX guid. Those resolve
            // null. Load the model root by path. A renderer on the asset is not required —
            // imported model roots often report none until they are instantiated.
            if (hierRunner == null)
                hierRunner = LoadHiPoly("Dummy_Mannequin_Tan_Hier_Hi.fbx");
            if (hierIt == null)
                hierIt = LoadHiPoly("Dummy_Mannequin_Orange_Hier_Hi.fbx");
#endif
            if (preferMannequinOverRunnerIt && hierRunner != null)
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

            if (preferMannequinOverRunnerIt && hierIt != null)
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

        void StripPlaceholderBody()
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
                    {
                        r.enabled = false;
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf != null) mf.sharedMesh = null;
                    }
                }
            }
            var rootMr = GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;
            var rootMf = GetComponent<MeshFilter>();
            if (rootMf != null) rootMf.sharedMesh = null;
            if (_it != null) _it.ReleaseRootAccent();
        }

        void LateUpdate()
        {
            // Revive() re-enables the serialized capsule accent after Awake.
            StripPlaceholderBody();
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
            _visualInstance = null;

            bool usedPrimitive = false;
            if (!forcePrimitiveMannequin)
                _visualInstance = SpawnBindable(prefab, asIt);
#if UNITY_EDITOR
            // Asset-level PrefabHasRenderer misses FBX roots. If the assigned prefab was the
            // flat Dummy_Runner, instantiate the approved Hier and keep it when the bones bind.
            if (_visualInstance == null && !forcePrimitiveMannequin)
            {
                var retry = LoadHiPoly(asIt
                    ? "Dummy_Mannequin_Orange_Hier_Hi.fbx"
                    : "Dummy_Mannequin_Tan_Hier_Hi.fbx");
                if (retry != null && retry != prefab)
                {
                    _visualInstance = SpawnBindable(retry, asIt);
                    if (_visualInstance != null) prefab = retry;
                }
            }
#endif
            if (_visualInstance == null)
            {
                if (prefab != null && fallbackToPrimitiveIfUnbound)
                    Debug.Log($"[DummyAvatarBinder] '{prefab.name}' has no hierarchical limb bones — using Navy Spade primitive.");
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

            StripPlaceholderBody();
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

        GameObject SpawnBindable(GameObject source, bool asIt)
        {
            if (source == null) return null;
            var inst = Instantiate(source, transform);
            inst.name = asIt ? "DummyVisual_It" : "DummyVisual_Runner";
            inst.transform.localPosition = visualLocalPosition;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = visualLocalScale;
            foreach (var cc in inst.GetComponentsInChildren<CharacterController>(true))
                Destroy(cc);
            foreach (var rb in inst.GetComponentsInChildren<Rigidbody>(true))
                Destroy(rb);

            // Flat Dummy_Runner has the limb names as siblings. Procedural swing cannot
            // move those. Keep the instance only when LowerArm sits under UpperArm.
            if (!fallbackToPrimitiveIfUnbound || DummyLocomotor.HasBindableBones(inst.transform))
            {
                ApplyCharacterMats(inst, asIt);
                FitVisual(inst);
                return inst;
            }

            Destroy(inst);
            return null;
        }

        void ApplyCharacterMats(GameObject visual, bool asIt)
        {
            // Hier FBX paint is Phong / Standard. Under URP that shader is magenta.
            // Keep a real URP material. Swap only broken slots, and keep their albedo
            // so Tan bone and Orange It do not collapse onto one chevron color.
            bool anyBroken = false;
            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                var shared = r.sharedMaterials;
                if (shared == null) continue;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (MaterialNeedsUrp(shared[i]))
                    {
                        anyBroken = true;
                        break;
                    }
                }
                if (anyBroken) break;
            }
            if (!anyBroken) return;

            Color body = asIt ? new Color(1f, 0.416f, 0f) : new Color(0.91f, 0.851f, 0.753f);
            Color accent = asIt ? new Color(0.04f, 0.04f, 0.04f) : new Color(0.169f, 0.702f, 0.639f);
            var fallback = asIt
                ? new[]
                {
                    itBaseMat ?? DummyPrimitiveFactory.MakeMat(body),
                    itAccentMat ?? DummyPrimitiveFactory.MakeMat(accent),
                    itOverrideMat ?? DummyPrimitiveFactory.MakeMat(body)
                }
                : new[]
                {
                    runnerBaseMat ?? DummyPrimitiveFactory.MakeMat(body),
                    runnerAccentMat ?? DummyPrimitiveFactory.MakeMat(accent),
                    runnerOverrideMat ?? DummyPrimitiveFactory.MakeMat(body)
                };

            foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r.gameObject.name.StartsWith("ItHat") || r.gameObject.name.StartsWith("ItHalo"))
                    continue;
                var shared = r.sharedMaterials;
                int n = shared != null && shared.Length > 0 ? shared.Length : 1;
                var next = new Material[n];
                for (int i = 0; i < n; i++)
                {
                    var src = shared != null && i < shared.Length ? shared[i] : null;
                    if (!MaterialNeedsUrp(src))
                    {
                        next[i] = src;
                        continue;
                    }
                    Color albedo = ReadAlbedo(src, fallback[Mathf.Min(i, fallback.Length - 1)].color);
                    next[i] = DummyPrimitiveFactory.MakeMat(albedo);
                }
                r.sharedMaterials = next;
            }
        }

        static bool MaterialNeedsUrp(Material m)
        {
            if (m == null) return true;
            if (m.name == "Default-Material") return true;
            var shader = m.shader;
            if (shader == null) return true;
            string n = shader.name;
            if (n.IndexOf("Universal Render Pipeline", System.StringComparison.Ordinal) >= 0)
                return false;
            return true;
        }

        static Color ReadAlbedo(Material m, Color fallback)
        {
            if (m == null || m.name == "Default-Material") return fallback;
            // Standard / Phong expose _Color. URP slots that we keep are not read here.
            if (m.HasProperty("_Color") || m.HasProperty("_BaseColor"))
                return m.color;
            return fallback;
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
            if (visual == null || visual.transform.parent == null) return;
            float h = BodyHeight(visual);
            // Unit-scale misses only. A standing Hier is already ~1.9 m.
            if (h > 0.15f && h < 80f && (h < 1.15f || h > 2.7f))
                visual.transform.localScale *= 1.8f / h;

            // Foot meshes (or the ankle bone). A torso-only bound used to plant the
            // hips on the pad and leave the body in the ground.
            float sole = LowestFootSole(visual);
            if (float.IsNaN(sole)) return;
            float delta = sole - visual.transform.parent.position.y;
            if (Mathf.Abs(delta) > 0.03f && Mathf.Abs(delta) < 3f)
                visual.transform.localPosition -= new Vector3(0f, delta, 0f);
        }

        static float BodyHeight(GameObject visual)
        {
            var rends = visual.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) return 0f;
            bool any = false;
            Bounds b = default;
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                if (!any)
                {
                    b = rends[i].bounds;
                    any = true;
                }
                else b.Encapsulate(rends[i].bounds);
            }
            return any ? b.size.y : 0f;
        }

        static float LowestFootSole(GameObject visual)
        {
            float y = float.PositiveInfinity;
            var rends = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                if (r == null) continue;
                if (r.gameObject.name.IndexOf("Foot", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                y = Mathf.Min(y, r.bounds.min.y);
            }
            if (y < float.PositiveInfinity) return y;

            var bones = visual.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < bones.Length; i++)
            {
                string n = bones[i].name;
                if (n != "Foot_L" && n != "Foot_R" && n != "Foot.L" && n != "Foot.R")
                    continue;
                // Ankle sits a few centimeters above the sole.
                y = Mathf.Min(y, bones[i].position.y - 0.04f);
            }
            return y < float.PositiveInfinity ? y : float.NaN;
        }
    }
}
