using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod OpponentChaseTellProofTest.Run
/// </summary>
public static class OpponentChaseTellProofTest
{
    public static void Run()
    {
        OpponentChaseTellReport report = OpponentChaseTellProof.Run();
        Debug.Log("[OpponentChase]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[OpponentChase] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[OpponentChase] OK");
        EditorApplication.Exit(0);
    }
}
