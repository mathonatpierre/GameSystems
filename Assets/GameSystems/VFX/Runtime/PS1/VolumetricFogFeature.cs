using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;

namespace GameSystems.VFX
{
    public sealed class VolumetricFogFeature : ScriptableRendererFeature
    {
        [SerializeField] Material material;
        FogPass pass;

        public void Configure(Material value) { material = value; Create(); }

        public override void Create()
        {
            pass = material == null ? null : new FogPass(material)
            { renderPassEvent = RenderPassEvent.AfterRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass != null && renderingData.cameraData.cameraType == CameraType.Game &&
                renderingData.cameraData.camera.cullingMask != 0)
                renderer.EnqueuePass(pass);
        }

        sealed class FogPass : ScriptableRenderPass
        {
            readonly Material material;

            public FogPass(Material value)
            {
                material = value;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer) return;
                TextureHandle source = resources.activeColorTexture;
                TextureDesc descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "CameraColor-LennieVolumetricFog";
                descriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(descriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, "Lennie Volumetric Fog + PS1 Finish");
                resources.cameraColor = destination;
            }
        }
    }
}
