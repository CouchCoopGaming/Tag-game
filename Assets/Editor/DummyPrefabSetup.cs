#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tag.EditorTools
{
    public static class DummyPrefabSetup
    {
        const string RunnerFbx = "Assets/Art/Characters/Dummy_Runner.fbx";
        const string ItFbx = "Assets/Art/Characters/Dummy_It.fbx";
        const string RunnerPrefab = "Assets/Art/Characters/Dummy_Runner.prefab";
        const string ItPrefab = "Assets/Art/Characters/Dummy_It.prefab";

        [MenuItem("Tag/Setup Hub Visuals (Dummies + Props + Play Bind)")]
        public static void SetupAll()
        {
            SetupDummyPrefabs();
            CopyToResources();
            BindPlayScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[Tag] Hub visuals ready — Play should show dummies + park props.");
        }

        [MenuItem("Tag/Setup Dummy Prefabs From FBX")]
        public static void SetupDummyPrefabs()
        {
            BuildCharacter(RunnerFbx, RunnerPrefab, new[]
            {
                "Assets/Art/Characters/Mat_Runner_Base.mat",
                "Assets/Art/Characters/Mat_Runner_Accent.mat",
                "Assets/Art/Characters/Mat_Runner_ItOverride.mat"
            });
            BuildCharacter(ItFbx, ItPrefab, new[]
            {
                "Assets/Art/Characters/Mat_It_Base.mat",
                "Assets/Art/Characters/Mat_It_Accent.mat",
                "Assets/Art/Characters/Mat_It_ItOverride.mat"
            });
        }

        static void BuildCharacter(string fbxPath, string prefabPath, string[] matPaths)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null) { Debug.LogError("Missing " + fbxPath); return; }
            var root = Object.Instantiate(fbx);
            root.name = Path.GetFileNameWithoutExtension(prefabPath);
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
            Debug.Log("[Tag] Prefab " + prefabPath);
        }

        static void CopyToResources()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Characters");
            EnsureFolder("Assets/Resources/Props");
            AssetDatabase.CopyAsset(RunnerPrefab, "Assets/Resources/Characters/Dummy_Runner.prefab");
            AssetDatabase.CopyAsset(ItPrefab, "Assets/Resources/Characters/Dummy_It.prefab");

            string[] props =
            {
                "Toy_Bars","Toy_Bench","Toy_PicnicTable","Toy_Slide","Toy_Tower","Toy_WallPanel",
                "Toy_VaultRail_090","Toy_VaultRail_100","Toy_VaultRail_105","Toy_Bumper",
                "Toy_SpawnPad_Teal","Toy_SpawnPad_Violet","Toy_SpawnPad_Coral","Toy_SpawnPad_Lime"
            };
            foreach (var p in props)
            {
                var src = $"Assets/Art/Props/Playground/{p}.fbx";
                if (File.Exists(src) || AssetDatabase.LoadAssetAtPath<Object>(src) != null)
                    AssetDatabase.CopyAsset(src, $"Assets/Resources/Props/{p}.prefab");
            }
            // Trail mat
            EnsureFolder("Assets/Resources");
            AssetDatabase.CopyAsset("Assets/Art/VFX/Trail/Mat_Trail_Cyan.mat", "Assets/Resources/Mat_Trail_Cyan.mat");
        }

        static void BindPlayScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Play.unity", OpenSceneMode.Single);
            foreach (var name in new[] { "Player", "DummyRunner" })
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                var binder = go.GetComponent<Tag.Art.DummyAvatarBinder>();
                if (binder == null) binder = go.AddComponent<Tag.Art.DummyAvatarBinder>();
                var so = new SerializedObject(binder);
                so.FindProperty("runnerVisualPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(RunnerPrefab);
                so.FindProperty("itVisualPrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(ItPrefab);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // Park dresser on CutArenaBootstrap host
            var bootstrap = Object.FindObjectOfType<Tag.Level.CutArenaBootstrap>();
            if (bootstrap != null)
            {
                if (bootstrap.GetComponent<Tag.Art.ParkPropDresser>() == null)
                    bootstrap.gameObject.AddComponent<Tag.Art.ParkPropDresser>();
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Tag] Play scene bound.");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        // Auto-run once when scripts recompile if flag missing
        [InitializeOnLoadMethod]
        static void AutoPrompt()
        {
            if (SessionState.GetBool("Tag.HubVisualsSetupDone", false)) return;
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool("Tag.HubVisualsSetupDone", false)) return;
                if (!File.Exists("Assets/Art/Characters/Dummy_Runner.fbx")) return;
                if (EditorUtility.DisplayDialog("Tag Hub Visuals",
                    "Dummy + PARK prop meshes are in the project. Run setup so Play shows them instead of graybox capsules?",
                    "Setup now", "Later"))
                {
                    SetupAll();
                    SessionState.SetBool("Tag.HubVisualsSetupDone", true);
                }
            };
        }
    }
}
#endif
