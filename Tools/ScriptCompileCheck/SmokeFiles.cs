using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Tag.Tools
{
    /// <summary>
    /// Headless half of Tag/Smoke Check. Scenes come from EditorBuildSettings.
    /// Missing scripts are MonoBehaviours whose script guid does not resolve.
    /// Resources, the arena registry, PracticeRoutes.json, zone names, and the
    /// shared kill box are the same files the editor menu checks. No gameplay
    /// numbers live here. Registry ok means arenas 0 Mega Park, 1 Pocket Park,
    /// and 2 Stack Yard, plus the 13 zones and the kill boxes.
    /// </summary>
    static class SmokeFiles
    {
        static readonly string[] BuiltinTags =
        {
            "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController"
        };

        static readonly string[] BuiltinLayers = { "Default", "TransparentFX", "Ignore Raycast", "Water", "UI" };

        static readonly Regex SceneRow = new Regex(
            @"enabled:\s*1\s*\r?\n\s*path:\s*(\S+)",
            RegexOptions.Compiled);

        static readonly Regex ScriptRef = new Regex(
            @"m_Script:\s*\{fileID:\s*(\d+)(?:,\s*guid:\s*([0-9a-fA-F]+),\s*type:\s*\d+)?\s*\}",
            RegexOptions.Compiled);

        static readonly Regex GuidRef = new Regex(
            @"guid:\s*([0-9a-fA-F]{32})",
            RegexOptions.Compiled);

        static readonly Regex AudioLiteral = new Regex(
            "\"((?:SFX|UI|Music)/[^\"]+)\"",
            RegexOptions.Compiled);

        static readonly string[] ArenaNames = { "Mega Park", "Pocket Park", "Stack Yard" };

        static readonly string[] RouteIds =
        {
            "mega-beginner", "mega-wall", "mega-toy",
            "pocket-beginner", "pocket-toy",
            "stack-beginner", "stack-toy"
        };

        static readonly string[] RouteArenas =
        {
            "Mega Park", "Mega Park", "Mega Park",
            "Pocket Park", "Pocket Park",
            "Stack Yard", "Stack Yard"
        };

        static readonly string[] MegaZones = { "West Yard", "North Bowl", "Mid Court", "South Court", "East Forts" };
        static readonly string[] PocketZones = { "West Lawn", "Center Court", "Fast Lane", "East Sand" };
        static readonly string[] StackZones = { "South Yard", "East Lane", "West Stack", "North Roof" };
        static readonly string[] ZoneMats = { "zbrick", "zwine", "zindigo", "zolive", "zslate" };

        static readonly string[] LandmarkMethods =
        {
            "Water", "Crane", "Clock", "Sign", "Light",
            "Windmill", "Archway", "Board", "Buoy",
            "Chimney", "Gantry", "Radio", "Billboard"
        };

        static readonly string[] LandmarkTags =
        {
            "Water", "Crane", "Clock", "Sign", "Light",
            "Mill", "Arch", "Board", "Buoy",
            "Chimney", "Gantry", "Mast", "Bill"
        };

        static readonly Regex TagString = new Regex(
            @"m_TagString:\s*(\S+)",
            RegexOptions.Compiled);

        static readonly Regex LayerIndex = new Regex(
            @"m_Layer:\s*(\d+)",
            RegexOptions.Compiled);

        public static bool Run(string root, out string proof, out string report)
        {
            var log = new StringBuilder();
            int scenes = 0;
            int missingScripts = 0;
            bool resourcesOk = true;
            bool registryOk = true;
            bool jsonOk = true;
            bool extrasOk = true;

            var guids = LoadGuids(root);
            var scenePaths = EnabledScenes(root);
            scenes = scenePaths.Count;
            if (scenes < 1)
            {
                extrasOk = false;
                log.AppendLine("no enabled scenes in ProjectSettings/EditorBuildSettings.asset");
            }

            var tags = new HashSet<string>(StringComparer.Ordinal);
            var layers = new List<string>();
            ReadTagManager(root, tags, layers, log, ref extrasOk);

            foreach (string rel in scenePaths)
            {
                string path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
                log.AppendLine("scene " + rel);
                if (!File.Exists(path))
                {
                    missingScripts++;
                    extrasOk = false;
                    log.AppendLine("  missing file");
                    continue;
                }
                string text = File.ReadAllText(path);
                int miss = CountMissingScripts(text, guids, log);
                missingScripts += miss;
                int listeners = CountType(text, "AudioListener:");
                int events = CountType(text, "EventSystem:");
                log.AppendLine("  listeners=" + listeners + " eventSystems=" + events + " missingScripts=" + miss);
                if (listeners > 1)
                {
                    extrasOk = false;
                    log.AppendLine("  duplicate AudioListener");
                }
                if (events > 1)
                {
                    extrasOk = false;
                    log.AppendLine("  duplicate EventSystem");
                }
                int broken = CountBrokenGuids(text, guids, log);
                if (broken > 0)
                {
                    extrasOk = false;
                    log.AppendLine("  brokenRefs=" + broken);
                }
                CheckTags(text, tags, log, ref extrasOk);
                CheckLayers(text, layers, log, ref extrasOk);
            }

            var names = new List<string>();
            var roots = new List<string>();
            if (!ReadRegistry(root, names, roots, log))
                registryOk = false;
            if (!CheckZones(root, log) || !CheckKill(root, log))
                registryOk = false;

            if (!CheckJson(root, names, log))
                jsonOk = false;

            if (!CheckResources(root, log))
                resourcesOk = false;

            string resTok = resourcesOk ? "ok" : "FAIL";
            string regTok = registryOk ? "ok" : "FAIL";
            string jsonTok = jsonOk ? "ok" : "FAIL";
            proof = "smoke scenes=" + scenes
                + " missingScripts=" + missingScripts
                + " resources=" + resTok
                + " registry=" + regTok
                + " json=" + jsonTok;

            var full = new StringBuilder();
            full.AppendLine(proof);
            full.Append(log);
            report = full.ToString();
            try
            {
                string dir = Path.Combine(root, "Logs");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "SmokeCheck.txt"), report);
            }
            catch (Exception e)
            {
                extrasOk = false;
                report += "report write failed: " + e.Message + "\n";
            }

            return missingScripts == 0 && resourcesOk && registryOk && jsonOk && extrasOk && scenes > 0;
        }

        static List<string> EnabledScenes(string root)
        {
            var list = new List<string>();
            string path = Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset");
            if (!File.Exists(path)) return list;
            string text = File.ReadAllText(path);
            foreach (Match m in SceneRow.Matches(text))
                list.Add(m.Groups[1].Value.Trim());
            return list;
        }

        static Dictionary<string, string> LoadGuids(string root)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string assets = Path.Combine(root, "Assets");
            if (!Directory.Exists(assets)) return map;
            foreach (string meta in Directory.GetFiles(assets, "*.meta", SearchOption.AllDirectories))
            {
                string guid = ReadGuid(meta);
                if (guid.Length == 0) continue;
                map[guid] = meta.Substring(0, meta.Length - 5);
            }
            return map;
        }

        static string ReadGuid(string metaPath)
        {
            using (var reader = new StreamReader(metaPath))
            {
                string line;
                int n = 0;
                while ((line = reader.ReadLine()) != null && n < 8)
                {
                    n++;
                    if (line.StartsWith("guid: ", StringComparison.Ordinal))
                        return line.Substring(6).Trim();
                }
            }
            return "";
        }

        static int CountMissingScripts(string text, Dictionary<string, string> guids, StringBuilder log)
        {
            int miss = 0;
            int at = 0;
            while (true)
            {
                int idx = text.IndexOf("MonoBehaviour:", at, StringComparison.Ordinal);
                if (idx < 0) break;
                int next = text.IndexOf("\n--- ", idx, StringComparison.Ordinal);
                string block = next < 0 ? text.Substring(idx) : text.Substring(idx, next - idx);
                Match m = ScriptRef.Match(block);
                if (!m.Success)
                {
                    miss++;
                    log.AppendLine("  missing script (no m_Script)");
                }
                else if (m.Groups[1].Value == "0" || m.Groups[2].Success == false || m.Groups[2].Value.Length == 0)
                {
                    miss++;
                    log.AppendLine("  missing script fileID 0");
                }
                else if (!guids.ContainsKey(m.Groups[2].Value))
                {
                    miss++;
                    log.AppendLine("  missing script guid " + m.Groups[2].Value);
                }
                at = idx + 14;
            }
            return miss;
        }

        static int CountType(string text, string header)
        {
            int n = 0;
            int at = 0;
            while (true)
            {
                int idx = text.IndexOf("\n" + header, at, StringComparison.Ordinal);
                if (idx < 0) break;
                n++;
                at = idx + header.Length;
            }
            return n;
        }

        static int CountBrokenGuids(string text, Dictionary<string, string> guids, StringBuilder log)
        {
            int broken = 0;
            foreach (Match m in GuidRef.Matches(text))
            {
                string guid = m.Groups[1].Value;
                if (guid.StartsWith("0000000000000000", StringComparison.Ordinal)) continue;
                if (guids.ContainsKey(guid)) continue;
                broken++;
                log.AppendLine("  unresolved guid " + guid);
            }
            return broken;
        }

        static void CheckTags(string text, HashSet<string> custom, StringBuilder log, ref bool ok)
        {
            foreach (Match m in TagString.Matches(text))
            {
                string tag = m.Groups[1].Value.Trim();
                if (IsBuiltinTag(tag) || custom.Contains(tag)) continue;
                ok = false;
                log.AppendLine("  unknown tag " + tag);
            }
        }

        static bool IsBuiltinTag(string tag)
        {
            for (int i = 0; i < BuiltinTags.Length; i++)
            {
                if (BuiltinTags[i] == tag) return true;
            }
            return false;
        }

        static void CheckLayers(string text, List<string> layers, StringBuilder log, ref bool ok)
        {
            foreach (Match m in LayerIndex.Matches(text))
            {
                int index;
                if (!int.TryParse(m.Groups[1].Value, out index)) continue;
                if (index < 0 || index >= layers.Count || string.IsNullOrEmpty(layers[index]))
                {
                    ok = false;
                    log.AppendLine("  layer index " + index + " has no name");
                }
            }
        }

        static void ReadTagManager(string root, HashSet<string> tags, List<string> layers, StringBuilder log, ref bool ok)
        {
            string path = Path.Combine(root, "ProjectSettings", "TagManager.asset");
            if (!File.Exists(path))
            {
                ok = false;
                log.AppendLine("TagManager.asset missing");
                return;
            }
            string[] lines = File.ReadAllLines(path);
            int mode = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("  tags:", StringComparison.Ordinal)) { mode = 1; continue; }
                if (line.StartsWith("  layers:", StringComparison.Ordinal)) { mode = 2; continue; }
                if (line.StartsWith("  m_", StringComparison.Ordinal)) { mode = 0; continue; }
                if (mode == 1 && line.StartsWith("  - ", StringComparison.Ordinal))
                    tags.Add(line.Substring(4).Trim());
                else if (mode == 2 && line.StartsWith("  - ", StringComparison.Ordinal))
                {
                    string name = line.Substring(4).Trim();
                    layers.Add(name);
                }
            }
            if (!tags.Contains("Player"))
            {
                ok = false;
                log.AppendLine("tag Player missing from TagManager");
            }
            for (int i = 0; i < BuiltinLayers.Length; i++)
            {
                if (!layers.Contains(BuiltinLayers[i]))
                {
                    ok = false;
                    log.AppendLine("layer " + BuiltinLayers[i] + " missing from TagManager");
                }
            }
            log.AppendLine("tags=" + tags.Count + " layersNamed=" + CountNamed(layers));
        }

        static int CountNamed(List<string> layers)
        {
            int n = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                if (!string.IsNullOrEmpty(layers[i])) n++;
            }
            return n;
        }

        static bool ReadRegistry(string root, List<string> names, List<string> roots, StringBuilder log)
        {
            string parkPath = Path.Combine(root, "Assets", "Scripts", "Level", "ParkArena.cs");
            string regPath = Path.Combine(root, "Assets", "Scripts", "Onboard", "ArenaRegistry.cs");
            if (!File.Exists(parkPath) || !File.Exists(regPath))
            {
                log.AppendLine("ArenaRegistry.cs or ParkArena.cs missing");
                return false;
            }
            string park = File.ReadAllText(parkPath);
            string reg = File.ReadAllText(regPath);
            if (!HasConstInt(park, "Mega", "0") || !HasConstInt(park, "Pocket", "1")
                || !HasConstInt(park, "Stack", "2") || !HasConstInt(park, "Count", "3"))
            {
                log.AppendLine("registry arena ids are not 0 Mega, 1 Pocket, 2 Stack");
                return false;
            }
            if (!Regex.IsMatch(park, "if\\s*\\(\\s*id\\s*==\\s*Pocket\\s*\\)\\s*return\\s*\"Pocket Park\"\\s*;")
                || !Regex.IsMatch(park, "if\\s*\\(\\s*id\\s*==\\s*Stack\\s*\\)\\s*return\\s*\"Stack Yard\"\\s*;")
                || !Regex.IsMatch(park, "return\\s*\"Mega Park\"\\s*;"))
            {
                log.AppendLine("registry names are not Mega Park, Pocket Park, Stack Yard");
                return false;
            }
            if (reg.IndexOf("ParkArena.Count", StringComparison.Ordinal) < 0
                || reg.IndexOf("ParkArena.NameOf", StringComparison.Ordinal) < 0
                || reg.IndexOf("PlayAction.Arena1", StringComparison.Ordinal) < 0
                || reg.IndexOf("PlayAction.Arena2", StringComparison.Ordinal) < 0
                || reg.IndexOf("PlayAction.Arena3", StringComparison.Ordinal) < 0
                || reg.IndexOf("\"MegaPark\"", StringComparison.Ordinal) < 0
                || reg.IndexOf("\"1\"", StringComparison.Ordinal) < 0
                || reg.IndexOf("\"2\"", StringComparison.Ordinal) < 0
                || reg.IndexOf("\"3\"", StringComparison.Ordinal) < 0)
            {
                log.AppendLine("registry is not wired to ParkArena keys 1/2/3");
                return false;
            }
            for (int i = 0; i < ArenaNames.Length; i++)
            {
                names.Add(ArenaNames[i]);
                roots.Add("MegaPark");
                log.AppendLine("registry " + i + " " + ArenaNames[i] + " key=" + (i + 1) + " root=MegaPark");
            }
            log.AppendLine("registry arenas=3");
            return true;
        }

        static bool HasConstInt(string text, string name, string value)
        {
            return Regex.IsMatch(text, "const\\s+int\\s+" + name + "\\s*=\\s*" + value + "\\s*;");
        }

        static bool CheckZones(string root, StringBuilder log)
        {
            string path = Path.Combine(root, "Assets", "Scripts", "Level", "ZoneReadability.cs");
            if (!File.Exists(path))
            {
                log.AppendLine("ZoneReadability.cs missing");
                return false;
            }
            string text = File.ReadAllText(path);
            if (!QuotedArray(text, "MegaNames", MegaZones, log)
                || !QuotedArray(text, "PocketNames", PocketZones, log)
                || !QuotedArray(text, "StackNames", StackZones, log)
                || !QuotedArray(text, "ZoneMats", ZoneMats, log))
                return false;
            string[] all = new string[MegaZones.Length + PocketZones.Length + StackZones.Length];
            MegaZones.CopyTo(all, 0);
            PocketZones.CopyTo(all, MegaZones.Length);
            StackZones.CopyTo(all, MegaZones.Length + PocketZones.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < all.Length; i++)
            {
                if (!seen.Add(all[i]) || text.IndexOf("Name = \"" + all[i] + "\"", StringComparison.Ordinal) < 0)
                {
                    log.AppendLine("zone band missing " + all[i]);
                    return false;
                }
            }
            var call = new Regex(
                "(Water|Crane|Clock|Sign|Light|Windmill|Archway|Board|Buoy|Chimney|Gantry|Radio|Billboard)\\(\\s*list\\s*,\\s*\"([^\"]+)\"\\s*,\\s*\"[^\"]+\"\\s*,\\s*\"[^\"]+\"\\s*,\\s*-?\\d+(?:\\.\\d+)?f\\s*,\\s*-?\\d+(?:\\.\\d+)?f\\s*,\\s*(\\d+(?:\\.\\d+)?)f\\s*\\)");
            MatchCollection marks = call.Matches(text);
            if (marks.Count != LandmarkTags.Length)
            {
                log.AppendLine("landmarks=" + marks.Count);
                return false;
            }
            var found = new bool[LandmarkTags.Length];
            for (int i = 0; i < marks.Count; i++)
            {
                string method = marks[i].Groups[1].Value;
                string tag = marks[i].Groups[2].Value;
                int slot = -1;
                for (int j = 0; j < LandmarkTags.Length; j++)
                {
                    if (method == LandmarkMethods[j] && tag == LandmarkTags[j]) slot = j;
                }
                if (slot < 0 || found[slot])
                {
                    log.AppendLine("landmark " + method + " " + tag);
                    return false;
                }
                found[slot] = true;
                double top;
                if (!double.TryParse(marks[i].Groups[3].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out top)
                    || top < 12d || top > 25d)
                {
                    log.AppendLine("landmark height " + tag);
                    return false;
                }
            }
            if (text.IndexOf("\"Landmark_Crown_\"", StringComparison.Ordinal) < 0)
            {
                log.AppendLine("landmark crowns missing");
                return false;
            }
            log.AppendLine("zones arenas=3 zones=13 landmarks=13");
            return true;
        }

        static bool QuotedArray(string text, string field, string[] expect, StringBuilder log)
        {
            Match m = Regex.Match(text, "string\\[\\]\\s+" + field + "\\s*=\\s*\\{([^}]*)\\}");
            if (!m.Success)
            {
                log.AppendLine("zones missing " + field);
                return false;
            }
            MatchCollection got = Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\"");
            if (got.Count != expect.Length)
            {
                log.AppendLine("zones " + field + " count " + got.Count);
                return false;
            }
            for (int i = 0; i < expect.Length; i++)
            {
                if (got[i].Groups[1].Value != expect[i])
                {
                    log.AppendLine("zones " + field + " " + got[i].Groups[1].Value);
                    return false;
                }
            }
            return true;
        }

        static bool CheckKill(string root, StringBuilder log)
        {
            string mega = ReadLayout(root, "MegaParkP1Layout.cs", log);
            string pocket = ReadLayout(root, "PocketParkLayout.cs", log);
            string stack = ReadLayout(root, "StackYardLayout.cs", log);
            string park = ReadLayout(root, "ParkArena.cs", log);
            string boot = ReadLayout(root, "MegaParkP1Bootstrap.cs", log);
            if (mega == null || pocket == null || stack == null || park == null || boot == null)
                return false;
            if (!Regex.IsMatch(mega, "const\\s+float\\s+FenceTop\\s*=\\s*33f\\s*;")
                || !Regex.IsMatch(mega, "const\\s+float\\s+FenceRail\\s*=\\s*2\\.75f\\s*;")
                || !Regex.IsMatch(mega, "const\\s+float\\s+KillPlaneY\\s*=\\s*-2\\.5f\\s*;"))
            {
                log.AppendLine("kill box Mega Park fence, rail, or plane drifted");
                return false;
            }
            if (!AliasesKill(pocket) || !AliasesKill(stack))
            {
                log.AppendLine("kill box Pocket or Stack does not share the Mega plane and fence");
                return false;
            }
            if (!HasFences(mega) || !HasFences(pocket) || !HasFences(stack))
            {
                log.AppendLine("kill box missing a fence side");
                return false;
            }
            if (park.IndexOf("killY = MegaParkP1Layout.KillPlaneY", StringComparison.Ordinal) < 0)
            {
                log.AppendLine("kill box containment is not the shared plane");
                return false;
            }
            if (boot.IndexOf("s.Kind == \"fence\"", StringComparison.Ordinal) < 0
                || boot.IndexOf("Visible rail collider", StringComparison.Ordinal) < 0
                || !FenceBranchStaysDrawn(boot)
                || boot.IndexOf("MegaParkP1Layout.FenceRail", StringComparison.Ordinal) < 0
                || boot.IndexOf("StripCollider(cube)", StringComparison.Ordinal) < 0
                || mega.IndexOf("FenceRail, 0.08f", StringComparison.Ordinal) < 0)
            {
                log.AppendLine("kill box collider is not the visible rail");
                return false;
            }
            log.AppendLine("kill fence=2.75 rail=2.75 plane=-2.5 arenas=3");
            return true;
        }

        // The infield hide turns a renderer off. That must not be the fence branch.
        static bool FenceBranchStaysDrawn(string boot)
        {
            int fence = boot.IndexOf("s.Kind == \"fence\"", StringComparison.Ordinal);
            int hides = boot.IndexOf("Hides(s.Name)", StringComparison.Ordinal);
            if (fence < 0 || hides < fence)
                return false;
            string branch = boot.Substring(fence, hides - fence);
            return branch.IndexOf("enabled = false", StringComparison.Ordinal) < 0;
        }

        static string ReadLayout(string root, string file, StringBuilder log)
        {
            string path = Path.Combine(root, "Assets", "Scripts", "Level", file);
            if (!File.Exists(path))
            {
                log.AppendLine("missing " + file);
                return null;
            }
            return File.ReadAllText(path);
        }

        static bool AliasesKill(string text)
        {
            return Regex.IsMatch(text, "const\\s+float\\s+FenceTop\\s*=\\s*MegaParkP1Layout\\.FenceTop\\s*;")
                && Regex.IsMatch(text, "const\\s+float\\s+KillPlaneY\\s*=\\s*MegaParkP1Layout\\.KillPlaneY\\s*;");
        }

        static bool HasFences(string text)
        {
            string[] sides = { "Fence_S", "Fence_N", "Fence_W", "Fence_E" };
            for (int i = 0; i < sides.Length; i++)
            {
                int at = text.IndexOf("\"" + sides[i] + "\"", StringComparison.Ordinal);
                if (at < 0) return false;
                int end = text.IndexOf(';', at);
                if (end < 0 || text.IndexOf("FenceRail", at, end - at, StringComparison.Ordinal) < 0)
                    return false;
            }
            return text.IndexOf("KillPlaneY", StringComparison.Ordinal) >= 0;
        }

        static bool CheckJson(string root, List<string> arenas, StringBuilder log)
        {
            string path = Path.Combine(root, "Assets", "Resources", "TagArena", "PracticeRoutes.json");
            if (!File.Exists(path))
            {
                log.AppendLine("PracticeRoutes.json missing");
                return false;
            }
            string json;
            try
            {
                json = File.ReadAllText(path);
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    if (!doc.RootElement.TryGetProperty("routes", out var routes) || routes.ValueKind != System.Text.Json.JsonValueKind.Array)
                    {
                        log.AppendLine("routes array missing");
                        return false;
                    }
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var route in routes.EnumerateArray())
                    {
                        if (!route.TryGetProperty("id", out var id) || id.GetString().Length == 0)
                        {
                            log.AppendLine("route missing id");
                            return false;
                        }
                        string idStr = id.GetString();
                        if (!route.TryGetProperty("arena", out var arena))
                        {
                            log.AppendLine("route missing arena");
                            return false;
                        }
                        string arenaName = arena.GetString() ?? "";
                        if (!arenas.Contains(arenaName))
                        {
                            log.AppendLine("route arena not in registry: " + arenaName);
                            return false;
                        }
                        int slot = RouteSlot(idStr);
                        if (slot < 0 || RouteArenas[slot] != arenaName || !seen.Add(idStr))
                        {
                            log.AppendLine("route " + idStr + " arena " + arenaName);
                            return false;
                        }
                        if (!route.TryGetProperty("gates", out var gates) || gates.GetArrayLength() < 2)
                        {
                            log.AppendLine("route " + idStr + " needs a start and a finish");
                            return false;
                        }
                    }
                    if (seen.Count != RouteIds.Length)
                    {
                        log.AppendLine("json routes=" + seen.Count);
                        return false;
                    }
                    log.AppendLine("json routes=7");
                    return true;
                }
            }
            catch (Exception e)
            {
                log.AppendLine("json parse " + e.Message);
                return false;
            }
        }

        static int RouteSlot(string id)
        {
            for (int i = 0; i < RouteIds.Length; i++)
            {
                if (RouteIds[i] == id) return i;
            }
            return -1;
        }

        static bool CheckResources(string root, StringBuilder log)
        {
            bool ok = true;
            string routes = Path.Combine(root, "Assets", "Resources", "TagArena", "PracticeRoutes.json");
            if (!File.Exists(routes))
            {
                ok = false;
                log.AppendLine("missing resource TagArena/PracticeRoutes.json");
            }
            string cfg = Path.Combine(root, "Assets", "Resources", "TagArena", "MovementConfig.asset");
            if (!File.Exists(cfg))
            {
                ok = false;
                log.AppendLine("missing resource TagArena/MovementConfig");
            }
            string trail = Path.Combine(root, "Assets", "Resources", "Mat_Trail_Cyan.mat");
            if (!File.Exists(trail))
            {
                ok = false;
                log.AppendLine("missing resource Mat_Trail_Cyan");
            }

            var clips = new HashSet<string>(StringComparer.Ordinal);
            string[] sources =
            {
                Path.Combine(root, "Assets", "Scripts", "Audio", "ClipCatalog.cs"),
                Path.Combine(root, "Assets", "Scripts", "Audio", "FootstepMap.cs"),
                Path.Combine(root, "Assets", "Scripts", "Audio", "TagSfx.cs"),
                Path.Combine(root, "Assets", "Scripts", "Audio", "AudioCuePlayer.cs"),
                Path.Combine(root, "Assets", "Scripts", "Audio", "AudioProof.cs")
            };
            for (int i = 0; i < sources.Length; i++)
            {
                if (!File.Exists(sources[i]))
                {
                    ok = false;
                    log.AppendLine("missing " + sources[i]);
                    continue;
                }
                string text = File.ReadAllText(sources[i]);
                foreach (Match m in AudioLiteral.Matches(text))
                {
                    string rel = m.Groups[1].Value.Replace('\\', '/');
                    if (!rel.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        rel += ".wav";
                    clips.Add(rel);
                }
            }
            int good = 0;
            foreach (string rel in clips)
            {
                string path = Path.Combine(root, "Assets", "Resources", "Audio", rel.Replace('/', Path.DirectorySeparatorChar));
                if (!IsWav(path))
                {
                    ok = false;
                    log.AppendLine("missing or unreadable audio " + rel);
                }
                else
                    good++;
            }
            log.AppendLine("audio clips=" + good + "/" + clips.Count);
            return ok && clips.Count > 0;
        }

        static bool IsWav(string path)
        {
            if (!File.Exists(path)) return false;
            var info = new FileInfo(path);
            if (info.Length < 44) return false;
            byte[] head = new byte[12];
            using (var stream = File.OpenRead(path))
            {
                if (stream.Read(head, 0, 12) != 12) return false;
            }
            return head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F'
                && head[8] == (byte)'W' && head[9] == (byte)'A' && head[10] == (byte)'V' && head[11] == (byte)'E';
        }
    }
}
