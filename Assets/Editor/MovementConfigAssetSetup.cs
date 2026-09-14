#if UNITY_EDITOR
using System.IO;
using TagArena.Movement;
using UnityEditor;
using UnityEngine;

namespace Tag.EditorTools
{
    public static class MovementConfigAssetSetup
    {
        public const string AssetPath = "Assets/Resources/TagArena/MovementConfig.asset";

        [MenuItem("Tag/Create MovementConfig Asset")]
        public static void Create()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/TagArena");

            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(AssetPath);
            if (existing != null)
            {
                Debug.Log("[Tag] MovementConfig already at " + AssetPath);
                return;
            }

            var cfg = ScriptableObject.CreateInstance<MovementConfig>();
            AssetDatabase.CreateAsset(cfg, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tag] Created " + AssetPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif