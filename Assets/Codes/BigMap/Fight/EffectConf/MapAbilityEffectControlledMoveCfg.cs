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
        public float FixedDuration = 0.6f;
        public bool IsEnmity;
        public float ControlForce = 0f;
    }
}

