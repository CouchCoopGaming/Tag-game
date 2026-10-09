using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tag.Tools
{
    /// <summary>
    /// Patterns the Unity compiler rejects that the stub compilation cannot see:
    /// Tag.Input shadowing, Gamepad.all (a struct) compared to null, a method named
    /// Tag shadowing the Tag namespace, ambiguous BCL names, and GUI.skin used off
    /// the OnGUI call path.
    /// </summary>
    static class UnityCompilePatterns
    {
        static readonly Regex BareInput = new Regex(
            @"(?<![\w.])Input\.(GetKeyDown|GetKeyUp|GetKey|GetAxisRaw|GetMouseButtonDown|GetMouseButtonUp|GetMouseButton)\b",
            RegexOptions.Compiled);

        static readonly Regex GamepadAllNull = new Regex(
            @"Gamepad\.all\s*(==|!=)\s*null",
            RegexOptions.Compiled);

        static readonly Regex GamepadAllAssign = new Regex(
            @"(?:var|ReadOnlyArray\s*<\s*Gamepad\s*>)\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?:UnityEngine\.InputSystem\.)?Gamepad\.all\b",
            RegexOptions.Compiled);

        static readonly Regex AmbientBare = new Regex(
            @"(?<![\w.])AmbientMode\b",
            RegexOptions.Compiled);

        static readonly Regex ComparisonBare = new Regex(
            @"(?<![\w.])Comparison\s*<",
            RegexOptions.Compiled);

        static readonly Regex ZipUninit = new Regex(
            @"Vector3\s+zipPoint\s*;",
            RegexOptions.Compiled);

        static readonly HashSet<string> Lifecycle = new HashSet<string>(StringComparer.Ordinal)
        {
            "Awake", "Start", "OnEnable", "OnDisable", "Update", "FixedUpdate", "LateUpdate", "OnDestroy"
        };

        public static bool Run(string root, List<string> files, out string line, out string report)
        {
            var log = new List<string>();
            int bareInput = 0;
            int gamepadNull = 0;
            int tagShadow = 0;
            int ambient = 0;
            int comparison = 0;
            int zip = 0;
            int compression = 0;
            int concatChain = 0;

            foreach (string file in files)
            {
                if (file.IndexOf("UnityStub", StringComparison.Ordinal) >= 0) continue;
                string text = File.ReadAllText(file);
                string rel = Rel(root, file);
                string ns = NamespaceOf(text);
                bool tagNs = ns == "Tag" || ns.StartsWith("Tag.", StringComparison.Ordinal);

                if (tagNs)
                {
                    foreach (Match m in BareInput.Matches(text))
                    {
                        bareInput++;
                        log.Add("bare-Input " + rel + ":" + LineOf(text, m.Index) + " " + m.Value);
                    }
                }

                foreach (Match m in GamepadAllNull.Matches(text))
                {
                    gamepadNull++;
                    log.Add("gamepad-all-null " + rel + ":" + LineOf(text, m.Index));
                }
                gamepadNull += GamepadLocalNull(text, rel, log);

                if (rel.EndsWith("TagRole.cs", StringComparison.Ordinal))
                    tagShadow += TagMethodShadow(text, rel, log);

                if (rel.EndsWith("MegaParkP1Bootstrap.cs", StringComparison.Ordinal))
                {
                    foreach (Match m in AmbientBare.Matches(text))
                    {
                        ambient++;
                        log.Add("bare-AmbientMode " + rel + ":" + LineOf(text, m.Index));
                    }
                }

                if (rel.EndsWith("SpeedEnergyHUD.cs", StringComparison.Ordinal))
                {
                    foreach (Match m in ComparisonBare.Matches(text))
                    {
                        comparison++;
                        log.Add("bare-Comparison " + rel + ":" + LineOf(text, m.Index));
                    }
                }

                if (rel.EndsWith("PlayPromptHud.cs", StringComparison.Ordinal))
                {
                    foreach (Match m in ZipUninit.Matches(text))
                    {
                        zip++;
                        log.Add("zipPoint-unassigned " + rel + ":" + LineOf(text, m.Index));
                    }
                }

                compression += AmbiguousCompression(text, rel, log);
                concatChain += ConcatChain(text, rel, log);
            }

            int gui = GuiSkinOutsideOnGui(files, root, log);
            bool unityRefs = UnityRefsPresent();
            string patterns = "unity-patterns bare-input=" + bareInput
                + " gamepad-all-null=" + gamepadNull
                + " tag-shadow=" + tagShadow
                + " ambient=" + ambient
                + " comparison=" + comparison
                + " zipPoint=" + zip
                + " compression=" + compression
                + " concat-chain=" + concatChain
                + " unity-refs=" + (unityRefs ? "present" : "absent");
            string guiLine = "qa-gui-skin outside-ongui=" + gui;
            var body = new System.Text.StringBuilder();
            body.AppendLine(patterns);
            body.AppendLine(guiLine);
            foreach (string row in log)
                body.AppendLine(row);
            line = patterns + " | " + guiLine;
            report = body.ToString();
            return bareInput == 0 && gamepadNull == 0 && tagShadow == 0
                && ambient == 0 && comparison == 0 && zip == 0 && gui == 0
                && compression == 0 && concatChain == 0;
        }

        /// <summary>
        /// Unity 6000.3 defines UnityEngine.CompressionLevel. A file that also
        /// imports System.IO.Compression must spell the BCL type in full.
        /// </summary>
        static int AmbiguousCompression(string text, string rel, List<string> log)
        {
            bool unity = Regex.IsMatch(text, @"(?m)^using\s+UnityEngine\s*;");
            bool io = Regex.IsMatch(text, @"(?m)^using\s+System\.IO\.Compression\s*;");
            if (!unity || !io) return 0;
            int n = 0;
            foreach (Match m in Regex.Matches(text, @"(?<![\w.])CompressionLevel\b"))
            {
                n++;
                log.Add("ambiguous-CompressionLevel " + rel + ":" + LineOf(text, m.Index));
            }
            return n;
        }

        /// <summary>
        /// A long chain of binary + expressions overflows csc (0xC00000FD) inside
        /// BinaryExpressionSyntax. Sixty-four operands is the gate. Real pose
        /// data belongs in an array or a file, not in one expression.
        /// </summary>
        static int ConcatChain(string text, string rel, List<string> log)
        {
            const int Limit = 64;
            string[] lines = text.Split('\n');
            int run = 0;
            int best = 0;
            int bestLine = 1;
            for (int i = 0; i < lines.Length; i++)
            {
                string s = lines[i].Trim();
                if (s.Length == 0) continue;
                if (s.EndsWith("+", StringComparison.Ordinal))
                {
                    run++;
                    if (run > best)
                    {
                        best = run;
                        bestLine = i + 1;
                    }
                }
                else
                {
                    run = 0;
                }
                int plus = 0;
                bool inString = false;
                for (int c = 0; c < s.Length; c++)
                {
                    char ch = s[c];
                    if (ch == '"' && (c == 0 || s[c - 1] != '\\')) inString = !inString;
                    else if (ch == '+' && !inString) plus++;
                }
                if (plus > Limit)
                {
                    log.Add("concat-chain " + rel + ":" + (i + 1) + " depth=" + plus);
                    return plus;
                }
            }
            if (best > Limit)
            {
                log.Add("concat-chain " + rel + ":" + bestLine + " depth=" + best);
                return best;
            }
            return 0;
        }

        static int GamepadLocalNull(string text, string rel, List<string> log)
        {
            int n = 0;
            if (GamepadAllAssign.Matches(text).Count == 0) return 0;
            // Walk each method-sized chunk that contains an assignment.
            int offset = 0;
            foreach (string chunk in SplitMethods(text))
            {
                var names = new List<string>();
                foreach (Match m in GamepadAllAssign.Matches(chunk))
                    names.Add(m.Groups[1].Value);
                foreach (string name in names)
                {
                    var cmp = Regex.Matches(chunk, @"\b" + Regex.Escape(name) + @"\s*(==|!=)\s*null");
                    foreach (Match m in cmp)
                    {
                        n++;
                        log.Add("gamepad-all-null " + rel + ":" + LineOf(text, offset + m.Index) + " " + name);
                    }
                }
                offset += chunk.Length;
            }
            return n;
        }

        static List<string> SplitMethods(string text)
        {
            var list = new List<string>();
            var starts = new List<int>();
            foreach (Match m in Regex.Matches(text, @"(?m)^[ \t]*(?:\[[^\]]+\][ \t]*\r?\n[ \t]*)*(?:public |private |protected |internal |static )"))
                starts.Add(m.Index);
            if (starts.Count == 0)
            {
                list.Add(text);
                return list;
            }
            for (int i = 0; i < starts.Count; i++)
            {
                int end = i + 1 < starts.Count ? starts[i + 1] : text.Length;
                list.Add(text.Substring(starts[i], end - starts[i]));
            }
            return list;
        }

        static int TagMethodShadow(string text, string rel, List<string> log)
        {
            int n = 0;
            var rx = new Regex(@"\b[\w<>\[\]]+\s+Tag\s*\([^)]*\)\s*\{", RegexOptions.Compiled);
            foreach (Match m in rx.Matches(text))
            {
                int body = BraceBody(text, m.Index + m.Length - 1);
                if (body < 0) continue;
                string slice = text.Substring(m.Index, body - m.Index);
                foreach (Match hit in Regex.Matches(slice, @"(?<!global::)(?<![\w.])Tag\.(Modes|Art|Audio)\b"))
                {
                    n++;
                    log.Add("tag-shadow " + rel + ":" + LineOf(text, m.Index + hit.Index) + " " + hit.Value);
                }
            }
            return n;
        }

        static int BraceBody(string text, int open)
        {
            if (open < 0 || open >= text.Length || text[open] != '{') return -1;
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return i + 1;
                }
            }
            return -1;
        }

        static int GuiSkinOutsideOnGui(List<string> files, string root, List<string> log)
        {
            var parse = new CSharpParseOptions(preprocessorSymbols: new[] { "ENABLE_INPUT_SYSTEM" });
            var methods = new List<MethodInfo>();
            var index = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (string file in files)
            {
                if (file.IndexOf("UnityStub", StringComparison.Ordinal) >= 0) continue;
                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), parse, path: file);
                foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    ClassDeclarationSyntax cls = method.Parent as ClassDeclarationSyntax;
                    if (cls == null) continue;
                    var info = new MethodInfo
                    {
                        Key = cls.Identifier.ValueText + "." + method.Identifier.ValueText,
                        Name = method.Identifier.ValueText,
                        ClassName = cls.Identifier.ValueText,
                        File = Rel(root, file),
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        UsesGuiSkin = UsesGuiSkin(method),
                        Callees = Callees(method, cls.Identifier.ValueText)
                    };
                    int id = methods.Count;
                    methods.Add(info);
                    if (!index.TryGetValue(info.Key, out List<int> ids))
                    {
                        ids = new List<int>();
                        index[info.Key] = ids;
                    }
                    ids.Add(id);
                }
            }

            var edges = new List<int>[methods.Count];
            for (int i = 0; i < methods.Count; i++)
            {
                edges[i] = new List<int>();
                foreach (string callee in methods[i].Callees)
                {
                    if (!index.TryGetValue(callee, out List<int> ids)) continue;
                    foreach (int id in ids)
                        edges[i].Add(id);
                }
            }

            var fromOnGui = Reach(methods, edges, name => name == "OnGUI");
            var fromLife = Reach(methods, edges, name => Lifecycle.Contains(name));
            int bad = 0;
            for (int i = 0; i < methods.Count; i++)
            {
                if (!methods[i].UsesGuiSkin) continue;
                bool onGui = methods[i].Name == "OnGUI" || fromOnGui.Contains(i);
                bool life = fromLife.Contains(i);
                if (onGui && !life) continue;
                bad++;
                log.Add("gui-skin " + methods[i].File + ":" + methods[i].Line + " " + methods[i].Key
                    + (life ? " reachable-from-lifecycle" : " not-on-ongui-path"));
            }
            return bad;
        }

        static HashSet<int> Reach(List<MethodInfo> methods, List<int>[] edges, Func<string, bool> root)
        {
            var seen = new HashSet<int>();
            var stack = new Stack<int>();
            for (int i = 0; i < methods.Count; i++)
            {
                if (!root(methods[i].Name)) continue;
                stack.Push(i);
            }
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                if (!seen.Add(i)) continue;
                foreach (int next in edges[i])
                    stack.Push(next);
            }
            return seen;
        }

        static bool UsesGuiSkin(MethodDeclarationSyntax method)
        {
            foreach (SyntaxNode node in method.DescendantNodes())
            {
                if (node is MemberAccessExpressionSyntax access
                    && access.Expression is IdentifierNameSyntax id
                    && id.Identifier.ValueText == "GUI"
                    && access.Name.Identifier.ValueText == "skin")
                    return true;
            }
            return false;
        }

        static List<string> Callees(MethodDeclarationSyntax method, string className)
        {
            var list = new List<string>();
            foreach (InvocationExpressionSyntax call in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                switch (call.Expression)
                {
                    case IdentifierNameSyntax id:
                        list.Add(className + "." + id.Identifier.ValueText);
                        break;
                    case MemberAccessExpressionSyntax access:
                        list.Add(Receiver(access.Expression, className) + "." + access.Name.Identifier.ValueText);
                        break;
                }
            }
            return list;
        }

        static string Receiver(ExpressionSyntax expression, string className)
        {
            switch (expression)
            {
                case IdentifierNameSyntax id:
                    return id.Identifier.ValueText;
                case MemberAccessExpressionSyntax access:
                    return access.Name.Identifier.ValueText;
                default:
                    return className;
            }
        }

        static string NamespaceOf(string text)
        {
            Match m = Regex.Match(text, @"namespace\s+([A-Za-z0-9_.]+)");
            return m.Success ? m.Groups[1].Value : "";
        }

        static int LineOf(string text, int index)
        {
            int line = 1;
            int n = index < text.Length ? index : text.Length;
            for (int i = 0; i < n; i++)
            {
                if (text[i] == '\n') line++;
            }
            return line;
        }

        static string Rel(string root, string path)
        {
            if (path.StartsWith(root, StringComparison.Ordinal))
                return path.Substring(root.Length).TrimStart('/', '\\');
            return path;
        }

        static bool UnityRefsPresent()
        {
            string[] roots =
            {
                Environment.GetEnvironmentVariable("UNITY_REF_DIR"),
                "/tmp/unity-6000",
                "/opt/unity",
                "/usr/lib/unity",
                "/opt/Unity",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity")
            };
            foreach (string dir in roots)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                foreach (string name in new[] { "UnityEngine.CoreModule.dll", "UnityEngine.dll" })
                {
                    foreach (string file in Directory.GetFiles(dir, name, SearchOption.AllDirectories))
                    {
                        if (file.IndexOf("InputSystem", StringComparison.Ordinal) >= 0) continue;
                        return true;
                    }
                }
            }
            return false;
        }

        sealed class MethodInfo
        {
            public string Key;
            public string Name;
            public string ClassName;
            public string File;
            public int Line;
            public bool UsesGuiSkin;
            public List<string> Callees;
        }
    }
}
