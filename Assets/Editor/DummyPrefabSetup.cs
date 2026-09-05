#if UNITY_EDITOR
using System;
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
        const string SessionDone = "Tag.HubVisualsSetupDone";
        const string SessionPrompted = "Tag.HubVisualsPrompted";
        const int AutoMaxAttempts = 40;

        [MenuItem("Tag/Setup Hub Visuals (Dummies + Props + Play Bind)")]
        public static void SetupAll()
        {
            if (!DummiesImported())
            {
                Debug.LogWarning("[Tag] Hub setup aborted — Dummy FBX not imported yet. Wait for Project import, then run Tag/Setup Hub Visuals again.");
                return;
            }

            try
            {
                SetupDummyPrefabs();
                if (!PrefabsReady())
                {
                    Debug.LogError("[Tag] Hub setup stopped — dummy prefabs were not created.");
                    return;
                }
                if (!CopyToResources())
                {
                    Debug.LogError("[Tag] Hub setup stopped — CopyToResources failed (see errors above). Editor left running.");
                    return;
                }
                BindPlayScene();
                AssetDatabase.SaveAssets();
                SessionState.SetBool(SessionDone, true);
                Debug.Log("[Tag] Hub visuals ready — Play should show dummies + park props.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Tag] Hub setup failed safely: " + e.Message + "\n" + e.StackTrace);
            }
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

        static bool PrefabsReady()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(RunnerPrefab) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(ItPrefab) != null;
        }

        static void BuildCharacter(string fbxPath, string prefabPath, string[] matPaths)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError("Missing or not imported yet: " + fbxPath);
                return;
            }
            var root = UnityEngine.Object.Instantiate(fbx);
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
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log("[Tag] Prefab " + prefabPath);
        }

        static bool CopyToResources()
        {
            try
            {
                EnsureFolder("Assets/Resources");
                EnsureFolder("Assets/Resources/Characters");
                EnsureFolder("Assets/Resources/Props");

                if (!SafeCopyAsset(RunnerPrefab, "Assets/Resources/Characters/Dummy_Runner.prefab"))
                    return false;
                if (!SafeCopyAsset(ItPrefab, "Assets/Resources/Characters/Dummy_It.prefab"))
                    return false;

                string[] props =
                {
                    "Toy_Bars","Toy_Bench","Toy_PicnicTable","Toy_Slide","Toy_Tower","Toy_WallPanel",
                    "Toy_VaultRail_090","Toy_VaultRail_100","Toy_VaultRail_105","Toy_Bumper",
                    "Toy_SpawnPad_Teal","Toy_SpawnPad_Violet","Toy_SpawnPad_Coral","Toy_SpawnPad_Lime"
                };
                foreach (var p in props)
                {
                    var src = $"Assets/Art/Props/Playground/{p}.fbx";
                    if (!AssetReady(src)) continue;
                    // Keep .fbx extension — Resources.Load("Props/Toy_Bars") still resolves.
                    SafeCopyAsset(src, $"Assets/Resources/Props/{p}.fbx");
                }

                SafeCopyAsset("Assets/Art/VFX/Trail/Mat_Trail_Cyan.mat", "Assets/Resources/Mat_Trail_Cyan.mat");
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Tag] CopyToResources exception (non-fatal): " + e.Message);
                return false;
            }
        }

        static bool SafeCopyAsset(string src, string dst)
        {
            if (!AssetReady(src))
            {
                Debug.LogWarning("[Tag] Skip copy — source missing/not imported: " + src);
                return false;
            }

            try
            {
                if (AssetDatabase.LoadMainAssetAtPath(dst) != null)
                    AssetDatabase.DeleteAsset(dst);

                var srcFull = Path.GetFullPath(src);
                var dstFull = Path.GetFullPath(dst);
                var dstDir = Path.GetDirectoryName(dstFull);
                if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
                    Directory.CreateDirectory(dstDir);

                File.Copy(srcFull, dstFull, overwrite: true);
                var srcMeta = srcFull + ".meta";
                var dstMeta = dstFull + ".meta";
                if (File.Exists(srcMeta))
                    File.Copy(srcMeta, dstMeta, overwrite: true);

                AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                if (AssetDatabase.LoadMainAssetAtPath(dst) == null)
                {
                    Debug.LogError("[Tag] Copy produced no asset at " + dst);
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Tag] SafeCopyAsset failed " + src + " → " + dst + ": " + e.Message);
                try
                {
                    if (!AssetDatabase.CopyAsset(src, dst))
                    {
                        Debug.LogError("[Tag] AssetDatabase.CopyAsset also failed: " + src);
                        return false;
                    }
                    return true;
                }
                catch (Exception e2)
                {
                    Debug.LogError("[Tag] CopyAsset threw (suppressed Force Quit path): " + e2.Message);
                    return false;
                }
            }
        }

        static bool AssetReady(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            var full = Path.GetFullPath(assetPath);
            if (!File.Exists(assetPath) && !File.Exists(full))
                return false;
            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }

        static bool DummiesImported()
        {
            return AssetReady(RunnerFbx) && AssetReady(ItFbx);
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
            var bootstrap = UnityEngine.Object.FindObjectOfType<Tag.Level.CutArenaBootstrap>();
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

        [InitializeOnLoadMethod]
        static void AutoPrompt()
        {
            if (SessionState.GetBool(SessionDone, false)) return;
            if (SessionState.GetBool(SessionPrompted, false)) return;
            ScheduleAutoPrompt(0);
        }

        static void ScheduleAutoPrompt(int attempt)
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionDone, false)) return;
                if (SessionState.GetBool(SessionPrompted, false)) return;

                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    if (attempt < AutoMaxAttempts)
                        ScheduleAutoPrompt(attempt + 1);
                    return;
                }

                if (!DummiesImported())
                {
                    if (attempt < AutoMaxAttempts)
                        ScheduleAutoPrompt(attempt + 1);
                    else
                        Debug.Log("[Tag] Auto Hub setup skipped — Dummy FBX never finished importing this session. Use Tag/Setup Hub Visuals later.");
                    return;
                }

                SessionState.SetBool(SessionPrompted, true);
                if (EditorUtility.DisplayDialog("Tag Hub Visuals",
                    "Dummy + PARK prop meshes are imported. Run setup so Play shows them instead of graybox capsules?",
                    "Setup now", "Later"))
                {
                    SetupAll();
                }
            };
        }
    }
}
#endif
