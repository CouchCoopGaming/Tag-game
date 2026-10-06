using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Tag.Core
{
    /// <summary>
    /// Counts allocation-shaped calls inside Update / FixedUpdate / LateUpdate / OnGUI
    /// and the methods those call directly in the same file.
    /// Baseline is the count on the settings-rebind tip before this pass.
    /// </summary>
    public static class HotPathAlloc
    {
        public const int Baseline = 101;

        static readonly string[] Flags =
        {
            ".ToString(",
            "$\"",
            "new List",
            "new GUIStyle",
            "new StringBuilder",
            "new MaterialPropertyBlock",
            "GetComponent",
            ".Where(",
            ".Select(",
            "string.Format",
            ".ToList(",
            ".ToArray("
        };

        static readonly Regex MethodRegex = new Regex(
            @"(?:^|\n)\s*(?:\[[^\]]+\]\s*)*(?:public|private|protected|internal|static|virtual|override|sealed|\s)+\s*[\w.<>,\[\]\?]+\s+(\w+)\s*\([^;{}]*\)\s*\{",
            RegexOptions.Compiled);

        public struct Report
        {
            public bool Ok;
            public int Before;
            public int After;
            public string Line;
            public string Failure;
        }

        public static Report Run()
        {
            var report = new Report();
            report.Before = Baseline;
            report.After = CountTree(FindRoot());
            report.Ok = report.After < report.Before && report.After >= 0;
            if (!report.Ok)
                report.Failure = "hot path allocations did not drop (" + report.After + " vs " + report.Before + ")";
            report.Line = "hot-path allocs before=" + report.Before.ToString()
                + " after=" + report.After.ToString()
                + " flags=" + (report.Ok ? "dropped" : "regressed");
            return report;
        }

        public static int CountTree(string root)
        {
            if (string.IsNullOrEmpty(root)) return -1;
            int total = 0;
            total += CountDir(Path.Combine(root, "Assets", "Scripts"));
            total += CountDir(Path.Combine(root, "Assets", "TagArenaMovement", "Scripts"));
            return total;
        }

        static int CountDir(string dir)
        {
            if (!Directory.Exists(dir)) return 0;
            int total = 0;
            string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) >= 0)
                    continue;
                total += CountFile(File.ReadAllText(files[i]));
            }
            return total;
        }

        public static int CountFile(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            Dictionary<string, List<string>> methods = Parse(text);
            var reached = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string[] roots = { "Update", "FixedUpdate", "LateUpdate", "OnGUI" };
            for (int r = 0; r < roots.Length; r++)
            {
                List<string> bodies;
                if (!methods.TryGetValue(roots[r], out bodies)) continue;
                for (int b = 0; b < bodies.Count; b++)
                {
                    reached.Add(bodies[b]);
                    CollectCalls(bodies[b], methods, seen, reached);
                }
            }

            int count = 0;
            for (int i = 0; i < reached.Count; i++)
            {
                string body = Strip(reached[i]);
                for (int f = 0; f < Flags.Length; f++)
                    count += Occurrences(body, Flags[f]);
            }
            return count;
        }

        static void CollectCalls(string body, Dictionary<string, List<string>> methods, HashSet<string> seen, List<string> reached)
        {
            int i = 0;
            while (i < body.Length)
            {
                int open = body.IndexOf('(', i);
                if (open < 0) break;
                int nameEnd = open;
                while (nameEnd > 0 && char.IsWhiteSpace(body[nameEnd - 1])) nameEnd--;
                int nameStart = nameEnd;
                while (nameStart > 0 && IsName(body[nameStart - 1])) nameStart--;
                if (nameStart < nameEnd)
                {
                    string name = body.Substring(nameStart, nameEnd - nameStart);
                    List<string> callees;
                    if (methods.TryGetValue(name, out callees) && seen.Add(name))
                    {
                        for (int c = 0; c < callees.Count; c++)
                            reached.Add(callees[c]);
                    }
                }
                i = open + 1;
            }
        }

        static bool IsName(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        static Dictionary<string, List<string>> Parse(string text)
        {
            var methods = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            MatchCollection matches = MethodRegex.Matches(text);
            for (int m = 0; m < matches.Count; m++)
            {
                Match match = matches[m];
                string name = match.Groups[1].Value;
                int brace = match.Index + match.Length - 1;
                int end = Close(text, brace);
                if (end < 0) continue;
                string body = text.Substring(brace, end - brace + 1);
                List<string> list;
                if (!methods.TryGetValue(name, out list))
                {
                    list = new List<string>();
                    methods[name] = list;
                }
                list.Add(body);
            }
            return methods;
        }

        static int Close(string text, int brace)
        {
            int depth = 0;
            for (int i = brace; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        static string Strip(string body)
        {
            var keep = new System.Text.StringBuilder(body.Length);
            int i = 0;
            while (i < body.Length)
            {
                if (i + 1 < body.Length && body[i] == '/' && body[i + 1] == '/')
                {
                    while (i < body.Length && body[i] != '\n') i++;
                    continue;
                }
                if (i + 1 < body.Length && body[i] == '/' && body[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < body.Length && !(body[i] == '*' && body[i + 1] == '/')) i++;
                    i += 2;
                    continue;
                }
                keep.Append(body[i]);
                i++;
            }
            return keep.ToString();
        }

        static int Occurrences(string hay, string needle)
        {
            int n = 0;
            int i = 0;
            while (i >= 0 && i < hay.Length)
            {
                i = hay.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) break;
                n++;
                i += needle.Length;
            }
            return n;
        }

        static string FindRoot()
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
