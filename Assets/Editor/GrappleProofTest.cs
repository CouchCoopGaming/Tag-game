using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod GrappleProofTest.Run
/// </summary>
public static class GrappleProofTest
{
    public static void Run()
    {
        GrappleReport report = GrappleProof.Run60();
        Debug.Log("[Grapple]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[Grapple] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[Grapple] OK");
        EditorApplication.Exit(0);
    }
}
