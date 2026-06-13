using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Entity
{
    /// <summary>
    /// 让自身或目标进行移动
    /// </summary>
    [Serializable]
    public class MapAbilityEffectControlledMoveCfg : MapFightEffectCfg
    {
        public int TargetType = 0; // 0 target 1 self
        public bool UseCastVec;
        // true 时用 ctx.TriggerPos 作为拉扯目标（如钩爪子弹命中点）
        public bool UseTriggerPos;
        // 落点沿「当前位置→目标点」方向内缩，避免穿模
        public float StopOffset = 0.55f;
        public float FixedDuration = 0.6f;
        public bool IsEnmity;
        public float ControlForce = 0f;
    }
}

