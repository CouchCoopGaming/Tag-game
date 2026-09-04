#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tag.EditorTools
{
    public static class AudioResourcesCopy
    {
        [MenuItem("Tag/Copy Audio To Resources")]
        public static void Copy()
        {
            Ensure("Assets/Resources");
            Ensure("Assets/Resources/Audio");
            Ensure("Assets/Resources/Audio/SFX");
            Ensure("Assets/Resources/Audio/UI");
            Ensure("Assets/Resources/Audio/Music");
            CopyDir("Assets/Audio/SFX", "Assets/Resources/Audio/SFX");
            CopyDir("Assets/Audio/UI", "Assets/Resources/Audio/UI");
            CopyDir("Assets/Audio/Music", "Assets/Resources/Audio/Music");
            AssetDatabase.Refresh();
            Debug.Log("[Tag] Audio copied to Resources/Audio");
        }

        static void CopyDir(string from, string to)
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { from }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Directory.Exists(path)) continue;
                var name = Path.GetFileName(path);
                AssetDatabase.CopyAsset(path, to + "/" + name);
            }
        }

        static void Ensure(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) Ensure(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        [InitializeOnLoadMethod]
        static void Auto()
        {
            EditorApplication.delayCall += () =>
            {
                if (!Directory.Exists("Assets/Audio/SFX")) return;
                if (Directory.Exists("Assets/Resources/Audio/SFX")) return;
                Copy();
            };
        }
    }
}
#endif
