using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Tag.Tools
{
    /// <summary>
    /// Unity 6000.3.24f1 reference assemblies taken from that editor's Managed
    /// folder (CoreModule and the other engine modules, UnityEditor modules,
    /// the netstandard 2.1 contract, and the template copies of Input System
    /// and URP). A NuGet UnityEngine redist directory can be passed with
    /// UNITY_REF_DIR when it contains UnityEngine.CoreModule.dll. The in-repo
    /// stub is not a reference.
    /// </summary>
    static class UnityReferenceSet
    {
        public const string EditorVersion = "6000.3.24f1";
        const string Changeset = "4e7b9b5b6244";
        const string CacheRoot = "/tmp/unity-6000";

        public static string Describe;
        public static List<string> Assemblies = new List<string>();

        public static bool Resolve()
        {
            Assemblies = new List<string>();
            string core = FindCoreModule();
            if (core == null)
            {
                FetchEditor();
                core = FindCoreModule();
            }
            if (core == null)
            {
                Describe = "absent";
                return false;
            }

            string engineDir = Path.GetDirectoryName(core);
            AddModules(engineDir);
            string managed = Path.GetDirectoryName(engineDir);
            string editorData = Path.GetDirectoryName(managed);
            string netstd = Path.Combine(editorData, "NetStandard", "ref", "2.1.0", "netstandard.dll");
            if (File.Exists(netstd))
                Assemblies.Add(netstd);
            else
                AddHostFramework();

            string packages = Path.Combine(
                editorData,
                "Resources", "PackageManager", "ProjectTemplates", "libcache",
                "com.unity.template.3d-cross-platform-17.0.14", "ScriptAssemblies");
            if (Directory.Exists(packages))
            {
                foreach (string dll in Directory.GetFiles(packages, "*.dll"))
                    Assemblies.Add(dll);
            }

            string flat = Environment.GetEnvironmentVariable("UNITY_REF_DIR");
            if (!string.IsNullOrEmpty(flat) && Directory.Exists(flat))
            {
                foreach (string dll in Directory.GetFiles(flat, "*.dll"))
                {
                    string name = Path.GetFileName(dll);
                    if (IsFacade(name)) continue;
                    if (!Assemblies.Exists(a => string.Equals(Path.GetFileName(a), name, StringComparison.OrdinalIgnoreCase)))
                        Assemblies.Add(dll);
                }
            }

            Describe = EditorVersion + " modules=" + Assemblies.Count;
            return Assemblies.Count > 0;
        }

        static void AddModules(string engineDir)
        {
            if (!Directory.Exists(engineDir)) return;
            foreach (string dll in Directory.GetFiles(engineDir, "*.dll"))
            {
                string name = Path.GetFileName(dll);
                if (IsFacade(name)) continue;
                if (name.StartsWith("UnityEngine.", StringComparison.Ordinal)
                    || name.StartsWith("UnityEditor.", StringComparison.Ordinal))
                    Assemblies.Add(dll);
            }
        }

        static bool IsFacade(string name)
        {
            return name.Equals("UnityEngine.dll", StringComparison.OrdinalIgnoreCase)
                || name.Equals("UnityEditor.dll", StringComparison.OrdinalIgnoreCase);
        }

        static string FindCoreModule()
        {
            var roots = new List<string>();
            string env = Environment.GetEnvironmentVariable("UNITY_REF_DIR");
            if (!string.IsNullOrEmpty(env)) roots.Add(env);
            roots.Add(CacheRoot);
            roots.Add("/opt/unity");
            roots.Add("/opt/Unity");
            roots.Add("/usr/lib/unity");
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            roots.Add(Path.Combine(home, "Unity"));
            roots.Add(Path.Combine(home, "Unity", "Hub", "Editor"));
            foreach (string dir in roots)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "UnityEngine.CoreModule.dll", SearchOption.AllDirectories))
                    return file;
            }
            return null;
        }

        static void AddHostFramework()
        {
            string list = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrEmpty(list)) return;
            foreach (string path in list.Split(Path.PathSeparator))
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith("System.", StringComparison.Ordinal)
                    || name == "System.Private.CoreLib.dll"
                    || name == "netstandard.dll"
                    || name == "mscorlib.dll")
                    Assemblies.Add(path);
            }
        }

        static void FetchEditor()
        {
            string tarball = "/tmp/Unity-" + EditorVersion + ".tar.xz";
            string url = "https://download.unity3d.com/download_unity/" + Changeset
                + "/LinuxEditorInstaller/Unity-" + EditorVersion + ".tar.xz";
            Console.WriteLine("script-compile-check fetching Unity " + EditorVersion + " reference assemblies");
            if (!File.Exists(tarball) || new FileInfo(tarball).Length < 1000000000L)
                Download(url, tarball).GetAwaiter().GetResult();
            Directory.CreateDirectory(CacheRoot);
            var tar = Process.Start(new ProcessStartInfo
            {
                FileName = "tar",
                ArgumentList =
                {
                    "-xJf", tarball,
                    "-C", CacheRoot,
                    "Editor/Data/Managed",
                    "Editor/Data/NetStandard",
                    "Editor/Data/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies"
                },
                UseShellExecute = false
            });
            tar.WaitForExit();
            if (tar.ExitCode != 0)
                Console.Error.WriteLine("script-compile-check tar exit " + tar.ExitCode);
        }

        static async Task Download(string url, string dest)
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(30);
            using var head = new HttpRequestMessage(HttpMethod.Head, url);
            using HttpResponseMessage probe = await http.SendAsync(head);
            probe.EnsureSuccessStatusCode();
            long total = probe.Content.Headers.ContentLength ?? 0;
            if (total <= 0) throw new InvalidOperationException("Unity editor download has no length");
            const int parts = 8;
            long chunk = (total + parts - 1) / parts;
            var tasks = new List<Task>();
            for (int i = 0; i < parts; i++)
            {
                long start = i * chunk;
                if (start >= total) break;
                long end = Math.Min(total - 1, start + chunk - 1);
                string part = dest + ".part" + i;
                tasks.Add(DownloadRange(http, url, part, start, end));
            }
            await Task.WhenAll(tasks);
            using (var w = new FileStream(dest, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    string part = dest + ".part" + i;
                    using var r = new FileStream(part, FileMode.Open, FileAccess.Read);
                    await r.CopyToAsync(w);
                    r.Close();
                    File.Delete(part);
                }
            }
        }

        static async Task DownloadRange(HttpClient http, string url, string part, long start, long end)
        {
            long need = end - start + 1;
            if (File.Exists(part) && new FileInfo(part).Length == need) return;
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
            using HttpResponseMessage res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            res.EnsureSuccessStatusCode();
            using Stream src = await res.Content.ReadAsStreamAsync();
            using var dst = new FileStream(part, FileMode.Create, FileAccess.Write);
            await src.CopyToAsync(dst);
            if (new FileInfo(part).Length != need)
                throw new InvalidOperationException("short read " + part);
        }
    }
}
