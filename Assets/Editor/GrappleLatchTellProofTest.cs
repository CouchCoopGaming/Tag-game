using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod GrappleLatchTellProofTest.Run
/// </summary>
public static class GrappleLatchTellProofTest
{
    public static void Run()
    {
        GrappleLatchTellReport report = GrappleLatchTellProof.Run();
        Debug.Log("[GrappleLatch]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[GrappleLatch] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[GrappleLatch] OK");
        EditorApplication.Exit(0);
    }
}
