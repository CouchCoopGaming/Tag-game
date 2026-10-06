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
    /// Build Settings and writes Logs/SmokeCheck.txt.
    /// </summary>
    public static class SmokeCheck
    {
        const string Menu = "Tag/Smoke Check";

        static readonly string[] BuiltinTags =
        {
            "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController"
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
            string path = Path.Combine(Application.dataPath, "Scripts", "Onboard", "ArenaRegistry.cs");
            if (!File.Exists(path))
            {
                log.AppendLine("ArenaRegistry.cs missing");
                return false;
            }
            var rx = new Regex("Name\\s*=\\s*\"([^\"]+)\"\\s*,\\s*Root\\s*=\\s*\"([^\"]+)\"");
            foreach (Match m in rx.Matches(File.ReadAllText(path)))
            {
                names.Add(m.Groups[1].Value);
                log.AppendLine("registry " + m.Groups[1].Value + " root=" + m.Groups[2].Value);
            }
            var seen = new HashSet<string>();
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].Length == 0 || !seen.Add(names[i])) return false;
            }
            return seen.Contains("PARK") && seen.Contains("Mega Park");
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
                int n = 0;
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
                    n++;
                    i = end + 1;
                }
                log.AppendLine("json routes=" + n);
                return n > 0;
            }
            catch (Exception e)
            {
                log.AppendLine("json " + e.Message);
                return false;
            }
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
