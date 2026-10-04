using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod WallJumpPushTellProofTest.Run
/// </summary>
public static class WallJumpPushTellProofTest
{
    public static void Run()
    {
        WallJumpPushTellReport report = WallJumpPushTellProof.Run();
        Debug.Log("[WallJumpPush]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[WallJumpPush] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[WallJumpPush] OK");
        EditorApplication.Exit(0);
    }
}
