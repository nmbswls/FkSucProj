

using System;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace My.Map.Scene
{

    public class SceneVolumnManager : MonoBehaviour
    {
        public static SceneVolumnManager Instance { get; private set; }

        [Header("后期处理 Volume")]
        public Volume specialVisionVolume;
        public Volume defaultVolume;

        [Header("泛红/色彩 渐变设置")]
        public float smoothTransitionSpeed = 10f; // 画面颜色过渡速度
        public Color targetVisionColor = new Color(1f, 0.5f, 0.5f); // 猎魔人视野的泛红颜色
        private Color defaultColor = Color.white; // 正常颜色

        [Header("镜头畸变 脉冲设置")]
        public float distortionDuration = 0.5f; // 扭曲脉冲持续时间
        public AnimationCurve distortionCurve;  // 扭曲曲线 (峰值型 ^)

        public float targetVignetteIntensity = 0.5f;
        private float defaultVignetteIntensity = 0f;

        // 事件
        public static event Action<bool> OnVisionStateChanged;

        private bool isSpecialVisionActive = false;
        private float distortionTimer = 0f;

        // 提取出来的特效组件
        private LensDistortion lensDistortion;
        private ColorAdjustments colorAdjustments;


        private Renderer2DData HuntingDistortFeature;

        private Vignette vignette; // 新增：暗角组件

        private void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            // 确保整体权重始终为1，让内部参数自己决定强度
            if (specialVisionVolume != null)
            {
                specialVisionVolume.weight = 0f;
            }

            // 分别获取 镜头畸变 和 色彩调整 组件
            if (specialVisionVolume.profile.TryGet(out lensDistortion))
            {
                lensDistortion.intensity.value = 0f;
            }

            if (specialVisionVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments.colorFilter.value = defaultColor;
            }

            if (specialVisionVolume.profile.TryGet(out vignette))
            {
                vignette.intensity.value = defaultVignetteIntensity;
            }
        }

        void Update()
        {
            
            // 1. 独立处理：色彩滤镜的平滑渐变 (控制红光)
            if (colorAdjustments != null)
            {
                Color targetColor = isSpecialVisionActive ? targetVisionColor : defaultColor;
                colorAdjustments.colorFilter.value = Color.Lerp(
                    colorAdjustments.colorFilter.value,
                    targetColor,
                    Time.deltaTime * smoothTransitionSpeed 
                );
            }

            if (vignette != null)
            {
                float targetVignette = isSpecialVisionActive ? targetVignetteIntensity : defaultVignetteIntensity;
                vignette.intensity.value = Mathf.Lerp(
                    vignette.intensity.value,
                    targetVignette,
                    Time.deltaTime * smoothTransitionSpeed
                );
            }


            // 2. 独立处理：镜头畸变的动态脉冲 (不受渐变速度影响，直接读取曲线)
            if (isSpecialVisionActive && lensDistortion != null)
            {
                if (distortionTimer < distortionDuration)
                {
                    distortionTimer += Time.deltaTime;
                    float progress = distortionTimer / distortionDuration;

                    // 从曲线读取强度，此时不会被外部 weight 削弱！
                    lensDistortion.intensity.value = distortionCurve.Evaluate(progress);
                }
            }
            else if (!isSpecialVisionActive && lensDistortion != null)
            {
                // 关闭时确保畸变归零（或者你可以给关闭也加一个曲线）
                lensDistortion.intensity.value = 0f;
            }
        }

        /// <summary>
        /// 对外提供的接口，方便UI点击或其他脚本调用
        /// </summary>
        public void EnterHuntingMode(bool active)
        {
            isSpecialVisionActive = active;

            if (active)
            {
                distortionTimer = 0f; // 开启时重置畸变计时器

                specialVisionVolume.weight = 1;
                defaultVolume.weight = 0;
            }
            else
            {
                specialVisionVolume.weight = 0;
                defaultVolume.weight = 1;
            }

            OnVisionStateChanged?.Invoke(active);
        }
    }
}