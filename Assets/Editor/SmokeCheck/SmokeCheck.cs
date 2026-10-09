#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tag.EditorTools
{
    /// <summary>
    /// Editor-only. The asmdef includes the Editor platform only, so player
    /// builds do not compile this menu. Tag/Smoke Check opens every scene in
    /// Build Settings and writes Logs/SmokeCheck.txt. Registry ok means arenas
    /// 0 Mega Park, 1 Pocket Park, and 2 Stack Yard, plus the 13 zones and the
    /// kill boxes. The headless compile check reads the same files.
    /// </summary>
    public static class SmokeCheck
    {
        const string Menu = "Tag/Smoke Check";

        static readonly string[] BuiltinTags =
        {
            "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController"
        };

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

        static readonly string[] RuntimeFilled =
        {
            "matchTuning", "accentRenderer", "bodyRb",
            "runnerBaseMat", "runnerAccentMat", "runnerOverrideMat",
            "itBaseMat", "itAccentMat", "itOverrideMat"
        };

        [MenuItem(Menu)]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var setup = EditorSceneManager.GetSceneManagerSetup();
            var log = new StringBuilder();
            int missingScripts = 0;
            int scenes = 0;
            bool resourcesOk = true;
            bool registryOk = true;
            bool jsonOk = true;
            bool extrasOk = true;

            try
            {
                var names = new List<string>();
                if (!ReadRegistry(names, log)) registryOk = false;
                if (!CheckZones(log) || !CheckKill(log)) registryOk = false;
                if (!CheckJson(names, log)) jsonOk = false;
                if (!CheckResources(log)) resourcesOk = false;
                CheckTagManager(log, ref extrasOk);

                foreach (var entry in EditorBuildSettings.scenes)
                {
                    if (!entry.enabled) continue;
                    scenes++;
                    log.AppendLine("scene " + entry.path);
                    if (!File.Exists(entry.path))
                    {
                        missingScripts++;
                        extrasOk = false;
                        log.AppendLine("  missing file");
                        continue;
                    }
                    Scene scene = EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Single);
                    int miss = 0;
                    int listeners = 0;
                    int events = 0;
                    var roots = scene.GetRootGameObjects();
                    for (int i = 0; i < roots.Length; i++)
                        Walk(roots[i], log, ref miss, ref listeners, ref events, ref extrasOk);
                    missingScripts += miss;
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
                }
            }
            finally
            {
                if (setup != null && setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            string proof = "smoke scenes=" + scenes
                + " missingScripts=" + missingScripts
                + " resources=" + (resourcesOk ? "ok" : "FAIL")
                + " registry=" + (registryOk ? "ok" : "FAIL")
                + " json=" + (jsonOk ? "ok" : "FAIL");
            var full = new StringBuilder();
            full.AppendLine(proof);
            full.Append(log);
            string project = Directory.GetParent(Application.dataPath).FullName;
            string dir = Path.Combine(project, "Logs");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "SmokeCheck.txt");
            File.WriteAllText(path, full.ToString());
            bool ok = missingScripts == 0 && resourcesOk && registryOk && jsonOk && extrasOk && scenes > 0;
            if (ok) Debug.Log("[Tag] " + proof + "\n" + path);
            else Debug.LogError("[Tag] " + proof + "\n" + path);
        }

        static void Walk(GameObject go, StringBuilder log, ref int missing, ref int listeners, ref int events, ref bool extrasOk)
        {
            int onObject = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (onObject > 0)
            {
                missing += onObject;
                log.AppendLine("  missing script x" + onObject + " on " + go.name);
            }
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                string typeName = c.GetType().Name;
                if (typeName == "AudioListener" && c is Behaviour listener && listener.enabled && go.activeInHierarchy)
                    listeners++;
                if (typeName == "EventSystem" && c is Behaviour ev && ev.enabled && go.activeInHierarchy)
                    events++;
                if (c is MonoBehaviour mb && !Nulls(go, mb, log))
                    extrasOk = false;
            }
            string tag = go.tag;
            if (!KnownTag(tag))
            {
                extrasOk = false;
                log.AppendLine("  unknown tag " + tag + " on " + go.name);
            }
            string layer = LayerMask.LayerToName(go.layer);
            if (string.IsNullOrEmpty(layer))
            {
                extrasOk = false;
                log.AppendLine("  unnamed layer " + go.layer + " on " + go.name);
            }
            int childCount = go.transform.childCount;
            for (int i = 0; i < childCount; i++)
                Walk(go.transform.GetChild(i).gameObject, log, ref missing, ref listeners, ref events, ref extrasOk);
        }

        static bool Nulls(GameObject go, MonoBehaviour mb, StringBuilder log)
        {
            string ns = mb.GetType().Namespace ?? "";
            if (!ns.StartsWith("Tag", StringComparison.Ordinal)) return true;
            var so = new SerializedObject(mb);
            var prop = so.GetIterator();
            bool ok = true;
            if (!prop.NextVisible(true)) return true;
            do
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (prop.name == "m_Script") continue;
                if (prop.objectReferenceValue != null) continue;
                if (prop.objectReferenceInstanceIDValue != 0)
                {
                    ok = false;
                    log.AppendLine("  broken ref " + go.name + "." + mb.GetType().Name + "." + prop.name);
                    continue;
                }
                string kind = IsRuntimeFilled(prop.name) ? "runtime" : "unassigned";
                log.AppendLine("  null " + kind + " " + go.name + "." + mb.GetType().Name + "." + prop.propertyPath);
            }
            while (prop.NextVisible(true));
            return ok;
        }

        static bool IsRuntimeFilled(string name)
        {
            for (int i = 0; i < RuntimeFilled.Length; i++)
            {
                if (RuntimeFilled[i] == name) return true;
            }
            return false;
        }

        static bool KnownTag(string tag)
        {
            for (int i = 0; i < BuiltinTags.Length; i++)
            {
                if (BuiltinTags[i] == tag) return true;
            }
            string[] custom = UnityEditorInternal.InternalEditorUtility.tags;
            for (int i = 0; i < custom.Length; i++)
            {
                if (custom[i] == tag) return true;
            }
            return false;
        }

        static void CheckTagManager(StringBuilder log, ref bool ok)
        {
            bool player = false;
            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == "Player") player = true;
            }
            if (!player)
            {
                ok = false;
                log.AppendLine("tag Player missing");
            }
            if (LayerMask.NameToLayer("Default") < 0 || LayerMask.NameToLayer("UI") < 0)
            {
                ok = false;
                log.AppendLine("layer Default or UI missing");
            }
            log.AppendLine("tags=" + tags.Length);
        }

        static bool ReadRegistry(List<string> names, StringBuilder log)
        {
            string parkPath = Path.Combine(Application.dataPath, "Scripts", "Level", "ParkArena.cs");
            string regPath = Path.Combine(Application.dataPath, "Scripts", "Onboard", "ArenaRegistry.cs");
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
                log.AppendLine("registry " + i + " " + ArenaNames[i] + " key=" + (i + 1) + " root=MegaPark");
            }
            log.AppendLine("registry arenas=3");
            return true;
        }

        static bool HasConstInt(string text, string name, string value)
        {
            return Regex.IsMatch(text, "const\\s+int\\s+" + name + "\\s*=\\s*" + value + "\\s*;");
        }

        static bool CheckZones(StringBuilder log)
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Level", "ZoneReadability.cs");
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
            var seen = new HashSet<string>();
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

        static bool CheckKill(StringBuilder log)
        {
            string mega = ReadLayout("MegaParkP1Layout.cs", log);
            string pocket = ReadLayout("PocketParkLayout.cs", log);
            string stack = ReadLayout("StackYardLayout.cs", log);
            string park = ReadLayout("ParkArena.cs", log);
            string boot = ReadLayout("MegaParkP1Bootstrap.cs", log);
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
                || boot.IndexOf("r.enabled = false", StringComparison.Ordinal) >= 0
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

        static string ReadLayout(string file, StringBuilder log)
        {
            string path = Path.Combine(Application.dataPath, "Scripts", "Level", file);
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

        static bool CheckJson(List<string> arenas, StringBuilder log)
        {
            string path = Path.Combine(Application.dataPath, "Resources", "TagArena", "PracticeRoutes.json");
            if (!File.Exists(path))
            {
                log.AppendLine("PracticeRoutes.json missing");
                return false;
            }
            try
            {
                string json = File.ReadAllText(path);
                int routes = json.IndexOf("\"routes\"", StringComparison.Ordinal);
                if (routes < 0) return false;
                var seen = new HashSet<string>();
                int i = json.IndexOf('[', routes);
                if (i < 0) return false;
                while (i < json.Length)
                {
                    int obj = json.IndexOf('{', i + 1);
                    if (obj < 0) break;
                    int end = MatchBrace(json, obj);
                    if (end < 0) break;
                    string body = json.Substring(obj, end - obj + 1);
                    string id = Field(body, "id");
                    string arena = Field(body, "arena");
                    if (id.Length == 0 || arena.Length == 0) return false;
                    if (!arenas.Contains(arena))
                    {
                        log.AppendLine("route arena not in registry: " + arena);
                        return false;
                    }
                    int slot = RouteSlot(id);
                    if (slot < 0 || RouteArenas[slot] != arena || !seen.Add(id))
                    {
                        log.AppendLine("route " + id + " arena " + arena);
                        return false;
                    }
                    int gates = 0;
                    int g = body.IndexOf("\"gates\"", StringComparison.Ordinal);
                    if (g >= 0)
                    {
                        int ga = body.IndexOf('[', g);
                        int depth = 0;
                        for (int k = ga; k < body.Length; k++)
                        {
                            if (body[k] == '{') { if (depth == 1) gates++; depth++; }
                            else if (body[k] == '}') depth--;
                            else if (body[k] == '[') depth++;
                            else if (body[k] == ']') { depth--; if (depth == 0) break; }
                        }
                    }
                    if (gates < 2)
                    {
                        log.AppendLine("route " + id + " needs a start and a finish");
                        return false;
                    }
                    i = end + 1;
                }
                if (seen.Count != RouteIds.Length)
                {
                    log.AppendLine("json routes=" + seen.Count);
                    return false;
                }
                log.AppendLine("json routes=7");
                return true;
            }
            catch (Exception e)
            {
                log.AppendLine("json " + e.Message);
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

        static int MatchBrace(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
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

        static string Field(string body, string key)
        {
            string needle = "\"" + key + "\"";
            int at = body.IndexOf(needle, StringComparison.Ordinal);
            if (at < 0) return "";
            int q = body.IndexOf('"', body.IndexOf(':', at) + 1);
            if (q < 0) return "";
            int q2 = body.IndexOf('"', q + 1);
            if (q2 < 0) return "";
            return body.Substring(q + 1, q2 - q - 1);
        }

        static bool CheckResources(StringBuilder log)
        {
            bool ok = true;
            string root = Application.dataPath;
            if (!File.Exists(Path.Combine(root, "Resources", "TagArena", "PracticeRoutes.json")))
            {
                ok = false;
                log.AppendLine("missing resource TagArena/PracticeRoutes.json");
            }
            if (!File.Exists(Path.Combine(root, "Resources", "TagArena", "MovementConfig.asset")))
            {
                ok = false;
                log.AppendLine("missing resource TagArena/MovementConfig");
            }
            if (!File.Exists(Path.Combine(root, "Resources", "Mat_Trail_Cyan.mat")))
            {
                ok = false;
                log.AppendLine("missing resource Mat_Trail_Cyan");
            }
            var clips = new HashSet<string>();
            string[] sources =
            {
                "Audio/ClipCatalog.cs", "Audio/FootstepMap.cs", "Audio/TagSfx.cs",
                "Audio/AudioCuePlayer.cs", "Audio/AudioProof.cs"
            };
            var rx = new Regex("\"((?:SFX|UI|Music)/[^\"]+)\"");
            for (int i = 0; i < sources.Length; i++)
            {
                string file = Path.Combine(root, "Scripts", sources[i]);
                if (!File.Exists(file))
                {
                    ok = false;
                    log.AppendLine("missing " + sources[i]);
                    continue;
                }
                foreach (Match m in rx.Matches(File.ReadAllText(file)))
                {
                    string rel = m.Groups[1].Value;
                    if (!rel.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) rel += ".wav";
                    clips.Add(rel);
                }
            }
            int good = 0;
            foreach (string rel in clips)
            {
                string path = Path.Combine(root, "Resources", "Audio", rel.Replace('/', Path.DirectorySeparatorChar));
                if (!IsWav(path))
                {
                    ok = false;
                    log.AppendLine("missing or unreadable audio " + rel);
                }
                else good++;
            }
            log.AppendLine("audio clips=" + good + "/" + clips.Count);
            return ok && clips.Count > 0;
        }

        static bool IsWav(string path)
        {
            if (!File.Exists(path)) return false;
            if (new FileInfo(path).Length < 44) return false;
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
#endif
