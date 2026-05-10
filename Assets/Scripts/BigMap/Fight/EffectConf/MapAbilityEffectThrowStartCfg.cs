using Map.Entity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    // 投技时间轴：相对 StartTime，到达后本条只触发一次；可多条 TimeFromStart=0 串联首帧效果。
    [Serializable]
    public class ThrowTimelineEventSpec
    {
        [Tooltip("相对投技逻辑 StartTime 的时刻（秒）")]
        public float TimeFromStart;

        [SerializeReference]
        public List<MapFightEffectCfg> Effects = new();
    }

    [Serializable]
    public class MapAbilityEffectThrowStartCfg : MapFightEffectCfg
    {
        public int Priority;
        public float Duration;


        [Tooltip("Legacy：无 ThrowTimelineEvents 时给目标的 Buff id")]
        public string ThrowMainBuffId;

        [Tooltip("时间轴事件")]
        public List<ThrowTimelineEventSpec> ThrowTimelineEvents = new();

        [Tooltip("持续时间结束（正常完结）")]
        [SerializeReference]
        public List<MapFightEffectCfg> OnThrowCompleteEffects = new();

        [Tooltip("出手方打断（如眩晕）")]
        [SerializeReference]
        public List<MapFightEffectCfg> OnInterruptLauncherEffects = new();

        [Tooltip("受击方打断")]
        [SerializeReference]
        public List<MapFightEffectCfg> OnInterruptTargetEffects = new();

        [Tooltip("被更高优先级投技顶替")]
        [SerializeReference]
        public List<MapFightEffectCfg> OnSupersededEffects = new();

        [Tooltip("QTE 失败等导致的受害者挣脱（ThrowBreakFree 效果）")]
        [SerializeReference]
        public List<MapFightEffectCfg> OnQteBreakFreeEffects = new();

        public MapFightEffectCfg ThrowFailEffect;
    }
}
