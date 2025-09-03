using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace XGame.Modules.SpatialMagnifier
{
    public class MagnifierOpaqueTexture : ScriptableRendererFeature
    {
        public static bool IsEnabled { get; set; }

        class OpaqueCopyPass : ScriptableRenderPass
        {
            class PassData
            {
                internal TextureHandle src;
                internal TextureHandle dst;
            }

            // 名称 ID（shader 中使用的名字）
            static readonly int k_GlobalOpaqueTexId = Shader.PropertyToID("_MagnifierOpaqueTexture");
            string m_PassName = "_MagnifierOpaqueTexture";

            public OpaqueCopyPass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            }

            // RenderGraph 里声明资源与操作（不要在这里执行 GPU 命令）
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                var resourceData = frameContext.Get<UniversalResourceData>();
                var camData = frameContext.Get<UniversalCameraData>();

                using (var builder = renderGraph.AddRasterRenderPass<PassData>(m_PassName, out var passData))
                {
                    passData.src = resourceData.activeColorTexture;

                    var desc = camData.cameraTargetDescriptor;
                    desc.msaaSamples = 1;    // 不保留 MSAA 到目标，避免复杂性
                    desc.depthBufferBits = 0;

                    // 如果想 downsample（比如 2x 或 4x），可以手动调整 desc.width/height：
                    // desc.width /= 2; desc.height /= 2;

                    passData.dst = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_MagnifierOpaqueTexture", false);

                    builder.UseTexture(passData.src);
                    builder.SetRenderAttachment(passData.dst, 0);

                    builder.AllowGlobalStateModification(true);

                    // 禁用剔除（因为我们明确需要这个 pass 始终执行）
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        // scaleBias = (1,1,0,0) 是默认不缩放、不偏移
                        Blitter.BlitTexture(ctx.cmd, data.src, new Vector4(1, 1, 0, 0), 0, false);
                    });

                    builder.SetGlobalTextureAfterPass(passData.dst, k_GlobalOpaqueTexId);
                }
            }
        }

        OpaqueCopyPass m_Pass;

        public override void Create()
        {
            m_Pass = new OpaqueCopyPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (IsEnabled)
            {
                renderer.EnqueuePass(m_Pass);
            }
        }
    }
}