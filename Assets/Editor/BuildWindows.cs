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
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;
            EditorUserBuildSettings.allowDebugging = false;
            string outDir = Path.GetFullPath("Builds/Windows");
            Directory.CreateDirectory(outDir);
            string exe = Path.Combine(outDir, "Tag.exe");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Boot.unity", "Assets/Scenes/Play.unity" },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                // Release: no Development / Profiler / Autoconnect
                options = BuildOptions.CompressWithLz4HC
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Tag] Windows build → " + exe + " result=" + report.summary.result);
            EditorUtility.RevealInFinder(exe);
        }
    }
}
#endif
