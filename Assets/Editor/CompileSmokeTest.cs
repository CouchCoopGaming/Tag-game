using System.IO;
using Tag.Art;
using Tag.FX;
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
    static readonly string[] DeflateFiles =
    {
        "Assets/Scripts/Art/SmoothMotion.cs",
        "Assets/Scripts/Art/Exits/VerbExitStills.cs",
        "Assets/Scripts/FX/FxStills.cs",
        "Assets/Scripts/FX/Pass5Stills.cs",
        "Assets/Scripts/FX/VerbFxStills.cs",
        "Assets/Scripts/Level/ArenaStill.cs",
        "Assets/Scripts/Level/MegaParkP1Layout.Pass9.cs",
    };

    public static void Run()
    {
        int fail = 0;
        if (SmoothMotion.RootMotion)
        {
            Debug.LogError("[CompileSmoke] root motion must stay off");
            fail++;
        }

        for (int i = 0; i < DeflateFiles.Length; i++)
            fail += ExpectQualified(DeflateFiles[i]);

        fail += ExpectPng("ComicAtlas", ComicAtlas.Png(), ComicAtlas.Columns * ComicAtlas.CellWidth, ComicAtlas.Rows * ComicAtlas.CellHeight);
        fail += ExpectPng("ComicBurstAtlas", ComicBurstAtlas.Png(), ComicBurstAtlas.Columns * ComicBurstAtlas.CellWidth, ComicBurstAtlas.Rows * ComicBurstAtlas.CellHeight);
        fail += ExpectPng("ComicDizzy", ComicDizzy.Png(), ComicDizzy.Size, ComicDizzy.Size);

        if (fail == 0)
            Debug.Log("[CompileSmoke] ok compression=qualified rootMotion=0 atlas=file dizzy=file");
        else
            Debug.LogError("[CompileSmoke] FAIL " + fail);
        EditorApplication.Exit(fail == 0 ? 0 : 1);
    }

    static int ExpectQualified(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("[CompileSmoke] MISSING " + path);
            return 1;
        }
        string src = File.ReadAllText(path);
        if (src.Contains("DeflateStream(ms, CompressionLevel."))
        {
            Debug.LogError("[CompileSmoke] CompressionLevel is not fully qualified in " + path);
            return 1;
        }
        if (!src.Contains("System.IO.Compression.CompressionLevel.Fastest"))
        {
            Debug.LogError("[CompileSmoke] expected System.IO.Compression.CompressionLevel.Fastest in " + path);
            return 1;
        }
        return 0;
    }

    static int ExpectPng(string name, byte[] png, int width, int height)
    {
        if (png == null || png.Length < 8 || png[0] != 0x89 || png[1] != 0x50)
        {
            Debug.LogError("[CompileSmoke] " + name + " did not load a png");
            return 1;
        }
        if (!ComicArt.DecodePng(png, out int w, out int h, out byte[] rgba) || w != width || h != height || rgba == null)
        {
            Debug.LogError("[CompileSmoke] " + name + " decoded " + w + "x" + h + " expected " + width + "x" + height);
            return 1;
        }
        Debug.Log("[CompileSmoke] " + name + " " + w + "x" + h + " bytes=" + png.Length);
        return 0;
    }
}
