using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod GrappleMissTellProofTest.Run
/// </summary>
public static class GrappleMissTellProofTest
{
    public static void Run()
    {
        GrappleMissTellReport report = GrappleMissTellProof.Run();
        Debug.Log("[GrappleMiss]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[GrappleMiss] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[GrappleMiss] OK");
        EditorApplication.Exit(0);
    }
}
