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
    /// Playground fantasy dresser: zone landmarks + chase-path structures
    /// (soft-play, named courts, wall-run faces, slide banks along figure-8 / ring).
    /// Pieces sit beside chase lanes — not on spine midlines.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class PgkLandmarkPlacer : MonoBehaviour
    {
        const string HiPolyRel = "Assets/Art/Props/Playground/HiPoly";
        const string RootFolder = "_PgkLandmarks";

        static Dictionary<string, string> _hiIndex;
        static float _hiIndexTime;

        [SerializeField] bool placeLandmarks = true;
        /// <summary>ON — sparse playground gear along chase paths (not dense pad dumps).</summary>
        [SerializeField] bool placePgkStructures = true;
        [SerializeField] float landmarkUniformScale = 1.15f;
        [SerializeField] float pgkUniformScale = 1f;

        // Edge-nudged landmarks — pad midpoints + Flow/Conn corridors stay clear.
        static readonly (string stem, Vector3 pos, float yaw, float scale)[] LandmarkSlots =
        {
            ("Landmark_CrashTorso_Hi", new Vector3(29f, 0f, 34.5f), -15f, 1.05f),
            ("Landmark_PirateMast_Hi", new Vector3(10f, 0f, 13.5f), 25f, 1.05f),
            ("Landmark_ArmyFoxhole_Hi", new Vector3(62f, 0f, 10f), -20f, 1.0f),
            ("Landmark_AstroHelmet_Hi", new Vector3(10f, 0f, 45f), 40f, 1.1f),
            ("Landmark_KnightShield_Hi", new Vector3(62f, 0f, 45f), 180f, 1.05f),
            ("Landmark_TronDisc_Hi", new Vector3(32f, 0f, 4.5f), 0f, 0.95f),
            ("Landmark_NinjaBladeRail_Hi", new Vector3(40f, 0f, 50f), 90f, 1.0f),
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

            // Same-frame clear so rebuild/context-menu Place does not leave orphaned Play_* colliders.
            var folder = park.Find(RootFolder);
            if (folder != null)
                DestroyImmediate(folder.gameObject);

            var root = new GameObject(RootFolder).transform;
            root.SetParent(park, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            int landmarks = 0, pgk = 0;
            if (placeLandmarks)
                landmarks = PlaceLandmarks(root);
            if (placePgkStructures)
                pgk = PlaceChasePlayground(root);

            Debug.Log($"[PgkLandmarkPlacer] chase playground landmarks={landmarks} pieces={pgk}");
#endif
        }

#if UNITY_EDITOR
        int PlaceLandmarks(Transform root)
        {
            int n = 0;
            foreach (var slot in LandmarkSlots)
            {
                if (!TryResolve(slot.stem, out var path))
                {
                    Debug.LogWarning($"[PgkLandmarkPlacer] Missing {slot.stem}");
                    continue;
                }
                var go = SpawnFbx(path, root, slot.stem, slot.pos, slot.yaw, landmarkUniformScale * slot.scale);
                if (go != null) n++;
            }
            return n;
        }

        /// <summary>
        /// Playground structures along figure-8 + outer ring.
        /// Named play areas sit beside chase lanes; ski midlines stay clear.
        /// </summary>
        int PlaceChasePlayground(Transform root)
        {
            int n = 0;
            // Succinct playground: fewer HiPoly clusters, grounded on mulch pads.
            n += SoftPlayPlaza(root, "Play_SoftPlay_CrashSW", new Vector3(19f, 0f, 13.5f), 0f);
            n += MerryGoRound(root, "Play_MerryGoRound", new Vector3(15.5f, 0f, 22f), 0f);
            n += SwingSet(root, "Play_Swing", new Vector3(60.5f, 0f, 33.5f), 0f);
            n += KickballField(root, "Play_Kickball", new Vector3(56.5f, 0f, 27f), 0f);
            n += HopscotchCourt(root, "Play_Hopscotch_SW", new Vector3(9.5f, 0f, 9f), 35f);
            // Outer ring S/N — monkey / slide / short wall face
            n += OuterRingSouth(root, "Play_Ring_S", new Vector3(36f, 0f, 5.5f), 0f);
            n += OuterRingNorth(root, "Play_Ring_N", new Vector3(36f, 0f, 49f), 0f);
            // Figure-8 loop wall-runs + one bank each (SW/NE); Ring_W/E dropped (Loop covers NS)
            n += LoopWallRun(root, "Play_Loop_W", new Vector3(19f, 0f, 27f), 0f);
            n += LoopWallRun(root, "Play_Loop_E", new Vector3(53f, 0f, 27f), 180f);
            n += SlideBank(root, "Play_Bank_SW", new Vector3(20f, 0f, 6f), 0f);
            n += SlideBank(root, "Play_Bank_NE", new Vector3(52f, 0f, 48f), 180f);
            // Pad-edge slides toward Conn / spines
            n += PadSlideExit(root, "Play_Slide_Pirate", new Vector3(16.5f, 0f, 12.0f), 25f);
            n += PadSlideExit(root, "Play_Slide_Army", new Vector3(55.5f, 0f, 12.0f), -25f);
            n += PadSlideExit(root, "Play_Slide_Astro", new Vector3(16.5f, 0f, 42.0f), 155f);
            n += PadSlideExit(root, "Play_Slide_Knight", new Vector3(55.5f, 0f, 42.0f), -155f);
            n += SpawnPlay_SW(root);
            n += SpawnPlay_SE(root);
            n += SpawnPlay_NW(root);
            n += SpawnPlay_NE(root);
            n += SpineAccents(root);
            return n;
        }

        int SoftPlayPlaza(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            // Readable chase path: climb -> decks -> tube -> Bay B exit.
            // Origin SW of SpineXw (24) / SpineZs (18) so Crash EW chase stays open.
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                // Compact tower on mulch: posts + deck + stairs + net + slide exit
                ("PGK_Post_Square_3m_LOD0", new Vector3(-2f, 0f, -2f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(2f, 0f, -2f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(-2f, 0f, 2f), 0f),
                ("PGK_Post_Square_3m_LOD0", new Vector3(2f, 0f, 2f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 1.15f, 0f), 0f),
                ("PGK_Stairs_5_LOD0", new Vector3(0f, 0f, -4f), 180f),
                ("Mega_ClimbNet", new Vector3(-3.6f, 0f, 0f), 90f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(0f, 1.15f, 3.6f), 0f),
                ("Mega_CrawlTunnel", new Vector3(0f, 0f, -5.2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 0f), 0f),
            });
        }

        int OuterRingSouth(Transform root, string name, Vector3 origin, float yaw)
        {
            // Tron arc — keep z south of SpineZs; walls face campus (+Z)
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Monkey_4m_LOD0", new Vector3(-8f, 0f, 0f), 0f),
                ("PGK_Monkey_4m_LOD0", new Vector3(8f, 0f, 0f), 0f),
                ("Mega_SlideTube", new Vector3(0f, 0f, -1.0f), 0f),
                ("Toy_WallPanel", new Vector3(-4f, 0f, 1.6f), 0f),
                ("Toy_WallPanel", new Vector3(4f, 0f, 1.6f), 0f),
                ("Toy_VaultRail_100", new Vector3(0f, 0f, 2.8f), 90f),
                ("PGK_Stairs_5_LOD0", new Vector3(-10f, 0f, -2f), 180f),
                ("PGK_Deck_1x2_LOD0", new Vector3(-10f, 1.2f, 0.4f), 0f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(-10f, 1.2f, 3.2f), 0f),
            });
        }

        int OuterRingNorth(Transform root, string name, Vector3 origin, float yaw)
        {
            // Ninja arc — keep z north of SpineZn; walls face campus (-Z)
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Monkey_4m_LOD0", new Vector3(-8f, 0f, 0f), 0f),
                ("PGK_Monkey_4m_LOD0", new Vector3(8f, 0f, 0f), 0f),
                ("Mega_CrawlTunnel", new Vector3(0f, 0f, 1.0f), 90f),
                ("Toy_WallPanel", new Vector3(-4f, 0f, -1.6f), 180f),
                ("Toy_WallPanel", new Vector3(4f, 0f, -1.6f), 180f),
                ("Toy_VaultRail_100", new Vector3(0f, 0f, -2.8f), 90f),
                ("PGK_Stairs_5_LOD0", new Vector3(10f, 0f, 2f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(10f, 1.2f, -0.4f), 0f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(10f, 1.2f, -3.2f), 180f),
            });
        }

        /// <summary>
        /// Figure-8 loop wall-run: vertical Toy_WallPanel face + vault rails + deck/slide exit.
        /// Local -X faces the NS spine; keep origin west of SpineXw / east of SpineXe.
        /// </summary>
        int LoopWallRun(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(0f, 0f, -4f), 0f),
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(0f, 0f, 4f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(0f, 1.35f, 0f), 90f),
                ("PGK_Stairs_5_LOD0", new Vector3(0f, 0f, -6.2f), 180f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(2.2f, 1.2f, 4.8f), 0f),
                ("Toy_WallPanel", new Vector3(-2.0f, 0f, -3f), 90f),
                ("Toy_WallPanel", new Vector3(-2.0f, 0f, 0f), 90f),
                ("Toy_WallPanel", new Vector3(-2.0f, 0f, 3f), 90f),
                ("Toy_VaultRail_100", new Vector3(2f, 0f, 0f), 0f),
                ("Mega_ParkourRamp", new Vector3(2.4f, 0f, -4.8f), 0f),
            });
        }

        /// <summary>
        /// Outer-ring wall-run strip: panels + vaults + mid deck, slide bank at +Z.
        /// </summary>
        int WallRunStrip(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_WallPanel", new Vector3(0f, 0f, -3.5f), 90f),
                ("Toy_WallPanel", new Vector3(0f, 0f, 0f), 90f),
                ("Toy_WallPanel", new Vector3(0f, 0f, 3.5f), 90f),
                ("Toy_VaultRail_100", new Vector3(2f, 0f, 0f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(0.4f, 0f, -1.2f), 0f),
                ("PGK_Post_Square_2m_LOD0", new Vector3(0.4f, 0f, 1.2f), 0f),
                ("PGK_Deck_1x2_LOD0", new Vector3(0.4f, 1.35f, 0f), 90f),
                ("PGK_Stairs_5_LOD0", new Vector3(0.4f, 0f, -4.2f), 180f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(0.4f, 1.35f, 4.0f), 0f),
            });
        }

        /// <summary>
        /// Compact slide bank: stairs → deck → slide, with a side wall to run/jump off.
        /// </summary>
        int SlideBank(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(-1.2f, 0f, 0f), 0f),
                ("PGK_Post_Square_2_5m_LOD0", new Vector3(1.2f, 0f, 0f), 0f),
                ("PGK_Deck_2x2_LOD0", new Vector3(0f, 1.4f, 0f), 0f),
                ("PGK_Stairs_5_LOD0", new Vector3(0f, 0f, -3.6f), 180f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(0f, 1.4f, 4f), 0f),
                ("Toy_WallPanel", new Vector3(-2.6f, 0f, 0f), 90f),
                ("Toy_VaultRail_100", new Vector3(2.6f, 0f, 0.5f), 0f),
                ("PGK_Rail_2m_LOD0", new Vector3(0f, 1.4f, -1.4f), 0f),
            });
        }

        /// <summary>Stairs (-Z) / deck / slide (+Z tip). Parent yaw aims tip onto Conn toward spines.</summary>
        int PadSlideExit(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Post_Square_2m_LOD0", new Vector3(0f, 0f, 0f), 0f),
                ("PGK_Deck_1x1_LOD0", new Vector3(0f, 1.2f, 0f), 0f),
                ("PGK_Slide_Straight_M_LOD0", new Vector3(0f, 1.2f, 3.4f), 0f),
                ("PGK_Stairs_5_LOD0", new Vector3(0f, 0f, -2.5f), 180f),
            });
        }

        int SpawnPlay_SW(Transform root)
        {
            // Teal spawn (6,5) → Pirate / SW jct — light lead toys (hopscotch is Play_Hopscotch_SW)
            var parent = MakeGroup(root, "Play_Spawn_SW", new Vector3(8f, 0f, 7f), 45f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_SpringRider", new Vector3(1.2f, 0f, -1.2f), 20f),
                ("Toy_Seesaw", new Vector3(-2.2f, 0f, 1.2f), 90f),
                ("Toy_Bumper", new Vector3(0f, 0f, 2.2f), 0f),
            });
        }

        int SpawnPlay_SE(Transform root)
        {
            var parent = MakeGroup(root, "Play_Spawn_SE", new Vector3(64f, 0f, 7f), -45f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_SpringRider", new Vector3(-1.2f, 0f, -1.2f), -20f),
                ("Toy_Seesaw", new Vector3(2.2f, 0f, 1.2f), 90f),
                ("Toy_Bumper", new Vector3(0f, 0f, 2.2f), 0f),
            });
        }

        int SpawnPlay_NW(Transform root)
        {
            var parent = MakeGroup(root, "Play_Spawn_NW", new Vector3(8f, 0f, 47f), 135f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Dome_Geo_3m_LOD0", new Vector3(-1f, 0f, -1f), 0f),
                ("Toy_NetFrame", new Vector3(2f, 0f, 0f), 0f),
                ("Toy_SpringRider", new Vector3(-2.5f, 0f, 1.5f), 40f),
            });
        }

        int SpawnPlay_NE(Transform root)
        {
            // Lead toys only — swings live in Play_Swing
            var parent = MakeGroup(root, "Play_Spawn_NE", new Vector3(64f, 0f, 47f), -135f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_SpringRider", new Vector3(2.2f, 0f, -1.2f), -40f),
                ("Toy_Seesaw", new Vector3(-2f, 0f, 1f), 0f),
                ("Toy_Bumper", new Vector3(0f, 0f, -2.2f), 0f),
            });
        }

        int MerryGoRound(Transform root, string name, Vector3 origin, float yaw)
        {
            // Classic spinner cluster — west lawn; east face open toward SpineXw
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Mega_Spinner", new Vector3(0f, 0f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 3.6f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(3.6f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, -3.6f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-3.6f, 0.02f, 0f), 0f),
                ("Toy_Bench", new Vector3(-5.5f, 0f, 0f), 90f),
            });
        }

        int SwingSet(Transform root, string name, Vector3 origin, float yaw)
        {
            // Classic swing frame — east lawn; west face open toward SpineXe
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_Bars", new Vector3(-3f, 0f, 0f), 0f),
                ("Toy_Bars", new Vector3(3f, 0f, 0f), 0f),
                ("Toy_Bars_Rail", new Vector3(0f, 0f, 0f), 90f),
                ("PGK_Monkey_4m_LOD0", new Vector3(0f, 0f, 3.6f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-2f, 0.02f, -2.8f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, -2.8f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(2f, 0.02f, -2.8f), 0f),
                ("Toy_Bench", new Vector3(6f, 0f, 0f), -90f),
            });
        }

        int KickballField(Transform root, string name, Vector3 origin, float yaw)
        {
            // Diamond between Loop_E and Ring_E — west face open (no benches) for figure-8
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_Goal", new Vector3(0f, 0f, -5.5f), 0f),
                ("Toy_Goal", new Vector3(0f, 0f, 5.5f), 180f),
                ("Toy_RubberTrack_C3", new Vector3(0f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, -4.2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(3.2f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 4.2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-3.2f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 0f), 0f),
                ("Toy_Bench", new Vector3(5.5f, 0f, 0f), -90f),
            });
        }

        int HopscotchCourt(Transform root, string name, Vector3 origin, float yaw)
        {
            // Tile chain centered on rubber pad; bench on outer flank (away from campus)
            var parent = MakeGroup(root, name, origin, yaw);
            int n = Hopscotch(parent, new Vector3(0f, 0.02f, -3.6f), 0f);
            n += SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_Bench", new Vector3(-3.6f, 0f, 0f), 90f),
            });
            return n;
        }

        int Hopscotch(Transform parent, Vector3 start, float yaw)
        {
            // Classic 1-2-3 / pair / 6 / pair / home — slight gaps so squares read at WorldScale 10
            var tiles = new List<(string id, Vector3 p, float y)>();
            const float step = 1.15f;
            const float pair = 0.65f;
            float[] xs = { 0f, 0f, 0f, -pair, pair, 0f, -pair, pair, 0f };
            float[] zs = { 0f, step, 2f * step, 3f * step, 3f * step, 4f * step, 5f * step, 5f * step, 6f * step };
            for (int i = 0; i < xs.Length; i++)
            {
                var local = Quaternion.Euler(0f, yaw, 0f) * new Vector3(xs[i], 0f, zs[i]);
                tiles.Add(("PGK_Safety_Tile_1m_LOD0", start + local, yaw));
            }
            return SpawnList(parent, tiles);
        }

        int SpineAccents(Transform root)
        {
            var parent = MakeGroup(root, "Play_SpineAccents", Vector3.zero, 0f);
            // Off cardinal midlines — beside SpineXw/Xe and EW spines
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Balance_Beam_3m_LOD0", new Vector3(21f, 0f, 18f), 90f),
                ("PGK_Balance_Beam_3m_LOD0", new Vector3(51f, 0f, 36f), 90f),
                ("Toy_VaultRail_100", new Vector3(21f, 0f, 27f), 0f),
                ("Toy_VaultRail_100", new Vector3(51f, 0f, 27f), 0f),
                ("Toy_WallPanel", new Vector3(21.5f, 0f, 26.5f), 90f),
                ("Toy_WallPanel", new Vector3(50.5f, 0f, 27.5f), 90f),
            });
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
            int n = 0;
            foreach (var p in pieces)
            {
                if (!TryResolve(p.id, out var path))
                {
                    Debug.LogWarning($"[PgkLandmarkPlacer] Missing {p.id}");
                    continue;
                }
                var go = SpawnFbx(path, parent, p.id, p.p, p.y, pgkUniformScale);
                if (go != null) n++;
            }
            return n;
        }

        static bool TryResolve(string id, out string path)
        {
            var map = IndexHiPoly();
            if (map.TryGetValue(id, out path)) return true;
            // Common aliases: request without _LOD0 / _Hi
            var alt = id.Replace("_LOD0", "");
            if (map.TryGetValue(alt, out path)) return true;
            if (map.TryGetValue(alt + "_Hi", out path)) return true;
            foreach (var kv in map)
            {
                if (kv.Key == id || kv.Key.StartsWith(alt) || alt.StartsWith(kv.Key))
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
            // Author Y = support/attach plane; stem offset fixes FBX pivot quirks (slide mouth, stair tread).
            float authoredY = localPos.y;
            localPos.y += StemSeatYOffset(name);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            StripImportCameras(go);
            SnapFeetToLocalY(go, authoredY);
            // Strip import/prefab colliders so Ensure can rebuild same-frame (ParkPropDresser path).
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);
            StaticPropColliders.EnsureStaticColliders(go);
            return go;
        }

        /// <summary>
        /// Name-based pivot seating. Author local Y means mulch feet or deck/mouth attach.
        /// Measured Unity AABB: Straight slide mouth ~+1.77, Spiral feet ~+0.51, Stairs tread0 ~+0.06.
        /// Decks/posts are bottom-pivoted (0). Coarse — tune in Play if needed.
        /// </summary>
        static float StemSeatYOffset(string stem)
        {
            if (string.IsNullOrEmpty(stem)) return 0f;
            if (stem.StartsWith("PGK_Slide_Straight")) return -1.77f; // mouth -> authored Y
            if (stem.StartsWith("PGK_Slide_Spiral")) return -0.51f;   // feet -> authored Y
            if (stem.StartsWith("PGK_Stairs")) return -0.06f;         // first tread -> mulch
            return 0f;
        }

        /// <summary>
        /// Seat ground props on mulch/lawn. authoredY is the support plane (before stem offset).
        /// Ground-only + sink-biased: never lift overhanging slides/stairs (AABB below pivot
        /// would otherwise float the whole assembly off the deck).
        /// </summary>
        static void SnapFeetToLocalY(GameObject go, float localGroundY)
        {
            if (go == null || go.transform.parent == null) return;
            // Elevated authored Y (decks / slide mouths) keeps placement; only mulch seating.
            if (localGroundY > 0.08f) return;
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends == null || rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                b.Encapsulate(rends[i].bounds);
            // Parent yaw-only — world Y of support is uniform across local XZ.
            float targetY = go.transform.parent.TransformPoint(new Vector3(0f, localGroundY, 0f)).y;
            float dy = targetY - b.min.y;
            if (Mathf.Abs(dy) < 0.001f) return;
            // Sink floaters; allow tiny upward for pivot noise; refuse larger lifts.
            if (dy > 0.015f) return;
            var lp = go.transform.localPosition;
            lp.y += dy;
            go.transform.localPosition = lp;
        }

        static Dictionary<string, string> IndexHiPoly()
        {
            if (_hiIndex != null && Time.realtimeSinceStartup - _hiIndexTime < 30f)
                return _hiIndex;

            var dir = Path.Combine(Application.dataPath, "Art/Props/Playground/HiPoly");
            var map = new Dictionary<string, string>();
            if (!Directory.Exists(dir)) return map;

            foreach (var full in Directory.GetFiles(dir, "*.fbx"))
            {
                var file = Path.GetFileName(full);
                var baseName = Path.GetFileNameWithoutExtension(file);
                foreach (var stem in StemsFor(baseName))
                {
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
            }

            _hiIndexTime = Time.realtimeSinceStartup;
            _hiIndex = map;
            return map;
        }

        /// <summary>
        /// Toy_Goal_Hi_000219 → Toy_Goal_Hi_000219, Toy_Goal_Hi, Toy_Goal
        /// Mega_ClimbNet_Hi_205615 → Mega_ClimbNet_Hi_205615, Mega_ClimbNet_Hi, Mega_ClimbNet
        /// PGK_Slide_Straight_M_LOD0 → full + without _LOD0
        /// Landmark_TronDisc_Hi → Landmark_TronDisc_Hi, Landmark_TronDisc
        /// </summary>
        static IEnumerable<string> StemsFor(string baseName)
        {
            yield return baseName;
            var s = baseName;
            if (s.EndsWith("_LOD0"))
            {
                s = s.Substring(0, s.Length - 5);
                yield return s;
            }
            var parts = s.Split('_');
            // drop trailing pure-digit token (export stamp)
            if (parts.Length >= 2 && parts[parts.Length - 1].All(char.IsDigit))
            {
                s = string.Join("_", parts.Take(parts.Length - 1));
                yield return s;
                parts = s.Split('_');
            }
            // drop trailing Hi
            if (parts.Length >= 2 && parts[parts.Length - 1] == "Hi")
            {
                s = string.Join("_", parts.Take(parts.Length - 1));
                yield return s;
            }
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
