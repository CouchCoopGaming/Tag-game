using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod OpponentLungeTellProofTest.Run
/// </summary>
public static class OpponentLungeTellProofTest
{
    public static void Run()
    {
        OpponentLungeTellReport report = OpponentLungeTellProof.Run();
        Debug.Log("[OpponentLunge]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[OpponentLunge] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[OpponentLunge] OK");
        EditorApplication.Exit(0);
    }
}
