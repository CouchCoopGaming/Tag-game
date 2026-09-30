using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Mega Park P1 graybox only. Footprint 160 m E–W × 100 m N–S.
    /// Origin = SW playable corner. +X east, +Z north, +Y up. Root scale is 1 (real meters).
    ///
    /// Builds fence, 3 m grass collar, footing, four CCW spawn markers, concrete spines,
    /// an open sand crash bowl, and the kickball diamond. Reserved zones are flush paint.
    /// No Flow stones. No toy meshes for P2–P4. No blue (cling) and no yellow (slide).
    ///
    /// Does not touch the campus map. Do not add this component to the campus scene.
    /// Campus tip 0a42007 stays the live map. Producer gates land.
    /// Host GameObject at world origin, rotation identity, scale 1.
    /// </summary>
    public class MegaParkP1Bootstrap : MonoBehaviour
    {
        const string RootName = "MegaPark";

        public const float MapW = 160f;
        public const float MapD = 100f;
        public const float BowlFloorY = -1f;
        public const float Collar = 3f;
        public const float LoopLengthM = 472f;

        // Closed by returning to point 0. Every vertex is a locked spawn or the
        // intersection of a locked spawn latitude/longitude with the locked south spine.
        public static readonly Vector3[] LoopCcw =
        {
            new Vector3(8f, 0f, 8f),
            new Vector3(38f, 0f, 8f),
            new Vector3(38f, 0f, 16f),
            new Vector3(118f, 0f, 16f),
            new Vector3(118f, 0f, 8f),
            new Vector3(152f, 0f, 8f),
            new Vector3(152f, 0f, 92f),
            new Vector3(8f, 0f, 92f),
        };

        // Locked palette. Blue and yellow are intentionally absent in P1.
        static readonly Color ColMulch = new Color(0x5C / 255f, 0x3A / 255f, 0x2E / 255f, 1f);
        static readonly Color ColGrass = new Color(0x3F / 255f, 0x7A / 255f, 0x4A / 255f, 1f);
        static readonly Color ColConcrete = new Color(0xC5 / 255f, 0xCB / 255f, 0xD1 / 255f, 1f);
        static readonly Color ColRubber = new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f, 1f);
        // No locked hex in the blueprint. Graybox tints only — not verb colors.
        static readonly Color ColSand = new Color(0xE6 / 255f, 0xD2 / 255f, 0xA2 / 255f, 1f);
        static readonly Color ColField = new Color(0x3C / 255f, 0x9A / 255f, 0x58 / 255f, 1f);
        static readonly Color ColCedar = new Color(0x8A / 255f, 0x5A / 255f, 0x3C / 255f, 1f);
        static readonly Color ColBark = new Color(0x3E / 255f, 0x26 / 255f, 0x1C / 255f, 1f);
        static readonly Color ColRimPad = new Color(0x6B / 255f, 0x46 / 255f, 0x36 / 255f, 1f);

        Transform _root;
        Transform _p1;
        Material _mulch, _grass, _concrete, _rubber, _sand, _field, _cedar, _bark, _rim;

        void Awake()
        {
            Build();
        }

        [ContextMenu("Rebuild Mega Park P1")]
        public void Build()
        {
            EnsureMaterials();
            EnsureRoot();
            BuildCollar();
            BuildFence();
            BuildMulch();
            BuildReservedPads();
            BuildSpines();
            BuildBowl();
            BuildKickball();
            BuildSpawns();
            BuildLoopMarkers();
            WarnIfLoopDrifted();
        }

        void EnsureRoot()
        {
            var existing = transform.Find(RootName);
            if (existing == null)
            {
                var rootGo = new GameObject(RootName);
                rootGo.transform.SetParent(transform, false);
                rootGo.transform.localPosition = Vector3.zero;
                rootGo.transform.localRotation = Quaternion.identity;
                rootGo.transform.localScale = Vector3.one;
                existing = rootGo.transform;
            }

            _root = existing;
            var prior = _root.Find("P1");
            if (prior != null)
                DestroyImmediate(prior.gameObject);

            var p1 = new GameObject("P1");
            p1.transform.SetParent(_root, false);
            p1.transform.localPosition = Vector3.zero;
            p1.transform.localRotation = Quaternion.identity;
            p1.transform.localScale = Vector3.one;
            _p1 = p1.transform;
        }

        void EnsureMaterials()
        {
            if (_mulch != null)
                return;
            _mulch = MakeGround(ColMulch, "MEGA_Mulch");
            _grass = MakeGround(ColGrass, "MEGA_Grass");
            _concrete = MakeGround(ColConcrete, "MEGA_Concrete");
            _rubber = MakeGround(ColRubber, "MEGA_Rubber");
            _sand = MakeGround(ColSand, "MEGA_Sand");
            _field = MakeGround(ColField, "MEGA_Field");
            _cedar = MakeGround(ColCedar, "MEGA_Cedar");
            _bark = MakeGround(ColBark, "MEGA_Bark");
            _rim = MakeGround(ColRimPad, "MEGA_RimPad");
        }

        static Material MakeGround(Color c, string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            var m = new Material(shader) { color = c, name = name };
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", 0.06f);
            if (m.HasProperty("_Glossiness"))
                m.SetFloat("_Glossiness", 0.06f);
            if (m.HasProperty("_Metallic"))
                m.SetFloat("_Metallic", 0f);
            return m;
        }

        Transform Group(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_p1, false);
            return go.transform;
        }

        GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, bool collide)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null)
                r.sharedMaterial = mat;
            if (!collide)
            {
                var col = go.GetComponent<Collider>();
                if (col != null)
                    DestroyImmediate(col);
            }
            return go;
        }

        // --- Collar / fence ---------------------------------------------------------

        void BuildCollar()
        {
            var g = Group("Collar");
            const float t = 0.2f;
            float y = -t * 0.5f - 0.02f;
            float span = MapW + Collar * 2f;
            Prim(g, "Collar_S", PrimitiveType.Cube, new Vector3(MapW * 0.5f, y, -Collar * 0.5f), new Vector3(span, t, Collar), _grass, true);
            Prim(g, "Collar_N", PrimitiveType.Cube, new Vector3(MapW * 0.5f, y, MapD + Collar * 0.5f), new Vector3(span, t, Collar), _grass, true);
            Prim(g, "Collar_W", PrimitiveType.Cube, new Vector3(-Collar * 0.5f, y, MapD * 0.5f), new Vector3(Collar, t, MapD), _grass, true);
            Prim(g, "Collar_E", PrimitiveType.Cube, new Vector3(MapW + Collar * 0.5f, y, MapD * 0.5f), new Vector3(Collar, t, MapD), _grass, true);
        }

        void BuildFence()
        {
            // Graybox boundary only. Height is NOT a locked meter (blueprint locks the 3 m collar).
            // Rubber, not steel: steel is reserved for P2 bars. Not blue, not a cling surface.
            var g = Group("Fence");
            const float h = 2.4f;
            const float t = 0.08f;
            float y = h * 0.5f;
            Prim(g, "Fence_S", PrimitiveType.Cube, new Vector3(MapW * 0.5f, y, -t * 0.5f), new Vector3(MapW + t, h, t), _rubber, true);
            Prim(g, "Fence_N", PrimitiveType.Cube, new Vector3(MapW * 0.5f, y, MapD + t * 0.5f), new Vector3(MapW + t, h, t), _rubber, true);
            Prim(g, "Fence_W", PrimitiveType.Cube, new Vector3(-t * 0.5f, y, MapD * 0.5f), new Vector3(t, h, MapD), _rubber, true);
            Prim(g, "Fence_E", PrimitiveType.Cube, new Vector3(MapW + t * 0.5f, y, MapD * 0.5f), new Vector3(t, h, MapD), _rubber, true);
        }

        // --- Footing ----------------------------------------------------------------

        void BuildMulch()
        {
            // Hole at the bowl so the sand floor is the collider there. Top at Y=0.
            var g = Group("Footing");
            const float t = 0.2f;
            float y = -t * 0.5f;
            Slab(g, "Mulch_West", 0f, 46f, 0f, MapD, y, t, _mulch, true);
            Slab(g, "Mulch_East", 78f, MapW, 0f, MapD, y, t, _mulch, true);
            Slab(g, "Mulch_South", 46f, 78f, 0f, 34f, y, t, _mulch, true);
            Slab(g, "Mulch_North", 46f, 78f, 66f, MapD, y, t, _mulch, true);
        }

        void Slab(Transform parent, string name, float x0, float x1, float z0, float z1, float y, float t, Material mat, bool collide)
        {
            Prim(parent, name, PrimitiveType.Cube,
                new Vector3((x0 + x1) * 0.5f, y, (z0 + z1) * 0.5f),
                new Vector3(x1 - x0, t, z1 - z0),
                mat, collide);
        }

        void Paint(Transform parent, string name, float x0, float x1, float z0, float z1, Material mat, float yTop)
        {
            // Flush landmark. No collider, so it cannot hide a silhouette or trip.
            const float t = 0.02f;
            Slab(parent, name, x0, x1, z0, z1, yTop - t * 0.5f, t, mat, false);
        }

        void BuildReservedPads()
        {
            var g = Group("Reserved");
            // Not blue. Not yellow. Empty ground only.
            Paint(g, "Z1_SoftPlay_SW", 2f, 38f, 2f, 36f, _cedar, 0.025f);
            Paint(g, "Z2_ClingFooting_W", 2f, 10f, 38f, 78f, _rubber, 0.025f);
            // x[14,18] stays bare mulch: the locked 4 m lawn east of the cling strip.
            Paint(g, "Z3_Merry", 22f, 46f, 34f, 60f, _cedar, 0.025f);
            Paint(g, "Z4_SlideMountain_NW", 22f, 56f, 72f, 98f, _rim, 0.025f);
            Paint(g, "Z5_SwingGrove_N", 58f, 100f, 78f, 98f, _cedar, 0.025f);
            // Forts split by the locked ≥8 m gap z[46,54]. No raised floors.
            Paint(g, "Z6_Army_S", 118f, 158f, 10f, 46f, _bark, 0.025f);
            Paint(g, "Z6_Knight_N", 118f, 158f, 54f, 90f, _bark, 0.025f);
            Paint(g, "Z10_Hopscotch_SE", 118f, 156f, 2f, 22f, _concrete, 0.025f);
        }

        void BuildSpines()
        {
            // Flat concrete. Collider off so a proud lip is not a trip wall.
            // Nothing is built in the 1.05 m above the south spine.
            // Crash cross x[52,72] z[40,58] is NOT paved.
            var g = Group("Spines");
            Paint(g, "Spine_South", 38f, 118f, 12f, 20f, _concrete, 0.06f);
            Paint(g, "Spine_North", 14f, 130f, 83f, 89f, _concrete, 0.06f);
            Paint(g, "Spine_West", 10f, 14f, 2f, 98f, _concrete, 0.06f);
            Paint(g, "Spine_East", 130f, 138f, 10f, 90f, _concrete, 0.06f);
        }

        // --- Bowl -------------------------------------------------------------------

        void BuildBowl()
        {
            var g = Group("Bowl");
            const float t = 0.2f;
            Prim(g, "Sandbox_Floor", PrimitiveType.Cube,
                new Vector3(62f, BowlFloorY - t * 0.5f, 50f),
                new Vector3(32f, t, 32f),
                _sand, true);

            // 4 m banks stay outside the locked open rect (margin to that rect is 6 m / 8 m).
            const float run = 4f;
            float pitch = Mathf.Atan(1f / run) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(run * run + 1f);
            Bank(g, "SandBank_W", new Vector3(46f + run * 0.5f, -0.5f, 50f), new Vector3(len, 0.3f, 32f), new Vector3(0f, 0f, -pitch));
            Bank(g, "SandBank_E", new Vector3(78f - run * 0.5f, -0.5f, 50f), new Vector3(len, 0.3f, 32f), new Vector3(0f, 0f, pitch));
            Bank(g, "SandBank_S", new Vector3(62f, -0.5f, 34f + run * 0.5f), new Vector3(32f, 0.3f, len), new Vector3(pitch, 0f, 0f));
            Bank(g, "SandBank_N", new Vector3(62f, -0.5f, 66f - run * 0.5f), new Vector3(32f, 0.3f, len), new Vector3(-pitch, 0f, 0f));

            // Named sandbox toys, nubs ≤ 0.6. SW / NE only. Outside x[52,72] z[40,58].
            Toy(g, "Toy_Sandbox_Bucket_SW", PrimitiveType.Cylinder, new Vector3(51f, BowlFloorY + 0.175f, 42.5f), new Vector3(0.40f, 0.175f, 0.40f), Quaternion.identity);
            Toy(g, "Toy_Sandbox_Shovel_SW", PrimitiveType.Cube, new Vector3(51f, BowlFloorY + 0.03f, 43.6f), new Vector3(0.55f, 0.06f, 0.08f), Quaternion.Euler(0f, 25f, 0f));
            Toy(g, "Toy_Sandbox_Mold_NE", PrimitiveType.Cylinder, new Vector3(73f, BowlFloorY + 0.06f, 60.4f), new Vector3(0.50f, 0.06f, 0.50f), Quaternion.identity);
            Toy(g, "Toy_Sandbox_Sifter_NE", PrimitiveType.Cube, new Vector3(73f, BowlFloorY + 0.03f, 61.2f), new Vector3(0.45f, 0.06f, 0.40f), Quaternion.Euler(0f, 15f, 0f));
        }

        void Bank(Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 euler)
        {
            var go = Prim(parent, name, PrimitiveType.Cube, pos, scale, _sand, true);
            go.transform.localRotation = Quaternion.Euler(euler);
        }

        void Toy(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var go = Prim(parent, name, type, pos, scale, _rubber, true);
            go.transform.localRotation = rot;
        }

        // --- Kickball ---------------------------------------------------------------

        void BuildKickball()
        {
            var g = Group("Kickball");
            // West of x=78 is the open bowl mouth (no rail, no bases).
            Paint(g, "Field", 78f, 114f, 28f, 68f, _field, 0.04f);
            Paint(g, "Field_MouthSouth", 64f, 78f, 28f, 34f, _field, 0.04f);
            Paint(g, "Field_MouthNorth", 64f, 78f, 66f, 68f, _field, 0.04f);

            Base(g, "Base_Home", new Vector3(96f, 0.10f, 34f));
            Base(g, "Base_First", new Vector3(110f, 0.10f, 48f));
            Base(g, "Base_Second", new Vector3(96f, 0.10f, 62f));
            Base(g, "Base_Third", new Vector3(82f, 0.10f, 48f));

            // Height 0.25 (inside 0.15–0.30). Unity cylinder is 2 units tall, so y-scale is 0.125.
            Prim(g, "Mound", PrimitiveType.Cylinder, new Vector3(96f, 0.125f, 48f), new Vector3(2.4f, 0.125f, 2.4f), _rubber, true);

            // East vault rail only. Top at 0.90. Rubber, not yellow and not steel (steel is P2 bars).
            Prim(g, "Rail_East", PrimitiveType.Cube, new Vector3(114f, 0.45f, 48f), new Vector3(0.12f, 0.90f, 40f), _rubber, true);
        }

        void Base(Transform parent, string name, Vector3 pos)
        {
            Prim(parent, name, PrimitiveType.Cube, pos, new Vector3(0.90f, 0.20f, 0.90f), _rubber, true);
        }

        // --- Spawns / loop ----------------------------------------------------------

        void BuildSpawns()
        {
            var g = Group("Spawns");
            Spawn(g, "Spawn_SW", 8f, 8f, Vector3.right);
            Spawn(g, "Spawn_SE", 152f, 8f, Vector3.forward);
            Spawn(g, "Spawn_NE", 152f, 92f, Vector3.left);
            Spawn(g, "Spawn_NW", 8f, 92f, Vector3.back);
        }

        void Spawn(Transform parent, string name, float x, float z, Vector3 face)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, z);
            go.transform.localRotation = Quaternion.LookRotation(face, Vector3.up);

            // Diameter 2. Cylinder mesh height is 2, so y-scale 0.02 → 0.04 m.
            Prim(go.transform, "Pad", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(2f, 0.02f, 2f), _rubber, false);
            // Local +Z is the trail direction. Concrete, not a verb color.
            Prim(go.transform, "Facing", PrimitiveType.Cube, new Vector3(0f, 0.08f, 1.15f), new Vector3(0.40f, 0.05f, 0.70f), _concrete, false);
        }

        void BuildLoopMarkers()
        {
            var g = Group("TrailTag");
            for (int i = 0; i < LoopCcw.Length; i++)
            {
                var go = new GameObject("WP_" + i.ToString("00"));
                go.transform.SetParent(g, false);
                go.transform.localPosition = LoopCcw[i];
                Vector3 next = LoopCcw[(i + 1) % LoopCcw.Length];
                Vector3 dir = next - LoopCcw[i];
                if (dir.sqrMagnitude > 0.0001f)
                    go.transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        void WarnIfLoopDrifted()
        {
            float len = 0f;
            for (int i = 0; i < LoopCcw.Length; i++)
                len += Vector3.Distance(LoopCcw[i], LoopCcw[(i + 1) % LoopCcw.Length]);
            if (Mathf.Abs(len - LoopLengthM) > 0.05f)
                Debug.LogWarning("MegaPark P1 CCW length drifted from 472 m: " + len.ToString("0.00"));
        }
    }
}
