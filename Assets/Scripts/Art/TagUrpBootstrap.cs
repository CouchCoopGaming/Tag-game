using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tag.Art
{
    /// <summary>
    /// Last-resort URP assignment so URP Lit materials are not magenta if
    /// Graphics/Quality have no pipeline yet (Editor <c>TagUrpSetup</c> creates
    /// the on-disk assets; this path builds an in-memory pipeline for Play).
    /// </summary>
    public static class TagUrpBootstrap
    {
        static readonly System.Collections.Generic.List<ScriptableObject> _keepAlive = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BeforeScene()
        {
            EnsurePipeline("runtime");
        }

        public static bool PipelineLooksValid()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return false;
            try
            {
                var list = urp.rendererDataList;
                return list.Length > 0 && list[0] != null;
            }
            catch
            {
                return false;
            }
        }

        public static void EnsurePipeline(string reason)
        {
            if (PipelineLooksValid()) return;
            try
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "TagURPRenderer_Runtime";
                var pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "TagURPAsset_Runtime";
                _keepAlive.Add(renderer);
                _keepAlive.Add(pipeline);
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                Debug.Log($"[Tag] URP pipeline assigned at {reason} (was missing or invalid).");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Tag] Failed to create a runtime URP pipeline: " + e.Message);
            }
        }
    }
}
