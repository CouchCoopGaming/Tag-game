using Tag.Art;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Preview body. Prefers the Hier HiPoly mesh for the picked color.
    /// The editor loads the FBX by path. A player build uses the catalog
    /// once the editor bake has filled it, then the primitive mannequin.
    /// </summary>
    public static partial class MenuMannequin
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
                    next[i].name = RoleTag(matName);
                }
                rend.sharedMaterials = next;
            }
        }

        /// <summary>
        /// RESULTS only. The body matches the seat slot color. Accent, joints,
        /// and eyes stay as Dress left them. Character select does not call this.
        /// </summary>
        public static void PaintSlot(GameObject inst, Color slot)
        {
            if (inst == null) return;
            slot.a = 1f;
            Renderer[] rends = inst.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rends.Length; r++)
            {
                Renderer rend = rends[r];
                if (rend == null) continue;
                string goName = rend.gameObject.name;
                if (goName.IndexOf("MenuHat", System.StringComparison.Ordinal) >= 0) continue;
                Material[] shared = rend.sharedMaterials;
                int n = shared != null ? shared.Length : 0;
                if (n < 1)
                {
                    if (BodyPart(goName))
                        rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(slot, 0.45f, 0f);
                    continue;
                }
                var next = new Material[n];
                for (int i = 0; i < n; i++)
                {
                    Material src = shared[i];
                    string matName = src != null ? src.name : "";
                    bool tagged = matName.IndexOf("MenuRole_", System.StringComparison.Ordinal) >= 0;
                    bool body = matName.IndexOf("MenuRole_Base", System.StringComparison.Ordinal) >= 0
                        || (!tagged && BodyPart(goName));
                    if (body)
                        next[i] = DummyPrimitiveFactory.MakeMat(slot, 0.45f, 0f);
                    else
                        next[i] = src;
                }
                rend.sharedMaterials = next;
            }
        }

        static string RoleTag(string matName)
        {
            if (IsAccent(matName)) return "MenuRole_Accent";
            if (IsTrim(matName)) return "MenuRole_Trim";
            return "MenuRole_Base";
        }

        static bool BodyPart(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (IsAccent(name) || IsTrim(name)) return false;
            return true;
        }

        static bool IsTrim(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return name.IndexOf("Joint", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Sensor", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Rubber", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Bellow", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Metal", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Neck", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Shoulder", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Elbow", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Knee", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Hip", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Hand", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Foot", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
