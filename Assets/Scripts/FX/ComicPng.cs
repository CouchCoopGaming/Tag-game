using System;
using System.Collections.Generic;
using System.IO;

namespace Tag.FX
{
    /// <summary>
    /// PNG bytes live in Assets/Art/FX and are copied to StreamingAssets for a
    /// player build. They used to be one chained string literal, which overflowed
    /// csc inside BinaryExpressionSyntax (0xC00000FD).
    /// </summary>
    public static class ComicPng
    {
        public static byte[] Read(string fileName)
        {
            foreach (string path in Candidates(fileName))
            {
                if (!File.Exists(path)) continue;
                return File.ReadAllBytes(path);
            }
            return Array.Empty<byte>();
        }

        static IEnumerable<string> Candidates(string fileName)
        {
            var roots = new List<string>();
            PushRoots(roots, Directory.GetCurrentDirectory());
            PushRoots(roots, AppContext.BaseDirectory);
            string data = DataFolder(AppContext.BaseDirectory);
            if (!string.IsNullOrEmpty(data))
                roots.Add(data);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < roots.Count; i++)
            {
                string root = roots[i];
                if (string.IsNullOrEmpty(root) || !seen.Add(root)) continue;
                yield return Path.Combine(root, "Assets", "Art", "FX", fileName);
                yield return Path.Combine(root, "Assets", "StreamingAssets", "FX", fileName);
                yield return Path.Combine(root, "StreamingAssets", "FX", fileName);
            }
        }

        static void PushRoots(List<string> roots, string start)
        {
            string dir = start;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                roots.Add(dir);
                DirectoryInfo parent = Directory.GetParent(dir);
                dir = parent == null ? null : parent.FullName;
            }
        }

        static string DataFolder(string baseDir)
        {
            if (string.IsNullOrEmpty(baseDir)) return null;
            string dir = baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            DirectoryInfo parent = Directory.GetParent(dir);
            if (parent == null) return null;
            if (parent.Name.EndsWith("_Data", StringComparison.Ordinal))
                return parent.FullName;
            return null;
        }
    }
}
