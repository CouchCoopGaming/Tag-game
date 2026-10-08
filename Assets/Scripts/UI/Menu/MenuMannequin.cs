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
                    string matName = src != null ? src.name : rend.gameObject.name;
                    Color color = Authored(src);
                    if (!Usable(color)) color = RoleColor(matName, body, accent);
                    else if (IsAccent(matName)) color = Swatch(accent);
                    next[i] = DummyPrimitiveFactory.MakeMat(color, 0.45f, 0f);
                }
                rend.sharedMaterials = next;
            }
        }

        static bool IsAccent(string name)
        {
            return name.IndexOf("Accent", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Panel", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Color Authored(Material src)
        {
            if (src == null) return new Color(1f, 0f, 1f, 1f);
            if (src.HasProperty("_BaseColor")) return src.GetColor("_BaseColor");
            if (src.HasProperty("_Color")) return src.color;
            return new Color(1f, 0f, 1f, 1f);
        }

        static bool Usable(Color c)
        {
            if (c.a < 0.2f) return false;
            bool magenta = c.r > 0.8f && c.b > 0.8f && c.g < 0.35f;
            return !magenta;
        }

        static Color RoleColor(string name, string body, string accent)
        {
            if (name.IndexOf("Joint", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Rubber", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Bellow", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Color(0.08f, 0.08f, 0.09f, 1f);
            if (name.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Sensor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return new Color(0.04f, 0.04f, 0.05f, 1f);
            if (IsAccent(name)) return Swatch(accent);
            return Swatch(body);
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
