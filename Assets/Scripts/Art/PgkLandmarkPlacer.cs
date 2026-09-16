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
    /// Mega campus dresser: one giant landmark per toy zone + big PGK playground
    /// structures with open space between them. Editor play-mode FBX resolve.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class PgkLandmarkPlacer : MonoBehaviour
    {
        const string HiPolyRel = "Assets/Art/Props/Playground/HiPoly";
        const string RootFolder = "_PgkLandmarks";

        static Dictionary<string, string> _stemCachePgk;
        static Dictionary<string, string> _stemCacheLandmark;
        static float _stemCacheTime;

        [SerializeField] bool placeLandmarks = true;
        /// <summary>OFF by default — graybox CutArenaBootstrap owns parkour; PGK dump stacked chaos on every pad.</summary>
        [SerializeField] bool placePgkStructures = false;
        [SerializeField] float landmarkUniformScale = 1.35f;
        [SerializeField] float pgkUniformScale = 1f;

        static readonly (string stem, Vector3 pos, float yaw, float scale)[] LandmarkSlots =
        {
            ("Landmark_CrashTorso_Hi", new Vector3(36f, 0f, 27f), 0f, 1.5f),
            ("Landmark_PirateMast_Hi", new Vector3(14f, 0f, 12f), 25f, 1.4f),
            ("Landmark_ArmyFoxhole_Hi", new Vector3(58f, 0f, 12f), -20f, 1.3f),
            ("Landmark_AstroHelmet_Hi", new Vector3(14f, 0f, 42f), 40f, 1.45f),
            ("Landmark_KnightShield_Hi", new Vector3(58f, 0f, 42f), 180f, 1.4f),
            ("Landmark_TronDisc_Hi", new Vector3(36f, 0f, 8f), 0f, 1.25f),
            ("Landmark_NinjaBladeRail_Hi", new Vector3(36f, 0f, 46f), 90f, 1.35f),
        };

        void Start() => Place();

        [ContextMenu("Place PGK + Landmarks")]
        public void Place()
        {
#if !UNITY_EDITOR
            Debug.LogWarning("[PgkLandmarkPlacer] FBX resolve needs Editor play mode.");
            return;
#else
            var park = transform.Find("PARK");
            if (park == null)
            {
                Debug.LogWarning("[PgkLandmarkPlacer] No PARK root.");
                return;
            }

            var folder = park.Find(RootFolder);
            if (folder != null)
                Destroy(folder.gameObject);

            var root = new GameObject(RootFolder).transform;
            root.SetParent(park, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            int landmarks = 0, pgk = 0;
            if (placeLandmarks)
                landmarks = PlaceLandmarks(root);
            if (placePgkStructures)
                pgk = PlaceAllStructures(root);

            Debug.Log($"[PgkLandmarkPlacer] mega campus landmarks={landmarks} pgkPieces={pgk}");
#endif
        }

#if UNITY_EDITOR
        int PlaceLandmarks(Transform root)
        {
            var byStem = LatestByStem("Landmark_*.fbx");
            int n = 0;
            foreach (var slot in LandmarkSlots)
            {
                if (!byStem.TryGetValue(slot.stem, out var path))
                {
                    Debug.LogWarning($"[PgkLandmarkPlacer] Missing {slot.stem}");
                    continue;
                }
                var go = SpawnFbx(path, root, slot.stem, slot.pos, slot.yaw, landmarkUniformScale * slot.scale);
                if (go != null) n++;
            }
            return n;
        }

        int PlaceAllStructures(Transform root)
        {
            int n = 0;
            n += BuildTowerCluster(root, "PGK_Crash_Fort", new Vector3(36f, 0f, 27f), 0f);
            n += BuildPirateRig(root, "PGK_Pirate_Rig", new Vector3(14f, 0f, 12f), 20f);
            n += BuildArmyBunker(root, "PGK_Army_Bunker", new Vector3(58f, 0f, 12f), -15f);
            n += BuildAstroLoft(root, "PGK_Astro_Loft", new Vector3(14f, 0f, 42f), 0f);
            n += BuildKnightKeep(root, "PGK_Knight_Keep", new Vector3(58f, 0f, 42f), 180f);
            n += BuildTronGrid(root, "PGK_Tron_Grid", new Vector3(36f, 0f, 8f), 0f);
            n += BuildNinjaCircuit(root, "PGK_Ninja_Circuit", new Vector3(36f, 0f, 46f), 0f);
            n += BuildConnectorToys(root);
            return n;
        }

        int BuildTowerCluster(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_3m_LOD0", new Vector3(-2f, 0f, -2f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(2f, 0f, -2f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(-2f, 0f, 2f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(2f, 0f, 2f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 1.20f, 0f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(-2f, 1.20f, -2f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(2f, 1.20f, -2f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(-2f, 1.20f, 2f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(2f, 1.20f, 2f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 2.00f, 0f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 2.00f, -2f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 2.00f, 2f), 0f),
                ("PGK_Rail_Corner90_LOD0", new Vector3(-2f, 2.00f, -2f), 0f),
                ("PGK_Stairs_5_LOD0", new Vector3(4.5f, 0f, 0f), 90f),
                ("PGK_Slide_Spiral270_LOD0", new Vector3(-5f, 0f, 0f), 0f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(0f, 1.20f, -5f), 180f),
                ("PGK_Monkey_4m_LOD0", new Vector3(0f, 0f, 6f), 0f),
                ("PGK_Dome_Geo_3m_LOD0", new Vector3(6f, 0f, 4f), 0f),
                ("PGK_Tunnel_Plastic_LOD0", new Vector3(-6f, 0f, 4f), 90f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildPirateRig(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(-1.5f, 0f, -1.5f), 0f),
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(1.5f, 0f, -1.5f), 0f),
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(-1.5f, 0f, 1.5f), 0f),
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(1.5f, 0f, 1.5f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 1.20f, 0f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(3f, 1.20f, 0f), 90f),
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(0f, 1.20f, 0f), 0f),
                ("PGK_Deck_1x1_LOD0", new Vector3(0f, 2.00f, 0f), 0f),
                ("PGK_Slide_Tube90_LOD0", new Vector3(-3f, 1.20f, 0f), -90f),
                ("PGK_Balance_Beam_3m_LOD0", new Vector3(5f, 0.40f, 0f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 1.20f, -1.5f), 0f),
                ("PGK_Stairs_5_LOD0", new Vector3(-4f, 0f, 2f), 0f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildArmyBunker(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Deck_Corner_L_LOD0", new Vector3(-2f, 0.40f, -2f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(1f, 0.40f, -2f), 0f),
                ("PGK_Deck_1x1_LOD0", new Vector3(2f, 0.40f, 0f), 0f),
                ("PGK_Post_Square_1m_LOD0", new Vector3(-3f, 0f, -3f), 0f),
                ("PGK_Post_Square_1m_LOD0", new Vector3(3f, 0f, -3f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(-3f, 0f, 2f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(3f, 0f, 2f), 0f),
                ("PGK_Rail_1m_LOD0", new Vector3(0f, 0.40f, -3f), 0f),
                ("PGK_Tunnel_Plastic_LOD0", new Vector3(0f, 0f, 3f), 0f),
                ("PGK_Spinner_StandOn_LOD0", new Vector3(5f, 0f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.01f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(1f, 0.01f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.01f, 1f), 0f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildAstroLoft(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_2m_LOD0", new Vector3(-3f, 0f, -3f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(3f, 0f, -3f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(-3f, 0f, 3f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(3f, 0f, 3f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 1.60f, 0f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(0f, 1.60f, -3f), 0f),
                ("PGK_Dome_Geo_3m_LOD0", new Vector3(0f, 1.60f, 0f), 0f),
                ("PGK_Slide_Tube90_LOD0", new Vector3(4f, 1.60f, 0f), 90f),
                ("PGK_Ladder_Rung_LOD0", new Vector3(-3f, 0f, 0f), 0f),
                ("PGK_Rail_Corner90_LOD0", new Vector3(-3f, 1.60f, -3f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 1.60f, 3f), 0f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildKnightKeep(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_3m_LOD0", new Vector3(-2.5f, 0f, -2.5f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(2.5f, 0f, -2.5f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(-2.5f, 0f, 2.5f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(2.5f, 0f, 2.5f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 1.60f, 0f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(-2.5f, 1.60f, -2.5f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(2.5f, 1.60f, -2.5f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(0f, 2.00f, 0f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 2.00f, -2.5f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 2.00f, 2.5f), 0f),
                ("PGK_Stairs_5_LOD0", new Vector3(5f, 0f, 0f), 90f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(-5f, 1.60f, 0f), -90f),
                ("PGK_Balance_Beam_3m_LOD0", new Vector3(0f, 0.80f, -5f), 0f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildTronGrid(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(-3f, 0f, -2f), 0f),
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(0f, 0f, -2f), 0f),
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(3f, 0f, -2f), 0f),
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(-3f, 0f, 2f), 0f),
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(0f, 0f, 2f), 0f),
                ("PGK_Post_Square_1_5m_LOD0", new Vector3(3f, 0f, 2f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(-1.5f, 1.20f, -2f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(1.5f, 1.20f, -2f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(-1.5f, 1.20f, 2f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(1.5f, 1.20f, 2f), 0f),
                ("PGK_Spinner_StandOn_LOD0", new Vector3(0f, 0f, 0f), 0f),
                ("PGK_Deck_1x1_LOD0", new Vector3(0f, 0.40f, 0f), 0f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildNinjaCircuit(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(-4f, 0f, 0f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(4f, 0f, 0f), 0f),
                ("PGK_Deck_1x1_LOD0", new Vector3(-4f, 2.00f, 0f), 0f),
                ("PGK_Deck_1x1_LOD0", new Vector3(4f, 1.60f, 0f), 0f),
                ("PGK_Balance_Beam_3m_LOD0", new Vector3(0f, 1.60f, 0f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 1.60f, 0f), 90f),
                ("PGK_Monkey_4m_LOD0", new Vector3(0f, 0f, 3f), 0f),
                ("PGK_Ladder_Rung_LOD0", new Vector3(-4f, 0f, 1f), 0f),
                ("PGK_Slide_Tube90_LOD0", new Vector3(4f, 1.60f, -2f), 180f),
            };
            return SpawnList(parent, pieces);
        }

        int BuildConnectorToys(Transform root)
        {
            // Sparse spine accents only — keep chase lanes readable (no tunnel/spinner clutter).
            var parent = MakeGroup(root, "PGK_Connectors", Vector3.zero, 0f);
            var pieces = new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(30f, 0.02f, 27f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(42f, 0.02f, 27f), 0f),
                ("PGK_Balance_Beam_3m_LOD0", new Vector3(36f, 0.40f, 27f), 0f),
            };
            return SpawnList(parent, pieces);
        }
        Transform MakeGroup(Transform root, string name, Vector3 origin, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = origin;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        int SpawnList(Transform parent, List<(string id, Vector3 p, float y)> pieces)
        {
            var byStem = LatestByStem("PGK_*.fbx");
            int n = 0;
            foreach (var p in pieces)
            {
                if (!TryResolve(byStem, p.id, out var path))
                {
                    Debug.LogWarning($"[PgkLandmarkPlacer] Missing PGK {p.id}");
                    continue;
                }
                var go = SpawnFbx(path, parent, p.id, p.p, p.y, pgkUniformScale);
                if (go != null) n++;
            }
            return n;
        }

        static bool TryResolve(Dictionary<string, string> byStem, string id, out string path)
        {
            if (byStem.TryGetValue(id, out path)) return true;
            var alt = id.Replace("_LOD0", "");
            foreach (var kv in byStem)
            {
                if (kv.Key == id || kv.Key.StartsWith(alt))
                {
                    path = kv.Value;
                    return true;
                }
            }
            path = null;
            return false;
        }

        GameObject SpawnFbx(string path, Transform parent, string name, Vector3 localPos, float yaw, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (go == null) go = Instantiate(prefab);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            StripImportCameras(go);
            StaticPropColliders.EnsureStaticColliders(go);
            return go;
        }

        static Dictionary<string, string> LatestByStem(string glob)
        {
            bool landmark = glob.StartsWith("Landmark");
            if (Time.realtimeSinceStartup - _stemCacheTime < 30f)
            {
                if (landmark && _stemCacheLandmark != null) return _stemCacheLandmark;
                if (!landmark && _stemCachePgk != null) return _stemCachePgk;
            }

            var dir = Path.Combine(Application.dataPath, "Art/Props/Playground/HiPoly");
            var map = new Dictionary<string, string>();
            if (!Directory.Exists(dir)) return map;

            foreach (var full in Directory.GetFiles(dir, glob))
            {
                var file = Path.GetFileName(full);
                var baseName = Path.GetFileNameWithoutExtension(file);
                string stem = baseName;
                if (baseName.StartsWith("Landmark_"))
                {
                    var parts = baseName.Split('_');
                    if (parts.Length >= 2 && parts[parts.Length - 1].All(char.IsDigit))
                        stem = string.Join("_", parts.Take(parts.Length - 1));
                }

                var rel = HiPolyRel + "/" + file;
                if (!map.TryGetValue(stem, out var existing))
                {
                    map[stem] = rel;
                    continue;
                }
                var exFull = Path.Combine(Application.dataPath, existing.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar));
                var tNew = File.GetLastWriteTimeUtc(full);
                var tOld = File.Exists(exFull) ? File.GetLastWriteTimeUtc(exFull) : System.DateTime.MinValue;
                if (tNew >= tOld) map[stem] = rel;
            }

            _stemCacheTime = Time.realtimeSinceStartup;
            if (landmark) _stemCacheLandmark = map;
            else _stemCachePgk = map;
            return map;
        }

        static void StripImportCameras(GameObject go)
        {
            foreach (var cam in go.GetComponentsInChildren<Camera>(true))
                Destroy(cam.gameObject);
            foreach (var al in go.GetComponentsInChildren<AudioListener>(true))
                Destroy(al);
            foreach (var l in go.GetComponentsInChildren<Light>(true))
                Destroy(l);
        }


#endif
    }
}
