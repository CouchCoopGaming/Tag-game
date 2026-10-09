using System.IO;
using Tag.Art;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EditMode smoke. Batchmode:
/// Unity -batchmode -nographics -quit -projectPath . -executeMethod CompileSmokeTest.Run
/// Confirms the scripts Unity compiles for Assembly-CSharp still name
/// System.IO.Compression.CompressionLevel in full. Unity 6000.3 also defines
/// UnityEngine.CompressionLevel, and the short name is CS0104.
/// </summary>
public static class CompileSmokeTest
{
    public static void Run()
    {
        int fail = 0;
        if (SmoothMotion.RootMotion)
        {
            Debug.LogError("[CompileSmoke] root motion must stay off");
            fail++;
        }

        string path = "Assets/Scripts/Art/SmoothMotion.cs";
        if (!File.Exists(path))
        {
            Debug.LogError("[CompileSmoke] MISSING " + path);
            fail++;
        }
        else
        {
            string src = File.ReadAllText(path);
            if (src.Contains("DeflateStream(ms, CompressionLevel."))
            {
                Debug.LogError("[CompileSmoke] CompressionLevel is not fully qualified in " + path);
                fail++;
            }
            if (!src.Contains("System.IO.Compression.CompressionLevel.Fastest"))
            {
                Debug.LogError("[CompileSmoke] expected System.IO.Compression.CompressionLevel.Fastest");
                fail++;
            }
        }

        if (fail == 0)
            Debug.Log("[CompileSmoke] ok compression=qualified rootMotion=0");
        else
            Debug.LogError("[CompileSmoke] FAIL " + fail);
        EditorApplication.Exit(fail == 0 ? 0 : 1);
    }
}
