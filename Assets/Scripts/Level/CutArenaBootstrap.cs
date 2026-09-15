using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Mega multi-playground PARK campus. Graybox meters, then WorldScale on the root.
    /// Origin = SW playable corner; +X east, +Z north. Plenty of open lawn between pads.
    /// </summary>
    public class CutArenaBootstrap : MonoBehaviour
    {
        const string RootName = "PARK";
        /// <summary>Soft-play fantasy scale (~10x).</summary>
        public const float WorldScale = 10f;

        // Playable campus in graybox meters (world = * WorldScale)
        public const float MapW = 72f;
        public const float MapD = 54f;

        static readonly Color ColFloor = new Color(0x5C / 255f, 0x3A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColBowl = new Color(0x5C / 255f, 0x3A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColLoft = new Color(0xC5 / 255f, 0xCB / 255f, 0xD1 / 255f, 1f);
        static readonly Color ColWallRun = new Color(0x3D / 255f, 0x7E / 255f, 1f, 1f);
        static readonly Color ColSlide = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColPadEdge = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColVault = new Color(0xF5 / 255f, 0xD5 / 255f, 0x47 / 255f, 1f);
        static readonly Color ColElbow = new Color(0xE2 / 255f, 0x3B / 255f, 0x2F / 255f, 1f);
        static readonly Color ColRamp = new Color(0xB8 / 255f, 0xC0 / 255f, 0xC8 / 255f, 1f);
        static readonly Color ColOob = new Color(0x3F / 255f, 0x7A / 255f, 0x4A / 255f, 1f);
        static readonly Color ColPath = new Color(0x6E / 255f, 0x4A / 255f, 0x38 / 255f, 1f);

        // Match ParkPropDresser Toy_SpawnPad_* palette (SW Teal, SE Coral, NW Violet, NE Lime)
        static readonly Color ColSpawnTeal = new Color(0x2E / 255f, 0xC4 / 255f, 0xB6 / 255f, 1f);
        static readonly Color ColSpawnCoral = new Color(0xFF / 255f, 0x6B / 255f, 0x4A / 255f, 1f);
        static readonly Color ColSpawnViolet = new Color(0x9B / 255f, 0x5C / 255f, 0xE6 / 255f, 1f);
        static readonly Color ColSpawnLime = new Color(0xA8 / 255f, 0xE6 / 255f, 0x1A / 255f, 1f);

        Transform _root;
        Material _matFloor, _matBowl, _matLoft, _matWall, _matSlide, _matPad, _matVault, _matElbow, _matRamp, _matOob, _matPath;

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
            BuildSkiSpines();
            BuildCrashCore();      // center
            BuildPiratePad();      // SW
            BuildArmyPad();        // SE
            BuildAstroPad();       // NW
            BuildKnightPad();      // NE
            BuildTronPad();        // S mid
            BuildNinjaPad();       // N mid
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
            _matElbow = MakeMat(ColElbow);
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

        // --- Ground / space ---------------------------------------------------------

        void BuildGround()
        {
            const float t = 0.25f;
            // One big mulch lawn — open chase / ski space between pads
            Box("Lawn_Campus", new Vector3(MapW * 0.5f, -t * 0.5f, MapD * 0.5f),
                new Vector3(MapW, t, MapD), _matFloor);
        }

        void BuildSkiSpines()
        {
            // Wider/thicker fall-line spines (Tribes ski) — continuous rubber highways + junctions
            const float spineW = 3.0f;
            const float spineT = 0.12f;
            const float diagW = 2.4f;
            float y = spineT * 0.5f;

            Box("Spine_EW_S", new Vector3(36f, y, 18f), new Vector3(56f, spineT, spineW), _matSlide);
            Box("Spine_EW_N", new Vector3(36f, y, 36f), new Vector3(56f, spineT, spineW), _matSlide);
            Box("Spine_NS_W", new Vector3(24f, y, 27f), new Vector3(spineW, spineT, 40f), _matSlide);
            Box("Spine_NS_E", new Vector3(48f, y, 27f), new Vector3(spineW, spineT, 40f), _matSlide);
            Box("Spine_Diag_A", new Vector3(36f, y + 0.01f, 27f), new Vector3(42f, spineT, diagW), _matPath)
                .transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            Box("Spine_Diag_B", new Vector3(36f, y + 0.01f, 27f), new Vector3(42f, spineT, diagW), _matPath)
                .transform.localRotation = Quaternion.Euler(0f, -35f, 0f);

            // Junction hubs so crossings stay readable / continuous
            float jy = y + 0.02f;
            const float j = 4.5f;
            Box("Spine_Jct_SW", new Vector3(24f, jy, 18f), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_SE", new Vector3(48f, jy, 18f), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_NW", new Vector3(24f, jy, 36f), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_NE", new Vector3(48f, jy, 36f), new Vector3(j, spineT, j), _matRamp);
            Box("Spine_Jct_Core", new Vector3(36f, jy + 0.01f, 27f), new Vector3(5.2f, spineT, 5.2f), _matPath);

            // Approach ramps: pad → nearest spine (clearer Tribes entry slopes)
            SkiRamp("Conn_Tron_N", new Vector3(36f, 0.55f, 12.8f), new Vector3(3.4f, 0.28f, 6.5f), -12f, 0f);
            SkiRamp("Conn_Ninja_S", new Vector3(36f, 0.55f, 41.2f), new Vector3(3.4f, 0.28f, 6.5f), 12f, 0f);
            SkiRamp("Conn_Pirate_N", new Vector3(14f, 0.55f, 15.2f), new Vector3(3.2f, 0.28f, 5.5f), -12f, 0f);
            SkiRamp("Conn_Army_N", new Vector3(58f, 0.55f, 15.2f), new Vector3(3.2f, 0.28f, 5.5f), -12f, 0f);
            SkiRamp("Conn_Astro_S", new Vector3(14f, 0.55f, 38.8f), new Vector3(3.2f, 0.28f, 5.5f), 12f, 0f);
            SkiRamp("Conn_Knight_S", new Vector3(58f, 0.55f, 38.8f), new Vector3(3.2f, 0.28f, 5.5f), 12f, 0f);
            SkiRamp("Conn_Crash_W", new Vector3(29.2f, 0.45f, 27f), new Vector3(5.5f, 0.28f, 3.2f), -10f, 90f);
            SkiRamp("Conn_Crash_E", new Vector3(42.8f, 0.45f, 27f), new Vector3(5.5f, 0.28f, 3.2f), -10f, -90f);
        }

        void SkiRamp(string name, Vector3 localPos, Vector3 scale, float pitchDeg, float yawDeg)
        {
            var go = Box(name, localPos, scale, _matRamp);
            go.transform.localRotation = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        }

        // --- Zone pads --------------------------------------------------------------

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

        void BuildCrashCore()
        {
            var z = Zone("Zone_Crash", 36f, 27f);
            PadFloor(z, 18f, 16f, _matBowl);
            // Sunken bowl
            ChildBox(z, "Toy_Sandbox", new Vector3(0f, -1.0f, 0f), new Vector3(10f, 0.2f, 10f), _matBowl);
            ChildBox(z, "Toy_SandboxRim_S", new Vector3(0f, 0.4f, -5f), new Vector3(10.4f, 0.8f, 0.35f), _matVault);
            ChildBox(z, "Toy_SandboxRim_N", new Vector3(0f, 0.4f, 5f), new Vector3(10.4f, 0.8f, 0.35f), _matVault);
            ChildBox(z, "Toy_SandboxRim_W", new Vector3(-5f, 0.4f, 0f), new Vector3(0.35f, 0.8f, 10.4f), _matVault);
            ChildBox(z, "Toy_SandboxRim_E", new Vector3(5f, 0.4f, 0f), new Vector3(0.35f, 0.8f, 10.4f), _matVault);
            // Twin towers + loft
            ChildBox(z, "Toy_TwinTower_W", new Vector3(-5f, 2.0f, 4f), new Vector3(2.2f, 4f, 2.2f), _matWall);
            ChildBox(z, "Toy_TwinTower_E", new Vector3(5f, 2.0f, 4f), new Vector3(2.2f, 4f, 2.2f), _matWall);
            ChildBox(z, "Toy_Tower", new Vector3(0f, 3.2f, 5.5f), new Vector3(8f, 0.35f, 4f), _matLoft);
            ChildBox(z, "Toy_ClimbWall_West", new Vector3(-7.5f, 1.5f, 0f), new Vector3(0.4f, 3f, 8f), _matWall);
            ChildBox(z, "Toy_Bars_0", new Vector3(-2f, 0.9f, -6f), new Vector3(0.25f, 1.8f, 3f), _matVault);
            ChildBox(z, "Toy_Bars_1", new Vector3(0f, 0.9f, -6f), new Vector3(0.25f, 1.8f, 3f), _matVault);
            ChildBox(z, "Toy_Bars_2", new Vector3(2f, 0.9f, -6f), new Vector3(0.25f, 1.8f, 3f), _matVault);
            ElbowAt(z, "Toy_Hedge_Crash", -7f, -6f, true, true);
        }

        void BuildPiratePad()
        {
            var z = Zone("Zone_Pirate", 14f, 12f);
            PadFloor(z, 16f, 14f, _matFloor);
            ChildBox(z, "MastBase", new Vector3(0f, 0.5f, 0f), new Vector3(3f, 1f, 3f), _matVault);
            ChildBox(z, "Deck_Low", new Vector3(0f, 1.2f, 0f), new Vector3(8f, 0.3f, 5f), _matLoft);
            ChildBox(z, "Deck_High", new Vector3(-2f, 2.4f, 2f), new Vector3(4f, 0.3f, 3f), _matLoft);
            ChildBox(z, "Plank_Run", new Vector3(4f, 1.5f, 0f), new Vector3(6f, 0.25f, 1.2f), _matVault);
            ChildBox(z, "ClimbNetWall", new Vector3(-6f, 1.6f, 0f), new Vector3(0.35f, 3.2f, 6f), _matWall);
            ChildBox(z, "Slide_Ramp", new Vector3(5f, 1.0f, -3f), new Vector3(2.8f, 0.3f, 5.5f), _matSlide)
                .transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
            ElbowAt(z, "Toy_Hedge_Pirate", -6f, -5f, true, true);
        }

        void BuildArmyPad()
        {
            var z = Zone("Zone_Army", 58f, 12f);
            PadFloor(z, 16f, 14f, _matFloor);
            ChildBox(z, "Bunker_A", new Vector3(-3f, 0.7f, -2f), new Vector3(4f, 1.4f, 3f), _matPad);
            ChildBox(z, "Bunker_B", new Vector3(3f, 0.7f, 2f), new Vector3(4f, 1.4f, 3f), _matPad);
            ChildBox(z, "FoxholeTrench", new Vector3(0f, -0.4f, 0f), new Vector3(10f, 0.8f, 2f), _matBowl);
            ChildBox(z, "Ramp_Up", new Vector3(-5f, 0.8f, 4f), new Vector3(3.6f, 0.3f, 6.5f), _matRamp)
                .transform.localRotation = Quaternion.Euler(-14f, 90f, 0f);
            ChildBox(z, "Wall_Cover", new Vector3(6f, 1.2f, 0f), new Vector3(0.4f, 2.4f, 8f), _matWall);
            ChildBox(z, "Vault_Low", new Vector3(0f, 0.5f, 5f), new Vector3(6f, 1f, 0.4f), _matVault);
        }

        void BuildAstroPad()
        {
            var z = Zone("Zone_Astro", 14f, 42f);
            PadFloor(z, 16f, 14f, _matFloor);
            ChildBox(z, "Loft_Ring", new Vector3(0f, 2.0f, 0f), new Vector3(10f, 0.3f, 10f), _matLoft);
            ChildBox(z, "VisorPipe_A", new Vector3(-3f, 1.0f, -3f), new Vector3(2f, 2f, 6f), _matWall)
                .transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            ChildBox(z, "VisorPipe_B", new Vector3(3f, 1.0f, 3f), new Vector3(2f, 2f, 6f), _matWall)
                .transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            ChildBox(z, "HalfPipe_L", new Vector3(-5f, 0.8f, 0f), new Vector3(0.5f, 2.4f, 8f), _matSlide);
            ChildBox(z, "HalfPipe_R", new Vector3(5f, 0.8f, 0f), new Vector3(0.5f, 2.4f, 8f), _matSlide);
            ChildBox(z, "Ladder_Stub", new Vector3(0f, 1.0f, -5.5f), new Vector3(1.2f, 2f, 0.4f), _matVault);
        }

        void BuildKnightPad()
        {
            var z = Zone("Zone_Knight", 58f, 42f);
            PadFloor(z, 16f, 14f, _matFloor);
            ChildBox(z, "Courtyard", new Vector3(0f, 0.05f, 0f), new Vector3(10f, 0.1f, 8f), _matLoft);
            ChildBox(z, "ShieldWall_N", new Vector3(0f, 1.5f, 5f), new Vector3(10f, 3f, 0.45f), _matWall);
            ChildBox(z, "ShieldWall_W", new Vector3(-5.5f, 1.2f, 0f), new Vector3(0.45f, 2.4f, 8f), _matWall);
            ChildBox(z, "Keep_Tower", new Vector3(4f, 2.5f, 3f), new Vector3(3f, 5f, 3f), _matPad);
            ChildBox(z, "Battlement", new Vector3(4f, 5.2f, 3f), new Vector3(4f, 0.4f, 4f), _matLoft);
            ChildBox(z, "VaultGate", new Vector3(0f, 0.9f, -4f), new Vector3(3f, 1.8f, 0.5f), _matVault);
            ChildBox(z, "Ramp_Keep", new Vector3(4f, 1.2f, -1f), new Vector3(2.8f, 0.3f, 5.5f), _matRamp)
                .transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);
        }

        void BuildTronPad()
        {
            var z = Zone("Zone_Tron", 36f, 8f);
            PadFloor(z, 14f, 10f, _matPad);
            // Grid courtyard posts
            for (int i = 0; i < 4; i++)
            {
                float x = -4.5f + i * 3f;
                ChildBox(z, $"GridPost_{i}", new Vector3(x, 1.2f, 0f), new Vector3(0.35f, 2.4f, 0.35f), _matWall);
            }
            ChildBox(z, "NeonTube_EW", new Vector3(0f, 2.4f, 0f), new Vector3(12f, 0.2f, 0.2f), _matVault);
            ChildBox(z, "DiscPad", new Vector3(0f, 0.15f, -2f), new Vector3(4f, 0.3f, 4f), _matSlide);
            ChildBox(z, "WallRun_S", new Vector3(0f, 1.4f, -4.5f), new Vector3(10f, 2.8f, 0.35f), _matWall);
        }

        void BuildNinjaPad()
        {
            var z = Zone("Zone_Ninja", 36f, 46f);
            PadFloor(z, 14f, 10f, _matFloor);
            ChildBox(z, "SilentTower_A", new Vector3(-4f, 2.5f, 0f), new Vector3(2f, 5f, 2f), _matPad);
            ChildBox(z, "SilentTower_B", new Vector3(4f, 2.0f, 2f), new Vector3(2f, 4f, 2f), _matPad);
            ChildBox(z, "BladeRail", new Vector3(0f, 1.6f, 0f), new Vector3(10f, 0.25f, 0.35f), _matVault);
            ChildBox(z, "BladeRail_High", new Vector3(0f, 3.2f, 1f), new Vector3(8f, 0.25f, 0.35f), _matVault);
            ChildBox(z, "ClimbFace", new Vector3(-6f, 1.8f, 0f), new Vector3(0.4f, 3.6f, 6f), _matWall);
            ChildBox(z, "LandingDeck", new Vector3(4f, 4.2f, 2f), new Vector3(3f, 0.3f, 3f), _matLoft);
        }

        void BuildSpawns()
        {
            // Corner lawns — face inward toward campus; emissive rim glow matches Toy_SpawnPad colors
            SpawnPad("Spawn_SW", 6f, 5f, 45f, ColSpawnTeal);
            SpawnPad("Spawn_SE", 66f, 5f, -45f, ColSpawnCoral);
            SpawnPad("Spawn_NW", 6f, 49f, 135f, ColSpawnViolet);
            SpawnPad("Spawn_NE", 66f, 49f, -135f, ColSpawnLime);
            Elbow("Toy_Hedge_SW", 8f, 7f, true, true);
            Elbow("Toy_Hedge_SE", 64f, 7f, false, true);
            Elbow("Toy_Hedge_NW", 8f, 47f, true, false);
            Elbow("Toy_Hedge_NE", 64f, 47f, false, false);
        }

        void SpawnPad(string name, float x, float z, float faceYawDeg, Color glow)
        {
            // Per-pad emissive mat (graybox). ParkPropDresser may hide this MeshRenderer when dressed.
            var padMat = MakeEmissiveMat(glow, 2.2f);
            Box(name, new Vector3(x, 0.03f, z), new Vector3(2.2f, 0.06f, 2.2f), padMat);
            var face = Box(name + "_Face", new Vector3(x, 0.08f, z), new Vector3(0.3f, 0.08f, 1.1f), _matVault);
            face.transform.localRotation = Quaternion.Euler(0f, faceYawDeg, 0f);
            face.transform.localPosition = new Vector3(x, 0.08f, z) + Quaternion.Euler(0f, faceYawDeg, 0f) * Vector3.forward * 0.9f;

            // Thin emissive ring sibling under PARK — no PointLight (URP AdditionalLightsPerObjectLimit=4;
            // pad PointLights were stealing slots from ItMarker). Survives dresser hideGrayboxMeshWhenDressed.
            // Raised Y + slightly oversized XY vs pad (2.2) to avoid z-fight with pad disc; no collider/shadows.
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

        void Elbow(string name, float x, float z, bool towardEast, bool towardNorth)
        {
            const float h = 1.2f, arm = 2.2f, thick = 0.4f;
            float sx = towardEast ? 1f : -1f;
            float sz = towardNorth ? 1f : -1f;
            var parent = new GameObject(name);
            parent.transform.SetParent(_root, false);
            parent.transform.localPosition = new Vector3(x, 0f, z);
            ChildBox(parent.transform, "Arm_X", new Vector3(sx * arm * 0.5f, h * 0.5f, 0f), new Vector3(arm, h, thick), _matElbow);
            ChildBox(parent.transform, "Arm_Z", new Vector3(0f, h * 0.5f, sz * arm * 0.5f), new Vector3(thick, h, arm), _matElbow);
        }

        void ElbowAt(Transform z, string name, float lx, float lz, bool towardEast, bool towardNorth)
        {
            const float h = 1.2f, arm = 2.0f, thick = 0.35f;
            float sx = towardEast ? 1f : -1f;
            float sz = towardNorth ? 1f : -1f;
            var parent = new GameObject(name);
            parent.transform.SetParent(z, false);
            parent.transform.localPosition = new Vector3(lx, 0f, lz);
            ChildBox(parent.transform, "Arm_X", new Vector3(sx * arm * 0.5f, h * 0.5f, 0f), new Vector3(arm, h, thick), _matElbow);
            ChildBox(parent.transform, "Arm_Z", new Vector3(0f, h * 0.5f, sz * arm * 0.5f), new Vector3(thick, h, arm), _matElbow);
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
