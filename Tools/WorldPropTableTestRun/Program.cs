using System;
using System.IO;

namespace Tag.Tools
{
    /// <summary>
    /// Runs WorldPropTableTests outside the editor. Unity is not installed here.
    /// The test method is the EditMode test; Resources.Load reads the YAML table.
    /// </summary>
    static class Program
    {
        static int Main()
        {
            string root = FindRepoRoot();
            Directory.SetCurrentDirectory(root);
            var test = new WorldPropTableTests();
            test.DistrictPlacementsResolveOutsideTheEditorPath();
            Console.WriteLine("WorldPropTableTests passed DistrictPlacementsResolveOutsideTheEditorPath");
            return 0;
        }

        static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "Assets", "Tests", "EditMode", "WorldPropTableTests.cs")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException("repo root not found");
        }
    }
}
