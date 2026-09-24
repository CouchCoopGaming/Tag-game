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
    /// Playground campus: zone landmarks + connected kit districts.
    /// Soft-play / army / astro / knight forts, merry, swing, kickball, hopscotch,
    /// outer-ring monkey + tube runs, figure-8 wall-run towers.
    /// Deck heights stay on the PGK grid (0.40 / 0.80 / 1.20 / 1.60 / 2.00).
    /// Posts sit on the 1 m grid. Chase spine midlines stay clear.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class PgkLandmarkPlacer : MonoBehaviour
    {
        const string HiPolyRel = "Assets/Art/Props/Playground/HiPoly";
        const string RootFolder = "_PgkLandmarks";

        static Dictionary<string, string> _hiIndex;
        static float _hiIndexTime;

        [SerializeField] bool placeLandmarks = true;
        /// <summary>ON — connected kit districts along the chase (not graybox flow stones).</summary>
        [SerializeField] bool placePgkStructures = true;
        [SerializeField] float landmarkUniformScale = 1.15f;
        [SerializeField] float pgkUniformScale = 1f;

        // Sundeck deck grid. Stairs_5 top tread is 0.84, so a flight from 0 meets 0.80
        // and a flight from 0.80 meets 1.60. 0.40 is the stoop. 1.20 is unused:
        // a beam on it walks at ~1.61, which matches neither the 1.60 deck nor the spiral lip.
        const float Deck040 = 0.40f;
        const float Deck080 = 0.80f;
        const float Deck160 = 1.60f;
        const float Deck200 = 2.00f;

        /// <summary>
        /// Straight-slide mouth sits at authored Y (stem -1.77). The chute drops 1.91 m,
        /// so authored 1.91 puts the exit on mulch and the mouth just under a 2.00 deck lip.
        /// </summary>
        const float SlideGroundMouthY = 1.91f;

        /// <summary>
        /// Mouth band is ~1.10 m behind the pivot (local −Z). A 2.00 m offset from the
        /// tower center tucks that mouth ~0.10 m inside the 2×2 lip (0.25 m grid).
        /// </summary>
        const float SlideLipOffset = 2.00f;

        /// <summary>
        /// Exit band is ~1.91 m ahead of the pivot (~4.0 m from the tower). Three tiles
        /// carry the runout. Forts are shifted so the far edge stays ~0.4 m off the ski spines.
        /// </summary>
        const float SlideLandingNear = 3.75f;
        const float SlideLandingMid = 4.75f;
        const float SlideLandingFar = 5.75f;

        // Landmarks sit on open lawn, off kit decks and off ski spines.
        static readonly (string stem, Vector3 pos, float yaw, float scale)[] LandmarkSlots =
        {
            ("Landmark_CrashTorso_Hi", new Vector3(29f, 0f, 34.5f), -15f, 1.05f),
            ("Landmark_PirateMast_Hi", new Vector3(5f, 0f, 15f), 25f, 1.0f),
            ("Landmark_ArmyFoxhole_Hi", new Vector3(69f, 0f, 3f), -20f, 0.75f),
            ("Landmark_AstroHelmet_Hi", new Vector3(4f, 0f, 46f), 40f, 0.55f),
            ("Landmark_KnightShield_Hi", new Vector3(68f, 0f, 49f), 180f, 0.9f),
            ("Landmark_TronDisc_Hi", new Vector3(36f, 0f, 11.5f), 0f, 0.5f),
            ("Landmark_NinjaBladeRail_Hi", new Vector3(22f, 0f, 48f), 0f, 1.0f),
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
        /// Connected playground districts. Ski spines (x=24/48, z=18/36, 3.2 m wide) stay open.
        /// Fort slide mouths tuck inside the 2×2 lip; exits sit on a three-tile pit.
        /// Mega_SlideTube and PGK_Slide_Tube90 stay unspawned: the mega tube's floor
        /// rises ~3.4 m over 10 m, and Tube90's opening span is ~3.45 m. Neither end
        /// pair lands on the 0.40–2.00 deck grid without burying the low mouth.
        /// Horizontal Toy_TunnelTube / Mega_CrawlTunnel runs are the crawl instead.
        /// </summary>
        int PlaceChasePlayground(Transform root)
        {
            int n = 0;
            // West play places: tube street + spiral. East bunker/keep: one crawl trench + climb net.
            // z is set so a 3-tile slide pit (far edge +0.5 at local 5.75) stays ~0.4 m off the spine.
            n += KitFort(root, "Play_SoftPlay", new Vector3(14f, 0f, 9.75f), 0f, true);
            n += KitFort(root, "Play_ArmyBunker", new Vector3(58f, 0f, 9.75f), 0f, false);
            n += KitFort(root, "Play_AstroLoft", new Vector3(14f, 0f, 44.25f), 180f, true);
            n += KitFort(root, "Play_KnightKeep", new Vector3(58f, 0f, 44.25f), 180f, false);

            n += MerryGoRound(root, "Play_MerryGoRound", new Vector3(7f, 0f, 24f), 0f);
            n += SwingSet(root, "Play_Swing", new Vector3(67f, 0f, 31f), 0f);
            n += KickballField(root, "Play_Kickball", new Vector3(67f, 0f, 24f), 0f);
            n += HopscotchCourt(root, "Play_Hopscotch_SW", new Vector3(7f, 0f, 9f), 0f);
            n += HopscotchCourt(root, "Play_Hopscotch_SE", new Vector3(70f, 0f, 12f), 0f);
            n += HopscotchCourt(root, "Play_Hopscotch_NE", new Vector3(70f, 0f, 38f), 0f);
            // Overhead bars linking the west play places, and the east bunker to the keep.
            // Segments stop at the EW spines (x=11 and x=61 are inside the spine's x-range).
            n += MonkeyLane(root, "Play_Bars_W", 11f, new[] { 12.6f, 22f, 26.2f, 30.4f, 40.2f });
            n += MonkeyLane(root, "Play_Bars_E", 61f, new[] { 13.2f, 22f, 26.2f, 30.4f, 40.2f, 44.4f });

            // Outer ring: monkey run + tube/crawl + a deck tower whose slide feeds the ring lane.
            n += OuterRing(root, "Play_Ring_S", new Vector3(36f, 0f, 3f), true);
            n += OuterRing(root, "Play_Ring_N", new Vector3(36f, 0f, 51f), false);

            // Figure-8 alleys, staggered so the end tower and its stairs stay off both EW spines.
            // Slides exit across the alley (toward the NS spine) and stop short of the spine.
            n += LoopWallRun(root, "Play_Loop_W", new Vector3(16f, 0f, 25f), 6f, true);
            n += LoopWallRun(root, "Play_Loop_E", new Vector3(56f, 0f, 29f), -6f, false);

            n += SpawnPlay_SW(root);
            n += SpawnPlay_SE(root);
            n += SpawnPlay_NW(root);
            n += SpawnPlay_NE(root);
            return n;
        }

        /// <summary>
        /// Deck tower plus one ground annex. Local +Z is the slide exit.
        /// playPlace = tube street + spiral (soft-play / loft). Otherwise a bunker crawl + climb net.
        /// </summary>
        int KitFort(Transform root, string name, Vector3 origin, float yaw, bool playPlace)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            var pieces = new List<(string id, Vector3 p, float y)>();
            AddDeckTower(pieces, 0f, 0f, true);
            // Stoop on the 0.40 grid, beside the ground stair.
            pieces.Add(("PGK_Deck_1x1_LOD0", new Vector3(1.5f, Deck040, -2.5f), 0f));

            if (playPlace)
                AddPlayPlaceAnnex(pieces);
            else
                AddBunkerAnnex(pieces);

            for (int ix = -1; ix <= 1; ix++)
            for (int iz = -1; iz <= 1; iz++)
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(ix, 0.02f, iz), 0f));
            return SpawnList(parent, pieces);
        }

        /// <summary>
        /// McDonald's ground floor: spiral you step into off the 1.60 deck, then a tube street.
        /// Spiral at x=2.50 yaw 180: entrance overlaps the deck by 0.25 m. Corner posts
        /// (±1, ±1) have no mesh within 0.15 m. Plastic end caps insert ~0.36 m.
        /// North mouth at z=-3 collars the tube by ~0.31 m and stays ~0.28 m off the stair.
        /// </summary>
        static void AddPlayPlaceAnnex(List<(string id, Vector3 p, float y)> pieces)
        {
            pieces.Add(("PGK_Slide_Spiral270_LOD0", new Vector3(2.5f, 0f, -0.5f), 180f));
            pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(3f, 0.02f, 0.75f), 0f));
            pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(4f, 0.02f, 0.75f), 0f));

            // Street sits at z=-4 so the north mouth (z=-3) collars the rim by ~0.31 m
            // and stays ~0.28 m clear of the ground stair. End caps still insert ~0.36 m.
            pieces.Add(("Toy_TunnelTube", new Vector3(-2.5f, 0f, -4f), 0f));
            pieces.Add(("Toy_TunnelTube", new Vector3(0f, 0f, -4f), 0f));
            pieces.Add(("Toy_TunnelTube", new Vector3(2.5f, 0f, -4f), 0f));
            pieces.Add(("PGK_Tunnel_Plastic_LOD0", new Vector3(-4f, 0f, -4f), 0f));
            pieces.Add(("PGK_Tunnel_Plastic_LOD0", new Vector3(4f, 0f, -4f), 0f));
            pieces.Add(("PGK_Tunnel_Plastic_LOD0", new Vector3(0f, 0f, -3f), 0f));
            pieces.Add(("Mega_ClimbNet", new Vector3(-5f, 0f, -1.5f), 90f));
        }

        /// <summary>
        /// Bunker / keep ground floor: one 8 m crawl clear of the stair, climb net on the outer side.
        /// No second spiral — that annex is the west play places.
        /// </summary>
        static void AddBunkerAnnex(List<(string id, Vector3 p, float y)> pieces)
        {
            // South rim misses Spawn_SE (x≤60). North rim stays ~1 m off the ground stair.
            // Knight yaw 180 flips this clear of Spawn_NE.
            pieces.Add(("Mega_CrawlTunnel", new Vector3(-2f, 0f, -4.5f), 0f));
            pieces.Add(("Mega_ClimbNet", new Vector3(5f, 0f, -1.5f), 90f));
        }

        /// <summary>
        /// Posts on the 1 m corners, decks at 0.80 / 1.60 / 2.00, two stair flights, straight slide.
        /// Slide mouth tucks under the 2.00 deck; exit is on mulch (authored SlideGroundMouthY).
        /// </summary>
        static void AddDeckTower(List<(string id, Vector3 p, float y)> pieces, float cx, float cz, bool slidePositiveZ)
        {
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(cx - 1f, 0f, cz - 1f), 0f));
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(cx + 1f, 0f, cz - 1f), 0f));
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(cx - 1f, 0f, cz + 1f), 0f));
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(cx + 1f, 0f, cz + 1f), 0f));

            pieces.Add(("PGK_Deck_2x2_LOD0", new Vector3(cx, Deck080, cz), 0f));
            pieces.Add(("PGK_Deck_2x2_LOD0", new Vector3(cx, Deck160, cz), 0f));
            pieces.Add(("PGK_Deck_2x2_LOD0", new Vector3(cx, Deck200, cz), 0f));

            // Rails on the three closed edges. The slide face stays open.
            pieces.Add(("PGK_Rail_2m_LOD0", new Vector3(cx - 1f, Deck200, cz), 90f));
            pieces.Add(("PGK_Rail_2m_LOD0", new Vector3(cx + 1f, Deck200, cz), 90f));
            float closedZ = slidePositiveZ ? cz - 1f : cz + 1f;
            pieces.Add(("PGK_Rail_2m_LOD0", new Vector3(cx, Deck200, closedZ), 0f));

            // Ground flight meets the 0.80 deck. Upper flight starts on that deck and meets 1.60.
            if (slidePositiveZ)
            {
                pieces.Add(("PGK_Stairs_5_LOD0", new Vector3(cx, 0f, cz - 2f), 180f));
                pieces.Add(("PGK_Slide_Straight_M_LOD0", new Vector3(cx, SlideGroundMouthY, cz + SlideLipOffset), 0f));
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(cx, 0.02f, cz + SlideLandingNear), 0f));
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(cx, 0.02f, cz + SlideLandingMid), 0f));
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(cx, 0.02f, cz + SlideLandingFar), 0f));
            }
            else
            {
                pieces.Add(("PGK_Stairs_5_LOD0", new Vector3(cx, 0f, cz + 2f), 0f));
                pieces.Add(("PGK_Slide_Straight_M_LOD0", new Vector3(cx, SlideGroundMouthY, cz - SlideLipOffset), 180f));
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(cx, 0.02f, cz - SlideLandingNear), 0f));
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(cx, 0.02f, cz - SlideLandingMid), 0f));
                pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(cx, 0.02f, cz - SlideLandingFar), 0f));
            }
            // Side flight: yaw -90 climbs toward +X onto the west edge of the deck.
            pieces.Add(("PGK_Stairs_5_LOD0", new Vector3(cx - 2f, Deck080, cz), -90f));
        }

        /// <summary>
        /// South ring faces campus (+Z). North ring faces campus (-Z).
        /// Monkeys abut at 4.2 m. Wall panels abut at 1.6 m. Tube/crawl sits off the lane.
        /// </summary>
        int OuterRing(Transform root, string name, Vector3 origin, bool facePositiveZ)
        {
            var parent = MakeGroup(root, name, origin, 0f);
            var pieces = new List<(string id, Vector3 p, float y)>();

            float face = facePositiveZ ? 1f : -1f;
            for (int i = -1; i <= 1; i++)
                pieces.Add(("PGK_Monkey_4m_LOD0", new Vector3(i * 4.2f, 0f, facePositiveZ ? 0.35f : 0f), 0f));

            float wallYaw = facePositiveZ ? 0f : 180f;
            float wallZ = 1.6f * face;
            for (int i = 0; i < 6; i++)
                pieces.Add(("Toy_WallPanel", new Vector3(-4f + i * 1.6f, 0f, wallZ), wallYaw));

            // South ring is the big crawl (2.8 m mouth). A 1.2 m plastic cap does not
            // match that rim, so the crawl is left open. North ring is the small-tube
            // street: plastic caps insert ~0.36 m, the same collar as the fort tubes.
            if (facePositiveZ)
                pieces.Add(("Mega_CrawlTunnel", new Vector3(0f, 0f, -1.5f), 0f));
            else
            {
                pieces.Add(("Toy_TunnelTube", new Vector3(-2.5f, 0f, 1.5f), 0f));
                pieces.Add(("Toy_TunnelTube", new Vector3(0f, 0f, 1.5f), 0f));
                pieces.Add(("Toy_TunnelTube", new Vector3(2.5f, 0f, 1.5f), 0f));
                pieces.Add(("PGK_Tunnel_Plastic_LOD0", new Vector3(-4f, 0f, 1.5f), 0f));
                pieces.Add(("PGK_Tunnel_Plastic_LOD0", new Vector3(4f, 0f, 1.5f), 0f));
            }

            // West-end tower. Slide feeds the ring lane (south ring goes north, north ring goes south).
            AddDeckTower(pieces, -8f, 0f, facePositiveZ);
            return SpawnList(parent, pieces);
        }

        /// <summary>
        /// Continuous NS wall-run (panels yaw 90, 1.6 m centers) plus a deck tower past one end.
        /// The slide exits across the alley, toward the NS spine, and lands short of it.
        /// </summary>
        int LoopWallRun(Transform root, string name, Vector3 origin, float towerZ, bool slideExitsEast)
        {
            var parent = MakeGroup(root, name, origin, 0f);
            var pieces = new List<(string id, Vector3 p, float y)>();
            for (int i = 0; i < 6; i++)
                pieces.Add(("Toy_WallPanel", new Vector3(0f, 0f, -4f + i * 1.6f), 90f));

            float towerX = slideExitsEast ? -3f : 3f;
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(towerX - 1f, 0f, towerZ - 1f), 0f));
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(towerX + 1f, 0f, towerZ - 1f), 0f));
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(towerX - 1f, 0f, towerZ + 1f), 0f));
            pieces.Add(("PGK_Post_Square_3m_LOD0", new Vector3(towerX + 1f, 0f, towerZ + 1f), 0f));
            pieces.Add(("PGK_Deck_2x2_LOD0", new Vector3(towerX, Deck080, towerZ), 0f));
            pieces.Add(("PGK_Deck_2x2_LOD0", new Vector3(towerX, Deck160, towerZ), 0f));
            pieces.Add(("PGK_Deck_2x2_LOD0", new Vector3(towerX, Deck200, towerZ), 0f));

            // Closed rails: outer side + both Z ends. Spine-facing side stays open for the slide.
            float outerX = slideExitsEast ? towerX - 1f : towerX + 1f;
            pieces.Add(("PGK_Rail_2m_LOD0", new Vector3(outerX, Deck200, towerZ), 90f));
            pieces.Add(("PGK_Rail_2m_LOD0", new Vector3(towerX, Deck200, towerZ - 1f), 0f));
            pieces.Add(("PGK_Rail_2m_LOD0", new Vector3(towerX, Deck200, towerZ + 1f), 0f));

            // Ground flight on the outer side (0 → 0.80). Upper flight on the outer Z end (0.80 → 1.60).
            float groundX = slideExitsEast ? towerX - 2f : towerX + 2f;
            float groundYaw = slideExitsEast ? -90f : 90f;
            pieces.Add(("PGK_Stairs_5_LOD0", new Vector3(groundX, 0f, towerZ), groundYaw));
            float upperZ = towerZ > 0f ? towerZ + 2f : towerZ - 2f;
            float upperYaw = towerZ > 0f ? 0f : 180f;
            pieces.Add(("PGK_Stairs_5_LOD0", new Vector3(towerX, Deck080, upperZ), upperYaw));

            // Yaw 90 sends the exit to +X; yaw -90 sends it to -X. Mouth tucks under the open deck edge.
            float slideYaw = slideExitsEast ? 90f : -90f;
            float slideSign = slideExitsEast ? 1f : -1f;
            float slideX = towerX + slideSign * SlideLipOffset;
            pieces.Add(("PGK_Slide_Straight_M_LOD0", new Vector3(slideX, SlideGroundMouthY, towerZ), slideYaw));
            pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(towerX + slideSign * SlideLandingNear, 0.02f, towerZ), 0f));
            pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(towerX + slideSign * SlideLandingMid, 0.02f, towerZ), 0f));
            pieces.Add(("PGK_Safety_Tile_1m_LOD0", new Vector3(towerX + slideSign * SlideLandingFar, 0.02f, towerZ), 0f));

            float vaultX = slideExitsEast ? 2.5f : -2.5f;
            pieces.Add(("Toy_VaultRail_090", new Vector3(vaultX, 0f, -3f), 0f));
            pieces.Add(("Toy_VaultRail_100", new Vector3(vaultX, 0f, 0f), 0f));
            pieces.Add(("Toy_VaultRail_105", new Vector3(vaultX, 0f, 3f), 0f));
            return SpawnList(parent, pieces);
        }

        int SpawnPlay_SW(Transform root)
        {
            var parent = MakeGroup(root, "Play_Spawn_SW", new Vector3(3.5f, 0f, 4f), 40f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_SpringRider", new Vector3(1f, 0f, -0.5f), 20f),
                ("Toy_Seesaw", new Vector3(-1f, 0f, 1f), 90f),
                ("Toy_Bumper", new Vector3(0.5f, 0f, 1.25f), 0f),
            });
        }

        int SpawnPlay_SE(Transform root)
        {
            var parent = MakeGroup(root, "Play_Spawn_SE", new Vector3(62f, 0f, 3.5f), -40f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_SpringRider", new Vector3(-1f, 0f, -0.5f), -20f),
                ("Toy_Seesaw", new Vector3(1f, 0f, 1f), 90f),
                ("Toy_Bumper", new Vector3(-0.5f, 0f, 1.25f), 0f),
            });
        }

        int SpawnPlay_NW(Transform root)
        {
            var parent = MakeGroup(root, "Play_Spawn_NW", new Vector3(3f, 0f, 52f), 135f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Dome_Geo_3m_LOD0", new Vector3(-1f, 0f, -1f), 0f),
                ("Toy_NetFrame", new Vector3(1.5f, 0f, 0.5f), 0f),
                ("Toy_SpringRider", new Vector3(-1.5f, 0f, 1.5f), 40f),
            });
        }

        int SpawnPlay_NE(Transform root)
        {
            var parent = MakeGroup(root, "Play_Spawn_NE", new Vector3(69f, 0f, 45f), -135f);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_SpringRider", new Vector3(1.5f, 0f, -1f), -40f),
                ("Toy_Seesaw", new Vector3(-1.5f, 0f, 1f), 0f),
                ("Toy_Bumper", new Vector3(0f, 0f, -1.5f), 0f),
            });
        }

        /// <summary>
        /// Bars yaw 90 run along Z and abut at 4.2 m. Centers are chosen so each run
        /// stops short of the EW spines (z 16.4–19.6 and 34.4–37.6). West x=11 misses
        /// the pirate mast and the astro spiral; east x=61 is the open side of kickball.
        /// </summary>
        int MonkeyLane(Transform root, string name, float x, float[] centersZ)
        {
            var parent = MakeGroup(root, name, Vector3.zero, 0f);
            var pieces = new List<(string id, Vector3 p, float y)>();
            foreach (var z in centersZ)
                pieces.Add(("PGK_Monkey_4m_LOD0", new Vector3(x, 0f, z), 90f));
            return SpawnList(parent, pieces);
        }

        int MerryGoRound(Transform root, string name, Vector3 origin, float yaw)
        {
            // Stand-on spinner. East apron faces the west bar lane (x=11); bench stays west.
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("PGK_Spinner_StandOn_LOD0", new Vector3(0f, 0f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-2f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, -2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-2f, 0.02f, 2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-2f, 0.02f, -2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(2f, 0.02f, 1f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(2f, 0.02f, -1f), 0f),
                ("Toy_Bench", new Vector3(-3f, 0f, 0f), 90f),
                ("Toy_Seesaw", new Vector3(-2f, 0f, 3f), 0f),
            });
        }

        int SwingSet(Transform root, string name, Vector3 origin, float yaw)
        {
            // Flat bar bays along X, fall tiles to the south, bench on the east edge.
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_Bars", new Vector3(-2f, 0f, 0f), 0f),
                ("Toy_Bars", new Vector3(2f, 0f, 0f), 0f),
                ("Toy_Bars_Rail", new Vector3(0f, 0f, 0f), 0f),
                ("PGK_Monkey_4m_LOD0", new Vector3(0f, 0f, 2.5f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-2f, 0.02f, -2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, -2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(2f, 0.02f, -2f), 0f),
                ("Toy_Bench", new Vector3(4f, 0f, 0f), -90f),
            });
        }

        int KickballField(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            return SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_Goal", new Vector3(0f, 0f, -3.75f), 0f),
                ("Toy_Goal", new Vector3(0f, 0f, 3.75f), 180f),
                ("Toy_RubberTrack_C3", new Vector3(0f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, -2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(2f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 2f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(-2f, 0.02f, 0f), 0f),
                ("PGK_Safety_Tile_1m_LOD0", new Vector3(0f, 0.02f, 0f), 0f),
                // South fence sits behind the goal (z=-4.25 → world 19.75) and off SpineZs (ends 19.6).
                // West stays open onto the bar lane.
                ("Toy_Fence", new Vector3(-2.6f, 0f, 4.5f), 0f),
                ("Toy_Fence", new Vector3(0f, 0f, 4.5f), 0f),
                ("Toy_Fence", new Vector3(2.6f, 0f, 4.5f), 0f),
                ("Toy_Fence", new Vector3(-2.6f, 0f, -4.25f), 0f),
                ("Toy_Fence", new Vector3(0f, 0f, -4.25f), 0f),
                ("Toy_Fence", new Vector3(2.6f, 0f, -4.25f), 0f),
                ("Toy_Fence", new Vector3(4.5f, 0f, -2.6f), 90f),
                ("Toy_Fence", new Vector3(4.5f, 0f, 0f), 90f),
                ("Toy_Fence", new Vector3(4.5f, 0f, 2.6f), 90f),
            });
        }

        int HopscotchCourt(Transform root, string name, Vector3 origin, float yaw)
        {
            var parent = MakeGroup(root, name, origin, yaw);
            int n = Hopscotch(parent, new Vector3(0f, 0.02f, -3.75f), 0f);
            n += SpawnList(parent, new List<(string id, Vector3 p, float y)>
            {
                ("Toy_Bench", new Vector3(-2f, 0f, 0f), 90f),
            });
            return n;
        }

        int Hopscotch(Transform parent, Vector3 start, float yaw)
        {
            // 1 m tiles on a 1.25 m step (0.25 m gap) so the court reads at WorldScale 10.
            var tiles = new List<(string id, Vector3 p, float y)>();
            const float step = 1.25f;
            const float pair = 0.75f;
            float[] xs = { 0f, 0f, 0f, -pair, pair, 0f, -pair, pair, 0f };
            float[] zs = { 0f, step, 2f * step, 3f * step, 3f * step, 4f * step, 5f * step, 5f * step, 6f * step };
            for (int i = 0; i < xs.Length; i++)
            {
                var local = Quaternion.Euler(0f, yaw, 0f) * new Vector3(xs[i], 0f, zs[i]);
                tiles.Add(("PGK_Safety_Tile_1m_LOD0", start + local, yaw));
            }
            return SpawnList(parent, tiles);
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
        /// Measured Unity AABB (Blender FBX height axis = Z in source): Straight mouth ~+1.77,
        /// Spiral feet ~+0.51, Stairs tread0 ~+0.06, Tube90 feet ~-0.52, Mega_SlideTube feet ~-0.24,
        /// Mega_ParkourRamp feet ~-0.26, Toy_TunnelTube ~-0.02, Mega_CrawlTunnel ~0, Toy_Bars ~+0.23,
        /// Seesaw ~+0.10, Bumper ~+0.17, Goal ~-0.05; Bars_Rail / Spinner / Monkey / Bench /
        /// VaultRail / WallPanel / SpringRider / Tower / Picnic / Safety_Tile / RubberTrack(~0.02) /
        /// NetFrame / posts/decks ~0. Offset = -feetOrMouthY. Coarse - tune in Play if needed.
        /// </summary>
        static float StemSeatYOffset(string stem)
        {
            if (string.IsNullOrEmpty(stem)) return 0f;
            if (stem.StartsWith("PGK_Slide_Straight")) return -1.77f; // mouth -> authored Y
            if (stem.StartsWith("PGK_Slide_Spiral")) return -0.51f;   // feet -> authored Y
            if (stem.StartsWith("PGK_Slide_Tube")) return 0.52f;      // feet (minY~-0.52)
            if (stem.StartsWith("PGK_Stairs")) return -0.06f;         // first tread -> mulch
            if (stem.StartsWith("Mega_SlideTube")) return 0.24f;      // feet (minY~-0.24)
            if (stem.StartsWith("Mega_ParkourRamp")) return 0.26f;    // feet (minY~-0.26) Loop wall-run
            if (stem.StartsWith("Toy_TunnelTube")) return 0.02f;      // feet (minY~-0.02)
            if (stem.StartsWith("Toy_Bars_Rail")) return 0f;         // feet ~0
            if (stem.StartsWith("Toy_Bars")) return -0.23f;           // feet (minY~+0.23) Play_Swing
            if (stem.StartsWith("Toy_Seesaw")) return -0.10f;        // feet (minY~+0.10) SpawnLead
            if (stem.StartsWith("Toy_Bumper")) return -0.17f;        // feet (minY~+0.17) SpawnLead
            if (stem.StartsWith("Toy_Goal")) return 0.05f;           // feet (minY~-0.05) Kickball lift
            // Mega_Spinner / Monkey / Bench / WallPanel / VaultRail / SpringRider / Tower / Picnic ~0
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
