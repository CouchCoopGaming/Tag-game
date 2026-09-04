using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Builds PARK v1 playground (CUT v0.2 meters + toy dressing). Fantasy: crash-test dummies in a park.
    /// Origin = SW playable corner; +X east, +Z north, +Y up.
    /// Idempotent: clears children under a CUT root before rebuild.
    /// </summary>
    public class CutArenaBootstrap : MonoBehaviour
    {
        const string RootName = "PARK";

        // PARK v1 palette (AD bible)
        // Lawn #5B8C4A · Sand #D4B483 · Rubber #8B3A3A · Climb #3AA6C8 · Furniture #E6C14A · Hedge #3F6B3A · Tower #C4B8A5
        static readonly Color ColFloor = new Color(0x5B / 255f, 0x8C / 255f, 0x4A / 255f, 1f); // Lawn
        static readonly Color ColBowl = new Color(0xD4 / 255f, 0xB4 / 255f, 0x83 / 255f, 1f);  // Sand
        static readonly Color ColLoft = new Color(0xC4 / 255f, 0xB8 / 255f, 0xA5 / 255f, 1f);  // Tower
        static readonly Color ColWallRun = new Color(0x3A / 255f, 0xA6 / 255f, 0xC8 / 255f, 1f); // Climb
        static readonly Color ColSlide = new Color(0x8B / 255f, 0x3A / 255f, 0x3A / 255f, 1f);  // Rubber
        static readonly Color ColPadEdge = new Color(0x8B / 255f, 0x3A / 255f, 0x3A / 255f, 1f); // Rubber pads
        static readonly Color ColVault = new Color(0xE6 / 255f, 0xC1 / 255f, 0x4A / 255f, 1f);  // Furniture
        static readonly Color ColSpawn = new Color(0x8B / 255f, 0x3A / 255f, 0x3A / 255f, 1f);  // Rubber
        static readonly Color ColElbow = new Color(0x3F / 255f, 0x6B / 255f, 0x3A / 255f, 1f);  // Hedge
        static readonly Color ColRamp = new Color(0xC4 / 255f, 0xB8 / 255f, 0xA5 / 255f, 1f);  // Tower slides
        static readonly Color ColOob = new Color(0.18f, 0.19f, 0.21f, 1f);

        Transform _root;
        Material _matFloor, _matBowl, _matLoft, _matWall, _matSlide, _matPad, _matVault, _matSpawn, _matElbow, _matRamp, _matOob;

        void Awake()
        {
            Build();
        }

        [ContextMenu("Rebuild CUT Graybox")]
        public void Build()
        {
            EnsureMaterials();
            EnsureRoot();
            ClearRootChildren();

            BuildFloorG();
            BuildBowl();
            BuildLoft();
            BuildWalls();
            BuildVaults();
            BuildSlideMarkers();
            BuildGroundPads();
            BuildSpawns();
            BuildDensity();
            BuildOobSkirt();
        }

        void EnsureRoot()
        {
            var existing = transform.Find(RootName);
            if (existing != null)
            {
                _root = existing;
                return;
            }

            var go = new GameObject(RootName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            _root = go.transform;
        }

        void ClearRootChildren()
        {
            // DestroyImmediate so rebuild is idempotent in the same frame (play or edit).
            for (int i = _root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(_root.GetChild(i).gameObject);
        }

        void EnsureMaterials()
        {
            if (_matFloor != null) return;
            _matFloor = MakeMat(ColFloor);
            _matBowl = MakeMat(ColBowl);
            _matLoft = MakeMat(ColLoft);
            _matWall = MakeMat(ColWallRun);
            _matSlide = MakeMat(ColSlide);
            _matPad = MakeMat(ColPadEdge);
            _matVault = MakeMat(ColVault);
            _matSpawn = MakeMat(ColSpawn);
            _matElbow = MakeMat(ColElbow);
            _matRamp = MakeMat(ColRamp);
            _matOob = MakeMat(ColOob);
        }

        static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            var m = new Material(shader) { color = c, name = "CUT_" + ColorUtility.ToHtmlStringRGB(c) };
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            return m;
        }

        // --- Floors -----------------------------------------------------------------

        void BuildFloorG()
        {
            // Composite G around bowl hole X[13,23] Z[9,19]. Thickness 0.2, top at Y=0.
            const float t = 0.2f;
            // West slab X[0,13] Z[0,28]
            Box("Lawn_West", new Vector3(6.5f, -t * 0.5f, 14f), new Vector3(13f, t, 28f), _matFloor);
            // East slab X[23,36] Z[0,28]
            Box("Lawn_East", new Vector3(29.5f, -t * 0.5f, 14f), new Vector3(13f, t, 28f), _matFloor);
            // South strip X[13,23] Z[0,9]
            Box("Lawn_South", new Vector3(18f, -t * 0.5f, 4.5f), new Vector3(10f, t, 9f), _matFloor);
            // North strip X[13,23] Z[19,28]
            Box("Lawn_North", new Vector3(18f, -t * 0.5f, 23.5f), new Vector3(10f, t, 9f), _matFloor);
        }

        void BuildBowl()
        {
            const float t = 0.2f;
            // Bowl floor Y=-1, X[13,23] Z[9,19]
            Box("Toy_Sandbox", new Vector3(18f, -1f - t * 0.5f, 14f), new Vector3(10f, t, 10f), _matBowl);

            // Four corner ramps 20° — rise 1.0 over ~2.75 m (tan20≈0.364)
            // Place in bowl corners, sloping from G rim down into bowl.
            const float run = 2.75f;
            const float rise = 1.0f;
            float pitch = 20f;

            // SW corner of bowl → ramp faces NE into bowl (pivot along SE-NW? )
            // Ramp box: length=run along slope direction, height thin, rotate about local X or Z.
            // SW: from (13,0,9) into bowl toward (15.75, -1, 11.75)
            PlaceRamp("Toy_SandboxRamp_SW", new Vector3(13f + run * 0.5f, -0.5f, 9f + run * 0.5f), run, rise, pitch, 45f);
            PlaceRamp("Toy_SandboxRamp_SE", new Vector3(23f - run * 0.5f, -0.5f, 9f + run * 0.5f), run, rise, pitch, -45f);
            PlaceRamp("Toy_SandboxRamp_NW", new Vector3(13f + run * 0.5f, -0.5f, 19f - run * 0.5f), run, rise, pitch, 135f);
            PlaceRamp("Toy_SandboxRamp_NE", new Vector3(23f - run * 0.5f, -0.5f, 19f - run * 0.5f), run, rise, pitch, -135f);

            // Rim vaults 1.00 on all four sides at G (top at 1.00).
            // Leave ~2.75 m corner gaps so 20° ramps can exit onto G without clipping the rail.
            const float rimH = 1.00f;
            const float rimT = 0.4f;
            const float rimLen = 4.5f; // centered on each side of the 10 m bowl
            // South rim along Z=9
            Box("Toy_SandboxRim_S", new Vector3(18f, rimH * 0.5f, 9f), new Vector3(rimLen, rimH, rimT), _matVault);
            // North rim along Z=19
            Box("Toy_SandboxRim_N", new Vector3(18f, rimH * 0.5f, 19f), new Vector3(rimLen, rimH, rimT), _matVault);
            // West rim along X=13
            Box("Toy_SandboxRim_W", new Vector3(13f, rimH * 0.5f, 14f), new Vector3(rimT, rimH, rimLen), _matVault);
            // East rim along X=23
            Box("Toy_SandboxRim_E", new Vector3(23f, rimH * 0.5f, 14f), new Vector3(rimT, rimH, rimLen), _matVault);
        }

        void PlaceRamp(string name, Vector3 center, float run, float rise, float pitchDeg, float yawDeg)
        {
            // Thin slab rotated so surface is ~20°. Length along slope = hypot(run,rise).
            float len = Mathf.Sqrt(run * run + rise * rise);
            const float width = 2.2f;
            const float thick = 0.2f;
            var go = Box(name, center, new Vector3(width, thick, len), _matRamp);
            go.transform.localRotation = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        }

        void BuildLoft()
        {
            // 6×4 at +1.5, X[15,21] Z[24,28]. Platform top at 1.5.
            const float t = 0.2f;
            Box("Toy_Tower", new Vector3(18f, 1.5f - t * 0.5f, 26f), new Vector3(6f, t, 4f), _matLoft);

            // High-vault lip 1.50 on south edge (Z=24). Lip is the vault obstacle from G — height 1.50.
            // Brief: "Loft lip | 1.50 | high | south edge of loft"
            Box("Toy_TowerLip", new Vector3(18f, 1.50f * 0.5f, 24f), new Vector3(6f, 1.50f, 0.4f), _matVault);

            // Two drop-off 20° ramps from loft down toward G (south-ish sides / ends).
            const float run = 2.75f; // same 20° for 1.0 rise; loft is +1.5 so longer run
            float loftRun = 1.5f / Mathf.Tan(20f * Mathf.Deg2Rad); // ≈4.12
            float loftLen = Mathf.Sqrt(loftRun * loftRun + 1.5f * 1.5f);
            // West drop from loft west edge
            var rw = Box("Toy_Slide_Tower_W",
                new Vector3(15f - loftRun * 0.5f, 1.5f * 0.5f, 26f),
                new Vector3(2.0f, 0.2f, loftLen), _matRamp);
            rw.transform.localRotation = Quaternion.Euler(20f, 90f, 0f);
            // East drop
            var re = Box("Toy_Slide_Tower_E",
                new Vector3(21f + loftRun * 0.5f, 1.5f * 0.5f, 26f),
                new Vector3(2.0f, 0.2f, loftLen), _matRamp);
            re.transform.localRotation = Quaternion.Euler(20f, -90f, 0f);
        }

        // --- Walls ------------------------------------------------------------------

        void BuildWalls()
        {
            const float thick = 0.4f;

            // West Wall 10×3.2 @ X=8, Z[8,18], Y[0,3.2] — cyan wall-run
            Box("Toy_ClimbWall_West",
                new Vector3(8.0f, 3.2f * 0.5f, 13f),
                new Vector3(thick, 3.2f, 10f), _matWall);

            // SW Wall copy 8×3.2 @ X=8, Z[0,8]
            Box("Toy_ClimbWall_SW",
                new Vector3(8.0f, 3.2f * 0.5f, 4f),
                new Vector3(thick, 3.2f, 8f), _matWall);

            // East Alley W face 10×3.5 @ X=28, Z[8,18]
            Box("Toy_TwinTower_W",
                new Vector3(28.0f, 3.5f * 0.5f, 13f),
                new Vector3(thick, 3.5f, 10f), _matWall);

            // East Alley E face 10×3.5 @ X=31.2, Z[8,18] — 3.2 m gap
            Box("Toy_TwinTower_E",
                new Vector3(31.2f, 3.5f * 0.5f, 13f),
                new Vector3(thick, 3.5f, 10f), _matWall);

            // SE Alley copies 8×3.5, same X, Z[0,8]
            Box("Toy_TwinTower_SE_W",
                new Vector3(28.0f, 3.5f * 0.5f, 4f),
                new Vector3(thick, 3.5f, 8f), _matWall);
            Box("Toy_TwinTower_SE_E",
                new Vector3(31.2f, 3.5f * 0.5f, 4f),
                new Vector3(thick, 3.5f, 8f), _matWall);
        }

        // --- Vaults -----------------------------------------------------------------

        void BuildVaults()
        {
            // South rail A: 0.90, X[10,12], Z≈4
            Box("Toy_Bench_South_A",
                new Vector3(11f, 0.90f * 0.5f, 4f),
                new Vector3(2f, 0.90f, 0.5f), _matVault);

            // South rail B: 1.05, X[14,16], Z≈4.5 (stagger)
            Box("Toy_Bench_South_B",
                new Vector3(15f, 1.05f * 0.5f, 4.5f),
                new Vector3(2f, 1.05f, 0.5f), _matVault);

            // Chain 1 landing: 1.00, X[10.4,12.4], Z[15,16] — 2.8 m east of West Wall on G
            Box("Toy_Bench_Chain1",
                new Vector3(11.4f, 1.00f * 0.5f, 15.5f),
                new Vector3(2f, 1.00f, 1f), _matVault);

            // Bowl rims + loft lip already built in BuildBowl / BuildLoft
        }

        // --- Slide markers (open floor, magenta) ------------------------------------

        void BuildSlideMarkers()
        {
            const float h = 0.05f;
            // Chain 1 slide: X[2,7] Z[10,12] — 6 m (5×2)
            Box("Toy_Slide_C1",
                new Vector3(4.5f, h * 0.5f, 11f),
                new Vector3(5f, h, 2f), _matSlide);

            // South Lane slide: X[16,22] Z[3,5] — 6 m
            Box("Toy_RubberTrack_C3",
                new Vector3(19f, h * 0.5f, 4f),
                new Vector3(6f, h, 2f), _matSlide);
        }

        // --- 3×3 G pads (magenta edge) ----------------------------------------------

        void BuildGroundPads()
        {
            // Centers from brief
            Vector2[] pads =
            {
                new Vector2(29.6f, 6.5f),  // East Alley S mouth
                new Vector2(29.6f, 19.5f), // East Alley N mouth
                new Vector2(29.6f, 1.5f),  // SE Alley S mouth
                new Vector2(8.0f, 6.5f),   // West Wall S
                new Vector2(8.0f, 19.5f),  // West Wall N
                new Vector2(8.0f, 1.5f),   // SW Wall S
            };
            string[] names =
            {
                "Pad_EastAlley_S", "Pad_EastAlley_N", "Pad_SEAlley_S",
                "Pad_WestWall_S", "Pad_WestWall_N", "Pad_SWWall_S"
            };

            for (int i = 0; i < pads.Length; i++)
                BuildPad(names[i], pads[i].x, pads[i].y);
        }

        void BuildPad(string name, float cx, float cz)
        {
            // Magenta-edge frame on G: thin border strips around 3×3, plus faint fill
            const float size = 3f;
            const float edge = 0.12f;
            const float h = 0.04f;
            float half = size * 0.5f;

            var parent = new GameObject(name);
            parent.transform.SetParent(_root, false);
            parent.transform.localPosition = new Vector3(cx, 0f, cz);

            // Fill (slightly darker magenta translucent look via same mat)
            ChildBox(parent.transform, "Fill",
                new Vector3(0f, h * 0.5f, 0f), new Vector3(size - edge * 2f, h * 0.5f, size - edge * 2f), _matPad);

            // Edges
            ChildBox(parent.transform, "Edge_N",
                new Vector3(0f, h * 0.5f, half - edge * 0.5f), new Vector3(size, h, edge), _matPad);
            ChildBox(parent.transform, "Edge_S",
                new Vector3(0f, h * 0.5f, -half + edge * 0.5f), new Vector3(size, h, edge), _matPad);
            ChildBox(parent.transform, "Edge_E",
                new Vector3(half - edge * 0.5f, h * 0.5f, 0f), new Vector3(edge, h, size - edge * 2f), _matPad);
            ChildBox(parent.transform, "Edge_W",
                new Vector3(-half + edge * 0.5f, h * 0.5f, 0f), new Vector3(edge, h, size - edge * 2f), _matPad);
        }

        // --- Spawns + elbows --------------------------------------------------------

        void BuildSpawns()
        {
            SpawnPad("Spawn_SW", 3f, 3f, 90f);  // CCW: east along south lawn
            SpawnPad("Spawn_SE", 33f, 3f, 0f);   // CCW: north along east lawn
            SpawnPad("Spawn_NW", 3f, 25f, 180f); // CCW: south along west lawn
            SpawnPad("Spawn_NE", 33f, 25f, -90f); // CCW: west along north lawn

            // 1.2 m elbow L-stubs ~2 m — break spawn LOS (yellow)
            // Place inward from each corner toward arena center
            Elbow("Toy_Hedge_SW", 5f, 3f, towardEast: true, towardNorth: true);
            Elbow("Toy_Hedge_SE", 31f, 3f, towardEast: false, towardNorth: true);
            Elbow("Toy_Hedge_NW", 5f, 25f, towardEast: true, towardNorth: false);
            Elbow("Toy_Hedge_NE", 31f, 25f, towardEast: false, towardNorth: false);
        }

        void SpawnPad(string name, float x, float z, float faceYawDeg)
        {
            // Rubber pad + facing wedge — Trail Tag anti-spaghetti: face perimeter CCW, not sandbox
            Box(name, new Vector3(x, 0.03f, z), new Vector3(1.5f, 0.06f, 1.5f), _matSpawn);
            var face = Box(name + "_Face", new Vector3(x, 0.08f, z), new Vector3(0.25f, 0.08f, 0.9f), _matVault);
            face.transform.localRotation = Quaternion.Euler(0f, faceYawDeg, 0f);
            // Offset wedge forward along facing
            face.transform.localPosition = new Vector3(x, 0.08f, z) + Quaternion.Euler(0f, faceYawDeg, 0f) * Vector3.forward * 0.7f;
        }

        void Elbow(string name, float x, float z, bool towardEast, bool towardNorth)
        {
            // L-stub: one arm along X (~2m), one along Z (~2m), height 1.2 (high-vault height)
            const float h = 1.2f;
            const float arm = 2.0f;
            const float thick = 0.35f;
            float sx = towardEast ? 1f : -1f;
            float sz = towardNorth ? 1f : -1f;

            var parent = new GameObject(name);
            parent.transform.SetParent(_root, false);
            parent.transform.localPosition = new Vector3(x, 0f, z);

            ChildBox(parent.transform, "Arm_X",
                new Vector3(sx * arm * 0.5f, h * 0.5f, 0f),
                new Vector3(arm, h, thick), _matElbow);
            ChildBox(parent.transform, "Arm_Z",
                new Vector3(0f, h * 0.5f, sz * arm * 0.5f),
                new Vector3(thick, h, arm), _matElbow);
        }

        void BuildOobSkirt()
        {
            // OOB skirt volume hint: floor ring X[-2,38] Z[-2,30] outside playable
            const float t = 0.15f;
            float y = -t * 0.5f - 0.02f;
            // South strip Z[-2,0] X[-2,38]
            Box("OOB_S", new Vector3(18f, y, -1f), new Vector3(40f, t, 2f), _matOob);
            // North strip Z[28,30]
            Box("OOB_N", new Vector3(18f, y, 29f), new Vector3(40f, t, 2f), _matOob);
            // West strip X[-2,0] Z[0,28]
            Box("OOB_W", new Vector3(-1f, y, 14f), new Vector3(2f, t, 28f), _matOob);
            // East strip X[36,38]
            Box("OOB_E", new Vector3(37f, y, 14f), new Vector3(2f, t, 28f), _matOob);
        }


        // --- v0.2 chase density (do not move v0.1 chain geo) -------------------------

        void BuildDensity()
        {
            // Producer snippet-aligned (CUT-v0.2-BuildDensity.cs.txt)
            BuildWestBackAlley();
            BuildMidCutHurdles();
            BuildBowlElbows();
            BuildJukeIslands();
            BuildBowlNubs();
            BuildSouthOffAxis();
            BuildMonkeyBars();
        }

        void BuildWestBackAlley()
        {
            // Three 0.90 stubs across X[0.4,2.6], Z=8,13,18 — length 2.2 in X, 0.5 thick
            float[] zs = { 8f, 13f, 18f };
            for (int i = 0; i < zs.Length; i++)
            {
                Box($"Toy_BackAlleyBar_{i}",
                    new Vector3(1.5f, 0.90f * 0.5f, zs[i]),
                    new Vector3(2.2f, 0.90f, 0.5f), _matVault);
            }
        }

        void BuildMidCutHurdles()
        {
            // Vault rails only — NOT wall-run (yellow vault mat)
            // Vault_MidCut_S — X=24.8, Z[10.5,12.5], 1.00 × 0.4 × 2.0
            Box("Toy_Picnic_MidCut_S",
                new Vector3(24.8f, 1.00f * 0.5f, 11.5f),
                new Vector3(0.4f, 1.00f, 2.0f), _matVault);
            // Vault_MidCut_N — X=26.2, Z[15.5,17.5]
            Box("Toy_Picnic_MidCut_N",
                new Vector3(26.2f, 1.00f * 0.5f, 16.5f),
                new Vector3(0.4f, 1.00f, 2.0f), _matVault);
        }

        void BuildBowlElbows()
        {
            // 1.2 m L-stubs at (18,7) and (18,21)
            Elbow("Toy_Hedge_BowlSouth", 18f, 7f, towardEast: true, towardNorth: true);
            Elbow("Toy_Hedge_BowlNorth", 18f, 21f, towardEast: true, towardNorth: false);
        }

        void BuildJukeIslands()
        {
            // Low 1.00 tables, 2×1 footprint, 0.4 thick
            Box("Toy_Picnic_Island_NW",
                new Vector3(10f, 1.00f * 0.5f, 22f),
                new Vector3(2f, 1.00f, 1f), _matVault);
            Box("Toy_Picnic_Island_NE",
                new Vector3(26f, 1.00f * 0.5f, 22f),
                new Vector3(2f, 1.00f, 1f), _matVault);
        }

        void BuildBowlNubs()
        {
            // 0.90 cubes 1×1 inside Bowl on Y=-1 floor
            Box("Toy_SandToy_SW",
                new Vector3(15.2f, -1.0f + 0.90f * 0.5f, 11.2f),
                new Vector3(1f, 0.90f, 1f), _matVault);
            Box("Toy_SandToy_NE",
                new Vector3(20.8f, -1.0f + 0.90f * 0.5f, 16.8f),
                new Vector3(1f, 0.90f, 1f), _matVault);
        }

        void BuildSouthOffAxis()
        {
            // 0.90 at X[18,20] Z=2.6 — south of C3 landing cone
            Box("Toy_Bench_SouthOff",
                new Vector3(19f, 0.90f * 0.5f, 2.6f),
                new Vector3(2f, 0.90f, 0.5f), _matVault);
        }


        void BuildMonkeyBars()
        {
            // PARK v1 — three 0.90 rails along X[10,16] Z=7, 1.2 m spacing. Vault only (no hang/climb).
            // Centers at X = 10.6, 11.8, 13.0 spanning toward 16 — 1.2m spacing, footprints 0.2×1.6
            float[] xs = { 10.6f, 11.8f, 13.0f };
            for (int i = 0; i < xs.Length; i++)
            {
                Box($"Toy_Bars_{i}",
                    new Vector3(xs[i], 0.90f * 0.5f, 7f),
                    new Vector3(0.2f, 0.90f, 1.6f), _matVault);
            }
        }

        // --- Helpers ----------------------------------------------------------------

        GameObject Box(string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            ApplyMat(go, mat);
            return go;
        }

        void ChildBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            ApplyMat(go, mat);
        }

        static void ApplyMat(GameObject go, Material mat)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
        }
    }
}
