


using UnityEngine.Rendering.Universal;
using UnityEngine;
using System.Collections.Generic;

namespace My
{
    public class URPFeatureController : MonoBehaviour
    {
        public static URPFeatureController Instance { get; private set; }


        [Header("配置")]
        public Renderer2DData rendererData;

        public static string HuntingDesireDistortFeature = "FullScreenHuntingDesireDistort"; // 替换成你扭曲特性的名字

        private Dictionary<string, ScriptableRendererFeature> _cachedFeatures = new();
        private Dictionary<string, bool> _originFeatureState = new();

        private void Awake()
        {
            Instance = this;

        }


        void Start()
        {
            // 初始化时，在 Renderer Data 中寻找目标 Feature
            if (rendererData != null)
            {
                foreach (var feature in rendererData.rendererFeatures)
                {
                    _cachedFeatures[feature.name] = feature;
                    _originFeatureState[feature.name] = feature.isActive;
                }
            }

            SetHuntingDistortionEffect(false);
        }

        /// <summary>
        /// 调用这个方法来开关扭曲效果
        /// </summary>
        public void SetHuntingDistortionEffect(bool isOn)
        {
            foreach (var kv in _cachedFeatures)
            {
                if(kv.Key == HuntingDesireDistortFeature)
                {
                    kv.Value.SetActive(isOn);
                }
            }
        }

        void OnDestroy()
        {
            foreach(var feature in _cachedFeatures.Values)
            {
                var state = _originFeatureState.GetValueOrDefault(feature.name);
                feature.SetActive(state);
            }
        }

    }

}