using System;
using System.Collections.Generic;
using System.IO;

namespace Tag.Tools
{
    /// <summary>
    /// Headless compile of every runtime script and every editor test.
    /// The compiler is csc (or mcs). References are the UnityEngine module DLLs,
    /// not the StrafeJumpSim stub. Diagnostics are not filtered.
    /// </summary>
    static class Program
    {
        static int Main()
        {
            string root = FindRepoRoot();
            bool compileOk = UnityCsc.Run(root, out string cscLine, out string cscReport);
            Console.WriteLine(cscLine);
            if (!compileOk)
                Console.Error.WriteLine(cscReport);

            var files = new List<string>();
            AddCs(files, Path.Combine(root, "Assets", "Scripts"));
            AddCs(files, Path.Combine(root, "Assets", "TagArenaMovement", "Scripts"));
            AddCs(files, Path.Combine(root, "Assets", "Editor"));
            bool smokeOk = SmokeFiles.Run(root, out string smokeLine, out string smokeReport);
            Console.WriteLine(smokeLine);
            if (!smokeOk)
                Console.Error.WriteLine(smokeReport);
            bool patternsOk = UnityCompilePatterns.Run(root, files, out string patternLine, out string patternReport);
            Console.WriteLine(patternLine);
            if (!patternsOk)
                Console.Error.WriteLine(patternReport);
            if (!compileOk || !smokeOk || !patternsOk)
                return 1;
            Console.WriteLine("script-compile-check ok csc");
            return 0;
        }

        static void AddCs(List<string> files, string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.IndexOf("/obj/", StringComparison.Ordinal) >= 0) continue;
                files.Add(file);
            }
        }

        static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (Directory.Exists(Path.Combine(dir, "Assets")) && Directory.Exists(Path.Combine(dir, "Tools")))
                    return dir;
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
