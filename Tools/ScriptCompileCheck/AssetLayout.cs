using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tag.Tools
{
    /// <summary>
    /// Layout faults Unity refuses at import but csc never sees:
    /// two .asmdef files in one folder, and two .meta files with the same guid.
    /// </summary>
    static class AssetLayout
    {
        public static bool Run(string root, out string line, out string report)
        {
            var log = new StringBuilder();
            string assets = Path.Combine(root, "Assets");
            int asmdefClash = 0;
            foreach (var dir in Directory.GetFiles(assets, "*.asmdef", SearchOption.AllDirectories)
                .GroupBy(f => Path.GetDirectoryName(f)))
            {
                if (dir.Count() < 2) continue;
                asmdefClash++;
                log.AppendLine("asmdef clash " + Rel(root, dir.Key) + ": "
                    + string.Join(", ", dir.Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal)));
            }

            var byGuid = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            int metas = 0;
            foreach (string meta in Directory.GetFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                metas++;
                foreach (string raw in File.ReadLines(meta))
                {
                    if (!raw.StartsWith("guid:", StringComparison.Ordinal)) continue;
                    string g = raw.Substring(5).Trim();
                    if (!byGuid.TryGetValue(g, out var list)) byGuid[g] = list = new List<string>();
                    list.Add(Rel(root, meta));
                    break;
                }
            }
            int dupGuids = 0;
            foreach (var kv in byGuid.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kv.Value.Count < 2) continue;
                dupGuids++;
                log.AppendLine("duplicate guid " + kv.Key + ": " + string.Join(", ", kv.Value.OrderBy(n => n, StringComparer.Ordinal)));
            }

            bool ok = asmdefClash == 0 && dupGuids == 0;
            line = "asset-layout metas=" + metas + " asmdefFolderClash=" + asmdefClash + " duplicateGuids=" + dupGuids
                + (ok ? "" : " FAIL");
            report = log.ToString();
            return ok;
        }

        static string Rel(string root, string path)
        {
            return path.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/');
        }
    }
}
