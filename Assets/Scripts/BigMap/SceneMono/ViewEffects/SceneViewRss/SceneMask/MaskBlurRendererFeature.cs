using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class MaskBlurRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class MaskBlurSettings
    {
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial;         // 一维高斯模糊材质
        public RenderTexture sourceMaskRT;    // 输入的二值遮罩
        public Material screenMaskMaterial;   // 屏幕遮罩的材质（采样 _MaskTex 控制透明）
        public string maskTexturePropertyName = "_LightMask"; // 材质中的纹理属性名
        public int blurRadius = 8;
        public float blurSigma = 3.0f;
        public FilterMode filterMode = FilterMode.Bilinear;
        public RenderTextureFormat outputFormat = RenderTextureFormat.ARGB32;
    }

    public MaskBlurSettings settings = new MaskBlurSettings();

    MaskBlurPass _pass;

    public override void Create()
    {
        _pass = new MaskBlurPass(settings);
        _pass.renderPassEvent = settings.injectionPoint;
    }

    // 每次渲染器设置时被调用
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.blurMaterial == null || settings.sourceMaskRT == null)
            return;
        if(renderingData.cameraData.camera.name != "FOVMaskCamera")
        {
            return;
        }
        _pass.Setup(renderer);
        renderer.EnqueuePass(_pass);
    }

    public class MaskBlurPass : ScriptableRenderPass
    {
        readonly MaskBlurRendererFeature.MaskBlurSettings _settings;
        ScriptableRenderer _renderer;

        // 持久输出 RT
        RenderTexture _resultRT;

        // 临时 RT id
        int _rtBlurX = Shader.PropertyToID("_MaskBlur_RT_X");
        int _rtBlurY = Shader.PropertyToID("_MaskBlur_RT_Y");

        // Profiling
        static readonly ProfilingSampler _profiler = new ProfilingSampler("MaskBlurPass");

        public MaskBlurPass(MaskBlurRendererFeature.MaskBlurSettings settings)
        {
            _settings = settings;
        }

        public void Setup(ScriptableRenderer renderer)
        {
            _renderer = renderer;
        }

        void EnsureResultRT(int w, int h)
        {
            if (_resultRT != null && (_resultRT.width != w || _resultRT.height != h))
            {
                _resultRT.Release();
                Object.Destroy(_resultRT);
                _resultRT = null;
            }

            if (_resultRT == null)
            {
                _resultRT = new RenderTexture(w, h, 0, _settings.outputFormat)
                {
                    name = "MaskBlurResultRT",
                    filterMode = _settings.filterMode,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _resultRT.Create();
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camData = renderingData.cameraData;
            int w = camData.camera.pixelWidth;
            int h = camData.camera.pixelHeight;

            // 输入遮罩不在时直接退出
            if (_settings.sourceMaskRT == null || !_settings.sourceMaskRT.IsCreated())
                return;
            if (_settings.blurMaterial == null)
                return;

            EnsureResultRT(w, h);

            CommandBuffer cmd = CommandBufferPool.Get("MaskBlur");
            using (new ProfilingScope(cmd, _profiler))
            {
                // 分配临时 RT
                cmd.GetTemporaryRT(_rtBlurX, w, h, 0, _settings.filterMode, _settings.outputFormat);
                cmd.GetTemporaryRT(_rtBlurY, w, h, 0, _settings.filterMode, _settings.outputFormat);

                //// 横向模糊
                //_settings.blurMaterial.SetVector("_Direction", new Vector4(1, 0, 0, 0));
                //_settings.blurMaterial.SetFloat("_Radius", _settings.blurRadius);
                //_settings.blurMaterial.SetFloat("_Sigma", _settings.blurSigma);
                //cmd.Blit(_settings.sourceMaskRT, _rtBlurX, _settings.blurMaterial);

                //// 纵向模糊
                //_settings.blurMaterial.SetVector("_Direction", new Vector4(0, 1, 0, 0));
                //cmd.Blit(_rtBlurX, _rtBlurY, _settings.blurMaterial);

                //// 输出到持久 RT
                //cmd.Blit(_rtBlurY, _resultRT);

                // 回传到屏幕遮罩材质
                if (_settings.screenMaskMaterial != null)
                {
                    //_settings.screenMaskMaterial.SetTexture(_settings.maskTexturePropertyName, _resultRT);
                    _settings.screenMaskMaterial.SetTexture(_settings.maskTexturePropertyName, _settings.sourceMaskRT);
                }

                // 释放临时 RT
                cmd.ReleaseTemporaryRT(_rtBlurX);
                cmd.ReleaseTemporaryRT(_rtBlurY);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // 持久 RT 保留以供 UI 材质采样，不在此释放
        }
    }
}