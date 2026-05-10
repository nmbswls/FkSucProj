using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技时间轴效果：弹出头顶 QTE（仅 Space），超时算失败；完成后解锁后续时间轴行
    [Serializable]
    public class MapAbilityEffectThrowQtePromptCfg : MapFightEffectCfg
    {
        [Tooltip("逻辑侧区分本次投技 QTE 的 id，便于日志/扩展")]
        public string QteId = "throw_qte_default";

        [Tooltip("提示语文案")]
        public string PromptText = "挣脱抵抗！连打 空格";

        [Tooltip("写入 ThrowContext.RunningVars 的 key")]
        public string ResultVarKey = "ThrowQte";

        public string SuccessValue = "1";
        public string FailValue = "0";

        public float TimeoutSeconds = 2.2f;
    }
}
