using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod JumpLandTellProofTest.Run
/// </summary>
public static class JumpLandTellProofTest
{
    public static void Run()
    {
        JumpLandTellReport report = JumpLandTellProof.Run();
        Debug.Log("[JumpLand]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[JumpLand] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[JumpLand] OK");
        EditorApplication.Exit(0);
    }
}
