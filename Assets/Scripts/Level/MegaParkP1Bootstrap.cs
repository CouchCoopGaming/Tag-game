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

        Transform _p1;
        Material _mulch, _grass, _sand, _rubber, _blue, _yellow, _steel, _concrete, _cedar, _bark, _rim, _field;

        void Awake()
        {
            Build();
        }

        [ContextMenu("Rebuild Mega Park P1")]
        public void Build()
        {
            EnsureMaterials();
            EnsureRoot();
            BuildSolids();
            BuildRamps();
            BuildPaint();
            BuildSpawns();
            BuildLoopMarkers();
            BuildLabels();

            MegaParkP1Layout.Audit audit = MegaParkP1Layout.Run();
            if (audit.Ok)
                Debug.Log(audit.Line);
            else
                Debug.LogError(audit.Line + " :: " + audit.Failure);
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
            _mulch = Make(new Color(0x5C / 255f, 0x3A / 255f, 0x2E / 255f), "MEGA_Mulch");
            _grass = Make(new Color(0x3F / 255f, 0x7A / 255f, 0x4A / 255f), "MEGA_Grass");
            _sand = Make(new Color(0xE6 / 255f, 0xD2 / 255f, 0xA2 / 255f), "MEGA_Sand");
            _rubber = Make(new Color(0x2A / 255f, 0x2A / 255f, 0x2E / 255f), "MEGA_Rubber");
            _blue = Make(new Color(0x3D / 255f, 0x7E / 255f, 0xFF / 255f), "MEGA_Cling");
            _yellow = Make(new Color(0xF5 / 255f, 0xD5 / 255f, 0x47 / 255f), "MEGA_Slide");
            _steel = Make(new Color(0xB8 / 255f, 0xC0 / 255f, 0xC8 / 255f), "MEGA_Steel");
            _concrete = Make(new Color(0xC5 / 255f, 0xCB / 255f, 0xD1 / 255f), "MEGA_Concrete");
            _cedar = Make(new Color(0x8A / 255f, 0x5A / 255f, 0x3C / 255f), "MEGA_Cedar");
            _bark = Make(new Color(0x3E / 255f, 0x26 / 255f, 0x1C / 255f), "MEGA_Bark");
            _rim = Make(new Color(0x6B / 255f, 0x46 / 255f, 0x36 / 255f), "MEGA_Rim");
            _field = Make(new Color(0x3C / 255f, 0x9A / 255f, 0x58 / 255f), "MEGA_Field");
        }

        static Material Make(Color c, string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
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
                default: return _mulch;
            }
        }

        Transform Group(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_p1, false);
            return go.transform;
        }

        void BuildSolids()
        {
            MegaParkP1Layout.Solid[] solids = MegaParkP1Layout.BuildSolids();
            Transform g = Group("Solids");
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = s.Name;
                go.transform.SetParent(g, false);
                go.transform.localPosition = new Vector3(s.X, s.Y, s.Z);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = new Vector3(s.Sx, s.Sy, s.Sz);
                MeshRenderer r = go.GetComponent<MeshRenderer>();
                if (r != null)
                    r.sharedMaterial = Pick(s.Mat);
            }
        }

        void BuildRamps()
        {
            MegaParkP1Layout.Ramp[] ramps = MegaParkP1Layout.BuildRamps();
            Transform g = Group("Ramps");
            for (int i = 0; i < ramps.Length; i++)
                BuildRamp(g, ramps[i]);
        }

        void BuildRamp(Transform parent, MegaParkP1Layout.Ramp r)
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
            MeshRenderer rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
                rend.sharedMaterial = Pick(r.Mat);
        }

        void BuildPaint()
        {
            Transform g = Group("Paint");
            Paint(g, "Z1_SoftPlay", 2f, 38f, 2f, 36f, _cedar, 0.025f);
            Paint(g, "Z2_ClingFooting", 2f, 10f, 38f, 78f, _rubber, 0.025f);
            Paint(g, "Z3_Merry", 22f, 46f, 34f, 60f, _cedar, 0.025f);
            Paint(g, "Z4_SlideMountain", 22f, 56f, 72f, 98f, _rim, 0.025f);
            Paint(g, "Z5_SwingGrove", 58f, 100f, 78f, 98f, _cedar, 0.025f);
            Paint(g, "Z6_Army", 118f, 158f, 10f, 46f, _bark, 0.025f);
            Paint(g, "Z6_Knight", 118f, 158f, 54f, 90f, _bark, 0.025f);
            Paint(g, "Z7_Field", 78f, 114f, 28f, 68f, _field, 0.04f);
            Paint(g, "Z7_MouthSouth", 64f, 78f, 28f, 34f, _field, 0.04f);
            Paint(g, "Z7_MouthNorth", 64f, 78f, 66f, 68f, _field, 0.04f);
            Paint(g, "Z10_Hopscotch", 118f, 156f, 2f, 22f, _concrete, 0.025f);
            Paint(g, "Spine_South", 38f, 118f, 12f, 20f, _concrete, 0.06f);
            Paint(g, "Spine_North", 14f, 130f, 83f, 89f, _concrete, 0.06f);
            Paint(g, "Spine_West", 10f, 14f, 2f, 98f, _concrete, 0.06f);
            Paint(g, "Spine_East", 130f, 138f, 10f, 90f, _concrete, 0.06f);
        }

        void Paint(Transform parent, string name, float x0, float x1, float z0, float z1, Material mat, float yTop)
        {
            const float t = 0.02f;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3((x0 + x1) * 0.5f, yTop - t * 0.5f, (z0 + z1) * 0.5f);
            go.transform.localScale = new Vector3(x1 - x0, t, z1 - z0);
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            if (r != null)
                r.sharedMaterial = mat;
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                DestroyImmediate(col);
        }

        void BuildSpawns()
        {
            Transform g = Group("Spawns");
            MegaParkP1Layout.SpawnPad[] pads = MegaParkP1Layout.Spawns;
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
                MeshRenderer r = disc.GetComponent<MeshRenderer>();
                if (r != null)
                    r.sharedMaterial = _rubber;
                Collider col = disc.GetComponent<Collider>();
                if (col != null)
                    DestroyImmediate(col);

                GameObject wedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wedge.name = "Facing";
                wedge.transform.SetParent(go.transform, false);
                wedge.transform.localPosition = new Vector3(0f, 0.08f, 1.15f);
                wedge.transform.localScale = new Vector3(0.4f, 0.05f, 0.7f);
                MeshRenderer wr = wedge.GetComponent<MeshRenderer>();
                if (wr != null)
                    wr.sharedMaterial = _concrete;
                Collider wc = wedge.GetComponent<Collider>();
                if (wc != null)
                    DestroyImmediate(wc);
            }
        }

        void BuildLoopMarkers()
        {
            Transform g = Group("TrailTag");
            MegaParkP1Layout.Pt[] loop = MegaParkP1Layout.LoopCcw;
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
    }
}
