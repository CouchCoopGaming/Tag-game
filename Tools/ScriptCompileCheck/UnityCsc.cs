using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tag.Tools
{
    /// <summary>
    /// Real csc (or mcs) compile of every game script and every editor test,
    /// referenced against the UnityEngine module DLLs. The stub compile filtered
    /// CS0103 unless the missing name was a declared type, so a typo such as
    /// _hookAge passed. This does not filter diagnostics.
    /// </summary>
    static class UnityCsc
    {
        public static bool Run(string root, out string line, out string report)
        {
            var log = new StringBuilder();
            if (!FindCompiler(out string compiler, out string compilerKind, out string compilerNote))
            {
                line = "script-csc compiler=missing";
                report = compilerNote;
                return false;
            }
            if (!FindUnity(out string editor, out string unityNote))
            {
                line = "script-csc unity-refs=absent compiler=" + compilerKind;
                report = unityNote;
                return false;
            }

            var refs = new List<string>();
            AddDlls(refs, Path.Combine(editor, "Data", "Managed"));
            AddDlls(refs, Path.Combine(editor, "Data", "Managed", "UnityEngine"));
            AddBcl(refs, editor);
            AddPackageDlls(refs, root, editor);
            refs = Unique(refs);
            if (!HasCore(refs))
            {
                line = "script-csc unity-refs=absent compiler=" + compilerKind;
                report = "UnityEngine.CoreModule.dll was not next to " + editor;
                return false;
            }

            string outDir = Path.Combine(Path.GetTempPath(), "tag-script-csc");
            Directory.CreateDirectory(outDir);
            string playerDll = Path.Combine(outDir, "Assembly-CSharp.dll");
            string editorDll = Path.Combine(outDir, "Assembly-CSharp-Editor.dll");

            var player = new List<string>();
            AddCs(player, Path.Combine(root, "Assets", "Scripts"), false);
            AddCs(player, Path.Combine(root, "Assets", "TagArenaMovement", "Scripts"), false);

            var editorFiles = new List<string>();
            AddCs(editorFiles, Path.Combine(root, "Assets"), true);

            string playerRsp = Path.Combine(outDir, "player.rsp");
            string editorRsp = Path.Combine(outDir, "editor.rsp");
            WriteRsp(playerRsp, playerDll, refs, player, false);
            var editorRefs = new List<string>(refs);
            editorRefs.Add(playerDll);
            WriteRsp(editorRsp, editorDll, editorRefs, editorFiles, true);

            int playerErrors = Invoke(compiler, playerRsp, root, log, out int playerFiles);
            int editorErrors = playerErrors == 0
                ? Invoke(compiler, editorRsp, root, log, out int editorCount)
                : -1;
            if (playerErrors != 0)
                editorErrors = -1;

            int shownEditor = editorErrors < 0 ? 0 : editorErrors;
            line = "script-csc compiler=" + compilerKind
                + " unity=" + editor
                + " playerFiles=" + player.Count
                + " editorFiles=" + editorFiles.Count
                + " refs=" + refs.Count
                + " playerErrors=" + playerErrors
                + " editorErrors=" + (editorErrors < 0 ? "skipped" : shownEditor.ToString());
            log.Insert(0, compilerNote + "\n" + unityNote + "\n");
            report = log.ToString();
            return playerErrors == 0 && editorErrors == 0;
        }

        static bool FindCompiler(out string compiler, out string kind, out string note)
        {
            compiler = "";
            kind = "";
            note = "";
            string dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrEmpty(dotnetRoot))
                dotnetRoot = "/usr/lib/dotnet";
            string sdk = Path.Combine(dotnetRoot, "sdk");
            if (Directory.Exists(sdk))
            {
                string[] hits = Directory.GetFiles(sdk, "csc.dll", SearchOption.AllDirectories);
                Array.Sort(hits, StringComparer.Ordinal);
                if (hits.Length > 0)
                {
                    compiler = hits[hits.Length - 1];
                    kind = "csc";
                    note = "csc " + compiler;
                    return true;
                }
            }
            string mcs = Which("mcs");
            if (!string.IsNullOrEmpty(mcs))
            {
                compiler = mcs;
                kind = "mcs";
                note = "mcs " + mcs;
                return true;
            }
            note = "Neither Roslyn csc.dll nor mcs was on this machine.";
            return false;
        }

        static bool FindUnity(out string editorDataParent, out string note)
        {
            editorDataParent = "";
            var roots = new List<string>();
            string env = Environment.GetEnvironmentVariable("UNITY_EDITOR");
            if (!string.IsNullOrEmpty(env))
                roots.Add(env);
            roots.Add("/opt/unity/6000.3.24f1/Editor");
            roots.Add("/opt/unity/Editor");
            roots.Add("/opt/Unity/Editor");
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            roots.Add(Path.Combine(home, "Unity", "Hub", "Editor"));
            string program = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(program))
                roots.Add(Path.Combine(program, "Unity", "Hub", "Editor"));

            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                    continue;
                string direct = CoreIn(root);
                if (direct != null)
                {
                    editorDataParent = direct;
                    note = "unity " + direct;
                    return true;
                }
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string nested = CoreIn(Path.Combine(dir, "Editor"));
                    if (nested == null)
                        nested = CoreIn(dir);
                    if (nested != null)
                    {
                        editorDataParent = nested;
                        note = "unity " + nested;
                        return true;
                    }
                }
            }
            note = "UnityEngine reference DLLs were not found. Set UNITY_EDITOR to the Editor directory that contains Data/Managed/UnityEngine/UnityEngine.CoreModule.dll.";
            return false;
        }

        static string CoreIn(string editorRoot)
        {
            string core = Path.Combine(editorRoot, "Data", "Managed", "UnityEngine", "UnityEngine.CoreModule.dll");
            if (File.Exists(core))
                return editorRoot;
            return null;
        }

        static bool HasCore(List<string> refs)
        {
            for (int i = 0; i < refs.Count; i++)
            {
                if (string.Equals(Path.GetFileName(refs[i]), "UnityEngine.CoreModule.dll", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        static void AddDlls(List<string> refs, string dir)
        {
            if (!Directory.Exists(dir))
                return;
            foreach (string file in Directory.GetFiles(dir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                if (name.Equals("UnityEngine.dll", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("UnityEditor.dll", StringComparison.OrdinalIgnoreCase))
                    continue;
                refs.Add(file);
            }
        }

        static void AddBcl(List<string> refs, string editorRoot)
        {
            string data = Path.Combine(editorRoot, "Data");
            string netstd = Path.Combine(data, "NetStandard", "ref", "2.1.0", "netstandard.dll");
            if (File.Exists(netstd))
                refs.Add(netstd);
            string csharp = Path.Combine(data, "NetStandard", "EditorExtensions", "Microsoft.CSharp.dll");
            if (File.Exists(csharp))
                refs.Add(csharp);
            // nunit.framework.dll inherits Attribute from mscorlib 4.0. This shim
            // type-forwards that identity onto netstandard, so editor tests compile.
            string mscorlib = Path.Combine(data, "NetStandard", "compat", "2.1.0", "shims", "netfx", "mscorlib.dll");
            if (File.Exists(mscorlib))
                refs.Add(mscorlib);
        }

        static void AddPackageDlls(List<string> refs, string root, string editorRoot)
        {
            var dirs = new List<string>();
            dirs.Add(Path.Combine(root, "Library", "ScriptAssemblies"));
            string templates = Path.Combine(editorRoot, "Data", "Resources", "PackageManager", "ProjectTemplates", "libcache");
            if (Directory.Exists(templates))
            {
                string[] packs = Directory.GetDirectories(templates);
                Array.Sort(packs, StringComparer.Ordinal);
                for (int i = 0; i < packs.Length; i++)
                {
                    string name = Path.GetFileName(packs[i]);
                    if (name.IndexOf("3d-cross-platform", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    dirs.Add(Path.Combine(packs[i], "ScriptAssemblies"));
                }
                for (int i = 0; i < packs.Length; i++)
                {
                    string name = Path.GetFileName(packs[i]);
                    if (name.IndexOf("3d-cross-platform", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    dirs.Add(Path.Combine(packs[i], "ScriptAssemblies"));
                }
            }
            dirs.Add("/opt/unity/refs");
            dirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Unity", "refs"));
            dirs.Add(Path.Combine(editorRoot, "Data", "Resources", "PackageManager", "BuiltInPackages"));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < dirs.Count; i++)
            {
                if (!Directory.Exists(dirs[i]))
                    continue;
                bool scriptAssemblies = dirs[i].EndsWith("ScriptAssemblies", StringComparison.OrdinalIgnoreCase);
                foreach (string file in Directory.GetFiles(dirs[i], "*.dll", scriptAssemblies ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(file);
                    if (!WantPackage(name))
                        continue;
                    if (!seen.Add(name))
                        continue;
                    refs.Add(file);
                }
            }
        }

        static bool WantPackage(string name)
        {
            if (name.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                return false;
            if (name.IndexOf("nunit.framework", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (name.StartsWith("Unity.InputSystem", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.RenderPipelines.", StringComparison.Ordinal)
                || name.StartsWith("Unity.RenderPipeline.", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.Mathematics", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.Collections", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.Burst", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("UnityEngine.UI", StringComparison.Ordinal)
                || name.StartsWith("UnityEditor.UI", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.TextMeshPro", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("UnityEngine.TestRunner", StringComparison.Ordinal)
                || name.StartsWith("UnityEditor.TestRunner", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.InternalAPI", StringComparison.Ordinal))
                return true;
            if (name.StartsWith("Unity.ShaderGraph", StringComparison.Ordinal))
                return true;
            return false;
        }

        static List<string> Unique(List<string> refs)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            for (int i = 0; i < refs.Count; i++)
            {
                string full = Path.GetFullPath(refs[i]);
                if (seen.Add(full))
                    list.Add(full);
            }
            return list;
        }

        static void AddCs(List<string> files, string dir, bool editorOnly)
        {
            if (!Directory.Exists(dir))
                return;
            foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string norm = file.Replace('\\', '/');
                if (norm.IndexOf("/obj/", StringComparison.Ordinal) >= 0)
                    continue;
                if (norm.IndexOf("/bin/", StringComparison.Ordinal) >= 0)
                    continue;
                bool editor = norm.IndexOf("/Editor/", StringComparison.Ordinal) >= 0;
                if (editorOnly)
                {
                    if (editor)
                        files.Add(file);
                }
                else if (!editor)
                    files.Add(file);
            }
        }

        static void WriteRsp(string path, string outDll, List<string> refs, List<string> files, bool editor)
        {
            var text = new StringBuilder();
            text.Append("-target:library\n");
            text.Append("-nologo\n");
            text.Append("-nostdlib+\n");
            text.Append("-langversion:9.0\n");
            text.Append("-nullable:disable\n");
            text.Append("-unsafe-\n");
            text.Append("-define:");
            text.Append(Defines(editor));
            text.Append('\n');
            text.Append("-out:");
            text.Append(Quote(outDll));
            text.Append('\n');
            for (int i = 0; i < refs.Count; i++)
            {
                text.Append("-r:");
                text.Append(Quote(refs[i]));
                text.Append('\n');
            }
            for (int i = 0; i < files.Count; i++)
            {
                text.Append(Quote(files[i]));
                text.Append('\n');
            }
            File.WriteAllText(path, text.ToString());
        }

        static string Defines(bool editor)
        {
            var d = new List<string>
            {
                "UNITY_6000_3_24", "UNITY_6000_3", "UNITY_6000",
                "UNITY_5_3_OR_NEWER", "UNITY_5_4_OR_NEWER", "UNITY_5_5_OR_NEWER", "UNITY_5_6_OR_NEWER",
                "UNITY_2017_1_OR_NEWER", "UNITY_2017_2_OR_NEWER", "UNITY_2017_3_OR_NEWER", "UNITY_2017_4_OR_NEWER",
                "UNITY_2018_1_OR_NEWER", "UNITY_2018_2_OR_NEWER", "UNITY_2018_3_OR_NEWER", "UNITY_2018_4_OR_NEWER",
                "UNITY_2019_1_OR_NEWER", "UNITY_2019_2_OR_NEWER", "UNITY_2019_3_OR_NEWER", "UNITY_2019_4_OR_NEWER",
                "UNITY_2020_1_OR_NEWER", "UNITY_2020_2_OR_NEWER", "UNITY_2020_3_OR_NEWER",
                "UNITY_2021_1_OR_NEWER", "UNITY_2021_2_OR_NEWER", "UNITY_2021_3_OR_NEWER",
                "UNITY_2022_1_OR_NEWER", "UNITY_2022_2_OR_NEWER", "UNITY_2022_3_OR_NEWER",
                "UNITY_2023_1_OR_NEWER", "UNITY_2023_2_OR_NEWER", "UNITY_2023_3_OR_NEWER",
                "UNITY_6000_0_OR_NEWER", "UNITY_6000_1_OR_NEWER", "UNITY_6000_2_OR_NEWER", "UNITY_6000_3_OR_NEWER",
                "ENABLE_MONO", "ENABLE_INPUT_SYSTEM", "ENABLE_LEGACY_INPUT_MANAGER",
                "ENABLE_PHYSICS", "ENABLE_AUDIO", "ENABLE_VIDEO",
                "NET_STANDARD", "NET_STANDARD_2_0", "NET_STANDARD_2_1",
                "UNITY_ASSERTIONS", "DEBUG", "TRACE",
                "CSHARP_7_OR_LATER", "CSHARP_7_3_OR_NEWER"
            };
            if (editor)
            {
                d.Add("UNITY_EDITOR");
                d.Add("UNITY_EDITOR_LINUX");
                d.Add("UNITY_INCLUDE_TESTS");
            }
            return string.Join(";", d);
        }

        static int Invoke(string compiler, string rsp, string root, StringBuilder log, out int fileCount)
        {
            fileCount = 0;
            string file = File.ReadAllText(rsp);
            int lines = 0;
            for (int i = 0; i < file.Length; i++)
            {
                if (file[i] == '\n')
                    lines++;
            }
            fileCount = lines;
            var psi = new ProcessStartInfo();
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            if (compiler.EndsWith("csc.dll", StringComparison.OrdinalIgnoreCase))
            {
                psi.FileName = "dotnet";
                psi.ArgumentList.Add("exec");
                psi.ArgumentList.Add(compiler);
                psi.ArgumentList.Add("/utf8output");
                psi.ArgumentList.Add("@" + rsp);
            }
            else
            {
                psi.FileName = compiler;
                psi.ArgumentList.Add("-sdk:4.5");
                psi.ArgumentList.Add("@" + rsp);
            }
            using (Process proc = Process.Start(psi))
            {
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                string combined = stdout + stderr;
                int errors = 0;
                int shown = 0;
                string[] rows = combined.Split('\n');
                for (int i = 0; i < rows.Length; i++)
                {
                    string row = rows[i].TrimEnd('\r');
                    if (row.Length == 0)
                        continue;
                    if (row.IndexOf("error CS", StringComparison.Ordinal) >= 0 || row.IndexOf(": error ", StringComparison.Ordinal) >= 0)
                    {
                        errors++;
                        if (shown < 80)
                        {
                            log.AppendLine(Shorten(root, row));
                            shown++;
                        }
                    }
                }
                if (errors == 0 && proc.ExitCode != 0)
                {
                    errors = 1;
                    log.AppendLine("csc exit " + proc.ExitCode);
                    if (combined.Length > 2000)
                        combined = combined.Substring(0, 2000);
                    log.AppendLine(combined);
                }
                if (errors > shown)
                    log.AppendLine("... " + (errors - shown) + " more csc errors");
                return errors;
            }
        }

        static string Shorten(string root, string row)
        {
            if (!string.IsNullOrEmpty(root) && row.StartsWith(root, StringComparison.Ordinal))
                return row.Substring(root.Length).TrimStart('/', '\\');
            return row;
        }

        static string Quote(string path)
        {
            if (path.IndexOf(' ') < 0 && path.IndexOf('"') < 0)
                return path;
            return "\"" + path.Replace("\"", "\\\"") + "\"";
        }

        static string Which(string name)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                return "";
            string[] parts = path.Split(Path.PathSeparator);
            for (int i = 0; i < parts.Length; i++)
            {
                string file = Path.Combine(parts[i], name);
                if (File.Exists(file))
                    return file;
            }
            return "";
        }
    }
}
