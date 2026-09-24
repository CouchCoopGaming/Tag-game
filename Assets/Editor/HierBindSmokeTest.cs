using System.IO;
using Tag.Art;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod HierBindSmokeTest.Run
/// Loads each Dummy_Mannequin_*_Hier_Hi.fbx and asserts DummyLocomotor.HasBindableBones.
/// </summary>
public static class HierBindSmokeTest
{
    const string HiPolyDir = "Assets/Art/Characters/HiPoly";

    public static void Run()
    {
        string[] colors = { "Blue", "Mint", "Orange", "Lavender", "Tan", "Red" };
        int fail = 0;
        int ok = 0;
        foreach (var color in colors)
        {
            string path = HiPolyDir + "/Dummy_Mannequin_" + color + "_Hier_Hi.fbx";
            if (!File.Exists(path))
            {
                Debug.LogError("[HierBindSmoke] MISSING " + path);
                fail++;
                continue;
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
            bool bindable = DummyLocomotor.HasBindableBones(inst.transform);
            if (!bindable)
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
}
