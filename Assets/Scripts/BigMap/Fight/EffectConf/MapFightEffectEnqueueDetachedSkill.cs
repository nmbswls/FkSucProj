using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 在 SourceInfo.SrcEntityId 单位上入队脱手技能，技能主目标为 TargetId（与全局 Src/ Target 语义一致）
    [Serializable]
    public class MapFightEffectEnqueueDetachedSkill : MapFightEffectCfg
    {
        public string SkillId = "";
    }
}
