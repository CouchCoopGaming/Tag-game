using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod StrafeJumpProofTest.Run
/// </summary>
public static class StrafeJumpProofTest
{
    public static void Run()
    {
        StrafeJumpReport report = StrafeJumpProof.Run60();
        Debug.Log("[StrafeJump]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[StrafeJump] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[StrafeJump] OK");
        EditorApplication.Exit(0);
    }
}
