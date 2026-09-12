using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// After CutArenaBootstrap builds PARK toys, dress named roots with 3D prop
    /// meshes from Resources/Props prefabs (filled by Tag → Setup Hub Visuals).
    /// Keeps collider/volume from graybox; hides graybox MeshRenderer when dressed.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ParkPropDresser : MonoBehaviour
    {
        [SerializeField] bool hideGrayboxMeshWhenDressed = true;
        [SerializeField] Material matYellow;
        [SerializeField] Material matBlue;
        [SerializeField] Material matSteel;
        [SerializeField] Material matRubber;
        [SerializeField] Material matRed;
        [SerializeField] Material matConcrete;
        [SerializeField] Material matMulch;

        static readonly Dictionary<string, string> Map = new Dictionary<string, string>
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
            var park = transform.Find("PARK");
            if (park == null) park = transform;
            int dressed = 0;
            foreach (var t in park.GetComponentsInChildren<Transform>(true))
            {
                if (!Map.TryGetValue(t.name, out var propName)) continue;
                if (t.Find("PropMesh") != null) continue;
                var prefab = LoadProp(propName);
                if (prefab == null) continue;
                var go = Instantiate(prefab, t);
                go.name = "PropMesh";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                foreach (var col in go.GetComponentsInChildren<Collider>())
                    Destroy(col);
                ApplyPropMats(go, propName);
                FitToParent(go, t);
                if (hideGrayboxMeshWhenDressed)
                {
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                }
                dressed++;
            }
            if (dressed > 0)
                Debug.Log($"[ParkPropDresser] Dressed {dressed} toys with 3D props.");
            else
                Debug.LogWarning("[ParkPropDresser] No props loaded — run Tag → Setup Hub Visuals.");
        }

        static GameObject LoadProp(string name)
        {
            var prefab = Resources.Load<GameObject>("Props/" + name);
            return DummyPrimitiveFactory.PrefabHasRenderer(prefab) ? prefab : null;
        }

        void ApplyPropMats(GameObject go, string propName)
        {
            Material mat = matYellow;
            if (propName.StartsWith("Toy_WallPanel") || propName.StartsWith("Toy_Tower"))
                mat = matBlue ?? matSteel ?? matYellow;
            else if (propName.StartsWith("Toy_Slide") && go.transform.parent != null && go.transform.parent.name.Contains("Rubber"))
                mat = matRubber ?? matYellow;
            else if (propName.StartsWith("Toy_Bumper"))
                mat = matRed ?? matYellow;
            else if (propName.StartsWith("Toy_SpawnPad"))
                mat = matConcrete ?? matYellow;
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
