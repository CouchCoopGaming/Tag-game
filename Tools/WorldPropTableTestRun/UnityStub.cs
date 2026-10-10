using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UnityEngine
{
    public class Object
    {
        public string name;
    }

    public class GameObject : Object
    {
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new()
        {
            return new T();
        }
    }

    /// <summary>
    /// Stand-in for the player Resources load. Reads the same YAML the editor
    /// would deserialize, and leaves a missing guid as a null prefab.
    /// </summary>
    public static class Resources
    {
        public static T Load<T>(string path) where T : Object
        {
            if (path != "World/WorldPropTable")
                return null;
            string asset = "Assets/Resources/World/WorldPropTable.asset";
            if (!File.Exists(asset))
                return null;
            var table = new Tag.Level.WorldPropTable();
            table.Entries = ReadEntries(asset);
            return table as T;
        }

        static Tag.Level.WorldPropTable.Entry[] ReadEntries(string asset)
        {
            string text = File.ReadAllText(asset);
            var guids = GuidIndex();
            var entries = new List<Tag.Level.WorldPropTable.Entry>();
            var blocks = Regex.Split(text, @"(?m)^  - Path: ");
            for (int i = 1; i < blocks.Length; i++)
            {
                string block = blocks[i];
                int nl = block.IndexOf('\n');
                if (nl < 0) continue;
                string path = block.Substring(0, nl).Trim();
                var file = Regex.Match(block, @"fileID:\s*(-?\d+)");
                var guid = Regex.Match(block, @"guid:\s*([0-9a-fA-F]{32})");
                long fileId = file.Success ? long.Parse(file.Groups[1].Value) : 0;
                string g = guid.Success ? guid.Groups[1].Value.ToLowerInvariant() : "";
                GameObject prefab = null;
                if (fileId != 0 && guids.Contains(g))
                    prefab = new GameObject();
                entries.Add(new Tag.Level.WorldPropTable.Entry { Path = path, Prefab = prefab });
            }
            return entries.ToArray();
        }

        static HashSet<string> GuidIndex()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (string meta in Directory.EnumerateFiles("Assets", "*.meta", SearchOption.AllDirectories))
            {
                foreach (string line in File.ReadLines(meta))
                {
                    if (line.StartsWith("guid: ", StringComparison.Ordinal))
                    {
                        set.Add(line.Substring(6).Trim().ToLowerInvariant());
                        break;
                    }
                }
            }
            return set;
        }
    }
}
