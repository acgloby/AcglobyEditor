using  UnityEngine;
using  UnityEngine.Rendering.Universal;
using  UnityEngine.Rendering;

public class VolumetricLight : ScriptableRendererFeature
{
    [System.Serializable]
    public class Setting
    {
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader;

        [Range(0, 1)]
        public float intensity = 0.7f;
        [Range(1, 64)]
        public float stepTimes = 16;
        [Range(0.1f, 10)]
        public float blurRange = 1;
    }

    class VolumetricLightPass : ScriptableRenderPass
    {
        private Material material;
        private RenderTextureDescriptor dsp;
        public RenderTargetIdentifier cameraTarget;
        private RenderTargetHandle temp;

        private Setting setting;
        private RenderTargetHandle buffer01, buffer02;
        public VolumetricLightPass(Setting setting)
        {
            this.renderPassEvent = setting.passEvent;
            this.setting = setting;

            if (setting.shader != null)
            {
                material = CoreUtils.CreateEngineMaterial(setting.shader);
            }
        }
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            dsp = cameraTextureDescriptor;
            temp.Init("Temp");
            buffer01.Init("b1");
            buffer02.Init("b2");
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || !renderingData.cameraData.postProcessEnabled) return;

            var cmd = CommandBufferPool.Get("VolumetricLight");

            var dsp = this.dsp;
            dsp.depthBufferBits = 0;
            //dsp.width /= 2;
            //dsp.height /= 2;

            material.SetFloat("_RandomNumber", Random.Range(0.0f, 1.0f));
            material.SetFloat("_Intensity", this.setting.intensity);
            material.SetFloat("_StepTime", this.setting.stepTimes);

            cmd.GetTemporaryRT(temp.id, dsp);

            //体积光光线步进
            cmd.Blit(cameraTarget, temp.Identifier(), material, 0);

            //Kawase模糊
            var width = dsp.width;
            var height = dsp.height;
            var blurRange = this.setting.blurRange;
            cmd.GetTemporaryRT(buffer01.id, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(buffer02.id, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            material.SetFloat("_BlurRange", 0);

            cmd.Blit(temp.Identifier(), buffer01.Identifier(), material, 1);

            for (int i = 0; i < 4; i++)
            {
                material.SetFloat("_BlurRange", (i + 1) * blurRange);
                cmd.Blit(buffer01.Identifier(), buffer02.Identifier(), material, 1);

                var temRT = buffer01;
                buffer01 = buffer02;
                buffer02 = temRT;
            }
            cmd.SetGlobalTexture("_LightTex", buffer01.Identifier());

            ////blit 混合
            cmd.Blit(cameraTarget, temp.Identifier(), material, 2);
            cmd.Blit(temp.Identifier(), cameraTarget);

            context.ExecuteCommandBuffer(cmd);
            cmd.ReleaseTemporaryRT(temp.id);
            cmd.ReleaseTemporaryRT(buffer01.id);
            cmd.ReleaseTemporaryRT(buffer02.id);
            CommandBufferPool.Release(cmd);
        }
    }
    public Setting setting = new Setting();
    private VolumetricLightPass volumeLightPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        volumeLightPass.cameraTarget = renderer.cameraColorTarget;
        renderer.EnqueuePass(volumeLightPass);
    }
    public override void Create()
    {
        volumeLightPass = new VolumetricLightPass(setting);
    }
}