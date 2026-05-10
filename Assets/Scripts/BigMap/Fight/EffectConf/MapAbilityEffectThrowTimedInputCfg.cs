using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技时间轴效果：头顶缩小圆环 + Space；命中带见 TimelineHoldSession 常量；成功/失败写入 OutcomeSuccess / OutcomeFail
    [Serializable]
    public class MapAbilityEffectThrowTimedInputCfg : MapFightEffectCfg
    {
        [Tooltip("提示语文案")]
        public string PromptText = "挣脱抵抗！环缩到亮圈时按 空格";

        [Tooltip("写入 ThrowContext.RunningVars 的 key")]
        public string ResultVarKey = "ThrowTimedInput";

        public float TimeoutSeconds = 2.2f;
    }
}
