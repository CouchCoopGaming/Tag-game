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
    /// Static import check for the Unity project. The editor is not on this machine.
    /// Package assets that ship with a manifest dependency are resolved from a
    /// small catalog (the URP Lit shader and the material version script).
    /// </summary>
    static class Program
    {
        const string PlayScene = "Assets/Scenes/Play.unity";

        // com.unity.render-pipelines.universal, referenced by project materials.
        static readonly PackageAsset[] PackageAssets =
        {
            new PackageAsset(
                "933532a4fcc9baf4fa0491de14d08ed7",
                "shader",
                "Universal Render Pipeline/Lit",
                4800000,
                "com.unity.render-pipelines.universal"),
            new PackageAsset(
                "d0353a89b1f911e48b9e16bdc9f2e058",
                "script",
                "UnityEditor.Rendering.Universal.AssetVersion",
                11500000,
                "com.unity.render-pipelines.universal"),
        };

        static readonly Regex GuidLine = new Regex(@"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Multiline);
        static readonly Regex MainId = new Regex(@"mainObjectFileID:\s*(-?\d+)");
        static readonly Regex Anchor = new Regex(@"&(-?\d+)\b");
        static readonly Regex ExternalRef = new Regex(
            @"\{fileID:\s*(-?\d+)\s*,\s*guid:\s*([0-9a-fA-F]{32})\s*,\s*type:\s*\d+\s*\}");
        static readonly Regex ShaderDecl = new Regex(@"Shader\s+""([^""]+)""");

        static int Main()
        {
            string root = FindRepoRoot();
            var fails = new List<string>();
            string assets = Path.Combine(root, "Assets");
            var metas = new Dictionary<string, Meta>(StringComparer.OrdinalIgnoreCase);
            int fileCount = 0;
            int metaCount = 0;

            if (!Directory.Exists(assets))
            {
                Console.Error.WriteLine("FAIL Assets/ missing");
                return 1;
            }

            foreach (string dir in Directory.EnumerateDirectories(assets, "*", SearchOption.AllDirectories))
            {
                if (Skip(dir)) continue;
                fileCount++;
                string metaPath = dir + ".meta";
                if (!File.Exists(metaPath))
                    fails.Add("missing meta " + Rel(root, dir));
            }

            foreach (string file in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
            {
                if (Skip(file)) continue;
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    metaCount++;
                    string assetPath = file.Substring(0, file.Length - 5);
                    if (!File.Exists(assetPath) && !Directory.Exists(assetPath))
                        fails.Add("orphan meta " + Rel(root, file));
                    continue;
                }

                fileCount++;
                string metaPath = file + ".meta";
                if (!File.Exists(metaPath))
                {
                    fails.Add("missing meta " + Rel(root, file));
                    continue;
                }
            }

            foreach (string metaPath in Directory.EnumerateFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(metaPath);
                Match g = GuidLine.Match(text);
                if (!g.Success)
                {
                    fails.Add("meta has no guid " + Rel(root, metaPath));
                    continue;
                }

                string guid = g.Groups[1].Value.ToLowerInvariant();
                string assetPath = metaPath.Substring(0, metaPath.Length - 5);
                var meta = new Meta
                {
                    Guid = guid,
                    MetaPath = metaPath,
                    AssetPath = assetPath,
                    Text = text,
                };
                if (metas.ContainsKey(guid))
                    fails.Add("duplicate guid " + guid + " " + Rel(root, metas[guid].MetaPath) + " and " + Rel(root, metaPath));
                else
                    metas.Add(guid, meta);
            }

            string manifest = File.Exists(Path.Combine(root, "Packages", "manifest.json"))
                ? File.ReadAllText(Path.Combine(root, "Packages", "manifest.json"))
                : "";
            var packages = new Dictionary<string, PackageAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (PackageAsset pkg in PackageAssets)
            {
                if (manifest.IndexOf(pkg.PackageId, StringComparison.Ordinal) >= 0)
                    packages[pkg.Guid] = pkg;
            }

            int refs = 0;
            int scriptRefs = 0;
            var shaderNames = new List<string>();
            foreach (string file in YamlDocs(assets))
            {
                string text = File.ReadAllText(file);
                string rel = Rel(root, file);
                foreach (Match m in ExternalRef.Matches(text))
                {
                    long fileId = long.Parse(m.Groups[1].Value);
                    string guid = m.Groups[2].Value.ToLowerInvariant();
                    if (fileId == 0 || IsNullGuid(guid)) continue;
                    refs++;
                    bool scriptSlot = IsScriptSlot(text, m.Index);
                    if (scriptSlot && (rel.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                        || rel.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                    {
                        scriptRefs++;
                        if (!ScriptResolves(guid, fileId, metas, packages, out string why))
                            fails.Add("missing script " + rel + " guid " + guid + " " + why);
                    }

                    if (!RefResolves(guid, fileId, metas, packages, out string reason))
                        fails.Add("unresolved ref " + rel + " fileID " + fileId + " guid " + guid + " " + reason);
                }

                if (rel.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Match m in ExternalRef.Matches(text))
                    {
                        int at = m.Index;
                        int line = text.LastIndexOf('\n', Math.Max(0, at - 1));
                        string row = text.Substring(line + 1, at - line);
                        if (row.IndexOf("m_Shader", StringComparison.Ordinal) < 0) continue;
                        long fileId = long.Parse(m.Groups[1].Value);
                        string guid = m.Groups[2].Value.ToLowerInvariant();
                        if (!ShaderName(guid, fileId, metas, packages, out string shader, out string why))
                            fails.Add("shader ref " + rel + " " + why);
                        else
                            shaderNames.Add(shader + " @ " + rel);
                    }
                }
            }

            CheckBehaviours(root, assets, fails);
            string pipeline = Pipeline(root, metas, fails);
            CheckShaders(pipeline, packages, shaderNames, fails);
            CheckPlayScene(root, metas, fails);

            fails.Sort(StringComparer.Ordinal);
            foreach (string line in fails)
                Console.Error.WriteLine("FAIL " + line);

            string status = fails.Count == 0 ? "ok" : "fail";
            Console.WriteLine(
                "unity-asset-audit " + status
                + " files=" + fileCount
                + " metas=" + metaCount
                + " guids=" + metas.Count
                + " refs=" + refs
                + " scripts=" + scriptRefs
                + " shaders=" + shaderNames.Count
                + " playFirst=" + (PlayIsFirst(root) ? "1" : "0")
                + " urp=" + (pipeline == "urp" ? "assigned" : pipeline)
                + " issues=" + fails.Count);
            return fails.Count == 0 ? 0 : 1;
        }

        static void CheckBehaviours(string root, string assets, List<string> fails)
        {
            var decls = new List<Decl>();
            foreach (string file in Directory.EnumerateFiles(assets, "*.cs", SearchOption.AllDirectories))
            {
                if (Skip(file)) continue;
                string text = File.ReadAllText(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text, path: file);
                foreach (Diagnostic d in tree.GetDiagnostics())
                {
                    if (d.Severity == DiagnosticSeverity.Error)
                        fails.Add("parse " + Rel(root, file) + " " + d.GetMessage());
                }

                foreach (BaseTypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    decls.Add(new Decl
                    {
                        Key = QualName(type),
                        Simple = type.Identifier.ValueText,
                        FileName = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        Partial = type.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                        Nested = type.Parent is BaseTypeDeclarationSyntax,
                        Bases = type is TypeDeclarationSyntax typed ? BaseNames(typed) : new List<string>(),
                        Ns = NamespaceOf(type),
                    });
                }

                foreach (DelegateDeclarationSyntax del in tree.GetRoot().DescendantNodes().OfType<DelegateDeclarationSyntax>())
                {
                    decls.Add(new Decl
                    {
                        Key = NamespaceOf(del) + "." + del.Identifier.ValueText,
                        Simple = del.Identifier.ValueText,
                        FileName = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        Partial = false,
                        Nested = false,
                        Bases = new List<string>(),
                        Ns = NamespaceOf(del),
                    });
                }
            }

            var bySimple = new Dictionary<string, List<Decl>>(StringComparer.Ordinal);
            foreach (Decl d in decls)
            {
                if (!bySimple.TryGetValue(d.Simple, out List<Decl> list))
                {
                    list = new List<Decl>();
                    bySimple.Add(d.Simple, list);
                }
                list.Add(d);
            }

            var behaviour = new HashSet<string>(StringComparer.Ordinal);
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (Decl d in decls)
                {
                    if (behaviour.Contains(d.Key)) continue;
                    if (!IsBehaviour(d, bySimple, behaviour)) continue;
                    behaviour.Add(d.Key);
                    grew = true;
                }
            }

            foreach (Decl d in decls)
            {
                if (!behaviour.Contains(d.Key)) continue;
                if (d.Nested || d.Simple != d.FileName)
                    fails.Add("script name " + Rel(root, d.Path) + " class " + d.Simple + " file " + d.FileName);
            }

            var groups = new Dictionary<string, List<Decl>>(StringComparer.Ordinal);
            foreach (Decl d in decls)
            {
                if (!groups.TryGetValue(d.Key, out List<Decl> list))
                {
                    list = new List<Decl>();
                    groups.Add(d.Key, list);
                }
                list.Add(d);
            }

            foreach (KeyValuePair<string, List<Decl>> pair in groups)
            {
                if (pair.Value.Count < 2) continue;
                bool allPartial = true;
                var files = new List<string>();
                foreach (Decl d in pair.Value)
                {
                    if (!d.Partial) allPartial = false;
                    string rel = Rel(root, d.Path);
                    if (!files.Contains(rel)) files.Add(rel);
                }
                if (allPartial) continue;
                if (files.Count < 2) continue;
                fails.Add("duplicate type " + pair.Key + " in " + string.Join(", ", files));
            }
        }

        static bool IsBehaviour(Decl d, Dictionary<string, List<Decl>> bySimple, HashSet<string> known)
        {
            if (d.Bases == null) return false;
            foreach (string name in d.Bases)
            {
                if (name == "MonoBehaviour" || name == "ScriptableObject") return true;
                if (!bySimple.TryGetValue(name, out List<Decl> cands)) continue;
                foreach (Decl c in cands)
                {
                    if (known.Contains(c.Key)) return true;
                }
            }
            return false;
        }

        static List<string> BaseNames(TypeDeclarationSyntax type)
        {
            var names = new List<string>();
            if (type.BaseList == null) return names;
            foreach (BaseTypeSyntax b in type.BaseList.Types)
            {
                string text = b.Type.ToString();
                int angle = text.IndexOf('<');
                if (angle >= 0) text = text.Substring(0, angle);
                int dot = text.LastIndexOf('.');
                if (dot >= 0) text = text.Substring(dot + 1);
                names.Add(text.Trim());
            }
            return names;
        }

        static string QualName(BaseTypeDeclarationSyntax type)
        {
            var parts = new List<string> { type.Identifier.ValueText };
            SyntaxNode p = type.Parent;
            while (p != null)
            {
                if (p is BaseTypeDeclarationSyntax parent)
                    parts.Add(parent.Identifier.ValueText);
                else if (p is BaseNamespaceDeclarationSyntax ns)
                    parts.Add(ns.Name.ToString());
                p = p.Parent;
            }
            parts.Reverse();
            return string.Join(".", parts);
        }

        static string NamespaceOf(SyntaxNode node)
        {
            var parts = new List<string>();
            SyntaxNode p = node.Parent;
            while (p != null)
            {
                if (p is BaseNamespaceDeclarationSyntax ns)
                    parts.Add(ns.Name.ToString());
                p = p.Parent;
            }
            parts.Reverse();
            return parts.Count == 0 ? "" : string.Join(".", parts);
        }

        static void CheckShaders(string pipeline, Dictionary<string, PackageAsset> packages, List<string> shaderNames, List<string> fails)
        {
            var allowed = new HashSet<string>(StringComparer.Ordinal);
            if (pipeline == "urp")
            {
                foreach (PackageAsset pkg in packages.Values)
                {
                    if (pkg.Kind == "shader")
                        allowed.Add(pkg.Name);
                }
            }
            else
            {
                allowed.Add("Standard");
                allowed.Add("Diffuse");
                allowed.Add("Sprites/Default");
                allowed.Add("Unlit/Color");
                allowed.Add("Unlit/Texture");
                allowed.Add("Skybox/Procedural");
            }

            foreach (string row in shaderNames)
            {
                int at = row.IndexOf(" @ ", StringComparison.Ordinal);
                string name = at < 0 ? row : row.Substring(0, at);
                string where = at < 0 ? row : row.Substring(at + 3);
                if (pipeline == "urp" && name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) && allowed.Contains(name))
                    continue;
                if (allowed.Contains(name))
                    continue;
                // Project shaders are added by ShaderName when the guid is a local .shader.
                if (name.StartsWith("project:", StringComparison.Ordinal))
                    continue;
                fails.Add("shader not in " + pipeline + " pipeline " + name + " " + where);
            }
        }

        static bool ShaderName(string guid, long fileId, Dictionary<string, Meta> metas, Dictionary<string, PackageAsset> packages, out string name, out string why)
        {
            name = "";
            if (packages.TryGetValue(guid, out PackageAsset pkg))
            {
                if (pkg.Kind != "shader")
                {
                    why = "guid is not a shader";
                    return false;
                }
                if (fileId != pkg.FileId)
                {
                    why = "fileID " + fileId + " is not " + pkg.FileId;
                    return false;
                }
                name = pkg.Name;
                why = "";
                return true;
            }

            if (!metas.TryGetValue(guid, out Meta meta) || !File.Exists(meta.AssetPath))
            {
                why = "guid not in project";
                return false;
            }

            string ext = Path.GetExtension(meta.AssetPath);
            if (!ext.Equals(".shader", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".shadergraph", StringComparison.OrdinalIgnoreCase))
            {
                why = "guid is " + ext;
                return false;
            }

            if (ext.Equals(".shader", StringComparison.OrdinalIgnoreCase))
            {
                if (fileId != 4800000 && !HasFileId(meta, fileId))
                {
                    why = "fileID " + fileId + " is not the shader";
                    return false;
                }
                string body = File.ReadAllText(meta.AssetPath);
                Match m = ShaderDecl.Match(body);
                name = m.Success ? "project:" + m.Groups[1].Value : "project:" + Path.GetFileNameWithoutExtension(meta.AssetPath);
                why = "";
                return true;
            }

            name = "project:" + Path.GetFileNameWithoutExtension(meta.AssetPath);
            why = "";
            return true;
        }

        static void CheckPlayScene(string root, Dictionary<string, Meta> metas, List<string> fails)
        {
            string buildPath = Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset");
            if (!File.Exists(buildPath))
            {
                fails.Add("EditorBuildSettings missing");
                return;
            }

            string build = File.ReadAllText(buildPath);
            var scenes = new List<SceneEntry>();
            string[] lines = build.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim().TrimStart('-').Trim();
                if (!line.StartsWith("enabled:", StringComparison.Ordinal)) continue;
                bool enabled = line.EndsWith("1", StringComparison.Ordinal);
                string path = "";
                string guid = "";
                if (i + 1 < lines.Length)
                {
                    string p = lines[i + 1].Trim();
                    if (p.StartsWith("path:", StringComparison.Ordinal))
                        path = p.Substring(5).Trim();
                }
                if (i + 2 < lines.Length)
                {
                    string g = lines[i + 2].Trim();
                    if (g.StartsWith("guid:", StringComparison.Ordinal))
                        guid = g.Substring(5).Trim().ToLowerInvariant();
                }
                scenes.Add(new SceneEntry { Enabled = enabled, Path = path, Guid = guid });
            }

            int play = -1;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].Path == PlayScene) play = i;
            }
            if (play < 0)
                fails.Add("Play scene is not in EditorBuildSettings");
            else
            {
                if (play != 0 || !scenes[0].Enabled)
                    fails.Add("Play scene is not the first enabled build scene");
                string sceneMeta = Path.Combine(root, PlayScene) + ".meta";
                string sceneGuid = "";
                if (File.Exists(sceneMeta))
                {
                    Match m = GuidLine.Match(File.ReadAllText(sceneMeta));
                    if (m.Success) sceneGuid = m.Groups[1].Value.ToLowerInvariant();
                }
                if (sceneGuid.Length == 0 || scenes[play].Guid != sceneGuid)
                    fails.Add("Play scene guid in EditorBuildSettings does not match the scene meta");
            }
        }

        static bool PlayIsFirst(string root)
        {
            string buildPath = Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset");
            if (!File.Exists(buildPath)) return false;
            string build = File.ReadAllText(buildPath);
            int play = build.IndexOf(PlayScene, StringComparison.Ordinal);
            int boot = build.IndexOf("Assets/Scenes/Boot.unity", StringComparison.Ordinal);
            if (play < 0) return false;
            if (boot >= 0 && boot < play) return false;
            return true;
        }

        static string Pipeline(string root, Dictionary<string, Meta> metas, List<string> fails)
        {
            string manifestPath = Path.Combine(root, "Packages", "manifest.json");
            bool urpPackage = File.Exists(manifestPath)
                && File.ReadAllText(manifestPath).IndexOf("com.unity.render-pipelines.universal", StringComparison.Ordinal) >= 0;
            string graphics = Read(Path.Combine(root, "ProjectSettings", "GraphicsSettings.asset"));
            string quality = Read(Path.Combine(root, "ProjectSettings", "QualitySettings.asset"));
            string graphicsGuid = PipelineGuid(graphics, "m_CustomRenderPipeline");
            bool graphicsUrp = IsUrpAsset(graphicsGuid, metas);
            var qualityGuids = new List<string>();
            if (quality != null)
            {
                foreach (Match m in Regex.Matches(quality, @"customRenderPipeline:\s*\{fileID:\s*(-?\d+)\s*,\s*guid:\s*([0-9a-fA-F]{32})"))
                    qualityGuids.Add(m.Groups[1].Value == "0" ? "" : m.Groups[2].Value.ToLowerInvariant());
                if (Regex.IsMatch(quality, @"customRenderPipeline:\s*\{fileID:\s*0\}"))
                    qualityGuids.Add("");
            }

            bool used = urpPackage || graphicsUrp;
            if (!used)
                return "builtin";

            if (!graphicsUrp)
                fails.Add("URP asset is not assigned in GraphicsSettings");
            if (qualityGuids.Count == 0 || qualityGuids.Exists(g => !IsUrpAsset(g, metas)))
                fails.Add("URP asset is not assigned in QualitySettings");
            return graphicsUrp || qualityGuids.Exists(g => IsUrpAsset(g, metas)) ? "urp" : "missing";
        }

        static string PipelineGuid(string text, string field)
        {
            if (text == null) return "";
            Match m = Regex.Match(text, field + @":\s*\{fileID:\s*(-?\d+)\s*,\s*guid:\s*([0-9a-fA-F]{32})");
            if (!m.Success) return "";
            if (m.Groups[1].Value == "0") return "";
            return m.Groups[2].Value.ToLowerInvariant();
        }

        static bool IsUrpAsset(string guid, Dictionary<string, Meta> metas)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            if (!metas.TryGetValue(guid, out Meta meta)) return false;
            if (!File.Exists(meta.AssetPath)) return false;
            string body = File.ReadAllText(meta.AssetPath);
            return body.IndexOf("UniversalRenderPipelineAsset", StringComparison.Ordinal) >= 0;
        }

        static bool ScriptResolves(string guid, long fileId, Dictionary<string, Meta> metas, Dictionary<string, PackageAsset> packages, out string why)
        {
            if (packages.TryGetValue(guid, out PackageAsset pkg) && pkg.Kind == "script")
            {
                if (fileId != pkg.FileId)
                {
                    why = "fileID";
                    return false;
                }
                why = "";
                return true;
            }

            if (!metas.TryGetValue(guid, out Meta meta))
            {
                why = "no meta";
                return false;
            }
            if (!meta.AssetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(meta.AssetPath))
            {
                why = "not a cs meta";
                return false;
            }
            if (fileId != 11500000 && !HasFileId(meta, fileId))
            {
                why = "fileID";
                return false;
            }
            why = "";
            return true;
        }

        static bool RefResolves(string guid, long fileId, Dictionary<string, Meta> metas, Dictionary<string, PackageAsset> packages, out string why)
        {
            if (IsBuiltin(guid))
            {
                why = "";
                return fileId != 0;
            }

            if (packages.TryGetValue(guid, out PackageAsset pkg))
            {
                if (fileId != pkg.FileId)
                {
                    why = "package fileID";
                    return false;
                }
                why = "";
                return true;
            }

            if (!metas.TryGetValue(guid, out Meta meta))
            {
                why = "unknown guid";
                return false;
            }
            if (!File.Exists(meta.AssetPath) && !Directory.Exists(meta.AssetPath))
            {
                why = "missing asset";
                return false;
            }
            if (Directory.Exists(meta.AssetPath))
            {
                why = "";
                return true;
            }

            string ext = Path.GetExtension(meta.AssetPath);
            if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                why = fileId == 11500000 || HasFileId(meta, fileId) ? "" : "script fileID";
                return why.Length == 0;
            }
            if (ext.Equals(".shader", StringComparison.OrdinalIgnoreCase))
            {
                why = fileId == 4800000 || HasFileId(meta, fileId) ? "" : "shader fileID";
                return why.Length == 0;
            }

            if (!LooksLikeYaml(meta.AssetPath))
            {
                why = "";
                return true;
            }

            if (HasFileId(meta, fileId))
            {
                why = "";
                return true;
            }
            why = "fileID not in asset";
            return false;
        }

        static bool HasFileId(Meta meta, long fileId)
        {
            if (meta.Ids == null)
            {
                meta.Ids = new HashSet<long>();
                Match main = MainId.Match(meta.Text);
                if (main.Success) meta.Ids.Add(long.Parse(main.Groups[1].Value));
                if (File.Exists(meta.AssetPath) && LooksLikeYaml(meta.AssetPath))
                {
                    string body = File.ReadAllText(meta.AssetPath);
                    foreach (Match a in Anchor.Matches(body))
                        meta.Ids.Add(long.Parse(a.Groups[1].Value));
                }
            }
            return meta.Ids.Contains(fileId);
        }

        static bool LooksLikeYaml(string path)
        {
            using (var reader = new StreamReader(path))
            {
                string line = reader.ReadLine() ?? "";
                return line.StartsWith("%YAML", StringComparison.Ordinal) || line.StartsWith("---", StringComparison.Ordinal);
            }
        }

        static bool IsScriptSlot(string text, int index)
        {
            int line = text.LastIndexOf('\n', Math.Max(0, index - 1));
            int start = line < 0 ? 0 : line + 1;
            string row = text.Substring(start, index - start);
            return row.IndexOf("m_Script", StringComparison.Ordinal) >= 0;
        }

        static IEnumerable<string> YamlDocs(string assets)
        {
            foreach (string file in Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories))
            {
                if (Skip(file)) continue;
                if (file.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                    yield return file;
            }
        }

        static bool IsBuiltin(string guid)
        {
            if (guid.Length < 16) return false;
            for (int i = 0; i < 16; i++)
            {
                if (guid[i] != '0') return false;
            }
            return true;
        }

        static bool IsNullGuid(string guid)
        {
            for (int i = 0; i < guid.Length; i++)
            {
                if (guid[i] != '0') return false;
            }
            return true;
        }

        static bool Skip(string path)
        {
            string name = Path.GetFileName(path);
            if (name.StartsWith(".", StringComparison.Ordinal)) return true;
            if (name.EndsWith("~", StringComparison.Ordinal)) return true;
            return false;
        }

        static string Read(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        static string Rel(string root, string path)
        {
            if (path.StartsWith(root, StringComparison.Ordinal))
                return path.Substring(root.Length).TrimStart('/', '\\').Replace('\\', '/');
            return path.Replace('\\', '/');
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

        sealed class Meta
        {
            public string Guid;
            public string MetaPath;
            public string AssetPath;
            public string Text;
            public HashSet<long> Ids;
        }

        sealed class Decl
        {
            public string Key;
            public string Simple;
            public string FileName;
            public string Path;
            public bool Partial;
            public bool Nested;
            public List<string> Bases;
            public string Ns;
        }

        sealed class PackageAsset
        {
            public readonly string Guid;
            public readonly string Kind;
            public readonly string Name;
            public readonly long FileId;
            public readonly string PackageId;

            public PackageAsset(string guid, string kind, string name, long fileId, string packageId)
            {
                Guid = guid;
                Kind = kind;
                Name = name;
                FileId = fileId;
                PackageId = packageId;
            }
        }

        struct SceneEntry
        {
            public bool Enabled;
            public string Path;
            public string Guid;
        }
    }
}
