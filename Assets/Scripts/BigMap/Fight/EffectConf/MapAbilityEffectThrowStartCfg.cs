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

        [Tooltip("投技持续期间出手方动画标签（GetAnimOverride）；空则不改 Layer0 栈")]
        public string LauncherHoldAnimTag;

        [Tooltip("Legacy：ThrowTimelineEvents 为空时给目标的 Buff id")]
        public string ThrowMainBuffId;

        [Header("投技开始时：出手方滑向受害者逻辑位置")]
        [Tooltip("true：投技创建后立即用受控 Dash 滑向目标（平滑）；false：不对齐")]
        public bool AlignLauncherToTargetOnStart;

        [Tooltip("滑向时长（秒）；AlignLauncherToTargetOnStart 且 >0 时生效")]
        public float AlignLauncherDuration = 0.15f;

        public Vector2 AlignLauncherLogicOffset;

        [Tooltip("对齐 Dash 撞墙是否结束")]
        public bool AlignLauncherStopOnWall = true;

        [Tooltip("true：对齐持续时间内不推进投技时间轴时钟（与挂起类似，避免 QTE 在对齐完成前触发）")]
        public bool FreezeThrowTimelineDuringLauncherAlign = true;

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

        [Tooltip("受害者挣脱等（ThrowBreakFree）：终止效果链")]
        [SerializeReference]
        public List<MapFightEffectCfg> OnPlayerBreakFreeEffects = new();

        public MapFightEffectCfg ThrowFailEffect;
    }
}
