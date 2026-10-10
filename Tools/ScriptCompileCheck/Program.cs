using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Tag.Tools
{
    /// <summary>
    /// Real csc compile of every Assets/Scripts C# file and the editor test
    /// assemblies, against Unity 6000.3.24f1 reference assemblies. CS1061,
    /// CS0117, CS0103, and CS0104 fail the run. The old stub compilation
    /// dropped those and let a missing member through.
    /// </summary>
    static class Program
    {
        static readonly string[] MustCatch = { "CS1061", "CS0117", "CS0103", "CS0104" };

        static readonly string[] Defines =
        {
            "UNITY_EDITOR", "UNITY_INCLUDE_TESTS",
            "ENABLE_INPUT_SYSTEM",
            "UNITY_6000_3",
            "UNITY_6000_3_OR_NEWER",
            "UNITY_6000",
            "UNITY_6000_OR_NEWER",
            "UNITY_2023_OR_NEWER",
            "UNITY_2022_OR_NEWER",
            "UNITY_2021_OR_NEWER"
        };

        static int Main()
        {
            string root = FindRepoRoot();
            if (!ProbeCatchesRequiredCodes(out string probeLine))
            {
                Console.Error.WriteLine(probeLine);
                return 1;
            }
            Console.WriteLine(probeLine);

            if (!UnityReferenceSet.Resolve())
            {
                Console.Error.WriteLine("script-compile-check unity-refs=absent");
                return 1;
            }

            var refs = new List<MetadataReference>();
            foreach (string dll in UnityReferenceSet.Assemblies)
                refs.Add(MetadataReference.CreateFromFile(dll));

            var playerFiles = new List<string>();
            AddCs(playerFiles, Path.Combine(root, "Assets", "Scripts"));
            AddCs(playerFiles, Path.Combine(root, "Assets", "TagArenaMovement", "Scripts"));

            var editorFiles = new List<string>();
            AddCs(editorFiles, Path.Combine(root, "Assets", "Editor"));
            // Unity compiles every script under Assets, not just these folders: a script
            // under any "Editor" folder goes to the editor assembly, the rest to the player.
            // (e.g. Assets/Art/Props/Library/Scripts/LibraryPropMeta.cs)
            AddLoose(root, playerFiles, editorFiles);
            var smokeFiles = editorFiles.Where(IsSmokeCheck).ToList();
            editorFiles.RemoveAll(IsSmokeCheck);

            string outDir = Path.Combine(Path.GetTempPath(), "tag-script-check-" + Environment.ProcessId);
            Directory.CreateDirectory(outDir);

            var errors = new List<string>();
            int parsed = 0;
            string playerDll = Path.Combine(outDir, "Assembly-CSharp.dll");
            parsed += Compile("Assembly-CSharp", playerFiles, refs, playerDll, root, errors);

            var nunit = MetadataReference.CreateFromFile(Environment.GetEnvironmentVariable("NUNIT_DLL") ?? "/tmp/nunit/lib/net45/nunit.framework.dll");
            var editorRefs = new List<MetadataReference>(refs);
            editorRefs.Add(nunit);
            var mscorlib = MetadataReference.CreateFromFile(Environment.GetEnvironmentVariable("MSCORLIB_DLL") ?? "/tmp/unity-6000/Editor/Data/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll");
            editorRefs.Add(mscorlib);
            if (File.Exists(playerDll))
                editorRefs.Add(MetadataReference.CreateFromFile(playerDll));
            string editorDll = Path.Combine(outDir, "Assembly-CSharp-Editor.dll");
            parsed += Compile("Assembly-CSharp-Editor", editorFiles, editorRefs, editorDll, root, errors);

            var testRefs = new List<MetadataReference>(editorRefs);
            if (File.Exists(editorDll))
                testRefs.Add(MetadataReference.CreateFromFile(editorDll));
            parsed += Compile("Tag.Editor.SmokeCheck", smokeFiles, testRefs, Path.Combine(outDir, "Tag.Editor.SmokeCheck.dll"), root, errors);

            var testFiles = new List<string>();
            AddCs(testFiles, Path.Combine(root, "Assets", "Tests"));
            var plainRefs = new List<MetadataReference>(refs);
            plainRefs.Add(nunit); plainRefs.Add(mscorlib);
            // Runtime asmdefs (e.g. Tag.World) are folded into the player compile here,
            // so the test assembly references the player dll to see them.
            if (File.Exists(playerDll)) plainRefs.Add(MetadataReference.CreateFromFile(playerDll));
            if (testFiles.Count > 0)
                parsed += Compile("Tag.Tests.EditMode", testFiles, plainRefs, Path.Combine(outDir, "Tag.Tests.EditMode.dll"), root, errors);
            Console.WriteLine("tests-asm files=" + testFiles.Count);
            errors.Sort(StringComparer.Ordinal);
            Console.WriteLine("script-compile-check files=" + (playerFiles.Count + editorFiles.Count + smokeFiles.Count)
                + " assemblies=3 parseErrors=0 cscErrors=" + errors.Count
                + " unity=" + UnityReferenceSet.Describe);
            foreach (string line in errors)
                Console.WriteLine(line);

            bool compileOk = errors.Count == 0;
            var watched = new List<string>();
            watched.AddRange(playerFiles);
            watched.AddRange(editorFiles);
            watched.AddRange(smokeFiles);
            bool smokeOk = SmokeFiles.Run(root, out string smokeLine, out string smokeReport);
            Console.WriteLine(smokeLine);
            if (!smokeOk)
                Console.Error.WriteLine(smokeReport);
            bool patternsOk = UnityCompilePatterns.Run(root, watched, out string patternLine, out string patternReport);
            Console.WriteLine(patternLine);
            if (!patternsOk)
                Console.Error.WriteLine(patternReport);
            bool layoutOk = AssetLayout.Run(root, out string layoutLine, out string layoutReport);
            Console.WriteLine(layoutLine);
            if (!layoutOk)
                Console.Error.WriteLine(layoutReport);
            if (!compileOk || !smokeOk || !patternsOk || !layoutOk)
                return 1;
            Console.WriteLine("script-compile-check ok csc CS1061 CS0117 CS0103 CS0104 unity=" + UnityReferenceSet.EditorVersion
                + " parsed=" + parsed);
            return 0;
        }

        static int Compile(string name, List<string> files, List<MetadataReference> refs, string dll, string root, List<string> errors)
        {
            var parse = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: Defines);
            var trees = new List<SyntaxTree>();
            foreach (string file in files)
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), parse, path: file);
                trees.Add(tree);
                foreach (Diagnostic d in tree.GetDiagnostics())
                {
                    if (d.Severity == DiagnosticSeverity.Error)
                        errors.Add(Format(root, d));
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                name,
                trees,
                refs,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    usings: ImmutableArray<string>.Empty,
                    allowUnsafe: false));

            if (File.Exists(dll)) File.Delete(dll);
            var emit = compilation.Emit(dll);
            if (!emit.Success && File.Exists(dll)) File.Delete(dll);
            foreach (Diagnostic d in emit.Diagnostics)
            {
                if (d.Severity != DiagnosticSeverity.Error) continue;
                string line = Format(root, d);
                if (!errors.Contains(line))
                    errors.Add(line);
            }
            return trees.Count;
        }

        /// <summary>
        /// A throwaway compilation must report the four codes. If csc stops
        /// emitting them, the game compile is not a real check.
        /// </summary>
        static bool ProbeCatchesRequiredCodes(out string line)
        {
            string source = @"
namespace AProbe { public class CompressionLevel { } public class Box { public int Only; } }
namespace BProbe { public class CompressionLevel { } }
namespace ProbeUse {
  using AProbe;
  using BProbe;
  class Use {
    void M() {
      var box = new Box();
      int missing = box.MissingMember;
      int type = Box.NoSuchMember;
      int name = notDeclared;
      CompressionLevel ambiguous;
    }
  }
}";
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            string corlib = typeof(object).Assembly.Location;
            CSharpCompilation compilation = CSharpCompilation.Create(
                "Probe",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(corlib) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: ImmutableArray<string>.Empty));
            var ids = new HashSet<string>();
            foreach (Diagnostic d in compilation.GetDiagnostics())
            {
                if (d.Severity == DiagnosticSeverity.Error)
                    ids.Add(d.Id);
            }
            var missing = new List<string>();
            foreach (string id in MustCatch)
            {
                if (!ids.Contains(id)) missing.Add(id);
            }
            if (missing.Count > 0)
            {
                line = "script-compile-check probe missed " + string.Join(" ", missing);
                return false;
            }
            line = "script-compile-check probe CS1061 CS0117 CS0103 CS0104";
            return true;
        }

        static bool IsSmokeCheck(string path)
        {
            return path.IndexOf(Path.DirectorySeparatorChar + "SmokeCheck" + Path.DirectorySeparatorChar, StringComparison.Ordinal) >= 0
                || path.IndexOf("/SmokeCheck/", StringComparison.Ordinal) >= 0;
        }

        static void AddLoose(string root, List<string> player, List<string> editor)
        {
            string assets = Path.Combine(root, "Assets");
            var known = new HashSet<string>(player.Concat(editor));
            string[] skip = { "Scripts", "Editor", "Tests" };
            foreach (string file in Directory.GetFiles(assets, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(assets.Length).TrimStart('/', '\\').Replace('\\', '/');
                string top = rel.Split('/')[0];
                if (Array.IndexOf(skip, top) >= 0 || rel.StartsWith("TagArenaMovement/Scripts/", StringComparison.Ordinal)) continue;
                if (known.Contains(file) || rel.Contains("/obj/")) continue;
                if (("/" + rel).Contains("/Editor/")) editor.Add(file); else player.Add(file);
            }
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
