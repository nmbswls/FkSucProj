using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace My.Map
{
    public class PostProcessVignette : MonoBehaviour
    {
        public Volume globalVolume; // 拖入场景里的 Global Volume
        private Vignette vignette;  // 缓存 Vignette 组件

        public bool IsDanger;

        void Start()
        {
            // 从 Volume 的 Profile 中尝试获取 Vignette 组件
            if (globalVolume.profile.TryGet(out vignette))
            {
                Debug.Log("找到 Vignette 组件了！");
            }
        }

        public void SetDangerState(bool isDanger)
        {
            if (vignette == null) return;

            if (this.IsDanger == isDanger) return;
            this.IsDanger = isDanger;
            if (IsDanger)
            {
                // 开启红色，且增加强度
                vignette.color.value = new Color(0.54f, 0, 0);
                vignette.intensity.value = 0.45f; // 0~1 之间调整
                vignette.intensity.overrideState = true;
            }
            else
            {
                // 恢复正常
                vignette.intensity.value = 0f;
                // 或者切回黑色暗角
                vignette.color.value = Color.black;
                vignette.intensity.overrideState = true;
            }
        }

        // 你也可以在 Update 里像方案A那样写呼吸效果
        void Update()
        {
            if (vignette != null && IsDanger)
            {
                // 动态改变 Intensity 制造呼吸感
                float baseIntensity = 0.3f;
                float pulse = Mathf.Sin(Time.time * 6f) * 0.1f;
                vignette.intensity.value = baseIntensity + pulse;
            }
        }
    }
}
