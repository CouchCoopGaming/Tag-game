#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tag.EditorTools
{
    /// <summary>
    /// Creates / assigns Tag URP pipeline + renderer so URP Lit mats resolve.
    /// Menu: Tag → Ensure URP Pipeline. Also runs once after domain reload.
    /// </summary>
    public static class TagUrpSetup
    {
        const string Folder = "Assets/Settings";
        const string RendererPath = Folder + "/TagURPRenderer.asset";
        const string PipelinePath = Folder + "/TagURPAsset.asset";

        [MenuItem("Tag/Ensure URP Pipeline")]
        public static void EnsureMenu()
        {
            Ensure();
            AssetDatabase.SaveAssets();
            Debug.Log("[Tag] URP pipeline assigned in GraphicsSettings + QualitySettings.");
        }

        [InitializeOnLoadMethod]
        static void AutoEnsure()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                try { Ensure(); }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Tag] URP auto-setup skipped: " + e.Message);
                }
            };
        }

        public static void Ensure()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "TagURPRenderer";
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "TagURPAsset";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                if (list != null)
                {
                    if (list.arraySize < 1) list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pipeline);
                }
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }
    }
}
#endif
