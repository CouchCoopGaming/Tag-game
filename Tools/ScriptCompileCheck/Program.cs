using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tag.Tools
{
    /// <summary>
    /// Headless compile of Assets/Scripts (plus the movement scripts they call).
    /// Unity is not installed here. Reference assemblies are the shared StrafeJumpSim
    /// Unity stub plus the BCL. Diagnostics are filtered to duplicate members and
    /// names that belong to this repo, so a missing Unity API does not fail the run.
    /// </summary>
    static class Program
    {
        static readonly string[] Watch = { "CS0102", "CS0128", "CS0136", "CS0103", "CS0246" };

        static readonly string[] ExternalPrefixes =
        {
            "UnityEngine", "UnityEditor", "Unity.", "TMPro", "Cinemachine",
            "System", "Microsoft", "JetBrains", "nunit", "NUnit"
        };

        static int Main()
        {
            string root = FindRepoRoot();
            var files = new List<string>();
            AddCs(files, Path.Combine(root, "Assets", "Scripts"));
            AddCs(files, Path.Combine(root, "Assets", "TagArenaMovement", "Scripts"));
            string stub = Path.Combine(root, "Tools", "StrafeJumpSim", "UnityStub", "UnityEngine.cs");
            if (File.Exists(stub))
                files.Add(stub);

            var parse = new CSharpParseOptions(preprocessorSymbols: new[] { "ENABLE_INPUT_SYSTEM" });
            var trees = new List<SyntaxTree>();
            var parseErrors = new List<string>();
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text, parse, path: file);
                trees.Add(tree);
                foreach (Diagnostic d in tree.GetDiagnostics())
                {
                    if (d.Severity == DiagnosticSeverity.Error)
                        parseErrors.Add(Format(root, d));
                }
            }

            var refs = new List<MetadataReference>();
            foreach (string path in TrustedAssemblies())
                refs.Add(MetadataReference.CreateFromFile(path));

            // No implicit global usings. Real Unity/InputSystem assemblies are not on
            // this machine (unity-refs=absent); the pattern scan below is the gate
            // for the errors the stub compilation cannot see.
            CSharpCompilation compilation = CSharpCompilation.Create(
                "TagScriptsCheck",
                trees,
                refs,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    usings: ImmutableArray<string>.Empty));

            HashSet<string> ours = DeclaredTypeNames(trees);
            var hits = new List<string>();
            foreach (Diagnostic d in compilation.GetDiagnostics())
            {
                if (d.Severity != DiagnosticSeverity.Error) continue;
                if (!Want(d, ours)) continue;
                hits.Add(Format(root, d));
            }

            hits.Sort(StringComparer.Ordinal);
            Console.WriteLine("script-compile-check files=" + files.Count
                + " parseErrors=" + parseErrors.Count
                + " duplicateOrMissing=" + hits.Count);
            foreach (string line in parseErrors)
                Console.WriteLine(line);
            foreach (string line in hits)
                Console.WriteLine(line);

            bool compileOk = parseErrors.Count == 0 && hits.Count == 0;
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
            Console.WriteLine("script-compile-check ok CS0102 CS0128 CS0136 CS0103 CS0246-in-our-code");
            return 0;
        }

        static bool Want(Diagnostic d, HashSet<string> ours)
        {
            string id = d.Id;
            if (id == "CS0102" || id == "CS0128" || id == "CS0136")
                return true;
            if (id != "CS0103" && id != "CS0246")
                return false;
            if (!Array.Exists(Watch, w => w == id))
                return false;

            string text = d.GetMessage();
            string missing = Quoted(text);
            if (string.IsNullOrEmpty(missing))
                return id == "CS0103";
            if (IsExternal(missing))
                return false;
            // CS0246 for a type this repo declares is a real break. A Unity type the
            // stub does not contain is not. CS0103 follows the same split.
            if (ours.Contains(Leaf(missing)))
                return true;
            if (id == "CS0246" && missing.IndexOf('.') < 0 && !ours.Contains(missing))
                return false;
            return id == "CS0103" && ours.Contains(Leaf(missing));
        }

        static string Quoted(string message)
        {
            int a = message.IndexOf('\'');
            if (a < 0) return "";
            int b = message.IndexOf('\'', a + 1);
            if (b < 0) return "";
            return message.Substring(a + 1, b - a - 1);
        }

        static string Leaf(string name)
        {
            int dot = name.LastIndexOf('.');
            return dot < 0 ? name : name.Substring(dot + 1);
        }

        static bool IsExternal(string name)
        {
            for (int i = 0; i < ExternalPrefixes.Length; i++)
            {
                string p = ExternalPrefixes[i];
                if (name == p || name.StartsWith(p, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static HashSet<string> DeclaredTypeNames(List<SyntaxTree> trees)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (SyntaxTree tree in trees)
            {
                foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
                    names.Add(type.Identifier.ValueText);
            }
            return names;
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

        static IEnumerable<string> TrustedAssemblies()
        {
            string list = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrEmpty(list))
                yield break;
            foreach (string path in list.Split(Path.PathSeparator))
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith("System.", StringComparison.Ordinal)
                    || name == "System.Private.CoreLib.dll"
                    || name == "netstandard.dll"
                    || name == "mscorlib.dll")
                    yield return path;
            }
        }

        static string Format(string root, Diagnostic d)
        {
            string path = d.Location.SourceTree != null ? d.Location.SourceTree.FilePath : "";
            if (path.StartsWith(root, StringComparison.Ordinal))
                path = path.Substring(root.Length).TrimStart('/', '\\');
            FileLinePositionSpan span = d.Location.GetLineSpan();
            int line = span.StartLinePosition.Line + 1;
            int col = span.StartLinePosition.Character + 1;
            return d.Id + " " + path + "(" + line + "," + col + "): " + d.GetMessage();
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
