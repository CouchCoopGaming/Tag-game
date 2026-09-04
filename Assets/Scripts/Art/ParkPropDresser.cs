using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// After CutArenaBootstrap builds PARK toys, dress named roots with 3D prop meshes from Resources/Props.
    /// Keeps collider/volume from graybox; hides graybox MeshRenderer when mesh prop loads.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class ParkPropDresser : MonoBehaviour
    {
        [SerializeField] bool hideGrayboxMeshWhenDressed = true;

        static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            { "Toy_Bars_0", "Props/Toy_Bars" },
            { "Toy_Bars_1", "Props/Toy_Bars" },
            { "Toy_Bars_2", "Props/Toy_Bars" },
            { "Toy_Bench_South_A", "Props/Toy_Bench" },
            { "Toy_Bench_South_B", "Props/Toy_Bench" },
            { "Toy_Bench_Chain1", "Props/Toy_Bench" },
            { "Toy_Bench_SouthOff", "Props/Toy_Bench" },
            { "Toy_Picnic_MidCut_S", "Props/Toy_PicnicTable" },
            { "Toy_Picnic_MidCut_N", "Props/Toy_PicnicTable" },
            { "Toy_Picnic_Island_NW", "Props/Toy_PicnicTable" },
            { "Toy_Picnic_Island_NE", "Props/Toy_PicnicTable" },
            { "Toy_Slide_C1", "Props/Toy_Slide" },
            { "Toy_Tower", "Props/Toy_Tower" },
            { "Toy_ClimbWall_West", "Props/Toy_WallPanel" },
            { "Toy_ClimbWall_SW", "Props/Toy_WallPanel" },
            { "Toy_TwinTower_W", "Props/Toy_WallPanel" },
            { "Toy_TwinTower_E", "Props/Toy_WallPanel" },
            { "Toy_TwinTower_SE_W", "Props/Toy_WallPanel" },
            { "Toy_TwinTower_SE_E", "Props/Toy_WallPanel" },
            { "Toy_VaultRail_hint", "Props/Toy_VaultRail_090" },
        };

        void Start()
        {
            // Delay one frame so CutArenaBootstrap.Awake Build() finished
            Dress();
        }

        [ContextMenu("Dress PARK Props")]
        public void Dress()
        {
            var park = transform.Find("PARK");
            if (park == null) park = transform;
            int dressed = 0;
            foreach (var t in park.GetComponentsInChildren<Transform>(true))
            {
                if (!Map.TryGetValue(t.name, out var resPath)) continue;
                if (t.Find("PropMesh") != null) continue;
                var prefab = Resources.Load<GameObject>(resPath);
                if (prefab == null) continue;
                var go = Instantiate(prefab, t);
                go.name = "PropMesh";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                // Fit roughly to parent graybox scale
                go.transform.localScale = Vector3.one;
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
                Debug.LogWarning("[ParkPropDresser] No props loaded — run Tag/Setup Hub Visuals to copy FBX into Resources/Props.");
        }
    }
}
