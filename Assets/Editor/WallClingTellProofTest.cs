using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod WallClingTellProofTest.Run
/// </summary>
public static class WallClingTellProofTest
{
    public static void Run()
    {
        WallClingTellReport report = WallClingTellProof.Run();
        Debug.Log("[WallCling]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[WallCling] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[WallCling] OK");
        EditorApplication.Exit(0);
    }
}
