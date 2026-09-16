using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tag.Art
{
    /// <summary>
    /// Dresses PARK graybox toys with HiPoly FBX (Toy_/Mega_) and Resources props.
    /// Editor play-mode resolves Assets/Art/Props/Playground/HiPoly.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ParkPropDresser : MonoBehaviour
    {
        const string HiPolyRel = "Assets/Art/Props/Playground/HiPoly";

        [SerializeField] bool hideGrayboxMeshWhenDressed = true;
        [SerializeField] bool preferHiPolyFbx = true;
        [SerializeField] Material matYellow;
        [SerializeField] Material matBlue;
        [SerializeField] Material matSteel;
        [SerializeField] Material matRubber;
        [SerializeField] Material matRed;
        [SerializeField] Material matConcrete;
        [SerializeField] Material matMulch;

        // graybox name → HiPoly stem (without _Hi / timestamp)
        static readonly Dictionary<string, string> HiMap = new Dictionary<string, string>
        {
            { "Toy_Bars_0", "Toy_Bars" },
            { "Toy_Bars_1", "Toy_Bars" },
            { "Toy_Bars_2", "Toy_Bars" },
            { "Toy_Bench_South_A", "Toy_Bench" },
            { "Toy_Bench_South_B", "Toy_Bench" },
            { "Toy_Bench_Chain1", "Toy_Bench" },
            { "Toy_Bench_SouthOff", "Toy_Bench" },
            { "Toy_Picnic_MidCut_S", "Toy_PicnicTable" },
            { "Toy_Picnic_MidCut_N", "Toy_PicnicTable" },
            { "Toy_Picnic_Island_NW", "Toy_PicnicTable" },
            { "Toy_Picnic_Island_NE", "Toy_PicnicTable" },
            { "Toy_Slide_C1", "Toy_Slide" },
            { "Toy_RubberTrack_C3", "Toy_RubberTrack_C3" },
            { "Toy_Tower", "Toy_Tower_Ultra" },
            { "Toy_TowerLip", "Toy_VaultRail_100" },
            { "Toy_ClimbWall_West", "Toy_WallPanel" },
            { "Toy_ClimbWall_SW", "Toy_WallPanel" },
            { "Toy_TwinTower_W", "Toy_Tower" },
            { "Toy_TwinTower_E", "Toy_Tower" },
            { "Toy_TwinTower_SE_W", "Toy_Tower" },
            { "Toy_TwinTower_SE_E", "Toy_Tower" },
            { "Toy_VaultRail_hint", "Toy_VaultRail_090" },
            { "Toy_SandboxRim_S", "Toy_VaultRail_100" },
            { "Toy_SandboxRim_N", "Toy_VaultRail_100" },
            { "Toy_SandboxRim_W", "Toy_VaultRail_100" },
            { "Toy_SandboxRim_E", "Toy_VaultRail_100" },
            { "Toy_Hedge_SW", "Toy_HedgeElbow" },
            { "Toy_Hedge_SE", "Toy_HedgeElbow" },
            { "Toy_Hedge_NW", "Toy_HedgeElbow" },
            { "Toy_Hedge_NE", "Toy_HedgeElbow" },
            { "Toy_Hedge_Crash", "Toy_HedgeElbow" },
            { "Toy_Hedge_Pirate", "Toy_HedgeElbow" },
            { "Spawn_SW", "Toy_SpawnPad_Teal" },
            { "Spawn_SE", "Toy_SpawnPad_Coral" },
            { "Spawn_NW", "Toy_SpawnPad_Violet" },
            { "Spawn_NE", "Toy_SpawnPad_Lime" },
            // Mega campus zone parts
            { "MastBase", "Toy_Tower" },
            { "Deck_Low", "Toy_Platform" },
            { "Deck_High", "Toy_Platform" },
            { "Plank_Run", "Toy_BalanceBeam" },
            { "ClimbNetWall", "Mega_ClimbNet" },
            { "Slide_Ramp", "Mega_ParkourRamp" },
            { "Bunker_A", "Toy_Crate" },
            { "Bunker_B", "Toy_Crate" },
            { "FoxholeTrench", "Toy_Ramp" },
            { "Ramp_Up", "Mega_ParkourRamp" },
            { "Wall_Cover", "Toy_WallPanel" },
            { "Vault_Low", "Toy_VaultRail_090" },
            { "Loft_Ring", "Mega_SkyBridge" },
            { "VisorPipe_A", "Toy_TunnelTube" },
            { "VisorPipe_B", "Toy_TunnelTube" },
            { "HalfPipe_L", "Mega_SlideTube" },
            { "HalfPipe_R", "Mega_SlideTube" },
            { "Ladder_Stub", "Toy_Ladder" },
            { "Courtyard", "Toy_Platform" },
            { "ShieldWall_N", "Toy_WallPanel" },
            { "ShieldWall_W", "Toy_WallPanel" },
            { "Keep_Tower", "Mega_TowerFort" },
            { "Battlement", "Toy_Platform" },
            { "VaultGate", "Toy_VaultRail_105" },
            { "Ramp_Keep", "Mega_ParkourRamp" },
            { "NeonTube_EW", "Toy_Bars_Rail" },
            { "DiscPad", "Toy_Spinner" },
            { "WallRun_S", "Toy_WallPanel" },
            { "SilentTower_A", "Mega_TowerFort" },
            { "SilentTower_B", "Toy_Tower" },
            { "BladeRail", "Toy_VaultRail_090" },
            { "BladeRail_High", "Toy_VaultRail_100" },
            { "ClimbFace", "Toy_WallPanel" },
            { "LandingDeck", "Toy_Platform" },
            { "Toy_Sandbox", "Mat_Mulch_Plane" },
        };

        // Resources fallback (Setup Hub Visuals)
        static readonly Dictionary<string, string> ResourceMap = new Dictionary<string, string>
        {
            { "Toy_Bars_0", "Toy_Bars" },
            { "Toy_Bars_1", "Toy_Bars" },
            { "Toy_Bars_2", "Toy_Bars" },
            { "Toy_Bench_South_A", "Toy_Bench" },
            { "Toy_Bench_South_B", "Toy_Bench" },
            { "Toy_Bench_Chain1", "Toy_Bench" },
            { "Toy_Bench_SouthOff", "Toy_Bench" },
            { "Toy_Picnic_MidCut_S", "Toy_PicnicTable" },
            { "Toy_Picnic_MidCut_N", "Toy_PicnicTable" },
            { "Toy_Picnic_Island_NW", "Toy_PicnicTable" },
            { "Toy_Picnic_Island_NE", "Toy_PicnicTable" },
            { "Toy_Slide_C1", "Toy_Slide" },
            { "Toy_RubberTrack_C3", "Toy_Slide" },
            { "Toy_Tower", "Toy_Tower" },
            { "Toy_ClimbWall_West", "Toy_WallPanel" },
            { "Toy_ClimbWall_SW", "Toy_WallPanel" },
            { "Toy_TwinTower_W", "Toy_WallPanel" },
            { "Toy_TwinTower_E", "Toy_WallPanel" },
            { "Toy_TwinTower_SE_W", "Toy_WallPanel" },
            { "Toy_TwinTower_SE_E", "Toy_WallPanel" },
            { "Toy_VaultRail_hint", "Toy_VaultRail_090" },
            { "Toy_SandboxRim_S", "Toy_VaultRail_100" },
            { "Toy_SandboxRim_N", "Toy_VaultRail_100" },
            { "Toy_SandboxRim_W", "Toy_VaultRail_100" },
            { "Toy_SandboxRim_E", "Toy_VaultRail_100" },
            { "Toy_Hedge_SW", "Toy_Bumper" },
            { "Toy_Hedge_SE", "Toy_Bumper" },
            { "Toy_Hedge_NW", "Toy_Bumper" },
            { "Toy_Hedge_NE", "Toy_Bumper" },
            { "Spawn_SW", "Toy_SpawnPad_Teal" },
            { "Spawn_SE", "Toy_SpawnPad_Coral" },
            { "Spawn_NW", "Toy_SpawnPad_Violet" },
            { "Spawn_NE", "Toy_SpawnPad_Lime" },
        };

        Dictionary<string, string> _hiPolyLatest;

        void Start()
        {
            ResolveMats();
            Dress();
        }

        void ResolveMats()
        {
            if (matYellow == null) matYellow = Resources.Load<Material>("Props/Mat_Park_Yellow");
            if (matBlue == null) matBlue = Resources.Load<Material>("Props/Mat_Park_Blue");
            if (matSteel == null) matSteel = Resources.Load<Material>("Props/Mat_Park_Steel");
            if (matRubber == null) matRubber = Resources.Load<Material>("Props/Mat_Park_Rubber");
            if (matRed == null) matRed = Resources.Load<Material>("Props/Mat_Park_Red");
            if (matConcrete == null) matConcrete = Resources.Load<Material>("Props/Mat_Park_Concrete");
            if (matMulch == null) matMulch = Resources.Load<Material>("Props/Mat_Park_Mulch");
        }

        [ContextMenu("Dress PARK Props")]
        public void Dress()
        {
            ResolveMats();
            CacheHiPolyIndex();

            var park = transform.Find("PARK");
            if (park == null) park = transform;
            int dressed = 0;

            foreach (var t in park.GetComponentsInChildren<Transform>(true))
            {
                if (t.Find("PropMesh") != null) continue;
                // Skip PGK / landmark folder — already HiPoly
                if (IsUnder(t, "_PgkLandmarks")) continue;
                // Keep layout lanes/spines as graybox — do not dress over chase highways
                if (t.name.StartsWith("Lane_") || t.name.StartsWith("Spine_") || t.name.StartsWith("Lawn_") || t.name.StartsWith("Flow_") || t.name.StartsWith("Conn_") || t.name.StartsWith("OOB_") || t.name.StartsWith("SpawnLead_") || t.name.StartsWith("EdgeRail_") || t.name.StartsWith("Play_")) continue;

                GameObject prefab = null;
                string propKey = null;

                if (preferHiPolyFbx && HiMap.TryGetValue(t.name, out var hiStem))
                {
                    prefab = LoadHiPolyStem(hiStem);
                    propKey = hiStem;
                }

                if (prefab == null && ResourceMap.TryGetValue(t.name, out var resName))
                {
                    prefab = LoadResourceProp(resName);
                    propKey = resName;
                }

                // Heuristic: GridPost_* → Toy_Bars
                if (prefab == null && t.name.StartsWith("GridPost_"))
                {
                    prefab = LoadHiPolyStem("Toy_Bars") ?? LoadResourceProp("Toy_Bars");
                    propKey = "Toy_Bars";
                }

                if (prefab == null) continue;

                var go = Instantiate(prefab, t);
                go.name = "PropMesh";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                // Strip import/prefab colliders immediately so Ensure can rebuild same-frame.
                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);
                if (propKey != null)
                    ApplyPropMats(go, propKey);
                FitToParent(go, t);
                StaticPropColliders.EnsureStaticColliders(go);
                // Prefer visual colliders - disable graybox host collider when PropMesh has any.
                if (go.GetComponentInChildren<Collider>() != null)
                {
                    var hostCol = t.GetComponent<Collider>();
                    if (hostCol != null) hostCol.enabled = false;
                }
                if (hideGrayboxMeshWhenDressed)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                }
                dressed++;
            }

            if (dressed > 0)
                Debug.Log($"[ParkPropDresser] Dressed {dressed} toys with HiPoly/Resources props.");
            else
                Debug.LogWarning("[ParkPropDresser] No props loaded — check HiPoly folder or Tag → Setup Hub Visuals.");
        }

        static bool IsUnder(Transform t, string folderName)
        {
            while (t != null)
            {
                if (t.name == folderName) return true;
                t = t.parent;
            }
            return false;
        }

        void CacheHiPolyIndex()
        {
            _hiPolyLatest = new Dictionary<string, string>();
#if UNITY_EDITOR
            var dir = Path.Combine(Application.dataPath, "Art/Props/Playground/HiPoly");
            if (!Directory.Exists(dir)) return;
            foreach (var full in Directory.GetFiles(dir, "*.fbx"))
            {
                var file = Path.GetFileName(full);
                var baseName = Path.GetFileNameWithoutExtension(file);
                // Toy_Bridge_Hi_001129 → Toy_Bridge ; Mega_TowerFort_Hi_205441 → Mega_TowerFort
                string stem = baseName;
                if (stem.EndsWith("_Hi"))
                    stem = stem.Substring(0, stem.Length - 3);
                else if (stem.Contains("_Hi_"))
                {
                    int idx = stem.IndexOf("_Hi_");
                    stem = stem.Substring(0, idx);
                }
                // drop trailing _digits
                var parts = stem.Split('_');
                if (parts.Length >= 2 && parts[parts.Length - 1].All(char.IsDigit))
                    stem = string.Join("_", parts.Take(parts.Length - 1));

                var rel = HiPolyRel + "/" + file;
                if (!_hiPolyLatest.TryGetValue(stem, out var existing))
                {
                    _hiPolyLatest[stem] = rel;
                    continue;
                }
                var exFull = Path.Combine(Application.dataPath, existing.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
                var tNew = File.GetLastWriteTimeUtc(full);
                var tOld = File.Exists(exFull) ? File.GetLastWriteTimeUtc(exFull) : System.DateTime.MinValue;
                if (tNew >= tOld) _hiPolyLatest[stem] = rel;
            }
#endif
        }

        GameObject LoadHiPolyStem(string stem)
        {
#if UNITY_EDITOR
            if (_hiPolyLatest == null) CacheHiPolyIndex();
            if (_hiPolyLatest != null && _hiPolyLatest.TryGetValue(stem, out var path))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (DummyPrimitiveFactory.PrefabHasRenderer(go)) return go;
            }
            // try exact filename without timestamp
            var exact = AssetDatabase.LoadAssetAtPath<GameObject>($"{HiPolyRel}/{stem}_Hi.fbx");
            if (DummyPrimitiveFactory.PrefabHasRenderer(exact)) return exact;
#endif
            return null;
        }

        static GameObject LoadResourceProp(string name)
        {
            var prefab = Resources.Load<GameObject>("Props/" + name);
            return DummyPrimitiveFactory.PrefabHasRenderer(prefab) ? prefab : null;
        }

        void ApplyPropMats(GameObject go, string propName)
        {
            // Keep authored FBX mats when present
            bool authored = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r != null && r.sharedMaterial != null &&
                    r.sharedMaterial.name != "Default-Material" &&
                    !r.sharedMaterial.name.StartsWith("CUT_"))
                {
                    authored = true;
                    break;
                }
            }
            if (authored) return;

            Material mat = matYellow;
            if (propName.Contains("Wall") || propName.Contains("Tower") || propName.StartsWith("Mega_"))
                mat = matBlue ?? matSteel ?? matYellow;
            else if (propName.Contains("Slide") || propName.Contains("Rubber") || propName.Contains("Tube"))
                mat = matRubber ?? matYellow;
            else if (propName.Contains("Bumper") || propName.Contains("Hedge"))
                mat = matRed ?? matYellow;
            else if (propName.Contains("Spawn") || propName.Contains("Pad"))
                mat = matConcrete ?? matYellow;
            else if (propName.Contains("Mulch"))
                mat = matMulch ?? matYellow;
            else
                mat = matYellow ?? matSteel;

            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }

        static void FitToParent(GameObject go, Transform parent)
        {
            var parentR = parent.GetComponent<Renderer>();
            var childRs = go.GetComponentsInChildren<Renderer>();
            if (parentR == null || childRs.Length == 0) return;
            var pb = parentR.bounds;
            var cb = childRs[0].bounds;
            for (int i = 1; i < childRs.Length; i++)
                cb.Encapsulate(childRs[i].bounds);
            if (cb.size.x < 0.01f || cb.size.y < 0.01f || cb.size.z < 0.01f) return;
            var scale = go.transform.localScale;
            scale.x *= pb.size.x / cb.size.x;
            scale.y *= pb.size.y / cb.size.y;
            scale.z *= pb.size.z / cb.size.z;
            go.transform.localScale = scale;
        }
    }
}
