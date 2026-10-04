using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod SlideScrapeTellProofTest.Run
/// </summary>
public static class SlideScrapeTellProofTest
{
    public static void Run()
    {
        SlideScrapeTellReport report = SlideScrapeTellProof.Run();
        Debug.Log("[SlideScrape]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[SlideScrape] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[SlideScrape] OK");
        EditorApplication.Exit(0);
    }
}
