using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class NightVisionRenderer : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Shader nightVisionShader;

        [Header("Look")]
        //i love rider it shows me the cool little colours if i add numbers
        public Color tintColor;
        public float scanlineIntensity;
        public float scanlineSpeed;
        public float noiseIntensity;
        public float vignetteRadius;
        public float vignetteSoftness;
        public float lensGap;
        public float gain;
        public float gammaCurve;
    }

    public Settings settings = new();

    public static Camera ActiveCamera;
    public static bool IsActive;

    private NightVisionPass _pass;
    private Material _material;

    public override void Create()
    {
        if (settings.nightVisionShader == null) return;

        _material = CoreUtils.CreateEngineMaterial(settings.nightVisionShader);
        _pass = new NightVisionPass(_material, settings)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!IsActive) return;
        if (renderingData.cameraData.camera != ActiveCamera) return;
        if (_material == null) return;

        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
        => CoreUtils.Destroy(_material);

    private class NightVisionPass : ScriptableRenderPass
    {
        private readonly Material _material;
        private readonly Settings _settings;

        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
        private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
        private static readonly int VignetteRadiusId = Shader.PropertyToID("_VignetteRadius");
        private static readonly int VignetteSoftnessId = Shader.PropertyToID("_VignetteSoftness");
        private static readonly int LensGapId = Shader.PropertyToID("_LensGap");
        private static readonly int Time1Id = Shader.PropertyToID("_Time1");
        private static readonly int GainId = Shader.PropertyToID("_Gain");
        private static readonly int GammaCurveId = Shader.PropertyToID("_GammaCurve");

        private class PassData
        {
            public TextureHandle Source;
            public Material Material;
        }

        public NightVisionPass(Material material, Settings settings)
        {
            _material = material;
            _settings = settings;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;

            var destDesc = renderGraph.GetTextureDesc(source);
            destDesc.name = "_NightVisionTemp";
            destDesc.clearBuffer = false;
            var destination = renderGraph.CreateTexture(destDesc);

            _material.SetColor(TintColorId, _settings.tintColor);
            _material.SetFloat(ScanlineIntensityId, _settings.scanlineIntensity);
            _material.SetFloat(ScanlineSpeedId, _settings.scanlineSpeed);
            _material.SetFloat(NoiseIntensityId, _settings.noiseIntensity);
            _material.SetFloat(VignetteRadiusId, _settings.vignetteRadius);
            _material.SetFloat(VignetteSoftnessId, _settings.vignetteSoftness);
            _material.SetFloat(LensGapId, _settings.lensGap);
            _material.SetFloat(Time1Id, Time.time);
            _material.SetFloat(GainId, _settings.gain);
            _material.SetFloat(GammaCurveId, _settings.gammaCurve);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("NightVision", out var passData))
            {
                passData.Source = source;
                passData.Material = _material;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
                });
            }

            resourceData.cameraColor = destination;
        }
    }
}