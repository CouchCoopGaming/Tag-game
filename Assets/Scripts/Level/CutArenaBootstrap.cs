using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// PARK campus graybox — figure-8 chase campus with readable cardinal ski spines.
    /// Origin = SW corner; +X east, +Z north. WorldScale on root.
    ///
    /// Path design:
    ///   Outer ring: Pirate → Tron → Army → Knight → Ninja → Astro → Pirate
    ///   Figure-8 cross: west NS spine + east NS spine through Crash (the X)
    ///   Height: pad approach vault → mid deck → high perch → slide exit to spine
    ///   Rule: open lawn between pads; spines are highways; 3–5 toys per pad; no clutter on lanes.
    /// </summary>
    public class CutArenaBootstrap : MonoBehaviour
    {
        const string RootName = "PARK";
        /// <summary>Soft-play fantasy scale (~10x).</summary>
        public const float WorldScale = 10f;

        // Playable campus in graybox meters (world = * WorldScale)
        public const float MapW = 72f;
        public const float MapD = 54f;

        // Named pad centers (keep in sync with ZoneNameMarkers / PgkLandmarkPlacer)
        public const float CxCrash = 36f, CzCrash = 27f;
        public const float CxPirate = 14f, CzPirate = 12f;
        public const float CxArmy = 58f, CzArmy = 12f;
        public const float CxAstro = 14f, CzAstro = 42f;
        public const float CxKnight = 58f, CzKnight = 42f;
        public const float CxTron = 36f, CzTron = 8f;
        public const float CxNinja = 36f, CzNinja = 46f;

        // Cardinal ski highway axes (figure-8 frame)
        const float SpineZs = 18f; // south EW
        const float SpineZn = 36f; // north EW
        const float SpineXw = 24f; // west NS
        const float SpineXe = 48f; // east NS

        static readonly Color ColFloor = new Color(0x5C / 255f, 0x3A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColBowl = new Color(0x5C / 255f, 0x3A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColLoft = new Color(0xC5 / 255f, 0xCB / 255f, 0xD1 / 255f, 1f);
        static readonly Color ColWallRun = new Color(0x3D / 255f, 0x7E / 255f, 1f, 1f);
        static readonly Color ColSlide = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColPadEdge = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColVault = new Color(0xF5 / 255f, 0xD5 / 255f, 0x47 / 255f, 1f);
        static readonly Color ColRamp = new Color(0xB8 / 255f, 0xC0 / 255f, 0xC8 / 255f, 1f);
        static readonly Color ColOob = new Color(0x3F / 255f, 0x7A / 255f, 0x4A / 255f, 1f);
        static readonly Color ColPath = new Color(0x6E / 255f, 0x4A / 255f, 0x38 / 255f, 1f);

        // Match ParkPropDresser Toy_SpawnPad_* palette (SW Teal, SE Coral, NW Violet, NE Lime)
        static readonly Color ColSpawnTeal = new Color(0x2E / 255f, 0xC4 / 255f, 0xB6 / 255f, 1f);
        static readonly Color ColSpawnCoral = new Color(0xFF / 255f, 0x6B / 255f, 0x4A / 255f, 1f);
        static readonly Color ColSpawnViolet = new Color(0x9B / 255f, 0x5C / 255f, 0xE6 / 255f, 1f);
        static readonly Color ColSpawnLime = new Color(0xA8 / 255f, 0xE6 / 255f, 0x1A / 255f, 1f);

        Transform _root;
        Material _matFloor, _matBowl, _matLoft, _matWall, _matSlide, _matPad, _matVault, _matRamp, _matOob, _matPath;

        void Awake()
        {
            Build();
            if (GetComponent<Tag.Art.ParkPropDresser>() == null)
                gameObject.AddComponent<Tag.Art.ParkPropDresser>();
            if (GetComponent<Tag.Art.PgkLandmarkPlacer>() == null)
                gameObject.AddComponent<Tag.Art.PgkLandmarkPlacer>();
            // EXPERIMENTAL fans — component present but spawnFans defaults false (not core loop).
            if (GetComponent<Tag.Experimental.ExperimentalFanPlacer>() == null)
                gameObject.AddComponent<Tag.Experimental.ExperimentalFanPlacer>();
            if (GetComponent<ZoneNameMarkers>() == null)
                gameObject.AddComponent<ZoneNameMarkers>();
        }

        [ContextMenu("Rebuild CUT Graybox")]
        public void Build()
        {
            EnsureMaterials();
            EnsureRoot();
            ClearRootChildren();

            BuildGround();
            BuildChaseLanes();    // figure-8 path tint (prop-light)
            BuildSkiSpines();     // cardinal ski highways + pad connectors
            BuildFlowSteps();     // sparse mid-height run→jump→slide stones
            BuildCrashCore();     // center X
            BuildPiratePad();     // SW
            BuildArmyPad();       // SE
            BuildAstroPad();      // NW
            BuildKnightPad();     // NE
            BuildTronPad();       // S mid
            BuildNinjaPad();      // N mid
            BuildSpawns();
            BuildOobSkirt();

            _root.localScale = Vector3.one * WorldScale;
        }

        void EnsureRoot()
        {
            var existing = transform.Find(RootName);
            if (existing != null) { _root = existing; return; }
            var go = new GameObject(RootName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            _root = go.transform;
        }

        void ClearRootChildren()
        {
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
            _matRamp = MakeMat(ColRamp);
            _matOob = MakeMat(ColOob);
            _matPath = MakeMat(ColPath);
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

        static Material MakeEmissiveMat(Color c, float emissionMul = 2.2f)
        {
            var m = MakeMat(c);
            m.name = "CUT_Emissive_" + ColorUtility.ToHtmlStringRGB(c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emissionMul);
            }
            return m;
        }

        // --- Ground / chase space ---------------------------------------------------

        void BuildGround()
        {
            const float t = 0.25f;
            Box("Lawn_Campus", new Vector3(MapW * 0.5f, -t * 0.5f, MapD * 0.5f),
                new Vector3(MapW, t, MapD), _matFloor);
        }

        /// <summary>
        /// Soft path tint for the figure-8: west + east loops cross at Crash.
        /// Tint only — never stacks props; keeps chase space readable.
        /// </summary>
        void BuildChaseLanes()
        {
            const float t = 0.06f;
            float y = t * 0.5f + 0.01f;
            const float laneW = 7f;

            // West loop (Pirate / Astro / Crash) — NS + south/north arcs
            Box("Lane_West_NS", new Vector3(SpineXw, y, CzCrash), new Vector3(laneW, t, 30f), _matPath);
            Box("Lane_West_S", new Vector3(19f, y, SpineZs), new Vector3(14f, t, laneW), _matPath);
            Box("Lane_West_N", new Vector3(19f, y, SpineZn), new Vector3(14f, t, laneW), _matPath);

            // East loop (Army / Knight / Crash)
            Box("Lane_East_NS", new Vector3(SpineXe, y, CzCrash), new Vector3(laneW, t, 30f), _matPath);
            Box("Lane_East_S", new Vector3(53f, y, SpineZs), new Vector3(14f, t, laneW), _matPath);
            Box("Lane_East_N", new Vector3(53f, y, SpineZn), new Vector3(14f, t, laneW), _matPath);

            // Outer ring connectors through Tron / Ninja mid pads
            Box("Lane_Outer_S", new Vector3(CxTron, y, CzTron + 2f), new Vector3(28f, t, 5.5f), _matPath);
            Box("Lane_Outer_N", new Vector3(CxNinja, y, CzNinja - 2f), new Vector3(28f, t, 5.5f), _matPath);

            // Cross at Crash (the X of the 8)
            Box("Lane_Cross_EW", new Vector3(CxCrash, y, CzCrash), new Vector3(28f, t, 6f), _matPath);
        }

        void BuildSkiSpines()
        {
            // Four cardinal highways only — no diagonal spaghetti.
            const float spineW = 3.2f;
            const float spineT = 0.12f;
            float y = spineT * 0.5f;

            Box("Spine_EW_S", new Vector3(CxCrash, y, SpineZs), new Vector3(52f, spineT, spineW), _matSlide);
            Box("Spine_EW_N", new Vector3(CxCrash, y, SpineZn), new Vector3(52f, spineT, spineW), _matSlide);
            Box("Spine_NS_W", new Vector3(SpineXw, y, CzCrash), new Vector3(spineW, spineT, 36f), _matSlide);
            Box("Spine_NS_E", new Vector3(SpineXe, y, CzCrash), new Vector3(spineW, spineT, 36f), _matSlide);

            // Junction plates at figure-8 corners + Crash cross
            float jy = y + 0.02f;
            const float j = 4f;
            Box("Spine_Jct_SW", new Vector3(SpineXw, jy, SpineZs), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_SE", new Vector3(SpineXe, jy, SpineZs), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_NW", new Vector3(SpineXw, jy, SpineZn), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_NE", new Vector3(SpineXe, jy, SpineZn), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_Core", new Vector3(CxCrash, jy + 0.01f, CzCrash), new Vector3(5.5f, spineT, 5.5f), _matPath);

            // One approach ramp per pad → nearest spine (fall-line into chase)
            SkiRamp("Conn_Tron_N", new Vector3(CxTron, 0.5f, 12.5f), new Vector3(3.2f, 0.26f, 5.5f), -11f, 0f);
            SkiRamp("Conn_Ninja_S", new Vector3(CxNinja, 0.5f, 41.5f), new Vector3(3.2f, 0.26f, 5.5f), 11f, 0f);
            SkiRamp("Conn_Pirate_N", new Vector3(CxPirate, 0.5f, 15.0f), new Vector3(3.0f, 0.26f, 5.0f), -11f, 0f);
            SkiRamp("Conn_Army_N", new Vector3(CxArmy, 0.5f, 15.0f), new Vector3(3.0f, 0.26f, 5.0f), -11f, 0f);
            SkiRamp("Conn_Astro_S", new Vector3(CxAstro, 0.5f, 39.0f), new Vector3(3.0f, 0.26f, 5.0f), 11f, 0f);
            SkiRamp("Conn_Knight_S", new Vector3(CxKnight, 0.5f, 39.0f), new Vector3(3.0f, 0.26f, 5.0f), 11f, 0f);
            SkiRamp("Conn_Crash_W", new Vector3(30.0f, 0.42f, CzCrash), new Vector3(5.0f, 0.26f, 3.0f), -9f, 90f);
            SkiRamp("Conn_Crash_E", new Vector3(42.0f, 0.42f, CzCrash), new Vector3(5.0f, 0.26f, 3.0f), -9f, -90f);
        }

        void SkiRamp(string name, Vector3 localPos, Vector3 scale, float pitchDeg, float yawDeg)
        {
            var go = Box(name, localPos, scale, _matRamp);
            go.transform.localRotation = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        }

        /// <summary>
        /// Sparse hop stones beside the figure-8 — run → jump → slide onto spines.
        /// jumpSpeed 24.7 / gravity 22 → apex ≈ 13.9 world ≈ 1.39 graybox (WorldScale 10).
        /// Tops stay ≤ ~1.20 from lawn; stones sit off spine axes so ski highways stay clear.
        /// Mid stones keep horizontal chain gaps ~5–6 graybox (skiable / stretch at sprint).
        /// </summary>
        void BuildFlowSteps()
        {
            // West NS — west of SpineXw (Pirate ↔ Astro)
            FlowStone("Flow_W_S", new Vector3(SpineXw - 2.6f, 0.52f, 21.5f), new Vector3(2.0f, 0.20f, 2.0f));
            FlowStone("Flow_W_Mid", new Vector3(SpineXw - 2.6f, 0.68f, 27.0f), new Vector3(2.0f, 0.20f, 2.0f));
            FlowStone("Flow_W_N", new Vector3(SpineXw - 2.6f, 0.52f, 32.5f), new Vector3(2.0f, 0.20f, 2.0f));
            // East NS — east of SpineXe (Army ↔ Knight)
            FlowStone("Flow_E_S", new Vector3(SpineXe + 2.6f, 0.52f, 21.5f), new Vector3(2.0f, 0.20f, 2.0f));
            FlowStone("Flow_E_Mid", new Vector3(SpineXe + 2.6f, 0.68f, 27.0f), new Vector3(2.0f, 0.20f, 2.0f));
            FlowStone("Flow_E_N", new Vector3(SpineXe + 2.6f, 0.52f, 32.5f), new Vector3(2.0f, 0.20f, 2.0f));
            // Outer south ring (Tron) — south of lane / Spine_EW_S
            FlowStone("Flow_S_W", new Vector3(26f, 0.48f, CzTron - 0.5f), new Vector3(2.0f, 0.18f, 2.0f));
            FlowStone("Flow_S_Mid", new Vector3(CxTron, 0.58f, CzTron - 0.5f), new Vector3(2.0f, 0.18f, 2.0f));
            FlowStone("Flow_S_E", new Vector3(46f, 0.48f, CzTron - 0.5f), new Vector3(2.0f, 0.18f, 2.0f));
            // Outer north ring (Ninja) — north of lane / Spine_EW_N
            FlowStone("Flow_N_W", new Vector3(26f, 0.48f, CzNinja + 0.5f), new Vector3(2.0f, 0.18f, 2.0f));
            FlowStone("Flow_N_Mid", new Vector3(CxNinja, 0.58f, CzNinja + 0.5f), new Vector3(2.0f, 0.18f, 2.0f));
            FlowStone("Flow_N_E", new Vector3(46f, 0.48f, CzNinja + 0.5f), new Vector3(2.0f, 0.18f, 2.0f));
            // Crash loft approaches — south of vault / north of towers (no overlap with Toy_VaultRail)
            FlowStone("Flow_Core_S", new Vector3(CxCrash, 0.95f, CzCrash - 7.5f), new Vector3(2.8f, 0.24f, 1.8f));
            FlowStone("Flow_Core_N", new Vector3(CxCrash, 0.95f, CzCrash + 7.0f), new Vector3(2.8f, 0.24f, 1.8f));
        }

        void FlowStone(string name, Vector3 localPos, Vector3 scale)
        {
            Box(name, localPos, scale, _matLoft);
        }

        // --- Zone pads (3–5 signature toys; open sightlines to campus) --------------

        Transform Zone(string name, float cx, float cz)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(cx, 0f, cz);
            return go.transform;
        }

        void PadFloor(Transform z, float w, float d, Material mat)
        {
            ChildBox(z, "PadFloor", new Vector3(0f, -0.08f, 0f), new Vector3(w, 0.16f, d), mat);
        }

        /// <summary>Crash = figure-8 X. Bowl + twin towers + open EW cross; no wall blocking spines.</summary>
        void BuildCrashCore()
        {
            var z = Zone("Zone_Crash", CxCrash, CzCrash);
            PadFloor(z, 16f, 14f, _matBowl);

            // Sunken bowl — open corners for chase through
            ChildBox(z, "Toy_Sandbox", new Vector3(0f, -0.9f, 0f), new Vector3(8f, 0.2f, 8f), _matBowl);
            ChildBox(z, "Toy_SandboxRim_S", new Vector3(0f, 0.35f, -4.2f), new Vector3(6f, 0.7f, 0.3f), _matVault);
            ChildBox(z, "Toy_SandboxRim_N", new Vector3(0f, 0.35f, 4.2f), new Vector3(6f, 0.7f, 0.3f), _matVault);
            // No full E/W rims — keep EW chase sightline through Crash

            // Twin towers north of bowl + loft bridge (run→jump→slide off loft south)
            ChildBox(z, "Toy_TwinTower_W", new Vector3(-4.5f, 2.0f, 3.5f), new Vector3(2.0f, 4f, 2.0f), _matWall);
            ChildBox(z, "Toy_TwinTower_E", new Vector3(4.5f, 2.0f, 3.5f), new Vector3(2.0f, 4f, 2.0f), _matWall);
            ChildBox(z, "Toy_Tower", new Vector3(0f, 3.1f, 3.5f), new Vector3(7f, 0.3f, 3.2f), _matLoft);

            // Climb face west but shortened — keep EW chase sightline through Crash
            ChildBox(z, "Toy_ClimbWall_West", new Vector3(-7.0f, 1.4f, 1.2f), new Vector3(0.35f, 2.8f, 4.2f), _matWall);
            ChildBox(z, "Toy_VaultRail", new Vector3(0f, 0.65f, -5.5f), new Vector3(3.6f, 1.1f, 0.3f), _matVault);
        }

        /// <summary>Pirate SW — low deck → high deck → plank east toward spine → slide south exit.</summary>
        void BuildPiratePad()
        {
            var z = Zone("Zone_Pirate", CxPirate, CzPirate);
            PadFloor(z, 14f, 12f, _matFloor);
            // Mast/decks nudged west — open pad center sightline toward campus
            ChildBox(z, "MastBase", new Vector3(-2.4f, 0.45f, 0.8f), new Vector3(2.4f, 0.9f, 2.4f), _matVault);
            ChildBox(z, "Deck_Low", new Vector3(-1.2f, 1.1f, 0.8f), new Vector3(6.2f, 0.28f, 4.0f), _matLoft);
            ChildBox(z, "Deck_High", new Vector3(-2.8f, 2.2f, 2.2f), new Vector3(3.2f, 0.28f, 2.6f), _matLoft);
            ChildBox(z, "Plank_Run", new Vector3(3.5f, 1.35f, 0.5f), new Vector3(5f, 0.22f, 1.1f), _matVault);
            ChildBox(z, "ClimbNetWall", new Vector3(-5.5f, 1.5f, 0.5f), new Vector3(0.3f, 3f, 5f), _matWall);
            ChildBox(z, "Slide_Ramp", new Vector3(4f, 0.95f, -3f), new Vector3(2.6f, 0.28f, 5f), _matSlide)
                .transform.localRotation = Quaternion.Euler(13f, 15f, 0f);
        }

        /// <summary>Army SE — staggered bunkers + trench lane + ramp north to spine.</summary>
        void BuildArmyPad()
        {
            var z = Zone("Zone_Army", CxArmy, CzArmy);
            PadFloor(z, 14f, 12f, _matFloor);
            // Staggered so mid trench stays a clear chase lane
            ChildBox(z, "Bunker_A", new Vector3(-3.5f, 0.65f, -2.5f), new Vector3(3.5f, 1.3f, 2.6f), _matPad);
            ChildBox(z, "Bunker_B", new Vector3(3.5f, 0.65f, 2.5f), new Vector3(3.5f, 1.3f, 2.6f), _matPad);
            ChildBox(z, "FoxholeTrench", new Vector3(0f, -0.35f, 0f), new Vector3(9f, 0.7f, 1.8f), _matBowl);
            ChildBox(z, "Ramp_Up", new Vector3(-4.5f, 0.75f, 3.5f), new Vector3(3.2f, 0.28f, 5.5f), _matRamp)
                .transform.localRotation = Quaternion.Euler(-13f, 90f, 0f);
            ChildBox(z, "Wall_Cover", new Vector3(5.5f, 1.1f, 0f), new Vector3(0.35f, 2.2f, 7f), _matWall);
            // Offset vault — leave mid trench → spine approach open
            ChildBox(z, "Vault_Low", new Vector3(-2.8f, 0.45f, 4.5f), new Vector3(3.4f, 0.9f, 0.35f), _matVault);
        }

        /// <summary>Astro NW — open loft ring (cut south for sightline) + half-pipes + climb stub.</summary>
        void BuildAstroPad()
        {
            var z = Zone("Zone_Astro", CxAstro, CzAstro);
            PadFloor(z, 14f, 12f, _matFloor);
            // U loft open south; ladder off mid so Conn_Astro approach stays clear
            ChildBox(z, "Loft_Ring", new Vector3(0f, 2.0f, 2.2f), new Vector3(8f, 0.28f, 5f), _matLoft);
            ChildBox(z, "VisorPipe_A", new Vector3(-3.8f, 0.95f, -1.5f), new Vector3(1.6f, 1.9f, 4.5f), _matWall)
                .transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            ChildBox(z, "HalfPipe_L", new Vector3(-5.2f, 0.75f, 0.8f), new Vector3(0.45f, 2.2f, 6.5f), _matSlide);
            ChildBox(z, "HalfPipe_R", new Vector3(5.2f, 0.75f, 0.8f), new Vector3(0.45f, 2.2f, 6.5f), _matSlide);
            ChildBox(z, "Ladder_Stub", new Vector3(-3.6f, 1.0f, -4.2f), new Vector3(1.1f, 2f, 0.35f), _matVault);
        }

        /// <summary>Knight NE — open courtyard, one keep, shield on north only, ramp down to spine.</summary>
        void BuildKnightPad()
        {
            var z = Zone("Zone_Knight", CxKnight, CzKnight);
            PadFloor(z, 14f, 12f, _matFloor);
            ChildBox(z, "Courtyard", new Vector3(0f, 0.05f, 0f), new Vector3(9f, 0.1f, 7f), _matLoft);
            ChildBox(z, "ShieldWall_N", new Vector3(0f, 1.4f, 4.5f), new Vector3(9f, 2.8f, 0.4f), _matWall);
            // Drop west shield — open sightline into campus / NS_E
            ChildBox(z, "Keep_Tower", new Vector3(3.5f, 2.4f, 2.5f), new Vector3(2.8f, 4.8f, 2.8f), _matPad);
            ChildBox(z, "Battlement", new Vector3(3.5f, 5.0f, 2.5f), new Vector3(3.6f, 0.35f, 3.6f), _matLoft);
            // Gate off mid — open courtyard sightline south to spine
            ChildBox(z, "VaultGate", new Vector3(2.2f, 0.85f, -3.5f), new Vector3(2.2f, 1.7f, 0.45f), _matVault);
            ChildBox(z, "Ramp_Keep", new Vector3(3.5f, 1.1f, -1.5f), new Vector3(2.6f, 0.28f, 5f), _matRamp)
                .transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
        }

        /// <summary>Tron S — open courtyard, two posts + neon, wall-run south edge only.</summary>
        void BuildTronPad()
        {
            var z = Zone("Zone_Tron", CxTron, CzTron);
            PadFloor(z, 12f, 9f, _matPad);
            ChildBox(z, "GridPost_0", new Vector3(-3.2f, 1.1f, 0.5f), new Vector3(0.3f, 2.2f, 0.3f), _matWall);
            ChildBox(z, "GridPost_1", new Vector3(3.2f, 1.1f, 0.5f), new Vector3(0.3f, 2.2f, 0.3f), _matWall);
            ChildBox(z, "NeonTube_EW", new Vector3(0f, 2.4f, 1.2f), new Vector3(9f, 0.16f, 0.16f), _matVault);
            // Disc south of center — clear pad midpoint sightline north to campus
            ChildBox(z, "DiscPad", new Vector3(0f, 0.12f, -2.2f), new Vector3(3.2f, 0.24f, 3.2f), _matSlide);
            ChildBox(z, "WallRun_S", new Vector3(0f, 1.3f, -3.8f), new Vector3(9f, 2.6f, 0.3f), _matWall);
        }

        /// <summary>Ninja N — twin silent towers + blade rails + climb; landing deck for jump exits.</summary>
        void BuildNinjaPad()
        {
            var z = Zone("Zone_Ninja", CxNinja, CzNinja);
            PadFloor(z, 12f, 9f, _matFloor);
            ChildBox(z, "SilentTower_A", new Vector3(-3.5f, 2.4f, 0.5f), new Vector3(1.8f, 4.8f, 1.8f), _matPad);
            ChildBox(z, "SilentTower_B", new Vector3(3.5f, 1.9f, 1.5f), new Vector3(1.8f, 3.8f, 1.8f), _matPad);
            // Rails north of center — leave mid courtyard open for sightline/run
            ChildBox(z, "BladeRail", new Vector3(0f, 1.6f, 2.0f), new Vector3(7f, 0.22f, 0.3f), _matVault);
            ChildBox(z, "BladeRail_High", new Vector3(0f, 3.0f, 2.4f), new Vector3(6f, 0.22f, 0.3f), _matVault);
            ChildBox(z, "ClimbFace", new Vector3(-5.5f, 1.7f, 1.2f), new Vector3(0.35f, 3.4f, 4.2f), _matWall);
            ChildBox(z, "LandingDeck", new Vector3(3.5f, 4.0f, 1.5f), new Vector3(2.8f, 0.28f, 2.8f), _matLoft);
        }

        void BuildSpawns()
        {
            // Corner lawns — face inward toward campus; emissive rim glow matches Toy_SpawnPad colors
            SpawnPad("Spawn_SW", 6f, 5f, 45f, ColSpawnTeal);
            SpawnPad("Spawn_SE", 66f, 5f, -45f, ColSpawnCoral);
            SpawnPad("Spawn_NW", 6f, 49f, 135f, ColSpawnViolet);
            SpawnPad("Spawn_NE", 66f, 49f, -135f, ColSpawnLime);
        }

        void SpawnPad(string name, float x, float z, float faceYawDeg, Color glow)
        {
            var padMat = MakeEmissiveMat(glow, 2.2f);
            Box(name, new Vector3(x, 0.03f, z), new Vector3(2.2f, 0.06f, 2.2f), padMat);
            var face = Box(name + "_Face", new Vector3(x, 0.08f, z), new Vector3(0.3f, 0.08f, 1.1f), _matVault);
            face.transform.localRotation = Quaternion.Euler(0f, faceYawDeg, 0f);
            face.transform.localPosition = new Vector3(x, 0.08f, z) + Quaternion.Euler(0f, faceYawDeg, 0f) * Vector3.forward * 0.9f;

            // Thin emissive ring sibling under PARK — no PointLight (URP AdditionalLightsPerObjectLimit=4).
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = name + "_RimGlow";
            rim.transform.SetParent(_root, false);
            rim.transform.localPosition = new Vector3(x, 0.07f, z);
            rim.transform.localRotation = Quaternion.identity;
            rim.transform.localScale = new Vector3(2.75f, 0.02f, 2.75f);
            var rimCol = rim.GetComponent<Collider>();
            if (rimCol != null)
                Object.DestroyImmediate(rimCol);
            var rimMr = rim.GetComponent<MeshRenderer>();
            if (rimMr != null)
                rimMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ApplyMat(rim, MakeEmissiveMat(glow, 2.8f));
        }

        void BuildOobSkirt()
        {
            const float t = 0.2f;
            float y = -t * 0.5f - 0.02f;
            float margin = 4f;
            Box("OOB_S", new Vector3(MapW * 0.5f, y, -margin * 0.5f), new Vector3(MapW + margin * 2f, t, margin), _matOob);
            Box("OOB_N", new Vector3(MapW * 0.5f, y, MapD + margin * 0.5f), new Vector3(MapW + margin * 2f, t, margin), _matOob);
            Box("OOB_W", new Vector3(-margin * 0.5f, y, MapD * 0.5f), new Vector3(margin, t, MapD), _matOob);
            Box("OOB_E", new Vector3(MapW + margin * 0.5f, y, MapD * 0.5f), new Vector3(margin, t, MapD), _matOob);
        }

        GameObject Box(string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            ApplyMat(go, mat);
            EnsureBoxCollider(go);
            return go;
        }

        GameObject ChildBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            ApplyMat(go, mat);
            EnsureBoxCollider(go);
            return go;
        }

        static void ApplyMat(GameObject go, Material mat)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
        }

        /// <summary>CreatePrimitive already adds a BoxCollider; keep it enabled for ski contact.</summary>
        static void EnsureBoxCollider(GameObject go)
        {
            var col = go.GetComponent<BoxCollider>();
            if (col == null)
                col = go.AddComponent<BoxCollider>();
            col.enabled = true;
        }
    }
}
