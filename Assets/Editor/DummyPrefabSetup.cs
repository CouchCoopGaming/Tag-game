#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Tag.EditorTools
{
    /// <summary>One-click: fill Dummy_Runner / Dummy_It prefab stubs with FBX mesh + mats.</summary>
    public static class DummyPrefabSetup
    {
        const string RunnerFbx = "Assets/Art/Characters/Dummy_Runner.fbx";
        const string ItFbx = "Assets/Art/Characters/Dummy_It.fbx";
        const string RunnerPrefab = "Assets/Art/Characters/Dummy_Runner.prefab";
        const string ItPrefab = "Assets/Art/Characters/Dummy_It.prefab";

        [MenuItem("Tag/Setup Dummy Prefabs From FBX")]
        public static void Setup()
        {
            Build(RunnerFbx, RunnerPrefab, new[]
            {
                "Assets/Art/Characters/Mat_Runner_Base.mat",
                "Assets/Art/Characters/Mat_Runner_Accent.mat",
                "Assets/Art/Characters/Mat_Runner_ItOverride.mat"
            });
            Build(ItFbx, ItPrefab, new[]
            {
                "Assets/Art/Characters/Mat_It_Base.mat",
                "Assets/Art/Characters/Mat_It_Accent.mat",
                "Assets/Art/Characters/Mat_It_ItOverride.mat"
            });
            AssetDatabase.SaveAssets();
            Debug.Log("[Tag] Dummy_Runner / Dummy_It prefabs updated from FBX + mats. Assign DummyAvatarBinder on Player.");
        }

        static void Build(string fbxPath, string prefabPath, string[] matPaths)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError("Missing FBX: " + fbxPath);
                return;
            }
            var root = Object.Instantiate(fbx);
            root.name = System.IO.Path.GetFileNameWithoutExtension(prefabPath);

            var mats = new Material[matPaths.Length];
            for (int i = 0; i < matPaths.Length; i++)
                mats[i] = AssetDatabase.LoadAssetAtPath<Material>(matPaths[i]);

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (mats.Length > 0 && mats[0] != null)
                    r.sharedMaterials = mats;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Tag] Wrote " + prefabPath);
        }
    }
}
#endif
