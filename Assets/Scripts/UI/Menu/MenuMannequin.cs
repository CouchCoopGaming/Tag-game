using Tag.Art;
using Tag.Profiles;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Preview body. Prefers the Hier HiPoly mesh for the picked color.
    /// The editor loads the FBX by path. A player build uses the catalog
    /// once the editor bake has filled it, then the primitive mannequin.
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
                    Dress(inst, body, accent);
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
            HierMannequinCatalog cat = Resources.Load<HierMannequinCatalog>("Characters/HierMannequinCatalog");
            if (cat != null)
            {
                GameObject slotted = cat.ForRunner(key);
                if (slotted != null) return slotted;
            }
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/Characters/HiPoly/Dummy_Mannequin_" + key + "_Hier_Hi.fbx");
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
                    Color color = Shell(matName + " " + rend.gameObject.name, primary, secondary);
                    next[i] = DummyPrimitiveFactory.MakeMat(color, smooth, metal);
                }
                rend.sharedMaterials = next;
            }
        }

        static bool Has(string name, string token)
        {
            return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Color Shell(string name, Color primary, Color secondary)
        {
            if (Has(name, "Joint") || Has(name, "Rubber") || Has(name, "Bellow")
                || Has(name, "Wear") || Has(name, "Sensor") || Has(name, "Eye")
                || Has(name, "Lip") || Has(name, "Metal"))
                return new Color(0.10f, 0.10f, 0.12f, 1f);
            if (Has(name, "Accent") || Has(name, "Panel") || Has(name, "Cal"))
                return secondary;
            return primary;
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
