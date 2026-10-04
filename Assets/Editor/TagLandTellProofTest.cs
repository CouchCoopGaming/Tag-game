using UnityEditor;
using UnityEngine;

/// <summary>
/// Batchmode: Unity -batchmode -nographics -quit -projectPath . -executeMethod TagLandTellProofTest.Run
/// </summary>
public static class TagLandTellProofTest
{
    public static void Run()
    {
        TagLandTellReport report = TagLandTellProof.Run();
        var host = new GameObject("TagLandTellProofHost");
        try
        {
            Tag.Art.TagLandFlash.PlayOn(host.transform);
            var mark = host.transform.Find(Tag.Art.TagLandTell.MarkerName);
            if (mark == null)
                report.Fail("TagLandFlash marker was not built");
            else if (mark.GetComponentInChildren<Collider>(true) != null)
                report.Fail("tag land tell has a collider and can change punch reach");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }

        Debug.Log("[TagLand]\n" + report);
        if (!report.Ok)
        {
            Debug.LogError("[TagLand] FAIL\n" + report.FailureText);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[TagLand] OK");
        EditorApplication.Exit(0);
    }
}
