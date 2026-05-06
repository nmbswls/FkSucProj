using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    [Serializable]
    public class ThrowEventSpec
    {
        public ThrowEventKind Kind;
        [SerializeReference]
        public List<MapFightEffectCfg> Effects = new();
    }

    [Serializable]
    public class ThrowTimelineEventSpec
    {
        [Tooltip("相对投技逻辑开始时刻的时间（秒），到达后触发本组 Effects 一次")]
        public float TimeFromStart;

        [SerializeReference]
        public List<MapFightEffectCfg> Effects = new();
    }

    [Serializable]
    public class MapAbilityEffectThrowStartCfg : MapFightEffectCfg
    {
        public int Priority;
        public float Duration;

        [Tooltip("是否自动为双方添加 throwing 状态 Buff（与旧逻辑一致，结束时统一移除）")]
        public bool AutoApplyThrowingStateBuff = true;

        [Tooltip("Legacy: 投技主效果 Buff；新流程请在 ThrowPhaseEffects 的 Accept 中配置 MapAbilityEffectAddBuffCfg")]
        public string ThrowMainBuffId;

        [Tooltip("按阶段触发的效果链，经 LogicFightEffectContext 派发，可配置位移、加 Buff、伤害等")]
        public List<ThrowEventSpec> ThrowPhaseEffects = new();

        [Tooltip("0~1：进程到达该比例时触发 Impact 阶段；-1 表示不自动触发")]
        public float ImpactAtNormalizedTime = -1f;

        [Tooltip("相对投技开始的时间轴事件（如 0.3s、0.9s 各打一次伤害），每条只触发一次，可复用 MapFightEffectCfg")]
        public List<ThrowTimelineEventSpec> ThrowTimelineEvents = new();

        public MapFightEffectCfg ThrowFailEffect;
    }
}


