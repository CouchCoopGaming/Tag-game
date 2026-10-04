using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod AirDashCooldownTellProofTest.Run
/// </summary>
public static class AirDashCooldownTellProofTest
{
    public static void Run()
    {
        AirDashCooldownTellReport report = AirDashCooldownTellProof.Run();
        Debug.Log("[AirDashCooldown]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[AirDashCooldown] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[AirDashCooldown] OK");
        EditorApplication.Exit(0);
    }
}
