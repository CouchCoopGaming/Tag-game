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
        static readonly Color ColSpawn = new Color(0xE2 / 255f, 0x3B / 255f, 0x2F / 255f, 1f);
        static readonly Color ColElbow = new Color(0xE2 / 255f, 0x3B / 255f, 0x2F / 255f, 1f);
        static readonly Color ColRamp = new Color(0xB8 / 255f, 0xC0 / 255f, 0xC8 / 255f, 1f);
        static readonly Color ColOob = new Color(0x3F / 255f, 0x7A / 255f, 0x4A / 255f, 1f);
        static readonly Color ColPath = new Color(0x6E / 255f, 0x4A / 255f, 0x38 / 255f, 1f);

        Transform _root;
        Material _matFloor, _matBowl, _matLoft, _matWall, _matSlide, _matPad, _matVault, _matSpawn, _matElbow, _matRamp, _matOob, _matPath;

        void Awake()
        {
            Build();
            if (GetComponent<Tag.Art.ParkPropDresser>() == null)
                gameObject.AddComponent<Tag.Art.ParkPropDresser>();
            if (GetComponent<Tag.Art.PgkLandmarkPlacer>() == null)
                gameObject.AddComponent<Tag.Art.PgkLandmarkPlacer>();
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
            _matSpawn = MakeMat(ColSpawn);
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
            // Long fall-line spines (Tribes) across campus — rubber strips
            Box("Spine_EW_S", new Vector3(36f, 0.02f, 18f), new Vector3(56f, 0.04f, 2.2f), _matSlide);
            Box("Spine_EW_N", new Vector3(36f, 0.02f, 36f), new Vector3(56f, 0.04f, 2.2f), _matSlide);
            Box("Spine_NS_W", new Vector3(24f, 0.02f, 27f), new Vector3(2.2f, 0.04f, 40f), _matSlide);
            Box("Spine_NS_E", new Vector3(48f, 0.02f, 27f), new Vector3(2.2f, 0.04f, 40f), _matSlide);
            Box("Spine_Diag_A", new Vector3(36f, 0.025f, 27f), new Vector3(40f, 0.04f, 1.6f), _matPath)
                .transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            Box("Spine_Diag_B", new Vector3(36f, 0.025f, 27f), new Vector3(40f, 0.04f, 1.6f), _matPath)
                .transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
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
            ChildBox(z, "Slide_Ramp", new Vector3(5f, 1.0f, -3f), new Vector3(2f, 0.3f, 5f), _matSlide)
                .transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            ElbowAt(z, "Toy_Hedge_Pirate", -6f, -5f, true, true);
        }

        void BuildArmyPad()
        {
            var z = Zone("Zone_Army", 58f, 12f);
            PadFloor(z, 16f, 14f, _matFloor);
            ChildBox(z, "Bunker_A", new Vector3(-3f, 0.7f, -2f), new Vector3(4f, 1.4f, 3f), _matPad);
            ChildBox(z, "Bunker_B", new Vector3(3f, 0.7f, 2f), new Vector3(4f, 1.4f, 3f), _matPad);
            ChildBox(z, "FoxholeTrench", new Vector3(0f, -0.4f, 0f), new Vector3(10f, 0.8f, 2f), _matBowl);
            ChildBox(z, "Ramp_Up", new Vector3(-5f, 0.8f, 4f), new Vector3(3f, 0.3f, 6f), _matRamp)
                .transform.localRotation = Quaternion.Euler(-16f, 90f, 0f);
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
            ChildBox(z, "Ramp_Keep", new Vector3(4f, 1.2f, -1f), new Vector3(2f, 0.3f, 5f), _matRamp)
                .transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
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
            // Corner lawns — face inward toward campus
            SpawnPad("Spawn_SW", 6f, 5f, 45f);
            SpawnPad("Spawn_SE", 66f, 5f, -45f);
            SpawnPad("Spawn_NW", 6f, 49f, 135f);
            SpawnPad("Spawn_NE", 66f, 49f, -135f);
            Elbow("Toy_Hedge_SW", 8f, 7f, true, true);
            Elbow("Toy_Hedge_SE", 64f, 7f, false, true);
            Elbow("Toy_Hedge_NW", 8f, 47f, true, false);
            Elbow("Toy_Hedge_NE", 64f, 47f, false, false);
        }

        void SpawnPad(string name, float x, float z, float faceYawDeg)
        {
            Box(name, new Vector3(x, 0.03f, z), new Vector3(2.2f, 0.06f, 2.2f), _matSpawn);
            var face = Box(name + "_Face", new Vector3(x, 0.08f, z), new Vector3(0.3f, 0.08f, 1.1f), _matVault);
            face.transform.localRotation = Quaternion.Euler(0f, faceYawDeg, 0f);
            face.transform.localPosition = new Vector3(x, 0.08f, z) + Quaternion.Euler(0f, faceYawDeg, 0f) * Vector3.forward * 0.9f;
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
            return go;
        }

        static void ApplyMat(GameObject go, Material mat)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
        }
    }
}