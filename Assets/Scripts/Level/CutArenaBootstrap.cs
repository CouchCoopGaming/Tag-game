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
    ///   Pass3: stronger ring tint + spawn lead lanes; playground gear lives in PgkLandmarkPlacer.
    ///   Pass4: named play-area floor pads (soft-play / swing / merry / kickball / hopscotch).
    ///   Pass5: thin theme pads — clear pad -> PadSlideExit -> Conn run-outs.
    ///   Pass6: Conn corridor ramps catch slide landings.
    ///   Pass7: Crash bowl open for EW chase.
    ///   Pass8: zone cubes removed (they stretched HiPoly). Kit forts, courts, ring
    ///   tube/monkey runs, and wall-run towers live in PgkLandmarkPlacer. No Flow stones.
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
        static readonly Color ColSlide = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColPadEdge = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColVault = new Color(0xF5 / 255f, 0xD5 / 255f, 0x47 / 255f, 1f);
        static readonly Color ColRamp = new Color(0xB8 / 255f, 0xC0 / 255f, 0xC8 / 255f, 1f);
        static readonly Color ColOob = new Color(0x3F / 255f, 0x7A / 255f, 0x4A / 255f, 1f);
        static readonly Color ColPath = new Color(0x6E / 255f, 0x4A / 255f, 0x38 / 255f, 1f);
        // Warmer outer-ring chase tint (readability without clutter)
        static readonly Color ColRing = new Color(0x8A / 255f, 0x5A / 255f, 0x3C / 255f, 1f);
        static readonly Color ColSpawnLead = new Color(0x7A / 255f, 0x6A / 255f, 0x48 / 255f, 1f);
        // Named play-area floors (mulch vs rubber) — read as zones beside chase lanes
        static readonly Color ColPlayMulch = new Color(0x7A / 255f, 0x4E / 255f, 0x32 / 255f, 1f);
        static readonly Color ColPlayRubber = new Color(0x3A / 255f, 0x4A / 255f, 0x5C / 255f, 1f);

        // Match ParkPropDresser Toy_SpawnPad_* palette (SW Teal, SE Coral, NW Violet, NE Lime)
        static readonly Color ColSpawnTeal = new Color(0x2E / 255f, 0xC4 / 255f, 0xB6 / 255f, 1f);
        static readonly Color ColSpawnCoral = new Color(0xFF / 255f, 0x6B / 255f, 0x4A / 255f, 1f);
        static readonly Color ColSpawnViolet = new Color(0x9B / 255f, 0x5C / 255f, 0xE6 / 255f, 1f);
        static readonly Color ColSpawnLime = new Color(0xA8 / 255f, 0xE6 / 255f, 0x1A / 255f, 1f);

        Transform _root;
        Material _matFloor, _matBowl, _matSlide, _matPad, _matVault, _matRamp, _matOob, _matPath, _matRing, _matSpawnLead, _matPlayMulch, _matPlayRubber;

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
            BuildChaseLanes();    // figure-8 + stronger outer-ring tint
            BuildSkiSpines();     // cardinal ski highways + pad connectors
            BuildSpawnLeads();    // spawn -> nearest spine in the first seconds
            BuildNamedPlayPads(); // mulch/rubber under named playground courts
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
            _matSlide = MakeMat(ColSlide);
            _matPad = MakeMat(ColPadEdge);
            _matVault = MakeMat(ColVault);
            _matRamp = MakeMat(ColRamp);
            _matOob = MakeMat(ColOob);
            _matPath = MakeMat(ColPath);
            _matRing = MakeMat(ColRing);
            _matSpawnLead = MakeMat(ColSpawnLead);
            _matPlayMulch = MakeMat(ColPlayMulch);
            _matPlayRubber = MakeMat(ColPlayRubber);
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

            // Outer ring — warmer/wider tint for chase readability (gear sits beside in placer)
            Box("Lane_Outer_S", new Vector3(CxTron, y, CzTron + 2f), new Vector3(32f, t, 6.5f), _matRing);
            Box("Lane_Outer_N", new Vector3(CxNinja, y, CzNinja - 2f), new Vector3(32f, t, 6.5f), _matRing);
            // Thin edge kerbs (south of S ring / north of N ring) — no clutter posts
            Box("EdgeRail_S", new Vector3(CxTron, 0.18f, CzTron - 1.6f), new Vector3(30f, 0.28f, 0.35f), _matVault);
            Box("EdgeRail_N", new Vector3(CxNinja, 0.18f, CzNinja + 1.6f), new Vector3(30f, 0.28f, 0.35f), _matVault);

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

            // Approach ramps: PadSlide/pad low end → crest to spine (skiMinSlope 6°); corner Conns under Play_Slide_* tips
            SkiRamp("Conn_Tron_N", new Vector3(CxTron, 0.48f, 13.2f), new Vector3(3.6f, 0.24f, 6.2f), -9f, 0f);
            SkiRamp("Conn_Ninja_S", new Vector3(CxNinja, 0.48f, 40.8f), new Vector3(3.6f, 0.24f, 6.2f), 9f, 0f);
            SkiRamp("Conn_Pirate_N", new Vector3(17.5f, 0.48f, 15.2f), new Vector3(4.2f, 0.24f, 5.6f), -9f, 0f);
            SkiRamp("Conn_Army_N", new Vector3(54.5f, 0.48f, 15.2f), new Vector3(4.2f, 0.24f, 5.6f), -9f, 0f);
            SkiRamp("Conn_Astro_S", new Vector3(17.5f, 0.48f, 38.8f), new Vector3(4.2f, 0.24f, 5.6f), 9f, 0f);
            SkiRamp("Conn_Knight_S", new Vector3(54.5f, 0.48f, 38.8f), new Vector3(4.2f, 0.24f, 5.6f), 9f, 0f);
            SkiRamp("Conn_Crash_W", new Vector3(30.2f, 0.40f, CzCrash), new Vector3(5.4f, 0.24f, 3.2f), -8f, 90f);
            SkiRamp("Conn_Crash_E", new Vector3(41.8f, 0.40f, CzCrash), new Vector3(5.4f, 0.24f, 3.2f), -8f, -90f);
        }

        void SkiRamp(string name, Vector3 localPos, Vector3 scale, float pitchDeg, float yawDeg)
        {
            var go = Box(name, localPos, scale, _matRamp);
            go.transform.localRotation = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        }

        /// <summary>
        /// Short tinted leads from corner spawns toward the nearest spine / Conn.
        /// Readable in the first ~3s of a run; kit forts sit beside these.
        /// </summary>
        void BuildSpawnLeads()
        {
            const float t = 0.05f;
            float y = t * 0.5f + 0.015f;
            const float w = 3.2f;
            // SW teal (6,5) → Pirate Conn / SW jct (SpineXw, SpineZs)
            Box("SpawnLead_SW", new Vector3(11f, y, 10f), new Vector3(10f, t, w), _matSpawnLead)
                .transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
            // SE coral (66,5) → Army Conn / SE jct
            Box("SpawnLead_SE", new Vector3(61f, y, 10f), new Vector3(10f, t, w), _matSpawnLead)
                .transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
            // NW violet (6,49) → Astro Conn / NW jct
            Box("SpawnLead_NW", new Vector3(11f, y, 44f), new Vector3(10f, t, w), _matSpawnLead)
                .transform.localRotation = Quaternion.Euler(0f, 140f, 0f);
            // NE lime (66,49) → Knight Conn / NE jct
            Box("SpawnLead_NE", new Vector3(61f, y, 44f), new Vector3(10f, t, w), _matSpawnLead)
                .transform.localRotation = Quaternion.Euler(0f, -140f, 0f);
        }

        /// <summary>
        /// Mulch / rubber carpets under kit districts (centers match PgkLandmarkPlacer).
        /// Tint only — kept off ski-spine midlines so the chase stays readable.
        /// </summary>
        void BuildNamedPlayPads()
        {
            const float t = 0.1f;
            float y = -t * 0.5f + 0.004f;
            // Forts sit on the theme-pad floors. These carpets are the courts and runs.
            // Merry reaches the west bars (apron tiles end at x=11.5) and stops short of Loop W (x=12).
            Box("PlayPad_Merry", new Vector3(7.4f, y, 24f), new Vector3(8.8f, t, 8f), _matPlayMulch);
            Box("PlayPad_Swing", new Vector3(67f, y, 31.5f), new Vector3(10f, t, 5.4f), _matPlayMulch);
            Box("PlayPad_Kickball", new Vector3(67f, y, 24f), new Vector3(10f, t, 10f), _matPlayRubber);
            // East-west court between Spawn_SW and the mast. East edge stops short of the pirate carpet (x=9).
            Box("PlayPad_Hopscotch_SW", new Vector3(4.5f, y, 9.4f), new Vector3(8.6f, t, 4f), _matPlayRubber);
            Box("PlayPad_Hopscotch_SE", new Vector3(70f, y, 11.5f), new Vector3(4f, t, 8f), _matPlayRubber);
            Box("PlayPad_Hopscotch_NE", new Vector3(70f, y, 38f), new Vector3(4f, t, 8f), _matPlayRubber);
            // North-south court south of Spawn_NW. Stops at z=46.5, 1.4 m short of the pad.
            Box("PlayPad_Hopscotch_NW", new Vector3(3.0f, y, 42f), new Vector3(4.6f, t, 9f), _matPlayRubber);
            // South ring stays below the outer lane; north ring stays inside the map edge.
            Box("PlayPad_Ring_S", new Vector3(36f, y, 3f), new Vector3(22f, t, 5.5f), _matPlayMulch);
            Box("PlayPad_Ring_N", new Vector3(36f, y, 51f), new Vector3(20f, t, 4.5f), _matPlayMulch);
            Box("PlayPad_Loop_W", new Vector3(16f, y, 26.5f), new Vector3(8f, t, 12f), _matPlayMulch);
            // East edge covers the beam lane (x=60.5) and stops 1 m short of the kickball pad (x=62).
            Box("PlayPad_Loop_E", new Vector3(56.5f, y, 27.5f), new Vector3(9f, t, 12f), _matPlayMulch);
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

        /// <summary>Crash = figure-8 X. Open mulch bowl; kit gear stays off the EW chase.</summary>
        void BuildCrashCore()
        {
            var z = Zone("Zone_Crash", CxCrash, CzCrash);
            PadFloor(z, 12f, 10f, _matBowl);
            // Sunken bowl only — stretched tower cubes used to dress as low-poly heroes.
            ChildBox(z, "Toy_Sandbox", new Vector3(0f, -0.9f, 0f), new Vector3(8f, 0.2f, 8f), _matBowl);
        }

        /// <summary>Pirate SW carpet under Play_SoftPlay. Kit fort owns the structure.</summary>
        void BuildPiratePad()
        {
            var z = Zone("Zone_Pirate", CxPirate, CzPirate);
            // Covers the tube street south of Play_SoftPlay (14, 9.75). Stops short of SpineZs.
            ChildBox(z, "PadFloor", new Vector3(1f, -0.08f, -2f), new Vector3(12f, 0.16f, 11f), _matFloor);
        }

        /// <summary>Army SE carpet under Play_ArmyBunker.</summary>
        void BuildArmyPad()
        {
            var z = Zone("Zone_Army", CxArmy, CzArmy);
            // Covers the bunker crawl and the east climb net. Stops short of SpineZs.
            ChildBox(z, "PadFloor", new Vector3(-1f, -0.08f, -2.5f), new Vector3(14f, 0.16f, 12f), _matFloor);
        }

        /// <summary>Astro NW carpet under Play_AstroLoft, north of SpineZn.</summary>
        void BuildAstroPad()
        {
            var z = Zone("Zone_Astro", CxAstro, CzAstro);
            // Covers Play_AstroLoft tubes (world z ~48) and stays north of SpineZn.
            ChildBox(z, "PadFloor", new Vector3(0f, -0.08f, 2f), new Vector3(12f, 0.16f, 12f), _matFloor);
        }

        /// <summary>Knight NE carpet under Play_KnightKeep.</summary>
        void BuildKnightPad()
        {
            var z = Zone("Zone_Knight", CxKnight, CzKnight);
            ChildBox(z, "PadFloor", new Vector3(0f, -0.08f, 2f), new Vector3(14f, 0.16f, 12f), _matFloor);
        }

        /// <summary>Tron disc lawn between the south ring and SpineZs. Ring gear is Play_Ring_S.</summary>
        void BuildTronPad()
        {
            var z = Zone("Zone_Tron", CxTron, CzTron);
            ChildBox(z, "PadFloor", new Vector3(0f, -0.08f, 3.5f), new Vector3(8f, 0.16f, 6f), _matPad);
        }

        /// <summary>Ninja approach south of Play_Ring_N. Blade landmark is a rail on the north rim.</summary>
        void BuildNinjaPad()
        {
            var z = Zone("Zone_Ninja", CxNinja, CzNinja);
            ChildBox(z, "PadFloor", new Vector3(0f, -0.08f, -1.5f), new Vector3(10f, 0.16f, 5f), _matFloor);
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
