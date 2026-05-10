using System;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技时间轴用：将出手方（逻辑 entity）瞬移到当前效果上下文 TriggerPos（受害者位置）+ 可选偏移；会走 TeleportTo，表现层随 EventOnEntityMove 同步
    [Serializable]
    public class MapAbilityEffectThrowAlignLauncherToTargetCfg : MapFightEffectCfg
    {
        public Vector2 LogicOffset;
    }
}
