using System;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Player-build prefab table for the dressed districts. Lives in Resources so
    /// a build follows these references. The editor asset database is not a load path.
    /// </summary>
    public class WorldPropTable : ScriptableObject
    {
        public const string ResourcePath = "World/WorldPropTable";

        [Serializable]
        public struct Entry
        {
            public string Path;
            public GameObject Prefab;
        }

        public Entry[] Entries;

        /// <summary>Resources load. Null means a player build would spawn nothing.</summary>
        public static WorldPropTable Load()
        {
            return Resources.Load<WorldPropTable>(ResourcePath);
        }

        public GameObject Find(string path)
        {
            if (Entries == null || string.IsNullOrEmpty(path))
                return null;
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].Path == path)
                    return Entries[i].Prefab;
            }
            return null;
        }
    }
}
