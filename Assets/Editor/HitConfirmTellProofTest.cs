using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod HitConfirmTellProofTest.Run
/// </summary>
public static class HitConfirmTellProofTest
{
    public static void Run()
    {
        HitConfirmTellReport report = HitConfirmTellProof.Run();
        Debug.Log("[HitConfirm]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[HitConfirm] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[HitConfirm] OK");
        EditorApplication.Exit(0);
    }
}
