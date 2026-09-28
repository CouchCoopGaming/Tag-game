using System.IO;
using Tag.Art;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod HierBindSmokeTest.Run
/// Runner is Tan Hier, It is Orange Hier. Those two must satisfy DummyLocomotor.HierSpawnContract
/// (Hand under LowerArm, Mesh_Head under Head, shin under thigh, feet present).
/// Finger bones are not required. Other colors on this tip still need HasBindableBones.
/// </summary>
public static class HierBindSmokeTest
{
    const string HiPolyDir = "Assets/Art/Characters/HiPoly";
    const string TanGuid = "ad3f2fa97db94e72869d746ecdf8e87d";
    const string OrangeGuid = "b33974ad57284ef28a7564e3bdf00540";

    public static void Run()
    {
        string[] colors = { "Blue", "Mint", "Orange", "Lavender", "Tan", "Red" };
        int fail = 0;
        int ok = 0;

        string runnerPath = ArtMeshPaths.PreferCharacterFbx(false);
        string itPath = ArtMeshPaths.PreferCharacterFbx(true);
        fail += ExpectSpawn("Runner", runnerPath, "Dummy_Mannequin_Tan_Hier_Hi.fbx", TanGuid);
        fail += ExpectSpawn("It", itPath, "Dummy_Mannequin_Orange_Hier_Hi.fbx", OrangeGuid);

        foreach (var color in colors)
        {
            string path = HiPolyDir + "/Dummy_Mannequin_" + color + "_Hier_Hi.fbx";
            bool spawn = color == "Tan" || color == "Orange";
            if (!File.Exists(path))
            {
                Debug.LogError("[HierBindSmoke] MISSING " + path);
                fail++;
                continue;
            }
            if (spawn)
            {
                string guid = color == "Tan" ? TanGuid : OrangeGuid;
                if (!GuidMatches(path, guid))
                {
                    Debug.LogError("[HierBindSmoke] GUID mismatch " + path + " expected " + guid);
                    fail++;
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("[HierBindSmoke] LoadAsset failed " + path);
                fail++;
                continue;
            }
            var inst = Object.Instantiate(prefab);
            inst.name = "Smoke_" + color;
            if (spawn)
            {
                string report = DummyLocomotor.HierSpawnContractReport(inst.transform);
                if (!report.StartsWith("ok"))
                {
                    Debug.LogError("[HierBindSmoke] FAIL contract " + path + " " + report);
                    fail++;
                }
                else
                {
                    Debug.Log("[HierBindSmoke] OK " + color + " " + report);
                    ok++;
                }
            }
            else if (!DummyLocomotor.HasBindableBones(inst.transform))
            {
                Debug.LogError("[HierBindSmoke] FAIL HasBindableBones=false on " + path);
                fail++;
            }
            else
            {
                Debug.Log("[HierBindSmoke] OK " + color + " Hier binds");
                ok++;
            }
            Object.DestroyImmediate(inst);
        }

        Debug.Log($"[HierBindSmoke] done ok={ok} fail={fail}");
        EditorApplication.Exit(fail > 0 ? 1 : 0);
    }

    static int ExpectSpawn(string role, string path, string fileName, string guid)
    {
        if (string.IsNullOrEmpty(path) || !path.EndsWith(fileName) || !File.Exists(path))
        {
            Debug.LogError("[HierBindSmoke] " + role + " default is not " + fileName + ": " + path);
            return 1;
        }
        if (!GuidMatches(path, guid))
        {
            Debug.LogError("[HierBindSmoke] " + role + " GUID mismatch " + path + " expected " + guid);
            return 1;
        }
        Debug.Log("[HierBindSmoke] OK " + role + " -> " + path + " guid " + guid);
        return 0;
    }

    static bool GuidMatches(string assetPath, string guid)
    {
        string meta = assetPath + ".meta";
        if (!File.Exists(meta)) return false;
        foreach (var line in File.ReadLines(meta))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("guid:"))
                return trimmed == "guid: " + guid;
        }
        return false;
    }
}
