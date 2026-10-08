#if UNITY_EDITOR
using System.IO;
using Tag.Art;
using UnityEditor;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Fills the Hier catalog from the HiPoly FBX roots and copies arena
    /// overview stills into the menu thumb folders. Run from the Tag menu
    /// after the project opens in Unity. Does not change feel numbers.
    /// </summary>
    public static class MenuArtBake
    {
        const string CatalogPath = "Assets/Resources/Characters/HierMannequinCatalog.asset";
        const string HiPoly = "Assets/Art/Characters/HiPoly/";

        [MenuItem("Tag/Menu/Bake Hier Catalog And Arena Thumbs")]
        public static void Bake()
        {
            BakeCatalog();
            BakeThumbs();
            AssetDatabase.SaveAssets();
            Debug.Log("[MenuArtBake] Hier catalog and arena thumbs refreshed.");
        }

        static void BakeCatalog()
        {
            var cat = AssetDatabase.LoadAssetAtPath<HierMannequinCatalog>(CatalogPath);
            if (cat == null)
            {
                Debug.LogWarning("[MenuArtBake] Missing " + CatalogPath);
                return;
            }
            cat.blue = Load("Blue");
            cat.mint = Load("Mint");
            cat.orange = Load("Orange");
            cat.lavender = Load("Lavender");
            cat.tan = Load("Tan");
            cat.red = Load("Red");
            EditorUtility.SetDirty(cat);
        }

        static GameObject Load(string color)
        {
            string path = HiPoly + "Dummy_Mannequin_" + color + "_Hier_Hi.fbx";
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static void BakeThumbs()
        {
            Copy("Mega");
            Copy("Pocket");
            Copy("Stack");
            AssetDatabase.Refresh();
        }

        static void Copy(string key)
        {
            string src = "Docs/ArenaStills/" + Source(key);
            if (!File.Exists(src)) return;
            Write(src, "Assets/UI/ArenaThumbs/" + key + ".png");
            Write(src, "Assets/Resources/UI/ArenaThumbs/" + key + ".png");
        }

        static string Source(string key)
        {
            if (key == "Pocket") return "PocketPark_Overview.png";
            if (key == "Stack") return "StackYard_Overview.png";
            return "MegaPark_Overview.png";
        }

        static void Write(string src, string dest)
        {
            string dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.Copy(src, dest, true);
        }
    }
}
#endif
