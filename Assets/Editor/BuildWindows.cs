#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tag.EditorTools
{
    public static class BuildWindows
    {
        [MenuItem("Tag/Build Windows Standalone")]
        public static void Build()
        {
            string outDir = Path.GetFullPath("Builds/Windows");
            Directory.CreateDirectory(outDir);
            string exe = Path.Combine(outDir, "Tag.exe");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Boot.unity", "Assets/Scenes/Play.unity" },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Tag] Windows build → " + exe + " result=" + report.summary.result);
            EditorUtility.RevealInFinder(exe);
        }
    }
}
#endif
