using Tag.Art;
using Tag.Profiles;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Preview body. Every look uses the runner mesh the match ships
    /// (Tan Hier when the catalog slot is empty) and tints it. The editor
    /// loads that FBX by path. A player build uses the catalog once the
    /// editor bake has filled it, then the primitive mannequin.
    /// </summary>
    public static class MenuMannequin
    {
        public static GameObject Spawn(Transform parent, string bodyKey, string accentKey, bool hat)
        {
            string body = Normalize(bodyKey);
            string accent = Normalize(string.IsNullOrEmpty(accentKey) ? body : accentKey);
            GameObject src = FindPrefab(body);
            GameObject inst = null;
            if (src != null)
            {
                inst = Object.Instantiate(src, parent, false);
                inst.name = "HierPreview";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                StripPhysics(inst);
                if (!DummyLocomotor.HasBindableBones(inst.transform))
                {
                    Object.DestroyImmediate(inst);
                    inst = null;
                }
                else
                {
                    Dress(inst, body, accent);
                    // The newer Hier files face Unity -Z. The card camera sits on +Z.
                    // A parent holds the turn so idle and cheer can still replace the
                    // root rotation. The older armature already faces the camera.
                    if (HasNamed(inst.transform, "Panel_Chest"))
                        Face(inst, 180f);
                }
            }
            if (inst == null)
                inst = DummyPrimitiveFactory.Build(parent, false, body, accent);
            if (hat)
                AddHat(inst.transform);
            MenuIdle idle = inst.GetComponent<MenuIdle>();
            if (idle == null) idle = inst.AddComponent<MenuIdle>();
            idle.Capture(inst.name.StartsWith("DummyVisual"));
            return inst;
        }

        public static string Normalize(string key)
        {
            if (string.IsNullOrEmpty(key)) return "Tan";
            for (int i = 0; i < LocalProfiles.HierNames.Length; i++)
            {
                if (LocalProfiles.HierNames[i] == key) return key;
            }
            return "Tan";
        }

        public static string NameOf(int index)
        {
            int n = LocalProfiles.HierNames.Length;
            if (n < 1) return "Tan";
            if (index < 0) index = 0;
            if (index >= n) index = n - 1;
            return LocalProfiles.HierNames[index];
        }

        static GameObject FindPrefab(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "Tan";
            // One body for every look. The match tints this same runner.
            // key is the colour Dress paints, not a second mesh.
            HierMannequinCatalog cat = Resources.Load<HierMannequinCatalog>("Characters/HierMannequinCatalog");
            if (cat != null)
            {
                GameObject runner = cat.Runner;
                if (runner != null) return runner;
                GameObject slotted = cat.ForRunner("Tan");
                if (slotted != null) return slotted;
            }
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Characters/HiPoly/Dummy_Mannequin_Tan_Hier_Hi.fbx");
#else
            return null;
#endif
        }

        static void StripPhysics(GameObject inst)
        {
            Collider[] cols = inst.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                Object.DestroyImmediate(cols[i]);
            Rigidbody[] bodies = inst.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
                Object.DestroyImmediate(bodies[i]);
        }

        static void Dress(GameObject inst, string body, string accent)
        {
            // The FBX albedo imports dark and metallic, so the look disappears.
            // Primary is the body swatch, secondary is the accent swatch. Those
            // are the same keys the runner foam uses. Matte: metallic 0,
            // smoothness 0.40 (roughness about 0.6).
            Color primary = Swatch(body);
            Color secondary = Swatch(accent);
            const float smooth = 0.40f;
            const float metal = 0f;
            Renderer[] rends = inst.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rends.Length; r++)
            {
                Renderer rend = rends[r];
                if (rend == null) continue;
                Material[] shared = rend.sharedMaterials;
                int n = shared != null && shared.Length > 0 ? shared.Length : 1;
                var next = new Material[n];
                for (int i = 0; i < n; i++)
                {
                    Material src = shared != null && i < shared.Length ? shared[i] : null;
                    string matName = src != null ? src.name : "";
                    // One slot at a time. Orange and Tan join Base, Joint, and Wear
                    // onto the chest mesh. A joint slot must not paint the shell.
                    Color color = Shell(matName, rend.gameObject.name, primary, secondary);
                    next[i] = DummyPrimitiveFactory.MakeMat(color, smooth, metal);
                }
                rend.sharedMaterials = next;
            }
        }

        static bool Has(string name, string token)
        {
            return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Color Shell(string matName, string objName, Color primary, Color secondary)
        {
            if (!string.IsNullOrEmpty(matName))
                return Paint(matName, primary, secondary);
            return Paint(objName, primary, secondary);
        }

        static Color Paint(string name, Color primary, Color secondary)
        {
            if (Has(name, "Joint") || Has(name, "Rubber") || Has(name, "Bellow")
                || Has(name, "Wear") || Has(name, "Sensor") || Has(name, "Eye")
                || Has(name, "Lip") || Has(name, "Metal"))
                return new Color(0.10f, 0.10f, 0.12f, 1f);
            if (Has(name, "Accent") || Has(name, "Panel") || Has(name, "Cal"))
                return secondary;
            return primary;
        }

        static bool HasNamed(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return true;
            }
            return false;
        }

        static void Face(GameObject inst, float yaw)
        {
            var pivot = new GameObject("FacePivot");
            Transform parent = inst.transform.parent;
            pivot.transform.SetParent(parent, false);
            pivot.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            inst.transform.SetParent(pivot.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
        }

        public static Color Swatch(string key)
        {
            switch (Normalize(key))
            {
                case "Blue": return new Color(0.42f, 0.68f, 0.92f, 1f);
                case "Mint": return new Color(0.42f, 0.82f, 0.70f, 1f);
                case "Orange": return new Color(0.94f, 0.42f, 0.14f, 1f);
                case "Lavender": return new Color(0.70f, 0.58f, 0.88f, 1f);
                case "Red": return new Color(0.88f, 0.22f, 0.24f, 1f);
                default: return new Color(0.90f, 0.76f, 0.52f, 1f);
            }
        }

        /// <summary>The costume swatch at full strength. The light step of that hue.</summary>
        public static Color LightStep(string key)
        {
            return Swatch(key);
        }

        /// <summary>
        /// Seat mark for P1–P4. 0 circle, 1 triangle, 2 square, 3 diamond.
        /// Arena chips, results tags, and the name plate read this. No second shape table.
        /// </summary>
        public static int Shape(int seat)
        {
            if (seat < 0) return 0;
            if (seat > 3) return 3;
            return seat;
        }

        /// <summary>
        /// Same hue as <see cref="Swatch"/>, one step darker. The colour-blind
        /// scheme pairs this ink with the light step. No second colour table.
        /// </summary>
        public static Color DarkStep(string key)
        {
            Color plate = Swatch(key);
            Color.RGBToHSV(plate, out float h, out float s, out float v);
            if (s < 0.62f) s = 0.62f;
            float inkV = v * 0.28f;
            if (inkV > 0.32f) inkV = 0.32f;
            if (inkV < 0.12f) inkV = 0.12f;
            Color ink = Color.HSVToRGB(h, s, inkV);
            ink.a = 1f;
            return ink;
        }

        static void AddHat(Transform root)
        {
            var hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hat.name = "MenuHat";
            hat.transform.SetParent(root, false);
            hat.transform.localPosition = new Vector3(0f, 1.72f, 0f);
            hat.transform.localScale = new Vector3(0.28f, 0.16f, 0.28f);
            Collider col = hat.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var rend = hat.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(MenuTheme.Gold, 0.4f, 0.1f);
        }
    }
}
