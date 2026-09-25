using System.IO;
using Tag.Art;
using Tag.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod TubeDeckSmokeTest.Run -logFile _tubedeck_smoke.log
/// Opens Play, Rebuild + Place, asserts one TubeDeck on soft-play, astro, and army.
/// Mesh local Y runs from the mulch (~0) to the shell crown (~2.48). Mouth center is 1.91.
/// </summary>
public static class TubeDeckSmokeTest
{
    const string PlayScene = "Assets/Scenes/Play.unity";
    const string Stem = "PGK_Slide_TubeDeck_2m";

    public static void Run()
    {
        int fail = 0;
        if (!File.Exists(PlayScene))
        {
            Debug.LogError("[TubeDeckSmoke] MISSING " + PlayScene);
            EditorApplication.Exit(1);
            return;
        }

        EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
        var boot = Object.FindFirstObjectByType<CutArenaBootstrap>();
        if (boot == null)
        {
            Debug.LogError("[TubeDeckSmoke] No CutArenaBootstrap in Play");
            EditorApplication.Exit(1);
            return;
        }

        boot.Build();
        var placer = boot.GetComponent<PgkLandmarkPlacer>();
        if (placer == null)
            placer = boot.gameObject.AddComponent<PgkLandmarkPlacer>();
        placer.Place();

        var tubes = new System.Collections.Generic.List<Transform>();
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (tr != null && tr.name == Stem)
                tubes.Add(tr);
        }

        Debug.Log("[TubeDeckSmoke] TubeDeck count=" + tubes.Count);
        if (tubes.Count != 3)
        {
            Debug.LogError("[TubeDeckSmoke] FAIL expected 3 TubeDeck (soft-play+astro+army), got " + tubes.Count);
            fail++;
        }
        int soft = 0, astro = 0, army = 0, other = 0;
        foreach (var tube in tubes)
        {
            string path = PathOf(tube);
            if (path.Contains("Play_SoftPlay/")) soft++;
            else if (path.Contains("Play_AstroLoft/")) astro++;
            else if (path.Contains("Play_ArmyBunker/")) army++;
            else other++;
        }
        if (soft != 1 || astro != 1 || army != 1 || other != 0)
        {
            Debug.LogError("[TubeDeckSmoke] FAIL seats soft=" + soft + " astro=" + astro + " army=" + army + " other=" + other);
            fail++;
        }

        foreach (var tube in tubes)
        {
            var mfs = tube.GetComponentsInChildren<MeshFilter>(true);
            Bounds local = new Bounds();
            bool any = false;
            foreach (var mf in mfs)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var mb = mf.sharedMesh.bounds;
                // mesh bounds are in mesh local space; combine in tube root local via renderer transform
                var b = TransformBounds(mf.transform, mb, tube);
                if (!any) { local = b; any = true; }
                else local.Encapsulate(b);
            }

            if (!any)
            {
                Debug.LogError("[TubeDeckSmoke] FAIL no meshes on " + PathOf(tube));
                fail++;
                continue;
            }

            float minY = local.min.y;
            float maxY = local.max.y;
            float spanY = maxY - minY;
            float spanX = local.size.x;
            float spanZ = local.size.z;
            Debug.Log("[TubeDeckSmoke] " + PathOf(tube) + " pos=" + tube.position + " yaw=" + tube.eulerAngles.y.ToString("0") +
                      " localMinY=" + minY.ToString("0.000") + " localMaxY=" + maxY.ToString("0.000") +
                      " spanY=" + spanY.ToString("0.000") + " spanX=" + spanX.ToString("0.000") + " spanZ=" + spanZ.ToString("0.000"));

            // Pivot is mulch. Shell crown is ~2.48; the 1.91 figure is the mouth center, not max Y.
            // Run is ~5.05 along local X. A swapped axis fails spanX.
            bool feetOk = minY > -0.15f && minY < 0.20f;
            bool crownOk = maxY > 2.20f && maxY < 2.70f;
            bool runOk = spanX > 4.5f && spanX < 5.6f;
            if (!feetOk || !crownOk || !runOk)
            {
                // Axis/pivot mismatch is a map/art polish item; keep smoke red so tip does not go silent.
                Debug.LogError("[TubeDeckSmoke] FAIL seat/axis localMinY=" + minY.ToString("0.000") +
                               " localMaxY=" + maxY.ToString("0.000") + " spanX=" + spanX.ToString("0.000") +
                               " (want feet~0, shell crown~2.48, runX~5) on " + PathOf(tube));
                fail++;
            }
        }

        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (tr == null) continue;
            if (tr.name.StartsWith("Mega_SlideTube") || tr.name.StartsWith("PGK_Slide_Tube90") ||
                (tr.name.StartsWith("PGK_Slide_Tube") && !tr.name.StartsWith("PGK_Slide_TubeDeck")))
            {
                Debug.LogError("[TubeDeckSmoke] FAIL blocked tube spawned: " + tr.name + " @ " + PathOf(tr));
                fail++;
            }
        }

        Debug.Log("[TubeDeckSmoke] done fail=" + fail);
        EditorApplication.Exit(fail > 0 ? 1 : 0);
    }

    static Bounds TransformBounds(Transform src, Bounds meshLocal, Transform dstRoot)
    {
        var corners = new Vector3[8];
        var c = meshLocal.center;
        var e = meshLocal.extents;
        int i = 0;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            corners[i++] = src.TransformPoint(c + new Vector3(e.x * x, e.y * y, e.z * z));
        var b = new Bounds(dstRoot.InverseTransformPoint(corners[0]), Vector3.zero);
        for (int k = 1; k < 8; k++)
            b.Encapsulate(dstRoot.InverseTransformPoint(corners[k]));
        return b;
    }

    static string PathOf(Transform t)
    {
        string s = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            s = t.name + "/" + s;
        }
        return s;
    }
}
