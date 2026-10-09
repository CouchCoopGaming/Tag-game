#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Tag.Level;

/// <summary>
/// Station world capture: builds Mega Park in a fresh scene and writes 1920x1080
/// stills plus capture-audit.txt to -stationOut. Zone cameras aim at the real
/// bounds of each WorldZn prop group (falling back to the painted zone centres),
/// and the eye-height court shot stands 1.5 m inside the west CourtFence.
/// Run: Unity -batchmode -projectPath . -executeMethod TagStationWorldCapture.Run -stationOut OUT
/// </summary>
public static class TagStationWorldCapture
{
    struct Shot { public string name; public Vector3 eye, look; public float fov; }

    const float FenceInset = 1.5f;
    const float EyeHeight = 1.7f;

    public static void Run()
    {
        string outDir = null;
        var a = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-stationOut") outDir = a[i + 1];
        if (string.IsNullOrEmpty(outDir)) outDir = "StationOut";
        Directory.CreateDirectory(outDir);
        var audit = new StringBuilder();
        int code = 0;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var go = new GameObject("MegaPark");
            var boot = go.AddComponent<MegaParkP1Bootstrap>();
            boot.Build();
            audit.AppendLine("Built=" + boot.Built + " LayoutOk=" + boot.LayoutOk);
            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Cam").AddComponent<Camera>();
            cam.farClipPlane = 1000f;
            cam.nearClipPlane = 0.05f;

            // Court: fence bounds if present, else court pivot (88.6, 53.2) with a 15 m wide slab.
            Bounds court;
            string courtSrc = "fence";
            if (!TryBounds("CourtFence", out court))
            {
                courtSrc = TryBounds("Court", out court) ? "court" : "fallback";
                if (courtSrc == "fallback") court = new Bounds(new Vector3(88.6f, 0f, 53.2f), new Vector3(15f, 3f, 22f));
            }
            audit.AppendLine("court src=" + courtSrc + " min=" + court.min + " max=" + court.max);
            float cz = court.center.z;
            Vector3 courtEye = new Vector3(court.min.x + FenceInset, EyeHeight, cz);
            Vector3 courtLook = new Vector3(court.max.x, 1.5f, cz);

            Shot[] shots = {
                S("z7-court-eye", courtEye, courtLook, 60f),
                S("z7-court-high", court.center + new Vector3(-18f, 22f, -28f), court.center, 55f),
                Zone("z1-softplay", "WorldZ1", new Vector3(20f, 0f, 19f), new Vector3(22f, 14f, -26f), audit),
                Zone("z2-cling", "WorldZ2", new Vector3(6f, 2f, 58f), new Vector3(20f, 10f, -14f), audit),
                Zone("z3-merry", "WorldZ3", new Vector3(34f, 0f, 47f), new Vector3(0f, 12f, -28f), audit),
                Zone("z4-slide", "WorldZ4", new Vector3(39f, 3f, 85f), new Vector3(0f, 14f, -28f), audit),
                Zone("z5-swing", "WorldZ5", new Vector3(79f, 2f, 88f), new Vector3(0f, 12f, -26f), audit),
                // The WorldZ6 group is the harbor-yard dressing on the west edge
                // (x 119-128), so its bounds aimed past the forts. Frame the Army
                // (143, 28) and Knight (143, 70) forts themselves, from the west.
                Forts(audit),
                Zone("z8-bowl", "WorldZ8", new Vector3(60f, 0f, 50f), new Vector3(0f, 16f, -28f), audit),
                Zone("z9-bars", "WorldZ9", new Vector3(73f, 2f, 17.6f), new Vector3(0f, 14f, -26f), audit),
                Zone("z10-hops", "WorldZ10", new Vector3(137f, 0f, 12f), new Vector3(0f, 12f, -24f), audit),
                S("overview", new Vector3(80f, 90f, -40f), new Vector3(80f, 0f, 50f), 60f),
            };
            var rt = new RenderTexture(1920, 1080, 24);
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            foreach (var s in shots)
            {
                cam.transform.position = s.eye; cam.transform.LookAt(s.look); cam.fieldOfView = s.fov;
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                int mag = 0; var px = tex.GetPixels32();
                for (int i = 0; i < px.Length; i += 7) if (px[i].r > 240 && px[i].g < 20 && px[i].b > 240) mag++;
                string p = Path.Combine(outDir, s.name + "-1920x1080.png");
                File.WriteAllBytes(p, tex.EncodeToPNG());
                audit.AppendLine(s.name + " eye=" + s.eye + " look=" + s.look + " fov=" + s.fov + " magentaSamples=" + mag);
            }
            cam.targetTexture = null; RenderTexture.active = null;
        }
        catch (System.Exception e) { audit.AppendLine("EXCEPTION " + e); code = 1; }
        File.WriteAllText(Path.Combine(outDir, "capture-audit.txt"), audit.ToString());
        if (Application.isBatchMode) EditorApplication.Exit(code);
    }

    static Shot Forts(StringBuilder audit)
    {
        Vector3 look = new Vector3(142f, 2.5f, 49f);
        Vector3 eye = new Vector3(112f, 19f, 49f);
        audit.AppendLine("z6-forts target src=forts look=" + look);
        return S("z6-forts", eye, look, 62f);
    }

    static Shot Zone(string name, string group, Vector3 fallback, Vector3 offset, StringBuilder audit)
    {
        Vector3 look = fallback;
        string src = "fallback";
        if (TryBounds(group, out Bounds b))
        {
            look = new Vector3(b.center.x, Mathf.Min(b.center.y, 3f), b.center.z);
            src = group;
        }
        audit.AppendLine(name + " target src=" + src + " look=" + look);
        return S(name, look + offset, look, 55f);
    }

    static bool TryBounds(string objectName, out Bounds bounds)
    {
        bounds = default;
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == objectName) { go = t.gameObject; break; }
        }
        if (go == null) return false;
        bool any = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    static Shot S(string n, Vector3 e, Vector3 l, float f) { return new Shot { name = n, eye = e, look = l, fov = f }; }
}
#endif
