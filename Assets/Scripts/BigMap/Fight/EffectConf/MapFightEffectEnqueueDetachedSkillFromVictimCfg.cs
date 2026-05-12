using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 由受害者（ctx.TargetId）对来源（ctx.SourceInfo.SrcEntityId，一般为玩家）入队脱手技能
    [Serializable]
    public class MapFightEffectEnqueueDetachedSkillFromVictimCfg : MapFightEffectCfg
    {
        public string SkillId = "";
    }
}
