#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Tag.Art;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Fills the Hier catalog, copies arena stills, and renders a portrait
    /// from each HiPoly mesh. Runs when the editor loads if those files are
    /// missing or older than the source, and again before a player build.
    /// The Tag menu item still bakes on demand. Does not change feel numbers.
    /// </summary>
    public static class MenuArtBake
    {
        const string CatalogPath = "Assets/Resources/Characters/HierMannequinCatalog.asset";
        const string HiPoly = "Assets/Art/Characters/HiPoly/";

        static readonly string[] Colors = { "Blue", "Mint", "Orange", "Lavender", "Tan", "Red" };
        static bool _busy;
        static int _defer;

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.delayCall += RunIfStale;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                    RunIfStale();
            };
        }

        [MenuItem("Tag/Menu/Bake Hier Catalog And Arena Thumbs")]
        public static void Bake()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                BakeCatalog();
                BakeThumbs();
                BakePortraits();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[MenuArtBake] Hier catalog, arena thumbs, and portraits refreshed.");
            }
            finally
            {
                _busy = false;
            }
        }

        public static void BakeIfStale()
        {
            if (!NeedsBake()) return;
            Bake();
        }

        static void RunIfStale()
        {
            if (_busy || EditorApplication.isPlaying) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (_defer++ > 8) return;
                EditorApplication.delayCall += RunIfStale;
                return;
            }
            _defer = 0;
            BakeIfStale();
        }

        static bool NeedsBake()
        {
            if (CatalogStale()) return true;
            if (ThumbStale("Mega")) return true;
            if (ThumbStale("Pocket")) return true;
            if (ThumbStale("Stack")) return true;
            for (int i = 0; i < Colors.Length; i++)
                if (PortraitStale(Colors[i])) return true;
            return false;
        }

        static bool CatalogStale()
        {
            var cat = AssetDatabase.LoadAssetAtPath<HierMannequinCatalog>(CatalogPath);
            if (cat == null) return false;
            return cat.blue == null || cat.mint == null || cat.orange == null
                || cat.lavender == null || cat.tan == null || cat.red == null;
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
            return AssetDatabase.LoadAssetAtPath<GameObject>(HiPoly + "Dummy_Mannequin_" + color + "_Hier_Hi.fbx");
        }

        static bool ThumbStale(string key)
        {
            // Mega's row plate is the golden-hour yard render. The block overview
            // is the audit still, and copying it would put the treeline back on the tile.
            if (key == "Mega") return false;
            string src = "Docs/ArenaStills/" + Source(key);
            if (!File.Exists(src)) return false;
            return Older("Assets/UI/ArenaThumbs/" + key + ".png", src)
                || Older("Assets/Resources/UI/ArenaThumbs/" + key + ".png", src);
        }

        static void BakeThumbs()
        {
            Copy("Mega");
            Copy("Pocket");
            Copy("Stack");
        }

        static void Copy(string key)
        {
            if (key == "Mega") return;
            string src = "Docs/ArenaStills/" + Source(key);
            if (!File.Exists(src)) return;
            if (!ThumbStale(key)) return;
            WriteFile(src, "Assets/UI/ArenaThumbs/" + key + ".png");
            WriteFile(src, "Assets/Resources/UI/ArenaThumbs/" + key + ".png");
        }

        static string Source(string key)
        {
            if (key == "Pocket") return "PocketPark_Overview.png";
            if (key == "Stack") return "StackYard_Overview.png";
            return "MegaPark_Overview.png";
        }

        static bool PortraitStale(string color)
        {
            string src = HiPoly + "Dummy_Mannequin_" + color + "_Hier_Hi.fbx";
            if (!File.Exists(src)) return false;
            return Older("Assets/UI/Portraits/" + color + ".png", src)
                || Older("Assets/Resources/UI/Portraits/" + color + ".png", src);
        }

        static void BakePortraits()
        {
            for (int i = 0; i < Colors.Length; i++)
            {
                string color = Colors[i];
                if (!PortraitStale(color)) continue;
                GameObject src = Load(color);
                if (src == null) continue;
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
                if (inst == null) continue;
                try
                {
                    byte[] png = Raster(inst, Swatch(color));
                    if (png == null || png.Length < 8) continue;
                    WriteBytes("Assets/UI/Portraits/" + color + ".png", png);
                    WriteBytes("Assets/Resources/UI/Portraits/" + color + ".png", png);
                }
                finally
                {
                    Object.DestroyImmediate(inst);
                }
            }
        }

        static Color Swatch(string color)
        {
            if (color == "Blue") return new Color(0.42f, 0.68f, 0.92f, 1f);
            if (color == "Mint") return new Color(0.42f, 0.82f, 0.70f, 1f);
            if (color == "Orange") return new Color(0.94f, 0.42f, 0.14f, 1f);
            if (color == "Lavender") return new Color(0.70f, 0.58f, 0.88f, 1f);
            if (color == "Red") return new Color(0.88f, 0.22f, 0.24f, 1f);
            return new Color(0.90f, 0.76f, 0.52f, 1f);
        }

        struct Tri
        {
            public Vector3 A, B, C;
            public float Shade;
        }

        static byte[] Raster(GameObject root, Color body)
        {
            var tris = new List<Tri>(2048);
            Collect(root.transform, body, tris);
            if (tris.Count == 0) return null;
            Bounds box = new Bounds(tris[0].A, Vector3.zero);
            for (int i = 0; i < tris.Count; i++)
            {
                box.Encapsulate(tris[i].A);
                box.Encapsulate(tris[i].B);
                box.Encapsulate(tris[i].C);
            }
            const int w = 256;
            const int h = 320;
            float span = Mathf.Max(box.size.x, box.size.y);
            if (span < 0.0001f) return null;
            float scale = (h * 0.86f) / span;
            float ox = w * 0.5f - box.center.x * scale;
            float oy = h * 0.46f - box.center.y * scale;
            var color = new Color32[w * h];
            var depth = new float[w * h];
            for (int i = 0; i < depth.Length; i++) depth[i] = float.NegativeInfinity;
            Vector3 light = new Vector3(-0.25f, 0.55f, 0.8f).normalized;
            for (int t = 0; t < tris.Count; t++)
            {
                Tri tri = tris[t];
                Vector3 n = Vector3.Cross(tri.B - tri.A, tri.C - tri.A);
                if (n.sqrMagnitude < 1e-8f) continue;
                n.Normalize();
                float ndl = Mathf.Abs(Vector3.Dot(n, light));
                float shade = 0.32f + 0.68f * ndl;
                RasterTri(color, depth, w, h, tri, scale, ox, oy, shade, body);
            }
            int filled = 0;
            for (int i = 0; i < depth.Length; i++)
                if (depth[i] > float.NegativeInfinity) filled++;
            if (filled < 32) return null;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(color);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }

        static void RasterTri(Color32[] color, float[] depth, int w, int h, Tri tri, float scale, float ox, float oy, float shade, Color body)
        {
            float ax = tri.A.x * scale + ox;
            float ay = tri.A.y * scale + oy;
            float az = tri.A.z;
            float bx = tri.B.x * scale + ox;
            float by = tri.B.y * scale + oy;
            float bz = tri.B.z;
            float cx = tri.C.x * scale + ox;
            float cy = tri.C.y * scale + oy;
            float cz = tri.C.z;
            int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx))), 0, w - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx))), 0, w - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy))), 0, h - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy))), 0, h - 1);
            float area = Area(ax, ay, bx, by, cx, cy);
            if (Mathf.Abs(area) < 0.01f) return;
            Color lit = body * shade;
            var pixel = new Color32(
                (byte)(Mathf.Clamp01(lit.r) * 255f),
                (byte)(Mathf.Clamp01(lit.g) * 255f),
                (byte)(Mathf.Clamp01(lit.b) * 255f),
                255);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Area(bx, by, cx, cy, x, y) / area;
                    float w1 = Area(cx, cy, ax, ay, x, y) / area;
                    float w2 = Area(ax, ay, bx, by, x, y) / area;
                    if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                    float z = w0 * az + w1 * bz + w2 * cz;
                    int i = y * w + x;
                    if (z <= depth[i]) continue;
                    depth[i] = z;
                    color[i] = pixel;
                }
            }
        }

        static float Area(float ax, float ay, float bx, float by, float cx, float cy)
        {
            return (bx - ax) * (cy - ay) - (cx - ax) * (by - ay);
        }

        static void Collect(Transform t, Color body, List<Tri> tris)
        {
            Mesh mesh = null;
            var filter = t.GetComponent<MeshFilter>();
            if (filter != null) mesh = filter.sharedMesh;
            var skin = t.GetComponent<SkinnedMeshRenderer>();
            if (skin != null) mesh = skin.sharedMesh;
            if (mesh != null) AddMesh(mesh, t.localToWorldMatrix, tris);
            for (int i = 0; i < t.childCount; i++)
                Collect(t.GetChild(i), body, tris);
        }

        static void AddMesh(Mesh mesh, Matrix4x4 matrix, List<Tri> tris)
        {
            Vector3[] verts = mesh.vertices;
            int[] idx = mesh.triangles;
            if (verts == null || idx == null) return;
            for (int i = 0; i + 2 < idx.Length; i += 3)
            {
                int ia = idx[i];
                int ib = idx[i + 1];
                int ic = idx[i + 2];
                if (ia < 0 || ib < 0 || ic < 0 || ia >= verts.Length || ib >= verts.Length || ic >= verts.Length)
                    continue;
                Tri tri = new Tri();
                tri.A = matrix.MultiplyPoint3x4(verts[ia]);
                tri.B = matrix.MultiplyPoint3x4(verts[ib]);
                tri.C = matrix.MultiplyPoint3x4(verts[ic]);
                tris.Add(tri);
            }
        }

        static bool Older(string dest, string src)
        {
            if (!File.Exists(dest)) return true;
            return File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dest);
        }

        static void WriteFile(string src, string dest)
        {
            Ensure(dest);
            File.Copy(src, dest, true);
        }

        static void WriteBytes(string dest, byte[] bytes)
        {
            Ensure(dest);
            File.WriteAllBytes(dest, bytes);
        }

        static void Ensure(string dest)
        {
            string dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }

    public sealed class MenuArtBakeBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            MenuArtBake.BakeIfStale();
        }
    }
}
#endif
