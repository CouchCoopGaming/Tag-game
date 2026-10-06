using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Local;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Builds the Mega Park graybox under MegaPark/P1 at world origin, scale 1.
    /// Solids come from <see cref="MegaParkP1Layout"/> so the headless audit and the
    /// scene cannot drift. Cubes keep the primitive collider. Paint has none.
    /// </summary>
    public class MegaParkP1Bootstrap : MonoBehaviour
    {
        const string RootName = "MegaPark";

        /// <summary>Assets/Prefabs/LaunchPad.prefab. Play.unity wires this.</summary>
        public GameObject launchPadPrefab;

        /// <summary>Assets/Prefabs/ZipLine.prefab. Play.unity wires this.</summary>
        public GameObject zipLinePrefab;

        /// <summary>0 Mega Park, 1 Pocket Park, 2 Stack Yard. Ignored after a player picks an arena.</summary>
        public int arenaId;

        Transform _p1;
        Material _mulch, _grass, _sand, _rubber, _blue, _yellow, _steel, _concrete, _cedar, _bark, _rim, _field;
        Material _soft, _pad, _merry, _amber, _swing, _army, _knight, _kick, _hop, _cover, _plate;
        Material _fence, _horizon, _leaf, _wood, _lamp, _trash, _skyline;
        Material _spawnSw, _spawnSe, _spawnNw, _spawnNe, _spawnRunS, _spawnRunN;

        /// <summary>True after a build whose layout audit passed.</summary>
        public bool Built { get; private set; }

        /// <summary>Last layout audit. False when the build threw or the audit failed.</summary>
        public bool LayoutOk { get; private set; }

        void Awake()
        {
            if (!ParkArena.HasExplicitChoice && PlayerPrefs.HasKey(ParkArena.PrefsKey))
                ParkArena.ApplySaved(true, PlayerPrefs.GetInt(ParkArena.PrefsKey, arenaId), arenaId);
            if (!ParkArena.HasExplicitChoice)
                ParkArena.Select(arenaId);
            try
            {
                if (ParkArena.IsPocket)
                    BuildPocket();
                else if (ParkArena.IsStack)
                    BuildStack();
                else
                    Build();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[MegaPark] build failed: " + e.Message);
                ClearBuilt();
            }
        }

        /// <summary>Drops a partial park so the campus fallback can take the origin.</summary>
        public void ClearBuilt()
        {
            Transform existing = transform.Find(RootName);
            if (existing != null)
                DestroyImmediate(existing.gameObject);
            _p1 = null;
            Built = false;
            LayoutOk = false;
        }

        /// <summary>
        /// Drops the live park, its pads and zips, and any dummy left from the previous arena.
        /// The next build (or the Play reload) respawns pawns on the new pads.
        /// </summary>
        public void TearDownArena()
        {
            ClearBuilt();
            DestroyStrays();
        }

        void DestroyStrays()
        {
            LaunchPad[] pads = UnityEngine.Object.FindObjectsByType<LaunchPad>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pads.Length; i++)
            {
                if (pads[i] != null)
                    DestroyImmediate(pads[i].gameObject);
            }
            ZipLine[] zips = UnityEngine.Object.FindObjectsByType<ZipLine>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < zips.Length; i++)
            {
                if (zips[i] != null)
                    DestroyImmediate(zips[i].gameObject);
            }
            ItController[] bodies = UnityEngine.Object.FindObjectsByType<ItController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] == null) continue;
                string name = bodies[i].gameObject.name;
                if (name.StartsWith("Dummy"))
                    DestroyImmediate(bodies[i].gameObject);
            }
        }

        void FinishArena()
        {
            ParkMinimap map = UnityEngine.Object.FindAnyObjectByType<ParkMinimap>();
            if (map != null)
                map.Rebuild();
            LocalPlayerSpawner spawner = UnityEngine.Object.FindAnyObjectByType<LocalPlayerSpawner>();
            if (spawner != null)
                spawner.Reseat();
        }

        [ContextMenu("Rebuild Mega Park P1")]
        public void Build()
        {
            ApplyLook();
            EnsureMaterials();
            EnsureRoot();
            Transform solids = BuildSolids();
            Transform ramps = BuildRamps();
            Transform paint = BuildPaint();
            Transform spawns = BuildSpawns();
            BuildLoopMarkers();
            BuildLaunchPads();
            BuildZipLines();
            BuildLabels();
            Transform dress = BuildDressing();
            BatchStatic(solids);
            BatchStatic(ramps);
            BatchStatic(paint);
            BatchStatic(spawns);
            BatchStatic(dress);

            MegaParkP1Layout.Audit audit = MegaParkP1Layout.Run();
            LayoutOk = audit.Ok;
            Built = audit.Ok && _p1 != null;
            if (audit.Ok)
                Debug.Log(audit.Line);
            else
                Debug.LogError(audit.Line + " :: " + audit.Failure);
            if (!Built)
                ClearBuilt();
            else
                FinishArena();
        }

        void EnsureRoot()
        {
            Transform existing = transform.Find(RootName);
            if (existing == null)
            {
                var rootGo = new GameObject(RootName);
                rootGo.transform.SetParent(transform, false);
                existing = rootGo.transform;
            }

            existing.localPosition = Vector3.zero;
            existing.localRotation = Quaternion.identity;
            existing.localScale = Vector3.one;

            Transform prior = existing.Find("P1");
            if (prior != null)
                DestroyImmediate(prior.gameObject);

            var p1 = new GameObject("P1");
            p1.transform.SetParent(existing, false);
            _p1 = p1.transform;
        }

        void EnsureMaterials()
        {
            if (_mulch != null)
                return;
            _mulch = Face("mulch");
            _grass = Face("grass");
            _sand = Face("sand");
            _rubber = Face("rubber");
            _blue = Face("cling");
            _yellow = Face("slide");
            _steel = Face("steel");
            _concrete = Face("concrete");
            _cedar = Face("cedar");
            _bark = Face("bark");
            _rim = Face("rim");
            _field = Face("field");
            // Zone callouts. Blue stays cling-only. Yellow stays slide-only. Orange is a grapple plate.
            _soft = Face("soft");
            _pad = Face("pad");
            _merry = Face("merry");
            _amber = Face("amber");
            _swing = Face("swing");
            _army = Face("army");
            _knight = Face("knight");
            _kick = Face("kick");
            _hop = Face("hop");
            _cover = Face("cover");
            _plate = Face("plate");
            _fence = Face("fence");
            _horizon = Face("horizon");
            _leaf = Face("leaf");
            _wood = Face("wood");
            _lamp = Face("lamp");
            _trash = Face("trash");
            _skyline = Face("skyline");
            _spawnSw = Make(new Color(0x2E / 255f, 0xC4 / 255f, 0xB6 / 255f), "MEGA_SpawnSW", 0.2f, 0f, "panel");
            _spawnSe = Make(new Color(0xFF / 255f, 0x6B / 255f, 0x6B / 255f), "MEGA_SpawnSE", 0.2f, 0f, "panel");
            _spawnNw = Make(new Color(0x9B / 255f, 0x5D / 255f, 0xE5 / 255f), "MEGA_SpawnNW", 0.2f, 0f, "panel");
            _spawnNe = Make(new Color(0xC6 / 255f, 0xF2 / 255f, 0x4A / 255f), "MEGA_SpawnNE", 0.2f, 0f, "panel");
            _spawnRunS = Make(new Color(0xFF / 255f, 0xE0 / 255f, 0x8A / 255f), "MEGA_SpawnRunS", 0.2f, 0f, "panel");
            _spawnRunN = Make(new Color(0x8A / 255f, 0xD7 / 255f, 0xFF / 255f), "MEGA_SpawnRunN", 0.2f, 0f, "panel");
        }

        Material Face(string name)
        {
            MegaParkP1Layout.TryLook(name, out float r, out float g, out float b, out float smooth, out float metal);
            return Make(new Color(r, g, b, 1f), "MEGA_" + name, smooth, metal, name);
        }

        static Material Make(Color c, string name, float smooth, float metal, string kind)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Diffuse");
            var m = new Material(shader) { color = c, name = name };
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness"))
                m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic"))
                m.SetFloat("_Metallic", metal);
            Texture2D tex = MakeTex(kind, c);
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap"))
                    m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex"))
                    m.SetTexture("_MainTex", tex);
            }
            if (kind == "fence")
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.45f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = 2450;
                if (m.HasProperty("_Mode"))
                    m.SetFloat("_Mode", 1f);
                m.SetTextureScale("_BaseMap", new Vector2(36f, 14f));
                m.SetTextureScale("_MainTex", new Vector2(36f, 14f));
            }
            m.enableInstancing = true;
            return m;
        }

        static Texture2D MakeTex(string kind, Color c)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "MEGA_Tex_" + kind;
            var pix = new Color[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float wobble = 0.96f + 0.08f * Hash(x, y);
                    if (wobble > 1.04f) wobble = 1.04f;
                    Color p = c * wobble;
                    p.a = 1f;
                    if (kind == "mulch" || kind == "rubber")
                    {
                        float crumb = Hash(x / 4, y / 4);
                        p = Color.Lerp(c * 0.96f, c * 1.04f, crumb);
                    }
                    else if (kind == "sand")
                    {
                        p = Color.Lerp(c * 0.97f, c * 1.03f, Hash(x, y));
                    }
                    else if (kind == "steel" || kind == "lamp" || kind == "plate")
                    {
                        float streak = 0.97f + 0.06f * Hash(0, y / 2);
                        p = c * Mathf.Min(1.04f, streak);
                    }
                    else if (kind == "wood" || kind == "cedar" || kind == "bark")
                    {
                        float grain = 0.96f + 0.08f * Mathf.Abs(Mathf.Sin(y * 0.55f + Hash(x / 8, 0) * 3f));
                        p = c * Mathf.Min(1.04f, grain);
                    }
                    else if (kind == "concrete" || kind == "skyline")
                    {
                        float seam = (x % 16 == 0 || y % 16 == 0) ? 0.96f : 1.02f;
                        p = c * seam;
                    }
                    else if (kind == "grass" || kind == "leaf" || kind == "field" || kind == "horizon")
                    {
                        float blade = ((x + y * 3) % 5 == 0) ? 1.04f : 0.97f;
                        p = c * blade;
                    }
                    else if (kind == "fence")
                    {
                        bool wire = (x % 8) < 2 || (y % 8) < 2;
                        p = c;
                        p.a = wire ? 1f : 0f;
                    }
                    p.r = Mathf.Clamp01(p.r);
                    p.g = Mathf.Clamp01(p.g);
                    p.b = Mathf.Clamp01(p.b);
                    pix[y * n + x] = p;
                }
            }
            tex.SetPixels(pix);
            tex.Apply(false, true);
            return tex;
        }

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 255) / 255f;
        }

        Material Pick(string mat)
        {
            switch (mat)
            {
                case "grass": return _grass;
                case "sand": return _sand;
                case "rubber": return _rubber;
                case "blue": return _blue;
                case "yellow": return _yellow;
                case "steel": return _steel;
                case "concrete": return _concrete;
                case "cedar": return _cedar;
                case "bark": return _bark;
                case "rim": return _rim;
                case "field": return _field;
                case "soft": return _soft;
                case "pad": return _pad;
                case "merry": return _merry;
                case "amber": return _amber;
                case "swing": return _swing;
                case "army": return _army;
                case "knight": return _knight;
                case "kick": return _kick;
                case "hop": return _hop;
                case "cover": return _cover;
                case "plate": return _plate;
                default: return _mulch;
            }
        }

        Transform Group(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_p1, false);
            return go.transform;
        }

        Transform BuildSolids()
        {
            return BuildSolidList(MegaParkP1Layout.BuildSolids());
        }

        Transform BuildSolidList(MegaParkP1Layout.Solid[] solids)
        {
            Transform g = Group("Solids");
            var zones = new Dictionary<string, Transform>();
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = s.Name;
                go.transform.SetParent(Occlusion(g, zones, s.Zone), false);
                go.transform.localPosition = new Vector3(s.X, s.Y, s.Z);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(s.Sx, s.Sy, s.Sz);
                go.isStatic = true;
                MeshRenderer r = go.GetComponent<MeshRenderer>();
                if (r != null)
                    r.sharedMaterial = s.Name.StartsWith("Fence_") ? _fence : Pick(s.Mat);
            }
            return g;
        }

        Transform BuildRamps()
        {
            return BuildRampList(MegaParkP1Layout.BuildRamps());
        }

        Transform BuildRampList(MegaParkP1Layout.Ramp[] ramps)
        {
            var drawn = new List<MegaParkP1Layout.RampDraw>();
            var merged = new List<MegaParkP1Layout.Ramp>();
            MegaParkP1Layout.PlanRampColliders(ramps, drawn, merged, out _);
            Transform g = Group("Ramps");
            var zones = new Dictionary<string, Transform>();
            for (int i = 0; i < drawn.Count; i++)
                BuildRamp(Occlusion(g, zones, drawn[i].Ramp.Zone), drawn[i].Ramp, drawn[i].KeepCollider, true);
            for (int i = 0; i < merged.Count; i++)
                BuildRamp(Occlusion(g, zones, merged[i].Zone), merged[i], true, false);
            return g;
        }

        void BuildRamp(Transform parent, MegaParkP1Layout.Ramp r, bool keepCollider, bool visible)
        {
            Vector3 a = new Vector3(r.X0, r.Y0, r.Z0);
            Vector3 b = new Vector3(r.X1, r.Y1, r.Z1);
            Vector3 along = b - a;
            float len = along.magnitude;
            if (len < 0.01f)
                return;
            along /= len;
            Vector3 side = Vector3.Cross(along, Vector3.up);
            if (side.sqrMagnitude < 1e-6f)
                side = Vector3.Cross(along, Vector3.forward);
            side.Normalize();
            Vector3 normal = Vector3.Cross(side, along).normalized;
            if (normal.y < 0f)
                normal = -normal;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = r.Name;
            go.transform.SetParent(parent, false);
            Vector3 topMid = (a + b) * 0.5f;
            go.transform.localPosition = topMid - normal * (r.Thickness * 0.5f);
            go.transform.localRotation = Quaternion.LookRotation(along, normal);
            go.transform.localScale = new Vector3(r.Width, r.Thickness, len);
            go.isStatic = true;
            MeshRenderer rend = go.GetComponent<MeshRenderer>();
            if (!visible)
            {
                if (rend != null)
                    DestroyImmediate(rend);
            }
            else if (rend != null)
                rend.sharedMaterial = Pick(r.Mat);
            if (!keepCollider)
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                    DestroyImmediate(col);
            }
        }

        Transform BuildPaint()
        {
            Transform g = Group("Paint");
            var zones = new Dictionary<string, Transform>();
            Paint(Occlusion(g, zones, "Z1"), "Z1_SoftPlay", 2f, 38f, 2f, 36f, _soft, 0.025f);
            Paint(Occlusion(g, zones, "Z2"), "Z2_ClingFooting", 2f, 10f, 38f, 78f, _pad, 0.025f);
            Paint(Occlusion(g, zones, "Z3"), "Z3_Merry", 22f, 46f, 34f, 60f, _merry, 0.025f);
            Paint(Occlusion(g, zones, "Z4"), "Z4_SlideMountain", 22f, 56f, 72f, 98f, _amber, 0.025f);
            Paint(Occlusion(g, zones, "Z5"), "Z5_SwingGrove", 58f, 100f, 78f, 98f, _swing, 0.025f);
            Paint(Occlusion(g, zones, "Z6"), "Z6_Army", 118f, 158f, 10f, 46f, _army, 0.025f);
            Paint(Occlusion(g, zones, "Z6"), "Z6_Knight", 118f, 158f, 54f, 90f, _knight, 0.025f);
            Paint(Occlusion(g, zones, "Z7"), "Z7_Field", 78f, 114f, 28f, 68f, _field, 0.04f);
            Paint(Occlusion(g, zones, "Z7"), "Z7_MouthSouth", 64f, 78f, 28f, 34f, _field, 0.04f);
            Paint(Occlusion(g, zones, "Z7"), "Z7_MouthNorth", 64f, 78f, 66f, 68f, _field, 0.04f);
            Paint(Occlusion(g, zones, "Z10"), "Z10_Hopscotch", 118f, 156f, 2f, 22f, _hop, 0.025f);
            Paint(Occlusion(g, zones, "Spine"), "Spine_South", 38f, 118f, 12f, 20f, _concrete, 0.06f);
            Paint(Occlusion(g, zones, "Spine"), "Spine_North", 14f, 130f, 83f, 89f, _concrete, 0.06f);
            Paint(Occlusion(g, zones, "Spine"), "Spine_West", 10f, 14f, 2f, 98f, _concrete, 0.06f);
            Paint(Occlusion(g, zones, "Spine"), "Spine_East", 130f, 138f, 10f, 90f, _concrete, 0.06f);
            // Crossing mouths. Paint only, so the stripe is not a lip.
            Paint(Occlusion(g, zones, "Cross"), "CrossA_West", 42f, 50f, 48.4f, 51.6f, _sand, 0.03f);
            Paint(Occlusion(g, zones, "Cross"), "CrossA_East", 74f, 82f, 48.4f, 51.6f, _sand, 0.03f);
            Paint(Occlusion(g, zones, "Cross"), "CrossA_South", 60.2f, 63.8f, 30f, 38f, _sand, 0.03f);
            Paint(Occlusion(g, zones, "Cross"), "CrossA_North", 60.2f, 63.8f, 62f, 70f, _sand, 0.03f);
            Paint(Occlusion(g, zones, "Cross"), "CrossB_South", 24f, 44f, 42.2f, 43.6f, _concrete, 0.03f);
            Paint(Occlusion(g, zones, "Cross"), "CrossB_North", 24f, 44f, 52.4f, 53.8f, _concrete, 0.03f);
            return g;
        }

        Transform Occlusion(Transform parent, Dictionary<string, Transform> cache, string zone)
        {
            string key = "Occlusion_" + zone;
            Transform found;
            if (cache.TryGetValue(key, out found))
                return found;
            var go = new GameObject(key);
            go.transform.SetParent(parent, false);
            go.isStatic = true;
            cache[key] = go.transform;
            return go.transform;
        }

        static void BatchStatic(Transform root)
        {
            if (root == null) return;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith("Occlusion_")) continue;
                Transform[] all = child.GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < all.Length; t++)
                    all[t].gameObject.isStatic = true;
                StaticBatchingUtility.Combine(child.gameObject);
            }
        }

        void BuildLaunchPads()
        {
            BuildLaunchPadList(MegaParkP1Layout.LaunchPads);
        }

        void BuildLaunchPadList(MegaParkP1Layout.PadSpot[] spots)
        {
            Transform g = Group("LaunchPads");
            for (int i = 0; i < spots.Length; i++)
            {
                MegaParkP1Layout.PadSpot s = spots[i];
                GameObject go;
                if (launchPadPrefab != null)
                    go = (GameObject)Instantiate(launchPadPrefab, g);
                else
                    go = new GameObject(s.Name);
                go.name = s.Name;
                go.transform.SetParent(g, false);
                go.transform.localPosition = new Vector3(s.X, s.Y, s.Z);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                LaunchPad pad = go.GetComponent<LaunchPad>();
                if (pad == null)
                    pad = go.AddComponent<LaunchPad>();
                pad.apexHeight = s.Apex;
                pad.horizontalDir = new Vector3(s.DirX, 0f, s.DirZ);
                pad.horizontalSpeed = s.Speed;
            }
        }

        void BuildZipLines()
        {
            BuildZipLineList(MegaParkP1Layout.ZipLines);
        }

        void BuildZipLineList(MegaParkP1Layout.ZipLineSpot[] lines)
        {
            Transform g = Group("ZipLines");
            for (int i = 0; i < lines.Length; i++)
            {
                MegaParkP1Layout.ZipLineSpot s = lines[i];
                GameObject go;
                if (zipLinePrefab != null)
                    go = (GameObject)Instantiate(zipLinePrefab, g);
                else
                    go = new GameObject(s.Name);
                go.name = s.Name;
                go.transform.SetParent(g, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                ZipLine zip = go.GetComponent<ZipLine>();
                if (zip == null)
                    zip = go.AddComponent<ZipLine>();
                Transform a = go.transform.Find("PointA");
                if (a == null)
                {
                    var point = new GameObject("PointA");
                    point.transform.SetParent(go.transform, false);
                    a = point.transform;
                }
                Transform b = go.transform.Find("PointB");
                if (b == null)
                {
                    var point = new GameObject("PointB");
                    point.transform.SetParent(go.transform, false);
                    b = point.transform;
                }
                a.name = s.Name + "_A";
                b.name = s.Name + "_B";
                a.position = new Vector3(s.Ax, s.Ay, s.Az);
                b.position = new Vector3(s.Bx, s.By, s.Bz);
                zip.pointA = a;
                zip.pointB = b;
                zip.rideSpeed = s.Speed;
            }
        }

        void Paint(Transform parent, string name, float x0, float x1, float z0, float z1, Material mat, float yTop)
        {
            const float t = 0.02f;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3((x0 + x1) * 0.5f, yTop - t * 0.5f, (z0 + z1) * 0.5f);
            go.transform.localScale = new Vector3(x1 - x0, t, z1 - z0);
            go.isStatic = true;
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            if (r != null)
                r.sharedMaterial = mat;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                DestroyImmediate(col);
        }

        Transform BuildSpawns()
        {
            Transform g = Group("Spawns");
            var zones = new Dictionary<string, Transform>();
            Transform bucket = Occlusion(g, zones, "Spawns");
            BuildSpawnList(bucket, MegaParkP1Layout.Spawns);
            BuildSpawnList(bucket, MegaParkP1Layout.RunnerSpawns);
            return g;
        }

        Transform BuildSpawnPads(MegaParkP1Layout.SpawnPad[] pads)
        {
            Transform g = Group("Spawns");
            var zones = new Dictionary<string, Transform>();
            Transform bucket = Occlusion(g, zones, "Spawns");
            BuildSpawnList(bucket, pads);
            return g;
        }

        void BuildSpawnList(Transform g, MegaParkP1Layout.SpawnPad[] pads)
        {
            for (int i = 0; i < pads.Length; i++)
            {
                MegaParkP1Layout.SpawnPad pad = pads[i];
                var go = new GameObject(pad.Name);
                go.transform.SetParent(g, false);
                go.transform.localPosition = new Vector3(pad.X, 0f, pad.Z);
                go.transform.localRotation = Quaternion.Euler(0f, pad.YawDeg, 0f);

                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = "Pad";
                disc.transform.SetParent(go.transform, false);
                disc.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                disc.transform.localScale = new Vector3(2f, 0.02f, 2f);
                disc.isStatic = true;
                MeshRenderer r = disc.GetComponent<MeshRenderer>();
                if (r != null)
                    r.sharedMaterial = PadMat(pad.Name);
                Collider col = disc.GetComponent<Collider>();
                if (col != null)
                    DestroyImmediate(col);

                GameObject wedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wedge.name = "Facing";
                wedge.transform.SetParent(go.transform, false);
                wedge.transform.localPosition = new Vector3(0f, 0.08f, 1.15f);
                wedge.transform.localScale = new Vector3(0.4f, 0.05f, 0.7f);
                wedge.isStatic = true;
                MeshRenderer wr = wedge.GetComponent<MeshRenderer>();
                if (wr != null)
                    wr.sharedMaterial = _concrete;
                Collider wc = wedge.GetComponent<Collider>();
                if (wc != null)
                    DestroyImmediate(wc);
            }
        }

        Material PadMat(string name)
        {
            switch (name)
            {
                case "Spawn_SW": return _spawnSw;
                case "Spawn_SE": return _spawnSe;
                case "Spawn_NW": return _spawnNw;
                case "Spawn_NE": return _spawnNe;
                case "Spawn_RunS": return _spawnRunS;
                case "Spawn_RunN": return _spawnRunN;
                default: return _rubber;
            }
        }

        void BuildLoopMarkers()
        {
            BuildLoop(MegaParkP1Layout.LoopCcw);
        }

        void BuildLoop(MegaParkP1Layout.Pt[] loop)
        {
            Transform g = Group("TrailTag");
            for (int i = 0; i < loop.Length; i++)
            {
                var go = new GameObject("WP_" + i.ToString("00"));
                go.transform.SetParent(g, false);
                MegaParkP1Layout.Pt p = loop[i];
                go.transform.localPosition = new Vector3(p.X, p.Y, p.Z);
                MegaParkP1Layout.Pt next = loop[(i + 1) % loop.Length];
                Vector3 dir = new Vector3(next.X - p.X, 0f, next.Z - p.Z);
                if (dir.sqrMagnitude > 0.0001f)
                    go.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        void BuildLabels()
        {
            Transform g = Group("Labels");
            Label(g, "SOFT-PLAY", 18f, 24f);
            Label(g, "CLING", 8f, 58f);
            Label(g, "MERRY", 34f, 56f);
            Label(g, "SLIDE", 40f, 84f);
            Label(g, "SWING", 78f, 86f);
            Label(g, "FORTS", 140f, 48f);
            Label(g, "KICKBALL", 96f, 52f);
            Label(g, "CRASH", 62f, 50f);
            Label(g, "BARS", 78f, 16f);
            Label(g, "HOPSCOTCH", 136f, 18f);
        }

        void Label(Transform parent, string text, float x, float z)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 4.5f, z);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.45f;
            tm.color = new Color(1f, 0.95f, 0.75f, 1f);
            tm.fontStyle = FontStyle.Bold;
        }

        void ApplyLook()
        {
            Light sun = null;
            Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                Light l = lights[i];
                if (l == null || l.type != LightType.Directional) continue;
                if (sun == null)
                    sun = l;
                else
                    l.enabled = false;
            }
            if (sun == null)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.color = new Color(MegaParkP1Layout.SunR, MegaParkP1Layout.SunG, MegaParkP1Layout.SunB, 1f);
            sun.intensity = MegaParkP1Layout.SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = MegaParkP1Layout.ShadowStrength;
            sun.shadowBias = 0.08f;
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            sun.transform.rotation = Quaternion.Euler(MegaParkP1Layout.SunPitch, MegaParkP1Layout.SunYaw, 0f);
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(MegaParkP1Layout.AmbSkyR, MegaParkP1Layout.AmbSkyG, MegaParkP1Layout.AmbSkyB, 1f);
            RenderSettings.ambientEquatorColor = new Color(MegaParkP1Layout.AmbEqR, MegaParkP1Layout.AmbEqG, MegaParkP1Layout.AmbEqB, 1f);
            RenderSettings.ambientGroundColor = new Color(MegaParkP1Layout.AmbGndR, MegaParkP1Layout.AmbGndG, MegaParkP1Layout.AmbGndB, 1f);
            RenderSettings.ambientIntensity = MegaParkP1Layout.AmbIntensity;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader == null)
                skyShader = Shader.Find("Skybox/Panoramic");
            if (skyShader != null)
            {
                MegaParkP1Layout.TryLook("sky", out float r, out float g, out float b, out _, out _);
                var sky = new Material(skyShader) { name = "MEGA_Sky" };
                var tint = new Color(r, g, b, 1f);
                if (sky.HasProperty("_SkyTint"))
                    sky.SetColor("_SkyTint", tint);
                if (sky.HasProperty("_GroundColor"))
                    sky.SetColor("_GroundColor", new Color(0.45f, 0.32f, 0.22f, 1f));
                if (sky.HasProperty("_Exposure"))
                    sky.SetFloat("_Exposure", 1.25f);
                if (sky.HasProperty("_AtmosphereThickness"))
                    sky.SetFloat("_AtmosphereThickness", 0.85f);
                if (sky.HasProperty("_SunSize"))
                    sky.SetFloat("_SunSize", 0.045f);
                if (sky.HasProperty("_SunSizeConvergence"))
                    sky.SetFloat("_SunSizeConvergence", 5f);
                RenderSettings.skybox = sky;
            }
        }

        Transform BuildDressing()
        {
            return BuildDressList(MegaParkP1Layout.BuildDressing());
        }

        Transform BuildDressList(MegaParkP1Layout.Dress[] all)
        {
            Transform g = Group("Dressing");
            var zones = new Dictionary<string, Transform>();
            Transform bucket = Occlusion(g, zones, "Dress");
            for (int i = 0; i < all.Length; i++)
            {
                MegaParkP1Layout.Dress d = all[i];
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = d.Name;
                go.transform.SetParent(bucket, false);
                go.transform.localPosition = new Vector3(d.X, d.Y, d.Z);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(d.Sx, d.Sy, d.Sz);
                go.isStatic = true;
                StripCollider(go);
                MeshRenderer rend = go.GetComponent<MeshRenderer>();
                if (rend != null)
                    rend.sharedMaterial = DressMat(d.Mat);
            }
            return g;
        }

        static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                DestroyImmediate(col);
        }

        Material DressMat(string mat)
        {
            switch (mat)
            {
                case "horizon": return _horizon;
                case "bark": return _bark;
                case "leaf": return _leaf;
                case "wood": return _wood;
                case "lamp": return _lamp;
                case "trash": return _trash;
                case "skyline": return _skyline;
                case "grass": return _grass;
                default: return _mulch;
            }
        }

        /// <summary>
        /// Same cube kit, materials, pads, and zips as Mega Park, on the pocket layout.
        /// </summary>
        public void BuildPocket()
        {
            ApplyLook();
            EnsureMaterials();
            EnsureRoot();
            Transform solids = BuildSolidList(PocketParkLayout.BuildSolids());
            Transform ramps = BuildRampList(PocketParkLayout.BuildRamps());
            Transform paint = BuildPocketPaint();
            Transform spawns = BuildSpawnPads(PocketParkLayout.Spawns);
            BuildLoop(PocketParkLayout.LoopCcw);
            BuildLaunchPadList(PocketParkLayout.LaunchPads);
            BuildZipLineList(PocketParkLayout.ZipLines);
            BuildPocketLabels();
            Transform dress = BuildDressList(PocketParkLayout.BuildDressing());
            BatchStatic(solids);
            BatchStatic(ramps);
            BatchStatic(paint);
            BatchStatic(spawns);
            BatchStatic(dress);

            PocketParkLayout.Audit audit = PocketParkLayout.Run();
            LayoutOk = audit.Ok;
            Built = audit.Ok && _p1 != null;
            if (audit.Ok)
                Debug.Log(audit.Line);
            else
                Debug.LogError(audit.Line + " :: " + audit.Failure);
            if (!Built)
                ClearBuilt();
            else
                FinishArena();
        }

        Transform BuildPocketPaint()
        {
            Transform g = Group("Paint");
            var zones = new Dictionary<string, Transform>();
            Paint(Occlusion(g, zones, "Dome"), "Dome_Carpet", 36f, 52f, 18f, 32f, _amber, 0.025f);
            Paint(Occlusion(g, zones, "Cling"), "Cling_Carpet", 49f, 58f, 20f, 30f, _pad, 0.025f);
            Paint(Occlusion(g, zones, "Yard"), "Yard_Carpet", 16f, 28f, 16f, 36f, _cover, 0.025f);
            Paint(Occlusion(g, zones, "Cut"), "Cut_South", 12f, 68f, 15.2f, 16.8f, _concrete, 0.02f);
            Paint(Occlusion(g, zones, "Cut"), "Cut_North", 12f, 68f, 37.2f, 38.8f, _concrete, 0.02f);
            Paint(Occlusion(g, zones, "Cut"), "Cut_West", 17.2f, 18.8f, 10f, 40f, _concrete, 0.02f);
            return g;
        }

        void BuildPocketLabels()
        {
            Transform g = Group("Labels");
            Label(g, "DOME", 44f, 25f);
            Label(g, "LANE", 53f, 24f);
            Label(g, "YARD", 24f, 32f);
        }

        /// <summary>
        /// Same cube kit, materials, pads, and zips as Mega Park, on the stack layout.
        /// </summary>
        public void BuildStack()
        {
            ApplyLook();
            EnsureMaterials();
            EnsureRoot();
            Transform solids = BuildSolidList(StackYardLayout.BuildSolids());
            Transform ramps = BuildRampList(StackYardLayout.BuildRamps());
            Transform paint = BuildStackPaint();
            Transform spawns = BuildSpawnPads(StackYardLayout.Spawns);
            BuildLoop(StackYardLayout.LoopCcw);
            BuildLaunchPadList(StackYardLayout.LaunchPads);
            BuildZipLineList(StackYardLayout.ZipLines);
            BuildStackLabels();
            Transform dress = BuildDressList(StackYardLayout.BuildDressing());
            BatchStatic(solids);
            BatchStatic(ramps);
            BatchStatic(paint);
            BatchStatic(spawns);
            BatchStatic(dress);

            StackYardLayout.Audit audit = StackYardLayout.Run();
            LayoutOk = audit.Ok;
            Built = audit.Ok && _p1 != null;
            if (audit.Ok)
                Debug.Log(audit.Line);
            else
                Debug.LogError(audit.Line + " :: " + audit.Failure);
            if (!Built)
                ClearBuilt();
            else
                FinishArena();
        }

        Transform BuildStackPaint()
        {
            Transform g = Group("Paint");
            var zones = new Dictionary<string, Transform>();
            Paint(Occlusion(g, zones, "Yard"), "Yard_Carpet", 8f, 50f, 8f, 28f, _cover, 0.025f);
            Paint(Occlusion(g, zones, "Lane"), "Lane_Carpet", 60f, 102f, 8f, 28f, _army, 0.025f);
            Paint(Occlusion(g, zones, "Mid"), "Mid_Carpet", 8f, 50f, 40f, 62f, _soft, 0.025f);
            Paint(Occlusion(g, zones, "Roof"), "Roof_Carpet", 60f, 102f, 40f, 62f, _amber, 0.025f);
            return g;
        }

        void BuildStackLabels()
        {
            Transform g = Group("Labels");
            Label(g, "YARD", 20f, 14f);
            Label(g, "LANE", 90f, 14f);
            Label(g, "MID", 20f, 60f);
            Label(g, "ROOF", 90f, 60f);
        }
    }
}
